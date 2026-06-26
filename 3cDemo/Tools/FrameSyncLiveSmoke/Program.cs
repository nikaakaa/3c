using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Fantasy;
using Fantasy.Helper;
using Fantasy.Network.Interface;
using Fantasy.Network;
using Fantasy.Platform.Net;

namespace FrameSyncLiveSmoke;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string configPath = args.FirstOrDefault(arg => arg.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            ?? Path.Combine(AppContext.BaseDirectory, "FrameSyncLiveSmoke.config.json");
        bool initialized = false;
        int exitCode = 1;

        try
        {
            LiveSmokeConfig config = LoadConfig(configPath);
            EnsureProtocolAssembliesLoaded();
            ConfigureAuthority(config);
            Console.WriteLine("FRAME_SYNC_LIVE_STAGE initialize");
            await InvokeEntryInitialize();
            ReplayGeneratedInitializer(typeof(G2C_FrameSyncHandshakeResponseHandler).Assembly, "Fantasy.Generated.FrameSyncLiveSmoke_AssemblyInitializer");
            initialized = true;
            Console.WriteLine("FRAME_SYNC_LIVE_STAGE process-create");
            Fantasy.Platform.Net.Process process = await InvokeProcessCreate(1);
            Console.WriteLine("FRAME_SYNC_LIVE_STAGE scene-create");
            Scene scene = GetProcessScene(process, config.ServerSceneConfigId);

            NetworkProtocolType protocol = Enum.Parse<NetworkProtocolType>(config.Protocol, true);
            Console.WriteLine("FRAME_SYNC_LIVE_STAGE connect-first");
            Session first = await Connect(scene, config.Endpoint, protocol, config.TimeoutMilliseconds);
            Console.WriteLine("FRAME_SYNC_LIVE_STAGE connect-second");
            Session second = await Connect(scene, config.Endpoint, protocol, config.TimeoutMilliseconds);
            UseOuterScheduler(first, scene);
            UseOuterScheduler(second, scene);
            await ReloadSessionDispatcher(first, typeof(G2C_FrameSyncHandshakeResponseHandler).Assembly, "Fantasy.Generated.FrameSyncLiveSmoke_AssemblyInitializer");
            await ReloadSessionDispatcher(second, typeof(G2C_FrameSyncHandshakeResponseHandler).Assembly, "Fantasy.Generated.FrameSyncLiveSmoke_AssemblyInitializer");

            try
            {
                FrameSyncLiveSmokeClientProbe firstProbe = FrameSyncLiveSmokeProbe.Register(first.RuntimeId, config.Clients[0].Name);
                FrameSyncLiveSmokeClientProbe secondProbe = FrameSyncLiveSmokeProbe.Register(second.RuntimeId, config.Clients[1].Name);

                await Handshake(first, firstProbe, config.Clients[0], config.Manifest, 1, config.TimeoutMilliseconds);
                await Handshake(second, secondProbe, config.Clients[1], config.Manifest, 2, config.TimeoutMilliseconds);

                first.C2G_FrameSyncInput(CreateInput(config.Tick, config.Clients[0], 1));
                second.C2G_FrameSyncInput(CreateInput(config.Tick, config.Clients[1], 1));

                bool closed = await WaitForClosedInputSet(config, firstProbe, secondProbe);
                if (!closed)
                    return Fail("confirmed-input", $"timeoutMs={config.TimeoutMilliseconds} ackA={firstProbe.InputAckCount} ackB={secondProbe.InputAckCount} confirmedA={firstProbe.ConfirmedInputSetCount} confirmedB={secondProbe.ConfirmedInputSetCount}");

                first.C2G_FrameSyncChecksum(config.Tick, config.Clients[0].PlayerId, 101, 201);
                second.C2G_FrameSyncChecksum(config.Tick, config.Clients[1].PlayerId, 102, 202);

                Console.WriteLine($"FRAME_SYNC_LIVE_CLIENT_RESULT endpoint={config.Endpoint} protocol={config.Protocol} clients=2 tick={config.Tick} ackCount={firstProbe.InputAckCount + secondProbe.InputAckCount} confirmedReceivers={ConfirmedReceiverCount(firstProbe, secondProbe, config.Tick)} confirmedPlayers=2 checksumReports=2 result=PASS");
                exitCode = 0;
                return exitCode;
            }
            finally
            {
                FrameSyncLiveSmokeProbe.Unregister(first.RuntimeId);
                FrameSyncLiveSmokeProbe.Unregister(second.RuntimeId);
                first.Dispose();
                second.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            exitCode = Fail("exception", ex.GetType().Name + ":" + ex.Message.Replace(' ', '_'));
            return exitCode;
        }
        finally
        {
            if (initialized)
                Environment.Exit(exitCode);
        }
    }

    static async Task<Session> Connect(Scene scene, string endpoint, NetworkProtocolType protocol, int timeoutMilliseconds)
    {
        TaskCompletionSource<Session> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AClientNetwork client = CreateClientNetwork(scene, protocol);
        Session session = client.Connect(
            endpoint,
            () => completion.TrySetResult(client.Session),
            () => completion.TrySetException(new InvalidOperationException("connect failed")),
            () => completion.TrySetException(new InvalidOperationException("connect disconnected")),
            false,
            timeoutMilliseconds);

        Task finished = await Task.WhenAny(completion.Task, Task.Delay(timeoutMilliseconds));
        if (finished != completion.Task)
            throw new TimeoutException($"connect timeout endpoint={endpoint} protocol={protocol}");

        return await completion.Task;
    }

    static async Task InvokeEntryInitialize()
    {
        Type entry = typeof(Session).Assembly.GetType("Fantasy.Platform.Net.Entry")
            ?? throw new InvalidOperationException("Fantasy Entry type was not found.");
        MethodInfo initialize = entry.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Fantasy Entry.Initialize was not found.");
        object? result = initialize.Invoke(null, new object[] { new ConsoleFantasyLog() });
        if (result != null)
            await (dynamic)result;
    }

    static void ReplayGeneratedInitializer(Assembly assembly, string typeName)
    {
        Type initializer = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"{typeName} was not found.");
        FieldInfo initialized = initializer.GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{typeName} initialized flag was not found.");
        MethodInfo initialize = initializer.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{typeName}.Initialize was not found.");

        initialized.SetValue(null, false);
        initialize.Invoke(null, null);
    }

    static async Task ReloadSessionDispatcher(Session session, Assembly assembly, string initializerTypeName)
    {
        ReplayGeneratedInitializer(assembly, initializerTypeName);

        Fantasy.Assembly.AssemblyManifest manifest = Fantasy.Assembly.AssemblyManifest.GetAssemblyManifest.FirstOrDefault(x => x.Assembly == assembly)
            ?? throw new InvalidOperationException($"{assembly.GetName().Name} manifest was not found.");
        PropertyInfo schedulerProperty = typeof(Session).GetProperty(
            "NetworkMessageScheduler",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NetworkMessageScheduler property was not found.");
        object scheduler = schedulerProperty.GetValue(session)
            ?? throw new InvalidOperationException("NetworkMessageScheduler was not found.");
        FieldInfo dispatcherField = scheduler.GetType().BaseType?.GetField(
            "MessageDispatcherComponent",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MessageDispatcherComponent field was not found.");
        MessageDispatcherComponent dispatcher = (MessageDispatcherComponent?)dispatcherField.GetValue(scheduler)
            ?? throw new InvalidOperationException("MessageDispatcherComponent was not found.");
        await dispatcher.OnLoad(manifest);
    }

    static void UseOuterScheduler(Session session, Scene scene)
    {
        Type schedulerType = typeof(Session).Assembly.GetType("Fantasy.Scheduler.OuterMessageScheduler")
            ?? throw new InvalidOperationException("OuterMessageScheduler type was not found.");
        object scheduler = Activator.CreateInstance(schedulerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { scene }, null)
            ?? throw new InvalidOperationException("OuterMessageScheduler could not be created.");
        PropertyInfo schedulerProperty = typeof(Session).GetProperty(
            "NetworkMessageScheduler",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NetworkMessageScheduler property was not found.");
        schedulerProperty.SetValue(session, scheduler);
    }

    static async Task<Fantasy.Platform.Net.Process> InvokeProcessCreate(uint processId)
    {
        MethodInfo create = typeof(Fantasy.Platform.Net.Process).GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Fantasy Process.Create was not found.");
        object? result = create.Invoke(null, new object[] { processId });
        if (result == null)
            throw new InvalidOperationException("Fantasy Process.Create returned null.");
        return await (dynamic)result;
    }

    static Scene GetProcessScene(Fantasy.Platform.Net.Process process, uint sceneConfigId)
    {
        MethodInfo tryGetScene = typeof(Fantasy.Platform.Net.Process).GetMethod(
            "TryGetSceneToProcess",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(uint), typeof(Scene).MakeByRefType() },
            null) ?? throw new InvalidOperationException("Fantasy Process.TryGetScene was not found.");

        object?[] args = { sceneConfigId, null };
        bool found = (bool)(tryGetScene.Invoke(process, args) ?? false);
        if (!found || args[1] is not Scene scene)
            throw new InvalidOperationException($"Fantasy server scene {sceneConfigId} was not found.");

        return scene;
    }

    static AClientNetwork CreateClientNetwork(Scene scene, NetworkProtocolType protocol)
    {
        Type factory = typeof(Session).Assembly.GetType("Fantasy.Network.NetworkProtocolFactory")
            ?? throw new InvalidOperationException("Fantasy NetworkProtocolFactory was not found.");
        MethodInfo createClient = factory.GetMethod("CreateClient", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Fantasy NetworkProtocolFactory.CreateClient was not found.");
        return (AClientNetwork)(createClient.Invoke(null, new object[] { scene, protocol, NetworkTarget.Outer })
            ?? throw new InvalidOperationException("Fantasy client network could not be created."));
    }

    static LiveSmokeConfig LoadConfig(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Frame sync live smoke config is required.", path);

        LiveSmokeConfig config = JsonSerializer.Deserialize<LiveSmokeConfig>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Frame sync live smoke config is empty.");

        config.Validate();
        return config;
    }

    static void EnsureProtocolAssembliesLoaded()
    {
        typeof(FrameSyncManifestMessage).Assembly.EnsureLoaded();
        typeof(C2G_FrameSyncHandshakeRequestHandler).Assembly.EnsureLoaded();
        typeof(G2C_FrameSyncInputAckHandler).Assembly.EnsureLoaded();
    }

    static void ConfigureAuthority(LiveSmokeConfig config)
    {
        FrameSyncRoomInputAuthority.Configure(new FrameSyncServerAuthoritySettings(
            1,
            CreateManifest(config.Manifest)));
    }

    static async Task Handshake(
        Session session,
        FrameSyncLiveSmokeClientProbe probe,
        LiveSmokeClientConfig client,
        LiveSmokeManifestConfig manifestConfig,
        uint sequence,
        int timeoutMilliseconds)
    {
        session.C2G_FrameSyncHandshakeRequest(
            client.PlayerId,
            client.UnitId,
            sequence,
            manifestConfig.ProtocolVersion,
            manifestConfig.InputSchemaVersion,
            manifestConfig.ChecksumSchemaVersion,
            manifestConfig.BehaviorRuntimeDefinitionHash,
            manifestConfig.LocomotionConfigHash,
            manifestConfig.MotionProfileHash,
            manifestConfig.InputMappingVersion);

        Stopwatch watch = Stopwatch.StartNew();
        G2C_FrameSyncHandshakeResponse? response = null;
        while (watch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            response = probe.HandshakeResponses.FirstOrDefault();
            if (response != null)
                break;

            await Task.Delay(10);
        }

        if (response == null)
            throw new TimeoutException($"handshake timeout for {client.Name}");

        if (!response.Accepted)
            throw new InvalidOperationException($"handshake rejected for {client.Name} error={response.ErrorCode} reasons={string.Join(",", response.FailureReasons)}");

        if (response.AssignedPlayerId == 0 || response.AssignedUnitId == 0)
            throw new InvalidOperationException($"handshake missing assigned identity for {client.Name}");

        client.PlayerId = response.AssignedPlayerId;
        client.UnitId = response.AssignedUnitId;
        Console.WriteLine($"FRAME_SYNC_LIVE_ASSIGNED_ID client={client.Name} player={client.PlayerId} unit={client.UnitId}");
    }

    static async Task<bool> WaitForClosedInputSet(
        LiveSmokeConfig config,
        FrameSyncLiveSmokeClientProbe first,
        FrameSyncLiveSmokeClientProbe second)
    {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < config.TimeoutMilliseconds)
        {
            if (HasAcceptedAck(first, config.Tick, config.Clients[0]) &&
                HasAcceptedAck(second, config.Tick, config.Clients[1]) &&
                HasConfirmedBothPlayers(first, config.Tick, config.Clients[0], config.Clients[1]) &&
                HasConfirmedBothPlayers(second, config.Tick, config.Clients[0], config.Clients[1]))
            {
                return true;
            }

            await Task.Delay(10);
        }

        return false;
    }

    static bool HasAcceptedAck(FrameSyncLiveSmokeClientProbe probe, int tick, LiveSmokeClientConfig client)
    {
        return probe.InputAcks.Any(ack => ack.Tick == tick && ack.PlayerId == client.PlayerId && ack.UnitId == client.UnitId && ack.Accepted);
    }

    static bool HasConfirmedBothPlayers(
        FrameSyncLiveSmokeClientProbe probe,
        int tick,
        LiveSmokeClientConfig first,
        LiveSmokeClientConfig second)
    {
        return HasConfirmedPlayer(probe, tick, first) &&
               HasConfirmedPlayer(probe, tick, second);
    }

    static bool HasConfirmedPlayer(
        FrameSyncLiveSmokeClientProbe probe,
        int tick,
        LiveSmokeClientConfig client)
    {
        return probe.ConfirmedInputSets.Any(inputSet =>
            inputSet.Tick == tick &&
            inputSet.Inputs.Any(input => input.PlayerId == client.PlayerId && input.UnitId == client.UnitId));
    }

    static int ConfirmedReceiverCount(
        FrameSyncLiveSmokeClientProbe first,
        FrameSyncLiveSmokeClientProbe second,
        int tick)
    {
        int count = 0;
        if (first.ConfirmedInputSets.Any(inputSet => inputSet.Tick == tick))
            count++;
        if (second.ConfirmedInputSets.Any(inputSet => inputSet.Tick == tick))
            count++;
        return count;
    }

    static FrameSyncManifestMessage CreateManifest(LiveSmokeManifestConfig config)
    {
        return new FrameSyncManifestMessage
        {
            ProtocolVersion = config.ProtocolVersion,
            InputSchemaVersion = config.InputSchemaVersion,
            ChecksumSchemaVersion = config.ChecksumSchemaVersion,
            BehaviorRuntimeDefinitionHash = config.BehaviorRuntimeDefinitionHash,
            LocomotionConfigHash = config.LocomotionConfigHash,
            MotionProfileHash = config.MotionProfileHash,
            InputMappingVersion = config.InputMappingVersion
        };
    }

    static FrameSyncInputFrameMessage CreateInput(int tick, LiveSmokeClientConfig client, uint sequence)
    {
        return new FrameSyncInputFrameMessage
        {
            Tick = tick,
            PlayerId = client.PlayerId,
            UnitId = client.UnitId,
            LocalInputSequence = sequence,
            MoveIntent = new FrameSyncVector2 { X = client.PlayerId == 1 ? 1f : -1f, Y = 0f },
            MoveIntentSpace = 1,
            LookIntent = new FrameSyncVector2 { X = 0f, Y = 0f },
            LookIntentSpace = 1,
            RunHeld = false,
            Dodge = new FrameSyncButtonFactMessage(),
            Attack = new FrameSyncButtonFactMessage(),
            Jump = new FrameSyncButtonFactMessage(),
            Interact = new FrameSyncButtonFactMessage(),
            TargetIntent = new FrameSyncTargetIntentMessage { AimIntent = new FrameSyncVector2() },
            CameraBasis = new FrameSyncCameraBasisMessage
            {
                PlanarForward = new FrameSyncVector3 { X = 0f, Y = 0f, Z = 1f },
                PlanarRight = new FrameSyncVector3 { X = 1f, Y = 0f, Z = 0f },
                Yaw = 0f
            },
            HasCameraBasis = true
        };
    }

    static int Fail(string stage, string reason)
    {
        Console.WriteLine($"FRAME_SYNC_LIVE_FIRST_FAILURE stage={stage} reason={reason}");
        return 1;
    }
}

public sealed class LiveSmokeConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public uint ServerSceneConfigId { get; set; }
    public int TimeoutMilliseconds { get; set; }
    public int Tick { get; set; }
    public LiveSmokeManifestConfig Manifest { get; set; } = new();
    public List<LiveSmokeClientConfig> Clients { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Live smoke endpoint is required.");
        if (string.IsNullOrWhiteSpace(Protocol))
            throw new InvalidOperationException("Live smoke protocol is required.");
        if (ServerSceneConfigId == 0)
            throw new InvalidOperationException("Live smoke server scene config id is required.");
        if (TimeoutMilliseconds <= 0)
            throw new InvalidOperationException("Live smoke timeout must be positive.");
        if (Clients.Count != 2)
            throw new InvalidOperationException("Live smoke requires exactly two clients.");

        Manifest.Validate();
        foreach (LiveSmokeClientConfig client in Clients)
            client.Validate();
    }
}

