using UnityEngine;

// 복귀 : 처음 자리로 돌아간다.
public class ZombieComebackState : ZombieStateBase
{
    public ZombieComebackState(Zombie zombie) : base(zombie)
    {
    }

    public override void Enter()
    {
        _zombie.MoveTo(_zombie.StartPosition);
    }

    public override void Update()
    {
        if (Vector3.Distance(_zombie.transform.position, _zombie.StartPosition) < 1f)
        {
            _zombie.ChangeState(ZombieState.Idle, "MoveToIdle");
        }
    }
}