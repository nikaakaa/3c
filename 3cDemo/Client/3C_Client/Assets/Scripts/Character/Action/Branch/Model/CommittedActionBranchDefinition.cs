using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;

namespace ThirdPersonAction
{
    public readonly struct CommittedActionBranchId : IEquatable<CommittedActionBranchId>
    {
        readonly string value;

        public CommittedActionBranchId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CommittedActionBranchId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CommittedActionBranchId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public readonly struct CommittedActionNodeId : IEquatable<CommittedActionNodeId>
    {
        readonly string value;

        public CommittedActionNodeId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CommittedActionNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CommittedActionNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public enum CommittedActionNodeKind
    {
        None = 0,
        Timeline = 1,
        Selector = 2,
        Condition = 3,
        Root = 4
    }

    public enum CommittedActionConditionKind
    {
        None = 0,
        ActionVariantEquals = 1,
        HasMoveIntent = 2,
        Always = 3,
        RequestHeld = 4,
        RequestReleased = 5,
        RequiredFactActive = 6,
        TimelineComplete = 7
    }

    public readonly struct CommittedActionConditionDefinition
    {
        public CommittedActionConditionDefinition(
            CommittedActionConditionKind kind,
            CharacterStateVariant expectedVariant,
            bool expectedBool)
            : this(kind, expectedVariant, expectedBool, default, string.Empty)
        {
        }

        public CommittedActionConditionDefinition(
            CommittedActionConditionKind kind,
            CharacterStateVariant expectedVariant,
            bool expectedBool,
            InputRequestKind requestKind,
            string requiredFactId)
        {
            Kind = kind;
            ExpectedVariant = expectedVariant;
            ExpectedBool = expectedBool;
            RequestKind = requestKind;
            RequiredFactId = new TimelineFactId(requiredFactId);
        }

        public CommittedActionConditionKind Kind { get; }
        public CharacterStateVariant ExpectedVariant { get; }
        public bool ExpectedBool { get; }
        public InputRequestKind RequestKind { get; }
        public TimelineFactId RequiredFactId { get; }
        public bool IsDefined => Kind != CommittedActionConditionKind.None;

        public static CommittedActionConditionDefinition Always()
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.Always,
                CharacterStateVariant.None,
                true);
        }

        public static CommittedActionConditionDefinition RequestHeld(InputRequestKind requestKind)
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.RequestHeld,
                CharacterStateVariant.None,
                true,
                requestKind,
                string.Empty);
        }

        public static CommittedActionConditionDefinition RequestReleased(InputRequestKind requestKind)
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.RequestReleased,
                CharacterStateVariant.None,
                true,
                requestKind,
                string.Empty);
        }

        public static CommittedActionConditionDefinition RequiredFactActive(string factId)
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.RequiredFactActive,
                CharacterStateVariant.None,
                true,
                default,
                factId);
        }

        public static CommittedActionConditionDefinition TimelineComplete()
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.TimelineComplete,
                CharacterStateVariant.None,
                true);
        }

        public static CommittedActionConditionDefinition ActionVariant(CharacterStateVariant variant)
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.ActionVariantEquals,
                variant,
                false);
        }

        public static CommittedActionConditionDefinition HasMoveIntent(bool expected)
        {
            return new CommittedActionConditionDefinition(
                CommittedActionConditionKind.HasMoveIntent,
                CharacterStateVariant.None,
                expected);
        }

        public static CommittedActionConditionDefinition Empty => default;
    }

    public readonly struct CommittedActionTimelineNodeDefinition
    {
        public CommittedActionTimelineNodeDefinition(ActionTimelineDefinition timeline)
        {
            Timeline = timeline ?? ActionTimelineDefinition.Empty;
        }

        public ActionTimelineDefinition Timeline { get; }
        public bool IsDefined => Timeline != null && Timeline.IsDefined;

        public static CommittedActionTimelineNodeDefinition Empty =>
            new CommittedActionTimelineNodeDefinition(ActionTimelineDefinition.Empty);
    }

    public readonly struct CommittedActionNodeDefinition
    {
        readonly CommittedActionNodeId[] childIds;

        public CommittedActionNodeDefinition(
            CommittedActionNodeId nodeId,
            CommittedActionNodeKind kind,
            CommittedActionTimelineNodeDefinition timelineNode)
            : this(
                nodeId,
                kind,
                timelineNode,
                CommittedActionConditionDefinition.Empty,
                Array.Empty<CommittedActionNodeId>())
        {
        }

        public CommittedActionNodeDefinition(
            CommittedActionNodeId nodeId,
            CommittedActionNodeKind kind,
            CommittedActionTimelineNodeDefinition timelineNode,
            CommittedActionConditionDefinition condition,
            CommittedActionNodeId[] childIds)
        {
            NodeId = nodeId;
            Kind = nodeId.IsValid ? kind : CommittedActionNodeKind.None;
            TimelineNode = timelineNode;
            Condition = condition;
            this.childIds = childIds ?? Array.Empty<CommittedActionNodeId>();
        }

        public CommittedActionNodeId NodeId { get; }
        public CommittedActionNodeKind Kind { get; }
        public CommittedActionTimelineNodeDefinition TimelineNode { get; }
        public CommittedActionConditionDefinition Condition { get; }
        public IReadOnlyList<CommittedActionNodeId> ChildIds => childIds ?? Array.Empty<CommittedActionNodeId>();
        public bool IsDefined => NodeId.IsValid && Kind != CommittedActionNodeKind.None;

        public static CommittedActionNodeDefinition Timeline(string nodeId, ActionTimelineDefinition timeline)
        {
            return new CommittedActionNodeDefinition(
                new CommittedActionNodeId(nodeId),
                CommittedActionNodeKind.Timeline,
                new CommittedActionTimelineNodeDefinition(timeline));
        }

        public static CommittedActionNodeDefinition Timeline(
            string nodeId,
            ActionTimelineDefinition timeline,
            params CommittedActionNodeId[] childIds)
        {
            return new CommittedActionNodeDefinition(
                new CommittedActionNodeId(nodeId),
                CommittedActionNodeKind.Timeline,
                new CommittedActionTimelineNodeDefinition(timeline),
                CommittedActionConditionDefinition.Empty,
                childIds);
        }

        public static CommittedActionNodeDefinition Selector(string nodeId, params CommittedActionNodeId[] childIds)
        {
            return new CommittedActionNodeDefinition(
                new CommittedActionNodeId(nodeId),
                CommittedActionNodeKind.Selector,
                CommittedActionTimelineNodeDefinition.Empty,
                CommittedActionConditionDefinition.Empty,
                childIds);
        }

        public static CommittedActionNodeDefinition Root(string nodeId, CommittedActionNodeId childId)
        {
            return new CommittedActionNodeDefinition(
                new CommittedActionNodeId(nodeId),
                CommittedActionNodeKind.Root,
                CommittedActionTimelineNodeDefinition.Empty,
                CommittedActionConditionDefinition.Empty,
                childId.IsValid ? new[] { childId } : Array.Empty<CommittedActionNodeId>());
        }

        public static CommittedActionNodeDefinition ConditionNode(
            string nodeId,
            CommittedActionConditionDefinition condition,
            params CommittedActionNodeId[] childIds)
        {
            return new CommittedActionNodeDefinition(
                new CommittedActionNodeId(nodeId),
                CommittedActionNodeKind.Condition,
                CommittedActionTimelineNodeDefinition.Empty,
                condition,
                childIds);
        }

        public static CommittedActionNodeDefinition Empty => default;
    }

    public readonly struct CommittedActionBranchDefinition
    {
        readonly CommittedActionNodeDefinition[] nodes;

        public CommittedActionBranchDefinition(
            CommittedActionBranchId branchId,
            ActionStateId actionState,
            CommittedActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim,
            bool enabled = true)
            : this(branchId, actionState, rootNode, defaultBodyClaim, Array.Empty<CommittedActionNodeDefinition>(), enabled)
        {
        }

        public CommittedActionBranchDefinition(
            CommittedActionBranchId branchId,
            ActionStateId actionState,
            CommittedActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim,
            CommittedActionNodeDefinition[] nodes,
            bool enabled = true)
        {
            BranchId = branchId;
            ActionState = actionState.IsValid ? actionState : ActionStateIds.None;
            RootNode = rootNode;
            DefaultBodyClaim = defaultBodyClaim;
            this.nodes = nodes ?? Array.Empty<CommittedActionNodeDefinition>();
            Enabled = enabled;
        }

        public CommittedActionBranchId BranchId { get; }
        public ActionStateId ActionState { get; }
        public CommittedActionNodeDefinition RootNode { get; }
        public IReadOnlyList<CommittedActionNodeDefinition> Nodes => nodes ?? Array.Empty<CommittedActionNodeDefinition>();
        public BodyOccupancyClaim DefaultBodyClaim { get; }
        public bool Enabled { get; }
        public bool IsDefined => BranchId.IsValid && ActionState.IsValid && ActionState != ActionStateIds.None;
        public bool CanEvaluate => IsDefined && Enabled && RootNode.IsDefined;

        public static CommittedActionBranchDefinition Empty =>
            new CommittedActionBranchDefinition(
                default,
                ActionStateIds.None,
                CommittedActionNodeDefinition.Empty,
                BodyOccupancyClaim.None(),
                false);

        public static CommittedActionBranchDefinition Define(
            string branchId,
            ActionStateId actionState,
            CommittedActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim)
        {
            return new CommittedActionBranchDefinition(
                new CommittedActionBranchId(branchId),
                actionState,
                rootNode,
                defaultBodyClaim);
        }

        public static CommittedActionBranchDefinition Define(
            string branchId,
            ActionStateId actionState,
            CommittedActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim,
            CommittedActionNodeDefinition[] nodes)
        {
            return new CommittedActionBranchDefinition(
                new CommittedActionBranchId(branchId),
                actionState,
                rootNode,
                defaultBodyClaim,
                nodes);
        }

        public bool TryGetNode(CommittedActionNodeId nodeId, out CommittedActionNodeDefinition node)
        {
            if (RootNode.NodeId.Equals(nodeId))
            {
                node = RootNode;
                return RootNode.IsDefined;
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                CommittedActionNodeDefinition candidate = Nodes[i];
                if (candidate.NodeId.Equals(nodeId))
                {
                    node = candidate;
                    return candidate.IsDefined;
                }
            }

            node = CommittedActionNodeDefinition.Empty;
            return false;
        }
    }
}
