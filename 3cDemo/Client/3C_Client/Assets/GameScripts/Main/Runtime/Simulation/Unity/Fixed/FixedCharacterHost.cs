using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using UnityEngine.SceneManagement;
using FixedCharacterSimulationProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using FixedWorldCollisionSummary = ThirdPersonSimulation.Fixed.WorldCollisionSummary;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class FixedCharacterHost : MonoBehaviour, ISimulationSessionActorHost
    {
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] FixedCharacterSimulationProgramAsset m_Program;
        [SerializeField] CharacterPresentationProjectionAsset m_PresentationProjection;
        [SerializeField] FixedCharacterControlSource m_ControlSource;
        [SerializeField] CharacterPresentationRole m_PresentationRole = CharacterPresentationRole.SimulatedActor;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] string m_WorldBodyBindingId = string.Empty;
        [SerializeField] Transform m_LogicalSpawn;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
        [SerializeField] CharacterFootPlacementComposition m_FootPlacement;
        [SerializeField] CharacterEquipmentRigBindingCatalog m_EquipmentRigBindings;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] CharacterAnimationRigBinding m_AnimationRigBinding;
        [SerializeField] ThirdPersonCameraController m_CameraRig;
        [SerializeField] Transform m_CameraFollowAnchor;
        [SerializeField] Transform m_CameraAimAnchor;
        [SerializeField] List<CameraTargetBinding> m_CameraTargetBindings = new List<CameraTargetBinding>();
        [SerializeField] string m_CameraLookInputValueId = string.Empty;
        [SerializeField, Min(1)] int m_MaximumActivePresentationRecords = 128;

        FixedCharacterRegistration m_Registration;

        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));
        public ActorId SimulationActorId => ActorId;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public FixedCharacterControlSource ControlSource => m_ControlSource;
        public ThirdPersonCameraController CameraRig => m_CameraRig;
        public CharacterPresentationRole PresentationRole => m_PresentationRole;
        public Vector3 VisualPosition => m_VisualRoot ? m_VisualRoot.position : transform.position;

