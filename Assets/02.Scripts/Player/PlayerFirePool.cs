using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// PlayerGunFire와 PlayerBombFire가 사용하는 발사 이펙트/폭탄 풀을 한 곳에서 관리한다.
// 두 발사 스크립트가 Awake에서 프리팹을 등록하므로 별도의 씬 오브젝트를 추가할 필요가 없다.
public sealed class PlayerFirePool : MonoBehaviour
{
    private const int DefaultHitEffectCapacity = 8;
    private const int DefaultBombCapacity = 10;
    private const int MaxPoolSize = 64;

    private static PlayerFirePool _instance;

    private readonly Dictionary<int, HitEffectPool> _hitEffectPools = new Dictionary<int, HitEffectPool>();
    private readonly Dictionary<int, BombPool> _bombPools = new Dictionary<int, BombPool>();
    private Transform _poolRoot;

    public static PlayerFirePool Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameObject poolObject = new GameObject(nameof(PlayerFirePool));
            poolObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(poolObject);
            _instance = poolObject.AddComponent<PlayerFirePool>();
            return _instance;
        }
    }

    public static void RegisterHitEffect(ParticleSystem prefab)
    {
        if (prefab != null)
        {
            Instance.GetHitEffectPool(prefab);
        }
    }

    public static void RegisterBomb(Bomb prefab)
    {
        if (prefab != null)
        {
            Instance.GetBombPool(prefab);
        }
    }

    public static ParticleSystem GetHitEffect(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        HitEffectPool pool = Instance.GetHitEffectPool(prefab);
        ParticleSystem effect = pool.Pool.Get();
        effect.transform.SetPositionAndRotation(position, rotation);
        return effect;
    }

    public static Bomb GetBomb(Bomb prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        BombPool pool = Instance.GetBombPool(prefab);
        Bomb bomb = pool.Pool.Get();
        bomb.transform.SetPositionAndRotation(position, rotation);
        return bomb;
    }

    internal void ReleaseHitEffect(ParticleSystem effect)
    {
        if (effect == null)
        {
            return;
        }

        PooledParticleEffect pooledEffect = effect.GetComponent<PooledParticleEffect>();
        if (pooledEffect == null || pooledEffect.IsInPool)
        {
            return;
        }

        pooledEffect.MarkInPool();

        int prefabId = pooledEffect.PrefabId;
        if (_hitEffectPools.TryGetValue(prefabId, out HitEffectPool pool))
        {
            pool.Pool.Release(effect);
        }
        else
        {
            Destroy(effect.gameObject);
        }
    }

    internal void ReleaseBomb(Bomb bomb)
    {
        if (bomb == null)
        {
            return;
        }

        foreach (BombPool pool in _bombPools.Values)
        {
            if (pool.Contains(bomb))
            {
                pool.Pool.Release(bomb);
                return;
            }
        }

        // 풀이 없는 폭탄(예: 풀링 적용 전 씬 인스턴스)은 기존 동작처럼 제거한다.
        Destroy(bomb.gameObject);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        GameObject rootObject = new GameObject("PooledObjects");
        rootObject.transform.SetParent(transform, false);
        _poolRoot = rootObject.transform;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private HitEffectPool GetHitEffectPool(ParticleSystem prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (_hitEffectPools.TryGetValue(prefabId, out HitEffectPool existingPool))
        {
            return existingPool;
        }

        HitEffectPool newPool = new HitEffectPool(this, prefab);
        _hitEffectPools.Add(prefabId, newPool);
        newPool.Prewarm(DefaultHitEffectCapacity);
        return newPool;
    }

    private BombPool GetBombPool(Bomb prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (_bombPools.TryGetValue(prefabId, out BombPool existingPool))
        {
            return existingPool;
        }

        BombPool newPool = new BombPool(this, prefab);
        _bombPools.Add(prefabId, newPool);
        newPool.Prewarm(DefaultBombCapacity);
        return newPool;
    }

    private ParticleSystem CreateHitEffect(ParticleSystem prefab)
    {
        ParticleSystem effect = Instantiate(prefab, _poolRoot);
        effect.gameObject.SetActive(false);

        PooledParticleEffect pooledEffect = effect.GetComponent<PooledParticleEffect>();
        if (pooledEffect == null)
        {
            pooledEffect = effect.gameObject.AddComponent<PooledParticleEffect>();
        }

        pooledEffect.Initialize(this, prefab.GetInstanceID(), effect);

        ParticleSystem.MainModule mainModule = effect.main;
        mainModule.stopAction = ParticleSystemStopAction.Callback;
        return effect;
    }

    private void GetHitEffectForPool(ParticleSystem effect)
    {
        PooledParticleEffect pooledEffect = effect.GetComponent<PooledParticleEffect>();
        pooledEffect.MarkActive();
        effect.Clear(true);
        effect.gameObject.SetActive(true);
    }

    private void ReleaseHitEffectForPool(ParticleSystem effect)
    {
        PooledParticleEffect pooledEffect = effect.GetComponent<PooledParticleEffect>();
        pooledEffect.MarkInPool();
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Clear(true);
        effect.gameObject.SetActive(false);
    }

    private Bomb CreateBomb(Bomb prefab)
    {
        Bomb bomb = Instantiate(prefab, _poolRoot);
        bomb.gameObject.SetActive(false);
        bomb.SetPool(this);
        return bomb;
    }

    private void GetBombForPool(Bomb bomb)
    {
        bomb.PrepareForReuse();
        bomb.gameObject.SetActive(true);
    }

    private void ReleaseBombForPool(Bomb bomb)
    {
        bomb.ResetForPool();
        bomb.gameObject.SetActive(false);
    }

    private sealed class HitEffectPool
    {
        internal readonly ObjectPool<ParticleSystem> Pool;

        internal HitEffectPool(PlayerFirePool owner, ParticleSystem prefab)
        {
            Pool = new ObjectPool<ParticleSystem>(
                () => owner.CreateHitEffect(prefab),
                owner.GetHitEffectForPool,
                owner.ReleaseHitEffectForPool,
                effect => Destroy(effect.gameObject),
                collectionCheck: true,
                defaultCapacity: DefaultHitEffectCapacity,
                maxSize: MaxPoolSize);
        }

        internal void Prewarm(int count)
        {
            List<ParticleSystem> prewarmed = new List<ParticleSystem>(count);
            for (int i = 0; i < count; i++)
            {
                prewarmed.Add(Pool.Get());
            }

            foreach (ParticleSystem effect in prewarmed)
            {
                Pool.Release(effect);
            }
        }
    }

    private sealed class BombPool
    {
        internal readonly ObjectPool<Bomb> Pool;
        private readonly HashSet<int> _instances = new HashSet<int>();

        internal BombPool(PlayerFirePool owner, Bomb prefab)
        {
            Pool = new ObjectPool<Bomb>(
                () => owner.CreateBomb(prefab),
                bomb =>
                {
                    _instances.Add(bomb.GetInstanceID());
                    owner.GetBombForPool(bomb);
                },
                owner.ReleaseBombForPool,
                bomb => Destroy(bomb.gameObject),
                collectionCheck: true,
                defaultCapacity: DefaultBombCapacity,
                maxSize: MaxPoolSize);
        }

        internal bool Contains(Bomb bomb)
        {
            return _instances.Contains(bomb.GetInstanceID());
        }

        internal void Prewarm(int count)
        {
            List<Bomb> prewarmed = new List<Bomb>(count);
            for (int i = 0; i < count; i++)
            {
                prewarmed.Add(Pool.Get());
            }

            foreach (Bomb bomb in prewarmed)
            {
                Pool.Release(bomb);
            }
        }
    }
}

// ParticleSystem의 재생이 끝났을 때 자신이 속한 풀로 돌아가게 하는 헬퍼 컴포넌트.
internal sealed class PooledParticleEffect : MonoBehaviour
{
    private PlayerFirePool _owner;
    private ParticleSystem _particleSystem;

    internal int PrefabId { get; private set; }
    internal bool IsInPool { get; private set; }

    internal void Initialize(PlayerFirePool owner, int prefabId, ParticleSystem particleSystem)
    {
        _owner = owner;
        PrefabId = prefabId;
        _particleSystem = particleSystem;
        IsInPool = true;
    }

    internal void MarkActive()
    {
        IsInPool = false;
    }

    internal void MarkInPool()
    {
        IsInPool = true;
    }

    private void OnParticleSystemStopped()
    {
        if (!IsInPool)
        {
            _owner.ReleaseHitEffect(_particleSystem);
        }
    }
}
