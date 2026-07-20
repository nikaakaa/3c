using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public sealed class DotRecastAuthoritySceneRuntime : IDisposable
    {
        readonly LoadedDotRecastAuthoritySceneManifest m_Loaded;
        readonly ServerAuthoritativeAuthoritySourceRuntime m_Source;
        readonly IServerAuthoritativeAuthorityControlTransport m_Control;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly ActorId[] m_ActorIds;
        readonly long m_FixedElapsedTimeTicks;
        ISimulationSessionRuntimeHandle m_RuntimeHandle;
        long m_ClockStartedAt;
        ulong m_CompletedSourceTicks;
        bool m_Disposed;
        Exception m_Failure;

        DotRecastAuthoritySceneRuntime(
            LoadedDotRecastAuthoritySceneManifest loaded,
            ServerAuthoritativeAuthoritySourceRuntime source,
            IServerAuthoritativeAuthorityControlTransport control,
            ISimulationDiagnosticsSink diagnostics,
            ActorId[] actorIds)
        {
            m_Loaded = loaded;
            m_Source = source;
            m_Control = control;
            m_Diagnostics = diagnostics;
            m_ActorIds = actorIds;
            m_FixedElapsedTimeTicks = Math.Max(
                1,
                (long)Math.Round(TimeSpan.TicksPerSecond / (double)loaded.Manifest.Pipeline.TickRate));
        }

        public LoadedDotRecastAuthoritySceneManifest Loaded => m_Loaded;
        public ISimulationSessionRuntimeHandle RuntimeHandle => m_RuntimeHandle;
        public ulong LatestAuthorityTick => m_Source.LatestAuthorityTick;
        public bool IsPrepared => !m_Disposed && m_Failure == null;
        public bool IsReady => IsPrepared && m_RuntimeHandle != null &&
            m_RuntimeHandle.LifecycleState == SimulationSessionLifecycleState.Active;
        public bool IsFailed => m_Failure != null ||
            m_RuntimeHandle?.LifecycleState == SimulationSessionLifecycleState.Failed;
        public Exception Failure => m_Failure;

        public static DotRecastAuthoritySceneRuntime Prepare(
            LoadedDotRecastAuthoritySceneManifest loaded,
            IServerAuthoritativeAuthorityControlTransport control,
            ISimulationDiagnosticsSink diagnostics)
        {
            if (loaded == null)
                throw new ArgumentNullException(nameof(loaded));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            DotRecastAuthoritySceneManifest manifest = loaded.Manifest;
            RequireRuntimeIdentities(manifest);
            ServerAuthoritativeAuthorityHostIdentity host = DotRecastAuthorityHostProduct.CreateSceneHostIdentity(
                manifest.HostId,
                manifest.RoomId);
            var actorIds = new ActorId[loaded.Roster.Count];
            for (int i = 0; i < loaded.Roster.Count; i++)
                actorIds[i] = loaded.Roster[i].Binding.Roster.ActorId;
            if (!IPAddress.TryParse(manifest.DataEndpoint.Host, out IPAddress dataAddress))
                throw new InvalidOperationException("DotRecast Authority data endpoint Host must be an explicit IP address.");

            ServerAuthoritativeDatagramEndpoint data = null;
            ServerAuthoritativeAuthoritySourceRuntime source = null;
            try
            {
                data = new ServerAuthoritativeDatagramEndpoint(
                    new IPEndPoint(dataAddress, manifest.DataEndpoint.Port),
                    manifest.Pipeline.SourcePolicy.CommandQueueCapacity,
                    manifest.Pipeline.SourcePolicy.ModelPolicy.MaxGameplayDatagramBytes);
                source = new ServerAuthoritativeAuthoritySourceRuntime(
                    manifest.Pipeline.Source,
                    manifest.Pipeline.SourcePolicy,
                    host,
                    actorIds,
                    loaded.Program,
                    control,
                    data,
                    diagnostics);
                RequireSourcePorts(manifest.Pipeline.SourcePorts, source.RuntimePorts);
                return new DotRecastAuthoritySceneRuntime(loaded, source, control, diagnostics, actorIds);
            }
            catch
            {
                if (source != null)
                    source.Dispose();
                else
                {
                    data?.Dispose();
                    control.Dispose();
                }
                throw;
            }
        }

        public void Pump()
        {
            RequireAlive();
            try
            {
                long now = Stopwatch.GetTimestamp();
                if (m_RuntimeHandle == null)
                {
                    m_Source.PumpTransport();
                    if (!m_Source.IsReady)
                        return;
                    m_RuntimeHandle = LaunchRuntime();
                    m_ClockStartedAt = now;
                    return;
                }
                ulong dueTicks = ElapsedTicks(now - m_ClockStartedAt, m_Loaded.Manifest.Pipeline.TickRate);
                if (dueTicks <= m_CompletedSourceTicks)
                {
                    m_Source.PumpTransport();
                    return;
                }
                ulong pending = dueTicks - m_CompletedSourceTicks;
                ServerAuthoritativeAuthoritySourcePolicy policy = m_Loaded.Manifest.Pipeline.SourcePolicy;
                if (pending > (ulong)policy.MaxClockLagTicks)
                    throw new InvalidOperationException($"Authority clock lag '{pending}' exceeds '{policy.MaxClockLagTicks}' ticks.");
                int count = (int)Math.Min(pending, (ulong)policy.MaxCatchUpTicksPerPump);
                for (int i = 0; i < count; i++)
                {
                    ulong sourceTick = checked(m_CompletedSourceTicks + 1);
                    m_RuntimeHandle.LogicTick(new SimulationSessionLogicTickContext(
                        new SimulationTickSourceIdentity(
                            SimulationTickSourceKind.Authoritative,
                            m_Loaded.Manifest.Runtime.SourceClockId.Value,
                            sourceTick),
                        m_Loaded.Manifest.World.WorldRevision,
                        m_FixedElapsedTimeTicks));
                    if (m_RuntimeHandle.LifecycleState == SimulationSessionLifecycleState.Failed)
                        throw new InvalidOperationException(m_RuntimeHandle.Failure?.ToString() ?? "Authority runtime failed without diagnostics.");
                    m_CompletedSourceTicks = sourceTick;
                    if (m_Source.LatestAuthorityTick != sourceTick)
                        throw new InvalidOperationException("Authority Source and runtime clocks diverged.");
                }
            }
            catch (Exception exception)
            {
                m_Failure = exception;
                try
                {
                    m_Control.SendFailure("dotrecast_authority_scene_runtime_failed", exception.Message);
                }
                catch
                {
                }
                throw;
            }
        }

        ISimulationSessionRuntimeHandle LaunchRuntime()
        {
            DotRecastAuthoritySceneManifest manifest = m_Loaded.Manifest;
            var actorBindings = new SimulationActorBinding[m_Loaded.Roster.Count];
            var bodyBindings = new DotRecastBodyBindingDescriptor[m_Loaded.Roster.Count];
            var initialActors = new SimulationActorState[m_Loaded.Roster.Count];
            var initialBodies = new WorldBodyState[m_Loaded.Roster.Count];
            var outputRoutes = new SimulationOutputRouteDescriptor[m_Loaded.Roster.Count];
            for (int i = 0; i < m_Loaded.Roster.Count; i++)
            {
                LoadedDotRecastAuthorityActor actor = m_Loaded.Roster[i];
                ActorId actorId = actor.Binding.Roster.ActorId;
                actorBindings[i] = new SimulationActorBinding(actorId, m_Loaded.Program, actor.Binding.WorldBodyBindingId);
                bodyBindings[i] = new DotRecastBodyBindingDescriptor(
                    actor.Binding.WorldBodyBindingId,
                    actor.Binding.InitialBody,
                    actor.Binding.ContactShape);
                initialActors[i] = new SimulationActorState(actorId, actor.InitialState);
                initialBodies[i] = actor.Binding.InitialBody;
                outputRoutes[i] = actor.Binding.OutputRoute;
            }

            var programRuntime = Float32ProgramRuntime.Create(actorBindings);
            DotRecastWorldSolver solver = null;
            try
            {
                solver = new DotRecastWorldSolver(
                    manifest.Pipeline.TickRate,
                    m_Loaded.CopyNavigationSurfaceBytes(),
                    manifest.World.ContactConfiguration,
                    bodyBindings);
                WorldSimulationState initialWorld = solver.Create(manifest.World.WorldRevision, initialBodies);
                var initialState = new SimulationWorldStateSet(0, initialActors, initialWorld);
                var outputs = new DotRecastAuthoritySuppressedOutputPort(m_ActorIds);
                var committer = new Float32SimulationCommitterAdapter(
                    manifest.Runtime.Committer,
                    new SimulationCommitter(outputs, outputs),
                    m_Source.SourceEgress,
                    outputs);
                var request = new Float32SimulationSessionCompositionRequest(
                    manifest.Runtime.SessionId,
                    manifest.World.WorldId,
                    manifest.Runtime.SourceClockId,
                    manifest.Pipeline.TickRate,
                    programRuntime,
                    Float32PassExecutionBackend.Descriptor,
                    m_Loaded.PipelineCatalog.RuntimePackage,
                    manifest.Pipeline.Source,
                    m_Source.RuntimePorts,
                    null,
                    manifest.World.SolverDefinition,
                    solver,
                    manifest.World.SolverFeatures,
                    initialState,
                    SimulationPipelineInitialStateSource.CaptureActivatedDefaults,
                    committer,
                    manifest.Runtime.Diagnostics,
                    m_Diagnostics,
                    outputRoutes,
                    new IDisposable[] { m_Source });
                var launcher = new ServerAuthoritativeAuthoritySessionRuntimeLauncher(
                    manifest.Pipeline.Source,
                    manifest.Pipeline.SourcePolicy,
                    manifest.Pipeline.Identity,
                    m_ActorIds);
                return launcher.Launch(request).RuntimeHandle;
            }
            catch
            {
                solver?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_RuntimeHandle != null)
                m_RuntimeHandle.Dispose();
            else
                m_Source.Dispose();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DotRecastAuthoritySceneRuntime));
            if (m_Failure != null)
                throw new InvalidOperationException("DotRecast Authority Scene runtime is failed.", m_Failure);
        }

        static ulong ElapsedTicks(long elapsedStopwatchTicks, int tickRate)
        {
            if (elapsedStopwatchTicks <= 0)
                return 0;
            long seconds = elapsedStopwatchTicks / Stopwatch.Frequency;
            long remainder = elapsedStopwatchTicks % Stopwatch.Frequency;
            return checked((ulong)seconds * (ulong)tickRate +
                (ulong)(remainder * tickRate / Stopwatch.Frequency));
        }

        static void RequireRuntimeIdentities(DotRecastAuthoritySceneManifest manifest)
        {
            if (!manifest.HostProductId.Equals(DotRecastAuthorityHostProduct.ProductId) ||
                !manifest.Pipeline.BackendIdentity.Equals(Float32PassExecutionBackend.Descriptor.Identity) ||
                !manifest.Runtime.SnapshotCodec.Equals(
                    Float32SimulationSessionComposer.BuildSnapshotCodecIdentity(
                        Float32ProgramRuntime.DescriptorDefinition,
                        Float32PassExecutionBackend.Descriptor)))
            {
                throw new InvalidOperationException("DotRecast Authority Scene manifest runtime identities are not canonical.");
            }
        }

        static void RequireSourcePorts(
            IReadOnlyList<SimulationPortDescriptor> expected,
            SimulationRuntimePortSet actual)
        {
            if (expected.Count != actual.Ports.Count)
                throw new InvalidOperationException("DotRecast Authority Source port count does not match the manifest.");
            for (int i = 0; i < expected.Count; i++)
            {
                SimulationPortDescriptor left = expected[i];
                SimulationPortDescriptor right = actual.Ports[i].Descriptor;
                if (!string.Equals(left.PortId, right.PortId, StringComparison.Ordinal) ||
                    !string.Equals(left.SchemaId, right.SchemaId, StringComparison.Ordinal) ||
                    left.SchemaVersion != right.SchemaVersion || left.Direction != right.Direction ||
                    !string.Equals(left.OwnerComponentId, right.OwnerComponentId, StringComparison.Ordinal) ||
                    !left.ConfigurationHash.Equals(right.ConfigurationHash))
                {
                    throw new InvalidOperationException($"DotRecast Authority Source port '{right.PortId}' does not match the manifest.");
                }
            }
        }
    }

    sealed class DotRecastAuthoritySuppressedOutputPort :
        ISimulationGameplayOutputPort,
        ISimulationPresentationOutputPort,
        IFloat32PublishedActorResultObserver
    {
        readonly HashSet<ActorId> m_Actors;

        public DotRecastAuthoritySuppressedOutputPort(IEnumerable<ActorId> actors)
        {
            m_Actors = new HashSet<ActorId>(actors ?? throw new ArgumentNullException(nameof(actors)));
            if (m_Actors.Count == 0)
                throw new ArgumentException("Authority output boundary requires an Actor roster.", nameof(actors));
        }

        public void Publish(GameplayFact fact) => Throw(fact.Header.EventId);
        public void Replace(EventId targetEventId, GameplayFact fact) => Throw(fact.Header.EventId);
        public void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId) => Throw(sourceEventId);
        public void Publish(PresentationCommand command) => Throw(command.Header.EventId);

        public void ObservePublished(SimulationActorTickResult result)
        {
            if (result == null || !m_Actors.Contains(result.ActorId))
                throw new InvalidOperationException("Authority published result targets an Actor outside the locked roster.");
        }

        static void Throw(EventId eventId)
        {
            throw new InvalidOperationException(
                $"Authority output '{eventId}' bypassed AuthorityReplicationEgress suppression.");
        }
    }
}
