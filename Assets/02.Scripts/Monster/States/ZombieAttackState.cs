using UnityEngine;

// 공격 : 일정 시간마다 플레이어를 때린다.
public class ZombieAttackState : ZombieStateBase
{
    private float _attackTimer;
    private bool _isAttacking = false;

    public ZombieAttackState(Zombie zombie) : base(zombie)
    {
    }

    public override void Enter()
    {
        _zombie.StopMove();
    }

    public override void Exit()
    {
        // 공격 도중 플레이어가 벗어나면 타격 이벤트가 오지 않으므로 직접 초기화한다.
        _attackTimer = 0f;
        _isAttacking = false;

        // Move에서만 소비되는 트리거가 남아 있으면 Move로 나가자마자 AttackDelay로 되돌아간다.
        _zombie.animator.ResetTrigger("MoveToAttackDelay");
    }

    public override void Update()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        if (_zombie.DistanceToPlayer() > _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Trace, "AttackMove");
            return;
        }

        if (_isAttacking == false)
        {
            _attackTimer += Time.deltaTime;
        }

        if (_attackTimer < _zombie.AttackInterval) return;

        _isAttacking = true;
        // 데미지는 애니메이션의 타격 이벤트가 AttackAction()을 호출할 때 들어간다.
        _zombie.animator.SetTrigger("StartAttack");

        _attackTimer = 0f;
    }

    // 공격 애니메이션의 타격 시점에 호출된다. 그 순간에도 플레이어가 사정 거리 안에 있어야 맞는다.
    public void AttackAction()
    {
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

        _isAttacking = false;
    }
}