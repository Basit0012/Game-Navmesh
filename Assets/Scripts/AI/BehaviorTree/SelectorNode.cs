using System.Collections.Generic;

namespace RelicDash.AI.BehaviorTree
{
    /// <summary>
    /// Composite Selector node.
    /// Evaluates children in priority order. Returns SUCCESS or RUNNING as soon as any child succeeds or runs.
    /// Returns FAILURE only if all children fail. (OR logic)
    /// </summary>
    public class SelectorNode : Node
    {
        protected List<Node> children = new List<Node>();

        public SelectorNode() { }

        public SelectorNode(List<Node> children)
        {
            this.children = children;
        }

        public void AddChild(Node child)
        {
            children.Add(child);
        }

        public override NodeState Evaluate()
        {
            for (int i = 0; i < children.Count; i++)
            {
                NodeState childState = children[i].Evaluate();

                switch (childState)
                {
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state;

                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;

                    case NodeState.FAILURE:
                        continue;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
