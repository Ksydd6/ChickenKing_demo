using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChangeImage : MonoBehaviour
{
    [SerializeField]
    //��������ͼƬ
    public Sprite[] chickenSprites;
    public Image Image;
    public TMP_Text chickenNumberText;
    
    //��������ֵ
    private int chickenNumber;
    private bool isChanging = false;
    private Animator animator;

    //�����ú���
    private void Awake()
    {
        animator = GetComponent<Animator>();
        chickenNumber = 15;

        if(animator == null)
        {
            Debug.Log("û�ж������");
        }
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
