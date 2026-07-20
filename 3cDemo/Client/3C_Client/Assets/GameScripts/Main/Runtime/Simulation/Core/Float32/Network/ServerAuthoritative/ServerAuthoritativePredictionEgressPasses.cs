using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    static class PredictionCompletedStepInput
    {
        public static OwnerCanonicalInputBatch Read(Float32CompletedSimulationStep step)
        {
            if (step == null || step.Step.Inputs.Count != 1)
                throw new InvalidOperationException("Prediction Step must contain exactly one owner input.");
            SimulationPipelineActorInput<Float32StepInput> actorInput = step.Step.Inputs[0];
            return new OwnerCanonicalInputBatch(
                actorInput.ActorId,
                step.Step.Source.SourceTick,
                actorInput.Sequence,
                actorInput.Value.Input);
        }
    }

    public sealed class PredictionHistoryEgressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public PredictionHistoryEgressPassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.HistoryEgress(policy),
                ServerAuthoritativePredictionPassIds.HistoryStateSchema);
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new PredictionHistoryEgressReads(
                context.Products.BindExclusiveReader<OwnerCanonicalInputBatch>(ServerAuthoritativeProducts.OwnerCanonicalInputBatch),
                context.BindSourcePort<IServerAuthoritativePredictionStatePort>(ServerAuthoritativeSourcePortContracts.PredictionStatePortId),
                context.BindTargetPort<IFloat32CompletedStepReadPort>(Float32PipelineRuntimePortIds.CompletedSteps),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            return new Float32EgressPassRuntimeAdapter<PredictionHistoryEgressReads, EmptyServerAuthoritativeWritePorts>(
                new PredictionHistoryEgressPassRuntime(context.Pass.Descriptor, reads.PredictionState.State),
                reads,
                EmptyServerAuthoritativeWritePorts.Instance);
        }
    }

    public sealed class PredictionHistoryEgressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<PredictionHistoryEgressReads, EmptyServerAuthoritativeWritePorts>,
        ISimulationPipelineStateParticipant
    {
        readonly ServerAuthoritativePredictionState m_State;

        public PredictionHistoryEgressPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            ServerAuthoritativePredictionState state) : base(descriptor)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                ServerAuthoritativePredictionPassIds.HistoryStateOwner,
                ServerAuthoritativePredictionPassIds.HistoryStateSchema,
                ServerAuthoritativePredictionPassIds.HistoryStateSchemaVersion);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.ReconstructForRestore;

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            ServerAuthoritativePredictionHistoryCheckpoint checkpoint = m_State.CaptureHistoryCheckpoint();
            return new SimulationPipelinePassStateCheckpoint(
                StateIdentity,
                () => m_State.RestoreHistoryCheckpoint(checkpoint));
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            PredictionHistoryEgressReads readPorts,
            EmptyServerAuthoritativeWritePorts writePorts)
        {
            RequireExecution();
            OwnerCanonicalInputBatch currentInput = readPorts.OwnerInput.Read();
            int currentCount = 0;
            for (int i = 0; i < readPorts.Completed.Steps.Count; i++)
            {
                Float32CompletedSimulationStep step = readPorts.Completed.Steps[i];
                if (step.Step.ExecutionKind == SimulationPipelineStepExecutionKind.Current)
                {
                    if (++currentCount > 2)
                        throw new InvalidOperationException("Prediction transaction contains more than two Current Steps.");
                }
                else if (step.Step.ExecutionKind != SimulationPipelineStepExecutionKind.Replay)
                    throw new InvalidOperationException("Prediction history accepts only Replay and Current Steps.");
                OwnerCanonicalInputBatch input = PredictionCompletedStepInput.Read(step);
                m_State.AddHistory(input, step);
            }
            if (readPorts.Diagnostics.Sink.IsEnabled)
            {
                readPorts.Diagnostics.Sink.PublishModel(new SimulationModelTraceRecord(
                    SimulationModelTraceKind.Queue,
                    "prediction_history_commit",
                    $"history={m_State.HistoryCount};journal={m_State.JournalCount};cursor={m_State.JournalCursor};lastBaseline={m_State.LastBaselineTick};lastAckTick={m_State.LastAuthorityAckTick}",
                    currentInput.ActorId,
                    currentInput.SourceTick,
                    m_State.LastBaselineTick,
                    currentInput.InputSequence,
                    m_State.ConfirmedInputSequence,
                    m_State.HistoryCount));
            }
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            return ServerAuthoritativePredictionStateSnapshot.Create(
                Descriptor.PassId,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                m_State.CaptureHistoryState(),
                ServerAuthoritativePredictionPassIds.HistoryStateSchemaVersion);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new ServerAuthoritativePassStateRestoreTransaction(
                StateIdentity,
                snapshot,
                m_State.CaptureHistoryState,
                m_State.RestoreHistoryState);
        }
    }

    public sealed class PredictionHistoryEgressReads : ISimulationPipelineReadPortSet
    {
        public PredictionHistoryEgressReads(
            IReadOnlySimulationPipelineProductPort<OwnerCanonicalInputBatch> ownerInput,
            IServerAuthoritativePredictionStatePort predictionState,
            IFloat32CompletedStepReadPort completed,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            OwnerInput = ownerInput ?? throw new ArgumentNullException(nameof(ownerInput));
            PredictionState = predictionState ?? throw new ArgumentNullException(nameof(predictionState));
            Completed = completed ?? throw new ArgumentNullException(nameof(completed));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<OwnerCanonicalInputBatch> OwnerInput { get; }
        public IServerAuthoritativePredictionStatePort PredictionState { get; }
        public IFloat32CompletedStepReadPort Completed { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class PredictionOutputDispositionPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public PredictionOutputDispositionPassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.OutputDisposition(policy),
                ServerAuthoritativePredictionPassIds.JournalStateSchema,
                ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion);
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new PredictionOutputDispositionReads(
                context.Products.BindExclusiveReader<PredictionCorrectionDecision>(ServerAuthoritativeProducts.PredictionCorrectionDecision),
                context.Products.BindAppendReader<Float32FinalizedActorResult>(SimulationPipelineProducts.FinalizedStepResult),
                context.BindSourcePort<IServerAuthoritativePredictionStatePort>(ServerAuthoritativeSourcePortContracts.PredictionStatePortId),
                context.BindTargetPort<IFloat32CompletedStepReadPort>(Float32PipelineRuntimePortIds.CompletedSteps),
                context.BindDiagnosticsPort<IFloat32DiagnosticsRuntimePort>(Float32PipelineRuntimePortIds.Diagnostics));
            var writes = new PredictionOutputDispositionWrites(
                context.Products.BindExclusiveWriter<SimulationPipelineOutputDispositionSet>(SimulationPipelineProducts.OutputDispositionSet));
            return new Float32EgressPassRuntimeAdapter<PredictionOutputDispositionReads, PredictionOutputDispositionWrites>(
                new PredictionOutputDispositionPassRuntime(context.Pass.Descriptor, reads.PredictionState.State),
                reads,
                writes);
        }
    }

    public sealed class PredictionOutputDispositionPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<PredictionOutputDispositionReads, PredictionOutputDispositionWrites>,
        ISimulationPipelineStateParticipant
    {
        readonly ServerAuthoritativePredictionState m_State;
        readonly List<SimulationOutputDisposition> m_Dispositions = new List<SimulationOutputDisposition>();

        public PredictionOutputDispositionPassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            ServerAuthoritativePredictionState state) : base(descriptor)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            StateIdentity = new SimulationPipelineStateParticipantIdentity(
                descriptor.PassId,
                descriptor.ImplementationVersion,
                ServerAuthoritativePredictionPassIds.JournalStateOwner,
                ServerAuthoritativePredictionPassIds.JournalStateSchema,
                ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion);
        }

        public SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        public SimulationPipelineStepProjectionMode StepProjectionMode => SimulationPipelineStepProjectionMode.Include;

        public ISimulationPipelinePassStateCheckpoint CaptureCheckpoint()
        {
            RequireCaptureOrRestore();
            ServerAuthoritativePredictionJournalCheckpoint checkpoint = m_State.CaptureJournalCheckpoint();
            return new SimulationPipelinePassStateCheckpoint(
                StateIdentity,
                () => m_State.RestoreJournalCheckpoint(checkpoint));
        }

        public void Execute(
            SimulationPipelineEgressContext context,
            PredictionOutputDispositionReads readPorts,
            PredictionOutputDispositionWrites writePorts)
        {
            RequireExecution();
            m_Dispositions.Clear();
            try
            {
                PredictionCorrectionDecision decision = readPorts.Decision.Read();
                int actorResultCount = 0;
                int publishedCount = 0;
                int duplicateCount = 0;
                SimulationTick currentTick = default;
                for (int stepIndex = 0; stepIndex < readPorts.Completed.Steps.Count; stepIndex++)
                {
                    Float32CompletedSimulationStep completed = readPorts.Completed.Steps[stepIndex];
                    bool replay = completed.Step.ExecutionKind == SimulationPipelineStepExecutionKind.Replay;
                    if (completed.Step.ExecutionKind == SimulationPipelineStepExecutionKind.Current)
                    {
                        if (currentTick.IsValid && completed.Step.Tick.Value != currentTick.Value + 1)
                            throw new InvalidOperationException("Prediction Current Steps are not contiguous.");
                        currentTick = completed.Step.Tick;
                    }
                    actorResultCount += completed.Result.Actors.Count;
                    for (int actorIndex = 0; actorIndex < completed.Result.Actors.Count; actorIndex++)
                    {
                        SimulationActorTickResult actor = completed.Result.Actors[actorIndex];
                        Add(actor.GameplayFacts, value => value.Header, actor.ActorId, replay, m_Dispositions, ref publishedCount, ref duplicateCount);
                        Add(actor.PresentationCommands, value => value.Header, actor.ActorId, replay, m_Dispositions, ref publishedCount, ref duplicateCount);
                    }
                }
                if (actorResultCount != readPorts.Results.Count)
                    throw new InvalidOperationException("Prediction finalized result count does not match completed Steps.");
                if (currentTick.IsValid)
                    m_State.SealHistoryJournalCursor(currentTick);
                writePorts.Dispositions.Write(new SimulationPipelineOutputDispositionSet(context.TransactionIdentity, m_Dispositions));
                if (readPorts.Diagnostics.Sink.IsEnabled)
                {
                    readPorts.Diagnostics.Sink.PublishModel(new SimulationModelTraceRecord(
                        SimulationModelTraceKind.OutputDisposition,
                        "prediction_output_disposition",
                        $"decision={decision.Kind};published={publishedCount};suppressedDuplicate={duplicateCount};predictedRejected={m_State.LastRejectedCount};journal={m_State.JournalCount};cursor={m_State.JournalCursor}",
                        m_Dispositions.Count == 0 ? default : m_Dispositions[0].ActorId,
                        context.Source.SourceTick,
                        decision.BaselineTick.Value,
                        0,
                        m_State.ConfirmedInputSequence,
                        0,
                        context.CompletedStepCount));
                }
            }
            finally
            {
                m_Dispositions.Clear();
            }
        }

        void Add<T>(
            IReadOnlyList<T> values,
            Func<T, SimulationEventHeader> header,
            ActorId actorId,
            bool replay,
            List<SimulationOutputDisposition> destination,
            ref int publishedCount,
            ref int duplicateCount)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader eventHeader = header(values[i]);
                EventId eventId = eventHeader.EventId;
                bool duplicate = replay && m_State.WasCommitted(eventId);
                if (duplicate)
                    duplicateCount++;
                else
                    publishedCount++;
                destination.Add(new SimulationOutputDisposition(
                    eventId,
                    actorId,
                    duplicate ? SimulationOutputDispositionKind.Suppress : SimulationOutputDispositionKind.Publish));
                m_State.Record(
                    eventHeader,
                    duplicate
                        ? ServerAuthoritativeEventDisposition.SuppressedDuplicate
                        : ServerAuthoritativeEventDisposition.PredictedCommitted);
            }
        }

        public SimulationPipelinePassStateSnapshot CaptureState()
        {
            RequireCaptureOrRestore();
            return ServerAuthoritativePredictionStateSnapshot.Create(
                Descriptor.PassId,
                StateIdentity.StateOwner,
                StateIdentity.StateSchemaId,
                m_State.CaptureJournalState(),
                ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion);
        }

        public ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot)
        {
            RequireCaptureOrRestore();
            return new ServerAuthoritativePassStateRestoreTransaction(
                StateIdentity,
                snapshot,
                m_State.CaptureJournalState,
                m_State.RestoreJournalState);
        }
    }

    public sealed class PredictionOutputDispositionReads : ISimulationPipelineReadPortSet
    {
        public PredictionOutputDispositionReads(
            IReadOnlySimulationPipelineProductPort<PredictionCorrectionDecision> decision,
            IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> results,
            IServerAuthoritativePredictionStatePort predictionState,
            IFloat32CompletedStepReadPort completed,
            IFloat32DiagnosticsRuntimePort diagnostics)
        {
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
            Results = results ?? throw new ArgumentNullException(nameof(results));
            PredictionState = predictionState ?? throw new ArgumentNullException(nameof(predictionState));
            Completed = completed ?? throw new ArgumentNullException(nameof(completed));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public IReadOnlySimulationPipelineProductPort<PredictionCorrectionDecision> Decision { get; }
        public IReadOnlySimulationPipelineAppendPort<Float32FinalizedActorResult> Results { get; }
        public IServerAuthoritativePredictionStatePort PredictionState { get; }
        public IFloat32CompletedStepReadPort Completed { get; }
        public IFloat32DiagnosticsRuntimePort Diagnostics { get; }
    }

    public sealed class PredictionOutputDispositionWrites : ISimulationPipelineWritePortSet
    {
        public PredictionOutputDispositionWrites(
            IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> dispositions)
        {
            Dispositions = dispositions ?? throw new ArgumentNullException(nameof(dispositions));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationPipelineOutputDispositionSet> Dispositions { get; }
    }

    public sealed class PredictionInputCommandEgressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public PredictionInputCommandEgressPassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.InputCommandEgress(policy));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new PredictionInputCommandEgressReads(
                context.Products.BindExclusiveReader<OwnerCanonicalInputBatch>(ServerAuthoritativeProducts.OwnerCanonicalInputBatch),
                context.BindTargetPort<IFloat32CompletedStepReadPort>(Float32PipelineRuntimePortIds.CompletedSteps),
                context.BindSourcePort<IServerAuthoritativeNetworkSendPort>(ServerAuthoritativeSourcePortContracts.PredictionSendPortId));
            var writes = new ServerAuthoritativeSourceEgressWrites(
                context.Products.BindAppendWriter<Float32SourceEgressRecord>(SimulationPipelineProducts.SourceEgress));
            return new Float32EgressPassRuntimeAdapter<PredictionInputCommandEgressReads, ServerAuthoritativeSourceEgressWrites>(
                new PredictionInputCommandEgressPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class PredictionInputCommandEgressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<PredictionInputCommandEgressReads, ServerAuthoritativeSourceEgressWrites>
    {
        public PredictionInputCommandEgressPassRuntime(SimulationPipelinePassDescriptor descriptor) : base(descriptor) { }

        public void Execute(
            SimulationPipelineEgressContext context,
            PredictionInputCommandEgressReads readPorts,
            ServerAuthoritativeSourceEgressWrites writePorts)
        {
            RequireExecution();
            _ = readPorts.Input.Read();
            for (int i = 0; i < readPorts.Completed.Steps.Count; i++)
            {
                Float32CompletedSimulationStep step = readPorts.Completed.Steps[i];
                if (step.Step.ExecutionKind != SimulationPipelineStepExecutionKind.Current)
                    continue;
                OwnerCanonicalInputBatch input = PredictionCompletedStepInput.Read(step);
                writePorts.Egress.Append(
                    new SimulationPipelineAppendEntryIdentity(
                        input.ActorId,
                        new SimulationTick(input.SourceTick),
                        input.InputSequence,
                        context.Source),
                    new Float32SourceEgressRecord(
                        input.ActorId,
                        new SimulationTick(input.SourceTick),
                        ServerAuthoritativeEgressChannels.ClientInput,
                        ServerAuthoritativeEgressChannels.ClientInputSchema,
                        ServerAuthoritativeEgressChannels.SchemaVersion,
                        ServerAuthoritativeEgressCodec.WriteOwnerInput(input)));
            }
        }
    }

    public sealed class PredictionInputCommandEgressReads : ISimulationPipelineReadPortSet
    {
        public PredictionInputCommandEgressReads(
            IReadOnlySimulationPipelineProductPort<OwnerCanonicalInputBatch> input,
            IFloat32CompletedStepReadPort completed,
            IServerAuthoritativeNetworkSendPort send)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Completed = completed ?? throw new ArgumentNullException(nameof(completed));
            Send = send ?? throw new ArgumentNullException(nameof(send));
        }

        public IReadOnlySimulationPipelineProductPort<OwnerCanonicalInputBatch> Input { get; }
        public IFloat32CompletedStepReadPort Completed { get; }
        public IServerAuthoritativeNetworkSendPort Send { get; }
    }

    public sealed class RemotePresentationEgressPassRuntimeFactory : IFloat32PipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;

        public RemotePresentationEgressPassRuntimeFactory(ServerAuthoritativeModelPolicy policy)
        {
            m_Descriptor = ServerAuthoritativePipelinePassContracts.CreateFactoryDescriptor(
                ServerAuthoritativePipelinePassContracts.RemotePresentationEgress(policy));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFloat32CompiledPipelinePassRuntime Create(Float32PipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RemotePresentationEgressReads(
                context.Products.BindExclusiveReader<RemotePresentationBatch>(ServerAuthoritativeProducts.RemotePresentationBatch),
                context.Products.BindExclusiveReader<SelectedRemoteBodyBatch>(ServerAuthoritativeProducts.SelectedRemoteBodyBatch));
            var writes = new ServerAuthoritativeSourceEgressWrites(
                context.Products.BindAppendWriter<Float32SourceEgressRecord>(SimulationPipelineProducts.SourceEgress));
            return new Float32EgressPassRuntimeAdapter<RemotePresentationEgressReads, ServerAuthoritativeSourceEgressWrites>(
                new RemotePresentationEgressPassRuntime(context.Pass.Descriptor),
                reads,
                writes);
        }
    }

    public sealed class RemotePresentationEgressPassRuntime :
        Float32PipelinePassRuntimeBase,
        ISimulationEgressPassRuntime<RemotePresentationEgressReads, ServerAuthoritativeSourceEgressWrites>
    {
        public RemotePresentationEgressPassRuntime(SimulationPipelinePassDescriptor descriptor) : base(descriptor) { }

        public void Execute(
            SimulationPipelineEgressContext context,
            RemotePresentationEgressReads readPorts,
            ServerAuthoritativeSourceEgressWrites writePorts)
        {
            RequireExecution();
            RemotePresentationBatch events = readPorts.Batch.Read();
            SelectedRemoteBodyBatch selected = readPorts.SelectedBodies.Read();
            if (events.ActorId != selected.ActorId)
                throw new InvalidOperationException("Remote Presentation event and selected body products target different Actors.");
            var batch = new RemotePresentationBatch(
                events.ActorId,
                selected.BodySamples,
                events.SampleCommands,
                events.ReliableEvents,
                selected.ResetStream);
            if (!batch.ResetBodyStream && batch.BodySamples.Count == 0 && batch.SampleCommands.Count == 0 && batch.ReliableEvents.Count == 0)
                return;
            SimulationTick tick = LatestTick(batch);
            if (!tick.IsValid)
                tick = selected.Tick;
            writePorts.Egress.Append(
                new SimulationPipelineAppendEntryIdentity(batch.ActorId, tick, 1, context.Source),
                new Float32SourceEgressRecord(
                    batch.ActorId,
                    tick,
                    ServerAuthoritativeEgressChannels.RemotePresentation,
                    ServerAuthoritativeEgressChannels.RemotePresentationSchema,
                    ServerAuthoritativeEgressChannels.RemotePresentationSchemaVersion,
                    ServerAuthoritativeEgressCodec.WriteRemotePresentation(batch)));
        }

        static SimulationTick LatestTick(RemotePresentationBatch batch)
        {
            SimulationTick tick = default;
            if (batch.BodySamples.Count > 0)
                tick = batch.BodySamples[batch.BodySamples.Count - 1].Tick;
            for (int i = 0; i < batch.SampleCommands.Count; i++)
            {
                if (!tick.IsValid || batch.SampleCommands[i].Header.Tick.CompareTo(tick) > 0)
                    tick = batch.SampleCommands[i].Header.Tick;
            }
            if (batch.ReliableEvents.Count > 0 &&
                (!tick.IsValid || batch.ReliableEvents[batch.ReliableEvents.Count - 1].Header.Tick.CompareTo(tick) > 0))
            {
                tick = batch.ReliableEvents[batch.ReliableEvents.Count - 1].Header.Tick;
            }
            return tick;
        }
    }

    public sealed class RemotePresentationEgressReads : ISimulationPipelineReadPortSet
    {
        public RemotePresentationEgressReads(
            IReadOnlySimulationPipelineProductPort<RemotePresentationBatch> batch,
            IReadOnlySimulationPipelineProductPort<SelectedRemoteBodyBatch> selectedBodies)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            SelectedBodies = selectedBodies ?? throw new ArgumentNullException(nameof(selectedBodies));
        }

        public IReadOnlySimulationPipelineProductPort<RemotePresentationBatch> Batch { get; }
        public IReadOnlySimulationPipelineProductPort<SelectedRemoteBodyBatch> SelectedBodies { get; }
    }

    public sealed class ServerAuthoritativeSourceEgressWrites : ISimulationPipelineWritePortSet
    {
        public ServerAuthoritativeSourceEgressWrites(
            IAppendOnlySimulationPipelineProductWriter<Float32SourceEgressRecord> egress)
        {
            Egress = egress ?? throw new ArgumentNullException(nameof(egress));
        }

        public IAppendOnlySimulationPipelineProductWriter<Float32SourceEgressRecord> Egress { get; }
    }

    public sealed class EmptyServerAuthoritativeWritePorts : ISimulationPipelineWritePortSet
    {
        public static readonly EmptyServerAuthoritativeWritePorts Instance = new EmptyServerAuthoritativeWritePorts();
        EmptyServerAuthoritativeWritePorts() { }
    }
}
