using System.Collections.Generic;
using UnityEngine;
using RelicDash.Core;
using RelicDash.Data;

namespace RelicDash.Relics
{
    /// <summary>
    /// Ancient energy relic collectible.
    /// Features pulsing emission shader, particle systems for glow and sparks,
    /// and collision-based collection publishing to the Observer Pattern event bus.
    /// Fulfills examination requirement #2 and #9.
    /// </summary>
    public class RelicItem : MonoBehaviour
    {
        private static readonly List<RelicItem> s_ActiveRelics = new List<RelicItem>();
        public static IReadOnlyList<RelicItem> ActiveRelics => s_ActiveRelics;

        [Header("Configuration")]
        [SerializeField] private GameSettings settings;
        [SerializeField] private int relicId = 1;

        [Header("Visual Effects & Feedback")]
        [SerializeField] private ParticleSystem ambientSparkParticles;
        [SerializeField] private ParticleSystem collectionBurstParticles;
        [SerializeField] private Light auraLight;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Renderer meshRenderer;

        [Header("Pulsing Shader Properties")]
        [SerializeField] private Material relicMaterial;
        [ColorUsage(true, true)]
        [SerializeField] private Color emissionColor = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private float pulseSpeed = 3.0f;
        [SerializeField] private float minEmissionIntensity = 1.5f;
        [SerializeField] private float maxEmissionIntensity = 5.0f;

        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private bool _isCollected = false;
        public bool IsCollected => _isCollected;
        public int RelicId => relicId;

        private void OnEnable()
        {
            if (!s_ActiveRelics.Contains(this))
            {
                s_ActiveRelics.Add(this);
            }
        }

        private void OnDisable()
        {
            s_ActiveRelics.Remove(this);
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();

            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            }

            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                relicMaterial = meshRenderer.material; // instantiate instance
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            // Runtime pulsing emission feedback
            UpdatePulsingEmission();
        }

        private void UpdatePulsingEmission()
        {
            if (meshRenderer == null) return;

            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);
            float currentIntensity = Mathf.Lerp(minEmissionIntensity, maxEmissionIntensity, pulse);
            Color dynamicEmission = emissionColor * Mathf.LinearToGammaSpace(currentIntensity);

            meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorId, dynamicEmission);
            meshRenderer.SetPropertyBlock(_propBlock);

            if (auraLight != null)
            {
                auraLight.intensity = currentIntensity * 0.8f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                Collect();
            }
        }

        /// <summary>
        /// Handles collision collection, audio-visual feedback, and dispatches Observer pattern event.
        /// </summary>
        public void Collect()
        {
            if (_isCollected) return;

            _isCollected = true;
            s_ActiveRelics.Remove(this);

            Debug.Log($"[RelicItem] Ancient Relic #{relicId} acquired!");

            // Trigger particle burst
            if (collectionBurstParticles != null)
            {
                collectionBurstParticles.transform.parent = null;
                collectionBurstParticles.Play();
                Destroy(collectionBurstParticles.gameObject, 2.5f);
            }

            // Play pickup sound
            if (audioSource != null && audioSource.clip != null)
            {
                AudioSource.PlayClipAtPoint(audioSource.clip, transform.position, 1.0f);
            }

            // Observer pattern dispatch - decoupled notification
            GameEvents.TriggerRelicCollected(gameObject);

            // Hide mesh and collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (meshRenderer != null) meshRenderer.enabled = false;
            if (ambientSparkParticles != null) ambientSparkParticles.Stop();
            if (auraLight != null) auraLight.enabled = false;

            // Destroy relic object after brief delay
            Destroy(gameObject, 0.5f);
        }
    }
}
