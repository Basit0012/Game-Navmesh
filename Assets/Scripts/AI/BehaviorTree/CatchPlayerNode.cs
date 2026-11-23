using RelicDash.Core;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Executes Game Over trigger when the autonomous guardian catches the player.
    /// </summary>
    public class CatchPlayerNode : Node
    {
        public override NodeState Evaluate()
        {
            GameEvents.TriggerPlayerCaught();
            state = NodeState.SUCCESS;
            return state;
        }
    }
}
