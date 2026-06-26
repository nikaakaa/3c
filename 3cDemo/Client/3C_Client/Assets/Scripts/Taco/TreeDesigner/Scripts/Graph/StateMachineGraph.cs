using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public class StateMachineGraph : BaseTree
    {
        public IEnumerable<StateNode> StateNodes => Nodes.OfType<StateNode>();
        public StateMachineEnterNode EnterNode => Nodes.OfType<StateMachineEnterNode>().FirstOrDefault();
        public StateMachineAnyStateNode AnyStateNode => Nodes.OfType<StateMachineAnyStateNode>().FirstOrDefault();
        public StateMachineExitNode ExitNode => Nodes.OfType<StateMachineExitNode>().FirstOrDefault();

        public override bool CanCreateNodeType(Type type)
        {
            if (type == null || type.IsAbstract)
                return false;

            if (typeof(StateMachineEnterNode).IsAssignableFrom(type))
                return EnterNode == null;

            if (typeof(StateMachineAnyStateNode).IsAssignableFrom(type))
                return AnyStateNode == null;

            if (typeof(StateMachineExitNode).IsAssignableFrom(type))
                return ExitNode == null;

            if (typeof(StateMachineControlNode).IsAssignableFrom(type))
                return false;

            if (typeof(StateMachineNode).IsAssignableFrom(type))
                return false;

            if (typeof(StateNode).IsAssignableFrom(type))
                return true;

            if (typeof(RunnableNode).IsAssignableFrom(type))
                return false;

            return false;
        }
    }
}
