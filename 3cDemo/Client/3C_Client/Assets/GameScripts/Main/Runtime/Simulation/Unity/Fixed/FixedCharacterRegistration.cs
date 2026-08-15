using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedCharacterBodySample = ThirdPersonSimulation.Fixed.CharacterBodySample;
using FixedCharacterSimulationProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedSimulationActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedSimulationActorTickResult = ThirdPersonSimulation.Fixed.SimulationActorTickResult;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedCharacterRegistration : IFixedLocalSimulationActorRegistration
    {
        readonly IUnityFixedCharacterControlSourceRuntime m_ControlSource;
        readonly FixedUnityPresentationOutputAdapter m_PresentationOutput;
        readonly ICharacterPresentationRuntime m_PresentationRuntime;
        readonly FixedCharacterSimulationDiagnosticsAdapter m_DiagnosticsAdapter;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;
        readonly FixedPresentationFrameTarget m_PresentationTarget;
        readonly SortedDictionary<ulong, FixedCharacterBodySample> m_PendingBodySamples =
            new SortedDictionary<ulong, FixedCharacterBodySample>();
        readonly SortedDictionary<ulong, EquipmentVisualSelection[]> m_PendingEquipmentSelections =
            new SortedDictionary<ulong, EquipmentVisualSelection[]>();
        readonly SortedDictionary<ulong, FixedSimulationActorTickResult> m_PendingTrajectoryResults =
            new SortedDictionary<ulong, FixedSimulationActorTickResult>();

        bool m_Activated;
        bool m_InputActivated;
        bool m_DiagnosticsRegistered;
        bool m_AnimationDiagnosticsRegistered;
        bool m_PresentationRegistered;
        bool m_ResultCommitActive;
        int m_MaximumBodySamples;
        ulong m_TrajectoryIntentSequence;
        bool m_Disposed;

        public FixedCharacterRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            FixedCharacterSimulationProgram program,
            CharacterPresentationSemanticContract presentationContract,
            string projectionRevision,
            string worldBodyBindingId,
            FixedWorldBodyState initialBody,
            IUnityFixedCharacterControlSourceRuntime controlSource,
            FixedUnityPresentationOutputAdapter presentationOutput,
            ICharacterPresentationRuntime presentationRuntime,
            RuntimeDiagnosticsContext diagnosticsContext,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            int maximumActivePresentationRecords)
        {
            if (ownerInstanceId == 0 || string.IsNullOrWhiteSpace(ownerName) || !actorId.IsValid)
                throw new ArgumentException("Fixed Actor registration owner identity is incomplete.");
            if (string.IsNullOrWhiteSpace(worldBodyBindingId) ||
                !string.Equals(worldBodyBindingId, worldBodyBindingId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Fixed Actor registration requires a stable world body binding.", nameof(worldBodyBindingId));
            }
            if (initialBody.ActorId != actorId)
                throw new ArgumentException("Fixed Actor registration body identity does not match ActorId.", nameof(initialBody));
            if (maximumActivePresentationRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumActivePresentationRecords));
            OwnerInstanceId = ownerInstanceId;
            OwnerName = ownerName.Trim();
            ActorId = actorId;
            Program = program ?? throw new ArgumentNullException(nameof(program));
            PresentationContract = presentationContract ?? throw new ArgumentNullException(nameof(presentationContract));
            WorldBodyBindingId = worldBodyBindingId.Trim();
            InitialBody = initialBody;
            m_ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
            if (!m_ControlSource.CharacterProgramId.Equals(Program.Manifest.ProgramId) ||
                !m_ControlSource.CharacterProgramHash.Equals(Program.ProgramHash))
            {
                throw new ArgumentException("Fixed Control Source does not match the Actor Program.", nameof(controlSource));
            }
            m_PresentationOutput = presentationOutput ?? throw new ArgumentNullException(nameof(presentationOutput));
            m_PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
            DiagnosticsContext = diagnosticsContext ?? throw new ArgumentNullException(nameof(diagnosticsContext));
            m_DiagnosticsAdapter = new FixedCharacterSimulationDiagnosticsAdapter(DiagnosticsContext, Program);
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
                throw new ArgumentException("Fixed Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
            m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
                diagnosticsTarget.CharacterRuntimeId,
                ownerInstanceId,
                ownerName,
                projectionRevision,
                animationSnapshotProvider);
            m_PresentationTarget = new FixedPresentationFrameTarget(presentationRuntime);
            ProgramIdentity = new FixedSimulationActorBinding(actorId, program, WorldBodyBindingId);
            OutputRoute = new SimulationOutputRouteDescriptor(
                $"fixed-character-output/{actorId.Value}",
                "fixed-character-output",
                1,
                actorId,
                StableHash.Compute(
                    actorId.Value,
                    program.Manifest.ProgramId.Value,
                    program.Manifest.SourceRevision.Value,
                    program.ProgramHash.ToString(),
                    program.LayoutHash.ToString(),
                    WorldBodyBindingId,
                    maximumActivePresentationRecords.ToString()));
        }

        public int OwnerInstanceId { get; }
        public string OwnerName { get; }
        public string OwnerIdentity => $"unity-fixed-character/{OwnerInstanceId}";
        public ActorId ActorId { get; }
        public FixedCharacterSimulationProgram Program { get; }
        public FixedSimulationActorBinding ProgramIdentity { get; }
        public CharacterPresentationSemanticContract PresentationContract { get; }
        public string WorldBodyBindingId { get; }
        public FixedWorldBodyState InitialBody { get; }
        public RuntimeDiagnosticsContext DiagnosticsContext { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
        public IFixedCharacterControlSourceRuntime FixedControlSource => m_ControlSource;
        public IFixedPresentationCommitOutputPort PresentationOutput => m_PresentationOutput;
        public ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink SimulationDiagnostics => m_DiagnosticsAdapter;
        StableHash ISimulationActorRegistration.DiagnosticsConfigurationHash => StableHash.Compute(
            Program.Manifest.ProgramId.Value,
            Program.Manifest.SourceRevision.Value,
            Program.ProgramHash.ToString(),
            Program.LayoutHash.ToString(),
            DiagnosticsContext.Revision.ToString());

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            try
            {
                m_ControlSource.Activate();
                m_InputActivated = true;
                RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
                m_DiagnosticsRegistered = true;
                AnimationPresentationRuntimeTargetRegistry.Register(m_AnimationDiagnosticsTarget);
                m_AnimationDiagnosticsRegistered = true;
                if (!GameplayTickSystem.RegisterPresentationTarget(m_PresentationTarget))
                    throw new InvalidOperationException("GameplayTickSystem rejected the Fixed Actor Presentation target.");
                m_PresentationRegistered = true;
                m_Activated = true;
            }
            catch (Exception exception)
            {
                var failures = new List<Exception> { exception };
                ReleaseActivation(failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(failures);
            }
        }

        public void Deactivate()
        {
            if (!m_Activated && !m_InputActivated && !m_DiagnosticsRegistered &&
                !m_AnimationDiagnosticsRegistered && !m_PresentationRegistered)
                return;
            var failures = new List<Exception>();
            ReleaseActivation(failures);
            if (failures.Count != 0)
                throw new AggregateException($"Fixed Actor '{ActorId}' activation resources failed to release.", failures);
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Activated)
                throw new InvalidOperationException($"Fixed Actor '{ActorId}' registration is not active.");
            m_ControlSource.CaptureRenderFrame(renderFrame);
        }

        public void BeginLogicTick()
        {
            RequireAlive();
        }

        public void BeginResultCommit(int maximumBodySamples)
        {
            RequireAlive();
            if (m_ResultCommitActive)
                throw new InvalidOperationException($"Fixed Actor '{ActorId}' Body result commit is already active.");
            if (maximumBodySamples <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumBodySamples));
            m_PendingBodySamples.Clear();
            m_PendingEquipmentSelections.Clear();
            m_PendingTrajectoryResults.Clear();
            m_MaximumBodySamples = maximumBodySamples;
            m_ResultCommitActive = true;
        }

        public void ObservePublished(FixedSimulationActorTickResult result)
        {
            RequireAlive();
            if (!m_ResultCommitActive)
                throw new InvalidOperationException($"Fixed Actor '{ActorId}' Body result mutation requires an active commit.");
            if (result == null || result.ActorId != ActorId)
                throw new ArgumentException("Fixed published result targets another Actor.", nameof(result));
            FixedCharacterBodySample sample = result.BodySample;
            m_PendingBodySamples[sample.Tick.Value] = sample;
            if (m_PresentationRuntime.AcceptsTrajectoryIntent)
                m_PendingTrajectoryResults[sample.Tick.Value] = result;
            if (result.State.TryGetEquipmentState(out EquipmentStateAggregate equipment))
            {
                var selections = new EquipmentVisualSelection[equipment.Slots.Count];
                for (int i = 0; i < selections.Length; i++)
                    selections[i] = equipment.Slots[i].CreateVisualSelection(ActorId, result.Tick.Value);
                m_PendingEquipmentSelections[result.Tick.Value] = selections;
            }
            if (m_PendingBodySamples.Count > m_MaximumBodySamples)
            {
                throw new InvalidOperationException(
                    $"Fixed Actor '{ActorId}' Body transaction exceeds capacity '{m_MaximumBodySamples}'.");
            }
        }

        public void CompleteResultCommit()
        {
            RequireAlive();
            RequireResultCommit();
            try
            {
                if (m_PendingBodySamples.Count == 0)
                    return;
                var intervals = new List<CharacterPresentationBodyInterval>(m_PendingBodySamples.Count);
                foreach (FixedCharacterBodySample sample in m_PendingBodySamples.Values)
                {
                    float yawVelocityDegreesPerSecond =
                        sample.AppliedYawDegrees.ToSingle() * Program.Manifest.TickRate;
                    intervals.Add(new CharacterPresentationBodyInterval(
                        sample.Tick.Value - 1,
                        FixedUnityPresentationBoundary.Convert(sample.BeforeBody),
                        sample.Tick.Value,
                        FixedUnityPresentationBoundary.Convert(sample.FinalBody),
                        yawVelocityDegreesPerSecond));
                }
                m_PresentationRuntime.CaptureBodyTransaction(intervals);
                foreach (FixedSimulationActorTickResult result in m_PendingTrajectoryResults.Values)
                {
                    m_PresentationRuntime.CaptureTrajectoryIntent(
                        CreateTrajectoryIntent(
                            result,
                            checked(++m_TrajectoryIntentSequence),
                            m_PresentationRuntime.BodyResetSequence));
                }
                foreach (EquipmentVisualSelection[] selections in m_PendingEquipmentSelections.Values)
                    m_PresentationRuntime.CaptureEquipmentSelections(selections);
            }
            finally
            {
                m_PendingBodySamples.Clear();
                m_PendingEquipmentSelections.Clear();
                m_PendingTrajectoryResults.Clear();
                m_MaximumBodySamples = 0;
                m_ResultCommitActive = false;
            }
        }

        public void AbortResultCommit()
        {
            m_PendingBodySamples.Clear();
            m_PendingEquipmentSelections.Clear();
            m_PendingTrajectoryResults.Clear();
            m_MaximumBodySamples = 0;
            m_ResultCommitActive = false;
        }

        static CharacterPresentationTrajectoryIntent CreateTrajectoryIntent(
            FixedSimulationActorTickResult result,
            ulong sourceSequence,
            ulong resetSequence)
        {
            FixedVector3 velocity = result.Motion.RequestedVelocity;
            FixedVector2 basis = result.Motion.LocomotionPlanarBasis;
            var desiredVelocity = new UnityEngine.Vector2(
                velocity.X.ToSingle(),
                velocity.Z.ToSingle());
            return new CharacterPresentationTrajectoryIntent(
                result.ActorId,
                result.Tick.Value > 1 ? new SimulationTick(result.Tick.Value - 1) : default,
                result.Tick,
                sourceSequence,
                new UnityEngine.Vector2(basis.X.ToSingle(), basis.Y.ToSingle()),
                desiredVelocity,
                CharacterPresentationTrajectoryIntent.ResolveDesiredFacing(
                    desiredVelocity,
                    result.BodySample.FinalBody.Yaw.Degrees.ToSingle()),
                float.MaxValue,
                float.MaxValue,
                CharacterPresentationTrajectoryIntent.HasPlanarMotion(desiredVelocity),
                result.BodySample.FinalBody.Grounded,
                CharacterPresentationTrajectoryIntent.ResolveMovementModeId(
                    result.Motion.MovementPlaybackClock.OwnerIdentity,
                    result.Motion.ActionOwnerIdentity,
                    result.Motion.GameplayResultOwnerIdentity),
                result.Motion.MovementPlaybackClock,
                result.Motion.LocomotionTimeline,
                resetSequence);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            AbortResultCommit();
            var failures = new List<Exception>();
            TryRelease(Deactivate, failures);
            TryRelease(m_DiagnosticsTarget.Terminate, failures);
            TryRelease(m_DiagnosticsTarget.Dispose, failures);
            TryRelease(m_PresentationRuntime.Dispose, failures);
            TryRelease(m_ControlSource.Dispose, failures);
            if (failures.Count != 0)
                throw new AggregateException($"Fixed Actor '{ActorId}' failed to dispose completely.", failures);
        }

        void ReleaseActivation(List<Exception> failures)
        {
            if (m_PresentationRegistered)
            {
                TryRelease(() => GameplayTickSystem.UnregisterPresentationTarget(m_PresentationTarget), failures);
                m_PresentationRegistered = false;
            }
            if (m_AnimationDiagnosticsRegistered)
            {
                TryRelease(() => AnimationPresentationRuntimeTargetRegistry.Unregister(m_AnimationDiagnosticsTarget), failures);
                m_AnimationDiagnosticsRegistered = false;
            }
            if (m_DiagnosticsRegistered)
            {
                TryRelease(() => RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget), failures);
                m_DiagnosticsRegistered = false;
            }
            if (m_InputActivated)
            {
                TryRelease(m_ControlSource.Deactivate, failures);
                m_InputActivated = false;
            }
            m_Activated = false;
        }

        static void TryRelease(Action release, List<Exception> failures)
        {
            try
            {
                release();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedCharacterRegistration));
        }

        void RequireResultCommit()
        {
            if (!m_ResultCommitActive)
                throw new InvalidOperationException($"Fixed Actor '{ActorId}' Body result commit is not active.");
        }
    }

    sealed class FixedPresentationFrameTarget : IGameplayPresentationFrameTarget
    {
        readonly ICharacterPresentationRuntime m_Runtime;

        public FixedPresentationFrameTarget(ICharacterPresentationRuntime runtime)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            m_Runtime.Present(context);
        }
    }
}
