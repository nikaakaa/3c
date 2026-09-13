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
    public sealed class FixedCharacterRegistration :
        IFixedLocalSimulationActorRegistration,
        ISimulationActorStartGate, ISimulationProgramEpochRegistration,
        ISimulationPresentationCheckpointRuntime
    {
        readonly IUnityFixedCharacterControlSourceRuntime m_ControlSource;
        readonly FixedUnityPresentationOutputAdapter m_PresentationOutput;
        ICharacterPresentationRuntime m_PresentationRuntime;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly FixedCharacterSimulationDiagnosticsAdapter m_DiagnosticsAdapter;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;
        readonly CharacterPresentationFrameTarget m_PresentationTarget;
        readonly int m_MaximumActivePresentationRecords;
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
        FixedCharacterSimulationProgramAsset m_PendingProgramAsset;
        FixedCharacterSimulationProgram m_PendingProgram;
        FixedSimulationActorBinding m_PendingProgramIdentity;
        CharacterPresentationProjectionAsset m_PendingProjectionAsset;
        CharacterPresentationProjection m_PendingProjection;
        CharacterPresentationSemanticContract m_PendingPresentationContract;
        ICharacterPresentationRuntime m_PendingPresentationRuntime;
        readonly Func<
            CharacterPresentationSemanticContract,
            CharacterPresentationProjection,
            CharacterPresentationRuntimeBinding> m_PresentationRuntimeFactory;
        readonly CharacterControlRuntimeBinding m_ControlRuntimeBinding;
        readonly CharacterBodyMotionBinding m_BodyMotionBinding;

        public FixedCharacterRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            FixedCharacterSimulationProgram program,
            CharacterPresentationProjectionAsset projectionAsset,
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract presentationContract,
            AnimationPresentationProgramIdentity presentationProgramIdentity,
            string worldBodyBindingId,
            FixedWorldBodyState initialBody,
            IUnityFixedCharacterControlSourceRuntime controlSource,
            FixedUnityPresentationOutputAdapter presentationOutput,
            ICharacterPresentationRuntime presentationRuntime,
            CharacterRootHierarchyBinding rootHierarchy,
            RuntimeDiagnosticsContext diagnosticsContext,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            int maximumActivePresentationRecords,
            Func<
                CharacterPresentationSemanticContract,
                CharacterPresentationProjection,
                CharacterPresentationRuntimeBinding> presentationRuntimeFactory,
            CharacterControlRuntimeBinding controlRuntimeBinding,
            CharacterBodyMotionBinding bodyMotionBinding)
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
            ProjectionAsset = projectionAsset ? projectionAsset : throw new ArgumentNullException(nameof(projectionAsset));
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            PresentationContract = presentationContract ?? throw new ArgumentNullException(nameof(presentationContract));
            Projection.RequireContract(PresentationContract);
            WorldBodyBindingId = worldBodyBindingId.Trim();
            InitialBody = initialBody;
            m_MaximumActivePresentationRecords = maximumActivePresentationRecords;
            m_ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
            if (!m_ControlSource.CharacterProgramId.Equals(Program.Manifest.ProgramId) ||
                !m_ControlSource.CharacterProgramHash.Equals(Program.ProgramHash))
            {
                throw new ArgumentException("Fixed Control Source does not match the Actor Program.", nameof(controlSource));
            }
            m_PresentationOutput = presentationOutput ?? throw new ArgumentNullException(nameof(presentationOutput));
            m_PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
            m_PresentationRuntimeFactory = presentationRuntimeFactory ??
                throw new ArgumentNullException(nameof(presentationRuntimeFactory));
            m_ControlRuntimeBinding = controlRuntimeBinding ?? throw new ArgumentNullException(nameof(controlRuntimeBinding));
            m_BodyMotionBinding = bodyMotionBinding ?? throw new ArgumentNullException(nameof(bodyMotionBinding));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            DiagnosticsContext = diagnosticsContext ?? throw new ArgumentNullException(nameof(diagnosticsContext));
            m_DiagnosticsAdapter = new FixedCharacterSimulationDiagnosticsAdapter(DiagnosticsContext, Program);
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
                throw new ArgumentException("Fixed Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
            m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
                diagnosticsTarget.CharacterRuntimeId,
                actorId,
                ownerInstanceId,
                ownerName,
                presentationProgramIdentity,
                animationSnapshotProvider);
            m_PresentationTarget =
                new CharacterPresentationFrameTarget(presentationRuntime);
            ProgramIdentity = new FixedSimulationActorBinding(actorId, program, WorldBodyBindingId, m_ControlRuntimeBinding, m_BodyMotionBinding);
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
        public FixedCharacterSimulationProgram Program { get; private set; }
        public FixedSimulationActorBinding ProgramIdentity { get; private set; }
        public CharacterBodyMotionBinding BodyMotionBinding => m_BodyMotionBinding;
        public CharacterPresentationProjectionAsset ProjectionAsset { get; private set; }
        public CharacterPresentationProjection Projection { get; private set; }
        public CharacterPresentationSemanticContract PresentationContract { get; private set; }
        public string WorldBodyBindingId { get; }
        public FixedWorldBodyState InitialBody { get; }
        public RuntimeDiagnosticsContext DiagnosticsContext { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; private set; }
        public IFixedCharacterControlSourceRuntime FixedControlSource => m_ControlSource;
        public ICharacterPresentationRuntime PresentationRuntime => m_PresentationRuntime;
        public IFixedPresentationCommitOutputPort PresentationOutput => m_PresentationOutput;
        public ThirdPersonSimulation.Fixed.ISimulationDiagnosticsSink SimulationDiagnostics => m_DiagnosticsAdapter;
        public ISimulationProgramBinding ProgramBinding => m_PendingProgramIdentity ?? ProgramIdentity;
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

        public bool TryPrepareProgram(
            FixedCharacterSimulationProgramAsset programAsset,
            FixedCharacterSimulationProgram program,
            CharacterPresentationProjectionAsset projectionAsset,
            CharacterPresentationProjection projection,
            out string error)
        {
            RequireAlive();
            error = string.Empty;
            DiscardProgramEpoch();
            if (!programAsset || program == null)
            {
                error = "Program adoption requires a valid Fixed Program asset and loaded Program.";
                return false;
            }
			if (program.Manifest.ProgramId != Program.Manifest.ProgramId ||
				!program.LayoutHash.Equals(Program.LayoutHash) ||
				program.Manifest.NumericProfile != Program.Manifest.NumericProfile)
            {
				error = "Program adoption changes the locked ProgramId, LayoutHash, or Numeric Profile.";
				return false;
			}
			CharacterPresentationSemanticContract candidateContract =
				FixedCharacterPresentationContractAdapter.Create(program);
            if (!CharacterPresentationSemanticContract.TryMatchProducerTopology(
					PresentationContract,
					candidateContract,
					out error))
				return false;
			if (projection != null)
			{
				if (!projectionAsset)
				{
					error = "Presentation Projection adoption requires a valid Projection asset.";
					return false;
				}
				try
				{
					projection.RequireContract(candidateContract);
					projection.RequirePosePayload();
					projection.RequireTuningPayload();
				}
				catch (Exception exception)
				{
					error = exception.Message;
					return false;
				}
				m_PendingProjectionAsset = projectionAsset;
				m_PendingProjection = projection;
				m_PendingPresentationContract = candidateContract;
				if (!SameProjectionIdentity(Projection, projection))
				{
					try
					{
						CharacterPresentationRuntimeBinding binding =
							m_PresentationRuntimeFactory(candidateContract, projection);
						if (binding == null || binding.Runtime == null ||
							!SameProjectionIdentity(binding.Projection, projection))
							throw new InvalidOperationException("Presentation Projection factory returned a mismatched runtime.");
						m_PendingPresentationRuntime = binding.Runtime;
					}
					catch (Exception exception)
					{
						DiscardPendingPresentation();
						error = exception.Message;
						return false;
					}
				}
			}
			m_PendingProgramAsset = programAsset;
			m_PendingProgram = program;
			m_PendingProgramIdentity = new FixedSimulationActorBinding(ActorId, program, WorldBodyBindingId, m_ControlRuntimeBinding, m_BodyMotionBinding);
			return true;
		}

		public void CommitProgramEpoch()
		{
			if (m_PendingProgram == null)
				return;
			if (m_ControlSource is IFixedCharacterControlSourceProgramAdoption adoption &&
				!adoption.TryAdoptProgram(m_PendingProgram, out string inputError))
			{
				throw new InvalidOperationException(inputError);
			}
			m_DiagnosticsAdapter.AdoptProgram(m_PendingProgram);
			if (m_PendingProjection != null)
			{
				if (m_PendingPresentationRuntime != null)
				{
					ICharacterPresentationRuntime previous = m_PresentationRuntime;
					ICharacterPresentationRuntime next = m_PendingPresentationRuntime;
					IAnimationPresentationRuntimeSnapshotProvider snapshotProvider =
						next as IAnimationPresentationRuntimeSnapshotProvider ??
						throw new InvalidOperationException("Presentation Projection runtime has no diagnostics provider.");
					m_PresentationOutput.Reset();
					m_PresentationOutput.ReplaceRuntime(m_PendingProjection, next);
					m_PresentationTarget.ReplaceRuntime(next);
					m_AnimationDiagnosticsTarget.Replace(
						new AnimationPresentationProgramIdentity(m_PendingProjection),
						snapshotProvider);
					m_PresentationRuntime = next;
					m_PendingPresentationRuntime = null;
					previous.Dispose();
				}
				ProjectionAsset = m_PendingProjectionAsset;
				Projection = m_PendingProjection;
				PresentationContract = m_PendingPresentationContract;
			}
			Program = m_PendingProgram;
			ProgramIdentity = m_PendingProgramIdentity;
			DiscardProgramEpoch();
		}

		public void DiscardProgramEpoch()
		{
			m_PendingProgramAsset = null;
			m_PendingProgram = null;
			m_PendingProgramIdentity = null;
			DiscardPendingPresentation();
		}

		void DiscardPendingPresentation()
		{
			m_PendingProjectionAsset = null;
			m_PendingProjection = null;
			m_PendingPresentationContract = null;
			ICharacterPresentationRuntime pending = m_PendingPresentationRuntime;
			m_PendingPresentationRuntime = null;
			pending?.Dispose();
		}

		static bool SameProjectionIdentity(
			CharacterPresentationProjection current,
			CharacterPresentationProjection candidate)
		{
			return current != null && candidate != null &&
				string.Equals(current.ProgramId, candidate.ProgramId, StringComparison.Ordinal) &&
				string.Equals(current.SourceRevision, candidate.SourceRevision, StringComparison.Ordinal) &&
				string.Equals(current.SemanticHash, candidate.SemanticHash, StringComparison.Ordinal) &&
				string.Equals(current.ContractHash, candidate.ContractHash, StringComparison.Ordinal) &&
				string.Equals(current.ProjectionRevision, candidate.ProjectionRevision, StringComparison.Ordinal);
		}

		public void PublishCheckpoint(ulong tick, string snapshotIdentity, StableHash snapshotHash) =>
			m_DiagnosticsAdapter.PublishCheckpoint(tick, snapshotIdentity, snapshotHash);

		public void BindProgramEpoch(ulong programEpoch) =>
			DiagnosticsContext.SetProgramEpoch(programEpoch);

		public void BindExecutionBranch(Guid executionBranchId) =>
			DiagnosticsContext.SetExecutionBranch(executionBranchId);
        public bool IsSimulationStartReady =>
            FixedCharacterInputTraceModule.CanAdvanceSimulation(ActorId);
        public string SimulationStartWaitReason =>
            IsSimulationStartReady
                ? string.Empty
                : "Canonical Fixed input trace start state is not released.";
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
                var intervals = new List<CharacterPresentationBodyInterval>(m_PendingBodySamples.Count);
                FixedCharacterBodySample finalSample = default;
                foreach (FixedCharacterBodySample sample in m_PendingBodySamples.Values)
                {
                    finalSample = sample;
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
