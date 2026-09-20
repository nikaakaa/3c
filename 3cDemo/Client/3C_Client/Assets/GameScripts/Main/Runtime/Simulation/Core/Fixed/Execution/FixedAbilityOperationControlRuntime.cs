using System;
using System.IO;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedAbilityExecutionServices
    {
        FixedAbilityExecutionTarget Target { get; }
        void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow);
        void EndEvaluation();
    }

    internal sealed class FixedTreeClipInvokerLink
    {
        public IAbilityTreeClipInvoker Invoker;
    }

    internal sealed class FixedAbilityOperationControlRuntime : IFixedAbilityOperationControlRuntime, IAbilityTreeClipInvoker
    {
        readonly IFixedAbilityExecutionServices m_Services;
        readonly Dictionary<string, OperationHandle> m_TreeClipEntries;
        readonly string m_AbilityId;
        readonly FixedTreeClipInvokerLink m_TreeClipLink = new FixedTreeClipInvokerLink();
        OperationControlRuntime<FixedAbilityExecutionTarget> m_Runtime;

        public FixedAbilityOperationControlRuntime(
            FixedGameplayAbilityExecutionData data,
            IFixedAbilityExecutionServices services,
            FixedTreeClipInvokerLink treeClipLink)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_TreeClipLink = treeClipLink ?? throw new ArgumentNullException(nameof(treeClipLink));
            m_TreeClipLink.Invoker = this;
            m_TreeClipEntries = BuildTreeClipEntries(data);
            m_AbilityId = data.AbilityId.Value;
            m_Runtime = new OperationControlRuntime<FixedAbilityExecutionTarget>(
                data.Topology,
                m_Services.Target,
                checked(Math.Max(1024, data.Operations.Count * 128)));
        }

        internal void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow)
        {
            m_Services.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
            m_Runtime.BeginEvaluation();
        }
        internal void EndEvaluation() => m_Services.EndEvaluation();
        public OperationExecutionResult Tick(OperationHandle operation) => m_Runtime.Tick(operation);

        public bool InvokeTreeClip(in AbilityTreeClipInvocation invocation)
        {
            if (!m_TreeClipEntries.TryGetValue(TreeClipKey(invocation.ClipAuthoringId, invocation.Hook), out OperationHandle entry))
            {
                if (invocation.Hook != AbilityTreeClipHook.OnEnable)
                    return false;
                throw new InvalidOperationException(
                    $"TreeClip '{invocation.ClipAuthoringId}' has no compiled OnEnable invocation in Ability '{m_AbilityId}'.");
            }
            m_Services.Target.BeginTreeClipInvocation(invocation);
            OperationExecutionResult result;
            try
            {
                result = invocation.Hook == AbilityTreeClipHook.Root
                    ? m_Runtime.TickPersistent(entry)
                    : m_Runtime.Tick(entry);
            }
            finally
            {
                m_Services.Target.EndTreeClipInvocation();
            }
            if (result == OperationExecutionResult.Failure)
                throw new InvalidOperationException(
                    $"TreeClip '{invocation.ClipAuthoringId}' {invocation.Hook} invocation failed in Ability '{m_AbilityId}'.");
            return true;
        }

        static string TreeClipKey(string clipAuthoringId, AbilityTreeClipHook hook) =>
            string.Concat(clipAuthoringId, "|", hook.ToString("G"));

        static Dictionary<string, OperationHandle> BuildTreeClipEntries(FixedGameplayAbilityExecutionData data)
        {
            var entries = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
            foreach (ProgramSourceMapEntry entry in data.SourceMap)
            {
                if (entry.TargetKind != ProgramSourceTargetKind.GraphInvocation ||
                    entry.InvocationCallerKind != ProgramInvocationCallerKind.TimelineClip ||
                    string.IsNullOrEmpty(entry.InvocationCallerClipId) ||
                    !Enum.TryParse(entry.InvocationCallerId, out AbilityTreeClipHook hook))
                {
                    continue;
                }
                if (!entries.TryAdd(TreeClipKey(entry.InvocationCallerClipId, hook), new OperationHandle(entry.TargetIndex)))
                    throw new InvalidDataException(
                        $"Ability '{data.AbilityId.Value}' has duplicate TreeClip invocation '{entry.InvocationCallerClipId}/{entry.InvocationCallerId}'.");
            }
            return entries;
        }

        public bool IsActive(OperationHandle operation) => m_Runtime.IsActive(operation);
        public bool IsStopping(OperationHandle operation) => m_Runtime.IsStopping(operation);
        public OperationStopStatus RequestStop(OperationHandle operation, OperationStopContext context) =>
            m_Runtime.RequestStop(operation, context);
        public OperationStopStatus ContinueStop(OperationHandle operation) => m_Runtime.ContinueStop(operation);
        public void ForceStop(OperationHandle operation, OperationStopContext context) => m_Runtime.ForceStop(operation, context);
        public OperationRunnableStatus ReadStatus(OperationHandle operation) => m_Runtime.ReadStatus(operation);
        public ulong ReadGeneration(OperationHandle operation) => m_Runtime.ReadGeneration(operation);
    }

    internal readonly struct FixedAbilityExecutionTarget :
        IOperationControlTarget<FixedAbilityExecutionTarget>,
        IOperationControlEdgeTraceTarget
    {
        readonly FixedGameplayAbilityExecutionAccess m_Access;
        readonly FixedStatePort m_ControlState;
        readonly FixedOperationStateReset m_ResetState;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedFactSink m_Facts;
        readonly FixedPresentationSink m_Presentation;
        readonly FixedTraceSink m_Trace;
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        readonly FixedActionStateStore m_ActionState;
        readonly SimulationTick m_Tick;
        readonly FixedTreeClipInvokerLink m_TreeClipLink;
        readonly List<IAbilityTimelinePending> m_TimelinePendingAdvances;
        readonly List<IAbilityTimelineStopPending> m_TimelinePendingStops;

        public FixedAbilityExecutionTarget(
            FixedGameplayAbilityExecutionAccess access,
            FixedStatePort controlState,
            FixedOperationStateReset resetState,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard,
            FixedActionRuntime actions,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedLocomotionRuntime locomotion,
            FixedFactSink facts,
            FixedPresentationSink presentation,
            FixedTraceSink trace,
            IAbilityTimelineRuntime timelineRuntime,
            FixedActionStateStore actionState,
            List<IAbilityTimelinePending> timelineAdvances,
            List<IAbilityTimelineStopPending> timelineStops,
            SimulationTick tick,
            FixedTreeClipInvokerLink treeClipLink)
        {
            m_Access = access;
            m_ControlState = controlState;
            m_ResetState = resetState;
            m_Values = values;
            m_Blackboard = blackboard;
            m_Actions = actions;
            m_GameplayEffects = gameplayEffects;
            m_Equipment = equipment;
            m_Locomotion = locomotion;
            m_Facts = facts;
            m_Presentation = presentation;
            m_Trace = trace;
            m_TimelineRuntime = timelineRuntime;
            m_ActionState = actionState;
            m_TimelinePendingAdvances = timelineAdvances ??
                throw new ArgumentNullException(nameof(timelineAdvances));
            m_TimelinePendingStops = timelineStops ??
                throw new ArgumentNullException(nameof(timelineStops));
            m_Tick = tick;
            m_TreeClipLink = treeClipLink;
        }

        public bool DiagnosticsEnabled => m_Trace.Enabled;
        public bool ControlTraceEnabled => m_Trace.CaptureControlFlow;
        public int ReadInt32(int slotIndex) => m_ControlState.Get(slotIndex).Int32;
        public void WriteInt32(int slotIndex, int value) => m_ControlState.Set(slotIndex, AbilityStateValue.FromInt32(value));
        public ulong ReadUInt64(int slotIndex) => m_ControlState.Get(slotIndex).UInt64;
        public void WriteUInt64(int slotIndex, ulong value) => m_ControlState.Set(slotIndex, AbilityStateValue.FromUInt64(value));
        public string ReadIdentity(int slotIndex) => m_ControlState.Get(slotIndex).Identity;
        public void WriteIdentity(int slotIndex, string value) => m_ControlState.Set(slotIndex, AbilityStateValue.FromIdentity(value));
        public void TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed) =>
            m_Trace.AddControlFlow(edge, selected, passed);

        internal void BeginTreeClipInvocation(in AbilityTreeClipInvocation invocation)
        {
            m_Presentation.BeginTreeClipInvocation(invocation, invocation.ActionInstanceId);
        }

        internal void EndTreeClipInvocation() => m_Presentation.EndTreeClipInvocation();

        public bool EvaluateCondition(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            ProgramControlFlowEdge edge) => m_Values.EvaluateCondition(cursor, edge);

        public OperationExecutionResult ExecuteLeaf(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor descriptor)
        {
            SimulationOperation operation = m_Access.Operation(descriptor.Handle);
            switch (descriptor.Code)
            {
                case SimulationOperationCode.BlackboardSet:
                    return m_Values.SetBlackboard(cursor, operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.ActivateActionInstance:
                    return m_Actions.Activate(cursor, operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.SubmitActionLifecycle:
                    return m_Actions.SubmitLifecycle(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.GameplayEffectApply:
                    return RequireGameplayEffects().Apply(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.GameplayEffectRemove:
                    return RequireGameplayEffects().Remove(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.RequestEquipmentChange:
                case SimulationOperationCode.BeginEquipmentChange:
                case SimulationOperationCode.CommitEquipmentChange:
                case SimulationOperationCode.CancelEquipmentChange:
                    using (FixedValueInputLease inputs = m_Values.ReadInputs(cursor, operation))
                        return RequireEquipment().Execute(cursor, operation, inputs)
                            ? OperationExecutionResult.Success
                            : OperationExecutionResult.Failure;
                case SimulationOperationCode.LocomotionInputMotion:
                    return TickLocomotion(cursor, operation);
                case SimulationOperationCode.StateRootCompleted:
                case SimulationOperationCode.StateExitCause:
                case SimulationOperationCode.BlackboardGet:
                case SimulationOperationCode.InputBoolean:
                case SimulationOperationCode.InputScalar:
                case SimulationOperationCode.InputVector2:
                case SimulationOperationCode.InputVector2Magnitude:
                case SimulationOperationCode.InputRequest:
                case SimulationOperationCode.MoveFacingAngle:
                case SimulationOperationCode.CharacterStateRead:
                case SimulationOperationCode.ActionContextActive:
                case SimulationOperationCode.ActionWindowActive:
                case SimulationOperationCode.CanActivateAction:
                case SimulationOperationCode.GameplayEffectHasTag:
                case SimulationOperationCode.GameplayEffectMatchTags:
                case SimulationOperationCode.GameplayAttributeRead:
                case SimulationOperationCode.CameraBasisRead:
                case SimulationOperationCode.ConditionResult:
                case SimulationOperationCode.Compare:
                case SimulationOperationCode.And:
                case SimulationOperationCode.Or:
                case SimulationOperationCode.Not:
                case SimulationOperationCode.Constant:
                case SimulationOperationCode.ReadEquipmentIdentity:
                case SimulationOperationCode.ReadEquipmentParameter:
                    return FixedValueRuntime.ToBoolean(m_Values.Evaluate(cursor, operation.Handle))
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.Timeline:
                    return TickTimeline(operation);
                case SimulationOperationCode.TimelineClipExitRequest:
                    return RequestTreeClipExit(operation);
                case SimulationOperationCode.CameraStateRequest:
                case SimulationOperationCode.CameraResponse:
                case SimulationOperationCode.CameraTarget:
                case SimulationOperationCode.CameraEffectRequest:
                    return SubmitCameraRequest(operation);
                case SimulationOperationCode.Root:
                case SimulationOperationCode.Loop:
                case SimulationOperationCode.Parallel:
                case SimulationOperationCode.Sequence:
                case SimulationOperationCode.Selector:
                case SimulationOperationCode.Succeed:
                case SimulationOperationCode.SubGraph:
                case SimulationOperationCode.StateMachine:
                case SimulationOperationCode.State:
                case SimulationOperationCode.StateOnEnter:
                case SimulationOperationCode.StateOnExit:
                case SimulationOperationCode.TimelineEnter:
                    throw new InvalidOperationException(
                        $"Portable control operation '{descriptor.Code}' reached the Ability leaf dispatcher.");
                case SimulationOperationCode.StateEnter:
                case SimulationOperationCode.StateAny:
                case SimulationOperationCode.StateExit:
                case SimulationOperationCode.TimelineAnimation:
                case SimulationOperationCode.TimelineMotionCurve:
                    case SimulationOperationCode.TimelineCue:
                case SimulationOperationCode.TimelineCameraState:
                case SimulationOperationCode.TimelineCameraCue:
                case SimulationOperationCode.TimelineCameraResponse:
                    throw new InvalidOperationException(
                        $"Timeline content operation '{descriptor.Code}' cannot execute as an Ability leaf.");
                default:
                    throw new InvalidOperationException(
                        $"Ability operation '{descriptor.Handle}' code '{descriptor.Code}' has no Fixed owner.");
            }
        }

        OperationExecutionResult TickLocomotion(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            SimulationOperation operation)
        {
            int slot = m_Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.LocomotionMotionElapsedTicks);
            int committedTicks = checked(m_ControlState.Get(slot).Int32 + 1);
            int generationSlot = m_Access.RequireOperationSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            m_Locomotion.Submit(
                cursor,
                operation,
                committedTicks,
                m_ControlState.Get(generationSlot).UInt64);
            var mode = (LocomotionInputMotionExecutionMode)operation.Integer0;
            if (mode == LocomotionInputMotionExecutionMode.Once)
                return OperationExecutionResult.Success;
            m_ControlState.Set(slot, AbilityStateValue.FromInt32(committedTicks));
            if (mode == LocomotionInputMotionExecutionMode.Continuous)
                return OperationExecutionResult.Running;
            if (mode != LocomotionInputMotionExecutionMode.Timed)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has invalid execution mode '{operation.Integer0}'.");
            ProgramConstant duration = m_Access.FindConstant(operation, OperationNamedConstant.DurationSeconds);
            if (duration == null || duration.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has no duration.");
            int requiredTicks = checked((int)Math.Ceiling(duration.Scalar.ToDouble() * m_Access.Data.TickRate));
            if (requiredTicks <= 0)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has a non-positive timed duration.");
            return committedTicks >= requiredTicks
                ? OperationExecutionResult.Success
                : OperationExecutionResult.Running;
        }

        OperationExecutionResult RequestTreeClipExit(SimulationOperation operation)
        {
            if (m_TimelineRuntime == null)
                throw new InvalidOperationException($"Timeline operation '{m_Access.SourcePath(operation)}' has no Timeline runtime binding.");
            if (!m_Presentation.HasTreeClipInvocation)
                throw new InvalidOperationException($"Timeline Clip exit operation '{m_Access.SourcePath(operation)}' must execute inside a TreeClip invocation.");
            AbilityTreeClipInvocation invocation = m_Presentation.TreeClipInvocation;
            _ = m_TimelineRuntime.RequestTreeClipExit(
                invocation.TimelineRuntimeHandle,
                invocation.ClipAuthoringId);
            return OperationExecutionResult.Success;
        }

        OperationExecutionResult TickTimeline(SimulationOperation operation)
        {
            if (m_TimelineRuntime == null)
                throw new InvalidOperationException($"Ability Timeline operation '{m_Access.SourcePath(operation)}' has no Timeline runtime binding.");
            int slot = m_Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.TimelinePlayback);
            int runtimeHandle = m_ControlState.Get(slot).Int32;
            if (runtimeHandle == 0)
            {
                if (!m_ActionState.TryGetCurrentSkillExecution(out FixedActionInstanceState action) || !action.IsActive)
                    throw new InvalidOperationException($"Ability Timeline operation '{m_Access.SourcePath(operation)}' has no active Action context.");
                var actionContext = new TimelineActionContextIdentity(
                    action.ActionId,
                    action.ContextId,
                    action.InstanceId,
                    action.PredictionKey,
                    action.SkillId,
                    action.SkillEntryOperation,
                    action.SkillExecutionGeneration);
                var request = new AbilityTimelineStartRequest(
                    operation.Text0,
                    operation.Integer0 == (int)AbilityTimelinePlaybackMode.Loop,
                    actionContext,
                    CreateTimelineInvocationSource(operation),
                    action.InputSequence,
                    m_Tick);
                runtimeHandle = m_TimelineRuntime.Start(in request);
                if (runtimeHandle == 0)
                    throw new InvalidOperationException($"Ability Timeline operation '{m_Access.SourcePath(operation)}' did not return a runtime handle.");
                m_ControlState.Set(slot, AbilityStateValue.FromInt32(runtimeHandle));
            }
            if (!m_ActionState.TryGetCurrentSkillExecution(out FixedActionInstanceState sourceAction) || !sourceAction.IsActive)
                throw new InvalidOperationException("Timeline progress has no owning Action instance.");
            IAbilityTreeClipInvokerHost treeClipInvokerHost = m_TimelineRuntime as IAbilityTreeClipInvokerHost;
            if (treeClipInvokerHost != null && m_TreeClipLink?.Invoker != null)
                treeClipInvokerHost.PushTreeClipInvoker(m_TreeClipLink.Invoker);
            AbilityTimelineTickResult tick;
            try
            {
                tick = m_TimelineRuntime.Tick(runtimeHandle, m_Tick.Value, 1);
            }
            finally
            {
                treeClipInvokerHost?.PopTreeClipInvoker();
            }
            if (tick.Pending != null)
                m_TimelinePendingAdvances.Add(tick.Pending);
            if (tick.Progress.IsValid)
            {
                SimulationEventHeader header = m_Presentation.Next(operation);
                m_Presentation.Add(new PresentationCommand(header, PresentationCommandKind.TimelineProgress,
                    m_Access.SourcePath(operation),
                    FixedScalar.Zero, FixedScalar.Zero,
                    sourceActionInstanceId: sourceAction.InstanceId,
                    timelineProgress: tick.Progress));
            }
            return tick.Status switch
            {
                AbilityTimelineRuntimeStatus.Running => OperationExecutionResult.Running,
                AbilityTimelineRuntimeStatus.Succeeded => OperationExecutionResult.Success,
                AbilityTimelineRuntimeStatus.Failed => OperationExecutionResult.Failure,
                AbilityTimelineRuntimeStatus.Cancelled => OperationExecutionResult.Failure,
                _ => throw new InvalidOperationException("Ability Timeline operation returned an invalid status.")
            };
        }

        AbilityTimelineInvocationSource CreateTimelineInvocationSource(SimulationOperation operation)
        {
            ProgramSourceMapEntry source = m_Access.RequireOperationSource(operation);
            int generationSlot = m_Access.RequireOperationSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            ulong generation = m_ControlState.Get(generationSlot).UInt64;
            if (generation == 0)
                throw new InvalidOperationException($"Ability Timeline operation '{m_Access.SourcePath(operation)}' has no active invocation generation.");
            return new AbilityTimelineInvocationSource(
                operation.Handle.Value,
                source.GraphId,
                source.NodeId,
                source.GraphInvocationPath,
                generation);
        }

        OperationExecutionResult SubmitCameraRequest(SimulationOperation operation)
        {
            if (!m_Presentation.HasTreeClipInvocation)
                throw new InvalidOperationException(
                    $"Camera operation '{m_Access.SourcePath(operation)}' must execute inside a TreeClip invocation.");
            ulong actionInstanceId = m_Presentation.TreeClipActionInstanceId;
            if (actionInstanceId == 0)
                throw new InvalidOperationException(
                    $"Camera operation '{m_Access.SourcePath(operation)}' has no active Action context.");
            AbilityTreeClipInvocation invocation = m_Presentation.TreeClipInvocation;
            PresentationCameraRequestLifecycle lifecycle = invocation.Hook == AbilityTreeClipHook.OnEnable
                ? PresentationCameraRequestLifecycle.Activate
                : PresentationCameraRequestLifecycle.Retire;
            PresentationCameraRequest request = operation.Code switch
            {
                SimulationOperationCode.CameraStateRequest => PresentationCameraRequest.Sequence(
                    lifecycle,
                    RequireString(operation, "SequenceId"),
                    operation.Integer1,
                    checked((int)operation.Flags),
                    RequireInt32(operation, "Priority"),
                    RequireScalar(operation, "Weight"),
                    RequireScalar(operation, "BlendInSeconds"),
                    RequireScalar(operation, "BlendOutSeconds"),
                    RequireOptionalString(operation, "TargetKey"),
                    m_Access.GetStringConstant(operation, "ActionContext", string.Empty)),
                SimulationOperationCode.CameraEffectRequest => PresentationCameraRequest.Effect(
                    lifecycle,
                    RequireString(operation, "RequestId"),
                    operation.Integer1,
                    RequireString(operation, "ResourceId"),
                    RequireInt32(operation, "Priority"),
                    RequireScalar(operation, "Weight"),
                    m_Access.GetStringConstant(operation, "ActionContext", string.Empty)),
                SimulationOperationCode.CameraResponse => PresentationCameraRequest.Response(
                    lifecycle,
                    operation.Integer1,
                    RequireScalar(operation, "ManualOrbitWeight"),
                    RequireScalar(operation, "PitchResponseWeight"),
                    RequireScalar(operation, "YawResponseWeight"),
                    RequireInt32(operation, "Priority"),
                    RequireScalar(operation, "Weight"),
                    m_Access.GetStringConstant(operation, "ActionContext", string.Empty)),
                SimulationOperationCode.CameraTarget => PresentationCameraRequest.Target(
                    lifecycle,
                    RequireOptionalString(operation, "TargetKey"),
                    RequireOptionalString(operation, "AnchorKey"),
                    RequireOptionalString(operation, "AimPointKey"),
                    RequireOptionalString(operation, "PreferredBoneKey"),
                    RequireInt32(operation, "Priority"),
                    RequireScalar(operation, "Weight"),
                    m_Access.GetStringConstant(operation, "ActionContext", string.Empty)),
                _ => throw new InvalidOperationException(
                    $"Operation '{m_Access.SourcePath(operation)}' is not a Camera request.")
            };
            SimulationEventHeader header = m_Presentation.Next(operation);
            m_Presentation.Add(new PresentationCommand(
                header,
                PresentationCommandKind.Camera,
                RequireCameraProducer(operation),
                FixedScalar.Zero,
                FixedScalar.One,
                header.Activation.Generation,
                invocation.Cycle,
                actionInstanceId,
                cameraRequest: request));
            return OperationExecutionResult.Success;
        }

        string RequireCameraProducer(SimulationOperation operation)
        {
            IReadOnlyList<ProgramReference> references = m_Access.References(operation.Handle, ProgramReferenceKind.Producer);
            if (references.Count != 1 || string.IsNullOrWhiteSpace(references[0].ExternalIdentity))
                throw new InvalidOperationException(
                    $"Camera operation '{m_Access.SourcePath(operation)}' has no unique Camera producer identity.");
            return references[0].ExternalIdentity;
        }

        int RequireInt32(SimulationOperation operation, string field)
        {
            ProgramConstant constant = RequireConstant(operation, field, ProgramConstantKind.Int32);
            return constant.Int32;
        }

        float RequireScalar(SimulationOperation operation, string field)
        {
            ProgramConstant constant = RequireConstant(operation, field, ProgramConstantKind.Scalar);
            return constant.Scalar.ToSingle();
        }

        string RequireString(SimulationOperation operation, string field)
        {
            ProgramConstant constant = RequireConstant(operation, field, ProgramConstantKind.String);
            if (string.IsNullOrWhiteSpace(constant.Text))
                throw new InvalidOperationException(
                    $"Camera operation '{m_Access.SourcePath(operation)}' constant '{field}' is empty.");
            return constant.Text;
        }

        string RequireOptionalString(SimulationOperation operation, string field) =>
            m_Access.GetStringConstant(operation, field, string.Empty);

        ProgramConstant RequireConstant(
            SimulationOperation operation,
            string field,
            ProgramConstantKind kind)
        {
            ProgramConstant constant = m_Access.FindConstant(operation, field);
            if (constant == null || constant.Kind != kind)
                throw new InvalidOperationException(
                    $"Camera operation '{m_Access.SourcePath(operation)}' constant '{field}' is missing or has kind '{constant?.Kind}'.");
            return constant;
        }

        int ReadTimelineRuntimeHandle(OperationExecutionDescriptor operation)
        {
            int slot = m_Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.TimelinePlayback);
            int runtimeHandle = m_ControlState.Get(slot).Int32;
            if (runtimeHandle == 0)
                throw new InvalidOperationException($"Ability Timeline operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' is not started.");
            return runtimeHandle;
        }

        FixedGameplayEffectOperationRuntime RequireGameplayEffects() => m_GameplayEffects ??
            throw new InvalidOperationException(
                "Ability operation requires the declared Gameplay Effect service.");

        FixedEquipmentRuntime RequireEquipment() => m_Equipment ??
            throw new InvalidOperationException(
                "Ability operation requires the declared Equipment service.");

        public void PrepareActivation(OperationExecutionDescriptor operation)
        {
        }

        public void PrepareSubGraph(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation) =>
            m_Values.PrepareSubGraph(cursor, m_Access.Operation(operation.Handle));

        public void ActivateScopes(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            ulong generation)
        {
            m_Actions.BindCurrentSkillExecution(operation.Handle, generation);
            m_Blackboard.ActivateOperationScopes(cursor, m_Access.Operation(operation.Handle), generation);
        }

        public void CompleteScopes(OperationExecutionDescriptor operation) =>
            m_Blackboard.CompleteOperationScopes(m_Access.Operation(operation.Handle));

        public void ClearStateScope(OperationExecutionDescriptor state) =>
            m_Blackboard.ClearStateScopes(m_Access.Operation(state.Handle));

        public void ResetOperationState(OperationExecutionDescriptor operation) =>
            m_ResetState.Reset(m_Access.Operation(operation.Handle));

        public OperationStopStatus ContinueLeafStop(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context)
        {
            if (operation.Code == SimulationOperationCode.Timeline)
            {
                var stop = StopTimeline(ReadTimelineRuntimeHandle(operation));
                if (stop.Pending != null)
                    m_TimelinePendingStops.Add(stop.Pending);
                return OperationStopStatus.Completed;
            }
            throw new InvalidOperationException(
                $"Ability operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' has no direct Timeline stop owner.");
        }

        public void ForceStopLeaf(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context)
        {
            if (operation.Code == SimulationOperationCode.Timeline)
            {
                var stop = StopTimeline(ReadTimelineRuntimeHandle(operation));
                if (stop.Pending != null)
                    m_TimelinePendingStops.Add(stop.Pending);
                return;
            }
            throw new InvalidOperationException(
                $"Ability operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' has no direct Timeline stop owner.");
        }

        AbilityTimelineStopResult StopTimeline(int runtimeHandle)
        {
            IAbilityTreeClipInvokerHost host = m_TimelineRuntime as IAbilityTreeClipInvokerHost;
            if (host != null && m_TreeClipLink?.Invoker != null)
                host.PushTreeClipInvoker(m_TreeClipLink.Invoker);
            try
            {
                return m_TimelineRuntime.Stop(runtimeHandle);
            }
            finally
            {
                host?.PopTreeClipInvoker();
            }
        }

        public void EmitTrace(
            OperationExecutionDescriptor operation,
            string code,
            OperationControlTraceSeverity severity,
            string detail) =>
            m_Trace.Add(
                m_Access.Operation(operation.Handle),
                code,
                severity == OperationControlTraceSeverity.Error
                    ? SimulationTraceSeverity.Error
                    : SimulationTraceSeverity.Detail,
                detail);

        public void NotifyStateLifecycle(
            OperationExecutionDescriptor machine,
            OperationHandle state,
            OperationStateLifecyclePhase phase)
        {
            SimulationOperation operation = m_Access.Operation(machine.Handle);
            SimulationEventHeader header = m_Facts.Next(operation);
            m_Facts.Add(new GameplayFact(
                header,
                GameplayFactKind.State,
                $"state:{state.Value}",
                phase.ToString(),
                FixedScalar.Zero));
        }

        public void NotifyStateTransition(
            OperationExecutionDescriptor machine,
            OperationHandle exitingState,
            OperationHandle targetState)
        {
            m_Actions.AdvanceSegmentGeneration();
            foreach (ActionAdmissionActiveAction action in ((IActionAdmissionReadPort)m_Actions).ActiveActions)
            {
                SimulationOperation operation = m_Access.Operation(machine.Handle);
                SimulationEventHeader header = m_Presentation.Next(operation);
                m_Presentation.Add(new PresentationCommand(
                    header,
                    PresentationCommandKind.DomainEvent,
                    "domain/action-segment-changed",
                    FixedScalar.Zero,
                    FixedScalar.Zero,
                    sourceActionInstanceId: action.InstanceId,
                    domainPayload: $"prev:{exitingState.Value};next:{targetState.Value}"));
            }
        }
    }
}
