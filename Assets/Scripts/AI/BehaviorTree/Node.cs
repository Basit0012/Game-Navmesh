namespace RelicDash.AI.BehaviorTree
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    /// <summary>
    /// Abstract base class for all nodes in the modular Behavior Tree.
    /// Fulfills examination requirement #10.
    /// </summary>
    public abstract class Node
    {
        protected NodeState state;
        public NodeState State => state;

        public abstract NodeState Evaluate();
    }
}
