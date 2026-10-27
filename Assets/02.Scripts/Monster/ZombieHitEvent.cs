using UnityEngine;

// Animator가 붙은 자식 오브젝트에 두고, 공격 애니메이션 클립의 Animation Event(Attack)로 호출한다.
public class ZombieHitEvent : MonoBehaviour
{
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    public void Attack()
    {
        _zombie.AttackAction();
    }
}