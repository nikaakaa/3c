using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootGroundSurfaceVisibility
    {
        const float GeometryEpsilon = 0.0001f;
        readonly float[] m_Events;
        int m_EventCount;

        internal CharacterFootGroundSurfaceVisibility(int contactCapacity)
        {
            m_Events = new float[
                checked(contactCapacity * CharacterFootGroundSurfacePage.SegmentsPerContact * 2 + 2)];
        }

        internal bool TryBuildEdges(
            in CharacterFootGroundPathInput input,
            CharacterFootGroundSurfacePage surfaces,
            CharacterFootGroundEdgeSummary edges,
            out CharacterFootGroundPathRejectReason reason,
            out CharacterFootGroundInvalidSegment invalid)
        {
            reason = CharacterFootGroundPathRejectReason.None;
            invalid = default;
            m_EventCount = 0;
            edges.Clear();
            if (!surfaces.IsReady || !surfaces.Matches(input.Query))
            {
                reason = CharacterFootGroundPathRejectReason.SurfaceGeometryUnavailable;
                return false;
            }
            if (surfaces.Count == 0)
            {
                reason = CharacterFootGroundPathRejectReason.SurfaceCoverageUnavailable;
                return false;
            }
            AddEvent(0f);
            AddEvent(surfaces.Length);
            for (int i = 0; i < surfaces.Count; i++)
            {
                CharacterFootGroundSurfaceSegment segment = surfaces.SegmentAt(i);
                AddEvent(segment.Start.x);
                AddEvent(segment.End.x);
            }
            SortEvents();

            float previousEvent = float.NegativeInfinity;
            float previousHeight = Vector3.Dot(input.LastLanding - surfaces.Origin, surfaces.Up);
            int previousSurface = input.LastLandingSurfaceIdentity;
            int previousFace = -1;
            for (int i = 0; i < m_EventCount; i++)
            {
                float distance = m_Events[i];
                if (distance - previousEvent <= GeometryEpsilon &&
                    distance != surfaces.Length)
                    continue;
                previousEvent = distance;
                bool atStart = distance <= GeometryEpsilon;
                bool atEnd = surfaces.Length - distance <= GeometryEpsilon;
                bool hasBefore = TryHeight(
                    surfaces, distance, -1,
                    out float beforeHeight, out int beforeSurface, out int beforeFace);
                bool hasAfter = TryHeight(
                    surfaces, distance, 1,
                    out float afterHeight, out int afterSurface, out int afterFace);
                bool hasPeak = TryHeight(
                    surfaces, distance, 0,
                    out float peakHeight, out int peakSurface, out int peakFace);
                if (atStart)
                {
                    hasBefore = true;
                    beforeHeight = Vector3.Dot(input.LastLanding - surfaces.Origin, surfaces.Up);
                    beforeSurface = input.LastLandingSurfaceIdentity;
                    beforeFace = -1;
                }
                if (atEnd)
                {
                    hasAfter = true;
                    afterHeight = Vector3.Dot(input.NextSwingLanding - surfaces.Origin, surfaces.Up);
                    afterSurface = input.NextSwingLandingSurfaceIdentity;
                    afterFace = -1;
                }
                if (!hasBefore)
                {
                    beforeHeight = previousHeight;
                    beforeSurface = previousSurface;
                    beforeFace = previousFace;
                }
                if (!hasAfter)
                {
                    afterHeight = hasPeak ? peakHeight : beforeHeight;
                    afterSurface = hasPeak ? peakSurface : beforeSurface;
                    afterFace = hasPeak ? peakFace : beforeFace;
                }
                previousHeight = afterHeight;
                previousSurface = afterSurface;
                previousFace = afterFace;
                if (hasPeak && peakHeight > Mathf.Max(beforeHeight, afterHeight) + GeometryEpsilon)
                {
                    if (!TryAddEdge(
                            input.MaximumReachableVerticalEdge, surfaces, edges,
                            distance, beforeHeight, peakHeight,
                            beforeSurface, beforeFace, peakSurface, peakFace,
                            out reason, out invalid) ||
                        !TryAddEdge(
                            input.MaximumReachableVerticalEdge, surfaces, edges,
                            distance, peakHeight, afterHeight,
                            peakSurface, peakFace, afterSurface, afterFace,
                            out reason, out invalid))
                        return false;
                }
                else if (!TryAddEdge(
                             input.MaximumReachableVerticalEdge, surfaces, edges,
                             distance, beforeHeight, afterHeight,
                             beforeSurface, beforeFace, afterSurface, afterFace,
                             out reason, out invalid))
                {
                    return false;
                }
            }
            return true;
        }

        void AddEvent(float distance)
        {
            if (m_EventCount >= m_Events.Length)
                throw new InvalidOperationException("Ground surface event capacity is inconsistent.");
            m_Events[m_EventCount++] = distance;
        }

        void SortEvents()
        {
            for (int i = 1; i < m_EventCount; i++)
            {
                float value = m_Events[i];
                int index = i;
                while (index > 0 && m_Events[index - 1] > value)
                {
                    m_Events[index] = m_Events[index - 1];
                    index--;
                }
                m_Events[index] = value;
            }
        }

        static bool TryHeight(
            CharacterFootGroundSurfacePage surfaces,
            float distance,
            int side,
            out float height,
            out int surfaceIdentity,
            out int faceIdentity)
        {
            height = float.NegativeInfinity;
            surfaceIdentity = 0;
            faceIdentity = -1;
            float sample = distance + side * GeometryEpsilon;
            for (int i = 0; i < surfaces.Count; i++)
            {
                CharacterFootGroundSurfaceSegment segment = surfaces.SegmentAt(i);
                bool contains = side < 0
                    ? segment.Start.x < sample && segment.End.x >= sample
                    : side > 0
                        ? segment.Start.x <= sample && segment.End.x > sample
                        : segment.Start.x <= distance + GeometryEpsilon &&
                          segment.End.x >= distance - GeometryEpsilon;
                if (!contains)
                    continue;
                float length = segment.End.x - segment.Start.x;
                float value = length > 0f
                    ? Mathf.Lerp(
                        segment.Start.y, segment.End.y,
                        Mathf.Clamp01((distance - segment.Start.x) / length))
                    : Mathf.Max(segment.Start.y, segment.End.y);
                if (side == 0)
                {
                    if (Mathf.Abs(segment.Start.x - distance) <= GeometryEpsilon)
                        value = Mathf.Max(value, segment.Start.y);
                    if (Mathf.Abs(segment.End.x - distance) <= GeometryEpsilon)
                        value = Mathf.Max(value, segment.End.y);
                }
                if (value > height)
                {
                    height = value;
                    surfaceIdentity = segment.SurfaceIdentity;
                    faceIdentity = segment.FaceIdentity;
                }
            }
            return surfaceIdentity != 0 && float.IsFinite(height);
        }

        static bool TryAddEdge(
            float maximumHeight,
            CharacterFootGroundSurfacePage surfaces,
            CharacterFootGroundEdgeSummary edges,
            float distance,
            float beforeHeight,
            float afterHeight,
            int beforeSurface,
            int beforeFace,
            int afterSurface,
            int afterFace,
            out CharacterFootGroundPathRejectReason reason,
            out CharacterFootGroundInvalidSegment invalid)
        {
            reason = CharacterFootGroundPathRejectReason.None;
            invalid = default;
            float height = Mathf.Abs(afterHeight - beforeHeight);
            if (height <= GeometryEpsilon)
                return true;
            Vector3 before = surfaces.ResolveWorldPoint(distance, beforeHeight);
            Vector3 after = surfaces.ResolveWorldPoint(distance, afterHeight);
            var edge = new CharacterFootGroundEdge(
                edges.Count,
                Identity(edges.Count, beforeSurface, beforeFace, afterSurface, afterFace),
                beforeHeight <= afterHeight ? before : after,
                beforeHeight <= afterHeight ? after : before,
                height);
            if (!edges.TryAdd(in edge))
            {
                reason = CharacterFootGroundPathRejectReason.EdgeCapacityExceeded;
                return false;
            }
            if (height > maximumHeight)
            {
                invalid = new CharacterFootGroundInvalidSegment(in edge);
                reason = CharacterFootGroundPathRejectReason.UnreachableEdge;
                return false;
            }
            return true;
        }

        static ulong Identity(int index, int firstSurface, int firstFace, int secondSurface, int secondFace)
        {
            ulong hash = 14695981039346656037UL;
            Add(ref hash, index);
            Add(ref hash, firstSurface);
            Add(ref hash, firstFace);
            Add(ref hash, secondSurface);
            Add(ref hash, secondFace);
            return hash != 0 ? hash : 1UL;
        }

        static void Add(ref ulong hash, int value)
        {
            uint bits = unchecked((uint)value);
            for (int i = 0; i < 4; i++)
            {
                hash ^= (byte)bits;
                hash *= 1099511628211UL;
                bits >>= 8;
            }
        }
    }
}
