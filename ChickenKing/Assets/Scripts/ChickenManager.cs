using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChickenManager : MonoBehaviour
{
    [Header("鸡群设置")]
    public GameObject chickenPrefab;         // 鸡的预制体
    public int initialChickenCount = 50;     // 初始鸡的数量
    public float spawnRadius = 20f;          // 生成半径
    public Transform chickenParent;          // 鸡的父物体（用于层级整理）

    [Header("UI设置")]
    public Text chickenCountText;            // 显示已收集鸡数量的UI文本
    public GameObject recruitmentCompletedUI; // 招募完成的UI提示

    [Header("目标设置")]
    public int requiredChickenCount = 20;    // 需要收集的鸡数量
    public Transform player;                 // 玩家引用

    // 内部变量
    private List<Chicken> allChickens = new List<Chicken>();
    private int recruitedChickenCount = 0;
    private bool isRecruitmentCompleted = false;
    private Player playerController;

    void Start()
    {
        // 如果没有指定父物体，使用自身
        if (chickenParent == null)
            chickenParent = transform;

        // 寻找玩家
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = player.GetComponent<Player>();
            }
        }
        else
        {
            playerController = player.GetComponent<Player>();
        }

        // 初始化UI
        if (recruitmentCompletedUI != null)
            recruitmentCompletedUI.SetActive(false);

        UpdateChickenCountUI();

        // 生成初始鸡群
        SpawnInitialChickens();

        // 开始检测鸡群加入队伍
        StartCoroutine(CheckChickenRecruitment());
    }

    void Update()
    {
        // 检查是否已达到目标数量
        if (!isRecruitmentCompleted && recruitedChickenCount >= requiredChickenCount)
        {
            isRecruitmentCompleted = true;
            OnRecruitmentCompleted();
        }
    }

    // 生成初始鸡群
    void SpawnInitialChickens()
    {
        if (chickenPrefab == null)
        {
            Debug.LogError("鸡预制体未设置！");
            return;
        }

        for (int i = 0; i < initialChickenCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = new Vector3(randomCircle.x, 0, randomCircle.y);

            // 确保生成位置在NavMesh上
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                GameObject chickenObj = Instantiate(chickenPrefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0), chickenParent);
                Chicken chicken = chickenObj.GetComponent<Chicken>();

                if (chicken != null)
                {
                    chicken.target = player;
                    allChickens.Add(chicken);
                }
            }
        }
    }

    // 检查鸡群加入队伍
    IEnumerator CheckChickenRecruitment()
    {
        while (true)
        {
            if (playerController != null)
            {
                // 更新已招募的鸡数量
                recruitedChickenCount = playerController.followingChickens.Count;
                UpdateChickenCountUI();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    // 更新UI显示
    void UpdateChickenCountUI()
    {
        if (chickenCountText != null)
        {
            chickenCountText.text = string.Format("已收集: {0}/{1}", recruitedChickenCount, requiredChickenCount);
        }
    }

    // 招募完成后的处理
    void OnRecruitmentCompleted()
    {
        Debug.Log("招募完成！已收集 " + recruitedChickenCount + " 只鸡");

        // 显示招募完成UI
        if (recruitmentCompletedUI != null)
            recruitmentCompletedUI.SetActive(true);

        // 在这里可以触发下一个阶段的逻辑，例如打开工厂大门等
        // ExampleGameManager.Instance.OnChickenRecruitmentCompleted();
    }

    // 获取已招募的鸡数量
    public int GetRecruitedChickenCount()
    {
        return recruitedChickenCount;
    }

    // 获取需要招募的鸡数量
    public int GetRequiredChickenCount()
    {
        return requiredChickenCount;
    }

    // 获取招募完成状态
    public bool IsRecruitmentCompleted()
    {
        return isRecruitmentCompleted;
    }
}