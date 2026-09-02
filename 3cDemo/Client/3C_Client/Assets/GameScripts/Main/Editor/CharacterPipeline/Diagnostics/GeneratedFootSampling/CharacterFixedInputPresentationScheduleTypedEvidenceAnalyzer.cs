using System;
using System.Collections.Generic;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    internal static class CharacterFixedInputPresentationScheduleEvidenceRegistration
    {
        static readonly CharacterFixedInputPresentationScheduleTypedEvidenceAnalyzer
            s_Analyzer =
                new CharacterFixedInputPresentationScheduleTypedEvidenceAnalyzer();

        static CharacterFixedInputPresentationScheduleEvidenceRegistration()
        {
            CharacterFixedInputPresentationScheduleEvidenceRegistry.Register(
                s_Analyzer);
            AssemblyReloadEvents.beforeAssemblyReload += Unregister;
        }

        static void Unregister() =>
            CharacterFixedInputPresentationScheduleEvidenceRegistry.Unregister(
                s_Analyzer);
    }

    internal sealed class CharacterFixedInputPresentationScheduleTypedEvidenceAnalyzer :
        ICharacterFixedInputPresentationScheduleEvidenceAnalyzer
    {
        const float CorridorEpsilon = 0.0001f;
        const float EndpointDistance = 0.03f;
        const float UpperEdgeHorizontalDistance = 0.25f;
        const float MinimumVerticalSeparation = 0.1f;
        const string SamplerId = "character-foot-ik/full";
        const string SafetyFloorAvailableField =
            "character-foot-ik/main/foot/foot-motion/output-stages/safety-floor-available";
        const string ComponentUpField =
            "character-foot-ik/main/foot/ground-path/component-up";
        const string LastLandingField =
            "character-foot-ik/main/foot/ground-path/last-landing";
        const string RadiusField =
            "character-foot-ik/main/foot/ground-path/query/radius";
        const string SafetyFloorClampField =
            "character-foot-ik/main/foot/foot-motion/output-stages/safety-floor-clamp-meters";
        const string OriginalSoleKey =
            "character-foot-ik/main/foot/foot-motion-original-sole";
        const string NextLandingKey =
            "character-foot-ik/main/foot/ground-path-next-landing";
        const string NextLandingSurfaceKey =
            "character-foot-ik/main/foot/ground-path-next-landing-surface-identity";
        const string ContactsTable = "ground-contacts";
        const string ContactSurfaceField =
            "character-foot-ik/ground-contacts/foot/surface-identity";
        const string ContactCandidateField =
            "character-foot-ik/ground-contacts/foot/candidate-identity";
        const string ContactPositionField =
            "character-foot-ik/ground-contacts/foot/position";
        const string ContactNormalField =
            "character-foot-ik/ground-contacts/foot/normal";
        const string EnvelopeTable = "ground-envelope";
        const string EnvelopePositionField =
            "character-foot-ik/ground-envelope/foot/position";

        public CharacterFixedInputPresentationScheduleEvidence Analyze(
            string capabilityManifestPath)
        {
            DiagnosticDataset dataset = Open(capabilityManifestPath);
            DiagnosticDatasetFieldHandle safetyAvailable = RequireField(
                dataset,
                string.Empty,
                SafetyFloorAvailableField,
                DiagnosticValueKind.Boolean);
            DiagnosticDatasetFieldHandle componentUp = RequireField(
                dataset,
                string.Empty,
                ComponentUpField,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetFieldHandle lastLanding = RequireField(
                dataset,
                string.Empty,
                LastLandingField,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetFieldHandle nextLanding = RequireKey(
                dataset,
                NextLandingKey,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetFieldHandle originalSole = RequireKey(
                dataset,
                OriginalSoleKey,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetFieldHandle radius = RequireField(
                dataset,
                string.Empty,
                RadiusField,
                DiagnosticValueKind.Float32);
            DiagnosticDatasetFieldHandle safetyClamp = RequireField(
                dataset,
                string.Empty,
                SafetyFloorClampField,
                DiagnosticValueKind.Float32);
            DiagnosticDatasetFieldHandle nextSurface = RequireKey(
                dataset,
                NextLandingSurfaceKey,
                DiagnosticValueKind.Int32);
            Dictionary<GeometryKey, GeometryFrame> geometry =
                ReadGeometry(dataset);
            int accepted = 0;
            int outside = 0;
            int largeOutsideClamp = 0;
            float maximumOutsideClamp = 0f;
            int verticalEndpointCount = 0;
            VerticalEndpointEvidence representative = default;
            DiagnosticDatasetCursor cursor = dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                DiagnosticDatasetRow row = cursor.Current;
                if (!Boolean(row, safetyAvailable))
                    continue;
                accepted++;
                Vector3 up = Vector(row, componentUp);
                Vector3 last = Vector(row, lastLanding);
                Vector3 next = Vector(row, nextLanding);
                Vector3 sole = Vector(row, originalSole);
                float corridorDistance = DistanceToHorizontalAxis(
                    sole,
                    last,
                    next,
                    up);
                bool corridorOutside =
                    corridorDistance > Single(row, radius) + CorridorEpsilon;
                float clamp = Single(row, safetyClamp);
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
                    checked((int)row.SampleKey.Lineage.ValueHigh),
                    Side(row.SampleKey.DimensionId));
                if (!geometry.TryGetValue(key, out GeometryFrame frame) ||
                    !TryFindVerticalEndpoint(
                        frame,
                        next,
                        Integer(row, nextSurface),
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
            return new CharacterFixedInputPresentationScheduleEvidence(
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

        public CharacterFixedInputPresentationScheduleFootCoverage AnalyzeCoverage(
            string capabilityManifestPath,
            IReadOnlyList<ulong> scheduleRenderFrames)
        {
            if (scheduleRenderFrames == null || scheduleRenderFrames.Count == 0)
                throw new ArgumentException(
                    "Presentation Schedule coverage requires frames.",
                    nameof(scheduleRenderFrames));
            var indices = new Dictionary<ulong, int>(
                scheduleRenderFrames.Count);
            for (int i = 0; i < scheduleRenderFrames.Count; i++)
            {
                if (!indices.TryAdd(scheduleRenderFrames[i], i))
                    throw new InvalidDataException(
                        "Presentation Schedule duplicates a Render Frame.");
            }
            DiagnosticDataset dataset = Open(capabilityManifestPath);
            var covered = new HashSet<int>();
            int rowCount = 0;
            int first = int.MaxValue;
            int last = -1;
            DiagnosticDatasetCursor cursor = dataset.Main.CreateCursor();
            while (cursor.MoveNext())
            {
                ulong renderFrame = cursor.Current.SampleKey.Lineage.ValueHigh;
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

        static DiagnosticDataset Open(string manifestPath) =>
            new DiagnosticArtifactDatasetReader().Open(
                manifestPath,
                SamplerId);

        static Dictionary<GeometryKey, GeometryFrame> ReadGeometry(
            DiagnosticDataset dataset)
        {
            var result = new Dictionary<GeometryKey, GeometryFrame>();
            DiagnosticDatasetRegion contacts = dataset.RequireTable(
                ContactsTable);
            DiagnosticDatasetFieldHandle contactSurface = RequireField(
                dataset,
                ContactsTable,
                ContactSurfaceField,
                DiagnosticValueKind.Int32);
            DiagnosticDatasetFieldHandle contactCandidate = RequireField(
                dataset,
                ContactsTable,
                ContactCandidateField,
                DiagnosticValueKind.UInt64);
            DiagnosticDatasetFieldHandle contactPosition = RequireField(
                dataset,
                ContactsTable,
                ContactPositionField,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetFieldHandle contactNormal = RequireField(
                dataset,
                ContactsTable,
                ContactNormalField,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetCursor contactCursor = contacts.CreateCursor();
            while (contactCursor.MoveNext())
            {
                DiagnosticDatasetRow row = contactCursor.Current;
                GeometryFrame frame = RequireGeometryFrame(result, row);
                if (frame.ContactIndices.Add(row.TableRowIndex))
                {
                    frame.Contacts.Add(new GeometryContact(
                        Integer(row, contactSurface),
                        UInt64(row, contactCandidate),
                        Vector(row, contactPosition),
                        Vector(row, contactNormal)));
                }
            }
            DiagnosticDatasetRegion envelope = dataset.RequireTable(
                EnvelopeTable);
            DiagnosticDatasetFieldHandle envelopePosition = RequireField(
                dataset,
                EnvelopeTable,
                EnvelopePositionField,
                DiagnosticValueKind.Vector3);
            DiagnosticDatasetCursor envelopeCursor = envelope.CreateCursor();
            while (envelopeCursor.MoveNext())
            {
                DiagnosticDatasetRow row = envelopeCursor.Current;
                GeometryFrame frame = RequireGeometryFrame(result, row);
                if (frame.VertexIndices.Add(row.TableRowIndex))
                {
                    frame.Vertices.Add(new GeometryVertex(
                        row.TableRowIndex,
                        Vector(row, envelopePosition)));
                }
            }
            foreach (GeometryFrame frame in result.Values)
            {
                frame.Vertices.Sort((left, right) =>
                    left.Index.CompareTo(right.Index));
            }
            return result;
        }

        static GeometryFrame RequireGeometryFrame(
            IDictionary<GeometryKey, GeometryFrame> frames,
            in DiagnosticDatasetRow row)
        {
            var key = new GeometryKey(
                checked((int)row.SampleKey.Lineage.ValueHigh),
                Side(row.SampleKey.DimensionId));
            if (!frames.TryGetValue(key, out GeometryFrame frame))
            {
                frame = new GeometryFrame();
                frames.Add(key, frame);
            }
            return frame;
        }

        static DiagnosticDatasetFieldHandle RequireKey(
            DiagnosticDataset dataset,
            string key,
            DiagnosticValueKind kind)
        {
            if (!dataset.TryBindKey(
                    string.Empty,
                    key,
                    out DiagnosticDatasetFieldHandle handle))
            {
                throw new InvalidDataException(
                    $"Presentation Schedule evidence dataset is missing key '{key}'.");
            }
            return RequireKind(handle, kind);
        }

        static DiagnosticDatasetFieldHandle RequireField(
            DiagnosticDataset dataset,
            string tableId,
            string fieldId,
            DiagnosticValueKind kind)
        {
            if (!dataset.TryBindField(
                    tableId,
                    fieldId,
                    out DiagnosticDatasetFieldHandle handle))
            {
                throw new InvalidDataException(
                    $"Presentation Schedule evidence dataset is missing field '{fieldId}'.");
            }
            return RequireKind(handle, kind);
        }

        static DiagnosticDatasetFieldHandle RequireKind(
            in DiagnosticDatasetFieldHandle handle,
            DiagnosticValueKind kind)
        {
            if (handle.ValueKind != kind)
            {
                throw new InvalidDataException(
                    $"Presentation Schedule evidence field '{handle.FieldId}' expects {kind}, found {handle.ValueKind}.");
            }
            return handle;
        }

        static bool Boolean(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            RequireAvailable(row, handle);
            return row.GetBoolean(handle);
        }

        static int Integer(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            RequireAvailable(row, handle);
            return row.GetInt32(handle);
        }

        static ulong UInt64(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            RequireAvailable(row, handle);
            return row.GetUInt64(handle);
        }

        static float Single(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            RequireAvailable(row, handle);
            return row.GetFloat32(handle);
        }

        static Vector3 Vector(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            RequireAvailable(row, handle);
            DiagnosticVector3 value = row.GetVector3(handle);
            return new Vector3(value.X, value.Y, value.Z);
        }

        static void RequireAvailable(
            in DiagnosticDatasetRow row,
            in DiagnosticDatasetFieldHandle handle)
        {
            if (!row.IsAvailable(handle))
            {
                throw new InvalidDataException(
                    $"Presentation Schedule evidence field '{handle.FieldId}' is unavailable for sample {row.SampleKey.Sequence}.");
            }
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
                landingContact.SurfaceIdentity == upperContact.SurfaceIdentity)
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

        static string Side(string dimension)
        {
            if (dimension.EndsWith("/left", StringComparison.Ordinal))
                return "Left";
            if (dimension.EndsWith("/right", StringComparison.Ordinal))
                return "Right";
            throw new InvalidDataException(
                $"Foot sample dimension is invalid: {dimension}.");
        }

        readonly struct GeometryKey : IEquatable<GeometryKey>
        {
            internal GeometryKey(int frameSequence, string side)
            {
                FrameSequence = frameSequence;
                Side = side ?? string.Empty;
            }

            internal int FrameSequence { get; }
            internal string Side { get; }
            public bool Equals(GeometryKey other) =>
                FrameSequence == other.FrameSequence &&
                string.Equals(Side, other.Side, StringComparison.Ordinal);
            public override bool Equals(object obj) =>
                obj is GeometryKey other && Equals(other);
            public override int GetHashCode() =>
                HashCode.Combine(FrameSequence, Side);
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
                LandingSurfaceIdentity != 0 &&
                UpperEdgeSurfaceIdentity != 0 &&
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
    }
}
