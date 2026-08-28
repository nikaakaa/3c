using System;
using System.Collections.Generic;
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
        Formal = 1
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

        public int SurfaceIdentity { get; }
        public Vector3 Point { get; }
        public float Distance { get; }
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

        public CharacterFootLandingQueryCandidateSelectionState State { get; }
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

    internal readonly struct CharacterFootLandingObservationKey :
        IEquatable<CharacterFootLandingObservationKey>
    {
        const float PositionScale = 1000f;
        const float DirectionScale = 10000f;

        internal CharacterFootLandingObservationKey(
            CharacterFootSide side,
            ulong landingEventIdentity,
            string sourceIdentity,
            int sourceCycle,
            ulong contributionContinuityIdentity,
            Vector3 rawLanding,
            Vector3 componentUp,
            string profileRevision,
            ulong worldRevision)
        {
            if ((side != CharacterFootSide.Left && side != CharacterFootSide.Right) ||
                landingEventIdentity == 0 ||
                string.IsNullOrWhiteSpace(sourceIdentity) || sourceCycle < 0 ||
                contributionContinuityIdentity == 0 || !Finite(rawLanding) ||
                !Finite(componentUp) || componentUp.sqrMagnitude <= 0.000001f ||
                string.IsNullOrWhiteSpace(profileRevision) || worldRevision == 0)
            {
                throw new ArgumentException("Foot Landing observation key is invalid.");
            }
            Vector3 up = componentUp.normalized;
            Side = side;
            LandingEventIdentity = landingEventIdentity;
            SourceIdentity = sourceIdentity.Trim();
            SourceCycle = sourceCycle;
            ContributionContinuityIdentity = contributionContinuityIdentity;
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
        internal ulong LandingEventIdentity { get; }
        internal string SourceIdentity { get; }
        internal int SourceCycle { get; }
        internal ulong ContributionContinuityIdentity { get; }
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
            LandingEventIdentity == other.LandingEventIdentity &&
            SourceCycle == other.SourceCycle &&
            ContributionContinuityIdentity == other.ContributionContinuityIdentity &&
            RawLandingX == other.RawLandingX &&
            RawLandingY == other.RawLandingY &&
            RawLandingZ == other.RawLandingZ &&
            ComponentUpX == other.ComponentUpX &&
            ComponentUpY == other.ComponentUpY &&
            ComponentUpZ == other.ComponentUpZ &&
            WorldRevision == other.WorldRevision &&
            string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal) &&
            string.Equals(ProfileRevision, other.ProfileRevision, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CharacterFootLandingObservationKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            (int)Side,
            LandingEventIdentity,
            SourceCycle,
            ContributionContinuityIdentity,
            RawLandingX,
            RawLandingY,
            RawLandingZ);

        internal bool HasSameRevisionLineage(
            CharacterFootSide side,
            ulong landingEventIdentity,
            string sourceIdentity,
            int sourceCycle,
            ulong contributionContinuityIdentity,
            string profileRevision,
            ulong worldRevision) =>
            Side == side &&
            LandingEventIdentity == landingEventIdentity &&
            SourceCycle == sourceCycle &&
            ContributionContinuityIdentity == contributionContinuityIdentity &&
            WorldRevision == worldRevision &&
            string.Equals(SourceIdentity, sourceIdentity, StringComparison.Ordinal) &&
            string.Equals(ProfileRevision, profileRevision, StringComparison.Ordinal);

        static int Quantize(float value, float scale) =>
            Mathf.RoundToInt(value * scale);

        static ulong ComputeIdentity(in CharacterFootLandingObservationKey key)
        {
            ulong hash = 14695981039346656037UL;
            Add(ref hash, (ulong)key.Side);
            Add(ref hash, key.LandingEventIdentity);
            Add(ref hash, key.SourceIdentity);
            Add(ref hash, unchecked((ulong)(long)key.SourceCycle));
            Add(ref hash, key.ContributionContinuityIdentity);
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
            bool hasPreviousQueryInput,
            float predictionInputMovement,
            float componentUpDeltaDegrees,
            bool revisionLineageChanged)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            CacheState = cacheState;
            HasPreviousQueryInput = hasPreviousQueryInput;
            PredictionInputMovement = predictionInputMovement;
            ComponentUpDeltaDegrees = componentUpDeltaDegrees;
            RevisionLineageChanged = revisionLineageChanged;
        }

        internal CharacterFootLandingObservationPage Page { get; }
        internal CharacterFootLandingObservationCacheState CacheState { get; }
        internal bool HasPreviousQueryInput { get; }
        internal float PredictionInputMovement { get; }
        internal float ComponentUpDeltaDegrees { get; }
        internal bool RevisionLineageChanged { get; }
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
            CacheState = result.CacheState;
            QueryExecutedThisFrame = result.QueryExecutedThisFrame;
            CanonicalRawLanding = page.Key.CanonicalRawLanding;
            HasPreviousQueryInput = result.HasPreviousQueryInput;
            PredictionInputMovement = result.PredictionInputMovement;
            ComponentUpDeltaDegrees = result.ComponentUpDeltaDegrees;
            RevisionLineageChanged = result.RevisionLineageChanged;
        }

        public ulong Identity { get; }
        public ulong WorldRevision { get; }
        public CharacterFootLandingObservationCacheState CacheState { get; }
        public bool QueryExecutedThisFrame { get; }
        public Vector3 CanonicalRawLanding { get; }
        public bool HasPreviousQueryInput { get; }
        public float PredictionInputMovement { get; }
        public float ComponentUpDeltaDegrees { get; }
        public bool RevisionLineageChanged { get; }
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
            CharacterFootLandingQuerySelectionDiagnostics querySelection,
            CharacterFullBodyIkGoal goal)
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
            Goal = goal;
            GroundPath = default;
            FootMotion = default;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootGroundPathResult groundPath)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = source.StepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = source.LandingConfidence;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            RootLocalLanding = source.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = source.CurrentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Observation = source.Observation;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            QuerySelection = source.QuerySelection;
            Goal = source.Goal;
            GroundPath = groundPath;
            FootMotion = source.FootMotion;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            CharacterFootLandingStepSource stepSource,
            AnimationFootMotionStep step,
            Vector3 currentAnimatedSole,
            CharacterFullBodyIkGoal goal)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = stepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = step.Confidence;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            RootLocalLanding = step.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = currentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Observation = source.Observation;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            QuerySelection = source.QuerySelection;
            Goal = goal;
            GroundPath = source.GroundPath;
            FootMotion = source.FootMotion;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootSwingMotionResult footMotion,
            CharacterFullBodyIkGoal goal)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = source.StepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = source.LandingConfidence;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            RootLocalLanding = source.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = source.CurrentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Observation = source.Observation;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            QuerySelection = source.QuerySelection;
            Goal = goal;
            GroundPath = source.GroundPath;
            FootMotion = footMotion;
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
        public CharacterFullBodyIkGoal Goal { get; }
        internal CharacterFootGroundPathResult GroundPath { get; }
        internal CharacterFootSwingMotionResult FootMotion { get; }
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;

        internal CharacterFootLandingPredictionResult WithLiveStep(
            CharacterFootLandingStepSource stepSource,
            AnimationFootMotionStep step,
            Vector3 currentAnimatedSole,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionResult(
                in this,
                stepSource,
                step,
                currentAnimatedSole,
                goal);
        internal CharacterFootLandingPredictionResult WithGroundPath(
            in CharacterFootGroundPathResult groundPath) =>
            new CharacterFootLandingPredictionResult(in this, in groundPath);

        internal CharacterFootLandingPredictionResult WithFootMotion(
            in CharacterFootSwingMotionResult footMotion,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionResult(
                in this,
                in footMotion,
                goal);
    }

    public readonly struct CharacterFootLandingPredictionFootDiagnostics
    {
        internal CharacterFootLandingPredictionFootDiagnostics(
            in CharacterFootLandingPredictionResult result,
            CharacterFootPlacementAnimatedFootPose sourcePose,
            in CharacterFootStepCandidateSelectionDiagnostics stepCandidateSelection)
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
            Goal = result.Goal;
            SourceAnklePosition = sourcePose.AnklePosition;
            SourceAnkleRotation = sourcePose.AnkleRotation;
            SourceHeelPosition = sourcePose.HeelPosition;
            SourceToePosition = sourcePose.ToePosition;
            StepCandidateSelection = stepCandidateSelection;
            CharacterFootGroundPathResult groundPath = result.GroundPath;
            CharacterFootSwingMotionResult footMotion = result.FootMotion;
            GroundPath = new CharacterFootGroundPathDiagnostics(in groundPath);
            FootMotion = new CharacterFootSwingMotionDiagnostics(in footMotion);
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
        public CharacterFootLandingQuerySelectionDiagnostics QuerySelection
        {
            get;
        }
        public int SurfaceIdentity { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 LandingNormal { get; }
        public float QueryDistance { get; }
        public CharacterFullBodyIkGoal Goal { get; }
        public Vector3 SourceAnklePosition { get; }
        public Quaternion SourceAnkleRotation { get; }
        public Vector3 SourceHeelPosition { get; }
        public Vector3 SourceToePosition { get; }
        public CharacterFootStepCandidateSelectionDiagnostics StepCandidateSelection { get; }
        public CharacterFootGroundPathDiagnostics GroundPath { get; }
        public CharacterFootSwingMotionDiagnostics FootMotion { get; }
        public bool RawLandingAvailable =>
            RejectReason == CharacterFootLandingPredictionRejectReason.None ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryMissed ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryCapacityExceeded;
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootStepCandidateDiagnostics
    {
        internal CharacterFootStepCandidateDiagnostics(
            in AnimationFootMotionStep step)
        {
            IsValid = step.IsValid;
            IsAuthoritative = step.IsAuthoritative;
            HasConsistentLandingEventIdentity =
                step.HasConsistentLandingEventIdentity;
            IsPreSwing = step.IsPreSwing;
            IsSwing = step.IsSwing;
            EventOrdinal = step.EventOrdinal;
            SourceLandingCycleOffset = step.SourceLandingCycleOffset;
            SourceSampleCycle = step.SourceSampleCycle;
            ContributionContinuityIdentity =
                step.ContributionContinuityIdentity;
            LandingEventIdentity = step.LandingEventIdentity;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            EventPhase = step.EventPhase;
            ApproachContactPhase = step.ApproachContactPhase;
            LandingPhase = step.LandingPhase;
            RootLocalLanding = step.RootLocalLanding;
        }

        public bool IsValid { get; }
        public bool IsAuthoritative { get; }
        public bool HasConsistentLandingEventIdentity { get; }
        public bool IsPreSwing { get; }
        public bool IsSwing { get; }
        public int EventOrdinal { get; }
        public int SourceLandingCycleOffset { get; }
        public int SourceSampleCycle { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong LandingEventIdentity { get; }
        public float TimeToLandingSeconds { get; }
        public float EventPhase { get; }
        public float ApproachContactPhase { get; }
        public float LandingPhase { get; }
        public bool AtOrAfterApproachContact =>
            IsValid && EventPhase >= ApproachContactPhase;
        public bool InApproachContactToLanding =>
            AtOrAfterApproachContact &&
            IsSwing &&
            EventPhase <= LandingPhase;
        public Vector3 RootLocalLanding { get; }
    }

    public readonly struct CharacterFootStepCandidateSelectionDiagnostics
    {
        internal CharacterFootStepCandidateSelectionDiagnostics(
            in AnimationFootMotionStep current,
            in AnimationFootMotionStep incoming,
            ulong lastLandingEventIdentity,
            CharacterFootLandingStepSource selectedSource,
            ulong selectedLandingEventIdentity,
            float maximumPredictionTimeSeconds)
        {
            Current = new CharacterFootStepCandidateDiagnostics(in current);
            Incoming = new CharacterFootStepCandidateDiagnostics(in incoming);
            LastLandingEventIdentity = lastLandingEventIdentity;
            SelectedSource = selectedSource;
            SelectedLandingEventIdentity = selectedLandingEventIdentity;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
        }

        public CharacterFootStepCandidateDiagnostics Current { get; }
        public CharacterFootStepCandidateDiagnostics Incoming { get; }
        public ulong LastLandingEventIdentity { get; }
        public CharacterFootLandingStepSource SelectedSource { get; }
        public ulong SelectedLandingEventIdentity { get; }
        public float MaximumPredictionTimeSeconds { get; }
    }

    public readonly struct CharacterFootMotionInputDiagnostics
    {
        internal CharacterFootMotionInputDiagnostics(
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
            Left = frame.Left.Observation;
            Right = frame.Right.Observation;
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
        public AnimationFootStepObservationSample Left { get; }
        public AnimationFootStepObservationSample Right { get; }
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
            in AnimationFootMotionRuntimeFrame footMotion)
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
            FootMotion =
                new CharacterFootMotionInputDiagnostics(in footMotion);
        }

        public float PresentationDeltaSeconds { get; }
        public bool Grounded { get; }
        public float HorizontalSpeed { get; }
        public ulong LeftActionInstanceIdentity { get; }
        public float LeftActionFootWeight { get; }
        public ulong RightActionInstanceIdentity { get; }
        public float RightActionFootWeight { get; }
        public ulong PreviousBodyTick { get; }
        public ulong CurrentBodyTick { get; }
        public float BodySampleAlpha { get; }
        public float BodySampleAgeSeconds { get; }
        public Vector3 VisibleBodyPosition { get; }
        public Quaternion VisibleBodyRotation { get; }
        public Vector3 VisibleBodyVelocity { get; }
        public float VisibleBodyYawVelocityDegreesPerSecond { get; }
        public Vector3 TargetBodyPosition { get; }
        public Quaternion TargetBodyRotation { get; }
        public Vector3 TargetBodyVelocity { get; }
        public float TargetBodyYawVelocityDegreesPerSecond { get; }
        public float BodyPositionError { get; }
        public float BodyRotationError { get; }
        public Vector3 CorrectionPositionError { get; }
        public Vector3 CorrectionPositionVelocity { get; }
        public float CorrectionYawVelocityDegreesPerSecond { get; }
        public bool CorrectionActive { get; }
        public bool CorrectionClamped { get; }
        public bool CorrectionSettled { get; }
        public ulong BodyResetSequence { get; }
        public bool MotionTimelineAvailable { get; }
        public ulong TimelineGeneration { get; }
        public ulong TimelineAuthorityTick { get; }
        public int TimelineTickRate { get; }
        public float TimelineCurrentVelocityX { get; }
        public float TimelineCurrentVelocityZ { get; }
        public float TimelineContinuationVelocityX { get; }
        public float TimelineContinuationVelocityZ { get; }
        public bool TimelineHasContinuation { get; }
        public float TimelineBodyYawVelocityDegreesPerSecond { get; }
        public float TimelineMaximumBodyYawVelocityDegreesPerSecond { get; }
        public float CurrentSegmentRemainingSeconds { get; }
        public CharacterFootMotionInputDiagnostics FootMotion { get; }
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        sealed class Frame
        {
            internal Frame(
                ulong frameSequence,
                ulong completionIdentity,
                int rootInstanceId,
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
                CharacterFootPlacementQueryPurpose.FutureLanding,
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
            string sourceIdentity,
            int sourceCycle,
            ulong contributionContinuityIdentity,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            string profileRevision,
            in CharacterFootLandingPredictionSettings settings,
            in CharacterFootMotionSettings motionSettings,
            ICharacterFootLandingWorldQuery world,
            CharacterFootLandingObservationPagePool pool,
            CharacterFootLandingObservationPage committedPage,
            out CharacterFootLandingObservationPage pendingPage)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (pool == null)
                throw new ArgumentNullException(nameof(pool));
            Vector3 normalizedUp = componentUp.normalized;
            bool hasPreviousQueryInput = committedPage != null &&
                                         committedPage.HasValue;
            bool sameRevisionLineage = hasPreviousQueryInput &&
                committedPage.Key.HasSameRevisionLineage(
                    side,
                    landingEventIdentity,
                    sourceIdentity,
                    sourceCycle,
                    contributionContinuityIdentity,
                    profileRevision,
                    world.WorldRevision);
            float predictionInputMovement = hasPreviousQueryInput
                ? Vector3.Distance(
                    committedPage.Key.CanonicalRawLanding,
                    rawLandingCandidate)
                : 0f;
            float componentUpDeltaDegrees = hasPreviousQueryInput
                ? Vector3.Angle(
                    committedPage.Key.CanonicalComponentUp,
                    normalizedUp)
                : 0f;
            if (sameRevisionLineage &&
                predictionInputMovement <=
                motionSettings.PredictionInputUpdateDistance &&
                componentUpDeltaDegrees <=
                motionSettings.PredictionInputUpAngleDegrees)
            {
                pendingPage = CharacterFootLandingObservationPagePool
                    .ReuseCommitted(committedPage);
                return new CharacterFootLandingObservationResult(
                    pendingPage,
                    CharacterFootLandingObservationCacheState.Reused,
                    true,
                    predictionInputMovement,
                    componentUpDeltaDegrees,
                    false);
            }
            var key = new CharacterFootLandingObservationKey(
                side,
                landingEventIdentity,
                sourceIdentity,
                sourceCycle,
                contributionContinuityIdentity,
                rawLandingCandidate,
                componentUp,
                profileRevision,
                world.WorldRevision);
            pendingPage = pool.AcquireWritable(committedPage);
            CharacterFootPlacementQueryRequest query = BuildQuery(
                in key,
                in settings);
            CharacterFootLandingQueryResult result = world.Query(in query);
            pendingPage.Set(in key, in query, in result);
            return new CharacterFootLandingObservationResult(
                pendingPage,
                CharacterFootLandingObservationCacheState.Queried,
                hasPreviousQueryInput,
                predictionInputMovement,
                componentUpDeltaDegrees,
                hasPreviousQueryInput && !sameRevisionLineage);
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
