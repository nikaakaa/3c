using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal enum CharacterFootCsvUnit
    {
        None, Identity, Frame, Category, Metres, Seconds, Degrees,
        MetresPerSecond, Unitless, Direction, Count, Bitmask, Hertz,
        DegreesPerSecond, PerSecond
    }

    internal enum CharacterFootCsvKind
    {
        Text, Boolean, Int32, UInt64, Float32, Vector3, Quaternion
    }

    internal readonly struct CharacterFootCsvColumnInfo
    {
        internal CharacterFootCsvColumnInfo(
            string name, CharacterFootCsvKind kind, CharacterFootCsvUnit unit,
            string availabilityColumn, string availabilityValue, string group = null)
        {
            Name = name;
            Kind = kind;
            Unit = unit;
            AvailabilityColumn = availabilityColumn;
            AvailabilityValue = availabilityValue;
            Group = group;
        }

        internal string Group { get; }
        internal string Name { get; }
        internal CharacterFootCsvKind Kind { get; }
        internal CharacterFootCsvUnit Unit { get; }
        internal string AvailabilityColumn { get; }
        internal string AvailabilityValue { get; }
    }

    internal delegate TValue CharacterFootCsvGetter<TSource, TValue>(in TSource source);
    internal delegate void CharacterFootCsvWrite<TSource>(StringBuilder row, in TSource source);

    internal sealed class CharacterFootCsvCodec<T>
    {
        internal CharacterFootCsvCodec(
            CharacterFootCsvKind kind, string[] suffixes,
            Action<StringBuilder, T> write, Func<string[], int[], string[], T> read)
        {
            Kind = kind;
            Suffixes = Array.AsReadOnly(suffixes);
            Write = write;
            Read = read;
        }

        internal CharacterFootCsvKind Kind { get; }
        internal ReadOnlyCollection<string> Suffixes { get; }
        internal Action<StringBuilder, T> Write { get; }
        internal Func<string[], int[], string[], T> Read { get; }
    }

    internal static class CharacterFootCsvCodecs
    {
        static readonly string[] Scalar = { string.Empty };
        internal static readonly CharacterFootCsvCodec<string> Text = new CharacterFootCsvCodec<string>(
            CharacterFootCsvKind.Text, Scalar, Add, (c, i, n) => c[i[0]]);
        internal static readonly CharacterFootCsvCodec<bool> Boolean = new CharacterFootCsvCodec<bool>(
            CharacterFootCsvKind.Boolean, Scalar, Add, (c, i, n) => ParseInt(c[i[0]], n[0]) != 0);
        internal static readonly CharacterFootCsvCodec<int> Int32 = new CharacterFootCsvCodec<int>(
            CharacterFootCsvKind.Int32, Scalar, Add, (c, i, n) => ParseInt(c[i[0]], n[0]));
        internal static readonly CharacterFootCsvCodec<ulong> UInt64 = new CharacterFootCsvCodec<ulong>(
            CharacterFootCsvKind.UInt64, Scalar, Add, (c, i, n) => ParseUlong(c[i[0]], n[0]));
        internal static readonly CharacterFootCsvCodec<float> Float32 = new CharacterFootCsvCodec<float>(
            CharacterFootCsvKind.Float32, Scalar, Add, (c, i, n) => ParseFloat(c[i[0]], n[0]));
        internal static readonly CharacterFootCsvCodec<float> NonNegativeDuration = new CharacterFootCsvCodec<float>(
            CharacterFootCsvKind.Float32, Scalar, Add, (c, i, n) => ParseNonNegativeDuration(c[i[0]], n[0]));
        internal static readonly CharacterFootCsvCodec<Vector3> Vector = new CharacterFootCsvCodec<Vector3>(
            CharacterFootCsvKind.Vector3, new[] { "X", "Y", "Z" }, Add,
            (c, i, n) => new Vector3(ParseFloat(c[i[0]], n[0]), ParseFloat(c[i[1]], n[1]), ParseFloat(c[i[2]], n[2])));
        internal static readonly CharacterFootCsvCodec<Quaternion> Rotation = new CharacterFootCsvCodec<Quaternion>(
            CharacterFootCsvKind.Quaternion, new[] { "X", "Y", "Z", "W" }, Add,
            (c, i, n) => new Quaternion(ParseFloat(c[i[0]], n[0]), ParseFloat(c[i[1]], n[1]), ParseFloat(c[i[2]], n[2]), ParseFloat(c[i[3]], n[3])));
    }

    internal sealed class CharacterFootCsvColumn<TSource, TRecord>
    {
        readonly CharacterFootCsvWrite<TSource> m_Write;
        readonly Func<Dictionary<string, int>, Action<string[], TRecord>> m_Bind;

        CharacterFootCsvColumn(
            CharacterFootCsvColumnInfo[] columns,
            CharacterFootCsvWrite<TSource> write,
            Func<Dictionary<string, int>, Action<string[], TRecord>> bind)
        {
            Columns = Array.AsReadOnly(columns);
            m_Write = write;
            m_Bind = bind;
        }

        internal ReadOnlyCollection<CharacterFootCsvColumnInfo> Columns { get; }
        internal void Write(StringBuilder row, in TSource source) => m_Write(row, in source);
        internal Action<string[], TRecord> Bind(Dictionary<string, int> indices) => m_Bind(indices);

        internal CharacterFootCsvColumn<TParentSource, TParentRecord> Project<TParentSource, TParentRecord>(
            CharacterFootCsvGetter<TParentSource, TSource> source,
            Func<TParentRecord, TRecord> record)
        {
            var columns = new CharacterFootCsvColumnInfo[Columns.Count];
            Columns.CopyTo(columns, 0);
            return new CharacterFootCsvColumn<TParentSource, TParentRecord>(
                columns,
                (StringBuilder row, in TParentSource parent) =>
                {
                    TSource value = source(in parent);
                    m_Write(row, in value);
                },
                indices =>
                {
                    Action<string[], TRecord> read = m_Bind(indices);
                    return (cells, parent) => read(cells, record(parent));
                });
        }

        internal static CharacterFootCsvColumn<TSource, TRecord> Create<T>(
            string name, CharacterFootCsvCodec<T> codec, CharacterFootCsvUnit unit,
            CharacterFootCsvGetter<TSource, T> source, Action<TRecord, T> target,
            string availabilityColumn = null, string availabilityValue = "1")
        {
            if (string.IsNullOrEmpty(name) || codec == null || source == null || target == null ||
                !Enum.IsDefined(typeof(CharacterFootCsvUnit), unit))
                throw new ArgumentException("Foot CSV column binding is incomplete.");
            var names = new string[codec.Suffixes.Count];
            var columns = new CharacterFootCsvColumnInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = name + codec.Suffixes[i];
                columns[i] = new CharacterFootCsvColumnInfo(
                    names[i],
                    codec.Kind == CharacterFootCsvKind.Vector3 || codec.Kind == CharacterFootCsvKind.Quaternion
                        ? CharacterFootCsvKind.Float32 : codec.Kind,
                    unit, availabilityColumn, availabilityValue);
            }
            return new CharacterFootCsvColumn<TSource, TRecord>(
                columns,
                (StringBuilder row, in TSource value) => codec.Write(row, source(in value)),
                indices =>
                {
                    var bound = new int[names.Length];
                    for (int i = 0; i < names.Length; i++)
                        if (!indices.TryGetValue(names[i], out bound[i]))
                            throw new InvalidDataException($"Foot Motion samples CSV is missing '{names[i]}'.");
                    return (cells, record) => target(record, codec.Read(cells, bound, names));
                });
        }
    }

    internal sealed class CharacterFootCsvReader<TRecord>
    {
        readonly Func<TRecord> m_Create;
        readonly Action<string[], TRecord>[] m_Read;

        internal CharacterFootCsvReader(Func<TRecord> create, Action<string[], TRecord>[] read)
        {
            m_Create = create;
            m_Read = read;
        }

        internal TRecord Read(string[] cells)
        {
            TRecord record = m_Create();
            for (int i = 0; i < m_Read.Length; i++)
                m_Read[i](cells, record);
            return record;
        }
    }

    internal sealed class CharacterFootCsvGroup<TSource, TRecord>
    {
        readonly CharacterFootCsvColumn<TSource, TRecord>[] m_Columns;
        readonly Func<TRecord> m_Create;

        internal CharacterFootCsvGroup(
            string group,
            Func<TRecord> create, CharacterFootCsvColumn<TSource, TRecord>[] columns)
        {
            if (string.IsNullOrEmpty(group))
                throw new ArgumentException("Foot CSV group is missing.", nameof(group));
            m_Create = create ?? throw new ArgumentNullException(nameof(create));
            m_Columns = columns;
            var names = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var metadata = new List<CharacterFootCsvColumnInfo>();
            foreach (CharacterFootCsvColumn<TSource, TRecord> column in columns)
                foreach (CharacterFootCsvColumnInfo info in column.Columns)
                {
                    if (!unique.Add(info.Name))
                        throw new InvalidOperationException($"Duplicate Foot CSV binding '{info.Name}'.");
                    names.Add(info.Name);
                    metadata.Add(new CharacterFootCsvColumnInfo(
                        info.Name, info.Kind, info.Unit,
                        info.AvailabilityColumn, info.AvailabilityValue, group));
                }
            foreach (CharacterFootCsvColumnInfo info in metadata)
                if (!string.IsNullOrEmpty(info.AvailabilityColumn) && !unique.Contains(info.AvailabilityColumn))
                    throw new InvalidOperationException($"Foot CSV availability binding '{info.AvailabilityColumn}' is missing.");
            Header = string.Join(",", names);
            Columns = metadata.AsReadOnly();
        }

        internal string Header { get; }
        internal ReadOnlyCollection<CharacterFootCsvColumnInfo> Columns { get; }

        internal void Write(StringBuilder row, in TSource source)
        {
            for (int i = 0; i < m_Columns.Length; i++)
                m_Columns[i].Write(row, in source);
        }

        internal CharacterFootCsvColumn<TParentSource, TParentRecord>[] Project<TParentSource, TParentRecord>(
            CharacterFootCsvGetter<TParentSource, TSource> source,
            Func<TParentRecord, TRecord> record)
        {
            var result = new CharacterFootCsvColumn<TParentSource, TParentRecord>[m_Columns.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = m_Columns[i].Project(source, record);
            return result;
        }

        internal CharacterFootCsvReader<TRecord> Bind(Dictionary<string, int> indices)
        {
            var read = new Action<string[], TRecord>[m_Columns.Length];
            for (int i = 0; i < m_Columns.Length; i++)
                read[i] = m_Columns[i].Bind(indices);
            return new CharacterFootCsvReader<TRecord>(m_Create, read);
        }
    }
}
