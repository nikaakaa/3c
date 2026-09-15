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
	public sealed class CharacterPipelineHost : MonoBehaviour, ISimulationSessionActorHost
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
		[SerializeField] CinemachineCameraRigAdapter m_CameraRig;
		[SerializeField] Transform m_CameraFollowAnchor;
		[SerializeField] Transform m_CameraAimAnchor;
		[SerializeField] List<CameraTargetBinding> m_CameraTargetBindings = new List<CameraTargetBinding>();
		[SerializeField] string m_CameraLookInputValueId;

		Float32CharacterRegistration m_Registration;

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
		public CinemachineCameraRigAdapter CameraRig => m_CameraRig;
		public Transform CameraFollowAnchor => m_CameraFollowAnchor;
		public Transform CameraAimAnchor => m_CameraAimAnchor;
		public IReadOnlyList<CameraTargetBinding> CameraTargetBindings => m_CameraTargetBindings;
		public string CameraLookInputValueId => string.IsNullOrWhiteSpace(m_CameraLookInputValueId)
			? string.Empty
			: m_CameraLookInputValueId.Trim();
		public Float32CharacterRegistration Registration => m_Registration;
		internal CharacterPoseTuningLayout LiveTuningLayout =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.TuningLayout;
		internal CharacterPoseTuningParameterBlock LiveActiveTuningBlock =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.ActiveTuningBlock;
		internal CharacterPoseTuningRuntimeState LiveTuningState =>
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)?.TuningState ?? default;
		internal bool SubmitLivePoseTuningCandidate(
			string sourceAuthoringRevision,
			string candidateRevision,
			CharacterPoseTuningParameterBlock block,
			out string error)
		{
			Float32CharacterRegistration registration = m_Registration;
			CharacterSimulationPresentationRuntime runtime =
				registration?.PresentationRuntime as CharacterSimulationPresentationRuntime;
			if (registration == null || runtime == null || block == null)
			{
				error = "Pose tuning requires an active Live Actor presentation runtime.";
				return false;
			}
			CharacterPresentationProjection projection = registration.PresentationProjection;
			if (projection.TuningLayout == null)
			{
				error = "Pose tuning payload is unavailable for this Live Actor.";
				return false;
			}
			return runtime.SubmitTuningCandidate(
				new CharacterPoseTuningCandidate(
					new CharacterPoseTuningTargetIdentity(
						registration.ActorId.Value,
						projection.PresentationId,
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
			(m_Registration?.PresentationRuntime as CharacterSimulationPresentationRuntime)
				?.ClearPendingTuningCandidate();
		}

		CharacterPresentationRuntimeBinding CreatePresentationRuntime(
			CharacterPresentationProjection projection,
			ActorId actorId,
			WorldBodyState initialBody,
			IUnityCharacterControlSourceRuntime inputAdapter,
			PhysicsScene physicsScene,
			RuntimeDiagnosticsContext diagnostics,
			int tickRate,
			bool initializeExternalState)
		{
			CharacterPresentationBodyState presentationBody =
				CharacterPresentationBodyState.FromFloat32(initialBody);
			if (m_Registration?.PresentationRuntime is CharacterSimulationPresentationRuntime current &&
				current.TryGetLatestBody(out CharacterPresentationBodyState currentBody))
			{
				presentationBody = currentBody;
			}
			if (m_PresentationRole == CharacterPresentationRole.LocalOwner)
			{
				if (!(inputAdapter is ICharacterPresentationLookInput lookInput))
					throw new InvalidOperationException("LocalOwner control source must provide Presentation look input.");
				return CharacterPresentationRuntimeFactory.CreateLocalOwner(
					tickRate,
					projection,
					actorId,
					m_Animancer,
					m_AnimationRigBinding,
					m_RootHierarchy,
					presentationBody,
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
					diagnostics,
					initializeExternalState);
			}
			return CharacterPresentationRuntimeFactory.CreateSimulatedActor(
				tickRate,
				projection,
				actorId,
				m_Animancer,
				m_AnimationRigBinding,
				m_RootHierarchy,
				presentationBody,
				m_BodyPresentationProfile,
				m_WorldAwarePresentation,
				physicsScene,
				m_EquipmentRigBindings,
				m_SessionHost,
				diagnostics,
				initializeExternalState);
		}

		public bool TryQueueInputRequest(string requestId, out ulong requestSequence, out string error)
		{
			requestSequence = 0;
			if (m_Registration == null)
			{
				error = "Character Actor registration is not active.";
				return false;
			}
			if (!(m_Registration.LocalControlSource is ICharacterControlSourceInputRequestRuntime input))
			{
				error = $"Character Control Source '{m_ControlSource.SourceIdentity}' does not expose the input request port.";
				return false;
			}
			return input.TryQueueInputRequest(requestId, out requestSequence, out error);
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
			CinemachineCameraRigAdapter cameraRig)
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
			if (!m_Definition.PresentationProjection)
			{
				Debug.LogError("CharacterPipelineHost requires a Character Presentation Projection asset.", this);
				return false;
			}

			IUnityCharacterControlSourceRuntime inputAdapter = null;
			ICharacterPresentationRuntime presentationRuntime = null;
			RuntimeDiagnosticsTarget diagnosticsTarget = null;
			Float32CharacterRegistration registration = null;
			try
			{
				var actorId = new ActorId(ActorId);
				if (m_WorldBodyBinding.ActorId != actorId)
					throw new InvalidOperationException("CharacterPipelineHost ActorId does not match its World body binding.");
				int tickRate = m_SessionHost.Composition
					? m_SessionHost.Composition.TickRate
					: throw new InvalidOperationException("SimulationSessionHost requires an explicit Composition Definition.");
				if (m_Definition.SimulationTickRate != tickRate)
					throw new InvalidOperationException("Character Definition and Session Composition Tick rates must match exactly.");
				CharacterControlModuleCatalog controlModules = CharacterControlRuntimeModuleCatalog.Create();
				CharacterControlRuntimeBinding controlRuntimeBinding = m_Definition.BuildControlRuntimeBinding(controlModules);
				CharacterControlModuleContract controlModule = controlModules.RequireContract(controlRuntimeBinding.ModuleId);
				CharacterBodyMotionBinding bodyMotionBinding = m_Definition.BuildBodyMotionRuntimeBinding();
				CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding = m_Definition.BuildGameplayEffectRuntimeBinding();
				CharacterEquipmentRuntimeBinding equipmentRuntimeBinding = m_Definition.BuildEquipmentRuntimeBinding();
				GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData =
					m_Definition.LoadFloat32AbilitySet();
				SimulationActorBinding actorBinding = new SimulationActorBinding(
					actorId,
					m_WorldBodyBinding.BindingId,
					controlRuntimeBinding,
					bodyMotionBinding,
					gameplayEffectRuntimeBinding,
					equipmentRuntimeBinding,
					abilityData);
				SimulationExecutionTargetManifest target = Float32SimulationTarget.Manifest.ExecutionTarget;
				Float32CharacterRuntime characterRuntime = new Float32CharacterRuntime(
					new[] { actorBinding },
					target.NumericProfile,
					tickRate,
					target.OperationSetVersion,
					controlModules);
				CharacterPresentationProjection projection = CharacterPresentationRuntimeFactory.LoadProjection(
					m_Definition.PresentationProjection);
				m_AnimationRigBinding.RequireValid(projection.Rig);
				RuntimeContentRevision diagnosticsRevision = new RuntimeContentRevision(
					$"float32-character-runtime/{actorId.Value}",
					characterRuntime.AbilitySetSourceRevision,
					characterRuntime.GameplayContentHash.ToString());
				var debugSourceMap = new DebugSourceMap(diagnosticsRevision);
				var diagnosticsContext = new RuntimeDiagnosticsContext(
					Guid.NewGuid(),
					Guid.NewGuid(),
					diagnosticsRevision,
					debugSourceMap,
					new RuntimeDiagnosticsStore());
				diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
				inputAdapter = m_ControlSource.Create(new CharacterControlSourceContext(this, m_Definition, controlModule));
				if (inputAdapter == null)
					throw new InvalidOperationException("Character control source returned no input adapter.");
				WorldBodyState initialBody = m_WorldBodyBinding.InitialBody;
				PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
				CharacterPresentationRuntimeBinding presentationBinding = CreatePresentationRuntime(
					projection,
					actorId,
					initialBody,
					inputAdapter,
					physicsScene,
					diagnosticsContext,
					tickRate,
					true);
				presentationRuntime = presentationBinding.Runtime;
				var gameplayOutput = new CharacterSimulationGameplayOutputBuffer();
				registration = new Float32CharacterRegistration(
					GetInstanceID(),
					name,
					actorId,
					characterRuntime,
					actorBinding,
					projection,
					m_WorldBodyBinding,
					initialBody,
					inputAdapter,
					gameplayOutput,
					presentationRuntime,
					ThirdPersonSimulation.NullSimulationDiagnosticsSink.Instance,
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

		void Awake()
		{
			if (!Application.isPlaying)
				return;
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
			DisposeRegistration();
		}

		void OnDestroy()
		{
			DisposeRegistration();
		}

		void DisposeRegistration()
		{
			if (m_Registration == null)
				return;
			Float32CharacterRegistration registration = m_Registration;
			m_Registration = null;
			if (m_SessionHost)
			{
				m_SessionHost.Stop();
				m_SessionHost.ReleaseActor(registration);
			}
			else
				registration.Dispose();
		}

	}
}
