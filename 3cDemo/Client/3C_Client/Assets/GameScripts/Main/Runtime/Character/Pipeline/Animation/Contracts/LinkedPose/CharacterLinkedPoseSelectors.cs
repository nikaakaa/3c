using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct LinkedPoseSelectorId : IEquatable<LinkedPoseSelectorId>, IComparable<LinkedPoseSelectorId>
    {
        public LinkedPoseSelectorId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(LinkedPoseSelectorId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(LinkedPoseSelectorId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LinkedPoseSelectorId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(LinkedPoseSelectorId left, LinkedPoseSelectorId right) => left.Equals(right);
        public static bool operator !=(LinkedPoseSelectorId left, LinkedPoseSelectorId right) => !left.Equals(right);
    }

    [Serializable]
    public sealed class CharacterLinkedPoseCompiledSelectorDescriptor
    {
        [SerializeField] string m_SelectorId = string.Empty;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string[] m_CandidateImplementationIds = Array.Empty<string>();

        public LinkedPoseSelectorId SelectorId => string.IsNullOrWhiteSpace(m_SelectorId) ? default : new LinkedPoseSelectorId(m_SelectorId);
        public LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => string.IsNullOrWhiteSpace(m_InterfaceId) ? default : new LinkedPoseInterfaceId(m_InterfaceId);
        public IReadOnlyList<string> CandidateImplementationIds => m_CandidateImplementationIds ?? Array.Empty<string>();

        public CharacterLinkedPoseCompiledSelectorDescriptor() { }

        public CharacterLinkedPoseCompiledSelectorDescriptor(
            LinkedPoseSelectorId selectorId,
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            IEnumerable<LinkedPoseImplementationId> candidates)
        {
            m_SelectorId = selectorId.IsValid ? selectorId.Value : throw new ArgumentException("Linked Pose selector identity is invalid.", nameof(selectorId));
            m_GroupId = groupId.IsValid ? groupId.Value : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            m_InterfaceId = interfaceId.IsValid ? interfaceId.Value : throw new ArgumentException("Linked Pose Interface identity is invalid.", nameof(interfaceId));
            m_CandidateImplementationIds = (candidates ?? throw new ArgumentNullException(nameof(candidates)))
                .Select(value => value.IsValid ? value.Value : throw new ArgumentException("Linked Pose selector candidate is invalid.", nameof(candidates)))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            RequireValid();
        }

        public void RequireValid()
        {
            if (!SelectorId.IsValid || !GroupId.IsValid || !InterfaceId.IsValid || CandidateImplementationIds.Count == 0)
                throw new InvalidOperationException("Compiled Linked Pose selector descriptor is incomplete.");
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < CandidateImplementationIds.Count; i++)
            {
                string value = CandidateImplementationIds[i];
                if (string.IsNullOrWhiteSpace(value) || !candidates.Add(value))
                    throw new InvalidOperationException($"Compiled Linked Pose selector '{SelectorId}' has an invalid or duplicated candidate.");
                _ = new LinkedPoseImplementationId(value);
            }
        }

        public bool Contains(LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < CandidateImplementationIds.Count; i++)
            {
                if (string.Equals(CandidateImplementationIds[i], implementationId.Value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }

    public interface ICharacterLinkedPoseSelectorAuthoring
    {
        LinkedPoseSelectorId SelectorId { get; }
        LinkedPoseGroupId GroupId { get; }
        IReadOnlyList<LinkedPoseImplementationId> CandidateImplementationIds { get; }
        CharacterLinkedPoseCompiledSelectorDescriptor CompileCore(CharacterLinkedPoseGroupBinding group);
        void RequireValid(CharacterLinkedPoseGroupBinding group, IReadOnlyDictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset> implementations);
    }

    public interface ICharacterLinkedPoseRuntimeSelectorAdapter
    {
        LinkedPoseGroupId GroupId { get; }
        bool TryReadSelection(out CharacterLinkedPoseSelectionFrame frame);
        void Reset();
    }

    public abstract class CharacterLinkedPoseSelectorBindingAsset : ScriptableObject, ICharacterLinkedPoseSelectorAuthoring
    {
        public abstract LinkedPoseSelectorId SelectorId { get; }
        public abstract LinkedPoseGroupId GroupId { get; }
        public abstract IReadOnlyList<LinkedPoseImplementationId> CandidateImplementationIds { get; }
        public abstract CharacterLinkedPoseCompiledSelectorDescriptor CompileCore(CharacterLinkedPoseGroupBinding group);
        public abstract void RequireValid(CharacterLinkedPoseGroupBinding group, IReadOnlyDictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset> implementations);

        protected static void RequireCandidateClosure(
            CharacterLinkedPoseGroupBinding group,
            IReadOnlyList<LinkedPoseImplementationId> candidates,
            IReadOnlyDictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset> implementations)
        {
            group?.RequireValid();
            if (group == null || candidates == null || candidates.Count == 0 || implementations == null)
                throw new InvalidOperationException("Linked Pose selector candidate closure is incomplete.");
            var ids = new HashSet<LinkedPoseImplementationId>();
            for (int i = 0; i < candidates.Count; i++)
            {
                LinkedPoseImplementationId id = candidates[i];
                if (!id.IsValid || !ids.Add(id) || !implementations.TryGetValue(id, out CharacterLinkedPoseImplementationAsset implementation))
                    throw new InvalidOperationException($"Linked Pose selector candidate '{id}' is missing or duplicated.");
                implementation.RequireValid();
                if (implementation.Interface != group.Interface)
                    throw new InvalidOperationException($"Linked Pose selector candidate '{id}' implements a different Interface.");
            }
        }
    }
}
