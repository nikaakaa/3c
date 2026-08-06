using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterLinkedPoseDiagnosticCode
    {
        MissingSelector = 1001,
        DuplicateSelector = 1002,
        MissingMapping = 1003,
        DuplicateCall = 1004,
        MissingEntry = 1005,
        SignatureMismatch = 1101,
        FactContractMismatch = 1102,
        RigMismatch = 1103,
        RuntimeAbiMismatch = 1104,
        SourceClosureMissing = 1105,
        CompletionInvalid = 1106
    }

    public static class CharacterLinkedPoseDiagnostic
    {
        public static string Format(CharacterLinkedPoseDiagnosticCode code, string message) =>
            $"[LinkedPose:{(int)code}:{code}] {message}";
    }

    public readonly struct LinkedPoseInterfaceId : IEquatable<LinkedPoseInterfaceId>, IComparable<LinkedPoseInterfaceId>
    {
        public LinkedPoseInterfaceId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(LinkedPoseInterfaceId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(LinkedPoseInterfaceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LinkedPoseInterfaceId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(LinkedPoseInterfaceId left, LinkedPoseInterfaceId right) => left.Equals(right);
        public static bool operator !=(LinkedPoseInterfaceId left, LinkedPoseInterfaceId right) => !left.Equals(right);
    }

    public readonly struct LinkedPoseEntryId : IEquatable<LinkedPoseEntryId>, IComparable<LinkedPoseEntryId>
    {
        public LinkedPoseEntryId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(LinkedPoseEntryId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(LinkedPoseEntryId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LinkedPoseEntryId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(LinkedPoseEntryId left, LinkedPoseEntryId right) => left.Equals(right);
        public static bool operator !=(LinkedPoseEntryId left, LinkedPoseEntryId right) => !left.Equals(right);
    }

    public readonly struct LinkedPoseImplementationId : IEquatable<LinkedPoseImplementationId>, IComparable<LinkedPoseImplementationId>
    {
        public LinkedPoseImplementationId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(LinkedPoseImplementationId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(LinkedPoseImplementationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LinkedPoseImplementationId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(LinkedPoseImplementationId left, LinkedPoseImplementationId right) => left.Equals(right);
        public static bool operator !=(LinkedPoseImplementationId left, LinkedPoseImplementationId right) => !left.Equals(right);
    }

    public readonly struct LinkedPoseGroupId : IEquatable<LinkedPoseGroupId>, IComparable<LinkedPoseGroupId>
    {
        public LinkedPoseGroupId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(LinkedPoseGroupId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(LinkedPoseGroupId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LinkedPoseGroupId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(LinkedPoseGroupId left, LinkedPoseGroupId right) => left.Equals(right);
        public static bool operator !=(LinkedPoseGroupId left, LinkedPoseGroupId right) => !left.Equals(right);
    }

    public readonly struct LinkedPoseRevision : IEquatable<LinkedPoseRevision>, IComparable<LinkedPoseRevision>
    {
        public LinkedPoseRevision(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public int CompareTo(LinkedPoseRevision other) => Value.CompareTo(other.Value);
        public bool Equals(LinkedPoseRevision other) => Value == other.Value;
        public override bool Equals(object obj) => obj is LinkedPoseRevision other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(LinkedPoseRevision left, LinkedPoseRevision right) => left.Equals(right);
        public static bool operator !=(LinkedPoseRevision left, LinkedPoseRevision right) => !left.Equals(right);
    }

    public readonly struct CharacterPresentationFactContractIdentity : IEquatable<CharacterPresentationFactContractIdentity>
    {
        public CharacterPresentationFactContractIdentity(StableHash value)
        {
            Value = value.IsValid ? value : throw new ArgumentException("Presentation Fact contract identity is invalid.", nameof(value));
        }

        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(CharacterPresentationFactContractIdentity other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterPresentationFactContractIdentity other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(CharacterPresentationFactContractIdentity left, CharacterPresentationFactContractIdentity right) => left.Equals(right);
        public static bool operator !=(CharacterPresentationFactContractIdentity left, CharacterPresentationFactContractIdentity right) => !left.Equals(right);
    }

    public static class CharacterPresentationFactContract
    {
        public static CharacterPresentationFactContractIdentity Current
        {
            get
            {
                IReadOnlyList<CharacterPresentationFactDeclaration> declarations = CharacterPresentationFactSchema.OrderedDeclarations;
                var values = new string[declarations.Count * 2 + 1];
                values[0] = CharacterPresentationFactSchema.Version;
                for (int i = 0; i < declarations.Count; i++)
                {
                    values[i * 2 + 1] = declarations[i].FactId.Value;
                    values[i * 2 + 2] = ((byte)declarations[i].ValueKind).ToString(CultureInfo.InvariantCulture);
                }
                return new CharacterPresentationFactContractIdentity(StableHash.Compute(values));
            }
        }
    }

    public static class CharacterLinkedPoseExecutionContract
    {
        public const string Current = "character-linked-pose-execution/v1";
    }

    [Serializable]
    public sealed class CharacterLinkedPoseInterfacePortDescriptor
    {
        [SerializeField] string m_PortId = string.Empty;
        [SerializeField] CharacterPosePortDirection m_Direction;
        [SerializeField] CharacterPosePortKind m_Kind;
        [SerializeField] CharacterPoseSpace m_Space;
        [SerializeField] bool m_Required = true;
        [SerializeField] int m_Order;

        public PoseInterfacePortId PortId => string.IsNullOrWhiteSpace(m_PortId) ? default : new PoseInterfacePortId(m_PortId);
        public CharacterPosePortDirection Direction => m_Direction;
        public CharacterPosePortKind Kind => m_Kind;
        public CharacterPoseSpace Space => m_Space;
        public bool Required => m_Required;
        public int Order => m_Order;

        public CharacterLinkedPoseInterfacePortDescriptor() { }

        public CharacterLinkedPoseInterfacePortDescriptor(
            PoseInterfacePortId portId,
            CharacterPosePortDirection direction,
            CharacterPosePortKind kind,
            CharacterPoseSpace space,
            bool required,
            int order)
        {
            m_PortId = portId.IsValid ? portId.Value : throw new ArgumentException("Linked Pose port identity is invalid.", nameof(portId));
            m_Direction = direction;
            m_Kind = kind;
            m_Space = space;
            m_Required = required;
            m_Order = order;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!PortId.IsValid || !Enum.IsDefined(typeof(CharacterPosePortDirection), Direction) ||
                !Enum.IsDefined(typeof(CharacterPosePortKind), Kind) || Order < 0)
            {
                throw new InvalidOperationException("Linked Pose interface port is invalid.");
            }
            CharacterPoseSpace expected = Kind == CharacterPosePortKind.LocalPose
                ? CharacterPoseSpace.Local
                : Kind == CharacterPosePortKind.ComponentPose || Kind == CharacterPosePortKind.FullBodyIkGoals
                    ? CharacterPoseSpace.Component
                    : CharacterPoseSpace.None;
            if (Space != expected)
                throw new InvalidOperationException($"Linked Pose interface port '{PortId}' has an invalid space.");
        }

        internal void AddSignatureParts(List<string> values)
        {
            values.Add(PortId.Value);
            values.Add(((byte)Direction).ToString(CultureInfo.InvariantCulture));
            values.Add(((byte)Kind).ToString(CultureInfo.InvariantCulture));
            values.Add(((byte)Space).ToString(CultureInfo.InvariantCulture));
            values.Add(Required ? "1" : "0");
            values.Add(Order.ToString(CultureInfo.InvariantCulture));
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseInterfaceEntryDescriptor
    {
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain = CharacterPoseExecutionDomain.PurePose;
        [SerializeField] CharacterLinkedPoseInterfacePortDescriptor[] m_Ports = Array.Empty<CharacterLinkedPoseInterfacePortDescriptor>();

        public LinkedPoseEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId) ? default : new LinkedPoseEntryId(m_EntryId);
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public IReadOnlyList<CharacterLinkedPoseInterfacePortDescriptor> Ports => m_Ports ?? Array.Empty<CharacterLinkedPoseInterfacePortDescriptor>();

        public CharacterLinkedPoseInterfaceEntryDescriptor() { }

        public CharacterLinkedPoseInterfaceEntryDescriptor(
            LinkedPoseEntryId entryId,
            CharacterPoseExecutionDomain executionDomain,
            CharacterLinkedPoseInterfacePortDescriptor[] ports)
        {
            m_EntryId = entryId.IsValid ? entryId.Value : throw new ArgumentException("Linked Pose Entry identity is invalid.", nameof(entryId));
            m_ExecutionDomain = executionDomain;
            m_Ports = ports ?? Array.Empty<CharacterLinkedPoseInterfacePortDescriptor>();
            RequireValid();
        }

        public void RequireValid()
        {
            if (!EntryId.IsValid || !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), ExecutionDomain) || Ports.Count == 0)
                throw new InvalidOperationException("Linked Pose Interface Entry is invalid.");
            var ids = new HashSet<PoseInterfacePortId>();
            for (int i = 0; i < Ports.Count; i++)
            {
                CharacterLinkedPoseInterfacePortDescriptor port = Ports[i];
                port?.RequireValid();
                if (port == null || port.Order != i || !ids.Add(port.PortId))
                    throw new InvalidOperationException($"Linked Pose Interface Entry '{EntryId}' ports are missing, duplicated or unordered.");
            }
        }

        internal void AddSignatureParts(List<string> values)
        {
            values.Add(EntryId.Value);
            values.Add(((byte)ExecutionDomain).ToString(CultureInfo.InvariantCulture));
            values.Add(Ports.Count.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < Ports.Count; i++)
                Ports[i].AddSignatureParts(values);
        }
    }

    public readonly struct CharacterLinkedPoseSelectionFrame
    {
        public CharacterLinkedPoseSelectionFrame(
            LinkedPoseGroupId groupId,
            LinkedPoseInterfaceId interfaceId,
            LinkedPoseImplementationId implementationId,
            LinkedPoseRevision selectionRevision)
        {
            if (!groupId.IsValid || !interfaceId.IsValid || !implementationId.IsValid || !selectionRevision.IsValid)
                throw new ArgumentException("Linked Pose selection frame is incomplete.");
            GroupId = groupId;
            InterfaceId = interfaceId;
            ImplementationId = implementationId;
            SelectionRevision = selectionRevision;
        }

        public LinkedPoseGroupId GroupId { get; }
        public LinkedPoseInterfaceId InterfaceId { get; }
        public LinkedPoseImplementationId ImplementationId { get; }
        public LinkedPoseRevision SelectionRevision { get; }
        public bool IsValid => GroupId.IsValid && InterfaceId.IsValid && ImplementationId.IsValid && SelectionRevision.IsValid;
    }
}
