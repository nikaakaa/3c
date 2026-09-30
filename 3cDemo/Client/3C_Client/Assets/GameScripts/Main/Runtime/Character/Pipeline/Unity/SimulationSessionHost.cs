using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonPerformance.Instrumentation;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class SimulationSessionHost : MonoBehaviour, IGameplayRenderFrameInputTarget, IGameplayLogicTickTarget,
        ICharacterFutureBodyTranslationSource
    {
#if UNITY_EDITOR
        public static event Action<string, string, long> StartupMilestone;

        public static void ReportStartupMilestone(string sessionId, string phase)
        {
            var listener = StartupMilestone;
            if (listener != null)
                listener(sessionId, phase, System.Diagnostics.Stopwatch.GetTimestamp());
        }

        void MarkStartup(string phase)
        {
            ReportStartupMilestone(m_Composition.SessionId, phase);
        }
#endif
        [SerializeField] SimulationSessionCompositionDefinition m_Composition;

        readonly List<ISimulationActorRegistration> m_Registrations =
            new List<ISimulationActorRegistration>();
        readonly SimulationSessionHistory m_History = new SimulationSessionHistory();
        ISimulationSessionCompositionPreparation m_Preparation;
        SimulationSessionLaunchPlan m_LaunchPlan;
        ISimulationSessionRuntimeHandle m_Runtime;
        ISimulationSessionOutputLifecycle m_OutputLifecycle;
        SimulationTickSourceKind m_OuterTickKind;
        SimulationSessionLifecycleState m_State = SimulationSessionLifecycleState.Uninitialized;
        SimulationSessionFailure m_Failure;
        SimulationSessionDiagnosticsSnapshot m_LastDiagnostics;
        SimulationSessionHostDebugControlPort m_DebugControlPort;
        bool m_TickTargetsRegistered;
        bool m_Quiesced;
        bool m_Disposed;
        ulong m_SessionGeneration = 1;

        public TimelineRuntimeNumericTarget TimelineNumericTarget
        {
            get
            {
                if (m_Composition == null)
                    throw new InvalidOperationException($"Simulation Session Host '{name}' requires a bound Session Composition to resolve the Timeline numeric target.");
                return m_Composition.ExecutionBackend.TimelineNumericTarget;
            }
        }

        public SimulationSessionCompositionDefinition Composition => m_Composition;
        public int TickRate => m_Composition.TickRate;
        public SimulationSessionLifecycleState LifecycleState => m_State;
        public SimulationSessionFailure Failure => m_Failure;
        public SimulationSessionLaunchPlan LaunchPlan => m_LaunchPlan;
        public SimulationSessionDiagnosticsSnapshot Diagnostics =>
            m_Runtime?.Diagnostics ?? m_Preparation?.Diagnostics ?? m_LastDiagnostics;
        public int RegistrationCount => m_Registrations.Count;
        public bool IsQuiesced => m_Quiesced;
        public bool SupportsInputReplay => m_Runtime is ISimulationSessionInputReplayRuntime;
        public bool SupportsPresentationCheckpointRestore =>
            SimulationSessionHistory.SupportsPresentationCheckpointRestore(m_Registrations);
        public bool IsInputRecording =>
            m_Runtime is ISimulationSessionInputReplayRuntime replay && replay.IsInputRecording;
        public ulong LatestCheckpointTick => m_History.LatestCheckpointTick;
        public ulong OldestCheckpointTick => m_History.OldestCheckpointTick;
        public int CheckpointCount => m_History.CheckpointCount;
        public string LastCheckpointFailure => m_History.LastCheckpointFailure;
        public Guid ExecutionBranchId => m_History.ExecutionBranchId;
        public Guid ParentExecutionBranchId => m_History.ParentExecutionBranchId;
        public ulong ExecutionBranchBaseTick => m_History.ExecutionBranchBaseTick;
        public ulong SessionGeneration => m_SessionGeneration;
        public string PredictionSourceIdentity =>
            m_Runtime is ICharacterFutureBodyTranslationSource source
                ? source.PredictionSourceIdentity
                : string.Empty;

        public bool TryPredict(
            in CharacterFutureBodyTranslationRequest request,
            CharacterFutureBodyTranslation output)
        {
            if (m_Disposed || m_Quiesced || m_State != SimulationSessionLifecycleState.Active ||
                m_Runtime is not ICharacterFutureBodyTranslationSource source)
            {
                output?.Clear();
                return false;
            }
            return source.TryPredict(in request, output);
        }

        public void BindComposition(SimulationSessionCompositionDefinition composition)
        {
            RequireAlive();
            if (m_State != SimulationSessionLifecycleState.Uninitialized || m_Registrations.Count != 0)
                throw new InvalidOperationException("Session Composition can only be bound before Actor registration and preparation.");
            m_Composition = composition ? composition : throw new ArgumentNullException(nameof(composition));
        }

        public void RegisterActor(ISimulationActorRegistration registration)
        {
            RequireAlive();
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));
            if (m_State != SimulationSessionLifecycleState.Uninitialized)
                throw new InvalidOperationException("Actor registrations are immutable after Session preparation starts.");
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                ISimulationActorRegistration current = m_Registrations[i];
                if (current.ActorId.Equals(registration.ActorId))
                    throw new InvalidOperationException($"Session Host already contains ActorId '{registration.ActorId}'.");
                if (string.Equals(current.OwnerIdentity, registration.OwnerIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Registration owner '{registration.OwnerIdentity}' already registered an Actor.");
            }
            m_Registrations.Add(registration);
            m_Registrations.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
        }

        public void ReleaseActor(ISimulationActorRegistration registration)
        {
            if (registration == null || m_Disposed)
                return;
            if (!m_Registrations.Contains(registration))
                throw new InvalidOperationException("Character Host attempted to release an unknown Actor registration.");
            if (m_State == SimulationSessionLifecycleState.Uninitialized)
            {
                m_Registrations.Remove(registration);
                registration.Dispose();
                return;
            }
            Fail(new SimulationSessionFailure(
                SimulationSessionFailureStage.Runtime,
                "active_actor_registration_released",
                $"Actor '{registration.ActorId}' was released after the Session roster was locked.",
                registration.ActorId.Value));
        }

        public bool TryCaptureCheckpointNow(out ulong checkpointTick, out string error)
        {
            checkpointTick = 0;
            if (m_Disposed || m_State != SimulationSessionLifecycleState.Active || m_Quiesced)
            {
                error = "Simulation Session is not active for a skill checkpoint.";
                return false;
            }
            return m_History.TryCaptureCheckpointNow(out checkpointTick, out error);
        }

        public bool TryRestoreCheckpoint(ulong tick, out string error)
        {
            if (m_Disposed || m_State != SimulationSessionLifecycleState.Active || m_Quiesced)
            {
                error = "Simulation Session is not active for checkpoint restore.";
                return false;
            }
            return m_History.TryRestoreCheckpoint(tick, out error);
        }

        public bool TryRestoreToTick(ulong targetTick, out ulong restoredCheckpointTick, out string error)
        {
            restoredCheckpointTick = 0;
            if (m_Disposed || m_State != SimulationSessionLifecycleState.Active || m_Quiesced)
            {
                error = "Simulation Session is not active for historical restore.";
                return false;
            }
            return m_History.TryRestoreToTick(targetTick, out restoredCheckpointTick, out error);
        }

        public bool TryReplayInputRange(ulong fromTick, ulong toTick, out string error)
        {
            if (m_Disposed || m_State != SimulationSessionLifecycleState.Active || m_Quiesced)
            {
                error = "Simulation Session is not active for input replay.";
                return false;
            }
            return m_History.TryReplayInputRange(fromTick, toTick, out error);
        }

        public void Stop()
        {
            DisposeSession();
        }

        public bool TryGetOnlyActorId(out ActorId actorId)
        {
            if (m_Registrations.Count == 1)
            {
                actorId = m_Registrations[0].ActorId;
                return true;
            }
            actorId = default;
            return false;
        }

        public bool TryStartInputRecording(out string error)
        {
            if (m_Runtime is ISimulationSessionInputReplayRuntime replay)
                return replay.TryStartInputRecording(out error);
            error = "Active Session runtime does not expose input recording.";
            return false;
        }

        public bool TryStopInputRecording(out string error)
        {
            if (m_Runtime is ISimulationSessionInputReplayRuntime replay)
                return replay.TryStopInputRecording(out error);
            error = "Active Session runtime does not expose input recording.";
            return false;
        }

        public void Quiesce()
        {
            RequireAlive();
            if (m_Quiesced)
                return;
            var failures = new List<Exception>();
            TryCleanup(UnregisterTickTargets, failures);
            TryCleanup(DeactivateActorPorts, failures);
            if (failures.Count != 0)
                throw new AggregateException("Simulation Session failed to quiesce completely.", failures);
            m_Quiesced = true;
        }

        public void ReleaseSessionRuntime()
        {
            RequireAlive();
            if (!m_Quiesced)
                throw new InvalidOperationException("Simulation Session must be quiesced before releasing its runtime.");
            var failures = new List<Exception>();
            ReleaseSessionResources(failures);
            if (failures.Count != 0)
                throw new AggregateException("Simulation Session runtime failed to release completely.", failures);
            CompleteSessionDisposal();
        }

        [PerformanceProbe("session.input")]
        public void BeginRenderFrame(ulong renderFrame)
        {
            if (m_Disposed || m_Quiesced || m_State != SimulationSessionLifecycleState.Active)
                return;
            try
            {
                for (int i = 0; i < m_Registrations.Count; i++)
                    m_Registrations[i].CaptureRenderFrame(renderFrame);
            }
            catch (Exception exception)
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Ingress,
                    "session_input_capture_failed",
                    exception.Message,
                    m_Composition ? m_Composition.name : string.Empty));
                throw;
            }
        }

        public void LogicTick(GameplayLogicTickContext context)
        {
            if (m_Disposed || m_Quiesced || m_State == SimulationSessionLifecycleState.Failed ||
                m_State == SimulationSessionLifecycleState.Disposed)
            {
                return;
            }
            try
            {
                if (m_State == SimulationSessionLifecycleState.Uninitialized)
                {
#if UNITY_EDITOR
                    MarkStartup("session-first-logic-tick");
#endif
                    BeginPreparation();
                    StepPreparation(context);
                    return;
                }
                if (m_State == SimulationSessionLifecycleState.Preparing)
                {
                    StepPreparation(context);
                    return;
                }
                if (ActorStartGatesReady())
                    ExecuteActiveLogicTick(context);
            }
            catch (SimulationSessionCompositionException exception)
            {
                Fail(exception.Failure);
                throw;
            }
            catch (Exception exception)
            {
                SimulationSessionFailure failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Runtime,
                    "session_runtime_tick_failed",
                    exception.Message,
                    m_LaunchPlan?.Descriptor.Identity.ToString() ?? string.Empty);
                Fail(failure);
                throw new SimulationSessionCompositionException(failure, exception);
            }
        }

        [PerformanceProbe("session.logic-tick")]
        void ExecuteActiveLogicTick(GameplayLogicTickContext context)
        {
            m_OutputLifecycle.BeginLogicTick();
            m_Runtime.LogicTick(BuildRuntimeContext(context, m_LaunchPlan.Descriptor.SourceClockId));
            m_History.CompleteLogicTick(context.LocalLogicTick);
        }

        void Awake()
        {
            if (!m_Composition)
                Debug.LogError("SimulationSessionHost requires an explicit Session Composition Definition.", this);
        }

        void OnEnable() => Activate();

        public void Activate()
        {
            if (m_Disposed)
                BeginFreshLifecycle();
            if (m_Quiesced || m_State == SimulationSessionLifecycleState.Failed || !m_Composition)
                return;
            TryActivateWithGameplayTickSystem();
        }

        void Update()
        {
            if (m_TickTargetsRegistered || m_Disposed || m_Quiesced ||
                m_State == SimulationSessionLifecycleState.Failed || !m_Composition)
            {
                return;
            }
            TryActivateWithGameplayTickSystem();
        }

        void TryActivateWithGameplayTickSystem()
        {
            if (!GameplayTickSystem.IsInitialized)
                return;
            try
            {
                RegisterTickTargets();
                if (m_State == SimulationSessionLifecycleState.Active)
                    ActivateActorPorts();
            }
            catch (Exception exception)
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Composition,
                    "session_host_activation_failed",
                    exception.Message,
                    name));
            }
        }

        void OnDisable()
        {
            Stop();
        }

        void OnDestroy()
        {
            Stop();
        }

        void BeginPreparation()
        {
            if (!m_Composition)
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Composition,
                    "composition_definition_missing",
                    "SimulationSessionHost has no explicit Session Composition Definition.",
                    name));
                return;
            }
            if (m_Registrations.Count == 0)
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Composition,
                    "actor_roster_missing",
                    "SimulationSessionHost has no Actor registrations.",
                    name));
                return;
            }
            try
            {
#if UNITY_EDITOR
                MarkStartup("session-preparation-start");
#endif
                m_Composition.RequireComplete();
                if (GameplayTickSystem.Current.Settings.LocalLogicTickRate != m_Composition.TickRate)
                    throw new InvalidOperationException("Session Composition TickRate does not match GameplayTickSystem LocalLogic TickRate.");
                m_Preparation = m_Composition.CreatePreparation(m_Registrations);
#if UNITY_EDITOR
                MarkStartup("session-preparation-created");
#endif
                m_State = SimulationSessionLifecycleState.Preparing;
            }
            catch (Exception exception)
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Composition,
                    "session_preparation_creation_failed",
                    exception.Message,
                    m_Composition.name));
            }
        }

        void StepPreparation(GameplayLogicTickContext context)
        {
            if (m_State != SimulationSessionLifecycleState.Preparing || m_Preparation == null)
                return;
#if UNITY_EDITOR
            MarkStartup("session-first-step-start");
#endif
            SimulationSessionLogicTickContext preparationContext = new SimulationSessionLogicTickContext(
                new SimulationTickSourceIdentity(
                    m_Preparation.SourceDescriptor.OuterTickKind,
                    m_Composition.SourceClockId,
                    context.LocalLogicTick),
                new WorldRevision(m_Composition.WorldRevision),
                ToElapsedTicks(context.FixedDeltaSeconds));
            SimulationSessionPreparationStatus status = m_Preparation.Step(preparationContext);
            if (status == SimulationSessionPreparationStatus.Pending)
            {
#if UNITY_EDITOR
                MarkStartup("session-first-pending-step-returned");
#endif
                return;
            }
            if (status == SimulationSessionPreparationStatus.Failed)
            {
                Fail(m_Preparation.Failure ?? new SimulationSessionFailure(
                    SimulationSessionFailureStage.Preparation,
                    "session_preparation_failed",
                    "Session preparation failed without a structured failure.",
                    m_Composition.name));
                return;
            }
#if UNITY_EDITOR
            MarkStartup("session-preparation-step-ready");
#endif
            SimulationSessionPreparedRuntime prepared = m_Preparation.TakePreparedRuntime();
            m_LaunchPlan = prepared.LaunchPlan;
            m_Runtime = prepared.RuntimeHandle;
            m_History.BindRuntime(m_Runtime, m_Registrations);
            m_OutputLifecycle = prepared.OutputLifecycle;
            m_OuterTickKind = prepared.OuterTickKind;
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                m_Registrations[i].BindExecutionBranch(m_History.ExecutionBranchId);
            }
            m_Preparation.Dispose();
            m_Preparation = null;
            ActivateActorPorts();
            CaptureActorInputs(context.RenderFrame);
            m_State = SimulationSessionLifecycleState.Active;
