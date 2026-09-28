using System;
using System.Collections.Generic;
using System.Globalization;
using Animancer;
using BTSMTL.Timeline;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
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
#if UNITY_EDITOR
        public static event Action<string, string, string, long> StartupMilestone;

        void MarkStartup(string phase)
        {
            var listener = StartupMilestone;
            if (listener != null)
                listener(m_ActorId, m_SessionHost.Composition.SessionId, phase,
                    System.Diagnostics.Stopwatch.GetTimestamp());
        }
#endif
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
        CharacterTimelineHost m_TimelineHost;

        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));
        public ActorId SimulationActorId => ActorId;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public CharacterPipelineDefinition CharacterDefinition => m_CharacterDefinition;
        public CharacterAnimationPresentationProfile AnimationPresentationProfile =>
            m_CharacterDefinition ? m_CharacterDefinition.AnimationPresentationProfile : null;
        public FixedCharacterControlSource ControlSource => m_ControlSource;
        public CinemachineCameraRigAdapter CameraRig => m_CameraRig;
        public CharacterPresentationRole PresentationRole => m_PresentationRole;
        public ICharacterPresentationDomainRuntime PresentationRuntime => m_Registration?.PresentationRuntime;
        public CharacterDomainRuntimeAssemblyFacts DomainFacts => m_Registration?.DomainFacts;
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

        public void EnqueueAbilityInputRequest(string requestId)
        {
            if (m_Registration == null)
                throw new InvalidOperationException($"Fixed Character Host '{name}' has no active character registration.");
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Ability input request id is required.", nameof(requestId));
            if (m_Registration.FixedControlSource is not IUnityFixedCharacterControlSourceRuntime controlSource)
                throw new InvalidOperationException($"Fixed Character Host '{name}' control source does not support request enqueue.");
            controlSource.EnqueueRequest(requestId);
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

        public CharacterTimelineHost TimelineHost => m_TimelineHost;

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
            CharacterAnimationPresentationProfile animationPresentationProfile =
                characterDefinition.AnimationPresentationProfile ?
                characterDefinition.AnimationPresentationProfile :
                throw new InvalidOperationException($"Fixed Character Host '{name}' Definition requires an Animation Presentation Profile.");
            CharacterAnimationRigPayload animationRig = new CharacterAnimationRigPayload(
                animationPresentationProfile.RigDefinition);
            ActorId actorId = ActorId;
            PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
            int tickRate = sessionHost.Composition
                ? sessionHost.Composition.TickRate
                : throw new InvalidOperationException($"Fixed Character Host '{name}' requires an explicit Composition Definition.");
#if UNITY_EDITOR
            MarkStartup("character-registration-start");
#endif
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
#if UNITY_EDITOR
            MarkStartup("fixed-abilities-loaded");
#endif
            m_TimelineHost?.Dispose();
            m_TimelineHost = new CharacterTimelineHost($"character-timeline/{name}");
            for (int i = 0; i < abilityData.Data.Count; i++)
                m_TimelineHost.InstallAbilitySources(abilityData.Data[i].SourceMap);
            m_TimelineHost.InstallPresentationPrograms(characterDefinition.LoadFloat32AbilitySet().Data);
#if UNITY_EDITOR
            MarkStartup("timeline-programs-installed");
#endif
            var timelineRuntime = new CharacterTimelineAbilityRuntime(m_TimelineHost, characterDefinition.ControlMotionTimelines);
            FixedSimulationActorBinding actorBinding = new FixedSimulationActorBinding(
                actorId,
                Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                abilityData,
                timelineRuntime);
            SimulationExecutionTargetManifest target = FixedSimulationTarget.Manifest.ExecutionTarget;
            FixedCharacterRuntime characterRuntime = new FixedCharacterRuntime(
                new[] { actorBinding },
                target.NumericProfile,
                tickRate,
                target.OperationSetVersion,
                controlModules);
#if UNITY_EDITOR
            MarkStartup("character-runtime-created");
#endif
            IUnityFixedCharacterControlSourceRuntime controlSource = null;
            ICharacterPresentationDomainRuntime presentation = null;
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
                int abilityTargetIndexOffset = 0;
                foreach (FixedGameplayAbilityExecutionData abilityEntryData in abilityData.Data)
                {
                    AbilityDebugSourceMapFiller.Fill(debugSourceMap, abilityTargetIndexOffset, abilityEntryData.SourceMap);
                    abilityTargetIndexOffset += abilityEntryData.SourceMap.Count;
                }
                CharacterTimelineDebugSourceMapFiller.Fill(
                    debugSourceMap,
                    characterDefinition.ControlMotionTimelines);
#if UNITY_EDITOR
                MarkStartup("debug-source-map-built");
#endif
                var diagnosticsStore = new RuntimeDiagnosticsStore();
                var diagnosticsContext = new RuntimeDiagnosticsContext(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    diagnosticsRevision,
                    debugSourceMap,
                    diagnosticsStore);
                diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
                m_TimelineHost.AttachRuntimeDiagnostics(diagnosticsContext);
                animationRigBinding.RequireValid(animationRig);
#if UNITY_EDITOR
                MarkStartup("control-source-start");
#endif
                controlSource = controlSourceDefinition.Create(
                    new FixedCharacterControlSourceContext(this, characterDefinition, controlModule));
#if UNITY_EDITOR
                MarkStartup("control-source-created");
#endif
                IUnityFixedCharacterControlSourceRuntime presentationControlSource = controlSource;
                presentation = CreatePresentationRuntime(
                    animationPresentationProfile,
                    animationRig,
                    actorId,
                    presentationBody,
                    presentationControlSource,
                    physicsScene,
                    diagnosticsContext,
                    tickRate,
                    true,
                    m_TimelineHost);
#if UNITY_EDITOR
                MarkStartup("presentation-runtime-created");
#endif
                timelineRuntime.Install();
#if UNITY_EDITOR
                MarkStartup("timeline-runtime-installed");
#endif
                CharacterDomainRuntimeAssemblyFacts domainFacts =
                    new CharacterDomainRuntimeAssemblyFacts(new[]
                    {
                        new CharacterDomainRuntimeFact(
                            CharacterDomainRuntimeFactKind.Ability,
                            CharacterDomainRuntimeFactState.Adopted,
                            $"ability-set:{characterRuntime.AbilitySetSourceRevision}",
                            $"ability-set:{characterRuntime.GameplayContentHash}",
                            string.Empty),
                        timelineRuntime.CaptureDomainFact(),
                        new CharacterDomainRuntimeFact(
                            CharacterDomainRuntimeFactKind.Motion,
                            CharacterDomainRuntimeFactState.Adopted,
                            $"motion:{actorBinding.BodyMotionBinding.SourceIdentity}:{actorBinding.BodyMotionBinding.ContentRevision}",
                            $"motion:{actorBinding.BodyMotionBinding.BindingHash}",
                            string.Empty)
                    }).Merge(presentation.CaptureDomainFacts().Facts);
                var presentationOutput = new FixedUnityPresentationOutputAdapter(
                    actorId,
                    presentation,
                    RequirePositive(m_MaximumActivePresentationRecords, nameof(m_MaximumActivePresentationRecords)));
                registration = new FixedCharacterRegistration(
                    GetInstanceID(),
                    name,
                    actorId,
                    characterRuntime,
                    actorBinding,
                    Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId)),
                    initialBody,
                    controlSource,
                    presentationOutput,
                    presentation,
                    domainFacts,
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
#if UNITY_EDITOR
                MarkStartup("character-registered");
