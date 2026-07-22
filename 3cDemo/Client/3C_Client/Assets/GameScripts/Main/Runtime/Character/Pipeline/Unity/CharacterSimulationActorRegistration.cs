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
	public sealed class CharacterSimulationActorRegistration : ILocalSimulationActorRegistration
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
			Transform visualRoot)
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
			var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
				throw new ArgumentException("Local Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
			m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
				diagnosticsTarget.CharacterRuntimeId,
				ownerInstanceId,
				ownerName,
				projection.ProjectionRevision,
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
		public CharacterSimulationProgramAsset ProgramAsset { get; }
		public CharacterSimulationProgram Program { get; }
		public CharacterPresentationProjectionAsset ProjectionAsset { get; }
		public CharacterPresentationProjection Projection { get; }
		public CharacterPresentationSemanticContract PresentationContract { get; }
		public SimulationActorBinding ProgramIdentity { get; }
		public Float32WorldBodyBinding WorldBodyBinding { get; }
		public WorldBodyState InitialBody { get; }
		public IUnityCharacterControlSourceRuntime LocalControlSource { get; }
		public ICharacterSimulationGameplayOutputPort GameplayOutput { get; }
		public ICharacterPresentationRuntime PresentationRuntime { get; }
		public ISimulationPresentationOutputPort PresentationOutput { get; }
		public CharacterSimulationDiagnosticsAdapter Diagnostics { get; }
		public Transform VisualRoot { get; }
		public string VisualRootIdentity { get; }
		public RuntimeProgramRevision SourceMapRevision { get; }
		public SimulationOutputRouteDescriptor OutputRoute { get; }
		public bool IsActivated => m_Activated;
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
				CharacterPresentationBodyInterval.FromFloat32(result.BodySample));
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
			if (!GameplayTickSystem.RegisterPresentationTarget(m_PresentationTarget))
				throw new InvalidOperationException("GameplayTickSystem rejected the Actor Presentation target.");
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
					GameplayTickSystem.UnregisterPresentationTarget(m_PresentationTarget);
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
