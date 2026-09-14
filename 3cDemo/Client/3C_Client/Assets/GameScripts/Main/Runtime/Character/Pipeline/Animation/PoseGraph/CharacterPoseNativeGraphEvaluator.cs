using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeNodeHandler : IDisposable
    {
        CharacterPoseNodeKind Kind { get; }
        void Initialize(CharacterPoseNativeGraphRuntime runtime);
        void Start(CharacterPoseNativeGraphRuntime runtime);
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
        readonly Dictionary<CharacterPoseNodeKind, ICharacterPoseNativeNodeHandler>
            m_Handlers =
                new Dictionary<CharacterPoseNodeKind, ICharacterPoseNativeNodeHandler>();
        readonly List<ICharacterPoseNativeNodeHandler> m_HandlerOrder =
            new List<ICharacterPoseNativeNodeHandler>();
        bool m_Disposed;

        internal CharacterPoseNativeGraphEvaluator(
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers)
        {
            Register(new ParameterInputHandler());
            Register(new ActionPlaybackInputHandler());
            if (handlers == null)
                throw new ArgumentNullException(nameof(handlers));
            for (int i = 0; i < handlers.Count; i++)
                Register(handlers[i]);
        }

        void Register(ICharacterPoseNativeNodeHandler handler)
        {
            if (handler == null)
                throw new ArgumentException(
                    "Pose native node handler is missing.",
                    nameof(handler));
            if (!Enum.IsDefined(typeof(CharacterPoseNodeKind), handler.Kind) ||
                !m_Handlers.TryAdd(handler.Kind, handler))
            {
                handler.Dispose();
                throw new ArgumentException(
                    $"Pose native node handler '{handler.Kind}' is duplicated or invalid.",
                    nameof(handler));
            }
            m_HandlerOrder.Add(handler);
        }

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (node.Kind == CharacterPoseNodeKind.OutputPose ||
                    node.Kind == CharacterPoseNodeKind.GraphOutput)
                    continue;
                RequireHandler(node).Initialize(runtime);
            }
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].Start(runtime);
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].BeginFrame(runtime, in input, in lineage);
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            var requests = new List<CharacterPoseNativeSourceRequest>();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                if (!m_Handlers.TryGetValue(node.Kind, out ICharacterPoseNativeNodeHandler handler))
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
                    requests.Add(nodeRequests[i]);
            }
            return requests;
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
            CharacterPoseCanvasNode[] outputPoses = FindNodes(
                runtime,
                CharacterPoseNodeKind.OutputPose);
            if (outputPoses.Length == 1)
                return runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(
                    outputPoses[0],
                    "pose");
            if (outputPoses.Length > 1)
                throw new InvalidOperationException(
                    $"Pose graph '{runtime.PreparedBinding.GraphId}' has multiple Output Pose nodes.");

            CharacterPoseCanvasNode[] graphOutputs = FindNodes(
                runtime,
                CharacterPoseNodeKind.GraphOutput);
            if (graphOutputs.Length != 1)
                throw new InvalidOperationException(
                    $"Pose graph '{runtime.PreparedBinding.GraphId}' has no unique graph output boundary.");
            CharacterPoseCanvasNode graphOutput = graphOutputs[0];
            CharacterPoseCanvasConnection connection = null;
            for (int i = 0; i < runtime.Graph.Connections.Count; i++)
            {
                CharacterPoseCanvasConnection candidate = runtime.Graph.Connections[i];
                if (candidate.TargetNodeId != graphOutput.NodeId)
                    continue;
                if (connection != null)
                    throw new InvalidOperationException(
                        $"Pose graph output '{graphOutput.NodeId}' has multiple inputs.");
                connection = candidate;
            }
            if (connection == null)
                throw new InvalidOperationException(
                    $"Pose graph output '{graphOutput.NodeId}' has no input.");
            CharacterPosePortDefinition targetPort = null;
            IReadOnlyList<CharacterPosePortDefinition> shape =
                CharacterPoseCanvasNativePorts.GetRuntimeShape(graphOutput);
            for (int i = 0; i < shape.Count; i++)
            {
                CharacterPosePortDefinition candidate = shape[i];
                if (candidate.Direction == CharacterPosePortDirection.Input &&
                    candidate.PortId == connection.TargetPortId)
                {
                    targetPort = candidate;
                    break;
                }
            }
            if (targetPort == null)
                throw new InvalidOperationException(
                    $"Pose graph output '{graphOutput.NodeId}' input '{connection.TargetPortId}' is not declared.");
            return ReadBoundaryInput(runtime, graphOutput, targetPort);
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
                m_HandlerOrder[i].EvaluateFrame(
                    runtime,
                    in input,
                    in lineage,
                    in demand,
                    barrierIdentity);
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].CommitFrame(runtime, in lineage, output);
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].DiscardFrame(runtime, in lineage, reason);
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            for (int i = 0; i < m_HandlerOrder.Count; i++)
                m_HandlerOrder[i].Stop(runtime);
        }

        CharacterPoseNativePortValue ReadBoundaryInput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            CharacterPosePortDefinition port)
        {
            return port.Kind switch
            {
                CharacterPosePortKind.LocalPose =>
                    runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.ComponentPose =>
                    runtime.ReadInput<CharacterPoseNativeComponentPoseValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.Parameter =>
                    runtime.ReadInput<CharacterPoseNativeParameterValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.PoseDiscontinuity =>
                    runtime.ReadInput<CharacterPoseNativeDiscontinuityValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.ActionPlayback =>
                    runtime.ReadInput<CharacterPoseNativeActionPlaybackValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.FullBodyIkGoals =>
                    runtime.ReadInput<CharacterPoseNativeFullBodyIkGoalsValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.FullBodyIkGoalContribution =>
                    runtime.ReadInput<CharacterPoseNativeGoalContributionValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.PoseHistory =>
                    runtime.ReadInput<CharacterPoseNativeHistoryValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.Trajectory =>
                    runtime.ReadInput<CharacterPoseNativeTrajectoryValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.PresentationFacts =>
                    runtime.ReadInput<CharacterPoseNativeFactsValue>(
                        node,
                        port.PortId.Value),
                CharacterPosePortKind.MotionMatchingBinding =>
                    runtime.ReadInput<CharacterPoseNativeMotionMatchingBindingValue>(
                        node,
                        port.PortId.Value),
                _ => throw new InvalidOperationException(
                    $"Pose graph output port '{port.PortId}' has an unsupported kind '{port.Kind}'.")
            };
        }

        static CharacterPoseCanvasNode[] FindNodes(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseNodeKind kind)
        {
            var result = new List<CharacterPoseCanvasNode>();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
                if (node.Kind == kind)
                    result.Add(node);
            return result.ToArray();
        }

        ICharacterPoseNativeNodeHandler RequireHandler(
            CharacterPoseCanvasNode node) =>
            m_Handlers.TryGetValue(node.Kind, out ICharacterPoseNativeNodeHandler handler)
                ? handler
                : throw new InvalidOperationException(
                    $"Pose native node '{node.NodeId}' of kind '{node.Kind}' has no registered handler.");

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
            if (failure != null)
                throw failure;
        }

        sealed class ParameterInputHandler : ICharacterPoseNativeNodeHandler
        {
            public CharacterPoseNodeKind Kind =>
                CharacterPoseNodeKind.ProgramParameterInput;

            public void Initialize(CharacterPoseNativeGraphRuntime runtime) { }
            public void Start(CharacterPoseNativeGraphRuntime runtime) { }
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
                return new CharacterPoseNativeParameterValue(
                    node.NodeId,
                    runtime.CurrentLineage.CompletionIdentity,
                    node.ParameterId,
                    runtime.CurrentInput.ParameterFrame.RequireValue(node.ParameterId));
            }

            public void EvaluateFrame(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterPoseNativeFrameInput input,
                in CharacterPoseNativeFrameLineage lineage,
                in CharacterPoseNativeSourceDemand demand,
                ulong barrierIdentity) { }

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
            public CharacterPoseNodeKind Kind =>
                CharacterPoseNodeKind.ActionPlaybackInput;

            public void Initialize(CharacterPoseNativeGraphRuntime runtime) { }
            public void Start(CharacterPoseNativeGraphRuntime runtime) { }
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
