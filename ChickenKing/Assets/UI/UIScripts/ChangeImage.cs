using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChangeImage : MonoBehaviour
{
    //��������ͼƬ
    [SerializeField]
    private Sprite[] chickenSprites;
    [SerializeField]
    private Image Image;
    [SerializeField]
    private TMP_Text chickenNumberText;
   //�������
    private Animator animator;

    //��������ֵ
    private int chickenNumber;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if(animator == null)
        {
            Debug.Log("û�ж������");
        }

        //������
        chickenNumber = 15;
    }

    //�����ü���
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

    //��������
    IEnumerator ChangingAnimation()
    {
        Debug.Log("��ʼ����");

        // ������Ϊtrue��������
        animator.SetBool("isChanging", true);

        // �ȴ�
        yield return new WaitForSeconds(0.2f);

        // Ȼ������Ϊfalse��������
        animator.SetBool("isChanging", false);

        Debug.Log("��������");
    }


    private void LateUpdate()
    {
        //�޸���ʾͼƬ
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

        //�޸�����
        chickenNumberText.text = chickenNumber.ToString();
    }

}
