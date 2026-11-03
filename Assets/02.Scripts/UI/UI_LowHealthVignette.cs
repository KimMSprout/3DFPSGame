using UnityEngine;
using UnityEngine.UI;

// 체력이 낮으면 화면 가장자리에 붉은 반투명 비네팅을 띄운다.
// 체력이 낮을수록 진해지고, 심장 박동처럼 더 빠르게 맥박친다.
public class UI_LowHealthVignette : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Player _player;

    [Header("표시 조건")]
    [SerializeField, Range(0f, 1f)] private float _startHealthRatio = 0.4f; // 이 비율 이하부터 표시

    [Header("세기")]
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.15f; // 막 표시되기 시작할 때
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.55f; // 체력이 0에 가까울 때

    [Header("맥박")]
    [SerializeField] private float _minPulseSpeed = 4f;  // 라디안/초
    [SerializeField] private float _maxPulseSpeed = 9f;
    [SerializeField, Range(0f, 1f)] private float _pulseAmount = 0.35f; // 알파가 흔들리는 비율

    private float _pulsePhase;

    private void Awake()
    {
        _image.enabled = false;
    }

    private void LateUpdate()
    {
        float healthRatio = _player.Health / _player.MaxHealth;

        if (healthRatio > _startHealthRatio)
        {
            if (_image.enabled)
            {
                _image.enabled = false;
            }

            return;
        }

        // 0: 막 표시되기 시작, 1: 체력 0
        float severity = 1f - Mathf.Clamp01(healthRatio / _startHealthRatio);

        // 속도가 바뀌어도 맥박이 튀지 않도록 위상을 누적한다.
        _pulsePhase += Mathf.Lerp(_minPulseSpeed, _maxPulseSpeed, severity) * Time.deltaTime;
        float pulse = 1f - _pulseAmount * (0.5f + 0.5f * Mathf.Sin(_pulsePhase));

        Color color = _image.color;
        color.a = Mathf.Lerp(_minAlpha, _maxAlpha, severity) * pulse;
        _image.color = color;

        if (!_image.enabled)
        {
            _image.enabled = true;
        }
    }
}
