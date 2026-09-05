using UnityEngine;
using System.Collections.Generic;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Toggle")]
    [Description("Whenever In is called the 'current' output is called as well. Calling 'Toggle +' or 'Toggle -' changes the current output respectively up or down. Reset, resets index to starting index.")]
    [Category("Flow Controllers/Togglers")]
    [ContextDefinedOutputs(typeof(int))]
    public class MultiToggle : FlowControlNode
    {

        [SerializeField, ExposeField]
        [GatherPortsCallback]
        [MinValue(2), DelayedField]
        private int _portCount = 4;

        [Name("Start Index"), MinValue(0)]
        public int current;
        private int original;

        public override string name => base.name + " " + string.Format("[{0}]", current.ToString());

        public override void OnGraphStarted() { current = Mathf.Clamp(current, 0, _portCount - 1); original = current; }
        public override void OnGraphStoped() { current = original; }

        protected override void RegisterPorts() {
            var outs = new List<FlowOutput>();
            for ( int i = 0; i < _portCount; i++ ) {
                outs.Add(AddFlowOutput(i.ToString()));
            }
            AddFlowInput("In", (f) => { outs[current].Call(f); });
            AddFlowInput("Toggle +", "+", (f) => { current = (int)Mathf.Repeat(current + 1, _portCount); });
            AddFlowInput("Toggle -", "-", (f) => { current = (int)Mathf.Repeat(current - 1, _portCount); });
            AddFlowInput("Reset", (f) => { current = original; });
            AddValueOutput<int>("Current", () => { return current; });
        }
    }
}