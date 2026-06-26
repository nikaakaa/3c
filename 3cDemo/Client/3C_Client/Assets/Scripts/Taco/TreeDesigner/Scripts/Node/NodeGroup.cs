#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public class NodeGroup
    {
        [SerializeField]
        string m_Title;
        public string Title { get => m_Title; set => m_Title = value; }

        [SerializeField]
        Vector2 m_Position;
        public Vector2 Position { get => m_Position; set => m_Position = value; }

        [SerializeField]
        List<string> m_NodeGUIDs = new List<string>();
        public List<string> NodeGUIDs => m_NodeGUIDs;

        [SerializeField]
        List<string> m_StackGUIDs = new List<string>();
        public List<string> StackGUIDs => m_StackGUIDs;

        public NodeGroup() 
        {
            m_Title = "New Group";
        }
    }
}
#endif