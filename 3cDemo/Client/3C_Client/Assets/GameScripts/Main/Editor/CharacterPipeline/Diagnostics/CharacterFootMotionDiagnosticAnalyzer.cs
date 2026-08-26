using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootMotionDiagnosticAnalysis
    {
        internal CharacterFootMotionDiagnosticAnalysis(
            string samplesPath,
            string geometryPath,
            string factsPath,
            string diagnosisDirectory,
            int frameCount,
            int footRowCount,
            int eventCount,
            int diagnosisTargetCount,
            int diagnosisMatchCount,
            string summary)
        {
            SamplesPath = samplesPath ?? string.Empty;
            GeometryPath = geometryPath ?? string.Empty;
            FactsPath = factsPath ?? string.Empty;
            DiagnosisDirectory = diagnosisDirectory ?? string.Empty;
            FrameCount = frameCount;
            FootRowCount = footRowCount;
            EventCount = eventCount;
            DiagnosisTargetCount = diagnosisTargetCount;
            DiagnosisMatchCount = diagnosisMatchCount;
            Summary = summary ?? string.Empty;
        }

        internal string SamplesPath { get; }
        internal string GeometryPath { get; }
        internal string FactsPath { get; }
        internal string DiagnosisDirectory { get; }
        internal int FrameCount { get; }
        internal int FootRowCount { get; }
        internal int EventCount { get; }
        internal int DiagnosisTargetCount { get; }
        internal int DiagnosisMatchCount { get; }
        internal string Summary { get; }
    }

    internal static class CharacterFootMotionDiagnosticAnalyzer
    {
        const string Schema = "character-foot-motion-facts/9";
        const string AnalyzerId = "character-foot-motion-fact-analyzer";
        const int AnalyzerVersion = 8;
        const string GeometryFileName = "ground-path-geometry.csv";
        const int HeaderColumnCapacity = 576;
        const float PositionNoiseFloor = 0.001f;
        const float TimeEpsilon = 0.000001f;

        internal static CharacterFootMotionDiagnosticAnalysis Analyze(
            string samplesPath)
        {
            if (string.IsNullOrWhiteSpace(samplesPath) ||
                !File.Exists(samplesPath))
            {
                throw new FileNotFoundException(
                    "Foot Motion diagnostic samples file is unavailable.",
                    samplesPath);
            }
            string fullSamplesPath = Path.GetFullPath(samplesPath);
            if (!string.Equals(
                    Path.GetFileName(fullSamplesPath),
                    "samples.csv",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Foot Motion diagnostic input must be a sealed samples.csv file.");
            }
            string geometryPath = Path.Combine(
                Path.GetDirectoryName(fullSamplesPath) ?? string.Empty,
                GeometryFileName);
            if (!File.Exists(geometryPath))
            {
                throw new FileNotFoundException(
                    "Foot Motion ground path geometry file is unavailable.",
                    geometryPath);
            }
            CsvCapture capture = ReadCapture(fullSamplesPath, geometryPath);
            var events = new List<EventFact>(256);
            AnalyzeSide(capture.Left, events);
            AnalyzeSide(capture.Right, events);
            AnalyzeSupportChanges(capture, events);
            events.Sort(EventFact.Compare);
            FactsDocument document = BuildDocument(
                fullSamplesPath,
                geometryPath,
                capture,
                events);
            string factsPath = Path.Combine(
                Path.GetDirectoryName(fullSamplesPath) ?? string.Empty,
                "facts.json");
            PublishFacts(factsPath, document);
            CharacterFootDiagnosisPublication publication =
                CharacterFootDiagnosisPublisher.Publish(factsPath);
            string primarySummary = publication.FormatPrimarySummary();
            string summary =
                primarySummary +
                $"frames={capture.UniqueFrameCount} footRows={capture.FootRows.Count} " +
                $"geometryRows={capture.GeometryRowCount} " +
                $"landingEvents={document.coverage.landingEventCount} " +
                $"landingStateBoundaries={document.coverage.landingStateBoundaryCount} " +
                $"landingStateSpans={document.coverage.landingStateSpanCount} " +
                $"lockedEvents={document.coverage.lockedEventCount} " +
                $"releaseEvents={document.coverage.releaseEventCount} " +
                $"pathChanges={document.coverage.pathChangeCount} " +
                $"pathContinuityEvents={document.coverage.pathContinuityEventCount} " +
                $"supportChanges={document.coverage.supportChangeCount} " +
                $"penetrationEvents={document.coverage.contactPlanePenetrationEventCount} " +
                $"diagnosisFiles={publication.DiagnosticCount} " +
                $"diagnosisTargets={publication.TargetCount} " +
                $"diagnosisMatches={publication.MatchCount}";
            return new CharacterFootMotionDiagnosticAnalysis(
                fullSamplesPath,
                geometryPath,
                factsPath,
                publication.Directory,
                capture.UniqueFrameCount,
                capture.FootRows.Count,
                events.Count,
                publication.TargetCount,
                publication.MatchCount,
                summary);
        }

        static void AnalyzeSide(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            if (frames.Count == 0)
                return;
            AnalyzeLandingEvents(frames, events);
            AnalyzeLandingStateConsistency(frames, events);
            AnalyzeLockedEvents(frames, events);
            AnalyzeContactPlanePenetration(frames, events);
            AnalyzeReleaseEvents(frames, events);
            AnalyzePathChanges(frames, events);
            AnalyzePathContinuity(frames, events);
        }

        static void AnalyzeLandingEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    previous.FormalLockMode != "Unlocked" ||
                    current.FormalLockMode != "Sliding" ||
                    current.FormalStepTime > TimeEpsilon)
                {
                    continue;
                }
                int end = i;
                while (end + 1 < frames.Count &&
                       Continuous(frames[end], frames[end + 1]) &&
                       frames[end + 1].FormalLockMode != "Unlocked")
                {
                    end++;
                    if (frames[end].FormalLockMode == "Locked")
                        break;
                }
                IReadOnlyList<FootFrame> window = frames.GetRange(
                    Math.Max(0, i - 1),
                    end - Math.Max(0, i - 1) + 1);
                double correctionStep = MaximumCorrectionStep(window);
                double targetExtensionPeak = window.Max(frame => frame.TargetExtensionRatio);
                double solvedExtensionPeak = window.Max(frame => frame.SolvedExtensionRatio);
                double bendMinimum = window.Min(frame => frame.SolvedBendDegrees);
                double compressionMinimum = window.Min(frame => frame.TargetCompressionReserve);
                double bendDirectionMinimum = window.Min(frame => frame.BendDirectionPreviousDot);
                double targetExtensionDelta =
                    targetExtensionPeak - previous.TargetExtensionRatio;
                double bendDrop = previous.SolvedBendDegrees - bendMinimum;
                var fact = new EventFact(
                    "Landing",
                    current.Side,
                    current.Frame,
                    frames[end].Frame,
                    PeakCorrectionFrame(window),
                    current.FootMotionEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    Duration(window),
                    new SortedDictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["bendDirectionPreviousDotMinimum"] = bendDirectionMinimum,
                        ["compressionReserveMinimumMeters"] = compressionMinimum,
                        ["correctionStepMaximumMeters"] = correctionStep,
                        ["solvedBendDegreesMinimum"] = bendMinimum,
                        ["solvedBendDropDegrees"] = bendDrop,
                        ["solvedExtensionRatioPeak"] = solvedExtensionPeak,
                        ["targetExtensionRatioBaseline"] = previous.TargetExtensionRatio,
                        ["targetExtensionRatioDelta"] = targetExtensionDelta,
                        ["targetExtensionRatioPeak"] = targetExtensionPeak
                    },
                    new SortedDictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["bendDirectionReversed"] = bendDirectionMinimum < 0d,
                        ["contactAnchorAvailable"] = window.Any(frame => frame.HasAnchor),
                        ["grounded"] = current.Grounded
                    });
                events.Add(fact);
                i = Math.Max(i, end - 1);
            }
        }

        static void AnalyzeLandingStateConsistency(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    !FormalLandingBoundary(previous, current))
                {
                    continue;
                }
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["formalStepTimeSeconds"] = current.FormalStepTime,
                    ["correctionStepMeters"] = Vector3.Distance(
                        previous.EffectiveCorrection,
                        current.EffectiveCorrection),
                    ["finalSoleStepMeters"] = Vector3.Distance(
                        FinalSole(previous),
                        FinalSole(current))
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["runtimeLandingAtBoundary"] =
                        current.ConstraintState == "Landing",
                    ["runtimeLockedAtBoundary"] =
                        current.ConstraintState == "Locked",
                    ["runtimeSwingAtBoundary"] =
                        current.ConstraintState == "Swing",
                    ["runtimeUnlockedSupportAtBoundary"] =
                        current.ConstraintState == "UnlockedSupport",
                    ["runtimeReleasingAtBoundary"] =
                        current.ConstraintState == "Releasing",
                    ["contactPlaneAvailable"] = current.ContactPlaneAvailable
                };
                events.Add(new EventFact(
                    "LandingStateBoundary",
                    current.Side,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    current.FootMotionEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence));
            }

            int index = 0;
            while (index < frames.Count)
            {
                if (frames[index].ConstraintState != "Landing")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].FootMotionEventIdentity;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].ConstraintState == "Landing" &&
                       frames[index + 1].FootMotionEventIdentity == eventIdentity)
                {
                    index++;
                }
                int end = index;
                List<FootFrame> window = frames.GetRange(start, end - start + 1);
                bool hasEntry = start > 0 &&
                                Continuous(frames[start - 1], frames[start]);
                bool hasExit = end + 1 < frames.Count &&
                               Continuous(frames[end], frames[end + 1]);
                FootFrame entryPrevious = hasEntry ? frames[start - 1] : null;
                FootFrame exitNext = hasExit ? frames[end + 1] : null;
                double correctedEntryDistance = Vector3.Distance(
                    window[0].CorrectedSole,
                    window[0].Anchor);
                double correctedExitDistance = Vector3.Distance(
                    window[^1].CorrectedSole,
                    window[^1].Anchor);
                double finalEntryDistance = Vector3.Distance(
                    FinalSole(window[0]),
                    window[0].Anchor);
                double finalExitDistance = Vector3.Distance(
                    FinalSole(window[^1]),
                    window[^1].Anchor);
                double entryStep = hasEntry
                    ? Vector3.Distance(
                        entryPrevious.EffectiveCorrection,
                        window[0].EffectiveCorrection)
                    : 0d;
                double exitStep = hasExit
                    ? Vector3.Distance(
                        window[^1].EffectiveCorrection,
                        exitNext.EffectiveCorrection)
                    : 0d;
                int peakFrame = entryStep >= exitStep
                    ? window[0].Frame
                    : hasExit
                        ? exitNext.Frame
                        : window[^1].Frame;
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["frameCount"] = window.Count,
                    ["correctedSoleAnchorDistanceEntryMeters"] =
                        correctedEntryDistance,
                    ["correctedSoleAnchorDistanceExitMeters"] =
                        correctedExitDistance,
                    ["correctedSoleAnchorClosureMeters"] =
                        correctedEntryDistance - correctedExitDistance,
                    ["finalSoleAnchorDistanceEntryMeters"] = finalEntryDistance,
                    ["finalSoleAnchorDistanceExitMeters"] = finalExitDistance,
                    ["finalSoleAnchorClosureMeters"] =
                        finalEntryDistance - finalExitDistance,
                    ["entryCorrectionStepMeters"] = entryStep,
                    ["exitCorrectionStepMeters"] = exitStep,
                    ["formalUnlockedFrameCount"] = window.Count(
                        value => value.FormalLockMode == "Unlocked")
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["entryFollowedFormalBoundary"] = hasEntry &&
                        FormalLandingBoundary(entryPrevious, window[0]),
                    ["contactPlaneAvailableThroughout"] = window.All(
                        value => value.ContactPlaneAvailable),
                    ["closedTowardAnchor"] = correctedExitDistance +
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters <
                        correctedEntryDistance,
                    ["hasContinuousExit"] = hasExit,
                    ["exitedToLocked"] = hasExit &&
                        exitNext.ConstraintState == "Locked",
                    ["exitedToReleasing"] = hasExit &&
                        exitNext.ConstraintState == "Releasing",
                    ["exitedToSwing"] = hasExit &&
                        exitNext.ConstraintState == "Swing",
                    ["exitedToUnlockedSupport"] = hasExit &&
                        exitNext.ConstraintState == "UnlockedSupport",
                    ["formalUnlockedWithinLanding"] = window.Any(
                        value => value.FormalLockMode == "Unlocked")
                };
                events.Add(new EventFact(
                    "LandingStateSpan",
                    window[0].Side,
                    window[0].Frame,
                    window[^1].Frame,
                    peakFrame,
                    eventIdentity,
                    window[0].SourceIdentity,
                    window[0].SourceCycle,
                    Duration(window),
                    metrics,
                    evidence));
                index++;
            }
        }

        static bool FormalLandingBoundary(
            FootFrame previous,
            FootFrame current) =>
            previous.FormalLockMode == "Unlocked" &&
            current.FormalLockMode != "Unlocked";

        static Vector3 FinalSole(FootFrame frame) =>
            (frame.FinalHeel + frame.FinalToe) * 0.5f;

        static void AnalyzeLockedEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            int index = 0;
            while (index < frames.Count)
            {
                if (frames[index].ConstraintState != "Locked")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].FootMotionEventIdentity;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].ConstraintState == "Locked" &&
                       frames[index + 1].FootMotionEventIdentity == eventIdentity)
                {
                    index++;
                }
                int end = index;
                List<FootFrame> window = frames.GetRange(start, end - start + 1);
                double anchorDisplacement = VectorRange(
                    window.Select(frame => frame.Anchor));
                List<double> anchorDistances = window
                    .Select(frame => (double)Vector3.Distance(frame.CorrectedSole, frame.Anchor))
                    .ToList();
                List<double> alongUp = window
                    .Select(frame => (double)Vector3.Dot(
                        frame.CorrectedSole - frame.Anchor,
                        frame.ComponentUp.normalized))
                    .ToList();
                double sink = alongUp[0] - alongUp.Min();
                double drift = anchorDistances[^1] - anchorDistances[0];
                double visibleStep = MaximumVectorStep(
                    window.Select(frame => frame.CorrectedSole).ToList());
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["anchorDisplacementMeters"] = anchorDisplacement,
                    ["correctedSoleAnchorDistanceEntryMeters"] = anchorDistances[0],
                    ["correctedSoleAnchorDistanceExitMeters"] = anchorDistances[^1],
                    ["correctedSoleAnchorDistanceMaximumMeters"] = anchorDistances.Max(),
                    ["correctedSoleAnchorDistanceMinimumMeters"] = anchorDistances.Min(),
                    ["correctedSoleAnchorDistanceChangeMeters"] = drift,
                    ["lockWeightEntry"] = window[0].FormalLockWeight,
                    ["lockWeightExit"] = window[^1].FormalLockWeight,
                    ["lockWeightMinimum"] = window.Min(frame => frame.FormalLockWeight),
                    ["soleAlongUpEntryMeters"] = alongUp[0],
                    ["soleAlongUpMinimumMeters"] = alongUp.Min(),
                    ["soleDownwardExcursionMeters"] = sink,
                    ["supportEntry"] = window[0].FormalSupport,
                    ["supportExit"] = window[^1].FormalSupport,
                    ["visibleSoleStepMaximumMeters"] = visibleStep
                };
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["anchorStable"] = anchorDisplacement <= PositionNoiseFloor,
                    ["groundedThroughout"] = window.All(frame => frame.Grounded),
                    ["lockWeightDecreased"] = window[^1].FormalLockWeight < window[0].FormalLockWeight,
                    ["supportStayedPositive"] = window.All(frame => frame.FormalSupport > 0f)
                };
                EventFact fact = new EventFact(
                    "Locked",
                    window[0].Side,
                    window[0].Frame,
                    window[^1].Frame,
                    PeakDistanceFrame(window),
                    eventIdentity,
                    window[0].SourceIdentity,
                    window[0].SourceCycle,
                    Duration(window),
                    metrics,
                    evidence);
                events.Add(fact);
                index++;
            }
        }

        static void AnalyzeContactPlanePenetration(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            int index = 0;
            while (index < frames.Count)
            {
                if (!frames[index].PenetrationAvailable)
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].FootMotionEventIdentity;
                int surfaceIdentity = frames[index].ContactSurfaceIdentity;
                string constraintState = frames[index].ConstraintState;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].PenetrationAvailable &&
                       frames[index + 1].FootMotionEventIdentity == eventIdentity &&
                       frames[index + 1].ContactSurfaceIdentity == surfaceIdentity &&
                       frames[index + 1].ConstraintState == constraintState)
                {
                    index++;
                }
                int end = index;
                List<FootFrame> window = frames.GetRange(
                    start,
                    end - start + 1);
                var samples = new List<CharacterFootContactPlanePenetrationSample>(
                    window.Count);
                var finalDepths = new List<double>(window.Count);
                double duration = 0d;
                double coefficientTime = 0d;
                double depthTime = 0d;
                int penetratingFrames = 0;
                int sourcePenetratingFrames = 0;
                int introducedFrames = 0;
                int amplifiedFrames = 0;
                int partiallyResolvedFrames = 0;
                int resolvedFrames = 0;
                int baselineResidualFrames = 0;
                int heelOnlyFrames = 0;
                int toeOnlyFrames = 0;
                int bothFrames = 0;
                int peakFrame = window[0].Frame;
                double peakDepth = -1d;
                for (int i = 0; i < window.Count; i++)
                {
                    FootFrame frame = window[i];
                    CharacterFootContactPlanePenetrationSample sample =
                        EvaluatePenetration(frame);
                    samples.Add(sample);
                    finalDepths.Add(sample.Final.MaximumDepth);
                    double dt = DeltaSeconds(frame);
                    duration += dt;
                    coefficientTime += sample.Final.LengthCoefficient * dt;
                    depthTime += sample.Final.MeanDepth * dt;
                    bool heelPenetrating = sample.Final.HeelDepth >
                                           CharacterFootContactPlanePenetration
                                               .GeometryEpsilonMeters;
                    bool toePenetrating = sample.Final.ToeDepth >
                                          CharacterFootContactPlanePenetration
                                              .GeometryEpsilonMeters;
                    if (heelPenetrating || toePenetrating)
                        penetratingFrames++;
                    if (sample.Source.MaximumDepth >
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters)
                    {
                        sourcePenetratingFrames++;
                    }
                    if (sample.IntroducedMaximumDepth >
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters)
                    {
                        introducedFrames++;
                    }
                    amplifiedFrames += ResponsibilityPresent(
                        sample,
                        CharacterFootContactPlanePenetrationResponsibility
                            .Amplified)
                        ? 1
                        : 0;
                    partiallyResolvedFrames += ResponsibilityPresent(
                        sample,
                        CharacterFootContactPlanePenetrationResponsibility
                            .PartiallyResolved)
                        ? 1
                        : 0;
                    resolvedFrames += ResponsibilityPresent(
                        sample,
                        CharacterFootContactPlanePenetrationResponsibility
                            .Resolved)
                        ? 1
                        : 0;
                    baselineResidualFrames += ResponsibilityPresent(
                        sample,
                        CharacterFootContactPlanePenetrationResponsibility
                            .BaselineResidual)
                        ? 1
                        : 0;
                    if (heelPenetrating && toePenetrating)
                        bothFrames++;
                    else if (heelPenetrating)
                        heelOnlyFrames++;
                    else if (toePenetrating)
                        toeOnlyFrames++;
                    if (sample.Final.MaximumDepth <= peakDepth)
                        continue;
                    peakDepth = sample.Final.MaximumDepth;
                    peakFrame = frame.Frame;
                }
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["availableFrameCount"] = window.Count,
                    ["sourcePenetratingFrameCount"] = sourcePenetratingFrames,
                    ["finalPenetratingFrameCount"] = penetratingFrames,
                    ["finalPenetratingFrameRatio"] =
                        window.Count > 0 ? (double)penetratingFrames / window.Count : 0d,
                    ["sourceHeelDepthMaximumMeters"] = samples.Max(value => value.Source.HeelDepth),
                    ["sourceToeDepthMaximumMeters"] = samples.Max(value => value.Source.ToeDepth),
                    ["sourceDepthMaximumMeters"] = samples.Max(value => value.Source.MaximumDepth),
                    ["sourceLengthCoefficientMaximum"] = samples.Max(value => value.Source.LengthCoefficient),
                    ["finalHeelDepthMaximumMeters"] = samples.Max(value => value.Final.HeelDepth),
                    ["finalToeDepthMaximumMeters"] = samples.Max(value => value.Final.ToeDepth),
                    ["finalDepthMaximumMeters"] = samples.Max(value => value.Final.MaximumDepth),
                    ["finalDepthMedianMeters"] = Percentile(finalDepths, 0.5d),
                    ["finalDepthP90Meters"] = Percentile(finalDepths, 0.9d),
                    ["finalDepthP99Meters"] = Percentile(finalDepths, 0.99d),
                    ["finalMeanDepthMaximumMeters"] = samples.Max(value => value.Final.MeanDepth),
                    ["finalLengthCoefficientMaximum"] = samples.Max(value => value.Final.LengthCoefficient),
                    ["finalLengthCoefficientDurationMean"] =
                        duration > 0d ? coefficientTime / duration : 0d,
                    ["finalDepthTimeIntegralMeterSeconds"] = depthTime,
                    ["introducedDepthMaximumMeters"] = samples.Max(value => value.IntroducedMaximumDepth),
                    ["resolvedDepthMaximumMeters"] = samples.Max(value => value.ResolvedMaximumDepth),
                    ["introducedFrameCount"] = introducedFrames,
                    ["amplifiedFrameCount"] = amplifiedFrames,
                    ["partiallyResolvedFrameCount"] = partiallyResolvedFrames,
                    ["resolvedFrameCount"] = resolvedFrames,
                    ["baselineResidualFrameCount"] = baselineResidualFrames,
                    ["heelOnlyFrameCount"] = heelOnlyFrames,
                    ["toeOnlyFrameCount"] = toeOnlyFrames,
                    ["bothFrameCount"] = bothFrames,
                    ["contactSurfaceIdentity"] = surfaceIdentity
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["sourcePenetrated"] = sourcePenetratingFrames > 0,
                    ["finalPenetrated"] = penetratingFrames > 0,
                    ["introduced"] = introducedFrames > 0,
                    ["amplified"] = amplifiedFrames > 0,
                    ["partiallyResolved"] = partiallyResolvedFrames > 0,
                    ["resolved"] = resolvedFrames > 0,
                    ["baselineResidual"] = baselineResidualFrames > 0,
                    ["heelResidual"] = samples.Any(value =>
                        value.Final.HeelDepth >
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters),
                    ["toeResidual"] = samples.Any(value =>
                        value.Final.ToeDepth >
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters)
                };
                events.Add(new EventFact(
                    "ContactPlanePenetration",
                    window[0].Side,
                    window[0].Frame,
                    window[^1].Frame,
                    peakFrame,
                    eventIdentity,
                    window[0].SourceIdentity,
                    window[0].SourceCycle,
                    duration,
                    metrics,
                    evidence));
                index++;
            }
        }

        static CharacterFootContactPlanePenetrationSample EvaluatePenetration(
            FootFrame frame)
        {
            Vector3 normal = frame.ContactNormal.normalized;
            double sourceHeelClearance = Vector3.Dot(
                frame.SourceHeel - frame.Anchor,
                normal);
            double sourceToeClearance = Vector3.Dot(
                frame.SourceToe - frame.Anchor,
                normal);
            double finalHeelClearance = Vector3.Dot(
                frame.FinalHeel - frame.Anchor,
                normal);
            double finalToeClearance = Vector3.Dot(
                frame.FinalToe - frame.Anchor,
                normal);
            return CharacterFootContactPlanePenetration.Evaluate(
                sourceHeelClearance,
                sourceToeClearance,
                finalHeelClearance,
                finalToeClearance);
        }

        static bool ResponsibilityPresent(
            CharacterFootContactPlanePenetrationSample sample,
            CharacterFootContactPlanePenetrationResponsibility responsibility) =>
            sample.HeelResponsibility == responsibility ||
            sample.ToeResponsibility == responsibility;

        static void AnalyzeReleaseEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            int index = 0;
            while (index < frames.Count)
            {
                if (frames[index].ConstraintState != "Releasing")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].FootMotionEventIdentity;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].ConstraintState == "Releasing" &&
                       frames[index + 1].FootMotionEventIdentity == eventIdentity)
                {
                    index++;
                }
                int end = index;
                List<FootFrame> window = frames.GetRange(start, end - start + 1);
                double correctionStep = MaximumCorrectionStep(window);
                double excursion = VectorRange(
                    window.Select(frame => frame.EffectiveCorrection));
                int reversals = VelocityReversalCount(
                    window.Select(frame => frame.EffectiveCorrection).ToList());
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["correctionExcursionMeters"] = excursion,
                    ["correctionStepMaximumMeters"] = correctionStep,
                    ["velocityDirectionReversalCount"] = reversals
                };
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["anchorAvailable"] = window.Any(frame => frame.HasAnchor),
                    ["groundedThroughout"] = window.All(frame => frame.Grounded),
                    ["pathChanged"] = HasPathChange(window)
                };
                EventFact fact = new EventFact(
                    "Release",
                    window[0].Side,
                    window[0].Frame,
                    window[^1].Frame,
                    PeakCorrectionFrame(window),
                    eventIdentity,
                    window[0].SourceIdentity,
                    window[0].SourceCycle,
                    Duration(window),
                    metrics,
                    evidence);
                events.Add(fact);
                index++;
            }
        }

        static void AnalyzePathChanges(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            int i = 1;
            while (i < frames.Count)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    !SemanticPathChanged(previous, current))
                {
                    i++;
                    continue;
                }
                int changeStart = i;
                int changeEnd = i;
                while (changeEnd + 1 < frames.Count &&
                       Continuous(frames[changeEnd], frames[changeEnd + 1]) &&
                       SemanticPathChanged(
                           frames[changeEnd],
                           frames[changeEnd + 1]))
                {
                    changeEnd++;
                }
                FootFrame beforeChange = frames[changeStart - 1];
                FootFrame afterChange = frames[changeEnd];
                double endpointDelta = Vector3.Distance(
                    beforeChange.NextLanding,
                    afterChange.NextLanding);
                bool inputChanged = false;
                bool eventChanged = false;
                bool stateChanged = false;
                for (int change = changeStart; change <= changeEnd; change++)
                {
                    FootFrame before = frames[change - 1];
                    FootFrame after = frames[change];
                    inputChanged |= before.GroundPathInputIdentity !=
                                    after.GroundPathInputIdentity;
                    eventChanged |= before.NextLandingEventIdentity !=
                                    after.NextLandingEventIdentity;
                    stateChanged |= before.GroundPathState !=
                                    after.GroundPathState;
                }
                int windowStart = Math.Max(0, changeStart - 3);
                int windowEnd = Math.Min(frames.Count - 1, changeEnd + 8);
                while (windowStart < changeStart &&
                       !Continuous(frames[windowStart], frames[windowStart + 1]))
                {
                    windowStart++;
                }
                while (windowEnd > changeEnd &&
                       !Continuous(frames[windowEnd - 1], frames[windowEnd]))
                {
                    windowEnd--;
                }
                List<FootFrame> window = frames.GetRange(
                    windowStart,
                    windowEnd - windowStart + 1);
                bool unanchoredSwingEligible = HasUnanchoredSwingPair(window);
                double correctionStep = MaximumCorrectionStep(
                    window,
                    true,
                    true);
                double correctionExcursion = MaximumUnanchoredCorrectionRange(window);
                double jerk = MaximumCorrectionJerk(window, true);
                int peakFrame = PeakCorrectionFrame(window, true, true);
                CharacterFootPathStageAnalysis pathStageAnalysis =
                    unanchoredSwingEligible
                        ? AnalyzePathStages(window, peakFrame)
                        : CharacterFootPathStageAnalysis.Unavailable(
                            "UnanchoredSwingPairUnavailable",
                            beforeChange.Frame,
                            afterChange.Frame,
                            afterChange.Side,
                            ResolveEventIdentity(afterChange).ToString(
                                CultureInfo.InvariantCulture),
                            afterChange.SourceIdentity);
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["correctionExcursionMeters"] = correctionExcursion,
                    ["correctionJerkMetersPerSecondCubed"] = jerk,
                    ["correctionStepMaximumMeters"] = correctionStep,
                    ["nextLandingEndpointDeltaMeters"] = endpointDelta
                };
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["anchorAvailable"] = afterChange.HasAnchor,
                    ["groundPathAcceptedAfter"] = afterChange.GroundPathState == "Accepted",
                    ["groundPathAcceptedBefore"] = beforeChange.GroundPathState == "Accepted",
                    ["pathEventChanged"] = eventChanged,
                    ["pathInputChanged"] = inputChanged,
                    ["pathStageAnalysisAvailable"] =
                        pathStageAnalysis.available,
                    ["pathStateChanged"] = stateChanged,
                    ["sourceChanged"] = beforeChange.SourceIdentity != afterChange.SourceIdentity,
                    ["unanchoredSwingEligible"] = unanchoredSwingEligible
                };
                EventFact fact = new EventFact(
                    "PathChange",
                    afterChange.Side,
                    beforeChange.Frame,
                    afterChange.Frame,
                    peakFrame,
                    afterChange.NextLandingEventIdentity,
                    afterChange.SourceIdentity,
                    afterChange.SourceCycle,
                    Duration(frames.GetRange(
                        changeStart,
                        changeEnd - changeStart + 1)),
                    metrics,
                    evidence,
                    pathStageAnalysis);
                events.Add(fact);
                i = changeEnd + 1;
            }
        }

        static CharacterFootPathStageAnalysis AnalyzePathStages(
            List<FootFrame> window,
            int peakFrame)
        {
            for (int i = 1; i < window.Count; i++)
            {
                FootFrame previous = window[i - 1];
                FootFrame current = window[i];
                if (current.Frame != peakFrame ||
                    !Continuous(previous, current) ||
                    previous.HasAnchor ||
                    current.HasAnchor ||
                    previous.ConstraintState != "Swing" ||
                    current.ConstraintState != "Swing")
                {
                    continue;
                }
                return BuildPathStageAnalysis(previous, current);
            }
            FootFrame first = window.Count > 0 ? window[0] : null;
            FootFrame last = window.Count > 0 ? window[^1] : null;
            return CharacterFootPathStageAnalysis.Unavailable(
                "PeakCorrectionPairUnavailable",
                first?.Frame ?? 0,
                last?.Frame ?? 0,
                last?.Side ?? string.Empty,
                ResolveEventIdentity(last).ToString(CultureInfo.InvariantCulture),
                last?.SourceIdentity ?? string.Empty);
        }

        static CharacterFootPathStageAnalysis BuildPathStageAnalysis(
            FootFrame previous,
            FootFrame current)
        {
            var missing = new List<string>();
            var stages = new List<CharacterFootPathStageDelta>(
                CharacterFootPathStageNames.All.Length)
            {
                Stage(
                    CharacterFootPathStageNames.RawLandingToPathTarget,
                    previous.RawLandingAvailable && current.RawLandingAvailable &&
                    previous.GroundPathTargetAvailable &&
                    current.GroundPathTargetAvailable,
                    "RawLandingOrPathTargetUnavailable",
                    previous.RawLanding,
                    current.RawLanding,
                    previous.NextLanding,
                    current.NextLanding,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.RawLandingAvailable || current.RawLandingAvailable ||
                    previous.GroundPathTargetAvailable ||
                    current.GroundPathTargetAvailable),
                Stage(
                    CharacterFootPathStageNames.PathTargetToSwingTarget,
                    previous.GroundPathTargetAvailable &&
                    current.GroundPathTargetAvailable &&
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated &&
                    previous.PathAvailableAfter && current.PathAvailableAfter,
                    "PathTargetOrSwingTargetUnavailable",
                    previous.NextLanding,
                    current.NextLanding,
                    previous.PathCurrentTargetCorrection,
                    current.PathCurrentTargetCorrection,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.PathAvailableAfter || current.PathAvailableAfter),
                Stage(
                    CharacterFootPathStageNames.SwingTargetToCapturedResidual,
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated &&
                    previous.PathAvailableAfter && current.PathAvailableAfter,
                    "SwingTargetOrCapturedResidualUnavailable",
                    previous.PathCurrentTargetCorrection,
                    current.PathCurrentTargetCorrection,
                    previous.SwingResidualBeforeSchedule,
                    current.SwingResidualBeforeSchedule,
                    previous.Frame,
                    current.Frame,
                    missing,
                    current.PathResidualRebuilt),
                Stage(
                    CharacterFootPathStageNames.CapturedResidualToScheduledResidual,
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated,
                    "ResidualScheduleFactsUnavailable",
                    previous.SwingResidualBeforeSchedule,
                    current.SwingResidualBeforeSchedule,
                    previous.SwingResidualAfterSchedule,
                    current.SwingResidualAfterSchedule,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.PathContinuityEvaluated ||
                    current.PathContinuityEvaluated),
                Stage(
                    CharacterFootPathStageNames.ResidualOutputToStateOutput,
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated &&
                    previous.OutputStagesAvailable &&
                    current.OutputStagesAvailable,
                    "ResidualOrStateOutputUnavailable",
                    previous.ResidualOutputCorrection,
                    current.ResidualOutputCorrection,
                    previous.CorrectionBeforeSafetyFloor,
                    current.CorrectionBeforeSafetyFloor,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.OutputStagesAvailable ||
                    current.OutputStagesAvailable),
                Stage(
                    CharacterFootPathStageNames.StateOutputToSafetyFloorOutput,
                    previous.OutputStagesAvailable &&
                    current.OutputStagesAvailable &&
                    previous.EnvelopeAvailable && current.EnvelopeAvailable,
                    "StateOutputOrGroundEnvelopeUnavailable",
                    previous.CorrectionBeforeSafetyFloor,
                    current.CorrectionBeforeSafetyFloor,
                    previous.SafetyFloorOutputCorrection,
                    current.SafetyFloorOutputCorrection,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.EnvelopeAvailable || current.EnvelopeAvailable ||
                    previous.SafetyFloorClamped || current.SafetyFloorClamped),
                Stage(
                    CharacterFootPathStageNames.FinalCorrectionToEncodedGoal,
                    previous.OutputStagesAvailable &&
                    current.OutputStagesAvailable &&
                    previous.EncodedGoalAvailable && current.EncodedGoalAvailable,
                    "FinalCorrectionOrEncodedGoalUnavailable",
                    previous.FinalEffectiveCorrection,
                    current.FinalEffectiveCorrection,
                    previous.EncodedGoalCorrection,
                    current.EncodedGoalCorrection,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.EncodedGoalAvailable || current.EncodedGoalAvailable),
                Stage(
                    CharacterFootPathStageNames.EncodedGoalToSolvedFoot,
                    previous.EncodedGoalAvailable && current.EncodedGoalAvailable &&
                    previous.FinalIkEffectorAvailable &&
                    current.FinalIkEffectorAvailable,
                    "EncodedGoalOrSolvedFootUnavailable",
                    previous.EncodedGoalPosition,
                    current.EncodedGoalPosition,
                    previous.FinalIkSolvedPosition,
                    current.FinalIkSolvedPosition,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.FinalIkEffectorAvailable ||
                    current.FinalIkEffectorAvailable)
            };
            var stateEvidence = new CharacterFootPathStageStateEvidence
            {
                previousState = previous.ConstraintState,
                stateBefore = current.ConstraintStateBefore,
                stateAfter = current.ConstraintState,
                previousLockResponse = previous.LockResponse,
                lockResponseBefore = current.LockResponseBefore,
                lockResponseAfter = current.LockResponse,
                revisionReason = current.PathRevisionReason,
                residualRebuilt = current.PathResidualRebuilt,
                safetyFloorClamped = current.SafetyFloorClamped
            };
            bool available = missing.Count == 0;
            CharacterFootPathFirstAmplification first = available
                ? ResolveFirstAmplification(stages, stateEvidence)
                : new CharacterFootPathFirstAmplification
                {
                    available = false,
                    unavailableReason = "RequiredStageUnavailable",
                    previousFrame = previous.Frame,
                    frame = current.Frame,
                    stateEvidence = stateEvidence
                };
            var analysis = new CharacterFootPathStageAnalysis
            {
                available = available,
                unavailableReason = available
                    ? string.Empty
                    : "RequiredStageUnavailable",
                amplificationNoiseFloorMeters = PositionNoiseFloor,
                lineage = new CharacterFootPathStageLineage
                {
                    previousFrame = previous.Frame,
                    frame = current.Frame,
                    previousCompletionIdentity =
                        previous.CompletionIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    completionIdentity = current.CompletionIdentity.ToString(
                        CultureInfo.InvariantCulture),
                    side = current.Side,
                    previousEventIdentity = ResolveEventIdentity(previous).ToString(
                        CultureInfo.InvariantCulture),
                    eventIdentity = ResolveEventIdentity(current).ToString(
                        CultureInfo.InvariantCulture),
                    previousSourceIdentity = previous.SourceIdentity,
                    sourceIdentity = current.SourceIdentity,
                    previousSourceCycle = previous.SourceCycle,
                    sourceCycle = current.SourceCycle,
                    previousPathInputIdentity =
                        previous.FootMotionGroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    pathInputIdentity =
                        current.FootMotionGroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture)
                },
                stateEvidence = stateEvidence,
                stageFacts = new CharacterFootPathStageFacts
                {
                    residualCaptureAvailable = current.PathResidualRebuilt,
                    residualBeforeRevisionPrevious = StageVector(
                        previous.SwingResidualBeforeRevision),
                    residualBeforeRevision = StageVector(
                        current.SwingResidualBeforeRevision),
                    capturedResidualPrevious = StageVector(
                        previous.SwingResidualBeforeSchedule),
                    capturedResidual = StageVector(
                        current.SwingResidualBeforeSchedule),
                    groundEnvelopeSafetyCorrectionAvailable =
                        previous.EnvelopeAvailable && current.EnvelopeAvailable,
                    groundEnvelopeSafetyCorrectionPrevious = StageVector(
                        previous.GroundEnvelopeSafetyCorrection),
                    groundEnvelopeSafetyCorrection = StageVector(
                        current.GroundEnvelopeSafetyCorrection),
                    physicalFootAvailable =
                        previous.FinalPhysicalWriteAvailable &&
                        current.FinalPhysicalWriteAvailable,
                    physicalFootPrevious = StageVector(
                        previous.FinalPhysicalAnkleComponentPosition),
                    physicalFoot = StageVector(
                        current.FinalPhysicalAnkleComponentPosition)
                },
                missingStages = missing,
                stages = stages,
                firstAmplificationStage = first
            };
            analysis.RequireValid();
            return analysis;
        }

        static CharacterFootPathStageDelta Stage(
            string name,
            bool available,
            string unavailableReason,
            Vector3 inputBefore,
            Vector3 inputAfter,
            Vector3 outputBefore,
            Vector3 outputAfter,
            int previousFrame,
            int frame,
            List<string> missing,
            bool applicable)
        {
            if (!applicable)
            {
                return new CharacterFootPathStageDelta
                {
                    stage = name,
                    applicable = false,
                    available = false,
                    unavailableReason = "NotApplicable",
                    previousFrame = previousFrame,
                    frame = frame
                };
            }
            if (!available)
            {
                missing.Add(name);
                return new CharacterFootPathStageDelta
                {
                    stage = name,
                    applicable = true,
                    available = false,
                    unavailableReason = unavailableReason,
                    previousFrame = previousFrame,
                    frame = frame
                };
            }
            double inputDelta = Vector3.Distance(inputBefore, inputAfter);
            double outputDelta = Vector3.Distance(outputBefore, outputAfter);
            double amplification = outputDelta - inputDelta;
            bool ratioAvailable = inputDelta > PositionNoiseFloor;
            return new CharacterFootPathStageDelta
            {
                stage = name,
                applicable = true,
                available = true,
                previousFrame = previousFrame,
                frame = frame,
                inputBefore = StageVector(inputBefore),
                inputAfter = StageVector(inputAfter),
                outputBefore = StageVector(outputBefore),
                outputAfter = StageVector(outputAfter),
                inputDeltaMeters = inputDelta,
                outputDeltaMeters = outputDelta,
                amplificationMeters = amplification,
                amplificationRatioAvailable = ratioAvailable,
                amplificationRatio = ratioAvailable
                    ? outputDelta / inputDelta
                    : null
            };
        }

        static CharacterFootPathStageVector3 StageVector(Vector3 value) =>
            new CharacterFootPathStageVector3
            {
                x = value.x,
                y = value.y,
                z = value.z
            };

        static CharacterFootPathFirstAmplification ResolveFirstAmplification(
            List<CharacterFootPathStageDelta> stages,
            CharacterFootPathStageStateEvidence stateEvidence)
        {
            for (int i = 0; i < stages.Count; i++)
            {
                CharacterFootPathStageDelta stage = stages[i];
                if (!stage.applicable ||
                    !stage.available ||
                    stage.amplificationMeters <= PositionNoiseFloor)
                    continue;
                return new CharacterFootPathFirstAmplification
                {
                    available = true,
                    stage = stage.stage,
                    previousFrame = stage.previousFrame,
                    frame = stage.frame,
                    inputDeltaMeters = stage.inputDeltaMeters,
                    outputDeltaMeters = stage.outputDeltaMeters,
                    amplificationMeters = stage.amplificationMeters,
                    amplificationRatioAvailable =
                        stage.amplificationRatioAvailable,
                    amplificationRatio = stage.amplificationRatio,
                    stateEvidence = stateEvidence
                };
            }
            CharacterFootPathStageDelta last = stages[^1];
            return new CharacterFootPathFirstAmplification
            {
                available = false,
                unavailableReason = "NoAmplificationAboveNoiseFloor",
                previousFrame = last.previousFrame,
                frame = last.frame,
                stateEvidence = stateEvidence
            };
        }

        static ulong ResolveEventIdentity(FootFrame frame)
        {
            if (frame == null)
                return 0;
            if (frame.PathCurrentLandingEventIdentity != 0)
                return frame.PathCurrentLandingEventIdentity;
            if (frame.FootMotionEventIdentity != 0)
                return frame.FootMotionEventIdentity;
            return frame.NextLandingEventIdentity;
        }

        static void AnalyzePathContinuity(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame current = frames[i];
                FootFrame previous = i > 0 ? frames[i - 1] : null;
                bool continuous = previous != null && Continuous(previous, current);
                bool inputIdentityChanged = continuous &&
                    previous.FootMotionGroundPathInputIdentity !=
                    current.FootMotionGroundPathInputIdentity;
                bool availabilityChanged =
                    current.PathAvailableBefore != current.PathAvailableAfter;
                bool comparablePath = current.PathAvailableBefore &&
                                      current.PathAvailableAfter;
                bool eventChanged = comparablePath &&
                    current.PathPreviousLandingEventIdentity !=
                    current.PathCurrentLandingEventIdentity;
                bool landingPointChanged = comparablePath &&
                    current.PathLandingPointDelta > current.LandingUpdateDistance;
                bool swingTargetChanged = comparablePath &&
                    current.PathTargetDelta > current.LandingUpdateDistance;
                bool revisionExpected = availabilityChanged || eventChanged ||
                                        landingPointChanged || swingTargetChanged;
                bool reasonAvailability = HasRevisionReason(
                    current.PathRevisionReason,
                    "PathAvailabilityChanged");
                bool reasonEvent = HasRevisionReason(
                    current.PathRevisionReason,
                    "LandingEventChanged");
                bool reasonLandingPoint = HasRevisionReason(
                    current.PathRevisionReason,
                    "LandingPointChanged");
                bool reasonSwingTarget = HasRevisionReason(
                    current.PathRevisionReason,
                    "SwingTargetChanged");
                bool reasonAvailable = reasonAvailability || reasonEvent ||
                                       reasonLandingPoint || reasonSwingTarget;
                bool reasonMatchesExpected =
                    reasonAvailability == availabilityChanged &&
                    reasonEvent == eventChanged &&
                    reasonLandingPoint == landingPointChanged &&
                    reasonSwingTarget == swingTargetChanged;
                double residualBeforeRevision =
                    current.SwingResidualBeforeRevision.magnitude;
                double residualBeforeSchedule =
                    current.SwingResidualBeforeSchedule.magnitude;
                double residualAfterSchedule =
                    current.SwingResidualAfterSchedule.magnitude;
                bool residualGrewWithoutRevision =
                    current.PathContinuityEvaluated &&
                    !current.PathResidualRebuilt &&
                    residualAfterSchedule > residualBeforeSchedule + PositionNoiseFloor;
                bool deadlineUnavailable =
                    current.ResidualDeadlineOutcome == "Unavailable";
                bool deadlineReached = current.PathContinuityEvaluated &&
                    current.ResidualDeadlineOutcome != "NotEvaluated" &&
                    current.ResidualTimeToLandingSeconds <=
                    DeltaSeconds(current) + TimeEpsilon;
                bool identityOnlyInputChange = inputIdentityChanged &&
                    current.PathContinuityEvaluated &&
                    !revisionExpected;
                bool relevant = inputIdentityChanged ||
                                current.PathResidualRebuilt ||
                                revisionExpected ||
                                reasonAvailable ||
                                current.ReleasingCompletedToSwing ||
                                current.SafetyFloorClamped ||
                                deadlineReached ||
                                deadlineUnavailable ||
                                residualGrewWithoutRevision;
                if (!relevant)
                    continue;
                double correctionStep = continuous
                    ? Vector3.Distance(
                        previous.EffectiveCorrection,
                        current.EffectiveCorrection)
                    : 0d;
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["appliedStepMeters"] =
                        current.ResidualAppliedStepMeters,
                    ["baseStepMeters"] = current.ResidualBaseStepMeters,
                    ["baseHalfLifeSeconds"] =
                        current.ResidualBaseHalfLifeSeconds,
                    ["correctionStepMeters"] = correctionStep,
                    ["deadlineRequiredStepMeters"] =
                        current.ResidualDeadlineRequiredStepMeters,
                    ["envelopeClearanceAfterMeters"] =
                        current.EnvelopeClearanceAfterMeters,
                    ["envelopeClearanceBeforeMeters"] =
                        current.EnvelopeClearanceBeforeMeters,
                    ["landingPointDeltaMeters"] =
                        current.PathLandingPointDelta,
                    ["landingUpdateDistanceMeters"] =
                        current.LandingUpdateDistance,
                    ["residualAfterScheduleMeters"] = residualAfterSchedule,
                    ["residualBeforeScheduleMeters"] = residualBeforeSchedule,
                    ["residualBeforeRevisionMeters"] = residualBeforeRevision,
                    ["residualRemainingDistanceMeters"] =
                        current.ResidualRemainingDistanceMeters,
                    ["safetyFloorClampMeters"] =
                        current.SafetyFloorClampMeters,
                    ["swingTargetDeltaMeters"] = current.PathTargetDelta,
                    ["timeToLandingSeconds"] =
                        current.ResidualTimeToLandingSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["deadlineScheduled"] =
                        current.ResidualDeadlineOutcome == "Scheduled",
                    ["deadlineUnavailable"] =
                        deadlineUnavailable,
                    ["deadlineWithinTolerance"] =
                        current.ResidualDeadlineOutcome == "WithinTolerance",
                    ["deadlineReached"] = deadlineReached,
                    ["envelopeAvailable"] = current.EnvelopeAvailable,
                    ["expectedLandingEventRevision"] = eventChanged,
                    ["expectedLandingPointRevision"] = landingPointChanged,
                    ["expectedPathAvailabilityRevision"] = availabilityChanged,
                    ["expectedSwingTargetRevision"] = swingTargetChanged,
                    ["identityOnlyInputChange"] = identityOnlyInputChange,
                    ["pathContinuityEvaluated"] =
                        current.PathContinuityEvaluated,
                    ["pathInputIdentityChanged"] = inputIdentityChanged,
                    ["pathResidualRebuilt"] = current.PathResidualRebuilt,
                    ["pathRevisionExpected"] = revisionExpected,
                    ["pathRevisionReasonMatchesExpected"] =
                        reasonMatchesExpected,
                    ["reasonLandingEventChanged"] = reasonEvent,
                    ["reasonLandingPointChanged"] = reasonLandingPoint,
                    ["reasonPathAvailabilityChanged"] = reasonAvailability,
                    ["reasonSwingTargetChanged"] = reasonSwingTarget,
                    ["releasingCompletedToSwing"] =
                        current.ReleasingCompletedToSwing,
                    ["residualGrewWithoutRevision"] =
                        residualGrewWithoutRevision,
                    ["safetyFloorClamped"] = current.SafetyFloorClamped,
                    ["stateAfterSwing"] =
                        current.ConstraintState == "Swing",
                    ["stateBeforeReleasing"] =
                        current.ConstraintStateBefore == "Releasing"
                };
                events.Add(new EventFact(
                    "PathContinuity",
                    current.Side,
                    continuous ? previous.Frame : current.Frame,
                    current.Frame,
                    current.Frame,
                    current.PathCurrentLandingEventIdentity != 0
                        ? current.PathCurrentLandingEventIdentity
                        : current.FootMotionEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence));
            }
        }

        static bool HasRevisionReason(string value, string reason)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            string[] values = value.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(
                        values[i].Trim(),
                        reason,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        static bool SemanticPathChanged(
            FootFrame previous,
            FootFrame current) =>
            previous.NextLandingEventIdentity != current.NextLandingEventIdentity ||
            previous.GroundPathState != current.GroundPathState ||
            Vector3.Distance(previous.NextLanding, current.NextLanding) >
            PositionNoiseFloor;

        static void AnalyzeSupportChanges(
            CsvCapture capture,
            List<EventFact> events)
        {
            Dictionary<int, FootFrame> left = capture.Left.ToDictionary(frame => frame.Frame);
            Dictionary<int, FootFrame> right = capture.Right.ToDictionary(frame => frame.Frame);
            List<int> frames = left.Keys.Intersect(right.Keys).OrderBy(value => value).ToList();
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = left[frames[i - 1]];
                FootFrame current = left[frames[i]];
                if (!Continuous(previous, current))
                    continue;
                bool changed = previous.PrimarySupportSide != current.PrimarySupportSide ||
                               previous.PrimarySupportEventIdentity != current.PrimarySupportEventIdentity;
                if (!changed)
                    continue;
                FootFrame previousRight = right[frames[i - 1]];
                FootFrame currentRight = right[frames[i]];
                double goalStep = Vector3.Distance(
                    previous.FinalPelvisGoal,
                    current.FinalPelvisGoal);
                double physicalStep = Vector3.Distance(
                    previous.PhysicalPelvis,
                    current.PhysicalPelvis);
                double extensionChange = Math.Max(
                    Math.Abs(current.TargetExtensionRatio - previous.TargetExtensionRatio),
                    Math.Abs(currentRight.TargetExtensionRatio - previousRight.TargetExtensionRatio));
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["pelvisGoalStepMeters"] = goalStep,
                    ["physicalPelvisStepMeters"] = physicalStep,
                    ["targetExtensionRatioChangeMaximum"] = extensionChange
                };
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["grounded"] = current.Grounded,
                    ["newSupportAvailable"] = current.PrimarySupportEventIdentity != 0,
                    ["supportSideChanged"] = previous.PrimarySupportSide != current.PrimarySupportSide
                };
                EventFact fact = new EventFact(
                    "SupportChange",
                    current.PrimarySupportSide,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    current.PrimarySupportEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence);
                events.Add(fact);
            }
        }

        static FactsDocument BuildDocument(
            string samplesPath,
            string geometryPath,
            CsvCapture capture,
            List<EventFact> events)
        {
            return new FactsDocument
            {
                schema = Schema,
                sample = new SampleFact
                {
                    identity = capture.SampleIdentity,
                    file = Path.GetFileName(samplesPath),
                    sha256 = ComputeSha256(samplesPath),
                    geometryFile = Path.GetFileName(geometryPath),
                    geometrySha256 = ComputeSha256(geometryPath),
                    programIdentity = capture.ProgramIdentity,
                    projectionRevision = capture.ProjectionRevision,
                    poseGraphId = capture.PoseGraphId,
                    poseGraphRevision = capture.PoseGraphRevision,
                    posePlanHash = capture.PosePlanHash,
                    frameCount = capture.UniqueFrameCount,
                    footRowCount = capture.FootRows.Count,
                    geometryRowCount = capture.GeometryRowCount
                },
                analyzer = new AnalyzerFact
                {
                    id = AnalyzerId,
                    version = AnalyzerVersion,
                    segmentationPositionEpsilonMeters = PositionNoiseFloor,
                    penetrationGeometryEpsilonMeters =
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters
                },
                coverage = new CoverageFact
                {
                    landingEventCount = events.Count(value => value.kind == "Landing"),
                    landingStateBoundaryCount = events.Count(
                        value => value.kind == "LandingStateBoundary"),
                    landingStateSpanCount = events.Count(
                        value => value.kind == "LandingStateSpan"),
                    lockedEventCount = events.Count(value => value.kind == "Locked"),
                    releaseEventCount = events.Count(value => value.kind == "Release"),
                    pathChangeCount = events.Count(value => value.kind == "PathChange"),
                    pathContinuityEventCount = events.Count(
                        value => value.kind == "PathContinuity"),
                    supportChangeCount = events.Count(value => value.kind == "SupportChange"),
                    contactPlanePenetrationEventCount = events.Count(
                        value => value.kind == "ContactPlanePenetration"),
                    leftFootFrameCount = capture.Left.Count,
                    rightFootFrameCount = capture.Right.Count,
                    frameGapCount = capture.FrameGapCount,
                    bodyResetCount = capture.BodyResetCount,
                    sourceChangeCount = capture.SourceChangeCount,
                    contactPlaneAvailableFootRowCount = capture.FootRows.Count(
                        value => value.PenetrationAvailable),
                    contactPlaneUnavailableFootRowCount = capture.FootRows.Count(
                        value => !value.PenetrationAvailable),
                    contactPlanePenetrationAvailability =
                        BuildPenetrationAvailabilityCounts(capture.FootRows),
                    groundPathRejectedFootRowCount = capture.FootRows.Count(value => value.GroundPathState != "Accepted")
                },
                events = events
            };
        }

        static CsvCapture ReadCapture(
            string samplesPath,
            string geometryPath)
        {
            using var reader = new StreamReader(samplesPath, Encoding.UTF8, true, 65536);
            string header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                throw new InvalidDataException("Foot Motion samples CSV is empty.");
            string[] names = ParseCsvLine(header);
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++)
                indices[names[i]] = i;
            RequireColumns(indices);
            var unique = new Dictionary<(int frame, string side), FootFrame>();
            int rawRows = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                    continue;
                rawRows++;
                string[] cells = ParseCsvLine(line);
                if (cells.Length != names.Length)
                {
                    throw new InvalidDataException(
                        $"Foot Motion samples CSV row {rawRows + 1} has " +
                        $"{cells.Length} columns; expected {names.Length}.");
                }
                FootFrame frame = ParseFrame(indices, cells);
                var key = (frame.Frame, frame.Side);
                if (!unique.TryAdd(key, frame))
                {
                    throw new InvalidDataException(
                        $"Foot Motion samples CSV has duplicate Foot row " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
            }
            if (unique.Count == 0)
                throw new InvalidDataException("Foot Motion samples CSV has no Foot rows.");
            List<FootFrame> footRows = unique.Values
                .OrderBy(value => value.Frame)
                .ThenBy(value => value.Side, StringComparer.Ordinal)
                .ToList();
            List<FootFrame> left = footRows.Where(value => value.Side == "Left").OrderBy(value => value.Frame).ToList();
            List<FootFrame> right = footRows.Where(value => value.Side == "Right").OrderBy(value => value.Frame).ToList();
            if (left.Count != right.Count ||
                !left.Select(value => value.Frame).SequenceEqual(
                    right.Select(value => value.Frame)))
            {
                throw new InvalidDataException(
                    "Foot Motion samples CSV does not contain one Left and one Right Foot row per frame.");
            }
            FootFrame first = footRows[0];
            int geometryRowCount = ReadGeometry(
                geometryPath,
                first.SampleIdentity,
                unique);
            int frameGapCount = CountTransitions(left, (previous, current) => current.Frame != previous.Frame + 1) +
                                CountTransitions(right, (previous, current) => current.Frame != previous.Frame + 1);
            int bodyResetCount = CountTransitions(left, (previous, current) => current.BodyResetSequence != previous.BodyResetSequence);
            int sourceChangeCount = CountTransitions(left, (previous, current) => previous.SourceIdentity != current.SourceIdentity) +
                                    CountTransitions(right, (previous, current) => previous.SourceIdentity != current.SourceIdentity);
            return new CsvCapture(
                first.SampleIdentity,
                first.ProgramIdentity,
                first.ProjectionRevision,
                first.PoseGraphId,
                first.PoseGraphRevision,
                first.PosePlanHash,
                geometryRowCount,
                footRows.Select(value => value.Frame).Distinct().Count(),
                frameGapCount,
                bodyResetCount,
                sourceChangeCount,
                footRows,
                left,
                right);
        }

        static int ReadGeometry(
            string geometryPath,
            string sampleIdentity,
            Dictionary<(int frame, string side), FootFrame> footRows)
        {
            using var reader = new StreamReader(
                geometryPath,
                Encoding.UTF8,
                true,
                65536);
            string header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                throw new InvalidDataException(
                    "Foot Motion ground path geometry CSV is empty.");
            string[] names = ParseCsvLine(header);
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++)
            {
                if (!indices.TryAdd(names[i], i))
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV has duplicate column '{names[i]}'.");
            }
            string[] required =
            {
                "SampleIdentity", "FrameSequence", "CompletionIdentity",
                "Side", "GroundPathInputIdentity", "GroundContactIndex",
                "GroundContactSegmentIndex", "GroundContactSurfaceIdentity",
                "GroundContactCandidateIdentity", "GroundContactPositionX",
                "GroundContactPositionY", "GroundContactPositionZ",
                "GroundContactNormalX", "GroundContactNormalY",
                "GroundContactNormalZ", "GroundContactQueryDistance",
                "GroundEnvelopeVertexIndex", "GroundEnvelopeVertexX",
                "GroundEnvelopeVertexY", "GroundEnvelopeVertexZ"
            };
            for (int i = 0; i < required.Length; i++)
            {
                if (!indices.ContainsKey(required[i]))
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV is missing '{required[i]}'.");
            }
            var contacts = new HashSet<(int frame, string side, int index)>();
            var envelope = new HashSet<(int frame, string side, int index)>();
            int rowCount = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                    continue;
                rowCount++;
                string[] cells = ParseCsvLine(line);
                if (cells.Length != names.Length)
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has " +
                        $"{cells.Length} columns; expected {names.Length}.");
                }
                string Cell(string name) => cells[indices[name]];
                if (!string.Equals(
                        Cell("SampleIdentity"),
                        sampleIdentity,
                        StringComparison.Ordinal))
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has mismatched Sample identity.");
                }
                int frame = ParseInt(
                    Cell("FrameSequence"),
                    "FrameSequence");
                string side = Cell("Side");
                if (!footRows.TryGetValue((frame, side), out FootFrame foot))
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has no Foot row.");
                }
                if (ParseUlong(
                        Cell("CompletionIdentity"),
                        "CompletionIdentity") !=
                        foot.CompletionIdentity ||
                    ParseUlong(
                        Cell("GroundPathInputIdentity"),
                        "GroundPathInputIdentity") !=
                        foot.GroundPathInputIdentity)
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has mismatched lineage.");
                }
                int contactIndex = ParseInt(
                    Cell("GroundContactIndex"),
                    "GroundContactIndex");
                int envelopeIndex = ParseInt(
                    Cell("GroundEnvelopeVertexIndex"),
                    "GroundEnvelopeVertexIndex");
                if (contactIndex < 0 && envelopeIndex < 0)
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has no geometry payload.");
                }
                if (contactIndex >= 0 &&
                    !contacts.Add((frame, side, contactIndex)))
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV has duplicate Contact index " +
                        $"Frame={frame} Side={side} Index={contactIndex}.");
                }
                if (envelopeIndex >= 0 &&
                    !envelope.Add((frame, side, envelopeIndex)))
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV has duplicate Envelope index " +
                        $"Frame={frame} Side={side} Index={envelopeIndex}.");
                }
            }
            return rowCount;
        }

        static string[] ParseCsvLine(string line)
        {
            var cells = new List<string>(HeaderColumnCapacity);
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char character = line[i];
                if (quoted)
                {
                    if (character != '"')
                    {
                        cell.Append(character);
                        continue;
                    }
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                        continue;
                    }
                    quoted = false;
                    continue;
                }
                if (character == '"' && cell.Length == 0)
                {
                    quoted = true;
                    continue;
                }
                if (character == ',')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }
                cell.Append(character);
            }
            if (quoted)
                throw new InvalidDataException("Foot Motion samples CSV has an unterminated quoted field.");
            cells.Add(cell.ToString());
            return cells.ToArray();
        }

        static FootFrame ParseFrame(
            Dictionary<string, int> indices,
            string[] cells)
        {
            string Cell(string name) =>
                indices.TryGetValue(name, out int index) && index < cells.Length
                    ? cells[index]
                    : string.Empty;
            float Float(string name) => ParseFloat(Cell(name), name);
            int Int(string name) => ParseInt(Cell(name), name);
            ulong Ulong(string name) => ParseUlong(Cell(name), name);
            Vector3 Vector(string prefix) => new Vector3(
                Float(prefix + "X"),
                Float(prefix + "Y"),
                Float(prefix + "Z"));
            var frame = new FootFrame
            {
                SampleIdentity = Cell("SampleIdentity"),
                ProgramIdentity = Cell("ProgramIdentity"),
                ProjectionRevision = Cell("ProjectionRevision"),
                PoseGraphId = Cell("PoseGraphId"),
                PoseGraphRevision = Cell("PoseGraphRevision"),
                PosePlanHash = Cell("PosePlanHash"),
                Frame = Int("FrameSequence"),
                CompletionIdentity = Ulong("CompletionIdentity"),
                Side = Cell("Side"),
                DeltaSeconds = Float("PresentationDeltaSeconds"),
                BodyResetSequence = Ulong("BodyResetSequence"),
                Grounded = Int("Grounded") != 0,
                SourceIdentity = Cell("InputFormalStepSourceIdentity"),
                SourceCycle = Int("InputFormalStepSourceCycle"),
                ContributionContinuityIdentity = Ulong("InputFormalStepContributionContinuityIdentity"),
                FormalNormalizedTime = Float("InputFormalStepSourceNormalizedTime"),
                FormalStepTime = Float("InputFormalStepTimeSeconds"),
                FormalLockMode = Cell("InputFormalLockMode"),
                FormalLockWeight = Float("InputFormalLockWeight"),
                FormalSupport = Float("InputFormalSupport"),
                LandingPredictionState = Cell("State"),
                RawLandingAvailable = Int("RawLandingAvailable") != 0,
                RawLanding = Vector("RawLandingCandidate"),
                GroundPathState = Cell("GroundPathState"),
                GroundPathRejectReason = Cell("GroundPathRejectReason"),
                GroundPathInputIdentity = Ulong("GroundPathInputIdentity"),
                GroundPathTargetAvailable =
                    Int("GroundPathTargetAvailable") != 0,
                LastLandingEventIdentity = Ulong("GroundPathLastLandingEventIdentity"),
                NextLandingEventIdentity = Ulong("GroundPathNextSwingLandingEventIdentity"),
                NextLanding = Vector("GroundPathNextSwingLanding"),
                ComponentUp = Vector("GroundPathComponentUp"),
                FootMotionEventIdentity = Ulong("FootMotionLandingEventIdentity"),
                FootMotionGroundPathInputIdentity =
                    Ulong("FootMotionGroundPathInputIdentity"),
                FootMotionState = Cell("FootMotionState"),
                ConstraintState = Cell("FootMotionConstraintState"),
                LockResponse = Cell("FootMotionLockResponse"),
                OriginalSole = Vector("FootMotionOriginalSole"),
                OriginalAnkle = Vector("FootMotionOriginalAnkle"),
                CorrectedSole = Vector("FootMotionCorrectedSole"),
                CorrectedAnkle = Vector("FootMotionCorrectedAnkle"),
                Anchor = Vector("FootMotionSupportContactAnchor"),
                ContactPlaneAvailable = Int("FootMotionContactPlaneAvailable") != 0,
                ContactSurfaceIdentity = Int("FootMotionContactSurfaceIdentity"),
                ContactNormal = Vector("FootMotionContactPlaneNormal"),
                PathContinuityEvaluated =
                    Int("FootMotionPathContinuityEvaluated") != 0,
                PathRevisionReason = Cell("FootMotionPathRevisionReason"),
                PathResidualRebuilt =
                    Int("FootMotionPathResidualRebuilt") != 0,
                PathAvailableBefore =
                    Int("FootMotionPathAvailableBefore") != 0,
                PathAvailableAfter =
                    Int("FootMotionPathAvailableAfter") != 0,
                PathPreviousLandingEventIdentity =
                    Ulong("FootMotionPathPreviousLandingEventIdentity"),
                PathCurrentLandingEventIdentity =
                    Ulong("FootMotionPathCurrentLandingEventIdentity"),
                PathPreviousTargetCorrection =
                    Vector("FootMotionPathPreviousTargetCorrection"),
                PathCurrentTargetCorrection =
                    Vector("FootMotionPathCurrentTargetCorrection"),
                PathLandingPointDelta =
                    Float("FootMotionPathLandingPointDeltaMeters"),
                PathTargetDelta = Float("FootMotionPathTargetDeltaMeters"),
                SwingResidualBeforeRevision =
                    Vector("FootMotionSwingResidualBeforeRevision"),
                SwingResidualBeforeSchedule =
                    Vector("FootMotionSwingResidualBeforeSchedule"),
                SwingResidualAfterSchedule =
                    Vector("FootMotionSwingResidualAfterSchedule"),
                ResidualOutputCorrection =
                    Vector("FootMotionResidualOutputCorrection"),
                LandingUpdateDistance = Float("FootMotionLandingUpdateDistance"),
                ResidualTimeToLandingSeconds =
                    Float("FootMotionResidualTimeToLandingSeconds"),
                ResidualBaseHalfLifeSeconds =
                    Float("FootMotionResidualBaseHalfLifeSeconds"),
                ResidualDeadlineOutcome =
                    Cell("FootMotionResidualDeadlineOutcome"),
                ResidualDeadlineRequiredStepMeters =
                    Float("FootMotionResidualDeadlineRequiredStepMeters"),
                ResidualBaseStepMeters =
                    Float("FootMotionResidualBaseStepMeters"),
                ResidualAppliedStepMeters =
                    Float("FootMotionResidualAppliedStepMeters"),
                ResidualRemainingDistanceMeters =
                    Float("FootMotionResidualRemainingDistanceMeters"),
                ConstraintStateBefore = Cell("FootMotionConstraintStateBefore"),
                LockResponseBefore = Cell("FootMotionLockResponseBefore"),
                OutputStagesAvailable =
                    Int("FootMotionOutputStagesAvailable") != 0,
                ReleasingCompletedToSwing =
                    Int("FootMotionReleasingCompletedToSwing") != 0,
                EnvelopeAvailable = Int("FootMotionEnvelopeAvailable") != 0,
                CorrectionBeforeSafetyFloor =
                    Vector("FootMotionCorrectionBeforeSafetyFloor"),
                GroundEnvelopeSafetyCorrection =
                    Vector("FootMotionGroundEnvelopeSafetyCorrection"),
                SafetyFloorOutputCorrection =
                    Vector("FootMotionSafetyFloorOutputCorrection"),
                FinalEffectiveCorrection =
                    Vector("FootMotionFinalEffectiveCorrection"),
                SafetyFloorClamped =
                    Int("FootMotionSafetyFloorClamped") != 0,
                SafetyFloorClampMeters =
                    Float("FootMotionSafetyFloorClampMeters"),
                EnvelopeClearanceBeforeMeters =
                    Float("FootMotionEnvelopeClearanceBeforeMeters"),
                EnvelopeClearanceAfterMeters =
                    Float("FootMotionEnvelopeClearanceAfterMeters"),
                EncodedGoalAvailable =
                    Int("FootMotionEncodedGoalAvailable") != 0,
                EncodedGoalPosition = Vector("FinalGoalPosition"),
                EncodedGoalCorrection =
                    Vector("FootMotionEncodedGoalCorrection"),
                FinalIkEffectorAvailable =
                    Int("FinalIkEffectorAvailable") != 0,
                FinalIkTargetPosition = Vector("FinalIkTargetPosition"),
                FinalIkSolvedPosition = Vector("FinalIkSolvedPosition"),
                FinalPhysicalWriteAvailable =
                    Int("FinalPhysicalWriteAvailable") != 0,
                FinalPhysicalAnkleComponentPosition =
                    Vector("FinalPhysicalAnkleComponentPosition"),
                PenetrationAvailability = Cell("FootContactPlanePenetrationAvailability"),
                SourceHeel = Vector("FootMotionSourceHeel"),
                SourceToe = Vector("FootMotionSourceToe"),
                FinalHeel = Vector("FinalPhysicalHeelWorld"),
                FinalToe = Vector("FinalPhysicalToeWorld"),
                HasAnchor = Ulong("FootMotionLandingEventIdentity") != 0 &&
                            Cell("FootMotionConstraintState") != "Swing",
                TargetExtensionRatio = Float("FinalIkLegTargetExtensionRatio"),
                SolvedExtensionRatio = Float("FinalIkLegSolvedExtensionRatio"),
                SolvedBendDegrees = Float("FinalIkLegSolvedBendDegrees"),
                TargetCompressionReserve = Float("FinalIkLegTargetCompressionReserve"),
                BendDirectionPreviousDot = Float("FinalIkLegEffectiveBendDirectionPreviousDot"),
                PrimarySupportSide = Cell("PrimarySupportSide"),
                PrimarySupportEventIdentity = Ulong("PrimarySupportLandingEventIdentity"),
                PelvisWeight = Float("PelvisPositionWeight"),
                FinalPelvisGoal = Vector("FinalPelvisGoal"),
                PhysicalPelvis = Vector("FinalPhysicalPelvisComponentPosition")
            };
            RequireValidFrame(frame);
            return frame;
        }

        static void RequireValidFrame(FootFrame frame)
        {
            if (frame.Frame <= 0 || frame.CompletionIdentity == 0)
                throw new InvalidDataException("Foot Motion Foot row lineage is invalid.");
            if (frame.Side != "Left" && frame.Side != "Right")
                throw new InvalidDataException(
                    $"Foot Motion Foot row Side '{frame.Side}' is invalid.");
            RequireEnum<CharacterFootLandingPredictionState>(
                frame.LandingPredictionState,
                "State");
            RequireEnum<CharacterFootSwingMotionState>(
                frame.FootMotionState,
                "FootMotionState");
            RequireEnum<CharacterFootConstraintState>(
                frame.ConstraintState,
                "FootMotionConstraintState");
            RequireEnum<CharacterFootConstraintState>(
                frame.ConstraintStateBefore,
                "FootMotionConstraintStateBefore");
            RequireEnum<CharacterFootLockResponse>(
                frame.LockResponse,
                "FootMotionLockResponse");
            RequireEnum<CharacterFootLockResponse>(
                frame.LockResponseBefore,
                "FootMotionLockResponseBefore");
            RequireEnum<CharacterFootResidualDeadlineOutcome>(
                frame.ResidualDeadlineOutcome,
                "FootMotionResidualDeadlineOutcome");
            RequireRevisionReason(frame.PathRevisionReason);
        }

        static void RequireEnum<T>(string value, string field)
            where T : struct, Enum
        {
            if (!Enum.TryParse(value, false, out T parsed) ||
                !Enum.IsDefined(typeof(T), parsed))
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row {field} '{value}' is invalid.");
            }
        }

        static void RequireRevisionReason(string value)
        {
            string[] values = value.Split(',');
            bool none = false;
            for (int i = 0; i < values.Length; i++)
            {
                string reason = values[i].Trim();
                bool valid = reason == "None" ||
                             reason == "PathAvailabilityChanged" ||
                             reason == "LandingEventChanged" ||
                             reason == "LandingPointChanged" ||
                             reason == "SwingTargetChanged";
                if (!valid)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Foot row PathRevisionReason '{value}' is invalid.");
                }
                none |= reason == "None";
            }
            if (none && values.Length != 1)
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row PathRevisionReason '{value}' is invalid.");
            }
        }

        static void RequireColumns(Dictionary<string, int> indices)
        {
            string[] required =
            {
                "SampleIdentity", "ProgramIdentity", "ProjectionRevision",
                "PoseGraphId", "PoseGraphRevision", "PosePlanHash",
                "FrameSequence", "CompletionIdentity", "Side",
                "PresentationDeltaSeconds", "BodyResetSequence", "Grounded",
                "InputFormalStepSourceIdentity", "InputFormalStepSourceCycle",
                "InputFormalStepContributionContinuityIdentity",
                "InputFormalStepSourceNormalizedTime", "InputFormalStepTimeSeconds",
                "InputFormalLockMode", "InputFormalLockWeight", "InputFormalSupport",
                "State", "RawLandingAvailable",
                "RawLandingCandidateX", "RawLandingCandidateY", "RawLandingCandidateZ",
                "GroundPathState", "GroundPathRejectReason", "GroundPathInputIdentity",
                "GroundPathTargetAvailable",
                "GroundPathLastLandingEventIdentity", "GroundPathNextSwingLandingEventIdentity",
                "GroundPathNextSwingLandingX", "GroundPathNextSwingLandingY", "GroundPathNextSwingLandingZ",
                "GroundPathComponentUpX", "GroundPathComponentUpY", "GroundPathComponentUpZ",
                "FootMotionLandingEventIdentity", "FootMotionGroundPathInputIdentity",
                "FootMotionState", "FootMotionConstraintState",
                "FootMotionLockResponse",
                "FootMotionOriginalSoleX", "FootMotionOriginalSoleY", "FootMotionOriginalSoleZ",
                "FootMotionOriginalAnkleX", "FootMotionOriginalAnkleY", "FootMotionOriginalAnkleZ",
                "FootMotionCorrectedSoleX", "FootMotionCorrectedSoleY", "FootMotionCorrectedSoleZ",
                "FootMotionCorrectedAnkleX", "FootMotionCorrectedAnkleY", "FootMotionCorrectedAnkleZ",
                "FootMotionSupportContactAnchorX", "FootMotionSupportContactAnchorY", "FootMotionSupportContactAnchorZ",
                "FootMotionContactPlaneAvailable", "FootMotionContactSurfaceIdentity",
                "FootMotionContactPlaneNormalX", "FootMotionContactPlaneNormalY", "FootMotionContactPlaneNormalZ",
                "FootContactPlanePenetrationAvailability",
                "FootMotionPathContinuityEvaluated", "FootMotionPathRevisionReason",
                "FootMotionPathResidualRebuilt", "FootMotionPathAvailableBefore",
                "FootMotionPathAvailableAfter", "FootMotionPathPreviousLandingEventIdentity",
                "FootMotionPathCurrentLandingEventIdentity",
                "FootMotionPathPreviousTargetCorrectionX",
                "FootMotionPathPreviousTargetCorrectionY",
                "FootMotionPathPreviousTargetCorrectionZ",
                "FootMotionPathCurrentTargetCorrectionX",
                "FootMotionPathCurrentTargetCorrectionY",
                "FootMotionPathCurrentTargetCorrectionZ",
                "FootMotionPathLandingPointDeltaMeters", "FootMotionPathTargetDeltaMeters",
                "FootMotionSwingResidualBeforeRevisionX",
                "FootMotionSwingResidualBeforeRevisionY",
                "FootMotionSwingResidualBeforeRevisionZ",
                "FootMotionSwingResidualBeforeScheduleX",
                "FootMotionSwingResidualBeforeScheduleY",
                "FootMotionSwingResidualBeforeScheduleZ",
                "FootMotionSwingResidualAfterScheduleX",
                "FootMotionSwingResidualAfterScheduleY",
                "FootMotionSwingResidualAfterScheduleZ",
                "FootMotionResidualOutputCorrectionX",
                "FootMotionResidualOutputCorrectionY",
                "FootMotionResidualOutputCorrectionZ",
                "FootMotionLandingUpdateDistance",
                "FootMotionResidualTimeToLandingSeconds",
                "FootMotionResidualBaseHalfLifeSeconds",
                "FootMotionResidualDeadlineOutcome",
                "FootMotionResidualDeadlineRequiredStepMeters",
                "FootMotionResidualBaseStepMeters",
                "FootMotionResidualAppliedStepMeters",
                "FootMotionResidualRemainingDistanceMeters",
                "FootMotionConstraintStateBefore", "FootMotionLockResponseBefore",
                "FootMotionOutputStagesAvailable",
                "FootMotionReleasingCompletedToSwing",
                "FootMotionEnvelopeAvailable",
                "FootMotionCorrectionBeforeSafetyFloorX",
                "FootMotionCorrectionBeforeSafetyFloorY",
                "FootMotionCorrectionBeforeSafetyFloorZ",
                "FootMotionGroundEnvelopeSafetyCorrectionX",
                "FootMotionGroundEnvelopeSafetyCorrectionY",
                "FootMotionGroundEnvelopeSafetyCorrectionZ",
                "FootMotionSafetyFloorOutputCorrectionX",
                "FootMotionSafetyFloorOutputCorrectionY",
                "FootMotionSafetyFloorOutputCorrectionZ",
                "FootMotionFinalEffectiveCorrectionX",
                "FootMotionFinalEffectiveCorrectionY",
                "FootMotionFinalEffectiveCorrectionZ",
                "FootMotionSafetyFloorClamped", "FootMotionSafetyFloorClampMeters",
                "FootMotionEnvelopeClearanceBeforeMeters",
                "FootMotionEnvelopeClearanceAfterMeters",
                "FootMotionEncodedGoalAvailable",
                "FootMotionEncodedGoalCorrectionX",
                "FootMotionEncodedGoalCorrectionY",
                "FootMotionEncodedGoalCorrectionZ",
                "FinalGoalPositionX", "FinalGoalPositionY", "FinalGoalPositionZ",
                "FinalIkEffectorAvailable",
                "FinalIkTargetPositionX", "FinalIkTargetPositionY",
                "FinalIkTargetPositionZ", "FinalIkSolvedPositionX",
                "FinalIkSolvedPositionY", "FinalIkSolvedPositionZ",
                "FinalPhysicalWriteAvailable",
                "FinalPhysicalAnkleComponentPositionX",
                "FinalPhysicalAnkleComponentPositionY",
                "FinalPhysicalAnkleComponentPositionZ",
                "FootMotionSourceHeelX", "FootMotionSourceHeelY", "FootMotionSourceHeelZ",
                "FootMotionSourceToeX", "FootMotionSourceToeY", "FootMotionSourceToeZ",
                "FinalPhysicalHeelWorldX", "FinalPhysicalHeelWorldY", "FinalPhysicalHeelWorldZ",
                "FinalPhysicalToeWorldX", "FinalPhysicalToeWorldY", "FinalPhysicalToeWorldZ",
                "FinalIkLegTargetExtensionRatio", "FinalIkLegSolvedExtensionRatio",
                "FinalIkLegSolvedBendDegrees", "FinalIkLegTargetCompressionReserve",
                "FinalIkLegEffectiveBendDirectionPreviousDot",
                "PrimarySupportSide", "PrimarySupportLandingEventIdentity",
                "PelvisPositionWeight", "FinalPelvisGoalX", "FinalPelvisGoalY", "FinalPelvisGoalZ",
                "FinalPhysicalPelvisComponentPositionX", "FinalPhysicalPelvisComponentPositionY",
                "FinalPhysicalPelvisComponentPositionZ"
            };
            foreach (string name in required)
            {
                if (!indices.ContainsKey(name))
                    throw new InvalidDataException($"Foot Motion samples CSV is missing '{name}'.");
            }
        }

        static void PublishFacts(string factsPath, FactsDocument document)
        {
            string partPath = factsPath + ".part";
            if (File.Exists(partPath))
                File.Delete(partPath);
            try
            {
                using (var stream = new FileStream(
                           partPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.Read,
                           65536,
                           FileOptions.SequentialScan))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                using (var json = new JsonTextWriter(writer)
                       {
                           Formatting = Formatting.Indented,
                           Culture = CultureInfo.InvariantCulture
                       })
                {
                    JsonSerializer serializer = JsonSerializer.Create(
                        new JsonSerializerSettings
                        {
                            Culture = CultureInfo.InvariantCulture,
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    serializer.Serialize(json, document);
                    json.Flush();
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(factsPath))
                    File.Replace(partPath, factsPath, null);
                else
                    File.Move(partPath, factsPath);
            }
            catch
            {
                if (File.Exists(partPath))
                    File.Delete(partPath);
                throw;
            }
        }

        static string ComputeSha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = sha.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        static bool Continuous(FootFrame previous, FootFrame current) =>
            current.Frame == previous.Frame + 1 &&
            current.BodyResetSequence == previous.BodyResetSequence;

        static double DeltaSeconds(FootFrame frame) =>
            Math.Max(frame.DeltaSeconds, 0.000001f);

        static double Duration(IReadOnlyList<FootFrame> frames)
        {
            double result = 0d;
            for (int i = 0; i < frames.Count; i++)
                result += DeltaSeconds(frames[i]);
            return result;
        }

        static double MaximumCorrectionStep(
            IReadOnlyList<FootFrame> frames,
            bool unanchoredOnly = false,
            bool swingOnly = false)
        {
            double maximum = 0d;
            for (int i = 1; i < frames.Count; i++)
            {
                if (unanchoredOnly &&
                    (frames[i - 1].HasAnchor || frames[i].HasAnchor))
                {
                    continue;
                }
                if (swingOnly &&
                    (frames[i - 1].ConstraintState != "Swing" ||
                     frames[i].ConstraintState != "Swing"))
                {
                    continue;
                }
                maximum = Math.Max(
                    maximum,
                    Vector3.Distance(
                        frames[i - 1].EffectiveCorrection,
                        frames[i].EffectiveCorrection));
            }
            return maximum;
        }

        static int PeakCorrectionFrame(
            IReadOnlyList<FootFrame> frames,
            bool unanchoredOnly = false,
            bool swingOnly = false)
        {
            double maximum = -1d;
            int frame = frames.Count > 0 ? frames[0].Frame : 0;
            for (int i = 1; i < frames.Count; i++)
            {
                if (unanchoredOnly &&
                    (frames[i - 1].HasAnchor || frames[i].HasAnchor))
                {
                    continue;
                }
                if (swingOnly &&
                    (frames[i - 1].ConstraintState != "Swing" ||
                     frames[i].ConstraintState != "Swing"))
                {
                    continue;
                }
                double value = Vector3.Distance(
                    frames[i - 1].EffectiveCorrection,
                    frames[i].EffectiveCorrection);
                if (value <= maximum)
                    continue;
                maximum = value;
                frame = frames[i].Frame;
            }
            return frame;
        }

        static bool HasUnanchoredSwingPair(IReadOnlyList<FootFrame> frames)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                if (Continuous(frames[i - 1], frames[i]) &&
                    !frames[i - 1].HasAnchor &&
                    !frames[i].HasAnchor &&
                    frames[i - 1].ConstraintState == "Swing" &&
                    frames[i].ConstraintState == "Swing")
                {
                    return true;
                }
            }
            return false;
        }

        static int PeakDistanceFrame(IReadOnlyList<FootFrame> frames)
        {
            double maximum = -1d;
            int frame = frames.Count > 0 ? frames[0].Frame : 0;
            for (int i = 0; i < frames.Count; i++)
            {
                double value = Vector3.Distance(frames[i].CorrectedSole, frames[i].Anchor);
                if (value <= maximum)
                    continue;
                maximum = value;
                frame = frames[i].Frame;
            }
            return frame;
        }

        static double MaximumCorrectionJerk(
            IReadOnlyList<FootFrame> frames,
            bool unanchoredOnly = false)
        {
            if (frames.Count < 4)
                return 0d;
            Vector3 previousVelocity = default;
            Vector3 previousAcceleration = default;
            bool hasVelocity = false;
            bool hasAcceleration = false;
            double maximum = 0d;
            for (int i = 1; i < frames.Count; i++)
            {
                if (unanchoredOnly &&
                    (frames[i - 1].HasAnchor || frames[i].HasAnchor))
                {
                    hasVelocity = false;
                    hasAcceleration = false;
                    continue;
                }
                double dt = DeltaSeconds(frames[i]);
                Vector3 velocity =
                    (frames[i].EffectiveCorrection - frames[i - 1].EffectiveCorrection) /
                    (float)dt;
                if (!hasVelocity)
                {
                    previousVelocity = velocity;
                    hasVelocity = true;
                    continue;
                }
                Vector3 acceleration = (velocity - previousVelocity) / (float)dt;
                previousVelocity = velocity;
                if (!hasAcceleration)
                {
                    previousAcceleration = acceleration;
                    hasAcceleration = true;
                    continue;
                }
                Vector3 jerk = (acceleration - previousAcceleration) / (float)dt;
                previousAcceleration = acceleration;
                maximum = Math.Max(maximum, jerk.magnitude);
            }
            return maximum;
        }

        static double MaximumUnanchoredCorrectionRange(
            IReadOnlyList<FootFrame> frames)
        {
            double maximum = 0d;
            int start = 0;
            while (start < frames.Count)
            {
                while (start < frames.Count && frames[start].HasAnchor)
                    start++;
                int end = start;
                while (end < frames.Count && !frames[end].HasAnchor)
                    end++;
                if (end > start)
                {
                    maximum = Math.Max(
                        maximum,
                        VectorRange(frames
                            .Skip(start)
                            .Take(end - start)
                            .Select(frame => frame.EffectiveCorrection)));
                }
                start = end + 1;
            }
            return maximum;
        }

        static int VelocityReversalCount(IReadOnlyList<Vector3> values)
        {
            int count = 0;
            Vector3 previous = default;
            bool hasPrevious = false;
            for (int i = 1; i < values.Count; i++)
            {
                Vector3 velocity = values[i] - values[i - 1];
                if (velocity.sqrMagnitude <= PositionNoiseFloor * PositionNoiseFloor)
                    continue;
                if (hasPrevious && Vector3.Dot(previous, velocity) < 0f)
                    count++;
                previous = velocity;
                hasPrevious = true;
            }
            return count;
        }

        static bool HasPathChange(IReadOnlyList<FootFrame> frames)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                if (frames[i - 1].GroundPathInputIdentity != frames[i].GroundPathInputIdentity ||
                    frames[i - 1].NextLandingEventIdentity != frames[i].NextLandingEventIdentity ||
                    Vector3.Distance(frames[i - 1].NextLanding, frames[i].NextLanding) > PositionNoiseFloor ||
                    frames[i - 1].GroundPathState != frames[i].GroundPathState)
                {
                    return true;
                }
            }
            return false;
        }

        static double VectorRange(IEnumerable<Vector3> values)
        {
            List<Vector3> list = values.ToList();
            double maximum = 0d;
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                    maximum = Math.Max(maximum, Vector3.Distance(list[i], list[j]));
            }
            return maximum;
        }

        static double MaximumVectorStep(IReadOnlyList<Vector3> values)
        {
            double maximum = 0d;
            for (int i = 1; i < values.Count; i++)
                maximum = Math.Max(maximum, Vector3.Distance(values[i - 1], values[i]));
            return maximum;
        }

        static int CountTransitions(
            List<FootFrame> values,
            Func<FootFrame, FootFrame, bool> predicate)
        {
            int count = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (predicate(values[i - 1], values[i]))
                    count++;
            }
            return count;
        }

        static SortedDictionary<string, int> BuildPenetrationAvailabilityCounts(
            IEnumerable<FootFrame> frames)
        {
            var result = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (FootFrame frame in frames)
            {
                string key = string.IsNullOrEmpty(frame.PenetrationAvailability)
                    ? "Unspecified"
                    : frame.PenetrationAvailability;
                result.TryGetValue(key, out int count);
                result[key] = count + 1;
            }
            return result;
        }

        static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
                return 0d;
            values.Sort();
            double position = (values.Count - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower == upper)
                return values[lower];
            double t = position - lower;
            return values[lower] + (values[upper] - values[lower]) * t;
        }

        static float ParseFloat(string value, string field)
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

        static int ParseInt(string value, string field)
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

        static ulong ParseUlong(string value, string field)
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

        sealed class CsvCapture
        {
            internal CsvCapture(
                string sampleIdentity,
                string programIdentity,
                string projectionRevision,
                string poseGraphId,
                string poseGraphRevision,
                string posePlanHash,
                int geometryRowCount,
                int uniqueFrameCount,
                int frameGapCount,
                int bodyResetCount,
                int sourceChangeCount,
                List<FootFrame> footRows,
                List<FootFrame> left,
                List<FootFrame> right)
            {
                SampleIdentity = sampleIdentity;
                ProgramIdentity = programIdentity;
                ProjectionRevision = projectionRevision;
                PoseGraphId = poseGraphId;
                PoseGraphRevision = poseGraphRevision;
                PosePlanHash = posePlanHash;
                GeometryRowCount = geometryRowCount;
                UniqueFrameCount = uniqueFrameCount;
                FrameGapCount = frameGapCount;
                BodyResetCount = bodyResetCount;
                SourceChangeCount = sourceChangeCount;
                FootRows = footRows;
                Left = left;
                Right = right;
            }

            internal string SampleIdentity { get; }
            internal string ProgramIdentity { get; }
            internal string ProjectionRevision { get; }
            internal string PoseGraphId { get; }
            internal string PoseGraphRevision { get; }
            internal string PosePlanHash { get; }
            internal int GeometryRowCount { get; }
            internal int UniqueFrameCount { get; }
            internal int FrameGapCount { get; }
            internal int BodyResetCount { get; }
            internal int SourceChangeCount { get; }
            internal List<FootFrame> FootRows { get; }
            internal List<FootFrame> Left { get; }
            internal List<FootFrame> Right { get; }
        }

        sealed class FootFrame
        {
            internal string SampleIdentity;
            internal string ProgramIdentity;
            internal string ProjectionRevision;
            internal string PoseGraphId;
            internal string PoseGraphRevision;
            internal string PosePlanHash;
            internal int Frame;
            internal ulong CompletionIdentity;
            internal string Side;
            internal float DeltaSeconds;
            internal ulong BodyResetSequence;
            internal bool Grounded;
            internal string SourceIdentity;
            internal int SourceCycle;
            internal ulong ContributionContinuityIdentity;
            internal float FormalNormalizedTime;
            internal float FormalStepTime;
            internal string FormalLockMode;
            internal float FormalLockWeight;
            internal float FormalSupport;
            internal string LandingPredictionState;
            internal bool RawLandingAvailable;
            internal Vector3 RawLanding;
            internal string GroundPathState;
            internal string GroundPathRejectReason;
            internal ulong GroundPathInputIdentity;
            internal bool GroundPathTargetAvailable;
            internal ulong LastLandingEventIdentity;
            internal ulong NextLandingEventIdentity;
            internal Vector3 NextLanding;
            internal Vector3 ComponentUp;
            internal ulong FootMotionEventIdentity;
            internal ulong FootMotionGroundPathInputIdentity;
            internal string FootMotionState;
            internal string ConstraintState;
            internal string LockResponse;
            internal Vector3 OriginalSole;
            internal Vector3 OriginalAnkle;
            internal Vector3 CorrectedSole;
            internal Vector3 CorrectedAnkle;
            internal Vector3 Anchor;
            internal bool ContactPlaneAvailable;
            internal int ContactSurfaceIdentity;
            internal Vector3 ContactNormal;
            internal bool PathContinuityEvaluated;
            internal string PathRevisionReason;
            internal bool PathResidualRebuilt;
            internal bool PathAvailableBefore;
            internal bool PathAvailableAfter;
            internal ulong PathPreviousLandingEventIdentity;
            internal ulong PathCurrentLandingEventIdentity;
            internal Vector3 PathPreviousTargetCorrection;
            internal Vector3 PathCurrentTargetCorrection;
            internal float PathLandingPointDelta;
            internal float PathTargetDelta;
            internal Vector3 SwingResidualBeforeRevision;
            internal Vector3 SwingResidualBeforeSchedule;
            internal Vector3 SwingResidualAfterSchedule;
            internal Vector3 ResidualOutputCorrection;
            internal float LandingUpdateDistance;
            internal float ResidualTimeToLandingSeconds;
            internal float ResidualBaseHalfLifeSeconds;
            internal string ResidualDeadlineOutcome;
            internal float ResidualDeadlineRequiredStepMeters;
            internal float ResidualBaseStepMeters;
            internal float ResidualAppliedStepMeters;
            internal float ResidualRemainingDistanceMeters;
            internal string ConstraintStateBefore;
            internal string LockResponseBefore;
            internal bool OutputStagesAvailable;
            internal bool ReleasingCompletedToSwing;
            internal bool EnvelopeAvailable;
            internal Vector3 CorrectionBeforeSafetyFloor;
            internal Vector3 GroundEnvelopeSafetyCorrection;
            internal Vector3 SafetyFloorOutputCorrection;
            internal Vector3 FinalEffectiveCorrection;
            internal bool SafetyFloorClamped;
            internal float SafetyFloorClampMeters;
            internal float EnvelopeClearanceBeforeMeters;
            internal float EnvelopeClearanceAfterMeters;
            internal bool EncodedGoalAvailable;
            internal Vector3 EncodedGoalPosition;
            internal Vector3 EncodedGoalCorrection;
            internal bool FinalIkEffectorAvailable;
            internal Vector3 FinalIkTargetPosition;
            internal Vector3 FinalIkSolvedPosition;
            internal bool FinalPhysicalWriteAvailable;
            internal Vector3 FinalPhysicalAnkleComponentPosition;
            internal string PenetrationAvailability;
            internal Vector3 SourceHeel;
            internal Vector3 SourceToe;
            internal Vector3 FinalHeel;
            internal Vector3 FinalToe;
            internal bool HasAnchor;
            internal bool PenetrationAvailable =>
                ContactPlaneAvailable &&
                (ConstraintState == "Landing" || ConstraintState == "Locked") &&
                PenetrationAvailability ==
                CharacterFootContactPlanePenetrationAvailability.Available.ToString();
            internal float TargetExtensionRatio;
            internal float SolvedExtensionRatio;
            internal float SolvedBendDegrees;
            internal float TargetCompressionReserve;
            internal float BendDirectionPreviousDot;
            internal string PrimarySupportSide;
            internal ulong PrimarySupportEventIdentity;
            internal float PelvisWeight;
            internal Vector3 FinalPelvisGoal;
            internal Vector3 PhysicalPelvis;
            internal Vector3 EffectiveCorrection => CorrectedAnkle - OriginalAnkle;
        }

        [Serializable]
        sealed class FactsDocument
        {
            public string schema;
            public SampleFact sample;
            public AnalyzerFact analyzer;
            public CoverageFact coverage;
            public List<EventFact> events;
        }

        [Serializable]
        sealed class SampleFact
        {
            public string identity;
            public string file;
            public string sha256;
            public string geometryFile;
            public string geometrySha256;
            public string programIdentity;
            public string projectionRevision;
            public string poseGraphId;
            public string poseGraphRevision;
            public string posePlanHash;
            public int frameCount;
            public int footRowCount;
            public int geometryRowCount;
        }

        [Serializable]
        sealed class AnalyzerFact
        {
            public string id;
            public int version;
            public double segmentationPositionEpsilonMeters;
            public double penetrationGeometryEpsilonMeters;
        }

        [Serializable]
        sealed class CoverageFact
        {
            public int landingEventCount;
            public int landingStateBoundaryCount;
            public int landingStateSpanCount;
            public int lockedEventCount;
            public int releaseEventCount;
            public int pathChangeCount;
            public int pathContinuityEventCount;
            public int supportChangeCount;
            public int contactPlanePenetrationEventCount;
            public int leftFootFrameCount;
            public int rightFootFrameCount;
            public int frameGapCount;
            public int bodyResetCount;
            public int sourceChangeCount;
            public int contactPlaneAvailableFootRowCount;
            public int contactPlaneUnavailableFootRowCount;
            public SortedDictionary<string, int>
                contactPlanePenetrationAvailability;
            public int groundPathRejectedFootRowCount;
        }

        [Serializable]
        sealed class EventFact
        {
            internal EventFact(
                string kind,
                string side,
                int startFrame,
                int endFrame,
                int peakFrame,
                ulong eventIdentity,
                string sourceIdentity,
                int sourceCycle,
                double durationSeconds,
                SortedDictionary<string, double> metrics,
                SortedDictionary<string, bool> evidence,
                CharacterFootPathStageAnalysis pathStageAnalysis = null)
            {
                this.kind = kind;
                this.side = side;
                this.startFrame = startFrame;
                this.endFrame = endFrame;
                this.peakFrame = peakFrame;
                this.eventIdentity = eventIdentity.ToString(CultureInfo.InvariantCulture);
                this.sourceIdentity = sourceIdentity;
                this.sourceCycle = sourceCycle;
                this.durationSeconds = durationSeconds;
                this.metrics = metrics;
                this.evidence = evidence;
                this.pathStageAnalysis = pathStageAnalysis;
            }

            public string kind;
            public string side;
            public int startFrame;
            public int endFrame;
            public int peakFrame;
            public string eventIdentity;
            public string sourceIdentity;
            public int sourceCycle;
            public double durationSeconds;
            public SortedDictionary<string, double> metrics;
            public SortedDictionary<string, bool> evidence;
            public CharacterFootPathStageAnalysis pathStageAnalysis;

            internal static int Compare(EventFact left, EventFact right)
            {
                int frame = left.startFrame.CompareTo(right.startFrame);
                if (frame != 0)
                    return frame;
                int side = string.Compare(left.side, right.side, StringComparison.Ordinal);
                return side != 0
                    ? side
                    : string.Compare(left.kind, right.kind, StringComparison.Ordinal);
            }
        }

    }
}
