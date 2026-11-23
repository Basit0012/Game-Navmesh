using UnityEngine;
using UnityEngine.AI;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Chases the detected player using NavMeshAgent.
    /// Returns RUNNING while pursuing, SUCCESS if player caught, FAILURE if player escapes detection threshold.
    /// </summary>
    public class ChasePlayerNode : Node
    {
        private readonly NavMeshAgent _agent;
        private readonly Transform _guardianTransform;
        private readonly Transform _playerTransform;
        private readonly float _chaseSpeed;
        private readonly float _loseRadius;
        private readonly float _catchDistance;

        public ChasePlayerNode(NavMeshAgent agent, Transform guardian, Transform player, float chaseSpeed, float loseRadius, float catchDistance)
        {
            _agent = agent;
            _guardianTransform = guardian;
            _playerTransform = player;
            _chaseSpeed = chaseSpeed;
            _loseRadius = loseRadius;
            _catchDistance = catchDistance;
        }

        public override NodeState Evaluate()
        {
            if (_agent == null || _guardianTransform == null || _playerTransform == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distSq = (_playerTransform.position - _guardianTransform.position).sqrMagnitude;

            // Player caught
            if (distSq <= _catchDistance * _catchDistance)
            {
                _agent.isStopped = true;
                state = NodeState.SUCCESS;
                return state;
            }

            // Player broke pursuit radius
            if (distSq > _loseRadius * _loseRadius)
            {
                state = NodeState.FAILURE;
                return state;
            }

            // Actively chase player
            _agent.isStopped = false;
            _agent.speed = _chaseSpeed;
            _agent.SetDestination(_playerTransform.position);

            state = NodeState.RUNNING;
            return state;
        }
    }
}
