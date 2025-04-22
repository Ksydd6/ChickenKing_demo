using UnityEngine;

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

    [Header("状态")]
    public bool isCrouching = false;      // 是否正在蹲伏
    public bool isHiding = false;         // 是否在隐藏状态
    public bool isRunning = false;        //是否奔跑

    // 组件引用
    private CharacterController characterController;
    private float footstepTimer;
    private Vector3 moveDirection;

    [Header("体力设置")]
    public StaminaSettings settings;

    private float currentStamina;   //实际体力
    private float recoveryRate;
    private float depletionRate;

    //公开属性
    public float CurrentStamina => currentStamina;
    public float StaminaPercent => currentStamina / settings.maxStamina;
    public bool IsRunning => isRunning;
    public bool CanRun => currentStamina > 0;

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