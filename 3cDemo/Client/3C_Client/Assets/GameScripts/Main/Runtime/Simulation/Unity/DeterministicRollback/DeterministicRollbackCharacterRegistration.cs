using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using FixedCharacterBodySample = ThirdPersonSimulation.Fixed.CharacterBodySample;
using FixedCharacterRuntime = ThirdPersonSimulation.Fixed.FixedCharacterRuntime;
using FixedSimulationActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedSimulationActorTickResult = ThirdPersonSimulation.Fixed.SimulationActorTickResult;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public sealed class DeterministicRollbackCharacterRegistration :
        IDeterministicRollbackSimulationActorRegistration,
        ISimulationPresentationCheckpointRuntime
    {
        readonly UnityFixedCharacterInputAdapter m_LocalInput;
        readonly FixedUnityPresentationOutputAdapter m_PresentationOutput;
        readonly ICharacterPresentationRuntime m_PresentationRuntime;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly FixedCharacterSimulationDiagnosticsAdapter m_DiagnosticsAdapter;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;
        readonly CharacterPresentationFrameTarget m_PresentationTarget;
        readonly FixedCharacterRuntime m_CharacterRuntime;
        readonly FixedSimulationActorBinding m_CharacterBinding;
        readonly SortedDictionary<ulong, FixedCharacterBodySample> m_PendingBodySamples =
            new SortedDictionary<ulong, FixedCharacterBodySample>();
        readonly SortedDictionary<ulong, FixedSimulationActorTickResult> m_PendingTrajectoryResults =
            new SortedDictionary<ulong, FixedSimulationActorTickResult>();

        RollbackRuntimeState m_RuntimeState;
        RollbackOutputCommitter m_OutputCommitter;
        IRollbackNetworkDiagnosticsSource m_NetworkDiagnostics;

        bool m_Activated;
        bool m_InputActivated;
        bool m_DiagnosticsRegistered;
        bool m_AnimationDiagnosticsRegistered;
        bool m_PresentationRegistered;
        bool m_ResultCommitActive;
        int m_MaximumBodySamples;
        ulong m_TrajectoryIntentSequence;
        bool m_Disposed;

        public DeterministicRollbackCharacterRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            FixedCharacterRuntime characterRuntime,
            FixedSimulationActorBinding characterBinding,
            AnimationPresentationProgramIdentity presentationProgramIdentity,
            string worldBodyBindingId,
            FixedWorldBodyState initialBody,
            UnityFixedCharacterInputAdapter localInput,
            FixedUnityPresentationOutputAdapter presentationOutput,
            ICharacterPresentationRuntime presentationRuntime,
            CharacterRootHierarchyBinding rootHierarchy,
            RuntimeDiagnosticsContext diagnosticsContext,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            int maximumActivePresentationRecords)
        {
            if (ownerInstanceId == 0 || string.IsNullOrWhiteSpace(ownerName) || !actorId.IsValid)
                throw new ArgumentException("Rollback Actor registration owner identity is incomplete.");
            if (string.IsNullOrWhiteSpace(worldBodyBindingId) ||
                !string.Equals(worldBodyBindingId, worldBodyBindingId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Rollback Actor registration requires a stable world body binding.", nameof(worldBodyBindingId));
            }
            if (initialBody.ActorId != actorId)
                throw new ArgumentException("Rollback Actor registration body identity does not match ActorId.", nameof(initialBody));
            if (maximumActivePresentationRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumActivePresentationRecords));

            OwnerInstanceId = ownerInstanceId;
            OwnerName = ownerName.Trim();
            ActorId = actorId;
            m_CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            m_CharacterBinding = characterBinding ?? throw new ArgumentNullException(nameof(characterBinding));
            if (!ReferenceEquals(
                    m_CharacterRuntime.Roster[m_CharacterRuntime.GetActorIndex(actorId)],
                    m_CharacterBinding))
            {
                throw new ArgumentException("Rollback Actor registration binding does not belong to the Character Runtime.", nameof(characterBinding));
            }
            WorldBodyBindingId = worldBodyBindingId.Trim();
            if (!string.Equals(m_CharacterBinding.WorldBodyBindingId, WorldBodyBindingId, StringComparison.Ordinal))
                throw new ArgumentException("Rollback Actor registration world binding does not match the Character Runtime binding.", nameof(characterBinding));
            InitialBody = initialBody;
            m_LocalInput = localInput;
            m_PresentationOutput = presentationOutput ?? throw new ArgumentNullException(nameof(presentationOutput));
            m_PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            DiagnosticsContext = diagnosticsContext ?? throw new ArgumentNullException(nameof(diagnosticsContext));
            m_DiagnosticsAdapter = ThirdPersonSimulation.Fixed.NullSimulationDiagnosticsSink.Instance;
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
                throw new ArgumentException("Rollback Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
            m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
                diagnosticsTarget.CharacterRuntimeId,
                actorId,
                ownerInstanceId,
                ownerName,
                presentationProgramIdentity,
                animationSnapshotProvider);
            m_PresentationTarget =
                new CharacterPresentationFrameTarget(presentationRuntime);
            OutputRoute = new SimulationOutputRouteDescriptor(
                $"deterministic-rollback-output/{actorId.Value}",
                "deterministic-rollback-fixed-output",
                1,
                actorId,
                StableHash.Compute(
                    actorId.Value,
                    m_CharacterRuntime.GameplayContentHash.ToString(),
                    m_CharacterBinding.GameplayContentHash.ToString(),
                    WorldBodyBindingId,
                    maximumActivePresentationRecords.ToString()));
        }

        public int OwnerInstanceId { get; }
        public string OwnerName { get; }
        public string OwnerIdentity => $"unity-deterministic-rollback-character/{OwnerInstanceId}";
        public ActorId ActorId { get; }
        public FixedCharacterRuntime CharacterRuntime => m_CharacterRuntime;
        public FixedSimulationActorBinding CharacterBinding => m_CharacterBinding;
        public string WorldBodyBindingId { get; }
        public FixedWorldBodyState InitialBody { get; }
        public RuntimeDiagnosticsContext DiagnosticsContext { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
        public IFixedCharacterControlSourceRuntime RollbackInput => m_LocalInput;
        public IFixedPresentationCommitOutputPort PresentationOutput => m_PresentationOutput;
        public ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink SimulationDiagnostics => m_DiagnosticsAdapter;
        public bool SupportsPresentationCheckpointCapture =>
            m_PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
            checkpoint.SupportsCheckpointCapture;
        public bool SupportsPresentationCheckpointRestore =>
            m_PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
            checkpoint.SupportsCheckpointCapture &&
            checkpoint.SupportsCheckpointRestore;
        public bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime is ICharacterPresentationCheckpointRuntime runtime)
                return runtime.TryCaptureCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint capture.";
            return false;
        }
        public bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime is ICharacterPresentationCheckpointRuntime restore)
                return restore.TryRestoreCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint restore.";
            return false;
        }
        StableHash ISimulationActorRegistration.DiagnosticsConfigurationHash => StableHash.Compute(
            m_CharacterRuntime.GameplayContentHash.ToString(),
            m_CharacterBinding.GameplayContentHash.ToString(),
            DiagnosticsContext.Revision.ToString());

        public bool TryGetRuntimeDiagnostics(out RollbackRuntimeDiagnosticsSnapshot snapshot)
        {
            if (m_Disposed || m_RuntimeState == null || m_OutputCommitter == null || m_NetworkDiagnostics == null)
            {
                snapshot = default;
                return false;
            }
            CharacterPresentationRuntimeDiagnosticsSnapshot presentation = m_PresentationRuntime.CaptureDiagnostics();
            snapshot = m_RuntimeState.CaptureDiagnostics(
                m_OutputCommitter.CaptureLifecycleSnapshot(),
                new RollbackPresentationDiagnosticsSnapshot(
                    presentation.BodyBranchReplacementCount,
                    presentation.AnimationBranchReplacementCount,
                    presentation.FollowerPositionCorrectionMeters,
                    presentation.FollowerYawCorrectionDegrees),
                m_NetworkDiagnostics.CaptureNetworkDiagnostics());
            return true;
        }

        public void BindRuntimeDiagnostics(
            RollbackRuntimeState state,
            RollbackOutputCommitter outputCommitter,
            IRollbackNetworkDiagnosticsSource networkDiagnostics)
        {
            RequireAlive();
            if (m_RuntimeState != null || m_OutputCommitter != null || m_NetworkDiagnostics != null)
                throw new InvalidOperationException($"Rollback Actor '{ActorId}' runtime diagnostics are already bound.");
            m_RuntimeState = state ?? throw new ArgumentNullException(nameof(state));
            m_OutputCommitter = outputCommitter ?? throw new ArgumentNullException(nameof(outputCommitter));
            m_NetworkDiagnostics = networkDiagnostics ?? throw new ArgumentNullException(nameof(networkDiagnostics));
        }

        public void PublishCheckpoint(ulong tick, string snapshotIdentity, StableHash snapshotHash) =>
            m_DiagnosticsAdapter.PublishCheckpoint(tick, snapshotIdentity, snapshotHash);

        public void BindExecutionBranch(Guid executionBranchId) =>
            DiagnosticsContext.SetExecutionBranch(executionBranchId);

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            try
            {
                if (m_LocalInput != null)
                {
                    m_LocalInput.Activate();
                    m_InputActivated = true;
                }
                RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
                m_DiagnosticsRegistered = true;
                AnimationPresentationRuntimeTargetRegistry.Register(m_AnimationDiagnosticsTarget);
                m_AnimationDiagnosticsRegistered = true;
                m_PresentationTarget.Activate();
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
                throw new AggregateException($"Rollback Actor '{ActorId}' activation resources failed to release.", failures);
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Activated)
                throw new InvalidOperationException($"Rollback Actor '{ActorId}' registration is not active.");
            m_LocalInput?.CaptureRenderFrame(renderFrame);
        }

        public void BeginLogicTick()
        {
            RequireAlive();
        }

        public void BeginResultCommit(int maximumBodySamples)
        {
            RequireAlive();
            if (m_ResultCommitActive)
                throw new InvalidOperationException($"Rollback Actor '{ActorId}' Body result commit is already active.");
            if (maximumBodySamples <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumBodySamples));
            m_PendingBodySamples.Clear();
            m_PendingTrajectoryResults.Clear();
            m_MaximumBodySamples = maximumBodySamples;
            m_ResultCommitActive = true;
        }

        public void ObservePublished(FixedSimulationActorTickResult result)
        {
            RequireAlive();
            if (!m_ResultCommitActive)
                throw new InvalidOperationException($"Rollback Actor '{ActorId}' Body result mutation requires an active commit.");
            if (result == null || result.ActorId != ActorId)
                throw new ArgumentException("Rollback published result targets another Actor.", nameof(result));
            FixedCharacterBodySample sample = result.BodySample;
            m_PendingBodySamples[sample.Tick.Value] = sample;
            if (m_PresentationRuntime.AcceptsTrajectoryIntent)
                m_PendingTrajectoryResults[sample.Tick.Value] = result;
            if (m_PendingBodySamples.Count > m_MaximumBodySamples)
            {
                throw new InvalidOperationException(
                    $"Rollback Actor '{ActorId}' Body transaction exceeds rollback history capacity '{m_MaximumBodySamples}'.");
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
                FixedCharacterBodySample finalSample = default;
                foreach (FixedCharacterBodySample sample in m_PendingBodySamples.Values)
                {
                    finalSample = sample;
                    float yawVelocityDegreesPerSecond =
                        sample.AppliedYawDegrees.ToSingle() * m_CharacterRuntime.TickRate;
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
                CharacterPresentationBodyState finalBody = FixedUnityPresentationBoundary.Convert(finalSample.FinalBody);
                m_RootHierarchy.ApplyLogicPose(finalBody.Position, finalBody.Rotation);
            }
            finally
            {
                m_PendingBodySamples.Clear();
                m_PendingTrajectoryResults.Clear();
                m_MaximumBodySamples = 0;
                m_ResultCommitActive = false;
            }
        }

        public void AbortResultCommit()
        {
            m_PendingBodySamples.Clear();
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
            if (m_LocalInput != null)
                TryRelease(m_LocalInput.Dispose, failures);
            if (failures.Count != 0)
                throw new AggregateException($"Rollback Actor '{ActorId}' failed to dispose completely.", failures);
        }

        void ReleaseActivation(List<Exception> failures)
        {
            if (m_PresentationRegistered)
            {
                TryRelease(m_PresentationTarget.Deactivate, failures);
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
                TryRelease(m_LocalInput.Deactivate, failures);
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
                throw new ObjectDisposedException(nameof(DeterministicRollbackCharacterRegistration));
        }

        void RequireResultCommit()
        {
            if (!m_ResultCommitActive)
                throw new InvalidOperationException($"Rollback Actor '{ActorId}' Body result commit is not active.");
        }
    }

}
