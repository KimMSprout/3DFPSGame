using System.Collections;
using UnityEngine;

// 죽음 : 쓰러지고 잠시 뒤 사라진다.
public class ZombieDeathState : ZombieStateBase
{
    private const float DespawnDelay = 0.5f;

    public ZombieDeathState(Zombie zombie) : base(zombie) { }

    public override void Enter()
    {
        _zombie.Controller.enabled = false;
        _zombie.StartCoroutine(Death_Coroutine());
    }

    public override void Update() { }

    private IEnumerator Death_Coroutine()
    {
        yield return new WaitForSeconds(DespawnDelay);

        _zombie.Despawn();
    }
}
