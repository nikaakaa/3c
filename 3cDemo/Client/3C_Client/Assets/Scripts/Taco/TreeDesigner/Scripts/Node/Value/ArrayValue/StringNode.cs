using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("String")]
    [NodePath("Base/Value/Basic/String")]
    public class StringNode : ArrayValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "String"), ShowIf("m_NodeType", NodeType.Single)]
        StringPropertyPort m_String = new StringPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "StringList"), ShowIf("m_NodeType", NodeType.List)]
        StringListPropertyPort m_StringList = new StringListPropertyPort();
    }
}