using System;
using System.Collections.Generic;
using System.Net;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [CreateAssetMenu(fileName = "DeterministicRollbackSessionSource", menuName = "3C/Simulation/Deterministic Rollback/Session Source")]
    public sealed class DeterministicRollbackSessionSourceDefinition : SimulationSessionSourceDefinition
    {
        [SerializeField, Min(1)] int m_TickRate = 30;
        [SerializeField] FixedCharacterSimulationProgramAsset m_FixedProgram;
        [SerializeField] DeterministicRollbackPipelineDefinition m_Pipeline;
        [SerializeField] DeterministicKccWorldSolverDefinition m_WorldSolver;
        [SerializeField] RollbackEndpointAuthoringDefinition m_Endpoint;

        public override SimulationSessionSourceAuthoringDescriptor BuildAuthoringDescriptor()
        {
            DeterministicRollbackModelDefinition model = BuildModelDefinition();
            SimulationSessionSourceDescriptor source = model.SourceDescriptor;
            SimulationPortDescriptor port = SimulationPortDescriptor.CreateSource(
                RollbackSourcePortContracts.InputRequirement,
                source.Identity);
            return new SimulationSessionSourceAuthoringDescriptor(source, new[]
            {
                port,
                RollbackSnapshotRestoreSource.CreateDescriptor(source.Identity)
            });
        }

        protected override ISimulationSessionSourcePreparation CreatePreparationCore(
            SimulationSessionSourcePreparationContext context)
        {
            return new DeterministicRollbackSessionSourcePreparation(
                context,
                BuildModelDefinition(),
                RequireEndpoint(),
                RequireProgramAsset().Load());
        }

        public DeterministicRollbackModelDefinition BuildModelDefinition()
        {
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program =
                RequireProgramAsset().Load();
            if (program.Manifest.TickRate != RequireTickRate())
                throw new InvalidOperationException($"Rollback Source '{name}' TickRate does not match its Fixed Program.");
            DeterministicRollbackPipelineDefinition pipeline = m_Pipeline ? m_Pipeline :
                throw new InvalidOperationException($"Rollback Source '{name}' requires a Pipeline Definition.");
            DeterministicKccWorldSolverDefinition solver = m_WorldSolver ? m_WorldSolver :
                throw new InvalidOperationException($"Rollback Source '{name}' requires a KCC World Solver Definition.");
            RollbackEndpointDefinition endpoint = RequireEndpoint().Build();
            return new DeterministicRollbackModelDefinition(
                pipeline.BuildPolicy(),
                program.Manifest.SemanticHash,
                program.ProgramHash,
                program.LayoutHash,
                RequireTickRate(),
                solver.LoadCollisionWorld().ContentHash,
                solver.BuildKccIdentityHash(RequireTickRate()),
                endpoint.ConfigurationHash);
        }

        int RequireTickRate()
        {
            return m_TickRate > 0
                ? m_TickRate
                : throw new InvalidOperationException($"Rollback Source '{name}' requires a positive TickRate.");
        }

        FixedCharacterSimulationProgramAsset RequireProgramAsset() => m_FixedProgram ? m_FixedProgram :
            throw new InvalidOperationException($"Rollback Source '{name}' requires a Fixed Program asset.");

        RollbackEndpointAuthoringDefinition RequireEndpoint() => m_Endpoint ? m_Endpoint :
            throw new InvalidOperationException($"Rollback Source '{name}' requires an Endpoint Definition.");
    }

    sealed class DeterministicRollbackSessionSourcePreparation : ISimulationSessionSourcePreparation
    {
        readonly SimulationSessionSourcePreparationContext m_Context;
        readonly DeterministicRollbackModelDefinition m_Model;
        readonly RollbackEndpointAuthoringDefinition m_EndpointAuthoring;
        readonly RollbackPeerLaunchProfile m_Profile;
        RollbackPeerEndpoint m_Peer;
        DeterministicRollbackPreparedSource m_Prepared;
        int m_PreparationTicks;
        bool m_Disposed;

        public DeterministicRollbackSessionSourcePreparation(
            SimulationSessionSourcePreparationContext context,
            DeterministicRollbackModelDefinition model,
            RollbackEndpointAuthoringDefinition endpointAuthoring,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram expectedProgram)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_EndpointAuthoring = endpointAuthoring ? endpointAuthoring : throw new ArgumentNullException(nameof(endpointAuthoring));
            if (expectedProgram == null)
                throw new ArgumentNullException(nameof(expectedProgram));
            ValidateComposition(context, model, expectedProgram);
            m_Profile = endpointAuthoring.ResolvePeerProfile();
            IDeterministicRollbackSimulationActorRegistration local = RequireRegistrations(
                context.Registrations,
                expectedProgram,
                m_Profile.ActorId);
            RollbackEndpointDefinition endpoint = endpointAuthoring.Build();
            if (!string.Equals(endpoint.SessionId, context.SessionId.Value, StringComparison.Ordinal))
                throw new InvalidOperationException("Rollback Endpoint SessionId does not match the Session Composition.");
            RollbackHandshake handshake = BuildHandshake(model, m_Profile.PeerId);
            m_Peer = new RollbackPeerEndpoint(
                endpoint,
                handshake,
                endpointAuthoring.RelayServerPeerId,
                m_Profile.BuildLocalEndPoint(),
                endpointAuthoring.BuildRelayServerEndPoint(),
                endpointAuthoring.InputRedundancyCount);
            m_Peer.Start();
            LocalInput = local.RollbackInput ?? throw new InvalidOperationException(
                $"Rollback local Actor '{local.ActorId}' has no local input adapter.");
        }

        IRollbackLocalInputAdapter LocalInput { get; }
        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionSourceDescriptor Descriptor => m_Model.SourceDescriptor;

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DeterministicRollbackSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Pending)
                return Status;
            try
            {
                if (context.Source.Kind != SimulationTickSourceKind.LocalLogic ||
                    !string.Equals(context.Source.ClockId, m_Context.SourceClockId.Value, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Rollback Source preparation requires its configured LocalLogic source clock.");
                }
                m_PreparationTicks = checked(m_PreparationTicks + 1);
                if (m_PreparationTicks > m_EndpointAuthoring.MaximumPreparationTicks)
                    throw new TimeoutException("Rollback Endpoint did not complete handshake and roster locking within the configured Tick limit.");
                m_Peer.Pump();
                if (!m_Peer.IsReady)
                    return Status;
                ValidateRoster(m_Peer.Roster, m_Context.Registrations, m_Profile);
                var input = new RollbackEndpointInputSourcePort(
                    m_Model.SourceIdentity,
                    m_Peer,
                    LocalInput,
                    m_Context.TickRate,
                    m_Context.SourceClockId.Value,
                    m_Model.Policy);
                var restore = new RollbackSnapshotRestoreSource(m_Model.SourceIdentity);
                m_Prepared = new DeterministicRollbackPreparedSource(
                    m_Model,
                    new SimulationRuntimePortSet(new ISimulationRuntimePort[] { input, restore }),
                    input,
                    restore,
                    m_Peer);
                m_Peer = null;
                Status = SimulationSessionPreparationStatus.Ready;
                return Status;
            }
            catch (Exception exception)
            {
                Failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Preparation,
                    "deterministic_rollback_source_preparation_failed",
                    exception.Message,
                    Descriptor.Identity.ToString());
                Status = SimulationSessionPreparationStatus.Failed;
                m_Peer?.Dispose();
                m_Peer = null;
                return Status;
            }
        }

        public ISimulationSessionPreparedSource TakePreparedSource()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DeterministicRollbackSessionSourcePreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_Prepared == null)
                throw new InvalidOperationException("Rollback prepared Source is not available.");
            DeterministicRollbackPreparedSource result = m_Prepared;
            m_Prepared = null;
            return result;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Peer?.Dispose();
            m_Peer = null;
            m_Prepared?.Dispose();
            m_Prepared = null;
        }

        static void ValidateComposition(
            SimulationSessionSourcePreparationContext context,
            DeterministicRollbackModelDefinition model,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program)
        {
            if (context.TickRate != model.TickRate || program.Manifest.TickRate != context.TickRate ||
                !context.ProgramRuntime.NumericProfileId.Equals(FixedSimulationNumericProfile.Value.Id) ||
                !context.ProgramRuntime.TargetAbiVersion.Equals(FixedSimulationNumericProfile.Value.AbiVersion) ||
                context.ExecutionBackend is not FixedPassExecutionBackendDefinition ||
                !context.WorldSolver.Identity.ConfigurationHash.Equals(model.KccIdentityHash) ||
                !context.WorldIdentity.WorldConfigurationHash.Equals(model.CollisionWorldHash))
            {
                throw new InvalidOperationException("Rollback Source Program, Backend, TickRate, world, or KCC identity does not match the Session Composition.");
            }
        }

        static IDeterministicRollbackSimulationActorRegistration RequireRegistrations(
            IReadOnlyList<ISimulationActorRegistration> registrations,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram expectedProgram,
            ActorId localActorId)
        {
            IDeterministicRollbackSimulationActorRegistration local = null;
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i] is not IDeterministicRollbackSimulationActorRegistration registration)
                    throw new InvalidOperationException($"Actor '{registrations[i].ActorId}' is not a Deterministic Rollback Fixed registration.");
                ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = registration.Program;
                if (!program.Manifest.SemanticHash.Equals(expectedProgram.Manifest.SemanticHash) ||
                    !program.ProgramHash.Equals(expectedProgram.ProgramHash) ||
                    !program.LayoutHash.Equals(expectedProgram.LayoutHash))
                {
                    throw new InvalidOperationException($"Actor '{registration.ActorId}' Fixed Program does not match the Rollback Model.");
                }
                if (registration.ActorId.Equals(localActorId))
                    local = registration;
            }
            return local ?? throw new InvalidOperationException($"Rollback launch profile local Actor '{localActorId}' is absent from the Session roster.");
        }

        static void ValidateRoster(
            RollbackRoster roster,
            IReadOnlyList<ISimulationActorRegistration> registrations,
            RollbackPeerLaunchProfile profile)
        {
            if (roster == null || roster.Entries.Count != registrations.Count)
                throw new InvalidOperationException("Rollback Endpoint roster count does not match the Session Actor roster.");
            bool localFound = false;
            for (int i = 0; i < registrations.Count; i++)
            {
                if (!roster.Entries[i].ActorId.Equals(registrations[i].ActorId))
                    throw new InvalidOperationException("Rollback Endpoint roster Actor order does not match the Session Actor roster.");
                if (!roster.Entries[i].ActorId.Equals(profile.ActorId))
                    continue;
                localFound = string.Equals(roster.Entries[i].PeerId, profile.PeerId, StringComparison.Ordinal) &&
                             string.Equals(roster.Entries[i].PlayerId, profile.PlayerId, StringComparison.Ordinal);
            }
            if (!localFound)
                throw new InvalidOperationException("Rollback Endpoint roster does not contain the selected local Peer/Player/Actor identity.");
        }

        static RollbackHandshake BuildHandshake(DeterministicRollbackModelDefinition model, string peerId)
        {
            return new RollbackHandshake(
                peerId,
                model.ModelIdentity,
                model.SemanticHash,
                model.FixedProgramHash,
                model.FixedLayoutHash,
                model.TickRate,
                model.CollisionWorldHash,
                model.KccIdentityHash,
                DeterministicRollbackModelIdentity.Protocol);
        }
    }

    sealed class DeterministicRollbackPreparedSource : IDeterministicRollbackPreparedSource
    {
        RollbackPeerEndpoint m_Peer;
        readonly RollbackEndpointInputSourcePort m_Input;
        readonly RollbackSnapshotRestoreSource m_Restore;
        RollbackEndpointRuntimeBridge m_RuntimeBridge;
        bool m_Disposed;

        public DeterministicRollbackPreparedSource(
            DeterministicRollbackModelDefinition modelDefinition,
            SimulationRuntimePortSet runtimePorts,
            RollbackEndpointInputSourcePort input,
            RollbackSnapshotRestoreSource restore,
            RollbackPeerEndpoint peer)
        {
            ModelDefinition = modelDefinition ?? throw new ArgumentNullException(nameof(modelDefinition));
            Descriptor = modelDefinition.SourceDescriptor;
            RuntimePorts = runtimePorts ?? throw new ArgumentNullException(nameof(runtimePorts));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Restore = restore ?? throw new ArgumentNullException(nameof(restore));
            m_Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            RuntimeLauncher = new DeterministicRollbackRuntimeLauncher(Descriptor);
        }

        public DeterministicRollbackModelDefinition ModelDefinition { get; }
        public string LocalPeerId => m_Peer?.LocalPeerId ?? throw new ObjectDisposedException(nameof(DeterministicRollbackPreparedSource));
        public SimulationSessionSourceDescriptor Descriptor { get; }
        public SimulationRuntimePortSet RuntimePorts { get; }
        public IFixedSimulationSessionRuntimeLauncher RuntimeLauncher { get; }
        public IFixedSimulationRestoreSource RestoreSource => m_Restore;
        public IFixedSourceEgressOutputPort SourceEgress => m_RuntimeBridge ??
            throw new InvalidOperationException("Rollback prepared Source Runtime bridge is not bound.");

        public IFixedSourceEgressOutputPort BindRuntime(
            RollbackRuntimeState state,
            IFixedSimulationSessionSnapshotCodec snapshotCodec,
            DeterministicRollbackModelPolicy policy)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(DeterministicRollbackPreparedSource));
            if (m_RuntimeBridge != null)
                throw new InvalidOperationException("Rollback prepared Source Runtime is already bound.");
            m_Restore.Bind(state);
            state.BindInputSource(m_Input);
            m_RuntimeBridge = new RollbackEndpointRuntimeBridge(m_Peer, state, snapshotCodec, policy);
            m_Input.BindRuntimeBridge(m_RuntimeBridge);
            return m_RuntimeBridge;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Peer?.Dispose();
            m_Peer = null;
            m_RuntimeBridge = null;
        }
    }
}
