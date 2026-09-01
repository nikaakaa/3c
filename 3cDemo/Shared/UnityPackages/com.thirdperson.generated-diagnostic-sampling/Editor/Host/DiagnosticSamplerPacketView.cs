using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPerson.GeneratedDiagnosticSampling.Host
{
    public readonly struct DiagnosticPacketValueView
    {
        readonly DiagnosticCapturePacket m_Packet;
        readonly IReadOnlyList<DiagnosticFieldHandle> m_Fields;

        internal DiagnosticPacketValueView(
            DiagnosticCapturePacket packet,
            IReadOnlyList<DiagnosticFieldHandle> fields)
        {
            m_Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            m_Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public DiagnosticSampleKey SampleKey => m_Packet.SampleKey;
        public IReadOnlyList<DiagnosticFieldHandle> Fields => m_Fields;

        public bool IsAvailable(DiagnosticFieldHandle field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            RequireField(field, field.ValueKind);
            DiagnosticFieldAvailability availability = field.Availability;
            if (!availability.IsDeclared)
                return true;
            switch (availability.ValueKind)
            {
                case DiagnosticValueKind.Boolean:
                    return m_Packet.BooleanValues[availability.DenseIndex] ==
                        (availability.ExpectedValue != 0);
                case DiagnosticValueKind.Int32:
                    return m_Packet.Int32Values[availability.DenseIndex] == availability.ExpectedValue;
                case DiagnosticValueKind.UInt32:
                    return m_Packet.UInt32Values[availability.DenseIndex] ==
                        unchecked((uint)availability.ExpectedValue);
                case DiagnosticValueKind.Int64:
                    return m_Packet.Int64Values[availability.DenseIndex] == availability.ExpectedValue;
                case DiagnosticValueKind.UInt64:
                    return m_Packet.UInt64Values[availability.DenseIndex] ==
                        unchecked((ulong)availability.ExpectedValue);
                default:
                    throw new InvalidOperationException("Diagnostic availability kind is invalid.");
            }
        }

        public bool GetBoolean(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Boolean);
            return m_Packet.BooleanValues[field.DenseIndex];
        }

        public int GetInt32(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Int32);
            return m_Packet.Int32Values[field.DenseIndex];
        }

        public uint GetUInt32(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.UInt32);
            return m_Packet.UInt32Values[field.DenseIndex];
        }

        public long GetInt64(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Int64);
            return m_Packet.Int64Values[field.DenseIndex];
        }

        public ulong GetUInt64(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.UInt64);
            return m_Packet.UInt64Values[field.DenseIndex];
        }

        public float GetFloat32(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Float32);
            return m_Packet.Float32Values[field.DenseIndex];
        }

        public double GetFloat64(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Float64);
            return m_Packet.Float64Values[field.DenseIndex];
        }

        public string GetIdentity(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Identity);
            return m_Packet.IdentityValues[field.DenseIndex];
        }

        public DiagnosticVector2 GetVector2(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Vector2);
            return m_Packet.Vector2Values[field.DenseIndex];
        }

        public DiagnosticVector3 GetVector3(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Vector3);
            return m_Packet.Vector3Values[field.DenseIndex];
        }

        public DiagnosticVector4 GetVector4(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Vector4);
            return m_Packet.Vector4Values[field.DenseIndex];
        }

        public DiagnosticQuaternion GetQuaternion(DiagnosticFieldHandle field)
        {
            RequireField(field, DiagnosticValueKind.Quaternion);
            return m_Packet.QuaternionValues[field.DenseIndex];
        }

        void RequireField(DiagnosticFieldHandle field, DiagnosticValueKind expectedKind)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (field.ValueKind != expectedKind)
                throw new ArgumentException("Diagnostic field value kind does not match the accessor.", nameof(field));
            for (int i = 0; i < m_Fields.Count; i++)
            {
                if (ReferenceEquals(m_Fields[i], field))
                    return;
            }
            throw new ArgumentException("Diagnostic field is not part of this sampler view.", nameof(field));
        }
    }

    public readonly struct DiagnosticSamplerTableView
    {
        readonly DiagnosticTablePacket m_Table;

        internal DiagnosticSamplerTableView(
            DiagnosticTablePacket table,
            DiagnosticTableSchema schema)
        {
            m_Table = table ?? throw new ArgumentNullException(nameof(table));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public DiagnosticTableSchema Schema { get; }
        public int Count => m_Table.Count;

        public DiagnosticPacketValueView Row(int index) =>
            new DiagnosticPacketValueView(m_Table.Row(index), Schema.Fields);
    }

    public readonly struct DiagnosticSamplerPacketView
    {
        readonly DiagnosticCapturePacket m_Packet;

        internal DiagnosticSamplerPacketView(
            DiagnosticCapturePacket packet,
            DiagnosticSamplerLayout sampler)
        {
            m_Packet = packet ?? throw new ArgumentNullException(nameof(packet));
            Sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
        }

        public DiagnosticSamplerLayout Sampler { get; }
        public DiagnosticPacketValueView Values =>
            new DiagnosticPacketValueView(m_Packet, Sampler.Fields);

        public DiagnosticSamplerTableView Table(int index)
        {
            if (index < 0 || index >= Sampler.Tables.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            DiagnosticTableSchema schema = Sampler.Tables[index];
            return new DiagnosticSamplerTableView(m_Packet.Tables[schema.DenseIndex], schema);
        }
    }

    public sealed class DiagnosticSamplerPacketSeries
    {
        readonly IReadOnlyList<DiagnosticCapturePacket> m_Packets;

        internal DiagnosticSamplerPacketSeries(
            DiagnosticSamplerLayout sampler,
            IReadOnlyList<DiagnosticCapturePacket> packets)
        {
            Sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            m_Packets = packets ?? throw new ArgumentNullException(nameof(packets));
        }

        public DiagnosticSamplerLayout Sampler { get; }
        public int Count => m_Packets.Count;
        public DiagnosticSamplerPacketView this[int index] =>
            new DiagnosticSamplerPacketView(m_Packets[index], Sampler);
    }

    public readonly struct DiagnosticFormattedValue
    {
        public DiagnosticFormattedValue(bool isAvailable, string text)
        {
            IsAvailable = isAvailable;
            Text = text ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public string Text { get; }
    }

    public interface IDiagnosticValueFormatter
    {
        DiagnosticFormattedValue Format(
            in DiagnosticPacketValueView values,
            DiagnosticFieldHandle field);
    }

    public sealed class DiagnosticInvariantValueFormatter : IDiagnosticValueFormatter
    {
        public DiagnosticFormattedValue Format(
            in DiagnosticPacketValueView values,
            DiagnosticFieldHandle field)
        {
            if (!values.IsAvailable(field))
                return new DiagnosticFormattedValue(false, string.Empty);
            string text;
            switch (field.ValueKind)
            {
                case DiagnosticValueKind.Boolean:
                    text = values.GetBoolean(field) ? "true" : "false";
                    break;
                case DiagnosticValueKind.Int32:
                    text = values.GetInt32(field).ToString(CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.UInt32:
                    text = values.GetUInt32(field).ToString(CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.Int64:
                    text = values.GetInt64(field).ToString(CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.UInt64:
                    text = values.GetUInt64(field).ToString(CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.Float32:
                    text = values.GetFloat32(field).ToString("R", CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.Float64:
                    text = values.GetFloat64(field).ToString("R", CultureInfo.InvariantCulture);
                    break;
                case DiagnosticValueKind.Identity:
                    text = values.GetIdentity(field) ?? string.Empty;
                    break;
                case DiagnosticValueKind.Vector2:
                    DiagnosticVector2 vector2 = values.GetVector2(field);
                    text = Join(vector2.X, vector2.Y);
                    break;
                case DiagnosticValueKind.Vector3:
                    DiagnosticVector3 vector3 = values.GetVector3(field);
                    text = Join(vector3.X, vector3.Y, vector3.Z);
                    break;
                case DiagnosticValueKind.Vector4:
                    DiagnosticVector4 vector4 = values.GetVector4(field);
                    text = Join(vector4.X, vector4.Y, vector4.Z, vector4.W);
                    break;
                case DiagnosticValueKind.Quaternion:
                    DiagnosticQuaternion quaternion = values.GetQuaternion(field);
                    text = Join(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(field));
            }
            return new DiagnosticFormattedValue(true, text);
        }

        static string Join(float x, float y) =>
            x.ToString("R", CultureInfo.InvariantCulture) + " " +
            y.ToString("R", CultureInfo.InvariantCulture);

        static string Join(float x, float y, float z) =>
            Join(x, y) + " " + z.ToString("R", CultureInfo.InvariantCulture);

        static string Join(float x, float y, float z, float w) =>
            Join(x, y, z) + " " + w.ToString("R", CultureInfo.InvariantCulture);
    }
}
