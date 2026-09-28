using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ParadoxNotion;
using UnityEngine;

namespace BTSMTL.EventGraphs
{
    [CreateAssetMenu(
        fileName = "HostEventGraph",
        menuName = "3C/BTSMTL/Host Event Graph")]
    public class HostEventGraph : FlowScript, IGraphExecutionFailureSink
    {
        [SerializeField] string m_AuthoringId = Guid.NewGuid().ToString("N");
        [SerializeField] string m_ContentRevision = "1";

        [NonSerialized] EventGraphHostContract m_RuntimeContract;
        [NonSerialized] IEventGraphHostContext m_HostContext;
        [NonSerialized] EventGraphExecutionFailure m_ExecutionFailure;

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        internal EventGraphExecutionFailure ExecutionFailure => m_ExecutionFailure;
        internal EventGraphInvocationContext CurrentInvocation =>
            m_HostContext == null
                ? throw new InvalidOperationException("Event graph has no active host invocation.")
                : m_HostContext.Invocation;

        public override bool allowBlackboardOverrides => false;
        public override bool requiresAgent => false;
        public override bool requiresPrimeNode => false;
        public override bool canAcceptVariableDrops => true;
        public override bool allowsPortIdentityAliases => false;

        public EventGraphVariableContract BuildVariableContract()
        {
            var descriptors = new List<EventGraphVariableDescriptor>();
            foreach (Variable variable in blackboard.variables.Values)
            {
                if (!variable.hasStableIdentity)
                    throw new InvalidOperationException(
                        $"Event graph variable '{variable.name}' has no stable identity.");
                if (!EventGraphValueKinds.TryGet(variable.varType, out _))
                    throw new InvalidOperationException(
                        $"Event graph variable '{variable.name}' uses unsupported type '{variable.varType.FullName}'.");
                descriptors.Add(new EventGraphVariableDescriptor(
                    new EventGraphVariableReference(AuthoringId, variable.ID),
                    variable.name,
                    variable.varType,
                    EventGraphValue.FromObject(variable.value),
                    variable.isExposedPublic));
            }
            return new EventGraphVariableContract(
                AuthoringId,
                ContentRevision,
                descriptors);
        }

        public void ConfigureIdentity(string authoringId, string contentRevision)
        {
            if (string.IsNullOrWhiteSpace(authoringId) ||
                string.IsNullOrWhiteSpace(contentRevision))
            {
                throw new ArgumentException("Event graph identity and revision are required.");
            }
            m_AuthoringId = authoringId.Trim();
            m_ContentRevision = contentRevision.Trim();
        }

#if UNITY_EDITOR
        public void ConfigureAuthoringIdentity(
            string authoringId,
            string contentRevision) =>
            HostEventGraphEditorMutation.ConfigureIdentity(
                this,
                authoringId,
                contentRevision);

        public void ClearAuthoringContent() =>
            HostEventGraphEditorMutation.ClearAuthoringContent(this);

        public Variable<T> DeclareVariable<T>(
            string variableId,
            string variableName,
            T defaultValue) =>
            HostEventGraphEditorMutation.DeclareVariable(
                this,
                variableId,
                variableName,
                defaultValue);

        public Node AddAuthoringNode(
            Type nodeType,
            string authoringIdentity,
            Vector2 position = default) =>
            HostEventGraphEditorMutation.AddAuthoringNode(
                this,
                nodeType,
                authoringIdentity,
                position);

        public T AddAuthoringNode<T>(
            string authoringIdentity,
            Vector2 position = default)
            where T : FlowNode =>
            (T)AddAuthoringNode(typeof(T), authoringIdentity, position);

        public void ConfigureNodeMetadata(
            Node node,
            string tag,
            string comments,
            bool isBreakpoint) =>
            HostEventGraphEditorMutation.ConfigureNodeMetadata(
                this,
                node,
                tag,
                comments,
                isBreakpoint);

        public void ConfigureCanvas(
            string category,
            string comments,
            Vector2 translation,
            float zoomFactor) =>
            HostEventGraphEditorMutation.ConfigureCanvas(
                this,
                category,
                comments,
                translation,
                zoomFactor);

        public void BindVariableNode(
            ParameterVariableNode node,
            string variableId) =>
            HostEventGraphEditorMutation.BindVariableNode(
                this,
                node,
                variableId);

        public void ConfigureVariable(
            string variableId,
            bool isExposedPublic) =>
            HostEventGraphEditorMutation.ConfigureVariable(
                this,
                variableId,
                isExposedPublic);

        public void ConfigureValueInput<T>(
            FlowNode node,
            string portId,
            T value) =>
            HostEventGraphEditorMutation.ConfigureValueInput(
                this,
                node,
                portId,
                value);

        public void ConfigureHostInput(Node node, string inputId) =>
            HostEventGraphEditorMutation.ConfigureHostInput(
                this,
                node,
                inputId);

        public void ConfigureUpdateEvent(UpdateEvent node) =>
            HostEventGraphEditorMutation.ConfigureUpdateEvent(
                this,
                node);

