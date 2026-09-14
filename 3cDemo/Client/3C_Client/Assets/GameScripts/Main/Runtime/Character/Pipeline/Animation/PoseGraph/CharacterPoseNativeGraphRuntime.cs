using System;
using System.Collections.Generic;
using System.Linq;
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
            m_Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            m_Graph = NodeCanvas.Framework.Graph.Clone(createRequest.PreparedBinding.Graph, null);
        }

        internal CharacterPoseCanvasGraph Graph => m_Graph;
        internal CharacterPoseNativePreparedBinding PreparedBinding => m_PreparedBinding;
        internal CharacterPoseNativeInstanceContext InstanceContext => m_CreateRequest.Context;
        internal ulong InstanceId => m_CreateRequest.InstanceId;
        internal ulong ResetGeneration => m_CreateRequest.ResetGeneration;
        internal bool IsInitialized => m_Initialized;
        internal bool IsStarted => m_Started;
        internal bool HasOpenFrame => m_FrameLease.IsValid;
        internal CharacterPoseNativeFrameLineage CurrentLineage => m_CompletedLineage;
        internal CharacterPoseNativeFrameInput CurrentInput => m_FrameInput;
        internal CharacterPoseNativePortValue LastCommittedOutput => m_LastCommittedOutput;

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
                return RequireTyped<T>(value, node, portId);
            }
            finally
            {
                m_Evaluating.Remove(key);
            }
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
            m_Evaluating.Clear();
            m_Started = false;
            m_Initialized = false;
        }
    }

    internal sealed class CharacterPoseNativeGraphEvaluator : ICharacterPoseNativeNodeEvaluator
    {
        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            if (runtime == null || runtime.Graph == null)
                throw new ArgumentNullException(nameof(runtime));
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) { }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage) { }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            var result = new List<CharacterPoseNativeSourceRequest>();
            foreach (CharacterPoseCanvasNode node in runtime.Graph.Nodes)
            {
                CharacterPresentationPoseSourceSlot source = node.PresentationPoseSourceSlot;
                if (source)
                    result.Add(new CharacterPoseNativeSourceRequest(node.NodeId, source, true));
            }
            return result;
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            if (node.Payload is CharacterProgramParameterInputPosePayload parameter)
            {
                if (!runtime.CurrentLineage.IsValid ||
                    !runtime.CurrentLineage.IsValid ||
                    !runtime.CurrentInput.ParameterFrame.TryRead(
                        parameter.ParameterId,
                        out BTSMTL.EventGraphs.EventGraphValue value))
                {
                    throw new CharacterPoseNativeGraphValidationException(
                        CharacterPoseNativeFailureCode.FrameInvalid,
                        $"{runtime.Graph.GraphId}/{node.NodeId}/{portId}",
                        $"Pose parameter '{parameter.ParameterId}' is unavailable in the current frame.");
                }
                return new CharacterPoseNativeParameterValue(
                    node.NodeId,
                    runtime.CurrentLineage.CompletionIdentity,
                    parameter.ParameterId,
                    value);
            }
            if (node.Payload is CharacterPoseHistoryCollectorPayload &&
                portId.Value == "pose.local")
            {
                return runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(
                    node,
                    "pose.local.input");
            }
            throw new CharacterPoseNativeGraphValidationException(
                CharacterPoseNativeFailureCode.UnsupportedNode,
                $"{runtime.Graph.GraphId}/{node.NodeId}/{portId}",
                $"Pose native evaluator has no implementation for node '{node.Kind}'.");
        }

        public CharacterPoseNativePortValue EvaluateGraphOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseNativeExecutionStage stage)
        {
            CharacterPoseCanvasNode output = runtime.Graph.Nodes.SingleOrDefault(
                node => node.Kind == CharacterPoseNodeKind.OutputPose);
            if (output == null)
                throw new CharacterPoseNativeGraphValidationException(
                    CharacterPoseNativeFailureCode.GraphInvalid,
                    runtime.Graph.GraphId.Value,
                    "Pose native graph has no Output Pose node.");
            return runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(output, "pose");
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
