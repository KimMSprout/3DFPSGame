using UnityEngine;

// 추격 : NavMeshAgent로 길을 찾아 플레이어를 쫓아간다.
public class ZombieTraceState : ZombieStateBase
{
    public ZombieTraceState(Zombie zombie) : base(zombie) { }

    public override void Update()
    {
        // TODO : Run 애니메이션 실행

        if (Vector3.Distance(_zombie.transform.position, _zombie.StartPosition) > _zombie.ComebackDistance)
        {
            _zombie.ChangeState(ZombieState.Comback);
            return;
        }

        if (_zombie.DistanceToPlayer() < _zombie.AttackRange)
        {
            _zombie.ChangeState(ZombieState.Attack);
            return;
        }

        _zombie.MoveTo(_zombie.Player.transform.position, _zombie.AttackRange);
    }
}
