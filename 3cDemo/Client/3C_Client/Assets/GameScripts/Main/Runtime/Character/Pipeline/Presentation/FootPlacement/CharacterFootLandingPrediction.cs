using System;
using System.Collections.Generic;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootLandingPredictionState : byte
    {
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootLandingPredictionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepIdentityMismatch = 2,
        LandingTimeInvalid = 3,
        MotionTimelineUnavailable = 4,
        FutureBodyTranslationUnavailable = 5,
        FutureBodyTranslationRangeInvalid = 6,
        GroundQueryMissed = 7,
        GroundQueryCapacityExceeded = 8
    }

    public enum CharacterFootLandingStepSource : byte
    {
        None = 0,
        FormalCurrentContact = 1,
        FormalNextLanding = 2
    }

    internal readonly struct CharacterFootLandingSupport
    {
        internal CharacterFootLandingSupport(
            int surfaceIdentity,
            Vector3 point,
            Vector3 normal,
            float distance)
        {
            if (surfaceIdentity == 0 || !Finite(point) || !Finite(normal) ||
                normal.sqrMagnitude <= 0.000001f ||
                !float.IsFinite(distance) || distance < 0f)
            {
                throw new ArgumentException("Foot Landing support is invalid.");
            }
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal.normalized;
            Distance = distance;
        }

        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal enum CharacterFootLandingQueryRejectReason : byte
    {
        None = 0,
        InvalidRequest = 1,
        NoHit = 2,
        CapacityExceeded = 3
    }

    public enum CharacterFootLandingQueryCandidateSelectionState : byte
    {
        NotExecuted = 0,
        InvalidRequest = 1,
        NoCandidate = 2,
        CapacityExceeded = 3,
        Selected = 4
    }

    public readonly struct CharacterFootLandingQueryCandidateDiagnostics
    {
        internal CharacterFootLandingQueryCandidateDiagnostics(
            int surfaceIdentity,
            Vector3 point,
            float distance)
        {
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Distance = distance;
        }

        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(IsAvailable))]
        public int SurfaceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(IsAvailable))]
        public Vector3 Point { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(IsAvailable))]
        public float Distance { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public bool IsAvailable => SurfaceIdentity != 0;
    }

    public readonly struct CharacterFootLandingQuerySelectionDiagnostics
    {
        internal CharacterFootLandingQuerySelectionDiagnostics(
            CharacterFootLandingQueryCandidateSelectionState state,
            int validCandidateCount,
            CharacterFootLandingQueryCandidateDiagnostics selected,
            CharacterFootSupportQueryDiagnostics coverage)
        {
            State = state;
            ValidCandidateCount = validCandidateCount;
            Selected = selected;
            Coverage = coverage;
        }

        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootLandingQueryCandidateSelectionState State { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public int ValidCandidateCount { get; }
        public CharacterFootLandingQueryCandidateDiagnostics Selected { get; }
        public CharacterFootSupportQueryDiagnostics Coverage { get; }
    }

    internal readonly struct CharacterFootLandingQueryResult
    {
        readonly CharacterFootLandingSupport m_Support;
        readonly CharacterFootLandingQuerySelectionDiagnostics m_SelectionDiagnostics;

        internal CharacterFootLandingQueryResult(
            CharacterFootLandingQueryRejectReason rejectReason,
            CharacterFootLandingSupport support,
            CharacterFootLandingQuerySelectionDiagnostics selectionDiagnostics)
        {
            RejectReason = rejectReason;
            m_Support = support;
            m_SelectionDiagnostics = selectionDiagnostics;
        }

        internal CharacterFootLandingQueryRejectReason RejectReason { get; }
        internal ref readonly CharacterFootLandingSupport Support =>
            ref m_Support;
        internal ref readonly CharacterFootLandingQuerySelectionDiagnostics
            SelectionDiagnostics => ref m_SelectionDiagnostics;
        internal bool Accepted => RejectReason == CharacterFootLandingQueryRejectReason.None;
    }

    internal interface ICharacterFootLandingWorldQuery
    {
        ulong WorldRevision { get; }

        CharacterFootLandingQueryResult Query(
            in CharacterFootPlacementQueryRequest request);
    }

    public enum CharacterFootLandingObservationCacheState : byte
    {
        Unavailable = 0,
        Queried = 1,
        Reused = 2
    }

    public enum CharacterFootLandingObservationRefreshMode : byte
    {
        Thresholded = 1,
        ChangedContactApproachInput = 2,
        ForcedPlantVerification = 3
    }

    [Flags]
    public enum CharacterFootLandingObservationQueryReason : ushort
    {
        None = 0,
        PageUnavailable = 1 << 0,
        SideChanged = 1 << 1,
        LandingEventChanged = 1 << 2,
        SourceChanged = 1 << 3,
        SourceCycleChanged = 1 << 4,
        ProfileRevisionChanged = 1 << 5,
        WorldRevisionChanged = 1 << 6,
        PredictionInputDistanceExceeded = 1 << 7,
        ComponentUpAngleExceeded = 1 << 8,
        ContactAcquisitionRefresh = 1 << 9,
        QueryPurposeChanged = 1 << 10
    }

    internal readonly struct CharacterFootLandingObservationKey :
        IEquatable<CharacterFootLandingObservationKey>
    {
        const float PositionScale = 1000f;
        const float DirectionScale = 10000f;

        internal CharacterFootLandingObservationKey(
            CharacterFootSide side,
            CharacterFootPlacementQueryPurpose queryPurpose,
            ulong landingEventIdentity,
            ulong sourceSampleIdentity,
            int sourceSampleCycle,
            Vector3 rawLanding,
            Vector3 componentUp,
            string profileRevision,
            ulong worldRevision,
            Vector3 heelOffset = default,
            Vector3 toeOffset = default)
        {
            if ((side != CharacterFootSide.Left && side != CharacterFootSide.Right) ||
                (queryPurpose != CharacterFootPlacementQueryPurpose.FutureLanding &&
                 queryPurpose != CharacterFootPlacementQueryPurpose
                     .CurrentContactVerification) ||
                landingEventIdentity == 0 || sourceSampleIdentity == 0 ||
                !Finite(rawLanding) ||
                !Finite(componentUp) || componentUp.sqrMagnitude <= 0.000001f ||
                string.IsNullOrWhiteSpace(profileRevision) || worldRevision == 0)
            {
                throw new ArgumentException("Foot Landing observation key is invalid.");
            }
            Vector3 up = componentUp.normalized;
            Side = side;
            QueryPurpose = queryPurpose;
            LandingEventIdentity = landingEventIdentity;
            SourceSampleIdentity = sourceSampleIdentity;
            SourceSampleCycle = sourceSampleCycle;
            RawLandingX = Quantize(rawLanding.x, PositionScale);
            RawLandingY = Quantize(rawLanding.y, PositionScale);
            RawLandingZ = Quantize(rawLanding.z, PositionScale);
            ComponentUpX = Quantize(up.x, DirectionScale);
            ComponentUpY = Quantize(up.y, DirectionScale);
            ComponentUpZ = Quantize(up.z, DirectionScale);
            HeelOffset = new Vector3(Quantize(heelOffset.x, PositionScale),
                Quantize(heelOffset.y, PositionScale), Quantize(heelOffset.z, PositionScale)) / PositionScale;
            ToeOffset = new Vector3(Quantize(toeOffset.x, PositionScale),
                Quantize(toeOffset.y, PositionScale), Quantize(toeOffset.z, PositionScale)) / PositionScale;
            ProfileRevision = profileRevision;
            WorldRevision = worldRevision;
            Identity = 0;
            Identity = ComputeIdentity(in this);
        }

        internal CharacterFootSide Side { get; }
        internal CharacterFootPlacementQueryPurpose QueryPurpose { get; }
        internal ulong LandingEventIdentity { get; }
        internal ulong SourceSampleIdentity { get; }
        internal int SourceSampleCycle { get; }
        internal int RawLandingX { get; }
        internal int RawLandingY { get; }
        internal int RawLandingZ { get; }
        internal int ComponentUpX { get; }
        internal int ComponentUpY { get; }
        internal int ComponentUpZ { get; }
        internal string ProfileRevision { get; }
        internal ulong WorldRevision { get; }
        internal ulong Identity { get; }
        internal Vector3 HeelOffset { get; }
        internal Vector3 ToeOffset { get; }
        internal Vector3 CanonicalRawLanding => new Vector3(
            RawLandingX / PositionScale,
            RawLandingY / PositionScale,
            RawLandingZ / PositionScale);
        internal Vector3 CanonicalComponentUp => new Vector3(
                ComponentUpX / DirectionScale,
                ComponentUpY / DirectionScale,
                ComponentUpZ / DirectionScale)
            .normalized;

        public bool Equals(CharacterFootLandingObservationKey other) =>
            Side == other.Side &&
            QueryPurpose == other.QueryPurpose &&
            LandingEventIdentity == other.LandingEventIdentity &&
            SourceSampleIdentity == other.SourceSampleIdentity &&
            SourceSampleCycle == other.SourceSampleCycle &&
            RawLandingX == other.RawLandingX &&
            RawLandingY == other.RawLandingY &&
            RawLandingZ == other.RawLandingZ &&
            ComponentUpX == other.ComponentUpX &&
            ComponentUpY == other.ComponentUpY &&
            ComponentUpZ == other.ComponentUpZ &&
            HeelOffset.Equals(other.HeelOffset) && ToeOffset.Equals(other.ToeOffset) &&
            WorldRevision == other.WorldRevision &&
            string.Equals(ProfileRevision, other.ProfileRevision, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CharacterFootLandingObservationKey other && Equals(other);

        public override int GetHashCode() => Identity.GetHashCode();

        static int Quantize(float value, float scale) =>
            Mathf.RoundToInt(value * scale);

        static ulong ComputeIdentity(in CharacterFootLandingObservationKey key)
        {
            ulong hash = 14695981039346656037UL;
            Add(ref hash, (ulong)key.Side);
            Add(ref hash, (ulong)key.QueryPurpose);
            Add(ref hash, key.LandingEventIdentity);
            Add(ref hash, key.SourceSampleIdentity);
            Add(ref hash, unchecked((ulong)(uint)key.SourceSampleCycle));
            Add(ref hash, unchecked((ulong)(uint)key.RawLandingX));
            Add(ref hash, unchecked((ulong)(uint)key.RawLandingY));
            Add(ref hash, unchecked((ulong)(uint)key.RawLandingZ));
            Add(ref hash, unchecked((ulong)(uint)key.ComponentUpX));
            Add(ref hash, unchecked((ulong)(uint)key.ComponentUpY));
            Add(ref hash, unchecked((ulong)(uint)key.ComponentUpZ));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.HeelOffset.x)));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.HeelOffset.y)));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.HeelOffset.z)));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.ToeOffset.x)));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.ToeOffset.y)));
            Add(ref hash, unchecked((ulong)(uint)BitConverter.SingleToInt32Bits(key.ToeOffset.z)));
            Add(ref hash, key.ProfileRevision);
            Add(ref hash, key.WorldRevision);
            return hash != 0 ? hash : 1UL;
        }

        static void Add(ref ulong hash, ulong value)
        {
            for (int i = 0; i < 8; i++)
            {
                hash ^= (byte)value;
                hash *= 1099511628211UL;
                value >>= 8;
            }
        }

        static void Add(ref ulong hash, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    internal sealed class CharacterFootLandingObservationPage
    {
        CharacterFootLandingObservationKey m_Key;
        CharacterFootPlacementQueryRequest m_Query;
        CharacterFootLandingQueryResult m_Result;

        internal bool HasValue { get; private set; }
        internal ref readonly CharacterFootLandingObservationKey Key =>
            ref m_Key;
        internal ref readonly CharacterFootPlacementQueryRequest Query =>
            ref m_Query;
        internal ref readonly CharacterFootLandingQueryResult Result =>
            ref m_Result;

        internal void Set(
            in CharacterFootLandingObservationKey key,
            in CharacterFootPlacementQueryRequest query,
            in CharacterFootLandingQueryResult result)
        {
            HasValue = true;
            m_Key = key;
            m_Query = query;
            m_Result = result;
        }

        internal void Clear()
        {
            HasValue = false;
            m_Key = default;
            m_Query = default;
            m_Result = default;
        }
    }

    internal sealed class CharacterFootLandingObservationPagePool
    {
        readonly CharacterFootLandingObservationPage m_First = new();
        readonly CharacterFootLandingObservationPage m_Second = new();

        internal CharacterFootLandingObservationPage AcquireWritable(
            CharacterFootLandingObservationPage committed)
        {
            CharacterFootLandingObservationPage pending =
                ReferenceEquals(committed, m_First) ? m_Second : m_First;
            pending.Clear();
            return pending;
        }

        internal static CharacterFootLandingObservationPage ReuseCommitted(
            CharacterFootLandingObservationPage committed)
        {
            if (committed == null || !committed.HasValue)
                throw new InvalidOperationException("Landing Observation committed page is unavailable.");
            return committed;
        }

        internal static void Discard(
            CharacterFootLandingObservationPage pending,
            CharacterFootLandingObservationPage committed)
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

    internal readonly struct CharacterFootLandingObservationResult
    {
        internal CharacterFootLandingObservationResult(
            CharacterFootLandingObservationPage page,
            CharacterFootLandingObservationCacheState cacheState,
            CharacterFootLandingObservationQueryReason queryReason,
            CharacterFootLandingObservationRefreshMode refreshMode,
            Vector3 candidateRawLanding,
            Vector3 candidateComponentUp,
            float queryInputDistance,
            float queryComponentUpAngleDegrees,
            in CharacterFootLandingPredictionSettings settings)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            CacheState = cacheState;
            QueryReason = queryReason;
            RefreshMode = refreshMode;
            CandidateRawLanding = candidateRawLanding;
            CandidateComponentUp = candidateComponentUp.normalized;
            QueryInputDistance = queryInputDistance;
            QueryComponentUpAngleDegrees = queryComponentUpAngleDegrees;
            PredictionInputAccumulationDistance =
                settings.PredictionInputAccumulationDistance;
            ComponentUpChangeAngleDegrees =
                settings.ComponentUpChangeAngleDegrees;
        }

        internal CharacterFootLandingObservationPage Page { get; }
        internal CharacterFootLandingObservationCacheState CacheState { get; }
        internal CharacterFootLandingObservationQueryReason QueryReason { get; }
        internal CharacterFootLandingObservationRefreshMode RefreshMode { get; }
        internal Vector3 CandidateRawLanding { get; }
        internal Vector3 CandidateComponentUp { get; }
        internal float QueryInputDistance { get; }
        internal float QueryComponentUpAngleDegrees { get; }
        internal float PredictionInputAccumulationDistance { get; }
        internal float ComponentUpChangeAngleDegrees { get; }
        internal bool QueryExecutedThisFrame =>
            CacheState == CharacterFootLandingObservationCacheState.Queried;
    }

    public readonly struct CharacterFootLandingObservationDiagnostics
    {
        internal CharacterFootLandingObservationDiagnostics(
            in CharacterFootLandingObservationResult result)
        {
            CharacterFootLandingObservationPage page = result.Page;
            ref readonly CharacterFootLandingObservationKey key = ref page.Key;
            Identity = key.Identity;
            WorldRevision = key.WorldRevision;
            SourceSampleIdentity = key.SourceSampleIdentity;
            SourceSampleCycle = key.SourceSampleCycle;
            CacheState = result.CacheState;
            QueryExecutedThisFrame = result.QueryExecutedThisFrame;
            QueryPurpose = page.Query.Purpose;
            CanonicalRawLanding = key.CanonicalRawLanding;
            CanonicalComponentUp = key.CanonicalComponentUp;
            QueryReason = result.QueryReason;
            RefreshMode = result.RefreshMode;
            CandidateRawLanding = result.CandidateRawLanding;
            CandidateComponentUp = result.CandidateComponentUp;
            QueryInputDistance = result.QueryInputDistance;
            QueryComponentUpAngleDegrees =
                result.QueryComponentUpAngleDegrees;
            PredictionInputAccumulationDistance =
                result.PredictionInputAccumulationDistance;
            ComponentUpChangeAngleDegrees =
                result.ComponentUpChangeAngleDegrees;
        }

        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public ulong Identity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public ulong WorldRevision { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public ulong SourceSampleIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public int SourceSampleCycle { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootLandingObservationCacheState CacheState { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public bool QueryExecutedThisFrame { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootPlacementQueryPurpose QueryPurpose { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 CanonicalRawLanding { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 CanonicalComponentUp { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootLandingObservationQueryReason QueryReason { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public CharacterFootLandingObservationRefreshMode RefreshMode { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 CandidateRawLanding { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 CandidateComponentUp { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float QueryInputDistance { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float QueryComponentUpAngleDegrees { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float PredictionInputAccumulationDistance { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public float ComponentUpChangeAngleDegrees { get; }
        public bool IsAvailable => Identity != 0;
    }

    internal readonly struct CharacterFootLandingPredictionResult
    {
        readonly CharacterFootLandingObservationDiagnostics m_Observation;
        readonly CharacterFootPlacementQueryRequest m_Query;
        readonly CharacterFootLandingQuerySelectionDiagnostics m_QuerySelection;
        readonly CharacterFootGroundPathResult m_GroundPath;

        internal CharacterFootLandingPredictionResult(
            CharacterFootSide side,
            CharacterFootLandingPredictionState state,
            CharacterFootLandingPredictionRejectReason rejectReason,
            CharacterFootLandingStepSource stepSource,
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            float landingConfidence,
            float timeToLandingSeconds,
            Vector3 rootLocalLanding,
            bool futureBodyTranslationAvailable,
            string futureBodyTranslationSourceIdentity,
            in ThirdPersonSimulation.CharacterFutureBodyTranslationSample futureBodyTranslation,
            Vector3 currentAnimatedSole,
            Vector3 rawLandingCandidate,
            in CharacterFootLandingObservationDiagnostics observation,
            in CharacterFootPlacementQueryRequest query,
            in CharacterFootLandingSupport support,
            in CharacterFootLandingQuerySelectionDiagnostics querySelection)
        {
            Side = side;
            State = state;
            RejectReason = rejectReason;
            StepSource = stepSource;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            LandingConfidence = landingConfidence;
            TimeToLandingSeconds = timeToLandingSeconds;
            RootLocalLanding = rootLocalLanding;
            FutureBodyTranslationAvailable = futureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity ?? string.Empty;
            FutureBodyRelativeTranslation = futureBodyTranslationAvailable
                ? new Vector3(
                    futureBodyTranslation.RelativePositionX,
                    futureBodyTranslation.RelativePositionY,
                    futureBodyTranslation.RelativePositionZ)
                : default;
            FutureBodyTranslationVelocity = futureBodyTranslationAvailable
                ? new Vector3(
                    futureBodyTranslation.VelocityX,
                    futureBodyTranslation.VelocityY,
                    futureBodyTranslation.VelocityZ)
                : default;
            CurrentAnimatedSole = currentAnimatedSole;
            RawLandingCandidate = rawLandingCandidate;
            m_Observation = observation;
            m_Query = query;
            SurfaceIdentity = support.SurfaceIdentity;
            LandingPoint = support.Point;
            LandingNormal = support.Normal;
            QueryDistance = support.Distance;
            m_QuerySelection = querySelection;
            m_GroundPath = default;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootGroundPathResult groundPath)
        {
            this = source;
            m_GroundPath = groundPath;
        }
        public CharacterFootSide Side { get; }
        public CharacterFootLandingPredictionState State { get; }
        public CharacterFootLandingPredictionRejectReason RejectReason { get; }
        public CharacterFootLandingStepSource StepSource { get; }
        public ulong LandingEventIdentity { get; }
        public ulong TrajectoryGeneration { get; }
        public float LandingConfidence { get; }
        public float TimeToLandingSeconds { get; }
        public Vector3 RootLocalLanding { get; }
        public bool FutureBodyTranslationAvailable { get; }
        public string FutureBodyTranslationSourceIdentity { get; }
        public Vector3 FutureBodyRelativeTranslation { get; }
        public Vector3 FutureBodyTranslationVelocity { get; }
        public Vector3 CurrentAnimatedSole { get; }
        public Vector3 RawLandingCandidate { get; }
        public ref readonly CharacterFootLandingObservationDiagnostics Observation =>
            ref m_Observation;
        public ref readonly CharacterFootPlacementQueryRequest Query =>
            ref m_Query;
        public int SurfaceIdentity { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 LandingNormal { get; }
        public float QueryDistance { get; }
        public ref readonly CharacterFootLandingQuerySelectionDiagnostics QuerySelection =>
            ref m_QuerySelection;
        internal ref readonly CharacterFootGroundPathResult GroundPath =>
            ref m_GroundPath;
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;

        internal CharacterFootLandingPredictionResult WithGroundPath(
            in CharacterFootGroundPathResult groundPath) =>
            new CharacterFootLandingPredictionResult(in this, in groundPath);

    }

    public readonly struct CharacterFootLandingPredictionFootDiagnostics
    {
        internal CharacterFootLandingPredictionFootDiagnostics(
            in CharacterFootLandingPredictionResult result,
            CharacterFootPlacementAnimatedFootPose sourcePose,
            in CharacterFootStepSelectionDiagnostics stepSelection,
            CharacterFootLandingSnapshot landing,
            bool approachPlantTargetPrepared,
            in CharacterFootCurrentSupportObservation currentSupport,
            in CharacterResolvedFootResult resolved,
            in CharacterFootSwingMotionResult footMotion,
            in CharacterFullBodyIkGoal goal,
            in CharacterFootCurrentSupportObservation outputSupport,
            in CharacterFootCurrentSupportObservation stateTargetSupport)
        {
            Side = result.Side;
            State = result.State;
            RejectReason = result.RejectReason;
            StepSource = result.StepSource;
            LandingEventIdentity = result.LandingEventIdentity;
            TrajectoryGeneration = result.TrajectoryGeneration;
            LandingConfidence = result.LandingConfidence;
            TimeToLandingSeconds = result.TimeToLandingSeconds;
            RootLocalLanding = result.RootLocalLanding;
            FutureBodyTranslationAvailable = result.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = result.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = result.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = result.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = result.CurrentAnimatedSole;
            RawLandingCandidate = result.RawLandingCandidate;
            Observation = result.Observation;
            Query = result.Query;
            QuerySelection = result.QuerySelection;
            SurfaceIdentity = result.SurfaceIdentity;
            LandingPoint = result.LandingPoint;
            LandingNormal = result.LandingNormal;
            QueryDistance = result.QueryDistance;
            Goal = goal;
            SourceAnklePosition = sourcePose.AnklePosition;
            SourceAnkleRotation = sourcePose.AnkleRotation;
            SourceHeelPosition = sourcePose.HeelPosition;
            SourceToePosition = sourcePose.ToePosition;
            StepSelection = stepSelection;
            NextLandingTrackingState = landing.NextTrackingState;
            NextLandingTrackingEventIdentity =
                landing.NextTrackingEventIdentity;
            VerifiedLastLandingAvailable = landing.HasVerifiedLastLanding;
            VerifiedLastLandingEventIdentity =
                landing.VerifiedLastLandingEventIdentity;
            PlantTargetState = landing.PlantTargetState;
            PlantTargetAvailable = landing.HasPlantTarget;
            ref readonly CharacterFootGroundPathLanding plantTarget =
                ref landing.PlantTarget;
            PlantTargetEventIdentity = landing.HasPlantTarget
                ? plantTarget.LandingEventIdentity
                : 0;
            PlantTargetSurfaceIdentity = landing.HasPlantTarget
                ? plantTarget.SurfaceIdentity
                : 0;
            PlantTargetPoint = landing.HasPlantTarget
                ? plantTarget.Point
                : default;
            PlantTargetNormal = landing.HasPlantTarget
                ? plantTarget.Normal
                : default;
            PlantTargetTrajectoryGeneration = landing.HasPlantTarget
                ? plantTarget.TrajectoryGeneration
                : 0;
            PlantTargetFutureBodyTranslationSourceIdentity =
                landing.HasPlantTarget
                    ? plantTarget.FutureBodyTranslationSourceIdentity
                    : string.Empty;
            PlantTargetUpdated = landing.PlantTargetUpdated;
            PlantVerificationAttempted = landing.PlantVerificationAttempted;
            PlantVerificationUnavailable = landing.PlantVerificationUnavailable;
            ApproachPlantTargetPrepared = approachPlantTargetPrepared;
            ref readonly CharacterFootGroundPathResult groundPath =
                ref result.GroundPath;
            GroundPath = new CharacterFootGroundPathDiagnostics(in groundPath);
            FootMotion = new CharacterFootSwingMotionDiagnostics(in footMotion);
            CurrentSupport = new CharacterFootCurrentSupportDiagnostics(
                in currentSupport);
            OutputSupport = new CharacterFootCurrentSupportDiagnostics(in outputSupport);
            StateTargetSupport = new CharacterFootCurrentSupportDiagnostics(in stateTargetSupport);
            Resolved = new CharacterResolvedFootDiagnostics(
                in resolved,
                in sourcePose);
        }

        public CharacterFootSide Side { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public CharacterFootLandingPredictionState State { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public CharacterFootLandingPredictionRejectReason RejectReason { get; }
        [DiagnosticField]
        [DiagnosticKey("landing-step-source")]
        [DiagnosticGroup("identity")]
        public CharacterFootLandingStepSource StepSource { get; }
        [DiagnosticField]
        [DiagnosticKey("landing-event-identity")]
        [DiagnosticGroup("identity")]
        public ulong LandingEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public ulong TrajectoryGeneration { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public float LandingConfidence { get; }
        [DiagnosticField]
        [DiagnosticKey("landing-time-to-landing")]
        [DiagnosticGroup("identity")]
        public float TimeToLandingSeconds { get; }
        [DiagnosticField]
        [DiagnosticGroup("root-landing")]
        public Vector3 RootLocalLanding { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public bool FutureBodyTranslationAvailable { get; }
        public string FutureBodyTranslationSourceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(FutureBodyTranslationAvailable))]
        public Vector3 FutureBodyRelativeTranslation { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(FutureBodyTranslationAvailable))]
        public Vector3 FutureBodyTranslationVelocity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public Vector3 CurrentAnimatedSole { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(RawLandingAvailable))]
        public Vector3 RawLandingCandidate { get; }
        public CharacterFootLandingObservationDiagnostics Observation { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public CharacterFootLandingQuerySelectionDiagnostics QuerySelection
        {
            get;
        }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public int SurfaceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public Vector3 LandingPoint { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public Vector3 LandingNormal { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(Accepted))]
        public float QueryDistance { get; }
        public CharacterFullBodyIkGoal Goal { get; }
        [DiagnosticField]
        [DiagnosticKey("source-ankle-position")]
        [DiagnosticGroup("motion-core")]
        public Vector3 SourceAnklePosition { get; }
        [DiagnosticField]
        [DiagnosticKey("source-ankle-rotation")]
        [DiagnosticGroup("motion-core")]
        public Quaternion SourceAnkleRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("source-heel-position")]
        [DiagnosticGroup("motion-core")]
        public Vector3 SourceHeelPosition { get; }

        [DiagnosticField]
        [DiagnosticKey("source-toe-position")]
        [DiagnosticGroup("motion-core")]
        public Vector3 SourceToePosition { get; }
        public CharacterFootStepSelectionDiagnostics StepSelection { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public CharacterFootNextLandingTrackingState NextLandingTrackingState { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public ulong NextLandingTrackingEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool VerifiedLastLandingAvailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public ulong VerifiedLastLandingEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public CharacterFootPlantTargetState PlantTargetState { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool PlantTargetAvailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public ulong PlantTargetEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public int PlantTargetSurfaceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public Vector3 PlantTargetPoint { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public Vector3 PlantTargetNormal { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public ulong PlantTargetTrajectoryGeneration { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(PlantTargetAvailable))]
        public string PlantTargetFutureBodyTranslationSourceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool PlantTargetUpdated { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool PlantVerificationAttempted { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool PlantVerificationUnavailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("identity")]
        public bool ApproachPlantTargetPrepared { get; }
        public CharacterFootGroundPathDiagnostics GroundPath { get; }
        public CharacterFootSwingMotionDiagnostics FootMotion { get; }
        public CharacterFootCurrentSupportDiagnostics CurrentSupport { get; }
        public CharacterFootCurrentSupportDiagnostics OutputSupport { get; }
        public CharacterFootCurrentSupportDiagnostics StateTargetSupport { get; }
        public CharacterResolvedFootDiagnostics Resolved { get; }
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public bool RawLandingAvailable =>
            RejectReason == CharacterFootLandingPredictionRejectReason.None ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryMissed ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryCapacityExceeded;
        [DiagnosticField]
        [DiagnosticGroup("landing-observation")]
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootStepSelectionDiagnostics
    {
        internal CharacterFootStepSelectionDiagnostics(
            ulong lastLandingEventIdentity,
            CharacterFootLandingStepSource selectedSource,
            ulong selectedLandingEventIdentity,
            float maximumPredictionTimeSeconds)
        {
            LastLandingEventIdentity = lastLandingEventIdentity;
            SelectedSource = selectedSource;
            SelectedLandingEventIdentity = selectedLandingEventIdentity;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
        }

        [DiagnosticField]
        [DiagnosticKey("step-selection-last-landing-event")]
        [DiagnosticGroup("identity")]
        public ulong LastLandingEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticKey("step-selection-source")]
        [DiagnosticGroup("identity")]
        public CharacterFootLandingStepSource SelectedSource { get; }
        [DiagnosticField]
        [DiagnosticKey("selected-landing-event-identity")]
        [DiagnosticGroup("identity")]
        public ulong SelectedLandingEventIdentity { get; }
        [DiagnosticField]
        [DiagnosticKey("step-selection-maximum-prediction-time")]
        [DiagnosticGroup("identity")]
        public float MaximumPredictionTimeSeconds { get; }
    }

    public readonly struct CharacterFootStepObservationInputDiagnostics
    {
        internal CharacterFootStepObservationInputDiagnostics(
            in AnimationFootMotionRuntimeFrame frame)
        {
            if (!frame.IsValid)
                throw new ArgumentException("Foot Step observation input diagnostics is invalid.");
            CompletionIdentity = frame.CompletionIdentity;
            SourceId = frame.SourceId.ToString();
            SourceIdentity = frame.SourceIdentity;
            ContributionContinuityIdentity = frame.ContributionContinuityIdentity;
            ClipBindingIndex = frame.ClipBindingIndex;
            Cycle = frame.Cycle;
            SourceWeight = frame.SourceWeight;
            NormalizedTime = frame.NormalizedTime;
            Left = frame.Left;
            Right = frame.Right;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        [DiagnosticField]
        [DiagnosticKey("formal-observation-completion")]
        [DiagnosticGroup("formal-source")]
        public ulong CompletionIdentity { get; }
        [DiagnosticField]
        [DiagnosticKey("formal-observation-source-id")]
        [DiagnosticGroup("formal-source")]
        public string SourceId { get; }
        [DiagnosticField]
        [DiagnosticKey("formal-observation-source-identity")]
        [DiagnosticGroup("formal-source")]
        public string SourceIdentity { get; }
        [DiagnosticField]
        [DiagnosticKey("formal-observation-continuity")]
        [DiagnosticGroup("formal-source")]
        public ulong ContributionContinuityIdentity { get; }
        public int ClipBindingIndex { get; }
        [DiagnosticField]
        [DiagnosticKey("formal-observation-cycle")]
        [DiagnosticGroup("formal-source")]
        public int Cycle { get; }
        public float SourceWeight { get; }
        [DiagnosticField]
        [DiagnosticKey("formal-observation-normalized-time")]
        [DiagnosticGroup("formal-source")]
        public float NormalizedTime { get; }
        internal AnimationFootMotionRuntimeSample Left { get; }
        internal AnimationFootMotionRuntimeSample Right { get; }
        public bool IsValid => m_IsSpecified != 0;
    }

    public readonly struct CharacterFootLandingPredictionInputDiagnostics
    {
        internal CharacterFootLandingPredictionInputDiagnostics(
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame body,
            bool grounded,
            float horizontalSpeed,
            in CharacterFootActionOccupancy leftAction,
            in CharacterFootActionOccupancy rightAction,
            in ThirdPersonSimulation.CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            in CharacterFootPredictionMotionResult predictionMotion,
            in AnimationFootMotionRuntimeFrame footStepObservation)
        {
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Grounded = grounded;
            HorizontalSpeed = horizontalSpeed;
            LeftActionInstanceIdentity = leftAction.ActionInstanceIdentity;
            LeftActionFootWeight = leftAction.Weight;
            RightActionInstanceIdentity = rightAction.ActionInstanceIdentity;
            RightActionFootWeight = rightAction.Weight;
            PreviousBodyTick = body.PreviousTick;
            CurrentBodyTick = body.CurrentTick;
            BodySampleAlpha = body.SampleAlpha;
            BodySampleAgeSeconds = body.SampleAgeSeconds;
            VisibleBodyPosition = body.VisiblePosition;
            VisibleBodyRotation = body.VisibleRotation;
            VisibleBodyVelocity = body.VisibleVelocity;
            VisibleBodyYawVelocityDegreesPerSecond =
                body.VisibleYawVelocityDegreesPerSecond;
            TargetBodyPosition = body.TargetPosition;
            TargetBodyRotation = body.TargetRotation;
            TargetBodyVelocity = body.TargetVelocity;
            TargetBodyYawVelocityDegreesPerSecond =
                body.TargetYawVelocityDegreesPerSecond;
            BodyPositionError = body.PositionError;
            BodyRotationError = body.RotationError;
            CorrectionPositionError = body.CorrectionPositionError;
            CorrectionPositionVelocity = body.CorrectionPositionVelocity;
            CorrectionYawVelocityDegreesPerSecond =
                body.CorrectionYawVelocityDegreesPerSecond;
            CorrectionActive = body.CorrectionActive;
            CorrectionClamped = body.CorrectionClamped;
            CorrectionSettled = body.CorrectionSettled;
            BodyResetSequence = body.ResetSequence;
            MotionTimelineAvailable = timeline.IsValid;
            TimelineGeneration = timeline.Generation;
            TimelineAuthorityTick = timeline.AuthorityTick.Value;
            TimelineTickRate = timeline.TickRate;
            TimelineCurrentVelocityX = timeline.CurrentVelocityX;
            TimelineCurrentVelocityZ = timeline.CurrentVelocityZ;
            TimelineContinuationVelocityX = timeline.ContinuationVelocityX;
            TimelineContinuationVelocityZ = timeline.ContinuationVelocityZ;
            TimelineHasContinuation = timeline.HasContinuation;
            TimelineBodyYawVelocityDegreesPerSecond =
                timeline.BodyYawVelocityDegreesPerSecond;
            TimelineMaximumBodyYawVelocityDegreesPerSecond =
                timeline.MaximumBodyYawVelocityDegreesPerSecond;
            CurrentSegmentRemainingSeconds = currentSegmentRemainingSeconds;
            PredictionMotionAvailable = predictionMotion.IsValid;
            PredictionMotionRejectReason = predictionMotion.RejectReason;
            PredictionMotionResetReason = predictionMotion.ResetReason;
            PredictionMotionSourceIdentity =
                predictionMotion.PredictionSourceIdentity;
            PredictionRawCurrentVelocityX = predictionMotion.RawCurrentVelocity.x;
            PredictionRawCurrentVelocityZ = predictionMotion.RawCurrentVelocity.y;
            PredictionRawContinuationVelocityX =
                predictionMotion.RawContinuationVelocity.x;
            PredictionRawContinuationVelocityZ =
                predictionMotion.RawContinuationVelocity.y;
            PredictionPreviousStableCurrentVelocityX =
                predictionMotion.PreviousStableCurrentVelocity.x;
            PredictionPreviousStableCurrentVelocityZ =
                predictionMotion.PreviousStableCurrentVelocity.y;
            PredictionPreviousStableContinuationVelocityX =
                predictionMotion.PreviousStableContinuationVelocity.x;
            PredictionPreviousStableContinuationVelocityZ =
                predictionMotion.PreviousStableContinuationVelocity.y;
            PredictionStableCurrentVelocityX =
                predictionMotion.StableCurrentVelocity.x;
            PredictionStableCurrentVelocityZ =
                predictionMotion.StableCurrentVelocity.y;
            PredictionStableContinuationVelocityX =
                predictionMotion.StableContinuationVelocity.x;
            PredictionStableContinuationVelocityZ =
                predictionMotion.StableContinuationVelocity.y;
            PredictionCurrentVelocityDeltaX =
                predictionMotion.CurrentVelocityDelta.x;
            PredictionCurrentVelocityDeltaZ =
                predictionMotion.CurrentVelocityDelta.y;
            PredictionContinuationVelocityDeltaX =
                predictionMotion.ContinuationVelocityDelta.x;
            PredictionContinuationVelocityDeltaZ =
                predictionMotion.ContinuationVelocityDelta.y;
            PredictionVelocityResponseAlpha = predictionMotion.ResponseAlpha;
            PredictionVelocityDeltaThreshold = predictionMotion.DeltaThreshold;
            PredictionVelocitySmoothSpeed = predictionMotion.SmoothSpeed;
            PredictionMaximumSpeed = predictionMotion.MaximumSpeed;
            PredictionCurrentResponseApplied =
                predictionMotion.CurrentResponseApplied;
            PredictionContinuationResponseApplied =
                predictionMotion.ContinuationResponseApplied;
            PredictionCurrentMaximumSpeedClamped =
                predictionMotion.CurrentMaximumSpeedClamped;
            PredictionContinuationMaximumSpeedClamped =
                predictionMotion.ContinuationMaximumSpeedClamped;
            PredictionMotionRevision = predictionMotion.Revision;
            FootStepObservation =
                new CharacterFootStepObservationInputDiagnostics(in footStepObservation);
        }

        [DiagnosticField]
        [DiagnosticKey("presentation-delta-seconds")]
        [DiagnosticGroup("timing")]
        public float PresentationDeltaSeconds { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public bool Grounded { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public float HorizontalSpeed { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public ulong LeftActionInstanceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public float LeftActionFootWeight { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public ulong RightActionInstanceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("action")]
        public float RightActionFootWeight { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public ulong PreviousBodyTick { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public ulong CurrentBodyTick { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float BodySampleAlpha { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float BodySampleAgeSeconds { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 VisibleBodyPosition { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Quaternion VisibleBodyRotation { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 VisibleBodyVelocity { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public float VisibleBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 TargetBodyPosition { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Quaternion TargetBodyRotation { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 TargetBodyVelocity { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public float TargetBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public float BodyPositionError { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public float BodyRotationError { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 CorrectionPositionError { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public Vector3 CorrectionPositionVelocity { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public float CorrectionYawVelocityDegreesPerSecond { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public bool CorrectionActive { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public bool CorrectionClamped { get; }
        [DiagnosticField]
        [DiagnosticGroup("body-correction")]
        public bool CorrectionSettled { get; }
        [DiagnosticField]
        [DiagnosticKey("body-reset-sequence")]
        [DiagnosticGroup("body-correction")]
        public ulong BodyResetSequence { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public bool MotionTimelineAvailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public ulong TimelineGeneration { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public ulong TimelineAuthorityTick { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public int TimelineTickRate { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineCurrentVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineCurrentVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineContinuationVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineContinuationVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public bool TimelineHasContinuation { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float TimelineMaximumBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField]
        [DiagnosticGroup("timing")]
        public float CurrentSegmentRemainingSeconds { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public bool PredictionMotionAvailable { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public CharacterFootPredictionMotionRejectReason PredictionMotionRejectReason { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public CharacterFootPredictionMotionResetReason PredictionMotionResetReason { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public string PredictionMotionSourceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionRawCurrentVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionRawCurrentVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionRawContinuationVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionRawContinuationVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionPreviousStableCurrentVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionPreviousStableCurrentVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionPreviousStableContinuationVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionPreviousStableContinuationVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionStableCurrentVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionStableCurrentVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionStableContinuationVelocityX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionStableContinuationVelocityZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionCurrentVelocityDeltaX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionCurrentVelocityDeltaZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionContinuationVelocityDeltaX { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionContinuationVelocityDeltaZ { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionVelocityResponseAlpha { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionVelocityDeltaThreshold { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionVelocitySmoothSpeed { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public float PredictionMaximumSpeed { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public bool PredictionCurrentResponseApplied { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public bool PredictionContinuationResponseApplied { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public bool PredictionCurrentMaximumSpeedClamped { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public bool PredictionContinuationMaximumSpeedClamped { get; }
        [DiagnosticField]
        [DiagnosticGroup("prediction-motion")]
        public ulong PredictionMotionRevision { get; }
        public CharacterFootStepObservationInputDiagnostics FootStepObservation { get; }
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        internal CharacterFootLandingPredictionDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            int rootInstanceId,
            string profileId,
            string profileRevision,
            CharacterFootLandingPredictionInputDiagnostics input,
            in CharacterFootPrimarySupportDiagnostics primarySupport,
            CharacterFullBodyIkGoal pelvisGoal,
            in CharacterFootStrideHipsDiagnostics strideHips,
            CharacterFootLandingPredictionFootDiagnostics left,
            CharacterFootLandingPredictionFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RootInstanceId = rootInstanceId;
            ProfileId = profileId;
            ProfileRevision = profileRevision;
            Input = input;
            PrimarySupport = primarySupport;
            PelvisGoal = pelvisGoal;
            StrideHips = strideHips;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public int RootInstanceId { get; }
        public string ProfileId { get; }
        public string ProfileRevision { get; }
        public CharacterFootLandingPredictionInputDiagnostics Input { get; }
        public CharacterFootPrimarySupportDiagnostics PrimarySupport { get; }
        public CharacterFullBodyIkGoal PelvisGoal { get; }
        public CharacterFootStrideHipsDiagnostics StrideHips { get; }
        public CharacterFootLandingPredictionFootDiagnostics Left { get; }
        public CharacterFootLandingPredictionFootDiagnostics Right { get; }
        public bool IsCompleted => FrameSequence != 0 && CompletionIdentity != 0 &&
            RootInstanceId != 0 && PelvisGoal.IsValid && Left.Goal.IsValid && Right.Goal.IsValid;
    }

    internal delegate void CharacterFootLandingPredictionPublishedHandler(
        in CharacterFootLandingPredictionDiagnostics diagnostics);

    internal static class CharacterFootLandingPredictionDebugRegistry
    {
        static readonly Dictionary<int, CharacterFootLandingPredictionDiagnostics> s_ByRoot =
            new Dictionary<int, CharacterFootLandingPredictionDiagnostics>();

        internal static event CharacterFootLandingPredictionPublishedHandler Published;

        internal static void Publish(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (!diagnostics.IsCompleted)
                return;
            s_ByRoot[diagnostics.RootInstanceId] = diagnostics;
            CharacterFootLandingPredictionPublishedHandler published = Published;
            try
            {
                published?.Invoke(in diagnostics);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        internal static bool TryGet(
            int rootInstanceId,
            out CharacterFootLandingPredictionDiagnostics diagnostics) =>
            s_ByRoot.TryGetValue(rootInstanceId, out diagnostics);

        internal static void Remove(int rootInstanceId) => s_ByRoot.Remove(rootInstanceId);
    }

    internal static class CharacterFootLandingPredictor
    {
        internal static Vector3 ProjectRawLanding(
            Vector3 rootPosition,
            Quaternion rootRotation,
            in ThirdPersonSimulation.CharacterFutureBodyTranslationSample bodyTranslation,
            Vector3 rootLocalLanding)
        {
            if (!Finite(rootPosition) || !Finite(rootLocalLanding))
            {
                throw new ArgumentException("Foot Landing projection input is invalid.");
            }
            Vector3 futureRootPosition = rootPosition + new Vector3(
                bodyTranslation.RelativePositionX,
                bodyTranslation.RelativePositionY,
                bodyTranslation.RelativePositionZ);
            return futureRootPosition + rootRotation * rootLocalLanding;
        }

        internal static CharacterFootPlacementQueryRequest BuildQuery(
            in CharacterFootLandingObservationKey key,
            in CharacterFootLandingPredictionSettings settings)
        {
            Vector3 up = key.CanonicalComponentUp;
            return new CharacterFootPlacementQueryRequest(
                key.QueryPurpose == CharacterFootPlacementQueryPurpose.CurrentContactVerification
                    ? CharacterFootPlacementQueryShape.Sole
                    : CharacterFootPlacementQueryShape.Sphere,
                key.QueryPurpose,
                key.Side == CharacterFootSide.Left ? 0 : 1,
                key.CanonicalRawLanding + up * settings.CastAbove,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot,
                key.HeelOffset,
                key.ToeOffset);
        }

        internal static CharacterFootLandingObservationResult ResolveObservation(
            CharacterFootSide side,
            ulong landingEventIdentity,
            ulong sourceSampleIdentity,
            int sourceSampleCycle,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            CharacterFootLandingObservationRefreshMode refreshMode,
            string profileRevision,
            in CharacterFootLandingPredictionSettings settings,
            ICharacterFootLandingWorldQuery world,
            CharacterFootLandingObservationPagePool pool,
            CharacterFootLandingObservationPage committedPage,
            out CharacterFootLandingObservationPage pendingPage,
            Vector3 heelOffset = default,
            Vector3 toeOffset = default)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (refreshMode != CharacterFootLandingObservationRefreshMode.Thresholded &&
                refreshMode != CharacterFootLandingObservationRefreshMode
                    .ChangedContactApproachInput &&
                refreshMode != CharacterFootLandingObservationRefreshMode
                    .ForcedPlantVerification)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshMode));
            }
            ulong worldRevision = world.WorldRevision;
            CharacterFootPlacementQueryPurpose queryPurpose =
                refreshMode ==
                CharacterFootLandingObservationRefreshMode.ForcedPlantVerification
                    ? CharacterFootPlacementQueryPurpose
                        .CurrentContactVerification
                    : CharacterFootPlacementQueryPurpose.FutureLanding;
            var key = new CharacterFootLandingObservationKey(
                side,
                queryPurpose,
                landingEventIdentity,
                sourceSampleIdentity,
                sourceSampleCycle,
                rawLandingCandidate,
                componentUp,
                profileRevision,
                worldRevision,
                heelOffset,
                toeOffset);
            CharacterFootLandingObservationQueryReason queryReason =
                CharacterFootLandingObservationQueryReason.None;
            float queryInputDistance = 0f;
            float queryComponentUpAngleDegrees = 0f;
            if (committedPage == null || !committedPage.HasValue)
            {
                queryReason =
                    CharacterFootLandingObservationQueryReason.PageUnavailable;
            }
            else
            {
                ref readonly CharacterFootLandingObservationKey committedKey =
                    ref committedPage.Key;
                queryInputDistance = Vector3.Distance(
                    rawLandingCandidate,
                    committedKey.CanonicalRawLanding);
                queryComponentUpAngleDegrees = Vector3.Angle(
                    componentUp,
                    committedKey.CanonicalComponentUp);
                if (committedKey.Side != side)
                    queryReason |= CharacterFootLandingObservationQueryReason.SideChanged;
                if (committedKey.QueryPurpose != queryPurpose)
                    queryReason |= CharacterFootLandingObservationQueryReason
                        .QueryPurposeChanged;
                if (committedKey.LandingEventIdentity != landingEventIdentity)
                    queryReason |= CharacterFootLandingObservationQueryReason.LandingEventChanged;
                if (committedKey.SourceSampleIdentity != sourceSampleIdentity)
                    queryReason |= CharacterFootLandingObservationQueryReason.SourceChanged;
                if (committedKey.SourceSampleCycle != sourceSampleCycle)
                    queryReason |= CharacterFootLandingObservationQueryReason.SourceCycleChanged;
                if (!string.Equals(
                        committedKey.ProfileRevision,
                        profileRevision,
                        StringComparison.Ordinal))
                {
                    queryReason |= CharacterFootLandingObservationQueryReason
                        .ProfileRevisionChanged;
                }
                if (committedKey.WorldRevision != worldRevision)
                    queryReason |= CharacterFootLandingObservationQueryReason.WorldRevisionChanged;
                if (queryInputDistance >
                    settings.PredictionInputAccumulationDistance)
                {
                    queryReason |= CharacterFootLandingObservationQueryReason
                        .PredictionInputDistanceExceeded;
                }
                if (queryComponentUpAngleDegrees >
                    settings.ComponentUpChangeAngleDegrees)
                {
                    queryReason |= CharacterFootLandingObservationQueryReason
                        .ComponentUpAngleExceeded;
                }
                if (refreshMode == CharacterFootLandingObservationRefreshMode
                        .ChangedContactApproachInput &&
                    committedKey.Identity != key.Identity)
                {
                    queryReason |= CharacterFootLandingObservationQueryReason
                        .ContactAcquisitionRefresh;
                }
            }
            if (refreshMode == CharacterFootLandingObservationRefreshMode
                    .ForcedPlantVerification)
            {
                queryReason |= CharacterFootLandingObservationQueryReason
                    .ContactAcquisitionRefresh;
            }
            if (queryReason == CharacterFootLandingObservationQueryReason.None)
            {
                pendingPage = CharacterFootLandingObservationPagePool
                    .ReuseCommitted(committedPage);
                return new CharacterFootLandingObservationResult(
                    pendingPage,
                    CharacterFootLandingObservationCacheState.Reused,
                    queryReason,
                    refreshMode,
                    rawLandingCandidate,
                    componentUp,
                    queryInputDistance,
                    queryComponentUpAngleDegrees,
                    in settings);
            }
            pendingPage = pool.AcquireWritable(committedPage);
            CharacterFootPlacementQueryRequest query = BuildQuery(
                in key,
                in settings);
            CharacterFootLandingQueryResult result = world.Query(in query);
            pendingPage.Set(in key, in query, in result);
            return new CharacterFootLandingObservationResult(
                pendingPage,
                CharacterFootLandingObservationCacheState.Queried,
                queryReason,
                refreshMode,
                rawLandingCandidate,
                componentUp,
                queryInputDistance,
                queryComponentUpAngleDegrees,
                in settings);
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
