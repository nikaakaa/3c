using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using UnityEngine.SceneManagement;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using FixedWorldCollisionSummary = ThirdPersonSimulation.Fixed.WorldCollisionSummary;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class DeterministicRollbackCharacterHost : MonoBehaviour, ISimulationSessionActorHost
    {
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] RollbackEndpointAuthoringDefinition m_Endpoint;
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] string m_WorldBodyBindingId = string.Empty;
        [SerializeField] CharacterRootHierarchyBinding m_RootHierarchy;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
        [SerializeField] CharacterWorldAwarePresentationBinding m_WorldAwarePresentation;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] CharacterAnimationRigBinding m_AnimationRigBinding;
        [SerializeField] CinemachineCameraRigAdapter m_CameraRig;
        [SerializeField] Transform m_CameraFollowAnchor;
        [SerializeField] Transform m_CameraAimAnchor;
        [SerializeField] List<CameraTargetBinding> m_CameraTargetBindings = new List<CameraTargetBinding>();
        [SerializeField] string m_CameraLookInputValueId = string.Empty;
        [SerializeField, Min(1)] int m_MaximumActivePresentationRecords = 128;

        DeterministicRollbackCharacterRegistration m_Registration;

        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));
        public ActorId SimulationActorId => ActorId;
        public bool IsLocalActor => m_Endpoint && m_Endpoint.ResolvePeerProfile().ActorId == ActorId;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public CharacterPipelineDefinition CharacterDefinition => m_CharacterDefinition;
        public string WorldBodyBindingId => string.IsNullOrWhiteSpace(m_WorldBodyBindingId)
            ? string.Empty
            : m_WorldBodyBindingId.Trim();
        public CharacterRootHierarchyBinding RootHierarchy => m_RootHierarchy;
        public Vector3 VisualPosition => m_RootHierarchy
            ? m_RootHierarchy.VisualRoot.position
            : throw new InvalidOperationException("Rollback Character Host requires a Root Hierarchy Binding.");
        public Transform VisualRoot => m_RootHierarchy ? m_RootHierarchy.VisualRoot : null;
        public CharacterWorldAwarePresentationBinding WorldAwarePresentation => m_WorldAwarePresentation;
        public AnimancerComponent Animancer => m_Animancer;
        public CharacterAnimationRigBinding AnimationRigBinding => m_AnimationRigBinding;
        public Transform CameraFollowAnchor => m_CameraFollowAnchor;
        public Transform CameraAimAnchor => m_CameraAimAnchor;

        public void ConfigureAnimationRigBinding(CharacterAnimationRigBinding binding)
        {
            m_AnimationRigBinding = binding ? binding : throw new ArgumentNullException(nameof(binding));
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            SimulationSessionHost sessionHost,
            RollbackEndpointAuthoringDefinition endpoint,
            CharacterPipelineDefinition characterDefinition,
            string actorId,
            string worldBodyBindingId,
            CharacterRootHierarchyBinding rootHierarchy,
            CinemachineCameraRigAdapter cameraRig,
            string cameraLookInputValueId)
        {
            m_SessionHost = sessionHost ? sessionHost : throw new ArgumentNullException(nameof(sessionHost));
            m_Endpoint = endpoint ? endpoint : throw new ArgumentNullException(nameof(endpoint));
            m_CharacterDefinition = characterDefinition ? characterDefinition : throw new ArgumentNullException(nameof(characterDefinition));
            m_ActorId = Require(actorId, nameof(actorId));
            m_WorldBodyBindingId = Require(worldBodyBindingId, nameof(worldBodyBindingId));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            m_CameraRig = cameraRig ? cameraRig : throw new ArgumentNullException(nameof(cameraRig));
            m_CameraLookInputValueId = Require(cameraLookInputValueId, nameof(cameraLookInputValueId));
        }
