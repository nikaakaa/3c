using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMotionWarping;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] string warpPolicyId;
        [SerializeField] string warpTargetBindingId;
        [SerializeField] string warpMotionProfileId;
        [SerializeField] bool warpAttackMagnet;
        [SerializeField] bool warpFacingCorrection;
        [SerializeField] bool warpRequireTarget;
        [SerializeField] bool warpRequireMotionProfile;
        [SerializeField] MotionWarpAxisMask warpAxisMask;
        [SerializeField] MotionWarpRotationPolicy warpRotationPolicy;
        [SerializeField, Min(0f)] float warpMaxPlanarDelta;
        [SerializeField, Min(0f)] float warpStoppingDistance;
        [SerializeField, Min(0f)] float warpMaxYawDeltaDegrees;
        [SerializeField, Range(0f, 1f)] float warpTranslationWeight;
        [SerializeField, Range(0f, 1f)] float warpRotationWeight;
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
            : this(
                animationKey,
                motionSourceStateId,
                motionVariant,
                motionDuration,
                motionDistance,
                rotateToDirection,
                setRunLatchOnComplete,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                false,
                false,
                false,
                MotionWarpAxisMask.Planar,
                MotionWarpRotationPolicy.FaceTargetPosition,
                0f,
                0f,
                0f,
                1f,
                1f,
                factId,
                cueId)
        {
        }

        public ActionTimelineClipPayloadAuthoring(
            string animationKey,
            string motionSourceStateId,
            CharacterStateVariant motionVariant,
            float motionDuration,
            float motionDistance,
            bool rotateToDirection,
            bool setRunLatchOnComplete,
            string warpPolicyId,
            string warpTargetBindingId,
            string warpMotionProfileId,
            bool warpAttackMagnet,
            bool warpFacingCorrection,
            bool warpRequireTarget,
            bool warpRequireMotionProfile,
            MotionWarpAxisMask warpAxisMask,
            MotionWarpRotationPolicy warpRotationPolicy,
            float warpMaxPlanarDelta,
            float warpStoppingDistance,
            float warpMaxYawDeltaDegrees,
            float warpTranslationWeight,
            float warpRotationWeight,
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
            this.warpPolicyId = warpPolicyId ?? string.Empty;
            this.warpTargetBindingId = warpTargetBindingId ?? string.Empty;
            this.warpMotionProfileId = warpMotionProfileId ?? string.Empty;
            this.warpAttackMagnet = warpAttackMagnet;
            this.warpFacingCorrection = warpFacingCorrection;
            this.warpRequireTarget = warpRequireTarget;
            this.warpRequireMotionProfile = warpRequireMotionProfile;
            this.warpAxisMask = warpAxisMask == MotionWarpAxisMask.None ? MotionWarpAxisMask.Planar : warpAxisMask;
            this.warpRotationPolicy = warpRotationPolicy;
            this.warpMaxPlanarDelta = Mathf.Max(0f, warpMaxPlanarDelta);
            this.warpStoppingDistance = Mathf.Max(0f, warpStoppingDistance);
            this.warpMaxYawDeltaDegrees = Mathf.Max(0f, warpMaxYawDeltaDegrees);
            this.warpTranslationWeight = Mathf.Clamp01(warpTranslationWeight);
            this.warpRotationWeight = Mathf.Clamp01(warpRotationWeight);
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
        public string WarpPolicyId => warpPolicyId ?? string.Empty;
        public string WarpTargetBindingId => warpTargetBindingId ?? string.Empty;
        public string WarpMotionProfileId => warpMotionProfileId ?? string.Empty;
        public bool WarpAttackMagnet => warpAttackMagnet;
        public bool WarpFacingCorrection => warpFacingCorrection;
        public bool WarpRequireTarget => warpRequireTarget;
        public bool WarpRequireMotionProfile => warpRequireMotionProfile;
        public MotionWarpAxisMask WarpAxisMask => warpAxisMask == MotionWarpAxisMask.None ? MotionWarpAxisMask.Planar : warpAxisMask;
        public MotionWarpRotationPolicy WarpRotationPolicy => warpRotationPolicy;
        public float WarpMaxPlanarDelta => Mathf.Max(0f, warpMaxPlanarDelta);
        public float WarpStoppingDistance => Mathf.Max(0f, warpStoppingDistance);
        public float WarpMaxYawDeltaDegrees => Mathf.Max(0f, warpMaxYawDeltaDegrees);
        public float WarpTranslationWeight => Mathf.Clamp01(warpTranslationWeight);
        public float WarpRotationWeight => Mathf.Clamp01(warpRotationWeight);
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
                        sourceStep,
                        ToMotionWarpPayload()));
                case ActionTimelineClipKind.HitboxWindow:
                case ActionTimelineClipKind.CancelWindow:
                    return ActionTimelineClipPayload.Fact(FactId);
                case ActionTimelineClipKind.Cue:
                    return ActionTimelineClipPayload.Cue(CueId);
                default:
                    return default;
            }
        }

        MotionWarpPayload ToMotionWarpPayload()
        {
            bool hasWarp = WarpAttackMagnet || WarpFacingCorrection || !string.IsNullOrWhiteSpace(WarpPolicyId);
            if (!hasWarp)
                return MotionWarpPayload.None;

            return new MotionWarpPayload(
                new MotionWarpPolicy(
                    new MotionWarpPolicyId(WarpPolicyId),
                    WarpAttackMagnet,
                    WarpFacingCorrection,
                    WarpRequireTarget,
                    WarpRequireMotionProfile,
                    WarpMotionProfileId,
                    WarpAxisMask,
                    WarpRotationPolicy,
                    WarpMaxPlanarDelta,
                    WarpStoppingDistance,
                    WarpMaxYawDeltaDegrees,
                    WarpTranslationWeight,
                    WarpRotationWeight),
                new MotionWarpTargetBindingId(WarpTargetBindingId));
        }
    }

    [Serializable]
    public struct ActionTimelineClipAuthoring
    {
        [SerializeField] string stableId;
        [SerializeField] ActionTimelineClipKind kind;
        [SerializeField, Min(0f)] float startSeconds;
        [SerializeField, Min(0f)] float endSeconds;
        [SerializeField, HideInInspector, FormerlySerializedAs("startFrame")] int legacyStartFrame;
        [SerializeField, HideInInspector, FormerlySerializedAs("endFrame")] int legacyEndFrame;
        [SerializeField] ActionTimelineClipPayloadAuthoring payload;

        public ActionTimelineClipAuthoring(
            ActionTimelineClipKind kind,
            float startSeconds,
            float endSeconds,
            ActionTimelineClipPayloadAuthoring payload)
        {
            stableId = string.Empty;
            this.kind = kind;
            this.startSeconds = startSeconds;
            this.endSeconds = endSeconds;
            legacyStartFrame = 0;
            legacyEndFrame = 0;
            this.payload = payload;
        }

        public string StableId => stableId ?? string.Empty;
        public ActionTimelineClipKind Kind => kind;
        public float StartSeconds => startSeconds;
        public float EndSeconds => endSeconds;
        public int LegacyStartFrame => Mathf.Max(0, legacyStartFrame);
        public int LegacyEndFrame => Mathf.Max(0, legacyEndFrame);
        public ActionTimelineClipPayloadAuthoring Payload => payload;

        public ActionTimelineClipDefinition ToDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            int startTick = ActionTimelineQuantizer.QuantizeSecondsToTick(StartSeconds, in compileContext);
            int endTick = ActionTimelineQuantizer.QuantizeSecondsToTick(EndSeconds, in compileContext);
            return new ActionTimelineClipDefinition(
                kind,
                startTick,
                endTick,
                payload.ToPayload(kind, actionState, sourceStep));
        }
    }

    [Serializable]
    public struct ActionTimelineTrackAuthoring
    {
        [SerializeField] string stableId;
        [SerializeField] ActionTimelineTrackKind kind;
        [SerializeField] ActionTimelineClipAuthoring[] clips;

        public ActionTimelineTrackAuthoring(
            ActionTimelineTrackKind kind,
            ActionTimelineClipAuthoring[] clips)
        {
            stableId = string.Empty;
            this.kind = kind;
            this.clips = clips ?? Array.Empty<ActionTimelineClipAuthoring>();
        }

        public string StableId => stableId ?? string.Empty;
        public ActionTimelineTrackKind Kind => kind;
        public IReadOnlyList<ActionTimelineClipAuthoring> Clips => clips ?? Array.Empty<ActionTimelineClipAuthoring>();

        public ActionTimelineTrackDefinition ToDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            ActionTimelineClipDefinition[] runtimeClips = new ActionTimelineClipDefinition[Clips.Count];
            for (int i = 0; i < Clips.Count; i++)
                runtimeClips[i] = Clips[i].ToDefinition(actionState, sourceStep, in compileContext);

            return new ActionTimelineTrackDefinition(kind, runtimeClips);
        }
    }

    [Serializable]
    public struct CommittedActionBranchConditionAuthoring
    {
        [SerializeField] CommittedActionConditionKind kind;
        [SerializeField] CharacterStateVariant expectedVariant;
        [SerializeField] bool expectedBool;
        [SerializeField] InputRequestKind requestKind;
        [SerializeField] string requiredFactId;

        public CommittedActionBranchConditionAuthoring(
            CommittedActionConditionKind kind,
            CharacterStateVariant expectedVariant,
            bool expectedBool)
            : this(kind, expectedVariant, expectedBool, default, string.Empty)
        {
        }

        public CommittedActionBranchConditionAuthoring(
            CommittedActionConditionKind kind,
            CharacterStateVariant expectedVariant,
            bool expectedBool,
            InputRequestKind requestKind,
            string requiredFactId)
        {
            this.kind = kind;
            this.expectedVariant = expectedVariant;
            this.expectedBool = expectedBool;
            this.requestKind = requestKind;
            this.requiredFactId = requiredFactId ?? string.Empty;
        }

        public CommittedActionConditionKind Kind => kind;
        public CharacterStateVariant ExpectedVariant => expectedVariant;
        public bool ExpectedBool => expectedBool;
        public InputRequestKind RequestKind => requestKind;
        public string RequiredFactId => requiredFactId ?? string.Empty;
        public bool IsDefined => kind != CommittedActionConditionKind.None;

        public CommittedActionConditionDefinition ToDefinition()
        {
            switch (kind)
            {
                case CommittedActionConditionKind.Always:
                    return CommittedActionConditionDefinition.Always();
                case CommittedActionConditionKind.RequestHeld:
                    return CommittedActionConditionDefinition.RequestHeld(requestKind);
                case CommittedActionConditionKind.RequestReleased:
                    return CommittedActionConditionDefinition.RequestReleased(requestKind);
                case CommittedActionConditionKind.RequiredFactActive:
                    return CommittedActionConditionDefinition.RequiredFactActive(RequiredFactId);
                case CommittedActionConditionKind.TimelineComplete:
                    return CommittedActionConditionDefinition.TimelineComplete();
                case CommittedActionConditionKind.HasMoveIntent:
                    return CommittedActionConditionDefinition.HasMoveIntent(expectedBool);
                case CommittedActionConditionKind.ActionVariantEquals:
                    return CommittedActionConditionDefinition.ActionVariant(expectedVariant);
                default:
                    return CommittedActionConditionDefinition.Empty;
            }
        }
    }

    [Serializable]
    public struct CommittedActionBranchNodeAuthoring
    {
        [SerializeField] string nodeId;
        [SerializeField] CommittedActionNodeKind kind;
        [SerializeField] CommittedActionBranchConditionAuthoring condition;
        [SerializeField] CommittedActionBranchTimelineAuthoring timeline;
        [SerializeField] string[] childNodeIds;
        [SerializeField] Vector2 editorPosition;

        public CommittedActionBranchNodeAuthoring(
            string nodeId,
            CommittedActionNodeKind kind,
            CommittedActionBranchConditionAuthoring condition,
            CommittedActionBranchTimelineAuthoring timeline,
            string[] childNodeIds,
            Vector2 editorPosition)
        {
            this.nodeId = nodeId ?? string.Empty;
            this.kind = kind;
            this.condition = condition;
            this.timeline = timeline;
            this.childNodeIds = childNodeIds ?? Array.Empty<string>();
            this.editorPosition = editorPosition;
        }

        public string NodeId => nodeId ?? string.Empty;
        public CommittedActionNodeKind Kind => kind;
        public CommittedActionBranchConditionAuthoring Condition => condition;
        public CommittedActionBranchTimelineAuthoring Timeline => timeline;
        public IReadOnlyList<string> ChildNodeIds => childNodeIds ?? Array.Empty<string>();
        public Vector2 EditorPosition => editorPosition;
        public bool HasNodeId => !string.IsNullOrWhiteSpace(NodeId);

        public static CommittedActionBranchNodeAuthoring Selector(
            string nodeId,
            string[] childNodeIds,
            Vector2 editorPosition)
        {
            return new CommittedActionBranchNodeAuthoring(
                nodeId,
                CommittedActionNodeKind.Selector,
                default,
                default,
                childNodeIds,
                editorPosition);
        }

        public static CommittedActionBranchNodeAuthoring Root(
            string nodeId,
            string childNodeId,
            Vector2 editorPosition)
        {
            return new CommittedActionBranchNodeAuthoring(
                nodeId,
                CommittedActionNodeKind.Root,
                default,
                default,
                string.IsNullOrWhiteSpace(childNodeId) ? Array.Empty<string>() : new[] { childNodeId },
                editorPosition);
        }

        public static CommittedActionBranchNodeAuthoring ConditionNode(
            string nodeId,
            CommittedActionBranchConditionAuthoring condition,
            string[] childNodeIds,
            Vector2 editorPosition)
        {
            return new CommittedActionBranchNodeAuthoring(
                nodeId,
                CommittedActionNodeKind.Condition,
                condition,
                default,
                childNodeIds,
                editorPosition);
        }

        public static CommittedActionBranchNodeAuthoring TimelineNode(
            string nodeId,
            CommittedActionBranchTimelineAuthoring timeline,
            Vector2 editorPosition)
        {
            return TimelineNode(nodeId, timeline, Array.Empty<string>(), editorPosition);
        }

        public static CommittedActionBranchNodeAuthoring TimelineNode(
            string nodeId,
            CommittedActionBranchTimelineAuthoring timeline,
            string[] childNodeIds,
            Vector2 editorPosition)
        {
            return new CommittedActionBranchNodeAuthoring(
                nodeId,
                CommittedActionNodeKind.Timeline,
                default,
                timeline,
                childNodeIds,
                editorPosition);
        }

        public CommittedActionNodeDefinition ToDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            switch (kind)
            {
                case CommittedActionNodeKind.Root:
                    return CommittedActionNodeDefinition.Root(
                        NodeId,
                        ChildNodeIds.Count > 0
                            ? new CommittedActionNodeId(ChildNodeIds[0])
                            : default);
                case CommittedActionNodeKind.Timeline:
                    return CommittedActionNodeDefinition.Timeline(
                        NodeId,
                        timeline.ToActionTimelineDefinition(actionState, sourceStep, in compileContext),
                        ToChildNodeIds());
                case CommittedActionNodeKind.Selector:
                    return CommittedActionNodeDefinition.Selector(NodeId, ToChildNodeIds());
                case CommittedActionNodeKind.Condition:
                    return CommittedActionNodeDefinition.ConditionNode(
                        NodeId,
                        condition.ToDefinition(),
                        ToChildNodeIds());
                default:
                    return CommittedActionNodeDefinition.Empty;
            }
        }

        CommittedActionNodeId[] ToChildNodeIds()
        {
            IReadOnlyList<string> source = ChildNodeIds;
            CommittedActionNodeId[] result = new CommittedActionNodeId[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = new CommittedActionNodeId(source[i]);
            return result;
        }
    }

    [Serializable]
    public struct CommittedActionBranchAuthoring
    {
        [SerializeField] int schemaVersion;
        [SerializeField] bool required;
        [SerializeField] string branchId;
        [SerializeField] string rootNodeId;
        [SerializeField] BodyOccupancyKind defaultBodyKind;
        [SerializeField] CharacterFrameOutputChannel defaultChannels;
        [SerializeField] CommittedActionBranchNodeAuthoring[] nodes;

        public CommittedActionBranchAuthoring(
            int schemaVersion,
            bool required,
            string branchId,
            string rootNodeId,
            BodyOccupancyKind defaultBodyKind,
            CharacterFrameOutputChannel defaultChannels,
            CommittedActionBranchNodeAuthoring[] nodes)
        {
            this.schemaVersion = schemaVersion;
            this.required = required;
            this.branchId = branchId ?? string.Empty;
            this.rootNodeId = rootNodeId ?? string.Empty;
            this.defaultBodyKind = defaultBodyKind;
            this.defaultChannels = defaultChannels;
            this.nodes = nodes ?? Array.Empty<CommittedActionBranchNodeAuthoring>();
        }

        public int SchemaVersion => schemaVersion;
        public bool Required => required;
        public string BranchId => branchId ?? string.Empty;
        public string RootNodeId => rootNodeId ?? string.Empty;
        public BodyOccupancyKind DefaultBodyKind => defaultBodyKind;
        public CharacterFrameOutputChannel DefaultChannels => defaultChannels;
        public IReadOnlyList<CommittedActionBranchNodeAuthoring> Nodes => nodes ?? Array.Empty<CommittedActionBranchNodeAuthoring>();
        public bool HasBranch => !string.IsNullOrWhiteSpace(RootNodeId) && Nodes.Count > 0;

        public CommittedActionBranchDefinition ToCommittedActionBranchDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            if (!CanCompile())
                return CommittedActionBranchDefinition.Empty;

            Dictionary<string, CommittedActionNodeDefinition> definitions =
                new Dictionary<string, CommittedActionNodeDefinition>(StringComparer.Ordinal);
            IReadOnlyList<CommittedActionBranchNodeAuthoring> source = Nodes;
            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                if (!node.HasNodeId)
                    continue;
                definitions[node.NodeId] = node.ToDefinition(actionState, sourceStep, in compileContext);
            }

            if (!definitions.TryGetValue(RootNodeId, out CommittedActionNodeDefinition root))
                return CommittedActionBranchDefinition.Empty;

            List<CommittedActionNodeDefinition> nonRootNodes = new List<CommittedActionNodeDefinition>();
            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                if (!node.HasNodeId || string.Equals(node.NodeId, RootNodeId, StringComparison.Ordinal))
                    continue;
                if (definitions.TryGetValue(node.NodeId, out CommittedActionNodeDefinition definition))
                    nonRootNodes.Add(definition);
            }

            return new CommittedActionBranchDefinition(
                new CommittedActionBranchId(BranchId),
                actionState,
                root,
                ToClaim(sourceStep),
                nonRootNodes.ToArray());
        }

        bool CanCompile()
        {
            if (!HasBranch ||
                string.IsNullOrWhiteSpace(BranchId) ||
                string.IsNullOrWhiteSpace(RootNodeId) ||
                defaultBodyKind == BodyOccupancyKind.None ||
                defaultChannels == CharacterFrameOutputChannel.None)
            {
                return false;
            }

            Dictionary<string, CommittedActionBranchNodeAuthoring> nodeMap =
                new Dictionary<string, CommittedActionBranchNodeAuthoring>(StringComparer.Ordinal);
            IReadOnlyList<CommittedActionBranchNodeAuthoring> source = Nodes;
            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                if (!node.HasNodeId || nodeMap.ContainsKey(node.NodeId) || !CanCompileNode(node))
                    return false;

                nodeMap.Add(node.NodeId, node);
            }

            if (!nodeMap.TryGetValue(RootNodeId, out CommittedActionBranchNodeAuthoring rootNode) ||
                rootNode.Kind != CommittedActionNodeKind.Root)
                return false;

            for (int i = 0; i < source.Count; i++)
            {
                IReadOnlyList<string> childIds = source[i].ChildNodeIds;
                for (int childIndex = 0; childIndex < childIds.Count; childIndex++)
                {
                    string childId = childIds[childIndex] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(childId) || !nodeMap.TryGetValue(childId, out CommittedActionBranchNodeAuthoring child))
                        return false;
                    if (source[i].Kind == CommittedActionNodeKind.Timeline &&
                        child.Kind != CommittedActionNodeKind.Condition)
                    {
                        return false;
                    }
                }
            }

            return !HasCycle(
                RootNodeId,
                nodeMap,
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal));
        }

        static bool CanCompileNode(CommittedActionBranchNodeAuthoring node)
        {
            switch (node.Kind)
            {
                case CommittedActionNodeKind.Root:
                    return node.ChildNodeIds.Count == 1;
                case CommittedActionNodeKind.Selector:
                    return node.ChildNodeIds.Count > 0;
                case CommittedActionNodeKind.Condition:
                    return CanCompileCondition(node.Condition) &&
                           node.ChildNodeIds.Count > 0;
                case CommittedActionNodeKind.Timeline:
                    return CanCompileTimeline(node.Timeline);
                default:
                    return false;
            }
        }

        static bool CanCompileCondition(CommittedActionBranchConditionAuthoring condition)
        {
            if (!condition.IsDefined)
                return false;

            switch (condition.Kind)
            {
                case CommittedActionConditionKind.Always:
                case CommittedActionConditionKind.RequestHeld:
                case CommittedActionConditionKind.RequestReleased:
                case CommittedActionConditionKind.TimelineComplete:
                case CommittedActionConditionKind.HasMoveIntent:
                    return true;
                case CommittedActionConditionKind.RequiredFactActive:
                    return ActionFactIdResolver.IsValidFactId(condition.RequiredFactId);
                case CommittedActionConditionKind.ActionVariantEquals:
                    return condition.ExpectedVariant != CharacterStateVariant.None;
                default:
                    return false;
            }
        }

        static bool CanCompileTimeline(CommittedActionBranchTimelineAuthoring timeline)
        {
            if (!timeline.HasTimeline ||
                float.IsNaN(timeline.DurationSeconds) ||
                float.IsInfinity(timeline.DurationSeconds) ||
                timeline.DurationSeconds < 0f)
            {
                return false;
            }

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                ActionTimelineTrackAuthoring track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    ActionTimelineClipAuthoring clip = track.Clips[clipIndex];
                    if (float.IsNaN(clip.StartSeconds) ||
                        float.IsInfinity(clip.StartSeconds) ||
                        clip.StartSeconds < 0f ||
                        float.IsNaN(clip.EndSeconds) ||
                        float.IsInfinity(clip.EndSeconds) ||
                        clip.EndSeconds < 0f ||
                        clip.EndSeconds < clip.StartSeconds)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static ActionFactCompileContext BuildFactContext(
            IReadOnlyList<CommittedActionBranchNodeAuthoring> source,
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            List<ActionFactDeclaration> declarations = new List<ActionFactDeclaration>();
            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                if (node.Kind != CommittedActionNodeKind.Timeline || !CanCompileTimeline(node.Timeline))
                    continue;

                ActionTimelineDefinition timeline = node.Timeline.ToActionTimelineDefinition(actionState, sourceStep, in compileContext);
                ActionFactCompileContext factContext = ActionFactCompileContext.FromTimeline(timeline);
                for (int factIndex = 0; factIndex < factContext.Declarations.Count; factIndex++)
                    declarations.Add(factContext.Declarations[factIndex]);
            }

            return new ActionFactCompileContext(declarations.ToArray());
        }

        public void ValidateInto(
            CharacterActionCatalogValidationResult result,
            string prefix,
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            if (!required && !HasBranch)
                return;

            if (schemaVersion <= 0)
                result.AddError($"{prefix} committed action branch schema version is missing.");
            if (required && !HasBranch)
                result.AddError($"{prefix} committed action branch is required.");
            if (string.IsNullOrWhiteSpace(BranchId))
                result.AddError($"{prefix} committed action branch id is missing.");
            if (string.IsNullOrWhiteSpace(RootNodeId))
                result.AddError($"{prefix} committed action branch root node id is missing.");
            if (defaultBodyKind == BodyOccupancyKind.None)
                result.AddError($"{prefix} committed action branch body claim is missing.");
            if (defaultChannels == CharacterFrameOutputChannel.None)
                result.AddError($"{prefix} committed action branch output channels are missing.");

            Dictionary<string, CommittedActionBranchNodeAuthoring> nodeMap =
                new Dictionary<string, CommittedActionBranchNodeAuthoring>(StringComparer.Ordinal);
            HashSet<string> duplicateIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<CommittedActionBranchNodeAuthoring> source = Nodes;
            ActionFactCompileContext factContext = BuildFactContext(source, actionState, sourceStep, in compileContext);
            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                string nodePrefix = $"{prefix} branch node {i}";
                if (!node.HasNodeId)
                {
                    result.AddError($"{nodePrefix} id is missing.");
                    continue;
                }

                if (nodeMap.ContainsKey(node.NodeId))
                {
                    duplicateIds.Add(node.NodeId);
                    result.AddError($"{nodePrefix} id is duplicate:{node.NodeId}.");
                    continue;
                }

                nodeMap.Add(node.NodeId, node);
                ValidateNode(result, nodePrefix, node, actionState, sourceStep, in compileContext, factContext);
            }

            if (!string.IsNullOrWhiteSpace(RootNodeId) && !nodeMap.ContainsKey(RootNodeId))
                result.AddError($"{prefix} committed action branch root node is missing:{RootNodeId}.");
            if (nodeMap.TryGetValue(RootNodeId, out CommittedActionBranchNodeAuthoring rootNode) &&
                rootNode.Kind != CommittedActionNodeKind.Root)
                result.AddError($"{prefix} committed action branch root node must be Branch Root:{RootNodeId}.");

            for (int i = 0; i < source.Count; i++)
            {
                CommittedActionBranchNodeAuthoring node = source[i];
                if (!node.HasNodeId || duplicateIds.Contains(node.NodeId))
                    continue;

                IReadOnlyList<string> childIds = node.ChildNodeIds;
                for (int childIndex = 0; childIndex < childIds.Count; childIndex++)
                {
                    string childId = childIds[childIndex] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(childId))
                    {
                        result.AddError($"{prefix} branch node {node.NodeId} child id is missing:{childIndex}.");
                        continue;
                    }

                    if (!nodeMap.TryGetValue(childId, out CommittedActionBranchNodeAuthoring child))
                    {
                        result.AddError($"{prefix} branch node {node.NodeId} child is missing:{childId}.");
                        continue;
                    }

                    if (node.Kind == CommittedActionNodeKind.Timeline &&
                        child.Kind != CommittedActionNodeKind.Condition)
                    {
                        result.AddError($"{prefix} branch node {node.NodeId} timeline child must be condition:{childId}.");
                    }
                }
            }

            if (nodeMap.ContainsKey(RootNodeId) &&
                HasCycle(RootNodeId, nodeMap, new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal)))
            {
                result.AddError($"{prefix} committed action branch has cycle:{RootNodeId}.");
            }
        }

        static void ValidateNode(
            CharacterActionCatalogValidationResult result,
            string prefix,
            CommittedActionBranchNodeAuthoring node,
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext,
            ActionFactCompileContext factContext)
        {
            switch (node.Kind)
            {
                case CommittedActionNodeKind.Root:
                    if (node.ChildNodeIds.Count != 1)
                        result.AddError($"{prefix} root must have exactly one child.");
                    break;
                case CommittedActionNodeKind.Selector:
                    if (node.ChildNodeIds.Count == 0)
                        result.AddError($"{prefix} selector has no child.");
                    break;
                case CommittedActionNodeKind.Condition:
                    ValidateCondition(result, prefix, node.Condition, factContext);
                    if (node.ChildNodeIds.Count == 0)
                        result.AddError($"{prefix} condition has no child.");
                    break;
                case CommittedActionNodeKind.Timeline:
                    node.Timeline.ValidateTimelineNodeInto(result, prefix, actionState, sourceStep, in compileContext);
                    break;
                default:
                    result.AddError($"{prefix} kind is invalid.");
                    break;
            }
        }

        static void ValidateCondition(
            CharacterActionCatalogValidationResult result,
            string prefix,
            CommittedActionBranchConditionAuthoring condition,
            ActionFactCompileContext factContext)
        {
            if (!condition.IsDefined)
            {
                result.AddError($"{prefix} condition is missing.");
                return;
            }

            switch (condition.Kind)
            {
                case CommittedActionConditionKind.RequiredFactActive:
                    TimelineFactId factId = new TimelineFactId(condition.RequiredFactId);
                    if (!factId.IsValid)
                    {
                        result.AddError($"{prefix} condition required fact id is missing.");
                        return;
                    }
                    if (!ActionFactIdResolver.IsValidFactId(factId.Value))
                    {
                        result.AddError($"{prefix} condition required fact id is invalid:{factId.Value}.");
                        return;
                    }
                    if (!ActionFactIdResolver.TryResolve(factContext, factId, out _))
                        result.AddError($"{prefix} condition required fact id is missing from action fact context:{factId.Value}.");
                    break;
                case CommittedActionConditionKind.ActionVariantEquals:
                    if (condition.ExpectedVariant == CharacterStateVariant.None)
                        result.AddError($"{prefix} condition expected variant is missing.");
                    break;
                case CommittedActionConditionKind.None:
                    result.AddError($"{prefix} condition is missing.");
                    break;
            }
        }

        static bool HasCycle(
            string nodeId,
            IReadOnlyDictionary<string, CommittedActionBranchNodeAuthoring> nodeMap,
            HashSet<string> visiting,
            HashSet<string> visited)
        {
            if (visited.Contains(nodeId))
                return false;
            if (!visiting.Add(nodeId))
                return true;
            if (!nodeMap.TryGetValue(nodeId, out CommittedActionBranchNodeAuthoring node))
                return false;

            IReadOnlyList<string> childIds = node.ChildNodeIds;
            for (int i = 0; i < childIds.Count; i++)
            {
                string childId = childIds[i] ?? string.Empty;
                if (!nodeMap.ContainsKey(childId))
                    continue;
                if (visiting.Contains(childId) && IsAllowedTimelineLoop(node, childId, nodeMap))
                    continue;
                if (HasCycle(childId, nodeMap, visiting, visited))
                    return true;
            }

            visiting.Remove(nodeId);
            visited.Add(nodeId);
            return false;
        }

        static bool IsAllowedTimelineLoop(
            CommittedActionBranchNodeAuthoring node,
            string childId,
            IReadOnlyDictionary<string, CommittedActionBranchNodeAuthoring> nodeMap)
        {
            return node.Kind == CommittedActionNodeKind.Condition &&
                   nodeMap.TryGetValue(childId, out CommittedActionBranchNodeAuthoring child) &&
                   child.Kind == CommittedActionNodeKind.Timeline;
        }

        BodyOccupancyClaim ToClaim(int sourceStep)
        {
            CharacterBodyDomain domain = defaultBodyKind == BodyOccupancyKind.UpperBody
                ? CharacterBodyDomain.UpperBody
                : CharacterBodyDomain.CommittedAction;

            return defaultBodyKind == BodyOccupancyKind.None
                ? BodyOccupancyClaim.None(sourceStep)
                : new BodyOccupancyClaim(domain, defaultBodyKind, defaultChannels, sourceStep);
        }
    }

    [Serializable]
    public struct CommittedActionBranchTimelineAuthoring
    {
        [SerializeField] bool required;
        [SerializeField] string branchId;
        [SerializeField] string timelineNodeId;
        [SerializeField, Min(0f)] float durationSeconds;
        [SerializeField, HideInInspector, FormerlySerializedAs("durationFrames")] int legacyDurationFrames;
        [SerializeField] BodyOccupancyKind defaultBodyKind;
        [SerializeField] CharacterFrameOutputChannel defaultChannels;
        [SerializeField] ActionTimelineTrackAuthoring[] tracks;

        public CommittedActionBranchTimelineAuthoring(
            bool required,
            string branchId,
            string timelineNodeId,
            float durationSeconds,
            BodyOccupancyKind defaultBodyKind,
            CharacterFrameOutputChannel defaultChannels,
            ActionTimelineTrackAuthoring[] tracks)
        {
            this.required = required;
            this.branchId = branchId ?? string.Empty;
            this.timelineNodeId = timelineNodeId ?? string.Empty;
            this.durationSeconds = durationSeconds;
            legacyDurationFrames = 0;
            this.defaultBodyKind = defaultBodyKind;
            this.defaultChannels = defaultChannels;
            this.tracks = tracks ?? Array.Empty<ActionTimelineTrackAuthoring>();
        }

        public bool Required => required;
        public string BranchId => branchId ?? string.Empty;
        public string TimelineNodeId => timelineNodeId ?? string.Empty;
        public float DurationSeconds => durationSeconds;
        public int LegacyDurationFrames => Mathf.Max(0, legacyDurationFrames);
        public BodyOccupancyKind DefaultBodyKind => defaultBodyKind;
        public CharacterFrameOutputChannel DefaultChannels => defaultChannels;
        public IReadOnlyList<ActionTimelineTrackAuthoring> Tracks => tracks ?? Array.Empty<ActionTimelineTrackAuthoring>();
        public bool HasTimeline => Tracks.Count > 0;

        public ActionTimelineDefinition ToActionTimelineDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            if (!HasTimeline)
                return ActionTimelineDefinition.Empty;

            ActionTimelineTrackDefinition[] runtimeTracks = new ActionTimelineTrackDefinition[Tracks.Count];
            for (int i = 0; i < Tracks.Count; i++)
                runtimeTracks[i] = Tracks[i].ToDefinition(actionState, sourceStep, in compileContext);

            return new ActionTimelineDefinition(
                actionState,
                ActionTimelineQuantizer.QuantizeSecondsToTick(DurationSeconds, in compileContext),
                runtimeTracks);
        }

        public CommittedActionBranchDefinition ToCommittedActionBranchDefinition(
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            if (!HasTimeline)
                return CommittedActionBranchDefinition.Empty;

            CommittedActionNodeDefinition timeline = CommittedActionNodeDefinition.Timeline(
                TimelineNodeId,
                ToActionTimelineDefinition(actionState, sourceStep, in compileContext));
            return new CommittedActionBranchDefinition(
                new CommittedActionBranchId(BranchId),
                actionState,
                CommittedActionNodeDefinition.Root(
                    $"branch.root.{BranchId}",
                    timeline.NodeId),
                ToClaim(sourceStep),
                new[] { timeline });
        }

        public void ValidateInto(
            CharacterActionCatalogValidationResult result,
            string prefix,
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            if (!required && !HasTimeline)
                return;

            int errorCount = result.Errors.Count;
            if (required && !HasTimeline)
                result.AddError($"{prefix} committed action branch timeline is required.");
            if (string.IsNullOrWhiteSpace(BranchId))
                result.AddError($"{prefix} committed action branch id is missing.");
            if (string.IsNullOrWhiteSpace(TimelineNodeId))
                result.AddError($"{prefix} timeline node id is missing.");
            if (defaultBodyKind == BodyOccupancyKind.None)
                result.AddError($"{prefix} committed action branch body claim is missing.");
            if (defaultChannels == CharacterFrameOutputChannel.None)
                result.AddError($"{prefix} committed action branch output channels are missing.");

            ValidateAuthoringSeconds(result, prefix);
            if (result.Errors.Count > errorCount)
                return;

            ActionTimelineValidationResult timelineResult = ActionTimelineValidator.Validate(
                ToCommittedActionBranchDefinition(actionState, sourceStep, in compileContext).RootNode.TimelineNode.Timeline);
            for (int i = 0; i < timelineResult.Errors.Count; i++)
                result.AddError($"{prefix} {timelineResult.Errors[i]}");
            for (int i = 0; i < timelineResult.Warnings.Count; i++)
                result.AddWarning($"{prefix} {timelineResult.Warnings[i]}");
        }

        public void ValidateTimelineNodeInto(
            CharacterActionCatalogValidationResult result,
            string prefix,
            ActionStateId actionState,
            int sourceStep,
            in ActionTimelineCompileContext compileContext)
        {
            int errorCount = result.Errors.Count;
            if (!HasTimeline)
                result.AddError($"{prefix} timeline is required.");

            ValidateAuthoringSeconds(result, prefix);
            if (result.Errors.Count > errorCount)
                return;

            ActionTimelineValidationResult timelineResult = ActionTimelineValidator.Validate(
                ToActionTimelineDefinition(actionState, sourceStep, in compileContext));
            for (int i = 0; i < timelineResult.Errors.Count; i++)
                result.AddError($"{prefix} {timelineResult.Errors[i]}");
            for (int i = 0; i < timelineResult.Warnings.Count; i++)
                result.AddWarning($"{prefix} {timelineResult.Warnings[i]}");
        }

        void ValidateAuthoringSeconds(CharacterActionCatalogValidationResult result, string prefix)
        {
            if (float.IsNaN(DurationSeconds) || float.IsInfinity(DurationSeconds) || DurationSeconds < 0f)
                result.AddError($"{prefix} timeline duration seconds is invalid.");

            for (int trackIndex = 0; trackIndex < Tracks.Count; trackIndex++)
            {
                ActionTimelineTrackAuthoring track = Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    ActionTimelineClipAuthoring clip = track.Clips[clipIndex];
                    if (float.IsNaN(clip.StartSeconds) || float.IsInfinity(clip.StartSeconds) || clip.StartSeconds < 0f)
                        result.AddError($"{prefix} clip start seconds is invalid:{trackIndex}:{clipIndex}.");
                    if (float.IsNaN(clip.EndSeconds) || float.IsInfinity(clip.EndSeconds) || clip.EndSeconds < 0f)
                        result.AddError($"{prefix} clip end seconds is invalid:{trackIndex}:{clipIndex}.");
                    if (clip.EndSeconds < clip.StartSeconds)
                        result.AddError($"{prefix} clip seconds range is invalid:{trackIndex}:{clipIndex}.");
                }
            }
        }

        BodyOccupancyClaim ToClaim(int sourceStep)
        {
            CharacterBodyDomain domain = defaultBodyKind == BodyOccupancyKind.UpperBody
                ? CharacterBodyDomain.UpperBody
                : CharacterBodyDomain.CommittedAction;

            return defaultBodyKind == BodyOccupancyKind.None
                ? BodyOccupancyClaim.None(sourceStep)
                : new BodyOccupancyClaim(domain, defaultBodyKind, defaultChannels, sourceStep);
        }
    }
}
