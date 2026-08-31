using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvValues;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal readonly struct CharacterFootMotionDiagnosticAnalysis
    {
        internal CharacterFootMotionDiagnosticAnalysis(
            string samplesPath,
            string geometryPath,
            string analysisPath,
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
            AnalysisPath = analysisPath ?? string.Empty;
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
        internal string AnalysisPath { get; }
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
        const string Schema = "character-foot-motion-facts/71";
        const string AnalyzerId = "character-foot-motion-fact-analyzer";
        const int AnalyzerVersion = 71;
        const float RuntimeGeometryEpsilon = 0.0001f;
        const float ExpectedCorrectionResponseIncreaseSpeed = 1.8f;
        const float ExpectedCorrectionResponseDecreaseSpeed = 1.5f;
        const string GeometryFileName = "ground-path-geometry.csv";
        const int HeaderColumnCapacity = 1280;
        const float PositionNoiseFloor = 0.001f;
        const float RotationNoiseFloorDegrees = 0.1f;
        const float DirectionComparisonEpsilonDegrees = 0.0001f;
        const float TimeEpsilon = 0.000001f;
        const double LandingReachCompressionReserveMeters = 0.02d;
        const double LowPresentationSamplingDeltaSeconds = 1d / 30d;
        const double SwingSpeedAnomalyMetersPerSecond = 5d;
        const float CorrectionHoldMaximumMeters = 0.005f;
        const float CorrectionAdvanceMinimumMeters = 0.02f;
        const float ExpectedGroundPenetrationToleranceMeters = 0.01f;
        internal const double ContactSupportGapThresholdMeters = 0.01d;
        internal const double ContactSupportGapPersistentSeconds = 0.1d;
        internal const double ContactSupportTouchToleranceMeters = PositionNoiseFloor;

        internal static CharacterFootMotionDiagnosticAnalysis Analyze(
            string samplesPath,
            string outputDirectory = null)
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
            string destination = Path.GetFullPath(outputDirectory ?? Path.Combine(
                Path.GetDirectoryName(fullSamplesPath), "diagnoses"));
            if (Directory.Exists(destination))
                throw new IOException("Foot diagnostic output already exists; use a new analysis directory.");
            var performance = new CharacterFootDiagnosticPerformance();
            var timer = Stopwatch.StartNew();
            CsvCapture capture = ReadCapture(fullSamplesPath, geometryPath);
            performance.readAndValidateMilliseconds = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
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
            document.sample.file = Path.GetRelativePath(destination, fullSamplesPath).Replace('\\', '/');
            document.sample.geometryFile = Path.GetRelativePath(destination, geometryPath).Replace('\\', '/');
            var context = new CharacterFootDiagnosisContext(JObject.FromObject(document,
                JsonSerializer.Create(CharacterFootDiagnosticStore.SerializerSettings())));
            context.SourceIndices = capture.SourceIndices;
            performance.analyzeMilliseconds = timer.Elapsed.TotalMilliseconds;
            CharacterFootDiagnosisPublication publication =
                CharacterFootDiagnosisPublisher.Publish(destination, context, performance);
            string summary =
                $"frames={capture.UniqueFrameCount} footRows={capture.FootRows.Count} " +
                $"geometryRows={capture.GeometryRowCount} " +
                $"landingEvents={document.coverage.landingEventCount} " +
                $"landingStateBoundaries={document.coverage.landingStateBoundaryCount} " +
                $"landingStateSpans={document.coverage.landingStateSpanCount} " +
                $"lockedEvents={document.coverage.lockedEventCount} " +
                $"lockedFullAnchorEvents={document.coverage.lockedFullAnchorEventCount} " +
                $"lockedSlidingEvents={document.coverage.lockedSlidingEventCount} " +
                $"releaseEvents={document.coverage.releaseEventCount} " +
                $"pathRevisionOutputJumps={document.coverage.pathRevisionOutputJumpCount} " +
                $"pathContinuityEvents={document.coverage.pathContinuityEventCount} " +
                $"stableSwingOutputJumps={document.coverage.stableSwingOutputJumpCount} " +
                $"contactStateOutputJumps={document.coverage.contactStateOutputJumpCount} " +
                $"swingToLandingHandoffs={document.coverage.swingToLandingFloorHandoffCount} " +
                $"plantInterpolationJumps={document.coverage.plantInterpolationOutputJumpCount} " +
                $"contactAcquisitions={document.coverage.contactAcquisitionContinuityCount} " +
                $"lockWeightEvents={document.coverage.lockWeightCompletionEventCount} " +
                $"approachOwnership={document.coverage.approachProgressOwnershipCount} " +
                $"actionHardOwnership={document.coverage.actionHardOwnershipCount} " +
                $"contactTransitions={document.coverage.contactTransitionContextCount} " +
                $"formalGoalWeights={document.coverage.formalGoalWeightPolicyCount} " +
                $"reentryGeometry={document.coverage.contactReentryOutputGeometryCount} " +
                $"stableSwingCorrectionCadence={document.coverage.stableSwingCorrectionResponseCadenceCount} " +
                $"actualEnvelopeCounterfactuals={document.coverage.actualFootEnvelopeCounterfactualCount} " +
                $"lateApproachLandingRevisions={document.coverage.lateApproachLandingRevisionCount} " +
                $"supportChanges={document.coverage.supportChangeCount} " +
                $"penetrationEvents={document.coverage.contactPlanePenetrationEventCount} " +
                $"stepTimeCandidateSelections={document.coverage.stepTimeCandidateSelectionCount} " +
                $"stepTimeRepresentativeEvents={document.coverage.stepTimeCandidateRepresentativeEventCount} " +
                $"landingObservations={document.coverage.landingObservationCount} " +
                $"futureLandingQueries={document.coverage.futureLandingQueryCount} " +
                $"currentContactVerificationQueries={document.coverage.currentContactVerificationQueryCount} " +
                $"currentSupportQueries={document.coverage.currentSupportQueryCount} " +
                $"predictionMotions={document.coverage.predictionMotionCount} " +
                $"predictionMotionResets={document.coverage.predictionMotionResetCount} " +
                $"diagnosisFiles={publication.DiagnosticCount} " +
                $"diagnosisTargets={publication.TargetCount} " +
                $"diagnosisMatches={publication.MatchCount}";
            return new CharacterFootMotionDiagnosticAnalysis(
                fullSamplesPath,
                geometryPath,
                Path.Combine(publication.Directory, CharacterFootDiagnosticStore.ManifestFileName),
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
            AnalyzeCurrentSupportQueries(frames, events);
            AnalyzeLandingEvents(frames, events);
            AnalyzeLandingStateConsistency(frames, events);
            AnalyzeLifecycleTransitions(frames, events);
            AnalyzeContactSupportGaps(frames, events);
            AnalyzeApproachProgressOwnership(frames, events);
            AnalyzeLockWeightCompletionEvents(frames, events);
            AnalyzeSwingToLandingFloorHandoffs(frames, events);
            AnalyzeLockedEvents(frames, events);
            AnalyzeContactPlanePenetration(frames, events);
            AnalyzeReleaseEvents(frames, events);
            AnalyzeLateApproachLandingRevisions(frames, events);
            AnalyzePlantInterpolationOutputJumps(frames, events);
            AnalyzeContactAcquisitionContinuity(frames, events);
            AnalyzeStableSwingCorrectionResponseCadence(frames, events);
            AnalyzeActualFootEnvelopeCounterfactuals(frames, events);
            AnalyzeVisibleOutputJumps(frames, events);
            AnalyzePathContinuity(frames, events);
        }

        static void AnalyzeLifecycleTransitions(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame current = frames[i];
                FootFrame previous = i > 0 &&
                                     Continuous(frames[i - 1], current) &&
                                     frames[i - 1].BodyResetSequence ==
                                     current.BodyResetSequence &&
                                     frames[i - 1].ProgramIdentity ==
                                     current.ProgramIdentity &&
                                     frames[i - 1].ProjectionRevision ==
                                     current.ProjectionRevision &&
                                     frames[i - 1].PoseGraphRevision ==
                                     current.PoseGraphRevision &&
                                     frames[i - 1].ProfileRevision ==
                                     current.ProfileRevision
                    ? frames[i - 1]
                    : null;
                bool contextMatchesPreviousFrame = previous == null ||
                    current.PreviousLockRequestAvailable &&
                    current.PreviousLockRequested ==
                        previous.CurrentLockRequested &&
                    current.PreviousLockRequestEventIdentity ==
                        previous.CurrentLockRequestEventIdentity &&
                    current.PreviousLockRequestMode ==
                        previous.CurrentLockRequestMode &&
                    Math.Abs(
                        current.PreviousLockRequestWeight -
                        previous.CurrentLockRequestWeight) <= TimeEpsilon &&
                    Math.Abs(
                        current.PreviousContactEdgeSeconds -
                        previous.CurrentContactEdgeSeconds) <= TimeEpsilon &&
                    current.PreviousLatestContactEventIdentity ==
                        previous.CurrentLatestContactEventIdentity &&
                    current.PreviousLatestReleasedContactEventIdentity ==
                        previous.CurrentLatestReleasedContactEventIdentity &&
                    current.PreviousCompletedLockWeightEventIdentity ==
                        previous.CurrentCompletedLockWeightEventIdentity &&
                    current.PreviousContactAnchorAvailable ==
                        previous.CurrentContactAnchorAvailable &&
                    current.PreviousContactAnchorEventIdentity ==
                        previous.CurrentContactAnchorEventIdentity &&
                    ContactAnchorFrame.From(current, true).SameAs(
                        ContactAnchorFrame.From(previous, false));
                if (!contextMatchesPreviousFrame)
                {
                    throw new InvalidDataException(
                        $"Foot Motion committed Lifecycle Transition context did not continue " +
                        $"Frame={current.Frame} Side={current.Side}.");
                }
                bool actionOccupied = current.ActionInstanceIdentity != 0 ||
                                      current.ActionFootWeight >
                                      RuntimeGeometryEpsilon;
                bool groundedAuthoritative = current.Grounded &&
                                             current.CurrentStep.IsAuthoritative;
                bool actionIndependentOwnership =
                    !actionOccupied || !groundedAuthoritative ||
                    !current.HardOwnershipLoss &&
                    current.PreTransitionReason != "OwnershipLost" &&
                    !current.PreTransitionSuppressOutput &&
                    !current.PreTransitionResetInterpolation;
                if (!actionIndependentOwnership)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Action occupancy incorrectly produced Hard Ownership Loss " +
                        $"Frame={current.Frame} Side={current.Side}.");
                }
                events.Add(new EventFact(
                    "FormalGoalWeightPolicy", current.Side, current.Frame,
                    current.Frame, current.Frame, ResolveEventIdentity(current),
                    current.SourceIdentity, current.SourceCycle,
                    DeltaSeconds(current),
                    new SortedDictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["FormalFootPlacementWeight"] =
                            current.FormalFootPlacementWeight,
                        ["LockWeight"] = current.CurrentLockRequestWeight,
                        ["MotionPositionWeight"] = current.MotionPositionWeight,
                        ["MotionRotationWeight"] = current.MotionRotationWeight,
                        ["ResolvedPositionWeight"] = current.Resolved.PositionWeight,
                        ["ResolvedRotationWeight"] = current.Resolved.RotationWeight,
                        ["FinalGoalPositionWeight"] = current.FinalGoalPositionWeight,
                        ["FinalGoalRotationWeight"] = current.FinalGoalRotationWeight
                    },
                    new SortedDictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["formalWeightPolicyConsistent"] = true,
                        ["ready"] = current.Resolved.Outcome == "Ready",
                        ["contactAnchorAvailable"] = current.CurrentContactAnchorAvailable
                    }));
                if (actionOccupied)
                {
                    events.Add(new EventFact(
                        "ActionHardOwnership",
                        current.Side,
                        current.Frame,
                        current.Frame,
                        current.Frame,
                        ResolveEventIdentity(current),
                        current.SourceIdentity,
                        current.SourceCycle,
                        DeltaSeconds(current),
                        new SortedDictionary<string, double>(
                            StringComparer.Ordinal)
                        {
                            ["ActionFootWeight"] = current.ActionFootWeight,
                            ["FormalFootPlacementWeight"] =
                                current.FormalFootPlacementWeight,
                            ["MotionPositionWeight"] =
                                current.MotionPositionWeight,
                            ["MotionRotationWeight"] =
                                current.MotionRotationWeight,
                            ["ResolvedPositionWeight"] =
                                current.Resolved.PositionWeight,
                            ["ResolvedRotationWeight"] =
                                current.Resolved.RotationWeight
                        },
                        new SortedDictionary<string, bool>(
                            StringComparer.Ordinal)
                        {
                            ["actionOccupied"] = true,
                            ["grounded"] = current.Grounded,
                            ["currentStepAuthoritative"] =
                                current.CurrentStep.IsAuthoritative,
                            ["hardOwnershipLoss"] =
                                current.HardOwnershipLoss,
                            ["preTransitionSuppressOutput"] =
                                current.PreTransitionSuppressOutput,
                            ["preTransitionResetInterpolation"] =
                                current.PreTransitionResetInterpolation,
                            ["postTransitionSuppressOutput"] =
                                current.PostTransitionSuppressOutput,
                            ["postTransitionEvaluated"] =
                                current.PostTransitionEvaluated,
                            ["postTransitionResetInterpolation"] =
                                current.PostTransitionResetInterpolation,
                            ["actionIndependentOwnership"] =
                                actionIndependentOwnership
                        }));
                }
                bool reentryGeometryAvailable =
                    current.SameEventContactReentryRefreshed &&
                    current.PreviousResponseOutputAvailable &&
                    current.PlantInterpolationEvaluated &&
                    current.CorrectionResponseEvaluated &&
                    current.Resolved.Outcome == "Ready";
                if (reentryGeometryAvailable)
                {
                    Vector3 capturedOutput = current.PlantSelectedWorldTarget +
                        current.PlantWorldResidualCapturedBeforeDecay;
                    events.Add(new EventFact(
                        "ContactReentryOutputGeometry", current.Side,
                        previous?.Frame ?? current.Frame, current.Frame,
                        current.Frame, current.CurrentContactAnchorEventIdentity,
                        current.SourceIdentity, current.SourceCycle,
                        DeltaSeconds(current),
                        new SortedDictionary<string, double>(StringComparer.Ordinal)
                        {
                            ["CapturedTargetToPreviousResponseDistanceMeters"] =
                                Vector3.Distance(capturedOutput,
                                    current.PreviousResponseOutputPoint),
                            ["ResidualDecayStepMeters"] = Vector3.Distance(
                                current.PlantWorldResidualCapturedBeforeDecay,
                                current.PlantWorldResidualAfterDecay),
                            ["CapturedTargetToDesiredStepMeters"] =
                                Vector3.Distance(capturedOutput,
                                    current.DesiredOutputPoint),
                            ["DesiredToResponseStepMeters"] = Vector3.Distance(
                                current.DesiredOutputPoint, current.ResponseOutputPoint),
                            ["PreviousResponseToResponseStepMeters"] =
                                Vector3.Distance(current.PreviousResponseOutputPoint,
                                    current.ResponseOutputPoint),
                            ["ResponseToFinalSoleStepMeters"] = Vector3.Distance(
                                current.ResponseOutputPoint, current.Resolved.FinalSole)
                        },
                        new SortedDictionary<string, bool>(StringComparer.Ordinal)
                        {
                            ["sameEventReentryGeometryAvailable"] = true,
                            ["residualCaptured"] =
                                current.PlantResidualCaptureReason != "None",
                            ["residualDecayApplied"] =
                                current.PlantWorldResidualDecayApplied,
                            ["reentryInterpolationHistoryRetained"] =
                                current.ReentryInterpolationHistoryRetained
                        }));
                }
                bool contactRelevant = current.ContactEdge != "None" ||
                    current.PreviousContactAnchorAvailable ||
                    current.CurrentContactAnchorAvailable ||
                    current.PreviousLatestContactEventIdentity != 0 ||
                    current.CurrentLatestContactEventIdentity != 0 ||
                    current.PreviousLatestReleasedContactEventIdentity != 0 ||
                    current.CurrentLatestReleasedContactEventIdentity != 0 ||
                    current.SameEventContactReentryRefreshed ||
                    current.SameEventContactReentryUnavailable;
                if (!contactRelevant)
                    continue;
                events.Add(new EventFact(
                    "ContactTransitionContext",
                    current.Side,
                    previous?.Frame ?? current.Frame,
                    current.Frame,
                    current.Frame,
                    current.CurrentLockRequestEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    new SortedDictionary<string, double>(
                        StringComparer.Ordinal)
                    {
                        ["PreviousLockRequestWeight"] =
                            current.PreviousLockRequestWeight,
                        ["CurrentLockRequestWeight"] =
                            current.CurrentLockRequestWeight,
                        ["PreviousContactEdgeSeconds"] =
                            current.PreviousContactEdgeSeconds,
                        ["CurrentContactEdgeSeconds"] =
                            current.CurrentContactEdgeSeconds
                    },
                    new SortedDictionary<string, bool>(
                        StringComparer.Ordinal)
                    {
                        ["transitionContractConsistent"] = true,
                        ["postTransitionEvaluated"] =
                            current.PostTransitionEvaluated,
                        ["reentryOutputFactsAvailable"] =
                            reentryGeometryAvailable,
                        ["contextMatchesPreviousFrame"] =
                            contextMatchesPreviousFrame,
                        ["previousLockRequested"] =
                            current.PreviousLockRequested,
                        ["currentLockRequested"] =
                            current.CurrentLockRequested,
                        ["contactEdgeRising"] =
                            current.ContactEdge == "Rising",
                        ["contactEdgeFalling"] =
                            current.ContactEdge == "Falling",
                        ["contactEdgeEventChanged"] =
                            current.ContactEdge == "EventChanged",
                        ["sameEventContactReentryRefreshed"] =
                            current.SameEventContactReentryRefreshed,
                        ["sameEventContactReentryUnavailable"] =
                            current.SameEventContactReentryUnavailable,
                        ["retainedVerifiedAnchor"] =
                            current.RetainedVerifiedAnchor,
                        ["reentryInterpolationHistoryRetained"] =
                            current.ReentryInterpolationHistoryRetained,
                        ["previousAnchorAvailable"] =
                            current.PreviousContactAnchorAvailable,
                        ["currentAnchorAvailable"] =
                            current.CurrentContactAnchorAvailable
                    }));
            }
        }

        static CharacterFootContactSupportGapFrame ResolveContactSupportGap(
            FootFrame frame)
        {
            var fact = new CharacterFootContactSupportGapFrame
            {
                frame = frame.Frame,
                side = frame.Side,
                requested = frame.CurrentLockRequested,
                observed = frame.CurrentLockRequested ||
                    frame.ConstraintState == "Releasing" && frame.CurrentContactAnchorAvailable,
                applicable = frame.CurrentLockRequested &&
                    (frame.ConstraintState == "Landing" || frame.ConstraintState == "Locked") &&
                    frame.Grounded && frame.CurrentStep.IsAuthoritative &&
                    frame.FormalFootPlacementWeight > 0d,
                constraintState = frame.ConstraintState,
                domain = ContactSupportDomain(frame),
                lockResponse = frame.LockResponse,
                targetKind = frame.PlantTargetKind,
                contactEdge = frame.ContactEdge,
                positionWeight = frame.FinalGoalPositionWeight,
                fullPositionWeight = frame.FinalGoalPositionWeight >= 1f - TimeEpsilon,
                requestEventIdentity = frame.CurrentLockRequestEventIdentity.ToString(
                    CultureInfo.InvariantCulture),
                anchorEventIdentity = frame.CurrentContactAnchorEventIdentity.ToString(
                    CultureInfo.InvariantCulture),
                anchorSurfaceIdentity = frame.CurrentContactAnchorSurfaceIdentity,
                anchorWorldRevision = frame.CurrentContactAnchorWorldRevision.ToString(
                    CultureInfo.InvariantCulture),
                anchorAcquiredFrame =
                    frame.CurrentContactAnchorAcquiredFrameSequence.ToString(
                        CultureInfo.InvariantCulture),
                anchorAcquiredCompletion =
                    frame.CurrentContactAnchorAcquiredCompletionIdentity.ToString(
                        CultureInfo.InvariantCulture),
                anchorPoint = CharacterFootVectorFact.From(
                    frame.CurrentContactAnchorPoint),
                anchorNormal = CharacterFootVectorFact.From(
                    frame.CurrentContactAnchorNormal),
                formalFootPlacementWeight = frame.FormalFootPlacementWeight,
                lockWeight = frame.CurrentLockRequestWeight,
                deltaSeconds = frame.DeltaSeconds,
                currentSupportAvailable = frame.CurrentSupport.Available,
                currentSupportRejectReason = frame.CurrentSupport.RejectReason,
                currentSupportSurfaceIdentity = frame.CurrentSupport.Target.Surface,
                landingReachAvailable = frame.LandingReachAvailable,
                gapMotion = "Unavailable"
            };
            CharacterFootContactSupportGapAvailability availability =
                !fact.observed
                    ? CharacterFootContactSupportGapAvailability.NotRequested
                    : !frame.Grounded || !frame.CurrentStep.IsAuthoritative
                        ? CharacterFootContactSupportGapAvailability.OwnershipUnavailable
                        : frame.FormalFootPlacementWeight <= 0d
                            ? CharacterFootContactSupportGapAvailability.PlacementWeightZero
                            : !frame.FinalPhysicalWriteAvailable ||
                              frame.FinalPhysicalWriteCompletionIdentity !=
                              frame.CompletionIdentity
                                ? CharacterFootContactSupportGapAvailability.PhysicalPoseUnavailable
                                : !frame.CurrentContactAnchorAvailable ||
                                  frame.ConstraintState != "Releasing" &&
                                  frame.CurrentContactAnchorEventIdentity != frame.CurrentLockRequestEventIdentity
                                    ? CharacterFootContactSupportGapAvailability.SameEventAnchorUnavailable
                                    : fact.domain == "Unclassified"
                                        ? CharacterFootContactSupportGapAvailability.ContactHoldingStateUnavailable
                                        : CharacterFootContactSupportGapAvailability.Available;
            fact.availability = availability.ToString();
            if (availability != CharacterFootContactSupportGapAvailability.Available)
                return fact;
            fact.qualityEligible = fact.applicable && fact.fullPositionWeight;
            if (!FiniteVector(frame.FinalHeel) || !FiniteVector(frame.FinalToe))
                throw new InvalidDataException(
                    $"Foot Motion Contact support gap physical pose is invalid " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            Vector3 normal = frame.CurrentContactAnchorNormal.normalized;
            Vector3 point = frame.CurrentContactAnchorPoint;
            double heel = Vector3.Dot(frame.FinalHeel - point, normal);
            double toe = Vector3.Dot(frame.FinalToe - point, normal);
            Vector3 sole = (frame.FinalHeel + frame.FinalToe) * 0.5f;
            fact.physicalHeel = CharacterFootVectorFact.From(frame.FinalHeel);
            fact.physicalToe = CharacterFootVectorFact.From(frame.FinalToe);
            fact.heelClearanceMeters = heel;
            fact.toeClearanceMeters = toe;
            fact.soleClearanceMeters = Vector3.Dot(sole - point, normal);
            fact.wholeFootGapMeters = Math.Max(0d, Math.Min(heel, toe));
            fact.inPlaneAnchorDistanceMeters =
                Vector3.ProjectOnPlane(sole - point, normal).magnitude;
            fact.gapMotion = fact.wholeFootGapMeters <= ContactSupportTouchToleranceMeters ? "Touching" :
                fact.wholeFootGapMeters <= ContactSupportGapThresholdMeters ? "WithinGapThreshold" : "FirstObservation";
            return fact;
        }

        static bool ContactSupportGapAvailable(FootFrame frame) =>
            frame.ContactSupportGap.availability ==
            CharacterFootContactSupportGapAvailability.Available.ToString();

        static string ContactSupportDomain(FootFrame frame) => frame.ConstraintState switch
        {
            "Landing" => "Landing",
            "Locked" when frame.LockResponse == "FullAnchor" => "FullAnchor",
            "Locked" when frame.LockResponse == "Sliding" => "Sliding",
            "Releasing" => "Release",
            _ => "Unclassified"
        };

        static bool SameContactSupportGapReference(
            FootFrame previous, FootFrame current) =>
            Continuous(previous, current) &&
            ContactSupportGapAvailable(previous) &&
            ContactSupportGapAvailable(current) &&
            previous.CurrentContactAnchorEventIdentity == current.CurrentContactAnchorEventIdentity &&
            ContactAnchorFrame.From(previous, false).SameAs(
                ContactAnchorFrame.From(current, false));

        static bool SameContactSupportPolicy(FootFrame previous, FootFrame current) =>
            SameContactSupportGapReference(previous, current) &&
            previous.ContactSupportGap.domain == current.ContactSupportGap.domain &&
            previous.ContactSupportGap.qualityEligible == current.ContactSupportGap.qualityEligible &&
            previous.ContactSupportGap.fullPositionWeight == current.ContactSupportGap.fullPositionWeight;

        static void AnalyzeContactSupportGaps(List<FootFrame> frames, List<EventFact> events)
        {
            bool touched = false;
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame frame = frames[i];
                CharacterFootContactSupportGapFrame fact = frame.ContactSupportGap;
                bool sameReference = i > 0 && SameContactSupportGapReference(frames[i - 1], frame);
                if (!sameReference || !fact.qualityEligible)
                    touched = false;
                if (!fact.observed)
                    continue;
                bool available = ContactSupportGapAvailable(frame);
                if (available && sameReference)
                {
                    double delta = fact.wholeFootGapMeters.Value -
                        frames[i - 1].ContactSupportGap.wholeFootGapMeters.Value;
                    fact.previousGapDeltaMeters = delta;
                    fact.gapVelocityMetersPerSecond = frame.DeltaSeconds > 0f
                        ? (double?)(delta / frame.DeltaSeconds) : null;
                    fact.gapMotion = fact.wholeFootGapMeters <= ContactSupportTouchToleranceMeters ? "Touching" :
                        fact.wholeFootGapMeters <= ContactSupportGapThresholdMeters ? "WithinGapThreshold" :
                        delta < -PositionNoiseFloor ? "Closing" :
                        delta > PositionNoiseFloor ? "Widening" : "StableGap";
                }
                if (available && fact.qualityEligible)
                {
                    if (fact.wholeFootGapMeters <= ContactSupportTouchToleranceMeters)
                        touched = true;
                    else if (fact.wholeFootGapMeters > ContactSupportGapThresholdMeters)
                    {
                        fact.reopenedAfterTouch = touched;
                        touched = false;
                    }
                }
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["GapThresholdMeters"] = ContactSupportGapThresholdMeters,
                    ["TouchToleranceMeters"] = ContactSupportTouchToleranceMeters,
                    ["PersistentMinimumSeconds"] = ContactSupportGapPersistentSeconds
                };
                if (available)
                {
                    metrics["WholeFootGapMeters"] = fact.wholeFootGapMeters.Value;
                    metrics["HeelClearanceMeters"] = fact.heelClearanceMeters.Value;
                    metrics["ToeClearanceMeters"] = fact.toeClearanceMeters.Value;
                    metrics["SoleClearanceMeters"] = fact.soleClearanceMeters.Value;
                    metrics["InPlaneAnchorDistanceMeters"] = fact.inPlaneAnchorDistanceMeters.Value;
                    if (fact.previousGapDeltaMeters.HasValue)
                        metrics["GapDeltaMeters"] = fact.previousGapDeltaMeters.Value;
                    if (fact.gapVelocityMetersPerSecond.HasValue)
                        metrics["GapVelocityMetersPerSecond"] = fact.gapVelocityMetersPerSecond.Value;
                }
                events.Add(new EventFact(
                    "ContactSupportGapObservation", frame.Side, frame.Frame, frame.Frame, frame.Frame,
                    frame.CurrentContactAnchorEventIdentity, frame.SourceIdentity, frame.SourceCycle,
                    DeltaSeconds(frame), metrics,
                    new SortedDictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["contactRequested"] = fact.requested,
                        ["referenceAvailable"] = available,
                        ["measurementApplicable"] = fact.applicable,
                        ["qualityEligible"] = fact.qualityEligible,
                        ["fullPositionWeight"] = fact.fullPositionWeight,
                        ["reopenedAfterTouch"] = fact.reopenedAfterTouch
                    },
                    contactSupportGap: new CharacterFootContactSupportGapSequence
                    {
                        domain = fact.domain,
                        qualityEligible = fact.qualityEligible,
                        classification = available ? fact.gapMotion : fact.availability,
                        frames = new List<CharacterFootContactSupportGapFrame> { fact }
                    }));
            }

            var segments = new List<EventFact>();
            int index = 0;
            while (index < frames.Count)
            {
                if (!ContactSupportGapAvailable(frames[index]))
                {
                    index++;
                    continue;
                }
                int start = index;
                while (index + 1 < frames.Count && SameContactSupportPolicy(frames[index], frames[index + 1]))
                    index++;
                EventFact segment = BuildContactGapSequence(frames, start, index, "ContactSupportGapSegment");
                segments.Add(segment);
                events.Add(segment);
                index++;
            }

            index = 0;
            int segmentCursor = 0;
            while (index < frames.Count)
            {
                if (!frames[index].ContactSupportGap.qualityEligible)
                {
                    index++;
                    continue;
                }
                int start = index;
                while (index + 1 < frames.Count && frames[index + 1].ContactSupportGap.qualityEligible &&
                    SameContactSupportGapReference(frames[index], frames[index + 1]))
                    index++;
                EventFact episode = BuildContactGapSequence(frames, start, index, "ContactSupportGapInterval");
                while (segmentCursor < segments.Count && segments[segmentCursor].endFrame < frames[start].Frame)
                    segmentCursor++;
                var members = new List<EventFact>();
                while (segmentCursor < segments.Count && segments[segmentCursor].endFrame <= frames[index].Frame)
                    members.Add(segments[segmentCursor++]);
                episode.metrics["ScoredGapMaximumMeters"] = members.Max(value => value.metrics["ScoredGapMaximumMeters"]);
                episode.metrics["PolicySegmentCount"] = members.Count;
                foreach (string flag in new[] { "fullAnchorGap", "slidingGap", "landingPersistentGap" })
                    episode.evidence[flag] = members.Any(value => value.evidence[flag]);
                episode.contactSupportGap.domain = "ContactEpisode";
                episode.contactSupportGap.classification = episode.metrics["ScoredGapMaximumMeters"] >
                    ContactSupportGapThresholdMeters ? "ContactGapByPolicy" : "NoScoredContactGap";
                episode.contactSupportGap.segmentStartFrames = members.Select(value => value.startFrame).ToList();
                events.Add(episode);
                index++;
            }
        }

        static EventFact BuildContactGapSequence(List<FootFrame> frames, int start, int end, string kind)
        {
            FootFrame first = frames[start], last = frames[end];
            string domain = first.ContactSupportGap.domain;
            int peak = start, gapFrames = 0, reopenedCount = 0;
            double duration = 0d, longestGap = 0d, runDuration = 0d, runMaximum = 0d;
            double persistentMaximum = 0d, closingSeconds = 0d, wideningSeconds = 0d, excessIntegral = 0d;
            double? firstGapTime = null, closureTime = null;
            bool previousAbove = false;
            void FinishGapRun()
            {
                longestGap = Math.Max(longestGap, runDuration);
                if (runDuration + TimeEpsilon >= ContactSupportGapPersistentSeconds)
                    persistentMaximum = Math.Max(persistentMaximum, runMaximum);
                runDuration = 0d;
                runMaximum = 0d;
            }
            for (int i = start; i <= end; i++)
            {
                CharacterFootContactSupportGapFrame fact = frames[i].ContactSupportGap;
                double gap = fact.wholeFootGapMeters.Value;
                if (gap > frames[peak].ContactSupportGap.wholeFootGapMeters.Value)
                    peak = i;
                if (i > start)
                {
                    double dt = frames[i].DeltaSeconds;
                    duration += dt;
                    double previousGap = frames[i - 1].ContactSupportGap.wholeFootGapMeters.Value;
                    excessIntegral += (Math.Max(0d, previousGap - ContactSupportGapThresholdMeters) +
                        Math.Max(0d, gap - ContactSupportGapThresholdMeters)) * 0.5d * dt;
                    if (gap < previousGap - PositionNoiseFloor) closingSeconds += dt;
                    if (gap > previousGap + PositionNoiseFloor) wideningSeconds += dt;
                }
                bool above = gap > ContactSupportGapThresholdMeters;
                if (above)
                {
                    firstGapTime ??= duration;
                    gapFrames++;
                    if (previousAbove) runDuration += frames[i].DeltaSeconds;
                    runMaximum = Math.Max(runMaximum, gap);
                }
                else if (previousAbove)
                    FinishGapRun();
                if (firstGapTime.HasValue && !closureTime.HasValue &&
                    gap <= ContactSupportTouchToleranceMeters)
                    closureTime = duration - firstGapTime.Value;
                previousAbove = above;
                if (fact.reopenedAfterTouch) reopenedCount++;
            }
            if (previousAbove) FinishGapRun();
            double entryGap = first.ContactSupportGap.wholeFootGapMeters.Value;
            double exitGap = last.ContactSupportGap.wholeFootGapMeters.Value;
            double maximum = frames[peak].ContactSupportGap.wholeFootGapMeters.Value;
            bool fullyWeighted = first.ContactSupportGap.qualityEligible;
            bool fullAnchorGap = fullyWeighted && domain == "FullAnchor" && maximum > ContactSupportGapThresholdMeters;
            bool slidingGap = fullyWeighted && domain == "Sliding" && maximum > ContactSupportGapThresholdMeters;
            bool landingPersistent = fullyWeighted && domain == "Landing" && persistentMaximum > ContactSupportGapThresholdMeters;
            double scoredMaximum = fullAnchorGap || slidingGap ? maximum : landingPersistent ? persistentMaximum : 0d;
            FootFrame next = end + 1 < frames.Count ? frames[end + 1] : null;
            bool adjacentNext = next != null && Continuous(last, next);
            string endReason = next == null ? "SampleEnded" : !adjacentNext ? "FrameGap" :
                next.ConstraintState == "Releasing" || !next.CurrentLockRequested ? "FormalContactExit" :
                !SameContactSupportGapReference(last, next) ? "ContactReferenceChangedOrUnavailable" :
                "ContactPolicyOrWeightChanged";
            bool endingTouch = exitGap <= ContactSupportTouchToleranceMeters;
            string trend = closingSeconds > 0d && wideningSeconds > 0d ? "Mixed" :
                closingSeconds > 0d ? "Closing" : wideningSeconds > 0d ? "Widening" : "NoSignificantChange";
            string closure = gapFrames == 0 ? endingTouch ? "Touching" : "WithinGapThresholdNotTouching" :
                endingTouch ? "ClosedWithinObservedSpan" :
                endReason == "FormalContactExit" ? "ExitedWithoutObservedClosure" :
                trend == "Mixed" ? "OpenMixedTrend" : trend == "Closing" ? "StillClosing" : "NotClosing";
            var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
            {
                ["FrameCount"] = end - start + 1,
                ["GapFrameCount"] = gapFrames,
                ["ObservedDurationSeconds"] = duration,
                ["LongestGapDurationSeconds"] = longestGap,
                ["MaximumWholeFootGapMeters"] = maximum,
                ["PersistentGapMaximumMeters"] = persistentMaximum,
                ["ScoredGapMaximumMeters"] = scoredMaximum,
                ["EntryWholeFootGapMeters"] = entryGap,
                ["ExitWholeFootGapMeters"] = exitGap,
                ["NetGapChangeMeters"] = exitGap - entryGap,
                ["ClosingDurationSeconds"] = closingSeconds,
                ["WideningDurationSeconds"] = wideningSeconds,
                ["ExcessGapIntegralMeterSeconds"] = excessIntegral,
                ["ReopenedAfterTouchCount"] = reopenedCount,
                ["GapThresholdMeters"] = ContactSupportGapThresholdMeters,
                ["TouchToleranceMeters"] = ContactSupportTouchToleranceMeters,
                ["PersistentMinimumSeconds"] = ContactSupportGapPersistentSeconds,
                ["EntryPositionWeight"] = first.ContactSupportGap.positionWeight,
                ["ExitPositionWeight"] = last.ContactSupportGap.positionWeight,
                ["EntryLockWeight"] = first.ContactSupportGap.lockWeight,
                ["ExitLockWeight"] = last.ContactSupportGap.lockWeight
            };
            if (closureTime.HasValue) metrics["FirstObservedClosureSeconds"] = closureTime.Value;
            if (duration > 0d)
            {
                metrics["NetGapVelocityMetersPerSecond"] = (exitGap - entryGap) / duration;
                metrics["ClosingTimeFraction"] = closingSeconds / duration;
            }
            return new EventFact(kind, first.Side, first.Frame, last.Frame, frames[peak].Frame,
                first.CurrentContactAnchorEventIdentity, first.SourceIdentity, first.SourceCycle, duration,
                metrics, new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["qualityEligible"] = fullyWeighted,
                    ["fullAnchorGap"] = fullAnchorGap,
                    ["slidingGap"] = slidingGap,
                    ["landingPersistentGap"] = landingPersistent,
                    ["reopenedAfterTouch"] = reopenedCount > 0,
                    ["endingWithinTouchTolerance"] = endingTouch,
                    ["observedClosure"] = closureTime.HasValue && endingTouch,
                    ["observedShortLargeGap"] = fullyWeighted && maximum > 0.1d &&
                        longestGap + TimeEpsilon < ContactSupportGapPersistentSeconds,
                    ["formalExitAfterSegment"] = endReason == "FormalContactExit",
                    ["rightCensored"] = endReason == "SampleEnded" || endReason == "FrameGap" ||
                        endReason == "ContactReferenceChangedOrUnavailable",
                    ["mixedGapTrend"] = closingSeconds > 0d && wideningSeconds > 0d
                },
                contactSupportGap: new CharacterFootContactSupportGapSequence
                {
                    domain = domain,
                    qualityEligible = fullyWeighted,
                    classification = domain + ":" + closure,
                    closureOutcome = closure,
                    trend = trend,
                    endReason = endReason,
                    nextDomain = adjacentNext ? next.ContactSupportGap.domain : null,
                    nextContactRequested = adjacentNext ? (bool?)next.CurrentLockRequested : null,
                    nextFrame = adjacentNext ? (int?)next.Frame : null
                });
        }

        static void AnalyzeApproachProgressOwnership(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame current = frames[i];
                if (!current.InputEvents.InApproach ||
                    current.InputEvents.Next.Identity == 0)
                {
                    continue;
                }
                ulong eventIdentity = current.InputEvents.Next.Identity;
                FootFrame previous = i > 0 &&
                                     Continuous(frames[i - 1], current)
                    ? frames[i - 1]
                    : null;
                bool sameLineage = previous != null &&
                    previous.InputEvents.InApproach &&
                    previous.InputEvents.Next.Identity == eventIdentity &&
                    previous.SourceIdentity == current.SourceIdentity &&
                    previous.SourceCycle == current.SourceCycle;
                float progressDelta = sameLineage
                    ? current.InputEvents.ApproachProgress -
                      previous.InputEvents.ApproachProgress
                    : 0f;
                bool progressMonotonic = !sameLineage ||
                    progressDelta >= -TimeEpsilon;
                bool sameEventPlantInterpolation =
                    current.PlantInterpolationEvaluated &&
                    current.PlantTargetEventIdentity == eventIdentity;
                bool sameEventResidualCapture =
                    sameEventPlantInterpolation &&
                    current.PlantResidualCaptureReason != "None";
                bool ordinarySwingDomain =
                    (current.ConstraintState == "Swing" ||
                     current.ConstraintState == "UnlockedSupport") &&
                    current.SelectedSupportTarget.Kind == "SwingGround";
                bool approachEventVisiblePositionOwned =
                    ordinarySwingDomain &&
                    (!current.SelectedSupportTarget.Available ||
                     current.SelectedSupportTarget.PositionSource !=
                     "SwingMotion" ||
                     current.SelectedSupportTarget.NormalSource !=
                     "CurrentSupport");
                bool ownershipConsistent = progressMonotonic &&
                    !sameEventPlantInterpolation &&
                    !sameEventResidualCapture &&
                    !approachEventVisiblePositionOwned;
                bool goalWeightChanged = sameLineage &&
                    (Math.Abs(
                         current.MotionPositionWeight -
                         previous.MotionPositionWeight) > TimeEpsilon ||
                     Math.Abs(
                         current.MotionRotationWeight -
                         previous.MotionRotationWeight) > TimeEpsilon);
                if (!ownershipConsistent)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Approach progress ownership is inconsistent " +
                        $"Frame={current.Frame} Side={current.Side} " +
                        $"Event={eventIdentity} ProgressDelta={progressDelta:R} " +
                        $"PlantInterpolation={sameEventPlantInterpolation} " +
                        $"ResidualCapture={sameEventResidualCapture} " +
                        $"PositionSource={current.SelectedSupportTarget.PositionSource}.");
                }
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["ApproachProgress"] =
                        current.InputEvents.ApproachProgress,
                    ["FormalFootPlacementWeight"] =
                        current.FormalFootPlacementWeight,
                    ["FormalFootPlacementWeightDelta"] = sameLineage
                        ? current.FormalFootPlacementWeight -
                          previous.FormalFootPlacementWeight : 0d,
                    ["ApproachProgressDelta"] = progressDelta,
                    ["PreparedTargetPointStep"] = sameLineage &&
                        previous.PreparedTargetAvailable &&
                        current.PreparedTargetAvailable
                            ? Vector3.Distance(
                                previous.PreparedTargetPoint,
                                current.PreparedTargetPoint)
                            : 0d,
                    ["SelectedTargetPositionStep"] = sameLineage &&
                        previous.SelectedSupportTarget.Available &&
                        current.SelectedSupportTarget.Available
                            ? Vector3.Distance(
                                previous.SelectedSupportTarget.Position,
                                current.SelectedSupportTarget.Position)
                            : 0d,
                    ["FinalEffectiveCorrectionStep"] = sameLineage
                        ? Vector3.Distance(
                            previous.FinalEffectiveCorrection,
                            current.FinalEffectiveCorrection)
                        : 0d,
                    ["PositionWeightDelta"] = sameLineage
                        ? current.MotionPositionWeight -
                          previous.MotionPositionWeight
                        : 0d,
                    ["RotationWeightDelta"] = sameLineage
                        ? current.MotionRotationWeight -
                          previous.MotionRotationWeight
                        : 0d
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["sameLineage"] = sameLineage,
                    ["progressMonotonic"] = progressMonotonic,
                    ["progressAdvanced"] = sameLineage &&
                        progressDelta > TimeEpsilon,
                    ["preparedTargetAvailable"] =
                        current.PreparedTargetAvailable,
                    ["ordinarySwingDomain"] = ordinarySwingDomain,
                    ["sameEventPlantInterpolation"] =
                        sameEventPlantInterpolation,
                    ["sameEventResidualCapture"] =
                        sameEventResidualCapture,
                    ["approachEventVisiblePositionOwned"] =
                        approachEventVisiblePositionOwned,
                    ["goalWeightChanged"] = goalWeightChanged,
                    ["goalWeightAttributionAvailable"] = true,
                    ["formalWeightPolicyConsistent"] = true,
                    ["selectedPositionFromSwingMotion"] =
                        current.SelectedSupportTarget.PositionSource ==
                        "SwingMotion",
                    ["selectedDirectionFromCurrentSupport"] =
                        current.SelectedSupportTarget.NormalSource ==
                        "CurrentSupport"
                };
                events.Add(new EventFact(
                    "ApproachProgressOwnership",
                    current.Side,
                    previous?.Frame ?? current.Frame,
                    current.Frame,
                    current.Frame,
                    eventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence));
            }
        }

        static void AnalyzeLockWeightCompletionEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            ulong expectedCompletedEvent = 0;
            bool releaseAppliedOnPreviousFrame = false;
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame frame = frames[i];
                if (releaseAppliedOnPreviousFrame ||
                    frame.PreTransitionAnchorCommand == "Release")
                {
                    expectedCompletedEvent = 0;
                }
                ulong requestEvent = frame.InputEvents.Current.Identity;
                if (requestEvent != 0 &&
                    expectedCompletedEvent != 0 &&
                    requestEvent != expectedCompletedEvent)
                {
                    expectedCompletedEvent = 0;
                }
                bool requestsLock = RequestsFormalLock(frame);
                if (requestsLock &&
                    frame.FormalLockWeight >=
                    1f - RuntimeGeometryEpsilon)
                {
                    expectedCompletedEvent = requestEvent;
                }
                bool expectedPublishedLatch =
                    frame.PlantInterpolationEvaluated &&
                    frame.PlantTargetEventIdentity != 0 &&
                    frame.PlantTargetEventIdentity == expectedCompletedEvent;
                if (frame.PlantLockWeightCompleted !=
                    expectedPublishedLatch)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant lock weight completion latch is inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side} " +
                        $"RequestEvent={requestEvent} PlantEvent={frame.PlantTargetEventIdentity} " +
                        $"Weight={frame.FormalLockWeight:R} Expected={expectedPublishedLatch} " +
                        $"Actual={frame.PlantLockWeightCompleted}.");
                }
                if (frame.PostTransitionReason == "LandingCompleted")
                {
                    bool completionConsistent =
                        frame.PlantLockWeightCompleted &&
                        frame.PlantOutputDistance <=
                        frame.PlantWorldResidualCompletionTolerance +
                        PositionNoiseFloor &&
                        frame.PlantPenetrationDepth <=
                        ExpectedGroundPenetrationToleranceMeters +
                        PositionNoiseFloor &&
                        frame.LandingReachAvailable;
                    if (!completionConsistent)
                    {
                        throw new InvalidDataException(
                            $"Foot Motion Landing completion eligibility is inconsistent " +
                            $"Frame={frame.Frame} Side={frame.Side} " +
                            $"Latch={frame.PlantLockWeightCompleted} " +
                            $"OutputDistance={frame.PlantOutputDistance:R} " +
                            $"Penetration={frame.PlantPenetrationDepth:R} " +
                            $"Tolerance={frame.PlantWorldResidualCompletionTolerance:R} " +
                            $"LandingReach={frame.LandingReachAvailable}.");
                    }
                }
                releaseAppliedOnPreviousFrame =
                    frame.PostTransitionAnchorCommand == "Release";
            }

            var eventIdentities = new HashSet<ulong>();
            for (int i = 0; i < frames.Count; i++)
            {
                if (RequestsFormalLock(frames[i]))
                {
                    eventIdentities.Add(
                        frames[i].InputEvents.Current.Identity);
                }
            }
            foreach (ulong eventIdentity in eventIdentities.OrderBy(value => value))
            {
                List<FootFrame> window = frames.Where(frame =>
                        frame.InputEvents.Current.Identity == eventIdentity ||
                        frame.PlantTargetEventIdentity == eventIdentity ||
                        frame.FootMotionEventIdentity == eventIdentity)
                    .OrderBy(frame => frame.Frame)
                    .ToList();
                List<FootFrame> requestFrames = window
                    .Where(frame =>
                        RequestsFormalLock(frame) &&
                        frame.InputEvents.Current.Identity == eventIdentity)
                    .ToList();
                if (requestFrames.Count == 0)
                    continue;
                FootFrame firstFullWeight = requestFrames.FirstOrDefault(
                    frame => frame.FormalLockWeight >=
                             1f - RuntimeGeometryEpsilon);
                bool reachedFullWeight = firstFullWeight != null;
                FootFrame completion = window.FirstOrDefault(frame =>
                    frame.PostTransitionReason == "LandingCompleted" &&
                    frame.PlantTargetEventIdentity == eventIdentity);
                bool enteredLocked = window.Any(frame =>
                    frame.ConstraintState == "Locked" &&
                    (frame.FootMotionEventIdentity == eventIdentity ||
                     frame.PlantTargetEventIdentity == eventIdentity));
                bool completionLatch =
                    completion?.PlantLockWeightCompleted == true;
                bool completionReach =
                    completion?.LandingReachAvailable == true;
                bool completionOutputClosed = completion != null &&
                    completion.PlantOutputDistance <=
                    completion.PlantWorldResidualCompletionTolerance +
                    PositionNoiseFloor;
                bool completionPenetrationClosed = completion != null &&
                    completion.PlantPenetrationDepth <=
                    ExpectedGroundPenetrationToleranceMeters +
                    PositionNoiseFloor;
                bool geometryClosedAndLocked = completion != null &&
                    enteredLocked &&
                    completionLatch &&
                    completionReach &&
                    completionOutputClosed &&
                    completionPenetrationClosed;
                bool latchPersistedAfterWeightDrop = reachedFullWeight &&
                    window.Any(frame =>
                        frame.Frame > firstFullWeight.Frame &&
                        frame.PlantTargetEventIdentity == eventIdentity &&
                        frame.PlantInterpolationEvaluated &&
                        frame.FormalLockWeight <
                        1f - RuntimeGeometryEpsilon &&
                        frame.PlantLockWeightCompleted);
                string outcome = reachedFullWeight
                    ? geometryClosedAndLocked
                        ? "FullWeightClosedAndLocked"
                        : "FullWeightNotClosedInWindow"
                    : enteredLocked
                        ? "LockedWithoutFullWeight"
                        : "NoFullWeightNoLock";
                FootFrame peak = completion ?? firstFullWeight ?? window[^1];
                float tolerance = completion != null
                    ? completion.PlantWorldResidualCompletionTolerance
                    : window.Where(frame => frame.PlantInterpolationEvaluated)
                        .Select(frame => frame.PlantWorldResidualCompletionTolerance)
                        .DefaultIfEmpty(0f)
                        .Last();
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["WindowFrameCount"] = window.Count,
                    ["RequestFrameCount"] = requestFrames.Count,
                    ["LockWeightMaximum"] = requestFrames.Max(
                        frame => frame.FormalLockWeight),
                    ["LockWeightCompletionThreshold"] =
                        1f - RuntimeGeometryEpsilon,
                    ["FirstFullWeightFrame"] =
                        firstFullWeight?.Frame ?? -1,
                    ["LandingCompletedFrame"] = completion?.Frame ?? -1,
                    ["PlantOutputDistanceAtCompletion"] =
                        completion?.PlantOutputDistance ?? 0f,
                    ["PlantPenetrationDepthAtCompletion"] =
                        completion?.PlantPenetrationDepth ?? 0f,
                    ["LandingLockCompletionTolerance"] = tolerance,
                    ["GroundPenetrationTolerance"] =
                        ExpectedGroundPenetrationToleranceMeters
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["reachedFullWeight"] = reachedFullWeight,
                    ["latchObserved"] = window.Any(frame =>
                        frame.PlantTargetEventIdentity == eventIdentity &&
                        frame.PlantLockWeightCompleted),
                    ["latchPersistedAfterWeightDrop"] =
                        latchPersistedAfterWeightDrop,
                    ["enteredLocked"] = enteredLocked,
                    ["landingCompleted"] = completion != null,
                    ["geometryClosedAndLocked"] =
                        geometryClosedAndLocked,
                    ["fullWeightNotClosedInWindow"] =
                        reachedFullWeight && !geometryClosedAndLocked,
                    ["lockedWithoutFullWeight"] =
                        !reachedFullWeight && enteredLocked,
                    ["completionLatch"] = completionLatch,
                    ["completionLandingReachAvailable"] = completionReach,
                    ["completionOutputClosed"] = completionOutputClosed,
                    ["completionPenetrationClosed"] =
                        completionPenetrationClosed
                };
                var detail = new CharacterFootLockWeightCompletionAnalysis
                {
                    outcome = outcome,
                    eventIdentity = eventIdentity.ToString(
                        CultureInfo.InvariantCulture),
                    firstFrame = window[0].Frame,
                    lastFrame = window[^1].Frame,
                    firstFullWeightFrame = firstFullWeight?.Frame,
                    landingCompletedFrame = completion?.Frame,
                    sourceIdentity = peak.SourceIdentity,
                    sourceCycle = peak.SourceCycle,
                    completionState = completion?.ConstraintState ??
                                      window[^1].ConstraintState,
                    completionPlantTargetKind =
                        completion?.PlantTargetKind ?? "None"
                };
                events.Add(new EventFact(
                    "LockWeightCompletionEvent",
                    peak.Side,
                    window[0].Frame,
                    window[^1].Frame,
                    peak.Frame,
                    eventIdentity,
                    peak.SourceIdentity,
                    peak.SourceCycle,
                    Duration(window),
                    metrics,
                    evidence,
                    lockWeightCompletion: detail));
            }
        }

        static bool RequestsFormalLock(FootFrame frame) =>
            frame.InputEvents.Current.Identity != 0 &&
            frame.FormalRequestContact > 0f &&
            frame.FormalLockMode != "Unlocked";

        static void AnalyzePlantInterpolationOutputJumps(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    !current.PlantInterpolationEvaluated ||
                    !current.FinalPhysicalWriteAvailable ||
                    !previous.FinalPhysicalWriteAvailable)
                {
                    continue;
                }
                CharacterFootVisibleOutputKinematics kinematics =
                    ResolveVisibleOutputKinematics(frames, i);
                double visibleStep = Math.Max(
                    kinematics.Ankle.StepMeters,
                    Math.Max(
                        kinematics.Heel.StepMeters,
                        kinematics.Toe.StepMeters));
                double visibleSpeed = Math.Max(
                    kinematics.Ankle.SpeedMetersPerSecond,
                    Math.Max(
                        kinematics.Heel.SpeedMetersPerSecond,
                        kinematics.Toe.SpeedMetersPerSecond));
                bool eventChanged = previous.PlantTargetEventIdentity !=
                                    current.PlantTargetEventIdentity;
                bool ownerChanged = !string.Equals(
                    previous.SafetyFloorOwner,
                    current.SafetyFloorOwner,
                    StringComparison.Ordinal);
                bool plantDesiredOutputStepAvailable =
                    previous.PlantInterpolationEvaluated;
                bool plantResponseOutputStepAvailable =
                    current.PreviousResponseOutputAvailable;
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["FootPlacementOutputOffsetStep"] = visibleStep,
                    ["FootPlacementOutputOffsetSpeed"] = visibleSpeed,
                    ["PlantSelectedWorldTargetStep"] = Vector3.Distance(
                        previous.PlantSelectedWorldTarget,
                        current.PlantSelectedWorldTarget),
                    ["DesiredOutputPointStep"] =
                        plantDesiredOutputStepAvailable
                            ? Vector3.Distance(
                                previous.DesiredOutputPoint,
                                current.DesiredOutputPoint)
                            : 0d,
                    ["ResponseOutputPointStep"] =
                        plantResponseOutputStepAvailable
                            ? Vector3.Distance(
                                current.PreviousResponseOutputPoint,
                                current.ResponseOutputPoint)
                            : 0d,
                    ["PlantWorldResidualCaptureDelta"] = Vector3.Distance(
                        current.PlantWorldResidualBeforeCapture,
                        current.PlantWorldResidualCapturedBeforeDecay),
                    ["PlantWorldResidualCaptureContinuityError"] =
                        current.PlantResidualCaptureReason != "None"
                            ? Vector3.Distance(
                                current.PlantWorldResidualCapturedBeforeDecay,
                                current.OriginalSole +
                                current.PlantEffectiveCorrectionBefore -
                                current.PlantSelectedWorldTarget)
                            : Vector3.Distance(
                                current.PlantWorldResidualCapturedBeforeDecay,
                                current.PlantWorldResidualBeforeCapture),
                    ["PlantWorldResidualDecayStep"] = Vector3.Distance(
                        current.PlantWorldResidualCapturedBeforeDecay,
                        current.PlantWorldResidualAfterDecay),
                    ["PlantWorldResidualAfterDecay"] =
                        current.PlantWorldResidualAfterDecay.magnitude,
                    ["PlantWorldResidualAppliedHalfLifeSeconds"] =
                        current.PlantWorldResidualAppliedHalfLifeSeconds,
                    ["CorrectionResponseDesired"] =
                        current.CorrectionResponseDesired,
                    ["CorrectionResponsePrevious"] =
                        current.CorrectionResponsePrevious,
                    ["CorrectionResponseCurrent"] =
                        current.CorrectionResponseCurrent,
                    ["CorrectionResponseSelectedSpeed"] =
                        current.CorrectionResponseSelectedSpeed,
                    ["CorrectionResponseAppliedDelta"] = Math.Abs(
                        current.CorrectionResponseAppliedDelta),
                    ["CorrectionResponseRequestedDirectionChangeDegrees"] =
                        DirectionAngleDegrees(
                            current.CorrectionResponsePreviousDirection,
                            current.CorrectionResponseRequestedDirection),
                    ["CorrectionResponseMaximumDirectionChangeDegrees"] =
                        current.CorrectionResponseMaximumDirectionChangeDegrees,
                    ["CorrectionResponseAppliedDirectionChangeDegrees"] =
                        current.CorrectionResponseAppliedDirectionChangeDegrees,
                    ["PlantEffectiveCorrectionStep"] = Vector3.Distance(
                        previous.PlantEffectiveCorrectionAfter,
                        current.PlantEffectiveCorrectionAfter),
                    ["PlantTargetAppliedVerticalDelta"] = Math.Abs(
                        current.PlantTargetAppliedVerticalDelta),
                    ["PlantOutputDistance"] =
                        current.PlantOutputDistance,
                    ["PlantPenetrationDepth"] =
                        current.PlantPenetrationDepth,
                    ["PresentationDeltaSeconds"] = current.DeltaSeconds,
                    ["BodyTickSpan"] = current.CurrentBodyTick >=
                                       previous.CurrentBodyTick
                        ? current.CurrentBodyTick - previous.CurrentBodyTick
                        : 0d
                };
                ApplyResponseDomainMetrics(current, metrics);
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["scalarResponseEvaluated"] = ScalarResponseEvaluated(current),
                    ["contactResidualResponseEvaluated"] = ContactWorldResponse(current),
                    ["responseDomainTransferred"] = current.CorrectionResponseDomainTransferred,
                    ["plantTargetEventChanged"] = eventChanged,
                    ["plantTargetKindChanged"] = !string.Equals(
                        previous.PlantTargetKind,
                        current.PlantTargetKind,
                        StringComparison.Ordinal),
                    ["plantLockResponseChanged"] = !string.Equals(
                        previous.PlantLockResponse,
                        current.PlantLockResponse,
                        StringComparison.Ordinal),
                    ["plantTargetForceRefreshed"] =
                        current.PlantTargetForceRefreshed,
                    ["plantTargetVerticalClamped"] =
                        current.PlantTargetVerticalClamped,
                    ["plantResidualCaptured"] =
                        current.PlantResidualCaptureReason != "None",
                    ["plantWorldResidualDecayApplied"] =
                        current.PlantWorldResidualDecayApplied,
                    ["plantWorldResidualDecayedOnCapture"] =
                        current.PlantResidualCaptureReason != "None" &&
                        current.PlantWorldResidualDecayApplied,
                    ["plantWorldResidualClearedAtCompletionTolerance"] =
                        current
                            .PlantWorldResidualClearedAtCompletionTolerance,
                    ["targetHeightOwned"] = HasRevisionReason(
                        current.PlantVerticalContinuityOwners,
                        "TargetHeightHistory"),
                    ["plantWorldResidualOwned"] =
                        HasRevisionReason(
                            current.PlantVerticalContinuityOwners,
                            "PlantWorldResidual"),
                    ["correctionResponseOwned"] = HasRevisionReason(
                        current.PlantVerticalContinuityOwners,
                        "CorrectionResponseHistory"),
                    ["plantTargetOwned"] = HasRevisionReason(
                        current.PlantVerticalContinuityOwners,
                        "PlantTarget"),
                    ["correctionResponseInitializedThisFrame"] =
                        current.CorrectionResponseInitializedThisFrame,
                    ["correctionResponseDirectionLimited"] =
                        current.CorrectionResponseDirectionLimited,
                    ["plantDesiredOutputStepAvailable"] =
                        plantDesiredOutputStepAvailable,
                    ["plantResponseOutputStepAvailable"] =
                        plantResponseOutputStepAvailable,
                    ["safetyFloorOwnerChanged"] = ownerChanged,
                    ["physicalOutputAvailable"] = true
                };
                events.Add(new EventFact(
                    "PlantInterpolationOutputJump",
                    current.Side,
                    previous.Frame,
                    current.Frame,
                    current.Frame,
                    current.PlantTargetEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence));
            }
        }

        static void AnalyzeContactAcquisitionContinuity(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                bool contactAcquired =
                    current.PreTransitionReason == "ContactAcquired" ||
                    current.PreTransitionReason == "NewEventContactAcquired";
                bool previousContactOnly =
                    previous.CurrentStep.IsValid &&
                    !previous.CurrentStep.IsSwing &&
                    previous.FormalContact >= 1f - RuntimeGeometryEpsilon;
                if (!Continuous(previous, current) ||
                    !contactAcquired ||
                    previousContactOnly ||
                    !current.HasAnchor ||
                    !current.PlantInterpolationEvaluated ||
                    !current.PreviousResponseOutputAvailable ||
                    previous.Resolved.Outcome != "Ready" ||
                    current.Resolved.Outcome != "Ready" ||
                    current.ComponentUp.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon)
                {
                    continue;
                }
                Vector3 up = current.ComponentUp.normalized;
                Vector3 animationBaselineStep =
                    current.OriginalSole - previous.OriginalSole;
                Vector3 originalSoleToAnchor =
                    current.Anchor - current.OriginalSole;
                Vector3 previousVisibleToAnchor =
                    current.Anchor - previous.Resolved.FinalSole;
                Vector3 previousResponseToAnchor =
                    current.Anchor - current.PreviousResponseOutputPoint;
                Vector3 desiredToResponse =
                    current.ResponseOutputPoint - current.DesiredOutputPoint;
                Vector3 previousVisibleToFinalOutput =
                    current.Resolved.FinalSole - previous.Resolved.FinalSole;
                Vector3 responseOutputToAnchor =
                    current.Anchor - current.ResponseOutputPoint;
                Vector3 finalOutputToAnchor =
                    current.Anchor - current.Resolved.FinalSole;
                Vector3 expectedCapturedResidual =
                    current.PreviousResponseOutputPoint -
                    current.PlantSelectedWorldTarget;
                bool sourceContinuous = string.Equals(
                    previous.SourceIdentity,
                    current.SourceIdentity,
                    StringComparison.Ordinal) &&
                    previous.SourceCycle == current.SourceCycle;
                bool contributionContinuous =
                    previous.ContributionContinuityIdentity ==
                    current.ContributionContinuityIdentity;
                string lineageClassification = sourceContinuous
                    ? contributionContinuous
                        ? "SourceAndContributionContinuous"
                        : "ContributionChanged"
                    : contributionContinuous
                        ? "SourceChanged"
                        : "SourceAndContributionChanged";
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["AnimationBaselineStepMeters"] =
                        animationBaselineStep.magnitude,
                    ["AnimationBaselineHorizontalStepMeters"] =
                        Vector3.ProjectOnPlane(
                            animationBaselineStep,
                            up).magnitude,
                    ["AnimationBaselineAlongUpStepMeters"] =
                        Vector3.Dot(animationBaselineStep, up),
                    ["OriginalSoleToAnchorMeters"] =
                        originalSoleToAnchor.magnitude,
                    ["OriginalSoleToAnchorHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            originalSoleToAnchor,
                            up).magnitude,
                    ["OriginalSoleToAnchorAlongUpMeters"] =
                        Vector3.Dot(originalSoleToAnchor, up),
                    ["PreviousVisibleOutputToAnchorMeters"] =
                        previousVisibleToAnchor.magnitude,
                    ["PreviousVisibleOutputToAnchorHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            previousVisibleToAnchor,
                            up).magnitude,
                    ["PreviousVisibleOutputToAnchorAlongUpMeters"] =
                        Vector3.Dot(previousVisibleToAnchor, up),
                    ["PreviousResponseOutputToAnchorMeters"] =
                        previousResponseToAnchor.magnitude,
                    ["PreviousResponseOutputToAnchorHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            previousResponseToAnchor,
                            up).magnitude,
                    ["PreviousResponseOutputToAnchorAlongUpMeters"] =
                        Vector3.Dot(previousResponseToAnchor, up),
                    ["CapturedResidualMeters"] =
                        current.PlantWorldResidualCapturedBeforeDecay.magnitude,
                    ["ResidualAfterDecayMeters"] =
                        current.PlantWorldResidualAfterDecay.magnitude,
                    ["ResidualDecayStepMeters"] = Vector3.Distance(
                        current.PlantWorldResidualCapturedBeforeDecay,
                        current.PlantWorldResidualAfterDecay),
                    ["ResidualCaptureContinuityErrorMeters"] =
                        Vector3.Distance(
                            expectedCapturedResidual,
                            current.PlantWorldResidualCapturedBeforeDecay),
                    ["DesiredToResponseMeters"] =
                        desiredToResponse.magnitude,
                    ["DesiredToResponseHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            desiredToResponse,
                            up).magnitude,
                    ["DesiredToResponseAlongUpMeters"] =
                        Vector3.Dot(desiredToResponse, up),
                    ["PreviousVisibleToFinalOutputStepMeters"] =
                        previousVisibleToFinalOutput.magnitude,
                    ["PreviousVisibleToFinalOutputHorizontalStepMeters"] =
                        Vector3.ProjectOnPlane(
                            previousVisibleToFinalOutput,
                            up).magnitude,
                    ["PreviousVisibleToFinalOutputAlongUpStepMeters"] =
                        Vector3.Dot(previousVisibleToFinalOutput, up),
                    ["ResponseOutputToAnchorMeters"] =
                        responseOutputToAnchor.magnitude,
                    ["ResponseOutputToAnchorHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            responseOutputToAnchor,
                            up).magnitude,
                    ["ResponseOutputToAnchorAlongUpMeters"] =
                        Vector3.Dot(responseOutputToAnchor, up),
                    ["FinalOutputToAnchorMeters"] =
                        finalOutputToAnchor.magnitude,
                    ["FinalOutputToAnchorHorizontalMeters"] =
                        Vector3.ProjectOnPlane(
                            finalOutputToAnchor,
                            up).magnitude,
                    ["FinalOutputToAnchorAlongUpMeters"] =
                        Vector3.Dot(finalOutputToAnchor, up),
                    ["AnchorToSelectedTargetErrorMeters"] =
                        Vector3.Distance(
                            current.Anchor,
                            current.PlantSelectedWorldTarget),
                    ["CorrectionResponseDesired"] =
                        current.CorrectionResponseDesired,
                    ["CorrectionResponsePrevious"] =
                        current.CorrectionResponsePrevious,
                    ["CorrectionResponseCurrent"] =
                        current.CorrectionResponseCurrent,
                    ["CorrectionResponseAppliedDelta"] =
                        current.CorrectionResponseAppliedDelta
                };
                ApplyResponseDomainMetrics(current, metrics);
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["scalarResponseEvaluated"] = ScalarResponseEvaluated(current),
                    ["contactResidualResponseEvaluated"] = ContactWorldResponse(current),
                    ["contactAcquired"] =
                        current.PreTransitionReason == "ContactAcquired",
                    ["newEventContactAcquired"] =
                        current.PreTransitionReason ==
                        "NewEventContactAcquired",
                    ["sourceContinuous"] = sourceContinuous,
                    ["contributionContinuous"] = contributionContinuous,
                    ["residualCaptured"] =
                        current.PlantResidualCaptureReason != "None",
                    ["residualDecayApplied"] =
                        current.PlantWorldResidualDecayApplied,
                    ["captureContinuitySatisfied"] =
                        metrics["ResidualCaptureContinuityErrorMeters"] <=
                        PositionNoiseFloor,
                    ["anchorMatchesSelectedTarget"] =
                        metrics["AnchorToSelectedTargetErrorMeters"] <=
                        PositionNoiseFloor
                };
                var detail = new CharacterFootContactAcquisitionContinuityAnalysis
                {
                    acquisitionReason = current.PreTransitionReason,
                    lineageClassification = lineageClassification,
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
                    previousEventIdentity = ResolveEventIdentity(previous)
                        .ToString(CultureInfo.InvariantCulture),
                    eventIdentity = ResolveEventIdentity(current)
                        .ToString(CultureInfo.InvariantCulture),
                    anchor = CharacterFootVectorFact.From(current.Anchor),
                    previousOriginalSole = CharacterFootVectorFact.From(
                        previous.OriginalSole),
                    originalSole = CharacterFootVectorFact.From(
                        current.OriginalSole),
                    previousVisibleOutput = CharacterFootVectorFact.From(
                        previous.Resolved.FinalSole),
                    previousResponseOutput = CharacterFootVectorFact.From(
                        current.PreviousResponseOutputPoint),
                    capturedBeforeDecay = CharacterFootVectorFact.From(
                        current.PlantWorldResidualCapturedBeforeDecay),
                    afterDecay = CharacterFootVectorFact.From(
                        current.PlantWorldResidualAfterDecay),
                    desiredOutput = CharacterFootVectorFact.From(
                        current.DesiredOutputPoint),
                    responseOutput = CharacterFootVectorFact.From(
                        current.ResponseOutputPoint),
                    finalOutput = CharacterFootVectorFact.From(
                        current.Resolved.FinalSole),
                    plantResidualCaptureReason =
                        current.PlantResidualCaptureReason,
                    responseDomain = ResponseDomainFact(current),
                    correctionResponseInitializationReason =
                        current.CorrectionResponseInitializationReason
                };
                events.Add(new EventFact(
                    "ContactAcquisitionContinuity",
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
                    contactAcquisitionContinuity: detail));
            }
        }

        static void AnalyzeStableSwingCorrectionResponseCadence(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 2; i < frames.Count; i++)
            {
                FootFrame first = frames[i - 2];
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(first, previous) ||
                    !Continuous(previous, current) ||
                    first.FootMotionState != "Accepted" ||
                    previous.FootMotionState != "Accepted" ||
                    current.FootMotionState != "Accepted" ||
                    first.ConstraintState != "Swing" ||
                    previous.ConstraintState != "Swing" ||
                    current.ConstraintState != "Swing" ||
                    first.SourceIdentity != current.SourceIdentity ||
                    previous.SourceIdentity != current.SourceIdentity ||
                    first.SourceCycle != current.SourceCycle ||
                    previous.SourceCycle != current.SourceCycle ||
                    first.FootMotionEventIdentity == 0 ||
                    first.FootMotionEventIdentity !=
                    current.FootMotionEventIdentity ||
                    previous.FootMotionEventIdentity !=
                    current.FootMotionEventIdentity ||
                    first.FootMotionGroundPathInputIdentity == 0 ||
                    first.FootMotionGroundPathInputIdentity !=
                    current.FootMotionGroundPathInputIdentity ||
                    previous.FootMotionGroundPathInputIdentity !=
                    current.FootMotionGroundPathInputIdentity ||
                    current.PathRevisionReason != "None" ||
                    current.PathResidualRebuilt ||
                    !first.OutputStagesAvailable ||
                    !previous.OutputStagesAvailable ||
                    !current.OutputStagesAvailable ||
                    !first.CorrectionResponseEvaluated ||
                    !previous.CorrectionResponseEvaluated ||
                    !current.CorrectionResponseEvaluated)
                {
                    continue;
                }
                float previousCorrectionStep = Vector3.Distance(
                    first.FinalEffectiveCorrection,
                    previous.FinalEffectiveCorrection);
                float currentCorrectionStep = Vector3.Distance(
                    previous.FinalEffectiveCorrection,
                    current.FinalEffectiveCorrection);
                bool holdToAdvance =
                    previousCorrectionStep < CorrectionHoldMaximumMeters &&
                    currentCorrectionStep > CorrectionAdvanceMinimumMeters;
                bool advanceToHold =
                    previousCorrectionStep > CorrectionAdvanceMinimumMeters &&
                    currentCorrectionStep < CorrectionHoldMaximumMeters;
                string classification = holdToAdvance
                    ? "HoldToAdvance"
                    : advanceToHold
                        ? "AdvanceToHold"
                        : "ContinuousCadence";
                float previousDesiredDelta =
                    previous.CorrectionResponseDesired -
                    first.CorrectionResponseDesired;
                float currentDesiredDelta =
                    current.CorrectionResponseDesired -
                    previous.CorrectionResponseDesired;
                float previousResponseOutputStep = Vector3.Distance(
                    first.ResponseOutputPoint,
                    previous.ResponseOutputPoint);
                float currentResponseOutputStep = Vector3.Distance(
                    previous.ResponseOutputPoint,
                    current.ResponseOutputPoint);
                float previousFormalHeightDelta =
                    previous.SwingFormalFootHeight -
                    first.SwingFormalFootHeight;
                float currentFormalHeightDelta =
                    current.SwingFormalFootHeight -
                    previous.SwingFormalFootHeight;
                float previousEnvelopeStep = Vector3.Distance(
                    first.SwingEnvelopeSample,
                    previous.SwingEnvelopeSample);
                float currentEnvelopeStep = Vector3.Distance(
                    previous.SwingEnvelopeSample,
                    current.SwingEnvelopeSample);
                float previousEnvelopeAlongUpDelta =
                    previous.SwingEnvelopeSampleAlongUp -
                    first.SwingEnvelopeSampleAlongUp;
                float currentEnvelopeAlongUpDelta =
                    current.SwingEnvelopeSampleAlongUp -
                    previous.SwingEnvelopeSampleAlongUp;
                float previousOriginalSoleStep = Vector3.Distance(
                    first.OriginalSole,
                    previous.OriginalSole);
                float currentOriginalSoleStep = Vector3.Distance(
                    previous.OriginalSole,
                    current.OriginalSole);
                float previousEnvelopeDirectionContribution = Vector3.Dot(
                    previous.SwingEnvelopeSample - first.SwingEnvelopeSample,
                    previous.CorrectionResponseDirection);
                float currentEnvelopeDirectionContribution = Vector3.Dot(
                    current.SwingEnvelopeSample -
                    previous.SwingEnvelopeSample,
                    current.CorrectionResponseDirection);
                float previousOriginalSoleDirectionContribution = -Vector3.Dot(
                    previous.OriginalSole - first.OriginalSole,
                    previous.CorrectionResponseDirection);
                float currentOriginalSoleDirectionContribution = -Vector3.Dot(
                    current.OriginalSole - previous.OriginalSole,
                    current.CorrectionResponseDirection);
                bool useCurrentStep = holdToAdvance || !advanceToHold;
                string firstLargeStepStage = ResolveFirstLargeCadenceStage(
                    useCurrentStep
                        ? Math.Abs(currentFormalHeightDelta)
                        : Math.Abs(previousFormalHeightDelta),
                    useCurrentStep
                        ? Math.Abs(currentDesiredDelta)
                        : Math.Abs(previousDesiredDelta),
                    useCurrentStep
                        ? Math.Abs(current.CorrectionResponseAppliedDelta)
                        : Math.Abs(previous.CorrectionResponseAppliedDelta),
                    useCurrentStep
                        ? currentCorrectionStep
                        : previousCorrectionStep);
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["HoldMaximumMeters"] = CorrectionHoldMaximumMeters,
                    ["AdvanceMinimumMeters"] =
                        CorrectionAdvanceMinimumMeters,
                    ["PreviousDesiredResponseDelta"] =
                        previousDesiredDelta,
                    ["CurrentDesiredResponseDelta"] = currentDesiredDelta,
                    ["PreviousCorrectionResponsePrevious"] =
                        previous.CorrectionResponsePrevious,
                    ["PreviousCorrectionResponseCurrent"] =
                        previous.CorrectionResponseCurrent,
                    ["PreviousCorrectionResponseAppliedDelta"] =
                        previous.CorrectionResponseAppliedDelta,
                    ["PreviousCorrectionResponseSelectedSpeed"] =
                        previous.CorrectionResponseSelectedSpeed,
                    ["CurrentCorrectionResponsePrevious"] =
                        current.CorrectionResponsePrevious,
                    ["CurrentCorrectionResponseCurrent"] =
                        current.CorrectionResponseCurrent,
                    ["CurrentCorrectionResponseAppliedDelta"] =
                        current.CorrectionResponseAppliedDelta,
                    ["CurrentCorrectionResponseSelectedSpeed"] =
                        current.CorrectionResponseSelectedSpeed,
                    ["PreviousResponseOutputStep"] =
                        previousResponseOutputStep,
                    ["CurrentResponseOutputStep"] =
                        currentResponseOutputStep,
                    ["PreviousFinalEffectiveCorrectionStep"] =
                        previousCorrectionStep,
                    ["CurrentFinalEffectiveCorrectionStep"] =
                        currentCorrectionStep,
                    ["PreviousFormalFootHeightDelta"] =
                        previousFormalHeightDelta,
                    ["CurrentFormalFootHeightDelta"] =
                        currentFormalHeightDelta,
                    ["PreviousEnvelopeSampleStep"] = previousEnvelopeStep,
                    ["CurrentEnvelopeSampleStep"] = currentEnvelopeStep,
                    ["PreviousEnvelopeSampleAlongUpDelta"] =
                        previousEnvelopeAlongUpDelta,
                    ["CurrentEnvelopeSampleAlongUpDelta"] =
                        currentEnvelopeAlongUpDelta,
                    ["PreviousOriginalSoleStep"] = previousOriginalSoleStep,
                    ["CurrentOriginalSoleStep"] = currentOriginalSoleStep,
                    ["PreviousEnvelopeDirectionContribution"] =
                        previousEnvelopeDirectionContribution,
                    ["CurrentEnvelopeDirectionContribution"] =
                        currentEnvelopeDirectionContribution,
                    ["PreviousOriginalSoleDirectionContribution"] =
                        previousOriginalSoleDirectionContribution,
                    ["CurrentOriginalSoleDirectionContribution"] =
                        currentOriginalSoleDirectionContribution
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["holdToAdvance"] = holdToAdvance,
                    ["advanceToHold"] = advanceToHold,
                    ["previousObservationQueryExecuted"] =
                        previous.LandingObservationQueryExecuted,
                    ["currentObservationQueryExecuted"] =
                        current.LandingObservationQueryExecuted,
                    ["previousObservationReused"] =
                        previous.LandingObservationCacheState == "Reused",
                    ["currentObservationReused"] =
                        current.LandingObservationCacheState == "Reused"
                };
                var detail = new CharacterFootCorrectionResponseCadenceAnalysis
                {
                    classification = classification,
                    firstFrame = first.Frame,
                    previousFrame = previous.Frame,
                    frame = current.Frame,
                    pathIdentity =
                        current.FootMotionGroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    previousPathRevisionReason =
                        previous.PathRevisionReason,
                    currentPathRevisionReason = current.PathRevisionReason,
                    previousObservationCacheState =
                        previous.LandingObservationCacheState,
                    previousObservationQueryPurpose =
                        previous.LandingObservationQueryPurpose,
                    previousObservationRefreshMode =
                        previous.LandingObservationRefreshMode,
                    previousObservationQueryReason =
                        previous.LandingObservationQueryReason,
                    currentObservationCacheState =
                        current.LandingObservationCacheState,
                    currentObservationQueryPurpose =
                        current.LandingObservationQueryPurpose,
                    currentObservationRefreshMode =
                        current.LandingObservationRefreshMode,
                    currentObservationQueryReason =
                        current.LandingObservationQueryReason,
                    firstLargeStepStage = firstLargeStepStage
                };
                events.Add(new EventFact(
                    "StableSwingCorrectionResponseCadence",
                    current.Side,
                    first.Frame,
                    current.Frame,
                    current.Frame,
                    current.FootMotionEventIdentity,
                    current.SourceIdentity,
                    current.SourceCycle,
                    DeltaSeconds(previous) + DeltaSeconds(current),
                    metrics,
                    evidence,
                    correctionResponseCadence: detail));
            }
        }

        static string ResolveFirstLargeCadenceStage(
            float formalHeightDelta,
            float desiredResponseDelta,
            float appliedResponseDelta,
            float finalCorrectionStep)
        {
            if (formalHeightDelta > CorrectionAdvanceMinimumMeters)
                return "FormalFootHeight";
            if (desiredResponseDelta > CorrectionAdvanceMinimumMeters)
                return "DesiredResponse";
            if (appliedResponseDelta > CorrectionAdvanceMinimumMeters)
                return "CorrectionResponseScalar";
            return finalCorrectionStep > CorrectionAdvanceMinimumMeters
                ? "FinalEffectiveCorrection"
                : "Unavailable";
        }

        static void AnalyzeActualFootEnvelopeCounterfactuals(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    previous.FootMotionState != "Accepted" ||
                    current.FootMotionState != "Accepted" ||
                    previous.ConstraintState != "Swing" ||
                    current.ConstraintState != "Swing" ||
                    previous.HasAnchor || current.HasAnchor ||
                    !previous.FinalPhysicalWriteAvailable ||
                    !current.FinalPhysicalWriteAvailable ||
                    previous.FootMotionEventIdentity == 0 ||
                    previous.FootMotionEventIdentity !=
                    current.FootMotionEventIdentity ||
                    !string.Equals(
                        previous.SourceIdentity,
                        current.SourceIdentity,
                        StringComparison.Ordinal) ||
                    previous.SourceCycle != current.SourceCycle ||
                    previous.GroundPathInputIdentity !=
                    current.GroundPathInputIdentity ||
                    current.PathResidualRebuilt ||
                    !previous.PathAvailableAfter ||
                    !current.PathAvailableAfter ||
                    previous.GroundPathState != "Accepted" ||
                    current.GroundPathState != "Accepted")
                {
                    continue;
                }
                CharacterFootVisibleOutputKinematics kinematics =
                    ResolveVisibleOutputKinematics(frames, i);
                double visibleStep = Math.Max(
                    kinematics.Ankle.StepMeters,
                    Math.Max(
                        kinematics.Heel.StepMeters,
                        kinematics.Toe.StepMeters));
                bool uniqueInCorridor =
                    current.ActualEnvelopeCounterfactualState ==
                    "UniqueInCorridor";
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["ActualProgressEnvelopeAdvanceAboveBuilderTarget"] =
                        current.ActualProgressEnvelopeAdvanceAboveBuilderTarget,
                    ["ActualProgressEnvelopeMinimumCorrection"] =
                        current.ActualProgressEnvelopeMinimumCorrection,
                    ["BuilderSwingTargetAlongUp"] =
                        current.ComponentUp.sqrMagnitude >
                        TimeEpsilon * TimeEpsilon
                            ? Vector3.Dot(
                                current.BuilderSwingTargetCorrection,
                                current.ComponentUp.normalized)
                            : 0d,
                    ["ActualFootCrossTrackDistance"] =
                        current.ActualFootCrossTrackDistance,
                    ["ActualEnvelopeCandidateCount"] =
                        current.ActualEnvelopeCandidateCount,
                    ["ActualEnvelopeHeightSpan"] =
                        current.ActualEnvelopeHeightSpan,
                    ["GroundEnvelopeHardClamp"] =
                        current.SafetyFloorOwner == "GroundPathEnvelope"
                            ? current.SafetyFloorClampMeters
                            : 0d,
                    ["FootPlacementOutputOffsetStep"] = visibleStep,
                    ["PresentationDeltaSeconds"] = current.DeltaSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["uniqueInCorridor"] = uniqueInCorridor,
                    ["ambiguousInCorridor"] =
                        current.ActualEnvelopeCounterfactualState ==
                        "AmbiguousInCorridor",
                    ["outsideGroundPathCorridor"] =
                        current.ActualEnvelopeCounterfactualState ==
                        "OutsideGroundPathCorridor",
                    ["noIntersection"] =
                        current.ActualEnvelopeCounterfactualState ==
                        "NoIntersection",
                    ["counterfactualUnavailable"] =
                        current.ActualEnvelopeCounterfactualState ==
                        "Unavailable",
                    ["groundEnvelopeOwner"] =
                        current.SafetyFloorOwner == "GroundPathEnvelope",
                    ["actualProgressCorrectionAvailable"] =
                        current.ActualProgressEnvelopeCorrectionAvailable,
                    ["visibleOutputAboveTwoCentimeters"] =
                        visibleStep > 0.02d
                };
                events.Add(new EventFact(
                    "ActualFootEnvelopeCounterfactual",
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
                bool contactOutputPair = IsContactOutputState(previous.ConstraintState) ||
                    IsContactOutputState(current.ConstraintState);
                bool acceptedUnanchoredSwingPair =
                    previous.ConstraintState == "Swing" &&
                    current.ConstraintState == "Swing" &&
                    previous.FootMotionState == "Accepted" &&
                    current.FootMotionState == "Accepted" &&
                    !previous.HasAnchor && !current.HasAnchor;
                if (!contactOutputPair && !acceptedUnanchoredSwingPair)
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
                if (contactOutputPair)
                {
                    category = "ContactStateOutputJump";
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
                ulong bodyTickSpan = current.CurrentBodyTick >=
                                     previous.CurrentBodyTick
                    ? current.CurrentBodyTick - previous.CurrentBodyTick
                    : 0;
                bool lowPresentationCadence =
                    current.DeltaSeconds >=
                    LowPresentationSamplingDeltaSeconds ||
                    bodyTickSpan > 1;
                bool outputSpeedAnomaly =
                    primarySpeed > SwingSpeedAnomalyMetersPerSecond;
                string presentationSamplingClassification =
                    outputSpeedAnomaly
                        ? lowPresentationCadence
                            ? "LowCadenceSpeedAnomaly"
                            : "RegularCadenceSpeedAnomaly"
                        : lowPresentationCadence
                            ? "LowCadenceNormalSpeed"
                            : "RegularCadenceNormalSpeed";
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
                    postTransitionEvaluated = current.PostTransitionEvaluated,
                    postTransitionReason = current.PostTransitionEvaluated
                        ? current.PostTransitionReason : null,
                    postTransitionSource = current.PostTransitionEvaluated
                        ? current.PostTransitionSource : null,
                    postTransitionTarget = current.PostTransitionEvaluated
                        ? current.PostTransitionTarget : null,
                    postTransitionAnchorCommand =
                        current.PostTransitionEvaluated
                            ? current.PostTransitionAnchorCommand : null,
                    stateTargetCorrection = CharacterFootVectorFact.From(
                        current.StateTargetCorrection),
                    interpolationPolicy = current.InterpolationPolicy,
                    interpolationOutputCorrection =
                        CharacterFootVectorFact.From(
                            current.InterpolationOutputCorrection),
                    interpolationCompleted = current.InterpolationCompleted,
                    plantInterpolationEvaluated =
                        current.PlantInterpolationEvaluated,
                    targetHeightComponentUp =
                        CharacterFootVectorFact.From(current.ComponentUp),
                    plantTargetEventIdentity =
                        current.PlantTargetEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    plantTargetVerified = current.PlantTargetVerified,
                    plantTargetKind = current.PlantTargetKind,
                    plantLockResponse = current.PlantLockResponse,
                    plantLockWeightCompleted =
                        current.PlantLockWeightCompleted,
                    plantDesiredPoint = CharacterFootVectorFact.From(
                        current.PlantDesiredPoint),
                    plantFilteredPoint = CharacterFootVectorFact.From(
                        current.PlantFilteredPoint),
                    swingTargetHeightAdoptionMode =
                        current.SwingTargetHeightAdoptionMode,
                    plantTargetHeightAdoptionMode =
                        current.PlantTargetHeightAdoptionMode,
                    plantTargetMaximumVerticalSpeed =
                        current.PlantTargetMaximumVerticalSpeed,
                    plantTargetHeightBefore =
                        current.PlantTargetHeightBefore,
                    plantTargetHeightTarget =
                        current.PlantTargetHeightTarget,
                    plantTargetVerticalDelta =
                        current.PlantTargetVerticalDelta,
                    plantTargetAppliedVerticalDelta =
                        current.PlantTargetAppliedVerticalDelta,
                    plantTargetHeightAfter =
                        current.PlantTargetHeightAfter,
                    plantTargetHeightEventIdentity =
                        current.PlantTargetHeightEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    plantTargetHeightUpdateReason =
                        current.PlantTargetHeightUpdateReason,
                    plantTargetVerticalClamped =
                        current.PlantTargetVerticalClamped,
                    plantPreviousSelectedWorldTarget =
                        CharacterFootVectorFact.From(
                            current.PlantPreviousSelectedWorldTarget),
                    plantSelectedWorldTarget = CharacterFootVectorFact.From(
                        current.PlantSelectedWorldTarget),
                    previousResponseOutputAvailable =
                        current.PreviousResponseOutputAvailable,
                    previousResponseOutputPoint =
                        CharacterFootVectorFact.From(
                            current.PreviousResponseOutputPoint),
                    desiredOutputPoint = CharacterFootVectorFact.From(
                        current.DesiredOutputPoint),
                    responseOutputPoint = CharacterFootVectorFact.From(
                        current.ResponseOutputPoint),
                    plantResidualCaptureReason =
                        current.PlantResidualCaptureReason,
                    plantWorldResidualBeforeCapture =
                        CharacterFootVectorFact.From(
                            current.PlantWorldResidualBeforeCapture),
                    plantWorldResidualCapturedBeforeDecay =
                        CharacterFootVectorFact.From(
                            current.PlantWorldResidualCapturedBeforeDecay),
                    plantWorldResidualDecayApplied =
                        current.PlantWorldResidualDecayApplied,
                    plantWorldResidualBaseHalfLifeSeconds =
                        current.PlantWorldResidualBaseHalfLifeSeconds,
                    plantWorldResidualDeadlineHalfLifeAvailable =
                        current.PlantWorldResidualDeadlineHalfLifeAvailable,
                    plantWorldResidualDeadlineHalfLifeSeconds =
                        current.PlantWorldResidualDeadlineHalfLifeSeconds,
                    plantWorldResidualAppliedHalfLifeSeconds =
                        current.PlantWorldResidualAppliedHalfLifeSeconds,
                    plantWorldResidualAfterDecay =
                        CharacterFootVectorFact.From(
                            current.PlantWorldResidualAfterDecay),
                    plantWorldResidualCompletionTolerance =
                        current.PlantWorldResidualCompletionTolerance,
                    plantWorldResidualClearedAtCompletionTolerance =
                        current
                            .PlantWorldResidualClearedAtCompletionTolerance,
                    correctionResponseEvaluated =
                        current.CorrectionResponseEvaluated,
                    responseDomain = ResponseDomainFact(current),
                    correctionResponseInitializedBefore =
                        current.CorrectionResponseInitializedBefore,
                    correctionResponseInitializedThisFrame =
                        current.CorrectionResponseInitializedThisFrame,
                    correctionResponseInitializationReason =
                        current.CorrectionResponseInitializationReason,
                    correctionResponseDesired =
                        ScalarResponseValue(current, current.CorrectionResponseDesired),
                    correctionResponseRequestedDirection =
                        CharacterFootVectorFact.From(
                            current.CorrectionResponseRequestedDirection),
                    correctionResponsePreviousDirection =
                        CharacterFootVectorFact.From(
                            current.CorrectionResponsePreviousDirection),
                    correctionResponseDirectionLimited =
                        current.CorrectionResponseDirectionLimited,
                    correctionResponseMaximumDirectionChangeDegrees =
                        current.CorrectionResponseMaximumDirectionChangeDegrees,
                    correctionResponseAppliedDirectionChangeDegrees =
                        current.CorrectionResponseAppliedDirectionChangeDegrees,
                    correctionResponseVisibleOutputTransferred =
                        current.CorrectionResponseVisibleOutputTransferred,
                    correctionResponseBeforeRebase =
                        ScalarResponseValue(current, current.CorrectionResponseBeforeRebase),
                    correctionResponsePrevious =
                        ScalarResponseValue(current, current.CorrectionResponsePrevious),
                    correctionResponseCurrent =
                        ScalarResponseValue(current, current.CorrectionResponseCurrent),
                    correctionResponseDirection =
                        CharacterFootVectorFact.From(
                            current.CorrectionResponseDirection),
                    correctionResponseDeltaDirection =
                        current.CorrectionResponseDeltaDirection,
                    correctionResponseSelectedSpeed =
                        ScalarResponseValue(current, current.CorrectionResponseSelectedSpeed),
                    correctionResponseAppliedDelta =
                        ScalarResponseValue(current, current.CorrectionResponseAppliedDelta),
                    plantVerticalContinuityOwners =
                        current.PlantVerticalContinuityOwners,
                    plantEffectiveCorrectionBefore =
                        CharacterFootVectorFact.From(
                            current.PlantEffectiveCorrectionBefore),
                    plantEffectiveCorrectionAfter =
                        CharacterFootVectorFact.From(
                            current.PlantEffectiveCorrectionAfter),
                    plantOutputDistance = current.PlantOutputDistance,
                    plantPenetrationDepth = current.PlantPenetrationDepth,
                    presentationDeltaSeconds = current.DeltaSeconds,
                    bodyTickSpan = bodyTickSpan,
                    presentationSamplingClassification =
                        presentationSamplingClassification,
                    lowPresentationCadence = lowPresentationCadence,
                    outputSpeedAnomaly = outputSpeedAnomaly,
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
                if (contactOutputPair)
                {
                    double ankleAdditional = ContactAdditionalStep(in kinematics.Ankle, out float ankleBlend);
                    double heelAdditional = ContactAdditionalStep(in kinematics.Heel, out float heelBlend);
                    double toeAdditional = ContactAdditionalStep(in kinematics.Toe, out float toeBlend);
                    double additional = Math.Max(ankleAdditional, Math.Max(heelAdditional, toeAdditional));
                    metrics["ContactStateAdditionalOutputStep"] = additional;
                    metrics["ContactStateAdditionalOutputSpeed"] = additional / DeltaSeconds(current);
                    metrics["ContactAnkleAdditionalOutputStep"] = ankleAdditional;
                    metrics["ContactHeelAdditionalOutputStep"] = heelAdditional;
                    metrics["ContactToeAdditionalOutputStep"] = toeAdditional;
                    metrics["ContactAnkleMotionBlendParameter"] = ankleBlend;
                    metrics["ContactHeelMotionBlendParameter"] = heelBlend;
                    metrics["ContactToeMotionBlendParameter"] = toeBlend;
                }
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["visibleOutputAvailable"] = true,
                    ["accelerationAvailable"] = accelerationAvailable,
                    ["jerkAvailable"] = jerkAvailable,
                    ["lowPresentationCadence"] =
                        lowPresentationCadence,
                    ["outputSpeedAnomaly"] = outputSpeedAnomaly,
                    ["stableSwing"] =
                        category == "StableSwingOutputJump",
                    ["pathRevision"] =
                        category == "PathRevisionOutputJump",
                    ["swingToLanding"] = swingToLanding,
                    ["contactStateOutput"] = contactOutputPair,
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

        static bool IsContactOutputState(string state) =>
            state == "Landing" || state == "Locked" || state == "Releasing";

        static double ContactAdditionalStep(
            in CharacterFootOutputProbeKinematics probe, out float blend)
        {
            Vector3 physical = probe.Physical - probe.PreviousPhysical;
            Vector3 source = probe.Source - probe.PreviousSource;
            blend = source.sqrMagnitude > RuntimeGeometryEpsilon * RuntimeGeometryEpsilon
                ? Mathf.Clamp01(Vector3.Dot(physical, source) / source.sqrMagnitude) : 0f;
            return (physical - source * blend).magnitude;
        }

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
            var forcedVerificationByEvent = new HashSet<string>(
                StringComparer.Ordinal);
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
                bool forcedVerification =
                    current.LandingObservationQueryPurpose ==
                    "CurrentContactVerification" &&
                    current.LandingObservationRefreshMode ==
                    "ForcedPlantVerification";
                string forcedVerificationKey = string.Concat(
                    current.Side,
                    ":",
                    current.SourceIdentity,
                    ":",
                    current.ObservedLandingEventIdentity.ToString(
                        CultureInfo.InvariantCulture));
                bool firstForcedVerification = forcedVerification &&
                    forcedVerificationByEvent.Add(forcedVerificationKey);
                FootFrame previousCommitted = i > 0 &&
                    Continuous(frames[i - 1], current)
                        ? frames[i - 1]
                        : null;
                bool contactEventChanged = previousCommitted != null &&
                    previousCommitted.Resolved.ContactAvailable &&
                    (previousCommitted.ConstraintState == "Landing" ||
                     previousCommitted.ConstraintState == "Locked" ||
                     previousCommitted.ConstraintState == "Releasing") &&
                    previousCommitted.InputEvents.Current.Identity != 0 &&
                    current.InputEvents.Current.Identity != 0 &&
                    previousCommitted.InputEvents.Current.Identity !=
                    current.InputEvents.Current.Identity;
                bool contactEventAcquisitionConsistent =
                    !contactEventChanged ||
                    forcedVerification && firstForcedVerification &&
                    current.LandingObservationQueryExecuted &&
                    current.PreTransitionReason ==
                    "NewEventContactAcquired" &&
                    current.PreTransitionSource ==
                    previousCommitted.ConstraintState &&
                    current.PreTransitionTarget == "Landing" &&
                    current.PreTransitionAnchorCommand == "Create" &&
                    current.ConstraintState == "Landing" &&
                    current.Resolved.SupportTarget.Available &&
                    current.Resolved.SupportTarget.PositionEvent ==
                    current.InputEvents.Current.Identity &&
                    current.Resolved.SupportTarget.NormalEvent ==
                    current.InputEvents.Current.Identity &&
                    current.SelectedSupportTarget.Available &&
                    current.SelectedSupportTarget.PositionEvent ==
                    current.InputEvents.Current.Identity &&
                    current.SelectedSupportTarget.NormalEvent ==
                    current.InputEvents.Current.Identity &&
                    current.Resolved.ContactAvailable &&
                    current.Resolved.ContactEventIdentity ==
                    current.InputEvents.Current.Identity &&
                    current.PlantTargetEventIdentity ==
                    current.InputEvents.Current.Identity;
                if (contactEventChanged &&
                    !contactEventAcquisitionConsistent)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Contact EventChanged acquisition is inconsistent " +
                        $"Frame={current.Frame} Side={current.Side}.");
                }
                bool previousCommittedIdentityMatches =
                    previousCommitted != null &&
                    previousCommitted.LandingObservationIdentity ==
                    current.LandingObservationIdentity;
                bool duplicateQuery =
                    current.LandingObservationQueryExecuted &&
                    (forcedVerification && !firstForcedVerification ||
                     previousCommittedIdentityMatches &&
                     !forcedVerification);
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
                bool purposeMatchesRefresh = forcedVerification ||
                    current.LandingObservationQueryPurpose ==
                    "FutureLanding" &&
                    (current.LandingObservationRefreshMode == "Thresholded" ||
                     current.LandingObservationRefreshMode ==
                     "ChangedSlidingAdmissionInput");
                bool queryThresholdContractConsistent =
                    distanceExceeded == distanceReason &&
                    angleExceeded == angleReason &&
                    queried == hasQueryReason &&
                    purposeMatchesRefresh &&
                    (!forcedVerification || queried) &&
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
                    queryPurpose =
                        current.LandingObservationQueryPurpose,
                    refreshMode = current.LandingObservationRefreshMode,
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
                    forcedPlantVerification = forcedVerification,
                    firstForcedPlantVerification =
                        firstForcedVerification,
                    duplicateQuery = duplicateQuery,
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
                    ["forcedPlantVerification"] = forcedVerification,
                    ["futureLandingPurpose"] =
                        current.LandingObservationQueryPurpose ==
                        "FutureLanding",
                    ["currentContactVerificationPurpose"] =
                        current.LandingObservationQueryPurpose ==
                        "CurrentContactVerification",
                    ["firstForcedPlantVerification"] =
                        firstForcedVerification,
                    ["contactEventChanged"] = contactEventChanged,
                    ["duplicateQuery"] = duplicateQuery,
                    ["purposeMatchesRefresh"] = purposeMatchesRefresh,
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
                    !previous.SelectedPhase.InApproachContactToLanding ||
                    !current.SelectedPhase.InApproachContactToLanding ||
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
                            previous.SelectedPhase.EventPhase,
                        selectedEventPhase =
                            current.SelectedPhase.EventPhase,
                        previousSelectedApproachContactToLandingProgress =
                            previous.SelectedPhase.ApproachContactToLandingProgress,
                        selectedApproachContactToLandingProgress =
                            current.SelectedPhase.ApproachContactToLandingProgress,
                        previousSelectedLandingPhase =
                            previous.SelectedPhase.LandingPhase,
                        selectedLandingPhase =
                            current.SelectedPhase.LandingPhase,
                        previousCurrentEventPhase =
                            previous.CurrentStep.EventPhase,
                        currentEventPhase =
                            current.CurrentStep.EventPhase,
                        previousCurrentApproachContactToLandingProgress =
                            previous.CurrentStep.ApproachContactToLandingProgress,
                        currentApproachContactToLandingProgress =
                            current.CurrentStep.ApproachContactToLandingProgress,
                        previousSelectedInApproachContactToLanding =
                            previous.SelectedPhase.InApproachContactToLanding,
                        selectedInApproachContactToLanding =
                            current.SelectedPhase.InApproachContactToLanding,
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
                        current.SelectedPhase.EventPhase,
                    ["SelectedApproachContactToLandingProgress"] =
                        current.SelectedPhase.ApproachContactToLandingProgress,
                    ["CurrentEventPhase"] =
                        current.CurrentStep.EventPhase,
                    ["CurrentApproachContactToLandingProgress"] =
                        current.CurrentStep.ApproachContactToLandingProgress
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
                double originalExtensionPeak = window.Max(
                    frame => frame.OriginalExtensionRatio);
                double targetExtensionPeak = window.Max(frame => frame.TargetExtensionRatio);
                double solvedExtensionPeak = window.Max(frame => frame.SolvedExtensionRatio);
                double bendMinimum = window.Min(frame => frame.SolvedBendDegrees);
                double originalCompressionMinimum = window.Min(
                    frame => frame.OriginalCompressionReserve);
                double targetCompressionMinimum = window.Min(
                    frame => frame.TargetCompressionReserve);
                double solvedCompressionMinimum = window.Min(
                    frame => frame.SolvedCompressionReserve);
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
                        ["originalCompressionReserveMinimumMeters"] =
                            originalCompressionMinimum,
                        ["targetCompressionReserveMinimumMeters"] =
                            targetCompressionMinimum,
                        ["solvedCompressionReserveMinimumMeters"] =
                            solvedCompressionMinimum,
                        ["correctionStepMaximumMeters"] = correctionStep,
                        ["originalExtensionRatioPeak"] =
                            originalExtensionPeak,
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
                        ["landingReachPelvisObservationMinimumAlongUpMeters"] =
                            landingReach.pelvisReachObservationMinimumAlongUpMeters,
                        ["landingReachPelvisObservationMaximumAlongUpMeters"] =
                            landingReach.pelvisReachObservationMaximumAlongUpMeters,
                        ["landingReachIntersectionMinimumAlongUpMeters"] =
                            landingReach.intersectionMinimumAlongUpMeters,
                        ["landingReachIntersectionMaximumAlongUpMeters"] =
                            landingReach.intersectionMaximumAlongUpMeters,
                        ["landingReachSupportConflictGapMeters"] =
                            landingReach.pelvisReachObservationConflictGapMeters,
                        ["landingReachActualTargetCompressionReserveMeters"] =
                            landingReach.actualTargetCompressionReserveMeters,
                        ["landingReachSolvedCompressionReserveMeters"] =
                            landingReach.solvedCompressionReserveMeters,
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
                        ["landingReachPelvisReachObservationAvailable"] =
                            landingReach.pelvisReachObservationEvaluated,
                        ["landingReachSupportIntersectionExists"] =
                            landingReach.pelvisReachObservationIntersectionExists,
                        ["landingReachRuntimeEvaluated"] =
                            landingReach.runtimeReachEvaluated,
                        ["landingReachRuntimeAvailable"] =
                            landingReach.runtimeReachAvailable,
                        ["landingReachResolvedRequestAvailable"] =
                            landingReach.resolvedReachRequestAvailable,
                        ["landingReachNoPelvisReachObservationLandingOnly"] =
                            landingReach.classification ==
                            "NoPelvisReachObservationLandingOnly",
                        ["landingReachSupportIntersection"] =
                            landingReach.classification ==
                            "PelvisReachObservationIntersection",
                        ["landingReachSupportConflict"] =
                            landingReach.classification ==
                            "PelvisReachObservationConflict",
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
                CharacterFootOutputBoundaryMotion entryMotion = hasEntry
                    ? ResolveOutputBoundaryMotion(entryPrevious, window[0])
                    : default;
                CharacterFootOutputBoundaryMotion exitMotion = hasExit
                    ? ResolveOutputBoundaryMotion(window[^1], exitNext)
                    : default;
                int peakFrame = entryMotion.StateAdditionalOutputStepMeters >=
                                exitMotion.StateAdditionalOutputStepMeters
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
                    ["entryCorrectionReexpressionStepMeters"] =
                        entryMotion.CorrectionReexpressionStepMeters,
                    ["exitCorrectionReexpressionStepMeters"] =
                        exitMotion.CorrectionReexpressionStepMeters,
                    ["entryCorrectedSoleStepMeters"] =
                        entryMotion.CorrectedSoleStepMeters,
                    ["exitCorrectedSoleStepMeters"] =
                        exitMotion.CorrectedSoleStepMeters,
                    ["entryAnimatedSoleStepMeters"] =
                        entryMotion.AnimatedSoleStepMeters,
                    ["exitAnimatedSoleStepMeters"] =
                        exitMotion.AnimatedSoleStepMeters,
                    ["entryStateAdditionalOutputStepMeters"] =
                        entryMotion.StateAdditionalOutputStepMeters,
                    ["exitStateAdditionalOutputStepMeters"] =
                        exitMotion.StateAdditionalOutputStepMeters,
                    ["entryOutputBlendParameter"] =
                        entryMotion.OutputBlendParameter,
                    ["exitOutputBlendParameter"] =
                        exitMotion.OutputBlendParameter,
                    ["entryFinalPhysicalAnkleStepMeters"] =
                        entryMotion.FinalPhysicalAnkleStepMeters,
                    ["exitFinalPhysicalAnkleStepMeters"] =
                        exitMotion.FinalPhysicalAnkleStepMeters,
                    ["entryFinalPhysicalSoleStepMeters"] =
                        entryMotion.FinalPhysicalSoleStepMeters,
                    ["exitFinalPhysicalSoleStepMeters"] =
                        exitMotion.FinalPhysicalSoleStepMeters,
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
                    ["entryPhysicalOutputAvailable"] =
                        entryMotion.PhysicalOutputAvailable,
                    ["exitPhysicalOutputAvailable"] =
                        exitMotion.PhysicalOutputAvailable,
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

        static CharacterFootOutputBoundaryMotion ResolveOutputBoundaryMotion(
            FootFrame previous,
            FootFrame current)
        {
            Vector3 correctedDelta =
                current.CorrectedSole - previous.CorrectedSole;
            Vector3 animatedDelta =
                current.OriginalSole - previous.OriginalSole;
            float animatedMagnitudeSquared = animatedDelta.sqrMagnitude;
            float blend = animatedMagnitudeSquared >
                          RuntimeGeometryEpsilon * RuntimeGeometryEpsilon
                ? Mathf.Clamp01(
                    Vector3.Dot(correctedDelta, animatedDelta) /
                    animatedMagnitudeSquared)
                : 0f;
            Vector3 stateAdditionalDelta =
                correctedDelta - animatedDelta * blend;
            bool physicalAvailable =
                previous.FinalPhysicalWriteAvailable &&
                current.FinalPhysicalWriteAvailable;
            return new CharacterFootOutputBoundaryMotion(
                Vector3.Distance(
                    previous.EffectiveCorrection,
                    current.EffectiveCorrection),
                correctedDelta.magnitude,
                animatedDelta.magnitude,
                stateAdditionalDelta.magnitude,
                blend,
                physicalAvailable,
                physicalAvailable
                    ? Vector3.Distance(
                        FinalPhysicalAnkleWorld(previous),
                        FinalPhysicalAnkleWorld(current))
                    : 0d,
                physicalAvailable
                    ? Vector3.Distance(
                        FinalSole(previous),
                        FinalSole(current))
                    : 0d);
        }

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
                CharacterFootOutputBoundaryMotion outputMotion =
                    ResolveOutputBoundaryMotion(previous, current);
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
                        entryCorrectionReexpressionStepMeters =
                            outputMotion.CorrectionReexpressionStepMeters,
                        entryCorrectionReexpressionAlongUpMeters =
                            correctionAlongUp,
                        entryCorrectedSoleStepMeters =
                            outputMotion.CorrectedSoleStepMeters,
                        entryAnimatedSoleStepMeters =
                            outputMotion.AnimatedSoleStepMeters,
                        entryStateAdditionalOutputStepMeters =
                            outputMotion.StateAdditionalOutputStepMeters,
                        entryOutputBlendParameter =
                            outputMotion.OutputBlendParameter,
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
                    ["entryCorrectionReexpressionStepMeters"] =
                        outputMotion.CorrectionReexpressionStepMeters,
                    ["entryCorrectionReexpressionAlongUpMeters"] =
                        correctionAlongUp,
                    ["entryCorrectedSoleStepMeters"] =
                        outputMotion.CorrectedSoleStepMeters,
                    ["entryAnimatedSoleStepMeters"] =
                        outputMotion.AnimatedSoleStepMeters,
                    ["entryStateAdditionalOutputStepMeters"] =
                        outputMotion.StateAdditionalOutputStepMeters,
                    ["entryOutputBlendParameter"] =
                        outputMotion.OutputBlendParameter,
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
                string lockResponse = frames[index].LockResponse;
                if (lockResponse != "FullAnchor" &&
                    lockResponse != "Sliding")
                {
                    throw new InvalidDataException(
                        $"Locked Foot response is invalid Frame={frames[index].Frame} Side={frames[index].Side} Response={lockResponse}.");
                }
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].ConstraintState == "Locked" &&
                       frames[index + 1].FootMotionEventIdentity == eventIdentity &&
                       frames[index + 1].LockResponse == lockResponse)
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
                List<double> horizontalAnchorDistances = window
                    .Select(frame => (double)Vector3.ProjectOnPlane(
                        frame.CorrectedSole - frame.Anchor,
                        frame.ComponentUp.normalized).magnitude)
                    .ToList();
                double sink = Math.Max(0d, -alongUp.Min());
                double drift = anchorDistances[^1] - anchorDistances[0];
                double visibleStep = MaximumVectorStep(
                    window.Select(frame => frame.CorrectedSole).ToList());
                bool physicalAnchorAvailable = window.All(frame =>
                    frame.FinalPhysicalWriteAvailable &&
                    frame.FinalPhysicalWriteCompletionIdentity == frame.CompletionIdentity &&
                    frame.CurrentContactAnchorAvailable &&
                    frame.CurrentContactAnchorEventIdentity == frame.FootMotionEventIdentity);
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["anchorDisplacementMeters"] = anchorDisplacement,
                    ["correctedSoleAnchorDistanceEntryMeters"] = anchorDistances[0],
                    ["correctedSoleAnchorDistanceExitMeters"] = anchorDistances[^1],
                    ["correctedSoleAnchorDistanceMaximumMeters"] = anchorDistances.Max(),
                    ["correctedSoleAnchorDistanceMinimumMeters"] = anchorDistances.Min(),
                    ["correctedSoleAnchorDistanceChangeMeters"] = drift,
                    ["correctedSoleAnchorHorizontalDistanceEntryMeters"] =
                        horizontalAnchorDistances[0],
                    ["correctedSoleAnchorHorizontalDistanceExitMeters"] =
                        horizontalAnchorDistances[^1],
                    ["correctedSoleAnchorHorizontalDistanceMaximumMeters"] =
                        horizontalAnchorDistances.Max(),
                    ["lockWeightEntry"] = window[0].FormalLockWeight,
                    ["lockWeightExit"] = window[^1].FormalLockWeight,
                    ["lockWeightMinimum"] = window.Min(frame => frame.FormalLockWeight),
                    ["soleAlongUpEntryMeters"] = alongUp[0],
                    ["soleAlongUpMinimumMeters"] = alongUp.Min(),
                    ["soleAlongUpAbsoluteMaximumMeters"] =
                        alongUp.Max(value => Math.Abs(value)),
                    ["soleDownwardExcursionMeters"] = sink,
                    ["supportEntry"] = window[0].FormalSupport,
                    ["supportExit"] = window[^1].FormalSupport,
                    ["visibleSoleStepMaximumMeters"] = visibleStep
                };
                if (physicalAnchorAvailable)
                    metrics["physicalSoleAnchorHorizontalDistanceMaximumMeters"] =
                        window.Max(frame => (double)Vector3.ProjectOnPlane(
                            (frame.FinalHeel + frame.FinalToe) * 0.5f -
                            frame.CurrentContactAnchorPoint,
                            frame.ComponentUp.normalized).magnitude);
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["physicalAnchorAvailable"] = physicalAnchorAvailable,
                    ["anchorStable"] = anchorDisplacement <= PositionNoiseFloor,
                    ["fullAnchorResponse"] = lockResponse == "FullAnchor",
                    ["groundedThroughout"] = window.All(frame => frame.Grounded),
                    ["lockWeightDecreased"] = window[^1].FormalLockWeight < window[0].FormalLockWeight,
                    ["slidingContinuityContractAvailable"] = false,
                    ["slideDistanceLimitAvailable"] = false,
                    ["slidingResponse"] = lockResponse == "Sliding",
                    ["supportStayedPositive"] = window.All(frame => frame.FormalSupport > 0f)
                };
                EventFact fact = new EventFact(
                    lockResponse == "FullAnchor"
                        ? "LockedFullAnchor"
                        : "LockedSliding",
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
                PositionNoiseFloor * PositionNoiseFloor ||
                path.GroundPathComponentUp.sqrMagnitude <=
                PositionNoiseFloor * PositionNoiseFloor)
            {
                return false;
            }
            Vector3 up = currentState.ComponentUp.normalized;
            Vector3 groundPathUp = path.GroundPathComponentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                path.NextLanding - path.LastLanding,
                groundPathUp);
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
            float rawTargetHeight = Vector3.Dot(
                envelopeSample,
                up) + currentState.SwingFormalFootHeight;
            float targetHeightDelta = rawTargetHeight -
                                      currentState
                                          .SwingFilteredTargetHeightBefore;
            float maximumHeightDelta = ResolveVerticalHistoryDelta(
                currentState.DeltaSeconds,
                currentState.SwingTargetMaximumVerticalSpeed);
            float filteredTargetHeight =
                currentState.SwingFilteredTargetHeightBefore +
                (currentState.SwingTargetHeightUpdateHeld
                    ? 0f
                    : currentState.SwingTargetHeightForceRefreshed
                    ? targetHeightDelta
                    : currentState.SwingTargetHeightRateLimited
                    ? Mathf.Clamp(
                        targetHeightDelta,
                        -maximumHeightDelta,
                        maximumHeightDelta)
                    : targetHeightDelta);
            if (!float.IsFinite(rawTargetHeight) ||
                !float.IsFinite(filteredTargetHeight) ||
                !float.IsFinite(originalSoleHeight))
            {
                return false;
            }
            target = up * Mathf.Max(
                0f,
                filteredTargetHeight - originalSoleHeight);
            return FiniteVector(target);
        }

        static float ResolveVerticalHistoryDelta(
            float deltaSeconds,
            float maximumSpeed)
        {
            if (deltaSeconds <= 0f)
                return 0f;
            return maximumSpeed * deltaSeconds;
        }

        static Vector3 AdvanceResidual(
            Vector3 residual,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return residual;
            float alpha = 1f -
                          Mathf.Pow(0.5f, deltaSeconds / halfLifeSeconds);
            return Vector3.LerpUnclamped(residual, default, alpha);
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

        static bool FiniteRotation(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);

        static Quaternion NormalizeRotation(Quaternion value)
        {
            float magnitude = MathF.Sqrt(
                value.x * value.x + value.y * value.y +
                value.z * value.z + value.w * value.w);
            if (magnitude <= RuntimeGeometryEpsilon)
                return Quaternion.identity;
            float inverse = 1f / magnitude;
            return new Quaternion(
                value.x * inverse,
                value.y * inverse,
                value.z * inverse,
                value.w * inverse);
        }

        static Quaternion InverseRotation(Quaternion value)
        {
            float norm = value.x * value.x + value.y * value.y +
                         value.z * value.z + value.w * value.w;
            if (norm <= RuntimeGeometryEpsilon)
                return Quaternion.identity;
            float inverse = 1f / norm;
            return new Quaternion(
                -value.x * inverse,
                -value.y * inverse,
                -value.z * inverse,
                value.w * inverse);
        }

        static Quaternion MultiplyRotation(Quaternion left, Quaternion right) =>
            new Quaternion(
                left.w * right.x + left.x * right.w +
                left.y * right.z - left.z * right.y,
                left.w * right.y - left.x * right.z +
                left.y * right.w + left.z * right.x,
                left.w * right.z + left.x * right.y -
                left.y * right.x + left.z * right.w,
                left.w * right.w - left.x * right.x -
                left.y * right.y - left.z * right.z);

        static Vector3 RotateVector(Quaternion rotation, Vector3 value)
        {
            float x = rotation.x * 2f;
            float y = rotation.y * 2f;
            float z = rotation.z * 2f;
            float xx = rotation.x * x;
            float yy = rotation.y * y;
            float zz = rotation.z * z;
            float xy = rotation.x * y;
            float xz = rotation.x * z;
            float yz = rotation.y * z;
            float wx = rotation.w * x;
            float wy = rotation.w * y;
            float wz = rotation.w * z;
            return new Vector3(
                (1f - (yy + zz)) * value.x +
                (xy - wz) * value.y + (xz + wy) * value.z,
                (xy + wz) * value.x +
                (1f - (xx + zz)) * value.y + (yz - wx) * value.z,
                (xz - wy) * value.x + (yz + wx) * value.y +
                (1f - (xx + yy)) * value.z);
        }

        static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector3 orthogonalUp = Vector3.Cross(forward, right);
            float m00 = right.x;
            float m01 = orthogonalUp.x;
            float m02 = forward.x;
            float m10 = right.y;
            float m11 = orthogonalUp.y;
            float m12 = forward.y;
            float m20 = right.z;
            float m21 = orthogonalUp.z;
            float m22 = forward.z;
            float trace = m00 + m11 + m22;
            Quaternion result;
            if (trace > 0f)
            {
                float scale = MathF.Sqrt(trace + 1f) * 2f;
                result = new Quaternion(
                    (m21 - m12) / scale,
                    (m02 - m20) / scale,
                    (m10 - m01) / scale,
                    scale * 0.25f);
            }
            else if (m00 > m11 && m00 > m22)
            {
                float scale = MathF.Sqrt(1f + m00 - m11 - m22) * 2f;
                result = new Quaternion(
                    scale * 0.25f,
                    (m01 + m10) / scale,
                    (m02 + m20) / scale,
                    (m21 - m12) / scale);
            }
            else if (m11 > m22)
            {
                float scale = MathF.Sqrt(1f + m11 - m00 - m22) * 2f;
                result = new Quaternion(
                    (m01 + m10) / scale,
                    scale * 0.25f,
                    (m12 + m21) / scale,
                    (m02 - m20) / scale);
            }
            else
            {
                float scale = MathF.Sqrt(1f + m22 - m00 - m11) * 2f;
                result = new Quaternion(
                    (m02 + m20) / scale,
                    (m12 + m21) / scale,
                    scale * 0.25f,
                    (m10 - m01) / scale);
            }
            return NormalizeRotation(result);
        }

        static Quaternion SlerpRotation(
            Quaternion from,
            Quaternion to,
            float value)
        {
            float dot = from.x * to.x + from.y * to.y +
                        from.z * to.z + from.w * to.w;
            if (dot < 0f)
            {
                dot = -dot;
                to = new Quaternion(-to.x, -to.y, -to.z, -to.w);
            }
            if (dot > 0.9995f)
            {
                return NormalizeRotation(new Quaternion(
                    from.x + (to.x - from.x) * value,
                    from.y + (to.y - from.y) * value,
                    from.z + (to.z - from.z) * value,
                    from.w + (to.w - from.w) * value));
            }
            float theta = MathF.Acos(Math.Clamp(dot, -1f, 1f));
            float sinTheta = MathF.Sin(theta);
            float fromWeight = MathF.Sin((1f - value) * theta) / sinTheta;
            float toWeight = MathF.Sin(value * theta) / sinTheta;
            return NormalizeRotation(new Quaternion(
                from.x * fromWeight + to.x * toWeight,
                from.y * fromWeight + to.y * toWeight,
                from.z * fromWeight + to.z * toWeight,
                from.w * fromWeight + to.w * toWeight));
        }

        static float RotationAngleDegrees(Quaternion first, Quaternion second)
        {
            float dot = MathF.Abs(
                first.x * second.x + first.y * second.y +
                first.z * second.z + first.w * second.w);
            return MathF.Acos(Math.Clamp(dot, -1f, 1f)) *
                   2f * 57.2957795f;
        }

        static float DirectionAngleDegrees(Vector3 first, Vector3 second)
        {
            float denominator = MathF.Sqrt(
                first.sqrMagnitude * second.sqrMagnitude);
            if (denominator <= RuntimeGeometryEpsilon)
                return 0f;
            float dot = Vector3.Dot(first, second) / denominator;
            return MathF.Acos(Math.Clamp(dot, -1f, 1f)) * 57.2957795f;
        }

        static Vector3 RotateDirectionTowards(
            Vector3 previous,
            Vector3 requested,
            float maximumDegrees)
        {
            Vector3 from = previous.normalized;
            Vector3 to = requested.normalized;
            float angleDegrees = DirectionAngleDegrees(from, to);
            if (angleDegrees <= maximumDegrees)
                return to;
            float angleRadians = angleDegrees * 0.0174532924f;
            float maximumRadians = maximumDegrees * 0.0174532924f;
            float ratio = maximumRadians / angleRadians;
            float sinAngle = MathF.Sin(angleRadians);
            if (MathF.Abs(sinAngle) <= RuntimeGeometryEpsilon)
                return Vector3.Lerp(from, to, ratio).normalized;
            float previousWeight =
                MathF.Sin((1f - ratio) * angleRadians) / sinAngle;
            float requestedWeight =
                MathF.Sin(ratio * angleRadians) / sinAngle;
            return (from * previousWeight + to * requestedWeight).normalized;
        }

        static bool FiniteVector(Vector2 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y);

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
                    profileId = capture.FootRows[0].ProfileId,
                    profileRevision = capture.FootRows[0].ProfileRevision,
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
                    groundPenetrationToleranceMeters =
                        ExpectedGroundPenetrationToleranceMeters,
                    penetrationGeometryEpsilonMeters =
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters,
                    contactSupportGapThresholdMeters = ContactSupportGapThresholdMeters,
                    contactSupportTouchToleranceMeters = ContactSupportTouchToleranceMeters,
                    contactSupportGapPersistentSeconds = ContactSupportGapPersistentSeconds
                },
                coverage = new CoverageFact
                {
                    contactSupportRequestedFrameCount = capture.FootRows.Count(
                        value => value.ContactSupportGap.requested),
                    contactSupportGapAvailableFrameCount = capture.FootRows.Count(
                        ContactSupportGapAvailable),
                    contactSupportGapNotApplicableFrameCount = capture.FootRows.Count(
                        value => value.ContactSupportGap.observed &&
                            !value.ContactSupportGap.applicable),
                    contactSupportGapUnavailableFrameCount = capture.FootRows.Count(
                        value => value.ContactSupportGap.applicable &&
                            !ContactSupportGapAvailable(value)),
                    contactSupportGapIntervalCount = events.Count(
                        value => value.kind == "ContactSupportGapInterval"),
                    landingEventCount = events.Count(value => value.kind == "Landing"),
                    landingStateBoundaryCount = events.Count(
                        value => value.kind == "LandingStateBoundary"),
                    landingStateSpanCount = events.Count(
                        value => value.kind == "LandingStateSpan"),
                    lockedFullAnchorEventCount = events.Count(
                        value => value.kind == "LockedFullAnchor"),
                    lockedSlidingEventCount = events.Count(
                        value => value.kind == "LockedSliding"),
                    lockedEventCount = events.Count(
                        value => value.kind == "LockedFullAnchor" ||
                                 value.kind == "LockedSliding"),
                    releaseEventCount = events.Count(value => value.kind == "Release"),
                    pathRevisionOutputJumpCount = events.Count(
                        value => value.kind == "PathRevisionOutputJump"),
                    pathContinuityEventCount = events.Count(
                        value => value.kind == "PathContinuity"),
                    stableSwingOutputJumpCount = events.Count(
                        value => value.kind == "StableSwingOutputJump"),
                    contactStateOutputJumpCount = events.Count(
                        value => value.kind ==
                                 "ContactStateOutputJump"),
                    swingToLandingFloorHandoffCount = events.Count(
                        value => value.kind ==
                                 "SwingToLandingFloorHandoff"),
                    plantInterpolationOutputJumpCount = events.Count(
                        value => value.kind ==
                                 "PlantInterpolationOutputJump"),
                    contactAcquisitionContinuityCount = events.Count(
                        value => value.kind ==
                                 "ContactAcquisitionContinuity"),
                    lockWeightCompletionEventCount = events.Count(
                        value => value.kind ==
                                 "LockWeightCompletionEvent"),
                    approachProgressOwnershipCount = events.Count(
                        value => value.kind ==
                                 "ApproachProgressOwnership"),
                    actionHardOwnershipCount = events.Count(
                        value => value.kind == "ActionHardOwnership"),
                    contactTransitionContextCount = events.Count(
                        value => value.kind == "ContactTransitionContext"),
                    formalGoalWeightPolicyCount = events.Count(
                        value => value.kind == "FormalGoalWeightPolicy"),
                    contactReentryOutputGeometryCount = events.Count(
                        value => value.kind == "ContactReentryOutputGeometry"),
                    postTransitionUnevaluatedCount = capture.FootRows.Count(
                        value => !value.PostTransitionEvaluated),
                    reentryOutputFactsUnavailableCount = capture.FootRows.Count(
                        value => value.SameEventContactReentryRefreshed &&
                            (!value.PreviousResponseOutputAvailable ||
                             !value.PlantInterpolationEvaluated ||
                             !value.CorrectionResponseEvaluated ||
                             value.Resolved.Outcome != "Ready")),
                    stableSwingCorrectionResponseCadenceCount = events.Count(
                        value => value.kind ==
                                 "StableSwingCorrectionResponseCadence"),
                    actualFootEnvelopeCounterfactualCount = events.Count(
                        value => value.kind ==
                                 "ActualFootEnvelopeCounterfactual"),
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
                    futureLandingQueryCount = events.Count(
                        value => value.kind == "LandingObservation" &&
                                 value.evidence.TryGetValue(
                                     "futureLandingPurpose",
                                     out bool matched) && matched),
                    currentContactVerificationQueryCount = events.Count(
                        value => value.kind == "LandingObservation" &&
                                 value.evidence.TryGetValue(
                                     "currentContactVerificationPurpose",
                                     out bool matched) && matched),
                    currentSupportQueryCount = events.Count(
                        value => value.kind == "CurrentSupportQuery"),
                    predictionMotionCount = capture.Left.Count,
                    predictionMotionUnavailableCount = capture.Left.Count(
                        value => !value.PredictionMotionAvailable),
                    predictionMotionResetCount = capture.Left.Count(
                        value => value.PredictionMotionResetReason != "None"),
                    predictionCurrentResponseCount = capture.Left.Count(
                        value => value.PredictionCurrentResponseApplied),
                    predictionContinuationResponseCount = capture.Left.Count(
                        value => value.PredictionContinuationResponseApplied),
                    predictionMaximumSpeedClampCount = capture.Left.Count(
                        value => value.PredictionCurrentMaximumSpeedClamped ||
                                 value.PredictionContinuationMaximumSpeedClamped),
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
                pelvisFrames = capture.Left
                    .Select((frame, index) => BuildPelvisFact(
                        frame, index > 0 ? capture.Left[index - 1] : null))
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

        static object BuildContactAnchorFact(ContactAnchorFrame anchor) => new
        {
            available = anchor.Available,
            eventIdentity = anchor.Event.ToString(CultureInfo.InvariantCulture),
            acquiredFrameSequence = anchor.AcquiredFrame.ToString(
                CultureInfo.InvariantCulture),
            acquiredCompletionIdentity = anchor.AcquiredCompletion.ToString(
                CultureInfo.InvariantCulture),
            worldRevision = anchor.WorldRevision.ToString(CultureInfo.InvariantCulture),
            surfaceIdentity = anchor.Surface,
            point = CharacterFootVectorFact.From(anchor.Point),
            normal = CharacterFootVectorFact.From(anchor.Normal)
        };

        static object BuildPostTransitionFact(FootFrame frame) =>
            !frame.PostTransitionEvaluated ? null : new
            {
                reason = frame.PostTransitionReason,
                source = frame.PostTransitionSource,
                target = frame.PostTransitionTarget,
                anchorCommand = frame.PostTransitionAnchorCommand,
                suppressOutput = frame.PostTransitionSuppressOutput,
                resetInterpolation = frame.PostTransitionResetInterpolation
            };

        static object SupportTargetFact(CharacterFootSupportTargetSample target) => new
        {
            available = target.Available,
            frame = target.Frame.ToString(CultureInfo.InvariantCulture),
            completionIdentity = target.Completion.ToString(
                CultureInfo.InvariantCulture),
            side = target.Side,
            position = CharacterFootVectorFact.From(target.Position),
            normal = CharacterFootVectorFact.From(target.Normal),
            surfaceIdentity = target.Surface,
            worldRevision = target.WorldRevision.ToString(
                CultureInfo.InvariantCulture),
            kind = target.Kind,
            positionSource = target.PositionSource,
            positionFrame = target.PositionFrame.ToString(
                CultureInfo.InvariantCulture),
            positionCompletion = target.PositionCompletion.ToString(
                CultureInfo.InvariantCulture),
            positionEvent = target.PositionEvent.ToString(
                CultureInfo.InvariantCulture),
            positionPath = target.PositionPath.ToString(
                CultureInfo.InvariantCulture),
            normalSource = target.NormalSource,
            normalFrame = target.NormalFrame.ToString(
                CultureInfo.InvariantCulture),
            normalCompletion = target.NormalCompletion.ToString(
                CultureInfo.InvariantCulture),
            normalEvent = target.NormalEvent.ToString(
                CultureInfo.InvariantCulture)
        };

        static object CurrentSupportProbeFact(
            CharacterFootCurrentSupportProbeSample probe) => new
        {
            purpose = probe.Purpose,
            kind = probe.Kind,
            state = probe.State,
            rejectReason = probe.RejectReason,
            probePosition = CharacterFootVectorFact.From(probe.ProbePosition),
            componentUp = CharacterFootVectorFact.From(probe.ComponentUp),
            origin = CharacterFootVectorFact.From(probe.Origin),
            direction = CharacterFootVectorFact.From(probe.Direction),
            maximumDistance = probe.MaximumDistance,
            radius = probe.Radius,
            layerMask = probe.LayerMask,
            minimumGroundNormalDot = probe.MinimumGroundNormalDot,
            hitCapacity = probe.HitCapacity,
            candidateCount = probe.CandidateCount,
            surfaceIdentity = probe.Surface,
            point = CharacterFootVectorFact.From(probe.Point),
            normal = CharacterFootVectorFact.From(probe.Normal),
            distance = probe.Distance,
            worldRevision = probe.WorldRevision.ToString(
                CultureInfo.InvariantCulture),
            sphereCastExecuted = probe.SphereCastExecuted,
            accepted = probe.Accepted
        };

        static object RotationFact(Quaternion value) => new
        {
            x = value.x,
            y = value.y,
            z = value.z,
            w = value.w
        };

        static CsvCapture ReadCapture(
            string samplesPath,
            string geometryPath)
        {
            var sourceIndices = new List<CharacterFootDiagnosticSourceIndex>(2);
            using var reader = new CharacterFootDiagnosticSourceReader(samplesPath, "samples");
            string header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                throw new InvalidDataException("Foot Motion samples CSV is empty.");
            string[] names = ParseCsvLine(header);
            reader.SetColumns(names);
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < names.Length; i++)
                if (!indices.TryAdd(names[i], i))
                    throw new InvalidDataException($"Foot Motion samples CSV has duplicate column '{names[i]}'.");
            RequireColumns(indices);
            var bindings = new CharacterFootSampleReadBindings(indices);
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
                FootFrame frame = ParseFrame(indices, cells, bindings);
                reader.Include(frame.Frame, frame.Side);
                var key = (frame.Frame, frame.Side);
                if (!unique.TryAdd(key, frame))
                {
                    throw new InvalidDataException(
                        $"Foot Motion samples CSV has duplicate Foot row " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
            }
            sourceIndices.Add(reader.Complete());
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
            for (int i = 0; i < left.Count; i++)
            {
                RequirePredictionMotionPair(left[i], right[i]);
                if (left[i].CompletionIdentity != right[i].CompletionIdentity ||
                    left[i].StrideState != right[i].StrideState ||
                    !left[i].PelvisHeightTarget.SameAs(right[i].PelvisHeightTarget) ||
                    !left[i].Pelvis.SameAs(right[i].Pelvis) ||
                    left[i].StrideSlope != right[i].StrideSlope ||
                    !left[i].StridePelvisDelta.Equals(right[i].StridePelvisDelta) ||
                    !left[i].PhysicalPelvis.Equals(right[i].PhysicalPelvis) ||
                    !left[i].FinalPelvisGoal.Equals(right[i].FinalPelvisGoal) ||
                    left[i].PelvisWeight != right[i].PelvisWeight ||
                    left[i].FinalPhysicalWriteAvailable != right[i].FinalPhysicalWriteAvailable ||
                    left[i].FinalPhysicalWriteCompletionIdentity != right[i].FinalPhysicalWriteCompletionIdentity)
                {
                    throw new InvalidDataException(
                        $"Foot Motion shared Pelvis height target differs between Foot rows Frame={left[i].Frame}.");
                }
            }
            RequireResponseDomainHistory(left);
            RequireResponseDomainHistory(right);
            RequirePelvisHistory(left);
            FootFrame first = footRows[0];
            int geometryRowCount = ReadGeometry(
                geometryPath,
                first.SampleIdentity,
                unique,
                sourceIndices);
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
                right,
                sourceIndices);
        }

        static void RequirePredictionMotionPair(
            FootFrame left,
            FootFrame right)
        {
            if (left.Frame != right.Frame ||
                left.PredictionMotionAvailable !=
                right.PredictionMotionAvailable ||
                !string.Equals(
                    left.PredictionMotionRejectReason,
                    right.PredictionMotionRejectReason,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    left.PredictionMotionResetReason,
                    right.PredictionMotionResetReason,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    left.PredictionMotionSourceIdentity,
                    right.PredictionMotionSourceIdentity,
                    StringComparison.Ordinal) ||
                left.PredictionRawCurrentVelocity !=
                right.PredictionRawCurrentVelocity ||
                left.PredictionRawContinuationVelocity !=
                right.PredictionRawContinuationVelocity ||
                left.PredictionPreviousStableCurrentVelocity !=
                right.PredictionPreviousStableCurrentVelocity ||
                left.PredictionPreviousStableContinuationVelocity !=
                right.PredictionPreviousStableContinuationVelocity ||
                left.PredictionStableCurrentVelocity !=
                right.PredictionStableCurrentVelocity ||
                left.PredictionStableContinuationVelocity !=
                right.PredictionStableContinuationVelocity ||
                left.PredictionCurrentVelocityDelta !=
                right.PredictionCurrentVelocityDelta ||
                left.PredictionContinuationVelocityDelta !=
                right.PredictionContinuationVelocityDelta ||
                left.PredictionVelocityResponseAlpha !=
                right.PredictionVelocityResponseAlpha ||
                left.PredictionVelocityDeltaThreshold !=
                right.PredictionVelocityDeltaThreshold ||
                left.PredictionVelocitySmoothSpeed !=
                right.PredictionVelocitySmoothSpeed ||
                left.PredictionMaximumSpeed != right.PredictionMaximumSpeed ||
                left.PredictionCurrentResponseApplied !=
                right.PredictionCurrentResponseApplied ||
                left.PredictionContinuationResponseApplied !=
                right.PredictionContinuationResponseApplied ||
                left.PredictionCurrentMaximumSpeedClamped !=
                right.PredictionCurrentMaximumSpeedClamped ||
                left.PredictionContinuationMaximumSpeedClamped !=
                right.PredictionContinuationMaximumSpeedClamped ||
                left.PredictionMotionRevision !=
                right.PredictionMotionRevision)
            {
                throw new InvalidDataException(
                    $"Foot Prediction Motion Frame {left.Frame} differs between feet.");
            }
        }

        static int ReadGeometry(
            string geometryPath,
            string sampleIdentity,
            Dictionary<(int frame, string side), FootFrame> footRows,
            List<CharacterFootDiagnosticSourceIndex> sourceIndices)
        {
            using var reader = new CharacterFootDiagnosticSourceReader(geometryPath, "geometry");
            string header = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(header))
                throw new InvalidDataException(
                    "Foot Motion ground path geometry CSV is empty.");
            string[] names = ParseCsvLine(header);
            reader.SetColumns(names);
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
            string[] surfaceColumns =
            {
                "GroundSurfaceSegmentIndex", "GroundSurfaceIdentity", "GroundSurfaceFaceIndex",
                "GroundSurfaceStartDistance", "GroundSurfaceStartHeight",
                "GroundSurfaceEndDistance", "GroundSurfaceEndHeight"
            };
            bool hasSurfaceGeometry = indices.ContainsKey(surfaceColumns[0]);
            foreach (string column in surfaceColumns)
            {
                if (indices.ContainsKey(column) != hasSurfaceGeometry)
                    throw new InvalidDataException("Foot Motion surface geometry columns are incomplete.");
            }
            foreach (FootFrame foot in footRows.Values)
            {
                if (foot.GroundSurfaceFactsAvailable != hasSurfaceGeometry)
                    throw new InvalidDataException("Foot Motion surface geometry schema does not match samples.");
            }
            var surfaceSegments = new HashSet<(int frame, string side, int index)>();
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
                reader.Include(frame, side);
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
                int surfaceIndex = hasSurfaceGeometry
                    ? ParseInt(Cell("GroundSurfaceSegmentIndex"), "GroundSurfaceSegmentIndex")
                    : -1;
                if (contactIndex < 0 && envelopeIndex < 0 && surfaceIndex < 0)
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has no geometry payload.");
                }
                if (surfaceIndex >= 0)
                {
                    int surfaceIdentity = ParseInt(Cell("GroundSurfaceIdentity"), "GroundSurfaceIdentity");
                    int faceIndex = ParseInt(Cell("GroundSurfaceFaceIndex"), "GroundSurfaceFaceIndex");
                    float startDistance = ParseFloat(Cell("GroundSurfaceStartDistance"), "GroundSurfaceStartDistance");
                    float endDistance = ParseFloat(Cell("GroundSurfaceEndDistance"), "GroundSurfaceEndDistance");
                    float startHeight = ParseFloat(Cell("GroundSurfaceStartHeight"), "GroundSurfaceStartHeight");
                    float endHeight = ParseFloat(Cell("GroundSurfaceEndHeight"), "GroundSurfaceEndHeight");
                    if (surfaceIndex >= foot.GroundSurfaceSegmentCount ||
                        surfaceIdentity == 0 || faceIndex < 0 ||
                        !float.IsFinite(startDistance) || !float.IsFinite(endDistance) ||
                        !float.IsFinite(startHeight) || !float.IsFinite(endHeight) ||
                        startDistance < 0f || endDistance < startDistance ||
                        !surfaceSegments.Add((frame, side, surfaceIndex)))
                    {
                        throw new InvalidDataException(
                            $"Foot Motion surface geometry is invalid Frame={frame} Side={side} Index={surfaceIndex}.");
                    }
                    foot.GroundSurfaceObservedCount++;
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
                if (foot.GroundSurfaceFactsAvailable &&
                    (foot.GroundSurfaceSegmentCount < 0 ||
                     foot.GroundSurfaceSegmentCount != foot.GroundSurfaceObservedCount ||
                     foot.GroundSurfaceSegmentCount > 0 && foot.GroundSurfaceWorldRevision == 0 ||
                     foot.GroundPathState == "Accepted" &&
                     (foot.GroundSurfaceState != CharacterFootGroundSurfaceState.Ready ||
                      foot.GroundSurfaceSegmentCount == 0)))
                {
                    throw new InvalidDataException(
                        $"Foot Motion surface geometry facts mismatch Frame={foot.Frame} Side={foot.Side}.");
                }
                RequireActualFootEnvelopeFacts(foot);
            }
            sourceIndices.Add(reader.Complete());
            return rowCount;
        }

        internal static string[] ParseCsvLine(string line)
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
            string[] cells,
            CharacterFootSampleReadBindings bindings)
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

            T EnumField<T>(string name) where T : struct, Enum
            {
                string value = Cell(name);
                RequireEnum<T>(value, name);
                return Enum.Parse<T>(value);
            }
            T FlagsField<T>(string name, char separator = ',') where T : struct, Enum
            {
                string value = Cell(name).Replace(separator, ',');
                RequireFlags<T>(value, name);
                return Enum.Parse<T>(value);
            }
            PelvisLegFrame PelvisLeg(string prefix) => new PelvisLegFrame
            {
                Role = FlagsField<CharacterFootPelvisLegReachRole>(prefix + "Role"),
                Status = EnumField<CharacterFootPelvisLegReachStatus>(prefix + "Status"),
                EventIdentity = Ulong(prefix + "EventIdentity"),
                Hip = Vector(prefix + "Hip"),
                TargetAnkle = Vector(prefix + "TargetAnkle"),
                LegLength = Float(prefix + "LegLength"),
                MinimumCompressionReserve = Float(prefix + "MinimumCompressionReserve"),
                UsableLegLength = Float(prefix + "UsableLegLength"),
                MinimumAlongUp = Float(prefix + "MinimumAlongUp"),
                MaximumAlongUp = Float(prefix + "MaximumAlongUp"),
                Requested = Int(prefix + "Requested") != 0,
                Available = Int(prefix + "Available") != 0,
            };

            string side = Cell("Side");
            bool hasSurfaceFacts = indices.ContainsKey("GroundSurfaceState");
            if (indices.ContainsKey("GroundSurfaceWorldRevision") != hasSurfaceFacts ||
                indices.ContainsKey("GroundSurfaceSegmentCount") != hasSurfaceFacts)
                throw new InvalidDataException("Foot Motion surface fact columns are incomplete.");
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
                ProfileId = Cell("FootProfileId"),
                ProfileRevision = Cell("FootProfileRevision"),
                Side = side,
                ApproachPlantTargetPrepared =
                    Int("ApproachPlantTargetPrepared") != 0,
                PreparedTargetAvailable = Int("PlantTargetAvailable") != 0,
                PreparedTargetEventIdentity =
                    Ulong("PlantTargetEventIdentity"),
                PreparedTargetSurfaceIdentity =
                    Int("PlantTargetSurfaceIdentity"),
                PreparedTargetPoint = Vector("PlantTargetPoint"),
                PreparedTargetNormal = Vector("PlantTargetNormal"),
                PreparedTargetTrajectoryGeneration =
                    Ulong("PlantTargetTrajectoryGeneration"),
                PreparedTargetFutureBodySource =
                    Cell("PlantTargetFutureBodyTranslationSourceIdentity"),
                DeltaSeconds = Float("PresentationDeltaSeconds"),
                BodyResetSequence = Ulong("BodyResetSequence"),
                CurrentBodyTick = Ulong("CurrentBodyTick"),
                BodyTargetVelocity = Vector("TargetBodyVelocity"),
                TimelineCurrentVelocity = new Vector2(
                    Float("TimelineCurrentVelocityX"),
                    Float("TimelineCurrentVelocityZ")),
                TimelineContinuationVelocity = new Vector2(
                    Float("TimelineContinuationVelocityX"),
                    Float("TimelineContinuationVelocityZ")),
                PredictionMotionAvailable =
                    Int("PredictionMotionAvailable") != 0,
                PredictionMotionRejectReason =
                    Cell("PredictionMotionRejectReason"),
                PredictionMotionResetReason =
                    Cell("PredictionMotionResetReason"),
                PredictionMotionSourceIdentity =
                    Cell("PredictionMotionSourceIdentity"),
                PredictionRawCurrentVelocity = new Vector2(
                    Float("PredictionRawCurrentVelocityX"),
                    Float("PredictionRawCurrentVelocityZ")),
                PredictionRawContinuationVelocity = new Vector2(
                    Float("PredictionRawContinuationVelocityX"),
                    Float("PredictionRawContinuationVelocityZ")),
                PredictionPreviousStableCurrentVelocity = new Vector2(
                    Float("PredictionPreviousStableCurrentVelocityX"),
                    Float("PredictionPreviousStableCurrentVelocityZ")),
                PredictionPreviousStableContinuationVelocity = new Vector2(
                    Float("PredictionPreviousStableContinuationVelocityX"),
                    Float("PredictionPreviousStableContinuationVelocityZ")),
                PredictionStableCurrentVelocity = new Vector2(
                    Float("PredictionStableCurrentVelocityX"),
                    Float("PredictionStableCurrentVelocityZ")),
                PredictionStableContinuationVelocity = new Vector2(
                    Float("PredictionStableContinuationVelocityX"),
                    Float("PredictionStableContinuationVelocityZ")),
                PredictionCurrentVelocityDelta = new Vector2(
                    Float("PredictionCurrentVelocityDeltaX"),
                    Float("PredictionCurrentVelocityDeltaZ")),
                PredictionContinuationVelocityDelta = new Vector2(
                    Float("PredictionContinuationVelocityDeltaX"),
                    Float("PredictionContinuationVelocityDeltaZ")),
                PredictionVelocityResponseAlpha =
                    Float("PredictionVelocityResponseAlpha"),
                PredictionVelocityDeltaThreshold =
                    Float("PredictionVelocityDeltaThreshold"),
                PredictionVelocitySmoothSpeed =
                    Float("PredictionVelocitySmoothSpeed"),
                PredictionMaximumSpeed = Float("PredictionMaximumSpeed"),
                PredictionCurrentResponseApplied =
                    Int("PredictionCurrentResponseApplied") != 0,
                PredictionContinuationResponseApplied =
                    Int("PredictionContinuationResponseApplied") != 0,
                PredictionCurrentMaximumSpeedClamped =
                    Int("PredictionCurrentMaximumSpeedClamped") != 0,
                PredictionContinuationMaximumSpeedClamped =
                    Int("PredictionContinuationMaximumSpeedClamped") != 0,
                PredictionMotionRevision = Ulong("PredictionMotionRevision"),
                Grounded = Int("Grounded") != 0,
                ActionInstanceIdentity = Ulong(
                    side == "Left"
                        ? "LeftActionInstanceIdentity"
                        : "RightActionInstanceIdentity"),
                ActionFootWeight = Float(
                    side == "Left"
                        ? "LeftActionFootWeight"
                        : "RightActionFootWeight"),
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
                OutputEvents = bindings.OutputEvents.Read(cells),
                InputEvents = bindings.InputEvents.Read(cells),
                SelectedPhase = bindings.SelectedPhase.Read(cells),
                CurrentStep = bindings.CurrentStep.Read(cells),
                IncomingStep = bindings.IncomingStep.Read(cells),
                FormalObservationAvailable =
                    Int("InputFormalStepObservationAvailable") != 0,
                SourceIdentity = Cell("InputFormalStepSourceIdentity"),
                SourceCycle = Int("InputFormalStepSourceCycle"),
                ContributionContinuityIdentity = Ulong("InputFormalStepContributionContinuityIdentity"),
                FormalObservationCompletionIdentity =
                    Ulong("InputFormalStepCompletionIdentity"),
                FormalNormalizedTime = Float("InputFormalStepSourceNormalizedTime"),
                FormalStepTime = Float("InputFormalStepTimeSeconds"),
                FormalContact = Float("FormalContact"),
                FormalRequestContact = Float("InputFormalContact"),
                FormalLockMode = Cell("InputFormalLockMode"),
                FormalLockWeight = Float("InputFormalLockWeight"),
                FormalSupport = Float("InputFormalSupport"),
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
                LandingObservationQueryPurpose =
                    Cell("LandingObservationQueryPurpose"),
                LandingObservationRefreshMode =
                    Cell("LandingObservationRefreshMode"),
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
                FutureLandingQueryPurpose = Cell("QueryPurpose"),
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
                GroundSurfaceFactsAvailable = hasSurfaceFacts,
                GroundSurfaceState = hasSurfaceFacts
                    ? EnumField<CharacterFootGroundSurfaceState>("GroundSurfaceState")
                    : default,
                GroundSurfaceWorldRevision = hasSurfaceFacts ? Ulong("GroundSurfaceWorldRevision") : 0,
                GroundSurfaceSegmentCount = hasSurfaceFacts ? Int("GroundSurfaceSegmentCount") : 0,
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
                GroundPathComponentUp = Vector("GroundPathComponentUp"),
                ComponentUp = Vector("FootMotionTargetHeightComponentUp"),
                GroundPathRadius = Float("GroundPathRadius"),
                FootMotionEventIdentity = Ulong("FootMotionLandingEventIdentity"),
                FootMotionGroundPathInputIdentity =
                    Ulong("FootMotionGroundPathInputIdentity"),
                FootMotionState = Cell("FootMotionState"),
                ConstraintState = Cell("FootMotionConstraintState"),
                LockResponse = Cell("FootMotionLockResponse"),
                OriginalSole = Vector("FootMotionOriginalSole"),
                OriginalAnkle = Vector("FootMotionOriginalAnkle"),
                SourceAnkleRotation =
                    Rotation("FootMotionSourceAnkleRotation"),
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
                SwingRawFormalTargetHeight =
                    Float("FootMotionRawFormalTargetHeight"),
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
                MotionPositionWeight = Float("FootMotionPositionWeight"),
                MotionRotationWeight = Float("FootMotionRotationWeight"),
                FinalGoalPositionWeight = Float("FinalGoalPositionWeight"),
                FinalGoalRotationWeight = Float("FinalGoalRotationWeight"),
                Anchor = Vector("FootMotionSupportContactAnchor"),
                ContactPlaneAvailable = Int("FootMotionContactPlaneAvailable") != 0,
                ContactOwnership = Float("FootMotionContactOwnership"),
                LandingReachEvaluated =
                    Int("FootMotionLandingReachEvaluated") != 0,
                LandingReachAvailable =
                    Int("FootMotionLandingReachAvailable") != 0,
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
                SwingTargetHeightAdoptionMode =
                    Cell("FootMotionSwingTargetHeightAdoptionMode"),
                SwingRawTargetHeightAlongUp =
                    Float("FootMotionSwingRawTargetHeightAlongUp"),
                SwingFilteredTargetHeightBefore =
                    Float("FootMotionSwingFilteredTargetHeightBefore"),
                SwingTargetHeightDelta =
                    Float("FootMotionSwingTargetHeightDelta"),
                SwingTargetHeightAppliedDelta =
                    Float("FootMotionSwingTargetHeightAppliedDelta"),
                SwingTargetHeightUpdateHeld =
                    Int("FootMotionSwingTargetHeightUpdateHeld") != 0,
                SwingTargetHeightForceRefreshed =
                    Int("FootMotionSwingTargetHeightForceRefreshed") != 0,
                SwingTargetHeightRateLimited =
                    Int("FootMotionSwingTargetHeightRateLimited") != 0,
                SwingTargetHeightClamped =
                    Int("FootMotionSwingTargetHeightClamped") != 0,
                SwingTargetHeightForceRefreshDistance =
                    Float("FootMotionSwingTargetHeightForceRefreshDistance"),
                SwingTargetMaximumVerticalSpeed =
                    Float("FootMotionSwingTargetMaximumVerticalSpeed"),
                SwingFilteredTargetHeightAlongUp =
                    Float("FootMotionSwingFilteredTargetHeightAlongUp"),
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
                LifecycleTransitionEvaluated =
                    Int("FootMotionLifecycleTransitionEvaluated") != 0,
                PreviousLockRequestAvailable =
                    Int("FootMotionPreviousLockRequestAvailable") != 0,
                PreviousLockRequested =
                    Int("FootMotionPreviousLockRequested") != 0,
                PreviousLockRequestEventIdentity =
                    Ulong("FootMotionPreviousLockRequestEventIdentity"),
                PreviousLockRequestMode =
                    Cell("FootMotionPreviousLockRequestMode"),
                PreviousLockRequestWeight =
                    Float("FootMotionPreviousLockRequestWeight"),
                PreviousContactEdgeSeconds =
                    Float("FootMotionPreviousContactEdgeSeconds"),
                PreviousLatestContactEventIdentity =
                    Ulong("FootMotionPreviousLatestContactEventIdentity"),
                PreviousLatestReleasedContactEventIdentity =
                    Ulong(
                        "FootMotionPreviousLatestReleasedContactEventIdentity"),
                PreviousCompletedLockWeightEventIdentity =
                    Ulong(
                        "FootMotionPreviousCompletedLockWeightEventIdentity"),
                PreviousContactAnchorAvailable =
                    Int("FootMotionPreviousContactAnchorAvailable") != 0,
                PreviousContactAnchorEventIdentity =
                    Ulong("FootMotionPreviousContactAnchorEventIdentity"),
                PreviousContactAnchorAcquiredFrameSequence =
                    Ulong("FootMotionPreviousContactAnchorAcquiredFrameSequence"),
                PreviousContactAnchorAcquiredCompletionIdentity =
                    Ulong("FootMotionPreviousContactAnchorAcquiredCompletionIdentity"),
                PreviousContactAnchorWorldRevision =
                    Ulong("FootMotionPreviousContactAnchorWorldRevision"),
                PreviousContactAnchorSurfaceIdentity =
                    Int("FootMotionPreviousContactAnchorSurfaceIdentity"),
                PreviousContactAnchorPoint =
                    Vector("FootMotionPreviousContactAnchorPoint"),
                PreviousContactAnchorNormal =
                    Vector("FootMotionPreviousContactAnchorNormal"),
                CurrentLockRequested =
                    Int("FootMotionCurrentLockRequested") != 0,
                CurrentLockRequestEventIdentity =
                    Ulong("FootMotionCurrentLockRequestEventIdentity"),
                CurrentLockRequestMode =
                    Cell("FootMotionCurrentLockRequestMode"),
                CurrentLockRequestWeight =
                    Float("FootMotionCurrentLockRequestWeight"),
                CurrentLockRequestAvailability =
                    Cell("FootMotionCurrentLockRequestAvailability"),
                ContactEdge = Cell("FootMotionContactEdge"),
                CurrentContactEdgeSeconds =
                    Float("FootMotionCurrentContactEdgeSeconds"),
                CurrentLatestContactEventIdentity =
                    Ulong("FootMotionCurrentLatestContactEventIdentity"),
                CurrentLatestReleasedContactEventIdentity =
                    Ulong(
                        "FootMotionCurrentLatestReleasedContactEventIdentity"),
                CurrentCompletedLockWeightEventIdentity =
                    Ulong(
                        "FootMotionCurrentCompletedLockWeightEventIdentity"),
                CurrentContactAnchorAvailable =
                    Int("FootMotionCurrentContactAnchorAvailable") != 0,
                CurrentContactAnchorEventIdentity =
                    Ulong("FootMotionCurrentContactAnchorEventIdentity"),
                CurrentContactAnchorAcquiredFrameSequence =
                    Ulong("FootMotionCurrentContactAnchorAcquiredFrameSequence"),
                CurrentContactAnchorAcquiredCompletionIdentity =
                    Ulong("FootMotionCurrentContactAnchorAcquiredCompletionIdentity"),
                CurrentContactAnchorWorldRevision =
                    Ulong("FootMotionCurrentContactAnchorWorldRevision"),
                CurrentContactAnchorSurfaceIdentity =
                    Int("FootMotionCurrentContactAnchorSurfaceIdentity"),
                CurrentContactAnchorPoint =
                    Vector("FootMotionCurrentContactAnchorPoint"),
                CurrentContactAnchorNormal =
                    Vector("FootMotionCurrentContactAnchorNormal"),
                SameEventContactReentryRefreshed =
                    Int("FootMotionSameEventContactReentryRefreshed") != 0,
                SameEventContactReentryUnavailable =
                    Int("FootMotionSameEventContactReentryUnavailable") != 0,
                RetainedVerifiedAnchor =
                    Int("FootMotionRetainedVerifiedAnchor") != 0,
                ReentryInterpolationHistoryRetained =
                    Int("FootMotionReentryInterpolationHistoryRetained") != 0,
                FormalFootPlacementWeight =
                    Float("FootMotionFormalFootPlacementWeight"),
                PostTransitionEvaluated =
                    Int("FootMotionPostTransitionEvaluated") != 0,
                HardOwnershipLoss =
                    Int("FootMotionHardOwnershipLoss") != 0,
                HardOwnershipLossReason =
                    Cell("FootMotionHardOwnershipLossReason"),
                PreTransitionSuppressOutput =
                    Int("FootMotionPreTransitionSuppressOutput") != 0,
                PreTransitionResetInterpolation =
                    Int("FootMotionPreTransitionResetInterpolation") != 0,
                PostTransitionSuppressOutput =
                    Int("FootMotionPostTransitionSuppressOutput") != 0,
                PostTransitionResetInterpolation =
                    Int("FootMotionPostTransitionResetInterpolation") != 0,
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
                PlantInterpolationEvaluated =
                    Int("FootMotionPlantInterpolationEvaluated") != 0,
                PlantTargetEventIdentity =
                    Ulong("FootMotionPlantTargetEventIdentity"),
                PlantTargetVerified =
                    Int("FootMotionPlantTargetVerified") != 0,
                PlantTargetKind = Cell("FootMotionPlantTargetKind"),
                PlantLockResponse = Cell("FootMotionPlantLockResponse"),
                PlantLockWeightCompleted =
                    Int("FootMotionPlantLockWeightCompleted") != 0,
                PlantDesiredPoint = Vector("FootMotionPlantDesiredPoint"),
                PlantFilteredPoint = Vector("FootMotionPlantFilteredPoint"),
                SelectedSupportTarget = bindings.SelectedTarget.Read(cells),
                PlantTargetHeightAdoptionMode =
                    Cell("FootMotionPlantTargetHeightAdoptionMode"),
                PlantTargetMaximumVerticalSpeed =
                    Float("FootMotionPlantTargetMaximumVerticalSpeed"),
                PlantTargetHeightBefore =
                    Float("FootMotionPlantTargetHeightBefore"),
                PlantTargetHeightTarget =
                    Float("FootMotionPlantTargetHeightTarget"),
                PlantTargetVerticalDelta =
                    Float("FootMotionPlantTargetVerticalDelta"),
                PlantTargetAppliedVerticalDelta =
                    Float("FootMotionPlantTargetAppliedVerticalDelta"),
                PlantTargetHeightAfter =
                    Float("FootMotionPlantTargetHeightAfter"),
                PlantTargetHeightEventIdentity =
                    Ulong("FootMotionPlantTargetHeightEventIdentity"),
                PlantTargetHeightUpdateReason =
                    Cell("FootMotionPlantTargetHeightUpdateReason"),
                PlantTargetForceRefreshed =
                    Int("FootMotionPlantTargetForceRefreshed") != 0,
                PlantTargetForceRefreshDistance =
                    Float("FootMotionPlantTargetForceRefreshDistance"),
                PlantTargetVerticalClamped =
                    Int("FootMotionPlantTargetVerticalClamped") != 0,
                PlantPreviousSelectedWorldTarget =
                    Vector("FootMotionPlantPreviousSelectedWorldTarget"),
                PlantSelectedWorldTarget =
                    Vector("FootMotionPlantSelectedWorldTarget"),
                PreviousResponseOutputAvailable =
                    Int("FootMotionPreviousResponseOutputAvailable") != 0,
                PreviousResponseOutputPoint =
                    Vector("FootMotionPreviousResponseOutputPoint"),
                DesiredOutputPoint =
                    Vector("FootMotionDesiredOutputPoint"),
                ResponseOutputPoint =
                    Vector("FootMotionResponseOutputPoint"),
                PlantResidualCaptureReason =
                    Cell("FootMotionPlantResidualCaptureReason"),
                PlantWorldResidualBeforeCapture =
                    Vector("FootMotionPlantWorldResidualBeforeCapture"),
                PlantWorldResidualCapturedBeforeDecay =
                    Vector("FootMotionPlantWorldResidualCapturedBeforeDecay"),
                PlantWorldResidualDecayApplied =
                    Int("FootMotionPlantWorldResidualDecayApplied") != 0,
                PlantWorldResidualBaseHalfLifeSeconds =
                    Float("FootMotionPlantWorldResidualBaseHalfLifeSeconds"),
                PlantWorldResidualDeadlineHalfLifeAvailable =
                    Int("FootMotionPlantWorldResidualDeadlineHalfLifeAvailable") != 0,
                PlantWorldResidualDeadlineHalfLifeSeconds =
                    Float("FootMotionPlantWorldResidualDeadlineHalfLifeSeconds"),
                PlantWorldResidualAppliedHalfLifeSeconds =
                    Float("FootMotionPlantWorldResidualAppliedHalfLifeSeconds"),
                PlantWorldResidualAfterDecay =
                    Vector("FootMotionPlantWorldResidualAfterDecay"),
                PlantWorldResidualCompletionTolerance =
                    Float("FootMotionPlantWorldResidualCompletionTolerance"),
                PlantWorldResidualClearedAtCompletionTolerance =
                    Int("FootMotionPlantWorldResidualClearedAtCompletionTolerance") != 0,
                CorrectionResponseEvaluated =
                    Int("FootMotionCorrectionResponseEvaluated") != 0,
                CorrectionResponseDomain = Cell("FootMotionCorrectionResponseDomain"),
                CorrectionResponsePreviousDomain = Cell("FootMotionCorrectionResponsePreviousDomain"),
                CorrectionResponseDomainTransferred = Int("FootMotionCorrectionResponseDomainTransferred") != 0,
                CorrectionResponseInitializedBefore =
                    Int("FootMotionCorrectionResponseInitializedBefore") != 0,
                CorrectionResponseInitializedThisFrame =
                    Int("FootMotionCorrectionResponseInitializedThisFrame") != 0,
                CorrectionResponseInitializationReason =
                    Cell("FootMotionCorrectionResponseInitializationReason"),
                CorrectionResponseDesired =
                    Float("FootMotionCorrectionResponseDesired"),
                CorrectionResponseRequestedDirection =
                    Vector("FootMotionCorrectionResponseRequestedDirection"),
                CorrectionResponsePreviousDirection =
                    Vector("FootMotionCorrectionResponsePreviousDirection"),
                CorrectionResponseDirectionLimited =
                    Int("FootMotionCorrectionResponseDirectionLimited") != 0,
                CorrectionResponseMaximumDirectionChangeDegrees =
                    Float("FootMotionCorrectionResponseMaximumDirectionChangeDegrees"),
                CorrectionResponseAppliedDirectionChangeDegrees =
                    Float("FootMotionCorrectionResponseAppliedDirectionChangeDegrees"),
                CorrectionResponseVisibleOutputTransferred =
                    Int("FootMotionCorrectionResponseVisibleOutputTransferred") != 0,
                CorrectionResponseBeforeRebase =
                    Float("FootMotionCorrectionResponseBeforeRebase"),
                CorrectionResponsePrevious =
                    Float("FootMotionCorrectionResponsePrevious"),
                CorrectionResponseCurrent =
                    Float("FootMotionCorrectionResponseCurrent"),
                CorrectionResponseDirection =
                    Vector("FootMotionCorrectionResponseDirection"),
                CorrectionResponseDeltaDirection =
                    Cell("FootMotionCorrectionResponseDeltaDirection"),
                CorrectionResponseSelectedSpeed =
                    Float("FootMotionCorrectionResponseSelectedSpeed"),
                CorrectionResponseAppliedDelta =
                    Float("FootMotionCorrectionResponseAppliedDelta"),
                PlantVerticalContinuityOwners =
                    Cell("FootMotionPlantVerticalContinuityOwners"),
                PlantEffectiveCorrectionBefore =
                    Vector("FootMotionPlantEffectiveCorrectionBefore"),
                PlantEffectiveCorrectionAfter =
                    Vector("FootMotionPlantEffectiveCorrectionAfter"),
                PlantOutputDistance =
                    Float("FootMotionPlantOutputDistance"),
                PlantPenetrationDepth =
                    Float("FootMotionPlantPenetrationDepth"),
                CurrentSupport = bindings.CurrentSupport.Read(cells),
                Resolved = bindings.Resolved.Read(cells),
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
                FinalPhysicalWriteCompletionIdentity =
                    Ulong("FinalPhysicalWriteCompletionIdentity"),
                FinalPhysicalAnkleComponentPosition =
                    Vector("FinalPhysicalAnkleComponentPosition"),
                FinalPhysicalAnkleGoalResidual = Float("FinalPhysicalAnkleGoalResidual"),
                PenetrationAvailability = Cell("FootContactPlanePenetrationAvailability"),
                SourceHeel = Vector("FootMotionSourceHeel"),
                SourceToe = Vector("FootMotionSourceToe"),
                FinalHeel = Vector("FinalPhysicalHeelWorld"),
                FinalToe = Vector("FinalPhysicalToeWorld"),
                HasAnchor = Ulong("FootMotionLandingEventIdentity") != 0 &&
                            Cell("FootMotionConstraintState") != "Swing",
                OriginalExtensionRatio =
                    Float("FinalIkLegOriginalExtensionRatio"),
                TargetExtensionRatio = Float("FinalIkLegTargetExtensionRatio"),
                SolvedExtensionRatio = Float("FinalIkLegSolvedExtensionRatio"),
                SolvedBendDegrees = Float("FinalIkLegSolvedBendDegrees"),
                OriginalCompressionReserve =
                    Float("FinalIkLegOriginalCompressionReserve"),
                TargetCompressionReserve =
                    Float("FinalIkLegTargetCompressionReserve"),
                SolvedCompressionReserve =
                    Float("FinalIkLegSolvedCompressionReserve"),
                BendDirectionPreviousDot = Float("FinalIkLegEffectiveBendDirectionPreviousDot"),
                FinalIkLegAvailable = Int("FinalIkLegAvailable") != 0,
                FinalIkLegOriginalHip = Vector("FinalIkLegOriginalHip"),
                FinalIkLegOriginalKnee = Vector("FinalIkLegOriginalKnee"),
                FinalIkLegOriginalAnkle = Vector("FinalIkLegOriginalAnkle"),
                FinalIkLegTargetAnkle = Vector("FinalIkLegTargetAnkle"),
                FinalIkLegSolvedHip = Vector("FinalIkLegSolvedHip"),
                FinalIkLegSolvedKnee = Vector("FinalIkLegSolvedKnee"),
                FinalIkLegSolvedAnkle = Vector("FinalIkLegSolvedAnkle"),
                PrimarySupportAvailable =
                    Int("PrimarySupportHasValue") != 0,
                PrimarySupportSide = Cell("PrimarySupportSide"),
                PrimarySupportEventIdentity = Ulong("PrimarySupportLandingEventIdentity"),
                StrideState = Cell("StrideState"),
                PelvisHeightTarget = new PelvisHeightTargetFrame
                {
                    Available = Int("PelvisHeightTargetAvailable") != 0,
                    ComponentUp = Vector("PelvisHeightTargetComponentUp"),
                    LeftAnimatedSole = Vector("PelvisHeightTargetLeftAnimatedSole"),
                    RightAnimatedSole = Vector("PelvisHeightTargetRightAnimatedSole"),
                    LeftTargetSole = Vector("PelvisHeightTargetLeftTargetSole"),
                    RightTargetSole = Vector("PelvisHeightTargetRightTargetSole"),
                    AnimatedMinimumAlongUp = Float("PelvisHeightTargetAnimatedMinimumAlongUp"),
                    TargetMinimumAlongUp = Float("PelvisHeightTargetMinimumAlongUp"),
                    RequestedOffsetAlongUp = Float("PelvisRequestedOffsetAlongUp")
                },
                StrideSupportSide = Cell("StrideSupportSide"),
                Pelvis = new PelvisFrame
                {
                    Observation = new PelvisObservationFrame
                    {
                        PoseInputAvailable = Int("PelvisPoseInputAvailable") != 0,
                        PoseRootWorldPosition = Vector("StridePoseRootPosition"),
                        AnimatedWorldPosition = Vector("StrideAnimatedPelvis"),
                        AnimatedComponentPosition = Vector("StrideAnimatedPelvisComponentPosition"),
                        PhysicalWorldPosition = Vector("FinalPhysicalPelvisWorldPosition"),
                        GoalResidualAvailable = Int("FinalPhysicalPelvisGoalResidualAvailable") != 0,
                        GoalResidual = Float("FinalPhysicalPelvisGoalResidual")
                    },
                    Posture = new PelvisPostureFrame
                    {
                        Evaluated = Int("PelvisPosturePreferenceEvaluated") != 0,
                        Available = Int("PelvisPosturePreferenceAvailable") != 0,
                        Hip = Vector("PelvisPosturePreferenceHip"),
                        AnimatedAnkle = Vector("PelvisPosturePreferenceAnimatedAnkle"),
                        TargetAnkle = Vector("PelvisPosturePreferenceTargetAnkle"),
                        LegLength = Float("PelvisPosturePreferenceLegLength"),
                        CompressionReserve = Float("PelvisPosturePreferenceCompressionReserve"),
                        UsableLegLength = Float("PelvisPosturePreferenceUsableLegLength"),
                        MinimumAlongUp = Float("PelvisPosturePreferenceMinimumAlongUp"),
                        MaximumAlongUp = Float("PelvisPosturePreferenceMaximumAlongUp"),
                        OffsetAlongUp = Float("PelvisPosturePreferenceOffsetAlongUp"),
                        TargetAdjusted = Int("PelvisPosturePreferenceTargetAdjusted") != 0,
                    },
                    Reach = new PelvisReachFrame
                    {
                        ComponentUp = Vector("PelvisReachComponentUp"),
                        Status = EnumField<CharacterFootPelvisReachStatus>("PelvisReachStatus"),
                        IntersectionEvaluated = Int("PelvisReachIntersectionEvaluated") != 0,
                        IntersectionMinimumAlongUp = Float("PelvisReachIntersectionMinimumAlongUp"),
                        IntersectionMaximumAlongUp = Float("PelvisReachIntersectionMaximumAlongUp"),
                        Left = PelvisLeg("PelvisReachLeft"),
                        Right = PelvisLeg("PelvisReachRight")
                    },
                    Response = new PelvisResponseFrame
                    {
                        Evaluated = Int("PelvisResponseEvaluated") != 0,
                        Completed = Int("PelvisSpringCompleted") != 0,
                        IntegratedOutput = Float("PelvisSpringIntegratedOutput"),
                        HadPreviousState = Int("StrideHadPreviousState") != 0,
                        SupportChanged = Int("StrideSupportChanged") != 0,
                        VelocityReset = Int("StrideSpringVelocityReset") != 0,
                        PreviousTarget = Float("StridePreviousSpringTarget"),
                        PreviousOutput = Float("StridePreviousSpringOutput"),
                        PreviousVelocity = Float("StridePreviousSpringVelocity"),
                        Input = Float("StrideSpringInput"),
                        InputVelocity = Float("StrideSpringInputVelocity"),
                        Frequency = Float("StrideSpringFrequency"),
                        Target = Float("StrideSpringTarget"),
                        Output = Float("StrideSpringOutput"),
                        Velocity = Float("StrideSpringVelocity"),
                        PositionWeight = Float("StridePositionWeight"),
                        PreviousSlope = EnumField<CharacterFootStrideSlope>("StridePreviousSlope"),
                        Handoff = FlagsField<CharacterFootPelvisSpringHandoffReason>("StrideSpringHandoffReason", '|')
                    }
                },
                StrideSlope = EnumField<CharacterFootStrideSlope>("StrideSlope"),
                StrideRejectReason = EnumField<CharacterFootStrideRejectReason>("StrideRejectReason"),
                StridePelvisDelta = Vector("StridePelvisDelta"),
                StrideSpringOutput = Float("StrideSpringOutput"),
                PelvisWeight = Float("PelvisPositionWeight"),
                FinalPelvisGoal = Vector("FinalPelvisGoal"),
                PhysicalPelvis = Vector("FinalPhysicalPelvisComponentPosition")
            };
            RequireValidFrame(frame);
            frame.ContactSupportGap = ResolveContactSupportGap(frame);
            return frame;
        }

        static void RequireValidFrame(FootFrame frame)
        {
            if (frame.Frame <= 0 || frame.CompletionIdentity == 0)
                throw new InvalidDataException("Foot Motion Foot row lineage is invalid.");
            if (frame.Side != "Left" && frame.Side != "Right")
                throw new InvalidDataException(
                    $"Foot Motion Foot row Side '{frame.Side}' is invalid.");
            RequirePredictionMotion(frame);
            RequireEnum<CharacterFootLandingStepSource>(
                frame.SelectedStepSource,
                "SelectedStepSource");
            bool selectedStepConsistent = frame.SelectedStepSource == "None"
                ? frame.SelectedLandingEventIdentity == 0
                : frame.SelectedStepSource == "FormalCurrentContact"
                    ? frame.SelectedLandingEventIdentity ==
                      frame.InputEvents.Current.Identity
                    : frame.SelectedLandingEventIdentity ==
                      frame.InputEvents.Next.Identity;
            if (!selectedStepConsistent ||
                frame.StepSelectionMaximumPredictionTimeSeconds <= 0f)
            {
                throw new InvalidDataException(
                    "Foot Motion Step candidate selection facts are inconsistent.");
            }
            RequireFormalApproachProgress(
                frame.FormalOutputObservationAvailable,
                frame.OutputEvents.Phase,
                frame.OutputEvents.ApproachProgress,
                frame.OutputEvents.InApproach,
                "FormalEvent");
            RequireFormalApproachProgress(
                frame.FormalObservationAvailable,
                frame.InputEvents.Phase,
                frame.InputEvents.ApproachProgress,
                frame.InputEvents.InApproach,
                "InputFormalEvent");
            RequireStepPhase(frame.CurrentStep, "CurrentStep");
            RequireStepPhase(frame.IncomingStep, "IncomingStep");
            CharacterFootStepCandidateSample selected = frame.SelectedStepSource ==
                                          "FormalNextLanding"
                ? frame.CurrentStep
                : null;
            if (selected == null
                    ? frame.SelectedPhase.EventPhase != 0f ||
                      frame.SelectedPhase.ApproachContactToLandingProgress != 0f ||
                      frame.SelectedPhase.LandingPhase != 0f ||
                      frame.SelectedPhase.AtOrAfterApproachContact ||
                      frame.SelectedPhase.InApproachContactToLanding
                    : Math.Abs(
                          frame.SelectedPhase.EventPhase -
                          selected.EventPhase) > TimeEpsilon ||
                      Math.Abs(
                          frame.SelectedPhase.ApproachContactToLandingProgress -
                          selected.ApproachContactToLandingProgress) > TimeEpsilon ||
                      Math.Abs(
                          frame.SelectedPhase.LandingPhase -
                          selected.LandingPhase) > TimeEpsilon ||
                      frame.SelectedPhase.AtOrAfterApproachContact !=
                      selected.AtOrAfterApproachContact ||
                      frame.SelectedPhase.InApproachContactToLanding !=
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
            RequireEnum<CharacterFootPlantTargetKind>(
                frame.PlantTargetKind,
                "FootMotionPlantTargetKind");
            RequireEnum<CharacterFootLockResponse>(
                frame.PlantLockResponse,
                "FootMotionPlantLockResponse");
            RequireEnum<CharacterFootPlantTargetHeightUpdateReason>(
                frame.PlantTargetHeightUpdateReason,
                "FootMotionPlantTargetHeightUpdateReason");
            RequireEnum<CharacterFootTargetHeightAdoptionMode>(
                frame.SwingTargetHeightAdoptionMode,
                "FootMotionSwingTargetHeightAdoptionMode");
            RequireFlags<CharacterFootPlantResidualCaptureReason>(
                frame.PlantResidualCaptureReason,
                "FootMotionPlantResidualCaptureReason");
            RequireEnum<CharacterFootCorrectionResponseDeltaDirection>(
                frame.CorrectionResponseDeltaDirection,
                "FootMotionCorrectionResponseDeltaDirection");
            RequireEnum<CharacterFootCorrectionResponseInitializationReason>(
                frame.CorrectionResponseInitializationReason,
                "FootMotionCorrectionResponseInitializationReason");
            RequireFlags<CharacterFootVerticalContinuityOwner>(
                frame.PlantVerticalContinuityOwners,
                "FootMotionPlantVerticalContinuityOwners");
            RequireLifecycleTransitionFacts(frame);
            RequireCurrentSupport(frame);
            RequirePreparedAndSelectedTarget(frame);
            RequireCorrectionResponseDirectionHistory(frame);
            RequireResponseDomain(frame);
            RequireResolvedFoot(frame);
            RequireFormalGoalWeights(frame);
            RequireFootGoalComponentFacts(frame);
            RequireLegReachFacts(frame);
            frame.PelvisHeightTarget.RequireValid(frame);
            RequirePelvisFacts(frame);
            if (frame.LandingReachAvailable &&
                !frame.LandingReachEvaluated ||
                frame.PostTransitionReason == "LandingCompleted" &&
                (!frame.LandingReachEvaluated ||
                 !frame.LandingReachAvailable))
            {
                throw new InvalidDataException(
                    "Foot Motion Landing Reach facts are inconsistent.");
            }
            bool floorOwnerValid = frame.SafetyFloorOwner switch
            {
                "None" => !frame.SafetyFloorAvailable &&
                          frame.SafetyFloorOwnerSurfaceIdentity == 0 &&
                          frame.SafetyFloorOwnerPathIdentity == 0,
                "GroundPathEnvelope" => frame.SafetyFloorAvailable &&
                                        frame.SafetyFloorOwnerSurfaceIdentity == 0 &&
                                        frame.SafetyFloorOwnerPathIdentity != 0,
                "ContactAnchor" => frame.SafetyFloorAvailable &&
                                   frame.SafetyFloorOwnerSurfaceIdentity != 0 &&
                                   frame.SafetyFloorOwnerPathIdentity == 0 &&
                                   (frame.ConstraintState == "Landing" ||
                                     frame.ConstraintState == "Locked"),
                "PlantTarget" => frame.SafetyFloorAvailable &&
                                 frame.SafetyFloorOwnerSurfaceIdentity != 0 &&
                                 frame.SafetyFloorOwnerPathIdentity == 0 &&
                                 frame.ApproachPlantTargetPrepared &&
                                 !frame.PlantInterpolationEvaluated &&
                                 (frame.ConstraintState == "Swing" ||
                                  frame.ConstraintState == "UnlockedSupport"),
                _ => false
            };
            if (!floorOwnerValid)
            {
                throw new InvalidDataException(
                    "Foot Motion Safety Floor owner facts are inconsistent.");
            }
            if (frame.PlantInterpolationEvaluated)
            {
                RequireEnum<CharacterFootTargetHeightAdoptionMode>(
                    frame.PlantTargetHeightAdoptionMode,
                    "FootMotionPlantTargetHeightAdoptionMode");
                bool targetAdoptionDirect =
                    frame.PlantTargetHeightAdoptionMode == "Direct";
                bool directTargetUpdate =
                    frame.PlantTargetHeightUpdateReason == "Initialized" ||
                    frame.PlantTargetHeightUpdateReason == "EventChanged" ||
                    frame.PlantTargetHeightUpdateReason ==
                    "VerificationRefresh" ||
                    frame.PlantTargetHeightUpdateReason == "DirectFollow" ||
                    frame.PlantTargetHeightUpdateReason ==
                    "DirectAdoption" ||
                    frame.PlantTargetHeightUpdateReason ==
                    "ForceRefreshDistanceExceeded";
                float targetBudget =
                    frame.PlantTargetMaximumVerticalSpeed *
                    frame.DeltaSeconds;
                bool targetClampExpected =
                    frame.PlantTargetHeightUpdateReason == "RateLimited";
                bool targetHeightConsistent = Math.Abs(
                    frame.PlantTargetHeightBefore +
                    frame.PlantTargetAppliedVerticalDelta -
                    frame.PlantTargetHeightAfter) <= PositionNoiseFloor;
                Vector3 up = frame.ComponentUp.normalized;
                bool targetHeightTargetConsistent = Math.Abs(
                    frame.PlantTargetHeightTarget -
                    Vector3.Dot(frame.PlantDesiredPoint, up)) <=
                    PositionNoiseFloor &&
                    Math.Abs(
                        frame.PlantTargetVerticalDelta -
                        (frame.PlantTargetHeightTarget -
                         frame.PlantTargetHeightBefore)) <=
                    PositionNoiseFloor;
                bool distanceForceRefresh =
                    frame.PlantTargetHeightUpdateReason ==
                    "ForceRefreshDistanceExceeded";
                bool verificationRefresh =
                    frame.PlantTargetHeightUpdateReason ==
                    "VerificationRefresh";
                bool heldWithinRevisionDistance =
                    frame.PlantTargetHeightUpdateReason ==
                    "HeldWithinRevisionDistance";
                bool refreshCaptured = HasRevisionReason(
                    frame.PlantResidualCaptureReason,
                    "TargetHeightForceRefreshed");
                bool zeroDeltaVerificationRefresh =
                    frame.PlantTargetHeightUpdateReason == "None" &&
                    Math.Abs(frame.PlantTargetVerticalDelta) <=
                    PositionNoiseFloor &&
                    HasRevisionReason(
                        frame.PlantResidualCaptureReason,
                        "VerificationChanged");
                bool refreshReasonConsistent =
                    frame.PlantTargetForceRefreshed == refreshCaptured &&
                    (frame.PlantTargetForceRefreshed
                        ? distanceForceRefresh ||
                          verificationRefresh ||
                          zeroDeltaVerificationRefresh
                        : !distanceForceRefresh && !verificationRefresh);
                bool residualCaptured =
                    frame.PlantResidualCaptureReason != "None";
                Vector3 outputBefore = frame.OriginalSole +
                                       frame.PlantEffectiveCorrectionBefore;
                Vector3 expectedCapturedBeforeDecay = residualCaptured
                    ? outputBefore - frame.PlantSelectedWorldTarget
                    : frame.PlantWorldResidualBeforeCapture;
                bool residualCaptureConsistent = Vector3.Distance(
                    frame.PlantWorldResidualCapturedBeforeDecay,
                    expectedCapturedBeforeDecay) <= RuntimeGeometryEpsilon;
                bool residualActiveBeforeDecay =
                    frame.PlantWorldResidualCapturedBeforeDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon;
                bool residualDecayRequired = residualActiveBeforeDecay &&
                                             frame.DeltaSeconds > 0f;
                bool residualDeadlineConsistent =
                    frame.PlantWorldResidualDeadlineHalfLifeAvailable
                    ? float.IsFinite(
                          frame.PlantWorldResidualDeadlineHalfLifeSeconds) &&
                      frame.PlantWorldResidualDeadlineHalfLifeSeconds > 0f
                    : Math.Abs(
                          frame.PlantWorldResidualDeadlineHalfLifeSeconds) <=
                      TimeEpsilon;
                float expectedAppliedHalfLife =
                    frame.PlantWorldResidualDeadlineHalfLifeAvailable
                        ? Math.Min(
                            frame.PlantWorldResidualBaseHalfLifeSeconds,
                            frame.PlantWorldResidualDeadlineHalfLifeSeconds)
                        : frame.PlantWorldResidualBaseHalfLifeSeconds;
                bool residualHalfLifeConsistent =
                    float.IsFinite(
                        frame.PlantWorldResidualBaseHalfLifeSeconds) &&
                    frame.PlantWorldResidualBaseHalfLifeSeconds > 0f &&
                    residualDeadlineConsistent &&
                    (frame.PlantWorldResidualDecayApplied
                        ? float.IsFinite(
                              frame.PlantWorldResidualAppliedHalfLifeSeconds) &&
                          frame.PlantWorldResidualAppliedHalfLifeSeconds > 0f &&
                          Math.Abs(
                              frame.PlantWorldResidualAppliedHalfLifeSeconds -
                              expectedAppliedHalfLife) <= TimeEpsilon &&
                          residualActiveBeforeDecay &&
                          frame.DeltaSeconds > 0f
                        : Math.Abs(
                              frame.PlantWorldResidualAppliedHalfLifeSeconds) <=
                          TimeEpsilon);
                Vector3 expectedAdvancedResidual =
                    frame.PlantWorldResidualDecayApplied
                        ? AdvanceResidual(
                            frame.PlantWorldResidualCapturedBeforeDecay,
                            frame.DeltaSeconds,
                            frame.PlantWorldResidualAppliedHalfLifeSeconds)
                        : frame.PlantWorldResidualCapturedBeforeDecay;
                bool expectedClearedAtCompletionTolerance =
                    frame.PlantWorldResidualDecayApplied &&
                    expectedAdvancedResidual.magnitude <=
                    frame.PlantWorldResidualCompletionTolerance;
                Vector3 expectedResidualAfterDecay =
                    expectedClearedAtCompletionTolerance
                        ? default
                        : expectedAdvancedResidual;
                bool residualDecayConsistent =
                    float.IsFinite(
                        frame.PlantWorldResidualCompletionTolerance) &&
                    frame.PlantWorldResidualCompletionTolerance > 0f &&
                    frame.PlantWorldResidualClearedAtCompletionTolerance ==
                    expectedClearedAtCompletionTolerance &&
                    Vector3.Distance(
                        frame.PlantWorldResidualAfterDecay,
                        expectedResidualAfterDecay) <=
                    RuntimeGeometryEpsilon;
                CharacterFootVerticalContinuityOwner expectedOwners =
                    CharacterFootVerticalContinuityOwner.PlantTarget;
                if (frame.PlantTargetHeightUpdateReason != "None" ||
                    frame.PlantTargetVerticalClamped ||
                    frame.PlantTargetForceRefreshed)
                {
                    expectedOwners |=
                        CharacterFootVerticalContinuityOwner.TargetHeightHistory;
                }
                if (residualCaptured ||
                    frame.PlantWorldResidualCapturedBeforeDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.PlantWorldResidualAfterDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon)
                {
                    expectedOwners |=
                        CharacterFootVerticalContinuityOwner.PlantWorldResidual;
                }
                bool ownersConsistent = Enum.TryParse(
                    frame.PlantVerticalContinuityOwners,
                    out CharacterFootVerticalContinuityOwner actualOwners) &&
                    actualOwners == expectedOwners;
                if (frame.PlantWorldResidualDecayApplied !=
                    residualDecayRequired)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant World Residual decay application is inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side} " +
                        $"CaptureReason={frame.PlantResidualCaptureReason} " +
                        $"ResidualActive={residualActiveBeforeDecay} " +
                        $"DecayApplied={frame.PlantWorldResidualDecayApplied}.");
                }
                if (frame.PlantTargetEventIdentity == 0 ||
                    frame.PlantTargetKind == "None" ||
                    !FiniteVector(frame.ComponentUp) ||
                    frame.ComponentUp.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.PlantTargetHeightEventIdentity !=
                    frame.PlantTargetEventIdentity ||
                    !FiniteVector(frame.PlantDesiredPoint) ||
                    !FiniteVector(frame.PlantFilteredPoint) ||
                    !FiniteVector(frame.PlantPreviousSelectedWorldTarget) ||
                    !FiniteVector(frame.PlantSelectedWorldTarget) ||
                    !FiniteVector(
                        frame.PreviousResponseOutputPoint) ||
                    !FiniteVector(frame.DesiredOutputPoint) ||
                    !FiniteVector(frame.ResponseOutputPoint) ||
                    !FiniteVector(frame.PlantWorldResidualBeforeCapture) ||
                    !FiniteVector(
                        frame.PlantWorldResidualCapturedBeforeDecay) ||
                    !FiniteVector(frame.PlantWorldResidualAfterDecay) ||
                    !FiniteVector(frame.PlantEffectiveCorrectionBefore) ||
                    !FiniteVector(frame.PlantEffectiveCorrectionAfter) ||
                    !FiniteVector(frame.CorrectionResponseRequestedDirection) ||
                    !FiniteVector(frame.CorrectionResponsePreviousDirection) ||
                    !FiniteVector(frame.CorrectionResponseDirection) ||
                    frame.CorrectionResponseRequestedDirection.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.CorrectionResponseDirection.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    Math.Abs(
                        frame.CorrectionResponseRequestedDirection.magnitude -
                        1f) > RuntimeGeometryEpsilon ||
                    Math.Abs(
                        frame.CorrectionResponseDirection.magnitude - 1f) >
                        RuntimeGeometryEpsilon ||
                    frame.CorrectionResponseInitializedBefore &&
                        Math.Abs(
                            frame.CorrectionResponsePreviousDirection.magnitude -
                            1f) > RuntimeGeometryEpsilon ||
                    !float.IsFinite(
                        frame.CorrectionResponseMaximumDirectionChangeDegrees) ||
                    frame.CorrectionResponseMaximumDirectionChangeDegrees <= 0f ||
                    frame.CorrectionResponseMaximumDirectionChangeDegrees > 180f ||
                    !float.IsFinite(
                        frame.CorrectionResponseAppliedDirectionChangeDegrees) ||
                    frame.CorrectionResponseAppliedDirectionChangeDegrees < 0f ||
                    frame.CorrectionResponseAppliedDirectionChangeDegrees >
                        frame.CorrectionResponseMaximumDirectionChangeDegrees +
                        RotationNoiseFloorDegrees ||
                    !float.IsFinite(frame.PlantTargetMaximumVerticalSpeed) ||
                    frame.PlantTargetMaximumVerticalSpeed <= 0f ||
                    !float.IsFinite(frame.PlantTargetHeightBefore) ||
                    !float.IsFinite(frame.PlantTargetHeightTarget) ||
                    !float.IsFinite(frame.PlantTargetVerticalDelta) ||
                    !float.IsFinite(frame.PlantTargetAppliedVerticalDelta) ||
                    !float.IsFinite(frame.PlantTargetHeightAfter) ||
                    !float.IsFinite(frame.PlantTargetForceRefreshDistance) ||
                    frame.PlantTargetForceRefreshDistance <=
                        frame.PathRevisionDistance ||
                    !refreshReasonConsistent ||
                    targetAdoptionDirect &&
                        (frame.PlantTargetHeightUpdateReason == "RateLimited" ||
                         frame.PlantTargetHeightUpdateReason == "WithinRate" ||
                         heldWithinRevisionDistance ||
                         distanceForceRefresh) ||
                    !targetAdoptionDirect &&
                        frame.PlantTargetHeightUpdateReason ==
                        "DirectAdoption" ||
                    heldWithinRevisionDistance &&
                        (targetAdoptionDirect ||
                         Math.Abs(frame.PlantTargetVerticalDelta) >
                         frame.PathRevisionDistance + PositionNoiseFloor ||
                         Math.Abs(
                             frame.PlantTargetAppliedVerticalDelta) >
                         PositionNoiseFloor) ||
                    distanceForceRefresh &&
                        Math.Abs(frame.PlantTargetVerticalDelta) <
                        frame.PlantTargetForceRefreshDistance -
                        PositionNoiseFloor ||
                    !directTargetUpdate &&
                    Math.Abs(frame.PlantTargetAppliedVerticalDelta) >
                    targetBudget + PositionNoiseFloor ||
                    frame.PlantTargetVerticalClamped != targetClampExpected ||
                    !targetHeightConsistent ||
                    !targetHeightTargetConsistent ||
                    !residualCaptureConsistent ||
                    !residualHalfLifeConsistent ||
                    !residualDecayConsistent ||
                    !frame.CorrectionResponseEvaluated ||
                    !float.IsFinite(frame.CorrectionResponseDesired) ||
                    !float.IsFinite(frame.CorrectionResponsePrevious) ||
                    !float.IsFinite(frame.CorrectionResponseCurrent) ||
                    !float.IsFinite(
                        frame.CorrectionResponseSelectedSpeed) ||
                    !float.IsFinite(
                        frame.CorrectionResponseAppliedDelta) ||
                    !ownersConsistent ||
                    frame.SelectedSupportTarget.Available &&
                        Vector3.Distance(
                            frame.SelectedSupportTarget.Normal,
                            frame.CorrectionResponseDirection) >
                        RuntimeGeometryEpsilon ||
                    Vector3.Distance(
                        frame.DesiredOutputPoint,
                        frame.PlantSelectedWorldTarget +
                        frame.PlantWorldResidualAfterDecay) >
                    PositionNoiseFloor ||
                    Vector3.Distance(frame.ResponseOutputPoint, ExpectedResponseOutput(frame)) >
                    PositionNoiseFloor ||
                    frame.PreviousResponseOutputAvailable &&
                        Vector3.Distance(
                            frame.PreviousResponseOutputPoint,
                            outputBefore) > PositionNoiseFloor ||
                    Vector3.Distance(
                        frame.PlantEffectiveCorrectionAfter,
                        frame.ResponseOutputPoint -
                        frame.OriginalSole) >
                    PositionNoiseFloor ||
                    Vector3.Distance(
                        frame.PlantEffectiveCorrectionAfter,
                        frame.InterpolationOutputCorrection) >
                    PositionNoiseFloor ||
                    !float.IsFinite(frame.PlantOutputDistance) ||
                    frame.PlantOutputDistance < 0f ||
                    !float.IsFinite(frame.PlantPenetrationDepth) ||
                    frame.PlantPenetrationDepth < 0f)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant interpolation facts are inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side} " +
                        $"TargetHeightUpdateReason={frame.PlantTargetHeightUpdateReason} " +
                        $"Refresh={refreshReasonConsistent} " +
                        $"TargetHeight={targetHeightConsistent && targetHeightTargetConsistent} " +
                        $"ResidualCapture={residualCaptureConsistent} " +
                        $"ResidualHalfLife={residualHalfLifeConsistent} " +
                        $"ResidualDecay={residualDecayConsistent} " +
                        $"ResponseDomain={frame.CorrectionResponseDomain} " +
                        $"Owners={ownersConsistent} " +
                        $"DesiredOutputError={Vector3.Distance(frame.DesiredOutputPoint, frame.PlantSelectedWorldTarget + frame.PlantWorldResidualAfterDecay):R} " +
                        $"ResponseOutputError={Vector3.Distance(frame.ResponseOutputPoint, ExpectedResponseOutput(frame)):R} " +
                        $"PreviousOutputError={Vector3.Distance(frame.PreviousResponseOutputPoint, outputBefore):R} " +
                        $"EffectiveResponseError={Vector3.Distance(frame.PlantEffectiveCorrectionAfter, frame.ResponseOutputPoint - frame.OriginalSole):R} " +
                        $"InterpolationError={Vector3.Distance(frame.PlantEffectiveCorrectionAfter, frame.InterpolationOutputCorrection):R} " +
                        $"DirectionMagnitude={frame.CorrectionResponseDirection.magnitude:R} " +
                        $"PreviousDirectionMagnitude={frame.CorrectionResponsePreviousDirection.magnitude:R} " +
                        $"DirectMode={targetAdoptionDirect} DirectUpdate={directTargetUpdate} " +
                        $"Held={heldWithinRevisionDistance} DistanceRefresh={distanceForceRefresh} " +
                        $"ClampExpected={targetClampExpected} Clamp={frame.PlantTargetVerticalClamped} " +
                        $"TargetBudget={targetBudget:R} AppliedTargetDelta={frame.PlantTargetAppliedVerticalDelta:R} " +
                        $"ForceDistance={frame.PlantTargetForceRefreshDistance:R} PathDistance={frame.PathRevisionDistance:R} " +
                        $"TargetEvent={frame.PlantTargetEventIdentity} HeightEvent={frame.PlantTargetHeightEventIdentity} Kind={frame.PlantTargetKind} " +
                        $"ResponseEvaluated={frame.CorrectionResponseEvaluated} OutputDistance={frame.PlantOutputDistance:R} Penetration={frame.PlantPenetrationDepth:R} " +
                        $"FinitePreviousTarget={FiniteVector(frame.PlantPreviousSelectedWorldTarget)} FiniteSelectedTarget={FiniteVector(frame.PlantSelectedWorldTarget)} " +
                        $"FiniteResponseScalars={float.IsFinite(frame.CorrectionResponseDesired) && float.IsFinite(frame.CorrectionResponsePrevious) && float.IsFinite(frame.CorrectionResponseCurrent) && float.IsFinite(frame.CorrectionResponseSelectedSpeed) && float.IsFinite(frame.CorrectionResponseAppliedDelta)}.");
                }
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
                float expectedRawFormalTargetHeight =
                    envelopeAlongUp + frame.SwingFormalFootHeight;
                float expectedEnvelopeMinimumCorrection =
                    envelopeAlongUp - originalSoleAlongUp;
                float expectedBuilderSelectedCorrection = Mathf.Max(
                    0f,
                    expectedRawFormalTargetHeight - originalSoleAlongUp);
                if (Math.Abs(
                        baselineAlongUp -
                        frame.SwingBaselineSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        envelopeAlongUp -
                        frame.SwingEnvelopeSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.SwingRawFormalTargetHeight -
                        expectedRawFormalTargetHeight) >
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
                    float expectedHeightDelta =
                        frame.SwingRawTargetHeightAlongUp -
                        frame.SwingFilteredTargetHeightBefore;
                    float maximumHeightDelta = ResolveVerticalHistoryDelta(
                        frame.DeltaSeconds,
                        frame.SwingTargetMaximumVerticalSpeed);
                    float expectedAppliedHeightDelta =
                        frame.SwingTargetHeightUpdateHeld
                            ? 0f
                            : frame.SwingTargetHeightForceRefreshed
                            ? expectedHeightDelta
                            : frame.SwingTargetHeightRateLimited
                            ? Mathf.Clamp(
                                expectedHeightDelta,
                                -maximumHeightDelta,
                                maximumHeightDelta)
                            : expectedHeightDelta;
                    float expectedFilteredTargetHeight =
                        frame.SwingFilteredTargetHeightBefore +
                        expectedAppliedHeightDelta;
                    bool expectedHeightClamp =
                        !frame.SwingTargetHeightUpdateHeld &&
                        !frame.SwingTargetHeightForceRefreshed &&
                        frame.SwingTargetHeightRateLimited &&
                        !Mathf.Approximately(
                            expectedHeightDelta,
                            expectedAppliedHeightDelta);
                    bool directHeightAdoption =
                        frame.SwingTargetHeightAdoptionMode == "Direct";
                    float expectedFilteredCorrection = Mathf.Max(
                        0f,
                        expectedFilteredTargetHeight - originalSoleAlongUp);
                    if (!frame.PathContinuityEvaluated ||
                        !frame.PathAvailableAfter ||
                        frame.PathCurrentLandingEventIdentity !=
                            frame.FootMotionEventIdentity ||
                        Math.Abs(
                            frame.SwingRawTargetHeightAlongUp -
                            expectedRawFormalTargetHeight) >
                        PositionNoiseFloor ||
                        Math.Abs(
                            frame.SwingTargetHeightDelta -
                            expectedHeightDelta) >
                        PositionNoiseFloor ||
                        Math.Abs(
                            frame.SwingTargetHeightAppliedDelta -
                            expectedAppliedHeightDelta) >
                        PositionNoiseFloor ||
                        frame.SwingTargetHeightClamped != expectedHeightClamp ||
                        !float.IsFinite(
                            frame.SwingTargetHeightForceRefreshDistance) ||
                        frame.SwingTargetHeightForceRefreshDistance <=
                            frame.PathRevisionDistance ||
                        directHeightAdoption &&
                            (frame.SwingTargetHeightForceRefreshed ||
                             frame.SwingTargetHeightRateLimited ||
                             frame.SwingTargetHeightClamped) ||
                        frame.SwingTargetHeightUpdateHeld &&
                            (frame.SwingTargetHeightForceRefreshed ||
                             frame.SwingTargetHeightRateLimited) ||
                        frame.SwingTargetHeightForceRefreshed &&
                            (frame.SwingTargetHeightRateLimited ||
                             frame.SwingTargetHeightClamped) ||
                        Math.Abs(
                            frame.SwingFilteredTargetHeightAlongUp -
                            expectedFilteredTargetHeight) >
                        PositionNoiseFloor ||
                        Vector3.Distance(
                            frame.BuilderSwingTargetCorrection,
                            up * expectedFilteredCorrection) >
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
            RequireRevisionReason(frame.PathRevisionReason);
        }

        static void RequirePredictionMotion(FootFrame frame)
        {
            RequireEnum<CharacterFootPredictionMotionRejectReason>(
                frame.PredictionMotionRejectReason,
                "PredictionMotionRejectReason");
            RequireEnum<CharacterFootPredictionMotionResetReason>(
                frame.PredictionMotionResetReason,
                "PredictionMotionResetReason");
            if (!FiniteVector(frame.BodyTargetVelocity) ||
                !FiniteVector(frame.TimelineCurrentVelocity) ||
                !FiniteVector(frame.TimelineContinuationVelocity) ||
                !FiniteVector(frame.PredictionRawCurrentVelocity) ||
                !FiniteVector(frame.PredictionRawContinuationVelocity) ||
                !FiniteVector(frame.PredictionPreviousStableCurrentVelocity) ||
                !FiniteVector(frame.PredictionPreviousStableContinuationVelocity) ||
                !FiniteVector(frame.PredictionStableCurrentVelocity) ||
                !FiniteVector(frame.PredictionStableContinuationVelocity) ||
                !FiniteVector(frame.PredictionCurrentVelocityDelta) ||
                !FiniteVector(frame.PredictionContinuationVelocityDelta) ||
                !float.IsFinite(frame.PredictionVelocityResponseAlpha) ||
                !float.IsFinite(frame.PredictionVelocityDeltaThreshold) ||
                frame.PredictionVelocityDeltaThreshold <= 0f ||
                !float.IsFinite(frame.PredictionVelocitySmoothSpeed) ||
                frame.PredictionVelocitySmoothSpeed <= 0f ||
                !float.IsFinite(frame.PredictionMaximumSpeed) ||
                frame.PredictionMaximumSpeed <=
                frame.PredictionVelocityDeltaThreshold)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion facts are non-finite or invalid.");
            }
            float expectedAlpha = Mathf.Clamp01(
                frame.PredictionVelocitySmoothSpeed * frame.DeltaSeconds);
            Vector2 bodyTargetCurrent = new Vector2(
                frame.BodyTargetVelocity.x,
                frame.BodyTargetVelocity.z);
            if (Vector2.Distance(
                    frame.PredictionRawCurrentVelocity,
                    bodyTargetCurrent) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionRawContinuationVelocity,
                    frame.TimelineContinuationVelocity) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion input facts are inconsistent.");
            }
            if (!frame.PredictionMotionAvailable)
            {
                if (frame.PredictionMotionRejectReason == "None" ||
                    frame.PredictionMotionResetReason != "None" ||
                    frame.PredictionMotionRevision != 0 ||
                    Math.Abs(frame.PredictionVelocityResponseAlpha) >
                    TimeEpsilon)
                {
                    throw new InvalidDataException(
                        "Unavailable Foot Prediction Motion facts are inconsistent.");
                }
                return;
            }
            if (frame.PredictionMotionRejectReason != "None" ||
                frame.PredictionMotionRevision == 0 ||
                string.IsNullOrWhiteSpace(
                    frame.PredictionMotionSourceIdentity) ||
                Math.Abs(
                    frame.PredictionVelocityResponseAlpha - expectedAlpha) >
                TimeEpsilon)
            {
                throw new InvalidDataException(
                    "Available Foot Prediction Motion lineage is invalid.");
            }
            Vector2 expectedCurrentDelta =
                frame.PredictionRawCurrentVelocity -
                frame.PredictionPreviousStableCurrentVelocity;
            Vector2 expectedContinuationDelta =
                frame.PredictionRawContinuationVelocity -
                frame.PredictionPreviousStableContinuationVelocity;
            bool reset = frame.PredictionMotionResetReason != "None";
            bool expectedCurrentResponse = !reset &&
                expectedCurrentDelta.magnitude >
                frame.PredictionVelocityDeltaThreshold;
            bool expectedContinuationResponse = !reset &&
                expectedContinuationDelta.magnitude >
                frame.PredictionVelocityDeltaThreshold;
            Vector2 currentCandidate = reset
                ? frame.PredictionRawCurrentVelocity
                : expectedCurrentResponse
                    ? frame.PredictionPreviousStableCurrentVelocity +
                      expectedCurrentDelta * expectedAlpha
                    : frame.PredictionPreviousStableCurrentVelocity;
            Vector2 continuationCandidate = reset
                ? frame.PredictionRawContinuationVelocity
                : expectedContinuationResponse
                    ? frame.PredictionPreviousStableContinuationVelocity +
                      expectedContinuationDelta * expectedAlpha
                    : frame.PredictionPreviousStableContinuationVelocity;
            bool expectedCurrentClamped =
                currentCandidate.magnitude > frame.PredictionMaximumSpeed;
            bool expectedContinuationClamped =
                continuationCandidate.magnitude > frame.PredictionMaximumSpeed;
            Vector2 expectedCurrent = Vector2.ClampMagnitude(
                currentCandidate,
                frame.PredictionMaximumSpeed);
            Vector2 expectedContinuation = Vector2.ClampMagnitude(
                continuationCandidate,
                frame.PredictionMaximumSpeed);
            if (Vector2.Distance(
                    frame.PredictionCurrentVelocityDelta,
                    expectedCurrentDelta) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionContinuationVelocityDelta,
                    expectedContinuationDelta) > PositionNoiseFloor ||
                frame.PredictionCurrentResponseApplied !=
                expectedCurrentResponse ||
                frame.PredictionContinuationResponseApplied !=
                expectedContinuationResponse ||
                frame.PredictionCurrentMaximumSpeedClamped !=
                expectedCurrentClamped ||
                frame.PredictionContinuationMaximumSpeedClamped !=
                expectedContinuationClamped ||
                Vector2.Distance(
                    frame.PredictionStableCurrentVelocity,
                    expectedCurrent) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionStableContinuationVelocity,
                    expectedContinuation) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion control facts are inconsistent.");
            }
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
                    frame.LandingObservationQueryPurpose != "0" ||
                    frame.LandingObservationRefreshMode != "0" ||
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
            bool forcedVerification =
                frame.LandingObservationRefreshMode ==
                "ForcedPlantVerification";
            bool purposeMatchesRefresh = forcedVerification
                ? frame.LandingObservationQueryPurpose ==
                  "CurrentContactVerification"
                : frame.LandingObservationQueryPurpose == "FutureLanding" &&
                  (frame.LandingObservationRefreshMode == "Thresholded" ||
                   frame.LandingObservationRefreshMode ==
                   "ChangedSlidingAdmissionInput");
            if (frame.LandingObservationWorldRevision == 0 ||
                frame.LandingObservationSourceSampleIdentity == 0 ||
                !queried && !reused ||
                !purposeMatchesRefresh ||
                frame.FutureLandingQueryPurpose !=
                frame.LandingObservationQueryPurpose ||
                forcedVerification && !queried ||
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
                    frame.GroundPathComponentUp.sqrMagnitude > 0.000001f ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.ActualFootWithinGroundPathCorridor)
                {
                    throw new InvalidDataException(
                        "Foot Motion invalid-up Swing Path axis facts are inconsistent.");
                }
                return;
            }
            Vector3 up = frame.GroundPathComponentUp.normalized;
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

        static void RequireFormalApproachProgress(
            bool available,
            string phase,
            float progress,
            bool inApproach,
            string prefix)
        {
            if (!available)
                return;
            RequireEnum<AnimationFootMotionEventPhase>(phase, prefix + "Phase");
            bool approach = phase == "ApproachContact";
            if (!float.IsFinite(progress) || progress < 0f || progress > 1f ||
                inApproach != approach || !approach && progress != 0f)
            {
                throw new InvalidDataException(
                    $"Foot Motion {prefix} Approach progress facts are inconsistent.");
            }
        }

        static void AnalyzeCurrentSupportQueries(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                FootFrame frame = frames[i];
                if (!frame.CurrentSupport.Specified)
                    continue;
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["HeelCandidateCount"] =
                        frame.CurrentSupport.Heel.CandidateCount,
                    ["ToeCandidateCount"] =
                        frame.CurrentSupport.Toe.CandidateCount,
                    ["HeelRequiredDisplacement"] =
                        frame.CurrentSupport.HeelRequiredDisplacement,
                    ["ToeRequiredDisplacement"] =
                        frame.CurrentSupport.ToeRequiredDisplacement
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["available"] = frame.CurrentSupport.Available,
                    ["heelAccepted"] = frame.CurrentSupport.Heel.Accepted,
                    ["toeAccepted"] = frame.CurrentSupport.Toe.Accepted,
                    ["heelSphereCastExecuted"] =
                        frame.CurrentSupport.Heel.SphereCastExecuted,
                    ["toeSphereCastExecuted"] =
                        frame.CurrentSupport.Toe.SphereCastExecuted
                };
                events.Add(new EventFact(
                    "CurrentSupportQuery",
                    frame.Side,
                    frame.Frame,
                    frame.Frame,
                    frame.Frame,
                    ResolveEventIdentity(frame),
                    frame.SourceIdentity,
                    frame.SourceCycle,
                    DeltaSeconds(frame),
                    metrics,
                    evidence));
            }
        }

        static void RequireSupportTarget(
            CharacterFootSupportTargetSample target,
            string side,
            string prefix)
        {
            if (!target.Available)
            {
                if (target.Frame != 0 || target.Completion != 0 ||
                    target.Surface != 0 || target.WorldRevision != 0)
                {
                    throw new InvalidDataException(
                        $"Foot Motion {prefix} unavailable lineage is inconsistent.");
                }
                return;
            }
            RequireEnum<CharacterFootSide>(target.Side, prefix + "Side");
            RequireEnum<CharacterFootSupportTargetKind>(
                target.Kind,
                prefix + "Kind");
            RequireEnum<CharacterFootSupportPositionSource>(
                target.PositionSource,
                prefix + "PositionSource");
            RequireEnum<CharacterFootSupportNormalSource>(
                target.NormalSource,
                prefix + "NormalSource");
            if (target.Side != side || target.Frame == 0 ||
                target.Completion == 0 || target.Surface == 0 ||
                target.WorldRevision == 0 || target.PositionFrame == 0 ||
                target.PositionCompletion == 0 || target.NormalFrame == 0 ||
                target.NormalCompletion == 0 ||
                !FiniteVector(target.Position) || !FiniteVector(target.Normal) ||
                Math.Abs(target.Normal.magnitude - 1f) >
                RuntimeGeometryEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion {prefix} facts are inconsistent.");
            }
        }

        static void RequireCurrentSupportProbe(
            CharacterFootCurrentSupportProbeSample probe,
            string kind,
            ulong worldRevision,
            string prefix)
        {
            RequireEnum<CharacterFootPlacementQueryPurpose>(
                probe.Purpose,
                prefix + "Purpose");
            RequireEnum<CharacterFootCurrentSupportProbeKind>(
                probe.Kind,
                prefix + "Kind");
            RequireEnum<CharacterFootCurrentSupportProbeState>(
                probe.State,
                prefix + "State");
            RequireEnum<CharacterFootCurrentSupportProbeRejectReason>(
                probe.RejectReason,
                prefix + "RejectReason");
            bool accepted = probe.State == "Accepted" &&
                            probe.RejectReason == "None" &&
                            probe.Surface != 0 &&
                            probe.WorldRevision == worldRevision;
            if (probe.Purpose != "CurrentSupport" || probe.Kind != kind ||
                !FiniteVector(probe.ProbePosition) ||
                !FiniteVector(probe.ComponentUp) ||
                probe.ComponentUp.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                !FiniteVector(probe.Origin) ||
                !FiniteVector(probe.Direction) ||
                probe.Direction.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                !float.IsFinite(probe.MaximumDistance) ||
                probe.MaximumDistance <= 0f ||
                !float.IsFinite(probe.Radius) || probe.Radius <= 0f ||
                probe.LayerMask == 0 || probe.HitCapacity < 4 ||
                probe.HitCapacity > 32 || probe.CandidateCount < 0 ||
                !float.IsFinite(probe.MinimumGroundNormalDot) ||
                probe.MinimumGroundNormalDot < -1f ||
                probe.MinimumGroundNormalDot > 1f ||
                probe.Accepted != accepted ||
                accepted && (!probe.SphereCastExecuted ||
                             !FiniteVector(probe.Point) ||
                             !FiniteVector(probe.Normal) ||
                             probe.Normal.sqrMagnitude <=
                             RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                             !float.IsFinite(probe.Distance) ||
                             probe.Distance < 0f))
            {
                throw new InvalidDataException(
                    $"Foot Motion {prefix} facts are inconsistent.");
            }
        }

        static void RequireCurrentSupport(FootFrame frame)
        {
            if (!frame.CurrentSupport.Specified)
            {
                if (frame.CurrentSupport.Available ||
                    frame.CurrentSupport.Target.Available)
                {
                    throw new InvalidDataException(
                        "Foot Motion unspecified Current Support is available.");
                }
                return;
            }
            RequireEnum<CharacterFootCurrentSupportRejectReason>(
                frame.CurrentSupport.RejectReason,
                "CurrentSupportRejectReason");
            RequireCurrentSupportProbe(
                frame.CurrentSupport.Heel,
                "Heel",
                frame.CurrentSupport.WorldRevision,
                "CurrentSupportHeel");
            RequireCurrentSupportProbe(
                frame.CurrentSupport.Toe,
                "Toe",
                frame.CurrentSupport.WorldRevision,
                "CurrentSupportToe");
            RequireSupportTarget(
                frame.CurrentSupport.Target,
                frame.Side,
                "CurrentSupportTarget");
            bool available = frame.CurrentSupport.RejectReason == "None" &&
                             frame.CurrentSupport.Heel.Accepted &&
                             frame.CurrentSupport.Toe.Accepted &&
                             frame.CurrentSupport.Target.Available;
            if (frame.CurrentSupport.Frame != (ulong)frame.Frame ||
                frame.CurrentSupport.Completion != frame.CompletionIdentity ||
                frame.CurrentSupport.WorldRevision == 0 ||
                frame.CurrentSupport.Available != available ||
                !float.IsFinite(
                    frame.CurrentSupport.HeelRequiredDisplacement) ||
                !float.IsFinite(
                    frame.CurrentSupport.ToeRequiredDisplacement) ||
                !float.IsFinite(frame.CurrentSupport.SelectionEpsilon) ||
                frame.CurrentSupport.SelectionEpsilon <= 0f ||
                available &&
                    (!FiniteVector(
                         frame.CurrentSupport.SelectedNormalBeforeNormalization) ||
                     frame.CurrentSupport.SelectedNormalBeforeNormalization
                         .sqrMagnitude <=
                     RuntimeGeometryEpsilon * RuntimeGeometryEpsilon))
            {
                throw new InvalidDataException(
                    "Foot Motion Current Support facts are inconsistent.");
            }
            if (available)
            {
                RequireEnum<CharacterFootCurrentSupportProbeKind>(
                    frame.CurrentSupport.SelectedProbe,
                    "CurrentSupportSelectedProbe");
                RequireEnum<CharacterFootCurrentSupportSelectionReason>(
                    frame.CurrentSupport.SelectionReason,
                    "CurrentSupportSelectionReason");
            }
        }

        static void RequirePreparedAndSelectedTarget(FootFrame frame)
        {
            RequireSupportTarget(
                frame.SelectedSupportTarget,
                frame.Side,
                "SelectedSupportTarget");
            if (frame.ApproachPlantTargetPrepared &&
                (!frame.PreparedTargetAvailable ||
                 frame.PreparedTargetEventIdentity == 0 ||
                 frame.PreparedTargetSurfaceIdentity == 0 ||
                 frame.PreparedTargetTrajectoryGeneration == 0 ||
                 string.IsNullOrWhiteSpace(
                     frame.PreparedTargetFutureBodySource) ||
                 !FiniteVector(frame.PreparedTargetPoint) ||
                 !FiniteVector(frame.PreparedTargetNormal) ||
                 frame.PreparedTargetNormal.sqrMagnitude <=
                 RuntimeGeometryEpsilon * RuntimeGeometryEpsilon))
            {
                throw new InvalidDataException(
                    "Foot Motion prepared Approach target facts are inconsistent.");
            }
            if (frame.PlantInterpolationEvaluated &&
                !frame.SelectedSupportTarget.Available)
            {
                throw new InvalidDataException(
                    "Foot Motion evaluated interpolation lacks a selected Support Target.");
            }
        }

        static bool ContactWorldResponse(FootFrame frame) =>
            frame.CorrectionResponseEvaluated && frame.CorrectionResponseDomain == "ContactWorldResidual";

        static bool ScalarResponseEvaluated(FootFrame frame) =>
            frame.CorrectionResponseEvaluated && frame.CorrectionResponseDomain == "AnimationRelativeScalar";

        static bool ExitingContactResponse(FootFrame frame) =>
            ScalarResponseEvaluated(frame) && frame.CorrectionResponseDomainTransferred &&
            frame.CorrectionResponsePreviousDomain == "ContactWorldResidual";

        static double? ScalarResponseValue(FootFrame frame, float value) =>
            ScalarResponseEvaluated(frame) ? (double?)value : null;

        static Vector3 ExpectedResponseOutput(FootFrame frame) =>
            ContactWorldResponse(frame) ? frame.DesiredOutputPoint :
                frame.DesiredOutputPoint + frame.CorrectionResponseDirection *
                (frame.CorrectionResponseCurrent - frame.CorrectionResponseDesired);

        static CharacterFootResponseDomainFact ResponseDomainFact(FootFrame frame) => new
            CharacterFootResponseDomainFact
            {
                domain = frame.CorrectionResponseDomain,
                previousDomain = frame.CorrectionResponsePreviousDomain,
                transferred = frame.CorrectionResponseDomainTransferred,
                scalarEvaluated = ScalarResponseEvaluated(frame),
                contactResidualEvaluated = ContactWorldResponse(frame)
            };

        static void ApplyResponseDomainMetrics(
            FootFrame frame, SortedDictionary<string, double> metrics)
        {
            if (ScalarResponseEvaluated(frame))
                return;
            metrics.Remove("CorrectionResponseDesired");
            metrics.Remove("CorrectionResponsePrevious");
            metrics.Remove("CorrectionResponseCurrent");
            metrics.Remove("CorrectionResponseSelectedSpeed");
            metrics.Remove("CorrectionResponseAppliedDelta");
        }

        static void RequireResponseDomain(FootFrame frame)
        {
            RequireEnum<CharacterFootCorrectionResponseDomain>(frame.CorrectionResponseDomain,
                "FootMotionCorrectionResponseDomain");
            RequireEnum<CharacterFootCorrectionResponseDomain>(frame.CorrectionResponsePreviousDomain,
                "FootMotionCorrectionResponsePreviousDomain");
            bool evaluated = frame.CorrectionResponseEvaluated;
            bool initialized = frame.CorrectionResponseInitializedBefore;
            bool contact = ContactWorldResponse(frame);
            bool exiting = ExitingContactResponse(frame);
            bool transferExpected = evaluated && initialized &&
                frame.CorrectionResponsePreviousDomain != frame.CorrectionResponseDomain;
            bool valid = frame.CorrectionResponseDomainTransferred == transferExpected &&
                (evaluated ? frame.CorrectionResponseDomain != "None" : frame.CorrectionResponseDomain == "None") &&
                ((evaluated && initialized) == (frame.CorrectionResponsePreviousDomain != "None"));
            if (evaluated)
            {
                bool verifiedSupport = frame.InterpolationPolicy == "VerifiedSupport";
                valid &= contact == verifiedSupport &&
                    (!initialized || frame.PreviousResponseOutputAvailable) &&
                    FiniteVector(frame.DesiredOutputPoint) && FiniteVector(frame.ResponseOutputPoint) &&
                    FiniteVector(frame.PreviousResponseOutputPoint) &&
                    Vector3.Distance(frame.ResponseOutputPoint, ExpectedResponseOutput(frame)) <= PositionNoiseFloor;
                if (contact)
                {
                    valid &= frame.PlantInterpolationEvaluated && frame.PlantTargetVerified &&
                        (frame.PlantTargetKind == "VerifiedAnchor" || frame.PlantTargetKind == "LockedFullAnchor" ||
                         frame.PlantTargetKind == "LockedSliding") &&
                        !frame.CorrectionResponseVisibleOutputTransferred &&
                        frame.CorrectionResponseDesired == 0f && frame.CorrectionResponseBeforeRebase == 0f &&
                        frame.CorrectionResponsePrevious == 0f && frame.CorrectionResponseCurrent == 0f &&
                        frame.CorrectionResponseSelectedSpeed == 0f && frame.CorrectionResponseAppliedDelta == 0f &&
                        frame.CorrectionResponseDeltaDirection == "None" &&
                        Vector3.Distance(frame.DesiredOutputPoint,
                            frame.PlantSelectedWorldTarget + frame.PlantWorldResidualAfterDecay) <= PositionNoiseFloor;
                    if (frame.PlantResidualCaptureReason != "None" && frame.PreviousResponseOutputAvailable)
                        valid &= Vector3.Distance(frame.PlantWorldResidualCapturedBeforeDecay,
                            frame.PreviousResponseOutputPoint - frame.PlantSelectedWorldTarget) <= RuntimeGeometryEpsilon;
                }
                else
                {
                    valid &= !frame.PlantInterpolationEvaluated &&
                        (frame.InterpolationPolicy == "SwingResidual" || frame.InterpolationPolicy == "ReleaseResidual");
                    float desired = Vector3.Dot(frame.DesiredOutputPoint - frame.OriginalSole,
                        frame.CorrectionResponseDirection);
                    float previous = exiting ? desired : frame.CorrectionResponseVisibleOutputTransferred
                        ? Vector3.Dot(frame.PreviousResponseOutputPoint - frame.OriginalSole,
                            frame.CorrectionResponseDirection) : frame.CorrectionResponseBeforeRebase;
                    float delta = desired - previous;
                    bool advance = initialized && !exiting;
                    string direction = !advance || delta == 0f ? "None" : delta > 0f ? "Increase" : "Decrease";
                    float speed = direction == "None" ? 0f : direction == "Increase"
                        ? ExpectedCorrectionResponseIncreaseSpeed : ExpectedCorrectionResponseDecreaseSpeed;
                    float applied = advance ? Mathf.Clamp(delta, -speed * frame.DeltaSeconds,
                        speed * frame.DeltaSeconds) : 0f;
                    valid &= (!frame.CorrectionResponseVisibleOutputTransferred || frame.PreviousResponseOutputAvailable) &&
                        Math.Abs(frame.CorrectionResponseDesired - desired) <= PositionNoiseFloor &&
                        Math.Abs(frame.CorrectionResponsePrevious - previous) <= PositionNoiseFloor &&
                        Math.Abs(frame.CorrectionResponseCurrent - previous - applied) <= PositionNoiseFloor &&
                        frame.CorrectionResponseDeltaDirection == direction &&
                        Math.Abs(frame.CorrectionResponseSelectedSpeed - speed) <= TimeEpsilon &&
                        Math.Abs(frame.CorrectionResponseAppliedDelta - applied) <= PositionNoiseFloor &&
                        (initialized || Math.Abs(frame.CorrectionResponseBeforeRebase - desired) <= PositionNoiseFloor);
                    if (exiting)
                    {
                        Vector3 captured = frame.PreviousResponseOutputPoint - frame.OriginalSole - frame.StateTargetCorrection;
                        Vector3 expectedDesired = frame.OriginalSole + frame.StateTargetCorrection +
                            AdvanceResidual(captured, frame.DeltaSeconds, frame.ResidualBaseHalfLifeSeconds);
                        valid &= frame.PreviousResponseOutputAvailable && !frame.CorrectionResponseVisibleOutputTransferred &&
                            frame.CorrectionResponseBeforeRebase == 0f && frame.CorrectionResponseAppliedDelta == 0f &&
                            frame.CorrectionResponseSelectedSpeed == 0f &&
                            frame.InterpolationPolicy == "ReleaseResidual" &&
                            frame.PreTransitionTarget == "Releasing" && frame.PreTransitionSource != "Releasing" &&
                            frame.ResidualBaseHalfLifeSeconds > 0f &&
                            Vector3.Distance(frame.DesiredOutputPoint, expectedDesired) <= RuntimeGeometryEpsilon;
                    }
                }
            }
            if (!valid)
                throw new InvalidDataException(
                    $"Foot Motion Correction Response domain is inconsistent Frame={frame.Frame} Side={frame.Side} " +
                    $"Domain={frame.CorrectionResponseDomain} Previous={frame.CorrectionResponsePreviousDomain} " +
                    $"Transferred={frame.CorrectionResponseDomainTransferred} Policy={frame.InterpolationPolicy}.");
        }

        static void RequireResponseDomainHistory(List<FootFrame> frames)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) || previous.ProfileRevision != current.ProfileRevision ||
                    previous.ProgramIdentity != current.ProgramIdentity || previous.ProjectionRevision != current.ProjectionRevision ||
                    !previous.CorrectionResponseEvaluated || !current.CorrectionResponseEvaluated ||
                    !current.CorrectionResponseInitializedBefore)
                    continue;
                bool valid = current.CorrectionResponsePreviousDomain == previous.CorrectionResponseDomain;
                if (!current.CorrectionResponseVisibleOutputTransferred)
                    valid &= current.PreviousResponseOutputAvailable && Vector3.Distance(
                        current.PreviousResponseOutputPoint, previous.ResponseOutputPoint) <= RuntimeGeometryEpsilon;
                if (ScalarResponseEvaluated(current) && !ExitingContactResponse(current))
                    valid &= Math.Abs(current.CorrectionResponseBeforeRebase - previous.CorrectionResponseCurrent) <= PositionNoiseFloor;
                if (ContactWorldResponse(current) && ContactWorldResponse(previous) &&
                    current.PlantResidualCaptureReason == "None" &&
                    current.PlantTargetEventIdentity == previous.PlantTargetEventIdentity)
                    valid &= Vector3.Distance(current.PlantWorldResidualBeforeCapture,
                        previous.PlantWorldResidualAfterDecay) <= RuntimeGeometryEpsilon;
                if (!valid)
                    throw new InvalidDataException(
                        $"Foot Motion committed Response domain history is inconsistent Frame={current.Frame} Side={current.Side}.");
            }
        }

        static void RequireCorrectionResponseDirectionHistory(FootFrame frame)
        {
            if (!frame.CorrectionResponseEvaluated)
                return;
            Vector3 requested =
                frame.CorrectionResponseRequestedDirection.normalized;
            bool initialized = frame.CorrectionResponseInitializedBefore;
            float rawAngle = initialized
                ? DirectionAngleDegrees(
                    frame.CorrectionResponsePreviousDirection,
                    requested)
                : 0f;
            bool limited = frame.CorrectionResponseDirectionLimited;
            bool directionLimitFlagConsistent = initialized
                ? limited
                    ? rawAngle >=
                      frame.CorrectionResponseMaximumDirectionChangeDegrees -
                      DirectionComparisonEpsilonDegrees
                    : rawAngle <=
                      frame.CorrectionResponseMaximumDirectionChangeDegrees +
                      DirectionComparisonEpsilonDegrees
                : !limited;
            Vector3 applied = limited
                ? RotateDirectionTowards(
                    frame.CorrectionResponsePreviousDirection,
                    requested,
                    frame.CorrectionResponseMaximumDirectionChangeDegrees)
                : requested;
            float appliedAngle = initialized
                ? DirectionAngleDegrees(
                    frame.CorrectionResponsePreviousDirection,
                    applied)
                : 0f;
            bool initializedThisFrame = !initialized;
            if (!FiniteVector(frame.CorrectionResponseRequestedDirection) ||
                !FiniteVector(frame.CorrectionResponsePreviousDirection) ||
                !FiniteVector(frame.CorrectionResponseDirection) ||
                frame.CorrectionResponseRequestedDirection.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                frame.CorrectionResponseDirection.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.CorrectionResponseRequestedDirection.magnitude -
                    1f) > RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.CorrectionResponseDirection.magnitude - 1f) >
                    RuntimeGeometryEpsilon ||
                !float.IsFinite(
                    frame.CorrectionResponseMaximumDirectionChangeDegrees) ||
                frame.CorrectionResponseMaximumDirectionChangeDegrees <= 0f ||
                frame.CorrectionResponseMaximumDirectionChangeDegrees > 180f ||
                !directionLimitFlagConsistent ||
                Vector3.Distance(
                    frame.CorrectionResponseDirection,
                    applied) > RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.CorrectionResponseAppliedDirectionChangeDegrees -
                    appliedAngle) > RotationNoiseFloorDegrees ||
                frame.CorrectionResponseInitializedThisFrame !=
                    initializedThisFrame ||
                initializedThisFrame &&
                    (frame.CorrectionResponseInitializationReason == "None" ||
                     Vector3.Distance(
                         frame.CorrectionResponsePreviousDirection,
                         requested) > RuntimeGeometryEpsilon) ||
                !initializedThisFrame &&
                    frame.CorrectionResponseInitializationReason != "None" ||
                frame.SelectedSupportTarget.Available &&
                    Vector3.Distance(
                        frame.SelectedSupportTarget.Normal,
                        applied) > RuntimeGeometryEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Correction Response Direction History is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side} " +
                    $"RawAngle={rawAngle:R} AppliedAngle={appliedAngle:R} " +
                    $"Maximum={frame.CorrectionResponseMaximumDirectionChangeDegrees:R}.");
            }
        }

        static void RequireLifecycleTransitionFacts(FootFrame frame)
        {
            ContactAnchorFrame previousAnchor =
                ContactAnchorFrame.From(frame, true);
            ContactAnchorFrame currentAnchor =
                ContactAnchorFrame.From(frame, false);
            previousAnchor.RequireValid(frame, "Previous");
            currentAnchor.RequireValid(frame, "Current");
            RequireTransitionExecution(frame);
            RequireEnum<AnimationFootStepObservationLockMode>(
                frame.PreviousLockRequestMode,
                "FootMotionPreviousLockRequestMode");
            RequireEnum<AnimationFootStepObservationLockMode>(
                frame.CurrentLockRequestMode,
                "FootMotionCurrentLockRequestMode");
            RequireEnum<CharacterFootLockRequestAvailability>(
                frame.CurrentLockRequestAvailability,
                "FootMotionCurrentLockRequestAvailability");
            RequireEnum<CharacterFootContactEdge>(
                frame.ContactEdge,
                "FootMotionContactEdge");
            RequireFlags<CharacterFootGoalOwnershipLossReason>(
                frame.HardOwnershipLossReason,
                "FootMotionHardOwnershipLossReason");
            Enum.TryParse(
                frame.CurrentLockRequestMode,
                out AnimationFootStepObservationLockMode currentMode);
            Enum.TryParse(
                frame.CurrentLockRequestAvailability,
                out CharacterFootLockRequestAvailability availability);
            Enum.TryParse(
                frame.ContactEdge,
                out CharacterFootContactEdge edge);
            Enum.TryParse(
                frame.HardOwnershipLossReason,
                out CharacterFootGoalOwnershipLossReason ownershipReason);
            bool formalRequestsLock = frame.FormalRequestContact > 0f &&
                frame.FormalLockMode !=
                AnimationFootStepObservationLockMode.Unlocked.ToString();
            CharacterFootLockRequestAvailability expectedAvailability =
                formalRequestsLock &&
                frame.InputEvents.Current.Identity == 0
                    ? CharacterFootLockRequestAvailability
                        .ContactEventUnavailable
                    : CharacterFootLockRequestAvailability.Ready;
            bool expectedCurrentRequested =
                expectedAvailability ==
                    CharacterFootLockRequestAvailability.Ready &&
                formalRequestsLock;
            CharacterFootContactEdge expectedEdge =
                !frame.PreviousLockRequestAvailable
                    ? expectedCurrentRequested
                        ? CharacterFootContactEdge.Rising
                        : CharacterFootContactEdge.None
                    : expectedCurrentRequested
                        ? !frame.PreviousLockRequested
                            ? CharacterFootContactEdge.Rising
                            : frame.CurrentLockRequestEventIdentity !=
                              frame.PreviousLockRequestEventIdentity
                                ? CharacterFootContactEdge.EventChanged
                                : CharacterFootContactEdge.None
                        : frame.PreviousLockRequested
                            ? CharacterFootContactEdge.Falling
                            : CharacterFootContactEdge.None;
            float expectedSeconds = expectedEdge ==
                                    CharacterFootContactEdge.None
                ? frame.PreviousContactEdgeSeconds + frame.DeltaSeconds
                : 0f;
            ulong expectedLatestContact =
                frame.PreviousLatestContactEventIdentity;
            ulong expectedLatestReleased =
                frame.PreviousLatestReleasedContactEventIdentity;
            if (expectedEdge == CharacterFootContactEdge.Falling ||
                expectedEdge == CharacterFootContactEdge.EventChanged)
            {
                expectedLatestReleased =
                    frame.PreviousLockRequestEventIdentity;
            }
            if (expectedEdge == CharacterFootContactEdge.Rising ||
                expectedEdge == CharacterFootContactEdge.EventChanged)
            {
                expectedLatestContact =
                    frame.CurrentLockRequestEventIdentity;
            }
            ulong expectedCompleted =
                frame.PreviousCompletedLockWeightEventIdentity;
            if (frame.CurrentLockRequestEventIdentity != 0 &&
                expectedCompleted != 0 &&
                expectedCompleted != frame.CurrentLockRequestEventIdentity)
            {
                expectedCompleted = 0;
            }
            if (expectedCurrentRequested &&
                frame.CurrentLockRequestEventIdentity != 0 &&
                frame.CurrentLockRequestWeight >=
                1f - RuntimeGeometryEpsilon)
            {
                expectedCompleted = frame.CurrentLockRequestEventIdentity;
            }
            bool expectedAnchorAvailable =
                frame.PreviousContactAnchorAvailable;
            ulong expectedAnchorEvent =
                frame.PreviousContactAnchorEventIdentity;
            ApplyAnchorCommand(
                frame.PreTransitionAnchorCommand,
                frame.CurrentLockRequestEventIdentity,
                ref expectedAnchorAvailable,
                ref expectedAnchorEvent,
                ref expectedCompleted);
            if (frame.PostTransitionEvaluated)
            {
                ApplyAnchorCommand(
                    frame.PostTransitionAnchorCommand,
                    frame.CurrentLockRequestEventIdentity,
                    ref expectedAnchorAvailable,
                    ref expectedAnchorEvent,
                    ref expectedCompleted);
            }
            bool expectedReentryRefreshed =
                frame.PreTransitionReason ==
                "SameEventContactReentryRefresh";
            bool expectedReentryUnavailable =
                frame.PreTransitionReason == "ContactUnavailable" &&
                expectedCurrentRequested &&
                frame.CurrentLockRequestEventIdentity != 0 &&
                frame.CurrentLockRequestEventIdentity ==
                    frame.PreviousLatestReleasedContactEventIdentity &&
                !frame.PreviousContactAnchorAvailable;
            bool expectedRetained =
                frame.PreviousContactAnchorAvailable &&
                frame.CurrentContactAnchorAvailable &&
                frame.PreviousContactAnchorEventIdentity ==
                    frame.CurrentContactAnchorEventIdentity &&
                frame.PreTransitionAnchorCommand != "Create" &&
                frame.PreTransitionAnchorCommand != "Release" &&
                (!frame.PostTransitionEvaluated ||
                 frame.PostTransitionAnchorCommand != "Create" &&
                 frame.PostTransitionAnchorCommand != "Release");
            bool expectedReentryHistoryRetained =
                expectedReentryRefreshed && expectedRetained &&
                !frame.PreTransitionSuppressOutput &&
                !frame.PreTransitionResetInterpolation &&
                (!frame.PostTransitionEvaluated ||
                 !frame.PostTransitionSuppressOutput &&
                 !frame.PostTransitionResetInterpolation);
            if (expectedRetained && !previousAnchor.SameAs(currentAnchor))
            {
                throw new InvalidDataException(
                    $"Foot Motion retained Anchor geometry or acquisition identity changed " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
            if (expectedReentryRefreshed &&
                (!expectedReentryHistoryRetained ||
                 !frame.CurrentLockRequested ||
                 frame.ContactEdge != "Rising" ||
                 frame.PreTransitionSource != "Releasing" ||
                 frame.PreTransitionTarget != "Landing" ||
                 frame.PreTransitionAnchorCommand != "Retain" ||
                 frame.CurrentLockRequestEventIdentity !=
                 frame.PreviousContactAnchorEventIdentity))
            {
                throw new InvalidDataException(
                    $"Foot Motion same-event Reentry history is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
            bool anchorCreated = frame.PreTransitionAnchorCommand == "Create" ||
                frame.PostTransitionEvaluated &&
                frame.PostTransitionAnchorCommand == "Create";
            if (anchorCreated && currentAnchor.Available &&
                (currentAnchor.AcquiredFrame != (ulong)frame.Frame ||
                 currentAnchor.AcquiredCompletion != frame.CompletionIdentity))
            {
                throw new InvalidDataException(
                    $"Foot Motion created Anchor acquisition identity is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
            CharacterFootGoalOwnershipLossReason expectedOwnershipReason =
                CharacterFootGoalOwnershipLossReason.None;
            if (!frame.Grounded)
            {
                expectedOwnershipReason |=
                    CharacterFootGoalOwnershipLossReason.Ungrounded;
            }
            if (!frame.CurrentStep.IsAuthoritative)
            {
                expectedOwnershipReason |= CharacterFootGoalOwnershipLossReason
                    .SourceLineageInvalidated;
            }
            bool expectedHardOwnershipLoss = expectedOwnershipReason !=
                CharacterFootGoalOwnershipLossReason.None;
            bool preOwnershipTransition = frame.PreTransitionReason ==
                "OwnershipLost";
            bool actionFactsValid =
                float.IsFinite(frame.ActionFootWeight) &&
                frame.ActionFootWeight >= 0f &&
                frame.ActionFootWeight <= 1f &&
                (frame.ActionInstanceIdentity == 0
                    ? frame.ActionFootWeight == 0f
                    : frame.ActionFootWeight > RuntimeGeometryEpsilon);
            bool consistent =
                frame.LifecycleTransitionEvaluated &&
                float.IsFinite(frame.PreviousLockRequestWeight) &&
                frame.PreviousLockRequestWeight >= 0f &&
                frame.PreviousLockRequestWeight <= 1f &&
                float.IsFinite(frame.CurrentLockRequestWeight) &&
                frame.CurrentLockRequestWeight >= 0f &&
                frame.CurrentLockRequestWeight <= 1f &&
                float.IsFinite(frame.PreviousContactEdgeSeconds) &&
                frame.PreviousContactEdgeSeconds >= 0f &&
                float.IsFinite(frame.CurrentContactEdgeSeconds) &&
                frame.CurrentContactEdgeSeconds >= 0f &&
                actionFactsValid &&
                currentMode.ToString() == frame.FormalLockMode &&
                Math.Abs(
                    frame.CurrentLockRequestWeight -
                    frame.FormalLockWeight) <= TimeEpsilon &&
                frame.CurrentLockRequestEventIdentity ==
                    frame.InputEvents.Current.Identity &&
                availability == expectedAvailability &&
                frame.CurrentLockRequested == expectedCurrentRequested &&
                edge == expectedEdge &&
                Math.Abs(
                    frame.CurrentContactEdgeSeconds - expectedSeconds) <=
                    TimeEpsilon &&
                frame.CurrentLatestContactEventIdentity ==
                    expectedLatestContact &&
                frame.CurrentLatestReleasedContactEventIdentity ==
                    expectedLatestReleased &&
                frame.CurrentCompletedLockWeightEventIdentity ==
                    expectedCompleted &&
                frame.CurrentContactAnchorAvailable ==
                    expectedAnchorAvailable &&
                frame.CurrentContactAnchorEventIdentity ==
                    expectedAnchorEvent &&
                frame.SameEventContactReentryRefreshed ==
                    expectedReentryRefreshed &&
                frame.SameEventContactReentryUnavailable ==
                    expectedReentryUnavailable &&
                frame.RetainedVerifiedAnchor == expectedRetained &&
                frame.ReentryInterpolationHistoryRetained ==
                    expectedReentryHistoryRetained &&
                ownershipReason == expectedOwnershipReason &&
                frame.HardOwnershipLoss == expectedHardOwnershipLoss &&
                preOwnershipTransition == expectedHardOwnershipLoss &&
                frame.PreTransitionSuppressOutput ==
                    expectedHardOwnershipLoss &&
                frame.PreTransitionResetInterpolation ==
                    expectedHardOwnershipLoss &&
                !frame.PostTransitionSuppressOutput &&
                frame.PostTransitionResetInterpolation ==
                    (frame.PostTransitionEvaluated &&
                     frame.PostTransitionReason == "ReleaseCompleted");
            if (!consistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion Lifecycle Transition facts are inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side} " +
                    $"Edge={frame.ContactEdge}/{expectedEdge} " +
                    $"Ownership={frame.HardOwnershipLossReason}/" +
                    $"{expectedOwnershipReason}.");
            }
        }

        static void RequireTransitionExecution(FootFrame frame)
        {
            RequireEnum<CharacterFootTransitionReason>(
                frame.PreTransitionReason, "FootMotionPreTransitionReason");
            RequireEnum<CharacterFootConstraintState>(
                frame.PreTransitionSource, "FootMotionPreTransitionSource");
            RequireEnum<CharacterFootConstraintState>(
                frame.PreTransitionTarget, "FootMotionPreTransitionTarget");
            RequireEnum<CharacterFootAnchorCommand>(
                frame.PreTransitionAnchorCommand,
                "FootMotionPreTransitionAnchorCommand");
            if (frame.ConstraintStateBefore != frame.PreTransitionSource)
                throw new InvalidDataException(
                    $"Foot Motion Lifecycle State Before is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            if (!frame.PostTransitionEvaluated)
            {
                if (frame.PostTransitionReason != "None" ||
                    frame.PostTransitionSource != "Swing" ||
                    frame.PostTransitionTarget != "Swing" ||
                    frame.PostTransitionAnchorCommand != "None" ||
                    frame.PostTransitionSuppressOutput ||
                    frame.PostTransitionResetInterpolation ||
                    frame.PreTransitionSuppressOutput ||
                    (frame.Resolved.Outcome != "CurrentSupportUnavailable" &&
                     frame.Resolved.Outcome != "SupportTargetUnavailable"))
                {
                    throw new InvalidDataException(
                        $"Foot Motion unevaluated Post Transition is inconsistent " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
                return;
            }
            RequireEnum<CharacterFootTransitionReason>(
                frame.PostTransitionReason, "FootMotionPostTransitionReason");
            RequireEnum<CharacterFootConstraintState>(
                frame.PostTransitionSource, "FootMotionPostTransitionSource");
            RequireEnum<CharacterFootConstraintState>(
                frame.PostTransitionTarget, "FootMotionPostTransitionTarget");
            RequireEnum<CharacterFootAnchorCommand>(
                frame.PostTransitionAnchorCommand,
                "FootMotionPostTransitionAnchorCommand");
            if (frame.PostTransitionSource != frame.PreTransitionTarget ||
                frame.Resolved.Outcome == "Ready" &&
                frame.ConstraintState != frame.PostTransitionTarget)
            {
                throw new InvalidDataException(
                    $"Foot Motion executed Post Transition State is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
        }

        static void RequireFormalGoalWeights(FootFrame frame)
        {
            float formal = frame.FormalFootPlacementWeight;
            if (!float.IsFinite(formal) || formal < 0f || formal > 1f)
                throw new InvalidDataException(
                    $"Foot Motion Formal Foot Placement Weight is invalid " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            float expectedRotation = 0f;
            float expectedPosition = 0f;
            if (frame.Resolved.Outcome == "Ready")
            {
                expectedRotation = frame.CurrentContactAnchorAvailable
                    ? formal * frame.CurrentLockRequestWeight
                    : 0f;
                expectedPosition = formal;
                if (frame.Resolved.ContactAvailable !=
                    frame.CurrentContactAnchorAvailable ||
                    frame.Resolved.ContactAvailable &&
                    (frame.Resolved.ContactEventIdentity !=
                     frame.CurrentContactAnchorEventIdentity ||
                     Vector3.Distance(frame.Resolved.ContactPoint,
                         frame.CurrentContactAnchorPoint) > PositionNoiseFloor))
                {
                    throw new InvalidDataException(
                        $"Foot Motion Resolved Contact does not match Lifecycle Anchor " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
                }
            }
            bool hasGoal = frame.Resolved.Outcome == "Ready" &&
                (expectedPosition > RuntimeGeometryEpsilon ||
                 expectedRotation > RuntimeGeometryEpsilon);
            float expectedGoalPosition = hasGoal ? expectedPosition : 0f;
            float expectedGoalRotation = hasGoal ? expectedRotation : 0f;
            if (Math.Abs(frame.MotionPositionWeight - expectedPosition) >
                    TimeEpsilon ||
                Math.Abs(frame.MotionRotationWeight - expectedRotation) >
                    TimeEpsilon ||
                Math.Abs(frame.Resolved.PositionWeight - expectedPosition) >
                    TimeEpsilon ||
                Math.Abs(frame.Resolved.RotationWeight - expectedRotation) >
                    TimeEpsilon ||
                Math.Abs(frame.FinalGoalPositionWeight -
                    expectedGoalPosition) > TimeEpsilon ||
                Math.Abs(frame.FinalGoalRotationWeight -
                    expectedGoalRotation) > TimeEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Formal Goal weight policy is inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side} " +
                    $"Formal={formal:R} Position={expectedPosition:R} " +
                    $"Rotation={expectedRotation:R}.");
            }
        }

        static void RequireFootGoalComponentFacts(FootFrame frame)
        {
            Vector3 target = Vector3.Lerp(
                frame.FinalIkLegOriginalAnkle, frame.EncodedGoalPosition, frame.FinalGoalPositionWeight);
            bool physicalAvailable = frame.FinalPhysicalWriteAvailable && frame.FinalIkLegAvailable &&
                frame.FinalGoalPositionWeight > 0f;
            float expectedResidual = physicalAvailable
                ? Vector3.Distance(frame.FinalPhysicalAnkleComponentPosition,
                    frame.FinalIkLegOriginalAnkle +
                    (frame.EncodedGoalPosition - frame.FinalIkLegOriginalAnkle) * frame.FinalGoalPositionWeight)
                : 0f;
            if (frame.FinalIkLegAvailable &&
                    Vector3.Distance(frame.FinalIkLegTargetAnkle, target) > PositionNoiseFloor ||
                frame.FinalPhysicalAnkleGoalResidual < 0f ||
                Math.Abs(frame.FinalPhysicalAnkleGoalResidual - expectedResidual) > PositionNoiseFloor ||
                physicalAvailable && frame.FinalPhysicalWriteCompletionIdentity != frame.CompletionIdentity)
                throw new InvalidDataException(
                    $"Foot Motion component Goal and physical residual facts are inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side} Residual={frame.FinalPhysicalAnkleGoalResidual:R}/{expectedResidual:R}.");
        }

        static void ApplyAnchorCommand(
            string command,
            ulong eventIdentity,
            ref bool available,
            ref ulong anchorEventIdentity,
            ref ulong completedLockWeightEventIdentity)
        {
            switch (command)
            {
                case "None":
                case "Retain":
                    return;
                case "Create":
                    available = true;
                    anchorEventIdentity = eventIdentity;
                    return;
                case "Release":
                    available = false;
                    anchorEventIdentity = 0;
                    completedLockWeightEventIdentity = 0;
                    return;
                default:
                    throw new InvalidDataException(
                        $"Foot Motion Anchor command '{command}' is invalid.");
            }
        }

        static void RequireResolvedFoot(FootFrame frame)
        {
            RequireEnum<CharacterFootResolvedOutcome>(
                frame.Resolved.Outcome,
                "ResolvedOutcome");
            RequireSupportTarget(
                frame.Resolved.SupportTarget,
                frame.Side,
                "ResolvedSupportTarget");
            if (frame.Resolved.Frame != (ulong)frame.Frame ||
                frame.Resolved.Completion != frame.CompletionIdentity ||
                frame.Resolved.Side != frame.Side ||
                string.IsNullOrWhiteSpace(frame.ProfileId) ||
                string.IsNullOrWhiteSpace(frame.ProfileRevision) ||
                string.IsNullOrWhiteSpace(frame.Resolved.RigId) ||
                string.IsNullOrWhiteSpace(frame.Resolved.RigRevision) ||
                !FiniteVector(frame.Resolved.FinalSole) ||
                !FiniteVector(frame.Resolved.EffectiveSole) ||
                !FiniteVector(frame.Resolved.GoalTargetAnkle) ||
                !FiniteVector(frame.Resolved.EffectiveAnkle) ||
                !FiniteVector(frame.Resolved.EffectiveHeel) ||
                !FiniteVector(frame.Resolved.EffectiveToe) ||
                !FiniteVector(frame.Resolved.EffectiveSoleFromContacts) ||
                !FiniteVector(frame.Resolved.SourceSoleForward) ||
                !FiniteRotation(frame.Resolved.GoalTargetRotation) ||
                !FiniteRotation(frame.Resolved.EffectiveRotation) ||
                !FiniteRotation(frame.Resolved.SourceSoleFrameLocalRotation) ||
                !float.IsFinite(frame.Resolved.PositionWeight) ||
                frame.Resolved.PositionWeight < 0f ||
                frame.Resolved.PositionWeight > 1f ||
                !float.IsFinite(frame.Resolved.RotationWeight) ||
                frame.Resolved.RotationWeight < 0f ||
                frame.Resolved.RotationWeight > 1f ||
                Vector3.Distance(
                    frame.Resolved.EffectiveSoleFromContacts,
                    (frame.Resolved.EffectiveHeel + frame.Resolved.EffectiveToe) *
                    0.5f) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Motion Resolved facts are inconsistent.");
            }
            if (frame.Resolved.Outcome != "Ready")
            {
                if (frame.Resolved.SupportTarget.Available ||
                    frame.Resolved.PositionWeight != 0f ||
                    frame.Resolved.RotationWeight != 0f ||
                    frame.Resolved.GoalTargetCorrection.sqrMagnitude != 0f)
                {
                    throw new InvalidDataException(
                        "Foot Motion unavailable Resolved output is not zeroed.");
                }
                return;
            }
            if (!frame.CorrectionResponseEvaluated)
            {
                throw new InvalidDataException(
                    "Foot Motion Ready visible policy did not evaluate Correction Response exactly once.");
            }
            Vector3 expectedEffectiveSole = Vector3.LerpUnclamped(
                frame.OriginalSole,
                frame.Resolved.FinalSole,
                frame.Resolved.PositionWeight);
            Vector3 expectedEffectiveSoleCorrection =
                frame.Resolved.EffectiveSoleFromContacts - frame.OriginalSole;
            Vector3 forward = Vector3.ProjectOnPlane(
                frame.Resolved.SourceSoleForward,
                frame.Resolved.SupportTarget.Normal);
            if (forward.sqrMagnitude <=
                RuntimeGeometryEpsilon * RuntimeGeometryEpsilon)
            {
                throw new InvalidDataException(
                    "Foot Motion Resolved Sole forward is degenerate.");
            }
            Quaternion targetSoleRotation = LookRotation(
                forward.normalized,
                frame.Resolved.SupportTarget.Normal);
            Quaternion expectedGoalRotation = NormalizeRotation(
                MultiplyRotation(
                    targetSoleRotation,
                    InverseRotation(
                        frame.Resolved.SourceSoleFrameLocalRotation)));
            Quaternion expectedEffectiveRotation = SlerpRotation(
                frame.SourceAnkleRotation,
                expectedGoalRotation,
                frame.Resolved.RotationWeight);
            Quaternion rotationDelta = NormalizeRotation(
                MultiplyRotation(
                    expectedEffectiveRotation,
                    InverseRotation(frame.SourceAnkleRotation)));
            Vector3 expectedEffectiveAnkle = expectedEffectiveSole -
                RotateVector(
                    rotationDelta,
                    frame.OriginalSole - frame.OriginalAnkle);
            Vector3 expectedGoalAnkle = frame.Resolved.PositionWeight >
                                        RuntimeGeometryEpsilon
                ? frame.OriginalAnkle +
                  (expectedEffectiveAnkle - frame.OriginalAnkle) /
                  frame.Resolved.PositionWeight
                : frame.OriginalAnkle;
            if (!frame.Resolved.SupportTarget.Available ||
                Vector3.Distance(
                    frame.Resolved.GoalTargetCorrection,
                    frame.Resolved.FinalSole - frame.OriginalSole) >
                PositionNoiseFloor ||
                Vector3.Distance(
                    frame.Resolved.EffectiveSole,
                    expectedEffectiveSole) > PositionNoiseFloor ||
                Vector3.Distance(
                    frame.Resolved.EffectiveSoleCorrection,
                    expectedEffectiveSoleCorrection) > PositionNoiseFloor ||
                RotationAngleDegrees(
                    frame.Resolved.GoalTargetRotation,
                    expectedGoalRotation) > RotationNoiseFloorDegrees ||
                RotationAngleDegrees(
                    frame.Resolved.EffectiveRotation,
                    expectedEffectiveRotation) > RotationNoiseFloorDegrees ||
                Vector3.Distance(
                    frame.Resolved.EffectiveAnkle,
                    expectedEffectiveAnkle) > PositionNoiseFloor ||
                Vector3.Distance(
                    frame.Resolved.GoalTargetAnkle,
                    expectedGoalAnkle) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Motion Resolved weighted Sole-to-Ankle facts are inconsistent.");
            }
        }

        static bool PelvisClose(float actual, float expected) =>
            float.IsFinite(actual) && float.IsFinite(expected) &&
            Math.Abs(actual - expected) <= RuntimeGeometryEpsilon;

        static void RequirePelvis(bool valid, FootFrame frame, string part)
        {
            if (!valid)
                throw new InvalidDataException(
                    $"Foot Motion Pelvis {part} facts are inconsistent Frame={frame.Frame} Side={frame.Side}.");
        }

        static void RequirePelvisLeg(PelvisLegFrame leg, Vector3 up, FootFrame frame)
        {
            bool requested = leg.Role != CharacterFootPelvisLegReachRole.None;
            RequirePelvis(leg.Requested == requested &&
                leg.Available == (leg.Status == CharacterFootPelvisLegReachStatus.Available), frame, "leg availability");
            if (!requested)
            {
                RequirePelvis(leg.SameAs(new PelvisLegFrame()), frame, "unrequested leg");
                return;
            }
            RequirePelvis(leg.EventIdentity != 0 && FiniteVector(leg.Hip) && FiniteVector(leg.TargetAnkle) &&
                float.IsFinite(leg.LegLength) && leg.LegLength > 0f &&
                float.IsFinite(leg.MinimumCompressionReserve) && leg.MinimumCompressionReserve >= 0f &&
                leg.MinimumCompressionReserve < leg.LegLength &&
                PelvisClose(leg.UsableLegLength, leg.LegLength - leg.MinimumCompressionReserve), frame, "leg input");
            Vector3 difference = leg.Hip - leg.TargetAnkle;
            float vertical = Vector3.Dot(difference, up);
            float horizontalSquare = (difference - up * vertical).sqrMagnitude;
            float radius = leg.LegLength - leg.MinimumCompressionReserve;
            float square = radius * radius - horizontalSquare;
            RequirePelvis(float.IsFinite(square) && float.IsFinite(vertical), frame, "leg geometry");
            if (square < 0f)
            {
                RequirePelvis(leg.Status == CharacterFootPelvisLegReachStatus.HorizontalUnreachable &&
                    leg.MinimumAlongUp == 0f && leg.MaximumAlongUp == 0f, frame, "unreachable leg");
                return;
            }
            float reach = Mathf.Sqrt(square);
            RequirePelvis(leg.Status == CharacterFootPelvisLegReachStatus.Available &&
                PelvisClose(leg.MinimumAlongUp, -vertical - reach) &&
                PelvisClose(leg.MaximumAlongUp, -vertical + reach), frame, "leg interval");
        }

        static void RequirePelvisReach(FootFrame frame)
        {
            PelvisReachFrame reach = frame.Pelvis.Reach;
            bool specified = !reach.ComponentUp.Equals(Vector3.zero);
            RequirePelvis(FiniteVector(reach.ComponentUp) &&
                (!specified || Math.Abs(reach.ComponentUp.sqrMagnitude - 1f) <= RuntimeGeometryEpsilon) &&
                (specified || !frame.Pelvis.Response.Evaluated && !frame.PelvisHeightTarget.Available &&
                    !reach.Left.Requested && !reach.Right.Requested), frame, "reach axis");
            RequirePelvisLeg(reach.Left, reach.ComponentUp, frame);
            RequirePelvisLeg(reach.Right, reach.ComponentUp, frame);
            RequirePelvis(!(reach.Left.PrimarySupport && reach.Right.PrimarySupport), frame, "primary role");
            CharacterFootPelvisReachStatus status = CharacterFootPelvisReachStatus.NotRequested;
            bool intersectionEvaluated = false;
            float intersectionMinimum = 0f, intersectionMaximum = 0f;
            if (reach.Left.Requested || reach.Right.Requested)
            {
                if (reach.Left.Requested && !reach.Left.Available || reach.Right.Requested && !reach.Right.Available)
                    status = CharacterFootPelvisReachStatus.LegUnreachable;
                else
                {
                    PelvisLegFrame first = reach.Left.Requested ? reach.Left : reach.Right;
                    intersectionMinimum = first.MinimumAlongUp;
                    intersectionMaximum = first.MaximumAlongUp;
                    if (reach.Left.Requested && reach.Right.Requested)
                    {
                        intersectionMinimum = Mathf.Max(intersectionMinimum, reach.Right.MinimumAlongUp);
                        intersectionMaximum = Mathf.Min(intersectionMaximum, reach.Right.MaximumAlongUp);
                    }
                    intersectionEvaluated = true;
                    status = intersectionMinimum <= intersectionMaximum
                        ? CharacterFootPelvisReachStatus.Available : CharacterFootPelvisReachStatus.NoCommonInterval;
                }
            }
            RequirePelvis(reach.Status == status &&
                reach.IntersectionEvaluated == intersectionEvaluated &&
                PelvisClose(reach.IntersectionMinimumAlongUp, intersectionMinimum) &&
                PelvisClose(reach.IntersectionMaximumAlongUp, intersectionMaximum), frame, "reach observation");
            PelvisLegFrame leg = frame.Side == "Left" ? reach.Left : reach.Right;
            bool primaryExpected = frame.StrideState == "Accepted" && frame.StrideSupportSide == frame.Side &&
                frame.Resolved.PositionWeight > RuntimeGeometryEpsilon;
            RequirePelvis(leg.PrimarySupport == primaryExpected, frame, "primary observation role");
            if (leg.FootTarget)
                RequirePelvis(frame.Resolved.LandingReachAvailable &&
                    leg.EventIdentity == frame.Resolved.LandingReachEventIdentity &&
                    Vector3.Distance(leg.Hip, frame.Resolved.LandingReachHip) <= RuntimeGeometryEpsilon &&
                    Vector3.Distance(leg.TargetAnkle, frame.Resolved.LandingReachTargetAnkle) <= RuntimeGeometryEpsilon &&
                    PelvisClose(leg.LegLength, frame.Resolved.LandingReachLegLength) &&
                    PelvisClose(leg.MinimumCompressionReserve, frame.Resolved.LandingReachMinimumCompressionReserve), frame, "Foot request lineage");
            if (leg.PrimarySupport)
            {
                RequirePelvis(frame.PelvisHeightTarget.Available && frame.PrimarySupportAvailable &&
                    frame.PrimarySupportSide == frame.Side && frame.StrideSupportSide == frame.Side, frame, "primary lineage");
                if (!leg.FootTarget)
                    RequirePelvis(leg.EventIdentity == frame.PrimarySupportEventIdentity &&
                        Vector3.Distance(leg.Hip, frame.Pelvis.Posture.Hip) <= RuntimeGeometryEpsilon &&
                        Vector3.Distance(leg.TargetAnkle, frame.Pelvis.Posture.TargetAnkle) <= RuntimeGeometryEpsilon &&
                        PelvisClose(leg.LegLength, frame.Pelvis.Posture.LegLength), frame, "primary-only input");
            }
            float applied = frame.Pelvis.Response.Output * frame.Pelvis.Response.PositionWeight;
            bool footAvailable = leg.FootTarget && leg.Available &&
                applied >= leg.MinimumAlongUp - RuntimeGeometryEpsilon &&
                applied <= leg.MaximumAlongUp + RuntimeGeometryEpsilon;
            RequirePelvis(frame.LandingReachEvaluated == leg.FootTarget && frame.LandingReachAvailable == footAvailable,
                frame, "weighted Foot reach result");
        }

        static void RequirePelvisPosture(FootFrame frame)
        {
            const float endpoint = 0.005f;
            PelvisPostureFrame posture = frame.Pelvis.Posture;
            RequirePelvis(posture.Evaluated == frame.PelvisHeightTarget.Available, frame, "posture execution");
            if (!posture.Evaluated)
            {
                RequirePelvis(posture.SameAs(new PelvisPostureFrame()), frame, "unevaluated posture");
                return;
            }
            Vector3 up = frame.Pelvis.Reach.ComponentUp;
            RequirePelvis(up.Equals(frame.PelvisHeightTarget.ComponentUp) &&
                FiniteVector(posture.Hip) && FiniteVector(posture.AnimatedAnkle) && FiniteVector(posture.TargetAnkle) &&
                float.IsFinite(posture.LegLength) && posture.LegLength > endpoint &&
                PelvisClose(posture.CompressionReserve, Mathf.Max(0f,
                    posture.LegLength - Vector3.Distance(posture.Hip, posture.AnimatedAnkle))), frame, "posture input");
            Vector3 difference = posture.Hip - posture.TargetAnkle;
            float horizontalSquare = Vector3.ProjectOnPlane(difference, up).sqrMagnitude;
            float maximumLength = posture.LegLength - endpoint;
            float usable = 0f, minimum = 0f, maximum = 0f;
            bool available = float.IsFinite(horizontalSquare) && maximumLength > endpoint &&
                horizontalSquare < maximumLength * maximumLength;
            if (available)
            {
                float minimumLength = Mathf.Min(maximumLength, Mathf.Sqrt(horizontalSquare + endpoint * endpoint));
                usable = Mathf.Clamp(posture.LegLength - Mathf.Max(endpoint, posture.CompressionReserve),
                    minimumLength, maximumLength);
                available = float.IsFinite(usable) && usable > endpoint && horizontalSquare < usable * usable;
                if (available)
                {
                    float vertical = Vector3.Dot(difference, up);
                    float reach = Mathf.Sqrt(usable * usable - horizontalSquare);
                    minimum = -vertical - reach;
                    maximum = -vertical + reach;
                    available = float.IsFinite(minimum) && float.IsFinite(maximum) && minimum <= maximum;
                }
                else
                    usable = 0f;
            }
            float requested = frame.PelvisHeightTarget.RequestedOffsetAlongUp;
            float preferred = available ? Mathf.Clamp(requested, minimum, maximum) : requested;
            preferred = Mathf.Clamp(preferred, Mathf.Min(0f, requested), Mathf.Max(0f, requested));
            RequirePelvis(posture.Available == available && PelvisClose(posture.UsableLegLength, usable) &&
                PelvisClose(posture.MinimumAlongUp, minimum) && PelvisClose(posture.MaximumAlongUp, maximum) &&
                PelvisClose(posture.OffsetAlongUp, preferred) &&
                posture.TargetAdjusted == (Math.Abs(preferred - requested) > RuntimeGeometryEpsilon), frame, "posture preference");
        }

        static void RequirePelvisFacts(FootFrame frame)
        {
            RequireEnum<CharacterFootStrideState>(frame.StrideState, "StrideState");
            RequirePelvis(frame.PelvisHeightTarget.Available == (frame.StrideState == "Accepted"),
                frame, "height target execution");
            RequirePelvisObservation(frame);
            RequirePelvisReach(frame);
            RequirePelvisPosture(frame);
            PelvisResponseFrame response = frame.Pelvis.Response;
            PelvisReachFrame reach = frame.Pelvis.Reach;
            if (!response.Evaluated)
            {
                RequirePelvis(response.SameAs(new PelvisResponseFrame()) && !frame.PelvisHeightTarget.Available &&
                    frame.StrideState != "Releasing" &&
                    frame.StridePelvisDelta.Equals(Vector3.zero) && frame.PelvisWeight == 0f, frame, "unevaluated response");
                return;
            }
            bool releasing = !frame.PelvisHeightTarget.Available;
            RequirePelvis(releasing
                ? response.HadPreviousState &&
                  frame.StrideState == (response.Completed ? "Rejected" : "Releasing")
                : !response.Completed, frame, "response execution");
            float preferred = releasing ? 0f : frame.Pelvis.Posture.OffsetAlongUp;
            float target = preferred;
            RequirePelvis(float.IsFinite(response.Frequency) && response.Frequency > 0f &&
                PelvisClose(response.Input, response.PreviousOutput) &&
                (response.HadPreviousState || response.PreviousTarget == 0f && response.PreviousOutput == 0f &&
                    response.PreviousVelocity == 0f && response.PreviousSlope == CharacterFootStrideSlope.Flat &&
                    !response.SupportChanged), frame, "response history input");
            float previousDirection = response.PreviousTarget - response.PreviousOutput;
            float direction = target - response.PreviousOutput;
            bool crossed = !releasing && response.HadPreviousState && Math.Abs(previousDirection) > 0.005f &&
                Math.Abs(direction) > 0.005f && previousDirection * direction < 0f;
            bool slopeChanged = response.HadPreviousState && response.PreviousSlope !=
                (releasing ? CharacterFootStrideSlope.Flat : frame.StrideSlope);
            CharacterFootPelvisSpringHandoffReason handoff = CharacterFootPelvisSpringHandoffReason.None;
            if (response.SupportChanged) handoff |= CharacterFootPelvisSpringHandoffReason.SupportChanged;
            if (slopeChanged) handoff |= CharacterFootPelvisSpringHandoffReason.SlopeChanged;
            if (crossed) handoff |= CharacterFootPelvisSpringHandoffReason.TargetCrossedOutput;
            bool reset = (handoff != CharacterFootPelvisSpringHandoffReason.None || response.PreviousVelocity > 0f) &&
                Math.Abs(direction) > RuntimeGeometryEpsilon && response.PreviousVelocity * direction < 0f;
            float inputVelocity = reset ? 0f : response.PreviousVelocity;
            float output = response.PreviousOutput, velocity = inputVelocity;
            if (frame.DeltaSeconds > 0f)
            {
                float omega = response.Frequency * 2f * Mathf.PI;
                float x = response.PreviousOutput - target;
                float j = inputVelocity + omega * x;
                float decay = Mathf.Exp(-omega * frame.DeltaSeconds);
                output = target + (x + j * frame.DeltaSeconds) * decay;
                velocity = (inputVelocity - omega * j * frame.DeltaSeconds) * decay;
            }
            float integrated = output;
            bool completed = releasing && Math.Abs(output) <= RuntimeGeometryEpsilon &&
                Math.Abs(velocity) <= RuntimeGeometryEpsilon;
            if (completed) { output = 0f; velocity = 0f; }
            float visibleTolerance = reach.Left.FootTarget || reach.Right.FootTarget ? RuntimeGeometryEpsilon : 0.005f;
            float weight = !completed && Math.Abs(output) > visibleTolerance
                ? frame.FormalFootPlacementWeight : 0f;
            RequirePelvis(response.Handoff == handoff && response.VelocityReset == reset &&
                PelvisClose(response.InputVelocity, inputVelocity) && PelvisClose(response.Target, target) &&
                PelvisClose(response.IntegratedOutput, integrated) && PelvisClose(response.Output, output) &&
                PelvisClose(response.Velocity, velocity) && response.Completed == completed &&
                PelvisClose(response.PositionWeight, weight) && PelvisClose(frame.PelvisWeight, weight) &&
                Vector3.Distance(frame.StridePelvisDelta, reach.ComponentUp * output) <= RuntimeGeometryEpsilon,
                frame, "single spring response");
        }

        static bool PelvisPhysicalAvailable(FootFrame frame) =>
            frame.FinalPhysicalWriteAvailable &&
            frame.FinalPhysicalWriteCompletionIdentity == frame.CompletionIdentity;

        static void RequirePelvisObservation(FootFrame frame)
        {
            PelvisObservationFrame observation = frame.Pelvis.Observation;
            bool poseExpected = frame.StrideState == "Accepted" || frame.StrideState == "Releasing";
            RequirePelvis(observation.PoseInputAvailable == poseExpected &&
                FiniteVector(observation.PoseRootWorldPosition) &&
                FiniteVector(observation.AnimatedWorldPosition) &&
                FiniteVector(observation.AnimatedComponentPosition) &&
                FiniteVector(observation.PhysicalWorldPosition) && FiniteVector(frame.PhysicalPelvis) &&
                FiniteVector(frame.FinalPelvisGoal) && float.IsFinite(frame.PelvisWeight) &&
                frame.PelvisWeight >= 0f && frame.PelvisWeight <= 1f,
                frame, "physical observation input");
            if (!observation.PoseInputAvailable)
                RequirePelvis(observation.PoseRootWorldPosition.Equals(Vector3.zero) &&
                    observation.AnimatedWorldPosition.Equals(Vector3.zero) &&
                    observation.AnimatedComponentPosition.Equals(Vector3.zero), frame, "unavailable pose input");
            bool residualAvailable = PelvisPhysicalAvailable(frame) &&
                observation.PoseInputAvailable && frame.PelvisWeight > 0f;
            float expectedResidual = residualAvailable
                ? Vector3.Distance(frame.PhysicalPelvis,
                    observation.AnimatedComponentPosition + frame.FinalPelvisGoal * frame.PelvisWeight)
                : 0f;
            RequirePelvis(observation.GoalResidualAvailable == residualAvailable &&
                observation.GoalResidual >= 0f && PelvisClose(observation.GoalResidual, expectedResidual) &&
                (residualAvailable || observation.GoalResidual == 0f), frame, "physical goal residual");
        }

        static void RequirePelvisHistory(List<FootFrame> frames)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1], current = frames[i];
                PelvisResponseFrame response = current.Pelvis.Response, prior = previous.Pelvis.Response;
                if (!Continuous(previous, current) || !response.Evaluated || !response.HadPreviousState ||
                    previous.ProgramIdentity != current.ProgramIdentity || previous.ProfileRevision != current.ProfileRevision ||
                    previous.BodyResetSequence != current.BodyResetSequence)
                    continue;
                bool supportChanged = !current.PelvisHeightTarget.Available || !previous.PelvisHeightTarget.Available ||
                    previous.StrideSupportSide != current.StrideSupportSide ||
                    previous.PrimarySupportEventIdentity != current.PrimarySupportEventIdentity;
                RequirePelvis(prior.Evaluated && !prior.Completed &&
                    PelvisClose(response.PreviousTarget, prior.Target) && PelvisClose(response.PreviousOutput, prior.Output) &&
                    PelvisClose(response.PreviousVelocity, prior.Velocity) && response.SupportChanged == supportChanged &&
                    response.PreviousSlope == (previous.PelvisHeightTarget.Available ? previous.StrideSlope : CharacterFootStrideSlope.Flat),
                    current, "committed spring carry");
            }
        }

        static void RequireLegReachFacts(FootFrame frame)
        {
            if (!frame.FinalIkLegAvailable)
                return;
            double legLength = Vector3.Distance(
                                   frame.FinalIkLegOriginalHip,
                                   frame.FinalIkLegOriginalKnee) +
                               Vector3.Distance(
                                   frame.FinalIkLegOriginalKnee,
                                   frame.FinalIkLegOriginalAnkle);
            if (!double.IsFinite(legLength) || legLength <= TimeEpsilon)
            {
                throw new InvalidDataException(
                    "Foot Motion leg length facts are invalid.");
            }
            double originalLength = Vector3.Distance(
                frame.FinalIkLegOriginalHip,
                frame.FinalIkLegOriginalAnkle);
            double targetLength = Vector3.Distance(
                frame.FinalIkLegOriginalHip,
                frame.FinalIkLegTargetAnkle);
            double solvedLegLength = Vector3.Distance(
                                         frame.FinalIkLegSolvedHip,
                                         frame.FinalIkLegSolvedKnee) +
                                     Vector3.Distance(
                                         frame.FinalIkLegSolvedKnee,
                                         frame.FinalIkLegSolvedAnkle);
            double solvedLength = Vector3.Distance(
                frame.FinalIkLegSolvedHip,
                frame.FinalIkLegSolvedAnkle);
            bool consistent =
                float.IsFinite(frame.OriginalExtensionRatio) &&
                float.IsFinite(frame.TargetExtensionRatio) &&
                float.IsFinite(frame.SolvedExtensionRatio) &&
                float.IsFinite(frame.OriginalCompressionReserve) &&
                float.IsFinite(frame.TargetCompressionReserve) &&
                float.IsFinite(frame.SolvedCompressionReserve) &&
                Math.Abs(solvedLegLength - legLength) <=
                    PositionNoiseFloor &&
                Math.Abs(
                    frame.OriginalExtensionRatio -
                    originalLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.TargetExtensionRatio -
                    targetLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.SolvedExtensionRatio -
                    solvedLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.OriginalCompressionReserve -
                    Math.Max(0d, legLength - originalLength)) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.TargetCompressionReserve -
                    Math.Max(0d, legLength - targetLength)) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.SolvedCompressionReserve -
                    Math.Max(0d, legLength - solvedLength)) <= PositionNoiseFloor;
            if (!consistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion leg extension and compression facts are inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
            bool resolvedReachConsistent =
                float.IsFinite(frame.Resolved.LandingReachLegLength) &&
                float.IsFinite(
                    frame.Resolved.LandingReachMinimumCompressionReserve) &&
                frame.Resolved.LandingReachLegLength >= 0f &&
                frame.Resolved.LandingReachMinimumCompressionReserve >= 0f &&
                (!frame.Resolved.LandingReachAvailable ||
                 frame.Resolved.LandingReachEventIdentity != 0 &&
                 FiniteVector(frame.Resolved.LandingReachHip) &&
                 FiniteVector(frame.Resolved.LandingReachTargetAnkle) &&
                 frame.Resolved.LandingReachLegLength > TimeEpsilon &&
                 frame.Resolved.LandingReachMinimumCompressionReserve <
                 frame.Resolved.LandingReachLegLength &&
                 Math.Abs(
                     frame.Resolved.LandingReachLegLength - legLength) <=
                 PositionNoiseFloor) &&
                (!frame.LandingReachEvaluated ||
                 frame.Resolved.LandingReachAvailable);
            if (!resolvedReachConsistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion Landing Reach request and interval facts are inconsistent " +
                    $"Frame={frame.Frame} Side={frame.Side}.");
            }
        }

        static void RequireStepPhase(
            CharacterFootStepCandidateSample step,
            string field)
        {
            bool approach = step.AtOrAfterApproachContact;
            if (!float.IsFinite(step.EventPhase) ||
                step.EventPhase < 0f || step.EventPhase > 1f ||
                !float.IsFinite(step.ApproachContactToLandingProgress) ||
                step.ApproachContactToLandingProgress < 0f ||
                step.ApproachContactToLandingProgress > 1f ||
                !float.IsFinite(step.LandingPhase) ||
                step.LandingPhase != (step.IsValid ? 1f : 0f) ||
                step.InApproachContactToLanding != approach ||
                approach && (!step.IsValid || !step.IsSwing) ||
                !approach &&
                step.ApproachContactToLandingProgress != 0f)
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

        static void RequireFlags<T>(string value, string field)
            where T : struct, Enum
        {
            if (!Enum.TryParse(value, false, out T parsed))
            {
                throw new InvalidDataException(
                    $"Foot Motion Foot row {field} '{value}' is invalid.");
            }
            ulong allowed = 0;
            foreach (T candidate in Enum.GetValues(typeof(T)))
                allowed |= Convert.ToUInt64(candidate);
            ulong actual = Convert.ToUInt64(parsed);
            if ((actual & ~allowed) != 0)
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
            if (indices.Keys.Any(name => name.StartsWith("FootMotionSlidingResponse", StringComparison.Ordinal)))
                throw new InvalidDataException("Foot Motion samples contain retired response-history columns.");
            RequireColumnGroup(indices,
                "FootMotionCorrectionResponseDomain,FootMotionCorrectionResponsePreviousDomain,FootMotionCorrectionResponseDomainTransferred");
            string[] required =
            {
                "SampleIdentity", "ProgramIdentity", "ProjectionRevision",
                "PoseGraphId", "PoseGraphRevision", "PosePlanHash",
                "FrameSequence", "CompletionIdentity", "Side",
                "PresentationDeltaSeconds", "BodyResetSequence", "Grounded",
                "LeftActionInstanceIdentity", "LeftActionFootWeight",
                "RightActionInstanceIdentity", "RightActionFootWeight",
                "CurrentBodyTick",
                "TimelineCurrentVelocityX", "TimelineCurrentVelocityZ",
                "TimelineContinuationVelocityX",
                "TimelineContinuationVelocityZ",
                "PredictionMotionAvailable",
                "PredictionMotionRejectReason", "PredictionMotionResetReason",
                "PredictionMotionSourceIdentity",
                "PredictionRawCurrentVelocityX",
                "PredictionRawCurrentVelocityZ",
                "PredictionRawContinuationVelocityX",
                "PredictionRawContinuationVelocityZ",
                "PredictionPreviousStableCurrentVelocityX",
                "PredictionPreviousStableCurrentVelocityZ",
                "PredictionPreviousStableContinuationVelocityX",
                "PredictionPreviousStableContinuationVelocityZ",
                "PredictionStableCurrentVelocityX",
                "PredictionStableCurrentVelocityZ",
                "PredictionStableContinuationVelocityX",
                "PredictionStableContinuationVelocityZ",
                "PredictionCurrentVelocityDeltaX",
                "PredictionCurrentVelocityDeltaZ",
                "PredictionContinuationVelocityDeltaX",
                "PredictionContinuationVelocityDeltaZ",
                "PredictionVelocityResponseAlpha",
                "PredictionVelocityDeltaThreshold",
                "PredictionVelocitySmoothSpeed", "PredictionMaximumSpeed",
                "PredictionCurrentResponseApplied",
                "PredictionContinuationResponseApplied",
                "PredictionCurrentMaximumSpeedClamped",
                "PredictionContinuationMaximumSpeedClamped",
                "PredictionMotionRevision",
                "TargetBodyVelocityX", "TargetBodyVelocityY",
                "TargetBodyVelocityZ",
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
                "FormalContact", "InputFormalContact", "InputFormalLockMode", "InputFormalLockWeight", "InputFormalSupport",
                "StepSelectionMaximumPredictionTimeSeconds",
                "StepSelectionLastLandingEventIdentity",
                "SelectedStepSource", "SelectedLandingEventIdentity",
                "State", "LandingEventIdentity", "Accepted",
                "SurfaceIdentity", "LandingPointX", "LandingPointY",
                "LandingPointZ", "QueryDistance",
                "LandingObservationIdentity",
                "LandingObservationWorldRevision",
                "LandingObservationSourceSampleIdentity",
                "LandingObservationSourceSampleCycle",
                "LandingObservationCacheState",
                "LandingObservationQueryExecuted",
                "LandingObservationQueryPurpose",
                "LandingObservationRefreshMode",
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
                "FootMotionTargetHeightComponentUpX",
                "FootMotionTargetHeightComponentUpY",
                "FootMotionTargetHeightComponentUpZ",
                "FootMotionLifecycleTransitionEvaluated",
                "FootMotionPreviousLockRequestAvailable",
                "FootMotionPreviousLockRequested",
                "FootMotionPreviousLockRequestEventIdentity",
                "FootMotionPreviousLockRequestMode",
                "FootMotionPreviousLockRequestWeight",
                "FootMotionPreviousContactEdgeSeconds",
                "FootMotionPreviousLatestContactEventIdentity",
                "FootMotionPreviousLatestReleasedContactEventIdentity",
                "FootMotionPreviousCompletedLockWeightEventIdentity",
                "FootMotionPreviousContactAnchorAvailable",
                "FootMotionPreviousContactAnchorEventIdentity",
                "FootMotionPreviousContactAnchorAcquiredFrameSequence",
                "FootMotionPreviousContactAnchorAcquiredCompletionIdentity",
                "FootMotionPreviousContactAnchorWorldRevision",
                "FootMotionPreviousContactAnchorSurfaceIdentity",
                "FootMotionPreviousContactAnchorPointX",
                "FootMotionPreviousContactAnchorPointY",
                "FootMotionPreviousContactAnchorPointZ",
                "FootMotionPreviousContactAnchorNormalX",
                "FootMotionPreviousContactAnchorNormalY",
                "FootMotionPreviousContactAnchorNormalZ",
                "FootMotionCurrentLockRequested",
                "FootMotionCurrentLockRequestEventIdentity",
                "FootMotionCurrentLockRequestMode",
                "FootMotionCurrentLockRequestWeight",
                "FootMotionCurrentLockRequestAvailability",
                "FootMotionContactEdge",
                "FootMotionCurrentContactEdgeSeconds",
                "FootMotionCurrentLatestContactEventIdentity",
                "FootMotionCurrentLatestReleasedContactEventIdentity",
                "FootMotionCurrentCompletedLockWeightEventIdentity",
                "FootMotionCurrentContactAnchorAvailable",
                "FootMotionCurrentContactAnchorEventIdentity",
                "FootMotionCurrentContactAnchorAcquiredFrameSequence",
                "FootMotionCurrentContactAnchorAcquiredCompletionIdentity",
                "FootMotionCurrentContactAnchorWorldRevision",
                "FootMotionCurrentContactAnchorSurfaceIdentity",
                "FootMotionCurrentContactAnchorPointX",
                "FootMotionCurrentContactAnchorPointY",
                "FootMotionCurrentContactAnchorPointZ",
                "FootMotionCurrentContactAnchorNormalX",
                "FootMotionCurrentContactAnchorNormalY",
                "FootMotionCurrentContactAnchorNormalZ",
                "FootMotionSameEventContactReentryRefreshed",
                "FootMotionSameEventContactReentryUnavailable",
                "FootMotionRetainedVerifiedAnchor",
                "FootMotionReentryInterpolationHistoryRetained",
                "FootMotionFormalFootPlacementWeight",
                "FinalGoalPositionWeight", "FinalGoalRotationWeight",
                "FootMotionPostTransitionEvaluated",
                "FootMotionPreTransitionReason", "FootMotionPreTransitionSource",
                "FootMotionPreTransitionTarget", "FootMotionPreTransitionAnchorCommand",
                "FootMotionPostTransitionReason", "FootMotionPostTransitionSource",
                "FootMotionPostTransitionTarget", "FootMotionPostTransitionAnchorCommand",
                "FootMotionHardOwnershipLoss",
                "FootMotionHardOwnershipLossReason",
                "FootMotionPreTransitionSuppressOutput",
                "FootMotionPreTransitionResetInterpolation",
                "FootMotionPostTransitionSuppressOutput",
                "FootMotionPostTransitionResetInterpolation",
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
                "FootMotionRawFormalTargetHeight",
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
                "FootMotionLandingReachEvaluated",
                "FootMotionLandingReachAvailable",
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
                "FootMotionSwingTargetHeightAdoptionMode",
                "FootMotionSwingRawTargetHeightAlongUp",
                "FootMotionSwingFilteredTargetHeightBefore",
                "FootMotionSwingTargetHeightDelta",
                "FootMotionSwingTargetHeightAppliedDelta",
                "FootMotionSwingTargetHeightUpdateHeld",
                "FootMotionSwingTargetHeightForceRefreshed",
                "FootMotionSwingTargetHeightRateLimited",
                "FootMotionSwingTargetHeightClamped",
                "FootMotionSwingTargetHeightForceRefreshDistance",
                "FootMotionSwingTargetMaximumVerticalSpeed",
                "FootMotionSwingFilteredTargetHeightAlongUp",
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
                "FootMotionPlantInterpolationEvaluated",
                "FootMotionPlantTargetEventIdentity",
                "FootMotionPlantTargetVerified",
                "FootMotionPlantTargetKind",
                "FootMotionPlantLockResponse",
                "FootMotionPlantDesiredPointX",
                "FootMotionPlantDesiredPointY",
                "FootMotionPlantDesiredPointZ",
                "FootMotionPlantFilteredPointX",
                "FootMotionPlantFilteredPointY",
                "FootMotionPlantFilteredPointZ",
                "FootMotionPlantTargetHeightAdoptionMode",
                "FootMotionPlantTargetMaximumVerticalSpeed",
                "FootMotionPlantTargetHeightBefore",
                "FootMotionPlantTargetHeightTarget",
                "FootMotionPlantTargetVerticalDelta",
                "FootMotionPlantTargetAppliedVerticalDelta",
                "FootMotionPlantTargetHeightAfter",
                "FootMotionPlantTargetHeightEventIdentity",
                "FootMotionPlantTargetHeightUpdateReason",
                "FootMotionPlantTargetForceRefreshed",
                "FootMotionPlantTargetForceRefreshDistance",
                "FootMotionPlantTargetVerticalClamped",
                "FootMotionPlantPreviousSelectedWorldTargetX",
                "FootMotionPlantPreviousSelectedWorldTargetY",
                "FootMotionPlantPreviousSelectedWorldTargetZ",
                "FootMotionPlantSelectedWorldTargetX",
                "FootMotionPlantSelectedWorldTargetY",
                "FootMotionPlantSelectedWorldTargetZ",
                "FootMotionPreviousResponseOutputAvailable",
                "FootMotionPreviousResponseOutputPointX",
                "FootMotionPreviousResponseOutputPointY",
                "FootMotionPreviousResponseOutputPointZ",
                "FootMotionDesiredOutputPointX",
                "FootMotionDesiredOutputPointY",
                "FootMotionDesiredOutputPointZ",
                "FootMotionResponseOutputPointX",
                "FootMotionResponseOutputPointY",
                "FootMotionResponseOutputPointZ",
                "FootMotionPlantResidualCaptureReason",
                "FootMotionPlantWorldResidualBeforeCaptureX",
                "FootMotionPlantWorldResidualBeforeCaptureY",
                "FootMotionPlantWorldResidualBeforeCaptureZ",
                "FootMotionPlantWorldResidualCapturedBeforeDecayX",
                "FootMotionPlantWorldResidualCapturedBeforeDecayY",
                "FootMotionPlantWorldResidualCapturedBeforeDecayZ",
                "FootMotionPlantWorldResidualDecayApplied",
                "FootMotionPlantWorldResidualBaseHalfLifeSeconds",
                "FootMotionPlantWorldResidualDeadlineHalfLifeAvailable",
                "FootMotionPlantWorldResidualDeadlineHalfLifeSeconds",
                "FootMotionPlantWorldResidualAppliedHalfLifeSeconds",
                "FootMotionPlantWorldResidualAfterDecayX",
                "FootMotionPlantWorldResidualAfterDecayY",
                "FootMotionPlantWorldResidualAfterDecayZ",
                "FootMotionPlantWorldResidualCompletionTolerance",
                "FootMotionPlantWorldResidualClearedAtCompletionTolerance",
                "FootMotionCorrectionResponseEvaluated",
                "FootMotionCorrectionResponseInitializedBefore",
                "FootMotionCorrectionResponseInitializedThisFrame",
                "FootMotionCorrectionResponseInitializationReason",
                "FootMotionCorrectionResponseDesired",
                "FootMotionCorrectionResponsePrevious",
                "FootMotionCorrectionResponseCurrent",
                "FootMotionCorrectionResponseDeltaDirection",
                "FootMotionCorrectionResponseSelectedSpeed",
                "FootMotionCorrectionResponseAppliedDelta",
                "FootMotionPlantVerticalContinuityOwners",
                "FootMotionPlantEffectiveCorrectionBeforeX",
                "FootMotionPlantEffectiveCorrectionBeforeY",
                "FootMotionPlantEffectiveCorrectionBeforeZ",
                "FootMotionPlantEffectiveCorrectionAfterX",
                "FootMotionPlantEffectiveCorrectionAfterY",
                "FootMotionPlantEffectiveCorrectionAfterZ",
                "FootMotionPlantOutputDistance",
                "FootMotionPlantPenetrationDepth",
                "FootMotionPlantLockWeightCompleted",
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
                "FinalPhysicalWriteCompletionIdentity",
                "FinalPhysicalAnkleComponentPositionX",
                "FinalPhysicalAnkleComponentPositionY",
                "FinalPhysicalAnkleComponentPositionZ",
                "FinalPhysicalAnkleGoalResidual",
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
                "FinalIkLegSolvedHipX", "FinalIkLegSolvedHipY",
                "FinalIkLegSolvedHipZ", "FinalIkLegSolvedKneeX",
                "FinalIkLegSolvedKneeY", "FinalIkLegSolvedKneeZ",
                "FinalIkLegSolvedAnkleX", "FinalIkLegSolvedAnkleY",
                "FinalIkLegSolvedAnkleZ",
                "FinalIkLegOriginalExtensionRatio", "FinalIkLegTargetExtensionRatio", "FinalIkLegSolvedExtensionRatio",
                "FinalIkLegSolvedBendDegrees", "FinalIkLegOriginalCompressionReserve", "FinalIkLegTargetCompressionReserve", "FinalIkLegSolvedCompressionReserve",
                "FinalIkLegEffectiveBendDirectionPreviousDot",
                "PrimarySupportHasValue", "PrimarySupportSide",
                "PrimarySupportLandingEventIdentity",
                "StrideState", "StrideSupportSide",
                "PelvisHeightTargetAvailable",
                "PelvisHeightTargetComponentUpX", "PelvisHeightTargetComponentUpY", "PelvisHeightTargetComponentUpZ",
                "PelvisHeightTargetLeftAnimatedSoleX", "PelvisHeightTargetLeftAnimatedSoleY", "PelvisHeightTargetLeftAnimatedSoleZ",
                "PelvisHeightTargetRightAnimatedSoleX", "PelvisHeightTargetRightAnimatedSoleY", "PelvisHeightTargetRightAnimatedSoleZ",
                "PelvisHeightTargetLeftTargetSoleX", "PelvisHeightTargetLeftTargetSoleY", "PelvisHeightTargetLeftTargetSoleZ",
                "PelvisHeightTargetRightTargetSoleX", "PelvisHeightTargetRightTargetSoleY", "PelvisHeightTargetRightTargetSoleZ",
                "PelvisHeightTargetAnimatedMinimumAlongUp", "PelvisHeightTargetMinimumAlongUp", "PelvisRequestedOffsetAlongUp",
                "PelvisPosturePreferenceEvaluated",
                "PelvisPosturePreferenceAvailable",
                "PelvisPosturePreferenceHipX",
                "PelvisPosturePreferenceHipY",
                "PelvisPosturePreferenceHipZ",
                "PelvisPosturePreferenceAnimatedAnkleX",
                "PelvisPosturePreferenceAnimatedAnkleY",
                "PelvisPosturePreferenceAnimatedAnkleZ",
                "PelvisPosturePreferenceTargetAnkleX",
                "PelvisPosturePreferenceTargetAnkleY",
                "PelvisPosturePreferenceTargetAnkleZ",
                "PelvisPosturePreferenceLegLength",
                "PelvisPosturePreferenceCompressionReserve",
                "PelvisPosturePreferenceUsableLegLength",
                "PelvisPosturePreferenceMinimumAlongUp",
                "PelvisPosturePreferenceMaximumAlongUp",
                "PelvisPosturePreferenceOffsetAlongUp",
                "PelvisPosturePreferenceTargetAdjusted",
                "PelvisReachComponentUpX",
                "PelvisReachComponentUpY",
                "PelvisReachComponentUpZ",
                "PelvisReachStatus",
                "PelvisReachIntersectionEvaluated",
                "PelvisReachIntersectionMinimumAlongUp",
                "PelvisReachIntersectionMaximumAlongUp",
                "PelvisReachLeftRole",
                "PelvisReachLeftStatus",
                "PelvisReachLeftEventIdentity",
                "PelvisReachLeftHipX",
                "PelvisReachLeftHipY",
                "PelvisReachLeftHipZ",
                "PelvisReachLeftTargetAnkleX",
                "PelvisReachLeftTargetAnkleY",
                "PelvisReachLeftTargetAnkleZ",
                "PelvisReachLeftLegLength",
                "PelvisReachLeftMinimumCompressionReserve",
                "PelvisReachLeftUsableLegLength",
                "PelvisReachLeftMinimumAlongUp",
                "PelvisReachLeftMaximumAlongUp",
                "PelvisReachLeftRequested",
                "PelvisReachLeftAvailable",
                "PelvisReachRightRole",
                "PelvisReachRightStatus",
                "PelvisReachRightEventIdentity",
                "PelvisReachRightHipX",
                "PelvisReachRightHipY",
                "PelvisReachRightHipZ",
                "PelvisReachRightTargetAnkleX",
                "PelvisReachRightTargetAnkleY",
                "PelvisReachRightTargetAnkleZ",
                "PelvisReachRightLegLength",
                "PelvisReachRightMinimumCompressionReserve",
                "PelvisReachRightUsableLegLength",
                "PelvisReachRightMinimumAlongUp",
                "PelvisReachRightMaximumAlongUp",
                "PelvisReachRightRequested",
                "PelvisReachRightAvailable",
                "PelvisResponseEvaluated",
                "PelvisSpringCompleted",
                "PelvisSpringIntegratedOutput",
                "StrideHadPreviousState",
                "StrideSupportChanged",
                "StrideSpringVelocityReset",
                "StridePreviousSpringTarget",
                "StridePreviousSpringOutput",
                "StridePreviousSpringVelocity",
                "StrideSpringInput",
                "StrideSpringInputVelocity",
                "StrideSpringFrequency",
                "StrideSpringTarget",
                "StrideSpringVelocity",
                "StridePositionWeight",
                "StridePreviousSlope",
                "StrideSpringHandoffReason",
                "StrideSlope",
                "StrideRejectReason",
                "StridePelvisDeltaX",
                "StridePelvisDeltaY",
                "StridePelvisDeltaZ",
                "StrideSpringOutput",
                "PelvisPositionWeight", "FinalPelvisGoalX", "FinalPelvisGoalY", "FinalPelvisGoalZ",
                "FinalPhysicalPelvisComponentPositionX", "FinalPhysicalPelvisComponentPositionY",
                "FinalPhysicalPelvisComponentPositionZ",
                "PelvisPoseInputAvailable",
                "StridePoseRootPositionX", "StridePoseRootPositionY", "StridePoseRootPositionZ",
                "StrideAnimatedPelvisX", "StrideAnimatedPelvisY", "StrideAnimatedPelvisZ",
                "StrideAnimatedPelvisComponentPositionX", "StrideAnimatedPelvisComponentPositionY", "StrideAnimatedPelvisComponentPositionZ",
                "FinalPhysicalPelvisWorldPositionX", "FinalPhysicalPelvisWorldPositionY", "FinalPhysicalPelvisWorldPositionZ",
                "FinalPhysicalPelvisGoalResidualAvailable", "FinalPhysicalPelvisGoalResidual"
            };
            foreach (string name in required)
            {
                if (!indices.ContainsKey(name))
                    throw new InvalidDataException($"Foot Motion samples CSV is missing '{name}'.");
            }
            RequireColumnGroup(
                indices,
                "FootProfileId,FootProfileRevision,ApproachPlantTargetPrepared,PlantTargetNormalX,PlantTargetNormalY,PlantTargetNormalZ,PlantTargetTrajectoryGeneration,PlantTargetFutureBodyTranslationSourceIdentity,FootMotionSourceAnkleRotationX,FootMotionSourceAnkleRotationY,FootMotionSourceAnkleRotationZ,FootMotionSourceAnkleRotationW,FootMotionPositionWeight,FootMotionRotationWeight");
            RequireColumnGroup(
                indices,
                "FootMotionCorrectionResponseRequestedDirectionX,FootMotionCorrectionResponseRequestedDirectionY,FootMotionCorrectionResponseRequestedDirectionZ,FootMotionCorrectionResponsePreviousDirectionX,FootMotionCorrectionResponsePreviousDirectionY,FootMotionCorrectionResponsePreviousDirectionZ,FootMotionCorrectionResponseDirectionLimited,FootMotionCorrectionResponseMaximumDirectionChangeDegrees,FootMotionCorrectionResponseAppliedDirectionChangeDegrees,FootMotionCorrectionResponseVisibleOutputTransferred,FootMotionCorrectionResponseBeforeRebase,FootMotionCorrectionResponseDirectionX,FootMotionCorrectionResponseDirectionY,FootMotionCorrectionResponseDirectionZ");
        }

        static void RequireColumnGroup(
            Dictionary<string, int> indices,
            string columns)
        {
            string[] values = columns.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                if (!indices.ContainsKey(values[i]))
                {
                    throw new InvalidDataException(
                        $"Foot Motion samples CSV is missing '{values[i]}'.");
                }
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

        static double DirectionCosine(Vector3 previous, Vector3 current)
        {
            double denominator = Math.Sqrt(
                previous.sqrMagnitude * current.sqrMagnitude);
            return denominator > RuntimeGeometryEpsilon *
                   RuntimeGeometryEpsilon
                ? Math.Clamp(
                    Vector3.Dot(previous, current) / denominator,
                    -1d,
                    1d)
                : 1d;
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

        readonly struct CharacterFootOutputBoundaryMotion
        {
            internal CharacterFootOutputBoundaryMotion(
                double correctionReexpressionStepMeters,
                double correctedSoleStepMeters,
                double animatedSoleStepMeters,
                double stateAdditionalOutputStepMeters,
                double outputBlendParameter,
                bool physicalOutputAvailable,
                double finalPhysicalAnkleStepMeters,
                double finalPhysicalSoleStepMeters)
            {
                CorrectionReexpressionStepMeters =
                    correctionReexpressionStepMeters;
                CorrectedSoleStepMeters = correctedSoleStepMeters;
                AnimatedSoleStepMeters = animatedSoleStepMeters;
                StateAdditionalOutputStepMeters =
                    stateAdditionalOutputStepMeters;
                OutputBlendParameter = outputBlendParameter;
                PhysicalOutputAvailable = physicalOutputAvailable;
                FinalPhysicalAnkleStepMeters =
                    finalPhysicalAnkleStepMeters;
                FinalPhysicalSoleStepMeters = finalPhysicalSoleStepMeters;
            }

            internal double CorrectionReexpressionStepMeters { get; }
            internal double CorrectedSoleStepMeters { get; }
            internal double AnimatedSoleStepMeters { get; }
            internal double StateAdditionalOutputStepMeters { get; }
            internal double OutputBlendParameter { get; }
            internal bool PhysicalOutputAvailable { get; }
            internal double FinalPhysicalAnkleStepMeters { get; }
            internal double FinalPhysicalSoleStepMeters { get; }
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
                List<FootFrame> right,
                List<CharacterFootDiagnosticSourceIndex> sourceIndices)
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
                SourceIndices = sourceIndices;
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
            internal IReadOnlyList<CharacterFootDiagnosticSourceIndex> SourceIndices { get; }
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
            internal CharacterFootResolvedSample Resolved;
            internal CharacterFootCurrentSupportSample CurrentSupport;
            internal CharacterFootStepPhaseSample SelectedPhase;
            internal CharacterFootEventSample OutputEvents;
            internal CharacterFootEventSample InputEvents;
            internal string SampleIdentity;
            internal string ProgramIdentity;
            internal string ProjectionRevision;
            internal string PoseGraphId;
            internal string PoseGraphRevision;
            internal string PosePlanHash;
            internal int Frame;
            internal ulong CompletionIdentity;
            internal string ProfileId;
            internal string ProfileRevision;
            internal string Side;
            internal bool ApproachPlantTargetPrepared;
            internal bool PreparedTargetAvailable;
            internal ulong PreparedTargetEventIdentity;
            internal int PreparedTargetSurfaceIdentity;
            internal Vector3 PreparedTargetPoint;
            internal Vector3 PreparedTargetNormal;
            internal ulong PreparedTargetTrajectoryGeneration;
            internal string PreparedTargetFutureBodySource;
            internal float DeltaSeconds;
            internal ulong BodyResetSequence;
            internal ulong CurrentBodyTick;
            internal Vector3 BodyTargetVelocity;
            internal Vector2 TimelineCurrentVelocity;
            internal Vector2 TimelineContinuationVelocity;
            internal bool PredictionMotionAvailable;
            internal string PredictionMotionRejectReason;
            internal string PredictionMotionResetReason;
            internal string PredictionMotionSourceIdentity;
            internal Vector2 PredictionRawCurrentVelocity;
            internal Vector2 PredictionRawContinuationVelocity;
            internal Vector2 PredictionPreviousStableCurrentVelocity;
            internal Vector2 PredictionPreviousStableContinuationVelocity;
            internal Vector2 PredictionStableCurrentVelocity;
            internal Vector2 PredictionStableContinuationVelocity;
            internal Vector2 PredictionCurrentVelocityDelta;
            internal Vector2 PredictionContinuationVelocityDelta;
            internal float PredictionVelocityResponseAlpha;
            internal float PredictionVelocityDeltaThreshold;
            internal float PredictionVelocitySmoothSpeed;
            internal float PredictionMaximumSpeed;
            internal bool PredictionCurrentResponseApplied;
            internal bool PredictionContinuationResponseApplied;
            internal bool PredictionCurrentMaximumSpeedClamped;
            internal bool PredictionContinuationMaximumSpeedClamped;
            internal ulong PredictionMotionRevision;
            internal bool Grounded;
            internal ulong ActionInstanceIdentity;
            internal float ActionFootWeight;
            internal float TimeToLandingSeconds;
            internal bool FormalOutputObservationAvailable;
            internal float FormalFootHeight;
            internal Vector3 PoseRootWorldPosition;
            internal Quaternion PoseRootWorldRotation;
            internal float StepSelectionMaximumPredictionTimeSeconds;
            internal ulong StepSelectionLastLandingEventIdentity;
            internal string SelectedStepSource;
            internal ulong SelectedLandingEventIdentity;
            internal CharacterFootStepCandidateSample CurrentStep;
            internal CharacterFootStepCandidateSample IncomingStep;
            internal bool FormalObservationAvailable;
            internal string SourceIdentity;
            internal int SourceCycle;
            internal ulong ContributionContinuityIdentity;
            internal ulong FormalObservationCompletionIdentity;
            internal float FormalNormalizedTime;
            internal float FormalStepTime;
            internal float FormalContact;
            internal float FormalRequestContact;
            internal string FormalLockMode;
            internal float FormalLockWeight;
            internal float FormalSupport;
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
            internal string LandingObservationQueryPurpose;
            internal string LandingObservationRefreshMode;
            internal string LandingObservationQueryReason;
            internal Vector3 LandingObservationCanonicalRaw;
            internal Vector3 LandingObservationCanonicalComponentUp;
            internal Vector3 LandingObservationCandidateRaw;
            internal Vector3 LandingObservationCandidateComponentUp;
            internal float LandingObservationQueryInputDistance;
            internal float LandingObservationQueryComponentUpAngleDegrees;
            internal float LandingObservationPredictionInputAccumulationDistance;
            internal float LandingObservationComponentUpChangeAngleDegrees;
            internal string FutureLandingQueryPurpose;
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
            internal bool GroundSurfaceFactsAvailable;
            internal CharacterFootGroundSurfaceState GroundSurfaceState;
            internal ulong GroundSurfaceWorldRevision;
            internal int GroundSurfaceSegmentCount;
            internal int GroundSurfaceObservedCount;
            internal ulong LastLandingEventIdentity;
            internal ulong NextLandingEventIdentity;
            internal int NextLandingSurfaceIdentity;
            internal Vector3 LastLanding;
            internal Vector3 NextLanding;
            internal int GroundEnvelopeVertexCount;
            internal Vector3 ComponentUp;
            internal Vector3 GroundPathComponentUp;
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
            internal Quaternion SourceAnkleRotation;
            internal float SwingProgress;
            internal Vector3 SwingBaselineSample;
            internal float SwingBaselineSampleAlongUp;
            internal Vector3 SwingEnvelopeSample;
            internal float SwingEnvelopeSampleAlongUp;
            internal float SwingFormalFootHeight;
            internal float SwingRawFormalTargetHeight;
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
            internal float MotionPositionWeight;
            internal float MotionRotationWeight;
            internal float FinalGoalPositionWeight;
            internal float FinalGoalRotationWeight;
            internal Vector3 Anchor;
            internal bool ContactPlaneAvailable;
            internal float ContactOwnership;
            internal bool LandingReachEvaluated;
            internal bool LandingReachAvailable;
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
            internal string SwingTargetHeightAdoptionMode;
            internal float SwingRawTargetHeightAlongUp;
            internal float SwingFilteredTargetHeightBefore;
            internal float SwingTargetHeightDelta;
            internal float SwingTargetHeightAppliedDelta;
            internal bool SwingTargetHeightUpdateHeld;
            internal bool SwingTargetHeightForceRefreshed;
            internal bool SwingTargetHeightRateLimited;
            internal bool SwingTargetHeightClamped;
            internal float SwingTargetHeightForceRefreshDistance;
            internal float SwingTargetMaximumVerticalSpeed;
            internal float SwingFilteredTargetHeightAlongUp;
            internal string PreTransitionReason;
            internal string PreTransitionSource;
            internal string PreTransitionTarget;
            internal string PreTransitionAnchorCommand;
            internal string PostTransitionReason;
            internal string PostTransitionSource;
            internal string PostTransitionTarget;
            internal string PostTransitionAnchorCommand;
            internal bool LifecycleTransitionEvaluated;
            internal bool PreviousLockRequestAvailable;
            internal bool PreviousLockRequested;
            internal ulong PreviousLockRequestEventIdentity;
            internal string PreviousLockRequestMode;
            internal float PreviousLockRequestWeight;
            internal float PreviousContactEdgeSeconds;
            internal ulong PreviousLatestContactEventIdentity;
            internal ulong PreviousLatestReleasedContactEventIdentity;
            internal ulong PreviousCompletedLockWeightEventIdentity;
            internal bool PreviousContactAnchorAvailable;
            internal ulong PreviousContactAnchorEventIdentity;
            internal ulong PreviousContactAnchorAcquiredFrameSequence;
            internal ulong PreviousContactAnchorAcquiredCompletionIdentity;
            internal ulong PreviousContactAnchorWorldRevision;
            internal int PreviousContactAnchorSurfaceIdentity;
            internal Vector3 PreviousContactAnchorPoint;
            internal Vector3 PreviousContactAnchorNormal;
            internal bool CurrentLockRequested;
            internal ulong CurrentLockRequestEventIdentity;
            internal string CurrentLockRequestMode;
            internal float CurrentLockRequestWeight;
            internal string CurrentLockRequestAvailability;
            internal string ContactEdge;
            internal float CurrentContactEdgeSeconds;
            internal ulong CurrentLatestContactEventIdentity;
            internal ulong CurrentLatestReleasedContactEventIdentity;
            internal ulong CurrentCompletedLockWeightEventIdentity;
            internal bool CurrentContactAnchorAvailable;
            internal ulong CurrentContactAnchorEventIdentity;
            internal ulong CurrentContactAnchorAcquiredFrameSequence;
            internal ulong CurrentContactAnchorAcquiredCompletionIdentity;
            internal ulong CurrentContactAnchorWorldRevision;
            internal int CurrentContactAnchorSurfaceIdentity;
            internal Vector3 CurrentContactAnchorPoint;
            internal Vector3 CurrentContactAnchorNormal;
            internal bool SameEventContactReentryRefreshed;
            internal bool SameEventContactReentryUnavailable;
            internal bool RetainedVerifiedAnchor;
            internal bool ReentryInterpolationHistoryRetained;
            internal float FormalFootPlacementWeight;
            internal bool PostTransitionEvaluated;
            internal bool HardOwnershipLoss;
            internal string HardOwnershipLossReason;
            internal bool PreTransitionSuppressOutput;
            internal bool PreTransitionResetInterpolation;
            internal bool PostTransitionSuppressOutput;
            internal bool PostTransitionResetInterpolation;
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
            internal bool PlantInterpolationEvaluated;
            internal ulong PlantTargetEventIdentity;
            internal bool PlantTargetVerified;
            internal string PlantTargetKind;
            internal string PlantLockResponse;
            internal bool PlantLockWeightCompleted;
            internal Vector3 PlantDesiredPoint;
            internal Vector3 PlantFilteredPoint;
            internal CharacterFootSupportTargetSample SelectedSupportTarget;
            internal string PlantTargetHeightAdoptionMode;
            internal float PlantTargetMaximumVerticalSpeed;
            internal float PlantTargetHeightBefore;
            internal float PlantTargetHeightTarget;
            internal float PlantTargetVerticalDelta;
            internal float PlantTargetAppliedVerticalDelta;
            internal float PlantTargetHeightAfter;
            internal ulong PlantTargetHeightEventIdentity;
            internal string PlantTargetHeightUpdateReason;
            internal bool PlantTargetForceRefreshed;
            internal float PlantTargetForceRefreshDistance;
            internal bool PlantTargetVerticalClamped;
            internal Vector3 PlantPreviousSelectedWorldTarget;
            internal Vector3 PlantSelectedWorldTarget;
            internal bool PreviousResponseOutputAvailable;
            internal Vector3 PreviousResponseOutputPoint;
            internal Vector3 DesiredOutputPoint;
            internal Vector3 ResponseOutputPoint;
            internal string PlantResidualCaptureReason;
            internal Vector3 PlantWorldResidualBeforeCapture;
            internal Vector3 PlantWorldResidualCapturedBeforeDecay;
            internal bool PlantWorldResidualDecayApplied;
            internal float PlantWorldResidualBaseHalfLifeSeconds;
            internal bool PlantWorldResidualDeadlineHalfLifeAvailable;
            internal float PlantWorldResidualDeadlineHalfLifeSeconds;
            internal float PlantWorldResidualAppliedHalfLifeSeconds;
            internal Vector3 PlantWorldResidualAfterDecay;
            internal float PlantWorldResidualCompletionTolerance;
            internal bool PlantWorldResidualClearedAtCompletionTolerance;
            internal bool CorrectionResponseEvaluated;
            internal string CorrectionResponseDomain;
            internal string CorrectionResponsePreviousDomain;
            internal bool CorrectionResponseDomainTransferred;
            internal bool CorrectionResponseInitializedBefore;
            internal bool CorrectionResponseInitializedThisFrame;
            internal string CorrectionResponseInitializationReason;
            internal float CorrectionResponseDesired;
            internal Vector3 CorrectionResponseRequestedDirection;
            internal Vector3 CorrectionResponsePreviousDirection;
            internal bool CorrectionResponseDirectionLimited;
            internal float CorrectionResponseMaximumDirectionChangeDegrees;
            internal float CorrectionResponseAppliedDirectionChangeDegrees;
            internal bool CorrectionResponseVisibleOutputTransferred;
            internal float CorrectionResponseBeforeRebase;
            internal float CorrectionResponsePrevious;
            internal float CorrectionResponseCurrent;
            internal Vector3 CorrectionResponseDirection;
            internal string CorrectionResponseDeltaDirection;
            internal float CorrectionResponseSelectedSpeed;
            internal float CorrectionResponseAppliedDelta;
            internal string PlantVerticalContinuityOwners;
            internal Vector3 PlantEffectiveCorrectionBefore;
            internal Vector3 PlantEffectiveCorrectionAfter;
            internal float PlantOutputDistance;
            internal float PlantPenetrationDepth;
            internal bool EncodedGoalAvailable;
            internal Vector3 EncodedGoalPosition;
            internal Vector3 EncodedGoalCorrection;
            internal bool FinalIkEffectorAvailable;
            internal Vector3 FinalIkTargetPosition;
            internal Vector3 FinalIkSolvedPosition;
            internal bool FinalPhysicalWriteAvailable;
            internal ulong FinalPhysicalWriteCompletionIdentity;
            internal CharacterFootContactSupportGapFrame ContactSupportGap;
            internal Vector3 FinalPhysicalAnkleComponentPosition;
            internal float FinalPhysicalAnkleGoalResidual;
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
            internal float OriginalExtensionRatio;
            internal float TargetExtensionRatio;
            internal float SolvedExtensionRatio;
            internal float SolvedBendDegrees;
            internal float OriginalCompressionReserve;
            internal float TargetCompressionReserve;
            internal float SolvedCompressionReserve;
            internal float BendDirectionPreviousDot;
            internal bool FinalIkLegAvailable;
            internal Vector3 FinalIkLegOriginalHip;
            internal Vector3 FinalIkLegOriginalKnee;
            internal Vector3 FinalIkLegOriginalAnkle;
            internal Vector3 FinalIkLegTargetAnkle;
            internal Vector3 FinalIkLegSolvedHip;
            internal Vector3 FinalIkLegSolvedKnee;
            internal Vector3 FinalIkLegSolvedAnkle;
            internal bool PrimarySupportAvailable;
            internal string PrimarySupportSide;
            internal ulong PrimarySupportEventIdentity;
            internal string StrideState;
            internal PelvisHeightTargetFrame PelvisHeightTarget;
            internal string StrideSupportSide;
            internal PelvisFrame Pelvis;
            internal CharacterFootStrideSlope StrideSlope;
            internal CharacterFootStrideRejectReason StrideRejectReason;
            internal Vector3 StridePelvisDelta;
            internal float StrideSpringOutput;
            internal float PelvisWeight;
            internal Vector3 FinalPelvisGoal;
            internal Vector3 PhysicalPelvis;
            internal Vector3 EffectiveCorrection => CorrectedAnkle - OriginalAnkle;
        }

        static CharacterFootPelvisFrameObservation BuildPelvisFact(FootFrame frame, FootFrame previous) =>
            new CharacterFootPelvisFrameObservation
            {
                frame = frame.Frame,
                completionIdentity = frame.CompletionIdentity.ToString(CultureInfo.InvariantCulture),
                strideState = frame.StrideState,
                strideRejectReason = frame.StrideRejectReason.ToString(),
                formalFootPlacementWeight = frame.FormalFootPlacementWeight,
                primarySupportSide = frame.PrimarySupportSide,
                primarySupportEventIdentity = frame.PrimarySupportEventIdentity.ToString(CultureInfo.InvariantCulture),
                observation = BuildPelvisObservationFact(frame),
                motion = BuildPelvisMotionFact(previous, frame),
                heightTarget = frame.PelvisHeightTarget.ToFact(frame),
                posturePreference = BuildPelvisPostureFact(frame.Pelvis.Posture),
                reach = BuildPelvisReachFact(frame.Pelvis.Reach),
                response = BuildPelvisResponseFact(frame.Pelvis.Response)
            };

        static CharacterFootPelvisOutputObservation BuildPelvisObservationFact(FootFrame frame)
        {
            PelvisObservationFrame observation = frame.Pelvis.Observation;
            bool physicalAvailable = PelvisPhysicalAvailable(frame);
            return new CharacterFootPelvisOutputObservation
            {
                poseInputAvailable = observation.PoseInputAvailable,
                poseRootWorldPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.PoseRootWorldPosition) : null,
                animatedWorldPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.AnimatedWorldPosition) : null,
                animatedComponentPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.AnimatedComponentPosition) : null,
                physicalWriteAvailable = physicalAvailable,
                physicalWriteCompletionIdentity = frame.FinalPhysicalWriteCompletionIdentity.ToString(CultureInfo.InvariantCulture),
                physicalWorldPosition = physicalAvailable ? CharacterFootVectorFact.From(observation.PhysicalWorldPosition) : null,
                physicalComponentPosition = physicalAvailable ? CharacterFootVectorFact.From(frame.PhysicalPelvis) : null,
                goalCorrectionComponent = CharacterFootVectorFact.From(frame.FinalPelvisGoal),
                positionWeight = frame.PelvisWeight,
                weightedCorrectionComponent = CharacterFootVectorFact.From(frame.FinalPelvisGoal * frame.PelvisWeight),
                goalResidualAvailable = observation.GoalResidualAvailable,
                expectedPhysicalComponentPosition = observation.GoalResidualAvailable
                    ? CharacterFootVectorFact.From(observation.AnimatedComponentPosition + frame.FinalPelvisGoal * frame.PelvisWeight) : null,
                goalResidualComponentUnits = observation.GoalResidualAvailable ? (double?)observation.GoalResidual : null
            };
        }

        static CharacterFootPelvisMotionObservation BuildPelvisMotionFact(FootFrame previous, FootFrame frame)
        {
            bool continuous = previous != null && Continuous(previous, frame);
            bool physicalAvailable = continuous && PelvisPhysicalAvailable(previous) && PelvisPhysicalAvailable(frame);
            return new CharacterFootPelvisMotionObservation
            {
                previousFrameAvailable = continuous,
                previousFrame = continuous ? (int?)previous.Frame : null,
                presentationDeltaSeconds = frame.DeltaSeconds,
                physicalStepAvailable = physicalAvailable,
                physicalWorldDelta = physicalAvailable
                    ? CharacterFootVectorFact.From(frame.Pelvis.Observation.PhysicalWorldPosition - previous.Pelvis.Observation.PhysicalWorldPosition) : null,
                physicalComponentDelta = physicalAvailable
                    ? CharacterFootVectorFact.From(frame.PhysicalPelvis - previous.PhysicalPelvis) : null,
                weightedCorrectionComponentDelta = continuous
                    ? CharacterFootVectorFact.From(frame.FinalPelvisGoal * frame.PelvisWeight - previous.FinalPelvisGoal * previous.PelvisWeight) : null
            };
        }

        static CharacterFootPelvisPostureObservation BuildPelvisPostureFact(PelvisPostureFrame posture) =>
            new CharacterFootPelvisPostureObservation
            {
                evaluated = posture.Evaluated,
                available = posture.Available,
                hip = posture.Evaluated ? CharacterFootVectorFact.From(posture.Hip) : null,
                animatedAnkle = posture.Evaluated ? CharacterFootVectorFact.From(posture.AnimatedAnkle) : null,
                targetAnkle = posture.Evaluated ? CharacterFootVectorFact.From(posture.TargetAnkle) : null,
                legLength = posture.Evaluated ? (double?)posture.LegLength : null,
                compressionReserve = posture.Evaluated ? (double?)posture.CompressionReserve : null,
                usableLegLength = posture.Available ? (double?)posture.UsableLegLength : null,
                minimumAlongUp = posture.Available ? (double?)posture.MinimumAlongUp : null,
                maximumAlongUp = posture.Available ? (double?)posture.MaximumAlongUp : null,
                offsetAlongUp = posture.Evaluated ? (double?)posture.OffsetAlongUp : null,
                targetAdjusted = posture.TargetAdjusted,
            };

        static CharacterFootPelvisLegReachObservation BuildPelvisLegFact(PelvisLegFrame leg) =>
            new CharacterFootPelvisLegReachObservation
            {
                role = leg.Role.ToString(),
                status = leg.Status.ToString(),
                eventIdentity = leg.Requested ? leg.EventIdentity.ToString(CultureInfo.InvariantCulture) : null,
                hip = leg.Requested ? CharacterFootVectorFact.From(leg.Hip) : null,
                targetAnkle = leg.Requested ? CharacterFootVectorFact.From(leg.TargetAnkle) : null,
                legLength = leg.Requested ? (double?)leg.LegLength : null,
                minimumCompressionReserve = leg.Requested ? (double?)leg.MinimumCompressionReserve : null,
                usableLegLength = leg.Requested ? (double?)leg.UsableLegLength : null,
                minimumAlongUp = leg.Available ? (double?)leg.MinimumAlongUp : null,
                maximumAlongUp = leg.Available ? (double?)leg.MaximumAlongUp : null,
                requested = leg.Requested,
                available = leg.Available,
            };

        static CharacterFootPelvisReachObservation BuildPelvisReachFact(PelvisReachFrame reach) =>
            new CharacterFootPelvisReachObservation
            {
                componentUp = reach.ComponentUp.Equals(Vector3.zero) ? null : CharacterFootVectorFact.From(reach.ComponentUp),
                status = reach.Status.ToString(),
                intersectionEvaluated = reach.IntersectionEvaluated,
                intersectionMinimumAlongUp = reach.IntersectionEvaluated ? (double?)reach.IntersectionMinimumAlongUp : null,
                intersectionMaximumAlongUp = reach.IntersectionEvaluated ? (double?)reach.IntersectionMaximumAlongUp : null,
                left = BuildPelvisLegFact(reach.Left),
                right = BuildPelvisLegFact(reach.Right)
            };

        static CharacterFootPelvisResponseObservation BuildPelvisResponseFact(PelvisResponseFrame response) =>
            new CharacterFootPelvisResponseObservation
            {
                evaluated = response.Evaluated,
                completed = response.Completed,
                integratedOutput = response.Evaluated ? (double?)response.IntegratedOutput : null,
                hadPreviousState = response.HadPreviousState,
                supportChanged = response.SupportChanged,
                velocityReset = response.VelocityReset,
                previousTarget = response.Evaluated ? (double?)response.PreviousTarget : null,
                previousOutput = response.Evaluated ? (double?)response.PreviousOutput : null,
                previousVelocity = response.Evaluated ? (double?)response.PreviousVelocity : null,
                input = response.Evaluated ? (double?)response.Input : null,
                inputVelocity = response.Evaluated ? (double?)response.InputVelocity : null,
                frequency = response.Evaluated ? (double?)response.Frequency : null,
                target = response.Evaluated ? (double?)response.Target : null,
                output = response.Evaluated ? (double?)response.Output : null,
                velocity = response.Evaluated ? (double?)response.Velocity : null,
                positionWeight = response.Evaluated ? (double?)response.PositionWeight : null,
                previousSlope = response.PreviousSlope.ToString(),
                handoff = response.Handoff.ToString(),
                appliedOffsetAlongUp = response.Evaluated ? (double?)(response.Output * response.PositionWeight) : null
            };

        sealed class PelvisFrame
        {
            internal PelvisObservationFrame Observation;
            internal PelvisPostureFrame Posture;
            internal PelvisReachFrame Reach;
            internal PelvisResponseFrame Response;
            internal bool SameAs(PelvisFrame other) =>
                Observation.SameAs(other.Observation) && Posture.SameAs(other.Posture) &&
                Reach.SameAs(other.Reach) && Response.SameAs(other.Response);
        }

        sealed class PelvisObservationFrame
        {
            internal bool PoseInputAvailable;
            internal Vector3 PoseRootWorldPosition;
            internal Vector3 AnimatedWorldPosition;
            internal Vector3 AnimatedComponentPosition;
            internal Vector3 PhysicalWorldPosition;
            internal bool GoalResidualAvailable;
            internal float GoalResidual;
            internal bool SameAs(PelvisObservationFrame other) =>
                PoseInputAvailable == other.PoseInputAvailable &&
                PoseRootWorldPosition.Equals(other.PoseRootWorldPosition) &&
                AnimatedWorldPosition.Equals(other.AnimatedWorldPosition) &&
                AnimatedComponentPosition.Equals(other.AnimatedComponentPosition) &&
                PhysicalWorldPosition.Equals(other.PhysicalWorldPosition) &&
                GoalResidualAvailable == other.GoalResidualAvailable && GoalResidual.Equals(other.GoalResidual);
        }

        sealed class PelvisPostureFrame
        {
            internal bool Evaluated;
            internal bool Available;
            internal Vector3 Hip;
            internal Vector3 AnimatedAnkle;
            internal Vector3 TargetAnkle;
            internal float LegLength;
            internal float CompressionReserve;
            internal float UsableLegLength;
            internal float MinimumAlongUp;
            internal float MaximumAlongUp;
            internal float OffsetAlongUp;
            internal bool TargetAdjusted;
            internal bool SameAs(PelvisPostureFrame other) =>
                Evaluated == other.Evaluated &&
                Available == other.Available &&
                Hip.Equals(other.Hip) &&
                AnimatedAnkle.Equals(other.AnimatedAnkle) &&
                TargetAnkle.Equals(other.TargetAnkle) &&
                LegLength == other.LegLength &&
                CompressionReserve == other.CompressionReserve &&
                UsableLegLength == other.UsableLegLength &&
                MinimumAlongUp == other.MinimumAlongUp &&
                MaximumAlongUp == other.MaximumAlongUp &&
                OffsetAlongUp == other.OffsetAlongUp &&
                TargetAdjusted == other.TargetAdjusted;
        }

        sealed class PelvisLegFrame
        {
            internal CharacterFootPelvisLegReachRole Role;
            internal CharacterFootPelvisLegReachStatus Status;
            internal ulong EventIdentity;
            internal Vector3 Hip;
            internal Vector3 TargetAnkle;
            internal float LegLength;
            internal float MinimumCompressionReserve;
            internal float UsableLegLength;
            internal float MinimumAlongUp;
            internal float MaximumAlongUp;
            internal bool Requested;
            internal bool Available;
            internal bool FootTarget => (Role & CharacterFootPelvisLegReachRole.FootTarget) != 0;
            internal bool PrimarySupport => (Role & CharacterFootPelvisLegReachRole.PrimarySupport) != 0;
            internal bool SameAs(PelvisLegFrame other) =>
                Role == other.Role &&
                Status == other.Status &&
                EventIdentity == other.EventIdentity &&
                Hip.Equals(other.Hip) &&
                TargetAnkle.Equals(other.TargetAnkle) &&
                LegLength == other.LegLength &&
                MinimumCompressionReserve == other.MinimumCompressionReserve &&
                UsableLegLength == other.UsableLegLength &&
                MinimumAlongUp == other.MinimumAlongUp &&
                MaximumAlongUp == other.MaximumAlongUp &&
                Requested == other.Requested &&
                Available == other.Available;
        }

        sealed class PelvisReachFrame
        {
            internal Vector3 ComponentUp;
            internal CharacterFootPelvisReachStatus Status;
            internal bool IntersectionEvaluated;
            internal float IntersectionMinimumAlongUp;
            internal float IntersectionMaximumAlongUp;
            internal PelvisLegFrame Left;
            internal PelvisLegFrame Right;
            internal bool SameAs(PelvisReachFrame other) =>
                ComponentUp.Equals(other.ComponentUp) &&
                Status == other.Status &&
                IntersectionEvaluated == other.IntersectionEvaluated &&
                IntersectionMinimumAlongUp == other.IntersectionMinimumAlongUp &&
                IntersectionMaximumAlongUp == other.IntersectionMaximumAlongUp &&
                Left.SameAs(other.Left) &&
                Right.SameAs(other.Right);
        }

        sealed class PelvisResponseFrame
        {
            internal bool Evaluated;
            internal bool Completed;
            internal float IntegratedOutput;
            internal bool HadPreviousState;
            internal bool SupportChanged;
            internal bool VelocityReset;
            internal float PreviousTarget;
            internal float PreviousOutput;
            internal float PreviousVelocity;
            internal float Input;
            internal float InputVelocity;
            internal float Frequency;
            internal float Target;
            internal float Output;
            internal float Velocity;
            internal float PositionWeight;
            internal CharacterFootStrideSlope PreviousSlope;
            internal CharacterFootPelvisSpringHandoffReason Handoff;
            internal bool SameAs(PelvisResponseFrame other) =>
                Evaluated == other.Evaluated &&
                Completed == other.Completed &&
                IntegratedOutput == other.IntegratedOutput &&
                HadPreviousState == other.HadPreviousState &&
                SupportChanged == other.SupportChanged &&
                VelocityReset == other.VelocityReset &&
                PreviousTarget == other.PreviousTarget &&
                PreviousOutput == other.PreviousOutput &&
                PreviousVelocity == other.PreviousVelocity &&
                Input == other.Input &&
                InputVelocity == other.InputVelocity &&
                Frequency == other.Frequency &&
                Target == other.Target &&
                Output == other.Output &&
                Velocity == other.Velocity &&
                PositionWeight == other.PositionWeight &&
                PreviousSlope == other.PreviousSlope &&
                Handoff == other.Handoff;
        }

        sealed class PelvisHeightTargetFrame
        {
            internal bool Available;
            internal Vector3 ComponentUp;
            internal Vector3 LeftAnimatedSole;
            internal Vector3 RightAnimatedSole;
            internal Vector3 LeftTargetSole;
            internal Vector3 RightTargetSole;
            internal float AnimatedMinimumAlongUp;
            internal float TargetMinimumAlongUp;
            internal float RequestedOffsetAlongUp;

            internal void RequireValid(FootFrame frame)
            {
                if (!Available)
                {
                    if (!ComponentUp.Equals(Vector3.zero) ||
                        !LeftAnimatedSole.Equals(Vector3.zero) || !RightAnimatedSole.Equals(Vector3.zero) ||
                        !LeftTargetSole.Equals(Vector3.zero) || !RightTargetSole.Equals(Vector3.zero) ||
                        AnimatedMinimumAlongUp != 0f || TargetMinimumAlongUp != 0f || RequestedOffsetAlongUp != 0f)
                        throw new InvalidDataException(
                            $"Foot Motion unavailable Pelvis height target is not default Frame={frame.Frame} Side={frame.Side}.");
                    return;
                }
                float animatedMinimum = Mathf.Min(Vector3.Dot(LeftAnimatedSole, ComponentUp),
                    Vector3.Dot(RightAnimatedSole, ComponentUp));
                float targetMinimum = Mathf.Min(Vector3.Dot(LeftTargetSole, ComponentUp),
                    Vector3.Dot(RightTargetSole, ComponentUp));
                if (!FiniteVector(ComponentUp) || Math.Abs(ComponentUp.sqrMagnitude - 1f) > RuntimeGeometryEpsilon ||
                    !FiniteVector(LeftAnimatedSole) || !FiniteVector(RightAnimatedSole) ||
                    !FiniteVector(LeftTargetSole) || !FiniteVector(RightTargetSole) ||
                    !float.IsFinite(AnimatedMinimumAlongUp) || !float.IsFinite(TargetMinimumAlongUp) ||
                    !float.IsFinite(RequestedOffsetAlongUp) || !float.IsFinite(animatedMinimum) ||
                    !float.IsFinite(targetMinimum) ||
                    Math.Abs(AnimatedMinimumAlongUp - animatedMinimum) > RuntimeGeometryEpsilon ||
                    Math.Abs(TargetMinimumAlongUp - targetMinimum) > RuntimeGeometryEpsilon ||
                    Math.Abs(RequestedOffsetAlongUp - (targetMinimum - animatedMinimum)) > RuntimeGeometryEpsilon)
                    throw new InvalidDataException(
                        $"Foot Motion Pelvis height target is inconsistent Frame={frame.Frame} Side={frame.Side} " +
                        $"AnimatedMinimum={AnimatedMinimumAlongUp:R}/{animatedMinimum:R} " +
                        $"TargetMinimum={TargetMinimumAlongUp:R}/{targetMinimum:R} RequestedOffset={RequestedOffsetAlongUp:R}.");
            }

            internal bool SameAs(PelvisHeightTargetFrame other) =>
                Available == other.Available && ComponentUp.Equals(other.ComponentUp) &&
                LeftAnimatedSole.Equals(other.LeftAnimatedSole) && RightAnimatedSole.Equals(other.RightAnimatedSole) &&
                LeftTargetSole.Equals(other.LeftTargetSole) && RightTargetSole.Equals(other.RightTargetSole) &&
                AnimatedMinimumAlongUp == other.AnimatedMinimumAlongUp &&
                TargetMinimumAlongUp == other.TargetMinimumAlongUp && RequestedOffsetAlongUp == other.RequestedOffsetAlongUp;

            internal CharacterFootPelvisHeightTargetObservation ToFact(FootFrame frame) =>
                new CharacterFootPelvisHeightTargetObservation
                {
                    frame = frame.Frame,
                    completionIdentity = frame.CompletionIdentity.ToString(CultureInfo.InvariantCulture),
                    strideState = frame.StrideState,
                    available = Available,
                    componentUp = Available ? CharacterFootVectorFact.From(ComponentUp) : null,
                    leftAnimatedSole = Available ? CharacterFootVectorFact.From(LeftAnimatedSole) : null,
                    rightAnimatedSole = Available ? CharacterFootVectorFact.From(RightAnimatedSole) : null,
                    leftTargetSole = Available ? CharacterFootVectorFact.From(LeftTargetSole) : null,
                    rightTargetSole = Available ? CharacterFootVectorFact.From(RightTargetSole) : null,
                    animatedMinimumAlongUp = Available ? (double?)AnimatedMinimumAlongUp : null,
                    targetMinimumAlongUp = Available ? (double?)TargetMinimumAlongUp : null,
                    requestedOffsetAlongUp = Available ? (double?)RequestedOffsetAlongUp : null
                };
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
            public double originalExtensionRatio;
            public double targetExtensionRatio;
            public double solvedExtensionRatio;
            public double originalCompressionReserveMeters;
            public double actualTargetCompressionReserveMeters;
            public double solvedCompressionReserveMeters;
            public bool runtimeReachEvaluated;
            public bool runtimeReachAvailable;
            public bool resolvedReachRequestAvailable;
            public string resolvedReachEventIdentity;
            public double resolvedReachLegLengthMeters;
            public double resolvedReachMinimumCompressionReserveMeters;
            public bool primarySupportAvailable;
            public string primarySupportSide;
            public string primarySupportLandingEventIdentity;
            public string strideState;
            public string strideSupportSide;
            public bool pelvisReachObservationEvaluated;
            public double pelvisReachObservationMinimumAlongUpMeters;
            public double pelvisReachObservationMaximumAlongUpMeters;
            public bool pelvisReachObservationIntersectionExists;
            public double intersectionMinimumAlongUpMeters;
            public double intersectionMaximumAlongUpMeters;
            public double pelvisReachObservationConflictGapMeters;

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
                    originalExtensionRatio = frame.OriginalExtensionRatio,
                    targetExtensionRatio = frame.TargetExtensionRatio,
                    solvedExtensionRatio = frame.SolvedExtensionRatio,
                    originalCompressionReserveMeters =
                        frame.OriginalCompressionReserve,
                    actualTargetCompressionReserveMeters =
                        frame.TargetCompressionReserve,
                    solvedCompressionReserveMeters =
                        frame.SolvedCompressionReserve,
                    runtimeReachEvaluated = frame.LandingReachEvaluated,
                    runtimeReachAvailable = frame.LandingReachAvailable,
                    resolvedReachRequestAvailable =
                        frame.Resolved.LandingReachAvailable,
                    resolvedReachEventIdentity =
                        frame.Resolved.LandingReachEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    resolvedReachLegLengthMeters =
                        frame.Resolved.LandingReachLegLength,
                    resolvedReachMinimumCompressionReserveMeters =
                        frame.Resolved.LandingReachMinimumCompressionReserve,
                    primarySupportAvailable =
                        frame.PrimarySupportAvailable,
                    primarySupportSide = frame.PrimarySupportSide,
                    primarySupportLandingEventIdentity =
                        frame.PrimarySupportEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    strideState = frame.StrideState,
                    strideSupportSide = frame.StrideSupportSide,
                    pelvisReachObservationEvaluated =
                        frame.Pelvis.Reach.IntersectionEvaluated,
                    pelvisReachObservationMinimumAlongUpMeters =
                        frame.Pelvis.Reach.IntersectionMinimumAlongUp,
                    pelvisReachObservationMaximumAlongUpMeters =
                        frame.Pelvis.Reach.IntersectionMaximumAlongUp,
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
                double output = frame.Pelvis.Response.Output * frame.Pelvis.Response.PositionWeight;
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
                if (!frame.Pelvis.Reach.IntersectionEvaluated)
                {
                    result.classification = "NoPelvisReachObservationLandingOnly";
                    return result;
                }
                double intersectionMinimum = Math.Max(
                    minimum,
                    frame.Pelvis.Reach.IntersectionMinimumAlongUp);
                double intersectionMaximum = Math.Min(
                    maximum,
                    frame.Pelvis.Reach.IntersectionMaximumAlongUp);
                result.intersectionMinimumAlongUpMeters =
                    intersectionMinimum;
                result.intersectionMaximumAlongUpMeters =
                    intersectionMaximum;
                result.pelvisReachObservationIntersectionExists =
                    intersectionMinimum <= intersectionMaximum;
                result.pelvisReachObservationConflictGapMeters = Math.Max(
                    0d,
                    intersectionMinimum - intersectionMaximum);
                result.classification = result.pelvisReachObservationIntersectionExists
                    ? "PelvisReachObservationIntersection"
                    : "PelvisReachObservationConflict";
                return result;
            }
        }

        sealed class ContactAnchorFrame
        {
            internal bool Available;
            internal ulong Event;
            internal ulong AcquiredFrame;
            internal ulong AcquiredCompletion;
            internal ulong WorldRevision;
            internal int Surface;
            internal Vector3 Point;
            internal Vector3 Normal;

            internal static ContactAnchorFrame From(
                FootFrame frame, bool previous) => new ContactAnchorFrame
            {
                Available = previous ? frame.PreviousContactAnchorAvailable :
                    frame.CurrentContactAnchorAvailable,
                Event = previous ? frame.PreviousContactAnchorEventIdentity :
                    frame.CurrentContactAnchorEventIdentity,
                AcquiredFrame = previous ?
                    frame.PreviousContactAnchorAcquiredFrameSequence :
                    frame.CurrentContactAnchorAcquiredFrameSequence,
                AcquiredCompletion = previous ?
                    frame.PreviousContactAnchorAcquiredCompletionIdentity :
                    frame.CurrentContactAnchorAcquiredCompletionIdentity,
                WorldRevision = previous ?
                    frame.PreviousContactAnchorWorldRevision :
                    frame.CurrentContactAnchorWorldRevision,
                Surface = previous ? frame.PreviousContactAnchorSurfaceIdentity :
                    frame.CurrentContactAnchorSurfaceIdentity,
                Point = previous ? frame.PreviousContactAnchorPoint :
                    frame.CurrentContactAnchorPoint,
                Normal = previous ? frame.PreviousContactAnchorNormal :
                    frame.CurrentContactAnchorNormal
            };

            internal bool SameAs(ContactAnchorFrame other) =>
                Available == other.Available && Event == other.Event &&
                AcquiredFrame == other.AcquiredFrame &&
                AcquiredCompletion == other.AcquiredCompletion &&
                WorldRevision == other.WorldRevision && Surface == other.Surface &&
                Point.Equals(other.Point) && Normal.Equals(other.Normal);

            internal void RequireValid(FootFrame frame, string label)
            {
                bool valid = FiniteVector(Point) && FiniteVector(Normal) &&
                    (Available
                        ? Event != 0 && AcquiredFrame != 0 &&
                          AcquiredFrame <= (ulong)frame.Frame &&
                          AcquiredCompletion != 0 && WorldRevision != 0 &&
                          Surface != 0 &&
                          Math.Abs(Normal.magnitude - 1f) <= RuntimeGeometryEpsilon
                        : Event == 0 && AcquiredFrame == 0 &&
                          AcquiredCompletion == 0 && WorldRevision == 0 &&
                          Surface == 0 && Point.Equals(Vector3.zero) &&
                          Normal.Equals(Vector3.zero));
                if (!valid)
                    throw new InvalidDataException(
                        $"Foot Motion {label} Anchor snapshot is invalid " +
                        $"Frame={frame.Frame} Side={frame.Side}.");
            }
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
            public double selectedApproachContactToLandingProgress;
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
                CharacterFootStepCandidateSample closerFrame = null;
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
                    selectedEventPhase = frame.SelectedPhase.EventPhase,
                    selectedApproachContactToLandingProgress =
                        frame.SelectedPhase.ApproachContactToLandingProgress,
                    selectedLandingPhase =
                        frame.SelectedPhase.LandingPhase,
                    selectedAtOrAfterApproachContact =
                        frame.SelectedPhase.AtOrAfterApproachContact,
                    selectedInApproachContactToLanding =
                        frame.SelectedPhase.InApproachContactToLanding,
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
                CharacterFootStepCandidateSample source,
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
                        source.ApproachContactToLandingProgress,
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
            public List<CharacterFootPelvisFrameObservation> pelvisFrames;
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
            public string profileId;
            public string profileRevision;
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
            public double groundPenetrationToleranceMeters;
            public double penetrationGeometryEpsilonMeters;
            public double contactSupportGapThresholdMeters;
            public double contactSupportTouchToleranceMeters;
            public double contactSupportGapPersistentSeconds;
        }

        [Serializable]
        sealed class CoverageFact
        {
            public int contactSupportRequestedFrameCount;
            public int contactSupportGapAvailableFrameCount;
            public int contactSupportGapNotApplicableFrameCount;
            public int contactSupportGapUnavailableFrameCount;
            public int contactSupportGapIntervalCount;
            public int landingEventCount;
            public int landingStateBoundaryCount;
            public int landingStateSpanCount;
            public int lockedEventCount;
            public int lockedFullAnchorEventCount;
            public int lockedSlidingEventCount;
            public int releaseEventCount;
            public int pathRevisionOutputJumpCount;
            public int pathContinuityEventCount;
            public int stableSwingOutputJumpCount;
            public int contactStateOutputJumpCount;
            public int swingToLandingFloorHandoffCount;
            public int plantInterpolationOutputJumpCount;
            public int contactAcquisitionContinuityCount;
            public int lockWeightCompletionEventCount;
            public int approachProgressOwnershipCount;
            public int actionHardOwnershipCount;
            public int contactTransitionContextCount;
            public int formalGoalWeightPolicyCount;
            public int contactReentryOutputGeometryCount;
            public int postTransitionUnevaluatedCount;
            public int reentryOutputFactsUnavailableCount;
            public int stableSwingCorrectionResponseCadenceCount;
            public int actualFootEnvelopeCounterfactualCount;
            public int lateApproachLandingRevisionCount;
            public int supportChangeCount;
            public int contactPlanePenetrationEventCount;
            public int stepTimeCandidateSelectionCount;
            public int stepTimeCandidateRepresentativeEventCount;
            public int normalizedTimeWrapCount;
            public int landingObservationCount;
            public int futureLandingQueryCount;
            public int currentContactVerificationQueryCount;
            public int currentSupportQueryCount;
            public int predictionMotionCount;
            public int predictionMotionUnavailableCount;
            public int predictionMotionResetCount;
            public int predictionCurrentResponseCount;
            public int predictionContinuationResponseCount;
            public int predictionMaximumSpeedClampCount;
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
                    visibleOutputJump = null,
                CharacterFootCorrectionResponseCadenceAnalysis
                    correctionResponseCadence = null,
                CharacterFootContactAcquisitionContinuityAnalysis
                    contactAcquisitionContinuity = null,
                CharacterFootLockWeightCompletionAnalysis
                    lockWeightCompletion = null,
                CharacterFootContactSupportGapSequence contactSupportGap = null)
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
                this.correctionResponseCadence = correctionResponseCadence;
                this.contactAcquisitionContinuity =
                    contactAcquisitionContinuity;
                this.lockWeightCompletion = lockWeightCompletion;
                this.contactSupportGap = contactSupportGap;
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
            public CharacterFootCorrectionResponseCadenceAnalysis
                correctionResponseCadence;
            public CharacterFootContactAcquisitionContinuityAnalysis
                contactAcquisitionContinuity;
            public CharacterFootLockWeightCompletionAnalysis
                lockWeightCompletion;
            public CharacterFootContactSupportGapSequence contactSupportGap;

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
