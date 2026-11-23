using UnityEngine;

namespace RelicDash.Data
{
    /// <summary>
    /// Configurable gameplay parameters stored as a ScriptableObject asset.
    /// Allows tuning movement, enemy AI, relic counts, and terrain parameters without recompiling.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "RelicDash/Game Settings", order = 1)]
    public class GameSettings : ScriptableObject
    {
        [Header("Player Movement Settings")]
        [Tooltip("Standard running speed in units per second.")]
        [SerializeField] private float moveSpeed = 6.0f;

        [Tooltip("Sprint multiplier or sprint speed.")]
        [SerializeField] private float sprintSpeed = 9.5f;

        [Tooltip("Rotation smoothness speed.")]
        [SerializeField] private float rotationSpeed = 12.0f;

        [Tooltip("Upward jump impulse force.")]
        [SerializeField] private float jumpForce = 6.5f;

        [Tooltip("Gravity acceleration magnitude applied to the character.")]
        [SerializeField] private float gravityModifier = 18.0f;

        [Header("Relic & Objective Settings")]
        [Tooltip("Target number of ancient energy relics required to activate the exit gate.")]
        [SerializeField] private int targetRelicCount = 3;

        [Tooltip("Floating bob amplitude for relic orbs.")]
        [SerializeField] private float relicBobHeight = 0.35f;

        [Tooltip("Oscillation frequency for relic bobbing.")]
        [SerializeField] private float relicBobSpeed = 2.0f;

        [Tooltip("Rotational spin speed around Y axis in degrees per second.")]
        [SerializeField] private float relicSpinSpeed = 45.0f;

        [Header("Enemy Guardian AI Settings")]
        [Tooltip("Player detection radius in meters as required by examination specs (8m).")]
        [SerializeField] private float guardianDetectionRadius = 8.0f;

        [Tooltip("Patrol movement speed.")]
        [SerializeField] private float guardianPatrolSpeed = 3.2f;

        [Tooltip("Chase movement speed when player is within detection radius.")]
        [SerializeField] private float guardianChaseSpeed = 5.2f;

        [Tooltip("Distance at which the guardian catches the player and triggers GameOver.")]
        [SerializeField] private float guardianCatchDistance = 1.4f;

        [Tooltip("Pause duration at each patrol waypoint in seconds.")]
        [SerializeField] private float waypointWaitTime = 1.5f;

        [Header("Animation Rigging Settings")]
        [Tooltip("Interpolation speed for aiming the head/visor toward relics.")]
        [SerializeField] private float rigWeightLerpSpeed = 4.0f;

        [Tooltip("Maximum distance from player to relic to engage head-aim constraint.")]
        [SerializeField] private float maxRelicNoticeDistance = 16.0f;

        [Header("Procedural Terrain Settings")]
        [Tooltip("Terrain width and length in world units.")]
        [SerializeField] private int terrainResolution = 128;

        [Tooltip("Terrain height scale.")]
        [SerializeField] private float terrainHeightScale = 14.0f;

        [Tooltip("Perlin noise frequency scale.")]
        [SerializeField] private float perlinFrequency = 0.045f;

        [Tooltip("Falloff exponent to sculpt the floating island edges.")]
        [SerializeField] private float islandFalloffPower = 2.5f;

        // Public Accessors
        public float MoveSpeed => moveSpeed;
        public float SprintSpeed => sprintSpeed;
        public float RotationSpeed => rotationSpeed;
        public float JumpForce => jumpForce;
        public float GravityModifier => gravityModifier;

        public int TargetRelicCount => targetRelicCount;
        public float RelicBobHeight => relicBobHeight;
        public float RelicBobSpeed => relicBobSpeed;
        public float RelicSpinSpeed => relicSpinSpeed;

        public float GuardianDetectionRadius => guardianDetectionRadius;
        public float GuardianPatrolSpeed => guardianPatrolSpeed;
        public float GuardianChaseSpeed => guardianChaseSpeed;
        public float GuardianCatchDistance => guardianCatchDistance;
        public float WaypointWaitTime => waypointWaitTime;

        public float RigWeightLerpSpeed => rigWeightLerpSpeed;
        public float MaxRelicNoticeDistance => maxRelicNoticeDistance;

        public int TerrainResolution => terrainResolution;
        public float TerrainHeightScale => terrainHeightScale;
        public float PerlinFrequency => perlinFrequency;
        public float IslandFalloffPower => islandFalloffPower;
    }
}
