using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterGraph
{
    public enum CharacterExecutionNodeKind
    {
        None = 0,
        Root = 1,
        ParallelComposite = 2,
        Branch = 3,
        Timeline = 4
    }

    public readonly struct CharacterExecutionNodeId : IEquatable<CharacterExecutionNodeId>
    {
        readonly string value;

        public CharacterExecutionNodeId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CharacterExecutionNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterExecutionNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(CharacterExecutionNodeId left, CharacterExecutionNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterExecutionNodeId left, CharacterExecutionNodeId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CharacterExecutionNodeDefinition
    {
        readonly CharacterExecutionNodeId[] children;

        public CharacterExecutionNodeDefinition(
            CharacterExecutionNodeId id,
            CharacterExecutionNodeKind kind,
            CharacterGraphBranchKind branchKind,
            CharacterExecutionNodeId[] children)
        {
            Id = id;
            Kind = id.IsValid ? kind : CharacterExecutionNodeKind.None;
            BranchKind = branchKind;
            this.children = children ?? Array.Empty<CharacterExecutionNodeId>();
        }

        public CharacterExecutionNodeId Id { get; }
        public CharacterExecutionNodeKind Kind { get; }
        public CharacterGraphBranchKind BranchKind { get; }
        public IReadOnlyList<CharacterExecutionNodeId> Children => children ?? Array.Empty<CharacterExecutionNodeId>();
        public bool IsValid => Id.IsValid && Kind != CharacterExecutionNodeKind.None;
        public bool IsComposite => Kind == CharacterExecutionNodeKind.Root || Kind == CharacterExecutionNodeKind.ParallelComposite;

        public static CharacterExecutionNodeDefinition Root(string id, params CharacterExecutionNodeId[] children)
        {
            return new CharacterExecutionNodeDefinition(
                new CharacterExecutionNodeId(id),
                CharacterExecutionNodeKind.Root,
                CharacterGraphBranchKind.None,
                children);
        }

        public static CharacterExecutionNodeDefinition Parallel(string id, params CharacterExecutionNodeId[] children)
        {
            return new CharacterExecutionNodeDefinition(
                new CharacterExecutionNodeId(id),
                CharacterExecutionNodeKind.ParallelComposite,
                CharacterGraphBranchKind.None,
                children);
        }

        public static CharacterExecutionNodeDefinition Branch(
            string id,
            CharacterGraphBranchKind branchKind,
            params CharacterExecutionNodeId[] children)
        {
            return new CharacterExecutionNodeDefinition(
                new CharacterExecutionNodeId(id),
                CharacterExecutionNodeKind.Branch,
                branchKind,
                children);
        }
    }

    public sealed class CharacterExecutionNodeTree
    {
        readonly CharacterExecutionNodeDefinition[] nodes;

        public CharacterExecutionNodeTree(
            CharacterExecutionNodeId rootNodeId,
            CharacterExecutionNodeDefinition[] nodes)
        {
            RootNodeId = rootNodeId;
            this.nodes = nodes ?? Array.Empty<CharacterExecutionNodeDefinition>();
        }

        public CharacterExecutionNodeId RootNodeId { get; }
        public IReadOnlyList<CharacterExecutionNodeDefinition> Nodes => nodes ?? Array.Empty<CharacterExecutionNodeDefinition>();
        public bool IsDefined => RootNodeId.IsValid && Nodes.Count > 0;

        public bool TryGetNode(CharacterExecutionNodeId id, out CharacterExecutionNodeDefinition node)
        {
            if (id.IsValid && nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    CharacterExecutionNodeDefinition candidate = nodes[i];
                    if (candidate.Id == id)
                    {
                        node = candidate;
                        return candidate.IsValid;
                    }
                }
            }

            node = default;
            return false;
        }

        public static CharacterExecutionNodeTree Empty =>
            new CharacterExecutionNodeTree(default, Array.Empty<CharacterExecutionNodeDefinition>());
    }
}
