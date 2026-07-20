using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using Fantasy.Platform.Net;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecastAuthority;
using ThirdPersonSimulation.ServerAuthoritative;

namespace Fantasy;

public sealed class DotRecastAuthoritySceneCreated : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        Scene scene = self.Scene;
        if (!string.Equals(scene.SceneConfig.SceneTypeString, "DotRecastAuthority", StringComparison.Ordinal))
            return;

        var host = scene.AddComponent<DotRecastAuthorityHost>();
        try
        {
            host.ServerPublishRoot = DotRecastAuthoritySceneStartupArguments.RequireServerPublishRoot();
            host.ManifestPath = Path.Combine(
                host.ServerPublishRoot,
                DotRecastAuthoritySceneManifest.PublishDirectoryName,
                DotRecastAuthoritySceneManifest.FileName);
            host.LoadedManifest = DotRecastAuthoritySceneManifestLoader.LoadFile(host.ManifestPath);
            host.Diagnostics = new DotRecastAuthoritySceneDiagnostics(host.LoadedManifest.Manifest);
            DotRecastAuthoritySceneStartupArguments.RequireSceneIdentity(scene, host.LoadedManifest.Manifest);
            host.GateSceneAddress = DotRecastAuthoritySceneStartupArguments.RequireGateSceneAddress(scene);
            var control = new DotRecastAuthoritySceneControlTransport(
                scene,
                host.GateSceneAddress,
                host.Address,
                host.LoadedManifest);
            host.ControlTransport = control;
            host.Runtime = DotRecastAuthoritySceneRuntime.Prepare(
                host.LoadedManifest,
                control,
                host.Diagnostics);
            var response = await scene.Call(
                host.GateSceneAddress,
                DotRecastAuthoritySceneRegistration.Build(host.Address, host.LoadedManifest.Manifest))
                as G2A_ServerAuthoritativeAuthoritySceneRegisterResponse ??
                throw new InvalidOperationException("Gate returned an unexpected Authority Scene registration response.");
            control.AcceptRegistration(response);
            if (control.ControlStatus == ServerAuthoritativeAuthorityControlTransportStatus.Failed)
                throw new InvalidOperationException(control.ControlFailure?.Message ?? "Authority Scene registration failed.");
            Log.Info(
                $"DotRecast Authority Scene prepared: host={control.Host} scene={scene.SceneConfigId} " +
                $"gate={host.GateSceneAddress} manifest={host.LoadedManifest.Manifest.ManifestHash}.");
        }
        catch (Exception exception)
        {
            host.Failed = true;
            host.FailureReason = exception.Message;
            try
            {
                host.ControlTransport?.SendFailure("dotrecast_authority_scene_startup_failed", exception.Message);
            }
            catch
            {
            }
            host.Runtime?.Dispose();
            if (host.Runtime == null)
                host.ControlTransport?.Dispose();
            Log.Error($"DotRecast Authority Scene startup failed: {exception}");
        }
    }
}

public sealed class DotRecastAuthorityHostUpdateSystem : UpdateSystem<DotRecastAuthorityHost>
{
    protected override void Update(DotRecastAuthorityHost self)
    {
        if (self.Failed || self.Runtime == null)
            return;
        try
        {
            self.Runtime.Pump();
        }
        catch (Exception exception)
        {
            self.Failed = true;
            self.FailureReason = exception.Message;
            try
            {
                self.ControlTransport?.SendFailure("dotrecast_authority_scene_pump_failed", exception.Message);
            }
            catch
            {
            }
            self.Runtime.Dispose();
            Log.Error($"DotRecast Authority Scene runtime failed: {exception}");
        }
    }
}

public sealed class DotRecastAuthorityHostDestroySystem : DestroySystem<DotRecastAuthorityHost>
{
    protected override void Destroy(DotRecastAuthorityHost self)
    {
        if (self.Runtime != null)
            self.Runtime.Dispose();
        else
            self.ControlTransport?.Dispose();
        self.Runtime = null;
        self.ControlTransport = null;
        self.LoadedManifest = null;
        self.Diagnostics = null;
        self.ServerPublishRoot = string.Empty;
        self.ManifestPath = string.Empty;
        self.GateSceneAddress = 0;
        self.Failed = false;
        self.FailureReason = string.Empty;
    }
}

static class DotRecastAuthoritySceneStartupArguments
{
    const string ServerRootEnvironmentVariable = "THIRDPERSON_DOTRECAST_AUTHORITY_SERVER_ROOT";

    public static string RequireServerPublishRoot()
    {
        string? value = Environment.GetEnvironmentVariable(ServerRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            throw new InvalidOperationException($"An absolute '{ServerRootEnvironmentVariable}' is required.");
        string root = Path.GetFullPath(value);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"DotRecast Authority server publish root does not exist: {root}");
        return root;
    }

