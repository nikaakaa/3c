using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal sealed class ServerAuthoritativeAuthorityActorRegistration : IFloat32SimulationActorRegistration
    {
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        bool m_Activated;
        bool m_Disposed;

        public ServerAuthoritativeAuthorityActorRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            CharacterSimulationProgramAsset programAsset,
            CharacterSimulationProgram program,
            Float32WorldBodyBinding worldBodyBinding,
            WorldBodyState initialBody,
            CharacterSimulationDiagnosticsAdapter diagnostics,
            RuntimeDiagnosticsTarget diagnosticsTarget)
        {
            if (ownerInstanceId == 0 || string.IsNullOrWhiteSpace(ownerName) || !actorId.IsValid)
                throw new ArgumentException("Authority Actor registration owner identity is incomplete.");
            OwnerInstanceId = ownerInstanceId;
            OwnerName = ownerName.Trim();
            ActorId = actorId;
            ProgramAsset = programAsset ? programAsset : throw new ArgumentNullException(nameof(programAsset));
            Program = program ?? throw new ArgumentNullException(nameof(program));
            WorldBodyBinding = worldBodyBinding ? worldBodyBinding : throw new ArgumentNullException(nameof(worldBodyBinding));
            if (worldBodyBinding.ActorId != actorId || initialBody.ActorId != actorId)
                throw new ArgumentException("Authority Actor body identity does not match ActorId.");
            InitialBody = initialBody;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            ProgramIdentity = new SimulationActorBinding(actorId, program, worldBodyBinding.BindingId);
            OutputRoute = new SimulationOutputRouteDescriptor(
                $"server-authoritative-authority-output/{actorId.Value}",
                "server-authoritative-authority-output",
                1,
                actorId,
                StableHash.Compute(
                    "server-authoritative-authority-output/1",
                    actorId.Value,
                    program.ProgramHash.ToString(),
                    worldBodyBinding.BindingId));
        }

        public int OwnerInstanceId { get; }
        public string OwnerName { get; }
        public string OwnerIdentity => $"unity-authority-actor-host/{OwnerInstanceId}";
        public ActorId ActorId { get; }
        public CharacterSimulationProgramAsset ProgramAsset { get; }
        public CharacterSimulationProgram Program { get; }
        public SimulationActorBinding ProgramIdentity { get; }
        public Float32WorldBodyBinding WorldBodyBinding { get; }
        public WorldBodyState InitialBody { get; }
        public CharacterSimulationDiagnosticsAdapter Diagnostics { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
        public StableHash DiagnosticsConfigurationHash => StableHash.Compute(
            "server-authoritative-authority-diagnostics/1",
            ActorId.Value,
            Program.ProgramHash.ToString(),
            WorldBodyBinding.BindingId);
        public ISimulationGameplayOutputPort GameplayOutput => ServerAuthoritativeAuthorityOutputPort.Instance;
        public ISimulationPresentationOutputPort PresentationOutput => ServerAuthoritativeAuthorityOutputPort.Instance;
        public ISimulationDiagnosticsSink SimulationDiagnostics => Diagnostics;

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
            m_Activated = true;
        }

        public void Deactivate()
        {
            if (!m_Activated)
                return;
            RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget);
            m_Activated = false;
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Activated)
                throw new InvalidOperationException($"Authority Actor '{ActorId}' registration is not active.");
        }

        public void BeginLogicTick()
        {
            RequireAlive();
        }

        public void ObservePublished(SimulationActorTickResult result)
        {
            RequireAlive();
            if (result == null || result.ActorId != ActorId)
                throw new InvalidOperationException("Authority published result targets another Actor.");
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            var failures = new List<Exception>();
            TryRelease(Deactivate, failures);
            TryRelease(m_DiagnosticsTarget.Terminate, failures);
            TryRelease(m_DiagnosticsTarget.Dispose, failures);
            if (failures.Count != 0)
                throw new AggregateException($"Authority Actor '{ActorId}' registration failed to dispose completely.", failures);
        }

        static void TryRelease(Action release, ICollection<Exception> failures)
        {
            try
            {
                release();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeAuthorityActorRegistration));
        }
    }

    internal sealed class ServerAuthoritativeAuthorityOutputPort :
        ISimulationGameplayOutputPort,
        ISimulationPresentationOutputPort
    {
        public static readonly ServerAuthoritativeAuthorityOutputPort Instance =
            new ServerAuthoritativeAuthorityOutputPort();

        ServerAuthoritativeAuthorityOutputPort() { }

        public void Publish(GameplayFact fact) => Throw(fact.Header.EventId);
        public void Replace(EventId targetEventId, GameplayFact fact) => Throw(fact.Header.EventId);
        public void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId) => Throw(sourceEventId);
        public void Publish(PresentationCommand command) => Throw(command.Header.EventId);

        static void Throw(EventId eventId)
        {
            throw new InvalidOperationException(
                $"Authority output '{eventId}' bypassed AuthorityReplicationEgress suppression.");
        }
    }
}
