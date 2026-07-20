using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using static ThirdPersonGameplay.Networking.ServerAuthoritative.ServerAuthoritativeFantasyEndpointHandlerDispatch;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    public sealed class ServerAuthoritativeClientRosterChangedHandler : Message<G2C_ServerAuthoritativeRosterChanged>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeRosterChanged message)
        {
            ServerAuthoritativeRosterMessage roster = CopyRoster(message?.Roster);
            Dispatch(session, runtime => runtime.ReceiveRoster(roster));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeWorkerRosterChangedHandler : Message<G2W_ServerAuthoritativeRosterChanged>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeRosterChanged message)
        {
            ServerAuthoritativeRosterMessage roster = CopyRoster(message?.Roster);
            Dispatch(session, runtime => runtime.ReceiveRoster(roster));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeClientTicketIssuedHandler : Message<G2C_ServerAuthoritativeDataPlaneTicketIssued>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeDataPlaneTicketIssued message)
        {
            ServerAuthoritativeDataPlaneTicketMessage ticket = CopyTicket(message?.Ticket);
            Dispatch(session, runtime => runtime.ReceiveTicket(ticket));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeWorkerTicketIssuedHandler : Message<G2W_ServerAuthoritativeDataPlaneTicketIssued>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeDataPlaneTicketIssued message)
        {
            ServerAuthoritativeDataPlaneTicketMessage ticket = CopyTicket(message?.Ticket);
            Dispatch(session, runtime => runtime.ReceiveTicket(ticket));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeClientTicketRevokedHandler : Message<G2C_ServerAuthoritativeDataPlaneTicketRevoked>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeDataPlaneTicketRevoked message)
        {
            string sessionId = message?.SessionId;
            string ticketId = message?.TicketId;
            string reason = message?.Reason;
            Dispatch(session, runtime => runtime.ReceiveTicketRevoked(sessionId, ticketId, reason));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeWorkerTicketRevokedHandler : Message<G2W_ServerAuthoritativeDataPlaneTicketRevoked>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeDataPlaneTicketRevoked message)
        {
            string sessionId = message?.SessionId;
            string ticketId = message?.TicketId;
            string reason = message?.Reason;
            Dispatch(session, runtime => runtime.ReceiveTicketRevoked(sessionId, ticketId, reason));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeClientControlHeartbeatAckHandler : Message<G2C_ServerAuthoritativeControlHeartbeatAck>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeControlHeartbeatAck message)
        {
            string roomId = message?.RoomId;
            string sessionId = message?.SessionId;
            ulong sequence = message?.Sequence ?? 0;
            long clientUnixMilliseconds = message?.ClientUnixMilliseconds ?? 0;
            long serverUnixMilliseconds = message?.ServerUnixMilliseconds ?? 0;
            Dispatch(session, runtime => runtime.ReceiveControlHeartbeatAck(
                roomId,
                sessionId,
                sequence,
                clientUnixMilliseconds,
                serverUnixMilliseconds));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeWorkerControlHeartbeatAckHandler : Message<G2W_ServerAuthoritativeControlHeartbeatAck>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeControlHeartbeatAck message)
        {
            string roomId = message?.RoomId;
            string sessionId = message?.SessionId;
            ulong sequence = message?.Sequence ?? 0;
            long clientUnixMilliseconds = message?.ClientUnixMilliseconds ?? 0;
            long serverUnixMilliseconds = message?.ServerUnixMilliseconds ?? 0;
            Dispatch(session, runtime => runtime.ReceiveControlHeartbeatAck(
                roomId,
                sessionId,
                sequence,
                clientUnixMilliseconds,
                serverUnixMilliseconds));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeReliableGameplayEventHandler : Message<G2C_ServerAuthoritativeReliableGameplayEventBatch>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeReliableGameplayEventBatch message)
        {
            G2C_ServerAuthoritativeReliableGameplayEventBatch copy = CopyReliableEvents(message);
            Dispatch(session, runtime => runtime.ReceiveReliableEvents(copy));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeFullCheckpointRequestHandler : Message<G2W_ServerAuthoritativeFullCheckpointRequest>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeFullCheckpointRequest message)
        {
            G2W_ServerAuthoritativeFullCheckpointRequest copy = CopyFullCheckpointRequest(message);
            Dispatch(session, runtime => runtime.ReceiveFullCheckpointRequest(copy));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeFullCheckpointResponseHandler : Message<G2C_ServerAuthoritativeFullCheckpointResponse>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeFullCheckpointResponse message)
        {
            G2C_ServerAuthoritativeFullCheckpointResponse copy = CopyFullCheckpoint(message);
            Dispatch(session, runtime => runtime.ReceiveFullCheckpoint(copy));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeClientSessionFailedHandler : Message<G2C_ServerAuthoritativeSessionFailed>
    {
        protected override async FTask Run(Session session, G2C_ServerAuthoritativeSessionFailed message)
        {
            string sessionId = message?.SessionId;
            int resultCode = message?.ResultCode ?? 0;
            string reason = message?.Reason;
            Dispatch(session, runtime => runtime.ReceiveFailure(sessionId, resultCode, reason));
            await FTask.CompletedTask;
        }
    }

    public sealed class ServerAuthoritativeWorkerSessionFailedHandler : Message<G2W_ServerAuthoritativeSessionFailed>
    {
        protected override async FTask Run(Session session, G2W_ServerAuthoritativeSessionFailed message)
        {
            string sessionId = message?.SessionId;
            int resultCode = message?.ResultCode ?? 0;
            string reason = message?.Reason;
            Dispatch(session, runtime => runtime.ReceiveFailure(sessionId, resultCode, reason));
            await FTask.CompletedTask;
        }
    }

    static class ServerAuthoritativeFantasyEndpointHandlerDispatch
    {
        public static void Dispatch(Session session, Action<ServerAuthoritativeFantasyConnection> action)
        {
            ServerAuthoritativeEndpointSessionBinding binding = session?.GetComponent<ServerAuthoritativeEndpointSessionBinding>();
            ServerAuthoritativeFantasyConnection runtime = binding?.Runtime;
            if (runtime == null)
                throw new InvalidOperationException("Fantasy Session has no ServerAuthoritative Endpoint binding.");
            runtime.Dispatch(() => action(runtime));
        }

        public static ServerAuthoritativeRosterMessage CopyRoster(ServerAuthoritativeRosterMessage source)
        {
            if (source == null)
                return null;
            var copy = new ServerAuthoritativeRosterMessage
            {
                RoomId = source.RoomId,
                SessionId = source.SessionId,
                Revision = source.Revision,
                Locked = source.Locked,
                HostId = source.HostId
            };
            if (source.Members == null)
                return copy;
            for (int i = 0; i < source.Members.Count; i++)
            {
                ServerAuthoritativeRosterMemberMessage member = source.Members[i];
                copy.Members.Add(member == null
                    ? null
                    : new ServerAuthoritativeRosterMemberMessage
                    {
                        PlayerId = member.PlayerId,
                        ActorId = member.ActorId,
                        ProcessRole = member.ProcessRole
                    });
            }
            return copy;
        }

        public static ServerAuthoritativeDataPlaneTicketMessage CopyTicket(
            ServerAuthoritativeDataPlaneTicketMessage source)
        {
            if (source == null)
                return null;
            return new ServerAuthoritativeDataPlaneTicketMessage
            {
                TicketId = source.TicketId,
                RoomId = source.RoomId,
                SessionId = source.SessionId,
                PlayerId = source.PlayerId,
                ActorId = source.ActorId,
                HostId = source.HostId,
                AuthorityEndpoint = source.AuthorityEndpoint == null
                    ? null
                    : new ServerAuthoritativeDataEndpointMessage
                    {
                        Host = source.AuthorityEndpoint.Host,
                        Port = source.AuthorityEndpoint.Port
                    },
                Nonce = source.Nonce,
                ExpiresAtUnixMilliseconds = source.ExpiresAtUnixMilliseconds
            };
        }

        public static G2C_ServerAuthoritativeReliableGameplayEventBatch CopyReliableEvents(
            G2C_ServerAuthoritativeReliableGameplayEventBatch source)
        {
            if (source == null)
                return null;
            var copy = new G2C_ServerAuthoritativeReliableGameplayEventBatch
            {
                RoomId = source.RoomId,
                SessionId = source.SessionId
            };
            if (source.Events == null)
                return copy;
            for (int i = 0; i < source.Events.Count; i++)
            {
                ServerAuthoritativeReliableGameplayEventMessage value = source.Events[i];
                copy.Events.Add(value == null
                    ? null
                    : new ServerAuthoritativeReliableGameplayEventMessage
                    {
                        ActorId = value.ActorId,
                        EventId = value.EventId,
                        EventSequence = value.EventSequence,
                        AuthorityTick = value.AuthorityTick,
                        EventKind = value.EventKind,
                        PayloadSchemaVersion = value.PayloadSchemaVersion,
                        PayloadLength = value.PayloadLength,
                        Payload = value.Payload == null ? null : (byte[])value.Payload.Clone()
                    });
            }
            return copy;
        }

        public static G2W_ServerAuthoritativeFullCheckpointRequest CopyFullCheckpointRequest(
            G2W_ServerAuthoritativeFullCheckpointRequest source) => source == null
            ? null
            : new G2W_ServerAuthoritativeFullCheckpointRequest
            {
                RoomId = source.RoomId,
                SessionId = source.SessionId,
                PlayerId = source.PlayerId,
                ActorId = source.ActorId,
                RequestSequence = source.RequestSequence,
                LastUsableSnapshotSequence = source.LastUsableSnapshotSequence,
                Reason = source.Reason
            };

        public static G2C_ServerAuthoritativeFullCheckpointResponse CopyFullCheckpoint(
            G2C_ServerAuthoritativeFullCheckpointResponse source) => source == null
            ? null
            : new G2C_ServerAuthoritativeFullCheckpointResponse
            {
                RoomId = source.RoomId,
                SessionId = source.SessionId,
                ActorId = source.ActorId,
                AuthorityTick = source.AuthorityTick,
                ConfirmedInputSequence = source.ConfirmedInputSequence,
                ReliableEventHorizon = source.ReliableEventHorizon,
                CheckpointLayoutHash = source.CheckpointLayoutHash,
                CheckpointHash = source.CheckpointHash,
                CheckpointLength = source.CheckpointLength,
                Checkpoint = source.Checkpoint == null ? null : (byte[])source.Checkpoint.Clone(),
                SnapshotSequence = source.SnapshotSequence
            };
    }
}
