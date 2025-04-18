using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("基础设置")]
    public float moveSpeed = 5f;          // 移动速度
    public float crouchSpeed = 2.5f;      // 蹲伏时的移动速度
    public float normalHeight = 2f;       // 正常站立高度
    public float crouchHeight = 1f;       // 蹲伏时的高度

    [Header("音效设置")]
    public float footstepInterval = 0.5f; // 脚步声间隔
    public AudioClip[] footstepSounds;    // 脚步声音效
    public AudioSource audioSource;        // 音源组件

    [Header("状态")]
    public bool isCrouching = false;      // 是否正在蹲伏
    public bool isHiding = false;         // 是否在隐藏状态

    // 组件引用
    private CharacterController characterController;
    private float footstepTimer;
    private Vector3 moveDirection;

    void Start()
    {
        // 获取必要组件
        characterController = GetComponent<CharacterController>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
    }

    void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // 计算移动方向（相对于世界坐标系）
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // 根据状态决定移动速度
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;

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