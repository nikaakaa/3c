using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Null")]
    [Category("Variables")]
    [Description("Simply returns a NULL")]
    [ContextDefinedOutputs(typeof(Wild))]
    public class NullObject : FlowScriptNode
    {
        public override string name => "<size=20><b>NULL</b></size>";

        protected override void RegisterPorts() {
            AddValueOutput<object>("Value", () => { return null; });
        }
    }
}