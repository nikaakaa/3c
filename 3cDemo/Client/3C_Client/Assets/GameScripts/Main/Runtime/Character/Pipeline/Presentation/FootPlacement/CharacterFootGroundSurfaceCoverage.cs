using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootGroundSurfaceState : byte
    {
        None = 0,
        Ready = 1,
        GeometryUnavailable = 2,
        UnsupportedGeometry = 3,
        GeometryChanged = 4,
        CapacityExceeded = 5
    }

    public readonly struct CharacterFootGroundSurfaceSegment
    {
        internal CharacterFootGroundSurfaceSegment(
            int surfaceIdentity,
            int faceIdentity,
            Vector2 start,
            Vector2 end)
        {
            SurfaceIdentity = surfaceIdentity;
            FaceIdentity = faceIdentity;
            Start = start;
            End = end;
        }

        public int SurfaceIdentity { get; }
        public int FaceIdentity { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
    }

    internal sealed class CharacterFootGroundSurfacePage
    {
        internal const int SegmentsPerContact = 8;
        readonly CharacterFootGroundSurfaceSegment[] m_Segments;
        CharacterFootGroundPathQueryRequest m_Query;

        internal CharacterFootGroundSurfacePage(int contactCapacity)
        {
            m_Segments = new CharacterFootGroundSurfaceSegment[
                checked(contactCapacity * SegmentsPerContact)];
        }

        internal int Capacity => m_Segments.Length;
        internal int Count { get; private set; }
        internal CharacterFootGroundSurfaceState State { get; private set; }
        internal Vector3 Origin { get; private set; }
        internal Vector3 Forward { get; private set; }
        internal Vector3 Up { get; private set; }
        internal Vector3 Right { get; private set; }
        internal float Length { get; private set; }
        internal float AxisRise { get; private set; }
        internal ulong WorldRevision { get; private set; }
        internal bool IsReady => State == CharacterFootGroundSurfaceState.Ready;

        internal bool Begin(
            in CharacterFootGroundPathQueryRequest query,
            ulong worldRevision)
        {
            Clear();
            m_Query = query;
            WorldRevision = worldRevision;
            Origin = query.AxisStart;
            Up = -query.Direction.normalized;
            Vector3 delta = query.AxisEnd - query.AxisStart;
            Vector3 planar = Vector3.ProjectOnPlane(delta, Up);
            Length = planar.magnitude;
            AxisRise = Vector3.Dot(delta, Up);
            if (!query.IsValid || worldRevision == 0 ||
                !float.IsFinite(Length) || Length <= 0.0001f ||
                !float.IsFinite(AxisRise))
            {
                State = CharacterFootGroundSurfaceState.GeometryUnavailable;
                return false;
            }
            Forward = planar / Length;
            Right = Vector3.Cross(Up, Forward).normalized;
            return true;
        }

        internal bool Matches(in CharacterFootGroundPathQueryRequest query) =>
            m_Query.AxisStart.Equals(query.AxisStart) &&
            m_Query.AxisEnd.Equals(query.AxisEnd) &&
            m_Query.Direction.Equals(query.Direction) &&
            m_Query.Radius.Equals(query.Radius) &&
            m_Query.MaximumDistance.Equals(query.MaximumDistance) &&
            m_Query.LayerMask == query.LayerMask;

        internal CharacterFootGroundSurfaceSegment SegmentAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Segments[index];
        }

        internal bool TryAdd(
            int surfaceIdentity,
            int faceIdentity,
            Vector2 first,
            Vector2 second)
        {
            if (!Finite(first) || !Finite(second) || surfaceIdentity == 0)
                return false;
            Vector2 start = first.x <= second.x ? first : second;
            Vector2 end = first.x <= second.x ? second : first;
            if (start.x == end.x)
                start.y = end.y = Mathf.Max(start.y, end.y);
            for (int i = 0; i < Count; i++)
            {
                CharacterFootGroundSurfaceSegment value = m_Segments[i];
                if (value.SurfaceIdentity == surfaceIdentity &&
                    value.FaceIdentity == faceIdentity &&
                    value.Start.Equals(start) && value.End.Equals(end))
                    return true;
            }
            if (Count >= Capacity)
                return false;
            m_Segments[Count++] = new CharacterFootGroundSurfaceSegment(
                surfaceIdentity, faceIdentity, start, end);
            return true;
        }

        internal void Complete()
        {
            for (int i = 1; i < Count; i++)
            {
                CharacterFootGroundSurfaceSegment value = m_Segments[i];
                int index = i;
                while (index > 0 && Compare(value, m_Segments[index - 1]) < 0)
                {
                    m_Segments[index] = m_Segments[index - 1];
                    index--;
                }
                m_Segments[index] = value;
            }
            State = CharacterFootGroundSurfaceState.Ready;
        }

        internal void Fail(CharacterFootGroundSurfaceState state)
        {
            if (state == CharacterFootGroundSurfaceState.None ||
                state == CharacterFootGroundSurfaceState.Ready)
                throw new ArgumentOutOfRangeException(nameof(state));
            State = state;
        }

        internal void Clear()
        {
            Array.Clear(m_Segments, 0, Count);
            Count = 0;
            State = default;
            Origin = default;
            Forward = default;
            Up = default;
            Right = default;
            Length = 0f;
            AxisRise = 0f;
            WorldRevision = 0;
            m_Query = default;
        }

        internal Vector3 ResolveWorldPoint(float distance, float height) =>
            Origin + Forward * distance + Up * height;

        static int Compare(
            CharacterFootGroundSurfaceSegment left,
            CharacterFootGroundSurfaceSegment right)
        {
            int value = left.SurfaceIdentity.CompareTo(right.SurfaceIdentity);
            if (value != 0) return value;
            value = left.FaceIdentity.CompareTo(right.FaceIdentity);
            if (value != 0) return value;
            value = left.Start.x.CompareTo(right.Start.x);
            if (value != 0) return value;
            value = left.End.x.CompareTo(right.End.x);
            if (value != 0) return value;
            value = left.Start.y.CompareTo(right.Start.y);
            return value != 0 ? value : left.End.y.CompareTo(right.End.y);
        }

        static bool Finite(Vector2 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y);
    }

    public readonly struct CharacterFootGroundSurfaceDiagnostics
    {
        readonly CharacterFootGroundSurfaceSegment[] m_Segments;

        internal CharacterFootGroundSurfaceDiagnostics(
            CharacterFootGroundSurfacePage page)
        {
            State = page.State;
            Origin = page.Origin;
            Forward = page.Forward;
            Up = page.Up;
            Length = page.Length;
            WorldRevision = page.WorldRevision;
            m_Segments = page.Count == 0
                ? Array.Empty<CharacterFootGroundSurfaceSegment>()
                : new CharacterFootGroundSurfaceSegment[page.Count];
            for (int i = 0; i < m_Segments.Length; i++)
                m_Segments[i] = page.SegmentAt(i);
        }

        public CharacterFootGroundSurfaceState State { get; }
        public Vector3 Origin { get; }
        public Vector3 Forward { get; }
        public Vector3 Up { get; }
        public float Length { get; }
        public ulong WorldRevision { get; }
        public int Count => m_Segments?.Length ?? 0;

        public CharacterFootGroundSurfaceSegment SegmentAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Segments[index];
        }
    }

    internal sealed class CharacterFootGroundSurfaceProjector
    {
        readonly Vector3[] m_First = new Vector3[8];
        readonly Vector3[] m_Second = new Vector3[8];

        internal bool TryAppend(
            int surfaceIdentity,
            int faceIdentity,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            bool limitVerticalAdmission,
            in CharacterFootGroundPathQueryRequest query,
            CharacterFootGroundSurfacePage output)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            float length = normal.magnitude;
            if (!float.IsFinite(length))
                return false;
            if (length <= 0.00000001f ||
                Vector3.Dot(normal / length, output.Up) <= 0.000001f)
                return true;

            m_First[0] = Project(a, output);
            m_First[1] = Project(b, output);
            m_First[2] = Project(c, output);
            Vector3[] input = m_First;
            Vector3[] target = m_Second;
            int count = 3;
            for (int plane = 0; plane < 4; plane++)
            {
                int axis = plane < 2 ? 0 : 2;
                float boundary = plane == 0 ? 0f :
                    plane == 1 ? output.Length :
                    plane == 2 ? -query.Radius : query.Radius;
                bool minimum = plane == 0 || plane == 2;
                if (!TryClip(input, count, target, axis, boundary, minimum, out count))
                    return false;
                if (count < 2)
                    return true;
                Vector3[] swap = input;
                input = target;
                target = swap;
            }

            float lowest = float.PositiveInfinity;
            float highest = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                float relativeHeight = input[i].y -
                    output.AxisRise * (input[i].x / output.Length);
                lowest = Mathf.Min(lowest, relativeHeight);
                highest = Mathf.Max(highest, relativeHeight);
            }
            if (limitVerticalAdmission && (lowest > query.Radius ||
                highest < -query.MaximumDistance - query.Radius))
                return true;

            for (int i = 0; i < count; i++)
            {
                Vector3 first = input[i];
                Vector3 second = input[(i + 1) % count];
                if (!output.TryAdd(
                        surfaceIdentity, faceIdentity,
                        new Vector2(first.x, first.y),
                        new Vector2(second.x, second.y)))
                    return false;
            }
            return true;
        }

        static Vector3 Project(
            Vector3 point,
            CharacterFootGroundSurfacePage frame)
        {
            Vector3 relative = point - frame.Origin;
            return new Vector3(
                Vector3.Dot(relative, frame.Forward),
                Vector3.Dot(relative, frame.Up),
                Vector3.Dot(relative, frame.Right));
        }

        static bool TryClip(
            Vector3[] input,
            int count,
            Vector3[] output,
            int axis,
            float boundary,
            bool minimum,
            out int written)
        {
            written = 0;
            Vector3 previous = input[count - 1];
            float previousDistance = Distance(previous, axis, boundary, minimum);
            bool previousInside = previousDistance >= 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 current = input[i];
                float currentDistance = Distance(current, axis, boundary, minimum);
                bool currentInside = currentDistance >= 0f;
                if (currentInside != previousInside)
                {
                    if (written >= output.Length)
                        return false;
                    float progress = previousDistance /
                        (previousDistance - currentDistance);
                    Vector3 intersection = Vector3.LerpUnclamped(previous, current, progress);
                    if (axis == 0) intersection.x = boundary;
                    else intersection.z = boundary;
                    output[written++] = intersection;
                }
                if (currentInside)
                {
                    if (written >= output.Length)
                        return false;
                    output[written++] = current;
                }
                previous = current;
                previousDistance = currentDistance;
                previousInside = currentInside;
            }
            return true;
        }

        static float Distance(Vector3 point, int axis, float boundary, bool minimum)
        {
            float value = axis == 0 ? point.x : point.z;
            return minimum ? value - boundary : boundary - value;
        }
    }
}
