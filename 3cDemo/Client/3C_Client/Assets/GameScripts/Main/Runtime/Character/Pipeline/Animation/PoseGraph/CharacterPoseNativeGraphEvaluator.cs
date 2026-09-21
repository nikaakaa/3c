using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.EventGraphs;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeNodeHandler : IDisposable
    {
        PoseNodeId NodeId { get; }
        CharacterPoseNodeKind Kind { get; }
        void Initialize(CharacterPoseNativeGraphRuntime runtime);
        void Start(CharacterPoseNativeGraphRuntime runtime);
        void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration);
        void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage);
        void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity);
        void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity);
        void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage);
        void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output);
        void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason);
        void Stop(CharacterPoseNativeGraphRuntime runtime);
    }

    internal sealed class CharacterPoseNativeGraphEvaluator :
        ICharacterPoseNativeNodeEvaluator
    {
        readonly Dictionary<PoseNodeId, ICharacterPoseNativeNodeHandler>
            m_Handlers =
                new Dictionary<PoseNodeId, ICharacterPoseNativeNodeHandler>();
        readonly List<ICharacterPoseNativeNodeHandler> m_HandlerOrder =
            new List<ICharacterPoseNativeNodeHandler>();
        readonly HashSet<PoseNodeId> m_ReachableNodeIds =
            new HashSet<PoseNodeId>();
        readonly List<CharacterPoseNativeSourceRequest> m_SourceRequests;
        CharacterPoseCanvasNode m_OutputPose;
        CharacterPoseCanvasNode m_GraphOutput;
        CharacterPosePortDefinition m_GraphOutputPort;
        bool m_Disposed;

        internal CharacterPoseNativeGraphEvaluator(
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers)
        {
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));
            m_SourceRequests =
                new List<CharacterPoseNativeSourceRequest>(handlers.Count);
            for (int i = 0; i < handlers.Count; i++)
                Register(handlers[i]);
        }

        void Register(ICharacterPoseNativeNodeHandler handler)
        {
            if (handler == null)
                throw new ArgumentException(
                    "Pose native node handler is missing.",
                    nameof(handler));
            if (!handler.NodeId.IsValid ||
                !m_Handlers.TryAdd(handler.NodeId, handler))
            {
                handler.Dispose();
                throw new ArgumentException(
                    $"Pose native node handler '{handler.NodeId}' is duplicated or invalid.",
                    nameof(handler));
            }
            m_HandlerOrder.Add(handler);
        }

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            var graphNodeIds = new HashSet<PoseNodeId>();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (!graphNodeIds.Add(node.NodeId))
                    throw new InvalidOperationException(
                        $"Pose graph '{runtime.PreparedBinding.GraphId}' contains duplicate node '{node.NodeId}'.");
                if (node.Kind == CharacterPoseNodeKind.OutputPose ||
                    node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    if (m_Handlers.ContainsKey(node.NodeId))
                        throw new InvalidOperationException(
                            $"Pose boundary node '{node.NodeId}' cannot have a native handler.");
                    continue;
                }
                if (!m_Handlers.ContainsKey(node.NodeId))
                {
                    ICharacterPoseNativeNodeHandler builtin =
                        CreateBuiltinHandler(node);
                    if (builtin != null)
                        Register(builtin);
                }
                RequireHandler(node).Initialize(runtime);
            }
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                ICharacterPoseNativeNodeHandler handler = m_HandlerOrder[i];
                if (!graphNodeIds.Contains(handler.NodeId))
                    throw new InvalidOperationException(
                        $"Pose native node handler '{handler.NodeId}' is not present in graph '{runtime.PreparedBinding.GraphId}'.");
                CharacterPoseCanvasNode node = runtime.Graph.RequireNode(handler.NodeId);
                if (node.Kind != handler.Kind)
                    throw new InvalidOperationException(
                        $"Pose native node handler '{handler.NodeId}' has kind '{handler.Kind}', expected '{node.Kind}'.");
            }
            BuildReachableNodeIds(runtime);
            BindGraphOutput(runtime);
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].Start(runtime);
        }

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            if (resetGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(resetGeneration));
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].Reset(runtime, resetGeneration);
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].BeginFrame(runtime, in input, in lineage);
            }
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            m_SourceRequests.Clear();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (!m_ReachableNodeIds.Contains(node.NodeId))
                    continue;
                if (!m_Handlers.TryGetValue(node.NodeId, out ICharacterPoseNativeNodeHandler handler))
                {
                    if (node.Kind == CharacterPoseNodeKind.OutputPose ||
                        node.Kind == CharacterPoseNodeKind.GraphOutput)
                        continue;
                    throw new InvalidOperationException(
                        $"Pose native node '{node.NodeId}' has no registered handler.");
                }
                IReadOnlyList<CharacterPoseNativeSourceRequest> nodeRequests =
                    handler.PrepareFrame(runtime, node, in input, in lineage);
                if (nodeRequests == null)
                    continue;
                for (int i = 0; i < nodeRequests.Count; i++)
                    m_SourceRequests.Add(nodeRequests[i]);
            }
            return m_SourceRequests;
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            return RequireHandler(node).EvaluateOutput(
                runtime,
                node,
                portId,
                stage);
        }

        public CharacterPoseNativePortValue EvaluateGraphOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (m_OutputPose != null)
                return runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(
                    m_OutputPose,
                    "pose");
            return runtime.ReadInputValue(m_GraphOutput, m_GraphOutputPort);
        }

        void BindGraphOutput(CharacterPoseNativeGraphRuntime runtime)
        {
            m_OutputPose = null;
            m_GraphOutput = null;
            m_GraphOutputPort = null;
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (node.Kind != CharacterPoseNodeKind.OutputPose)
                    continue;
                if (m_OutputPose != null)
                    throw new InvalidOperationException(
                        $"Pose graph '{runtime.PreparedBinding.GraphId}' has multiple Output Pose nodes.");
                m_OutputPose = node;
            }
            if (m_OutputPose != null)
                return;
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (node.Kind != CharacterPoseNodeKind.GraphOutput)
                    continue;
                if (m_GraphOutput != null)
                    throw new InvalidOperationException(
                        $"Pose graph '{runtime.PreparedBinding.GraphId}' has no unique graph output boundary.");
                m_GraphOutput = node;
            }
            if (m_GraphOutput == null)
                throw new InvalidOperationException(
                    $"Pose graph '{runtime.PreparedBinding.GraphId}' has no unique graph output boundary.");
            CharacterPoseCanvasConnection connection = null;
            for (int i = 0; i < runtime.Graph.Connections.Count; i++)
            {
                CharacterPoseCanvasConnection candidate = runtime.Graph.Connections[i];
                if (candidate.TargetNodeId != m_GraphOutput.NodeId)
                    continue;
                if (connection != null)
                    throw new InvalidOperationException(
                        $"Pose graph output '{m_GraphOutput.NodeId}' has multiple inputs.");
                connection = candidate;
            }
            if (connection == null)
                throw new InvalidOperationException(
                    $"Pose graph output '{m_GraphOutput.NodeId}' has no input.");
            IReadOnlyList<CharacterPosePortDefinition> shape =
                CharacterPoseCanvasNativePorts.GetRuntimeShape(m_GraphOutput);
            for (int i = 0; i < shape.Count; i++)
            {
                CharacterPosePortDefinition candidate = shape[i];
                if (candidate.Direction == CharacterPosePortDirection.Input &&
                    candidate.PortId == connection.TargetPortId)
                {
                    m_GraphOutputPort = candidate;
                    break;
                }
            }
            if (m_GraphOutputPort == null)
                throw new InvalidOperationException(
                    $"Pose graph output '{m_GraphOutput.NodeId}' input '{connection.TargetPortId}' is not declared.");
        }

        public void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            if (barrierIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(barrierIdentity));
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].EvaluateFrame(
                        runtime,
                        in input,
                        in lineage,
                        in demand,
                        barrierIdentity);
            }
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            if (barrierIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(barrierIdentity));
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].PrepareEvaluation(
                        runtime,
                        in lineage,
                        in demand,
                        barrierIdentity);
            }
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].ValidatePending(
                        runtime,
                        in lineage);
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].CommitFrame(runtime, in lineage, output);
            }
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
            {
                if (m_ReachableNodeIds.Contains(m_HandlerOrder[i].NodeId))
                    m_HandlerOrder[i].DiscardFrame(runtime, in lineage, reason);
            }
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].Stop(runtime);
        }

        void BuildReachableNodeIds(CharacterPoseNativeGraphRuntime runtime)
        {
            m_ReachableNodeIds.Clear();
            var pending = new Queue<PoseNodeId>();
            foreach (CharacterPoseCanvasNode boundary in runtime.Graph.Nodes)
            {
                if (boundary.Kind != CharacterPoseNodeKind.OutputPose &&
                    boundary.Kind != CharacterPoseNodeKind.GraphOutput)
                    continue;
                for (int i = 0; i < runtime.Graph.Connections.Count; i++)
                {
                    CharacterPoseCanvasConnection connection =
                        runtime.Graph.Connections[i];
                    if (connection.TargetNodeId == boundary.NodeId)
                        pending.Enqueue(connection.SourceNodeId);
                }
            }
            while (pending.Count > 0)
            {
                PoseNodeId nodeId = pending.Dequeue();
                if (!m_ReachableNodeIds.Add(nodeId))
                    continue;
                for (int i = 0; i < runtime.Graph.Connections.Count; i++)
                {
                    CharacterPoseCanvasConnection connection =
                        runtime.Graph.Connections[i];
                    if (connection.TargetNodeId == nodeId)
                        pending.Enqueue(connection.SourceNodeId);
                }
            }
        }

        ICharacterPoseNativeNodeHandler RequireHandler(
            CharacterPoseCanvasNode node) =>
            m_Handlers.TryGetValue(node.NodeId, out ICharacterPoseNativeNodeHandler handler)
                ? handler
                : throw new InvalidOperationException(
                    $"Pose native node '{node.NodeId}' of kind '{node.Kind}' has no registered handler.");

        static ICharacterPoseNativeNodeHandler CreateBuiltinHandler(
            CharacterPoseCanvasNode node) =>
            node.Kind switch
            {
                CharacterPoseNodeKind.ProgramParameterInput =>
                    new ParameterInputHandler(node.NodeId),
                CharacterPoseNodeKind.ActionPlaybackInput =>
                    new ActionPlaybackInputHandler(node.NodeId),
                CharacterPoseNodeKind.GraphInput =>
                    new GraphInputHandler(node.NodeId),
                _ => null
            };

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseNativeGraphEvaluator));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            for (int i = m_HandlerOrder.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_HandlerOrder[i].Dispose();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }
            m_HandlerOrder.Clear();
            m_Handlers.Clear();
            m_ReachableNodeIds.Clear();
            if (failure != null)
                throw failure;
        }

        sealed class ParameterInputHandler : ICharacterPoseNativeNodeHandler
        {
            PoseParameterValueType m_ValueType;
            bool m_Initialized;

            internal ParameterInputHandler(PoseNodeId nodeId) => NodeId = nodeId;

            public PoseNodeId NodeId { get; }
            public CharacterPoseNodeKind Kind =>
                CharacterPoseNodeKind.ProgramParameterInput;

            public void Initialize(CharacterPoseNativeGraphRuntime runtime)
            {
                if (m_Initialized)
                    throw new InvalidOperationException(
                        $"Pose parameter node '{NodeId}' was initialized twice.");
                CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
                if (!node.ParameterId.IsValid)
                    throw new InvalidOperationException(
                        $"Pose parameter node '{NodeId}' has no parameter identity.");
                CharacterPoseParameterDeclaration declaration =
                    runtime.PreparedBinding.InputContract.Parameters
                        .SingleOrDefault(value =>
                            value != null &&
                            value.ParameterId.Equals(node.ParameterId));
                if (declaration == null ||
                    declaration.Usage != CharacterPoseParameterUsage.Control ||
                    !CharacterPoseParameterAccess.IsBlackboardInput(declaration))
                {
                    throw new InvalidOperationException(
                        $"Pose parameter node '{NodeId}' does not reference a read-only EventGraph Control parameter.");
                }
                m_ValueType = declaration.ValueType;
                m_Initialized = true;
            }
            public void Start(CharacterPoseNativeGraphRuntime runtime) { }
            public void Reset(
                CharacterPoseNativeGraphRuntime runtime,
                ulong resetGeneration) { }
            public void BeginFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) { }
            public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) => null;

            public CharacterPoseNativePortValue EvaluateOutput(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                PosePortId portId,
                CharacterPoseNativeExecutionStage stage)
            {
                if (portId.Value != "parameter")
                    throw new InvalidOperationException(
                        $"Pose parameter node '{node.NodeId}' has no output '{portId}'.");
                EventGraphValue value =
                    runtime.CurrentInput.ParameterFrame.RequireValue(node.ParameterId);
                if (m_ValueType == PoseParameterValueType.Float &&
                    value.Kind != EventGraphValueKind.Float32 ||
                    m_ValueType == PoseParameterValueType.Int &&
                    value.Kind != EventGraphValueKind.Int32 ||
                    m_ValueType == PoseParameterValueType.Bool &&
                    value.Kind != EventGraphValueKind.Bool)
                {
                    throw new InvalidOperationException(
                        $"Pose parameter '{node.ParameterId}' EventGraph value type does not match its declaration.");
                }
                return new CharacterPoseNativeParameterValue(
                    node.NodeId,
                    runtime.CurrentLineage.CompletionIdentity,
                    node.ParameterId,
                    value);
            }

            public void EvaluateFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void PrepareEvaluation(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void ValidatePending(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage) { }

            public void CommitFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativePortValue output) { }

            public void DiscardFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativeFailureCode reason) { }

            public void Stop(CharacterPoseNativeGraphRuntime runtime) { }
            public void Dispose() { }
        }

        sealed class ActionPlaybackInputHandler : ICharacterPoseNativeNodeHandler
        {
            internal ActionPlaybackInputHandler(PoseNodeId nodeId) => NodeId = nodeId;

            public PoseNodeId NodeId { get; }
            public CharacterPoseNodeKind Kind =>
                CharacterPoseNodeKind.ActionPlaybackInput;

            public void Initialize(CharacterPoseNativeGraphRuntime runtime) { }
            public void Start(CharacterPoseNativeGraphRuntime runtime) { }
            public void Reset(
                CharacterPoseNativeGraphRuntime runtime,
                ulong resetGeneration) { }
            public void BeginFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) { }
            public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) => null;

            public CharacterPoseNativePortValue EvaluateOutput(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                PosePortId portId,
                CharacterPoseNativeExecutionStage stage)
            {
                if (portId.Value != "action-playback")
                    throw new InvalidOperationException(
                        $"Pose action input node '{node.NodeId}' has no output '{portId}'.");
                for (int i = runtime.CurrentInput.ActionCommands.Count - 1; i >= 0; i--)
                {
                    ActionAnimationPlaybackCommand command =
                        runtime.CurrentInput.ActionCommands[i];
                    if (command.AnimationChannelId != node.AnimationChannelId)
                        continue;
                    return new CharacterPoseNativeActionPlaybackValue(
                        node.NodeId,
                        runtime.CurrentLineage.CompletionIdentity,
                        command);
                }
                throw new InvalidOperationException(
                    $"Pose action input node '{node.NodeId}' has no command for channel '{node.AnimationChannelId}'.");
            }

            public void EvaluateFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void PrepareEvaluation(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void ValidatePending(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage) { }

            public void CommitFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativePortValue output) { }

            public void DiscardFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativeFailureCode reason) { }

            public void Stop(CharacterPoseNativeGraphRuntime runtime) { }
            public void Dispose() { }
        }

        sealed class GraphInputHandler : ICharacterPoseNativeNodeHandler
        {
            internal GraphInputHandler(PoseNodeId nodeId) => NodeId = nodeId;

            public PoseNodeId NodeId { get; }
            public CharacterPoseNodeKind Kind =>
                CharacterPoseNodeKind.GraphInput;

            public void Initialize(CharacterPoseNativeGraphRuntime runtime) { }
            public void Start(CharacterPoseNativeGraphRuntime runtime) { }
            public void Reset(
                CharacterPoseNativeGraphRuntime runtime,
                ulong resetGeneration) { }
            public void BeginFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) { }
            public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage) => null;

            public CharacterPoseNativePortValue EvaluateOutput(
                CharacterPoseNativeGraphRuntime runtime,
                CharacterPoseCanvasNode node,
                PosePortId portId,
                CharacterPoseNativeExecutionStage stage) =>
                runtime.ReadGraphInput(portId);

            public void EvaluateFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void PrepareEvaluation(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

            public void ValidatePending(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage) { }

            public void CommitFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativePortValue output) { }

            public void DiscardFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameLineage lineage,
                CharacterPoseNativeFailureCode reason) { }

            public void Stop(CharacterPoseNativeGraphRuntime runtime) { }
            public void Dispose() { }
        }
    }
}
