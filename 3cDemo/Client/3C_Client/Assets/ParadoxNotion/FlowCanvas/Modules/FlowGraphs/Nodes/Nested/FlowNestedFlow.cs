using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{
    [Name("Sub Flow")]
    [DropReferenceType(typeof(FlowScript))]
    [Icon("FS")]
    public class FlowNestedFlow : FlowNestedBase<FlowScript>
    {

    }
}