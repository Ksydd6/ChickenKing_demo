using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // 添加UI命名空间

public class StaminaSettings
{
    [Tooltip("最大体力值")]
    public float maxStamina = 1f;

    [Tooltip("从0恢复到1所需时间(秒)")]
    public float fullRecoveryTime = 2f;

    [Tooltip("从1消耗到0所需时间(秒)")]
    public float fullDepletionTime = 2f;
}

public class Player : MonoBehaviour
{
    [Header("基础设置")]
    public float moveSpeed = 5f;          // 移动速度
    public float crouchSpeed = 2.5f;      // 蹲伏时的移动速度
    public float normalHeight = 2f;       // 正常站立高度
    public float crouchHeight = 1f;       // 蹲伏时的高度

    public float runSpeed = 7f;         //冲刺速度

    [Header("音效设置")]
    public float footstepInterval = 0.5f; // 脚步声间隔
    public AudioClip[] footstepSounds;    // 脚步声音效
    public AudioSource audioSource;        // 音源组件

    [Header("鸡群跟随设置")]
    public float followAreaLength = 5f;   // 三角形跟随区域长度
    public float followAreaWidth = 3f;    // 三角形跟随区域宽度
    public Color followAreaColor = new Color(0, 1, 0, 0.2f); // 跟随区域颜色（绿色半透明）
    public int maxFollowingChickens = 50; // 最大跟随鸡数量
    public List<Transform> followingChickens = new List<Transform>(); // 跟随的鸡列表

    [Header("鸡交互设置")]
    public float interactionRadius = 3f;      // 与鸡交互的范围
    public LayerMask chickenLayerMask;        // 鸡的层级掩码
    public KeyCode interactKey = KeyCode.E;   // 互动按键
    public Text chickenCountText;             // 显示当前跟随的鸡的数量
    public GameObject interactionPrompt;      // 互动提示UI

    [Header("状态")]
    public bool isCrouching = false;      // 是否正在蹲伏
    public bool isHiding = false;         // 是否在隐藏状态
    public bool isRunning = false;        //是否奔跑

    // 组件引用
    private CharacterController characterController;
    private float footstepTimer;
    private Vector3 moveDirection;
    private ChickenManager chickenManager;  // 鸡群管理器引用
    private Chicken nearestChicken;         // 最近的可交互鸡

    [Header("体力设置")]
    public StaminaSettings settings;

    private float currentStamina;   //实际体力
    private float recoveryRate;
    private float depletionRate;

    // 三角形跟随区域顶点
    private Vector3[] followAreaVertices = new Vector3[3];
    private bool showFollowArea = true;

    //公开属性
    public float CurrentStamina => currentStamina;
    public float StaminaPercent => currentStamina / settings.maxStamina;
    public bool IsRunning => isRunning;
    public bool CanRun => currentStamina > 0;
    public Vector3[] FollowAreaVertices => followAreaVertices;

    //初始化
    private void Awake()
    {
        if (settings == null)
        {
            settings = new StaminaSettings(); // 默认初始化
            Debug.LogWarning("StaminaSettings 未在 Inspector 中赋值，已使用默认值初始化。");
        }

        InitializeStamina();
    }

    void Start()
    {
        // 获取必要组件
        characterController = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 获取鸡群管理器引用
        chickenManager = FindObjectOfType<ChickenManager>();
        if (chickenManager == null)
        {
            Debug.LogWarning("场景中没有ChickenManager实例！");
        }

        // 默认隐藏交互提示
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        // 初始化计数UI
        UpdateChickenCountUI();
    }

    private void InitializeStamina()
    {
        currentStamina = settings.maxStamina;
        recoveryRate = settings.maxStamina / settings.fullRecoveryTime;
        depletionRate = settings.maxStamina / settings.fullDepletionTime;
    }

    void Update()
    {
        HandleRecover();
        HandleRun();
        HandleMovement();
        HandleCrouch();
        UpdateFollowArea();

        // 检查附近的鸡
        CheckForNearbyChickens();

        // 处理与鸡的交互输入
        HandleChickenInteraction();

        // 更新UI
        UpdateChickenCountUI();
    }

    // 更新三角形跟随区域
    void UpdateFollowArea()
    {
        // 计算三角形顶点位置（相对于玩家位置和朝向）
        Vector3 playerPos = transform.position;
        Vector3 backDirection = -transform.forward; // 玩家后方向量

        // 三角形顶点（一个在玩家位置，两个在后方）
        followAreaVertices[0] = playerPos; // 直接使用玩家位置作为第一个顶点
        followAreaVertices[1] = playerPos + backDirection * followAreaLength + transform.right * followAreaWidth / 2f;
        followAreaVertices[2] = playerPos + backDirection * followAreaLength - transform.right * followAreaWidth / 2f;

        // 检测是否有鸡在跟随区域内并管理跟随的鸡
        ManageFollowingChickens();
    }