    public static void RequireSceneIdentity(Scene scene, DotRecastAuthoritySceneManifest manifest)
    {
        DotRecastAuthoritySceneIdentity expected = manifest.Scene;
        SceneConfig actual = scene.SceneConfig;
        if (expected.ProcessConfigId != actual.ProcessConfigId ||
            expected.SceneConfigId != actual.Id ||
            !string.Equals(expected.SceneType, actual.SceneTypeString, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Authority Scene manifest expects process/scene/type '{expected.ProcessConfigId}/{expected.SceneConfigId}/{expected.SceneType}', " +
                $"but Fantasy started '{actual.ProcessConfigId}/{actual.Id}/{actual.SceneTypeString}'.");
        }
    }

    public static long RequireGateSceneAddress(Scene authorityScene)
    {
        List<SceneConfig> gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        SceneConfig[] matching = gates
            .Where(value => value.ProcessConfigId == authorityScene.SceneConfig.ProcessConfigId)
            .OrderBy(value => value.Id)
            .ToArray();
        if (matching.Length != 1 || matching[0].Address == 0)
            throw new InvalidOperationException("DotRecast Authority Scene requires exactly one Gate Scene in the same Fantasy process.");
        return matching[0].Address;
    }
}

static class DotRecastAuthoritySceneRegistration
{
    public static A2G_ServerAuthoritativeAuthoritySceneRegisterRequest Build(
        long authorityAddress,
        DotRecastAuthoritySceneManifest manifest)
    {
        SimulationSessionSourceDescriptor source = manifest.Pipeline.Source;
        SimulationComponentIdentity model = source.Model ??
            throw new InvalidOperationException("Authority Source descriptor has no Model identity.");
        SimulationComponentIdentity endpoint = source.Endpoint ??
            throw new InvalidOperationException("Authority Source descriptor has no Endpoint identity.");
        SimulationProtocolIdentity protocol = source.Protocol ??
            throw new InvalidOperationException("Authority Source descriptor has no Protocol identity.");
        if (!string.Equals(model.ComponentId, ServerAuthoritativeModelIdentity.ModelId, StringComparison.Ordinal) ||
            !string.Equals(protocol.ProtocolId, ServerAuthoritativeModelIdentity.ProtocolId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Authority Source Model or Protocol identity is not ServerAuthoritative.");
        }
        return new A2G_ServerAuthoritativeAuthoritySceneRegisterRequest
        {
            AuthorityAddress = authorityAddress,
            Host = new ServerAuthoritativeInnerHostIdentity
            {
                HostProductId = manifest.HostProductId.Value,
                HostId = manifest.HostId,
                RouteKind = (int)ThirdPersonSimulation.ServerAuthoritative.ServerAuthoritativeAuthorityHostRouteKind.InProcessAuthorityScene,
                RoomId = manifest.RoomId.Value
            },
            Protocol = new ServerAuthoritativeInnerProtocolIdentity
            {
                ModelProtocolVersion = ServerAuthoritativeModelIdentity.ProtocolVersion,
                ModelId = model.ComponentId,
                ModelConfigurationHash = model.ConfigurationHash.ToString(),
                EndpointId = endpoint.ComponentId
            },
            Program = new ServerAuthoritativeInnerProgramIdentity
            {
                ProgramId = manifest.Program.ProgramId.Value,
                ProgramHash = manifest.Program.ProgramHash.ToString(),
                LayoutHash = manifest.Program.LayoutHash.ToString(),
                OperationSetId = CharacterGameplayOperationSet.Id,
                OperationSetVersion = manifest.Program.OperationSetVersion.Value
            },
            AuthorityPipeline = new ServerAuthoritativeInnerPipelineIdentity
            {
                PipelineId = manifest.Pipeline.Identity.Id.Value,
                PipelineHash = manifest.Pipeline.Identity.Hash.ToString(),
                BackendId = manifest.Pipeline.BackendIdentity.ComponentId,
                SolverId = manifest.World.SolverId.Value,
                SolverVersion = manifest.World.SolverVersion,
                TickRate = checked((uint)manifest.Pipeline.TickRate),
                SolverCapabilities = (ulong)manifest.World.SolverCapabilities,
                SolverFeatures = (ulong)manifest.World.SolverFeatures
            },
            PredictionPipelineId = manifest.Pipeline.PredictionIdentity.Id.Value,
            PredictionPipelineHash = manifest.Pipeline.PredictionIdentity.Hash.ToString(),
            DataEndpoint = new ServerAuthoritativeInnerDataEndpoint
            {
                Host = manifest.DataEndpoint.Host,
                Port = checked((uint)manifest.DataEndpoint.Port)
            },
            World = new ServerAuthoritativeInnerWorldIdentity
            {
                SolverId = manifest.World.SolverId.Value,
                SolverVersion = manifest.World.SolverVersion,
                SolverCapabilities = (ulong)manifest.World.SolverCapabilities,
                SolverFeatures = (ulong)manifest.World.SolverFeatures,
                WorldId = manifest.World.WorldId.Value,
                MapId = manifest.World.MapId,
                WorldRevision = manifest.World.WorldRevision.Value,
                WorldConfigurationHash = manifest.World.WorldConfigurationHash.ToString(),
                NavigationSurfaceArtifactHash = manifest.World.NavigationSurfaceContentHash.ToString(),
                QueryProfileHash = manifest.World.QueryProfileHash.ToString()
            }
        };
    }
}
