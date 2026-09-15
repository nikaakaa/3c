using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecastAuthority;

namespace Fantasy;

public sealed class DotRecastAuthoritySceneDiagnostics : ISimulationDiagnosticsSink
{
    const int Capacity = 512;
    readonly Queue<string> m_Records = new(Capacity);

    public DotRecastAuthoritySceneDiagnostics(DotRecastAuthoritySceneManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        Identity =
            $"hostProduct={manifest.HostProductId};host={manifest.HostId};route=InProcessAuthorityScene;" +
            $"program={manifest.Program.ProgramId}/{manifest.Program.ProgramHash};" +
            $"backend={manifest.Pipeline.BackendIdentity};pipeline={manifest.Pipeline.Identity};source={manifest.Pipeline.Source.Identity};" +
            $"solver={manifest.World.SolverId}@{manifest.World.SolverVersion};world={manifest.World.WorldId};" +
            $"map={manifest.World.MapId};surface={manifest.World.NavigationSurfaceContentHash};query={manifest.World.QueryProfileHash};" +
            $"scene={manifest.Scene.ProcessConfigId}/{manifest.Scene.SceneConfigId}/{manifest.Scene.SceneType}";
        Add($"identity|{Identity}");
        Log.Info($"DotRecast Authority diagnostics identity: {Identity}");
    }

    public bool IsEnabled => true;
    public string Identity { get; }
    public ulong LatestAuthorityTick { get; private set; }
    public ulong LatestAckSequence { get; private set; }
    public ulong LatestSnapshotSequence { get; private set; }
    public string LatestTransportStatus { get; private set; } = "pending";
    public string LatestFailure { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Records => m_Records;

    public void PublishBoundary(SimulationBoundaryTraceRecord record)
    {
        Add($"boundary|{record.Tick.Value}|{record.ActorId}|{record.Kind}|{record.Success}|{record.Detail}");
        if (!record.Success)
            Fail($"boundary/{record.Kind}: {record.Detail}");
    }

    public void PublishPipeline(SimulationPipelineTraceRecord record)
    {
        LatestAuthorityTick = Math.Max(LatestAuthorityTick, record.CompletedTick);
        Add($"pipeline|{record.CompletedTick}|{record.Kind}|{record.Phase}|{record.PassId}|{record.Success}|{record.Detail}");
        if (!record.Success)
            Fail($"pipeline/{record.Kind}/{record.PassId}: {record.Detail}");
    }

    public void PublishOperation(SimulationTraceRecord record)
    {
        Add($"operation|{record.Header.Tick.Value}|{record.Header.ActorId}|{record.Severity}|{record.Boundary}|{record.Code}|{record.Detail}");
        if (record.Severity == SimulationTraceSeverity.Error)
            Fail($"operation/{record.Boundary}/{record.Code}: {record.Detail}");
    }

    public void PublishModel(SimulationModelTraceRecord record)
    {
        LatestAuthorityTick = Math.Max(LatestAuthorityTick, record.AuthorityTick);
        LatestAckSequence = Math.Max(LatestAckSequence, record.AckSequence);
        LatestSnapshotSequence = Math.Max(LatestSnapshotSequence, record.SnapshotSequence);
        if (record.Kind == SimulationModelTraceKind.Transport)
            LatestTransportStatus = record.Code;
        Add(
            $"model|{record.LocalSourceTick}|{record.AuthorityTick}|{record.ActorId}|{record.Kind}|{record.Code}|" +
            $"input={record.InputSequence}|ack={record.AckSequence}|snapshot={record.SnapshotSequence}|queue={record.QueueDepth}|{record.Success}|{record.Detail}");
        if (!record.Success || record.Kind == SimulationModelTraceKind.Failure)
            Fail($"model/{record.Code}: {record.Detail}");
    }

    public void PublishWorld(SimulationWorldTraceRecord record)
    {
        LatestAuthorityTick = Math.Max(LatestAuthorityTick, record.Tick.Value);
        Add(
            $"world|{record.Tick.Value}|{record.ActorId}|{record.SolverId}@{record.SolverVersion}|{record.Kind}|" +
            $"region={record.Region}|traversal={record.TraversalCount}|{record.Success}|{record.Detail}");
        if (!record.Success || record.Kind == SimulationWorldTraceKind.Failure)
            Fail($"world/{record.Code}: {record.Detail}");
    }

    void Add(string value)
    {
        if (m_Records.Count == Capacity)
            m_Records.Dequeue();
        m_Records.Enqueue(value);
    }

    void Fail(string value)
    {
        LatestFailure = value;
        Log.Error($"DotRecast Authority diagnostics failure: {value}");
    }
}
