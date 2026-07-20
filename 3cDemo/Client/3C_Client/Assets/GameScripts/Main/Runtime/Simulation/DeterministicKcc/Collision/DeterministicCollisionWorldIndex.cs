using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    internal sealed class DeterministicCollisionWorldIndex
    {
        struct Entry
        {
            public Entry(int primitiveId, DeterministicCollisionBounds bounds)
            {
                PrimitiveId = primitiveId;
                Bounds = bounds;
                Center = new FixedVector3(
                    (bounds.Minimum.X + bounds.Maximum.X) / FixedScalar.FromInt64(2),
                    (bounds.Minimum.Y + bounds.Maximum.Y) / FixedScalar.FromInt64(2),
                    (bounds.Minimum.Z + bounds.Maximum.Z) / FixedScalar.FromInt64(2));
            }

            public int PrimitiveId;
            public DeterministicCollisionBounds Bounds;
            public FixedVector3 Center;
        }

        readonly struct Node
        {
            public Node(DeterministicCollisionBounds bounds, int left, int right, int start, int count)
            {
                Bounds = bounds;
                Left = left;
                Right = right;
                Start = start;
                Count = count;
            }

            public DeterministicCollisionBounds Bounds { get; }
            public int Left { get; }
            public int Right { get; }
            public int Start { get; }
            public int Count { get; }
            public bool IsLeaf => Count > 0;
        }

        readonly Entry[] m_Entries;
        readonly Node[] m_Nodes;
        readonly int[] m_TraversalStack;
        readonly int m_Root;

        public DeterministicCollisionWorldIndex(DeterministicCollisionWorldArtifact world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            m_Entries = new Entry[world.Primitives.Count];
            for (int i = 0; i < m_Entries.Length; i++)
                m_Entries[i] = new Entry(world.Primitives[i].Id, world.Primitives[i].Bounds);
            if (m_Entries.Length == 0)
            {
                m_Nodes = Array.Empty<Node>();
                m_TraversalStack = Array.Empty<int>();
                m_Root = -1;
                return;
            }
            var nodes = new List<Node>(m_Entries.Length * 2);
            m_Root = Build(nodes, 0, m_Entries.Length);
            m_Nodes = nodes.ToArray();
            m_TraversalStack = new int[m_Nodes.Length];
        }

        public int Query(DeterministicCollisionBounds bounds, int[] primitiveIds)
        {
            if (primitiveIds == null || primitiveIds.Length == 0)
                throw new ArgumentNullException(nameof(primitiveIds));
            if (m_Root < 0)
                return 0;
            int primitiveCount = 0;
            int stackCount = 1;
            m_TraversalStack[0] = m_Root;
            while (stackCount > 0)
            {
                Node node = m_Nodes[m_TraversalStack[--stackCount]];
                if (!Overlaps(bounds, node.Bounds))
                    continue;
                if (node.IsLeaf)
                {
                    for (int i = 0; i < node.Count; i++)
                    {
                        Entry entry = m_Entries[node.Start + i];
                        if (!Overlaps(bounds, entry.Bounds))
                            continue;
                        if (primitiveCount >= primitiveIds.Length)
                        {
                            throw new DeterministicKccQueryException(
                                DeterministicKccQueryStage.CandidateGather,
                                "Candidate buffer capacity was exceeded.",
                                entry.PrimitiveId,
                                primitiveCount + 1,
                                primitiveIds.Length);
                        }
                        primitiveIds[primitiveCount++] = entry.PrimitiveId;
                    }
                    continue;
                }
                m_TraversalStack[stackCount++] = node.Right;
                m_TraversalStack[stackCount++] = node.Left;
            }
            Array.Sort(primitiveIds, 0, primitiveCount);
            return primitiveCount;
        }

        int Build(List<Node> nodes, int start, int count)
        {
            DeterministicCollisionBounds bounds = Union(start, count);
            int nodeIndex = nodes.Count;
            nodes.Add(default);
            if (count <= 4)
            {
                Array.Sort(m_Entries, start, count, PrimitiveIdComparer.Instance);
                nodes[nodeIndex] = new Node(bounds, -1, -1, start, count);
                return nodeIndex;
            }
            int axis = LongestAxis(bounds);
            Array.Sort(m_Entries, start, count, new CenterComparer(axis));
            int leftCount = count / 2;
            int left = Build(nodes, start, leftCount);
            int right = Build(nodes, start + leftCount, count - leftCount);
            nodes[nodeIndex] = new Node(bounds, left, right, 0, 0);
            return nodeIndex;
        }

        DeterministicCollisionBounds Union(int start, int count)
        {
            FixedVector3 minimum = m_Entries[start].Bounds.Minimum;
            FixedVector3 maximum = m_Entries[start].Bounds.Maximum;
            for (int i = 1; i < count; i++)
            {
                DeterministicCollisionBounds bounds = m_Entries[start + i].Bounds;
                minimum = new FixedVector3(
                    FixedScalar.Min(minimum.X, bounds.Minimum.X),
                    FixedScalar.Min(minimum.Y, bounds.Minimum.Y),
                    FixedScalar.Min(minimum.Z, bounds.Minimum.Z));
                maximum = new FixedVector3(
                    FixedScalar.Max(maximum.X, bounds.Maximum.X),
                    FixedScalar.Max(maximum.Y, bounds.Maximum.Y),
                    FixedScalar.Max(maximum.Z, bounds.Maximum.Z));
            }
            return new DeterministicCollisionBounds(minimum, maximum);
        }

        static int LongestAxis(DeterministicCollisionBounds bounds)
        {
            FixedVector3 size = bounds.Maximum - bounds.Minimum;
            if (size.Y > size.X && size.Y >= size.Z)
                return 1;
            return size.Z > size.X && size.Z > size.Y ? 2 : 0;
        }

        static FixedScalar Coordinate(FixedVector3 value, int axis)
        {
            return axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;
        }

        static bool Overlaps(DeterministicCollisionBounds left, DeterministicCollisionBounds right)
        {
            return left.Minimum.X <= right.Maximum.X && left.Maximum.X >= right.Minimum.X &&
                   left.Minimum.Y <= right.Maximum.Y && left.Maximum.Y >= right.Minimum.Y &&
                   left.Minimum.Z <= right.Maximum.Z && left.Maximum.Z >= right.Minimum.Z;
        }

        sealed class PrimitiveIdComparer : IComparer<Entry>
        {
            public static PrimitiveIdComparer Instance { get; } = new PrimitiveIdComparer();
            public int Compare(Entry left, Entry right) => left.PrimitiveId.CompareTo(right.PrimitiveId);
        }

        sealed class CenterComparer : IComparer<Entry>
        {
            readonly int m_Axis;

            public CenterComparer(int axis)
            {
                m_Axis = axis;
            }

            public int Compare(Entry left, Entry right)
            {
                int coordinate = Coordinate(left.Center, m_Axis).CompareTo(Coordinate(right.Center, m_Axis));
                return coordinate != 0 ? coordinate : left.PrimitiveId.CompareTo(right.PrimitiveId);
            }
        }
    }
}
