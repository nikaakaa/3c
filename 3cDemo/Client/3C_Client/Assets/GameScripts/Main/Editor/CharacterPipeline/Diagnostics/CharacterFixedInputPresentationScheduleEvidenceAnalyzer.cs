using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFixedInputPresentationScheduleFootCoverage
    {
        internal CharacterFixedInputPresentationScheduleFootCoverage(
            int rowCount,
            int distinctFrameCount,
            int firstScheduleFrameIndex,
            int lastScheduleFrameIndex)
        {
            if (rowCount <= 0 || distinctFrameCount <= 0 ||
                firstScheduleFrameIndex < 0 ||
                lastScheduleFrameIndex < firstScheduleFrameIndex)
            {
                throw new ArgumentException(
                    "Presentation Schedule Foot coverage is invalid.");
            }
            RowCount = rowCount;
            DistinctFrameCount = distinctFrameCount;
            FirstScheduleFrameIndex = firstScheduleFrameIndex;
            LastScheduleFrameIndex = lastScheduleFrameIndex;
        }

        internal int RowCount { get; }
        internal int DistinctFrameCount { get; }
        internal int FirstScheduleFrameIndex { get; }
        internal int LastScheduleFrameIndex { get; }
    }

    internal static class CharacterFixedInputPresentationScheduleEvidenceAnalyzer
    {
        const float CorridorEpsilon = 0.0001f;
        const float EndpointDistance = 0.03f;
        const float UpperEdgeHorizontalDistance = 0.25f;
        const float MinimumVerticalSeparation = 0.1f;

        internal static CharacterFixedInputPresentationScheduleRepresentativeEvidence Analyze(
            string samplesPath,
            string geometryPath)
        {
            var geometry = ReadGeometry(geometryPath);
            using var reader = new CsvReader(samplesPath);
            int accepted = 0;
            int outside = 0;
            int largeOutsideClamp = 0;
            float maximumOutsideClamp = 0f;
            int verticalEndpointCount = 0;
            VerticalEndpointEvidence representative = default;
            while (reader.Read())
            {
                if (!reader.Boolean("FootMotionSafetyFloorAvailable"))
                    continue;
                accepted++;
                Vector3 up = reader.Vector("GroundPathComponentUp");
                Vector3 last = reader.Vector("GroundPathLastLanding");
                Vector3 next = reader.Vector("GroundPathNextSwingLanding");
                Vector3 sole = reader.Vector("FootMotionOriginalSole");
                float radius = reader.Single("GroundPathRadius");
                float corridorDistance = DistanceToHorizontalAxis(
                    sole,
                    last,
                    next,
                    up);
                bool corridorOutside =
                    corridorDistance > radius + CorridorEpsilon;
                float clamp = reader.Single(
                    "FootMotionSafetyFloorClampMeters");
                if (corridorOutside)
                {
                    outside++;
                    maximumOutsideClamp = Mathf.Max(
                        maximumOutsideClamp,
                        clamp);
                    if (clamp > 0.1f)
                        largeOutsideClamp++;
                }
                var key = new GeometryKey(
                    reader.Integer("FrameSequence"),
                    reader.String("Side"),
                    reader.UInt64("GroundPathInputIdentity"));
                if (!geometry.TryGetValue(key, out GeometryFrame frame) ||
                    !TryFindVerticalEndpoint(
                        frame,
                        next,
                        reader.Integer(
                            "GroundPathNextSwingLandingSurfaceIdentity"),
                        up,
                        out VerticalEndpointEvidence evidence))
                {
                    continue;
                }
                verticalEndpointCount++;
                if (!representative.IsValid ||
                    evidence.VerticalSeparationMeters >
                    representative.VerticalSeparationMeters)
                {
                    representative = evidence.WithFrame(
                        key.FrameSequence,
                        key.Side);
                }
            }
            if (!representative.IsValid)
                throw new InvalidDataException(
                    "Live Presentation Schedule Foot sample contains no typed vertical endpoint Surface evidence.");
            return new CharacterFixedInputPresentationScheduleRepresentativeEvidence(
                accepted,
                outside,
                largeOutsideClamp,
                maximumOutsideClamp,
                verticalEndpointCount,
                representative.FrameSequence,
                representative.Side,
                representative.LandingSurfaceIdentity,
                representative.UpperEdgeSurfaceIdentity,
                representative.LandingHeight,
                representative.UpperEdgeHeight,
                representative.VerticalSeparationMeters);
        }

        internal static CharacterFixedInputPresentationScheduleFootCoverage
            AnalyzeCoverage(
                string samplesPath,
                IReadOnlyList<GameplayPresentationScheduleFrame> scheduleFrames)
        {
            if (scheduleFrames == null || scheduleFrames.Count == 0)
                throw new ArgumentException(
                    "Presentation Schedule coverage requires frames.",
                    nameof(scheduleFrames));
            var indices = new Dictionary<ulong, int>(scheduleFrames.Count);
            for (int i = 0; i < scheduleFrames.Count; i++)
            {
                if (!indices.TryAdd(scheduleFrames[i].RenderFrame, i))
                    throw new InvalidDataException(
                        "Presentation Schedule duplicates a Render Frame.");
            }
            var covered = new HashSet<int>();
            int rowCount = 0;
            int first = int.MaxValue;
            int last = -1;
            using var reader = new CsvReader(samplesPath);
            while (reader.Read())
            {
                ulong renderFrame = reader.UInt64("FrameSequence");
                if (!indices.TryGetValue(renderFrame, out int index))
                {
                    throw new InvalidDataException(
                        $"Foot sample Render Frame {renderFrame} is outside the formal Presentation Schedule window.");
                }
                rowCount++;
                covered.Add(index);
                first = Mathf.Min(first, index);
                last = Mathf.Max(last, index);
            }
            return new CharacterFixedInputPresentationScheduleFootCoverage(
                rowCount,
                covered.Count,
                first,
                last);
        }

        static Dictionary<GeometryKey, GeometryFrame> ReadGeometry(
            string path)
        {
            var result = new Dictionary<GeometryKey, GeometryFrame>();
            using var reader = new CsvReader(path);
            while (reader.Read())
            {
                var key = new GeometryKey(
                    reader.Integer("FrameSequence"),
                    reader.String("Side"),
                    reader.UInt64("GroundPathInputIdentity"));
                if (!result.TryGetValue(key, out GeometryFrame frame))
                {
                    frame = new GeometryFrame();
                    result.Add(key, frame);
                }
                int contactIndex = reader.Integer("GroundContactIndex");
                if (contactIndex >= 0 &&
                    frame.ContactIndices.Add(contactIndex))
                {
                    frame.Contacts.Add(new GeometryContact(
                        reader.Integer("GroundContactSurfaceIdentity"),
                        reader.UInt64("GroundContactCandidateIdentity"),
                        reader.Vector("GroundContactPosition"),
                        reader.Vector("GroundContactNormal")));
                }
                int vertexIndex = reader.Integer("GroundEnvelopeVertexIndex");
                if (vertexIndex >= 0 &&
                    frame.VertexIndices.Add(vertexIndex))
                {
                    frame.Vertices.Add(new GeometryVertex(
                        vertexIndex,
                        reader.Vector("GroundEnvelopeVertex")));
                }
            }
            foreach (GeometryFrame frame in result.Values)
                frame.Vertices.Sort((left, right) =>
                    left.Index.CompareTo(right.Index));
            return result;
        }

        static bool TryFindVerticalEndpoint(
            GeometryFrame frame,
            Vector3 landing,
            int landingSurfaceIdentity,
            Vector3 componentUp,
            out VerticalEndpointEvidence evidence)
        {
            evidence = default;
            if (frame.Vertices.Count < 2 || componentUp.sqrMagnitude <= 0f)
                return false;
            Vector3 up = componentUp.normalized;
            GeometryVertex endpoint = frame.Vertices[frame.Vertices.Count - 1];
            GeometryVertex upper = frame.Vertices[frame.Vertices.Count - 2];
            if (Vector3.Distance(endpoint.Position, landing) > EndpointDistance)
                return false;
            float landingHeight = Vector3.Dot(endpoint.Position, up);
            float upperHeight = Vector3.Dot(upper.Position, up);
            float separation = upperHeight - landingHeight;
            float horizontal = Vector3.ProjectOnPlane(
                upper.Position - endpoint.Position,
                up).magnitude;
            if (separation <= MinimumVerticalSeparation ||
                horizontal > UpperEdgeHorizontalDistance)
            {
                return false;
            }
            GeometryContact landingContact = FindContact(
                frame,
                endpoint.Position,
                landingSurfaceIdentity);
            GeometryContact upperContact = FindContact(
                frame,
                upper.Position,
                0);
            if (!landingContact.IsValid || !upperContact.IsValid ||
                landingContact.SurfaceIdentity ==
                upperContact.SurfaceIdentity)
            {
                return false;
            }
            evidence = new VerticalEndpointEvidence(
                0,
                string.Empty,
                landingContact.SurfaceIdentity,
                upperContact.SurfaceIdentity,
                landingHeight,
                upperHeight,
                separation);
            return true;
        }

        static GeometryContact FindContact(
            GeometryFrame frame,
            Vector3 position,
            int requiredSurfaceIdentity)
        {
            GeometryContact result = default;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < frame.Contacts.Count; i++)
            {
                GeometryContact contact = frame.Contacts[i];
                if (requiredSurfaceIdentity != 0 &&
                    contact.SurfaceIdentity != requiredSurfaceIdentity)
                {
                    continue;
                }
                float candidate = Vector3.Distance(
                    contact.Position,
                    position);
                if (candidate >= distance)
                    continue;
                result = contact;
                distance = candidate;
            }
            return distance <= UpperEdgeHorizontalDistance
                ? result
                : default;
        }

        static float DistanceToHorizontalAxis(
            Vector3 point,
            Vector3 start,
            Vector3 end,
            Vector3 componentUp)
        {
            if (componentUp.sqrMagnitude <= 0f)
                return float.PositiveInfinity;
            Vector3 up = componentUp.normalized;
            Vector3 axis = Vector3.ProjectOnPlane(end - start, up);
            float lengthSquared = axis.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
                return float.PositiveInfinity;
            Vector3 relative = Vector3.ProjectOnPlane(point - start, up);
            float distance = Vector3.Dot(relative, axis) / lengthSquared;
            return (relative - axis * distance).magnitude;
        }

        readonly struct GeometryKey : IEquatable<GeometryKey>
        {
            internal GeometryKey(
                int frameSequence,
                string side,
                ulong pathIdentity)
            {
                FrameSequence = frameSequence;
                Side = side ?? string.Empty;
                PathIdentity = pathIdentity;
            }

            internal int FrameSequence { get; }
            internal string Side { get; }
            internal ulong PathIdentity { get; }
            public bool Equals(GeometryKey other) =>
                FrameSequence == other.FrameSequence &&
                PathIdentity == other.PathIdentity &&
                string.Equals(Side, other.Side, StringComparison.Ordinal);
            public override bool Equals(object obj) =>
                obj is GeometryKey other && Equals(other);
            public override int GetHashCode() =>
                HashCode.Combine(FrameSequence, Side, PathIdentity);
        }

        sealed class GeometryFrame
        {
            internal readonly HashSet<int> ContactIndices =
                new HashSet<int>();
            internal readonly HashSet<int> VertexIndices =
                new HashSet<int>();
            internal readonly List<GeometryContact> Contacts =
                new List<GeometryContact>();
            internal readonly List<GeometryVertex> Vertices =
                new List<GeometryVertex>();
        }

        readonly struct GeometryContact
        {
            internal GeometryContact(
                int surfaceIdentity,
                ulong candidateIdentity,
                Vector3 position,
                Vector3 normal)
            {
                SurfaceIdentity = surfaceIdentity;
                CandidateIdentity = candidateIdentity;
                Position = position;
                Normal = normal;
            }

            internal int SurfaceIdentity { get; }
            internal ulong CandidateIdentity { get; }
            internal Vector3 Position { get; }
            internal Vector3 Normal { get; }
            internal bool IsValid =>
                SurfaceIdentity != 0 && CandidateIdentity != 0;
        }

        readonly struct GeometryVertex
        {
            internal GeometryVertex(int index, Vector3 position)
            {
                Index = index;
                Position = position;
            }

            internal int Index { get; }
            internal Vector3 Position { get; }
        }

        readonly struct VerticalEndpointEvidence
        {
            internal VerticalEndpointEvidence(
                int frameSequence,
                string side,
                int landingSurfaceIdentity,
                int upperEdgeSurfaceIdentity,
                float landingHeight,
                float upperEdgeHeight,
                float verticalSeparationMeters)
            {
                FrameSequence = frameSequence;
                Side = side ?? string.Empty;
                LandingSurfaceIdentity = landingSurfaceIdentity;
                UpperEdgeSurfaceIdentity = upperEdgeSurfaceIdentity;
                LandingHeight = landingHeight;
                UpperEdgeHeight = upperEdgeHeight;
                VerticalSeparationMeters = verticalSeparationMeters;
            }

            internal int FrameSequence { get; }
            internal string Side { get; }
            internal int LandingSurfaceIdentity { get; }
            internal int UpperEdgeSurfaceIdentity { get; }
            internal float LandingHeight { get; }
            internal float UpperEdgeHeight { get; }
            internal float VerticalSeparationMeters { get; }
            internal bool IsValid =>
                LandingSurfaceIdentity != 0 && UpperEdgeSurfaceIdentity != 0 &&
                VerticalSeparationMeters > MinimumVerticalSeparation;
            internal VerticalEndpointEvidence WithFrame(
                int frameSequence,
                string side) =>
                new VerticalEndpointEvidence(
                    frameSequence,
                    side,
                    LandingSurfaceIdentity,
                    UpperEdgeSurfaceIdentity,
                    LandingHeight,
                    UpperEdgeHeight,
                    VerticalSeparationMeters);
        }

        sealed class CsvReader : IDisposable
        {
            readonly StreamReader m_Reader;
            readonly Dictionary<string, int> m_Columns;
            string[] m_Cells;

            internal CsvReader(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    throw new FileNotFoundException(
                        "Presentation Schedule evidence CSV is unavailable.", path);
                m_Reader = new StreamReader(path, Encoding.UTF8, true);
                string header = m_Reader.ReadLine();
                if (string.IsNullOrEmpty(header))
                    throw new InvalidDataException("Presentation Schedule evidence CSV is empty.");
                string[] names = Parse(header);
                m_Columns = new Dictionary<string, int>(
                    names.Length,
                    StringComparer.Ordinal);
                for (int i = 0; i < names.Length; i++)
                {
                    if (!m_Columns.TryAdd(names[i], i))
                        throw new InvalidDataException(
                            $"Presentation Schedule evidence CSV duplicates '{names[i]}'.");
                }
            }

            internal bool Read()
            {
                string line = m_Reader.ReadLine();
                if (line == null)
                    return false;
                m_Cells = Parse(line);
                if (m_Cells.Length != m_Columns.Count)
                    throw new InvalidDataException(
                        "Presentation Schedule evidence CSV row width is invalid.");
                return true;
            }

            internal string String(string name) => Cell(name);
            internal int Integer(string name) =>
                int.Parse(Cell(name), NumberStyles.Integer, CultureInfo.InvariantCulture);
            internal ulong UInt64(string name) =>
                ulong.Parse(Cell(name), NumberStyles.Integer, CultureInfo.InvariantCulture);
            internal float Single(string name) =>
                float.Parse(Cell(name), NumberStyles.Float, CultureInfo.InvariantCulture);
            internal bool Boolean(string name) => Cell(name) == "1";
            internal Vector3 Vector(string prefix) => new Vector3(
                Single(prefix + "X"),
                Single(prefix + "Y"),
                Single(prefix + "Z"));

            string Cell(string name)
            {
                if (!m_Columns.TryGetValue(name, out int index))
                    throw new InvalidDataException(
                        $"Presentation Schedule evidence CSV is missing '{name}'.");
                return m_Cells[index];
            }

            public void Dispose() => m_Reader.Dispose();

            static string[] Parse(string line)
            {
                var values = new List<string>();
                var value = new System.Text.StringBuilder();
                bool quoted = false;
                for (int i = 0; i < line.Length; i++)
                {
                    char current = line[i];
                    if (current == '"')
                    {
                        if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            value.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = !quoted;
                        }
                    }
                    else if (current == ',' && !quoted)
                    {
                        values.Add(value.ToString());
                        value.Clear();
                    }
                    else
                    {
                        value.Append(current);
                    }
                }
                if (quoted)
                    throw new InvalidDataException(
                        "Presentation Schedule evidence CSV has an unterminated quote.");
                values.Add(value.ToString());
                return values.ToArray();
            }
        }
    }
}
