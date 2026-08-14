using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class SimulationSessionHost : MonoBehaviour, IGameplayRenderFrameInputTarget, IGameplayLogicTickTarget,
        ICharacterFutureBodyTrajectorySource
    {
        static readonly ProfilerMarker InputMarker = new ProfilerMarker("ThirdPerson.Session.Input");
        static readonly ProfilerMarker LogicMarker = new ProfilerMarker("ThirdPerson.Session.LogicTick");
        static readonly ISimulationPerformanceSink Performance = UnitySimulationPerformanceSink.Instance;

        [SerializeField] SimulationSessionCompositionDefinition m_Composition;

        readonly List<ISimulationActorRegistration> m_Registrations =
            new List<ISimulationActorRegistration>();
        SimulationSessionCompositionPreparation m_Preparation;
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

        public SimulationSessionCompositionDefinition Composition => m_Composition;
        public SimulationSessionLifecycleState LifecycleState => m_State;
        public SimulationSessionFailure Failure => m_Failure;
        public SimulationSessionLaunchPlan LaunchPlan => m_LaunchPlan;
        public SimulationSessionDiagnosticsSnapshot Diagnostics =>
            m_Runtime?.Diagnostics ?? m_Preparation?.Diagnostics ?? m_LastDiagnostics;
        public int RegistrationCount => m_Registrations.Count;
        public bool IsQuiesced => m_Quiesced;

        public bool TryPredict(
            in CharacterFutureBodyTrajectoryRequest request,
            out CharacterFutureBodyTrajectory trajectory)
        {
            if (m_Disposed || m_Quiesced || m_State != SimulationSessionLifecycleState.Active ||
                m_Runtime is not ICharacterFutureBodyTrajectorySource source)
            {
                trajectory = null;
                return false;
            }
            return source.TryPredict(in request, out trajectory);
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

        public void Stop()
        {
            DisposeSession();
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

        public void BeginRenderFrame(ulong renderFrame)
        {
            if (m_Disposed || m_Quiesced || m_State != SimulationSessionLifecycleState.Active)
                return;
            try
            {
                using (InputMarker.Auto())
                {
                    for (int i = 0; i < m_Registrations.Count; i++)
                        m_Registrations[i].CaptureRenderFrame(renderFrame);
                }
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
                    BeginPreparation();
                    StepPreparation(context);
                    return;
                }
                if (m_State == SimulationSessionLifecycleState.Preparing)
                {
                    StepPreparation(context);
                    return;
                }
                using (LogicMarker.Auto())
                {
                    m_OutputLifecycle.BeginLogicTick();
                    m_Runtime.LogicTick(BuildRuntimeContext(context, m_LaunchPlan.Descriptor.SourceClockId));
                }
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

        void Awake()
        {
            if (!m_Composition)
                Debug.LogError("SimulationSessionHost requires an explicit Session Composition Definition.", this);
        }

        void OnEnable()
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
                m_Composition.RequireComplete();
                if (GameplayTickSystem.Current.Settings.LocalLogicTickRate != m_Composition.TickRate)
                    throw new InvalidOperationException("Session Composition TickRate does not match GameplayTickSystem LocalLogic TickRate.");
                m_Preparation = m_Composition.CreatePreparation(m_Registrations);
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
            SimulationSessionLogicTickContext preparationContext = new SimulationSessionLogicTickContext(
                new SimulationTickSourceIdentity(
                    m_Preparation.SourceDescriptor.OuterTickKind,
                    m_Composition.SourceClockId,
                    context.LocalLogicTick),
                new WorldRevision(m_Composition.WorldRevision),
                ToElapsedTicks(context.FixedDeltaSeconds));
            SimulationSessionPreparationStatus status = m_Preparation.Step(preparationContext);
            if (status == SimulationSessionPreparationStatus.Pending)
                return;
            if (status == SimulationSessionPreparationStatus.Failed)
            {
                Fail(m_Preparation.Failure ?? new SimulationSessionFailure(
                    SimulationSessionFailureStage.Preparation,
                    "session_preparation_failed",
                    "Session preparation failed without a structured failure.",
                    m_Composition.name));
                return;
            }
            SimulationSessionPreparedRuntime prepared = m_Preparation.TakePreparedRuntime();
            m_LaunchPlan = prepared.LaunchPlan;
            m_Runtime = prepared.RuntimeHandle;
            m_OutputLifecycle = prepared.OutputLifecycle;
            m_OuterTickKind = prepared.OuterTickKind;
            m_Preparation.Dispose();
            m_Preparation = null;
            ActivateActorPorts();
            CaptureActorInputs(context.RenderFrame);
            m_State = SimulationSessionLifecycleState.Active;
            RegisterDebugControlPort();
        }

        SimulationSessionLogicTickContext BuildRuntimeContext(
            GameplayLogicTickContext context,
            SimulationSourceClockId sourceClock)
        {
            if (m_LaunchPlan == null || !Enum.IsDefined(typeof(SimulationTickSourceKind), m_OuterTickKind))
                throw new InvalidOperationException("Active Session has no formal outer Tick mapping.");
            return new SimulationSessionLogicTickContext(
                new SimulationTickSourceIdentity(m_OuterTickKind, sourceClock.Value, context.LocalLogicTick),
                new WorldRevision(m_Composition.WorldRevision),
                ToElapsedTicks(context.FixedDeltaSeconds),
                Performance);
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
            ReleaseFailedResources(cleanupFailures);
            for (int i = 0; i < cleanupFailures.Count; i++)
                Debug.LogException(cleanupFailures[i], this);
            Debug.LogError(failure.ToString(), this);
        }

        void ReleaseFailedResources(List<Exception> failures)
        {
            if (m_Preparation != null)
                TryCleanup(m_Preparation.Dispose, failures);
            m_Preparation = null;

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
            if (m_Preparation != null)
                TryCleanup(m_Preparation.Dispose, failures);
            m_Preparation = null;
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
            m_OuterTickKind = default;
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
