using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePredictionHistory
    {
        readonly int m_Capacity;
        readonly ulong[] m_Ticks;
        readonly ServerAuthoritativePredictionHistoryRecord[] m_Records;
        int m_Count;

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
            m_Ticks = new ulong[capacity];
            m_Records = new ServerAuthoritativePredictionHistoryRecord[capacity];
            m_RemoteBodies = new ServerAuthoritativeRemoteBodyTimeline(
                checked(capacity * 4),
                tickRate,
                maximumExtrapolationTicks,
                lockedRemoteActors);
        }

        public int Count => m_Count;
        public ulong FirstRetainedTick => m_Count == 0 ? ulong.MaxValue : m_Ticks[0];
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
            for (int i = 0; i < m_Count; i++)
                sequence = Math.Max(sequence, m_Records[i].Input.InputSequence);
            return sequence;
        }

        public bool TryGet(SimulationTick tick, out ServerAuthoritativePredictionHistoryRecord record)
        {
            int index = Find(tick.Value);
            if (index < 0)
            {
                record = null;
                return false;
            }
            record = m_Records[index];
            return true;
        }

        public ServerAuthoritativePredictionHistoryRecord FirstRecord() =>
            m_Count == 0
                ? throw new InvalidOperationException("Prediction history is empty.")
                : m_Records[0];

        public ServerAuthoritativePredictionHistoryRecord LastRecord()
        {
            if (m_Count == 0)
                throw new InvalidOperationException("Hard recovery has no local Pipeline frame to reconstruct model-owned state.");
            return m_Records[m_Count - 1];
        }

        public IReadOnlyList<ServerAuthoritativePredictionHistoryRecord> GetReplayAfter(ulong confirmedInputSequence)
        {
            int count = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Records[i].Input.InputSequence > confirmedInputSequence)
                    count = checked(count + 1);
            }
            var values = new ServerAuthoritativePredictionHistoryRecord[count];
            int index = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Records[i].Input.InputSequence > confirmedInputSequence)
                    values[index++] = m_Records[i];
            }
            return values;
        }

        public void Add(
            OwnerCanonicalInputBatch input,
            Float32CompletedSimulationStep completed,
            ulong journalCursor,
            ulong confirmedInputSequence,
            ulong lastAuthorityAckTick,
            ulong lastBaselineTick)
        {
            if (!input.IsValid || !completed.IsValid || completed.StepSnapshot == null)
                throw new ArgumentException("Prediction history capture requires input and a completed Step snapshot.");
            if (completed.Step.Tick != completed.StepSnapshot.Tick || completed.State.Actors.Count != 1 ||
                completed.State.Actors[0].ActorId != input.ActorId)
            {
                throw new InvalidOperationException("Prediction history completed step does not match its owner input.");
            }
            int insertionIndex = Find(completed.Step.Tick.Value);
            if (insertionIndex >= 0)
                throw new InvalidOperationException($"Prediction history already contains Tick '{completed.Step.Tick}'.");
            var record = new ServerAuthoritativePredictionHistoryRecord(
                input,
                completed.StepSnapshot.CompositionIdentity,
                completed.StepSnapshot.World,
                completed.StepSnapshot.PipelineProjection,
                completed.Step.ObservedWorldConstraints,
                journalCursor);

            if (m_Count >= m_Capacity)
            {
                if (m_Records[0].Input.InputSequence > confirmedInputSequence)
                {
                    throw new InvalidOperationException(
                        $"Prediction history capacity cannot discard unconfirmed input: firstTick={m_Ticks[0]};firstSequence={m_Records[0].Input.InputSequence};confirmedSequence={confirmedInputSequence};lastAckTick={lastAuthorityAckTick};lastBaselineTick={lastBaselineTick}.");
                }
                m_Count--;
                Array.Copy(m_Ticks, 1, m_Ticks, 0, m_Count);
                Array.Copy(m_Records, 1, m_Records, 0, m_Count);
                if (insertionIndex > 0)
                    insertionIndex--;
            }

            Array.Copy(m_Ticks, insertionIndex, m_Ticks, insertionIndex + 1, m_Count - insertionIndex);
            Array.Copy(m_Records, insertionIndex, m_Records, insertionIndex + 1, m_Count - insertionIndex);
            m_Ticks[insertionIndex] = completed.Step.Tick.Value;
            m_Records[insertionIndex] = record;
            m_Count++;
        }

        public void SealJournalCursor(SimulationTick tick, ulong journalCursor)
        {
            int index = Find(tick.Value);
            if (index < 0)
                throw new InvalidOperationException($"Prediction history has no Current Tick '{tick}' to seal its journal cursor.");
            m_Records[index] = m_Records[index].WithJournalCursor(journalCursor);
        }

        public ServerAuthoritativePredictionHistoryCheckpoint PreparePruneConfirmedThrough(ulong inputSequence)
        {
            int count = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Records[i].Input.InputSequence > inputSequence)
                    count = checked(count + 1);
            }
            var records = new KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>[count];
            int outputIndex = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Records[i].Input.InputSequence > inputSequence)
                    records[outputIndex++] = new KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>(m_Ticks[i], m_Records[i]);
            }
            return new ServerAuthoritativePredictionHistoryCheckpoint(records, m_RemoteBodies.Capture());
        }

        public ServerAuthoritativePredictionHistoryCheckpoint PrepareClear() =>
            ServerAuthoritativePredictionHistoryCheckpoint.Empty(m_RemoteBodies.Capture());

        public ServerAuthoritativePredictionHistoryCheckpoint Capture()
        {
            var records = new KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>[m_Count];
            for (int i = 0; i < records.Length; i++)
                records[i] = new KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>(m_Ticks[i], m_Records[i]);
            return new ServerAuthoritativePredictionHistoryCheckpoint(records, m_RemoteBodies.Capture());
        }

        public void Restore(ServerAuthoritativePredictionHistoryCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            if (checkpoint.Records.Count > m_Capacity)
                throw new InvalidOperationException("Prediction history checkpoint exceeds its configured capacity.");
            for (int i = 0; i < checkpoint.Records.Count; i++)
            {
                KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord> pair = checkpoint.Records[i];
                if (pair.Value == null || pair.Value.Tick.Value != pair.Key ||
                    i > 0 && checkpoint.Records[i - 1].Key >= pair.Key)
                {
                    throw new InvalidOperationException("Prediction history checkpoint Tick order is invalid.");
                }
                m_Ticks[i] = pair.Key;
                m_Records[i] = pair.Value;
            }
            m_Count = checkpoint.Records.Count;
            m_RemoteBodies.Restore(checkpoint.RemoteBodies);
        }

        int Find(ulong tick)
        {
            int left = 0;
            int right = m_Count - 1;
            while (left <= right)
            {
                int middle = left + (right - left) / 2;
                if (m_Ticks[middle] == tick)
                    return middle;
                if (m_Ticks[middle] < tick)
                    left = middle + 1;
                else
                    right = middle - 1;
            }
            return ~left;
        }
    }

    internal sealed class ServerAuthoritativePredictionHistoryCheckpoint
    {
        readonly KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>[] m_Records;

        public ServerAuthoritativePredictionHistoryCheckpoint(
            KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>[] records,
            ServerAuthoritativeRemoteBodyTimelineCheckpoint remoteBodies)
        {
            KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>[] values = records ??
                throw new ArgumentNullException(nameof(records));
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Value == null || values[i].Value.Tick.Value != values[i].Key ||
                    i > 0 && values[i - 1].Key >= values[i].Key)
                {
                    throw new ArgumentException("Prediction history checkpoint Tick order is invalid.", nameof(records));
                }
            }
            m_Records = values;
            RemoteBodies = remoteBodies ?? throw new ArgumentNullException(nameof(remoteBodies));
        }

        public static ServerAuthoritativePredictionHistoryCheckpoint Empty(
            ServerAuthoritativeRemoteBodyTimelineCheckpoint remoteBodies) =>
            new ServerAuthoritativePredictionHistoryCheckpoint(
                Array.Empty<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>>(),
                remoteBodies);

        public IReadOnlyList<KeyValuePair<ulong, ServerAuthoritativePredictionHistoryRecord>> Records => m_Records;
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
                samplingKind is not (ObservedWorldConstraintSamplingKind.Exact or
                    ObservedWorldConstraintSamplingKind.Interpolation or ObservedWorldConstraintSamplingKind.ConstantVelocityExtrapolation))
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

    internal readonly struct ServerAuthoritativeRemoteBodySelectionFrame
    {
        readonly ServerAuthoritativeRemoteBodySelection[] m_Selections;
        static readonly Comparison<ServerAuthoritativeRemoteBodySelection> s_CompareByActor =
            (left, right) => left.ActorId.CompareTo(right.ActorId);

        public ServerAuthoritativeRemoteBodySelectionFrame(
            SimulationTick tick,
            ServerAuthoritativeRemoteBodySelection[] selections)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Remote body selection frame Tick is invalid.", nameof(tick));
            ServerAuthoritativeRemoteBodySelection[] values = selections ??
                throw new ArgumentNullException(nameof(selections));
            Array.Sort(values, s_CompareByActor);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].TargetTick != tick || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Remote body selection frame contains a duplicate Actor or mismatched Tick.", nameof(selections));
            }
            Tick = tick;
            m_Selections = values;
        }

        public SimulationTick Tick { get; }
        public IReadOnlyList<ServerAuthoritativeRemoteBodySelection> Selections => m_Selections;
        public bool IsValid => Tick.IsValid && m_Selections != null;

        public ObservedWorldConstraintFrame ToObservedWorldConstraints(StableHash contactShapeConfigurationHash)
        {
            var constraints = new ObservedWorldConstraint[m_Selections.Length];
            for (int i = 0; i < constraints.Length; i++)
                constraints[i] = m_Selections[i].ToObservedConstraint(contactShapeConfigurationHash);
            return new ObservedWorldConstraintFrame(Tick, constraints);
        }

        public CharacterBodySample[] ToBodySamples()
        {
            var samples = new CharacterBodySample[m_Selections.Length];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = m_Selections[i].ToBodySample();
            return samples;
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
            if (!batch.IsValid)
                throw new ArgumentException("Remote presentation batch is invalid.", nameof(batch));
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
            var actors = new ServerAuthoritativeRemoteBodyActorCheckpoint[m_LockedActors.Length];
            for (int i = 0; i < m_LockedActors.Length; i++)
            {
                ActorId actorId = m_LockedActors[i];
                SortedDictionary<ulong, CharacterBodySample> samples = m_Samples[actorId];
                var values = new CharacterBodySample[samples.Count];
                int index = 0;
                foreach (KeyValuePair<ulong, CharacterBodySample> pair in samples)
                    values[index++] = pair.Value;
                actors[i] = new ServerAuthoritativeRemoteBodyActorCheckpoint(actorId, values);
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
            foreach (KeyValuePair<ulong, CharacterBodySample> pair in samples)
            {
                CharacterBodySample sample = pair.Value;
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
        readonly ServerAuthoritativeRemoteBodyActorCheckpoint[] m_Actors;
        static readonly Comparison<ServerAuthoritativeRemoteBodyActorCheckpoint> s_CompareActorsByActor =
            (left, right) => left.ActorId.CompareTo(right.ActorId);

        public ServerAuthoritativeRemoteBodyTimelineCheckpoint(
            ServerAuthoritativeRemoteBodyActorCheckpoint[] actors)
        {
            ServerAuthoritativeRemoteBodyActorCheckpoint[] values = actors ??
                throw new ArgumentNullException(nameof(actors));
            Array.Sort(values, s_CompareActorsByActor);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Remote body checkpoint roster is invalid.", nameof(actors));
            }
            m_Actors = values;
        }

        public IReadOnlyList<ServerAuthoritativeRemoteBodyActorCheckpoint> Actors => m_Actors;
    }

    internal sealed class ServerAuthoritativeRemoteBodyActorCheckpoint
    {
        readonly CharacterBodySample[] m_Samples;
        static readonly Comparison<CharacterBodySample> s_CompareSamplesByTick =
            (left, right) => left.Tick.CompareTo(right.Tick);

        public ServerAuthoritativeRemoteBodyActorCheckpoint(
            ActorId actorId,
            CharacterBodySample[] samples)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Remote body checkpoint ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            CharacterBodySample[] values = samples ?? Array.Empty<CharacterBodySample>();
            Array.Sort(values, s_CompareSamplesByTick);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].ActorId != actorId || i > 0 && values[i - 1].Tick == values[i].Tick)
                    throw new ArgumentException("Remote body checkpoint sample order is invalid.", nameof(samples));
            }
            m_Samples = values;
        }

        public ActorId ActorId { get; }
        public IReadOnlyList<CharacterBodySample> Samples => m_Samples;
    }
}