#endif
            }
            catch
            {
                registration?.Dispose();
                diagnosticsTarget?.Dispose();
                presentation?.Dispose();
                controlSource?.Dispose();
                m_TimelineHost?.Dispose();
                m_TimelineHost = null;
                throw;
            }
        }

        ICharacterPresentationDomainRuntime CreatePresentationRuntime(
            CharacterAnimationPresentationProfile animationPresentationProfile,
            CharacterAnimationRigPayload animationRig,
            ActorId actorId,
            CharacterPresentationBodyState initialPresentationBody,
            IUnityFixedCharacterControlSourceRuntime controlSource,
            PhysicsScene physicsScene,
            RuntimeDiagnosticsContext diagnostics,
            int tickRate,
            bool initializeExternalState,
            CharacterTimelineHost characterTimelineHost = null)
        {
            CharacterPresentationBodyState presentationBody = initialPresentationBody;
            if (m_Registration?.PresentationRuntime is ICharacterPresentationDomainRuntime current &&
                current.TryGetLatestBody(out CharacterPresentationBodyState currentBody))
            {
                presentationBody = currentBody;
            }
            CinemachineCameraRigAdapter cameraRig = null;
            Transform followAnchor = null;
            Transform aimAnchor = null;
            IReadOnlyList<CameraTargetBinding> cameraTargetBindings = null;
            ICharacterPresentationLookInput lookInput = null;
            string lookInputId = string.Empty;
            switch (m_PresentationRole)
            {
                case CharacterPresentationRole.LocalOwner:
                {
                    cameraRig = m_CameraRig ? m_CameraRig :
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
                    if (controlSource is not ICharacterPresentationLookInput lookInputContract)
                        throw new InvalidOperationException($"Local Fixed Character Host '{name}' Control Source has no look input contract.");
                    followAnchor = m_CameraFollowAnchor;
                    aimAnchor = m_CameraAimAnchor;
                    cameraTargetBindings = m_CameraTargetBindings;
                    lookInput = lookInputContract;
                    lookInputId = Require(m_CameraLookInputValueId, nameof(m_CameraLookInputValueId));
                    break;
                }
                case CharacterPresentationRole.SimulatedActor:
                    break;
                default:
                    throw new InvalidOperationException($"Fixed Character Host '{name}' has an invalid Presentation Role.");
            }
            PreparedCharacterLocomotionPresentationBinding locomotionBinding =
                CharacterLocomotionPresentationPreparation.RequireBinding(
                    new CharacterLocomotionPresentationPreparationRequest(
                        new CharacterLocomotionPresentationPlan(
                            string.Concat(
                                m_SessionHost.Composition.SessionId,
                                "/locomotion/fixed"),
                            CharacterLocomotionClockMode.FreeRun,
                            CharacterLocomotionBodySource.CommittedStream),
                        m_BodyPresentationProfile,
                        CharacterLocomotionPresentationSourceCapability.CommittedBodyStream));
            return CharacterPresentationDomainRuntimeFactory.Create(
                tickRate,
                animationPresentationProfile,
                animationRig,
                actorId,
                m_Animancer,
                m_AnimationRigBinding,
                m_RootHierarchy,
                presentationBody,
                m_PresentationRole,
                locomotionBinding,
                m_WorldAwarePresentation,
                physicsScene,
                cameraRig,
                followAnchor,
                aimAnchor,
                cameraTargetBindings,
                lookInput,
                lookInputId,
                m_PresentationRole == CharacterPresentationRole.LocalOwner ? m_CharacterDefinition.CameraProfile : null,
                m_CharacterDefinition.EquipmentPresentationProfile,
                m_EquipmentRigBindings,
                m_SessionHost,
                diagnostics,
                initializeExternalState,
                characterTimelineHost);
        }

        void DisposeRegistration()
        {
            if (m_Registration != null)
            {
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

            m_TimelineHost?.Dispose();
            m_TimelineHost = null;
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

    public sealed class CharacterTimelineAbilityRuntime : IAbilityTimelineRuntime, IAbilityTreeClipInvokerHost,
        IAbilityTimelineLogicMotionReader, IAbilityTimelineLogicMotionWarpReader, IAbilityTimelineMotionWarpCatalogProvider
    {
        readonly CharacterTimelineHost m_Host;
        readonly IReadOnlyList<TimelineAsset> m_TimelineAssets;
        readonly AbilityTimelineMotionWarpCatalog m_MotionWarpCatalog;
        bool m_ContentInstalled;
        readonly Dictionary<int, AbilityTimelineStartRequest> m_Requests =
            new Dictionary<int, AbilityTimelineStartRequest>();

        public CharacterTimelineAbilityRuntime(
            CharacterTimelineHost host,
            IReadOnlyList<TimelineAsset> timelineAssets)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
            m_TimelineAssets = timelineAssets ?? throw new ArgumentNullException(nameof(timelineAssets));
            m_MotionWarpCatalog = BuildMotionWarpCatalog(timelineAssets);
        }

        public int Start(in AbilityTimelineStartRequest request)
        {
            InstallContent();
            bool started = m_Host.RequestAbilityTimelinePlayback(
                request.TimelineId,
                request.ActionContext,
                request.Loop,
                request.InvocationSource,
                request.InputSequence,
                request.Tick,
                out TimelinePlaybackHandle handle);
            if (!started)
                throw new InvalidOperationException($"Ability Timeline '{request.TimelineId}' failed to start for Action '{request.ActionContext.ActionId}'.");
            try
            {
                int runtimeHandle = checked((int)handle.Value);
                m_Requests.Add(runtimeHandle, request);
                return runtimeHandle;
            }
            catch
            {
                m_Host.DiscardUnpublishedTimelinePlayback(handle);
                throw;
            }
        }

        public AbilityTimelineTickResult Tick(int runtimeHandle, ulong logicTick, int tickCount, AbilityTimelinePlaybackControl control)
        {
            CharacterTimelinePendingAdvance pending = m_Host.AdvanceTimelinePlayback(
                new TimelinePlaybackHandle((ulong)runtimeHandle), logicTick, tickCount, control);
            return new AbilityTimelineTickResult(pending.Status, pending.Pending);
        }



        public void Commit(AbilityTimelineAdvancePending pending)
        {
            m_Host.CommitTimelinePlayback(pending);
        }

        public void Discard(AbilityTimelineAdvancePending pending)
        {
            m_Host.DiscardTimelinePlayback(pending);
        }

        public void CopyPendingMotion(int runtimeHandle, List<AbilityTimelineLogicMotion> results) =>
            m_Host.CopyPendingTimelineMotion(runtimeHandle, results);

        public void CopyPendingMotionWarps(int runtimeHandle, List<AbilityTimelineLogicMotionWarp> results) =>
            m_Host.CopyPendingMotionWarps(runtimeHandle, m_MotionWarpCatalog, results);

        public AbilityTimelineMotionWarpCatalog MotionWarpCatalog => m_MotionWarpCatalog;

        static AbilityTimelineMotionWarpCatalog BuildMotionWarpCatalog(IReadOnlyList<TimelineAsset> timelineAssets)
        {
            var identities = new List<AbilityTimelineMotionWarpStateIdentity>();
            var contentParts = new List<string> { "ability-timeline-motion-warp-content/1" };
            for (int assetIndex = 0; assetIndex < timelineAssets.Count; assetIndex++)
            {
                TimelineAsset timelineAsset = timelineAssets[assetIndex]
                    ?? throw new ArgumentException("Control Motion Timeline contains a null asset.", nameof(timelineAssets));
                TimelineData timeline = timelineAsset.Data
                    ?? throw new InvalidOperationException($"Control Motion Timeline '{timelineAsset.name}' has no TimelineData.");
                string timelineId = timeline.AuthoringId;
                if (!AuthoringIdentity.IsValid(timelineId))
                    throw new InvalidOperationException($"Control Motion Timeline '{timelineAsset.name}' has an invalid authoring identity.");
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    Track track = timeline.Tracks[trackIndex];
                    if (track == null)
                        throw new InvalidOperationException($"Control Motion Timeline '{timelineId}' contains a null track.");
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is not MotionWarpClip warp)
                            continue;
                        if (!AuthoringIdentity.IsValid(warp.AuthoringId))
                            throw new InvalidOperationException($"MotionWarpClip in Timeline '{timelineId}' has an invalid authoring identity.");
                        if (!MotionWarpAuthoring.TryResolveSource(timeline, warp.SourceMotionClipId, out MotionCurveClip source))
                            throw new InvalidOperationException(
                                $"MotionWarpClip '{warp.AuthoringId}' in Timeline '{timelineId}' has no source MotionCurve.");
                        identities.Add(new AbilityTimelineMotionWarpStateIdentity(timelineId, warp.AuthoringId, default));
                        contentParts.Add(timelineId);
                        contentParts.Add(warp.AuthoringId);
                        contentParts.Add(source.AuthoringId);
                        contentParts.Add(SourceContentHasher.Hash(JsonUtility.ToJson(source.SourceCurve)));
                        contentParts.Add(warp.StartTime.Raw.ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(warp.EndTime.Raw.ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(((byte)warp.TranslationMode).ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(((byte)warp.TargetOffsetSpace).ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(((byte)warp.RotationMode).ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(((byte)warp.RotationMethod).ToString(CultureInfo.InvariantCulture));
                        contentParts.Add(warp.TargetPlanarOffset.x.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(warp.TargetPlanarOffset.y.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(warp.TargetYawOffsetDegrees.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(warp.MaxTotalPositionCorrection.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(warp.MaxTotalYawCorrectionDegrees.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(warp.MaximumYawRateDegreesPerSecond.ToString("R", CultureInfo.InvariantCulture));
                        contentParts.Add(((byte)warp.LimitPolicy).ToString(CultureInfo.InvariantCulture));
                        AddCurveContent(contentParts, warp.UsesPositionProgress ? warp.PositionProgressCurve : null);
                        AddCurveContent(contentParts, warp.UsesYawProgress ? warp.YawProgressCurve : null);
                        AddCurveContent(contentParts, warp.UsesYawResponse ? warp.YawResponseCurve : null);
                        contentParts.Add(warp.SteeringInputId);
                        AddCurveContent(contentParts, warp.UsesYawResponse ? warp.InputYawResponseCurve : null);
                    }
                }
            }
            identities.Sort((left, right) =>
            {
                int result = string.CompareOrdinal(left.TimelineId, right.TimelineId);
                return result != 0 ? result : string.CompareOrdinal(left.ClipAuthoringId, right.ClipAuthoringId);
            });
            string schema = StableHash.Compute(
                "ability-timeline-motion-warp-state-schema/1",
                identities.Count.ToString(CultureInfo.InvariantCulture)).Value;
            string content = StableHash.Compute(contentParts.ToArray()).Value;
            return new AbilityTimelineMotionWarpCatalog(identities, schema, content);
        }

        static void AddCurveContent(List<string> parts, AnimationCurve curve)
        {
            if (curve == null)
            {
                parts.Add("disabled");
                return;
            }
            Keyframe[] keys = curve.keys;
            parts.Add(keys.Length.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                parts.Add(key.time.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(key.value.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(key.inWeight.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(key.outWeight.ToString("R", CultureInfo.InvariantCulture));
                parts.Add(((int)key.weightedMode).ToString(CultureInfo.InvariantCulture));
            }
        }

        public void PushTreeClipInvoker(IAbilityTreeClipInvoker invoker) =>
            m_Host.PushTreeClipInvoker(invoker);

        public void PopTreeClipInvoker() =>
            m_Host.PopTreeClipInvoker();

        public bool RequestTreeClipExit(int runtimeHandle, string clipAuthoringId) =>
            m_Host.RequestAbilityTreeClipExit(runtimeHandle, clipAuthoringId);

        public void ReleaseUnreferencedPlaybacks(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots, ulong committedTick)
        {
            int handle;
            while ((handle = FindUnreferencedPlayback(snapshots)) != 0)
            {
                m_Host.ReleaseAbilityTimelineOwner(handle, committedTick);
                m_Requests.Remove(handle);
            }
            for (int index = 0; index < snapshots.Count; index++)
                m_Host.PublishAbilityTimelineOwner(snapshots[index].RuntimeHandle);
        }

        public void DiscardUnpublishedPlaybacks(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots)
        {
            int handle;
            while ((handle = FindUnreferencedPlayback(snapshots)) != 0)
            {
                m_Host.DiscardUnpublishedTimelinePlayback(new TimelinePlaybackHandle((ulong)handle));
                m_Requests.Remove(handle);
            }
        }

        int FindUnreferencedPlayback(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots)
        {
            foreach (KeyValuePair<int, AbilityTimelineStartRequest> entry in m_Requests)
            {
                bool retained = false;
                for (int index = 0; index < snapshots.Count; index++)
                    if (snapshots[index].RuntimeHandle == entry.Key)
                    {
                        retained = true;
                        break;
                    }
                if (!retained)
                    return entry.Key;
            }
            return 0;
        }

        public AbilityTimelineRuntimeSnapshot Capture(int runtimeHandle)
        {
            if (!m_Requests.TryGetValue(runtimeHandle, out AbilityTimelineStartRequest request))
                throw new InvalidOperationException($"Ability Timeline runtime '{runtimeHandle}' is not owned by this runtime.");
            return m_Host.CaptureAbilityTimelinePlayback(
                new TimelinePlaybackHandle((ulong)runtimeHandle),
                request);
        }

        public int ApplyRestore(AbilityTimelineRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsValid)
                throw new ArgumentException("Ability Timeline snapshot is invalid.", nameof(snapshot));
            int restoredHandle = m_Host.ApplyAbilityTimelineSnapshot(snapshot);
            m_Requests[restoredHandle] = new AbilityTimelineStartRequest(
                snapshot.TimelineId,
                snapshot.Loop,
                snapshot.ActionContext,
                snapshot.InvocationSource,
                snapshot.InputSequence,
                snapshot.StartTick);
            return restoredHandle;
        }

        internal CharacterDomainRuntimeFact CaptureDomainFact()
        {
            if (m_TimelineAssets.Count == 0)
            {
                return new CharacterDomainRuntimeFact(
                    CharacterDomainRuntimeFactKind.Timeline,
                    CharacterDomainRuntimeFactState.Unavailable,
                    "timeline-set",
                    string.Empty,
                    "Character Definition has no Timeline content.");
            }

            var revisionParts = new List<string> { "character-timeline-set/1" };
            for (int i = 0; i < m_TimelineAssets.Count; i++)
            {
                TimelineAsset asset = m_TimelineAssets[i];
                if (!asset || asset.Data == null)
                {
                    return new CharacterDomainRuntimeFact(
                        CharacterDomainRuntimeFactKind.Timeline,
                        CharacterDomainRuntimeFactState.Failed,
                        $"timeline-set:{i}",
                        string.Empty,
                        $"Timeline content #{i} is invalid.");
                }
                revisionParts.Add(asset.Data.AuthoringId);
                revisionParts.Add(TimelineAuthoringFingerprint.Compute(asset.Data));
            }
            string revision = StableHash.Compute(revisionParts.ToArray()).ToString();
            return new CharacterDomainRuntimeFact(
                CharacterDomainRuntimeFactKind.Timeline,
                m_ContentInstalled
                    ? CharacterDomainRuntimeFactState.Adopted
                    : CharacterDomainRuntimeFactState.Prepared,
                $"timeline-set:{revision}",
                m_ContentInstalled ? $"timeline-set:{revision}" : string.Empty,
                m_ContentInstalled ? string.Empty : "Timeline content has not been installed.");
        }

        internal void Install()
        {
            InstallContent();
        }

        public AbilityTimelineStopResult Stop(int runtimeHandle, ulong logicTick)
        {
            CharacterTimelinePendingStop pending = m_Host.RequestStopTimelinePlayback(
                new TimelinePlaybackHandle((ulong)runtimeHandle),
                new TimelinePlaybackStopContext(TimelinePlaybackStopCause.SelfAbort, logicTick));
            return new AbilityTimelineStopResult(pending.Status, pending.Pending, m_Requests[runtimeHandle].ActionContext.InstanceId);
        }

        public void CommitStop(AbilityTimelineStopPending pending)
        {
            m_Host.CommitStopTimelinePlayback(pending);
        }

        public void DiscardStop(AbilityTimelineStopPending pending)
        {
            m_Host.DiscardStopTimelinePlayback(pending);
        }

        void InstallContent()
        {
            if (m_ContentInstalled)
                return;
            m_Host.SetTimelineContent(m_TimelineAssets);
            m_ContentInstalled = true;
        }
    }
}
