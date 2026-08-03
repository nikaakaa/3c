using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("ExposedProperty")]
    [NodePath("Base/Custom/ExposedProperty")]
    [NodeView("ExposedPropertyNodeView")]
    [NodeAuthoringCapability(NodeAuthoringCapability.SharedBlackboard)]
    public partial class ExposedPropertyNode : RunnableNode
    {
        public const string FlowInputPortName = "Input";

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
        PipelineBlackboardVariableReference m_BlackboardVariable;
        public PipelineBlackboardVariableReference BlackboardVariable => m_BlackboardVariable;

        [SerializeField, ShowInPanel("Fact Context")]
        UnityEngine.Object m_FactContext;
        public UnityEngine.Object FactContext => m_FactContext;

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
            foreach (var inputEdge in m_Owner.GetInputEdges(this, FlowInputPortName))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }

            if (m_BlackboardVariable.IsValid &&
                m_Owner.GUIDExposedPropertyMap.TryGetValue(m_BlackboardVariable.DeclarationId, out BaseExposedProperty exposedProperty))
                m_ExposedProperty = exposedProperty;
            else if (m_BlackboardVariable.IsValid &&
                     m_Owner.User is IPipelineBlackboardRuntimeAccess blackboardRuntime &&
                     blackboardRuntime.TryResolvePipelineBlackboardDeclaration(m_BlackboardVariable, out exposedProperty))
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
        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (NodeAssetReference reference in base.GetAssetReferences())
                yield return reference;
            if (m_FactContext)
                yield return new NodeAssetReference(this, "m_FactContext", "Fact Context", m_FactContext, false);
        }
        protected override void OutputValue()
        {
            base.OutputValue();
            if (m_NodeType == ExposedPropertyNodeType.Get && m_BlackboardVariable.IsValid)
            {
                IPipelineBlackboardRuntimeAccess blackboardRuntime = RequireBlackboardRuntime();
                if (!blackboardRuntime.TryGetPipelineBlackboardValue(
                        Owner,
                        m_BlackboardVariable,
                        m_Value.ValueType,
                        out object value))
                    throw new InvalidOperationException(
                        $"Pipeline blackboard read failed for declaration '{m_BlackboardVariable.DeclarationId}'.");
                m_Value.SetValue(value);
            }
        }
        protected override void OnStart()
        {
            base.OnStart();
            if (m_NodeType != ExposedPropertyNodeType.Set || !m_BlackboardVariable.IsValid || m_Parent == null || m_Parent.State != State.Running)
                return;

            object value = m_Value.GetValue();
            IPipelineBlackboardRuntimeAccess blackboardRuntime = RequireBlackboardRuntime();
            if (!blackboardRuntime.SetPipelineBlackboardValue(Owner, m_BlackboardVariable, value, m_FactContext))
                throw new InvalidOperationException(
                    $"Pipeline blackboard write failed for declaration '{m_BlackboardVariable.DeclarationId}'.");
        }
        protected override State OnUpdate()
        {
            return State.Success;
        }

        IPipelineBlackboardRuntimeAccess RequireBlackboardRuntime()
        {
            if (Owner != null && Owner.User is IPipelineBlackboardRuntimeAccess blackboardRuntime)
                return blackboardRuntime;
            throw new InvalidOperationException(
                $"Pipeline blackboard declaration '{m_BlackboardVariable.DeclarationId}' requires a registered runtime owner.");
        }

#if UNITY_EDITOR
        public override IEnumerable<FlowPortDeclaration> GetFlowPortDeclarations(BaseGraph owner)
        {
            foreach (FlowPortDeclaration declaration in base.GetFlowPortDeclarations(owner))
                yield return declaration;
            if (m_NodeType == ExposedPropertyNodeType.Set)
                yield return new FlowPortDeclaration(
                    FlowInputPortName,
                    PortDirection.Input,
                    PortCapacity.Single);
        }

        public override IEnumerable<FlowPortDeclaration> GetSupportedFlowPortDeclarations(BaseGraph owner)
        {
            foreach (FlowPortDeclaration declaration in base.GetFlowPortDeclarations(owner))
                yield return declaration;
            yield return new FlowPortDeclaration(
                FlowInputPortName,
                PortDirection.Input,
                PortCapacity.Single);
        }

        public static ExposedPropertyNode Create(BaseGraph targetGraph, BaseExposedProperty exposedProperty)
        {
            ExposedPropertyNode exposedPropertyNode = targetGraph.CreateNode(typeof(ExposedPropertyNode)) as ExposedPropertyNode;
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
            m_BlackboardVariable = exposedProperty.CreateBlackboardReference();
            m_ExposedProperty = exposedProperty;
        }
        public void SetExposedPropertyWithoutChangePropertyPort(BaseExposedProperty exposedProperty)
        {
            m_BlackboardVariable = exposedProperty.CreateBlackboardReference();
            m_ExposedProperty = exposedProperty;
        }
        public void SetFactContext(UnityEngine.Object factContext)
        {
            m_FactContext = factContext;
        }
        public void ResolveExposedProperty(BaseExposedProperty exposedProperty)
        {
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
            m_BlackboardVariable = PipelineBlackboardVariableReference.None;
            m_ExposedProperty = null;
        }
#endif
    }
    public enum ExposedPropertyNodeType { Get, Set }
}

