using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootCurrentSupportProbeKind : byte
    {
        Base = 1,
        Rear = 2,
        PositiveLateral = 3,
        NegativeLateral = 4,
        Toe = 5
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
        BaseUnavailable = 1,
        ToeUnavailable = 2,
        BaseAndToeUnavailable = 3,
        InvalidSupportNormal = 4,
        NotGrounded = 5,
        WorldRevisionMismatch = 6,
        CapacityExceeded = 7
    }

    public enum CharacterFootCurrentSupportSelectionReason : byte
    {
        None = 0,
        HigherAlongComponentUp = 1,
        EquivalentHeightSurfaceIdentity = 2,
        EquivalentHeightProbeOrder = 3
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
        internal bool IsValid =>
            (Side == CharacterFootSide.Left || Side == CharacterFootSide.Right) &&
            Kind >= CharacterFootCurrentSupportProbeKind.Base &&
            Kind <= CharacterFootCurrentSupportProbeKind.Toe &&
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
            bool sphereCastExecuted) =>
            new CharacterFootCurrentSupportProbeResult(
                kind,
                CharacterFootCurrentSupportProbeState.Rejected,
                reason,
                0,
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
        RetainedContactAnchor = 3
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
            ulong normalEventIdentity,
            CharacterFootCurrentSupportProbeKind currentSupportProbeKind)
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
                normalCompletionIdentity == 0 ||
                (positionSource == CharacterFootSupportPositionSource.CurrentSupport ||
                 normalSource == CharacterFootSupportNormalSource.CurrentSupport) &&
                (currentSupportProbeKind <
                     CharacterFootCurrentSupportProbeKind.Base ||
                 currentSupportProbeKind >
                     CharacterFootCurrentSupportProbeKind.Toe) ||
                positionSource != CharacterFootSupportPositionSource.CurrentSupport &&
                normalSource != CharacterFootSupportNormalSource.CurrentSupport &&
                currentSupportProbeKind != 0)
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
            CurrentSupportProbeKind = currentSupportProbeKind;
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
        internal CharacterFootCurrentSupportProbeKind CurrentSupportProbeKind { get; }
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
                NormalEventIdentity,
                CurrentSupportProbeKind);

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
            CurrentSupportProbeKind = target.CurrentSupportProbeKind;
        }

        public bool Available { get; }
        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public CharacterFootSide Side { get; }
        public Vector3 Position { get; }
        public Vector3 SupportNormal { get; }
        public int SurfaceIdentity { get; }
        public ulong WorldRevision { get; }
        public CharacterFootSupportTargetKind Kind { get; }
        public CharacterFootSupportPositionSource PositionSource { get; }
        public ulong PositionFrameSequence { get; }
        public ulong PositionCompletionIdentity { get; }
        public ulong PositionEventIdentity { get; }
        public ulong PositionPathIdentity { get; }
        public CharacterFootSupportNormalSource NormalSource { get; }
        public ulong NormalFrameSequence { get; }
        public ulong NormalCompletionIdentity { get; }
        public ulong NormalEventIdentity { get; }
        public CharacterFootCurrentSupportProbeKind CurrentSupportProbeKind { get; }
    }

    internal readonly struct CharacterFootCurrentSupportCandidate
    {
        internal CharacterFootCurrentSupportCandidate(
            CharacterFootCurrentSupportProbeKind kind,
            Vector3 solePosition,
            float heightAlongUp,
            Vector3 direction,
            int surfaceIdentity,
            ulong worldRevision)
        {
            Kind = kind;
            SolePosition = solePosition;
            HeightAlongUp = heightAlongUp;
            Direction = direction;
            SurfaceIdentity = surfaceIdentity;
            WorldRevision = worldRevision;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal CharacterFootCurrentSupportProbeKind Kind { get; }
        internal Vector3 SolePosition { get; }
        internal float HeightAlongUp { get; }
        internal Vector3 Direction { get; }
        internal int SurfaceIdentity { get; }
        internal ulong WorldRevision { get; }
        internal bool Available => m_IsSpecified != 0;

        internal static CharacterFootCurrentSupportCandidate Resolve(
            Vector3 originalSole,
            Vector3 componentUp,
            in CharacterFootCurrentSupportProbeRequest request,
            in CharacterFootCurrentSupportProbeResult result)
        {
            if (!result.Accepted)
                return default;
            Vector3 direction = result.Normal.normalized;
            Vector3 translation = direction * Vector3.Dot(
                result.Point - request.ProbePosition,
                direction);
            Vector3 solePosition = originalSole + translation;
            return new CharacterFootCurrentSupportCandidate(
                request.Kind,
                solePosition,
                Vector3.Dot(solePosition, componentUp.normalized),
                direction,
                result.SurfaceIdentity,
                result.WorldRevision);
        }
    }

    internal readonly struct CharacterFootCurrentSupportObservation
    {
        internal CharacterFootCurrentSupportObservation(
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootSide side,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest baseRequest,
            in CharacterFootCurrentSupportProbeRequest rearRequest,
            in CharacterFootCurrentSupportProbeRequest positiveLateralRequest,
            in CharacterFootCurrentSupportProbeRequest negativeLateralRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            in CharacterFootCurrentSupportProbeResult baseResult,
            in CharacterFootCurrentSupportProbeResult rearResult,
            in CharacterFootCurrentSupportProbeResult positiveLateralResult,
            in CharacterFootCurrentSupportProbeResult negativeLateralResult,
            in CharacterFootCurrentSupportProbeResult toeResult,
            in CharacterFootCurrentSupportCandidate baseCandidate,
            in CharacterFootCurrentSupportCandidate rearCandidate,
            in CharacterFootCurrentSupportCandidate positiveLateralCandidate,
            in CharacterFootCurrentSupportCandidate negativeLateralCandidate,
            in CharacterFootCurrentSupportCandidate toeCandidate,
            CharacterFootCurrentSupportRejectReason rejectReason,
            CharacterFootCurrentSupportProbeKind selectedProbe,
            CharacterFootCurrentSupportSelectionReason selectionReason,
            Vector3 selectedDirectionBeforeNormalization,
            in CharacterFootSupportTarget target)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            Side = side;
            WorldRevision = worldRevision;
            BaseRequest = baseRequest;
            RearRequest = rearRequest;
            PositiveLateralRequest = positiveLateralRequest;
            NegativeLateralRequest = negativeLateralRequest;
            ToeRequest = toeRequest;
            BaseResult = baseResult;
            RearResult = rearResult;
            PositiveLateralResult = positiveLateralResult;
            NegativeLateralResult = negativeLateralResult;
            ToeResult = toeResult;
            BaseCandidate = baseCandidate;
            RearCandidate = rearCandidate;
            PositiveLateralCandidate = positiveLateralCandidate;
            NegativeLateralCandidate = negativeLateralCandidate;
            ToeCandidate = toeCandidate;
            RejectReason = rejectReason;
            SelectedProbe = selectedProbe;
            SelectionReason = selectionReason;
            SelectedDirectionBeforeNormalization = selectedDirectionBeforeNormalization;
            Target = target;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal CharacterFootSide Side { get; }
        internal ulong WorldRevision { get; }
        internal CharacterFootCurrentSupportProbeRequest BaseRequest { get; }
        internal CharacterFootCurrentSupportProbeRequest RearRequest { get; }
        internal CharacterFootCurrentSupportProbeRequest PositiveLateralRequest { get; }
        internal CharacterFootCurrentSupportProbeRequest NegativeLateralRequest { get; }
        internal CharacterFootCurrentSupportProbeRequest ToeRequest { get; }
        internal CharacterFootCurrentSupportProbeResult BaseResult { get; }
        internal CharacterFootCurrentSupportProbeResult RearResult { get; }
        internal CharacterFootCurrentSupportProbeResult PositiveLateralResult { get; }
        internal CharacterFootCurrentSupportProbeResult NegativeLateralResult { get; }
        internal CharacterFootCurrentSupportProbeResult ToeResult { get; }
        internal CharacterFootCurrentSupportCandidate BaseCandidate { get; }
        internal CharacterFootCurrentSupportCandidate RearCandidate { get; }
        internal CharacterFootCurrentSupportCandidate PositiveLateralCandidate { get; }
        internal CharacterFootCurrentSupportCandidate NegativeLateralCandidate { get; }
        internal CharacterFootCurrentSupportCandidate ToeCandidate { get; }
        internal CharacterFootCurrentSupportRejectReason RejectReason { get; }
        internal CharacterFootCurrentSupportProbeKind SelectedProbe { get; }
        internal CharacterFootCurrentSupportSelectionReason SelectionReason { get; }
        internal float SelectionEpsilon => 0.0001f;
        internal Vector3 SelectedDirectionBeforeNormalization { get; }
        internal CharacterFootSupportTarget Target { get; }
        internal bool IsSpecified => m_IsSpecified != 0;
        internal bool Available =>
            IsSpecified && RejectReason == CharacterFootCurrentSupportRejectReason.None &&
            Target.IsValid;

        internal static CharacterFootCurrentSupportObservation Resolve(
            ulong frameSequence,
            ulong completionIdentity,
            ulong worldRevision,
            Vector3 originalSole,
            in CharacterFootCurrentSupportProbeRequest baseRequest,
            in CharacterFootCurrentSupportProbeRequest rearRequest,
            in CharacterFootCurrentSupportProbeRequest positiveLateralRequest,
            in CharacterFootCurrentSupportProbeRequest negativeLateralRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            in CharacterFootCurrentSupportProbeResult baseResult,
            in CharacterFootCurrentSupportProbeResult rearResult,
            in CharacterFootCurrentSupportProbeResult positiveLateralResult,
            in CharacterFootCurrentSupportProbeResult negativeLateralResult,
            in CharacterFootCurrentSupportProbeResult toeResult)
        {
            CharacterFootSide side = baseRequest.Side;
            if (!WorldMatches(
                    worldRevision,
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult))
            {
                return Rejected(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in baseRequest,
                    in rearRequest,
                    in positiveLateralRequest,
                    in negativeLateralRequest,
                    in toeRequest,
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult,
                    CharacterFootCurrentSupportRejectReason.WorldRevisionMismatch);
            }
            if (CapacityExceeded(
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult))
            {
                return Rejected(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in baseRequest,
                    in rearRequest,
                    in positiveLateralRequest,
                    in negativeLateralRequest,
                    in toeRequest,
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult,
                    CharacterFootCurrentSupportRejectReason.CapacityExceeded);
            }
            CharacterFootCurrentSupportRejectReason requiredRejectReason =
                ResolveRequiredRejectReason(baseResult.Accepted, toeResult.Accepted);
            if (requiredRejectReason != CharacterFootCurrentSupportRejectReason.None)
            {
                return Rejected(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in baseRequest,
                    in rearRequest,
                    in positiveLateralRequest,
                    in negativeLateralRequest,
                    in toeRequest,
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult,
                    requiredRejectReason);
            }
            Vector3 up = baseRequest.ComponentUp.normalized;
            CharacterFootCurrentSupportCandidate baseCandidate =
                CharacterFootCurrentSupportCandidate.Resolve(
                    originalSole,
                    up,
                    in baseRequest,
                    in baseResult);
            CharacterFootCurrentSupportCandidate rearCandidate =
                CharacterFootCurrentSupportCandidate.Resolve(
                    originalSole,
                    up,
                    in rearRequest,
                    in rearResult);
            CharacterFootCurrentSupportCandidate positiveLateralCandidate =
                CharacterFootCurrentSupportCandidate.Resolve(
                    originalSole,
                    up,
                    in positiveLateralRequest,
                    in positiveLateralResult);
            CharacterFootCurrentSupportCandidate negativeLateralCandidate =
                CharacterFootCurrentSupportCandidate.Resolve(
                    originalSole,
                    up,
                    in negativeLateralRequest,
                    in negativeLateralResult);
            CharacterFootCurrentSupportCandidate toeCandidate =
                CharacterFootCurrentSupportCandidate.Resolve(
                    originalSole,
                    up,
                    in toeRequest,
                    in toeResult);
            if (!baseCandidate.Available || !toeCandidate.Available)
            {
                return Rejected(
                    frameSequence,
                    completionIdentity,
                    side,
                    worldRevision,
                    in baseRequest,
                    in rearRequest,
                    in positiveLateralRequest,
                    in negativeLateralRequest,
                    in toeRequest,
                    in baseResult,
                    in rearResult,
                    in positiveLateralResult,
                    in negativeLateralResult,
                    in toeResult,
                    CharacterFootCurrentSupportRejectReason.InvalidSupportNormal);
            }
            CharacterFootCurrentSupportCandidate selected = baseCandidate;
            CharacterFootCurrentSupportSelectionReason selectionReason =
                CharacterFootCurrentSupportSelectionReason.None;
            SelectCandidate(ref selected, in rearCandidate, ref selectionReason);
            SelectCandidate(ref selected, in positiveLateralCandidate, ref selectionReason);
            SelectCandidate(ref selected, in negativeLateralCandidate, ref selectionReason);
            SelectCandidate(ref selected, in toeCandidate, ref selectionReason);
            var target = new CharacterFootSupportTarget(
                frameSequence,
                completionIdentity,
                side,
                selected.SolePosition,
                selected.Direction,
                selected.SurfaceIdentity,
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
                0,
                selected.Kind);
            return new CharacterFootCurrentSupportObservation(
                frameSequence,
                completionIdentity,
                side,
                worldRevision,
                in baseRequest,
                in rearRequest,
                in positiveLateralRequest,
                in negativeLateralRequest,
                in toeRequest,
                in baseResult,
                in rearResult,
                in positiveLateralResult,
                in negativeLateralResult,
                in toeResult,
                in baseCandidate,
                in rearCandidate,
                in positiveLateralCandidate,
                in negativeLateralCandidate,
                in toeCandidate,
                CharacterFootCurrentSupportRejectReason.None,
                selected.Kind,
                selectionReason,
                selected.Direction,
                in target);
        }

        internal static CharacterFootCurrentSupportObservation Unavailable(
            ulong frameSequence,
            ulong completionIdentity,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest baseRequest,
            in CharacterFootCurrentSupportProbeRequest rearRequest,
            in CharacterFootCurrentSupportProbeRequest positiveLateralRequest,
            in CharacterFootCurrentSupportProbeRequest negativeLateralRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            CharacterFootCurrentSupportRejectReason reason)
        {
            if (reason == CharacterFootCurrentSupportRejectReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            CharacterFootCurrentSupportProbeResult baseResult = NotExecuted(
                CharacterFootCurrentSupportProbeKind.Base,
                worldRevision);
            CharacterFootCurrentSupportProbeResult rearResult = NotExecuted(
                CharacterFootCurrentSupportProbeKind.Rear,
                worldRevision);
            CharacterFootCurrentSupportProbeResult positiveLateralResult = NotExecuted(
                CharacterFootCurrentSupportProbeKind.PositiveLateral,
                worldRevision);
            CharacterFootCurrentSupportProbeResult negativeLateralResult = NotExecuted(
                CharacterFootCurrentSupportProbeKind.NegativeLateral,
                worldRevision);
            CharacterFootCurrentSupportProbeResult toeResult = NotExecuted(
                CharacterFootCurrentSupportProbeKind.Toe,
                worldRevision);
            return Rejected(
                frameSequence,
                completionIdentity,
                baseRequest.Side,
                worldRevision,
                in baseRequest,
                in rearRequest,
                in positiveLateralRequest,
                in negativeLateralRequest,
                in toeRequest,
                in baseResult,
                in rearResult,
                in positiveLateralResult,
                in negativeLateralResult,
                in toeResult,
                reason);
        }

        static CharacterFootCurrentSupportObservation Rejected(
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootSide side,
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeRequest baseRequest,
            in CharacterFootCurrentSupportProbeRequest rearRequest,
            in CharacterFootCurrentSupportProbeRequest positiveLateralRequest,
            in CharacterFootCurrentSupportProbeRequest negativeLateralRequest,
            in CharacterFootCurrentSupportProbeRequest toeRequest,
            in CharacterFootCurrentSupportProbeResult baseResult,
            in CharacterFootCurrentSupportProbeResult rearResult,
            in CharacterFootCurrentSupportProbeResult positiveLateralResult,
            in CharacterFootCurrentSupportProbeResult negativeLateralResult,
            in CharacterFootCurrentSupportProbeResult toeResult,
            CharacterFootCurrentSupportRejectReason reason) =>
            new CharacterFootCurrentSupportObservation(
                frameSequence,
                completionIdentity,
                side,
                worldRevision,
                in baseRequest,
                in rearRequest,
                in positiveLateralRequest,
                in negativeLateralRequest,
                in toeRequest,
                in baseResult,
                in rearResult,
                in positiveLateralResult,
                in negativeLateralResult,
                in toeResult,
                default,
                default,
                default,
                default,
                default,
                reason,
                default,
                CharacterFootCurrentSupportSelectionReason.None,
                default,
                default);

        static CharacterFootCurrentSupportProbeResult NotExecuted(
            CharacterFootCurrentSupportProbeKind kind,
            ulong worldRevision) =>
            CharacterFootCurrentSupportProbeResult.NotExecuted(
                kind,
                CharacterFootCurrentSupportProbeRejectReason.NotGrounded,
                worldRevision);

        static CharacterFootCurrentSupportRejectReason ResolveRequiredRejectReason(
            bool baseAccepted,
            bool toeAccepted)
        {
            if (baseAccepted && toeAccepted)
                return CharacterFootCurrentSupportRejectReason.None;
            if (!baseAccepted && !toeAccepted)
                return CharacterFootCurrentSupportRejectReason.BaseAndToeUnavailable;
            return baseAccepted
                ? CharacterFootCurrentSupportRejectReason.ToeUnavailable
                : CharacterFootCurrentSupportRejectReason.BaseUnavailable;
        }

        static bool WorldMatches(
            ulong worldRevision,
            in CharacterFootCurrentSupportProbeResult baseResult,
            in CharacterFootCurrentSupportProbeResult rearResult,
            in CharacterFootCurrentSupportProbeResult positiveLateralResult,
            in CharacterFootCurrentSupportProbeResult negativeLateralResult,
            in CharacterFootCurrentSupportProbeResult toeResult) =>
            worldRevision != 0 &&
            baseResult.WorldRevision == worldRevision &&
            rearResult.WorldRevision == worldRevision &&
            positiveLateralResult.WorldRevision == worldRevision &&
            negativeLateralResult.WorldRevision == worldRevision &&
            toeResult.WorldRevision == worldRevision;

        static bool CapacityExceeded(
            in CharacterFootCurrentSupportProbeResult baseResult,
            in CharacterFootCurrentSupportProbeResult rearResult,
            in CharacterFootCurrentSupportProbeResult positiveLateralResult,
            in CharacterFootCurrentSupportProbeResult negativeLateralResult,
            in CharacterFootCurrentSupportProbeResult toeResult) =>
            IsCapacityExceeded(in baseResult) ||
            IsCapacityExceeded(in rearResult) ||
            IsCapacityExceeded(in positiveLateralResult) ||
            IsCapacityExceeded(in negativeLateralResult) ||
            IsCapacityExceeded(in toeResult);

        static bool IsCapacityExceeded(
            in CharacterFootCurrentSupportProbeResult result) =>
            result.RejectReason ==
            CharacterFootCurrentSupportProbeRejectReason.CapacityExceeded;

        static void SelectCandidate(
            ref CharacterFootCurrentSupportCandidate selected,
            in CharacterFootCurrentSupportCandidate candidate,
            ref CharacterFootCurrentSupportSelectionReason reason)
        {
            if (!candidate.Available)
                return;
            float delta = candidate.HeightAlongUp - selected.HeightAlongUp;
            if (delta > 0.0001f)
            {
                selected = candidate;
                reason = CharacterFootCurrentSupportSelectionReason.HigherAlongComponentUp;
                return;
            }
            if (delta < -0.0001f)
                return;
            int identity = candidate.SurfaceIdentity.CompareTo(
                selected.SurfaceIdentity);
            if (identity < 0)
            {
                selected = candidate;
                reason = CharacterFootCurrentSupportSelectionReason
                    .EquivalentHeightSurfaceIdentity;
                return;
            }
            if (identity == 0 && candidate.Kind < selected.Kind)
            {
                selected = candidate;
                reason = CharacterFootCurrentSupportSelectionReason
                    .EquivalentHeightProbeOrder;
            }
        }
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
            Radius = request.Radius;
            LayerMask = request.LayerMask;
            MinimumGroundNormalDot = request.MinimumGroundNormalDot;
            HitCapacity = request.HitCapacity;
            CandidateCount = result.CandidateCount;
            SurfaceIdentity = result.SurfaceIdentity;
            Point = result.Point;
            Normal = result.Normal;
            Distance = result.Distance;
            WorldRevision = result.WorldRevision;
            SphereCastExecuted = result.SphereCastExecuted;
            Accepted = result.Accepted;
        }

        public CharacterFootPlacementQueryPurpose Purpose { get; }
        public CharacterFootCurrentSupportProbeKind Kind { get; }
        public CharacterFootCurrentSupportProbeState State { get; }
        public CharacterFootCurrentSupportProbeRejectReason RejectReason { get; }
        public Vector3 ProbePosition { get; }
        public Vector3 ComponentUp { get; }
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }
        public float MaximumDistance { get; }
        public float Radius { get; }
        public int LayerMask { get; }
        public float MinimumGroundNormalDot { get; }
        public int HitCapacity { get; }
        public int CandidateCount { get; }
        public int SurfaceIdentity { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public ulong WorldRevision { get; }
        public bool SphereCastExecuted { get; }
        public bool Accepted { get; }
    }

    public readonly struct CharacterFootCurrentSupportCandidateDiagnostics
    {
        internal CharacterFootCurrentSupportCandidateDiagnostics(
            in CharacterFootCurrentSupportCandidate candidate)
        {
            Available = candidate.Available;
            Kind = candidate.Kind;
            SolePosition = candidate.SolePosition;
            HeightAlongUp = candidate.HeightAlongUp;
            Direction = candidate.Direction;
            SurfaceIdentity = candidate.SurfaceIdentity;
            WorldRevision = candidate.WorldRevision;
        }

        public bool Available { get; }
        public CharacterFootCurrentSupportProbeKind Kind { get; }
        public Vector3 SolePosition { get; }
        public float HeightAlongUp { get; }
        public Vector3 Direction { get; }
        public int SurfaceIdentity { get; }
        public ulong WorldRevision { get; }
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
            Base = Probe(
                observation.BaseRequest,
                observation.BaseResult);
            Rear = Probe(
                observation.RearRequest,
                observation.RearResult);
            PositiveLateral = Probe(
                observation.PositiveLateralRequest,
                observation.PositiveLateralResult);
            NegativeLateral = Probe(
                observation.NegativeLateralRequest,
                observation.NegativeLateralResult);
            Toe = Probe(
                observation.ToeRequest,
                observation.ToeResult);
            BaseCandidate = Candidate(observation.BaseCandidate);
            RearCandidate = Candidate(observation.RearCandidate);
            PositiveLateralCandidate = Candidate(
                observation.PositiveLateralCandidate);
            NegativeLateralCandidate = Candidate(
                observation.NegativeLateralCandidate);
            ToeCandidate = Candidate(observation.ToeCandidate);
            SelectedProbe = observation.SelectedProbe;
            SelectionReason = observation.SelectionReason;
            SelectionEpsilon = observation.SelectionEpsilon;
            SelectedDirectionBeforeNormalization =
                observation.SelectedDirectionBeforeNormalization;
            CharacterFootSupportTarget target = observation.Target;
            Target = new CharacterFootSupportTargetDiagnostics(
                in target);
        }

        static CharacterFootCurrentSupportProbeDiagnostics Probe(
            CharacterFootCurrentSupportProbeRequest request,
            CharacterFootCurrentSupportProbeResult result) =>
            new CharacterFootCurrentSupportProbeDiagnostics(
                in request,
                in result);

        static CharacterFootCurrentSupportCandidateDiagnostics Candidate(
            CharacterFootCurrentSupportCandidate candidate) =>
            new CharacterFootCurrentSupportCandidateDiagnostics(in candidate);

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public CharacterFootSide Side { get; }
        public ulong WorldRevision { get; }
        public bool IsSpecified { get; }
        public bool Available { get; }
        public CharacterFootCurrentSupportRejectReason RejectReason { get; }
        public CharacterFootCurrentSupportProbeDiagnostics Base { get; }
        public CharacterFootCurrentSupportProbeDiagnostics Rear { get; }
        public CharacterFootCurrentSupportProbeDiagnostics PositiveLateral { get; }
        public CharacterFootCurrentSupportProbeDiagnostics NegativeLateral { get; }
        public CharacterFootCurrentSupportProbeDiagnostics Toe { get; }
        public CharacterFootCurrentSupportCandidateDiagnostics BaseCandidate { get; }
        public CharacterFootCurrentSupportCandidateDiagnostics RearCandidate { get; }
        public CharacterFootCurrentSupportCandidateDiagnostics PositiveLateralCandidate { get; }
        public CharacterFootCurrentSupportCandidateDiagnostics NegativeLateralCandidate { get; }
        public CharacterFootCurrentSupportCandidateDiagnostics ToeCandidate { get; }
        public CharacterFootCurrentSupportProbeKind SelectedProbe { get; }
        public CharacterFootCurrentSupportSelectionReason SelectionReason
        {
            get;
        }
        public float SelectionEpsilon { get; }
        public Vector3 SelectedDirectionBeforeNormalization { get; }
        public CharacterFootSupportTargetDiagnostics Target { get; }
    }

    internal interface ICharacterFootCurrentSupportWorldQuery
    {
        CharacterFootCurrentSupportProbeResult Query(
            in CharacterFootCurrentSupportProbeRequest request);
    }

    internal sealed class CharacterFootCurrentSupportObservationPage
    {
        internal bool HasValue { get; private set; }
        internal CharacterFootCurrentSupportObservation Observation { get; private set; }

        internal void Set(in CharacterFootCurrentSupportObservation observation)
        {
            if (!observation.IsSpecified)
                throw new ArgumentException("Current Support observation is invalid.");
            HasValue = true;
            Observation = observation;
        }

        internal void Clear()
        {
            HasValue = false;
            Observation = default;
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
}
