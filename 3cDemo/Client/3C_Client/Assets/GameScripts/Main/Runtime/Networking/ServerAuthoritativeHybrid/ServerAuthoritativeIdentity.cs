namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public readonly struct ServerAuthoritativeActorIdentity
    {
        public ServerAuthoritativeActorIdentity(
            string subjectActorId,
            string ownerPlayerId,
            string teamId,
            string performerActorId,
            string targetActorId)
        {
            SubjectActorId = subjectActorId ?? string.Empty;
            OwnerPlayerId = ownerPlayerId ?? string.Empty;
            TeamId = teamId ?? string.Empty;
            PerformerActorId = performerActorId ?? string.Empty;
            TargetActorId = targetActorId ?? string.Empty;
        }

        public string SubjectActorId { get; }
        public string OwnerPlayerId { get; }
        public string TeamId { get; }
        public string PerformerActorId { get; }
        public string TargetActorId { get; }
    }

    public readonly struct ServerAuthoritativePacketEnvelope
    {
        public ServerAuthoritativePacketEnvelope(
            ulong packetId,
            ServerAuthoritativeDomain syncDomain,
            ServerAuthoritativePacketKind packetKind,
            string policyId,
            ServerAuthoritativeActorIdentity identity,
            string stableId,
            ulong predictionKey,
            ulong inputSequence,
            ulong localLogicTick,
            ulong serverTick)
        {
            PacketId = packetId;
            SyncDomain = syncDomain;
            PacketKind = packetKind;
            PolicyId = policyId ?? string.Empty;
            Identity = identity;
            StableId = stableId ?? string.Empty;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            LocalLogicTick = localLogicTick;
            ServerTick = serverTick;
        }

        public ulong PacketId { get; }
        public ServerAuthoritativeDomain SyncDomain { get; }
        public ServerAuthoritativePacketKind PacketKind { get; }
        public string PolicyId { get; }
        public ServerAuthoritativeActorIdentity Identity { get; }
        public string StableId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public ulong ServerTick { get; }

        public ServerAuthoritativePacketEnvelope WithPacketId(ulong packetId)
        {
            return new ServerAuthoritativePacketEnvelope(
                packetId,
                SyncDomain,
                PacketKind,
                PolicyId,
                Identity,
                StableId,
                PredictionKey,
                InputSequence,
                LocalLogicTick,
                ServerTick);
        }

        public ServerAuthoritativePacketEnvelope WithServerTick(ulong serverTick)
        {
            return new ServerAuthoritativePacketEnvelope(
                PacketId,
                SyncDomain,
                PacketKind,
                PolicyId,
                Identity,
                StableId,
                PredictionKey,
                InputSequence,
                LocalLogicTick,
                serverTick);
        }
    }
}
