using System;
using System.Collections;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    // 매 초마다 "1초!" -. "2초!" -> "3초!"를 센 다음에 "나로호 발사!" 라는 로그를 출력하고 싶다.

    // - IEnumerator: 코루틴 함수의 반환형
    // - StartCoroutine(함수()) : 유니티에서 코루틴 함수를 실행하는 방법
    // - yield: 정해진 시간/프레임 동안 쉼 

    private void Start()
    {
        Debug.Log("시작!");

        // Countdown 이라는 메서드를 비동기 방식으로 실행한다.
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("1초!");

        yield return new WaitForSeconds(1f);
        Debug.Log("2초!");
        yield return new WaitForSeconds(1f);
        Debug.Log("3초!");
        yield return new WaitForSeconds(1f);
        Debug.Log("나로호 발사");
    }
}