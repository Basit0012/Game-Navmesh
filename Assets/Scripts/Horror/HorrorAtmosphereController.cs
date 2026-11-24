using UnityEngine;

namespace RelicDash.Horror
{
    /// <summary>
    /// Manages the ambient horror soundscape and psychological audio stingers.
    /// Fulfills Phase 4 & Phase 6 AAA Audio & Atmosphere design.
    /// </summary>
    public class HorrorAtmosphereController : MonoBehaviour
    {
        public static HorrorAtmosphereController Instance { get; private set; }

        [Header("Ambient Soundscape")]
        [SerializeField] private AudioSource ambientDroneSource;
        [SerializeField] private AudioClip horrorAmbientDroneClip;
        [Range(0f, 1f)]
        [SerializeField] private float droneBaseVolume = 0.65f;

        [Header("Tension Stinger")]
        [SerializeField] private AudioSource tensionStingerSource;
        [SerializeField] private AudioClip tensionStingerClip;
        [SerializeField] private float stingerCooldown = 25.0f;

        private float _lastStingerTime = -100f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ambient drone source
            if (ambientDroneSource == null)
            {
                ambientDroneSource = gameObject.AddComponent<AudioSource>();
                ambientDroneSource.loop = true;
                ambientDroneSource.playOnAwake = false;
                ambientDroneSource.spatialBlend = 0.0f; // 2D immersion
                ambientDroneSource.volume = droneBaseVolume;
            }

            if (horrorAmbientDroneClip != null)
            {
                ambientDroneSource.clip = horrorAmbientDroneClip;
            }

            // Tension stinger source
            if (tensionStingerSource == null)
            {
                tensionStingerSource = gameObject.AddComponent<AudioSource>();
                tensionStingerSource.loop = false;
                tensionStingerSource.playOnAwake = false;
                tensionStingerSource.spatialBlend = 0.0f;
                tensionStingerSource.volume = 0.85f;
            }

            if (tensionStingerClip != null)
            {
                tensionStingerSource.clip = tensionStingerClip;
            }
        }

        private void Start()
        {
            if (ambientDroneSource != null && ambientDroneSource.clip != null)
            {
                ambientDroneSource.Play();
            }
        }

        public void PlayTensionStinger()
        {
            if (Time.time - _lastStingerTime < stingerCooldown) return;

            if (tensionStingerSource != null && tensionStingerClip != null)
            {
                _lastStingerTime = Time.time;
                tensionStingerSource.PlayOneShot(tensionStingerClip, 0.9f);

                // Briefly duck drone
                StartCoroutine(DuckDroneRoutine(2.5f));
            }
        }

        private System.Collections.IEnumerator DuckDroneRoutine(float duration)
        {
            if (ambientDroneSource == null) yield break;
            ambientDroneSource.volume = droneBaseVolume * 0.35f;
            yield return new WaitForSeconds(duration);
            ambientDroneSource.volume = droneBaseVolume;
        }
    }
}
