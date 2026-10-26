using UnityEngine;

// 공격 : 일정 시간마다 플레이어를 때린다.
public class ZombieAttackState : ZombieStateBase
{
    private float _attackTimer;

    public ZombieAttackState(Zombie zombie) : base(zombie) { }

    public override void Update()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        if (_zombie.DistanceToPlayer() > _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer < _zombie.AttackInterval) return;

        // TODO : 공격 애니메이션 실행
        Vector3 toPlayer = _zombie.Player.transform.position - _zombie.transform.position;
        Ray ray = new Ray(_zombie.transform.position, toPlayer);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, _zombie.AttackRange)
            && hitInfo.transform.TryGetComponent(out Player player))
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.Value = _zombie.AttackDamage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            player.TakeDamage(damageInfo);
        }

        _attackTimer = 0f;
    }
}
