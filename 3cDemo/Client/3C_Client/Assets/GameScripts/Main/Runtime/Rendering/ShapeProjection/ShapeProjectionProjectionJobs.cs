using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ThirdPersonRendering.ShapeProjection
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionRegionNative
    {
        public int TriangleStart;
        public int TriangleCount;
        public int RendererSlot;
        public int SharedChainStart;
        public int SharedChainCount;
        public int Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShapeProjectionRegionBounds
    {
        public float2 Min;
        public float2 Max;
        public int Valid;
        public int ValidTriangleCount;
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ShapeProjectionProjectVerticesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> LocalVertices;
        [ReadOnly] public NativeArray<int> VertexRendererSlots;
        [ReadOnly] public NativeArray<float4x4> LocalToWorld;
        [ReadOnly] public float4x4 ScreenViewProjection;
        [ReadOnly] public float4x4 DepthViewProjection;
        [ReadOnly] public float2 DepthScaleBias;
        [ReadOnly] public float2 ViewportSize;
        [WriteOnly] public NativeArray<ShapeProjectionProjectedVertex> ProjectedVertices;

        public void Execute(int index)
        {
            int rendererSlot = VertexRendererSlots[index];
            float4 world = math.mul(LocalToWorld[rendererSlot], new float4(LocalVertices[index], 1f));
            float4 screenClip = math.mul(ScreenViewProjection, world);
            float4 depthClip = math.mul(DepthViewProjection, world);
            if (screenClip.w <= 0.00001f || depthClip.w <= 0.00001f)
            {
                ProjectedVertices[index] = new ShapeProjectionProjectedVertex
                {
                    Screen = new UnityEngine.Vector2(float.NaN, float.NaN),
                    Depth = -1f,
                    ClipW = math.min(screenClip.w, depthClip.w)
                };
                return;
            }

            float invW = math.rcp(screenClip.w);
            float2 ndc = screenClip.xy * invW;
            float2 screen = (ndc * 0.5f + 0.5f) * ViewportSize;
            float depth = depthClip.z * math.rcp(depthClip.w);
            ProjectedVertices[index] = new ShapeProjectionProjectedVertex
            {
                Screen = new UnityEngine.Vector2(screen.x, screen.y),
                Depth = depth * DepthScaleBias.x + DepthScaleBias.y,
                ClipW = math.min(screenClip.w, depthClip.w)
            };
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ShapeProjectionRegionBoundsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ShapeProjectionRegionNative> Regions;
        [ReadOnly] public NativeArray<int> TriangleIndices;
        [ReadOnly] public NativeArray<ShapeProjectionProjectedVertex> ProjectedVertices;
        [ReadOnly] public float2 ViewportSize;
        [WriteOnly] public NativeArray<ShapeProjectionRegionBounds> Bounds;

        public void Execute(int index)
        {
            ShapeProjectionRegionNative region = Regions[index];
            float2 minimum = new float2(float.MaxValue, float.MaxValue);
            float2 maximum = new float2(float.MinValue, float.MinValue);
            int validTriangles = 0;
            for (int triangle = 0; triangle < region.TriangleCount; triangle++)
            {
                int triangleBase = (region.TriangleStart + triangle) * 3;
                ShapeProjectionProjectedVertex a = ProjectedVertices[TriangleIndices[triangleBase]];
                ShapeProjectionProjectedVertex b = ProjectedVertices[TriangleIndices[triangleBase + 1]];
                ShapeProjectionProjectedVertex c = ProjectedVertices[TriangleIndices[triangleBase + 2]];
                if (a.ClipW <= 0.00001f || b.ClipW <= 0.00001f || c.ClipW <= 0.00001f)
                    continue;

                float2 pa = new float2(a.Screen.x, a.Screen.y);
                float2 pb = new float2(b.Screen.x, b.Screen.y);
                float2 pc = new float2(c.Screen.x, c.Screen.y);
                float area = math.abs(Cross(pb - pa, pc - pa));
                if (area <= 0.0001f)
                    continue;

                minimum = math.min(minimum, math.min(pa, math.min(pb, pc)));
                maximum = math.max(maximum, math.max(pa, math.max(pb, pc)));
                validTriangles++;
            }

            minimum = math.max(minimum, float2.zero);
            maximum = math.min(maximum, ViewportSize);
            bool valid = validTriangles > 0 && maximum.x > minimum.x && maximum.y > minimum.y;
            Bounds[index] = new ShapeProjectionRegionBounds
            {
                Min = minimum,
                Max = maximum,
                Valid = valid ? 1 : 0,
                ValidTriangleCount = validTriangles
            };
        }

        static float Cross(float2 left, float2 right)
        {
            return left.x * right.y - left.y * right.x;
        }
    }
}
