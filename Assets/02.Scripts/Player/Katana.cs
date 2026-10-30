using System;
using UnityEngine;

public class Katana : MonoBehaviour
{
    private Player _player;
    private float _damage = 25f;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Player")) return;

        if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _damage;

            Vector3 hitPoint = other.ClosestPoint(_player.transform.position);
            damageInfo.HitPoint = hitPoint;
            damageInfo.HitNormal = (_player.transform.position - hitPoint).normalized;

            damageable.TakeDamage(damageInfo);
        }
    }
}