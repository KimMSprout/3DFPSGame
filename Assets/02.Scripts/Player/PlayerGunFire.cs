using System;
using System.Collections;
using Lean.Pool;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private float _reloadTime = 3f;
    [SerializeField] private int _maxMagazine = 250;
    [SerializeField] private int _reloadMagazine = 50;
    [SerializeField] private int _currentMagazine;

    [SerializeField] private Text _maxMagazineText;
    [SerializeField] private Text _currentMagazineText;

    [Header("탄환")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Material _bulletMaterial;
    [SerializeField] private float _bulletLineWidth = 0.02f;
    [SerializeField] private float _bulletLineDuration = 0.2f;

    private Coroutine _bulletLineCoroutine;

    private LineRenderer _lineRenderer;

    private float _fireTimer = 0f;
    private bool _isReloading = false;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        _currentMagazine = _reloadMagazine;

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = _bulletLineWidth;
        _lineRenderer.endWidth = _bulletLineWidth;
        _lineRenderer.enabled = false;
        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.yellow;

        PlayerFirePool.RegisterHitEffect(_hitEffectPrefab);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _fireTimer -= Time.deltaTime;

        bool isMouseVisible = Cursor.visible;

        // 1. 마우스 왼쪽 버튼 입력 받기
        if (!isMouseVisible && Input.GetButton("Fire1") && _fireTimer <= 0f)
        {
            _animator.SetBool("Fire", true);
            Fire();
            _fireTimer = _fireInterval;
        }

        else
        {
            _animator.SetBool("Fire", false);
        }

        // 2. 키보드 R키 재장전
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (_isReloading == false)
            {
                StartCoroutine(Reload());
            }
        }
    }

    private void Fire()
    {
        if (_isReloading || _currentMagazine <= 0)
        {
            return;
        }

        _currentMagazine--;
        _currentMagazineText.text = _currentMagazine.ToString();

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
            // Lean Pool 에셋
            ParticleSystem hitEffect =
                LeanPool.Spawn(_hitEffectPrefab, hitInfo.point, Quaternion.identity);

            // AI 코드
            // ParticleSystem hitEffect = PlayerFirePool.GetHitEffect(
            //     _hitEffectPrefab,
            //     hitInfo.point,
            //     Quaternion.LookRotation(hitInfo.normal));

            if (hitEffect == null)
            {
                return;
            }

            hitEffect.transform.forward = hitInfo.normal; // 수직 위치로 회전
            hitEffect.Play(true);

            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            DrawBullet(hitInfo.point, _muzzle.transform.position);

            IDamageable damageable =
                hitInfo.collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damageInfo);
            }
        }
    }

    private IEnumerator Reload()
    {
        if (_currentMagazine == _reloadMagazine || _maxMagazine <= 0)
        {
            Debug.Log("장전 탄창이 이미 가득 차있습니다!");
            yield break;
        }

        Debug.Log("재장전 시작!");

        _isReloading = true;

        yield return new WaitForSeconds(_reloadTime);

        _isReloading = false;
        _maxMagazine -= (_reloadMagazine - _currentMagazine);
        _currentMagazine = _reloadMagazine;

        Debug.Log("재장전 종료!");

        _maxMagazineText.text = _maxMagazine.ToString();
        _currentMagazineText.text = _currentMagazine.ToString();
    }

    private void DrawBullet(Vector3 start, Vector3 end)
    {
        _lineRenderer.SetPosition(0, start);
        _lineRenderer.SetPosition(1, end);
        _lineRenderer.enabled = true;

        if (_bulletLineCoroutine != null)
        {
            StopCoroutine(_bulletLineCoroutine);
        }

        _bulletLineCoroutine = StartCoroutine(HideBullet_Coroutine());
    }

    private IEnumerator HideBullet_Coroutine()
    {
        yield return new WaitForSeconds(_bulletLineDuration);
        _lineRenderer.enabled = false;
        _bulletLineCoroutine = null;
    }
}