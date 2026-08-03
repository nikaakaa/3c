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
    public sealed class AgentActionEligibilityMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureBlackboardDeclarationMutation value:
                    if (!ValidateBlackboardDeclarationTarget(session, value, out BaseTree declarationGraph)) return false;
                    session.PlanBlackboardDeclaration(
                        string.IsNullOrEmpty(value.DeclarationAuthoringId)
                            ? value.Id
                            : value.DeclarationAuthoringId,
                        declarationGraph.GraphAuthoringId,
                        value.ValueType,
                        value.Key);
                    session.AddPlanned(value, declarationGraph, value.Key, "ensure declaration");
                    return true;
                case AgentMoveBlackboardDeclarationMutation value:
                    return PreflightMoveDeclaration(session, value);
                case AgentDeleteBlackboardDeclarationMutation value:
                    if (!TryResolveGraphDeclaration(session, value.Graph, value.DeclarationAuthoringId, value.Path, out BaseTree deleteGraph, out BaseExposedProperty declaration)) return false;
                    session.AddPlanned(value, deleteGraph, declaration.BlackboardKey, "delete declaration");
                    return true;
                case AgentEnsureBlackboardWriteMutation value:
                    return PreflightBlackboardWrite(session, value);
                case AgentEnsureTimelineTreeClipMutation value:
                    if (!ValidateTimelineTreeClipTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, $"ensure TreeClip {value.StartFrame}..{value.EndFrame}/{value.Phase}");
                    return true;
                case AgentEnsureInlineTimelineMutation value:
                    if (!ValidateInlineTimelineTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.DisplayName, "ensure inline Timeline");
                    return true;
                case AgentEnsureMotionCurveTrackMutation value:
                    if (!ValidateMotionCurveTrackTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.DisplayName, "ensure MotionCurve track");
                    return true;
                case AgentEnsureMotionCurveClipMutation value:
                    if (!ValidateMotionCurveClipTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, $"ensure MotionCurve clip {value.StartFrame}..{value.EndFrame}");
                    return true;
                case AgentConfigureMotionCurveClipMutation value:
                    if (!ValidateMotionCurveClipConfiguration(session, value)) return false;
                    session.AddPlanned(value, null, value.CurveId, "configure MotionCurve clip");
                    return true;
                case AgentEnsureMotionWarpTrackMutation value:
                    if (!ValidateMotionWarpTrackTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.TrackAuthoringId, "ensure MotionWarp track");
                    return true;
                case AgentDeleteTimelineTrackMutation value:
                    if (!TryResolveTimelineTrack(session, value.TimelineAuthoringId, value.TrackAuthoringId, value.Path, out _, out Track deleteTrack)) return false;
                    session.AddPlanned(value, null, deleteTrack.AuthoringId, "delete track");
                    return true;
                case AgentEnsureMotionWarpClipMutation value:
                    if (!ValidateMotionWarpClipTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, $"ensure MotionWarp clip {value.StartFrame}..{value.EndFrame}");
                    return true;
                case AgentConfigureMotionWarpSourceMutation value:
                    if (!ValidateMotionWarpSource(session, value)) return false;
                    session.AddPlanned(value, null, value.SourceMotionClipAuthoringId, "configure MotionWarp source");
                    return true;
                case AgentConfigureMotionWarpParametersMutation value:
                    if (!ValidateMotionWarpParameters(session, value)) return false;
                    session.AddPlanned(value, null, value.Target.ClipAuthoringId, "configure MotionWarp parameters");
                    return true;
                case AgentMoveTimelineClipMutation value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip moveClip)) return false;
                    if (moveClip.StartFrame + value.FrameOffset < 0)
                    {
                        session.Report.Error(value.Path, "timeline_clip_start_negative", $"Timeline Clip '{moveClip.AuthoringId}' 平移后的 StartFrame 不能小于 0。");
                        return false;
                    }
                    session.AddPlanned(value, null, moveClip.AuthoringId,
                        $"move clip {moveClip.StartFrame}..{moveClip.EndFrame} -> {moveClip.StartFrame + value.FrameOffset}..{moveClip.EndFrame + value.FrameOffset}");
                    return true;
                case AgentConfigureTimelineClipEaseMutation value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip easeClip)) return false;
                    if (easeClip != null && !ValidateTimelineClipEase(session, value, easeClip)) return false;
                    session.AddPlanned(value, null, easeClip?.AuthoringId ?? value.Target.ClipPlannedIdentity.Value,
                        easeClip == null
                            ? $"configure clip ease self {value.SelfEaseInFrame}/{value.SelfEaseOutFrame}"
                            : $"configure clip ease self {easeClip.SelfEaseInFrame}/{easeClip.SelfEaseOutFrame} -> {value.SelfEaseInFrame}/{value.SelfEaseOutFrame}");
                    return true;
                case AgentConfigureTimelineCurveChannelMutation value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip curveOwner)) return false;
                    TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.Require(value.ChannelId.Value);
                    if (curveOwner != null && !descriptor.Supports(curveOwner))
                    {
                        session.Report.Error(value.Path, "timeline_curve_owner_incompatible", $"Timeline Clip '{curveOwner.AuthoringId}' 不拥有Curve Channel '{value.ChannelId}'。");
                        return false;
                    }
                    if (curveOwner != null)
                    {
                        try
                        {
                            descriptor.Validate(curveOwner, value.Curve);
                        }
                        catch (InvalidOperationException exception)
                        {
                            session.Report.Error(value.Path, "timeline_curve_owner_validation_failed", exception.Message);
                            return false;
                        }
                    }
                    session.AddPlanned(value, null, curveOwner?.AuthoringId ?? value.Target.ClipPlannedIdentity.Value, $"configure Timeline curve {value.ChannelId}");
                    return true;
                case AgentConfigureAnimationTrackChannelMutation value:
                    if (!TryResolveAnimationTrack(session, value.Target, value.Path, out _, out AnimationTrack channelTrack)) return false;
                    session.AddPlanned(value, null, channelTrack.AuthoringId,
                        $"configure animation channel {channelTrack.AnimationChannelId} -> {value.AnimationChannelId}");
                    return true;
                case AgentConfigureAnimationTrackMarkerSyncMutation value:
                    if (!TryResolveAnimationTrack(session, value.Target, value.Path, out _, out AnimationTrack configureTrack)) return false;
                    session.AddPlanned(value, null, configureTrack?.AuthoringId ?? value.Target.TrackAuthoringId,
                        $"configure marker sync {value.Mode}/{value.SyncGroupId}/{value.Topology}/{value.SyncRole}");
                    return true;
                case AgentEnsureAnimationSyncMarkerMutation value:
                    if (!TryResolveAnimationTrack(session, value.Target, value.Path, out _, out _)) return false;
                    session.AddPlanned(value, null, value.MarkerAuthoringId,
                        $"ensure animation marker {value.MarkerId}@{value.Frame}");
                    return true;
                case AgentMoveAnimationSyncMarkerMutation value:
                    if (!TryResolveAnimationMarker(session, value.Target, value.Marker, value.Path, out _, out _, out AnimationSyncMarker moveMarker)) return false;
                    session.AddPlanned(value, null, moveMarker?.AuthoringId ?? value.Marker.Identity,
                        $"move animation marker to {value.Frame}");
                    return true;
                case AgentDeleteAnimationSyncMarkerMutation value:
                    if (!TryResolveAnimationMarker(session, value.Target, value.Marker, value.Path, out _, out _, out AnimationSyncMarker deleteMarker)) return false;
                    session.AddPlanned(value, null, deleteMarker?.AuthoringId ?? value.Marker.Identity, "delete animation marker");
                    return true;
                case AgentDeleteTimelineClipMutation value:
                    if (!TryResolveTimelineClip(session, value.Target, value.Path, out _, out Clip deleteClip)) return false;
                    session.AddPlanned(value, null, deleteClip.AuthoringId, "delete clip");
                    return true;
                case AgentEnsureTreeClipBlackboardWriteMutation value:
                    if (!ValidateTreeClipWriteTarget(session, value)) return false;
                    session.AddPlanned(value, null, value.Declaration.Identity, "ensure TreeClip Blackboard write");
                    return true;
                case AgentDeleteTransitionMutation value:
                    if (!TryResolveTransition(session, value, out StateMachineGraph transitionGraph, out BaseEdge transition)) return false;
                    session.AddPlanned(value, transitionGraph, transition.GUID, "delete transition");
                    return true;
                case AgentEnsureGameplayTagMutation value:
                    if (!ValidateTag(session, value.Tag, value.ParentTag, value.Path)) return false;
                    session.PlanGameplayTag(value.Tag.Value);
                    session.AddPlanned(value, null, value.Tag.Value, "ensure gameplay tag");
                    return true;
                case AgentSetActionProfileGrantedTagsMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile grantedProfile) || !ValidateTags(session, value.Tags, value.Path)) return false;
                    session.AddPlanned(value, null, grantedProfile.ActionId, "set granted tags");
                    return true;
                case AgentSetActionProfileCancelQueryMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile cancelProfile) ||
                        !ValidateTags(session, value.All.Concat(value.Any).Concat(value.None).ToList(), value.Path)) return false;
                    session.AddPlanned(value, null, cancelProfile.ActionId, "set cancel query");
                    return true;
                case AgentSetActionProfileTargetRequirementMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile targetProfile)) return false;
                    session.AddPlanned(value, null, targetProfile.ActionId, $"set target requirement {value.TargetRequirement}");
                    return true;
                case AgentSetActionRequestTimingClassMutation value:
                    if (!TryResolveActionRequest(session, value.RequestId, value.Path, out _)) return false;
                    session.AddPlanned(value, null, value.RequestId, $"set request timing {value.TimingClass}");
                    return true;
                default:
                    throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureBlackboardDeclarationMutation value: ApplyEnsureDeclaration(session, value); break;
                case AgentMoveBlackboardDeclarationMutation value: ApplyMoveDeclaration(session, value); break;
                case AgentDeleteBlackboardDeclarationMutation value: ApplyDeleteDeclaration(session, value); break;
                case AgentEnsureBlackboardWriteMutation value: ApplyEnsureBlackboardWrite(session, value); break;
                case AgentEnsureTimelineTreeClipMutation value: ApplyEnsureTreeClip(session, value); break;
                case AgentEnsureInlineTimelineMutation value: ApplyEnsureInlineTimeline(session, value); break;
                case AgentEnsureMotionCurveTrackMutation value: ApplyEnsureMotionCurveTrack(session, value); break;
                case AgentEnsureMotionCurveClipMutation value: ApplyEnsureMotionCurveClip(session, value); break;
                case AgentConfigureMotionCurveClipMutation value: ApplyConfigureMotionCurveClip(session, value); break;
                case AgentEnsureMotionWarpTrackMutation value: ApplyEnsureMotionWarpTrack(session, value); break;
                case AgentDeleteTimelineTrackMutation value: ApplyDeleteTimelineTrack(session, value); break;
                case AgentEnsureMotionWarpClipMutation value: ApplyEnsureMotionWarpClip(session, value); break;
                case AgentConfigureMotionWarpSourceMutation value: ApplyConfigureMotionWarpSource(session, value); break;
                case AgentConfigureMotionWarpParametersMutation value: ApplyConfigureMotionWarpParameters(session, value); break;
                case AgentMoveTimelineClipMutation value: ApplyMoveTimelineClip(session, value); break;
                case AgentConfigureTimelineClipEaseMutation value: ApplyConfigureTimelineClipEase(session, value); break;
                case AgentConfigureTimelineCurveChannelMutation value: ApplyConfigureTimelineCurveChannel(session, value); break;
                case AgentConfigureAnimationTrackChannelMutation value: ApplyConfigureAnimationTrackChannel(session, value); break;
                case AgentConfigureAnimationTrackMarkerSyncMutation value: ApplyConfigureAnimationTrackMarkerSync(session, value); break;
                case AgentEnsureAnimationSyncMarkerMutation value: ApplyEnsureAnimationSyncMarker(session, value); break;
                case AgentMoveAnimationSyncMarkerMutation value: ApplyMoveAnimationSyncMarker(session, value); break;
                case AgentDeleteAnimationSyncMarkerMutation value: ApplyDeleteAnimationSyncMarker(session, value); break;
                case AgentDeleteTimelineClipMutation value: ApplyDeleteTimelineClip(session, value); break;
                case AgentEnsureTreeClipBlackboardWriteMutation value: ApplyEnsureTreeClipWrite(session, value); break;
                case AgentDeleteTransitionMutation value: ApplyDeleteTransition(session, value); break;
                case AgentEnsureGameplayTagMutation value: ApplyEnsureGameplayTag(session, value); break;
                case AgentSetActionProfileGrantedTagsMutation value: ApplyGrantedTags(session, value); break;
                case AgentSetActionProfileCancelQueryMutation value: ApplyCancelQuery(session, value); break;
                case AgentSetActionProfileTargetRequirementMutation value: ApplyTargetRequirement(session, value); break;
                case AgentSetActionRequestTimingClassMutation value: ApplyRequestTimingClass(session, value); break;
                default: throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        static void ApplyEnsureDeclaration(AgentMutationSession session, AgentEnsureBlackboardDeclarationMutation command)
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
            declaration.SetValue(command.DefaultValue);
            declaration.ConfigurePipelineBlackboard(command.Key, command.Scope, command.Lifetime, command.Authority, command.SyncPolicy, command.InputValueId, command.CategoryPath);
            declaration.ConfigureFactProjection(command.FactProjection, command.WindowType, command.WindowId, command.Digest);
            graph.CheckInit();
            session.AddAppliedAuthoring(command, graph.SerializedOwner, declaration, command.Key, "ensure declaration");
        }

        static bool PreflightMoveDeclaration(AgentMutationSession session, AgentMoveBlackboardDeclarationMutation command)
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

        static void ApplyMoveDeclaration(AgentMutationSession session, AgentMoveBlackboardDeclarationMutation command)
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
            AgentMutationSession session,
            AgentEnsureBlackboardDeclarationMutation command,
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
            AgentMutationSession session,
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

        static void ApplyDeleteDeclaration(AgentMutationSession session, AgentDeleteBlackboardDeclarationMutation command)
        {
            if (!TryResolveGraphDeclaration(session, command.Graph, command.DeclarationAuthoringId, command.Path, out BaseTree graph, out BaseExposedProperty declaration))
                return;
            graph.DeleteExposedProperty(declaration);
            graph.CheckInit();
            session.AddAppliedAuthoring(command, graph.SerializedOwner, null, declaration.BlackboardKey, "delete declaration");
        }

        static bool PreflightBlackboardWrite(AgentMutationSession session, AgentEnsureBlackboardWriteMutation command)
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

        static void ApplyEnsureBlackboardWrite(AgentMutationSession session, AgentEnsureBlackboardWriteMutation command)
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

        static void ApplyEnsureTreeClip(AgentMutationSession session, AgentEnsureTimelineTreeClipMutation command)
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

        static void ApplyDeleteTimelineClip(AgentMutationSession session, AgentDeleteTimelineClipMutation command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            timeline.RemoveClip(clip);
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, command.Target.ClipAuthoringId, "delete clip");
        }

        static void ApplyDeleteTimelineTrack(AgentMutationSession session, AgentDeleteTimelineTrackMutation command)
        {
            if (!TryResolveTimelineTrack(session, command.TimelineAuthoringId, command.TrackAuthoringId, command.Path, out TimelineData timeline, out Track track))
                return;
            timeline.RemoveTrack(track);
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, command.TrackAuthoringId, "delete track");
        }

        static void ApplyMoveTimelineClip(AgentMutationSession session, AgentMoveTimelineClipMutation command)
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

        static void ApplyConfigureTimelineClipEase(AgentMutationSession session, AgentConfigureTimelineClipEaseMutation command)
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
            AgentMutationSession session,
            AgentConfigureTimelineCurveChannelMutation command)
        {
            if (!TryResolveTimelineClip(session, command.Target, command.Path, out TimelineData timeline, out Clip clip))
                return;
            TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.Require(command.ChannelId.Value);
            descriptor.Replace(clip, command.Curve);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, $"configure Timeline curve {command.ChannelId}");
        }

        static void ApplyConfigureAnimationTrackMarkerSync(
            AgentMutationSession session,
            AgentConfigureAnimationTrackMarkerSyncMutation command)
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

        static void ApplyConfigureAnimationTrackChannel(
            AgentMutationSession session,
            AgentConfigureAnimationTrackChannelMutation command)
        {
            if (!TryResolveAnimationTrack(session, command.Target, command.Path, out TimelineData timeline, out AnimationTrack track))
                return;
            track.SetAnimationChannelId(command.AnimationChannelId);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, track, track.AuthoringId, $"configure animation channel {command.AnimationChannelId}");
        }

        static void ApplyEnsureAnimationSyncMarker(
            AgentMutationSession session,
            AgentEnsureAnimationSyncMarkerMutation command)
        {
            if (!TryResolveAnimationTrack(session, command.Target, command.Path, out TimelineData timeline, out AnimationTrack track))
                return;
            AnimationSyncMarker marker = string.IsNullOrEmpty(command.MarkerAuthoringId)
                ? track.AddMarker(command.MarkerId, command.Frame)
                : track.EnsureMarker(command.MarkerAuthoringId, command.MarkerId, command.Frame);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, marker, marker.AuthoringId, "ensure animation marker");
        }

        static void ApplyMoveAnimationSyncMarker(
            AgentMutationSession session,
            AgentMoveAnimationSyncMarkerMutation command)
        {
            if (!TryResolveAnimationMarker(session, command.Target, command.Marker, command.Path, out TimelineData timeline, out AnimationTrack track, out AnimationSyncMarker marker))
                return;
            track.MoveMarker(marker.AuthoringId, command.Frame);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, marker, marker.AuthoringId, "move animation marker");
        }

        static void ApplyDeleteAnimationSyncMarker(
            AgentMutationSession session,
            AgentDeleteAnimationSyncMarkerMutation command)
        {
            if (!TryResolveAnimationMarker(session, command.Target, command.Marker, command.Path, out TimelineData timeline, out AnimationTrack track, out AnimationSyncMarker marker))
                return;
            string markerAuthoringId = marker.AuthoringId;
            track.DeleteMarker(markerAuthoringId);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, markerAuthoringId, "delete animation marker");
        }

        static void ApplyEnsureInlineTimeline(AgentMutationSession session, AgentEnsureInlineTimelineMutation command)
        {
            if (!TryResolveInlineTimelineNode(session, command, out TimelineNode node))
                return;
            TimelineData timeline = node.InlineTimeline;
            if (timeline == null)
            {
                session.Report.Error(command.Path, "inline_timeline_missing", "TimelineNode 未建立正式 Inline Timeline。");
                return;
            }
            timeline.Name = command.DisplayName;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, timeline, timeline.AuthoringId, "ensure inline Timeline");
        }

        static void ApplyEnsureMotionCurveTrack(AgentMutationSession session, AgentEnsureMotionCurveTrackMutation command)
        {
            if (!TryResolveTimeline(session, command.Target, command.Path, out TimelineData timeline))
                return;
            MotionCurveTrack track = string.IsNullOrEmpty(command.Target.TrackAuthoringId)
                ? null
                : timeline.Tracks.OfType<MotionCurveTrack>().SingleOrDefault(value => value.AuthoringId == command.Target.TrackAuthoringId);
            if (track == null)
            {
                timeline.AddTrack(typeof(MotionCurveTrack));
                track = timeline.Tracks.OfType<MotionCurveTrack>().Last();
            }
            track.Name = command.DisplayName;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, track, track.AuthoringId, "ensure MotionCurve track");
        }

        static void ApplyEnsureMotionCurveClip(AgentMutationSession session, AgentEnsureMotionCurveClipMutation command)
        {
            if (!TryResolveMotionCurveTrack(session, command.Target, command.Path, out TimelineData timeline, out MotionCurveTrack track))
                return;
            MotionCurveClip clip = string.IsNullOrEmpty(command.Target.ClipAuthoringId)
                ? null
                : track.Clips.OfType<MotionCurveClip>().SingleOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            clip ??= timeline.AddClip(track, command.StartFrame) as MotionCurveClip;
            clip.StartFrame = command.StartFrame;
            clip.EndFrame = command.EndFrame;
            clip.CurveEndFrame = command.EndFrame;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "ensure MotionCurve clip");
        }

        static void ApplyConfigureMotionCurveClip(AgentMutationSession session, AgentConfigureMotionCurveClipMutation command)
        {
            if (!TryResolveMotionCurveClip(session, command.Target, command.Path, out TimelineData timeline, out MotionCurveClip clip))
                return;
            clip.CurveId = command.CurveId;
            clip.CurveEndFrame = command.CurveEndFrame;
            clip.Space = command.Space;
            clip.Channel = command.Channel;
            clip.BlendMode = command.BlendMode;
            clip.Priority = command.Priority;
            clip.ConsumeLowerChannels = command.ConsumeLowerChannels;
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, clip, clip.AuthoringId, "configure MotionCurve clip");
        }

        static void ApplyEnsureMotionWarpTrack(AgentMutationSession session, AgentEnsureMotionWarpTrackMutation command)
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

        static void ApplyEnsureMotionWarpClip(AgentMutationSession session, AgentEnsureMotionWarpClipMutation command)
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

        static void ApplyConfigureMotionWarpSource(AgentMutationSession session, AgentConfigureMotionWarpSourceMutation command)
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

        static void ApplyConfigureMotionWarpParameters(AgentMutationSession session, AgentConfigureMotionWarpParametersMutation command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out MotionWarpClip warp))
                return;
            warp.ConfigureAuthoring(
                command.TranslationMode,
                command.TargetOffsetSpace,
                command.RotationMode,
                command.RotationMethod,
                command.TargetPlanarOffset,
                command.TargetYawOffsetDegrees,
                command.MaxTotalPositionCorrection,
                command.MaxTotalYawCorrectionDegrees,
                command.MaximumYawRateDegreesPerSecond,
                command.LimitPolicy,
                command.PositionProgressCurve,
                command.YawProgressCurve);
            timeline.Init();
            session.AddAppliedAuthoring(command, timeline.SerializedOwner, null, warp.AuthoringId, "configure MotionWarp parameters");
        }

        static void ApplyEnsureTreeClipWrite(AgentMutationSession session, AgentEnsureTreeClipBlackboardWriteMutation command)
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
            if (command.Target.ClipPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(command.Target.ClipPlannedIdentity, command.Path, out clip) ||
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

        static void ApplyDeleteTransition(AgentMutationSession session, AgentDeleteTransitionMutation command)
        {
            if (!TryResolveTransition(session, command, out StateMachineGraph graph, out BaseEdge edge))
                return;
            graph.UnLink(edge);
            session.AddAppliedAuthoring(command, graph.SerializedOwner, null, edge.GUID, "delete transition");
        }

        static void ApplyEnsureGameplayTag(AgentMutationSession session, AgentEnsureGameplayTagMutation command)
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

        static void ApplyGrantedTags(AgentMutationSession session, AgentSetActionProfileGrantedTagsMutation command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            WriteTagArray(serialized.FindProperty("m_Tags"), command.Tags);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, "set granted tags");
        }

        static void ApplyCancelQuery(AgentMutationSession session, AgentSetActionProfileCancelQueryMutation command)
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
            AgentMutationSession session,
            AgentSetActionProfileTargetRequirementMutation command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("m_TargetRequirement").enumValueIndex = (int)command.TargetRequirement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, $"set target requirement {command.TargetRequirement}");
        }

        static void ApplyRequestTimingClass(
            AgentMutationSession session,
            AgentSetActionRequestTimingClassMutation command)
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
            AgentMutationSession session,
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

        static bool TryResolveGraphDeclaration(AgentMutationSession session, AgentGraphTargetReference graphReference, string declarationId, string path, out BaseTree graph, out BaseExposedProperty declaration)
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
            AgentMutationSession session,
            AgentAuthoringReference reference,
            string path,
            out BaseExposedProperty declaration)
        {
            declaration = null;
            if (reference.PlannedIdentity.IsValid)
                return session.TryResolvePlannedIdentity(reference.PlannedIdentity, path, out declaration);
            if (TryResolveDeclaration(session.RootTree, reference.AuthoringId, out declaration))
                return true;
            session.Report.Error(path, "blackboard_declaration_missing", $"Blackboard declaration 无法解析：{reference.Identity}");
            return false;
        }

        static bool TryResolveVisibleBoolDeclaration(
            AgentMutationSession session,
            BaseTree graph,
            AgentAuthoringReference declarationReference,
            string path,
            out BaseExposedProperty declaration)
        {
            declaration = null;
            if (declarationReference.PlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(declarationReference.PlannedIdentity, path, out declaration))
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

        static bool TryResolveTimeline(AgentMutationSession session, string timelineId, string path, out TimelineData timeline)
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

        static bool TryResolveTimeline(
            AgentMutationSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline)
        {
            if (target.TimelinePlannedIdentity.IsValid)
                return session.TryResolvePlannedIdentity(target.TimelinePlannedIdentity, path, out timeline);
            return TryResolveTimeline(session, target.TimelineAuthoringId, path, out timeline);
        }

        static bool TryResolveTimelineClip(AgentMutationSession session, AgentTimelineTargetReference target, string path, out TimelineData timeline, out Clip clip)
        {
            timeline = null;
            clip = null;
            if (target.ClipPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(target.ClipPlannedIdentity, path, out clip))
                    return false;
                if (!session.IsApply)
                    return true;
                timeline = clip?.Timeline;
                if (timeline != null)
                    return true;
                session.Report.Error(path, "timeline_clip_owner_missing", $"Timeline Clip 缺少 Timeline owner：{target.ClipPlannedIdentity.Value}");
                return false;
            }
            if (!TryResolveTimeline(session, target, path, out timeline))
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
            AgentMutationSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out AnimationTrack track)
        {
            timeline = null;
            track = null;
            if (target.TrackPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(target.TrackPlannedIdentity, path, out Track outputTrack))
                    return false;
                if (!session.IsApply)
                    return true;
                track = outputTrack as AnimationTrack;
                timeline = outputTrack?.Timeline;
            }
            else
            {
                if (!TryResolveTimeline(session, target, path, out timeline))
                    return false;
                track = timeline.Tracks.OfType<AnimationTrack>().SingleOrDefault(value =>
                    string.Equals(value.AuthoringId, target.TrackAuthoringId, StringComparison.Ordinal));
            }
            if (track != null)
                return true;
            session.Report.Error(path, "animation_track_not_found", $"AnimationTrack identity 无法解析：{target.TrackAuthoringId}");
            return false;
        }

        static bool TryResolveAnimationMarker(
            AgentMutationSession session,
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
            if (markerReference.PlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(markerReference.PlannedIdentity, path, out marker))
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
            AgentMutationSession session,
            AgentConfigureTimelineClipEaseMutation command,
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

        static bool TryResolveTransition(AgentMutationSession session, AgentDeleteTransitionMutation command, out StateMachineGraph graph, out BaseEdge edge)
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

        static bool TryResolveProfile(AgentMutationSession session, AgentAssetReference reference, string path, out ActionProfile profile)
        {
            if (session.Resolver.TryResolveActionProfile(reference.LogicalId, out profile))
                return true;
            session.Report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{reference.LogicalId}");
            return false;
        }

        static bool ValidateTag(AgentMutationSession session, GameplayTagId tag, GameplayTagId parent, string path)
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

        static bool ValidateTags(AgentMutationSession session, IReadOnlyList<GameplayTagId> tags, string path)
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

        static bool ValidateInlineTimelineTarget(AgentMutationSession session, AgentEnsureInlineTimelineMutation command)
        {
            if (!command.TimelineNode.Value.PlannedIdentity.IsValid)
            {
                session.Report.Error(command.Path, "inline_timeline_node_requires_planned_identity", "新增 Inline Timeline 必须引用同一事务中更早创建的 TimelineNode。");
                return false;
            }
            if (!TryResolveInlineTimelineNode(session, command, out TimelineNode node))
                return false;
            if (node != null && node.TimelineOwnership != TimelineOwnership.Inline)
            {
                session.Report.Error(command.Path, "inline_timeline_ownership_invalid", "目标 TimelineNode 不是 Inline ownership。");
                return false;
            }
            return true;
        }

        static bool TryResolveInlineTimelineNode(
            AgentMutationSession session,
            AgentEnsureInlineTimelineMutation command,
            out TimelineNode node)
        {
            node = null;
            if (!session.TryResolvePlannedIdentity(command.TimelineNode.Value.PlannedIdentity, command.Path, out BaseNode planned))
                return false;
            if (!session.IsApply)
                return true;
            node = planned as TimelineNode;
            if (node != null)
                return true;
            session.Report.Error(command.Path, "inline_timeline_node_type_invalid", "计划内节点不是 TimelineNode。");
            return false;
        }

        static bool ValidateMotionCurveTrackTarget(AgentMutationSession session, AgentEnsureMotionCurveTrackMutation command)
        {
            if (!TryResolveTimeline(session, command.Target, command.Path, out TimelineData timeline))
                return false;
            if (timeline == null || string.IsNullOrEmpty(command.Target.TrackAuthoringId))
                return true;
            Track existing = timeline.Tracks.SingleOrDefault(value => value.AuthoringId == command.Target.TrackAuthoringId);
            if (existing is MotionCurveTrack)
                return true;
            session.Report.Error(command.Path, "motion_curve_track_not_found", $"MotionCurveTrack identity 无法解析：{command.Target.TrackAuthoringId}");
            return false;
        }

        static bool ValidateMotionCurveClipTarget(AgentMutationSession session, AgentEnsureMotionCurveClipMutation command)
        {
            if (command.StartFrame >= command.EndFrame)
            {
                session.Report.Error(command.Path, "motion_curve_window_invalid", "MotionCurveClip 必须满足 StartFrame < EndFrame。");
                return false;
            }
            if (!TryResolveMotionCurveTrack(session, command.Target, command.Path, out _, out MotionCurveTrack track))
                return false;
            if (track == null || string.IsNullOrEmpty(command.Target.ClipAuthoringId))
                return true;
            Clip existing = track.Clips.SingleOrDefault(value => value.AuthoringId == command.Target.ClipAuthoringId);
            if (existing is MotionCurveClip)
                return true;
            session.Report.Error(command.Path, "motion_curve_clip_not_found", $"MotionCurveClip identity 无法解析：{command.Target.ClipAuthoringId}");
            return false;
        }

        static bool ValidateMotionCurveClipConfiguration(AgentMutationSession session, AgentConfigureMotionCurveClipMutation command)
        {
            if (!TryResolveMotionCurveClip(session, command.Target, command.Path, out _, out MotionCurveClip clip))
                return false;
            if (clip == null)
                return true;
            if (command.CurveEndFrame <= clip.StartFrame || command.CurveEndFrame > clip.EndFrame)
            {
                session.Report.Error(command.Path, "motion_curve_end_frame_invalid", "MotionCurveClip 必须满足 StartFrame < CurveEndFrame <= EndFrame。");
                return false;
            }
            return true;
        }

        static bool ValidateMotionWarpTrackTarget(AgentMutationSession session, AgentEnsureMotionWarpTrackMutation command)
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

        static bool ValidateMotionWarpClipTarget(AgentMutationSession session, AgentEnsureMotionWarpClipMutation command)
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

        static bool ValidateMotionWarpSource(AgentMutationSession session, AgentConfigureMotionWarpSourceMutation command)
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

        static bool ValidateMotionWarpParameters(AgentMutationSession session, AgentConfigureMotionWarpParametersMutation command)
        {
            if (!TryResolveMotionWarpClip(session, command.Target, command.Path, out TimelineData timeline, out _) ||
                !ValidateMotionWarpActionRequirement(session, timeline, command.Path))
                return false;
            var issues = new List<MotionWarpAuthoringIssue>();
            bool valid = MotionWarpAuthoring.ValidateConfiguration(
                command.TranslationMode,
                command.TargetOffsetSpace,
                command.RotationMode,
                command.RotationMethod,
                command.TargetPlanarOffset,
                command.TargetYawOffsetDegrees,
                command.MaxTotalPositionCorrection,
                command.MaxTotalYawCorrectionDegrees,
                command.MaximumYawRateDegreesPerSecond,
                command.LimitPolicy,
                command.PositionProgressCurve,
                command.YawProgressCurve,
                issues);
            for (int i = 0; i < issues.Count; i++)
                session.Report.Error(command.Path, issues[i].Code, issues[i].Message);
            return valid;
        }

        static bool TryResolveMotionWarpTrack(
            AgentMutationSession session,
            TimelineData timeline,
            AgentTimelineTargetReference target,
            string path,
            out MotionWarpTrack track)
        {
            track = null;
            if (target.TrackPlannedIdentity.IsValid)
                return session.TryResolvePlannedIdentity(target.TrackPlannedIdentity, path, out track);
            track = timeline.Tracks.OfType<MotionWarpTrack>().SingleOrDefault(value => value.AuthoringId == target.TrackAuthoringId);
            if (track != null)
                return true;
            session.Report.Error(path, "motion_warp_track_not_found", $"MotionWarpTrack identity 无法解析：{target.TrackAuthoringId}");
            return false;
        }

        static bool TryResolveMotionCurveTrack(
            AgentMutationSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out MotionCurveTrack track)
        {
            timeline = null;
            track = null;
            if (target.TrackPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(target.TrackPlannedIdentity, path, out track))
                    return false;
                timeline = track?.Timeline;
                return !session.IsApply || timeline != null;
            }
            if (!TryResolveTimeline(session, target, path, out timeline))
                return false;
            track = timeline.Tracks.OfType<MotionCurveTrack>().SingleOrDefault(value =>
                value.AuthoringId == target.TrackAuthoringId);
            if (track != null)
                return true;
            session.Report.Error(path, "motion_curve_track_not_found", $"MotionCurveTrack identity 无法解析：{target.TrackAuthoringId}");
            return false;
        }

        static bool TryResolveMotionCurveClip(
            AgentMutationSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out MotionCurveClip clip)
        {
            timeline = null;
            clip = null;
            if (target.ClipPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(target.ClipPlannedIdentity, path, out clip))
                    return false;
                timeline = clip?.Timeline;
                return !session.IsApply || timeline != null;
            }
            if (!TryResolveTimeline(session, target, path, out timeline))
                return false;
            clip = timeline.Tracks.SelectMany(value => value.Clips).OfType<MotionCurveClip>().SingleOrDefault(value =>
                value.AuthoringId == target.ClipAuthoringId);
            if (clip != null)
                return true;
            session.Report.Error(path, "motion_curve_clip_not_found", $"MotionCurveClip identity 无法解析：{target.ClipAuthoringId}");
            return false;
        }

        static bool TryResolveTimelineTrack(
            AgentMutationSession session,
            string timelineAuthoringId,
            string trackAuthoringId,
            string path,
            out TimelineData timeline,
            out Track track)
        {
            track = null;
            if (!TryResolveTimeline(session, timelineAuthoringId, path, out timeline))
                return false;
            track = timeline.Tracks.SingleOrDefault(value => value.AuthoringId == trackAuthoringId);
            if (track != null)
                return true;
            session.Report.Error(path, "timeline_track_not_found", $"Timeline Track identity 无法解析：{trackAuthoringId}");
            return false;
        }

        static bool TryResolveMotionWarpClip(
            AgentMutationSession session,
            AgentTimelineTargetReference target,
            string path,
            out TimelineData timeline,
            out MotionWarpClip clip)
        {
            timeline = null;
            clip = null;
            if (target.ClipPlannedIdentity.IsValid)
            {
                if (!session.TryResolvePlannedIdentity(target.ClipPlannedIdentity, path, out clip))
                    return false;
                timeline = clip?.Timeline;
                return !session.IsApply || timeline != null;
            }
            if (!TryResolveTimeline(session, target, path, out timeline))
                return false;
            clip = timeline.Tracks.SelectMany(value => value.Clips).OfType<MotionWarpClip>().SingleOrDefault(value => value.AuthoringId == target.ClipAuthoringId);
            if (clip != null)
                return true;
            session.Report.Error(path, "motion_warp_clip_not_found", $"MotionWarpClip identity 无法解析：{target.ClipAuthoringId}");
            return false;
        }

        static bool ValidateMotionWarpActionRequirement(AgentMutationSession session, TimelineData timeline, string path)
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

        static bool ValidateTimelineTreeClipTarget(AgentMutationSession session, AgentEnsureTimelineTreeClipMutation command)
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

        static bool ValidateTreeClipWriteTarget(AgentMutationSession session, AgentEnsureTreeClipBlackboardWriteMutation command)
        {
            if (!TryResolveDeclaration(session, command.Declaration, command.Path, out BaseExposedProperty declaration))
                return false;
            if (declaration != null && declaration.ValueType != typeof(bool))
            {
                session.Report.Error(command.Path, "blackboard_write_type_invalid", $"TreeClip write 只接受 Bool declaration，实际为 {declaration.ValueType?.Name ?? "Unknown"}。");
                return false;
            }
            if (command.Target.ClipPlannedIdentity.IsValid)
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