public sealed class LiveSmokeClientConfig
{
    public string Name { get; set; } = string.Empty;
    public uint PlayerId { get; set; }
    public uint UnitId { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Live smoke client name is required.");
        if ((PlayerId == 0) != (UnitId == 0))
            throw new InvalidOperationException("Live smoke playerId and unitId must both be assigned or both request server assignment.");
    }
}

public sealed class LiveSmokeManifestConfig
{
    public uint ProtocolVersion { get; set; }
    public uint InputSchemaVersion { get; set; }
    public uint ChecksumSchemaVersion { get; set; }
    public uint BehaviorRuntimeDefinitionHash { get; set; }
    public uint LocomotionConfigHash { get; set; }
    public uint MotionProfileHash { get; set; }
    public uint InputMappingVersion { get; set; }

    public void Validate()
    {
        if (ProtocolVersion == 0 ||
            InputSchemaVersion == 0 ||
            ChecksumSchemaVersion == 0 ||
            BehaviorRuntimeDefinitionHash == 0 ||
            LocomotionConfigHash == 0 ||
            MotionProfileHash == 0 ||
            InputMappingVersion == 0)
        {
            throw new InvalidOperationException("Live smoke manifest is incomplete.");
        }
    }
}

sealed class ConsoleFantasyLog : ILog
{
    public void Initialize(string name, ProcessMode processMode)
    {
    }

