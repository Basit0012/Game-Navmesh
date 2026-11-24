using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Continuously patrols between designated waypoints using NavMeshAgent.
    /// Pauses briefly at each waypoint before proceeding to the next.
    /// Returns RUNNING while navigating or waiting.
    /// </summary>
    public class PatrolNode : Node
    {
        private readonly NavMeshAgent _agent;
        private readonly List<Transform> _waypoints;
        private readonly float _patrolSpeed;
        private readonly float _waitTime;

        private int _currentWaypointIndex = 0;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;

        public PatrolNode(NavMeshAgent agent, List<Transform> waypoints, float patrolSpeed, float waitTime)
        {
            _agent = agent;
            _waypoints = waypoints;
            _patrolSpeed = patrolSpeed;
            _waitTime = waitTime;
        }

        public override NodeState Evaluate()
        {
            if (_agent == null || _waypoints == null || _waypoints.Count == 0)
            {
                state = NodeState.FAILURE;
                return state;
            }

            _agent.speed = _patrolSpeed;

            if (_isWaiting)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= _waitTime)
                {
                    _isWaiting = false;
                    _waitTimer = 0f;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
                    SetNextDestination();
                }

                state = NodeState.RUNNING;
                return state;
            }

            // Check if agent arrived at current waypoint
            Transform targetWp = _waypoints[_currentWaypointIndex];
            float distToWp = targetWp != null ? Vector3.Distance(_agent.transform.position, targetWp.position) : float.MaxValue;

            if ((_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.5f) ||
                (!_agent.isOnNavMesh && distToWp <= _agent.stoppingDistance + 0.8f))
            {
                _isWaiting = true;
                _waitTimer = 0f;
                if (_agent.isOnNavMesh) _agent.isStopped = true;

                state = NodeState.RUNNING;
                return state;
            }

            // Move towards waypoint
            if (_agent.isOnNavMesh)
            {
                if (!_agent.hasPath)
                {
                    SetNextDestination();
                }
            }
            else if (targetWp != null)
            {
                Vector3 wpDir = (targetWp.position - _agent.transform.position).normalized;
                wpDir.y = 0;
                if (wpDir.sqrMagnitude > 0.001f)
                {
                    _agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, Quaternion.LookRotation(wpDir), Time.deltaTime * 4.0f);
                    _agent.transform.position += wpDir * (_patrolSpeed * Time.deltaTime);
                }
            }

            state = NodeState.RUNNING;
            return state;
        }

        private void SetNextDestination()
        {
            if (_waypoints.Count == 0) return;

            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            if (targetWaypoint != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(targetWaypoint.position);
            }
        }
    }
}
