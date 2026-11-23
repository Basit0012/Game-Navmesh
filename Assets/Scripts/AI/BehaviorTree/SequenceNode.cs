using System.Collections.Generic;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Composite Sequence node.
    /// Evaluates children sequentially. Returns FAILURE as soon as any child fails.
    /// Returns SUCCESS only when all children succeed. (AND logic)
    /// </summary>
    public class SequenceNode : Node
    {
        protected List<Node> children = new List<Node>();

        public SequenceNode() { }

        public SequenceNode(List<Node> children)
        {
            this.children = children;
        }

        public void AddChild(Node child)
        {
            children.Add(child);
        }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            for (int i = 0; i < children.Count; i++)
            {
                NodeState childState = children[i].Evaluate();

                switch (childState)
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;

                    case NodeState.RUNNING:
                        anyChildRunning = true;
                        break;

                    case NodeState.SUCCESS:
                        continue;
                }
            }

            state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }
}
