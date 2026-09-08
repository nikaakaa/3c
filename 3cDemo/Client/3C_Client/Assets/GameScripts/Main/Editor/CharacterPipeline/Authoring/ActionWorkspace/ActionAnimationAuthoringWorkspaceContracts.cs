using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class ActionAnimationWorkspaceCommands
    {
        public static readonly GraphAuthoringCommandId Open =
            new GraphAuthoringCommandId(
                "open-action-animation-workspace");
    }

    public readonly struct ActionAnimationWorkspaceId : IEquatable<ActionAnimationWorkspaceId>
    {
        public ActionAnimationWorkspaceId(
            string definitionAssetGuid,
            string actionId,
            AnimationProducerId producerId,
            AnimationSlotId slotId,
            string skillId = "",
            string timelineRouteId = "")
        {
            DefinitionAssetGuid = Require(definitionAssetGuid, nameof(definitionAssetGuid));
            ActionId = Require(actionId, nameof(actionId));
            ProducerId = producerId;
            SlotId = slotId;
            SkillId = Normalize(skillId);
            TimelineRouteId = Normalize(timelineRouteId);
        }

        public string DefinitionAssetGuid { get; }
        public string ActionId { get; }
        public string SkillId { get; }
        public string TimelineRouteId { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationSlotId SlotId { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(DefinitionAssetGuid) &&
            !string.IsNullOrEmpty(ActionId);

        public bool Equals(ActionAnimationWorkspaceId other) =>
            string.Equals(DefinitionAssetGuid, other.DefinitionAssetGuid, StringComparison.Ordinal) &&
            string.Equals(ActionId, other.ActionId, StringComparison.Ordinal) &&
            string.Equals(SkillId, other.SkillId, StringComparison.Ordinal) &&
            string.Equals(TimelineRouteId, other.TimelineRouteId, StringComparison.Ordinal) &&
            ProducerId.Equals(other.ProducerId) &&
            SlotId.Equals(other.SlotId);

        public override bool Equals(object obj) =>
            obj is ActionAnimationWorkspaceId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(DefinitionAssetGuid);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ActionId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SkillId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TimelineRouteId);
                hash = (hash * 397) ^ ProducerId.GetHashCode();
                return (hash * 397) ^ SlotId.GetHashCode();
            }
        }

        public override string ToString() =>
            IsValid
                ? $"{DefinitionAssetGuid}/{ActionId}/{SkillId}/{TimelineRouteId}/{ProducerId}/{SlotId}"
                : string.Empty;

        static string Require(string value, string parameterName)
        {
            string normalized = value?.Trim();
            return string.IsNullOrEmpty(normalized)
                ? throw new ArgumentException("Workspace identity is missing.", parameterName)
                : normalized;
        }

        static string Normalize(string value) => value?.Trim() ?? string.Empty;
    }

    public sealed class ActionAnimationWorkspaceOpenRequest
    {
        public ActionAnimationWorkspaceOpenRequest(
            CharacterPipelineDefinition definition,
            string actionId,
            string timelineAuthoringId = "",
            string trackAuthoringId = "",
            string slotId = "",
            string skillId = "",
            string timelineRouteId = "")
        {
            Definition = definition
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            ActionId = Require(actionId, nameof(actionId));
            TimelineAuthoringId = Normalize(timelineAuthoringId);
            TrackAuthoringId = Normalize(trackAuthoringId);
            SlotId = Normalize(slotId);
            SkillId = Normalize(skillId);
            TimelineRouteId = Normalize(timelineRouteId);
            if (!string.IsNullOrEmpty(TrackAuthoringId) &&
                string.IsNullOrEmpty(TimelineAuthoringId))
                throw new ArgumentException(
                    "Track identity requires an exact Timeline identity.",
                    nameof(trackAuthoringId));
            if (!string.IsNullOrEmpty(TimelineRouteId) &&
                string.IsNullOrEmpty(TimelineAuthoringId))
                throw new ArgumentException(
                    "Timeline route identity requires an exact Timeline identity.",
                    nameof(timelineRouteId));
        }

        public CharacterPipelineDefinition Definition { get; }
        public string ActionId { get; }
        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string SlotId { get; }
        public string SkillId { get; }
        public string TimelineRouteId { get; }

        static string Normalize(string value) => value?.Trim() ?? string.Empty;

        static string Require(string value, string parameterName)
        {
            string normalized = Normalize(value);
            return string.IsNullOrEmpty(normalized)
                ? throw new ArgumentException("Action identity is missing.", parameterName)
                : normalized;
        }
    }

    public enum ActionAnimationWorkspaceFailureCode
    {
        DefinitionMissing,
        DefinitionIdentityMissing,
        RootGraphMissing,
        ActionProfileMissing,
        ActionProfileAmbiguous,
        SkillDefinitionMissing,
        SkillDefinitionAmbiguous,
        ActionIdentityInvalid,
        AuthoringTopologyInvalid,
        ActionCallSiteMissing,
        ActionCallSiteAmbiguous,
        ActionContextMissing,
        SkillEntryGraphMissing,
        FiniteTimelineMissing,
        FiniteTimelineAmbiguous,
        TimelineIdentityMismatch,
        TimelineIdentityInvalid,
        TimelineOwnerMissing,
        AnimationProducerMissing,
        AnimationProducerAmbiguous,
        AnimationProducerIdentityMismatch,
        AnimationProducerIdentityInvalid,
        PresentationProfileMissing,
        PresentationBindingMissing,
        PresentationBindingAmbiguous,
        PoseGraphMissing,
        AnimationSlotMissing,
        AnimationSlotAmbiguous,
        AnimationSlotIdentityMismatch,
        ProjectionMissing,
        ProjectionRevisionMissing,
        RigMissing
    }

    public enum ActionAnimationWorkspaceSelectionState
    {
        Unavailable,
        RequiresSelection,
        Selected
    }

    public sealed class ActionAnimationTimelineCandidate
    {
        public ActionAnimationTimelineCandidate(CharacterAuthoringTimelineEntry entry)
        {
            Entry = entry;
            AuthoringId = entry.Timeline?.AuthoringId?.Trim() ?? string.Empty;
            RouteIdentity = entry.Route?.ToString() ?? string.Empty;
            GraphAuthoringId = entry.Graph?.GraphAuthoringId?.Trim() ?? string.Empty;
        }

        public CharacterAuthoringTimelineEntry Entry { get; }
        public string AuthoringId { get; }
        public string RouteIdentity { get; }
        public string GraphAuthoringId { get; }
        public bool IsFinite =>
            Entry.Node != null &&
            Entry.Node.PlaybackMode == TimelinePlaybackMode.Once &&
            Entry.Timeline != null &&
            Entry.Timeline.MaxFrame > 0;
        public bool HasStableIdentity =>
            !string.IsNullOrEmpty(AuthoringId) &&
            !string.IsNullOrEmpty(RouteIdentity);
        public string DisplayName =>
            string.IsNullOrEmpty(GraphAuthoringId)
                ? AuthoringId
                : $"{AuthoringId} · {GraphAuthoringId} · {RouteIdentity}";
    }

    public sealed class ActionAnimationWorkspacePageState
    {
        readonly ActionAnimationTimelineCandidate[] m_TimelineCandidates;

        public ActionAnimationWorkspacePageState(
            IEnumerable<ActionAnimationTimelineCandidate> timelineCandidates,
            ActionAnimationTimelineCandidate selectedTimeline)
        {
            var values = timelineCandidates == null
                ? Array.Empty<ActionAnimationTimelineCandidate>()
                : new List<ActionAnimationTimelineCandidate>(timelineCandidates).ToArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Timeline candidate is missing.", nameof(timelineCandidates));
            }
            m_TimelineCandidates = values;
            SelectedTimeline = selectedTimeline;
            if (selectedTimeline != null &&
                !Array.Exists(values, value => ReferenceEquals(value, selectedTimeline)))
                throw new ArgumentException(
                    "Selected Timeline is not in the candidate list.",
                    nameof(selectedTimeline));
        }

        public IReadOnlyList<ActionAnimationTimelineCandidate> TimelineCandidates => m_TimelineCandidates;
        public ActionAnimationTimelineCandidate SelectedTimeline { get; }
        public ActionAnimationWorkspaceSelectionState TimelineSelectionState =>
            SelectedTimeline != null
                ? ActionAnimationWorkspaceSelectionState.Selected
                : m_TimelineCandidates.Length == 0
                    ? ActionAnimationWorkspaceSelectionState.Unavailable
                    : ActionAnimationWorkspaceSelectionState.RequiresSelection;
        public bool RequiresTimelineSelection =>
            TimelineSelectionState == ActionAnimationWorkspaceSelectionState.RequiresSelection;
        public bool IsReady => TimelineSelectionState == ActionAnimationWorkspaceSelectionState.Selected;
    }

    public sealed class ActionAnimationWorkspaceFailure
    {
        public ActionAnimationWorkspaceFailure(
            ActionAnimationWorkspaceFailureCode code,
            string message,
            UnityEngine.Object owner = null,
            string graphAuthoringId = "",
            string elementAuthoringId = "")
        {
            Code = code;
            Message = string.IsNullOrWhiteSpace(message)
                ? throw new ArgumentException("Workspace failure message is missing.", nameof(message))
                : message.Trim();
            Owner = owner;
            GraphAuthoringId = graphAuthoringId?.Trim() ?? string.Empty;
            ElementAuthoringId = elementAuthoringId?.Trim() ?? string.Empty;
        }

        public ActionAnimationWorkspaceFailureCode Code { get; }
        public string Message { get; }
        public UnityEngine.Object Owner { get; }
        public string GraphAuthoringId { get; }
        public string ElementAuthoringId { get; }
    }

    public sealed class ActionAnimationDefinitionContext
    {
        public ActionAnimationDefinitionContext(
            CharacterPipelineDefinition definition,
            string assetGuid,
            BaseTree rootGraph)
        {
            Definition = definition
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            AssetGuid = string.IsNullOrWhiteSpace(assetGuid)
                ? throw new ArgumentException("Definition asset GUID is missing.", nameof(assetGuid))
                : assetGuid;
            RootGraph = rootGraph ?? throw new ArgumentNullException(nameof(rootGraph));
        }

        public CharacterPipelineDefinition Definition { get; }
        public string AssetGuid { get; }
        public BaseTree RootGraph { get; }
    }

    public sealed class ActionAnimationProfileContext
    {
        public ActionAnimationProfileContext(
            ActionProfile profile,
            CharacterSkillAuthoringDefinition skill = null)
        {
            Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            ActionId = string.IsNullOrWhiteSpace(profile.ActionId)
                ? throw new ArgumentException("ActionProfile identity is missing.", nameof(profile))
                : profile.ActionId;
            Skill = skill;
            SkillId = skill?.SkillId?.Trim() ?? string.Empty;
            EntryGraphAuthoringId = skill?.EntryGraphAuthoringId?.Trim() ?? string.Empty;
        }

        public ActionProfile Profile { get; }
        public string ActionId { get; }
        public CharacterSkillAuthoringDefinition Skill { get; }
        public string SkillId { get; }
        public string EntryGraphAuthoringId { get; }
    }

    public sealed class ActionAnimationCallSiteContext
    {
        public ActionAnimationCallSiteContext(
            CharacterAuthoringGraphEntry graphEntry,
            BaseNode node,
            ActionContextSlot actionContext = null)
        {
            GraphEntry = graphEntry;
            Node = node;
            ActionContext = actionContext ?? (node as ActivateActionInstanceNode)?.ActionContext;
            if (!ActionContext)
                throw new ArgumentException("Action call site has no Action Context.", nameof(node));
        }

        public CharacterAuthoringGraphEntry GraphEntry { get; }
        public BaseTree Graph => GraphEntry.Graph;
        public BaseNode Node { get; }
        public ActionContextSlot ActionContext { get; }
    }

    public sealed class ActionAnimationTimelineContext
    {
        public ActionAnimationTimelineContext(CharacterAuthoringTimelineEntry entry)
        {
            Entry = entry;
            Node = entry.Node ?? throw new ArgumentException("Timeline node is missing.", nameof(entry));
            Timeline = entry.Timeline ?? throw new ArgumentException("Timeline data is missing.", nameof(entry));
            if (Node.PlaybackMode != TimelinePlaybackMode.Once || Timeline.MaxFrame <= 0)
                throw new ArgumentException("Timeline is not a finite Action Timeline.", nameof(entry));
            if (!Timeline.SerializedOwner || string.IsNullOrWhiteSpace(Timeline.SerializedPropertyPath))
                throw new ArgumentException("Timeline serialized owner is missing.", nameof(entry));
        }

        public CharacterAuthoringTimelineEntry Entry { get; }
        public BaseTree Graph => Entry.Graph;
        public TimelineNode Node { get; }
        public TimelineData Timeline { get; }
        public UnityEngine.Object SerializedOwner => Timeline.SerializedOwner;
        public string SerializedPropertyPath => Timeline.SerializedPropertyPath;
        public TimelineOwnership Ownership => Node.TimelineOwnership;
        public string RouteIdentity => Entry.Route?.ToString() ?? string.Empty;
    }

    public sealed class ActionAnimationProducerContext
    {
        public ActionAnimationProducerContext(
            ActionAnimationTimelineContext timeline,
            AnimationTrack track)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Track = track ?? throw new ArgumentNullException(nameof(track));
            ProducerId = new AnimationProducerId(
                timeline.Timeline.AuthoringId,
                track.AuthoringId);
            if (!ProducerId.IsValid || !track.AnimationChannelId.IsValid)
                throw new ArgumentException("Animation producer identity or channel is invalid.", nameof(track));
        }

        public ActionAnimationTimelineContext Timeline { get; }
        public AnimationTrack Track { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationChannelId AnimationChannelId => Track.AnimationChannelId;
    }

    public sealed class ActionAnimationPresentationBindingContext
    {
        public ActionAnimationPresentationBindingContext(
            CharacterAnimationPresentationProfile profile,
            AnimationProducerPresentationBinding binding)
        {
            Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public CharacterAnimationPresentationProfile Profile { get; }
        public AnimationProducerPresentationBinding Binding { get; }
    }

    public sealed class ActionAnimationSlotConsumerContext
    {
        public ActionAnimationSlotConsumerContext(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph graph,
            CharacterPoseCanvasNode node,
            CharacterAnimationSlotPosePayload payload)
        {
            Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public CharacterPresentationPoseGraphAsset Asset { get; }
        public CharacterPoseCanvasGraph Graph { get; }
        public CharacterPoseCanvasNode Node { get; }
        public CharacterAnimationSlotPosePayload Payload { get; }
        public AnimationSlotId SlotId => Payload.SlotId;
        public AnimationChannelId AnimationChannelId => Payload.AnimationChannelId;
        public CharacterAnimationBlendPolicy BlendPolicy => Payload.BlendPolicy;
    }

    public sealed class ActionAnimationRuntimeDebugBinding :
        ITimelineEditorRuntimeDebugBinding
    {
        public ActionAnimationRuntimeDebugBinding(
            string timelineAuthoringId,
            string definitionAssetGuid,
            string sourceRevision,
            string projectionRevision)
        {
            BindingId = string.IsNullOrWhiteSpace(timelineAuthoringId)
                ? throw new ArgumentException("Runtime Debug Timeline identity is invalid.", nameof(timelineAuthoringId))
                : timelineAuthoringId;
            DefinitionAssetGuid = definitionAssetGuid?.Trim() ?? string.Empty;
            SourceRevision = sourceRevision?.Trim() ?? string.Empty;
            ProjectionRevision = projectionRevision?.Trim() ?? string.Empty;
        }

        public string BindingId { get; }
        public string DefinitionAssetGuid { get; }
        public string SourceRevision { get; }
        public string ProjectionRevision { get; }
        public bool HasExactRevision =>
            !string.IsNullOrEmpty(DefinitionAssetGuid) &&
            !string.IsNullOrEmpty(SourceRevision) &&
            !string.IsNullOrEmpty(ProjectionRevision);
    }

    public sealed class ActionAnimationPreviewTargetContext
    {
        public ActionAnimationPreviewTargetContext(
            CharacterPresentationProjectionAsset projection,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseGraphAsset poseGraph)
        {
            Projection = projection;
            Rig = rig;
            PoseGraph = poseGraph;
        }

        public CharacterPresentationProjectionAsset Projection { get; }
        public CharacterAnimationRigDefinition Rig { get; }
        public CharacterPresentationPoseGraphAsset PoseGraph { get; }
        public bool IsReady =>
            Projection &&
            Rig &&
            PoseGraph &&
            !string.IsNullOrWhiteSpace(Projection.SourceRevision) &&
            !string.IsNullOrWhiteSpace(Projection.ProjectionRevision);
    }

    public sealed class ActionAnimationWorkspaceSession
    {
        public ActionAnimationWorkspaceSession(
            ActionAnimationWorkspaceId workspaceId,
            ActionAnimationDefinitionContext definition,
            ActionAnimationProfileContext action,
            ActionAnimationCallSiteContext callSite,
            ActionAnimationTimelineContext timeline,
            ActionAnimationProducerContext producer,
            ActionAnimationPresentationBindingContext presentation,
            ActionAnimationSlotConsumerContext slot,
            ActionAnimationRuntimeDebugBinding runtimeDebug,
            ActionAnimationPreviewTargetContext previewTarget,
            ActionAnimationWorkspacePageState pageState = null)
        {
            WorkspaceId = workspaceId.IsValid
                ? workspaceId
                : throw new ArgumentException("Workspace identity is invalid.", nameof(workspaceId));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            CallSite = callSite;
            Timeline = timeline;
            Producer = producer;
            Presentation = presentation;
            Slot = slot;
            RuntimeDebug = runtimeDebug;
            PreviewTarget = previewTarget;
            PageState = pageState;
        }

        public ActionAnimationWorkspaceId WorkspaceId { get; }
        public ActionAnimationDefinitionContext Definition { get; }
        public ActionAnimationProfileContext Action { get; }
        public ActionAnimationCallSiteContext CallSite { get; }
        public ActionAnimationTimelineContext Timeline { get; }
        public ActionAnimationProducerContext Producer { get; }
        public ActionAnimationPresentationBindingContext Presentation { get; }
        public ActionAnimationSlotConsumerContext Slot { get; }
        public ActionAnimationRuntimeDebugBinding RuntimeDebug { get; }
        public ActionAnimationPreviewTargetContext PreviewTarget { get; }
        public ActionAnimationWorkspacePageState PageState { get; }
    }

    public sealed class ActionAnimationWorkspaceResolution
    {
        public ActionAnimationWorkspaceResolution(
            ActionAnimationWorkspaceSession session,
            IReadOnlyList<ActionAnimationWorkspaceFailure> failures,
            ActionAnimationDefinitionContext definition = null,
            ActionAnimationProfileContext action = null,
            ActionAnimationCallSiteContext callSite = null,
            ActionAnimationTimelineContext timeline = null,
            ActionAnimationProducerContext producer = null,
            ActionAnimationPresentationBindingContext presentation = null,
            ActionAnimationSlotConsumerContext slot = null,
            ActionAnimationRuntimeDebugBinding runtimeDebug = null,
            ActionAnimationPreviewTargetContext previewTarget = null,
            ActionAnimationWorkspacePageState pageState = null)
        {
            Session = session;
            Failures = failures ?? Array.Empty<ActionAnimationWorkspaceFailure>();
            Definition = session?.Definition ?? definition;
            Action = session?.Action ?? action;
            CallSite = session?.CallSite ?? callSite;
            Timeline = session?.Timeline ?? timeline;
            Producer = session?.Producer ?? producer;
            Presentation = session?.Presentation ?? presentation;
            Slot = session?.Slot ?? slot;
            RuntimeDebug = session?.RuntimeDebug ?? runtimeDebug;
            PreviewTarget = session?.PreviewTarget ?? previewTarget;
            PageState = session?.PageState ?? pageState;
        }

        public ActionAnimationWorkspaceSession Session { get; }
        public IReadOnlyList<ActionAnimationWorkspaceFailure> Failures { get; }
        public ActionAnimationDefinitionContext Definition { get; }
        public ActionAnimationProfileContext Action { get; }
        public ActionAnimationCallSiteContext CallSite { get; }
        public ActionAnimationTimelineContext Timeline { get; }
        public ActionAnimationProducerContext Producer { get; }
        public ActionAnimationPresentationBindingContext Presentation { get; }
        public ActionAnimationSlotConsumerContext Slot { get; }
        public ActionAnimationRuntimeDebugBinding RuntimeDebug { get; }
        public ActionAnimationPreviewTargetContext PreviewTarget { get; }
        public ActionAnimationWorkspacePageState PageState { get; }
        public bool IsComplete =>
            Session != null &&
            Failures.Count == 0 &&
            (PageState == null || PageState.IsReady);
    }
}
