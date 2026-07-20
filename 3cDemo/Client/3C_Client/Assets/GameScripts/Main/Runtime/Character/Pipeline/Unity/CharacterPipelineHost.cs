using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonCamera;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline
{
    [DisallowMultipleComponent]
    public sealed class CharacterPipelineHost : TimelinePreviewTarget, ITimelineAnimationMarkerSyncAuthoringContext
    {
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] string m_ActorId;
        [SerializeField] CharacterControlSource m_ControlSource;
        [SerializeField] CharacterPresentationRole m_PresentationRole = CharacterPresentationRole.LocalOwner;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] Float32WorldBodyBinding m_WorldBodyBinding;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;
        [SerializeField] CharacterFootPlacementComposition m_FootPlacement;
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
        public CharacterControlSource ControlSource => m_ControlSource;
        public CharacterPresentationRole PresentationRole => m_PresentationRole;
        public string WorldRevision => m_SessionHost && m_SessionHost.Composition
            ? m_SessionHost.Composition.WorldRevision
            : string.Empty;
        public AnimancerComponent Animancer => m_Animancer;
        public Float32WorldBodyBinding WorldBodyBinding => m_WorldBodyBinding;
        public Transform VisualRoot => m_VisualRoot;
        public CharacterBodyPresentationProfile BodyPresentationProfile => m_BodyPresentationProfile;
        public CharacterFootPlacementComposition FootPlacement => m_FootPlacement;
        public ThirdPersonCameraController CameraRig => m_CameraRig;
        public Transform CameraFollowAnchor => m_CameraFollowAnchor;
        public Transform CameraAimAnchor => m_CameraAimAnchor;
        public CharacterSimulationActorRegistration Registration => m_Registration;
        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> PreviewAnimationSnapshot =>
            m_PreviewController != null
                ? m_PreviewController.AnimationSnapshots
                : Array.Empty<AnimationPlaybackLifecycleSnapshot>();
        public override bool CanPreviewTimeline =>
            !Application.isPlaying &&
            m_Definition &&
            m_Definition.AnimationPresentationProfile &&
            m_Definition.SimulationProgram &&
            m_Definition.PresentationProjection &&
            m_Animancer &&
            m_VisualRoot;
        public override string PreviewStatus =>
            "Animation and authored MotionCurve preview. MotionWarp, collision, and Foot Placement require a formal runtime session.";

        public void CollectAnimationMarkerSyncAuthoringIssues(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncAuthoringIssue> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!m_Definition || !m_Definition.RootTreeAsset || m_Definition.RootTreeAsset.Tree == null || timeline == null)
                return;

            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                m_Definition.RootTreeAsset.Tree,
                topologyErrors);
            for (int i = 0; i < topologyErrors.Count; i++)
            {
                destination.Add(new TimelineAnimationMarkerSyncAuthoringIssue(
                    "character_authoring_topology",
                    topologyErrors[i],
                    m_Definition.RootTreeAsset.name,
                    string.Empty));
            }
            if (!topology.IsValid)
                return;

            var issues = new List<AnimationMarkerSyncAuthoringIssue>();
            CharacterAnimationMarkerSyncAuthoringContext.ValidateTrackContext(
                topology,
                timeline.AuthoringId,
                targetTrackAuthoringId,
                issues);
            for (int i = 0; i < issues.Count; i++)
            {
                AnimationMarkerSyncAuthoringIssue issue = issues[i];
                destination.Add(new TimelineAnimationMarkerSyncAuthoringIssue(
                    issue.Code,
                    issue.Message,
                    issue.AuthoringPath,
                    issue.RelatedIdentity));
            }
        }

        public void CollectAnimationMarkerSyncGroupMembers(
            TimelineData timeline,
            string targetTrackAuthoringId,
            List<TimelineAnimationMarkerSyncGroupMember> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!m_Definition || !m_Definition.RootTreeAsset || m_Definition.RootTreeAsset.Tree == null || timeline == null)
                return;

            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(
                m_Definition.RootTreeAsset.Tree,
                topologyErrors);
            if (!topology.IsValid)
                return;
            CharacterAnimationMarkerSyncAuthoringContext.CollectGroupMembers(
                topology,
                timeline.AuthoringId,
                targetTrackAuthoringId,
                destination);
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
            if (!m_VisualRoot)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit visual root.", this);
                return false;
            }
            if (!m_BodyPresentationProfile)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit Body Presentation Profile.", this);
                return false;
            }
            if (!m_FootPlacement)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit Foot Placement Composition.", this);
                return false;
            }
            if (!m_Animancer.Animator)
            {
                Debug.LogError("CharacterPipelineHost requires Animancer to reference a valid Animator.", this);
                return false;
            }
            if (m_Animancer.Animator.transform != m_VisualRoot)
            {
                Debug.LogError("CharacterPipelineHost requires VisualRoot to be the Animancer Animator transform.", this);
                return false;
            }
            if (m_WorldBodyBinding is UnityCharacterControllerWorldBodyBinding ccBinding &&
                m_VisualRoot == ccBinding.LogicRoot)
            {
                Debug.LogError("CharacterPipelineHost visual root must be separate from the logic pose root.", this);
                return false;
            }
            if (m_PresentationRole == CharacterPresentationRole.LocalOwner &&
                (!m_CameraRig || !m_CameraFollowAnchor || !m_CameraAimAnchor || string.IsNullOrEmpty(m_CameraLookInputValueId)))
            {
                Debug.LogError("LocalOwner CharacterPipelineHost requires explicit camera rig, follow anchor, aim anchor, and look input id.", this);
                return false;
            }
            if (!m_Definition.SimulationProgram || !m_Definition.PresentationProjection)
            {
                Debug.LogError("CharacterPipelineHost requires compiled Program and Presentation Projection assets.", this);
                return false;
            }

            IUnityCharacterSimulationInputAdapter inputAdapter = null;
            ICharacterPresentationRuntime presentationRuntime = null;
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            CharacterSimulationActorRegistration registration = null;
            try
            {
                var actorId = new ActorId(ActorId);
                if (m_WorldBodyBinding.ActorId != actorId)
                    throw new InvalidOperationException("CharacterPipelineHost ActorId does not match its World body binding.");
                CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
                CharacterPresentationProjection projection = m_Definition.PresentationProjection.Load(program);
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
                ICharacterFootPlacementSolver footPlacementSolver = m_FootPlacement.RequireSolver(m_VisualRoot);
                PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
                CharacterPresentationRuntimeBinding presentationBinding;
                if (m_PresentationRole == CharacterPresentationRole.LocalOwner)
                {
                    if (!(inputAdapter is ICharacterPresentationLookInput lookInput))
                        throw new InvalidOperationException("LocalOwner control source must provide Presentation look input.");
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateLocalOwner(
                        CharacterPresentationProgramIdentity.From(program),
                        tickRate,
                        projection,
                        actorId,
                        m_Animancer,
                        m_VisualRoot,
                        CharacterPresentationBodyState.FromFloat32(initialBody),
                        m_BodyPresentationProfile,
                        m_FootPlacement.Profile,
                        m_FootPlacement.Rig,
                        footPlacementSolver,
                        physicsScene,
                        m_CameraRig,
                        m_CameraFollowAnchor,
                        m_CameraAimAnchor,
                        m_CameraTargetBindings,
                        lookInput,
                        m_CameraLookInputValueId,
                        diagnosticsContext);
                }
                else
                {
                    presentationBinding = CharacterPresentationRuntimeFactory.CreateSimulatedActor(
                        CharacterPresentationProgramIdentity.From(program),
                        tickRate,
                        projection,
                        actorId,
                        m_Animancer,
                        m_VisualRoot,
                        CharacterPresentationBodyState.FromFloat32(initialBody),
                        m_BodyPresentationProfile,
                        m_FootPlacement.Profile,
                        m_FootPlacement.Rig,
                        footPlacementSolver,
                        physicsScene,
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
                    m_WorldBodyBinding,
                    initialBody,
                    inputAdapter,
                    gameplayOutput,
                    presentationRuntime,
                    diagnosticsAdapter,
                    diagnosticsTarget,
                    m_VisualRoot);
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

        public override void ClearTimelinePreview(Guid sessionId)
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