#endif

        public bool TryGetRuntimeDiagnostics(out RollbackRuntimeDiagnosticsSnapshot snapshot)
        {
            if (m_Registration != null)
                return m_Registration.TryGetRuntimeDiagnostics(out snapshot);
            snapshot = default;
            return false;
        }

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
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a SimulationSessionHost.");
            RollbackEndpointAuthoringDefinition endpoint = m_Endpoint ? m_Endpoint :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires an Endpoint Definition.");
            CharacterPipelineDefinition characterDefinition = m_CharacterDefinition ? m_CharacterDefinition :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a Character Pipeline Definition.");
            CharacterPresentationProjectionAsset projectionAsset = characterDefinition.PresentationProjection ?
                characterDefinition.PresentationProjection :
                throw new InvalidOperationException($"Rollback Character Host '{name}' Definition requires a Presentation Projection asset.");
            CharacterInputProfile inputProfile = characterDefinition.InputProfile ? characterDefinition.InputProfile :
                throw new InvalidOperationException($"Rollback Character Host '{name}' Definition requires an Input Profile.");
            CharacterRootHierarchyBinding rootHierarchy = m_RootHierarchy ? m_RootHierarchy :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a Root Hierarchy Binding.");
            rootHierarchy.RequireValid();
            CharacterBodyPresentationProfile bodyPresentationProfile = m_BodyPresentationProfile ? m_BodyPresentationProfile :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a Body Presentation Profile.");
            CharacterWorldAwarePresentationBinding worldAwarePresentation = m_WorldAwarePresentation ? m_WorldAwarePresentation :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a World-Aware Presentation Binding.");
            AnimancerComponent animancer = m_Animancer ? m_Animancer :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires an AnimancerComponent.");
            CharacterAnimationRigBinding animationRigBinding = m_AnimationRigBinding
                ? m_AnimationRigBinding
                : throw new InvalidOperationException($"Rollback Character Host '{name}' requires an Animation Rig Binding.");
            ActorId actorId = ActorId;
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int tickRate = sessionHost.Composition
                ? sessionHost.Composition.TickRate
                : throw new InvalidOperationException($"Rollback Character Host '{name}' requires an explicit Composition Definition.");
            if (characterDefinition.SimulationTickRate != tickRate)
                throw new InvalidOperationException($"Rollback Character Host '{name}' Definition and Session Composition TickRate must match.");
            CharacterControlModuleCatalog controlModules = CharacterControlRuntimeModuleCatalog.Create();
            CharacterControlRuntimeBinding controlRuntimeBinding = characterDefinition.BuildControlRuntimeBinding(controlModules);
            CharacterControlModuleContract controlModule = controlModules.RequireContract(controlRuntimeBinding.ModuleId);
            CharacterBodyMotionBinding bodyMotionBinding = characterDefinition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding = characterDefinition.BuildGameplayEffectRuntimeBinding();
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding = characterDefinition.BuildEquipmentRuntimeBinding();
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilityData =
                characterDefinition.LoadFixedCharacterAbilities();
            FixedSimulationActorBinding characterBinding = new FixedSimulationActorBinding(
                actorId,
                Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                abilityData);
            FixedCharacterRuntime characterRuntime = FixedCharacterRuntime.Create(
                new[] { characterBinding },
                tickRate,
                controlModules);
            bool local = endpoint.ResolvePeerProfile().ActorId == actorId;
            UnityFixedCharacterInputAdapter input = null;
            ICharacterPresentationRuntime presentation = null;
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            DeterministicRollbackCharacterRegistration registration = null;
            try
            {
                if (animancer.Animator.transform != rootHierarchy.PoseRoot)
                    throw new InvalidOperationException($"Rollback Character Host '{name}' Animancer Animator must use PoseRoot.");
                if (worldAwarePresentation.PresentationRoot != rootHierarchy.VisualRoot ||
                    worldAwarePresentation.SelfColliderRoot != rootHierarchy.LogicRoot)
                {
                    throw new InvalidOperationException($"Rollback Character Host '{name}' World-Aware binding must match its Root Hierarchy.");
                }
                FixedWorldBodyState initialBody = BuildInitialBody(actorId, rootHierarchy.LogicRoot);
                CharacterPresentationBodyState presentationBody = FixedUnityPresentationBoundary.Convert(initialBody);
                RuntimeContentRevision diagnosticsRevision = new RuntimeContentRevision(
                    $"fixed-character-runtime/{actorId.Value}",
                    characterRuntime.Abilities[0].SourceRevision.Value,
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
                CharacterPresentationRuntimeBinding presentationBinding;
                if (local)
                {
                    CinemachineCameraRigAdapter cameraRig = m_CameraRig ? m_CameraRig :
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' requires a Camera Rig.");
                    if (!m_CameraFollowAnchor || !m_CameraAimAnchor)
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' requires camera follow and aim anchors.");
                    if (m_CameraFollowAnchor != rootHierarchy.VisualRoot &&
                        !m_CameraFollowAnchor.IsChildOf(rootHierarchy.VisualRoot) ||
                        m_CameraAimAnchor != rootHierarchy.VisualRoot &&
                        !m_CameraAimAnchor.IsChildOf(rootHierarchy.VisualRoot))
                    {
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' camera anchors must belong to VisualRoot.");
                    }
                    input = new UnityFixedCharacterInputAdapter(inputProfile, controlModule, cameraRig);
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateLocalOwner(
                        projectionAsset,
                        tickRate,
                        actorId,
                        animancer,
                        animationRigBinding,
                        rootHierarchy,
                        presentationBody,
                        bodyPresentationProfile,
                        worldAwarePresentation,
                        physicsScene,
                        cameraRig,
                        m_CameraFollowAnchor,
                        m_CameraAimAnchor,
                        m_CameraTargetBindings,
                        input,
                        Require(m_CameraLookInputValueId, nameof(m_CameraLookInputValueId)),
                        sessionHost,
                        diagnosticsContext);
                }
                else
                {
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateSimulatedActor(
                        projectionAsset,
                        tickRate,
                        actorId,
                        animancer,
                        animationRigBinding,
                        rootHierarchy,
                        presentationBody,
                        bodyPresentationProfile,
                        worldAwarePresentation,
                        physicsScene,
                        sessionHost,
                        diagnosticsContext);
                }
                presentation = presentationBinding.Runtime;
                CharacterPresentationProjection projection = presentationBinding.Projection;

                var presentationOutput = new FixedUnityPresentationOutputAdapter(
                    actorId,
                    projection,
                    presentation,
                    RequirePositive(m_MaximumActivePresentationRecords, nameof(m_MaximumActivePresentationRecords)));
                registration = new DeterministicRollbackCharacterRegistration(
                    GetInstanceID(),
                    name,
                    actorId,
                    characterRuntime,
                    characterBinding,
                    new AnimationPresentationProgramIdentity(projection),
                    Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                    initialBody,
                    input,
                    presentationOutput,
                    presentation,
                    rootHierarchy,
                    diagnosticsContext,
                    diagnosticsTarget,
                    m_MaximumActivePresentationRecords);
                input = null;
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
                input?.Dispose();
                throw;
            }
        }

        void DisposeRegistration()
        {
            if (m_Registration == null)
                return;
            DeterministicRollbackCharacterRegistration registration = m_Registration;
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
                throw new InvalidOperationException($"Rollback Character Host requires an explicit '{field}'.");
            return value;
        }

        static int RequirePositive(int value, string field)
        {
            return value > 0
                ? value
                : throw new InvalidOperationException($"Rollback Character Host requires a positive '{field}'.");
        }
    }
}


