using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using RelicDash.Data;
using RelicDash.Relics;

namespace RelicDash.Player
{
    /// <summary>
    /// Configures Animation Rigging Multi-Aim Constraint to dynamically direct the player's
    /// head or visor toward nearby active ancient energy relics.
    /// Fulfills examination requirement #5.
    /// </summary>
    public class RelicLookAtTarget : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings settings;

        [Header("Rig Components")]
        [Tooltip("The Multi-Aim Constraint component controlling the head/visor bone.")]
        [SerializeField] private MultiAimConstraint multiAimConstraint;

        [Tooltip("Transform used as the aim target source for the constraint.")]
        [SerializeField] private Transform aimTarget;

        [Header("Detection Parameters")]
        [Tooltip("Maximum distance to look towards an ancient relic.")]
        [SerializeField] private float maxDetectionDistance = 16.0f;

        [Tooltip("Weight transition speed.")]
        [SerializeField] private float blendSpeed = 4.0f;

        [Header("Debug Visualization")]
        [SerializeField] private bool drawDebugGizmos = true;

        private Transform _currentRelicTarget;
        private float _targetWeight = 0f;

        private void Start()
        {
            if (settings != null)
            {
                maxDetectionDistance = settings.MaxRelicNoticeDistance;
                blendSpeed = settings.RigWeightLerpSpeed;
            }

            if (aimTarget == null)
            {
                GameObject targetObj = new GameObject("PlayerRig_AimTarget");
                aimTarget = targetObj.transform;
                aimTarget.position = transform.position + transform.forward * 5f + Vector3.up * 1.5f;
            }
        }

        private void Update()
        {
            FindClosestActiveRelic();

            if (_currentRelicTarget != null)
            {
                // Smoothly track target position
                aimTarget.position = Vector3.Lerp(aimTarget.position, _currentRelicTarget.position, 8.0f * Time.deltaTime);
                _targetWeight = 1.0f;
            }
            else
            {
                // Default target straight ahead
                Vector3 defaultTargetPos = transform.position + transform.forward * 6.0f + Vector3.up * 1.5f;
                aimTarget.position = Vector3.Lerp(aimTarget.position, defaultTargetPos, 4.0f * Time.deltaTime);
                _targetWeight = 0.0f;
            }

            if (multiAimConstraint != null)
            {
                multiAimConstraint.weight = Mathf.MoveTowards(multiAimConstraint.weight, _targetWeight, blendSpeed * Time.deltaTime);
            }
        }

        private void FindClosestActiveRelic()
        {
            IReadOnlyList<RelicItem> activeRelics = RelicItem.ActiveRelics;
            if (activeRelics == null || activeRelics.Count == 0)
            {
                _currentRelicTarget = null;
                return;
            }

            RelicItem closestRelic = null;
            float closestDistSq = maxDetectionDistance * maxDetectionDistance;
            Vector3 playerPos = transform.position;

            for (int i = 0; i < activeRelics.Count; i++)
            {
                RelicItem relic = activeRelics[i];
                if (relic == null || !relic.gameObject.activeInHierarchy || relic.IsCollected) continue;

                float distSq = (relic.transform.position - playerPos).sqrMagnitude;
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closestRelic = relic;
                }
            }

            _currentRelicTarget = closestRelic != null ? closestRelic.transform : null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxDetectionDistance);

            if (_currentRelicTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, _currentRelicTarget.position);
            }
        }
    }
}
