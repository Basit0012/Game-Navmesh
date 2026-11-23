using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RelicDash.AI.BehaviorTree;
using RelicDash.Core;
using RelicDash.Data;

namespace RelicDash.AI
{
    /// <summary>
    /// Controller for the autonomous island guardian.
    /// Manages NavMesh navigation and evaluates the modular Behavior Tree.
    /// Fulfills examination requirements #10 and #11.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyBTController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings settings;

        [Header("Target & Waypoints")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Header("Guardian AI Parameters")]
        [Tooltip("Player detection radius in meters (8m per exam requirements).")]
        [SerializeField] private float detectionRadius = 8.0f;

        [Tooltip("Chase speed when pursuing the player.")]
        [SerializeField] private float chaseSpeed = 5.2f;

        [Tooltip("Normal patrol speed between waypoints.")]
        [SerializeField] private float patrolSpeed = 3.2f;

        [Tooltip("Distance at which the guardian catches the player.")]
        [SerializeField] private float catchDistance = 1.4f;

        [Tooltip("Pursuit lose distance before returning to patrol.")]
        [SerializeField] private float losePursuitRadius = 11.0f;

        [Tooltip("Waypoint wait duration in seconds.")]
        [SerializeField] private float waypointWaitTime = 1.5f;

        [Header("Visual Feedback & Intimidation")]
        [SerializeField] private Light eyeVisorLight;
        [SerializeField] private Color patrolEyeColor = new Color(0.8f, 0f, 1f); // Menacing purple
        [SerializeField] private Color chaseEyeColor = new Color(1f, 0.1f, 0f);  // Hostile red
        [SerializeField] private ParticleSystem auraParticles;
        [SerializeField] private AudioSource chaseAlarmAudio;

        [Header("Debug")]
        [SerializeField] private string currentBehaviorState = "Idle";
        [SerializeField] private bool showGizmos = true;

        private NavMeshAgent _agent;
        private Node _rootNode;
        private bool _isAiActive = true;
        private bool _wasChasing = false;

        public NavMeshAgent Agent => _agent;
        public float DetectionRadius => detectionRadius;
        public List<Transform> Waypoints => waypoints;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (settings != null)
            {
                detectionRadius = settings.GuardianDetectionRadius;
                chaseSpeed = settings.GuardianChaseSpeed;
                patrolSpeed = settings.GuardianPatrolSpeed;
                catchDistance = settings.GuardianCatchDistance;
                waypointWaitTime = settings.WaypointWaitTime;
            }

            if (eyeVisorLight != null)
            {
                eyeVisorLight.color = patrolEyeColor;
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
            }

            BuildBehaviorTree();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        /// <summary>
        /// Assembles the modular Behavior Tree with Selector and Sequence nodes.
        /// Tree Hierarchy:
        /// - Root (Selector)
        ///     ├── Catch Sequence (Sequence)
        ///     │   ├── DetectPlayerNode (catch radius)
        ///     │   └── CatchPlayerNode (GameOver trigger)
        ///     ├── Chase Sequence (Sequence)
        ///     │   ├── DetectPlayerNode (8m detection radius)
        ///     │   └── ChasePlayerNode (navigates toward player)
        ///     └── Patrol Sequence / Node (continuous waypoint route)
        /// </summary>
        public void BuildBehaviorTree()
        {
            // 1. Catch Sequence
            SequenceNode catchSequence = new SequenceNode();
            catchSequence.AddChild(new DetectPlayerNode(transform, playerTransform, catchDistance));
            catchSequence.AddChild(new CatchPlayerNode());

            // 2. Chase Sequence
            SequenceNode chaseSequence = new SequenceNode();
            chaseSequence.AddChild(new DetectPlayerNode(transform, playerTransform, detectionRadius));
            chaseSequence.AddChild(new ChasePlayerNode(_agent, transform, playerTransform, chaseSpeed, losePursuitRadius, catchDistance));

            // 3. Patrol Node
            PatrolNode patrolNode = new PatrolNode(_agent, waypoints, patrolSpeed, waypointWaitTime);

            // Root Selector
            SelectorNode rootSelector = new SelectorNode();
            rootSelector.AddChild(catchSequence);
            rootSelector.AddChild(chaseSequence);
            rootSelector.AddChild(patrolNode);

            _rootNode = rootSelector;
            Debug.Log("[EnemyBTController] Modular Behavior Tree assembled successfully.");
        }

        private void Update()
        {
            if (!_isAiActive || _rootNode == null) return;

            // Tick Behavior Tree
            NodeState treeState = _rootNode.Evaluate();

            UpdateVisualFeedback();
        }

        private void UpdateVisualFeedback()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool isChasing = distToPlayer <= detectionRadius;

            if (isChasing && !_wasChasing)
            {
                // Engaged chase
                _wasChasing = true;
                currentBehaviorState = "CHASING PLAYER";
                if (eyeVisorLight != null) eyeVisorLight.color = chaseEyeColor;
                if (chaseAlarmAudio != null && !chaseAlarmAudio.isPlaying) chaseAlarmAudio.Play();
            }
            else if (!isChasing && _wasChasing)
            {
                // Returned to patrol
                _wasChasing = false;
                currentBehaviorState = "PATROLLING WAYPOINTS";
                if (eyeVisorLight != null) eyeVisorLight.color = patrolEyeColor;
                if (chaseAlarmAudio != null && chaseAlarmAudio.isPlaying) chaseAlarmAudio.Stop();
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            _isAiActive = (newState == GameState.Playing);

            if (!_isAiActive && _agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }

        public void SetWaypoints(List<Transform> newWaypoints)
        {
            waypoints = newWaypoints;
            BuildBehaviorTree();
        }

        public void SetPlayer(Transform player)
        {
            playerTransform = player;
            BuildBehaviorTree();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // 8m Detection Radius Gizmo
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Catch Radius Gizmo
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, catchDistance);

            // Patrol path gizmos
            if (waypoints != null && waypoints.Count > 1)
            {
                Gizmos.color = Color.magenta;
                for (int i = 0; i < waypoints.Count; i++)
                {
                    Transform current = waypoints[i];
                    Transform next = waypoints[(i + 1) % waypoints.Count];
                    if (current != null && next != null)
                    {
                        Gizmos.DrawLine(current.position, next.position);
                        Gizmos.DrawSphere(current.position, 0.4f);
                    }
                }
            }
        }
    }
}
