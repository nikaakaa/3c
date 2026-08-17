using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFootGroundEnvelopeVertex
    {
        internal CharacterFootGroundEnvelopeVertex(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    readonly struct CharacterFootGroundEnvelopeCandidate
    {
        internal CharacterFootGroundEnvelopeCandidate(
            float distance,
            float height,
            float normalDistance,
            float normalHeight,
            ulong identity,
            byte endpoint)
        {
            Distance = distance;
            Height = height;
            NormalDistance = normalDistance;
            NormalHeight = normalHeight;
            Identity = identity;
            Endpoint = endpoint;
        }

        internal float Distance { get; }
        internal float Height { get; }
        internal float NormalDistance { get; }
        internal float NormalHeight { get; }
        internal ulong Identity { get; }
        internal byte Endpoint { get; }
    }

    internal sealed class CharacterFootGroundEnvelopeWorkspace
    {
        readonly CharacterFootGroundEnvelopeCandidate[] m_Contacts;
        readonly CharacterFootGroundEnvelopeCandidate[] m_Profile;

        internal CharacterFootGroundEnvelopeWorkspace(int contactCapacity)
        {
            if (contactCapacity < 4 || contactCapacity > 64)
                throw new ArgumentOutOfRangeException(nameof(contactCapacity));
            m_Contacts = new CharacterFootGroundEnvelopeCandidate[contactCapacity];
            m_Profile = new CharacterFootGroundEnvelopeCandidate[contactCapacity + 4];
        }

        internal int ContactCount { get; private set; }
        internal int ProfileCount { get; private set; }

        internal CharacterFootGroundEnvelopeCandidate ContactAt(int index) =>
            m_Contacts[index];

        internal CharacterFootGroundEnvelopeCandidate ProfileAt(int index) =>
            m_Profile[index];

        internal void SetProfile(int index, in CharacterFootGroundEnvelopeCandidate value) =>
            m_Profile[index] = value;

        internal void Clear()
        {
            Array.Clear(m_Contacts, 0, ContactCount);
            Array.Clear(m_Profile, 0, ProfileCount);
            ContactCount = 0;
            ProfileCount = 0;
        }

        internal bool TryAddContact(in CharacterFootGroundEnvelopeCandidate candidate)
        {
            if (ContactCount >= m_Contacts.Length)
                return false;
            m_Contacts[ContactCount++] = candidate;
            return true;
        }

        internal bool TryAddProfile(in CharacterFootGroundEnvelopeCandidate candidate)
        {
            if (ProfileCount >= m_Profile.Length)
                return false;
            m_Profile[ProfileCount++] = candidate;
            return true;
        }

        internal void SortContacts() => Sort(m_Contacts, ContactCount);

        internal void SortProfile() => Sort(m_Profile, ProfileCount);

        internal void ReplaceProfileCount(int count)
        {
            if (count < 0 || count > ProfileCount)
                throw new ArgumentOutOfRangeException(nameof(count));
            Array.Clear(m_Profile, count, ProfileCount - count);
            ProfileCount = count;
        }

        static void Sort(CharacterFootGroundEnvelopeCandidate[] values, int count)
        {
            for (int i = 1; i < count; i++)
            {
                CharacterFootGroundEnvelopeCandidate value = values[i];
                int insertion = i;
                while (insertion > 0 && Compare(value, values[insertion - 1]) < 0)
                {
                    values[insertion] = values[insertion - 1];
                    insertion--;
                }
                values[insertion] = value;
            }
        }

        static int Compare(
            CharacterFootGroundEnvelopeCandidate left,
            CharacterFootGroundEnvelopeCandidate right)
        {
            int distance = left.Distance.CompareTo(right.Distance);
            if (distance != 0)
                return distance;
            int height = left.Height.CompareTo(right.Height);
            if (height != 0)
                return height;
            int endpoint = left.Endpoint.CompareTo(right.Endpoint);
            return endpoint != 0 ? endpoint : left.Identity.CompareTo(right.Identity);
        }
    }

    internal sealed class CharacterFootGroundEnvelopePage
    {
        readonly CharacterFootGroundEnvelopeVertex[] m_Vertices;

        internal CharacterFootGroundEnvelopePage(int contactCapacity)
        {
            if (contactCapacity < 4 || contactCapacity > 64)
                throw new ArgumentOutOfRangeException(nameof(contactCapacity));
            m_Vertices = new CharacterFootGroundEnvelopeVertex[contactCapacity + 4];
        }

        internal int Count { get; private set; }

        internal CharacterFootGroundEnvelopeVertex VertexAt(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Vertices[index];
        }

        internal void Clear()
        {
            Array.Clear(m_Vertices, 0, Count);
            Count = 0;
        }

        internal bool TryPush(Vector3 position)
        {
            if (Count >= m_Vertices.Length)
                return false;
            m_Vertices[Count++] = new CharacterFootGroundEnvelopeVertex(position);
            return true;
        }

        internal void Pop()
        {
            if (Count <= 0)
                throw new InvalidOperationException("Ground Envelope has no vertex to remove.");
            m_Vertices[--Count] = default;
        }
    }

    internal static class CharacterFootGroundEnvelopeBuilder
    {
        const float GeometryEpsilon = 0.0001f;

        internal static bool TryBuild(
            in CharacterFootGroundPathInput input,
            CharacterFootGroundContactPage contacts,
            CharacterFootGroundEnvelopeWorkspace workspace,
            CharacterFootGroundEnvelopePage output,
            out CharacterFootGroundPathRejectReason rejectReason)
        {
            if (!input.IsValid || contacts == null || workspace == null || output == null)
                throw new ArgumentException("Ground Envelope input is invalid.");

            workspace.Clear();
            output.Clear();
            Vector3 up = input.ComponentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                input.NextLanding - input.CurrentLanding,
                up);
            float pathLength = horizontal.magnitude;
            float endHeight = Vector3.Dot(
                input.NextLanding - input.CurrentLanding,
                up);
            if (!float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
            {
                rejectReason = CharacterFootGroundPathRejectReason.DegenerateEnvelope;
                return false;
            }

            Vector3 forward = horizontal / pathLength;
            for (int i = 0; i < contacts.Count; i++)
            {
                CharacterFootGroundContact contact = contacts.ContactAt(i);
                if (!TryProjectContact(
                        in contact,
                        input.CurrentLanding,
                        forward,
                        up,
                        pathLength,
                        out CharacterFootGroundEnvelopeCandidate candidate))
                {
                    continue;
                }
                if (!workspace.TryAddContact(in candidate))
                {
                    rejectReason = CharacterFootGroundPathRejectReason.EnvelopeCapacityExceeded;
                    return false;
                }
            }

            if (workspace.ContactCount <= 0)
            {
                rejectReason = CharacterFootGroundPathRejectReason.NoEnvelopeContact;
                return false;
            }

            workspace.SortContacts();
            if (!TryBuildSurfaceProfile(workspace, pathLength, endHeight))
            {
                rejectReason = CharacterFootGroundPathRejectReason.EnvelopeCapacityExceeded;
                return false;
            }
            if (!TryCollapseDistances(workspace, pathLength))
            {
                rejectReason = CharacterFootGroundPathRejectReason.EnvelopeCapacityExceeded;
                return false;
            }
            if (!TryBuildUpperHull(
                    workspace,
                    input.CurrentLanding,
                    input.NextLanding,
                    forward,
                    up,
                    output))
            {
                rejectReason = CharacterFootGroundPathRejectReason.EnvelopeCapacityExceeded;
                return false;
            }
            if (output.Count < 2)
            {
                output.Clear();
                rejectReason = CharacterFootGroundPathRejectReason.DegenerateEnvelope;
                return false;
            }
            if (Vector3.Distance(output.VertexAt(0).Position, input.CurrentLanding) > GeometryEpsilon ||
                Vector3.Distance(
                    output.VertexAt(output.Count - 1).Position,
                    input.NextLanding) > GeometryEpsilon)
            {
                output.Clear();
                rejectReason = CharacterFootGroundPathRejectReason.DegenerateEnvelope;
                return false;
            }

            rejectReason = CharacterFootGroundPathRejectReason.None;
            return true;
        }

        static bool TryProjectContact(
            in CharacterFootGroundContact contact,
            Vector3 origin,
            Vector3 forward,
            Vector3 up,
            float pathLength,
            out CharacterFootGroundEnvelopeCandidate candidate)
        {
            Vector3 relative = contact.Position - origin;
            float distance = Mathf.Clamp(Vector3.Dot(relative, forward), 0f, pathLength);
            float height = Vector3.Dot(relative, up);
            float normalDistance = Vector3.Dot(contact.Normal, forward);
            float normalHeight = Vector3.Dot(contact.Normal, up);
            float normalLength = Mathf.Sqrt(
                normalDistance * normalDistance + normalHeight * normalHeight);
            if (!float.IsFinite(distance) || !float.IsFinite(height))
            {
                candidate = default;
                return false;
            }
            if (!float.IsFinite(normalLength) || normalLength <= GeometryEpsilon)
            {
                normalDistance = 0f;
                normalHeight = 0f;
            }
            else
            {
                normalDistance /= normalLength;
                normalHeight /= normalLength;
            }
            candidate = new CharacterFootGroundEnvelopeCandidate(
                distance,
                height,
                normalDistance,
                normalHeight,
                contact.CandidateIdentity,
                0);
            return true;
        }

        static bool TryBuildSurfaceProfile(
            CharacterFootGroundEnvelopeWorkspace workspace,
            float pathLength,
            float endHeight)
        {
            var start = new CharacterFootGroundEnvelopeCandidate(
                0f,
                0f,
                0f,
                1f,
                0,
                1);
            if (!workspace.TryAddProfile(in start))
                return false;

            for (int i = 0; i + 1 < workspace.ContactCount; i++)
            {
                CharacterFootGroundEnvelopeCandidate current = workspace.ContactAt(i);
                CharacterFootGroundEnvelopeCandidate next = workspace.ContactAt(i + 1);
                CharacterFootGroundEnvelopeCandidate value = TryIntersect(
                    in current,
                    in next,
                    pathLength,
                    out CharacterFootGroundEnvelopeCandidate intersection)
                    ? intersection
                    : current;
                if (!workspace.TryAddProfile(in value))
                    return false;
            }

            CharacterFootGroundEnvelopeCandidate lastContact =
                workspace.ContactAt(workspace.ContactCount - 1);
            if (!workspace.TryAddProfile(in lastContact))
                return false;

            var end = new CharacterFootGroundEnvelopeCandidate(
                pathLength,
                endHeight,
                0f,
                1f,
                ulong.MaxValue,
                2);
            if (!workspace.TryAddProfile(in end))
                return false;
            workspace.SortProfile();
            return true;
        }

        static bool TryIntersect(
            in CharacterFootGroundEnvelopeCandidate first,
            in CharacterFootGroundEnvelopeCandidate second,
            float pathLength,
            out CharacterFootGroundEnvelopeCandidate intersection)
        {
            float determinant =
                first.NormalDistance * second.NormalHeight -
                second.NormalDistance * first.NormalHeight;
            if (Mathf.Abs(determinant) <= GeometryEpsilon)
            {
                intersection = default;
                return false;
            }

            float firstPlane =
                first.NormalDistance * first.Distance +
                first.NormalHeight * first.Height;
            float secondPlane =
                second.NormalDistance * second.Distance +
                second.NormalHeight * second.Height;
            float distance =
                (firstPlane * second.NormalHeight -
                 secondPlane * first.NormalHeight) / determinant;
            float height =
                (first.NormalDistance * secondPlane -
                 second.NormalDistance * firstPlane) / determinant;
            float minimumDistance = Mathf.Min(first.Distance, second.Distance) - GeometryEpsilon;
            float maximumDistance = Mathf.Max(first.Distance, second.Distance) + GeometryEpsilon;
            float minimumHeight = Mathf.Min(first.Height, second.Height) - GeometryEpsilon;
            float maximumHeight = Mathf.Max(first.Height, second.Height) + GeometryEpsilon;
            if (!float.IsFinite(distance) || !float.IsFinite(height) ||
                distance < minimumDistance || distance > maximumDistance ||
                distance < -GeometryEpsilon || distance > pathLength + GeometryEpsilon ||
                height < minimumHeight || height > maximumHeight)
            {
                intersection = default;
                return false;
            }

            intersection = new CharacterFootGroundEnvelopeCandidate(
                Mathf.Clamp(distance, 0f, pathLength),
                height,
                first.NormalDistance,
                first.NormalHeight,
                first.Identity ^ RotateLeft(second.Identity, 29),
                0);
            return true;
        }

        static bool TryCollapseDistances(
            CharacterFootGroundEnvelopeWorkspace workspace,
            float pathLength)
        {
            int sourceCount = workspace.ProfileCount;
            int write = 0;
            int read = 0;
            while (read < sourceCount)
            {
                int groupEnd = read + 1;
                while (groupEnd < sourceCount &&
                       Mathf.Abs(
                           workspace.ProfileAt(groupEnd).Distance -
                           workspace.ProfileAt(read).Distance) <= GeometryEpsilon)
                {
                    groupEnd++;
                }

                CharacterFootGroundEnvelopeCandidate highest = workspace.ProfileAt(read);
                CharacterFootGroundEnvelopeCandidate start = default;
                CharacterFootGroundEnvelopeCandidate end = default;
                bool hasStart = false;
                bool hasEnd = false;
                for (int i = read; i < groupEnd; i++)
                {
                    CharacterFootGroundEnvelopeCandidate value = workspace.ProfileAt(i);
                    if (value.Height > highest.Height)
                        highest = value;
                    if (value.Endpoint == 1)
                    {
                        start = value;
                        hasStart = true;
                    }
                    else if (value.Endpoint == 2)
                    {
                        end = value;
                        hasEnd = true;
                    }
                }

                if (hasStart)
                {
                    workspace.SetProfile(write++, in start);
                    if (!SamePosition(start, highest))
                        workspace.SetProfile(write++, in highest);
                }
                else if (hasEnd)
                {
                    if (!SamePosition(highest, end))
                        workspace.SetProfile(write++, in highest);
                    workspace.SetProfile(write++, in end);
                }
                else
                {
                    workspace.SetProfile(write++, in highest);
                }
                read = groupEnd;
            }

            workspace.ReplaceProfileCount(write);
            if (write < 2)
                return false;
            CharacterFootGroundEnvelopeCandidate first = workspace.ProfileAt(0);
            CharacterFootGroundEnvelopeCandidate last = workspace.ProfileAt(write - 1);
            return first.Endpoint == 1 && last.Endpoint == 2 &&
                   Mathf.Abs(first.Distance) <= GeometryEpsilon &&
                   Mathf.Abs(last.Distance - pathLength) <= GeometryEpsilon;
        }

        static bool TryBuildUpperHull(
            CharacterFootGroundEnvelopeWorkspace workspace,
            Vector3 origin,
            Vector3 endPoint,
            Vector3 forward,
            Vector3 up,
            CharacterFootGroundEnvelopePage output)
        {
            for (int i = 0; i < workspace.ProfileCount; i++)
            {
                CharacterFootGroundEnvelopeCandidate value = workspace.ProfileAt(i);
                Vector3 position = value.Endpoint == 1
                    ? origin
                    : value.Endpoint == 2
                        ? endPoint
                        : origin + forward * value.Distance + up * value.Height;
                while (output.Count >= 2)
                {
                    Vector3 first = output.VertexAt(output.Count - 2).Position - origin;
                    Vector3 second = output.VertexAt(output.Count - 1).Position - origin;
                    float firstDistance = Vector3.Dot(first, forward);
                    float firstHeight = Vector3.Dot(first, up);
                    float secondDistance = Vector3.Dot(second, forward);
                    float secondHeight = Vector3.Dot(second, up);
                    float cross =
                        (secondDistance - firstDistance) * (value.Height - secondHeight) -
                        (secondHeight - firstHeight) * (value.Distance - secondDistance);
                    if (cross < -GeometryEpsilon)
                        break;
                    output.Pop();
                }
                if (!output.TryPush(position))
                    return false;
            }
            return true;
        }

        static bool SamePosition(
            CharacterFootGroundEnvelopeCandidate first,
            CharacterFootGroundEnvelopeCandidate second) =>
            Mathf.Abs(first.Distance - second.Distance) <= GeometryEpsilon &&
            Mathf.Abs(first.Height - second.Height) <= GeometryEpsilon;

        static ulong RotateLeft(ulong value, int count) =>
            value << count | value >> (64 - count);
    }
}
