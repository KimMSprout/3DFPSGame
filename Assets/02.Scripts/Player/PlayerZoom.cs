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
    private Camera _camera;
    private Image _sniperZoomImage;
    private bool _isZooming;

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _camera = Camera.main;
        _sniperZoomImage = _sniperZoom.GetComponent<Image>();
    }

    private void Start()
    {
        ApplyZoom(false);
    }

    private void Update()
    {
        bool isZooming = GameManager.Instance.State == GameState.Playing
                         && _weaponMode.Mode == WeaponMode.Sniper
                         && Input.GetMouseButton(1);

        // 매 프레임 같은 값을 다시 넣지 않도록 줌 상태가 바뀐 프레임에만 반영한다.
        if (isZooming == _isZooming) return;

        ApplyZoom(isZooming);
    }

    private void ApplyZoom(bool isZooming)
    {
        _isZooming = isZooming;
        _camera.fieldOfView = isZooming ? _zoomFov : _normalFov;
        _crosshair.SetActive(!isZooming);
        _sniperZoom.SetActive(isZooming);
        _sniperZoomImage.enabled = isZooming;
    }
}
