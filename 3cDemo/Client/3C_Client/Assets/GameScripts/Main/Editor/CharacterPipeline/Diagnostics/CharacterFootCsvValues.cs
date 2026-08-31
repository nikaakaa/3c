using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterFootCsvValues
    {
        internal static void Add(StringBuilder row, string value)
        {
            Separate(row);
            value ??= string.Empty;
            if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
                throw new InvalidOperationException(
                    "Foot Landing CSV string contains a line break.");
            bool quote = value.IndexOf(',') >= 0 ||
                         value.IndexOf('"') >= 0;
            if (!quote)
            {
                row.Append(value);
                return;
            }
            row.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '"')
                    row.Append('"');
                row.Append(character);
            }
            row.Append('"');
        }

        internal static void Add(StringBuilder row, bool value) => Add(row, value ? 1 : 0);

        internal static void Add(StringBuilder row, int value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        internal static void Add(StringBuilder row, ulong value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        internal static void Add(StringBuilder row, float value)
        {
            Separate(row);
            row.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal static void Add(StringBuilder row, Vector3 value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
        }

        internal static void Add(StringBuilder row, Quaternion value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
            Add(row, value.w);
        }

        internal static void Separate(StringBuilder row)
        {
            if (row.Length > 0)
                row.Append(',');
        }

        internal static float ParseFloat(string value, string field)
        {
            if (!float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result) ||
                !float.IsFinite(result))
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row {field} '{value}' is invalid.");
            }
            return result;
        }

        internal static int ParseInt(string value, string field)
        {
            if (!int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result))
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row {field} '{value}' is invalid.");
            }
            return result;
        }

        internal static ulong ParseUlong(string value, string field)
        {
            if (!ulong.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out ulong result))
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row {field} '{value}' is invalid.");
            }
            return result;
        }
    }
}
