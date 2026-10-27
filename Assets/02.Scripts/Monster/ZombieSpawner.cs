using System;
using Lean.Pool;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private Transform[] _spawnPoints;

    private float _elapsed;

    private void Start()
    {
        _elapsed = _spawnInterval;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= _spawnInterval && ZombieManager.Instance.Count < ZombieManager.Instance.maxCount)
        {
            Transform spawnPoint = _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)];

            GameObject zombiePrefab = LeanPool.Spawn(_zombiePrefab, spawnPoint.position + Vector3.up,
                transform.rotation, gameObject.transform);
            Zombie zombie = zombiePrefab.GetComponent<Zombie>();

            // 풀에서 재사용되는 좀비에 핸들러가 중복 등록되지 않도록 먼저 해제한다.
            zombie.ZombieDeathed -= ZombieDeath;
            zombie.ZombieDeathed += ZombieDeath;

            // Spawn 시점(OnEnable)에는 아직 구독 전이므로 스포너가 직접 카운트를 올린다.
            ZombieManager.Instance.ZombieSpawn();
            _elapsed = 0f;
        }
    }

    private void ZombieDeath()
    {
        ZombieManager.Instance.ZombieDeath();
    }
}