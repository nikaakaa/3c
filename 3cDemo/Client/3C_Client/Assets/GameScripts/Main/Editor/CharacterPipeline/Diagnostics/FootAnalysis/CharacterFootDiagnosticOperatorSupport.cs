using System;
using System.Collections.Generic;
using System.Globalization;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    internal static class CharacterFootDiagnosticOperatorSupport
    {
        static readonly double[] s_MeterSeverityThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d,
            0.20d,
            0.30d
        };

        static readonly double[] s_MeterSeverityPenalties =
        {
            0d,
            0.1d,
            0.35d,
            0.7d,
            0.85d,
            0.95d,
            1d
        };

        internal static double RequireNumber(
            DiagnosticOperatorExecutionContext context,
            string id)
        {
            DiagnosticPlanValue value = context.RequireParameter(id);
            double number;
            switch (value.Kind)
            {
                case DiagnosticPlanValueKind.Integer:
                    number = value.Integer;
                    break;
                case DiagnosticPlanValueKind.Number:
                    number = value.Number;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Foot diagnostic parameter '{id}' must be numeric.");
            }
            if (!double.IsFinite(number))
                throw new InvalidOperationException(
                    $"Foot diagnostic parameter '{id}' must be finite.");
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
                    $"Foot diagnostic parameter '{id}' must be a UInt32 integer.");
            }
            return (uint)value.Integer;
        }

        internal static bool TryNormalize(
            in DiagnosticVector3 value,
            out double x,
            out double y,
            out double z)
        {
            double length = Math.Sqrt(
                value.X * value.X +
                value.Y * value.Y +
                value.Z * value.Z);
            if (!double.IsFinite(length) || length <= 0.000000001d)
            {
                x = 0d;
                y = 0d;
                z = 0d;
                return false;
            }
            x = value.X / length;
            y = value.Y / length;
            z = value.Z / length;
            return true;
        }

        internal static double Clearance(
            in DiagnosticVector3 point,
            in DiagnosticVector3 anchor,
            double normalX,
            double normalY,
            double normalZ) =>
            (point.X - anchor.X) * normalX +
            (point.Y - anchor.Y) * normalY +
            (point.Z - anchor.Z) * normalZ;

        internal static double Distance(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right)
        {
            double x = left.X - right.X;
            double y = left.Y - right.Y;
            double z = left.Z - right.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        internal static double HorizontalDistance(
            in DiagnosticVector3 point,
            in DiagnosticVector3 anchor,
            double normalX,
            double normalY,
            double normalZ)
        {
            double x = point.X - anchor.X;
            double y = point.Y - anchor.Y;
            double z = point.Z - anchor.Z;
            double along = x * normalX + y * normalY + z * normalZ;
            double horizontalX = x - along * normalX;
            double horizontalY = y - along * normalY;
            double horizontalZ = z - along * normalZ;
            return Math.Sqrt(
                horizontalX * horizontalX +
                horizontalY * horizontalY +
                horizontalZ * horizontalZ);
        }

        internal static DiagnosticVector3 Midpoint(
            in DiagnosticVector3 left,
            in DiagnosticVector3 right) =>
            new DiagnosticVector3(
                (left.X + right.X) * 0.5f,
                (left.Y + right.Y) * 0.5f,
                (left.Z + right.Z) * 0.5f);

        internal static bool TryRebuildPhysicalProbe(
            in DiagnosticVector3 sourceAnklePosition,
            in DiagnosticQuaternion sourceAnkleRotation,
            in DiagnosticVector3 sourceProbePosition,
            in DiagnosticVector3 physicalAnklePosition,
            in DiagnosticQuaternion physicalAnkleRotation,
            out DiagnosticVector3 physicalProbePosition)
        {
            if (!TryNormalize(
                    sourceAnkleRotation,
                    out double sourceX,
                    out double sourceY,
                    out double sourceZ,
                    out double sourceW) ||
                !TryNormalize(
                    physicalAnkleRotation,
                    out double physicalX,
                    out double physicalY,
                    out double physicalZ,
                    out double physicalW))
            {
                physicalProbePosition = default;
                return false;
            }
            double worldX = sourceProbePosition.X - sourceAnklePosition.X;
            double worldY = sourceProbePosition.Y - sourceAnklePosition.Y;
            double worldZ = sourceProbePosition.Z - sourceAnklePosition.Z;
            Rotate(
                -sourceX,
                -sourceY,
                -sourceZ,
                sourceW,
                worldX,
                worldY,
                worldZ,
                out double localX,
                out double localY,
                out double localZ);
            Rotate(
                physicalX,
                physicalY,
                physicalZ,
                physicalW,
                localX,
                localY,
                localZ,
                out double rotatedX,
                out double rotatedY,
                out double rotatedZ);
            double resultX = physicalAnklePosition.X + rotatedX;
            double resultY = physicalAnklePosition.Y + rotatedY;
            double resultZ = physicalAnklePosition.Z + rotatedZ;
            if (!double.IsFinite(resultX) ||
                !double.IsFinite(resultY) ||
                !double.IsFinite(resultZ) ||
                Math.Abs(resultX) > float.MaxValue ||
                Math.Abs(resultY) > float.MaxValue ||
                Math.Abs(resultZ) > float.MaxValue)
            {
                physicalProbePosition = default;
                return false;
            }
            physicalProbePosition = new DiagnosticVector3(
                (float)resultX,
                (float)resultY,
                (float)resultZ);
            return true;
        }

        internal static string Identity(in DiagnosticBoundInput input) =>
            string.IsNullOrEmpty(input.Handle.Key)
                ? input.Handle.FieldId
                : input.Handle.Key;

        internal static DiagnosticEvidence Evidence(
            string id,
            in DiagnosticBoundInput input,
            string dimensionId,
            ulong sequenceStart,
            ulong sequenceEnd,
            string value) =>
            new DiagnosticEvidence(
                id,
                input.DatasetId,
                Identity(input),
                dimensionId,
                sequenceStart,
                sequenceEnd,
                value);

        internal static DiagnosticFindingCorrelationRange FrameRange(
            in DiagnosticBoundInput frameInput,
            ulong frameStart,
            ulong frameEnd) =>
            new DiagnosticFindingCorrelationRange(
                Identity(frameInput),
                frameStart.ToString(CultureInfo.InvariantCulture),
                frameEnd.ToString(CultureInfo.InvariantCulture));

        internal static string Format(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        internal static string Format(in DiagnosticVector3 value) =>
            value.X.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Y.ToString("R", CultureInfo.InvariantCulture) + " " +
            value.Z.ToString("R", CultureInfo.InvariantCulture);

        internal static string FindingId(string ruleId, ulong sequence) =>
            ruleId + "-sequence-" +
            sequence.ToString(CultureInfo.InvariantCulture);

        internal static string MissingSummary(IEnumerable<string> identities) =>
            "Foot diagnostic evidence is incomplete: " +
            string.Join(", ", identities) + ".";

        internal static string SeverityBand(double meters)
        {
            int index = SeverityBandIndex(meters);
            if (index == 0)
                return "at-or-below-0.01m";
            if (index == s_MeterSeverityThresholds.Length)
                return "above-0.30m";
            return "above-" +
                s_MeterSeverityThresholds[index - 1].ToString("0.00", CultureInfo.InvariantCulture) +
                "m-at-or-below-" +
                s_MeterSeverityThresholds[index].ToString("0.00", CultureInfo.InvariantCulture) +
                "m";
        }

        internal static DiagnosticSeverity Severity(double meters)
        {
            int index = SeverityBandIndex(meters);
            return index >= 3
                ? DiagnosticSeverity.Error
                : index >= 1
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Information;
        }

        internal static double SeverityHealth(
            IReadOnlyList<double> occurrences,
            int eligibleCount)
        {
            if (eligibleCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(eligibleCount));
            double burden = 0d;
            for (int i = 0; i < occurrences.Count; i++)
                burden += s_MeterSeverityPenalties[SeverityBandIndex(occurrences[i])];
            return Math.Max(0d, Math.Min(1d, 1d - burden / eligibleCount));
        }

        static int SeverityBandIndex(double meters)
        {
            for (int i = 0; i < s_MeterSeverityThresholds.Length; i++)
            {
                if (meters <= s_MeterSeverityThresholds[i])
                    return i;
            }
            return s_MeterSeverityThresholds.Length;
        }

        static bool TryNormalize(
            in DiagnosticQuaternion value,
            out double x,
            out double y,
            out double z,
            out double w)
        {
            double length = Math.Sqrt(
                value.X * value.X +
                value.Y * value.Y +
                value.Z * value.Z +
                value.W * value.W);
            if (!double.IsFinite(length) || length <= 0.000000001d)
            {
                x = 0d;
                y = 0d;
                z = 0d;
                w = 1d;
                return false;
            }
            x = value.X / length;
            y = value.Y / length;
            z = value.Z / length;
            w = value.W / length;
            return true;
        }

        static void Rotate(
            double quaternionX,
            double quaternionY,
            double quaternionZ,
            double quaternionW,
            double vectorX,
            double vectorY,
            double vectorZ,
            out double resultX,
            out double resultY,
            out double resultZ)
        {
            double crossX = 2d * (quaternionY * vectorZ - quaternionZ * vectorY);
            double crossY = 2d * (quaternionZ * vectorX - quaternionX * vectorZ);
            double crossZ = 2d * (quaternionX * vectorY - quaternionY * vectorX);
            resultX = vectorX + quaternionW * crossX +
                quaternionY * crossZ - quaternionZ * crossY;
            resultY = vectorY + quaternionW * crossY +
                quaternionZ * crossX - quaternionX * crossZ;
            resultZ = vectorZ + quaternionW * crossZ +
                quaternionX * crossY - quaternionY * crossX;
        }
    }
}
