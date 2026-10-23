using UnityEngine;

public class Box : MonoBehaviour, IDamageable
{
    private float health = 30f;

    public void TakeDamage(DamageInfo damageInfo)
    {
        health -= damageInfo.Value;

        if (health <= 0f)
        {
            // TODO : 파편 이펙트 발생
        }
    }
}