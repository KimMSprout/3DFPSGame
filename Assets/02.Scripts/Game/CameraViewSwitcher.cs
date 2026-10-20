using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public sealed class CameraViewSwitcher : MonoBehaviour
{
    public enum CameraView
    {
        FirstPerson,
        ThirdPerson,
        Shoulder,
        Quarter,
        TopDown,
        Side
    }

    [Serializable]
    private sealed class ViewSettings
    {
        [Tooltip("CameraPosition 기준 위치입니다. X: 오른쪽, Y: 위, Z: 앞입니다.")]
        [SerializeField]
        private Vector3 _positionOffset;

        [Tooltip("바라보는 방향입니다. X: 상하, Y: 좌우, Z: 기울기입니다. 단위는 도입니다.")]
        [SerializeField]
        private Vector3 _rotationOffset;

        [Tooltip("켜면 위치와 방향이 Player의 Y축 회전을 따라갑니다. 끄면 월드 기준입니다.")]
        [SerializeField]
        private bool _followPlayerYaw;

        [Tooltip("켜면 마우스 Y 입력으로 상하 회전합니다. 위치도 눈 위치를 중심으로 회전합니다.")]
        [SerializeField]
        private bool _useMousePitch;

        public Vector3 PositionOffset => _positionOffset;
        public Vector3 RotationOffset => _rotationOffset;
        public bool FollowPlayerYaw => _followPlayerYaw;
        public bool UseMousePitch => _useMousePitch;

        public ViewSettings(Vector3 positionOffset, Vector3 rotationOffset,
            bool followPlayerYaw, bool useMousePitch)
        {
            _positionOffset = positionOffset;
            _rotationOffset = rotationOffset;
            _followPlayerYaw = followPlayerYaw;
            _useMousePitch = useMousePitch;
        }
    }

    [Header("References")]
    [Tooltip("PlayerRotate가 붙어 있는 Player를 연결합니다.")]
    [SerializeField]
    private Transform _player;

    [Tooltip("Player의 자식인 눈 위치 CameraPosition을 연결합니다.")]
    [SerializeField]
    private Transform _cameraPosition;

    [Tooltip("기존 CameraRotate 컴포넌트를 연결합니다. 중복 회전을 막기 위해 자동 비활성화합니다.")]
    [SerializeField]
    private MonoBehaviour _cameraRotate;

    [Tooltip("기존 CameraFollow 컴포넌트를 연결합니다. 중복 이동을 막기 위해 자동 비활성화합니다.")]
    [SerializeField]
    private MonoBehaviour _cameraFollow;

    [Header("Input - Legacy Input Manager")]
    [SerializeField] private KeyCode _switchKey = KeyCode.V;
    [SerializeField, Min(0f)] private float _mouseSensitivity = 2f;
    [SerializeField] private bool _invertMouseY;
    [SerializeField] private bool _lockCursor = true;

    [Header("Current View")]
    [SerializeField] private CameraView _currentView = CameraView.FirstPerson;

    // 1인칭: 카메라를 눈 위치에 두고, 몸의 좌우 회전과 마우스 상하 회전을 반영합니다.
    [Header("01 - First Person / 1인칭")]
    [SerializeField]
    private ViewSettings _firstPerson = new ViewSettings(
        Vector3.zero, Vector3.zero, true, true);

    // 3인칭: 카메라를 눈보다 조금 위, 뒤쪽으로 이동하고 아래를 약간 바라봅니다.
    [Header("02 - Third Person / 3인칭")]
    [SerializeField]
    private ViewSettings _thirdPerson = new ViewSettings(
        new Vector3(0f, 0.7f, -4f), new Vector3(10f, 0f, 0f), true, true);

    // 숄더뷰: 카메라를 오른쪽 어깨 뒤로 이동하고 플레이어 전방을 바라봅니다.
    [Header("03 - Shoulder / 숄더뷰")]
    [SerializeField]
    private ViewSettings _shoulder = new ViewSettings(
        new Vector3(0.65f, 0.25f, -2.5f), new Vector3(5f, 0f, 0f), true, true);

    // 쿼터뷰: 카메라를 월드 기준 대각선 위쪽에 두고 45도 아래를 바라봅니다.
    [Header("04 - Quarter / 쿼터뷰")]
    [SerializeField]
    private ViewSettings _quarter = new ViewSettings(
        new Vector3(-5f, 7.071068f, -5f), new Vector3(45f, 45f, 0f), false, false);

    // 탑다운: 카메라를 눈 위치의 수직 위쪽에 두고 X축 90도로 바닥을 바라봅니다.
    [Header("05 - Top Down / 탑다운")]
    [SerializeField]
    private ViewSettings _topDown = new ViewSettings(
        new Vector3(0f, 8f, 0f), new Vector3(90f, 0f, 0f), false, false);

    // 사이드뷰: 카메라를 월드 +X 방향에 두고 -X 방향으로 플레이어 옆면을 바라봅니다.
    [Header("06 - Side / 사이드뷰")]
    [SerializeField]
    private ViewSettings _side = new ViewSettings(
        new Vector3(8f, 0f, 0f), new Vector3(0f, -90f, 0f), false, false);

    private float _mousePitch;
    private bool _cameraRotateWasEnabled;
    private bool _cameraFollowWasEnabled;
    private bool _ownsCamera;
    private bool _ownsCursor;
    private CursorLockMode _previousCursorLockState;
    private bool _previousCursorVisible;

    public CameraView CurrentView => _currentView;

    private void OnEnable()
    {
        if (_player == null || _cameraPosition == null ||
            _cameraRotate == this || _cameraFollow == this)
        {
            Debug.LogError("Player와 CameraPosition을 연결하고, 기존 카메라 스크립트를 올바르게 지정하세요.", this);
            enabled = false;
            return;
        }

        // 기존 두 스크립트의 상태를 보관하고, 이 컴포넌트만 카메라를 제어하게 합니다.
        _cameraRotateWasEnabled = _cameraRotate != null && _cameraRotate.enabled;
        _cameraFollowWasEnabled = _cameraFollow != null && _cameraFollow.enabled;

        if (_cameraRotate != null)
        {
            _cameraRotate.enabled = false;
        }

        if (_cameraFollow != null)
        {
            _cameraFollow.enabled = false;
        }

        _ownsCamera = true;

        if (_lockCursor)
        {
            _previousCursorLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _ownsCursor = true;
        }
    }

    private void Update()
    {
        // 키를 누른 프레임에 한 번만 다음 시점으로 전환합니다.
        if (Input.GetKeyDown(_switchKey))
        {
            SwitchToNextView();
        }

        ViewSettings settings = GetCurrentSettings();

        if (!settings.UseMousePitch)
        {
            return;
        }

        // 좌우 회전은 기존 PlayerRotate가 담당하므로 Mouse X를 다시 적용하지 않습니다.
        // Mouse Y는 마우스 이동량이므로 Time.deltaTime을 곱하지 않습니다.
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _mousePitch += _invertMouseY ? mouseY : -mouseY;
        ClampMousePitch(settings);
    }

    private void LateUpdate()
    {
        if (_player == null || _cameraPosition == null)
        {
            return;
        }

        ViewSettings settings = GetCurrentSettings();
        ClampMousePitch(settings);

        // 추적 모드는 Player의 좌우 회전을 기준으로, 고정 모드는 월드 기준으로 계산합니다.
        Quaternion yawRotation = settings.FollowPlayerYaw
            ? Quaternion.Euler(0f, _player.eulerAngles.y, 0f)
            : Quaternion.identity;

        float pitch = settings.UseMousePitch ? _mousePitch : 0f;
        Quaternion mouseRotation = Quaternion.Euler(pitch, 0f, 0f);
        Quaternion viewFrame = yawRotation * mouseRotation;

        // 모든 시점은 눈 위치를 추적합니다. 오프셋에는 Player의 스케일이 적용되지 않습니다.
        Vector3 position = _cameraPosition.position + viewFrame * settings.PositionOffset;
        Quaternion rotation = viewFrame * Quaternion.Euler(settings.RotationOffset);
        transform.SetPositionAndRotation(position, rotation);
    }

    public void SwitchToNextView()
    {
        // 1인칭 → 3인칭 → 숄더뷰 → 쿼터뷰 → 탑다운 → 사이드뷰 → 1인칭
        _currentView = _currentView == CameraView.Side
            ? CameraView.FirstPerson
            : (CameraView)((int)_currentView + 1);
    }

    private void ClampMousePitch(ViewSettings settings)
    {
        if (settings.UseMousePitch)
        {
            // 기본 회전 오프셋까지 합친 상하 각도를 -90~90도로 제한합니다.
            float basePitch = Mathf.DeltaAngle(0f, settings.RotationOffset.x);
            _mousePitch = Mathf.Clamp(_mousePitch, -90f - basePitch, 90f - basePitch);
        }
    }

    private ViewSettings GetCurrentSettings()
    {
        switch (_currentView)
        {
            case CameraView.ThirdPerson:
                return _thirdPerson;
            case CameraView.Shoulder:
                return _shoulder;
            case CameraView.Quarter:
                return _quarter;
            case CameraView.TopDown:
                return _topDown;
            case CameraView.Side:
                return _side;
            default:
                return _firstPerson;
        }
    }

    private void OnDisable()
    {
        if (_ownsCamera)
        {
            if (_cameraRotate != null)
            {
                _cameraRotate.enabled = _cameraRotateWasEnabled;
            }

            if (_cameraFollow != null)
            {
                _cameraFollow.enabled = _cameraFollowWasEnabled;
            }

            _ownsCamera = false;
        }

        if (_ownsCursor)
        {
            Cursor.lockState = _previousCursorLockState;
            Cursor.visible = _previousCursorVisible;
            _ownsCursor = false;
        }
    }
}