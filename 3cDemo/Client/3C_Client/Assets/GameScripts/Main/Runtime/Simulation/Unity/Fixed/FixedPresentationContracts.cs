using System;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal readonly struct FixedPresentationStateKey :
        IEquatable<FixedPresentationStateKey>,
        IComparable<FixedPresentationStateKey>
    {
        public FixedPresentationStateKey(
            string channel,
            string producer,
            ulong generation)
        {
            Channel = channel ?? string.Empty;
            Producer = producer ?? string.Empty;
            Generation = generation;
        }

        public string Channel { get; }
        public string Producer { get; }
        public ulong Generation { get; }

        public bool Equals(FixedPresentationStateKey other) =>
            Generation == other.Generation &&
            string.Equals(Channel, other.Channel, StringComparison.Ordinal) &&
            string.Equals(Producer, other.Producer, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is FixedPresentationStateKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Channel,
            Producer,
            Generation);

        public int CompareTo(FixedPresentationStateKey other)
        {
            int channel = ChannelOrder(Channel).CompareTo(ChannelOrder(other.Channel));
            if (channel != 0)
                return channel;
            channel = string.CompareOrdinal(Channel, other.Channel);
            if (channel != 0)
                return channel;
            int producer = string.CompareOrdinal(Producer, other.Producer);
            return producer != 0 ? producer : Generation.CompareTo(other.Generation);
        }

        public override string ToString() => $"{Channel}/{Producer}/{Generation}";

        static int ChannelOrder(string channel)
        {
            return channel switch
            {
                "animation-selection" => 0,
                "animation-sample" => 1,
                "animation-terminal" => 2,
                _ => throw new InvalidOperationException(
                    $"Fixed Presentation state channel '{channel}' has no reconciliation order.")
            };
        }
    }

    internal readonly struct FixedPresentationRecord
    {
        public FixedPresentationRecord(
            FixedPresentationStateKey key,
            CharacterPresentationCommand command)
        {
            Key = key;
            Command = command;
        }

        public FixedPresentationStateKey Key { get; }
        public CharacterPresentationCommand Command { get; }
    }
}
