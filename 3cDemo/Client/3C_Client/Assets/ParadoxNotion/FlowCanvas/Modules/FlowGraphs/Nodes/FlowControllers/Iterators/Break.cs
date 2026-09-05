using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{
    [Name("Break", -100)]
    [Category("Flow Controllers/Iterators")]
    [Description("Can be used within a For Loop, For Each, While True, and Coroutine nodes, as well as any other node that has a 'Break' input (eg Wait) to Break the iteration. This has the same effect as calling the 'Break' input on that other node.")]
    public class Break : FlowControlNode
    {
        protected override void RegisterPorts() {
            AddFlowInput("Break", (f) => { f.Break(this); });
        }
    }
}