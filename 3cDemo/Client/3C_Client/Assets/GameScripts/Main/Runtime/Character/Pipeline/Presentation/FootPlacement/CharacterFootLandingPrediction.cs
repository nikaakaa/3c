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

        [DiagnosticField(1, "identity", "landing-observation", AvailabilityMember = nameof(IsAvailable))]
        public int SurfaceIdentity { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(IsAvailable))]
        public Vector3 Point { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(IsAvailable))]
        public float Distance { get; }
        [DiagnosticField(1, "none", "landing-observation")]
        public bool IsAvailable => SurfaceIdentity != 0;
    }

    public readonly struct CharacterFootLandingQuerySelectionDiagnostics
    {
        internal CharacterFootLandingQuerySelectionDiagnostics(
            CharacterFootLandingQueryCandidateSelectionState state,
            int validCandidateCount,
            CharacterFootLandingQueryCandidateDiagnostics selected)
        {
            State = state;
            ValidCandidateCount = validCandidateCount;
            Selected = selected;
        }

        [DiagnosticField(1, "category", "landing-observation")]
        public CharacterFootLandingQueryCandidateSelectionState State { get; }
        [DiagnosticField(1, "count", "landing-observation")]
        public int ValidCandidateCount { get; }
        public CharacterFootLandingQueryCandidateDiagnostics Selected { get; }
    }

    internal readonly struct CharacterFootLandingQueryResult
    {
        internal CharacterFootLandingQueryResult(
            CharacterFootLandingQueryRejectReason rejectReason,
            CharacterFootLandingSupport support,
            CharacterFootLandingQuerySelectionDiagnostics selectionDiagnostics)
        {
            RejectReason = rejectReason;
            Support = support;
            SelectionDiagnostics = selectionDiagnostics;
        }

        internal CharacterFootLandingQueryRejectReason RejectReason { get; }
        internal CharacterFootLandingSupport Support { get; }
        internal CharacterFootLandingQuerySelectionDiagnostics
            SelectionDiagnostics { get; }
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
        ChangedSlidingAdmissionInput = 2,
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
            ulong worldRevision)
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
        internal bool HasValue { get; private set; }
        internal CharacterFootLandingObservationKey Key { get; private set; }
        internal CharacterFootPlacementQueryRequest Query { get; private set; }
        internal CharacterFootLandingQueryResult Result { get; private set; }

        internal void Set(
            in CharacterFootLandingObservationKey key,
            in CharacterFootPlacementQueryRequest query,
            in CharacterFootLandingQueryResult result)
        {
            HasValue = true;
            Key = key;
            Query = query;
            Result = result;
        }

        internal void Clear()
        {
            HasValue = false;
            Key = default;
            Query = default;
            Result = default;
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
            Identity = page.Key.Identity;
            WorldRevision = page.Key.WorldRevision;
            SourceSampleIdentity = page.Key.SourceSampleIdentity;
            SourceSampleCycle = page.Key.SourceSampleCycle;
            CacheState = result.CacheState;
            QueryExecutedThisFrame = result.QueryExecutedThisFrame;
            QueryPurpose = page.Query.Purpose;
            CanonicalRawLanding = page.Key.CanonicalRawLanding;
            CanonicalComponentUp = page.Key.CanonicalComponentUp;
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

        [DiagnosticField(1, "identity", "landing-observation")]
        public ulong Identity { get; }
        [DiagnosticField(1, "identity", "landing-observation")]
        public ulong WorldRevision { get; }
        [DiagnosticField(1, "identity", "landing-observation")]
        public ulong SourceSampleIdentity { get; }
        [DiagnosticField(1, "count", "landing-observation")]
        public int SourceSampleCycle { get; }
        [DiagnosticField(1, "category", "landing-observation")]
        public CharacterFootLandingObservationCacheState CacheState { get; }
        [DiagnosticField(1, "none", "landing-observation")]
        public bool QueryExecutedThisFrame { get; }
        [DiagnosticField(1, "category", "landing-observation")]
        public CharacterFootPlacementQueryPurpose QueryPurpose { get; }
        [DiagnosticField(1, "metres", "landing-observation")]
        public Vector3 CanonicalRawLanding { get; }
        [DiagnosticField(1, "direction", "landing-observation")]
        public Vector3 CanonicalComponentUp { get; }
        [DiagnosticField(1, "category", "landing-observation")]
        public CharacterFootLandingObservationQueryReason QueryReason { get; }
        [DiagnosticField(1, "category", "landing-observation")]
        public CharacterFootLandingObservationRefreshMode RefreshMode { get; }
        [DiagnosticField(1, "metres", "landing-observation")]
        public Vector3 CandidateRawLanding { get; }
        [DiagnosticField(1, "direction", "landing-observation")]
        public Vector3 CandidateComponentUp { get; }
        [DiagnosticField(1, "metres", "landing-observation")]
        public float QueryInputDistance { get; }
        [DiagnosticField(1, "degrees", "landing-observation")]
        public float QueryComponentUpAngleDegrees { get; }
        [DiagnosticField(1, "metres", "landing-observation")]
        public float PredictionInputAccumulationDistance { get; }
        [DiagnosticField(1, "degrees", "landing-observation")]
        public float ComponentUpChangeAngleDegrees { get; }
        public bool IsAvailable => Identity != 0;
    }

    internal readonly struct CharacterFootLandingPredictionResult
    {
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
            CharacterFootLandingObservationDiagnostics observation,
            CharacterFootPlacementQueryRequest query,
            CharacterFootLandingSupport support,
            CharacterFootLandingQuerySelectionDiagnostics querySelection)
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
            Observation = observation;
            Query = query;
            SurfaceIdentity = support.SurfaceIdentity;
            LandingPoint = support.Point;
            LandingNormal = support.Normal;
            QueryDistance = support.Distance;
            QuerySelection = querySelection;
            GroundPath = default;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootGroundPathResult groundPath)
        {
            this = source;
            GroundPath = groundPath;
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
        public CharacterFootLandingObservationDiagnostics Observation { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public int SurfaceIdentity { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 LandingNormal { get; }
        public float QueryDistance { get; }
        public CharacterFootLandingQuerySelectionDiagnostics QuerySelection
        {
            get;
        }
        internal CharacterFootGroundPathResult GroundPath { get; }
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
            in CharacterFootStepCandidateSelectionDiagnostics stepCandidateSelection,
            CharacterFootLandingSnapshot landing,
            bool approachPlantTargetPrepared,
            in CharacterFootCurrentSupportObservation currentSupport,
            in CharacterResolvedFootResult resolved,
            in CharacterFootSwingMotionResult footMotion,
            in CharacterFullBodyIkGoal goal)
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
            StepCandidateSelection = stepCandidateSelection;
            NextLandingTrackingState = landing.NextTrackingState;
            NextLandingTrackingEventIdentity =
                landing.NextTrackingEventIdentity;
            VerifiedLastLandingAvailable = landing.HasVerifiedLastLanding;
            VerifiedLastLandingEventIdentity =
                landing.VerifiedLastLandingEventIdentity;
            PlantTargetState = landing.PlantTargetState;
            PlantTargetAvailable = landing.HasPlantTarget;
            PlantTargetEventIdentity = landing.HasPlantTarget
                ? landing.PlantTarget.LandingEventIdentity
                : 0;
            PlantTargetSurfaceIdentity = landing.HasPlantTarget
                ? landing.PlantTarget.SurfaceIdentity
                : 0;
            PlantTargetPoint = landing.HasPlantTarget
                ? landing.PlantTarget.Point
                : default;
            PlantTargetNormal = landing.HasPlantTarget
                ? landing.PlantTarget.Normal
                : default;
            PlantTargetTrajectoryGeneration = landing.HasPlantTarget
                ? landing.PlantTarget.TrajectoryGeneration
                : 0;
            PlantTargetFutureBodyTranslationSourceIdentity =
                landing.HasPlantTarget
                    ? landing.PlantTarget.FutureBodyTranslationSourceIdentity
                    : string.Empty;
            PlantTargetUpdated = landing.PlantTargetUpdated;
            PlantVerificationAttempted = landing.PlantVerificationAttempted;
            PlantVerificationUnavailable = landing.PlantVerificationUnavailable;
            ApproachPlantTargetPrepared = approachPlantTargetPrepared;
            CharacterFootGroundPathResult groundPath = result.GroundPath;
            GroundPath = new CharacterFootGroundPathDiagnostics(in groundPath);
            FootMotion = new CharacterFootSwingMotionDiagnostics(in footMotion);
            CurrentSupport = new CharacterFootCurrentSupportDiagnostics(
                in currentSupport);
            Resolved = new CharacterResolvedFootDiagnostics(
                in resolved,
                in sourcePose);
        }

        public CharacterFootSide Side { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootLandingPredictionState State { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootLandingPredictionRejectReason RejectReason { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootLandingStepSource StepSource { get; }
        [DiagnosticField(1, "identity", "identity")]
        public ulong LandingEventIdentity { get; }
        [DiagnosticField(1, "identity", "identity")]
        public ulong TrajectoryGeneration { get; }
        [DiagnosticField(1, "unitless", "identity")]
        public float LandingConfidence { get; }
        [DiagnosticField(1, "seconds", "identity")]
        public float TimeToLandingSeconds { get; }
        [DiagnosticField(1, "metres", "root-landing")]
        public Vector3 RootLocalLanding { get; }
        [DiagnosticField(1, "none", "landing-observation")]
        public bool FutureBodyTranslationAvailable { get; }
        public string FutureBodyTranslationSourceIdentity { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(FutureBodyTranslationAvailable))]
        public Vector3 FutureBodyRelativeTranslation { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(FutureBodyTranslationAvailable))]
        public Vector3 FutureBodyTranslationVelocity { get; }
        [DiagnosticField(1, "metres", "landing-observation")]
        public Vector3 CurrentAnimatedSole { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(RawLandingAvailable))]
        public Vector3 RawLandingCandidate { get; }
        public CharacterFootLandingObservationDiagnostics Observation { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public CharacterFootLandingQuerySelectionDiagnostics QuerySelection
        {
            get;
        }
        [DiagnosticField(1, "identity", "landing-observation", AvailabilityMember = nameof(Accepted))]
        public int SurfaceIdentity { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(Accepted))]
        public Vector3 LandingPoint { get; }
        [DiagnosticField(1, "direction", "landing-observation", AvailabilityMember = nameof(Accepted))]
        public Vector3 LandingNormal { get; }
        [DiagnosticField(1, "metres", "landing-observation", AvailabilityMember = nameof(Accepted))]
        public float QueryDistance { get; }
        public CharacterFullBodyIkGoal Goal { get; }
        public Vector3 SourceAnklePosition { get; }
        [DiagnosticField(1, "unitless", "motion-core")]
        public Quaternion SourceAnkleRotation { get; }

        [DiagnosticField(1, "metres", "motion-core")]
        public Vector3 SourceHeelPosition { get; }

        [DiagnosticField(1, "metres", "motion-core")]
        public Vector3 SourceToePosition { get; }
        public CharacterFootStepCandidateSelectionDiagnostics StepCandidateSelection { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootNextLandingTrackingState NextLandingTrackingState { get; }
        [DiagnosticField(1, "identity", "identity")]
        public ulong NextLandingTrackingEventIdentity { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool VerifiedLastLandingAvailable { get; }
        [DiagnosticField(1, "identity", "identity")]
        public ulong VerifiedLastLandingEventIdentity { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootPlantTargetState PlantTargetState { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool PlantTargetAvailable { get; }
        [DiagnosticField(1, "identity", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public ulong PlantTargetEventIdentity { get; }
        [DiagnosticField(1, "count", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public int PlantTargetSurfaceIdentity { get; }
        [DiagnosticField(1, "metres", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public Vector3 PlantTargetPoint { get; }
        [DiagnosticField(1, "direction", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public Vector3 PlantTargetNormal { get; }
        [DiagnosticField(1, "identity", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public ulong PlantTargetTrajectoryGeneration { get; }
        [DiagnosticField(1, "identity", "identity", AvailabilityMember = nameof(PlantTargetAvailable))]
        public string PlantTargetFutureBodyTranslationSourceIdentity { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool PlantTargetUpdated { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool PlantVerificationAttempted { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool PlantVerificationUnavailable { get; }
        [DiagnosticField(1, "none", "identity")]
        public bool ApproachPlantTargetPrepared { get; }
        public CharacterFootGroundPathDiagnostics GroundPath { get; }
        public CharacterFootSwingMotionDiagnostics FootMotion { get; }
        public CharacterFootCurrentSupportDiagnostics CurrentSupport { get; }
        public CharacterResolvedFootDiagnostics Resolved { get; }
        [DiagnosticField(1, "none", "landing-observation")]
        public bool RawLandingAvailable =>
            RejectReason == CharacterFootLandingPredictionRejectReason.None ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryMissed ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryCapacityExceeded;
        [DiagnosticField(1, "none", "landing-observation")]
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootStepCandidateDiagnostics
    {
        internal CharacterFootStepCandidateDiagnostics(
            in AnimationFootMotionRuntimeSample step)
        {
            IsValid = step.IsValid;
            IsAuthoritative = step.IsAuthoritative;
            HasConsistentLandingEventIdentity =
                step.HasConsistentLandingEventIdentity;
            IsPreSwing = step.IsPreSwing;
            IsSwing = step.IsSwing;
            HasCurrentContactEvent = step.HasCurrentContactEvent;
            CurrentContactEventIdentity = step.CurrentContactEventIdentity;
            EventOrdinal = step.EventOrdinal;
            SourceSampleCycle = step.SourceSampleCycle;
            ContributionContinuityIdentity =
                step.ContributionContinuityIdentity;
            LandingEventIdentity = step.LandingEventIdentity;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            Distance = step.Distance;
            Phase = step.Events.Phase;
            SwingProgress = step.SwingProgress;
            ApproachContactToLandingProgress =
                step.ApproachContactToLandingProgress;
            RootLocalLanding = step.RootLocalLanding;
        }

        [DiagnosticField(1, "none", "current-step")]
        public bool IsValid { get; }
        [DiagnosticField(1, "none", "current-step")]
        public bool IsAuthoritative { get; }
        [DiagnosticField(1, "none", "current-step")]
        public bool HasConsistentLandingEventIdentity { get; }
        [DiagnosticField(1, "none", "current-step")]
        public bool IsPreSwing { get; }
        [DiagnosticField(1, "none", "current-step")]
        public bool IsSwing { get; }
        public bool HasCurrentContactEvent { get; }
        public ulong CurrentContactEventIdentity { get; }
        [DiagnosticField(1, "count", "current-step")]
        public int EventOrdinal { get; }
        public int SourceLandingCycleOffset => 0;
        [DiagnosticField(1, "count", "current-step")]
        public int SourceSampleCycle { get; }
        [DiagnosticField(1, "identity", "current-step")]
        public ulong ContributionContinuityIdentity { get; }
        [DiagnosticField(1, "identity", "current-step")]
        public ulong LandingEventIdentity { get; }
        [DiagnosticField(1, "seconds", "current-step")]
        public float TimeToLandingSeconds { get; }
        public float Distance { get; }
        public AnimationFootMotionEventPhase Phase { get; }
        [DiagnosticField(1, "unitless", "current-step")]
        public float SwingProgress { get; }
        [DiagnosticField(1, "unitless", "current-step")]
        public float EventPhase => SwingProgress;
        [DiagnosticField(1, "unitless", "current-step")]
        public float ApproachContactToLandingProgress { get; }
        [DiagnosticField(1, "unitless", "current-step")]
        public float LandingPhase => IsValid ? 1f : 0f;
        [DiagnosticField(1, "none", "current-step")]
        public bool AtOrAfterApproachContact =>
            IsValid && Phase == AnimationFootMotionEventPhase.ApproachContact;
        [DiagnosticField(1, "none", "current-step")]
        public bool InApproachContactToLanding =>
            IsValid && Phase == AnimationFootMotionEventPhase.ApproachContact;
        [DiagnosticField(1, "metres", "current-step")]
        public Vector3 RootLocalLanding { get; }
    }

    public readonly struct CharacterFootStepCandidateSelectionDiagnostics
    {
        internal CharacterFootStepCandidateSelectionDiagnostics(
            in AnimationFootMotionRuntimeSample footMotion,
            ulong lastLandingEventIdentity,
            CharacterFootLandingStepSource selectedSource,
            ulong selectedLandingEventIdentity,
            float maximumPredictionTimeSeconds)
        {
            FootMotion = new CharacterFootStepCandidateDiagnostics(in footMotion);
            LastLandingEventIdentity = lastLandingEventIdentity;
            SelectedSource = selectedSource;
            SelectedLandingEventIdentity = selectedLandingEventIdentity;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
        }

        public CharacterFootStepCandidateDiagnostics FootMotion { get; }
        public CharacterFootStepCandidateDiagnostics Current => FootMotion;
        public CharacterFootStepCandidateDiagnostics Incoming => default;
        [DiagnosticField(1, "identity", "identity")]
        public ulong LastLandingEventIdentity { get; }
        [DiagnosticField(1, "category", "identity")]
        public CharacterFootLandingStepSource SelectedSource { get; }
        [DiagnosticField(1, "identity", "identity")]
        public ulong SelectedLandingEventIdentity { get; }
        [DiagnosticField(1, "seconds", "identity")]
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
        public ulong CompletionIdentity { get; }
        public string SourceId { get; }
        public string SourceIdentity { get; }
        public ulong ContributionContinuityIdentity { get; }
        public int ClipBindingIndex { get; }
        public int Cycle { get; }
        public float SourceWeight { get; }
        public float NormalizedTime { get; }
        public AnimationFootMotionRuntimeSample Left { get; }
        public AnimationFootMotionRuntimeSample Right { get; }
        public bool IsValid => m_IsSpecified != 0;
    }

    public readonly struct CharacterFootLandingPredictionInputDiagnostics
    {
        internal CharacterFootLandingPredictionInputDiagnostics(
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
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

        [DiagnosticField(1, "seconds", "timing")]
        public float PresentationDeltaSeconds { get; }
        [DiagnosticField(1, "none", "action")]
        public bool Grounded { get; }
        [DiagnosticField(1, "metres-per-second", "action")]
        public float HorizontalSpeed { get; }
        [DiagnosticField(1, "identity", "action")]
        public ulong LeftActionInstanceIdentity { get; }
        [DiagnosticField(1, "unitless", "action")]
        public float LeftActionFootWeight { get; }
        [DiagnosticField(1, "identity", "action")]
        public ulong RightActionInstanceIdentity { get; }
        [DiagnosticField(1, "unitless", "action")]
        public float RightActionFootWeight { get; }
        [DiagnosticField(1, "frame", "timing")]
        public ulong PreviousBodyTick { get; }
        [DiagnosticField(1, "frame", "timing")]
        public ulong CurrentBodyTick { get; }
        [DiagnosticField(1, "unitless", "timing")]
        public float BodySampleAlpha { get; }
        [DiagnosticField(1, "seconds", "timing")]
        public float BodySampleAgeSeconds { get; }
        [DiagnosticField(1, "metres", "body-correction")]
        public Vector3 VisibleBodyPosition { get; }
        [DiagnosticField(1, "unitless", "body-correction")]
        public Quaternion VisibleBodyRotation { get; }
        [DiagnosticField(1, "metres-per-second", "body-correction")]
        public Vector3 VisibleBodyVelocity { get; }
        [DiagnosticField(1, "degrees-per-second", "body-correction")]
        public float VisibleBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField(1, "metres", "body-correction")]
        public Vector3 TargetBodyPosition { get; }
        [DiagnosticField(1, "unitless", "body-correction")]
        public Quaternion TargetBodyRotation { get; }
        [DiagnosticField(1, "metres-per-second", "body-correction")]
        public Vector3 TargetBodyVelocity { get; }
        [DiagnosticField(1, "degrees-per-second", "body-correction")]
        public float TargetBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField(1, "metres", "body-correction")]
        public float BodyPositionError { get; }
        [DiagnosticField(1, "degrees", "body-correction")]
        public float BodyRotationError { get; }
        [DiagnosticField(1, "metres", "body-correction")]
        public Vector3 CorrectionPositionError { get; }
        [DiagnosticField(1, "metres-per-second", "body-correction")]
        public Vector3 CorrectionPositionVelocity { get; }
        [DiagnosticField(1, "degrees-per-second", "body-correction")]
        public float CorrectionYawVelocityDegreesPerSecond { get; }
        [DiagnosticField(1, "none", "body-correction")]
        public bool CorrectionActive { get; }
        [DiagnosticField(1, "none", "body-correction")]
        public bool CorrectionClamped { get; }
        [DiagnosticField(1, "none", "body-correction")]
        public bool CorrectionSettled { get; }
        [DiagnosticField(1, "identity", "body-correction")]
        public ulong BodyResetSequence { get; }
        [DiagnosticField(1, "none", "timing")]
        public bool MotionTimelineAvailable { get; }
        [DiagnosticField(1, "identity", "timing")]
        public ulong TimelineGeneration { get; }
        [DiagnosticField(1, "frame", "timing")]
        public ulong TimelineAuthorityTick { get; }
        [DiagnosticField(1, "hertz", "timing")]
        public int TimelineTickRate { get; }
        [DiagnosticField(1, "metres-per-second", "timing")]
        public float TimelineCurrentVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "timing")]
        public float TimelineCurrentVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "timing")]
        public float TimelineContinuationVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "timing")]
        public float TimelineContinuationVelocityZ { get; }
        [DiagnosticField(1, "none", "timing")]
        public bool TimelineHasContinuation { get; }
        [DiagnosticField(1, "degrees-per-second", "timing")]
        public float TimelineBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField(1, "degrees-per-second", "timing")]
        public float TimelineMaximumBodyYawVelocityDegreesPerSecond { get; }
        [DiagnosticField(1, "seconds", "timing")]
        public float CurrentSegmentRemainingSeconds { get; }
        [DiagnosticField(1, "none", "prediction-motion")]
        public bool PredictionMotionAvailable { get; }
        [DiagnosticField(1, "category", "prediction-motion")]
        public CharacterFootPredictionMotionRejectReason PredictionMotionRejectReason { get; }
        [DiagnosticField(1, "category", "prediction-motion")]
        public CharacterFootPredictionMotionResetReason PredictionMotionResetReason { get; }
        [DiagnosticField(1, "identity", "prediction-motion")]
        public string PredictionMotionSourceIdentity { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionRawCurrentVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionRawCurrentVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionRawContinuationVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionRawContinuationVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionPreviousStableCurrentVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionPreviousStableCurrentVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionPreviousStableContinuationVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionPreviousStableContinuationVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionStableCurrentVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionStableCurrentVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionStableContinuationVelocityX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionStableContinuationVelocityZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionCurrentVelocityDeltaX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionCurrentVelocityDeltaZ { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionContinuationVelocityDeltaX { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionContinuationVelocityDeltaZ { get; }
        [DiagnosticField(1, "unitless", "prediction-motion")]
        public float PredictionVelocityResponseAlpha { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionVelocityDeltaThreshold { get; }
        [DiagnosticField(1, "per-second", "prediction-motion")]
        public float PredictionVelocitySmoothSpeed { get; }
        [DiagnosticField(1, "metres-per-second", "prediction-motion")]
        public float PredictionMaximumSpeed { get; }
        [DiagnosticField(1, "none", "prediction-motion")]
        public bool PredictionCurrentResponseApplied { get; }
        [DiagnosticField(1, "none", "prediction-motion")]
        public bool PredictionContinuationResponseApplied { get; }
        [DiagnosticField(1, "none", "prediction-motion")]
        public bool PredictionCurrentMaximumSpeedClamped { get; }
        [DiagnosticField(1, "none", "prediction-motion")]
        public bool PredictionContinuationMaximumSpeedClamped { get; }
        [DiagnosticField(1, "identity", "prediction-motion")]
        public ulong PredictionMotionRevision { get; }
        public CharacterFootStepObservationInputDiagnostics FootStepObservation { get; }
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        sealed class Frame
        {
            internal Frame(
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

            internal ulong FrameSequence { get; }
            internal ulong CompletionIdentity { get; }
            internal int RootInstanceId { get; }
            internal string ProfileId { get; }
            internal string ProfileRevision { get; }
            internal CharacterFootLandingPredictionInputDiagnostics Input { get; }
            internal CharacterFootPrimarySupportDiagnostics PrimarySupport { get; }
            internal CharacterFullBodyIkGoal PelvisGoal { get; }
            internal CharacterFootStrideHipsDiagnostics StrideHips { get; }
            internal CharacterFootLandingPredictionFootDiagnostics Left { get; }
            internal CharacterFootLandingPredictionFootDiagnostics Right { get; }
        }

        readonly Frame m_Frame;

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
            m_Frame = new Frame(
                frameSequence,
                completionIdentity,
                rootInstanceId,
                profileId,
                profileRevision,
                input,
                in primarySupport,
                pelvisGoal,
                in strideHips,
                left,
                right);
        }

        public ulong FrameSequence => m_Frame?.FrameSequence ?? 0;
        public ulong CompletionIdentity => m_Frame?.CompletionIdentity ?? 0;
        public int RootInstanceId => m_Frame?.RootInstanceId ?? 0;
        public string ProfileId => m_Frame?.ProfileId ?? string.Empty;
        public string ProfileRevision =>
            m_Frame?.ProfileRevision ?? string.Empty;
        public CharacterFootLandingPredictionInputDiagnostics Input =>
            m_Frame == null ? default : m_Frame.Input;
        public CharacterFootPrimarySupportDiagnostics PrimarySupport =>
            m_Frame == null ? default : m_Frame.PrimarySupport;
        public CharacterFullBodyIkGoal PelvisGoal =>
            m_Frame == null ? default : m_Frame.PelvisGoal;
        public CharacterFootStrideHipsDiagnostics StrideHips =>
            m_Frame == null ? default : m_Frame.StrideHips;
        public CharacterFootLandingPredictionFootDiagnostics Left =>
            m_Frame == null ? default : m_Frame.Left;
        public CharacterFootLandingPredictionFootDiagnostics Right =>
            m_Frame == null ? default : m_Frame.Right;
        public bool IsCompleted =>
            m_Frame != null &&
            m_Frame.FrameSequence != 0 &&
            m_Frame.CompletionIdentity != 0 &&
            m_Frame.RootInstanceId != 0 &&
            m_Frame.PelvisGoal.IsValid &&
            m_Frame.Left.Goal.IsValid &&
            m_Frame.Right.Goal.IsValid;
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
                CharacterFootPlacementQueryShape.Sphere,
                key.QueryPurpose,
                key.Side == CharacterFootSide.Left ? 0 : 1,
                key.CanonicalRawLanding + up * settings.CastAbove,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot);
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
            out CharacterFootLandingObservationPage pendingPage)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            if (refreshMode != CharacterFootLandingObservationRefreshMode.Thresholded &&
                refreshMode != CharacterFootLandingObservationRefreshMode
                    .ChangedSlidingAdmissionInput &&
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
                worldRevision);
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
                CharacterFootLandingObservationKey committedKey =
                    committedPage.Key;
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
                        .ChangedSlidingAdmissionInput &&
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
