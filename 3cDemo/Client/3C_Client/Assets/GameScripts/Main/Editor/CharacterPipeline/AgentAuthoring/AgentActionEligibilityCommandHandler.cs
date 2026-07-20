using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentActionEligibilityCommandHandler : IAgentPatchCommandHandler
    {
        public bool Preflight(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureBlackboardDeclarationCommand value:
                    if (!ValidateBlackboardDeclarationTarget(session, value, out BaseTree declarationGraph)) return false;
                    session.PlanBlackboardDeclaration(value.DeclarationAuthoringId, declarationGraph.GraphAuthoringId, value.ValueType);
                    session.AddPlanned(value, declarationGraph, value.Key, "ensure declaration");
                    return true;
                case AgentMoveBlackboardDeclarationCommand value:
                    return PreflightMoveDeclaration(session, value);
                case AgentDeleteBlackboardDeclarationCommand value:
                    if (!TryResolveGraphDeclaration(session, value.Graph, value.DeclarationAuthoringId, value.Path, out BaseTree deleteGraph, out BaseExposedProperty declaration)) return false;
                    session.AddPlanned(value, deleteGraph, declaration.BlackboardKey, "delete declaration");
                    return true;
                case AgentEnsureBlackboardWriteCommand value:
                    return PreflightBlackboardWrite(session, value);
                case AgentEnsureTimelineTreeClipCommand value:
                    if (!ValidateTimelineTreeClipTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, $"ensure TreeClip {value.StartFrame}..{value.EndFrame}/{value.Phase}");
                    return true;
                case AgentEnsureMotionWarpTrackCommand value:
                    if (!ValidateMotionWarpTrackTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.TrackAuthoringId, "ensure MotionWarp track");
                    return true;
                case AgentEnsureMotionWarpClipCommand value:
                    if (!ValidateMotionWarpClipTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, $"ensure MotionWarp clip {value.StartFrame}..{value.EndFrame}");
                    return true;
                case AgentConfigureMotionWarpSourceCommand value:
                    if (!ValidateMotionWarpSource(session, value)) return false;
                    session.AddPlanned(value, null, value.SourceMotionClipAuthoringId, "configure MotionWarp source");
                    return true;
                case AgentConfigureMotionWarpParametersCommand value:
                    if (!ValidateMotionWarpParameters(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, "configure MotionWarp parameters");
                    return true;
                case AgentMoveTimelineClipCommand value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip moveClip)) return false;
                    if (moveClip.StartFrame + value.FrameOffset < 0)
                    {
                        session.Report.Error(value.Path, "timeline_clip_start_negative", $"Timeline Clip '{moveClip.AuthoringId}' 平移后的 StartFrame 不能小于 0。");
                        return false;
                    }
                    session.AddPlanned(value, null, moveClip.AuthoringId,
                        $"move clip {moveClip.StartFrame}..{moveClip.EndFrame} -> {moveClip.StartFrame + value.FrameOffset}..{moveClip.EndFrame + value.FrameOffset}");
                    return true;
                case AgentConfigureTimelineClipEaseCommand value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip easeClip)) return false;
                    if (!ValidateTimelineClipEase(session, value, easeClip)) return false;
                    session.AddPlanned(value, null, easeClip.AuthoringId,
                        $"configure clip ease self {easeClip.SelfEaseInFrame}/{easeClip.SelfEaseOutFrame} -> {value.SelfEaseInFrame}/{value.SelfEaseOutFrame}");
                    return true;
                case AgentConfigureTimelineCurveChannelCommand value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip curveOwner)) return false;
                    TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.Require(value.ChannelId.Value);
                    if (!descriptor.Supports(curveOwner))
                    {
                        session.Report.Error(value.Path, "timeline_curve_owner_incompatible", $"Timeline Clip '{curveOwner.AuthoringId}' 不拥有Curve Channel '{value.ChannelId}'。");
                        return false;
                    }
                    try
                    {
                        descriptor.Validate(curveOwner, value.Curve);
                    }
                    catch (InvalidOperationException exception)
                    {
                        session.Report.Error(value.Path, "timeline_curve_owner_validation_failed", exception.Message);
                        return false;
                    }
                    session.AddPlanned(value, null, curveOwner.AuthoringId, $"configure Timeline curve {value.ChannelId}");
                    return true;
                case AgentConfigureAnimationTrackMarkerSyncCommand value:
                    if (!TryResolveAnimationTrack(session, value.Target, value.Path, out _, out AnimationTrack configureTrack)) return false;
                    session.AddPlanned(value, null, configureTrack?.AuthoringId ?? value.Target.TrackAuthoringId,
                        $"configure marker sync {value.Mode}/{value.SyncGroupId}/{value.Topology}/{value.SyncRole}");
                    return true;
                case AgentEnsureAnimationSyncMarkerCommand value:
                    if (!TryResolveAnimationTrack(session, value.Target, value.Path, out _, out _)) return false;
                    session.AddPlanned(value, null, value.MarkerAuthoringId,
                        $"ensure animation marker {value.MarkerId}@{value.Frame}");
                    return true;
                case AgentMoveAnimationSyncMarkerCommand value:
                    if (!TryResolveAnimationMarker(session, value.Target, value.Marker, value.Path, out _, out _, out AnimationSyncMarker moveMarker)) return false;
                    session.AddPlanned(value, null, moveMarker?.AuthoringId ?? value.Marker.Identity,
                        $"move animation marker to {value.Frame}");
                    return true;
                case AgentDeleteAnimationSyncMarkerCommand value:
                    if (!TryResolveAnimationMarker(session, value.Target, value.Marker, value.Path, out _, out _, out AnimationSyncMarker deleteMarker)) return false;
                    session.AddPlanned(value, null, deleteMarker?.AuthoringId ?? value.Marker.Identity, "delete animation marker");
                    return true;
                case AgentDeleteTimelineClipCommand value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip deleteClip)) return false;
                    session.AddPlanned(value, null, deleteClip.AuthoringId, "delete clip");
                    return true;
                case AgentEnsureTreeClipBlackboardWriteCommand value:
                    if (!ValidateTreeClipWriteTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Declaration.Identity, "ensure TreeClip Blackboard write");
                    return true;
                case AgentDeleteTransitionCommand value:
                    if (!TryResolveTransition(session, value, out StateMachineGraph transitionGraph, out BaseEdge transition)) return false;
                    session.AddPlanned(value, transitionGraph, transition.GUID, "delete transition");
                    return true;
                case AgentEnsureGameplayTagCommand value:
                    if (!ValidateTag(session, value.Tag, value.ParentTag, value.Path)) return false;
                    session.PlanGameplayTag(value.Tag.Value);
                    session.AddPlanned(value, null, value.Tag.Value, "ensure gameplay tag");
                    return true;
                case AgentSetActionProfileGrantedTagsCommand value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile grantedProfile) || !ValidateTags(session, value.Tags, value.Path)) return false;
                    session.AddPlanned(value, null, grantedProfile.ActionId, "set granted tags");
                    return true;
                case AgentSetActionProfileCancelQueryCommand value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile cancelProfile) ||
                        !ValidateTags(session, value.All.Concat(value.Any).Concat(value.None).ToList(), value.Path)) return false;
                    session.AddPlanned(value, null, cancelProfile.ActionId, "set cancel query");
                    return true;
                case AgentSetActionProfileTargetRequirementCommand value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile targetProfile)) return false;
                    session.AddPlanned(value, null, targetProfile.ActionId, $"set target requirement {value.TargetRequirement}");
                    return true;
                case AgentSetActionRequestTimingClassCommand value:
                    if (!TryResolveActionRequest(session, value.RequestId, value.Path, out _)) return false;
                    session.AddPlanned(value, null, value.RequestId, $"set request timing {value.TimingClass}");
                    return true;
                default:
                    throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        public void Apply(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentEnsureBlackboardDeclarationCommand value: ApplyEnsureDeclaration(session, value); break;
                case AgentMoveBlackboardDeclarationCommand value: ApplyMoveDeclaration(session, value); break;
                case AgentDeleteBlackboardDeclarationCommand value: ApplyDeleteDeclaration(session, value); break;
                case AgentEnsureBlackboardWriteCommand value: ApplyEnsureBlackboardWrite(session, value); break;
                case AgentEnsureTimelineTreeClipCommand value: ApplyEnsureTreeClip(session, value); break;
                case AgentEnsureMotionWarpTrackCommand value: ApplyEnsureMotionWarpTrack(session, value); break;
                case AgentEnsureMotionWarpClipCommand value: ApplyEnsureMotionWarpClip(session, value); break;
                case AgentConfigureMotionWarpSourceCommand value: ApplyConfigureMotionWarpSource(session, value); break;
                case AgentConfigureMotionWarpParametersCommand value: ApplyConfigureMotionWarpParameters(session, value); break;
                case AgentMoveTimelineClipCommand value: ApplyMoveTimelineClip(session, value); break;
                case AgentConfigureTimelineClipEaseCommand value: ApplyConfigureTimelineClipEase(session, value); break;
                case AgentConfigureTimelineCurveChannelCommand value: ApplyConfigureTimelineCurveChannel(session, value); break;
                case AgentConfigureAnimationTrackMarkerSyncCommand value: ApplyConfigureAnimationTrackMarkerSync(session, value); break;
                case AgentEnsureAnimationSyncMarkerCommand value: ApplyEnsureAnimationSyncMarker(session, value); break;
                case AgentMoveAnimationSyncMarkerCommand value: ApplyMoveAnimationSyncMarker(session, value); break;
                case AgentDeleteAnimationSyncMarkerCommand value: ApplyDeleteAnimationSyncMarker(session, value); break;
                case AgentDeleteTimelineClipCommand value: ApplyDeleteTimelineClip(session, value); break;
                case AgentEnsureTreeClipBlackboardWriteCommand value: ApplyEnsureTreeClipWrite(session, value); break;
                case AgentDeleteTransitionCommand value: ApplyDeleteTransition(session, value); break;
                case AgentEnsureGameplayTagCommand value: ApplyEnsureGameplayTag(session, value); break;
                case AgentSetActionProfileGrantedTagsCommand value: ApplyGrantedTags(session, value); break;
                case AgentSetActionProfileCancelQueryCommand value: ApplyCancelQuery(session, value); break;
                case AgentSetActionProfileTargetRequirementCommand value: ApplyTargetRequirement(session, value); break;
                case AgentSetActionRequestTimingClassCommand value: ApplyRequestTimingClass(session, value); break;
                default: throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        static void ApplyEnsureDeclaration(AgentPatchCompileSession session, AgentEnsureBlackboardDeclarationCommand command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return;
            BaseExposedProperty declaration = graph.ExposedProperties.FirstOrDefault(value =>
                !string.IsNullOrEmpty(command.DeclarationAuthoringId) && string.Equals(value.DeclarationId, command.DeclarationAuthoringId, StringComparison.Ordinal));
            declaration ??= graph.ExposedProperties.FirstOrDefault(value => string.Equals(value.BlackboardKey, command.Key, StringComparison.Ordinal));
            if (declaration != null && declaration.ValueType != command.ValueType)
            {
                session.Report.Error(command.Path, "blackboard_type_change_rejected", $"已有 declaration {command.Key} 是 {declaration.ValueType?.Name}，不能原位改成 {command.ValueType.Name}。");
                return;
            }
            if (declaration == null)
            {
                declaration = graph.CreateExposedProperty(ResolveDeclarationType(command.ValueType));
                if (!string.IsNullOrEmpty(command.DeclarationAuthoringId))
                    declaration.GUID = command.DeclarationAuthoringId;
            }
            declaration.Name = command.Key;
            declaration.ConfigurePipelineBlackboard(command.Key, command.Scope, command.Lifetime, command.Authority, command.SyncPolicy, command.InputValueId, command.CategoryPath);
            declaration.ConfigureFactProjection(command.FactProjection, command.WindowType, command.WindowId, command.Digest);
            graph.CheckInit();
            session.AddAppliedAuthoring(command, graph.SerializedOwner, declaration, command.Key, "ensure declaration");
        }

        static bool PreflightMoveDeclaration(AgentPatchCompileSession session, AgentMoveBlackboardDeclarationCommand command)
        {
            if (!session.TryResolveGraph(command.SourceGraph, command.Path, out BaseTree source) ||
                !session.TryResolveGraph(command.TargetGraph, command.Path, out BaseTree target))
                return false;
            if (ReferenceEquals(source, target))
            {
                session.Report.Error(command.Path, "blackboard_move_same_owner", "move_blackboard_declaration 的 source 与 target owner 相同。");
                return false;
            }

            BaseExposedProperty sourceDeclaration = source.ExposedProperties.FirstOrDefault(value => value.DeclarationId == command.DeclarationAuthoringId);
            BaseExposedProperty targetDeclaration = target.ExposedProperties.FirstOrDefault(value => value.DeclarationId == command.DeclarationAuthoringId);
            if (sourceDeclaration != null && targetDeclaration != null)
            {
                session.Report.Error(command.Path, "blackboard_move_identity_duplicated", $"Declaration identity {command.DeclarationAuthoringId} 同时存在于 source 与 target owner。");
                return false;
            }
            BaseExposedProperty declaration = sourceDeclaration ?? targetDeclaration;
            if (declaration == null)
            {
                session.Report.Error(command.Path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{command.DeclarationAuthoringId}");
                return false;
            }
            if (declaration.ValueType != command.ValueType)
            {
                session.Report.Error(command.Path, "blackboard_type_change_rejected", $"Declaration {command.DeclarationAuthoringId} 是 {declaration.ValueType?.Name}，不能迁移为 {command.ValueType.Name}。");
                return false;
            }
            BaseExposedProperty keyConflict = target.ExposedProperties.FirstOrDefault(value =>
                !ReferenceEquals(value, targetDeclaration) && string.Equals(value.BlackboardKey, command.Key, StringComparison.Ordinal));
            if (keyConflict != null)
            {
                session.Report.Error(command.Path, "blackboard_key_ambiguous", $"Target Graph {target.GraphAuthoringId} 已包含 key={command.Key} declaration。");
                return false;
            }
            if (!ValidateInputDerivedPolicy(
                    session,
                    command.Path,
                    command.ValueType,
                    command.Scope,
                    command.Lifetime,
                    command.Authority,
                    command.SyncPolicy,
                    command.InputValueId))
                return false;
            session.PlanBlackboardDeclaration(command.DeclarationAuthoringId, target.GraphAuthoringId, command.ValueType);
            session.AddPlanned(command, target, command.Key, sourceDeclaration != null ? "move declaration" : "declaration already moved");
            return true;
        }

        static void ApplyMoveDeclaration(AgentPatchCompileSession session, AgentMoveBlackboardDeclarationCommand command)
        {
            if (!session.TryResolveGraph(command.SourceGraph, command.Path, out BaseTree source) ||
                !session.TryResolveGraph(command.TargetGraph, command.Path, out BaseTree target))
                return;
            BaseExposedProperty sourceDeclaration = source.ExposedProperties.FirstOrDefault(value => value.DeclarationId == command.DeclarationAuthoringId);
            BaseExposedProperty declaration = target.ExposedProperties.FirstOrDefault(value => value.DeclarationId == command.DeclarationAuthoringId);
            if (sourceDeclaration != null && declaration != null)
            {
                session.Report.Error(command.Path, "blackboard_move_identity_duplicated", $"Declaration identity {command.DeclarationAuthoringId} 同时存在于 source 与 target owner。");
                return;
            }
            if (declaration == null)
            {
                if (sourceDeclaration == null)
                {
                    session.Report.Error(command.Path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{command.DeclarationAuthoringId}");
                    return;
                }
                source.DeleteExposedProperty(sourceDeclaration);
                declaration = target.CreateExposedProperty(ResolveDeclarationType(command.ValueType));
                declaration.GUID = command.DeclarationAuthoringId;
            }
            declaration.Name = command.Key;
            declaration.ConfigurePipelineBlackboard(command.Key, command.Scope, command.Lifetime, command.Authority, command.SyncPolicy, command.InputValueId, command.CategoryPath);
            declaration.ConfigureFactProjection(command.FactProjection, command.WindowType, command.WindowId, command.Digest);
            source.CheckInit();
            target.CheckInit();
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddAppliedAuthoring(command, target.SerializedOwner, declaration, command.Key, sourceDeclaration != null ? "moved declaration" : "declaration already moved");
        }

        static bool ValidateBlackboardDeclarationTarget(
            AgentPatchCompileSession session,
            AgentEnsureBlackboardDeclarationCommand command,
            out BaseTree graph)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out graph))
                return false;
            List<BaseExposedProperty> byKey = graph.ExposedProperties
                .Where(value => string.Equals(value.BlackboardKey, command.Key, StringComparison.Ordinal))
                .ToList();
            if (byKey.Count > 1)
            {
                session.Report.Error(command.Path, "blackboard_key_ambiguous", $"Graph {graph.GraphAuthoringId} 包含多个 key={command.Key} declaration。");
                return false;
            }
            BaseExposedProperty byIdentity = !string.IsNullOrEmpty(command.DeclarationAuthoringId)
                ? graph.ExposedProperties.FirstOrDefault(value => value.DeclarationId == command.DeclarationAuthoringId)
                : null;
            BaseExposedProperty byKeyDeclaration = byKey.SingleOrDefault();
            if (byIdentity != null && byKeyDeclaration != null && !ReferenceEquals(byIdentity, byKeyDeclaration))
            {
                session.Report.Error(command.Path, "blackboard_declaration_identity_conflict", $"Declaration identity {command.DeclarationAuthoringId} 与 key {command.Key} 指向不同声明。");
                return false;
            }
            if (byIdentity == null && byKeyDeclaration != null &&
                !string.IsNullOrEmpty(command.DeclarationAuthoringId) &&
                !string.Equals(byKeyDeclaration.DeclarationId, command.DeclarationAuthoringId, StringComparison.Ordinal))
            {
                session.Report.Error(command.Path, "blackboard_declaration_identity_mismatch", $"已有 key {command.Key} 的 identity 是 {byKeyDeclaration.DeclarationId}，不能改写为 {command.DeclarationAuthoringId}。");
                return false;
            }
            AgentSnapshotBlackboardDeclaration identityOwner = session.Snapshot.blackboardDeclarations.FirstOrDefault(value =>
                !string.IsNullOrEmpty(command.DeclarationAuthoringId) && value.declarationId == command.DeclarationAuthoringId);
            if (identityOwner != null && identityOwner.ownerId != graph.GraphAuthoringId)
            {
                session.Report.Error(command.Path, "blackboard_declaration_identity_conflict", $"Declaration identity {command.DeclarationAuthoringId} 已属于 owner {identityOwner.ownerId}。");
                return false;
            }
            BaseExposedProperty declaration = byIdentity ?? byKeyDeclaration;
            if (declaration != null && declaration.ValueType != command.ValueType)
            {
                session.Report.Error(command.Path, "blackboard_type_change_rejected", $"已有 declaration {command.Key} 是 {declaration.ValueType?.Name}，不能原位改成 {command.ValueType.Name}。");
                return false;
            }
            if (!ValidateInputDerivedPolicy(
                    session,
                    command.Path,
                    command.ValueType,
                    command.Scope,
                    command.Lifetime,
                    command.Authority,
                    command.SyncPolicy,
                    command.InputValueId))
                return false;
            return true;
        }

        static bool ValidateInputDerivedPolicy(
            AgentPatchCompileSession session,
            string path,
            Type valueType,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            PipelineBlackboardVariableAuthority authority,
            PipelineBlackboardVariableSyncPolicy syncPolicy,
            string inputValueId)
        {
            if (syncPolicy != PipelineBlackboardVariableSyncPolicy.InputDerived)
            {
                if (string.IsNullOrEmpty(inputValueId))
                    return true;
                session.Report.Error(path, "input_value_id_forbidden", "非 InputDerived declaration 不得保留 InputValueId。");
                return false;
            }
            if (valueType != typeof(ActionTargetSnapshot) ||
                scope != PipelineBlackboardVariableScope.Character ||
                lifetime != PipelineBlackboardVariableLifetime.Spawn ||
                authority == PipelineBlackboardVariableAuthority.PresentationOnly ||
                string.IsNullOrWhiteSpace(inputValueId))
            {
                session.Report.Error(path, "input_derived_policy_invalid", "InputDerived ActionTargetSnapshot declaration 必须使用 Character scope、Spawn lifetime、非 PresentationOnly authority，并提供稳定 InputValueId。");
                return false;
            }
            return true;
        }

        static void ApplyDeleteDeclaration(AgentPatchCompileSession session, AgentDeleteBlackboardDeclarationCommand command)
        {
            if (!TryResolveGraphDeclaration(session, command.Graph, command.DeclarationAuthoringId, command.Path, out BaseTree graph, out BaseExposedProperty declaration))
                return;
            graph.DeleteExposedProperty(declaration);
            graph.CheckInit();
            session.AddAppliedAuthoring(command, graph.SerializedOwner, null, declaration.BlackboardKey, "delete declaration");
        }

        static bool PreflightBlackboardWrite(AgentPatchCompileSession session, AgentEnsureBlackboardWriteCommand command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !TryResolveVisibleBoolDeclaration(session, graph, command.Declaration, command.Path, out _))
                return false;
            if (!graph.CanCreateNodeType(typeof(ExposedPropertyNode)))
            {
                session.Report.Error(command.Path, "blackboard_write_graph_rejected", $"Graph {graph.GraphAuthoringId} 不允许创建 ExposedPropertyNode。");
                return false;
            }
            if (!string.IsNullOrEmpty(command.ElementAuthoringId) &&
                (!session.TryResolveNode(graph, ElementReference(command.ElementAuthoringId), command.Path, out BaseNode existing) || existing is not ExposedPropertyNode))
            {
                session.Report.Error(command.Path, "blackboard_write_node_invalid", $"目标 element 不是 ExposedPropertyNode：{command.ElementAuthoringId}");
                return false;
            }
            session.AddPlanned(command, graph, command.DisplayName, $"ensure Bool write {command.Value}");
            return true;
        }

        static void ApplyEnsureBlackboardWrite(AgentPatchCompileSession session, AgentEnsureBlackboardWriteCommand command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !TryResolveVisibleBoolDeclaration(session, graph, command.Declaration, command.Path, out BaseExposedProperty declaration))
                return;

            ExposedPropertyNode node = null;
            if (!string.IsNullOrEmpty(command.ElementAuthoringId))
            {
                if (!session.TryResolveNode(graph, ElementReference(command.ElementAuthoringId), command.Path, out BaseNode existing))
                    return;
                node = existing as ExposedPropertyNode;
            }
            if (node == null)
            {
                List<ExposedPropertyNode> matches = graph.Nodes.OfType<ExposedPropertyNode>().Where(value =>
                    value.NodeType == ExposedPropertyNodeType.Set &&
                    value.BlackboardVariable.DeclarationId == declaration.DeclarationId &&
                    value.BlackboardVariable.DeclarationOwnerId == declaration.DeclarationOwnerId &&
                    value.Value.GetValue() is bool current && current == command.Value).ToList();
                if (matches.Count > 1)
                {
                    session.Report.Error(command.Path, "blackboard_write_ambiguous", $"Graph {graph.GraphAuthoringId} 已有多个同值 write，必须提供 stable node identity。");
                    return;
                }
                node = matches.SingleOrDefault() ?? graph.CreateNode(typeof(ExposedPropertyNode)) as ExposedPropertyNode;
            }
            node.SetNodeType(ExposedPropertyNodeType.Set);
            node.SetExposedProperty(declaration);
            node.Value.SetValue(command.Value);
            node.DisplayName = command.DisplayName;
            node.Position = command.Position;
            graph.CheckInit();
            session.AddApplied(command, graph, node, $"set {declaration.BlackboardKey}={command.Value}");
        }

        static void ApplyEnsureTreeClip(AgentPatchCompileSession session, AgentEnsureTimelineTreeClipCommand command)
        {
            if (!TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out TimelineData timeline))
                return;
            TreeClip clip = null;
            if (!string.IsNullOrEmpty(command.Target.ClipAuthoringId))
                clip = timeline.Tracks.SelectMany(value => value.Clips).OfType<TreeClip>().FirstOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            TreeTrack track = clip?.Track as TreeTrack ?? ResolveTreeTrack(timeline, command.Target.TrackAuthoringId);
            if (clip == null && track == null)
            {
                timeline.AddTrack(typeof(TreeTrack));
                track = timeline.Tracks.OfType<TreeTrack>().Last();
            }
            clip ??= timeline.AddClip(track, command.StartFrame) as TreeClip;
            clip.StartFrame = command.StartFrame;
            clip.EndFrame = command.EndFrame;
            if (!Enum.TryParse(command.Phase, true, out TimelineTreeExecutionPhase phase))
            {
                session.Report.Error(command.Path, "timeline_phase_invalid", $"Timeline TreeClip phase 无效：{command.Phase}");
                return;
            }
            clip.SetExecutionPhase(phase);
            if (clip.Ownership == TimelineTreeOwnership.Shared)
                clip.SetInlineTree(clip.ResolvedTree.CloneForAuthoring() as TimelineRunningTree);
            else
                clip.EnsureInlineTree();
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "ensure TreeClip");
        }

        static void ApplyDeleteTimelineClip(AgentPatchCompileSession session, AgentDeleteTimelineClipCommand command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            timeline.RemoveClip(clip);
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, command.Target.ClipAuthoringId, "delete clip");
        }

        static void ApplyMoveTimelineClip(AgentPatchCompileSession session, AgentMoveTimelineClipCommand command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            clip.StartFrame += command.FrameOffset;
            clip.EndFrame += command.FrameOffset;
            if (clip is MotionCurveClip motionCurve)
                motionCurve.CurveEndFrame += command.FrameOffset;
            clip.Track.UpdateMix();
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "move clip");
        }

        static void ApplyConfigureTimelineClipEase(AgentPatchCompileSession session, AgentConfigureTimelineClipEaseCommand command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            clip.SelfEaseInFrame = command.SelfEaseInFrame;
            clip.SelfEaseOutFrame = command.SelfEaseOutFrame;
            clip.Track.UpdateMix();
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "configure clip ease");
        }

        static void ApplyConfigureTimelineCurveChannel(
            AgentPatchCompileSession session,
            AgentConfigureTimelineCurveChannelCommand command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.Require(command.ChannelId.Value);
            descriptor.Replace(clip, command.Curve);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, $"configure Timeline curve {command.ChannelId}");
        }

        static void ApplyConfigureAnimationTrackMarkerSync(
            AgentPatchCompileSession session,
            AgentConfigureAnimationTrackMarkerSyncCommand command)
        {
            if (!TryResolveAnimationTrack(session, command.Target, command.Path, out TimelineData timeline, out AnimationTrack track))
                return;
            if (command.Mode == AnimationSyncMode.None)
                track.ConfigureNone();
            else
                track.ConfigureMarkerGroup(command.SyncGroupId, command.Topology, command.SyncRole);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, track, track.AuthoringId, "configure animation marker sync");
        }

        static void ApplyEnsureAnimationSyncMarker(
            AgentPatchCompileSession session,
            AgentEnsureAnimationSyncMarkerCommand command)
        {
            if (!TryResolveAnimationTrack(session, command.Target, command.Path, out TimelineData timeline, out AnimationTrack track))
                return;
            AnimationSyncMarker marker = track.EnsureMarker(command.MarkerAuthoringId, command.MarkerId, command.Frame);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, marker, marker.AuthoringId, "ensure animation marker");
        }

        static void ApplyMoveAnimationSyncMarker(
            AgentPatchCompileSession session,
            AgentMoveAnimationSyncMarkerCommand command)
        {
            if (!TryResolveAnimationMarker(session, command.Target, command.Marker, command.Path, out TimelineData timeline, out AnimationTrack track, out AnimationSyncMarker marker))
                return;
            track.MoveMarker(marker.AuthoringId, command.Frame);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, marker, marker.AuthoringId, "move animation marker");
        }

        static void ApplyDeleteAnimationSyncMarker(
            AgentPatchCompileSession session,
            AgentDeleteAnimationSyncMarkerCommand command)
        {
            if (!TryResolveAnimationMarker(session, command.Target, command.Marker, command.Path, out TimelineData timeline, out AnimationTrack track, out AnimationSyncMarker marker))
                return;
            string markerAuthoringId = marker.AuthoringId;
            track.DeleteMarker(markerAuthoringId);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, markerAuthoringId, "delete animation marker");
        }

        static void ApplyEnsureMotionWarpTrack(AgentPatchCompileSession session, AgentEnsureMotionWarpTrackCommand command)
        {
            if (!TryResolveTimeline(session, command.TimelineAuthoringId, command.Path, out TimelineData timeline))
                return;
            MotionWarpTrack track = string.IsNullOrEmpty(command.TrackAuthoringId)
                ? timeline.Tracks.OfType<MotionWarpTrack>().SingleOrDefault()
                : timeline.Tracks.OfType<MotionWarpTrack>().SingleOrDefault(value => value.AuthoringId == command.TrackAuthoringId);
            if (track == null)
            {
                timeline.AddTrack(typeof(MotionWarpTrack));
                track = timeline.Tracks.OfType<MotionWarpTrack>().Last();
            }
            track.Name = command.DisplayName;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, track, track.AuthoringId, "ensure MotionWarp track");
        }

        static void ApplyEnsureMotionWarpClip(AgentPatchCompileSession session, AgentEnsureMotionWarpClipCommand command)
        {
            if (!TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out TimelineData timeline) ||
                !TryResolveMotionWarpTrack(session, timeline, command.Target, command.Path, out MotionWarpTrack track))
                return;
            MotionWarpClip clip = null;
            if (!string.IsNullOrEmpty(command.Target.ClipAuthoringId))
                clip = track.Clips.OfType<MotionWarpClip>().SingleOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            clip ??= timeline.AddClip(track, command.StartFrame) as MotionWarpClip;
            clip.StartFrame = command.StartFrame;
            clip.EndFrame = command.EndFrame;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "ensure MotionWarp clip");
        }

        static void ApplyConfigureMotionWarpSource(AgentPatchCompileSession session, AgentConfigureMotionWarpSourceCommand command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out MotionWarpClip warp))
                return;
            if (!MotionWarpAuthoring.TryResolveSource(timeline, command.SourceMotionClipAuthoringId, out MotionCurveClip source))
            {
                session.Report.Error(command.Path, "motion_warp_source_not_found", $"MotionCurve source identity 无法解析：{command.SourceMotionClipAuthoringId}");
                return;
            }
            MotionWarpAuthoring.BindSource(timeline, warp, source);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, warp.AuthoringId, "configure MotionWarp source");
        }

        static void ApplyConfigureMotionWarpParameters(AgentPatchCompileSession session, AgentConfigureMotionWarpParametersCommand command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out MotionWarpClip warp))
                return;
            warp.ConfigureAuthoring(
                command.PositionMode,
                command.RotationMode,
                command.TargetLocalPlanarOffset,
                command.TargetYawOffsetDegrees,
                command.PositionWeight,
                command.YawWeight,
                command.MaxTotalPositionCorrection,
                command.MaxTotalYawCorrectionDegrees,
                command.PositionProgressCurve,
                command.YawProgressCurve);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, warp.AuthoringId, "configure MotionWarp parameters");
        }

        static void ApplyEnsureTreeClipWrite(AgentPatchCompileSession session, AgentEnsureTreeClipBlackboardWriteCommand command)
        {
            if (!TryResolveDeclaration(session, command.Declaration, command.Path, out BaseExposedProperty declaration))
            {
                session.Report.Error(command.Path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{command.Declaration.Identity}");
                return;
            }
            if (declaration.ValueType != typeof(bool))
            {
                session.Report.Error(command.Path, "blackboard_write_type_invalid", $"TreeClip write 只接受 Bool declaration，实际为 {declaration.ValueType?.Name ?? "Unknown"}。");
                return;
            }
            TimelineData timeline;
            TreeClip clip;
            if (command.Target.ClipOperationOutput.IsValid)
            {
                if (!session.TryResolveOperationOutput(command.Target.ClipOperationOutput, command.Path, out clip) ||
                    !TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out timeline))
                    return;
            }
            else
            {
                if (!TryResolveTimelineClip(session, command.Target, command.Path, out timeline, out Clip resolved) || resolved is not TreeClip treeClip)
                {
                    session.Report.Error(command.Path, "tree_clip_missing", "目标 Clip 不是 TreeClip。");
                    return;
                }
                clip = treeClip;
            }
            if (clip.Ownership != TimelineTreeOwnership.Inline || clip.ResolvedTree == null)
            {
                session.Report.Error(command.Path, "tree_clip_inline_required", "TreeClip Blackboard write 只允许写入正式 inline tree。先用 ensure_timeline_tree_clip 收口 ownership。");
                return;
            }
            TimelineRunningTree tree = clip.ResolvedTree;
            List<ExposedPropertyNode> setters = tree.Nodes
                .OfType<ExposedPropertyNode>()
                .Where(value => value.NodeType == ExposedPropertyNodeType.Set)
                .ToList();
            ExposedPropertyNode setter = setters.FirstOrDefault(value =>
                value.BlackboardVariable.DeclarationId == declaration.DeclarationId &&
                value.BlackboardVariable.DeclarationOwnerId == declaration.DeclarationOwnerId);
            if (setter == null && setters.Count == 1)
                setter = setters[0];
            if (setter == null && setters.Count > 1)
            {
                session.Report.Error(command.Path, "tree_clip_write_ambiguous", $"TreeClip {clip.AuthoringId} 包含多个 setter，无法安全决定要迁移哪一个。请先用 stable node identity 显式删除旧 write。");
                return;
            }
            if (setter == null)
            {
                setter = tree.CreateNode(typeof(ExposedPropertyNode)) as ExposedPropertyNode;
                setter.Position = new Vector2(320f, 0f);
                RootNode root = tree.Nodes.OfType<RootNode>().Single();
                tree.Link(root, setter, "Output", "Input");
            }
            setter.SetNodeType(ExposedPropertyNodeType.Set);
            setter.SetExposedProperty(declaration);
            setter.Value.SetValue(true);
            setter.DisplayName = $"Set {declaration.BlackboardKey}";
            tree.name = $"Decision {declaration.BlackboardKey}";
            tree.CheckInit();
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, setter, declaration.BlackboardKey, "ensure TreeClip write");
        }

        static void ApplyDeleteTransition(AgentPatchCompileSession session, AgentDeleteTransitionCommand command)
        {
            if (!TryResolveTransition(session, command, out StateMachineGraph graph, out BaseEdge edge))
                return;
            graph.UnLink(edge);
            session.AddAppliedAuthoring(command, graph.SerializedOwner, null, edge.GUID, "delete transition");
        }

        static void ApplyEnsureGameplayTag(AgentPatchCompileSession session, AgentEnsureGameplayTagCommand command)
        {
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile.TagCatalog;
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty tags = serialized.FindProperty("m_Tags");
            SerializedProperty entry = FindTag(tags, command.Tag.Value);
            if (entry == null)
            {
                int index = tags.arraySize;
                tags.InsertArrayElementAtIndex(index);
                entry = tags.GetArrayElementAtIndex(index);
            }
            entry.FindPropertyRelative("m_TagId").FindPropertyRelative("m_Value").stringValue = command.Tag.Value;
            entry.FindPropertyRelative("m_DisplayName").stringValue = command.DisplayName;
            entry.FindPropertyRelative("m_ParentTag").FindPropertyRelative("m_Value").stringValue = command.ParentTag.Value;
            entry.FindPropertyRelative("m_DebugCategory").stringValue = command.DebugCategory;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, catalog, null, command.Tag.Value, "ensure gameplay tag");
        }

        static void ApplyGrantedTags(AgentPatchCompileSession session, AgentSetActionProfileGrantedTagsCommand command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            WriteTagArray(serialized.FindProperty("m_Tags"), command.Tags);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, "set granted tags");
        }

        static void ApplyCancelQuery(AgentPatchCompileSession session, AgentSetActionProfileCancelQueryCommand command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty query = serialized.FindProperty("m_CancelTags");
            WriteTagArray(query.FindPropertyRelative("m_All"), command.All);
            WriteTagArray(query.FindPropertyRelative("m_Any"), command.Any);
            WriteTagArray(query.FindPropertyRelative("m_None"), command.None);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, "set cancel query");
        }

        static void ApplyTargetRequirement(
            AgentPatchCompileSession session,
            AgentSetActionProfileTargetRequirementCommand command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("m_TargetRequirement").enumValueIndex = (int)command.TargetRequirement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, $"set target requirement {command.TargetRequirement}");
        }

        static void ApplyRequestTimingClass(
            AgentPatchCompileSession session,
            AgentSetActionRequestTimingClassCommand command)
        {
            if (!TryResolveActionRequest(session, command.RequestId, command.Path, out CharacterActionRequestDefinition request))
                return;
            CharacterInputProfile profile = session.Definition.InputProfile;
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty requests = serialized.FindProperty("m_ActionRequests");
            SerializedProperty target = null;
            for (int i = 0; i < requests.arraySize; i++)
            {
                SerializedProperty candidate = requests.GetArrayElementAtIndex(i);
                if (string.Equals(
                        candidate.FindPropertyRelative("m_RequestId").stringValue,
                        request.RequestId,
                        StringComparison.Ordinal))
                {
                    target = candidate;
                    break;
                }
            }
            if (target == null)
                throw new InvalidOperationException($"Action request '{command.RequestId}' disappeared during apply.");
            target.FindPropertyRelative("m_TimingClass").intValue = (int)command.TimingClass;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, command.RequestId, "set request timing class");
        }

        static bool TryResolveActionRequest(
            AgentPatchCompileSession session,
            string requestId,
            string path,
            out CharacterActionRequestDefinition request)
        {
            request = null;
            CharacterInputProfile profile = session.Definition ? session.Definition.InputProfile : null;
            if (profile)
            {
                for (int i = 0; i < profile.ActionRequests.Count; i++)
                {
                    CharacterActionRequestDefinition candidate = profile.ActionRequests[i];
                    if (candidate != null && string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal))
                    {
                        request = candidate;
                        return true;
                    }
                }
            }
            session.Report.Error(path, "action_request_missing", $"Action request 无法解析：{requestId}");
            return false;
        }

        static bool TryResolveGraphDeclaration(AgentPatchCompileSession session, AgentGraphTargetReference graphReference, string declarationId, string path, out BaseTree graph, out BaseExposedProperty declaration)
        {
            declaration = null;
            if (!session.TryResolveGraph(graphReference, path, out graph))
                return false;
            declaration = graph?.ExposedProperties.FirstOrDefault(value => value.DeclarationId == declarationId);
            if (declaration != null)
                return true;
            session.Report.Error(path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{declarationId}");
            return false;
        }

        static bool TryResolveDeclaration(BaseTree root, string declarationId, out BaseExposedProperty declaration)
        {
            declaration = null;
            if (root == null)
                return false;
            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(root, errors);
            declaration = projection.Graphs.SelectMany(value => value.Graph.ExposedProperties).FirstOrDefault(value => value.DeclarationId == declarationId);
            return declaration != null;
        }

        static bool TryResolveDeclaration(
            AgentPatchCompileSession session,
            AgentAuthoringReference reference,
            string path,
            out BaseExposedProperty declaration)
        {
            declaration = null;
            if (reference.OperationOutput.IsValid)
                return session.TryResolveOperationOutput(reference.OperationOutput, path, out declaration);
            if (TryResolveDeclaration(session.RootTree, reference.AuthoringId, out declaration))
                return true;
            session.Report.Error(path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{reference.Identity}");
            return false;
        }

        static bool TryResolveVisibleBoolDeclaration(
            AgentPatchCompileSession session,
            BaseTree graph,
            AgentAuthoringReference declarationReference,
            string path,
            out BaseExposedProperty declaration)
        {
            declaration = null;
            if (declarationReference.OperationOutput.IsValid)
            {
                if (!session.TryResolveOperationOutput(declarationReference.OperationOutput, path, out declaration))
                    return false;
                if (!session.IsApply)
                    return true;
            }
            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(session.RootTree, errors);
            if (!projection.IsValid)
            {
                session.Report.Error(path, "blackboard_topology_invalid", string.Join("\n", errors));
                return false;
            }
            CharacterAuthoringGraphEntry entry = projection.Graphs.FirstOrDefault(value => ReferenceEquals(value.Graph, graph));
            declaration ??= projection.Graphs
                .SelectMany(value => value.Graph.ExposedProperties)
                .FirstOrDefault(value => value.DeclarationId == declarationReference.AuthoringId);
            if (declaration == null)
            {
                if (!session.TryGetPlannedBlackboardDeclaration(declarationReference.AuthoringId, out string plannedOwnerId, out Type plannedType))
                {
                    session.Report.Error(path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{declarationReference.Identity}");
                    return false;
                }
                if (plannedType != typeof(bool))
                {
                    session.Report.Error(path, "blackboard_write_type_invalid", $"ensure_blackboard_write 只接受 Bool declaration，实际为 {plannedType?.Name ?? "Unknown"}。");
                    return false;
                }
                if (entry.Graph == null || !entry.VisibleGraphs.OfType<BaseTree>().Any(value => value.GraphAuthoringId == plannedOwnerId))
                {
                    session.Report.Error(path, "blackboard_declaration_not_visible", $"Planned declaration {declarationReference.Identity} 对 Graph {graph.GraphAuthoringId} 不可见。");
                    return false;
                }
                return true;
            }
            if (declaration.ValueType != typeof(bool))
            {
                session.Report.Error(path, "blackboard_write_type_invalid", $"ensure_blackboard_write 只接受 Bool declaration，实际为 {declaration.ValueType?.Name ?? "Unknown"}。");
                return false;
            }
            string declarationOwnerId = declaration.DeclarationOwnerId;
            if (entry.Graph == null || !entry.VisibleGraphs.OfType<BaseTree>().Any(value => value.GraphAuthoringId == declarationOwnerId))
            {
                session.Report.Error(path, "blackboard_declaration_not_visible", $"Declaration {declaration.BlackboardKey}/{declarationReference.Identity} 对 Graph {graph.GraphAuthoringId} 不可见。");
                return false;
            }
            return true;
        }

        static AgentElementTargetReference ElementReference(string authoringId)
        {
            return new AgentElementTargetReference(new AgentAuthoringReference(authoringId, default));
        }

        static bool TryResolveTimeline(AgentPatchCompileSession session, string timelineId, string path, out TimelineData timeline)
        {
            timeline = null;
            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(session.RootTree, errors);
            if (!projection.IsValid)
            {
                session.Report.Error(path, "timeline_topology_invalid", string.Join("\n", errors));
                return false;
            }
            timeline = projection.Timelines.Select(value => value.Timeline).Distinct().FirstOrDefault(value => value.AuthoringId == timelineId);
            if (timeline != null)
                return true;
            session.Report.Error(path, "timeline_not_found", $"Timeline identity 无法解析：{timelineId}");
            return false;
        }

        static bool TryResolveTimelineClip(AgentPatchCompileSession session, AgentTimelineTargetReference target, string path, out TimelineData timeline, out Clip clip)
        {
            clip = null;
            if (!TryResolveTimeline(session, target.TimelineAuthoringId, path, out timeline))
                return false;
            IEnumerable<Track> tracks = timeline.Tracks;
            if (!string.IsNullOrEmpty(target.TrackAuthoringId))
            {
                Track track = timeline.Tracks.FirstOrDefault(value => value.AuthoringId == target.TrackAuthoringId);
                if (track == null)
                {
                    session.Report.Error(path, "timeline_track_not_found", $"Timeline Track identity 无法解析：{target.TrackAuthoringId}");
                    return false;
                }
                tracks = new[] { track };
            }
            clip = tracks.SelectMany(value => value.Clips).FirstOrDefault(value => value.AuthoringId == target.ClipAuthoringId);
            if (clip != null)
                return true;
            session.Report.Error(path, "timeline_clip_not_found", $"Timeline Clip identity 无法解析：{target.ClipAuthoringId}");
            return false;
        }

        static bool TryResolveAnimationTrack(
            AgentPatchCompileSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out AnimationTrack track)
        {
            track = null;
            if (!TryResolveTimeline(session, target.TimelineAuthoringId, path, out timeline))
                return false;
            if (target.TrackOperationOutput.IsValid)
            {
                if (!session.TryResolveOperationOutput(target.TrackOperationOutput, path, out Track outputTrack))
                    return false;
                if (!session.IsApply)
                    return true;
                track = outputTrack as AnimationTrack;
            }
            else
            {
                track = timeline.Tracks.OfType<AnimationTrack>().SingleOrDefault(value =>
                    string.Equals(value.AuthoringId, target.TrackAuthoringId, StringComparison.Ordinal));
            }
            if (track != null)
                return true;
            session.Report.Error(path, "animation_track_not_found", $"AnimationTrack identity 无法解析：{target.TrackAuthoringId}");
            return false;
        }

        static bool TryResolveAnimationMarker(
            AgentPatchCompileSession session,
            AgentTimelineTargetReference target,
            AgentAuthoringReference markerReference,
            string path,
            out TimelineData timeline,
            out AnimationTrack track,
            out AnimationSyncMarker marker)
        {
            marker = null;
            if (!TryResolveAnimationTrack(session, target, path, out timeline, out track))
                return false;
            if (markerReference.OperationOutput.IsValid)
            {
                if (!session.TryResolveOperationOutput(markerReference.OperationOutput, path, out marker))
                    return false;
                if (!session.IsApply)
                    return true;
            }
            else
            {
                marker = track?.SyncMarkers.SingleOrDefault(value =>
                    string.Equals(value.AuthoringId, markerReference.AuthoringId, StringComparison.Ordinal));
            }
            if (marker != null && (track == null || track.SyncMarkers.Contains(marker)))
                return true;
            session.Report.Error(path, "animation_marker_not_found", $"Animation marker identity 无法解析：{markerReference.Identity}");
            return false;
        }

        static bool ValidateTimelineClipEase(
            AgentPatchCompileSession session,
            AgentConfigureTimelineClipEaseCommand command,
            Clip clip)
        {
            if (command.SelfEaseInFrame + command.SelfEaseOutFrame > clip.Duration)
            {
                session.Report.Error(command.Path, "timeline_clip_ease_duration_exceeded",
                    $"Timeline Clip '{clip.AuthoringId}' 的 self ease 总和不能超过 Duration {clip.Duration}。");
                return false;
            }

            CalculateOtherEaseFrames(clip, out int otherEaseInFrame, out int otherEaseOutFrame);
            if (command.SelfEaseInFrame > clip.Duration - otherEaseOutFrame ||
                command.SelfEaseOutFrame > clip.Duration - otherEaseInFrame)
            {
                session.Report.Error(command.Path, "timeline_clip_ease_overlap_conflict",
                    $"Timeline Clip '{clip.AuthoringId}' 的 self ease 与当前 overlap 冲突：other={otherEaseInFrame}/{otherEaseOutFrame}。");
                return false;
            }
            return true;
        }

        static void CalculateOtherEaseFrames(Clip clip, out int otherEaseInFrame, out int otherEaseOutFrame)
        {
            otherEaseInFrame = 0;
            otherEaseOutFrame = 0;
            if (clip.Invalid)
                return;
            foreach (Clip other in clip.Track.Clips)
            {
                if (other == clip || other.Invalid)
                    continue;
                if (other.StartFrame < clip.StartFrame && other.EndFrame > clip.EndFrame ||
                    other.StartFrame > clip.StartFrame && other.EndFrame < clip.EndFrame)
                    return;
                if (other.StartFrame < clip.StartFrame && other.EndFrame > clip.StartFrame)
                    otherEaseInFrame = other.EndFrame - clip.StartFrame;
                if (other.StartFrame > clip.StartFrame && other.StartFrame < clip.EndFrame)
                    otherEaseOutFrame = clip.EndFrame - other.StartFrame;
                if (other.StartFrame != clip.StartFrame)
                    continue;
                if (other.EndFrame < clip.EndFrame)
                    otherEaseInFrame = other.EndFrame - clip.StartFrame;
                else if (other.EndFrame > clip.EndFrame)
                    otherEaseOutFrame = clip.EndFrame - clip.StartFrame;
            }
        }

        static bool TryResolveTransition(AgentPatchCompileSession session, AgentDeleteTransitionCommand command, out StateMachineGraph graph, out BaseEdge edge)
        {
            edge = null;
            if (!session.TryResolveStateMachine(command.StateMachine, command.Path, out graph))
                return false;
            edge = graph?.Edges.FirstOrDefault(value => value.GUID == command.EdgeAuthoringId);
            if (edge != null)
                return true;
            session.Report.Error(command.Path, "transition_not_found", $"Transition identity 无法解析：{command.EdgeAuthoringId}");
            return false;
        }

        static bool TryResolveProfile(AgentPatchCompileSession session, AgentAssetReference reference, string path, out ActionProfile profile)
        {
            if (session.Resolver.TryResolveActionProfile(reference.LogicalId, out profile))
                return true;
            session.Report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{reference.LogicalId}");
            return false;
        }

        static bool ValidateTag(AgentPatchCompileSession session, GameplayTagId tag, GameplayTagId parent, string path)
        {
            if (!tag.IsValid)
            {
                session.Report.Error(path, "gameplay_tag_invalid", "GameplayTag id 缺失。");
                return false;
            }
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile?.TagCatalog;
            if (!catalog)
            {
                session.Report.Error(path, "gameplay_tag_catalog_missing", "GameplayTagCatalog 缺失。");
                return false;
            }
            if (parent.IsValid &&
                !catalog.Tags.Any(value => value != null && value.TagId == parent) &&
                !session.IsGameplayTagPlanned(parent.Value))
            {
                session.Report.Error(path, "gameplay_tag_parent_missing", $"GameplayTag parent 未注册：{parent}");
                return false;
            }
            return true;
        }

        static bool ValidateTags(AgentPatchCompileSession session, IReadOnlyList<GameplayTagId> tags, string path)
        {
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile?.TagCatalog;
            if (!catalog)
            {
                session.Report.Error(path, "gameplay_tag_catalog_missing", "GameplayTagCatalog 缺失。");
                return false;
            }
            bool valid = true;
            for (int i = 0; i < tags.Count; i++)
            {
                if (catalog.Tags.Any(value => value != null && value.TagId == tags[i]) || session.IsGameplayTagPlanned(tags[i].Value))
                    continue;
                session.Report.Error(path, "gameplay_tag_missing", $"GameplayTag 未注册：{tags[i]}");
                valid = false;
            }
            return valid;
        }

        static TreeTrack ResolveTreeTrack(TimelineData timeline, string trackId)
        {
            return string.IsNullOrEmpty(trackId)
                ? timeline.Tracks.OfType<TreeTrack>().FirstOrDefault()
                : timeline.Tracks.OfType<TreeTrack>().FirstOrDefault(value => value.AuthoringId == trackId);
        }

        static bool ValidateMotionWarpTrackTarget(AgentPatchCompileSession session, AgentEnsureMotionWarpTrackCommand command)
        {
            if (!TryResolveTimeline(session, command.TimelineAuthoringId, command.Path, out TimelineData timeline))
                return false;
            if (!string.IsNullOrEmpty(command.TrackAuthoringId))
            {
                Track existing = timeline.Tracks.SingleOrDefault(value => value.AuthoringId == command.TrackAuthoringId);
                if (existing is MotionWarpTrack)
                    return true;
                session.Report.Error(command.Path, "motion_warp_track_not_found", $"MotionWarpTrack identity 无法解析：{command.TrackAuthoringId}");
                return false;
            }
            int count = timeline.Tracks.OfType<MotionWarpTrack>().Count();
            if (count <= 1)
                return true;
            session.Report.Error(command.Path, "motion_warp_track_ambiguous", "Timeline 已有多个 MotionWarpTrack，必须提供 stable track identity。");
            return false;
        }

        static bool ValidateMotionWarpClipTarget(AgentPatchCompileSession session, AgentEnsureMotionWarpClipCommand command)
        {
            if (command.StartFrame >= command.EndFrame)
            {
                session.Report.Error(command.Path, "motion_warp_window_invalid", "MotionWarp clip requires StartFrame < EndFrame.");
                return false;
            }
            if (!TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out TimelineData timeline) ||
                !TryResolveMotionWarpTrack(session, timeline, command.Target, command.Path, out MotionWarpTrack track))
                return false;
            if (!ValidateMotionWarpActionRequirement(session, timeline, command.Path))
                return false;
            if (string.IsNullOrEmpty(command.Target.ClipAuthoringId))
                return true;
            Clip existing = timeline.Tracks.SelectMany(value => value.Clips).SingleOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            if (existing is MotionWarpClip warp && (track == null || ReferenceEquals(warp.Track, track)))
                return true;
            session.Report.Error(command.Path, "motion_warp_clip_not_found", $"MotionWarpClip identity 无法解析：{command.Target.ClipAuthoringId}");
            return false;
        }

        static bool ValidateMotionWarpSource(AgentPatchCompileSession session, AgentConfigureMotionWarpSourceCommand command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out _))
                return false;
            if (!ValidateMotionWarpActionRequirement(session, timeline, command.Path))
                return false;
            if (MotionWarpAuthoring.TryResolveSource(timeline, command.SourceMotionClipAuthoringId, out _))
                return true;
            session.Report.Error(command.Path, "motion_warp_source_not_found", $"MotionCurve source identity 无法解析：{command.SourceMotionClipAuthoringId}");
            return false;
        }

        static bool ValidateMotionWarpParameters(AgentPatchCompileSession session, AgentConfigureMotionWarpParametersCommand command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out _) ||
                !ValidateMotionWarpActionRequirement(session, timeline, command.Path))
                return false;
            var issues = new List<MotionWarpAuthoringIssue>();
            bool valid = MotionWarpAuthoring.ValidateConfiguration(
                command.PositionMode,
                command.RotationMode,
                command.TargetLocalPlanarOffset,
                command.TargetYawOffsetDegrees,
                command.PositionWeight,
                command.YawWeight,
                command.MaxTotalPositionCorrection,
                command.MaxTotalYawCorrectionDegrees,
                command.PositionProgressCurve,
                command.YawProgressCurve,
                issues);
            for (int i = 0; i < issues.Count; i++)
                session.Report.Error(command.Path, issues[i].Code, issues[i].Message);
            return valid;
        }

        static bool TryResolveMotionWarpTrack(
            AgentPatchCompileSession session,
            TimelineData timeline,
            AgentTimelineTargetReference target,
            string path,
            out MotionWarpTrack track)
        {
            track = null;
            if (target.TrackOperationOutput.IsValid)
                return session.TryResolveOperationOutput(target.TrackOperationOutput, path, out track);
            track = timeline.Tracks.OfType<MotionWarpTrack>().SingleOrDefault(value => value.AuthoringId == target.TrackAuthoringId);
            if (track != null)
                return true;
            session.Report.Error(path, "motion_warp_track_not_found", $"MotionWarpTrack identity 无法解析：{target.TrackAuthoringId}");
            return false;
        }

        static bool TryResolveMotionWarpClip(
            AgentPatchCompileSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out MotionWarpClip clip)
        {
            clip = null;
            if (!TryResolveTimeline(session, target.TimelineAuthoringId, path, out timeline))
                return false;
            if (target.ClipOperationOutput.IsValid)
                return session.TryResolveOperationOutput(target.ClipOperationOutput, path, out clip);
            clip = timeline.Tracks.SelectMany(value => value.Clips).OfType<MotionWarpClip>().SingleOrDefault(value => value.AuthoringId == target.ClipAuthoringId);
            if (clip != null)
                return true;
            session.Report.Error(path, "motion_warp_clip_not_found", $"MotionWarpClip identity 无法解析：{target.ClipAuthoringId}");
            return false;
        }

        static bool ValidateMotionWarpActionRequirement(AgentPatchCompileSession session, TimelineData timeline, string path)
        {
            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(session.RootTree, errors);
            if (!projection.IsValid)
            {
                session.Report.Error(path, "motion_warp_topology_invalid", string.Join("\n", errors));
                return false;
            }
            bool found = false;
            bool valid = true;
            for (int graphIndex = 0; graphIndex < projection.Graphs.Count; graphIndex++)
            {
                if (projection.Graphs[graphIndex].Graph is not StateBehaviorSubTree graph)
                    continue;
                foreach (TimelineNode timelineNode in graph.Nodes.OfType<TimelineNode>().Where(value => ReferenceEquals(value.Timeline, timeline)))
                {
                    found = true;
                    List<ActivateActionInstanceNode> matches = graph.Nodes
                        .OfType<ActivateActionInstanceNode>()
                        .Where(value => value.ActionContext == timelineNode.ActionContext)
                        .ToList();
                    ThirdPersonSimulation.ActionTargetRequirement requirement = matches.Count == 1 && matches[0].ActionProfile
                        ? session.ResolveEffectiveTargetRequirement(matches[0].ActionProfile)
                        : ThirdPersonSimulation.ActionTargetRequirement.None;
                    if (!timelineNode.ActionContext || matches.Count != 1 || !matches[0].ActionProfile ||
                        requirement == ThirdPersonSimulation.ActionTargetRequirement.None ||
                        !session.HasEffectiveTargetSnapshot(matches[0]))
                    {
                        session.Report.Error(path, "motion_warp_target_requirement_invalid", "MotionWarp Timeline 必须由唯一、显式 Action Context、OptionalSnapshot 或 SnapshotRequired Profile及有效TargetSnapshot declaration 的 Action activation 拥有。");
                        valid = false;
                    }
                }
            }
            if (!found)
            {
                session.Report.Error(path, "motion_warp_timeline_owner_missing", "MotionWarp Timeline 没有可解析的 StateBehavior TimelineNode owner。");
                return false;
            }
            return valid;
        }

        static bool ValidateTimelineTreeClipTarget(AgentPatchCompileSession session, AgentEnsureTimelineTreeClipCommand command)
        {
            if (!TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out TimelineData timeline))
                return false;
            if (!Enum.TryParse(command.Phase, true, out TimelineTreeExecutionPhase _))
            {
                session.Report.Error(command.Path, "timeline_phase_invalid", $"Timeline TreeClip phase 无效：{command.Phase}");
                return false;
            }
            if (!string.IsNullOrEmpty(command.Target.TrackAuthoringId) && ResolveTreeTrack(timeline, command.Target.TrackAuthoringId) == null)
            {
                session.Report.Error(command.Path, "timeline_track_not_found", $"TreeTrack identity 无法解析：{command.Target.TrackAuthoringId}");
                return false;
            }
            if (string.IsNullOrEmpty(command.Target.ClipAuthoringId))
                return true;
            Clip clip = timeline.Tracks.SelectMany(value => value.Clips).FirstOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            if (clip is not TreeClip treeClip)
            {
                session.Report.Error(command.Path, "tree_clip_not_found", $"TreeClip identity 无法解析：{command.Target.ClipAuthoringId}");
                return false;
            }
            if (!string.IsNullOrEmpty(command.Target.TrackAuthoringId) && treeClip.Track.AuthoringId != command.Target.TrackAuthoringId)
            {
                session.Report.Error(command.Path, "tree_clip_track_mismatch", $"TreeClip {treeClip.AuthoringId} 不属于 Track {command.Target.TrackAuthoringId}。");
                return false;
            }
            return true;
        }

        static bool ValidateTreeClipWriteTarget(AgentPatchCompileSession session, AgentEnsureTreeClipBlackboardWriteCommand command)
        {
            if (!TryResolveDeclaration(session, command.Declaration, command.Path, out BaseExposedProperty declaration))
                return false;
            if (declaration != null && declaration.ValueType != typeof(bool))
            {
                session.Report.Error(command.Path, "blackboard_write_type_invalid", $"TreeClip write 只接受 Bool declaration，实际为 {declaration.ValueType?.Name ?? "Unknown"}。");
                return false;
            }
            if (command.Target.ClipOperationOutput.IsValid)
                return TryResolveTimeline(session, command.Target.TimelineAuthoringId, command.Path, out _);
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out _, out Clip clip))
                return false;
            if (clip is not TreeClip treeClip)
            {
                session.Report.Error(command.Path, "tree_clip_missing", "目标 Clip 不是 TreeClip。");
                return false;
            }
            if (treeClip.Ownership != TimelineTreeOwnership.Inline || treeClip.ResolvedTree == null)
            {
                session.Report.Error(command.Path, "tree_clip_inline_required", "TreeClip Blackboard write 只允许写入正式 inline tree。");
                return false;
            }
            int setterCount = treeClip.ResolvedTree.Nodes.OfType<ExposedPropertyNode>().Count(value => value.NodeType == ExposedPropertyNodeType.Set);
            if (setterCount > 1)
            {
                session.Report.Error(command.Path, "tree_clip_write_ambiguous", $"TreeClip {treeClip.AuthoringId} 包含多个 setter，无法安全迁移。");
                return false;
            }
            return true;
        }

        static Type ResolveDeclarationType(Type valueType)
        {
            if (valueType == typeof(bool)) return typeof(BoolExposedProperty);
            if (valueType == typeof(int)) return typeof(IntExposedProperty);
            if (valueType == typeof(float)) return typeof(FloatExposedProperty);
            if (valueType == typeof(string)) return typeof(StringExposedProperty);
            if (valueType == typeof(Vector2)) return typeof(Vector2ExposedProperty);
            if (valueType == typeof(Vector3)) return typeof(Vector3ExposedProperty);
            if (valueType == typeof(ActionTargetSnapshot)) return typeof(ActionTargetSnapshotExposedProperty);
            throw new InvalidOperationException($"Unsupported Blackboard value type: {valueType}");
        }

        static SerializedProperty FindTag(SerializedProperty tags, string tag)
        {
            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty entry = tags.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("m_TagId").FindPropertyRelative("m_Value").stringValue == tag)
                    return entry;
            }
            return null;
        }

        static void WriteTagArray(SerializedProperty array, IReadOnlyList<GameplayTagId> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).FindPropertyRelative("m_Value").stringValue = values[i].Value;
        }
    }
}
