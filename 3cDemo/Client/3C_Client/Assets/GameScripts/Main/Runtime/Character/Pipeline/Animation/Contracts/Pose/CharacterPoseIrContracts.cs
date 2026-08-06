using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterMotionMatchingPoseIrStage : byte
    {
        FrameContextResolve = 1,
        HistoryRead = 2,
        ChooserResolve = 3,
        Search = 4,
        EntrySourceCapture = 5,
        EntryProcessing = 6,
        InternalBlend = 7,
        HistoryCommit = 8
    }

    public readonly struct CharacterPoseIrNodeId : IEquatable<CharacterPoseIrNodeId>
    {
        public CharacterPoseIrNodeId(string value) => Value = PoseIdentity.Require(value, nameof(value));
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(CharacterPoseIrNodeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterPoseIrNodeId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct CharacterPoseIrLinkId : IEquatable<CharacterPoseIrLinkId>
    {
        public CharacterPoseIrLinkId(string value) => Value = PoseIdentity.Require(value, nameof(value));
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(CharacterPoseIrLinkId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterPoseIrLinkId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
    }

    public sealed class CharacterPoseIrInput
    {
        public CharacterPoseIrInput(CharacterPoseIrLinkId linkId, string targetPortId, CharacterPoseIrNodeId sourceNodeId, string sourcePortId, CharacterPosePortKind valueKind)
        {
            LinkId = linkId.IsValid ? linkId : throw new ArgumentException("Pose IR link identity is invalid.", nameof(linkId));
            TargetPortId = PoseIdentity.Require(targetPortId, nameof(targetPortId));
            SourceNodeId = sourceNodeId.IsValid ? sourceNodeId : throw new ArgumentException("Pose IR source node identity is invalid.", nameof(sourceNodeId));
            SourcePortId = PoseIdentity.Require(sourcePortId, nameof(sourcePortId));
            ValueKind = valueKind;
        }

        public CharacterPoseIrLinkId LinkId { get; }
        public string TargetPortId { get; }
        public CharacterPoseIrNodeId SourceNodeId { get; }
        public string SourcePortId { get; }
        public CharacterPosePortKind ValueKind { get; }
    }

    public sealed class CharacterPoseIrNode
    {
        public CharacterPoseIrNode(
            CharacterPoseIrNodeId nodeId,
            string capabilityIdentity,
            CharacterPoseNodePayload payload,
            IReadOnlyList<CharacterPoseIrInput> inputs,
            string sourcePath)
        {
            NodeId = nodeId.IsValid ? nodeId : throw new ArgumentException("Pose IR node identity is invalid.", nameof(nodeId));
            CapabilityIdentity = PoseIdentity.Require(capabilityIdentity, nameof(capabilityIdentity));
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Inputs = inputs ?? Array.Empty<CharacterPoseIrInput>();
            SourcePath = PoseIdentity.Require(sourcePath, nameof(sourcePath));
        }

        public CharacterPoseIrNodeId NodeId { get; }
        public string CapabilityIdentity { get; }
        public CharacterPoseNodePayload Payload { get; }
        public IReadOnlyList<CharacterPoseIrInput> Inputs { get; }
        public string SourcePath { get; }
    }

    public sealed class CharacterPoseIrGraph
    {
        public CharacterPoseIrGraph(PoseGraphId graphId, string sourceRevision, IReadOnlyList<CharacterPoseIrNode> nodes, CharacterPoseIrNodeId outputNodeId)
        {
            GraphId = graphId.IsValid ? graphId : throw new ArgumentException("Pose IR graph identity is invalid.", nameof(graphId));
            SourceRevision = PoseIdentity.Require(sourceRevision, nameof(sourceRevision));
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            OutputNodeId = outputNodeId.IsValid ? outputNodeId : throw new ArgumentException("Pose IR output node identity is invalid.", nameof(outputNodeId));
        }

        public PoseGraphId GraphId { get; }
        public string SourceRevision { get; }
        public IReadOnlyList<CharacterPoseIrNode> Nodes { get; }
        public CharacterPoseIrNodeId OutputNodeId { get; }
    }
}
