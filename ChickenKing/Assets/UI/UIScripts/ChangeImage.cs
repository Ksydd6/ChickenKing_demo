using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChangeImage : MonoBehaviour
{
    //鸡计数板图片
    [SerializeField]
    private Sprite[] chickenSprites;
    [SerializeField]
    private Image Image;
    [SerializeField]
    private TMP_Text chickenNumberText;
   //动画组件
    private Animator animator;

    //测试用数值
    private int chickenNumber;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if(animator == null)
        {
            Debug.Log("没有动画组件");
        }

        //测试用
        chickenNumber = 15;
    }

    //测试用计数
    private void Start()
    {
        StartCoroutine(Decrease());
    }
    IEnumerator Decrease()
    {
        while (true) 
        {
            chickenNumber--;
            yield return new WaitForSeconds(1f);
        }
    }

    //动画启动
    IEnumerator ChangingAnimation()
    {
        Debug.Log("开始动画");

        // 先设置为true触发动画
        animator.SetBool("isChanging", true);

        // 等待
        yield return new WaitForSeconds(0.2f);

        // 然后设置为false结束动画
        animator.SetBool("isChanging", false);

        Debug.Log("结束动画");
    }


    private void LateUpdate()
    {
        //修改显示图片
        if (chickenNumber == 10)
        {
            Image.sprite = chickenSprites[1];
            StartCoroutine(ChangingAnimation());
        }
        else if (chickenNumber == 5)
        {
            Image.sprite = chickenSprites[2];
            StartCoroutine(ChangingAnimation());
        }

        //修改数字
        chickenNumberText.text = chickenNumber.ToString();
    }

}
