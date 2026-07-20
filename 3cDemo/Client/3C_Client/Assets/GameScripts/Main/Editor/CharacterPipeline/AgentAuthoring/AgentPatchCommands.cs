using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public enum AgentPatchCommandKind
    {
        EnsureStateMachine,
        EnsureState,
        DeleteState,
        EnsureTransition,
        EnsureConditionRule,
        EnsureActionExitLifecycle,
        DeleteStateBehaviorNode,
        EnsureStateBehaviorNode,
        EnsureTimelineNode,
        EnsureActionActivation,
        EnsureActionLifecycleTransition,
        EnsureInputNode,
        EnsureBlackboardDeclaration,
        MoveBlackboardDeclaration,
        DeleteBlackboardDeclaration,
        EnsureBlackboardWrite,
        EnsureTimelineTreeClip,
        EnsureMotionWarpTrack,
        EnsureMotionWarpClip,
        ConfigureMotionWarpSource,
        ConfigureMotionWarpParameters,
        MoveTimelineClip,
        ConfigureTimelineClipEase,
        ConfigureTimelineCurveChannel,
        ConfigureAnimationTrackMarkerSync,
        EnsureAnimationSyncMarker,
        MoveAnimationSyncMarker,
        DeleteAnimationSyncMarker,
        DeleteTimelineClip,
        EnsureTreeClipBlackboardWrite,
        DeleteTransition,
        EnsureGameplayTag,
        SetActionProfileGrantedTags,
        SetActionProfileCancelQuery,
        SetActionProfileTargetRequirement,
        SetActionRequestTimingClass,
        DeleteFlowEdge,
        LinkFlow,
        LinkProperty
    }

    public enum AgentPatchOutputKind
    {
        None,
        StateMachine,
        State,
        Transition,
        Node,
        BlackboardDeclaration,
        TimelineTrack,
        TimelineClip,
        TimelineMarker,
        FlowEdge,
        PropertyEdge
    }

    public enum AgentConditionTermKind
    {
        MoveStop,
        MoveHas,
        MoveRun,
        MoveWalk,
        TurnFacingAngle,
        BlackboardBool,
        StateRootCompleted,
        ActionRequest,
        ActionWindowActive,
        CanActivateAction
    }

    public readonly struct AgentOperationOutputReference
    {
        public AgentOperationOutputReference(string operationId, string role)
        {
            OperationId = operationId ?? string.Empty;
            Role = role ?? string.Empty;
        }

        public string OperationId { get; }
        public string Role { get; }
        public bool IsValid => !string.IsNullOrEmpty(OperationId);
        public string Value => string.IsNullOrEmpty(Role) ? OperationId : $"{OperationId}#{Role}";

        public static AgentOperationOutputReference Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;
            int separator = value.LastIndexOf('#');
            return separator < 0
                ? new AgentOperationOutputReference(value, string.Empty)
                : new AgentOperationOutputReference(value.Substring(0, separator), value.Substring(separator + 1));
        }
    }

    public readonly struct AgentAuthoringReference
    {
        public AgentAuthoringReference(string authoringId, AgentOperationOutputReference operationOutput)
        {
            AuthoringId = authoringId ?? string.Empty;
            OperationOutput = operationOutput;
        }

        public string AuthoringId { get; }
        public AgentOperationOutputReference OperationOutput { get; }
        public bool IsValid => !string.IsNullOrEmpty(AuthoringId) || OperationOutput.IsValid;
        public string Identity => !string.IsNullOrEmpty(AuthoringId) ? AuthoringId : OperationOutput.Value;
    }

    public readonly struct AgentGraphTargetReference
    {
        public AgentGraphTargetReference(AgentAuthoringReference value) => Value = value;
        public AgentAuthoringReference Value { get; }
        public bool IsValid => Value.IsValid;
        public string Identity => Value.Identity;
    }

    public readonly struct AgentStateMachineTargetReference
    {
        public AgentStateMachineTargetReference(AgentAuthoringReference value) => Value = value;
        public AgentAuthoringReference Value { get; }
        public bool IsValid => Value.IsValid;
        public string Identity => Value.Identity;
    }

    public readonly struct AgentStateTargetReference
    {
        public AgentStateTargetReference(AgentAuthoringReference value) => Value = value;
        public AgentAuthoringReference Value { get; }
        public bool IsValid => Value.IsValid;
        public string Identity => Value.Identity;
    }

    public readonly struct AgentElementTargetReference
    {
        public AgentElementTargetReference(AgentAuthoringReference value) => Value = value;
        public AgentAuthoringReference Value { get; }
        public bool IsValid => Value.IsValid;
        public string Identity => Value.Identity;
    }

    public readonly struct AgentTimelineTargetReference
    {
        public AgentTimelineTargetReference(string timelineAuthoringId, string trackAuthoringId, string clipAuthoringId)
            : this(timelineAuthoringId, trackAuthoringId, default, clipAuthoringId, default) { }

        public AgentTimelineTargetReference(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            AgentOperationOutputReference clipOperationOutput)
            : this(timelineAuthoringId, trackAuthoringId, default, clipAuthoringId, clipOperationOutput) { }

        public AgentTimelineTargetReference(
            string timelineAuthoringId,
            string trackAuthoringId,
            AgentOperationOutputReference trackOperationOutput,
            string clipAuthoringId,
            AgentOperationOutputReference clipOperationOutput)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            TrackOperationOutput = trackOperationOutput;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            ClipOperationOutput = clipOperationOutput;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public AgentOperationOutputReference TrackOperationOutput { get; }
        public string ClipAuthoringId { get; }
        public AgentOperationOutputReference ClipOperationOutput { get; }
    }

    public readonly struct AgentAssetReference
    {
        public AgentAssetReference(string logicalId, string assetPath, string assetGuid)
        {
            LogicalId = logicalId ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            AssetGuid = assetGuid ?? string.Empty;
        }

        public string LogicalId { get; }
        public string AssetPath { get; }
        public string AssetGuid { get; }
        public bool HasExplicitAsset => !string.IsNullOrEmpty(AssetPath) || !string.IsNullOrEmpty(AssetGuid);
    }

    public readonly struct AgentStateBehaviorTargetReference
    {
        public AgentStateBehaviorTargetReference(
            AgentGraphTargetReference directGraph,
            AgentStateMachineTargetReference stateMachine,
            AgentStateTargetReference state)
        {
            DirectGraph = directGraph;
            StateMachine = stateMachine;
            State = state;
        }

        public AgentGraphTargetReference DirectGraph { get; }
        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentStateTargetReference State { get; }
        public bool IsDirect => DirectGraph.IsValid;
        public bool IsValid => IsDirect || StateMachine.IsValid && State.IsValid;
        public string OwnerScope => IsDirect ? DirectGraph.Identity : $"{StateMachine.Identity}/{State.Identity}";
    }

    public sealed class AgentConditionTermCommand
    {
        public AgentConditionTermCommand(
            AgentConditionTermKind kind,
            string blackboardKey,
            bool negate,
            string request,
            string windowType,
            AgentAssetReference actionProfile,
            string targetSnapshotBlackboardKey)
        {
            Kind = kind;
            BlackboardKey = blackboardKey ?? string.Empty;
            Negate = negate;
            Request = request ?? string.Empty;
            WindowType = windowType ?? string.Empty;
            ActionProfile = actionProfile;
            TargetSnapshotBlackboardKey = targetSnapshotBlackboardKey ?? string.Empty;
        }

        public AgentConditionTermKind Kind { get; }
        public string BlackboardKey { get; }
        public bool Negate { get; }
        public string Request { get; }
        public string WindowType { get; }
        public AgentAssetReference ActionProfile { get; }
        public string TargetSnapshotBlackboardKey { get; }
    }

    public sealed class AgentConditionGroupCommand
    {
        readonly ReadOnlyCollection<AgentConditionTermCommand> m_Terms;

        public AgentConditionGroupCommand(IList<AgentConditionTermCommand> terms)
        {
            m_Terms = new ReadOnlyCollection<AgentConditionTermCommand>(new List<AgentConditionTermCommand>(terms));
        }

        public IReadOnlyList<AgentConditionTermCommand> Terms => m_Terms;
    }

    public abstract class AgentPatchCommand
    {
        protected AgentPatchCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            AgentPatchOutputKind outputKind,
            string path,
            string ownerScope,
            Vector2 position)
        {
            Id = id;
            Kind = kind;
            OperationName = operationName;
            OutputKind = outputKind;
            Path = path;
            OwnerScope = ownerScope ?? string.Empty;
            Position = position;
        }

        public string Id { get; }
        public AgentPatchCommandKind Kind { get; }
        public string OperationName { get; }
        public AgentPatchOutputKind OutputKind { get; }
        public string Path { get; }
        public string OwnerScope { get; }
        public Vector2 Position { get; }
    }

    public sealed class AgentEnsureStateMachineCommand : AgentPatchCommand
    {
        public AgentEnsureStateMachineCommand(
            string id,
            string path,
            AgentGraphTargetReference parentGraph,
            AgentElementTargetReference existingOwner,
            string existingGraphAuthoringId,
            string displayName,
            string lifecycleSlot,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureStateMachine, "ensure_state_machine", AgentPatchOutputKind.StateMachine, path, parentGraph.Identity, position)
        {
            ParentGraph = parentGraph;
            ExistingOwner = existingOwner;
            ExistingGraphAuthoringId = existingGraphAuthoringId ?? string.Empty;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot ?? string.Empty;
        }

        public AgentGraphTargetReference ParentGraph { get; }
        public AgentElementTargetReference ExistingOwner { get; }
        public string ExistingGraphAuthoringId { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
    }

    public sealed class AgentEnsureStateCommand : AgentPatchCommand
    {
        public AgentEnsureStateCommand(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentStateTargetReference existingState,
            string stateName,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureState, "ensure_state", AgentPatchOutputKind.State, path, stateMachine.Identity, position)
        {
            StateMachine = stateMachine;
            ExistingState = existingState;
            StateName = stateName;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentStateTargetReference ExistingState { get; }
        public string StateName { get; }
    }

    public sealed class AgentDeleteStateCommand : AgentPatchCommand
    {
        public AgentDeleteStateCommand(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentStateTargetReference state)
            : base(id, AgentPatchCommandKind.DeleteState, "delete_state", AgentPatchOutputKind.None, path, stateMachine.Identity, Vector2.zero)
        {
            StateMachine = stateMachine;
            State = state;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentStateTargetReference State { get; }
    }

    public class AgentEnsureTransitionCommand : AgentPatchCommand
    {
        public AgentEnsureTransitionCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentElementTargetReference from,
            AgentElementTargetReference to,
            string edgeAuthoringId,
            int priority,
            Vector2 position)
            : base(id, kind, operationName, AgentPatchOutputKind.Transition, path, stateMachine.Identity, position)
        {
            StateMachine = stateMachine;
            From = from;
            To = to;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
            Priority = priority;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentElementTargetReference From { get; }
        public AgentElementTargetReference To { get; }
        public string EdgeAuthoringId { get; }
        public int Priority { get; }
    }

    public sealed class AgentEnsureConditionRuleCommand : AgentEnsureTransitionCommand
    {
        readonly ReadOnlyCollection<AgentConditionGroupCommand> m_Groups;

        public AgentEnsureConditionRuleCommand(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentElementTargetReference from,
            AgentElementTargetReference to,
            string edgeAuthoringId,
            int priority,
            IList<AgentConditionGroupCommand> groups,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureConditionRule, "ensure_condition_rule", path, stateMachine, from, to, edgeAuthoringId, priority, position)
        {
            m_Groups = new ReadOnlyCollection<AgentConditionGroupCommand>(new List<AgentConditionGroupCommand>(groups));
        }

        public IReadOnlyList<AgentConditionGroupCommand> Groups => m_Groups;
    }

    public abstract class AgentStateBehaviorCommand : AgentPatchCommand
    {
        protected AgentStateBehaviorCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            AgentPatchOutputKind outputKind,
            string path,
            AgentStateBehaviorTargetReference target,
            Vector2 position)
            : base(id, kind, operationName, outputKind, path, target.OwnerScope, position)
        {
            Target = target;
        }

        public AgentStateBehaviorTargetReference Target { get; }
    }

    public sealed class AgentEnsureActionExitLifecycleCommand : AgentStateBehaviorCommand
    {
        readonly ReadOnlyCollection<AgentConditionGroupCommand> m_CancelConditionGroups;

        public AgentEnsureActionExitLifecycleCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference source,
            AgentElementTargetReference existingElement,
            AgentAssetReference actionContext,
            string cancelReason,
            string interruptReason,
            string abortReason,
            string completeReason,
            IList<AgentConditionGroupCommand> cancelConditionGroups,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureActionExitLifecycle, "ensure_action_exit_lifecycle", AgentPatchOutputKind.Node, path, target, position)
        {
            Source = source;
            ExistingElement = existingElement;
            ActionContext = actionContext;
            CancelReason = cancelReason;
            InterruptReason = interruptReason;
            AbortReason = abortReason;
            CompleteReason = completeReason;
            m_CancelConditionGroups = new ReadOnlyCollection<AgentConditionGroupCommand>(new List<AgentConditionGroupCommand>(cancelConditionGroups));
        }

        public AgentElementTargetReference Source { get; }
        public AgentElementTargetReference ExistingElement { get; }
        public AgentAssetReference ActionContext { get; }
        public string CancelReason { get; }
        public string InterruptReason { get; }
        public string AbortReason { get; }
        public string CompleteReason { get; }
        public IReadOnlyList<AgentConditionGroupCommand> CancelConditionGroups => m_CancelConditionGroups;
    }

    public sealed class AgentDeleteStateBehaviorNodeCommand : AgentStateBehaviorCommand
    {
        public AgentDeleteStateBehaviorNodeCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference element)
            : base(id, AgentPatchCommandKind.DeleteStateBehaviorNode, "delete_state_behavior_node", AgentPatchOutputKind.None, path, target, Vector2.zero)
        {
            Element = element;
        }

        public AgentElementTargetReference Element { get; }
    }

    public sealed class AgentEnsureStateBehaviorNodeCommand : AgentStateBehaviorCommand
    {
        public AgentEnsureStateBehaviorNodeCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference existingElement,
            string nodeType,
            string displayName,
            string lifecycleSlot,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureStateBehaviorNode, "ensure_state_behavior_node", AgentPatchOutputKind.Node, path, target, position)
        {
            ExistingElement = existingElement;
            NodeType = nodeType;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot ?? string.Empty;
        }

        public AgentElementTargetReference ExistingElement { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
    }

    public sealed class AgentEnsureTimelineNodeCommand : AgentStateBehaviorCommand
    {
        public AgentEnsureTimelineNodeCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference existingElement,
            string displayName,
            string lifecycleSlot,
            AgentTimelineOwnership ownership,
            AgentAssetReference timelineAsset,
            AgentAssetReference actionContext,
            AgentTimelineTargetReference timelineTarget,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureTimelineNode, "ensure_timeline_node", AgentPatchOutputKind.Node, path, target, position)
        {
            ExistingElement = existingElement;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot;
            Ownership = ownership;
            TimelineAsset = timelineAsset;
            ActionContext = actionContext;
            TimelineTarget = timelineTarget;
        }

        public AgentElementTargetReference ExistingElement { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
        public AgentTimelineOwnership Ownership { get; }
        public AgentAssetReference TimelineAsset { get; }
        public AgentAssetReference ActionContext { get; }
        public AgentTimelineTargetReference TimelineTarget { get; }
    }

    public sealed class AgentEnsureActionActivationCommand : AgentStateBehaviorCommand
    {
        public AgentEnsureActionActivationCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference existingElement,
            string displayName,
            string lifecycleSlot,
            AgentAssetReference actionProfile,
            AgentAssetReference actionContext,
            string sourceRequestId,
            bool consumeSourceRequest,
            string targetKey,
            string targetSnapshotBlackboardKey,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureActionActivation, "ensure_action_activation", AgentPatchOutputKind.Node, path, target, position)
        {
            ExistingElement = existingElement;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot;
            ActionProfile = actionProfile;
            ActionContext = actionContext;
            SourceRequestId = sourceRequestId;
            ConsumeSourceRequest = consumeSourceRequest;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshotBlackboardKey = targetSnapshotBlackboardKey ?? string.Empty;
        }

        public AgentElementTargetReference ExistingElement { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
        public AgentAssetReference ActionProfile { get; }
        public AgentAssetReference ActionContext { get; }
        public string SourceRequestId { get; }
        public bool ConsumeSourceRequest { get; }
        public string TargetKey { get; }
        public string TargetSnapshotBlackboardKey { get; }
    }

    public sealed class AgentEnsureActionLifecycleTransitionCommand : AgentStateBehaviorCommand
    {
        public AgentEnsureActionLifecycleTransitionCommand(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference existingElement,
            string displayName,
            string lifecycleSlot,
            ActionLifecycleTransitionType transitionType,
            string reason,
            AgentAssetReference actionContext,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureActionLifecycleTransition, "ensure_action_lifecycle_transition", AgentPatchOutputKind.Node, path, target, position)
        {
            ExistingElement = existingElement;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot;
            TransitionType = transitionType;
            Reason = reason ?? string.Empty;
            ActionContext = actionContext;
        }

        public AgentElementTargetReference ExistingElement { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
        public ActionLifecycleTransitionType TransitionType { get; }
        public string Reason { get; }
        public AgentAssetReference ActionContext { get; }
    }

    public sealed class AgentEnsureInputNodeCommand : AgentPatchCommand
    {
        public AgentEnsureInputNodeCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existingElement,
            string nodeType,
            string displayName,
            string inputId,
            string inputValueType,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureInputNode, "ensure_input_node", AgentPatchOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingElement = existingElement;
            NodeType = nodeType ?? string.Empty;
            DisplayName = displayName;
            InputId = inputId;
            InputValueType = inputValueType ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingElement { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public string InputId { get; }
        public string InputValueType { get; }
    }

    public sealed class AgentEnsureBlackboardDeclarationCommand : AgentPatchCommand
    {
        public AgentEnsureBlackboardDeclarationCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string declarationAuthoringId,
            string key,
            Type valueType,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            PipelineBlackboardVariableAuthority authority,
            PipelineBlackboardVariableSyncPolicy syncPolicy,
            string inputValueId,
            PipelineBlackboardFactProjectionKind factProjection,
            string windowType,
            string windowId,
            ulong digest,
            string categoryPath)
            : base(id, AgentPatchCommandKind.EnsureBlackboardDeclaration, "ensure_blackboard_declaration", AgentPatchOutputKind.BlackboardDeclaration, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType;
            Scope = scope;
            Lifetime = lifetime;
            Authority = authority;
            SyncPolicy = syncPolicy;
            InputValueId = inputValueId ?? string.Empty;
            FactProjection = factProjection;
            WindowType = windowType ?? string.Empty;
            WindowId = windowId ?? string.Empty;
            Digest = digest;
            CategoryPath = categoryPath ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string DeclarationAuthoringId { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public PipelineBlackboardVariableLifetime Lifetime { get; }
        public PipelineBlackboardVariableAuthority Authority { get; }
        public PipelineBlackboardVariableSyncPolicy SyncPolicy { get; }
        public string InputValueId { get; }
        public PipelineBlackboardFactProjectionKind FactProjection { get; }
        public string WindowType { get; }
        public string WindowId { get; }
        public ulong Digest { get; }
        public string CategoryPath { get; }
    }

    public sealed class AgentDeleteBlackboardDeclarationCommand : AgentPatchCommand
    {
        public AgentDeleteBlackboardDeclarationCommand(string id, string path, AgentGraphTargetReference graph, string declarationAuthoringId)
            : base(id, AgentPatchCommandKind.DeleteBlackboardDeclaration, "delete_blackboard_declaration", AgentPatchOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string DeclarationAuthoringId { get; }
    }

    public sealed class AgentMoveBlackboardDeclarationCommand : AgentPatchCommand
    {
        public AgentMoveBlackboardDeclarationCommand(
            string id,
            string path,
            AgentGraphTargetReference sourceGraph,
            AgentGraphTargetReference targetGraph,
            string declarationAuthoringId,
            string key,
            Type valueType,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            PipelineBlackboardVariableAuthority authority,
            PipelineBlackboardVariableSyncPolicy syncPolicy,
            string inputValueId,
            PipelineBlackboardFactProjectionKind factProjection,
            string windowType,
            string windowId,
            ulong digest,
            string categoryPath)
            : base(id, AgentPatchCommandKind.MoveBlackboardDeclaration, "move_blackboard_declaration", AgentPatchOutputKind.BlackboardDeclaration, path, targetGraph.Identity, Vector2.zero)
        {
            SourceGraph = sourceGraph;
            TargetGraph = targetGraph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType;
            Scope = scope;
            Lifetime = lifetime;
            Authority = authority;
            SyncPolicy = syncPolicy;
            InputValueId = inputValueId ?? string.Empty;
            FactProjection = factProjection;
            WindowType = windowType ?? string.Empty;
            WindowId = windowId ?? string.Empty;
            Digest = digest;
            CategoryPath = categoryPath ?? string.Empty;
        }

        public AgentGraphTargetReference SourceGraph { get; }
        public AgentGraphTargetReference TargetGraph { get; }
        public string DeclarationAuthoringId { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public PipelineBlackboardVariableLifetime Lifetime { get; }
        public PipelineBlackboardVariableAuthority Authority { get; }
        public PipelineBlackboardVariableSyncPolicy SyncPolicy { get; }
        public string InputValueId { get; }
        public PipelineBlackboardFactProjectionKind FactProjection { get; }
        public string WindowType { get; }
        public string WindowId { get; }
        public ulong Digest { get; }
        public string CategoryPath { get; }
    }

    public sealed class AgentEnsureBlackboardWriteCommand : AgentPatchCommand
    {
        public AgentEnsureBlackboardWriteCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string elementAuthoringId,
            AgentAuthoringReference declaration,
            bool value,
            string displayName,
            Vector2 position)
            : base(id, AgentPatchCommandKind.EnsureBlackboardWrite, "ensure_blackboard_write", AgentPatchOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ElementAuthoringId = elementAuthoringId ?? string.Empty;
            Declaration = declaration;
            Value = value;
            DisplayName = displayName ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string ElementAuthoringId { get; }
        public AgentAuthoringReference Declaration { get; }
        public bool Value { get; }
        public string DisplayName { get; }
    }

    public abstract class AgentTimelineClipCommand : AgentPatchCommand
    {
        protected AgentTimelineClipCommand(string id, AgentPatchCommandKind kind, string operationName, AgentPatchOutputKind outputKind, string path, AgentTimelineTargetReference target)
            : base(id, kind, operationName, outputKind, path, target.TimelineAuthoringId, Vector2.zero)
        {
            Target = target;
        }

        public AgentTimelineTargetReference Target { get; }
    }

    public sealed class AgentEnsureTimelineTreeClipCommand : AgentTimelineClipCommand
    {
        public AgentEnsureTimelineTreeClipCommand(string id, string path, AgentTimelineTargetReference target, int startFrame, int endFrame, string phase)
            : base(id, AgentPatchCommandKind.EnsureTimelineTreeClip, "ensure_timeline_tree_clip", AgentPatchOutputKind.TimelineClip, path, target)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            Phase = phase ?? string.Empty;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
        public string Phase { get; }
    }

    public sealed class AgentEnsureMotionWarpTrackCommand : AgentPatchCommand
    {
        public AgentEnsureMotionWarpTrackCommand(string id, string path, string timelineAuthoringId, string trackAuthoringId, string displayName)
            : base(id, AgentPatchCommandKind.EnsureMotionWarpTrack, "ensure_motion_warp_track", AgentPatchOutputKind.TimelineTrack, path, timelineAuthoringId, Vector2.zero)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string DisplayName { get; }
    }

    public sealed class AgentEnsureMotionWarpClipCommand : AgentTimelineClipCommand
    {
        public AgentEnsureMotionWarpClipCommand(string id, string path, AgentTimelineTargetReference target, int startFrame, int endFrame)
            : base(id, AgentPatchCommandKind.EnsureMotionWarpClip, "ensure_motion_warp_clip", AgentPatchOutputKind.TimelineClip, path, target)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
    }

    public sealed class AgentConfigureMotionWarpSourceCommand : AgentTimelineClipCommand
    {
        public AgentConfigureMotionWarpSourceCommand(string id, string path, AgentTimelineTargetReference target, string sourceMotionClipAuthoringId)
            : base(id, AgentPatchCommandKind.ConfigureMotionWarpSource, "configure_motion_warp_source", AgentPatchOutputKind.None, path, target)
        {
            SourceMotionClipAuthoringId = sourceMotionClipAuthoringId ?? string.Empty;
        }

        public string SourceMotionClipAuthoringId { get; }
    }

    public sealed class AgentConfigureMotionWarpParametersCommand : AgentTimelineClipCommand
    {
        public AgentConfigureMotionWarpParametersCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.MotionWarpPositionMode positionMode,
            BTSMTL.Timeline.MotionWarpRotationMode rotationMode,
            Vector2 targetLocalPlanarOffset,
            float targetYawOffsetDegrees,
            float positionWeight,
            float yawWeight,
            float maxTotalPositionCorrection,
            float maxTotalYawCorrectionDegrees,
            AnimationCurve positionProgressCurve,
            AnimationCurve yawProgressCurve)
            : base(id, AgentPatchCommandKind.ConfigureMotionWarpParameters, "configure_motion_warp_parameters", AgentPatchOutputKind.None, path, target)
        {
            PositionMode = positionMode;
            RotationMode = rotationMode;
            TargetLocalPlanarOffset = targetLocalPlanarOffset;
            TargetYawOffsetDegrees = targetYawOffsetDegrees;
            PositionWeight = positionWeight;
            YawWeight = yawWeight;
            MaxTotalPositionCorrection = maxTotalPositionCorrection;
            MaxTotalYawCorrectionDegrees = maxTotalYawCorrectionDegrees;
            PositionProgressCurve = positionProgressCurve;
            YawProgressCurve = yawProgressCurve;
        }

        public BTSMTL.Timeline.MotionWarpPositionMode PositionMode { get; }
        public BTSMTL.Timeline.MotionWarpRotationMode RotationMode { get; }
        public Vector2 TargetLocalPlanarOffset { get; }
        public float TargetYawOffsetDegrees { get; }
        public float PositionWeight { get; }
        public float YawWeight { get; }
        public float MaxTotalPositionCorrection { get; }
        public float MaxTotalYawCorrectionDegrees { get; }
        public AnimationCurve PositionProgressCurve { get; }
        public AnimationCurve YawProgressCurve { get; }
    }

    public sealed class AgentDeleteTimelineClipCommand : AgentTimelineClipCommand
    {
        public AgentDeleteTimelineClipCommand(string id, string path, AgentTimelineTargetReference target)
            : base(id, AgentPatchCommandKind.DeleteTimelineClip, "delete_timeline_clip", AgentPatchOutputKind.None, path, target) { }
    }

    public sealed class AgentMoveTimelineClipCommand : AgentTimelineClipCommand
    {
        public AgentMoveTimelineClipCommand(string id, string path, AgentTimelineTargetReference target, int frameOffset)
            : base(id, AgentPatchCommandKind.MoveTimelineClip, "move_timeline_clip", AgentPatchOutputKind.None, path, target)
        {
            FrameOffset = frameOffset;
        }

        public int FrameOffset { get; }
    }

    public sealed class AgentConfigureTimelineClipEaseCommand : AgentTimelineClipCommand
    {
        public AgentConfigureTimelineClipEaseCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            int selfEaseInFrame,
            int selfEaseOutFrame)
            : base(id, AgentPatchCommandKind.ConfigureTimelineClipEase, "configure_timeline_clip_ease", AgentPatchOutputKind.None, path, target)
        {
            SelfEaseInFrame = selfEaseInFrame;
            SelfEaseOutFrame = selfEaseOutFrame;
        }

        public int SelfEaseInFrame { get; }
        public int SelfEaseOutFrame { get; }
    }

    public sealed class AgentConfigureTimelineCurveChannelCommand : AgentTimelineClipCommand
    {
        public AgentConfigureTimelineCurveChannelCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.TimelineCurveChannelId channelId,
            AnimationCurve curve)
            : base(
                id,
                AgentPatchCommandKind.ConfigureTimelineCurveChannel,
                "configure_timeline_curve_channel",
                AgentPatchOutputKind.None,
                path,
                target)
        {
            ChannelId = channelId;
            Curve = curve;
        }

        public BTSMTL.Timeline.TimelineCurveChannelId ChannelId { get; }
        public AnimationCurve Curve { get; }
    }

    public abstract class AgentAnimationTrackCommand : AgentPatchCommand
    {
        protected AgentAnimationTrackCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            AgentPatchOutputKind outputKind,
            string path,
            AgentTimelineTargetReference target)
            : base(id, kind, operationName, outputKind, path, target.TimelineAuthoringId, Vector2.zero)
        {
            Target = target;
        }

        public AgentTimelineTargetReference Target { get; }
    }

    public sealed class AgentConfigureAnimationTrackMarkerSyncCommand : AgentAnimationTrackCommand
    {
        public AgentConfigureAnimationTrackMarkerSyncCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.AnimationSyncMode mode,
            string syncGroupId,
            BTSMTL.Timeline.AnimationMarkerSequenceTopology topology,
            BTSMTL.Timeline.AnimationMarkerSyncRole syncRole)
            : base(id, AgentPatchCommandKind.ConfigureAnimationTrackMarkerSync, "configure_animation_track_marker_sync", AgentPatchOutputKind.None, path, target)
        {
            Mode = mode;
            SyncGroupId = syncGroupId ?? string.Empty;
            Topology = topology;
            SyncRole = syncRole;
        }

        public BTSMTL.Timeline.AnimationSyncMode Mode { get; }
        public string SyncGroupId { get; }
        public BTSMTL.Timeline.AnimationMarkerSequenceTopology Topology { get; }
        public BTSMTL.Timeline.AnimationMarkerSyncRole SyncRole { get; }
    }

    public sealed class AgentEnsureAnimationSyncMarkerCommand : AgentAnimationTrackCommand
    {
        public AgentEnsureAnimationSyncMarkerCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            string markerAuthoringId,
            string markerId,
            int frame)
            : base(id, AgentPatchCommandKind.EnsureAnimationSyncMarker, "ensure_animation_sync_marker", AgentPatchOutputKind.TimelineMarker, path, target)
        {
            MarkerAuthoringId = markerAuthoringId ?? string.Empty;
            MarkerId = markerId ?? string.Empty;
            Frame = frame;
        }

        public string MarkerAuthoringId { get; }
        public string MarkerId { get; }
        public int Frame { get; }
    }

    public abstract class AgentAnimationMarkerCommand : AgentAnimationTrackCommand
    {
        protected AgentAnimationMarkerCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker)
            : base(id, kind, operationName, AgentPatchOutputKind.None, path, target)
        {
            Marker = marker;
        }

        public AgentAuthoringReference Marker { get; }
    }

    public sealed class AgentMoveAnimationSyncMarkerCommand : AgentAnimationMarkerCommand
    {
        public AgentMoveAnimationSyncMarkerCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker,
            int frame)
            : base(id, AgentPatchCommandKind.MoveAnimationSyncMarker, "move_animation_sync_marker", path, target, marker)
        {
            Frame = frame;
        }

        public int Frame { get; }
    }

    public sealed class AgentDeleteAnimationSyncMarkerCommand : AgentAnimationMarkerCommand
    {
        public AgentDeleteAnimationSyncMarkerCommand(
            string id,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker)
            : base(id, AgentPatchCommandKind.DeleteAnimationSyncMarker, "delete_animation_sync_marker", path, target, marker)
        {
        }
    }

    public sealed class AgentEnsureTreeClipBlackboardWriteCommand : AgentTimelineClipCommand
    {
        public AgentEnsureTreeClipBlackboardWriteCommand(string id, string path, AgentTimelineTargetReference target, AgentAuthoringReference declaration)
            : base(id, AgentPatchCommandKind.EnsureTreeClipBlackboardWrite, "ensure_tree_clip_blackboard_write", AgentPatchOutputKind.None, path, target)
        {
            Declaration = declaration;
        }

        public AgentAuthoringReference Declaration { get; }
    }

    public sealed class AgentDeleteTransitionCommand : AgentPatchCommand
    {
        public AgentDeleteTransitionCommand(string id, string path, AgentStateMachineTargetReference stateMachine, string edgeAuthoringId)
            : base(id, AgentPatchCommandKind.DeleteTransition, "delete_transition", AgentPatchOutputKind.None, path, stateMachine.Identity, Vector2.zero)
        {
            StateMachine = stateMachine;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentEnsureGameplayTagCommand : AgentPatchCommand
    {
        public AgentEnsureGameplayTagCommand(string id, string path, string tag, string parentTag, string displayName, string debugCategory)
            : base(id, AgentPatchCommandKind.EnsureGameplayTag, "ensure_gameplay_tag", AgentPatchOutputKind.None, path, "GameplayTagCatalog", Vector2.zero)
        {
            Tag = new GameplayTagId(tag);
            ParentTag = new GameplayTagId(parentTag);
            DisplayName = displayName ?? string.Empty;
            DebugCategory = debugCategory ?? string.Empty;
        }

        public GameplayTagId Tag { get; }
        public GameplayTagId ParentTag { get; }
        public string DisplayName { get; }
        public string DebugCategory { get; }
    }

    public abstract class AgentActionProfileAdmissionCommand : AgentPatchCommand
    {
        protected AgentActionProfileAdmissionCommand(string id, AgentPatchCommandKind kind, string operationName, string path, AgentAssetReference actionProfile)
            : base(id, kind, operationName, AgentPatchOutputKind.None, path, actionProfile.LogicalId, Vector2.zero)
        {
            ActionProfile = actionProfile;
        }

        public AgentAssetReference ActionProfile { get; }
    }

    public sealed class AgentSetActionProfileGrantedTagsCommand : AgentActionProfileAdmissionCommand
    {
        readonly ReadOnlyCollection<GameplayTagId> m_Tags;

        public AgentSetActionProfileGrantedTagsCommand(string id, string path, AgentAssetReference actionProfile, IList<GameplayTagId> tags)
            : base(id, AgentPatchCommandKind.SetActionProfileGrantedTags, "set_action_profile_granted_tags", path, actionProfile)
        {
            m_Tags = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(tags));
        }

        public IReadOnlyList<GameplayTagId> Tags => m_Tags;
    }

    public sealed class AgentSetActionProfileCancelQueryCommand : AgentActionProfileAdmissionCommand
    {
        readonly ReadOnlyCollection<GameplayTagId> m_All;
        readonly ReadOnlyCollection<GameplayTagId> m_Any;
        readonly ReadOnlyCollection<GameplayTagId> m_None;

        public AgentSetActionProfileCancelQueryCommand(string id, string path, AgentAssetReference actionProfile, IList<GameplayTagId> all, IList<GameplayTagId> any, IList<GameplayTagId> none)
            : base(id, AgentPatchCommandKind.SetActionProfileCancelQuery, "set_action_profile_cancel_query", path, actionProfile)
        {
            m_All = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(all));
            m_Any = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(any));
            m_None = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(none));
        }

        public IReadOnlyList<GameplayTagId> All => m_All;
        public IReadOnlyList<GameplayTagId> Any => m_Any;
        public IReadOnlyList<GameplayTagId> None => m_None;
    }

    public sealed class AgentSetActionProfileTargetRequirementCommand : AgentActionProfileAdmissionCommand
    {
        public AgentSetActionProfileTargetRequirementCommand(
            string id,
            string path,
            AgentAssetReference actionProfile,
            ActionTargetRequirement targetRequirement)
            : base(id, AgentPatchCommandKind.SetActionProfileTargetRequirement, "set_action_profile_target_requirement", path, actionProfile)
        {
            TargetRequirement = targetRequirement;
        }

        public ActionTargetRequirement TargetRequirement { get; }
    }

    public sealed class AgentSetActionRequestTimingClassCommand : AgentPatchCommand
    {
        public AgentSetActionRequestTimingClassCommand(
            string id,
            string path,
            string requestId,
            CharacterActionRequestTimingClass timingClass)
            : base(
                id,
                AgentPatchCommandKind.SetActionRequestTimingClass,
                "set_action_request_timing_class",
                AgentPatchOutputKind.None,
                path,
                requestId,
                Vector2.zero)
        {
            RequestId = requestId;
            TimingClass = timingClass;
        }

        public string RequestId { get; }
        public CharacterActionRequestTimingClass TimingClass { get; }
    }

    public abstract class AgentGraphLinkCommand : AgentPatchCommand
    {
        protected AgentGraphLinkCommand(
            string id,
            AgentPatchCommandKind kind,
            string operationName,
            AgentPatchOutputKind outputKind,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference source,
            AgentElementTargetReference target)
            : base(id, kind, operationName, outputKind, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            Source = source;
            Target = target;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference Source { get; }
        public AgentElementTargetReference Target { get; }
    }

    public sealed class AgentDeleteFlowEdgeCommand : AgentPatchCommand
    {
        public AgentDeleteFlowEdgeCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string edgeAuthoringId)
            : base(id, AgentPatchCommandKind.DeleteFlowEdge, "delete_flow_edge", AgentPatchOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentLinkFlowCommand : AgentGraphLinkCommand
    {
        public AgentLinkFlowCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference source,
            AgentElementTargetReference target,
            string startPort,
            string endPort)
            : base(id, AgentPatchCommandKind.LinkFlow, "link_flow", AgentPatchOutputKind.FlowEdge, path, graph, source, target)
        {
            StartPort = startPort;
            EndPort = endPort;
        }

        public string StartPort { get; }
        public string EndPort { get; }
    }

    public sealed class AgentLinkPropertyCommand : AgentGraphLinkCommand
    {
        public AgentLinkPropertyCommand(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference source,
            AgentElementTargetReference target,
            string startPropertyPort,
            string endPropertyPort)
            : base(id, AgentPatchCommandKind.LinkProperty, "link_property", AgentPatchOutputKind.PropertyEdge, path, graph, source, target)
        {
            StartPropertyPort = startPropertyPort;
            EndPropertyPort = endPropertyPort;
        }

        public string StartPropertyPort { get; }
        public string EndPropertyPort { get; }
    }

    public readonly struct AgentPlannedOutputSymbol
    {
        public AgentPlannedOutputSymbol(string operationId, AgentPatchOutputKind kind, string ownerScope)
        {
            OperationId = operationId;
            Kind = kind;
            OwnerScope = ownerScope ?? string.Empty;
        }

        public string OperationId { get; }
        public AgentPatchOutputKind Kind { get; }
        public string OwnerScope { get; }
    }

    public sealed class AgentPatchCommandPlan
    {
        readonly ReadOnlyCollection<AgentPatchCommand> m_Commands;
        readonly ReadOnlyDictionary<string, AgentPlannedOutputSymbol> m_Symbols;

        public AgentPatchCommandPlan(
            IList<AgentPatchCommand> commands,
            IDictionary<string, AgentPlannedOutputSymbol> symbols,
            string sourceMacro,
            string sourceMacroVersion)
        {
            m_Commands = new ReadOnlyCollection<AgentPatchCommand>(new List<AgentPatchCommand>(commands));
            m_Symbols = new ReadOnlyDictionary<string, AgentPlannedOutputSymbol>(
                new Dictionary<string, AgentPlannedOutputSymbol>(symbols, StringComparer.Ordinal));
            SourceMacro = sourceMacro ?? string.Empty;
            SourceMacroVersion = sourceMacroVersion ?? string.Empty;
        }

        public IReadOnlyList<AgentPatchCommand> Commands => m_Commands;
        public IReadOnlyDictionary<string, AgentPlannedOutputSymbol> Symbols => m_Symbols;
        public string SourceMacro { get; }
        public string SourceMacroVersion { get; }
    }
}
