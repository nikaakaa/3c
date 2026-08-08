using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackEndpointInputSourcePort : IRollbackInputSourcePort
    {
        sealed class Checkpoint : IRollbackInputSourceCheckpoint
        {
            public RollbackEndpointInputSourcePort Owner;
            public byte[] ControlSourceState;
            public Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> Explicit;
            public Dictionary<ActorId, RollbackActorInputFrame> LastConfirmed;
            public SortedDictionary<ulong, RollbackCanonicalInputBundle> CanonicalPending;
            public ActorId LocalActorId;
            public RollbackActorInputFrame PendingLocalFrame;
            public RollbackCanonicalInputBundle PendingPredicted;
            public ulong PendingSimulationTick;
            public ulong LocalExplicitFrontier;
            public ulong NextInputSequence;
            public ulong NextPredictedBundleSequence;
            public ulong LastOuterSourceTick;
            public int ExplicitCount;
        }

        sealed class RemoteInputDiagnostics
        {
            public ulong ExactInputHitCount;
            public ulong PredictedFallbackCount;
            public long LastArrivalDeltaTicks;
        }

        readonly RollbackPeerEndpoint m_Peer;
        readonly IFixedCharacterControlSourceRuntime m_InputAdapter;
        readonly int m_TickRate;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly string m_ClockId;
        readonly string m_PredictionSourceIdentity;
        readonly SortedDictionary<ulong, RollbackCanonicalInputBundle> m_CanonicalPending =
            new SortedDictionary<ulong, RollbackCanonicalInputBundle>();
        readonly Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> m_Explicit =
            new Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>>();
        readonly Dictionary<ActorId, RollbackActorInputFrame> m_LastConfirmed =
            new Dictionary<ActorId, RollbackActorInputFrame>();
        readonly Dictionary<ActorId, RemoteInputDiagnostics> m_RemoteDiagnostics =
            new Dictionary<ActorId, RemoteInputDiagnostics>();
        ActorId m_LocalActorId;
        RollbackActorInputFrame m_PendingLocalFrame;
        RollbackCanonicalInputBundle m_PendingPredicted;
        ulong m_PendingSimulationTick;
        ulong m_LocalExplicitFrontier;
        ulong m_NextInputSequence = 1;
        ulong m_NextPredictedBundleSequence = 1;
        ulong m_LastOuterSourceTick;
        int m_ExplicitCount;
        ulong m_RelayedArrivalCount;
        ulong m_RelayedArrivalLeadCount;
        ulong m_RelayedArrivalLateCount;
        long m_LastRelayedArrivalDeltaTicks;
        RollbackEndpointRuntimeBridge m_RuntimeBridge;

        public RollbackEndpointInputSourcePort(
            SimulationComponentIdentity sessionSource,
            RollbackPeerEndpoint peer,
            IFixedCharacterControlSourceRuntime inputAdapter,
            int tickRate,
            string clockId,
            DeterministicRollbackModelPolicy policy)
        {
            if (!sessionSource.IsValid || sessionSource.Role != SimulationComponentRole.SessionSource)
                throw new ArgumentException("Rollback Session Source identity is invalid.", nameof(sessionSource));
            m_Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            m_InputAdapter = inputAdapter ?? throw new ArgumentNullException(nameof(inputAdapter));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_ClockId = RollbackEndpointIdentity.Require(clockId, nameof(clockId));
            m_PredictionSourceIdentity = $"{sessionSource.ComponentId}/{inputAdapter.SourceIdentity}";
            Descriptor = new SimulationPortDescriptor(
                RollbackSourcePortContracts.InputPortId,
                RollbackSourcePortContracts.InputSchemaId,
                3,
                SimulationPortDirection.Input,
                sessionSource.ComponentId,
                StableHash.Compute(
                    "deterministic-rollback-endpoint-input-port/3",
                    sessionSource.ToString(),
                    inputAdapter.SourceIdentity,
                    tickRate.ToString(),
                    m_ClockId,
                    policy.ConfigurationHash.Value));
        }

        public SimulationPortDescriptor Descriptor { get; }

        public void BindRuntimeBridge(RollbackEndpointRuntimeBridge runtimeBridge)
        {
            if (runtimeBridge == null)
                throw new ArgumentNullException(nameof(runtimeBridge));
            if (m_RuntimeBridge != null)
                throw new InvalidOperationException("Rollback input Source Runtime bridge is already bound.");
            m_RuntimeBridge = runtimeBridge;
        }

        public RollbackIngressBatch Read(
            SimulationTickSourceIdentity outerSource,
            SimulationTick nextSimulationTick,
            IReadOnlyList<SimulationActorBinding> roster)
        {
            if (outerSource.Kind != SimulationTickSourceKind.LocalLogic || outerSource.SourceTick <= m_LastOuterSourceTick)
                throw new InvalidOperationException("Rollback input Source requires a new LocalLogic outer Tick.");
            if (!nextSimulationTick.IsValid || roster == null || !m_Peer.IsReady)
                throw new InvalidOperationException("Rollback input Source is not ready for a formal simulation Tick.");
            if (m_RuntimeBridge == null)
                throw new InvalidOperationException("Rollback input Source has no bound Runtime bridge.");
            RequireRoster(roster);
            var explicitArrivals = new List<RollbackActorInputFrame>();
            m_RuntimeBridge.Pump();
            DrainRelayedExplicit(explicitArrivals, nextSimulationTick.Value);
            DrainCanonical(roster);
            ReleaseConfirmedHistory();
            var source = new SimulationTickSourceIdentity(
                SimulationTickSourceKind.LocalLogic,
                m_ClockId,
                nextSimulationTick.Value);
            if (m_PendingSimulationTick != nextSimulationTick.Value)
            {
                if (m_PendingSimulationTick != 0 && nextSimulationTick.Value != checked(m_PendingSimulationTick + 1))
                    throw new InvalidOperationException("Rollback input Source observed a non-contiguous Simulation Tick.");
                BuildLocalFrame(nextSimulationTick, source);
            }
            m_Peer.SendInput(m_PendingLocalFrame);
            m_RuntimeBridge.Pump();
            DrainRelayedExplicit(explicitArrivals, nextSimulationTick.Value);
            DrainCanonical(roster);
            ReleaseConfirmedHistory();
            m_PendingPredicted = BuildPredictedBundle(nextSimulationTick, source, roster);
            var canonicalArrivals = new RollbackCanonicalInputBundle[m_CanonicalPending.Count];
            int arrivalIndex = 0;
            foreach (RollbackCanonicalInputBundle bundle in m_CanonicalPending.Values)
                canonicalArrivals[arrivalIndex++] = bundle;
            m_CanonicalPending.Clear();
            m_LastOuterSourceTick = outerSource.SourceTick;
            return new RollbackIngressBatch(
                m_PendingPredicted,
                explicitArrivals,
                canonicalArrivals,
                m_Peer.ConfirmedCanonicalTick == 0
                    ? default
                    : new SimulationTick(m_Peer.ConfirmedCanonicalTick),
                new FixedTypedIngressBatch(Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>()));
        }

        public IRollbackInputSourceCheckpoint CaptureCheckpoint()
        {
            return new Checkpoint
            {
                Owner = this,
                ControlSourceState = m_InputAdapter.CaptureState(),
                Explicit = CloneExplicit(m_Explicit),
                LastConfirmed = new Dictionary<ActorId, RollbackActorInputFrame>(m_LastConfirmed),
                CanonicalPending = new SortedDictionary<ulong, RollbackCanonicalInputBundle>(m_CanonicalPending),
                LocalActorId = m_LocalActorId,
                PendingLocalFrame = m_PendingLocalFrame,
                PendingPredicted = m_PendingPredicted,
                PendingSimulationTick = m_PendingSimulationTick,
                LocalExplicitFrontier = m_LocalExplicitFrontier,
                NextInputSequence = m_NextInputSequence,
                NextPredictedBundleSequence = m_NextPredictedBundleSequence,
                LastOuterSourceTick = m_LastOuterSourceTick,
                ExplicitCount = m_ExplicitCount
            };
        }

        public void RestoreCheckpoint(IRollbackInputSourceCheckpoint checkpoint)
        {
            if (checkpoint is not Checkpoint value || !ReferenceEquals(value.Owner, this))
                throw new ArgumentException("Rollback input Source checkpoint belongs to another Source.", nameof(checkpoint));
            m_InputAdapter.RestoreState(value.ControlSourceState);
            m_InputAdapter.NotifyStateDisposition(FixedCharacterControlSourceStateDisposition.Discarded);
            RestoreExplicit(value.Explicit);
            m_LastConfirmed.Clear();
            foreach (KeyValuePair<ActorId, RollbackActorInputFrame> pair in value.LastConfirmed)
                m_LastConfirmed.Add(pair.Key, pair.Value);
            m_CanonicalPending.Clear();
            foreach (KeyValuePair<ulong, RollbackCanonicalInputBundle> pair in value.CanonicalPending)
                m_CanonicalPending.Add(pair.Key, pair.Value);
            m_LocalActorId = value.LocalActorId;
            m_PendingLocalFrame = value.PendingLocalFrame;
            m_PendingPredicted = value.PendingPredicted;
            m_PendingSimulationTick = value.PendingSimulationTick;
            m_LocalExplicitFrontier = value.LocalExplicitFrontier;
            m_NextInputSequence = value.NextInputSequence;
            m_NextPredictedBundleSequence = value.NextPredictedBundleSequence;
            m_LastOuterSourceTick = value.LastOuterSourceTick;
            m_ExplicitCount = value.ExplicitCount;
        }

        public RollbackInputSourceDiagnosticsSnapshot CaptureDiagnostics()
        {
            var remote = new List<RollbackRemoteActorInputDiagnosticsSnapshot>();
            foreach (KeyValuePair<ActorId, RemoteInputDiagnostics> pair in m_RemoteDiagnostics)
            {
                if (pair.Key.Equals(m_LocalActorId))
                    continue;
                remote.Add(new RollbackRemoteActorInputDiagnosticsSnapshot(
                    pair.Key,
                    pair.Value.ExactInputHitCount,
                    pair.Value.PredictedFallbackCount,
                    pair.Value.LastArrivalDeltaTicks,
                    ExplicitFrontier(pair.Key)));
            }
            remote.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            FixedCharacterControlSourceDiagnosticsSnapshot local = m_InputAdapter.CaptureDiagnostics();
            return new RollbackInputSourceDiagnosticsSnapshot(
                new RollbackLocalInputDiagnosticsSnapshot(
                    local.PendingOffensiveRequestCount,
                    local.OldestCaptureTick,
                    local.OldestEligibleTick),
                remote.ToArray(),
                m_RelayedArrivalCount,
                m_RelayedArrivalLeadCount,
                m_RelayedArrivalLateCount,
                m_LastRelayedArrivalDeltaTicks,
                ExplicitFrontier(m_LocalActorId));
        }

        ulong ExplicitFrontier(ActorId actorId)
        {
            if (actorId.Equals(m_LocalActorId))
                return m_LocalExplicitFrontier;
            if (m_Explicit.TryGetValue(actorId, out SortedDictionary<ulong, RollbackActorInputFrame> history) &&
                history.Count > 0)
            {
                ulong frontier = 0;
                foreach (ulong tick in history.Keys)
                    frontier = tick;
                return frontier;
            }
            return m_LastConfirmed.TryGetValue(actorId, out RollbackActorInputFrame confirmed)
                ? confirmed.Tick.Value
                : 0;
        }

        void BuildLocalFrame(SimulationTick tick, SimulationTickSourceIdentity source)
        {
            ulong inputSequence = m_NextInputSequence;
            m_NextInputSequence = checked(inputSequence + 1);
            var context = new FixedCharacterInputBuildContext(
                m_LocalActorId,
                tick,
                source,
                inputSequence,
                m_TickRate,
                m_Policy.OffensiveRequestDelayTicks,
                m_Policy.MaximumQueuedBundles);
            CharacterSimulationInput localInput = m_InputAdapter.BuildInput(context) ??
                throw new InvalidOperationException("Rollback local input Adapter returned no input.");
            if (localInput.NumericProfile != FixedSimulationNumericProfile.Value ||
                !localInput.TickSource.Equals(source) || localInput.Sequence != inputSequence)
            {
                throw new InvalidOperationException("Rollback local input Adapter returned input outside the Fixed source contract.");
            }
            m_PendingLocalFrame = new RollbackActorInputFrame(
                m_LocalActorId,
                tick,
                inputSequence,
                localInput,
                RollbackInputProvenance.LocalExplicit);
            m_LocalExplicitFrontier = tick.Value;
            m_PendingSimulationTick = tick.Value;
        }

        RollbackCanonicalInputBundle BuildPredictedBundle(
            SimulationTick tick,
            SimulationTickSourceIdentity source,
            IReadOnlyList<SimulationActorBinding> roster)
        {
            var actors = new RollbackActorInputFrame[roster.Count];
            for (int i = 0; i < roster.Count; i++)
            {
                ActorId actorId = roster[i].ActorId;
                actors[i] = actorId.Equals(m_LocalActorId)
                    ? m_PendingLocalFrame
                    : BuildRemotePrediction(actorId, tick, source);
            }
            ulong sequence = m_NextPredictedBundleSequence;
            m_NextPredictedBundleSequence = checked(sequence + 1);
            return new RollbackCanonicalInputBundle(tick, sequence, actors);
        }

        RollbackActorInputFrame BuildRemotePrediction(
            ActorId actorId,
            SimulationTick tick,
            SimulationTickSourceIdentity source)
        {
            if (m_Explicit.TryGetValue(actorId, out SortedDictionary<ulong, RollbackActorInputFrame> history) &&
                history.TryGetValue(tick.Value, out RollbackActorInputFrame exact))
            {
                RequireRemoteDiagnostics(actorId).ExactInputHitCount++;
                return exact;
            }
            RequireRemoteDiagnostics(actorId).PredictedFallbackCount++;
            RollbackActorInputFrame previous = FindLatest(actorId, tick.Value);
            IReadOnlyList<SimulationInputValue> values = previous == null
                ? Array.Empty<SimulationInputValue>()
                : previous.Input.Values;
            RollbackInputProvenance provenance = RollbackInputProvenance.PredictedContinuous;
            if (m_Policy.MissingInputPolicy == RollbackMissingInputPolicy.NeutralValuesWithEmptyRequests)
            {
                values = BuildNeutralValues(values);
                provenance = RollbackInputProvenance.PredictedNeutral;
            }
            ulong sequence = previous?.InputSequence ?? tick.Value;
            var input = new CharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                source,
                m_PredictionSourceIdentity,
                sequence,
                values,
                Array.Empty<SimulationInputRequest>());
            return new RollbackActorInputFrame(actorId, tick, sequence, input, provenance);
        }

        void RequireRoster(IReadOnlyList<SimulationActorBinding> roster)
        {
            RollbackRoster endpointRoster = m_Peer.Roster;
            if (endpointRoster == null || roster.Count != endpointRoster.Entries.Count)
                throw new InvalidOperationException("Rollback Pipeline roster does not match the Endpoint roster.");
            ActorId local = default;
            for (int i = 0; i < roster.Count; i++)
            {
                ActorId actorId = roster[i].ActorId;
                if (!actorId.Equals(endpointRoster.Entries[i].ActorId))
                    throw new InvalidOperationException("Rollback Pipeline Actor order does not match the Endpoint roster.");
                if (!m_Explicit.ContainsKey(actorId))
                    m_Explicit.Add(actorId, new SortedDictionary<ulong, RollbackActorInputFrame>());
                if (!m_RemoteDiagnostics.ContainsKey(actorId))
                    m_RemoteDiagnostics.Add(actorId, new RemoteInputDiagnostics());
                if (string.Equals(endpointRoster.Entries[i].PeerId, m_Peer.LocalPeerId, StringComparison.Ordinal))
                    local = actorId;
            }
            if (!local.IsValid)
                throw new InvalidOperationException("Rollback Endpoint roster has no local Actor.");
            if (m_LocalActorId.IsValid && !m_LocalActorId.Equals(local))
                throw new InvalidOperationException("Rollback local Actor changed after Source creation.");
            m_LocalActorId = local;
        }

        void DrainRelayedExplicit(List<RollbackActorInputFrame> arrivals, ulong currentSimulationTick)
        {
            while (m_Peer.TryReceiveRelayedExplicit(out RollbackRelayedExplicitInputBatch batch))
            {
                for (int i = 0; i < batch.Frames.Count; i++)
                {
                    RollbackActorInputFrame frame = batch.Frames[i];
                    if (frame.ActorId.Equals(m_LocalActorId) || frame.Provenance != RollbackInputProvenance.RelayedExplicit)
                        throw new InvalidOperationException("Rollback relayed explicit input ownership or provenance is invalid.");
                    if (RecordExplicit(frame))
                    {
                        RecordRelayedArrival(frame, currentSimulationTick);
                        arrivals.Add(frame);
                    }
                }
            }
        }

        void RecordRelayedArrival(RollbackActorInputFrame frame, ulong currentSimulationTick)
        {
            long delta = checked((long)frame.Tick.Value - (long)currentSimulationTick);
            RemoteInputDiagnostics actor = RequireRemoteDiagnostics(frame.ActorId);
            actor.LastArrivalDeltaTicks = delta;
            m_LastRelayedArrivalDeltaTicks = delta;
            m_RelayedArrivalCount = checked(m_RelayedArrivalCount + 1);
            if (delta >= 0)
                m_RelayedArrivalLeadCount = checked(m_RelayedArrivalLeadCount + 1);
            else
                m_RelayedArrivalLateCount = checked(m_RelayedArrivalLateCount + 1);
        }

        RemoteInputDiagnostics RequireRemoteDiagnostics(ActorId actorId)
        {
            if (!m_RemoteDiagnostics.TryGetValue(actorId, out RemoteInputDiagnostics value))
                throw new InvalidOperationException($"Rollback diagnostics Actor '{actorId}' is absent from the locked roster.");
            return value;
        }

        void DrainCanonical(IReadOnlyList<SimulationActorBinding> roster)
        {
            while (m_Peer.TryReceiveCanonicalBundle(out RollbackCanonicalInputBundle bundle))
            {
                if (bundle.Actors.Count != roster.Count)
                    throw new InvalidOperationException("Rollback canonical bundle roster count is invalid.");
                for (int i = 0; i < roster.Count; i++)
                {
                    RollbackActorInputFrame frame = bundle.Actors[i];
                    if (!frame.ActorId.Equals(roster[i].ActorId) ||
                        frame.Provenance != RollbackInputProvenance.CanonicalExplicit)
                    {
                        throw new InvalidOperationException("Rollback canonical bundle Actor order or provenance is invalid.");
                    }
                    PromoteCanonical(frame);
                }
                if (m_CanonicalPending.TryGetValue(bundle.Tick.Value, out RollbackCanonicalInputBundle current))
                {
                    if (!current.BundleHash.Equals(bundle.BundleHash))
                        throw new InvalidOperationException($"Rollback canonical Tick '{bundle.Tick}' changed after arrival.");
                    continue;
                }
                m_CanonicalPending.Add(bundle.Tick.Value, bundle);
            }
        }

        bool RecordExplicit(RollbackActorInputFrame frame)
        {
            if (!m_Explicit.TryGetValue(frame.ActorId, out SortedDictionary<ulong, RollbackActorInputFrame> history))
                throw new InvalidOperationException($"Rollback relayed Actor '{frame.ActorId}' is absent from the locked roster.");
            if (history.TryGetValue(frame.Tick.Value, out RollbackActorInputFrame current))
            {
                if (current.InputSequence == frame.InputSequence && current.GameplayHash.Equals(frame.GameplayHash))
                    return false;
                if (current.InputSequence == frame.InputSequence)
                    throw new InvalidOperationException($"Rollback explicit input identity conflict at Actor '{frame.ActorId}' Tick '{frame.Tick}'.");
                throw new InvalidOperationException($"Rollback explicit input Tick '{frame.Tick}' has multiple sequences for Actor '{frame.ActorId}'.");
            }
            RequireExplicitCapacity();
            history.Add(frame.Tick.Value, frame);
            m_ExplicitCount++;
            return true;
        }

        void PromoteCanonical(RollbackActorInputFrame frame)
        {
            SortedDictionary<ulong, RollbackActorInputFrame> history = m_Explicit[frame.ActorId];
            if (history.TryGetValue(frame.Tick.Value, out RollbackActorInputFrame current))
            {
                if (current.InputSequence != frame.InputSequence || !current.GameplayHash.Equals(frame.GameplayHash))
                    throw new InvalidOperationException($"Rollback canonical input conflicts with explicit input at Actor '{frame.ActorId}' Tick '{frame.Tick}'.");
                history[frame.Tick.Value] = frame;
                return;
            }
            RequireExplicitCapacity();
            history.Add(frame.Tick.Value, frame);
            m_ExplicitCount++;
        }

        void ReleaseConfirmedHistory()
        {
            ulong confirmed = m_Peer.ConfirmedCanonicalTick;
            if (confirmed == 0)
                return;
            foreach (KeyValuePair<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> actor in m_Explicit)
            {
                var remove = new List<ulong>();
                RollbackActorInputFrame latest = null;
                foreach (KeyValuePair<ulong, RollbackActorInputFrame> pair in actor.Value)
                {
                    if (pair.Key > confirmed)
                        break;
                    latest = pair.Value;
                    remove.Add(pair.Key);
                }
                if (latest != null)
                {
                    m_LastConfirmed[actor.Key] = new RollbackActorInputFrame(
                        latest.ActorId,
                        latest.Tick,
                        latest.InputSequence,
                        latest.Input,
                        RollbackInputProvenance.ConfirmedExplicit);
                }
                for (int i = 0; i < remove.Count; i++)
                {
                    actor.Value.Remove(remove[i]);
                    m_ExplicitCount--;
                }
            }
        }

        RollbackActorInputFrame FindLatest(ActorId actorId, ulong beforeTick)
        {
            RollbackActorInputFrame result = m_LastConfirmed.TryGetValue(actorId, out RollbackActorInputFrame confirmed)
                ? confirmed
                : null;
            if (!m_Explicit.TryGetValue(actorId, out SortedDictionary<ulong, RollbackActorInputFrame> history))
                return result;
            foreach (KeyValuePair<ulong, RollbackActorInputFrame> pair in history)
            {
                if (pair.Key >= beforeTick)
                    break;
                result = pair.Value;
            }
            return result;
        }

        void RequireExplicitCapacity()
        {
            int capacity = checked((m_Policy.HistoryLengthTicks + m_Policy.MaximumQueuedBundles) * m_Peer.Roster.Entries.Count);
            if (m_ExplicitCount >= capacity)
                throw new InvalidOperationException("Rollback explicit input history capacity is exhausted before confirmation release.");
        }

        static Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> CloneExplicit(
            Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> source)
        {
            var result = new Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>>();
            foreach (KeyValuePair<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> pair in source)
                result.Add(pair.Key, new SortedDictionary<ulong, RollbackActorInputFrame>(pair.Value));
            return result;
        }

        void RestoreExplicit(Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> source)
        {
            m_Explicit.Clear();
            foreach (KeyValuePair<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> pair in source)
                m_Explicit.Add(pair.Key, new SortedDictionary<ulong, RollbackActorInputFrame>(pair.Value));
        }

        static SimulationInputValue[] BuildNeutralValues(IReadOnlyList<SimulationInputValue> source)
        {
            var result = new SimulationInputValue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                SimulationInputValue value = source[i];
                result[i] = value.Kind switch
                {
                    SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(value.InputId, false),
                    SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(value.InputId, FixedScalar.Zero),
                    SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(value.InputId, FixedVector2.Zero),
                    SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(value.InputId, FixedVector3.Zero),
                    SimulationInputValueKind.Yaw => SimulationInputValue.FromYaw(value.InputId, FixedYaw.Zero),
                    SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(value.InputId, SimulationActionTargetSnapshot.None),
                    _ => throw new InvalidOperationException($"Rollback input value kind '{value.Kind}' cannot be neutralized.")
                };
            }
            return result;
        }
    }
}
