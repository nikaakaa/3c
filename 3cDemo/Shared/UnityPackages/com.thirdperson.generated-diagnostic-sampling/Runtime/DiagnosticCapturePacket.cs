using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticPacketLayout
    {
        public DiagnosticPacketLayout(
            string identity,
            int booleanCount,
            int int32Count,
            int uint32Count,
            int int64Count,
            int uint64Count,
            int float32Count,
            int float64Count,
            int identityCount,
            int vector2Count,
            int vector3Count,
            int vector4Count,
            int quaternionCount,
            IEnumerable<DiagnosticTableLayout> tables = null)
        {
            Identity = DiagnosticIdentity.RequireId(identity, nameof(identity));
            BooleanCount = RequireCount(booleanCount, nameof(booleanCount));
            Int32Count = RequireCount(int32Count, nameof(int32Count));
            UInt32Count = RequireCount(uint32Count, nameof(uint32Count));
            Int64Count = RequireCount(int64Count, nameof(int64Count));
            UInt64Count = RequireCount(uint64Count, nameof(uint64Count));
            Float32Count = RequireCount(float32Count, nameof(float32Count));
            Float64Count = RequireCount(float64Count, nameof(float64Count));
            IdentityCount = RequireCount(identityCount, nameof(identityCount));
            Vector2Count = RequireCount(vector2Count, nameof(vector2Count));
            Vector3Count = RequireCount(vector3Count, nameof(vector3Count));
            Vector4Count = RequireCount(vector4Count, nameof(vector4Count));
            QuaternionCount = RequireCount(quaternionCount, nameof(quaternionCount));
            Tables = tables == null
                ? Array.Empty<DiagnosticTableLayout>()
                : tables.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray();
        }

        public string Identity { get; }
        public int BooleanCount { get; }
        public int Int32Count { get; }
        public int UInt32Count { get; }
        public int Int64Count { get; }
        public int UInt64Count { get; }
        public int Float32Count { get; }
        public int Float64Count { get; }
        public int IdentityCount { get; }
        public int Vector2Count { get; }
        public int Vector3Count { get; }
        public int Vector4Count { get; }
        public int QuaternionCount { get; }
        public IReadOnlyList<DiagnosticTableLayout> Tables { get; }

        static int RequireCount(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    public sealed class DiagnosticTableLayout
    {
        public DiagnosticTableLayout(
            string id,
            int capacity,
            DiagnosticPacketLayout rowLayout)
        {
            Id = DiagnosticIdentity.RequireId(id, nameof(id));
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            RowLayout = rowLayout ?? throw new ArgumentNullException(nameof(rowLayout));
            if (rowLayout.Tables.Count != 0)
                throw new ArgumentException("Nested diagnostic tables are not supported.", nameof(rowLayout));
        }

        public string Id { get; }
        public int Capacity { get; }
        public DiagnosticPacketLayout RowLayout { get; }
    }

    public sealed class DiagnosticTablePacket
    {
        readonly DiagnosticCapturePacket[] m_Rows;

        public DiagnosticTablePacket(DiagnosticTableLayout layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Rows = new DiagnosticCapturePacket[layout.Capacity];
            for (int i = 0; i < m_Rows.Length; i++)
                m_Rows[i] = new DiagnosticCapturePacket(layout.RowLayout);
        }

        public DiagnosticTableLayout Layout { get; }
        public int Count { get; private set; }

        public void Begin(int count, in DiagnosticSampleKey sampleKey)
        {
            if (count < 0 || count > Layout.Capacity)
                throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            for (int i = 0; i < count; i++)
                m_Rows[i].Begin(sampleKey);
        }

        public DiagnosticCapturePacket Row(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Rows[index];
        }
    }

    public sealed class DiagnosticCapturePacket
    {
        int m_LeaseVersion;
        bool m_IsLeased;

        public DiagnosticCapturePacket(DiagnosticPacketLayout layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            BooleanValues = new bool[layout.BooleanCount];
            Int32Values = new int[layout.Int32Count];
            UInt32Values = new uint[layout.UInt32Count];
            Int64Values = new long[layout.Int64Count];
            UInt64Values = new ulong[layout.UInt64Count];
            Float32Values = new float[layout.Float32Count];
            Float64Values = new double[layout.Float64Count];
            IdentityValues = new string[layout.IdentityCount];
            Vector2Values = new DiagnosticVector2[layout.Vector2Count];
            Vector3Values = new DiagnosticVector3[layout.Vector3Count];
            Vector4Values = new DiagnosticVector4[layout.Vector4Count];
            QuaternionValues = new DiagnosticQuaternion[layout.QuaternionCount];
            Tables = layout.Tables.Select(value => new DiagnosticTablePacket(value)).ToArray();
        }

        public DiagnosticPacketLayout Layout { get; }
        public DiagnosticSampleKey SampleKey { get; private set; }
        public bool[] BooleanValues { get; }
        public int[] Int32Values { get; }
        public uint[] UInt32Values { get; }
        public long[] Int64Values { get; }
        public ulong[] UInt64Values { get; }
        public float[] Float32Values { get; }
        public double[] Float64Values { get; }
        public string[] IdentityValues { get; }
        public DiagnosticVector2[] Vector2Values { get; }
        public DiagnosticVector3[] Vector3Values { get; }
        public DiagnosticVector4[] Vector4Values { get; }
        public DiagnosticQuaternion[] QuaternionValues { get; }
        public DiagnosticTablePacket[] Tables { get; }

        public void Begin(in DiagnosticSampleKey sampleKey)
        {
            SampleKey = sampleKey;
            Array.Clear(BooleanValues, 0, BooleanValues.Length);
            Array.Clear(Int32Values, 0, Int32Values.Length);
            Array.Clear(UInt32Values, 0, UInt32Values.Length);
            Array.Clear(Int64Values, 0, Int64Values.Length);
            Array.Clear(UInt64Values, 0, UInt64Values.Length);
            Array.Clear(Float32Values, 0, Float32Values.Length);
            Array.Clear(Float64Values, 0, Float64Values.Length);
            Array.Clear(IdentityValues, 0, IdentityValues.Length);
            Array.Clear(Vector2Values, 0, Vector2Values.Length);
            Array.Clear(Vector3Values, 0, Vector3Values.Length);
            Array.Clear(Vector4Values, 0, Vector4Values.Length);
            Array.Clear(QuaternionValues, 0, QuaternionValues.Length);
            for (int i = 0; i < Tables.Length; i++)
                Tables[i].Begin(0, sampleKey);
        }

        internal int BeginLease(in DiagnosticSampleKey sampleKey)
        {
            if (m_IsLeased)
                throw new InvalidOperationException("Diagnostic packet is already leased.");
            m_LeaseVersion++;
            if (m_LeaseVersion == 0)
                m_LeaseVersion = 1;
            m_IsLeased = true;
            Begin(sampleKey);
            return m_LeaseVersion;
        }

        internal void RequireLease(int version)
        {
            if (!m_IsLeased || version != m_LeaseVersion)
                throw new InvalidOperationException("Diagnostic packet lease is invalid.");
        }

        internal bool IsLeaseValid(int version) =>
            m_IsLeased && version == m_LeaseVersion;

        internal void EndLease(int version)
        {
            RequireLease(version);
            m_IsLeased = false;
        }

        public void SetBoolean(int index, bool value) => BooleanValues[index] = value;
        public void SetInt32(int index, int value) => Int32Values[index] = value;
        public void SetUInt32(int index, uint value) => UInt32Values[index] = value;
        public void SetInt64(int index, long value) => Int64Values[index] = value;
        public void SetUInt64(int index, ulong value) => UInt64Values[index] = value;
        public void SetFloat32(int index, float value) => Float32Values[index] = value;
        public void SetFloat64(int index, double value) => Float64Values[index] = value;
        public void SetIdentity(int index, string value) =>
            IdentityValues[index] = DiagnosticIdentity.RequireId(value, nameof(value));
        public void SetVector2(int index, DiagnosticVector2 value) => Vector2Values[index] = value;
        public void SetVector3(int index, DiagnosticVector3 value) => Vector3Values[index] = value;
        public void SetVector4(int index, DiagnosticVector4 value) => Vector4Values[index] = value;
        public void SetQuaternion(int index, DiagnosticQuaternion value) => QuaternionValues[index] = value;
    }
}
