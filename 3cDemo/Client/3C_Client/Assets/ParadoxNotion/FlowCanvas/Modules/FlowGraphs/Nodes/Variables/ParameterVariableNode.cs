using NodeCanvas.Framework;

namespace FlowCanvas.Nodes
{
    abstract public class ParameterVariableNode : FlowScriptNode
    {
        abstract public BBParameter parameter { get; }
    }
}