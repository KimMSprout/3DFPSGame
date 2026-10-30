using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;
    [SerializeField] private float _fadeTime = 0.3f;

    [SerializeField] private Image _image;

    private void Awake()
    {
        _image.enabled = false;
    }

    private void Start()
    {
        Player player = GameObject.FindAnyObjectByType<Player>();

        if (player != null)
        {
            player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(0.6f);
    }

    public void Flash()
    {
        StartCoroutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        SetAlpha(1f);

        _image.gameObject.SetActive(true);
        _image.enabled = true;

        yield return new WaitForSeconds(_flashTime);

        _image.enabled = false;
        _image.gameObject.SetActive(false);
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        StartCoroutine(Fade_Coroutine());
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

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}