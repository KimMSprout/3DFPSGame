using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _steminaSlider;
    [SerializeField] private Player player;
    [SerializeField] private PlayerMove _playerMove;

    private void Awake()
    {
        _playerMove = GetComponent<PlayerMove>();
        player = GetComponent<Player>();
    }

    private void LateUpdate()
    {
        _healthSlider.value = player.Health / player.MaxHealth;
        _steminaSlider.value = _playerMove.Stemina / _playerMove.MaxStemina;
    }
}