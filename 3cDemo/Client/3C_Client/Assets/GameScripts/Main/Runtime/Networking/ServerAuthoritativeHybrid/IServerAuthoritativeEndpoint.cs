using System;
using System.Collections.Generic;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public interface IServerAuthoritativeEndpoint : IDisposable
    {
        IReadOnlyList<ServerAuthoritativeDebugRecord> PendingDebugRecords { get; }
        IReadOnlyList<ServerAuthoritativeDebugRecord> DroppedDebugRecords { get; }
        void EnqueueOutgoing(ServerAuthoritativePacket packet);
        void Pump(ulong localLogicTick);
        bool TryDequeueIncoming(out ServerAuthoritativePacket packet);
    }
}
