using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.DotRecast
{
    public static class DotRecastSourceIdentity
    {
        public const string Tag = "2026.1.3";
        public const string Commit = "693b8f8d2e38b9d5ae98bf38510738c9f703c365";
        public const string AdapterVersion = "1";
    }

    public sealed class NavigationGeometrySource
    {
        public NavigationGeometrySource(string identity, byte area, double[] localVertices, int[] triangleIndices, double[] localToWorld)
        {
            Identity = RequireIdentity(identity, nameof(identity));
            Area = area;
            LocalVertices = localVertices == null ? throw new ArgumentNullException(nameof(localVertices)) : (double[])localVertices.Clone();
            TriangleIndices = triangleIndices == null ? throw new ArgumentNullException(nameof(triangleIndices)) : (int[])triangleIndices.Clone();
            LocalToWorld = localToWorld == null ? throw new ArgumentNullException(nameof(localToWorld)) : (double[])localToWorld.Clone();
            Validate();
        }

        public string Identity { get; }
        public byte Area { get; }
        public double[] LocalVertices { get; }
        public int[] TriangleIndices { get; }
        public double[] LocalToWorld { get; }

        public void AppendWorldGeometry(List<float> vertices, List<int> indices)
        {
            if (vertices == null || indices == null)
                throw new ArgumentNullException();
            int vertexOffset = vertices.Count / 3;
            for (int i = 0; i < LocalVertices.Length; i += 3)
            {
                double x = LocalVertices[i];
                double y = LocalVertices[i + 1];
                double z = LocalVertices[i + 2];
                vertices.Add(ToFiniteFloat(LocalToWorld[0] * x + LocalToWorld[4] * y + LocalToWorld[8] * z + LocalToWorld[12]));
                vertices.Add(ToFiniteFloat(LocalToWorld[1] * x + LocalToWorld[5] * y + LocalToWorld[9] * z + LocalToWorld[13]));
                vertices.Add(ToFiniteFloat(LocalToWorld[2] * x + LocalToWorld[6] * y + LocalToWorld[10] * z + LocalToWorld[14]));
            }
            for (int i = 0; i < TriangleIndices.Length; i++)
                indices.Add(vertexOffset + TriangleIndices[i]);
        }

        void Validate()
        {
            if (LocalVertices.Length < 9 || LocalVertices.Length % 3 != 0)
                throw new InvalidDataException($"Navigation geometry source '{Identity}' has an invalid vertex payload.");
            if (TriangleIndices.Length < 3 || TriangleIndices.Length % 3 != 0)
                throw new InvalidDataException($"Navigation geometry source '{Identity}' has an invalid triangle payload.");
            if (LocalToWorld.Length != 16)
                throw new InvalidDataException($"Navigation geometry source '{Identity}' requires a 4x4 transform.");
            for (int i = 0; i < LocalVertices.Length; i++)
                RequireFinite(LocalVertices[i], $"{Identity}/vertex/{i}");
            for (int i = 0; i < LocalToWorld.Length; i++)
                RequireFinite(LocalToWorld[i], $"{Identity}/transform/{i}");
            int vertexCount = LocalVertices.Length / 3;
            for (int i = 0; i < TriangleIndices.Length; i++)
            {
                if (TriangleIndices[i] < 0 || TriangleIndices[i] >= vertexCount)
                    throw new InvalidDataException($"Navigation geometry source '{Identity}' contains an out-of-range triangle index.");
            }
            var world = new List<float>(LocalVertices.Length);
            var triangles = new List<int>(TriangleIndices.Length);
            AppendWorldGeometry(world, triangles);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i] * 3;
                int b = triangles[i + 1] * 3;
                int c = triangles[i + 2] * 3;
                double abx = world[b] - world[a];
                double aby = world[b + 1] - world[a + 1];
                double abz = world[b + 2] - world[a + 2];
                double acx = world[c] - world[a];
                double acy = world[c + 1] - world[a + 1];
                double acz = world[c + 2] - world[a + 2];
                double cx = aby * acz - abz * acy;
                double cy = abz * acx - abx * acz;
                double cz = abx * acy - aby * acx;
                if (cx * cx + cy * cy + cz * cz <= 1e-12d)
                    throw new InvalidDataException($"Navigation geometry source '{Identity}' contains a degenerate triangle.");
            }
        }

        internal static string RequireIdentity(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Identity is required.", parameter);
            return value.Trim();
        }

        internal static void RequireFinite(double value, string identity)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException($"Navigation value '{identity}' is not finite.");
        }

        static float ToFiniteFloat(double value)
        {
            RequireFinite(value, "transformed-vertex");
            float result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result))
                throw new InvalidDataException("Transformed navigation vertex is outside Float32 range.");
            return result;
        }
    }

    public sealed class NavigationGeometryArtifact
    {
        readonly NavigationGeometrySource[] m_Sources;

        public NavigationGeometryArtifact(
            string mapId,
            string worldRevision,
            string sceneRevision,
            string coordinateProfile,
            IEnumerable<NavigationGeometrySource> sources,
            StableHash geometryHash = default)
        {
            MapId = NavigationGeometrySource.RequireIdentity(mapId, nameof(mapId));
            WorldRevision = NavigationGeometrySource.RequireIdentity(worldRevision, nameof(worldRevision));
            SceneRevision = NavigationGeometrySource.RequireIdentity(sceneRevision, nameof(sceneRevision));
            CoordinateProfile = NavigationGeometrySource.RequireIdentity(coordinateProfile, nameof(coordinateProfile));
            m_Sources = sources == null ? Array.Empty<NavigationGeometrySource>() : new List<NavigationGeometrySource>(sources).ToArray();
            if (m_Sources.Length == 0)
                throw new InvalidDataException("Navigation geometry requires at least one explicit mesh source.");
            Array.Sort(m_Sources, (left, right) => string.CompareOrdinal(left.Identity, right.Identity));
            for (int i = 0; i < m_Sources.Length; i++)
            {
                if (m_Sources[i] == null || i > 0 && string.Equals(m_Sources[i - 1].Identity, m_Sources[i].Identity, StringComparison.Ordinal))
                    throw new InvalidDataException("Navigation geometry contains a missing or duplicate mesh source identity.");
            }
            GeometryHash = geometryHash;
        }

        public string MapId { get; }
        public string WorldRevision { get; }
        public string SceneRevision { get; }
        public string CoordinateProfile { get; }
        public IReadOnlyList<NavigationGeometrySource> Sources => m_Sources;
        public StableHash GeometryHash { get; }
    }

    public static class NavigationGeometryArtifactCodec
    {
        const string Magic = "thirdperson.navigation.geometry";
        const int Schema = 1;

        public static byte[] Write(NavigationGeometryArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            byte[] payload = WritePayload(artifact);
            StableHash hash = NavigationArtifactHash.Compute(payload);
            using var writer = new CanonicalWriter();
            writer.WriteString(Magic);
            writer.WriteInt32(Schema);
            writer.WriteBytes(payload);
            writer.WriteString(hash.Value);
            return writer.ToArray();
        }

        public static NavigationGeometryArtifact Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal) || reader.ReadInt32() != Schema)
                throw new InvalidDataException("Navigation geometry magic or schema is unsupported.");
            byte[] payload = reader.ReadBytes();
            var expected = new StableHash(reader.ReadString());
            reader.RequireComplete();
            StableHash actual = NavigationArtifactHash.Compute(payload);
            if (actual != expected)
                throw new InvalidDataException("Navigation geometry hash does not match its canonical payload.");
            var payloadReader = new CanonicalReader(payload);
            string mapId = payloadReader.ReadString();
            string worldRevision = payloadReader.ReadString();
            string sceneRevision = payloadReader.ReadString();
            string coordinateProfile = payloadReader.ReadString();
            int count = ReadCount(payloadReader, "geometry source");
            var sources = new NavigationGeometrySource[count];
            for (int i = 0; i < count; i++)
            {
                string identity = payloadReader.ReadString();
                byte area = payloadReader.ReadByte();
                double[] transform = ReadDoubles(payloadReader, 16, "transform");
                double[] vertices = ReadDoubles(payloadReader, -1, "vertices");
                int[] indices = ReadInts(payloadReader, "indices");
                sources[i] = new NavigationGeometrySource(identity, area, vertices, indices, transform);
            }
            payloadReader.RequireComplete();
            return new NavigationGeometryArtifact(mapId, worldRevision, sceneRevision, coordinateProfile, sources, actual);
        }

        static byte[] WritePayload(NavigationGeometryArtifact artifact)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString(artifact.MapId);
            writer.WriteString(artifact.WorldRevision);
            writer.WriteString(artifact.SceneRevision);
            writer.WriteString(artifact.CoordinateProfile);
            writer.WriteInt32(artifact.Sources.Count);
            for (int i = 0; i < artifact.Sources.Count; i++)
            {
                NavigationGeometrySource source = artifact.Sources[i];
                writer.WriteString(source.Identity);
                writer.WriteByte(source.Area);
                WriteDoubles(writer, source.LocalToWorld);
                WriteDoubles(writer, source.LocalVertices);
                writer.WriteInt32(source.TriangleIndices.Length);
                for (int index = 0; index < source.TriangleIndices.Length; index++)
                    writer.WriteInt32(source.TriangleIndices[index]);
            }
            return writer.ToArray();
        }

        static void WriteDoubles(CanonicalWriter writer, double[] values)
        {
            writer.WriteInt32(values.Length);
            for (int i = 0; i < values.Length; i++)
                writer.WriteDouble(values[i]);
        }

        static double[] ReadDoubles(CanonicalReader reader, int requiredLength, string label)
        {
            int count = ReadCount(reader, label);
            if (requiredLength >= 0 && count != requiredLength)
                throw new InvalidDataException($"Navigation {label} length is invalid.");
            var result = new double[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadDouble();
            return result;
        }

        static int[] ReadInts(CanonicalReader reader, string label)
        {
            int count = ReadCount(reader, label);
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = reader.ReadInt32();
            return result;
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count <= 0 || count > 100000000)
                throw new InvalidDataException($"Navigation {label} count is invalid.");
            return count;
        }
    }

    public sealed class DotRecastBuildProfile
    {
        public DotRecastBuildProfile(
            double cellSize,
            double cellHeight,
            double agentHeight,
            double agentRadius,
            double agentMaxClimb,
            double agentMaxSlope,
            int regionMinSize,
            int regionMergeSize,
            double edgeMaxLength,
            double edgeMaxError,
            int verticesPerPolygon,
            double detailSampleDistance,
            double detailSampleMaxError)
        {
            CellSize = Positive(cellSize, nameof(cellSize));
            CellHeight = Positive(cellHeight, nameof(cellHeight));
            AgentHeight = Positive(agentHeight, nameof(agentHeight));
            AgentRadius = Positive(agentRadius, nameof(agentRadius));
            AgentMaxClimb = NonNegative(agentMaxClimb, nameof(agentMaxClimb));
            AgentMaxSlope = Positive(agentMaxSlope, nameof(agentMaxSlope));
            if (AgentMaxSlope >= 90d || regionMinSize <= 0 || regionMergeSize <= 0 || verticesPerPolygon < 3 || verticesPerPolygon > 12)
                throw new ArgumentOutOfRangeException();
            RegionMinSize = regionMinSize;
            RegionMergeSize = regionMergeSize;
            EdgeMaxLength = Positive(edgeMaxLength, nameof(edgeMaxLength));
            EdgeMaxError = Positive(edgeMaxError, nameof(edgeMaxError));
            VerticesPerPolygon = verticesPerPolygon;
            DetailSampleDistance = NonNegative(detailSampleDistance, nameof(detailSampleDistance));
            DetailSampleMaxError = NonNegative(detailSampleMaxError, nameof(detailSampleMaxError));
        }

        public double CellSize { get; }
        public double CellHeight { get; }
        public double AgentHeight { get; }
        public double AgentRadius { get; }
        public double AgentMaxClimb { get; }
        public double AgentMaxSlope { get; }
        public int RegionMinSize { get; }
        public int RegionMergeSize { get; }
        public double EdgeMaxLength { get; }
        public double EdgeMaxError { get; }
        public int VerticesPerPolygon { get; }
        public double DetailSampleDistance { get; }
        public double DetailSampleMaxError { get; }

        static double Positive(double value, string parameter)
        {
            NavigationGeometrySource.RequireFinite(value, parameter);
            if (value <= 0d)
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }

        static double NonNegative(double value, string parameter)
        {
            NavigationGeometrySource.RequireFinite(value, parameter);
            if (value < 0d)
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }
    }

    public sealed class DotRecastQueryProfile
    {
        readonly double[] m_AreaCosts;

        public DotRecastQueryProfile(
            double nearestExtentX,
            double nearestExtentY,
            double nearestExtentZ,
            double projectionTolerance,
            double heightTolerance,
            double maximumDisplacement,
            double boundaryInset,
            int maximumVisitedPolygons,
            int includeFlags,
            int excludeFlags,
            double[] areaCosts)
        {
            NearestExtentX = Positive(nearestExtentX, nameof(nearestExtentX));
            NearestExtentY = Positive(nearestExtentY, nameof(nearestExtentY));
            NearestExtentZ = Positive(nearestExtentZ, nameof(nearestExtentZ));
            ProjectionTolerance = NonNegative(projectionTolerance, nameof(projectionTolerance));
            HeightTolerance = NonNegative(heightTolerance, nameof(heightTolerance));
            MaximumDisplacement = Positive(maximumDisplacement, nameof(maximumDisplacement));
            BoundaryInset = Positive(boundaryInset, nameof(boundaryInset));
            if (maximumVisitedPolygons <= 0 || maximumVisitedPolygons > 4096 || includeFlags == 0)
                throw new ArgumentOutOfRangeException();
            MaximumVisitedPolygons = maximumVisitedPolygons;
            IncludeFlags = includeFlags;
            ExcludeFlags = excludeFlags;
            m_AreaCosts = areaCosts == null ? throw new ArgumentNullException(nameof(areaCosts)) : (double[])areaCosts.Clone();
            if (m_AreaCosts.Length == 0 || m_AreaCosts.Length > 64)
                throw new ArgumentOutOfRangeException(nameof(areaCosts));
            for (int i = 0; i < m_AreaCosts.Length; i++)
                Positive(m_AreaCosts[i], $"areaCosts/{i}");
            ConfigurationHash = ComputeConfigurationHash();
        }

        public double NearestExtentX { get; }
        public double NearestExtentY { get; }
        public double NearestExtentZ { get; }
        public double ProjectionTolerance { get; }
        public double HeightTolerance { get; }
        public double MaximumDisplacement { get; }
        public double BoundaryInset { get; }
        public int MaximumVisitedPolygons { get; }
        public int IncludeFlags { get; }
        public int ExcludeFlags { get; }
        public IReadOnlyList<double> AreaCosts => m_AreaCosts;
        public StableHash ConfigurationHash { get; }

        StableHash ComputeConfigurationHash()
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("dotrecast-query-profile/2");
            writer.WriteDouble(NearestExtentX);
            writer.WriteDouble(NearestExtentY);
            writer.WriteDouble(NearestExtentZ);
            writer.WriteDouble(ProjectionTolerance);
            writer.WriteDouble(HeightTolerance);
            writer.WriteDouble(MaximumDisplacement);
            writer.WriteDouble(BoundaryInset);
            writer.WriteInt32(MaximumVisitedPolygons);
            writer.WriteInt32(IncludeFlags);
            writer.WriteInt32(ExcludeFlags);
            writer.WriteInt32(m_AreaCosts.Length);
            for (int i = 0; i < m_AreaCosts.Length; i++)
                writer.WriteDouble(m_AreaCosts[i]);
            return writer.ComputeHash();
        }

        static double Positive(double value, string parameter)
        {
            NavigationGeometrySource.RequireFinite(value, parameter);
            if (value <= 0d)
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }

        static double NonNegative(double value, string parameter)
        {
            NavigationGeometrySource.RequireFinite(value, parameter);
            if (value < 0d)
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }
    }

    public readonly struct NavigationAreaDefinition
    {
        public NavigationAreaDefinition(byte id, string name)
        {
            Id = id;
            Name = NavigationGeometrySource.RequireIdentity(name, nameof(name));
        }

        public byte Id { get; }
        public string Name { get; }
    }

    public sealed class NavigationSurfaceArtifact
    {
        readonly NavigationAreaDefinition[] m_Areas;
        readonly byte[] m_NavMeshBytes;

        public NavigationSurfaceArtifact(
            string mapId,
            string worldRevision,
            StableHash geometryHash,
            string coordinateProfile,
            DotRecastBuildProfile buildProfile,
            DotRecastQueryProfile queryProfile,
            IEnumerable<NavigationAreaDefinition> areas,
            byte[] navMeshBytes,
            StableHash contentHash = default)
        {
            MapId = NavigationGeometrySource.RequireIdentity(mapId, nameof(mapId));
            WorldRevision = NavigationGeometrySource.RequireIdentity(worldRevision, nameof(worldRevision));
            if (!geometryHash.IsValid)
                throw new ArgumentException("Geometry hash is required.", nameof(geometryHash));
            GeometryHash = geometryHash;
            CoordinateProfile = NavigationGeometrySource.RequireIdentity(coordinateProfile, nameof(coordinateProfile));
            BuildProfile = buildProfile ?? throw new ArgumentNullException(nameof(buildProfile));
            QueryProfile = queryProfile ?? throw new ArgumentNullException(nameof(queryProfile));
            m_Areas = areas == null ? Array.Empty<NavigationAreaDefinition>() : new List<NavigationAreaDefinition>(areas).ToArray();
            if (m_Areas.Length == 0)
                throw new InvalidDataException("Navigation surface requires an explicit area catalog.");
            Array.Sort(m_Areas, (left, right) => left.Id.CompareTo(right.Id));
            for (int i = 1; i < m_Areas.Length; i++)
            {
                if (m_Areas[i - 1].Id == m_Areas[i].Id || string.Equals(m_Areas[i - 1].Name, m_Areas[i].Name, StringComparison.Ordinal))
                    throw new InvalidDataException("Navigation surface area catalog contains duplicate values.");
            }
            m_NavMeshBytes = navMeshBytes == null ? throw new ArgumentNullException(nameof(navMeshBytes)) : (byte[])navMeshBytes.Clone();
            if (m_NavMeshBytes.Length == 0)
                throw new InvalidDataException("Navigation surface contains no Detour navmesh bytes.");
            ContentHash = contentHash;
        }

        public string MapId { get; }
        public string WorldRevision { get; }
        public StableHash GeometryHash { get; }
        public string CoordinateProfile { get; }
        public DotRecastBuildProfile BuildProfile { get; }
        public DotRecastQueryProfile QueryProfile { get; }
        public IReadOnlyList<NavigationAreaDefinition> Areas => m_Areas;
        public byte[] NavMeshBytes => (byte[])m_NavMeshBytes.Clone();
        public StableHash ContentHash { get; }
        public StableHash WorldConfigurationHash => StableHash.Compute(
            "thirdperson.navigation.world-configuration/1",
            MapId,
            WorldRevision,
            GeometryHash.Value,
            ContentHash.Value,
            DotRecastSourceIdentity.Commit,
            DotRecastSourceIdentity.AdapterVersion);
    }

    public static class NavigationSurfaceArtifactCodec
    {
        const string Magic = "thirdperson.navigation.surface";
        const int Schema = 2;

        public static byte[] Write(NavigationSurfaceArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            byte[] payload = WritePayload(artifact);
            StableHash hash = NavigationArtifactHash.Compute(payload);
            using var writer = new CanonicalWriter();
            writer.WriteString(Magic);
            writer.WriteInt32(Schema);
            writer.WriteBytes(payload);
            writer.WriteString(hash.Value);
            return writer.ToArray();
        }

        public static NavigationSurfaceArtifact Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (!string.Equals(reader.ReadString(), Magic, StringComparison.Ordinal) || reader.ReadInt32() != Schema)
                throw new InvalidDataException("Navigation surface magic or schema is unsupported.");
            byte[] payload = reader.ReadBytes();
            var expected = new StableHash(reader.ReadString());
            reader.RequireComplete();
            StableHash actual = NavigationArtifactHash.Compute(payload);
            if (actual != expected)
                throw new InvalidDataException("Navigation surface hash does not match its canonical payload.");
            var payloadReader = new CanonicalReader(payload);
            if (!string.Equals(payloadReader.ReadString(), DotRecastSourceIdentity.Tag, StringComparison.Ordinal) ||
                !string.Equals(payloadReader.ReadString(), DotRecastSourceIdentity.Commit, StringComparison.Ordinal) ||
                !string.Equals(payloadReader.ReadString(), DotRecastSourceIdentity.AdapterVersion, StringComparison.Ordinal))
                throw new InvalidDataException("Navigation surface DotRecast source identity is unsupported.");
            string mapId = payloadReader.ReadString();
            string worldRevision = payloadReader.ReadString();
            var geometryHash = new StableHash(payloadReader.ReadString());
            string coordinateProfile = payloadReader.ReadString();
            DotRecastBuildProfile build = ReadBuildProfile(payloadReader);
            DotRecastQueryProfile query = ReadQueryProfile(payloadReader);
            int areaCount = ReadCount(payloadReader, "area");
            var areas = new NavigationAreaDefinition[areaCount];
            for (int i = 0; i < areaCount; i++)
                areas[i] = new NavigationAreaDefinition(payloadReader.ReadByte(), payloadReader.ReadString());
            byte[] navMeshBytes = payloadReader.ReadBytes();
            payloadReader.RequireComplete();
            return new NavigationSurfaceArtifact(mapId, worldRevision, geometryHash, coordinateProfile, build, query, areas, navMeshBytes, actual);
        }

        static byte[] WritePayload(NavigationSurfaceArtifact artifact)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString(DotRecastSourceIdentity.Tag);
            writer.WriteString(DotRecastSourceIdentity.Commit);
            writer.WriteString(DotRecastSourceIdentity.AdapterVersion);
            writer.WriteString(artifact.MapId);
            writer.WriteString(artifact.WorldRevision);
            writer.WriteString(artifact.GeometryHash.Value);
            writer.WriteString(artifact.CoordinateProfile);
            WriteBuildProfile(writer, artifact.BuildProfile);
            WriteQueryProfile(writer, artifact.QueryProfile);
            writer.WriteInt32(artifact.Areas.Count);
            for (int i = 0; i < artifact.Areas.Count; i++)
            {
                writer.WriteByte(artifact.Areas[i].Id);
                writer.WriteString(artifact.Areas[i].Name);
            }
            writer.WriteBytes(artifact.NavMeshBytes);
            return writer.ToArray();
        }

        static void WriteBuildProfile(CanonicalWriter writer, DotRecastBuildProfile value)
        {
            writer.WriteDouble(value.CellSize);
            writer.WriteDouble(value.CellHeight);
            writer.WriteDouble(value.AgentHeight);
            writer.WriteDouble(value.AgentRadius);
            writer.WriteDouble(value.AgentMaxClimb);
            writer.WriteDouble(value.AgentMaxSlope);
            writer.WriteInt32(value.RegionMinSize);
            writer.WriteInt32(value.RegionMergeSize);
            writer.WriteDouble(value.EdgeMaxLength);
            writer.WriteDouble(value.EdgeMaxError);
            writer.WriteInt32(value.VerticesPerPolygon);
            writer.WriteDouble(value.DetailSampleDistance);
            writer.WriteDouble(value.DetailSampleMaxError);
        }

        static DotRecastBuildProfile ReadBuildProfile(CanonicalReader reader)
        {
            return new DotRecastBuildProfile(
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadInt32(), reader.ReadInt32(),
                reader.ReadDouble(), reader.ReadDouble(), reader.ReadInt32(), reader.ReadDouble(), reader.ReadDouble());
        }

        static void WriteQueryProfile(CanonicalWriter writer, DotRecastQueryProfile value)
        {
            writer.WriteDouble(value.NearestExtentX);
            writer.WriteDouble(value.NearestExtentY);
            writer.WriteDouble(value.NearestExtentZ);
            writer.WriteDouble(value.ProjectionTolerance);
            writer.WriteDouble(value.HeightTolerance);
            writer.WriteDouble(value.MaximumDisplacement);
            writer.WriteDouble(value.BoundaryInset);
            writer.WriteInt32(value.MaximumVisitedPolygons);
            writer.WriteInt32(value.IncludeFlags);
            writer.WriteInt32(value.ExcludeFlags);
            writer.WriteInt32(value.AreaCosts.Count);
            for (int i = 0; i < value.AreaCosts.Count; i++)
                writer.WriteDouble(value.AreaCosts[i]);
        }

        static DotRecastQueryProfile ReadQueryProfile(CanonicalReader reader)
        {
            double x = reader.ReadDouble();
            double y = reader.ReadDouble();
            double z = reader.ReadDouble();
            double projection = reader.ReadDouble();
            double height = reader.ReadDouble();
            double displacement = reader.ReadDouble();
            double boundary = reader.ReadDouble();
            int visited = reader.ReadInt32();
            int include = reader.ReadInt32();
            int exclude = reader.ReadInt32();
            int costCount = ReadCount(reader, "area cost");
            var costs = new double[costCount];
            for (int i = 0; i < costs.Length; i++)
                costs[i] = reader.ReadDouble();
            return new DotRecastQueryProfile(x, y, z, projection, height, displacement, boundary, visited, include, exclude, costs);
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count <= 0 || count > 4096)
                throw new InvalidDataException($"Navigation {label} count is invalid.");
            return count;
        }
    }

    static class NavigationArtifactHash
    {
        public static StableHash Compute(byte[] bytes)
        {
            using var writer = new CanonicalWriter();
            writer.WriteRawBytes(bytes ?? throw new ArgumentNullException(nameof(bytes)), 0, bytes.Length);
            return writer.ComputeHash();
        }
    }
}
