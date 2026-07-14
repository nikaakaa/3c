using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonCamera;
using TreeDesigner;
using UnityEngine;
using UnityEngine.InputSystem;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    public sealed class CharacterGraphContext : ITimelinePlaybackService, ITimelinePlaybackActionContextSource, IActionRuntimeService, IInputActionValueSource, IPipelineBlackboardRuntimeAccess, IStateMachineExecutionScopeSink, IStateExitContextRuntimeAccess, INodeStopTickSource, IRuntimeDiagnosticsContextSource, ITreeExecutionContextSource, ICharacterMotionContext, IDisposable
    {
        readonly CharacterInputStage m_InputStage;
        readonly ActionRuntime m_ActionRuntime;
        readonly ICharacterLogicPosePort m_LogicPosePort;
        readonly string m_ActorId;
        readonly CharacterGameplayEffectQueryPorts m_GameplayEffectQueries;
        readonly CharacterGameplayEffectCommandPorts m_GameplayEffectCommands;
        readonly PipelineBlackboardRuntime m_Blackboard = new PipelineBlackboardRuntime();
        readonly List<CharacterTimelinePlaybackRequest> m_TimelinePlaybackRequests = new List<CharacterTimelinePlaybackRequest>();
        readonly Dictionary<ulong, TimelinePlaybackStatus> m_TimelinePlaybackStatuses = new Dictionary<ulong, TimelinePlaybackStatus>();
        readonly Dictionary<ulong, TimelineData> m_TimelinePlaybackSources = new Dictionary<ulong, TimelineData>();
        readonly Dictionary<ulong, RuntimeTimelinePlaybackProvenance> m_TimelinePlaybackProvenances = new Dictionary<ulong, RuntimeTimelinePlaybackProvenance>();
        readonly Dictionary<ulong, TimelinePlaybackStopContext> m_TimelinePlaybackStopContexts = new Dictionary<ulong, TimelinePlaybackStopContext>();
        readonly Dictionary<ActionContextSlot, ActionInstanceHandle> m_ActionContexts = new Dictionary<ActionContextSlot, ActionInstanceHandle>();
        readonly List<ActionContextSlot> m_RemoveActionContexts = new List<ActionContextSlot>();
        readonly Stack<StateMachineExecutionScope> m_StateMachineExecutionScopes = new Stack<StateMachineExecutionScope>();
        readonly Stack<StateExitContext> m_StateExitContexts = new Stack<StateExitContext>();
        readonly List<ActionWindowProjectionCandidate> m_WindowProjectionCandidates = new List<ActionWindowProjectionCandidate>();
        readonly HashSet<string> m_WindowProjectionKeys = new HashSet<string>(StringComparer.Ordinal);
        readonly TreeExecutionContext m_TreeExecutionContext;

        CharacterPipelineFrame m_Frame;
        ulong m_NextTimelinePlaybackHandle;
        ulong m_NextAnimationSelectionSequence;
        CameraBasisSnapshot m_CameraBasisSnapshot;
        CharacterActorPoseSnapshot m_ActorPoseSnapshot;

        public CharacterGraphContext(
            string actorId,
            CharacterInputStage inputStage,
            ActionRuntime actionRuntime,
            ICharacterLogicPosePort logicPosePort,
            CharacterGameplayEffectQueryPorts gameplayEffectQueries,
            CharacterGameplayEffectCommandPorts gameplayEffectCommands)
        {
            m_ActorId = string.IsNullOrWhiteSpace(actorId)
                ? throw new ArgumentException("Character actor id is required.", nameof(actorId))
                : actorId.Trim();
            m_InputStage = inputStage;
            m_ActionRuntime = actionRuntime;
            m_LogicPosePort = logicPosePort ?? throw new ArgumentNullException(nameof(logicPosePort));
            m_GameplayEffectQueries = gameplayEffectQueries ?? throw new ArgumentNullException(nameof(gameplayEffectQueries));
            m_GameplayEffectCommands = gameplayEffectCommands ?? throw new ArgumentNullException(nameof(gameplayEffectCommands));
            m_TreeExecutionContext = new TreeExecutionContext();
        }

        public GameplayLogicTickContext TickContext { get; private set; }
        public ulong LocalLogicTick => TickContext.LocalLogicTick;
        ulong INodeStopTickSource.NodeStopLocalLogicTick => LocalLogicTick;
        public CharacterNetworkInput NetworkInput => m_Frame?.NetworkInput;
        public CharacterPipelineOutput Output => m_Frame?.Output;
        public CharacterInputFrame InputFrame => m_Frame?.Input;
        public CharacterInputRequestBuffer RequestBuffer => m_InputStage?.RequestBuffer;
        public CharacterInputHistory InputHistory => m_InputStage?.History;
        public IReadOnlyList<PipelineBlackboardDebugEntry> BlackboardDebug => m_Blackboard.DebugEntries;
        public RuntimeDiagnosticsContext RuntimeDiagnostics { get; private set; }
        public TreeExecutionContext TreeExecutionContext => m_TreeExecutionContext;
        public string ActorId => m_ActorId;
        public CharacterGameplayEffectQueryPorts GameplayEffectQueries => m_GameplayEffectQueries;
        public CharacterGameplayEffectCommandPorts GameplayEffectCommands => m_GameplayEffectCommands;
        public ulong ActiveActionInstanceId => m_ActionRuntime != null
            ? m_ActionRuntime.ActionContext.ActionInstanceId
            : 0;

        public void SetRuntimeDiagnostics(RuntimeDiagnosticsContext diagnostics)
        {
            RuntimeDiagnostics = diagnostics;
        }

        public bool RequestTimelinePlayback(
            TimelineData timeline,
            string sourceId,
            string sourceName,
            TimelinePlaybackActionContext actionContext,
            TimelinePlaybackMode playbackMode,
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            out TimelinePlaybackHandle handle)
        {
            handle = TimelinePlaybackHandle.Invalid;
            if (timeline == null || !sourceActivation.IsValid || sourceRuntimeGraph == null ||
                sourceRuntimeGraph.RuntimeId != sourceActivation.ActivationId.GraphRuntimeId)
                return false;

            RuntimeTimelinePlaybackProvenance provenance = BuildTimelinePlaybackProvenance(
                sourceActivation,
                sourceRuntimeGraph,
                sourceId,
                sourceName);

            m_NextTimelinePlaybackHandle++;
            if (m_NextTimelinePlaybackHandle == 0)
                m_NextTimelinePlaybackHandle++;

            handle = new TimelinePlaybackHandle(m_NextTimelinePlaybackHandle);
            m_TimelinePlaybackStatuses[handle.Value] = TimelinePlaybackStatus.Requested;
            m_TimelinePlaybackSources[handle.Value] = timeline;
            m_TimelinePlaybackProvenances[handle.Value] = provenance;
            m_TimelinePlaybackRequests.Add(new CharacterTimelinePlaybackRequest(
                new TimelinePlaybackRequest(
                    handle,
                    timeline,
                    sourceId,
                    sourceName,
                    actionContext,
                    playbackMode,
                    sourceActivation,
                    sourceRuntimeGraph,
                    provenance)));
            RuntimeDiagnosticsContext diagnostics = RuntimeDiagnostics;
            if (diagnostics != null && diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TimelineRequested))
            {
                diagnostics.Publish(
                    RuntimeTraceChannel.Timeline,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.TimelineRequested,
                    RuntimeSourceElementKey.Timeline(timeline.AuthoringId),
                    RuntimeInstanceKey.Timeline(diagnostics.CharacterRuntimeId, handle.Value),
                    new RuntimeTracePayload
                    {
                        Name = timeline.Name,
                        Status = TimelinePlaybackStatus.Requested.ToString(),
                        Detail = sourceName,
                        RelatedElementId = sourceId,
                        TimelinePlayback = provenance
                    });
            }
            return true;
        }

        public bool TryGetTimelinePlaybackActionContext(ActionContextSlot actionContext, out TimelinePlaybackActionContext playbackActionContext)
        {
            playbackActionContext = default;
            if (!TryGetActionContextHandle(actionContext, out ActionInstanceHandle handle))
                return false;

            playbackActionContext = new TimelinePlaybackActionContext(
                handle.ActionInstanceId,
                handle.ActionId,
                handle.PredictionKey,
                handle.InputSequence,
                handle.StartLocalLogicTick);
            return true;
        }

        public TimelinePlaybackStatus GetTimelinePlaybackStatus(TimelinePlaybackHandle handle)
        {
            if (!handle.IsValid)
                return TimelinePlaybackStatus.None;

            return m_TimelinePlaybackStatuses.TryGetValue(handle.Value, out TimelinePlaybackStatus status)
                ? status
                : TimelinePlaybackStatus.None;
        }

        public void CancelTimelinePlayback(TimelinePlaybackHandle handle, TimelinePlaybackStopContext stopContext)
        {
            TimelinePlaybackStatus status = GetTimelinePlaybackStatus(handle);
            if (status == TimelinePlaybackStatus.Requested || status == TimelinePlaybackStatus.Running)
            {
                m_TimelinePlaybackStatuses[handle.Value] = TimelinePlaybackStatus.Cancelled;
                m_TimelinePlaybackStopContexts[handle.Value] = stopContext;
                RuntimeDiagnosticsContext diagnostics = RuntimeDiagnostics;
                if (diagnostics != null && diagnostics.ShouldPublish(RuntimeTraceChannel.Timeline, RuntimeTraceEventKind.TimelineCancelled))
                {
                    if (!m_TimelinePlaybackSources.TryGetValue(handle.Value, out TimelineData source))
                        throw new InvalidOperationException($"Timeline playback source is missing for handle {handle.Value}.");
                    if (!m_TimelinePlaybackProvenances.TryGetValue(handle.Value, out RuntimeTimelinePlaybackProvenance provenance))
                        throw new InvalidOperationException($"Timeline playback provenance is missing for handle {handle.Value}.");
                    diagnostics.Publish(
                        RuntimeTraceChannel.Timeline,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.TimelineCancelled,
                        RuntimeSourceElementKey.Timeline(source.AuthoringId),
                        RuntimeInstanceKey.Timeline(diagnostics.CharacterRuntimeId, handle.Value),
                        new RuntimeTracePayload
                        {
                            Name = source.Name,
                            Status = TimelinePlaybackStatus.Cancelled.ToString(),
                            Cause = stopContext.Cause.ToString(),
                            TimelinePlayback = provenance
                        });
                }
            }
        }

        public bool TryConsumeTimelinePlaybackStopContext(TimelinePlaybackHandle handle, out TimelinePlaybackStopContext stopContext)
        {
            if (handle.IsValid && m_TimelinePlaybackStopContexts.TryGetValue(handle.Value, out stopContext))
            {
                m_TimelinePlaybackStopContexts.Remove(handle.Value);
                return true;
            }

            stopContext = default;
            return false;
        }

        public void ConsumeTimelinePlaybackRequests(List<CharacterTimelinePlaybackRequest> requests)
        {
            if (requests == null)
                return;

            requests.AddRange(m_TimelinePlaybackRequests);
            m_TimelinePlaybackRequests.Clear();
        }

        public void SetTimelinePlaybackStatus(TimelinePlaybackHandle handle, TimelinePlaybackStatus status)
        {
            if (handle.IsValid && m_TimelinePlaybackStatuses.ContainsKey(handle.Value))
                m_TimelinePlaybackStatuses[handle.Value] = status;
        }

        public void PushStateMachineExecutionScope(StateMachineExecutionScope scope)
        {
            if (!scope.IsValid)
                return;

            m_StateMachineExecutionScopes.Push(scope);
            m_TreeExecutionContext.PushStateMachineExecutionScope(scope);
            if (RuntimeDiagnostics != null)
            {
                RuntimeDiagnostics.PushRuntimeInstance(RuntimeInstanceKey.State(
                    RuntimeDiagnostics.CharacterRuntimeId,
                    scope.RuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration));
            }
        }

        public void PopStateMachineExecutionScope(StateMachineExecutionScope scope)
        {
            if (m_StateMachineExecutionScopes.Count == 0 || !m_StateMachineExecutionScopes.Peek().Equals(scope))
            {
                Debug.LogError($"StateMachine execution scope stack mismatch while popping '{scope.StateId}/{scope.ActivationGeneration}'.");
                return;
            }

            m_StateMachineExecutionScopes.Pop();
            m_TreeExecutionContext.PopStateMachineExecutionScope(scope);
            if (RuntimeDiagnostics != null)
            {
                RuntimeDiagnostics.PopRuntimeInstance(RuntimeInstanceKey.State(
                    RuntimeDiagnostics.CharacterRuntimeId,
                    scope.RuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration));
            }
        }

        public ulong NextAnimationSelectionSequence()
        {
            m_NextAnimationSelectionSequence++;
            if (m_NextAnimationSelectionSequence == 0)
                m_NextAnimationSelectionSequence++;
            return m_NextAnimationSelectionSequence;
        }

        public bool SubmitAnimationLayerSelection(
            AnimationLayerSelection selection,
            string sourceId,
            string sourceName)
        {
            if (m_Frame == null)
                return false;
            return m_Frame.AnimationSelections.Submit(selection, sourceId, sourceName);
        }

        public void ReportAnimationLayerSelectionError(string message)
        {
            m_Frame?.AnimationSelections.ReportError(message);
        }

        public bool CommitAnimationLayerSelections(IAnimationPlaybackCommandSink sink)
        {
            if (m_Frame == null || sink == null)
                return false;

            AnimationLayerSelectionBatch batch = m_Frame.AnimationSelections;
            if (!batch.IsValid)
            {
                for (int i = 0; i < batch.Errors.Count; i++)
                    Debug.LogError(batch.Errors[i]);
                return false;
            }

            for (int i = 0; i < batch.Selections.Count; i++)
                sink.EnqueueSelection(batch.Selections[i].Selection);
            return true;
        }

        public void PushStateExitContext(StateExitContext context)
        {
            if (context.IsValid)
                m_StateExitContexts.Push(context);
        }

        public void PopStateExitContext(StateExitContext context)
        {
            if (m_StateExitContexts.Count == 0 || !m_StateExitContexts.Peek().Equals(context))
            {
                Debug.LogError($"State exit context stack mismatch while popping '{context.SourceStateGuid}'.");
                return;
            }

            m_StateExitContexts.Pop();
        }

        public bool TryGetStateExitContext(out StateExitContext context)
        {
            if (m_StateExitContexts.Count > 0)
            {
                context = m_StateExitContexts.Peek();
                return true;
            }

            context = default;
            return false;
        }

        public void BeginFrame(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            TickContext = context;
            RuntimeDiagnostics?.BeginLogicTick(context.LocalLogicTick);
            m_Blackboard.BeginFrame(context.LocalLogicTick);
            m_Frame = frame;
            if (!m_LogicPosePort.TryReadState(out CharacterLogicBodyState state, out string error))
                throw new InvalidOperationException($"Logic pose port failed to capture Graph actor pose: {error}");
            m_ActorPoseSnapshot = CharacterActorPoseSnapshot.Capture(state);
        }

        public void ProjectWindowFacts()
        {
            m_WindowProjectionCandidates.Clear();
            m_WindowProjectionKeys.Clear();
            m_Blackboard.ConsumeActionWindowProjectionCandidates(m_WindowProjectionCandidates);
            for (int i = 0; i < m_WindowProjectionCandidates.Count; i++)
            {
                ActionWindowProjectionCandidate candidate = m_WindowProjectionCandidates[i];
                string key = $"{candidate.DeclarationId}:{candidate.ActionInstanceId}:{candidate.LocalLogicTick}";
                if (!m_WindowProjectionKeys.Add(key) || m_Frame == null)
                    continue;

                var sample = new ActionWindowSample(
                    candidate.ActionInstanceId,
                    candidate.WindowId,
                    candidate.WindowType,
                    candidate.LocalLogicTick,
                    candidate.LocalLogicTick,
                    candidate.Digest);
                m_Frame.Output.SyncFacts.Action.WindowSamples.Add(sample);
                m_ActionRuntime?.RecordOutput(sample);
                m_Blackboard.MarkProjectionProduced(candidate);
                RuntimeDiagnosticsContext diagnostics = RuntimeDiagnostics;
                if (diagnostics != null && diagnostics.ShouldPublish(RuntimeTraceChannel.Blackboard, RuntimeTraceEventKind.BlackboardProjected))
                {
                    diagnostics.Publish(
                        RuntimeTraceChannel.Blackboard,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.BlackboardProjected,
                        RuntimeSourceElementKey.Declaration(candidate.DeclarationOwnerId, candidate.DeclarationId),
                        diagnostics.CurrentRuntimeInstance,
                        new RuntimeTracePayload
                        {
                            Name = candidate.WindowId,
                            Detail = candidate.WindowType,
                            Value = DebugValueSnapshot.Capture(true)
                        });
                }
            }

            m_WindowProjectionCandidates.Clear();
            m_WindowProjectionKeys.Clear();
        }

        public void EndFrame()
        {
            m_Blackboard.EndFrame(LocalLogicTick);
        }

        public void ResetPipelineBlackboard()
        {
            m_Blackboard.ClearValues();
            m_ActionContexts.Clear();
            m_ActorPoseSnapshot = default;
            m_CameraBasisSnapshot = default;
        }

        public bool TryReadActorPoseSnapshot(out CharacterActorPoseSnapshot snapshot)
        {
            snapshot = m_ActorPoseSnapshot;
            return snapshot.Valid;
        }

        public void ClearActorPoseSnapshot()
        {
            m_ActorPoseSnapshot = default;
        }

        public bool TryReadButton(InputActionAsset sourceAsset, string actionId, out bool value)
        {
            return m_InputStage.TryReadButton(sourceAsset, actionId, out value);
        }

        public bool TryReadFloat(InputActionAsset sourceAsset, string actionId, out float value)
        {
            return m_InputStage.TryReadFloat(sourceAsset, actionId, out value);
        }

        public bool TryReadVector2(InputActionAsset sourceAsset, string actionId, out Vector2 value)
        {
            return m_InputStage.TryReadVector2(sourceAsset, actionId, out value);
        }

        public bool TryReadInputValueBool(string inputValueId, out bool value)
        {
            value = false;
            return InputFrame != null && InputFrame.TryGetBool(inputValueId, out value);
        }

        public bool TryReadInputValueFloat(string inputValueId, out float value)
        {
            value = 0f;
            return InputFrame != null && InputFrame.TryGetFloat(inputValueId, out value);
        }

        public bool TryReadInputValueVector2(string inputValueId, out Vector2 value)
        {
            value = Vector2.zero;
            return InputFrame != null && InputFrame.TryGetVector2(inputValueId, out value);
        }

        public bool HasInputRequest(string requestId)
        {
            return RequestBuffer != null && RequestBuffer.HasRequest(requestId, LocalLogicTick);
        }

        public bool TryConsumeInputRequest(string requestId, out CharacterInputRequest request)
        {
            request = default;
            return RequestBuffer != null && RequestBuffer.TryConsumeRequest(requestId, LocalLogicTick, out request);
        }

        public bool TryGetInputRequest(string requestId, out CharacterInputRequest request)
        {
            request = default;
            return RequestBuffer != null && RequestBuffer.TryGetRequest(requestId, LocalLogicTick, out request);
        }

        public ActionActivationOutcome ActivateAction(ActionActivationRequest request)
        {
            if (m_ActionRuntime == null)
                return new ActionActivationOutcome(ActionActivationResult.InvalidRequest, default);

            ActionActivationOutcome outcome = m_ActionRuntime.ActivateAction(request);
            if (outcome.Result == ActionActivationResult.Activated && m_Frame != null)
            {
                if (outcome.HasReplacedTransition)
                {
                    m_Frame.Output.SyncFacts.Action.LifecycleTransitions.Add(outcome.ReplacedTransition);
                    ClearActionContexts(outcome.ReplacedTransition.ActionInstanceId);
                }

                m_Frame.Output.SyncFacts.Action.ActivationRequests.Add(request);
                m_Frame.Output.SyncFacts.Action.ActivationOutputs.Add(new ActionActivationOutput(request, outcome.Handle));
            }

            return outcome;
        }

        public ActionActivationResult SubmitActionActivation(ActionActivationRequest request, out ActionInstanceHandle handle)
        {
            ActionActivationOutcome outcome = ActivateAction(request);
            handle = outcome.Handle;
            return outcome.Result;
        }

        public bool ApplyActionLifecycleTransition(ActionLifecycleTransition transition)
        {
            if (m_ActionRuntime == null || !m_ActionRuntime.ApplyActionLifecycleTransition(transition))
                return false;

            if (transition.IsTerminal)
                ClearActionContexts(transition.ActionInstanceId);

            m_Frame?.Output.SyncFacts.Action.LifecycleTransitions.Add(transition);
            return true;
        }

        public bool SubmitActionLifecycleTransition(ActionLifecycleTransition transition)
        {
            return ApplyActionLifecycleTransition(transition);
        }

        public bool TryGetInputFrameByLocalLogicTick(ulong localLogicTick, out CharacterInputFrame inputFrame)
        {
            inputFrame = null;
            return InputHistory != null && InputHistory.TryGetByLocalLogicTick(localLogicTick, out inputFrame);
        }

        public bool TryGetInputFrameByInputSequence(ulong inputSequence, out CharacterInputFrame inputFrame)
        {
            inputFrame = null;
            return InputHistory != null && InputHistory.TryGetByInputSequence(inputSequence, out inputFrame);
        }

        public bool TryGetBlackboardValue<T>(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference variable,
            out T value)
        {
            value = default;
            if (!TryGetPipelineBlackboardValue(accessGraph, variable, typeof(T), out object rawValue) || !(rawValue is T typedValue))
                return false;

            value = typedValue;
            return true;
        }

        public bool SetBlackboardValue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference variable,
            object value)
        {
            return SetPipelineBlackboardValue(accessGraph, variable, value, null);
        }

        public void SetActionContext(ActionContextSlot actionContext, ActionInstanceHandle handle)
        {
            if (!actionContext)
                return;

            if (handle.IsValid)
                m_ActionContexts[actionContext] = handle;
            else
                m_ActionContexts.Remove(actionContext);
        }

        public bool TryGetActionContextHandle(ActionContextSlot actionContext, out ActionInstanceHandle handle)
        {
            handle = default;
            if (!actionContext)
                return false;

            if (!m_ActionContexts.TryGetValue(actionContext, out ActionInstanceHandle value) || !value.IsValid)
                return false;

            if (m_ActionRuntime == null || !m_ActionRuntime.IsActionActive(value.ActionInstanceId))
            {
                m_ActionContexts.Remove(actionContext);
                return false;
            }

            handle = value;
            return true;
        }

        public bool TryGetActionInstanceHandle(ulong actionInstanceId, out ActionInstanceHandle handle)
        {
            handle = default;
            return m_ActionRuntime != null && m_ActionRuntime.TryGetActiveHandle(actionInstanceId, out handle);
        }

        void ClearActionContexts(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                return;

            m_RemoveActionContexts.Clear();
            foreach (var pair in m_ActionContexts)
            {
                if (pair.Value.ActionInstanceId == actionInstanceId)
                    m_RemoveActionContexts.Add(pair.Key);
            }

            for (int i = 0; i < m_RemoveActionContexts.Count; i++)
                m_ActionContexts.Remove(m_RemoveActionContexts[i]);
            m_RemoveActionContexts.Clear();
            m_Blackboard.ClearActionInstance(actionInstanceId);
        }

        public bool SubmitActionMotionSample(ActionMotionSample sample)
        {
            if (sample.ActionInstanceId == 0 || m_Frame == null)
                return false;

            m_Frame.Output.SyncFacts.Motion.ActionMotionSamples.Add(sample);
            return true;
        }

        public bool SubmitMotionContribution(MotionContribution contribution)
        {
            if (m_Frame == null || !contribution.CanResolve)
                return false;

            m_Frame.Output.StrictGameplay.MotionContributions.Add(contribution);
            return true;
        }

        public bool SubmitCameraStateRequest(CameraStateRequest request)
        {
            if (m_Frame == null || !request.Active)
                return false;

            m_Frame.Output.Presentation.CameraStateRequests.Add(request);
            return true;
        }

        public bool SubmitCameraCue(CameraCue cue)
        {
            if (m_Frame == null || !cue.Active)
                return false;

            m_Frame.Output.Presentation.CameraCues.Add(cue);
            return true;
        }

        public bool SubmitCameraResponsePolicy(CameraResponsePolicy policy)
        {
            if (m_Frame == null || !policy.Active)
                return false;

            m_Frame.Output.Presentation.CameraResponsePolicies.Add(policy);
            return true;
        }

        public bool SubmitCameraTargetRequest(CameraTargetRequest request)
        {
            if (m_Frame == null || !request.Active || !request.HasAnyKey)
                return false;

            m_Frame.Output.Presentation.CameraTargetRequests.Add(request);
            return true;
        }

        public bool TryReadCameraBasisSnapshot(out CameraBasisSnapshot snapshot)
        {
            snapshot = m_CameraBasisSnapshot;
            return snapshot.Valid;
        }

        public void SetCameraBasisSnapshot(CameraBasisSnapshot snapshot)
        {
            m_CameraBasisSnapshot = snapshot;
            if (m_Frame != null)
                m_Frame.Output.Presentation.CameraBasisSnapshot = snapshot;
        }

        public bool SubmitGameplayCue(GameplayCueFact cue)
        {
            return SubmitGameplayCue(null, default, cue);
        }

        public bool SubmitGameplayCue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference variable,
            GameplayCueFact cue)
        {
            if (m_Frame == null || !cue.IsValid)
                return false;

            if (variable.IsValid && !SetBlackboardValue(accessGraph, variable, cue))
                return false;
            if (variable.IsValid)
                m_Blackboard.MarkSyncFactProduced(BuildBlackboardAccess(accessGraph), variable);
            m_Frame.Output.SyncFacts.Presentation.CueEvents.Add(cue);
            m_ActionRuntime?.RecordOutput(cue);
            return true;
        }

        public bool SubmitGameplayResultEvent(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference variable,
            GameplayResultEvent resultEvent)
        {
            if (m_Frame == null || (resultEvent.ResultId == 0 && string.IsNullOrEmpty(resultEvent.ResultType)))
                return false;

            if (variable.IsValid && !SetBlackboardValue(accessGraph, variable, resultEvent))
                return false;
            if (variable.IsValid)
                m_Blackboard.MarkSyncFactProduced(BuildBlackboardAccess(accessGraph), variable);
            m_Frame.Output.SyncFacts.GameplayResult.Events.Add(resultEvent);
            m_ActionRuntime?.RecordOutput(resultEvent);
            return true;
        }

        public void RegisterPipelineBlackboardVariables(BaseGraph graph, IReadOnlyList<BaseExposedProperty> variables)
        {
            m_Blackboard.RegisterGraphVariables(graph, variables, BuildBlackboardAccess(graph));
        }

        public void UnregisterPipelineBlackboardGraph(BaseGraph graph)
        {
            m_Blackboard.UnregisterGraph(graph);
        }

        public bool TryResolvePipelineBlackboardDeclaration(
            PipelineBlackboardVariableReference reference,
            out BaseExposedProperty declaration)
        {
            return m_Blackboard.TryResolveDeclaration(reference, out declaration);
        }

        public bool TryGetPipelineBlackboardValue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference reference,
            Type expectedType,
            out object value)
        {
            return m_Blackboard.TryGetValue(BuildBlackboardAccess(accessGraph), reference, expectedType, out value);
        }

        public bool SetPipelineBlackboardValue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference reference,
            object value,
            UnityEngine.Object factContext)
        {
            if (!TryBuildBlackboardWriteProvenance(accessGraph, factContext, out PipelineBlackboardWriteProvenance provenance))
                return false;

            bool written = m_Blackboard.SetValue(BuildBlackboardAccess(accessGraph), reference, value, provenance);
            RuntimeDiagnosticsContext diagnostics = RuntimeDiagnostics;
            RuntimeTraceEventKind kind = value == null ? RuntimeTraceEventKind.BlackboardCleared : RuntimeTraceEventKind.BlackboardWritten;
            if (written && diagnostics != null && diagnostics.ShouldPublish(RuntimeTraceChannel.Blackboard, kind))
            {
                diagnostics.Publish(
                    RuntimeTraceChannel.Blackboard,
                    RuntimeTraceDomain.Logic,
                    kind,
                    RuntimeSourceElementKey.Declaration(reference.DeclarationOwnerId, reference.DeclarationId),
                    diagnostics.CurrentRuntimeInstance,
                    new RuntimeTracePayload
                    {
                        Name = reference.DisplayKey,
                        Detail = provenance.DebugIdentity,
                        Value = DebugValueSnapshot.Capture(value)
                    });
            }
            return written;
        }

        public void NotifyPipelineBlackboardStateEntered(StateMachineExecutionScope scope)
        {
            m_Blackboard.EnterState(scope);
        }

        public void NotifyPipelineBlackboardStateExited(StateMachineExecutionScope scope)
        {
            m_Blackboard.ExitState(scope);
        }

        PipelineBlackboardAccessScope BuildBlackboardAccess(BaseGraph graph)
        {
            var frames = new List<StateMachineExecutionScope>();
            StateMachineExecutionScope[] stackFrames = m_StateMachineExecutionScopes.ToArray();
            for (int i = stackFrames.Length - 1; i >= 0; i--)
                frames.Add(stackFrames[i]);

            if (graph != null &&
                graph.TryGetEvaluationContext(out ConditionRuleEvaluationContext evaluationContext) &&
                evaluationContext.StateScope.IsValid &&
                !ContainsScope(frames, evaluationContext.StateScope))
                frames.Add(evaluationContext.StateScope);

            ulong actionInstanceId = 0;
            ActionContext actionContext = m_ActionRuntime != null ? m_ActionRuntime.ActionContext : default;
            if (actionContext.HasActiveInstance)
                actionInstanceId = actionContext.Instance.InstanceId;

            return new PipelineBlackboardAccessScope(
                graph,
                new StateMachineExecutionPath(frames),
                actionInstanceId,
                LocalLogicTick);
        }

        bool TryBuildBlackboardWriteProvenance(
            BaseGraph graph,
            UnityEngine.Object factContext,
            out PipelineBlackboardWriteProvenance provenance)
        {
            provenance = default;
            if (graph == null || graph.RuntimeId == Guid.Empty || string.IsNullOrEmpty(graph.GraphAuthoringId))
            {
                Debug.LogError("Pipeline blackboard write requires a registered source Graph/runtime owner.");
                return false;
            }

            TimelinePlaybackActionContext actionContext = default;
            ulong playbackIdentity = 0;
            int trackIndex = -1;
            int clipIndex = -1;
            int cycle = -1;
            if (graph is TimelineRunningTree timelineTree)
            {
                TimelineTreeClipRuntimeContext clipContext = timelineTree.ClipContext;
                if (clipContext == null || clipContext.LocalLogicTick != LocalLogicTick)
                {
                    Debug.LogError($"TimelineData blackboard write is missing current Clip runtime context: graph={graph.name} tick={LocalLogicTick}.");
                    return false;
                }

                playbackIdentity = clipContext.PlaybackIdentity;
                trackIndex = clipContext.TrackIndex;
                clipIndex = clipContext.ClipIndex;
                cycle = clipContext.Cycle;
                actionContext = clipContext.ActionContext;
            }
            else if (factContext is ActionContextSlot actionContextSlot &&
                     TryGetActionContextHandle(actionContextSlot, out ActionInstanceHandle handle))
            {
                actionContext = new TimelinePlaybackActionContext(
                    handle.ActionInstanceId,
                    handle.ActionId,
                    handle.PredictionKey,
                    handle.InputSequence,
                    handle.StartLocalLogicTick);
            }

            provenance = new PipelineBlackboardWriteProvenance(
                LocalLogicTick,
                graph.GraphAuthoringId,
                graph.RuntimeId,
                playbackIdentity,
                trackIndex,
                clipIndex,
                cycle,
                actionContext);
            return true;
        }

        public void Dispose()
        {
            m_Frame = null;
            m_ActorPoseSnapshot = default;
            m_TimelinePlaybackRequests.Clear();
            m_TimelinePlaybackStatuses.Clear();
            m_TimelinePlaybackSources.Clear();
            m_TimelinePlaybackProvenances.Clear();
            m_TimelinePlaybackStopContexts.Clear();
            m_ActionContexts.Clear();
            m_RemoveActionContexts.Clear();
            m_StateMachineExecutionScopes.Clear();
            m_TreeExecutionContext.Reset();
            m_StateExitContexts.Clear();
            m_WindowProjectionCandidates.Clear();
            m_WindowProjectionKeys.Clear();
            m_Blackboard.Reset();
            RuntimeDiagnostics = null;
        }

        static bool ContainsScope(IReadOnlyList<StateMachineExecutionScope> frames, StateMachineExecutionScope scope)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].Equals(scope))
                    return true;
            }
            return false;
        }

        static RuntimeTimelinePlaybackProvenance BuildTimelinePlaybackProvenance(
            TreeExecutionActivationScope sourceActivation,
            BaseGraph sourceRuntimeGraph,
            string sourceId,
            string sourceName)
        {
            TreeAuthoringElementKey source = sourceActivation.Source;
            if (!sourceActivation.IsValid || sourceRuntimeGraph == null ||
                !string.Equals(source.GraphAuthoringId, sourceRuntimeGraph.GraphAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(source.ElementAuthoringId, sourceId, StringComparison.Ordinal))
                throw new InvalidOperationException("Timeline playback request has no exact source activation provenance.");

            StateMachineExecutionScope stateScope = sourceActivation.StateMachineExecutionPath.Leaf;
            return new RuntimeTimelinePlaybackProvenance(
                source.GraphAuthoringId,
                source.ElementAuthoringId,
                sourceActivation.ActivationId.GraphRuntimeId,
                sourceActivation.ActivationId.Generation,
                stateScope.StateMachineGraphOwnerId,
                stateScope.StateId,
                stateScope.StateMachineGraphRuntimeId,
                stateScope.ActivationGeneration,
                sourceName);
        }
    }

    public readonly struct CharacterTimelinePlaybackRequest
    {
        public CharacterTimelinePlaybackRequest(TimelinePlaybackRequest request)
        {
            Request = request;
        }

        public TimelinePlaybackRequest Request { get; }
        public StateMachineExecutionScope StateScope => Request.SourceActivation.StateMachineExecutionPath.Leaf;
        public TreeExecutionActivationScope SourceActivation => Request.SourceActivation;
        public BaseGraph SourceRuntimeGraph => Request.SourceRuntimeGraph;
        public TimelinePlaybackHandle Handle => Request.Handle;
        public TimelineData Timeline => Request.Timeline;
        public string SourceId => Request.SourceId;
        public string SourceName => Request.SourceName;
        public TimelinePlaybackActionContext ActionContext => Request.ActionContext;
        public TimelinePlaybackMode PlaybackMode => Request.PlaybackMode;
        public RuntimeTimelinePlaybackProvenance DiagnosticsProvenance => Request.DiagnosticsProvenance;
    }

    public readonly struct PipelineBlackboardWriteProvenance
    {
        public PipelineBlackboardWriteProvenance(
            ulong localLogicTick,
            string graphOwnerId,
            Guid graphRuntimeId,
            ulong playbackIdentity,
            int trackIndex,
            int clipIndex,
            int cycle,
            TimelinePlaybackActionContext actionContext)
        {
            LocalLogicTick = localLogicTick;
            GraphOwnerId = graphOwnerId ?? string.Empty;
            GraphRuntimeId = graphRuntimeId;
            PlaybackIdentity = playbackIdentity;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            Cycle = cycle;
            ActionContext = actionContext;
        }

        public ulong LocalLogicTick { get; }
        public string GraphOwnerId { get; }
        public Guid GraphRuntimeId { get; }
        public ulong PlaybackIdentity { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public int Cycle { get; }
        public TimelinePlaybackActionContext ActionContext { get; }
        public bool IsTimeline => PlaybackIdentity != 0;
        public bool IsValid => LocalLogicTick != 0 && GraphRuntimeId != Guid.Empty && !string.IsNullOrEmpty(GraphOwnerId);
        public string DebugIdentity => IsTimeline
            ? $"TreeClip:{PlaybackIdentity}/{TrackIndex}/{ClipIndex}/{Cycle} graph:{GraphRuntimeId:N} tick:{LocalLogicTick} action:{ActionContext.ActionInstanceId}"
            : $"Graph:{GraphRuntimeId:N} tick:{LocalLogicTick} action:{ActionContext.ActionInstanceId}";
    }

    readonly struct ActionWindowProjectionCandidate
    {
        public ActionWindowProjectionCandidate(
            PipelineBlackboardVariableAddress address,
            PipelineBlackboardVariableDeclaration declaration,
            PipelineBlackboardWriteProvenance provenance)
        {
            Address = address;
            DeclarationId = declaration.DeclarationId;
            DeclarationOwnerId = declaration.OwnerId;
            WindowType = declaration.ActionWindowType;
            WindowId = declaration.ActionWindowId;
            Digest = declaration.ActionWindowDigest;
            ActionInstanceId = provenance.ActionContext.ActionInstanceId;
            LocalLogicTick = provenance.LocalLogicTick;
            Provenance = provenance;
        }

        public PipelineBlackboardVariableAddress Address { get; }
        public string DeclarationId { get; }
        public string DeclarationOwnerId { get; }
        public string WindowType { get; }
        public string WindowId { get; }
        public ulong Digest { get; }
        public ulong ActionInstanceId { get; }
        public ulong LocalLogicTick { get; }
        public PipelineBlackboardWriteProvenance Provenance { get; }
    }

    public readonly struct PipelineBlackboardDebugEntry
    {
        public PipelineBlackboardDebugEntry(
            string declarationId,
            string key,
            string valueType,
            string value,
            string scope,
            string lifetime,
            string authority,
            string syncPolicy,
            string factProjection,
            string projectionIdentity,
            string syncStatus,
            string source,
            string categoryPath,
            string owner,
            string address,
            bool hasValue)
        {
            DeclarationId = declarationId ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType ?? string.Empty;
            Value = value ?? string.Empty;
            Scope = scope ?? string.Empty;
            Lifetime = lifetime ?? string.Empty;
            Authority = authority ?? string.Empty;
            SyncPolicy = syncPolicy ?? string.Empty;
            FactProjection = factProjection ?? string.Empty;
            ProjectionIdentity = projectionIdentity ?? string.Empty;
            SyncStatus = syncStatus ?? string.Empty;
            Source = source ?? string.Empty;
            CategoryPath = categoryPath ?? string.Empty;
            Owner = owner ?? string.Empty;
            Address = address ?? string.Empty;
            HasValue = hasValue;
        }

        public string DeclarationId { get; }
        public string Key { get; }
        public string ValueType { get; }
        public string Value { get; }
        public string Scope { get; }
        public string Lifetime { get; }
        public string Authority { get; }
        public string SyncPolicy { get; }
        public string FactProjection { get; }
        public string ProjectionIdentity { get; }
        public string SyncStatus { get; }
        public string Source { get; }
        public string CategoryPath { get; }
        public string Owner { get; }
        public string Address { get; }
        public bool HasValue { get; }
    }

    public readonly struct PipelineBlackboardAccessScope
    {
        public PipelineBlackboardAccessScope(
            BaseGraph graph,
            StateMachineExecutionPath executionPath,
            ulong actionInstanceId,
            ulong localLogicTick)
        {
            Graph = graph;
            ExecutionPath = executionPath;
            ActionInstanceId = actionInstanceId;
            LocalLogicTick = localLogicTick;
        }

        public BaseGraph Graph { get; }
        public StateMachineExecutionPath ExecutionPath { get; }
        public ulong ActionInstanceId { get; }
        public ulong LocalLogicTick { get; }
    }

    readonly struct PipelineBlackboardVariableAddress : IEquatable<PipelineBlackboardVariableAddress>
    {
        public PipelineBlackboardVariableAddress(
            string declarationId,
            PipelineBlackboardVariableScope scope,
            Guid ownerRuntimeId,
            string stateId,
            ulong ownerGenerationOrId)
        {
            DeclarationId = declarationId ?? string.Empty;
            Scope = scope;
            OwnerRuntimeId = ownerRuntimeId;
            StateId = stateId ?? string.Empty;
            OwnerGenerationOrId = ownerGenerationOrId;
        }

        public string DeclarationId { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public Guid OwnerRuntimeId { get; }
        public string StateId { get; }
        public ulong OwnerGenerationOrId { get; }

        public bool Equals(PipelineBlackboardVariableAddress other)
        {
            return Scope == other.Scope &&
                   OwnerRuntimeId.Equals(other.OwnerRuntimeId) &&
                   OwnerGenerationOrId == other.OwnerGenerationOrId &&
                   string.Equals(DeclarationId, other.DeclarationId, StringComparison.Ordinal) &&
                   string.Equals(StateId, other.StateId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PipelineBlackboardVariableAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Scope;
                hash = hash * 31 + OwnerRuntimeId.GetHashCode();
                hash = hash * 31 + OwnerGenerationOrId.GetHashCode();
                hash = hash * 31 + (DeclarationId ?? string.Empty).GetHashCode();
                hash = hash * 31 + (StateId ?? string.Empty).GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            switch (Scope)
            {
                case PipelineBlackboardVariableScope.Character:
                case PipelineBlackboardVariableScope.Graph:
                    return $"{Scope}:{OwnerRuntimeId:N}/{DeclarationId}";
                case PipelineBlackboardVariableScope.State:
                    return $"State:{OwnerRuntimeId:N}/{StateId}/{OwnerGenerationOrId}/{DeclarationId}";
                default:
                    return $"{Scope}:{OwnerGenerationOrId}/{DeclarationId}";
            }
        }
    }

    sealed class PipelineBlackboardVariableDeclaration
    {
        public PipelineBlackboardVariableDeclaration(BaseGraph graph, BaseExposedProperty source)
        {
            Source = source;
            DeclarationId = source.DeclarationId;
            OwnerId = source.DeclarationOwnerId;
            Key = source.BlackboardKey;
            ValueType = source.ValueType ?? typeof(object);
            DefaultValue = source.GetValue();
            Scope = source.BlackboardScope;
            Lifetime = source.BlackboardLifetime;
            Authority = source.BlackboardAuthority;
            SyncPolicy = source.BlackboardSyncPolicy;
            FactProjection = source.BlackboardFactProjection;
            ActionWindowType = source.ActionWindowType;
            ActionWindowId = source.ActionWindowId;
            ActionWindowDigest = source.ActionWindowDigest;
            SourceName = graph?.name ?? string.Empty;
            DisplayName = source.Name ?? string.Empty;
            CategoryPath = source.BlackboardCategoryPath;
            RegistrationCount = 1;
        }

        public BaseExposedProperty Source { get; private set; }
        public string DeclarationId { get; }
        public string OwnerId { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public object DefaultValue { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public PipelineBlackboardVariableLifetime Lifetime { get; }
        public PipelineBlackboardVariableAuthority Authority { get; }
        public PipelineBlackboardVariableSyncPolicy SyncPolicy { get; }
        public PipelineBlackboardFactProjectionKind FactProjection { get; }
        public string ActionWindowType { get; }
        public string ActionWindowId { get; }
        public ulong ActionWindowDigest { get; }
        public string SourceName { get; }
        public string DisplayName { get; }
        public string CategoryPath { get; }
        public int RegistrationCount { get; set; }

        public void RefreshSource(BaseExposedProperty source)
        {
            if (source != null)
                Source = source;
        }
    }

    public sealed class PipelineBlackboardRuntime
    {
        readonly Guid m_CharacterRuntimeId = Guid.NewGuid();
        readonly Dictionary<string, PipelineBlackboardVariableDeclaration> m_Declarations =
            new Dictionary<string, PipelineBlackboardVariableDeclaration>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_DeclarationIdsByOwnerKey =
            new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<Guid, List<string>> m_GraphDeclarationIds = new Dictionary<Guid, List<string>>();
        readonly Dictionary<PipelineBlackboardVariableAddress, object> m_Values =
            new Dictionary<PipelineBlackboardVariableAddress, object>();
        readonly Dictionary<PipelineBlackboardVariableAddress, string> m_ValueSources =
            new Dictionary<PipelineBlackboardVariableAddress, string>();
        readonly HashSet<PipelineBlackboardVariableAddress> m_SyncFactAddresses =
            new HashSet<PipelineBlackboardVariableAddress>();
        readonly HashSet<string> m_ReportedErrors = new HashSet<string>(StringComparer.Ordinal);
        readonly List<PipelineBlackboardVariableAddress> m_RemoveAddresses = new List<PipelineBlackboardVariableAddress>();
        readonly List<string> m_RemoveDeclarationIds = new List<string>();
        readonly List<PipelineBlackboardDebugEntry> m_DebugEntries = new List<PipelineBlackboardDebugEntry>();
        readonly List<ActionWindowProjectionCandidate> m_ActionWindowProjectionCandidates = new List<ActionWindowProjectionCandidate>();

        public IReadOnlyList<PipelineBlackboardDebugEntry> DebugEntries
        {
            get
            {
                RebuildDebugEntries();
                return m_DebugEntries;
            }
        }

        public void RegisterGraphVariables(
            BaseGraph graph,
            IReadOnlyList<BaseExposedProperty> variables,
            PipelineBlackboardAccessScope access)
        {
            if (graph == null || graph.RuntimeId == Guid.Empty || variables == null)
                return;

            if (!m_GraphDeclarationIds.TryGetValue(graph.RuntimeId, out List<string> graphDeclarationIds))
            {
                graphDeclarationIds = new List<string>();
                m_GraphDeclarationIds.Add(graph.RuntimeId, graphDeclarationIds);
            }

            var graphKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < variables.Count; i++)
            {
                BaseExposedProperty variable = variables[i];
                if (!variable)
                    continue;

                if (!ValidateDeclaration(graph, variable, graphKeys))
                    continue;

                PipelineBlackboardVariableDeclaration declaration = RegisterDeclaration(graph, variable);
                if (declaration == null)
                    continue;

                graphDeclarationIds.Add(declaration.DeclarationId);
                if (declaration.Scope != PipelineBlackboardVariableScope.State)
                    InitializeValue(access, declaration);
            }
        }

        public void UnregisterGraph(BaseGraph graph)
        {
            if (graph == null || graph.RuntimeId == Guid.Empty)
                return;

            ClearAddresses(address =>
                address.Scope == PipelineBlackboardVariableScope.Graph &&
                address.OwnerRuntimeId == graph.RuntimeId);

            if (!m_GraphDeclarationIds.TryGetValue(graph.RuntimeId, out List<string> declarationIds))
                return;

            for (int i = 0; i < declarationIds.Count; i++)
            {
                string declarationId = declarationIds[i];
                if (!m_Declarations.TryGetValue(declarationId, out PipelineBlackboardVariableDeclaration declaration))
                    continue;

                declaration.RegistrationCount--;
                if (declaration.RegistrationCount > 0)
                    continue;

                m_RemoveDeclarationIds.Add(declarationId);
                m_DeclarationIdsByOwnerKey.Remove(OwnerKey(declaration.OwnerId, declaration.Key));
            }

            m_GraphDeclarationIds.Remove(graph.RuntimeId);
            for (int i = 0; i < m_RemoveDeclarationIds.Count; i++)
            {
                string declarationId = m_RemoveDeclarationIds[i];
                m_Declarations.Remove(declarationId);
                ClearAddresses(address => string.Equals(address.DeclarationId, declarationId, StringComparison.Ordinal));
            }
            m_RemoveDeclarationIds.Clear();
        }

        public bool TryResolveDeclaration(
            PipelineBlackboardVariableReference reference,
            out BaseExposedProperty declaration)
        {
            declaration = null;
            if (!TryGetDeclaration(reference, out PipelineBlackboardVariableDeclaration registered))
                return false;

            declaration = registered.Source;
            return declaration != null;
        }

        public bool TryGetValue(
            PipelineBlackboardAccessScope access,
            PipelineBlackboardVariableReference reference,
            Type expectedType,
            out object value)
        {
            value = null;
            if (!TryGetDeclaration(reference, out PipelineBlackboardVariableDeclaration declaration))
                return false;

            if (!IsExpectedType(declaration.ValueType, expectedType))
            {
                ReportError(
                    $"ReadType:{declaration.DeclarationId}:{expectedType?.FullName}",
                    $"Pipeline blackboard '{declaration.Key}' is {declaration.ValueType.Name}, not {expectedType?.Name ?? "null"}.");
                return false;
            }

            if (!TryResolveAddress(access, declaration, out PipelineBlackboardVariableAddress address))
                return false;

            if (!m_Values.TryGetValue(address, out value))
                value = declaration.DefaultValue;

            return value == null || expectedType == null || expectedType.IsInstanceOfType(value);
        }

        public bool SetValue(
            PipelineBlackboardAccessScope access,
            PipelineBlackboardVariableReference reference,
            object value,
            PipelineBlackboardWriteProvenance provenance)
        {
            if (!TryGetDeclaration(reference, out PipelineBlackboardVariableDeclaration declaration))
                return false;

            if (declaration.Lifetime == PipelineBlackboardVariableLifetime.Config)
            {
                ReportError(
                    $"ConfigWrite:{declaration.DeclarationId}",
                    $"Pipeline blackboard config '{declaration.Key}' is read-only at runtime.");
                return false;
            }

            if (value != null && !declaration.ValueType.IsInstanceOfType(value))
            {
                ReportError(
                    $"WriteType:{declaration.DeclarationId}:{value.GetType().FullName}",
                    $"Pipeline blackboard '{declaration.Key}' expects {declaration.ValueType.Name}, got {value.GetType().Name}.");
                return false;
            }

            if (!TryResolveAddress(access, declaration, out PipelineBlackboardVariableAddress address))
                return false;

            if (!provenance.IsValid || provenance.LocalLogicTick != access.LocalLogicTick)
            {
                ReportError(
                    $"InvalidWriteProvenance:{declaration.DeclarationId}:{access.LocalLogicTick}",
                    $"Pipeline blackboard '{declaration.Key}' write has invalid Graph/runtime provenance.");
                return false;
            }

            if (value == null)
            {
                RemoveAddress(address);
                return true;
            }

            m_Values[address] = value;
            m_ValueSources[address] = provenance.DebugIdentity;
            if (declaration.FactProjection == PipelineBlackboardFactProjectionKind.ActionWindow &&
                value is bool active && active)
            {
                if (!provenance.ActionContext.IsValid)
                {
                    ReportError(
                        $"MissingActionContext:{declaration.DeclarationId}:{provenance.GraphRuntimeId:N}",
                        $"Pipeline blackboard '{declaration.Key}' ActionWindow projection requires explicit Action Context provenance.");
                }
                else
                {
                    m_ActionWindowProjectionCandidates.Add(new ActionWindowProjectionCandidate(address, declaration, provenance));
                }
            }
            return true;
        }

        internal void ConsumeActionWindowProjectionCandidates(List<ActionWindowProjectionCandidate> output)
        {
            if (output != null)
                output.AddRange(m_ActionWindowProjectionCandidates);
            m_ActionWindowProjectionCandidates.Clear();
        }

        internal void MarkProjectionProduced(ActionWindowProjectionCandidate candidate)
        {
            m_SyncFactAddresses.Add(candidate.Address);
        }

        public void MarkSyncFactProduced(
            PipelineBlackboardAccessScope access,
            PipelineBlackboardVariableReference reference)
        {
            if (TryGetDeclaration(reference, out PipelineBlackboardVariableDeclaration declaration) &&
                TryResolveAddress(access, declaration, out PipelineBlackboardVariableAddress address))
                m_SyncFactAddresses.Add(address);
        }

        public void BeginFrame(ulong localLogicTick)
        {
            ClearAddresses(address => address.Scope == PipelineBlackboardVariableScope.Frame);
            m_ActionWindowProjectionCandidates.Clear();
        }

        public void EndFrame(ulong localLogicTick)
        {
            ClearAddresses(address =>
                address.Scope == PipelineBlackboardVariableScope.Frame &&
                address.OwnerGenerationOrId == localLogicTick);
            m_ActionWindowProjectionCandidates.Clear();
        }

        public void EnterState(StateMachineExecutionScope scope)
        {
            if (!scope.IsValid)
                return;

            foreach (PipelineBlackboardVariableDeclaration declaration in m_Declarations.Values)
            {
                if (declaration.Scope != PipelineBlackboardVariableScope.State ||
                    !string.Equals(declaration.OwnerId, scope.StateBodyGraphOwnerId, StringComparison.Ordinal))
                    continue;

                var address = new PipelineBlackboardVariableAddress(
                    declaration.DeclarationId,
                    declaration.Scope,
                    scope.RuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration);
                m_Values[address] = declaration.DefaultValue;
            }
        }

        public void ExitState(StateMachineExecutionScope scope)
        {
            if (!scope.IsValid)
                return;

            ClearAddresses(address =>
                address.Scope == PipelineBlackboardVariableScope.State &&
                address.OwnerRuntimeId == scope.RuntimeId &&
                address.OwnerGenerationOrId == scope.ActivationGeneration &&
                string.Equals(address.StateId, scope.StateId, StringComparison.Ordinal));
        }

        public void ClearActionInstance(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                return;

            ClearAddresses(address =>
                address.Scope == PipelineBlackboardVariableScope.ActionInstance &&
                address.OwnerGenerationOrId == actionInstanceId);
        }

        public void ClearValues()
        {
            m_Values.Clear();
            m_ValueSources.Clear();
            m_SyncFactAddresses.Clear();
            m_DebugEntries.Clear();
            m_ActionWindowProjectionCandidates.Clear();
        }

        public void Reset()
        {
            ClearValues();
            m_Declarations.Clear();
            m_DeclarationIdsByOwnerKey.Clear();
            m_GraphDeclarationIds.Clear();
            m_ReportedErrors.Clear();
        }

        bool ValidateDeclaration(BaseGraph graph, BaseExposedProperty variable, HashSet<string> graphKeys)
        {
            if (string.IsNullOrEmpty(graph.GraphAuthoringId))
            {
                ReportError($"MissingOwner:{graph.RuntimeId}", $"Pipeline blackboard graph '{graph.name}' has no declaration owner id.");
                return false;
            }

            if (string.IsNullOrEmpty(variable.DeclarationId) || string.IsNullOrEmpty(variable.BlackboardKey))
            {
                ReportError($"MissingDeclaration:{graph.RuntimeId}:{variable.Name}", $"Pipeline blackboard declaration '{variable.Name}' is missing id or key.");
                return false;
            }

            if (!graphKeys.Add(variable.BlackboardKey))
            {
                ReportError($"DuplicateOwnerKey:{graph.GraphAuthoringId}:{variable.BlackboardKey}", $"Graph '{graph.name}' has duplicate blackboard key '{variable.BlackboardKey}'.");
                return false;
            }

            if (!PipelineBlackboardVariablePolicy.IsValid(variable.BlackboardScope, variable.BlackboardLifetime))
            {
                ReportError(
                    $"InvalidPolicy:{variable.DeclarationId}",
                    $"Pipeline blackboard '{variable.BlackboardKey}' has invalid {variable.BlackboardScope}/{variable.BlackboardLifetime}.");
                return false;
            }

            if (!PipelineBlackboardFactProjectionPolicy.TryValidate(variable, out string projectionError))
            {
                ReportError(
                    $"InvalidProjection:{variable.DeclarationId}",
                    $"Pipeline blackboard '{variable.BlackboardKey}' projection is invalid: {projectionError}");
                return false;
            }

            return true;
        }

        PipelineBlackboardVariableDeclaration RegisterDeclaration(BaseGraph graph, BaseExposedProperty variable)
        {
            var incoming = new PipelineBlackboardVariableDeclaration(graph, variable);
            string ownerKey = OwnerKey(incoming.OwnerId, incoming.Key);
            if (m_DeclarationIdsByOwnerKey.TryGetValue(ownerKey, out string ownerKeyDeclarationId) &&
                !string.Equals(ownerKeyDeclarationId, incoming.DeclarationId, StringComparison.Ordinal))
            {
                ReportError($"DuplicateOwnerKey:{ownerKey}", $"Pipeline blackboard owner '{incoming.OwnerId}' has duplicate key '{incoming.Key}'.");
                return null;
            }

            if (m_Declarations.TryGetValue(incoming.DeclarationId, out PipelineBlackboardVariableDeclaration existing))
            {
                if (!DeclarationsMatch(existing, incoming))
                {
                    ReportError($"DuplicateDeclaration:{incoming.DeclarationId}", $"Pipeline blackboard declaration id '{incoming.DeclarationId}' has conflicting definitions.");
                    return null;
                }

                existing.RegistrationCount++;
                existing.RefreshSource(variable);
                return existing;
            }

            m_Declarations.Add(incoming.DeclarationId, incoming);
            m_DeclarationIdsByOwnerKey.Add(ownerKey, incoming.DeclarationId);
            return incoming;
        }

        bool TryGetDeclaration(
            PipelineBlackboardVariableReference reference,
            out PipelineBlackboardVariableDeclaration declaration)
        {
            declaration = null;
            if (!reference.IsValid)
            {
                ReportError("InvalidReference", "Pipeline blackboard variable reference is missing declaration or owner identity.");
                return false;
            }

            if (!m_Declarations.TryGetValue(reference.DeclarationId, out declaration))
            {
                ReportError($"MissingDeclaration:{reference.DeclarationId}", $"Pipeline blackboard declaration '{reference.DisplayKey}/{reference.DeclarationId}' is not registered.");
                return false;
            }

            if (!string.Equals(declaration.OwnerId, reference.DeclarationOwnerId, StringComparison.Ordinal))
            {
                ReportError($"OwnerMismatch:{reference.DeclarationId}", $"Pipeline blackboard reference '{reference.DisplayKey}' points to the wrong declaration owner.");
                declaration = null;
                return false;
            }

            return true;
        }

        bool TryResolveAddress(
            PipelineBlackboardAccessScope access,
            PipelineBlackboardVariableDeclaration declaration,
            out PipelineBlackboardVariableAddress address)
        {
            address = default;
            switch (declaration.Scope)
            {
                case PipelineBlackboardVariableScope.Character:
                    address = new PipelineBlackboardVariableAddress(declaration.DeclarationId, declaration.Scope, m_CharacterRuntimeId, string.Empty, 0);
                    return true;
                case PipelineBlackboardVariableScope.Graph:
                    BaseGraph ownerGraph = access.Graph?.ResolveRuntimeGraph(declaration.OwnerId);
                    if (ownerGraph == null || ownerGraph.RuntimeId == Guid.Empty)
                        return ReportMissingOwner(declaration, "Graph runtime");
                    address = new PipelineBlackboardVariableAddress(declaration.DeclarationId, declaration.Scope, ownerGraph.RuntimeId, string.Empty, 0);
                    return true;
                case PipelineBlackboardVariableScope.State:
                    if (!TryResolveStateScope(access, declaration, out StateMachineExecutionScope stateScope))
                        return false;
                    address = new PipelineBlackboardVariableAddress(
                        declaration.DeclarationId,
                        declaration.Scope,
                        stateScope.RuntimeId,
                        stateScope.StateId,
                        stateScope.ActivationGeneration);
                    return true;
                case PipelineBlackboardVariableScope.ActionInstance:
                    if (access.ActionInstanceId == 0)
                        return ReportMissingOwner(declaration, "ActionInstanceId");
                    address = new PipelineBlackboardVariableAddress(declaration.DeclarationId, declaration.Scope, Guid.Empty, string.Empty, access.ActionInstanceId);
                    return true;
                case PipelineBlackboardVariableScope.Frame:
                    if (access.LocalLogicTick == 0)
                        return ReportMissingOwner(declaration, "LocalLogicTick");
                    address = new PipelineBlackboardVariableAddress(declaration.DeclarationId, declaration.Scope, Guid.Empty, string.Empty, access.LocalLogicTick);
                    return true;
                default:
                    return false;
            }
        }

        void InitializeValue(PipelineBlackboardAccessScope access, PipelineBlackboardVariableDeclaration declaration)
        {
            if (declaration.Lifetime == PipelineBlackboardVariableLifetime.Config ||
                declaration.Lifetime == PipelineBlackboardVariableLifetime.ManualClear)
                return;

            if (declaration.Scope == PipelineBlackboardVariableScope.ActionInstance && access.ActionInstanceId == 0 ||
                declaration.Scope == PipelineBlackboardVariableScope.Frame && access.LocalLogicTick == 0)
                return;

            if (TryResolveAddress(access, declaration, out PipelineBlackboardVariableAddress address) && !m_Values.ContainsKey(address))
                m_Values.Add(address, declaration.DefaultValue);
        }

        bool ReportMissingOwner(PipelineBlackboardVariableDeclaration declaration, string owner)
        {
            ReportError($"MissingOwner:{declaration.DeclarationId}:{owner}", $"Pipeline blackboard '{declaration.Key}' requires {owner}.");
            return false;
        }

        bool TryResolveStateScope(
            PipelineBlackboardAccessScope access,
            PipelineBlackboardVariableDeclaration declaration,
            out StateMachineExecutionScope stateScope)
        {
            stateScope = default;
            BaseGraph declarationGraph = access.Graph?.ResolveRuntimeGraph(declaration.OwnerId);
            int matches = 0;
            for (int i = 0; i < access.ExecutionPath.Count; i++)
            {
                StateMachineExecutionScope frame = access.ExecutionPath[i];
                bool matchesOwner = string.Equals(
                    frame.StateBodyGraphOwnerId,
                    declaration.OwnerId,
                    StringComparison.Ordinal);
                if (!matchesOwner && declarationGraph != null)
                {
                    for (BaseGraph graph = declarationGraph; graph != null; graph = graph.ParentRuntimeGraph)
                    {
                        if (graph.RuntimeId == frame.StateBodyGraphRuntimeId)
                        {
                            matchesOwner = true;
                            break;
                        }
                    }
                }

                if (!matchesOwner)
                    continue;
                stateScope = frame;
                matches++;
            }

            if (matches == 1)
                return true;

            string code = matches == 0 ? "StateOwnerNotInExecutionPath" : "AmbiguousStateOwnerInExecutionPath";
            ReportError(
                $"{code}:{declaration.DeclarationId}",
                $"Pipeline blackboard '{declaration.Key}' owner '{declaration.OwnerId}' resolved to {matches} StateMachine execution frames.");
            stateScope = default;
            return false;
        }

        void ClearAddresses(Predicate<PipelineBlackboardVariableAddress> predicate)
        {
            m_RemoveAddresses.Clear();
            foreach (var pair in m_Values)
            {
                if (predicate(pair.Key))
                    m_RemoveAddresses.Add(pair.Key);
            }
            foreach (PipelineBlackboardVariableAddress address in m_SyncFactAddresses)
            {
                if (predicate(address) && !m_RemoveAddresses.Contains(address))
                    m_RemoveAddresses.Add(address);
            }

            for (int i = 0; i < m_RemoveAddresses.Count; i++)
                RemoveAddress(m_RemoveAddresses[i]);
            m_RemoveAddresses.Clear();
        }

        void RemoveAddress(PipelineBlackboardVariableAddress address)
        {
            m_Values.Remove(address);
            m_ValueSources.Remove(address);
            m_SyncFactAddresses.Remove(address);
        }

        void RebuildDebugEntries()
        {
            m_DebugEntries.Clear();
            foreach (var pair in m_Declarations)
            {
                PipelineBlackboardVariableDeclaration declaration = pair.Value;
                bool foundAddress = false;
                foreach (var valuePair in m_Values)
                {
                    if (!string.Equals(valuePair.Key.DeclarationId, declaration.DeclarationId, StringComparison.Ordinal))
                        continue;

                    foundAddress = true;
                    AddDebugEntry(declaration, valuePair.Key, valuePair.Value, true);
                }

                if (!foundAddress)
                    AddDebugEntry(declaration, default, declaration.DefaultValue, false);
            }
        }

        void AddDebugEntry(
            PipelineBlackboardVariableDeclaration declaration,
            PipelineBlackboardVariableAddress address,
            object value,
            bool hasValue)
        {
            bool syncFactProduced = hasValue && m_SyncFactAddresses.Contains(address);
            m_DebugEntries.Add(new PipelineBlackboardDebugEntry(
                declaration.DeclarationId,
                declaration.Key,
                declaration.ValueType.Name,
                ValueSummary(value),
                declaration.Scope.ToString(),
                declaration.Lifetime.ToString(),
                declaration.Authority.ToString(),
                declaration.SyncPolicy.ToString(),
                declaration.FactProjection.ToString(),
                declaration.FactProjection == PipelineBlackboardFactProjectionKind.ActionWindow
                    ? $"{declaration.ActionWindowType}/{declaration.ActionWindowId}/{declaration.ActionWindowDigest}"
                    : string.Empty,
                ResolveSyncStatus(declaration, syncFactProduced),
                hasValue && m_ValueSources.TryGetValue(address, out string source) ? source : declaration.SourceName,
                declaration.CategoryPath,
                declaration.OwnerId,
                hasValue ? address.ToString() : "Unbound",
                hasValue));
        }

        static bool DeclarationsMatch(
            PipelineBlackboardVariableDeclaration left,
            PipelineBlackboardVariableDeclaration right)
        {
            return string.Equals(left.OwnerId, right.OwnerId, StringComparison.Ordinal) &&
                   string.Equals(left.Key, right.Key, StringComparison.Ordinal) &&
                   left.ValueType == right.ValueType &&
                   left.Scope == right.Scope &&
                   left.Lifetime == right.Lifetime &&
                   left.Authority == right.Authority &&
                   left.SyncPolicy == right.SyncPolicy &&
                   ProjectionsMatch(left, right);
        }

        static bool ProjectionsMatch(
            PipelineBlackboardVariableDeclaration left,
            PipelineBlackboardVariableDeclaration right)
        {
            if (left.FactProjection != right.FactProjection)
                return false;
            return left.FactProjection == PipelineBlackboardFactProjectionKind.None ||
                   string.Equals(left.ActionWindowType, right.ActionWindowType, StringComparison.Ordinal) &&
                   string.Equals(left.ActionWindowId, right.ActionWindowId, StringComparison.Ordinal) &&
                   left.ActionWindowDigest == right.ActionWindowDigest;
        }

        static bool IsExpectedType(Type declarationType, Type expectedType)
        {
            return expectedType != null &&
                   (expectedType.IsAssignableFrom(declarationType) || declarationType.IsAssignableFrom(expectedType));
        }

        static string OwnerKey(string ownerId, string key)
        {
            return $"{ownerId}\u001f{key}";
        }

        static string ResolveSyncStatus(PipelineBlackboardVariableDeclaration declaration, bool syncFactProduced)
        {
            switch (declaration.SyncPolicy)
            {
                case PipelineBlackboardVariableSyncPolicy.ConfigVersion:
                    return "Config identity";
                case PipelineBlackboardVariableSyncPolicy.InputDerived:
                    return "Derived locally";
                case PipelineBlackboardVariableSyncPolicy.SyncFact:
                    return syncFactProduced ? "Explicit SyncFacts output observed" : "Missing explicit SyncFacts output";
                case PipelineBlackboardVariableSyncPolicy.ReplicatedCue:
                    return syncFactProduced ? "Presentation SyncDomain output observed" : "Missing replicated cue output";
                case PipelineBlackboardVariableSyncPolicy.CorrectionOnly:
                    return "Correction domain only";
                default:
                    return syncFactProduced ? "SyncFacts output observed but policy is None" : "Blackboard only";
            }
        }

        static string ValueSummary(object value)
        {
            if (value == null)
                return "null";
            if (value is Vector2 vector2)
                return $"{vector2.x:0.###}, {vector2.y:0.###}";
            if (value is Vector3 vector3)
                return $"{vector3.x:0.###}, {vector3.y:0.###}, {vector3.z:0.###}";
            return value.ToString();
        }

        void ReportError(string key, string message)
        {
            if (m_ReportedErrors.Add(key))
                Debug.LogError(message);
        }
    }
}
