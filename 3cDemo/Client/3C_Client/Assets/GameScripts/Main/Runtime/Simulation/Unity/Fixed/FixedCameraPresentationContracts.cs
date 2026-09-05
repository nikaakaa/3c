using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal readonly struct FixedCameraPresentationScopeKey : IEquatable<FixedCameraPresentationScopeKey>
    {
        public FixedCameraPresentationScopeKey(
            string producer,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            Producer = producer ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public string Producer { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }

        public bool Equals(FixedCameraPresentationScopeKey other) =>
            Generation == other.Generation &&
            SourceActionInstanceId == other.SourceActionInstanceId &&
            string.Equals(Producer, other.Producer, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is FixedCameraPresentationScopeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Producer,
            Generation,
            SourceActionInstanceId);
    }

    internal readonly struct FixedCameraPresentationStateKey :
        IEquatable<FixedCameraPresentationStateKey>,
        IComparable<FixedCameraPresentationStateKey>
    {
        public FixedCameraPresentationStateKey(
            string channel,
            string producer,
            ulong generation,
            ulong sourceActionInstanceId = 0,
            int cycle = 0)
        {
            Channel = channel ?? string.Empty;
            Producer = producer ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            Cycle = cycle;
        }

        public string Channel { get; }
        public string Producer { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public int Cycle { get; }
        public bool IsCamera => string.Equals(Channel, "camera", StringComparison.Ordinal);
        public bool IsCameraForce => string.Equals(Channel, "camera-force", StringComparison.Ordinal);
        public FixedCameraPresentationScopeKey Scope => new FixedCameraPresentationScopeKey(
            Producer,
            Generation,
            SourceActionInstanceId);

        public bool Equals(FixedCameraPresentationStateKey other) =>
            Generation == other.Generation &&
            SourceActionInstanceId == other.SourceActionInstanceId &&
            Cycle == other.Cycle &&
            string.Equals(Channel, other.Channel, StringComparison.Ordinal) &&
            string.Equals(Producer, other.Producer, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is FixedCameraPresentationStateKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Channel,
            Producer,
            Generation,
            SourceActionInstanceId,
            Cycle);

        public int CompareTo(FixedCameraPresentationStateKey other)
        {
            int channel = ChannelOrder(Channel).CompareTo(ChannelOrder(other.Channel));
            if (channel != 0)
                return channel;
            channel = string.CompareOrdinal(Channel, other.Channel);
            if (channel != 0)
                return channel;
            int producer = string.CompareOrdinal(Producer, other.Producer);
            if (producer != 0)
                return producer;
            int generation = Generation.CompareTo(other.Generation);
            if (generation != 0)
                return generation;
            int action = SourceActionInstanceId.CompareTo(other.SourceActionInstanceId);
            return action != 0 ? action : Cycle.CompareTo(other.Cycle);
        }

        public override string ToString() =>
            $"{Channel}/{Producer}/{Generation}/{SourceActionInstanceId}/{Cycle}";

        static int ChannelOrder(string channel)
        {
            return channel switch
            {
                "animation-selection" => 0,
                "animation-sample" => 1,
                "animation-terminal" => 2,
                "camera-force" => 3,
                "camera" => 4,
                _ => throw new InvalidOperationException(
                    $"Fixed Presentation state channel '{channel}' has no reconciliation order.")
            };
        }
    }

    internal readonly struct FixedCameraPresentationRecord
    {
        public FixedCameraPresentationRecord(
            FixedCameraPresentationStateKey key,
            CharacterPresentationCommand command)
        {
            Key = key;
            Command = command;
        }

        public FixedCameraPresentationStateKey Key { get; }
        public CharacterPresentationCommand Command { get; }
    }

    internal interface IFixedCameraPresentationHistory
    {
        List<FixedCameraPresentationRecord> CollectCameraRecords(
            FixedCameraPresentationStateKey key,
            ulong confirmedTick);

        List<FixedCameraPresentationStateKey> CollectCameraStateKeys(
            CharacterPresentationCommand command);

        bool HasCameraForceScope(
            CharacterPresentationCommand command,
            ulong confirmedTick);
    }
}
