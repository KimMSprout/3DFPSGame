using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 풀로 반환되는 폭탄
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

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
}
