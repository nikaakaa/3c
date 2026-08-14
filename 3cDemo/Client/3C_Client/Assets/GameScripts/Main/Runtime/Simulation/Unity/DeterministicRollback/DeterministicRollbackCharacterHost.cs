using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
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
using FixedCharacterSimulationProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using FixedWorldCollisionSummary = ThirdPersonSimulation.Fixed.WorldCollisionSummary;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class DeterministicRollbackCharacterHost : MonoBehaviour, ISimulationSessionActorHost
    {
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] RollbackEndpointAuthoringDefinition m_Endpoint;
        [SerializeField] FixedCharacterSimulationProgramAsset m_Program;
        [SerializeField] CharacterPresentationProjectionAsset m_PresentationProjection;
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] string m_WorldBodyBindingId = string.Empty;
        [SerializeField] Transform m_LogicalSpawn;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
        [SerializeField] CharacterWorldAwarePresentationBinding m_WorldAwarePresentation;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] CharacterAnimationRigBinding m_AnimationRigBinding;
        [SerializeField] ThirdPersonCameraController m_CameraRig;
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
        public Vector3 VisualPosition => m_VisualRoot ? m_VisualRoot.position : transform.position;
        public Transform VisualRoot => m_VisualRoot;
        public CharacterWorldAwarePresentationBinding WorldAwarePresentation => m_WorldAwarePresentation;
        public AnimancerComponent Animancer => m_Animancer;
        public CharacterAnimationRigBinding AnimationRigBinding => m_AnimationRigBinding;

        public void ConfigureAnimationRigBinding(CharacterAnimationRigBinding binding)
        {
            m_AnimationRigBinding = binding ? binding : throw new ArgumentNullException(nameof(binding));
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            SimulationSessionHost sessionHost,
            RollbackEndpointAuthoringDefinition endpoint,
            FixedCharacterSimulationProgramAsset program,
            CharacterPresentationProjectionAsset projection,
            CharacterInputProfile inputProfile,
            string actorId,
            string worldBodyBindingId,
            ThirdPersonCameraController cameraRig,
            string cameraLookInputValueId)
        {
            m_SessionHost = sessionHost ? sessionHost : throw new ArgumentNullException(nameof(sessionHost));
            m_Endpoint = endpoint ? endpoint : throw new ArgumentNullException(nameof(endpoint));
            m_Program = program ? program : throw new ArgumentNullException(nameof(program));
            m_PresentationProjection = projection ? projection : throw new ArgumentNullException(nameof(projection));
            m_InputProfile = inputProfile ? inputProfile : throw new ArgumentNullException(nameof(inputProfile));
            m_ActorId = Require(actorId, nameof(actorId));
            m_WorldBodyBindingId = Require(worldBodyBindingId, nameof(worldBodyBindingId));
            m_LogicalSpawn = transform;
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

        void Reset()
        {
            if (!m_SessionHost)
                m_SessionHost = GetComponentInParent<SimulationSessionHost>();
            if (!m_LogicalSpawn)
                m_LogicalSpawn = transform;
            if (!m_VisualRoot)
                m_VisualRoot = transform;
            if (!m_Animancer)
                m_Animancer = GetComponentInChildren<AnimancerComponent>(true);
            if (!m_CameraRig)
                m_CameraRig = FindObjectOfType<ThirdPersonCameraController>(true);
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
            FixedCharacterSimulationProgramAsset programAsset = m_Program ? m_Program :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a Fixed Program asset.");
            CharacterPresentationProjectionAsset projectionAsset = m_PresentationProjection ? m_PresentationProjection :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a Presentation Projection asset.");
            Transform logicalSpawn = m_LogicalSpawn ? m_LogicalSpawn :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a logical spawn Transform.");
            Transform visualRoot = m_VisualRoot ? m_VisualRoot :
                throw new InvalidOperationException($"Rollback Character Host '{name}' requires a visual root Transform.");
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
            FixedCharacterSimulationProgram program = programAsset.Load();
            bool local = endpoint.ResolvePeerProfile().ActorId == actorId;
            UnityFixedCharacterInputAdapter input = null;
            ICharacterPresentationRuntime presentation = null;
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            DeterministicRollbackCharacterRegistration registration = null;
            try
            {
                FixedWorldBodyState initialBody = BuildInitialBody(actorId, logicalSpawn);
                CharacterPresentationBodyState presentationBody = FixedUnityPresentationBoundary.Convert(initialBody);
                CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(
                    program.Manifest.ProgramId.Value,
                    program.Manifest.SourceRevision.Value,
                    program.ProgramHash.ToString(),
                    program.SourceMap);
                var diagnosticsStore = new RuntimeDiagnosticsStore();
                CharacterPipelineTraceCommandLine.Enable(diagnosticsStore);
                var diagnosticsContext = new RuntimeDiagnosticsContext(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    debugProgram.Revision,
                    debugProgram.SourceMap,
                    diagnosticsStore);
                diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
                CharacterPresentationSemanticContract presentationContract =
                    FixedCharacterPresentationContractAdapter.Create(program);
                CharacterPresentationRuntimeBinding presentationBinding;
                if (local)
                {
                    CharacterInputProfile inputProfile = m_InputProfile ? m_InputProfile :
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' requires an Input Profile.");
                    ThirdPersonCameraController cameraRig = m_CameraRig ? m_CameraRig :
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' requires a Camera Rig.");
                    if (!m_CameraFollowAnchor || !m_CameraAimAnchor)
                        throw new InvalidOperationException($"Local Rollback Character Host '{name}' requires camera follow and aim anchors.");
                    input = new UnityFixedCharacterInputAdapter(inputProfile, program, cameraRig);
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateLocalOwner(
                        projectionAsset,
                        presentationContract,
                        program.Manifest.TickRate,
                        actorId,
                        animancer,
                        animationRigBinding,
                        visualRoot,
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
                        null,
                        diagnosticsContext);
                }
                else
                {
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateSimulatedActor(
                        projectionAsset,
                        presentationContract,
                        program.Manifest.TickRate,
                        actorId,
                        animancer,
                        animationRigBinding,
                        visualRoot,
                        presentationBody,
                        bodyPresentationProfile,
                        worldAwarePresentation,
                        physicsScene,
                        null,
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
                    program,
                    presentationContract,
                    projection.ProjectionRevision,
                    Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                    initialBody,
                    input,
                    presentationOutput,
                    presentation,
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
