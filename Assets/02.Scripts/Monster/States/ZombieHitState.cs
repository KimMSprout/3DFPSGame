using System.Collections;
using UnityEngine;

// 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
public class ZombieHitState : ZombieStateBase
{
    private const float StaggerTime = 0.3f;

    public ZombieHitState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _zombie.StartCoroutine(Stagger_Coroutine());
    }

    public override void Update() { }

    private IEnumerator Stagger_Coroutine()
    {
        yield return new WaitForSeconds(StaggerTime);

        // 맞았으니 쫓아 온다.
        _zombie.ChangeState(ZombieState.Trace);
    }
}
