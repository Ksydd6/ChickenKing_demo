using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EmployeeChickenHunter : MonoBehaviour
{
    [Header("导航设置")]
    public NavMeshAgent navAgent;            // 导航代理组件
    public float patrolSpeed = 3.0f;         // 巡逻速度
    public float chaseSpeed = 5.0f;          // 追击速度

    [Header("巡逻设置")]
    public Transform[] patrolPoints;         // 巡逻点数组
    public float waitTime = 2f;              // 在巡逻点等待时间
    private int currentPatrolIndex = 0;      // 当前巡逻点索引
    private bool isWaiting = false;          // 是否在巡逻点等待

    [Header("视野设置")]
    public float viewRadius = 10f;           // 视野半径（仅用于初次发现目标）
    public float viewAngle = 90f;            // 视野角度（仅用于初次发现目标）
    public LayerMask targetMask;             // 目标层级掩码(包括玩家和鸡)
    public LayerMask obstacleMask;           // 障碍物层级掩码
    public Transform eyePosition;            // 眼睛位置
    public float targetCheckInterval = 0.5f; // 目标检查间隔

    [Header("捕鸡设置")]
    public int maxCaptureCount = 3;          // 最多抓捕的鸡数量
    public float captureInterval = 2f;       // 抓捕间隔（秒）
    public float captureRange = 1.5f;        // 抓捕范围

    [Header("状态")]
    public int currentCaptureCount = 0;      // 当前已抓捕的鸡数量
    public bool isCapturing = false;         // 是否正在抓捕
    public bool isCoolingDown = false;       // 是否在冷却中
    public bool hasTarget = false;           // 是否已经锁定目标

    // 追踪目标
    private Transform currentTarget;         // 当前追踪的目标
    private Chicken targetChicken;           // 追踪的鸡对象引用
    private Transform playerTarget;          // 玩家对象引用
    private float lastTargetCheckTime;       // 上次检查目标的时间
    private Vector3 lastKnownPosition;       // 目标最后已知位置
    private GameObject player;               // 玩家游戏对象的缓存
    private ChickenManager chickenManager;   // 鸡群管理器引用

    // 员工状态枚举
    public enum HunterState { Patrol, ChaseTarget, Capturing, Cooldown }
    public HunterState currentState = HunterState.Patrol;

    void Start()
    {
        // 获取导航代理组件
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();

        // 如果没有设置眼睛位置，使用当前对象位置
        if (eyePosition == null)
            eyePosition = transform;

        // 初始化巡逻行为
        navAgent.speed = patrolSpeed;
        if (patrolPoints.Length > 0)
            SetDestinationToNextPatrolPoint();

        // 初始化上次检查时间
        lastTargetCheckTime = Time.time;

        // 缓存玩家引用（假设场景中只有一个玩家）
        player = GameObject.FindGameObjectWithTag("Player");

        // 获取鸡群管理器
        chickenManager = FindObjectOfType<ChickenManager>();
    }

    void Update()
    {
        // 如果已经抓够鸡，就消失
        if (currentCaptureCount >= maxCaptureCount)
        {
            StartCoroutine(Disappear());
            return;
        }

        // 定期检查目标
        if (Time.time >= lastTargetCheckTime + targetCheckInterval &&
            currentState != HunterState.Capturing &&
            currentState != HunterState.Cooldown)
        {
            if (!hasTarget)
            {
                // 还没有发现目标，使用视野范围寻找目标
                SearchNewTargetWithinView();
            }
            else
            {
                // 已经发现目标，直接追踪（无论距离多远）
                UpdateTargetPosition();
            }

            lastTargetCheckTime = Time.time;
        }

        // 根据当前状态执行对应行为
        switch (currentState)
        {
            case HunterState.Patrol:
                Patrol();
                break;

            case HunterState.ChaseTarget:
                ChaseCurrentTarget();
                break;

            case HunterState.Capturing:
                // 正在抓鸡，不移动
                if (!isCapturing)
                {
                    // 抓捕完成后，重置状态
                    currentState = HunterState.Cooldown;
                    StartCoroutine(CooldownAfterCapture());
                }
                break;

            case HunterState.Cooldown:
                // 抓捕后冷却状态，不执行任何操作
                break;
        }
    }

    // 更新目标位置 - 持续检查并追踪最近的目标
    void UpdateTargetPosition()
    {
        // 记录当前目标信息，用于后续比较
        Transform previousTarget = currentTarget;
        string previousTargetName = previousTarget != null ? previousTarget.name : "无";

        // 搜索视野范围内的所有可能目标
        float searchRadius = 30f; // 可以在类变量中定义一个搜索半径
        Collider[] possibleTargets = Physics.OverlapSphere(transform.position, searchRadius, targetMask);

        // 找出最近的有效目标
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        bool isNearestChicken = false;
        Chicken nearestChickenComponent = null;

        foreach (Collider targetCollider in possibleTargets)
        {
            // 忽略已捕获的鸡
            Chicken chicken = targetCollider.GetComponent<Chicken>();
            if (chicken != null && chicken.isCaptured)
                continue;

            Transform target = targetCollider.transform;
            float distance = Vector3.Distance(transform.position, target.position);

            // 检查是否有障碍物遮挡视线
            if (!Physics.Raycast(eyePosition.position, target.position - eyePosition.position, distance, obstacleMask))
            {
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = target;

                    // 判断是否是鸡
                    isNearestChicken = chicken != null;
                    nearestChickenComponent = chicken;
                }
            }
        }

        // 如果找到了目标
        if (nearestTarget != null)
        {
            // 检查是否与当前目标不同
            if (currentTarget != nearestTarget)
            {
                // 更新目标
                currentTarget = nearestTarget;
                lastKnownPosition = nearestTarget.position;

                // 更新目标类型引用
                if (isNearestChicken)
                {
                    targetChicken = nearestChickenComponent;
                    playerTarget = null;
                    Debug.Log($"切换目标: 从[{previousTargetName}]到[鸡:{nearestTarget.name}]");
                }
                else if (nearestTarget.CompareTag("Player"))
                {
                    playerTarget = nearestTarget;
                    targetChicken = null;
                    Debug.Log($"切换目标: 从[{previousTargetName}]到[玩家]");
                }

                // 确保处于追踪状态
                if (currentState != HunterState.ChaseTarget &&
                    currentState != HunterState.Capturing &&
                    currentState != HunterState.Cooldown)
                {
                    currentState = HunterState.ChaseTarget;
                    navAgent.speed = chaseSpeed;
                }
            }
            else
            {
                // 目标没变，只更新位置
                lastKnownPosition = currentTarget.position;
            }

            // 标记为已有目标
            hasTarget = true;
        }
        else if (currentTarget == null)
        {
            // 如果没有找到任何目标但当前目标引用无效
            if (hasTarget)
            {
                // 尝试寻找新的鸡目标
                if (chickenManager != null)
                {
                    Chicken newChicken = GetNearestChicken(transform.position);
                    if (newChicken != null && !newChicken.isCaptured)
                    {
                        currentTarget = newChicken.transform;
                        targetChicken = newChicken;
                        playerTarget = null;
                        lastKnownPosition = currentTarget.position;
                        Debug.Log("未找到附近目标，锁定新的鸡目标");
                        return;
                    }
                }

                // 尝试重新获取玩家引用
                if (player != null)
                {
                    playerTarget = player.transform;
                    currentTarget = playerTarget;
                    targetChicken = null;
                    lastKnownPosition = currentTarget.position;
                    Debug.Log("未找到附近目标，重新锁定玩家");
                    return;
                }

                // 都没找到，前往最后已知位置
                if (currentState != HunterState.ChaseTarget)
                {
                    currentState = HunterState.ChaseTarget;
                    navAgent.speed = chaseSpeed;
                    navAgent.SetDestination(lastKnownPosition);
                    Debug.Log("所有目标均丢失，前往最后已知位置");
                }
            }
        }
    }

    // 获取最近的鸡
    private Chicken GetNearestChicken(Vector3 position)
    {
        if (chickenManager == null) return null;

        Chicken nearestChicken = null;
        float nearestDistance = float.MaxValue;

        // 搜索场景中所有鸡
        Collider[] chickens = Physics.OverlapSphere(position, 50f, LayerMask.GetMask("Chicken"));

        foreach (Collider col in chickens)
        {
            Chicken chicken = col.GetComponent<Chicken>();
            if (chicken != null && !chicken.isCaptured)
            {
                float distance = Vector3.Distance(position, chicken.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestChicken = chicken;
                }
            }
        }

        return nearestChicken;
    }

    // 未发现目标时，使用视野限制搜索新目标
    bool SearchNewTargetWithinView()
    {
        // 获取视野范围内的所有碰撞体
        Collider[] targetsInViewRadius = Physics.OverlapSphere(eyePosition.position, viewRadius, targetMask);

        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        bool isNearestChicken = false;

        foreach (Collider targetCollider in targetsInViewRadius)
        {
            Transform target = targetCollider.transform;
            Vector3 dirToTarget = (target.position - eyePosition.position).normalized;

            // 检查目标是否在视野角度内（只在初次发现时检查）
            if (Vector3.Angle(eyePosition.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(eyePosition.position, target.position);

                // 检查是否有障碍物遮挡视线
                if (!Physics.Raycast(eyePosition.position, dirToTarget, distToTarget, obstacleMask))
                {
                    // 如果这个目标更近，更新最近目标
                    if (distToTarget < nearestDistance)
                    {
                        nearestDistance = distToTarget;
                        nearestTarget = target;

                        // 判断是否是鸡
                        Chicken chicken = target.GetComponent<Chicken>();
                        isNearestChicken = chicken != null && !chicken.isCaptured;

                        // 判断是否是玩家
                        if (target.CompareTag("Player"))
                        {
                            isNearestChicken = false;
                        }
                    }
                }
            }
        }

        // 如果找到了新目标
        if (nearestTarget != null)
        {
            // 更新当前目标
            currentTarget = nearestTarget;
            lastKnownPosition = nearestTarget.position;

            // 更新相应引用
            if (isNearestChicken)
            {
                targetChicken = nearestTarget.GetComponent<Chicken>();
                playerTarget = null;
            }
            else if (nearestTarget.CompareTag("Player"))
            {
                playerTarget = nearestTarget;
                targetChicken = null;
            }

            // 标记为已有目标 - 这是关键，从此将永远追踪
            hasTarget = true;

            // 切换到追击状态
            currentState = HunterState.ChaseTarget;
            navAgent.speed = chaseSpeed;
            Debug.Log("发现新目标！开始追踪: " + nearestTarget.name);

            return true;
        }

        return false;
    }

    // 巡逻行为
    void Patrol()
    {
        // 如果没有设置巡逻点，则原地待命
        if (patrolPoints.Length == 0)
            return;

        // 如果当前不在等待状态且已接近目标点
        if (!isWaiting && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            // 开始等待
            isWaiting = true;
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    // 在巡逻点等待
    IEnumerator WaitAtPatrolPoint()
    {
        // 等待指定时间
        yield return new WaitForSeconds(waitTime);

        // 前往下一个巡逻点
        SetDestinationToNextPatrolPoint();
        isWaiting = false;
    }

    // 设置前往下一个巡逻点
    void SetDestinationToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        // 获取下一个巡逻点
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    // 追击当前目标
    void ChaseCurrentTarget()
    {
        if (currentTarget == null)
        {
            // 目标被销毁，但仍保持追踪最后位置
            navAgent.SetDestination(lastKnownPosition);
            return;
        }

        // 设置导航目标为当前追踪对象
        navAgent.SetDestination(currentTarget.position);

        // 更新最后已知位置
        lastKnownPosition = currentTarget.position;

        // 判断是追击玩家还是鸡
        if (targetChicken != null)
        {
            // 如果是追击鸡，检查是否可以抓捕
            float distanceToChicken = Vector3.Distance(transform.position, targetChicken.transform.position);
            if (distanceToChicken <= captureRange && !targetChicken.isCaptured)
            {
                // 开始抓捕
                StartCoroutine(CaptureChicken(targetChicken));
            }
        }
        else if (playerTarget != null)
        {
            // 如果是追击玩家，可以添加玩家特定行为
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= captureRange)
            {
                // 在这里添加捕获玩家的逻辑
                // 例如：玩家.GetCaught();
                Debug.Log("玩家已被捕获!");

                // 可以添加一个冷却期，或者其他后续处理
                currentState = HunterState.Cooldown;
                StartCoroutine(CooldownAfterCapture());
            }
        }
    }

    // 抓捕鸡
    IEnumerator CaptureChicken(Chicken chicken)
    {
        // 设置状态
        currentState = HunterState.Capturing;
        isCapturing = true;

        // 停止移动
        navAgent.isStopped = true;

        // 执行抓捕动画/声音（如果有）
        // GetComponent<Animator>().SetTrigger("Capture");

        // 等待一小段时间模拟抓捕动作
        yield return new WaitForSeconds(0.5f);

        // 确保鸡还在且未被抓
        if (chicken != null && !chicken.isCaptured)
        {
            // 抓住鸡
            chicken.GetCaptured();

            // 增加计数
            currentCaptureCount++;

            // 如果已达到最大抓捕数，准备消失
            if (currentCaptureCount >= maxCaptureCount)
            {
                yield return new WaitForSeconds(2f);
                StartCoroutine(Disappear());
                yield break;
            }
        }

        // 完成抓捕
        isCapturing = false;
        targetChicken = null;
        currentTarget = null;

        // 注意：保持hasTarget为true，继续寻找新目标
        // 只有在玩家原意的情况下才会放弃追踪

        // 让NavMeshAgent继续工作
        navAgent.isStopped = false;
    }

    // 抓捕后冷却
    IEnumerator CooldownAfterCapture()
    {
        isCoolingDown = true;

        // 冷却时间
        yield return new WaitForSeconds(captureInterval);

        isCoolingDown = false;

        // 冷却结束，立即回到追踪状态（因为我们永远不放弃目标）
        currentState = HunterState.ChaseTarget;
        navAgent.speed = chaseSpeed;
    }

    // 抓够鸡后消失
    IEnumerator Disappear()
    {
        // 停止一切行为
        navAgent.isStopped = true;

        // 淡出效果
        float fadeTime = 1.5f;
        float startTime = Time.time;

        // 获取所有渲染器
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // 淡出
        while (Time.time < startTime + fadeTime)
        {
            float t = (Time.time - startTime) / fadeTime;

            foreach (Renderer r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color newColor = r.material.color;
                    newColor.a = 1 - t;
                    r.material.color = newColor;
                }
            }

            yield return null;
        }

        // 销毁对象
        Destroy(gameObject);
    }

    // 绘制视野范围（用于调试）
    private void OnDrawGizmosSelected()
    {
        // 视野范围（初次发现用）
        Gizmos.color = Color.yellow;
        Transform viewPos = eyePosition != null ? eyePosition : transform;
        Gizmos.DrawWireSphere(viewPos.position, viewRadius);

        // 视野角度（初次发现用）
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(viewPos.position, viewPos.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(viewPos.position, viewPos.position + viewAngleB * viewRadius);

        // 抓捕范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, captureRange);

        // 目标最后已知位置
        if (hasTarget)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }
    }

    // 辅助方法：获取角度对应的方向向量
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += eyePosition != null ? eyePosition.eulerAngles.y : transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}