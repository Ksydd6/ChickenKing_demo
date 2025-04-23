using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CountDown : MonoBehaviour
{
    [SerializeField] 
    private TMP_Text timerText;
    [SerializeField]
    public float totalSeconds = 300; // 5���� = 300��
    private bool isRunning = false;

    //���ü�ʱʾ��
    private void LateUpdate()
    {
        HandleStartCountdown();
        
        //���µ���ʱ����
        UpdateTimerDisplay(totalSeconds);
    }

    public void HandleStartCountdown()
    {
        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(CountdownCoroutine());
        }
    }

    IEnumerator CountdownCoroutine()
    {
        while (totalSeconds > 0 && isRunning)
        {
            yield return new WaitForSeconds(1f);
            totalSeconds--;
            UpdateTimerDisplay(totalSeconds);
        }

        StopCountdown();
    }

    //���µ���ʱ
    void UpdateTimerDisplay(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
    }

    public void StopCountdown()
    {
        isRunning = false;
    }
}