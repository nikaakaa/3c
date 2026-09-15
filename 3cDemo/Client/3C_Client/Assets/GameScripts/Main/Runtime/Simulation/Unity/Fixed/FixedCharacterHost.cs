using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using UnityEngine.SceneManagement;
using FixedSimulationActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using FixedWorldCollisionSummary = ThirdPersonSimulation.Fixed.WorldCollisionSummary;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class FixedCharacterHost : MonoBehaviour, ISimulationSessionActorHost
    {
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] FixedCharacterControlSource m_ControlSource;
        [SerializeField] CharacterPresentationRole m_PresentationRole = CharacterPresentationRole.SimulatedActor;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] string m_WorldBodyBindingId = string.Empty;
        [SerializeField] CharacterRootHierarchyBinding m_RootHierarchy;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
        [SerializeField] CharacterWorldAwarePresentationBinding m_WorldAwarePresentation;
        [SerializeField] CharacterEquipmentRigBindingCatalog m_EquipmentRigBindings;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] CharacterAnimationRigBinding m_AnimationRigBinding;
        [SerializeField] CinemachineCameraRigAdapter m_CameraRig;
        [SerializeField] Transform m_CameraFollowAnchor;
        [SerializeField] Transform m_CameraAimAnchor;
        [SerializeField] List<CameraTargetBinding> m_CameraTargetBindings = new List<CameraTargetBinding>();
        [SerializeField] string m_CameraLookInputValueId = string.Empty;
        [SerializeField, Min(1)] int m_MaximumActivePresentationRecords = 128;

        FixedCharacterRegistration m_Registration;

        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));
        public ActorId SimulationActorId => ActorId;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public CharacterPipelineDefinition CharacterDefinition => m_CharacterDefinition;
        public CharacterPresentationProjectionAsset ProjectionAsset =>
            m_CharacterDefinition ? m_CharacterDefinition.PresentationProjection : null;
        public FixedCharacterControlSource ControlSource => m_ControlSource;
        public CinemachineCameraRigAdapter CameraRig => m_CameraRig;
        public CharacterPresentationRole PresentationRole => m_PresentationRole;
        public ICharacterPresentationRuntime PresentationRuntime => m_Registration?.PresentationRuntime;
        public FixedCharacterRegistration Registration => m_Registration;
        public CharacterRootHierarchyBinding RootHierarchy => m_RootHierarchy;
        public Vector3 VisualPosition => m_RootHierarchy
            ? m_RootHierarchy.VisualRoot.position
            : throw new InvalidOperationException("Fixed Character Host requires a Root Hierarchy Binding.");
        public bool TryGetInitialBody(out FixedWorldBodyState body)
        {
            if (m_Registration == null)
            {
                body = default;
                return false;
            }
            body = m_Registration.InitialBody;
            return true;
        }

