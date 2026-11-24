using UnityEngine;

namespace RelicDash.Horror
{
    /// <summary>
    /// Generates subtle environmental psychological anomalies in the Hollow Loop.
    /// Displays vanishing distant silhouettes, flickering cursed braziers,
    /// and atmospheric anomalies to build tension without cheap reliance on jumpscares.
    /// </summary>
    public class LoopAnomalyController : MonoBehaviour
    {
        [Header("Vanishing Apparition")]
        [SerializeField] private GameObject shadowApparition;
        [SerializeField] private float apparitionVisibleDistance = 25.0f;
        [SerializeField] private float apparitionVanishDistance = 12.0f;

        [Header("Target Tracking")]
        [SerializeField] private Transform playerTransform;

        private bool _hasVanished = false;

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (_hasVanished || shadowApparition == null || playerTransform == null) return;

            float dist = Vector3.Distance(playerTransform.position, shadowApparition.transform.position);

            if (dist < apparitionVanishDistance)
            {
                // Vanish into the mist!
                _hasVanished = true;
                if (HorrorAtmosphereController.Instance != null)
                {
                    HorrorAtmosphereController.Instance.PlayTensionStinger();
                }
                Destroy(shadowApparition, 0.3f);
            }
            else if (dist < apparitionVisibleDistance)
            {
                if (!shadowApparition.activeSelf)
                {
                    shadowApparition.SetActive(true);
                }
            }
        }
    }
}
