using System;
using UnityEngine;

public class ZombieManager : MonoBehaviour
{
    private static ZombieManager _instance;
    public static ZombieManager Instance => _instance;

    public int maxCount = 5;

    private int _count;
    public int Count => _count;

    public event Action ZombieCountChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _count = 0;
    }

    public void ZombieSpawn()
    {
        _count++;
        ZombieCountChanged?.Invoke();
    }

    public void ZombieDeath()
    {
        _count--;
        ZombieCountChanged?.Invoke();
    }
}