using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 풀로 반환되는 폭탄
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private float _damage = 100f;
    [SerializeField] private float _explosionRange = 4.5f;

    private PlayerFirePool _pool;
    private Rigidbody _rigidbody;
    private bool _hasCollided;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    internal void SetPool(PlayerFirePool pool)
    {
        _pool = pool;
    }

    internal void PrepareForReuse()
    {
        _hasCollided = false;

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.WakeUp();
    }

    internal void ResetForPool()
    {
        _hasCollided = true;

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.Sleep();
    }

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasCollided)
        {
            return;
        }

        _hasCollided = true;

        ApplyAreaDamage();

        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        if (_pool != null)
        {
            _pool.ReleaseBomb(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다.
        // Physics.OverlapSphere(원점, 반지름, 찾을 레이어들)
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_damage);
            }
        }

        // 1. 드럼통들에게 데미지를 준다.
    }
}