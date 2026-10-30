using System;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private float _normalFov = 60f;
    [SerializeField] private float _zoomFov = 15f;

    private PlayerWeaponMode _weaponMode;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
    }

    private void Update()
    {
        bool isZooming = GameManager.Instance.State == GameState.Playing
                         && _weaponMode.Mode == WeaponMode.Sniper
                         && Input.GetMouseButton(1);

        Camera.main.fieldOfView = isZooming ? _zoomFov : _normalFov;
        _crosshair.SetActive(!isZooming);
        _sniperZoom.SetActive(isZooming);
        _sniperZoom.GetComponent<Image>().enabled = isZooming;
    }
}