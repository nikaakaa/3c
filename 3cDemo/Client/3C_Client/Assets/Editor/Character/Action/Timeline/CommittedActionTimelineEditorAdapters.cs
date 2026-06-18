using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacterBehavior.Editor.ActionTimeline
{
    public enum CommittedActionTimelineVariant
    {
        Directional = 0,
        Backstep = 1,
        Generic = 2
    }

    public interface ICommittedActionTimelineEditorModel
    {
        CharacterActionDefinitionSO ActionDefinition { get; }
        SerializedObject SerializedObject { get; }
        bool IsValid { get; }
        bool IsDodge { get; }
        IReadOnlyList<CommittedActionTimelineVariant> Variants { get; }
        bool TryGetTimelineProperty(CommittedActionTimelineVariant variant, out SerializedProperty timeline, out string diagnostic);
    }

    public readonly struct CommittedActionTimelineClipIdentity : IEquatable<CommittedActionTimelineClipIdentity>
    {
        public CommittedActionTimelineClipIdentity(
            CommittedActionTimelineVariant variant,
            string trackStableId,
            string clipStableId)
        {
            Variant = variant;
            TrackStableId = trackStableId ?? string.Empty;
            ClipStableId = clipStableId ?? string.Empty;
        }

        public CommittedActionTimelineVariant Variant { get; }
        public string TrackStableId { get; }
        public string ClipStableId { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(TrackStableId) && !string.IsNullOrWhiteSpace(ClipStableId);

        public bool Equals(CommittedActionTimelineClipIdentity other)
        {
            return Variant == other.Variant &&
                   string.Equals(TrackStableId, other.TrackStableId, StringComparison.Ordinal) &&
                   string.Equals(ClipStableId, other.ClipStableId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CommittedActionTimelineClipIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Variant;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TrackStableId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ClipStableId);
                return hash;
            }
        }
    }

    public readonly struct CommittedActionTimelineTrackSnapshot
    {
        public CommittedActionTimelineTrackSnapshot(
            CommittedActionTimelineVariant variant,
            int index,
            string stableId,
            ActionTimelineTrackKind kind,
            int clipCount,
            string propertyPath)
        {
            Variant = variant;
            Index = index;
            StableId = stableId ?? string.Empty;
            Kind = kind;
            ClipCount = Mathf.Max(0, clipCount);
            PropertyPath = propertyPath ?? string.Empty;
        }

        public CommittedActionTimelineVariant Variant { get; }
        public int Index { get; }
        public string StableId { get; }
        public ActionTimelineTrackKind Kind { get; }
        public int ClipCount { get; }
        public string PropertyPath { get; }
    }

    public readonly struct CommittedActionTimelineClipSnapshot
    {
        public CommittedActionTimelineClipSnapshot(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string trackStableId,
            string clipStableId,
            ActionTimelineClipKind kind,
            float startSeconds,
            float endSeconds,
            string propertyPath,
            string label,
            bool invalid)
        {
            Variant = variant;
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            TrackStableId = trackStableId ?? string.Empty;
            ClipStableId = clipStableId ?? string.Empty;
            Kind = kind;
            StartSeconds = Mathf.Max(0f, startSeconds);
            EndSeconds = Mathf.Max(StartSeconds, endSeconds);
            PropertyPath = propertyPath ?? string.Empty;
            Label = label ?? string.Empty;
            Invalid = invalid;
        }

        public CommittedActionTimelineVariant Variant { get; }
        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public string TrackStableId { get; }
        public string ClipStableId { get; }
        public ActionTimelineClipKind Kind { get; }
        public float StartSeconds { get; }
        public float EndSeconds { get; }
        public string PropertyPath { get; }
        public string Label { get; }
        public bool Invalid { get; }
        public CommittedActionTimelineClipIdentity Identity =>
            new CommittedActionTimelineClipIdentity(Variant, TrackStableId, ClipStableId);
    }

    public sealed class CommittedActionTimelineEditorSnapshot
    {
        public CommittedActionTimelineEditorSnapshot(
            CommittedActionTimelineVariant variant,
            float durationSeconds,
            CommittedActionTimelineTrackSnapshot[] tracks,
            CommittedActionTimelineClipSnapshot[] clips,
            CommittedActionTimelineEditorValidationResult validation)
        {
            Variant = variant;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            this.tracks = tracks ?? Array.Empty<CommittedActionTimelineTrackSnapshot>();
            this.clips = clips ?? Array.Empty<CommittedActionTimelineClipSnapshot>();
            Validation = validation ?? new CommittedActionTimelineEditorValidationResult();
        }

        readonly CommittedActionTimelineTrackSnapshot[] tracks;
        readonly CommittedActionTimelineClipSnapshot[] clips;

        public CommittedActionTimelineVariant Variant { get; }
        public float DurationSeconds { get; }
        public IReadOnlyList<CommittedActionTimelineTrackSnapshot> Tracks => tracks;
        public IReadOnlyList<CommittedActionTimelineClipSnapshot> Clips => clips;
        public CommittedActionTimelineEditorValidationResult Validation { get; }
    }

    public sealed class CommittedActionTimelineEditorModel
    {
        readonly CommittedActionTimelineSerializedAdapter adapter;

        public CommittedActionTimelineEditorModel(CommittedActionTimelineSerializedAdapter adapter)
        {
            this.adapter = adapter;
        }

        public CommittedActionTimelineEditorSnapshot Capture(CommittedActionTimelineVariant variant)
        {
            if (adapter == null || !adapter.IsValid ||
                !adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out _))
            {
                return new CommittedActionTimelineEditorSnapshot(
                    variant,
                    0,
                    Array.Empty<CommittedActionTimelineTrackSnapshot>(),
                    Array.Empty<CommittedActionTimelineClipSnapshot>(),
                    CommittedActionTimelineEditorValidator.Validate(adapter));
            }

            SerializedProperty duration = timeline.FindPropertyRelative("durationSeconds");
            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            List<CommittedActionTimelineTrackSnapshot> trackSnapshots = new List<CommittedActionTimelineTrackSnapshot>();
            List<CommittedActionTimelineClipSnapshot> clipSnapshots = new List<CommittedActionTimelineClipSnapshot>();
            for (int trackIndex = 0; tracks != null && trackIndex < tracks.arraySize; trackIndex++)
            {
                SerializedProperty track = tracks.GetArrayElementAtIndex(trackIndex);
                string trackId = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
                ActionTimelineTrackKind trackKind = CommittedActionTimelineSerializedAdapter.ReadTrackKind(track);
                SerializedProperty clips = track.FindPropertyRelative("clips");
                int clipCount = clips != null ? clips.arraySize : 0;
                trackSnapshots.Add(new CommittedActionTimelineTrackSnapshot(
                    variant,
                    trackIndex,
                    trackId,
                    trackKind,
                    clipCount,
                    track.propertyPath));

                for (int clipIndex = 0; clips != null && clipIndex < clips.arraySize; clipIndex++)
                {
                    SerializedProperty clip = clips.GetArrayElementAtIndex(clipIndex);
                    ActionTimelineClipKind clipKind = CommittedActionTimelineSerializedAdapter.ReadClipKind(clip);
                    float start = clip.FindPropertyRelative("startSeconds").floatValue;
                    float end = clip.FindPropertyRelative("endSeconds").floatValue;
                    clipSnapshots.Add(new CommittedActionTimelineClipSnapshot(
                        variant,
                        trackIndex,
                        clipIndex,
                        trackId,
                        CommittedActionTimelineSerializedAdapter.ReadStableId(clip),
                        clipKind,
                        start,
                        end,
                        clip.propertyPath,
                        clipKind.ToString(),
                        CommittedActionTimelineEditorValidator.IsClipInvalid(track, clip)));
                }
            }

            return new CommittedActionTimelineEditorSnapshot(
                variant,
                duration != null ? duration.floatValue : 0f,
                trackSnapshots.ToArray(),
                clipSnapshots.ToArray(),
                CommittedActionTimelineEditorValidator.Validate(adapter));
        }

        public bool TryResolveClip(
            CommittedActionTimelineClipIdentity identity,
            out int trackIndex,
            out int clipIndex,
            out string clipPath)
        {
            trackIndex = -1;
            clipIndex = -1;
            clipPath = string.Empty;
            if (adapter == null || !identity.IsValid ||
                !adapter.TryGetTimelineProperty(identity.Variant, out SerializedProperty timeline, out _))
                return false;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            for (int i = 0; tracks != null && i < tracks.arraySize; i++)
            {
                SerializedProperty track = tracks.GetArrayElementAtIndex(i);
                if (!string.Equals(CommittedActionTimelineSerializedAdapter.ReadStableId(track), identity.TrackStableId, StringComparison.Ordinal))
                    continue;

                SerializedProperty clips = track.FindPropertyRelative("clips");
                for (int j = 0; clips != null && j < clips.arraySize; j++)
                {
                    SerializedProperty clip = clips.GetArrayElementAtIndex(j);
                    if (!string.Equals(CommittedActionTimelineSerializedAdapter.ReadStableId(clip), identity.ClipStableId, StringComparison.Ordinal))
                        continue;

                    trackIndex = i;
                    clipIndex = j;
                    clipPath = clip.propertyPath;
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class CommittedActionTimelineSerializedAdapter : ICommittedActionTimelineEditorModel
    {
        const string StableIdPropertyName = "stableId";

        readonly CharacterActionDefinitionSO actionDefinition;
        readonly SerializedObject serializedObject;
        readonly string selectedTimelineNodeId;

        public CommittedActionTimelineSerializedAdapter(CharacterActionDefinitionSO actionDefinition)
            : this(actionDefinition, actionDefinition != null ? new SerializedObject(actionDefinition) : null, string.Empty)
        {
        }

        public CommittedActionTimelineSerializedAdapter(
            CharacterActionDefinitionSO actionDefinition,
            string selectedTimelineNodeId)
            : this(
                actionDefinition,
                actionDefinition != null ? new SerializedObject(actionDefinition) : null,
                selectedTimelineNodeId)
        {
        }

        public CommittedActionTimelineSerializedAdapter(
            CharacterActionDefinitionSO actionDefinition,
            SerializedObject serializedObject)
            : this(actionDefinition, serializedObject, string.Empty)
        {
        }

        public CommittedActionTimelineSerializedAdapter(
            CharacterActionDefinitionSO actionDefinition,
            SerializedObject serializedObject,
            string selectedTimelineNodeId)
        {
            this.actionDefinition = actionDefinition;
            this.serializedObject = serializedObject;
            this.selectedTimelineNodeId = selectedTimelineNodeId ?? string.Empty;
            EnsureStableIds();
        }

        public CharacterActionDefinitionSO ActionDefinition => actionDefinition;
        public SerializedObject SerializedObject => serializedObject;
        public bool IsValid => actionDefinition != null && serializedObject != null;
        public bool IsDodge => IsValid && actionDefinition.ActionState.Matches(ActionStateIds.Dodge);

        public bool TryGetTimelineProperty(
            CommittedActionTimelineVariant variant,
            out SerializedProperty timeline,
            out string diagnostic)
        {
            timeline = null;
            diagnostic = string.Empty;
            if (!IsValid)
            {
                diagnostic = "action-definition-missing";
                return false;
            }

            serializedObject.UpdateIfRequiredOrScript();
            string nodeId = ResolveTimelineNodeId(variant);
            return TryGetTimelinePropertyByNodeId(nodeId, out timeline, out diagnostic);
        }

        public bool TryGetTimelinePropertyByNodeId(
            string timelineNodeId,
            out SerializedProperty timeline,
            out string diagnostic)
        {
            timeline = null;
            diagnostic = string.Empty;
            if (!IsValid)
            {
                diagnostic = "action-definition-missing";
                return false;
            }

            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty branch = serializedObject.FindProperty("committedActionBranch");
            if (branch == null)
            {
                diagnostic = "committed-action-branch-property-missing";
                return false;
            }

            SerializedProperty nodes = branch.FindPropertyRelative("nodes");
            if (nodes == null)
            {
                diagnostic = "committed-action-branch-nodes-missing";
                return false;
            }

            for (int i = 0; i < nodes.arraySize; i++)
            {
                SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                if ((CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex != CommittedActionNodeKind.Timeline)
                    continue;

                string nodeId = node.FindPropertyRelative("nodeId").stringValue;
                if (!string.IsNullOrWhiteSpace(timelineNodeId) &&
                    !string.Equals(nodeId, timelineNodeId, StringComparison.Ordinal))
                {
                    continue;
                }

                timeline = node.FindPropertyRelative("timeline");
                if (timeline == null)
                    diagnostic = $"timeline-property-missing:{nodeId}";
                return timeline != null;
            }

            diagnostic = string.IsNullOrWhiteSpace(timelineNodeId)
                ? "timeline-node-missing"
                : $"timeline-node-missing:{timelineNodeId}";
            return false;
        }

        public IReadOnlyList<CommittedActionTimelineVariant> Variants
        {
            get
            {
                if (!IsValid)
                    return Array.Empty<CommittedActionTimelineVariant>();

                serializedObject.UpdateIfRequiredOrScript();
                if (!string.IsNullOrWhiteSpace(selectedTimelineNodeId))
                    return new[] { VariantForNodeId(selectedTimelineNodeId) };

                List<CommittedActionTimelineVariant> variants = new List<CommittedActionTimelineVariant>();
                SerializedProperty branch = serializedObject.FindProperty("committedActionBranch");
                SerializedProperty nodes = branch?.FindPropertyRelative("nodes");
                for (int i = 0; nodes != null && i < nodes.arraySize; i++)
                {
                    SerializedProperty node = nodes.GetArrayElementAtIndex(i);
                    if ((CommittedActionNodeKind)node.FindPropertyRelative("kind").enumValueIndex != CommittedActionNodeKind.Timeline)
                        continue;

                    CommittedActionTimelineVariant variant =
                        VariantForNodeId(node.FindPropertyRelative("nodeId").stringValue);
                    if (!variants.Contains(variant))
                        variants.Add(variant);
                }

                return variants.Count > 0
                    ? variants.ToArray()
                    : new[] { CommittedActionTimelineVariant.Generic };
            }
        }

        string ResolveTimelineNodeId(CommittedActionTimelineVariant variant)
        {
            if (!string.IsNullOrWhiteSpace(selectedTimelineNodeId))
                return selectedTimelineNodeId;
            switch (variant)
            {
                case CommittedActionTimelineVariant.Directional:
                    return "timeline.dodge.directional";
                case CommittedActionTimelineVariant.Backstep:
                    return "timeline.dodge.backstep";
                default:
                    return string.Empty;
            }
        }

        static CommittedActionTimelineVariant VariantForNodeId(string nodeId)
        {
            string value = nodeId ?? string.Empty;
            if (value.IndexOf("backstep", StringComparison.OrdinalIgnoreCase) >= 0)
                return CommittedActionTimelineVariant.Backstep;
            if (value.IndexOf("directional", StringComparison.OrdinalIgnoreCase) >= 0)
                return CommittedActionTimelineVariant.Directional;
            return CommittedActionTimelineVariant.Generic;
        }

        public bool AddTrack(
            CommittedActionTimelineVariant variant,
            ActionTimelineTrackKind kind,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (kind == ActionTimelineTrackKind.None)
            {
                diagnostic = "track-kind-invalid";
                return false;
            }

            if (!BeginEdit(variant, "Add Track", out SerializedProperty timeline, out diagnostic))
                return false;

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            int index = tracks.arraySize;
            tracks.InsertArrayElementAtIndex(index);
            InitializeTrack(tracks.GetArrayElementAtIndex(index), kind);
            EndEdit();
            return true;
        }

        public bool RemoveTrack(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            out string diagnostic)
        {
            if (!BeginTrackEdit(variant, trackIndex, "Remove Track", out SerializedProperty tracks, out _, out diagnostic))
                return false;

            tracks.DeleteArrayElementAtIndex(trackIndex);
            EndEdit();
            return true;
        }

        public bool ReorderTrack(
            CommittedActionTimelineVariant variant,
            int fromIndex,
            int toIndex,
            out string diagnostic)
        {
            if (!BeginTrackEdit(variant, fromIndex, "Reorder Track", out SerializedProperty tracks, out _, out diagnostic))
                return false;

            int target = Mathf.Clamp(toIndex, 0, tracks.arraySize - 1);
            tracks.MoveArrayElement(fromIndex, target);
            EndEdit();
            return true;
        }

        public bool AddClip(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            ActionTimelineClipKind kind,
            float startSeconds,
            float endSeconds,
            out string diagnostic)
        {
            if (!BeginTrackEdit(variant, trackIndex, "Add Clip", out _, out SerializedProperty track, out diagnostic))
                return false;

            ActionTimelineTrackKind trackKind = ReadTrackKind(track);
            if (!IsClipKindAllowed(trackKind, kind))
            {
                diagnostic = $"clip-kind-not-allowed:{trackKind}:{kind}";
                return false;
            }

            SerializedProperty clips = track.FindPropertyRelative("clips");
            int index = clips.arraySize;
            clips.InsertArrayElementAtIndex(index);
            InitializeClip(clips.GetArrayElementAtIndex(index), kind, startSeconds, endSeconds);
            EndEdit();
            return true;
        }

        public bool RemoveClip(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Remove Clip", out _, out SerializedProperty clips, out _, out diagnostic))
                return false;

            clips.DeleteArrayElementAtIndex(clipIndex);
            EndEdit();
            return true;
        }

        public bool MoveClip(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            float startSeconds,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Move Clip", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            SerializedProperty start = clip.FindPropertyRelative("startSeconds");
            SerializedProperty end = clip.FindPropertyRelative("endSeconds");
            float length = Mathf.Max(0f, end.floatValue - start.floatValue);
            float clampedStart = Mathf.Max(0f, startSeconds);
            start.floatValue = clampedStart;
            end.floatValue = clampedStart + length;
            EndEdit();
            return true;
        }

        public bool MoveClipRange(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            float startSeconds,
            float endSeconds,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Move Clip", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            SetClipRange(clip, startSeconds, endSeconds);
            EndEdit();
            return true;
        }

        public bool ResizeClip(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            float startSeconds,
            float endSeconds,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Resize Clip", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            SetClipRange(clip, startSeconds, endSeconds);
            EndEdit();
            return true;
        }

        public bool SetAnimationKey(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            ActionAnimationKey key,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Edit Animation Key", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            clip.FindPropertyRelative("payload").FindPropertyRelative("animationKey").stringValue = key.Value;
            EndEdit();
            return true;
        }

        public bool SetMotionPayload(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            CharacterStateId sourceState,
            CharacterStateVariant motionVariant,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatchOnComplete,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Edit Motion", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            SerializedProperty payload = clip.FindPropertyRelative("payload");
            payload.FindPropertyRelative("motionSourceStateId").stringValue = sourceState.Value;
            payload.FindPropertyRelative("motionVariant").enumValueIndex = (int)motionVariant;
            payload.FindPropertyRelative("motionDuration").floatValue = Mathf.Max(0f, duration);
            payload.FindPropertyRelative("motionDistance").floatValue = Mathf.Max(0f, distance);
            payload.FindPropertyRelative("rotateToDirection").boolValue = rotateToDirection;
            payload.FindPropertyRelative("setRunLatchOnComplete").boolValue = setRunLatchOnComplete;
            EndEdit();
            return true;
        }

        public bool SetFactPayload(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string factId,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Edit Window Fact", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            clip.FindPropertyRelative("payload").FindPropertyRelative("factId").stringValue = factId ?? string.Empty;
            EndEdit();
            return true;
        }

        public bool SetCuePayload(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string cueId,
            out string diagnostic)
        {
            if (!BeginClipEdit(variant, trackIndex, clipIndex, "Edit Cue", out _, out _, out SerializedProperty clip, out diagnostic))
                return false;

            clip.FindPropertyRelative("payload").FindPropertyRelative("cueId").stringValue = cueId ?? string.Empty;
            EndEdit();
            return true;
        }

        public bool Save(out CommittedActionTimelineEditorValidationResult validation)
        {
            validation = CommittedActionTimelineEditorValidator.Validate(this);
            if (!IsValid)
                return false;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(actionDefinition);
            AssetDatabase.SaveAssets();
            return !validation.HasErrors;
        }

        public bool EnsureStableIds()
        {
            if (!IsValid)
                return false;

            bool changed = false;
            Undo.RecordObject(actionDefinition, "Committed Action Timeline Editor: Ensure Stable Ids");
            serializedObject.UpdateIfRequiredOrScript();
            IReadOnlyList<CommittedActionTimelineVariant> variants = Variants;
            for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
            {
                if (!TryGetTimelineProperty(variants[variantIndex], out SerializedProperty timeline, out _))
                    continue;

                bool timelineChanged = false;
                SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
                for (int trackIndex = 0; tracks != null && trackIndex < tracks.arraySize; trackIndex++)
                {
                    SerializedProperty track = tracks.GetArrayElementAtIndex(trackIndex);
                    timelineChanged |= EnsureStableId(track, "track");
                    SerializedProperty clips = track.FindPropertyRelative("clips");
                    for (int clipIndex = 0; clips != null && clipIndex < clips.arraySize; clipIndex++)
                        timelineChanged |= EnsureStableId(clips.GetArrayElementAtIndex(clipIndex), "clip");
                }

                if (!timelineChanged)
                    continue;

                changed = true;
                serializedObject.ApplyModifiedProperties();
            }

            if (!changed)
                return false;

            EditorUtility.SetDirty(actionDefinition);
            return true;
        }

        public bool TryGetClipIdentity(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            out CommittedActionTimelineClipIdentity identity,
            out string diagnostic)
        {
            identity = default;
            if (!BeginClipRead(variant, trackIndex, clipIndex, out SerializedProperty track, out _, out SerializedProperty clip, out diagnostic))
                return false;

            identity = new CommittedActionTimelineClipIdentity(
                variant,
                ReadStableId(track),
                ReadStableId(clip));
            return identity.IsValid;
        }

        public static ActionTimelineClipKind DefaultClipKind(ActionTimelineTrackKind trackKind)
        {
            switch (trackKind)
            {
                case ActionTimelineTrackKind.Animation:
                    return ActionTimelineClipKind.AnimationKey;
                case ActionTimelineTrackKind.Motion:
                    return ActionTimelineClipKind.Motion;
                case ActionTimelineTrackKind.Hitbox:
                    return ActionTimelineClipKind.HitboxWindow;
                case ActionTimelineTrackKind.Cancel:
                    return ActionTimelineClipKind.CancelWindow;
                case ActionTimelineTrackKind.Cue:
                    return ActionTimelineClipKind.Cue;
                default:
                    return ActionTimelineClipKind.None;
            }
        }

        public static bool IsClipKindAllowed(ActionTimelineTrackKind trackKind, ActionTimelineClipKind clipKind)
        {
            return DefaultClipKind(trackKind) == clipKind;
        }

        public static string ReadStableId(SerializedProperty property)
        {
            return property?.FindPropertyRelative(StableIdPropertyName)?.stringValue ?? string.Empty;
        }

        public static ActionTimelineTrackKind ReadTrackKind(SerializedProperty track)
        {
            SerializedProperty kind = track?.FindPropertyRelative("kind");
            return kind == null ? ActionTimelineTrackKind.None : (ActionTimelineTrackKind)kind.enumValueIndex;
        }

        public static ActionTimelineClipKind ReadClipKind(SerializedProperty clip)
        {
            SerializedProperty kind = clip?.FindPropertyRelative("kind");
            return kind == null ? ActionTimelineClipKind.None : (ActionTimelineClipKind)kind.enumValueIndex;
        }

        bool BeginEdit(
            CommittedActionTimelineVariant variant,
            string undoName,
            out SerializedProperty timeline,
            out string diagnostic)
        {
            if (!TryGetTimelineProperty(variant, out timeline, out diagnostic))
                return false;

            Undo.RecordObject(actionDefinition, $"Committed Action Timeline Editor: {undoName}");
            return true;
        }

        bool BeginClipRead(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            out SerializedProperty track,
            out SerializedProperty clips,
            out SerializedProperty clip,
            out string diagnostic)
        {
            clips = null;
            clip = null;
            if (!TryGetTimelineProperty(variant, out SerializedProperty timeline, out diagnostic))
            {
                track = null;
                return false;
            }

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.arraySize)
            {
                track = null;
                diagnostic = $"track-index-invalid:{trackIndex}";
                return false;
            }

            track = tracks.GetArrayElementAtIndex(trackIndex);
            clips = track.FindPropertyRelative("clips");
            if (clips == null || clipIndex < 0 || clipIndex >= clips.arraySize)
            {
                diagnostic = $"clip-index-invalid:{trackIndex}:{clipIndex}";
                return false;
            }

            clip = clips.GetArrayElementAtIndex(clipIndex);
            return true;
        }

        bool BeginTrackEdit(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            string undoName,
            out SerializedProperty tracks,
            out SerializedProperty track,
            out string diagnostic)
        {
            tracks = null;
            track = null;
            if (!BeginEdit(variant, undoName, out SerializedProperty timeline, out diagnostic))
                return false;

            tracks = timeline.FindPropertyRelative("tracks");
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.arraySize)
            {
                diagnostic = $"track-index-invalid:{trackIndex}";
                return false;
            }

            track = tracks.GetArrayElementAtIndex(trackIndex);
            return true;
        }

        bool BeginClipEdit(
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex,
            string undoName,
            out SerializedProperty track,
            out SerializedProperty clips,
            out SerializedProperty clip,
            out string diagnostic)
        {
            clips = null;
            clip = null;
            if (!BeginTrackEdit(variant, trackIndex, undoName, out _, out track, out diagnostic))
                return false;

            clips = track.FindPropertyRelative("clips");
            if (clips == null || clipIndex < 0 || clipIndex >= clips.arraySize)
            {
                diagnostic = $"clip-index-invalid:{trackIndex}:{clipIndex}";
                return false;
            }

            clip = clips.GetArrayElementAtIndex(clipIndex);
            return true;
        }

        void EndEdit()
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(actionDefinition);
        }

        static void InitializeTrack(SerializedProperty track, ActionTimelineTrackKind kind)
        {
            track.FindPropertyRelative(StableIdPropertyName).stringValue = NewStableId("track");
            track.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            track.FindPropertyRelative("clips").arraySize = 0;
        }

        static void InitializeClip(
            SerializedProperty clip,
            ActionTimelineClipKind kind,
            float startSeconds,
            float endSeconds)
        {
            clip.FindPropertyRelative(StableIdPropertyName).stringValue = NewStableId("clip");
            clip.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            SetClipRange(clip, startSeconds, endSeconds);
            SerializedProperty payload = clip.FindPropertyRelative("payload");
            payload.FindPropertyRelative("animationKey").stringValue = string.Empty;
            payload.FindPropertyRelative("motionSourceStateId").stringValue = string.Empty;
            payload.FindPropertyRelative("motionVariant").enumValueIndex = (int)CharacterStateVariant.None;
            payload.FindPropertyRelative("motionDuration").floatValue = 0f;
            payload.FindPropertyRelative("motionDistance").floatValue = 0f;
            payload.FindPropertyRelative("rotateToDirection").boolValue = false;
            payload.FindPropertyRelative("setRunLatchOnComplete").boolValue = false;
            payload.FindPropertyRelative("factId").stringValue = string.Empty;
            payload.FindPropertyRelative("cueId").stringValue = string.Empty;
        }

        static void SetClipRange(SerializedProperty clip, float startSeconds, float endSeconds)
        {
            float start = Mathf.Max(0f, startSeconds);
            float end = Mathf.Max(start, endSeconds);
            clip.FindPropertyRelative("startSeconds").floatValue = start;
            clip.FindPropertyRelative("endSeconds").floatValue = end;
        }

        static bool EnsureStableId(SerializedProperty property, string prefix)
        {
            SerializedProperty stableId = property?.FindPropertyRelative(StableIdPropertyName);
            if (stableId == null || !string.IsNullOrWhiteSpace(stableId.stringValue))
                return false;

            stableId.stringValue = NewStableId(prefix);
            return true;
        }

        static string NewStableId(string prefix)
        {
            return $"{prefix}.{Guid.NewGuid():N}";
        }
    }

    public sealed class CommittedActionTimelineEditorValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }

        public string Describe()
        {
            return string.Join(Environment.NewLine, errors);
        }
    }

    public static class CommittedActionTimelineEditorValidator
    {
        public static CommittedActionTimelineEditorValidationResult Validate(
            ICommittedActionTimelineEditorModel model)
        {
            CommittedActionTimelineEditorValidationResult result = new CommittedActionTimelineEditorValidationResult();
            if (model == null || !model.IsValid)
            {
                result.AddError("action-definition-missing");
                return result;
            }

            CharacterActionCatalogValidationResult actionValidation = model.ActionDefinition.Validate(
                ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default));
            for (int i = 0; i < actionValidation.Errors.Count; i++)
                result.AddError(actionValidation.Errors[i]);
            for (int i = 0; i < actionValidation.Warnings.Count; i++)
                result.AddWarning(actionValidation.Warnings[i]);

            IReadOnlyList<CommittedActionTimelineVariant> variants = model.Variants;
            for (int i = 0; i < variants.Count; i++)
                ValidateTimeline(model, variants[i], VariantLabel(variants[i]), true, result);

            return result;
        }

        public static bool IsClipInvalid(SerializedProperty track, SerializedProperty clip)
        {
            if (track == null || clip == null)
                return true;

            ActionTimelineTrackKind trackKind = ReadTrackKind(track);
            ActionTimelineClipKind clipKind = ReadClipKind(clip);
            if (!CommittedActionTimelineSerializedAdapter.IsClipKindAllowed(trackKind, clipKind))
                return true;
            if (clipKind == ActionTimelineClipKind.None)
                return true;

            float start = clip.FindPropertyRelative("startSeconds").floatValue;
            float end = clip.FindPropertyRelative("endSeconds").floatValue;
            if (float.IsNaN(start) || float.IsInfinity(start) || start < 0f ||
                float.IsNaN(end) || float.IsInfinity(end) || end < start)
                return true;

            SerializedProperty payload = clip.FindPropertyRelative("payload");
            switch (clipKind)
            {
                case ActionTimelineClipKind.AnimationKey:
                    return string.IsNullOrWhiteSpace(payload.FindPropertyRelative("animationKey").stringValue);
                case ActionTimelineClipKind.Motion:
                    return string.IsNullOrWhiteSpace(payload.FindPropertyRelative("motionSourceStateId").stringValue) ||
                           payload.FindPropertyRelative("motionDuration").floatValue <= 0f ||
                           payload.FindPropertyRelative("motionDistance").floatValue < 0f;
                case ActionTimelineClipKind.HitboxWindow:
                case ActionTimelineClipKind.CancelWindow:
                    return string.IsNullOrWhiteSpace(payload.FindPropertyRelative("factId").stringValue);
                case ActionTimelineClipKind.Cue:
                    return string.IsNullOrWhiteSpace(payload.FindPropertyRelative("cueId").stringValue);
                default:
                    return true;
            }
        }

        static void ValidateTimeline(
            ICommittedActionTimelineEditorModel model,
            CommittedActionTimelineVariant variant,
            string label,
            bool required,
            CommittedActionTimelineEditorValidationResult result)
        {
            if (!model.TryGetTimelineProperty(variant, out SerializedProperty timeline, out string diagnostic))
            {
                result.AddError($"{label}:timeline-missing:{diagnostic}");
                return;
            }

            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            if (required && (tracks == null || tracks.arraySize == 0))
                result.AddError($"{label}:timeline-required");

            bool hasAnimation = false;
            bool hasMotion = false;
            HashSet<string> trackIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> clipIds = new HashSet<string>(StringComparer.Ordinal);
            for (int trackIndex = 0; tracks != null && trackIndex < tracks.arraySize; trackIndex++)
            {
                SerializedProperty track = tracks.GetArrayElementAtIndex(trackIndex);
                string trackId = CommittedActionTimelineSerializedAdapter.ReadStableId(track);
                if (string.IsNullOrWhiteSpace(trackId))
                    result.AddError($"{label}:track-stable-id-missing:{trackIndex}");
                else if (!trackIds.Add(trackId))
                    result.AddError($"{label}:track-stable-id-duplicate:{trackIndex}:{trackId}");

                ActionTimelineTrackKind trackKind = ReadTrackKind(track);
                if (trackKind == ActionTimelineTrackKind.None)
                    result.AddError($"{label}:track-kind-invalid:{trackIndex}");

                SerializedProperty clips = track.FindPropertyRelative("clips");
                for (int clipIndex = 0; clips != null && clipIndex < clips.arraySize; clipIndex++)
                {
                    SerializedProperty clip = clips.GetArrayElementAtIndex(clipIndex);
                    string clipId = CommittedActionTimelineSerializedAdapter.ReadStableId(clip);
                    if (string.IsNullOrWhiteSpace(clipId))
                        result.AddError($"{label}:clip-stable-id-missing:{trackIndex}:{clipIndex}");
                    else if (!clipIds.Add(clipId))
                        result.AddError($"{label}:clip-stable-id-duplicate:{trackIndex}:{clipIndex}:{clipId}");

                    ActionTimelineClipKind clipKind = ReadClipKind(clip);
                    if (!CommittedActionTimelineSerializedAdapter.IsClipKindAllowed(trackKind, clipKind))
                        result.AddError($"{label}:clip-kind-not-allowed:{trackIndex}:{clipIndex}:{trackKind}:{clipKind}");
                    if (clipKind == ActionTimelineClipKind.AnimationKey)
                        hasAnimation = true;
                    if (clipKind == ActionTimelineClipKind.Motion)
                        hasMotion = true;
                    if (IsClipInvalid(track, clip))
                        result.AddError($"{label}:clip-invalid:{trackIndex}:{clipIndex}:{clipKind}");
                }
            }

            if (required && !hasAnimation)
                result.AddError($"{label}:animation-clip-missing");
            if (required && !hasMotion)
                result.AddError($"{label}:motion-clip-missing");
        }

        static ActionTimelineTrackKind ReadTrackKind(SerializedProperty track)
        {
            SerializedProperty kind = track.FindPropertyRelative("kind");
            return kind == null ? ActionTimelineTrackKind.None : (ActionTimelineTrackKind)kind.enumValueIndex;
        }

        static ActionTimelineClipKind ReadClipKind(SerializedProperty clip)
        {
            SerializedProperty kind = clip.FindPropertyRelative("kind");
            return kind == null ? ActionTimelineClipKind.None : (ActionTimelineClipKind)kind.enumValueIndex;
        }

        static string VariantLabel(CommittedActionTimelineVariant variant)
        {
            switch (variant)
            {
                case CommittedActionTimelineVariant.Directional:
                    return "Directional";
                case CommittedActionTimelineVariant.Backstep:
                    return "Backstep";
                default:
                    return "CommittedAction";
            }
        }
    }

    public readonly struct CommittedActionTimelinePreviewResult
    {
        public CommittedActionTimelinePreviewResult(
            bool hasPreview,
            string bindingStatus,
            float localTimeSeconds,
            int localTick,
            CommittedActionNodeId selectedNodeId,
            ActionAnimationKey animationKey,
            ActionMotionSpec motionSpec,
            string[] activeWindowFacts,
            string[] cueIds)
            : this(
                hasPreview,
                bindingStatus,
                localTimeSeconds,
                localTick,
                selectedNodeId,
                animationKey,
                motionSpec,
                activeWindowFacts,
                cueIds,
                bindingStatus,
                string.Empty,
                "preview-visual-unbound",
                0f,
                false)
        {
        }

        CommittedActionTimelinePreviewResult(
            bool hasPreview,
            string bindingStatus,
            float localTimeSeconds,
            int localTick,
            CommittedActionNodeId selectedNodeId,
            ActionAnimationKey animationKey,
            ActionMotionSpec motionSpec,
            string[] activeWindowFacts,
            string[] cueIds,
            string sceneBindingStatus,
            string resolvedClipName,
            string visualPreviewStatus,
            float visualClipTimeSeconds,
            bool visualPreviewSampled)
        {
            HasPreview = hasPreview;
            BindingStatus = bindingStatus ?? string.Empty;
            LocalTimeSeconds = Mathf.Max(0f, localTimeSeconds);
            LocalTick = Mathf.Max(0, localTick);
            SelectedNodeId = selectedNodeId;
            AnimationKey = animationKey;
            MotionSpec = motionSpec;
            this.activeWindowFacts = activeWindowFacts ?? Array.Empty<string>();
            this.cueIds = cueIds ?? Array.Empty<string>();
            SceneBindingStatus = sceneBindingStatus ?? string.Empty;
            ResolvedClipName = resolvedClipName ?? string.Empty;
            VisualPreviewStatus = visualPreviewStatus ?? string.Empty;
            VisualClipTimeSeconds = Mathf.Max(0f, visualClipTimeSeconds);
            VisualPreviewSampled = visualPreviewSampled;
        }

        readonly string[] activeWindowFacts;
        readonly string[] cueIds;

        public bool HasPreview { get; }
        public string BindingStatus { get; }
        public float LocalTimeSeconds { get; }
        public int LocalTick { get; }
        public CommittedActionNodeId SelectedNodeId { get; }
        public ActionAnimationKey AnimationKey { get; }
        public ActionMotionSpec MotionSpec { get; }
        public IReadOnlyList<string> ActiveWindowFacts => activeWindowFacts ?? Array.Empty<string>();
        public IReadOnlyList<string> CueIds => cueIds ?? Array.Empty<string>();
        public string SceneBindingStatus { get; }
        public string ResolvedClipName { get; }
        public string VisualPreviewStatus { get; }
        public float VisualClipTimeSeconds { get; }
        public bool VisualPreviewSampled { get; }

        public CommittedActionTimelinePreviewResult WithVisualPreview(CommittedActionTimelineVisualPreviewResult visual)
        {
            return new CommittedActionTimelinePreviewResult(
                HasPreview,
                BindingStatus,
                LocalTimeSeconds,
                LocalTick,
                SelectedNodeId,
                AnimationKey,
                MotionSpec,
                Copy(ActiveWindowFacts),
                Copy(CueIds),
                visual.BindingStatus,
                visual.ResolvedClipName,
                visual.VisualPreviewStatus,
                visual.ClipTimeSeconds,
                visual.Sampled);
        }

        static string[] Copy(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];
            return result;
        }
    }

    public sealed class CommittedActionTimelinePreviewAdapter
    {
        public CommittedActionTimelinePreviewResult Preview(
            CharacterActionDefinitionSO actionDefinition,
            CommittedActionTimelineVariant variant,
            float localTimeSeconds,
            int sourceStep)
        {
            ActionTimelineCompileContext compileContext = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
            int localTick = ActionTimelineQuantizer.QuantizeSecondsToTick(localTimeSeconds, in compileContext);
            if (actionDefinition == null)
            {
                return new CommittedActionTimelinePreviewResult(
                    false,
                    "preview-action-definition-missing",
                    localTimeSeconds,
                    localTick,
                    default,
                    default,
                    ActionMotionSpec.None(sourceStep),
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            CharacterActionDefinition definition = actionDefinition.ToDefinition(in compileContext);
            if (!definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch))
            {
                return new CommittedActionTimelinePreviewResult(
                    false,
                    "preview-branch-missing",
                    localTimeSeconds,
                    localTick,
                    default,
                    default,
                    ActionMotionSpec.None(sourceStep),
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }

            CharacterStateVariant requestVariant = variant == CommittedActionTimelineVariant.Backstep
                ? CharacterStateVariant.Backstep
                : CharacterStateVariant.Directional;
            CharacterInputRequestFact request = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                0,
                Mathf.Max(1, sourceStep + 4),
                sourceStep,
                requestVariant,
                requestVariant == CharacterStateVariant.Backstep ? Vector3.back : Vector3.forward);
            CommittedActionBranchEvaluationContext context = new CommittedActionBranchEvaluationContext(
                sourceStep,
                default,
                request,
                default,
                default);
            CommittedActionBranchOutcome outcome = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(
                    branch,
                    localTick,
                    sourceStep,
                    context));
            ActionTimelineOutcome timeline = outcome.TimelineOutcome;

            return new CommittedActionTimelinePreviewResult(
                outcome.HasOutcome,
                "preview-binding-unbound",
                localTimeSeconds,
                localTick,
                outcome.SelectedNodeId,
                timeline.AnimationKey,
                timeline.MotionSpec,
                Copy(timeline.ActiveWindowFactIds),
                CopyCueIds(timeline.CueRequests));
        }

        static string[] Copy(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];
            return result;
        }

        static string[] CopyCueIds(IReadOnlyList<ActionCueRequest> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            string[] result = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                result[i] = values[i].CueId;
            return result;
        }
    }
}
