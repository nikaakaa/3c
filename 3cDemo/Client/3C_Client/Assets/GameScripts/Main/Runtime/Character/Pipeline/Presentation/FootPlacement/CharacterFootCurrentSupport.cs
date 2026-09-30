using System;
using KK.GeneratedDiagnosticSampling;
using UnityEngine;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootCurrentSupportProbeState : byte
    {
        Accepted = 1,
        Rejected = 2,
        NotExecuted = 3
    }

    public enum CharacterFootCurrentSupportProbeRejectReason : byte
    {
        None = 0,
        CapacityExceeded = 2,
        NoHit = 3,
        NotGrounded = 4
    }

    public enum CharacterFootCurrentSupportRejectReason : byte
    {
        None = 0,
        NoSupport = 1,
        IncompleteQuery = 2,
        NotGrounded = 3,
        WorldRevisionMismatch = 4
    }

    public enum CharacterFootCurrentSupportSelectionReason : byte
    {
        None = 0,
        HighestRequiredDisplacement = 1,
        EquivalentDisplacementSurfaceIdentity = 2,
        EquivalentDisplacementSampleOrder = 3,
        SingleSampleSupport = 4
    }

    internal readonly struct CharacterFootCurrentSupportProbeRequest
    {
        internal CharacterFootCurrentSupportProbeRequest(
            CharacterFootSide side, int sampleIndex, Vector3 probePosition,
            Vector3 unitComponentUp, in CharacterFootCurrentSupportQuerySettings settings)
        {
            Side = side;
            SampleIndex = sampleIndex;
            ProbePosition = probePosition;
            UnitComponentUp = unitComponentUp;
            Settings = settings;
        }

        internal CharacterFootSide Side { get; }
        internal int SampleIndex { get; }
        internal Vector3 ProbePosition { get; }
        internal Vector3 UnitComponentUp { get; }
        internal readonly CharacterFootCurrentSupportQuerySettings Settings;
        internal int LayerMask => Settings.GroundLayerMask;
        internal float MinimumGroundNormalDot => Settings.MinimumGroundNormalDot;
        internal int HitCapacity => Settings.HitCapacity;
        internal CharacterFootPlacementQueryPurpose Purpose => CharacterFootPlacementQueryPurpose.CurrentSupport;
        internal Vector3 Origin => ProbePosition + UnitComponentUp * Settings.CastAbove;
        internal Vector3 Direction => -UnitComponentUp;
        internal float MaximumDistance => Settings.CastAbove + Settings.CastBelow;
    }

    internal readonly struct CharacterFootCurrentSupportProbeResult
    {
        internal CharacterFootCurrentSupportProbeResult(
            CharacterFootCurrentSupportProbeState state,
            CharacterFootCurrentSupportProbeRejectReason rejectReason,
            CharacterFootSupportQueryDiagnostics coverage,
            int surfaceIdentity, Vector3 point, Vector3 normal,
            float distance, ulong worldRevision)
        {
            State = state;
            RejectReason = rejectReason;
            Coverage = coverage;
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal;
            Distance = distance;
            WorldRevision = worldRevision;
        }

        internal CharacterFootCurrentSupportProbeState State { get; }
        internal CharacterFootCurrentSupportProbeRejectReason RejectReason { get; }
        internal CharacterFootSupportQueryDiagnostics Coverage { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }
        internal ulong WorldRevision { get; }
        internal bool Accepted => State == CharacterFootCurrentSupportProbeState.Accepted;

        internal static CharacterFootCurrentSupportProbeResult Rejected(
            CharacterFootCurrentSupportProbeRejectReason reason, ulong worldRevision,
            CharacterFootSupportQueryDiagnostics coverage) =>
            new CharacterFootCurrentSupportProbeResult(
                CharacterFootCurrentSupportProbeState.Rejected, reason, coverage,
                0, default, default, 0f, worldRevision);

        internal static CharacterFootCurrentSupportProbeResult NotGrounded(ulong worldRevision) =>
            new CharacterFootCurrentSupportProbeResult(
                CharacterFootCurrentSupportProbeState.NotExecuted,
                CharacterFootCurrentSupportProbeRejectReason.NotGrounded, default,
                0, default, default, 0f, worldRevision);
    }

    internal readonly struct CharacterFootSoleProbeObservation
    {
        internal CharacterFootSoleProbeObservation(Vector3 position, in CharacterFootCurrentSupportProbeResult result)
        {
            Position = position;
            Result = result;
        }

        internal readonly Vector3 Position;
        internal readonly CharacterFootCurrentSupportProbeResult Result;
    }

    public enum CharacterFootSupportTargetKind : byte
    {
        CurrentSupport = 1,
        SwingGround = 2,
        VerifiedAnchor = 3,
        LockedFullAnchor = 4,
        LockedSliding = 5,
        Releasing = 6
    }

    public enum CharacterFootSupportPositionSource : byte
    {
        CurrentSupport = 1,
        SwingMotion = 2,
        ContactAnchor = 3,
        ReleasingSwing = 4
    }

    public enum CharacterFootSupportNormalSource : byte
    {
        CurrentSupport = 1,
        ContactAnchor = 2,
        RetainedContactAnchor = 3,
        PredictedLanding = 4
    }

    internal readonly struct CharacterFootSupportTarget
    {
        internal CharacterFootSupportTarget(
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootSide side,
            Vector3 position,
            Vector3 supportNormal,
            int surfaceIdentity,
            ulong worldRevision,
            CharacterFootSupportTargetKind kind,
            CharacterFootSupportPositionSource positionSource,
            ulong positionFrameSequence,
            ulong positionCompletionIdentity,
            ulong positionEventIdentity,
            ulong positionPathIdentity,
            CharacterFootSupportNormalSource normalSource,
            ulong normalFrameSequence,
            ulong normalCompletionIdentity,
            ulong normalEventIdentity)
        {
            if (frameSequence == 0 || completionIdentity == 0 ||
                (side != CharacterFootSide.Left && side != CharacterFootSide.Right) ||
                surfaceIdentity == 0 || worldRevision == 0 ||
                !Finite(position) || !Finite(supportNormal) ||
                supportNormal.sqrMagnitude <= 0.000001f ||
                kind == 0 || positionSource == 0 || normalSource == 0 ||
                positionFrameSequence == 0 ||
                positionCompletionIdentity == 0 ||
                normalFrameSequence == 0 ||
                normalCompletionIdentity == 0)
            {
                throw new ArgumentException("Current Support target is invalid.");
            }
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            Side = side;
            Position = position;
            SupportNormal = supportNormal.normalized;
            SurfaceIdentity = surfaceIdentity;
            WorldRevision = worldRevision;
            Kind = kind;
            PositionSource = positionSource;
            PositionFrameSequence = positionFrameSequence;
            PositionCompletionIdentity = positionCompletionIdentity;
            PositionEventIdentity = positionEventIdentity;
            PositionPathIdentity = positionPathIdentity;
            NormalSource = normalSource;
            NormalFrameSequence = normalFrameSequence;
            NormalCompletionIdentity = normalCompletionIdentity;
            NormalEventIdentity = normalEventIdentity;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterFootSide Side { get; }
        internal Vector3 Position { get; }
        internal Vector3 SupportNormal { get; }
        internal int SurfaceIdentity { get; }
        internal ulong WorldRevision { get; }
        internal CharacterFootSupportTargetKind Kind { get; }
        internal CharacterFootSupportPositionSource PositionSource { get; }
        internal ulong PositionFrameSequence { get; }
        internal ulong PositionCompletionIdentity { get; }
        internal ulong PositionEventIdentity { get; }
        internal ulong PositionPathIdentity { get; }
        internal CharacterFootSupportNormalSource NormalSource { get; }
        internal ulong NormalFrameSequence { get; }
        internal ulong NormalCompletionIdentity { get; }
        internal ulong NormalEventIdentity { get; }
        internal bool IsValid => m_IsSpecified != 0;

        internal CharacterFootSupportTarget WithSupportNormal(
            Vector3 supportNormal) =>
            new CharacterFootSupportTarget(
                FrameSequence,
                CompletionIdentity,
                Side,
                Position,
                supportNormal,
                SurfaceIdentity,
                WorldRevision,
                Kind,
                PositionSource,
                PositionFrameSequence,
                PositionCompletionIdentity,
                PositionEventIdentity,
                PositionPathIdentity,
                NormalSource,
                NormalFrameSequence,
                NormalCompletionIdentity,
                NormalEventIdentity);

        internal CharacterFootSupportTarget WithPosition(Vector3 position) =>
            new CharacterFootSupportTarget(
                FrameSequence, CompletionIdentity, Side, position, SupportNormal,
                SurfaceIdentity, WorldRevision, Kind, PositionSource,
                PositionFrameSequence, PositionCompletionIdentity,
                PositionEventIdentity, PositionPathIdentity, NormalSource,
                NormalFrameSequence, NormalCompletionIdentity, NormalEventIdentity);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    public readonly struct CharacterFootSupportTargetDiagnostics
    {
        internal CharacterFootSupportTargetDiagnostics(
            in CharacterFootSupportTarget target)
        {
            Available = target.IsValid;
            FrameSequence = target.FrameSequence;
            CompletionIdentity = target.CompletionIdentity;
            Side = target.Side;
            Position = target.Position;
            SupportNormal = target.SupportNormal;
            SurfaceIdentity = target.SurfaceIdentity;
            WorldRevision = target.WorldRevision;
            Kind = target.Kind;
            PositionSource = target.PositionSource;
            PositionFrameSequence = target.PositionFrameSequence;
            PositionCompletionIdentity = target.PositionCompletionIdentity;
            PositionEventIdentity = target.PositionEventIdentity;
            PositionPathIdentity = target.PositionPathIdentity;
            NormalSource = target.NormalSource;
            NormalFrameSequence = target.NormalFrameSequence;
            NormalCompletionIdentity = target.NormalCompletionIdentity;
            NormalEventIdentity = target.NormalEventIdentity;
        }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        public bool Available { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        public ulong FrameSequence { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        public ulong CompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        public CharacterFootSide Side { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public Vector3 Position { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public Vector3 SupportNormal { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public int SurfaceIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong WorldRevision { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public CharacterFootSupportTargetKind Kind { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public CharacterFootSupportPositionSource PositionSource { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong PositionFrameSequence { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong PositionCompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong PositionEventIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong PositionPathIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public CharacterFootSupportNormalSource NormalSource { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong NormalFrameSequence { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong NormalCompletionIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("support-target")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Available))]
        public ulong NormalEventIdentity { get; }
    }

    internal readonly struct CharacterFootCurrentSupportObservation
    {
        internal const float SelectionEpsilon = 0.0001f;
        readonly FixedList4096Bytes<CharacterFootSoleProbeObservation> m_Probes;
        readonly CharacterFootCurrentSupportQuerySettings m_Settings;
        readonly Vector3 m_UnitUp;

        internal CharacterFootCurrentSupportObservation(
            ulong frameSequence, ulong completionIdentity, CharacterFootSide side,
            ulong worldRevision, Vector3 unitUp,
            in CharacterFootCurrentSupportQuerySettings settings,
            in FixedList4096Bytes<CharacterFootSoleProbeObservation> probes,
            CharacterFootCurrentSupportRejectReason rejectReason,
            int acceptedSampleCount, float requiredDisplacement, int selectedSampleIndex,
            CharacterFootCurrentSupportSelectionReason selectionReason,
            in CharacterFootSupportTarget target)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            Side = side;
            WorldRevision = worldRevision;
            m_UnitUp = unitUp;
            m_Settings = settings;
            m_Probes = probes;
            RejectReason = rejectReason;
            AcceptedSampleCount = acceptedSampleCount;
            RequiredDisplacement = requiredDisplacement;
            SelectedSampleIndex = selectedSampleIndex;
            SelectionReason = selectionReason;
            Target = target;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterFootSide Side { get; }
        internal ulong WorldRevision { get; }
        internal int SampleCount => m_Probes.Length;
        internal int AcceptedSampleCount { get; }
        internal CharacterFootCurrentSupportRejectReason RejectReason { get; }
        internal float RequiredDisplacement { get; }
        internal int SelectedSampleIndex { get; }
        internal CharacterFootCurrentSupportSelectionReason SelectionReason { get; }
        internal readonly CharacterFootSupportTarget Target;
        internal bool IsSpecified => FrameSequence != 0;
        internal bool Available => IsSpecified && RejectReason == CharacterFootCurrentSupportRejectReason.None;

        internal CharacterFootCurrentSupportProbeDiagnostics GetProbe(int index)
        {
            CharacterFootSoleProbeObservation probe = m_Probes[index];
            var request = new CharacterFootCurrentSupportProbeRequest(Side, index, probe.Position, m_UnitUp, in m_Settings);
            return new CharacterFootCurrentSupportProbeDiagnostics(in request, in probe.Result);
        }

        internal bool TryResolveHeightConstraint(out float displacement, out int surfaceIdentity)
        {
            displacement = RequiredDisplacement;
            surfaceIdentity = Target.SurfaceIdentity;
            return Available;
        }
    }

    public readonly struct CharacterFootCurrentSupportProbeDiagnostics
    {
        internal CharacterFootCurrentSupportProbeDiagnostics(
            in CharacterFootCurrentSupportProbeRequest request,
            in CharacterFootCurrentSupportProbeResult result)
        {
            SampleIndex = request.SampleIndex;
            State = result.State;
            RejectReason = result.RejectReason;
            ProbePosition = request.ProbePosition;
            Origin = request.Origin;
            Direction = request.Direction;
            MaximumDistance = request.MaximumDistance;
            LayerMask = request.LayerMask;
            MinimumGroundNormalDot = request.MinimumGroundNormalDot;
            HitCapacity = request.HitCapacity;
            Coverage = result.Coverage;
            SurfaceIdentity = result.SurfaceIdentity;
            Point = result.Point;
            Normal = result.Normal;
            Distance = result.Distance;
            WorldRevision = result.WorldRevision;
            RequiredDisplacement = result.Accepted
                ? Vector3.Dot(result.Point - request.ProbePosition, request.UnitComponentUp) : 0f;
        }

        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public int SampleIndex { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public CharacterFootCurrentSupportProbeState State { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public CharacterFootCurrentSupportProbeRejectReason RejectReason { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public Vector3 ProbePosition { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public Vector3 Origin { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public Vector3 Direction { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public float MaximumDistance { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public int LayerMask { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public float MinimumGroundNormalDot { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public int HitCapacity { get; }
        public CharacterFootSupportQueryDiagnostics Coverage { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public int SurfaceIdentity { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public Vector3 Point { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public Vector3 Normal { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public float Distance { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public ulong WorldRevision { get; }
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public bool RaycastExecuted => State != CharacterFootCurrentSupportProbeState.NotExecuted;
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public bool Accepted => State == CharacterFootCurrentSupportProbeState.Accepted;
        [DiagnosticField] [DiagnosticGroup("current-support-probe")]
        public float RequiredDisplacement { get; }
    }

    public readonly struct CharacterFootCurrentSupportDiagnostics
    {
        readonly CharacterFootCurrentSupportObservation m_Observation;

        internal CharacterFootCurrentSupportDiagnostics(in CharacterFootCurrentSupportObservation observation)
        {
            m_Observation = observation;
            Target = new CharacterFootSupportTargetDiagnostics(in observation.Target);
        }

        [DiagnosticField] [DiagnosticGroup("current-support")]
        public ulong FrameSequence => m_Observation.FrameSequence;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public ulong CompletionIdentity => m_Observation.CompletionIdentity;
        public CharacterFootSide Side => m_Observation.Side;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public ulong WorldRevision => m_Observation.WorldRevision;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public bool IsSpecified => m_Observation.IsSpecified;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public bool Available => m_Observation.Available;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public CharacterFootCurrentSupportRejectReason RejectReason => m_Observation.RejectReason;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public int SampleCount => m_Observation.SampleCount;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public int AcceptedSampleCount => m_Observation.AcceptedSampleCount;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public float RequiredDisplacement => m_Observation.RequiredDisplacement;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public int SelectedSampleIndex => m_Observation.SelectedSampleIndex;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public CharacterFootCurrentSupportSelectionReason SelectionReason => m_Observation.SelectionReason;
        [DiagnosticField] [DiagnosticGroup("current-support")]
        public float SelectionEpsilon => CharacterFootCurrentSupportObservation.SelectionEpsilon;
        public CharacterFootSupportTargetDiagnostics Target { get; }
        public CharacterFootCurrentSupportProbeDiagnostics GetProbe(int index) => m_Observation.GetProbe(index);
        public CharacterFootCurrentSupportProbePage Probes => new CharacterFootCurrentSupportProbePage(in m_Observation);
    }

    public readonly struct CharacterFootCurrentSupportProbePage
    {
        readonly CharacterFootCurrentSupportObservation m_Observation;

        internal CharacterFootCurrentSupportProbePage(in CharacterFootCurrentSupportObservation observation)
        {
            m_Observation = observation;
        }

        public int Count => m_Observation.SampleCount;
        public CharacterFootCurrentSupportProbeDiagnostics this[int index] => m_Observation.GetProbe(index);
    }

    internal interface ICharacterFootCurrentSupportWorldQuery
    {
        CharacterFootCurrentSupportProbeResult Query(
            in CharacterFootCurrentSupportProbeRequest request);
    }

    internal sealed class CharacterFootCurrentSupportObservationPage
    {
        CharacterFootCurrentSupportObservation m_Observation;

        internal bool HasValue { get; private set; }
        internal ref readonly CharacterFootCurrentSupportObservation Observation =>
            ref m_Observation;

        internal void Set(in CharacterFootCurrentSupportObservation observation)
        {
            if (!observation.IsSpecified)
                throw new ArgumentException("Current Support observation is invalid.");
            HasValue = true;
            m_Observation = observation;
        }

        internal void Clear()
        {
            HasValue = false;
            m_Observation = default;
        }
    }

    internal sealed class CharacterFootCurrentSupportObservationPagePool
    {
        readonly CharacterFootCurrentSupportObservationPage m_First = new();
        readonly CharacterFootCurrentSupportObservationPage m_Second = new();

        internal CharacterFootCurrentSupportObservationPage AcquireWritable(
            CharacterFootCurrentSupportObservationPage committed)
        {
            CharacterFootCurrentSupportObservationPage pending =
                ReferenceEquals(committed, m_First) ? m_Second : m_First;
            pending.Clear();
            return pending;
        }

        internal static void Discard(
            CharacterFootCurrentSupportObservationPage pending,
            CharacterFootCurrentSupportObservationPage committed)
        {
            if (pending != null && !ReferenceEquals(pending, committed))
                pending.Clear();
        }

        internal void Reset()
        {
            m_First.Clear();
            m_Second.Clear();
        }
    }
    internal readonly struct CharacterFootSoleSupportQuery
    {
        readonly ICharacterFootCurrentSupportWorldQuery m_World;
        readonly CharacterFootCurrentSupportQuerySettings m_Settings;

        internal CharacterFootSoleSupportQuery(
            ICharacterFootCurrentSupportWorldQuery world,
            in CharacterFootCurrentSupportQuerySettings settings)
        {
            m_World = world;
            m_Settings = settings;
        }

        internal CharacterFootCurrentSupportObservation Query(
            ulong frameSequence, ulong completionIdentity, ulong worldRevision,
            CharacterFootSide side, Vector3 componentUp, bool grounded,
            in CharacterFootPlacementSoleContactPose contacts)
        {
            Vector3 up = componentUp.normalized;
            var probes = new FixedList4096Bytes<CharacterFootSoleProbeObservation>();
            int acceptedCount = 0;
            float displacement = float.NegativeInfinity;
            CharacterFootCurrentSupportRejectReason rejection = grounded
                ? CharacterFootCurrentSupportRejectReason.None : CharacterFootCurrentSupportRejectReason.NotGrounded;
            for (int i = 0; i < contacts.SoleSamples.Length; i++)
            {
                Vector3 position = contacts.SoleSamples[i];
                var request = new CharacterFootCurrentSupportProbeRequest(side, i, position, up, in m_Settings);
                CharacterFootCurrentSupportProbeResult result = grounded
                    ? m_World.Query(in request) : CharacterFootCurrentSupportProbeResult.NotGrounded(worldRevision);
                probes.Add(new CharacterFootSoleProbeObservation(position, in result));
                if (result.WorldRevision != worldRevision)
                    rejection = CharacterFootCurrentSupportRejectReason.WorldRevisionMismatch;
                else if (result.RejectReason == CharacterFootCurrentSupportProbeRejectReason.CapacityExceeded)
                    rejection = CharacterFootCurrentSupportRejectReason.IncompleteQuery;
                if (result.Accepted)
                {
                    acceptedCount++;
                    displacement = Mathf.Max(displacement, Vector3.Dot(result.Point - position, up));
                }
            }
            if (acceptedCount == 0 && rejection == CharacterFootCurrentSupportRejectReason.None)
                rejection = CharacterFootCurrentSupportRejectReason.NoSupport;

            int selected = -1;
            var reason = CharacterFootCurrentSupportSelectionReason.None;
            CharacterFootSupportTarget target = default;
            if (rejection == CharacterFootCurrentSupportRejectReason.None)
            {
                reason = acceptedCount == 1
                    ? CharacterFootCurrentSupportSelectionReason.SingleSampleSupport
                    : CharacterFootCurrentSupportSelectionReason.HighestRequiredDisplacement;
                for (int i = 0; i < probes.Length; i++)
                {
                    CharacterFootSoleProbeObservation probe = probes[i];
                    if (!probe.Result.Accepted ||
                        displacement - Vector3.Dot(probe.Result.Point - probe.Position, up) >
                        CharacterFootCurrentSupportObservation.SelectionEpsilon)
                        continue;
                    if (selected < 0)
                        selected = i;
                    else
                    {
                        int previousIdentity = probes[selected].Result.SurfaceIdentity;
                        if (probe.Result.SurfaceIdentity != previousIdentity)
                            reason = CharacterFootCurrentSupportSelectionReason.EquivalentDisplacementSurfaceIdentity;
                        else if (reason != CharacterFootCurrentSupportSelectionReason.EquivalentDisplacementSurfaceIdentity)
                            reason = CharacterFootCurrentSupportSelectionReason.EquivalentDisplacementSampleOrder;
                        if (probe.Result.SurfaceIdentity < previousIdentity)
                            selected = i;
                    }
                }
                CharacterFootCurrentSupportProbeResult support = probes[selected].Result;
                Vector3 originalSole = (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
                target = new CharacterFootSupportTarget(
                    frameSequence, completionIdentity, side, originalSole + up * displacement,
                    support.Normal, support.SurfaceIdentity, worldRevision,
                    CharacterFootSupportTargetKind.CurrentSupport,
                    CharacterFootSupportPositionSource.CurrentSupport, frameSequence, completionIdentity, 0, 0,
                    CharacterFootSupportNormalSource.CurrentSupport, frameSequence, completionIdentity, 0);
            }
            return new CharacterFootCurrentSupportObservation(
                frameSequence, completionIdentity, side, worldRevision, up, in m_Settings, in probes,
                rejection, acceptedCount, acceptedCount > 0 ? displacement : 0f, selected, reason, in target);
        }
    }
}
