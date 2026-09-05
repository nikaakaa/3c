using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Read Flow Param")]
    [Category("Variables/Flow")]
    [Description("Reads a named parameter from the incomming Flow and returns it's value.\nFlow parameters can be set with a WriteFlowParameter node.\nFlow parameters are temporary variables that exist only in the context of the same Flow.")]
    [ContextDefinedOutputs(typeof(Wild))]
    public class ReadFlowParameter<T> : FlowControlNode
    {

        private T flowValue;
        protected override void RegisterPorts() {
            var o = AddFlowOutput("Out");
            var pName = AddValueInput<string>("Name");
            AddValueOutput<T>("Value", () => { return flowValue; });
            AddFlowInput("In", (f) =>
            {
                flowValue = f.ReadParameter<T>(pName.value);
                o.Call(f);
            });
        }
    }
}