using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Employee : MonoBehaviour
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
    public float viewRadius = 10f;           // 视野半径
    public float viewAngle = 90f;            // 视野角度
    public LayerMask playerMask;             // 玩家层级掩码
    public LayerMask obstacleMask;           // 障碍物层级掩码
    public Transform eyePosition;            // 眼睛位置

    // AI状态
    public enum AIState { Patrol, Chase }
    public AIState currentState = AIState.Patrol;

    // 目标玩家
    private Transform playerTarget;
    private bool hasDetectedPlayer = false;  // 是否已经发现过玩家

    // Start is called before the first frame update
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
    }

    // Update is called once per frame
    void Update()
    {
        // 如果还没有发现过玩家，继续检测
        if (!hasDetectedPlayer)
        {
            CheckForPlayerInView();
        }

        // 根据当前状态执行对应行为
        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                break;

            case AIState.Chase:
                Chase();
                break;
        }
    }

    // 检查视野中是否有玩家
    void CheckForPlayerInView()
    {
        // 获取视野范围内的所有碰撞体
        Collider[] playersInViewRadius = Physics.OverlapSphere(eyePosition.position, viewRadius, playerMask);

        for (int i = 0; i < playersInViewRadius.Length; i++)
        {
            Transform target = playersInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - eyePosition.position).normalized;

            // 检查目标是否在视野角度内
            if (Vector3.Angle(eyePosition.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(eyePosition.position, target.position);

                // 检查是否有障碍物遮挡视线
                if (!Physics.Raycast(eyePosition.position, dirToTarget, distToTarget, obstacleMask))
                {
                    // 视野内检测到玩家，永久切换到追击状态
                    playerTarget = target;
                    hasDetectedPlayer = true;  // 标记为已发现玩家

                    // 切换到追击状态
                    currentState = AIState.Chase;
                    navAgent.speed = chaseSpeed;

                    // 播放警报音效或动画（如果有）
                    Debug.Log("发现玩家！开始永久追击!");
                }
            }
        }
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

    // 追击行为 - 一旦进入追击状态，就会永久追击玩家
    void Chase()
    {
        if (playerTarget != null)
        {
            // 直接设置目标为玩家位置，持续追击
            navAgent.SetDestination(playerTarget.position);
        }
        else
        {
            // 如果玩家被销毁或者不存在了，寻找新的Player对象
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }

    // 绘制视野范围（用于调试）
    private void OnDrawGizmosSelected()
    {
        // 视野范围
        Gizmos.color = Color.yellow;
        Transform viewPos = eyePosition != null ? eyePosition : transform;
        Gizmos.DrawWireSphere(viewPos.position, viewRadius);

        // 视野角度
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(viewPos.position, viewPos.position + viewAngleA * viewRadius);
        Gizmos.DrawLine(viewPos.position, viewPos.position + viewAngleB * viewRadius);
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
