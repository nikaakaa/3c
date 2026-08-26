using System;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal enum CharacterFootContactPlanePenetrationAvailability : byte
    {
        Available = 0,
        FinalPhysicalPoseUnavailable = 1,
        ContactLifecycleUnavailable = 2,
        ContactPlaneUnavailable = 3,
        EventLineageMismatch = 4,
        SurfaceLineageMismatch = 5,
        InvalidContactNormal = 6
    }

    internal enum CharacterFootContactPlanePenetrationResponsibility : byte
    {
        Clear = 0,
        Introduced = 1,
        Amplified = 2,
        PartiallyResolved = 3,
        Resolved = 4,
        BaselineResidual = 5
    }

    internal readonly struct CharacterFootContactLinePenetration
    {
        internal CharacterFootContactLinePenetration(
            double heelDepth,
            double toeDepth,
            double maximumDepth,
            double meanDepth,
            double lengthCoefficient)
        {
            HeelDepth = heelDepth;
            ToeDepth = toeDepth;
            MaximumDepth = maximumDepth;
            MeanDepth = meanDepth;
            LengthCoefficient = lengthCoefficient;
        }

        internal double HeelDepth { get; }
        internal double ToeDepth { get; }
        internal double MaximumDepth { get; }
        internal double MeanDepth { get; }
        internal double LengthCoefficient { get; }
    }

    internal readonly struct CharacterFootContactPlanePenetrationSample
    {
        internal CharacterFootContactPlanePenetrationSample(
            CharacterFootContactLinePenetration source,
            CharacterFootContactLinePenetration final,
            double introducedHeelDepth,
            double introducedToeDepth,
            double resolvedHeelDepth,
            double resolvedToeDepth,
            CharacterFootContactPlanePenetrationResponsibility heelResponsibility,
            CharacterFootContactPlanePenetrationResponsibility toeResponsibility)
        {
            Source = source;
            Final = final;
            IntroducedHeelDepth = introducedHeelDepth;
            IntroducedToeDepth = introducedToeDepth;
            ResolvedHeelDepth = resolvedHeelDepth;
            ResolvedToeDepth = resolvedToeDepth;
            HeelResponsibility = heelResponsibility;
            ToeResponsibility = toeResponsibility;
        }

        internal CharacterFootContactLinePenetration Source { get; }
        internal CharacterFootContactLinePenetration Final { get; }
        internal double IntroducedHeelDepth { get; }
        internal double IntroducedToeDepth { get; }
        internal double ResolvedHeelDepth { get; }
        internal double ResolvedToeDepth { get; }
        internal double IntroducedMaximumDepth =>
            Math.Max(IntroducedHeelDepth, IntroducedToeDepth);
        internal double ResolvedMaximumDepth =>
            Math.Max(ResolvedHeelDepth, ResolvedToeDepth);
        internal CharacterFootContactPlanePenetrationResponsibility HeelResponsibility { get; }
        internal CharacterFootContactPlanePenetrationResponsibility ToeResponsibility { get; }
    }

    internal static class CharacterFootContactPlanePenetration
    {
        internal const double GeometryEpsilonMeters = 0.00001d;

        internal static CharacterFootContactPlanePenetrationSample Evaluate(
            double sourceHeelClearance,
            double sourceToeClearance,
            double finalHeelClearance,
            double finalToeClearance)
        {
            RequireFinite(sourceHeelClearance);
            RequireFinite(sourceToeClearance);
            RequireFinite(finalHeelClearance);
            RequireFinite(finalToeClearance);
            CharacterFootContactLinePenetration source =
                ResolveLine(sourceHeelClearance, sourceToeClearance);
            CharacterFootContactLinePenetration final =
                ResolveLine(finalHeelClearance, finalToeClearance);
            return new CharacterFootContactPlanePenetrationSample(
                source,
                final,
                Math.Max(0d, final.HeelDepth - source.HeelDepth),
                Math.Max(0d, final.ToeDepth - source.ToeDepth),
                Math.Max(0d, source.HeelDepth - final.HeelDepth),
                Math.Max(0d, source.ToeDepth - final.ToeDepth),
                ResolveResponsibility(source.HeelDepth, final.HeelDepth),
                ResolveResponsibility(source.ToeDepth, final.ToeDepth));
        }

        internal static CharacterFootContactLinePenetration ResolveLine(
            double heelClearance,
            double toeClearance)
        {
            RequireFinite(heelClearance);
            RequireFinite(toeClearance);
            double heelDepth = Math.Max(0d, -heelClearance);
            double toeDepth = Math.Max(0d, -toeClearance);
            double coefficient;
            double meanDepth;
            if (heelClearance >= 0d && toeClearance >= 0d)
            {
                coefficient = 0d;
                meanDepth = 0d;
            }
            else if (heelClearance < 0d && toeClearance < 0d)
            {
                coefficient = 1d;
                meanDepth = (heelDepth + toeDepth) * 0.5d;
            }
            else if (heelClearance < 0d)
            {
                coefficient = -heelClearance / (toeClearance - heelClearance);
                meanDepth = 0.5d * coefficient * heelDepth;
            }
            else
            {
                coefficient = -toeClearance / (heelClearance - toeClearance);
                meanDepth = 0.5d * coefficient * toeDepth;
            }
            return new CharacterFootContactLinePenetration(
                heelDepth,
                toeDepth,
                Math.Max(heelDepth, toeDepth),
                meanDepth,
                Math.Max(0d, Math.Min(1d, coefficient)));
        }

        internal static CharacterFootContactPlanePenetrationResponsibility
            ResolveResponsibility(double sourceDepth, double finalDepth)
        {
            RequireFinite(sourceDepth);
            RequireFinite(finalDepth);
            if (sourceDepth <= GeometryEpsilonMeters &&
                finalDepth <= GeometryEpsilonMeters)
            {
                return CharacterFootContactPlanePenetrationResponsibility.Clear;
            }
            if (sourceDepth <= GeometryEpsilonMeters)
                return CharacterFootContactPlanePenetrationResponsibility.Introduced;
            if (finalDepth <= GeometryEpsilonMeters)
                return CharacterFootContactPlanePenetrationResponsibility.Resolved;
            double change = finalDepth - sourceDepth;
            if (Math.Abs(change) <= GeometryEpsilonMeters)
            {
                return CharacterFootContactPlanePenetrationResponsibility
                    .BaselineResidual;
            }
            return change > 0d
                ? CharacterFootContactPlanePenetrationResponsibility.Amplified
                : CharacterFootContactPlanePenetrationResponsibility.PartiallyResolved;
        }

        static void RequireFinite(double value)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
