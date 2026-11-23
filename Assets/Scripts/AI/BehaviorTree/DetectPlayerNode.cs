using UnityEngine;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Checks if the player is within a specified radius (e.g. 8.0 meters).
    /// Returns SUCCESS if detected, otherwise FAILURE.
    /// </summary>
    public class DetectPlayerNode : Node
    {
        private readonly Transform _guardianTransform;
        private readonly Transform _playerTransform;
        private readonly float _detectionRadius;

        public DetectPlayerNode(Transform guardian, Transform player, float radius)
        {
            _guardianTransform = guardian;
            _playerTransform = player;
            _detectionRadius = radius;
        }

        public override NodeState Evaluate()
        {
            if (_guardianTransform == null || _playerTransform == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float distSq = (_playerTransform.position - _guardianTransform.position).sqrMagnitude;
            if (distSq <= _detectionRadius * _detectionRadius)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
