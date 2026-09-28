using System;
using System.Collections.Generic;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterPoseNativeExecutionStage : byte
    {
        None = 0,
        Frame = 1,
        Prepare = 2,
        Evaluate = 3,
        Validate = 4,
        Commit = 5
    }

    internal readonly struct CharacterPoseNativePortKey : IEquatable<CharacterPoseNativePortKey>
    {
        internal CharacterPoseNativePortKey(
            PoseNodeId nodeId,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            NodeId = nodeId;
            PortId = portId;
            Stage = stage;
        }

        internal PoseNodeId NodeId { get; }
        internal PosePortId PortId { get; }
        internal CharacterPoseNativeExecutionStage Stage { get; }
        public bool Equals(CharacterPoseNativePortKey other) =>
            NodeId == other.NodeId && PortId.Equals(other.PortId) && Stage == other.Stage;
        public override bool Equals(object obj) =>
            obj is CharacterPoseNativePortKey other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(NodeId, PortId, (byte)Stage);
    }

    internal readonly struct CharacterPoseNativePortDefinitionKey :
        IEquatable<CharacterPoseNativePortDefinitionKey>
    {
        internal CharacterPoseNativePortDefinitionKey(
            PoseNodeId nodeId,
            PosePortId portId,
            CharacterPosePortDirection direction)
        {
            NodeId = nodeId;
            PortId = portId;
            Direction = direction;
        }

        internal PoseNodeId NodeId { get; }
        internal PosePortId PortId { get; }
        internal CharacterPosePortDirection Direction { get; }
        public bool Equals(CharacterPoseNativePortDefinitionKey other) =>
            NodeId == other.NodeId && PortId == other.PortId && Direction == other.Direction;
        public override bool Equals(object obj) =>
            obj is CharacterPoseNativePortDefinitionKey other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(NodeId, PortId, (byte)Direction);
    }

    internal interface ICharacterPoseNativeNodeEvaluator : IDisposable
    {
        Diagnostics.ICharacterNativeStateCaptureSource StateCapture { get; }
        ICharacterPoseNativePhaseSource PhaseSources { get; }
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
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage);
        CharacterPoseNativePortValue EvaluateGraphOutput(
            CharacterPoseNativeGraphRuntime runtime,
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

    internal interface ICharacterPoseNativeNodeHandlerFactory
    {
        IReadOnlyList<ICharacterPoseNativeNodeHandler> Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context);
    }

    internal sealed class CharacterPoseNativeGraphRuntime : ICharacterPoseCanvasNativeRuntime
    {
        readonly CharacterPoseNativePreparedBinding m_PreparedBinding;
        readonly CharacterPoseNativeCreateInstanceRequest m_CreateRequest;
        readonly ICharacterPoseNativeNodeEvaluator m_Evaluator;
        readonly Dictionary<CharacterPoseNativePortKey, CharacterPoseNativePortValue> m_OutputCache =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativePortValue>();
        Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation> m_Observations =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation>();
        Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation> m_CommittedObservations =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation>();
        readonly Dictionary<PosePortId, CharacterPoseNativePortValue> m_GraphInputs =
            new Dictionary<PosePortId, CharacterPoseNativePortValue>();
        readonly HashSet<CharacterPoseNativePortKey> m_Evaluating =
            new HashSet<CharacterPoseNativePortKey>();
        readonly HashSet<CharacterPoseNativeSourceDemandKey> m_SourceDemandKeys;
        readonly Dictionary<CharacterPoseNativePortDefinitionKey,
            CharacterPosePortDefinition> m_PortDefinitions =
                new Dictionary<CharacterPoseNativePortDefinitionKey,
                    CharacterPosePortDefinition>();
        CharacterPoseCanvasGraph m_Graph;
        CharacterPoseCanvasNode m_GraphInputNode;
        CharacterPoseCanvasNode m_GraphOutputNode;
        CharacterPoseNativeFrameInput m_FrameInput;
        CharacterPoseNativeFrameLineage m_OpenLineage;
        CharacterPoseNativeFrameLineage m_CompletedLineage;
        CharacterPoseNativeFrameLease m_FrameLease;
        CharacterPoseNativeSourceDemand m_SourceDemand;
        CharacterPoseNativeEvaluationResult m_Evaluation;
        CharacterPoseNativeValidationResult m_Validation;
        CharacterPoseNativeFrameLineage m_LastCommittedLineage;
        ulong m_ResetGeneration;
        CharacterPoseNativeExecutionStage m_Stage;
        ulong m_NextCompletionIdentity = 1;
        bool m_Initialized;
        bool m_Started;
        bool m_Disposed;

        CharacterPoseNativeGraphRuntime(
            in CharacterPoseNativeCreateInstanceRequest createRequest,
            ICharacterPoseNativeNodeEvaluator evaluator)
        {
            if (!createRequest.IsValid)
                throw new ArgumentException("Pose native graph create request is invalid.", nameof(createRequest));
            m_PreparedBinding = createRequest.PreparedBinding;
            m_CreateRequest = createRequest;
            m_ResetGeneration = createRequest.ResetGeneration;
            m_Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            m_SourceDemandKeys = new HashSet<CharacterPoseNativeSourceDemandKey>(
                createRequest.Context.SourceRequestLayout.RequireGraph(m_PreparedBinding.GraphId));
            m_Graph = NodeCanvas.Framework.Graph.Clone(createRequest.PreparedBinding.Graph, null);
#if UNITY_EDITOR
            if (m_PreparedBinding.Boundary == CharacterPoseNativeGraphBoundary.Root)
                CharacterPoseNativeDomainRuntimeFactory.MarkStartup(m_PreparedBinding.ActorId, "pose-root-graph-cloned");
#endif
        }

        internal Diagnostics.CharacterNativeStateCapturePage StateCapture => new Diagnostics.CharacterNativeStateCapturePage(m_Evaluator.StateCapture);
        internal ICharacterPoseNativePhaseSource PhaseSources => m_Evaluator.PhaseSources;
        internal CharacterPoseCanvasGraph Graph => m_Graph;
        internal IReadOnlyList<CharacterPoseCanvasNode> Nodes { get; private set; }
        internal CharacterPoseNativePreparedBinding PreparedBinding => m_PreparedBinding;
        internal CharacterPoseNativeInstanceContext InstanceContext => m_CreateRequest.Context;
        internal ulong InstanceId => m_CreateRequest.InstanceId;
        internal ulong ResetGeneration => m_ResetGeneration;
        internal bool IsInitialized => m_Initialized;
        internal bool IsStarted => m_Started;
        internal bool HasOpenFrame => m_FrameLease.IsValid;
        internal CharacterPoseNativeFrameLineage CurrentLineage => m_CompletedLineage;
        internal CharacterPoseNativeFrameInput CurrentInput => m_FrameInput;

        internal CharacterPoseNativeGraphPrepareResult PrepareChild(
            ulong requestId,
            PoseGraphId graphId,
            CharacterPoseNativeGraphBoundary boundary)
        {
            RequireAlive();
            CharacterPoseCanvasGraph graph =
                m_PreparedBinding.GraphAsset.RequireGraph(graphId);
            var request = new CharacterPoseNativeGraphPrepareRequest(
                requestId,
                m_PreparedBinding.ActorId,
                m_PreparedBinding.GraphAsset,
                graph,
                m_PreparedBinding.Profile,
                m_PreparedBinding.Rig,
                m_PreparedBinding.InputContract,
                m_PreparedBinding.ResourceRevision,
                boundary);
            return Prepare(in request);
        }

        internal CharacterPoseNativeAdoptedResult CreateChild(
            ulong requestId,
            PoseGraphId graphId,
            CharacterPoseNativeGraphBoundary boundary,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers,
            out CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            if (instanceId == InstanceId)
                throw new ArgumentException(
                    "Pose child graph cannot reuse its parent instance identity.",
                    nameof(instanceId));
            CharacterPoseNativeGraphPrepareResult preparation =
                PrepareChild(requestId, graphId, boundary);
            if (!preparation.IsReady)
            {
                runtime = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in preparation,
                    resetGeneration,
                    preparation.FailureCode,
                    preparation.Message);
            }
            CharacterPoseNativeInstanceContext context = InstanceContext;
            CharacterPoseNativePreparedBinding preparedBinding =
                preparation.PreparedBinding;
            return Create(
                in preparedBinding,
                in context,
                instanceId,
                resetGeneration,
                reason,
                handlers,
                out runtime);
        }

        internal CharacterPoseNativeAdoptedResult CreateChild(
            ulong requestId,
            PoseGraphId graphId,
            CharacterPoseNativeGraphBoundary boundary,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeHandlerFactory factory,
            out CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (instanceId == InstanceId)
                throw new ArgumentException(
                    "Pose child graph cannot reuse its parent instance identity.",
                    nameof(instanceId));
            CharacterPoseNativeGraphPrepareResult preparation =
                PrepareChild(requestId, graphId, boundary);
            if (!preparation.IsReady)
            {
                runtime = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in preparation,
                    resetGeneration,
                    preparation.FailureCode,
                    preparation.Message);
            }
            CharacterPoseNativePreparedBinding preparedBinding =
                preparation.PreparedBinding;
            CharacterPoseNativeInstanceContext context = InstanceContext;
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers =
                factory.Create(in preparedBinding, in context);
            if (handlers == null)
                throw new InvalidOperationException(
                    "Pose child handler factory returned no handler list.");
            return Create(
                in preparedBinding,
                in context,
                instanceId,
                resetGeneration,
                reason,
                handlers,
                out runtime);
        }

        internal static CharacterPoseNativeGraphPrepareResult Prepare(
            in CharacterPoseNativeGraphPrepareRequest request)
        {
            try
            {
                CharacterPoseNativeGraphValidator.RequireValid(
                    request.GraphAsset,
                    request.Graph,
                    request.Boundary);
                return CharacterPoseNativeGraphPrepareResult.Ready(
                    in request,
                    new CharacterPoseNativePreparedBinding(in request));
            }
            catch (CharacterPoseNativeGraphValidationException exception)
            {
                return CharacterPoseNativeGraphPrepareResult.Failed(
                    in request,
                    CharacterPoseNativePreparationStatus.Invalid,
                    exception.Code,
                    exception.Origin,
                    exception.Message);
            }
            catch (Exception exception)
            {
                return CharacterPoseNativeGraphPrepareResult.Failed(
                    in request,
                    CharacterPoseNativePreparationStatus.Failed,
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    request.Graph?.GraphId.Value ?? "Pose",
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        internal static CharacterPoseNativeAdoptedResult Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeEvaluator evaluator,
            out CharacterPoseNativeGraphRuntime runtime)
        {
            var request = new CharacterPoseNativeCreateInstanceRequest(
                in preparedBinding,
                in context,
                instanceId,
                resetGeneration,
                reason);
            runtime = null;
            try
            {
                runtime = new CharacterPoseNativeGraphRuntime(in request, evaluator);
                runtime.AttachAndStart();
                return CharacterPoseNativeAdoptedResult.Adopted(in request);
            }
            catch (Exception exception)
            {
                runtime?.Dispose();
                runtime = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in request,
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    "Pose/Create",
                    $"{exception.GetType().Name}: {exception.Message}");
            }
        }

        internal static CharacterPoseNativeAdoptedResult Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers,
            out CharacterPoseNativeGraphRuntime runtime) =>
            Create(
                in preparedBinding,
                in context,
                instanceId,
                resetGeneration,
                reason,
                new CharacterPoseNativeGraphEvaluator(handlers),
                out runtime);

        internal static CharacterPoseNativeAdoptedResult Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeHandlerFactory factory,
            out CharacterPoseNativeGraphRuntime runtime)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            IReadOnlyList<ICharacterPoseNativeNodeHandler> handlers =
                factory.Create(in preparedBinding, in context);
            if (handlers == null)
                throw new InvalidOperationException(
                    "Pose native handler factory returned no handler list.");
            return Create(
                in preparedBinding,
                in context,
                instanceId,
                resetGeneration,
                reason,
                handlers,
                out runtime);
        }

        void AttachAndStart()
        {
            RequireAlive();
            m_Graph.AttachNativeRuntime(this);
#if UNITY_EDITOR
            if (m_PreparedBinding.Boundary == CharacterPoseNativeGraphBoundary.Root)
                CharacterPoseNativeDomainRuntimeFactory.MarkStartup(m_PreparedBinding.ActorId, "pose-root-runtime-attached");
#endif
            m_Graph.StartGraph(
                m_CreateRequest.Context.Animancer,
                null,
                NodeCanvas.Framework.Graph.UpdateMode.Manual,
                null);
#if UNITY_EDITOR
            if (m_PreparedBinding.Boundary == CharacterPoseNativeGraphBoundary.Root)
                CharacterPoseNativeDomainRuntimeFactory.MarkStartup(m_PreparedBinding.ActorId, "pose-root-graph-started");
#endif
        }

        void InitializeGraph()
        {
            RequireAlive();
            if (m_Initialized)
                throw new InvalidOperationException("Pose native graph is already initialized.");
            Nodes = m_Graph.Nodes;
            BuildPortDefinitions();
            m_Evaluator.Initialize(this);
            m_Initialized = true;
        }

        void BuildPortDefinitions()
        {
            m_PortDefinitions.Clear();
            int outputPortCount = 0;
            int graphInputCount = 0;
            for (int nodeIndex = 0; nodeIndex < Nodes.Count; nodeIndex++)
            {
                CharacterPoseCanvasNode node = Nodes[nodeIndex];
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    if (m_GraphInputNode != null)
                        throw new InvalidOperationException("Pose native graph has multiple Graph Input boundaries.");
                    m_GraphInputNode = node;
                }
                else if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    if (m_GraphOutputNode != null)
                        throw new InvalidOperationException("Pose native graph has multiple Graph Output boundaries.");
                    m_GraphOutputNode = node;
                }
                IReadOnlyList<CharacterPosePortDefinition> shape =
                    CharacterPoseCanvasNativePorts.GetRuntimeShape(node);
                for (int i = 0; i < shape.Count; i++)
                {
                    CharacterPosePortDefinition port = shape[i];
                    var key = new CharacterPoseNativePortDefinitionKey(
                        node.NodeId,
                        port.PortId,
                        port.Direction);
                    if (!m_PortDefinitions.TryAdd(key, port))
                        throw new InvalidOperationException(
                            $"Pose node '{node.NodeId}' contains duplicate port '{port.PortId}'.");
                    if (port.Direction == CharacterPosePortDirection.Output)
                    {
                        outputPortCount++;
                        if (node.Kind == CharacterPoseNodeKind.GraphInput)
                            graphInputCount++;
                    }
                }
            }
            int frameOutputCount = checked(outputPortCount * 2);
            m_OutputCache.EnsureCapacity(frameOutputCount);
            m_Observations.EnsureCapacity(frameOutputCount);
            m_CommittedObservations.EnsureCapacity(frameOutputCount);
            m_Evaluating.EnsureCapacity(frameOutputCount);
            m_GraphInputs.EnsureCapacity(graphInputCount);
        }
        internal CharacterPoseNativeFrameLease BeginFrame(
            in CharacterPoseNativeFrameInput input)
        {
            RequireStarted();
            if (m_NextCompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Pose native frame completion identity was exhausted.");
            return BeginFrame(in input, m_NextCompletionIdentity);
        }

        internal CharacterPoseNativeFrameLease BeginFrame(
            in CharacterPoseNativeFrameInput input,
            ulong completionIdentity)
        {
            RequireStarted();
            if (m_FrameLease.IsValid || !input.IsValid || input.ActorId != m_PreparedBinding.ActorId)
                throw new ArgumentException("Pose native frame cannot begin from the current instance.", nameof(input));
            if (completionIdentity == 0 ||
                completionIdentity < m_NextCompletionIdentity ||
                completionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Pose native frame completion identity was exhausted.");
            m_NextCompletionIdentity = completionIdentity + 1;
            m_OpenLineage = new CharacterPoseNativeFrameLineage(
                input.ActorId,
                input.FrameIdentity,
                0,
                input.PresentationFrame,
                input.BodyTick,
                m_PreparedBinding.GraphId,
                m_PreparedBinding.GraphRevision,
                m_PreparedBinding.RigId,
                m_PreparedBinding.RigRevision,
                m_PreparedBinding.InputContractHash,
                InstanceId,
                ResetGeneration);
            m_CompletedLineage = m_OpenLineage.WithCompletion(completionIdentity);
            m_FrameLease = new CharacterPoseNativeFrameLease(in m_OpenLineage);
            m_FrameInput = input;
            m_SourceDemand = default;
            m_Evaluation = default;
            m_Validation = default;
            m_GraphInputs.Clear();
            m_OutputCache.Clear();
            m_Observations.Clear();
            m_Evaluating.Clear();
            m_Stage = CharacterPoseNativeExecutionStage.Frame;
            try
            {
                m_Evaluator.BeginFrame(this, in input, in m_CompletedLineage);
                return m_FrameLease;
            }
            catch
            {
                Discard(m_FrameLease, CharacterPoseNativeFailureCode.FrameInvalid);
                throw;
            }
        }

        [PerformanceProbe("presentation.animation.pose-graph.prepare")]
        internal CharacterPoseNativePreparationResult PrepareFrame(
            CharacterPoseNativeFrameLease lease)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Frame);
            try
            {
                m_Stage = CharacterPoseNativeExecutionStage.Prepare;
                IReadOnlyList<CharacterPoseNativeSourceRequest> requests =
                    m_Evaluator.PrepareFrame(this, in m_FrameInput, in m_CompletedLineage) ??
                    throw new InvalidOperationException("Pose native source demand is missing.");
                m_SourceDemand = new CharacterPoseNativeSourceDemand(
                    in m_CompletedLineage,
                    requests,
                    m_SourceDemandKeys);
                return new CharacterPoseNativePreparationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Prepared,
                    CharacterPoseNativeFailureCode.None,
                    "Pose",
                    "Pose source demand is prepared.",
                    in m_SourceDemand);
            }
            catch (Exception exception)
            {
                m_Stage = CharacterPoseNativeExecutionStage.Prepare;
                string message = $"{exception.GetType().Name}: {exception.Message}";
                if (!m_CompletedLineage.IsValid)
                    throw new InvalidOperationException(
                        $"Pose frame prepare failed on an invalid lineage. {message}", exception);
                return new CharacterPoseNativePreparationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Invalid,
                    CharacterPoseNativeFailureCode.FrameInvalid,
                    "Pose/Prepare",
                    message,
                    default);
            }
        }

        internal void BindGraphInput(
            PosePortId portId,
            CharacterPoseNativePortValue value)
        {
            RequireStarted();
            if (m_Stage != CharacterPoseNativeExecutionStage.Frame &&
                m_Stage != CharacterPoseNativeExecutionStage.Prepare)
            {
                throw new InvalidOperationException(
                    "Pose native graph input binding is only available before evaluation.");
            }
            if (!portId.IsValid || value == null ||
                value.CompletionIdentity != m_CompletedLineage.CompletionIdentity)
            {
                throw new ArgumentException(
                    "Pose native graph input binding is invalid.",
                    nameof(value));
            }
            CharacterPosePortDefinition definition = FindPort(
                RequireBoundary(CharacterPoseNodeKind.GraphInput),
                portId,
                CharacterPosePortDirection.Output);
            if (definition == null ||
                !CharacterPoseCanvasNativePorts.RuntimeBindingType(definition.Kind)
                    .IsInstanceOfType(value))
            {
                throw new InvalidOperationException(
                    $"Pose graph input '{portId}' is not declared with the supplied native type.");
            }
            if (!m_GraphInputs.TryAdd(portId, value))
                throw new InvalidOperationException(
                    $"Pose graph input '{portId}' was bound more than once.");
        }

        internal CharacterPoseNativePortValue ReadGraphInput(
            PosePortId portId)
        {
            RequireEvaluationStage();
            if (!m_GraphInputs.TryGetValue(portId, out CharacterPoseNativePortValue value) ||
                value.CompletionIdentity != m_CompletedLineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Pose graph input '{portId}' has no value for the current frame.");
            }
            return value;
        }

        internal CharacterPoseNativePortValue ReadInputValue(
            CharacterPoseCanvasNode node,
            PosePortId portId)
        {
            RequireEvaluationStage();
            CharacterPosePortDefinition definition = FindPort(
                node,
                portId,
                CharacterPosePortDirection.Input);
            if (definition == null)
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' input '{portId}' is not declared.");
            return ReadInputValue(node, definition);
        }

        internal CharacterPoseNativePortValue ReadInputValue(
            CharacterPoseCanvasNode node,
            CharacterPosePortDefinition definition)
        {
            RequireEvaluationStage();
            if (node == null || definition == null ||
                definition.Direction != CharacterPosePortDirection.Input)
            {
                throw new ArgumentException(
                    "Pose native input binding is invalid.");
            }
            PosePortId portId = definition.PortId;
            return definition.Kind switch
            {
                CharacterPosePortKind.LocalPose =>
                    ReadInput<CharacterPoseNativeLocalPoseValue>(node, portId.Value),
                CharacterPosePortKind.ComponentPose =>
                    ReadInput<CharacterPoseNativeComponentPoseValue>(node, portId.Value),
                CharacterPosePortKind.Parameter =>
                    ReadInput<CharacterPoseNativeParameterValue>(node, portId.Value),
                CharacterPosePortKind.PoseDiscontinuity =>
                    ReadInput<CharacterPoseNativeDiscontinuityValue>(node, portId.Value),
                CharacterPosePortKind.ActionPlayback =>
                    ReadInput<CharacterPoseNativeActionPlaybackValue>(node, portId.Value),
                CharacterPosePortKind.FullBodyIkGoals =>
                    ReadInput<CharacterPoseNativeFullBodyIkGoalsValue>(node, portId.Value),
                CharacterPosePortKind.FullBodyIkGoalContribution =>
                    ReadInput<CharacterPoseNativeGoalContributionValue>(node, portId.Value),
                CharacterPosePortKind.PresentationFacts =>
                    ReadInput<CharacterPoseNativeFactsValue>(node, portId.Value),
                CharacterPosePortKind.MotionMatchingBinding =>
                    ReadInput<CharacterPoseNativeMotionMatchingBindingValue>(node, portId.Value),
                _ => throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' input '{portId}' has unsupported kind '{definition.Kind}'.")
            };
        }

        internal CharacterPoseNativePortValue ReadGraphOutput(
            PosePortId portId)
        {
            RequireEvaluationStage();
            return ReadInputValue(RequireBoundary(CharacterPoseNodeKind.GraphOutput), portId);
        }

        internal CharacterPoseCanvasNode RequireBoundary(CharacterPoseNodeKind kind) =>
            (kind switch
            {
                CharacterPoseNodeKind.GraphInput => m_GraphInputNode,
                CharacterPoseNodeKind.GraphOutput => m_GraphOutputNode,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            }) ?? throw new InvalidOperationException(
                $"Pose native graph '{m_PreparedBinding.GraphId}' has no '{kind}' boundary.");

        internal CharacterPoseNativePortValue ReadOutput()
        {
            RequireEvaluationStage();
            return m_Evaluator.EvaluateGraphOutput(
                       this,
                       m_Stage) ??
                throw new InvalidOperationException(
                    $"Pose native graph '{m_PreparedBinding.GraphId}' has no evaluated output.");
        }

        internal void PrepareEvaluation(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Prepare);
            if (!demand.IsValid || demand.Lineage != m_CompletedLineage ||
                barrierIdentity == 0)
            {
                throw new ArgumentException(
                    "Pose native evaluation preparation input is invalid.",
                    nameof(demand));
            }
            m_Evaluator.PrepareEvaluation(
                this,
                in m_CompletedLineage,
                in demand,
                barrierIdentity);
        }

        [PerformanceProbe("presentation.animation.pose-graph.evaluate")]
        internal CharacterPoseNativeEvaluationResult Evaluate(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Prepare);
            if (!demand.IsValid || demand.Lineage != m_CompletedLineage || barrierIdentity == 0)
                throw new ArgumentException("Pose native evaluation input is invalid.", nameof(demand));
            try
            {
                m_Stage = CharacterPoseNativeExecutionStage.Evaluate;
                m_Evaluator.EvaluateFrame(
                    this,
                    in m_FrameInput,
                    in m_CompletedLineage,
                    in demand,
                    barrierIdentity);
                CharacterPoseNativePortValue output =
                    m_Evaluator.EvaluateGraphOutput(
                        this,
                        CharacterPoseNativeExecutionStage.Evaluate) ??
                    throw new InvalidOperationException("Pose native graph output is missing.");
                if (!output.IsValid ||
                    output.CompletionIdentity !=
                    m_CompletedLineage.CompletionIdentity)
                {
                    throw new InvalidOperationException(
                        "Pose native graph output does not match the current completion identity.");
                }
                m_Evaluation = new CharacterPoseNativeEvaluationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Evaluated,
                    CharacterPoseNativeFailureCode.None,
                    "Pose/Evaluate",
                    "Pose graph evaluation completed.",
                    output);
                return m_Evaluation;
            }
            catch (Exception exception)
            {
                m_Stage = CharacterPoseNativeExecutionStage.Evaluate;
                m_Evaluation = new CharacterPoseNativeEvaluationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Faulted,
                    CharacterPoseNativeFailureCode.FrameInvalid,
                    "Pose/Evaluate",
                    exception.Message);
                return m_Evaluation;
            }
        }

        internal CharacterPoseNativeValidationResult ValidatePending(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Evaluate);
            if (!evaluation.IsValid || evaluation.Lineage != m_CompletedLineage ||
                evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated)
            {
                throw new ArgumentException(
                    "Pose native pending validation input is invalid.",
                    nameof(evaluation));
            }
            try
            {
                m_Stage = CharacterPoseNativeExecutionStage.Validate;
                m_Evaluator.ValidatePending(
                    this,
                    in m_CompletedLineage);
                m_Validation = CharacterPoseNativeValidationResult.Succeeded(
                    in m_CompletedLineage,
                    "Pose/Validate",
                    "Pose graph pending output is valid for commit.");
                return m_Validation;
            }
            catch (Exception exception)
            {
                m_Stage = CharacterPoseNativeExecutionStage.Validate;
                m_Validation = CharacterPoseNativeValidationResult.Failed(
                    in m_CompletedLineage,
                    exception is CharacterPoseNativeGraphValidationException validation
                        ? validation.Code
                        : CharacterPoseNativeFailureCode.FrameInvalid,
                    "Pose/Validate",
                    exception.Message);
                return m_Validation;
            }
        }

        [PerformanceProbe("presentation.animation.pose-graph.commit")]
        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Validate);
            if (!evaluation.IsValid || evaluation.Lineage != m_CompletedLineage ||
                evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated ||
                !m_Validation.IsValidated ||
                m_Validation.Lineage != m_CompletedLineage)
            {
                throw new ArgumentException("Pose native commit evaluation is invalid.", nameof(evaluation));
            }
            try
            {
                m_Stage = CharacterPoseNativeExecutionStage.Commit;
                CommitGraphOutput(in evaluation);
                CharacterPoseNativePublicationResult result = new CharacterPoseNativePublicationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Committed,
                    CharacterPoseNativeFailureCode.None,
                    m_CompletedLineage.CompletionIdentity,
                    "Pose/Commit",
                    "Pose graph frame committed.");
                CloseFrame();
                return result;
            }
            catch (Exception exception)
            {
                m_Evaluator.DiscardFrame(
                    this,
                    in m_CompletedLineage,
                    CharacterPoseNativeFailureCode.PublicationFailed);
                m_Observations.Clear();
                CloseFrame();
                return new CharacterPoseNativePublicationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Faulted,
                    CharacterPoseNativeFailureCode.PublicationFailed,
                    0,
                    "Pose/Commit",
                    exception.Message);
            }
        }

        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation,
            CharacterFinalPoseNativePublication publication,
            bool captureFootIkDiagnostics)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Validate);
            if (!evaluation.IsValid || evaluation.Lineage != m_CompletedLineage ||
                evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated ||
                !m_Validation.IsValidated ||
                m_Validation.Lineage != m_CompletedLineage)
            {
                throw new ArgumentException(
                    "Pose native final publication evaluation is invalid.",
                    nameof(evaluation));
            }
            if (publication == null)
                throw new ArgumentNullException(nameof(publication));
            CharacterPoseNativePublicationFrameLease publicationLease = default;
            bool publicationOpen = false;
            bool graphCommitted = false;
            try
            {
                publicationLease = publication.BeginFrame(in m_CompletedLineage);
                publicationOpen = true;
                publication.PreparePending(publicationLease, in evaluation);
                publication.WritePhysicalPose(
                    publicationLease,
                    captureFootIkDiagnostics);
                m_Stage = CharacterPoseNativeExecutionStage.Commit;
                CommitGraphOutput(in evaluation);
                graphCommitted = true;
                CharacterPoseNativePublicationResult result =
                    publication.Commit(publicationLease);
                CloseFrame();
                return result;
            }
            catch (Exception exception)
            {
                Exception failure = exception;
                try
                {
                    if (publicationOpen)
                        publication.Discard(publicationLease);
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException(
                        "Pose native final publication cleanup failed.",
                        failure,
                        cleanup);
                }
                if (!graphCommitted)
                {
                    try
                    {
                        m_Evaluator.DiscardFrame(
                            this,
                            in m_CompletedLineage,
                            CharacterPoseNativeFailureCode.PublicationFailed);
                    }
                    catch (Exception cleanup)
                    {
                        failure = new AggregateException(
                            "Pose native graph cleanup failed.",
                            failure,
                            cleanup);
                    }
                }
                m_Observations.Clear();
                CharacterPoseNativeFrameLineage lineage = m_CompletedLineage;
                CloseFrame();
                return new CharacterPoseNativePublicationResult(
                    in lineage,
                    CharacterPoseNativeFrameStatus.Faulted,
                    CharacterPoseNativeFailureCode.PublicationFailed,
                    0,
                    "Pose/FinalPublication",
                    failure.Message);
            }
        }

        internal void Discard(
            CharacterPoseNativeFrameLease lease,
            CharacterPoseNativeFailureCode reason)
        {
            RequireLease(lease);
            if (reason == CharacterPoseNativeFailureCode.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            try
            {
                m_Evaluator.DiscardFrame(this, in m_CompletedLineage, reason);
            }
            finally
            {
                m_Observations.Clear();
                CloseFrame();
            }
        }

        void CommitGraphOutput(
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            m_Evaluator.CommitFrame(
                this,
                in m_CompletedLineage,
                evaluation.Output);
            (m_CommittedObservations, m_Observations) = (m_Observations, m_CommittedObservations);
            m_Observations.Clear();
            m_LastCommittedLineage = m_CompletedLineage;
        }

        internal void StopInstance()
        {
            if (m_Disposed)
                return;
            if (m_FrameLease.IsValid)
                Discard(m_FrameLease, CharacterPoseNativeFailureCode.Disposed);
            if (m_Started)
            {
                m_Evaluator.Stop(this);
                m_Started = false;
            }
        }

        internal void ResetForStateEntry()
        {
            RequireStarted();
            if (m_FrameLease.IsValid)
                throw new InvalidOperationException("Pose state entry requires a closed graph frame.");
            ResetState(m_ResetGeneration);
        }

        void ResetState(ulong resetGeneration)
        {
            m_Evaluator.Reset(this, resetGeneration);
            m_OutputCache.Clear();
            m_Observations.Clear();
            m_CommittedObservations.Clear();
            m_Evaluating.Clear();
            m_LastCommittedLineage = default;
        }

        internal CharacterPoseNativeResetResult ResetInstance(
            ulong resetGeneration)
        {
            if (m_Disposed)
                return CharacterPoseNativeResetResult.Failed(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    m_ResetGeneration,
                    CharacterPoseNativeFailureCode.Disposed,
                    "Pose graph instance is disposed.");
            if (!m_Started)
                return CharacterPoseNativeResetResult.Failed(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    m_ResetGeneration,
                    CharacterPoseNativeFailureCode.Stale,
                    "Pose graph instance is not running.");
            if (resetGeneration == 0 || resetGeneration <= m_ResetGeneration)
                return CharacterPoseNativeResetResult.Failed(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    m_ResetGeneration,
                    CharacterPoseNativeFailureCode.Stale,
                    "Pose graph reset generation is not newer than the current instance.");
            ulong previous = m_ResetGeneration;
            try
            {
                if (m_FrameLease.IsValid)
                    Discard(
                        m_FrameLease,
                        CharacterPoseNativeFailureCode.Stale);
                ResetState(resetGeneration);
                m_ResetGeneration = resetGeneration;
                return CharacterPoseNativeResetResult.Succeeded(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    previous,
                    resetGeneration);
            }
            catch (Exception exception)
            {
                StopInstance();
                m_OutputCache.Clear();
                m_Observations.Clear();
                m_CommittedObservations.Clear();
                m_Evaluating.Clear();
                m_LastCommittedLineage = default;
                return CharacterPoseNativeResetResult.Failed(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    previous,
                    CharacterPoseNativeFailureCode.FrameInvalid,
                    exception.Message);
            }
        }

        internal T ReadInput<T>(
            CharacterPoseCanvasNode node,
            string portId)
            where T : CharacterPoseNativePortValue
        {
            RequireEvaluationStage();
            FlowCanvas.ValueInput<T> input = node.GetInputPort(portId) as FlowCanvas.ValueInput<T>;
            if (input == null)
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' input '{portId}' is not a typed native input.");
            return input.value ??
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' input '{portId}' has no value.");
        }

        T ICharacterPoseCanvasNativeRuntime.Read<T>(
            CharacterPoseCanvasNode node,
            PosePortId portId)
        {
            RequireEvaluationStage();
            if (node == null || !portId.IsValid)
                throw new ArgumentException("Pose native output request is invalid.");
            var key = new CharacterPoseNativePortKey(node.NodeId, portId, m_Stage);
            if (m_OutputCache.TryGetValue(key, out CharacterPoseNativePortValue cached))
                return RequireTyped<T>(cached, node, portId);
            if (!m_Evaluating.Add(key))
                throw new InvalidOperationException(
                    $"Pose native graph output cycle reached at '{node.NodeId}/{portId}'.");
            try
            {
                CharacterPoseNativePortValue value =
                    m_Evaluator.EvaluateOutput(this, node, portId, m_Stage) ??
                    throw new InvalidOperationException(
                        $"Pose native node '{node.NodeId}' output '{portId}' is missing.");
                m_OutputCache.Add(key, value);
                m_Observations[key] = new CharacterPoseNativeNodeObservation(
                    m_PreparedBinding.GraphId,
                    node.NodeId,
                    portId,
                    InstanceId,
                    m_CompletedLineage.CompletionIdentity,
                    m_Stage == CharacterPoseNativeExecutionStage.Prepare
                        ? CharacterPoseNativeFrameStatus.Prepared
                        : CharacterPoseNativeFrameStatus.Evaluated,
                    CharacterPoseNativeFailureCode.None,
                    "Pose native node output is available.");
                return RequireTyped<T>(value, node, portId);
            }
            catch (Exception exception)
            {
                m_Observations[key] = new CharacterPoseNativeNodeObservation(
                    m_PreparedBinding.GraphId,
                    node.NodeId,
                    portId,
                    InstanceId,
                    m_CompletedLineage.CompletionIdentity,
                    CharacterPoseNativeFrameStatus.Faulted,
                    exception is CharacterPoseNativeGraphValidationException validation
                        ? validation.Code
                        : CharacterPoseNativeFailureCode.FrameInvalid,
                    exception.Message);
                throw;
            }
            finally
            {
                m_Evaluating.Remove(key);
            }
        }

        public bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation)
        {
            observation = default;
            if (m_Disposed || !nodeId.IsValid || !portId.IsValid ||
                !m_LastCommittedLineage.IsValid)
                return false;
            CharacterPoseNativePortKey key = new CharacterPoseNativePortKey(
                nodeId,
                portId,
                CharacterPoseNativeExecutionStage.Evaluate);
            if (!m_CommittedObservations.TryGetValue(key, out observation) ||
                observation.InstanceId != InstanceId ||
                observation.CompletionIdentity != m_LastCommittedLineage.CompletionIdentity)
            {
                key = new CharacterPoseNativePortKey(
                    nodeId,
                    portId,
                    CharacterPoseNativeExecutionStage.Prepare);
                if (!m_CommittedObservations.TryGetValue(key, out observation) ||
                    observation.InstanceId != InstanceId ||
                    observation.CompletionIdentity != m_LastCommittedLineage.CompletionIdentity)
                {
                    observation = default;
                    return false;
                }
            }
            return true;
        }

        public void Initialize(CharacterPoseCanvasGraph graph)
        {
            if (!ReferenceEquals(graph, m_Graph))
                throw new InvalidOperationException("Pose native runtime initialized by an unrelated graph.");
            InitializeGraph();
        }

        public void Start(CharacterPoseCanvasGraph graph)
        {
            if (!ReferenceEquals(graph, m_Graph) || !m_Initialized || m_Started)
                throw new InvalidOperationException("Pose native runtime start is invalid.");
            m_Evaluator.Start(this);
            m_Started = true;
        }

        public void Stop(CharacterPoseCanvasGraph graph)
        {
            if (!ReferenceEquals(graph, m_Graph))
                throw new InvalidOperationException("Pose native runtime stopped by an unrelated graph.");
            StopInstance();
        }

        void CloseFrame()
        {
            m_FrameLease = default;
            m_SourceDemand = default;
            m_Evaluation = default;
            m_Validation = default;
            m_GraphInputs.Clear();
            m_FrameInput = default;
            m_OpenLineage = default;
            m_Stage = CharacterPoseNativeExecutionStage.None;
        }

        static T RequireTyped<T>(
            CharacterPoseNativePortValue value,
            CharacterPoseCanvasNode node,
            PosePortId portId)
            where T : CharacterPoseNativePortValue => value as T ??
                throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' output '{portId}' returned '{value.GetType().Name}', expected '{typeof(T).Name}'.");

        CharacterPosePortDefinition FindPort(
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPosePortDirection direction)
        {
            if (node == null || !portId.IsValid)
                return null;
            var key = new CharacterPoseNativePortDefinitionKey(
                node.NodeId,
                portId,
                direction);
            return m_PortDefinitions.TryGetValue(key, out CharacterPosePortDefinition definition)
                ? definition
                : null;
        }

        void RequireLease(CharacterPoseNativeFrameLease lease)
        {
            RequireStarted();
            if (!m_FrameLease.IsValid || !lease.Matches(m_FrameLease.Lineage))
                throw new InvalidOperationException("Pose native frame lease is stale.");
        }

        void RequireStage(CharacterPoseNativeExecutionStage stage)
        {
            if (m_Stage != stage)
                throw new InvalidOperationException(
                    $"Pose native runtime is in stage '{m_Stage}', expected '{stage}'.");
        }

        void RequireEvaluationStage()
        {
            if (m_Stage != CharacterPoseNativeExecutionStage.Prepare &&
                m_Stage != CharacterPoseNativeExecutionStage.Evaluate)
            {
                throw new InvalidOperationException("Pose native output is unavailable outside Prepare or Evaluate.");
            }
        }

        void RequireStarted()
        {
            RequireAlive();
            if (!m_Started)
                throw new InvalidOperationException("Pose native graph instance is not running.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseNativeGraphRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            StopInstance();
            if (m_Graph != null && m_Graph.isRunning)
                m_Graph.Stop(false);
            m_Disposed = true;
            m_Evaluator.Dispose();
            if (m_Graph != null)
            {
                if (!m_Graph.isRunning)
                    m_Graph.DetachNativeRuntime(this);
                UnityEngine.Object.Destroy(m_Graph);
                m_Graph = null;
            }
            m_OutputCache.Clear();
            m_Observations.Clear();
            m_CommittedObservations.Clear();
            m_Evaluating.Clear();
            m_GraphInputs.Clear();
            m_PortDefinitions.Clear();
            m_Started = false;
            Nodes = null;
            m_GraphInputNode = null;
            m_GraphOutputNode = null;
            m_Initialized = false;
            m_LastCommittedLineage = default;
        }
    }

}
