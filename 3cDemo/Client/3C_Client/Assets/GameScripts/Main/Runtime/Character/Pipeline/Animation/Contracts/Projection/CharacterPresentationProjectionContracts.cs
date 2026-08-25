using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPresentationProducerKind
    {
        Animation,
        Camera,
        Cue
    }

    [Serializable]
    public sealed partial class CharacterPresentationProjection : ISerializationCallbackReceiver
    {
        public const string CurrentAbiVersion = "character-presentation-projection/v13";

        [SerializeField] string m_AbiVersion = string.Empty;
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_SourceRevision = string.Empty;
        [SerializeField] string m_SemanticHash = string.Empty;
        [SerializeField] string m_ContractHash = string.Empty;
        [SerializeField] CharacterPresentationProducerEntry[] m_Producers = Array.Empty<CharacterPresentationProducerEntry>();
        [SerializeField] AnimationFootAnalysisProjectionIdentity m_FootAnalysis;

        public string ProgramId => m_ProgramId;
        public string AbiVersion => m_AbiVersion ?? string.Empty;
        public string SourceRevision => m_SourceRevision;
        public string SemanticHash => m_SemanticHash;
        public string ContractHash => m_ContractHash;
        public IReadOnlyList<CharacterPresentationProducerEntry> Producers => m_Producers ?? Array.Empty<CharacterPresentationProducerEntry>();
        public AnimationFootAnalysisProjectionIdentity FootAnalysis => m_FootAnalysis;
        public bool IsValid
        {
            get
            {
                if (!string.Equals(AbiVersion, CurrentAbiVersion, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(m_ProgramId) ||
                    string.IsNullOrEmpty(m_SourceRevision) ||
                    string.IsNullOrEmpty(m_SemanticHash) ||
                    string.IsNullOrEmpty(m_ContractHash) ||
                    string.IsNullOrEmpty(m_ProjectionRevision) ||
                    m_LinkedPose == null || !m_LinkedPose.IsValid ||
                    m_FootAnalysis != null && !m_FootAnalysis.IsValid)
                {
                    return false;
                }
                for (int i = 0; i < Producers.Count; i++)
                {
                    if (Producers[i] == null || Producers[i].ProgramProducerIndex != i || !Producers[i].IsValid)
                        return false;
                }
                return true;
            }
        }

        public IReadOnlyList<CharacterPresentationProducerEntry> AnimationProducers
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Producers.Count; i++)
                {
                    if (Producers[i].Kind == CharacterPresentationProducerKind.Animation)
                        count++;
                }
                if (count == 0)
                    return Array.Empty<CharacterPresentationProducerEntry>();
                var values = new CharacterPresentationProducerEntry[count];
                int index = 0;
                for (int i = 0; i < Producers.Count; i++)
                {
                    if (Producers[i].Kind == CharacterPresentationProducerKind.Animation)
                        values[index++] = Producers[i];
                }
                return values;
            }
        }

        public bool TryGetProducer(string programProducerIdentity, out CharacterPresentationProducerEntry producer)
        {
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry candidate = Producers[i];
                if (string.Equals(candidate.ProgramProducerIdentity, programProducerIdentity, StringComparison.Ordinal))
                {
                    producer = candidate;
                    return true;
                }
            }
            producer = null;
            return false;
        }

        public void RequireContract(CharacterPresentationSemanticContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (!IsValid ||
                !string.Equals(m_ProgramId, contract.ProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(m_SourceRevision, contract.SourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(m_SemanticHash, contract.SemanticHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(m_ContractHash, contract.ContractHash.ToString(), StringComparison.Ordinal) ||
                Producers.Count != contract.Producers.Count)
            {
                string invalidProducer = string.Empty;
                for (int i = 0; i < Producers.Count; i++)
                {
                    CharacterPresentationProducerEntry producer = Producers[i];
                    if (producer == null)
                    {
                        invalidProducer = $"Producer[{i}]=null";
                        break;
                    }
                    if (producer.ProgramProducerIndex != i || !producer.IsValid)
                    {
                        invalidProducer = producer.DescribeValidity(i);
                        break;
                    }
                }
                throw new InvalidOperationException(
                    $"Character Presentation Projection does not match the loaded semantic contract. " +
                    $"Actual Valid={IsValid} ProjectionRevision={m_ProjectionRevision} Program={m_ProgramId} Source={m_SourceRevision} Semantic={m_SemanticHash} Contract={m_ContractHash} Producers={Producers.Count} InvalidProducer={invalidProducer}; " +
                    $"Expected Program={contract.ProgramId.Value} Source={contract.SourceRevision.Value} Semantic={contract.SemanticHash} Contract={contract.ContractHash} Producers={contract.Producers.Count}.");
            }
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = Producers[i];
                if (producer == null || producer.ProgramProducerIndex != i ||
                    producer.ProgramProducerIndex != contract.Producers[i].Index ||
                    !string.Equals(producer.ProgramProducerIdentity, contract.Producers[i].Identity, StringComparison.Ordinal) ||
                    producer.AnimationChannelId != contract.Producers[i].AnimationChannelId ||
                    !string.Equals(producer.SourceIdentity, contract.Producers[i].SourceIdentity, StringComparison.Ordinal) ||
                    producer.ChannelKind != contract.Producers[i].ChannelKind)
                {
                    throw new InvalidOperationException($"Character Presentation Projection producer #{i} does not match the loaded semantic contract.");
                }
            }
        }

    }

    [Serializable]
    public sealed class CharacterPresentationProducerEntry
    {
        [SerializeField] int m_ProgramProducerIndex;
        [SerializeField] string m_ProgramProducerIdentity = string.Empty;
        [SerializeField] string m_SourceIdentity = string.Empty;
        [SerializeField] ProgramOutputChannelKind m_ChannelKind;
        [SerializeField] CharacterPresentationProducerKind m_Kind;
        [SerializeField] TimelinePlaybackMode m_PlaybackMode;
        [SerializeField] string m_TimelineAuthoringId = string.Empty;
        [SerializeField] string m_TrackAuthoringId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] string m_SourceGraphId = string.Empty;
        [SerializeField] string m_SourceNodeId = string.Empty;
        [SerializeField] string m_SourceTimelineId = string.Empty;
        [SerializeField] string m_SourceTrackId = string.Empty;
        [SerializeField] string m_SourceDisplayPath = string.Empty;
        [SerializeReference] CharacterPresentationAnimationBinding m_Animation;
        [SerializeReference] CharacterPresentationCameraBinding m_Camera;
        [SerializeReference] CharacterPresentationCueBinding m_Cue;

        public CharacterPresentationProducerEntry(
            int programProducerIndex,
            string programProducerIdentity,
            string sourceIdentity,
            ProgramOutputChannelKind channelKind,
            CharacterPresentationProducerKind kind,
            TimelinePlaybackMode playbackMode,
            string timelineAuthoringId,
            string trackAuthoringId,
            AnimationChannelId animationChannelId,
            string sourceGraphId,
            string sourceNodeId,
            string sourceTimelineId,
            string sourceTrackId,
            string sourceDisplayPath,
            CharacterPresentationAnimationBinding animation,
            CharacterPresentationCameraBinding camera,
            CharacterPresentationCueBinding cue)
        {
            if (!animationChannelId.IsValid)
                throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            m_ProgramProducerIndex = programProducerIndex;
            m_ProgramProducerIdentity = programProducerIdentity ?? string.Empty;
            m_SourceIdentity = sourceIdentity ?? string.Empty;
            m_ChannelKind = channelKind;
            m_Kind = kind;
            m_PlaybackMode = playbackMode;
            m_TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            m_TrackAuthoringId = trackAuthoringId ?? string.Empty;
            m_AnimationChannelId = animationChannelId.Value;
            m_SourceGraphId = sourceGraphId ?? string.Empty;
            m_SourceNodeId = sourceNodeId ?? string.Empty;
            m_SourceTimelineId = sourceTimelineId ?? string.Empty;
            m_SourceTrackId = sourceTrackId ?? string.Empty;
            m_SourceDisplayPath = sourceDisplayPath ?? string.Empty;
            m_Animation = animation;
            m_Camera = camera;
            m_Cue = cue;
        }

        public int ProgramProducerIndex => m_ProgramProducerIndex;
        public string ProgramProducerIdentity => m_ProgramProducerIdentity;
        public string SourceIdentity => m_SourceIdentity;
        public ProgramOutputChannelKind ChannelKind => m_ChannelKind;
        public CharacterPresentationProducerKind Kind => m_Kind;
        public TimelinePlaybackMode PlaybackMode => m_PlaybackMode;
        public AnimationProducerId ProducerId => new AnimationProducerId(m_TimelineAuthoringId, m_TrackAuthoringId);
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId)
            ? default
            : new AnimationChannelId(m_AnimationChannelId);
        public string SourceGraphId => m_SourceGraphId;
        public string SourceNodeId => m_SourceNodeId;
        public string SourceTimelineId => m_SourceTimelineId;
        public string SourceTrackId => m_SourceTrackId;
        public string SourceDisplayPath => m_SourceDisplayPath;
        public CharacterPresentationAnimationBinding Animation => m_Animation;
        public CharacterPresentationCameraBinding Camera => m_Camera;
        public CharacterPresentationCueBinding Cue => m_Cue;
        public int AuthoredClipCount => m_Animation?.Clips.Count ?? 0;
        public float SourceDurationSeconds => m_Animation?.DurationSeconds ?? 0f;
        bool HasCleanAnimationPayload => m_Animation != null;

        public bool IsValid => m_ProgramProducerIndex >= 0 &&
                               !string.IsNullOrWhiteSpace(m_ProgramProducerIdentity) &&
                               !string.IsNullOrWhiteSpace(m_SourceIdentity) &&
                               AnimationChannelId.IsValid &&
                               Enum.IsDefined(typeof(ProgramOutputChannelKind), m_ChannelKind) &&
                               Enum.IsDefined(typeof(CharacterPresentationProducerKind), m_Kind) &&
                               Enum.IsDefined(typeof(TimelinePlaybackMode), m_PlaybackMode) &&
                               (m_Kind == CharacterPresentationProducerKind.Animation &&
                                 HasCleanAnimationPayload && m_Camera == null && m_Cue == null ||
                                m_Kind == CharacterPresentationProducerKind.Camera && m_Camera != null && m_Animation == null &&
                                m_Cue == null ||
                                m_Kind == CharacterPresentationProducerKind.Cue && m_Cue != null && m_Animation == null &&
                                m_Camera == null);

        internal string DescribeValidity(int expectedIndex)
        {
            return $"Producer[{expectedIndex}] Index={m_ProgramProducerIndex} Identity={m_ProgramProducerIdentity} Source={m_SourceIdentity} " +
                   $"Channel={m_AnimationChannelId} ChannelKind={m_ChannelKind} Kind={m_Kind} " +
                   $"PlaybackMode={m_PlaybackMode} " +
                   $"Animation={m_Animation != null} Camera={m_Camera != null} Cue={m_Cue != null} " +
                   $"CleanAnimationPayload={HasCleanAnimationPayload} IsValid={IsValid}";
        }
    }

    public enum CharacterPresentationCameraBindingKind
    {
        State,
        Cue,
        Response,
        Target
    }

    [Serializable]
    public sealed class CharacterPresentationCameraBinding
    {
        [SerializeField] CharacterPresentationCameraBindingKind m_Kind;
        [SerializeField] TimelineCameraMode m_Mode;
        [SerializeField] int m_Priority;
        [SerializeField] float m_BlendInSeconds;
        [SerializeField] float m_BlendOutSeconds;
        [SerializeField] string m_TargetKey = string.Empty;
        [SerializeField] TimelineCameraInterruptPolicy m_InterruptPolicy;
        [SerializeField] string m_CueId = string.Empty;
        [SerializeField] TimelineCameraCueKind m_CueKind;
        [SerializeField] string m_CueType = string.Empty;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] TimelineCameraLookResponseMode m_LookResponse;
        [SerializeField] float m_ManualOrbitWeight;
        [SerializeField] float m_PitchResponseWeight;
        [SerializeField] float m_YawResponseWeight;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;

        public CharacterPresentationCameraBindingKind Kind => m_Kind;
        public TimelineCameraMode Mode => m_Mode;
        public int Priority => m_Priority;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public string TargetKey => m_TargetKey;
        public TimelineCameraInterruptPolicy InterruptPolicy => m_InterruptPolicy;
        public string CueId => m_CueId;
        public TimelineCameraCueKind CueKind => m_CueKind;
        public string CueType => m_CueType;
        public float DurationSeconds => m_DurationSeconds;
        public TimelineCameraLookResponseMode LookResponse => m_LookResponse;
        public float ManualOrbitWeight => m_ManualOrbitWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;
        public string AnchorKey => m_AnchorKey;
        public string AimPointKey => m_AimPointKey;
        public string PreferredBoneKey => m_PreferredBoneKey;

        public static CharacterPresentationCameraBinding State(
            TimelineCameraMode mode,
            int priority,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            TimelineCameraInterruptPolicy interruptPolicy)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.State,
                m_Mode = mode,
                m_Priority = priority,
                m_BlendInSeconds = blendInSeconds,
                m_BlendOutSeconds = blendOutSeconds,
                m_TargetKey = targetKey ?? string.Empty,
                m_InterruptPolicy = interruptPolicy
            };
        }

        public static CharacterPresentationCameraBinding Cue(
            string cueId,
            TimelineCameraCueKind cueKind,
            string cueType,
            float durationSeconds,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Cue,
                m_CueId = cueId ?? string.Empty,
                m_CueKind = cueKind,
                m_CueType = cueType ?? string.Empty,
                m_DurationSeconds = durationSeconds,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Response(
            TimelineCameraLookResponseMode lookResponse,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Response,
                m_LookResponse = lookResponse,
                m_ManualOrbitWeight = manualOrbitWeight,
                m_PitchResponseWeight = pitchResponseWeight,
                m_YawResponseWeight = yawResponseWeight,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Target(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Target,
                m_TargetKey = targetKey ?? string.Empty,
                m_AnchorKey = anchorKey ?? string.Empty,
                m_AimPointKey = aimPointKey ?? string.Empty,
                m_PreferredBoneKey = preferredBoneKey ?? string.Empty,
                m_Priority = priority
            };
        }
    }

    [Serializable]
    public sealed class CharacterPresentationCueBinding
    {
        [SerializeField] string m_CueId = string.Empty;
        [SerializeField] string m_CueType = string.Empty;

        public CharacterPresentationCueBinding(string cueId, string cueType)
        {
            m_CueId = cueId ?? string.Empty;
            m_CueType = cueType ?? string.Empty;
        }

        public string CueId => m_CueId;
        public string CueType => m_CueType;
    }

    [Serializable]
    public sealed class CharacterPresentationAnimationBinding
    {
        [SerializeField] TransitionAssetBase m_Source;
        [SerializeField] string m_TrackName = string.Empty;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] float m_LastSampleTimeSeconds;
        [SerializeField] CharacterPresentationAnimationClipBinding[] m_Clips = Array.Empty<CharacterPresentationAnimationClipBinding>();

        public CharacterPresentationAnimationBinding(
            TransitionAssetBase source,
            string trackName,
            float durationSeconds,
            float lastSampleTimeSeconds,
            CharacterPresentationAnimationClipBinding[] clips)
        {
            m_Source = source;
            m_TrackName = trackName ?? string.Empty;
            m_DurationSeconds = durationSeconds;
            m_LastSampleTimeSeconds = lastSampleTimeSeconds;
            m_Clips = clips ?? Array.Empty<CharacterPresentationAnimationClipBinding>();
        }

        public TransitionAssetBase Source => m_Source;
        public string TrackName => m_TrackName;
        public float DurationSeconds => m_DurationSeconds;
        public float LastSampleTimeSeconds => m_LastSampleTimeSeconds;
        public IReadOnlyList<CharacterPresentationAnimationClipBinding> Clips => m_Clips ?? Array.Empty<CharacterPresentationAnimationClipBinding>();

        public int Sample(
            float sampleTime,
            int cycle,
            bool isTrackLooping,
            float visualTimeScale,
            ClipSamplePlan[] destination,
            int destinationOffset,
            out AnimationFootPlacementSample footPlacement)
        {
            if (!float.IsFinite(sampleTime) || cycle < 0 ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
            {
                throw new ArgumentException("Animation binding sample time, cycle or visual time scale is invalid.");
            }
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (Clips.Count == 0 || destinationOffset < 0 || destinationOffset > destination.Length - Clips.Count)
                throw new ArgumentOutOfRangeException(nameof(destinationOffset));

            for (int i = 0; i < Clips.Count; i++)
            {
                CharacterPresentationAnimationClipBinding clip = Clips[i];
                if (clip == null)
                    throw new InvalidOperationException($"Presentation animation binding clip #{i} is missing.");
                clip.RequireSampleable(i);
            }

            float totalWeight = 0f;
            float footPlacementWeight = 0f;
            var left = new AnimationFootFeatureBlendAccumulator();
            var right = new AnimationFootFeatureBlendAccumulator();
            int activeCount = 0;
            for (int i = 0; i < Clips.Count; i++)
            {
                activeCount += Clips[i].WriteSample(
                    i,
                    sampleTime,
                    cycle,
                    isTrackLooping,
                    visualTimeScale,
                    destination,
                    destinationOffset + activeCount,
                    ref totalWeight,
                    ref footPlacementWeight,
                    ref left,
                    ref right);
            }

            if (activeCount == 0)
            {
                footPlacement = default;
                return 0;
            }
            if (!float.IsFinite(totalWeight) || totalWeight <= 0f || !float.IsFinite(footPlacementWeight))
                throw new InvalidOperationException("Presentation animation binding has no valid active clip sample.");

            footPlacement = new AnimationFootPlacementSample(
                footPlacementWeight / totalWeight,
                left.Resolve(),
                right.Resolve());
            return activeCount;
        }
    }

    [Serializable]
    public sealed class CharacterPresentationAnimationClipBinding
    {
        [SerializeField] string m_ClipAuthoringId = string.Empty;
        [SerializeField] UnityEngine.AnimationClip m_Clip;
        [SerializeField] string m_ClipIdentity = string.Empty;
        [SerializeField] string m_FullClipDependencyHash = string.Empty;
        [SerializeField] string m_AnalysisInputHash = string.Empty;
        [SerializeField] string m_RegisteredCurveHash = string.Empty;
        [SerializeField] float m_SourceDurationSeconds;
        [SerializeField] float m_StartTime;
        [SerializeField] float m_EndTime;
        [SerializeField] float m_ClipInTime;
        [SerializeField] float m_DurationTime;
        [SerializeField] float m_EaseInTime;
        [SerializeField] float m_EaseOutTime;
        [SerializeField] ExtraPolationMode m_Extrapolation;
        [SerializeField] AnimationCurve m_WeightCurve;
        [SerializeField] AnimationCurve m_EaseInCurve;
        [SerializeField] AnimationCurve m_EaseOutCurve;
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve;
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;

        public string ClipAuthoringId => m_ClipAuthoringId;
        public UnityEngine.AnimationClip Clip => m_Clip;
        public float StartTime => m_StartTime;
        public float EndTime => m_EndTime;
        public float ClipInTime => m_ClipInTime;
        public float DurationTime => m_DurationTime;
        public AnimationFootFeatureCurveSet LeftFootFeatures => m_LeftFootFeatures;
        public AnimationFootFeatureCurveSet RightFootFeatures => m_RightFootFeatures;
        public bool HasFootAnalysis => m_LeftFootFeatures != null && m_RightFootFeatures != null;
        public string ClipIdentity => m_ClipIdentity ?? string.Empty;
        public string FullClipDependencyHash => m_FullClipDependencyHash ?? string.Empty;
        public string AnalysisInputHash => m_AnalysisInputHash ?? string.Empty;
        public string RegisteredCurveHash => m_RegisteredCurveHash ?? string.Empty;
        public float SourceDurationSeconds => m_SourceDurationSeconds > 0f ? m_SourceDurationSeconds : m_Clip ? m_Clip.length : 0f;

        internal CharacterPresentationAnimationClipBinding(
            string clipAuthoringId,
            string clipIdentity,
            string fullClipDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            UnityEngine.AnimationClip clip,
            float sourceDurationSeconds,
            float startTime,
            float endTime,
            float clipInTime,
            float durationTime,
            float easeInTime,
            float easeOutTime,
            ExtraPolationMode extrapolation,
            AnimationCurve weightCurve,
            AnimationCurve easeInCurve,
            AnimationCurve easeOutCurve,
            AnimationCurve footPlacementWeightCurve,
            AnimationFootFeaturePair footFeatures)
        {
            m_ClipAuthoringId = clipAuthoringId ?? string.Empty;
            m_ClipIdentity = clipIdentity ?? string.Empty;
            m_FullClipDependencyHash = fullClipDependencyHash ?? string.Empty;
            m_AnalysisInputHash = analysisInputHash ?? string.Empty;
            m_RegisteredCurveHash = registeredCurveHash ?? string.Empty;
            m_Clip = clip;
            m_SourceDurationSeconds = sourceDurationSeconds;
            m_StartTime = startTime;
            m_EndTime = endTime;
            m_ClipInTime = clipInTime;
            m_DurationTime = durationTime;
            m_EaseInTime = easeInTime;
            m_EaseOutTime = easeOutTime;
            m_Extrapolation = extrapolation;
            m_WeightCurve = CopyCurve(weightCurve);
            m_EaseInCurve = CopyCurve(easeInCurve);
            m_EaseOutCurve = CopyCurve(easeOutCurve);
            m_FootPlacementWeightCurve = CopyCurve(footPlacementWeightCurve);
            if (footFeatures.IsValid)
            {
                m_LeftFootFeatures = footFeatures.Left;
                m_RightFootFeatures = footFeatures.Right;
            }
        }

        internal void RequireSampleable(int clipBindingIndex)
        {
            if (clipBindingIndex < 0 || string.IsNullOrWhiteSpace(m_ClipAuthoringId) ||
                string.IsNullOrWhiteSpace(ClipIdentity) ||
                string.IsNullOrWhiteSpace(FullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(AnalysisInputHash) ||
                string.IsNullOrWhiteSpace(RegisteredCurveHash) ||
                !float.IsFinite(SourceDurationSeconds) || SourceDurationSeconds <= 0f ||
                !m_Clip ||
                !float.IsFinite(m_Clip.length) || m_Clip.length <= 0f ||
                !float.IsFinite(m_StartTime) || !float.IsFinite(m_EndTime) || m_EndTime < m_StartTime ||
                !float.IsFinite(m_ClipInTime) || m_ClipInTime < 0f ||
                !float.IsFinite(m_DurationTime) || m_DurationTime <= 0f ||
                !float.IsFinite(m_EaseInTime) || m_EaseInTime < 0f ||
                !float.IsFinite(m_EaseOutTime) || m_EaseOutTime < 0f ||
                !Enum.IsDefined(typeof(ExtraPolationMode), m_Extrapolation) ||
                !HasKeys(m_WeightCurve) || !HasKeys(m_EaseInCurve) || !HasKeys(m_EaseOutCurve) ||
                !HasKeys(m_FootPlacementWeightCurve) || !HasFootAnalysis ||
                !m_LeftFootFeatures.HasFormalStepEvents ||
                !m_RightFootFeatures.HasFormalStepEvents)
            {
                throw new InvalidOperationException($"Presentation Projection animation clip binding #{clipBindingIndex} is not sampleable.");
            }
        }

        internal int WriteSample(
            int clipBindingIndex,
            float timelineTime,
            int cycle,
            bool isTrackLooping,
            float visualTimeScale,
            ClipSamplePlan[] destination,
            int destinationIndex,
            ref float totalWeight,
            ref float footPlacementWeight,
            ref AnimationFootFeatureBlendAccumulator left,
            ref AnimationFootFeatureBlendAccumulator right)
        {
            RequireSampleable(clipBindingIndex);
            if (!float.IsFinite(timelineTime) || cycle < 0 ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                destination == null || destinationIndex < 0 || destinationIndex >= destination.Length)
            {
                throw new ArgumentException("Animation clip sample request is invalid.");
            }
            if (!m_Clip || timelineTime < m_StartTime)
                return 0;
            bool hold = timelineTime > m_EndTime && m_Extrapolation == ExtraPolationMode.Hold;
            if (timelineTime > m_EndTime && !hold)
                return 0;

            float selfTime = hold ? m_DurationTime : Mathf.Clamp(timelineTime - m_StartTime, 0f, m_DurationTime);
            float remainTime = Mathf.Max(0f, m_EndTime - timelineTime);
            float authoringNormalized = Mathf.Clamp01(selfTime / m_DurationTime);
            float fadeIn = !hold && m_EaseInTime > 0f && selfTime < m_EaseInTime
                ? EvaluateRequired(m_EaseInCurve, Mathf.Clamp01(selfTime / m_EaseInTime), nameof(m_EaseInCurve))
                : 1f;
            float fadeOut = !hold && m_EaseOutTime > 0f && remainTime < m_EaseOutTime
                ? 1f - EvaluateRequired(m_EaseOutCurve, Mathf.Clamp01(1f - remainTime / m_EaseOutTime), nameof(m_EaseOutCurve))
                : 1f;
            float weighted = EvaluateRequired(m_WeightCurve, authoringNormalized, nameof(m_WeightCurve)) * fadeIn * fadeOut;
            if (!float.IsFinite(weighted))
                throw new InvalidOperationException($"Presentation Projection animation clip '{m_ClipAuthoringId}' produced a non-finite weight.");
            float weight = Mathf.Clamp01(weighted);
            if (weight <= 0f)
                return 0;

            double continuousClipTime = (double)m_ClipInTime + selfTime + (double)cycle * m_DurationTime;
            if (double.IsNaN(continuousClipTime) || double.IsInfinity(continuousClipTime) || continuousClipTime < 0d)
                throw new InvalidOperationException($"Presentation Projection animation clip '{m_ClipAuthoringId}' produced an invalid continuous time.");
            bool isLooping = m_Clip.isLooping || isTrackLooping;
            double effectiveClipTime = isLooping
                ? continuousClipTime % SourceDurationSeconds
                : Math.Min(continuousClipTime, SourceDurationSeconds);
            float clipTime = (float)effectiveClipTime;
            float animationNormalized = clipTime / SourceDurationSeconds;
            var plan = new ClipSamplePlan(
                clipBindingIndex,
                m_Clip,
                clipTime,
                continuousClipTime,
                animationNormalized,
                weight,
                isLooping);
            var footSample = new AnimationFootPlacementSample(
                EvaluateRequired(m_FootPlacementWeightCurve, animationNormalized, nameof(m_FootPlacementWeightCurve)),
                m_LeftFootFeatures.Sample(animationNormalized).BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(m_ClipAuthoringId),
                    checked((int)Math.Floor(continuousClipTime / SourceDurationSeconds))),
                m_RightFootFeatures.Sample(animationNormalized).BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(m_ClipAuthoringId),
                    checked((int)Math.Floor(continuousClipTime / SourceDurationSeconds))));

            destination[destinationIndex] = plan;
            totalWeight += plan.Weight;
            footPlacementWeight += footSample.Weight * plan.Weight;
            left.Add(footSample.Left, plan.Weight, visualTimeScale);
            right.Add(footSample.Right, plan.Weight, visualTimeScale);
            return 1;
        }

        static float EvaluateRequired(AnimationCurve curve, float time, string field)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Presentation Projection animation clip requires '{field}'.");
            float value = curve.Evaluate(time);
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"Presentation Projection animation clip curve '{field}' produced a non-finite value.");
            return value;
        }

        static bool HasKeys(AnimationCurve curve) => curve != null && curve.length > 0;

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                return null;
            var result = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }
    }
}
