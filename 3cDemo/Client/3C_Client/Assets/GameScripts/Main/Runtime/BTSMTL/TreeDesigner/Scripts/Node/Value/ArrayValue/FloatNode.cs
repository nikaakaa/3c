using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Float")]
    [NodePath("Base/Value/Basic/Float")]
    public class FloatNode : ArrayValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Float"), ShowIf("m_NodeType", NodeType.Single)]
        FloatPropertyPort m_Float = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "FloatList"), ShowIf("m_NodeType", NodeType.List)]
        FloatListPropertyPort m_FloatList = new FloatListPropertyPort();
    }
}