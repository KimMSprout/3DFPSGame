using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeaponMode : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] _weaponImage;
    [SerializeField] private PlayerWeaponMode _weaponMode;

    private void Awake()
    {
        _weaponMode.ModeChanged += ChangeModeSprite;
    }

    private void ChangeModeSprite(WeaponMode mode)
    {
        switch (mode)
        {
            case WeaponMode.Normal:
                _image.sprite = _weaponImage[0];
                break;
            case WeaponMode.Sniper:
                _image.sprite = _weaponImage[1];
                break;
            case WeaponMode.Melee:
                _image.sprite = _weaponImage[2];
                break;
        }
    }
}