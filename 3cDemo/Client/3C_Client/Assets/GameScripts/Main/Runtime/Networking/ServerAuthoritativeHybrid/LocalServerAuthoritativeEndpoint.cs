using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public enum LocalServerAuthoritativeActionDecisionMode
    {
        AlwaysConfirm,
        AlwaysReject,
        RejectNextOnly,
        ConfirmThenCorrect
    }

    public enum LocalServerAuthoritativeCorrectionMode
    {
        None,
        OnClientCommand,
        AfterActionConfirm
    }

    [Serializable]
    public sealed class LocalServerAuthoritativeEndpointSettings
    {
        [SerializeField, Min(0)] int m_LatencyLocalLogicTicks = 2;
        [SerializeField] LocalServerAuthoritativeActionDecisionMode m_ActionDecisionMode = LocalServerAuthoritativeActionDecisionMode.AlwaysConfirm;
        [SerializeField] string m_RejectReason = "RejectedByLoopback";
        [SerializeField] LocalServerAuthoritativeCorrectionMode m_CorrectionMode;
        [SerializeField] Vector3 m_CorrectionOffset;
        [SerializeField] Vector3 m_CorrectionEuler;
        [SerializeField, Range(0f, 1f)] float m_PacketDropRate;
        [SerializeField] bool m_EmitMotionSnapshot;
        [SerializeField, Min(1)] int m_SnapshotIntervalLocalLogicTicks = 3;
        [SerializeField] Vector3 m_SnapshotPosition;
        [SerializeField] Vector3 m_SnapshotEuler;
        [SerializeField] string m_SnapshotStateId = "Loopback";
        [SerializeField] bool m_DefenseFavorApplied;
        [SerializeField] bool m_EmitGameplayResultOnActionConfirm;
        [SerializeField] string m_GameplayResultType = GameplayDemoResultTypes.HitConfirmed;
        [SerializeField] string m_GameplayResultWindowId = GameplayDemoWindowTypes.HitWindow;

        public int LatencyLocalLogicTicks => Mathf.Max(0, m_LatencyLocalLogicTicks);

        public LocalServerAuthoritativeActionDecisionMode ActionDecisionMode
        {
            get => m_ActionDecisionMode;
            set => m_ActionDecisionMode = value;
        }

        public string RejectReason => m_RejectReason ?? string.Empty;
        public LocalServerAuthoritativeCorrectionMode CorrectionMode => m_CorrectionMode;
        public Vector3 CorrectionOffset => m_CorrectionOffset;
        public Quaternion CorrectionRotation => Quaternion.Euler(m_CorrectionEuler);
        public float PacketDropRate => Mathf.Clamp01(m_PacketDropRate);
        public bool EmitMotionSnapshot => m_EmitMotionSnapshot;
        public int SnapshotIntervalLocalLogicTicks => Mathf.Max(1, m_SnapshotIntervalLocalLogicTicks);
        public Vector3 SnapshotPosition => m_SnapshotPosition;
        public Quaternion SnapshotRotation => Quaternion.Euler(m_SnapshotEuler);
        public string SnapshotStateId => m_SnapshotStateId ?? string.Empty;
        public bool DefenseFavorApplied => m_DefenseFavorApplied;
        public bool EmitGameplayResultOnActionConfirm => m_EmitGameplayResultOnActionConfirm;
        public string GameplayResultType => m_GameplayResultType ?? string.Empty;
        public string GameplayResultWindowId => m_GameplayResultWindowId ?? string.Empty;

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrWhiteSpace(m_RejectReason))
            {
                errors?.Add("LocalServerAuthoritative endpoint reject reason is missing.");
                valid = false;
            }
            if (m_EmitMotionSnapshot && string.IsNullOrWhiteSpace(m_SnapshotStateId))
            {
                errors?.Add("LocalServerAuthoritative endpoint snapshot state id is missing.");
                valid = false;
            }
            if (m_EmitGameplayResultOnActionConfirm && string.IsNullOrWhiteSpace(m_GameplayResultType))
            {
                errors?.Add("LocalServerAuthoritative endpoint gameplay result type is missing.");
                valid = false;
            }
            if (m_EmitGameplayResultOnActionConfirm && string.IsNullOrWhiteSpace(m_GameplayResultWindowId))
            {
                errors?.Add("LocalServerAuthoritative endpoint gameplay result window id is missing.");
                valid = false;
            }

            return valid;
        }
    }

    public sealed class LocalServerAuthoritativeEndpoint : IServerAuthoritativeEndpoint
    {
        public const string StableEndpointId = "LocalLoopback";

        readonly LocalServerAuthoritativeEndpointSettings m_Settings;
        readonly List<ScheduledPacket> m_Pending = new List<ScheduledPacket>();
        readonly Queue<ServerAuthoritativePacket> m_Incoming = new Queue<ServerAuthoritativePacket>();
        readonly List<ServerAuthoritativeDebugRecord> m_PendingDebugRecords = new List<ServerAuthoritativeDebugRecord>();
        readonly List<ServerAuthoritativeDebugRecord> m_DroppedDebugRecords = new List<ServerAuthoritativeDebugRecord>();
        readonly System.Random m_Random = new System.Random(1);
        ulong m_NextServerTick = 1;
        ulong m_NextSnapshotLocalLogicTick;

        public LocalServerAuthoritativeEndpoint(LocalServerAuthoritativeEndpointSettings settings)
        {
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IReadOnlyList<ServerAuthoritativeDebugRecord> PendingDebugRecords => m_PendingDebugRecords;
        public IReadOnlyList<ServerAuthoritativeDebugRecord> DroppedDebugRecords => m_DroppedDebugRecords;

        public void EnqueueOutgoing(ServerAuthoritativePacket packet)
        {
            if (ShouldDrop())
            {
                RecordDropped(new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Dropped, packet));
                return;
            }

            switch (packet.Envelope.PacketKind)
            {
                case ServerAuthoritativePacketKind.MotionCommand:
                    TryScheduleSnapshot(packet);
                    TryScheduleCommandCorrection(packet);
                    break;
                case ServerAuthoritativePacketKind.ActionActivation:
                    ScheduleActionDecision(packet);
                    break;
                case ServerAuthoritativePacketKind.GameplayResult:
                    ScheduleGameplayResult(packet);
                    break;
                case ServerAuthoritativePacketKind.GameplayEffectLifecycle:
                case ServerAuthoritativePacketKind.GameplayAttributeValue:
                case ServerAuthoritativePacketKind.GameplayCue:
                    ScheduleGameplayResult(packet);
                    break;
            }
        }

        public void Pump(ulong localLogicTick)
        {
            for (int i = m_Pending.Count - 1; i >= 0; i--)
            {
                ScheduledPacket scheduled = m_Pending[i];
                if (scheduled.DueLocalLogicTick > localLogicTick)
                    continue;

                if (ShouldDrop())
                    RecordDropped(new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Dropped, scheduled.Packet));
                else
                    m_Incoming.Enqueue(scheduled.Packet);

                m_Pending.RemoveAt(i);
            }

            RebuildPendingDebug();
        }

        public bool TryDequeueIncoming(out ServerAuthoritativePacket packet)
        {
            if (m_Incoming.Count == 0)
            {
                packet = default;
                return false;
            }

            packet = m_Incoming.Dequeue();
            return true;
        }

        public void Dispose()
        {
            m_Pending.Clear();
            m_Incoming.Clear();
            m_PendingDebugRecords.Clear();
            m_DroppedDebugRecords.Clear();
        }

        void ScheduleActionDecision(ServerAuthoritativePacket packet)
        {
            LocalServerAuthoritativeActionDecisionMode mode = m_Settings.ActionDecisionMode;
            bool rejected = mode == LocalServerAuthoritativeActionDecisionMode.AlwaysReject || mode == LocalServerAuthoritativeActionDecisionMode.RejectNextOnly;
            if (mode == LocalServerAuthoritativeActionDecisionMode.RejectNextOnly)
                m_Settings.ActionDecisionMode = LocalServerAuthoritativeActionDecisionMode.AlwaysConfirm;

            bool defenseFavor = !rejected && m_Settings.DefenseFavorApplied && IsDefenseAction(packet.ActionActivation.ActionId);
            var decision = new ServerAuthoritativeActionInstanceDecision(
                packet.ActionActivation.ActionInstanceId,
                packet.ActionActivation.ActionId,
                packet.Envelope.PredictionKey,
                packet.Envelope.InputSequence,
                packet.Envelope.LocalLogicTick,
                NextServerTick(),
                rejected ? ServerAuthoritativeActionDecisionKind.Rejected : ServerAuthoritativeActionDecisionKind.Confirmed,
                rejected ? m_Settings.RejectReason : string.Empty,
                defenseFavor);
            Schedule(packet.Envelope.LocalLogicTick, ServerAuthoritativePacket.ActionInstanceDecisionPacket(packet.Envelope.Identity, decision, packet.Envelope.PolicyId));

            if (!rejected && (mode == LocalServerAuthoritativeActionDecisionMode.ConfirmThenCorrect || m_Settings.CorrectionMode == LocalServerAuthoritativeCorrectionMode.AfterActionConfirm))
                ScheduleCorrection(packet);

            if (!rejected && m_Settings.EmitGameplayResultOnActionConfirm)
                ScheduleConfirmedGameplayResult(packet);
        }

        void TryScheduleSnapshot(ServerAuthoritativePacket packet)
        {
            if (!m_Settings.EmitMotionSnapshot || packet.Envelope.LocalLogicTick < m_NextSnapshotLocalLogicTick)
                return;

            m_NextSnapshotLocalLogicTick = packet.Envelope.LocalLogicTick + (ulong)m_Settings.SnapshotIntervalLocalLogicTicks;
            var snapshot = new ServerAuthoritativeMotionSnapshot(
                NextServerTick(),
                m_Settings.SnapshotPosition,
                m_Settings.SnapshotRotation,
                m_Settings.SnapshotStateId);
            Schedule(packet.Envelope.LocalLogicTick, ServerAuthoritativePacket.MotionSnapshotPacket(packet.Envelope.Identity, snapshot, packet.Envelope.InputSequence, packet.Envelope.LocalLogicTick, packet.Envelope.PolicyId));
        }

        void TryScheduleCommandCorrection(ServerAuthoritativePacket packet)
        {
            if (m_Settings.CorrectionMode != LocalServerAuthoritativeCorrectionMode.OnClientCommand)
                return;

            ScheduleCorrection(packet);
        }

        void ScheduleCorrection(ServerAuthoritativePacket packet)
        {
            var correction = new ServerAuthoritativeMotionCorrection(
                packet.Envelope.InputSequence,
                NextServerTick(),
                m_Settings.CorrectionOffset,
                m_Settings.CorrectionRotation);
            Schedule(packet.Envelope.LocalLogicTick + 1, ServerAuthoritativePacket.MotionCorrectionPacket(packet.Envelope.Identity, correction, packet.Envelope.LocalLogicTick, packet.Envelope.PolicyId));
        }

        void ScheduleGameplayResult(ServerAuthoritativePacket packet)
        {
            ulong serverTick = packet.Envelope.ServerTick != 0 ? packet.Envelope.ServerTick : NextServerTick();
            Schedule(packet.Envelope.LocalLogicTick, packet.WithPacketId(0).WithServerTick(serverTick));
        }

        void ScheduleConfirmedGameplayResult(ServerAuthoritativePacket packet)
        {
            ulong serverTick = NextServerTick();
            ulong resultId = packet.ActionActivation.ActionInstanceId * 1000003UL + serverTick;
            string targetActorId = !string.IsNullOrEmpty(packet.ActionActivation.TargetStableId)
                ? packet.ActionActivation.TargetStableId
                : packet.Envelope.Identity.TargetActorId;
            var result = new ServerAuthoritativeGameplayResult(
                resultId,
                packet.ActionActivation.ActionInstanceId,
                m_Settings.GameplayResultWindowId,
                packet.Envelope.Identity.PerformerActorId,
                targetActorId,
                m_Settings.GameplayResultType,
                string.Empty);
            Schedule(packet.Envelope.LocalLogicTick, ServerAuthoritativePacket.GameplayResultPacket(packet.Envelope.Identity, result, packet.Envelope.LocalLogicTick, serverTick, packet.Envelope.PolicyId));
        }

        void Schedule(ulong localLogicTick, ServerAuthoritativePacket packet)
        {
            ulong dueTick = localLogicTick + (ulong)m_Settings.LatencyLocalLogicTicks;
            m_Pending.Add(new ScheduledPacket(dueTick, packet));
            RebuildPendingDebug();
        }

        ulong NextServerTick()
        {
            return m_NextServerTick++;
        }

        bool ShouldDrop()
        {
            float dropRate = m_Settings.PacketDropRate;
            return dropRate > 0f && m_Random.NextDouble() < dropRate;
        }

        void RecordDropped(ServerAuthoritativeDebugRecord record)
        {
            m_DroppedDebugRecords.Add(record);
            if (m_DroppedDebugRecords.Count > 64)
                m_DroppedDebugRecords.RemoveAt(0);
        }

        void RebuildPendingDebug()
        {
            m_PendingDebugRecords.Clear();
            for (int i = 0; i < m_Pending.Count; i++)
                m_PendingDebugRecords.Add(new ServerAuthoritativeDebugRecord(ServerAuthoritativePacketDirection.Pending, m_Pending[i].Packet));
        }

        static bool IsDefenseAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return false;

            return actionId.StartsWith("Defense.", StringComparison.Ordinal) ||
                   actionId.StartsWith("Support.", StringComparison.Ordinal) ||
                   actionId.IndexOf("Parry", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   actionId.IndexOf("Guard", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        readonly struct ScheduledPacket
        {
            public ScheduledPacket(ulong dueLocalLogicTick, ServerAuthoritativePacket packet)
            {
                DueLocalLogicTick = dueLocalLogicTick;
                Packet = packet;
            }

            public ulong DueLocalLogicTick { get; }
            public ServerAuthoritativePacket Packet { get; }
        }
    }
}