        public void ConfigureInstantSplit(Split node, int portCount) =>
            HostEventGraphEditorMutation.ConfigureInstantSplit(
                this,
                node,
                portCount);

        public void ConfigureSequence(
            Sequence node,
            int portCount,
            int startIndex) =>
            HostEventGraphEditorMutation.ConfigureSequence(
                this,
                node,
                portCount,
                startIndex);

        public void ConfigureAssignment<T>(
            SetVariable<T> node,
            AssignOp operation,
            bool perSecond) =>
            HostEventGraphEditorMutation.ConfigureAssignment(
                this,
                node,
                operation,
                perSecond);

        public void ConfigureMacro(
            MacroNodeWrapper node,
            Macro macro) =>
            HostEventGraphEditorMutation.ConfigureMacro(
                this,
                node,
                macro);

        public BinderConnection ConnectAuthoringPorts(
            string connectionIdentity,
            Node sourceNode,
            string sourcePortId,
            Node targetNode,
            string targetPortId) =>
            HostEventGraphEditorMutation.ConnectAuthoringPorts(
                this,
                connectionIdentity,
                sourceNode,
                sourcePortId,
                targetNode,
                targetPortId);

#endif

        internal void AdvanceContentRevision()
        {
            m_ContentRevision = Guid.NewGuid().ToString("N");
        }

        internal void BeginInvocation(
            EventGraphHostContract contract,
            IEventGraphHostContext context)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!string.Equals(
                    context.Invocation.EventId,
                    contract.UpdateEventId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Event graph invocation '{context.Invocation.EventId}' is not accepted by contract '{contract.ContractId}'.");
            }
            m_RuntimeContract = contract;
            m_HostContext = context;
            m_ExecutionFailure = null;
        }

        internal void EndInvocation()
        {
            m_HostContext = null;
            m_RuntimeContract = null;
        }

        internal T ReadInput<T>(string inputId)
        {
            if (m_HostContext == null || m_RuntimeContract == null)
                throw new InvalidOperationException("Event graph input was read outside an active host invocation.");
            if (!m_RuntimeContract.TryGetInput(inputId, out EventGraphInputDescriptor descriptor))
                throw new InvalidOperationException(
                    $"Event graph input '{inputId}' is not declared by host contract '{m_RuntimeContract.ContractId}'.");
            if (descriptor.ValueType != typeof(T))
                throw new InvalidOperationException(
                    $"Event graph input '{inputId}' requires '{descriptor.ValueType.FullName}', not '{typeof(T).FullName}'.");
            if (!m_HostContext.TryRead(inputId, descriptor.ValueKind, out EventGraphValue value))
                throw new InvalidOperationException(
                    $"Event graph host did not provide input '{inputId}' for invocation '{m_HostContext.Invocation.Identity}'.");
            return value.As<T>();
        }

        interface IVariableReader
        {
            EventGraphValue Read();
        }

        sealed class VariableReader<T> : IVariableReader
        {
            readonly Variable<T> m_Variable;

            public VariableReader(Variable variable) => m_Variable = (Variable<T>)variable;
            public EventGraphValue Read() => EventGraphValue.FromTyped(m_Variable.value);
        }

        [NonSerialized] IVariableReader[] m_OutputReaders;
        [NonSerialized] EventGraphVariableBuffer m_OutputBuffer;
        [NonSerialized] EventGraphVariableContract m_OutputContract;

