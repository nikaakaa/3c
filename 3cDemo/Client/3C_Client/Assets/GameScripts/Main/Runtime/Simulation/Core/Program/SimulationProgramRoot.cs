using System;

namespace ThirdPersonSimulation
{
    public enum SimulationProgramRootKind : byte
    {
        Character = 1,
        Timeline = 2
    }

    public readonly struct SimulationProgramRootDescriptor : IEquatable<SimulationProgramRootDescriptor>
    {
        public SimulationProgramRootDescriptor(
            SimulationProgramRootKind kind,
            string rootIdentity,
            string entryIdentity,
            string contentIdentity)
        {
            if (!Enum.IsDefined(typeof(SimulationProgramRootKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            RootIdentity = SimulationIdentity.Require(rootIdentity, nameof(rootIdentity));
            EntryIdentity = SimulationIdentity.Require(entryIdentity, nameof(entryIdentity));
            ContentIdentity = SimulationIdentity.Require(contentIdentity, nameof(contentIdentity));
        }

        public SimulationProgramRootKind Kind { get; }
        public string RootIdentity { get; }
        public string EntryIdentity { get; }
        public string ContentIdentity { get; }
        public bool IsValid =>
            Enum.IsDefined(typeof(SimulationProgramRootKind), Kind) &&
            !string.IsNullOrEmpty(RootIdentity) &&
            !string.IsNullOrEmpty(EntryIdentity) &&
            !string.IsNullOrEmpty(ContentIdentity);
        public bool IsCharacter => Kind == SimulationProgramRootKind.Character;
        public bool IsTimeline => Kind == SimulationProgramRootKind.Timeline;

        public bool Equals(SimulationProgramRootDescriptor other) =>
            Kind == other.Kind &&
            string.Equals(RootIdentity, other.RootIdentity, StringComparison.Ordinal) &&
            string.Equals(EntryIdentity, other.EntryIdentity, StringComparison.Ordinal) &&
            string.Equals(ContentIdentity, other.ContentIdentity, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is SimulationProgramRootDescriptor other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Kind, RootIdentity, EntryIdentity, ContentIdentity);

        public override string ToString() =>
            IsValid
                ? $"{Kind}:{RootIdentity}:{EntryIdentity}:{ContentIdentity}"
                : "Invalid";

        public static bool operator ==(
            SimulationProgramRootDescriptor left,
            SimulationProgramRootDescriptor right) => left.Equals(right);

        public static bool operator !=(
            SimulationProgramRootDescriptor left,
            SimulationProgramRootDescriptor right) => !left.Equals(right);
    }

    public static class SimulationProgramRootDescriptorCodec
    {
        public static void Write(CanonicalWriter writer, SimulationProgramRootDescriptor root)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (!root.IsValid)
                throw new ArgumentException("Simulation Program root descriptor is invalid.", nameof(root));
            writer.WriteByte((byte)root.Kind);
            writer.WriteString(root.RootIdentity);
            writer.WriteString(root.EntryIdentity);
            writer.WriteString(root.ContentIdentity);
        }

        public static SimulationProgramRootDescriptor Read(CanonicalReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return new SimulationProgramRootDescriptor(
                (SimulationProgramRootKind)reader.ReadByte(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString());
        }
    }
}
