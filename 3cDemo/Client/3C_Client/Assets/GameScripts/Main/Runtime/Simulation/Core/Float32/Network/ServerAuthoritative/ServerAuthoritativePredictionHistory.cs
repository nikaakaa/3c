using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePredictionHistory
    {
        readonly int m_Capacity;
        SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord> m_Records =
            new SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord>();

        readonly ServerAuthoritativeRemoteBodyTimeline m_RemoteBodies;

        public ServerAuthoritativePredictionHistory(
            int capacity,
            int tickRate,
            int maximumExtrapolationTicks,
            IEnumerable<ActorId> lockedRemoteActors)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Capacity = capacity;
            m_RemoteBodies = new ServerAuthoritativeRemoteBodyTimeline(
                checked(capacity * 4),
                tickRate,
                maximumExtrapolationTicks,
                lockedRemoteActors);
        }

        public int Count => m_Records.Count;
        public ulong FirstRetainedTick => m_Records.Count == 0 ? ulong.MaxValue : First().Key;
        public bool IsRemoteObservationPrimed => m_RemoteBodies.IsPrimed;
        public int RemoteBodySampleCount => m_RemoteBodies.SampleCount;
        public int RemoteBodyCapacityPerActor => m_RemoteBodies.CapacityPerActor;
        public ulong RemoteBodyFirstSampleTick => m_RemoteBodies.FirstSampleTick;
        public ulong RemoteBodyLastSampleTick => m_RemoteBodies.LastSampleTick;
        public ulong RemoteBodyEvictionCount => m_RemoteBodies.EvictionCount;

        public void Observe(RemotePresentationBatch batch) => m_RemoteBodies.Observe(batch);

        public ServerAuthoritativeRemoteBodySelectionFrame SelectRemoteBodyFrame(SimulationTick tick) =>
            m_RemoteBodies.Select(tick);

        public ulong GetLastPredictedInputSequence(ulong confirmedInputSequence)
        {
            ulong sequence = confirmedInputSequence;
            foreach (ServerAuthoritativePredictionHistoryRecord record in m_Records.Values)
                sequence = Math.Max(sequence, record.Input.InputSequence);
            return sequence;
        }

        public bool TryGet(SimulationTick tick, out ServerAuthoritativePredictionHistoryRecord record) =>
            m_Records.TryGetValue(tick.Value, out record);

        public ServerAuthoritativePredictionHistoryRecord FirstRecord() => First().Value;

        public ServerAuthoritativePredictionHistoryRecord LastRecord()
        {
            ServerAuthoritativePredictionHistoryRecord last = null;
            foreach (ServerAuthoritativePredictionHistoryRecord record in m_Records.Values)
                last = record;
            return last ?? throw new InvalidOperationException("Hard recovery has no local Pipeline frame to reconstruct model-owned state.");
        }

        public IReadOnlyList<ServerAuthoritativePredictionHistoryRecord> GetReplayAfter(ulong confirmedInputSequence)
        {
            var values = new List<ServerAuthoritativePredictionHistoryRecord>();
            foreach (ServerAuthoritativePredictionHistoryRecord record in m_Records.Values)
            {
                if (record.Input.InputSequence > confirmedInputSequence)
                    values.Add(record);
            }
            return values.AsReadOnly();
        }

        public void Add(
            OwnerCanonicalInputBatch input,
            Float32CompletedSimulationStep completed,
            ulong journalCursor,
            ulong confirmedInputSequence,
            ulong lastAuthorityAckTick,
            ulong lastBaselineTick)
        {
            if (input == null || completed == null || completed.StepSnapshot == null)
                throw new ArgumentException("Prediction history capture requires input and a completed Step snapshot.");
            if (completed.Step.Tick != completed.StepSnapshot.Tick || completed.State.Actors.Count != 1 ||
                completed.State.Actors[0].ActorId != input.ActorId)
            {
                throw new InvalidOperationException("Prediction history completed step does not match its owner input.");
            }
            if (m_Records.ContainsKey(completed.Step.Tick.Value))
                throw new InvalidOperationException($"Prediction history already contains Tick '{completed.Step.Tick}'.");
            var record = new ServerAuthoritativePredictionHistoryRecord(
                input,
                completed.StepSnapshot.CompositionIdentity,
                completed.StepSnapshot.World,
                completed.StepSnapshot.PipelineProjection,
                completed.Step.ObservedWorldConstraints,
                journalCursor);
            Restore(PrepareAdd(record, confirmedInputSequence, lastAuthorityAckTick, lastBaselineTick));
        }

        public void SealJournalCursor(SimulationTick tick, ulong journalCursor)
        {
            if (!m_Records.TryGetValue(tick.Value, out ServerAuthoritativePredictionHistoryRecord record))
                throw new InvalidOperationException($"Prediction history has no Current Tick '{tick}' to seal its journal cursor.");
            m_Records[tick.Value] = record.WithJournalCursor(journalCursor);
        }

        public ServerAuthoritativePredictionHistoryCheckpoint PreparePruneConfirmedThrough(ulong inputSequence)
        {
            var records = CopyRecords();
            var remove = new List<ulong>();
            foreach (KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> pair in records)
            {
                if (pair.Value.Input.InputSequence <= inputSequence)
                    remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                records.Remove(remove[i]);
            return new ServerAuthoritativePredictionHistoryCheckpoint(records, m_RemoteBodies.Capture());
        }

        public ServerAuthoritativePredictionHistoryCheckpoint PrepareClear() =>
            new ServerAuthoritativePredictionHistoryCheckpoint(
                Array.Empty<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>>(),
                m_RemoteBodies.Capture());

        public ServerAuthoritativePredictionHistoryCheckpoint Capture() =>
            new ServerAuthoritativePredictionHistoryCheckpoint(m_Records, m_RemoteBodies.Capture());

        public void Restore(ServerAuthoritativePredictionHistoryCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            var records = new SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord>();
            for (int i = 0; i < checkpoint.Records.Count; i++)
            {
                KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> pair = checkpoint.Records[i];
                records.Add(pair.Key, pair.Value);
            }
            m_Records = records;
            m_RemoteBodies.Restore(checkpoint.RemoteBodies);
        }

        ServerAuthoritativePredictionHistoryCheckpoint PrepareAdd(
            ServerAuthoritativePredictionHistoryRecord record,
            ulong confirmedInputSequence,
            ulong lastAuthorityAckTick,
            ulong lastBaselineTick)
        {
            var records = CopyRecords();
            while (records.Count >= m_Capacity)
            {
                KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> first = First(records);
                if (first.Value.Input.InputSequence > confirmedInputSequence)
                {
                    throw new InvalidOperationException(
                        $"Prediction history capacity cannot discard unconfirmed input: firstTick={first.Key};firstSequence={first.Value.Input.InputSequence};confirmedSequence={confirmedInputSequence};lastAckTick={lastAuthorityAckTick};lastBaselineTick={lastBaselineTick}.");
                }
                records.Remove(first.Key);
            }
            records.Add(record.Tick.Value, record);
            return new ServerAuthoritativePredictionHistoryCheckpoint(records, m_RemoteBodies.Capture());
        }

        SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord> CopyRecords() =>
            new SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord>(m_Records);

        KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> First() => First(m_Records);

        static KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> First(
            SortedDictionary<ulong, ServerAuthoritativePredictionHistoryRecord> records)
        {
            foreach (KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> pair in records)
                return pair;
            throw new InvalidOperationException("Prediction history is empty.");
        }
    }

    internal sealed class ServerAuthoritativePredictionHistoryCheckpoint
    {
        public ServerAuthoritativePredictionHistoryCheckpoint(
            IEnumerable<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>> records,
            ServerAuthoritativeRemoteBodyTimelineCheckpoint remoteBodies)
        {
            Records = new List<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>>(records).AsReadOnly();
            RemoteBodies = remoteBodies ?? throw new ArgumentNullException(nameof(remoteBodies));
        }

        public IReadOnlyList<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>> Records { get; }
        public ServerAuthoritativeRemoteBodyTimelineCheckpoint RemoteBodies { get; }
        public ulong FirstRetainedTick => Records.Count == 0 ? ulong.MaxValue : Records[0].Key;
    }

    internal readonly struct ServerAuthoritativeRemoteBodySelection
    {
        public ServerAuthoritativeRemoteBodySelection(
            ActorId actorId,
            SimulationTick targetTick,
            WorldBodyState beforeBody,
            WorldBodyState finalBody,
            SimulationTick sourcePreviousTick,
            SimulationTick sourceCurrentTick,
            ObservedWorldConstraintSamplingKind samplingKind)
        {
            if (!actorId.IsValid || !targetTick.IsValid ||
                beforeBody.ActorId != actorId || finalBody.ActorId != actorId ||
                !sourcePreviousTick.IsValid || !sourceCurrentTick.IsValid ||
                sourceCurrentTick.CompareTo(sourcePreviousTick) < 0 ||
                !Enum.IsDefined(typeof(ObservedWorldConstraintSamplingKind), samplingKind))
            {
                throw new ArgumentException("Remote body selection identity is incomplete or inconsistent.");
            }
            ActorId = actorId;
            TargetTick = targetTick;
            BeforeBody = beforeBody;
            FinalBody = finalBody;
            SourcePreviousTick = sourcePreviousTick;
            SourceCurrentTick = sourceCurrentTick;
            SamplingKind = samplingKind;
        }

        public ActorId ActorId { get; }
        public SimulationTick TargetTick { get; }
        public WorldBodyState BeforeBody { get; }
        public WorldBodyState FinalBody { get; }
        public SimulationTick SourcePreviousTick { get; }
        public SimulationTick SourceCurrentTick { get; }
        public ObservedWorldConstraintSamplingKind SamplingKind { get; }

        public ObservedWorldConstraint ToObservedConstraint(StableHash contactShapeConfigurationHash) =>
            new ObservedWorldConstraint(
                ActorId,
                TargetTick,
                BeforeBody,
                FinalBody,
                SourcePreviousTick,
                SourceCurrentTick,
                SamplingKind,
                contactShapeConfigurationHash);

        public CharacterBodySample ToBodySample() =>
            new CharacterBodySample(
                ActorId,
                TargetTick,
                BeforeBody,
                FinalBody,
                FinalBody.Position - BeforeBody.Position,
                Float32Angle.Delta(BeforeBody.Yaw, FinalBody.Yaw));
    }

    internal sealed class ServerAuthoritativeRemoteBodySelectionFrame
    {
        readonly IReadOnlyList<ServerAuthoritativeRemoteBodySelection> m_Selections;

        public ServerAuthoritativeRemoteBodySelectionFrame(
            SimulationTick tick,
            IEnumerable<ServerAuthoritativeRemoteBodySelection> selections)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Remote body selection frame Tick is invalid.", nameof(tick));
            var values = selections == null
                ? throw new ArgumentNullException(nameof(selections))
                : new List<ServerAuthoritativeRemoteBodySelection>(selections);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].TargetTick != tick || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Remote body selection frame contains a duplicate Actor or mismatched Tick.", nameof(selections));
            }
            Tick = tick;
            m_Selections = values.AsReadOnly();
        }

        public SimulationTick Tick { get; }
        public IReadOnlyList<ServerAuthoritativeRemoteBodySelection> Selections => m_Selections;

        public ObservedWorldConstraintFrame ToObservedWorldConstraints(StableHash contactShapeConfigurationHash)
        {
            var constraints = new ObservedWorldConstraint[m_Selections.Count];
            for (int i = 0; i < constraints.Length; i++)
                constraints[i] = m_Selections[i].ToObservedConstraint(contactShapeConfigurationHash);
            return new ObservedWorldConstraintFrame(Tick, constraints);
        }

        public IReadOnlyList<CharacterBodySample> ToBodySamples()
        {
            var samples = new CharacterBodySample[m_Selections.Count];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = m_Selections[i].ToBodySample();
            return Array.AsReadOnly(samples);
        }
    }

    internal sealed class ServerAuthoritativeRemoteBodyTimeline
    {
        readonly int m_CapacityPerActor;
        readonly int m_TickRate;
        readonly int m_MaximumExtrapolationTicks;
        readonly ActorId[] m_LockedActors;
        readonly SortedDictionary<ActorId, SortedDictionary<ulong, CharacterBodySample>> m_Samples =
            new SortedDictionary<ActorId, SortedDictionary<ulong, CharacterBodySample>>();
        ulong m_EvictionCount;

        public ServerAuthoritativeRemoteBodyTimeline(
            int capacityPerActor,
            int tickRate,
            int maximumExtrapolationTicks,
            IEnumerable<ActorId> lockedActors)
        {
            if (capacityPerActor <= 0 || tickRate <= 0 || maximumExtrapolationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacityPerActor));
            var actors = lockedActors == null ? new List<ActorId>() : new List<ActorId>(lockedActors);
            actors.Sort((left, right) => left.CompareTo(right));
            if (actors.Count == 0)
                throw new ArgumentException("Remote body timeline requires a locked remote Actor roster.", nameof(lockedActors));
            for (int i = 0; i < actors.Count; i++)
            {
                if (!actors[i].IsValid || i > 0 && actors[i - 1] == actors[i])
                    throw new ArgumentException("Remote body timeline Actor roster is invalid.", nameof(lockedActors));
                m_Samples.Add(actors[i], new SortedDictionary<ulong, CharacterBodySample>());
            }
            m_CapacityPerActor = capacityPerActor;
            m_TickRate = tickRate;
            m_MaximumExtrapolationTicks = maximumExtrapolationTicks;
            m_LockedActors = actors.ToArray();
        }

        public bool IsPrimed
        {
            get
            {
                for (int i = 0; i < m_LockedActors.Length; i++)
                {
                    if (m_Samples[m_LockedActors[i]].Count == 0)
                        return false;
                }
                return true;
            }
        }

        public int SampleCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_LockedActors.Length; i++)
                    count = checked(count + m_Samples[m_LockedActors[i]].Count);
                return count;
            }
        }

        public int CapacityPerActor => m_CapacityPerActor;

        public ulong FirstSampleTick
        {
            get
            {
                ulong tick = ulong.MaxValue;
                for (int i = 0; i < m_LockedActors.Length; i++)
                {
                    SortedDictionary<ulong, CharacterBodySample> samples = m_Samples[m_LockedActors[i]];
                    if (samples.Count > 0)
                        tick = Math.Min(tick, First(samples).Key);
                }
                return tick == ulong.MaxValue ? 0 : tick;
            }
        }

        public ulong LastSampleTick
        {
            get
            {
                ulong tick = 0;
                for (int i = 0; i < m_LockedActors.Length; i++)
                {
                    SortedDictionary<ulong, CharacterBodySample> samples = m_Samples[m_LockedActors[i]];
                    if (samples.Count > 0)
                        tick = Math.Max(tick, Last(samples).Key);
                }
                return tick;
            }
        }

        public ulong EvictionCount => m_EvictionCount;

        public void Observe(RemotePresentationBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            if (!m_Samples.TryGetValue(batch.ActorId, out SortedDictionary<ulong, CharacterBodySample> samples))
                throw new InvalidOperationException($"Remote body sample targets unlocked Actor '{batch.ActorId}'.");
            for (int i = 0; i < batch.BodySamples.Count; i++)
                Add(samples, batch.BodySamples[i]);
            while (samples.Count > m_CapacityPerActor)
            {
                samples.Remove(First(samples).Key);
                m_EvictionCount = checked(m_EvictionCount + 1);
            }
        }

        public ServerAuthoritativeRemoteBodySelectionFrame Select(SimulationTick targetTick)
        {
            if (!IsPrimed)
                throw new InvalidOperationException("Remote body timeline has not completed observation priming.");
            var selections = new ServerAuthoritativeRemoteBodySelection[m_LockedActors.Length];
            for (int i = 0; i < m_LockedActors.Length; i++)
            {
                ActorId actorId = m_LockedActors[i];
                selections[i] = SelectActor(
                    actorId,
                    m_Samples[actorId],
                    targetTick);
            }
            return new ServerAuthoritativeRemoteBodySelectionFrame(targetTick, selections);
        }

        public ServerAuthoritativeRemoteBodyTimelineCheckpoint Capture()
        {
            var actors = new List<ServerAuthoritativeRemoteBodyActorCheckpoint>(m_LockedActors.Length);
            for (int i = 0; i < m_LockedActors.Length; i++)
            {
                ActorId actorId = m_LockedActors[i];
                actors.Add(new ServerAuthoritativeRemoteBodyActorCheckpoint(actorId, m_Samples[actorId].Values));
            }
            return new ServerAuthoritativeRemoteBodyTimelineCheckpoint(actors);
        }

        public void Restore(ServerAuthoritativeRemoteBodyTimelineCheckpoint checkpoint)
        {
            if (checkpoint == null || checkpoint.Actors.Count != m_LockedActors.Length)
                throw new InvalidOperationException("Remote body timeline checkpoint roster does not match the locked roster.");
            for (int i = 0; i < m_LockedActors.Length; i++)
            {
                ServerAuthoritativeRemoteBodyActorCheckpoint actor = checkpoint.Actors[i];
                if (actor.ActorId != m_LockedActors[i])
                    throw new InvalidOperationException("Remote body timeline checkpoint Actor order does not match the locked roster.");
                var samples = new SortedDictionary<ulong, CharacterBodySample>();
                for (int sampleIndex = 0; sampleIndex < actor.Samples.Count; sampleIndex++)
                    Add(samples, actor.Samples[sampleIndex]);
                if (samples.Count > m_CapacityPerActor)
                    throw new InvalidOperationException("Remote body timeline checkpoint exceeds its configured capacity.");
                m_Samples[actor.ActorId] = samples;
            }
        }

        ServerAuthoritativeRemoteBodySelection SelectActor(
            ActorId actorId,
            SortedDictionary<ulong, CharacterBodySample> samples,
            SimulationTick targetTick)
        {
            if (samples.TryGetValue(targetTick.Value, out CharacterBodySample exact))
            {
                return new ServerAuthoritativeRemoteBodySelection(
                    actorId,
                    targetTick,
                    exact.BeforeBody,
                    exact.FinalBody,
                    exact.Tick,
                    exact.Tick,
                    ObservedWorldConstraintSamplingKind.Exact);
            }
            ulong beforeTick = targetTick.Value > 1 ? targetTick.Value - 1 : targetTick.Value;
            BodySelection before = ResolveBodyAt(actorId, samples, beforeTick, true);
            BodySelection final = ResolveBodyAt(actorId, samples, targetTick.Value, false);
            ObservedWorldConstraintSamplingKind kind = before.Kind.CompareTo(final.Kind) >= 0
                ? before.Kind
                : final.Kind;
            return new ServerAuthoritativeRemoteBodySelection(
                actorId,
                targetTick,
                before.Body,
                final.Body,
                before.SourcePreviousTick.CompareTo(final.SourcePreviousTick) <= 0
                    ? before.SourcePreviousTick
                    : final.SourcePreviousTick,
                before.SourceCurrentTick.CompareTo(final.SourceCurrentTick) >= 0
                    ? before.SourceCurrentTick
                    : final.SourceCurrentTick,
                kind);
        }

        BodySelection ResolveBodyAt(
            ActorId actorId,
            SortedDictionary<ulong, CharacterBodySample> samples,
            ulong targetTick,
            bool allowFirstBefore)
        {
            if (samples.TryGetValue(targetTick, out CharacterBodySample exact))
                return new BodySelection(exact.FinalBody, exact.Tick, exact.Tick, ObservedWorldConstraintSamplingKind.Exact);
            if (targetTick < ulong.MaxValue && samples.TryGetValue(targetTick + 1, out CharacterBodySample nextExact))
                return new BodySelection(nextExact.BeforeBody, nextExact.Tick, nextExact.Tick, ObservedWorldConstraintSamplingKind.Exact);

            CharacterBodySample lower = default;
            CharacterBodySample upper = default;
            bool hasLower = false;
            bool hasUpper = false;
            foreach (CharacterBodySample sample in samples.Values)
            {
                if (sample.Tick.Value < targetTick)
                {
                    lower = sample;
                    hasLower = true;
                    continue;
                }
                if (sample.Tick.Value > targetTick)
                {
                    upper = sample;
                    hasUpper = true;
                    break;
                }
            }
            if (hasLower && hasUpper)
            {
                Float32Scalar amount = Float32Scalar.FromDouble(
                    (targetTick - lower.Tick.Value) /
                    (double)(upper.Tick.Value - lower.Tick.Value));
                return new BodySelection(
                    InterpolateBody(actorId, lower.FinalBody, upper.FinalBody, amount),
                    lower.Tick,
                    upper.Tick,
                    ObservedWorldConstraintSamplingKind.Interpolation);
            }
            if (!hasLower && allowFirstBefore)
            {
                KeyValuePair<ulong, CharacterBodySample> first = First(samples);
                if (first.Key == targetTick + 1)
                    return new BodySelection(first.Value.BeforeBody, first.Value.Tick, first.Value.Tick, ObservedWorldConstraintSamplingKind.Exact);
            }
            CharacterBodySample latest = Last(samples).Value;
            if (targetTick < latest.Tick.Value)
                throw new InvalidOperationException($"Remote Actor '{actorId}' has no observation interval for Tick '{targetTick}'.");
            ulong extrapolationTicks = targetTick - latest.Tick.Value;
            if (extrapolationTicks > (ulong)m_MaximumExtrapolationTicks)
            {
                throw new InvalidOperationException(
                    $"Remote Actor '{actorId}' observation extrapolation '{extrapolationTicks}' exceeds configured maximum '{m_MaximumExtrapolationTicks}'.");
            }
            Float32Scalar seconds = Float32Scalar.FromDouble(extrapolationTicks / (double)m_TickRate);
            WorldBodyState body = latest.FinalBody;
            return new BodySelection(
                new WorldBodyState(
                    actorId,
                    body.Position + body.Velocity * seconds,
                    body.Yaw,
                    body.Velocity,
                    body.VerticalVelocity,
                    body.Grounded,
                    body.Collision),
                latest.Tick,
                latest.Tick,
                ObservedWorldConstraintSamplingKind.ConstantVelocityExtrapolation);
        }

        static WorldBodyState InterpolateBody(
            ActorId actorId,
            WorldBodyState from,
            WorldBodyState to,
            Float32Scalar amount)
        {
            Float32Scalar yawDelta = Float32Angle.Delta(from.Yaw, to.Yaw);
            return new WorldBodyState(
                actorId,
                from.Position + (to.Position - from.Position) * amount,
                new Float32Yaw(from.Yaw.Degrees + yawDelta * amount),
                from.Velocity + (to.Velocity - from.Velocity) * amount,
                from.VerticalVelocity + (to.VerticalVelocity - from.VerticalVelocity) * amount,
                amount < Float32Scalar.FromDouble(0.5d) ? from.Grounded : to.Grounded,
                amount < Float32Scalar.FromDouble(0.5d) ? from.Collision : to.Collision);
        }

        static void Add(
            SortedDictionary<ulong, CharacterBodySample> samples,
            CharacterBodySample sample)
        {
            if (samples.TryGetValue(sample.Tick.Value, out CharacterBodySample existing))
            {
                if (!SampleEquals(existing, sample))
                    throw new InvalidOperationException($"Remote body timeline Tick '{sample.Tick}' changed canonical value.");
                return;
            }
            KeyValuePair<ulong, CharacterBodySample>? previous = null;
            KeyValuePair<ulong, CharacterBodySample>? next = null;
            foreach (KeyValuePair<ulong, CharacterBodySample> pair in samples)
            {
                if (pair.Key < sample.Tick.Value)
                    previous = pair;
                else
                {
                    next = pair;
                    break;
                }
            }
            if (previous.HasValue && previous.Value.Key + 1 == sample.Tick.Value &&
                !WorldSolveBatchRequest.BodyEquals(previous.Value.Value.FinalBody, sample.BeforeBody))
            {
                throw new InvalidOperationException("Remote body timeline contains a discontinuous consecutive BeforeBody.");
            }
            if (next.HasValue && sample.Tick.Value + 1 == next.Value.Key &&
                !WorldSolveBatchRequest.BodyEquals(sample.FinalBody, next.Value.Value.BeforeBody))
            {
                throw new InvalidOperationException("Remote body timeline contains a discontinuous consecutive FinalBody.");
            }
            samples.Add(sample.Tick.Value, sample);
        }

        static bool SampleEquals(CharacterBodySample left, CharacterBodySample right) =>
            left.ActorId == right.ActorId && left.Tick == right.Tick &&
            WorldSolveBatchRequest.BodyEquals(left.BeforeBody, right.BeforeBody) &&
            WorldSolveBatchRequest.BodyEquals(left.FinalBody, right.FinalBody) &&
            left.AppliedDisplacement == right.AppliedDisplacement &&
            left.AppliedYawDegrees == right.AppliedYawDegrees;

        static KeyValuePair<ulong, CharacterBodySample> First(
            SortedDictionary<ulong, CharacterBodySample> samples)
        {
            foreach (KeyValuePair<ulong, CharacterBodySample> pair in samples)
                return pair;
            throw new InvalidOperationException("Remote body timeline Actor track is empty.");
        }

        static KeyValuePair<ulong, CharacterBodySample> Last(
            SortedDictionary<ulong, CharacterBodySample> samples)
        {
            KeyValuePair<ulong, CharacterBodySample> last = default;
            bool found = false;
            foreach (KeyValuePair<ulong, CharacterBodySample> pair in samples)
            {
                last = pair;
                found = true;
            }
            return found ? last : throw new InvalidOperationException("Remote body timeline Actor track is empty.");
        }

        readonly struct BodySelection
        {
            public BodySelection(
                WorldBodyState body,
                SimulationTick sourcePreviousTick,
                SimulationTick sourceCurrentTick,
                ObservedWorldConstraintSamplingKind kind)
            {
                Body = body;
                SourcePreviousTick = sourcePreviousTick;
                SourceCurrentTick = sourceCurrentTick;
                Kind = kind;
            }

            public WorldBodyState Body { get; }
            public SimulationTick SourcePreviousTick { get; }
            public SimulationTick SourceCurrentTick { get; }
            public ObservedWorldConstraintSamplingKind Kind { get; }
        }
    }

    internal sealed class ServerAuthoritativeRemoteBodyTimelineCheckpoint
    {
        public ServerAuthoritativeRemoteBodyTimelineCheckpoint(
            IEnumerable<ServerAuthoritativeRemoteBodyActorCheckpoint> actors)
        {
            var values = actors == null
                ? throw new ArgumentNullException(nameof(actors))
                : new List<ServerAuthoritativeRemoteBodyActorCheckpoint>(actors);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Remote body checkpoint roster is invalid.", nameof(actors));
            }
            Actors = values.AsReadOnly();
        }

        public IReadOnlyList<ServerAuthoritativeRemoteBodyActorCheckpoint> Actors { get; }
    }

    internal sealed class ServerAuthoritativeRemoteBodyActorCheckpoint
    {
        public ServerAuthoritativeRemoteBodyActorCheckpoint(
            ActorId actorId,
            IEnumerable<CharacterBodySample> samples)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Remote body checkpoint ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            var values = samples == null ? new List<CharacterBodySample>() : new List<CharacterBodySample>(samples);
            values.Sort((left, right) => left.Tick.CompareTo(right.Tick));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].ActorId != actorId || i > 0 && values[i - 1].Tick == values[i].Tick)
                    throw new ArgumentException("Remote body checkpoint sample order is invalid.", nameof(samples));
            }
            Samples = values.AsReadOnly();
        }

        public ActorId ActorId { get; }
        public IReadOnlyList<CharacterBodySample> Samples { get; }
    }
}
