using UnityEngine;

// 마우스 오른쪽 버튼을 누르면 시선이 바라보는 방향으로 수류탄을 던진다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 0f;
    [SerializeField] private int _maxBombCount = 10;
    [SerializeField] private float _chargeSpeed = 5f;


    private int _bombCount;

    private void Awake()
    {
        _bombCount = _maxBombCount;
        PlayerFirePool.RegisterBomb(_bombPrefab);
    }

    private void Update()
    {
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButton("Fire2"))
        {
            _throwPower += _chargeSpeed * Time.deltaTime;
        }

        if (Input.GetButtonUp("Fire2"))
        {
            // 2. 발사 위치에서 수류탄 생성하기
            Shoot();
        }
    }

    private void Shoot()
    {
        if (_bombCount <= 0)
        {
            return;
        }

        Bomb bomb = PlayerFirePool.GetBomb(_bombPrefab, _firePoint.position, Quaternion.identity);

        if (bomb == null)
        {
            return;
        }

        Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
        rigidbody.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);

        _bombCount--;
        _throwPower = 0f;

        Debug.Log($"최대 폭탄 {_maxBombCount} / 현재 폭탄 {_bombCount}");
    }
}