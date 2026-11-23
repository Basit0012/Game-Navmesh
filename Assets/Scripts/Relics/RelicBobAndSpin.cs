using UnityEngine;
using RelicDash.Data;

namespace RelicDash.Relics
{
    /// <summary>
    /// Smoothly levitates and spins ancient relic orbs in 3D space with harmonic oscillation.
    /// </summary>
    public class RelicBobAndSpin : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings settings;

        [Header("Motion Parameters")]
        [SerializeField] private float bobHeight = 0.35f;
        [SerializeField] private float bobSpeed = 2.0f;
        [SerializeField] private float spinSpeed = 45.0f;
        [SerializeField] private Vector3 spinAxis = Vector3.up;

        private Vector3 _initialLocalPosition;

        private void Start()
        {
            _initialLocalPosition = transform.localPosition;

            if (settings != null)
            {
                bobHeight = settings.RelicBobHeight;
                bobSpeed = settings.RelicBobSpeed;
                spinSpeed = settings.RelicSpinSpeed;
            }
        }

        private void Update()
        {
            // Sinusoidal levitation
            float newY = _initialLocalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = new Vector3(_initialLocalPosition.x, newY, _initialLocalPosition.z);

            // Rotational spin
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
