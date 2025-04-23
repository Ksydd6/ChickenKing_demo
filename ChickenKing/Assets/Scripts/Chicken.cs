using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Chicken : MonoBehaviour
{
    [Header("行为设置")]
    public float followSpeed = 3.5f;     // 跟随速度
    public float minFollowDistance = 2f;  // 最小跟随距离
    public float maxFollowDistance = 5f;  // 最大跟随距离
    public float avoidanceRadius = 1f;    // 避让半径
    public float positionUpdateInterval = 0.5f; // 位置更新间隔

    [Header("引用")]
    public Transform target;              // 跟随目标

    [Header("状态")]
    public bool isFollowing = false;      // 是否正在跟随 - 默认为false
    public bool isCaptured = false;       // 是否被抓捕
    public bool isInFollowArea = false;   // 是否在跟随区域内

    // 新增变量，用于控制固定距离及可视化
    [Header("固定距离设置")]
    public float fixedRowSpacing = 1.0f;   // 行间固定距离
    public float fixedColSpacing = 0.8f;   // 列间固定距离
    public float distanceFromPlayer = 2.0f; // 与玩家的基础距离
    public int maxVisualizePositions = 50; // 编辑器中最多显示的位置数量
    public bool showAllPositions = true;   // 是否在编辑器中显示所有可能位置

    // 组件引用
    private NavMeshAgent navAgent;
    private Animator animator;
    private Player playerController;
    private Vector3 targetPosition;
    private float positionUpdateTimer = 0f;

    void Start()
    {
        // 初始化组件
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            navAgent.speed = followSpeed;
            navAgent.angularSpeed = 360;
            navAgent.acceleration = 8;
            navAgent.stoppingDistance = minFollowDistance;
            // 适合鸡的设置
            navAgent.height = 0.5f;
            navAgent.radius = 0.4f;
        }

        // 默认停止导航（因为默认不跟随）
        navAgent.isStopped = true;

        animator = GetComponent<Animator>();

        // 如果没有目标，尝试找到玩家
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                playerController = player.GetComponent<Player>();
            }
        }
        else if (target.CompareTag("Player"))
        {
            playerController = target.GetComponent<Player>();
        }
    }

    void Update()
    {
        if (isCaptured)
            return; // 被抓后不再移动

        // 如果不在跟随状态，保持静止
        if (!isFollowing)
            return;

        // 检查是否在玩家的跟随区域内 - 现在只处理列表添加/移除
        CheckIfInFollowArea();

        if (target != null && playerController != null)
        {
            // 确保导航代理处于活动状态
            if (navAgent.isStopped)
            {
                navAgent.isStopped = false;
            }

            // 直接使用FollowInFormation进行跟随
            FollowInFormation();
        }
        else
        {
            // 如果没有目标，尝试寻找玩家
            FindPlayer();
        }
    }

    // 查找玩家
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            playerController = player.GetComponent<Player>();
            // 不再自动设置isFollowing为true
        }
    }

    // 检查是否在玩家的三角形跟随区域内
    void CheckIfInFollowArea()
    {
        if (playerController != null)
        {
            // 修改后的逻辑：不再检查位置条件
            // 如果已经在跟随状态，但不在跟随列表中，直接加入
            if (isFollowing && !playerController.followingChickens.Contains(transform))
            {
                playerController.AddChickenToFollow(transform);
                isInFollowArea = true; // 设置为true以保持状态一致性
            }
            // 如果不再跟随，从玩家的跟随列表中移除
            else if (!isFollowing && playerController.followingChickens.Contains(transform))
            {
                playerController.RemoveChickenFromFollow(transform);
                isInFollowArea = false;
            }
        }
    }

    // 检查是否在玩家后方 - 简化版本，总是返回true
    bool IsInPlayerBackArea()
    {
        // 不再实际检查位置，永远返回true
        return true;
    }

    // 跟随目标
    void FollowTarget()
    {
        if (playerController == null || target == null) return;

        // 获取三角形区域信息
        Vector3[] vertices = playerController.FollowAreaVertices;
        if (vertices == null || vertices.Length < 3) return;

        // 获取跟随列表中自己的索引和总数
        int myIndex = playerController.followingChickens.IndexOf(transform);
        if (myIndex == -1) myIndex = 0; // 保护处理
        int totalChickens = playerController.followingChickens.Count;

        // 不再检查是否在玩家后方，直接计算分配位置
        // 计算这只鸡在队列中的固定位置
        Vector3 assignedPosition = CalculateAssignedPosition(vertices, myIndex, totalChickens);

        // 判断是否需要移动
        float distanceToAssignedPos = Vector3.Distance(transform.position, assignedPosition);

        if (distanceToAssignedPos > 0.5f) // 只有当距离分配位置超过阈值时才移动
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(assignedPosition);
        }
        else
        {
            // 已经在指定位置附近，完全静止
            navAgent.isStopped = true;
        }
    }

    // 计算鸡在队列中的固定位置
    Vector3 CalculateAssignedPosition(Vector3[] vertices, int index, int totalCount)
    {
        // 计算三角形的轴线和宽度
        Vector3 backwardAxis = (vertices[1] + vertices[2]) / 2f - vertices[0];
        float triangleLength = backwardAxis.magnitude;
        backwardAxis = backwardAxis.normalized;

        Vector3 rightAxis = (vertices[1] - vertices[2]).normalized;
        float triangleMaxWidth = Vector3.Distance(vertices[1], vertices[2]);

        // 使用固定的行列数计算方法
        // 第一排最多能放多少只鸡
        int firstRowMaxCount = Mathf.FloorToInt(triangleMaxWidth * 0.3f / fixedColSpacing) + 1;
        if (firstRowMaxCount < 1) firstRowMaxCount = 1;

        // 根据索引计算在哪一行
        int row = 0;
        int processedCount = 0;

        // 计算每行最多容纳的鸡数量（每行宽度不同）
        while (true)
        {
            // 计算当前行宽度比例
            float loopRowWidthRatio = Mathf.Min(0.3f + 0.7f * ((float)row / 10f), 1.0f);
            // 当前行最多容纳数量
            int currentRowMaxCount = Mathf.FloorToInt(triangleMaxWidth * loopRowWidthRatio / fixedColSpacing) + 1;

            // 如果当前行放得下，就放在这行
            if (index < processedCount + currentRowMaxCount)
            {
                break;
            }

            // 否则处理下一行
            processedCount += currentRowMaxCount;
            row++;
        }

        // 计算在当前行的位置
        int colIndexInRow = index - processedCount;

        // 当前行的最大宽度
        float finalRowWidthRatio = Mathf.Min(0.3f + 0.7f * ((float)row / 10f), 1.0f);
        float rowWidth = triangleMaxWidth * finalRowWidthRatio;

        // 当前行最多容纳数量
        int colsInThisRow = Mathf.FloorToInt(rowWidth / fixedColSpacing) + 1;

        // 确保colIndexInRow在有效范围内
        colIndexInRow = Mathf.Clamp(colIndexInRow, 0, colsInThisRow - 1);

        // 当前行的起始点 (从玩家位置开始按固定距离向后偏移)
        Vector3 rowStartPoint = vertices[0] + backwardAxis * (distanceFromPlayer + fixedRowSpacing * row);

        // 计算横向偏移 (当前列位置 - 行中心位置) * 固定列间距
        float rowOffset = ((float)colIndexInRow - (colsInThisRow - 1) / 2.0f) * fixedColSpacing;

        // 最终位置计算
        Vector3 position = rowStartPoint + rightAxis * rowOffset;

        // 确保Y坐标正确
        position.y = vertices[0].y;

        return position;
    }

    // 在阵型中跟随玩家
    void FollowInFormation()
    {
        positionUpdateTimer += Time.deltaTime;

        // 定时更新目标位置，减少计算负担但不要太不频繁
        if (positionUpdateTimer >= positionUpdateInterval || targetPosition == Vector3.zero)
        {
            positionUpdateTimer = 0f;

            // 在跟随区域内找一个位置
            if (playerController != null)
            {
                Vector3[] vertices = playerController.FollowAreaVertices;

                if (vertices != null && vertices.Length >= 3)
                {
                    // 获取跟随列表中自己的索引
                    int myIndex = playerController.followingChickens.IndexOf(transform);

                    // 如果找不到索引，使用一个基于实例ID的唯一值，确保不同鸡获得不同位置
                    if (myIndex == -1)
                    {
                        myIndex = gameObject.GetInstanceID() % 100; // 使用实例ID作为后备索引
                    }

                    // 计算总共的鸡数量，确保至少为1以避免除零错误
                    int totalChickens = Mathf.Max(1, playerController.followingChickens.Count);

                    // 计算指定位置
                    targetPosition = CalculateAssignedPosition(vertices, myIndex, totalChickens);

                    // 设置导航目标
                    navAgent.SetDestination(targetPosition);
                }
                else
                {
                    // 如果无法获取跟随区域顶点，直接跟随在玩家后方
                    Vector3 backPosition = target.position - target.forward * distanceFromPlayer;
                    navAgent.SetDestination(backPosition);
                }
            }
        }

        // 确保导航代理没有停止
        if (navAgent.isStopped)
        {
            navAgent.isStopped = false;
        }

        // 调试输出
        Debug.DrawLine(transform.position, targetPosition, Color.green);
    }

    // 被抓住
    public void GetCaptured()
    {
        isCaptured = true;
        navAgent.isStopped = true;

        // 如果在跟随列表中，移除
        if (playerController != null)
        {
            playerController.RemoveChickenFromFollow(transform);
        }

        // 播放被抓动画（如果有）
        if (animator != null)
        {
            animator.SetTrigger("Captured");
        }

        // 让鸡在2秒后消失
        StartCoroutine(DisappearAfterCapture());
    }

    // 被抓后消失
    IEnumerator DisappearAfterCapture()
    {
        yield return new WaitForSeconds(2f);

        // 消失前的简单效果
        float fadeTime = 1.0f;
        float startTime = Time.time;

        // 获取所有渲染器组件
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // 淡出效果
        while (Time.time < startTime + fadeTime)
        {
            float t = (Time.time - startTime) / fadeTime;
            foreach (Renderer r in renderers)
            {
                Color newColor = r.material.color;
                newColor.a = 1 - t;
                r.material.color = newColor;
            }
            yield return null;
        }

        // 销毁鸡对象
        Destroy(gameObject);
    }

    // 添加到鸡群
    public void JoinFlock(Transform flockTarget)
    {
        target = flockTarget;
        isFollowing = true;

        // 获取玩家控制器组件
        if (target.CompareTag("Player"))
        {
            playerController = target.GetComponent<Player>();
        }

        // 确保导航代理准备就绪
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.speed = followSpeed;

            // 立即更新目标位置
            if (playerController != null)
            {
                Vector3[] vertices = playerController.FollowAreaVertices;
                if (vertices != null && vertices.Length >= 3)
                {
                    // 使用实例ID获取一个唯一位置
                    int tempIndex = gameObject.GetInstanceID() % 100;
                    int totalCount = Mathf.Max(1, playerController.followingChickens.Count + 1);
                    Vector3 initialTargetPos = CalculateAssignedPosition(vertices, tempIndex, totalCount);

                    // 立即设置目标位置并开始移动
                    targetPosition = initialTargetPos;
                    navAgent.SetDestination(targetPosition);
                }
                else
                {
                    // 后备方案：直接朝向玩家后方移动
                    Vector3 backPos = target.position - target.forward * distanceFromPlayer;
                    navAgent.SetDestination(backPos);
                }
            }
        }

        // 标记为在跟随区域内
        isInFollowArea = true;

        // 设置为未被捕获状态
        isCaptured = false;
    }

    // 在编辑器中可视化导航路径
    void OnDrawGizmosSelected()
    {
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // 显示目标位置
        if (isInFollowArea && targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }

    // 在编辑器中可视化所有可能位置
    void OnDrawGizmos()
    {
        // 如果不需要显示所有位置，则退出
        if (!showAllPositions)
            return;

        // 尝试获取玩家引用
        if (playerController == null && target != null && target.CompareTag("Player"))
        {
            playerController = target.GetComponent<Player>();
        }
        else if (playerController == null && target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<Player>();
            }
        }

        // 如果有玩家引用，显示所有可能位置
        if (playerController != null)
        {
            // 获取跟随区域顶点
            Vector3[] vertices = playerController.FollowAreaVertices;

            // 如果在编辑模式，计算预览顶点
            if (!Application.isPlaying)
            {
                Vector3 playerPos = playerController.transform.position;
                Vector3 backDirection = -playerController.transform.forward;

                vertices = new Vector3[]
                {
                    playerPos,
                    playerPos + backDirection * playerController.followAreaLength + playerController.transform.right * playerController.followAreaWidth / 2f,
                    playerPos + backDirection * playerController.followAreaLength - playerController.transform.right * playerController.followAreaWidth / 2f
                };
            }

            if (vertices != null && vertices.Length >= 3)
            {
                // 显示可能的位置
                for (int i = 0; i < maxVisualizePositions; i++)
                {
                    Vector3 pos = CalculateAssignedPosition(vertices, i, maxVisualizePositions);
                    // 使用渐变颜色，前面的位置显示为蓝色，后面的显示为红色
                    float t = (float)i / maxVisualizePositions;
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, t);
                    Gizmos.DrawWireSphere(pos, 0.15f);

                    // 显示索引号
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, i.ToString());
#endif
                }
            }
        }
    }
}