    // 管理跟随的鸡群
    void ManageFollowingChickens()
    {
        // 这里可以实现寻找并管理跟随鸡群的逻辑
        // 例如检测哪些鸡在三角形区域内并加入跟随列表

        // 为跟随的鸡分配位置
        ArrangeFollowingChickens();
    }

    // 为跟随的鸡分配位置
    void ArrangeFollowingChickens()
    {
        // 在三角形区域内为每只鸡分配位置
        if (followingChickens.Count == 0) return;

        for (int i = 0; i < followingChickens.Count; i++)
        {
            if (followingChickens[i] == null) continue;

            // 计算在三角形内的随机位置
            float t1 = Random.value;
            float t2 = Random.value;

            if (t1 + t2 > 1)
            {
                t1 = 1 - t1;
                t2 = 1 - t2;
            }

            float t3 = 1 - t1 - t2;

            Vector3 targetPosition = t1 * followAreaVertices[0] + t2 * followAreaVertices[1] + t3 * followAreaVertices[2];

            // 这里可以实现鸡移动到目标位置的逻辑
            // 比如：followingChickens[i].position = Vector3.Lerp(followingChickens[i].position, targetPosition, Time.deltaTime * chickenMoveSpeed);
        }
    }

    // 判断点是否在三角形内
    public bool IsPointInFollowArea(Vector3 point)
    {
        Vector3 p1 = followAreaVertices[0];
        Vector3 p2 = followAreaVertices[1];
        Vector3 p3 = followAreaVertices[2];

        // 忽略Y坐标，只在XZ平面上判断
        p1.y = point.y;
        p2.y = point.y;
        p3.y = point.y;

        // 利用叉积判断点是否在三角形内
        Vector3 v0 = p3 - p1;
        Vector3 v1 = p2 - p1;
        Vector3 v2 = point - p1;

        float dot00 = Vector3.Dot(v0, v0);
        float dot01 = Vector3.Dot(v0, v1);
        float dot02 = Vector3.Dot(v0, v2);
        float dot11 = Vector3.Dot(v1, v1);
        float dot12 = Vector3.Dot(v1, v2);

        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }

    // 添加一只鸡到跟随列表
    public bool AddChickenToFollow(Transform chicken)
    {
        if (followingChickens.Count < maxFollowingChickens && !followingChickens.Contains(chicken))
        {
            followingChickens.Add(chicken);
            return true;
        }
        return false;
    }

    // 从跟随列表中移除一只鸡
    public bool RemoveChickenFromFollow(Transform chicken)
    {
        return followingChickens.Remove(chicken);
    }

    // 检查附近是否有可交互的鸡
    void CheckForNearbyChickens()
    {
        nearestChicken = null;

        // 获取附近的鸡
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, chickenLayerMask);

        float nearestDistance = float.MaxValue;

        foreach (Collider col in hitColliders)
        {
            Chicken chicken = col.GetComponent<Chicken>();

            // 确保是有效的鸡，且未被抓，且未加入鸡群
            if (chicken != null && !chicken.isCaptured && !chicken.isFollowing)
            {
                float distance = Vector3.Distance(transform.position, chicken.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestChicken = chicken;
                }
            }
        }

