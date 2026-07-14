using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public readonly struct ServerAuthoritativeDebugRecord
    {
        public ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection direction, ServerAuthoritativePacket packet)
        {
            Direction = direction;
            PacketId = packet.Envelope.PacketId;
            PacketKind = packet.Envelope.PacketKind;
            SyncDomain = packet.Envelope.SyncDomain;
            SubjectActorId = packet.Envelope.Identity.SubjectActorId;
            StableId = packet.Envelope.StableId;
            PolicyId = packet.Envelope.PolicyId;
            PredictionKey = packet.Envelope.PredictionKey;
            InputSequence = packet.Envelope.PacketKind == ServerAuthoritativePacketKind.MotionCorrectionAck
                ? packet.MotionCorrectionAcknowledgement.InputSequence
                : packet.Envelope.InputSequence;
            LocalLogicTick = packet.Envelope.LocalLogicTick;
            ServerTick = packet.Envelope.PacketKind == ServerAuthoritativePacketKind.MotionCorrectionAck
                ? packet.MotionCorrectionAcknowledgement.ServerTick
                : packet.Envelope.ServerTick;
            ActionInstanceId = packet.ActionActivation.ActionInstanceId != 0 ? packet.ActionActivation.ActionInstanceId : packet.ActionDecision.ActionInstanceId;
            if (ActionInstanceId == 0)
                ActionInstanceId = packet.ActionLifecycleTransition.ActionInstanceId;
            if (ActionInstanceId == 0)
                ActionInstanceId = packet.ActionWindowDigest.ActionInstanceId;
            if (ActionInstanceId == 0)
                ActionInstanceId = packet.ActionMotionDigest.ActionInstanceId != 0 ? packet.ActionMotionDigest.ActionInstanceId : packet.GameplayCue.SourceActionInstanceId;
            if (ActionInstanceId == 0)
                ActionInstanceId = packet.GameplayResult.ActionInstanceId;
            ActionId = !string.IsNullOrEmpty(packet.ActionActivation.ActionId) ? packet.ActionActivation.ActionId : packet.ActionDecision.ActionId;
            Decision = packet.ActionLifecycleTransition.TransitionKind != ServerAuthoritativeActionLifecycleTransitionKind.None
                ? packet.ActionLifecycleTransition.TransitionKind.ToString()
                : packet.ActionDecision.Decision.ToString();
            Reason = !string.IsNullOrEmpty(packet.ActionLifecycleTransition.Reason)
                ? packet.ActionLifecycleTransition.Reason
                : (!string.IsNullOrEmpty(packet.ActionDecision.Reason) ? packet.ActionDecision.Reason : packet.GameplayResult.Reason);
            CorrectionPosition = packet.Envelope.PacketKind == ServerAuthoritativePacketKind.MotionCorrection
                ? packet.MotionCorrection.Position
                : Vector3.zero;
            ResultId = packet.GameplayResult.ResultId;
            ResultType = packet.GameplayResult.ResultType;
            WindowId = !string.IsNullOrEmpty(packet.ActionWindowDigest.WindowId) ? packet.ActionWindowDigest.WindowId : packet.GameplayResult.WindowId;
            BehaviorId = ResolveBehaviorId(packet);
            EffectInstanceId = packet.GameplayEffectLifecycle.InstanceId.Value != 0
                ? packet.GameplayEffectLifecycle.InstanceId.Value
                : packet.GameplayAttributeValue.CauseEffectInstanceId.Value;
            Revision = packet.GameplayEffectLifecycle.LifecycleRevision != 0
                ? packet.GameplayEffectLifecycle.LifecycleRevision
                : packet.GameplayAttributeValue.ValueRevision;
        }

        public ServerAuthoritativePacketDirection Direction { get; }
        public ulong PacketId { get; }
        public ServerAuthoritativePacketKind PacketKind { get; }
        public ServerAuthoritativeDomain SyncDomain { get; }
        public string SubjectActorId { get; }
        public string StableId { get; }
        public string PolicyId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public ulong ServerTick { get; }
        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public string Decision { get; }
        public string Reason { get; }
        public Vector3 CorrectionPosition { get; }
        public ulong ResultId { get; }
        public string ResultType { get; }
        public string WindowId { get; }
        public string BehaviorId { get; }
        public ulong EffectInstanceId { get; }
        public ulong Revision { get; }

        static string ResolveBehaviorId(ServerAuthoritativePacket packet)
        {
            if (packet.GameplayEffectLifecycle.EffectId.IsValid)
                return packet.GameplayEffectLifecycle.BehaviorId;
            if (packet.GameplayAttributeValue.CauseEffectId.IsValid)
                return packet.GameplayAttributeValue.CauseBehaviorId;
            if (!string.IsNullOrEmpty(packet.GameplayCue.BehaviorId))
                return packet.GameplayCue.BehaviorId;
            if (!string.IsNullOrEmpty(packet.ActionActivation.ActionId))
                return packet.ActionActivation.ActionId;
            return packet.ActionDecision.ActionId ?? string.Empty;
        }
    }

    public sealed class ServerAuthoritativeDebug
    {
        readonly List<ServerAuthoritativeDebugRecord> m_RecentOutgoing = new List<ServerAuthoritativeDebugRecord>();
        readonly List<ServerAuthoritativeDebugRecord> m_RecentIncoming = new List<ServerAuthoritativeDebugRecord>();
        readonly List<ServerAuthoritativeDebugRecord> m_Pending = new List<ServerAuthoritativeDebugRecord>();
        readonly List<ServerAuthoritativeDebugRecord> m_Dropped = new List<ServerAuthoritativeDebugRecord>();
        readonly List<ServerAuthoritativePolicyDecisionDebugRecord> m_PolicyDecisions = new List<ServerAuthoritativePolicyDecisionDebugRecord>();
        readonly HashSet<ulong> m_EndpointDropPacketIds = new HashSet<ulong>();
        readonly int m_Capacity;

        public ServerAuthoritativeDebug(int capacity)
        {
            m_Capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public IReadOnlyList<ServerAuthoritativeDebugRecord> RecentOutgoing => m_RecentOutgoing;
        public IReadOnlyList<ServerAuthoritativeDebugRecord> RecentIncoming => m_RecentIncoming;
        public IReadOnlyList<ServerAuthoritativeDebugRecord> Pending => m_Pending;
        public IReadOnlyList<ServerAuthoritativeDebugRecord> Dropped => m_Dropped;
        public IReadOnlyList<ServerAuthoritativePolicyDecisionDebugRecord> PolicyDecisions => m_PolicyDecisions;

        public void Clear()
        {
            m_RecentOutgoing.Clear();
            m_RecentIncoming.Clear();
            m_Pending.Clear();
            m_Dropped.Clear();
            m_PolicyDecisions.Clear();
            m_EndpointDropPacketIds.Clear();
        }

        internal void RecordOutgoing(ServerAuthoritativePacket packet)
        {
            Add(m_RecentOutgoing, new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Outgoing, packet));
        }

        internal void RecordIncoming(ServerAuthoritativePacket packet)
        {
            Add(m_RecentIncoming, new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Incoming, packet));
        }

        internal void RecordPolicyDecision(ServerAuthoritativePolicyDecisionDebugRecord record)
        {
            Add(m_PolicyDecisions, record);
        }

        internal void RecordDropped(ServerAuthoritativePacket packet)
        {
            Add(m_Dropped, new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Dropped, packet));
        }

        internal void SetPending(IReadOnlyList<ServerAuthoritativeDebugRecord> records)
        {
            CopyLatest(records, m_Pending);
        }

        internal void SetEndpointDropped(IReadOnlyList<ServerAuthoritativeDebugRecord> records)
        {
            if (records == null)
                return;

            for (int i = 0; i < records.Count; i++)
            {
                ServerAuthoritativeDebugRecord record = records[i];
                if (record.PacketId != 0 && m_EndpointDropPacketIds.Add(record.PacketId))
                    Add(m_Dropped, record);
            }
        }

        void Add(List<ServerAuthoritativeDebugRecord> records, ServerAuthoritativeDebugRecord record)
        {
            records.Add(record);
            if (records.Count > m_Capacity)
                records.RemoveAt(0);
        }

        void Add(List<ServerAuthoritativePolicyDecisionDebugRecord> records, ServerAuthoritativePolicyDecisionDebugRecord record)
        {
            records.Add(record);
            if (records.Count > m_Capacity)
                records.RemoveAt(0);
        }

        void CopyLatest(IReadOnlyList<ServerAuthoritativeDebugRecord> source, List<ServerAuthoritativeDebugRecord> target)
        {
            target.Clear();
            if (source == null)
                return;

            for (int i = Math.Max(0, source.Count - m_Capacity); i < source.Count; i++)
                target.Add(source[i]);
        }
    }

    public readonly struct ServerAuthoritativePolicyDecisionDebugRecord
    {
        public ServerAuthoritativePolicyDecisionDebugRecord(
            string behaviorId,
            string behaviorKind,
            ulong actionInstanceId,
            string actionId,
            string factKind,
            string syncDomain,
            string packetKind,
            string policyId,
            bool shouldSend,
            string reason,
            string summary)
        {
            BehaviorId = behaviorId ?? string.Empty;
            BehaviorKind = behaviorKind ?? string.Empty;
            ActionInstanceId = actionInstanceId;
            ActionId = actionId ?? string.Empty;
            FactKind = factKind ?? string.Empty;
            SyncDomain = syncDomain ?? string.Empty;
            PacketKind = packetKind ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            ShouldSend = shouldSend;
            Reason = reason ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public string BehaviorId { get; }
        public string BehaviorKind { get; }
        public ulong ActionInstanceId { get; }
        public string ActionId { get; }
        public string FactKind { get; }
        public string SyncDomain { get; }
        public string PacketKind { get; }
        public string PolicyId { get; }
        public bool ShouldSend { get; }
        public string Reason { get; }
        public string Summary { get; }
    }
}
