using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public readonly struct RollbackExplicitInputFrontier
    {
        public RollbackExplicitInputFrontier(ActorId actorId, ulong tick)
        {
            ActorId = actorId;
            Tick = tick;
        }

        public ActorId ActorId { get; }
        public ulong Tick { get; }
    }

    public sealed class RollbackCanonicalInputAssembler
    {
        readonly RollbackRoster m_Roster;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>> m_Explicit =
            new Dictionary<ActorId, SortedDictionary<ulong, RollbackActorInputFrame>>();
        readonly SortedDictionary<ulong, RollbackCanonicalInputBundle> m_Canonical =
            new SortedDictionary<ulong, RollbackCanonicalInputBundle>();
        readonly string m_ClockId;
        readonly string m_InputSourceIdentity;
        ulong m_HistoryFloor;
        ulong m_NextTick;
        ulong m_NextBundleSequence = 1;
        int m_ExplicitCount;

        public RollbackCanonicalInputAssembler(
            RollbackRoster roster,
            DeterministicRollbackModelPolicy policy,
            SimulationTick firstTick,
            string clockId,
            string inputSourceIdentity)
        {
            m_Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (!firstTick.IsValid)
                throw new ArgumentException("Rollback canonical input epoch is invalid.", nameof(firstTick));
            m_ClockId = SimulationIdentity.Require(clockId, nameof(clockId));
            m_InputSourceIdentity = SimulationIdentity.Require(inputSourceIdentity, nameof(inputSourceIdentity));
            m_HistoryFloor = firstTick.Value;
            m_NextTick = firstTick.Value;
            for (int i = 0; i < roster.Entries.Count; i++)
                m_Explicit.Add(roster.Entries[i].ActorId, new SortedDictionary<ulong, RollbackActorInputFrame>());
        }

        public SimulationTick NextTick => new SimulationTick(m_NextTick);
        public ulong HistoryFloor => m_HistoryFloor;
        public ulong ExplicitContiguousTick
        {
            get
            {
                ulong result = ulong.MaxValue;
                for (int i = 0; i < m_Roster.Entries.Count; i++)
                {
                    SortedDictionary<ulong, RollbackActorInputFrame> actor = m_Explicit[m_Roster.Entries[i].ActorId];
                    ulong tick = m_HistoryFloor - 1;
                    while (tick < ulong.MaxValue && actor.ContainsKey(tick + 1))
                        tick++;
                    if (tick < result)
                        result = tick;
                }
                return result;
            }
        }
        public ulong ConfirmedTick
        {
            get
            {
                ulong lastCanonical = m_NextTick - 1;
                ulong delay = (ulong)m_Policy.ConfirmationDelayTicks;
                return lastCanonical > delay ? lastCanonical - delay : 0;
            }
        }
        public int ExplicitInputCount => m_ExplicitCount;

        public IReadOnlyList<RollbackExplicitInputFrontier> CaptureExplicitInputFrontiers()
        {
            var result = new RollbackExplicitInputFrontier[m_Roster.Entries.Count];
            for (int i = 0; i < m_Roster.Entries.Count; i++)
            {
                ActorId actorId = m_Roster.Entries[i].ActorId;
                SortedDictionary<ulong, RollbackActorInputFrame> actor = m_Explicit[actorId];
                ulong tick = m_HistoryFloor - 1;
                while (tick < ulong.MaxValue && actor.ContainsKey(tick + 1))
                    tick++;
                result[i] = new RollbackExplicitInputFrontier(actorId, tick);
            }
            return result;
        }

        public bool HasExplicitInputForEveryActor(SimulationTick tick)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Rollback canonical input Tick is invalid.", nameof(tick));
            for (int i = 0; i < m_Roster.Entries.Count; i++)
            {
                if (!m_Explicit[m_Roster.Entries[i].ActorId].ContainsKey(tick.Value))
                    return false;
            }
            return true;
        }

        public IReadOnlyList<RollbackActorInputFrame> SubmitBatch(IReadOnlyList<RollbackActorInputFrame> frames)
        {
            if (frames == null || frames.Count == 0)
                throw new ArgumentException("Rollback canonical input batch is empty.", nameof(frames));
            var accepted = new List<RollbackActorInputFrame>(frames.Count);
            for (int i = 0; i < frames.Count; i++)
            {
                if (SubmitFrame(frames[i]))
                    accepted.Add(frames[i]);
            }
            return accepted.AsReadOnly();
        }

        public bool SubmitFrame(RollbackActorInputFrame frame)
        {
            if (frame == null || frame.Provenance != RollbackInputProvenance.LocalExplicit)
                throw new ArgumentException("Rollback canonical assembler accepts only local explicit frames.", nameof(frame));
            if (!m_Explicit.TryGetValue(frame.ActorId, out SortedDictionary<ulong, RollbackActorInputFrame> actor))
                throw new InvalidOperationException($"Rollback input Actor '{frame.ActorId}' is not in the locked roster.");
            if (frame.Tick.Value < m_HistoryFloor)
                throw new InvalidOperationException($"Rollback input Tick '{frame.Tick}' predates immutable bounded canonical history.");
            if (frame.Tick.Value >= checked(m_NextTick + (ulong)m_Policy.MaximumQueuedBundles))
                throw new InvalidOperationException($"Rollback input Tick '{frame.Tick}' exceeds the bounded canonical assembly window.");
            if (actor.TryGetValue(frame.Tick.Value, out RollbackActorInputFrame current))
            {
                if (current.Identity.Equals(frame.Identity))
                    return false;
                if (current.InputSequence == frame.InputSequence && !current.GameplayHash.Equals(frame.GameplayHash))
                    throw new InvalidOperationException($"Rollback input identity conflict for Actor '{frame.ActorId}' Tick '{frame.Tick}' Sequence '{frame.InputSequence}'.");
                throw new InvalidOperationException($"Rollback Actor '{frame.ActorId}' assigned multiple input sequences to Tick '{frame.Tick}'.");
            }
            if (frame.Tick.Value < m_NextTick)
            {
                RollbackActorInputFrame canonical = m_Canonical.TryGetValue(frame.Tick.Value, out RollbackCanonicalInputBundle bundle)
                    ? bundle.GetRequired(frame.ActorId)
                    : throw new InvalidOperationException($"Rollback immutable canonical Tick '{frame.Tick}' is absent from history.");
                if (canonical.InputSequence == frame.InputSequence && canonical.GameplayHash.Equals(frame.GameplayHash))
                    return false;
                throw new InvalidOperationException($"Rollback input attempts to revise immutable canonical Tick '{frame.Tick}'.");
            }
            int capacity = checked((m_Policy.HistoryLengthTicks + m_Policy.MaximumQueuedBundles) * m_Roster.Entries.Count);
            if (m_ExplicitCount >= capacity)
                throw new InvalidOperationException("Rollback canonical explicit-input capacity is exhausted.");
            actor.Add(frame.Tick.Value, frame);
            m_ExplicitCount++;
            return true;
        }

        public RollbackCanonicalInputBundle AssembleNext()
        {
            var tick = new SimulationTick(m_NextTick);
            if (!HasExplicitInputForEveryActor(tick))
                throw new InvalidOperationException($"Rollback canonical Tick '{tick}' is missing explicit roster input.");
            var frames = new RollbackActorInputFrame[m_Roster.Entries.Count];
            for (int i = 0; i < m_Roster.Entries.Count; i++)
            {
                ActorId actorId = m_Roster.Entries[i].ActorId;
                RollbackActorInputFrame source = m_Explicit[actorId][tick.Value];
                var input = new CharacterSimulationInput(
                    FixedSimulationNumericProfile.Value,
                    new SimulationTickSourceIdentity(SimulationTickSourceKind.Authoritative, m_ClockId, tick.Value),
                    m_InputSourceIdentity,
                    source.InputSequence,
                    source.Input.Values,
                    source.Input.Requests);
                frames[i] = new RollbackActorInputFrame(
                    actorId,
                    tick,
                    source.InputSequence,
                    input,
                    RollbackInputProvenance.CanonicalExplicit);
            }
            ulong sequence = m_NextBundleSequence;
            m_NextBundleSequence = checked(sequence + 1);
            var result = new RollbackCanonicalInputBundle(tick, sequence, frames);
            m_Canonical.Add(tick.Value, result);
            m_NextTick = checked(m_NextTick + 1);
            TrimHistory();
            return result;
        }

        public IReadOnlyList<RollbackCanonicalInputBundle> CaptureCanonicalRange(
            ulong previousConfirmedTick,
            ulong confirmedTick)
        {
            if (confirmedTick <= previousConfirmedTick || confirmedTick >= m_NextTick)
                throw new ArgumentException("Rollback canonical confirmation range is invalid.");
            ulong firstTick = checked(previousConfirmedTick + 1);
            if (firstTick < m_HistoryFloor)
                throw new InvalidOperationException("Rollback canonical confirmation range predates bounded history.");
            int count = checked((int)(confirmedTick - previousConfirmedTick));
            var result = new RollbackCanonicalInputBundle[count];
            for (int i = 0; i < count; i++)
            {
                ulong tick = checked(firstTick + (ulong)i);
                result[i] = m_Canonical.TryGetValue(tick, out RollbackCanonicalInputBundle bundle)
                    ? bundle
                    : throw new InvalidOperationException($"Rollback canonical confirmation Tick '{tick}' is absent.");
            }
            return result;
        }

        void TrimHistory()
        {
            while (m_Canonical.Count > m_Policy.HistoryLengthTicks)
            {
                ulong removeTick = FirstCanonicalTick();
                m_Canonical.Remove(removeTick);
                for (int i = 0; i < m_Roster.Entries.Count; i++)
                {
                    if (m_Explicit[m_Roster.Entries[i].ActorId].Remove(removeTick))
                        m_ExplicitCount--;
                }
                m_HistoryFloor = checked(removeTick + 1);
            }
        }

        ulong FirstCanonicalTick()
        {
            foreach (ulong tick in m_Canonical.Keys)
                return tick;
            throw new InvalidOperationException("Rollback canonical history is empty.");
        }
    }
}
