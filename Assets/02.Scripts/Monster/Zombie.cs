using System;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

// FSM(유한 상태 머신) : 유한 개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"
// 각 상태의 행동은 Monster/States/ 의 상태 클래스가 맡고, Zombie는 스탯과 상태 전환을 관리한다.

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다.
    Roaming, // 로밍 : 무작위 이동
    Trace, // 추격 : 플레이어를 쫓아간다.
    Comback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이러를 때린다.
    Hit, // 피격 : 꽁격(총알, 폭탄)을 받아 잠깐 멈칛한다.
    Death, // 죽음 : 쓰러지고 잠시 뒤 사라진다.
}

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _idleInterval = 3f;
    [SerializeField] private float _detectDistance = 8f;
    [SerializeField] private float _attackDamage = 30f;
    [SerializeField] private float _attackRange = 3f;
    [SerializeField] private float _attackInterval = 3f;
    [SerializeField] private float _comabackDistance = 15f;
    [SerializeField] private float _randomRoamingDistance = 10f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _health;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private CharacterController _controller;
    private GameObject _player;

    private readonly Dictionary<ZombieState, IZombieState> _states = new Dictionary<ZombieState, IZombieState>();
    private IZombieState _currentState;
    private ZombieState _state = ZombieState.Idle;

    // 이 좀비에게 붙어 있는 피 이펙트 인스턴스. 죽을 때 풀에 반환한다.
    private readonly List<GameObject> _bloodSprays = new List<GameObject>();

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    // 상태 클래스가 읽는 값들
    public CharacterController Controller => _controller;
    public GameObject Player => _player;
    public Vector3 StartPosition { get; set; }
    public float IdleInterval => _idleInterval;
    public float DetectDistance => _detectDistance;
    public float AttackDamage => _attackDamage;
    public float AttackRange => _attackRange;
    public float AttackInterval => _attackInterval;
    public float ComebackDistance => _comabackDistance;
    public float RandomRoamingDistance => _randomRoamingDistance;
    public float MoveSpeed => _moveSpeed;

    public event Action ZombieDeathed;
    public event Action OnDamaged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        _player = GameObject.FindGameObjectWithTag("Player");
        StartPosition = transform.position;
        _health = _maxHealth;

        _states[ZombieState.Idle] = new ZombieIdleState(this);
        _states[ZombieState.Roaming] = new ZombieRoamingState(this);
        _states[ZombieState.Trace] = new ZombieTraceState(this);
        _states[ZombieState.Comback] = new ZombieComebackState(this);
        _states[ZombieState.Attack] = new ZombieAttackState(this);
        _states[ZombieState.Hit] = new ZombieHitState(this);
        _states[ZombieState.Death] = new ZombieDeathState(this);
    }

    private void OnEnable()
    {
        _health = _maxHealth;
        _controller.enabled = true;
        ChangeState(ZombieState.Idle);
        OnDamaged?.Invoke();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _currentState.Update();
    }

    public void ChangeState(ZombieState next)
    {
        if (_currentState != null)
        {
            Debug.Log($"{_state} => {next}");
            _currentState.Exit();
        }

        _state = next;
        _currentState = _states[next];
        _currentState.Enter();
    }

    public float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, _player.transform.position);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damageInfo.Value;
        OnDamaged?.Invoke();

        StopAllCoroutines();

        if (_health > 0)
        {
            // TODO : 맞은 부위에 피 이펙트를 실행하고 싶다.
            GameObject bloodSpray = LeanPool.Spawn(_bloodSprayPrefab, gameObject.transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            _bloodSprays.Add(bloodSpray);

            ChangeState(ZombieState.Hit);
        }
        else
        {
            ChangeState(ZombieState.Death);
        }
    }

    // 죽음 상태가 끝날 때 호출. 이펙트를 풀에 반환하고 좀비도 반환한다.
    public void Despawn()
    {
        ZombieDeathed?.Invoke();

        // 이미 자동 반환된 이펙트는 비활성 상태이므로 건너뛴다.
        foreach (GameObject bloodSpray in _bloodSprays)
        {
            if (bloodSpray != null && bloodSpray.activeInHierarchy)
            {
                LeanPool.Despawn(bloodSpray);
            }
        }

        _bloodSprays.Clear();
        LeanPool.Despawn(gameObject);
    }
}
