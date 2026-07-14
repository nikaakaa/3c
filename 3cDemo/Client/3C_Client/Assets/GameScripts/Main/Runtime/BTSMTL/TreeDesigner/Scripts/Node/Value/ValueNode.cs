using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner 
{
    [Serializable]
    [NodeColor(74, 42, 192)]
    public abstract partial class ValueNode : BaseNode
    {
        protected override void OutputValue()
        {
            InputValue();
        }
    }
}