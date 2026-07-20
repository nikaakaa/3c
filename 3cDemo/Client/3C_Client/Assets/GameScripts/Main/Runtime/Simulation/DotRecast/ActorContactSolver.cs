using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.DotRecast
{
    public enum ActorContactMobility : byte
    {
        ActiveSimulated = 1,
        ObservedKinematic = 2
    }

    public readonly struct ActorContactCandidate
    {
        public ActorContactCandidate(
            ActorId actorId,
            Float32Vector3 beforePosition,
            Float32Vector3 candidatePosition,
            ActorContactShape shape,
            ActorContactMobility mobility)
        {
            if (!actorId.IsValid || !Enum.IsDefined(typeof(ActorContactMobility), mobility))
                throw new ArgumentException("Actor contact candidate ActorId is invalid.", nameof(actorId));
            ActorId = actorId;
            BeforePosition = beforePosition;
            CandidatePosition = candidatePosition;
            Shape = shape;
            Mobility = mobility;
        }

        public ActorId ActorId { get; }
        public Float32Vector3 BeforePosition { get; }
        public Float32Vector3 CandidatePosition { get; }
        public ActorContactShape Shape { get; }
        public ActorContactMobility Mobility { get; }
    }

    public enum ActorContactTraceKind : byte
    {
        Sweep = 1,
        NormalClip = 2,
        Depenetration = 3,
        Validation = 4,
        Failure = 5
    }

    public readonly struct ActorContactTrace
    {
        public ActorContactTrace(
            ActorContactTraceKind kind,
            int iteration,
            ActorId actorA,
            ActorId actorB,
            ActorContactMobility mobilityA,
            ActorContactMobility mobilityB,
            Float32Scalar timeOfImpact,
            Float32Vector3 normal,
            Float32Vector3 correctionA,
            Float32Vector3 correctionB,
            string detail)
        {
            Kind = kind;
            Iteration = iteration;
            ActorA = actorA;
            ActorB = actorB;
            MobilityA = mobilityA;
            MobilityB = mobilityB;
            TimeOfImpact = timeOfImpact;
            Normal = normal;
            CorrectionA = correctionA;
            CorrectionB = correctionB;
            Detail = detail ?? string.Empty;
        }

        public ActorContactTraceKind Kind { get; }
        public int Iteration { get; }
        public ActorId ActorA { get; }
        public ActorId ActorB { get; }
        public ActorContactMobility MobilityA { get; }
        public ActorContactMobility MobilityB { get; }
        public Float32Scalar TimeOfImpact { get; }
        public Float32Vector3 Normal { get; }
        public Float32Vector3 CorrectionA { get; }
        public Float32Vector3 CorrectionB { get; }
        public string Detail { get; }
    }

    public sealed class ActorContactSolveException : InvalidOperationException
    {
        public ActorContactSolveException(string message, IReadOnlyList<ActorContactTrace> traces)
            : base(message)
        {
            Traces = traces ?? Array.Empty<ActorContactTrace>();
        }

        public IReadOnlyList<ActorContactTrace> Traces { get; }
    }

    public sealed class ActorContactBatchResult
    {
        readonly Float32Vector3[] m_Positions;
        readonly bool[] m_Contacts;
        readonly ReadOnlyCollection<ActorContactTrace> m_Traces;

        internal ActorContactBatchResult(
            Float32Vector3[] positions,
            bool[] contacts,
            List<ActorContactTrace> traces)
        {
            m_Positions = positions;
            m_Contacts = contacts;
            m_Traces = traces.AsReadOnly();
        }

        public int Count => m_Positions.Length;
        public IReadOnlyList<ActorContactTrace> Traces => m_Traces;
        public Float32Vector3 PositionAt(int index) => m_Positions[index];
        public bool HadContactAt(int index) => m_Contacts[index];
    }

    public static class ActorContactSolver
    {
        const double NormalEpsilon = 0.0000001d;

        public static ActorContactBatchResult Resolve(
            IReadOnlyList<ActorContactCandidate> candidates,
            ActorContactSolverConfiguration configuration)
        {
            if (candidates == null || candidates.Count == 0)
                throw new ArgumentException("Actor contact solver requires a candidate roster.", nameof(candidates));
            RequireStableRoster(candidates);
            int count = candidates.Count;
            var originX = new double[count];
            var originZ = new double[count];
            var displacementX = new double[count];
            var displacementZ = new double[count];
            var correctionX = new double[count];
            var correctionZ = new double[count];
            var totalDepenetrationX = new double[count];
            var totalDepenetrationZ = new double[count];
            var contacts = new bool[count];
            var traces = new List<ActorContactTrace>();
            for (int i = 0; i < count; i++)
            {
                ActorContactCandidate candidate = candidates[i];
                originX[i] = candidate.BeforePosition.X.ToDouble();
                originZ[i] = candidate.BeforePosition.Z.ToDouble();
                displacementX[i] = candidate.CandidatePosition.X.ToDouble() - originX[i];
                displacementZ[i] = candidate.CandidatePosition.Z.ToDouble() - originZ[i];
            }

            ResolveInitialOverlaps(
                candidates,
                configuration,
                originX,
                originZ,
                correctionX,
                correctionZ,
                totalDepenetrationX,
                totalDepenetrationZ,
                contacts,
                traces);
            ResolveSweeps(
                candidates,
                configuration,
                originX,
                originZ,
                displacementX,
                displacementZ,
                correctionX,
                correctionZ,
                contacts,
                traces);

            var positions = new Float32Vector3[count];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Float32Vector3(
                    Float32Scalar.FromDouble(originX[i] + displacementX[i]),
                    candidates[i].CandidatePosition.Y,
                    Float32Scalar.FromDouble(originZ[i] + displacementZ[i]));
            }
            ValidateFinal(candidates, positions, configuration, traces);
            return new ActorContactBatchResult(positions, contacts, traces);
        }

        public static IReadOnlyList<ActorContactTrace> ValidateFinal(
            IReadOnlyList<ActorContactCandidate> candidates,
            IReadOnlyList<Float32Vector3> finalPositions,
            ActorContactSolverConfiguration configuration)
        {
            if (candidates == null || candidates.Count == 0)
                throw new ArgumentException("Actor contact solver requires a candidate roster.", nameof(candidates));
            RequireStableRoster(candidates);
            var traces = new List<ActorContactTrace>();
            ValidateFinal(candidates, finalPositions, configuration, traces);
            return traces.AsReadOnly();
        }

        static void ResolveInitialOverlaps(
            IReadOnlyList<ActorContactCandidate> candidates,
            ActorContactSolverConfiguration configuration,
            double[] originX,
            double[] originZ,
            double[] correctionX,
            double[] correctionZ,
            double[] totalDepenetrationX,
            double[] totalDepenetrationZ,
            bool[] contacts,
            List<ActorContactTrace> traces)
        {
            double tolerance = configuration.ContactTolerance.ToDouble();
            double maximum = configuration.MaximumDepenetrationDistance.ToDouble();
            for (int iteration = 0; iteration < configuration.IterationCount; iteration++)
            {
                Array.Clear(correctionX, 0, correctionX.Length);
                Array.Clear(correctionZ, 0, correctionZ.Length);
                bool corrected = false;
                for (int a = 0; a < candidates.Count - 1; a++)
                {
                    for (int b = a + 1; b < candidates.Count; b++)
                    {
                        bool activeA = candidates[a].Mobility == ActorContactMobility.ActiveSimulated;
                        bool activeB = candidates[b].Mobility == ActorContactMobility.ActiveSimulated;
                        if (!activeA && !activeB)
                            continue;
                        if (!VerticalOverlapAtStart(candidates[a], candidates[b], tolerance))
                            continue;
                        double dx = originX[a] - originX[b];
                        double dz = originZ[a] - originZ[b];
                        double distance = Math.Sqrt(dx * dx + dz * dz);
                        double separation = Separation(candidates[a], candidates[b]);
                        double penetration = separation - distance;
                        if (penetration <= tolerance)
                            continue;
                        if (penetration > maximum + tolerance)
                        {
                            traces.Add(BuildTrace(
                                ActorContactTraceKind.Failure,
                                iteration,
                                candidates[a].ActorId,
                                candidates[b].ActorId,
                                candidates[a].Mobility,
                                candidates[b].Mobility,
                                0d,
                                0d,
                                0d,
                                0d,
                                0d,
                                0d,
                                0d,
                                "initial-overlap-exceeds-maximum"));
                            throw new ActorContactSolveException(
                                $"Actor contact pair '{candidates[a].ActorId}/{candidates[b].ActorId}' exceeds maximum initial depenetration.",
                                traces.AsReadOnly());
                        }
                        Normal(dx, dz, out double nx, out double nz);
                        double shareA = activeA && activeB ? penetration * 0.5d : activeA ? penetration : 0d;
                        double shareB = activeA && activeB ? penetration * 0.5d : activeB ? penetration : 0d;
                        correctionX[a] += nx * shareA;
                        correctionZ[a] += nz * shareA;
                        correctionX[b] -= nx * shareB;
                        correctionZ[b] -= nz * shareB;
                        contacts[a] |= activeA;
                        contacts[b] |= activeB;
                        corrected = true;
                        traces.Add(BuildTrace(
                            ActorContactTraceKind.Depenetration,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            candidates[a].Mobility,
                            candidates[b].Mobility,
                            0d,
                            nx,
                            nz,
                            nx * shareA,
                            nz * shareA,
                            -nx * shareB,
                            -nz * shareB,
                            activeA && activeB ? "initial-overlap-symmetric" : "initial-overlap-active-only"));
                    }
                }
                if (!corrected)
                    return;
                for (int i = 0; i < candidates.Count; i++)
                {
                    totalDepenetrationX[i] += correctionX[i];
                    totalDepenetrationZ[i] += correctionZ[i];
                    double total = Math.Sqrt(
                        totalDepenetrationX[i] * totalDepenetrationX[i] +
                        totalDepenetrationZ[i] * totalDepenetrationZ[i]);
                    if (total > maximum + tolerance)
                    {
                        traces.Add(BuildTrace(
                            ActorContactTraceKind.Failure,
                            iteration,
                            candidates[i].ActorId,
                            candidates[i].ActorId,
                            candidates[i].Mobility,
                            candidates[i].Mobility,
                            0d,
                            0d,
                            0d,
                            correctionX[i],
                            correctionZ[i],
                            0d,
                            0d,
                            "accumulated-depenetration-exceeds-maximum"));
                        throw new ActorContactSolveException(
                            $"Actor '{candidates[i].ActorId}' exceeds maximum accumulated depenetration.",
                            traces.AsReadOnly());
                    }
                    originX[i] += correctionX[i];
                    originZ[i] += correctionZ[i];
                }
            }
        }

        static void ResolveSweeps(
            IReadOnlyList<ActorContactCandidate> candidates,
            ActorContactSolverConfiguration configuration,
            double[] originX,
            double[] originZ,
            double[] displacementX,
            double[] displacementZ,
            double[] correctionX,
            double[] correctionZ,
            bool[] contacts,
            List<ActorContactTrace> traces)
        {
            double tolerance = configuration.ContactTolerance.ToDouble();
            for (int iteration = 0; iteration < configuration.IterationCount; iteration++)
            {
                Array.Clear(correctionX, 0, correctionX.Length);
                Array.Clear(correctionZ, 0, correctionZ.Length);
                bool clipped = false;
                for (int a = 0; a < candidates.Count - 1; a++)
                {
                    for (int b = a + 1; b < candidates.Count; b++)
                    {
                        bool activeA = candidates[a].Mobility == ActorContactMobility.ActiveSimulated;
                        bool activeB = candidates[b].Mobility == ActorContactMobility.ActiveSimulated;
                        if (!activeA && !activeB)
                            continue;
                        if (!VerticalOverlapDuringMotion(candidates[a], candidates[b], tolerance))
                            continue;
                        double px = originX[a] - originX[b];
                        double pz = originZ[a] - originZ[b];
                        double vx = displacementX[a] - displacementX[b];
                        double vz = displacementZ[a] - displacementZ[b];
                        if (!TrySweep(px, pz, vx, vz, Separation(candidates[a], candidates[b]), out double toi))
                            continue;
                        double contactX = px + vx * toi;
                        double contactZ = pz + vz * toi;
                        Normal(contactX, contactZ, out double nx, out double nz);
                        double remaining = 1d - toi;
                        double remainingAX = displacementX[a] * remaining;
                        double remainingAZ = displacementZ[a] * remaining;
                        double remainingBX = displacementX[b] * remaining;
                        double remainingBZ = displacementZ[b] * remaining;
                        double correctionAX;
                        double correctionAZ;
                        double correctionBX;
                        double correctionBZ;
                        if (activeA && activeB)
                        {
                            double closingA = remainingAX * nx + remainingAZ * nz;
                            double closingB = remainingBX * nx + remainingBZ * nz;
                            correctionAX = closingA < 0d ? -nx * closingA : 0d;
                            correctionAZ = closingA < 0d ? -nz * closingA : 0d;
                            correctionBX = closingB > 0d ? -nx * closingB : 0d;
                            correctionBZ = closingB > 0d ? -nz * closingB : 0d;
                        }
                        else
                        {
                            double relativeClosing =
                                (remainingAX - remainingBX) * nx +
                                (remainingAZ - remainingBZ) * nz;
                            correctionAX = activeA && relativeClosing < 0d ? -nx * relativeClosing : 0d;
                            correctionAZ = activeA && relativeClosing < 0d ? -nz * relativeClosing : 0d;
                            correctionBX = activeB && relativeClosing < 0d ? nx * relativeClosing : 0d;
                            correctionBZ = activeB && relativeClosing < 0d ? nz * relativeClosing : 0d;
                        }
                        if (MagnitudeSquared(correctionAX, correctionAZ) <= NormalEpsilon * NormalEpsilon &&
                            MagnitudeSquared(correctionBX, correctionBZ) <= NormalEpsilon * NormalEpsilon)
                        {
                            continue;
                        }
                        correctionX[a] += correctionAX;
                        correctionZ[a] += correctionAZ;
                        correctionX[b] += correctionBX;
                        correctionZ[b] += correctionBZ;
                        contacts[a] |= activeA;
                        contacts[b] |= activeB;
                        clipped = true;
                        traces.Add(BuildTrace(
                            ActorContactTraceKind.Sweep,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            candidates[a].Mobility,
                            candidates[b].Mobility,
                            toi,
                            nx,
                            nz,
                            correctionAX,
                            correctionAZ,
                            correctionBX,
                            correctionBZ,
                            "continuous-disk-toi"));
                        traces.Add(BuildTrace(
                            ActorContactTraceKind.NormalClip,
                            iteration,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            candidates[a].Mobility,
                            candidates[b].Mobility,
                            toi,
                            nx,
                            nz,
                            correctionAX,
                            correctionAZ,
                            correctionBX,
                            correctionBZ,
                            activeA && activeB
                                ? "remove-closing-normal-preserve-tangent"
                                : "remove-relative-closing-normal-from-active"));
                    }
                }
                if (!clipped)
                    return;
                for (int i = 0; i < candidates.Count; i++)
                {
                    displacementX[i] += correctionX[i];
                    displacementZ[i] += correctionZ[i];
                }
            }
        }

        static void ValidateFinal(
            IReadOnlyList<ActorContactCandidate> candidates,
            IReadOnlyList<Float32Vector3> positions,
            ActorContactSolverConfiguration configuration,
            List<ActorContactTrace> traces)
        {
            if (positions == null || positions.Count != candidates.Count)
                throw new ArgumentException("Actor contact final position roster is invalid.", nameof(positions));
            double tolerance = configuration.ContactTolerance.ToDouble();
            for (int a = 0; a < candidates.Count - 1; a++)
            {
                for (int b = a + 1; b < candidates.Count; b++)
                {
                    if (candidates[a].Mobility == ActorContactMobility.ObservedKinematic &&
                        candidates[b].Mobility == ActorContactMobility.ObservedKinematic)
                    {
                        continue;
                    }
                    if (!VerticalOverlapAtFinal(candidates[a], positions[a], candidates[b], positions[b], tolerance))
                        continue;
                    double dx = positions[a].X.ToDouble() - positions[b].X.ToDouble();
                    double dz = positions[a].Z.ToDouble() - positions[b].Z.ToDouble();
                    double distance = Math.Sqrt(dx * dx + dz * dz);
                    double separation = Separation(candidates[a], candidates[b]);
                    if (distance + tolerance >= separation)
                    {
                        traces.Add(BuildTrace(
                            ActorContactTraceKind.Validation,
                            configuration.IterationCount,
                            candidates[a].ActorId,
                            candidates[b].ActorId,
                            candidates[a].Mobility,
                            candidates[b].Mobility,
                            1d,
                            0d,
                            0d,
                            0d,
                            0d,
                            0d,
                            0d,
                            "minimum-separation-valid"));
                        continue;
                    }
                    traces.Add(BuildTrace(
                        ActorContactTraceKind.Failure,
                        configuration.IterationCount,
                        candidates[a].ActorId,
                        candidates[b].ActorId,
                        candidates[a].Mobility,
                        candidates[b].Mobility,
                        1d,
                        0d,
                        0d,
                        0d,
                        0d,
                        0d,
                        0d,
                        "minimum-separation-failed"));
                    throw new ActorContactSolveException(
                        $"Actor contact pair '{candidates[a].ActorId}/{candidates[b].ActorId}' remains penetrated after fixed iterations.",
                        traces.AsReadOnly());
                }
            }
        }

        static void RequireStableRoster(IReadOnlyList<ActorContactCandidate> candidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].ActorId.IsValid)
                    throw new ArgumentException("Actor contact candidate roster contains an invalid ActorId.", nameof(candidates));
                if (i > 0 && candidates[i - 1].ActorId.CompareTo(candidates[i].ActorId) >= 0)
                    throw new ArgumentException("Actor contact candidate roster must be uniquely sorted by ActorId.", nameof(candidates));
            }
        }

        static bool TrySweep(
            double px,
            double pz,
            double vx,
            double vz,
            double separation,
            out double timeOfImpact)
        {
            timeOfImpact = 0d;
            double c = px * px + pz * pz - separation * separation;
            double relativeClosing = px * vx + pz * vz;
            if (c <= 0d)
                return relativeClosing < 0d;
            double a = vx * vx + vz * vz;
            if (a <= NormalEpsilon * NormalEpsilon)
                return false;
            double b = 2d * relativeClosing;
            if (b >= 0d)
                return false;
            double discriminant = b * b - 4d * a * c;
            if (discriminant < 0d)
                return false;
            double value = (-b - Math.Sqrt(discriminant)) / (2d * a);
            if (value < 0d || value > 1d)
                return false;
            timeOfImpact = value;
            return true;
        }

        static bool VerticalOverlapAtStart(
            ActorContactCandidate a,
            ActorContactCandidate b,
            double tolerance) => IntervalsOverlap(
                a.BeforePosition.Y.ToDouble(),
                a.BeforePosition.Y.ToDouble() + a.Shape.Height.ToDouble(),
                b.BeforePosition.Y.ToDouble(),
                b.BeforePosition.Y.ToDouble() + b.Shape.Height.ToDouble(),
                tolerance);

        static bool VerticalOverlapDuringMotion(
            ActorContactCandidate a,
            ActorContactCandidate b,
            double tolerance)
        {
            double aMin = Math.Min(a.BeforePosition.Y.ToDouble(), a.CandidatePosition.Y.ToDouble());
            double aMax = Math.Max(a.BeforePosition.Y.ToDouble(), a.CandidatePosition.Y.ToDouble()) + a.Shape.Height.ToDouble();
            double bMin = Math.Min(b.BeforePosition.Y.ToDouble(), b.CandidatePosition.Y.ToDouble());
            double bMax = Math.Max(b.BeforePosition.Y.ToDouble(), b.CandidatePosition.Y.ToDouble()) + b.Shape.Height.ToDouble();
            return IntervalsOverlap(aMin, aMax, bMin, bMax, tolerance);
        }

        static bool VerticalOverlapAtFinal(
            ActorContactCandidate a,
            Float32Vector3 aPosition,
            ActorContactCandidate b,
            Float32Vector3 bPosition,
            double tolerance) => IntervalsOverlap(
                aPosition.Y.ToDouble(),
                aPosition.Y.ToDouble() + a.Shape.Height.ToDouble(),
                bPosition.Y.ToDouble(),
                bPosition.Y.ToDouble() + b.Shape.Height.ToDouble(),
                tolerance);

        static bool IntervalsOverlap(double aMin, double aMax, double bMin, double bMax, double tolerance) =>
            aMin < bMax - tolerance && bMin < aMax - tolerance;

        static double Separation(ActorContactCandidate a, ActorContactCandidate b) =>
            a.Shape.SeparationRadius.ToDouble() + b.Shape.SeparationRadius.ToDouble();

        static void Normal(double x, double z, out double normalX, out double normalZ)
        {
            double length = Math.Sqrt(x * x + z * z);
            if (length <= NormalEpsilon)
            {
                normalX = 1d;
                normalZ = 0d;
                return;
            }
            normalX = x / length;
            normalZ = z / length;
        }

        static double MagnitudeSquared(double x, double z) => x * x + z * z;

        static ActorContactTrace BuildTrace(
            ActorContactTraceKind kind,
            int iteration,
            ActorId actorA,
            ActorId actorB,
            ActorContactMobility mobilityA,
            ActorContactMobility mobilityB,
            double timeOfImpact,
            double normalX,
            double normalZ,
            double correctionAX,
            double correctionAZ,
            double correctionBX,
            double correctionBZ,
            string detail) => new ActorContactTrace(
                kind,
                iteration,
                actorA,
                actorB,
                mobilityA,
                mobilityB,
                Float32Scalar.FromDouble(timeOfImpact),
                new Float32Vector3(
                    Float32Scalar.FromDouble(normalX),
                    Float32Scalar.Zero,
                    Float32Scalar.FromDouble(normalZ)),
                new Float32Vector3(
                    Float32Scalar.FromDouble(correctionAX),
                    Float32Scalar.Zero,
                    Float32Scalar.FromDouble(correctionAZ)),
                new Float32Vector3(
                    Float32Scalar.FromDouble(correctionBX),
                    Float32Scalar.Zero,
                    Float32Scalar.FromDouble(correctionBZ)),
                detail);
    }
}
