using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner 
{
    [Serializable]
    [NodeName("Debug")]
    [NodePath("Base/Action/Debug")]
    public class DebugNode : ActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Log")]
        StringPropertyPort m_Log = new StringPropertyPort();

        protected override void DoAction()
        {
            this.Log(m_Log.Value);
        }
    }
}