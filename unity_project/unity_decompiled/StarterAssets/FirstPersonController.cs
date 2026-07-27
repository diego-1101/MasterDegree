using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FirstPersonController : MonoBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 4f;

	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 6f;

	[Tooltip("Rotation speed of the character")]
	public float RotationSpeed = 1f;

	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10f;

	[Space(10f)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;

	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15f;

	[Space(10f)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0.1f;

	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;

	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;

	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.5f;

	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;

	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 90f;

	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -90f;

	private float _cinemachineTargetPitch;

	private float _speed;

	private float _rotationVelocity;

	private float _verticalVelocity;

	private float _terminalVelocity = 53f;

	private float _jumpTimeoutDelta;

	private float _fallTimeoutDelta;

	private PlayerInput _playerInput;

	private CharacterController _controller;

	private StarterAssetsInputs _input;

	private GameObject _mainCamera;

	private const float _threshold = 0.01f;

	private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

	private void Awake()
	{
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}
	}

	private void Start()
	{
		_controller = GetComponent<CharacterController>();
		_input = GetComponent<StarterAssetsInputs>();
		_playerInput = GetComponent<PlayerInput>();
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
	}

	private void Update()
	{
		JumpAndGravity();
		GroundedCheck();
		Move();
	}

	private void LateUpdate()
	{
		CameraRotation();
	}

	private void GroundedCheck()
	{
		Vector3 position = new Vector3(base.transform.position.x, base.transform.position.y - GroundedOffset, base.transform.position.z);
		Grounded = Physics.CheckSphere(position, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	private void CameraRotation()
	{
		if (_input.look.sqrMagnitude >= 0.01f)
		{
			float num = (IsCurrentDeviceMouse ? 1f : Time.deltaTime);
			_cinemachineTargetPitch += _input.look.y * RotationSpeed * num;
			_rotationVelocity = _input.look.x * RotationSpeed * num;
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0f, 0f);
			base.transform.Rotate(Vector3.up * _rotationVelocity);
		}
	}

	private void Move()
	{
		float num = (_input.sprint ? SprintSpeed : MoveSpeed);
		if (_input.move == Vector2.zero)
		{
			num = 0f;
		}
		float magnitude = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
		float num2 = 0.1f;
		float num3 = (_input.analogMovement ? _input.move.magnitude : 1f);
		if (magnitude < num - num2 || magnitude > num + num2)
		{
			_speed = Mathf.Lerp(magnitude, num * num3, Time.deltaTime * SpeedChangeRate);
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = num;
		}
		Vector3 vector = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
		if (_input.move != Vector2.zero)
		{
			vector = base.transform.right * _input.move.x + base.transform.forward * _input.move.y;
		}
		if (_controller.enabled)
		{
			_controller.Move(vector.normalized * (_speed * Time.deltaTime) + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
		}
	}

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			_fallTimeoutDelta = FallTimeout;
			if (_verticalVelocity < 0f)
			{
				_verticalVelocity = -2f;
			}
			if (_input.jump && _jumpTimeoutDelta <= 0f)
			{
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			}
			if (_jumpTimeoutDelta >= 0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			_jumpTimeoutDelta = JumpTimeout;
			if (_fallTimeoutDelta >= 0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}
			_input.jump = false;
		}
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f)
		{
			lfAngle += 360f;
		}
		if (lfAngle > 360f)
		{
			lfAngle -= 360f;
		}
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	private void OnDrawGizmosSelected()
	{
		Color color = new Color(0f, 1f, 0f, 0.35f);
		Color color2 = new Color(1f, 0f, 0f, 0.35f);
		if (Grounded)
		{
			Gizmos.color = color;
		}
		else
		{
			Gizmos.color = color2;
		}
		Gizmos.DrawSphere(new Vector3(base.transform.position.x, base.transform.position.y - GroundedOffset, base.transform.position.z), GroundedRadius);
	}
}
