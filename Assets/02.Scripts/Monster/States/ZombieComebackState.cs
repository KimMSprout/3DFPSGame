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
        // agent의 baseOffset 때문에 transform의 Y가 NavMesh보다 높으므로 XZ 평면 거리로 비교한다.
        Vector3 offset = _zombie.transform.position - _zombie.StartPosition;
        offset.y = 0f;

        if (offset.sqrMagnitude < 1f)
        {
            _zombie.ChangeState(ZombieState.Idle, "MoveToIdle");
        }
    }
}