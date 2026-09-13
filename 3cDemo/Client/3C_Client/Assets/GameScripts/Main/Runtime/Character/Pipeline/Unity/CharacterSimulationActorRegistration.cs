using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
	public sealed class CharacterSimulationActorRegistration :
		ILocalSimulationActorRegistration,
		ISimulationProgramEpochRegistration,
		ISimulationPresentationCheckpointRuntime
	{
		readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
		readonly AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;
		readonly CharacterPresentationFrameTarget m_PresentationTarget;
		readonly List<EquipmentVisualSelection> m_EquipmentVisualSelections = new List<EquipmentVisualSelection>();
		bool m_Activated;
		bool m_InputActivated;
		bool m_DiagnosticsRegistered;
		bool m_AnimationDiagnosticsRegistered;
		bool m_PresentationRegistered;
		ulong m_TrajectoryIntentSequence;
		bool m_Disposed;
		CharacterSimulationProgramAsset m_PendingProgramAsset;
		CharacterSimulationProgram m_PendingProgram;
		SimulationActorBinding m_PendingProgramIdentity;
		CharacterPresentationProjectionAsset m_PendingProjectionAsset;
		CharacterPresentationProjection m_PendingProjection;
		CharacterPresentationSemanticContract m_PendingPresentationContract;
		ICharacterPresentationRuntime m_PendingPresentationRuntime;
		readonly Func<
			CharacterPresentationSemanticContract,
			CharacterPresentationProjection,
			CharacterPresentationRuntimeBinding> m_PresentationRuntimeFactory;

		public CharacterSimulationActorRegistration(
			int ownerInstanceId,
			string ownerName,
			ActorId actorId,
			CharacterSimulationProgramAsset programAsset,
			CharacterSimulationProgram program,
			CharacterPresentationProjectionAsset projectionAsset,
			CharacterPresentationProjection projection,
			CharacterPresentationSemanticContract presentationContract,
			Float32WorldBodyBinding worldBodyBinding,
			WorldBodyState initialBody,
			IUnityCharacterControlSourceRuntime localControlSource,
			ICharacterSimulationGameplayOutputPort gameplayOutput,
			ICharacterPresentationRuntime presentationRuntime,
			CharacterSimulationDiagnosticsAdapter diagnostics,
			RuntimeDiagnosticsTarget diagnosticsTarget,
			Transform visualRoot,
			Func<
				CharacterPresentationSemanticContract,
				CharacterPresentationProjection,
				CharacterPresentationRuntimeBinding> presentationRuntimeFactory)
		{
			if (ownerInstanceId == 0 || string.IsNullOrWhiteSpace(ownerName) || !actorId.IsValid)
				throw new ArgumentException("Actor registration owner identity is incomplete.");
			OwnerInstanceId = ownerInstanceId;
			OwnerName = ownerName.Trim();
			ActorId = actorId;
			ProgramAsset = programAsset ? programAsset : throw new ArgumentNullException(nameof(programAsset));
			Program = program ?? throw new ArgumentNullException(nameof(program));
			ProjectionAsset = projectionAsset ? projectionAsset : throw new ArgumentNullException(nameof(projectionAsset));
			Projection = projection ?? throw new ArgumentNullException(nameof(projection));
			PresentationContract = presentationContract ?? throw new ArgumentNullException(nameof(presentationContract));
			Projection.RequireContract(PresentationContract);
			WorldBodyBinding = worldBodyBinding ? worldBodyBinding : throw new ArgumentNullException(nameof(worldBodyBinding));
			if (worldBodyBinding.ActorId != actorId || initialBody.ActorId != actorId)
				throw new ArgumentException("Actor registration body identity does not match ActorId.");
			InitialBody = initialBody;
			LocalControlSource = localControlSource;
			GameplayOutput = gameplayOutput ?? throw new ArgumentNullException(nameof(gameplayOutput));
			PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
			PresentationOutput = presentationRuntime as ISimulationPresentationOutputPort ??
				throw new ArgumentException("Local Presentation Runtime does not expose the Float32 output port.", nameof(presentationRuntime));
			Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
			m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
			m_PresentationRuntimeFactory = presentationRuntimeFactory ??
				throw new ArgumentNullException(nameof(presentationRuntimeFactory));
			var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
				throw new ArgumentException("Local Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
			m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
				diagnosticsTarget.CharacterRuntimeId,
				actorId,
				ownerInstanceId,
				ownerName,
				new AnimationPresentationProgramIdentity(projection),
				animationSnapshotProvider);
			VisualRoot = visualRoot ? visualRoot : throw new ArgumentNullException(nameof(visualRoot));
			ProgramIdentity = new SimulationActorBinding(actorId, program, worldBodyBinding.BindingId);
			VisualRootIdentity = BuildTransformIdentity(visualRoot);
			SourceMapRevision = diagnostics.Context.Revision;
			m_PresentationTarget = new CharacterPresentationFrameTarget(presentationRuntime);
			OutputRoute = new SimulationOutputRouteDescriptor(
				$"character-output/{actorId.Value}",
				"character-simulation-output",
				1,
				actorId,
				StableHash.Compute(
					actorId.Value,
					program.ProgramHash.ToString(),
					projection.SourceRevision,
					worldBodyBinding.BindingId,
					VisualRootIdentity,
					SourceMapRevision.ProgramId,
					SourceMapRevision.SourceRevision,
					SourceMapRevision.ProgramHash));
		}

		public int OwnerInstanceId { get; }
		public string OwnerName { get; }
		public string OwnerIdentity => $"unity-character-host/{OwnerInstanceId}";
		public ActorId ActorId { get; }
		public CharacterSimulationProgramAsset ProgramAsset { get; private set; }
		public CharacterSimulationProgram Program { get; private set; }
		public CharacterPresentationProjectionAsset ProjectionAsset { get; private set; }
		public CharacterPresentationProjection Projection { get; private set; }
		public CharacterPresentationSemanticContract PresentationContract { get; private set; }
		public SimulationActorBinding ProgramIdentity { get; private set; }
		public Float32WorldBodyBinding WorldBodyBinding { get; }
		public WorldBodyState InitialBody { get; }
		public IUnityCharacterControlSourceRuntime LocalControlSource { get; }
		public ICharacterSimulationGameplayOutputPort GameplayOutput { get; }
		public ICharacterPresentationRuntime PresentationRuntime { get; private set; }
		public ISimulationPresentationOutputPort PresentationOutput { get; private set; }
		public CharacterSimulationDiagnosticsAdapter Diagnostics { get; }
		public Transform VisualRoot { get; }
		public string VisualRootIdentity { get; }
		public RuntimeProgramRevision SourceMapRevision { get; private set; }
		public SimulationOutputRouteDescriptor OutputRoute { get; private set; }
		public bool IsActivated => m_Activated;
		public ISimulationProgramBinding ProgramBinding => m_PendingProgramIdentity ?? ProgramIdentity;
		public bool SupportsPresentationCheckpointCapture =>
			PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
			checkpoint.SupportsCheckpointCapture;
		public bool SupportsPresentationCheckpointRestore =>
			PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
			checkpoint.SupportsCheckpointCapture &&
			checkpoint.SupportsCheckpointRestore;
        public bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (PresentationRuntime is ICharacterPresentationCheckpointRuntime runtime)
                return runtime.TryCaptureCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint capture.";
            return false;
        }
        public bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
		{
			if (PresentationRuntime is ICharacterPresentationCheckpointRuntime restore)
				return restore.TryRestoreCheckpoint(checkpoint, out error);
			error = "Character Presentation runtime does not expose checkpoint restore.";
			return false;
		}

		public bool TryPrepareProgram(
			CharacterSimulationProgramAsset programAsset,
			CharacterSimulationProgram program,
			CharacterPresentationProjectionAsset projectionAsset,
			CharacterPresentationProjection projection,
			out string error)
		{
			RequireAlive();
			error = string.Empty;
			DiscardProgramEpoch();
			if (!programAsset || program == null)
			{
				error = "Program adoption requires a valid Program asset and loaded Program.";
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
				Float32CharacterPresentationContractAdapter.Create(program);
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
			m_PendingProgramIdentity = new SimulationActorBinding(ActorId, program, WorldBodyBinding.BindingId);
			return true;
		}

		public void CommitProgramEpoch()
		{
			if (m_PendingProgram == null)
				return;
			if (LocalControlSource is ICharacterControlSourceProgramAdoption adoption &&
				!adoption.TryAdoptProgram(m_PendingProgram, out string inputError))
			{
				throw new InvalidOperationException(inputError);
			}
			Diagnostics.AdoptProgram(m_PendingProgram);
			if (m_PendingProjection != null)
			{
				if (m_PendingPresentationRuntime != null)
				{
					ICharacterPresentationRuntime previous = PresentationRuntime;
					ICharacterPresentationRuntime next = m_PendingPresentationRuntime;
					IAnimationPresentationRuntimeSnapshotProvider snapshotProvider =
						next as IAnimationPresentationRuntimeSnapshotProvider ??
						throw new InvalidOperationException("Presentation Projection runtime has no diagnostics provider.");
					ISimulationPresentationOutputPort presentationOutput =
						next as ISimulationPresentationOutputPort ??
						throw new InvalidOperationException("Presentation Projection runtime has no Presentation output port.");
					m_PresentationTarget.ReplaceRuntime(m_PendingPresentationRuntime);
					m_AnimationDiagnosticsTarget.Replace(
						new AnimationPresentationProgramIdentity(m_PendingProjection),
						snapshotProvider);
					PresentationRuntime = next;
					PresentationOutput = presentationOutput;
					m_PendingPresentationRuntime = null;
					previous.Dispose();
				}
				ProjectionAsset = m_PendingProjectionAsset;
				Projection = m_PendingProjection;
				PresentationContract = m_PendingPresentationContract;
			}
			ProgramAsset = m_PendingProgramAsset;
			Program = m_PendingProgram;
			ProgramIdentity = m_PendingProgramIdentity;
			SourceMapRevision = Diagnostics.Context.Revision;
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
			Diagnostics.PublishCheckpoint(tick, snapshotIdentity, snapshotHash);

		public void BindProgramEpoch(ulong programEpoch) =>
			Diagnostics.Context.SetProgramEpoch(programEpoch);

		public void BindExecutionBranch(Guid executionBranchId) =>
			Diagnostics.Context.SetExecutionBranch(executionBranchId);

		ICharacterControlSourceRuntime ILocalSimulationActorRegistration.LocalControlSource => LocalControlSource;
		ISimulationGameplayOutputPort IFloat32SimulationActorRegistration.GameplayOutput => GameplayOutput;
		ISimulationPresentationOutputPort IFloat32SimulationActorRegistration.PresentationOutput => PresentationOutput;
		ISimulationDiagnosticsSink IFloat32SimulationActorRegistration.SimulationDiagnostics => Diagnostics;
		StableHash ISimulationActorRegistration.DiagnosticsConfigurationHash => StableHash.Compute(
			SourceMapRevision.ProgramId,
			SourceMapRevision.SourceRevision,
			SourceMapRevision.ProgramHash);

		public void Activate()
		{
			RequireAlive();
			if (m_Activated)
				return;
			try
			{
				if (LocalControlSource != null)
				{
					LocalControlSource.Activate();
					m_InputActivated = true;
				}
				RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
				m_DiagnosticsRegistered = true;
				AnimationPresentationRuntimeTargetRegistry.Register(m_AnimationDiagnosticsTarget);
				m_AnimationDiagnosticsRegistered = true;
				RegisterPresentationTarget();
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
				throw new AggregateException($"Actor '{ActorId}' activation resources failed to release.", failures);
		}

		public void CaptureRenderFrame(ulong renderFrame)
		{
			RequireAlive();
			if (!m_Activated)
				throw new InvalidOperationException($"Actor '{ActorId}' registration is not active.");
			LocalControlSource?.CaptureRenderFrame(renderFrame);
		}

		public void BeginLogicTick()
		{
			RequireAlive();
			GameplayOutput.BeginTick();
		}

		public void CapturePublishedResult(SimulationActorTickResult result)
		{
			RequireAlive();
			if (result == null || result.ActorId != ActorId)
				throw new ArgumentException("Published Presentation result targets another Actor.", nameof(result));
			PresentationRuntime.CaptureBodyInterval(
				CharacterPresentationBodyInterval.FromFloat32(result.BodySample, Program.Manifest.TickRate));
			if (PresentationRuntime.AcceptsTrajectoryIntent)
			{
				PresentationRuntime.CaptureTrajectoryIntent(
					CharacterPresentationTrajectoryIntent.FromFloat32(
						result,
						checked(++m_TrajectoryIntentSequence),
						PresentationRuntime.BodyResetSequence));
			}
			if (!result.State.TryGetEquipmentState(out EquipmentStateAggregate equipment))
				return;
			m_EquipmentVisualSelections.Clear();
			for (int i = 0; i < equipment.Slots.Count; i++)
				m_EquipmentVisualSelections.Add(equipment.Slots[i].CreateVisualSelection(ActorId, result.Tick.Value));
			PresentationRuntime.CaptureEquipmentSelections(m_EquipmentVisualSelections);
		}

		void IFloat32PublishedActorResultObserver.ObservePublished(SimulationActorTickResult result)
		{
			CapturePublishedResult(result);
		}

		void RegisterPresentationTarget()
		{
			RequireAlive();
			if (m_PresentationRegistered)
				return;
			m_PresentationTarget.Activate();
			m_PresentationRegistered = true;
		}

		public void Dispose()
		{
			if (m_Disposed)
				return;
			m_Disposed = true;
			var failures = new List<Exception>();
			TryRelease(Deactivate, failures);
			TryRelease(m_DiagnosticsTarget.Terminate, failures);
			TryRelease(m_DiagnosticsTarget.Dispose, failures);
			TryRelease(PresentationRuntime.Dispose, failures);
			if (LocalControlSource != null)
				TryRelease(LocalControlSource.Dispose, failures);
			if (failures.Count != 0)
				throw new AggregateException($"Actor '{ActorId}' registration failed to dispose completely.", failures);
		}

		void ReleaseActivation(List<Exception> failures)
		{
			if (m_PresentationRegistered)
			{
				try
				{
					m_PresentationTarget.Deactivate();
				}
				catch (Exception exception)
				{
					failures.Add(exception);
				}
				finally
				{
					m_PresentationRegistered = false;
				}
			}
			if (m_AnimationDiagnosticsRegistered)
			{
				try
				{
					AnimationPresentationRuntimeTargetRegistry.Unregister(m_AnimationDiagnosticsTarget);
				}
				catch (Exception exception)
				{
					failures.Add(exception);
				}
				finally
				{
					m_AnimationDiagnosticsRegistered = false;
				}
			}
			if (m_DiagnosticsRegistered)
			{
				try
				{
					RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget);
				}
				catch (Exception exception)
				{
					failures.Add(exception);
				}
				finally
				{
					m_DiagnosticsRegistered = false;
				}
			}
			if (m_InputActivated)
			{
				try
				{
					LocalControlSource.Deactivate();
				}
				catch (Exception exception)
				{
					failures.Add(exception);
				}
				finally
				{
					m_InputActivated = false;
				}
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
				throw new ObjectDisposedException(nameof(CharacterSimulationActorRegistration));
		}

		static string BuildTransformIdentity(Transform transform)
		{
			string path = transform.name;
			Transform current = transform.parent;
			while (current)
			{
				path = current.name + "/" + path;
				current = current.parent;
			}
			return $"{transform.gameObject.scene.path}:{path}";
		}
	}
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
