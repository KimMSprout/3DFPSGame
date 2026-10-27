using UnityEngine;

// 대기 : 제자리에서 플레이어를 기다린다.
public class ZombieIdleState : ZombieStateBase
{
    private float _idleTimer;
    private float _idleInterval;

    public ZombieIdleState(Zombie zombie) : base(zombie)
    {
        _idleInterval = zombie.IdleInterval;
    }

    public override void Enter()
    {
        _idleTimer = 0f;
        _zombie.StopMove();
    }

    public override void Update()
    {
        _idleTimer += Time.deltaTime;

        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.ChangeState(ZombieState.Trace, "IdleToMove");
            return;
        }

        if (_idleTimer >= _idleInterval)
        {
            _idleInterval = Random.Range(1f, 3f);
            _zombie.ChangeState(ZombieState.Roaming, "IdleToMove");
        }
    }
}