        internal void InitializeVariableOutput(EventGraphVariableContract variableContract,
            EventGraphHostContract hostContract)
        {
            m_OutputContract = variableContract;
            m_OutputReaders = new IVariableReader[variableContract.Layout.Entries.Count];
            m_OutputBuffer = new EventGraphVariableBuffer(m_OutputReaders.Length);
            for (int i = 0; i < m_OutputReaders.Length; i++)
            {
                EventGraphVariableDescriptor descriptor = variableContract.Layout.Entries[i].Descriptor;
                if (!hostContract.TryGetOutput(descriptor.Reference.VariableId, out var output) ||
                    output.ValueType != descriptor.ValueType)
                    throw new InvalidOperationException($"Event graph output '{descriptor.Reference.VariableId}' does not match its contract.");
                Variable variable = blackboard.GetVariableByID(descriptor.Reference.VariableId);
                if (variable == null || variable.varType != descriptor.ValueType)
                    throw new InvalidOperationException($"Event graph variable '{descriptor.Reference.VariableId}' does not match its declaration.");
                m_OutputReaders[i] = (IVariableReader)Activator.CreateInstance(
                    typeof(VariableReader<>).MakeGenericType(descriptor.ValueType), variable);
            }
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i] is ParameterVariableNode node &&
                    (node.parameter.varRef == null || !ReferenceEquals(node.parameter.varRef,
                        blackboard.GetVariableByID(node.parameter.targetVariableID))))
                    throw new InvalidOperationException($"Event graph variable node '{node.UID}' is not bound to its instance.");
            }
        }

        internal void InvalidateVariableOutput() => m_OutputBuffer?.Invalidate();

        internal EventGraphVariableFrame CreateVariableFrame(
            EventGraphInvocationIdentity invocation,
            ulong resetGeneration)
        {
            if (m_RuntimeContract == null)
                throw new InvalidOperationException("Event graph output was requested outside an active host invocation.");
            for (int i = 0; i < m_OutputReaders.Length; i++)
                m_OutputBuffer.Write(i, m_OutputReaders[i].Read());
            return m_OutputBuffer.Publish(m_OutputContract, invocation, resetGeneration);
        }

        internal Node AddNodeNative(Type nodeType, Vector2 position) =>
            base.AddNode(nodeType, position);

        internal BinderConnection CreatePortConnectionNative(
            Port source,
            Port target) =>
            base.CreatePortConnection(source, target);

        internal void RemoveNodeNative(
            Node node,
            bool recordUndo,
            bool force) =>
            base.RemoveNode(node, recordUndo, force);

        internal void RemoveConnectionNative(
            Connection connection,
            bool recordUndo) =>
            base.RemoveConnection(connection, recordUndo);

        internal List<Node> DuplicateNodesNative(
            List<Node> nodes,
            Vector2 originPosition) =>
            base.DuplicateNodes(nodes, originPosition);

        internal void ClearNativeNodes()
        {
            canvasGroups = null;
            foreach (Node node in allNodes.ToArray())
                base.RemoveNode(node, false, false);
        }

        void IGraphExecutionFailureSink.ReportExecutionFailure(
            Node node,
            string message,
            Exception exception)
        {
            if (m_ExecutionFailure != null)
                return;
            string eventId = m_HostContext?.Invocation.EventId ?? string.Empty;
            m_ExecutionFailure = new EventGraphExecutionFailure(
                AuthoringId,
                eventId,
                node?.UID ?? string.Empty,
                message,
                exception);
        }

        protected override void OnGraphInitialize()
        {
            base.OnGraphInitialize();
        }

#if UNITY_EDITOR
        public override bool usesDomainAuthoring => true;
        public override bool allowsEditorExecution => false;
        public override bool isEditorReadOnly => Application.isPlaying;

        public override bool CanAuthorNodeType(Type nodeType) =>
            EventGraphCapabilityCatalog.Allows(nodeType);

        public override bool CanAuthorConnection(
            Port source,
            Port target,
            out string reason) =>
            HostEventGraphEditorMutation.CanConnect(
                this,
                source,
                target,
                out reason);

        public override UnityEditor.GenericMenu GetNodesMenu(
            Vector2 position,
            Port context,
            UnityEngine.Object dropInstance)
        {
            return AppendFlowNodesMenu(
                new UnityEditor.GenericMenu(),
                string.Empty,
                position,
                context,
                dropInstance);
        }

        public override void AppendNodeCreationItem(
            UnityEditor.GenericMenu menu,
            string category,
            Type type,
            Vector2 position,
            Port context,
            object dropInstance) =>
            HostEventGraphEditorMutation.AppendCreationItem(
                this,
                menu,
                category,
                type,
                position,
                context,
                dropInstance);

        public override Node AddNode(
            Type nodeType,
            Vector2 position = default) =>
            HostEventGraphEditorMutation.AddNode(
                this,
                nodeType,
                position);

        public override BinderConnection CreatePortConnection(
            Port source,
            Port target) =>
            HostEventGraphEditorMutation.CreatePortConnection(
                this,
                source,
                target);

        public override void DisconnectPort(Port port) =>
            HostEventGraphEditorMutation.DisconnectPort(this, port);

        public override void RemoveNode(
            Node node,
            bool recordUndo = true,
            bool force = false) =>
            HostEventGraphEditorMutation.RemoveNode(
                this,
                node,
                recordUndo,
                force);

        public override void RemoveConnection(
            Connection connection,
            bool recordUndo = true) =>
            HostEventGraphEditorMutation.RemoveConnection(
                this,
                connection,
                recordUndo);

        public override List<Node> DuplicateNodes(
            List<Node> nodes,
            Vector2 originPosition = default) =>
            HostEventGraphEditorMutation.DuplicateNodes(
                this,
                nodes,
                originPosition);

        public override void ClearGraph() =>
            HostEventGraphEditorMutation.Clear(this);

        public override bool HandleEditorCommand(
            string command,
            Vector2 position) =>
            HostEventGraphEditorMutation.HandleCommand(
                this,
                command,
                position);

        protected override void OnVariableDropInGraph(
            IBlackboard blackboard,
            Variable variable,
            Vector2 mousePosition) =>
            HostEventGraphEditorMutation.HandleBlackboardVariableDrop(
                this,
                blackboard,
                variable,
                mousePosition);

        public override UnityEngine.Object EditorUndoTarget =>
            HostEventGraphEditorMutation.UndoTarget(this);
#endif
    }
}
