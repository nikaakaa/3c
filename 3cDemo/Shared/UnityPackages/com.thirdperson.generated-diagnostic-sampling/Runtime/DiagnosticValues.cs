using System;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public readonly struct DiagnosticVector2
    {
        public DiagnosticVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
    }

    public readonly struct DiagnosticVector3
    {
        public DiagnosticVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }

    public readonly struct DiagnosticVector4
    {
        public DiagnosticVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
    }

    public readonly struct DiagnosticQuaternion
    {
        public DiagnosticQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
    }

    public readonly struct DiagnosticLineageKey
    {
        public DiagnosticLineageKey(
            string typeIdentity,
            ulong valueHigh,
            ulong valueLow)
        {
            TypeIdentity = DiagnosticIdentity.RequireId(typeIdentity, nameof(typeIdentity));
            ValueHigh = valueHigh;
            ValueLow = valueLow;
        }

        public string TypeIdentity { get; }
        public ulong ValueHigh { get; }
        public ulong ValueLow { get; }
    }

    public readonly struct DiagnosticSampleKey
    {
        public DiagnosticSampleKey(
            ulong sequence,
            in DiagnosticLineageKey lineage)
        {
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Lineage = lineage;
        }

        public ulong Sequence { get; }
        public DiagnosticLineageKey Lineage { get; }
    }
}
