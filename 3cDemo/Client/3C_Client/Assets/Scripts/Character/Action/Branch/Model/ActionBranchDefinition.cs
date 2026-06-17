using System;

namespace ThirdPersonAction
{
    public readonly struct ActionBranchId : IEquatable<ActionBranchId>
    {
        readonly string value;

        public ActionBranchId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActionBranchId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionBranchId other && Equals(other);
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

    public readonly struct ActionNodeId : IEquatable<ActionNodeId>
    {
        readonly string value;

        public ActionNodeId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActionNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionNodeId other && Equals(other);
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

    public enum ActionNodeKind
    {
        None = 0,
        Timeline = 1
    }

    public readonly struct ActionTimelineNodeDefinition
    {
        public ActionTimelineNodeDefinition(ActionTimelineDefinition timeline)
        {
            Timeline = timeline ?? ActionTimelineDefinition.Empty;
        }

        public ActionTimelineDefinition Timeline { get; }
        public bool IsDefined => Timeline != null && Timeline.IsDefined;

        public static ActionTimelineNodeDefinition Empty =>
            new ActionTimelineNodeDefinition(ActionTimelineDefinition.Empty);
    }

    public readonly struct ActionNodeDefinition
    {
        public ActionNodeDefinition(
            ActionNodeId nodeId,
            ActionNodeKind kind,
            ActionTimelineNodeDefinition timelineNode)
        {
            NodeId = nodeId;
            Kind = nodeId.IsValid ? kind : ActionNodeKind.None;
            TimelineNode = timelineNode;
        }

        public ActionNodeId NodeId { get; }
        public ActionNodeKind Kind { get; }
        public ActionTimelineNodeDefinition TimelineNode { get; }
        public bool IsDefined => NodeId.IsValid && Kind != ActionNodeKind.None;

        public static ActionNodeDefinition Timeline(string nodeId, ActionTimelineDefinition timeline)
        {
            return new ActionNodeDefinition(
                new ActionNodeId(nodeId),
                ActionNodeKind.Timeline,
                new ActionTimelineNodeDefinition(timeline));
        }

        public static ActionNodeDefinition Empty => default;
    }

    public readonly struct ActionBranchDefinition
    {
        public ActionBranchDefinition(
            ActionBranchId branchId,
            ActionStateId actionState,
            ActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim,
            bool enabled = true)
        {
            BranchId = branchId;
            ActionState = actionState.IsValid ? actionState : ActionStateIds.None;
            RootNode = rootNode;
            DefaultBodyClaim = defaultBodyClaim;
            Enabled = enabled;
        }

        public ActionBranchId BranchId { get; }
        public ActionStateId ActionState { get; }
        public ActionNodeDefinition RootNode { get; }
        public BodyOccupancyClaim DefaultBodyClaim { get; }
        public bool Enabled { get; }
        public bool IsDefined => BranchId.IsValid && ActionState.IsValid && ActionState != ActionStateIds.None;
        public bool CanEvaluate => IsDefined && Enabled && RootNode.IsDefined;

        public static ActionBranchDefinition Empty =>
            new ActionBranchDefinition(
                default,
                ActionStateIds.None,
                ActionNodeDefinition.Empty,
                BodyOccupancyClaim.None(),
                false);

        public static ActionBranchDefinition Define(
            string branchId,
            ActionStateId actionState,
            ActionNodeDefinition rootNode,
            BodyOccupancyClaim defaultBodyClaim)
        {
            return new ActionBranchDefinition(
                new ActionBranchId(branchId),
                actionState,
                rootNode,
                defaultBodyClaim);
        }
    }
}
