using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonCamera;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline
{
	[DisallowMultipleComponent]
	public sealed class CharacterPipelineHost : TimelinePreviewTarget, ISimulationSessionActorHost
	{
		[SerializeField] CharacterPipelineDefinition m_Definition;
		[SerializeField] SimulationSessionHost m_SessionHost;
		[SerializeField] string m_ActorId;
		[SerializeField] CharacterControlSource m_ControlSource;
		[SerializeField] CharacterPresentationRole m_PresentationRole = CharacterPresentationRole.LocalOwner;
		[SerializeField] AnimancerComponent m_Animancer;
		[SerializeField] CharacterAnimationRigBinding m_AnimationRigBinding;
		[SerializeField] Float32WorldBodyBinding m_WorldBodyBinding;
		[SerializeField] CharacterRootHierarchyBinding m_RootHierarchy;
		[SerializeField] CharacterEquipmentRigBindingCatalog m_EquipmentRigBindings;
		[SerializeField] CharacterEquipmentPreviewFixture m_EquipmentPreviewFixture;
		[SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
		[SerializeField] CharacterWorldAwarePresentationBinding m_WorldAwarePresentation;
		[SerializeField] ThirdPersonCameraController m_CameraRig;
		[SerializeField] Transform m_CameraFollowAnchor;
		[SerializeField] Transform m_CameraAimAnchor;
		[SerializeField] List<CameraTargetBinding> m_CameraTargetBindings = new List<CameraTargetBinding>();
		[SerializeField] string m_CameraLookInputValueId;

		CharacterSimulationActorRegistration m_Registration;
		CharacterPipelinePreviewController m_PreviewController;

		public CharacterPipelineDefinition Definition => m_Definition;
		public SimulationSessionHost SessionHost => m_SessionHost;
		public string ActorId => string.IsNullOrWhiteSpace(m_ActorId) ? string.Empty : m_ActorId.Trim();
		public ActorId SimulationActorId => new ActorId(ActorId);
		public CharacterControlSource ControlSource => m_ControlSource;
		public CharacterPresentationRole PresentationRole => m_PresentationRole;
		public string WorldRevision => m_SessionHost && m_SessionHost.Composition
			? m_SessionHost.Composition.WorldRevision
			: string.Empty;
		public AnimancerComponent Animancer => m_Animancer;
		public CharacterAnimationRigBinding AnimationRigBinding => m_AnimationRigBinding;
		public Float32WorldBodyBinding WorldBodyBinding => m_WorldBodyBinding;
		public CharacterRootHierarchyBinding RootHierarchy => m_RootHierarchy;
		public Transform LogicRoot => m_RootHierarchy ? m_RootHierarchy.LogicRoot : null;
		public Transform VisualRoot => m_RootHierarchy ? m_RootHierarchy.VisualRoot : null;
		public Transform PoseRoot => m_RootHierarchy ? m_RootHierarchy.PoseRoot : null;
		public CharacterEquipmentRigBindingCatalog EquipmentRigBindings => m_EquipmentRigBindings;
		public CharacterEquipmentPreviewFixture EquipmentPreviewFixture => m_EquipmentPreviewFixture;
		public CharacterBodyPresentationProfile BodyPresentationProfile => m_BodyPresentationProfile;
		public CharacterWorldAwarePresentationBinding WorldAwarePresentation => m_WorldAwarePresentation;
		public ThirdPersonCameraController CameraRig => m_CameraRig;
		public Transform CameraFollowAnchor => m_CameraFollowAnchor;
		public Transform CameraAimAnchor => m_CameraAimAnchor;
		public IReadOnlyList<CameraTargetBinding> CameraTargetBindings => m_CameraTargetBindings;
		public string CameraLookInputValueId => string.IsNullOrWhiteSpace(m_CameraLookInputValueId)
			? string.Empty
			: m_CameraLookInputValueId.Trim();
		public CharacterSimulationActorRegistration Registration => m_Registration;
		public bool HasPreviewAnimationDebugView =>
			m_PreviewController != null &&
			m_PreviewController.HasAnimationDebugView;
		public AnimationPresentationDebugView PreviewAnimationDebugView =>
			m_PreviewController != null
				? m_PreviewController.AnimationDebugView
				: throw new InvalidOperationException(
					"Animation Preview Debug View is unavailable.");
		public CharacterPosePlanStageSnapshot PreviewPosePlanStages =>
			m_PreviewController != null
				? m_PreviewController.PosePlanStages
				: default;
		internal CharacterPoseTuningLayout PreviewTuningLayout =>
			m_PreviewController?.TuningLayout;
		internal CharacterPoseTuningParameterBlock PreviewActiveTuningBlock =>
			m_PreviewController?.ActiveTuningBlock;
		internal CharacterPoseTuningRuntimeState PreviewTuningState =>
			m_PreviewController?.TuningState ?? default;
		internal CharacterPoseTuningLayout LiveTuningLayout =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.TuningLayout;
		internal CharacterPoseTuningParameterBlock LiveActiveTuningBlock =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.ActiveTuningBlock;
		internal CharacterPoseTuningRuntimeState LiveTuningState =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.TuningState ?? default;
		internal bool SubmitPreviewPoseTuningCandidate(
			string sourceAuthoringRevision,
			string candidateRevision,
			CharacterPoseTuningParameterBlock block,
			out string error)
		{
			if (m_PreviewController == null)
			{
				error = "Pose tuning requires an active Preview controller.";
				return false;
			}
			return m_PreviewController.SubmitPoseTuningCandidate(
				sourceAuthoringRevision,
				candidateRevision,
				block,
				out error);
		}
		internal bool SubmitLivePoseTuningCandidate(
			string sourceAuthoringRevision,
			string candidateRevision,
			CharacterPoseTuningParameterBlock block,
			out string error)
		{
			CharacterSimulationActorRegistration registration = m_Registration;
			CharacterSimulationPresentationRuntime runtime =
				registration?.PresentationRuntime as CharacterSimulationPresentationRuntime;
			if (registration == null || runtime == null || block == null)
			{
				error = "Pose tuning requires an active Live Actor presentation runtime.";
				return false;
			}
			CharacterPresentationProjection projection = registration.Projection;
			if (projection.TuningLayout == null)
			{
				error = "Pose tuning payload is unavailable for this Live Actor.";
				return false;
			}
			return runtime.SubmitTuningCandidate(
				new CharacterPoseTuningCandidate(
					new CharacterPoseTuningTargetIdentity(
						registration.ActorId.Value,
						projection.ProgramId,
						projection.ProjectionRevision,
						projection.PosePlan.PlanHash,
						projection.Rig.RigId,
						projection.Rig.RigRevision,
						projection.TuningLayout.LayoutHash),
					sourceAuthoringRevision,
					candidateRevision,
					block),
				out error);
		}
		internal void ClearPoseTuningCandidate()
		{
			m_PreviewController?.ClearPoseTuningCandidate();
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)
				?.ClearPendingTuningCandidate();
		}
		public bool TrySetPreviewPoseWatchInterests(
			Guid sessionId,
			Guid ownerId,
			IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
			m_PreviewController != null &&
			m_PreviewController.TrySetPoseWatchInterests(sessionId, ownerId, interests);
		public void RemovePreviewPoseWatchInterests(Guid ownerId) =>
			m_PreviewController?.RemovePoseWatchInterests(ownerId);
		public override bool CanPreviewTimeline =>
			!Application.isPlaying &&
			m_Definition &&
			m_Definition.AnimationPresentationProfile &&
			m_Definition.SimulationProgram &&
			m_Definition.PresentationProjection &&
			m_Animancer &&
			m_Animancer.Animator &&
			m_AnimationRigBinding &&
			m_WorldBodyBinding &&
			m_RootHierarchy;
		public bool CanPreviewPoseGraph => CanPreviewTimeline;
		public override string PreviewStatus =>
			"Pose Graph preview uses the selected Host's formal Body fixture and Scene PhysicsScene. A missing World-Aware Binding is reported at the first world-aware stage.";
		public override bool TryGetAnimationSequencePreviewStatus(
			AnimationSequenceAsset sequence,
			out string status)
		{
			if (!CanPreviewTimeline)
			{
				status = "Sequence Preview is unavailable: the selected Host is missing its compiled Definition, Animancer, Rig Binding, Body fixture or Visual Root.";
				return false;
			}
			try
			{
				return EnsurePreviewController()
					.TryGetSequencePreviewStatus(sequence, out status);
			}
			catch (InvalidOperationException exception)
			{
				status = exception.Message;
				return false;
			}
		}

		public void BindSessionActor(SimulationSessionHost sessionHost, ActorId actorId)
		{
			if (m_Registration != null)
				throw new InvalidOperationException("Character Actor identity cannot change after registration.");
			if (!sessionHost || !actorId.IsValid || !m_WorldBodyBinding)
				throw new ArgumentException("Character Session Actor binding is incomplete.");
			m_SessionHost = sessionHost;
			m_ActorId = actorId.Value;
			m_WorldBodyBinding.BindSessionActor(actorId);
		}

#if UNITY_EDITOR
		public void ConfigureAnimationRigBinding(CharacterAnimationRigBinding animationRigBinding)
		{
			m_AnimationRigBinding = animationRigBinding
				? animationRigBinding
				: throw new ArgumentNullException(nameof(animationRigBinding));
		}

		public void SetRuntimeAuthoring(
			CharacterControlSource controlSource,
			CharacterPresentationRole presentationRole,
			ThirdPersonCameraController cameraRig)
		{
			m_ControlSource = controlSource ? controlSource :
				throw new ArgumentNullException(nameof(controlSource));
			m_PresentationRole = presentationRole;
			m_CameraRig = cameraRig;
		}
#endif

		public bool EnsureRegistration()
		{
			if (m_Registration != null)
				return true;
			if (!m_Definition)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit CharacterPipelineDefinition.", this);
				return false;
			}
			if (string.IsNullOrEmpty(ActorId))
			{
				Debug.LogError("CharacterPipelineHost requires an explicit ActorId.", this);
				return false;
			}
			if (!m_SessionHost)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit SimulationSessionHost.", this);
				return false;
			}
			if (!m_ControlSource)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit Character control source.", this);
				return false;
			}
			if (!Enum.IsDefined(typeof(CharacterPresentationRole), m_PresentationRole))
			{
				Debug.LogError("CharacterPipelineHost requires a valid Presentation role.", this);
				return false;
			}
			if (!m_Animancer)
			{
				Debug.LogError("CharacterPipelineHost requires an AnimancerComponent.", this);
				return false;
			}
			if (!m_AnimationRigBinding)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit Animation Rig Binding.", this);
				return false;
			}
			if (!m_WorldBodyBinding)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit Float32 World body binding.", this);
				return false;
			}
			try
			{
				m_WorldBodyBinding.RequireValid();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception, this);
				return false;
			}
			if (!m_RootHierarchy)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit Root Hierarchy Binding.", this);
				return false;
			}
			try
			{
				m_RootHierarchy.RequireValid();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception, this);
				return false;
			}
			if (!m_BodyPresentationProfile)
			{
				Debug.LogError("CharacterPipelineHost requires an explicit Body Presentation Profile.", this);
				return false;
			}
			if (m_Definition.EquipmentCapabilityEnabled && !m_EquipmentRigBindings)
			{
				Debug.LogError("Equipment-enabled CharacterPipelineHost requires an explicit Equipment Rig Binding Catalog.", this);
				return false;
			}
			if (m_Definition.EquipmentCapabilityEnabled)
			{
				try
				{
					m_EquipmentRigBindings.RequireValid();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception, this);
					return false;
				}
			}
			if (!m_WorldAwarePresentation)
			{
				Debug.LogError("Foot Placement Pose Graph requires an explicit World-Aware Presentation Binding.", this);
				return false;
			}
			if (!m_Animancer.Animator)
			{
				Debug.LogError("CharacterPipelineHost requires Animancer to reference a valid Animator.", this);
				return false;
			}
			if (m_Animancer.Animator.transform != m_RootHierarchy.PoseRoot)
			{
				Debug.LogError("CharacterPipelineHost requires Animancer Animator to use the formal PoseRoot.", this);
				return false;
			}
			if (m_WorldBodyBinding is UnityCharacterControllerWorldBodyBinding ccBinding &&
				m_RootHierarchy.LogicRoot != ccBinding.LogicRoot)
			{
				Debug.LogError("CharacterPipelineHost Root Hierarchy LogicRoot must match the World body binding LogicRoot.", this);
				return false;
			}
			if (m_PresentationRole == CharacterPresentationRole.LocalOwner &&
				(!m_CameraRig || !m_CameraFollowAnchor || !m_CameraAimAnchor || string.IsNullOrEmpty(m_CameraLookInputValueId)))
			{
				Debug.LogError("LocalOwner CharacterPipelineHost requires explicit camera rig, follow anchor, aim anchor, and look input id.", this);
				return false;
			}
			if (m_PresentationRole == CharacterPresentationRole.LocalOwner &&
				(!m_CameraFollowAnchor.IsChildOf(m_RootHierarchy.VisualRoot) && m_CameraFollowAnchor != m_RootHierarchy.VisualRoot ||
				 !m_CameraAimAnchor.IsChildOf(m_RootHierarchy.VisualRoot) && m_CameraAimAnchor != m_RootHierarchy.VisualRoot))
			{
				Debug.LogError("LocalOwner camera anchors must belong to the VisualRoot presentation subtree.", this);
				return false;
			}
			if (!m_Definition.SimulationProgram || !m_Definition.PresentationProjection)
			{
				Debug.LogError("CharacterPipelineHost requires compiled Program and Presentation Projection assets.", this);
				return false;
			}

			IUnityCharacterControlSourceRuntime inputAdapter = null;
			ICharacterPresentationRuntime presentationRuntime = null;
			RuntimeDiagnosticsTarget diagnosticsTarget = null;
			CharacterSimulationActorRegistration registration = null;
			try
			{
				var actorId = new ActorId(ActorId);
				if (m_WorldBodyBinding.ActorId != actorId)
					throw new InvalidOperationException("CharacterPipelineHost ActorId does not match its World body binding.");
				CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
				CharacterPresentationSemanticContract presentationContract =
					Float32CharacterPresentationContractAdapter.Create(program);
				CharacterPresentationProjection projection = m_Definition.PresentationProjection.Load(
					presentationContract);
				m_AnimationRigBinding.RequireValid(projection.Rig);
				CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(program);
				var diagnosticsContext = new RuntimeDiagnosticsContext(
					Guid.NewGuid(),
					Guid.NewGuid(),
					debugProgram.Revision,
					debugProgram.SourceMap,
					new RuntimeDiagnosticsStore());
				var diagnosticsAdapter = new CharacterSimulationDiagnosticsAdapter(diagnosticsContext, program);
				diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
				int tickRate = m_SessionHost.Composition
					? m_SessionHost.Composition.TickRate
					: throw new InvalidOperationException("SimulationSessionHost requires an explicit Composition Definition.");
				if (program.Manifest.TickRate != tickRate || m_Definition.SimulationTickRate != tickRate)
					throw new InvalidOperationException("Program, Character Definition, and Session Composition Tick rates must match exactly.");

				inputAdapter = m_ControlSource.Create(new CharacterControlSourceContext(this, m_Definition, program));
				if (inputAdapter == null)
					throw new InvalidOperationException("Character control source returned no input adapter.");
				WorldBodyState initialBody = m_WorldBodyBinding.InitialBody;
				PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
				CharacterPresentationRuntimeBinding presentationBinding;
				if (m_PresentationRole == CharacterPresentationRole.LocalOwner)
				{
					if (!(inputAdapter is ICharacterPresentationLookInput lookInput))
						throw new InvalidOperationException("LocalOwner control source must provide Presentation look input.");
					presentationBinding = CharacterPresentationRuntimeFactory.CreateLocalOwner(
						presentationContract,
						tickRate,
						projection,
						actorId,
						m_Animancer,
						m_AnimationRigBinding,
						m_RootHierarchy,
						CharacterPresentationBodyState.FromFloat32(initialBody),
						m_BodyPresentationProfile,
						m_WorldAwarePresentation,
						physicsScene,
						m_CameraRig,
						m_CameraFollowAnchor,
						m_CameraAimAnchor,
						m_CameraTargetBindings,
						lookInput,
						m_CameraLookInputValueId,
						m_EquipmentRigBindings,
						m_SessionHost,
						diagnosticsContext);
				}
				else
				{
					presentationBinding = CharacterPresentationRuntimeFactory.CreateSimulatedActor(
						presentationContract,
						tickRate,
						projection,
						actorId,
						m_Animancer,
						m_AnimationRigBinding,
						m_RootHierarchy,
						CharacterPresentationBodyState.FromFloat32(initialBody),
						m_BodyPresentationProfile,
						m_WorldAwarePresentation,
						physicsScene,
						m_EquipmentRigBindings,
						m_SessionHost,
						diagnosticsContext);
				}
				presentationRuntime = presentationBinding.Runtime;
				var gameplayOutput = new CharacterSimulationGameplayOutputBuffer();
				registration = new CharacterSimulationActorRegistration(
					GetInstanceID(),
					name,
					actorId,
					m_Definition.SimulationProgram,
					program,
					m_Definition.PresentationProjection,
					projection,
					presentationContract,
					m_WorldBodyBinding,
					initialBody,
					inputAdapter,
					gameplayOutput,
					presentationRuntime,
					diagnosticsAdapter,
					diagnosticsTarget,
					m_RootHierarchy.VisualRoot);
				inputAdapter = null;
				presentationRuntime = null;
				diagnosticsTarget = null;
				m_SessionHost.RegisterActor(registration);
				m_Registration = registration;
				registration = null;
				return true;
			}
			catch (Exception exception)
			{
				registration?.Dispose();
				presentationRuntime?.Dispose();
				inputAdapter?.Dispose();
				diagnosticsTarget?.Terminate();
				diagnosticsTarget?.Dispose();
				Debug.LogException(exception, this);
				return false;
			}
		}

		public override void EvaluateTimelinePreview(
			Guid sessionId,
			TimelineData timeline,
			float previousTime,
			float currentTime,
			string sourceId,
			string sourceName,
			ulong evaluationTick,
			float presentationDeltaSeconds,
			bool resetLifecycle)
		{
			if (sessionId == Guid.Empty || timeline == null || !CanPreviewTimeline)
			{
				ClearTimelinePreview(sessionId);
				return;
			}
			EnsurePreviewController().Evaluate(
				sessionId,
				timeline,
				previousTime,
				currentTime,
				sourceId,
				sourceName,
				evaluationTick,
				presentationDeltaSeconds,
				resetLifecycle);
		}

		public override void EvaluateAnimationSequencePreview(
			Guid sessionId,
			AnimationSequenceAsset sequence,
			float previousTime,
			float currentTime,
			ulong evaluationTick,
			float presentationDeltaSeconds,
			bool resetLifecycle)
		{
			if (sessionId == Guid.Empty || sequence == null || !CanPreviewTimeline)
			{
				ClearAnimationSequencePreview(sessionId);
				return;
			}
			EnsurePreviewController().EvaluateSequence(
				sessionId,
				sequence,
				currentTime,
				evaluationTick,
				presentationDeltaSeconds,
				resetLifecycle || currentTime + 0.000001f < previousTime);
		}

		public void EvaluatePoseGraphPreview(
			Guid sessionId,
			double presentationTime,
			ulong evaluationTick,
			float presentationDeltaSeconds,
			bool resetLifecycle,
			bool grounded,
			float horizontalSpeed,
			float horizontalAcceleration,
			float verticalSpeed,
			Vector2 movementDirection,
			Vector2 desiredDirection,
			float facingError,
			CharacterPresentationMotionPhase motionPhase,
			IReadOnlyList<PoseParameterId> directParameterIds = null,
			IReadOnlyList<float> directParameterValues = null)
		{
			if (sessionId == Guid.Empty || !CanPreviewPoseGraph)
			{
				ClearPoseGraphPreview(sessionId);
				return;
			}
			EnsurePreviewController().EvaluatePoseGraph(
				sessionId,
				presentationTime,
				evaluationTick,
				presentationDeltaSeconds,
				resetLifecycle,
				grounded,
				horizontalSpeed,
				horizontalAcceleration,
				verticalSpeed,
				movementDirection,
				desiredDirection,
				facingError,
				motionPhase,
				directParameterIds,
				directParameterValues);
		}

		public void ClearPoseGraphPreview(Guid sessionId)
		{
			m_PreviewController?.Clear(sessionId);
		}

		public void SetLinkedPosePreviewOverride(
			Guid sessionId,
			LinkedPoseGroupId groupId,
			LinkedPoseImplementationId implementationId)
		{
			if (sessionId == Guid.Empty || !CanPreviewPoseGraph)
				return;
			EnsurePreviewController().SetLinkedPosePreviewOverride(
				sessionId,
				groupId,
				implementationId);
		}

		public void ClearLinkedPosePreviewOverride(
			Guid sessionId,
			LinkedPoseGroupId groupId)
		{
			m_PreviewController?.ClearLinkedPosePreviewOverride(
				sessionId,
				groupId);
		}

		public void ClearLinkedPosePreviewOverrides(Guid sessionId)
		{
			m_PreviewController?.ClearLinkedPosePreviewOverrides(sessionId);
		}

		public override void ClearTimelinePreview(Guid sessionId)
		{
			m_PreviewController?.Clear(sessionId);
		}

		public override void ClearAnimationSequencePreview(Guid sessionId)
		{
			m_PreviewController?.Clear(sessionId);
		}

		public override void CollectAnimationMarkerSyncPreviewSources(
			TimelineData timeline,
			string targetTrackAuthoringId,
			List<TimelineAnimationMarkerSyncPreviewCandidate> destination)
		{
			EnsurePreviewController().CollectMarkerSyncSources(timeline, targetTrackAuthoringId, destination);
		}

		public override void ConfigureAnimationMarkerSyncPreviewSource(
			Guid sessionId,
			string targetTimelineAuthoringId,
			string targetTrackAuthoringId,
			string sourceTimelineAuthoringId,
			string sourceTrackAuthoringId)
		{
			EnsurePreviewController().ConfigureMarkerSyncSource(
				sessionId,
				targetTimelineAuthoringId,
				targetTrackAuthoringId,
				sourceTimelineAuthoringId,
				sourceTrackAuthoringId);
		}

		public override bool TryGetAnimationMarkerSyncPreviewState(
			Guid sessionId,
			string targetTrackAuthoringId,
			out TimelineAnimationMarkerSyncPreviewState state)
		{
			if (m_PreviewController != null)
				return m_PreviewController.TryGetMarkerSyncPreviewState(sessionId, targetTrackAuthoringId, out state);
			state = default;
			return false;
		}

		void Awake()
		{
			if (!Application.isPlaying)
				return;
			ClearAllTimelinePreviews();
			EnsureRegistration();
		}

		void Reset()
		{
			m_Animancer = GetComponent<AnimancerComponent>();
		}

		void OnValidate()
		{
			if (!m_Animancer)
				m_Animancer = GetComponent<AnimancerComponent>();
		}

		void OnEnable()
		{
			if (!Application.isPlaying)
				return;
			EnsureRegistration();
		}

		void OnDisable()
		{
			ClearAllTimelinePreviews();
			DisposeRegistration();
		}

		void OnDestroy()
		{
			ClearAllTimelinePreviews();
			DisposeRegistration();
		}

		void DisposeRegistration()
		{
			if (m_Registration == null)
				return;
			CharacterSimulationActorRegistration registration = m_Registration;
			m_Registration = null;
			if (m_SessionHost)
			{
				m_SessionHost.Stop();
				m_SessionHost.ReleaseActor(registration);
			}
			else
				registration.Dispose();
		}

		CharacterPipelinePreviewController EnsurePreviewController()
		{
			if (m_PreviewController != null && m_PreviewController.Matches(m_Definition, m_Animancer))
				return m_PreviewController;
			ClearAllTimelinePreviews();
			m_PreviewController = new CharacterPipelinePreviewController(this);
			return m_PreviewController;
		}

		void ClearAllTimelinePreviews()
		{
			m_PreviewController?.Dispose();
			m_PreviewController = null;
		}
	}
}
