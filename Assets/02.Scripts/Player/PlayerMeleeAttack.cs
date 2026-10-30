using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public enum MeleeHitHide
{
    Collider,
    Fan
}

public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private PlayerWeaponMode _weaponMode;
    [SerializeField] private Collider _katanaCollider;

    private Player _player;
    private Animator _animator;
    private int _meleeLayerIndex;

    private float _damage = 25f;
    private MeleeHitHide _meleeHitMode = MeleeHitHide.Fan;

    [Header("부채꼴")]
    [SerializeField] private float _range = 2f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private LayerMask _targetMask;

    private void Awake()
    {
        _weaponMode = GetComponentInParent<PlayerWeaponMode>();
        _player = GetComponentInParent<Player>();
        _animator = GetComponent<Animator>();
        _katanaCollider = GetComponentInChildren<BoxCollider>(true);

        _katanaCollider.enabled = false;
        _meleeLayerIndex = _animator.GetLayerIndex("Melee Layer");
    }

    public void FanAttack()
    {
        if (_meleeHitMode != MeleeHitHide.Fan) return;

        Vector3 origin = _player.transform.position;

        Collider[] colliders = Physics.OverlapSphere(_player.transform.position, _range, _targetMask);

        foreach (Collider col in colliders)
        {
            Vector3 hitPoint = col.ClosestPoint(_player.transform.position);

            Vector3 direction = (hitPoint - origin).normalized;
            direction.y = 0f;

            float angle = Vector3.Angle(_player.transform.forward, direction);

            if (angle > _angle) return;

            if (Physics.Linecast(origin, hitPoint, LayerMask.NameToLayer("Wall"))) continue;

            if (col.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.Value = 25f;
                damageInfo.HitPoint = hitPoint;
                damageInfo.HitNormal = (origin - hitPoint).normalized;
                damageable.TakeDamage(damageInfo);
            }
        }

        Debug.Log("부채꼴 공격!");
    }

    public void FanAttackEvent()
    {
    }

    public void ActivateCollider()
    {
        if (_meleeHitMode != MeleeHitHide.Collider) return;

        _katanaCollider.enabled = true;
    }

    public void DeactivateCollider()
    {
        _katanaCollider.enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (_weaponMode.Mode != WeaponMode.Melee) return;

        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("Slash");
            _animator.SetTrigger("Slash");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 플레이하지 않을 때도 불리므로, Awake에서 찾는 _player 대신 여기서 찾는다.
        Player player = GetComponentInParent<Player>();
        if (player == null) return;

        Vector3 origin = player.transform.position;
        Vector3 forward = player.transform.forward;
        Gizmos.color = Color.yellow;

        // 앞쪽을 y축으로 -Angle/2, +Angle/2만큼 돌린 두 방향이 부채꼴의 양 끝이다.
        Vector3 left = Quaternion.Euler(0f, -_angle / 2f, 0f) * forward;
        Vector3 right = Quaternion.Euler(0f, _angle / 2f, 0f) * forward;
        Gizmos.DrawLine(origin, origin + left * _range);
        Gizmos.DrawLine(origin, origin + right * _range);

        // 호: 10도마다 짧은 선을 이어 그린다.
        int count = Mathf.Max(1, Mathf.CeilToInt(_angle / 10f));
        Vector3 previous = origin + left * _range;
        for (int i = 1; i <= count; i++)
        {
            float angle = -_angle / 2f + _angle * i / count;
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * _range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}