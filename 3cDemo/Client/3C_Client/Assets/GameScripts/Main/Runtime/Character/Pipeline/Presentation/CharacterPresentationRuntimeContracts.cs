using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
	public interface ICharacterPresentationLookInput
	{
		bool TryGetLatchedVector2(string inputId, out Vector2 value);
	}

	public enum CharacterPresentationBodyStreamUpdateKind : byte
	{
		Append = 1,
		Reset = 2
	}

	public readonly struct CharacterPresentationBodyInterval
	{
		public CharacterPresentationBodyInterval(
			ulong previousTick,
			CharacterPresentationBodyState previousBody,
			ulong currentTick,
			CharacterPresentationBodyState currentBody,
			CharacterPresentationBodyStreamUpdateKind updateKind = CharacterPresentationBodyStreamUpdateKind.Append)
		{
			if (!previousBody.ActorId.IsValid || previousBody.ActorId != currentBody.ActorId)
				throw new ArgumentException("Presentation Body interval Actor identity is invalid.");
			if (currentTick == 0 || previousTick > currentTick)
				throw new ArgumentException("Presentation Body interval Tick order is invalid.");
			if (updateKind != CharacterPresentationBodyStreamUpdateKind.Append &&
				updateKind != CharacterPresentationBodyStreamUpdateKind.Reset)
			{
				throw new ArgumentOutOfRangeException(nameof(updateKind));
			}
			PreviousTick = previousTick;
			PreviousBody = previousBody;
			CurrentTick = currentTick;
			CurrentBody = currentBody;
			UpdateKind = updateKind;
		}

		public ActorId ActorId => CurrentBody.ActorId;
		public ulong PreviousTick { get; }
		public CharacterPresentationBodyState PreviousBody { get; }
		public ulong CurrentTick { get; }
		public CharacterPresentationBodyState CurrentBody { get; }
		public CharacterPresentationBodyStreamUpdateKind UpdateKind { get; }

		public static CharacterPresentationBodyInterval FromFloat32(
			CharacterBodySample sample,
			CharacterPresentationBodyStreamUpdateKind updateKind = CharacterPresentationBodyStreamUpdateKind.Append)
		{
			return new CharacterPresentationBodyInterval(
				sample.Tick.Value - 1,
				CharacterPresentationBodyState.FromFloat32(sample.BeforeBody),
				sample.Tick.Value,
				CharacterPresentationBodyState.FromFloat32(sample.FinalBody),
				updateKind);
		}
	}

	public readonly struct CharacterPresentationRuntimeDiagnosticsSnapshot
	{
		public CharacterPresentationRuntimeDiagnosticsSnapshot(
			ulong bodyBranchReplacementCount,
			ulong animationBranchReplacementCount,
			float followerPositionCorrectionMeters,
			float followerYawCorrectionDegrees,
			CharacterFootPlacementFrameSnapshot footPlacement)
		{
			BodyBranchReplacementCount = bodyBranchReplacementCount;
			AnimationBranchReplacementCount = animationBranchReplacementCount;
			FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
			FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
			FootPlacement = footPlacement;
		}

		public ulong BodyBranchReplacementCount { get; }
		public ulong AnimationBranchReplacementCount { get; }
		public float FollowerPositionCorrectionMeters { get; }
		public float FollowerYawCorrectionDegrees { get; }
		public CharacterFootPlacementFrameSnapshot FootPlacement { get; }
	}

	public interface ICharacterPresentationRuntime : IDisposable
	{
		void CaptureBodyInterval(CharacterPresentationBodyInterval interval);
		void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals);
		void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections);
		void Publish(CharacterPresentationCommand command);
		void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement);
		void Retire(CharacterPresentationCommand command);
		void Present(GameplayPresentationFrameContext context);
		CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics();
		void Reset();
	}

	public sealed class CharacterPresentationRuntimeBinding
	{
		public CharacterPresentationRuntimeBinding(
			CharacterPresentationProjection projection,
			ICharacterPresentationRuntime runtime)
		{
			Projection = projection ?? throw new ArgumentNullException(nameof(projection));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
		}

		public CharacterPresentationProjection Projection { get; }
		public ICharacterPresentationRuntime Runtime { get; }
	}

	public static class CharacterPresentationRuntimeFactory
	{
		public static CharacterPresentationProjection LoadProjection(
			CharacterPresentationProjectionAsset projectionAsset,
			CharacterPresentationSemanticContract contract)
		{
			if (!projectionAsset)
				throw new ArgumentNullException(nameof(projectionAsset));
			return projectionAsset.Load(contract);
		}

		public static CharacterPresentationRuntimeBinding CreateLocalOwner(
			CharacterPresentationProjectionAsset projectionAsset,
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			ThirdPersonCameraController cameraRig,
			Transform followAnchor,
			Transform aimAnchor,
			IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
			ICharacterPresentationLookInput inputAdapter,
			string lookInputId,
			RuntimeDiagnosticsContext diagnostics)
		{
			if (!projectionAsset)
				throw new ArgumentNullException(nameof(projectionAsset));
			return CreateLocalOwner(
				contract,
				simulationTickRate,
				LoadProjection(projectionAsset, contract),
				actorId,
				animancer,
				visualRoot,
				initialBody,
				bodyProfile,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				cameraRig,
				followAnchor,
				aimAnchor,
				cameraTargetBindings,
				inputAdapter,
				lookInputId,
				null,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateLocalOwner(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			ThirdPersonCameraController cameraRig,
			Transform followAnchor,
			Transform aimAnchor,
			IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
			ICharacterPresentationLookInput inputAdapter,
			string lookInputId,
			CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
			RuntimeDiagnosticsContext diagnostics)
		{
			return Create(
				contract,
				simulationTickRate,
				projection,
				actorId,
				animancer,
				visualRoot,
				initialBody,
				CharacterBodyPresentationSourceMode.CommittedStream,
				RequireBodySettings(bodyProfile),
				CharacterAnimationStartupPolicy.RequireCommittedSelection,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				cameraRig,
				followAnchor,
				aimAnchor,
				cameraTargetBindings,
				inputAdapter,
				lookInputId,
				equipmentRigCatalog,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateLocalOwner(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			ThirdPersonCameraController cameraRig,
			Transform followAnchor,
			Transform aimAnchor,
			IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
			ICharacterPresentationLookInput inputAdapter,
			string lookInputId,
			RuntimeDiagnosticsContext diagnostics)
		{
			return Create(
				contract,
				simulationTickRate,
				projection,
				actorId,
				animancer,
				visualRoot,
				initialBody,
				CharacterBodyPresentationSourceMode.CommittedStream,
				RequireBodySettings(bodyProfile),
				CharacterAnimationStartupPolicy.RequireCommittedSelection,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				cameraRig,
				followAnchor,
				aimAnchor,
				cameraTargetBindings,
				inputAdapter,
				lookInputId,
				null,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateSimulatedActor(
			CharacterPresentationProjectionAsset projectionAsset,
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			RuntimeDiagnosticsContext diagnostics)
		{
			if (!projectionAsset)
				throw new ArgumentNullException(nameof(projectionAsset));
			return CreateSimulatedActor(
				contract,
				simulationTickRate,
				LoadProjection(projectionAsset, contract),
				actorId,
				animancer,
				visualRoot,
				initialBody,
				bodyProfile,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateSimulatedActor(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			RuntimeDiagnosticsContext diagnostics)
		{
			return Create(
				contract,
				simulationTickRate,
				projection,
				actorId,
				animancer,
				visualRoot,
				initialBody,
				CharacterBodyPresentationSourceMode.CommittedStream,
				RequireBodySettings(bodyProfile),
				CharacterAnimationStartupPolicy.RequireCommittedSelection,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				null,
				null,
				null,
				null,
				null,
				string.Empty,
				null,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateSimulatedActor(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
			RuntimeDiagnosticsContext diagnostics)
		{
			return Create(
				contract,
				simulationTickRate,
				projection,
				actorId,
				animancer,
				visualRoot,
				initialBody,
				CharacterBodyPresentationSourceMode.CommittedStream,
				RequireBodySettings(bodyProfile),
				CharacterAnimationStartupPolicy.RequireCommittedSelection,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				null,
				null,
				null,
				null,
				null,
				string.Empty,
				equipmentRigCatalog,
				diagnostics);
		}

		public static CharacterPresentationRuntimeBinding CreateObservedActor(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationProfile bodyProfile,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			RuntimeDiagnosticsContext diagnostics)
		{
			return Create(
				contract,
				simulationTickRate,
				projection,
				actorId,
				animancer,
				visualRoot,
				initialBody,
				CharacterBodyPresentationSourceMode.SelectedStream,
				RequireBodySettings(bodyProfile),
				CharacterAnimationStartupPolicy.AwaitCommittedSelection,
				footPlacementProfile,
				footPlacementRig,
				footPlacementSolver,
				physicsScene,
				null,
				null,
				null,
				null,
				null,
				string.Empty,
				null,
				diagnostics);
		}

		static CharacterPresentationRuntimeBinding Create(
			CharacterPresentationSemanticContract contract,
			int simulationTickRate,
			CharacterPresentationProjection projection,
			ActorId actorId,
			AnimancerComponent animancer,
			Transform visualRoot,
			CharacterPresentationBodyState initialBody,
			CharacterBodyPresentationSourceMode bodySourceMode,
			CharacterBodyPresentationSettings bodySettings,
			CharacterAnimationStartupPolicy animationStartupPolicy,
			CharacterFootPlacementProfile footPlacementProfile,
			CharacterFootPlacementRig footPlacementRig,
			ICharacterFootPlacementSolver footPlacementSolver,
			PhysicsScene physicsScene,
			ThirdPersonCameraController cameraRig,
			Transform followAnchor,
			Transform aimAnchor,
			IReadOnlyList<CameraTargetBinding> cameraTargetBindings,
			ICharacterPresentationLookInput inputAdapter,
			string lookInputId,
			CharacterEquipmentRigBindingCatalog equipmentRigCatalog,
			RuntimeDiagnosticsContext diagnostics)
		{
			if (contract == null)
				throw new ArgumentNullException(nameof(contract));
			if (projection == null)
				throw new ArgumentNullException(nameof(projection));
			projection.RequireContract(contract);
			CharacterBodyPresentationRuntime body = null;
			CharacterAnimationPlaybackRuntime animation = null;
			CharacterFootPlacementRuntime footPlacement = null;
			CharacterCameraPresentationRuntime camera = null;
			CharacterEquipmentVisualRuntime equipment = null;
			try
			{
				body = new CharacterBodyPresentationRuntime(
					actorId,
					simulationTickRate,
					bodySourceMode,
					bodySettings,
					visualRoot,
					initialBody,
					diagnostics);
				animation = new CharacterAnimationPlaybackRuntime(
					contract,
					projection,
					animancer,
					true,
					AnimationTransitionEvaluationMode.Timed);
				equipment = new CharacterEquipmentVisualRuntime(
					actorId,
					projection,
					equipmentRigCatalog,
					diagnostics);
				if (!footPlacementProfile)
					throw new ArgumentNullException(nameof(footPlacementProfile));
				if (!footPlacementRig)
					throw new ArgumentNullException(nameof(footPlacementRig));
				if (footPlacementSolver == null)
					throw new ArgumentNullException(nameof(footPlacementSolver));
				if (!physicsScene.IsValid())
					throw new ArgumentException("Foot Placement requires a valid PhysicsScene.", nameof(physicsScene));
				CharacterFootPlacementRigBinding rig = footPlacementRig.BuildBinding();
				if (rig.VisualRoot != visualRoot)
					throw new InvalidOperationException("Foot Placement Rig VisualRoot must match Presentation VisualRoot exactly.");
				CharacterFootPlacementRuntimeSettings footPlacementSettings =
					footPlacementProfile.BuildSettings(projection, rig);
				footPlacement = new CharacterFootPlacementRuntime(
					actorId,
					footPlacementSettings,
					rig,
					footPlacementSolver,
					physicsScene,
					diagnostics);
				if (cameraRig)
				{
					camera = new CharacterCameraPresentationRuntime(
						projection,
						cameraRig,
						initialBody,
						followAnchor,
						aimAnchor,
						cameraTargetBindings,
						inputAdapter,
						lookInputId);
				}
				else if (followAnchor || aimAnchor || inputAdapter != null ||
						 cameraTargetBindings != null || !string.IsNullOrEmpty(lookInputId))
				{
					throw new ArgumentException("Camera-less Presentation received partial Camera configuration.");
				}

				var runtime = new CharacterSimulationPresentationRuntime(
					actorId,
					projection,
					body,
					animation,
					equipment,
					footPlacement,
					camera,
					animationStartupPolicy,
					diagnostics);
				body = null;
				animation = null;
				equipment = null;
				footPlacement = null;
				camera = null;
				return new CharacterPresentationRuntimeBinding(projection, runtime);
			}
			catch
			{
				CharacterPresentationModuleLifetime.Dispose(camera, footPlacement, equipment, animation, body);
				throw;
			}
		}

		static CharacterBodyPresentationSettings RequireBodySettings(
			CharacterBodyPresentationProfile profile)
		{
			if (!profile)
				throw new ArgumentNullException(nameof(profile));
			return profile.BuildSettings();
		}
	}

	internal static class CharacterPresentationModuleLifetime
	{
		public static void Dispose(
			CharacterCameraPresentationRuntime camera,
			CharacterFootPlacementRuntime footPlacement,
			CharacterEquipmentVisualRuntime equipment,
			CharacterAnimationPlaybackRuntime animation,
			CharacterBodyPresentationRuntime body)
		{
			try
			{
				camera?.Dispose();
			}
			finally
			{
				try
				{
					footPlacement?.Dispose();
				}
				finally
				{
					try
					{
						equipment?.Dispose();
					}
					finally
					{
						try
						{
							animation?.Dispose();
						}
						finally
						{
							body?.Dispose();
						}
					}
				}
			}
		}
		public static void Dispose(
			CharacterCameraPresentationRuntime camera,
			CharacterFootPlacementRuntime footPlacement,
			CharacterAnimationPlaybackRuntime animation,
			CharacterBodyPresentationRuntime body)
		{
			try
			{
				camera?.Dispose();
			}
			finally
			{
				try
				{
					footPlacement?.Dispose();
				}
				finally
				{
					try
					{
						animation?.Dispose();
					}
					finally
					{
						body?.Dispose();
					}
				}
			}
		}
	}

	public readonly struct CharacterPresentationEventHeader
	{
		public CharacterPresentationEventHeader(
			EventId eventId,
			ActorId actorId,
			SimulationTick tick,
			ActivationId activation,
			ulong sequence,
			string channel)
		{
			if (!eventId.IsValid || !actorId.IsValid || !tick.IsValid || !activation.IsValid || sequence == 0)
				throw new ArgumentException("Presentation event header is incomplete.");
			EventId = eventId;
			ActorId = actorId;
			Tick = tick;
			Activation = activation;
			Sequence = sequence;
			Channel = RequireIdentity(channel, nameof(channel));
		}

		public EventId EventId { get; }
		public ActorId ActorId { get; }
		public SimulationTick Tick { get; }
		public ActivationId Activation { get; }
		public ulong Sequence { get; }
		public string Channel { get; }

		static string RequireIdentity(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("Presentation identity is missing.", parameterName);
			return value.Trim();
		}
	}

	public enum CharacterPresentationCommandKind : byte
	{
		SelectProducer = 1,
		SampleProducer = 2,
		CompleteProducer = 3,
		ReleaseProducer = 4,
		Camera = 5,
		Cue = 6,
		Vfx = 7,
		Ui = 8
	}

	public readonly struct CharacterPresentationCommand
	{
		public CharacterPresentationCommand(
			CharacterPresentationEventHeader header,
			CharacterPresentationCommandKind kind,
			string producerId,
			float sampleTime,
			float weight,
			ulong producerGeneration = 0,
			int cycle = 0)
		{
			if (float.IsNaN(sampleTime) || float.IsInfinity(sampleTime) ||
				float.IsNaN(weight) || float.IsInfinity(weight))
			{
				throw new ArgumentOutOfRangeException(nameof(sampleTime));
			}
			if (IsPlaybackCommand(kind) && producerGeneration == 0)
				throw new ArgumentOutOfRangeException(nameof(producerGeneration));
			if (cycle < 0)
				throw new ArgumentOutOfRangeException(nameof(cycle));
			Header = header;
			Kind = kind;
			ProducerId = RequireIdentity(producerId, nameof(producerId));
			SampleTime = sampleTime;
			Weight = weight;
			ProducerGeneration = producerGeneration;
			Cycle = cycle;
		}

		public CharacterPresentationEventHeader Header { get; }
		public CharacterPresentationCommandKind Kind { get; }
		public string ProducerId { get; }
		public float SampleTime { get; }
		public float Weight { get; }
		public ulong ProducerGeneration { get; }
		public int Cycle { get; }

		public static CharacterPresentationCommand FromFloat32(PresentationCommand command)
		{
			return new CharacterPresentationCommand(
				new CharacterPresentationEventHeader(
					command.Header.EventId,
					command.Header.ActorId,
					command.Header.Tick,
					command.Header.Activation,
					command.Header.Sequence,
					command.Header.Channel),
				(CharacterPresentationCommandKind)(byte)command.Kind,
				command.ProducerId,
				command.SampleTime.ToSingle(),
				command.Weight.ToSingle(),
				command.ProducerGeneration,
				command.Cycle);
		}

		static bool IsPlaybackCommand(CharacterPresentationCommandKind kind)
		{
			return kind == CharacterPresentationCommandKind.SelectProducer ||
				   kind == CharacterPresentationCommandKind.SampleProducer ||
				   kind == CharacterPresentationCommandKind.CompleteProducer ||
				   kind == CharacterPresentationCommandKind.ReleaseProducer;
		}

		static string RequireIdentity(string value, string parameterName)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("Presentation identity is missing.", parameterName);
			return value.Trim();
		}
	}

	public readonly struct CharacterPresentationBodyState
	{
		public CharacterPresentationBodyState(
			ActorId actorId,
			Vector3 position,
			Quaternion rotation,
			Vector3 linearVelocity,
			bool grounded)
		{
			if (!actorId.IsValid || !IsFinite(position) || !IsFinite(rotation) || !IsFinite(linearVelocity))
				throw new ArgumentException("Presentation body state is incomplete.");
			ActorId = actorId;
			Position = position;
			Rotation = rotation.normalized;
			LinearVelocity = linearVelocity;
			Grounded = grounded;
		}

		public ActorId ActorId { get; }
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public Vector3 LinearVelocity { get; }
		public bool Grounded { get; }

		public static CharacterPresentationBodyState FromFloat32(WorldBodyState body)
		{
			return new CharacterPresentationBodyState(
				body.ActorId,
				new Vector3(body.Position.X.ToSingle(), body.Position.Y.ToSingle(), body.Position.Z.ToSingle()),
				Quaternion.Euler(0f, body.Yaw.Degrees.ToSingle(), 0f),
				new Vector3(body.Velocity.X.ToSingle(), body.Velocity.Y.ToSingle(), body.Velocity.Z.ToSingle()),
				body.Grounded);
		}

		static bool IsFinite(Vector3 value) =>
			IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

		static bool IsFinite(Quaternion value) =>
			IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

		static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
	}
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
