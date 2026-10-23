using System;
using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Zombie _zombie;

    private void Awake()
    {
        _slider = GetComponentInChildren<Slider>();
        _zombie = GetComponent<Zombie>();
        _slider.value = 1f;
    }

    private void LateUpdate()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;

        // 빌보드 : 체력바가 카메라를 쳐다보게 한다.
        transform.forward = Camera.main.transform.forward;
    }
}