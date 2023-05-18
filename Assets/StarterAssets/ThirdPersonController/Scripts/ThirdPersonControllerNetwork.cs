using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{


    [RequireComponent(typeof(CharacterController))]


    public class ThirdPersonControllerNetwork : NetworkBehaviour
    {
        public Animator _animator;
        //Debug values
        bool Freefall = false;
        float MovementSpeed;
        bool Jumping = false;

        public Transform CameraMount;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Speed of turning")]
        public float TurnSpeed = 1.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDPunch;
        private int _animIDSprint;
        private int _animIDFly;
        private int _animIDFightingStanceActive;


        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse = true;
        /*{
            get
            {
                return _playerInput.currentControlScheme == "KeyboardMouse";
            }
        }*/


        private void Awake()
        {

            _input = new StarterAssetsInputs();
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            }



        }













        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _animator = GetComponent<Animator>();
            _hasAnimator = TryGetComponent(out _animator);

            _controller = GetComponent<CharacterController>();
            //_input = GetComponent<StarterAssetsInputs>();



            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }


        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _mainCamera.GetComponent<PlayerCamera>().playerInstance = CameraMount.transform;
            }
        }


        private void Update()
        {
            if (_input == null) { _input = GetComponent<StarterAssetsInputs>(); }
            if (_animator == null) { _hasAnimator = TryGetComponent(out _animator); }
            GetCurrentAnimation();
            JumpAndGravity();

            GroundedCheck();


            if (IsOwner)
            {
                //Debugger("Debugger On");
                Move();


            }


        }



        private void Debugger(string printme)  //-------------------------------------------------------------------------------DEBUGGER-----------------------------
        {
            //if (!DEBUGGING_ACTIVE) return;
            Debug.Log(printme);
            //Debug.Log("Grounded:" + Grounded + "  Jump:" + Jumping +jump+ "  Freefall:" + Freefall + " Speed:" + _speed);
            //Debug.Log("CurrentAnim = " + currentAnimation + " Sprint:" + sprint + "  Fly:" + fly + " Stance:" + fightstance + "  Punch:" + punch);


        }


        private void LateUpdate()
        {
            CameraRotation();
        }




        //---------------------------------------------
        //                INPUT SECTION
        //---------------------------------------------

        [Header("Character Input Values")]
        //these values get set by the input system on a dynamic update outside the normal update cycle.  They are always current to the input states
        public Vector2 move;
        public Vector2 look;
        public int jump = 0;
        public int sprint = 0;
        public int punch = 0;
        public int fightstance = 0;
        public int fly = 0;

        int notstarted = 0;
        int started = 1;
        int performed = 2;
        int canceled = -1;


        [Header("Movement Settings")]
        public bool analogMovement;

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private void OnEnable()  //enable individual inputs and subscribe them to their output methods
        {
            _input.Enable();
            _input.Player.Move.Enable();
            _input.Player.Look.Enable();
            _input.Player.Sprint.Enable();
            _input.Player.Punch.Enable();
            _input.Player.FightStance.Enable();
            _input.Player.Fly.Enable();
            _input.Player.Jump.Enable();

            _input.Player.Jump.performed += OnJump;
            _input.Player.Jump.canceled += OnJump;
            _input.Player.Jump.started += OnJump;

            _input.Player.Move.performed += OnMove;
            _input.Player.Move.canceled += OnMove;
            _input.Player.Move.started += OnMove;

            _input.Player.Look.performed += OnLook;
            _input.Player.Look.canceled += OnLook;
            _input.Player.Look.started += OnLook;

            _input.Player.Sprint.performed += OnSprint;
            _input.Player.Sprint.canceled += OnSprint;
            _input.Player.Sprint.started += OnSprint;

            _input.Player.Punch.performed += OnPunch;
            _input.Player.Punch.canceled += OnPunch;
            _input.Player.Punch.started += OnPunch;

            _input.Player.FightStance.performed += OnFightStance;
            _input.Player.FightStance.canceled += OnFightStance;
            _input.Player.FightStance.started += OnFightStance;

            _input.Player.Fly.performed += OnFly;
            _input.Player.Fly.canceled += OnFly;
            _input.Player.Fly.started += OnFly;



        }


        private void OnDisable() //remove method subscriptsions and disable
        {
            _input.Player.Jump.performed -= OnJump;
            _input.Player.Jump.canceled -= OnJump;
            _input.Player.Jump.started -= OnJump;

            _input.Player.Move.performed -= OnMove;
            _input.Player.Move.canceled -= OnMove;
            _input.Player.Move.started -= OnMove;

            _input.Player.Look.performed -= OnLook;
            _input.Player.Look.canceled -= OnLook;
            _input.Player.Look.started -= OnLook;

            _input.Player.Sprint.performed -= OnSprint;
            _input.Player.Sprint.canceled -= OnSprint;
            _input.Player.Sprint.started -= OnSprint;

            _input.Player.Punch.performed -= OnPunch;
            _input.Player.Punch.canceled -= OnPunch;
            _input.Player.Punch.started -= OnPunch;

            _input.Player.FightStance.performed -= OnFightStance;
            _input.Player.FightStance.canceled -= OnFightStance;
            _input.Player.FightStance.started -= OnFightStance;

            _input.Player.Fly.performed -= OnFly;
            _input.Player.Fly.canceled -= OnFly;
            _input.Player.Fly.started -= OnFly;

            _input.Player.Move.Disable();
            _input.Player.Look.Disable();
            _input.Player.Sprint.Disable();
            _input.Player.Punch.Disable();
            _input.Player.FightStance.Disable();
            _input.Player.Fly.Disable();
            _input.Player.Jump.Disable();

            _input.Disable();

            //_input.Player.. += On; 


        }
        //These are just reference ints for internal use, new animations need to be represented here
        public int currentAnimation;
        int idleAnim = 0;
        int walkingAnim = 1;
        int runningAnim = 2;
        int jumptoflyAnim = 3;
        int flytolandAnim = 4;
        int jumpbeginAnim = 5;
        int jumptopAnim = 6;
        int jumpendingAnim = 7;
        int bigjumpAnim = 8;
        int fightstanceAnim = 9;
        int punchAnim = 10;


        public int GetCurrentAnimation()  //Get the animation that is currently playing to see if an action has been transmitted to the animator
        {

            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) { currentAnimation = idleAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Walking")) { currentAnimation = walkingAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Running")) { currentAnimation = runningAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("JumpToFly")) { currentAnimation = jumptoflyAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("FlyToLand")) { currentAnimation = flytolandAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("JumpBegin")) { currentAnimation = jumpbeginAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("JumpTop")) { currentAnimation = jumptopAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("JumpEnding")) { currentAnimation = jumpendingAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("BigJump")) { currentAnimation = bigjumpAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("FightStance")) { currentAnimation = fightstanceAnim; }
            if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Punch")) { currentAnimation = punchAnim; }
            //if (_animator.GetCurrentAnimatorStateInfo(0).IsName("")) {currentAnimation =  ; }
            //if (_animator.GetCurrentAnimatorStateInfo(0).IsName("")) {currentAnimation =  ; }
            return currentAnimation;

        }



        //          INTERACTION INPUT TEMPLATE   don't delete
        // replace "Interaction" with the button or function name of choice throughout
        /*
         * 
        public void OnInteraction(InputAction.CallbackContext context)
        {
                switch (context.phase)
                {
                        case InputActionPhase.Performed:
                            Debugger("Performed");
                            //logic to store variable state goes here
                            InteractionInput(performed);
                            break;
                        case InputActionPhase.Started:
                            Debugger("Started");
                            //logic to store variable state goes here
                            InteractionInput(Started);
                            break;
                        case InputActionPhase.Canceled:
                            Debugger("canceled");
                            //logic to store variable state goes here
                            InteractionInput(Canceled);
                            break;
                    }
        }

        int localInteractionIntegerVariable;   //this variable is used in the local code to reflect the input

        public void InteractionInput(int newInteractionStateValue)  //replace "Interaction" with the button or function of choice
        {
            localInteractionIntegerVariable = newInteractionValue;
        }

        */

        //Input methods, these are run dynamically by the new input system as input is received, updating the local variables

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
                    Debugger("Sprint Performed");
                    SprintInput(performed);
                    break;
                case InputActionPhase.Started:
                    Debugger("Sprint Started");
                    SprintInput(started);
                    break;
                case InputActionPhase.Canceled:
                    Debugger("Sprint canceled");
                    SprintInput(canceled);
                    break;

            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Performed:
                    Debugger("Jump Performed");
                    JumpInput(performed);
                    break;
                case InputActionPhase.Started:
                    Debugger("Jump Started");
                    JumpInput(started);
                    break;
                case InputActionPhase.Canceled:
                    Debugger("Jump canceled");
                    JumpInput(canceled);
                    break;

            }
        }

        public void OnPunch(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Performed:
                    Debugger("Punch Performed");
                    PunchInput(performed);
                    break;
                case InputActionPhase.Started:
                    Debugger("Punch Started");
                    PunchInput(started);
                    break;
                case InputActionPhase.Canceled:
                    Debugger("Punch canceled");
                    PunchInput(canceled);
                    break;

            }
        }
        public void OnFightStance(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Performed:
                    Debugger("FightStance Performed");
                    FightStanceInput(performed);
                    break;
                case InputActionPhase.Started:
                    Debugger("FightStance Started");
                    FightStanceInput(started);
                    break;
                case InputActionPhase.Canceled:
                    Debugger("FightStance canceled");
                    FightStanceInput(canceled);
                    break;

            }
        }

        public void OnFly(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Performed:
                    FlyInput(performed);
                    Debugger("Fly Performed");
                    break;
                case InputActionPhase.Started:
                    Debugger("Fly Started");
                    FlyInput(started);
                    break;
                case InputActionPhase.Canceled:
                    Debugger("Fly canceled");
                    FlyInput(canceled);
                    break;

            }
        }

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







        //---------------------------END OF INPUT SECTION------------------------








        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDSprint = Animator.StringToHash("Sprint");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDFightingStanceActive = Animator.StringToHash("FightingStanceActive");
            _animIDFly = Animator.StringToHash("Fly");
            _animIDPunch = Animator.StringToHash("Punch");


        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);

            }
        }

        private void CameraRotation()
        {

            if (_input != null)
            {
                // if there is an input and camera position is not fixed
                if (look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    //Don't multiply mouse input by Time.deltaTime;
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                    _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += look.y * deltaTimeMultiplier;
                }
            }
            else { Debugger("****   _input is null   ****"); }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        //these variables store input states
        bool isFlying;
        bool isFightStance;
        bool isPunching;
        bool isSprinting;


        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = (sprint > 0) ? SprintSpeed : MoveSpeed;
            isSprinting = (sprint > 0);
            isFightStance = (fightstance > 0);
            isFlying = (fly > 0);
            isPunching = (punch > 0);

            _animator.SetBool(_animIDFly, isFlying);
            _animator.SetBool(_animIDPunch, isPunching);
            _animator.SetBool(_animIDFightingStanceActive, isFightStance);
            _animator.SetBool(_animIDSprint, isSprinting);


            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = analogMovement ? move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(move.x, 0.0f, move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {

                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);


                MovementSpeed = inputMagnitude;
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                Jumping = (jump > 0); //If the button is either started or performed, Jumping is true, if it is canceled, Jumping is false
                Debugger("jump =" + jump + "    Jumping = " + Jumping);

                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    /*
                    _animator.SetBool(_animIDJump, false);
                    Jumping = false;
                    _animator.SetBool(_animIDFreeFall, false);
                    Freefall = false;
                    */
                    
                    _animator.SetBool(_animIDJump, Jumping);
                    Freefall = false;
                    _animator.SetBool(_animIDFreeFall, Freefall);
                    

                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                

                if (_input != null)
                {
                    if ((jump > 0) && _jumpTimeoutDelta <= 0.0f)
                    {
                        // the square root of H * -2 * G = how much velocity needed to reach desired height
                        _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                        // update animator if using character
                        if (_hasAnimator)
                        {
                            //Jumping = true;
                            _animator.SetBool(_animIDJump, Jumping);


                        }
                    }
                }
                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        //TODO need to check for direction here only set true if falling downward, false if rising
                        Freefall = true;
                        _animator.SetBool(_animIDFreeFall, Freefall);


                    }
                }

                // if we are not grounded, do not jump
                //jump = notstarted;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }





        public bool DEBUGGING_ACTIVE = true;









    }
}