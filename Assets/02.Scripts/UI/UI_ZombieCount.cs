using System;
using TMPro;
using UnityEngine;

public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _maxZombieCountText;
    [SerializeField] private TextMeshProUGUI _currentZombieCountText;

    private void Start()
    {
        ZombieManager.Instance.ZombieCountChanged += ChangeCountText;
        _maxZombieCountText.text = $"최대 Zombie : {ZombieManager.Instance.maxCount}";
        ChangeCountText();
    }

    private void OnDestroy()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.ZombieCountChanged -= ChangeCountText;
        }
    }

    private void ChangeCountText()
    {
        _currentZombieCountText.text = $"필드 위 Zombie : {ZombieManager.Instance.Count:N0} 마리";
    }
}