    public void Trace(string message)
    {
        Console.WriteLine(message);
    }

    public void Warning(string message)
    {
        Console.WriteLine(message);
    }

    public void Info(string message)
    {
        Console.WriteLine(message);
    }

    public void Debug(string message)
    {
        Console.WriteLine(message);
    }

    public void Error(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void Trace(string sceneName, string message) => Trace($"[{sceneName}] {message}");
    public void Warning(string sceneName, string message) => Warning($"[{sceneName}] {message}");
    public void Info(string sceneName, string message) => Info($"[{sceneName}] {message}");
    public void Debug(string sceneName, string message) => Debug($"[{sceneName}] {message}");
    public void Error(string sceneName, string message) => Error($"[{sceneName}] {message}");
    public void Trace(string message, params object[] args) => Trace(string.Format(message, args));
    public void Warning(string message, params object[] args) => Warning(string.Format(message, args));
    public void Info(string message, params object[] args) => Info(string.Format(message, args));
    public void Debug(string message, params object[] args) => Debug(string.Format(message, args));
    public void Error(string message, params object[] args) => Error(string.Format(message, args));
    public void Trace(string sceneName, string message, params object[] args) => Trace(sceneName, string.Format(message, args));
    public void Warning(string sceneName, string message, params object[] args) => Warning(sceneName, string.Format(message, args));
    public void Info(string sceneName, string message, params object[] args) => Info(sceneName, string.Format(message, args));
    public void Debug(string sceneName, string message, params object[] args) => Debug(sceneName, string.Format(message, args));
    public void Error(string sceneName, string message, params object[] args) => Error(sceneName, string.Format(message, args));
}
