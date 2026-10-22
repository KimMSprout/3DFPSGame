using System;
using System.Collections;
using UnityEngine;

// FSM(유한 상태 머신) : 유한 개의 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 유니티에서 애니메이션 -> 상태 머신

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

public class Zombie : MonoBehaviour
{
    [Header("스탯")]
    [SerializeField] private float _idleInterval = 3f;
    [SerializeField] private float _detectDistance = 8f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _comabackDistance = 15f;
    [SerializeField] private float _randomRoamingDistance = 10f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _health = 30f;

    private float _idleTimer = 0f;
    private float _attackTimer = 0f;
    private CharacterController _controller;
    private GameObject _player;
    private Vector3 _startPosition;

    private Vector3 _endPosition;

    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        _player = GameObject.FindGameObjectWithTag("Player");
        _startPosition = transform.position;
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
                Idle();
                break;
            case ZombieState.Roaming:
                Roaming();
                break;
            case ZombieState.Trace:
                Trace();
                break;
            case ZombieState.Comback:
                Comback();
                break;
            case ZombieState.Attack:
                Attack();
                break;
        }
    }

    private void Idle()
    {
        _idleTimer += Time.deltaTime;

        float distance = Vector3.Distance(transform.position, _player.transform.position);

        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} => Trace");
            _state = ZombieState.Trace;
            return;
        }

        if (_idleTimer >= _idleInterval)
        {
            Debug.Log($"{_state} => Roaming");
            _state = ZombieState.Roaming;
            _endPosition = transform.position +
                           new Vector3(UnityEngine.Random.Range(_randomRoamingDistance * -1f, _randomRoamingDistance),
                               0f, UnityEngine.Random.Range(_randomRoamingDistance * -1f, _randomRoamingDistance));
            _idleInterval = UnityEngine.Random.Range(1f, 3f);
        }
    }

    private void Roaming()
    {
        _idleTimer = 0f;

        float distance = Vector3.Distance(transform.position, _player.transform.position);

        if (distance < _detectDistance)
        {
            Debug.Log($"{_state} => Trace");
            _state = ZombieState.Trace;
            return;
        }

        if (Vector3.Distance(transform.position, _endPosition) < 1f)
        {
            Debug.Log($"{_state} => Idle");
            _state = ZombieState.Idle;
            _startPosition = _endPosition;
            return;
        }

        Vector3 direction = _endPosition - transform.position;
        direction.Normalize();
        direction.y = 0f;

        transform.forward = direction;
        _controller.Move(direction * _moveSpeed * Time.deltaTime);
    }

    private void Trace()
    {
        // TODO : Run 애니메이션 실행

        if (Vector3.Distance(transform.position, _startPosition) > _comabackDistance)
        {
            Debug.Log($"{_state} => Comback");
            _state = ZombieState.Comback;
            return;
        }

        if (Vector3.Distance(transform.position, _player.transform.position) < _attackRange)
        {
            Debug.Log($"{_state} => Attack");
            _state = ZombieState.Attack;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.Move(direction * _moveSpeed * Time.deltaTime);
    }

    private void Comback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            Debug.Log($"{_state} => Idle");
            _state = ZombieState.Idle;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
        // 플레이어가 공격 사정 거리 밖으로 나가면 쫓아!
        float distance = Vector3.Distance(transform.position, _player.transform.position);

        if (distance < _attackRange)
        {
            Debug.Log($"{_state} => Trace");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _attackInterval)
        {
            // TODO : 공격 애니메이션 실행
            // TODO : 플레이어를 떄려!

            Debug.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damage;

        StopAllCoroutines();

        if (_health > 0)
        {
            Debug.Log($"{_state} -> Hit");
            _state = ZombieState.Hit;
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            Debug.Log($"{_state} -> Death");
            _state = ZombieState.Death;
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        yield return new WaitForSeconds(0.3f); // 잠깐 멈칫한다!!

        // 맞았으니 쫓아 온다.
        Debug.Log($"{_state} -> Trace");
        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        _controller.enabled = false;
        yield return new WaitForSeconds(0.5f); // 잠깐 멈칫한다!!

        // 맞았으니 쫓아 온다.
        Destroy(gameObject);
    }
}