using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedCharacterBodySample = ThirdPersonSimulation.Fixed.CharacterBodySample;
using FixedSimulationActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedSimulationActorTickResult = ThirdPersonSimulation.Fixed.SimulationActorTickResult;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedCharacterRegistration :
        IFixedLocalSimulationActorRegistration,
        ISimulationActorStartGate,
        ISimulationPresentationCheckpointRuntime
    {
        readonly IUnityFixedCharacterControlSourceRuntime m_ControlSource;
        readonly FixedCharacterRuntime m_CharacterRuntime;
        readonly FixedSimulationActorBinding m_ActorBinding;
        readonly FixedUnityPresentationOutputAdapter m_PresentationOutput;
        ICharacterPresentationDomainRuntime m_PresentationRuntime;
        readonly CharacterDomainRuntimeAssemblyFacts m_DomainFacts;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink m_DiagnosticsAdapter;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly int m_MaximumActivePresentationRecords;
        readonly SortedDictionary<ulong, FixedCharacterBodySample> m_PendingBodySamples =
            new SortedDictionary<ulong, FixedCharacterBodySample>();
        readonly SortedDictionary<ulong, EquipmentVisualSelection[]> m_PendingEquipmentSelections =
            new SortedDictionary<ulong, EquipmentVisualSelection[]>();
        readonly SortedDictionary<ulong, FixedSimulationActorTickResult> m_PendingTrajectoryResults =
            new SortedDictionary<ulong, FixedSimulationActorTickResult>();
        readonly CharacterPresentationBodyInterval[] m_BodyIntervalScratch;

        bool m_Activated;
        bool m_InputActivated;
        bool m_DiagnosticsRegistered;
        bool m_PresentationRegistered;
        bool m_ResultCommitActive;
        int m_MaximumBodySamples;
        ulong m_TrajectoryIntentSequence;
        bool m_Disposed;
        public FixedCharacterRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            FixedCharacterRuntime characterRuntime,
            FixedSimulationActorBinding actorBinding,
            string worldBodyBindingId,
            FixedWorldBodyState initialBody,
            IUnityFixedCharacterControlSourceRuntime controlSource,
            FixedUnityPresentationOutputAdapter presentationOutput,
            ICharacterPresentationDomainRuntime presentationRuntime,
            CharacterDomainRuntimeAssemblyFacts domainFacts,
            CharacterRootHierarchyBinding rootHierarchy,
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
            m_CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            m_ActorBinding = actorBinding ?? throw new ArgumentNullException(nameof(actorBinding));
            if (!ReferenceEquals(
                    m_CharacterRuntime.Roster[m_CharacterRuntime.GetActorIndex(actorId)],
                    m_ActorBinding))
                throw new ArgumentException("Fixed Actor registration binding does not belong to the Character Runtime.", nameof(actorBinding));
            if (!string.Equals(m_ActorBinding.WorldBodyBindingId, worldBodyBindingId.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Fixed Actor registration world binding does not match the Character Runtime binding.", nameof(actorBinding));
            WorldBodyBindingId = worldBodyBindingId.Trim();
            InitialBody = initialBody;
            m_MaximumActivePresentationRecords = maximumActivePresentationRecords;
            m_BodyIntervalScratch = new CharacterPresentationBodyInterval[maximumActivePresentationRecords];
            m_ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
            m_PresentationOutput = presentationOutput ?? throw new ArgumentNullException(nameof(presentationOutput));
            m_PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
            m_DomainFacts = domainFacts ?? throw new ArgumentNullException(nameof(domainFacts));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            DiagnosticsContext = diagnosticsContext ?? throw new ArgumentNullException(nameof(diagnosticsContext));
            m_DiagnosticsAdapter = ThirdPersonSimulation.Fixed.NullSimulationDiagnosticsSink.Instance;
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            OutputRoute = new SimulationOutputRouteDescriptor(
                $"fixed-character-output/{actorId.Value}",
                "fixed-character-output",
                1,
                actorId,
                StableHash.Compute(
                    actorId.Value,
                    m_CharacterRuntime.GameplayContentHash.ToString(),
                    m_ActorBinding.GameplayContentHash.ToString(),
                    WorldBodyBindingId,
                    maximumActivePresentationRecords.ToString()));
        }

        public int OwnerInstanceId { get; }
        public string OwnerName { get; }
        public string OwnerIdentity => $"unity-fixed-character/{OwnerInstanceId}";
        public ActorId ActorId { get; }
        public FixedCharacterRuntime CharacterRuntime => m_CharacterRuntime;
        public FixedSimulationActorBinding CharacterBinding => m_ActorBinding;
        public string WorldBodyBindingId { get; }
        public FixedWorldBodyState InitialBody { get; }
        public RuntimeDiagnosticsContext DiagnosticsContext { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; private set; }
        public IFixedCharacterControlSourceRuntime FixedControlSource => m_ControlSource;
        public ICharacterPresentationDomainRuntime PresentationRuntime => m_PresentationRuntime;
        public CharacterDomainRuntimeAssemblyFacts DomainFacts => m_DomainFacts;
        public IFixedPresentationCommitOutputPort PresentationOutput => m_PresentationOutput;
        public ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink SimulationDiagnostics => m_DiagnosticsAdapter;
        public bool SupportsPresentationCheckpointCapture =>
            m_PresentationRuntime.SupportsCheckpointCapture;
        public bool SupportsPresentationCheckpointRestore =>
            m_PresentationRuntime.SupportsCheckpointRestore;
        public bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime.SupportsCheckpointCapture)
                return m_PresentationRuntime.TryCaptureCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint capture.";
            return false;
        }
        public bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime.SupportsCheckpointRestore)
                return m_PresentationRuntime.TryRestoreCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint restore.";
            return false;
        }

		public void PublishCheckpoint(ulong tick, string snapshotIdentity, StableHash snapshotHash)
		{
		}

		public void BindExecutionBranch(Guid executionBranchId) =>
			DiagnosticsContext.SetExecutionBranch(executionBranchId);
        public bool IsSimulationStartReady =>
            FixedCharacterInputTraceModule.CanAdvanceSimulation(ActorId);
        public string SimulationStartWaitReason =>
            IsSimulationStartReady
                ? string.Empty
                : "Canonical Fixed input trace start state is not released.";
        StableHash ISimulationActorRegistration.DiagnosticsConfigurationHash => StableHash.Compute(
            m_CharacterRuntime.GameplayContentHash.ToString(),
            m_ActorBinding.GameplayContentHash.ToString(),
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
                if (!GameplayTickSystem.RegisterPresentationTarget(m_PresentationRuntime))
                    throw new InvalidOperationException("Gameplay Tick System is not initialized.");
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
                !m_PresentationRegistered)
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
            FixedCharacterInputTraceModule.ObservePublishedBody(
                ActorId,
                result.Tick,
                sample.FinalBody);
            FixedCharacterInputTraceMode traceMode = FixedCharacterInputTraceModule.Status.Mode;
            if (traceMode == FixedCharacterInputTraceMode.ReplayPaused ||
                traceMode == FixedCharacterInputTraceMode.Completed)
            {
                GameplayTickSystem.RequestCurrentFrameLogicStop();
            }
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
                int intervalCount = 0;
                FixedCharacterBodySample finalSample = default;
                foreach (FixedCharacterBodySample sample in m_PendingBodySamples.Values)
                {
                    finalSample = sample;
                    float yawVelocityDegreesPerSecond =
							 sample.AppliedYawDegrees.ToSingle() * m_CharacterRuntime.TickRate;
                    m_BodyIntervalScratch[intervalCount++] = new CharacterPresentationBodyInterval(
                        sample.Tick.Value - 1,
                        FixedUnityPresentationBoundary.Convert(sample.BeforeBody),
                        sample.Tick.Value,
                        FixedUnityPresentationBoundary.Convert(sample.FinalBody),
                        yawVelocityDegreesPerSecond);
                }
                m_PresentationRuntime.CaptureBodyStream(m_BodyIntervalScratch, intervalCount);
                foreach (FixedSimulationActorTickResult result in m_PendingTrajectoryResults.Values)
                {
                    LocomotionPresentationFailureCode failureCode =
                        m_PresentationRuntime.CaptureTrajectoryIntent(
                        CreateTrajectoryIntent(
                            result,
                            checked(++m_TrajectoryIntentSequence),
                            m_PresentationRuntime.BodyResetSequence));
                    if (failureCode != LocomotionPresentationFailureCode.None)
                    {
                        throw new InvalidOperationException(
                            $"Fixed Actor '{ActorId}' locomotion presentation rejected Fact: {failureCode}.");
                    }
                }
                foreach (EquipmentVisualSelection[] selections in m_PendingEquipmentSelections.Values)
                    m_PresentationRuntime.CaptureEquipmentSelections(selections);
                CharacterPresentationBodyState finalBody = FixedUnityPresentationBoundary.Convert(finalSample.FinalBody);
                m_RootHierarchy.ApplyLogicPose(finalBody.Position, finalBody.Rotation);
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

        CharacterPresentationTrajectoryIntent CreateTrajectoryIntent(
            FixedSimulationActorTickResult result,
            ulong sourceSequence,
            ulong resetSequence)
        {
            FixedVector3 velocity = result.Motion.RequestedVelocity;
            FixedVector2 basis = result.Motion.LocomotionPlanarBasis;
            CommittedMovementPlaybackClock movementClock =
                result.Motion.MovementPlaybackClock;
            var desiredVelocity = new UnityEngine.Vector2(
                velocity.X.ToSingle(),
                velocity.Z.ToSingle());
            CharacterLocomotionPresentationFactLineage factLineage =
                m_PresentationRuntime.CreateLocomotionFactLineage(in movementClock);
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
                    movementClock.OwnerIdentity,
                    result.Motion.ActionOwnerIdentity,
                    result.Motion.GameplayResultOwnerIdentity),
                movementClock,
                result.Motion.LocomotionTimeline,
                resetSequence,
                factLineage,
                CharacterPresentationTrajectoryIntent.ResolvePoseDiscontinuityIdentity(
                    in movementClock));
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
                TryRelease(() => GameplayTickSystem.UnregisterPresentationTarget(m_PresentationRuntime), failures);
                m_PresentationRegistered = false;
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

}
