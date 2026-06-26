using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Stop")]
    [NodePath("Base/Action/Stop")]
    public class StopNode : ActionNode
    {
        protected override void DoAction()
        {
            
        }
    }
}