using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonGameplay.Tick;
using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [DisallowMultipleComponent]
    public sealed class CharacterPipelineHost : TimelinePreviewTarget
    {
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_ActorId;
        [SerializeField] AnimancerComponent m_Animancer;
        [SerializeField] MonoBehaviour m_LogicPoseAdapter;
        [SerializeField] MonoBehaviour m_MotionExecutorAdapter;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] ThirdPersonCameraController m_CameraRig;
        [SerializeField] Transform m_CameraFollowAnchor;
        [SerializeField] Transform m_CameraAimAnchor;
        [SerializeField] string m_CameraLookInputValueId;
        [SerializeField] CharacterInputSource m_InputSource = CharacterInputSource.LocalDevice;
        [SerializeField] CharacterMotionAuthority m_MotionAuthority = CharacterMotionAuthority.LocalSolver;

        CharacterPipeline m_Pipeline;
        CharacterPipelineDefinition m_PreviewDefinition;
        AnimancerComponent m_PreviewAnimancer;
        PreviewSession m_PreviewSession;
        Guid m_PreviewSessionId;
        ulong m_PreviewGeneration;
        bool m_OwnsPreviewGraphClock;

        public CharacterPipelineDefinition Definition => m_Definition;
        public string ActorId => string.IsNullOrWhiteSpace(m_ActorId) ? string.Empty : m_ActorId.Trim();
        public AnimancerComponent Animancer => m_Animancer;
        public MonoBehaviour LogicPoseAdapter => m_LogicPoseAdapter;
        public MonoBehaviour MotionExecutorAdapter => m_MotionExecutorAdapter;
        public Transform VisualRoot => m_VisualRoot;
        public ThirdPersonCameraController CameraRig => m_CameraRig;
        public Transform CameraFollowAnchor => m_CameraFollowAnchor;
        public Transform CameraAimAnchor => m_CameraAimAnchor;
        public CharacterInputSource InputSource => m_InputSource;
        public CharacterMotionAuthority MotionAuthority => m_MotionAuthority;
        public CharacterPipeline Pipeline => m_Pipeline;
        public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> PreviewAnimationSnapshot =>
            m_PreviewSession != null
                ? m_PreviewSession.Engine.Snapshots
                : Array.Empty<AnimationPlaybackLifecycleSnapshot>();
        public override bool CanPreviewTimeline => !Application.isPlaying && m_Definition && m_Animancer;

        public bool EnsurePipeline()
        {
            if (m_Pipeline != null)
                return true;
            if (string.IsNullOrEmpty(ActorId))
            {
                Debug.LogError("CharacterPipelineHost requires an explicit ActorId.", this);
                return false;
            }
            if (!m_Animancer)
            {
                Debug.LogError("CharacterPipelineHost requires an AnimancerComponent.", this);
                return false;
            }
            ICharacterLogicPosePort logicPosePort = m_LogicPoseAdapter as ICharacterLogicPosePort;
            if (logicPosePort == null)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit Logic Pose Adapter.", this);
                return false;
            }
            ICharacterMotionExecutor motionExecutor = null;
            if (m_MotionAuthority == CharacterMotionAuthority.LocalSolver)
            {
                motionExecutor = m_MotionExecutorAdapter as ICharacterMotionExecutor;
                if (motionExecutor == null)
                {
                    Debug.LogError("CharacterPipelineHost LocalSolver requires an explicit Motion Executor Adapter.", this);
                    return false;
                }
            }
            if (!m_VisualRoot)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit visual root.", this);
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
            if (m_VisualRoot == m_LogicPoseAdapter.transform)
            {
                Debug.LogError("CharacterPipelineHost visual root must be separate from the logic pose root.", this);
                return false;
            }
            if (!m_CameraRig)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit camera rig.", this);
                return false;
            }
            if (!m_CameraFollowAnchor)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit camera follow anchor.", this);
                return false;
            }
            if (!m_CameraAimAnchor)
            {
                Debug.LogError("CharacterPipelineHost requires an explicit camera aim anchor.", this);
                return false;
            }
            if (string.IsNullOrEmpty(m_CameraLookInputValueId))
            {
                Debug.LogError("CharacterPipelineHost requires an explicit camera look input value id.", this);
                return false;
            }
            if (!GameplayTickSystem.IsInitialized)
            {
                Debug.LogError("CharacterPipelineHost requires GameplayTickSystem before pipeline creation.", this);
                return false;
            }

            m_CameraRig.SnapTargets(m_CameraFollowAnchor.position, m_CameraAimAnchor.position);
            m_Pipeline = new CharacterPipeline(
                m_Definition,
                ActorId,
                m_Animancer,
                logicPosePort,
                motionExecutor,
                m_VisualRoot,
                m_CameraRig,
                m_CameraFollowAnchor,
                m_CameraAimAnchor,
                m_CameraLookInputValueId,
                GameplayTickSystem.Current.Settings.LocalLogicTickRate,
                m_InputSource,
                m_MotionAuthority);
            return true;
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
            EnsurePreviewInfrastructure();
            if (sessionId == Guid.Empty || timeline == null || !CanPreviewTimeline)
            {
                ClearTimelinePreview(sessionId);
                return;
            }
            if (evaluationTick == 0)
                throw new InvalidOperationException("Timeline preview evaluation tick must be non-zero.");

            if (m_PreviewSession != null && m_PreviewSessionId != sessionId)
                throw new InvalidOperationException(
                    $"Timeline preview target '{name}' is already owned by session '{m_PreviewSessionId}'.");

            bool created = m_PreviewSession == null;
            if (created)
            {
                m_PreviewSession = new PreviewSession(
                    NextPreviewGeneration(),
                    new PreviewPlaybackEngine(m_Definition, m_Animancer));
                m_PreviewSessionId = sessionId;
                AcquirePreviewGraphClock();
            }

            if (resetLifecycle && !created)
            {
                m_PreviewSession.Engine.Reset();
                m_PreviewSession.Generation = NextPreviewGeneration();
            }

            m_PreviewSession.Capture(
                timeline,
                previousTime,
                currentTime,
                sourceId,
                sourceName,
                evaluationTick,
                Mathf.Max(0f, presentationDeltaSeconds));
            m_PreviewSession.Engine.Evaluate(m_PreviewSession);
        }

        public override void ClearTimelinePreview(Guid sessionId)
        {
            if (sessionId == Guid.Empty || m_PreviewSession == null || m_PreviewSessionId != sessionId)
                return;
            m_PreviewSession.Dispose();
            m_PreviewSession = null;
            m_PreviewSessionId = Guid.Empty;
            ReleasePreviewGraphClock();
        }

        void Awake()
        {
            ClearAllTimelinePreviews();
            EnsurePipeline();
        }

        void Reset()
        {
            m_Animancer = GetComponent<AnimancerComponent>();
            m_CameraRig = GetComponentInChildren<ThirdPersonCameraController>(true);
        }

        void OnValidate()
        {
            if (!m_Animancer)
                m_Animancer = GetComponent<AnimancerComponent>();
            if (!m_CameraRig)
                m_CameraRig = GetComponentInChildren<ThirdPersonCameraController>(true);
        }

        void OnEnable()
        {
            if (!EnsurePipeline())
                return;
            m_Pipeline.RegisterDiagnosticsTarget(name, GetInstanceID());
            m_Pipeline.Activate();
            GameplayTickSystem.RegisterTarget(m_Pipeline);
        }

        void OnDisable()
        {
            ClearAllTimelinePreviews();
            GameplayTickSystem.UnregisterTarget(m_Pipeline);
            m_Pipeline?.Deactivate();
            m_Pipeline?.UnregisterDiagnosticsTarget();
        }

        void OnDestroy()
        {
            ClearAllTimelinePreviews();
            m_Pipeline?.Dispose();
            m_Pipeline = null;
        }

        void EnsurePreviewInfrastructure()
        {
            if (m_PreviewDefinition == m_Definition &&
                m_PreviewAnimancer == m_Animancer)
                return;

            ClearAllTimelinePreviews();
            m_PreviewDefinition = m_Definition;
            m_PreviewAnimancer = m_Animancer;
        }

        void ClearAllTimelinePreviews()
        {
            m_PreviewSession?.Dispose();
            m_PreviewSession = null;
            m_PreviewSessionId = Guid.Empty;
            ReleasePreviewGraphClock();
        }

        void AcquirePreviewGraphClock()
        {
            if (m_OwnsPreviewGraphClock)
                return;
            m_Animancer.Graph.PauseGraph();
            m_OwnsPreviewGraphClock = true;
        }

        void ReleasePreviewGraphClock()
        {
            if (!m_OwnsPreviewGraphClock)
                return;
            if (!Application.isPlaying && m_PreviewAnimancer && m_PreviewAnimancer.IsGraphInitialized)
                m_PreviewAnimancer.Graph.UnpauseGraph();
            m_OwnsPreviewGraphClock = false;
        }

        ulong NextPreviewGeneration()
        {
            m_PreviewGeneration++;
            if (m_PreviewGeneration == 0)
                m_PreviewGeneration++;
            return m_PreviewGeneration;
        }

        sealed class PreviewSession : IDisposable
        {
            public PreviewSession(ulong generation, PreviewPlaybackEngine engine)
            {
                Generation = generation;
                Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            }

            public PreviewPlaybackEngine Engine { get; }
            public ulong Generation { get; set; }
            public TimelineData Timeline { get; private set; }
            public float PreviousTime { get; private set; }
            public float CurrentTime { get; private set; }
            public string SourceId { get; private set; }
            public string SourceName { get; private set; }
            public ulong EvaluationTick { get; private set; }
            public float PresentationDeltaSeconds { get; private set; }
            public bool HasEvaluation => Timeline != null && EvaluationTick != 0;

            public void Capture(
                TimelineData timeline,
                float previousTime,
                float currentTime,
                string sourceId,
                string sourceName,
                ulong evaluationTick,
                float presentationDeltaSeconds)
            {
                Timeline = timeline;
                PreviousTime = previousTime;
                CurrentTime = currentTime;
                SourceId = sourceId ?? string.Empty;
                SourceName = sourceName ?? string.Empty;
                EvaluationTick = evaluationTick;
                PresentationDeltaSeconds = Mathf.Max(0f, presentationDeltaSeconds);
            }

            public void Dispose()
            {
                Engine.Dispose();
            }
        }

        sealed class PreviewPlaybackEngine : IDisposable
        {
            readonly CharacterAnimationPresentationBindingIndex m_Bindings;
            readonly AnimancerPlaybackAdapter m_Adapter;
            readonly AnimationPlaybackLifecycle m_Lifecycle;
            readonly CharacterAnimationPlaybackCommandQueue m_Commands =
                new CharacterAnimationPlaybackCommandQueue();
            readonly List<TimelineAnimationContribution> m_TimelineSamples =
                new List<TimelineAnimationContribution>();
            readonly List<AnimationClipSample> m_ClipSamples = new List<AnimationClipSample>();
            readonly List<AnimationProducerSample> m_ProducerSamples = new List<AnimationProducerSample>();
            readonly List<AnimationPlaybackCommand> m_CommandBuffer = new List<AnimationPlaybackCommand>();
            readonly List<AnimationPlaybackId> m_Retired = new List<AnimationPlaybackId>();
            readonly List<AnimationPlaybackLifecycleSnapshot> m_Snapshots =
                new List<AnimationPlaybackLifecycleSnapshot>();
            readonly HashSet<string> m_SelectedLayers = new HashSet<string>(StringComparer.Ordinal);
            ulong m_SelectionSequence;

            public PreviewPlaybackEngine(CharacterPipelineDefinition definition, AnimancerComponent animancer)
            {
                var errors = new List<string>();
                m_Bindings = CharacterAnimationPresentationBindingIndex.Build(
                    definition.AnimationPresentation,
                    definition.RootTree,
                    errors);
                if (!m_Bindings.IsValid)
                    throw new InvalidOperationException(string.Join("\n", errors));
                m_Adapter = new AnimancerPlaybackAdapter(animancer, m_Bindings, false);
                m_Lifecycle = new AnimationPlaybackLifecycle(m_Bindings, m_Adapter);
            }

            public IReadOnlyList<AnimationPlaybackLifecycleSnapshot> Snapshots => m_Snapshots;

            public void Evaluate(PreviewSession session)
            {
                if (session == null || !session.HasEvaluation)
                    throw new ArgumentException("Timeline preview session has no evaluation.", nameof(session));

                m_SelectedLayers.Clear();
                m_ProducerSamples.Clear();
                for (int trackIndex = 0; trackIndex < session.Timeline.Tracks.Count; trackIndex++)
                {
                    if (session.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;

                    var producerId = new AnimationProducerId(session.Timeline.AuthoringId, track.AuthoringId);
                    if (!m_Bindings.TryGetBinding(producerId, out ResolvedAnimationProducerBinding binding))
                        throw new InvalidOperationException($"Timeline preview producer '{producerId}' has no presentation binding.");
                    if (!m_SelectedLayers.Add(binding.LayerId))
                        throw new InvalidOperationException(
                            $"Timeline preview contains multiple selected producers for layer '{binding.LayerId}'.");

                    var playbackId = new AnimationPlaybackId(producerId, session.Generation);
                    m_TimelineSamples.Clear();
                    track.Sample(
                        session.PreviousTime,
                        session.CurrentTime,
                        trackIndex,
                        session.SourceId,
                        session.SourceName,
                        m_TimelineSamples);
                    m_ClipSamples.Clear();
                    for (int i = 0; i < m_TimelineSamples.Count; i++)
                    {
                        TimelineAnimationContribution clipSample = m_TimelineSamples[i];
                        m_ClipSamples.Add(new AnimationClipSample(
                            clipSample.ClipAuthoringId,
                            RuntimeSourceElementHandle.Invalid,
                            clipSample.Clip,
                            clipSample.ClipTime,
                            clipSample.NormalizedTime,
                            clipSample.Weight,
                            clipSample.IsLooping,
                            clipSample.ClipLoopStartTime,
                            clipSample.ClipLoopDuration,
                            clipSample.ContinuousClipTime));
                    }
                    var producerSample = new AnimationProducerSample(
                        playbackId,
                        binding.LayerId,
                        session.SourceId,
                        session.SourceName,
                        track.Name,
                        session.CurrentTime,
                        0,
                        m_ClipSamples);
                    if (!producerSample.IsValid)
                        throw new InvalidOperationException(
                            $"Timeline preview producer '{producerId}' produced an invalid sample.");
                    m_ProducerSamples.Add(producerSample);
                }

                for (int i = 0; i < m_ProducerSamples.Count; i++)
                {
                    AnimationProducerSample sample = m_ProducerSamples[i];
                    m_Commands.EnqueueSelection(AnimationLayerSelection.Select(
                        sample.LayerId,
                        sample.PlaybackId,
                        session.EvaluationTick,
                        NextSelectionSequence()));
                    m_Commands.EnqueueSample(session.EvaluationTick, sample);
                }

                foreach (ResolvedAnimationLayer layer in m_Bindings.Layers.Values)
                {
                    if (!m_SelectedLayers.Contains(layer.Id))
                    {
                        m_Commands.EnqueueSelection(AnimationLayerSelection.Empty(
                            layer.Id,
                            session.EvaluationTick,
                            NextSelectionSequence()));
                    }
                }

                m_Commands.CopyPendingTo(m_CommandBuffer);
                m_Lifecycle.Apply(
                    m_CommandBuffer,
                    session.PresentationDeltaSeconds,
                    m_Retired);
                m_Lifecycle.BuildSnapshot(m_Snapshots);
                m_Commands.Acknowledge(m_CommandBuffer);
                m_CommandBuffer.Clear();
            }

            public void Reset()
            {
                m_Commands.Clear();
                m_Lifecycle.Reset();
                m_TimelineSamples.Clear();
                m_ClipSamples.Clear();
                m_ProducerSamples.Clear();
                m_CommandBuffer.Clear();
                m_Retired.Clear();
                m_Snapshots.Clear();
                m_SelectedLayers.Clear();
            }

            public void Dispose()
            {
                Reset();
                m_Adapter.Dispose();
            }

            ulong NextSelectionSequence()
            {
                m_SelectionSequence++;
                if (m_SelectionSequence == 0)
                    m_SelectionSequence++;
                return m_SelectionSequence;
            }
        }
    }
}
