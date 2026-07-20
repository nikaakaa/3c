using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct ProgramCurveKey
    {
        public ProgramCurveKey(Float32Scalar time, Float32Scalar value, Float32Scalar inTangent, Float32Scalar outTangent, Float32Scalar inWeight, Float32Scalar outWeight, int weightedMode)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
            InWeight = inWeight;
            OutWeight = outWeight;
            WeightedMode = weightedMode;
        }
        public Float32Scalar Time { get; }
        public Float32Scalar Value { get; }
        public Float32Scalar InTangent { get; }
        public Float32Scalar OutTangent { get; }
        public Float32Scalar InWeight { get; }
        public Float32Scalar OutWeight { get; }
        public int WeightedMode { get; }
    }

    public sealed class ProgramCurve
    {
        readonly ReadOnlyCollection<ProgramCurveKey> m_Keys;

        public ProgramCurve(int preWrapMode, int postWrapMode, IEnumerable<ProgramCurveKey> keys)
        {
            PreWrapMode = preWrapMode;
            PostWrapMode = postWrapMode;
            var copied = keys == null ? new List<ProgramCurveKey>() : new List<ProgramCurveKey>(keys);
            copied.Sort((left, right) => left.Time.CompareTo(right.Time));
            for (int i = 1; i < copied.Count; i++)
            {
                if (copied[i - 1].Time == copied[i].Time)
                    throw new ArgumentException($"Program Curve contains duplicate key time '{copied[i].Time}'.", nameof(keys));
            }
            m_Keys = copied.AsReadOnly();
        }

        public int PreWrapMode { get; }
        public int PostWrapMode { get; }
        public IReadOnlyList<ProgramCurveKey> Keys => m_Keys;

        public Float32Scalar Evaluate(Float32Scalar time, Float32Scalar fallback)
        {
            if (m_Keys.Count == 0)
                return fallback;
            if (m_Keys.Count == 1 || time <= m_Keys[0].Time)
                return m_Keys[0].Value;
            if (time >= m_Keys[m_Keys.Count - 1].Time)
                return m_Keys[m_Keys.Count - 1].Value;
            int low = 0;
            int high = m_Keys.Count - 1;
            while (high - low > 1)
            {
                int middle = low + (high - low) / 2;
                if (m_Keys[middle].Time <= time)
                    low = middle;
                else
                    high = middle;
            }
            ProgramCurveKey from = m_Keys[low];
            ProgramCurveKey to = m_Keys[high];
            Float32Scalar duration = to.Time - from.Time;
            if (duration == Float32Scalar.Zero)
                return to.Value;
            Float32Scalar t = (time - from.Time) / duration;
            Float32Scalar t2 = t * t;
            Float32Scalar t3 = t2 * t;
            Float32Scalar two = Float32Scalar.FromInt64(2);
            Float32Scalar three = Float32Scalar.FromInt64(3);
            Float32Scalar h00 = two * t3 - three * t2 + Float32Scalar.One;
            Float32Scalar h10 = t3 - two * t2 + t;
            Float32Scalar h01 = -two * t3 + three * t2;
            Float32Scalar h11 = t3 - t2;
            return h00 * from.Value + h10 * duration * from.OutTangent + h01 * to.Value + h11 * duration * to.InTangent;
        }
    }

    public static class ProgramCurveCodec
    {
        const uint Magic = 0x56525543;
        const int Version = 1;

        public static byte[] Write(ProgramCurve curve)
        {
            if (curve == null)
                throw new ArgumentNullException(nameof(curve));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteInt32(curve.PreWrapMode);
            writer.WriteInt32(curve.PostWrapMode);
            writer.WriteInt32(curve.Keys.Count);
            for (int i = 0; i < curve.Keys.Count; i++)
            {
                ProgramCurveKey key = curve.Keys[i];
                writer.WriteScalar(key.Time);
                writer.WriteScalar(key.Value);
                writer.WriteScalar(key.InTangent);
                writer.WriteScalar(key.OutTangent);
                writer.WriteScalar(key.InWeight);
                writer.WriteScalar(key.OutWeight);
                writer.WriteInt32(key.WeightedMode);
            }
            return writer.ToArray();
        }

        public static ProgramCurve Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Program Curve header is invalid.");
            int preWrap = reader.ReadInt32();
            int postWrap = reader.ReadInt32();
            int count = reader.ReadInt32();
            if (count < 0 || count > 1000000)
                throw new InvalidDataException($"Program Curve key count '{count}' is invalid.");
            var keys = new ProgramCurveKey[count];
            for (int i = 0; i < count; i++)
            {
                keys[i] = new ProgramCurveKey(
                    reader.ReadScalar(),
                    reader.ReadScalar(),
                    reader.ReadScalar(),
                    reader.ReadScalar(),
                    reader.ReadScalar(),
                    reader.ReadScalar(),
                    reader.ReadInt32());
            }
            reader.RequireComplete();
            return new ProgramCurve(preWrap, postWrap, keys);
        }
    }
}
