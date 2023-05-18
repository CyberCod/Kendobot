using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;
        public ThirdPersonControllerNetwork controller;

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            controller.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            controller.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {


            controller.JumpInput(intState(virtualJumpState));
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            controller.SprintInput(intState(virtualSprintState));
        }





        private int intState(bool test)
        {

            if (test) { return 1; }
            return 0;


        }
    }


   
}
