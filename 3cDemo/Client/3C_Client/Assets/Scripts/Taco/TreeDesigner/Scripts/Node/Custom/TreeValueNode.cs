using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("TreeValue")]
    [NodePath("Base/Custom/TreeValue")]
    [NodeView("TreeValueNodeView")]
    public partial class TreeValueNode : RunnableNode
    {
        [SerializeField]
        TreeValueNodeType m_NodeType;
        public TreeValueNodeType NodeType => m_NodeType;

        [SerializeField, PropertyPort(PortDirection.Input, "Tree"), ReadOnly]
        TreePropertyPort m_Tree = new TreePropertyPort();
        public BaseTree Tree => m_Tree.Value;

        [SerializeReference]
        PropertyPort m_Value = new PropertyPort() { Direction = PortDirection.Output };
        public PropertyPort Value => m_Value;

        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        string m_ExposedPropertyName;
        public string ExposedPropertyName => m_ExposedPropertyName;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }
        }
        public override void Dispose()
        {
            base.Dispose();

            m_Parent = null;
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
        protected override void OutputValue()
        {
            InputValue();
            if (m_NodeType == TreeValueNodeType.Get && Tree && !string.IsNullOrEmpty(m_ExposedPropertyName) && Tree.GetExposedProperty(m_ExposedPropertyName) is BaseExposedProperty exposedProperty)
                m_Value.SetValue(exposedProperty.GetValue());
        }
        protected override void OnStart()
        {
            base.OnStart();
            if (m_Parent.State == State.Running && m_NodeType == TreeValueNodeType.Set && Tree && !string.IsNullOrEmpty(m_ExposedPropertyName) && Tree.GetExposedProperty(m_ExposedPropertyName) is BaseExposedProperty exposedProperty)
                exposedProperty.SetValue(m_Value.GetValue());
        }
        protected override State OnUpdate()
        {
            return State.Success;
        }

#if UNITY_EDITOR

        [SerializeField, ShowInPanel("Preview")]
        BaseTree m_PreviewTree;
        public BaseTree PreviewTree => m_PreviewTree;

        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);
            m_InputEdgeGUID = edge.GUID;
            m_Parent = edge.StartNode as RunnableNode;
        }
        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);
            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }

        public void SetNodeType(TreeValueNodeType nodeType)
        {
            m_NodeType = nodeType;
        }
        public void SetExposedProperty(BaseExposedProperty exposedProperty)
        {
            foreach (var targetTypePair in PropertyPortUtility.TargetTypeMap)
            {
                if (targetTypePair.Value == ExposedPropertyUtility.TargetType(exposedProperty.GetType()))
                {
                    switch (m_NodeType)
                    {
                        case TreeValueNodeType.Get:
                            SetPropertyPort("m_Value", targetTypePair.Key, PortDirection.Output).Index = 1;
                            break;
                        case TreeValueNodeType.Set:
                            SetPropertyPort("m_Value", targetTypePair.Key, PortDirection.Input).Index = 1;
                            break;
                    }
                    break;
                }
            }
            m_ExposedPropertyName = exposedProperty.Name;
        }
        public void SetExposedPropertyWithoutChangePropertyPort(BaseExposedProperty exposedProperty)
        {
            m_ExposedPropertyName = exposedProperty.Name;
        }
        public void RemoveExposedProperty()
        {
            switch (m_NodeType)
            {
                case TreeValueNodeType.Get:
                    SetPropertyPort("m_Value", typeof(PropertyPort), PortDirection.Output);
                    break;
                case TreeValueNodeType.Set:
                    SetPropertyPort("m_Value", typeof(PropertyPort), PortDirection.Input);
                    break;
            }
            m_ExposedPropertyName = string.Empty;
        }
#endif
    }

    public enum TreeValueNodeType { Get, Set }
}

