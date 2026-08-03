using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class ActionAnimationAuthoringWorkspaceResolver
    {
        public static ActionAnimationWorkspaceResolution Resolve(
            ActionAnimationWorkspaceOpenRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var failures = new List<ActionAnimationWorkspaceFailure>();
            ActionAnimationDefinitionContext definition = ResolveDefinition(request, failures);
            ActionAnimationProfileContext action = ResolveAction(request, failures);
            CharacterAuthoringTopologyProjection topology =
                ResolveTopology(definition, failures);
            ActionAnimationCallSiteContext callSite =
                ResolveCallSite(topology, action, failures);
            ActionAnimationTimelineContext timeline =
                ResolveTimeline(request, topology, callSite, failures);
            ActionAnimationProducerContext producer =
                ResolveProducer(request, timeline, failures);
            ActionAnimationPresentationBindingContext presentation =
                ResolvePresentation(definition, producer, failures);
            ActionAnimationSlotConsumerContext slot =
                ResolveSlot(request, definition, producer, failures);
            ActionAnimationPreviewTargetContext previewTarget =
                ResolvePreviewTarget(definition, failures);
            ActionAnimationRuntimeDebugBinding runtimeDebug =
                ResolveRuntimeDebug(definition, timeline);

            ActionAnimationWorkspaceSession session = null;
            if (failures.Count == 0)
            {
                var workspaceId = new ActionAnimationWorkspaceId(
                    definition.AssetGuid,
                    action.ActionId,
                    producer.ProducerId,
                    slot.SlotId);
                session = new ActionAnimationWorkspaceSession(
                    workspaceId,
                    definition,
                    action,
                    callSite,
                    timeline,
                    producer,
                    presentation,
                    slot,
                    runtimeDebug,
                    previewTarget);
            }

            return new ActionAnimationWorkspaceResolution(
                session,
                failures.AsReadOnly(),
                definition,
                action,
                callSite,
                timeline,
                producer,
                presentation,
                slot,
                runtimeDebug,
                previewTarget);
        }

        static ActionAnimationDefinitionContext ResolveDefinition(
            ActionAnimationWorkspaceOpenRequest request,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            CharacterPipelineDefinition definition = request.Definition;
            if (!definition)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.DefinitionMissing,
                    "Action Animation Workspace requires an exact Character Definition.");
                return null;
            }
            string path = AssetDatabase.GetAssetPath(definition);
            string assetGuid = string.IsNullOrEmpty(path)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(assetGuid))
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.DefinitionIdentityMissing,
                    "Character Definition has no persistent asset GUID.",
                    definition);
                return null;
            }
            if (!definition.RootTreeAsset || definition.RootTreeAsset.Tree == null)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.RootGraphMissing,
                    "Character Definition has no formal Root Graph.",
                    definition);
                return null;
            }
            return new ActionAnimationDefinitionContext(
                definition,
                assetGuid,
                definition.RootTreeAsset.Tree);
        }

        static ActionAnimationProfileContext ResolveAction(
            ActionAnimationWorkspaceOpenRequest request,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            CharacterPipelineDefinition definition = request.Definition;
            if (!definition)
                return null;
            ActionProfile[] matches = definition.ActionProfiles
                .Where(value =>
                    value &&
                    string.Equals(
                        value.ActionId,
                        request.ActionId,
                        StringComparison.Ordinal))
                .ToArray();
            if (matches.Length == 0)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionProfileMissing,
                    $"Definition does not own ActionProfile '{request.ActionId}'.",
                    definition);
                return null;
            }
            if (matches.Length > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionProfileAmbiguous,
                    $"Definition owns {matches.Length} ActionProfiles with identity '{request.ActionId}'.",
                    definition);
                return null;
            }
            if (string.IsNullOrWhiteSpace(matches[0].ActionId))
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionIdentityInvalid,
                    "ActionProfile has no stable Action identity.",
                    matches[0]);
                return null;
            }
            return new ActionAnimationProfileContext(matches[0]);
        }

        static CharacterAuthoringTopologyProjection ResolveTopology(
            ActionAnimationDefinitionContext definition,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (definition == null)
                return null;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology =
                CharacterAuthoringTopologyProjection.Build(
                    definition.RootGraph,
                    topologyErrors);
            if (topology.IsValid && topologyErrors.Count == 0)
                return topology;
            if (topologyErrors.Count == 0)
                topologyErrors.Add("Character authoring topology is invalid.");
            for (int i = 0; i < topologyErrors.Count; i++)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.AuthoringTopologyInvalid,
                    topologyErrors[i],
                    definition.Definition);
            }
            return null;
        }

        static ActionAnimationCallSiteContext ResolveCallSite(
            CharacterAuthoringTopologyProjection topology,
            ActionAnimationProfileContext action,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (topology == null || action == null)
                return null;
            var matches =
                new List<(CharacterAuthoringGraphEntry graph, ActivateActionInstanceNode node)>();
            for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
            {
                CharacterAuthoringGraphEntry graph = topology.Graphs[graphIndex];
                if (!graph.FirstOccurrence || graph.Graph == null)
                    continue;
                for (int nodeIndex = 0; nodeIndex < graph.Graph.Nodes.Count; nodeIndex++)
                {
                    if (graph.Graph.Nodes[nodeIndex] is ActivateActionInstanceNode node &&
                        ReferenceEquals(node.ActionProfile, action.Profile))
                        matches.Add((graph, node));
                }
            }
            if (matches.Count == 0)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionCallSiteMissing,
                    $"Action '{action.ActionId}' has no Activate Action Instance call site.",
                    action.Profile);
                return null;
            }
            if (matches.Count > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionCallSiteAmbiguous,
                    $"Action '{action.ActionId}' has {matches.Count} Activate Action Instance call sites.",
                    action.Profile);
                return null;
            }
            if (!matches[0].node.ActionContext)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ActionContextMissing,
                    $"Action call site '{matches[0].node.GUID}' has no Action Context.",
                    matches[0].graph.Graph.SerializedOwner,
                    matches[0].graph.Graph.GraphAuthoringId,
                    matches[0].node.GUID);
                return null;
            }
            return new ActionAnimationCallSiteContext(
                matches[0].graph,
                matches[0].node);
        }

        static ActionAnimationTimelineContext ResolveTimeline(
            ActionAnimationWorkspaceOpenRequest request,
            CharacterAuthoringTopologyProjection topology,
            ActionAnimationCallSiteContext callSite,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (topology == null || callSite == null)
                return null;
            CharacterAuthoringTimelineEntry[] candidates = topology.Timelines
                .Where(value =>
                    value.Node != null &&
                    ReferenceEquals(
                        value.Node.ActionContext,
                        callSite.ActionContext) &&
                    value.Node.PlaybackMode == TimelinePlaybackMode.Once &&
                    value.Timeline != null &&
                    value.Timeline.MaxFrame > 0)
                .ToArray();
            CharacterAuthoringTimelineEntry[] matches =
                string.IsNullOrEmpty(request.TimelineAuthoringId)
                    ? candidates
                    : candidates
                        .Where(value =>
                            string.Equals(
                                value.Timeline.AuthoringId,
                                request.TimelineAuthoringId,
                                StringComparison.Ordinal))
                        .ToArray();
            if (matches.Length == 0)
            {
                Add(
                    failures,
                    string.IsNullOrEmpty(request.TimelineAuthoringId)
                        ? ActionAnimationWorkspaceFailureCode.FiniteTimelineMissing
                        : ActionAnimationWorkspaceFailureCode.TimelineIdentityMismatch,
                    string.IsNullOrEmpty(request.TimelineAuthoringId)
                        ? $"Action call site '{callSite.Node.GUID}' has no finite Timeline."
                        : $"Action call site '{callSite.Node.GUID}' does not own finite Timeline '{request.TimelineAuthoringId}'.",
                    callSite.Graph.SerializedOwner,
                    callSite.Graph.GraphAuthoringId,
                    callSite.Node.GUID);
                return null;
            }
            if (matches.Length > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.FiniteTimelineAmbiguous,
                    $"Action call site '{callSite.Node.GUID}' resolves {matches.Length} finite Timelines.",
                    callSite.Graph.SerializedOwner,
                    callSite.Graph.GraphAuthoringId,
                    callSite.Node.GUID);
                return null;
            }
            if (string.IsNullOrWhiteSpace(matches[0].Timeline.AuthoringId))
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.TimelineIdentityInvalid,
                    "Finite Action Timeline has no stable AuthoringId.",
                    matches[0].Graph.SerializedOwner,
                    matches[0].Graph.GraphAuthoringId,
                    matches[0].Node.GUID);
                return null;
            }
            if (!matches[0].Timeline.SerializedOwner ||
                string.IsNullOrWhiteSpace(matches[0].Timeline.SerializedPropertyPath))
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.TimelineOwnerMissing,
                    $"Timeline '{matches[0].Timeline.AuthoringId}' has no serialized owner/path.",
                    matches[0].Graph.SerializedOwner,
                    matches[0].Graph.GraphAuthoringId,
                    matches[0].Node.GUID);
                return null;
            }
            return new ActionAnimationTimelineContext(matches[0]);
        }

        static ActionAnimationProducerContext ResolveProducer(
            ActionAnimationWorkspaceOpenRequest request,
            ActionAnimationTimelineContext timeline,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (timeline == null)
                return null;
            AnimationTrack[] candidates = timeline.Timeline.Tracks
                .OfType<AnimationTrack>()
                .ToArray();
            AnimationTrack[] matches =
                string.IsNullOrEmpty(request.TrackAuthoringId)
                    ? candidates
                    : candidates
                        .Where(value =>
                            string.Equals(
                                value.AuthoringId,
                                request.TrackAuthoringId,
                                StringComparison.Ordinal))
                        .ToArray();
            if (matches.Length == 0)
            {
                Add(
                    failures,
                    string.IsNullOrEmpty(request.TrackAuthoringId)
                        ? ActionAnimationWorkspaceFailureCode.AnimationProducerMissing
                        : ActionAnimationWorkspaceFailureCode.AnimationProducerIdentityMismatch,
                    string.IsNullOrEmpty(request.TrackAuthoringId)
                        ? $"Timeline '{timeline.Timeline.AuthoringId}' has no Animation producer track."
                        : $"Timeline '{timeline.Timeline.AuthoringId}' does not own Animation producer '{request.TrackAuthoringId}'.",
                    timeline.SerializedOwner,
                    timeline.Graph.GraphAuthoringId,
                    timeline.Node.GUID);
                return null;
            }
            if (matches.Length > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.AnimationProducerAmbiguous,
                    $"Timeline '{timeline.Timeline.AuthoringId}' resolves {matches.Length} Animation producers.",
                    timeline.SerializedOwner,
                    timeline.Graph.GraphAuthoringId,
                    timeline.Node.GUID);
                return null;
            }
            var producerId = new AnimationProducerId(
                timeline.Timeline.AuthoringId,
                matches[0].AuthoringId);
            if (!producerId.IsValid || !matches[0].AnimationChannelId.IsValid)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.AnimationProducerIdentityInvalid,
                    $"Animation producer '{producerId}' has no stable identity or Animation Channel.",
                    timeline.SerializedOwner,
                    timeline.Graph.GraphAuthoringId,
                    timeline.Node.GUID);
                return null;
            }
            return new ActionAnimationProducerContext(timeline, matches[0]);
        }

        static ActionAnimationPresentationBindingContext ResolvePresentation(
            ActionAnimationDefinitionContext definition,
            ActionAnimationProducerContext producer,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (definition == null)
                return null;
            CharacterAnimationPresentationProfile profile =
                definition.Definition.AnimationPresentationProfile;
            if (!profile)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.PresentationProfileMissing,
                    "Character Definition has no Animation Presentation Profile.",
                    definition.Definition);
                return null;
            }
            if (producer == null)
                return null;
            AnimationProducerPresentationBinding[] matches =
                profile.ProducerBindings
                    .Where(value =>
                        value != null &&
                        value.ProducerId.Equals(producer.ProducerId))
                    .ToArray();
            if (matches.Length == 0)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.PresentationBindingMissing,
                    $"Animation producer '{producer.ProducerId}' has no Presentation binding.",
                    profile);
                return null;
            }
            if (matches.Length > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.PresentationBindingAmbiguous,
                    $"Animation producer '{producer.ProducerId}' has {matches.Length} Presentation bindings.",
                    profile);
                return null;
            }
            return new ActionAnimationPresentationBindingContext(
                profile,
                matches[0]);
        }

        static ActionAnimationSlotConsumerContext ResolveSlot(
            ActionAnimationWorkspaceOpenRequest request,
            ActionAnimationDefinitionContext definition,
            ActionAnimationProducerContext producer,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (definition == null)
                return null;
            CharacterAnimationPresentationProfile profile =
                definition.Definition.AnimationPresentationProfile;
            CharacterPresentationPoseGraphAsset poseGraph =
                profile ? profile.PoseGraph : null;
            if (!poseGraph)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.PoseGraphMissing,
                    "Animation Presentation Profile has no formal Pose Graph.",
                    profile ? profile : definition.Definition);
                return null;
            }
            if (producer == null)
                return null;
            var candidates =
                new List<(CharacterTypedPoseGraph graph, CharacterTypedPoseNode node, CharacterAnimationSlotPosePayload payload)>();
            foreach (CharacterTypedPoseGraph graph in poseGraph.EnumerateGraphs())
            {
                if (graph == null)
                    continue;
                for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
                {
                    CharacterTypedPoseNode node = graph.Nodes[nodeIndex];
                    if (node?.Payload is CharacterAnimationSlotPosePayload payload &&
                        payload.AnimationChannelId == producer.AnimationChannelId)
                        candidates.Add((graph, node, payload));
                }
            }
            var matches = string.IsNullOrEmpty(request.SlotId)
                ? candidates
                : candidates
                    .Where(value =>
                        string.Equals(
                            value.payload.SlotId.Value,
                            request.SlotId,
                            StringComparison.Ordinal))
                    .ToList();
            if (matches.Count == 0)
            {
                Add(
                    failures,
                    string.IsNullOrEmpty(request.SlotId)
                        ? ActionAnimationWorkspaceFailureCode.AnimationSlotMissing
                        : ActionAnimationWorkspaceFailureCode.AnimationSlotIdentityMismatch,
                    string.IsNullOrEmpty(request.SlotId)
                        ? $"Animation Channel '{producer.AnimationChannelId}' has no AnimationSlot consumer."
                        : $"Animation Channel '{producer.AnimationChannelId}' has no AnimationSlot '{request.SlotId}'.",
                    poseGraph);
                return null;
            }
            if (matches.Count > 1)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.AnimationSlotAmbiguous,
                    $"Animation Channel '{producer.AnimationChannelId}' resolves {matches.Count} AnimationSlot consumers.",
                    poseGraph);
                return null;
            }
            return new ActionAnimationSlotConsumerContext(
                poseGraph,
                matches[0].graph,
                matches[0].node,
                matches[0].payload);
        }

        static ActionAnimationPreviewTargetContext ResolvePreviewTarget(
            ActionAnimationDefinitionContext definition,
            List<ActionAnimationWorkspaceFailure> failures)
        {
            if (definition == null)
                return null;
            CharacterPipelineDefinition value = definition.Definition;
            CharacterAnimationPresentationProfile profile =
                value.AnimationPresentationProfile;
            var context = new ActionAnimationPreviewTargetContext(
                value.PresentationProjection,
                profile ? profile.RigDefinition : null,
                profile ? profile.PoseGraph : null);
            if (!context.Projection)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ProjectionMissing,
                    "Definition has no explicitly published Presentation Projection.",
                    value);
            }
            else if (string.IsNullOrWhiteSpace(context.Projection.SourceRevision) ||
                     string.IsNullOrWhiteSpace(context.Projection.ProjectionRevision))
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.ProjectionRevisionMissing,
                    "Published Presentation Projection has no exact source/projection revision.",
                    context.Projection);
            }
            if (!context.Rig)
            {
                Add(
                    failures,
                    ActionAnimationWorkspaceFailureCode.RigMissing,
                    "Animation Presentation Profile has no formal Rig Definition.",
                    profile ? profile : value);
            }
            return context;
        }

        static ActionAnimationRuntimeDebugBinding ResolveRuntimeDebug(
            ActionAnimationDefinitionContext definition,
            ActionAnimationTimelineContext timeline)
        {
            if (definition == null || timeline == null)
                return null;
            var projection = definition.Definition.PresentationProjection;
            return new ActionAnimationRuntimeDebugBinding(
                timeline.Timeline.AuthoringId,
                definition.AssetGuid,
                projection ? projection.SourceRevision : string.Empty,
                projection ? projection.ProjectionRevision : string.Empty);
        }

        static void Add(
            List<ActionAnimationWorkspaceFailure> failures,
            ActionAnimationWorkspaceFailureCode code,
            string message,
            UnityEngine.Object owner = null,
            string graphAuthoringId = "",
            string elementAuthoringId = "")
        {
            failures.Add(new ActionAnimationWorkspaceFailure(
                code,
                message,
                owner,
                graphAuthoringId,
                elementAuthoringId));
        }
    }
}
