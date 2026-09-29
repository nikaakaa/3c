using System;
using System.Collections.Generic;
using FlowCanvas;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeSubgraphHandler :
        ICharacterPoseNativeNodeHandler, Diagnostics.ICharacterNativeStateCaptureSource, ICharacterPoseNativePhaseSource
    {
        readonly struct SubgraphPortBinding
        {
            internal SubgraphPortBinding(
                PoseNodeId nodeId,
                PosePortId parent,
                PosePortId child,
                ValueInput input,
                CharacterPosePortKind kind)
            {
                NodeId = nodeId;
                Parent = parent;
                Child = child;
                Input = input;
                Kind = kind;
            }

            internal PoseNodeId SourceNodeId { get; }
            internal PosePortId Parent { get; }
            internal PosePortId Child { get; }
            internal ValueInput Input { get; }
            internal CharacterPosePortKind Kind { get; }
        }

        readonly PoseNodeId m_NodeId;
        readonly ulong m_RequestId;
        readonly ulong m_InstanceId;
        readonly ulong m_ResetGeneration;
        readonly string m_Reason;
        readonly ICharacterPoseNativeNodeHandlerFactory m_Factory;
        readonly List<SubgraphPortBinding> m_ImmediateInputs =
            new List<SubgraphPortBinding>();
        readonly List<SubgraphPortBinding> m_DeferredInputs =
            new List<SubgraphPortBinding>();
        readonly Dictionary<PosePortId, SubgraphPortBinding> m_OutputPorts =
            new Dictionary<PosePortId, SubgraphPortBinding>();
        CharacterPoseCanvasNode m_CallNode;
        CharacterPoseNativeGraphRuntime m_Child;
        CharacterPoseNativeFrameLease m_ChildLease;
        CharacterPoseNativePreparationResult m_ChildPreparation;
        CharacterPoseNativeEvaluationResult m_ChildEvaluation;
        bool m_ChildFrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeSubgraphHandler(
            PoseNodeId nodeId,
            ulong requestId,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeHandlerFactory factory)
        {
            if (!nodeId.IsValid || requestId == 0 || instanceId == 0 ||
                resetGeneration == 0 || string.IsNullOrWhiteSpace(reason) ||
                factory == null)
            {
                throw new ArgumentException(
                    "Pose native subgraph handler binding is invalid.");
            }
            m_NodeId = nodeId;
            m_RequestId = requestId;
            m_InstanceId = instanceId;
            m_ResetGeneration = resetGeneration;
            m_Reason = reason.Trim();
            m_Factory = factory;
        }

        public int StateCaptureCount => m_Child?.StateCapture.Count ?? 0;
        public int PhasePlayerCount => m_ChildFrameOpen ? m_Child.PhaseSources.PhasePlayerCount : 0;
        public Presentation.AnimationClipPlayerRuntime ReadPhasePlayer(int index) => m_Child.PhaseSources.ReadPhasePlayer(index);
        public Diagnostics.CharacterNativeStateCaptureRow ReadStateCapture(int index) => m_Child.StateCapture[index];
        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseSubgraph;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            m_CallNode = runtime.Graph.RequireNode(NodeId);
            if (m_CallNode.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                m_CallNode.Subgraph == null)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph handler '{NodeId}' does not match its call node.");
            }
            CharacterPoseNativeAdoptedResult adopted =
                runtime.CreateChild(
                    m_RequestId,
                    m_CallNode.Subgraph.PoseGraphId,
                    CharacterPoseNativeGraphBoundary.Subgraph,
                    m_InstanceId,
                    m_ResetGeneration,
                    m_Reason,
                    m_Factory,
                    out m_Child);
            if (!adopted.IsAdopted || m_Child == null)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' was not adopted: {adopted.Message}");
            }
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            RequireChild();
            BindInterface();
        }

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            RequireChild();
            if (resetGeneration == m_Child.ResetGeneration)
                m_Child.ResetForStateEntry();
            else
            {
                CharacterPoseNativeResetResult result =
                    m_Child.ResetInstance(resetGeneration);
                if (!result.IsReset)
                    throw new InvalidOperationException(result.Message);
            }
            m_ChildFrameOpen = false;
            m_ChildPreparation = default;
            m_ChildEvaluation = default;
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireChild();
            if (m_ChildFrameOpen)
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has an open child frame.");
            m_ChildPreparation = default;
            m_ChildEvaluation = default;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireChild();
            m_ChildLease = m_Child.BeginFrame(
                in input,
                lineage.CompletionIdentity);
            m_ChildFrameOpen = true;
            BindInputs(runtime, false);
            m_ChildPreparation = m_Child.PrepareFrame(m_ChildLease);
            if (!m_ChildPreparation.IsValid ||
                m_ChildPreparation.Status != CharacterPoseNativeFrameStatus.Prepared)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' preparation failed: {m_ChildPreparation.Message}");
            }
            return m_ChildPreparation.Demand.Requests;
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            RequireChild();
            if (!m_OutputPorts.TryGetValue(portId, out SubgraphPortBinding binding))
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has no output '{portId}'.");
            return m_Child.ReadInputValue(
                binding.Input,
                binding.SourceNodeId,
                binding.Child,
                binding.Kind);
        }

        public void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireChildFrame();
            if (!m_ChildPreparation.IsValid ||
                m_ChildPreparation.Status != CharacterPoseNativeFrameStatus.Prepared)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has no prepared child demand.");
            }
            BindInputs(runtime, true);
            ref readonly CharacterPoseNativeSourceDemand childDemand =
                ref m_ChildPreparation.Demand;
            m_ChildEvaluation = m_Child.Evaluate(
                m_ChildLease,
                in childDemand,
                barrierIdentity);
            if (!m_ChildEvaluation.IsValid ||
                m_ChildEvaluation.Status != CharacterPoseNativeFrameStatus.Evaluated)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' evaluation failed: {m_ChildEvaluation.Message}");
            }
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireChildFrame();
            if (!m_ChildPreparation.IsValid ||
                m_ChildPreparation.Status != CharacterPoseNativeFrameStatus.Prepared)
            {
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has no prepared child demand.");
            }
            ref readonly CharacterPoseNativeSourceDemand childDemand =
                ref m_ChildPreparation.Demand;
            m_Child.PrepareEvaluation(
                m_ChildLease,
                in childDemand,
                barrierIdentity);
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireChildFrame();
            CharacterPoseNativeValidationResult result =
                m_Child.ValidatePending(
                    m_ChildLease,
                    in m_ChildEvaluation);
            if (!result.IsValidated)
                throw new InvalidOperationException(result.Message);
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireChildFrame();
            CharacterPoseNativePublicationResult result =
                m_Child.Commit(
                    m_ChildLease,
                    in m_ChildEvaluation);
            if (!result.IsPublished)
            {
                m_ChildFrameOpen = false;
                m_ChildLease = default;
                m_ChildPreparation = default;
                m_ChildEvaluation = default;
                throw new InvalidOperationException(result.Message);
            }
            m_ChildFrameOpen = false;
            m_ChildLease = default;
            m_ChildPreparation = default;
            m_ChildEvaluation = default;
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            if (!m_ChildFrameOpen)
                return;
            m_Child.Discard(m_ChildLease, reason);
            m_ChildFrameOpen = false;
            m_ChildLease = default;
            m_ChildPreparation = default;
            m_ChildEvaluation = default;
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed || m_Child == null || m_Child.Graph == null)
                return;
            if (m_Child.Graph.isRunning)
                m_Child.Graph.Stop(false);
            else
                m_Child.StopInstance();
            m_ChildFrameOpen = false;
            m_ChildLease = default;
            m_ChildPreparation = default;
            m_ChildEvaluation = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Child?.Dispose();
            m_Child = null;
            m_ChildFrameOpen = false;
            m_ImmediateInputs.Clear();
            m_DeferredInputs.Clear();
            m_OutputPorts.Clear();
        }

        void BindInputs(
            CharacterPoseNativeGraphRuntime runtime,
            bool bindDeferredPoseInputs)
        {
            var bindings = bindDeferredPoseInputs ? m_DeferredInputs : m_ImmediateInputs;
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                CharacterPoseNativePortValue value = runtime.ReadInputValue(
                    binding.Input,
                    binding.SourceNodeId,
                    binding.Parent,
                    binding.Kind);
                m_Child.BindGraphInput(binding.Child, value);
            }
        }

        void BindInterface()
        {
            m_ImmediateInputs.Clear();
            m_DeferredInputs.Clear();
            m_OutputPorts.Clear();
            CharacterPoseCanvasNode childInput = m_Child.RequireBoundary(CharacterPoseNodeKind.GraphInput);
            for (int i = 0; i < childInput.DynamicPorts.Count; i++)
            {
                CharacterPoseDynamicPort childPort = childInput.DynamicPorts[i];
                if (childPort.Direction != CharacterPosePortDirection.Output)
                    continue;
                bool deferred = IsDeferredInput(childPort.Kind);
                CharacterPoseDynamicPort parentPort = FindDynamicPort(
                    m_CallNode,
                    childPort.InterfacePortId,
                    CharacterPosePortDirection.Input);
                if (parentPort == null)
                {
                    if (childPort.Required)
                        throw new InvalidOperationException(
                            $"Pose subgraph '{NodeId}' is missing required input '{childPort.InterfacePortId}'.");
                    continue;
                }
                var inputPort = m_CallNode.GetInputPort(
                    parentPort.PortId.Value) as ValueInput;
                if (inputPort == null || !inputPort.isConnected)
                {
                    if (childPort.Required)
                        throw new InvalidOperationException(
                            $"Pose subgraph '{NodeId}' is missing required connection '{parentPort.PortId}'.");
                    continue;
                }
                var bindings = deferred ? m_DeferredInputs : m_ImmediateInputs;
                bindings.Add(new SubgraphPortBinding(
                    m_NodeId,
                    parentPort.PortId,
                    childPort.PortId,
                    inputPort,
                    childPort.Kind));
            }
            CharacterPoseCanvasNode childOutput = m_Child.RequireBoundary(CharacterPoseNodeKind.GraphOutput);
            for (int i = 0; i < m_CallNode.DynamicPorts.Count; i++)
            {
                CharacterPoseDynamicPort parentPort = m_CallNode.DynamicPorts[i];
                if (parentPort.Direction != CharacterPosePortDirection.Output)
                    continue;
                CharacterPoseDynamicPort childPort = FindDynamicPort(
                    childOutput,
                    parentPort.InterfacePortId,
                    CharacterPosePortDirection.Input);
                if (childPort == null)
                    throw new InvalidOperationException(
                        $"Pose subgraph '{NodeId}' output '{parentPort.PortId}' has no child GraphOutput binding.");
                var childInputPort = childOutput.GetInputPort(
                    childPort.PortId.Value) as ValueInput;
                m_OutputPorts.Add(parentPort.PortId, new SubgraphPortBinding(
                    childOutput.NativeNodeId,
                    parentPort.PortId,
                    childPort.PortId,
                    childInputPort,
                    childPort.Kind));
            }
        }

        static bool IsDeferredInput(CharacterPosePortKind kind) =>
            kind == CharacterPosePortKind.LocalPose ||
            kind == CharacterPosePortKind.ComponentPose ||
            kind == CharacterPosePortKind.PoseDiscontinuity ||
            kind == CharacterPosePortKind.FullBodyIkGoals ||
            kind == CharacterPosePortKind.FullBodyIkGoalContribution;

        static CharacterPoseDynamicPort FindDynamicPort(
            CharacterPoseCanvasNode node,
            PoseInterfacePortId interfacePortId,
            CharacterPosePortDirection direction)
        {
            if (node == null)
                return null;
            for (int i = 0; i < node.DynamicPorts.Count; i++)
            {
                CharacterPoseDynamicPort port = node.DynamicPorts[i];
                if (port.Direction != direction)
                    continue;
                if (port.InterfacePortId == interfacePortId)
                    return port;
            }
            return null;
        }

        void RequireChild()
        {
            if (m_Child == null)
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has no child instance.");
        }

        void RequireChildFrame()
        {
            RequireChild();
            if (!m_ChildFrameOpen)
                throw new InvalidOperationException(
                    $"Pose subgraph '{NodeId}' has no open child frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeSubgraphHandler));
        }
    }
}
