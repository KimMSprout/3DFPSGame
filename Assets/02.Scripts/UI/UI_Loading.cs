using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 씬에서 다음 씬을 비동기로 불러오며 진행률을 슬라이더와 텍스트로 보여줘요.
public class UI_Loading : MonoBehaviour
{
    // AsyncOperation.progress는 allowSceneActivation이 false일 때 0.9에서 멈춰요.
    private const float LoadCompleteProgress = 0.9f;

    [SerializeField] private string _nextSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("연출")]
    [Tooltip("표시 진행률이 실제 진행률을 따라가는 속도 (초당 비율)")]
    [SerializeField] private float _fillSpeed = 0.8f;
    [Tooltip("100%를 보여준 뒤 씬을 전환하기까지 기다리는 시간")]
    [SerializeField] private float _completeDelay = 0.3f;

    private void Start()
    {
        StartCoroutine(LoadScene_Coroutine());
    }

    private IEnumerator LoadScene_Coroutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_nextSceneName);
        if (operation == null)
        {
            Debug.LogError($"씬을 불러올 수 없어요: {_nextSceneName} (Build Profiles에 등록되어 있는지 확인하세요)");
            yield break;
        }

        operation.allowSceneActivation = false;

        float displayProgress = 0f;
        Refresh(displayProgress);

        while (displayProgress < 1f)
        {
            // 실제 진행률(0~0.9)을 0~1로 환산해요.
            float targetProgress = Mathf.Clamp01(operation.progress / LoadCompleteProgress);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, _fillSpeed * Time.deltaTime);
            Refresh(displayProgress);
            yield return null;
        }

        yield return new WaitForSeconds(_completeDelay);
        operation.allowSceneActivation = true;
    }

    private void Refresh(float progress)
    {
        _progressSlider.value = progress;
        _progressText.text = $"불러오는 중 {Mathf.FloorToInt(progress * 100f)}%";
    }
}
