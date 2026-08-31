namespace ThirdPerson.Development.Gm.Rollback;

public interface IRollbackGmQuerySource
{
    Task<RollbackGmSessionSnapshot> CaptureSessionAsync(CancellationToken cancellation);
    Task<RollbackGmActorSnapshot[]> CaptureActorsAsync(CancellationToken cancellation);
    Task<RollbackGmRuntimeSnapshot> CaptureRuntimeAsync(CancellationToken cancellation);
}
