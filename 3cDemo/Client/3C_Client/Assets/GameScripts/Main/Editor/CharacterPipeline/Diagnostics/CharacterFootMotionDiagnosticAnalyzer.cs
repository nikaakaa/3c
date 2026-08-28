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
        const string Schema = "character-foot-motion-facts/24";
        const string AnalyzerId = "character-foot-motion-fact-analyzer";
        const int AnalyzerVersion = 24;
        const string GeometryFileName = "ground-path-geometry.csv";
        const int HeaderColumnCapacity = 672;
        const float PositionNoiseFloor = 0.001f;
        const float TimeEpsilon = 0.000001f;
        const double LandingReachCompressionReserveMeters = 0.02d;
        const double LowPresentationSamplingDeltaSeconds = 1d / 30d;
        const double SwingSpeedAnomalyMetersPerSecond = 5d;

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
            var stepTimeCandidateSelections =
                new List<StepTimeCandidateSelectionFact>(
                    capture.FootRows.Count);
            AnalyzeSide(
                capture.Left,
                events,
                stepTimeCandidateSelections);
            AnalyzeSide(
                capture.Right,
                events,
                stepTimeCandidateSelections);
            AnalyzeSupportChanges(capture, events);
            events.Sort(EventFact.Compare);
            FactsDocument document = BuildDocument(
                fullSamplesPath,
                geometryPath,
                capture,
                stepTimeCandidateSelections,
                events);
            string factsPath = Path.Combine(
                Path.GetDirectoryName(fullSamplesPath) ?? string.Empty,
                "facts.json");
            PublishFacts(factsPath, document);
            CharacterFootDiagnosisPublication publication =
                CharacterFootDiagnosisPublisher.Publish(factsPath);
            string summary =
                $"frames={capture.UniqueFrameCount} footRows={capture.FootRows.Count} " +
                $"geometryRows={capture.GeometryRowCount} " +
                $"landingEvents={document.coverage.landingEventCount} " +
                $"landingStateBoundaries={document.coverage.landingStateBoundaryCount} " +
                $"landingStateSpans={document.coverage.landingStateSpanCount} " +
                $"lockedEvents={document.coverage.lockedEventCount} " +
                $"releaseEvents={document.coverage.releaseEventCount} " +
                $"pathRevisionOutputJumps={document.coverage.pathRevisionOutputJumpCount} " +
                $"pathContinuityEvents={document.coverage.pathContinuityEventCount} " +
                $"stableSwingOutputJumps={document.coverage.stableSwingOutputJumpCount} " +
                $"swingToLandingOutputJumps={document.coverage.swingToLandingOutputJumpCount} " +
                $"swingToLandingHandoffs={document.coverage.swingToLandingFloorHandoffCount} " +
                $"lateApproachLandingRevisions={document.coverage.lateApproachLandingRevisionCount} " +
                $"supportChanges={document.coverage.supportChangeCount} " +
                $"penetrationEvents={document.coverage.contactPlanePenetrationEventCount} " +
                $"stepTimeCandidateSelections={document.coverage.stepTimeCandidateSelectionCount} " +
                $"stepTimeRepresentativeEvents={document.coverage.stepTimeCandidateRepresentativeEventCount} " +
                $"landingObservations={document.coverage.landingObservationCount} " +
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
            List<EventFact> events,
            List<StepTimeCandidateSelectionFact> stepTimeCandidateSelections)
        {
            if (frames.Count == 0)
                return;
            AnalyzeStepTimeCandidateSelections(
                frames,
                events,
                stepTimeCandidateSelections);
            AnalyzeLandingObservations(frames, events);
            AnalyzeLandingEvents(frames, events);
            AnalyzeLandingStateConsistency(frames, events);
            AnalyzeSwingToLandingFloorHandoffs(frames, events);
            AnalyzeLockedEvents(frames, events);
            AnalyzeContactPlanePenetration(frames, events);
            AnalyzeReleaseEvents(frames, events);
            AnalyzeLateApproachLandingRevisions(frames, events);
            AnalyzeVisibleOutputJumps(frames, events);
            AnalyzePathContinuity(frames, events);
        }

        static void AnalyzeVisibleOutputJumps(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    !previous.FinalPhysicalWriteAvailable ||
                    !current.FinalPhysicalWriteAvailable)
                {
                    continue;
                }
                bool swingToLanding =
                    current.ConstraintStateBefore == "Swing" &&
                    current.ConstraintState == "Landing";
                bool acceptedUnanchoredSwingPair =
                    previous.ConstraintState == "Swing" &&
                    current.ConstraintState == "Swing" &&
                    previous.FootMotionState == "Accepted" &&
                    current.FootMotionState == "Accepted" &&
                    !previous.HasAnchor && !current.HasAnchor;
                if (!swingToLanding && !acceptedUnanchoredSwingPair)
                    continue;
                double pathNoiseFloor = Math.Max(
                    PositionNoiseFloor,
                    current.PathRevisionDistance);
                bool pathAvailabilityChanged =
                    previous.PathAvailableAfter != current.PathAvailableAfter ||
                    RevisionReasonIncludes(
                        current.PathRevisionReason,
                        "PathAvailabilityChanged");
                bool landingEventChanged =
                    previous.NextLandingEventIdentity !=
                    current.NextLandingEventIdentity ||
                    RevisionReasonIncludes(
                        current.PathRevisionReason,
                        "LandingEventChanged");
                bool endpointTreadChanged =
                    previous.NextLandingSurfaceIdentity != 0 &&
                    current.NextLandingSurfaceIdentity != 0 &&
                    previous.NextLandingSurfaceIdentity !=
                    current.NextLandingSurfaceIdentity;
                double endpointDeltaMeters = Vector3.Distance(
                    previous.NextLanding,
                    current.NextLanding);
                bool landingPointRevised =
                    current.PathLandingPointDelta > pathNoiseFloor ||
                    endpointDeltaMeters > pathNoiseFloor;
                CharacterFootSwingTargetCounterfactual counterfactual =
                    acceptedUnanchoredSwingPair
                        ? AnalyzeSwingTargetCounterfactual(previous, current)
                        : null;
                bool counterfactualPathRevision =
                    counterfactual?.available == true &&
                    counterfactual.pathRevisionDelta > pathNoiseFloor;
                bool semanticPathRevision = pathAvailabilityChanged ||
                    landingEventChanged ||
                    endpointTreadChanged ||
                    landingPointRevised ||
                    counterfactualPathRevision;
                bool sameEvent = previous.FootMotionEventIdentity != 0 &&
                    previous.FootMotionEventIdentity ==
                    current.FootMotionEventIdentity;
                bool sameSource = string.Equals(
                    previous.SourceIdentity,
                    current.SourceIdentity,
                    StringComparison.Ordinal) &&
                    previous.SourceCycle == current.SourceCycle;
                string category;
                if (swingToLanding)
                {
                    category = "SwingToLandingOutputJump";
                }
                else if (semanticPathRevision)
                {
                    category = "PathRevisionOutputJump";
                }
                else if (sameEvent && sameSource &&
                         previous.PathAvailableAfter &&
                         current.PathAvailableAfter &&
                         previous.GroundPathState == "Accepted" &&
                         current.GroundPathState == "Accepted")
                {
                    category = "StableSwingOutputJump";
                }
                else
                {
                    continue;
                }
                CharacterFootVisibleOutputKinematics kinematics =
                    ResolveVisibleOutputKinematics(frames, i);
                CharacterFootPathStageAnalysis stageAnalysis =
                    category == "PathRevisionOutputJump"
                        ? BuildPathStageAnalysis(previous, current)
                        : null;
                string primaryProbe = ResolvePrimaryProbe(in kinematics);
                double primaryStep = Math.Max(
                    kinematics.Ankle.StepMeters,
                    Math.Max(
                        kinematics.Heel.StepMeters,
                        kinematics.Toe.StepMeters));
                double primarySpeed = Math.Max(
                    kinematics.Ankle.SpeedMetersPerSecond,
                    Math.Max(
                        kinematics.Heel.SpeedMetersPerSecond,
                        kinematics.Toe.SpeedMetersPerSecond));
                bool accelerationAvailable =
                    kinematics.Ankle.AccelerationAvailable &&
                    kinematics.Heel.AccelerationAvailable &&
                    kinematics.Toe.AccelerationAvailable;
                double primaryAcceleration = accelerationAvailable
                    ? Math.Max(
                        kinematics.Ankle.AccelerationMetersPerSecondSquared,
                        Math.Max(
                            kinematics.Heel.AccelerationMetersPerSecondSquared,
                            kinematics.Toe.AccelerationMetersPerSecondSquared))
                    : 0d;
                bool jerkAvailable = kinematics.Ankle.JerkAvailable &&
                                     kinematics.Heel.JerkAvailable &&
                                     kinematics.Toe.JerkAvailable;
                double primaryJerk = jerkAvailable
                    ? Math.Max(
                        kinematics.Ankle.JerkMetersPerSecondCubed,
                        Math.Max(
                            kinematics.Heel.JerkMetersPerSecondCubed,
                            kinematics.Toe.JerkMetersPerSecondCubed))
                    : 0d;
                var detail = new CharacterFootVisibleOutputJumpAnalysis
                {
                    category = category,
                    previousFrame = previous.Frame,
                    frame = current.Frame,
                    side = current.Side,
                    landingEventIdentity = ResolveEventIdentity(current)
                        .ToString(CultureInfo.InvariantCulture),
                    sourceIdentity = current.SourceIdentity,
                    sourceCycle = current.SourceCycle,
                    previousConstraintState = previous.ConstraintState,
                    constraintStateBefore = current.ConstraintStateBefore,
                    constraintState = current.ConstraintState,
                    preTransitionReason = current.PreTransitionReason,
                    preTransitionSource = current.PreTransitionSource,
                    preTransitionTarget = current.PreTransitionTarget,
                    preTransitionAnchorCommand =
                        current.PreTransitionAnchorCommand,
                    postTransitionReason = current.PostTransitionReason,
                    postTransitionSource = current.PostTransitionSource,
                    postTransitionTarget = current.PostTransitionTarget,
                    postTransitionAnchorCommand =
                        current.PostTransitionAnchorCommand,
                    stateTargetCorrection = CharacterFootVectorFact.From(
                        current.StateTargetCorrection),
                    interpolationPolicy = current.InterpolationPolicy,
                    interpolationOutputCorrection =
                        CharacterFootVectorFact.From(
                            current.InterpolationOutputCorrection),
                    interpolationCompleted = current.InterpolationCompleted,
                    presentationDeltaSeconds = current.DeltaSeconds,
                    bodyTickSpan = current.CurrentBodyTick >=
                                   previous.CurrentBodyTick
                        ? current.CurrentBodyTick - previous.CurrentBodyTick
                        : 0,
                    primaryProbe = primaryProbe,
                    footPlacementOutputOffsetStepMeters = primaryStep,
                    footPlacementOutputOffsetSpeedMetersPerSecond =
                        primarySpeed,
                    accelerationAvailable = accelerationAvailable,
                    footPlacementOutputOffsetAccelerationMetersPerSecondSquared =
                        primaryAcceleration,
                    jerkAvailable = jerkAvailable,
                    footPlacementOutputOffsetJerkMetersPerSecondCubed =
                        primaryJerk,
                    ankle = ProbeFact(in kinematics.Ankle),
                    heel = ProbeFact(in kinematics.Heel),
                    toe = ProbeFact(in kinematics.Toe),
                    safetyFloorOwner = current.SafetyFloorOwner,
                    safetyFloorOwnerSurfaceIdentity =
                        current.SafetyFloorOwnerSurfaceIdentity,
                    safetyFloorOwnerPathIdentity =
                        current.SafetyFloorOwnerPathIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    pathRevisionReason = current.PathRevisionReason,
                    pathNoiseFloorMeters = pathNoiseFloor,
                    endpointDeltaMeters = endpointDeltaMeters,
                    landingPointDeltaMeters =
                        current.PathLandingPointDelta,
                    targetDeltaMeters = current.PathTargetDelta,
                    pathAvailabilityChanged = pathAvailabilityChanged,
                    landingEventChanged = landingEventChanged,
                    endpointTreadChanged = endpointTreadChanged,
                    counterfactualPathRevision =
                        counterfactualPathRevision,
                    pathResidualRebuilt = current.PathResidualRebuilt,
                    swingTargetCounterfactual = counterfactual
                };
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["FootPlacementOutputOffsetStep"] = primaryStep,
                    ["FootPlacementOutputOffsetSpeed"] = primarySpeed,
                    ["FootPlacementOutputOffsetAcceleration"] =
                        primaryAcceleration,
                    ["FootPlacementOutputOffsetJerk"] = primaryJerk,
                    ["AnkleOutputOffsetStep"] =
                        kinematics.Ankle.StepMeters,
                    ["HeelOutputOffsetStep"] =
                        kinematics.Heel.StepMeters,
                    ["ToeOutputOffsetStep"] =
                        kinematics.Toe.StepMeters,
                    ["AnkleOutputOffsetSpeed"] =
                        kinematics.Ankle.SpeedMetersPerSecond,
                    ["HeelOutputOffsetSpeed"] =
                        kinematics.Heel.SpeedMetersPerSecond,
                    ["ToeOutputOffsetSpeed"] =
                        kinematics.Toe.SpeedMetersPerSecond,
                    ["EndpointDelta"] = endpointDeltaMeters,
                    ["LandingPointDelta"] =
                        current.PathLandingPointDelta,
                    ["TargetDelta"] = current.PathTargetDelta,
                    ["PathRevisionDelta"] =
                        counterfactual?.pathRevisionDelta ?? 0d,
                    ["PhaseAdvanceDelta"] =
                        counterfactual?.phaseAdvanceDelta ?? 0d,
                    ["ObservedSwingTargetDelta"] =
                        counterfactual?.observedSwingTargetDelta ?? 0d,
                    ["PresentationDeltaSeconds"] =
                        current.DeltaSeconds,
                    ["BodyTickSpan"] = detail.bodyTickSpan
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["visibleOutputAvailable"] = true,
                    ["accelerationAvailable"] = accelerationAvailable,
                    ["jerkAvailable"] = jerkAvailable,
                    ["stableSwing"] =
                        category == "StableSwingOutputJump",
                    ["pathRevision"] =
                        category == "PathRevisionOutputJump",
                    ["swingToLanding"] =
                        category == "SwingToLandingOutputJump",
                    ["pathAvailabilityChanged"] =
                        pathAvailabilityChanged,
                    ["landingEventChanged"] = landingEventChanged,
                    ["endpointTreadChanged"] = endpointTreadChanged,
                    ["landingPointRevisedAboveNoiseFloor"] =
                        landingPointRevised,
                    ["counterfactualPathRevisionAboveNoiseFloor"] =
                        counterfactualPathRevision,
                    ["safetyFloorOwnerGroundPathEnvelope"] =
                        current.SafetyFloorOwner == "GroundPathEnvelope",
                    ["safetyFloorOwnerContactAnchor"] =
                        current.SafetyFloorOwner == "ContactAnchor"
                };
                events.Add(new EventFact(
                    category,
                    current.Side,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    ResolveEventIdentity(current),
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    stageAnalysis,
                    visibleOutputJump: detail));
            }
        }

        static CharacterFootVisibleOutputKinematics
            ResolveVisibleOutputKinematics(
                List<FootFrame> frames,
                int index) =>
            new CharacterFootVisibleOutputKinematics
            {
                Ankle = ResolveOutputProbeKinematics(frames, index, 0),
                Heel = ResolveOutputProbeKinematics(frames, index, 1),
                Toe = ResolveOutputProbeKinematics(frames, index, 2)
            };

        static CharacterFootOutputProbeKinematics
            ResolveOutputProbeKinematics(
                List<FootFrame> frames,
                int index,
                int probe)
        {
            FootFrame previous = frames[index - 1];
            FootFrame current = frames[index];
            Vector3 previousSource = ResolveSourceProbe(previous, probe);
            Vector3 source = ResolveSourceProbe(current, probe);
            Vector3 previousPhysical = ResolvePhysicalProbe(previous, probe);
            Vector3 physical = ResolvePhysicalProbe(current, probe);
            Vector3 previousOffset = previousPhysical - previousSource;
            Vector3 offset = physical - source;
            Vector3 step = offset - previousOffset;
            float deltaSeconds = (float)DeltaSeconds(current);
            Vector3 velocity = step / deltaSeconds;
            bool accelerationAvailable = TryResolveOutputVelocity(
                frames,
                index - 1,
                probe,
                out Vector3 previousVelocity);
            Vector3 acceleration = accelerationAvailable
                ? (velocity - previousVelocity) / deltaSeconds
                : default;
            Vector3 previousAcceleration = default;
            bool jerkAvailable = accelerationAvailable &&
                TryResolveOutputAcceleration(
                    frames,
                    index - 1,
                    probe,
                    out previousAcceleration);
            Vector3 jerk = jerkAvailable
                ? (acceleration - previousAcceleration) / deltaSeconds
                : default;
            return new CharacterFootOutputProbeKinematics
            {
                PreviousSource = previousSource,
                Source = source,
                PreviousPhysical = previousPhysical,
                Physical = physical,
                PreviousOffset = previousOffset,
                Offset = offset,
                Step = step,
                StepMeters = step.magnitude,
                Velocity = velocity,
                SpeedMetersPerSecond = velocity.magnitude,
                AccelerationAvailable = accelerationAvailable,
                Acceleration = acceleration,
                AccelerationMetersPerSecondSquared =
                    acceleration.magnitude,
                JerkAvailable = jerkAvailable,
                Jerk = jerk,
                JerkMetersPerSecondCubed = jerk.magnitude
            };
        }

        static bool TryResolveOutputVelocity(
            List<FootFrame> frames,
            int index,
            int probe,
            out Vector3 velocity)
        {
            velocity = default;
            if (index < 1)
                return false;
            FootFrame previous = frames[index - 1];
            FootFrame current = frames[index];
            if (!Continuous(previous, current) ||
                !previous.FinalPhysicalWriteAvailable ||
                !current.FinalPhysicalWriteAvailable)
            {
                return false;
            }
            float deltaSeconds = (float)DeltaSeconds(current);
            Vector3 previousOffset =
                ResolvePhysicalProbe(previous, probe) -
                ResolveSourceProbe(previous, probe);
            Vector3 offset = ResolvePhysicalProbe(current, probe) -
                             ResolveSourceProbe(current, probe);
            velocity = (offset - previousOffset) / deltaSeconds;
            return true;
        }

        static bool TryResolveOutputAcceleration(
            List<FootFrame> frames,
            int index,
            int probe,
            out Vector3 acceleration)
        {
            acceleration = default;
            if (index < 2 ||
                !TryResolveOutputVelocity(
                    frames,
                    index,
                    probe,
                    out Vector3 velocity) ||
                !TryResolveOutputVelocity(
                    frames,
                    index - 1,
                    probe,
                    out Vector3 previousVelocity))
            {
                return false;
            }
            acceleration = (velocity - previousVelocity) /
                           (float)DeltaSeconds(frames[index]);
            return true;
        }

        static Vector3 ResolveSourceProbe(FootFrame frame, int probe) =>
            probe switch
            {
                0 => frame.OriginalAnkle,
                1 => frame.SourceHeel,
                2 => frame.SourceToe,
                _ => throw new ArgumentOutOfRangeException(nameof(probe))
            };

        static Vector3 ResolvePhysicalProbe(FootFrame frame, int probe) =>
            probe switch
            {
                0 => FinalPhysicalAnkleWorld(frame),
                1 => frame.FinalHeel,
                2 => frame.FinalToe,
                _ => throw new ArgumentOutOfRangeException(nameof(probe))
            };

        static string ResolvePrimaryProbe(
            in CharacterFootVisibleOutputKinematics value) =>
            value.Ankle.StepMeters >= value.Heel.StepMeters &&
            value.Ankle.StepMeters >= value.Toe.StepMeters
                ? "Ankle"
                : value.Heel.StepMeters >= value.Toe.StepMeters
                    ? "Heel"
                    : "Toe";

        static CharacterFootVisibleOutputProbeFact ProbeFact(
            in CharacterFootOutputProbeKinematics value) =>
            new CharacterFootVisibleOutputProbeFact
            {
                previousAnimatedSource = CharacterFootVectorFact.From(
                    value.PreviousSource),
                animatedSource = CharacterFootVectorFact.From(value.Source),
                previousFinalPhysical = CharacterFootVectorFact.From(
                    value.PreviousPhysical),
                finalPhysical = CharacterFootVectorFact.From(value.Physical),
                previousOutputOffset = CharacterFootVectorFact.From(
                    value.PreviousOffset),
                outputOffset = CharacterFootVectorFact.From(value.Offset),
                outputOffsetStep = CharacterFootVectorFact.From(value.Step),
                outputOffsetStepMeters = value.StepMeters,
                outputOffsetVelocity = CharacterFootVectorFact.From(
                    value.Velocity),
                outputOffsetSpeedMetersPerSecond =
                    value.SpeedMetersPerSecond,
                accelerationAvailable = value.AccelerationAvailable,
                outputOffsetAcceleration = CharacterFootVectorFact.From(
                    value.Acceleration),
                outputOffsetAccelerationMetersPerSecondSquared =
                    value.AccelerationMetersPerSecondSquared,
                jerkAvailable = value.JerkAvailable,
                outputOffsetJerk = CharacterFootVectorFact.From(value.Jerk),
                outputOffsetJerkMetersPerSecondCubed =
                    value.JerkMetersPerSecondCubed
            };

        static void AnalyzeLandingObservations(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            var firstByIdentity = new Dictionary<ulong, FootFrame>();
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame current = frames[i];
                if (current.LandingObservationIdentity == 0)
                    continue;
                firstByIdentity.TryGetValue(
                    current.LandingObservationIdentity,
                    out FootFrame previous);
                bool identitySeenBefore = previous != null;
                bool resultMatchesPrevious = identitySeenBefore &&
                    previous.ObservedLandingAccepted ==
                    current.ObservedLandingAccepted &&
                    previous.ObservedLandingSurfaceIdentity ==
                    current.ObservedLandingSurfaceIdentity &&
                    Vector3.Distance(
                        previous.ObservedLandingPoint,
                        current.ObservedLandingPoint) <= PositionNoiseFloor &&
                    Math.Abs(
                        previous.ObservedLandingQueryDistance -
                        current.ObservedLandingQueryDistance) <= PositionNoiseFloor;
                bool queried = current.LandingObservationCacheState ==
                               "Queried";
                bool reused = current.LandingObservationCacheState ==
                              "Reused";
                bool duplicateQuery = identitySeenBefore &&
                                      current.LandingObservationQueryExecuted;
                bool distanceExceeded =
                    current.LandingObservationQueryInputDistance >
                    current.LandingObservationPredictionInputAccumulationDistance;
                bool angleExceeded =
                    current.LandingObservationQueryComponentUpAngleDegrees >
                    current.LandingObservationComponentUpChangeAngleDegrees;
                bool distanceReason = HasRevisionReason(
                    current.LandingObservationQueryReason,
                    "PredictionInputDistanceExceeded");
                bool angleReason = HasRevisionReason(
                    current.LandingObservationQueryReason,
                    "ComponentUpAngleExceeded");
                bool hasQueryReason = current.LandingObservationQueryReason !=
                                      "None";
                bool queryThresholdContractConsistent =
                    distanceExceeded == distanceReason &&
                    angleExceeded == angleReason &&
                    queried == hasQueryReason &&
                    (!reused || !distanceExceeded && !angleExceeded);
                bool cacheStateConsistent =
                    (queried && current.LandingObservationQueryExecuted ||
                     reused && !current.LandingObservationQueryExecuted) &&
                    !duplicateQuery;
                var detail = new CharacterFootLandingObservationAnalysis
                {
                    previousFrame = previous?.Frame ?? 0,
                    frame = current.Frame,
                    side = current.Side,
                    landingEventIdentity = current.ObservedLandingEventIdentity
                        .ToString(CultureInfo.InvariantCulture),
                    sourceIdentity = current.SourceIdentity,
                    sourceCycle = current.SourceCycle,
                    observationIdentity = current.LandingObservationIdentity
                        .ToString(CultureInfo.InvariantCulture),
                    worldRevision = current.LandingObservationWorldRevision
                        .ToString(CultureInfo.InvariantCulture),
                    sourceSampleIdentity =
                        current.LandingObservationSourceSampleIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    sourceSampleCycle =
                        current.LandingObservationSourceSampleCycle,
                    cacheState = current.LandingObservationCacheState,
                    queryExecutedThisFrame =
                        current.LandingObservationQueryExecuted,
                    queryReason = current.LandingObservationQueryReason,
                    canonicalRawLanding = CharacterFootVectorFact.From(
                        current.LandingObservationCanonicalRaw),
                    canonicalComponentUp = CharacterFootVectorFact.From(
                        current.LandingObservationCanonicalComponentUp),
                    candidateRawLanding = CharacterFootVectorFact.From(
                        current.LandingObservationCandidateRaw),
                    candidateComponentUp = CharacterFootVectorFact.From(
                        current.LandingObservationCandidateComponentUp),
                    queryInputDistanceMeters =
                        current.LandingObservationQueryInputDistance,
                    queryComponentUpAngleDegrees =
                        current.LandingObservationQueryComponentUpAngleDegrees,
                    predictionInputAccumulationDistanceMeters =
                        current.LandingObservationPredictionInputAccumulationDistance,
                    componentUpChangeAngleDegrees =
                        current.LandingObservationComponentUpChangeAngleDegrees,
                    selectionState =
                        current.FutureLandingCandidateSelectionState,
                    validCandidateCount =
                        current.FutureLandingValidCandidateCount,
                    selected = new CharacterFootLandingQueryCandidateFact
                    {
                        available = current.FutureLandingSelectedAvailable,
                        surfaceIdentity =
                            current.FutureLandingSelectedSurfaceIdentity,
                        point = CharacterFootVectorFact.From(
                            current.FutureLandingSelectedPoint),
                        distanceMeters =
                            current.FutureLandingSelectedDistance
                    },
                    identitySeenBefore = identitySeenBefore,
                    resultMatchesPrevious = resultMatchesPrevious,
                    cacheStateConsistent = cacheStateConsistent,
                    queryThresholdContractConsistent =
                        queryThresholdContractConsistent
                };
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["ValidCandidateCount"] =
                        current.FutureLandingValidCandidateCount,
                    ["QueryInputDistance"] =
                        current.LandingObservationQueryInputDistance,
                    ["PredictionInputAccumulationDistance"] =
                        current.LandingObservationPredictionInputAccumulationDistance,
                    ["QueryComponentUpAngleDegrees"] =
                        current.LandingObservationQueryComponentUpAngleDegrees,
                    ["ComponentUpChangeAngleDegrees"] =
                        current.LandingObservationComponentUpChangeAngleDegrees
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["queried"] = queried,
                    ["reused"] = reused,
                    ["queryExecutedThisFrame"] =
                        current.LandingObservationQueryExecuted,
                    ["identitySeenBefore"] = identitySeenBefore,
                    ["resultMatchesPrevious"] = resultMatchesPrevious,
                    ["cacheStateConsistent"] = cacheStateConsistent,
                    ["distanceExceeded"] = distanceExceeded,
                    ["angleExceeded"] = angleExceeded,
                    ["distanceReason"] = distanceReason,
                    ["angleReason"] = angleReason,
                    ["queryThresholdContractConsistent"] =
                        queryThresholdContractConsistent
                };
                events.Add(new EventFact(
                    "LandingObservation",
                    current.Side,
                    current.Frame,
                    current.Frame,
                    current.Frame,
                    current.ObservedLandingEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    landingObservation: detail));
                if (!identitySeenBefore)
                    firstByIdentity.Add(
                        current.LandingObservationIdentity,
                        current);
            }
        }

        static void AnalyzeStepTimeCandidateSelections(
            List<FootFrame> frames,
            List<EventFact> events,
            List<StepTimeCandidateSelectionFact> facts)
        {
            FootFrame previous = null;
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame current = frames[i];
                StepTimeCandidateSelectionFact fact =
                    StepTimeCandidateSelectionFact.From(
                        previous,
                        current);
                facts.Add(fact);
                bool representative = fact.normalizedTimeWrapped ||
                                      fact.selectedSourceChanged ||
                                      fact.selectedLandingEventChanged ||
                                      fact.formalToSelectedTimeDeltaAboveOneMillisecond;
                if (representative)
                {
                    var metrics = new SortedDictionary<string, double>(
                        StringComparer.Ordinal)
                    {
                        ["formalTimeSeconds"] = fact.formalTimeSeconds,
                        ["currentOldTimeSeconds"] =
                            fact.current.timeToLandingSeconds,
                        ["incomingOldTimeSeconds"] =
                            fact.incoming.timeToLandingSeconds,
                        ["maximumPredictionTimeSeconds"] =
                            fact.maximumPredictionTimeSeconds
                    };
                    if (fact.formalToCurrentAbsoluteDeltaSeconds.HasValue)
                    {
                        metrics["formalToCurrentAbsoluteDeltaSeconds"] =
                            fact.formalToCurrentAbsoluteDeltaSeconds.Value;
                    }
                    if (fact.formalToIncomingAbsoluteDeltaSeconds.HasValue)
                    {
                        metrics["formalToIncomingAbsoluteDeltaSeconds"] =
                            fact.formalToIncomingAbsoluteDeltaSeconds.Value;
                    }
                    if (fact.selectedOldTimeSeconds.HasValue)
                        metrics["selectedOldTimeSeconds"] =
                            fact.selectedOldTimeSeconds.Value;
                    if (fact.formalToSelectedAbsoluteDeltaSeconds.HasValue)
                    {
                        metrics["formalToSelectedAbsoluteDeltaSeconds"] =
                            fact.formalToSelectedAbsoluteDeltaSeconds.Value;
                    }
                    var evidence = new SortedDictionary<string, bool>(
                        StringComparer.Ordinal)
                    {
                        ["formalObservationAvailable"] =
                            fact.formalObservationAvailable,
                        ["normalizedTimeWrapped"] =
                            fact.normalizedTimeWrapped,
                        ["selectedSourceChanged"] =
                            fact.selectedSourceChanged,
                        ["selectedLandingEventChanged"] =
                            fact.selectedLandingEventChanged,
                        ["formalToSelectedTimeDeltaAboveOneMillisecond"] =
                            fact.formalToSelectedTimeDeltaAboveOneMillisecond,
                        ["currentTimeConditionEligible"] =
                            fact.current.timeConditionEligible,
                        ["currentOtherConditionsEligible"] =
                            fact.current.otherConditionsEligible,
                        ["currentEligible"] = fact.current.eligible,
                        ["incomingTimeConditionEligible"] =
                            fact.incoming.timeConditionEligible,
                        ["incomingOtherConditionsEligible"] =
                            fact.incoming.otherConditionsEligible,
                        ["incomingEligible"] = fact.incoming.eligible,
                        ["formalCloserToCurrent"] =
                            fact.formalCloserCandidate == "Current",
                        ["formalCloserToIncoming"] =
                            fact.formalCloserCandidate == "Incoming",
                        ["closerCandidateLandingEventDiffersFromLastLanding"] =
                            fact.closerCandidateLandingEventDiffersFromLastLanding
                    };
                    events.Add(new EventFact(
                        "StepTimeCandidateSelection",
                        current.Side,
                        current.Frame,
                        current.Frame,
                        current.Frame,
                        current.SelectedLandingEventIdentity,
                        current.SourceIdentity,
                        current.SourceCycle,
                        current.DeltaSeconds,
                        metrics,
                        evidence));
                }
                previous = current;
            }
        }

        static void AnalyzeLateApproachLandingRevisions(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                bool previousConsumedAvailable =
                    ConsumedNextSwingLandingAvailable(previous);
                bool currentConsumedAvailable =
                    ConsumedNextSwingLandingAvailable(current);
                if (!Continuous(previous, current) ||
                    previous.SelectedLandingEventIdentity == 0 ||
                    previous.SelectedLandingEventIdentity !=
                    current.SelectedLandingEventIdentity ||
                    !previous.SelectedStepInApproachContactToLanding ||
                    !current.SelectedStepInApproachContactToLanding ||
                    !previousConsumedAvailable ||
                    !currentConsumedAvailable ||
                    previous.NextLandingEventIdentity !=
                    previous.SelectedLandingEventIdentity ||
                    current.NextLandingEventIdentity !=
                    current.SelectedLandingEventIdentity)
                {
                    continue;
                }
                double consumedPointDelta = Vector3.Distance(
                    previous.NextLanding,
                    current.NextLanding);
                bool consumedSurfaceChanged =
                    previous.NextLandingSurfaceIdentity !=
                    current.NextLandingSurfaceIdentity;
                bool pointExceededAcceptanceDistance =
                    consumedPointDelta > current.LandingAcceptanceDistance;
                double observedPointDelta =
                    previous.ObservedLandingAccepted &&
                    current.ObservedLandingAccepted
                        ? Vector3.Distance(
                            previous.ObservedLandingPoint,
                            current.ObservedLandingPoint)
                        : 0d;
                double correctionStep = Vector3.Distance(
                    previous.FinalEffectiveCorrection,
                    current.FinalEffectiveCorrection);
                bool componentUpAvailable =
                    current.ComponentUp.sqrMagnitude >
                    TimeEpsilon * TimeEpsilon;
                Vector3 up = componentUpAvailable
                    ? current.ComponentUp.normalized
                    : default;
                bool physicalAvailable =
                    previous.FinalPhysicalWriteAvailable &&
                    current.FinalPhysicalWriteAvailable;
                Vector3 physicalAnkleDelta = physicalAvailable
                    ? FinalPhysicalAnkleWorld(current) -
                      FinalPhysicalAnkleWorld(previous)
                    : default;
                Vector3 physicalSoleDelta = physicalAvailable
                    ? FinalSole(current) - FinalSole(previous)
                    : default;
                double physicalAnkleAlongUpStep =
                    physicalAvailable && componentUpAvailable
                        ? Math.Abs(Vector3.Dot(
                            physicalAnkleDelta,
                            up))
                        : 0d;
                double physicalSoleAlongUpStep =
                    physicalAvailable && componentUpAvailable
                        ? Math.Abs(Vector3.Dot(
                            physicalSoleDelta,
                            up))
                        : 0d;
                var detail =
                    new CharacterFootLateApproachLandingRevisionAnalysis
                    {
                        previousFrame = previous.Frame,
                        frame = current.Frame,
                        side = current.Side,
                        landingEventIdentity =
                            current.SelectedLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousSourceIdentity = previous.SourceIdentity,
                        sourceIdentity = current.SourceIdentity,
                        previousSourceCycle = previous.SourceCycle,
                        sourceCycle = current.SourceCycle,
                        previousContributionContinuityIdentity =
                            previous.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        contributionContinuityIdentity =
                            current.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousSelectedEventPhase =
                            previous.SelectedStepEventPhase,
                        selectedEventPhase =
                            current.SelectedStepEventPhase,
                        previousSelectedApproachContactPhase =
                            previous.SelectedStepApproachContactPhase,
                        selectedApproachContactPhase =
                            current.SelectedStepApproachContactPhase,
                        previousSelectedLandingPhase =
                            previous.SelectedStepLandingPhase,
                        selectedLandingPhase =
                            current.SelectedStepLandingPhase,
                        previousCurrentEventPhase =
                            previous.CurrentStep.EventPhase,
                        currentEventPhase =
                            current.CurrentStep.EventPhase,
                        previousCurrentApproachContactPhase =
                            previous.CurrentStep.ApproachContactPhase,
                        currentApproachContactPhase =
                            current.CurrentStep.ApproachContactPhase,
                        previousSelectedInApproachContactToLanding =
                            previous.SelectedStepInApproachContactToLanding,
                        selectedInApproachContactToLanding =
                            current.SelectedStepInApproachContactToLanding,
                        previousCurrentAtOrAfterApproachContact =
                            previous.CurrentStep.AtOrAfterApproachContact,
                        currentAtOrAfterApproachContact =
                            current.CurrentStep.AtOrAfterApproachContact,
                        previousObservedAvailable =
                            previous.ObservedLandingAccepted,
                        observedAvailable =
                            current.ObservedLandingAccepted,
                        previousObservedEventIdentity =
                            previous.ObservedLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        observedEventIdentity =
                            current.ObservedLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousObservedSurfaceIdentity =
                            previous.ObservedLandingSurfaceIdentity,
                        observedSurfaceIdentity =
                            current.ObservedLandingSurfaceIdentity,
                        previousObservedPoint =
                            CharacterFootVectorFact.From(
                                previous.ObservedLandingPoint),
                        observedPoint = CharacterFootVectorFact.From(
                            current.ObservedLandingPoint),
                        observedLandingPointDeltaMeters =
                            observedPointDelta,
                        previousConsumedEventIdentity =
                            previous.NextLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        consumedEventIdentity =
                            current.NextLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousConsumedSurfaceIdentity =
                            previous.NextLandingSurfaceIdentity,
                        consumedSurfaceIdentity =
                            current.NextLandingSurfaceIdentity,
                        previousConsumedPoint =
                            CharacterFootVectorFact.From(
                                previous.NextLanding),
                        consumedPoint = CharacterFootVectorFact.From(
                            current.NextLanding),
                        landingPointDeltaMeters = consumedPointDelta,
                        landingAcceptanceDistanceMeters =
                            current.LandingAcceptanceDistance,
                        correctionStepMeters = correctionStep,
                        physicalAnkleAvailable = physicalAvailable,
                        physicalAnkleAlongUpStepMeters =
                            physicalAnkleAlongUpStep,
                        physicalSoleAvailable = physicalAvailable,
                        physicalSoleAlongUpStepMeters =
                            physicalSoleAlongUpStep,
                        consumedSurfaceChanged =
                            consumedSurfaceChanged,
                        consumedPointExceededLandingAcceptanceDistance =
                            pointExceededAcceptanceDistance
                    };
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["LandingPointDelta"] = consumedPointDelta,
                    ["ObservedLandingPointDelta"] = observedPointDelta,
                    ["LandingAcceptanceDistance"] =
                        current.LandingAcceptanceDistance,
                    ["CorrectionStep"] = correctionStep,
                    ["PhysicalAnkleAlongUpStep"] =
                        physicalAnkleAlongUpStep,
                    ["PhysicalSoleAlongUpStep"] =
                        physicalSoleAlongUpStep,
                    ["SelectedEventPhase"] =
                        current.SelectedStepEventPhase,
                    ["SelectedApproachContactPhase"] =
                        current.SelectedStepApproachContactPhase,
                    ["CurrentEventPhase"] =
                        current.CurrentStep.EventPhase,
                    ["CurrentApproachContactPhase"] =
                        current.CurrentStep.ApproachContactPhase
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["sameLandingEvent"] = true,
                    ["bothInApproachContactToLanding"] = true,
                    ["consumedLandingAvailable"] = true,
                    ["consumedSurfaceChanged"] =
                        consumedSurfaceChanged,
                    ["consumedPointExceededLandingAcceptanceDistance"] =
                        pointExceededAcceptanceDistance,
                    ["observedLandingAvailable"] =
                        previous.ObservedLandingAccepted &&
                        current.ObservedLandingAccepted,
                    ["physicalAnkleAvailable"] = physicalAvailable,
                    ["physicalSoleAvailable"] = physicalAvailable,
                    ["componentUpAvailable"] = componentUpAvailable,
                    ["sourceChanged"] = previous.SourceIdentity !=
                                        current.SourceIdentity
                };
                events.Add(new EventFact(
                    "LateApproachLandingRevision",
                    current.Side,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    current.SelectedLandingEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    lateApproachLandingRevision: detail));
            }
        }

        static bool ConsumedNextSwingLandingAvailable(
            FootFrame frame) =>
            frame.NextLandingEventIdentity != 0 &&
            frame.NextLandingSurfaceIdentity != 0;

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
                int peakFrame = PeakCorrectionFrame(window);
                FootFrame peak = window.First(
                    frame => frame.Frame == peakFrame);
                LandingReachFact landingReach =
                    LandingReachFact.From(peak);
                var fact = new EventFact(
                    "Landing",
                    current.Side,
                    current.Frame,
                    frames[end].Frame,
                    peakFrame,
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
                        ["targetExtensionRatioPeak"] = targetExtensionPeak,
                        ["landingReachCandidateCompressionReserveMeters"] =
                            landingReach.candidateCompressionReserveMeters,
                        ["landingReachLegLengthMeters"] =
                            landingReach.legLengthMeters,
                        ["landingReachUsableLegLengthMeters"] =
                            landingReach.landingUsableLegLengthMeters,
                        ["landingReachMinimumAlongUpMeters"] =
                            landingReach.landingReachMinimumAlongUpMeters,
                        ["landingReachMaximumAlongUpMeters"] =
                            landingReach.landingReachMaximumAlongUpMeters,
                        ["landingReachStrideSpringOutputMeters"] =
                            landingReach.strideSpringOutputMeters,
                        ["landingReachMinimumCorrectionMeters"] =
                            landingReach.minimumCorrectionMeters,
                        ["landingReachSignedCorrectionAlongUpMeters"] =
                            landingReach.signedCorrectionAlongUpMeters,
                        ["landingReachSupportMinimumAlongUpMeters"] =
                            landingReach.supportReachMinimumAlongUpMeters,
                        ["landingReachSupportMaximumAlongUpMeters"] =
                            landingReach.supportReachMaximumAlongUpMeters,
                        ["landingReachIntersectionMinimumAlongUpMeters"] =
                            landingReach.intersectionMinimumAlongUpMeters,
                        ["landingReachIntersectionMaximumAlongUpMeters"] =
                            landingReach.intersectionMaximumAlongUpMeters,
                        ["landingReachSupportConflictGapMeters"] =
                            landingReach.supportConflictGapMeters,
                        ["landingReachActualTargetCompressionReserveMeters"] =
                            landingReach.actualTargetCompressionReserveMeters
                    },
                    new SortedDictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["bendDirectionReversed"] = bendDirectionMinimum < 0d,
                        ["contactAnchorAvailable"] = window.Any(frame => frame.HasAnchor),
                        ["grounded"] = current.Grounded,
                        ["landingReachAvailable"] =
                            landingReach.landingReachAvailable,
                        ["landingReachCurrentOutputWithinInterval"] =
                            landingReach.currentOutputWithinLandingReach,
                        ["landingReachCorrectionUp"] =
                            landingReach.correctionDirection == "Up",
                        ["landingReachCorrectionDown"] =
                            landingReach.correctionDirection == "Down",
                        ["landingReachCorrectionNone"] =
                            landingReach.correctionDirection == "None",
                        ["landingReachPrimarySupportAvailable"] =
                            landingReach.primarySupportAvailable,
                        ["landingReachSupportReachAvailable"] =
                            landingReach.supportReachAvailable,
                        ["landingReachSupportIntersectionExists"] =
                            landingReach.supportIntersectionExists,
                        ["landingReachNoSupportLandingOnly"] =
                            landingReach.classification ==
                            "NoSupportLandingOnly",
                        ["landingReachSupportIntersection"] =
                            landingReach.classification ==
                            "SupportIntersection",
                        ["landingReachSupportConflict"] =
                            landingReach.classification ==
                            "SupportConflict",
                        ["landingReachUnavailable"] =
                            landingReach.classification ==
                            "LandingReachUnavailable"
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

        static void AnalyzeSwingToLandingFloorHandoffs(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    previous.ConstraintState != "Swing" ||
                    current.ConstraintState != "Landing")
                {
                    continue;
                }
                bool upAvailable =
                    previous.ComponentUp.sqrMagnitude >
                    TimeEpsilon * TimeEpsilon;
                Vector3 up = upAvailable
                    ? previous.ComponentUp.normalized
                    : default;
                Vector3 correctionDelta =
                    current.FinalEffectiveCorrection -
                    previous.FinalEffectiveCorrection;
                double correctionStep = correctionDelta.magnitude;
                double correctionAlongUp = upAvailable
                    ? Vector3.Dot(correctionDelta, up)
                    : 0d;
                bool physicalAvailable =
                    previous.FinalPhysicalWriteAvailable &&
                    current.FinalPhysicalWriteAvailable;
                Vector3 physicalAnkleDelta = physicalAvailable
                    ? FinalPhysicalAnkleWorld(current) -
                      FinalPhysicalAnkleWorld(previous)
                    : default;
                Vector3 physicalSoleDelta = physicalAvailable
                    ? FinalSole(current) - FinalSole(previous)
                    : default;
                double previousResidualAfterDecay =
                    previous.SwingResidualAfterDecay.magnitude;
                bool previousSafetyFloorOwned =
                    previous.SafetyFloorOwner != "None" &&
                    previous.SafetyFloorClamped &&
                    previous.SafetyFloorClampMeters > PositionNoiseFloor;
                bool residualWithinDeadline =
                    previous.SwingResidualTolerance > 0f &&
                    previousResidualAfterDecay <=
                    previous.SwingResidualTolerance + TimeEpsilon;
                Vector3 previousFloorCompensation =
                    previous.SafetyFloorOutputCorrection -
                    previous.CorrectionBeforeSafetyFloor;
                double previousFloorCompensationAlongUp = upAvailable
                    ? Vector3.Dot(previousFloorCompensation, up)
                    : 0d;
                bool floorCompensationDroppedAtLanding =
                    previousSafetyFloorOwned &&
                    current.SafetyFloorOwner !=
                    previous.SafetyFloorOwner &&
                    upAvailable &&
                    correctionAlongUp <=
                    -previousFloorCompensationAlongUp +
                    PositionNoiseFloor;
                double stepHeight = upAvailable &&
                                    previous.GroundPathTargetAvailable
                    ? Vector3.Dot(
                        previous.NextLanding - previous.LastLanding,
                        up)
                    : 0d;
                string stepDirection = stepHeight > PositionNoiseFloor
                    ? "Up"
                    : stepHeight < -PositionNoiseFloor
                        ? "Down"
                        : "Flat";
                var detail =
                    new CharacterFootSwingToLandingFloorHandoffAnalysis
                    {
                        previousFrame = previous.Frame,
                        frame = current.Frame,
                        side = current.Side,
                        eventIdentity = ResolveEventIdentity(current)
                            .ToString(CultureInfo.InvariantCulture),
                        previousSourceIdentity = previous.SourceIdentity,
                        sourceIdentity = current.SourceIdentity,
                        previousSourceCycle = previous.SourceCycle,
                        sourceCycle = current.SourceCycle,
                        previousContributionContinuityIdentity =
                            previous.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        contributionContinuityIdentity =
                            current.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        stateBefore = previous.ConstraintState,
                        stateAfter = current.ConstraintState,
                        entryCorrectionStepMeters = correctionStep,
                        entryCorrectionAlongUpMeters = correctionAlongUp,
                        entryPhysicalAnkleAvailable = physicalAvailable,
                        entryPhysicalAnkleStepMeters =
                            physicalAnkleDelta.magnitude,
                        entryPhysicalAnkleAlongUpMeters = upAvailable &&
                            physicalAvailable
                            ? Vector3.Dot(physicalAnkleDelta, up)
                            : 0d,
                        entryPhysicalSoleAvailable = physicalAvailable,
                        entryPhysicalSoleStepMeters =
                            physicalSoleDelta.magnitude,
                        entryPhysicalSoleAlongUpMeters = upAvailable &&
                            physicalAvailable
                            ? Vector3.Dot(physicalSoleDelta, up)
                            : 0d,
                        previousSafetyFloorClampMeters =
                            previous.SafetyFloorClampMeters,
                        previousSafetyFloorClearanceBeforeMeters =
                            previous.SafetyFloorClearanceBeforeMeters,
                        previousSafetyFloorClearanceAfterMeters =
                            previous.SafetyFloorClearanceAfterMeters,
                        previousResidualAfterDecayMeters =
                            previousResidualAfterDecay,
                        swingResidualToleranceMeters =
                            previous.SwingResidualTolerance,
                        previousFinalEffectiveCorrection =
                            CharacterFootVectorFact.From(
                                previous.FinalEffectiveCorrection),
                        finalEffectiveCorrection =
                            CharacterFootVectorFact.From(
                                current.FinalEffectiveCorrection),
                        previousSafetyFloorMinimumCorrection =
                            CharacterFootVectorFact.From(
                                previous.SafetyFloorMinimumCorrection),
                        previousSafetyFloorOutputCorrection =
                            CharacterFootVectorFact.From(
                                previous.SafetyFloorOutputCorrection),
                        previousSafetyFloorCompensationMeters =
                            previousFloorCompensation.magnitude,
                        previousSafetyFloorCompensationAlongUpMeters =
                            previousFloorCompensationAlongUp,
                        previousSafetyFloorOwner =
                            previous.SafetyFloorOwner,
                        previousSafetyFloorOwnerSurfaceIdentity =
                            previous.SafetyFloorOwnerSurfaceIdentity,
                        previousSafetyFloorOwnerPathIdentity =
                            previous.SafetyFloorOwnerPathIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        safetyFloorOwner = current.SafetyFloorOwner,
                        safetyFloorOwnerSurfaceIdentity =
                            current.SafetyFloorOwnerSurfaceIdentity,
                        safetyFloorOwnerPathIdentity =
                            current.SafetyFloorOwnerPathIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        currentSafetyFloorAvailable =
                            current.SafetyFloorAvailable,
                        currentContactOwnership =
                            current.ContactOwnership,
                        currentContactPlaneAvailable =
                            current.ContactPlaneAvailable,
                        currentContactSurfaceIdentity =
                            current.ContactSurfaceIdentity,
                        stepHeightMeters = stepHeight,
                        stepDirection = stepDirection,
                        previousFormalFootHeightMeters =
                            previous.FormalFootHeight,
                        formalFootHeightMeters = current.FormalFootHeight,
                        previousFormalFootHeightAvailable =
                            previous.FormalOutputObservationAvailable,
                        formalFootHeightAvailable =
                            current.FormalOutputObservationAvailable,
                        previousProgress = previous.SwingProgress,
                        progress = current.SwingProgress,
                        previousTimeToLandingSeconds =
                            previous.TimeToLandingSeconds,
                        timeToLandingSeconds = current.TimeToLandingSeconds,
                        previousSafetyFloorOwned =
                            previousSafetyFloorOwned,
                        residualWithinDeadline = residualWithinDeadline,
                        floorCompensationDroppedAtLanding =
                            floorCompensationDroppedAtLanding
                    };
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["entryCorrectionStepMeters"] = correctionStep,
                    ["entryCorrectionAlongUpMeters"] =
                        correctionAlongUp,
                    ["entryPhysicalAnkleStepMeters"] =
                        physicalAnkleDelta.magnitude,
                    ["entryPhysicalAnkleAlongUpMeters"] =
                        detail.entryPhysicalAnkleAlongUpMeters,
                    ["entryPhysicalSoleStepMeters"] =
                        physicalSoleDelta.magnitude,
                    ["entryPhysicalSoleAlongUpMeters"] =
                        detail.entryPhysicalSoleAlongUpMeters,
                    ["previousSafetyFloorClampMeters"] =
                        previous.SafetyFloorClampMeters,
                    ["previousClearanceBeforeMeters"] =
                        previous.SafetyFloorClearanceBeforeMeters,
                    ["previousClearanceAfterMeters"] =
                        previous.SafetyFloorClearanceAfterMeters,
                    ["previousResidualAfterDecayMeters"] =
                        previousResidualAfterDecay,
                    ["swingResidualToleranceMeters"] =
                        previous.SwingResidualTolerance,
                    ["previousSafetyFloorCompensationMeters"] =
                        previousFloorCompensation.magnitude,
                    ["stepHeightMeters"] = stepHeight,
                    ["previousFormalFootHeightMeters"] =
                        previous.FormalFootHeight,
                    ["formalFootHeightMeters"] =
                        current.FormalFootHeight,
                    ["previousProgress"] = previous.SwingProgress,
                    ["progress"] = current.SwingProgress,
                    ["previousTimeToLandingSeconds"] =
                        previous.TimeToLandingSeconds,
                    ["timeToLandingSeconds"] =
                        current.TimeToLandingSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["stateBeforeSwing"] = true,
                    ["stateAfterLanding"] = true,
                    ["componentUpAvailable"] = upAvailable,
                    ["previousSafetyFloorOwned"] =
                        previousSafetyFloorOwned,
                    ["residualWithinDeadline"] =
                        residualWithinDeadline,
                    ["floorCompensationDroppedAtLanding"] =
                        floorCompensationDroppedAtLanding,
                    ["upStep"] = stepDirection == "Up",
                    ["downStep"] = stepDirection == "Down",
                    ["flatStep"] = stepDirection == "Flat",
                    ["entryPhysicalAnkleAvailable"] =
                        physicalAvailable,
                    ["entryPhysicalSoleAvailable"] =
                        physicalAvailable,
                    ["currentSafetyFloorAvailable"] =
                        current.SafetyFloorAvailable,
                    ["currentContactPlaneAvailable"] =
                        current.ContactPlaneAvailable,
                    ["previousFormalFootHeightAvailable"] =
                        previous.FormalOutputObservationAvailable,
                    ["formalFootHeightAvailable"] =
                        current.FormalOutputObservationAvailable
                };
                events.Add(new EventFact(
                    "SwingToLandingFloorHandoff",
                    current.Side,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    ResolveEventIdentity(current),
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    swingToLandingFloorHandoff: detail));
            }
        }

        static Vector3 FinalSole(FootFrame frame) =>
            (frame.FinalHeel + frame.FinalToe) * 0.5f;

        static Vector3 FinalPhysicalAnkleWorld(FootFrame frame) =>
            frame.PoseRootWorldPosition +
            frame.PoseRootWorldRotation *
            frame.FinalPhysicalAnkleComponentPosition;

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
                    previous.BuilderSwingTargetAvailable &&
                    current.BuilderSwingTargetAvailable,
                    "PathTargetOrSwingTargetUnavailable",
                    previous.NextLanding,
                    current.NextLanding,
                    previous.BuilderSwingTargetCorrection,
                    current.BuilderSwingTargetCorrection,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.BuilderSwingTargetAvailable ||
                    current.BuilderSwingTargetAvailable),
                Stage(
                    CharacterFootPathStageNames.SwingTargetToCapturedResidual,
                    previous.BuilderSwingTargetAvailable &&
                    current.BuilderSwingTargetAvailable &&
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated,
                    "SwingTargetOrCapturedResidualUnavailable",
                    previous.BuilderSwingTargetCorrection,
                    current.BuilderSwingTargetCorrection,
                    previous.SwingResidualBeforeDecay,
                    current.SwingResidualBeforeDecay,
                    previous.Frame,
                    current.Frame,
                    missing,
                    current.PathResidualRebuilt),
                Stage(
                    CharacterFootPathStageNames.CapturedResidualToDecayedResidual,
                    previous.PathContinuityEvaluated &&
                    current.PathContinuityEvaluated,
                    "ResidualDecayFactsUnavailable",
                    previous.SwingResidualBeforeDecay,
                    current.SwingResidualBeforeDecay,
                    previous.SwingResidualAfterDecay,
                    current.SwingResidualAfterDecay,
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
                    previous.SafetyFloorOwner != "None" &&
                    current.SafetyFloorOwner != "None",
                    "StateOutputOrGroundEnvelopeUnavailable",
                    previous.CorrectionBeforeSafetyFloor,
                    current.CorrectionBeforeSafetyFloor,
                    previous.SafetyFloorOutputCorrection,
                    current.SafetyFloorOutputCorrection,
                    previous.Frame,
                    current.Frame,
                    missing,
                    previous.SafetyFloorOwner != "None" ||
                    current.SafetyFloorOwner != "None" ||
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
                targetTrackingApplied = current.TargetTrackingApplied,
                safetyFloorClamped = current.SafetyFloorClamped
            };
            CharacterFootSwingTargetCounterfactual counterfactual =
                AnalyzeSwingTargetCounterfactual(previous, current);
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
            RefineSwingTargetFirstAmplification(
                first,
                counterfactual);
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
                    residualCaptureAvailable =
                        current.PathResidualRebuilt ||
                        current.TargetTrackingApplied,
                    residualBeforeRevisionPrevious = StageVector(
                        previous.SwingResidualBeforeRevision),
                    residualBeforeRevision = StageVector(
                        current.SwingResidualBeforeRevision),
                    capturedResidualPrevious = StageVector(
                        previous.SwingResidualBeforeDecay),
                    capturedResidual = StageVector(
                        current.SwingResidualBeforeDecay),
                    groundEnvelopeSafetyCorrectionAvailable =
                        previous.SafetyFloorOwner == "GroundPathEnvelope" &&
                        current.SafetyFloorOwner == "GroundPathEnvelope",
                    groundEnvelopeSafetyCorrectionPrevious = StageVector(
                        previous.SafetyFloorMinimumCorrection),
                    groundEnvelopeSafetyCorrection = StageVector(
                        current.SafetyFloorMinimumCorrection),
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
                firstAmplificationStage = first,
                swingTargetCounterfactual = counterfactual
            };
            analysis.RequireValid();
            return analysis;
        }

        static CharacterFootSwingTargetCounterfactual
            AnalyzeSwingTargetCounterfactual(
                FootFrame previous,
                FootFrame current)
        {
            if (!previous.BuilderSwingTargetAvailable ||
                !TryReconstructSwingTarget(
                    current,
                    previous,
                    out Vector3 phaseOnlyTarget) ||
                !TryReconstructSwingTarget(
                    current,
                    current,
                    out Vector3 pathRevisedTarget))
            {
                return new CharacterFootSwingTargetCounterfactual
                {
                    available = false,
                    unavailableReason =
                        "SwingTargetReconstructionInputUnavailable"
                };
            }
            Vector3 actualTarget =
                current.BuilderSwingTargetCorrection;
            double reconstructionError = Vector3.Distance(
                pathRevisedTarget,
                actualTarget);
            double phaseDelta = Vector3.Distance(
                previous.BuilderSwingTargetCorrection,
                phaseOnlyTarget);
            double pathDelta = Vector3.Distance(
                phaseOnlyTarget,
                pathRevisedTarget);
            double observedDelta = Vector3.Distance(
                previous.BuilderSwingTargetCorrection,
                actualTarget);
            double totalCounterfactualDelta =
                phaseDelta + pathDelta;
            double phaseContribution =
                totalCounterfactualDelta > PositionNoiseFloor
                    ? phaseDelta / totalCounterfactualDelta
                    : 0d;
            double pathContribution =
                totalCounterfactualDelta > PositionNoiseFloor
                    ? pathDelta / totalCounterfactualDelta
                    : 0d;
            bool reconstructed =
                reconstructionError <= PositionNoiseFloor;
            string classification = !reconstructed
                ? string.Empty
                : pathDelta > PositionNoiseFloor
                    ? "PathRevision"
                    : phaseDelta > PositionNoiseFloor
                        ? "SwingPhaseAdvance"
                        : "SwingTargetStable";
            return new CharacterFootSwingTargetCounterfactual
            {
                available = reconstructed,
                unavailableReason = reconstructed
                    ? string.Empty
                    : "ActualSwingTargetReconstructionMismatch",
                classification = classification,
                phaseOnlyTarget = StageVector(phaseOnlyTarget),
                pathRevisedTarget = StageVector(pathRevisedTarget),
                actualSwingTarget = StageVector(actualTarget),
                actualReconstructionError = reconstructionError,
                phaseAdvanceDelta = phaseDelta,
                pathRevisionDelta = pathDelta,
                observedSwingTargetDelta = observedDelta,
                pathRevisionContribution = pathContribution,
                phaseContribution = phaseContribution
            };
        }

        static bool TryReconstructSwingTarget(
            FootFrame currentState,
            FootFrame path,
            out Vector3 target)
        {
            target = default;
            if (currentState.FootMotionState != "Accepted" ||
                !currentState.BuilderSwingTargetAvailable ||
                path.GroundPathState != "Accepted" ||
                path.GroundEnvelopeVertices.Count < 2 ||
                !float.IsFinite(currentState.SwingProgress) ||
                !currentState.FormalOutputObservationAvailable ||
                !float.IsFinite(
                    currentState.SwingFormalFootHeight) ||
                currentState.ComponentUp.sqrMagnitude <=
                PositionNoiseFloor * PositionNoiseFloor)
            {
                return false;
            }
            Vector3 up = currentState.ComponentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                path.NextLanding - path.LastLanding,
                up);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) ||
                pathLength <= 0.0001f)
            {
                return false;
            }
            float progress = currentState.SwingProgress;
            if (!TrySampleEnvelope(
                    path.GroundEnvelopeVertices.Values,
                    progress,
                    out Vector3 envelopeSample))
            {
                return false;
            }
            float originalSoleHeight = Vector3.Dot(
                currentState.OriginalSole,
                up);
            float formalTargetHeight = Vector3.Dot(
                envelopeSample,
                up) + currentState.SwingFormalFootHeight;
            float formalTargetCorrection =
                formalTargetHeight - originalSoleHeight;
            if (!float.IsFinite(formalTargetHeight) ||
                !float.IsFinite(formalTargetCorrection) ||
                !float.IsFinite(originalSoleHeight))
            {
                return false;
            }
            float verticalCorrection = Mathf.Max(0f, formalTargetCorrection);
            target = up * verticalCorrection;
            return FiniteVector(target);
        }

        static bool TrySampleEnvelope(
            IEnumerable<Vector3> source,
            float progress,
            out Vector3 sample)
        {
            List<Vector3> vertices = source.ToList();
            sample = default;
            if (vertices.Count < 2 ||
                !FiniteVector(vertices[0]))
            {
                return false;
            }
            Vector3 previous = vertices[0];
            float totalLength = 0f;
            for (int i = 1; i < vertices.Count; i++)
            {
                Vector3 current = vertices[i];
                if (!FiniteVector(current))
                    return false;
                float segmentLength = Vector3.Distance(
                    previous,
                    current);
                if (!float.IsFinite(segmentLength))
                    return false;
                totalLength += segmentLength;
                previous = current;
            }
            if (!float.IsFinite(totalLength) ||
                totalLength <= 0.0001f)
            {
                return false;
            }
            float targetDistance =
                Mathf.Clamp01(progress) * totalLength;
            if (targetDistance >= totalLength - 0.0001f)
            {
                sample = vertices[^1];
                return true;
            }
            float accumulatedLength = 0f;
            previous = vertices[0];
            for (int i = 1; i < vertices.Count; i++)
            {
                Vector3 current = vertices[i];
                float segmentLength = Vector3.Distance(
                    previous,
                    current);
                if (segmentLength <= 0.0001f)
                {
                    previous = current;
                    continue;
                }
                if (targetDistance <=
                    accumulatedLength + segmentLength)
                {
                    float t = Mathf.Clamp01(
                        (targetDistance - accumulatedLength) /
                        segmentLength);
                    sample = Vector3.Lerp(
                        previous,
                        current,
                        t);
                    return FiniteVector(sample);
                }
                accumulatedLength += segmentLength;
                previous = current;
            }
            sample = vertices[^1];
            return true;
        }

        static bool FiniteVector(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        static void RefineSwingTargetFirstAmplification(
            CharacterFootPathFirstAmplification first,
            CharacterFootSwingTargetCounterfactual counterfactual)
        {
            if (first?.available != true ||
                first.stage !=
                CharacterFootPathStageNames.PathTargetToSwingTarget)
            {
                return;
            }
            if (counterfactual?.available != true)
            {
                first.available = false;
                first.unavailableReason =
                    counterfactual?.unavailableReason ??
                    "SwingTargetCounterfactualUnavailable";
                first.stage = string.Empty;
                return;
            }
            if (counterfactual.classification ==
                "SwingPhaseAdvance")
            {
                first.stage = "SwingPhaseAdvance";
            }
            else if (counterfactual.classification ==
                     "SwingTargetStable")
            {
                first.stage = "SwingTargetStable";
            }
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
                    current.PathLandingPointDelta > current.PathRevisionDistance;
                bool revisionExpected = availabilityChanged || eventChanged ||
                                        landingPointChanged;
                bool reasonAvailability = HasRevisionReason(
                    current.PathRevisionReason,
                    "PathAvailabilityChanged");
                bool reasonEvent = HasRevisionReason(
                    current.PathRevisionReason,
                    "LandingEventChanged");
                bool reasonLandingPoint = HasRevisionReason(
                    current.PathRevisionReason,
                    "LandingPointChanged");
                bool reasonAvailable = reasonAvailability || reasonEvent ||
                                       reasonLandingPoint;
                bool reasonMatchesExpected =
                    reasonAvailability == availabilityChanged &&
                    reasonEvent == eventChanged &&
                    reasonLandingPoint == landingPointChanged;
                double residualBeforeRevision =
                    current.SwingResidualBeforeRevision.magnitude;
                double residualBeforeDecay =
                    current.SwingResidualBeforeDecay.magnitude;
                double residualAfterDecay =
                    current.SwingResidualAfterDecay.magnitude;
                bool residualGrewWithoutRevision =
                    current.PathContinuityEvaluated &&
                    !current.PathResidualRebuilt &&
                    residualAfterDecay > residualBeforeDecay + PositionNoiseFloor;
                bool deadlineReached = current.PathContinuityEvaluated &&
                    current.ResidualTimeToLandingSeconds > 0f &&
                    current.ResidualTimeToLandingSeconds <=
                    DeltaSeconds(current) + TimeEpsilon;
                bool identityOnlyInputChange = inputIdentityChanged &&
                    current.PathContinuityEvaluated &&
                    !revisionExpected;
                bool relevant = inputIdentityChanged ||
                                current.PathResidualRebuilt ||
                                current.TargetTrackingApplied ||
                                current.SwingVerticalRateLimitApplied ||
                                revisionExpected ||
                                reasonAvailable ||
                                current.ReleasingCompletedToSwing ||
                                current.SafetyFloorClamped ||
                                deadlineReached ||
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
                    ["appliedHalfLifeSeconds"] =
                        current.ResidualAppliedHalfLifeSeconds,
                    ["baseHalfLifeSeconds"] =
                        current.ResidualBaseHalfLifeSeconds,
                    ["correctionStepMeters"] = correctionStep,
                    ["deadlineHalfLifeSeconds"] =
                        current.ResidualDeadlineHalfLifeSeconds,
                    ["safetyFloorClearanceAfterMeters"] =
                        current.SafetyFloorClearanceAfterMeters,
                    ["safetyFloorClearanceBeforeMeters"] =
                        current.SafetyFloorClearanceBeforeMeters,
                    ["landingPointDeltaMeters"] =
                        current.PathLandingPointDelta,
                    ["pathRevisionDistanceMeters"] =
                        current.PathRevisionDistance,
                    ["swingResidualToleranceMeters"] =
                        current.SwingResidualTolerance,
                    ["residualAfterDecayMeters"] = residualAfterDecay,
                    ["residualBeforeDecayMeters"] = residualBeforeDecay,
                    ["residualBeforeRevisionMeters"] = residualBeforeRevision,
                    ["safetyFloorClampMeters"] =
                        current.SafetyFloorClampMeters,
                    ["swingTargetDeltaMeters"] = current.PathTargetDelta,
                    ["swingVerticalRateLimitMeters"] = Vector3.Distance(
                        current.CorrectionBeforeSwingVerticalRateLimit,
                        current.ResidualOutputCorrection),
                    ["swingVerticalCorrectionMaximumSpeed"] =
                        current.SwingVerticalCorrectionMaximumSpeed,
                    ["timeToLandingSeconds"] =
                        current.ResidualTimeToLandingSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["deadlineHalfLifeAvailable"] =
                        current.ResidualDeadlineHalfLifeAvailable,
                    ["deadlineReached"] = deadlineReached,
                    ["safetyFloorAvailable"] = current.SafetyFloorAvailable,
                    ["expectedLandingEventRevision"] = eventChanged,
                    ["expectedLandingPointRevision"] = landingPointChanged,
                    ["expectedPathAvailabilityRevision"] = availabilityChanged,
                    ["identityOnlyInputChange"] = identityOnlyInputChange,
                    ["pathContinuityEvaluated"] =
                        current.PathContinuityEvaluated,
                    ["pathInputIdentityChanged"] = inputIdentityChanged,
                    ["pathResidualRebuilt"] = current.PathResidualRebuilt,
                    ["targetTrackingApplied"] =
                        current.TargetTrackingApplied,
                    ["swingVerticalRateLimitApplied"] =
                        current.SwingVerticalRateLimitApplied,
                    ["pathRevisionExpected"] = revisionExpected,
                    ["pathRevisionReasonMatchesExpected"] =
                        reasonMatchesExpected,
                    ["reasonLandingEventChanged"] = reasonEvent,
                    ["reasonLandingPointChanged"] = reasonLandingPoint,
                    ["reasonPathAvailabilityChanged"] = reasonAvailability,
                    ["releasingCompletedToSwing"] =
                        current.ReleasingCompletedToSwing,
                    ["residualGrewWithoutRevision"] =
                        residualGrewWithoutRevision,
                    ["safetyFloorClamped"] = current.SafetyFloorClamped,
                    ["safetyFloorOwnerGroundPathEnvelope"] =
                        current.SafetyFloorOwner == "GroundPathEnvelope",
                    ["safetyFloorOwnerContactAnchor"] =
                        current.SafetyFloorOwner == "ContactAnchor",
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
            List<StepTimeCandidateSelectionFact> stepTimeCandidateSelections,
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
                    landingReachCandidateCompressionReserveMeters =
                        LandingReachCompressionReserveMeters,
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
                    pathRevisionOutputJumpCount = events.Count(
                        value => value.kind == "PathRevisionOutputJump"),
                    pathContinuityEventCount = events.Count(
                        value => value.kind == "PathContinuity"),
                    stableSwingOutputJumpCount = events.Count(
                        value => value.kind == "StableSwingOutputJump"),
                    swingToLandingOutputJumpCount = events.Count(
                        value => value.kind ==
                                 "SwingToLandingOutputJump"),
                    swingToLandingFloorHandoffCount = events.Count(
                        value => value.kind ==
                                 "SwingToLandingFloorHandoff"),
                    lateApproachLandingRevisionCount = events.Count(
                        value => value.kind ==
                                 "LateApproachLandingRevision"),
                    supportChangeCount = events.Count(value => value.kind == "SupportChange"),
                    contactPlanePenetrationEventCount = events.Count(
                        value => value.kind == "ContactPlanePenetration"),
                    stepTimeCandidateSelectionCount =
                        stepTimeCandidateSelections.Count,
                    stepTimeCandidateRepresentativeEventCount = events.Count(
                        value => value.kind ==
                                 "StepTimeCandidateSelection"),
                    normalizedTimeWrapCount = events.Count(
                        value => value.kind ==
                                     "StepTimeCandidateSelection" &&
                                 value.evidence["normalizedTimeWrapped"]),
                    landingObservationCount = events.Count(
                        value => value.kind == "LandingObservation"),
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
                landingReaches = capture.FootRows
                    .Select(LandingReachFact.From)
                    .ToList(),
                stepTimeCandidateSelections =
                    stepTimeCandidateSelections
                        .OrderBy(value => value.frame)
                        .ThenBy(
                            value => value.side,
                            StringComparer.Ordinal)
                        .ToList(),
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
                if (envelopeIndex >= 0)
                {
                    if (!envelope.Add((frame, side, envelopeIndex)))
                    {
                        throw new InvalidDataException(
                            $"Foot Motion geometry CSV has duplicate Envelope index " +
                            $"Frame={frame} Side={side} Index={envelopeIndex}.");
                    }
                    foot.GroundEnvelopeVertices.Add(
                        envelopeIndex,
                        new Vector3(
                            ParseFloat(
                                Cell("GroundEnvelopeVertexX"),
                                "GroundEnvelopeVertexX"),
                            ParseFloat(
                                Cell("GroundEnvelopeVertexY"),
                                "GroundEnvelopeVertexY"),
                            ParseFloat(
                                Cell("GroundEnvelopeVertexZ"),
                                "GroundEnvelopeVertexZ")));
                }
            }
            foreach (FootFrame foot in footRows.Values)
            {
                if (foot.GroundEnvelopeVertexCount !=
                    foot.GroundEnvelopeVertices.Count)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Envelope geometry count mismatch " +
                        $"Frame={foot.Frame} Side={foot.Side}.");
                }
                RequireActualFootEnvelopeFacts(foot);
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
            Quaternion Rotation(string prefix) => new Quaternion(
                Float(prefix + "X"),
                Float(prefix + "Y"),
                Float(prefix + "Z"),
                Float(prefix + "W"));
            StepCandidateFrame Candidate(string prefix) =>
                new StepCandidateFrame
                {
                    IsValid = Int(prefix + "IsValid") != 0,
                    IsAuthoritative =
                        Int(prefix + "IsAuthoritative") != 0,
                    HasConsistentLandingEventIdentity =
                        Int(prefix +
                            "HasConsistentLandingEventIdentity") != 0,
                    IsPreSwing = Int(prefix + "IsPreSwing") != 0,
                    IsSwing = Int(prefix + "IsSwing") != 0,
                    EventOrdinal = Int(prefix + "EventOrdinal"),
                    SourceLandingCycleOffset =
                        Int(prefix + "SourceLandingCycleOffset"),
                    SourceSampleCycle =
                        Int(prefix + "SourceSampleCycle"),
                    ContributionContinuityIdentity =
                        Ulong(prefix +
                              "ContributionContinuityIdentity"),
                    LandingEventIdentity =
                        Ulong(prefix + "LandingEventIdentity"),
                    TimeToLandingSeconds =
                        Float(prefix + "TimeToLandingSeconds"),
                    EventPhase = Float(prefix + "EventPhase"),
                    ApproachContactPhase =
                        Float(prefix + "ApproachContactPhase"),
                    LandingPhase = Float(prefix + "LandingPhase"),
                    AtOrAfterApproachContact =
                        Int(prefix + "AtOrAfterApproachContact") != 0,
                    InApproachContactToLanding =
                        Int(prefix + "InApproachContactToLanding") != 0,
                    RootLocalLanding =
                        Vector(prefix + "RootLocalLanding")
                };
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
                CurrentBodyTick = Ulong("CurrentBodyTick"),
                Grounded = Int("Grounded") != 0,
                TimeToLandingSeconds = Float("TimeToLandingSeconds"),
                FormalOutputObservationAvailable =
                    Int("FormalStepObservationAvailable") != 0,
                FormalFootHeight = Float("FormalFootHeight"),
                PoseRootWorldPosition = Vector("PoseRootWorldPosition"),
                PoseRootWorldRotation = Rotation("PoseRootWorldRotation"),
                StepSelectionMaximumPredictionTimeSeconds =
                    Float("StepSelectionMaximumPredictionTimeSeconds"),
                StepSelectionLastLandingEventIdentity =
                    Ulong("StepSelectionLastLandingEventIdentity"),
                SelectedStepSource = Cell("SelectedStepSource"),
                SelectedLandingEventIdentity =
                    Ulong("SelectedLandingEventIdentity"),
                SelectedStepEventPhase =
                    Float("SelectedStepEventPhase"),
                SelectedStepApproachContactPhase =
                    Float("SelectedStepApproachContactPhase"),
                SelectedStepLandingPhase =
                    Float("SelectedStepLandingPhase"),
                SelectedStepAtOrAfterApproachContact =
                    Int("SelectedStepAtOrAfterApproachContact") != 0,
                SelectedStepInApproachContactToLanding =
                    Int("SelectedStepInApproachContactToLanding") != 0,
                CurrentStep = Candidate("CurrentStep"),
                IncomingStep = Candidate("IncomingStep"),
                FormalObservationAvailable =
                    Int("InputFormalStepObservationAvailable") != 0,
                SourceIdentity = Cell("InputFormalStepSourceIdentity"),
                SourceCycle = Int("InputFormalStepSourceCycle"),
                ContributionContinuityIdentity = Ulong("InputFormalStepContributionContinuityIdentity"),
                FormalObservationCompletionIdentity =
                    Ulong("InputFormalStepCompletionIdentity"),
                FormalNormalizedTime = Float("InputFormalStepSourceNormalizedTime"),
                FormalStepTime = Float("InputFormalStepTimeSeconds"),
                FormalLockMode = Cell("InputFormalLockMode"),
                FormalLockWeight = Float("InputFormalLockWeight"),
                FormalSupport = Float("InputFormalSupport"),
                FormalCurrentContactEventIdentity =
                    Ulong("InputFormalCurrentContactEventIdentity"),
                FormalNextLandingEventIdentity =
                    Ulong("InputFormalNextLandingEventIdentity"),
                LandingPredictionState = Cell("State"),
                ObservedLandingEventIdentity = Ulong("LandingEventIdentity"),
                ObservedLandingAccepted = Int("Accepted") != 0,
                ObservedLandingSurfaceIdentity = Int("SurfaceIdentity"),
                ObservedLandingPoint = Vector("LandingPoint"),
                ObservedLandingQueryDistance = Float("QueryDistance"),
                LandingObservationIdentity =
                    Ulong("LandingObservationIdentity"),
                LandingObservationWorldRevision =
                    Ulong("LandingObservationWorldRevision"),
                LandingObservationSourceSampleIdentity =
                    Ulong("LandingObservationSourceSampleIdentity"),
                LandingObservationSourceSampleCycle =
                    Int("LandingObservationSourceSampleCycle"),
                LandingObservationCacheState =
                    Cell("LandingObservationCacheState"),
                LandingObservationQueryExecuted =
                    Int("LandingObservationQueryExecuted") != 0,
                LandingObservationQueryReason =
                    Cell("LandingObservationQueryReason"),
                LandingObservationCanonicalRaw =
                    Vector("LandingObservationCanonicalRaw"),
                LandingObservationCanonicalComponentUp =
                    Vector("LandingObservationCanonicalComponentUp"),
                LandingObservationCandidateRaw =
                    Vector("LandingObservationCandidateRaw"),
                LandingObservationCandidateComponentUp =
                    Vector("LandingObservationCandidateComponentUp"),
                LandingObservationQueryInputDistance =
                    Float("LandingObservationQueryInputDistance"),
                LandingObservationQueryComponentUpAngleDegrees =
                    Float("LandingObservationQueryComponentUpAngleDegrees"),
                LandingObservationPredictionInputAccumulationDistance =
                    Float("LandingObservationPredictionInputAccumulationDistance"),
                LandingObservationComponentUpChangeAngleDegrees =
                    Float("LandingObservationComponentUpChangeAngleDegrees"),
                FutureLandingQueryDirection = Vector("QueryDirection"),
                FutureLandingCandidateSelectionState =
                    Cell("QueryCandidateSelectionState"),
                FutureLandingValidCandidateCount =
                    Int("QueryValidCandidateCount"),
                FutureLandingSelectedAvailable =
                    Int("QuerySelectedCandidateAvailable") != 0,
                FutureLandingSelectedSurfaceIdentity =
                    Int("QuerySelectedSurfaceIdentity"),
                FutureLandingSelectedPoint = Vector("QuerySelectedPoint"),
                FutureLandingSelectedDistance =
                    Float("QuerySelectedDistance"),
                RawLandingAvailable = Int("RawLandingAvailable") != 0,
                CurrentAnimatedSole = Vector("CurrentAnimatedSole"),
                RawLanding = Vector("RawLandingCandidate"),
                GroundPathState = Cell("GroundPathState"),
                GroundPathRejectReason = Cell("GroundPathRejectReason"),
                GroundPathInputIdentity = Ulong("GroundPathInputIdentity"),
                GroundPathTargetAvailable =
                    Int("GroundPathTargetAvailable") != 0,
                LastLandingEventIdentity = Ulong("GroundPathLastLandingEventIdentity"),
                NextLandingEventIdentity = Ulong("GroundPathNextSwingLandingEventIdentity"),
                NextLandingSurfaceIdentity =
                    Int("GroundPathNextSwingLandingSurfaceIdentity"),
                LastLanding = Vector("GroundPathLastLanding"),
                NextLanding = Vector("GroundPathNextSwingLanding"),
                GroundEnvelopeVertexCount =
                    Int("GroundEnvelopeVertexCount"),
                ComponentUp = Vector("GroundPathComponentUp"),
                GroundPathRadius = Float("GroundPathRadius"),
                FootMotionEventIdentity = Ulong("FootMotionLandingEventIdentity"),
                FootMotionGroundPathInputIdentity =
                    Ulong("FootMotionGroundPathInputIdentity"),
                FootMotionState = Cell("FootMotionState"),
                ConstraintState = Cell("FootMotionConstraintState"),
                LockResponse = Cell("FootMotionLockResponse"),
                OriginalSole = Vector("FootMotionOriginalSole"),
                OriginalAnkle = Vector("FootMotionOriginalAnkle"),
                SwingProgress = Float("FootMotionProgress"),
                SwingBaselineSample =
                    Vector("FootMotionBaselineSample"),
                SwingBaselineSampleAlongUp =
                    Float("FootMotionBaselineSampleAlongUp"),
                SwingEnvelopeSample =
                    Vector("FootMotionEnvelopeSample"),
                SwingEnvelopeSampleAlongUp =
                    Float("FootMotionEnvelopeSampleAlongUp"),
                SwingFormalFootHeight =
                    Float("FootMotionFormalFootHeight"),
                SwingFormalTargetHeight =
                    Float("FootMotionFormalTargetHeight"),
                SwingFormalTargetCorrection =
                    Float("FootMotionFormalTargetCorrection"),
                SwingEnvelopeMinimumCorrection =
                    Float("FootMotionEnvelopeMinimumCorrection"),
                SwingBuilderSelectedCorrection =
                    Float("FootMotionBuilderSelectedCorrection"),
                BuilderSwingTargetAvailable =
                    Int("FootMotionBuilderSwingTargetAvailable") != 0,
                BuilderSwingTargetCorrection =
                    Vector("FootMotionBuilderSwingTargetCorrection"),
                SwingPathHorizontalAxisState =
                    Cell("FootMotionSwingPathHorizontalAxisState"),
                ActualFootHorizontalDistance =
                    Float("FootMotionActualFootHorizontalDistanceMeters"),
                BaselineHorizontalDistance =
                    Float("FootMotionBaselineHorizontalDistanceMeters"),
                EnvelopeHorizontalDistance =
                    Float("FootMotionEnvelopeHorizontalDistanceMeters"),
                ActualMinusEnvelopeHorizontalDistance =
                    Float("FootMotionActualMinusEnvelopeHorizontalDistanceMeters"),
                ActualFootAxisRegion =
                    Cell("FootMotionActualFootAxisRegion"),
                ActualFootClosestPathParameter =
                    Float("FootMotionActualFootClosestPathParameter"),
                ActualFootDistanceAlongAxis =
                    Float("FootMotionActualFootDistanceAlongAxisMeters"),
                ActualFootCrossTrackDistance =
                    Float("FootMotionActualFootCrossTrackDistanceMeters"),
                ActualFootGroundPathCorridorRadius =
                    Float("FootMotionActualFootGroundPathCorridorRadiusMeters"),
                ActualFootWithinGroundPathCorridor =
                    Int("FootMotionActualFootWithinGroundPathCorridor") != 0,
                ActualEnvelopeIntersectionState =
                    Cell("FootMotionActualEnvelopeIntersectionState"),
                ActualEnvelopeCandidateCount =
                    Int("FootMotionActualEnvelopeCandidateCount"),
                ActualEnvelopeMinimumHeightAlongUp =
                    Float("FootMotionActualEnvelopeMinimumHeightAlongUp"),
                ActualEnvelopeMaximumHeightAlongUp =
                    Float("FootMotionActualEnvelopeMaximumHeightAlongUp"),
                ActualEnvelopeHeightSpan =
                    Float("FootMotionActualEnvelopeHeightSpan"),
                ActualEnvelopeHasVerticalEdge =
                    Int("FootMotionActualEnvelopeHasVerticalEdge") != 0,
                ActualEnvelopeHasMultipleHeights =
                    Int("FootMotionActualEnvelopeHasMultipleHeights") != 0,
                ActualEnvelopeAmbiguous =
                    Int("FootMotionActualEnvelopeAmbiguous") != 0,
                ActualEnvelopeCounterfactualState =
                    Cell("FootMotionActualEnvelopeCounterfactualState"),
                ActualProgressEnvelopeCorrectionAvailable =
                    Int("FootMotionActualProgressEnvelopeCorrectionAvailable") != 0,
                ActualProgressEnvelopeMinimumCorrection =
                    Float("FootMotionActualProgressEnvelopeMinimumCorrection"),
                ActualProgressEnvelopeAdvanceAboveBuilderTarget =
                    Float("FootMotionActualProgressEnvelopeAdvanceAboveBuilderTarget"),
                SwingDesiredCorrection =
                    Vector("FootMotionDesiredCorrection"),
                CorrectedSole = Vector("FootMotionCorrectedSole"),
                CorrectedAnkle = Vector("FootMotionCorrectedAnkle"),
                Anchor = Vector("FootMotionSupportContactAnchor"),
                ContactPlaneAvailable = Int("FootMotionContactPlaneAvailable") != 0,
                ContactOwnership = Float("FootMotionContactOwnership"),
                ContactSurfaceIdentity = Int("FootMotionContactSurfaceIdentity"),
                ContactNormal = Vector("FootMotionContactPlaneNormal"),
                PathContinuityEvaluated =
                    Int("FootMotionPathContinuityEvaluated") != 0,
                PathRevisionReason = Cell("FootMotionPathRevisionReason"),
                PathResidualRebuilt =
                    Int("FootMotionPathResidualRebuilt") != 0,
                TargetTrackingApplied =
                    Int("FootMotionTargetTrackingApplied") != 0,
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
                SwingResidualBeforeDecay =
                    Vector("FootMotionSwingResidualBeforeDecay"),
                SwingResidualAfterDecay =
                    Vector("FootMotionSwingResidualAfterDecay"),
                ResidualOutputCorrection =
                    Vector("FootMotionResidualOutputCorrection"),
                LandingAcceptanceDistance =
                    Float("FootMotionLandingAcceptanceDistance"),
                PathRevisionDistance =
                    Float("FootMotionPathRevisionDistance"),
                SwingResidualTolerance =
                    Float("FootMotionSwingResidualTolerance"),
                ResidualTimeToLandingSeconds =
                    Float("FootMotionResidualTimeToLandingSeconds"),
                ResidualBaseHalfLifeSeconds =
                    Float("FootMotionResidualBaseHalfLifeSeconds"),
                ResidualDeadlineHalfLifeAvailable =
                    Int("FootMotionResidualDeadlineHalfLifeAvailable") != 0,
                ResidualDeadlineHalfLifeSeconds =
                    Float("FootMotionResidualDeadlineHalfLifeSeconds"),
                ResidualAppliedHalfLifeSeconds =
                    Float("FootMotionResidualAppliedHalfLifeSeconds"),
                SwingVerticalCorrectionMaximumSpeed =
                    Float("FootMotionSwingVerticalCorrectionMaximumSpeed"),
                SwingVerticalRateLimitApplied =
                    Int("FootMotionSwingVerticalRateLimitApplied") != 0,
                CorrectionBeforeSwingVerticalRateLimit =
                    Vector("FootMotionCorrectionBeforeSwingVerticalRateLimit"),
                PreTransitionReason = Cell("FootMotionPreTransitionReason"),
                PreTransitionSource = Cell("FootMotionPreTransitionSource"),
                PreTransitionTarget = Cell("FootMotionPreTransitionTarget"),
                PreTransitionAnchorCommand =
                    Cell("FootMotionPreTransitionAnchorCommand"),
                PostTransitionReason = Cell("FootMotionPostTransitionReason"),
                PostTransitionSource = Cell("FootMotionPostTransitionSource"),
                PostTransitionTarget = Cell("FootMotionPostTransitionTarget"),
                PostTransitionAnchorCommand =
                    Cell("FootMotionPostTransitionAnchorCommand"),
                StateTargetCorrection =
                    Vector("FootMotionStateTargetCorrection"),
                InterpolationPolicy = Cell("FootMotionInterpolationPolicy"),
                InterpolationOutputCorrection =
                    Vector("FootMotionInterpolationOutputCorrection"),
                InterpolationCompleted =
                    Int("FootMotionInterpolationCompleted") != 0,
                ConstraintStateBefore = Cell("FootMotionConstraintStateBefore"),
                LockResponseBefore = Cell("FootMotionLockResponseBefore"),
                OutputStagesAvailable =
                    Int("FootMotionOutputStagesAvailable") != 0,
                ReleasingCompletedToSwing =
                    Int("FootMotionReleasingCompletedToSwing") != 0,
                SafetyFloorAvailable = Int("FootMotionSafetyFloorAvailable") != 0,
                SafetyFloorOwner = Cell("FootMotionSafetyFloorOwner"),
                SafetyFloorOwnerSurfaceIdentity =
                    Int("FootMotionSafetyFloorOwnerSurfaceIdentity"),
                SafetyFloorOwnerPathIdentity =
                    Ulong("FootMotionSafetyFloorOwnerPathIdentity"),
                CorrectionBeforeSafetyFloor =
                    Vector("FootMotionCorrectionBeforeSafetyFloor"),
                SafetyFloorMinimumCorrection =
                    Vector("FootMotionSafetyFloorMinimumCorrection"),
                SafetyFloorOutputCorrection =
                    Vector("FootMotionSafetyFloorOutputCorrection"),
                FinalEffectiveCorrection =
                    Vector("FootMotionFinalEffectiveCorrection"),
                SafetyFloorClamped =
                    Int("FootMotionSafetyFloorClamped") != 0,
                SafetyFloorClampMeters =
                    Float("FootMotionSafetyFloorClampMeters"),
                SafetyFloorClearanceBeforeMeters =
                    Float("FootMotionSafetyFloorClearanceBeforeMeters"),
                SafetyFloorClearanceAfterMeters =
                    Float("FootMotionSafetyFloorClearanceAfterMeters"),
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
                FinalIkLegAvailable = Int("FinalIkLegAvailable") != 0,
                FinalIkLegOriginalHip = Vector("FinalIkLegOriginalHip"),
                FinalIkLegOriginalKnee = Vector("FinalIkLegOriginalKnee"),
                FinalIkLegOriginalAnkle = Vector("FinalIkLegOriginalAnkle"),
                FinalIkLegTargetAnkle = Vector("FinalIkLegTargetAnkle"),
                PrimarySupportAvailable =
                    Int("PrimarySupportHasValue") != 0,
                PrimarySupportSide = Cell("PrimarySupportSide"),
                PrimarySupportEventIdentity = Ulong("PrimarySupportLandingEventIdentity"),
                StrideState = Cell("StrideState"),
                StrideSupportSide = Cell("StrideSupportSide"),
                StrideSupportReachAvailable =
                    Int("StrideSupportReachAvailable") != 0,
                StrideSupportReachMinimumAlongUp =
                    Float("StrideSupportReachMinimumAlongUp"),
                StrideSupportReachMaximumAlongUp =
                    Float("StrideSupportReachMaximumAlongUp"),
                StrideSpringOutput = Float("StrideSpringOutput"),
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
            RequireEnum<CharacterFootLandingStepSource>(
                frame.SelectedStepSource,
                "SelectedStepSource");
            bool selectedStepConsistent = frame.SelectedStepSource == "None"
                ? frame.SelectedLandingEventIdentity == 0
                : frame.SelectedStepSource == "FormalCurrentContact"
                    ? frame.SelectedLandingEventIdentity ==
                      frame.FormalCurrentContactEventIdentity
                    : frame.SelectedLandingEventIdentity ==
                      frame.FormalNextLandingEventIdentity;
            if (!selectedStepConsistent ||
                frame.StepSelectionMaximumPredictionTimeSeconds <= 0f)
            {
                throw new InvalidDataException(
                    "Foot Motion Step candidate selection facts are inconsistent.");
            }
            RequireStepPhase(frame.CurrentStep, "CurrentStep");
            RequireStepPhase(frame.IncomingStep, "IncomingStep");
            StepCandidateFrame selected = frame.SelectedStepSource ==
                                          "FormalNextLanding"
                ? frame.CurrentStep
                : null;
            if (selected == null
                    ? frame.SelectedStepEventPhase != 0f ||
                      frame.SelectedStepApproachContactPhase != 0f ||
                      frame.SelectedStepLandingPhase != 0f ||
                      frame.SelectedStepAtOrAfterApproachContact ||
                      frame.SelectedStepInApproachContactToLanding
                    : Math.Abs(
                          frame.SelectedStepEventPhase -
                          selected.EventPhase) > TimeEpsilon ||
                      Math.Abs(
                          frame.SelectedStepApproachContactPhase -
                          selected.ApproachContactPhase) > TimeEpsilon ||
                      Math.Abs(
                          frame.SelectedStepLandingPhase -
                          selected.LandingPhase) > TimeEpsilon ||
                      frame.SelectedStepAtOrAfterApproachContact !=
                      selected.AtOrAfterApproachContact ||
                      frame.SelectedStepInApproachContactToLanding !=
                      selected.InApproachContactToLanding)
            {
                throw new InvalidDataException(
                    "Foot Motion selected Step Phase facts are inconsistent.");
            }
            RequireEnum<CharacterFootLandingPredictionState>(
                frame.LandingPredictionState,
                "State");
            RequireEnum<CharacterFootLandingQueryCandidateSelectionState>(
                frame.FutureLandingCandidateSelectionState,
                "QueryCandidateSelectionState");
            RequireLandingObservation(frame);
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
            RequireEnum<CharacterFootSafetyFloorOwner>(
                frame.SafetyFloorOwner,
                "FootMotionSafetyFloorOwner");
            bool floorOwnerValid = frame.SafetyFloorOwner switch
            {
                "None" => !frame.SafetyFloorAvailable &&
                          frame.SafetyFloorOwnerSurfaceIdentity == 0 &&
                          frame.SafetyFloorOwnerPathIdentity == 0,
                "GroundPathEnvelope" => frame.SafetyFloorAvailable &&
                                        frame.SafetyFloorOwnerSurfaceIdentity == 0 &&
                                        frame.SafetyFloorOwnerPathIdentity != 0,
                "ContactAnchor" => !frame.SafetyFloorAvailable &&
                                   frame.SafetyFloorOwnerSurfaceIdentity != 0 &&
                                   frame.SafetyFloorOwnerPathIdentity == 0 &&
                                   (frame.ConstraintState == "Landing" ||
                                    frame.ConstraintState == "Locked"),
                _ => false
            };
            if (!floorOwnerValid)
            {
                throw new InvalidDataException(
                    "Foot Motion Safety Floor owner facts are inconsistent.");
            }
            RequireEnum<CharacterFootSwingPathHorizontalAxisState>(
                frame.SwingPathHorizontalAxisState,
                "FootMotionSwingPathHorizontalAxisState");
            RequireEnum<CharacterFootActualEnvelopeIntersectionState>(
                frame.ActualEnvelopeIntersectionState,
                "FootMotionActualEnvelopeIntersectionState");
            RequireEnum<CharacterFootActualFootAxisRegion>(
                frame.ActualFootAxisRegion,
                "FootMotionActualFootAxisRegion");
            RequireEnum<CharacterFootActualEnvelopeCounterfactualState>(
                frame.ActualEnvelopeCounterfactualState,
                "FootMotionActualEnvelopeCounterfactualState");
            if (frame.FootMotionState == "Accepted" &&
                frame.ComponentUp.sqrMagnitude >
                TimeEpsilon * TimeEpsilon)
            {
                Vector3 up = frame.ComponentUp.normalized;
                float originalSoleAlongUp = Vector3.Dot(
                    frame.OriginalSole,
                    up);
                float baselineAlongUp = Vector3.Dot(
                    frame.SwingBaselineSample,
                    up);
                float envelopeAlongUp = Vector3.Dot(
                    frame.SwingEnvelopeSample,
                    up);
                float expectedFormalTargetHeight =
                    envelopeAlongUp + frame.SwingFormalFootHeight;
                float expectedFormalTargetCorrection =
                    expectedFormalTargetHeight - originalSoleAlongUp;
                float expectedEnvelopeMinimumCorrection =
                    envelopeAlongUp - originalSoleAlongUp;
                float expectedBuilderSelectedCorrection = Mathf.Max(
                    0f,
                    expectedFormalTargetCorrection);
                if (Math.Abs(
                        baselineAlongUp -
                        frame.SwingBaselineSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        envelopeAlongUp -
                        frame.SwingEnvelopeSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.SwingFormalTargetHeight -
                        expectedFormalTargetHeight) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.SwingFormalTargetCorrection -
                        expectedFormalTargetCorrection) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.SwingEnvelopeMinimumCorrection -
                        expectedEnvelopeMinimumCorrection) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.SwingBuilderSelectedCorrection -
                        expectedBuilderSelectedCorrection) >
                    PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion formal Swing height facts are inconsistent.");
                }
                if (frame.BuilderSwingTargetAvailable)
                {
                    if (!frame.PathContinuityEvaluated ||
                        !frame.PathAvailableAfter ||
                        frame.PathCurrentLandingEventIdentity !=
                        frame.FootMotionEventIdentity ||
                        Vector3.Distance(
                            frame.BuilderSwingTargetCorrection,
                            up * frame.SwingBuilderSelectedCorrection) >
                        PositionNoiseFloor)
                    {
                        throw new InvalidDataException(
                            "Foot Motion Builder Swing target facts are inconsistent.");
                    }
                }
                else if (frame.BuilderSwingTargetCorrection.sqrMagnitude >
                         PositionNoiseFloor * PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion unavailable Builder Swing target is nonzero.");
                }
            }
            if (!float.IsFinite(frame.SwingVerticalCorrectionMaximumSpeed) ||
                frame.SwingVerticalCorrectionMaximumSpeed <= 0f)
            {
                throw new InvalidDataException(
                    "Foot Motion Swing vertical rate limit is invalid.");
            }
            if (frame.PathContinuityEvaluated)
            {
                if (!FiniteVector(
                        frame.CorrectionBeforeSwingVerticalRateLimit))
                {
                    throw new InvalidDataException(
                        "Foot Motion Swing vertical rate input is invalid.");
                }
                Vector3 rateDelta =
                    frame.CorrectionBeforeSwingVerticalRateLimit -
                    frame.ResidualOutputCorrection;
                Vector3 up = frame.ComponentUp.normalized;
                if (Vector3.ProjectOnPlane(rateDelta, up).magnitude >
                    PositionNoiseFloor ||
                    !frame.SwingVerticalRateLimitApplied &&
                    rateDelta.magnitude > PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion Swing vertical rate facts are inconsistent.");
                }
            }
            RequireRevisionReason(frame.PathRevisionReason);
        }

        static void RequireLandingObservation(FootFrame frame)
        {
            bool observationAvailable =
                frame.LandingObservationIdentity != 0;
            if (!observationAvailable)
            {
                if (frame.LandingObservationWorldRevision != 0 ||
                    frame.LandingObservationSourceSampleIdentity != 0 ||
                    frame.LandingObservationCacheState != "Unavailable" ||
                    frame.LandingObservationQueryExecuted ||
                    frame.LandingObservationQueryReason != "None" ||
                    frame.FutureLandingValidCandidateCount != 0 ||
                    frame.FutureLandingSelectedAvailable)
                {
                    throw new InvalidDataException(
                        $"Foot Motion unavailable Landing Observation is inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
                return;
            }
            bool queried = frame.LandingObservationCacheState == "Queried";
            bool reused = frame.LandingObservationCacheState == "Reused";
            if (frame.LandingObservationWorldRevision == 0 ||
                frame.LandingObservationSourceSampleIdentity == 0 ||
                !queried && !reused ||
                queried != frame.LandingObservationQueryExecuted ||
                queried == (frame.LandingObservationQueryReason == "None") ||
                frame.LandingObservationCanonicalComponentUp.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon ||
                frame.LandingObservationCandidateComponentUp.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon ||
                frame.LandingObservationPredictionInputAccumulationDistance <= 0f ||
                frame.LandingObservationComponentUpChangeAngleDegrees <= 0f ||
                frame.FutureLandingQueryDirection.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Landing Observation cache facts are inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
            bool selected = frame.FutureLandingCandidateSelectionState ==
                            "Selected";
            if (!selected)
            {
                if (frame.FutureLandingValidCandidateCount != 0 ||
                    frame.FutureLandingSelectedAvailable ||
                    frame.ObservedLandingAccepted)
                {
                    throw new InvalidDataException(
                        $"Foot Motion unavailable FutureLanding candidates are inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
                return;
            }
            if (frame.FutureLandingValidCandidateCount <= 0 ||
                !frame.FutureLandingSelectedAvailable ||
                frame.FutureLandingSelectedSurfaceIdentity == 0 ||
                !frame.ObservedLandingAccepted ||
                frame.FutureLandingSelectedSurfaceIdentity !=
                frame.ObservedLandingSurfaceIdentity ||
                Vector3.Distance(
                    frame.FutureLandingSelectedPoint,
                    frame.ObservedLandingPoint) > PositionNoiseFloor ||
                Math.Abs(
                    frame.FutureLandingSelectedDistance -
                    frame.ObservedLandingQueryDistance) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    $"Foot Motion selected FutureLanding candidate is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
        }

        static void RequireActualFootEnvelopeFacts(FootFrame frame)
        {
            bool accepted = frame.GroundPathState == "Accepted" &&
                            frame.FootMotionState == "Accepted" &&
                            frame.ConstraintState == "Swing" &&
                            frame.GroundEnvelopeVertices.Count >= 2;
            float valueMagnitude = Math.Max(
                Math.Abs(frame.ActualFootHorizontalDistance),
                Math.Max(
                    Math.Abs(frame.BaselineHorizontalDistance),
                    Math.Max(
                        Math.Abs(frame.EnvelopeHorizontalDistance),
                        Math.Abs(
                            frame.ActualMinusEnvelopeHorizontalDistance))));
            float finiteSegmentMagnitude = Math.Max(
                Math.Abs(frame.ActualFootClosestPathParameter),
                Math.Max(
                    Math.Abs(frame.ActualFootDistanceAlongAxis),
                    Math.Max(
                        Math.Abs(frame.ActualFootCrossTrackDistance),
                        Math.Abs(
                            frame.ActualFootGroundPathCorridorRadius))));
            if (frame.SwingPathHorizontalAxisState == "Unavailable")
            {
                if (accepted ||
                    frame.ActualEnvelopeIntersectionState != "Unavailable" ||
                    frame.ActualFootAxisRegion != "Unavailable" ||
                    frame.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.ActualFootWithinGroundPathCorridor)
                    throw new InvalidDataException(
                        "Foot Motion unavailable Swing Path axis facts are inconsistent.");
                return;
            }
            if (frame.SwingPathHorizontalAxisState == "InvalidComponentUp")
            {
                if (!accepted ||
                    frame.ActualEnvelopeIntersectionState !=
                    "InvalidComponentUp" ||
                    frame.ActualFootAxisRegion != "Unavailable" ||
                    frame.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    frame.ComponentUp.sqrMagnitude > 0.000001f ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.ActualFootWithinGroundPathCorridor)
                {
                    throw new InvalidDataException(
                        "Foot Motion invalid-up Swing Path axis facts are inconsistent.");
                }
                return;
            }
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 horizontalAxis = Vector3.ProjectOnPlane(
                frame.NextLanding - frame.LastLanding,
                up);
            if (frame.SwingPathHorizontalAxisState == "DegenerateAxis")
            {
                if (!accepted ||
                    frame.ActualEnvelopeIntersectionState !=
                    "DegenerateAxis" ||
                    frame.ActualFootAxisRegion != "Unavailable" ||
                    frame.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    horizontalAxis.sqrMagnitude > 0.00000001f ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.ActualFootWithinGroundPathCorridor)
                {
                    throw new InvalidDataException(
                        "Foot Motion degenerate Swing Path axis facts are inconsistent.");
                }
                return;
            }
            if (!accepted ||
                horizontalAxis.sqrMagnitude <= 0.00000001f)
            {
                throw new InvalidDataException(
                    $"Foot Motion available Swing Path axis lacks a valid input " +
                    $"Frame={frame.Frame} Side={frame.Side} " +
                    $"GroundPathState={frame.GroundPathState} " +
                    $"FootMotionState={frame.FootMotionState} " +
                    $"ConstraintState={frame.ConstraintState} " +
                    $"EnvelopeVertices={frame.GroundEnvelopeVertices.Count}.");
            }
            Vector3 direction = horizontalAxis.normalized;
            float expectedActual = Vector3.Dot(
                frame.OriginalSole - frame.LastLanding,
                direction);
            float expectedBaseline = Vector3.Dot(
                frame.SwingBaselineSample - frame.LastLanding,
                direction);
            float expectedEnvelope = Vector3.Dot(
                frame.SwingEnvelopeSample - frame.LastLanding,
                direction);
            if (Math.Abs(
                    frame.ActualFootHorizontalDistance - expectedActual) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.BaselineHorizontalDistance - expectedBaseline) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.EnvelopeHorizontalDistance - expectedEnvelope) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualMinusEnvelopeHorizontalDistance -
                    (expectedActual - expectedEnvelope)) >
                PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Motion Swing Path distance facts are inconsistent.");
            }
            float pathLength = horizontalAxis.magnitude;
            float closestPathParameter = Mathf.Clamp01(
                expectedActual / pathLength);
            float distanceAlongAxis = closestPathParameter * pathLength;
            Vector3 actualHorizontalOffset = Vector3.ProjectOnPlane(
                frame.OriginalSole - frame.LastLanding,
                up);
            float crossTrackDistance = Vector3.Distance(
                actualHorizontalOffset,
                horizontalAxis * closestPathParameter);
            string axisRegion = expectedActual < -PositionNoiseFloor
                ? "BeforePathStart"
                : expectedActual > pathLength + PositionNoiseFloor
                    ? "AfterPathEnd"
                    : "WithinPathSegment";
            bool withinGroundPathCorridor = frame.GroundPathRadius > 0f &&
                crossTrackDistance <=
                frame.GroundPathRadius + PositionNoiseFloor;
            if (frame.ActualFootAxisRegion != axisRegion ||
                Math.Abs(
                    frame.ActualFootClosestPathParameter -
                    closestPathParameter) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualFootDistanceAlongAxis -
                    distanceAlongAxis) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualFootCrossTrackDistance -
                    crossTrackDistance) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualFootGroundPathCorridorRadius -
                    frame.GroundPathRadius) > PositionNoiseFloor ||
                frame.ActualFootWithinGroundPathCorridor !=
                    withinGroundPathCorridor)
            {
                throw new InvalidDataException(
                    "Foot Motion finite Ground Path corridor facts are inconsistent.");
            }
            var heights = new List<float>(
                frame.GroundEnvelopeVertices.Count * 2);
            bool hasVerticalEdge = false;
            List<Vector3> vertices = frame.GroundEnvelopeVertices.Values
                .ToList();
            for (int i = 1; i < vertices.Count; i++)
            {
                Vector3 previous = vertices[i - 1];
                Vector3 current = vertices[i];
                float previousDistance = Vector3.Dot(
                    previous - frame.LastLanding,
                    direction);
                float currentDistance = Vector3.Dot(
                    current - frame.LastLanding,
                    direction);
                float minimumDistance = Mathf.Min(
                    previousDistance,
                    currentDistance);
                float maximumDistance = Mathf.Max(
                    previousDistance,
                    currentDistance);
                if (expectedActual < minimumDistance - PositionNoiseFloor ||
                    expectedActual > maximumDistance + PositionNoiseFloor)
                {
                    continue;
                }
                float previousHeight = Vector3.Dot(previous, up);
                float currentHeight = Vector3.Dot(current, up);
                float distanceDelta = currentDistance - previousDistance;
                if (Mathf.Abs(distanceDelta) <= PositionNoiseFloor)
                {
                    if (Mathf.Abs(expectedActual - previousDistance) >
                        PositionNoiseFloor)
                    {
                        continue;
                    }
                    AddUniqueActualEnvelopeHeight(heights, previousHeight);
                    AddUniqueActualEnvelopeHeight(heights, currentHeight);
                    if (Mathf.Abs(currentHeight - previousHeight) >
                        PositionNoiseFloor)
                    {
                        hasVerticalEdge = true;
                    }
                    continue;
                }
                float interpolation = Mathf.Clamp01(
                    (expectedActual - previousDistance) / distanceDelta);
                AddUniqueActualEnvelopeHeight(
                    heights,
                    Mathf.Lerp(previousHeight, currentHeight, interpolation));
            }
            if (heights.Count == 0)
            {
                string emptyCounterfactualState =
                    withinGroundPathCorridor
                        ? "NoIntersection"
                        : "OutsideGroundPathCorridor";
                if (frame.ActualEnvelopeIntersectionState !=
                        "NoIntersection" ||
                    frame.ActualEnvelopeCounterfactualState !=
                    emptyCounterfactualState ||
                    frame.ActualEnvelopeCandidateCount != 0 ||
                    Math.Abs(
                        frame.ActualEnvelopeMinimumHeightAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.ActualEnvelopeMaximumHeightAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(frame.ActualEnvelopeHeightSpan) >
                    PositionNoiseFloor ||
                    frame.ActualEnvelopeHasVerticalEdge ||
                    frame.ActualEnvelopeHasMultipleHeights ||
                    frame.ActualEnvelopeAmbiguous ||
                    frame.ActualProgressEnvelopeCorrectionAvailable)
                {
                    throw new InvalidDataException(
                        "Foot Motion empty Actual Envelope intersection facts are inconsistent.");
                }
                return;
            }
            float minimumHeight = heights.Min();
            float maximumHeight = heights.Max();
            float heightSpan = maximumHeight - minimumHeight;
            bool hasMultipleHeights = heights.Count > 1 &&
                                      heightSpan > PositionNoiseFloor;
            bool ambiguous = hasVerticalEdge || hasMultipleHeights;
            string expectedState = ambiguous
                ? "AmbiguousEnvelopeAtActualFootDistance"
                : "Unique";
            string expectedCounterfactualState =
                !withinGroundPathCorridor
                    ? "OutsideGroundPathCorridor"
                    : ambiguous
                        ? "AmbiguousInCorridor"
                        : "UniqueInCorridor";
            if (frame.ActualEnvelopeIntersectionState != expectedState ||
                frame.ActualEnvelopeCounterfactualState !=
                expectedCounterfactualState ||
                frame.ActualEnvelopeCandidateCount != heights.Count ||
                Math.Abs(
                    frame.ActualEnvelopeMinimumHeightAlongUp -
                    minimumHeight) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualEnvelopeMaximumHeightAlongUp -
                    maximumHeight) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualEnvelopeHeightSpan - heightSpan) >
                PositionNoiseFloor ||
                frame.ActualEnvelopeHasVerticalEdge != hasVerticalEdge ||
                frame.ActualEnvelopeHasMultipleHeights !=
                hasMultipleHeights ||
                frame.ActualEnvelopeAmbiguous != ambiguous)
            {
                throw new InvalidDataException(
                    "Foot Motion Actual Envelope candidate facts are inconsistent.");
            }
            if (ambiguous)
            {
                if (frame.ActualProgressEnvelopeCorrectionAvailable ||
                    Math.Abs(
                        frame.ActualProgressEnvelopeMinimumCorrection) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.ActualProgressEnvelopeAdvanceAboveBuilderTarget) >
                    PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion ambiguous Actual Envelope produced a correction conclusion.");
                }
                return;
            }
            bool correctionAvailable =
                expectedCounterfactualState == "UniqueInCorridor" &&
                frame.BuilderSwingTargetAvailable;
            float originalSoleAlongUp = Vector3.Dot(frame.OriginalSole, up);
            float minimumCorrection = correctionAvailable
                ? minimumHeight - originalSoleAlongUp
                : 0f;
            float builderTargetAlongUp = correctionAvailable
                ? Vector3.Dot(frame.BuilderSwingTargetCorrection, up)
                : 0f;
            float advanceAboveBuilder = correctionAvailable
                ? Mathf.Max(
                    0f,
                    minimumCorrection - builderTargetAlongUp)
                : 0f;
            if (frame.ActualProgressEnvelopeCorrectionAvailable !=
                    correctionAvailable ||
                Math.Abs(
                    frame.ActualProgressEnvelopeMinimumCorrection -
                    minimumCorrection) > PositionNoiseFloor ||
                Math.Abs(
                    frame.ActualProgressEnvelopeAdvanceAboveBuilderTarget -
                    advanceAboveBuilder) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Motion Actual Envelope counterfactual facts are inconsistent.");
            }
        }

        static void AddUniqueActualEnvelopeHeight(
            List<float> heights,
            float value)
        {
            if (!float.IsFinite(value))
                return;
            for (int i = 0; i < heights.Count; i++)
            {
                if (Mathf.Abs(heights[i] - value) <= PositionNoiseFloor)
                    return;
            }
            heights.Add(value);
        }

        static void RequireStepPhase(
            StepCandidateFrame step,
            string field)
        {
            bool atOrAfter = step.IsValid &&
                             step.EventPhase >=
                             step.ApproachContactPhase;
            bool inRange = atOrAfter &&
                           step.IsSwing &&
                           step.EventPhase <= step.LandingPhase;
            if (step.AtOrAfterApproachContact != atOrAfter ||
                step.InApproachContactToLanding != inRange)
            {
                throw new InvalidDataException(
                    $"Foot Motion {field} Phase facts are inconsistent.");
            }
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
                             reason == "LandingPointChanged";
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
                "CurrentBodyTick",
                "TimeToLandingSeconds", "FormalStepObservationAvailable",
                "FormalFootHeight",
                "PoseRootWorldPositionX", "PoseRootWorldPositionY",
                "PoseRootWorldPositionZ", "PoseRootWorldRotationX",
                "PoseRootWorldRotationY", "PoseRootWorldRotationZ",
                "PoseRootWorldRotationW",
                "InputFormalStepSourceIdentity", "InputFormalStepSourceCycle",
                "InputFormalStepContributionContinuityIdentity",
                "InputFormalStepSourceNormalizedTime", "InputFormalStepTimeSeconds",
                "InputFormalStepObservationAvailable",
                "InputFormalStepCompletionIdentity",
                "InputFormalLockMode", "InputFormalLockWeight", "InputFormalSupport",
                "StepSelectionMaximumPredictionTimeSeconds",
                "StepSelectionLastLandingEventIdentity",
                "SelectedStepSource", "SelectedLandingEventIdentity",
                "SelectedStepEventPhase",
                "SelectedStepApproachContactPhase",
                "SelectedStepLandingPhase",
                "SelectedStepAtOrAfterApproachContact",
                "SelectedStepInApproachContactToLanding",
                "CurrentStepIsValid", "CurrentStepIsAuthoritative",
                "CurrentStepHasConsistentLandingEventIdentity",
                "CurrentStepIsPreSwing", "CurrentStepIsSwing",
                "CurrentStepEventOrdinal",
                "CurrentStepSourceLandingCycleOffset",
                "CurrentStepSourceSampleCycle",
                "CurrentStepContributionContinuityIdentity",
                "CurrentStepLandingEventIdentity",
                "CurrentStepTimeToLandingSeconds",
                "CurrentStepEventPhase",
                "CurrentStepApproachContactPhase",
                "CurrentStepLandingPhase",
                "CurrentStepAtOrAfterApproachContact",
                "CurrentStepInApproachContactToLanding",
                "CurrentStepRootLocalLandingX",
                "CurrentStepRootLocalLandingY",
                "CurrentStepRootLocalLandingZ",
                "IncomingStepIsValid", "IncomingStepIsAuthoritative",
                "IncomingStepHasConsistentLandingEventIdentity",
                "IncomingStepIsPreSwing", "IncomingStepIsSwing",
                "IncomingStepEventOrdinal",
                "IncomingStepSourceLandingCycleOffset",
                "IncomingStepSourceSampleCycle",
                "IncomingStepContributionContinuityIdentity",
                "IncomingStepLandingEventIdentity",
                "IncomingStepTimeToLandingSeconds",
                "IncomingStepEventPhase",
                "IncomingStepApproachContactPhase",
                "IncomingStepLandingPhase",
                "IncomingStepAtOrAfterApproachContact",
                "IncomingStepInApproachContactToLanding",
                "IncomingStepRootLocalLandingX",
                "IncomingStepRootLocalLandingY",
                "IncomingStepRootLocalLandingZ",
                "State", "LandingEventIdentity", "Accepted",
                "SurfaceIdentity", "LandingPointX", "LandingPointY",
                "LandingPointZ", "QueryDistance",
                "LandingObservationIdentity",
                "LandingObservationWorldRevision",
                "LandingObservationSourceSampleIdentity",
                "LandingObservationSourceSampleCycle",
                "LandingObservationCacheState",
                "LandingObservationQueryExecuted",
                "LandingObservationQueryReason",
                "LandingObservationCanonicalRawX",
                "LandingObservationCanonicalRawY",
                "LandingObservationCanonicalRawZ",
                "LandingObservationCanonicalComponentUpX",
                "LandingObservationCanonicalComponentUpY",
                "LandingObservationCanonicalComponentUpZ",
                "LandingObservationCandidateRawX",
                "LandingObservationCandidateRawY",
                "LandingObservationCandidateRawZ",
                "LandingObservationCandidateComponentUpX",
                "LandingObservationCandidateComponentUpY",
                "LandingObservationCandidateComponentUpZ",
                "LandingObservationQueryInputDistance",
                "LandingObservationQueryComponentUpAngleDegrees",
                "LandingObservationPredictionInputAccumulationDistance",
                "LandingObservationComponentUpChangeAngleDegrees",
                "QueryDirectionX", "QueryDirectionY", "QueryDirectionZ",
                "QueryCandidateSelectionState", "QueryValidCandidateCount",
                "QuerySelectedCandidateAvailable",
                "QuerySelectedSurfaceIdentity", "QuerySelectedPointX",
                "QuerySelectedPointY", "QuerySelectedPointZ",
                "QuerySelectedDistance",
                "RawLandingAvailable",
                "CurrentAnimatedSoleX", "CurrentAnimatedSoleY",
                "CurrentAnimatedSoleZ",
                "RawLandingCandidateX", "RawLandingCandidateY", "RawLandingCandidateZ",
                "GroundPathState", "GroundPathRejectReason", "GroundPathInputIdentity",
                "GroundPathTargetAvailable",
                "GroundPathLastLandingEventIdentity", "GroundPathNextSwingLandingEventIdentity",
                "GroundPathNextSwingLandingSurfaceIdentity",
                "GroundPathLastLandingX", "GroundPathLastLandingY",
                "GroundPathLastLandingZ",
                "GroundPathNextSwingLandingX", "GroundPathNextSwingLandingY", "GroundPathNextSwingLandingZ",
                "GroundEnvelopeVertexCount",
                "GroundPathComponentUpX", "GroundPathComponentUpY", "GroundPathComponentUpZ",
                "GroundPathRadius",
                "FootMotionLandingEventIdentity", "FootMotionGroundPathInputIdentity",
                "FootMotionState", "FootMotionConstraintState",
                "FootMotionLockResponse",
                "FootMotionOriginalSoleX", "FootMotionOriginalSoleY", "FootMotionOriginalSoleZ",
                "FootMotionOriginalAnkleX", "FootMotionOriginalAnkleY", "FootMotionOriginalAnkleZ",
                "FootMotionProgress",
                "FootMotionBaselineSampleX", "FootMotionBaselineSampleY",
                "FootMotionBaselineSampleZ", "FootMotionBaselineSampleAlongUp",
                "FootMotionEnvelopeSampleX", "FootMotionEnvelopeSampleY",
                "FootMotionEnvelopeSampleZ",
                "FootMotionEnvelopeSampleAlongUp",
                "FootMotionFormalFootHeight",
                "FootMotionFormalTargetHeight",
                "FootMotionFormalTargetCorrection",
                "FootMotionEnvelopeMinimumCorrection",
                "FootMotionBuilderSelectedCorrection",
                "FootMotionBuilderSwingTargetAvailable",
                "FootMotionBuilderSwingTargetCorrectionX",
                "FootMotionBuilderSwingTargetCorrectionY",
                "FootMotionBuilderSwingTargetCorrectionZ",
                "FootMotionSwingPathHorizontalAxisState",
                "FootMotionActualFootHorizontalDistanceMeters",
                "FootMotionBaselineHorizontalDistanceMeters",
                "FootMotionEnvelopeHorizontalDistanceMeters",
                "FootMotionActualMinusEnvelopeHorizontalDistanceMeters",
                "FootMotionActualFootAxisRegion",
                "FootMotionActualFootClosestPathParameter",
                "FootMotionActualFootDistanceAlongAxisMeters",
                "FootMotionActualFootCrossTrackDistanceMeters",
                "FootMotionActualFootGroundPathCorridorRadiusMeters",
                "FootMotionActualFootWithinGroundPathCorridor",
                "FootMotionActualEnvelopeIntersectionState",
                "FootMotionActualEnvelopeCandidateCount",
                "FootMotionActualEnvelopeMinimumHeightAlongUp",
                "FootMotionActualEnvelopeMaximumHeightAlongUp",
                "FootMotionActualEnvelopeHeightSpan",
                "FootMotionActualEnvelopeHasVerticalEdge",
                "FootMotionActualEnvelopeHasMultipleHeights",
                "FootMotionActualEnvelopeAmbiguous",
                "FootMotionActualEnvelopeCounterfactualState",
                "FootMotionActualProgressEnvelopeCorrectionAvailable",
                "FootMotionActualProgressEnvelopeMinimumCorrection",
                "FootMotionActualProgressEnvelopeAdvanceAboveBuilderTarget",
                "FootMotionDesiredCorrectionX",
                "FootMotionDesiredCorrectionY",
                "FootMotionDesiredCorrectionZ",
                "FootMotionCorrectedSoleX", "FootMotionCorrectedSoleY", "FootMotionCorrectedSoleZ",
                "FootMotionCorrectedAnkleX", "FootMotionCorrectedAnkleY", "FootMotionCorrectedAnkleZ",
                "FootMotionSupportContactAnchorX", "FootMotionSupportContactAnchorY", "FootMotionSupportContactAnchorZ",
                "FootMotionContactOwnership",
                "FootMotionContactPlaneAvailable", "FootMotionContactSurfaceIdentity",
                "FootMotionContactPlaneNormalX", "FootMotionContactPlaneNormalY", "FootMotionContactPlaneNormalZ",
                "FootContactPlanePenetrationAvailability",
                "FootMotionPathContinuityEvaluated", "FootMotionPathRevisionReason",
                "FootMotionPathResidualRebuilt", "FootMotionTargetTrackingApplied",
                "FootMotionPathAvailableBefore",
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
                "FootMotionSwingResidualBeforeDecayX",
                "FootMotionSwingResidualBeforeDecayY",
                "FootMotionSwingResidualBeforeDecayZ",
                "FootMotionSwingResidualAfterDecayX",
                "FootMotionSwingResidualAfterDecayY",
                "FootMotionSwingResidualAfterDecayZ",
                "FootMotionResidualOutputCorrectionX",
                "FootMotionResidualOutputCorrectionY",
                "FootMotionResidualOutputCorrectionZ",
                "FootMotionLandingAcceptanceDistance",
                "FootMotionPathRevisionDistance",
                "FootMotionSwingResidualTolerance",
                "FootMotionResidualTimeToLandingSeconds",
                "FootMotionResidualBaseHalfLifeSeconds",
                "FootMotionResidualDeadlineHalfLifeAvailable",
                "FootMotionResidualDeadlineHalfLifeSeconds",
                "FootMotionResidualAppliedHalfLifeSeconds",
                "FootMotionSwingVerticalCorrectionMaximumSpeed",
                "FootMotionSwingVerticalRateLimitApplied",
                "FootMotionCorrectionBeforeSwingVerticalRateLimitX",
                "FootMotionCorrectionBeforeSwingVerticalRateLimitY",
                "FootMotionCorrectionBeforeSwingVerticalRateLimitZ",
                "FootMotionConstraintStateBefore", "FootMotionLockResponseBefore",
                "FootMotionOutputStagesAvailable",
                "FootMotionReleasingCompletedToSwing",
                "FootMotionSafetyFloorAvailable",
                "FootMotionSafetyFloorOwner",
                "FootMotionSafetyFloorOwnerSurfaceIdentity",
                "FootMotionSafetyFloorOwnerPathIdentity",
                "FootMotionCorrectionBeforeSafetyFloorX",
                "FootMotionCorrectionBeforeSafetyFloorY",
                "FootMotionCorrectionBeforeSafetyFloorZ",
                "FootMotionSafetyFloorMinimumCorrectionX",
                "FootMotionSafetyFloorMinimumCorrectionY",
                "FootMotionSafetyFloorMinimumCorrectionZ",
                "FootMotionSafetyFloorOutputCorrectionX",
                "FootMotionSafetyFloorOutputCorrectionY",
                "FootMotionSafetyFloorOutputCorrectionZ",
                "FootMotionFinalEffectiveCorrectionX",
                "FootMotionFinalEffectiveCorrectionY",
                "FootMotionFinalEffectiveCorrectionZ",
                "FootMotionSafetyFloorClamped", "FootMotionSafetyFloorClampMeters",
                "FootMotionSafetyFloorClearanceBeforeMeters",
                "FootMotionSafetyFloorClearanceAfterMeters",
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
                "FinalIkLegAvailable",
                "FinalIkLegOriginalHipX", "FinalIkLegOriginalHipY",
                "FinalIkLegOriginalHipZ",
                "FinalIkLegOriginalKneeX", "FinalIkLegOriginalKneeY",
                "FinalIkLegOriginalKneeZ",
                "FinalIkLegOriginalAnkleX", "FinalIkLegOriginalAnkleY",
                "FinalIkLegOriginalAnkleZ",
                "FinalIkLegTargetAnkleX", "FinalIkLegTargetAnkleY",
                "FinalIkLegTargetAnkleZ",
                "FinalIkLegTargetExtensionRatio", "FinalIkLegSolvedExtensionRatio",
                "FinalIkLegSolvedBendDegrees", "FinalIkLegTargetCompressionReserve",
                "FinalIkLegEffectiveBendDirectionPreviousDot",
                "PrimarySupportHasValue", "PrimarySupportSide",
                "PrimarySupportLandingEventIdentity",
                "StrideState", "StrideSupportSide",
                "StrideSupportReachAvailable",
                "StrideSupportReachMinimumAlongUp",
                "StrideSupportReachMaximumAlongUp",
                "StrideSpringOutput",
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

        struct CharacterFootVisibleOutputKinematics
        {
            internal CharacterFootOutputProbeKinematics Ankle;
            internal CharacterFootOutputProbeKinematics Heel;
            internal CharacterFootOutputProbeKinematics Toe;
        }

        struct CharacterFootOutputProbeKinematics
        {
            internal Vector3 PreviousSource;
            internal Vector3 Source;
            internal Vector3 PreviousPhysical;
            internal Vector3 Physical;
            internal Vector3 PreviousOffset;
            internal Vector3 Offset;
            internal Vector3 Step;
            internal double StepMeters;
            internal Vector3 Velocity;
            internal double SpeedMetersPerSecond;
            internal bool AccelerationAvailable;
            internal Vector3 Acceleration;
            internal double AccelerationMetersPerSecondSquared;
            internal bool JerkAvailable;
            internal Vector3 Jerk;
            internal double JerkMetersPerSecondCubed;
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
            internal ulong CurrentBodyTick;
            internal bool Grounded;
            internal float TimeToLandingSeconds;
            internal bool FormalOutputObservationAvailable;
            internal float FormalFootHeight;
            internal Vector3 PoseRootWorldPosition;
            internal Quaternion PoseRootWorldRotation;
            internal float StepSelectionMaximumPredictionTimeSeconds;
            internal ulong StepSelectionLastLandingEventIdentity;
            internal string SelectedStepSource;
            internal ulong SelectedLandingEventIdentity;
            internal float SelectedStepEventPhase;
            internal float SelectedStepApproachContactPhase;
            internal float SelectedStepLandingPhase;
            internal bool SelectedStepAtOrAfterApproachContact;
            internal bool SelectedStepInApproachContactToLanding;
            internal StepCandidateFrame CurrentStep;
            internal StepCandidateFrame IncomingStep;
            internal bool FormalObservationAvailable;
            internal string SourceIdentity;
            internal int SourceCycle;
            internal ulong ContributionContinuityIdentity;
            internal ulong FormalObservationCompletionIdentity;
            internal float FormalNormalizedTime;
            internal float FormalStepTime;
            internal string FormalLockMode;
            internal float FormalLockWeight;
            internal float FormalSupport;
            internal ulong FormalCurrentContactEventIdentity;
            internal ulong FormalNextLandingEventIdentity;
            internal string LandingPredictionState;
            internal ulong ObservedLandingEventIdentity;
            internal bool ObservedLandingAccepted;
            internal int ObservedLandingSurfaceIdentity;
            internal Vector3 ObservedLandingPoint;
            internal float ObservedLandingQueryDistance;
            internal ulong LandingObservationIdentity;
            internal ulong LandingObservationWorldRevision;
            internal ulong LandingObservationSourceSampleIdentity;
            internal int LandingObservationSourceSampleCycle;
            internal string LandingObservationCacheState;
            internal bool LandingObservationQueryExecuted;
            internal string LandingObservationQueryReason;
            internal Vector3 LandingObservationCanonicalRaw;
            internal Vector3 LandingObservationCanonicalComponentUp;
            internal Vector3 LandingObservationCandidateRaw;
            internal Vector3 LandingObservationCandidateComponentUp;
            internal float LandingObservationQueryInputDistance;
            internal float LandingObservationQueryComponentUpAngleDegrees;
            internal float LandingObservationPredictionInputAccumulationDistance;
            internal float LandingObservationComponentUpChangeAngleDegrees;
            internal Vector3 FutureLandingQueryDirection;
            internal string FutureLandingCandidateSelectionState;
            internal int FutureLandingValidCandidateCount;
            internal bool FutureLandingSelectedAvailable;
            internal int FutureLandingSelectedSurfaceIdentity;
            internal Vector3 FutureLandingSelectedPoint;
            internal float FutureLandingSelectedDistance;
            internal Vector3 CurrentAnimatedSole;
            internal bool RawLandingAvailable;
            internal Vector3 RawLanding;
            internal string GroundPathState;
            internal string GroundPathRejectReason;
            internal ulong GroundPathInputIdentity;
            internal bool GroundPathTargetAvailable;
            internal ulong LastLandingEventIdentity;
            internal ulong NextLandingEventIdentity;
            internal int NextLandingSurfaceIdentity;
            internal Vector3 LastLanding;
            internal Vector3 NextLanding;
            internal int GroundEnvelopeVertexCount;
            internal Vector3 ComponentUp;
            internal float GroundPathRadius;
            internal readonly SortedDictionary<int, Vector3>
                GroundEnvelopeVertices =
                    new SortedDictionary<int, Vector3>();
            internal ulong FootMotionEventIdentity;
            internal ulong FootMotionGroundPathInputIdentity;
            internal string FootMotionState;
            internal string ConstraintState;
            internal string LockResponse;
            internal Vector3 OriginalSole;
            internal Vector3 OriginalAnkle;
            internal float SwingProgress;
            internal Vector3 SwingBaselineSample;
            internal float SwingBaselineSampleAlongUp;
            internal Vector3 SwingEnvelopeSample;
            internal float SwingEnvelopeSampleAlongUp;
            internal float SwingFormalFootHeight;
            internal float SwingFormalTargetHeight;
            internal float SwingFormalTargetCorrection;
            internal float SwingEnvelopeMinimumCorrection;
            internal float SwingBuilderSelectedCorrection;
            internal bool BuilderSwingTargetAvailable;
            internal Vector3 BuilderSwingTargetCorrection;
            internal string SwingPathHorizontalAxisState;
            internal float ActualFootHorizontalDistance;
            internal float BaselineHorizontalDistance;
            internal float EnvelopeHorizontalDistance;
            internal float ActualMinusEnvelopeHorizontalDistance;
            internal string ActualFootAxisRegion;
            internal float ActualFootClosestPathParameter;
            internal float ActualFootDistanceAlongAxis;
            internal float ActualFootCrossTrackDistance;
            internal float ActualFootGroundPathCorridorRadius;
            internal bool ActualFootWithinGroundPathCorridor;
            internal string ActualEnvelopeIntersectionState;
            internal int ActualEnvelopeCandidateCount;
            internal float ActualEnvelopeMinimumHeightAlongUp;
            internal float ActualEnvelopeMaximumHeightAlongUp;
            internal float ActualEnvelopeHeightSpan;
            internal bool ActualEnvelopeHasVerticalEdge;
            internal bool ActualEnvelopeHasMultipleHeights;
            internal bool ActualEnvelopeAmbiguous;
            internal string ActualEnvelopeCounterfactualState;
            internal bool ActualProgressEnvelopeCorrectionAvailable;
            internal float ActualProgressEnvelopeMinimumCorrection;
            internal float ActualProgressEnvelopeAdvanceAboveBuilderTarget;
            internal Vector3 SwingDesiredCorrection;
            internal Vector3 CorrectedSole;
            internal Vector3 CorrectedAnkle;
            internal Vector3 Anchor;
            internal bool ContactPlaneAvailable;
            internal float ContactOwnership;
            internal int ContactSurfaceIdentity;
            internal Vector3 ContactNormal;
            internal bool PathContinuityEvaluated;
            internal string PathRevisionReason;
            internal bool PathResidualRebuilt;
            internal bool TargetTrackingApplied;
            internal bool PathAvailableBefore;
            internal bool PathAvailableAfter;
            internal ulong PathPreviousLandingEventIdentity;
            internal ulong PathCurrentLandingEventIdentity;
            internal Vector3 PathPreviousTargetCorrection;
            internal Vector3 PathCurrentTargetCorrection;
            internal float PathLandingPointDelta;
            internal float PathTargetDelta;
            internal Vector3 SwingResidualBeforeRevision;
            internal Vector3 SwingResidualBeforeDecay;
            internal Vector3 SwingResidualAfterDecay;
            internal Vector3 ResidualOutputCorrection;
            internal float LandingAcceptanceDistance;
            internal float PathRevisionDistance;
            internal float SwingResidualTolerance;
            internal float ResidualTimeToLandingSeconds;
            internal float ResidualBaseHalfLifeSeconds;
            internal bool ResidualDeadlineHalfLifeAvailable;
            internal float ResidualDeadlineHalfLifeSeconds;
            internal float ResidualAppliedHalfLifeSeconds;
            internal float SwingVerticalCorrectionMaximumSpeed;
            internal bool SwingVerticalRateLimitApplied;
            internal Vector3 CorrectionBeforeSwingVerticalRateLimit;
            internal string PreTransitionReason;
            internal string PreTransitionSource;
            internal string PreTransitionTarget;
            internal string PreTransitionAnchorCommand;
            internal string PostTransitionReason;
            internal string PostTransitionSource;
            internal string PostTransitionTarget;
            internal string PostTransitionAnchorCommand;
            internal Vector3 StateTargetCorrection;
            internal string InterpolationPolicy;
            internal Vector3 InterpolationOutputCorrection;
            internal bool InterpolationCompleted;
            internal string ConstraintStateBefore;
            internal string LockResponseBefore;
            internal bool OutputStagesAvailable;
            internal bool ReleasingCompletedToSwing;
            internal bool SafetyFloorAvailable;
            internal string SafetyFloorOwner;
            internal int SafetyFloorOwnerSurfaceIdentity;
            internal ulong SafetyFloorOwnerPathIdentity;
            internal Vector3 CorrectionBeforeSafetyFloor;
            internal Vector3 SafetyFloorMinimumCorrection;
            internal Vector3 SafetyFloorOutputCorrection;
            internal Vector3 FinalEffectiveCorrection;
            internal bool SafetyFloorClamped;
            internal float SafetyFloorClampMeters;
            internal float SafetyFloorClearanceBeforeMeters;
            internal float SafetyFloorClearanceAfterMeters;
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
            internal bool FinalIkLegAvailable;
            internal Vector3 FinalIkLegOriginalHip;
            internal Vector3 FinalIkLegOriginalKnee;
            internal Vector3 FinalIkLegOriginalAnkle;
            internal Vector3 FinalIkLegTargetAnkle;
            internal bool PrimarySupportAvailable;
            internal string PrimarySupportSide;
            internal ulong PrimarySupportEventIdentity;
            internal string StrideState;
            internal string StrideSupportSide;
            internal bool StrideSupportReachAvailable;
            internal float StrideSupportReachMinimumAlongUp;
            internal float StrideSupportReachMaximumAlongUp;
            internal float StrideSpringOutput;
            internal float PelvisWeight;
            internal Vector3 FinalPelvisGoal;
            internal Vector3 PhysicalPelvis;
            internal Vector3 EffectiveCorrection => CorrectedAnkle - OriginalAnkle;
        }

        static bool RevisionReasonIncludes(
            string value,
            string expected) =>
            value.Split(',').Any(
                reason => string.Equals(
                    reason.Trim(),
                    expected,
                    StringComparison.Ordinal));

        [Serializable]
        sealed class LandingReachFact
        {
            public int frame;
            public string side;
            public string availability;
            public string classification;
            public double candidateCompressionReserveMeters;
            public bool finalIkLegAvailable;
            public ScalarVector3Fact componentUp;
            public ScalarVector3Fact originalHip;
            public ScalarVector3Fact originalKnee;
            public ScalarVector3Fact originalAnkle;
            public ScalarVector3Fact targetAnkle;
            public ScalarVector3Fact baselineHipBeforePelvisOutput;
            public double appliedPelvisGoalAlongUpMeters;
            public double upperLegLengthMeters;
            public double lowerLegLengthMeters;
            public double legLengthMeters;
            public double landingUsableLegLengthMeters;
            public double hipTargetHorizontalDistanceMeters;
            public double hipTargetVerticalAlongUpMeters;
            public bool landingReachAvailable;
            public double landingReachMinimumAlongUpMeters;
            public double landingReachMaximumAlongUpMeters;
            public double strideSpringOutputMeters;
            public bool currentOutputWithinLandingReach;
            public double minimumCorrectionMeters;
            public double signedCorrectionAlongUpMeters;
            public string correctionDirection;
            public double actualTargetCompressionReserveMeters;
            public bool primarySupportAvailable;
            public string primarySupportSide;
            public string primarySupportLandingEventIdentity;
            public string strideState;
            public string strideSupportSide;
            public bool supportReachAvailable;
            public double supportReachMinimumAlongUpMeters;
            public double supportReachMaximumAlongUpMeters;
            public bool supportIntersectionExists;
            public double intersectionMinimumAlongUpMeters;
            public double intersectionMaximumAlongUpMeters;
            public double supportConflictGapMeters;

            internal static LandingReachFact From(FootFrame frame)
            {
                var result = new LandingReachFact
                {
                    frame = frame.Frame,
                    side = frame.Side,
                    availability = "None",
                    classification = "LandingReachUnavailable",
                    candidateCompressionReserveMeters =
                        LandingReachCompressionReserveMeters,
                    finalIkLegAvailable = frame.FinalIkLegAvailable,
                    componentUp = ScalarVector3Fact.From(frame.ComponentUp),
                    originalHip = ScalarVector3Fact.From(
                        frame.FinalIkLegOriginalHip),
                    originalKnee = ScalarVector3Fact.From(
                        frame.FinalIkLegOriginalKnee),
                    originalAnkle = ScalarVector3Fact.From(
                        frame.FinalIkLegOriginalAnkle),
                    targetAnkle = ScalarVector3Fact.From(
                        frame.FinalIkLegTargetAnkle),
                    baselineHipBeforePelvisOutput =
                        ScalarVector3Fact.From(
                            frame.FinalIkLegOriginalHip),
                    strideSpringOutputMeters = frame.StrideSpringOutput,
                    actualTargetCompressionReserveMeters =
                        frame.TargetCompressionReserve,
                    primarySupportAvailable =
                        frame.PrimarySupportAvailable,
                    primarySupportSide = frame.PrimarySupportSide,
                    primarySupportLandingEventIdentity =
                        frame.PrimarySupportEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    strideState = frame.StrideState,
                    strideSupportSide = frame.StrideSupportSide,
                    supportReachAvailable =
                        frame.StrideSupportReachAvailable,
                    supportReachMinimumAlongUpMeters =
                        frame.StrideSupportReachMinimumAlongUp,
                    supportReachMaximumAlongUpMeters =
                        frame.StrideSupportReachMaximumAlongUp,
                    correctionDirection = "Unavailable"
                };
                if (!frame.FinalIkLegAvailable)
                {
                    result.availability = "FinalIkLegUnavailable";
                    return result;
                }
                if (frame.ComponentUp.sqrMagnitude <=
                    TimeEpsilon * TimeEpsilon)
                {
                    result.availability = "ComponentUpUnavailable";
                    return result;
                }
                Vector3 up = frame.ComponentUp.normalized;
                double upperLength = Vector3.Distance(
                    frame.FinalIkLegOriginalHip,
                    frame.FinalIkLegOriginalKnee);
                double lowerLength = Vector3.Distance(
                    frame.FinalIkLegOriginalKnee,
                    frame.FinalIkLegOriginalAnkle);
                double legLength = upperLength + lowerLength;
                double usableLegLength = legLength -
                    LandingReachCompressionReserveMeters;
                result.upperLegLengthMeters = upperLength;
                result.lowerLegLengthMeters = lowerLength;
                result.legLengthMeters = legLength;
                result.landingUsableLegLengthMeters = usableLegLength;
                if (!double.IsFinite(usableLegLength) ||
                    usableLegLength <= TimeEpsilon)
                {
                    result.availability = "UsableLegLengthUnavailable";
                    return result;
                }
                double appliedPelvisAlongUp = Vector3.Dot(
                    frame.FinalPelvisGoal,
                    up) * frame.PelvisWeight;
                Vector3 baselineHip = frame.FinalIkLegOriginalHip -
                    up * (float)appliedPelvisAlongUp;
                result.appliedPelvisGoalAlongUpMeters =
                    appliedPelvisAlongUp;
                result.baselineHipBeforePelvisOutput =
                    ScalarVector3Fact.From(baselineHip);
                Vector3 hipFromTarget =
                    baselineHip - frame.FinalIkLegTargetAnkle;
                double vertical = Vector3.Dot(hipFromTarget, up);
                Vector3 horizontal = Vector3.ProjectOnPlane(
                    hipFromTarget,
                    up);
                double horizontalSquare = horizontal.sqrMagnitude;
                result.hipTargetHorizontalDistanceMeters =
                    Math.Sqrt(horizontalSquare);
                result.hipTargetVerticalAlongUpMeters = vertical;
                double usableSquare = usableLegLength * usableLegLength;
                if (!double.IsFinite(horizontalSquare) ||
                    horizontalSquare >= usableSquare)
                {
                    result.availability = "HorizontalTargetUnreachable";
                    return result;
                }
                double verticalReach = Math.Sqrt(
                    usableSquare - horizontalSquare);
                double minimum = -vertical - verticalReach;
                double maximum = -vertical + verticalReach;
                if (!double.IsFinite(minimum) ||
                    !double.IsFinite(maximum) ||
                    minimum > maximum)
                {
                    result.availability = "LandingIntervalUnavailable";
                    return result;
                }
                result.availability = "Available";
                result.landingReachAvailable = true;
                result.landingReachMinimumAlongUpMeters = minimum;
                result.landingReachMaximumAlongUpMeters = maximum;
                double output = frame.StrideSpringOutput;
                double signedCorrection = output < minimum
                    ? minimum - output
                    : output > maximum
                        ? maximum - output
                        : 0d;
                result.currentOutputWithinLandingReach =
                    signedCorrection == 0d;
                result.signedCorrectionAlongUpMeters = signedCorrection;
                result.minimumCorrectionMeters =
                    Math.Abs(signedCorrection);
                result.correctionDirection = signedCorrection > 0d
                    ? "Up"
                    : signedCorrection < 0d
                        ? "Down"
                        : "None";
                if (!frame.StrideSupportReachAvailable)
                {
                    result.classification = "NoSupportLandingOnly";
                    return result;
                }
                double intersectionMinimum = Math.Max(
                    minimum,
                    frame.StrideSupportReachMinimumAlongUp);
                double intersectionMaximum = Math.Min(
                    maximum,
                    frame.StrideSupportReachMaximumAlongUp);
                result.intersectionMinimumAlongUpMeters =
                    intersectionMinimum;
                result.intersectionMaximumAlongUpMeters =
                    intersectionMaximum;
                result.supportIntersectionExists =
                    intersectionMinimum <= intersectionMaximum;
                result.supportConflictGapMeters = Math.Max(
                    0d,
                    intersectionMinimum - intersectionMaximum);
                result.classification = result.supportIntersectionExists
                    ? "SupportIntersection"
                    : "SupportConflict";
                return result;
            }
        }

        sealed class StepCandidateFrame
        {
            internal bool IsValid;
            internal bool IsAuthoritative;
            internal bool HasConsistentLandingEventIdentity;
            internal bool IsPreSwing;
            internal bool IsSwing;
            internal int EventOrdinal;
            internal int SourceLandingCycleOffset;
            internal int SourceSampleCycle;
            internal ulong ContributionContinuityIdentity;
            internal ulong LandingEventIdentity;
            internal float TimeToLandingSeconds;
            internal float EventPhase;
            internal float ApproachContactPhase;
            internal float LandingPhase;
            internal bool AtOrAfterApproachContact;
            internal bool InApproachContactToLanding;
            internal Vector3 RootLocalLanding;
        }

        [Serializable]
        sealed class StepTimeCandidateSelectionFact
        {
            public int frame;
            public ulong completionIdentity;
            public string side;
            public bool formalObservationAvailable;
            public string formalSourceIdentity;
            public int formalSourceCycle;
            public string formalContributionContinuityIdentity;
            public string formalCompletionIdentity;
            public double formalNormalizedTime;
            public double formalTimeSeconds;
            public double maximumPredictionTimeSeconds;
            public string lastLandingEventIdentity;
            public string selectedSource;
            public string selectedLandingEventIdentity;
            public double selectedEventPhase;
            public double selectedApproachContactPhase;
            public double selectedLandingPhase;
            public bool selectedAtOrAfterApproachContact;
            public bool selectedInApproachContactToLanding;
            public StepTimeCandidateFact current;
            public StepTimeCandidateFact incoming;
            public double? selectedOldTimeSeconds;
            public double? formalToCurrentAbsoluteDeltaSeconds;
            public double? formalToIncomingAbsoluteDeltaSeconds;
            public double? formalToSelectedAbsoluteDeltaSeconds;
            public string formalCloserCandidate;
            public bool closerCandidateAvailable;
            public string closerCandidateLandingEventIdentity;
            public int closerCandidateSourceSampleCycle;
            public int closerCandidateSourceLandingCycleOffset;
            public bool closerCandidateLandingEventDiffersFromLastLanding;
            public bool normalizedTimeWrapped;
            public bool selectedSourceChanged;
            public bool selectedLandingEventChanged;
            public bool formalToSelectedTimeDeltaAboveOneMillisecond;

            internal static StepTimeCandidateSelectionFact From(
                FootFrame previous,
                FootFrame frame)
            {
                StepTimeCandidateFact current =
                    StepTimeCandidateFact.From(
                        frame.CurrentStep,
                        frame.StepSelectionLastLandingEventIdentity,
                        frame.StepSelectionMaximumPredictionTimeSeconds);
                StepTimeCandidateFact incoming =
                    StepTimeCandidateFact.From(
                        frame.IncomingStep,
                        frame.StepSelectionLastLandingEventIdentity,
                        frame.StepSelectionMaximumPredictionTimeSeconds);
                double? currentDelta = frame.FormalObservationAvailable
                    ? Math.Abs(
                        frame.FormalStepTime -
                        frame.CurrentStep.TimeToLandingSeconds)
                    : null;
                double? incomingDelta = frame.FormalObservationAvailable
                    ? Math.Abs(
                        frame.FormalStepTime -
                        frame.IncomingStep.TimeToLandingSeconds)
                    : null;
                double? selectedOldTime =
                    frame.SelectedStepSource == "FormalNextLanding"
                        ? frame.CurrentStep.TimeToLandingSeconds
                        : null;
                double? selectedDelta = frame.FormalObservationAvailable &&
                                        selectedOldTime.HasValue
                    ? Math.Abs(frame.FormalStepTime - selectedOldTime.Value)
                    : null;
                string closer = "Unavailable";
                StepCandidateFrame closerFrame = null;
                if (currentDelta.HasValue && incomingDelta.HasValue)
                {
                    if (Math.Abs(
                            currentDelta.Value -
                            incomingDelta.Value) <= TimeEpsilon)
                    {
                        closer = "Equal";
                    }
                    else if (currentDelta.Value < incomingDelta.Value)
                    {
                        closer = "Current";
                        closerFrame = frame.CurrentStep;
                    }
                    else
                    {
                        closer = "Incoming";
                        closerFrame = frame.IncomingStep;
                    }
                }
                bool sameFormalSource = previous != null &&
                                        previous.FormalObservationAvailable &&
                                        frame.FormalObservationAvailable &&
                                        previous.SourceIdentity ==
                                        frame.SourceIdentity;
                return new StepTimeCandidateSelectionFact
                {
                    frame = frame.Frame,
                    completionIdentity = frame.CompletionIdentity,
                    side = frame.Side,
                    formalObservationAvailable =
                        frame.FormalObservationAvailable,
                    formalSourceIdentity = frame.SourceIdentity,
                    formalSourceCycle = frame.SourceCycle,
                    formalContributionContinuityIdentity =
                        frame.ContributionContinuityIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    formalCompletionIdentity =
                        frame.FormalObservationCompletionIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    formalNormalizedTime = frame.FormalNormalizedTime,
                    formalTimeSeconds = frame.FormalStepTime,
                    maximumPredictionTimeSeconds =
                        frame.StepSelectionMaximumPredictionTimeSeconds,
                    lastLandingEventIdentity =
                        frame.StepSelectionLastLandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    selectedSource = frame.SelectedStepSource,
                    selectedLandingEventIdentity =
                        frame.SelectedLandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    selectedEventPhase = frame.SelectedStepEventPhase,
                    selectedApproachContactPhase =
                        frame.SelectedStepApproachContactPhase,
                    selectedLandingPhase =
                        frame.SelectedStepLandingPhase,
                    selectedAtOrAfterApproachContact =
                        frame.SelectedStepAtOrAfterApproachContact,
                    selectedInApproachContactToLanding =
                        frame.SelectedStepInApproachContactToLanding,
                    current = current,
                    incoming = incoming,
                    selectedOldTimeSeconds = selectedOldTime,
                    formalToCurrentAbsoluteDeltaSeconds = currentDelta,
                    formalToIncomingAbsoluteDeltaSeconds = incomingDelta,
                    formalToSelectedAbsoluteDeltaSeconds = selectedDelta,
                    formalCloserCandidate = closer,
                    closerCandidateAvailable = closerFrame != null,
                    closerCandidateLandingEventIdentity = closerFrame == null
                        ? "0"
                        : closerFrame.LandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    closerCandidateSourceSampleCycle =
                        closerFrame?.SourceSampleCycle ?? 0,
                    closerCandidateSourceLandingCycleOffset =
                        closerFrame?.SourceLandingCycleOffset ?? 0,
                    closerCandidateLandingEventDiffersFromLastLanding =
                        closerFrame != null &&
                        closerFrame.LandingEventIdentity !=
                        frame.StepSelectionLastLandingEventIdentity,
                    normalizedTimeWrapped = sameFormalSource &&
                        frame.FormalNormalizedTime + TimeEpsilon <
                        previous.FormalNormalizedTime,
                    selectedSourceChanged = previous != null &&
                        frame.SelectedStepSource != previous.SelectedStepSource,
                    selectedLandingEventChanged = previous != null &&
                        frame.SelectedLandingEventIdentity !=
                        previous.SelectedLandingEventIdentity,
                    formalToSelectedTimeDeltaAboveOneMillisecond =
                        selectedDelta.HasValue &&
                        selectedDelta.Value > 0.001d
                };
            }
        }

        [Serializable]
        sealed class StepTimeCandidateFact
        {
            public bool isValid;
            public bool isAuthoritative;
            public bool hasConsistentLandingEventIdentity;
            public bool isPreSwing;
            public bool isSwing;
            public int eventOrdinal;
            public int sourceLandingCycleOffset;
            public int sourceSampleCycle;
            public string contributionContinuityIdentity;
            public string landingEventIdentity;
            public double timeToLandingSeconds;
            public double eventPhase;
            public double approachContactPhase;
            public double landingPhase;
            public bool atOrAfterApproachContact;
            public bool inApproachContactToLanding;
            public ScalarVector3Fact rootLocalLanding;
            public bool positiveTime;
            public bool withinMaximumPredictionTime;
            public bool timeConditionEligible;
            public bool landingEventDiffersFromLastLanding;
            public bool otherConditionsEligible;
            public bool eligible;

            internal static StepTimeCandidateFact From(
                StepCandidateFrame source,
                ulong lastLandingEventIdentity,
                float maximumPredictionTimeSeconds)
            {
                bool positiveTime =
                    source.TimeToLandingSeconds > TimeEpsilon;
                bool withinMaximumPredictionTime =
                    source.TimeToLandingSeconds <=
                    maximumPredictionTimeSeconds;
                bool timeConditionEligible =
                    positiveTime && withinMaximumPredictionTime;
                bool landingEventDiffersFromLastLanding =
                    source.LandingEventIdentity !=
                    lastLandingEventIdentity;
                bool otherConditionsEligible =
                    source.IsAuthoritative &&
                    source.HasConsistentLandingEventIdentity &&
                    (source.IsPreSwing || source.IsSwing) &&
                    landingEventDiffersFromLastLanding;
                return new StepTimeCandidateFact
                {
                    isValid = source.IsValid,
                    isAuthoritative = source.IsAuthoritative,
                    hasConsistentLandingEventIdentity =
                        source.HasConsistentLandingEventIdentity,
                    isPreSwing = source.IsPreSwing,
                    isSwing = source.IsSwing,
                    eventOrdinal = source.EventOrdinal,
                    sourceLandingCycleOffset =
                        source.SourceLandingCycleOffset,
                    sourceSampleCycle = source.SourceSampleCycle,
                    contributionContinuityIdentity =
                        source.ContributionContinuityIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    landingEventIdentity =
                        source.LandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    timeToLandingSeconds =
                        source.TimeToLandingSeconds,
                    eventPhase = source.EventPhase,
                    approachContactPhase =
                        source.ApproachContactPhase,
                    landingPhase = source.LandingPhase,
                    atOrAfterApproachContact =
                        source.AtOrAfterApproachContact,
                    inApproachContactToLanding =
                        source.InApproachContactToLanding,
                    rootLocalLanding = ScalarVector3Fact.From(
                        source.RootLocalLanding),
                    positiveTime = positiveTime,
                    withinMaximumPredictionTime =
                        withinMaximumPredictionTime,
                    timeConditionEligible = timeConditionEligible,
                    landingEventDiffersFromLastLanding =
                        landingEventDiffersFromLastLanding,
                    otherConditionsEligible = otherConditionsEligible,
                    eligible = timeConditionEligible &&
                               otherConditionsEligible
                };
            }
        }

        [Serializable]
        sealed class FactsDocument
        {
            public string schema;
            public SampleFact sample;
            public AnalyzerFact analyzer;
            public CoverageFact coverage;
            public List<LandingReachFact> landingReaches;
            public List<StepTimeCandidateSelectionFact>
                stepTimeCandidateSelections;
            public List<EventFact> events;
        }

        [Serializable]
        sealed class ScalarVector3Fact
        {
            public float x;
            public float y;
            public float z;

            internal static ScalarVector3Fact From(Vector3 value) =>
                new ScalarVector3Fact
                {
                    x = value.x,
                    y = value.y,
                    z = value.z
                };
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
            public double landingReachCandidateCompressionReserveMeters;
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
            public int pathRevisionOutputJumpCount;
            public int pathContinuityEventCount;
            public int stableSwingOutputJumpCount;
            public int swingToLandingOutputJumpCount;
            public int swingToLandingFloorHandoffCount;
            public int lateApproachLandingRevisionCount;
            public int supportChangeCount;
            public int contactPlanePenetrationEventCount;
            public int stepTimeCandidateSelectionCount;
            public int stepTimeCandidateRepresentativeEventCount;
            public int normalizedTimeWrapCount;
            public int landingObservationCount;
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
                CharacterFootPathStageAnalysis pathStageAnalysis = null,
                CharacterFootSwingToLandingFloorHandoffAnalysis
                    swingToLandingFloorHandoff = null,
                CharacterFootLateApproachLandingRevisionAnalysis
                    lateApproachLandingRevision = null,
                CharacterFootLandingObservationAnalysis
                    landingObservation = null,
                CharacterFootVisibleOutputJumpAnalysis
                    visibleOutputJump = null)
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
                this.swingToLandingFloorHandoff =
                    swingToLandingFloorHandoff;
                this.lateApproachLandingRevision =
                    lateApproachLandingRevision;
                this.landingObservation = landingObservation;
                this.visibleOutputJump = visibleOutputJump;
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
            public CharacterFootSwingToLandingFloorHandoffAnalysis
                swingToLandingFloorHandoff;
            public CharacterFootLateApproachLandingRevisionAnalysis
                lateApproachLandingRevision;
            public CharacterFootLandingObservationAnalysis landingObservation;
            public CharacterFootVisibleOutputJumpAnalysis visibleOutputJump;

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
