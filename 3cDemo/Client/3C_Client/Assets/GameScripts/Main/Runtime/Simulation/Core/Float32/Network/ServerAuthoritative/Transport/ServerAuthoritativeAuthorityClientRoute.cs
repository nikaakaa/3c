using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative.Transport
{
    sealed class ServerAuthoritativeAuthorityClientRoute
    {
        readonly int m_Capacity;
        readonly SortedDictionary<ulong, CanonicalInputSample> m_Inputs = new SortedDictionary<ulong, CanonicalInputSample>();
        readonly SortedDictionary<ulong, NetworkCheckpoint> m_Sent = new SortedDictionary<ulong, NetworkCheckpoint>();
        CanonicalInputSample m_Held;
        ulong m_HeldAcceptedTick;
        ulong m_LastEnqueuedInputSequence;
        ulong m_SendPacketSequence;
        ulong m_SnapshotSequence;
        ulong m_PreviousCommandPacketCount;
        ulong m_PreviousCommandPayloadBytes;
        ulong m_PreviousDeltaSnapshotCount;
        ulong m_PreviousDeltaPayloadBytes;
        ulong m_DeltaPayloadBytes;

        public ServerAuthoritativeAuthorityClientRoute(ServerAuthoritativeRosterEntry roster, int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Roster = roster;
            m_Capacity = capacity;
        }

        public ServerAuthoritativeRosterEntry Roster { get; }
        public ServerAuthoritativeAuthorityDataPlaneTicket Ticket { get; private set; }
        public ServerAuthoritativeDatagramIdentity Identity { get; private set; }
        public bool DataPlaneReady { get; set; }
        public bool TicketConsumptionReported { get; set; }
        public ulong LastReceivedPacketSequence { get; private set; }
        public ulong PendingCheckpointRequest { get; set; }
        public NetworkCheckpoint AcknowledgedCheckpoint { get; private set; }
        public ulong AcknowledgedSnapshotSequence { get; private set; }
        public ulong DeltaSnapshotCount { get; private set; }
        public ulong FullCheckpointCount { get; private set; }
        public ulong DeltaMtuExceededCount { get; private set; }
        public int LastDeltaPayloadBytes { get; private set; }
        public bool HasInput => m_Held != null || m_Inputs.Count > 0;
        public ulong CommandPacketCount { get; private set; }
        public ulong CommandPayloadBytes { get; private set; }
        public ulong PacketSequenceGaps { get; private set; }
        public ulong DuplicatePackets { get; private set; }
        public ulong OutOfOrderPackets { get; private set; }
        public ulong ExactInputCount { get; private set; }
        public ulong HeldInputCount { get; private set; }
        public ulong NeutralInputCount { get; private set; }
        public ulong LateInputCount { get; private set; }
        public long LastCommandLead { get; private set; }
        public ulong LastCommandSourceTick { get; private set; }

        public void SetTicket(
            ServerAuthoritativeAuthorityDataPlaneTicket ticket,
            ServerAuthoritativeDatagramIdentity identity)
        {
            if (Ticket != null)
                throw new InvalidOperationException("Authority route received more than one data-plane ticket.");
            Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
            Identity = identity;
        }

        public void Enqueue(CanonicalInputSample sample)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));
            if (sample.InputSequence <= m_LastEnqueuedInputSequence)
                return;
            m_LastEnqueuedInputSequence = sample.InputSequence;
            if (m_Inputs.Count >= m_Capacity)
                throw new InvalidOperationException($"Authority command queue for Actor '{Roster.ActorId}' overflowed.");
            if (m_Inputs.TryGetValue(sample.TargetAuthorityTick, out CanonicalInputSample current))
            {
                if (sample.InputSequence <= current.InputSequence)
                    return;
                m_Inputs[sample.TargetAuthorityTick] = sample;
                return;
            }
            m_Inputs.Add(sample.TargetAuthorityTick, sample);
        }

        public AcceptedAuthorityInput Select(ulong authorityTick, int holdTicks)
        {
            CanonicalInputSample selected = null;
            var remove = new List<ulong>();
            foreach (KeyValuePair<ulong, CanonicalInputSample> pair in m_Inputs)
            {
                if (pair.Key > authorityTick)
                    break;
                selected = pair.Value;
                remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                m_Inputs.Remove(remove[i]);
            if (selected != null)
            {
                if (selected.TargetAuthorityTick == authorityTick)
                    ExactInputCount++;
                else
                    LateInputCount++;
                m_Held = selected;
                m_HeldAcceptedTick = authorityTick;
            }
            if (m_Held == null)
                throw new InvalidOperationException($"Authority has no canonical input for Actor '{Roster.ActorId}' at Tick '{authorityTick}'.");
            if (authorityTick - m_HeldAcceptedTick > (ulong)holdTicks)
            {
                NeutralInputCount++;
                return new AcceptedAuthorityInput(Roster.ActorId, m_Held.InputSequence, Neutral(m_Held.Input, authorityTick));
            }
            if (selected == null)
                HeldInputCount++;
            return new AcceptedAuthorityInput(Roster.ActorId, m_Held.InputSequence, m_Held.Input);
        }

        public void AcceptHelloSequence(ulong sequence)
        {
            if (sequence > LastReceivedPacketSequence)
                LastReceivedPacketSequence = sequence;
        }

        public bool AcceptPacketSequence(ulong sequence)
        {
            if (sequence <= LastReceivedPacketSequence)
            {
                if (sequence == LastReceivedPacketSequence)
                    DuplicatePackets++;
                else
                    OutOfOrderPackets++;
                return false;
            }
            if (LastReceivedPacketSequence != 0 && sequence > LastReceivedPacketSequence + 1)
                PacketSequenceGaps = checked(PacketSequenceGaps + sequence - LastReceivedPacketSequence - 1);
            LastReceivedPacketSequence = sequence;
            return true;
        }

        public void RecordCommand(int payloadBytes, ulong sourceTick)
        {
            if (payloadBytes < 0 || sourceTick == 0)
                throw new ArgumentOutOfRangeException(payloadBytes < 0 ? nameof(payloadBytes) : nameof(sourceTick));
            CommandPacketCount++;
            CommandPayloadBytes = checked(CommandPayloadBytes + (ulong)payloadBytes);
            LastCommandSourceTick = sourceTick;
        }

        public void RecordCommandLead(ulong targetTick, ulong authorityTick)
        {
            LastCommandLead = targetTick >= authorityTick
                ? checked((long)(targetTick - authorityTick))
                : -checked((long)(authorityTick - targetTick));
        }

        public void AcknowledgeSnapshot(ulong latestSnapshot, ulong latestBase)
        {
            if (latestSnapshot == 0 && latestBase == 0)
                return;
            if (latestSnapshot != latestBase || latestSnapshot < AcknowledgedSnapshotSequence)
                throw new InvalidOperationException("Client snapshot acknowledgement is inconsistent or regressed.");
            if (!m_Sent.TryGetValue(latestSnapshot, out NetworkCheckpoint checkpoint))
                return;
            AcknowledgedSnapshotSequence = latestSnapshot;
            AcknowledgedCheckpoint = checkpoint;
            var remove = new List<ulong>();
            foreach (ulong sequence in m_Sent.Keys)
            {
                if (sequence < latestSnapshot)
                    remove.Add(sequence);
            }
            for (int i = 0; i < remove.Count; i++)
                m_Sent.Remove(remove[i]);
        }

        public void StoreSent(ulong sequence, NetworkCheckpoint checkpoint)
        {
            m_Sent[sequence] = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            while (m_Sent.Count > m_Capacity)
            {
                using IEnumerator<ulong> iterator = m_Sent.Keys.GetEnumerator();
                iterator.MoveNext();
                if (iterator.Current >= AcknowledgedSnapshotSequence && AcknowledgedSnapshotSequence != 0)
                    throw new InvalidOperationException("Authority snapshot baseline capacity cannot discard an unconfirmed checkpoint.");
                m_Sent.Remove(iterator.Current);
            }
        }

        public void RecordDeltaSnapshot(int payloadBytes)
        {
            DeltaSnapshotCount++;
            m_DeltaPayloadBytes = checked(m_DeltaPayloadBytes + (ulong)payloadBytes);
            LastDeltaPayloadBytes = payloadBytes;
        }

        public void RecordFullCheckpoint(int payloadBytes)
        {
            FullCheckpointCount++;
            LastDeltaPayloadBytes = payloadBytes;
        }

        public void RecordDeltaMtuExceeded(int payloadBytes)
        {
            DeltaMtuExceededCount++;
            LastDeltaPayloadBytes = payloadBytes;
        }

        public void RequestFullCheckpoint(ulong sequence)
        {
            if (PendingCheckpointRequest != 0)
                throw new InvalidOperationException("Authority route already has a pending full checkpoint request.");
            PendingCheckpointRequest = sequence;
        }

        public ulong NextSendPacketSequence() => ++m_SendPacketSequence;
        public ulong NextSnapshotSequence() => ++m_SnapshotSequence;

        public string DescribeMetrics(float elapsedSeconds)
        {
            float seconds = Math.Max(elapsedSeconds, 0.001f);
            float commandPacketsPerSecond = (CommandPacketCount - m_PreviousCommandPacketCount) / seconds;
            float commandBytesPerSecond = (CommandPayloadBytes - m_PreviousCommandPayloadBytes) / seconds;
            float snapshotPacketsPerSecond = (DeltaSnapshotCount - m_PreviousDeltaSnapshotCount) / seconds;
            float snapshotBytesPerSecond = (m_DeltaPayloadBytes - m_PreviousDeltaPayloadBytes) / seconds;
            m_PreviousCommandPacketCount = CommandPacketCount;
            m_PreviousCommandPayloadBytes = CommandPayloadBytes;
            m_PreviousDeltaSnapshotCount = DeltaSnapshotCount;
            m_PreviousDeltaPayloadBytes = m_DeltaPayloadBytes;
            return $"{Roster.ActorId}:commands={CommandPacketCount}/{CommandPayloadBytes}@{commandPacketsPerSecond:0.##}pps/{commandBytesPerSecond:0.##}Bps,packetGaps={PacketSequenceGaps},duplicates={DuplicatePackets},outOfOrder={OutOfOrderPackets},exact={ExactInputCount},held={HeldInputCount},neutral={NeutralInputCount},late={LateInputCount},lead={LastCommandLead},delta={DeltaSnapshotCount}@{snapshotPacketsPerSecond:0.##}pps/{snapshotBytesPerSecond:0.##}Bps,full={FullCheckpointCount},oversize={DeltaMtuExceededCount},lastBytes={LastDeltaPayloadBytes}";
        }

        static CharacterSimulationInput Neutral(CharacterSimulationInput source, ulong authorityTick)
        {
            var values = new SimulationInputValue[source.Values.Count];
            for (int i = 0; i < values.Length; i++)
            {
                SimulationInputValue value = source.Values[i];
                values[i] = value.Kind switch
                {
                    SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(value.InputId, false),
                    SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(value.InputId, Float32Scalar.Zero),
                    SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(value.InputId, Float32Vector2.Zero),
                    SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(value.InputId, Float32Vector3.Zero),
                    SimulationInputValueKind.Yaw => SimulationInputValue.FromYaw(value.InputId, Float32Yaw.Zero),
                    SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(value.InputId, SimulationActionTargetSnapshot.None),
                    _ => throw new InvalidDataException($"Unsupported input value kind '{value.Kind}'.")
                };
            }
            return new CharacterSimulationInput(
                source.NumericProfile,
                new SimulationTickSourceIdentity(SimulationTickSourceKind.Authoritative, source.TickSource.ClockId, authorityTick),
                source.InputSourceIdentity,
                source.Sequence,
                values,
                Array.Empty<SimulationInputRequest>());
        }
    }
}
