using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class oldStarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public int jump;
		public int sprint;
		public int punch;
		public int fightstance;
		public int fly;
		
		int started = 0;
		int performed = 1;
		int cancelled = -1;


		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputAction.CallbackContext context)
		{
			MoveInput(context.ReadValue<Vector2>());
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			if (cursorInputForLook)
			{
				LookInput(context.ReadValue<Vector2>());
			}
		}


		

		public void OnSprint(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Debug.Log("Performed");
					SprintInput(performed);
					break;
				case InputActionPhase.Started:
					Debug.Log("Started");
					SprintInput(started);
					break;
				case InputActionPhase.Canceled:
					Debug.Log("Cancelled");
					SprintInput(cancelled);
					break;

			}
		}



		public void OnPunch(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Debug.Log("Punch Performed");
					PunchInput(performed);
					break;
				case InputActionPhase.Started:
					Debug.Log("Punch Started");
					break;
				case InputActionPhase.Canceled:
					Debug.Log("Punch Cancelled");
					break;

			}
		}
		public void OnFightStance(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Debug.Log("FightStance Performed");
					FightStanceInput(performed);
					break;
				case InputActionPhase.Started:
					Debug.Log("FightStance Started");
					FightStanceInput(started);
					break;
				case InputActionPhase.Canceled:
					Debug.Log("FightStance Cancelled");
					FightStanceInput(cancelled);
					break;

			}
		}

		public void OnFly(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					FlyInput(performed);
					Debug.Log("Fly Performed");
					break;
				case InputActionPhase.Started:
					Debug.Log("Fly Started");
					FlyInput(started);
					break;
				case InputActionPhase.Canceled:
					Debug.Log("Fly Cancelled");
					FlyInput(cancelled);
					break;

			}
		}

/*
		public void OnInteraction(InputAction.CallbackContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Performed:
					Debug.Log("Performed");
					break;
				case InputActionPhase.Started:
					Debug.Log("Started");
					break;
				case InputActionPhase.Canceled:
					Debug.Log("Cancelled");
					break;

			}
		}
*/
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(int newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(int newSprintState)
		{
			sprint = newSprintState;
		}


		public void FlyInput(int newFlyState)
		{
			fly = newFlyState;
		}

		public void FightStanceInput(int newFightStanceState)
		{
			fightstance = newFightStanceState;
		}
		public void PunchInput(int newPunchState)
		{
			punch = newPunchState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}