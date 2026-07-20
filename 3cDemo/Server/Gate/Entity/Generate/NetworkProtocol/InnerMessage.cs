using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerHostIdentity : AMessage
    {
        public static ServerAuthoritativeInnerHostIdentity Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerHostIdentity>.Rent();
        }

        public void Dispose()
        {
            HostProductId = default;
            HostId = default;
            RouteKind = default;
            RoomId = default;
            MessageObjectPool<ServerAuthoritativeInnerHostIdentity>.Return(this);
        }
        [ProtoMember(1)]
        public string HostProductId { get; set; }
        [ProtoMember(2)]
        public string HostId { get; set; }
        [ProtoMember(3)]
        public int RouteKind { get; set; }
        [ProtoMember(4)]
        public string RoomId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerProtocolIdentity : AMessage
    {
        public static ServerAuthoritativeInnerProtocolIdentity Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerProtocolIdentity>.Rent();
        }

        public void Dispose()
        {
            ModelProtocolVersion = default;
            ModelId = default;
            ModelConfigurationHash = default;
            EndpointId = default;
            MessageObjectPool<ServerAuthoritativeInnerProtocolIdentity>.Return(this);
        }
        [ProtoMember(1)]
        public uint ModelProtocolVersion { get; set; }
        [ProtoMember(2)]
        public string ModelId { get; set; }
        [ProtoMember(3)]
        public string ModelConfigurationHash { get; set; }
        [ProtoMember(4)]
        public string EndpointId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerProgramIdentity : AMessage
    {
        public static ServerAuthoritativeInnerProgramIdentity Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerProgramIdentity>.Rent();
        }

        public void Dispose()
        {
            ProgramId = default;
            ProgramHash = default;
            LayoutHash = default;
            OperationSetId = default;
            OperationSetVersion = default;
            MessageObjectPool<ServerAuthoritativeInnerProgramIdentity>.Return(this);
        }
        [ProtoMember(1)]
        public string ProgramId { get; set; }
        [ProtoMember(2)]
        public string ProgramHash { get; set; }
        [ProtoMember(3)]
        public string LayoutHash { get; set; }
        [ProtoMember(4)]
        public string OperationSetId { get; set; }
        [ProtoMember(5)]
        public string OperationSetVersion { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerPipelineIdentity : AMessage
    {
        public static ServerAuthoritativeInnerPipelineIdentity Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerPipelineIdentity>.Rent();
        }

        public void Dispose()
        {
            PipelineId = default;
            PipelineHash = default;
            BackendId = default;
            SolverId = default;
            SolverVersion = default;
            TickRate = default;
            SolverCapabilities = default;
            SolverFeatures = default;
            MessageObjectPool<ServerAuthoritativeInnerPipelineIdentity>.Return(this);
        }
        [ProtoMember(1)]
        public string PipelineId { get; set; }
        [ProtoMember(2)]
        public string PipelineHash { get; set; }
        [ProtoMember(3)]
        public string BackendId { get; set; }
        [ProtoMember(4)]
        public string SolverId { get; set; }
        [ProtoMember(5)]
        public string SolverVersion { get; set; }
        [ProtoMember(6)]
        public uint TickRate { get; set; }
        [ProtoMember(7)]
        public ulong SolverCapabilities { get; set; }
        [ProtoMember(8)]
        public ulong SolverFeatures { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerWorldIdentity : AMessage
    {
        public static ServerAuthoritativeInnerWorldIdentity Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerWorldIdentity>.Rent();
        }

        public void Dispose()
        {
            SolverId = default;
            SolverVersion = default;
            SolverCapabilities = default;
            SolverFeatures = default;
            WorldId = default;
            MapId = default;
            WorldRevision = default;
            WorldConfigurationHash = default;
            NavigationSurfaceArtifactHash = default;
            QueryProfileHash = default;
            MessageObjectPool<ServerAuthoritativeInnerWorldIdentity>.Return(this);
        }
        [ProtoMember(1)]
        public string SolverId { get; set; }
        [ProtoMember(2)]
        public string SolverVersion { get; set; }
        [ProtoMember(3)]
        public ulong SolverCapabilities { get; set; }
        [ProtoMember(4)]
        public ulong SolverFeatures { get; set; }
        [ProtoMember(5)]
        public string WorldId { get; set; }
        [ProtoMember(6)]
        public string MapId { get; set; }
        [ProtoMember(7)]
        public string WorldRevision { get; set; }
        [ProtoMember(8)]
        public string WorldConfigurationHash { get; set; }
        [ProtoMember(9)]
        public string NavigationSurfaceArtifactHash { get; set; }
        [ProtoMember(10)]
        public string QueryProfileHash { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerDataEndpoint : AMessage
    {
        public static ServerAuthoritativeInnerDataEndpoint Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerDataEndpoint>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            Port = default;
            MessageObjectPool<ServerAuthoritativeInnerDataEndpoint>.Return(this);
        }
        [ProtoMember(1)]
        public string Host { get; set; }
        [ProtoMember(2)]
        public uint Port { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerRosterMember : AMessage
    {
        public static ServerAuthoritativeInnerRosterMember Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerRosterMember>.Rent();
        }

        public void Dispose()
        {
            PlayerId = default;
            ActorId = default;
            ProcessRole = default;
            MessageObjectPool<ServerAuthoritativeInnerRosterMember>.Return(this);
        }
        [ProtoMember(1)]
        public string PlayerId { get; set; }
        [ProtoMember(2)]
        public string ActorId { get; set; }
        [ProtoMember(3)]
        public int ProcessRole { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerDataPlaneTicket : AMessage
    {
        public static ServerAuthoritativeInnerDataPlaneTicket Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerDataPlaneTicket>.Rent();
        }

        public void Dispose()
        {
            TicketId = default;
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            Host = default;
            Nonce = default;
            ExpiresAtUnixMilliseconds = default;
            MessageObjectPool<ServerAuthoritativeInnerDataPlaneTicket>.Return(this);
        }
        [ProtoMember(1)]
        public string TicketId { get; set; }
        [ProtoMember(2)]
        public string RoomId { get; set; }
        [ProtoMember(3)]
        public string SessionId { get; set; }
        [ProtoMember(4)]
        public string PlayerId { get; set; }
        [ProtoMember(5)]
        public string ActorId { get; set; }
        [ProtoMember(6)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(7)]
        public string Nonce { get; set; }
        [ProtoMember(8)]
        public long ExpiresAtUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeInnerReliableGameplayEvent : AMessage
    {
        public static ServerAuthoritativeInnerReliableGameplayEvent Create()
        {
            return MessageObjectPool<ServerAuthoritativeInnerReliableGameplayEvent>.Rent();
        }

        public void Dispose()
        {
            ActorId = default;
            EventId = default;
            EventSequence = default;
            AuthorityTick = default;
            EventKind = default;
            PayloadSchemaVersion = default;
            PayloadLength = default;
            Payload = null;
            MessageObjectPool<ServerAuthoritativeInnerReliableGameplayEvent>.Return(this);
        }
        [ProtoMember(1)]
        public string ActorId { get; set; }
        [ProtoMember(2)]
        public string EventId { get; set; }
        [ProtoMember(3)]
        public ulong EventSequence { get; set; }
        [ProtoMember(4)]
        public ulong AuthorityTick { get; set; }
        [ProtoMember(5)]
        public string EventKind { get; set; }
        [ProtoMember(6)]
        public uint PayloadSchemaVersion { get; set; }
        [ProtoMember(7)]
        public uint PayloadLength { get; set; }
        [ProtoMember(8)]
        public byte[] Payload { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneRegisterRequest : AMessage, IAddressRequest
    {
        public static A2G_ServerAuthoritativeAuthoritySceneRegisterRequest Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneRegisterRequest>.Rent();
        }

        public void Dispose()
        {
            AuthorityAddress = default;
            Host = default;
            Protocol = default;
            Program = default;
            AuthorityPipeline = default;
            PredictionPipelineId = default;
            PredictionPipelineHash = default;
            DataEndpoint = default;
            World = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneRegisterRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneRegisterRequest; } 
        [ProtoIgnore]
        public G2A_ServerAuthoritativeAuthoritySceneRegisterResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long AuthorityAddress { get; set; }
        [ProtoMember(2)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(3)]
        public ServerAuthoritativeInnerProtocolIdentity Protocol { get; set; }
        [ProtoMember(4)]
        public ServerAuthoritativeInnerProgramIdentity Program { get; set; }
        [ProtoMember(5)]
        public ServerAuthoritativeInnerPipelineIdentity AuthorityPipeline { get; set; }
        [ProtoMember(6)]
        public string PredictionPipelineId { get; set; }
        [ProtoMember(7)]
        public string PredictionPipelineHash { get; set; }
        [ProtoMember(8)]
        public ServerAuthoritativeInnerDataEndpoint DataEndpoint { get; set; }
        [ProtoMember(9)]
        public ServerAuthoritativeInnerWorldIdentity World { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneRegisterResponse : AMessage, IAddressResponse
    {
        public static G2A_ServerAuthoritativeAuthoritySceneRegisterResponse Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneRegisterResponse>.Rent();
        }

        public void Dispose()
        {
            ResultCode = default;
            RoomRevision = default;
            SessionId = default;
            FailureReason = default;
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneRegisterResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneRegisterResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public int ResultCode { get; set; }
        [ProtoMember(3)]
        public ulong RoomRevision { get; set; }
        [ProtoMember(4)]
        public string SessionId { get; set; }
        [ProtoMember(5)]
        public string FailureReason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneRosterLock : AMessage, IAddressMessage
    {
        public static G2A_ServerAuthoritativeAuthoritySceneRosterLock Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneRosterLock>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            RoomRevision = default;
            Members.Clear();
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneRosterLock>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneRosterLock; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong RoomRevision { get; set; }
        [ProtoMember(4)]
        public List<ServerAuthoritativeInnerRosterMember> Members { get; set; } = new List<ServerAuthoritativeInnerRosterMember>();
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket : AMessage, IAddressMessage
    {
        public static G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket>.Rent();
        }

        public void Dispose()
        {
            Ticket = default;
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneDataPlaneTicket; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerDataPlaneTicket Ticket { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneTicketConsumed : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneTicketConsumed Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneTicketConsumed>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            PlayerId = default;
            TicketId = default;
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneTicketConsumed>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneTicketConsumed; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string TicketId { get; set; }
        [ProtoMember(5)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneHeartbeat : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneHeartbeat Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneHeartbeat>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            Sequence = default;
            SentUnixMilliseconds = default;
            LatestAuthorityTick = default;
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneHeartbeat>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneHeartbeat; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong Sequence { get; set; }
        [ProtoMember(4)]
        public long SentUnixMilliseconds { get; set; }
        [ProtoMember(5)]
        public ulong LatestAuthorityTick { get; set; }
        [ProtoMember(6)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck : AMessage, IAddressMessage
    {
        public static G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            Sequence = default;
            SentUnixMilliseconds = default;
            ServerUnixMilliseconds = default;
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneHeartbeatAck; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong Sequence { get; set; }
        [ProtoMember(4)]
        public long SentUnixMilliseconds { get; set; }
        [ProtoMember(5)]
        public long ServerUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            RecipientActorId = default;
            Events.Clear();
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneReliableGameplayEventBatch; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string RecipientActorId { get; set; }
        [ProtoMember(4)]
        public List<ServerAuthoritativeInnerReliableGameplayEvent> Events { get; set; } = new List<ServerAuthoritativeInnerReliableGameplayEvent>();
        [ProtoMember(5)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest : AMessage, IAddressMessage
    {
        public static G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            RequestSequence = default;
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneFullCheckpointRequest; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string ActorId { get; set; }
        [ProtoMember(5)]
        public ulong RequestSequence { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            RequestSequence = default;
            AuthorityTick = default;
            ConfirmedInputSequence = default;
            ReliableEventHorizon = default;
            CheckpointLayoutHash = default;
            CheckpointHash = default;
            CheckpointLength = default;
            Checkpoint = null;
            SnapshotSequence = default;
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneFullCheckpointResponse; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string ActorId { get; set; }
        [ProtoMember(5)]
        public ulong RequestSequence { get; set; }
        [ProtoMember(6)]
        public ulong AuthorityTick { get; set; }
        [ProtoMember(7)]
        public ulong ConfirmedInputSequence { get; set; }
        [ProtoMember(8)]
        public ulong ReliableEventHorizon { get; set; }
        [ProtoMember(9)]
        public string CheckpointLayoutHash { get; set; }
        [ProtoMember(10)]
        public string CheckpointHash { get; set; }
        [ProtoMember(11)]
        public uint CheckpointLength { get; set; }
        [ProtoMember(12)]
        public byte[] Checkpoint { get; set; }
        [ProtoMember(13)]
        public ulong SnapshotSequence { get; set; }
        [ProtoMember(14)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneLeave : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneLeave Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneLeave>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            Reason = default;
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneLeave>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneLeave; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string Reason { get; set; }
        [ProtoMember(4)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class A2G_ServerAuthoritativeAuthoritySceneFailure : AMessage, IAddressMessage
    {
        public static A2G_ServerAuthoritativeAuthoritySceneFailure Create()
        {
            return MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneFailure>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            Code = default;
            Reason = default;
            AuthorityAddress = default;
            MessageObjectPool<A2G_ServerAuthoritativeAuthoritySceneFailure>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.A2G_ServerAuthoritativeAuthoritySceneFailure; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string Code { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
        [ProtoMember(5)]
        public long AuthorityAddress { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2A_ServerAuthoritativeAuthoritySceneFailure : AMessage, IAddressMessage
    {
        public static G2A_ServerAuthoritativeAuthoritySceneFailure Create()
        {
            return MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneFailure>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            SessionId = default;
            ResultCode = default;
            Reason = default;
            MessageObjectPool<G2A_ServerAuthoritativeAuthoritySceneFailure>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_ServerAuthoritativeAuthoritySceneFailure; } 
        [ProtoMember(1)]
        public ServerAuthoritativeInnerHostIdentity Host { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public int ResultCode { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

}