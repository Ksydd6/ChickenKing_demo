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
    private bool[] havePlayed;

    //测试用数值
    private int chickenNumber;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if(animator == null)
        {
            Debug.Log("没有动画组件");
        }
        havePlayed = new bool[2] { false,false };

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


    private void LateUpdate()
    {
        //修改显示图片
        if (chickenNumber == 10 & !havePlayed[0])
        {
            Image.sprite = chickenSprites[1];
            animator.Play("chickenShake");
            havePlayed[0] = true;
        }
        else if (chickenNumber == 5 & !havePlayed[1])
        {
            Image.sprite = chickenSprites[2];
            animator.Play("chickenShake");
            havePlayed[1] = true;
        }

        //修改数字
        chickenNumberText.text = chickenNumber.ToString();
    }

}
