using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{
    [Name("Write Flow Param")]
    [Category("Variables/Flow")]
    [Description("Writes (or creates) a named parameter to the incomming Flow, which you can later read with a ReadFlowParameter node. The output value returns the last set input value for convenience. Flow parameters are temporary variables that exist only in the context of the same Flow.")]
    [ContextDefinedInputs(typeof(Wild))]
    public class WriteFlowParameter<T> : FlowControlNode
    {
        private T setValue;

        protected override void RegisterPorts() {
            var o = AddFlowOutput("Out");
            var pName = AddValueInput<string>("Name");
            var pValue = AddValueInput<T>("Value");
            AddValueOutput<T>("Value", () => setValue);
            AddFlowInput("In", (f) =>
            {
                setValue = pValue.value;
                f.WriteParameter<T>(pName.value, pValue.value);
                o.Call(f);
            });
        }
    }
}