        // 根据是否有附近的鸡显示或隐藏提示
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(nearestChicken != null);
        }
    }

    // 处理与鸡的交互输入
    void HandleChickenInteraction()
    {
        // 如果按下交互键并且附近有鸡
        if (Input.GetKeyDown(interactKey) && nearestChicken != null)
        {
            // 让鸡加入鸡群
            if (AddChickenToFollow(nearestChicken.transform))
            {
                // 设置鸡的跟随状态
                nearestChicken.JoinFlock(transform);

                // 播放加入音效或动画（可选）
                // AudioSource.PlayClipAtPoint(joinSound, nearestChicken.transform.position);

                // 清除当前最近的鸡引用
                nearestChicken = null;
            }
        }
    }

    // 更新鸡计数UI
    void UpdateChickenCountUI()
    {
        if (chickenCountText != null)
        {
            chickenCountText.text = "鸡群: " + followingChickens.Count + " / " +
                (chickenManager != null ? chickenManager.requiredChickenCount : 20);
        }
    }

    // 可视化三角形跟随区域
    void OnDrawGizmos()
    {
        // 检查是否需要在编辑器中显示三角形区域
        if (!showFollowArea) return;

        // 如果顶点未初始化（游戏未运行），则在编辑器中计算预览版本的顶点
        if (followAreaVertices[0] == Vector3.zero || !Application.isPlaying)
        {
            Vector3 playerPos = transform.position;
            Vector3 forwardDir = transform.forward;
            Vector3 backDirection = -forwardDir;

            // 计算预览版三角形顶点
            Vector3[] previewVertices = new Vector3[3];
            previewVertices[0] = playerPos; // 直接使用玩家位置作为第一个顶点
            previewVertices[1] = playerPos + backDirection * followAreaLength + transform.right * followAreaWidth / 2f;
            previewVertices[2] = playerPos + backDirection * followAreaLength - transform.right * followAreaWidth / 2f;

            // 绘制预览三角形
            Gizmos.color = followAreaColor;
            Gizmos.DrawLine(previewVertices[0], previewVertices[1]);
            Gizmos.DrawLine(previewVertices[1], previewVertices[2]);
            Gizmos.DrawLine(previewVertices[2], previewVertices[0]);

            // 填充预览三角形
            Mesh previewMesh = new Mesh();
            previewMesh.vertices = previewVertices;
            previewMesh.triangles = new int[] { 0, 1, 2 };
            previewMesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up };
            previewMesh.RecalculateBounds();

            Gizmos.DrawMesh(previewMesh);
        }

        // 绘制交互半径
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        if (!Application.isPlaying) return;

        // 运行时的原始绘制逻辑
        Gizmos.color = followAreaColor;
        Gizmos.DrawLine(followAreaVertices[0], followAreaVertices[1]);
        Gizmos.DrawLine(followAreaVertices[1], followAreaVertices[2]);
        Gizmos.DrawLine(followAreaVertices[2], followAreaVertices[0]);

        // 填充三角形
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            followAreaVertices[0],
            followAreaVertices[1],
            followAreaVertices[2]
        };
        mesh.triangles = new int[] { 0, 1, 2 };

        // 添加法线
        mesh.normals = new Vector3[] {
            Vector3.up,
            Vector3.up,
            Vector3.up
        };

        // 重新计算边界
        mesh.RecalculateBounds();

        Gizmos.DrawMesh(mesh);
    }

    //停止奔跑
    public void StopRunning()
    {
        isRunning = false;
    }
    //开始奔跑
    public void StartRunning()
    {
        if (CanRun)
        {
            isRunning = true;
        }
    }
    //消耗体力
    private void DepleteStamina()
    {
        currentStamina -= depletionRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, settings.maxStamina);

        // 体力耗尽自动停止
        if (currentStamina <= 0)
        {
            currentStamina = 0;
            StopRunning();
        }
    }

    // 恢复体力（不奔跑时调用）
    private void RecoverStamina()
    {
        currentStamina += recoveryRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0f, settings.maxStamina);
    }

    //奔跑
    void HandleRun()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartRunning();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            StopRunning();
        }
    }

    void HandleRecover()
    {
        if (isRunning)
        {
            DepleteStamina();
        }
        else
        {
            RecoverStamina();
        }
    }

    void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 计算移动方向（相对于世界坐标系）
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // 根据状态决定移动速度
        float currentSpeed;
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isRunning)//奔跑
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        // 应用移动
        if (moveDirection.magnitude > 0.1f)
        {
            // 移动角色
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            // 设置角色朝向（平滑转向）
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 10f * Time.deltaTime);

            // 处理脚步声
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }

        // 应用重力
        characterController.Move(Physics.gravity * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;

            // 调整胶囊碰撞体高度
            float targetHeight = isCrouching ? crouchHeight : normalHeight;
            characterController.height = targetHeight;
        }
    }

    void PlayFootstepSound()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            // 随机选择一个脚步声音效
            AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];

            // 根据是否蹲伏调整音量
            float volume = isCrouching ? 0.3f : 0.7f;
            audioSource.PlayOneShot(footstep, volume);
        }
    }

    // 检测是否被敌人发现
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 如果不在隐藏状态，可以触发被发现的逻辑
            if (!isHiding)
            {
                Debug.Log("被敌人发现了！");
                // 在这里添加被发现后的处理逻辑
            }
        }
    }

    // 提供给其他脚本调用的公共方法
    public void SetHidingState(bool hiding)
    {
        isHiding = hiding;
    }
}