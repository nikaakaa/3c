using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class C2G_GuestLoginRequest : AMessage, IRequest
    {
        public static C2G_GuestLoginRequest Create()
        {
            return MessageObjectPool<C2G_GuestLoginRequest>.Rent();
        }

        public void Dispose()
        {
            GuestAccountId = default;
            ClientInstanceId = default;
            ClientBuildVersion = default;
            AuthProtocolVersion = default;
            MessageObjectPool<C2G_GuestLoginRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_GuestLoginRequest; } 
        [ProtoIgnore]
        public G2C_GuestLoginResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string GuestAccountId { get; set; }
        [ProtoMember(2)]
        public string ClientInstanceId { get; set; }
        [ProtoMember(3)]
        public string ClientBuildVersion { get; set; }
        [ProtoMember(4)]
        public uint AuthProtocolVersion { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_GuestLoginResponse : AMessage, IResponse
    {
        public static G2C_GuestLoginResponse Create()
        {
            return MessageObjectPool<G2C_GuestLoginResponse>.Rent();
        }

        public void Dispose()
        {
            ResultCode = default;
            AccountId = default;
            SessionGeneration = default;
            SessionToken = default;
            TokenExpiresAt = default;
            MessageObjectPool<G2C_GuestLoginResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_GuestLoginResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public int ResultCode { get; set; }
        [ProtoMember(3)]
        public string AccountId { get; set; }
        [ProtoMember(4)]
        public ulong SessionGeneration { get; set; }
        [ProtoMember(5)]
        public string SessionToken { get; set; }
        [ProtoMember(6)]
        public long TokenExpiresAt { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_AccountSessionReplaced : AMessage, IMessage
    {
        public static G2C_AccountSessionReplaced Create()
        {
            return MessageObjectPool<G2C_AccountSessionReplaced>.Rent();
        }

        public void Dispose()
        {
            Reason = default;
            NewSessionGeneration = default;
            MessageObjectPool<G2C_AccountSessionReplaced>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_AccountSessionReplaced; } 
        [ProtoMember(1)]
        public string Reason { get; set; }
        [ProtoMember(2)]
        public ulong NewSessionGeneration { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeProtocolIdentityMessage : AMessage
    {
        public static ServerAuthoritativeProtocolIdentityMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeProtocolIdentityMessage>.Rent();
        }

        public void Dispose()
        {
            ModelProtocolVersion = default;
            ModelId = default;
            ModelConfigurationHash = default;
            EndpointId = default;
            MessageObjectPool<ServerAuthoritativeProtocolIdentityMessage>.Return(this);
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
    public partial class ServerAuthoritativeProgramIdentityMessage : AMessage
    {
        public static ServerAuthoritativeProgramIdentityMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeProgramIdentityMessage>.Rent();
        }

        public void Dispose()
        {
            ProgramId = default;
            ProgramHash = default;
            LayoutHash = default;
            OperationSetId = default;
            OperationSetVersion = default;
            MessageObjectPool<ServerAuthoritativeProgramIdentityMessage>.Return(this);
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
    public partial class ServerAuthoritativePipelineIdentityMessage : AMessage
    {
        public static ServerAuthoritativePipelineIdentityMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativePipelineIdentityMessage>.Rent();
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
            MessageObjectPool<ServerAuthoritativePipelineIdentityMessage>.Return(this);
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
    public partial class ServerAuthoritativeAuthorityHostIdentityMessage : AMessage
    {
        public static ServerAuthoritativeAuthorityHostIdentityMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeAuthorityHostIdentityMessage>.Rent();
        }

        public void Dispose()
        {
            HostProductId = default;
            HostId = default;
            RouteKind = default;
            RoomId = default;
            MessageObjectPool<ServerAuthoritativeAuthorityHostIdentityMessage>.Return(this);
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
    public partial class ServerAuthoritativeWorldIdentityMessage : AMessage
    {
        public static ServerAuthoritativeWorldIdentityMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeWorldIdentityMessage>.Rent();
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
            MessageObjectPool<ServerAuthoritativeWorldIdentityMessage>.Return(this);
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
    public partial class ServerAuthoritativeDataEndpointMessage : AMessage
    {
        public static ServerAuthoritativeDataEndpointMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeDataEndpointMessage>.Rent();
        }

        public void Dispose()
        {
            Host = default;
            Port = default;
            MessageObjectPool<ServerAuthoritativeDataEndpointMessage>.Return(this);
        }
        [ProtoMember(1)]
        public string Host { get; set; }
        [ProtoMember(2)]
        public uint Port { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeRosterMemberMessage : AMessage
    {
        public static ServerAuthoritativeRosterMemberMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeRosterMemberMessage>.Rent();
        }

        public void Dispose()
        {
            PlayerId = default;
            ActorId = default;
            ProcessRole = default;
            MessageObjectPool<ServerAuthoritativeRosterMemberMessage>.Return(this);
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
    public partial class ServerAuthoritativeRosterMessage : AMessage
    {
        public static ServerAuthoritativeRosterMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeRosterMessage>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            Revision = default;
            Members.Clear();
            Locked = default;
            HostId = default;
            MessageObjectPool<ServerAuthoritativeRosterMessage>.Return(this);
        }
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong Revision { get; set; }
        [ProtoMember(4)]
        public List<ServerAuthoritativeRosterMemberMessage> Members { get; set; } = new List<ServerAuthoritativeRosterMemberMessage>();
        [ProtoMember(5)]
        public bool Locked { get; set; }
        [ProtoMember(6)]
        public string HostId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeDataPlaneTicketMessage : AMessage
    {
        public static ServerAuthoritativeDataPlaneTicketMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeDataPlaneTicketMessage>.Rent();
        }

        public void Dispose()
        {
            TicketId = default;
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            HostId = default;
            AuthorityEndpoint = default;
            Nonce = default;
            ExpiresAtUnixMilliseconds = default;
            MessageObjectPool<ServerAuthoritativeDataPlaneTicketMessage>.Return(this);
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
        public string HostId { get; set; }
        [ProtoMember(7)]
        public ServerAuthoritativeDataEndpointMessage AuthorityEndpoint { get; set; }
        [ProtoMember(8)]
        public string Nonce { get; set; }
        [ProtoMember(9)]
        public long ExpiresAtUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class ServerAuthoritativeReliableGameplayEventMessage : AMessage
    {
        public static ServerAuthoritativeReliableGameplayEventMessage Create()
        {
            return MessageObjectPool<ServerAuthoritativeReliableGameplayEventMessage>.Rent();
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
            MessageObjectPool<ServerAuthoritativeReliableGameplayEventMessage>.Return(this);
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
    public partial class W2G_ServerAuthoritativeAuthorityRegisterRequest : AMessage, IRequest
    {
        public static W2G_ServerAuthoritativeAuthorityRegisterRequest Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeAuthorityRegisterRequest>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            Host = default;
            ProcessRole = default;
            Protocol = default;
            Program = default;
            AuthorityPipeline = default;
            PredictionPipelineId = default;
            PredictionPipelineHash = default;
            DataEndpoint = default;
            World = default;
            MessageObjectPool<W2G_ServerAuthoritativeAuthorityRegisterRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeAuthorityRegisterRequest; } 
        [ProtoIgnore]
        public G2W_ServerAuthoritativeAuthorityRegisterResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public ServerAuthoritativeAuthorityHostIdentityMessage Host { get; set; }
        [ProtoMember(3)]
        public int ProcessRole { get; set; }
        [ProtoMember(4)]
        public ServerAuthoritativeProtocolIdentityMessage Protocol { get; set; }
        [ProtoMember(5)]
        public ServerAuthoritativeProgramIdentityMessage Program { get; set; }
        [ProtoMember(6)]
        public ServerAuthoritativePipelineIdentityMessage AuthorityPipeline { get; set; }
        [ProtoMember(7)]
        public string PredictionPipelineId { get; set; }
        [ProtoMember(8)]
        public string PredictionPipelineHash { get; set; }
        [ProtoMember(9)]
        public ServerAuthoritativeDataEndpointMessage DataEndpoint { get; set; }
        [ProtoMember(10)]
        public ServerAuthoritativeWorldIdentityMessage World { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeAuthorityRegisterResponse : AMessage, IResponse
    {
        public static G2W_ServerAuthoritativeAuthorityRegisterResponse Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeAuthorityRegisterResponse>.Rent();
        }

        public void Dispose()
        {
            ResultCode = default;
            RoomRevision = default;
            SessionId = default;
            FailureReason = default;
            MessageObjectPool<G2W_ServerAuthoritativeAuthorityRegisterResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeAuthorityRegisterResponse; } 
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
    public partial class C2G_ServerAuthoritativeClientJoinRequest : AMessage, IRequest
    {
        public static C2G_ServerAuthoritativeClientJoinRequest Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeClientJoinRequest>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            PlayerId = default;
            ProcessRole = default;
            Protocol = default;
            Program = default;
            PredictionPipelineId = default;
            PredictionPipelineHash = default;
            PredictionWorld = default;
            MessageObjectPool<C2G_ServerAuthoritativeClientJoinRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeClientJoinRequest; } 
        [ProtoIgnore]
        public G2C_ServerAuthoritativeClientJoinResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string PlayerId { get; set; }
        [ProtoMember(3)]
        public int ProcessRole { get; set; }
        [ProtoMember(4)]
        public ServerAuthoritativeProtocolIdentityMessage Protocol { get; set; }
        [ProtoMember(5)]
        public ServerAuthoritativeProgramIdentityMessage Program { get; set; }
        [ProtoMember(6)]
        public string PredictionPipelineId { get; set; }
        [ProtoMember(7)]
        public string PredictionPipelineHash { get; set; }
        [ProtoMember(8)]
        public ServerAuthoritativeWorldIdentityMessage PredictionWorld { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeClientJoinResponse : AMessage, IResponse
    {
        public static G2C_ServerAuthoritativeClientJoinResponse Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeClientJoinResponse>.Rent();
        }

        public void Dispose()
        {
            ResultCode = default;
            SessionId = default;
            OwnedActorId = default;
            Roster = default;
            LatestAuthorityTick = default;
            FailureReason = default;
            AuthorityHost = default;
            AuthorityWorld = default;
            AuthorityPipeline = default;
            MessageObjectPool<G2C_ServerAuthoritativeClientJoinResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeClientJoinResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public int ResultCode { get; set; }
        [ProtoMember(3)]
        public string SessionId { get; set; }
        [ProtoMember(4)]
        public string OwnedActorId { get; set; }
        [ProtoMember(5)]
        public ServerAuthoritativeRosterMessage Roster { get; set; }
        [ProtoMember(6)]
        public ulong LatestAuthorityTick { get; set; }
        [ProtoMember(7)]
        public string FailureReason { get; set; }
        [ProtoMember(8)]
        public ServerAuthoritativeAuthorityHostIdentityMessage AuthorityHost { get; set; }
        [ProtoMember(9)]
        public ServerAuthoritativeWorldIdentityMessage AuthorityWorld { get; set; }
        [ProtoMember(10)]
        public ServerAuthoritativePipelineIdentityMessage AuthorityPipeline { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_ServerAuthoritativeClientJoinAccepted : AMessage, IMessage
    {
        public static C2G_ServerAuthoritativeClientJoinAccepted Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeClientJoinAccepted>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            HostId = default;
            MessageObjectPool<C2G_ServerAuthoritativeClientJoinAccepted>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeClientJoinAccepted; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string HostId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeRosterChanged : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeRosterChanged Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeRosterChanged>.Rent();
        }

        public void Dispose()
        {
            Roster = default;
            MessageObjectPool<G2C_ServerAuthoritativeRosterChanged>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeRosterChanged; } 
        [ProtoMember(1)]
        public ServerAuthoritativeRosterMessage Roster { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeRosterChanged : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeRosterChanged Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeRosterChanged>.Rent();
        }

        public void Dispose()
        {
            Roster = default;
            MessageObjectPool<G2W_ServerAuthoritativeRosterChanged>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeRosterChanged; } 
        [ProtoMember(1)]
        public ServerAuthoritativeRosterMessage Roster { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeDataPlaneTicketIssued : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeDataPlaneTicketIssued Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeDataPlaneTicketIssued>.Rent();
        }

        public void Dispose()
        {
            Ticket = default;
            MessageObjectPool<G2C_ServerAuthoritativeDataPlaneTicketIssued>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeDataPlaneTicketIssued; } 
        [ProtoMember(1)]
        public ServerAuthoritativeDataPlaneTicketMessage Ticket { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeDataPlaneTicketIssued : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeDataPlaneTicketIssued Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeDataPlaneTicketIssued>.Rent();
        }

        public void Dispose()
        {
            Ticket = default;
            MessageObjectPool<G2W_ServerAuthoritativeDataPlaneTicketIssued>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeDataPlaneTicketIssued; } 
        [ProtoMember(1)]
        public ServerAuthoritativeDataPlaneTicketMessage Ticket { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_ServerAuthoritativeDataPlaneTicketConsumed : AMessage, IMessage
    {
        public static C2G_ServerAuthoritativeDataPlaneTicketConsumed Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeDataPlaneTicketConsumed>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            TicketId = default;
            MessageObjectPool<C2G_ServerAuthoritativeDataPlaneTicketConsumed>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeDataPlaneTicketConsumed; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string TicketId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class W2G_ServerAuthoritativeDataPlaneTicketConsumed : AMessage, IMessage
    {
        public static W2G_ServerAuthoritativeDataPlaneTicketConsumed Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeDataPlaneTicketConsumed>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            HostId = default;
            PlayerId = default;
            TicketId = default;
            MessageObjectPool<W2G_ServerAuthoritativeDataPlaneTicketConsumed>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeDataPlaneTicketConsumed; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string HostId { get; set; }
        [ProtoMember(4)]
        public string PlayerId { get; set; }
        [ProtoMember(5)]
        public string TicketId { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeDataPlaneTicketRevoked : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeDataPlaneTicketRevoked Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeDataPlaneTicketRevoked>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            TicketId = default;
            Reason = default;
            MessageObjectPool<G2C_ServerAuthoritativeDataPlaneTicketRevoked>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeDataPlaneTicketRevoked; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string TicketId { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeDataPlaneTicketRevoked : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeDataPlaneTicketRevoked Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeDataPlaneTicketRevoked>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            TicketId = default;
            Reason = default;
            MessageObjectPool<G2W_ServerAuthoritativeDataPlaneTicketRevoked>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeDataPlaneTicketRevoked; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string TicketId { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_ServerAuthoritativeControlHeartbeat : AMessage, IMessage
    {
        public static C2G_ServerAuthoritativeControlHeartbeat Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeControlHeartbeat>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            Sequence = default;
            ClientUnixMilliseconds = default;
            MessageObjectPool<C2G_ServerAuthoritativeControlHeartbeat>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeControlHeartbeat; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public ulong Sequence { get; set; }
        [ProtoMember(5)]
        public long ClientUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeControlHeartbeatAck : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeControlHeartbeatAck Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeControlHeartbeatAck>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            Sequence = default;
            ClientUnixMilliseconds = default;
            ServerUnixMilliseconds = default;
            MessageObjectPool<G2C_ServerAuthoritativeControlHeartbeatAck>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeControlHeartbeatAck; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong Sequence { get; set; }
        [ProtoMember(4)]
        public long ClientUnixMilliseconds { get; set; }
        [ProtoMember(5)]
        public long ServerUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class W2G_ServerAuthoritativeControlHeartbeat : AMessage, IMessage
    {
        public static W2G_ServerAuthoritativeControlHeartbeat Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeControlHeartbeat>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            HostId = default;
            Sequence = default;
            ClientUnixMilliseconds = default;
            MessageObjectPool<W2G_ServerAuthoritativeControlHeartbeat>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeControlHeartbeat; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string HostId { get; set; }
        [ProtoMember(4)]
        public ulong Sequence { get; set; }
        [ProtoMember(5)]
        public long ClientUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeControlHeartbeatAck : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeControlHeartbeatAck Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeControlHeartbeatAck>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            Sequence = default;
            ClientUnixMilliseconds = default;
            ServerUnixMilliseconds = default;
            MessageObjectPool<G2W_ServerAuthoritativeControlHeartbeatAck>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeControlHeartbeatAck; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public ulong Sequence { get; set; }
        [ProtoMember(4)]
        public long ClientUnixMilliseconds { get; set; }
        [ProtoMember(5)]
        public long ServerUnixMilliseconds { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class W2G_ServerAuthoritativeReliableGameplayEventBatch : AMessage, IMessage
    {
        public static W2G_ServerAuthoritativeReliableGameplayEventBatch Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeReliableGameplayEventBatch>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            RecipientActorId = default;
            Events.Clear();
            MessageObjectPool<W2G_ServerAuthoritativeReliableGameplayEventBatch>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeReliableGameplayEventBatch; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string RecipientActorId { get; set; }
        [ProtoMember(4)]
        public List<ServerAuthoritativeReliableGameplayEventMessage> Events { get; set; } = new List<ServerAuthoritativeReliableGameplayEventMessage>();
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeReliableGameplayEventBatch : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeReliableGameplayEventBatch Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeReliableGameplayEventBatch>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            Events.Clear();
            MessageObjectPool<G2C_ServerAuthoritativeReliableGameplayEventBatch>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeReliableGameplayEventBatch; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public List<ServerAuthoritativeReliableGameplayEventMessage> Events { get; set; } = new List<ServerAuthoritativeReliableGameplayEventMessage>();
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_ServerAuthoritativeFullCheckpointRequest : AMessage, IMessage
    {
        public static C2G_ServerAuthoritativeFullCheckpointRequest Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeFullCheckpointRequest>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            LastUsableSnapshotSequence = default;
            Reason = default;
            MessageObjectPool<C2G_ServerAuthoritativeFullCheckpointRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeFullCheckpointRequest; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string ActorId { get; set; }
        [ProtoMember(5)]
        public ulong LastUsableSnapshotSequence { get; set; }
        [ProtoMember(6)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeFullCheckpointRequest : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeFullCheckpointRequest Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeFullCheckpointRequest>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            ActorId = default;
            RequestSequence = default;
            LastUsableSnapshotSequence = default;
            Reason = default;
            MessageObjectPool<G2W_ServerAuthoritativeFullCheckpointRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeFullCheckpointRequest; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string ActorId { get; set; }
        [ProtoMember(5)]
        public ulong RequestSequence { get; set; }
        [ProtoMember(6)]
        public ulong LastUsableSnapshotSequence { get; set; }
        [ProtoMember(7)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class W2G_ServerAuthoritativeFullCheckpointResponse : AMessage, IMessage
    {
        public static W2G_ServerAuthoritativeFullCheckpointResponse Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeFullCheckpointResponse>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
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
            MessageObjectPool<W2G_ServerAuthoritativeFullCheckpointResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeFullCheckpointResponse; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
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
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeFullCheckpointResponse : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeFullCheckpointResponse Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeFullCheckpointResponse>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            ActorId = default;
            AuthorityTick = default;
            ConfirmedInputSequence = default;
            ReliableEventHorizon = default;
            CheckpointLayoutHash = default;
            CheckpointHash = default;
            CheckpointLength = default;
            Checkpoint = null;
            SnapshotSequence = default;
            MessageObjectPool<G2C_ServerAuthoritativeFullCheckpointResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeFullCheckpointResponse; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string ActorId { get; set; }
        [ProtoMember(4)]
        public ulong AuthorityTick { get; set; }
        [ProtoMember(5)]
        public ulong ConfirmedInputSequence { get; set; }
        [ProtoMember(6)]
        public ulong ReliableEventHorizon { get; set; }
        [ProtoMember(7)]
        public string CheckpointLayoutHash { get; set; }
        [ProtoMember(8)]
        public string CheckpointHash { get; set; }
        [ProtoMember(9)]
        public uint CheckpointLength { get; set; }
        [ProtoMember(10)]
        public byte[] Checkpoint { get; set; }
        [ProtoMember(11)]
        public ulong SnapshotSequence { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2C_ServerAuthoritativeSessionFailed : AMessage, IMessage
    {
        public static G2C_ServerAuthoritativeSessionFailed Create()
        {
            return MessageObjectPool<G2C_ServerAuthoritativeSessionFailed>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            ResultCode = default;
            Reason = default;
            MessageObjectPool<G2C_ServerAuthoritativeSessionFailed>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ServerAuthoritativeSessionFailed; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public int ResultCode { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class G2W_ServerAuthoritativeSessionFailed : AMessage, IMessage
    {
        public static G2W_ServerAuthoritativeSessionFailed Create()
        {
            return MessageObjectPool<G2W_ServerAuthoritativeSessionFailed>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            ResultCode = default;
            Reason = default;
            MessageObjectPool<G2W_ServerAuthoritativeSessionFailed>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2W_ServerAuthoritativeSessionFailed; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public int ResultCode { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class C2G_ServerAuthoritativeLeave : AMessage, IMessage
    {
        public static C2G_ServerAuthoritativeLeave Create()
        {
            return MessageObjectPool<C2G_ServerAuthoritativeLeave>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            PlayerId = default;
            Reason = default;
            MessageObjectPool<C2G_ServerAuthoritativeLeave>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ServerAuthoritativeLeave; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string PlayerId { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public partial class W2G_ServerAuthoritativeLeave : AMessage, IMessage
    {
        public static W2G_ServerAuthoritativeLeave Create()
        {
            return MessageObjectPool<W2G_ServerAuthoritativeLeave>.Rent();
        }

        public void Dispose()
        {
            RoomId = default;
            SessionId = default;
            HostId = default;
            Reason = default;
            MessageObjectPool<W2G_ServerAuthoritativeLeave>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.W2G_ServerAuthoritativeLeave; } 
        [ProtoMember(1)]
        public string RoomId { get; set; }
        [ProtoMember(2)]
        public string SessionId { get; set; }
        [ProtoMember(3)]
        public string HostId { get; set; }
        [ProtoMember(4)]
        public string Reason { get; set; }
    }

}