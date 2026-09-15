using System.Collections.Generic;
using UnityEngine;
using ParadoxNotion.Design;

namespace FlowCanvas.Nodes
{

    [Name("Flip Flop")]
    [Category("Flow Controllers/Togglers")]
    [Description("Each time input is called, the next output in order is called. After the last output, the order loops from the start. Reset, resets index to starting index.")]
    [ContextDefinedOutputs(typeof(int))]
    public class Sequence : FlowControlNode
    {

        [SerializeField, ExposeField]
        [GatherPortsCallback]
        [MinValue(2), DelayedField]
        private int _portCount = 2;

        [Name("Start Index"), MinValue(0)]
        public int current;
        private int original;

        public override string name => base.name + " " + string.Format("[{0}]", current.ToString());

        public override void OnGraphStarted() { current = Mathf.Clamp(current, 0, _portCount - 1); original = current; }
        public override void OnGraphStoped() { current = original; }

        protected override void RegisterPorts() {
            var outs = new List<FlowOutput>();
            for ( var i = 0; i < _portCount; i++ ) {
                outs.Add(AddFlowOutput(i.ToString()));
            }

            AddFlowInput("In", (f) =>
            {
                outs[current].Call(f);
                current = (int)Mathf.Repeat(current + 1, _portCount);
            });

            AddFlowInput("Reset", (f) => { current = original; });
            AddValueOutput<int>("Current", () => { return current; });
        }

        public void ConfigurePortCount(int portCount)
        {
            if (portCount < 2)
                throw new System.ArgumentOutOfRangeException(nameof(portCount));
            _portCount = portCount;
            current = 0;
            GatherPorts();
        }
    }
}
