using System;
using System.Collections.Generic;
using System.IO;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using ThirdPersonSimulation.DotRecast;

namespace ThirdPersonSimulation.NavigationBuildTool
{
    public static class NavigationSurfaceBuilder
    {
        public static NavigationSurfaceArtifact Build(
            NavigationGeometryArtifact geometry,
            DotRecastBuildProfile buildProfile,
            DotRecastQueryProfile queryProfile,
            IReadOnlyList<NavigationAreaDefinition> areas)
        {
            if (geometry == null || buildProfile == null || queryProfile == null || areas == null)
                throw new ArgumentNullException();
            var vertices = new List<float>();
            var indices = new List<int>();
            for (int i = 0; i < geometry.Sources.Count; i++)
            {
                if (geometry.Sources[i].Area != 0)
                    throw new InvalidDataException("Navigation builder version 1 supports only the explicit Ground area id 0.");
                geometry.Sources[i].AppendWorldGeometry(vertices, indices);
            }
            var input = new RcSampleInputGeomProvider(vertices.ToArray(), indices.ToArray());
            var config = new RcConfig(
                RcPartition.WATERSHED,
                ToFloat(buildProfile.CellSize),
                ToFloat(buildProfile.CellHeight),
                ToFloat(buildProfile.AgentMaxSlope),
                ToFloat(buildProfile.AgentHeight),
                ToFloat(buildProfile.AgentRadius),
                ToFloat(buildProfile.AgentMaxClimb),
                buildProfile.RegionMinSize,
                buildProfile.RegionMergeSize,
                ToFloat(buildProfile.EdgeMaxLength),
                ToFloat(buildProfile.EdgeMaxError),
                buildProfile.VerticesPerPolygon,
                ToFloat(buildProfile.DetailSampleDistance),
                ToFloat(buildProfile.DetailSampleMaxError),
                true,
                true,
                true,
                new RcAreaModification(RcRecast.RC_WALKABLE_AREA),
                true);
            var builderConfig = new RcBuilderConfig(config, input.GetMeshBoundsMin(), input.GetMeshBoundsMax());
            RcBuilderResult result = new RcBuilder().Build(input, builderConfig, false);
            RcPolyMesh polygonMesh = result.Mesh;
            if (polygonMesh == null || polygonMesh.npolys == 0)
                throw new InvalidDataException(
                    $"Recast produced no navigation polygons: vertices={vertices.Count / 3}, " +
                    $"triangles={indices.Count / 3}, upwardTriangles={CountUpwardTriangles(vertices, indices)}, " +
                    $"bounds=({input.GetMeshBoundsMin().X:R},{input.GetMeshBoundsMin().Y:R},{input.GetMeshBoundsMin().Z:R})-" +
                    $"({input.GetMeshBoundsMax().X:R},{input.GetMeshBoundsMax().Y:R},{input.GetMeshBoundsMax().Z:R}).");
            for (int i = 0; i < polygonMesh.npolys; i++)
            {
                polygonMesh.areas[i] = 0;
                polygonMesh.flags[i] = 1;
            }
            RcPolyMeshDetail detailMesh = result.MeshDetail;
            var create = new DtNavMeshCreateParams
            {
                verts = polygonMesh.verts,
                vertCount = polygonMesh.nverts,
                polys = polygonMesh.polys,
                polyAreas = polygonMesh.areas,
                polyFlags = polygonMesh.flags,
                polyCount = polygonMesh.npolys,
                nvp = polygonMesh.nvp,
                detailMeshes = detailMesh?.meshes,
                detailVerts = detailMesh?.verts,
                detailVertsCount = detailMesh?.nverts ?? 0,
                detailTris = detailMesh?.tris,
                detailTriCount = detailMesh?.ntris ?? 0,
                walkableHeight = ToFloat(buildProfile.AgentHeight),
                walkableRadius = ToFloat(buildProfile.AgentRadius),
                walkableClimb = ToFloat(buildProfile.AgentMaxClimb),
                bmin = polygonMesh.bmin,
                bmax = polygonMesh.bmax,
                cs = ToFloat(buildProfile.CellSize),
                ch = ToFloat(buildProfile.CellHeight),
                buildBvTree = true
            };
            DtMeshData meshData = DtNavMeshBuilder.CreateNavMeshData(create) ??
                throw new InvalidDataException("Detour failed to create navigation mesh data.");
            var navMesh = new DtNavMesh();
            DtStatus status = navMesh.Init(meshData, polygonMesh.nvp, 0);
            if (status.Failed())
                throw new InvalidDataException("Detour failed to initialize the navigation mesh.");
            byte[] navMeshBytes;
            using (var stream = new MemoryStream())
            {
                using var writer = new BinaryWriter(stream);
                new DtMeshSetWriter().Write(writer, navMesh, RcByteOrder.LITTLE_ENDIAN, false);
                writer.Flush();
                navMeshBytes = stream.ToArray();
            }
            var artifact = new NavigationSurfaceArtifact(
                geometry.MapId,
                geometry.WorldRevision,
                geometry.GeometryHash,
                geometry.CoordinateProfile,
                buildProfile,
                queryProfile,
                areas,
                navMeshBytes);
            return NavigationSurfaceArtifactCodec.Read(NavigationSurfaceArtifactCodec.Write(artifact));
        }

        static float ToFloat(double value)
        {
            float result = (float)value;
            if (float.IsNaN(result) || float.IsInfinity(result))
                throw new InvalidDataException("Navigation build profile exceeds Float32 range.");
            return result;
        }

        static int CountUpwardTriangles(IReadOnlyList<float> vertices, IReadOnlyList<int> indices)
        {
            int count = 0;
            for (int i = 0; i < indices.Count; i += 3)
            {
                int a = indices[i] * 3;
                int b = indices[i + 1] * 3;
                int c = indices[i + 2] * 3;
                float abx = vertices[b] - vertices[a];
                float abz = vertices[b + 2] - vertices[a + 2];
                float acx = vertices[c] - vertices[a];
                float acz = vertices[c + 2] - vertices[a + 2];
                if (abz * acx - abx * acz > 0f)
                    count++;
            }
            return count;
        }

    }
}
