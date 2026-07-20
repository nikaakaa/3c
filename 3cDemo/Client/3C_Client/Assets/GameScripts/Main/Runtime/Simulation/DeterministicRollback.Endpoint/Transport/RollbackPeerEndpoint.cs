using System;
using System.Collections.Generic;
using System.Net;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackPeerEndpoint : IDisposable
    {
        readonly RollbackEndpointDefinition m_Definition;
        readonly RollbackHandshake m_LocalHandshake;
        readonly RollbackDatagramEndpoint m_Endpoint;
        readonly RollbackDatagramChannel m_Channel;
        readonly SortedDictionary<ulong, RollbackCanonicalInputBundle> m_CanonicalBundles =
            new SortedDictionary<ulong, RollbackCanonicalInputBundle>();
        readonly SortedDictionary<ulong, RollbackCanonicalConfirmation> m_PendingConfirmations =
            new SortedDictionary<ulong, RollbackCanonicalConfirmation>();
        readonly Queue<IRollbackProtocolPayload> m_ControlMessages = new Queue<IRollbackProtocolPayload>();
        readonly Queue<RollbackRelayedExplicitInputBatch> m_RelayedExplicitInputs =
            new Queue<RollbackRelayedExplicitInputBatch>();
        readonly SortedDictionary<ulong, RollbackActorInputFrame> m_InputRedundancy =
            new SortedDictionary<ulong, RollbackActorInputFrame>();
        readonly int m_InputRedundancyCount;
        bool m_Started;
        bool m_RemoteHandshakeAccepted;
        bool m_Disposed;

        public RollbackPeerEndpoint(
            RollbackEndpointDefinition definition,
            RollbackHandshake localHandshake,
            string relayServerPeerId,
            IPEndPoint localEndPoint,
            IPEndPoint relayServerEndPoint,
            int inputRedundancyCount)
        {
            m_Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_LocalHandshake = localHandshake ?? throw new ArgumentNullException(nameof(localHandshake));
            if (localEndPoint == null || relayServerEndPoint == null)
                throw new ArgumentNullException(localEndPoint == null ? nameof(localEndPoint) : nameof(relayServerEndPoint));
            if (inputRedundancyCount <= 0 || inputRedundancyCount > definition.MaximumQueuedMessages)
                throw new ArgumentOutOfRangeException(nameof(inputRedundancyCount));
            m_InputRedundancyCount = inputRedundancyCount;
            m_Endpoint = new RollbackDatagramEndpoint(
                localEndPoint,
                definition.MaximumQueuedMessages,
                definition.MaximumDatagramBytes);
            m_Channel = new RollbackDatagramChannel(
                m_Endpoint,
                definition,
                localHandshake.PeerId,
                relayServerPeerId,
                relayServerEndPoint);
        }

        public IPEndPoint LocalEndPoint => m_Endpoint.LocalEndPoint;
        public string LocalPeerId => m_LocalHandshake.PeerId;
        public bool IsReady => m_RemoteHandshakeAccepted && Roster != null;
        public RollbackRoster Roster { get; private set; }
        public ulong ConfirmedCanonicalTick { get; private set; }
        public long DroppedReceivedDatagrams => m_Endpoint.DroppedReceivedDatagrams;

        public void Start()
        {
            ThrowIfDisposed();
            if (m_Started)
                throw new InvalidOperationException("Rollback peer Endpoint is already started.");
            m_Started = true;
            m_Channel.Send(m_LocalHandshake, true);
            m_Endpoint.PumpSend();
        }

        public void Pump()
        {
            ThrowIfDisposed();
            if (!m_Started)
                throw new InvalidOperationException("Rollback peer Endpoint is not started.");
            while (m_Endpoint.TryReceive(out RollbackReceivedDatagram received))
                m_Channel.Process(received);
            while (m_Channel.TryReceive(out RollbackProtocolEnvelope envelope))
                Process(envelope.Payload);
            m_Channel.Pump();
            m_Endpoint.PumpSend();
        }

        public void SendInput(RollbackActorInputFrame input)
        {
            RequireReady();
            if (input == null || input.Provenance != RollbackInputProvenance.LocalExplicit)
                throw new ArgumentException("Rollback peer input must be a local explicit frame.", nameof(input));
            RollbackRosterEntry local = GetLocalRosterEntry();
            if (!local.ActorId.Equals(input.ActorId))
                throw new InvalidOperationException("Rollback peer cannot send input for another roster Actor.");
            if (m_InputRedundancy.TryGetValue(input.Tick.Value, out RollbackActorInputFrame current))
            {
                if (!current.Identity.Equals(input.Identity))
                    throw new InvalidOperationException("Rollback local input Tick cannot be revised after capture.");
            }
            else
            {
                m_InputRedundancy.Add(input.Tick.Value, input);
            }
            while (m_InputRedundancy.Count > m_InputRedundancyCount)
                m_InputRedundancy.Remove(FirstInputTick());
            SendInputBatchWithinDatagram();
        }

        void SendInputBatchWithinDatagram()
        {
            var frames = new List<RollbackActorInputFrame>(m_InputRedundancy.Values);
            while (frames.Count > 0)
            {
                var batch = new RollbackActorInputBatch(frames);
                if (m_Channel.FitsSingleDatagram(batch, out int encodedBytes, out int maximumBytes))
                {
                    m_Channel.Send(batch, false);
                    return;
                }
                if (frames.Count == 1)
                {
                    throw new InvalidOperationException(
                        $"Rollback current input frame requires {encodedBytes} bytes but the unreliable payload budget is {maximumBytes} bytes.");
                }
                frames.RemoveAt(0);
            }
            throw new InvalidOperationException("Rollback input redundancy history is empty.");
        }

        ulong FirstInputTick()
        {
            foreach (ulong tick in m_InputRedundancy.Keys)
                return tick;
            throw new InvalidOperationException("Rollback input redundancy history is empty.");
        }

        public void SendStateHash(RollbackStateHashReport report)
        {
            RequireReady();
            m_Channel.Send(report ?? throw new ArgumentNullException(nameof(report)), false);
        }

        public void SendSnapshotRequest(RollbackSnapshotRequest request)
        {
            RequireReady();
            m_Channel.Send(request ?? throw new ArgumentNullException(nameof(request)), true);
        }

        public void SendSnapshotResponse(RollbackSnapshotResponse response)
        {
            RequireReady();
            m_Channel.Send(response ?? throw new ArgumentNullException(nameof(response)), true);
        }

        public bool TryReceiveCanonicalBundle(out RollbackCanonicalInputBundle bundle)
        {
            if (m_CanonicalBundles.Count == 0)
            {
                bundle = null;
                return false;
            }
            ulong tick = FirstCanonicalTick();
            bundle = m_CanonicalBundles[tick];
            m_CanonicalBundles.Remove(tick);
            return true;
        }

        public bool TryReceiveRelayedExplicit(out RollbackRelayedExplicitInputBatch batch)
        {
            if (m_RelayedExplicitInputs.Count == 0)
            {
                batch = null;
                return false;
            }
            batch = m_RelayedExplicitInputs.Dequeue();
            return true;
        }

        public bool TryReceiveControl(out IRollbackProtocolPayload payload)
        {
            if (m_ControlMessages.Count == 0)
            {
                payload = null;
                return false;
            }
            payload = m_ControlMessages.Dequeue();
            return true;
        }

        void Process(IRollbackProtocolPayload payload)
        {
            switch (payload)
            {
                case RollbackHandshake handshake:
                    if (!string.Equals(handshake.PeerId, m_Channel.RemotePeerId, StringComparison.Ordinal))
                        throw new InvalidOperationException("Rollback Relay handshake PeerId does not match the UDP channel.");
                    m_LocalHandshake.RequireCompatible(handshake);
                    m_RemoteHandshakeAccepted = true;
                    break;
                case RollbackRoster roster:
                    if (!m_RemoteHandshakeAccepted)
                        throw new InvalidOperationException("Rollback roster arrived before a compatible Relay handshake.");
                    if (Roster != null && !Roster.RosterHash.Equals(roster.RosterHash))
                        throw new InvalidOperationException("Rollback roster changed after it was locked.");
                    Roster = roster;
                    _ = GetLocalRosterEntry();
                    break;
                case RollbackCanonicalInputBundle bundle:
                    RequireReady();
                    AddCanonical(bundle);
                    break;
                case RollbackRelayedExplicitInputBatch relayed:
                    RequireReady();
                    if (m_RelayedExplicitInputs.Count >= m_Definition.MaximumQueuedMessages)
                        throw new InvalidOperationException("Rollback relayed explicit input receive capacity is exhausted.");
                    m_RelayedExplicitInputs.Enqueue(relayed);
                    break;
                case RollbackCanonicalConfirmation confirmation:
                    RequireReady();
                    ReceiveConfirmation(confirmation);
                    break;
                case RollbackStateHashReport:
                case RollbackSnapshotRequest:
                case RollbackSnapshotResponse:
                    RequireReady();
                    if (m_ControlMessages.Count >= m_Definition.MaximumQueuedMessages)
                        throw new InvalidOperationException("Rollback control message receive capacity is exhausted.");
                    m_ControlMessages.Enqueue(payload);
                    break;
                case RollbackLeave leave:
                    throw new InvalidOperationException($"Rollback Relay closed the Session: {leave.Reason}");
                default:
                    throw new InvalidOperationException($"Rollback Relay payload '{payload.Kind}' is not valid for a peer.");
            }
        }

        RollbackRosterEntry GetLocalRosterEntry()
        {
            if (Roster == null)
                throw new InvalidOperationException("Rollback roster is not locked.");
            for (int i = 0; i < Roster.Entries.Count; i++)
            {
                if (string.Equals(Roster.Entries[i].PeerId, m_LocalHandshake.PeerId, StringComparison.Ordinal))
                    return Roster.Entries[i];
            }
            throw new InvalidOperationException($"Rollback roster has no entry for Peer '{m_LocalHandshake.PeerId}'.");
        }

        void AddCanonical(RollbackCanonicalInputBundle bundle)
        {
            if (bundle.Tick.Value <= ConfirmedCanonicalTick)
                return;
            if (m_CanonicalBundles.TryGetValue(bundle.Tick.Value, out RollbackCanonicalInputBundle current))
            {
                if (current.BundleHash.Equals(bundle.BundleHash))
                    return;
                throw new InvalidOperationException($"Rollback canonical Tick '{bundle.Tick}' changed after publication.");
            }
            if (m_CanonicalBundles.Count >= m_Definition.MaximumQueuedMessages)
                throw new InvalidOperationException("Rollback canonical bundle receive capacity is exhausted.");
            m_CanonicalBundles.Add(bundle.Tick.Value, bundle);
        }

        void ReceiveConfirmation(RollbackCanonicalConfirmation confirmation)
        {
            if (confirmation.ConfirmedTick.Value <= ConfirmedCanonicalTick)
                return;
            if (confirmation.PreviousConfirmedTick < ConfirmedCanonicalTick)
                throw new InvalidOperationException("Rollback canonical confirmation overlaps the committed frontier.");
            if (m_PendingConfirmations.TryGetValue(
                    confirmation.PreviousConfirmedTick,
                    out RollbackCanonicalConfirmation pending))
            {
                if (pending.ConfirmedTick != confirmation.ConfirmedTick)
                    throw new InvalidOperationException("Rollback canonical confirmation forked its Tick range.");
                return;
            }
            if (m_PendingConfirmations.Count >= m_Definition.MaximumQueuedMessages)
                throw new InvalidOperationException("Rollback canonical confirmation capacity is exhausted.");
            m_PendingConfirmations.Add(confirmation.PreviousConfirmedTick, confirmation);
            while (m_PendingConfirmations.TryGetValue(
                       ConfirmedCanonicalTick,
                       out RollbackCanonicalConfirmation contiguous))
            {
                m_PendingConfirmations.Remove(ConfirmedCanonicalTick);
                for (int i = 0; i < contiguous.FinalBundles.Count; i++)
                {
                    RollbackCanonicalInputBundle bundle = contiguous.FinalBundles[i];
                    if (m_CanonicalBundles.TryGetValue(bundle.Tick.Value, out RollbackCanonicalInputBundle current))
                    {
                        if (current.BundleHash.Equals(bundle.BundleHash))
                            continue;
                        throw new InvalidOperationException($"Rollback confirmed Tick '{bundle.Tick}' differs from published canonical input.");
                    }
                    if (!m_CanonicalBundles.ContainsKey(bundle.Tick.Value) &&
                        m_CanonicalBundles.Count >= m_Definition.MaximumQueuedMessages)
                    {
                        throw new InvalidOperationException("Rollback canonical bundle receive capacity is exhausted.");
                    }
                    m_CanonicalBundles[bundle.Tick.Value] = bundle;
                }
                ConfirmedCanonicalTick = contiguous.ConfirmedTick.Value;
            }
        }

        ulong FirstCanonicalTick()
        {
            foreach (ulong tick in m_CanonicalBundles.Keys)
                return tick;
            throw new InvalidOperationException("Rollback canonical bundle queue is empty.");
        }

        void RequireReady()
        {
            ThrowIfDisposed();
            if (!IsReady)
                throw new InvalidOperationException("Rollback peer Endpoint is not ready.");
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(RollbackPeerEndpoint));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Endpoint.Dispose();
            m_CanonicalBundles.Clear();
            m_PendingConfirmations.Clear();
            m_ControlMessages.Clear();
            m_RelayedExplicitInputs.Clear();
            m_InputRedundancy.Clear();
        }
    }
}
