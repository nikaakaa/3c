using System;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public sealed class TransitionRuleGraph : BaseTree
    {
        public TransitionRuleResultNode ResultNode => Nodes.OfType<TransitionRuleResultNode>().FirstOrDefault();

        public bool Evaluate()
        {
            return ResultNode != null && ResultNode.Evaluate();
        }

        public override bool CanCreateNodeType(Type type)
        {
            if (type == null || type.IsAbstract)
                return false;

            if (typeof(TransitionRuleResultNode).IsAssignableFrom(type))
                return ResultNode == null;

            if (typeof(RunnableNode).IsAssignableFrom(type) ||
                typeof(StateMachineControlNode).IsAssignableFrom(type) ||
                typeof(StateMachineNode).IsAssignableFrom(type) ||
                typeof(StateNode).IsAssignableFrom(type) ||
                typeof(StateLifecycleNode).IsAssignableFrom(type) ||
                typeof(RootNode).IsAssignableFrom(type) ||
                IsTimelineValueNode(type))
                return false;

            return typeof(ValueNode).IsAssignableFrom(type);
        }

        static bool IsTimelineValueNode(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (current.FullName == "Taco.Timeline.TimelineValueNode")
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            if (ResultNode != null)
                return dirty;

            BaseNode node = CreateNode(typeof(TransitionRuleResultNode));
            node.Position = new Vector2(360f, 0f);
            node.Refresh();
            return true;
        }
#endif
    }
}
