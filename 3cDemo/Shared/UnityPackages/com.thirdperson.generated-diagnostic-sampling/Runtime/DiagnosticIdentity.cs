using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public static class DiagnosticIdentity
    {
        public static string RequireId(string value, string parameterName)
        {
            value = RequireText(value, parameterName);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                bool valid =
                    character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9' ||
                    character == '.' ||
                    character == '-' ||
                    character == '_' ||
                    character == '/';
                if (!valid)
                    throw new ArgumentException($"Diagnostic identity '{value}' is invalid.", parameterName);
            }
            return value;
        }

        public static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            return value.Trim();
        }

        public static int RequireRevision(int value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }

        public static IReadOnlyList<string> NormalizeIds(
            IEnumerable<string> values,
            string parameterName)
        {
            if (values == null)
                return Array.Empty<string>();
            string[] result = values
                .Select(value => RequireId(value, parameterName))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return result;
        }

        public static IReadOnlyList<T> NormalizeDescriptors<T>(
            IEnumerable<T> values,
            Func<T, string> identity,
            string parameterName)
        {
            if (values == null)
                return Array.Empty<T>();
            T[] result = values
                .OrderBy(identity, StringComparer.Ordinal)
                .ToArray();
            for (int i = 1; i < result.Length; i++)
            {
                if (string.Equals(identity(result[i - 1]), identity(result[i]), StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate diagnostic identity '{identity(result[i])}'.", parameterName);
            }
            return result;
        }

        public static string Hash(IEnumerable<string> canonicalParts)
        {
            if (canonicalParts == null)
                throw new ArgumentNullException(nameof(canonicalParts));
            string canonical = string.Join("\n", canonicalParts);
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }
    }
}
