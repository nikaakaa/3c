using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    internal static class CharacterPresentationReplicationDiagnosticOperatorSupport
    {
        internal static DiagnosticOperatorInputSlot Main(
            string id,
            DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(
                id,
                kind,
                DiagnosticDatasetCardinality.Main,
                true);

        internal static DiagnosticOperatorInputSlot Table(
            string id,
            DiagnosticValueKind kind) =>
            new DiagnosticOperatorInputSlot(
                id,
                kind,
                DiagnosticDatasetCardinality.Table,
                true);

        internal static DiagnosticOperatorParameter Number(
            string id,
            double? minimum = null,
            double? maximum = null) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Number,
                true,
                minimum,
                maximum);

        internal static DiagnosticOperatorParameter Integer(
            string id,
            double? minimum = null,
            double? maximum = null) =>
            new DiagnosticOperatorParameter(
                id,
                DiagnosticOperatorParameterKind.Integer,
                true,
                minimum,
                maximum);

        internal static double RequireNumber(
            DiagnosticOperatorExecutionContext context,
            string id)
        {
            DiagnosticPlanValue value = context.RequireParameter(id);
            double number = value.Kind == DiagnosticPlanValueKind.Integer
                ? value.Integer
                : value.Kind == DiagnosticPlanValueKind.Number
                    ? value.Number
                    : throw new InvalidOperationException(
                        $"Presentation replication parameter '{id}' must be numeric.");
            if (!double.IsFinite(number))
                throw new InvalidOperationException(
                    $"Presentation replication parameter '{id}' must be finite.");
            return number;
        }

        internal static uint RequireUInt32(
            DiagnosticOperatorExecutionContext context,
            string id)
        {
            DiagnosticPlanValue value = context.RequireParameter(id);
            if (value.Kind != DiagnosticPlanValueKind.Integer ||
                value.Integer < 0 ||
                value.Integer > uint.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Presentation replication parameter '{id}' must be a UInt32 integer.");
            }
            return (uint)value.Integer;
        }

        internal static bool Require(
            in DiagnosticDatasetRow row,
            IEnumerable<DiagnosticBoundInput> inputs,
            ISet<string> missing)
        {
            bool available = true;
            foreach (DiagnosticBoundInput input in inputs)
            {
                if (row.IsAvailable(input.Handle))
                    continue;
                missing.Add(Identity(input));
                available = false;
            }
            return available;
        }

        internal static string Identity(in DiagnosticBoundInput input) =>
            string.IsNullOrEmpty(input.Handle.Key)
                ? input.Handle.FieldId
                : input.Handle.Key;

        internal static DiagnosticEvidence Evidence(
            string id,
            DiagnosticBoundInput input,
            in DiagnosticDatasetRow row,
            string value) =>
            new DiagnosticEvidence(
                id,
                input.DatasetId,
                Identity(input),
                row.SampleKey.DimensionId,
                row.SampleKey.Sequence,
                row.SampleKey.Sequence,
                value);

        internal static DiagnosticFinding Finding(
            DiagnosticOperatorExecutionContext context,
            DiagnosticSeverity severity,
            in DiagnosticDatasetRow row,
            string message,
            int index,
            IEnumerable<DiagnosticEvidence> evidence) =>
            new DiagnosticFinding(
                context.RuleId +
                "-sequence-" +
                row.SampleKey.Sequence.ToString(CultureInfo.InvariantCulture) +
                "-finding-" +
                index.ToString(CultureInfo.InvariantCulture),
                severity,
                row.SampleKey.DimensionId,
                row.SampleKey.Sequence,
                row.SampleKey.Sequence,
                message,
                evidence);

        internal static DiagnosticFinding Finding(
            DiagnosticOperatorExecutionContext context,
            DiagnosticSeverity severity,
            string dimension,
            ulong sequenceStart,
            ulong sequenceEnd,
            string message,
            int index,
            IEnumerable<DiagnosticEvidence> evidence) =>
            new DiagnosticFinding(
                context.RuleId +
                "-sequence-" +
                sequenceStart.ToString(CultureInfo.InvariantCulture) +
                "-finding-" +
                index.ToString(CultureInfo.InvariantCulture),
                severity,
                dimension,
                sequenceStart,
                sequenceEnd,
                message,
                evidence);

        internal static string MissingSummary(IEnumerable<string> values) =>
            "Presentation replication evidence is incomplete: " +
            string.Join(", ", values) + ".";

        internal static string Format(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        internal static string Format(in DiagnosticVector3 value) =>
            value.X.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Y.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Z.ToString("R", CultureInfo.InvariantCulture);

        internal static string Format(in DiagnosticQuaternion value) =>
            value.X.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Y.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Z.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.W.ToString("R", CultureInfo.InvariantCulture);

        internal static double Distance(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right)
        {
            double x = left.X - right.X;
            double y = left.Y - right.Y;
            double z = left.Z - right.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        internal static double AngleDegrees(
            in DiagnosticQuaternion left,
            in DiagnosticQuaternion right)
        {
            double leftLength = Math.Sqrt(
                left.X * left.X +
                left.Y * left.Y +
                left.Z * left.Z +
                left.W * left.W);
            double rightLength = Math.Sqrt(
                right.X * right.X +
                right.Y * right.Y +
                right.Z * right.Z +
                right.W * right.W);
            if (!double.IsFinite(leftLength) ||
                !double.IsFinite(rightLength) ||
                leftLength <= 0.000000001d ||
                rightLength <= 0.000000001d)
            {
                return double.PositiveInfinity;
            }
            double dot = Math.Abs((
                left.X * right.X +
                left.Y * right.Y +
                left.Z * right.Z +
                left.W * right.W) / (leftLength * rightLength));
            dot = Math.Max(-1d, Math.Min(1d, dot));
            return 2d * Math.Acos(dot) * 180d / Math.PI;
        }

        internal static bool IsFinite(in DiagnosticVector3 value) =>
            double.IsFinite(value.X) &&
            double.IsFinite(value.Y) &&
            double.IsFinite(value.Z);

        internal static bool IsFinite(in DiagnosticQuaternion value) =>
            double.IsFinite(value.X) &&
            double.IsFinite(value.Y) &&
            double.IsFinite(value.Z) &&
            double.IsFinite(value.W);
    }
}
