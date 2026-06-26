using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("ExposedProperty")]
    [NodePath("Base/Custom/ExposedProperty")]
    [NodeView("ExposedPropertyNodeView")]
    public partial class ExposedPropertyNode : RunnableNode
    {
        [SerializeField]
        ExposedPropertyNodeType m_NodeType;
        public ExposedPropertyNodeType NodeType => m_NodeType;

        [SerializeReference]
        PropertyPort m_Value = new PropertyPort() { Direction = PortDirection.Output };
        public PropertyPort Value => m_Value;

        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        string m_ExposedPropertyGUID;
        public string ExposedPropertyGUID => m_ExposedPropertyGUID;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        BaseExposedProperty m_ExposedProperty;
        public BaseExposedProperty ExposedProperty => m_ExposedProperty;

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

            if (!string.IsNullOrEmpty(m_ExposedPropertyGUID) && m_Owner.GUIDExposedPropertyMap.TryGetValue(m_ExposedPropertyGUID, out BaseExposedProperty exposedProperty))
                m_ExposedProperty = exposedProperty;
        }
        public override void Dispose()
        {
            base.Dispose();

            m_Parent = null;
            m_ExposedProperty = null;
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
        protected override void OutputValue()
        {
            base.OutputValue();
            if (m_NodeType == ExposedPropertyNodeType.Get && m_ExposedProperty)
                m_Value.SetValue(m_ExposedProperty.GetValue());
        }
        protected override void OnStart()
        {
            base.OnStart();
            if (m_Parent.State == State.Running && m_NodeType == ExposedPropertyNodeType.Set && m_ExposedProperty)
                m_ExposedProperty.SetValue(m_Value.GetValue());
        }
        protected override State OnUpdate()
        {
            return State.Success;
        }

#if UNITY_EDITOR
        public static ExposedPropertyNode Create(BaseExposedProperty exposedProperty)
        {
            ExposedPropertyNode exposedPropertyNode = exposedProperty.Owner.CreateNode(typeof(ExposedPropertyNode)) as ExposedPropertyNode;
            exposedPropertyNode.SetExposedProperty(exposedProperty);
            return exposedPropertyNode;
        }
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

        public void SetNodeType(ExposedPropertyNodeType nodeType)
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
                        case ExposedPropertyNodeType.Get:
                            SetPropertyPort("m_Value", targetTypePair.Key, PortDirection.Output);
                            break;
                        case ExposedPropertyNodeType.Set:
                            SetPropertyPort("m_Value", targetTypePair.Key, PortDirection.Input);
                            break;
                    }
                    break;
                }
            }
            m_ExposedPropertyGUID = exposedProperty.GUID;
            m_ExposedProperty = exposedProperty;
        }
        public void SetExposedPropertyWithoutChangePropertyPort(BaseExposedProperty exposedProperty)
        {
            m_ExposedPropertyGUID = exposedProperty.GUID;
            m_ExposedProperty = exposedProperty;
        }
        public void RemoveExposedProperty()
        {
            switch (m_NodeType)
            {
                case ExposedPropertyNodeType.Get:
                    SetPropertyPort("m_Value", typeof(PropertyPort), PortDirection.Output);
                    break;
                case ExposedPropertyNodeType.Set:
                    SetPropertyPort("m_Value", typeof(PropertyPort), PortDirection.Input);
                    break;
            }
            m_ExposedPropertyGUID = string.Empty;
            m_ExposedProperty = null;
        }
#endif
    }
    public enum ExposedPropertyNodeType { Get, Set }
}

