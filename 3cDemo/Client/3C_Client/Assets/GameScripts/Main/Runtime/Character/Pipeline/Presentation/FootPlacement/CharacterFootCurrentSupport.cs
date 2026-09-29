using System;
using KK.GeneratedDiagnosticSampling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootCurrentSupportProbeKind : byte
    {
        Heel = 1,
        Toe = 2
    }

    public enum CharacterFootCurrentSupportProbeState : byte
    {
        Accepted = 1,
        Rejected = 2,
        NotExecuted = 3
    }

    public enum CharacterFootCurrentSupportProbeRejectReason : byte
    {
        None = 0,
        InvalidRequest = 1,
        CapacityExceeded = 2,
        NoHit = 3,
        NotGrounded = 4
    }

    public enum CharacterFootCurrentSupportRejectReason : byte
    {
        None = 0,
        HeelUnavailable = 1,
        ToeUnavailable = 2,
        HeelAndToeUnavailable = 3,
        InvalidSupportNormal = 4,
        NotGrounded = 5,
        WorldRevisionMismatch = 6
    }

    public enum CharacterFootCurrentSupportSelectionReason : byte
    {
        None = 0,
        HeelHigherRequiredDisplacement = 1,
        ToeHigherRequiredDisplacement = 2,
        EquivalentDisplacementSurfaceIdentity = 3,
        EquivalentDisplacementHeelOrder = 4,
        HeelOnlySupport = 5,
        ToeOnlySupport = 6
    }

    internal readonly struct CharacterFootCurrentSupportProbeRequest
    {
        internal CharacterFootCurrentSupportProbeRequest(
            CharacterFootSide side,
            CharacterFootCurrentSupportProbeKind kind,
            Vector3 probePosition,
            Vector3 componentUp,
            float castAbove,
            float castBelow,
            float radius,
            int layerMask,
            float minimumGroundNormalDot,
            int hitCapacity)
        {
            Side = side;
            Kind = kind;
            ProbePosition = probePosition;
            ComponentUp = componentUp;
            CastAbove = castAbove;
            CastBelow = castBelow;
            Radius = radius;
            LayerMask = layerMask;
            MinimumGroundNormalDot = minimumGroundNormalDot;
            HitCapacity = hitCapacity;
        }

        internal static CharacterFootCurrentSupportProbeRequest Create(
            CharacterFootSide side,
            CharacterFootCurrentSupportProbeKind kind,
            Vector3 probePosition,
            Vector3 componentUp,
            in CharacterFootCurrentSupportQuerySettings support,
            in CharacterFootLandingPredictionSettings landing) =>
            new CharacterFootCurrentSupportProbeRequest(
                side, kind, probePosition, componentUp,
                support.CastAbove, support.CastBelow, landing.SphereRadius,
                support.GroundLayerMask, support.MinimumGroundNormalDot,
                support.HitCapacity);

        internal CharacterFootSide Side { get; }
        internal CharacterFootCurrentSupportProbeKind Kind { get; }
        internal Vector3 ProbePosition { get; }
        internal Vector3 ComponentUp { get; }
        internal float CastAbove { get; }
        internal float CastBelow { get; }
        internal float Radius { get; }
        internal int LayerMask { get; }
        internal float MinimumGroundNormalDot { get; }
        internal int HitCapacity { get; }
        internal CharacterFootPlacementQueryPurpose Purpose =>
            CharacterFootPlacementQueryPurpose.CurrentSupport;
        internal Vector3 Origin => ProbePosition + ComponentUp.normalized * CastAbove;
        internal Vector3 Direction => -ComponentUp.normalized;
        internal float MaximumDistance => CastAbove + CastBelow;
        internal float SupportMaximumDistance => MaximumDistance + Radius;
        internal bool IsValid =>
            (Side == CharacterFootSide.Left || Side == CharacterFootSide.Right) &&
            (Kind == CharacterFootCurrentSupportProbeKind.Heel ||
             Kind == CharacterFootCurrentSupportProbeKind.Toe) &&
            Finite(ProbePosition) && Finite(ComponentUp) &&
            ComponentUp.sqrMagnitude > 0.000001f &&
            float.IsFinite(CastAbove) && CastAbove > Radius &&
            float.IsFinite(CastBelow) && CastBelow > 0f &&
            float.IsFinite(Radius) && Radius > 0f &&
            LayerMask != 0 &&
            HitCapacity >= 4 && HitCapacity <= 32 &&
            float.IsFinite(MinimumGroundNormalDot) &&
            MinimumGroundNormalDot >= -1f && MinimumGroundNormalDot <= 1f;

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    internal readonly struct CharacterFootCurrentSupportProbeResult
    {
        internal CharacterFootCurrentSupportProbeResult(
            CharacterFootCurrentSupportProbeKind kind,
            CharacterFootCurrentSupportProbeState state,
            CharacterFootCurrentSupportProbeRejectReason rejectReason,
            int candidateCount,
            CharacterFootSupportQueryDiagnostics coverage,
            int surfaceIdentity,
            Vector3 point,
            Vector3 normal,
            float distance,
            ulong worldRevision,
            bool sphereCastExecuted)
        {
            Kind = kind;
            State = state;
            RejectReason = rejectReason;
            CandidateCount = candidateCount;
            Coverage = coverage;
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal;
            Distance = distance;
            WorldRevision = worldRevision;
            SphereCastExecuted = sphereCastExecuted;
        }

        internal CharacterFootCurrentSupportProbeKind Kind { get; }
        internal CharacterFootCurrentSupportProbeState State { get; }
        internal CharacterFootCurrentSupportProbeRejectReason RejectReason { get; }
        internal int CandidateCount { get; }
        internal CharacterFootSupportQueryDiagnostics Coverage { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }
        internal ulong WorldRevision { get; }
        internal bool SphereCastExecuted { get; }
        internal bool Accepted =>
            State == CharacterFootCurrentSupportProbeState.Accepted &&
            RejectReason == CharacterFootCurrentSupportProbeRejectReason.None &&
            SurfaceIdentity != 0 && WorldRevision != 0;

        internal static CharacterFootCurrentSupportProbeResult Rejected(
            CharacterFootCurrentSupportProbeKind kind,
            CharacterFootCurrentSupportProbeRejectReason reason,
            ulong worldRevision,
            bool sphereCastExecuted,
            CharacterFootSupportQueryDiagnostics coverage) =>
            new CharacterFootCurrentSupportProbeResult(
                kind,
                CharacterFootCurrentSupportProbeState.Rejected,
                reason,
                0,
                coverage,
                0,
                default,
                default,
                0f,
                worldRevision,
                sphereCastExecuted);

        internal static CharacterFootCurrentSupportProbeResult NotExecuted(
            CharacterFootCurrentSupportProbeKind kind,
            CharacterFootCurrentSupportProbeRejectReason reason,
            ulong worldRevision) =>
            new CharacterFootCurrentSupportProbeResult(
                kind,
                CharacterFootCurrentSupportProbeState.NotExecuted,
                reason,
                0,
                default,
                0,
                default,
                default,
                0f,
                worldRevision,
                false);
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
        readonly CharacterFootCurrentSupportProbeRequest m_HeelRequest;
        readonly CharacterFootCurrentSupportProbeRequest m_ToeRequest;
        readonly CharacterFootCurrentSupportProbeResult m_Heel;
        readonly CharacterFootCurrentSupportProbeResult m_Toe;
        readonly CharacterFootSupportTarget m_Target;

        internal CharacterFootCurrentSupportObservation(
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootSide side,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest heelRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            in CharacterFootCurrentSupportProbeResult heel,
            in CharacterFootCurrentSupportProbeResult toe,
            CharacterFootCurrentSupportRejectReason rejectReason,
            float heelRequiredDisplacement,
            float toeRequiredDisplacement,
            CharacterFootCurrentSupportProbeKind selectedProbe,
            CharacterFootCurrentSupportSelectionReason selectionReason,
            Vector3 selectedSupportNormalBeforeNormalization,
            in CharacterFootSupportTarget target)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            Side = side;
            WorldRevision = worldRevision;
            m_HeelRequest = heelRequest;
            m_ToeRequest = toeRequest;
            m_Heel = heel;
            m_Toe = toe;
            RejectReason = rejectReason;
            HeelRequiredDisplacement = heelRequiredDisplacement;
            ToeRequiredDisplacement = toeRequiredDisplacement;
            SelectedProbe = selectedProbe;
            SelectionReason = selectionReason;
            SelectedSupportNormalBeforeNormalization =
                selectedSupportNormalBeforeNormalization;
            m_Target = target;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterFootSide Side { get; }
        internal ulong WorldRevision { get; }
        internal ref readonly CharacterFootCurrentSupportProbeRequest HeelRequest =>
            ref m_HeelRequest;
        internal ref readonly CharacterFootCurrentSupportProbeRequest ToeRequest =>
            ref m_ToeRequest;
        internal ref readonly CharacterFootCurrentSupportProbeResult Heel =>
            ref m_Heel;
        internal ref readonly CharacterFootCurrentSupportProbeResult Toe =>
            ref m_Toe;
        internal CharacterFootCurrentSupportRejectReason RejectReason { get; }
        internal float HeelRequiredDisplacement { get; }
        internal float ToeRequiredDisplacement { get; }
        internal CharacterFootCurrentSupportProbeKind SelectedProbe { get; }
        internal CharacterFootCurrentSupportSelectionReason SelectionReason
        {
            get;
        }
        internal float SelectionEpsilon => 0.0001f;
        internal Vector3 SelectedSupportNormalBeforeNormalization { get; }
        internal ref readonly CharacterFootSupportTarget Target =>
            ref m_Target;
        internal bool IsSpecified => m_IsSpecified != 0;
        internal bool Available =>
            IsSpecified && RejectReason == CharacterFootCurrentSupportRejectReason.None &&
            Target.IsValid;

        internal bool TryResolveHeightConstraint(out float displacement, out int surfaceIdentity)
        {
            displacement = 0f;
            surfaceIdentity = 0;
            if (!Available)
                return false;
            displacement = SelectedProbe == CharacterFootCurrentSupportProbeKind.Heel
                ? HeelRequiredDisplacement : ToeRequiredDisplacement;
            surfaceIdentity = Target.SurfaceIdentity;
            return true;
        }

        internal static CharacterFootCurrentSupportObservation Resolve(
            ulong frameSequence,
            ulong completionIdentity,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest heelRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            in CharacterFootCurrentSupportProbeResult heel,
            in CharacterFootCurrentSupportProbeResult toe)
        {
            CharacterFootSide side = heelRequest.Side;
            if (worldRevision == 0 || heel.WorldRevision != worldRevision ||
                toe.WorldRevision != worldRevision)
            {
                return new CharacterFootCurrentSupportObservation(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in heelRequest,
                    in toeRequest,
                    in heel,
                    in toe,
                    CharacterFootCurrentSupportRejectReason
                        .WorldRevisionMismatch,
                    0f,
                    0f,
                    default,
                    CharacterFootCurrentSupportSelectionReason.None,
                    default,
                    default);
            }
            CharacterFootCurrentSupportRejectReason rejectReason =
                ResolveRejectReason(in heel, in toe);
            if (rejectReason != CharacterFootCurrentSupportRejectReason.None)
            {
                return new CharacterFootCurrentSupportObservation(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in heelRequest,
                    in toeRequest,
                    in heel,
                    in toe,
                    rejectReason,
                    0f,
                    0f,
                    default,
                    CharacterFootCurrentSupportSelectionReason.None,
                    default,
                    default);
            }
            Vector3 animatedHeel = heelRequest.ProbePosition;
            Vector3 animatedToe = toeRequest.ProbePosition;
            Vector3 up = heelRequest.ComponentUp.normalized;
            float heelDisplacement = heel.Accepted ? Vector3.Dot(heel.Point - animatedHeel, up) : 0f;
            float toeDisplacement = toe.Accepted ? Vector3.Dot(toe.Point - animatedToe, up) : 0f;
            CharacterFootCurrentSupportSelectionReason selectionReason;
            CharacterFootCurrentSupportProbeKind selected;
            if (!toe.Accepted)
            {
                selected = CharacterFootCurrentSupportProbeKind.Heel;
                selectionReason = CharacterFootCurrentSupportSelectionReason.HeelOnlySupport;
            }
            else if (!heel.Accepted)
            {
                selected = CharacterFootCurrentSupportProbeKind.Toe;
                selectionReason = CharacterFootCurrentSupportSelectionReason.ToeOnlySupport;
            }
            else
            {
                selected = SelectProbe(heelDisplacement, toeDisplacement,
                    heel.SurfaceIdentity, toe.SurfaceIdentity, out selectionReason);
            }
            Vector3 selectedNormal = selected ==
                                     CharacterFootCurrentSupportProbeKind.Heel
                ? heel.Normal
                : toe.Normal;
            if (!Finite(selectedNormal) ||
                selectedNormal.sqrMagnitude <= 0.000001f)
            {
                return new CharacterFootCurrentSupportObservation(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in heelRequest,
                    in toeRequest,
                    in heel,
                    in toe,
                    CharacterFootCurrentSupportRejectReason.InvalidSupportNormal,
                    heelDisplacement,
                    toeDisplacement,
                    selected,
                    selectionReason,
                    selectedNormal,
                    default);
            }
            float displacement = selected == CharacterFootCurrentSupportProbeKind.Heel
                ? heelDisplacement : toeDisplacement;
            Vector3 originalSole = (animatedHeel + animatedToe) * 0.5f;
            int surfaceIdentity = selected ==
                                  CharacterFootCurrentSupportProbeKind.Heel
                ? heel.SurfaceIdentity
                : toe.SurfaceIdentity;
            var target = new CharacterFootSupportTarget(
                frameSequence,
                completionIdentity,
                side,
                originalSole + up * displacement,
                selectedNormal,
                surfaceIdentity,
                worldRevision,
                CharacterFootSupportTargetKind.CurrentSupport,
                CharacterFootSupportPositionSource.CurrentSupport,
                frameSequence,
                completionIdentity,
                0,
                0,
                CharacterFootSupportNormalSource.CurrentSupport,
                frameSequence,
                completionIdentity,
                0);
            return new CharacterFootCurrentSupportObservation(
                frameSequence,
                completionIdentity,
                side,
                worldRevision,
                in heelRequest,
                in toeRequest,
                in heel,
                in toe,
                CharacterFootCurrentSupportRejectReason.None,
                heelDisplacement,
                toeDisplacement,
                selected,
                selectionReason,
                selectedNormal,
                in target);
        }

        internal static CharacterFootCurrentSupportObservation Unavailable(
            ulong frameSequence,
            ulong completionIdentity,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest heelRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            CharacterFootCurrentSupportRejectReason reason)
        {
            if (reason == CharacterFootCurrentSupportRejectReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            CharacterFootCurrentSupportProbeResult heel =
                CharacterFootCurrentSupportProbeResult.NotExecuted(
                    CharacterFootCurrentSupportProbeKind.Heel,
                    CharacterFootCurrentSupportProbeRejectReason.NotGrounded,
                    worldRevision);
            CharacterFootCurrentSupportProbeResult toe =
                CharacterFootCurrentSupportProbeResult.NotExecuted(
                    CharacterFootCurrentSupportProbeKind.Toe,
                    CharacterFootCurrentSupportProbeRejectReason.NotGrounded,
                    worldRevision);
            return new CharacterFootCurrentSupportObservation(
                frameSequence,
                completionIdentity,
                heelRequest.Side,
                worldRevision,
                in heelRequest,
                in toeRequest,
                in heel,
                in toe,
                reason,
                0f,
                0f,
                default,
                CharacterFootCurrentSupportSelectionReason.None,
                default,
                default);
        }

        static CharacterFootCurrentSupportRejectReason ResolveRejectReason(
            in CharacterFootCurrentSupportProbeResult heel,
            in CharacterFootCurrentSupportProbeResult toe)
        {
            bool heelAccepted = heel.Accepted;
            bool toeAccepted = toe.Accepted;
            if (heelAccepted && toeAccepted ||
                heelAccepted && toe.RejectReason == CharacterFootCurrentSupportProbeRejectReason.NoHit ||
                toeAccepted && heel.RejectReason == CharacterFootCurrentSupportProbeRejectReason.NoHit)
                return CharacterFootCurrentSupportRejectReason.None;
            if (!heelAccepted && !toeAccepted)
                return CharacterFootCurrentSupportRejectReason.HeelAndToeUnavailable;
            return heelAccepted
                ? CharacterFootCurrentSupportRejectReason.ToeUnavailable
                : CharacterFootCurrentSupportRejectReason.HeelUnavailable;
        }

        static CharacterFootCurrentSupportProbeKind SelectProbe(
            float heelDisplacement,
            float toeDisplacement,
            int heelSurfaceIdentity,
            int toeSurfaceIdentity,
            out CharacterFootCurrentSupportSelectionReason reason)
        {
            float displacement = heelDisplacement - toeDisplacement;
            if (displacement > 0.0001f)
            {
                reason = CharacterFootCurrentSupportSelectionReason
                    .HeelHigherRequiredDisplacement;
                return CharacterFootCurrentSupportProbeKind.Heel;
            }
            if (displacement < -0.0001f)
            {
                reason = CharacterFootCurrentSupportSelectionReason
                    .ToeHigherRequiredDisplacement;
                return CharacterFootCurrentSupportProbeKind.Toe;
            }
            int identity = heelSurfaceIdentity.CompareTo(toeSurfaceIdentity);
            reason = identity == 0
                ? CharacterFootCurrentSupportSelectionReason
                    .EquivalentDisplacementHeelOrder
                : CharacterFootCurrentSupportSelectionReason
                    .EquivalentDisplacementSurfaceIdentity;
            return identity <= 0
                ? CharacterFootCurrentSupportProbeKind.Heel
                : CharacterFootCurrentSupportProbeKind.Toe;
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    public readonly struct CharacterFootCurrentSupportProbeDiagnostics
    {
        internal CharacterFootCurrentSupportProbeDiagnostics(
            in CharacterFootCurrentSupportProbeRequest request,
            in CharacterFootCurrentSupportProbeResult result)
        {
            Purpose = request.Purpose;
            Kind = request.Kind;
            State = result.State;
            RejectReason = result.RejectReason;
            ProbePosition = request.ProbePosition;
            ComponentUp = request.ComponentUp;
            Origin = request.Origin;
            Direction = request.Direction;
            MaximumDistance = request.MaximumDistance;
            SupportMaximumDistance = request.SupportMaximumDistance;
            Radius = request.Radius;
            LayerMask = request.LayerMask;
            MinimumGroundNormalDot = request.MinimumGroundNormalDot;
            HitCapacity = request.HitCapacity;
            CandidateCount = result.CandidateCount;
            Coverage = result.Coverage;
            SurfaceIdentity = result.SurfaceIdentity;
            Point = result.Point;
            Normal = result.Normal;
            Distance = result.Distance;
            WorldRevision = result.WorldRevision;
            SphereCastExecuted = result.SphereCastExecuted;
            Accepted = result.Accepted;
        }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public CharacterFootPlacementQueryPurpose Purpose { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public CharacterFootCurrentSupportProbeKind Kind { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public CharacterFootCurrentSupportProbeState State { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public CharacterFootCurrentSupportProbeRejectReason RejectReason { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public Vector3 ProbePosition { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public Vector3 ComponentUp { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public Vector3 Origin { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public Vector3 Direction { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public float MaximumDistance { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public float SupportMaximumDistance { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public float Radius { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public int LayerMask { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public float MinimumGroundNormalDot { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public int HitCapacity { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public int CandidateCount { get; }

        public CharacterFootSupportQueryDiagnostics Coverage { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public int SurfaceIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public Vector3 Point { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public Vector3 Normal { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public float Distance { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public ulong WorldRevision { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public bool SphereCastExecuted { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support-probe")]
        public bool Accepted { get; }
    }

    public readonly struct CharacterFootCurrentSupportDiagnostics
    {
        internal CharacterFootCurrentSupportDiagnostics(
            in CharacterFootCurrentSupportObservation observation)
        {
            FrameSequence = observation.FrameSequence;
            CompletionIdentity = observation.CompletionIdentity;
            Side = observation.Side;
            WorldRevision = observation.WorldRevision;
            IsSpecified = observation.IsSpecified;
            Available = observation.Available;
            RejectReason = observation.RejectReason;
            ref readonly CharacterFootCurrentSupportProbeRequest heelRequest =
                ref observation.HeelRequest;
            ref readonly CharacterFootCurrentSupportProbeResult heel =
                ref observation.Heel;
            Heel = new CharacterFootCurrentSupportProbeDiagnostics(
                in heelRequest,
                in heel);
            ref readonly CharacterFootCurrentSupportProbeRequest toeRequest =
                ref observation.ToeRequest;
            ref readonly CharacterFootCurrentSupportProbeResult toe =
                ref observation.Toe;
            Toe = new CharacterFootCurrentSupportProbeDiagnostics(
                in toeRequest,
                in toe);
            HeelRequiredDisplacement = observation.HeelRequiredDisplacement;
            ToeRequiredDisplacement = observation.ToeRequiredDisplacement;
            SelectedProbe = observation.SelectedProbe;
            SelectionReason = observation.SelectionReason;
            SelectionEpsilon = observation.SelectionEpsilon;
            SelectedSupportNormalBeforeNormalization =
                observation.SelectedSupportNormalBeforeNormalization;
            ref readonly CharacterFootSupportTarget target =
                ref observation.Target;
            Target = new CharacterFootSupportTargetDiagnostics(
                in target);
        }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public ulong FrameSequence { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public ulong CompletionIdentity { get; }

        public CharacterFootSide Side { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public ulong WorldRevision { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public bool IsSpecified { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public bool Available { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public CharacterFootCurrentSupportRejectReason RejectReason { get; }
        public CharacterFootCurrentSupportProbeDiagnostics Heel { get; }
        public CharacterFootCurrentSupportProbeDiagnostics Toe { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public float HeelRequiredDisplacement { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public float ToeRequiredDisplacement { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public CharacterFootCurrentSupportProbeKind SelectedProbe { get; }
        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public CharacterFootCurrentSupportSelectionReason SelectionReason
        {
            get;
        }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public float SelectionEpsilon { get; }

        [DiagnosticField]
        [DiagnosticGroup("current-support")]
        public Vector3 SelectedSupportNormalBeforeNormalization { get; }
        public CharacterFootSupportTargetDiagnostics Target { get; }
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
        readonly CharacterFootLandingPredictionSettings m_LandingSettings;

        internal CharacterFootSoleSupportQuery(
            ICharacterFootCurrentSupportWorldQuery world,
            in CharacterFootCurrentSupportQuerySettings settings,
            in CharacterFootLandingPredictionSettings landingSettings)
        {
            m_World = world;
            m_Settings = settings;
            m_LandingSettings = landingSettings;
        }

        internal CharacterFootCurrentSupportObservation Query(
            in CharacterFootStateFrame frame,
            in CharacterFootPlacementSoleContactPose contacts)
        {
            var heelRequest = CharacterFootCurrentSupportProbeRequest.Create(
                frame.Side, CharacterFootCurrentSupportProbeKind.Heel,
                contacts.HeelPosition, frame.ComponentUp, m_Settings, m_LandingSettings);
            var toeRequest = CharacterFootCurrentSupportProbeRequest.Create(
                frame.Side, CharacterFootCurrentSupportProbeKind.Toe,
                contacts.ToePosition, frame.ComponentUp, m_Settings, m_LandingSettings);
            CharacterFootCurrentSupportProbeResult heel = m_World.Query(in heelRequest);
            CharacterFootCurrentSupportProbeResult toe = m_World.Query(in toeRequest);
            return CharacterFootCurrentSupportObservation.Resolve(
                frame.FrameSequence, frame.CompletionIdentity, frame.WorldRevision,
                in heelRequest, in toeRequest, in heel, in toe);
        }
    }

}
