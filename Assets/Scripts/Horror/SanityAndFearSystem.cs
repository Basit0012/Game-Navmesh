using UnityEngine;
using RelicDash.Core;

namespace RelicDash.Horror
{
    /// <summary>
    /// Core psychological horror fear and sanity system.
    /// Monitors player proximity to the stalker entity, darkness, and anomaly shards.
    /// Modulates heartbeat audio (rate & volume), camera breathing/FOV pulse,
    /// and chromatic aberration/vignette intensity.
    /// </summary>
    public class SanityAndFearSystem : MonoBehaviour
    {
        public static SanityAndFearSystem Instance { get; private set; }

        [Header("Fear Parameters")]
        [Range(0f, 1f)]
        [SerializeField] private float currentFear = 0.0f;
        [SerializeField] private float fearRecoverySpeed = 0.08f;
        [SerializeField] private float stalkerFearBuildRate = 0.35f;
        [SerializeField] private float darknessFearBuildRate = 0.04f;

        [Header("Proximity Thresholds")]
        [SerializeField] private float stalkerWarningDistance = 14.0f;
        [SerializeField] private float stalkerPanicDistance = 6.0f;

        [Header("Audio Components")]
        [SerializeField] private AudioSource heartbeatAudioSource;
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Camera & Visual Feedback")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float baseFOV = 60.0f;
        [SerializeField] private float maxFOVPulse = 4.0f;

        [Header("External References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform stalkerTransform;
        [SerializeField] private HorrorFlashlightController flashlight;

        private float _breathingTimer = 0f;

        public float CurrentFear => currentFear;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (heartbeatAudioSource == null)
            {
                heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
                heartbeatAudioSource.playOnAwake = false;
                heartbeatAudioSource.loop = true;
                heartbeatAudioSource.spatialBlend = 0.0f;
                heartbeatAudioSource.volume = 0.0f;
            }

            if (heartbeatClip != null)
            {
                heartbeatAudioSource.clip = heartbeatClip;
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            if (stalkerTransform == null)
            {
                var enemy = GameObject.FindGameObjectWithTag("Enemy");
                if (enemy != null) stalkerTransform = enemy.transform;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera != null)
            {
                baseFOV = playerCamera.fieldOfView;
            }

            if (heartbeatAudioSource != null && heartbeatAudioSource.clip != null)
            {
                heartbeatAudioSource.Play();
            }
        }

        private void Update()
        {
            EvaluateFearSources();
            UpdateHeartbeatAudio();
            UpdateCameraBreathing();
        }

        private void EvaluateFearSources()
        {
            bool inDanger = false;

            // 1. Proximity to stalker
            if (playerTransform != null && stalkerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, stalkerTransform.position);
                if (dist < stalkerWarningDistance)
                {
                    inDanger = true;
                    float proximityFactor = 1.0f - Mathf.Clamp01((dist - stalkerPanicDistance) / (stalkerWarningDistance - stalkerPanicDistance));
                    currentFear += stalkerFearBuildRate * proximityFactor * Time.deltaTime;

                    // Pass flicker warning to flashlight
                    if (flashlight != null)
                    {
                        float flickerMult = Mathf.Lerp(1.0f, 0.3f, proximityFactor);
                        flashlight.SetExternalFlickerIntensity(flickerMult, proximityFactor > 0.6f);
                    }
                }
            }

            // 2. Darkness penalty
            if (flashlight != null && !flashlight.IsOn)
            {
                currentFear += darknessFearBuildRate * Time.deltaTime;
            }

            // Recovery when safe
            if (!inDanger)
            {
                currentFear -= fearRecoverySpeed * Time.deltaTime;
                if (flashlight != null)
                {
                    flashlight.SetExternalFlickerIntensity(1.0f, false);
                }
            }

            currentFear = Mathf.Clamp01(currentFear);
        }

        private void UpdateHeartbeatAudio()
        {
            if (heartbeatAudioSource == null) return;

            if (currentFear > 0.08f)
            {
                // Heartbeat audible: volume scales from 0.1 to 0.95
                float targetVol = Mathf.Lerp(0.1f, 0.95f, (currentFear - 0.08f) / 0.92f);
                heartbeatAudioSource.volume = Mathf.Lerp(heartbeatAudioSource.volume, targetVol, Time.deltaTime * 3.0f);

                // Heart rate pitch accelerates from 1.0x to 1.65x
                float targetPitch = Mathf.Lerp(1.0f, 1.65f, currentFear);
                heartbeatAudioSource.pitch = Mathf.Lerp(heartbeatAudioSource.pitch, targetPitch, Time.deltaTime * 2.5f);
            }
            else
            {
                heartbeatAudioSource.volume = Mathf.Lerp(heartbeatAudioSource.volume, 0.0f, Time.deltaTime * 2.0f);
            }
        }

        private void UpdateCameraBreathing()
        {
            if (playerCamera == null) return;

            if (currentFear > 0.1f)
            {
                float speed = Mathf.Lerp(1.5f, 5.0f, currentFear);
                _breathingTimer += Time.deltaTime * speed;
                float fovOffset = Mathf.Sin(_breathingTimer) * (maxFOVPulse * currentFear);
                playerCamera.fieldOfView = baseFOV + fovOffset;
            }
            else
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, baseFOV, Time.deltaTime * 2.0f);
            }
        }

        public void SpikeFear(float amount)
        {
            currentFear = Mathf.Clamp01(currentFear + amount);
        }
    }
}
