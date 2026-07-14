using System.Collections.Generic;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public readonly struct ServerAuthoritativeHistoryRecord
    {
        public ServerAuthoritativeHistoryRecord(ServerAuthoritativePacket packet)
        {
            PacketId = packet.Envelope.PacketId;
            SubjectActorId = packet.Envelope.Identity.SubjectActorId;
            SyncDomain = packet.Envelope.SyncDomain;
            PolicyId = packet.Envelope.PolicyId;
            StableId = packet.Envelope.StableId;
            InputSequence = packet.Envelope.InputSequence;
            LocalLogicTick = packet.Envelope.LocalLogicTick;
            ServerTick = packet.Envelope.ServerTick;
            PredictionKey = packet.Envelope.PredictionKey;
            BehaviorId = packet.GameplayEffectLifecycle.EffectId.IsValid
                ? packet.GameplayEffectLifecycle.BehaviorId
                : packet.GameplayAttributeValue.CauseBehaviorId;
            EffectInstanceId = packet.GameplayEffectLifecycle.InstanceId.IsValid
                ? packet.GameplayEffectLifecycle.InstanceId.Value
                : packet.GameplayAttributeValue.CauseEffectInstanceId.Value;
            Revision = packet.GameplayEffectLifecycle.LifecycleRevision != 0
                ? packet.GameplayEffectLifecycle.LifecycleRevision
                : packet.GameplayAttributeValue.ValueRevision;
        }

        public ulong PacketId { get; }
        public string SubjectActorId { get; }
        public ServerAuthoritativeDomain SyncDomain { get; }
        public string PolicyId { get; }
        public string StableId { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public ulong ServerTick { get; }
        public ulong PredictionKey { get; }
        public string BehaviorId { get; }
        public ulong EffectInstanceId { get; }
        public ulong Revision { get; }
    }

    public sealed class ServerAuthoritativeHistory
    {
        readonly List<ServerAuthoritativeHistoryRecord> m_Records = new List<ServerAuthoritativeHistoryRecord>();
        readonly int m_Capacity;

        public ServerAuthoritativeHistory(int capacity)
        {
            m_Capacity = capacity > 0 ? capacity : throw new System.ArgumentOutOfRangeException(nameof(capacity));
        }

        public IReadOnlyList<ServerAuthoritativeHistoryRecord> Records => m_Records;

        public void Clear()
        {
            m_Records.Clear();
        }

        public void Record(ServerAuthoritativePacket packet)
        {
            m_Records.Add(new ServerAuthoritativeHistoryRecord(packet));
            if (m_Records.Count > m_Capacity)
                m_Records.RemoveAt(0);
        }
    }
}
