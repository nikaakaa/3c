using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal readonly struct DeterministicActorContactCandidate
    {
        public DeterministicActorContactCandidate(
            ActorId actorId,
            FixedVector3 beforePosition,
            FixedVector3 candidatePosition,
            DeterministicActorContactShape shape)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Deterministic Actor contact candidate requires an ActorId.", nameof(actorId));
            ActorId = actorId;
            BeforePosition = beforePosition;
            CandidatePosition = candidatePosition;
            Shape = shape;
        }

        public ActorId ActorId { get; }
        public FixedVector3 BeforePosition { get; }
        public FixedVector3 CandidatePosition { get; }
        public DeterministicActorContactShape Shape { get; }
        public FixedVector3 Displacement => CandidatePosition - BeforePosition;
    }

    internal enum DeterministicActorContactTraceKind : byte
    {
        Sweep = 1,
        NormalClip = 2,
        Depenetration = 3,
        Validation = 4,
        Failure = 5
    }

    internal readonly struct DeterministicActorContactTrace
    {
        public DeterministicActorContactTrace(
            DeterministicActorContactTraceKind kind,
            int pairIndex,
            int iteration,
            ActorId actorA,
            ActorId actorB,
            FixedScalar timeOfImpact,
            FixedVector3 normal,
            FixedVector3 correctionA,
            FixedVector3 correctionB,
            string detail)
        {
            Kind = kind;
            PairIndex = pairIndex;
            Iteration = iteration;
            ActorA = actorA;
            ActorB = actorB;
            TimeOfImpact = timeOfImpact;
            Normal = normal;
            CorrectionA = correctionA;
            CorrectionB = correctionB;
            Detail = detail ?? string.Empty;
        }

        public DeterministicActorContactTraceKind Kind { get; }
        public int PairIndex { get; }
        public int Iteration { get; }
        public ActorId ActorA { get; }
        public ActorId ActorB { get; }
        public FixedScalar TimeOfImpact { get; }
        public FixedVector3 Normal { get; }
        public FixedVector3 CorrectionA { get; }
        public FixedVector3 CorrectionB { get; }
        public string Detail { get; }
    }

    internal readonly struct DeterministicActorContactSummary
    {
        public DeterministicActorContactSummary(
            int pairCount,
            int pairChecks,
            int sweepCount,
            int normalClipCount,
            int depenetrationCount,
            int iterationCount,
            int validationCount)
        {
            PairCount = pairCount;
            PairChecks = pairChecks;
            SweepCount = sweepCount;
            NormalClipCount = normalClipCount;
            DepenetrationCount = depenetrationCount;
            IterationCount = iterationCount;
            ValidationCount = validationCount;
        }

        public int PairCount { get; }
        public int PairChecks { get; }
        public int SweepCount { get; }
        public int NormalClipCount { get; }
        public int DepenetrationCount { get; }
        public int IterationCount { get; }
        public int ValidationCount { get; }

        public DeterministicActorContactSummary Add(DeterministicActorContactSummary other) =>
            new DeterministicActorContactSummary(
                Math.Max(PairCount, other.PairCount),
                checked(PairChecks + other.PairChecks),
                checked(SweepCount + other.SweepCount),
                checked(NormalClipCount + other.NormalClipCount),
                checked(DepenetrationCount + other.DepenetrationCount),
                checked(IterationCount + other.IterationCount),
                checked(ValidationCount + other.ValidationCount));
    }

    internal readonly struct DeterministicActorContactBatchResult
    {
        readonly FixedVector3[] m_Positions;
        readonly bool[] m_Contacts;
        readonly List<DeterministicActorContactTrace> m_Traces;
        readonly int m_Count;

        public DeterministicActorContactBatchResult(
            FixedVector3[] positions,
            bool[] contacts,
            List<DeterministicActorContactTrace> traces,
            int count,
            DeterministicActorContactSummary summary,
            bool corrected)
        {
            m_Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            m_Contacts = contacts ?? throw new ArgumentNullException(nameof(contacts));
            m_Traces = traces ?? throw new ArgumentNullException(nameof(traces));
            m_Count = count;
            Summary = summary;
            Corrected = corrected;
        }

        public int Count => m_Count;
        public IReadOnlyList<DeterministicActorContactTrace> Traces => m_Traces;
        public DeterministicActorContactSummary Summary { get; }
        public bool Corrected { get; }
        public FixedVector3 PositionAt(int index) => m_Positions[index];
        public bool HadContactAt(int index) => m_Contacts[index];
    }

    internal sealed class DeterministicActorContactWorkspace
    {
        readonly int m_MaximumPairs;
        readonly int m_MaximumIterations;

        public DeterministicActorContactWorkspace(int actorCapacity, int maximumPairs, int maximumIterations)
        {
            if (actorCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCapacity));
            if (maximumPairs <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumPairs));
            if (maximumIterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumIterations));
            m_MaximumPairs = maximumPairs;
            m_MaximumIterations = maximumIterations;
            Origins = new FixedVector3[actorCapacity];
            Displacements = new FixedVector3[actorCapacity];
            Positions = new FixedVector3[actorCapacity];
            Corrections = new FixedVector3[actorCapacity];
            Contacts = new bool[actorCapacity];
            int traceCapacity = checked(maximumPairs * (maximumIterations * 4 + 1));
            Traces = new List<DeterministicActorContactTrace>(traceCapacity);
        }

        public int ActorCapacity => Origins.Length;
        public int MaximumPairs => m_MaximumPairs;
        public int MaximumIterations => m_MaximumIterations;
        public FixedVector3[] Origins { get; }
        public FixedVector3[] Displacements { get; }
        public FixedVector3[] Positions { get; }
        public FixedVector3[] Corrections { get; }
        public bool[] Contacts { get; }
        public List<DeterministicActorContactTrace> Traces { get; }

        public void Reset(int count)
        {
            if (count <= 0 || count > ActorCapacity)
                throw new InvalidOperationException($"Deterministic Actor contact roster '{count}' exceeds capacity '{ActorCapacity}'.");
            Array.Clear(Contacts, 0, count);
            Traces.Clear();
        }

        public void AddTrace(DeterministicActorContactTrace trace)
        {
            if (Traces.Count >= Traces.Capacity)
            {
                throw new DeterministicActorContactSolveException(
                    $"Deterministic Actor contact trace capacity '{Traces.Capacity}' was exceeded.",
                    SnapshotTraces());
            }
            Traces.Add(trace);
        }

        public DeterministicActorContactTrace[] SnapshotTraces()
        {
            var values = new DeterministicActorContactTrace[Traces.Count];
            Traces.CopyTo(values, 0);
            return values;
        }

        public DeterministicActorContactBatchResult BuildResult(
            int count,
            DeterministicActorContactSummary summary,
            bool corrected) => new DeterministicActorContactBatchResult(
                Positions,
                Contacts,
                Traces,
                count,
                summary,
                corrected);
    }

    internal sealed class DeterministicActorContactSolveException : InvalidOperationException
    {
        public DeterministicActorContactSolveException(string message, IReadOnlyList<DeterministicActorContactTrace> traces)
            : base(message)
        {
            Traces = traces ?? Array.Empty<DeterministicActorContactTrace>();
        }

        public IReadOnlyList<DeterministicActorContactTrace> Traces { get; }
    }

    internal static class DeterministicActorContactSolver
    {
        static readonly FixedScalar NumericalTolerance = FixedScalar.FromRaw(16);
        static readonly FixedScalar Two = FixedScalar.FromInt64(2);
        static readonly FixedScalar Four = FixedScalar.FromInt64(4);

        public static DeterministicActorContactBatchResult Resolve(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            DeterministicActorContactWorkspace workspace)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            int pairCount = Validate(candidates, workspace.MaximumPairs, workspace.MaximumIterations);
            int count = candidates.Count;
            workspace.Reset(count);
            FixedVector3[] origins = workspace.Origins;
            FixedVector3[] displacements = workspace.Displacements;
            bool[] contacts = workspace.Contacts;
            Counters counters = new Counters(pairCount);
            for (int i = 0; i < count; i++)
            {
                origins[i] = candidates[i].BeforePosition;
                displacements[i] = candidates[i].Displacement;
            }

            bool corrected = false;
            for (int iteration = 0; iteration < workspace.MaximumIterations; iteration++)
            {
                bool pass = ResolvePenetrationPass(
                    candidates,
                    origins,
                    displacements,
                    origins,
                    contacts,
                    workspace,
                    ref counters,
                    iteration,
                    "initial-overlap");
                counters.IterationCount++;
                corrected |= pass;
                if (!pass)
                    break;
            }

            corrected |= ResolveSweeps(
                candidates,
                origins,
                displacements,
                contacts,
                workspace,
                ref counters,
                workspace.MaximumIterations);

            FixedVector3[] positions = workspace.Positions;
            for (int i = 0; i < count; i++)
            {
                FixedVector3 value = origins[i] + displacements[i];
                positions[i] = new FixedVector3(value.X, candidates[i].CandidatePosition.Y, value.Z);
            }

            for (int iteration = 0; iteration < workspace.MaximumIterations; iteration++)
            {
                bool pass = ResolvePenetrationPass(
                    candidates,
                    positions,
                    displacements,
                    positions,
                    contacts,
                    workspace,
                    ref counters,
                    iteration,
                    "final-overlap");
                counters.IterationCount++;
                corrected |= pass;
                if (!pass)
                    break;
            }

            ValidateFinal(candidates, positions, workspace, ref counters, workspace.MaximumIterations);
            return workspace.BuildResult(count, counters.Build(), corrected);
        }

        public static DeterministicActorContactBatchResult ResolveFinalPenetrationPass(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            IReadOnlyList<FixedVector3> positions,
            DeterministicActorContactWorkspace workspace,
            int iteration)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            int pairCount = Validate(candidates, workspace.MaximumPairs, 1);
            if (positions == null || positions.Count != candidates.Count)
                throw new ArgumentException("Deterministic Actor contact position roster is invalid.", nameof(positions));
            workspace.Reset(positions.Count);
            FixedVector3[] values = workspace.Positions;
            FixedVector3[] displacements = workspace.Displacements;
            bool[] contacts = workspace.Contacts;
            for (int i = 0; i < positions.Count; i++)
            {
                values[i] = positions[i];
                displacements[i] = candidates[i].Displacement;
            }
            Counters counters = new Counters(pairCount);
            bool corrected = ResolvePenetrationPass(
                candidates,
                values,
                displacements,
                values,
                contacts,
                workspace,
                ref counters,
                iteration,
                "post-static-overlap");
            counters.IterationCount++;
            return workspace.BuildResult(positions.Count, counters.Build(), corrected);
        }

        public static DeterministicActorContactBatchResult ValidateFinal(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            IReadOnlyList<FixedVector3> positions,
            DeterministicActorContactWorkspace workspace,
            int iteration)
        {
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            int pairCount = Validate(candidates, workspace.MaximumPairs, 1);
            if (positions == null || positions.Count != candidates.Count)
                throw new ArgumentException("Deterministic Actor contact position roster is invalid.", nameof(positions));
            workspace.Reset(positions.Count);
            for (int i = 0; i < positions.Count; i++)
                workspace.Positions[i] = positions[i];
            Counters counters = new Counters(pairCount);
            ValidateFinal(candidates, workspace.Positions, workspace, ref counters, iteration);
            return workspace.BuildResult(positions.Count, counters.Build(), false);
        }

        static bool ResolveSweeps(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            FixedVector3[] origins,
            FixedVector3[] displacements,
            bool[] contacts,
            DeterministicActorContactWorkspace workspace,
            ref Counters counters,
            int maximumIterations)
        {
            FixedVector3[] corrections = workspace.Corrections;
            bool anyCorrection = false;
            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                Array.Clear(corrections, 0, candidates.Count);
                bool clipped = false;
                int pairIndex = 0;
                for (int a = 0; a < candidates.Count - 1; a++)
                {
                    for (int b = a + 1; b < candidates.Count; b++, pairIndex++)
                    {
                        counters.PairChecks++;
                        if (!VerticalOverlapDuringMotion(candidates[a], candidates[b]))
                            continue;
                        FixedVector3 relativePosition = Planar(origins[a] - origins[b]);
                        FixedVector3 relativeDisplacement = Planar(displacements[a] - displacements[b]);
                        FixedScalar separation = Separation(candidates[a], candidates[b]);
                        if (!TrySweep(relativePosition, relativeDisplacement, separation, out FixedScalar timeOfImpact))
                            continue;
                        FixedVector3 normal = Normal(relativePosition + relativeDisplacement * timeOfImpact);
                        FixedScalar remaining = FixedScalar.One - timeOfImpact;
                        FixedVector3 remainingA = Planar(displacements[a]) * remaining;
                        FixedVector3 remainingB = Planar(displacements[b]) * remaining;
                        FixedScalar closingA = FixedVector3.Dot(remainingA, normal);
                        FixedScalar closingB = FixedVector3.Dot(remainingB, normal);
                        FixedVector3 correctionA = closingA < FixedScalar.Zero
                            ? normal * -closingA
                            : FixedVector3.Zero;
                        FixedVector3 correctionB = closingB > FixedScalar.Zero
                            ? normal * -closingB
                            : FixedVector3.Zero;
                        if (correctionA == FixedVector3.Zero && correctionB == FixedVector3.Zero)
                            continue;
                        corrections[a] += correctionA;
                        corrections[b] += correctionB;
                        contacts[a] = true;
                        contacts[b] = true;
                        clipped = true;
                        anyCorrection = true;
                        counters.SweepCount++;
                        counters.NormalClipCount++;
                        workspace.AddTrace(new DeterministicActorContactTrace(
                            DeterministicActorContactTraceKind.Sweep,
                            pairIndex,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            timeOfImpact,
                            normal,
                            correctionA,
                            correctionB,
                            "continuous-planar-toi"));
                        workspace.AddTrace(new DeterministicActorContactTrace(
                            DeterministicActorContactTraceKind.NormalClip,
                            pairIndex,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            timeOfImpact,
                            normal,
                            correctionA,
                            correctionB,
                            "solid-body-block"));
                    }
                }
                counters.IterationCount++;
                if (!clipped)
                    return anyCorrection;
                for (int i = 0; i < candidates.Count; i++)
                    displacements[i] += corrections[i];
            }
            RequireNoClosingSweep(candidates, origins, displacements, workspace, ref counters, maximumIterations);
            return anyCorrection;
        }

        static void RequireNoClosingSweep(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            IReadOnlyList<FixedVector3> origins,
            IReadOnlyList<FixedVector3> displacements,
            DeterministicActorContactWorkspace workspace,
            ref Counters counters,
            int iteration)
        {
            int pairIndex = 0;
            for (int a = 0; a < candidates.Count - 1; a++)
            {
                for (int b = a + 1; b < candidates.Count; b++, pairIndex++)
                {
                    counters.PairChecks++;
                    if (!VerticalOverlapDuringMotion(candidates[a], candidates[b]))
                        continue;
                    FixedVector3 relativePosition = Planar(origins[a] - origins[b]);
                    FixedVector3 relativeDisplacement = Planar(displacements[a] - displacements[b]);
                    if (!TrySweep(
                            relativePosition,
                            relativeDisplacement,
                            Separation(candidates[a], candidates[b]),
                            out FixedScalar timeOfImpact))
                    {
                        continue;
                    }
                    FixedVector3 normal = Normal(relativePosition + relativeDisplacement * timeOfImpact);
                    FixedScalar remaining = FixedScalar.One - timeOfImpact;
                    FixedScalar closingA = FixedVector3.Dot(Planar(displacements[a]) * remaining, normal);
                    FixedScalar closingB = FixedVector3.Dot(Planar(displacements[b]) * remaining, normal);
                    if (closingA >= FixedScalar.Zero && closingB <= FixedScalar.Zero)
                        continue;
                    workspace.AddTrace(new DeterministicActorContactTrace(
                        DeterministicActorContactTraceKind.Failure,
                        pairIndex,
                        iteration,
                        candidates[a].ActorId,
                        candidates[b].ActorId,
                        timeOfImpact,
                        normal,
                        FixedVector3.Zero,
                        FixedVector3.Zero,
                        "sweep-iteration-limit"));
                    throw new DeterministicActorContactSolveException(
                        $"Deterministic Actor contact pair '{candidates[a].ActorId}/{candidates[b].ActorId}' still has closing sweep motion after fixed iterations.",
                        workspace.SnapshotTraces());
                }
            }
        }

        static bool ResolvePenetrationPass(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            IReadOnlyList<FixedVector3> positions,
            IReadOnlyList<FixedVector3> displacements,
            FixedVector3[] output,
            bool[] contacts,
            DeterministicActorContactWorkspace workspace,
            ref Counters counters,
            int iteration,
            string detail)
        {
            FixedVector3[] corrections = workspace.Corrections;
            Array.Clear(corrections, 0, candidates.Count);
            bool corrected = false;
            int pairIndex = 0;
            for (int a = 0; a < candidates.Count - 1; a++)
            {
                for (int b = a + 1; b < candidates.Count; b++, pairIndex++)
                {
                    counters.PairChecks++;
                    if (!VerticalOverlapAt(positions[a], candidates[a].Shape, positions[b], candidates[b].Shape))
                        continue;
                    FixedVector3 delta = Planar(positions[a] - positions[b]);
                    FixedScalar separation = Separation(candidates[a], candidates[b]);
                    FixedScalar distanceSquared = delta.SqrMagnitude;
                    FixedScalar required = FixedScalar.Max(FixedScalar.Zero, separation - NumericalTolerance);
                    if (distanceSquared >= required * required)
                        continue;
                    FixedScalar distance = FixedScalar.Sqrt(distanceSquared);
                    FixedScalar penetration = separation - distance;
                    FixedVector3 normal = Normal(delta);
                    bool movedA = HasPlanarMotion(displacements[a]);
                    bool movedB = HasPlanarMotion(displacements[b]);
                    FixedVector3 correctionA;
                    FixedVector3 correctionB;
                    if (movedA && !movedB)
                    {
                        correctionA = normal * penetration;
                        correctionB = FixedVector3.Zero;
                    }
                    else if (!movedA && movedB)
                    {
                        correctionA = FixedVector3.Zero;
                        correctionB = normal * -penetration;
                    }
                    else
                    {
                        FixedScalar aShare = penetration / Two;
                        FixedScalar bShare = penetration - aShare;
                        correctionA = normal * aShare;
                        correctionB = normal * -bShare;
                    }
                    corrections[a] += correctionA;
                    corrections[b] += correctionB;
                    contacts[a] = true;
                    contacts[b] = true;
                    corrected = true;
                    counters.DepenetrationCount++;
                    workspace.AddTrace(new DeterministicActorContactTrace(
                        DeterministicActorContactTraceKind.Depenetration,
                        pairIndex,
                        iteration,
                        candidates[a].ActorId,
                        candidates[b].ActorId,
                        FixedScalar.Zero,
                        normal,
                        correctionA,
                        correctionB,
                        detail));
                }
            }
            for (int i = 0; i < candidates.Count; i++)
                output[i] = positions[i] + corrections[i];
            return corrected;
        }

        static void ValidateFinal(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            IReadOnlyList<FixedVector3> positions,
            DeterministicActorContactWorkspace workspace,
            ref Counters counters,
            int iteration)
        {
            int pairIndex = 0;
            for (int a = 0; a < candidates.Count - 1; a++)
            {
                for (int b = a + 1; b < candidates.Count; b++, pairIndex++)
                {
                    counters.PairChecks++;
                    if (!VerticalOverlapAt(positions[a], candidates[a].Shape, positions[b], candidates[b].Shape))
                        continue;
                    FixedVector3 delta = Planar(positions[a] - positions[b]);
                    FixedScalar separation = Separation(candidates[a], candidates[b]);
                    FixedScalar required = FixedScalar.Max(FixedScalar.Zero, separation - NumericalTolerance);
                    counters.ValidationCount++;
                    if (delta.SqrMagnitude >= required * required)
                    {
                        workspace.AddTrace(new DeterministicActorContactTrace(
                            DeterministicActorContactTraceKind.Validation,
                            pairIndex,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            FixedScalar.One,
                            FixedVector3.Zero,
                            FixedVector3.Zero,
                            FixedVector3.Zero,
                            "minimum-separation-valid"));
                        continue;
                    }
                    workspace.AddTrace(new DeterministicActorContactTrace(
                        DeterministicActorContactTraceKind.Failure,
                        pairIndex,
                        iteration,
                        candidates[a].ActorId,
                        candidates[b].ActorId,
                        FixedScalar.One,
                        Normal(delta),
                        FixedVector3.Zero,
                        FixedVector3.Zero,
                        "minimum-separation-failed"));
                    throw new DeterministicActorContactSolveException(
                        $"Deterministic Actor contact pair '{candidates[a].ActorId}/{candidates[b].ActorId}' remains penetrated.",
                        workspace.SnapshotTraces());
                }
            }
        }

        static bool TrySweep(
            FixedVector3 relativePosition,
            FixedVector3 relativeDisplacement,
            FixedScalar separation,
            out FixedScalar timeOfImpact)
        {
            timeOfImpact = FixedScalar.Zero;
            FixedScalar c = relativePosition.SqrMagnitude - separation * separation;
            FixedScalar closing = FixedVector3.Dot(relativePosition, relativeDisplacement);
            if (c <= FixedScalar.Zero)
                return closing < FixedScalar.Zero;
            FixedScalar a = relativeDisplacement.SqrMagnitude;
            if (a <= NumericalTolerance * NumericalTolerance)
                return false;
            FixedScalar b = closing * Two;
            if (b >= FixedScalar.Zero)
                return false;
            FixedScalar discriminant = b * b - Four * a * c;
            if (discriminant < FixedScalar.Zero)
                return false;
            FixedScalar value = (-b - FixedScalar.Sqrt(discriminant)) / (Two * a);
            if (value < FixedScalar.Zero || value > FixedScalar.One)
                return false;
            timeOfImpact = value;
            return true;
        }

        static int Validate(
            IReadOnlyList<DeterministicActorContactCandidate> candidates,
            int maximumPairs,
            int maximumIterations)
        {
            if (candidates == null || candidates.Count == 0)
                throw new ArgumentException("Deterministic Actor contact requires an Actor roster.", nameof(candidates));
            if (maximumPairs <= 0 || maximumIterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumPairs));
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].ActorId.IsValid ||
                    i > 0 && candidates[i - 1].ActorId.CompareTo(candidates[i].ActorId) >= 0)
                {
                    throw new ArgumentException("Deterministic Actor contact roster must be uniquely sorted by ActorId.", nameof(candidates));
                }
            }
            long pairCount = checked((long)candidates.Count * (candidates.Count - 1) / 2);
            if (pairCount > maximumPairs)
                throw new InvalidOperationException($"Deterministic Actor contact pair count '{pairCount}' exceeds capacity '{maximumPairs}'.");
            return checked((int)pairCount);
        }

        static bool VerticalOverlapDuringMotion(
            DeterministicActorContactCandidate a,
            DeterministicActorContactCandidate b)
        {
            FixedScalar aMinimum = FixedScalar.Min(a.BeforePosition.Y, a.CandidatePosition.Y);
            FixedScalar aMaximum = FixedScalar.Max(a.BeforePosition.Y, a.CandidatePosition.Y) + a.Shape.Height;
            FixedScalar bMinimum = FixedScalar.Min(b.BeforePosition.Y, b.CandidatePosition.Y);
            FixedScalar bMaximum = FixedScalar.Max(b.BeforePosition.Y, b.CandidatePosition.Y) + b.Shape.Height;
            FixedScalar tolerance = FixedScalar.Max(a.Shape.CollisionOffset, b.Shape.CollisionOffset);
            return aMinimum < bMaximum - tolerance && bMinimum < aMaximum - tolerance;
        }

        static bool VerticalOverlapAt(
            FixedVector3 aPosition,
            DeterministicActorContactShape aShape,
            FixedVector3 bPosition,
            DeterministicActorContactShape bShape)
        {
            FixedScalar tolerance = FixedScalar.Max(aShape.CollisionOffset, bShape.CollisionOffset);
            return aPosition.Y < bPosition.Y + bShape.Height - tolerance &&
                   bPosition.Y < aPosition.Y + aShape.Height - tolerance;
        }

        static FixedScalar Separation(
            DeterministicActorContactCandidate a,
            DeterministicActorContactCandidate b) =>
            a.Shape.SeparationRadius + b.Shape.SeparationRadius;

        static FixedVector3 Normal(FixedVector3 value)
        {
            FixedScalar magnitudeSquared = value.SqrMagnitude;
            if (magnitudeSquared <= NumericalTolerance * NumericalTolerance)
                return new FixedVector3(FixedScalar.One, FixedScalar.Zero, FixedScalar.Zero);
            FixedScalar inverseMagnitude = FixedScalar.One / FixedScalar.Sqrt(magnitudeSquared);
            return value * inverseMagnitude;
        }

        static FixedVector3 Planar(FixedVector3 value) =>
            new FixedVector3(value.X, FixedScalar.Zero, value.Z);

        static bool HasPlanarMotion(FixedVector3 value) =>
            value.X != FixedScalar.Zero || value.Z != FixedScalar.Zero;

        struct Counters
        {
            public Counters(int pairCount)
            {
                PairCount = pairCount;
                PairChecks = 0;
                SweepCount = 0;
                NormalClipCount = 0;
                DepenetrationCount = 0;
                IterationCount = 0;
                ValidationCount = 0;
            }

            public int PairCount { get; }
            public int PairChecks { get; set; }
            public int SweepCount { get; set; }
            public int NormalClipCount { get; set; }
            public int DepenetrationCount { get; set; }
            public int IterationCount { get; set; }
            public int ValidationCount { get; set; }

            public DeterministicActorContactSummary Build() => new DeterministicActorContactSummary(
                PairCount,
                PairChecks,
                SweepCount,
                NormalClipCount,
                DepenetrationCount,
                IterationCount,
                ValidationCount);
        }
    }
}
