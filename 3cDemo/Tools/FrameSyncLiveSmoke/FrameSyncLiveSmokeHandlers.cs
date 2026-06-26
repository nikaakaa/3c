using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using FrameSyncLiveSmoke;

namespace Fantasy;

public sealed class G2C_FrameSyncHandshakeResponseHandler : Message<G2C_FrameSyncHandshakeResponse>
{
    protected override async FTask Run(Session session, G2C_FrameSyncHandshakeResponse message)
    {
        FrameSyncLiveSmokeProbe.Add(session.RuntimeId, message);
        await FTask.CompletedTask;
    }
}

public sealed class G2C_FrameSyncInputAckHandler : Message<G2C_FrameSyncInputAck>
{
    protected override async FTask Run(Session session, G2C_FrameSyncInputAck message)
    {
        FrameSyncLiveSmokeProbe.Add(session.RuntimeId, message);
        await FTask.CompletedTask;
    }
}

public sealed class G2C_FrameSyncConfirmedInputSetHandler : Message<G2C_FrameSyncConfirmedInputSet>
{
    protected override async FTask Run(Session session, G2C_FrameSyncConfirmedInputSet message)
    {
        FrameSyncLiveSmokeProbe.Add(session.RuntimeId, message);
        await FTask.CompletedTask;
    }
}

public sealed class G2C_FrameSyncCorrectionHandler : Message<G2C_FrameSyncCorrection>
{
    protected override async FTask Run(Session session, G2C_FrameSyncCorrection message)
    {
        FrameSyncLiveSmokeProbe.Add(session.RuntimeId, message);
        await FTask.CompletedTask;
    }
}

public sealed class G2C_FrameSyncDiagnosticHandler : Message<G2C_FrameSyncDiagnostic>
{
    protected override async FTask Run(Session session, G2C_FrameSyncDiagnostic message)
    {
        FrameSyncLiveSmokeProbe.Add(session.RuntimeId, message);
        await FTask.CompletedTask;
    }
}
