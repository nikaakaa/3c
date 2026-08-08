using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public enum AgentMutationKind
    {
        EnsureStateMachine,
        EnsureState,
        DeleteState,
        EnsureTransition,
        RewireTransition,
        EnsureConditionRule,
        EnsureActionExitLifecycle,
        DeleteStateBehaviorNode,
        EnsureStateBehaviorNode,
        EnsureTimelineNode,
        EnsureInlineTimeline,
        EnsureActionActivation,
        EnsureActionLifecycleTransition,
        EnsureInputNode,
        EnsureConditionValueNode,
        ConfigureActionAdmission,
        EnsureBlackboardDeclaration,
        MoveBlackboardDeclaration,
        DeleteBlackboardDeclaration,
        SetBlackboardSchemaRevision,
        EnsureExposedPropertyNode,
        EnsureTimelineTreeClip,
        EnsureMotionCurveTrack,
        EnsureMotionCurveClip,
        ConfigureMotionCurveClip,
        EnsureMotionWarpTrack,
        DeleteTimelineTrack,
        EnsureMotionWarpClip,
        ConfigureMotionWarpSource,
        ConfigureMotionWarpParameters,
        MoveTimelineClip,
        ConfigureTimelineClipEase,
        ConfigureTimelineCurveChannel,
        ConfigureAnimationTrackChannel,
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
        EnsureAIControllerDefinition,
        EnsureAIControllerTree,
        BindAIControllerAssets,
        ConfigureAICandidates,
        EnsureAIBlackboardDeclaration,
        EnsureAISharedNode,
        EnsureAIObservationNode,
        EnsureAIMemoryNode,
        EnsureAIContinuousInput,
        EnsureAIActionTarget,
        EnsureAIActionRequest,
        EnsureBTConditionRule,
        EnsureGraphNode,
        DeleteGraphNode,
        DeleteFlowEdge,
        DeletePropertyEdge,
        LinkFlow,
        LinkProperty
    }

    public enum AgentMutationOutputKind
    {
        None,
        StateMachine,
        State,
        Transition,
        Node,
        BlackboardDeclaration,
        Timeline,
        TimelineTrack,
        TimelineClip,
        TimelineMarker,
        FlowEdge,
        PropertyEdge
    }

    public enum AgentAIObservationNodeKind
    {
        ReadSelf,
        EnumerateConfiguredCandidates,
        SelectNearestCandidate,
        ReadTargetDistance,
        ReadTargetDirection,
        ReadSelectedTargetSnapshot
    }

    public enum AgentAISharedNodeKind
    {
        Loop,
        Sequence,
        Selector,
        Compare,
        WaitTicks
    }

    public enum AgentAIMemoryNodeKind
    {
        Read,
        Write
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
        CanActivateAction,
        AITargetDistanceCompareBlackboard
    }

    public readonly struct AgentPlannedIdentityReference
    {
        public AgentPlannedIdentityReference(string identity, string role)
        {
            Identity = identity ?? string.Empty;
            Role = role ?? string.Empty;
        }

        public string Identity { get; }
        public string Role { get; }
        public bool IsValid => !string.IsNullOrEmpty(Identity);
        public string Value => string.IsNullOrEmpty(Role) ? Identity : $"{Identity}#{Role}";

        public static AgentPlannedIdentityReference Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
                return default;
            int separator = value.LastIndexOf('#');
            return separator < 0
                ? new AgentPlannedIdentityReference(value, string.Empty)
                : new AgentPlannedIdentityReference(value.Substring(0, separator), value.Substring(separator + 1));
        }
    }

    public readonly struct AgentAuthoringReference
    {
        public AgentAuthoringReference(string authoringId, AgentPlannedIdentityReference plannedIdentity)
        {
            AuthoringId = authoringId ?? string.Empty;
            PlannedIdentity = plannedIdentity;
        }

        public string AuthoringId { get; }
        public AgentPlannedIdentityReference PlannedIdentity { get; }
        public bool IsValid => !string.IsNullOrEmpty(AuthoringId) || PlannedIdentity.IsValid;
        public string Identity => !string.IsNullOrEmpty(AuthoringId) ? AuthoringId : PlannedIdentity.Value;
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

    public readonly struct AgentFlowEdgeTargetReference
    {
        public AgentFlowEdgeTargetReference(AgentAuthoringReference value) => Value = value;
        public AgentAuthoringReference Value { get; }
        public bool IsValid => Value.IsValid;
        public string Identity => Value.Identity;
    }

    public readonly struct AgentTimelineTargetReference
    {
        public AgentTimelineTargetReference(string timelineAuthoringId, string trackAuthoringId, string clipAuthoringId)
            : this(timelineAuthoringId, default, trackAuthoringId, default, clipAuthoringId, default) { }

        public AgentTimelineTargetReference(
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            AgentPlannedIdentityReference clipPlannedIdentity)
            : this(timelineAuthoringId, default, trackAuthoringId, default, clipAuthoringId, clipPlannedIdentity) { }

        public AgentTimelineTargetReference(
            string timelineAuthoringId,
            string trackAuthoringId,
            AgentPlannedIdentityReference trackPlannedIdentity,
            string clipAuthoringId,
            AgentPlannedIdentityReference clipPlannedIdentity)
            : this(timelineAuthoringId, default, trackAuthoringId, trackPlannedIdentity, clipAuthoringId, clipPlannedIdentity) { }

        public AgentTimelineTargetReference(
            string timelineAuthoringId,
            AgentPlannedIdentityReference timelinePlannedIdentity,
            string trackAuthoringId,
            AgentPlannedIdentityReference trackPlannedIdentity,
            string clipAuthoringId,
            AgentPlannedIdentityReference clipPlannedIdentity)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TimelinePlannedIdentity = timelinePlannedIdentity;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            TrackPlannedIdentity = trackPlannedIdentity;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            ClipPlannedIdentity = clipPlannedIdentity;
        }

        public string TimelineAuthoringId { get; }
        public AgentPlannedIdentityReference TimelinePlannedIdentity { get; }
        public string TimelineIdentity => TimelinePlannedIdentity.IsValid ? TimelinePlannedIdentity.Identity : TimelineAuthoringId;
        public string TrackAuthoringId { get; }
        public AgentPlannedIdentityReference TrackPlannedIdentity { get; }
        public string ClipAuthoringId { get; }
        public AgentPlannedIdentityReference ClipPlannedIdentity { get; }
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

    public sealed class AgentConditionTermMutation
    {
        public AgentConditionTermMutation(
            AgentConditionTermKind kind,
            string blackboardKey,
            bool negate,
            string request,
            string windowType,
            AgentAssetReference actionProfile,
            string targetSnapshotBlackboardKey,
            CompareNode.CompareType compareType)
        {
            Kind = kind;
            BlackboardKey = blackboardKey ?? string.Empty;
            Negate = negate;
            Request = request ?? string.Empty;
            WindowType = windowType ?? string.Empty;
            ActionProfile = actionProfile;
            TargetSnapshotBlackboardKey = targetSnapshotBlackboardKey ?? string.Empty;
            CompareType = compareType;
        }

        public AgentConditionTermKind Kind { get; }
        public string BlackboardKey { get; }
        public bool Negate { get; }
        public string Request { get; }
        public string WindowType { get; }
        public AgentAssetReference ActionProfile { get; }
        public string TargetSnapshotBlackboardKey { get; }
        public CompareNode.CompareType CompareType { get; }
    }

    public sealed class AgentConditionGroupMutation
    {
        readonly ReadOnlyCollection<AgentConditionTermMutation> m_Terms;

        public AgentConditionGroupMutation(IList<AgentConditionTermMutation> terms)
        {
            m_Terms = new ReadOnlyCollection<AgentConditionTermMutation>(new List<AgentConditionTermMutation>(terms));
        }

        public IReadOnlyList<AgentConditionTermMutation> Terms => m_Terms;
    }

    public abstract class AgentMutation
    {
        protected AgentMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            AgentMutationOutputKind outputKind,
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
        public AgentMutationKind Kind { get; }
        public string OperationName { get; }
        public AgentMutationOutputKind OutputKind { get; }
        public string Path { get; }
        public string OwnerScope { get; }
        public Vector2 Position { get; }
    }

    public sealed class AgentEnsureStateMachineMutation : AgentMutation
    {
        public AgentEnsureStateMachineMutation(
            string id,
            string path,
            AgentGraphTargetReference parentGraph,
            AgentElementTargetReference existingOwner,
            string existingGraphAuthoringId,
            string displayName,
            string lifecycleSlot,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureStateMachine, "ensure_state_machine", AgentMutationOutputKind.StateMachine, path, parentGraph.Identity, position)
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

    public sealed class AgentEnsureStateMutation : AgentMutation
    {
        public AgentEnsureStateMutation(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentStateTargetReference existingState,
            string stateName,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureState, "ensure_state", AgentMutationOutputKind.State, path, stateMachine.Identity, position)
        {
            StateMachine = stateMachine;
            ExistingState = existingState;
            StateName = stateName;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentStateTargetReference ExistingState { get; }
        public string StateName { get; }
    }

    public sealed class AgentDeleteStateMutation : AgentMutation
    {
        public AgentDeleteStateMutation(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentStateTargetReference state)
            : base(id, AgentMutationKind.DeleteState, "delete_state", AgentMutationOutputKind.None, path, stateMachine.Identity, Vector2.zero)
        {
            StateMachine = stateMachine;
            State = state;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public AgentStateTargetReference State { get; }
    }

    public class AgentEnsureTransitionMutation : AgentMutation
    {
        public AgentEnsureTransitionMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentElementTargetReference from,
            AgentElementTargetReference to,
            string edgeAuthoringId,
            int priority,
            Vector2 position)
            : base(id, kind, operationName, AgentMutationOutputKind.Transition, path, stateMachine.Identity, position)
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

    public sealed class AgentEnsureConditionRuleMutation : AgentEnsureTransitionMutation
    {
        readonly ReadOnlyCollection<AgentConditionGroupMutation> m_Groups;

        public AgentEnsureConditionRuleMutation(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentElementTargetReference from,
            AgentElementTargetReference to,
            string edgeAuthoringId,
            int priority,
            IList<AgentConditionGroupMutation> groups,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureConditionRule, "ensure_condition_rule", path, stateMachine, from, to, edgeAuthoringId, priority, position)
        {
            m_Groups = new ReadOnlyCollection<AgentConditionGroupMutation>(new List<AgentConditionGroupMutation>(groups));
        }

        public IReadOnlyList<AgentConditionGroupMutation> Groups => m_Groups;
    }

    public sealed class AgentRewireTransitionMutation : AgentMutation
    {
        public AgentRewireTransitionMutation(
            string id,
            string path,
            AgentStateMachineTargetReference stateMachine,
            AgentElementTargetReference from,
            AgentElementTargetReference to,
            string edgeAuthoringId,
            int priority)
            : base(
                id,
                AgentMutationKind.RewireTransition,
                "rewire_transition",
                AgentMutationOutputKind.None,
                path,
                stateMachine.Identity,
                Vector2.zero)
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

    public abstract class AgentStateBehaviorMutation : AgentMutation
    {
        protected AgentStateBehaviorMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            AgentMutationOutputKind outputKind,
            string path,
            AgentStateBehaviorTargetReference target,
            Vector2 position)
            : base(id, kind, operationName, outputKind, path, target.OwnerScope, position)
        {
            Target = target;
        }

        public AgentStateBehaviorTargetReference Target { get; }
    }

    public sealed class AgentEnsureActionExitLifecycleMutation : AgentStateBehaviorMutation
    {
        readonly ReadOnlyCollection<AgentConditionGroupMutation> m_CancelConditionGroups;

        public AgentEnsureActionExitLifecycleMutation(
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
            IList<AgentConditionGroupMutation> cancelConditionGroups,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureActionExitLifecycle, "ensure_action_exit_lifecycle", AgentMutationOutputKind.Node, path, target, position)
        {
            Source = source;
            ExistingElement = existingElement;
            ActionContext = actionContext;
            CancelReason = cancelReason;
            InterruptReason = interruptReason;
            AbortReason = abortReason;
            CompleteReason = completeReason;
            m_CancelConditionGroups = new ReadOnlyCollection<AgentConditionGroupMutation>(new List<AgentConditionGroupMutation>(cancelConditionGroups));
        }

        public AgentElementTargetReference Source { get; }
        public AgentElementTargetReference ExistingElement { get; }
        public AgentAssetReference ActionContext { get; }
        public string CancelReason { get; }
        public string InterruptReason { get; }
        public string AbortReason { get; }
        public string CompleteReason { get; }
        public IReadOnlyList<AgentConditionGroupMutation> CancelConditionGroups => m_CancelConditionGroups;
    }

    public sealed class AgentDeleteStateBehaviorNodeMutation : AgentStateBehaviorMutation
    {
        public AgentDeleteStateBehaviorNodeMutation(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference element)
            : base(id, AgentMutationKind.DeleteStateBehaviorNode, "delete_state_behavior_node", AgentMutationOutputKind.None, path, target, Vector2.zero)
        {
            Element = element;
        }

        public AgentElementTargetReference Element { get; }
    }

    public sealed class AgentEnsureStateBehaviorNodeMutation : AgentStateBehaviorMutation
    {
        public AgentEnsureStateBehaviorNodeMutation(
            string id,
            string path,
            AgentStateBehaviorTargetReference target,
            AgentElementTargetReference existingElement,
            string nodeType,
            string displayName,
            string lifecycleSlot,
            LoopNode.StopType loopStopType,
            CompareNode.CompareType compareType,
            float moveSpeed,
            float turnSpeedDegrees,
            bool cameraRelative,
            bool continuous,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureStateBehaviorNode, "ensure_state_behavior_node", AgentMutationOutputKind.Node, path, target, position)
        {
            ExistingElement = existingElement;
            NodeType = nodeType;
            DisplayName = displayName;
            LifecycleSlot = lifecycleSlot ?? string.Empty;
            LoopStopType = loopStopType;
            CompareType = compareType;
            MoveSpeed = moveSpeed;
            TurnSpeedDegrees = turnSpeedDegrees;
            CameraRelative = cameraRelative;
            Continuous = continuous;
        }

        public AgentElementTargetReference ExistingElement { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public string LifecycleSlot { get; }
        public LoopNode.StopType LoopStopType { get; }
        public CompareNode.CompareType CompareType { get; }
        public float MoveSpeed { get; }
        public float TurnSpeedDegrees { get; }
        public bool CameraRelative { get; }
        public bool Continuous { get; }
    }

    public sealed class AgentEnsureTimelineNodeMutation : AgentStateBehaviorMutation
    {
        public AgentEnsureTimelineNodeMutation(
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
            : base(id, AgentMutationKind.EnsureTimelineNode, "ensure_timeline_node", AgentMutationOutputKind.Node, path, target, position)
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

    public sealed class AgentEnsureActionActivationMutation : AgentStateBehaviorMutation
    {
        public AgentEnsureActionActivationMutation(
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
            : base(id, AgentMutationKind.EnsureActionActivation, "ensure_action_activation", AgentMutationOutputKind.Node, path, target, position)
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

    public sealed class AgentEnsureActionLifecycleTransitionMutation : AgentStateBehaviorMutation
    {
        public AgentEnsureActionLifecycleTransitionMutation(
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
            : base(id, AgentMutationKind.EnsureActionLifecycleTransition, "ensure_action_lifecycle_transition", AgentMutationOutputKind.Node, path, target, position)
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

    public sealed class AgentEnsureInputNodeMutation : AgentMutation
    {
        public AgentEnsureInputNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existingElement,
            string nodeType,
            string displayName,
            string inputId,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureInputNode, "ensure_input_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingElement = existingElement;
            NodeType = nodeType ?? string.Empty;
            DisplayName = displayName;
            InputId = inputId;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingElement { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public string InputId { get; }
    }

    public enum AgentConditionValueNodeConfigurationKind
    {
        None,
        BlackboardDeclaration,
        StateExitCause,
        ActionContext,
        ActionWindow,
        ActionAdmission
    }

    public sealed class AgentEnsureConditionValueNodeMutation : AgentMutation
    {
        public AgentEnsureConditionValueNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existingElement,
            string nodeType,
            string displayName,
            Vector2 position,
            AgentConditionValueNodeConfigurationKind configurationKind,
            AgentAuthoringReference blackboardDeclaration,
            StateExitCause stateExitCause,
            AgentAssetReference actionContext,
            string windowType,
            AgentAssetReference actionProfile,
            AgentAuthoringReference targetSnapshotDeclaration)
            : base(id, AgentMutationKind.EnsureConditionValueNode, "ensure_condition_value_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingElement = existingElement;
            NodeType = nodeType ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ConfigurationKind = configurationKind;
            BlackboardDeclaration = blackboardDeclaration;
            StateExitCause = stateExitCause;
            ActionContext = actionContext;
            WindowType = windowType ?? string.Empty;
            ActionProfile = actionProfile;
            TargetSnapshotDeclaration = targetSnapshotDeclaration;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingElement { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public AgentConditionValueNodeConfigurationKind ConfigurationKind { get; }
        public AgentAuthoringReference BlackboardDeclaration { get; }
        public StateExitCause StateExitCause { get; }
        public AgentAssetReference ActionContext { get; }
        public string WindowType { get; }
        public AgentAssetReference ActionProfile { get; }
        public AgentAuthoringReference TargetSnapshotDeclaration { get; }
    }

    public sealed class AgentConfigureActionAdmissionMutation : AgentMutation
    {
        public AgentConfigureActionAdmissionMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference element,
            AgentAssetReference actionProfile)
            : base(id, AgentMutationKind.ConfigureActionAdmission, "configure_action_admission", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            Element = element;
            ActionProfile = actionProfile;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference Element { get; }
        public AgentAssetReference ActionProfile { get; }
    }

    public sealed class AgentEnsureBlackboardDeclarationMutation : AgentMutation
    {
        public AgentEnsureBlackboardDeclarationMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string declarationAuthoringId,
            string key,
            Type valueType,
            object defaultValue,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            AgentSnapshotBlackboardInputBinding inputBinding,
            AgentSnapshotBlackboardFactProjection factProjection,
            string categoryPath)
            : base(id, AgentMutationKind.EnsureBlackboardDeclaration, "ensure_blackboard_declaration", AgentMutationOutputKind.BlackboardDeclaration, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType;
            DefaultValue = defaultValue;
            Scope = scope;
            Lifetime = lifetime;
            InputBinding = inputBinding;
            FactProjection = factProjection;
            CategoryPath = categoryPath ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string DeclarationAuthoringId { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public object DefaultValue { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public PipelineBlackboardVariableLifetime Lifetime { get; }
        public AgentSnapshotBlackboardInputBinding InputBinding { get; }
        public AgentSnapshotBlackboardFactProjection FactProjection { get; }
        public string CategoryPath { get; }
    }

    public sealed class AgentDeleteBlackboardDeclarationMutation : AgentMutation
    {
        public AgentDeleteBlackboardDeclarationMutation(string id, string path, AgentGraphTargetReference graph, string declarationAuthoringId)
            : base(id, AgentMutationKind.DeleteBlackboardDeclaration, "delete_blackboard_declaration", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string DeclarationAuthoringId { get; }
    }

    public sealed class AgentSetBlackboardSchemaRevisionMutation : AgentMutation
    {
        public AgentSetBlackboardSchemaRevisionMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            int revision)
            : base(id, AgentMutationKind.SetBlackboardSchemaRevision, "set_blackboard_schema_revision", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            Revision = revision;
        }

        public AgentGraphTargetReference Graph { get; }
        public int Revision { get; }
    }

    public sealed class AgentMoveBlackboardDeclarationMutation : AgentMutation
    {
        public AgentMoveBlackboardDeclarationMutation(
            string id,
            string path,
            AgentGraphTargetReference sourceGraph,
            AgentGraphTargetReference targetGraph,
            string declarationAuthoringId,
            string key,
            Type valueType,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            AgentSnapshotBlackboardInputBinding inputBinding,
            AgentSnapshotBlackboardFactProjection factProjection,
            string categoryPath)
            : base(id, AgentMutationKind.MoveBlackboardDeclaration, "move_blackboard_declaration", AgentMutationOutputKind.BlackboardDeclaration, path, targetGraph.Identity, Vector2.zero)
        {
            SourceGraph = sourceGraph;
            TargetGraph = targetGraph;
            DeclarationAuthoringId = declarationAuthoringId ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType;
            Scope = scope;
            Lifetime = lifetime;
            InputBinding = inputBinding;
            FactProjection = factProjection;
            CategoryPath = categoryPath ?? string.Empty;
        }

        public AgentGraphTargetReference SourceGraph { get; }
        public AgentGraphTargetReference TargetGraph { get; }
        public string DeclarationAuthoringId { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public PipelineBlackboardVariableLifetime Lifetime { get; }
        public AgentSnapshotBlackboardInputBinding InputBinding { get; }
        public AgentSnapshotBlackboardFactProjection FactProjection { get; }
        public string CategoryPath { get; }
    }

    public sealed class AgentEnsureExposedPropertyNodeMutation : AgentMutation
    {
        public AgentEnsureExposedPropertyNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string elementAuthoringId,
            AgentAuthoringReference declaration,
            ExposedPropertyNodeType mode,
            Type valueType,
            object value,
            string displayName,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureExposedPropertyNode, "ensure_exposed_property_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ElementAuthoringId = elementAuthoringId ?? string.Empty;
            Declaration = declaration;
            Mode = mode;
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            Value = value;
            DisplayName = displayName ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string ElementAuthoringId { get; }
        public AgentAuthoringReference Declaration { get; }
        public ExposedPropertyNodeType Mode { get; }
        public Type ValueType { get; }
        public object Value { get; }
        public string DisplayName { get; }
    }

    public abstract class AgentTimelineClipMutation : AgentMutation
    {
        protected AgentTimelineClipMutation(string id, AgentMutationKind kind, string operationName, AgentMutationOutputKind outputKind, string path, AgentTimelineTargetReference target)
            : base(id, kind, operationName, outputKind, path, target.TimelineIdentity, Vector2.zero)
        {
            Target = target;
        }

        public AgentTimelineTargetReference Target { get; }
    }

    public sealed class AgentEnsureTimelineTreeClipMutation : AgentTimelineClipMutation
    {
        public AgentEnsureTimelineTreeClipMutation(string id, string path, AgentTimelineTargetReference target, int startFrame, int endFrame, string phase)
            : base(id, AgentMutationKind.EnsureTimelineTreeClip, "ensure_timeline_tree_clip", AgentMutationOutputKind.TimelineClip, path, target)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
            Phase = phase ?? string.Empty;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
        public string Phase { get; }
    }

    public sealed class AgentEnsureInlineTimelineMutation : AgentMutation
    {
        public AgentEnsureInlineTimelineMutation(
            string id,
            string path,
            AgentElementTargetReference timelineNode,
            string displayName)
            : base(id, AgentMutationKind.EnsureInlineTimeline, "ensure_inline_timeline", AgentMutationOutputKind.Timeline, path, timelineNode.Identity, Vector2.zero)
        {
            TimelineNode = timelineNode;
            DisplayName = displayName ?? string.Empty;
        }

        public AgentElementTargetReference TimelineNode { get; }
        public string DisplayName { get; }
    }

    public sealed class AgentEnsureMotionCurveTrackMutation : AgentMutation
    {
        public AgentEnsureMotionCurveTrackMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            string displayName)
            : base(id, AgentMutationKind.EnsureMotionCurveTrack, "ensure_motion_curve_track", AgentMutationOutputKind.TimelineTrack, path, target.TimelineIdentity, Vector2.zero)
        {
            Target = target;
            DisplayName = displayName ?? string.Empty;
        }

        public AgentTimelineTargetReference Target { get; }
        public string DisplayName { get; }
    }

    public sealed class AgentEnsureMotionCurveClipMutation : AgentTimelineClipMutation
    {
        public AgentEnsureMotionCurveClipMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            int startFrame,
            int endFrame)
            : base(id, AgentMutationKind.EnsureMotionCurveClip, "ensure_motion_curve_clip", AgentMutationOutputKind.TimelineClip, path, target)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
    }

    public sealed class AgentConfigureMotionCurveClipMutation : AgentTimelineClipMutation
    {
        public AgentConfigureMotionCurveClipMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            string curveId,
            int curveEndFrame,
            TimelineMotionContributionSpace space,
            TimelineMotionChannel channel,
            TimelineMotionBlendMode blendMode,
            int priority,
            bool consumeLowerChannels)
            : base(id, AgentMutationKind.ConfigureMotionCurveClip, "configure_motion_curve_clip", AgentMutationOutputKind.None, path, target)
        {
            CurveId = curveId ?? string.Empty;
            CurveEndFrame = curveEndFrame;
            Space = space;
            Channel = channel;
            BlendMode = blendMode;
            Priority = priority;
            ConsumeLowerChannels = consumeLowerChannels;
        }

        public string CurveId { get; }
        public int CurveEndFrame { get; }
        public TimelineMotionContributionSpace Space { get; }
        public TimelineMotionChannel Channel { get; }
        public TimelineMotionBlendMode BlendMode { get; }
        public int Priority { get; }
        public bool ConsumeLowerChannels { get; }
    }

    public sealed class AgentEnsureMotionWarpTrackMutation : AgentMutation
    {
        public AgentEnsureMotionWarpTrackMutation(string id, string path, string timelineAuthoringId, string trackAuthoringId, string displayName)
            : base(id, AgentMutationKind.EnsureMotionWarpTrack, "ensure_motion_warp_track", AgentMutationOutputKind.TimelineTrack, path, timelineAuthoringId, Vector2.zero)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string DisplayName { get; }
    }

    public sealed class AgentDeleteTimelineTrackMutation : AgentMutation
    {
        public AgentDeleteTimelineTrackMutation(string id, string path, string timelineAuthoringId, string trackAuthoringId)
            : base(id, AgentMutationKind.DeleteTimelineTrack, "delete_timeline_track", AgentMutationOutputKind.None, path, timelineAuthoringId, Vector2.zero)
        {
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
        }

        public string TimelineAuthoringId { get; }
        public AgentPlannedIdentityReference TimelinePlannedIdentity { get; }
        public string TimelineIdentity => TimelinePlannedIdentity.IsValid ? TimelinePlannedIdentity.Identity : TimelineAuthoringId;
        public string TrackAuthoringId { get; }
    }

    public sealed class AgentEnsureMotionWarpClipMutation : AgentTimelineClipMutation
    {
        public AgentEnsureMotionWarpClipMutation(string id, string path, AgentTimelineTargetReference target, int startFrame, int endFrame)
            : base(id, AgentMutationKind.EnsureMotionWarpClip, "ensure_motion_warp_clip", AgentMutationOutputKind.TimelineClip, path, target)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
    }

    public sealed class AgentConfigureMotionWarpSourceMutation : AgentTimelineClipMutation
    {
        public AgentConfigureMotionWarpSourceMutation(string id, string path, AgentTimelineTargetReference target, string sourceMotionClipAuthoringId)
            : base(id, AgentMutationKind.ConfigureMotionWarpSource, "configure_motion_warp_source", AgentMutationOutputKind.None, path, target)
        {
            SourceMotionClipAuthoringId = sourceMotionClipAuthoringId ?? string.Empty;
        }

        public string SourceMotionClipAuthoringId { get; }
    }

    public sealed class AgentConfigureMotionWarpParametersMutation : AgentTimelineClipMutation
    {
        public AgentConfigureMotionWarpParametersMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.MotionWarpTranslationMode translationMode,
            BTSMTL.Timeline.MotionWarpTargetOffsetSpace targetOffsetSpace,
            BTSMTL.Timeline.MotionWarpRotationMode rotationMode,
            BTSMTL.Timeline.MotionWarpRotationMethod rotationMethod,
            Vector2 targetPlanarOffset,
            float targetYawOffsetDegrees,
            float maxTotalPositionCorrection,
            float maxTotalYawCorrectionDegrees,
            float maximumYawRateDegreesPerSecond,
            BTSMTL.Timeline.MotionWarpLimitPolicy limitPolicy,
            AnimationCurve positionProgressCurve,
            AnimationCurve yawProgressCurve)
            : base(id, AgentMutationKind.ConfigureMotionWarpParameters, "configure_motion_warp_parameters", AgentMutationOutputKind.None, path, target)
        {
            TranslationMode = translationMode;
            TargetOffsetSpace = targetOffsetSpace;
            RotationMode = rotationMode;
            RotationMethod = rotationMethod;
            TargetPlanarOffset = targetPlanarOffset;
            TargetYawOffsetDegrees = targetYawOffsetDegrees;
            MaxTotalPositionCorrection = maxTotalPositionCorrection;
            MaxTotalYawCorrectionDegrees = maxTotalYawCorrectionDegrees;
            MaximumYawRateDegreesPerSecond = maximumYawRateDegreesPerSecond;
            LimitPolicy = limitPolicy;
            PositionProgressCurve = positionProgressCurve;
            YawProgressCurve = yawProgressCurve;
        }

        public BTSMTL.Timeline.MotionWarpTranslationMode TranslationMode { get; }
        public BTSMTL.Timeline.MotionWarpTargetOffsetSpace TargetOffsetSpace { get; }
        public BTSMTL.Timeline.MotionWarpRotationMode RotationMode { get; }
        public BTSMTL.Timeline.MotionWarpRotationMethod RotationMethod { get; }
        public Vector2 TargetPlanarOffset { get; }
        public float TargetYawOffsetDegrees { get; }
        public float MaxTotalPositionCorrection { get; }
        public float MaxTotalYawCorrectionDegrees { get; }
        public float MaximumYawRateDegreesPerSecond { get; }
        public BTSMTL.Timeline.MotionWarpLimitPolicy LimitPolicy { get; }
        public AnimationCurve PositionProgressCurve { get; }
        public AnimationCurve YawProgressCurve { get; }
    }

    public sealed class AgentDeleteTimelineClipMutation : AgentTimelineClipMutation
    {
        public AgentDeleteTimelineClipMutation(string id, string path, AgentTimelineTargetReference target)
            : base(id, AgentMutationKind.DeleteTimelineClip, "delete_timeline_clip", AgentMutationOutputKind.None, path, target) { }
    }

    public sealed class AgentMoveTimelineClipMutation : AgentTimelineClipMutation
    {
        public AgentMoveTimelineClipMutation(string id, string path, AgentTimelineTargetReference target, int frameOffset)
            : base(id, AgentMutationKind.MoveTimelineClip, "move_timeline_clip", AgentMutationOutputKind.None, path, target)
        {
            FrameOffset = frameOffset;
        }

        public int FrameOffset { get; }
    }

    public sealed class AgentConfigureTimelineClipEaseMutation : AgentTimelineClipMutation
    {
        public AgentConfigureTimelineClipEaseMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            int selfEaseInFrame,
            int selfEaseOutFrame)
            : base(id, AgentMutationKind.ConfigureTimelineClipEase, "configure_timeline_clip_ease", AgentMutationOutputKind.None, path, target)
        {
            SelfEaseInFrame = selfEaseInFrame;
            SelfEaseOutFrame = selfEaseOutFrame;
        }

        public int SelfEaseInFrame { get; }
        public int SelfEaseOutFrame { get; }
    }

    public sealed class AgentConfigureTimelineCurveChannelMutation : AgentTimelineClipMutation
    {
        public AgentConfigureTimelineCurveChannelMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.TimelineCurveChannelId channelId,
            AnimationCurve curve)
            : base(
                id,
                AgentMutationKind.ConfigureTimelineCurveChannel,
                "configure_timeline_curve_channel",
                AgentMutationOutputKind.None,
                path,
                target)
        {
            ChannelId = channelId;
            Curve = curve;
        }

        public BTSMTL.Timeline.TimelineCurveChannelId ChannelId { get; }
        public AnimationCurve Curve { get; }
    }

    public abstract class AgentAnimationTrackMutation : AgentMutation
    {
        protected AgentAnimationTrackMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            AgentMutationOutputKind outputKind,
            string path,
            AgentTimelineTargetReference target)
            : base(id, kind, operationName, outputKind, path, target.TimelineAuthoringId, Vector2.zero)
        {
            Target = target;
        }

        public AgentTimelineTargetReference Target { get; }
    }

    public sealed class AgentConfigureAnimationTrackChannelMutation : AgentAnimationTrackMutation
    {
        public AgentConfigureAnimationTrackChannelMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            AnimationChannelId animationChannelId)
            : base(id, AgentMutationKind.ConfigureAnimationTrackChannel, "configure_animation_track_channel", AgentMutationOutputKind.None, path, target)
        {
            AnimationChannelId = animationChannelId;
        }

        public AnimationChannelId AnimationChannelId { get; }
    }

    public sealed class AgentConfigureAnimationTrackMarkerSyncMutation : AgentAnimationTrackMutation
    {
        public AgentConfigureAnimationTrackMarkerSyncMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            BTSMTL.Timeline.AnimationSyncMode mode,
            string syncGroupId,
            BTSMTL.Timeline.AnimationMarkerSequenceTopology topology,
            BTSMTL.Timeline.AnimationMarkerSyncRole syncRole)
            : base(id, AgentMutationKind.ConfigureAnimationTrackMarkerSync, "configure_animation_track_marker_sync", AgentMutationOutputKind.None, path, target)
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

    public sealed class AgentEnsureAnimationSyncMarkerMutation : AgentAnimationTrackMutation
    {
        public AgentEnsureAnimationSyncMarkerMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            string markerAuthoringId,
            string markerId,
            int frame)
            : base(id, AgentMutationKind.EnsureAnimationSyncMarker, "ensure_animation_sync_marker", AgentMutationOutputKind.TimelineMarker, path, target)
        {
            MarkerAuthoringId = markerAuthoringId ?? string.Empty;
            MarkerId = markerId ?? string.Empty;
            Frame = frame;
        }

        public string MarkerAuthoringId { get; }
        public string MarkerId { get; }
        public int Frame { get; }
    }

    public abstract class AgentAnimationMarkerMutation : AgentAnimationTrackMutation
    {
        protected AgentAnimationMarkerMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker)
            : base(id, kind, operationName, AgentMutationOutputKind.None, path, target)
        {
            Marker = marker;
        }

        public AgentAuthoringReference Marker { get; }
    }

    public sealed class AgentMoveAnimationSyncMarkerMutation : AgentAnimationMarkerMutation
    {
        public AgentMoveAnimationSyncMarkerMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker,
            int frame)
            : base(id, AgentMutationKind.MoveAnimationSyncMarker, "move_animation_sync_marker", path, target, marker)
        {
            Frame = frame;
        }

        public int Frame { get; }
    }

    public sealed class AgentDeleteAnimationSyncMarkerMutation : AgentAnimationMarkerMutation
    {
        public AgentDeleteAnimationSyncMarkerMutation(
            string id,
            string path,
            AgentTimelineTargetReference target,
            AgentAuthoringReference marker)
            : base(id, AgentMutationKind.DeleteAnimationSyncMarker, "delete_animation_sync_marker", path, target, marker)
        {
        }
    }

    public sealed class AgentEnsureTreeClipBlackboardWriteMutation : AgentTimelineClipMutation
    {
        public AgentEnsureTreeClipBlackboardWriteMutation(string id, string path, AgentTimelineTargetReference target, AgentAuthoringReference declaration)
            : base(id, AgentMutationKind.EnsureTreeClipBlackboardWrite, "ensure_tree_clip_blackboard_write", AgentMutationOutputKind.None, path, target)
        {
            Declaration = declaration;
        }

        public AgentAuthoringReference Declaration { get; }
    }

    public sealed class AgentDeleteTransitionMutation : AgentMutation
    {
        public AgentDeleteTransitionMutation(string id, string path, AgentStateMachineTargetReference stateMachine, string edgeAuthoringId)
            : base(id, AgentMutationKind.DeleteTransition, "delete_transition", AgentMutationOutputKind.None, path, stateMachine.Identity, Vector2.zero)
        {
            StateMachine = stateMachine;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public AgentStateMachineTargetReference StateMachine { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentEnsureGameplayTagMutation : AgentMutation
    {
        public AgentEnsureGameplayTagMutation(string id, string path, string tag, string parentTag, string displayName, string debugCategory)
            : base(id, AgentMutationKind.EnsureGameplayTag, "ensure_gameplay_tag", AgentMutationOutputKind.None, path, "GameplayTagCatalog", Vector2.zero)
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

    public abstract class AgentActionProfileAdmissionMutation : AgentMutation
    {
        protected AgentActionProfileAdmissionMutation(string id, AgentMutationKind kind, string operationName, string path, AgentAssetReference actionProfile)
            : base(id, kind, operationName, AgentMutationOutputKind.None, path, actionProfile.LogicalId, Vector2.zero)
        {
            ActionProfile = actionProfile;
        }

        public AgentAssetReference ActionProfile { get; }
    }

    public sealed class AgentSetActionProfileGrantedTagsMutation : AgentActionProfileAdmissionMutation
    {
        readonly ReadOnlyCollection<GameplayTagId> m_Tags;

        public AgentSetActionProfileGrantedTagsMutation(string id, string path, AgentAssetReference actionProfile, IList<GameplayTagId> tags)
            : base(id, AgentMutationKind.SetActionProfileGrantedTags, "set_action_profile_granted_tags", path, actionProfile)
        {
            m_Tags = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(tags));
        }

        public IReadOnlyList<GameplayTagId> Tags => m_Tags;
    }

    public sealed class AgentSetActionProfileCancelQueryMutation : AgentActionProfileAdmissionMutation
    {
        readonly ReadOnlyCollection<GameplayTagId> m_All;
        readonly ReadOnlyCollection<GameplayTagId> m_Any;
        readonly ReadOnlyCollection<GameplayTagId> m_None;

        public AgentSetActionProfileCancelQueryMutation(string id, string path, AgentAssetReference actionProfile, IList<GameplayTagId> all, IList<GameplayTagId> any, IList<GameplayTagId> none)
            : base(id, AgentMutationKind.SetActionProfileCancelQuery, "set_action_profile_cancel_query", path, actionProfile)
        {
            m_All = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(all));
            m_Any = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(any));
            m_None = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(none));
        }

        public IReadOnlyList<GameplayTagId> All => m_All;
        public IReadOnlyList<GameplayTagId> Any => m_Any;
        public IReadOnlyList<GameplayTagId> None => m_None;
    }

    public sealed class AgentSetActionProfileTargetRequirementMutation : AgentActionProfileAdmissionMutation
    {
        public AgentSetActionProfileTargetRequirementMutation(
            string id,
            string path,
            AgentAssetReference actionProfile,
            ActionTargetRequirement targetRequirement)
            : base(id, AgentMutationKind.SetActionProfileTargetRequirement, "set_action_profile_target_requirement", path, actionProfile)
        {
            TargetRequirement = targetRequirement;
        }

        public ActionTargetRequirement TargetRequirement { get; }
    }

    public sealed class AgentSetActionRequestTimingClassMutation : AgentMutation
    {
        public AgentSetActionRequestTimingClassMutation(
            string id,
            string path,
            string requestId,
            CharacterActionRequestTimingClass timingClass)
            : base(
                id,
                AgentMutationKind.SetActionRequestTimingClass,
                "set_action_request_timing_class",
                AgentMutationOutputKind.None,
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

    public abstract class AgentGraphLinkMutation : AgentMutation
    {
        protected AgentGraphLinkMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            AgentMutationOutputKind outputKind,
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

    public sealed class AgentDeleteFlowEdgeMutation : AgentMutation
    {
        public AgentDeleteFlowEdgeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string edgeAuthoringId)
            : base(id, AgentMutationKind.DeleteFlowEdge, "delete_flow_edge", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentEnsureGraphNodeMutation : AgentMutation
    {
        public AgentEnsureGraphNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existing,
            string nodeType,
            string displayName,
            LoopNode.StopType loopStopType,
            CompareNode.CompareType compareType,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureGraphNode, "ensure_graph_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            Existing = existing;
            NodeType = nodeType;
            DisplayName = displayName;
            LoopStopType = loopStopType;
            CompareType = compareType;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference Existing { get; }
        public string NodeType { get; }
        public string DisplayName { get; }
        public LoopNode.StopType LoopStopType { get; }
        public CompareNode.CompareType CompareType { get; }
    }

    public sealed class AgentDeleteGraphNodeMutation : AgentMutation
    {
        public AgentDeleteGraphNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference element)
            : base(id, AgentMutationKind.DeleteGraphNode, "delete_graph_node", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            Element = element;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference Element { get; }
    }

    public sealed class AgentDeletePropertyEdgeMutation : AgentMutation
    {
        public AgentDeletePropertyEdgeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            string edgeAuthoringId)
            : base(id, AgentMutationKind.DeletePropertyEdge, "delete_property_edge", AgentMutationOutputKind.None, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public AgentGraphTargetReference Graph { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentLinkFlowMutation : AgentGraphLinkMutation
    {
        public AgentLinkFlowMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference source,
            AgentElementTargetReference target,
            string startPort,
            string endPort,
            string edgeAuthoringId)
            : base(id, AgentMutationKind.LinkFlow, "link_flow", AgentMutationOutputKind.FlowEdge, path, graph, source, target)
        {
            StartPort = startPort;
            EndPort = endPort;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public string StartPort { get; }
        public string EndPort { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentLinkPropertyMutation : AgentGraphLinkMutation
    {
        public AgentLinkPropertyMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference source,
            AgentElementTargetReference target,
            string startPropertyPort,
            string endPropertyPort,
            string edgeAuthoringId)
            : base(id, AgentMutationKind.LinkProperty, "link_property", AgentMutationOutputKind.PropertyEdge, path, graph, source, target)
        {
            StartPropertyPort = startPropertyPort;
            EndPropertyPort = endPropertyPort;
            EdgeAuthoringId = edgeAuthoringId ?? string.Empty;
        }

        public string StartPropertyPort { get; }
        public string EndPropertyPort { get; }
        public string EdgeAuthoringId { get; }
    }

    public sealed class AgentEnsureBTConditionRuleMutation : AgentMutation
    {
        readonly ReadOnlyCollection<AgentConditionGroupMutation> m_Groups;

        public AgentEnsureBTConditionRuleMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentFlowEdgeTargetReference edge,
            BTAbortPolicy abortPolicy,
            IList<AgentConditionGroupMutation> groups)
            : base(id, AgentMutationKind.EnsureBTConditionRule, "ensure_bt_condition_rule", AgentMutationOutputKind.FlowEdge, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            Edge = edge;
            AbortPolicy = abortPolicy;
            m_Groups = new ReadOnlyCollection<AgentConditionGroupMutation>(new List<AgentConditionGroupMutation>(groups));
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentFlowEdgeTargetReference Edge { get; }
        public BTAbortPolicy AbortPolicy { get; }
        public IReadOnlyList<AgentConditionGroupMutation> Groups => m_Groups;
    }

    public sealed class AgentEnsureAIControllerDefinitionMutation : AgentMutation
    {
        public AgentEnsureAIControllerDefinitionMutation(string id, string path, string controllerId)
            : base(id, AgentMutationKind.EnsureAIControllerDefinition, "ensure_ai_controller_definition", AgentMutationOutputKind.None, path, controllerId, Vector2.zero)
        {
            ControllerId = controllerId;
        }

        public string ControllerId { get; }
    }

    public sealed class AgentEnsureAIControllerTreeMutation : AgentMutation
    {
        public AgentEnsureAIControllerTreeMutation(string id, string path, string treeAssetPath)
            : base(id, AgentMutationKind.EnsureAIControllerTree, "ensure_ai_controller_tree", AgentMutationOutputKind.None, path, treeAssetPath, Vector2.zero)
        {
            TreeAssetPath = treeAssetPath;
        }

        public string TreeAssetPath { get; }
    }

    public sealed class AgentBindAIControllerAssetsMutation : AgentMutation
    {
        public AgentBindAIControllerAssetsMutation(
            string id,
            string path,
            AgentAssetReference controlledCharacter,
            AgentAssetReference perceptionProfile)
            : base(id, AgentMutationKind.BindAIControllerAssets, "bind_ai_controller_assets", AgentMutationOutputKind.None, path, string.Empty, Vector2.zero)
        {
            ControlledCharacter = controlledCharacter;
            PerceptionProfile = perceptionProfile;
        }

        public AgentAssetReference ControlledCharacter { get; }
        public AgentAssetReference PerceptionProfile { get; }
    }

    public sealed class AgentConfigureAICandidatesMutation : AgentMutation
    {
        readonly ReadOnlyCollection<string> m_CandidateActorIds;

        public AgentConfigureAICandidatesMutation(
            string id,
            string path,
            AICandidateOrdering ordering,
            IList<string> candidateActorIds)
            : base(id, AgentMutationKind.ConfigureAICandidates, "configure_ai_candidates", AgentMutationOutputKind.None, path, string.Empty, Vector2.zero)
        {
            Ordering = ordering;
            m_CandidateActorIds = new ReadOnlyCollection<string>(new List<string>(candidateActorIds));
        }

        public AICandidateOrdering Ordering { get; }
        public IReadOnlyList<string> CandidateActorIds => m_CandidateActorIds;
    }

    public sealed class AgentEnsureAIBlackboardDeclarationMutation : AgentMutation
    {
        public AgentEnsureAIBlackboardDeclarationMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existingDeclaration,
            string key,
            Type valueType,
            PipelineBlackboardVariableScope scope,
            object defaultValue)
            : base(id, AgentMutationKind.EnsureAIBlackboardDeclaration, "ensure_ai_blackboard_declaration", AgentMutationOutputKind.BlackboardDeclaration, path, graph.Identity, Vector2.zero)
        {
            Graph = graph;
            ExistingDeclaration = existingDeclaration;
            Key = key;
            ValueType = valueType;
            Scope = scope;
            DefaultValue = defaultValue;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingDeclaration { get; }
        public string Key { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope Scope { get; }
        public object DefaultValue { get; }
    }

    public sealed class AgentEnsureAIObservationNodeMutation : AgentMutation
    {
        public AgentEnsureAIObservationNodeMutation(string id, string path, AgentGraphTargetReference graph, AgentElementTargetReference existingNode, AgentAIObservationNodeKind nodeKind, Vector2 position)
            : base(id, AgentMutationKind.EnsureAIObservationNode, "ensure_ai_observation_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            NodeKind = nodeKind;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public AgentAIObservationNodeKind NodeKind { get; }
    }

    public sealed class AgentEnsureAISharedNodeMutation : AgentMutation
    {
        public AgentEnsureAISharedNodeMutation(
            string id,
            string path,
            AgentGraphTargetReference graph,
            AgentElementTargetReference existingNode,
            AgentAISharedNodeKind nodeKind,
            LoopNode.StopType loopStopType,
            CompareNode.CompareType compareType,
            Vector2 position)
            : base(id, AgentMutationKind.EnsureAISharedNode, "ensure_ai_shared_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            NodeKind = nodeKind;
            LoopStopType = loopStopType;
            CompareType = compareType;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public AgentAISharedNodeKind NodeKind { get; }
        public LoopNode.StopType LoopStopType { get; }
        public CompareNode.CompareType CompareType { get; }
    }

    public sealed class AgentEnsureAIMemoryNodeMutation : AgentMutation
    {
        public AgentEnsureAIMemoryNodeMutation(string id, string path, AgentGraphTargetReference graph, AgentElementTargetReference existingNode, AgentAuthoringReference declaration, AgentAIMemoryNodeKind nodeKind, AIMemoryValueKind valueKind, Vector2 position)
            : base(id, AgentMutationKind.EnsureAIMemoryNode, "ensure_ai_memory_node", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            Declaration = declaration;
            NodeKind = nodeKind;
            ValueKind = valueKind;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public AgentAuthoringReference Declaration { get; }
        public AgentAIMemoryNodeKind NodeKind { get; }
        public AIMemoryValueKind ValueKind { get; }
    }

    public sealed class AgentEnsureAIContinuousInputMutation : AgentMutation
    {
        public AgentEnsureAIContinuousInputMutation(string id, string path, AgentGraphTargetReference graph, AgentElementTargetReference existingNode, string inputId, Vector2 position)
            : base(id, AgentMutationKind.EnsureAIContinuousInput, "ensure_ai_continuous_input", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            InputId = inputId;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public string InputId { get; }
    }

    public sealed class AgentEnsureAIActionTargetMutation : AgentMutation
    {
        public AgentEnsureAIActionTargetMutation(string id, string path, AgentGraphTargetReference graph, AgentElementTargetReference existingNode, string inputId, Vector2 position)
            : base(id, AgentMutationKind.EnsureAIActionTarget, "ensure_ai_action_target", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            InputId = inputId;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public string InputId { get; }
    }

    public sealed class AgentEnsureAIActionRequestMutation : AgentMutation
    {
        public AgentEnsureAIActionRequestMutation(string id, string path, AgentGraphTargetReference graph, AgentElementTargetReference existingNode, string requestId, float bufferSeconds, int priority, AIRequestRepeatPolicy repeatPolicy, Vector2 position)
            : base(id, AgentMutationKind.EnsureAIActionRequest, "ensure_ai_action_request", AgentMutationOutputKind.Node, path, graph.Identity, position)
        {
            Graph = graph;
            ExistingNode = existingNode;
            RequestId = requestId;
            BufferSeconds = bufferSeconds;
            Priority = priority;
            RepeatPolicy = repeatPolicy;
        }

        public AgentGraphTargetReference Graph { get; }
        public AgentElementTargetReference ExistingNode { get; }
        public string RequestId { get; }
        public float BufferSeconds { get; }
        public int Priority { get; }
        public AIRequestRepeatPolicy RepeatPolicy { get; }
    }

    public readonly struct AgentPlannedIdentitySymbol
    {
        public AgentPlannedIdentitySymbol(string identity, AgentMutationOutputKind kind, string ownerScope)
        {
            Identity = identity ?? string.Empty;
            Kind = kind;
            OwnerScope = ownerScope ?? string.Empty;
        }

        public string Identity { get; }
        public AgentMutationOutputKind Kind { get; }
        public string OwnerScope { get; }
    }

    public sealed class AgentMutationPlan
    {
        readonly ReadOnlyCollection<AgentMutation> m_Commands;
        public AgentMutationPlan(
            IList<AgentMutation> commands,
            string domain,
            string rootIdentity,
            string sourceRevision)
        {
            m_Commands = new ReadOnlyCollection<AgentMutation>(new List<AgentMutation>(commands));
            Domain = domain;
            RootIdentity = rootIdentity;
            SourceRevision = sourceRevision;
        }

        public IReadOnlyList<AgentMutation> Commands => m_Commands;
        public string Domain { get; }
        public string RootIdentity { get; }
        public string SourceRevision { get; }
    }
}
