using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionSharedChainNative
    {
        public int LeftRegion;
        public int RightRegion;
        public int VertexStart;
        public int VertexCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionBoundaryEdge
    {
        public int2 Start;
        public int2 End;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionLoopNative
    {
        public int RegionIndex;
        public int PointStart;
        public int PointCount;
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ShapeProjectionContourJob : IJob
    {
        [ReadOnly] public NativeArray<byte> Mask;
        [ReadOnly] public NativeArray<ShapeProjectionRegionGpu> RegionGpu;
        [ReadOnly] public NativeArray<ShapeProjectionRegionNative> Regions;
        [ReadOnly] public NativeArray<int> RegionSharedChainIndices;
        [ReadOnly] public NativeArray<ShapeProjectionSharedChainNative> SharedChains;
        [ReadOnly] public NativeArray<int> SharedChainVertexIndices;
        [ReadOnly] public NativeArray<ShapeProjectionProjectedVertex> ProjectedVertices;
        public int RegionCount;
        public int AtlasWidth;
        public int AtlasHeight;
        public float MaximumSimplifyEpsilon;
        public float MinimumSecondaryLoopArea;
        public float MinimumSharedEdgeLength;

        public NativeArray<ShapeProjectionBoundaryEdge> Edges;
        public NativeArray<byte> EdgeUsed;
        public NativeParallelMultiHashMap<int, int> EdgeStarts;
        public NativeArray<float2> RawPoints;
        public NativeArray<byte> RawAnchors;
        public NativeArray<ShapeProjectionLoopNative> RawLoops;
        public NativeArray<float2> OutputPoints;
        public NativeArray<uint> OutputAnchorFlags;
        public NativeArray<ShapeProjectionLoopNative> OutputLoops;
        public NativeArray<ShapeProjectionCompositeRegionGpu> CompositeRegions;
        public NativeArray<float2> RdpInput;
        public NativeArray<byte> RdpAnchors;
        public NativeArray<byte> RdpKeep;
        public NativeArray<int2> RdpStack;
        public NativeArray<uint> IndirectArgs;
        public NativeReference<int> ErrorCode;
        public NativeReference<int> RawPointCount;
        public NativeReference<int> RawLoopCount;
        public NativeReference<int> OutputPointCount;
        public NativeReference<int> OutputLoopCount;
        public NativeReference<int> CompositeRegionCount;

        public void Execute()
        {
            ErrorCode.Value = 0;
            RawPointCount.Value = 0;
            RawLoopCount.Value = 0;
            OutputPointCount.Value = 0;
            OutputLoopCount.Value = 0;
            CompositeRegionCount.Value = 0;
            IndirectArgs[0] = 6;
            IndirectArgs[1] = 0;
            IndirectArgs[2] = 0;
            IndirectArgs[3] = 0;

            for (int regionIndex = 0; regionIndex < RegionCount; regionIndex++)
            {
                ShapeProjectionRegionGpu gpu = RegionGpu[regionIndex];
                if (gpu.Meta.z < 0.5f)
                    continue;

                int rawLoopStart = RawLoopCount.Value;
                if (!ExtractRegionLoops(regionIndex, gpu))
                    return;
                int rawLoopEnd = RawLoopCount.Value;
                if (rawLoopEnd == rawLoopStart)
                    continue;

                SnapSharedChains(regionIndex, rawLoopStart, rawLoopEnd);
                if (ErrorCode.Value != 0)
                    return;

                int primaryLoop = rawLoopStart;
                float primaryArea = 0f;
                for (int loopIndex = rawLoopStart; loopIndex < rawLoopEnd; loopIndex++)
                {
                    ShapeProjectionLoopNative loop = RawLoops[loopIndex];
                    float area = PolygonArea(RawPoints, loop.PointStart, loop.PointCount);
                    if (area > primaryArea)
                    {
                        primaryArea = area;
                        primaryLoop = loopIndex;
                    }
                }

                int outputLoopStart = OutputLoopCount.Value;
                for (int loopIndex = rawLoopStart; loopIndex < rawLoopEnd; loopIndex++)
                {
                    if (!SimplifyLoop(RawLoops[loopIndex], loopIndex == primaryLoop))
                        return;
                }

                int outputLoopCount = OutputLoopCount.Value - outputLoopStart;
                if (outputLoopCount <= 0)
                    continue;
                int compositeIndex = CompositeRegionCount.Value;
                if (compositeIndex >= CompositeRegions.Length)
                {
                    ErrorCode.Value = 7;
                    return;
                }

                CompositeRegions[compositeIndex] = new ShapeProjectionCompositeRegionGpu
                {
                    ScreenRect = gpu.ScreenRect,
                    AtlasRect = gpu.AtlasRect,
                    Color = gpu.Color,
                    LoopRange = new Vector4(outputLoopStart, outputLoopCount, 0f, 0f)
                };
                CompositeRegionCount.Value = compositeIndex + 1;
            }

            IndirectArgs[1] = (uint)CompositeRegionCount.Value;
        }

        bool ExtractRegionLoops(int regionIndex, ShapeProjectionRegionGpu gpu)
        {
            EdgeStarts.Clear();
            int edgeCount = 0;
            int atlasX = (int)gpu.AtlasRect.x;
            int atlasY = (int)gpu.AtlasRect.y;
            int width = (int)gpu.AtlasRect.z;
            int height = (int)gpu.AtlasRect.w;
            for (int localY = 0; localY < height; localY++)
            {
                int y = atlasY + localY;
                for (int localX = 0; localX < width; localX++)
                {
                    int x = atlasX + localX;
                    if (!IsFilled(x, y, atlasX, atlasY, width, height))
                        continue;
                    if (!IsFilled(x, y - 1, atlasX, atlasY, width, height)
                        && !AddEdge(ref edgeCount, new int2(x, y), new int2(x + 1, y)))
                        return false;
                    if (!IsFilled(x + 1, y, atlasX, atlasY, width, height)
                        && !AddEdge(ref edgeCount, new int2(x + 1, y), new int2(x + 1, y + 1)))
                        return false;
                    if (!IsFilled(x, y + 1, atlasX, atlasY, width, height)
                        && !AddEdge(ref edgeCount, new int2(x + 1, y + 1), new int2(x, y + 1)))
                        return false;
                    if (!IsFilled(x - 1, y, atlasX, atlasY, width, height)
                        && !AddEdge(ref edgeCount, new int2(x, y + 1), new int2(x, y)))
                        return false;
                }
            }

            for (int edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
            {
                if (EdgeUsed[edgeIndex] != 0)
                    continue;
                int pointStart = RawPointCount.Value;
                ShapeProjectionBoundaryEdge first = Edges[edgeIndex];
                int2 start = first.Start;
                int2 current = start;
                int currentEdge = edgeIndex;
                int safety = 0;
                while (currentEdge >= 0 && EdgeUsed[currentEdge] == 0)
                {
                    if (RawPointCount.Value >= RawPoints.Length)
                    {
                        ErrorCode.Value = 2;
                        return false;
                    }
                    ShapeProjectionBoundaryEdge edge = Edges[currentEdge];
                    EdgeUsed[currentEdge] = 1;
                    RawPoints[RawPointCount.Value] = AtlasPointToScreen(edge.Start, gpu);
                    RawAnchors[RawPointCount.Value] = 0;
                    RawPointCount.Value++;
                    current = edge.End;
                    if (math.all(current == start))
                        break;
                    currentEdge = FindNextEdge(current, edge.End - edge.Start);
                    if (currentEdge < 0)
                    {
                        ErrorCode.Value = 4;
                        return false;
                    }
                    safety++;
                    if (safety > edgeCount)
                    {
                        ErrorCode.Value = 4;
                        return false;
                    }
                }

                int pointCount = RawPointCount.Value - pointStart;
                if (pointCount < 3)
                {
                    RawPointCount.Value = pointStart;
                    continue;
                }
                if (RawLoopCount.Value >= RawLoops.Length)
                {
                    ErrorCode.Value = 3;
                    return false;
                }
                RawLoops[RawLoopCount.Value] = new ShapeProjectionLoopNative
                {
                    RegionIndex = regionIndex,
                    PointStart = pointStart,
                    PointCount = pointCount
                };
                RawLoopCount.Value++;
            }
            return true;
        }

        bool AddEdge(ref int edgeCount, int2 start, int2 end)
        {
            if (edgeCount >= Edges.Length || edgeCount >= EdgeStarts.Capacity)
            {
                ErrorCode.Value = 1;
                return false;
            }
            Edges[edgeCount] = new ShapeProjectionBoundaryEdge { Start = start, End = end };
            EdgeUsed[edgeCount] = 0;
            EdgeStarts.Add(EncodePoint(start), edgeCount);
            edgeCount++;
            return true;
        }

        int FindNextEdge(int2 point, int2 incoming)
        {
            NativeParallelMultiHashMapIterator<int> iterator;
            if (!EdgeStarts.TryGetFirstValue(EncodePoint(point), out int candidate, out iterator))
                return -1;
            int best = -1;
            int bestRank = int.MaxValue;
            SelectNextEdge(candidate, incoming, ref best, ref bestRank);
            while (EdgeStarts.TryGetNextValue(out candidate, ref iterator))
                SelectNextEdge(candidate, incoming, ref best, ref bestRank);
            return best;
        }

        void SelectNextEdge(int candidate, int2 incoming, ref int best, ref int bestRank)
        {
            if (EdgeUsed[candidate] != 0)
                return;
            int2 outgoing = Edges[candidate].End - Edges[candidate].Start;
            int cross = incoming.x * outgoing.y - incoming.y * outgoing.x;
            int dot = incoming.x * outgoing.x + incoming.y * outgoing.y;
            int rank = cross > 0 ? 0 : dot > 0 ? 1 : cross < 0 ? 2 : 3;
            if (rank < bestRank || rank == bestRank && (best < 0 || candidate < best))
            {
                best = candidate;
                bestRank = rank;
            }
        }

        void SnapSharedChains(int regionIndex, int rawLoopStart, int rawLoopEnd)
        {
            ShapeProjectionRegionNative region = Regions[regionIndex];
            float snapDistanceSquared = (MaximumSimplifyEpsilon + 2f) * (MaximumSimplifyEpsilon + 2f);
            for (int relation = 0; relation < region.SharedChainCount; relation++)
            {
                int relationIndex = region.SharedChainStart + relation;
                if (relationIndex < 0 || relationIndex >= RegionSharedChainIndices.Length)
                {
                    ErrorCode.Value = 8;
                    return;
                }
                int chainIndex = RegionSharedChainIndices[relationIndex];
                ShapeProjectionSharedChainNative chain = SharedChains[chainIndex];
                if (chain.VertexCount < 2 || chain.VertexCount > RdpInput.Length)
                    continue;

                bool valid = true;
                for (int i = 0; i < chain.VertexCount; i++)
                {
                    ShapeProjectionProjectedVertex vertex = ProjectedVertices[SharedChainVertexIndices[chain.VertexStart + i]];
                    if (vertex.ClipW <= 0.00001f)
                    {
                        valid = false;
                        break;
                    }
                    RdpInput[i] = new float2(vertex.Screen.x, vertex.Screen.y);
                    RdpAnchors[i] = 0;
                    RdpKeep[i] = 0;
                }
                if (!valid || math.distance(RdpInput[0], RdpInput[chain.VertexCount - 1]) < MinimumSharedEdgeLength)
                    continue;

                RdpKeep[0] = 1;
                RdpKeep[chain.VertexCount - 1] = 1;
                if (!RunRdp(0, chain.VertexCount - 1, chain.VertexCount, MaximumSimplifyEpsilon))
                    return;
                SnapRawPointsToChain(rawLoopStart, rawLoopEnd, chain.VertexCount, snapDistanceSquared);
                for (int pointIndex = 0; pointIndex < chain.VertexCount; pointIndex++)
                {
                    if (RdpKeep[pointIndex] == 0)
                        continue;
                    float2 point = RdpInput[pointIndex];
                    int bestRawPoint = -1;
                    float bestDistance = snapDistanceSquared;
                    for (int loopIndex = rawLoopStart; loopIndex < rawLoopEnd; loopIndex++)
                    {
                        ShapeProjectionLoopNative loop = RawLoops[loopIndex];
                        for (int rawOffset = 0; rawOffset < loop.PointCount; rawOffset++)
                        {
                            int rawIndex = loop.PointStart + rawOffset;
                            float distance = math.distancesq(RawPoints[rawIndex], point);
                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestRawPoint = rawIndex;
                            }
                        }
                    }
                    if (bestRawPoint >= 0)
                    {
                        RawPoints[bestRawPoint] = point;
                        RawAnchors[bestRawPoint] = 2;
                    }
                }
            }
        }

        void SnapRawPointsToChain(int rawLoopStart, int rawLoopEnd, int chainPointCount, float snapDistanceSquared)
        {
            for (int loopIndex = rawLoopStart; loopIndex < rawLoopEnd; loopIndex++)
            {
                ShapeProjectionLoopNative loop = RawLoops[loopIndex];
                for (int rawOffset = 0; rawOffset < loop.PointCount; rawOffset++)
                {
                    int rawIndex = loop.PointStart + rawOffset;
                    float2 original = RawPoints[rawIndex];
                    float2 bestPoint = original;
                    float bestDistance = snapDistanceSquared;
                    int previousKept = -1;
                    for (int pointIndex = 0; pointIndex < chainPointCount; pointIndex++)
                    {
                        if (RdpKeep[pointIndex] == 0)
                            continue;
                        if (previousKept >= 0)
                        {
                            float2 a = RdpInput[previousKept];
                            float2 segment = RdpInput[pointIndex] - a;
                            float denominator = math.lengthsq(segment);
                            if (denominator > 0.000001f)
                            {
                                float t = math.saturate(math.dot(original - a, segment) / denominator);
                                float2 projected = a + segment * t;
                                float distance = math.distancesq(original, projected);
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestPoint = projected;
                                }
                            }
                        }
                        previousKept = pointIndex;
                    }
                    RawPoints[rawIndex] = bestPoint;
                }
            }
        }

        bool SimplifyLoop(ShapeProjectionLoopNative loop, bool preservePrimary)
        {
            if (loop.PointCount + 1 > RdpInput.Length)
            {
                ErrorCode.Value = 5;
                return false;
            }
            float rawArea = PolygonArea(RawPoints, loop.PointStart, loop.PointCount);
            if (!preservePrimary && rawArea < MinimumSecondaryLoopArea)
                return true;
            float perimeter = PolygonPerimeter(RawPoints, loop.PointStart, loop.PointCount);
            float effectiveEpsilon = math.min(MaximumSimplifyEpsilon, math.max(0.25f, rawArea / math.max(1f, perimeter)));
            int split = 0;
            for (int i = 0; i < loop.PointCount; i++)
            {
                if (RawAnchors[loop.PointStart + i] != 0)
                {
                    split = i;
                    break;
                }
            }

            int inputCount = loop.PointCount + 1;
            int anchorCount = 0;
            for (int i = 0; i < inputCount; i++)
            {
                int rawOffset = (split + i) % loop.PointCount;
                int rawIndex = loop.PointStart + rawOffset;
                RdpInput[i] = RawPoints[rawIndex];
                RdpAnchors[i] = RawAnchors[rawIndex];
                RdpKeep[i] = 0;
                if (i < loop.PointCount && RdpAnchors[i] != 0)
                    anchorCount++;
            }
            if (RdpAnchors[0] == 0)
                RdpAnchors[0] = 1;
            if (RdpAnchors[inputCount - 1] == 0)
                RdpAnchors[inputCount - 1] = 1;
            if (anchorCount < 2)
            {
                int farthest = 1;
                float farthestDistance = 0f;
                for (int i = 1; i < loop.PointCount; i++)
                {
                    float distance = math.distancesq(RdpInput[0], RdpInput[i]);
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthest = i;
                    }
                }
                RdpAnchors[farthest] = 1;
            }

            int previousAnchor = 0;
            for (int i = 1; i < inputCount; i++)
            {
                if (RdpAnchors[i] == 0)
                    continue;
                RdpKeep[previousAnchor] = 1;
                RdpKeep[i] = 1;
                if (!RunRdp(previousAnchor, i, inputCount, effectiveEpsilon))
                    return false;
                previousAnchor = i;
            }

            int outputStart = OutputPointCount.Value;
            for (int i = 0; i < loop.PointCount; i++)
            {
                if (RdpKeep[i] == 0)
                    continue;
                if (OutputPointCount.Value >= OutputPoints.Length)
                {
                    ErrorCode.Value = 5;
                    return false;
                }
                int outputIndex = OutputPointCount.Value++;
                OutputPoints[outputIndex] = RdpInput[i];
                OutputAnchorFlags[outputIndex] = RdpAnchors[i] == 2 ? 1u : 0u;
            }
            int outputCount = OutputPointCount.Value - outputStart;
            if (outputCount < 3 || PolygonArea(OutputPoints, outputStart, outputCount) < rawArea * 0.25f)
            {
                OutputPointCount.Value = outputStart;
                for (int i = 0; i < loop.PointCount; i++)
                {
                    if (OutputPointCount.Value >= OutputPoints.Length)
                    {
                        ErrorCode.Value = 5;
                        return false;
                    }
                    int rawIndex = loop.PointStart + i;
                    int outputIndex = OutputPointCount.Value++;
                    OutputPoints[outputIndex] = RawPoints[rawIndex];
                    OutputAnchorFlags[outputIndex] = RawAnchors[rawIndex] == 2 ? 1u : 0u;
                }
                outputCount = loop.PointCount;
            }
            if (OutputLoopCount.Value >= OutputLoops.Length)
            {
                ErrorCode.Value = 6;
                return false;
            }
            OutputLoops[OutputLoopCount.Value] = new ShapeProjectionLoopNative
            {
                RegionIndex = loop.RegionIndex,
                PointStart = outputStart,
                PointCount = outputCount
            };
            OutputLoopCount.Value++;
            return true;
        }

        bool RunRdp(int start, int end, int inputCount, float epsilon)
        {
            int stackCount = 0;
            if (stackCount >= RdpStack.Length)
            {
                ErrorCode.Value = 6;
                return false;
            }
            RdpStack[stackCount++] = new int2(start, end);
            float epsilonSquared = epsilon * epsilon;
            while (stackCount > 0)
            {
                int2 range = RdpStack[--stackCount];
                float2 a = RdpInput[range.x];
                float2 b = RdpInput[range.y];
                int farthest = -1;
                float maximum = epsilonSquared;
                for (int i = range.x + 1; i < range.y; i++)
                {
                    if (RdpAnchors[i] != 0)
                    {
                        farthest = i;
                        maximum = float.MaxValue;
                        break;
                    }
                    float distance = DistanceToSegmentSquared(RdpInput[i], a, b);
                    if (distance > maximum)
                    {
                        maximum = distance;
                        farthest = i;
                    }
                }
                if (farthest < 0)
                    continue;
                RdpKeep[farthest] = 1;
                if (stackCount + 2 > RdpStack.Length || farthest >= inputCount)
                {
                    ErrorCode.Value = 6;
                    return false;
                }
                RdpStack[stackCount++] = new int2(range.x, farthest);
                RdpStack[stackCount++] = new int2(farthest, range.y);
            }
            return true;
        }

        bool IsFilled(int x, int y, int atlasX, int atlasY, int width, int height)
        {
            return x >= atlasX && y >= atlasY && x < atlasX + width && y < atlasY + height
                   && x < AtlasWidth && y < AtlasHeight && Mask[x + y * AtlasWidth] > 127;
        }

        int EncodePoint(int2 point)
        {
            return point.x + point.y * (AtlasWidth + 1);
        }

        static float2 AtlasPointToScreen(int2 point, ShapeProjectionRegionGpu gpu)
        {
            return new float2(gpu.ScreenRect.x + point.x - gpu.AtlasRect.x,
                gpu.ScreenRect.y + point.y - gpu.AtlasRect.y);
        }

        static float DistanceToSegmentSquared(float2 point, float2 a, float2 b)
        {
            float2 line = b - a;
            float denominator = math.lengthsq(line);
            if (denominator <= 0.000001f)
                return math.distancesq(point, a);
            float t = math.saturate(math.dot(point - a, line) / denominator);
            return math.distancesq(point, a + line * t);
        }

        static float PolygonArea(NativeArray<float2> points, int start, int count)
        {
            float area = 0f;
            for (int i = 0; i < count; i++)
            {
                float2 a = points[start + i];
                float2 b = points[start + (i + 1) % count];
                area += a.x * b.y - b.x * a.y;
            }
            return math.abs(area) * 0.5f;
        }

        static float PolygonPerimeter(NativeArray<float2> points, int start, int count)
        {
            float perimeter = 0f;
            for (int i = 0; i < count; i++)
                perimeter += math.distance(points[start + i], points[start + (i + 1) % count]);
            return perimeter;
        }
    }
}