#if UNITY_EDITOR
        public void SetProfileAuthoring(
            CharacterPipelineDefinition characterDefinition,
            FixedCharacterControlSource controlSource,
            CharacterPresentationRole presentationRole,
            ActorId actorId,
            string worldBodyBindingId,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterBodyPresentationProfile bodyPresentationProfile,
            CharacterWorldAwarePresentationBinding worldAwarePresentation,
            CharacterEquipmentRigBindingCatalog equipmentRigBindings,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            Transform cameraFollowAnchor,
            Transform cameraAimAnchor,
            IEnumerable<CameraTargetBinding> cameraTargetBindings,
            string cameraLookInputValueId,
            int maximumActivePresentationRecords)
        {
            m_SessionHost = null;
            m_CharacterDefinition = characterDefinition ? characterDefinition : throw new ArgumentNullException(nameof(characterDefinition));
            m_ControlSource = controlSource ? controlSource : throw new ArgumentNullException(nameof(controlSource));
            m_PresentationRole = presentationRole;
            m_ActorId = actorId.IsValid ? actorId.Value : throw new ArgumentException("ActorId is invalid.", nameof(actorId));
            m_WorldBodyBindingId = Require(worldBodyBindingId, nameof(worldBodyBindingId));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            m_BodyPresentationProfile = bodyPresentationProfile ? bodyPresentationProfile :
                throw new ArgumentNullException(nameof(bodyPresentationProfile));
            m_WorldAwarePresentation = worldAwarePresentation ? worldAwarePresentation : throw new ArgumentNullException(nameof(worldAwarePresentation));
            m_EquipmentRigBindings = equipmentRigBindings;
            m_Animancer = animancer ? animancer : throw new ArgumentNullException(nameof(animancer));
            m_AnimationRigBinding = animationRigBinding
                ? animationRigBinding
                : throw new ArgumentNullException(nameof(animationRigBinding));
            m_CameraRig = null;
            m_CameraFollowAnchor = cameraFollowAnchor;
            m_CameraAimAnchor = cameraAimAnchor;
            m_CameraTargetBindings = cameraTargetBindings == null
                ? new List<CameraTargetBinding>()
                : new List<CameraTargetBinding>(cameraTargetBindings);
            m_CameraLookInputValueId = string.IsNullOrWhiteSpace(cameraLookInputValueId)
                ? string.Empty
                : cameraLookInputValueId.Trim();
            m_MaximumActivePresentationRecords = RequirePositive(
                maximumActivePresentationRecords,
                nameof(maximumActivePresentationRecords));
        }

        public void SetSceneAuthoring(
            SimulationSessionHost sessionHost,
            CinemachineCameraRigAdapter cameraRig)
        {
            m_SessionHost = sessionHost ? sessionHost : throw new ArgumentNullException(nameof(sessionHost));
            if (m_PresentationRole == CharacterPresentationRole.LocalOwner)
                m_CameraRig = cameraRig ? cameraRig : throw new ArgumentNullException(nameof(cameraRig));
            else if (cameraRig)
                throw new ArgumentException("Simulated Fixed Character cannot receive a Camera Rig.", nameof(cameraRig));
            else
                m_CameraRig = null;
        }
