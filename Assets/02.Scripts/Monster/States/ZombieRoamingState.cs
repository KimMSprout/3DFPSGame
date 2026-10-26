using UnityEngine;

// 로밍 : 무작위 위치로 이동한다.
public class ZombieRoamingState : ZombieStateBase
{
    private Vector3 _endPosition;

    public ZombieRoamingState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        float range = _zombie.RandomRoamingDistance;
        _endPosition = _zombie.transform.position +
                       new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));
    }

    public override void Update()
    {
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        if (Vector3.Distance(_zombie.transform.position, _endPosition) < 1f)
        {
            _zombie.StartPosition = _endPosition;
            _zombie.ChangeState(ZombieState.Idle);
            return;
        }

        Vector3 direction = _endPosition - _zombie.transform.position;
        direction.Normalize();
        direction.y = 0f;

        _zombie.transform.forward = direction;
        _zombie.Controller.Move(direction * _zombie.MoveSpeed * Time.deltaTime);
    }
}
