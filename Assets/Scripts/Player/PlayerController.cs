using UnityEngine;
using UnityEngine.InputSystem;
using RelicDash.Core;
using RelicDash.Data;

namespace RelicDash.Player
{
    /// <summary>
    /// Third-person player controller utilizing Unity's New Input System and CharacterController.
    /// Supports camera-relative movement, smooth model rotation, sprinting, and jumping.
    /// Responds to GameState transitions by disabling inputs during cutscenes and game over.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings settings;

        [Header("Visual Model & Rig")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        [Header("Ground Check & Gravity")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Audio & Effects")]
        [SerializeField] private AudioSource footstepAudio;
        [SerializeField] private AudioSource jumpAudio;
        [SerializeField] private AudioSource landAudio;
        [SerializeField] private ParticleSystem movementDustParticles;

        private CharacterController _characterController;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private Vector2 _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isSprinting;
        private bool _controlsEnabled = true;

        // Animator parameter hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerInput = GetComponent<PlayerInput>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (_playerInput != null && _playerInput.actions != null)
            {
                _moveAction = _playerInput.actions.FindAction("Move");
                _jumpAction = _playerInput.actions.FindAction("Jump");
                _sprintAction = _playerInput.actions.FindAction("Sprint");
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;

            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpPerformed;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += ctx => _isSprinting = true;
                _sprintAction.canceled += ctx => _isSprinting = false;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;

            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
            }
        }

        private void Update()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            CheckGroundedStatus();

            if (_controlsEnabled)
            {
                ProcessMovement();
            }
            else
            {
                // Apply idle damping when inputs are locked
                _velocity.x = 0;
                _velocity.z = 0;
            }

            ApplyGravity();
            _characterController.Move(_velocity * Time.deltaTime);

            UpdateAnimation();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!_controlsEnabled || !_isGrounded) return;

            float jumpForce = settings != null ? settings.JumpForce : 6.5f;
            float gravity = settings != null ? settings.GravityModifier : 18.0f;

            // Vertical velocity formula: v = sqrt(2 * h * g)
            _velocity.y = Mathf.Sqrt(2f * jumpForce * gravity);

            if (animator != null)
            {
                animator.SetTrigger(JumpTriggerHash);
            }

            if (jumpAudio != null)
            {
                jumpAudio.Play();
            }
        }

        private void CheckGroundedStatus()
        {
            bool wasGrounded = _isGrounded;

            if (groundCheck != null)
            {
                _isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
            }
            else
            {
                _isGrounded = _characterController.isGrounded;
            }

            if (!wasGrounded && _isGrounded && _velocity.y < -3.0f)
            {
                if (landAudio != null)
                {
                    landAudio.Play();
                }
            }

            if (_isGrounded && _velocity.y < 0)
            {
                // Small downward force to stay grounded on slopes
                _velocity.y = -2.0f;
            }
        }

        private void ProcessMovement()
        {
            if (cameraTransform == null) return;

            // Calculate camera-relative forward and right vectors projected onto XZ plane
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 targetDir = (camForward * _moveInput.y + camRight * _moveInput.x);
            float inputMagnitude = Mathf.Clamp01(targetDir.magnitude);

            float speed = settings != null 
                ? (_isSprinting ? settings.SprintSpeed : settings.MoveSpeed) 
                : (_isSprinting ? 9.5f : 6.0f);

            float rotSpeed = settings != null ? settings.RotationSpeed : 12.0f;

            if (inputMagnitude > 0.05f)
            {
                targetDir.Normalize();

                // Smooth model rotation facing movement direction
                Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
                Transform rotTransform = modelRoot != null ? modelRoot : transform;
                rotTransform.rotation = Quaternion.Slerp(rotTransform.rotation, targetRotation, rotSpeed * Time.deltaTime);

                // Horizontal movement
                Vector3 moveDelta = targetDir * (speed * inputMagnitude);
                _velocity.x = moveDelta.x;
                _velocity.z = moveDelta.z;

                if (movementDustParticles != null && !movementDustParticles.isPlaying && _isGrounded)
                {
                    movementDustParticles.Play();
                }

                if (footstepAudio != null && _isGrounded)
                {
                    if (!footstepAudio.isPlaying) footstepAudio.Play();
                }
            }
            else
            {
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0, speed * 4f * Time.deltaTime);
                _velocity.z = Mathf.MoveTowards(_velocity.z, 0, speed * 4f * Time.deltaTime);

                if (movementDustParticles != null && movementDustParticles.isPlaying)
                {
                    movementDustParticles.Stop();
                }

                if (footstepAudio != null && footstepAudio.isPlaying)
                {
                    footstepAudio.Pause();
                }
            }
        }

        private void ApplyGravity()
        {
            float gravity = settings != null ? settings.GravityModifier : 18.0f;
            _velocity.y -= gravity * Time.deltaTime;
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            float currentSpeed = new Vector2(_velocity.x, _velocity.z).magnitude;
            animator.SetFloat(SpeedHash, currentSpeed);
            animator.SetBool(IsGroundedHash, _isGrounded);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            _controlsEnabled = (newState == GameState.Playing);

            if (!_controlsEnabled)
            {
                _moveInput = Vector2.zero;
                _velocity.x = 0;
                _velocity.z = 0;
                if (footstepAudio != null && footstepAudio.isPlaying) footstepAudio.Stop();
            }
        }
    }
}
