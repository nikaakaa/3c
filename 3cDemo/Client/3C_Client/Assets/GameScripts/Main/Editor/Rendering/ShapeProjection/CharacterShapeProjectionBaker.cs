using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    public readonly struct ShapeProjectionBakeReport
    {
        public readonly int RendererCount;
        public readonly int VertexCount;
        public readonly int TriangleCount;
        public readonly int ExcludedTriangleCount;
        public readonly int RegionCount;
        public readonly int SharedChainCount;
        public readonly int DependencyCount;
        public readonly Hash128 ContentHash;

        public ShapeProjectionBakeReport(int rendererCount, int vertexCount, int triangleCount, int excludedTriangleCount,
            int regionCount, int sharedChainCount, int dependencyCount, Hash128 contentHash)
        {
            RendererCount = rendererCount;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            ExcludedTriangleCount = excludedTriangleCount;
            RegionCount = regionCount;
            SharedChainCount = sharedChainCount;
            DependencyCount = dependencyCount;
            ContentHash = contentHash;
        }
    }

    public static class CharacterShapeProjectionBaker
    {
        readonly struct VertexKey : IEquatable<VertexKey>, IComparable<VertexKey>
        {
            const float Scale = 100000f;
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public VertexKey(Vector3 value)
            {
                X = Mathf.RoundToInt(value.x * Scale);
                Y = Mathf.RoundToInt(value.y * Scale);
                Z = Mathf.RoundToInt(value.z * Scale);
            }

            public bool Equals(VertexKey other) => X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);
            public override int GetHashCode() => ((X * 397) ^ Y) * 397 ^ Z;

            public int CompareTo(VertexKey other)
            {
                int x = X.CompareTo(other.X);
                if (x != 0)
                    return x;
                int y = Y.CompareTo(other.Y);
                return y != 0 ? y : Z.CompareTo(other.Z);
            }
        }

        readonly struct EdgeKey : IEquatable<EdgeKey>, IComparable<EdgeKey>
        {
            public readonly int A;
            public readonly int B;

            public EdgeKey(int first, int second)
            {
                A = Mathf.Min(first, second);
                B = Mathf.Max(first, second);
            }

            public bool Equals(EdgeKey other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is EdgeKey other && Equals(other);
            public override int GetHashCode() => (A * 397) ^ B;

            public int CompareTo(EdgeKey other)
            {
                int a = A.CompareTo(other.A);
                return a != 0 ? a : B.CompareTo(other.B);
            }
        }

        readonly struct BoundaryPair : IEquatable<BoundaryPair>, IComparable<BoundaryPair>
        {
            public readonly int Left;
            public readonly int Right;

            public BoundaryPair(int left, int right)
            {
                Left = left;
                Right = right;
            }

            public bool Equals(BoundaryPair other) => Left == other.Left && Right == other.Right;
            public override bool Equals(object obj) => obj is BoundaryPair other && Equals(other);
            public override int GetHashCode() => (Left * 397) ^ Right;

            public int CompareTo(BoundaryPair other)
            {
                int left = Left.CompareTo(other.Left);
                return left != 0 ? left : Right.CompareTo(other.Right);
            }
        }

        sealed class TriangleBuild
        {
            public int Order;
            public int A;
            public int B;
            public int C;
            public int CanonicalA;
            public int CanonicalB;
            public int CanonicalC;
            public Color Color;
            public int Region = -1;
        }

        readonly struct EdgeUse
        {
            public readonly int Triangle;
            public readonly int StartCanonical;
            public readonly int EndCanonical;
            public readonly int StartVertex;
            public readonly int EndVertex;

            public EdgeUse(int triangle, int startCanonical, int endCanonical, int startVertex, int endVertex)
            {
                Triangle = triangle;
                StartCanonical = startCanonical;
                EndCanonical = endCanonical;
                StartVertex = startVertex;
                EndVertex = endVertex;
            }
        }

        sealed class RegionBuild
        {
            public int RendererSlot;
            public int LocalIndex;
            public readonly List<TriangleBuild> Triangles = new List<TriangleBuild>();
            public Color RepresentativeColor;
            public int TriangleStart;
            public readonly List<int> ChainIndices = new List<int>();
        }

        readonly struct BoundarySegment
        {
            public readonly int StartCanonical;
            public readonly int EndCanonical;
            public readonly int StartVertex;
            public readonly int EndVertex;

            public BoundarySegment(int startCanonical, int endCanonical, int startVertex, int endVertex)
            {
                StartCanonical = startCanonical;
                EndCanonical = endCanonical;
                StartVertex = startVertex;
                EndVertex = endVertex;
            }
        }

        sealed class ChainBuild
        {
            public int RendererSlot;
            public int LeftRegion;
            public int RightRegion;
            public readonly List<int> Vertices = new List<int>();
        }

        sealed class RendererBuild
        {
            public int Slot;
            public string SlotId;
            public SkinnedMeshRenderer Renderer;
            public ShapeProjectionEditorMeshData MeshData;
            public int VertexStart;
            public int ExcludedTriangles;
            public readonly List<TriangleBuild> Triangles = new List<TriangleBuild>();
            public readonly Dictionary<EdgeKey, List<EdgeUse>> Edges = new Dictionary<EdgeKey, List<EdgeUse>>();
            public readonly List<RegionBuild> Regions = new List<RegionBuild>();
        }

        public static ShapeProjectionBakeReport Bake(CharacterShapeProjectionProfile profile,
            IReadOnlyList<SkinnedMeshRenderer> renderers, CharacterShapeProjectionArtifact artifact)
        {
            if (profile == null || artifact == null)
                throw new InvalidOperationException("Bake必须提供Profile和Artifact");
            if (renderers == null || renderers.Count == 0)
                throw new InvalidOperationException("Bake必须显式选择至少一个SkinnedMeshRenderer");

            profile.EnsureIdentity();
            ShapeProjectionValidationResult profileValidation = profile.ValidateProfile();
            if (!profileValidation.IsValid)
                throw new InvalidOperationException(profileValidation.Error);

            ShapeProjectionArtifactId artifactId = artifact.ArtifactId.IsValid
                ? artifact.ArtifactId
                : new ShapeProjectionArtifactId(Guid.NewGuid().ToString("N"));
            Hash128 profileHash = ComputeProfileHash(profile);

            List<RendererBuild> rendererBuilds = new List<RendererBuild>(renderers.Count);
            List<RegionBuild> allRegions = new List<RegionBuild>();
            List<ChainBuild> allChains = new List<ChainBuild>();
            int vertexStart = 0;
            int excludedTriangleCount = 0;

            using (ShapeProjectionMaterialSamplerCache samplers = new ShapeProjectionMaterialSamplerCache())
            {
                for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
                {
                    SkinnedMeshRenderer renderer = renderers[rendererIndex];
                    if (renderer == null || renderer.sharedMesh == null)
                        throw new InvalidOperationException($"Renderer选择{rendererIndex}为空或没有Mesh");
                    for (int prior = 0; prior < rendererIndex; prior++)
                    {
                        if (renderers[prior] == renderer || string.Equals(renderers[prior].name, renderer.name, StringComparison.Ordinal))
                            throw new InvalidOperationException($"Renderer或Slot名称重复：{renderer.name}");
                    }

                    RendererBuild build = BuildRenderer(rendererIndex, renderer, vertexStart, profile, samplers);
                    BuildRegions(build, profile, allRegions.Count);
                    rendererBuilds.Add(build);
                    allRegions.AddRange(build.Regions);
                    BuildBoundaryChains(build, allChains);
                    vertexStart += build.MeshData.Vertices.Length;
                    excludedTriangleCount += build.ExcludedTriangles;
                }
            }

            ValidateCapacity(profile.Capacity, rendererBuilds, allRegions, allChains, vertexStart);

            List<int> triangleIndices = new List<int>();
            ShapeProjectionRegionRecord[] regionRecords = new ShapeProjectionRegionRecord[allRegions.Count];
            for (int regionIndex = 0; regionIndex < allRegions.Count; regionIndex++)
            {
                RegionBuild region = allRegions[regionIndex];
                region.TriangleStart = triangleIndices.Count / 3;
                for (int triangleIndex = 0; triangleIndex < region.Triangles.Count; triangleIndex++)
                {
                    TriangleBuild triangle = region.Triangles[triangleIndex];
                    triangleIndices.Add(triangle.A);
                    triangleIndices.Add(triangle.B);
                    triangleIndices.Add(triangle.C);
                }

                regionRecords[regionIndex] = new ShapeProjectionRegionRecord
                {
                    RegionId = new ShapeProjectionRegionId($"{artifactId.Value}:region:{region.RendererSlot}:{region.LocalIndex}"),
                    RendererSlot = region.RendererSlot,
                    TriangleStart = region.TriangleStart,
                    TriangleCount = region.Triangles.Count,
                    RepresentativeColor = region.RepresentativeColor,
                    Label = $"{rendererBuilds[region.RendererSlot].SlotId}/Region {region.LocalIndex}"
                };
            }

            List<int> chainVertexIndices = new List<int>();
            ShapeProjectionSharedChainRecord[] chainRecords = new ShapeProjectionSharedChainRecord[allChains.Count];
            for (int chainIndex = 0; chainIndex < allChains.Count; chainIndex++)
            {
                ChainBuild chain = allChains[chainIndex];
                int vertexRangeStart = chainVertexIndices.Count;
                chainVertexIndices.AddRange(chain.Vertices);
                chainRecords[chainIndex] = new ShapeProjectionSharedChainRecord
                {
                    ChainId = new ShapeProjectionChainId($"{artifactId.Value}:chain:{chain.RendererSlot}:{chainIndex}"),
                    RendererSlot = chain.RendererSlot,
                    LeftRegion = chain.LeftRegion,
                    RightRegion = chain.RightRegion,
                    VertexStart = vertexRangeStart,
                    VertexCount = chain.Vertices.Count
                };
                allRegions[chain.LeftRegion].ChainIndices.Add(chainIndex);
                if (chain.RightRegion >= 0)
                    allRegions[chain.RightRegion].ChainIndices.Add(chainIndex);
            }

            List<int> regionChainIndices = new List<int>();
            for (int regionIndex = 0; regionIndex < allRegions.Count; regionIndex++)
            {
                RegionBuild region = allRegions[regionIndex];
                region.ChainIndices.Sort();
                ShapeProjectionRegionRecord record = regionRecords[regionIndex];
                record.SharedChainStart = regionChainIndices.Count;
                record.SharedChainCount = region.ChainIndices.Count;
                regionChainIndices.AddRange(region.ChainIndices);
                regionRecords[regionIndex] = record;
            }

            ShapeProjectionRendererRecord[] rendererRecords = BuildRendererRecords(rendererBuilds, allRegions);
            ShapeProjectionAssetDependency[] dependencies = BuildDependencies(rendererBuilds);
            Hash128 contentHash = ComputeArtifactHash(profileHash, dependencies, rendererRecords, regionRecords, triangleIndices,
                chainRecords, chainVertexIndices, regionChainIndices);

            CharacterShapeProjectionArtifact validationArtifact = ScriptableObject.CreateInstance<CharacterShapeProjectionArtifact>();
            try
            {
                validationArtifact.ReplaceContent(artifactId, profile.ProfileId, profile.Revision, profileHash, contentHash,
                    profile.Capacity, dependencies, rendererRecords, regionRecords, triangleIndices.ToArray(), chainRecords,
                    chainVertexIndices.ToArray(), regionChainIndices.ToArray(), triangleIndices.Count / 3, excludedTriangleCount);
                ShapeProjectionValidationResult validation = validationArtifact.ValidateArtifact();
                if (!validation.IsValid)
                    throw new InvalidOperationException(validation.Error);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validationArtifact);
            }

            Undo.RecordObjects(new UnityEngine.Object[] { profile, artifact }, "Bake Character Shape Projection Artifact");
            profile.PublishContentHash(profileHash);
            artifact.ReplaceContent(artifactId, profile.ProfileId, profile.Revision, profileHash, contentHash,
                profile.Capacity, dependencies, rendererRecords, regionRecords, triangleIndices.ToArray(), chainRecords,
                chainVertexIndices.ToArray(), regionChainIndices.ToArray(), triangleIndices.Count / 3, excludedTriangleCount);
            ShapeProjectionValidationResult artifactValidation = artifact.ValidateArtifact();
            if (!artifactValidation.IsValid)
                throw new InvalidOperationException(artifactValidation.Error);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(artifact);
            AssetDatabase.SaveAssetIfDirty(profile);
            AssetDatabase.SaveAssetIfDirty(artifact);

            return new ShapeProjectionBakeReport(rendererRecords.Length, vertexStart, triangleIndices.Count / 3,
                excludedTriangleCount, regionRecords.Length, chainRecords.Length, dependencies.Length, contentHash);
        }

        static RendererBuild BuildRenderer(int slot, SkinnedMeshRenderer renderer, int vertexStart,
            CharacterShapeProjectionProfile profile, ShapeProjectionMaterialSamplerCache samplers)
        {
            ShapeProjectionEditorMeshData meshData = ShapeProjectionEditorMeshData.Read(renderer.sharedMesh);
            Material[] materials = renderer.sharedMaterials;
            if (meshData.SubmeshIndices.Length != materials.Length)
                throw new InvalidOperationException($"Renderer {renderer.name}的Submesh与Material数量不一致");

            RendererBuild build = new RendererBuild
            {
                Slot = slot,
                SlotId = renderer.name,
                Renderer = renderer,
                MeshData = meshData,
                VertexStart = vertexStart
            };

            Dictionary<VertexKey, int> canonicalIds = new Dictionary<VertexKey, int>(meshData.Vertices.Length);
            int[] vertexCanonical = new int[meshData.Vertices.Length];
            for (int vertexIndex = 0; vertexIndex < meshData.Vertices.Length; vertexIndex++)
            {
                VertexKey key = new VertexKey(meshData.Vertices[vertexIndex]);
                if (!canonicalIds.TryGetValue(key, out int canonical))
                {
                    canonical = canonicalIds.Count;
                    canonicalIds.Add(key, canonical);
                }
                vertexCanonical[vertexIndex] = canonical;
            }

            int order = 0;
            for (int submesh = 0; submesh < meshData.SubmeshIndices.Length; submesh++)
            {
                if (!profile.TryGetSubmeshRule(build.SlotId, submesh, materials[submesh], out ShapeProjectionSubmeshRule rule))
                    throw new InvalidOperationException($"Renderer {build.SlotId} Submesh {submesh}没有与当前Material一致的显式规则");
                int[] indices = meshData.SubmeshIndices[submesh];
                if (indices.Length % 3 != 0)
                    throw new InvalidOperationException($"Renderer {renderer.name} Submesh {submesh} index数量不是3的倍数");

                for (int index = 0; index < indices.Length; index += 3, order++)
                {
                    int localA = indices[index];
                    int localB = indices[index + 1];
                    int localC = indices[index + 2];
                    if (!samplers.TrySampleTriangle(rule, meshData.Uv[localA], meshData.Uv[localB], meshData.Uv[localC], out Color color))
                    {
                        build.ExcludedTriangles++;
                        continue;
                    }

                    TriangleBuild triangle = new TriangleBuild
                    {
                        Order = order,
                        A = vertexStart + localA,
                        B = vertexStart + localB,
                        C = vertexStart + localC,
                        CanonicalA = vertexCanonical[localA],
                        CanonicalB = vertexCanonical[localB],
                        CanonicalC = vertexCanonical[localC],
                        Color = color
                    };
                    int triangleIndex = build.Triangles.Count;
                    build.Triangles.Add(triangle);
                    AddEdge(build.Edges, triangleIndex, triangle.CanonicalA, triangle.CanonicalB, triangle.A, triangle.B);
                    AddEdge(build.Edges, triangleIndex, triangle.CanonicalB, triangle.CanonicalC, triangle.B, triangle.C);
                    AddEdge(build.Edges, triangleIndex, triangle.CanonicalC, triangle.CanonicalA, triangle.C, triangle.A);
                }
            }

            if (build.Triangles.Count == 0)
                throw new InvalidOperationException($"Renderer {renderer.name}没有被材质规则纳入的三角形");
            return build;
        }

        static void BuildRegions(RendererBuild build, CharacterShapeProjectionProfile profile, int globalRegionStart)
        {
            int[] parent = new int[build.Triangles.Count];
            for (int i = 0; i < parent.Length; i++)
                parent[i] = i;

            List<KeyValuePair<EdgeKey, List<EdgeUse>>> sortedEdges = SortedEdges(build.Edges);
            for (int edgeIndex = 0; edgeIndex < sortedEdges.Count; edgeIndex++)
            {
                List<EdgeUse> uses = sortedEdges[edgeIndex].Value;
                for (int i = 0; i < uses.Count; i++)
                {
                    for (int j = i + 1; j < uses.Count; j++)
                    {
                        Color left = build.Triangles[uses[i].Triangle].Color;
                        Color right = build.Triangles[uses[j].Triangle].Color;
                        if (ColorDistance255(left, right) <= profile.ColorClusterThreshold)
                            Union(parent, uses[i].Triangle, uses[j].Triangle);
                    }
                }
            }

            bool merged;
            do
            {
                merged = false;
                Dictionary<int, List<int>> groups = BuildGroups(parent);
                List<int> roots = new List<int>(groups.Keys);
                roots.Sort();
                for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
                {
                    int root = Find(parent, roots[rootIndex]);
                    if (!groups.TryGetValue(root, out List<int> group) || group.Count > profile.SmallRegionTriangleLimit)
                        continue;

                    Color color = AverageColor(build.Triangles, group);
                    int bestNeighbor = -1;
                    float bestDistance = float.MaxValue;
                    for (int edgeIndex = 0; edgeIndex < sortedEdges.Count; edgeIndex++)
                    {
                        List<EdgeUse> uses = sortedEdges[edgeIndex].Value;
                        for (int i = 0; i < uses.Count; i++)
                        {
                            int leftRoot = Find(parent, uses[i].Triangle);
                            if (leftRoot != root)
                                continue;
                            for (int j = 0; j < uses.Count; j++)
                            {
                                int rightRoot = Find(parent, uses[j].Triangle);
                                if (rightRoot == root || !groups.TryGetValue(rightRoot, out List<int> neighborGroup))
                                    continue;
                                float distance = ColorDistance255(color, AverageColor(build.Triangles, neighborGroup));
                                if (distance < bestDistance || Mathf.Approximately(distance, bestDistance) && rightRoot < bestNeighbor)
                                {
                                    bestDistance = distance;
                                    bestNeighbor = rightRoot;
                                }
                            }
                        }
                    }

                    if (bestNeighbor >= 0 && bestDistance <= profile.SmallRegionMergeThreshold)
                    {
                        Union(parent, root, bestNeighbor);
                        merged = true;
                        break;
                    }
                }
            } while (merged);

            Dictionary<int, List<int>> finalGroups = BuildGroups(parent);
            List<List<int>> sortedGroups = new List<List<int>>(finalGroups.Values);
            sortedGroups.Sort((left, right) => build.Triangles[MinOrderTriangle(build.Triangles, left)].Order
                .CompareTo(build.Triangles[MinOrderTriangle(build.Triangles, right)].Order));

            int localRegion = 0;
            for (int groupIndex = 0; groupIndex < sortedGroups.Count; groupIndex++)
            {
                List<int> triangleGroup = sortedGroups[groupIndex];
                if (triangleGroup.Count < profile.MinimumProjectedRegionTriangles)
                {
                    build.ExcludedTriangles += triangleGroup.Count;
                    continue;
                }
                triangleGroup.Sort((left, right) => build.Triangles[left].Order.CompareTo(build.Triangles[right].Order));
                RegionBuild region = new RegionBuild
                {
                    RendererSlot = build.Slot,
                    LocalIndex = localRegion,
                    RepresentativeColor = AverageColor(build.Triangles, triangleGroup)
                };
                int globalRegion = globalRegionStart + localRegion;
                for (int i = 0; i < triangleGroup.Count; i++)
                {
                    TriangleBuild triangle = build.Triangles[triangleGroup[i]];
                    triangle.Region = globalRegion;
                    region.Triangles.Add(triangle);
                }
                build.Regions.Add(region);
                localRegion++;
            }
        }

        static void BuildBoundaryChains(RendererBuild build, List<ChainBuild> output)
        {
            Dictionary<BoundaryPair, List<BoundarySegment>> groups = new Dictionary<BoundaryPair, List<BoundarySegment>>();
            List<KeyValuePair<EdgeKey, List<EdgeUse>>> edges = SortedEdges(build.Edges);
            for (int edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
            {
                List<EdgeUse> uses = edges[edgeIndex].Value;
                List<int> regions = new List<int>(2);
                for (int i = 0; i < uses.Count; i++)
                {
                    int region = build.Triangles[uses[i].Triangle].Region;
                    if (region >= 0 && !regions.Contains(region))
                        regions.Add(region);
                }
                regions.Sort();
                if (regions.Count > 2)
                    throw new InvalidOperationException($"Renderer {build.Renderer.name}存在跨三个Region的非流形边");
                if (regions.Count != 2)
                    continue;

                int left = regions[0];
                int right = regions[1];
                EdgeUse oriented = default;
                bool found = false;
                for (int i = 0; i < uses.Count; i++)
                {
                    if (build.Triangles[uses[i].Triangle].Region != left)
                        continue;
                    oriented = uses[i];
                    found = true;
                    break;
                }
                if (!found)
                    throw new InvalidOperationException("无法确定Shared Chain方向");

                BoundaryPair pair = new BoundaryPair(left, right);
                if (!groups.TryGetValue(pair, out List<BoundarySegment> segments))
                {
                    segments = new List<BoundarySegment>();
                    groups.Add(pair, segments);
                }
                segments.Add(new BoundarySegment(oriented.StartCanonical, oriented.EndCanonical,
                    oriented.StartVertex, oriented.EndVertex));
            }

            List<BoundaryPair> pairs = new List<BoundaryPair>(groups.Keys);
            pairs.Sort();
            for (int pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
                TraceChains(build.Slot, pairs[pairIndex], groups[pairs[pairIndex]], output);
        }

        static void TraceChains(int rendererSlot, BoundaryPair pair, List<BoundarySegment> segments, List<ChainBuild> output)
        {
            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
            for (int i = 0; i < segments.Count; i++)
            {
                AddAdjacency(adjacency, segments[i].StartCanonical, i);
                AddAdjacency(adjacency, segments[i].EndCanonical, i);
            }
            foreach (KeyValuePair<int, List<int>> entry in adjacency)
                entry.Value.Sort();

            bool[] used = new bool[segments.Count];
            List<int> endpoints = new List<int>();
            foreach (KeyValuePair<int, List<int>> entry in adjacency)
            {
                if (entry.Value.Count != 2)
                    endpoints.Add(entry.Key);
            }
            endpoints.Sort();

            for (int endpointIndex = 0; endpointIndex < endpoints.Count; endpointIndex++)
            {
                List<int> connected = adjacency[endpoints[endpointIndex]];
                for (int i = 0; i < connected.Count; i++)
                {
                    if (!used[connected[i]])
                        output.Add(TraceChain(rendererSlot, pair, segments, adjacency, used, connected[i], endpoints[endpointIndex]));
                }
            }

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                if (used[segmentIndex])
                    continue;
                int start = Mathf.Min(segments[segmentIndex].StartCanonical, segments[segmentIndex].EndCanonical);
                output.Add(TraceChain(rendererSlot, pair, segments, adjacency, used, segmentIndex, start));
            }
        }

        static ChainBuild TraceChain(int rendererSlot, BoundaryPair pair, List<BoundarySegment> segments,
            Dictionary<int, List<int>> adjacency, bool[] used, int firstSegment, int firstCanonical)
        {
            ChainBuild chain = new ChainBuild
            {
                RendererSlot = rendererSlot,
                LeftRegion = pair.Left,
                RightRegion = pair.Right
            };
            int currentSegment = firstSegment;
            int currentCanonical = firstCanonical;
            while (currentSegment >= 0 && !used[currentSegment])
            {
                BoundarySegment segment = segments[currentSegment];
                used[currentSegment] = true;
                bool forward = segment.StartCanonical == currentCanonical;
                chain.Vertices.Add(forward ? segment.StartVertex : segment.EndVertex);
                currentCanonical = forward ? segment.EndCanonical : segment.StartCanonical;
                int terminalVertex = forward ? segment.EndVertex : segment.StartVertex;
                currentSegment = -1;
                if (adjacency.TryGetValue(currentCanonical, out List<int> candidates))
                {
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        if (used[candidates[i]])
                            continue;
                        currentSegment = candidates[i];
                        break;
                    }
                }
                if (currentSegment < 0)
                    chain.Vertices.Add(terminalVertex);
            }
            return chain;
        }

        static ShapeProjectionRendererRecord[] BuildRendererRecords(List<RendererBuild> renderers, List<RegionBuild> allRegions)
        {
            ShapeProjectionRendererRecord[] records = new ShapeProjectionRendererRecord[renderers.Count];
            for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                RendererBuild build = renderers[rendererIndex];
                ShapeProjectionAssetIdentity.Get(build.Renderer.sharedMesh, out string meshGuid, out long meshLocalId);
                int regionStart = -1;
                int regionCount = 0;
                int triangleStart = -1;
                int triangleCount = 0;
                for (int regionIndex = 0; regionIndex < allRegions.Count; regionIndex++)
                {
                    RegionBuild region = allRegions[regionIndex];
                    if (region.RendererSlot != rendererIndex)
                        continue;
                    if (regionStart < 0)
                    {
                        regionStart = regionIndex;
                        triangleStart = region.TriangleStart;
                    }
                    regionCount++;
                    triangleCount += region.Triangles.Count;
                }

                records[rendererIndex] = new ShapeProjectionRendererRecord
                {
                    SlotId = build.SlotId,
                    SourceMesh = build.Renderer.sharedMesh,
                    MeshGuid = meshGuid,
                    MeshLocalId = meshLocalId,
                    VertexStart = build.VertexStart,
                    VertexCount = build.MeshData.Vertices.Length,
                    TriangleStart = triangleStart,
                    TriangleCount = triangleCount,
                    RegionStart = regionStart,
                    RegionCount = regionCount,
                    SourceMaterials = build.Renderer.sharedMaterials
                };
            }
            return records;
        }

        static Hash128 ComputeProfileHash(CharacterShapeProjectionProfile profile)
        {
            StringBuilder value = new StringBuilder(2048);
            value.Append(profile.ProfileId.Value).Append('|').Append(profile.Revision).Append('|')
                .Append(profile.ColorClusterThreshold.ToString("R")).Append('|')
                .Append(profile.SmallRegionMergeThreshold.ToString("R")).Append('|')
                .Append(profile.SmallRegionTriangleLimit).Append('|')
                .Append(profile.MinimumProjectedRegionTriangles).Append('|');
            AppendCapacity(value, profile.Capacity);
            List<ShapeProjectionSubmeshRule> rules = new List<ShapeProjectionSubmeshRule>(profile.SubmeshRules);
            rules.Sort((left, right) =>
            {
                int slot = string.Compare(left.RendererSlotId, right.RendererSlotId, StringComparison.Ordinal);
                return slot != 0 ? slot : left.SubmeshIndex.CompareTo(right.SubmeshIndex);
            });
            for (int i = 0; i < rules.Count; i++)
            {
                ShapeProjectionAssetIdentity.Get(rules[i].Material, out string guid, out long localId);
                value.Append('|').Append(rules[i].RendererSlotId).Append(':').Append(rules[i].SubmeshIndex)
                    .Append(':').Append(guid).Append(':').Append(localId).Append(':').Append((int)rules[i].Mode)
                    .Append(':').Append(rules[i].AlphaThreshold.ToString("R")).Append(':')
                    .Append(rules[i].OverrideRepresentativeColor ? 1 : 0).Append(':').Append(rules[i].RepresentativeColor);
            }
            return Hash128.Compute(value.ToString());
        }

        static ShapeProjectionAssetDependency[] BuildDependencies(List<RendererBuild> renderers)
        {
            List<ShapeProjectionAssetDependency> dependencies = new List<ShapeProjectionAssetDependency>();
            HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
            for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                RendererBuild renderer = renderers[rendererIndex];
                AddDependency(dependencies, identities, ShapeProjectionDependencyKind.Mesh, renderer.Renderer.sharedMesh);
                Material[] materials = renderer.Renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    AddDependency(dependencies, identities, ShapeProjectionDependencyKind.Material, material);
                    string property = material != null && material.HasProperty("_BaseMap") ? "_BaseMap"
                        : material != null && material.HasProperty("_MainTex") ? "_MainTex"
                        : string.Empty;
                    if (!string.IsNullOrEmpty(property))
                        AddDependency(dependencies, identities, ShapeProjectionDependencyKind.Texture,
                            material.GetTexture(property));
                }
            }
            dependencies.Sort((left, right) =>
            {
                int kind = left.Kind.CompareTo(right.Kind);
                if (kind != 0)
                    return kind;
                int guid = string.Compare(left.Guid, right.Guid, StringComparison.Ordinal);
                return guid != 0 ? guid : left.LocalId.CompareTo(right.LocalId);
            });
            return dependencies.ToArray();
        }

        static void AddDependency(List<ShapeProjectionAssetDependency> dependencies, HashSet<string> identities,
            ShapeProjectionDependencyKind kind, UnityEngine.Object asset)
        {
            if (asset == null)
                return;
            ShapeProjectionAssetDependency dependency = ShapeProjectionAssetIdentity.Create(kind, asset);
            string identity = $"{dependency.Guid}:{dependency.LocalId}";
            if (identities.Add(identity))
                dependencies.Add(dependency);
        }

        static Hash128 ComputeArtifactHash(Hash128 profileHash, ShapeProjectionAssetDependency[] dependencies,
            ShapeProjectionRendererRecord[] renderers,
            ShapeProjectionRegionRecord[] regions, List<int> triangles, ShapeProjectionSharedChainRecord[] chains,
            List<int> chainVertices, List<int> regionChains)
        {
            StringBuilder value = new StringBuilder(8192);
            value.Append(profileHash).Append('|');
            for (int i = 0; i < dependencies.Length; i++)
            {
                ShapeProjectionAssetDependency dependency = dependencies[i];
                value.Append((int)dependency.Kind).Append(':').Append(dependency.Guid).Append(':')
                    .Append(dependency.LocalId).Append(':').Append(dependency.DependencyHash).Append('|');
            }
            for (int i = 0; i < renderers.Length; i++)
            {
                ShapeProjectionRendererRecord renderer = renderers[i];
                value.Append(renderer.SlotId).Append(':').Append(renderer.MeshGuid).Append(':').Append(renderer.MeshLocalId)
                    .Append(':').Append(renderer.VertexStart).Append(':').Append(renderer.VertexCount).Append('|');
            }
            for (int i = 0; i < regions.Length; i++)
            {
                ShapeProjectionRegionRecord region = regions[i];
                value.Append(region.RegionId.Value).Append(':').Append(region.RendererSlot).Append(':')
                    .Append(region.RepresentativeColor).Append(':').Append(region.TriangleCount).Append('|');
            }
            for (int i = 0; i < triangles.Count; i++)
                value.Append(triangles[i]).Append(',');
            for (int i = 0; i < chains.Length; i++)
            {
                ShapeProjectionSharedChainRecord chain = chains[i];
                value.Append('|').Append(chain.LeftRegion).Append(':').Append(chain.RightRegion).Append(':')
                    .Append(chain.VertexStart).Append(':').Append(chain.VertexCount);
            }
            for (int i = 0; i < chainVertices.Count; i++)
                value.Append(',').Append(chainVertices[i]);
            for (int i = 0; i < regionChains.Count; i++)
                value.Append(';').Append(regionChains[i]);
            return Hash128.Compute(value.ToString());
        }

        static void ValidateCapacity(ShapeProjectionCapacity capacity, List<RendererBuild> renderers,
            List<RegionBuild> regions, List<ChainBuild> chains, int vertexCount)
        {
            int triangleCount = 0;
            for (int i = 0; i < renderers.Count; i++)
                triangleCount += renderers[i].Triangles.Count;
            if (renderers.Count > capacity.MaxRenderers || vertexCount > capacity.MaxVertices
                                                        || triangleCount > capacity.MaxTriangles
                                                        || regions.Count > capacity.MaxRegions
                                                        || regions.Count > capacity.MaxIndirectInstances
                                                        || chains.Count > capacity.MaxSharedChains)
                throw new InvalidOperationException($"Bake结果超过Profile固定容量：Renderer {renderers.Count}/{capacity.MaxRenderers}，Vertex {vertexCount}/{capacity.MaxVertices}，Triangle {triangleCount}/{capacity.MaxTriangles}，Region {regions.Count}/{capacity.MaxRegions}，Indirect {regions.Count}/{capacity.MaxIndirectInstances}，Shared Chain {chains.Count}/{capacity.MaxSharedChains}");
        }

        static void AppendCapacity(StringBuilder value, ShapeProjectionCapacity capacity)
        {
            value.Append(capacity.MaxRenderers).Append(':').Append(capacity.MaxVertices).Append(':')
                .Append(capacity.MaxTriangles).Append(':').Append(capacity.MaxRegions).Append(':')
                .Append(capacity.MaxSharedChains).Append(':').Append(capacity.AtlasWidth).Append(':')
                .Append(capacity.AtlasHeight).Append(':').Append(capacity.MaxContourPoints).Append(':')
                .Append(capacity.MaxLoops).Append(':').Append(capacity.MaxIndirectInstances).Append(':')
                .Append(capacity.ReadbackSlots);
        }

        static List<KeyValuePair<EdgeKey, List<EdgeUse>>> SortedEdges(Dictionary<EdgeKey, List<EdgeUse>> edges)
        {
            List<KeyValuePair<EdgeKey, List<EdgeUse>>> sorted = new List<KeyValuePair<EdgeKey, List<EdgeUse>>>(edges);
            sorted.Sort((left, right) => left.Key.CompareTo(right.Key));
            return sorted;
        }

        static Dictionary<int, List<int>> BuildGroups(int[] parent)
        {
            Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < parent.Length; i++)
            {
                int root = Find(parent, i);
                if (!groups.TryGetValue(root, out List<int> group))
                {
                    group = new List<int>();
                    groups.Add(root, group);
                }
                group.Add(i);
            }
            return groups;
        }

        static Color AverageColor(List<TriangleBuild> triangles, List<int> indices)
        {
            Color sum = Color.clear;
            for (int i = 0; i < indices.Count; i++)
                sum += triangles[indices[i]].Color;
            return sum / Mathf.Max(1, indices.Count);
        }

        static float ColorDistance255(Color left, Color right)
        {
            float r = (left.r - right.r) * 255f;
            float g = (left.g - right.g) * 255f;
            float b = (left.b - right.b) * 255f;
            return Mathf.Sqrt(r * r + g * g + b * b);
        }

        static int MinOrderTriangle(List<TriangleBuild> triangles, List<int> indices)
        {
            int best = indices[0];
            for (int i = 1; i < indices.Count; i++)
            {
                if (triangles[indices[i]].Order < triangles[best].Order)
                    best = indices[i];
            }
            return best;
        }

        static void AddEdge(Dictionary<EdgeKey, List<EdgeUse>> edges, int triangle, int startCanonical,
            int endCanonical, int startVertex, int endVertex)
        {
            EdgeKey key = new EdgeKey(startCanonical, endCanonical);
            if (!edges.TryGetValue(key, out List<EdgeUse> uses))
            {
                uses = new List<EdgeUse>(2);
                edges.Add(key, uses);
            }
            uses.Add(new EdgeUse(triangle, startCanonical, endCanonical, startVertex, endVertex));
        }

        static void AddAdjacency(Dictionary<int, List<int>> adjacency, int vertex, int segment)
        {
            if (!adjacency.TryGetValue(vertex, out List<int> segments))
            {
                segments = new List<int>(2);
                adjacency.Add(vertex, segments);
            }
            segments.Add(segment);
        }

        static int Find(int[] parent, int value)
        {
            int root = value;
            while (parent[root] != root)
                root = parent[root];
            while (parent[value] != value)
            {
                int next = parent[value];
                parent[value] = root;
                value = next;
            }
            return root;
        }

        static void Union(int[] parent, int left, int right)
        {
            int leftRoot = Find(parent, left);
            int rightRoot = Find(parent, right);
            if (leftRoot == rightRoot)
                return;
            if (leftRoot < rightRoot)
                parent[rightRoot] = leftRoot;
            else
                parent[leftRoot] = rightRoot;
        }
    }
}
