using Benchmarking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets;

public class StarterAssetsInputs : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;

	public Vector2 look;

	public bool jump;

	public bool sprint;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;

	public bool cursorInputForLook = true;

	public PlayerManager CameraManager;

	private bool m_IgnoreInput;

	private static bool m_FocusActionsSetUp;

	private void Start()
	{
		if (!m_FocusActionsSetUp)
		{
			InputAction inputAction = new InputAction(null, InputActionType.Value, "<pointer>/press");
			inputAction.performed += delegate
			{
				CameraManager.NotifyPlayerMoved();
			};
			inputAction.Enable();
			m_FocusActionsSetUp = true;
		}
	}

	private void OnDestroy()
	{
		m_FocusActionsSetUp = false;
	}

	public void OnMove(InputValue value)
	{
		if (m_IgnoreInput)
		{
			MoveInput(Vector2.zero);
			return;
		}
		if (CameraManager != null)
		{
			CameraManager.NotifyPlayerMoved();
		}
		MoveInput(value.Get<Vector2>());
	}

	public void OnLook(InputValue value)
	{
		if (m_IgnoreInput)
		{
			LookInput(Vector2.zero);
			return;
		}
		if (CameraManager != null)
		{
			CameraManager.NotifyPlayerMoved();
		}
		if (cursorInputForLook)
		{
			LookInput(value.Get<Vector2>());
		}
	}

	public void OnJump(InputValue value)
	{
		if (CameraManager != null)
		{
			CameraManager.NotifyPlayerMoved();
		}
		JumpInput(value.isPressed);
	}

	public void OnSprint(InputValue value)
	{
		if (CameraManager != null)
		{
			CameraManager.NotifyPlayerMoved();
		}
		SprintInput(value.isPressed);
	}

	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}

	public void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	public void JumpInput(bool newJumpState)
	{
		jump = newJumpState;
	}

	public void SprintInput(bool newSprintState)
	{
		sprint = newSprintState;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(cursorLocked);
		m_IgnoreInput = !hasFocus;
	}

	private void SetCursorState(bool newState)
	{
		if (!PerformanceTest.RunningBenchmark)
		{
			Cursor.lockState = (newState ? CursorLockMode.Locked : CursorLockMode.None);
		}
	}
}
