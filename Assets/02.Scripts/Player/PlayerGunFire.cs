using UnityEngine;

public enum MyKey
{
    Fire1,
}

// 마우스 좌클릭을 하면 바라보는 방향으로 총알을 발사
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;
    private float _fireTimer = 0f;

    private void Update()
    {
        _fireTimer -= Time.deltaTime;

        // 1. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f)
        {
            Fire();
            _fireTimer = _fireInterval;
        }
    }

    private void Fire()
    {
        // Ray : 레이저 (시작 위치, 날아갈 방향)
        // Phsics.RayCast : 레이저를 발사
        // RaycastHit : 레이저가 물체와 부딪히면 그 정보를 반환

        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방법 설정하기
        Transform cameraTransform = Camera.main.transform;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성하기www
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);

        if (isHit)
        {
            ParticleSystem hitEffect =
                Instantiate(_hitEffectPrefab);
            hitEffect.transform.position = hitInfo.point;
            hitEffect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            hitEffect.Play(true);
        }
    }
}