using System;

namespace ThirdPerson.Development.Gm
{
    public enum GmConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    public interface IGmCommandConnection : IDisposable
    {
        GmConnectionState State { get; }
        string Endpoint { get; }
        string StatusMessage { get; }
        GmServiceDescription Service { get; }
        void Connect();
        void Disconnect();
        void Pump();
        bool TrySend(GmCommandRequest request, out string error);
        bool TryDequeueResponse(out GmCommandResponse response);
    }
}
