using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonAction
{
    [Serializable]
    public struct ActionTimelineClipPayloadAuthoring
    {
        [SerializeField] string animationKey;
        [SerializeField] string motionSourceStateId;
        [SerializeField] CharacterStateVariant motionVariant;
        [SerializeField, Min(0f)] float motionDuration;
        [SerializeField, Min(0f)] float motionDistance;
        [SerializeField] bool rotateToDirection;
        [SerializeField] bool setRunLatchOnComplete;
        [SerializeField] string factId;
        [SerializeField] string cueId;

        public ActionTimelineClipPayloadAuthoring(
            string animationKey,
            string motionSourceStateId,
            CharacterStateVariant motionVariant,
            float motionDuration,
            float motionDistance,
            bool rotateToDirection,
            bool setRunLatchOnComplete,
            string factId,
            string cueId)
        {
            this.animationKey = animationKey ?? string.Empty;
            this.motionSourceStateId = motionSourceStateId ?? string.Empty;
            this.motionVariant = motionVariant;
            this.motionDuration = Mathf.Max(0f, motionDuration);
            this.motionDistance = Mathf.Max(0f, motionDistance);
            this.rotateToDirection = rotateToDirection;
            this.setRunLatchOnComplete = setRunLatchOnComplete;
            this.factId = factId ?? string.Empty;
            this.cueId = cueId ?? string.Empty;
        }

        public static ActionTimelineClipPayloadAuthoring Animation(string animationKey)
        {
            return new ActionTimelineClipPayloadAuthoring(
                animationKey,
                string.Empty,
                CharacterStateVariant.None,
                0f,
                0f,
                false,
                false,
                string.Empty,
                string.Empty);
        }

        public static ActionTimelineClipPayloadAuthoring Motion(
            string sourceStateId,
            CharacterStateVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatchOnComplete)
        {
            return new ActionTimelineClipPayloadAuthoring(
                string.Empty,
                sourceStateId,
                variant,
                duration,
                distance,
                rotateToDirection,
                setRunLatchOnComplete,
                string.Empty,
                string.Empty);
        }

        public static ActionTimelineClipPayloadAuthoring Fact(string factId)
        {
            return new ActionTimelineClipPayloadAuthoring(
                string.Empty,
                string.Empty,
                CharacterStateVariant.None,
                0f,
                0f,
                false,
                false,
                factId,
                string.Empty);
        }

        public static ActionTimelineClipPayloadAuthoring Cue(string cueId)
        {
            return new ActionTimelineClipPayloadAuthoring(
                string.Empty,
                string.Empty,
                CharacterStateVariant.None,
                0f,
                0f,
                false,
                false,
                string.Empty,
                cueId);
        }

        public string AnimationKey => animationKey ?? string.Empty;
        public string MotionSourceStateId => motionSourceStateId ?? string.Empty;
        public CharacterStateVariant MotionVariant => motionVariant;
        public float MotionDuration => Mathf.Max(0f, motionDuration);
        public float MotionDistance => Mathf.Max(0f, motionDistance);
        public bool RotateToDirection => rotateToDirection;
        public bool SetRunLatchOnComplete => setRunLatchOnComplete;
        public string FactId => factId ?? string.Empty;
        public string CueId => cueId ?? string.Empty;

        public ActionTimelineClipPayload ToPayload(ActionTimelineClipKind kind, ActionStateId actionState, int sourceStep)
        {
            switch (kind)
            {
                case ActionTimelineClipKind.AnimationKey:
                    return ActionTimelineClipPayload.Animation(new ActionAnimationKey(AnimationKey));
                case ActionTimelineClipKind.Motion:
                    return ActionTimelineClipPayload.Motion(new ActionMotionSpec(
                        actionState,
                        new CharacterStateId(MotionSourceStateId),
                        MotionVariant,
                        MotionDuration,
                        MotionDistance,
                        RotateToDirection,
                        SetRunLatchOnComplete,
                        Vector3.zero,
                        0f,
                        sourceStep));
                case ActionTimelineClipKind.HitboxWindow:
                case ActionTimelineClipKind.CancelWindow:
                    return ActionTimelineClipPayload.Fact(FactId);
                case ActionTimelineClipKind.Cue:
                    return ActionTimelineClipPayload.Cue(CueId);
                default:
                    return default;
            }
        }
    }

    [Serializable]
    public struct ActionTimelineClipAuthoring
    {
        [SerializeField] ActionTimelineClipKind kind;
        [SerializeField, Min(0)] int startFrame;
        [SerializeField, Min(0)] int endFrame;
        [SerializeField] ActionTimelineClipPayloadAuthoring payload;

        public ActionTimelineClipAuthoring(
            ActionTimelineClipKind kind,
            int startFrame,
            int endFrame,
            ActionTimelineClipPayloadAuthoring payload)
        {
            this.kind = kind;
            this.startFrame = Mathf.Max(0, startFrame);
            this.endFrame = Mathf.Max(0, endFrame);
            this.payload = payload;
        }

        public ActionTimelineClipKind Kind => kind;
        public int StartFrame => Mathf.Max(0, startFrame);
        public int EndFrame => Mathf.Max(0, endFrame);
        public ActionTimelineClipPayloadAuthoring Payload => payload;

        public ActionTimelineClipDefinition ToDefinition(ActionStateId actionState, int sourceStep)
        {
            return new ActionTimelineClipDefinition(
                kind,
                StartFrame,
                EndFrame,
                payload.ToPayload(kind, actionState, sourceStep));
        }
    }

    [Serializable]
    public struct ActionTimelineTrackAuthoring
    {
        [SerializeField] ActionTimelineTrackKind kind;
        [SerializeField] ActionTimelineClipAuthoring[] clips;

        public ActionTimelineTrackAuthoring(
            ActionTimelineTrackKind kind,
            ActionTimelineClipAuthoring[] clips)
        {
            this.kind = kind;
            this.clips = clips ?? Array.Empty<ActionTimelineClipAuthoring>();
        }

        public ActionTimelineTrackKind Kind => kind;
        public IReadOnlyList<ActionTimelineClipAuthoring> Clips => clips ?? Array.Empty<ActionTimelineClipAuthoring>();

        public ActionTimelineTrackDefinition ToDefinition(ActionStateId actionState, int sourceStep)
        {
            ActionTimelineClipDefinition[] runtimeClips = new ActionTimelineClipDefinition[Clips.Count];
            for (int i = 0; i < Clips.Count; i++)
                runtimeClips[i] = Clips[i].ToDefinition(actionState, sourceStep);

            return new ActionTimelineTrackDefinition(kind, runtimeClips);
        }
    }

    [Serializable]
    public struct ActionBranchTimelineAuthoring
    {
        [SerializeField] bool required;
        [SerializeField] string branchId;
        [SerializeField] string timelineNodeId;
        [SerializeField, Min(0)] int durationFrames;
        [SerializeField] BodyOccupancyKind defaultBodyKind;
        [SerializeField] CharacterFrameOutputChannel defaultChannels;
        [SerializeField] ActionTimelineTrackAuthoring[] tracks;

        public ActionBranchTimelineAuthoring(
            bool required,
            string branchId,
            string timelineNodeId,
            int durationFrames,
            BodyOccupancyKind defaultBodyKind,
            CharacterFrameOutputChannel defaultChannels,
            ActionTimelineTrackAuthoring[] tracks)
        {
            this.required = required;
            this.branchId = branchId ?? string.Empty;
            this.timelineNodeId = timelineNodeId ?? string.Empty;
            this.durationFrames = Mathf.Max(0, durationFrames);
            this.defaultBodyKind = defaultBodyKind;
            this.defaultChannels = defaultChannels;
            this.tracks = tracks ?? Array.Empty<ActionTimelineTrackAuthoring>();
        }

        public bool Required => required;
        public string BranchId => branchId ?? string.Empty;
        public string TimelineNodeId => timelineNodeId ?? string.Empty;
        public int DurationFrames => Mathf.Max(0, durationFrames);
        public BodyOccupancyKind DefaultBodyKind => defaultBodyKind;
        public CharacterFrameOutputChannel DefaultChannels => defaultChannels;
        public IReadOnlyList<ActionTimelineTrackAuthoring> Tracks => tracks ?? Array.Empty<ActionTimelineTrackAuthoring>();
        public bool HasTimeline => Tracks.Count > 0;

        public ActionBranchDefinition ToBranchDefinition(ActionStateId actionState, int sourceStep)
        {
            if (!HasTimeline)
                return ActionBranchDefinition.Empty;

            ActionTimelineTrackDefinition[] runtimeTracks = new ActionTimelineTrackDefinition[Tracks.Count];
            for (int i = 0; i < Tracks.Count; i++)
                runtimeTracks[i] = Tracks[i].ToDefinition(actionState, sourceStep);

            ActionTimelineDefinition timeline = new ActionTimelineDefinition(
                actionState,
                DurationFrames,
                runtimeTracks);

            return new ActionBranchDefinition(
                new ActionBranchId(BranchId),
                actionState,
                ActionNodeDefinition.Timeline(TimelineNodeId, timeline),
                ToClaim(sourceStep));
        }

        public void ValidateInto(
            CharacterActionCatalogValidationResult result,
            string prefix,
            ActionStateId actionState,
            int sourceStep)
        {
            if (!required && !HasTimeline)
                return;

            if (required && !HasTimeline)
                result.AddError($"{prefix} action branch timeline is required.");
            if (string.IsNullOrWhiteSpace(BranchId))
                result.AddError($"{prefix} action branch id is missing.");
            if (string.IsNullOrWhiteSpace(TimelineNodeId))
                result.AddError($"{prefix} timeline node id is missing.");
            if (defaultBodyKind == BodyOccupancyKind.None)
                result.AddError($"{prefix} action branch body claim is missing.");
            if (defaultChannels == CharacterFrameOutputChannel.None)
                result.AddError($"{prefix} action branch output channels are missing.");

            ActionTimelineValidationResult timelineResult = ActionTimelineValidator.Validate(
                ToBranchDefinition(actionState, sourceStep).RootNode.TimelineNode.Timeline);
            for (int i = 0; i < timelineResult.Errors.Count; i++)
                result.AddError($"{prefix} {timelineResult.Errors[i]}");
            for (int i = 0; i < timelineResult.Warnings.Count; i++)
                result.AddWarning($"{prefix} {timelineResult.Warnings[i]}");
        }

        BodyOccupancyClaim ToClaim(int sourceStep)
        {
            CharacterBodyDomain domain = defaultBodyKind == BodyOccupancyKind.UpperBody
                ? CharacterBodyDomain.UpperBody
                : CharacterBodyDomain.FullBodyAction;

            return defaultBodyKind == BodyOccupancyKind.None
                ? BodyOccupancyClaim.None(sourceStep)
                : new BodyOccupancyClaim(domain, defaultBodyKind, defaultChannels, sourceStep);
        }
    }
}
