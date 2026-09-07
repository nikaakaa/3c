using System.Collections.Generic;
using UnityEngine;
using ParadoxNotion.Design;
using ParadoxNotion.Serialization.FullSerializer;

namespace FlowCanvas.Nodes
{

    [System.Obsolete("Use Switch Probability")]
    [Name("Switch Random")]
    [Category("Flow Controllers/Switchers")]
    [Description("Calls one random output each time In is called.")]
    [ContextDefinedOutputs(typeof(Flow), typeof(int))]
    [fsMigrateTo(typeof(SwitchProbability))]
    public class Random : FlowControlNode
    {

        [SerializeField, ExposeField]
        [GatherPortsCallback]
        [MinValue(2), DelayedField]
        public int _portCount = 4;

        private int current;

        protected override void RegisterPorts() {
            var outs = new List<FlowOutput>();
            for ( var i = 0; i < _portCount; i++ ) {
                outs.Add(AddFlowOutput(i.ToString()));
            }
            AddFlowInput("In", (f) =>
            {
                current = UnityEngine.Random.Range(0, _portCount);
                outs[current].Call(f);
            });
            AddValueOutput<int>("Current", () => { return current; });
        }
    }
}