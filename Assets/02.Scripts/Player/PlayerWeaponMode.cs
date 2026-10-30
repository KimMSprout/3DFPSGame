using System;
using UnityEngine;
using TMPro;

public class PlayerWeaponMode : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _modeText;

    private WeaponMode _mode = WeaponMode.Normal;
    public WeaponMode Mode => _mode;

    public event Action<WeaponMode> ModeChanged;

    private void Start()
    {
        ShowMode();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeMode(WeaponMode.Normal);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeMode(WeaponMode.Sniper);
        }

        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeMode(WeaponMode.Melee);
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        _mode = mode;
        ShowMode();
        ModeChanged?.Invoke(mode);
        Debug.Log($"무기 모드 : {mode}");
    }

    private void ShowMode()
    {
        switch (_mode)
        {
            case WeaponMode.Normal:
                _modeText.text = "일반 모드";
                break;
            case WeaponMode.Sniper:
                _modeText.text = "저격 모드";
                break;
        }
    }
}