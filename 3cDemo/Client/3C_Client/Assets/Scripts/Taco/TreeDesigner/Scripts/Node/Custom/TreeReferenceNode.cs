using System;
using System.Collections.Generic;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Tree Reference")]
    [NodeColor(107, 203, 119)]
    [NodePath("Base/Nesting/TreeReference")]
    [Input("Input"), Output("Output", PortCapacity.Single)]
    public sealed class TreeReferenceNode : BaseNode
    {
        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new TreeReferenceModule();
        }
    }
}
