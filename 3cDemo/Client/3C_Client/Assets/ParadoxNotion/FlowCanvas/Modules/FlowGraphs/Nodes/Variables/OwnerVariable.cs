using ParadoxNotion.Design;
using UnityEngine;


namespace FlowCanvas.Nodes
{

    [Name("Self", 100)]
    [Category("Variables")]
    [Description("Returns the Owner GameObject")]
    [ContextDefinedOutputs(typeof(GameObject))]
    public class OwnerVariable : FlowScriptNode
    {

        public override string name => "<size=20><b>SELF</b></size>";

        protected override void RegisterPorts() {
            AddValueOutput<GameObject>("Value", () => { return graphAgent ? graphAgent.gameObject : null; });
        }
    }
}