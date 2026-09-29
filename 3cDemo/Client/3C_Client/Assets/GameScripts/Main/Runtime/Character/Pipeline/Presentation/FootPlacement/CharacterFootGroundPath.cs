using System;
using KK.GeneratedDiagnosticSampling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootGroundPathState : byte
    {
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootGroundPathRejectReason : byte
    {
        None = 0,
        CurrentLandingUnavailable = 1,
        NextLandingUnavailable = 2,
        InvalidRequest = 3,
        NoContact = 4,
        CapacityExceeded = 5,
        DegenerateEnvelope = 6,
        NoEnvelopeContact = 7,
        EnvelopeCapacityExceeded = 8,
        UnreachableEdge = 9,
        EdgeCapacityExceeded = 10,
        SurfaceGeometryUnavailable = 11,
        SurfaceGeometryUnsupported = 12,
        SurfaceGeometryChanged = 13,
        SurfaceCoverageCapacityExceeded = 14,
        SurfaceCoverageUnavailable = 15
    }

    public readonly struct CharacterFootGroundPathQueryRequest
    {
        internal CharacterFootGroundPathQueryRequest(
            CharacterFootSide side,
            Vector3 axisStart,
            Vector3 axisEnd,
            float radius,
            float maximumAxisSegmentLength,
            Vector3 direction,
            float maximumDistance,
            int layerMask,
            int segmentHitCapacity,
            int contactCapacity)
        {
            Side = side;
            AxisStart = axisStart;
            AxisEnd = axisEnd;
            Radius = radius;
            MaximumAxisSegmentLength = maximumAxisSegmentLength;
            Direction = direction;
            MaximumDistance = maximumDistance;
            LayerMask = layerMask;
            SegmentHitCapacity = segmentHitCapacity;
            ContactCapacity = contactCapacity;
        }

        public CharacterFootSide Side { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 AxisStart { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 AxisEnd { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public float Radius { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public float MaximumAxisSegmentLength { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 Direction { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public float MaximumDistance { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int LayerMask { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int SegmentHitCapacity { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int ContactCapacity { get; }

        internal bool IsValid =>
            (Side == CharacterFootSide.Left || Side == CharacterFootSide.Right) &&
            Finite(AxisStart) && Finite(AxisEnd) &&
            float.IsFinite(Radius) && Radius > 0f &&
            float.IsFinite(MaximumAxisSegmentLength) && MaximumAxisSegmentLength > 0f &&
            Finite(Direction) && Direction.sqrMagnitude > 0.000001f &&
            float.IsFinite(MaximumDistance) && MaximumDistance > 0f &&
            LayerMask != 0 &&
            SegmentHitCapacity >= 4 && SegmentHitCapacity <= 32 &&
            ContactCapacity >= 4 && ContactCapacity <= 64;

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    public readonly struct CharacterFootGroundContact
    {
        internal CharacterFootGroundContact(
            int segmentIndex,
            int surfaceIdentity,
            ulong candidateIdentity,
            Vector3 position,
            Vector3 normal,
            float queryDistance)
        {
            SegmentIndex = segmentIndex;
            SurfaceIdentity = surfaceIdentity;
            CandidateIdentity = candidateIdentity;
            Position = position;
            Normal = normal.normalized;
            QueryDistance = queryDistance;
        }

        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public int SegmentIndex { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public int SurfaceIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public ulong CandidateIdentity { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public Vector3 Position { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public Vector3 Normal { get; }
        [DiagnosticField]
        [DiagnosticGroup("ground-geometry")]
        public float QueryDistance { get; }
    }

    internal readonly struct CharacterFootGroundPathQueryResult
    {
        internal CharacterFootGroundPathQueryResult(
            CharacterFootGroundPathRejectReason rejectReason,
            int segmentCount)
        {
            RejectReason = rejectReason;
            SegmentCount = segmentCount;
        }

        internal CharacterFootGroundPathRejectReason RejectReason { get; }
        internal int SegmentCount { get; }
        internal bool Accepted => RejectReason == CharacterFootGroundPathRejectReason.None;
    }

    public sealed class CharacterFootGroundContactPage
    {
        readonly CharacterFootGroundContact[] m_Contacts;

        internal CharacterFootGroundContactPage(int capacity)
        {
            if (capacity < 4 || capacity > 64)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Contacts = new CharacterFootGroundContact[capacity];
            SurfaceCoverage = new CharacterFootGroundSurfacePage(capacity);
        }

        internal CharacterFootGroundSurfacePage SurfaceCoverage { get; }
        internal int Capacity => m_Contacts.Length;
        public int Count { get; private set; }

        public CharacterFootGroundContact this[int index] => ContactAt(index);

        internal CharacterFootGroundContact ContactAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Contacts[index];
        }

        internal void Clear()
        {
            Array.Clear(m_Contacts, 0, Count);
            Count = 0;
            SurfaceCoverage.Clear();
        }

        internal bool Contains(int segmentIndex, int surfaceIdentity)
        {
            for (int i = 0; i < Count; i++)
            {
                CharacterFootGroundContact contact = m_Contacts[i];
                if (contact.SegmentIndex == segmentIndex &&
                    contact.SurfaceIdentity == surfaceIdentity)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool TryAdd(in CharacterFootGroundContact contact)
        {
            if (Count >= Capacity)
                return false;
            m_Contacts[Count++] = contact;
            return true;
        }

        internal void SortCanonical()
        {
            for (int i = 1; i < Count; i++)
            {
                CharacterFootGroundContact value = m_Contacts[i];
                int insertion = i;
                while (insertion > 0 && Compare(value, m_Contacts[insertion - 1]) < 0)
                {
                    m_Contacts[insertion] = m_Contacts[insertion - 1];
                    insertion--;
                }
                m_Contacts[insertion] = value;
            }
        }

        static int Compare(
            CharacterFootGroundContact left,
            CharacterFootGroundContact right)
        {
            int identity = left.CandidateIdentity.CompareTo(right.CandidateIdentity);
            if (identity != 0)
                return identity;
            int distance = left.QueryDistance.CompareTo(right.QueryDistance);
            if (distance != 0)
                return distance;
            int x = left.Position.x.CompareTo(right.Position.x);
            if (x != 0)
                return x;
            int y = left.Position.y.CompareTo(right.Position.y);
            return y != 0 ? y : left.Position.z.CompareTo(right.Position.z);
        }
    }

    internal interface ICharacterFootGroundPathWorldQuery
    {
        CharacterFootGroundPathQueryResult Query(
            in CharacterFootGroundPathQueryRequest request,
            CharacterFootGroundContactPage output);
    }

    internal interface ICharacterFootPlacementWorldQuery :
        ICharacterFootLandingWorldQuery,
        ICharacterFootGroundPathWorldQuery,
        ICharacterFootCurrentSupportWorldQuery
    {
    }

    internal readonly struct CharacterFootGroundPathLanding
    {
        internal CharacterFootGroundPathLanding(
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            string futureBodyTranslationSourceIdentity,
            int surfaceIdentity,
            Vector3 point,
            Vector3 normal)
        {
            if (landingEventIdentity == 0 || surfaceIdentity == 0 ||
                !Finite(point) || !Finite(normal) || normal.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentException("Ground Path landing is invalid.");
            }
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity ?? string.Empty;
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal.normalized;
        }

        internal ulong LandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal string FutureBodyTranslationSourceIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal readonly struct CharacterFootGroundPathInputKey : IEquatable<CharacterFootGroundPathInputKey>
    {
        internal CharacterFootGroundPathInputKey(
            CharacterFootSide side,
            ulong lastLandingEventIdentity,
            ulong nextSwingLandingEventIdentity,
            ulong trajectoryGeneration,
            ulong authorityTick,
            string lastFutureBodyTranslationSourceIdentity,
            string nextSwingFutureBodyTranslationSourceIdentity,
            int lastLandingSurfaceIdentity,
            int nextSwingLandingSurfaceIdentity,
            Vector3 lastLandingPoint,
            Vector3 nextSwingLandingPoint,
            Vector3 lastLandingNormal,
            Vector3 nextSwingLandingNormal,
            Vector3 componentUp,
            string profileRevision)
        {
            Side = side;
            LastLandingEventIdentity = lastLandingEventIdentity;
            NextSwingLandingEventIdentity = nextSwingLandingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            AuthorityTick = authorityTick;
            LastFutureBodyTranslationSourceIdentity = lastFutureBodyTranslationSourceIdentity ?? string.Empty;
            NextSwingFutureBodyTranslationSourceIdentity = nextSwingFutureBodyTranslationSourceIdentity ?? string.Empty;
            LastLandingSurfaceIdentity = lastLandingSurfaceIdentity;
            NextSwingLandingSurfaceIdentity = nextSwingLandingSurfaceIdentity;
            LastLandingPointX = Quantize(lastLandingPoint.x, 1000f);
            LastLandingPointY = Quantize(lastLandingPoint.y, 1000f);
            LastLandingPointZ = Quantize(lastLandingPoint.z, 1000f);
            NextSwingLandingPointX = Quantize(nextSwingLandingPoint.x, 1000f);
            NextSwingLandingPointY = Quantize(nextSwingLandingPoint.y, 1000f);
            NextSwingLandingPointZ = Quantize(nextSwingLandingPoint.z, 1000f);
            LastLandingNormalX = Quantize(lastLandingNormal.x, 10000f);
            LastLandingNormalY = Quantize(lastLandingNormal.y, 10000f);
            LastLandingNormalZ = Quantize(lastLandingNormal.z, 10000f);
            NextSwingLandingNormalX = Quantize(nextSwingLandingNormal.x, 10000f);
            NextSwingLandingNormalY = Quantize(nextSwingLandingNormal.y, 10000f);
            NextSwingLandingNormalZ = Quantize(nextSwingLandingNormal.z, 10000f);
            ComponentUpX = Quantize(componentUp.x, 10000f);
            ComponentUpY = Quantize(componentUp.y, 10000f);
            ComponentUpZ = Quantize(componentUp.z, 10000f);
            ProfileRevision = profileRevision ?? string.Empty;
        }

        internal CharacterFootSide Side { get; }
        internal ulong LastLandingEventIdentity { get; }
        internal ulong NextSwingLandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal ulong AuthorityTick { get; }
        internal string LastFutureBodyTranslationSourceIdentity { get; }
        internal string NextSwingFutureBodyTranslationSourceIdentity { get; }
        internal int LastLandingSurfaceIdentity { get; }
        internal int NextSwingLandingSurfaceIdentity { get; }
        internal int LastLandingPointX { get; }
        internal int LastLandingPointY { get; }
        internal int LastLandingPointZ { get; }
        internal int NextSwingLandingPointX { get; }
        internal int NextSwingLandingPointY { get; }
        internal int NextSwingLandingPointZ { get; }
        internal int LastLandingNormalX { get; }
        internal int LastLandingNormalY { get; }
        internal int LastLandingNormalZ { get; }
        internal int NextSwingLandingNormalX { get; }
        internal int NextSwingLandingNormalY { get; }
        internal int NextSwingLandingNormalZ { get; }
        internal int ComponentUpX { get; }
        internal int ComponentUpY { get; }
        internal int ComponentUpZ { get; }
        internal string ProfileRevision { get; }

        public bool Equals(CharacterFootGroundPathInputKey other) =>
            Side == other.Side &&
            LastLandingEventIdentity == other.LastLandingEventIdentity &&
            NextSwingLandingEventIdentity == other.NextSwingLandingEventIdentity &&
            TrajectoryGeneration == other.TrajectoryGeneration &&
            AuthorityTick == other.AuthorityTick &&
            string.Equals(
                LastFutureBodyTranslationSourceIdentity,
                other.LastFutureBodyTranslationSourceIdentity,
                StringComparison.Ordinal) &&
            string.Equals(
                NextSwingFutureBodyTranslationSourceIdentity,
                other.NextSwingFutureBodyTranslationSourceIdentity,
                StringComparison.Ordinal) &&
            LastLandingSurfaceIdentity == other.LastLandingSurfaceIdentity &&
            NextSwingLandingSurfaceIdentity == other.NextSwingLandingSurfaceIdentity &&
            LastLandingPointX == other.LastLandingPointX &&
            LastLandingPointY == other.LastLandingPointY &&
            LastLandingPointZ == other.LastLandingPointZ &&
            NextSwingLandingPointX == other.NextSwingLandingPointX &&
            NextSwingLandingPointY == other.NextSwingLandingPointY &&
            NextSwingLandingPointZ == other.NextSwingLandingPointZ &&
            LastLandingNormalX == other.LastLandingNormalX &&
            LastLandingNormalY == other.LastLandingNormalY &&
            LastLandingNormalZ == other.LastLandingNormalZ &&
            NextSwingLandingNormalX == other.NextSwingLandingNormalX &&
            NextSwingLandingNormalY == other.NextSwingLandingNormalY &&
            NextSwingLandingNormalZ == other.NextSwingLandingNormalZ &&
            ComponentUpX == other.ComponentUpX &&
            ComponentUpY == other.ComponentUpY &&
            ComponentUpZ == other.ComponentUpZ &&
            string.Equals(ProfileRevision, other.ProfileRevision, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CharacterFootGroundPathInputKey other && Equals(other);

        public override int GetHashCode()
        {
            int first = HashCode.Combine(
                (int)Side,
                LastLandingEventIdentity,
                NextSwingLandingEventIdentity,
                TrajectoryGeneration,
                AuthorityTick);
            first = HashCode.Combine(
                first,
                LastFutureBodyTranslationSourceIdentity,
                NextSwingFutureBodyTranslationSourceIdentity,
                LastLandingSurfaceIdentity,
                NextSwingLandingSurfaceIdentity);
            return HashCode.Combine(
                first,
                LastLandingPointX,
                LastLandingPointY,
                LastLandingPointZ,
                NextSwingLandingPointX,
                NextSwingLandingPointY,
                NextSwingLandingPointZ);
        }

        static int Quantize(float value, float scale) =>
            Mathf.RoundToInt(value * scale);
    }

    internal readonly struct CharacterFootGroundPathInput
    {
        internal CharacterFootGroundPathInput(
            ulong identity,
            in CharacterFootGroundPathInputKey key,
            Vector3 lastLanding,
            Vector3 nextSwingLanding,
            Vector3 lastLandingNormal,
            Vector3 nextSwingLandingNormal,
            int lastLandingSurfaceIdentity,
            int nextSwingLandingSurfaceIdentity,
            Vector3 componentUp,
            float maximumReachableVerticalEdge,
            in CharacterFootGroundPathQueryRequest query)
        {
            Identity = identity;
            Key = key;
            LastLanding = lastLanding;
            NextSwingLanding = nextSwingLanding;
            LastLandingNormal = lastLandingNormal;
            NextSwingLandingNormal = nextSwingLandingNormal;
            LastLandingSurfaceIdentity = lastLandingSurfaceIdentity;
            NextSwingLandingSurfaceIdentity = nextSwingLandingSurfaceIdentity;
            ComponentUp = componentUp;
            MaximumReachableVerticalEdge = maximumReachableVerticalEdge;
            Query = query;
        }

        internal ulong Identity { get; }
        internal CharacterFootGroundPathInputKey Key { get; }
        internal Vector3 LastLanding { get; }
        internal Vector3 NextSwingLanding { get; }
        internal Vector3 LastLandingNormal { get; }
        internal Vector3 NextSwingLandingNormal { get; }
        internal int LastLandingSurfaceIdentity { get; }
        internal int NextSwingLandingSurfaceIdentity { get; }
        internal Vector3 ComponentUp { get; }
        internal float MaximumReachableVerticalEdge { get; }
        internal CharacterFootGroundPathQueryRequest Query { get; }
        internal bool IsValid =>
            Identity != 0 && Query.IsValid &&
            float.IsFinite(MaximumReachableVerticalEdge) &&
            MaximumReachableVerticalEdge > 0f;
    }

    internal static class CharacterFootGroundPathInputBuilder
    {
        internal static CharacterFootGroundPathInputKey BuildKey(
            CharacterFootSide side,
            in CharacterFootGroundPathLanding lastLanding,
            in CharacterFootGroundPathLanding nextSwingLanding,
            ulong authorityTick,
            Vector3 componentUp,
            string profileRevision) =>
            new CharacterFootGroundPathInputKey(
                side,
                lastLanding.LandingEventIdentity,
                nextSwingLanding.LandingEventIdentity,
                lastLanding.TrajectoryGeneration,
                authorityTick,
                lastLanding.FutureBodyTranslationSourceIdentity,
                nextSwingLanding.FutureBodyTranslationSourceIdentity,
                lastLanding.SurfaceIdentity,
                nextSwingLanding.SurfaceIdentity,
                lastLanding.Point,
                nextSwingLanding.Point,
                lastLanding.Normal,
                nextSwingLanding.Normal,
                componentUp,
                profileRevision);

        internal static bool TryBuild(
            in CharacterFootGroundPathInputKey key,
            Vector3 lastLanding,
            Vector3 nextSwingLanding,
            Vector3 lastLandingNormal,
            Vector3 nextSwingLandingNormal,
            int lastLandingSurfaceIdentity,
            int nextSwingLandingSurfaceIdentity,
            Vector3 componentUp,
            in CharacterFootGroundDetectionSettings settings,
            out CharacterFootGroundPathInput input)
        {
            if (!Finite(lastLanding) || !Finite(nextSwingLanding) ||
                !Finite(lastLandingNormal) || lastLandingNormal.sqrMagnitude <= 0.000001f ||
                !Finite(nextSwingLandingNormal) || nextSwingLandingNormal.sqrMagnitude <= 0.000001f ||
                lastLandingSurfaceIdentity == 0 || nextSwingLandingSurfaceIdentity == 0 ||
                !Finite(componentUp) ||
                componentUp.sqrMagnitude <= 0.000001f)
            {
                input = default;
                return false;
            }
            Vector3 up = componentUp.normalized;
            var query = new CharacterFootGroundPathQueryRequest(
                key.Side,
                lastLanding + up * settings.CastAbove,
                nextSwingLanding + up * settings.CastAbove,
                settings.CapsuleRadius,
                settings.MaximumAxisSegmentLength,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.GroundLayerMask,
                settings.SegmentHitCapacity,
                settings.ContactCapacity);
            if (!query.IsValid)
            {
                input = default;
                return false;
            }
            ulong identity = ComputeIdentity(
                in key,
                in query,
                settings.MaximumReachableVerticalEdge);
            input = new CharacterFootGroundPathInput(
                identity,
                in key,
                lastLanding,
                nextSwingLanding,
                lastLandingNormal.normalized,
                nextSwingLandingNormal.normalized,
                lastLandingSurfaceIdentity,
                nextSwingLandingSurfaceIdentity,
                up,
                settings.MaximumReachableVerticalEdge,
                in query);
            return true;
        }

        static ulong ComputeIdentity(
            in CharacterFootGroundPathInputKey key,
            in CharacterFootGroundPathQueryRequest query,
            float maximumReachableVerticalEdge)
        {
            ulong hash = 14695981039346656037UL;
            Add(ref hash, (ulong)key.Side);
            Add(ref hash, key.LastLandingEventIdentity);
            Add(ref hash, key.NextSwingLandingEventIdentity);
            Add(ref hash, key.TrajectoryGeneration);
            Add(ref hash, key.LastFutureBodyTranslationSourceIdentity);
            Add(ref hash, key.NextSwingFutureBodyTranslationSourceIdentity);
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingSurfaceIdentity));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingSurfaceIdentity));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingPointX));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingPointY));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingPointZ));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingPointX));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingPointY));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingPointZ));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingNormalX));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingNormalY));
            Add(ref hash, unchecked((ulong)(uint)key.LastLandingNormalZ));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingNormalX));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingNormalY));
            Add(ref hash, unchecked((ulong)(uint)key.NextSwingLandingNormalZ));
            Add(ref hash, key.ProfileRevision);
            Add(ref hash, unchecked((ulong)(uint)Mathf.RoundToInt(query.Radius * 10000f)));
            Add(ref hash, unchecked((ulong)(uint)Mathf.RoundToInt(query.MaximumAxisSegmentLength * 10000f)));
            Add(ref hash, unchecked((ulong)(uint)Mathf.RoundToInt(maximumReachableVerticalEdge * 10000f)));
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
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal sealed class CharacterFootGroundPathPage
    {
        CharacterFootGroundPathInput m_Input;

        internal CharacterFootGroundPathPage(int contactCapacity)
        {
            Contacts = new CharacterFootGroundContactPage(contactCapacity);
            Edges = new CharacterFootGroundEdgeSummary(contactCapacity);
            Envelope = new CharacterFootGroundEnvelopePage(contactCapacity);
        }

        internal CharacterFootGroundPathState State { get; private set; }
        internal CharacterFootGroundPathRejectReason RejectReason { get; private set; }
        internal bool QueryExecuted { get; private set; }
        internal int SegmentCount { get; private set; }
        internal ref readonly CharacterFootGroundPathInput Input =>
            ref m_Input;
        internal CharacterFootGroundInvalidSegment InvalidSegment { get; private set; }
        internal CharacterFootGroundContactPage Contacts { get; }
        internal CharacterFootGroundEdgeSummary Edges { get; }
        internal CharacterFootGroundEnvelopePage Envelope { get; }
        internal bool HasInput => Input.IsValid;

        internal void SetRejected(
            CharacterFootGroundPathRejectReason reason,
            bool queryExecuted,
            int segmentCount,
            in CharacterFootGroundPathInput input,
            in CharacterFootGroundInvalidSegment invalidSegment)
        {
            if (reason == CharacterFootGroundPathRejectReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            if ((reason == CharacterFootGroundPathRejectReason.UnreachableEdge) !=
                invalidSegment.HasValue)
                throw new ArgumentException("Ground Path invalid segment is inconsistent.");
            State = CharacterFootGroundPathState.Rejected;
            RejectReason = reason;
            QueryExecuted = queryExecuted;
            SegmentCount = segmentCount;
            m_Input = input;
            InvalidSegment = invalidSegment;
            if (!queryExecuted)
            {
                Contacts.Clear();
                Edges.Clear();
            }
            Envelope.Clear();
        }

        internal void SetAccepted(
            int segmentCount,
            in CharacterFootGroundPathInput input)
        {
            if (!input.IsValid || Contacts.Count <= 0 ||
                !Contacts.SurfaceCoverage.IsReady || Contacts.SurfaceCoverage.Count <= 0 ||
                Envelope.Count < 2 || InvalidSegment.HasValue ||
                segmentCount <= 0)
                throw new ArgumentException("Ground Path accepted page is invalid.");
            State = CharacterFootGroundPathState.Accepted;
            RejectReason = CharacterFootGroundPathRejectReason.None;
            QueryExecuted = true;
            SegmentCount = segmentCount;
            m_Input = input;
            InvalidSegment = default;
        }

        internal void Clear()
        {
            State = default;
            RejectReason = default;
            QueryExecuted = false;
            SegmentCount = 0;
            m_Input = default;
            InvalidSegment = default;
            Contacts.Clear();
            Edges.Clear();
            Envelope.Clear();
        }
    }

    internal sealed class CharacterFootGroundPathPagePool
    {
        readonly CharacterFootGroundPathPage m_First;
        readonly CharacterFootGroundPathPage m_Second;

        internal CharacterFootGroundPathPagePool(int contactCapacity)
        {
            m_First = new CharacterFootGroundPathPage(contactCapacity);
            m_Second = new CharacterFootGroundPathPage(contactCapacity);
            EnvelopeWorkspace = new CharacterFootGroundEnvelopeWorkspace(contactCapacity);
        }

        internal CharacterFootGroundEnvelopeWorkspace EnvelopeWorkspace { get; }

        internal CharacterFootGroundPathPage AcquireWritable(
            CharacterFootGroundPathPage committed)
        {
            CharacterFootGroundPathPage pending =
                ReferenceEquals(committed, m_First)
                ? m_Second
                : m_First;
            pending.Clear();
            return pending;
        }

        internal static CharacterFootGroundPathPage ReuseCommitted(
            CharacterFootGroundPathPage committed)
        {
            if (committed == null)
                throw new InvalidOperationException("Ground Path committed page is unavailable.");
            return committed;
        }

        internal static void Discard(
            CharacterFootGroundPathPage pending,
            CharacterFootGroundPathPage committed)
        {
            if (pending != null && !ReferenceEquals(pending, committed))
                pending.Clear();
        }

        internal void Reset()
        {
            m_First.Clear();
            m_Second.Clear();
            EnvelopeWorkspace.Clear();
        }
    }

    internal readonly struct CharacterFootGroundPathResult
    {
        readonly CharacterFootGroundPathPage m_Page;

        internal CharacterFootGroundPathResult(
            CharacterFootGroundPathPage page,
            bool queryExecutedThisFrame)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            QueryExecutedThisFrame = queryExecutedThisFrame;
        }

        internal CharacterFootGroundPathPage Page =>
            m_Page ?? throw new InvalidOperationException("Ground Path Result is unavailable.");
        internal CharacterFootGroundPathPage PageOrNull => m_Page;
        internal CharacterFootGroundPathState State => Page.State;
        internal CharacterFootGroundPathRejectReason RejectReason => Page.RejectReason;
        internal bool QueryExecutedThisFrame { get; }
        internal int SegmentCount => Page.SegmentCount;
        internal ulong InputIdentity => Page.Input.Identity;
        internal ulong LastLandingEventIdentity =>
            Page.HasInput ? Page.Input.Key.LastLandingEventIdentity : 0;
        internal ulong NextSwingLandingEventIdentity =>
            Page.HasInput ? Page.Input.Key.NextSwingLandingEventIdentity : 0;
        internal Vector3 LastLanding => Page.HasInput ? Page.Input.LastLanding : default;
        internal Vector3 NextSwingLanding =>
            Page.HasInput ? Page.Input.NextSwingLanding : default;
        internal int ContactCount => Page.Contacts.Count;
        internal int EnvelopeVertexCount => Page.Envelope.Count;
        internal bool Accepted => State == CharacterFootGroundPathState.Accepted;

        internal CharacterFootGroundContact ContactAt(int index) =>
            Page.Contacts.ContactAt(index);

        internal CharacterFootGroundEnvelopeVertex EnvelopeVertexAt(int index) =>
            Page.Envelope.VertexAt(index);
    }



    public readonly struct CharacterFootGroundPathDiagnostics
    {
        readonly CharacterFootGroundPathResult m_Result;

        internal CharacterFootGroundPathDiagnostics(
            in CharacterFootGroundPathResult result) => m_Result = result;

        CharacterFootGroundPathPage Page => m_Result.PageOrNull;

        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public CharacterFootGroundPathState State =>
            Page == null ? default : Page.State;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public CharacterFootGroundPathRejectReason RejectReason =>
            Page == null ? default : Page.RejectReason;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public bool QueryExecutedThisFrame =>
            Page != null && m_Result.QueryExecutedThisFrame;
        public bool QueryExecuted => QueryExecutedThisFrame;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int SegmentCount => Page?.SegmentCount ?? 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public ulong InputIdentity => Page == null ? 0 : Page.Input.Identity;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public ulong LastLandingEventIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.Key.LastLandingEventIdentity
                : 0;
        [DiagnosticField]
        [DiagnosticKey("ground-path-next-landing-event-identity")]
        [DiagnosticGroup("ground-path")]
        public ulong NextSwingLandingEventIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.Key.NextSwingLandingEventIdentity
                : 0;
        [DiagnosticField]
        [DiagnosticKey("ground-path-target-available")]
        [DiagnosticGroup("ground-path")]
        public bool TargetAvailable => NextSwingLandingEventIdentity != 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public ulong TrajectoryGeneration =>
            Page != null && Page.HasInput
                ? Page.Input.Key.TrajectoryGeneration
                : 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public ulong AuthorityTick =>
            Page != null && Page.HasInput ? Page.Input.Key.AuthorityTick : 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public string LastFutureBodyTranslationSourceIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.Key.LastFutureBodyTranslationSourceIdentity
                : string.Empty;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public string NextSwingFutureBodyTranslationSourceIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.Key.NextSwingFutureBodyTranslationSourceIdentity
                : string.Empty;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 LastLanding =>
            Page != null && Page.HasInput ? Page.Input.LastLanding : default;
        [DiagnosticField]
        [DiagnosticKey("ground-path-next-landing")]
        [DiagnosticGroup("ground-path")]
        public Vector3 NextSwingLanding =>
            Page != null && Page.HasInput ? Page.Input.NextSwingLanding : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 LastLandingNormal =>
            Page != null && Page.HasInput ? Page.Input.LastLandingNormal : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 NextSwingLandingNormal =>
            Page != null && Page.HasInput ? Page.Input.NextSwingLandingNormal : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int LastLandingSurfaceIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.LastLandingSurfaceIdentity
                : 0;
        [DiagnosticField]
        [DiagnosticKey("ground-path-next-landing-surface-identity")]
        [DiagnosticGroup("ground-path")]
        public int NextSwingLandingSurfaceIdentity =>
            Page != null && Page.HasInput
                ? Page.Input.NextSwingLandingSurfaceIdentity
                : 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public Vector3 ComponentUp =>
            Page != null && Page.HasInput ? Page.Input.ComponentUp : default;
        public CharacterFootGroundPathQueryRequest Query =>
            Page != null && Page.HasInput ? Page.Input.Query : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public float MaximumReachableVerticalEdge =>
            Page != null && Page.HasInput
                ? Page.Input.MaximumReachableVerticalEdge
                : 0f;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int EdgeCount => Page?.Edges.Count ?? 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public bool HasInvalidSegment =>
            Page != null && Page.InvalidSegment.HasValue;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasInvalidSegment))]
        public int FirstInvalidSegmentIndex =>
            HasInvalidSegment ? Page.InvalidSegment.EdgeIndex : -1;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasInvalidSegment))]
        public ulong FirstInvalidSegmentIdentity =>
            HasInvalidSegment ? Page.InvalidSegment.EdgeIdentity : 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasInvalidSegment))]
        public Vector3 FirstInvalidSegmentBottom =>
            HasInvalidSegment ? Page.InvalidSegment.Bottom : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasInvalidSegment))]
        public Vector3 FirstInvalidSegmentTop =>
            HasInvalidSegment ? Page.InvalidSegment.Top : default;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasInvalidSegment))]
        public float FirstInvalidSegmentVerticalDistance =>
            HasInvalidSegment ? Page.InvalidSegment.VerticalDistance : 0f;
        public CharacterFootGroundSurfaceDiagnostics SurfaceCoverage =>
            new(Page?.Contacts.SurfaceCoverage);
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int ContactCount => Page?.Contacts.Count ?? 0;
        [DiagnosticField]
        [DiagnosticGroup("ground-path")]
        public int EnvelopeVertexCount => Page?.Envelope.Count ?? 0;
        public bool Accepted => State == CharacterFootGroundPathState.Accepted;

        [DiagnosticTable("ground-contacts", 1, 64)]
        public CharacterFootGroundContactPage Contacts => Page?.Contacts;

        [DiagnosticTable("ground-envelope", 1, 68)]
        public CharacterFootGroundEnvelopePage Envelope => Page?.Envelope;

        [DiagnosticTable("ground-surfaces", 1, 512)]
        public CharacterFootGroundSurfacePage Surfaces =>
            Page?.Contacts.SurfaceCoverage;

        public CharacterFootGroundContact ContactAt(int index)
        {
            if ((uint)index >= (uint)ContactCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Page.Contacts[index];
        }

        public CharacterFootGroundEnvelopeVertex EnvelopeVertexAt(int index)
        {
            if ((uint)index >= (uint)EnvelopeVertexCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Page.Envelope[index];
        }
    }
}
