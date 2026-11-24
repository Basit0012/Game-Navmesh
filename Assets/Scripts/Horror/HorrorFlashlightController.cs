using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicDash.Horror
{
    /// <summary>
    /// Realistic psychological horror flashlight controller.
    /// Features smooth inertia lag matching the player camera, toggle mechanics,
    /// and dynamic horror flicker when near the stalker entity or cursed anomalies.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class HorrorFlashlightController : MonoBehaviour
    {
        [Header("Light Parameters")]
        [SerializeField] private Light flashlightLight;
        [SerializeField] private float baseIntensity = 2.4f;
        [SerializeField] private float spotAngle = 52.0f;
        [SerializeField] private float innerSpotAngle = 26.0f;
        [SerializeField] private float range = 24.0f;
        [SerializeField] private Color lightColor = new Color(0.96f, 0.94f, 0.88f);

        [Header("Camera Following Lag & Inertia")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private float followSpeed = 12.0f;
        [SerializeField] private Vector3 offset = new Vector3(0.25f, -0.2f, 0.35f);

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip toggleClickClip;

        [Header("Flicker & Anomaly Behavior")]
        [SerializeField] private bool allowFlicker = true;
        [SerializeField] private float minFlickerInterval = 6.0f;
        [SerializeField] private float maxFlickerInterval = 18.0f;

        private bool _isOn = true;
        private float _nextFlickerTime;
        private bool _isFlickering = false;
        private float _flickerDuration = 0f;
        private float _flickerTimer = 0f;
        private float _externalFlickerMultiplier = 1.0f;

        public bool IsOn => _isOn;

        private void Awake()
        {
            if (flashlightLight == null)
            {
                flashlightLight = GetComponent<Light>();
            }

            flashlightLight.type = LightType.Spot;
            flashlightLight.spotAngle = spotAngle;
            flashlightLight.innerSpotAngle = innerSpotAngle;
            flashlightLight.range = range;
            flashlightLight.color = lightColor;
            flashlightLight.intensity = baseIntensity;
            flashlightLight.shadows = LightShadows.Soft;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.0f; // 2D crisp sound
                }
            }

            ScheduleNextFlicker();
        }

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
        }

        private void Update()
        {
            HandleLagFollow();
            HandleInputToggle();
            HandleFlickerLogic();
        }

        private void HandleLagFollow()
        {
            if (targetCamera == null)
            {
                if (Camera.main != null) targetCamera = Camera.main.transform;
                else return;
            }

            Vector3 targetPos = targetCamera.position +
                                targetCamera.right * offset.x +
                                targetCamera.up * offset.y +
                                targetCamera.forward * offset.z;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetCamera.rotation, Time.deltaTime * followSpeed);
        }

        private void HandleInputToggle()
        {
            // Support 'F' key via New Input System Keyboard or Legacy fallback
            bool toggleRequested = false;
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                toggleRequested = true;
            }

            if (toggleRequested)
            {
                ToggleFlashlight();
            }
        }

        public void ToggleFlashlight()
        {
            _isOn = !_isOn;
            flashlightLight.enabled = _isOn;

            if (audioSource != null && toggleClickClip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(toggleClickClip, 0.8f);
            }
        }

        private void HandleFlickerLogic()
        {
            if (!_isOn || !allowFlicker) return;

            if (!_isFlickering)
            {
                if (Time.time >= _nextFlickerTime)
                {
                    TriggerFlicker(Random.Range(0.4f, 1.2f));
                }
            }
            else
            {
                _flickerTimer += Time.deltaTime;
                if (_flickerTimer < _flickerDuration)
                {
                    // Erratic rapid strobing
                    float noise = Mathf.PerlinNoise(Time.time * 45.0f, 0.0f);
                    flashlightLight.intensity = (noise > 0.35f ? baseIntensity : baseIntensity * 0.15f) * _externalFlickerMultiplier;
                }
                else
                {
                    _isFlickering = false;
                    flashlightLight.intensity = baseIntensity * _externalFlickerMultiplier;
                    ScheduleNextFlicker();
                }
            }
        }

        public void SetExternalFlickerIntensity(float intensityMultiplier, bool forceStrobe = false)
        {
            _externalFlickerMultiplier = Mathf.Clamp(intensityMultiplier, 0.1f, 1.0f);
            if (forceStrobe && !_isFlickering)
            {
                TriggerFlicker(0.8f);
            }
        }

        public void TriggerFlicker(float duration)
        {
            _isFlickering = true;
            _flickerDuration = duration;
            _flickerTimer = 0f;
        }

        private void ScheduleNextFlicker()
        {
            _nextFlickerTime = Time.time + Random.Range(minFlickerInterval, maxFlickerInterval);
        }
    }
}