#if UNITY_EDITOR
            MarkStartup("session-active");
#endif
            RegisterDebugControlPort();
        }

        SimulationSessionLogicTickContext BuildRuntimeContext(
            GameplayLogicTickContext context,
            SimulationSourceClockId sourceClock)
        {
            if (m_LaunchPlan == null)
                throw new InvalidOperationException("Active Session has no formal outer Tick mapping.");
            return new SimulationSessionLogicTickContext(
                new SimulationTickSourceIdentity(m_OuterTickKind, sourceClock.Value, context.LocalLogicTick),
                new WorldRevision(m_Composition.WorldRevision),
                ToElapsedTicks(context.FixedDeltaSeconds));
        }

        void ActivateActorPorts()
        {
            int activatedCount = 0;
            try
            {
                for (int i = 0; i < m_Registrations.Count; i++)
                {
                    m_Registrations[i].Activate();
                    activatedCount++;
                }
            }
            catch (Exception exception)
            {
                var failures = new List<Exception> { exception };
                int last = Math.Min(activatedCount, m_Registrations.Count - 1);
                for (int i = last; i >= 0; i--)
                    TryCleanup(m_Registrations[i].Deactivate, failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("Actor ports failed to activate transactionally.", failures);
            }
        }

        void CaptureActorInputs(ulong renderFrame)
        {
            for (int i = 0; i < m_Registrations.Count; i++)
                m_Registrations[i].CaptureRenderFrame(renderFrame);
        }

        bool ActorStartGatesReady()
        {
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                if (m_Registrations[i] is ISimulationActorStartGate gate &&
                    !gate.IsSimulationStartReady)
                {
                    return false;
                }
            }
            return true;
        }

        void DeactivateActorPorts()
        {
            var failures = new List<Exception>();
            for (int i = m_Registrations.Count - 1; i >= 0; i--)
                TryCleanup(m_Registrations[i].Deactivate, failures);
            if (failures.Count != 0)
                throw new AggregateException("Actor ports failed to deactivate completely.", failures);
        }

        void RegisterTickTargets()
        {
            if (m_TickTargetsRegistered)
                return;
            if (!GameplayTickSystem.RegisterInputTarget(this))
                throw new InvalidOperationException("GameplayTickSystem rejected the Simulation Session targets.");
            if (!GameplayTickSystem.RegisterLogicTarget(this))
            {
                try
                {
                    GameplayTickSystem.UnregisterInputTarget(this);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(
                        "GameplayTickSystem rejected the Logic target and Input target cleanup failed.",
                        cleanup);
                }
                throw new InvalidOperationException("GameplayTickSystem rejected the Simulation Session targets.");
            }
            m_TickTargetsRegistered = true;
        }

        void UnregisterTickTargets()
        {
            if (!m_TickTargetsRegistered)
                return;
            var failures = new List<Exception>();
            TryCleanup(() => GameplayTickSystem.UnregisterInputTarget(this), failures);
            TryCleanup(() => GameplayTickSystem.UnregisterLogicTarget(this), failures);
            m_TickTargetsRegistered = false;
            if (failures.Count != 0)
                throw new AggregateException("Simulation Session Tick targets failed to unregister completely.", failures);
        }

        void Fail(SimulationSessionFailure failure)
        {
            if (m_Disposed || m_State == SimulationSessionLifecycleState.Disposed)
                return;
            m_Failure = failure ?? throw new ArgumentNullException(nameof(failure));
            m_State = SimulationSessionLifecycleState.Failed;
            m_LastDiagnostics = m_Runtime?.Diagnostics ?? m_Preparation?.Diagnostics ?? BuildFailureDiagnostics(failure);
            var cleanupFailures = new List<Exception>();
            TryCleanup(UnregisterDebugControlPort, cleanupFailures);
            TryCleanup(UnregisterTickTargets, cleanupFailures);
            TryCleanup(DeactivateActorPorts, cleanupFailures);
            ReleaseRuntimeResources(cleanupFailures);
            for (int i = 0; i < cleanupFailures.Count; i++)
                Debug.LogException(cleanupFailures[i], this);
            Debug.LogError(failure.ToString(), this);
        }

        void ReleaseRuntimeResources(List<Exception> failures)
        {
            if (m_Preparation != null)
                TryCleanup(m_Preparation.Dispose, failures);
            m_Preparation = null;

            m_History.DetachRuntime();
            if (m_Runtime != null)
            {
                TryCleanup(m_Runtime.Dispose, failures);
                m_Runtime = null;
            }
            else
            {
                for (int i = m_Registrations.Count - 1; i >= 0; i--)
                    TryCleanup(m_Registrations[i].Dispose, failures);
            }
            m_OutputLifecycle = null;
        }

        void DisposeSession()
        {
            if (m_Disposed)
                return;
            var failures = new List<Exception>();
            TryCleanup(Quiesce, failures);
            ReleaseSessionResources(failures);
            CompleteSessionDisposal();
            for (int i = 0; i < failures.Count; i++)
                Debug.LogException(failures[i], this);
        }

        void ReleaseSessionResources(List<Exception> failures)
        {
            TryCleanup(UnregisterDebugControlPort, failures);
            ReleaseRuntimeResources(failures);
        }

        void RegisterDebugControlPort()
        {
            if (m_DebugControlPort != null || m_Runtime == null)
                return;
            m_DebugControlPort = new SimulationSessionHostDebugControlPort(this, m_Runtime.Descriptor);
            LocalSimulationDebugControlService.Register(m_DebugControlPort);
        }

        void UnregisterDebugControlPort()
        {
            if (m_DebugControlPort == null)
                return;
            LocalSimulationDebugControlService.Unregister(m_DebugControlPort);
            m_DebugControlPort = null;
        }

        void CompleteSessionDisposal()
        {
            UnregisterDebugControlPort();
            m_Disposed = true;
            m_Quiesced = true;
            m_Registrations.Clear();
            m_OutputLifecycle = null;
            m_LaunchPlan = null;
            m_State = SimulationSessionLifecycleState.Disposed;
        }

        void BeginFreshLifecycle()
        {
            if (m_Preparation != null || m_Runtime != null || m_Registrations.Count != 0 ||
                m_TickTargetsRegistered || m_DebugControlPort != null)
            {
                throw new InvalidOperationException("Disposed Simulation Session retained runtime state before reactivation.");
            }
            m_Disposed = false;
            m_Quiesced = false;
            m_State = SimulationSessionLifecycleState.Uninitialized;
            m_Failure = null;
            m_LastDiagnostics = null;
            m_LaunchPlan = null;
            m_OutputLifecycle = null;
            m_History.Reset();
            m_OuterTickKind = default;
            m_SessionGeneration = checked(m_SessionGeneration + 1);
        }

        static void TryCleanup(Action cleanup, List<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(SimulationSessionHost));
        }

        static long ToElapsedTicks(float fixedDeltaSeconds)
        {
            long ticks = checked((long)Math.Round(fixedDeltaSeconds * TimeSpan.TicksPerSecond));
            return Math.Max(1, ticks);
        }

        SimulationSessionDiagnosticsSnapshot BuildFailureDiagnostics(SimulationSessionFailure failure)
        {
            string sessionId;
            try
            {
                sessionId = m_Composition ? m_Composition.SessionId : string.Empty;
            }
            catch
            {
                sessionId = string.Empty;
            }
            if (string.IsNullOrWhiteSpace(sessionId))
                sessionId = $"uncomposed-session-host-{GetInstanceID()}";
            var components = new[]
            {
                new SimulationSessionComponentDiagnostic(
                    "Failure",
                    $"{failure.Stage}:{failure.Code}",
                    SimulationSessionComponentDiagnosticState.Failed,
                    $"{failure.Message} | Component={failure.ComponentIdentity} | Pass={failure.PassIdentity} | Product={failure.ProductIdentity}")
            };
            return new SimulationSessionDiagnosticsSnapshot(
                new SimulationSessionId(sessionId),
                SimulationSessionLifecycleState.Failed,
                SimulationSessionPreparationStatus.Failed,
                0,
                failure,
                components);
        }
    }
}
