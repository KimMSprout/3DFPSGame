using UnityEngine;

// 추격 : NavMeshAgent로 길을 찾아 플레이어를 쫓아간다. NavMesh Link를 만나면 포물선으로 점프해서 건넌다.
public class ZombieTraceState : ZombieStateBase
{
    public ZombieTraceState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        // 링크를 agent가 알아서 건너지 못하게 하고, 점프로 직접 건넌다.
        _zombie.SetAutoTraverseLink(false);
    }

    public override void Exit()
    {
        // 점프 중이면 점프가 끝날 때 Zombie가 되돌린다.
        if (_zombie.IsJumping == false)
        {
            _zombie.SetAutoTraverseLink(true);
        }
    }

    public override void Update()
    {
        // TODO : Run 애니메이션 실행

        if (_zombie.IsJumping) return;

        _zombie.TryJumpAcrossLink();
        if (_zombie.IsJumping) return;

        if (Vector3.Distance(_zombie.transform.position, _zombie.StartPosition) > _zombie.ComebackDistance)
        {
            _zombie.ChangeState(ZombieState.Comback);
            return;
        }

        if (_zombie.DistanceToPlayer() < _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Attack, "MoveToAttack");
            return;
        }

        _zombie.MoveTo(_zombie.Player.transform.position, _zombie.AttackRange);
    }
}
