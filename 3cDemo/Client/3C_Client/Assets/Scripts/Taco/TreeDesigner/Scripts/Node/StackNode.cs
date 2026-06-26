#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public class StackNode
    {
        [SerializeField]
        string m_GUID;
        public string GUID { get => m_GUID; set => m_GUID = value; }

        [SerializeField]
        Vector2 m_Position;
        public Vector2 Position { get => m_Position; set => m_Position = value; }

        [SerializeField]
        List<string> m_NodeGUIDs = new List<string>();
        public List<string> NodeGUIDs => m_NodeGUIDs;

        public StackNode() { }
    }
}
#endif