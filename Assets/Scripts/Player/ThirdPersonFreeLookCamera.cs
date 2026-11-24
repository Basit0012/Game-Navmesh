using UnityEngine;
using UnityEngine.InputSystem;
using RelicDash.Core;
using RelicDash.Data;

namespace RelicDash.Player
{
    /// <summary>
    /// GTA-style third-person free-look camera orbit pivot controller.
    /// Manages independent mouse orbit around the player character without rotating the player body.
    /// Supports Unity's New Input System, smooth rotation damping, vertical clamping, and cursor locking.
    /// </summary>
    public class ThirdPersonFreeLookCamera : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [Tooltip("The player transform to follow.")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Height offset of the orbit pivot above the player's root.")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Sensitivity & Rotation Limits")]
        [Tooltip("Horizontal mouse look sensitivity.")]
        [SerializeField] private float mouseSensitivityX = 1.8f;

        [Tooltip("Vertical mouse look sensitivity.")]
        [SerializeField] private float mouseSensitivityY = 1.4f;

        [Tooltip("Invert vertical look axis.")]
        [SerializeField] private bool invertY = false;

        [Tooltip("Minimum pitch angle (looking up at player). Prevents ground clipping.")]
        [SerializeField] private float minPitch = -32.0f;

        [Tooltip("Maximum pitch angle (looking down from above player).")]
        [SerializeField] private float maxPitch = 65.0f;

        [Tooltip("Smooth damping speed for rotational orbit interpolation.")]
        [SerializeField] private float rotationDamping = 18.0f;

        [Header("Input & Settings")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private GameSettings settings;

        [Header("Cursor Control")]
        [SerializeField] private bool autoLockCursor = true;

        private InputAction _lookAction;
        private float _targetYaw;
        private float _targetPitch = 10.0f;
        private float _currentYaw;
        private float _currentPitch = 10.0f;
        private bool _isFreeLookActive = true;

        public Transform FollowTarget
        {
            get => followTarget;
            set => followTarget = value;
        }

        public Vector3 PivotOffset
        {
            get => pivotOffset;
            set => pivotOffset = value;
        }

        public float MouseSensitivityX
        {
            get => mouseSensitivityX;
            set => mouseSensitivityX = value;
        }

        public float MouseSensitivityY
        {
            get => mouseSensitivityY;
            set => mouseSensitivityY = value;
        }

        public float CurrentYaw => _currentYaw;
        public float CurrentPitch => _currentPitch;

        private void Awake()
        {
            if (settings != null)
            {
                mouseSensitivityX = settings.CameraSensitivityX;
                mouseSensitivityY = settings.CameraSensitivityY;
                minPitch = settings.CameraMinPitch;
                maxPitch = settings.CameraMaxPitch;
                rotationDamping = settings.CameraDamping;
                invertY = settings.InvertCameraY;
            }

            // Initialize yaw and pitch from current orientation
            Vector3 euler = transform.eulerAngles;
            _targetYaw = euler.y;
            _currentYaw = euler.y;
            _targetPitch = euler.x > 180f ? euler.x - 360f : euler.x;
            _currentPitch = _targetPitch;
        }

        private void Start()
        {
            if (followTarget == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) followTarget = playerObj.transform;
            }

            if (playerInput == null && followTarget != null)
            {
                playerInput = followTarget.GetComponent<PlayerInput>();
            }

            if (playerInput != null && playerInput.actions != null)
            {
                _lookAction = playerInput.actions.FindAction("Look");
            }

            if (autoLockCursor)
            {
                LockCursor();
            }

            // Snap immediately to target position on start
            if (followTarget != null)
            {
                transform.position = followTarget.position + pivotOffset;
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            HandleCursorToggle();
            ProcessMouseLook();
        }

        private void LateUpdate()
        {
            UpdatePivotPositionAndRotation();
        }

        private void HandleCursorToggle()
        {
            // Toggle cursor lock with ESC key
            bool escPressed = false;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }

            if (escPressed)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    UnlockCursor();
                }
                else
                {
                    LockCursor();
                }
            }

            // Auto re-lock when clicking in viewport if unlocked (and not clicking on UI)
            if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    LockCursor();
                }
            }
        }

        private void ProcessMouseLook()
        {
            if (!_isFreeLookActive || Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookDelta = Vector2.zero;

            // 1. Try reading from Input System Action
            if (_lookAction != null)
            {
                lookDelta = _lookAction.ReadValue<Vector2>();
            }

            // 2. Fallback to direct Mouse Delta
            if (lookDelta == Vector2.zero && Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue();
            }

            // Scale delta by sensitivity
            float deltaX = lookDelta.x * mouseSensitivityX * 0.1f;
            float deltaY = (invertY ? 1f : -1f) * lookDelta.y * mouseSensitivityY * 0.1f;

            _targetYaw = Mathf.Repeat(_targetYaw + deltaX, 360f);
            _targetPitch = Mathf.Clamp(_targetPitch + deltaY, minPitch, maxPitch);
        }

        private void UpdatePivotPositionAndRotation()
        {
            if (followTarget == null) return;

            // Follow player position without inheriting player rotation
            transform.position = followTarget.position + pivotOffset;

            // Smoothly damp yaw and pitch towards target angles
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * rotationDamping);
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, Time.deltaTime * rotationDamping);

            // Apply independent world rotation to the pivot
            transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetLookAngles(float yaw, float pitch)
        {
            _targetYaw = yaw;
            _currentYaw = yaw;
            _targetPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            _currentPitch = _targetPitch;
            transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                _isFreeLookActive = true;
                LockCursor();
            }
            else
            {
                // In Cutscene, Victory, or GameOver: disable free look and release cursor
                _isFreeLookActive = false;
                UnlockCursor();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 2.0f);
        }
    }
}
