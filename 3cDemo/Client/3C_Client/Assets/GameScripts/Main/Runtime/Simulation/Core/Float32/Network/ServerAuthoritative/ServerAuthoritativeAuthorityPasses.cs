using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    public sealed class AuthorityAcceptedInputIngressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public AuthorityAcceptedInputIngressPassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.AcceptedInputIngress(policy));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new AuthorityAcceptedInputIngressReads(
                context.BindSourcePort<IServerAuthoritativeAcceptedInputSourcePort>(ServerAuthoritativeSourcePortContracts.AcceptedInputPortId));
            var writes = new AuthorityAcceptedInputIngressWrites(
                context.Products.BindExclusiveWriter<AcceptedAuthorityInputBatch>(ServerAuthoritativeProducts.AcceptedAuthorityInputBatch));
            return new Float32IngressPassRuntimeAdapter<AuthorityAcceptedInputIngressReads, AuthorityAcceptedInputIngressWrites>(
                new AuthorityAcceptedInputIngressPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class AuthorityAcceptedInputIngressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationIngressPassRuntime<AuthorityAcceptedInputIngressReads, AuthorityAcceptedInputIngressWrites>
    {
        public AuthorityAcceptedInputIngressPassRuntime(SimulationPipelinePassDescriptor descriptor) : base(descriptor) { }

        public void Execute(
            SimulationPipelineIngressContext context,
            AuthorityAcceptedInputIngressReads readPorts,
            AuthorityAcceptedInputIngressWrites writePorts)
        {
            RequireExecution();
            AcceptedAuthorityInputBatch batch = readPorts.Source.Read(context.Source) ??
                throw new InvalidOperationException("Authority accepted-input Source returned no canonical batch.");
            writePorts.Batch.Write(batch);
        }
    }

    public sealed class AuthorityAcceptedInputIngressReads : ISimulationPipelineReadPortSet
    {
        public AuthorityAcceptedInputIngressReads(IServerAuthoritativeAcceptedInputSourcePort source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public IServerAuthoritativeAcceptedInputSourcePort Source { get; }
    }

    public sealed class AuthorityAcceptedInputIngressWrites : ISimulationPipelineWritePortSet
    {
        public AuthorityAcceptedInputIngressWrites(
            IExclusiveSimulationPipelineProductWriter<AcceptedAuthorityInputBatch> batch)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
        }

        public IExclusiveSimulationPipelineProductWriter<AcceptedAuthorityInputBatch> Batch { get; }
    }

    public sealed class AuthorityTickSchedulePassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public AuthorityTickSchedulePassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.AuthorityTickSchedule(policy),
                ServerAuthoritativeAuthorityPassIds.ScheduleStateSchema);
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new AuthorityTickScheduleReads(
                context.Products.BindExclusiveReader<AcceptedAuthorityInputBatch>(ServerAuthoritativeProducts.AcceptedAuthorityInputBatch),
                context.BindSourcePort<IServerAuthoritativeAuthorityClockSourcePort>(ServerAuthoritativeSourcePortContracts.AuthorityClockPortId),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new AuthorityTickScheduleWrites(
                context.Products.BindExclusiveWriter<SimulationSessionExecutionPlan<Float32SimulationStep>>(SimulationPipelineProducts.ExecutionPlan));
            return new Float32SchedulePassRuntimeAdapter<AuthorityTickScheduleReads, AuthorityTickScheduleWrites>(
                new AuthorityTickSchedulePassRuntime(context.Pass.Descriptor, m_Policy),
                reads,
                writes);
        }
    }

    public sealed class AuthorityTickSchedulePassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationExecutionPlanSchedulePassRuntime<AuthorityTickScheduleReads, AuthorityTickScheduleWrites>,
        ISimulationPipelineStateParticipant
    {
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly SortedDictionary<ActorId, HeldAuthorityInput> m_Held =
            new SortedDictionary<ActorId, HeldAuthorityInput>();

        public AuthorityTickSchedulePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            ServerAuthoritativeModelPolicy policy) : base(descriptor)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                ServerAuthoritativeAuthorityPassIds.ScheduleStateOwner,
                ServerAuthoritativeAuthorityPassIds.ScheduleStateSchema,
                1);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            byte[] checkpoint = Capture();
            return new SimulationPipelinePassStateCheckpoint(StateIdentity, () => Restore(checkpoint));
        }

        public void Execute(
            SimulationPipelineScheduleContext context,
            AuthorityTickScheduleReads readPorts,
            AuthorityTickScheduleWrites writePorts)
        {
            RequireExecution();
            AcceptedAuthorityInputBatch accepted = readPorts.Accepted.Read();
            SimulationTick authorityTick = readPorts.Clock.ReadAuthorityTick(context.Source);
            if (authorityTick.Value != checked(context.CurrentCompletedTick + 1) ||
                accepted.AuthorityTick != authorityTick)
            {
                throw new InvalidOperationException("Authority clock, accepted input and Session Tick are not contiguous.");
            }
            Accept(accepted, authorityTick);
            var authoritySource = new SimulationTickSourceIdentity(
                SimulationTickSourceKind.Authoritative,
                context.Source.ClockId,
                authorityTick.Value);
            var actorInputs = new List<SimulationPipelineActorInput<Float32StepInput>>();
            for (int i = 0; i < readPorts.ProgramRuntime.Roster.Count; i++)
            {
                ActorId actorId = readPorts.ProgramRuntime.Roster[i].ActorId;
                if (!m_Held.TryGetValue(actorId, out HeldAuthorityInput held))
                {
                    writePorts.ExecutionPlan.Write(Pending(context, readPorts.ProgramRuntime));
                    return;
                }
                CharacterSimulationInput input = BuildInput(held, authoritySource, authorityTick);
                actorInputs.Add(new SimulationPipelineActorInput<Float32StepInput>(
                    actorId,
                    held.InputSequence,
                    new Float32StepInput(input)));
                held.MarkConsumed(authorityTick);
            }
            var step = new Float32SimulationStep(
                authorityTick,
                new SimulationPipelineStepProvenance(
                    SimulationPipelineStepExecutionKind.Authoritative,
                    authoritySource,
                    authorityTick.Value),
                actorInputs,
                Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>(),
                ObservedWorldConstraintFrame.Empty(authorityTick));
            writePorts.ExecutionPlan.Write(new SimulationSessionExecutionPlan<Float32SimulationStep>(
                SimulationSessionExecutionPlanStatus.Executable,
                context.Source,
                readPorts.ProgramRuntime.Catalog.CatalogHash,
                context.Pipeline.Hash,
                Roster(readPorts.ProgramRuntime),
                new[]
                {
                    new SimulationPipelineStepSourceMapping(
                        context.Source.ClockId,
                        context.Source.ClockId,
                        SimulationTickSourceKind.Authoritative)
                },
                null,
                new[] { step },
                SimulationSessionPlanRequirement.WorkingState |
                SimulationSessionPlanRequirement.OutputDisposition |
                SimulationSessionPlanRequirement.StateHash));
            if (readPorts.Diagnostics.Sink.IsEnabled)
            {
                for (int i = 0; i < actorInputs.Count; i++)
                {
                    readPorts.Diagnostics.Sink.PublishModel(new SimulationModelTraceRecord(
                        SimulationModelTraceKind.Queue,
                        "authority_schedule",
                        $"roster={actorInputs.Count};held={m_Held.Count};missingPolicy={m_Policy.MissingInputPolicy};maxLag={m_Policy.MaximumInputLagTicks}",
                        actorInputs[i].ActorId,
                        context.Source.SourceTick,
                        authorityTick.Value,
                        actorInputs[i].Sequence,
                        0,
                        m_Held.Count));
                }
            }
        }

        void Accept(AcceptedAuthorityInputBatch batch, SimulationTick authorityTick)
        {
            for (int i = 0; i < batch.Inputs.Count; i++)
            {
                AcceptedAuthorityInput input = batch.Inputs[i];
                if (m_Held.TryGetValue(input.ActorId, out HeldAuthorityInput previous) &&
                    input.InputSequence <= previous.InputSequence)
                {
                    continue;
                }
                m_Held[input.ActorId] = new HeldAuthorityInput(
                    input.ActorId,
                    input.InputSequence,
                    input.Input,
                    authorityTick);
            }
        }

        CharacterSimulationInput BuildInput(
            HeldAuthorityInput held,
            SimulationTickSourceIdentity source,
            SimulationTick authorityTick)
        {
            bool fresh = held.AcceptedTick == authorityTick && held.LastConsumedTick < authorityTick.Value;
            int age = checked((int)(authorityTick.Value - held.AcceptedTick.Value));
            bool reuse = fresh || m_Policy.MissingInputPolicy == ServerAuthoritativeMissingInputPolicy.ReuseLastCanonicalInput &&
                age <= m_Policy.MaximumInputLagTicks;
            IReadOnlyList<SimulationInputValue> values = reuse
                ? held.Input.Values
                : NeutralValues(held.Input.Values);
            IReadOnlyList<SimulationInputRequest> requests = fresh
                ? held.Input.Requests
                : Array.Empty<SimulationInputRequest>();
            return new CharacterSimulationInput(
                held.Input.NumericProfile,
                source,
                held.Input.InputSourceIdentity,
                held.InputSequence,
                values,
                requests);
        }

        static SimulationInputValue[] NeutralValues(IReadOnlyList<SimulationInputValue> values)
        {
            var result = new SimulationInputValue[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                SimulationInputValue value = values[i];
                result[i] = value.Kind switch
                {
                    SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(value.InputId, false),
                    SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(value.InputId, Float32Scalar.Zero),
                    SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(value.InputId, Float32Vector2.Zero),
                    SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(value.InputId, Float32Vector3.Zero),
                    SimulationInputValueKind.Yaw => SimulationInputValue.FromYaw(value.InputId, Float32Yaw.Zero),
                    SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(value.InputId, SimulationActionTargetSnapshot.None),
                    _ => throw new InvalidDataException($"Unsupported input value kind '{value.Kind}'.")
                };
            }
            return result;
        }

        static SimulationSessionExecutionPlan<Float32SimulationStep> Pending(
            SimulationPipelineScheduleContext context,
            IFloat32ProgramRuntimePort runtime) =>
            new SimulationSessionExecutionPlan<Float32SimulationStep>(
                SimulationSessionExecutionPlanStatus.Pending,
                context.Source,
                runtime.Catalog.CatalogHash,
                context.Pipeline.Hash,
                Roster(runtime),
                Array.Empty<SimulationPipelineStepSourceMapping>(),
                null,
                Array.Empty<Float32SimulationStep>(),
                SimulationSessionPlanRequirement.None);

        static SimulationActorRosterDescriptor Roster(IFloat32ProgramRuntimePort runtime)
        {
            var actors = new ActorId[runtime.Roster.Count];
            for (int i = 0; i < actors.Length; i++)
                actors[i] = runtime.Roster[i].ActorId;
            return new SimulationActorRosterDescriptor(actors);
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            byte[] payload = Capture();
            return new SimulationPipelinePassStateSnapshot(
                Descriptor.PassId,
                Descriptor.ImplementationVersion,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                StateIdentity.StateSchemaVersion,
                SimulationCanonicalPayloadHash.Compute(payload),
                payload);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new ServerAuthoritativePassStateRestoreTransaction(StateIdentity, snapshot, Capture, Restore);
        }

        byte[] Capture()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x48494153);
            writer.WriteInt32(1);
            writer.WriteInt32(m_Held.Count);
            foreach (KeyValuePair<ActorId, HeldAuthorityInput> pair in m_Held)
            {
                writer.WriteString(pair.Key.Value);
                writer.WriteUInt64(pair.Value.InputSequence);
                writer.WriteUInt64(pair.Value.AcceptedTick.Value);
                writer.WriteUInt64(pair.Value.LastConsumedTick);
                writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteInput(pair.Value.Input));
            }
            return writer.ToArray();
        }

        void Restore(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != 0x48494153 || reader.ReadInt32() != 1)
                throw new InvalidDataException("Authority input hold state header is invalid.");
            int count = reader.ReadInt32();
            if (count < 0 || count > 64)
                throw new InvalidDataException("Authority input hold state count is invalid.");
            m_Held.Clear();
            for (int i = 0; i < count; i++)
            {
                var actorId = new ActorId(reader.ReadString());
                ulong sequence = reader.ReadUInt64();
                var acceptedTick = new SimulationTick(reader.ReadUInt64());
                ulong consumedTick = reader.ReadUInt64();
                CharacterSimulationInput input = ServerAuthoritativeCanonicalCodec.ReadInput(reader.ReadBytes());
                m_Held.Add(actorId, new HeldAuthorityInput(actorId, sequence, input, acceptedTick, consumedTick));
            }
            reader.RequireComplete();
        }

        sealed class HeldAuthorityInput
        {
            public HeldAuthorityInput(
                ActorId actorId,
                ulong inputSequence,
                CharacterSimulationInput input,
                SimulationTick acceptedTick,
                ulong lastConsumedTick = 0)
            {
                ActorId = actorId;
                InputSequence = inputSequence;
                Input = input ?? throw new ArgumentNullException(nameof(input));
                AcceptedTick = acceptedTick;
                LastConsumedTick = lastConsumedTick;
            }

            public ActorId ActorId { get; }
            public ulong InputSequence { get; }
            public CharacterSimulationInput Input { get; }
            public SimulationTick AcceptedTick { get; }
            public ulong LastConsumedTick { get; private set; }
            public void MarkConsumed(SimulationTick tick) => LastConsumedTick = tick.Value;
        }
    }

    public sealed class AuthorityTickScheduleReads : ISimulationPipelineReadPortSet
    {
        public AuthorityTickScheduleReads(
            IReadOnlySimulationPipelineProductPort<AcceptedAuthorityInputBatch> accepted,
            IServerAuthoritativeAuthorityClockSourcePort clock,
            IFloat32ProgramRuntimePort programRuntime,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            Accepted = accepted ?? throw new ArgumentNullException(nameof(accepted));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<AcceptedAuthorityInputBatch> Accepted { get; }
        public IServerAuthoritativeAuthorityClockSourcePort Clock { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class AuthorityTickScheduleWrites : ISimulationPipelineWritePortSet
    {
        public AuthorityTickScheduleWrites(
            IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<Float32SimulationStep>> executionPlan)
        {
            ExecutionPlan = executionPlan ?? throw new ArgumentNullException(nameof(executionPlan));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<Float32SimulationStep>> ExecutionPlan { get; }
    }

    public sealed class AuthorityReplicationEgressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly ServerAuthoritativeReplicationPolicy m_ReplicationPolicy;
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public AuthorityReplicationEgressPassRuntimeFactory(
            ServerAuthoritativeModelPolicy policy,
            ServerAuthoritativeReplicationPolicy replicationPolicy)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_ReplicationPolicy = replicationPolicy ?? throw new ArgumentNullException(nameof(replicationPolicy));
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.AuthorityReplicationEgress(policy, replicationPolicy),
                ServerAuthoritativeAuthorityPassIds.ReplicationStateSchema);
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new AuthorityReplicationEgressReads(
                context.Products.BindExclusiveReader<AcceptedAuthorityInputBatch>(ServerAuthoritativeProducts.AcceptedAuthorityInputBatch),
                context.Products.BindAppendReader<Float32FinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult),
                context.BindSourcePort<IServerAuthoritativeNetworkSendPort>(ServerAuthoritativeSourcePortContracts.AuthoritySendPortId),
                context.BindSourcePort<IServerAuthoritativeFullBaselineRequestSourcePort>(ServerAuthoritativeSourcePortContracts.FullBaselineRequestPortId),
                context.BindTargetPort<IFloat32CompletedStepReadPort>(Float32PipelineRuntimePortIds.CompletedSteps),
                context.BindTargetPort<IFloat32ProgramRuntimePort>(Float32PipelineRuntimePortIds.ProgramRuntime),
                context.BindSolverPort<IFloat32WorldSolverRuntimePort>(Float32PipelineRuntimePortIds.WorldSolver),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new AuthorityReplicationEgressWrites(
                context.Products.BindExclusiveWriter<AuthorityReplicationBatch>(ServerAuthoritativeProducts.AuthorityReplicationBatch),
                context.Products.BindExclusiveWriter<SimulationPipelineOutputDispositionSet>(SimulationPipelineProducts.OutputDispositionSet),
                context.Products.BindAppendWriter<Float32SourceEgressRecord>(SimulationPipelineProducts.SourceEgress));
            return new Float32EgressPassRuntimeAdapter<AuthorityReplicationEgressReads, AuthorityReplicationEgressWrites>(
                new AuthorityReplicationEgressPassRuntime(context.Pass.Descriptor, m_Policy, m_ReplicationPolicy),
                reads,
                writes);
        }
    }

    public sealed class AuthorityReplicationEgressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<AuthorityReplicationEgressReads, AuthorityReplicationEgressWrites>,
        ISimulationPipelineStateParticipant
    {
        readonly ServerAuthoritativeModelPolicy m_Policy;
        readonly ServerAuthoritativeReplicationPolicy m_ReplicationPolicy;
        readonly SortedDictionary<ActorId, ServerAuthoritativeEventHorizon> m_Horizons =
            new SortedDictionary<ActorId, ServerAuthoritativeEventHorizon>();
        readonly List<AuthoritativeActorBaseline> m_Baselines = new List<AuthoritativeActorBaseline>();
        readonly List<AuthoritativeInputAck> m_Acks = new List<AuthoritativeInputAck>();
        readonly List<RemotePresentationBatch> m_Remote = new List<RemotePresentationBatch>();
        readonly List<SimulationOutputDisposition> m_Dispositions = new List<SimulationOutputDisposition>();
        readonly List<PresentationCommand> m_SampleCommands = new List<PresentationCommand>();
        readonly List<ServerAuthoritativeReliableEvent> m_ReliableEvents = new List<ServerAuthoritativeReliableEvent>();
        readonly CharacterBodySample[] m_BodySample = new CharacterBodySample[1];

        public AuthorityReplicationEgressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            ServerAuthoritativeModelPolicy policy,
            ServerAuthoritativeReplicationPolicy replicationPolicy) : base(descriptor)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_ReplicationPolicy = replicationPolicy ?? throw new ArgumentNullException(nameof(replicationPolicy));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                ServerAuthoritativeAuthorityPassIds.ReplicationStateOwner,
                ServerAuthoritativeAuthorityPassIds.ReplicationStateSchema,
                1);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            byte[] checkpoint = Capture();
            return new SimulationPipelinePassStateCheckpoint(StateIdentity, () => Restore(checkpoint));
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            AuthorityReplicationEgressReads readPorts,
            AuthorityReplicationEgressWrites writePorts)
        {
            RequireExecution();
            ClearScratch();
            try
            {
                AcceptedAuthorityInputBatch accepted = readPorts.Accepted.Read();
                if (readPorts.Completed.Steps.Count != 1 ||
                    readPorts.Completed.Steps[0].Step.ExecutionKind != SimulationPipelineStepExecutionKind.Authoritative)
                {
                    throw new InvalidOperationException("Authority Egress requires exactly one authoritative completed Step.");
                }
                Float32CompletedSimulationStep completed = readPorts.Completed.Steps[0];
                if (readPorts.Results.Count != completed.Result.Actors.Count)
                    throw new InvalidOperationException("Authority finalized result count does not match the completed Step.");
                ulong snapshotInterval = checked((ulong)(m_Policy.SimulationTickRate / m_Policy.SnapshotPacketRate));
                bool emitBaseline = completed.Step.Tick.Value == 1 ||
                    completed.Step.Tick.Value % snapshotInterval == 0 ||
                    readPorts.FullBaselineRequest.IsRequested;
                for (int i = 0; i < completed.Result.Actors.Count; i++)
                {
                    SimulationActorTickResult actor = completed.Result.Actors[i];
                    m_SampleCommands.Clear();
                    m_ReliableEvents.Clear();
                    for (int eventIndex = 0; eventIndex < actor.GameplayFacts.Count; eventIndex++)
                    {
                        GameplayFact fact = actor.GameplayFacts[eventIndex];
                        if (m_ReplicationPolicy.ShouldReplicateReliably(fact))
                            m_ReliableEvents.Add(new ServerAuthoritativeReliableEvent(fact));
                        m_Dispositions.Add(new SimulationOutputDisposition(
                            fact.Header.EventId,
                            actor.ActorId,
                            SimulationOutputDispositionKind.Suppress));
                        AdvanceHorizon(actor.ActorId, fact.Header);
                    }
                    for (int eventIndex = 0; eventIndex < actor.PresentationCommands.Count; eventIndex++)
                    {
                        PresentationCommand command = actor.PresentationCommands[eventIndex];
                        if (m_ReplicationPolicy.ShouldStream(command))
                            m_SampleCommands.Add(command);
                        if (m_ReplicationPolicy.ShouldReplicateReliably(command))
                            m_ReliableEvents.Add(new ServerAuthoritativeReliableEvent(command));
                        m_Dispositions.Add(new SimulationOutputDisposition(
                            command.Header.EventId,
                            actor.ActorId,
                            SimulationOutputDispositionKind.Suppress));
                        AdvanceHorizon(actor.ActorId, command.Header);
                    }
                    m_BodySample[0] = actor.BodySample;
                    m_Remote.Add(new RemotePresentationBatch(actor.ActorId, m_BodySample, m_SampleCommands, m_ReliableEvents, false));
                    int acceptedIndex = FindAcceptedInput(accepted, actor.ActorId);
                    ServerAuthoritativeEventHorizon horizon = m_Horizons.TryGetValue(actor.ActorId, out ServerAuthoritativeEventHorizon currentHorizon)
                        ? currentHorizon
                        : ServerAuthoritativeEventHorizon.Empty;
                    m_Acks.Add(new AuthoritativeInputAck(
                        actor.ActorId,
                        completed.Step.Tick,
                        accepted.Inputs[acceptedIndex].InputSequence,
                        horizon));
                    if (emitBaseline)
                        m_Baselines.Add(BuildBaseline(actor, completed, readPorts));
                }
                var replication = new AuthorityReplicationBatch(completed.Step.Tick, m_Acks, m_Baselines, m_Remote);
                writePorts.Replication.Write(replication);
                writePorts.Dispositions.Write(new SimulationPipelineOutputDispositionSet(context.TransactionIdentity, m_Dispositions));
                writePorts.Egress.Append(
                    new SimulationPipelineAppendEntryIdentity(
                        completed.Result.Actors[0].ActorId,
                        completed.Step.Tick,
                        completed.Step.Tick.Value,
                        context.Source),
                    new Float32SourceEgressRecord(
                        completed.Result.Actors[0].ActorId,
                        completed.Step.Tick,
                        ServerAuthoritativeEgressChannels.AuthorityReplication,
                        ServerAuthoritativeEgressChannels.AuthorityReplicationSchema,
                        ServerAuthoritativeEgressChannels.AuthorityReplicationSchemaVersion,
                        ServerAuthoritativeEgressCodec.WriteAuthorityReplication(replication)));
                if (readPorts.Diagnostics.Sink.IsEnabled)
                {
                    readPorts.Diagnostics.Sink.PublishModel(new SimulationModelTraceRecord(
                        SimulationModelTraceKind.Transport,
                        "authority_replication_commit",
                        $"acks={m_Acks.Count};baselines={m_Baselines.Count};remote={m_Remote.Count};fullBaseline={emitBaseline};reliable={m_Dispositions.Count}",
                        default,
                        context.Source.SourceTick,
                        completed.Step.Tick.Value,
                        0,
                        0,
                        0,
                        0,
                        0f,
                        0f,
                        true));
                }
            }
            finally
            {
                ClearScratch();
            }
        }

        void ClearScratch()
        {
            m_Baselines.Clear();
            m_Acks.Clear();
            m_Remote.Clear();
            m_Dispositions.Clear();
            m_SampleCommands.Clear();
            m_ReliableEvents.Clear();
            m_BodySample[0] = default;
        }

        AuthoritativeActorBaseline BuildBaseline(
            SimulationActorTickResult actor,
            Float32CompletedSimulationStep completed,
            AuthorityReplicationEgressReads ports)
        {
            CharacterSimulationProgram program = ports.ProgramRuntime.Catalog.GetRequired(actor.State.ProgramId);
            int actorIndex = FindActorInput(completed.Step, actor.ActorId);
            ulong inputSequence = completed.Step.Inputs[actorIndex].Sequence;
            ServerAuthoritativeEventHorizon horizon = m_Horizons.TryGetValue(actor.ActorId, out ServerAuthoritativeEventHorizon value)
                ? value
                : ServerAuthoritativeEventHorizon.Empty;
            return new AuthoritativeActorBaseline(
                actor.ActorId,
                completed.Step.Tick,
                program.Manifest.NumericProfile,
                program.Manifest.NumericProfile.AbiVersion,
                CharacterSimulationStateCodec.CodecIdentity,
                program.ProgramHash,
                program.LayoutHash,
                program.Manifest.OperationSetVersion,
                CharacterSimulationStateCodec.Write(actor.State),
                actor.StateHash,
                completed.State.WorldState.WorldRevision,
                completed.State.WorldState.SolverId,
                completed.State.WorldState.SolverVersion,
                ports.Solver.Solver.Descriptor.Capabilities,
                actor.BodySample.FinalBody,
                inputSequence,
                horizon);
        }

        static int FindActorInput(Float32SimulationStep step, ActorId actorId)
        {
            for (int i = 0; i < step.Inputs.Count; i++)
            {
                if (step.Inputs[i].ActorId == actorId)
                    return i;
            }
            throw new InvalidOperationException($"Authority Step has no input for Actor '{actorId}'.");
        }

        static int FindAcceptedInput(AcceptedAuthorityInputBatch batch, ActorId actorId)
        {
            for (int i = 0; i < batch.Inputs.Count; i++)
            {
                if (batch.Inputs[i].ActorId == actorId)
                    return i;
            }
            throw new InvalidOperationException($"Authority accepted input batch has no Actor '{actorId}'.");
        }

        void AdvanceHorizon(ActorId actorId, SimulationEventHeader header)
        {
            if (!m_Horizons.TryGetValue(actorId, out ServerAuthoritativeEventHorizon current) ||
                header.Sequence > current.Sequence)
            {
                m_Horizons[actorId] = new ServerAuthoritativeEventHorizon(header.Sequence, header.EventId);
            }
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            byte[] payload = Capture();
            return new SimulationPipelinePassStateSnapshot(
                Descriptor.PassId,
                Descriptor.ImplementationVersion,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                StateIdentity.StateSchemaVersion,
                SimulationCanonicalPayloadHash.Compute(payload),
                payload);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new ServerAuthoritativePassStateRestoreTransaction(StateIdentity, snapshot, Capture, Restore);
        }

        byte[] Capture()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x52494153);
            writer.WriteInt32(1);
            writer.WriteInt32(m_Horizons.Count);
            foreach (KeyValuePair<ActorId, ServerAuthoritativeEventHorizon> pair in m_Horizons)
            {
                writer.WriteString(pair.Key.Value);
                writer.WriteUInt64(pair.Value.Sequence);
                writer.WriteString(pair.Value.EventId.ToString());
            }
            return writer.ToArray();
        }

        void Restore(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != 0x52494153 || reader.ReadInt32() != 1)
                throw new InvalidDataException("Authority replication state header is invalid.");
            int count = reader.ReadInt32();
            if (count < 0 || count > 64)
                throw new InvalidDataException("Authority replication state count is invalid.");
            m_Horizons.Clear();
            for (int i = 0; i < count; i++)
            {
                var actorId = new ActorId(reader.ReadString());
                ulong sequence = reader.ReadUInt64();
                var eventId = new EventId(new StableHash(reader.ReadString()));
                m_Horizons.Add(actorId, new ServerAuthoritativeEventHorizon(sequence, eventId));
            }
            reader.RequireComplete();
        }
    }

    public sealed class AuthorityReplicationEgressReads : ISimulationPipelineReadPortSet
    {
        public AuthorityReplicationEgressReads(
            IReadOnlySimulationPipelineProductPort<AcceptedAuthorityInputBatch> accepted,
            IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> results,
            IServerAuthoritativeNetworkSendPort send,
            IServerAuthoritativeFullBaselineRequestSourcePort fullBaselineRequest,
            IFloat32CompletedStepReadPort completed,
            IFloat32ProgramRuntimePort programRuntime,
            IFloat32WorldSolverRuntimePort solver,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            Accepted = accepted ?? throw new ArgumentNullException(nameof(accepted));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            Send = send ?? throw new ArgumentNullException(nameof(send));
            FullBaselineRequest = fullBaselineRequest ?? throw new ArgumentNullException(nameof(fullBaselineRequest));
            Completed = completed ?? throw new ArgumentNullException(nameof(completed));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<AcceptedAuthorityInputBatch> Accepted { get; }
        public IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> Results { get; }
        public IServerAuthoritativeNetworkSendPort Send { get; }
        public IServerAuthoritativeFullBaselineRequestSourcePort FullBaselineRequest { get; }
        public IFloat32CompletedStepReadPort Completed { get; }
        public IFloat32ProgramRuntimePort ProgramRuntime { get; }
        public IFloat32WorldSolverRuntimePort Solver { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class AuthorityReplicationEgressWrites : ISimulationPipelineWritePortSet
    {
        public AuthorityReplicationEgressWrites(
            IExclusiveSimulationPipelineProductWriter<AuthorityReplicationBatch> replication,
            IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> dispositions,
            IAppendOnlySimulationPipelineProductWriter<Float32SourceEgressRecord> egress)
        {
            Replication = replication ?? throw new ArgumentNullException(nameof(replication));
            Dispositions = dispositions ?? throw new ArgumentNullException(nameof(dispositions));
            Egress = egress ?? throw new ArgumentNullException(nameof(egress));
        }

        public IExclusiveSimulationPipelineProductWriter<AuthorityReplicationBatch> Replication { get; }
        public IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> Dispositions { get; }
        public IAppendOnlySimulationPipelineProductWriter<Float32SourceEgressRecord> Egress { get; }
    }
}
