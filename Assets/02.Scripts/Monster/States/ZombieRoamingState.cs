using UnityEngine;
using UnityEngine.AI;

// 로밍 : 무작위 위치로 이동한다.
public class ZombieRoamingState : ZombieStateBase
{
    private Vector3 _endPosition;

    public ZombieRoamingState(Zombie zombie) : base(zombie)
    {
    }

    public override void Enter()
    {
        float range = _zombie.RandomRoamingDistance;
        Vector3 randomPosition = _zombie.transform.position +
                                 new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));

        // 무작위 위치에서 가장 가까운 NavMesh 위의 점을 목적지로 쓴다. 못 찾으면 제자리에서 끝낸다.
        if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, range, NavMesh.AllAreas))
        {
            _endPosition = hit.position;
        }
        else
        {
            _endPosition = _zombie.transform.position;
        }

        _zombie.MoveTo(_endPosition);
    }

    public override void Update()
    {
        if (_zombie.DistanceToPlayer() < _zombie.DetectDistance)
        {
            _zombie.ChangeState(ZombieState.Trace, "IdleToMove");
            return;
        }

        if (Vector3.Distance(_zombie.transform.position, _endPosition) < 1f)
        {
            _zombie.StartPosition = _endPosition;
            _zombie.ChangeState(ZombieState.Idle, "MoveToIdle");
        }
    }
}