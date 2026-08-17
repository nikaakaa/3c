using System;
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
        EnvelopeCapacityExceeded = 8
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
        public Vector3 AxisStart { get; }
        public Vector3 AxisEnd { get; }
        public float Radius { get; }
        public float MaximumAxisSegmentLength { get; }
        public Vector3 Direction { get; }
        public float MaximumDistance { get; }
        public int LayerMask { get; }
        public int SegmentHitCapacity { get; }
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

        public int SegmentIndex { get; }
        public int SurfaceIdentity { get; }
        public ulong CandidateIdentity { get; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
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

    internal sealed class CharacterFootGroundContactPage
    {
        readonly CharacterFootGroundContact[] m_Contacts;

        internal CharacterFootGroundContactPage(int capacity)
        {
            if (capacity < 4 || capacity > 64)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Contacts = new CharacterFootGroundContact[capacity];
        }

        internal int Capacity => m_Contacts.Length;
        internal int Count { get; private set; }

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
        ICharacterFootGroundPathWorldQuery
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
            ulong currentLandingEventIdentity,
            ulong nextLandingEventIdentity,
            ulong trajectoryGeneration,
            ulong authorityTick,
            string currentFutureBodyTranslationSourceIdentity,
            string nextFutureBodyTranslationSourceIdentity,
            int currentLandingSurfaceIdentity,
            int nextLandingSurfaceIdentity,
            Vector3 currentLandingPoint,
            Vector3 nextLandingPoint,
            Vector3 currentLandingNormal,
            Vector3 nextLandingNormal,
            Vector3 componentUp,
            string profileRevision)
        {
            Side = side;
            CurrentLandingEventIdentity = currentLandingEventIdentity;
            NextLandingEventIdentity = nextLandingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            AuthorityTick = authorityTick;
            CurrentFutureBodyTranslationSourceIdentity = currentFutureBodyTranslationSourceIdentity ?? string.Empty;
            NextFutureBodyTranslationSourceIdentity = nextFutureBodyTranslationSourceIdentity ?? string.Empty;
            CurrentLandingSurfaceIdentity = currentLandingSurfaceIdentity;
            NextLandingSurfaceIdentity = nextLandingSurfaceIdentity;
            CurrentLandingPointX = Quantize(currentLandingPoint.x, 1000f);
            CurrentLandingPointY = Quantize(currentLandingPoint.y, 1000f);
            CurrentLandingPointZ = Quantize(currentLandingPoint.z, 1000f);
            NextLandingPointX = Quantize(nextLandingPoint.x, 1000f);
            NextLandingPointY = Quantize(nextLandingPoint.y, 1000f);
            NextLandingPointZ = Quantize(nextLandingPoint.z, 1000f);
            CurrentLandingNormalX = Quantize(currentLandingNormal.x, 10000f);
            CurrentLandingNormalY = Quantize(currentLandingNormal.y, 10000f);
            CurrentLandingNormalZ = Quantize(currentLandingNormal.z, 10000f);
            NextLandingNormalX = Quantize(nextLandingNormal.x, 10000f);
            NextLandingNormalY = Quantize(nextLandingNormal.y, 10000f);
            NextLandingNormalZ = Quantize(nextLandingNormal.z, 10000f);
            ComponentUpX = Quantize(componentUp.x, 10000f);
            ComponentUpY = Quantize(componentUp.y, 10000f);
            ComponentUpZ = Quantize(componentUp.z, 10000f);
            ProfileRevision = profileRevision ?? string.Empty;
        }

        internal CharacterFootSide Side { get; }
        internal ulong CurrentLandingEventIdentity { get; }
        internal ulong NextLandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal ulong AuthorityTick { get; }
        internal string CurrentFutureBodyTranslationSourceIdentity { get; }
        internal string NextFutureBodyTranslationSourceIdentity { get; }
        internal int CurrentLandingSurfaceIdentity { get; }
        internal int NextLandingSurfaceIdentity { get; }
        internal int CurrentLandingPointX { get; }
        internal int CurrentLandingPointY { get; }
        internal int CurrentLandingPointZ { get; }
        internal int NextLandingPointX { get; }
        internal int NextLandingPointY { get; }
        internal int NextLandingPointZ { get; }
        internal int CurrentLandingNormalX { get; }
        internal int CurrentLandingNormalY { get; }
        internal int CurrentLandingNormalZ { get; }
        internal int NextLandingNormalX { get; }
        internal int NextLandingNormalY { get; }
        internal int NextLandingNormalZ { get; }
        internal int ComponentUpX { get; }
        internal int ComponentUpY { get; }
        internal int ComponentUpZ { get; }
        internal string ProfileRevision { get; }

        public bool Equals(CharacterFootGroundPathInputKey other) =>
            Side == other.Side &&
            CurrentLandingEventIdentity == other.CurrentLandingEventIdentity &&
            NextLandingEventIdentity == other.NextLandingEventIdentity &&
            TrajectoryGeneration == other.TrajectoryGeneration &&
            string.Equals(
                CurrentFutureBodyTranslationSourceIdentity,
                other.CurrentFutureBodyTranslationSourceIdentity,
                StringComparison.Ordinal) &&
            string.Equals(
                NextFutureBodyTranslationSourceIdentity,
                other.NextFutureBodyTranslationSourceIdentity,
                StringComparison.Ordinal) &&
            CurrentLandingSurfaceIdentity == other.CurrentLandingSurfaceIdentity &&
            NextLandingSurfaceIdentity == other.NextLandingSurfaceIdentity &&
            CurrentLandingPointX == other.CurrentLandingPointX &&
            CurrentLandingPointY == other.CurrentLandingPointY &&
            CurrentLandingPointZ == other.CurrentLandingPointZ &&
            NextLandingPointX == other.NextLandingPointX &&
            NextLandingPointY == other.NextLandingPointY &&
            NextLandingPointZ == other.NextLandingPointZ &&
            CurrentLandingNormalX == other.CurrentLandingNormalX &&
            CurrentLandingNormalY == other.CurrentLandingNormalY &&
            CurrentLandingNormalZ == other.CurrentLandingNormalZ &&
            NextLandingNormalX == other.NextLandingNormalX &&
            NextLandingNormalY == other.NextLandingNormalY &&
            NextLandingNormalZ == other.NextLandingNormalZ &&
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
                CurrentLandingEventIdentity,
                NextLandingEventIdentity,
                TrajectoryGeneration,
                CurrentFutureBodyTranslationSourceIdentity,
                NextFutureBodyTranslationSourceIdentity,
                CurrentLandingSurfaceIdentity,
                NextLandingSurfaceIdentity);
            return HashCode.Combine(
                first,
                CurrentLandingPointX,
                CurrentLandingPointY,
                CurrentLandingPointZ,
                NextLandingPointX,
                NextLandingPointY,
                NextLandingPointZ);
        }

        static int Quantize(float value, float scale) =>
            Mathf.RoundToInt(value * scale);
    }

    internal readonly struct CharacterFootGroundPathInput
    {
        internal CharacterFootGroundPathInput(
            ulong identity,
            in CharacterFootGroundPathInputKey key,
            Vector3 currentLanding,
            Vector3 nextLanding,
            Vector3 currentLandingNormal,
            Vector3 nextLandingNormal,
            int currentLandingSurfaceIdentity,
            int nextLandingSurfaceIdentity,
            Vector3 componentUp,
            in CharacterFootGroundPathQueryRequest query)
        {
            Identity = identity;
            Key = key;
            CurrentLanding = currentLanding;
            NextLanding = nextLanding;
            CurrentLandingNormal = currentLandingNormal;
            NextLandingNormal = nextLandingNormal;
            CurrentLandingSurfaceIdentity = currentLandingSurfaceIdentity;
            NextLandingSurfaceIdentity = nextLandingSurfaceIdentity;
            ComponentUp = componentUp;
            Query = query;
        }

        internal ulong Identity { get; }
        internal CharacterFootGroundPathInputKey Key { get; }
        internal Vector3 CurrentLanding { get; }
        internal Vector3 NextLanding { get; }
        internal Vector3 CurrentLandingNormal { get; }
        internal Vector3 NextLandingNormal { get; }
        internal int CurrentLandingSurfaceIdentity { get; }
        internal int NextLandingSurfaceIdentity { get; }
        internal Vector3 ComponentUp { get; }
        internal CharacterFootGroundPathQueryRequest Query { get; }
        internal bool IsValid => Identity != 0 && Query.IsValid;
    }

    internal static class CharacterFootGroundPathInputBuilder
    {
        internal static CharacterFootGroundPathInputKey BuildKey(
            CharacterFootSide side,
            in CharacterFootGroundPathLanding currentLanding,
            CharacterFootLandingPredictionFootDiagnostics targetLanding,
            ulong authorityTick,
            Vector3 componentUp,
            string profileRevision) =>
            new CharacterFootGroundPathInputKey(
                side,
                currentLanding.LandingEventIdentity,
                targetLanding.LandingEventIdentity,
                currentLanding.TrajectoryGeneration,
                authorityTick,
                currentLanding.FutureBodyTranslationSourceIdentity,
                targetLanding.FutureBodyTranslationSourceIdentity,
                currentLanding.SurfaceIdentity,
                targetLanding.SurfaceIdentity,
                currentLanding.Point,
                targetLanding.LandingPoint,
                currentLanding.Normal,
                targetLanding.LandingNormal,
                componentUp,
                profileRevision);

        internal static bool TryBuild(
            in CharacterFootGroundPathInputKey key,
            Vector3 currentLanding,
            Vector3 nextLanding,
            Vector3 currentLandingNormal,
            Vector3 nextLandingNormal,
            int currentLandingSurfaceIdentity,
            int nextLandingSurfaceIdentity,
            Vector3 componentUp,
            in CharacterFootGroundDetectionSettings settings,
            out CharacterFootGroundPathInput input)
        {
            if (!Finite(currentLanding) || !Finite(nextLanding) ||
                !Finite(currentLandingNormal) || currentLandingNormal.sqrMagnitude <= 0.000001f ||
                !Finite(nextLandingNormal) || nextLandingNormal.sqrMagnitude <= 0.000001f ||
                nextLandingSurfaceIdentity == 0 ||
                !Finite(componentUp) ||
                componentUp.sqrMagnitude <= 0.000001f)
            {
                input = default;
                return false;
            }
            Vector3 up = componentUp.normalized;
            var query = new CharacterFootGroundPathQueryRequest(
                key.Side,
                currentLanding + up * settings.CastAbove,
                nextLanding + up * settings.CastAbove,
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
            ulong identity = ComputeIdentity(in key, in query);
            input = new CharacterFootGroundPathInput(
                identity,
                in key,
                currentLanding,
                nextLanding,
                currentLandingNormal.normalized,
                nextLandingNormal.normalized,
                currentLandingSurfaceIdentity,
                nextLandingSurfaceIdentity,
                up,
                in query);
            return true;
        }

        static ulong ComputeIdentity(
            in CharacterFootGroundPathInputKey key,
            in CharacterFootGroundPathQueryRequest query)
        {
            ulong hash = 14695981039346656037UL;
            Add(ref hash, (ulong)key.Side);
            Add(ref hash, key.CurrentLandingEventIdentity);
            Add(ref hash, key.NextLandingEventIdentity);
            Add(ref hash, key.TrajectoryGeneration);
            Add(ref hash, key.AuthorityTick);
            Add(ref hash, key.CurrentFutureBodyTranslationSourceIdentity);
            Add(ref hash, key.NextFutureBodyTranslationSourceIdentity);
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingSurfaceIdentity));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingSurfaceIdentity));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingPointX));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingPointY));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingPointZ));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingPointX));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingPointY));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingPointZ));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingNormalX));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingNormalY));
            Add(ref hash, unchecked((ulong)(uint)key.CurrentLandingNormalZ));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingNormalX));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingNormalY));
            Add(ref hash, unchecked((ulong)(uint)key.NextLandingNormalZ));
            Add(ref hash, key.ProfileRevision);
            Add(ref hash, unchecked((ulong)(uint)Mathf.RoundToInt(query.Radius * 10000f)));
            Add(ref hash, unchecked((ulong)(uint)Mathf.RoundToInt(query.MaximumAxisSegmentLength * 10000f)));
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
        internal CharacterFootGroundPathPage(int contactCapacity)
        {
            Contacts = new CharacterFootGroundContactPage(contactCapacity);
            Envelope = new CharacterFootGroundEnvelopePage(contactCapacity);
        }

        internal CharacterFootGroundPathState State { get; private set; }
        internal CharacterFootGroundPathRejectReason RejectReason { get; private set; }
        internal bool QueryExecuted { get; private set; }
        internal int SegmentCount { get; private set; }
        internal CharacterFootGroundPathInput Input { get; private set; }
        internal CharacterFootGroundContactPage Contacts { get; }
        internal CharacterFootGroundEnvelopePage Envelope { get; }
        internal bool HasInput => Input.IsValid;

        internal void SetRejected(
            CharacterFootGroundPathRejectReason reason,
            bool queryExecuted,
            int segmentCount,
            in CharacterFootGroundPathInput input)
        {
            if (reason == CharacterFootGroundPathRejectReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            State = CharacterFootGroundPathState.Rejected;
            RejectReason = reason;
            QueryExecuted = queryExecuted;
            SegmentCount = segmentCount;
            Input = input;
            if (!queryExecuted)
                Contacts.Clear();
        }

        internal void SetAccepted(
            int segmentCount,
            in CharacterFootGroundPathInput input)
        {
            if (!input.IsValid || Contacts.Count <= 0 || Envelope.Count < 2 ||
                segmentCount <= 0)
                throw new ArgumentException("Ground Path accepted page is invalid.");
            State = CharacterFootGroundPathState.Accepted;
            RejectReason = CharacterFootGroundPathRejectReason.None;
            QueryExecuted = true;
            SegmentCount = segmentCount;
            Input = input;
        }

        internal void Clear()
        {
            State = default;
            RejectReason = default;
            QueryExecuted = false;
            SegmentCount = 0;
            Input = default;
            Contacts.Clear();
            Envelope.Clear();
        }
    }

    internal sealed class CharacterFootGroundPathFootState
    {
        readonly CharacterFootGroundPathPage m_First;
        readonly CharacterFootGroundPathPage m_Second;

        CharacterFootGroundPathPage m_Committed;
        CharacterFootGroundPathPage m_Pending;
        CharacterFootGroundPathLanding m_CommittedLanding;
        CharacterFootGroundPathLanding m_PendingLanding;
        bool m_HasCommitted;
        bool m_HasPending;
        bool m_HasCommittedLanding;
        bool m_HasPendingLanding;

        internal CharacterFootGroundPathFootState(int contactCapacity)
        {
            m_First = new CharacterFootGroundPathPage(contactCapacity);
            m_Second = new CharacterFootGroundPathPage(contactCapacity);
            EnvelopeWorkspace = new CharacterFootGroundEnvelopeWorkspace(contactCapacity);
        }

        internal CharacterFootGroundEnvelopeWorkspace EnvelopeWorkspace { get; }

        internal bool HasCurrentLanding => m_HasPendingLanding || m_HasCommittedLanding;
        internal CharacterFootGroundPathLanding CurrentLanding =>
            m_HasPendingLanding ? m_PendingLanding : m_CommittedLanding;
        internal bool HasCommittedInput => m_HasCommitted && m_Committed.HasInput;
        internal CharacterFootGroundPathInputKey CommittedKey => m_Committed.Input.Key;
        internal bool CommittedAccepted =>
            m_HasCommitted && m_Committed.State == CharacterFootGroundPathState.Accepted;
        internal ulong CommittedAuthorityTick =>
            HasCommittedInput ? m_Committed.Input.Key.AuthorityTick : 0;

        internal void SetPendingLanding(in CharacterFootGroundPathLanding landing)
        {
            m_PendingLanding = landing;
            m_HasPendingLanding = true;
        }

        internal CharacterFootGroundPathPage BeginPending()
        {
            if (m_HasPending)
                throw new InvalidOperationException("Ground Path already has a pending page.");
            m_Pending = m_HasCommitted && ReferenceEquals(m_Committed, m_First)
                ? m_Second
                : m_First;
            m_Pending.Clear();
            m_HasPending = true;
            return m_Pending;
        }

        internal CharacterFootGroundPathPage ReuseCommitted()
        {
            if (m_HasPending || !m_HasCommitted)
                throw new InvalidOperationException("Ground Path committed page is unavailable.");
            m_Pending = m_Committed;
            m_HasPending = true;
            return m_Pending;
        }

        internal void Seal()
        {
            if (!m_HasPending)
                throw new InvalidOperationException("Ground Path has no pending page.");
            m_Committed = m_Pending;
            m_HasCommitted = true;
            m_Pending = null;
            m_HasPending = false;
            if (m_HasPendingLanding)
            {
                m_CommittedLanding = m_PendingLanding;
                m_HasCommittedLanding = true;
                m_PendingLanding = default;
                m_HasPendingLanding = false;
            }
        }

        internal void Discard()
        {
            if (m_HasPending)
            {
                if (!ReferenceEquals(m_Pending, m_Committed))
                    m_Pending.Clear();
                m_Pending = null;
                m_HasPending = false;
            }
            m_PendingLanding = default;
            m_HasPendingLanding = false;
        }

        internal void Reset()
        {
            m_First.Clear();
            m_Second.Clear();
            m_Committed = null;
            m_Pending = null;
            m_CommittedLanding = default;
            m_PendingLanding = default;
            m_HasCommitted = false;
            m_HasPending = false;
            m_HasCommittedLanding = false;
            m_HasPendingLanding = false;
            EnvelopeWorkspace.Clear();
        }
    }
    readonly struct CharacterFootGroundPathDiagnosticContacts
    {
        readonly CharacterFootGroundContact m_0;
        readonly CharacterFootGroundContact m_1;
        readonly CharacterFootGroundContact m_2;
        readonly CharacterFootGroundContact m_3;
        readonly CharacterFootGroundContact m_4;
        readonly CharacterFootGroundContact m_5;
        readonly CharacterFootGroundContact m_6;
        readonly CharacterFootGroundContact m_7;
        readonly CharacterFootGroundContact m_8;
        readonly CharacterFootGroundContact m_9;
        readonly CharacterFootGroundContact m_10;
        readonly CharacterFootGroundContact m_11;
        readonly CharacterFootGroundContact m_12;
        readonly CharacterFootGroundContact m_13;
        readonly CharacterFootGroundContact m_14;
        readonly CharacterFootGroundContact m_15;
        readonly CharacterFootGroundContact m_16;
        readonly CharacterFootGroundContact m_17;
        readonly CharacterFootGroundContact m_18;
        readonly CharacterFootGroundContact m_19;
        readonly CharacterFootGroundContact m_20;
        readonly CharacterFootGroundContact m_21;
        readonly CharacterFootGroundContact m_22;
        readonly CharacterFootGroundContact m_23;
        readonly CharacterFootGroundContact m_24;
        readonly CharacterFootGroundContact m_25;
        readonly CharacterFootGroundContact m_26;
        readonly CharacterFootGroundContact m_27;
        readonly CharacterFootGroundContact m_28;
        readonly CharacterFootGroundContact m_29;
        readonly CharacterFootGroundContact m_30;
        readonly CharacterFootGroundContact m_31;
        readonly CharacterFootGroundContact m_32;
        readonly CharacterFootGroundContact m_33;
        readonly CharacterFootGroundContact m_34;
        readonly CharacterFootGroundContact m_35;
        readonly CharacterFootGroundContact m_36;
        readonly CharacterFootGroundContact m_37;
        readonly CharacterFootGroundContact m_38;
        readonly CharacterFootGroundContact m_39;
        readonly CharacterFootGroundContact m_40;
        readonly CharacterFootGroundContact m_41;
        readonly CharacterFootGroundContact m_42;
        readonly CharacterFootGroundContact m_43;
        readonly CharacterFootGroundContact m_44;
        readonly CharacterFootGroundContact m_45;
        readonly CharacterFootGroundContact m_46;
        readonly CharacterFootGroundContact m_47;
        readonly CharacterFootGroundContact m_48;
        readonly CharacterFootGroundContact m_49;
        readonly CharacterFootGroundContact m_50;
        readonly CharacterFootGroundContact m_51;
        readonly CharacterFootGroundContact m_52;
        readonly CharacterFootGroundContact m_53;
        readonly CharacterFootGroundContact m_54;
        readonly CharacterFootGroundContact m_55;
        readonly CharacterFootGroundContact m_56;
        readonly CharacterFootGroundContact m_57;
        readonly CharacterFootGroundContact m_58;
        readonly CharacterFootGroundContact m_59;
        readonly CharacterFootGroundContact m_60;
        readonly CharacterFootGroundContact m_61;
        readonly CharacterFootGroundContact m_62;
        readonly CharacterFootGroundContact m_63;

        internal CharacterFootGroundPathDiagnosticContacts(
            CharacterFootGroundContactPage page)
        {
            Count = page.Count;
            m_0 = Read(page, 0);
            m_1 = Read(page, 1);
            m_2 = Read(page, 2);
            m_3 = Read(page, 3);
            m_4 = Read(page, 4);
            m_5 = Read(page, 5);
            m_6 = Read(page, 6);
            m_7 = Read(page, 7);
            m_8 = Read(page, 8);
            m_9 = Read(page, 9);
            m_10 = Read(page, 10);
            m_11 = Read(page, 11);
            m_12 = Read(page, 12);
            m_13 = Read(page, 13);
            m_14 = Read(page, 14);
            m_15 = Read(page, 15);
            m_16 = Read(page, 16);
            m_17 = Read(page, 17);
            m_18 = Read(page, 18);
            m_19 = Read(page, 19);
            m_20 = Read(page, 20);
            m_21 = Read(page, 21);
            m_22 = Read(page, 22);
            m_23 = Read(page, 23);
            m_24 = Read(page, 24);
            m_25 = Read(page, 25);
            m_26 = Read(page, 26);
            m_27 = Read(page, 27);
            m_28 = Read(page, 28);
            m_29 = Read(page, 29);
            m_30 = Read(page, 30);
            m_31 = Read(page, 31);
            m_32 = Read(page, 32);
            m_33 = Read(page, 33);
            m_34 = Read(page, 34);
            m_35 = Read(page, 35);
            m_36 = Read(page, 36);
            m_37 = Read(page, 37);
            m_38 = Read(page, 38);
            m_39 = Read(page, 39);
            m_40 = Read(page, 40);
            m_41 = Read(page, 41);
            m_42 = Read(page, 42);
            m_43 = Read(page, 43);
            m_44 = Read(page, 44);
            m_45 = Read(page, 45);
            m_46 = Read(page, 46);
            m_47 = Read(page, 47);
            m_48 = Read(page, 48);
            m_49 = Read(page, 49);
            m_50 = Read(page, 50);
            m_51 = Read(page, 51);
            m_52 = Read(page, 52);
            m_53 = Read(page, 53);
            m_54 = Read(page, 54);
            m_55 = Read(page, 55);
            m_56 = Read(page, 56);
            m_57 = Read(page, 57);
            m_58 = Read(page, 58);
            m_59 = Read(page, 59);
            m_60 = Read(page, 60);
            m_61 = Read(page, 61);
            m_62 = Read(page, 62);
            m_63 = Read(page, 63);
        }

        internal int Count { get; }

        internal CharacterFootGroundContact ContactAt(int index) => index switch
        {
            0 => m_0,
            1 => m_1,
            2 => m_2,
            3 => m_3,
            4 => m_4,
            5 => m_5,
            6 => m_6,
            7 => m_7,
            8 => m_8,
            9 => m_9,
            10 => m_10,
            11 => m_11,
            12 => m_12,
            13 => m_13,
            14 => m_14,
            15 => m_15,
            16 => m_16,
            17 => m_17,
            18 => m_18,
            19 => m_19,
            20 => m_20,
            21 => m_21,
            22 => m_22,
            23 => m_23,
            24 => m_24,
            25 => m_25,
            26 => m_26,
            27 => m_27,
            28 => m_28,
            29 => m_29,
            30 => m_30,
            31 => m_31,
            32 => m_32,
            33 => m_33,
            34 => m_34,
            35 => m_35,
            36 => m_36,
            37 => m_37,
            38 => m_38,
            39 => m_39,
            40 => m_40,
            41 => m_41,
            42 => m_42,
            43 => m_43,
            44 => m_44,
            45 => m_45,
            46 => m_46,
            47 => m_47,
            48 => m_48,
            49 => m_49,
            50 => m_50,
            51 => m_51,
            52 => m_52,
            53 => m_53,
            54 => m_54,
            55 => m_55,
            56 => m_56,
            57 => m_57,
            58 => m_58,
            59 => m_59,
            60 => m_60,
            61 => m_61,
            62 => m_62,
            63 => m_63,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        static CharacterFootGroundContact Read(
            CharacterFootGroundContactPage page,
            int index) =>
            index < page.Count ? page.ContactAt(index) : default;
    }

    readonly struct CharacterFootGroundEnvelopeDiagnosticVertices
    {
        readonly CharacterFootGroundEnvelopeVertex m_0;
        readonly CharacterFootGroundEnvelopeVertex m_1;
        readonly CharacterFootGroundEnvelopeVertex m_2;
        readonly CharacterFootGroundEnvelopeVertex m_3;
        readonly CharacterFootGroundEnvelopeVertex m_4;
        readonly CharacterFootGroundEnvelopeVertex m_5;
        readonly CharacterFootGroundEnvelopeVertex m_6;
        readonly CharacterFootGroundEnvelopeVertex m_7;
        readonly CharacterFootGroundEnvelopeVertex m_8;
        readonly CharacterFootGroundEnvelopeVertex m_9;
        readonly CharacterFootGroundEnvelopeVertex m_10;
        readonly CharacterFootGroundEnvelopeVertex m_11;
        readonly CharacterFootGroundEnvelopeVertex m_12;
        readonly CharacterFootGroundEnvelopeVertex m_13;
        readonly CharacterFootGroundEnvelopeVertex m_14;
        readonly CharacterFootGroundEnvelopeVertex m_15;
        readonly CharacterFootGroundEnvelopeVertex m_16;
        readonly CharacterFootGroundEnvelopeVertex m_17;
        readonly CharacterFootGroundEnvelopeVertex m_18;
        readonly CharacterFootGroundEnvelopeVertex m_19;
        readonly CharacterFootGroundEnvelopeVertex m_20;
        readonly CharacterFootGroundEnvelopeVertex m_21;
        readonly CharacterFootGroundEnvelopeVertex m_22;
        readonly CharacterFootGroundEnvelopeVertex m_23;
        readonly CharacterFootGroundEnvelopeVertex m_24;
        readonly CharacterFootGroundEnvelopeVertex m_25;
        readonly CharacterFootGroundEnvelopeVertex m_26;
        readonly CharacterFootGroundEnvelopeVertex m_27;
        readonly CharacterFootGroundEnvelopeVertex m_28;
        readonly CharacterFootGroundEnvelopeVertex m_29;
        readonly CharacterFootGroundEnvelopeVertex m_30;
        readonly CharacterFootGroundEnvelopeVertex m_31;
        readonly CharacterFootGroundEnvelopeVertex m_32;
        readonly CharacterFootGroundEnvelopeVertex m_33;
        readonly CharacterFootGroundEnvelopeVertex m_34;
        readonly CharacterFootGroundEnvelopeVertex m_35;
        readonly CharacterFootGroundEnvelopeVertex m_36;
        readonly CharacterFootGroundEnvelopeVertex m_37;
        readonly CharacterFootGroundEnvelopeVertex m_38;
        readonly CharacterFootGroundEnvelopeVertex m_39;
        readonly CharacterFootGroundEnvelopeVertex m_40;
        readonly CharacterFootGroundEnvelopeVertex m_41;
        readonly CharacterFootGroundEnvelopeVertex m_42;
        readonly CharacterFootGroundEnvelopeVertex m_43;
        readonly CharacterFootGroundEnvelopeVertex m_44;
        readonly CharacterFootGroundEnvelopeVertex m_45;
        readonly CharacterFootGroundEnvelopeVertex m_46;
        readonly CharacterFootGroundEnvelopeVertex m_47;
        readonly CharacterFootGroundEnvelopeVertex m_48;
        readonly CharacterFootGroundEnvelopeVertex m_49;
        readonly CharacterFootGroundEnvelopeVertex m_50;
        readonly CharacterFootGroundEnvelopeVertex m_51;
        readonly CharacterFootGroundEnvelopeVertex m_52;
        readonly CharacterFootGroundEnvelopeVertex m_53;
        readonly CharacterFootGroundEnvelopeVertex m_54;
        readonly CharacterFootGroundEnvelopeVertex m_55;
        readonly CharacterFootGroundEnvelopeVertex m_56;
        readonly CharacterFootGroundEnvelopeVertex m_57;
        readonly CharacterFootGroundEnvelopeVertex m_58;
        readonly CharacterFootGroundEnvelopeVertex m_59;
        readonly CharacterFootGroundEnvelopeVertex m_60;
        readonly CharacterFootGroundEnvelopeVertex m_61;
        readonly CharacterFootGroundEnvelopeVertex m_62;
        readonly CharacterFootGroundEnvelopeVertex m_63;
        readonly CharacterFootGroundEnvelopeVertex m_64;
        readonly CharacterFootGroundEnvelopeVertex m_65;
        readonly CharacterFootGroundEnvelopeVertex m_66;
        readonly CharacterFootGroundEnvelopeVertex m_67;

        internal CharacterFootGroundEnvelopeDiagnosticVertices(
            CharacterFootGroundEnvelopePage page)
        {
            Count = page.Count;
            m_0 = Read(page, 0);
            m_1 = Read(page, 1);
            m_2 = Read(page, 2);
            m_3 = Read(page, 3);
            m_4 = Read(page, 4);
            m_5 = Read(page, 5);
            m_6 = Read(page, 6);
            m_7 = Read(page, 7);
            m_8 = Read(page, 8);
            m_9 = Read(page, 9);
            m_10 = Read(page, 10);
            m_11 = Read(page, 11);
            m_12 = Read(page, 12);
            m_13 = Read(page, 13);
            m_14 = Read(page, 14);
            m_15 = Read(page, 15);
            m_16 = Read(page, 16);
            m_17 = Read(page, 17);
            m_18 = Read(page, 18);
            m_19 = Read(page, 19);
            m_20 = Read(page, 20);
            m_21 = Read(page, 21);
            m_22 = Read(page, 22);
            m_23 = Read(page, 23);
            m_24 = Read(page, 24);
            m_25 = Read(page, 25);
            m_26 = Read(page, 26);
            m_27 = Read(page, 27);
            m_28 = Read(page, 28);
            m_29 = Read(page, 29);
            m_30 = Read(page, 30);
            m_31 = Read(page, 31);
            m_32 = Read(page, 32);
            m_33 = Read(page, 33);
            m_34 = Read(page, 34);
            m_35 = Read(page, 35);
            m_36 = Read(page, 36);
            m_37 = Read(page, 37);
            m_38 = Read(page, 38);
            m_39 = Read(page, 39);
            m_40 = Read(page, 40);
            m_41 = Read(page, 41);
            m_42 = Read(page, 42);
            m_43 = Read(page, 43);
            m_44 = Read(page, 44);
            m_45 = Read(page, 45);
            m_46 = Read(page, 46);
            m_47 = Read(page, 47);
            m_48 = Read(page, 48);
            m_49 = Read(page, 49);
            m_50 = Read(page, 50);
            m_51 = Read(page, 51);
            m_52 = Read(page, 52);
            m_53 = Read(page, 53);
            m_54 = Read(page, 54);
            m_55 = Read(page, 55);
            m_56 = Read(page, 56);
            m_57 = Read(page, 57);
            m_58 = Read(page, 58);
            m_59 = Read(page, 59);
            m_60 = Read(page, 60);
            m_61 = Read(page, 61);
            m_62 = Read(page, 62);
            m_63 = Read(page, 63);
            m_64 = Read(page, 64);
            m_65 = Read(page, 65);
            m_66 = Read(page, 66);
            m_67 = Read(page, 67);
        }

        internal int Count { get; }

        internal CharacterFootGroundEnvelopeVertex VertexAt(int index) => index switch
        {
            0 => m_0,
            1 => m_1,
            2 => m_2,
            3 => m_3,
            4 => m_4,
            5 => m_5,
            6 => m_6,
            7 => m_7,
            8 => m_8,
            9 => m_9,
            10 => m_10,
            11 => m_11,
            12 => m_12,
            13 => m_13,
            14 => m_14,
            15 => m_15,
            16 => m_16,
            17 => m_17,
            18 => m_18,
            19 => m_19,
            20 => m_20,
            21 => m_21,
            22 => m_22,
            23 => m_23,
            24 => m_24,
            25 => m_25,
            26 => m_26,
            27 => m_27,
            28 => m_28,
            29 => m_29,
            30 => m_30,
            31 => m_31,
            32 => m_32,
            33 => m_33,
            34 => m_34,
            35 => m_35,
            36 => m_36,
            37 => m_37,
            38 => m_38,
            39 => m_39,
            40 => m_40,
            41 => m_41,
            42 => m_42,
            43 => m_43,
            44 => m_44,
            45 => m_45,
            46 => m_46,
            47 => m_47,
            48 => m_48,
            49 => m_49,
            50 => m_50,
            51 => m_51,
            52 => m_52,
            53 => m_53,
            54 => m_54,
            55 => m_55,
            56 => m_56,
            57 => m_57,
            58 => m_58,
            59 => m_59,
            60 => m_60,
            61 => m_61,
            62 => m_62,
            63 => m_63,
            64 => m_64,
            65 => m_65,
            66 => m_66,
            67 => m_67,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        static CharacterFootGroundEnvelopeVertex Read(
            CharacterFootGroundEnvelopePage page,
            int index) =>
            index < page.Count ? page.VertexAt(index) : default;
    }

    public readonly struct CharacterFootGroundPathDiagnostics
    {
        readonly CharacterFootGroundPathDiagnosticContacts m_Contacts;
        readonly CharacterFootGroundEnvelopeDiagnosticVertices m_Envelope;

        internal CharacterFootGroundPathDiagnostics(
            CharacterFootGroundPathPage page,
            bool queryExecutedThisFrame)
        {
            State = page.State;
            RejectReason = page.RejectReason;
            QueryExecutedThisFrame = queryExecutedThisFrame;
            SegmentCount = page.SegmentCount;
            InputIdentity = page.Input.Identity;
            CurrentLandingEventIdentity = page.HasInput
                ? page.Input.Key.CurrentLandingEventIdentity
                : 0;
            NextLandingEventIdentity = page.HasInput
                ? page.Input.Key.NextLandingEventIdentity
                : 0;
            TrajectoryGeneration = page.HasInput
                ? page.Input.Key.TrajectoryGeneration
                : 0;
            AuthorityTick = page.HasInput
                ? page.Input.Key.AuthorityTick
                : 0;
            CurrentFutureBodyTranslationSourceIdentity = page.HasInput
                ? page.Input.Key.CurrentFutureBodyTranslationSourceIdentity
                : string.Empty;
            NextFutureBodyTranslationSourceIdentity = page.HasInput
                ? page.Input.Key.NextFutureBodyTranslationSourceIdentity
                : string.Empty;
            CurrentLanding = page.HasInput
                ? page.Input.CurrentLanding
                : default;
            NextLanding = page.HasInput
                ? page.Input.NextLanding
                : default;
            CurrentLandingNormal = page.HasInput
                ? page.Input.CurrentLandingNormal
                : default;
            NextLandingNormal = page.HasInput
                ? page.Input.NextLandingNormal
                : default;
            CurrentLandingSurfaceIdentity = page.HasInput
                ? page.Input.CurrentLandingSurfaceIdentity
                : 0;
            NextLandingSurfaceIdentity = page.HasInput
                ? page.Input.NextLandingSurfaceIdentity
                : 0;
            ComponentUp = page.HasInput
                ? page.Input.ComponentUp
                : default;
            Query = page.HasInput
                ? page.Input.Query
                : default;
            m_Contacts = new CharacterFootGroundPathDiagnosticContacts(page.Contacts);
            m_Envelope = new CharacterFootGroundEnvelopeDiagnosticVertices(page.Envelope);
        }

        public CharacterFootGroundPathState State { get; }
        public CharacterFootGroundPathRejectReason RejectReason { get; }
        public bool QueryExecutedThisFrame { get; }
        public bool QueryExecuted => QueryExecutedThisFrame;
        public int SegmentCount { get; }
        public ulong InputIdentity { get; }
        public ulong CurrentLandingEventIdentity { get; }
        public ulong NextLandingEventIdentity { get; }
        public ulong TrajectoryGeneration { get; }
        public ulong AuthorityTick { get; }
        public string CurrentFutureBodyTranslationSourceIdentity { get; }
        public string NextFutureBodyTranslationSourceIdentity { get; }
        public Vector3 CurrentLanding { get; }
        public Vector3 NextLanding { get; }
        public Vector3 CurrentLandingNormal { get; }
        public Vector3 NextLandingNormal { get; }
        public int CurrentLandingSurfaceIdentity { get; }
        public int NextLandingSurfaceIdentity { get; }
        public Vector3 ComponentUp { get; }
        public CharacterFootGroundPathQueryRequest Query { get; }
        public int ContactCount => m_Contacts.Count;
        public int EnvelopeVertexCount => m_Envelope.Count;
        public bool Accepted => State == CharacterFootGroundPathState.Accepted;

        public CharacterFootGroundContact ContactAt(int index)
        {
            if ((uint)index >= (uint)m_Contacts.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Contacts.ContactAt(index);
        }

        public CharacterFootGroundEnvelopeVertex EnvelopeVertexAt(int index)
        {
            if ((uint)index >= (uint)m_Envelope.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Envelope.VertexAt(index);
        }
    }
}
