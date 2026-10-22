using System.Collections;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchPower = 10f;
    [SerializeField] private float _spinPower = 10f;
    [SerializeField] private float _destroyDelay = 3f;

    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float damage)
    {
        if (_health <= 0)
        {
            return;
        }

        _health -= damage;

        if (_health <= 0)
        {
            StartCoroutine(Explode());
        }
    }

    private IEnumerator Explode()
    {
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);

        effect.Play(true);

        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(Random.insideUnitSphere * _spinPower, ForceMode.Impulse);


        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}