#endif

        void OnEnable()
        {
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

        void EnsureRegistration()
        {
            if (m_Registration != null)
                return;
            SimulationSessionHost sessionHost = m_SessionHost ? m_SessionHost :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a SimulationSessionHost.");
            FixedCharacterControlSource controlSourceDefinition = m_ControlSource ? m_ControlSource :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a formal Fixed Control Source.");
            CharacterRootHierarchyBinding rootHierarchy = m_RootHierarchy ? m_RootHierarchy :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Root Hierarchy Binding.");
            rootHierarchy.RequireValid();
            CharacterBodyPresentationProfile bodyPresentationProfile = m_BodyPresentationProfile ? m_BodyPresentationProfile :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Body Presentation Profile.");
            CharacterWorldAwarePresentationBinding worldAwarePresentation = m_WorldAwarePresentation ? m_WorldAwarePresentation :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a World-Aware Presentation Binding.");
            AnimancerComponent animancer = m_Animancer ? m_Animancer :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires an AnimancerComponent.");
            CharacterAnimationRigBinding animationRigBinding = m_AnimationRigBinding
                ? m_AnimationRigBinding
                : throw new InvalidOperationException($"Fixed Character Host '{name}' requires an Animation Rig Binding.");
            CharacterPipelineDefinition characterDefinition = m_CharacterDefinition ? m_CharacterDefinition :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Character Pipeline Definition.");
            CharacterPresentationProjectionAsset projectionAsset = characterDefinition.PresentationProjection ?
                characterDefinition.PresentationProjection :
                throw new InvalidOperationException($"Fixed Character Host '{name}' Definition requires a Presentation Projection asset.");
            ActorId actorId = ActorId;
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int tickRate = sessionHost.Composition
                ? sessionHost.Composition.TickRate
                : throw new InvalidOperationException($"Fixed Character Host '{name}' requires an explicit Composition Definition.");
            if (characterDefinition.SimulationTickRate != tickRate)
                throw new InvalidOperationException($"Fixed Character Host '{name}' Definition and Session Composition TickRate must match.");
            CharacterControlModuleCatalog controlModules = CharacterControlRuntimeModuleCatalog.Create();
            CharacterControlRuntimeBinding controlRuntimeBinding = characterDefinition.BuildControlRuntimeBinding(controlModules);
            CharacterControlModuleContract controlModule = controlModules.RequireContract(controlRuntimeBinding.ModuleId);
            CharacterBodyMotionBinding bodyMotionBinding = characterDefinition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding = characterDefinition.BuildGameplayEffectRuntimeBinding();
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding = characterDefinition.BuildEquipmentRuntimeBinding();
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilityData =
                characterDefinition.LoadFixedAbilitySet();
            FixedSimulationActorBinding actorBinding = new FixedSimulationActorBinding(
                actorId,
                Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                abilityData);
            SimulationExecutionTargetManifest target = FixedSimulationTarget.Manifest.ExecutionTarget;
            FixedCharacterRuntime characterRuntime = new FixedCharacterRuntime(
                new[] { actorBinding },
                target.NumericProfile,
                tickRate,
                target.OperationSetVersion,
                controlModules);
            IUnityFixedCharacterControlSourceRuntime controlSource = null;
            ICharacterPresentationRuntime presentation = null;
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            FixedCharacterRegistration registration = null;
            try
            {
                if (animancer.Animator.transform != rootHierarchy.PoseRoot)
                    throw new InvalidOperationException($"Fixed Character Host '{name}' Animancer Animator must use PoseRoot.");
                if (worldAwarePresentation.PresentationRoot != rootHierarchy.VisualRoot ||
                    worldAwarePresentation.SelfColliderRoot != rootHierarchy.LogicRoot)
                {
                    throw new InvalidOperationException($"Fixed Character Host '{name}' World-Aware binding must match its Root Hierarchy.");
                }
                FixedWorldBodyState initialBody =
                    FixedCharacterInputTraceModule.ResolveInitialBody(
                        BuildInitialBody(actorId, rootHierarchy.LogicRoot));
                CharacterPresentationBodyState presentationBody = FixedUnityPresentationBoundary.Convert(initialBody);
                RuntimeContentRevision diagnosticsRevision = new RuntimeContentRevision(
                    $"fixed-character-runtime/{actorId.Value}",
                    characterRuntime.AbilitySetSourceRevision,
                    characterRuntime.GameplayContentHash.ToString());
                var debugSourceMap = new DebugSourceMap(diagnosticsRevision);
                var diagnosticsStore = new RuntimeDiagnosticsStore();
                CharacterPipelineTraceCommandLine.Enable(diagnosticsStore);
                var diagnosticsContext = new RuntimeDiagnosticsContext(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    diagnosticsRevision,
                    debugSourceMap,
                    diagnosticsStore);
                diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
                CharacterPresentationProjection projection = CharacterPresentationRuntimeFactory.LoadProjection(
                    projectionAsset);
                animationRigBinding.RequireValid(projection.Rig);
                if (projection.EquipmentVisualBindings.Count != 0 && !m_EquipmentRigBindings)
                    throw new InvalidOperationException($"Fixed Character Host '{name}' requires an Equipment Rig Binding Catalog.");
                if (projection.EquipmentVisualBindings.Count != 0)
                    m_EquipmentRigBindings.RequireValid();
                controlSource = controlSourceDefinition.Create(
                    new FixedCharacterControlSourceContext(this, characterDefinition, controlModule));
                IUnityFixedCharacterControlSourceRuntime presentationControlSource = controlSource;
                CharacterPresentationRuntimeBinding presentationBinding;
                presentationBinding = CreatePresentationRuntime(
                    projection,
                    actorId,
                    presentationBody,
                    presentationControlSource,
                    physicsScene,
                    diagnosticsContext,
                    tickRate,
                    true);
                presentation = presentationBinding.Runtime;
                var presentationOutput = new FixedUnityPresentationOutputAdapter(
                    actorId,
                    presentationBinding.Projection,
                    presentation,
                    RequirePositive(m_MaximumActivePresentationRecords, nameof(m_MaximumActivePresentationRecords)));
                registration = new FixedCharacterRegistration(
                    GetInstanceID(),
                    name,
                    actorId,
                    characterRuntime,
                    actorBinding,
                    new AnimationPresentationIdentity(projection),
                    Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                    initialBody,
                    controlSource,
                    presentationOutput,
                    presentation,
                    rootHierarchy,
                    diagnosticsContext,
                    diagnosticsTarget,
                    m_MaximumActivePresentationRecords);
                controlSource = null;
                presentation = null;
                diagnosticsTarget = null;
                sessionHost.RegisterActor(registration);
                m_Registration = registration;
                registration = null;
            }
            catch
            {
                registration?.Dispose();
                diagnosticsTarget?.Dispose();
                presentation?.Dispose();
                controlSource?.Dispose();
                throw;
            }
        }

        CharacterPresentationRuntimeBinding CreatePresentationRuntime(
            CharacterPresentationProjection projection,
            ActorId actorId,
            CharacterPresentationBodyState initialPresentationBody,
            IUnityFixedCharacterControlSourceRuntime controlSource,
            PhysicsScene physicsScene,
            RuntimeDiagnosticsContext diagnostics,
            int tickRate,
            bool initializeExternalState)
        {
            CharacterPresentationBodyState presentationBody = initialPresentationBody;
            if (m_Registration?.PresentationRuntime is CharacterSimulationPresentationRuntime current &&
                current.TryGetLatestBody(out CharacterPresentationBodyState currentBody))
            {
                presentationBody = currentBody;
            }
            switch (m_PresentationRole)
            {
                case CharacterPresentationRole.LocalOwner:
                {
                    CinemachineCameraRigAdapter cameraRig = m_CameraRig ? m_CameraRig :
                        throw new InvalidOperationException($"Local Fixed Character Host '{name}' requires a Camera Rig.");
                    if (!m_CameraFollowAnchor || !m_CameraAimAnchor)
                        throw new InvalidOperationException($"Local Fixed Character Host '{name}' requires camera follow and aim anchors.");
                    if (m_CameraFollowAnchor != m_RootHierarchy.VisualRoot &&
                        !m_CameraFollowAnchor.IsChildOf(m_RootHierarchy.VisualRoot) ||
                        m_CameraAimAnchor != m_RootHierarchy.VisualRoot &&
                        !m_CameraAimAnchor.IsChildOf(m_RootHierarchy.VisualRoot))
                    {
                        throw new InvalidOperationException($"Local Fixed Character Host '{name}' camera anchors must belong to VisualRoot.");
                    }
                    if (controlSource is not ICharacterPresentationLookInput lookInput)
                        throw new InvalidOperationException($"Local Fixed Character Host '{name}' Control Source has no look input contract.");
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
                        cameraRig,
                        m_CameraFollowAnchor,
                        m_CameraAimAnchor,
                        m_CameraTargetBindings,
                        lookInput,
                        Require(m_CameraLookInputValueId, nameof(m_CameraLookInputValueId)),
                        m_EquipmentRigBindings,
                        m_SessionHost,
                        diagnostics,
                        initializeExternalState);
                }
                case CharacterPresentationRole.SimulatedActor:
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
                default:
                    throw new InvalidOperationException($"Fixed Character Host '{name}' has an invalid Presentation Role.");
            }
        }

        void DisposeRegistration()
        {
            if (m_Registration == null)
                return;
            FixedCharacterRegistration registration = m_Registration;
            m_Registration = null;
            if (m_SessionHost)
            {
                m_SessionHost.Stop();
                m_SessionHost.ReleaseActor(registration);
            }
            else
            {
                registration.Dispose();
            }
        }

        static FixedWorldBodyState BuildInitialBody(ActorId actorId, Transform spawn)
        {
            Vector3 position = spawn.position;
            return new FixedWorldBodyState(
                actorId,
                new FixedVector3(
                    FixedScalar.FromSingle(position.x),
                    FixedScalar.FromSingle(position.y),
                    FixedScalar.FromSingle(position.z)),
                new FixedYaw(FixedScalar.FromSingle(spawn.eulerAngles.y)),
                FixedVector3.Zero,
                FixedScalar.Zero,
                true,
                FixedWorldCollisionSummary.Below);
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Fixed Character Host requires an explicit '{field}'.");
            return value;
        }

        static int RequirePositive(int value, string field)
        {
            return value > 0
                ? value
                : throw new InvalidOperationException($"Fixed Character Host requires a positive '{field}'.");
        }
    }
}
