using System;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.AI;

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

[RequireComponent(typeof(NavMeshAgent))]
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

    [Header("점프")]
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpSpeed = 6f;
    [SerializeField] private float _minJumpDuration = 0.3f;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab;

    private NavMeshAgent _agent;
    private bool _isJumping;
    private Vector3 _jumpEnd;
    private GameObject _player;

    private readonly Dictionary<ZombieState, IZombieState> _states = new Dictionary<ZombieState, IZombieState>();
    public Dictionary<ZombieState, IZombieState> States => _states;
    private IZombieState _currentState;
    private ZombieAttackState _attackState;
    private ZombieState _state = ZombieState.Idle;

    // 이 좀비에게 붙어 있는 피 이펙트 인스턴스. 죽을 때 풀에 반환한다.
    private readonly List<GameObject> _bloodSprays = new List<GameObject>();

    public Animator animator;

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    // 상태 클래스가 읽는 값들
    public NavMeshAgent Agent => _agent;
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
    public bool IsJumping => _isJumping;

    public event Action ZombieDeathed;
    public event Action OnDamaged;

    private void Awake()
    {
        // 길을 찾아 걷게 해주는 컴포넌트 NavMeshAgent를 찾아서 참조. 모든 이동은 이 agent가 맡는다.
        _agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        _agent.speed = _moveSpeed;

        _player = GameObject.FindGameObjectWithTag("Player");
        _health = _maxHealth;

        _states[ZombieState.Idle] = new ZombieIdleState(this);
        _states[ZombieState.Roaming] = new ZombieRoamingState(this);
        _states[ZombieState.Trace] = new ZombieTraceState(this);
        _states[ZombieState.Comback] = new ZombieComebackState(this);
        _attackState = new ZombieAttackState(this);
        _states[ZombieState.Attack] = _attackState;
        _states[ZombieState.Hit] = new ZombieHitState(this);
        _states[ZombieState.Death] = new ZombieDeathState(this);
    }

    private void OnEnable()
    {
        _health = _maxHealth;

        // 죽을 때 꺼둔 agent를 다시 켜면 현재 위치에서 가장 가까운 NavMesh 위로 올라선다.
        _agent.enabled = true;
        _agent.autoTraverseOffMeshLink = true;
        _isJumping = false;
        StartPosition = transform.position;

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

    public void ChangeState(ZombieState next, string trigger)
    {
        if (_currentState != null)
        {
            Debug.Log($"{_state} => {next}");
            _currentState.Exit();
        }

        _state = next;
        _currentState = _states[next];
        animator.SetTrigger(trigger);
        _currentState.Enter();
    }

    // 상태 클래스가 쓰는 이동 명령. agent가 NavMesh 위에 없으면 아무것도 하지 않는다.
    public void MoveTo(Vector3 destination, float stoppingDistance = 0f)
    {
        if (_agent.isOnNavMesh == false) return;

        _agent.stoppingDistance = stoppingDistance;
        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    public void StopMove()
    {
        if (_agent.isOnNavMesh == false) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    // 추격 상태가 켜고 끄는 값. false면 agent가 링크를 직접 건너지 않고 JumpAcrossLink가 대신 건넌다.
    public void SetAutoTraverseLink(bool value)
    {
        _agent.autoTraverseOffMeshLink = value;
    }

    // agent가 NavMesh Link 위에 올라서면 포물선 점프로 건너편까지 이동한다.
    public void TryJumpAcrossLink()
    {
        if (_isJumping || _agent.isOnNavMesh == false || _agent.isOnOffMeshLink == false) return;

        StartCoroutine(JumpAcrossLink_Coroutine());
    }

    private IEnumerator JumpAcrossLink_Coroutine()
    {
        _isJumping = true;

        OffMeshLinkData link = _agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        Vector3 end = link.endPos + Vector3.up * _agent.baseOffset;
        _jumpEnd = end;

        Vector3 flat = end - start;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(flat);
        }

        // 거리를 속도로 나눈 시간 동안 t를 0에서 1로 올린다.
        float duration = Mathf.Max(_minJumpDuration, Vector3.Distance(start, end) / _jumpSpeed);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 직선 보간 + 포물선 높이(4t(1-t)는 t=0.5에서 1이 되는 포물선)
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += _jumpHeight * 4f * t * (1f - t);
            transform.position = position;

            yield return null;
        }

        FinishJump();
    }

    // 점프를 끝내고 agent에게 링크 통과 완료를 알린다. 피격으로 코루틴이 끊겼을 때도 호출된다.
    private void FinishJump()
    {
        transform.position = _jumpEnd;
        _isJumping = false;

        if (_agent.isOnNavMesh && _agent.isOnOffMeshLink)
        {
            _agent.CompleteOffMeshLink();
        }

        // 점프 중에 추격이 끝났다면 기본 동작(자동 통과)으로 되돌린다.
        _agent.autoTraverseOffMeshLink = _state != ZombieState.Trace;
    }

    // 공격 애니메이션의 타격 이벤트(ZombieHitEvent)가 호출. 공격 상태일 때만 데미지를 준다.
    public void AttackAction()
    {
        if (_state != ZombieState.Attack) return;

        _attackState.AttackAction();
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

        // 점프 코루틴이 끊기면 agent가 링크 위에 남으므로 도착 지점에서 마무리한다.
        if (_isJumping)
        {
            FinishJump();
        }

        if (_health > 0)
        {
            // TODO : 맞은 부위에 피 이펙트를 실행하고 싶다.
            GameObject bloodSpray = LeanPool.Spawn(_bloodSprayPrefab, gameObject.transform);
            bloodSpray.transform.position = damageInfo.HitPoint;
            bloodSpray.transform.forward = damageInfo.HitNormal;
            _bloodSprays.Add(bloodSpray);

            ChangeState(ZombieState.Hit, "Hit");
        }
        else
        {
            ChangeState(ZombieState.Death, "Death");
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

        // 씬에 미리 배치된 좀비는 풀에서 만든 게 아니므로 직접 파괴한다.
        LeanGameObjectPool pool = null;
        if (LeanPool.Links.ContainsKey(gameObject) || LeanGameObjectPool.TryFindPoolByClone(gameObject, ref pool))
        {
            LeanPool.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}