using UnityEngine;

// 추격 : 플레이어를 쫓아간다.
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

        Vector3 direction = _zombie.Player.transform.position - _zombie.transform.position;
        direction.y = 0f;
        direction.Normalize();

        _zombie.transform.forward = direction;
        _zombie.Controller.Move(direction * _zombie.MoveSpeed * Time.deltaTime);
    }
}
