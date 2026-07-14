using System;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public sealed class ConditionRuleGraph : BaseTree
    {
        public ConditionRuleResultNode ResultNode => Nodes.OfType<ConditionRuleResultNode>().FirstOrDefault();

        public bool Evaluate()
        {
            return ResultNode != null && ResultNode.Evaluate();
        }

        public static ConditionRuleGraph CreateDefaultGraph(string graphName)
        {
            ConditionRuleGraph graph = new ConditionRuleGraph();
            graph.name = string.IsNullOrEmpty(graphName) ? "Condition Rule" : graphName;
            ConditionRuleResultNode resultNode = graph.CreateNode(typeof(ConditionRuleResultNode)) as ConditionRuleResultNode;
            resultNode?.SetDefaultResult(true);
#if UNITY_EDITOR
            if (resultNode != null)
            {
                resultNode.Position = new Vector2(360f, 0f);
                resultNode.Refresh();
            }
#endif
            return graph;
        }

        public override bool CanCreateNodeType(Type type)
        {
            if (type == null || type.IsAbstract)
                return false;

            if (typeof(ConditionRuleResultNode).IsAssignableFrom(type))
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
                if (current.FullName == "BTSMTL.Timeline.TimelineValueNode")
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

            ConditionRuleResultNode node = CreateNode(typeof(ConditionRuleResultNode)) as ConditionRuleResultNode;
            node?.SetDefaultResult(true);
            if (node != null)
            {
                node.Position = new Vector2(360f, 0f);
                node.Refresh();
            }
            return true;
        }
#endif
    }
}
