using System;
using System.Collections.Generic;
using FlowCanvas;
using NodeCanvas.Framework;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal enum CharacterPoseNativeExecutionStage : byte
    {
        None = 0,
        Frame = 1,
        Prepare = 2,
        Evaluate = 3,
        Commit = 4
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

    internal interface ICharacterPoseNativeNodeEvaluator : IDisposable
    {
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

    internal sealed class CharacterPoseNativeGraphRuntime : ICharacterPoseCanvasNativeRuntime
    {
        readonly CharacterPoseNativePreparedBinding m_PreparedBinding;
        readonly CharacterPoseNativeCreateInstanceRequest m_CreateRequest;
        readonly ICharacterPoseNativeNodeEvaluator m_Evaluator;
        readonly Dictionary<CharacterPoseNativePortKey, CharacterPoseNativePortValue> m_OutputCache =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativePortValue>();
        readonly Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation> m_Observations =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation>();
        readonly Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation> m_CommittedObservations =
            new Dictionary<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation>();
        readonly HashSet<CharacterPoseNativePortKey> m_Evaluating =
            new HashSet<CharacterPoseNativePortKey>();
        CharacterPoseCanvasGraph m_Graph;
        CharacterPoseNativeFrameInput m_FrameInput;
        CharacterPoseNativeFrameLineage m_OpenLineage;
        CharacterPoseNativeFrameLineage m_CompletedLineage;
        CharacterPoseNativeFrameLease m_FrameLease;
        CharacterPoseNativeSourceDemand m_SourceDemand;
        CharacterPoseNativeEvaluationResult m_Evaluation;
        CharacterPoseNativePortValue m_LastCommittedOutput;
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
            m_Graph = NodeCanvas.Framework.Graph.Clone(createRequest.PreparedBinding.Graph, null);
        }

        internal CharacterPoseCanvasGraph Graph => m_Graph;
        internal CharacterPoseNativePreparedBinding PreparedBinding => m_PreparedBinding;
        internal CharacterPoseNativeInstanceContext InstanceContext => m_CreateRequest.Context;
        internal ulong InstanceId => m_CreateRequest.InstanceId;
        internal ulong ResetGeneration => m_ResetGeneration;
        internal bool IsInitialized => m_Initialized;
        internal bool IsStarted => m_Started;
        internal bool HasOpenFrame => m_FrameLease.IsValid;
        internal CharacterPoseNativeFrameLineage CurrentLineage => m_CompletedLineage;
        internal CharacterPoseNativeFrameInput CurrentInput => m_FrameInput;
        internal CharacterPoseNativePortValue LastCommittedOutput => m_LastCommittedOutput;

        internal CharacterPoseNativeGraphPrepareResult PrepareChild(
            ulong requestId,
            PoseGraphId graphId)
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
                m_PreparedBinding.ResourceRevision);
            return Prepare(in request);
        }

        internal static CharacterPoseNativeGraphPrepareResult Prepare(
            in CharacterPoseNativeGraphPrepareRequest request)
        {
            try
            {
                CharacterPoseNativeGraphValidator.RequireValid(request.GraphAsset, request.Graph);
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
                    exception.Message);
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
                    exception.Message);
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

        void AttachAndStart()
        {
            RequireAlive();
            m_Graph.AttachNativeRuntime(this);
            m_Graph.StartGraph(
                m_CreateRequest.Context.Agent,
                m_CreateRequest.Context.ParentBlackboard,
                NodeCanvas.Framework.Graph.UpdateMode.Manual,
                null);
        }

        void InitializeGraph()
        {
            RequireAlive();
            if (m_Initialized)
                throw new InvalidOperationException("Pose native graph is already initialized.");
            CharacterPoseNativeGraphValidator.RequireValid(
                m_PreparedBinding.GraphAsset,
                m_Graph);
            m_Evaluator.Initialize(this);
            m_Initialized = true;
        }

        internal CharacterPoseNativeFrameLease BeginFrame(
            in CharacterPoseNativeFrameInput input)
        {
            RequireStarted();
            if (m_FrameLease.IsValid || !input.IsValid || input.ActorId != m_PreparedBinding.ActorId)
                throw new ArgumentException("Pose native frame cannot begin from the current instance.", nameof(input));
            if (m_NextCompletionIdentity == ulong.MaxValue)
                throw new InvalidOperationException("Pose native frame completion identity was exhausted.");
            ulong completionIdentity = m_NextCompletionIdentity++;
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
            m_OutputCache.Clear();
            m_Observations.Clear();
            m_Evaluating.Clear();
            m_Stage = CharacterPoseNativeExecutionStage.Frame;
            m_Evaluator.BeginFrame(this, in input, in m_CompletedLineage);
            return m_FrameLease;
        }

        internal CharacterPoseNativePreparationResult PrepareFrame(
            CharacterPoseNativeFrameLease lease)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Frame);
            try
            {
                IReadOnlyList<CharacterPoseNativeSourceRequest> requests =
                    m_Evaluator.PrepareFrame(this, in m_FrameInput, in m_CompletedLineage) ??
                    throw new InvalidOperationException("Pose native source demand is missing.");
                m_SourceDemand = new CharacterPoseNativeSourceDemand(
                    in m_CompletedLineage,
                    requests);
                m_Stage = CharacterPoseNativeExecutionStage.Prepare;
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
                return new CharacterPoseNativePreparationResult(
                    in m_CompletedLineage,
                    CharacterPoseNativeFrameStatus.Invalid,
                    CharacterPoseNativeFailureCode.FrameInvalid,
                    "Pose/Prepare",
                    exception.Message,
                    default);
            }
        }

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

        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            RequireLease(lease);
            RequireStage(CharacterPoseNativeExecutionStage.Evaluate);
            if (!evaluation.IsValid || evaluation.Lineage != m_CompletedLineage ||
                evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated)
            {
                throw new ArgumentException("Pose native commit evaluation is invalid.", nameof(evaluation));
            }
            try
            {
                m_Stage = CharacterPoseNativeExecutionStage.Commit;
                m_Evaluator.CommitFrame(this, in m_CompletedLineage, evaluation.Output);
                m_LastCommittedOutput = evaluation.Output;
                m_CommittedObservations.Clear();
                foreach (KeyValuePair<CharacterPoseNativePortKey, CharacterPoseNativeNodeObservation> observation in m_Observations)
                    m_CommittedObservations.Add(observation.Key, observation.Value);
                m_LastCommittedLineage = m_CompletedLineage;
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

        internal void Discard(
            CharacterPoseNativeFrameLease lease,
            CharacterPoseNativeFailureCode reason)
        {
            RequireLease(lease);
            if (reason == CharacterPoseNativeFailureCode.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            m_Evaluator.DiscardFrame(this, in m_CompletedLineage, reason);
            m_Observations.Clear();
            CloseFrame();
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
                m_Evaluator.Reset(this, resetGeneration);
                m_OutputCache.Clear();
                m_Observations.Clear();
                m_CommittedObservations.Clear();
                m_Evaluating.Clear();
                m_LastCommittedOutput = null;
                m_LastCommittedLineage = default;
                m_ResetGeneration = resetGeneration;
                return CharacterPoseNativeResetResult.Succeeded(
                    m_CreateRequest.Context.ActorId,
                    InstanceId,
                    previous,
                    resetGeneration);
            }
            catch (Exception exception)
            {
                m_OutputCache.Clear();
                m_Observations.Clear();
                m_CommittedObservations.Clear();
                m_Evaluating.Clear();
                m_LastCommittedOutput = null;
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
            m_Started = false;
            m_Initialized = false;
            m_LastCommittedLineage = default;
        }
    }

}
