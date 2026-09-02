using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterFixedInputPresentationScheduleFootCoverage
    {
        public CharacterFixedInputPresentationScheduleFootCoverage(
            int rowCount,
            int distinctFrameCount,
            int firstScheduleFrameIndex,
            int lastScheduleFrameIndex)
        {
            if (rowCount <= 0 || distinctFrameCount <= 0 ||
                firstScheduleFrameIndex < 0 ||
                lastScheduleFrameIndex < firstScheduleFrameIndex)
            {
                throw new ArgumentException(
                    "Presentation Schedule Foot coverage is invalid.");
            }
            RowCount = rowCount;
            DistinctFrameCount = distinctFrameCount;
            FirstScheduleFrameIndex = firstScheduleFrameIndex;
            LastScheduleFrameIndex = lastScheduleFrameIndex;
        }

        public int RowCount { get; }
        public int DistinctFrameCount { get; }
        public int FirstScheduleFrameIndex { get; }
        public int LastScheduleFrameIndex { get; }
    }

    public readonly struct CharacterFixedInputPresentationScheduleEvidence
    {
        public CharacterFixedInputPresentationScheduleEvidence(
            int acceptedEnvelopeRowCount,
            int corridorOutsideEnvelopeRowCount,
            int clampAboveTenCentimetersOutsideCorridorCount,
            float maximumOutsideCorridorClampMeters,
            int verticalEndpointEventCount,
            int representativeFrameSequence,
            string representativeSide,
            int landingSurfaceIdentity,
            int verticalSurfaceIdentity,
            float landingHeight,
            float verticalEdgeUpperHeight,
            float verticalSeparationMeters)
        {
            AcceptedEnvelopeRowCount = acceptedEnvelopeRowCount;
            CorridorOutsideEnvelopeRowCount = corridorOutsideEnvelopeRowCount;
            ClampAboveTenCentimetersOutsideCorridorCount =
                clampAboveTenCentimetersOutsideCorridorCount;
            MaximumOutsideCorridorClampMeters = maximumOutsideCorridorClampMeters;
            VerticalEndpointEventCount = verticalEndpointEventCount;
            RepresentativeFrameSequence = representativeFrameSequence;
            RepresentativeSide = representativeSide ?? string.Empty;
            LandingSurfaceIdentity = landingSurfaceIdentity;
            VerticalSurfaceIdentity = verticalSurfaceIdentity;
            LandingHeight = landingHeight;
            VerticalEdgeUpperHeight = verticalEdgeUpperHeight;
            VerticalSeparationMeters = verticalSeparationMeters;
        }

        public int AcceptedEnvelopeRowCount { get; }
        public int CorridorOutsideEnvelopeRowCount { get; }
        public int ClampAboveTenCentimetersOutsideCorridorCount { get; }
        public float MaximumOutsideCorridorClampMeters { get; }
        public int VerticalEndpointEventCount { get; }
        public int RepresentativeFrameSequence { get; }
        public string RepresentativeSide { get; }
        public int LandingSurfaceIdentity { get; }
        public int VerticalSurfaceIdentity { get; }
        public float LandingHeight { get; }
        public float VerticalEdgeUpperHeight { get; }
        public float VerticalSeparationMeters { get; }
    }

    public interface ICharacterFixedInputPresentationScheduleEvidenceAnalyzer
    {
        CharacterFixedInputPresentationScheduleEvidence Analyze(
            string capabilityManifestPath);

        CharacterFixedInputPresentationScheduleFootCoverage AnalyzeCoverage(
            string capabilityManifestPath,
            IReadOnlyList<ulong> scheduleRenderFrames);
    }

    public static class CharacterFixedInputPresentationScheduleEvidenceRegistry
    {
        static ICharacterFixedInputPresentationScheduleEvidenceAnalyzer
            s_Analyzer;

        public static void Register(
            ICharacterFixedInputPresentationScheduleEvidenceAnalyzer analyzer)
        {
            if (analyzer == null)
                throw new ArgumentNullException(nameof(analyzer));
            if (s_Analyzer != null)
                throw new InvalidOperationException(
                    "Presentation Schedule evidence analyzer is already registered.");
            s_Analyzer = analyzer;
        }

        public static void Unregister(
            ICharacterFixedInputPresentationScheduleEvidenceAnalyzer analyzer)
        {
            if (ReferenceEquals(s_Analyzer, analyzer))
                s_Analyzer = null;
        }

        public static ICharacterFixedInputPresentationScheduleEvidenceAnalyzer
            Require() => s_Analyzer ?? throw new InvalidOperationException(
                "Presentation Schedule evidence analyzer is not compiled.");
    }
}
