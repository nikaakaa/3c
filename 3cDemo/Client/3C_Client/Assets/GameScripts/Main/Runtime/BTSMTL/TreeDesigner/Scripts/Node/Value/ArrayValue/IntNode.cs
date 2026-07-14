using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Int")]
    [NodePath("Base/Value/Basic/Int")]
    public class IntNode : ArrayValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Int"), ShowIf("m_NodeType", NodeType.Single)]
        IntPropertyPort m_Int = new IntPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "IntList"), ShowIf("m_NodeType", NodeType.List)]
        IntListPropertyPort m_IntList = new IntListPropertyPort();
    }
}