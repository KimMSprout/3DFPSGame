using System;
using UnityEngine;

// 키보드 방향키에 따라 그 방향으로 이동하고 싶다.
public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private float _yVelocity = 0f; // 위 아래 속도, 매 프레임마다 중력 가속도가 붙는다.
    private float _xVelocity = 0f;

    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField] private float _dashSpeedMultiplier = 3f;
    [SerializeField] private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        _dashSpeedMultiplier = 1f;

        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0f, v);
        direction.Normalize(); // 대각선이어도 방향의 길이 1로 바꾸기 위해
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계 : 한 대상을 중심 (이 파일의 경우는 플레이어 중심)
        // 월드 좌표계 : 세상을 중심

        // 3. 땅에 서있지 않을때만 중력을 누적한다.
        if (_characterController.isGrounded == false)
        {
            _yVelocity += Gravity * Time.deltaTime;
        }
        else
        {
            _yVelocity = 0f;
        }

        // 4. 만약에 점프 키를 누르면... 점프를 한다.
        if (Input.GetButtonDown("Jump") && _characterController.isGrounded) // 중재자 패턴
        {
            _yVelocity = _jumpPower;
        }

        if (Input.GetButton("Fire3") && _characterController.isGrounded)
        {
            _dashSpeedMultiplier = 1.2f;
        }

        // 5. 속도를 구한다.
        Vector3 velocity = direction * _moveSpeed * _dashSpeedMultiplier;

        velocity.y = _yVelocity;

        // 6. 방향과 속력에 따라 이동한다.
        // transform.position += direction * Time.deltaTime * _moveSpeed;
        _characterController.Move(velocity * Time.deltaTime);
    }
}