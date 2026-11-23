using UnityEngine;
using RelicDash.Core;

namespace RelicDash.Environment
{
    /// <summary>
    /// Ancient stone exit gate located on the floating island.
    /// Can be opened via Unity Timeline animation or script trigger once all relics are collected.
    /// Contains an ancient energy barrier and exit trigger zone.
    /// </summary>
    public class ExitGate : MonoBehaviour
    {
        [Header("Gate Components")]
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private GameObject portalVortexEffect;
        [SerializeField] private Light gateGlowLight;
        [SerializeField] private AudioSource gateAudio;

        [Header("Animation Settings")]
        [SerializeField] private Vector3 leftDoorOpenOffset = new Vector3(-3.5f, 0, 0);
        [SerializeField] private Vector3 rightDoorOpenOffset = new Vector3(3.5f, 0, 0);
        [SerializeField] private float openDuration = 3.5f;

        [Header("State")]
        [SerializeField] private bool isOpen = false;

        private Vector3 _leftDoorClosedPos;
        private Vector3 _rightDoorClosedPos;
        private float _openProgress = 0f;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (leftDoor != null) _leftDoorClosedPos = leftDoor.localPosition;
            if (rightDoor != null) _rightDoorClosedPos = rightDoor.localPosition;

            if (portalVortexEffect != null) portalVortexEffect.SetActive(false);
            if (gateGlowLight != null) gateGlowLight.intensity = 0.5f;
        }

        private void Update()
        {
            if (isOpen && _openProgress < 1.0f)
            {
                _openProgress += Time.deltaTime / openDuration;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_openProgress));

                if (leftDoor != null)
                {
                    leftDoor.localPosition = Vector3.Lerp(_leftDoorClosedPos, _leftDoorClosedPos + leftDoorOpenOffset, t);
                }

                if (rightDoor != null)
                {
                    rightDoor.localPosition = Vector3.Lerp(_rightDoorClosedPos, _rightDoorClosedPos + rightDoorOpenOffset, t);
                }

                if (gateGlowLight != null)
                {
                    gateGlowLight.intensity = Mathf.Lerp(0.5f, 4.0f, t);
                }
            }
        }

        /// <summary>
        /// Unseals the ancient gate doors and powers up the mystic portal vortex.
        /// </summary>
        public void OpenGate()
        {
            if (isOpen) return;

            isOpen = true;
            _openProgress = 0f;

            if (portalVortexEffect != null)
            {
                portalVortexEffect.SetActive(true);
            }

            if (gateAudio != null)
            {
                gateAudio.Play();
            }

            Debug.Log("[ExitGate] Ancient exit gate unsealed! Energy portal active.");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isOpen && other.CompareTag("Player"))
            {
                Debug.Log("[ExitGate] Player entered unsealed portal! Victory achieved.");
                GameEvents.TriggerVictoryAchieved();
            }
        }
    }
}
