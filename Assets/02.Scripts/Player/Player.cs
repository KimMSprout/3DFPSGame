using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private UI_HitEffect _uiHitEffect;

    private float _health;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private void Start()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return;

        _health -= damageInfo.Value;
        _uiHitEffect.Flash();
        _uiHitEffect.Fade(0.6f);
        Debug.Log($"플레이어가 맞았다. 데미지 : {damageInfo.Value}");

        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다.");
            GameManager.Instance.GameOver();
        }
    }
}