#if UNITY_EDITOR
        public void SetAuthoring(
            SimulationSessionHost sessionHost,
            FixedCharacterSimulationProgramAsset program,
            CharacterPresentationProjectionAsset presentationProjection,
            FixedCharacterControlSource controlSource,
            CharacterPresentationRole presentationRole,
            ActorId actorId,
            string worldBodyBindingId,
            Transform logicalSpawn,
            Transform visualRoot,
            CharacterBodyPresentationProfile bodyPresentationProfile,
            CharacterFootPlacementComposition footPlacement,
            CharacterEquipmentRigBindingCatalog equipmentRigBindings,
            AnimancerComponent animancer,
            CharacterAnimationRigBinding animationRigBinding,
            ThirdPersonCameraController cameraRig,
            Transform cameraFollowAnchor,
            Transform cameraAimAnchor,
            IEnumerable<CameraTargetBinding> cameraTargetBindings,
            string cameraLookInputValueId,
            int maximumActivePresentationRecords)
        {
            m_SessionHost = sessionHost ? sessionHost : throw new ArgumentNullException(nameof(sessionHost));
            m_Program = program ? program : throw new ArgumentNullException(nameof(program));
            m_PresentationProjection = presentationProjection ? presentationProjection :
                throw new ArgumentNullException(nameof(presentationProjection));
            m_ControlSource = controlSource ? controlSource : throw new ArgumentNullException(nameof(controlSource));
            m_PresentationRole = presentationRole;
            m_ActorId = actorId.IsValid ? actorId.Value : throw new ArgumentException("ActorId is invalid.", nameof(actorId));
            m_WorldBodyBindingId = Require(worldBodyBindingId, nameof(worldBodyBindingId));
            m_LogicalSpawn = logicalSpawn ? logicalSpawn : throw new ArgumentNullException(nameof(logicalSpawn));
            m_VisualRoot = visualRoot ? visualRoot : throw new ArgumentNullException(nameof(visualRoot));
            m_BodyPresentationProfile = bodyPresentationProfile ? bodyPresentationProfile :
                throw new ArgumentNullException(nameof(bodyPresentationProfile));
            m_FootPlacement = footPlacement ? footPlacement : throw new ArgumentNullException(nameof(footPlacement));
            m_EquipmentRigBindings = equipmentRigBindings;
            m_Animancer = animancer ? animancer : throw new ArgumentNullException(nameof(animancer));
            m_AnimationRigBinding = animationRigBinding
                ? animationRigBinding
                : throw new ArgumentNullException(nameof(animationRigBinding));
            m_CameraRig = cameraRig;
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
#endif

        void Reset()
        {
            if (!m_SessionHost)
                m_SessionHost = GetComponentInParent<SimulationSessionHost>();
            if (!m_ControlSource)
                m_ControlSource = GetComponent<FixedCharacterControlSource>();
            if (!m_LogicalSpawn)
                m_LogicalSpawn = transform;
            if (!m_VisualRoot)
                m_VisualRoot = transform;
            if (!m_Animancer)
                m_Animancer = GetComponentInChildren<AnimancerComponent>(true);
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
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a SimulationSessionHost.");
            FixedCharacterSimulationProgramAsset programAsset = m_Program ? m_Program :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Fixed Program asset.");
            CharacterPresentationProjectionAsset projectionAsset = m_PresentationProjection ? m_PresentationProjection :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Presentation Projection asset.");
            FixedCharacterControlSource controlSourceDefinition = m_ControlSource ? m_ControlSource :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a formal Fixed Control Source.");
            Transform logicalSpawn = m_LogicalSpawn ? m_LogicalSpawn :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a logical spawn Transform.");
            Transform visualRoot = m_VisualRoot ? m_VisualRoot :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a visual root Transform.");
            CharacterBodyPresentationProfile bodyPresentationProfile = m_BodyPresentationProfile ? m_BodyPresentationProfile :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Body Presentation Profile.");
            CharacterFootPlacementComposition footPlacement = m_FootPlacement ? m_FootPlacement :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires a Foot Placement Composition.");
            AnimancerComponent animancer = m_Animancer ? m_Animancer :
                throw new InvalidOperationException($"Fixed Character Host '{name}' requires an AnimancerComponent.");
            CharacterAnimationRigBinding animationRigBinding = m_AnimationRigBinding
                ? m_AnimationRigBinding
                : throw new InvalidOperationException($"Fixed Character Host '{name}' requires an Animation Rig Binding.");
            ActorId actorId = ActorId;
            ICharacterFootPlacementSolver footPlacementSolver = footPlacement.RequireSolver(visualRoot);
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            FixedCharacterSimulationProgram program = programAsset.Load();
            IUnityFixedCharacterControlSourceRuntime controlSource = null;
            ICharacterPresentationRuntime presentation = null;
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            FixedCharacterRegistration registration = null;
            try
            {
                FixedWorldBodyState initialBody = BuildInitialBody(actorId, logicalSpawn);
                CharacterPresentationBodyState presentationBody = FixedUnityPresentationBoundary.Convert(initialBody);
                CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(
                    program.Manifest.ProgramId.Value,
                    program.Manifest.SourceRevision.Value,
                    program.ProgramHash.ToString(),
                    program.SourceMap);
                var diagnosticsContext = new RuntimeDiagnosticsContext(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    debugProgram.Revision,
                    debugProgram.SourceMap,
                    new RuntimeDiagnosticsStore());
                diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
                CharacterPresentationSemanticContract presentationContract =
                    FixedCharacterPresentationContractAdapter.Create(program);
                CharacterPresentationProjection projection = CharacterPresentationRuntimeFactory.LoadProjection(
                    projectionAsset,
                    presentationContract);
                animationRigBinding.RequireValid(projection.Rig);
                if (projection.EquipmentVisualBindings.Count != 0 && !m_EquipmentRigBindings)
                    throw new InvalidOperationException($"Fixed Character Host '{name}' requires an Equipment Rig Binding Catalog.");
                if (projection.EquipmentVisualBindings.Count != 0)
                    m_EquipmentRigBindings.RequireValid();
                controlSource = controlSourceDefinition.Create(
                    new FixedCharacterControlSourceContext(this, program));
                CharacterPresentationRuntimeBinding presentationBinding;
                switch (m_PresentationRole)
                {
                    case CharacterPresentationRole.LocalOwner:
                    {
                        ThirdPersonCameraController cameraRig = m_CameraRig ? m_CameraRig :
                            throw new InvalidOperationException($"Local Fixed Character Host '{name}' requires a Camera Rig.");
                        if (!m_CameraFollowAnchor || !m_CameraAimAnchor)
                            throw new InvalidOperationException($"Local Fixed Character Host '{name}' requires camera follow and aim anchors.");
                        if (controlSource is not ICharacterPresentationLookInput lookInput)
                            throw new InvalidOperationException($"Local Fixed Character Host '{name}' Control Source has no look input contract.");
                        presentationBinding = CharacterPresentationRuntimeFactory.CreateLocalOwner(
                            presentationContract,
                            program.Manifest.TickRate,
                            projection,
                            actorId,
                            animancer,
                            animationRigBinding,
                            visualRoot,
                            presentationBody,
                            bodyPresentationProfile,
                            footPlacement.Profile,
                            footPlacement.Rig,
                            footPlacementSolver,
                            physicsScene,
                            cameraRig,
                            m_CameraFollowAnchor,
                            m_CameraAimAnchor,
                            m_CameraTargetBindings,
                            lookInput,
                            Require(m_CameraLookInputValueId, nameof(m_CameraLookInputValueId)),
                            m_EquipmentRigBindings,
                            diagnosticsContext);
                        break;
                    }
                    case CharacterPresentationRole.SimulatedActor:
                        presentationBinding = CharacterPresentationRuntimeFactory.CreateSimulatedActor(
                            presentationContract,
                            program.Manifest.TickRate,
                            projection,
                            actorId,
                            animancer,
                            animationRigBinding,
                            visualRoot,
                            presentationBody,
                            bodyPresentationProfile,
                            footPlacement.Profile,
                            footPlacement.Rig,
                            footPlacementSolver,
                            physicsScene,
                            m_EquipmentRigBindings,
                            diagnosticsContext);
                        break;
                    default:
                        throw new InvalidOperationException($"Fixed Character Host '{name}' has an invalid Presentation Role.");
                }
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
                    program,
                    presentationContract,
                    Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                    initialBody,
                    controlSource,
                    presentationOutput,
                    presentation,
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
