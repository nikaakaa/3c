using Fantasy.Entitas;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecastAuthority;
using ThirdPersonSimulation.ServerAuthoritative;

namespace Fantasy;

public sealed class DotRecastAuthorityHost : Entity
{
    public string ServerPublishRoot = string.Empty;
    public string ManifestPath = string.Empty;
    public long GateSceneAddress;
    public LoadedDotRecastAuthoritySceneManifest? LoadedManifest;
    public IServerAuthoritativeAuthorityControlTransport? ControlTransport;
    public DotRecastAuthoritySceneRuntime? Runtime;
    public DotRecastAuthoritySceneDiagnostics? Diagnostics;
    public bool Failed;
    public string FailureReason = string.Empty;
}
