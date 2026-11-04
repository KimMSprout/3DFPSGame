using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UI_Loading : MonoBehaviour
{
    // allowSceneActivation이 false면 progress는 0.9에서 멈춰요.
    private const float LoadCompleteProgress = 0.9f;

    [Header("UI")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("연출")]
    [Tooltip("표시 진행률이 실제 진행률을 따라가는 속도 (초당 비율, 1이면 0→100%에 1초)")]
    [SerializeField] private float _fillSpeed = 0.8f;

    private void Start()
    {
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // 비동기 씬 로드
        // 반환: 씬의 비동기 진행사항을 확인할 수 있는 객체
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName);

        // 씬 로드가 끝나도 바로 넘어가지 않고 활성화를 미뤄둬요.
        operation.allowSceneActivation = false;

        // 화면에 보여주는 진행률 (실제 진행률을 부드럽게 따라가요)
        float displayValue = 0f;
        Refresh(displayValue);

        while (!operation.isDone)
        {
            // 실제 진행률(0~0.9)을 0~1로 환산
            float targetValue = Mathf.Clamp01(operation.progress / LoadCompleteProgress);
            displayValue = Mathf.MoveTowards(displayValue, targetValue, _fillSpeed * Time.deltaTime);
            Refresh(displayValue);

            // 로드가 끝났고 표시도 100%까지 다 찼을 때 씬 전환
            if (operation.progress >= LoadCompleteProgress && displayValue >= 1f)
            {
                Application.backgroundLoadingPriority = ThreadPriority.Normal;
                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }

    private void Refresh(float value)
    {
        _progressSlider.value = value;
        _progressText.text = $"불러오는 중 {Mathf.FloorToInt(value * 100f)}%";
    }
}
