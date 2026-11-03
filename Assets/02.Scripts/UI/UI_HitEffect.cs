using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어가 피격되면 화면 가장자리에 붉은 비네팅을 잠깐 띄웠다가 서서히 지운다.
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;
    [SerializeField] private float _fadeTime = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _hitAlpha = 0.6f;

    [SerializeField] private Image _image;

    private Player _player;
    private Coroutine _effectCoroutine;

    private void Awake()
    {
        _image.enabled = false;
    }

    private void Start()
    {
        _player = GameObject.FindAnyObjectByType<Player>();

        if (_player != null)
        {
            _player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamaged -= OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(_hitAlpha);
    }

    public void Flash()
    {
        Restart(Flash_Coroutine());
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        Restart(Fade_Coroutine());
    }

    // 연속으로 맞아도 이전 효과와 겹치지 않도록 진행 중인 코루틴을 멈추고 다시 시작한다.
    private void Restart(IEnumerator routine)
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
        }

        _image.enabled = true;
        _effectCoroutine = StartCoroutine(routine);
    }

    private IEnumerator Flash_Coroutine()
    {
        SetAlpha(1f);

        yield return new WaitForSeconds(_flashTime);

        Hide();
    }

    private IEnumerator Fade_Coroutine()
    {
        float elapsed = 0f;

        float startAlpha = _image.color.a;

        while (elapsed < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / _fadeTime);

            SetAlpha(alpha);

            elapsed += Time.deltaTime;

            yield return null;
        }

        Hide();
    }

    private void Hide()
    {
        SetAlpha(0f);
        _image.enabled = false;
        _effectCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
