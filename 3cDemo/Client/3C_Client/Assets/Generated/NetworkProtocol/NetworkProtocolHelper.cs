using System.Runtime.CompilerServices;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using System.Collections.Generic;
#pragma warning disable CS8618
namespace Fantasy
{
   public static class NetworkProtocolHelper
   {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_GuestLoginResponse> C2G_GuestLoginRequest(this Session session, C2G_GuestLoginRequest request)
		{
			return (G2C_GuestLoginResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_GuestLoginResponse> C2G_GuestLoginRequest(this Session session, string guestAccountId, string clientInstanceId, string clientBuildVersion, uint authProtocolVersion)
		{
			using var request = Fantasy.C2G_GuestLoginRequest.Create();
			request.GuestAccountId = guestAccountId;
			request.ClientInstanceId = clientInstanceId;
			request.ClientBuildVersion = clientBuildVersion;
			request.AuthProtocolVersion = authProtocolVersion;
			return (G2C_GuestLoginResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_AccountSessionReplaced(this Session session, G2C_AccountSessionReplaced message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_AccountSessionReplaced(this Session session, string reason, ulong newSessionGeneration)
		{
			using var message = Fantasy.G2C_AccountSessionReplaced.Create();
			message.Reason = reason;
			message.NewSessionGeneration = newSessionGeneration;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2W_ServerAuthoritativeAuthorityRegisterResponse> W2G_ServerAuthoritativeAuthorityRegisterRequest(this Session session, W2G_ServerAuthoritativeAuthorityRegisterRequest request)
		{
			return (G2W_ServerAuthoritativeAuthorityRegisterResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2W_ServerAuthoritativeAuthorityRegisterResponse> W2G_ServerAuthoritativeAuthorityRegisterRequest(this Session session, string roomId, ServerAuthoritativeAuthorityHostIdentityMessage host, int processRole, ServerAuthoritativeProtocolIdentityMessage protocol, ServerAuthoritativeProgramIdentityMessage program, ServerAuthoritativePipelineIdentityMessage authorityPipeline, string predictionPipelineId, string predictionPipelineHash, ServerAuthoritativeDataEndpointMessage dataEndpoint, ServerAuthoritativeWorldIdentityMessage world)
		{
			using var request = Fantasy.W2G_ServerAuthoritativeAuthorityRegisterRequest.Create();
			request.RoomId = roomId;
			request.Host = host;
			request.ProcessRole = processRole;
			request.Protocol = protocol;
			request.Program = program;
			request.AuthorityPipeline = authorityPipeline;
			request.PredictionPipelineId = predictionPipelineId;
			request.PredictionPipelineHash = predictionPipelineHash;
			request.DataEndpoint = dataEndpoint;
			request.World = world;
			return (G2W_ServerAuthoritativeAuthorityRegisterResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ServerAuthoritativeClientJoinResponse> C2G_ServerAuthoritativeClientJoinRequest(this Session session, C2G_ServerAuthoritativeClientJoinRequest request)
		{
			return (G2C_ServerAuthoritativeClientJoinResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static async FTask<G2C_ServerAuthoritativeClientJoinResponse> C2G_ServerAuthoritativeClientJoinRequest(this Session session, string roomId, string playerId, int processRole, ServerAuthoritativeProtocolIdentityMessage protocol, ServerAuthoritativeProgramIdentityMessage program, string predictionPipelineId, string predictionPipelineHash, ServerAuthoritativeWorldIdentityMessage predictionWorld)
		{
			using var request = Fantasy.C2G_ServerAuthoritativeClientJoinRequest.Create();
			request.RoomId = roomId;
			request.PlayerId = playerId;
			request.ProcessRole = processRole;
			request.Protocol = protocol;
			request.Program = program;
			request.PredictionPipelineId = predictionPipelineId;
			request.PredictionPipelineHash = predictionPipelineHash;
			request.PredictionWorld = predictionWorld;
			return (G2C_ServerAuthoritativeClientJoinResponse)await session.Call(request);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeClientJoinAccepted(this Session session, C2G_ServerAuthoritativeClientJoinAccepted message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeClientJoinAccepted(this Session session, string roomId, string sessionId, string playerId, string hostId)
		{
			using var message = Fantasy.C2G_ServerAuthoritativeClientJoinAccepted.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.HostId = hostId;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeRosterChanged(this Session session, G2C_ServerAuthoritativeRosterChanged message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeRosterChanged(this Session session, ServerAuthoritativeRosterMessage roster)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeRosterChanged.Create();
			message.Roster = roster;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeRosterChanged(this Session session, G2W_ServerAuthoritativeRosterChanged message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeRosterChanged(this Session session, ServerAuthoritativeRosterMessage roster)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeRosterChanged.Create();
			message.Roster = roster;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeDataPlaneTicketIssued(this Session session, G2C_ServerAuthoritativeDataPlaneTicketIssued message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeDataPlaneTicketIssued(this Session session, ServerAuthoritativeDataPlaneTicketMessage ticket)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeDataPlaneTicketIssued.Create();
			message.Ticket = ticket;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeDataPlaneTicketIssued(this Session session, G2W_ServerAuthoritativeDataPlaneTicketIssued message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeDataPlaneTicketIssued(this Session session, ServerAuthoritativeDataPlaneTicketMessage ticket)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeDataPlaneTicketIssued.Create();
			message.Ticket = ticket;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeDataPlaneTicketConsumed(this Session session, C2G_ServerAuthoritativeDataPlaneTicketConsumed message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeDataPlaneTicketConsumed(this Session session, string roomId, string sessionId, string playerId, string ticketId)
		{
			using var message = Fantasy.C2G_ServerAuthoritativeDataPlaneTicketConsumed.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.TicketId = ticketId;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeDataPlaneTicketConsumed(this Session session, W2G_ServerAuthoritativeDataPlaneTicketConsumed message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeDataPlaneTicketConsumed(this Session session, string roomId, string sessionId, string hostId, string playerId, string ticketId)
		{
			using var message = Fantasy.W2G_ServerAuthoritativeDataPlaneTicketConsumed.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.HostId = hostId;
			message.PlayerId = playerId;
			message.TicketId = ticketId;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeDataPlaneTicketRevoked(this Session session, G2C_ServerAuthoritativeDataPlaneTicketRevoked message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeDataPlaneTicketRevoked(this Session session, string roomId, string sessionId, string ticketId, string reason)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeDataPlaneTicketRevoked.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.TicketId = ticketId;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeDataPlaneTicketRevoked(this Session session, G2W_ServerAuthoritativeDataPlaneTicketRevoked message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeDataPlaneTicketRevoked(this Session session, string roomId, string sessionId, string ticketId, string reason)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeDataPlaneTicketRevoked.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.TicketId = ticketId;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeControlHeartbeat(this Session session, C2G_ServerAuthoritativeControlHeartbeat message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeControlHeartbeat(this Session session, string roomId, string sessionId, string playerId, ulong sequence, long clientUnixMilliseconds)
		{
			using var message = Fantasy.C2G_ServerAuthoritativeControlHeartbeat.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.Sequence = sequence;
			message.ClientUnixMilliseconds = clientUnixMilliseconds;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeControlHeartbeatAck(this Session session, G2C_ServerAuthoritativeControlHeartbeatAck message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeControlHeartbeatAck(this Session session, string roomId, string sessionId, ulong sequence, long clientUnixMilliseconds, long serverUnixMilliseconds)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeControlHeartbeatAck.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.Sequence = sequence;
			message.ClientUnixMilliseconds = clientUnixMilliseconds;
			message.ServerUnixMilliseconds = serverUnixMilliseconds;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeControlHeartbeat(this Session session, W2G_ServerAuthoritativeControlHeartbeat message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeControlHeartbeat(this Session session, string roomId, string sessionId, string hostId, ulong sequence, long clientUnixMilliseconds)
		{
			using var message = Fantasy.W2G_ServerAuthoritativeControlHeartbeat.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.HostId = hostId;
			message.Sequence = sequence;
			message.ClientUnixMilliseconds = clientUnixMilliseconds;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeControlHeartbeatAck(this Session session, G2W_ServerAuthoritativeControlHeartbeatAck message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeControlHeartbeatAck(this Session session, string roomId, string sessionId, ulong sequence, long clientUnixMilliseconds, long serverUnixMilliseconds)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeControlHeartbeatAck.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.Sequence = sequence;
			message.ClientUnixMilliseconds = clientUnixMilliseconds;
			message.ServerUnixMilliseconds = serverUnixMilliseconds;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeReliableGameplayEventBatch(this Session session, W2G_ServerAuthoritativeReliableGameplayEventBatch message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeReliableGameplayEventBatch(this Session session, string roomId, string sessionId, string recipientActorId, List<ServerAuthoritativeReliableGameplayEventMessage> events)
		{
			using var message = Fantasy.W2G_ServerAuthoritativeReliableGameplayEventBatch.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.RecipientActorId = recipientActorId;
			message.Events = events;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeReliableGameplayEventBatch(this Session session, G2C_ServerAuthoritativeReliableGameplayEventBatch message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeReliableGameplayEventBatch(this Session session, string roomId, string sessionId, List<ServerAuthoritativeReliableGameplayEventMessage> events)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeReliableGameplayEventBatch.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.Events = events;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeFullCheckpointRequest(this Session session, C2G_ServerAuthoritativeFullCheckpointRequest message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeFullCheckpointRequest(this Session session, string roomId, string sessionId, string playerId, string actorId, ulong lastUsableSnapshotSequence, string reason)
		{
			using var message = Fantasy.C2G_ServerAuthoritativeFullCheckpointRequest.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.ActorId = actorId;
			message.LastUsableSnapshotSequence = lastUsableSnapshotSequence;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeFullCheckpointRequest(this Session session, G2W_ServerAuthoritativeFullCheckpointRequest message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeFullCheckpointRequest(this Session session, string roomId, string sessionId, string playerId, string actorId, ulong requestSequence, ulong lastUsableSnapshotSequence, string reason)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeFullCheckpointRequest.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.ActorId = actorId;
			message.RequestSequence = requestSequence;
			message.LastUsableSnapshotSequence = lastUsableSnapshotSequence;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeFullCheckpointResponse(this Session session, W2G_ServerAuthoritativeFullCheckpointResponse message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeFullCheckpointResponse(this Session session, string roomId, string sessionId, string playerId, string actorId, ulong requestSequence, ulong authorityTick, ulong confirmedInputSequence, ulong reliableEventHorizon, string checkpointLayoutHash, string checkpointHash, uint checkpointLength, byte[] checkpoint, ulong snapshotSequence)
		{
			using var message = Fantasy.W2G_ServerAuthoritativeFullCheckpointResponse.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.ActorId = actorId;
			message.RequestSequence = requestSequence;
			message.AuthorityTick = authorityTick;
			message.ConfirmedInputSequence = confirmedInputSequence;
			message.ReliableEventHorizon = reliableEventHorizon;
			message.CheckpointLayoutHash = checkpointLayoutHash;
			message.CheckpointHash = checkpointHash;
			message.CheckpointLength = checkpointLength;
			message.Checkpoint = checkpoint;
			message.SnapshotSequence = snapshotSequence;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeFullCheckpointResponse(this Session session, G2C_ServerAuthoritativeFullCheckpointResponse message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeFullCheckpointResponse(this Session session, string roomId, string sessionId, string actorId, ulong authorityTick, ulong confirmedInputSequence, ulong reliableEventHorizon, string checkpointLayoutHash, string checkpointHash, uint checkpointLength, byte[] checkpoint, ulong snapshotSequence)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeFullCheckpointResponse.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.ActorId = actorId;
			message.AuthorityTick = authorityTick;
			message.ConfirmedInputSequence = confirmedInputSequence;
			message.ReliableEventHorizon = reliableEventHorizon;
			message.CheckpointLayoutHash = checkpointLayoutHash;
			message.CheckpointHash = checkpointHash;
			message.CheckpointLength = checkpointLength;
			message.Checkpoint = checkpoint;
			message.SnapshotSequence = snapshotSequence;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeSessionFailed(this Session session, G2C_ServerAuthoritativeSessionFailed message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2C_ServerAuthoritativeSessionFailed(this Session session, string roomId, string sessionId, int resultCode, string reason)
		{
			using var message = Fantasy.G2C_ServerAuthoritativeSessionFailed.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.ResultCode = resultCode;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeSessionFailed(this Session session, G2W_ServerAuthoritativeSessionFailed message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void G2W_ServerAuthoritativeSessionFailed(this Session session, string roomId, string sessionId, int resultCode, string reason)
		{
			using var message = Fantasy.G2W_ServerAuthoritativeSessionFailed.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.ResultCode = resultCode;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeLeave(this Session session, C2G_ServerAuthoritativeLeave message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void C2G_ServerAuthoritativeLeave(this Session session, string roomId, string sessionId, string playerId, string reason)
		{
			using var message = Fantasy.C2G_ServerAuthoritativeLeave.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.PlayerId = playerId;
			message.Reason = reason;
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeLeave(this Session session, W2G_ServerAuthoritativeLeave message)
		{
			session.Send(message);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void W2G_ServerAuthoritativeLeave(this Session session, string roomId, string sessionId, string hostId, string reason)
		{
			using var message = Fantasy.W2G_ServerAuthoritativeLeave.Create();
			message.RoomId = roomId;
			message.SessionId = sessionId;
			message.HostId = hostId;
			message.Reason = reason;
			session.Send(message);
		}

   }
}