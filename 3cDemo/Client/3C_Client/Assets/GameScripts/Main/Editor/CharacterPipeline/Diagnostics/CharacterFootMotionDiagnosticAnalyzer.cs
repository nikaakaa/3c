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
        const string AnalyzerId = "character-foot-motion-fact-analyzer";
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
                                     frames[i - 1].BodyCorrection.ResetSequence ==
                                     current.BodyCorrection.ResetSequence &&
                                     frames[i - 1].Identity.ProgramIdentity ==
                                     current.Identity.ProgramIdentity &&
                                     frames[i - 1].Identity.ProjectionRevision ==
                                     current.Identity.ProjectionRevision &&
                                     frames[i - 1].Identity.PoseGraphRevision ==
                                     current.Identity.PoseGraphRevision &&
                                     frames[i - 1].Identity.ProfileRevision ==
                                     current.Identity.ProfileRevision
                    ? frames[i - 1]
                    : null;
                bool contextMatchesPreviousFrame = previous == null ||
                    current.Lifecycle.PreviousLockRequestAvailable &&
                    current.Lifecycle.PreviousLockRequested ==
                        previous.Lifecycle.CurrentLockRequested &&
                    current.Lifecycle.PreviousLockRequestEventIdentity ==
                        previous.Lifecycle.CurrentLockRequestEventIdentity &&
                    current.Lifecycle.PreviousLockRequestMode ==
                        previous.Lifecycle.CurrentLockRequestMode &&
                    Math.Abs(
                        current.Lifecycle.PreviousLockRequestWeight -
                        previous.Lifecycle.CurrentLockRequestWeight) <= TimeEpsilon &&
                    Math.Abs(
                        current.Lifecycle.PreviousContactEdgeSeconds -
                        previous.Lifecycle.CurrentContactEdgeSeconds) <= TimeEpsilon &&
                    current.Lifecycle.PreviousLatestContactEventIdentity ==
                        previous.Lifecycle.CurrentLatestContactEventIdentity &&
                    current.Lifecycle.PreviousLatestReleasedContactEventIdentity ==
                        previous.Lifecycle.CurrentLatestReleasedContactEventIdentity &&
                    current.Lifecycle.PreviousCompletedLockWeightEventIdentity ==
                        previous.Lifecycle.CurrentCompletedLockWeightEventIdentity &&
                    current.Lifecycle.PreviousContactAnchorAvailable ==
                        previous.Lifecycle.CurrentContactAnchorAvailable &&
                    current.Lifecycle.PreviousContactAnchorEventIdentity ==
                        previous.Lifecycle.CurrentContactAnchorEventIdentity &&
                    ContactAnchorFrame.From(current, true).SameAs(
                        ContactAnchorFrame.From(previous, false));
                if (!contextMatchesPreviousFrame)
                {
                    throw new InvalidDataException(
                        $"Foot Motion committed Lifecycle Transition context did not continue " +
                        $"Frame={current.Identity.FrameSequence} Side={current.Identity.Side}.");
                }
                bool actionOccupied = current.Action.InstanceIdentity(current.Identity.Side) != 0 ||
                                      current.Action.FootWeight(current.Identity.Side) >
                                      RuntimeGeometryEpsilon;
                bool groundedAuthoritative = current.Action.Grounded &&
                                             current.CurrentStep.IsAuthoritative;
                bool actionIndependentOwnership =
                    !actionOccupied || !groundedAuthoritative ||
                    !current.Lifecycle.HardOwnershipLoss &&
                    current.Lifecycle.PreTransitionReason != "OwnershipLost" &&
                    !current.Lifecycle.PreTransitionSuppressOutput &&
                    !current.Lifecycle.PreTransitionResetInterpolation;
                if (!actionIndependentOwnership)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Action occupancy incorrectly produced Hard Ownership Loss " +
                        $"Frame={current.Identity.FrameSequence} Side={current.Identity.Side}.");
                }
                events.Add(new EventFact(
                    "FormalGoalWeightPolicy", current.Identity.Side, current.Identity.FrameSequence,
                    current.Identity.FrameSequence, current.Identity.FrameSequence, ResolveEventIdentity(current),
                    current.FormalInput.SourceIdentity, current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    new SortedDictionary<string, double>(StringComparer.Ordinal)
                    {
                        ["FormalFootPlacementWeight"] =
                            current.Lifecycle.FormalFootPlacementWeight,
                        ["LockWeight"] = current.Lifecycle.CurrentLockRequestWeight,
                        ["MotionPositionWeight"] = current.MotionCore.MotionPositionWeight,
                        ["MotionRotationWeight"] = current.MotionCore.MotionRotationWeight,
                        ["ResolvedPositionWeight"] = current.Resolved.PositionWeight,
                        ["ResolvedRotationWeight"] = current.Resolved.RotationWeight,
                        ["FinalGoalPositionWeight"] = current.Goal.PositionWeight,
                        ["FinalGoalRotationWeight"] = current.Goal.RotationWeight
                    },
                    new SortedDictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["formalWeightPolicyConsistent"] = true,
                        ["ready"] = current.Resolved.Outcome == "Ready",
                        ["contactAnchorAvailable"] = current.Lifecycle.CurrentContactAnchorAvailable
                    }));
                if (actionOccupied)
                {
                    events.Add(new EventFact(
                        "ActionHardOwnership",
                        current.Identity.Side,
                        current.Identity.FrameSequence,
                        current.Identity.FrameSequence,
                        current.Identity.FrameSequence,
                        ResolveEventIdentity(current),
                        current.FormalInput.SourceIdentity,
                        current.FormalInput.SourceCycle,
                        DeltaSeconds(current),
                        new SortedDictionary<string, double>(
                            StringComparer.Ordinal)
                        {
                            ["ActionFootWeight"] = current.Action.FootWeight(current.Identity.Side),
                            ["FormalFootPlacementWeight"] =
                                current.Lifecycle.FormalFootPlacementWeight,
                            ["MotionPositionWeight"] =
                                current.MotionCore.MotionPositionWeight,
                            ["MotionRotationWeight"] =
                                current.MotionCore.MotionRotationWeight,
                            ["ResolvedPositionWeight"] =
                                current.Resolved.PositionWeight,
                            ["ResolvedRotationWeight"] =
                                current.Resolved.RotationWeight
                        },
                        new SortedDictionary<string, bool>(
                            StringComparer.Ordinal)
                        {
                            ["actionOccupied"] = true,
                            ["grounded"] = current.Action.Grounded,
                            ["currentStepAuthoritative"] =
                                current.CurrentStep.IsAuthoritative,
                            ["hardOwnershipLoss"] =
                                current.Lifecycle.HardOwnershipLoss,
                            ["preTransitionSuppressOutput"] =
                                current.Lifecycle.PreTransitionSuppressOutput,
                            ["preTransitionResetInterpolation"] =
                                current.Lifecycle.PreTransitionResetInterpolation,
                            ["postTransitionSuppressOutput"] =
                                current.Lifecycle.PostTransitionSuppressOutput,
                            ["postTransitionEvaluated"] =
                                current.Lifecycle.PostTransitionEvaluated,
                            ["postTransitionResetInterpolation"] =
                                current.Lifecycle.PostTransitionResetInterpolation,
                            ["actionIndependentOwnership"] =
                                actionIndependentOwnership
                        }));
                }
                bool reentryGeometryAvailable =
                    current.Lifecycle.SameEventContactReentryRefreshed &&
                    current.Response.PreviousResponseOutputAvailable &&
                    current.OutputStages.PlantInterpolationEvaluated &&
                    current.Response.CorrectionResponseEvaluated &&
                    current.Resolved.Outcome == "Ready";
                if (reentryGeometryAvailable)
                {
                    Vector3 capturedOutput = current.Response.PlantSelectedWorldTarget +
                        current.Response.PlantWorldResidualCapturedBeforeDecay;
                    events.Add(new EventFact(
                        "ContactReentryOutputGeometry", current.Identity.Side,
                        previous?.Identity.FrameSequence ?? current.Identity.FrameSequence, current.Identity.FrameSequence,
                        current.Identity.FrameSequence, current.Lifecycle.CurrentContactAnchorEventIdentity,
                        current.FormalInput.SourceIdentity, current.FormalInput.SourceCycle,
                        DeltaSeconds(current),
                        new SortedDictionary<string, double>(StringComparer.Ordinal)
                        {
                            ["CapturedTargetToPreviousResponseDistanceMeters"] =
                                Vector3.Distance(capturedOutput,
                                    current.Response.PreviousResponseOutputPoint),
                            ["ResidualDecayStepMeters"] = Vector3.Distance(
                                current.Response.PlantWorldResidualCapturedBeforeDecay,
                                current.Response.PlantWorldResidualAfterDecay),
                            ["CapturedTargetToDesiredStepMeters"] =
                                Vector3.Distance(capturedOutput,
                                    current.Response.DesiredOutputPoint),
                            ["DesiredToResponseStepMeters"] = Vector3.Distance(
                                current.Response.DesiredOutputPoint, current.Response.ResponseOutputPoint),
                            ["PreviousResponseToResponseStepMeters"] =
                                Vector3.Distance(current.Response.PreviousResponseOutputPoint,
                                    current.Response.ResponseOutputPoint),
                            ["ResponseToFinalSoleStepMeters"] = Vector3.Distance(
                                current.Response.ResponseOutputPoint, current.Resolved.FinalSole)
                        },
                        new SortedDictionary<string, bool>(StringComparer.Ordinal)
                        {
                            ["sameEventReentryGeometryAvailable"] = true,
                            ["residualCaptured"] =
                                current.Response.PlantResidualCaptureReason != "None",
                            ["residualDecayApplied"] =
                                current.Response.PlantWorldResidualDecayApplied,
                            ["reentryInterpolationHistoryRetained"] =
                                current.Lifecycle.ReentryInterpolationHistoryRetained
                        }));
                }
                bool contactRelevant = current.Lifecycle.ContactEdge != "None" ||
                    current.Lifecycle.PreviousContactAnchorAvailable ||
                    current.Lifecycle.CurrentContactAnchorAvailable ||
                    current.Lifecycle.PreviousLatestContactEventIdentity != 0 ||
                    current.Lifecycle.CurrentLatestContactEventIdentity != 0 ||
                    current.Lifecycle.PreviousLatestReleasedContactEventIdentity != 0 ||
                    current.Lifecycle.CurrentLatestReleasedContactEventIdentity != 0 ||
                    current.Lifecycle.SameEventContactReentryRefreshed ||
                    current.Lifecycle.SameEventContactReentryUnavailable;
                if (!contactRelevant)
                    continue;
                events.Add(new EventFact(
                    "ContactTransitionContext",
                    current.Identity.Side,
                    previous?.Identity.FrameSequence ?? current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Lifecycle.CurrentLockRequestEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    new SortedDictionary<string, double>(
                        StringComparer.Ordinal)
                    {
                        ["PreviousLockRequestWeight"] =
                            current.Lifecycle.PreviousLockRequestWeight,
                        ["CurrentLockRequestWeight"] =
                            current.Lifecycle.CurrentLockRequestWeight,
                        ["PreviousContactEdgeSeconds"] =
                            current.Lifecycle.PreviousContactEdgeSeconds,
                        ["CurrentContactEdgeSeconds"] =
                            current.Lifecycle.CurrentContactEdgeSeconds
                    },
                    new SortedDictionary<string, bool>(
                        StringComparer.Ordinal)
                    {
                        ["transitionContractConsistent"] = true,
                        ["postTransitionEvaluated"] =
                            current.Lifecycle.PostTransitionEvaluated,
                        ["reentryOutputFactsAvailable"] =
                            reentryGeometryAvailable,
                        ["contextMatchesPreviousFrame"] =
                            contextMatchesPreviousFrame,
                        ["previousLockRequested"] =
                            current.Lifecycle.PreviousLockRequested,
                        ["currentLockRequested"] =
                            current.Lifecycle.CurrentLockRequested,
                        ["contactEdgeRising"] =
                            current.Lifecycle.ContactEdge == "Rising",
                        ["contactEdgeFalling"] =
                            current.Lifecycle.ContactEdge == "Falling",
                        ["contactEdgeEventChanged"] =
                            current.Lifecycle.ContactEdge == "EventChanged",
                        ["sameEventContactReentryRefreshed"] =
                            current.Lifecycle.SameEventContactReentryRefreshed,
                        ["sameEventContactReentryUnavailable"] =
                            current.Lifecycle.SameEventContactReentryUnavailable,
                        ["retainedVerifiedAnchor"] =
                            current.Lifecycle.RetainedVerifiedAnchor,
                        ["reentryInterpolationHistoryRetained"] =
                            current.Lifecycle.ReentryInterpolationHistoryRetained,
                        ["previousAnchorAvailable"] =
                            current.Lifecycle.PreviousContactAnchorAvailable,
                        ["currentAnchorAvailable"] =
                            current.Lifecycle.CurrentContactAnchorAvailable
                    }));
            }
        }

        static CharacterFootContactSupportGapFrame ResolveContactSupportGap(
            FootFrame frame)
        {
            var fact = new CharacterFootContactSupportGapFrame
            {
                frame = frame.Identity.FrameSequence,
                side = frame.Identity.Side,
                requested = frame.Lifecycle.CurrentLockRequested,
                observed = frame.Lifecycle.CurrentLockRequested ||
                    frame.MotionCore.ConstraintState == "Releasing" && frame.Lifecycle.CurrentContactAnchorAvailable,
                applicable = frame.Lifecycle.CurrentLockRequested &&
                    (frame.MotionCore.ConstraintState == "Landing" || frame.MotionCore.ConstraintState == "Locked") &&
                    frame.Action.Grounded && frame.CurrentStep.IsAuthoritative &&
                    frame.Lifecycle.FormalFootPlacementWeight > 0d,
                constraintState = frame.MotionCore.ConstraintState,
                domain = ContactSupportDomain(frame),
                lockResponse = frame.MotionCore.LockResponse,
                targetKind = frame.OutputStages.PlantTargetKind,
                contactEdge = frame.Lifecycle.ContactEdge,
                positionWeight = frame.Goal.PositionWeight,
                fullPositionWeight = frame.Goal.PositionWeight >= 1f - TimeEpsilon,
                requestEventIdentity = frame.Lifecycle.CurrentLockRequestEventIdentity.ToString(
                    CultureInfo.InvariantCulture),
                anchorEventIdentity = frame.Lifecycle.CurrentContactAnchorEventIdentity.ToString(
                    CultureInfo.InvariantCulture),
                anchorSurfaceIdentity = frame.Lifecycle.CurrentContactAnchorSurfaceIdentity,
                anchorWorldRevision = frame.Lifecycle.CurrentContactAnchorWorldRevision.ToString(
                    CultureInfo.InvariantCulture),
                anchorAcquiredFrame =
                    frame.Lifecycle.CurrentContactAnchorAcquiredFrameSequence.ToString(
                        CultureInfo.InvariantCulture),
                anchorAcquiredCompletion =
                    frame.Lifecycle.CurrentContactAnchorAcquiredCompletionIdentity.ToString(
                        CultureInfo.InvariantCulture),
                anchorPoint = CharacterFootVectorFact.From(
                    frame.Lifecycle.CurrentContactAnchorPoint),
                anchorNormal = CharacterFootVectorFact.From(
                    frame.Lifecycle.CurrentContactAnchorNormal),
                formalFootPlacementWeight = frame.Lifecycle.FormalFootPlacementWeight,
                lockWeight = frame.Lifecycle.CurrentLockRequestWeight,
                deltaSeconds = frame.Timing.DeltaSeconds,
                currentSupportAvailable = frame.CurrentSupport.Available,
                currentSupportRejectReason = frame.CurrentSupport.RejectReason,
                currentSupportSurfaceIdentity = frame.CurrentSupport.Target.Surface,
                landingReachAvailable = frame.MotionCore.LandingReachAvailable,
                gapMotion = "Unavailable"
            };
            CharacterFootContactSupportGapAvailability availability =
                !fact.observed
                    ? CharacterFootContactSupportGapAvailability.NotRequested
                    : !frame.Action.Grounded || !frame.CurrentStep.IsAuthoritative
                        ? CharacterFootContactSupportGapAvailability.OwnershipUnavailable
                        : frame.Lifecycle.FormalFootPlacementWeight <= 0d
                            ? CharacterFootContactSupportGapAvailability.PlacementWeightZero
                            : !frame.Solver.PhysicalWriteAvailable ||
                              frame.Solver.PhysicalWriteCompletionIdentity !=
                              frame.Identity.CompletionIdentity
                                ? CharacterFootContactSupportGapAvailability.PhysicalPoseUnavailable
                                : !frame.Lifecycle.CurrentContactAnchorAvailable ||
                                  frame.MotionCore.ConstraintState != "Releasing" &&
                                  frame.Lifecycle.CurrentContactAnchorEventIdentity != frame.Lifecycle.CurrentLockRequestEventIdentity
                                    ? CharacterFootContactSupportGapAvailability.SameEventAnchorUnavailable
                                    : fact.domain == "Unclassified"
                                        ? CharacterFootContactSupportGapAvailability.ContactHoldingStateUnavailable
                                        : CharacterFootContactSupportGapAvailability.Available;
            fact.availability = availability.ToString();
            if (availability != CharacterFootContactSupportGapAvailability.Available)
                return fact;
            fact.qualityEligible = fact.applicable && fact.fullPositionWeight;
            if (!FiniteVector(frame.Solver.PhysicalHeelWorld) || !FiniteVector(frame.Solver.PhysicalToeWorld))
                throw new InvalidDataException(
                    $"Foot Motion Contact support gap physical pose is invalid " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            Vector3 normal = frame.Lifecycle.CurrentContactAnchorNormal.normalized;
            Vector3 point = frame.Lifecycle.CurrentContactAnchorPoint;
            double heel = Vector3.Dot(frame.Solver.PhysicalHeelWorld - point, normal);
            double toe = Vector3.Dot(frame.Solver.PhysicalToeWorld - point, normal);
            Vector3 sole = (frame.Solver.PhysicalHeelWorld + frame.Solver.PhysicalToeWorld) * 0.5f;
            fact.physicalHeel = CharacterFootVectorFact.From(frame.Solver.PhysicalHeelWorld);
            fact.physicalToe = CharacterFootVectorFact.From(frame.Solver.PhysicalToeWorld);
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

        static string ContactSupportDomain(FootFrame frame) => frame.MotionCore.ConstraintState switch
        {
            "Landing" => "Landing",
            "Locked" when frame.MotionCore.LockResponse == "FullAnchor" => "FullAnchor",
            "Locked" when frame.MotionCore.LockResponse == "Sliding" => "Sliding",
            "Releasing" => "Release",
            _ => "Unclassified"
        };

        static bool SameContactSupportGapReference(
            FootFrame previous, FootFrame current) =>
            Continuous(previous, current) &&
            ContactSupportGapAvailable(previous) &&
            ContactSupportGapAvailable(current) &&
            previous.Lifecycle.CurrentContactAnchorEventIdentity == current.Lifecycle.CurrentContactAnchorEventIdentity &&
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
                    fact.gapVelocityMetersPerSecond = frame.Timing.DeltaSeconds > 0f
                        ? (double?)(delta / frame.Timing.DeltaSeconds) : null;
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
                    "ContactSupportGapObservation", frame.Identity.Side, frame.Identity.FrameSequence, frame.Identity.FrameSequence, frame.Identity.FrameSequence,
                    frame.Lifecycle.CurrentContactAnchorEventIdentity, frame.FormalInput.SourceIdentity, frame.FormalInput.SourceCycle,
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
                while (segmentCursor < segments.Count && segments[segmentCursor].endFrame < frames[start].Identity.FrameSequence)
                    segmentCursor++;
                var members = new List<EventFact>();
                while (segmentCursor < segments.Count && segments[segmentCursor].endFrame <= frames[index].Identity.FrameSequence)
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
                    double dt = frames[i].Timing.DeltaSeconds;
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
                    if (previousAbove) runDuration += frames[i].Timing.DeltaSeconds;
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
                next.MotionCore.ConstraintState == "Releasing" || !next.Lifecycle.CurrentLockRequested ? "FormalContactExit" :
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
            return new EventFact(kind, first.Identity.Side, first.Identity.FrameSequence, last.Identity.FrameSequence, frames[peak].Identity.FrameSequence,
                first.Lifecycle.CurrentContactAnchorEventIdentity, first.FormalInput.SourceIdentity, first.FormalInput.SourceCycle, duration,
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
                    nextContactRequested = adjacentNext ? (bool?)next.Lifecycle.CurrentLockRequested : null,
                    nextFrame = adjacentNext ? (int?)next.Identity.FrameSequence : null
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
                    previous.FormalInput.SourceIdentity == current.FormalInput.SourceIdentity &&
                    previous.FormalInput.SourceCycle == current.FormalInput.SourceCycle;
                float progressDelta = sameLineage
                    ? current.InputEvents.ApproachProgress -
                      previous.InputEvents.ApproachProgress
                    : 0f;
                bool progressMonotonic = !sameLineage ||
                    progressDelta >= -TimeEpsilon;
                bool sameEventPlantInterpolation =
                    current.OutputStages.PlantInterpolationEvaluated &&
                    current.OutputStages.PlantTargetEventIdentity == eventIdentity;
                bool sameEventResidualCapture =
                    sameEventPlantInterpolation &&
                    current.Response.PlantResidualCaptureReason != "None";
                bool ordinarySwingDomain =
                    (current.MotionCore.ConstraintState == "Swing" ||
                     current.MotionCore.ConstraintState == "UnlockedSupport") &&
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
                         current.MotionCore.MotionPositionWeight -
                         previous.MotionCore.MotionPositionWeight) > TimeEpsilon ||
                     Math.Abs(
                         current.MotionCore.MotionRotationWeight -
                         previous.MotionCore.MotionRotationWeight) > TimeEpsilon);
                if (!ownershipConsistent)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Approach progress ownership is inconsistent " +
                        $"Frame={current.Identity.FrameSequence} Side={current.Identity.Side} " +
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
                        current.Lifecycle.FormalFootPlacementWeight,
                    ["FormalFootPlacementWeightDelta"] = sameLineage
                        ? current.Lifecycle.FormalFootPlacementWeight -
                          previous.Lifecycle.FormalFootPlacementWeight : 0d,
                    ["ApproachProgressDelta"] = progressDelta,
                    ["PreparedTargetPointStep"] = sameLineage &&
                        previous.Identity.PlantTargetAvailable &&
                        current.Identity.PlantTargetAvailable
                            ? Vector3.Distance(
                                previous.Identity.PlantTargetPoint,
                                current.Identity.PlantTargetPoint)
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
                            previous.OutputStages.FinalEffectiveCorrection,
                            current.OutputStages.FinalEffectiveCorrection)
                        : 0d,
                    ["PositionWeightDelta"] = sameLineage
                        ? current.MotionCore.MotionPositionWeight -
                          previous.MotionCore.MotionPositionWeight
                        : 0d,
                    ["RotationWeightDelta"] = sameLineage
                        ? current.MotionCore.MotionRotationWeight -
                          previous.MotionCore.MotionRotationWeight
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
                        current.Identity.PlantTargetAvailable,
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
                    current.Identity.Side,
                    previous?.Identity.FrameSequence ?? current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    eventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                    frame.Lifecycle.PreTransitionAnchorCommand == "Release")
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
                    frame.FormalInput.LockWeight >=
                    1f - RuntimeGeometryEpsilon)
                {
                    expectedCompletedEvent = requestEvent;
                }
                bool expectedPublishedLatch =
                    frame.OutputStages.PlantInterpolationEvaluated &&
                    frame.OutputStages.PlantTargetEventIdentity != 0 &&
                    frame.OutputStages.PlantTargetEventIdentity == expectedCompletedEvent;
                if (frame.OutputStages.PlantLockWeightCompleted !=
                    expectedPublishedLatch)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant lock weight completion latch is inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                        $"RequestEvent={requestEvent} PlantEvent={frame.OutputStages.PlantTargetEventIdentity} " +
                        $"Weight={frame.FormalInput.LockWeight:R} Expected={expectedPublishedLatch} " +
                        $"Actual={frame.OutputStages.PlantLockWeightCompleted}.");
                }
                if (frame.Lifecycle.PostTransitionReason == "LandingCompleted")
                {
                    bool completionConsistent =
                        frame.OutputStages.PlantLockWeightCompleted &&
                        frame.Response.PlantOutputDistance <=
                        frame.Response.PlantWorldResidualCompletionTolerance +
                        PositionNoiseFloor &&
                        frame.Response.PlantPenetrationDepth <=
                        ExpectedGroundPenetrationToleranceMeters +
                        PositionNoiseFloor &&
                        frame.MotionCore.LandingReachAvailable;
                    if (!completionConsistent)
                    {
                        throw new InvalidDataException(
                            $"Foot Motion Landing completion eligibility is inconsistent " +
                            $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                            $"Latch={frame.OutputStages.PlantLockWeightCompleted} " +
                            $"OutputDistance={frame.Response.PlantOutputDistance:R} " +
                            $"Penetration={frame.Response.PlantPenetrationDepth:R} " +
                            $"Tolerance={frame.Response.PlantWorldResidualCompletionTolerance:R} " +
                            $"LandingReach={frame.MotionCore.LandingReachAvailable}.");
                    }
                }
                releaseAppliedOnPreviousFrame =
                    frame.Lifecycle.PostTransitionAnchorCommand == "Release";
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
                        frame.OutputStages.PlantTargetEventIdentity == eventIdentity ||
                        frame.MotionCore.LandingEventIdentity == eventIdentity)
                    .OrderBy(frame => frame.Identity.FrameSequence)
                    .ToList();
                List<FootFrame> requestFrames = window
                    .Where(frame =>
                        RequestsFormalLock(frame) &&
                        frame.InputEvents.Current.Identity == eventIdentity)
                    .ToList();
                if (requestFrames.Count == 0)
                    continue;
                FootFrame firstFullWeight = requestFrames.FirstOrDefault(
                    frame => frame.FormalInput.LockWeight >=
                             1f - RuntimeGeometryEpsilon);
                bool reachedFullWeight = firstFullWeight != null;
                FootFrame completion = window.FirstOrDefault(frame =>
                    frame.Lifecycle.PostTransitionReason == "LandingCompleted" &&
                    frame.OutputStages.PlantTargetEventIdentity == eventIdentity);
                bool enteredLocked = window.Any(frame =>
                    frame.MotionCore.ConstraintState == "Locked" &&
                    (frame.MotionCore.LandingEventIdentity == eventIdentity ||
                     frame.OutputStages.PlantTargetEventIdentity == eventIdentity));
                bool completionLatch =
                    completion?.OutputStages.PlantLockWeightCompleted == true;
                bool completionReach =
                    completion?.MotionCore.LandingReachAvailable == true;
                bool completionOutputClosed = completion != null &&
                    completion.Response.PlantOutputDistance <=
                    completion.Response.PlantWorldResidualCompletionTolerance +
                    PositionNoiseFloor;
                bool completionPenetrationClosed = completion != null &&
                    completion.Response.PlantPenetrationDepth <=
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
                        frame.Identity.FrameSequence > firstFullWeight.Identity.FrameSequence &&
                        frame.OutputStages.PlantTargetEventIdentity == eventIdentity &&
                        frame.OutputStages.PlantInterpolationEvaluated &&
                        frame.FormalInput.LockWeight <
                        1f - RuntimeGeometryEpsilon &&
                        frame.OutputStages.PlantLockWeightCompleted);
                string outcome = reachedFullWeight
                    ? geometryClosedAndLocked
                        ? "FullWeightClosedAndLocked"
                        : "FullWeightNotClosedInWindow"
                    : enteredLocked
                        ? "LockedWithoutFullWeight"
                        : "NoFullWeightNoLock";
                FootFrame peak = completion ?? firstFullWeight ?? window[^1];
                float tolerance = completion != null
                    ? completion.Response.PlantWorldResidualCompletionTolerance
                    : window.Where(frame => frame.OutputStages.PlantInterpolationEvaluated)
                        .Select(frame => frame.Response.PlantWorldResidualCompletionTolerance)
                        .DefaultIfEmpty(0f)
                        .Last();
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["WindowFrameCount"] = window.Count,
                    ["RequestFrameCount"] = requestFrames.Count,
                    ["LockWeightMaximum"] = requestFrames.Max(
                        frame => frame.FormalInput.LockWeight),
                    ["LockWeightCompletionThreshold"] =
                        1f - RuntimeGeometryEpsilon,
                    ["FirstFullWeightFrame"] =
                        firstFullWeight?.Identity.FrameSequence ?? -1,
                    ["LandingCompletedFrame"] = completion?.Identity.FrameSequence ?? -1,
                    ["PlantOutputDistanceAtCompletion"] =
                        completion?.Response.PlantOutputDistance ?? 0f,
                    ["PlantPenetrationDepthAtCompletion"] =
                        completion?.Response.PlantPenetrationDepth ?? 0f,
                    ["LandingLockCompletionTolerance"] = tolerance,
                    ["GroundPenetrationTolerance"] =
                        ExpectedGroundPenetrationToleranceMeters
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["reachedFullWeight"] = reachedFullWeight,
                    ["latchObserved"] = window.Any(frame =>
                        frame.OutputStages.PlantTargetEventIdentity == eventIdentity &&
                        frame.OutputStages.PlantLockWeightCompleted),
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
                    firstFrame = window[0].Identity.FrameSequence,
                    lastFrame = window[^1].Identity.FrameSequence,
                    firstFullWeightFrame = firstFullWeight?.Identity.FrameSequence,
                    landingCompletedFrame = completion?.Identity.FrameSequence,
                    sourceIdentity = peak.FormalInput.SourceIdentity,
                    sourceCycle = peak.FormalInput.SourceCycle,
                    completionState = completion?.MotionCore.ConstraintState ??
                                      window[^1].MotionCore.ConstraintState,
                    completionPlantTargetKind =
                        completion?.OutputStages.PlantTargetKind ?? "None"
                };
                events.Add(new EventFact(
                    "LockWeightCompletionEvent",
                    peak.Identity.Side,
                    window[0].Identity.FrameSequence,
                    window[^1].Identity.FrameSequence,
                    peak.Identity.FrameSequence,
                    eventIdentity,
                    peak.FormalInput.SourceIdentity,
                    peak.FormalInput.SourceCycle,
                    Duration(window),
                    metrics,
                    evidence,
                    lockWeightCompletion: detail));
            }
        }

        static bool RequestsFormalLock(FootFrame frame) =>
            frame.InputEvents.Current.Identity != 0 &&
            frame.FormalInput.Contact > 0f &&
            frame.FormalInput.LockMode != "Unlocked";

        static void AnalyzePlantInterpolationOutputJumps(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    !current.OutputStages.PlantInterpolationEvaluated ||
                    !current.Solver.PhysicalWriteAvailable ||
                    !previous.Solver.PhysicalWriteAvailable)
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
                bool eventChanged = previous.OutputStages.PlantTargetEventIdentity !=
                                    current.OutputStages.PlantTargetEventIdentity;
                bool ownerChanged = !string.Equals(
                    previous.OutputStages.SafetyFloorOwner,
                    current.OutputStages.SafetyFloorOwner,
                    StringComparison.Ordinal);
                bool plantDesiredOutputStepAvailable =
                    previous.OutputStages.PlantInterpolationEvaluated;
                bool plantResponseOutputStepAvailable =
                    current.Response.PreviousResponseOutputAvailable;
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["FootPlacementOutputOffsetStep"] = visibleStep,
                    ["FootPlacementOutputOffsetSpeed"] = visibleSpeed,
                    ["PlantSelectedWorldTargetStep"] = Vector3.Distance(
                        previous.Response.PlantSelectedWorldTarget,
                        current.Response.PlantSelectedWorldTarget),
                    ["DesiredOutputPointStep"] =
                        plantDesiredOutputStepAvailable
                            ? Vector3.Distance(
                                previous.Response.DesiredOutputPoint,
                                current.Response.DesiredOutputPoint)
                            : 0d,
                    ["ResponseOutputPointStep"] =
                        plantResponseOutputStepAvailable
                            ? Vector3.Distance(
                                current.Response.PreviousResponseOutputPoint,
                                current.Response.ResponseOutputPoint)
                            : 0d,
                    ["PlantWorldResidualCaptureDelta"] = Vector3.Distance(
                        current.Response.PlantWorldResidualBeforeCapture,
                        current.Response.PlantWorldResidualCapturedBeforeDecay),
                    ["PlantWorldResidualCaptureContinuityError"] =
                        current.Response.PlantResidualCaptureReason != "None"
                            ? Vector3.Distance(
                                current.Response.PlantWorldResidualCapturedBeforeDecay,
                                current.MotionCore.OriginalSole +
                                current.Response.PlantEffectiveCorrectionBefore -
                                current.Response.PlantSelectedWorldTarget)
                            : Vector3.Distance(
                                current.Response.PlantWorldResidualCapturedBeforeDecay,
                                current.Response.PlantWorldResidualBeforeCapture),
                    ["PlantWorldResidualDecayStep"] = Vector3.Distance(
                        current.Response.PlantWorldResidualCapturedBeforeDecay,
                        current.Response.PlantWorldResidualAfterDecay),
                    ["PlantWorldResidualAfterDecay"] =
                        current.Response.PlantWorldResidualAfterDecay.magnitude,
                    ["PlantWorldResidualAppliedHalfLifeSeconds"] =
                        current.Response.PlantWorldResidualAppliedHalfLifeSeconds,
                    ["CorrectionResponseDesired"] =
                        current.Response.CorrectionResponseDesired,
                    ["CorrectionResponsePrevious"] =
                        current.Response.CorrectionResponsePrevious,
                    ["CorrectionResponseCurrent"] =
                        current.Response.CorrectionResponseCurrent,
                    ["CorrectionResponseSelectedSpeed"] =
                        current.Response.CorrectionResponseSelectedSpeed,
                    ["CorrectionResponseAppliedDelta"] = Math.Abs(
                        current.Response.CorrectionResponseAppliedDelta),
                    ["CorrectionResponseRequestedDirectionChangeDegrees"] =
                        DirectionAngleDegrees(
                            current.Response.CorrectionResponsePreviousDirection,
                            current.Response.CorrectionResponseRequestedDirection),
                    ["CorrectionResponseMaximumDirectionChangeDegrees"] =
                        current.Response.CorrectionResponseMaximumDirectionChangeDegrees,
                    ["CorrectionResponseAppliedDirectionChangeDegrees"] =
                        current.Response.CorrectionResponseAppliedDirectionChangeDegrees,
                    ["PlantEffectiveCorrectionStep"] = Vector3.Distance(
                        previous.Response.PlantEffectiveCorrectionAfter,
                        current.Response.PlantEffectiveCorrectionAfter),
                    ["PlantTargetAppliedVerticalDelta"] = Math.Abs(
                        current.Response.PlantTargetAppliedVerticalDelta),
                    ["PlantOutputDistance"] =
                        current.Response.PlantOutputDistance,
                    ["PlantPenetrationDepth"] =
                        current.Response.PlantPenetrationDepth,
                    ["PresentationDeltaSeconds"] = current.Timing.DeltaSeconds,
                    ["BodyTickSpan"] = current.Timing.CurrentBodyTick >=
                                       previous.Timing.CurrentBodyTick
                        ? current.Timing.CurrentBodyTick - previous.Timing.CurrentBodyTick
                        : 0d
                };
                ApplyResponseDomainMetrics(current, metrics);
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["scalarResponseEvaluated"] = ScalarResponseEvaluated(current),
                    ["contactResidualResponseEvaluated"] = ContactWorldResponse(current),
                    ["responseDomainTransferred"] = current.Response.CorrectionResponseDomainTransferred,
                    ["plantTargetEventChanged"] = eventChanged,
                    ["plantTargetKindChanged"] = !string.Equals(
                        previous.OutputStages.PlantTargetKind,
                        current.OutputStages.PlantTargetKind,
                        StringComparison.Ordinal),
                    ["plantLockResponseChanged"] = !string.Equals(
                        previous.OutputStages.PlantLockResponse,
                        current.OutputStages.PlantLockResponse,
                        StringComparison.Ordinal),
                    ["plantTargetForceRefreshed"] =
                        current.Response.PlantTargetForceRefreshed,
                    ["plantTargetVerticalClamped"] =
                        current.Response.PlantTargetVerticalClamped,
                    ["plantResidualCaptured"] =
                        current.Response.PlantResidualCaptureReason != "None",
                    ["plantWorldResidualDecayApplied"] =
                        current.Response.PlantWorldResidualDecayApplied,
                    ["plantWorldResidualDecayedOnCapture"] =
                        current.Response.PlantResidualCaptureReason != "None" &&
                        current.Response.PlantWorldResidualDecayApplied,
                    ["plantWorldResidualClearedAtCompletionTolerance"] =
                        current
                            .Response.PlantWorldResidualClearedAtCompletionTolerance,
                    ["targetHeightOwned"] = HasRevisionReason(
                        current.Response.PlantVerticalContinuityOwners,
                        "TargetHeightHistory"),
                    ["plantWorldResidualOwned"] =
                        HasRevisionReason(
                            current.Response.PlantVerticalContinuityOwners,
                            "PlantWorldResidual"),
                    ["correctionResponseOwned"] = HasRevisionReason(
                        current.Response.PlantVerticalContinuityOwners,
                        "CorrectionResponseHistory"),
                    ["plantTargetOwned"] = HasRevisionReason(
                        current.Response.PlantVerticalContinuityOwners,
                        "PlantTarget"),
                    ["correctionResponseInitializedThisFrame"] =
                        current.Response.CorrectionResponseInitializedThisFrame,
                    ["correctionResponseDirectionLimited"] =
                        current.Response.CorrectionResponseDirectionLimited,
                    ["plantDesiredOutputStepAvailable"] =
                        plantDesiredOutputStepAvailable,
                    ["plantResponseOutputStepAvailable"] =
                        plantResponseOutputStepAvailable,
                    ["safetyFloorOwnerChanged"] = ownerChanged,
                    ["physicalOutputAvailable"] = true
                };
                events.Add(new EventFact(
                    "PlantInterpolationOutputJump",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.OutputStages.PlantTargetEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                    current.Lifecycle.PreTransitionReason == "ContactAcquired" ||
                    current.Lifecycle.PreTransitionReason == "NewEventContactAcquired";
                bool previousContactOnly =
                    previous.CurrentStep.IsValid &&
                    !previous.CurrentStep.IsSwing &&
                    previous.FormalOutput.Contact >= 1f - RuntimeGeometryEpsilon;
                if (!Continuous(previous, current) ||
                    !contactAcquired ||
                    previousContactOnly ||
                    !current.HasAnchor ||
                    !current.OutputStages.PlantInterpolationEvaluated ||
                    !current.Response.PreviousResponseOutputAvailable ||
                    previous.Resolved.Outcome != "Ready" ||
                    current.Resolved.Outcome != "Ready" ||
                    current.PathContinuity.ComponentUp.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon)
                {
                    continue;
                }
                Vector3 up = current.PathContinuity.ComponentUp.normalized;
                Vector3 animationBaselineStep =
                    current.MotionCore.OriginalSole - previous.MotionCore.OriginalSole;
                Vector3 originalSoleToAnchor =
                    current.MotionCore.Anchor - current.MotionCore.OriginalSole;
                Vector3 previousVisibleToAnchor =
                    current.MotionCore.Anchor - previous.Resolved.FinalSole;
                Vector3 previousResponseToAnchor =
                    current.MotionCore.Anchor - current.Response.PreviousResponseOutputPoint;
                Vector3 desiredToResponse =
                    current.Response.ResponseOutputPoint - current.Response.DesiredOutputPoint;
                Vector3 previousVisibleToFinalOutput =
                    current.Resolved.FinalSole - previous.Resolved.FinalSole;
                Vector3 responseOutputToAnchor =
                    current.MotionCore.Anchor - current.Response.ResponseOutputPoint;
                Vector3 finalOutputToAnchor =
                    current.MotionCore.Anchor - current.Resolved.FinalSole;
                Vector3 expectedCapturedResidual =
                    current.Response.PreviousResponseOutputPoint -
                    current.Response.PlantSelectedWorldTarget;
                bool sourceContinuous = string.Equals(
                    previous.FormalInput.SourceIdentity,
                    current.FormalInput.SourceIdentity,
                    StringComparison.Ordinal) &&
                    previous.FormalInput.SourceCycle == current.FormalInput.SourceCycle;
                bool contributionContinuous =
                    previous.FormalInput.ContributionContinuityIdentity ==
                    current.FormalInput.ContributionContinuityIdentity;
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
                        current.Response.PlantWorldResidualCapturedBeforeDecay.magnitude,
                    ["ResidualAfterDecayMeters"] =
                        current.Response.PlantWorldResidualAfterDecay.magnitude,
                    ["ResidualDecayStepMeters"] = Vector3.Distance(
                        current.Response.PlantWorldResidualCapturedBeforeDecay,
                        current.Response.PlantWorldResidualAfterDecay),
                    ["ResidualCaptureContinuityErrorMeters"] =
                        Vector3.Distance(
                            expectedCapturedResidual,
                            current.Response.PlantWorldResidualCapturedBeforeDecay),
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
                            current.MotionCore.Anchor,
                            current.Response.PlantSelectedWorldTarget),
                    ["CorrectionResponseDesired"] =
                        current.Response.CorrectionResponseDesired,
                    ["CorrectionResponsePrevious"] =
                        current.Response.CorrectionResponsePrevious,
                    ["CorrectionResponseCurrent"] =
                        current.Response.CorrectionResponseCurrent,
                    ["CorrectionResponseAppliedDelta"] =
                        current.Response.CorrectionResponseAppliedDelta
                };
                ApplyResponseDomainMetrics(current, metrics);
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["scalarResponseEvaluated"] = ScalarResponseEvaluated(current),
                    ["contactResidualResponseEvaluated"] = ContactWorldResponse(current),
                    ["contactAcquired"] =
                        current.Lifecycle.PreTransitionReason == "ContactAcquired",
                    ["newEventContactAcquired"] =
                        current.Lifecycle.PreTransitionReason ==
                        "NewEventContactAcquired",
                    ["sourceContinuous"] = sourceContinuous,
                    ["contributionContinuous"] = contributionContinuous,
                    ["residualCaptured"] =
                        current.Response.PlantResidualCaptureReason != "None",
                    ["residualDecayApplied"] =
                        current.Response.PlantWorldResidualDecayApplied,
                    ["captureContinuitySatisfied"] =
                        metrics["ResidualCaptureContinuityErrorMeters"] <=
                        PositionNoiseFloor,
                    ["anchorMatchesSelectedTarget"] =
                        metrics["AnchorToSelectedTargetErrorMeters"] <=
                        PositionNoiseFloor
                };
                var detail = new CharacterFootContactAcquisitionContinuityAnalysis
                {
                    acquisitionReason = current.Lifecycle.PreTransitionReason,
                    lineageClassification = lineageClassification,
                    previousSourceIdentity = previous.FormalInput.SourceIdentity,
                    sourceIdentity = current.FormalInput.SourceIdentity,
                    previousSourceCycle = previous.FormalInput.SourceCycle,
                    sourceCycle = current.FormalInput.SourceCycle,
                    previousContributionContinuityIdentity =
                        previous.FormalInput.ContributionContinuityIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    contributionContinuityIdentity =
                        current.FormalInput.ContributionContinuityIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    previousEventIdentity = ResolveEventIdentity(previous)
                        .ToString(CultureInfo.InvariantCulture),
                    eventIdentity = ResolveEventIdentity(current)
                        .ToString(CultureInfo.InvariantCulture),
                    anchor = CharacterFootVectorFact.From(current.MotionCore.Anchor),
                    previousOriginalSole = CharacterFootVectorFact.From(
                        previous.MotionCore.OriginalSole),
                    originalSole = CharacterFootVectorFact.From(
                        current.MotionCore.OriginalSole),
                    previousVisibleOutput = CharacterFootVectorFact.From(
                        previous.Resolved.FinalSole),
                    previousResponseOutput = CharacterFootVectorFact.From(
                        current.Response.PreviousResponseOutputPoint),
                    capturedBeforeDecay = CharacterFootVectorFact.From(
                        current.Response.PlantWorldResidualCapturedBeforeDecay),
                    afterDecay = CharacterFootVectorFact.From(
                        current.Response.PlantWorldResidualAfterDecay),
                    desiredOutput = CharacterFootVectorFact.From(
                        current.Response.DesiredOutputPoint),
                    responseOutput = CharacterFootVectorFact.From(
                        current.Response.ResponseOutputPoint),
                    finalOutput = CharacterFootVectorFact.From(
                        current.Resolved.FinalSole),
                    plantResidualCaptureReason =
                        current.Response.PlantResidualCaptureReason,
                    responseDomain = ResponseDomainFact(current),
                    correctionResponseInitializationReason =
                        current.Response.CorrectionResponseInitializationReason
                };
                events.Add(new EventFact(
                    "ContactAcquisitionContinuity",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    ResolveEventIdentity(current),
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                    first.MotionCore.State != "Accepted" ||
                    previous.MotionCore.State != "Accepted" ||
                    current.MotionCore.State != "Accepted" ||
                    first.MotionCore.ConstraintState != "Swing" ||
                    previous.MotionCore.ConstraintState != "Swing" ||
                    current.MotionCore.ConstraintState != "Swing" ||
                    first.FormalInput.SourceIdentity != current.FormalInput.SourceIdentity ||
                    previous.FormalInput.SourceIdentity != current.FormalInput.SourceIdentity ||
                    first.FormalInput.SourceCycle != current.FormalInput.SourceCycle ||
                    previous.FormalInput.SourceCycle != current.FormalInput.SourceCycle ||
                    first.MotionCore.LandingEventIdentity == 0 ||
                    first.MotionCore.LandingEventIdentity !=
                    current.MotionCore.LandingEventIdentity ||
                    previous.MotionCore.LandingEventIdentity !=
                    current.MotionCore.LandingEventIdentity ||
                    first.MotionCore.GroundPathInputIdentity == 0 ||
                    first.MotionCore.GroundPathInputIdentity !=
                    current.MotionCore.GroundPathInputIdentity ||
                    previous.MotionCore.GroundPathInputIdentity !=
                    current.MotionCore.GroundPathInputIdentity ||
                    current.PathContinuity.PathRevisionReason != "None" ||
                    current.PathContinuity.PathResidualRebuilt ||
                    !first.OutputStages.OutputStagesAvailable ||
                    !previous.OutputStages.OutputStagesAvailable ||
                    !current.OutputStages.OutputStagesAvailable ||
                    !first.Response.CorrectionResponseEvaluated ||
                    !previous.Response.CorrectionResponseEvaluated ||
                    !current.Response.CorrectionResponseEvaluated)
                {
                    continue;
                }
                float previousCorrectionStep = Vector3.Distance(
                    first.OutputStages.FinalEffectiveCorrection,
                    previous.OutputStages.FinalEffectiveCorrection);
                float currentCorrectionStep = Vector3.Distance(
                    previous.OutputStages.FinalEffectiveCorrection,
                    current.OutputStages.FinalEffectiveCorrection);
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
                    previous.Response.CorrectionResponseDesired -
                    first.Response.CorrectionResponseDesired;
                float currentDesiredDelta =
                    current.Response.CorrectionResponseDesired -
                    previous.Response.CorrectionResponseDesired;
                float previousResponseOutputStep = Vector3.Distance(
                    first.Response.ResponseOutputPoint,
                    previous.Response.ResponseOutputPoint);
                float currentResponseOutputStep = Vector3.Distance(
                    previous.Response.ResponseOutputPoint,
                    current.Response.ResponseOutputPoint);
                float previousFormalHeightDelta =
                    previous.MotionCore.SwingFormalFootHeight -
                    first.MotionCore.SwingFormalFootHeight;
                float currentFormalHeightDelta =
                    current.MotionCore.SwingFormalFootHeight -
                    previous.MotionCore.SwingFormalFootHeight;
                float previousEnvelopeStep = Vector3.Distance(
                    first.MotionCore.SwingEnvelopeSample,
                    previous.MotionCore.SwingEnvelopeSample);
                float currentEnvelopeStep = Vector3.Distance(
                    previous.MotionCore.SwingEnvelopeSample,
                    current.MotionCore.SwingEnvelopeSample);
                float previousEnvelopeAlongUpDelta =
                    previous.MotionCore.SwingEnvelopeSampleAlongUp -
                    first.MotionCore.SwingEnvelopeSampleAlongUp;
                float currentEnvelopeAlongUpDelta =
                    current.MotionCore.SwingEnvelopeSampleAlongUp -
                    previous.MotionCore.SwingEnvelopeSampleAlongUp;
                float previousOriginalSoleStep = Vector3.Distance(
                    first.MotionCore.OriginalSole,
                    previous.MotionCore.OriginalSole);
                float currentOriginalSoleStep = Vector3.Distance(
                    previous.MotionCore.OriginalSole,
                    current.MotionCore.OriginalSole);
                float previousEnvelopeDirectionContribution = Vector3.Dot(
                    previous.MotionCore.SwingEnvelopeSample - first.MotionCore.SwingEnvelopeSample,
                    previous.Response.CorrectionResponseDirection);
                float currentEnvelopeDirectionContribution = Vector3.Dot(
                    current.MotionCore.SwingEnvelopeSample -
                    previous.MotionCore.SwingEnvelopeSample,
                    current.Response.CorrectionResponseDirection);
                float previousOriginalSoleDirectionContribution = -Vector3.Dot(
                    previous.MotionCore.OriginalSole - first.MotionCore.OriginalSole,
                    previous.Response.CorrectionResponseDirection);
                float currentOriginalSoleDirectionContribution = -Vector3.Dot(
                    current.MotionCore.OriginalSole - previous.MotionCore.OriginalSole,
                    current.Response.CorrectionResponseDirection);
                bool useCurrentStep = holdToAdvance || !advanceToHold;
                string firstLargeStepStage = ResolveFirstLargeCadenceStage(
                    useCurrentStep
                        ? Math.Abs(currentFormalHeightDelta)
                        : Math.Abs(previousFormalHeightDelta),
                    useCurrentStep
                        ? Math.Abs(currentDesiredDelta)
                        : Math.Abs(previousDesiredDelta),
                    useCurrentStep
                        ? Math.Abs(current.Response.CorrectionResponseAppliedDelta)
                        : Math.Abs(previous.Response.CorrectionResponseAppliedDelta),
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
                        previous.Response.CorrectionResponsePrevious,
                    ["PreviousCorrectionResponseCurrent"] =
                        previous.Response.CorrectionResponseCurrent,
                    ["PreviousCorrectionResponseAppliedDelta"] =
                        previous.Response.CorrectionResponseAppliedDelta,
                    ["PreviousCorrectionResponseSelectedSpeed"] =
                        previous.Response.CorrectionResponseSelectedSpeed,
                    ["CurrentCorrectionResponsePrevious"] =
                        current.Response.CorrectionResponsePrevious,
                    ["CurrentCorrectionResponseCurrent"] =
                        current.Response.CorrectionResponseCurrent,
                    ["CurrentCorrectionResponseAppliedDelta"] =
                        current.Response.CorrectionResponseAppliedDelta,
                    ["CurrentCorrectionResponseSelectedSpeed"] =
                        current.Response.CorrectionResponseSelectedSpeed,
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
                        previous.LandingObservation.ObservationQueryExecuted,
                    ["currentObservationQueryExecuted"] =
                        current.LandingObservation.ObservationQueryExecuted,
                    ["previousObservationReused"] =
                        previous.LandingObservation.ObservationCacheState == "Reused",
                    ["currentObservationReused"] =
                        current.LandingObservation.ObservationCacheState == "Reused"
                };
                var detail = new CharacterFootCorrectionResponseCadenceAnalysis
                {
                    classification = classification,
                    firstFrame = first.Identity.FrameSequence,
                    previousFrame = previous.Identity.FrameSequence,
                    frame = current.Identity.FrameSequence,
                    pathIdentity =
                        current.MotionCore.GroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    previousPathRevisionReason =
                        previous.PathContinuity.PathRevisionReason,
                    currentPathRevisionReason = current.PathContinuity.PathRevisionReason,
                    previousObservationCacheState =
                        previous.LandingObservation.ObservationCacheState,
                    previousObservationQueryPurpose =
                        previous.LandingObservation.ObservationQueryPurpose,
                    previousObservationRefreshMode =
                        previous.LandingObservation.ObservationRefreshMode,
                    previousObservationQueryReason =
                        previous.LandingObservation.ObservationQueryReason,
                    currentObservationCacheState =
                        current.LandingObservation.ObservationCacheState,
                    currentObservationQueryPurpose =
                        current.LandingObservation.ObservationQueryPurpose,
                    currentObservationRefreshMode =
                        current.LandingObservation.ObservationRefreshMode,
                    currentObservationQueryReason =
                        current.LandingObservation.ObservationQueryReason,
                    firstLargeStepStage = firstLargeStepStage
                };
                events.Add(new EventFact(
                    "StableSwingCorrectionResponseCadence",
                    current.Identity.Side,
                    first.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.MotionCore.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                    previous.MotionCore.State != "Accepted" ||
                    current.MotionCore.State != "Accepted" ||
                    previous.MotionCore.ConstraintState != "Swing" ||
                    current.MotionCore.ConstraintState != "Swing" ||
                    previous.HasAnchor || current.HasAnchor ||
                    !previous.Solver.PhysicalWriteAvailable ||
                    !current.Solver.PhysicalWriteAvailable ||
                    previous.MotionCore.LandingEventIdentity == 0 ||
                    previous.MotionCore.LandingEventIdentity !=
                    current.MotionCore.LandingEventIdentity ||
                    !string.Equals(
                        previous.FormalInput.SourceIdentity,
                        current.FormalInput.SourceIdentity,
                        StringComparison.Ordinal) ||
                    previous.FormalInput.SourceCycle != current.FormalInput.SourceCycle ||
                    previous.GroundPath.InputIdentity !=
                    current.GroundPath.InputIdentity ||
                    current.PathContinuity.PathResidualRebuilt ||
                    !previous.PathContinuity.PathAvailableAfter ||
                    !current.PathContinuity.PathAvailableAfter ||
                    previous.GroundPath.State != "Accepted" ||
                    current.GroundPath.State != "Accepted")
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
                    current.MotionCore.ActualEnvelopeCounterfactualState ==
                    "UniqueInCorridor";
                var metrics = new SortedDictionary<string, double>(
                    StringComparer.Ordinal)
                {
                    ["ActualProgressEnvelopeAdvanceAboveBuilderTarget"] =
                        current.MotionCore.ActualProgressEnvelopeAdvanceAboveBuilderTarget,
                    ["ActualProgressEnvelopeMinimumCorrection"] =
                        current.MotionCore.ActualProgressEnvelopeMinimumCorrection,
                    ["BuilderSwingTargetAlongUp"] =
                        current.PathContinuity.ComponentUp.sqrMagnitude >
                        TimeEpsilon * TimeEpsilon
                            ? Vector3.Dot(
                                current.MotionCore.BuilderSwingTargetCorrection,
                                current.PathContinuity.ComponentUp.normalized)
                            : 0d,
                    ["ActualFootCrossTrackDistance"] =
                        current.MotionCore.ActualFootCrossTrackDistance,
                    ["ActualEnvelopeCandidateCount"] =
                        current.MotionCore.ActualEnvelopeCandidateCount,
                    ["ActualEnvelopeHeightSpan"] =
                        current.MotionCore.ActualEnvelopeHeightSpan,
                    ["GroundEnvelopeHardClamp"] =
                        current.OutputStages.SafetyFloorOwner == "GroundPathEnvelope"
                            ? current.OutputStages.SafetyFloorClampMeters
                            : 0d,
                    ["FootPlacementOutputOffsetStep"] = visibleStep,
                    ["PresentationDeltaSeconds"] = current.Timing.DeltaSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["uniqueInCorridor"] = uniqueInCorridor,
                    ["ambiguousInCorridor"] =
                        current.MotionCore.ActualEnvelopeCounterfactualState ==
                        "AmbiguousInCorridor",
                    ["outsideGroundPathCorridor"] =
                        current.MotionCore.ActualEnvelopeCounterfactualState ==
                        "OutsideGroundPathCorridor",
                    ["noIntersection"] =
                        current.MotionCore.ActualEnvelopeCounterfactualState ==
                        "NoIntersection",
                    ["counterfactualUnavailable"] =
                        current.MotionCore.ActualEnvelopeCounterfactualState ==
                        "Unavailable",
                    ["groundEnvelopeOwner"] =
                        current.OutputStages.SafetyFloorOwner == "GroundPathEnvelope",
                    ["actualProgressCorrectionAvailable"] =
                        current.MotionCore.ActualProgressEnvelopeCorrectionAvailable,
                    ["visibleOutputAboveTwoCentimeters"] =
                        visibleStep > 0.02d
                };
                events.Add(new EventFact(
                    "ActualFootEnvelopeCounterfactual",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.MotionCore.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                    !previous.Solver.PhysicalWriteAvailable ||
                    !current.Solver.PhysicalWriteAvailable)
                {
                    continue;
                }
                bool swingToLanding =
                    current.OutputStages.ConstraintStateBefore == "Swing" &&
                    current.MotionCore.ConstraintState == "Landing";
                bool contactOutputPair = IsContactOutputState(previous.MotionCore.ConstraintState) ||
                    IsContactOutputState(current.MotionCore.ConstraintState);
                bool acceptedUnanchoredSwingPair =
                    previous.MotionCore.ConstraintState == "Swing" &&
                    current.MotionCore.ConstraintState == "Swing" &&
                    previous.MotionCore.State == "Accepted" &&
                    current.MotionCore.State == "Accepted" &&
                    !previous.HasAnchor && !current.HasAnchor;
                if (!contactOutputPair && !acceptedUnanchoredSwingPair)
                    continue;
                double pathNoiseFloor = Math.Max(
                    PositionNoiseFloor,
                    current.PathContinuity.PathRevisionDistance);
                bool pathAvailabilityChanged =
                    previous.PathContinuity.PathAvailableAfter != current.PathContinuity.PathAvailableAfter ||
                    RevisionReasonIncludes(
                        current.PathContinuity.PathRevisionReason,
                        "PathAvailabilityChanged");
                bool landingEventChanged =
                    previous.GroundPath.NextSwingLandingEventIdentity !=
                    current.GroundPath.NextSwingLandingEventIdentity ||
                    RevisionReasonIncludes(
                        current.PathContinuity.PathRevisionReason,
                        "LandingEventChanged");
                bool endpointTreadChanged =
                    previous.GroundPath.NextSwingLandingSurfaceIdentity != 0 &&
                    current.GroundPath.NextSwingLandingSurfaceIdentity != 0 &&
                    previous.GroundPath.NextSwingLandingSurfaceIdentity !=
                    current.GroundPath.NextSwingLandingSurfaceIdentity;
                double endpointDeltaMeters = Vector3.Distance(
                    previous.GroundPath.NextSwingLanding,
                    current.GroundPath.NextSwingLanding);
                bool landingPointRevised =
                    current.PathContinuity.PathLandingPointDelta > pathNoiseFloor ||
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
                bool sameEvent = previous.MotionCore.LandingEventIdentity != 0 &&
                    previous.MotionCore.LandingEventIdentity ==
                    current.MotionCore.LandingEventIdentity;
                bool sameSource = string.Equals(
                    previous.FormalInput.SourceIdentity,
                    current.FormalInput.SourceIdentity,
                    StringComparison.Ordinal) &&
                    previous.FormalInput.SourceCycle == current.FormalInput.SourceCycle;
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
                         previous.PathContinuity.PathAvailableAfter &&
                         current.PathContinuity.PathAvailableAfter &&
                         previous.GroundPath.State == "Accepted" &&
                         current.GroundPath.State == "Accepted")
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
                ulong bodyTickSpan = current.Timing.CurrentBodyTick >=
                                     previous.Timing.CurrentBodyTick
                    ? current.Timing.CurrentBodyTick - previous.Timing.CurrentBodyTick
                    : 0;
                bool lowPresentationCadence =
                    current.Timing.DeltaSeconds >=
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
                    previousFrame = previous.Identity.FrameSequence,
                    frame = current.Identity.FrameSequence,
                    side = current.Identity.Side,
                    landingEventIdentity = ResolveEventIdentity(current)
                        .ToString(CultureInfo.InvariantCulture),
                    sourceIdentity = current.FormalInput.SourceIdentity,
                    sourceCycle = current.FormalInput.SourceCycle,
                    previousConstraintState = previous.MotionCore.ConstraintState,
                    constraintStateBefore = current.OutputStages.ConstraintStateBefore,
                    constraintState = current.MotionCore.ConstraintState,
                    preTransitionReason = current.Lifecycle.PreTransitionReason,
                    preTransitionSource = current.Lifecycle.PreTransitionSource,
                    preTransitionTarget = current.Lifecycle.PreTransitionTarget,
                    preTransitionAnchorCommand =
                        current.Lifecycle.PreTransitionAnchorCommand,
                    postTransitionEvaluated = current.Lifecycle.PostTransitionEvaluated,
                    postTransitionReason = current.Lifecycle.PostTransitionEvaluated
                        ? current.Lifecycle.PostTransitionReason : null,
                    postTransitionSource = current.Lifecycle.PostTransitionEvaluated
                        ? current.Lifecycle.PostTransitionSource : null,
                    postTransitionTarget = current.Lifecycle.PostTransitionEvaluated
                        ? current.Lifecycle.PostTransitionTarget : null,
                    postTransitionAnchorCommand =
                        current.Lifecycle.PostTransitionEvaluated
                            ? current.Lifecycle.PostTransitionAnchorCommand : null,
                    stateTargetCorrection = CharacterFootVectorFact.From(
                        current.OutputStages.StateTargetCorrection),
                    interpolationPolicy = current.OutputStages.InterpolationPolicy,
                    interpolationOutputCorrection =
                        CharacterFootVectorFact.From(
                            current.OutputStages.InterpolationOutputCorrection),
                    interpolationCompleted = current.OutputStages.InterpolationCompleted,
                    plantInterpolationEvaluated =
                        current.OutputStages.PlantInterpolationEvaluated,
                    targetHeightComponentUp =
                        CharacterFootVectorFact.From(current.PathContinuity.ComponentUp),
                    plantTargetEventIdentity =
                        current.OutputStages.PlantTargetEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    plantTargetVerified = current.OutputStages.PlantTargetVerified,
                    plantTargetKind = current.OutputStages.PlantTargetKind,
                    plantLockResponse = current.OutputStages.PlantLockResponse,
                    plantLockWeightCompleted =
                        current.OutputStages.PlantLockWeightCompleted,
                    plantDesiredPoint = CharacterFootVectorFact.From(
                        current.OutputStages.PlantDesiredPoint),
                    plantFilteredPoint = CharacterFootVectorFact.From(
                        current.OutputStages.PlantFilteredPoint),
                    swingTargetHeightAdoptionMode =
                        current.PathContinuity.SwingTargetHeightAdoptionMode,
                    plantTargetHeightAdoptionMode =
                        current.Response.PlantTargetHeightAdoptionMode,
                    plantTargetMaximumVerticalSpeed =
                        current.Response.PlantTargetMaximumVerticalSpeed,
                    plantTargetHeightBefore =
                        current.Response.PlantTargetHeightBefore,
                    plantTargetHeightTarget =
                        current.Response.PlantTargetHeightTarget,
                    plantTargetVerticalDelta =
                        current.Response.PlantTargetVerticalDelta,
                    plantTargetAppliedVerticalDelta =
                        current.Response.PlantTargetAppliedVerticalDelta,
                    plantTargetHeightAfter =
                        current.Response.PlantTargetHeightAfter,
                    plantTargetHeightEventIdentity =
                        current.Response.PlantTargetHeightEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    plantTargetHeightUpdateReason =
                        current.Response.PlantTargetHeightUpdateReason,
                    plantTargetVerticalClamped =
                        current.Response.PlantTargetVerticalClamped,
                    plantPreviousSelectedWorldTarget =
                        CharacterFootVectorFact.From(
                            current.Response.PlantPreviousSelectedWorldTarget),
                    plantSelectedWorldTarget = CharacterFootVectorFact.From(
                        current.Response.PlantSelectedWorldTarget),
                    previousResponseOutputAvailable =
                        current.Response.PreviousResponseOutputAvailable,
                    previousResponseOutputPoint =
                        CharacterFootVectorFact.From(
                            current.Response.PreviousResponseOutputPoint),
                    desiredOutputPoint = CharacterFootVectorFact.From(
                        current.Response.DesiredOutputPoint),
                    responseOutputPoint = CharacterFootVectorFact.From(
                        current.Response.ResponseOutputPoint),
                    plantResidualCaptureReason =
                        current.Response.PlantResidualCaptureReason,
                    plantWorldResidualBeforeCapture =
                        CharacterFootVectorFact.From(
                            current.Response.PlantWorldResidualBeforeCapture),
                    plantWorldResidualCapturedBeforeDecay =
                        CharacterFootVectorFact.From(
                            current.Response.PlantWorldResidualCapturedBeforeDecay),
                    plantWorldResidualDecayApplied =
                        current.Response.PlantWorldResidualDecayApplied,
                    plantWorldResidualBaseHalfLifeSeconds =
                        current.Response.PlantWorldResidualBaseHalfLifeSeconds,
                    plantWorldResidualDeadlineHalfLifeAvailable =
                        current.Response.PlantWorldResidualDeadlineHalfLifeAvailable,
                    plantWorldResidualDeadlineHalfLifeSeconds =
                        current.Response.PlantWorldResidualDeadlineHalfLifeSeconds,
                    plantWorldResidualAppliedHalfLifeSeconds =
                        current.Response.PlantWorldResidualAppliedHalfLifeSeconds,
                    plantWorldResidualAfterDecay =
                        CharacterFootVectorFact.From(
                            current.Response.PlantWorldResidualAfterDecay),
                    plantWorldResidualCompletionTolerance =
                        current.Response.PlantWorldResidualCompletionTolerance,
                    plantWorldResidualClearedAtCompletionTolerance =
                        current
                            .Response.PlantWorldResidualClearedAtCompletionTolerance,
                    correctionResponseEvaluated =
                        current.Response.CorrectionResponseEvaluated,
                    responseDomain = ResponseDomainFact(current),
                    correctionResponseInitializedBefore =
                        current.Response.CorrectionResponseInitializedBefore,
                    correctionResponseInitializedThisFrame =
                        current.Response.CorrectionResponseInitializedThisFrame,
                    correctionResponseInitializationReason =
                        current.Response.CorrectionResponseInitializationReason,
                    correctionResponseDesired =
                        ScalarResponseValue(current, current.Response.CorrectionResponseDesired),
                    correctionResponseRequestedDirection =
                        CharacterFootVectorFact.From(
                            current.Response.CorrectionResponseRequestedDirection),
                    correctionResponsePreviousDirection =
                        CharacterFootVectorFact.From(
                            current.Response.CorrectionResponsePreviousDirection),
                    correctionResponseDirectionLimited =
                        current.Response.CorrectionResponseDirectionLimited,
                    correctionResponseMaximumDirectionChangeDegrees =
                        current.Response.CorrectionResponseMaximumDirectionChangeDegrees,
                    correctionResponseAppliedDirectionChangeDegrees =
                        current.Response.CorrectionResponseAppliedDirectionChangeDegrees,
                    correctionResponseVisibleOutputTransferred =
                        current.Response.CorrectionResponseVisibleOutputTransferred,
                    correctionResponseBeforeRebase =
                        ScalarResponseValue(current, current.Response.CorrectionResponseBeforeRebase),
                    correctionResponsePrevious =
                        ScalarResponseValue(current, current.Response.CorrectionResponsePrevious),
                    correctionResponseCurrent =
                        ScalarResponseValue(current, current.Response.CorrectionResponseCurrent),
                    correctionResponseDirection =
                        CharacterFootVectorFact.From(
                            current.Response.CorrectionResponseDirection),
                    correctionResponseDeltaDirection =
                        current.Response.CorrectionResponseDeltaDirection,
                    correctionResponseSelectedSpeed =
                        ScalarResponseValue(current, current.Response.CorrectionResponseSelectedSpeed),
                    correctionResponseAppliedDelta =
                        ScalarResponseValue(current, current.Response.CorrectionResponseAppliedDelta),
                    plantVerticalContinuityOwners =
                        current.Response.PlantVerticalContinuityOwners,
                    plantEffectiveCorrectionBefore =
                        CharacterFootVectorFact.From(
                            current.Response.PlantEffectiveCorrectionBefore),
                    plantEffectiveCorrectionAfter =
                        CharacterFootVectorFact.From(
                            current.Response.PlantEffectiveCorrectionAfter),
                    plantOutputDistance = current.Response.PlantOutputDistance,
                    plantPenetrationDepth = current.Response.PlantPenetrationDepth,
                    presentationDeltaSeconds = current.Timing.DeltaSeconds,
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
                    safetyFloorOwner = current.OutputStages.SafetyFloorOwner,
                    safetyFloorOwnerSurfaceIdentity =
                        current.OutputStages.SafetyFloorOwnerSurfaceIdentity,
                    safetyFloorOwnerPathIdentity =
                        current.OutputStages.SafetyFloorOwnerPathIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    pathRevisionReason = current.PathContinuity.PathRevisionReason,
                    pathNoiseFloorMeters = pathNoiseFloor,
                    endpointDeltaMeters = endpointDeltaMeters,
                    landingPointDeltaMeters =
                        current.PathContinuity.PathLandingPointDelta,
                    targetDeltaMeters = current.PathContinuity.PathTargetDelta,
                    pathAvailabilityChanged = pathAvailabilityChanged,
                    landingEventChanged = landingEventChanged,
                    endpointTreadChanged = endpointTreadChanged,
                    counterfactualPathRevision =
                        counterfactualPathRevision,
                    pathResidualRebuilt = current.PathContinuity.PathResidualRebuilt,
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
                        current.PathContinuity.PathLandingPointDelta,
                    ["TargetDelta"] = current.PathContinuity.PathTargetDelta,
                    ["PathRevisionDelta"] =
                        counterfactual?.pathRevisionDelta ?? 0d,
                    ["PhaseAdvanceDelta"] =
                        counterfactual?.phaseAdvanceDelta ?? 0d,
                    ["ObservedSwingTargetDelta"] =
                        counterfactual?.observedSwingTargetDelta ?? 0d,
                    ["PresentationDeltaSeconds"] =
                        current.Timing.DeltaSeconds,
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
                        current.OutputStages.SafetyFloorOwner == "GroundPathEnvelope",
                    ["safetyFloorOwnerContactAnchor"] =
                        current.OutputStages.SafetyFloorOwner == "ContactAnchor"
                };
                events.Add(new EventFact(
                    category,
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    ResolveEventIdentity(current),
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                !previous.Solver.PhysicalWriteAvailable ||
                !current.Solver.PhysicalWriteAvailable)
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
                0 => frame.MotionCore.OriginalAnkle,
                1 => frame.MotionCore.SourceHeel,
                2 => frame.MotionCore.SourceToe,
                _ => throw new ArgumentOutOfRangeException(nameof(probe))
            };

        static Vector3 ResolvePhysicalProbe(FootFrame frame, int probe) =>
            probe switch
            {
                0 => FinalPhysicalAnkleWorld(frame),
                1 => frame.Solver.PhysicalHeelWorld,
                2 => frame.Solver.PhysicalToeWorld,
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
                if (current.LandingObservation.ObservationIdentity == 0)
                    continue;
                firstByIdentity.TryGetValue(
                    current.LandingObservation.ObservationIdentity,
                    out FootFrame previous);
                bool identitySeenBefore = previous != null;
                bool resultMatchesPrevious = identitySeenBefore &&
                    previous.LandingObservation.Accepted ==
                    current.LandingObservation.Accepted &&
                    previous.LandingObservation.SurfaceIdentity ==
                    current.LandingObservation.SurfaceIdentity &&
                    Vector3.Distance(
                        previous.LandingObservation.Point,
                        current.LandingObservation.Point) <= PositionNoiseFloor &&
                    Math.Abs(
                        previous.LandingObservation.QueryDistance -
                        current.LandingObservation.QueryDistance) <= PositionNoiseFloor;
                bool queried = current.LandingObservation.ObservationCacheState ==
                               "Queried";
                bool reused = current.LandingObservation.ObservationCacheState ==
                              "Reused";
                bool forcedVerification =
                    current.LandingObservation.ObservationQueryPurpose ==
                    "CurrentContactVerification" &&
                    current.LandingObservation.ObservationRefreshMode ==
                    "ForcedPlantVerification";
                string forcedVerificationKey = string.Concat(
                    current.Identity.Side,
                    ":",
                    current.FormalInput.SourceIdentity,
                    ":",
                    current.Identity.LandingEventIdentity.ToString(
                        CultureInfo.InvariantCulture));
                bool firstForcedVerification = forcedVerification &&
                    forcedVerificationByEvent.Add(forcedVerificationKey);
                FootFrame previousCommitted = i > 0 &&
                    Continuous(frames[i - 1], current)
                        ? frames[i - 1]
                        : null;
                bool contactEventChanged = previousCommitted != null &&
                    previousCommitted.Resolved.ContactAvailable &&
                    (previousCommitted.MotionCore.ConstraintState == "Landing" ||
                     previousCommitted.MotionCore.ConstraintState == "Locked" ||
                     previousCommitted.MotionCore.ConstraintState == "Releasing") &&
                    previousCommitted.InputEvents.Current.Identity != 0 &&
                    current.InputEvents.Current.Identity != 0 &&
                    previousCommitted.InputEvents.Current.Identity !=
                    current.InputEvents.Current.Identity;
                bool contactEventAcquisitionConsistent =
                    !contactEventChanged ||
                    forcedVerification && firstForcedVerification &&
                    current.LandingObservation.ObservationQueryExecuted &&
                    current.Lifecycle.PreTransitionReason ==
                    "NewEventContactAcquired" &&
                    current.Lifecycle.PreTransitionSource ==
                    previousCommitted.MotionCore.ConstraintState &&
                    current.Lifecycle.PreTransitionTarget == "Landing" &&
                    current.Lifecycle.PreTransitionAnchorCommand == "Create" &&
                    current.MotionCore.ConstraintState == "Landing" &&
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
                    current.OutputStages.PlantTargetEventIdentity ==
                    current.InputEvents.Current.Identity;
                if (contactEventChanged &&
                    !contactEventAcquisitionConsistent)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Contact EventChanged acquisition is inconsistent " +
                        $"Frame={current.Identity.FrameSequence} Side={current.Identity.Side}.");
                }
                bool previousCommittedIdentityMatches =
                    previousCommitted != null &&
                    previousCommitted.LandingObservation.ObservationIdentity ==
                    current.LandingObservation.ObservationIdentity;
                bool duplicateQuery =
                    current.LandingObservation.ObservationQueryExecuted &&
                    (forcedVerification && !firstForcedVerification ||
                     previousCommittedIdentityMatches &&
                     !forcedVerification);
                bool distanceExceeded =
                    current.LandingObservation.ObservationQueryInputDistance >
                    current.LandingObservation.ObservationPredictionInputAccumulationDistance;
                bool angleExceeded =
                    current.LandingObservation.ObservationQueryComponentUpAngleDegrees >
                    current.LandingObservation.ObservationComponentUpChangeAngleDegrees;
                bool distanceReason = HasRevisionReason(
                    current.LandingObservation.ObservationQueryReason,
                    "PredictionInputDistanceExceeded");
                bool angleReason = HasRevisionReason(
                    current.LandingObservation.ObservationQueryReason,
                    "ComponentUpAngleExceeded");
                bool hasQueryReason = current.LandingObservation.ObservationQueryReason !=
                                      "None";
                bool purposeMatchesRefresh = forcedVerification ||
                    current.LandingObservation.ObservationQueryPurpose ==
                    "FutureLanding" &&
                    (current.LandingObservation.ObservationRefreshMode == "Thresholded" ||
                     current.LandingObservation.ObservationRefreshMode ==
                     "ChangedSlidingAdmissionInput");
                bool queryThresholdContractConsistent =
                    distanceExceeded == distanceReason &&
                    angleExceeded == angleReason &&
                    queried == hasQueryReason &&
                    purposeMatchesRefresh &&
                    (!forcedVerification || queried) &&
                    (!reused || !distanceExceeded && !angleExceeded);
                bool cacheStateConsistent =
                    (queried && current.LandingObservation.ObservationQueryExecuted ||
                     reused && !current.LandingObservation.ObservationQueryExecuted) &&
                    !duplicateQuery;
                var detail = new CharacterFootLandingObservationAnalysis
                {
                    previousFrame = previous?.Identity.FrameSequence ?? 0,
                    frame = current.Identity.FrameSequence,
                    side = current.Identity.Side,
                    landingEventIdentity = current.Identity.LandingEventIdentity
                        .ToString(CultureInfo.InvariantCulture),
                    sourceIdentity = current.FormalInput.SourceIdentity,
                    sourceCycle = current.FormalInput.SourceCycle,
                    observationIdentity = current.LandingObservation.ObservationIdentity
                        .ToString(CultureInfo.InvariantCulture),
                    worldRevision = current.LandingObservation.ObservationWorldRevision
                        .ToString(CultureInfo.InvariantCulture),
                    sourceSampleIdentity =
                        current.LandingObservation.ObservationSourceSampleIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    sourceSampleCycle =
                        current.LandingObservation.ObservationSourceSampleCycle,
                    cacheState = current.LandingObservation.ObservationCacheState,
                    queryExecutedThisFrame =
                        current.LandingObservation.ObservationQueryExecuted,
                    queryPurpose =
                        current.LandingObservation.ObservationQueryPurpose,
                    refreshMode = current.LandingObservation.ObservationRefreshMode,
                    queryReason = current.LandingObservation.ObservationQueryReason,
                    canonicalRawLanding = CharacterFootVectorFact.From(
                        current.LandingObservation.ObservationCanonicalRaw),
                    canonicalComponentUp = CharacterFootVectorFact.From(
                        current.LandingObservation.ObservationCanonicalComponentUp),
                    candidateRawLanding = CharacterFootVectorFact.From(
                        current.LandingObservation.ObservationCandidateRaw),
                    candidateComponentUp = CharacterFootVectorFact.From(
                        current.LandingObservation.ObservationCandidateComponentUp),
                    queryInputDistanceMeters =
                        current.LandingObservation.ObservationQueryInputDistance,
                    queryComponentUpAngleDegrees =
                        current.LandingObservation.ObservationQueryComponentUpAngleDegrees,
                    predictionInputAccumulationDistanceMeters =
                        current.LandingObservation.ObservationPredictionInputAccumulationDistance,
                    componentUpChangeAngleDegrees =
                        current.LandingObservation.ObservationComponentUpChangeAngleDegrees,
                    selectionState =
                        current.LandingObservation.SelectionState,
                    validCandidateCount =
                        current.LandingObservation.ValidCandidateCount,
                    selected = new CharacterFootLandingQueryCandidateFact
                    {
                        available = current.LandingObservation.SelectedAvailable,
                        surfaceIdentity =
                            current.LandingObservation.SelectedSurfaceIdentity,
                        point = CharacterFootVectorFact.From(
                            current.LandingObservation.SelectedPoint),
                        distanceMeters =
                            current.LandingObservation.SelectedDistance
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
                        current.LandingObservation.ValidCandidateCount,
                    ["QueryInputDistance"] =
                        current.LandingObservation.ObservationQueryInputDistance,
                    ["PredictionInputAccumulationDistance"] =
                        current.LandingObservation.ObservationPredictionInputAccumulationDistance,
                    ["QueryComponentUpAngleDegrees"] =
                        current.LandingObservation.ObservationQueryComponentUpAngleDegrees,
                    ["ComponentUpChangeAngleDegrees"] =
                        current.LandingObservation.ObservationComponentUpChangeAngleDegrees
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["queried"] = queried,
                    ["reused"] = reused,
                    ["queryExecutedThisFrame"] =
                        current.LandingObservation.ObservationQueryExecuted,
                    ["forcedPlantVerification"] = forcedVerification,
                    ["futureLandingPurpose"] =
                        current.LandingObservation.ObservationQueryPurpose ==
                        "FutureLanding",
                    ["currentContactVerificationPurpose"] =
                        current.LandingObservation.ObservationQueryPurpose ==
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
                    current.Identity.Side,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    landingObservation: detail));
                if (!identitySeenBefore)
                    firstByIdentity.Add(
                        current.LandingObservation.ObservationIdentity,
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
                        current.Identity.Side,
                        current.Identity.FrameSequence,
                        current.Identity.FrameSequence,
                        current.Identity.FrameSequence,
                        current.Identity.SelectedLandingEventIdentity,
                        current.FormalInput.SourceIdentity,
                        current.FormalInput.SourceCycle,
                        current.Timing.DeltaSeconds,
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
                    previous.Identity.SelectedLandingEventIdentity == 0 ||
                    previous.Identity.SelectedLandingEventIdentity !=
                    current.Identity.SelectedLandingEventIdentity ||
                    !previous.SelectedPhase.InApproachContactToLanding ||
                    !current.SelectedPhase.InApproachContactToLanding ||
                    !previousConsumedAvailable ||
                    !currentConsumedAvailable ||
                    previous.GroundPath.NextSwingLandingEventIdentity !=
                    previous.Identity.SelectedLandingEventIdentity ||
                    current.GroundPath.NextSwingLandingEventIdentity !=
                    current.Identity.SelectedLandingEventIdentity)
                {
                    continue;
                }
                double consumedPointDelta = Vector3.Distance(
                    previous.GroundPath.NextSwingLanding,
                    current.GroundPath.NextSwingLanding);
                bool consumedSurfaceChanged =
                    previous.GroundPath.NextSwingLandingSurfaceIdentity !=
                    current.GroundPath.NextSwingLandingSurfaceIdentity;
                bool pointExceededAcceptanceDistance =
                    consumedPointDelta > current.PathContinuity.LandingAcceptanceDistance;
                double observedPointDelta =
                    previous.LandingObservation.Accepted &&
                    current.LandingObservation.Accepted
                        ? Vector3.Distance(
                            previous.LandingObservation.Point,
                            current.LandingObservation.Point)
                        : 0d;
                double correctionStep = Vector3.Distance(
                    previous.OutputStages.FinalEffectiveCorrection,
                    current.OutputStages.FinalEffectiveCorrection);
                bool componentUpAvailable =
                    current.PathContinuity.ComponentUp.sqrMagnitude >
                    TimeEpsilon * TimeEpsilon;
                Vector3 up = componentUpAvailable
                    ? current.PathContinuity.ComponentUp.normalized
                    : default;
                bool physicalAvailable =
                    previous.Solver.PhysicalWriteAvailable &&
                    current.Solver.PhysicalWriteAvailable;
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
                        previousFrame = previous.Identity.FrameSequence,
                        frame = current.Identity.FrameSequence,
                        side = current.Identity.Side,
                        landingEventIdentity =
                            current.Identity.SelectedLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousSourceIdentity = previous.FormalInput.SourceIdentity,
                        sourceIdentity = current.FormalInput.SourceIdentity,
                        previousSourceCycle = previous.FormalInput.SourceCycle,
                        sourceCycle = current.FormalInput.SourceCycle,
                        previousContributionContinuityIdentity =
                            previous.FormalInput.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        contributionContinuityIdentity =
                            current.FormalInput.ContributionContinuityIdentity.ToString(
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
                            previous.LandingObservation.Accepted,
                        observedAvailable =
                            current.LandingObservation.Accepted,
                        previousObservedEventIdentity =
                            previous.Identity.LandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        observedEventIdentity =
                            current.Identity.LandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousObservedSurfaceIdentity =
                            previous.LandingObservation.SurfaceIdentity,
                        observedSurfaceIdentity =
                            current.LandingObservation.SurfaceIdentity,
                        previousObservedPoint =
                            CharacterFootVectorFact.From(
                                previous.LandingObservation.Point),
                        observedPoint = CharacterFootVectorFact.From(
                            current.LandingObservation.Point),
                        observedLandingPointDeltaMeters =
                            observedPointDelta,
                        previousConsumedEventIdentity =
                            previous.GroundPath.NextSwingLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        consumedEventIdentity =
                            current.GroundPath.NextSwingLandingEventIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        previousConsumedSurfaceIdentity =
                            previous.GroundPath.NextSwingLandingSurfaceIdentity,
                        consumedSurfaceIdentity =
                            current.GroundPath.NextSwingLandingSurfaceIdentity,
                        previousConsumedPoint =
                            CharacterFootVectorFact.From(
                                previous.GroundPath.NextSwingLanding),
                        consumedPoint = CharacterFootVectorFact.From(
                            current.GroundPath.NextSwingLanding),
                        landingPointDeltaMeters = consumedPointDelta,
                        landingAcceptanceDistanceMeters =
                            current.PathContinuity.LandingAcceptanceDistance,
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
                        current.PathContinuity.LandingAcceptanceDistance,
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
                        previous.LandingObservation.Accepted &&
                        current.LandingObservation.Accepted,
                    ["physicalAnkleAvailable"] = physicalAvailable,
                    ["physicalSoleAvailable"] = physicalAvailable,
                    ["componentUpAvailable"] = componentUpAvailable,
                    ["sourceChanged"] = previous.FormalInput.SourceIdentity !=
                                        current.FormalInput.SourceIdentity
                };
                events.Add(new EventFact(
                    "LateApproachLandingRevision",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.SelectedLandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    lateApproachLandingRevision: detail));
            }
        }

        static bool ConsumedNextSwingLandingAvailable(
            FootFrame frame) =>
            frame.GroundPath.NextSwingLandingEventIdentity != 0 &&
            frame.GroundPath.NextSwingLandingSurfaceIdentity != 0;

        static void AnalyzeLandingEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) ||
                    previous.FormalInput.LockMode != "Unlocked" ||
                    current.FormalInput.LockMode != "Sliding" ||
                    current.FormalInput.TimeToLandingSeconds > TimeEpsilon)
                {
                    continue;
                }
                int end = i;
                while (end + 1 < frames.Count &&
                       Continuous(frames[end], frames[end + 1]) &&
                       frames[end + 1].FormalInput.LockMode != "Unlocked")
                {
                    end++;
                    if (frames[end].FormalInput.LockMode == "Locked")
                        break;
                }
                IReadOnlyList<FootFrame> window = frames.GetRange(
                    Math.Max(0, i - 1),
                    end - Math.Max(0, i - 1) + 1);
                double correctionStep = MaximumCorrectionStep(window);
                double originalExtensionPeak = window.Max(
                    frame => frame.Solver.IkLegOriginalExtensionRatio);
                double targetExtensionPeak = window.Max(frame => frame.Solver.IkLegTargetExtensionRatio);
                double solvedExtensionPeak = window.Max(frame => frame.Solver.IkLegSolvedExtensionRatio);
                double bendMinimum = window.Min(frame => frame.Solver.IkLegSolvedBendDegrees);
                double originalCompressionMinimum = window.Min(
                    frame => frame.Solver.IkLegOriginalCompressionReserve);
                double targetCompressionMinimum = window.Min(
                    frame => frame.Solver.IkLegTargetCompressionReserve);
                double solvedCompressionMinimum = window.Min(
                    frame => frame.Solver.IkLegSolvedCompressionReserve);
                double bendDirectionMinimum = window.Min(frame => frame.Solver.IkLegEffectiveBendDirectionPreviousDot);
                double targetExtensionDelta =
                    targetExtensionPeak - previous.Solver.IkLegTargetExtensionRatio;
                double bendDrop = previous.Solver.IkLegSolvedBendDegrees - bendMinimum;
                int peakFrame = PeakCorrectionFrame(window);
                FootFrame peak = window.First(
                    frame => frame.Identity.FrameSequence == peakFrame);
                LandingReachFact landingReach =
                    LandingReachFact.From(peak);
                var fact = new EventFact(
                    "Landing",
                    current.Identity.Side,
                    current.Identity.FrameSequence,
                    frames[end].Identity.FrameSequence,
                    peakFrame,
                    current.MotionCore.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                        ["targetExtensionRatioBaseline"] = previous.Solver.IkLegTargetExtensionRatio,
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
                        ["grounded"] = current.Action.Grounded,
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
                    ["formalStepTimeSeconds"] = current.FormalInput.TimeToLandingSeconds,
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
                        current.MotionCore.ConstraintState == "Landing",
                    ["runtimeLockedAtBoundary"] =
                        current.MotionCore.ConstraintState == "Locked",
                    ["runtimeSwingAtBoundary"] =
                        current.MotionCore.ConstraintState == "Swing",
                    ["runtimeUnlockedSupportAtBoundary"] =
                        current.MotionCore.ConstraintState == "UnlockedSupport",
                    ["runtimeReleasingAtBoundary"] =
                        current.MotionCore.ConstraintState == "Releasing",
                    ["contactPlaneAvailable"] = current.MotionCore.ContactPlaneAvailable
                };
                events.Add(new EventFact(
                    "LandingStateBoundary",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.MotionCore.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence));
            }

            int index = 0;
            while (index < frames.Count)
            {
                if (frames[index].MotionCore.ConstraintState != "Landing")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].MotionCore.LandingEventIdentity;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].MotionCore.ConstraintState == "Landing" &&
                       frames[index + 1].MotionCore.LandingEventIdentity == eventIdentity)
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
                    window[0].MotionCore.CorrectedSole,
                    window[0].MotionCore.Anchor);
                double correctedExitDistance = Vector3.Distance(
                    window[^1].MotionCore.CorrectedSole,
                    window[^1].MotionCore.Anchor);
                double finalEntryDistance = Vector3.Distance(
                    FinalSole(window[0]),
                    window[0].MotionCore.Anchor);
                double finalExitDistance = Vector3.Distance(
                    FinalSole(window[^1]),
                    window[^1].MotionCore.Anchor);
                CharacterFootOutputBoundaryMotion entryMotion = hasEntry
                    ? ResolveOutputBoundaryMotion(entryPrevious, window[0])
                    : default;
                CharacterFootOutputBoundaryMotion exitMotion = hasExit
                    ? ResolveOutputBoundaryMotion(window[^1], exitNext)
                    : default;
                int peakFrame = entryMotion.StateAdditionalOutputStepMeters >=
                                exitMotion.StateAdditionalOutputStepMeters
                    ? window[0].Identity.FrameSequence
                    : hasExit
                        ? exitNext.Identity.FrameSequence
                        : window[^1].Identity.FrameSequence;
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
                        value => value.FormalInput.LockMode == "Unlocked")
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["entryFollowedFormalBoundary"] = hasEntry &&
                        FormalLandingBoundary(entryPrevious, window[0]),
                    ["contactPlaneAvailableThroughout"] = window.All(
                        value => value.MotionCore.ContactPlaneAvailable),
                    ["closedTowardAnchor"] = correctedExitDistance +
                        CharacterFootContactPlanePenetration.GeometryEpsilonMeters <
                        correctedEntryDistance,
                    ["hasContinuousExit"] = hasExit,
                    ["entryPhysicalOutputAvailable"] =
                        entryMotion.PhysicalOutputAvailable,
                    ["exitPhysicalOutputAvailable"] =
                        exitMotion.PhysicalOutputAvailable,
                    ["exitedToLocked"] = hasExit &&
                        exitNext.MotionCore.ConstraintState == "Locked",
                    ["exitedToReleasing"] = hasExit &&
                        exitNext.MotionCore.ConstraintState == "Releasing",
                    ["exitedToSwing"] = hasExit &&
                        exitNext.MotionCore.ConstraintState == "Swing",
                    ["exitedToUnlockedSupport"] = hasExit &&
                        exitNext.MotionCore.ConstraintState == "UnlockedSupport",
                    ["formalUnlockedWithinLanding"] = window.Any(
                        value => value.FormalInput.LockMode == "Unlocked")
                };
                events.Add(new EventFact(
                    "LandingStateSpan",
                    window[0].Identity.Side,
                    window[0].Identity.FrameSequence,
                    window[^1].Identity.FrameSequence,
                    peakFrame,
                    eventIdentity,
                    window[0].FormalInput.SourceIdentity,
                    window[0].FormalInput.SourceCycle,
                    Duration(window),
                    metrics,
                    evidence));
                index++;
            }
        }

        static bool FormalLandingBoundary(
            FootFrame previous,
            FootFrame current) =>
            previous.FormalInput.LockMode == "Unlocked" &&
            current.FormalInput.LockMode != "Unlocked";

        static CharacterFootOutputBoundaryMotion ResolveOutputBoundaryMotion(
            FootFrame previous,
            FootFrame current)
        {
            Vector3 correctedDelta =
                current.MotionCore.CorrectedSole - previous.MotionCore.CorrectedSole;
            Vector3 animatedDelta =
                current.MotionCore.OriginalSole - previous.MotionCore.OriginalSole;
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
                previous.Solver.PhysicalWriteAvailable &&
                current.Solver.PhysicalWriteAvailable;
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
                    previous.MotionCore.ConstraintState != "Swing" ||
                    current.MotionCore.ConstraintState != "Landing")
                {
                    continue;
                }
                bool upAvailable =
                    previous.PathContinuity.ComponentUp.sqrMagnitude >
                    TimeEpsilon * TimeEpsilon;
                Vector3 up = upAvailable
                    ? previous.PathContinuity.ComponentUp.normalized
                    : default;
                Vector3 correctionDelta =
                    current.OutputStages.FinalEffectiveCorrection -
                    previous.OutputStages.FinalEffectiveCorrection;
                CharacterFootOutputBoundaryMotion outputMotion =
                    ResolveOutputBoundaryMotion(previous, current);
                double correctionAlongUp = upAvailable
                    ? Vector3.Dot(correctionDelta, up)
                    : 0d;
                bool physicalAvailable =
                    previous.Solver.PhysicalWriteAvailable &&
                    current.Solver.PhysicalWriteAvailable;
                Vector3 physicalAnkleDelta = physicalAvailable
                    ? FinalPhysicalAnkleWorld(current) -
                      FinalPhysicalAnkleWorld(previous)
                    : default;
                Vector3 physicalSoleDelta = physicalAvailable
                    ? FinalSole(current) - FinalSole(previous)
                    : default;
                double previousResidualAfterDecay =
                    previous.PathContinuity.SwingResidualAfterDecay.magnitude;
                bool previousSafetyFloorOwned =
                    previous.OutputStages.SafetyFloorOwner != "None" &&
                    previous.OutputStages.SafetyFloorClamped &&
                    previous.OutputStages.SafetyFloorClampMeters > PositionNoiseFloor;
                bool residualWithinDeadline =
                    previous.PathContinuity.SwingResidualTolerance > 0f &&
                    previousResidualAfterDecay <=
                    previous.PathContinuity.SwingResidualTolerance + TimeEpsilon;
                Vector3 previousFloorCompensation =
                    previous.OutputStages.SafetyFloorOutputCorrection -
                    previous.OutputStages.CorrectionBeforeSafetyFloor;
                double previousFloorCompensationAlongUp = upAvailable
                    ? Vector3.Dot(previousFloorCompensation, up)
                    : 0d;
                bool floorCompensationDroppedAtLanding =
                    previousSafetyFloorOwned &&
                    current.OutputStages.SafetyFloorOwner !=
                    previous.OutputStages.SafetyFloorOwner &&
                    upAvailable &&
                    correctionAlongUp <=
                    -previousFloorCompensationAlongUp +
                    PositionNoiseFloor;
                double stepHeight = upAvailable &&
                                    previous.GroundPath.TargetAvailable
                    ? Vector3.Dot(
                        previous.GroundPath.NextSwingLanding - previous.GroundPath.LastLanding,
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
                        previousFrame = previous.Identity.FrameSequence,
                        frame = current.Identity.FrameSequence,
                        side = current.Identity.Side,
                        eventIdentity = ResolveEventIdentity(current)
                            .ToString(CultureInfo.InvariantCulture),
                        previousSourceIdentity = previous.FormalInput.SourceIdentity,
                        sourceIdentity = current.FormalInput.SourceIdentity,
                        previousSourceCycle = previous.FormalInput.SourceCycle,
                        sourceCycle = current.FormalInput.SourceCycle,
                        previousContributionContinuityIdentity =
                            previous.FormalInput.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        contributionContinuityIdentity =
                            current.FormalInput.ContributionContinuityIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        stateBefore = previous.MotionCore.ConstraintState,
                        stateAfter = current.MotionCore.ConstraintState,
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
                            previous.OutputStages.SafetyFloorClampMeters,
                        previousSafetyFloorClearanceBeforeMeters =
                            previous.OutputStages.SafetyFloorClearanceBeforeMeters,
                        previousSafetyFloorClearanceAfterMeters =
                            previous.OutputStages.SafetyFloorClearanceAfterMeters,
                        previousResidualAfterDecayMeters =
                            previousResidualAfterDecay,
                        swingResidualToleranceMeters =
                            previous.PathContinuity.SwingResidualTolerance,
                        previousFinalEffectiveCorrection =
                            CharacterFootVectorFact.From(
                                previous.OutputStages.FinalEffectiveCorrection),
                        finalEffectiveCorrection =
                            CharacterFootVectorFact.From(
                                current.OutputStages.FinalEffectiveCorrection),
                        previousSafetyFloorMinimumCorrection =
                            CharacterFootVectorFact.From(
                                previous.OutputStages.SafetyFloorMinimumCorrection),
                        previousSafetyFloorOutputCorrection =
                            CharacterFootVectorFact.From(
                                previous.OutputStages.SafetyFloorOutputCorrection),
                        previousSafetyFloorCompensationMeters =
                            previousFloorCompensation.magnitude,
                        previousSafetyFloorCompensationAlongUpMeters =
                            previousFloorCompensationAlongUp,
                        previousSafetyFloorOwner =
                            previous.OutputStages.SafetyFloorOwner,
                        previousSafetyFloorOwnerSurfaceIdentity =
                            previous.OutputStages.SafetyFloorOwnerSurfaceIdentity,
                        previousSafetyFloorOwnerPathIdentity =
                            previous.OutputStages.SafetyFloorOwnerPathIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        safetyFloorOwner = current.OutputStages.SafetyFloorOwner,
                        safetyFloorOwnerSurfaceIdentity =
                            current.OutputStages.SafetyFloorOwnerSurfaceIdentity,
                        safetyFloorOwnerPathIdentity =
                            current.OutputStages.SafetyFloorOwnerPathIdentity.ToString(
                                CultureInfo.InvariantCulture),
                        currentSafetyFloorAvailable =
                            current.OutputStages.SafetyFloorAvailable,
                        currentContactOwnership =
                            current.MotionCore.ContactOwnership,
                        currentContactPlaneAvailable =
                            current.MotionCore.ContactPlaneAvailable,
                        currentContactSurfaceIdentity =
                            current.MotionCore.ContactSurfaceIdentity,
                        stepHeightMeters = stepHeight,
                        stepDirection = stepDirection,
                        previousFormalFootHeightMeters =
                            previous.FormalOutput.FootHeight,
                        formalFootHeightMeters = current.FormalOutput.FootHeight,
                        previousFormalFootHeightAvailable =
                            previous.FormalOutput.Available,
                        formalFootHeightAvailable =
                            current.FormalOutput.Available,
                        previousProgress = previous.MotionCore.SwingProgress,
                        progress = current.MotionCore.SwingProgress,
                        previousTimeToLandingSeconds =
                            previous.Identity.TimeToLandingSeconds,
                        timeToLandingSeconds = current.Identity.TimeToLandingSeconds,
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
                        previous.OutputStages.SafetyFloorClampMeters,
                    ["previousClearanceBeforeMeters"] =
                        previous.OutputStages.SafetyFloorClearanceBeforeMeters,
                    ["previousClearanceAfterMeters"] =
                        previous.OutputStages.SafetyFloorClearanceAfterMeters,
                    ["previousResidualAfterDecayMeters"] =
                        previousResidualAfterDecay,
                    ["swingResidualToleranceMeters"] =
                        previous.PathContinuity.SwingResidualTolerance,
                    ["previousSafetyFloorCompensationMeters"] =
                        previousFloorCompensation.magnitude,
                    ["stepHeightMeters"] = stepHeight,
                    ["previousFormalFootHeightMeters"] =
                        previous.FormalOutput.FootHeight,
                    ["formalFootHeightMeters"] =
                        current.FormalOutput.FootHeight,
                    ["previousProgress"] = previous.MotionCore.SwingProgress,
                    ["progress"] = current.MotionCore.SwingProgress,
                    ["previousTimeToLandingSeconds"] =
                        previous.Identity.TimeToLandingSeconds,
                    ["timeToLandingSeconds"] =
                        current.Identity.TimeToLandingSeconds
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
                        current.OutputStages.SafetyFloorAvailable,
                    ["currentContactPlaneAvailable"] =
                        current.MotionCore.ContactPlaneAvailable,
                    ["previousFormalFootHeightAvailable"] =
                        previous.FormalOutput.Available,
                    ["formalFootHeightAvailable"] =
                        current.FormalOutput.Available
                };
                events.Add(new EventFact(
                    "SwingToLandingFloorHandoff",
                    current.Identity.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    ResolveEventIdentity(current),
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
                    DeltaSeconds(current),
                    metrics,
                    evidence,
                    swingToLandingFloorHandoff: detail));
            }
        }

        static Vector3 FinalSole(FootFrame frame) =>
            (frame.Solver.PhysicalHeelWorld + frame.Solver.PhysicalToeWorld) * 0.5f;

        static Vector3 FinalPhysicalAnkleWorld(FootFrame frame) =>
            frame.RootHierarchy.PoseRootWorldPosition +
            frame.RootHierarchy.PoseRootWorldRotation *
            frame.Solver.PhysicalAnkleComponentPosition;

        static void AnalyzeLockedEvents(
            List<FootFrame> frames,
            List<EventFact> events)
        {
            int index = 0;
            while (index < frames.Count)
            {
                if (frames[index].MotionCore.ConstraintState != "Locked")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].MotionCore.LandingEventIdentity;
                string lockResponse = frames[index].MotionCore.LockResponse;
                if (lockResponse != "FullAnchor" &&
                    lockResponse != "Sliding")
                {
                    throw new InvalidDataException(
                        $"Locked Foot response is invalid Frame={frames[index].Identity.FrameSequence} Side={frames[index].Identity.Side} Response={lockResponse}.");
                }
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].MotionCore.ConstraintState == "Locked" &&
                       frames[index + 1].MotionCore.LandingEventIdentity == eventIdentity &&
                       frames[index + 1].MotionCore.LockResponse == lockResponse)
                {
                    index++;
                }
                int end = index;
                List<FootFrame> window = frames.GetRange(start, end - start + 1);
                double anchorDisplacement = VectorRange(
                    window.Select(frame => frame.MotionCore.Anchor));
                List<double> anchorDistances = window
                    .Select(frame => (double)Vector3.Distance(frame.MotionCore.CorrectedSole, frame.MotionCore.Anchor))
                    .ToList();
                List<double> alongUp = window
                    .Select(frame => (double)Vector3.Dot(
                        frame.MotionCore.CorrectedSole - frame.MotionCore.Anchor,
                        frame.PathContinuity.ComponentUp.normalized))
                    .ToList();
                List<double> horizontalAnchorDistances = window
                    .Select(frame => (double)Vector3.ProjectOnPlane(
                        frame.MotionCore.CorrectedSole - frame.MotionCore.Anchor,
                        frame.PathContinuity.ComponentUp.normalized).magnitude)
                    .ToList();
                double sink = Math.Max(0d, -alongUp.Min());
                double drift = anchorDistances[^1] - anchorDistances[0];
                double visibleStep = MaximumVectorStep(
                    window.Select(frame => frame.MotionCore.CorrectedSole).ToList());
                bool physicalAnchorAvailable = window.All(frame =>
                    frame.Solver.PhysicalWriteAvailable &&
                    frame.Solver.PhysicalWriteCompletionIdentity == frame.Identity.CompletionIdentity &&
                    frame.Lifecycle.CurrentContactAnchorAvailable &&
                    frame.Lifecycle.CurrentContactAnchorEventIdentity == frame.MotionCore.LandingEventIdentity);
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
                    ["lockWeightEntry"] = window[0].FormalInput.LockWeight,
                    ["lockWeightExit"] = window[^1].FormalInput.LockWeight,
                    ["lockWeightMinimum"] = window.Min(frame => frame.FormalInput.LockWeight),
                    ["soleAlongUpEntryMeters"] = alongUp[0],
                    ["soleAlongUpMinimumMeters"] = alongUp.Min(),
                    ["soleAlongUpAbsoluteMaximumMeters"] =
                        alongUp.Max(value => Math.Abs(value)),
                    ["soleDownwardExcursionMeters"] = sink,
                    ["supportEntry"] = window[0].FormalInput.Support,
                    ["supportExit"] = window[^1].FormalInput.Support,
                    ["visibleSoleStepMaximumMeters"] = visibleStep
                };
                if (physicalAnchorAvailable)
                    metrics["physicalSoleAnchorHorizontalDistanceMaximumMeters"] =
                        window.Max(frame => (double)Vector3.ProjectOnPlane(
                            (frame.Solver.PhysicalHeelWorld + frame.Solver.PhysicalToeWorld) * 0.5f -
                            frame.Lifecycle.CurrentContactAnchorPoint,
                            frame.PathContinuity.ComponentUp.normalized).magnitude);
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["physicalAnchorAvailable"] = physicalAnchorAvailable,
                    ["anchorStable"] = anchorDisplacement <= PositionNoiseFloor,
                    ["fullAnchorResponse"] = lockResponse == "FullAnchor",
                    ["groundedThroughout"] = window.All(frame => frame.Action.Grounded),
                    ["lockWeightDecreased"] = window[^1].FormalInput.LockWeight < window[0].FormalInput.LockWeight,
                    ["slidingContinuityContractAvailable"] = false,
                    ["slideDistanceLimitAvailable"] = false,
                    ["slidingResponse"] = lockResponse == "Sliding",
                    ["supportStayedPositive"] = window.All(frame => frame.FormalInput.Support > 0f)
                };
                EventFact fact = new EventFact(
                    lockResponse == "FullAnchor"
                        ? "LockedFullAnchor"
                        : "LockedSliding",
                    window[0].Identity.Side,
                    window[0].Identity.FrameSequence,
                    window[^1].Identity.FrameSequence,
                    PeakDistanceFrame(window),
                    eventIdentity,
                    window[0].FormalInput.SourceIdentity,
                    window[0].FormalInput.SourceCycle,
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
                ulong eventIdentity = frames[index].MotionCore.LandingEventIdentity;
                int surfaceIdentity = frames[index].MotionCore.ContactSurfaceIdentity;
                string constraintState = frames[index].MotionCore.ConstraintState;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].PenetrationAvailable &&
                       frames[index + 1].MotionCore.LandingEventIdentity == eventIdentity &&
                       frames[index + 1].MotionCore.ContactSurfaceIdentity == surfaceIdentity &&
                       frames[index + 1].MotionCore.ConstraintState == constraintState)
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
                int peakFrame = window[0].Identity.FrameSequence;
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
                    peakFrame = frame.Identity.FrameSequence;
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
                    window[0].Identity.Side,
                    window[0].Identity.FrameSequence,
                    window[^1].Identity.FrameSequence,
                    peakFrame,
                    eventIdentity,
                    window[0].FormalInput.SourceIdentity,
                    window[0].FormalInput.SourceCycle,
                    duration,
                    metrics,
                    evidence));
                index++;
            }
        }

        static CharacterFootContactPlanePenetrationSample EvaluatePenetration(
            FootFrame frame)
        {
            Vector3 normal = frame.MotionCore.ContactNormal.normalized;
            double sourceHeelClearance = Vector3.Dot(
                frame.MotionCore.SourceHeel - frame.MotionCore.Anchor,
                normal);
            double sourceToeClearance = Vector3.Dot(
                frame.MotionCore.SourceToe - frame.MotionCore.Anchor,
                normal);
            double finalHeelClearance = Vector3.Dot(
                frame.Solver.PhysicalHeelWorld - frame.MotionCore.Anchor,
                normal);
            double finalToeClearance = Vector3.Dot(
                frame.Solver.PhysicalToeWorld - frame.MotionCore.Anchor,
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
                if (frames[index].MotionCore.ConstraintState != "Releasing")
                {
                    index++;
                    continue;
                }
                int start = index;
                ulong eventIdentity = frames[index].MotionCore.LandingEventIdentity;
                while (index + 1 < frames.Count &&
                       Continuous(frames[index], frames[index + 1]) &&
                       frames[index + 1].MotionCore.ConstraintState == "Releasing" &&
                       frames[index + 1].MotionCore.LandingEventIdentity == eventIdentity)
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
                    ["groundedThroughout"] = window.All(frame => frame.Action.Grounded),
                    ["pathChanged"] = HasPathChange(window)
                };
                EventFact fact = new EventFact(
                    "Release",
                    window[0].Identity.Side,
                    window[0].Identity.FrameSequence,
                    window[^1].Identity.FrameSequence,
                    PeakCorrectionFrame(window),
                    eventIdentity,
                    window[0].FormalInput.SourceIdentity,
                    window[0].FormalInput.SourceCycle,
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
                if (current.Identity.FrameSequence != peakFrame ||
                    !Continuous(previous, current) ||
                    previous.HasAnchor ||
                    current.HasAnchor ||
                    previous.MotionCore.ConstraintState != "Swing" ||
                    current.MotionCore.ConstraintState != "Swing")
                {
                    continue;
                }
                return BuildPathStageAnalysis(previous, current);
            }
            FootFrame first = window.Count > 0 ? window[0] : null;
            FootFrame last = window.Count > 0 ? window[^1] : null;
            return CharacterFootPathStageAnalysis.Unavailable(
                "PeakCorrectionPairUnavailable",
                first?.Identity.FrameSequence ?? 0,
                last?.Identity.FrameSequence ?? 0,
                last?.Identity.Side ?? string.Empty,
                ResolveEventIdentity(last).ToString(CultureInfo.InvariantCulture),
                last?.FormalInput.SourceIdentity ?? string.Empty);
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
                    previous.LandingObservation.RawLandingAvailable && current.LandingObservation.RawLandingAvailable &&
                    previous.GroundPath.TargetAvailable &&
                    current.GroundPath.TargetAvailable,
                    "RawLandingOrPathTargetUnavailable",
                    previous.LandingObservation.RawLanding,
                    current.LandingObservation.RawLanding,
                    previous.GroundPath.NextSwingLanding,
                    current.GroundPath.NextSwingLanding,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.LandingObservation.RawLandingAvailable || current.LandingObservation.RawLandingAvailable ||
                    previous.GroundPath.TargetAvailable ||
                    current.GroundPath.TargetAvailable),
                Stage(
                    CharacterFootPathStageNames.PathTargetToSwingTarget,
                    previous.GroundPath.TargetAvailable &&
                    current.GroundPath.TargetAvailable &&
                    previous.MotionCore.BuilderSwingTargetAvailable &&
                    current.MotionCore.BuilderSwingTargetAvailable,
                    "PathTargetOrSwingTargetUnavailable",
                    previous.GroundPath.NextSwingLanding,
                    current.GroundPath.NextSwingLanding,
                    previous.MotionCore.BuilderSwingTargetCorrection,
                    current.MotionCore.BuilderSwingTargetCorrection,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.MotionCore.BuilderSwingTargetAvailable ||
                    current.MotionCore.BuilderSwingTargetAvailable),
                Stage(
                    CharacterFootPathStageNames.SwingTargetToCapturedResidual,
                    previous.MotionCore.BuilderSwingTargetAvailable &&
                    current.MotionCore.BuilderSwingTargetAvailable &&
                    previous.PathContinuity.PathContinuityEvaluated &&
                    current.PathContinuity.PathContinuityEvaluated,
                    "SwingTargetOrCapturedResidualUnavailable",
                    previous.MotionCore.BuilderSwingTargetCorrection,
                    current.MotionCore.BuilderSwingTargetCorrection,
                    previous.PathContinuity.SwingResidualBeforeDecay,
                    current.PathContinuity.SwingResidualBeforeDecay,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    current.PathContinuity.PathResidualRebuilt),
                Stage(
                    CharacterFootPathStageNames.CapturedResidualToDecayedResidual,
                    previous.PathContinuity.PathContinuityEvaluated &&
                    current.PathContinuity.PathContinuityEvaluated,
                    "ResidualDecayFactsUnavailable",
                    previous.PathContinuity.SwingResidualBeforeDecay,
                    current.PathContinuity.SwingResidualBeforeDecay,
                    previous.PathContinuity.SwingResidualAfterDecay,
                    current.PathContinuity.SwingResidualAfterDecay,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.PathContinuity.PathContinuityEvaluated ||
                    current.PathContinuity.PathContinuityEvaluated),
                Stage(
                    CharacterFootPathStageNames.ResidualOutputToStateOutput,
                    previous.PathContinuity.PathContinuityEvaluated &&
                    current.PathContinuity.PathContinuityEvaluated &&
                    previous.OutputStages.OutputStagesAvailable &&
                    current.OutputStages.OutputStagesAvailable,
                    "ResidualOrStateOutputUnavailable",
                    previous.PathContinuity.ResidualOutputCorrection,
                    current.PathContinuity.ResidualOutputCorrection,
                    previous.OutputStages.CorrectionBeforeSafetyFloor,
                    current.OutputStages.CorrectionBeforeSafetyFloor,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.OutputStages.OutputStagesAvailable ||
                    current.OutputStages.OutputStagesAvailable),
                Stage(
                    CharacterFootPathStageNames.StateOutputToSafetyFloorOutput,
                    previous.OutputStages.OutputStagesAvailable &&
                    current.OutputStages.OutputStagesAvailable &&
                    previous.OutputStages.SafetyFloorOwner != "None" &&
                    current.OutputStages.SafetyFloorOwner != "None",
                    "StateOutputOrGroundEnvelopeUnavailable",
                    previous.OutputStages.CorrectionBeforeSafetyFloor,
                    current.OutputStages.CorrectionBeforeSafetyFloor,
                    previous.OutputStages.SafetyFloorOutputCorrection,
                    current.OutputStages.SafetyFloorOutputCorrection,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.OutputStages.SafetyFloorOwner != "None" ||
                    current.OutputStages.SafetyFloorOwner != "None" ||
                    previous.OutputStages.SafetyFloorClamped || current.OutputStages.SafetyFloorClamped),
                Stage(
                    CharacterFootPathStageNames.FinalCorrectionToEncodedGoal,
                    previous.OutputStages.OutputStagesAvailable &&
                    current.OutputStages.OutputStagesAvailable &&
                    previous.Goal.Available && current.Goal.Available,
                    "FinalCorrectionOrEncodedGoalUnavailable",
                    previous.OutputStages.FinalEffectiveCorrection,
                    current.OutputStages.FinalEffectiveCorrection,
                    previous.Goal.Correction,
                    current.Goal.Correction,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.Goal.Available || current.Goal.Available),
                Stage(
                    CharacterFootPathStageNames.EncodedGoalToSolvedFoot,
                    previous.Goal.Available && current.Goal.Available &&
                    previous.Solver.IkEffectorAvailable &&
                    current.Solver.IkEffectorAvailable,
                    "EncodedGoalOrSolvedFootUnavailable",
                    previous.Goal.Position,
                    current.Goal.Position,
                    previous.Solver.IkSolvedPosition,
                    current.Solver.IkSolvedPosition,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    missing,
                    previous.Solver.IkEffectorAvailable ||
                    current.Solver.IkEffectorAvailable)
            };
            var stateEvidence = new CharacterFootPathStageStateEvidence
            {
                previousState = previous.MotionCore.ConstraintState,
                stateBefore = current.OutputStages.ConstraintStateBefore,
                stateAfter = current.MotionCore.ConstraintState,
                previousLockResponse = previous.MotionCore.LockResponse,
                lockResponseBefore = current.OutputStages.LockResponseBefore,
                lockResponseAfter = current.MotionCore.LockResponse,
                revisionReason = current.PathContinuity.PathRevisionReason,
                residualRebuilt = current.PathContinuity.PathResidualRebuilt,
                targetTrackingApplied = current.PathContinuity.TargetTrackingApplied,
                safetyFloorClamped = current.OutputStages.SafetyFloorClamped
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
                    previousFrame = previous.Identity.FrameSequence,
                    frame = current.Identity.FrameSequence,
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
                    previousFrame = previous.Identity.FrameSequence,
                    frame = current.Identity.FrameSequence,
                    previousCompletionIdentity =
                        previous.Identity.CompletionIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    completionIdentity = current.Identity.CompletionIdentity.ToString(
                        CultureInfo.InvariantCulture),
                    side = current.Identity.Side,
                    previousEventIdentity = ResolveEventIdentity(previous).ToString(
                        CultureInfo.InvariantCulture),
                    eventIdentity = ResolveEventIdentity(current).ToString(
                        CultureInfo.InvariantCulture),
                    previousSourceIdentity = previous.FormalInput.SourceIdentity,
                    sourceIdentity = current.FormalInput.SourceIdentity,
                    previousSourceCycle = previous.FormalInput.SourceCycle,
                    sourceCycle = current.FormalInput.SourceCycle,
                    previousPathInputIdentity =
                        previous.MotionCore.GroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    pathInputIdentity =
                        current.MotionCore.GroundPathInputIdentity.ToString(
                            CultureInfo.InvariantCulture)
                },
                stateEvidence = stateEvidence,
                stageFacts = new CharacterFootPathStageFacts
                {
                    residualCaptureAvailable =
                        current.PathContinuity.PathResidualRebuilt ||
                        current.PathContinuity.TargetTrackingApplied,
                    residualBeforeRevisionPrevious = StageVector(
                        previous.PathContinuity.SwingResidualBeforeRevision),
                    residualBeforeRevision = StageVector(
                        current.PathContinuity.SwingResidualBeforeRevision),
                    capturedResidualPrevious = StageVector(
                        previous.PathContinuity.SwingResidualBeforeDecay),
                    capturedResidual = StageVector(
                        current.PathContinuity.SwingResidualBeforeDecay),
                    groundEnvelopeSafetyCorrectionAvailable =
                        previous.OutputStages.SafetyFloorOwner == "GroundPathEnvelope" &&
                        current.OutputStages.SafetyFloorOwner == "GroundPathEnvelope",
                    groundEnvelopeSafetyCorrectionPrevious = StageVector(
                        previous.OutputStages.SafetyFloorMinimumCorrection),
                    groundEnvelopeSafetyCorrection = StageVector(
                        current.OutputStages.SafetyFloorMinimumCorrection),
                    physicalFootAvailable =
                        previous.Solver.PhysicalWriteAvailable &&
                        current.Solver.PhysicalWriteAvailable,
                    physicalFootPrevious = StageVector(
                        previous.Solver.PhysicalAnkleComponentPosition),
                    physicalFoot = StageVector(
                        current.Solver.PhysicalAnkleComponentPosition)
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
            if (!previous.MotionCore.BuilderSwingTargetAvailable ||
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
                current.MotionCore.BuilderSwingTargetCorrection;
            double reconstructionError = Vector3.Distance(
                pathRevisedTarget,
                actualTarget);
            double phaseDelta = Vector3.Distance(
                previous.MotionCore.BuilderSwingTargetCorrection,
                phaseOnlyTarget);
            double pathDelta = Vector3.Distance(
                phaseOnlyTarget,
                pathRevisedTarget);
            double observedDelta = Vector3.Distance(
                previous.MotionCore.BuilderSwingTargetCorrection,
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
            if (currentState.MotionCore.State != "Accepted" ||
                !currentState.MotionCore.BuilderSwingTargetAvailable ||
                path.GroundPath.State != "Accepted" ||
                path.GroundEnvelopeVertices.Count < 2 ||
                !float.IsFinite(currentState.MotionCore.SwingProgress) ||
                !currentState.FormalOutput.Available ||
                !float.IsFinite(
                    currentState.MotionCore.SwingFormalFootHeight) ||
                currentState.PathContinuity.ComponentUp.sqrMagnitude <=
                PositionNoiseFloor * PositionNoiseFloor ||
                path.GroundPath.ComponentUp.sqrMagnitude <=
                PositionNoiseFloor * PositionNoiseFloor)
            {
                return false;
            }
            Vector3 up = currentState.PathContinuity.ComponentUp.normalized;
            Vector3 groundPathUp = path.GroundPath.ComponentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                path.GroundPath.NextSwingLanding - path.GroundPath.LastLanding,
                groundPathUp);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) ||
                pathLength <= 0.0001f)
            {
                return false;
            }
            float progress = currentState.MotionCore.SwingProgress;
            if (!TrySampleEnvelope(
                    path.GroundEnvelopeVertices.Values,
                    progress,
                    out Vector3 envelopeSample))
            {
                return false;
            }
            float originalSoleHeight = Vector3.Dot(
                currentState.MotionCore.OriginalSole,
                up);
            float rawTargetHeight = Vector3.Dot(
                envelopeSample,
                up) + currentState.MotionCore.SwingFormalFootHeight;
            float targetHeightDelta = rawTargetHeight -
                                      currentState
                                          .PathContinuity.SwingFilteredTargetHeightBefore;
            float maximumHeightDelta = ResolveVerticalHistoryDelta(
                currentState.Timing.DeltaSeconds,
                currentState.PathContinuity.SwingTargetMaximumVerticalSpeed);
            float filteredTargetHeight =
                currentState.PathContinuity.SwingFilteredTargetHeightBefore +
                (currentState.PathContinuity.SwingTargetHeightUpdateHeld
                    ? 0f
                    : currentState.PathContinuity.SwingTargetHeightForceRefreshed
                    ? targetHeightDelta
                    : currentState.PathContinuity.SwingTargetHeightRateLimited
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
            if (frame.PathContinuity.PathCurrentLandingEventIdentity != 0)
                return frame.PathContinuity.PathCurrentLandingEventIdentity;
            if (frame.MotionCore.LandingEventIdentity != 0)
                return frame.MotionCore.LandingEventIdentity;
            return frame.GroundPath.NextSwingLandingEventIdentity;
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
                    previous.MotionCore.GroundPathInputIdentity !=
                    current.MotionCore.GroundPathInputIdentity;
                bool availabilityChanged =
                    current.PathContinuity.PathAvailableBefore != current.PathContinuity.PathAvailableAfter;
                bool comparablePath = current.PathContinuity.PathAvailableBefore &&
                                      current.PathContinuity.PathAvailableAfter;
                bool eventChanged = comparablePath &&
                    current.PathContinuity.PathPreviousLandingEventIdentity !=
                    current.PathContinuity.PathCurrentLandingEventIdentity;
                bool landingPointChanged = comparablePath &&
                    current.PathContinuity.PathLandingPointDelta > current.PathContinuity.PathRevisionDistance;
                bool revisionExpected = availabilityChanged || eventChanged ||
                                        landingPointChanged;
                bool reasonAvailability = HasRevisionReason(
                    current.PathContinuity.PathRevisionReason,
                    "PathAvailabilityChanged");
                bool reasonEvent = HasRevisionReason(
                    current.PathContinuity.PathRevisionReason,
                    "LandingEventChanged");
                bool reasonLandingPoint = HasRevisionReason(
                    current.PathContinuity.PathRevisionReason,
                    "LandingPointChanged");
                bool reasonAvailable = reasonAvailability || reasonEvent ||
                                       reasonLandingPoint;
                bool reasonMatchesExpected =
                    reasonAvailability == availabilityChanged &&
                    reasonEvent == eventChanged &&
                    reasonLandingPoint == landingPointChanged;
                double residualBeforeRevision =
                    current.PathContinuity.SwingResidualBeforeRevision.magnitude;
                double residualBeforeDecay =
                    current.PathContinuity.SwingResidualBeforeDecay.magnitude;
                double residualAfterDecay =
                    current.PathContinuity.SwingResidualAfterDecay.magnitude;
                bool residualGrewWithoutRevision =
                    current.PathContinuity.PathContinuityEvaluated &&
                    !current.PathContinuity.PathResidualRebuilt &&
                    residualAfterDecay > residualBeforeDecay + PositionNoiseFloor;
                bool deadlineReached = current.PathContinuity.PathContinuityEvaluated &&
                    current.PathContinuity.ResidualTimeToLandingSeconds > 0f &&
                    current.PathContinuity.ResidualTimeToLandingSeconds <=
                    DeltaSeconds(current) + TimeEpsilon;
                bool identityOnlyInputChange = inputIdentityChanged &&
                    current.PathContinuity.PathContinuityEvaluated &&
                    !revisionExpected;
                bool relevant = inputIdentityChanged ||
                                current.PathContinuity.PathResidualRebuilt ||
                                current.PathContinuity.TargetTrackingApplied ||
                                revisionExpected ||
                                reasonAvailable ||
                                current.OutputStages.ReleasingCompletedToSwing ||
                                current.OutputStages.SafetyFloorClamped ||
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
                        current.PathContinuity.ResidualAppliedHalfLifeSeconds,
                    ["baseHalfLifeSeconds"] =
                        current.PathContinuity.ResidualBaseHalfLifeSeconds,
                    ["correctionStepMeters"] = correctionStep,
                    ["deadlineHalfLifeSeconds"] =
                        current.PathContinuity.ResidualDeadlineHalfLifeSeconds,
                    ["safetyFloorClearanceAfterMeters"] =
                        current.OutputStages.SafetyFloorClearanceAfterMeters,
                    ["safetyFloorClearanceBeforeMeters"] =
                        current.OutputStages.SafetyFloorClearanceBeforeMeters,
                    ["landingPointDeltaMeters"] =
                        current.PathContinuity.PathLandingPointDelta,
                    ["pathRevisionDistanceMeters"] =
                        current.PathContinuity.PathRevisionDistance,
                    ["swingResidualToleranceMeters"] =
                        current.PathContinuity.SwingResidualTolerance,
                    ["residualAfterDecayMeters"] = residualAfterDecay,
                    ["residualBeforeDecayMeters"] = residualBeforeDecay,
                    ["residualBeforeRevisionMeters"] = residualBeforeRevision,
                    ["safetyFloorClampMeters"] =
                        current.OutputStages.SafetyFloorClampMeters,
                    ["swingTargetDeltaMeters"] = current.PathContinuity.PathTargetDelta,
                    ["timeToLandingSeconds"] =
                        current.PathContinuity.ResidualTimeToLandingSeconds
                };
                var evidence = new SortedDictionary<string, bool>(
                    StringComparer.Ordinal)
                {
                    ["deadlineHalfLifeAvailable"] =
                        current.PathContinuity.ResidualDeadlineHalfLifeAvailable,
                    ["deadlineReached"] = deadlineReached,
                    ["safetyFloorAvailable"] = current.OutputStages.SafetyFloorAvailable,
                    ["expectedLandingEventRevision"] = eventChanged,
                    ["expectedLandingPointRevision"] = landingPointChanged,
                    ["expectedPathAvailabilityRevision"] = availabilityChanged,
                    ["identityOnlyInputChange"] = identityOnlyInputChange,
                    ["pathContinuityEvaluated"] =
                        current.PathContinuity.PathContinuityEvaluated,
                    ["pathInputIdentityChanged"] = inputIdentityChanged,
                    ["pathResidualRebuilt"] = current.PathContinuity.PathResidualRebuilt,
                    ["targetTrackingApplied"] =
                        current.PathContinuity.TargetTrackingApplied,
                    ["pathRevisionExpected"] = revisionExpected,
                    ["pathRevisionReasonMatchesExpected"] =
                        reasonMatchesExpected,
                    ["reasonLandingEventChanged"] = reasonEvent,
                    ["reasonLandingPointChanged"] = reasonLandingPoint,
                    ["reasonPathAvailabilityChanged"] = reasonAvailability,
                    ["releasingCompletedToSwing"] =
                        current.OutputStages.ReleasingCompletedToSwing,
                    ["residualGrewWithoutRevision"] =
                        residualGrewWithoutRevision,
                    ["safetyFloorClamped"] = current.OutputStages.SafetyFloorClamped,
                    ["safetyFloorOwnerGroundPathEnvelope"] =
                        current.OutputStages.SafetyFloorOwner == "GroundPathEnvelope",
                    ["safetyFloorOwnerContactAnchor"] =
                        current.OutputStages.SafetyFloorOwner == "ContactAnchor",
                    ["stateAfterSwing"] =
                        current.MotionCore.ConstraintState == "Swing",
                    ["stateBeforeReleasing"] =
                        current.OutputStages.ConstraintStateBefore == "Releasing"
                };
                events.Add(new EventFact(
                    "PathContinuity",
                    current.Identity.Side,
                    continuous ? previous.Identity.FrameSequence : current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.PathContinuity.PathCurrentLandingEventIdentity != 0
                        ? current.PathContinuity.PathCurrentLandingEventIdentity
                        : current.MotionCore.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
            Dictionary<int, FootFrame> left = capture.Left.ToDictionary(frame => frame.Identity.FrameSequence);
            Dictionary<int, FootFrame> right = capture.Right.ToDictionary(frame => frame.Identity.FrameSequence);
            List<int> frames = left.Keys.Intersect(right.Keys).OrderBy(value => value).ToList();
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = left[frames[i - 1]];
                FootFrame current = left[frames[i]];
                if (!Continuous(previous, current))
                    continue;
                bool changed = previous.PrimarySupport.Side != current.PrimarySupport.Side ||
                               previous.PrimarySupport.LandingEventIdentity != current.PrimarySupport.LandingEventIdentity;
                if (!changed)
                    continue;
                FootFrame previousRight = right[frames[i - 1]];
                FootFrame currentRight = right[frames[i]];
                double goalStep = Vector3.Distance(
                    previous.Pelvis.FinalGoal,
                    current.Pelvis.FinalGoal);
                double physicalStep = Vector3.Distance(
                    previous.Pelvis.PhysicalComponent,
                    current.Pelvis.PhysicalComponent);
                double extensionChange = Math.Max(
                    Math.Abs(current.Solver.IkLegTargetExtensionRatio - previous.Solver.IkLegTargetExtensionRatio),
                    Math.Abs(currentRight.Solver.IkLegTargetExtensionRatio - previousRight.Solver.IkLegTargetExtensionRatio));
                var metrics = new SortedDictionary<string, double>(StringComparer.Ordinal)
                {
                    ["pelvisGoalStepMeters"] = goalStep,
                    ["physicalPelvisStepMeters"] = physicalStep,
                    ["targetExtensionRatioChangeMaximum"] = extensionChange
                };
                var evidence = new SortedDictionary<string, bool>(StringComparer.Ordinal)
                {
                    ["grounded"] = current.Action.Grounded,
                    ["newSupportAvailable"] = current.PrimarySupport.LandingEventIdentity != 0,
                    ["supportSideChanged"] = previous.PrimarySupport.Side != current.PrimarySupport.Side
                };
                EventFact fact = new EventFact(
                    "SupportChange",
                    current.PrimarySupport.Side,
                    previous.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.Identity.FrameSequence,
                    current.PrimarySupport.LandingEventIdentity,
                    current.FormalInput.SourceIdentity,
                    current.FormalInput.SourceCycle,
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
                schema = CharacterFootDiagnosticFormatIdentity.FactsSchema,
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
                    profileId = capture.FootRows[0].Identity.ProfileId,
                    profileRevision = capture.FootRows[0].Identity.ProfileRevision,
                    frameCount = capture.UniqueFrameCount,
                    footRowCount = capture.FootRows.Count,
                    geometryRowCount = capture.GeometryRowCount
                },
                analyzer = new AnalyzerFact
                {
                    id = AnalyzerId,
                    version = CharacterFootDiagnosticFormatIdentity.AnalyzerVersion,
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
                        value => !value.Lifecycle.PostTransitionEvaluated),
                    reentryOutputFactsUnavailableCount = capture.FootRows.Count(
                        value => value.Lifecycle.SameEventContactReentryRefreshed &&
                            (!value.Response.PreviousResponseOutputAvailable ||
                             !value.OutputStages.PlantInterpolationEvaluated ||
                             !value.Response.CorrectionResponseEvaluated ||
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
                        value => !value.PredictionMotion.Available),
                    predictionMotionResetCount = capture.Left.Count(
                        value => value.PredictionMotion.ResetReason != "None"),
                    predictionCurrentResponseCount = capture.Left.Count(
                        value => value.PredictionMotion.CurrentResponseApplied),
                    predictionContinuationResponseCount = capture.Left.Count(
                        value => value.PredictionMotion.ContinuationResponseApplied),
                    predictionMaximumSpeedClampCount = capture.Left.Count(
                        value => value.PredictionMotion.CurrentMaximumSpeedClamped ||
                                 value.PredictionMotion.ContinuationMaximumSpeedClamped),
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
                    groundPathRejectedFootRowCount = capture.FootRows.Count(value => value.GroundPath.State != "Accepted")
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
            !frame.Lifecycle.PostTransitionEvaluated ? null : new
            {
                reason = frame.Lifecycle.PostTransitionReason,
                source = frame.Lifecycle.PostTransitionSource,
                target = frame.Lifecycle.PostTransitionTarget,
                anchorCommand = frame.Lifecycle.PostTransitionAnchorCommand,
                suppressOutput = frame.Lifecycle.PostTransitionSuppressOutput,
                resetInterpolation = frame.Lifecycle.PostTransitionResetInterpolation
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
            RejectRetiredColumns(indices);
            CharacterFootCsvReader<FootFrame> bindings = CharacterFootSampleColumns.Schema.Bind(indices);
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
                FootFrame frame = ParseFrame(cells, bindings);
                reader.Include(frame.Identity.FrameSequence, frame.Identity.Side);
                var key = (frame.Identity.FrameSequence, frame.Identity.Side);
                if (!unique.TryAdd(key, frame))
                {
                    throw new InvalidDataException(
                        $"Foot Motion samples CSV has duplicate Foot row " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                }
            }
            sourceIndices.Add(reader.Complete());
            if (unique.Count == 0)
                throw new InvalidDataException("Foot Motion samples CSV has no Foot rows.");
            List<FootFrame> footRows = unique.Values
                .OrderBy(value => value.Identity.FrameSequence)
                .ThenBy(value => value.Identity.Side, StringComparer.Ordinal)
                .ToList();
            List<FootFrame> left = footRows.Where(value => value.Identity.Side == "Left").OrderBy(value => value.Identity.FrameSequence).ToList();
            List<FootFrame> right = footRows.Where(value => value.Identity.Side == "Right").OrderBy(value => value.Identity.FrameSequence).ToList();
            if (left.Count != right.Count ||
                !left.Select(value => value.Identity.FrameSequence).SequenceEqual(
                    right.Select(value => value.Identity.FrameSequence)))
            {
                throw new InvalidDataException(
                    "Foot Motion samples CSV does not contain one Left and one Right Foot row per frame.");
            }
            for (int i = 0; i < left.Count; i++)
            {
                RequirePredictionMotionPair(left[i], right[i]);
                if (left[i].Identity.CompletionIdentity != right[i].Identity.CompletionIdentity ||
                    left[i].Pelvis.State != right[i].Pelvis.State ||
                    !left[i].Pelvis.HeightTarget.SameAs(right[i].Pelvis.HeightTarget) ||
                    !left[i].Pelvis.SameAs(right[i].Pelvis) ||
                    left[i].Pelvis.Slope != right[i].Pelvis.Slope ||
                    !left[i].Pelvis.Delta.Equals(right[i].Pelvis.Delta) ||
                    !left[i].Pelvis.PhysicalComponent.Equals(right[i].Pelvis.PhysicalComponent) ||
                    !left[i].Pelvis.FinalGoal.Equals(right[i].Pelvis.FinalGoal) ||
                    left[i].Goal.PelvisPositionWeight != right[i].Goal.PelvisPositionWeight ||
                    left[i].Solver.PhysicalWriteAvailable != right[i].Solver.PhysicalWriteAvailable ||
                    left[i].Solver.PhysicalWriteCompletionIdentity != right[i].Solver.PhysicalWriteCompletionIdentity)
                {
                    throw new InvalidDataException(
                        $"Foot Motion shared Pelvis height target differs between Foot rows Frame={left[i].Identity.FrameSequence}.");
                }
            }
            RequireResponseDomainHistory(left);
            RequireResponseDomainHistory(right);
            RequirePelvisHistory(left);
            FootFrame first = footRows[0];
            int geometryRowCount = ReadGeometry(
                geometryPath,
                first.Identity.SampleIdentity,
                unique,
                sourceIndices);
            int frameGapCount = CountTransitions(left, (previous, current) => current.Identity.FrameSequence != previous.Identity.FrameSequence + 1) +
                                CountTransitions(right, (previous, current) => current.Identity.FrameSequence != previous.Identity.FrameSequence + 1);
            int bodyResetCount = CountTransitions(left, (previous, current) => current.BodyCorrection.ResetSequence != previous.BodyCorrection.ResetSequence);
            int sourceChangeCount = CountTransitions(left, (previous, current) => previous.FormalInput.SourceIdentity != current.FormalInput.SourceIdentity) +
                                    CountTransitions(right, (previous, current) => previous.FormalInput.SourceIdentity != current.FormalInput.SourceIdentity);
            return new CsvCapture(
                first.Identity.SampleIdentity,
                first.Identity.ProgramIdentity,
                first.Identity.ProjectionRevision,
                first.Identity.PoseGraphId,
                first.Identity.PoseGraphRevision,
                first.Identity.PosePlanHash,
                geometryRowCount,
                footRows.Select(value => value.Identity.FrameSequence).Distinct().Count(),
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
            if (left.Identity.FrameSequence != right.Identity.FrameSequence ||
                left.PredictionMotion.Available !=
                right.PredictionMotion.Available ||
                !string.Equals(
                    left.PredictionMotion.RejectReason,
                    right.PredictionMotion.RejectReason,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    left.PredictionMotion.ResetReason,
                    right.PredictionMotion.ResetReason,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    left.PredictionMotion.SourceIdentity,
                    right.PredictionMotion.SourceIdentity,
                    StringComparison.Ordinal) ||
                left.PredictionMotion.RawCurrentVelocity !=
                right.PredictionMotion.RawCurrentVelocity ||
                left.PredictionMotion.RawContinuationVelocity !=
                right.PredictionMotion.RawContinuationVelocity ||
                left.PredictionMotion.PreviousStableCurrentVelocity !=
                right.PredictionMotion.PreviousStableCurrentVelocity ||
                left.PredictionMotion.PreviousStableContinuationVelocity !=
                right.PredictionMotion.PreviousStableContinuationVelocity ||
                left.PredictionMotion.StableCurrentVelocity !=
                right.PredictionMotion.StableCurrentVelocity ||
                left.PredictionMotion.StableContinuationVelocity !=
                right.PredictionMotion.StableContinuationVelocity ||
                left.PredictionMotion.CurrentVelocityDelta !=
                right.PredictionMotion.CurrentVelocityDelta ||
                left.PredictionMotion.ContinuationVelocityDelta !=
                right.PredictionMotion.ContinuationVelocityDelta ||
                left.PredictionMotion.VelocityResponseAlpha !=
                right.PredictionMotion.VelocityResponseAlpha ||
                left.PredictionMotion.VelocityDeltaThreshold !=
                right.PredictionMotion.VelocityDeltaThreshold ||
                left.PredictionMotion.VelocitySmoothSpeed !=
                right.PredictionMotion.VelocitySmoothSpeed ||
                left.PredictionMotion.MaximumSpeed != right.PredictionMotion.MaximumSpeed ||
                left.PredictionMotion.CurrentResponseApplied !=
                right.PredictionMotion.CurrentResponseApplied ||
                left.PredictionMotion.ContinuationResponseApplied !=
                right.PredictionMotion.ContinuationResponseApplied ||
                left.PredictionMotion.CurrentMaximumSpeedClamped !=
                right.PredictionMotion.CurrentMaximumSpeedClamped ||
                left.PredictionMotion.ContinuationMaximumSpeedClamped !=
                right.PredictionMotion.ContinuationMaximumSpeedClamped ||
                left.PredictionMotion.Revision !=
                right.PredictionMotion.Revision)
            {
                throw new InvalidDataException(
                    $"Foot Prediction Motion Frame {left.Identity.FrameSequence} differs between feet.");
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
            foreach (string column in surfaceColumns)
                if (!indices.ContainsKey(column))
                    throw new InvalidDataException($"Foot Motion geometry CSV is missing '{column}'.");
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
                string side = Cell("Side");
                int frame = ParseInt(
                    Cell("FrameSequence"),
                    "FrameSequence");
                reader.Include(frame, side);
                if (!footRows.TryGetValue((frame, side), out FootFrame foot))
                {
                    throw new InvalidDataException(
                        $"Foot Motion geometry CSV row {rowCount + 1} has no Foot row.");
                }
                if (ParseUlong(
                        Cell("CompletionIdentity"),
                        "CompletionIdentity") !=
                        foot.Identity.CompletionIdentity ||
                    ParseUlong(
                        Cell("GroundPathInputIdentity"),
                        "GroundPathInputIdentity") !=
                        foot.GroundPath.InputIdentity)
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
                int surfaceIndex = ParseInt(Cell("GroundSurfaceSegmentIndex"), "GroundSurfaceSegmentIndex");
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
                    if (surfaceIndex >= foot.GroundPath.SurfaceSegmentCount ||
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
                if (foot.GroundPath.EnvelopeVertexCount !=
                    foot.GroundEnvelopeVertices.Count)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Envelope geometry count mismatch " +
                        $"Frame={foot.Identity.FrameSequence} Side={foot.Identity.Side}.");
                }
                if (foot.GroundPath.SurfaceSegmentCount < 0 ||
                     foot.GroundPath.SurfaceSegmentCount != foot.GroundSurfaceObservedCount ||
                     foot.GroundPath.SurfaceSegmentCount > 0 && foot.GroundPath.SurfaceWorldRevision == 0 ||
                     foot.GroundPath.State == "Accepted" &&
                     (foot.GroundPath.SurfaceState != CharacterFootGroundSurfaceState.Ready ||
                      foot.GroundPath.SurfaceSegmentCount == 0))
                {
                    throw new InvalidDataException(
                        $"Foot Motion surface geometry facts mismatch Frame={foot.Identity.FrameSequence} Side={foot.Identity.Side}.");
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
            string[] cells,
            CharacterFootCsvReader<FootFrame> bindings)
        {
            FootFrame frame = bindings.Read(cells);
            frame.HasAnchor = frame.MotionCore.LandingEventIdentity != 0 &&
                              frame.MotionCore.ConstraintState != "Swing";
            RequireValidFrame(frame);
            frame.ContactSupportGap = ResolveContactSupportGap(frame);
            return frame;
        }

        static void RequireValidFrame(FootFrame frame)
        {
            if (frame.Identity.FrameSequence <= 0 || frame.Identity.CompletionIdentity == 0)
                throw new InvalidDataException("Foot Motion Foot row lineage is invalid.");
            if (frame.Identity.Side != "Left" && frame.Identity.Side != "Right")
                throw new InvalidDataException(
                    $"Foot Motion Foot row Side '{frame.Identity.Side}' is invalid.");
            RequirePredictionMotion(frame);
            RequireEnum<CharacterFootLandingStepSource>(
                frame.Identity.SelectedStepSource,
                "SelectedStepSource");
            bool selectedStepConsistent = frame.Identity.SelectedStepSource == "None"
                ? frame.Identity.SelectedLandingEventIdentity == 0
                : frame.Identity.SelectedStepSource == "FormalCurrentContact"
                    ? frame.Identity.SelectedLandingEventIdentity ==
                      frame.InputEvents.Current.Identity
                    : frame.Identity.SelectedLandingEventIdentity ==
                      frame.InputEvents.Next.Identity;
            if (!selectedStepConsistent ||
                frame.Identity.StepSelectionMaximumPredictionTimeSeconds <= 0f)
            {
                throw new InvalidDataException(
                    "Foot Motion Step candidate selection facts are inconsistent.");
            }
            RequireFormalApproachProgress(
                frame.FormalOutput.Available,
                frame.OutputEvents.Phase,
                frame.OutputEvents.ApproachProgress,
                frame.OutputEvents.InApproach,
                "FormalEvent");
            RequireFormalApproachProgress(
                frame.FormalInput.Available,
                frame.InputEvents.Phase,
                frame.InputEvents.ApproachProgress,
                frame.InputEvents.InApproach,
                "InputFormalEvent");
            RequireStepPhase(frame.CurrentStep, "CurrentStep");
            RequireStepPhase(frame.IncomingStep, "IncomingStep");
            CharacterFootStepCandidateSample selected = frame.Identity.SelectedStepSource ==
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
                frame.Identity.State,
                "State");
            RequireEnum<CharacterFootLandingQueryCandidateSelectionState>(
                frame.LandingObservation.SelectionState,
                "QueryCandidateSelectionState");
            RequireLandingObservation(frame);
            RequireEnum<CharacterFootSwingMotionState>(
                frame.MotionCore.State,
                "FootMotionState");
            RequireEnum<CharacterFootConstraintState>(
                frame.MotionCore.ConstraintState,
                "FootMotionConstraintState");
            RequireEnum<CharacterFootConstraintState>(
                frame.OutputStages.ConstraintStateBefore,
                "FootMotionConstraintStateBefore");
            RequireEnum<CharacterFootLockResponse>(
                frame.MotionCore.LockResponse,
                "FootMotionLockResponse");
            RequireEnum<CharacterFootLockResponse>(
                frame.OutputStages.LockResponseBefore,
                "FootMotionLockResponseBefore");
            RequireEnum<CharacterFootSafetyFloorOwner>(
                frame.OutputStages.SafetyFloorOwner,
                "FootMotionSafetyFloorOwner");
            RequireEnum<CharacterFootPlantTargetKind>(
                frame.OutputStages.PlantTargetKind,
                "FootMotionPlantTargetKind");
            RequireEnum<CharacterFootLockResponse>(
                frame.OutputStages.PlantLockResponse,
                "FootMotionPlantLockResponse");
            RequireEnum<CharacterFootPlantTargetHeightUpdateReason>(
                frame.Response.PlantTargetHeightUpdateReason,
                "FootMotionPlantTargetHeightUpdateReason");
            RequireEnum<CharacterFootTargetHeightAdoptionMode>(
                frame.PathContinuity.SwingTargetHeightAdoptionMode,
                "FootMotionSwingTargetHeightAdoptionMode");
            RequireFlags<CharacterFootPlantResidualCaptureReason>(
                frame.Response.PlantResidualCaptureReason,
                "FootMotionPlantResidualCaptureReason");
            RequireEnum<CharacterFootCorrectionResponseDeltaDirection>(
                frame.Response.CorrectionResponseDeltaDirection,
                "FootMotionCorrectionResponseDeltaDirection");
            RequireEnum<CharacterFootCorrectionResponseInitializationReason>(
                frame.Response.CorrectionResponseInitializationReason,
                "FootMotionCorrectionResponseInitializationReason");
            RequireFlags<CharacterFootVerticalContinuityOwner>(
                frame.Response.PlantVerticalContinuityOwners,
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
            RequirePelvisHeightTarget(frame);
            RequirePelvisFacts(frame);
            if (frame.MotionCore.LandingReachAvailable &&
                !frame.MotionCore.LandingReachEvaluated ||
                frame.Lifecycle.PostTransitionReason == "LandingCompleted" &&
                (!frame.MotionCore.LandingReachEvaluated ||
                 !frame.MotionCore.LandingReachAvailable))
            {
                throw new InvalidDataException(
                    "Foot Motion Landing Reach facts are inconsistent.");
            }
            bool floorOwnerValid = frame.OutputStages.SafetyFloorOwner switch
            {
                "None" => !frame.OutputStages.SafetyFloorAvailable &&
                          frame.OutputStages.SafetyFloorOwnerSurfaceIdentity == 0 &&
                          frame.OutputStages.SafetyFloorOwnerPathIdentity == 0,
                "GroundPathEnvelope" => frame.OutputStages.SafetyFloorAvailable &&
                                        frame.OutputStages.SafetyFloorOwnerSurfaceIdentity == 0 &&
                                        frame.OutputStages.SafetyFloorOwnerPathIdentity != 0,
                "ContactAnchor" => frame.OutputStages.SafetyFloorAvailable &&
                                   frame.OutputStages.SafetyFloorOwnerSurfaceIdentity != 0 &&
                                   frame.OutputStages.SafetyFloorOwnerPathIdentity == 0 &&
                                   (frame.MotionCore.ConstraintState == "Landing" ||
                                     frame.MotionCore.ConstraintState == "Locked"),
                "PlantTarget" => frame.OutputStages.SafetyFloorAvailable &&
                                 frame.OutputStages.SafetyFloorOwnerSurfaceIdentity != 0 &&
                                 frame.OutputStages.SafetyFloorOwnerPathIdentity == 0 &&
                                 frame.Identity.ApproachPlantTargetPrepared &&
                                 !frame.OutputStages.PlantInterpolationEvaluated &&
                                 (frame.MotionCore.ConstraintState == "Swing" ||
                                  frame.MotionCore.ConstraintState == "UnlockedSupport"),
                _ => false
            };
            if (!floorOwnerValid)
            {
                throw new InvalidDataException(
                    "Foot Motion Safety Floor owner facts are inconsistent.");
            }
            if (frame.OutputStages.PlantInterpolationEvaluated)
            {
                RequireEnum<CharacterFootTargetHeightAdoptionMode>(
                    frame.Response.PlantTargetHeightAdoptionMode,
                    "FootMotionPlantTargetHeightAdoptionMode");
                bool targetAdoptionDirect =
                    frame.Response.PlantTargetHeightAdoptionMode == "Direct";
                bool directTargetUpdate =
                    frame.Response.PlantTargetHeightUpdateReason == "Initialized" ||
                    frame.Response.PlantTargetHeightUpdateReason == "EventChanged" ||
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "VerificationRefresh" ||
                    frame.Response.PlantTargetHeightUpdateReason == "DirectFollow" ||
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "DirectAdoption" ||
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "ForceRefreshDistanceExceeded";
                float targetBudget =
                    frame.Response.PlantTargetMaximumVerticalSpeed *
                    frame.Timing.DeltaSeconds;
                bool targetClampExpected =
                    frame.Response.PlantTargetHeightUpdateReason == "RateLimited";
                bool targetHeightConsistent = Math.Abs(
                    frame.Response.PlantTargetHeightBefore +
                    frame.Response.PlantTargetAppliedVerticalDelta -
                    frame.Response.PlantTargetHeightAfter) <= PositionNoiseFloor;
                Vector3 up = frame.PathContinuity.ComponentUp.normalized;
                bool targetHeightTargetConsistent = Math.Abs(
                    frame.Response.PlantTargetHeightTarget -
                    Vector3.Dot(frame.OutputStages.PlantDesiredPoint, up)) <=
                    PositionNoiseFloor &&
                    Math.Abs(
                        frame.Response.PlantTargetVerticalDelta -
                        (frame.Response.PlantTargetHeightTarget -
                         frame.Response.PlantTargetHeightBefore)) <=
                    PositionNoiseFloor;
                bool distanceForceRefresh =
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "ForceRefreshDistanceExceeded";
                bool verificationRefresh =
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "VerificationRefresh";
                bool heldWithinRevisionDistance =
                    frame.Response.PlantTargetHeightUpdateReason ==
                    "HeldWithinRevisionDistance";
                bool refreshCaptured = HasRevisionReason(
                    frame.Response.PlantResidualCaptureReason,
                    "TargetHeightForceRefreshed");
                bool zeroDeltaVerificationRefresh =
                    frame.Response.PlantTargetHeightUpdateReason == "None" &&
                    Math.Abs(frame.Response.PlantTargetVerticalDelta) <=
                    PositionNoiseFloor &&
                    HasRevisionReason(
                        frame.Response.PlantResidualCaptureReason,
                        "VerificationChanged");
                bool refreshReasonConsistent =
                    frame.Response.PlantTargetForceRefreshed == refreshCaptured &&
                    (frame.Response.PlantTargetForceRefreshed
                        ? distanceForceRefresh ||
                          verificationRefresh ||
                          zeroDeltaVerificationRefresh
                        : !distanceForceRefresh && !verificationRefresh);
                bool residualCaptured =
                    frame.Response.PlantResidualCaptureReason != "None";
                Vector3 outputBefore = frame.MotionCore.OriginalSole +
                                       frame.Response.PlantEffectiveCorrectionBefore;
                Vector3 expectedCapturedBeforeDecay = residualCaptured
                    ? outputBefore - frame.Response.PlantSelectedWorldTarget
                    : frame.Response.PlantWorldResidualBeforeCapture;
                bool residualCaptureConsistent = Vector3.Distance(
                    frame.Response.PlantWorldResidualCapturedBeforeDecay,
                    expectedCapturedBeforeDecay) <= RuntimeGeometryEpsilon;
                bool residualActiveBeforeDecay =
                    frame.Response.PlantWorldResidualCapturedBeforeDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon;
                bool residualDecayRequired = residualActiveBeforeDecay &&
                                             frame.Timing.DeltaSeconds > 0f;
                bool residualDeadlineConsistent =
                    frame.Response.PlantWorldResidualDeadlineHalfLifeAvailable
                    ? float.IsFinite(
                          frame.Response.PlantWorldResidualDeadlineHalfLifeSeconds) &&
                      frame.Response.PlantWorldResidualDeadlineHalfLifeSeconds > 0f
                    : Math.Abs(
                          frame.Response.PlantWorldResidualDeadlineHalfLifeSeconds) <=
                      TimeEpsilon;
                float expectedAppliedHalfLife =
                    frame.Response.PlantWorldResidualDeadlineHalfLifeAvailable
                        ? Math.Min(
                            frame.Response.PlantWorldResidualBaseHalfLifeSeconds,
                            frame.Response.PlantWorldResidualDeadlineHalfLifeSeconds)
                        : frame.Response.PlantWorldResidualBaseHalfLifeSeconds;
                bool residualHalfLifeConsistent =
                    float.IsFinite(
                        frame.Response.PlantWorldResidualBaseHalfLifeSeconds) &&
                    frame.Response.PlantWorldResidualBaseHalfLifeSeconds > 0f &&
                    residualDeadlineConsistent &&
                    (frame.Response.PlantWorldResidualDecayApplied
                        ? float.IsFinite(
                              frame.Response.PlantWorldResidualAppliedHalfLifeSeconds) &&
                          frame.Response.PlantWorldResidualAppliedHalfLifeSeconds > 0f &&
                          Math.Abs(
                              frame.Response.PlantWorldResidualAppliedHalfLifeSeconds -
                              expectedAppliedHalfLife) <= TimeEpsilon &&
                          residualActiveBeforeDecay &&
                          frame.Timing.DeltaSeconds > 0f
                        : Math.Abs(
                              frame.Response.PlantWorldResidualAppliedHalfLifeSeconds) <=
                          TimeEpsilon);
                Vector3 expectedAdvancedResidual =
                    frame.Response.PlantWorldResidualDecayApplied
                        ? AdvanceResidual(
                            frame.Response.PlantWorldResidualCapturedBeforeDecay,
                            frame.Timing.DeltaSeconds,
                            frame.Response.PlantWorldResidualAppliedHalfLifeSeconds)
                        : frame.Response.PlantWorldResidualCapturedBeforeDecay;
                bool expectedClearedAtCompletionTolerance =
                    frame.Response.PlantWorldResidualDecayApplied &&
                    expectedAdvancedResidual.magnitude <=
                    frame.Response.PlantWorldResidualCompletionTolerance;
                Vector3 expectedResidualAfterDecay =
                    expectedClearedAtCompletionTolerance
                        ? default
                        : expectedAdvancedResidual;
                bool residualDecayConsistent =
                    float.IsFinite(
                        frame.Response.PlantWorldResidualCompletionTolerance) &&
                    frame.Response.PlantWorldResidualCompletionTolerance > 0f &&
                    frame.Response.PlantWorldResidualClearedAtCompletionTolerance ==
                    expectedClearedAtCompletionTolerance &&
                    Vector3.Distance(
                        frame.Response.PlantWorldResidualAfterDecay,
                        expectedResidualAfterDecay) <=
                    RuntimeGeometryEpsilon;
                CharacterFootVerticalContinuityOwner expectedOwners =
                    CharacterFootVerticalContinuityOwner.PlantTarget;
                if (frame.Response.PlantTargetHeightUpdateReason != "None" ||
                    frame.Response.PlantTargetVerticalClamped ||
                    frame.Response.PlantTargetForceRefreshed)
                {
                    expectedOwners |=
                        CharacterFootVerticalContinuityOwner.TargetHeightHistory;
                }
                if (residualCaptured ||
                    frame.Response.PlantWorldResidualCapturedBeforeDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.Response.PlantWorldResidualAfterDecay.sqrMagnitude >
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon)
                {
                    expectedOwners |=
                        CharacterFootVerticalContinuityOwner.PlantWorldResidual;
                }
                bool ownersConsistent = Enum.TryParse(
                    frame.Response.PlantVerticalContinuityOwners,
                    out CharacterFootVerticalContinuityOwner actualOwners) &&
                    actualOwners == expectedOwners;
                if (frame.Response.PlantWorldResidualDecayApplied !=
                    residualDecayRequired)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant World Residual decay application is inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                        $"CaptureReason={frame.Response.PlantResidualCaptureReason} " +
                        $"ResidualActive={residualActiveBeforeDecay} " +
                        $"DecayApplied={frame.Response.PlantWorldResidualDecayApplied}.");
                }
                if (frame.OutputStages.PlantTargetEventIdentity == 0 ||
                    frame.OutputStages.PlantTargetKind == "None" ||
                    !FiniteVector(frame.PathContinuity.ComponentUp) ||
                    frame.PathContinuity.ComponentUp.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.Response.PlantTargetHeightEventIdentity !=
                    frame.OutputStages.PlantTargetEventIdentity ||
                    !FiniteVector(frame.OutputStages.PlantDesiredPoint) ||
                    !FiniteVector(frame.OutputStages.PlantFilteredPoint) ||
                    !FiniteVector(frame.Response.PlantPreviousSelectedWorldTarget) ||
                    !FiniteVector(frame.Response.PlantSelectedWorldTarget) ||
                    !FiniteVector(
                        frame.Response.PreviousResponseOutputPoint) ||
                    !FiniteVector(frame.Response.DesiredOutputPoint) ||
                    !FiniteVector(frame.Response.ResponseOutputPoint) ||
                    !FiniteVector(frame.Response.PlantWorldResidualBeforeCapture) ||
                    !FiniteVector(
                        frame.Response.PlantWorldResidualCapturedBeforeDecay) ||
                    !FiniteVector(frame.Response.PlantWorldResidualAfterDecay) ||
                    !FiniteVector(frame.Response.PlantEffectiveCorrectionBefore) ||
                    !FiniteVector(frame.Response.PlantEffectiveCorrectionAfter) ||
                    !FiniteVector(frame.Response.CorrectionResponseRequestedDirection) ||
                    !FiniteVector(frame.Response.CorrectionResponsePreviousDirection) ||
                    !FiniteVector(frame.Response.CorrectionResponseDirection) ||
                    frame.Response.CorrectionResponseRequestedDirection.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    frame.Response.CorrectionResponseDirection.sqrMagnitude <=
                        RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                    Math.Abs(
                        frame.Response.CorrectionResponseRequestedDirection.magnitude -
                        1f) > RuntimeGeometryEpsilon ||
                    Math.Abs(
                        frame.Response.CorrectionResponseDirection.magnitude - 1f) >
                        RuntimeGeometryEpsilon ||
                    frame.Response.CorrectionResponseInitializedBefore &&
                        Math.Abs(
                            frame.Response.CorrectionResponsePreviousDirection.magnitude -
                            1f) > RuntimeGeometryEpsilon ||
                    !float.IsFinite(
                        frame.Response.CorrectionResponseMaximumDirectionChangeDegrees) ||
                    frame.Response.CorrectionResponseMaximumDirectionChangeDegrees <= 0f ||
                    frame.Response.CorrectionResponseMaximumDirectionChangeDegrees > 180f ||
                    !float.IsFinite(
                        frame.Response.CorrectionResponseAppliedDirectionChangeDegrees) ||
                    frame.Response.CorrectionResponseAppliedDirectionChangeDegrees < 0f ||
                    frame.Response.CorrectionResponseAppliedDirectionChangeDegrees >
                        frame.Response.CorrectionResponseMaximumDirectionChangeDegrees +
                        RotationNoiseFloorDegrees ||
                    !float.IsFinite(frame.Response.PlantTargetMaximumVerticalSpeed) ||
                    frame.Response.PlantTargetMaximumVerticalSpeed <= 0f ||
                    !float.IsFinite(frame.Response.PlantTargetHeightBefore) ||
                    !float.IsFinite(frame.Response.PlantTargetHeightTarget) ||
                    !float.IsFinite(frame.Response.PlantTargetVerticalDelta) ||
                    !float.IsFinite(frame.Response.PlantTargetAppliedVerticalDelta) ||
                    !float.IsFinite(frame.Response.PlantTargetHeightAfter) ||
                    !float.IsFinite(frame.Response.PlantTargetForceRefreshDistance) ||
                    frame.Response.PlantTargetForceRefreshDistance <=
                        frame.PathContinuity.PathRevisionDistance ||
                    !refreshReasonConsistent ||
                    targetAdoptionDirect &&
                        (frame.Response.PlantTargetHeightUpdateReason == "RateLimited" ||
                         frame.Response.PlantTargetHeightUpdateReason == "WithinRate" ||
                         heldWithinRevisionDistance ||
                         distanceForceRefresh) ||
                    !targetAdoptionDirect &&
                        frame.Response.PlantTargetHeightUpdateReason ==
                        "DirectAdoption" ||
                    heldWithinRevisionDistance &&
                        (targetAdoptionDirect ||
                         Math.Abs(frame.Response.PlantTargetVerticalDelta) >
                         frame.PathContinuity.PathRevisionDistance + PositionNoiseFloor ||
                         Math.Abs(
                             frame.Response.PlantTargetAppliedVerticalDelta) >
                         PositionNoiseFloor) ||
                    distanceForceRefresh &&
                        Math.Abs(frame.Response.PlantTargetVerticalDelta) <
                        frame.Response.PlantTargetForceRefreshDistance -
                        PositionNoiseFloor ||
                    !directTargetUpdate &&
                    Math.Abs(frame.Response.PlantTargetAppliedVerticalDelta) >
                    targetBudget + PositionNoiseFloor ||
                    frame.Response.PlantTargetVerticalClamped != targetClampExpected ||
                    !targetHeightConsistent ||
                    !targetHeightTargetConsistent ||
                    !residualCaptureConsistent ||
                    !residualHalfLifeConsistent ||
                    !residualDecayConsistent ||
                    !frame.Response.CorrectionResponseEvaluated ||
                    !float.IsFinite(frame.Response.CorrectionResponseDesired) ||
                    !float.IsFinite(frame.Response.CorrectionResponsePrevious) ||
                    !float.IsFinite(frame.Response.CorrectionResponseCurrent) ||
                    !float.IsFinite(
                        frame.Response.CorrectionResponseSelectedSpeed) ||
                    !float.IsFinite(
                        frame.Response.CorrectionResponseAppliedDelta) ||
                    !ownersConsistent ||
                    frame.SelectedSupportTarget.Available &&
                        Vector3.Distance(
                            frame.SelectedSupportTarget.Normal,
                            frame.Response.CorrectionResponseDirection) >
                        RuntimeGeometryEpsilon ||
                    Vector3.Distance(
                        frame.Response.DesiredOutputPoint,
                        frame.Response.PlantSelectedWorldTarget +
                        frame.Response.PlantWorldResidualAfterDecay) >
                    PositionNoiseFloor ||
                    Vector3.Distance(frame.Response.ResponseOutputPoint, ExpectedResponseOutput(frame)) >
                    PositionNoiseFloor ||
                    frame.Response.PreviousResponseOutputAvailable &&
                        Vector3.Distance(
                            frame.Response.PreviousResponseOutputPoint,
                            outputBefore) > PositionNoiseFloor ||
                    Vector3.Distance(
                        frame.Response.PlantEffectiveCorrectionAfter,
                        frame.Response.ResponseOutputPoint -
                        frame.MotionCore.OriginalSole) >
                    PositionNoiseFloor ||
                    Vector3.Distance(
                        frame.Response.PlantEffectiveCorrectionAfter,
                        frame.OutputStages.InterpolationOutputCorrection) >
                    PositionNoiseFloor ||
                    !float.IsFinite(frame.Response.PlantOutputDistance) ||
                    frame.Response.PlantOutputDistance < 0f ||
                    !float.IsFinite(frame.Response.PlantPenetrationDepth) ||
                    frame.Response.PlantPenetrationDepth < 0f)
                {
                    throw new InvalidDataException(
                        $"Foot Motion Plant interpolation facts are inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                        $"TargetHeightUpdateReason={frame.Response.PlantTargetHeightUpdateReason} " +
                        $"Refresh={refreshReasonConsistent} " +
                        $"TargetHeight={targetHeightConsistent && targetHeightTargetConsistent} " +
                        $"ResidualCapture={residualCaptureConsistent} " +
                        $"ResidualHalfLife={residualHalfLifeConsistent} " +
                        $"ResidualDecay={residualDecayConsistent} " +
                        $"ResponseDomain={frame.Response.CorrectionResponseDomain} " +
                        $"Owners={ownersConsistent} " +
                        $"DesiredOutputError={Vector3.Distance(frame.Response.DesiredOutputPoint, frame.Response.PlantSelectedWorldTarget + frame.Response.PlantWorldResidualAfterDecay):R} " +
                        $"ResponseOutputError={Vector3.Distance(frame.Response.ResponseOutputPoint, ExpectedResponseOutput(frame)):R} " +
                        $"PreviousOutputError={Vector3.Distance(frame.Response.PreviousResponseOutputPoint, outputBefore):R} " +
                        $"EffectiveResponseError={Vector3.Distance(frame.Response.PlantEffectiveCorrectionAfter, frame.Response.ResponseOutputPoint - frame.MotionCore.OriginalSole):R} " +
                        $"InterpolationError={Vector3.Distance(frame.Response.PlantEffectiveCorrectionAfter, frame.OutputStages.InterpolationOutputCorrection):R} " +
                        $"DirectionMagnitude={frame.Response.CorrectionResponseDirection.magnitude:R} " +
                        $"PreviousDirectionMagnitude={frame.Response.CorrectionResponsePreviousDirection.magnitude:R} " +
                        $"DirectMode={targetAdoptionDirect} DirectUpdate={directTargetUpdate} " +
                        $"Held={heldWithinRevisionDistance} DistanceRefresh={distanceForceRefresh} " +
                        $"ClampExpected={targetClampExpected} Clamp={frame.Response.PlantTargetVerticalClamped} " +
                        $"TargetBudget={targetBudget:R} AppliedTargetDelta={frame.Response.PlantTargetAppliedVerticalDelta:R} " +
                        $"ForceDistance={frame.Response.PlantTargetForceRefreshDistance:R} PathDistance={frame.PathContinuity.PathRevisionDistance:R} " +
                        $"TargetEvent={frame.OutputStages.PlantTargetEventIdentity} HeightEvent={frame.Response.PlantTargetHeightEventIdentity} Kind={frame.OutputStages.PlantTargetKind} " +
                        $"ResponseEvaluated={frame.Response.CorrectionResponseEvaluated} OutputDistance={frame.Response.PlantOutputDistance:R} Penetration={frame.Response.PlantPenetrationDepth:R} " +
                        $"FinitePreviousTarget={FiniteVector(frame.Response.PlantPreviousSelectedWorldTarget)} FiniteSelectedTarget={FiniteVector(frame.Response.PlantSelectedWorldTarget)} " +
                        $"FiniteResponseScalars={float.IsFinite(frame.Response.CorrectionResponseDesired) && float.IsFinite(frame.Response.CorrectionResponsePrevious) && float.IsFinite(frame.Response.CorrectionResponseCurrent) && float.IsFinite(frame.Response.CorrectionResponseSelectedSpeed) && float.IsFinite(frame.Response.CorrectionResponseAppliedDelta)}.");
                }
            }
            RequireEnum<CharacterFootSwingPathHorizontalAxisState>(
                frame.MotionCore.SwingPathHorizontalAxisState,
                "FootMotionSwingPathHorizontalAxisState");
            RequireEnum<CharacterFootActualEnvelopeIntersectionState>(
                frame.MotionCore.ActualEnvelopeIntersectionState,
                "FootMotionActualEnvelopeIntersectionState");
            RequireEnum<CharacterFootActualFootAxisRegion>(
                frame.MotionCore.ActualFootAxisRegion,
                "FootMotionActualFootAxisRegion");
            RequireEnum<CharacterFootActualEnvelopeCounterfactualState>(
                frame.MotionCore.ActualEnvelopeCounterfactualState,
                "FootMotionActualEnvelopeCounterfactualState");
            if (frame.MotionCore.State == "Accepted" &&
                frame.PathContinuity.ComponentUp.sqrMagnitude >
                TimeEpsilon * TimeEpsilon)
            {
                Vector3 up = frame.PathContinuity.ComponentUp.normalized;
                float originalSoleAlongUp = Vector3.Dot(
                    frame.MotionCore.OriginalSole,
                    up);
                float baselineAlongUp = Vector3.Dot(
                    frame.MotionCore.SwingBaselineSample,
                    up);
                float envelopeAlongUp = Vector3.Dot(
                    frame.MotionCore.SwingEnvelopeSample,
                    up);
                float expectedRawFormalTargetHeight =
                    envelopeAlongUp + frame.MotionCore.SwingFormalFootHeight;
                float expectedEnvelopeMinimumCorrection =
                    envelopeAlongUp - originalSoleAlongUp;
                float expectedBuilderSelectedCorrection = Mathf.Max(
                    0f,
                    expectedRawFormalTargetHeight - originalSoleAlongUp);
                if (Math.Abs(
                        baselineAlongUp -
                        frame.MotionCore.SwingBaselineSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        envelopeAlongUp -
                        frame.MotionCore.SwingEnvelopeSampleAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.MotionCore.SwingRawFormalTargetHeight -
                        expectedRawFormalTargetHeight) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.MotionCore.SwingEnvelopeMinimumCorrection -
                        expectedEnvelopeMinimumCorrection) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.MotionCore.SwingBuilderSelectedCorrection -
                        expectedBuilderSelectedCorrection) >
                    PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion formal Swing height facts are inconsistent.");
                }
                if (frame.MotionCore.BuilderSwingTargetAvailable)
                {
                    float expectedHeightDelta =
                        frame.PathContinuity.SwingRawTargetHeightAlongUp -
                        frame.PathContinuity.SwingFilteredTargetHeightBefore;
                    float maximumHeightDelta = ResolveVerticalHistoryDelta(
                        frame.Timing.DeltaSeconds,
                        frame.PathContinuity.SwingTargetMaximumVerticalSpeed);
                    float expectedAppliedHeightDelta =
                        frame.PathContinuity.SwingTargetHeightUpdateHeld
                            ? 0f
                            : frame.PathContinuity.SwingTargetHeightForceRefreshed
                            ? expectedHeightDelta
                            : frame.PathContinuity.SwingTargetHeightRateLimited
                            ? Mathf.Clamp(
                                expectedHeightDelta,
                                -maximumHeightDelta,
                                maximumHeightDelta)
                            : expectedHeightDelta;
                    float expectedFilteredTargetHeight =
                        frame.PathContinuity.SwingFilteredTargetHeightBefore +
                        expectedAppliedHeightDelta;
                    bool expectedHeightClamp =
                        !frame.PathContinuity.SwingTargetHeightUpdateHeld &&
                        !frame.PathContinuity.SwingTargetHeightForceRefreshed &&
                        frame.PathContinuity.SwingTargetHeightRateLimited &&
                        !Mathf.Approximately(
                            expectedHeightDelta,
                            expectedAppliedHeightDelta);
                    bool directHeightAdoption =
                        frame.PathContinuity.SwingTargetHeightAdoptionMode == "Direct";
                    float expectedFilteredCorrection = Mathf.Max(
                        0f,
                        expectedFilteredTargetHeight - originalSoleAlongUp);
                    if (!frame.PathContinuity.PathContinuityEvaluated ||
                        !frame.PathContinuity.PathAvailableAfter ||
                        frame.PathContinuity.PathCurrentLandingEventIdentity !=
                            frame.MotionCore.LandingEventIdentity ||
                        Math.Abs(
                            frame.PathContinuity.SwingRawTargetHeightAlongUp -
                            expectedRawFormalTargetHeight) >
                        PositionNoiseFloor ||
                        Math.Abs(
                            frame.PathContinuity.SwingTargetHeightDelta -
                            expectedHeightDelta) >
                        PositionNoiseFloor ||
                        Math.Abs(
                            frame.PathContinuity.SwingTargetHeightAppliedDelta -
                            expectedAppliedHeightDelta) >
                        PositionNoiseFloor ||
                        frame.PathContinuity.SwingTargetHeightClamped != expectedHeightClamp ||
                        !float.IsFinite(
                            frame.PathContinuity.SwingTargetHeightForceRefreshDistance) ||
                        frame.PathContinuity.SwingTargetHeightForceRefreshDistance <=
                            frame.PathContinuity.PathRevisionDistance ||
                        directHeightAdoption &&
                            (frame.PathContinuity.SwingTargetHeightForceRefreshed ||
                             frame.PathContinuity.SwingTargetHeightRateLimited ||
                             frame.PathContinuity.SwingTargetHeightClamped) ||
                        frame.PathContinuity.SwingTargetHeightUpdateHeld &&
                            (frame.PathContinuity.SwingTargetHeightForceRefreshed ||
                             frame.PathContinuity.SwingTargetHeightRateLimited) ||
                        frame.PathContinuity.SwingTargetHeightForceRefreshed &&
                            (frame.PathContinuity.SwingTargetHeightRateLimited ||
                             frame.PathContinuity.SwingTargetHeightClamped) ||
                        Math.Abs(
                            frame.PathContinuity.SwingFilteredTargetHeightAlongUp -
                            expectedFilteredTargetHeight) >
                        PositionNoiseFloor ||
                        Vector3.Distance(
                            frame.MotionCore.BuilderSwingTargetCorrection,
                            up * expectedFilteredCorrection) >
                        PositionNoiseFloor)
                    {
                        throw new InvalidDataException(
                            "Foot Motion Builder Swing target facts are inconsistent.");
                    }
                }
                else if (frame.MotionCore.BuilderSwingTargetCorrection.sqrMagnitude >
                         PositionNoiseFloor * PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion unavailable Builder Swing target is nonzero.");
                }
            }
            RequireRevisionReason(frame.PathContinuity.PathRevisionReason);
        }

        static void RequirePredictionMotion(FootFrame frame)
        {
            RequireEnum<CharacterFootPredictionMotionRejectReason>(
                frame.PredictionMotion.RejectReason,
                "PredictionMotionRejectReason");
            RequireEnum<CharacterFootPredictionMotionResetReason>(
                frame.PredictionMotion.ResetReason,
                "PredictionMotionResetReason");
            if (!FiniteVector(frame.BodyCorrection.TargetVelocity) ||
                !FiniteVector(frame.Timing.TimelineCurrentVelocity) ||
                !FiniteVector(frame.Timing.TimelineContinuationVelocity) ||
                !FiniteVector(frame.PredictionMotion.RawCurrentVelocity) ||
                !FiniteVector(frame.PredictionMotion.RawContinuationVelocity) ||
                !FiniteVector(frame.PredictionMotion.PreviousStableCurrentVelocity) ||
                !FiniteVector(frame.PredictionMotion.PreviousStableContinuationVelocity) ||
                !FiniteVector(frame.PredictionMotion.StableCurrentVelocity) ||
                !FiniteVector(frame.PredictionMotion.StableContinuationVelocity) ||
                !FiniteVector(frame.PredictionMotion.CurrentVelocityDelta) ||
                !FiniteVector(frame.PredictionMotion.ContinuationVelocityDelta) ||
                !float.IsFinite(frame.PredictionMotion.VelocityResponseAlpha) ||
                !float.IsFinite(frame.PredictionMotion.VelocityDeltaThreshold) ||
                frame.PredictionMotion.VelocityDeltaThreshold <= 0f ||
                !float.IsFinite(frame.PredictionMotion.VelocitySmoothSpeed) ||
                frame.PredictionMotion.VelocitySmoothSpeed <= 0f ||
                !float.IsFinite(frame.PredictionMotion.MaximumSpeed) ||
                frame.PredictionMotion.MaximumSpeed <=
                frame.PredictionMotion.VelocityDeltaThreshold)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion facts are non-finite or invalid.");
            }
            float expectedAlpha = Mathf.Clamp01(
                frame.PredictionMotion.VelocitySmoothSpeed * frame.Timing.DeltaSeconds);
            Vector2 bodyTargetCurrent = new Vector2(
                frame.BodyCorrection.TargetVelocity.x,
                frame.BodyCorrection.TargetVelocity.z);
            if (Vector2.Distance(
                    frame.PredictionMotion.RawCurrentVelocity,
                    bodyTargetCurrent) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionMotion.RawContinuationVelocity,
                    frame.Timing.TimelineContinuationVelocity) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion input facts are inconsistent.");
            }
            if (!frame.PredictionMotion.Available)
            {
                if (frame.PredictionMotion.RejectReason == "None" ||
                    frame.PredictionMotion.ResetReason != "None" ||
                    frame.PredictionMotion.Revision != 0 ||
                    Math.Abs(frame.PredictionMotion.VelocityResponseAlpha) >
                    TimeEpsilon)
                {
                    throw new InvalidDataException(
                        "Unavailable Foot Prediction Motion facts are inconsistent.");
                }
                return;
            }
            if (frame.PredictionMotion.RejectReason != "None" ||
                frame.PredictionMotion.Revision == 0 ||
                string.IsNullOrWhiteSpace(
                    frame.PredictionMotion.SourceIdentity) ||
                Math.Abs(
                    frame.PredictionMotion.VelocityResponseAlpha - expectedAlpha) >
                TimeEpsilon)
            {
                throw new InvalidDataException(
                    "Available Foot Prediction Motion lineage is invalid.");
            }
            Vector2 expectedCurrentDelta =
                frame.PredictionMotion.RawCurrentVelocity -
                frame.PredictionMotion.PreviousStableCurrentVelocity;
            Vector2 expectedContinuationDelta =
                frame.PredictionMotion.RawContinuationVelocity -
                frame.PredictionMotion.PreviousStableContinuationVelocity;
            bool reset = frame.PredictionMotion.ResetReason != "None";
            bool expectedCurrentResponse = !reset &&
                expectedCurrentDelta.magnitude >
                frame.PredictionMotion.VelocityDeltaThreshold;
            bool expectedContinuationResponse = !reset &&
                expectedContinuationDelta.magnitude >
                frame.PredictionMotion.VelocityDeltaThreshold;
            Vector2 currentCandidate = reset
                ? frame.PredictionMotion.RawCurrentVelocity
                : expectedCurrentResponse
                    ? frame.PredictionMotion.PreviousStableCurrentVelocity +
                      expectedCurrentDelta * expectedAlpha
                    : frame.PredictionMotion.PreviousStableCurrentVelocity;
            Vector2 continuationCandidate = reset
                ? frame.PredictionMotion.RawContinuationVelocity
                : expectedContinuationResponse
                    ? frame.PredictionMotion.PreviousStableContinuationVelocity +
                      expectedContinuationDelta * expectedAlpha
                    : frame.PredictionMotion.PreviousStableContinuationVelocity;
            bool expectedCurrentClamped =
                currentCandidate.magnitude > frame.PredictionMotion.MaximumSpeed;
            bool expectedContinuationClamped =
                continuationCandidate.magnitude > frame.PredictionMotion.MaximumSpeed;
            Vector2 expectedCurrent = Vector2.ClampMagnitude(
                currentCandidate,
                frame.PredictionMotion.MaximumSpeed);
            Vector2 expectedContinuation = Vector2.ClampMagnitude(
                continuationCandidate,
                frame.PredictionMotion.MaximumSpeed);
            if (Vector2.Distance(
                    frame.PredictionMotion.CurrentVelocityDelta,
                    expectedCurrentDelta) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionMotion.ContinuationVelocityDelta,
                    expectedContinuationDelta) > PositionNoiseFloor ||
                frame.PredictionMotion.CurrentResponseApplied !=
                expectedCurrentResponse ||
                frame.PredictionMotion.ContinuationResponseApplied !=
                expectedContinuationResponse ||
                frame.PredictionMotion.CurrentMaximumSpeedClamped !=
                expectedCurrentClamped ||
                frame.PredictionMotion.ContinuationMaximumSpeedClamped !=
                expectedContinuationClamped ||
                Vector2.Distance(
                    frame.PredictionMotion.StableCurrentVelocity,
                    expectedCurrent) > PositionNoiseFloor ||
                Vector2.Distance(
                    frame.PredictionMotion.StableContinuationVelocity,
                    expectedContinuation) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    "Foot Prediction Motion control facts are inconsistent.");
            }
        }

        static void RequireLandingObservation(FootFrame frame)
        {
            bool observationAvailable =
                frame.LandingObservation.ObservationIdentity != 0;
            if (!observationAvailable)
            {
                if (frame.LandingObservation.ObservationWorldRevision != 0 ||
                    frame.LandingObservation.ObservationSourceSampleIdentity != 0 ||
                    frame.LandingObservation.ObservationCacheState != "Unavailable" ||
                    frame.LandingObservation.ObservationQueryExecuted ||
                    frame.LandingObservation.ObservationQueryPurpose != "0" ||
                    frame.LandingObservation.ObservationRefreshMode != "0" ||
                    frame.LandingObservation.ObservationQueryReason != "None" ||
                    frame.LandingObservation.ValidCandidateCount != 0 ||
                    frame.LandingObservation.SelectedAvailable)
                {
                    throw new InvalidDataException(
                        $"Foot Motion unavailable Landing Observation is inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                }
                return;
            }
            bool queried = frame.LandingObservation.ObservationCacheState == "Queried";
            bool reused = frame.LandingObservation.ObservationCacheState == "Reused";
            bool forcedVerification =
                frame.LandingObservation.ObservationRefreshMode ==
                "ForcedPlantVerification";
            bool purposeMatchesRefresh = forcedVerification
                ? frame.LandingObservation.ObservationQueryPurpose ==
                  "CurrentContactVerification"
                : frame.LandingObservation.ObservationQueryPurpose == "FutureLanding" &&
                  (frame.LandingObservation.ObservationRefreshMode == "Thresholded" ||
                   frame.LandingObservation.ObservationRefreshMode ==
                   "ChangedSlidingAdmissionInput");
            if (frame.LandingObservation.ObservationWorldRevision == 0 ||
                frame.LandingObservation.ObservationSourceSampleIdentity == 0 ||
                !queried && !reused ||
                !purposeMatchesRefresh ||
                frame.LandingObservation.QueryPurpose !=
                frame.LandingObservation.ObservationQueryPurpose ||
                forcedVerification && !queried ||
                queried != frame.LandingObservation.ObservationQueryExecuted ||
                queried == (frame.LandingObservation.ObservationQueryReason == "None") ||
                frame.LandingObservation.ObservationCanonicalComponentUp.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon ||
                frame.LandingObservation.ObservationCandidateComponentUp.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon ||
                frame.LandingObservation.ObservationPredictionInputAccumulationDistance <= 0f ||
                frame.LandingObservation.ObservationComponentUpChangeAngleDegrees <= 0f ||
                frame.LandingObservation.QueryDirection.sqrMagnitude <=
                TimeEpsilon * TimeEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Landing Observation cache facts are inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
            bool selected = frame.LandingObservation.SelectionState ==
                            "Selected";
            if (!selected)
            {
                if (frame.LandingObservation.ValidCandidateCount != 0 ||
                    frame.LandingObservation.SelectedAvailable ||
                    frame.LandingObservation.Accepted)
                {
                    throw new InvalidDataException(
                        $"Foot Motion unavailable FutureLanding candidates are inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                }
                return;
            }
            if (frame.LandingObservation.ValidCandidateCount <= 0 ||
                !frame.LandingObservation.SelectedAvailable ||
                frame.LandingObservation.SelectedSurfaceIdentity == 0 ||
                !frame.LandingObservation.Accepted ||
                frame.LandingObservation.SelectedSurfaceIdentity !=
                frame.LandingObservation.SurfaceIdentity ||
                Vector3.Distance(
                    frame.LandingObservation.SelectedPoint,
                    frame.LandingObservation.Point) > PositionNoiseFloor ||
                Math.Abs(
                    frame.LandingObservation.SelectedDistance -
                    frame.LandingObservation.QueryDistance) > PositionNoiseFloor)
            {
                throw new InvalidDataException(
                    $"Foot Motion selected FutureLanding candidate is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
        }

        static void RequireActualFootEnvelopeFacts(FootFrame frame)
        {
            bool accepted = frame.GroundPath.State == "Accepted" &&
                            frame.MotionCore.State == "Accepted" &&
                            frame.MotionCore.ConstraintState == "Swing" &&
                            frame.GroundEnvelopeVertices.Count >= 2;
            float valueMagnitude = Math.Max(
                Math.Abs(frame.MotionCore.ActualFootHorizontalDistance),
                Math.Max(
                    Math.Abs(frame.MotionCore.BaselineHorizontalDistance),
                    Math.Max(
                        Math.Abs(frame.MotionCore.EnvelopeHorizontalDistance),
                        Math.Abs(
                            frame.MotionCore.ActualMinusEnvelopeHorizontalDistance))));
            float finiteSegmentMagnitude = Math.Max(
                Math.Abs(frame.MotionCore.ActualFootClosestPathParameter),
                Math.Max(
                    Math.Abs(frame.MotionCore.ActualFootDistanceAlongAxis),
                    Math.Max(
                        Math.Abs(frame.MotionCore.ActualFootCrossTrackDistance),
                        Math.Abs(
                            frame.MotionCore.ActualFootGroundPathCorridorRadius))));
            if (frame.MotionCore.SwingPathHorizontalAxisState == "Unavailable")
            {
                if (accepted ||
                    frame.MotionCore.ActualEnvelopeIntersectionState != "Unavailable" ||
                    frame.MotionCore.ActualFootAxisRegion != "Unavailable" ||
                    frame.MotionCore.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.MotionCore.ActualFootWithinGroundPathCorridor)
                    throw new InvalidDataException(
                        "Foot Motion unavailable Swing Path axis facts are inconsistent.");
                return;
            }
            if (frame.MotionCore.SwingPathHorizontalAxisState == "InvalidComponentUp")
            {
                if (!accepted ||
                    frame.MotionCore.ActualEnvelopeIntersectionState !=
                    "InvalidComponentUp" ||
                    frame.MotionCore.ActualFootAxisRegion != "Unavailable" ||
                    frame.MotionCore.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    frame.GroundPath.ComponentUp.sqrMagnitude > 0.000001f ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.MotionCore.ActualFootWithinGroundPathCorridor)
                {
                    throw new InvalidDataException(
                        "Foot Motion invalid-up Swing Path axis facts are inconsistent.");
                }
                return;
            }
            Vector3 up = frame.GroundPath.ComponentUp.normalized;
            Vector3 horizontalAxis = Vector3.ProjectOnPlane(
                frame.GroundPath.NextSwingLanding - frame.GroundPath.LastLanding,
                up);
            if (frame.MotionCore.SwingPathHorizontalAxisState == "DegenerateAxis")
            {
                if (!accepted ||
                    frame.MotionCore.ActualEnvelopeIntersectionState !=
                    "DegenerateAxis" ||
                    frame.MotionCore.ActualFootAxisRegion != "Unavailable" ||
                    frame.MotionCore.ActualEnvelopeCounterfactualState != "Unavailable" ||
                    horizontalAxis.sqrMagnitude > 0.00000001f ||
                    valueMagnitude > PositionNoiseFloor ||
                    finiteSegmentMagnitude > PositionNoiseFloor ||
                    frame.MotionCore.ActualFootWithinGroundPathCorridor)
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
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"GroundPathState={frame.GroundPath.State} " +
                    $"FootMotionState={frame.MotionCore.State} " +
                    $"ConstraintState={frame.MotionCore.ConstraintState} " +
                    $"EnvelopeVertices={frame.GroundEnvelopeVertices.Count}.");
            }
            Vector3 direction = horizontalAxis.normalized;
            float expectedActual = Vector3.Dot(
                frame.MotionCore.OriginalSole - frame.GroundPath.LastLanding,
                direction);
            float expectedBaseline = Vector3.Dot(
                frame.MotionCore.SwingBaselineSample - frame.GroundPath.LastLanding,
                direction);
            float expectedEnvelope = Vector3.Dot(
                frame.MotionCore.SwingEnvelopeSample - frame.GroundPath.LastLanding,
                direction);
            if (Math.Abs(
                    frame.MotionCore.ActualFootHorizontalDistance - expectedActual) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.BaselineHorizontalDistance - expectedBaseline) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.EnvelopeHorizontalDistance - expectedEnvelope) >
                PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualMinusEnvelopeHorizontalDistance -
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
                frame.MotionCore.OriginalSole - frame.GroundPath.LastLanding,
                up);
            float crossTrackDistance = Vector3.Distance(
                actualHorizontalOffset,
                horizontalAxis * closestPathParameter);
            string axisRegion = expectedActual < -PositionNoiseFloor
                ? "BeforePathStart"
                : expectedActual > pathLength + PositionNoiseFloor
                    ? "AfterPathEnd"
                    : "WithinPathSegment";
            bool withinGroundPathCorridor = frame.GroundPath.Radius > 0f &&
                crossTrackDistance <=
                frame.GroundPath.Radius + PositionNoiseFloor;
            if (frame.MotionCore.ActualFootAxisRegion != axisRegion ||
                Math.Abs(
                    frame.MotionCore.ActualFootClosestPathParameter -
                    closestPathParameter) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualFootDistanceAlongAxis -
                    distanceAlongAxis) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualFootCrossTrackDistance -
                    crossTrackDistance) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualFootGroundPathCorridorRadius -
                    frame.GroundPath.Radius) > PositionNoiseFloor ||
                frame.MotionCore.ActualFootWithinGroundPathCorridor !=
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
                    previous - frame.GroundPath.LastLanding,
                    direction);
                float currentDistance = Vector3.Dot(
                    current - frame.GroundPath.LastLanding,
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
                if (frame.MotionCore.ActualEnvelopeIntersectionState !=
                        "NoIntersection" ||
                    frame.MotionCore.ActualEnvelopeCounterfactualState !=
                    emptyCounterfactualState ||
                    frame.MotionCore.ActualEnvelopeCandidateCount != 0 ||
                    Math.Abs(
                        frame.MotionCore.ActualEnvelopeMinimumHeightAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.MotionCore.ActualEnvelopeMaximumHeightAlongUp) >
                    PositionNoiseFloor ||
                    Math.Abs(frame.MotionCore.ActualEnvelopeHeightSpan) >
                    PositionNoiseFloor ||
                    frame.MotionCore.ActualEnvelopeHasVerticalEdge ||
                    frame.MotionCore.ActualEnvelopeHasMultipleHeights ||
                    frame.MotionCore.ActualEnvelopeAmbiguous ||
                    frame.MotionCore.ActualProgressEnvelopeCorrectionAvailable)
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
            if (frame.MotionCore.ActualEnvelopeIntersectionState != expectedState ||
                frame.MotionCore.ActualEnvelopeCounterfactualState !=
                expectedCounterfactualState ||
                frame.MotionCore.ActualEnvelopeCandidateCount != heights.Count ||
                Math.Abs(
                    frame.MotionCore.ActualEnvelopeMinimumHeightAlongUp -
                    minimumHeight) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualEnvelopeMaximumHeightAlongUp -
                    maximumHeight) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualEnvelopeHeightSpan - heightSpan) >
                PositionNoiseFloor ||
                frame.MotionCore.ActualEnvelopeHasVerticalEdge != hasVerticalEdge ||
                frame.MotionCore.ActualEnvelopeHasMultipleHeights !=
                hasMultipleHeights ||
                frame.MotionCore.ActualEnvelopeAmbiguous != ambiguous)
            {
                throw new InvalidDataException(
                    "Foot Motion Actual Envelope candidate facts are inconsistent.");
            }
            if (ambiguous)
            {
                if (frame.MotionCore.ActualProgressEnvelopeCorrectionAvailable ||
                    Math.Abs(
                        frame.MotionCore.ActualProgressEnvelopeMinimumCorrection) >
                    PositionNoiseFloor ||
                    Math.Abs(
                        frame.MotionCore.ActualProgressEnvelopeAdvanceAboveBuilderTarget) >
                    PositionNoiseFloor)
                {
                    throw new InvalidDataException(
                        "Foot Motion ambiguous Actual Envelope produced a correction conclusion.");
                }
                return;
            }
            bool correctionAvailable =
                expectedCounterfactualState == "UniqueInCorridor" &&
                frame.MotionCore.BuilderSwingTargetAvailable;
            float originalSoleAlongUp = Vector3.Dot(frame.MotionCore.OriginalSole, up);
            float minimumCorrection = correctionAvailable
                ? minimumHeight - originalSoleAlongUp
                : 0f;
            float builderTargetAlongUp = correctionAvailable
                ? Vector3.Dot(frame.MotionCore.BuilderSwingTargetCorrection, up)
                : 0f;
            float advanceAboveBuilder = correctionAvailable
                ? Mathf.Max(
                    0f,
                    minimumCorrection - builderTargetAlongUp)
                : 0f;
            if (frame.MotionCore.ActualProgressEnvelopeCorrectionAvailable !=
                    correctionAvailable ||
                Math.Abs(
                    frame.MotionCore.ActualProgressEnvelopeMinimumCorrection -
                    minimumCorrection) > PositionNoiseFloor ||
                Math.Abs(
                    frame.MotionCore.ActualProgressEnvelopeAdvanceAboveBuilderTarget -
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
                    frame.Identity.Side,
                    frame.Identity.FrameSequence,
                    frame.Identity.FrameSequence,
                    frame.Identity.FrameSequence,
                    ResolveEventIdentity(frame),
                    frame.FormalInput.SourceIdentity,
                    frame.FormalInput.SourceCycle,
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
                frame.Identity.Side,
                "CurrentSupportTarget");
            bool available = frame.CurrentSupport.RejectReason == "None" &&
                             frame.CurrentSupport.Heel.Accepted &&
                             frame.CurrentSupport.Toe.Accepted &&
                             frame.CurrentSupport.Target.Available;
            if (frame.CurrentSupport.Frame != (ulong)frame.Identity.FrameSequence ||
                frame.CurrentSupport.Completion != frame.Identity.CompletionIdentity ||
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
                frame.Identity.Side,
                "SelectedSupportTarget");
            if (frame.Identity.ApproachPlantTargetPrepared &&
                (!frame.Identity.PlantTargetAvailable ||
                 frame.Identity.PlantTargetEventIdentity == 0 ||
                 frame.Identity.PlantTargetSurfaceIdentity == 0 ||
                 frame.Identity.PlantTargetTrajectoryGeneration == 0 ||
                 string.IsNullOrWhiteSpace(
                     frame.Identity.PlantTargetFutureBodySource) ||
                 !FiniteVector(frame.Identity.PlantTargetPoint) ||
                 !FiniteVector(frame.Identity.PlantTargetNormal) ||
                 frame.Identity.PlantTargetNormal.sqrMagnitude <=
                 RuntimeGeometryEpsilon * RuntimeGeometryEpsilon))
            {
                throw new InvalidDataException(
                    "Foot Motion prepared Approach target facts are inconsistent.");
            }
            if (frame.OutputStages.PlantInterpolationEvaluated &&
                !frame.SelectedSupportTarget.Available)
            {
                throw new InvalidDataException(
                    "Foot Motion evaluated interpolation lacks a selected Support Target.");
            }
        }

        static bool ContactWorldResponse(FootFrame frame) =>
            frame.Response.CorrectionResponseEvaluated && frame.Response.CorrectionResponseDomain == "ContactWorldResidual";

        static bool ScalarResponseEvaluated(FootFrame frame) =>
            frame.Response.CorrectionResponseEvaluated && frame.Response.CorrectionResponseDomain == "AnimationRelativeScalar";

        static bool ExitingContactResponse(FootFrame frame) =>
            ScalarResponseEvaluated(frame) && frame.Response.CorrectionResponseDomainTransferred &&
            frame.Response.CorrectionResponsePreviousDomain == "ContactWorldResidual";

        static double? ScalarResponseValue(FootFrame frame, float value) =>
            ScalarResponseEvaluated(frame) ? (double?)value : null;

        static Vector3 ExpectedResponseOutput(FootFrame frame) =>
            ContactWorldResponse(frame) ? frame.Response.DesiredOutputPoint :
                frame.Response.DesiredOutputPoint + frame.Response.CorrectionResponseDirection *
                (frame.Response.CorrectionResponseCurrent - frame.Response.CorrectionResponseDesired);

        static CharacterFootResponseDomainFact ResponseDomainFact(FootFrame frame) => new
            CharacterFootResponseDomainFact
            {
                domain = frame.Response.CorrectionResponseDomain,
                previousDomain = frame.Response.CorrectionResponsePreviousDomain,
                transferred = frame.Response.CorrectionResponseDomainTransferred,
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
            RequireEnum<CharacterFootCorrectionResponseDomain>(frame.Response.CorrectionResponseDomain,
                "FootMotionCorrectionResponseDomain");
            RequireEnum<CharacterFootCorrectionResponseDomain>(frame.Response.CorrectionResponsePreviousDomain,
                "FootMotionCorrectionResponsePreviousDomain");
            bool evaluated = frame.Response.CorrectionResponseEvaluated;
            bool initialized = frame.Response.CorrectionResponseInitializedBefore;
            bool contact = ContactWorldResponse(frame);
            bool exiting = ExitingContactResponse(frame);
            bool transferExpected = evaluated && initialized &&
                frame.Response.CorrectionResponsePreviousDomain != frame.Response.CorrectionResponseDomain;
            bool valid = frame.Response.CorrectionResponseDomainTransferred == transferExpected &&
                (evaluated ? frame.Response.CorrectionResponseDomain != "None" : frame.Response.CorrectionResponseDomain == "None") &&
                ((evaluated && initialized) == (frame.Response.CorrectionResponsePreviousDomain != "None"));
            if (evaluated)
            {
                bool verifiedSupport = frame.OutputStages.InterpolationPolicy == "VerifiedSupport";
                valid &= contact == verifiedSupport &&
                    (!initialized || frame.Response.PreviousResponseOutputAvailable) &&
                    FiniteVector(frame.Response.DesiredOutputPoint) && FiniteVector(frame.Response.ResponseOutputPoint) &&
                    FiniteVector(frame.Response.PreviousResponseOutputPoint) &&
                    Vector3.Distance(frame.Response.ResponseOutputPoint, ExpectedResponseOutput(frame)) <= PositionNoiseFloor;
                if (contact)
                {
                    valid &= frame.OutputStages.PlantInterpolationEvaluated && frame.OutputStages.PlantTargetVerified &&
                        (frame.OutputStages.PlantTargetKind == "VerifiedAnchor" || frame.OutputStages.PlantTargetKind == "LockedFullAnchor" ||
                         frame.OutputStages.PlantTargetKind == "LockedSliding") &&
                        !frame.Response.CorrectionResponseVisibleOutputTransferred &&
                        frame.Response.CorrectionResponseDesired == 0f && frame.Response.CorrectionResponseBeforeRebase == 0f &&
                        frame.Response.CorrectionResponsePrevious == 0f && frame.Response.CorrectionResponseCurrent == 0f &&
                        frame.Response.CorrectionResponseSelectedSpeed == 0f && frame.Response.CorrectionResponseAppliedDelta == 0f &&
                        frame.Response.CorrectionResponseDeltaDirection == "None" &&
                        Vector3.Distance(frame.Response.DesiredOutputPoint,
                            frame.Response.PlantSelectedWorldTarget + frame.Response.PlantWorldResidualAfterDecay) <= PositionNoiseFloor;
                    if (frame.Response.PlantResidualCaptureReason != "None" && frame.Response.PreviousResponseOutputAvailable)
                        valid &= Vector3.Distance(frame.Response.PlantWorldResidualCapturedBeforeDecay,
                            frame.Response.PreviousResponseOutputPoint - frame.Response.PlantSelectedWorldTarget) <= RuntimeGeometryEpsilon;
                }
                else
                {
                    valid &= !frame.OutputStages.PlantInterpolationEvaluated &&
                        (frame.OutputStages.InterpolationPolicy == "SwingResidual" || frame.OutputStages.InterpolationPolicy == "ReleaseResidual");
                    float desired = Vector3.Dot(frame.Response.DesiredOutputPoint - frame.MotionCore.OriginalSole,
                        frame.Response.CorrectionResponseDirection);
                    float previous = exiting ? desired : frame.Response.CorrectionResponseVisibleOutputTransferred
                        ? Vector3.Dot(frame.Response.PreviousResponseOutputPoint - frame.MotionCore.OriginalSole,
                            frame.Response.CorrectionResponseDirection) : frame.Response.CorrectionResponseBeforeRebase;
                    float delta = desired - previous;
                    bool advance = initialized && !exiting;
                    string direction = !advance || delta == 0f ? "None" : delta > 0f ? "Increase" : "Decrease";
                    float speed = direction == "None" ? 0f : direction == "Increase"
                        ? ExpectedCorrectionResponseIncreaseSpeed : ExpectedCorrectionResponseDecreaseSpeed;
                    float applied = advance ? Mathf.Clamp(delta, -speed * frame.Timing.DeltaSeconds,
                        speed * frame.Timing.DeltaSeconds) : 0f;
                    valid &= (!frame.Response.CorrectionResponseVisibleOutputTransferred || frame.Response.PreviousResponseOutputAvailable) &&
                        Math.Abs(frame.Response.CorrectionResponseDesired - desired) <= PositionNoiseFloor &&
                        Math.Abs(frame.Response.CorrectionResponsePrevious - previous) <= PositionNoiseFloor &&
                        Math.Abs(frame.Response.CorrectionResponseCurrent - previous - applied) <= PositionNoiseFloor &&
                        frame.Response.CorrectionResponseDeltaDirection == direction &&
                        Math.Abs(frame.Response.CorrectionResponseSelectedSpeed - speed) <= TimeEpsilon &&
                        Math.Abs(frame.Response.CorrectionResponseAppliedDelta - applied) <= PositionNoiseFloor &&
                        (initialized || Math.Abs(frame.Response.CorrectionResponseBeforeRebase - desired) <= PositionNoiseFloor);
                    if (exiting)
                    {
                        Vector3 captured = frame.Response.PreviousResponseOutputPoint - frame.MotionCore.OriginalSole - frame.OutputStages.StateTargetCorrection;
                        Vector3 expectedDesired = frame.MotionCore.OriginalSole + frame.OutputStages.StateTargetCorrection +
                            AdvanceResidual(captured, frame.Timing.DeltaSeconds, frame.PathContinuity.ResidualBaseHalfLifeSeconds);
                        valid &= frame.Response.PreviousResponseOutputAvailable && !frame.Response.CorrectionResponseVisibleOutputTransferred &&
                            frame.Response.CorrectionResponseBeforeRebase == 0f && frame.Response.CorrectionResponseAppliedDelta == 0f &&
                            frame.Response.CorrectionResponseSelectedSpeed == 0f &&
                            frame.OutputStages.InterpolationPolicy == "ReleaseResidual" &&
                            frame.Lifecycle.PreTransitionTarget == "Releasing" && frame.Lifecycle.PreTransitionSource != "Releasing" &&
                            frame.PathContinuity.ResidualBaseHalfLifeSeconds > 0f &&
                            Vector3.Distance(frame.Response.DesiredOutputPoint, expectedDesired) <= RuntimeGeometryEpsilon;
                    }
                }
            }
            if (!valid)
                throw new InvalidDataException(
                    $"Foot Motion Correction Response domain is inconsistent Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"Domain={frame.Response.CorrectionResponseDomain} Previous={frame.Response.CorrectionResponsePreviousDomain} " +
                    $"Transferred={frame.Response.CorrectionResponseDomainTransferred} Policy={frame.OutputStages.InterpolationPolicy}.");
        }

        static void RequireResponseDomainHistory(List<FootFrame> frames)
        {
            for (int i = 1; i < frames.Count; i++)
            {
                FootFrame previous = frames[i - 1];
                FootFrame current = frames[i];
                if (!Continuous(previous, current) || previous.Identity.ProfileRevision != current.Identity.ProfileRevision ||
                    previous.Identity.ProgramIdentity != current.Identity.ProgramIdentity || previous.Identity.ProjectionRevision != current.Identity.ProjectionRevision ||
                    !previous.Response.CorrectionResponseEvaluated || !current.Response.CorrectionResponseEvaluated ||
                    !current.Response.CorrectionResponseInitializedBefore)
                    continue;
                bool valid = current.Response.CorrectionResponsePreviousDomain == previous.Response.CorrectionResponseDomain;
                if (!current.Response.CorrectionResponseVisibleOutputTransferred)
                    valid &= current.Response.PreviousResponseOutputAvailable && Vector3.Distance(
                        current.Response.PreviousResponseOutputPoint, previous.Response.ResponseOutputPoint) <= RuntimeGeometryEpsilon;
                if (ScalarResponseEvaluated(current) && !ExitingContactResponse(current))
                    valid &= Math.Abs(current.Response.CorrectionResponseBeforeRebase - previous.Response.CorrectionResponseCurrent) <= PositionNoiseFloor;
                if (ContactWorldResponse(current) && ContactWorldResponse(previous) &&
                    current.Response.PlantResidualCaptureReason == "None" &&
                    current.OutputStages.PlantTargetEventIdentity == previous.OutputStages.PlantTargetEventIdentity)
                    valid &= Vector3.Distance(current.Response.PlantWorldResidualBeforeCapture,
                        previous.Response.PlantWorldResidualAfterDecay) <= RuntimeGeometryEpsilon;
                if (!valid)
                    throw new InvalidDataException(
                        $"Foot Motion committed Response domain history is inconsistent Frame={current.Identity.FrameSequence} Side={current.Identity.Side}.");
            }
        }

        static void RequireCorrectionResponseDirectionHistory(FootFrame frame)
        {
            if (!frame.Response.CorrectionResponseEvaluated)
                return;
            Vector3 requested =
                frame.Response.CorrectionResponseRequestedDirection.normalized;
            bool initialized = frame.Response.CorrectionResponseInitializedBefore;
            float rawAngle = initialized
                ? DirectionAngleDegrees(
                    frame.Response.CorrectionResponsePreviousDirection,
                    requested)
                : 0f;
            bool limited = frame.Response.CorrectionResponseDirectionLimited;
            bool directionLimitFlagConsistent = initialized
                ? limited
                    ? rawAngle >=
                      frame.Response.CorrectionResponseMaximumDirectionChangeDegrees -
                      DirectionComparisonEpsilonDegrees
                    : rawAngle <=
                      frame.Response.CorrectionResponseMaximumDirectionChangeDegrees +
                      DirectionComparisonEpsilonDegrees
                : !limited;
            Vector3 applied = limited
                ? RotateDirectionTowards(
                    frame.Response.CorrectionResponsePreviousDirection,
                    requested,
                    frame.Response.CorrectionResponseMaximumDirectionChangeDegrees)
                : requested;
            float appliedAngle = initialized
                ? DirectionAngleDegrees(
                    frame.Response.CorrectionResponsePreviousDirection,
                    applied)
                : 0f;
            bool initializedThisFrame = !initialized;
            if (!FiniteVector(frame.Response.CorrectionResponseRequestedDirection) ||
                !FiniteVector(frame.Response.CorrectionResponsePreviousDirection) ||
                !FiniteVector(frame.Response.CorrectionResponseDirection) ||
                frame.Response.CorrectionResponseRequestedDirection.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                frame.Response.CorrectionResponseDirection.sqrMagnitude <=
                    RuntimeGeometryEpsilon * RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.Response.CorrectionResponseRequestedDirection.magnitude -
                    1f) > RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.Response.CorrectionResponseDirection.magnitude - 1f) >
                    RuntimeGeometryEpsilon ||
                !float.IsFinite(
                    frame.Response.CorrectionResponseMaximumDirectionChangeDegrees) ||
                frame.Response.CorrectionResponseMaximumDirectionChangeDegrees <= 0f ||
                frame.Response.CorrectionResponseMaximumDirectionChangeDegrees > 180f ||
                !directionLimitFlagConsistent ||
                Vector3.Distance(
                    frame.Response.CorrectionResponseDirection,
                    applied) > RuntimeGeometryEpsilon ||
                Math.Abs(
                    frame.Response.CorrectionResponseAppliedDirectionChangeDegrees -
                    appliedAngle) > RotationNoiseFloorDegrees ||
                frame.Response.CorrectionResponseInitializedThisFrame !=
                    initializedThisFrame ||
                initializedThisFrame &&
                    (frame.Response.CorrectionResponseInitializationReason == "None" ||
                     Vector3.Distance(
                         frame.Response.CorrectionResponsePreviousDirection,
                         requested) > RuntimeGeometryEpsilon) ||
                !initializedThisFrame &&
                    frame.Response.CorrectionResponseInitializationReason != "None" ||
                frame.SelectedSupportTarget.Available &&
                    Vector3.Distance(
                        frame.SelectedSupportTarget.Normal,
                        applied) > RuntimeGeometryEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Correction Response Direction History is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"RawAngle={rawAngle:R} AppliedAngle={appliedAngle:R} " +
                    $"Maximum={frame.Response.CorrectionResponseMaximumDirectionChangeDegrees:R}.");
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
                frame.Lifecycle.PreviousLockRequestMode,
                "FootMotionPreviousLockRequestMode");
            RequireEnum<AnimationFootStepObservationLockMode>(
                frame.Lifecycle.CurrentLockRequestMode,
                "FootMotionCurrentLockRequestMode");
            RequireEnum<CharacterFootLockRequestAvailability>(
                frame.Lifecycle.CurrentLockRequestAvailability,
                "FootMotionCurrentLockRequestAvailability");
            RequireEnum<CharacterFootContactEdge>(
                frame.Lifecycle.ContactEdge,
                "FootMotionContactEdge");
            RequireFlags<CharacterFootGoalOwnershipLossReason>(
                frame.Lifecycle.HardOwnershipLossReason,
                "FootMotionHardOwnershipLossReason");
            Enum.TryParse(
                frame.Lifecycle.CurrentLockRequestMode,
                out AnimationFootStepObservationLockMode currentMode);
            Enum.TryParse(
                frame.Lifecycle.CurrentLockRequestAvailability,
                out CharacterFootLockRequestAvailability availability);
            Enum.TryParse(
                frame.Lifecycle.ContactEdge,
                out CharacterFootContactEdge edge);
            Enum.TryParse(
                frame.Lifecycle.HardOwnershipLossReason,
                out CharacterFootGoalOwnershipLossReason ownershipReason);
            bool formalRequestsLock = frame.FormalInput.Contact > 0f &&
                frame.FormalInput.LockMode !=
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
                !frame.Lifecycle.PreviousLockRequestAvailable
                    ? expectedCurrentRequested
                        ? CharacterFootContactEdge.Rising
                        : CharacterFootContactEdge.None
                    : expectedCurrentRequested
                        ? !frame.Lifecycle.PreviousLockRequested
                            ? CharacterFootContactEdge.Rising
                            : frame.Lifecycle.CurrentLockRequestEventIdentity !=
                              frame.Lifecycle.PreviousLockRequestEventIdentity
                                ? CharacterFootContactEdge.EventChanged
                                : CharacterFootContactEdge.None
                        : frame.Lifecycle.PreviousLockRequested
                            ? CharacterFootContactEdge.Falling
                            : CharacterFootContactEdge.None;
            float expectedSeconds = expectedEdge ==
                                    CharacterFootContactEdge.None
                ? frame.Lifecycle.PreviousContactEdgeSeconds + frame.Timing.DeltaSeconds
                : 0f;
            ulong expectedLatestContact =
                frame.Lifecycle.PreviousLatestContactEventIdentity;
            ulong expectedLatestReleased =
                frame.Lifecycle.PreviousLatestReleasedContactEventIdentity;
            if (expectedEdge == CharacterFootContactEdge.Falling ||
                expectedEdge == CharacterFootContactEdge.EventChanged)
            {
                expectedLatestReleased =
                    frame.Lifecycle.PreviousLockRequestEventIdentity;
            }
            if (expectedEdge == CharacterFootContactEdge.Rising ||
                expectedEdge == CharacterFootContactEdge.EventChanged)
            {
                expectedLatestContact =
                    frame.Lifecycle.CurrentLockRequestEventIdentity;
            }
            ulong expectedCompleted =
                frame.Lifecycle.PreviousCompletedLockWeightEventIdentity;
            if (frame.Lifecycle.CurrentLockRequestEventIdentity != 0 &&
                expectedCompleted != 0 &&
                expectedCompleted != frame.Lifecycle.CurrentLockRequestEventIdentity)
            {
                expectedCompleted = 0;
            }
            if (expectedCurrentRequested &&
                frame.Lifecycle.CurrentLockRequestEventIdentity != 0 &&
                frame.Lifecycle.CurrentLockRequestWeight >=
                1f - RuntimeGeometryEpsilon)
            {
                expectedCompleted = frame.Lifecycle.CurrentLockRequestEventIdentity;
            }
            bool expectedAnchorAvailable =
                frame.Lifecycle.PreviousContactAnchorAvailable;
            ulong expectedAnchorEvent =
                frame.Lifecycle.PreviousContactAnchorEventIdentity;
            ApplyAnchorCommand(
                frame.Lifecycle.PreTransitionAnchorCommand,
                frame.Lifecycle.CurrentLockRequestEventIdentity,
                ref expectedAnchorAvailable,
                ref expectedAnchorEvent,
                ref expectedCompleted);
            if (frame.Lifecycle.PostTransitionEvaluated)
            {
                ApplyAnchorCommand(
                    frame.Lifecycle.PostTransitionAnchorCommand,
                    frame.Lifecycle.CurrentLockRequestEventIdentity,
                    ref expectedAnchorAvailable,
                    ref expectedAnchorEvent,
                    ref expectedCompleted);
            }
            bool expectedReentryRefreshed =
                frame.Lifecycle.PreTransitionReason ==
                "SameEventContactReentryRefresh";
            bool expectedReentryUnavailable =
                frame.Lifecycle.PreTransitionReason == "ContactUnavailable" &&
                expectedCurrentRequested &&
                frame.Lifecycle.CurrentLockRequestEventIdentity != 0 &&
                frame.Lifecycle.CurrentLockRequestEventIdentity ==
                    frame.Lifecycle.PreviousLatestReleasedContactEventIdentity &&
                !frame.Lifecycle.PreviousContactAnchorAvailable;
            bool expectedRetained =
                frame.Lifecycle.PreviousContactAnchorAvailable &&
                frame.Lifecycle.CurrentContactAnchorAvailable &&
                frame.Lifecycle.PreviousContactAnchorEventIdentity ==
                    frame.Lifecycle.CurrentContactAnchorEventIdentity &&
                frame.Lifecycle.PreTransitionAnchorCommand != "Create" &&
                frame.Lifecycle.PreTransitionAnchorCommand != "Release" &&
                (!frame.Lifecycle.PostTransitionEvaluated ||
                 frame.Lifecycle.PostTransitionAnchorCommand != "Create" &&
                 frame.Lifecycle.PostTransitionAnchorCommand != "Release");
            bool expectedReentryHistoryRetained =
                expectedReentryRefreshed && expectedRetained &&
                !frame.Lifecycle.PreTransitionSuppressOutput &&
                !frame.Lifecycle.PreTransitionResetInterpolation &&
                (!frame.Lifecycle.PostTransitionEvaluated ||
                 !frame.Lifecycle.PostTransitionSuppressOutput &&
                 !frame.Lifecycle.PostTransitionResetInterpolation);
            if (expectedRetained && !previousAnchor.SameAs(currentAnchor))
            {
                throw new InvalidDataException(
                    $"Foot Motion retained Anchor geometry or acquisition identity changed " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
            if (expectedReentryRefreshed &&
                (!expectedReentryHistoryRetained ||
                 !frame.Lifecycle.CurrentLockRequested ||
                 frame.Lifecycle.ContactEdge != "Rising" ||
                 frame.Lifecycle.PreTransitionSource != "Releasing" ||
                 frame.Lifecycle.PreTransitionTarget != "Landing" ||
                 frame.Lifecycle.PreTransitionAnchorCommand != "Retain" ||
                 frame.Lifecycle.CurrentLockRequestEventIdentity !=
                 frame.Lifecycle.PreviousContactAnchorEventIdentity))
            {
                throw new InvalidDataException(
                    $"Foot Motion same-event Reentry history is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
            bool anchorCreated = frame.Lifecycle.PreTransitionAnchorCommand == "Create" ||
                frame.Lifecycle.PostTransitionEvaluated &&
                frame.Lifecycle.PostTransitionAnchorCommand == "Create";
            if (anchorCreated && currentAnchor.Available &&
                (currentAnchor.AcquiredFrame != (ulong)frame.Identity.FrameSequence ||
                 currentAnchor.AcquiredCompletion != frame.Identity.CompletionIdentity))
            {
                throw new InvalidDataException(
                    $"Foot Motion created Anchor acquisition identity is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
            CharacterFootGoalOwnershipLossReason expectedOwnershipReason =
                CharacterFootGoalOwnershipLossReason.None;
            if (!frame.Action.Grounded)
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
            bool preOwnershipTransition = frame.Lifecycle.PreTransitionReason ==
                "OwnershipLost";
            bool actionFactsValid =
                float.IsFinite(frame.Action.FootWeight(frame.Identity.Side)) &&
                frame.Action.FootWeight(frame.Identity.Side) >= 0f &&
                frame.Action.FootWeight(frame.Identity.Side) <= 1f &&
                (frame.Action.InstanceIdentity(frame.Identity.Side) == 0
                    ? frame.Action.FootWeight(frame.Identity.Side) == 0f
                    : frame.Action.FootWeight(frame.Identity.Side) > RuntimeGeometryEpsilon);
            bool consistent =
                frame.Lifecycle.LifecycleTransitionEvaluated &&
                float.IsFinite(frame.Lifecycle.PreviousLockRequestWeight) &&
                frame.Lifecycle.PreviousLockRequestWeight >= 0f &&
                frame.Lifecycle.PreviousLockRequestWeight <= 1f &&
                float.IsFinite(frame.Lifecycle.CurrentLockRequestWeight) &&
                frame.Lifecycle.CurrentLockRequestWeight >= 0f &&
                frame.Lifecycle.CurrentLockRequestWeight <= 1f &&
                float.IsFinite(frame.Lifecycle.PreviousContactEdgeSeconds) &&
                frame.Lifecycle.PreviousContactEdgeSeconds >= 0f &&
                float.IsFinite(frame.Lifecycle.CurrentContactEdgeSeconds) &&
                frame.Lifecycle.CurrentContactEdgeSeconds >= 0f &&
                actionFactsValid &&
                currentMode.ToString() == frame.FormalInput.LockMode &&
                Math.Abs(
                    frame.Lifecycle.CurrentLockRequestWeight -
                    frame.FormalInput.LockWeight) <= TimeEpsilon &&
                frame.Lifecycle.CurrentLockRequestEventIdentity ==
                    frame.InputEvents.Current.Identity &&
                availability == expectedAvailability &&
                frame.Lifecycle.CurrentLockRequested == expectedCurrentRequested &&
                edge == expectedEdge &&
                Math.Abs(
                    frame.Lifecycle.CurrentContactEdgeSeconds - expectedSeconds) <=
                    TimeEpsilon &&
                frame.Lifecycle.CurrentLatestContactEventIdentity ==
                    expectedLatestContact &&
                frame.Lifecycle.CurrentLatestReleasedContactEventIdentity ==
                    expectedLatestReleased &&
                frame.Lifecycle.CurrentCompletedLockWeightEventIdentity ==
                    expectedCompleted &&
                frame.Lifecycle.CurrentContactAnchorAvailable ==
                    expectedAnchorAvailable &&
                frame.Lifecycle.CurrentContactAnchorEventIdentity ==
                    expectedAnchorEvent &&
                frame.Lifecycle.SameEventContactReentryRefreshed ==
                    expectedReentryRefreshed &&
                frame.Lifecycle.SameEventContactReentryUnavailable ==
                    expectedReentryUnavailable &&
                frame.Lifecycle.RetainedVerifiedAnchor == expectedRetained &&
                frame.Lifecycle.ReentryInterpolationHistoryRetained ==
                    expectedReentryHistoryRetained &&
                ownershipReason == expectedOwnershipReason &&
                frame.Lifecycle.HardOwnershipLoss == expectedHardOwnershipLoss &&
                preOwnershipTransition == expectedHardOwnershipLoss &&
                frame.Lifecycle.PreTransitionSuppressOutput ==
                    expectedHardOwnershipLoss &&
                frame.Lifecycle.PreTransitionResetInterpolation ==
                    expectedHardOwnershipLoss &&
                !frame.Lifecycle.PostTransitionSuppressOutput &&
                frame.Lifecycle.PostTransitionResetInterpolation ==
                    (frame.Lifecycle.PostTransitionEvaluated &&
                     frame.Lifecycle.PostTransitionReason == "ReleaseCompleted");
            if (!consistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion Lifecycle Transition facts are inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"Edge={frame.Lifecycle.ContactEdge}/{expectedEdge} " +
                    $"Ownership={frame.Lifecycle.HardOwnershipLossReason}/" +
                    $"{expectedOwnershipReason}.");
            }
        }

        static void RequireTransitionExecution(FootFrame frame)
        {
            RequireEnum<CharacterFootTransitionReason>(
                frame.Lifecycle.PreTransitionReason, "FootMotionPreTransitionReason");
            RequireEnum<CharacterFootConstraintState>(
                frame.Lifecycle.PreTransitionSource, "FootMotionPreTransitionSource");
            RequireEnum<CharacterFootConstraintState>(
                frame.Lifecycle.PreTransitionTarget, "FootMotionPreTransitionTarget");
            RequireEnum<CharacterFootAnchorCommand>(
                frame.Lifecycle.PreTransitionAnchorCommand,
                "FootMotionPreTransitionAnchorCommand");
            if (frame.OutputStages.ConstraintStateBefore != frame.Lifecycle.PreTransitionSource)
                throw new InvalidDataException(
                    $"Foot Motion Lifecycle State Before is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            if (!frame.Lifecycle.PostTransitionEvaluated)
            {
                if (frame.Lifecycle.PostTransitionReason != "None" ||
                    frame.Lifecycle.PostTransitionSource != "Swing" ||
                    frame.Lifecycle.PostTransitionTarget != "Swing" ||
                    frame.Lifecycle.PostTransitionAnchorCommand != "None" ||
                    frame.Lifecycle.PostTransitionSuppressOutput ||
                    frame.Lifecycle.PostTransitionResetInterpolation ||
                    frame.Lifecycle.PreTransitionSuppressOutput ||
                    (frame.Resolved.Outcome != "CurrentSupportUnavailable" &&
                     frame.Resolved.Outcome != "SupportTargetUnavailable"))
                {
                    throw new InvalidDataException(
                        $"Foot Motion unevaluated Post Transition is inconsistent " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                }
                return;
            }
            RequireEnum<CharacterFootTransitionReason>(
                frame.Lifecycle.PostTransitionReason, "FootMotionPostTransitionReason");
            RequireEnum<CharacterFootConstraintState>(
                frame.Lifecycle.PostTransitionSource, "FootMotionPostTransitionSource");
            RequireEnum<CharacterFootConstraintState>(
                frame.Lifecycle.PostTransitionTarget, "FootMotionPostTransitionTarget");
            RequireEnum<CharacterFootAnchorCommand>(
                frame.Lifecycle.PostTransitionAnchorCommand,
                "FootMotionPostTransitionAnchorCommand");
            if (frame.Lifecycle.PostTransitionSource != frame.Lifecycle.PreTransitionTarget ||
                frame.Resolved.Outcome == "Ready" &&
                frame.MotionCore.ConstraintState != frame.Lifecycle.PostTransitionTarget)
            {
                throw new InvalidDataException(
                    $"Foot Motion executed Post Transition State is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            }
        }

        static void RequireFormalGoalWeights(FootFrame frame)
        {
            float formal = frame.Lifecycle.FormalFootPlacementWeight;
            if (!float.IsFinite(formal) || formal < 0f || formal > 1f)
                throw new InvalidDataException(
                    $"Foot Motion Formal Foot Placement Weight is invalid " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
            float expectedRotation = 0f;
            float expectedPosition = 0f;
            if (frame.Resolved.Outcome == "Ready")
            {
                expectedRotation = frame.Lifecycle.CurrentContactAnchorAvailable
                    ? formal * frame.Lifecycle.CurrentLockRequestWeight
                    : 0f;
                expectedPosition = formal;
                if (frame.Resolved.ContactAvailable !=
                    frame.Lifecycle.CurrentContactAnchorAvailable ||
                    frame.Resolved.ContactAvailable &&
                    (frame.Resolved.ContactEventIdentity !=
                     frame.Lifecycle.CurrentContactAnchorEventIdentity ||
                     Vector3.Distance(frame.Resolved.ContactPoint,
                         frame.Lifecycle.CurrentContactAnchorPoint) > PositionNoiseFloor))
                {
                    throw new InvalidDataException(
                        $"Foot Motion Resolved Contact does not match Lifecycle Anchor " +
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                }
            }
            bool hasGoal = frame.Resolved.Outcome == "Ready" &&
                (expectedPosition > RuntimeGeometryEpsilon ||
                 expectedRotation > RuntimeGeometryEpsilon);
            float expectedGoalPosition = hasGoal ? expectedPosition : 0f;
            float expectedGoalRotation = hasGoal ? expectedRotation : 0f;
            if (Math.Abs(frame.MotionCore.MotionPositionWeight - expectedPosition) >
                    TimeEpsilon ||
                Math.Abs(frame.MotionCore.MotionRotationWeight - expectedRotation) >
                    TimeEpsilon ||
                Math.Abs(frame.Resolved.PositionWeight - expectedPosition) >
                    TimeEpsilon ||
                Math.Abs(frame.Resolved.RotationWeight - expectedRotation) >
                    TimeEpsilon ||
                Math.Abs(frame.Goal.PositionWeight -
                    expectedGoalPosition) > TimeEpsilon ||
                Math.Abs(frame.Goal.RotationWeight -
                    expectedGoalRotation) > TimeEpsilon)
            {
                throw new InvalidDataException(
                    $"Foot Motion Formal Goal weight policy is inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"Formal={formal:R} Position={expectedPosition:R} " +
                    $"Rotation={expectedRotation:R}.");
            }
        }

        static void RequireFootGoalComponentFacts(FootFrame frame)
        {
            Vector3 target = Vector3.Lerp(
                frame.Solver.IkLegOriginalAnkle, frame.Goal.Position, frame.Goal.PositionWeight);
            bool physicalAvailable = frame.Solver.PhysicalWriteAvailable && frame.Solver.IkLegAvailable &&
                frame.Goal.PositionWeight > 0f;
            float expectedResidual = physicalAvailable
                ? Vector3.Distance(frame.Solver.PhysicalAnkleComponentPosition,
                    frame.Solver.IkLegOriginalAnkle +
                    (frame.Goal.Position - frame.Solver.IkLegOriginalAnkle) * frame.Goal.PositionWeight)
                : 0f;
            if (frame.Solver.IkLegAvailable &&
                    Vector3.Distance(frame.Solver.IkLegTargetAnkle, target) > PositionNoiseFloor ||
                frame.Solver.PhysicalAnkleGoalResidual < 0f ||
                Math.Abs(frame.Solver.PhysicalAnkleGoalResidual - expectedResidual) > PositionNoiseFloor ||
                physicalAvailable && frame.Solver.PhysicalWriteCompletionIdentity != frame.Identity.CompletionIdentity)
                throw new InvalidDataException(
                    $"Foot Motion component Goal and physical residual facts are inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} Residual={frame.Solver.PhysicalAnkleGoalResidual:R}/{expectedResidual:R}.");
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
                frame.Identity.Side,
                "ResolvedSupportTarget");
            if (frame.Resolved.Frame != (ulong)frame.Identity.FrameSequence ||
                frame.Resolved.Completion != frame.Identity.CompletionIdentity ||
                frame.Resolved.Side != frame.Identity.Side ||
                string.IsNullOrWhiteSpace(frame.Identity.ProfileId) ||
                string.IsNullOrWhiteSpace(frame.Identity.ProfileRevision) ||
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
            if (!frame.Response.CorrectionResponseEvaluated)
            {
                throw new InvalidDataException(
                    "Foot Motion Ready visible policy did not evaluate Correction Response exactly once.");
            }
            Vector3 expectedEffectiveSole = Vector3.LerpUnclamped(
                frame.MotionCore.OriginalSole,
                frame.Resolved.FinalSole,
                frame.Resolved.PositionWeight);
            Vector3 expectedEffectiveSoleCorrection =
                frame.Resolved.EffectiveSoleFromContacts - frame.MotionCore.OriginalSole;
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
                frame.MotionCore.SourceAnkleRotation,
                expectedGoalRotation,
                frame.Resolved.RotationWeight);
            Quaternion rotationDelta = NormalizeRotation(
                MultiplyRotation(
                    expectedEffectiveRotation,
                    InverseRotation(frame.MotionCore.SourceAnkleRotation)));
            Vector3 expectedEffectiveAnkle = expectedEffectiveSole -
                RotateVector(
                    rotationDelta,
                    frame.MotionCore.OriginalSole - frame.MotionCore.OriginalAnkle);
            Vector3 expectedGoalAnkle = frame.Resolved.PositionWeight >
                                        RuntimeGeometryEpsilon
                ? frame.MotionCore.OriginalAnkle +
                  (expectedEffectiveAnkle - frame.MotionCore.OriginalAnkle) /
                  frame.Resolved.PositionWeight
                : frame.MotionCore.OriginalAnkle;
            if (!frame.Resolved.SupportTarget.Available ||
                Vector3.Distance(
                    frame.Resolved.GoalTargetCorrection,
                    frame.Resolved.FinalSole - frame.MotionCore.OriginalSole) >
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
                    $"Foot Motion Pelvis {part} facts are inconsistent Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
        }

        static void RequirePelvisLeg(CharacterFootPelvisLegSample leg, Vector3 up, FootFrame frame)
        {
            bool requested = leg.Role != CharacterFootPelvisLegReachRole.None;
            RequirePelvis(leg.Requested == requested &&
                leg.Available == (leg.Status == CharacterFootPelvisLegReachStatus.Available), frame, "leg availability");
            if (!requested)
            {
                RequirePelvis(leg.SameAs(new CharacterFootPelvisLegSample()), frame, "unrequested leg");
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
            CharacterFootPelvisReachSample reach = frame.Pelvis.Reach;
            bool specified = !reach.ComponentUp.Equals(Vector3.zero);
            RequirePelvis(FiniteVector(reach.ComponentUp) &&
                (!specified || Math.Abs(reach.ComponentUp.sqrMagnitude - 1f) <= RuntimeGeometryEpsilon) &&
                (specified || !frame.Pelvis.Response.Evaluated && !frame.Pelvis.HeightTarget.Available &&
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
                    CharacterFootPelvisLegSample first = reach.Left.Requested ? reach.Left : reach.Right;
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
            CharacterFootPelvisLegSample leg = frame.Identity.Side == "Left" ? reach.Left : reach.Right;
            bool primaryExpected = frame.Pelvis.State == "Accepted" && frame.Pelvis.SupportSide == frame.Identity.Side &&
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
                RequirePelvis(frame.Pelvis.HeightTarget.Available && frame.PrimarySupport.HasValue &&
                    frame.PrimarySupport.Side == frame.Identity.Side && frame.Pelvis.SupportSide == frame.Identity.Side, frame, "primary lineage");
                if (!leg.FootTarget)
                    RequirePelvis(leg.EventIdentity == frame.PrimarySupport.LandingEventIdentity &&
                        Vector3.Distance(leg.Hip, frame.Pelvis.Posture.Hip) <= RuntimeGeometryEpsilon &&
                        Vector3.Distance(leg.TargetAnkle, frame.Pelvis.Posture.TargetAnkle) <= RuntimeGeometryEpsilon &&
                        PelvisClose(leg.LegLength, frame.Pelvis.Posture.LegLength), frame, "primary-only input");
            }
            float applied = frame.Pelvis.Response.Output * frame.Pelvis.Response.PositionWeight;
            bool footAvailable = leg.FootTarget && leg.Available &&
                applied >= leg.MinimumAlongUp - RuntimeGeometryEpsilon &&
                applied <= leg.MaximumAlongUp + RuntimeGeometryEpsilon;
            RequirePelvis(frame.MotionCore.LandingReachEvaluated == leg.FootTarget && frame.MotionCore.LandingReachAvailable == footAvailable,
                frame, "weighted Foot reach result");
        }

        static void RequirePelvisPosture(FootFrame frame)
        {
            const float endpoint = 0.005f;
            CharacterFootPelvisPostureSample posture = frame.Pelvis.Posture;
            RequirePelvis(posture.Evaluated == frame.Pelvis.HeightTarget.Available, frame, "posture execution");
            if (!posture.Evaluated)
            {
                RequirePelvis(posture.SameAs(new CharacterFootPelvisPostureSample()), frame, "unevaluated posture");
                return;
            }
            Vector3 up = frame.Pelvis.Reach.ComponentUp;
            RequirePelvis(up.Equals(frame.Pelvis.HeightTarget.ComponentUp) &&
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
            float requested = frame.Pelvis.HeightTarget.RequestedOffsetAlongUp;
            float preferred = available ? Mathf.Clamp(requested, minimum, maximum) : requested;
            preferred = Mathf.Clamp(preferred, Mathf.Min(0f, requested), Mathf.Max(0f, requested));
            RequirePelvis(posture.Available == available && PelvisClose(posture.UsableLegLength, usable) &&
                PelvisClose(posture.MinimumAlongUp, minimum) && PelvisClose(posture.MaximumAlongUp, maximum) &&
                PelvisClose(posture.OffsetAlongUp, preferred) &&
                posture.TargetAdjusted == (Math.Abs(preferred - requested) > RuntimeGeometryEpsilon), frame, "posture preference");
        }

        static void RequirePelvisFacts(FootFrame frame)
        {
            RequireEnum<CharacterFootStrideState>(frame.Pelvis.State, "StrideState");
            RequirePelvis(frame.Pelvis.HeightTarget.Available == (frame.Pelvis.State == "Accepted"),
                frame, "height target execution");
            RequirePelvisObservation(frame);
            RequirePelvisReach(frame);
            RequirePelvisPosture(frame);
            CharacterFootPelvisResponseSample response = frame.Pelvis.Response;
            CharacterFootPelvisReachSample reach = frame.Pelvis.Reach;
            if (!response.Evaluated)
            {
                RequirePelvis(response.SameAs(new CharacterFootPelvisResponseSample()) && !frame.Pelvis.HeightTarget.Available &&
                    frame.Pelvis.State != "Releasing" &&
                    frame.Pelvis.Delta.Equals(Vector3.zero) && frame.Goal.PelvisPositionWeight == 0f, frame, "unevaluated response");
                return;
            }
            bool releasing = !frame.Pelvis.HeightTarget.Available;
            RequirePelvis(releasing
                ? response.HadPreviousState &&
                  frame.Pelvis.State == (response.Completed ? "Rejected" : "Releasing")
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
                (releasing ? CharacterFootStrideSlope.Flat : frame.Pelvis.Slope);
            CharacterFootPelvisSpringHandoffReason handoff = CharacterFootPelvisSpringHandoffReason.None;
            if (response.SupportChanged) handoff |= CharacterFootPelvisSpringHandoffReason.SupportChanged;
            if (slopeChanged) handoff |= CharacterFootPelvisSpringHandoffReason.SlopeChanged;
            if (crossed) handoff |= CharacterFootPelvisSpringHandoffReason.TargetCrossedOutput;
            bool reset = (handoff != CharacterFootPelvisSpringHandoffReason.None || response.PreviousVelocity > 0f) &&
                Math.Abs(direction) > RuntimeGeometryEpsilon && response.PreviousVelocity * direction < 0f;
            float inputVelocity = reset ? 0f : response.PreviousVelocity;
            float output = response.PreviousOutput, velocity = inputVelocity;
            if (frame.Timing.DeltaSeconds > 0f)
            {
                float omega = response.Frequency * 2f * Mathf.PI;
                float x = response.PreviousOutput - target;
                float j = inputVelocity + omega * x;
                float decay = Mathf.Exp(-omega * frame.Timing.DeltaSeconds);
                output = target + (x + j * frame.Timing.DeltaSeconds) * decay;
                velocity = (inputVelocity - omega * j * frame.Timing.DeltaSeconds) * decay;
            }
            float integrated = output;
            bool completed = releasing && Math.Abs(output) <= RuntimeGeometryEpsilon &&
                Math.Abs(velocity) <= RuntimeGeometryEpsilon;
            if (completed) { output = 0f; velocity = 0f; }
            float visibleTolerance = reach.Left.FootTarget || reach.Right.FootTarget ? RuntimeGeometryEpsilon : 0.005f;
            float weight = !completed && Math.Abs(output) > visibleTolerance
                ? frame.Lifecycle.FormalFootPlacementWeight : 0f;
            RequirePelvis(response.Handoff == handoff && response.VelocityReset == reset &&
                PelvisClose(response.InputVelocity, inputVelocity) && PelvisClose(response.Target, target) &&
                PelvisClose(response.IntegratedOutput, integrated) && PelvisClose(response.Output, output) &&
                PelvisClose(response.Velocity, velocity) && response.Completed == completed &&
                PelvisClose(response.PositionWeight, weight) && PelvisClose(frame.Goal.PelvisPositionWeight, weight) &&
                Vector3.Distance(frame.Pelvis.Delta, reach.ComponentUp * output) <= RuntimeGeometryEpsilon,
                frame, "single spring response");
        }

        static bool PelvisPhysicalAvailable(FootFrame frame) =>
            frame.Solver.PhysicalWriteAvailable &&
            frame.Solver.PhysicalWriteCompletionIdentity == frame.Identity.CompletionIdentity;

        static void RequirePelvisObservation(FootFrame frame)
        {
            CharacterFootPelvisObservationSample observation = frame.Pelvis.Observation;
            bool poseExpected = frame.Pelvis.State == "Accepted" || frame.Pelvis.State == "Releasing";
            RequirePelvis(observation.PoseInputAvailable == poseExpected &&
                FiniteVector(observation.PoseRootWorldPosition) &&
                FiniteVector(observation.AnimatedWorldPosition) &&
                FiniteVector(observation.AnimatedComponentPosition) &&
                FiniteVector(observation.PhysicalWorldPosition) && FiniteVector(frame.Pelvis.PhysicalComponent) &&
                FiniteVector(frame.Pelvis.FinalGoal) && float.IsFinite(frame.Goal.PelvisPositionWeight) &&
                frame.Goal.PelvisPositionWeight >= 0f && frame.Goal.PelvisPositionWeight <= 1f,
                frame, "physical observation input");
            if (!observation.PoseInputAvailable)
                RequirePelvis(observation.PoseRootWorldPosition.Equals(Vector3.zero) &&
                    observation.AnimatedWorldPosition.Equals(Vector3.zero) &&
                    observation.AnimatedComponentPosition.Equals(Vector3.zero), frame, "unavailable pose input");
            bool residualAvailable = PelvisPhysicalAvailable(frame) &&
                observation.PoseInputAvailable && frame.Goal.PelvisPositionWeight > 0f;
            float expectedResidual = residualAvailable
                ? Vector3.Distance(frame.Pelvis.PhysicalComponent,
                    observation.AnimatedComponentPosition + frame.Pelvis.FinalGoal * frame.Goal.PelvisPositionWeight)
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
                CharacterFootPelvisResponseSample response = current.Pelvis.Response, prior = previous.Pelvis.Response;
                if (!Continuous(previous, current) || !response.Evaluated || !response.HadPreviousState ||
                    previous.Identity.ProgramIdentity != current.Identity.ProgramIdentity || previous.Identity.ProfileRevision != current.Identity.ProfileRevision ||
                    previous.BodyCorrection.ResetSequence != current.BodyCorrection.ResetSequence)
                    continue;
                bool supportChanged = !current.Pelvis.HeightTarget.Available || !previous.Pelvis.HeightTarget.Available ||
                    previous.Pelvis.SupportSide != current.Pelvis.SupportSide ||
                    previous.PrimarySupport.LandingEventIdentity != current.PrimarySupport.LandingEventIdentity;
                RequirePelvis(prior.Evaluated && !prior.Completed &&
                    PelvisClose(response.PreviousTarget, prior.Target) && PelvisClose(response.PreviousOutput, prior.Output) &&
                    PelvisClose(response.PreviousVelocity, prior.Velocity) && response.SupportChanged == supportChanged &&
                    response.PreviousSlope == (previous.Pelvis.HeightTarget.Available ? previous.Pelvis.Slope : CharacterFootStrideSlope.Flat),
                    current, "committed spring carry");
            }
        }

        static void RequireLegReachFacts(FootFrame frame)
        {
            if (!frame.Solver.IkLegAvailable)
                return;
            double legLength = Vector3.Distance(
                                   frame.Solver.IkLegOriginalHip,
                                   frame.Solver.IkLegOriginalKnee) +
                               Vector3.Distance(
                                   frame.Solver.IkLegOriginalKnee,
                                   frame.Solver.IkLegOriginalAnkle);
            if (!double.IsFinite(legLength) || legLength <= TimeEpsilon)
            {
                throw new InvalidDataException(
                    "Foot Motion leg length facts are invalid.");
            }
            double originalLength = Vector3.Distance(
                frame.Solver.IkLegOriginalHip,
                frame.Solver.IkLegOriginalAnkle);
            double targetLength = Vector3.Distance(
                frame.Solver.IkLegOriginalHip,
                frame.Solver.IkLegTargetAnkle);
            double solvedLegLength = Vector3.Distance(
                                         frame.Solver.IkLegSolvedHip,
                                         frame.Solver.IkLegSolvedKnee) +
                                     Vector3.Distance(
                                         frame.Solver.IkLegSolvedKnee,
                                         frame.Solver.IkLegSolvedAnkle);
            double solvedLength = Vector3.Distance(
                frame.Solver.IkLegSolvedHip,
                frame.Solver.IkLegSolvedAnkle);
            bool consistent =
                float.IsFinite(frame.Solver.IkLegOriginalExtensionRatio) &&
                float.IsFinite(frame.Solver.IkLegTargetExtensionRatio) &&
                float.IsFinite(frame.Solver.IkLegSolvedExtensionRatio) &&
                float.IsFinite(frame.Solver.IkLegOriginalCompressionReserve) &&
                float.IsFinite(frame.Solver.IkLegTargetCompressionReserve) &&
                float.IsFinite(frame.Solver.IkLegSolvedCompressionReserve) &&
                Math.Abs(solvedLegLength - legLength) <=
                    PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegOriginalExtensionRatio -
                    originalLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegTargetExtensionRatio -
                    targetLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegSolvedExtensionRatio -
                    solvedLength / legLength) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegOriginalCompressionReserve -
                    Math.Max(0d, legLength - originalLength)) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegTargetCompressionReserve -
                    Math.Max(0d, legLength - targetLength)) <= PositionNoiseFloor &&
                Math.Abs(
                    frame.Solver.IkLegSolvedCompressionReserve -
                    Math.Max(0d, legLength - solvedLength)) <= PositionNoiseFloor;
            if (!consistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion leg extension and compression facts are inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
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
                (!frame.MotionCore.LandingReachEvaluated ||
                 frame.Resolved.LandingReachAvailable);
            if (!resolvedReachConsistent)
            {
                throw new InvalidDataException(
                    $"Foot Motion Landing Reach request and interval facts are inconsistent " +
                    $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
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

        static void RejectRetiredColumns(Dictionary<string, int> indices)
        {
            if (indices.Keys.Any(name => name.StartsWith("FootMotionSlidingResponse", StringComparison.Ordinal)))
                throw new InvalidDataException("Foot Motion samples contain retired response-history columns.");
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
            current.Identity.FrameSequence == previous.Identity.FrameSequence + 1 &&
            current.BodyCorrection.ResetSequence == previous.BodyCorrection.ResetSequence;

        static double DeltaSeconds(FootFrame frame) =>
            Math.Max(frame.Timing.DeltaSeconds, 0.000001f);

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
                    (frames[i - 1].MotionCore.ConstraintState != "Swing" ||
                     frames[i].MotionCore.ConstraintState != "Swing"))
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
            int frame = frames.Count > 0 ? frames[0].Identity.FrameSequence : 0;
            for (int i = 1; i < frames.Count; i++)
            {
                if (unanchoredOnly &&
                    (frames[i - 1].HasAnchor || frames[i].HasAnchor))
                {
                    continue;
                }
                if (swingOnly &&
                    (frames[i - 1].MotionCore.ConstraintState != "Swing" ||
                     frames[i].MotionCore.ConstraintState != "Swing"))
                {
                    continue;
                }
                double value = Vector3.Distance(
                    frames[i - 1].EffectiveCorrection,
                    frames[i].EffectiveCorrection);
                if (value <= maximum)
                    continue;
                maximum = value;
                frame = frames[i].Identity.FrameSequence;
            }
            return frame;
        }

        static int PeakDistanceFrame(IReadOnlyList<FootFrame> frames)
        {
            double maximum = -1d;
            int frame = frames.Count > 0 ? frames[0].Identity.FrameSequence : 0;
            for (int i = 0; i < frames.Count; i++)
            {
                double value = Vector3.Distance(frames[i].MotionCore.CorrectedSole, frames[i].MotionCore.Anchor);
                if (value <= maximum)
                    continue;
                maximum = value;
                frame = frames[i].Identity.FrameSequence;
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
                if (frames[i - 1].GroundPath.InputIdentity != frames[i].GroundPath.InputIdentity ||
                    frames[i - 1].GroundPath.NextSwingLandingEventIdentity != frames[i].GroundPath.NextSwingLandingEventIdentity ||
                    Vector3.Distance(frames[i - 1].GroundPath.NextSwingLanding, frames[i].GroundPath.NextSwingLanding) > PositionNoiseFloor ||
                    frames[i - 1].GroundPath.State != frames[i].GroundPath.State)
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
                string key = string.IsNullOrEmpty(frame.MotionCore.PenetrationAvailability)
                    ? "Unspecified"
                    : frame.MotionCore.PenetrationAvailability;
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

        internal sealed class FootFrame
        {
            internal CharacterFootResolvedSample Resolved;
            internal CharacterFootCurrentSupportSample CurrentSupport;
            internal CharacterFootStepPhaseSample SelectedPhase;
            internal CharacterFootEventSample OutputEvents;
            internal CharacterFootEventSample InputEvents;
            internal CharacterFootStepCandidateSample CurrentStep;
            internal CharacterFootStepCandidateSample IncomingStep;
            internal int GroundSurfaceObservedCount;
            internal readonly SortedDictionary<int, Vector3>
                GroundEnvelopeVertices =
                    new SortedDictionary<int, Vector3>();
            internal CharacterFootSupportTargetSample SelectedSupportTarget;
            internal CharacterFootContactSupportGapFrame ContactSupportGap;
            internal bool HasAnchor;
            internal bool PenetrationAvailable =>
                MotionCore.ContactPlaneAvailable &&
                (MotionCore.ConstraintState == "Landing" || MotionCore.ConstraintState == "Locked") &&
                MotionCore.PenetrationAvailability ==
                CharacterFootContactPlanePenetrationAvailability.Available.ToString();
            internal CharacterFootPelvisSample Pelvis;
            internal CharacterFootSolverSample Solver;
            internal CharacterFootResponseSample Response;
            internal CharacterFootGroundPathSample GroundPath;
            internal CharacterFootLandingObservationSample LandingObservation;
            internal CharacterFootFormalObservationSample FormalOutput;
            internal CharacterFootFormalInputSample FormalInput;
            internal CharacterFootBodyCorrectionSample BodyCorrection;
            internal CharacterFootRootHierarchySample RootHierarchy;
            internal CharacterFootPrimarySupportSample PrimarySupport;
            internal CharacterFootPredictionMotionSample PredictionMotion;
            internal CharacterFootTimingSample Timing;
            internal CharacterFootOutputStagesSample OutputStages;
            internal CharacterFootLifecycleSample Lifecycle;
            internal CharacterFootPathContinuitySample PathContinuity;
            internal CharacterFootMotionCoreSample MotionCore;
            internal CharacterFootIdentitySample Identity;
            internal CharacterFootRootLandingSample RootLanding;
            internal CharacterFootActionSample Action;
            internal CharacterFootGoalSample Goal;
            internal Vector3 EffectiveCorrection => MotionCore.CorrectedAnkle - MotionCore.OriginalAnkle;
        }

        static CharacterFootPelvisFrameObservation BuildPelvisFact(FootFrame frame, FootFrame previous) =>
            new CharacterFootPelvisFrameObservation
            {
                frame = frame.Identity.FrameSequence,
                completionIdentity = frame.Identity.CompletionIdentity.ToString(CultureInfo.InvariantCulture),
                strideState = frame.Pelvis.State,
                strideRejectReason = frame.Pelvis.RejectReason.ToString(),
                formalFootPlacementWeight = frame.Lifecycle.FormalFootPlacementWeight,
                primarySupportSide = frame.PrimarySupport.Side,
                primarySupportEventIdentity = frame.PrimarySupport.LandingEventIdentity.ToString(CultureInfo.InvariantCulture),
                observation = BuildPelvisObservationFact(frame),
                motion = BuildPelvisMotionFact(previous, frame),
                heightTarget = BuildPelvisHeightTargetFact(frame),
                posturePreference = BuildPelvisPostureFact(frame.Pelvis.Posture),
                reach = BuildPelvisReachFact(frame.Pelvis.Reach),
                response = BuildPelvisResponseFact(frame.Pelvis.Response)
            };

        static CharacterFootPelvisOutputObservation BuildPelvisObservationFact(FootFrame frame)
        {
            CharacterFootPelvisObservationSample observation = frame.Pelvis.Observation;
            bool physicalAvailable = PelvisPhysicalAvailable(frame);
            return new CharacterFootPelvisOutputObservation
            {
                poseInputAvailable = observation.PoseInputAvailable,
                poseRootWorldPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.PoseRootWorldPosition) : null,
                animatedWorldPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.AnimatedWorldPosition) : null,
                animatedComponentPosition = observation.PoseInputAvailable ? CharacterFootVectorFact.From(observation.AnimatedComponentPosition) : null,
                physicalWriteAvailable = physicalAvailable,
                physicalWriteCompletionIdentity = frame.Solver.PhysicalWriteCompletionIdentity.ToString(CultureInfo.InvariantCulture),
                physicalWorldPosition = physicalAvailable ? CharacterFootVectorFact.From(observation.PhysicalWorldPosition) : null,
                physicalComponentPosition = physicalAvailable ? CharacterFootVectorFact.From(frame.Pelvis.PhysicalComponent) : null,
                goalCorrectionComponent = CharacterFootVectorFact.From(frame.Pelvis.FinalGoal),
                positionWeight = frame.Goal.PelvisPositionWeight,
                weightedCorrectionComponent = CharacterFootVectorFact.From(frame.Pelvis.FinalGoal * frame.Goal.PelvisPositionWeight),
                goalResidualAvailable = observation.GoalResidualAvailable,
                expectedPhysicalComponentPosition = observation.GoalResidualAvailable
                    ? CharacterFootVectorFact.From(observation.AnimatedComponentPosition + frame.Pelvis.FinalGoal * frame.Goal.PelvisPositionWeight) : null,
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
                previousFrame = continuous ? (int?)previous.Identity.FrameSequence : null,
                presentationDeltaSeconds = frame.Timing.DeltaSeconds,
                physicalStepAvailable = physicalAvailable,
                physicalWorldDelta = physicalAvailable
                    ? CharacterFootVectorFact.From(frame.Pelvis.Observation.PhysicalWorldPosition - previous.Pelvis.Observation.PhysicalWorldPosition) : null,
                physicalComponentDelta = physicalAvailable
                    ? CharacterFootVectorFact.From(frame.Pelvis.PhysicalComponent - previous.Pelvis.PhysicalComponent) : null,
                weightedCorrectionComponentDelta = continuous
                    ? CharacterFootVectorFact.From(frame.Pelvis.FinalGoal * frame.Goal.PelvisPositionWeight - previous.Pelvis.FinalGoal * previous.Goal.PelvisPositionWeight) : null
            };
        }

        static CharacterFootPelvisPostureObservation BuildPelvisPostureFact(CharacterFootPelvisPostureSample posture) =>
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

        static CharacterFootPelvisLegReachObservation BuildPelvisLegFact(CharacterFootPelvisLegSample leg) =>
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

        static CharacterFootPelvisReachObservation BuildPelvisReachFact(CharacterFootPelvisReachSample reach) =>
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

        static CharacterFootPelvisResponseObservation BuildPelvisResponseFact(CharacterFootPelvisResponseSample response) =>
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

        static void RequirePelvisHeightTarget(FootFrame frame)
        {
            CharacterFootPelvisHeightTargetSample target = frame.Pelvis.HeightTarget;
            if (!target.Available)
            {
                if (!target.ComponentUp.Equals(Vector3.zero) ||
                    !target.LeftAnimatedSole.Equals(Vector3.zero) || !target.RightAnimatedSole.Equals(Vector3.zero) ||
                    !target.LeftTargetSole.Equals(Vector3.zero) || !target.RightTargetSole.Equals(Vector3.zero) ||
                    target.AnimatedMinimumAlongUp != 0f || target.TargetMinimumAlongUp != 0f || target.RequestedOffsetAlongUp != 0f)
                    throw new InvalidDataException(
                        $"Foot Motion unavailable Pelvis height target is not default Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
                return;
            }
            float animatedMinimum = Mathf.Min(Vector3.Dot(target.LeftAnimatedSole, target.ComponentUp),
                Vector3.Dot(target.RightAnimatedSole, target.ComponentUp));
            float targetMinimum = Mathf.Min(Vector3.Dot(target.LeftTargetSole, target.ComponentUp),
                Vector3.Dot(target.RightTargetSole, target.ComponentUp));
            if (!FiniteVector(target.ComponentUp) || Math.Abs(target.ComponentUp.sqrMagnitude - 1f) > RuntimeGeometryEpsilon ||
                !FiniteVector(target.LeftAnimatedSole) || !FiniteVector(target.RightAnimatedSole) ||
                !FiniteVector(target.LeftTargetSole) || !FiniteVector(target.RightTargetSole) ||
                !float.IsFinite(target.AnimatedMinimumAlongUp) || !float.IsFinite(target.TargetMinimumAlongUp) ||
                !float.IsFinite(target.RequestedOffsetAlongUp) || !float.IsFinite(animatedMinimum) ||
                !float.IsFinite(targetMinimum) ||
                Math.Abs(target.AnimatedMinimumAlongUp - animatedMinimum) > RuntimeGeometryEpsilon ||
                Math.Abs(target.TargetMinimumAlongUp - targetMinimum) > RuntimeGeometryEpsilon ||
                Math.Abs(target.RequestedOffsetAlongUp - (targetMinimum - animatedMinimum)) > RuntimeGeometryEpsilon)
                throw new InvalidDataException(
                    $"Foot Motion Pelvis height target is inconsistent Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side} " +
                    $"AnimatedMinimum={target.AnimatedMinimumAlongUp:R}/{animatedMinimum:R} " +
                    $"TargetMinimum={target.TargetMinimumAlongUp:R}/{targetMinimum:R} RequestedOffset={target.RequestedOffsetAlongUp:R}.");
        }

        static CharacterFootPelvisHeightTargetObservation BuildPelvisHeightTargetFact(FootFrame frame) =>
            new CharacterFootPelvisHeightTargetObservation
            {
                frame = frame.Identity.FrameSequence,
                completionIdentity = frame.Identity.CompletionIdentity.ToString(CultureInfo.InvariantCulture),
                strideState = frame.Pelvis.State,
                available = frame.Pelvis.HeightTarget.Available,
                componentUp = frame.Pelvis.HeightTarget.Available ? CharacterFootVectorFact.From(frame.Pelvis.HeightTarget.ComponentUp) : null,
                leftAnimatedSole = frame.Pelvis.HeightTarget.Available ? CharacterFootVectorFact.From(frame.Pelvis.HeightTarget.LeftAnimatedSole) : null,
                rightAnimatedSole = frame.Pelvis.HeightTarget.Available ? CharacterFootVectorFact.From(frame.Pelvis.HeightTarget.RightAnimatedSole) : null,
                leftTargetSole = frame.Pelvis.HeightTarget.Available ? CharacterFootVectorFact.From(frame.Pelvis.HeightTarget.LeftTargetSole) : null,
                rightTargetSole = frame.Pelvis.HeightTarget.Available ? CharacterFootVectorFact.From(frame.Pelvis.HeightTarget.RightTargetSole) : null,
                animatedMinimumAlongUp = frame.Pelvis.HeightTarget.Available ? (double?)frame.Pelvis.HeightTarget.AnimatedMinimumAlongUp : null,
                targetMinimumAlongUp = frame.Pelvis.HeightTarget.Available ? (double?)frame.Pelvis.HeightTarget.TargetMinimumAlongUp : null,
                requestedOffsetAlongUp = frame.Pelvis.HeightTarget.Available ? (double?)frame.Pelvis.HeightTarget.RequestedOffsetAlongUp : null
            };

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
                    frame = frame.Identity.FrameSequence,
                    side = frame.Identity.Side,
                    availability = "None",
                    classification = "LandingReachUnavailable",
                    candidateCompressionReserveMeters =
                        LandingReachCompressionReserveMeters,
                    finalIkLegAvailable = frame.Solver.IkLegAvailable,
                    componentUp = ScalarVector3Fact.From(frame.PathContinuity.ComponentUp),
                    originalHip = ScalarVector3Fact.From(
                        frame.Solver.IkLegOriginalHip),
                    originalKnee = ScalarVector3Fact.From(
                        frame.Solver.IkLegOriginalKnee),
                    originalAnkle = ScalarVector3Fact.From(
                        frame.Solver.IkLegOriginalAnkle),
                    targetAnkle = ScalarVector3Fact.From(
                        frame.Solver.IkLegTargetAnkle),
                    baselineHipBeforePelvisOutput =
                        ScalarVector3Fact.From(
                            frame.Solver.IkLegOriginalHip),
                    strideSpringOutputMeters = frame.Pelvis.Response.Output,
                    originalExtensionRatio = frame.Solver.IkLegOriginalExtensionRatio,
                    targetExtensionRatio = frame.Solver.IkLegTargetExtensionRatio,
                    solvedExtensionRatio = frame.Solver.IkLegSolvedExtensionRatio,
                    originalCompressionReserveMeters =
                        frame.Solver.IkLegOriginalCompressionReserve,
                    actualTargetCompressionReserveMeters =
                        frame.Solver.IkLegTargetCompressionReserve,
                    solvedCompressionReserveMeters =
                        frame.Solver.IkLegSolvedCompressionReserve,
                    runtimeReachEvaluated = frame.MotionCore.LandingReachEvaluated,
                    runtimeReachAvailable = frame.MotionCore.LandingReachAvailable,
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
                        frame.PrimarySupport.HasValue,
                    primarySupportSide = frame.PrimarySupport.Side,
                    primarySupportLandingEventIdentity =
                        frame.PrimarySupport.LandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    strideState = frame.Pelvis.State,
                    strideSupportSide = frame.Pelvis.SupportSide,
                    pelvisReachObservationEvaluated =
                        frame.Pelvis.Reach.IntersectionEvaluated,
                    pelvisReachObservationMinimumAlongUpMeters =
                        frame.Pelvis.Reach.IntersectionMinimumAlongUp,
                    pelvisReachObservationMaximumAlongUpMeters =
                        frame.Pelvis.Reach.IntersectionMaximumAlongUp,
                    correctionDirection = "Unavailable"
                };
                if (!frame.Solver.IkLegAvailable)
                {
                    result.availability = "FinalIkLegUnavailable";
                    return result;
                }
                if (frame.PathContinuity.ComponentUp.sqrMagnitude <=
                    TimeEpsilon * TimeEpsilon)
                {
                    result.availability = "ComponentUpUnavailable";
                    return result;
                }
                Vector3 up = frame.PathContinuity.ComponentUp.normalized;
                double upperLength = Vector3.Distance(
                    frame.Solver.IkLegOriginalHip,
                    frame.Solver.IkLegOriginalKnee);
                double lowerLength = Vector3.Distance(
                    frame.Solver.IkLegOriginalKnee,
                    frame.Solver.IkLegOriginalAnkle);
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
                    frame.Pelvis.FinalGoal,
                    up) * frame.Goal.PelvisPositionWeight;
                Vector3 baselineHip = frame.Solver.IkLegOriginalHip -
                    up * (float)appliedPelvisAlongUp;
                result.appliedPelvisGoalAlongUpMeters =
                    appliedPelvisAlongUp;
                result.baselineHipBeforePelvisOutput =
                    ScalarVector3Fact.From(baselineHip);
                Vector3 hipFromTarget =
                    baselineHip - frame.Solver.IkLegTargetAnkle;
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
                Available = previous ? frame.Lifecycle.PreviousContactAnchorAvailable :
                    frame.Lifecycle.CurrentContactAnchorAvailable,
                Event = previous ? frame.Lifecycle.PreviousContactAnchorEventIdentity :
                    frame.Lifecycle.CurrentContactAnchorEventIdentity,
                AcquiredFrame = previous ?
                    frame.Lifecycle.PreviousContactAnchorAcquiredFrameSequence :
                    frame.Lifecycle.CurrentContactAnchorAcquiredFrameSequence,
                AcquiredCompletion = previous ?
                    frame.Lifecycle.PreviousContactAnchorAcquiredCompletionIdentity :
                    frame.Lifecycle.CurrentContactAnchorAcquiredCompletionIdentity,
                WorldRevision = previous ?
                    frame.Lifecycle.PreviousContactAnchorWorldRevision :
                    frame.Lifecycle.CurrentContactAnchorWorldRevision,
                Surface = previous ? frame.Lifecycle.PreviousContactAnchorSurfaceIdentity :
                    frame.Lifecycle.CurrentContactAnchorSurfaceIdentity,
                Point = previous ? frame.Lifecycle.PreviousContactAnchorPoint :
                    frame.Lifecycle.CurrentContactAnchorPoint,
                Normal = previous ? frame.Lifecycle.PreviousContactAnchorNormal :
                    frame.Lifecycle.CurrentContactAnchorNormal
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
                          AcquiredFrame <= (ulong)frame.Identity.FrameSequence &&
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
                        $"Frame={frame.Identity.FrameSequence} Side={frame.Identity.Side}.");
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
                        frame.Identity.StepSelectionLastLandingEventIdentity,
                        frame.Identity.StepSelectionMaximumPredictionTimeSeconds);
                StepTimeCandidateFact incoming =
                    StepTimeCandidateFact.From(
                        frame.IncomingStep,
                        frame.Identity.StepSelectionLastLandingEventIdentity,
                        frame.Identity.StepSelectionMaximumPredictionTimeSeconds);
                double? currentDelta = frame.FormalInput.Available
                    ? Math.Abs(
                        frame.FormalInput.TimeToLandingSeconds -
                        frame.CurrentStep.TimeToLandingSeconds)
                    : null;
                double? incomingDelta = frame.FormalInput.Available
                    ? Math.Abs(
                        frame.FormalInput.TimeToLandingSeconds -
                        frame.IncomingStep.TimeToLandingSeconds)
                    : null;
                double? selectedOldTime =
                    frame.Identity.SelectedStepSource == "FormalNextLanding"
                        ? frame.CurrentStep.TimeToLandingSeconds
                        : null;
                double? selectedDelta = frame.FormalInput.Available &&
                                        selectedOldTime.HasValue
                    ? Math.Abs(frame.FormalInput.TimeToLandingSeconds - selectedOldTime.Value)
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
                                        previous.FormalInput.Available &&
                                        frame.FormalInput.Available &&
                                        previous.FormalInput.SourceIdentity ==
                                        frame.FormalInput.SourceIdentity;
                return new StepTimeCandidateSelectionFact
                {
                    frame = frame.Identity.FrameSequence,
                    completionIdentity = frame.Identity.CompletionIdentity,
                    side = frame.Identity.Side,
                    formalObservationAvailable =
                        frame.FormalInput.Available,
                    formalSourceIdentity = frame.FormalInput.SourceIdentity,
                    formalSourceCycle = frame.FormalInput.SourceCycle,
                    formalContributionContinuityIdentity =
                        frame.FormalInput.ContributionContinuityIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    formalCompletionIdentity =
                        frame.FormalInput.CompletionIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    formalNormalizedTime = frame.FormalInput.NormalizedTime,
                    formalTimeSeconds = frame.FormalInput.TimeToLandingSeconds,
                    maximumPredictionTimeSeconds =
                        frame.Identity.StepSelectionMaximumPredictionTimeSeconds,
                    lastLandingEventIdentity =
                        frame.Identity.StepSelectionLastLandingEventIdentity.ToString(
                            CultureInfo.InvariantCulture),
                    selectedSource = frame.Identity.SelectedStepSource,
                    selectedLandingEventIdentity =
                        frame.Identity.SelectedLandingEventIdentity.ToString(
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
                        frame.Identity.StepSelectionLastLandingEventIdentity,
                    normalizedTimeWrapped = sameFormalSource &&
                        frame.FormalInput.NormalizedTime + TimeEpsilon <
                        previous.FormalInput.NormalizedTime,
                    selectedSourceChanged = previous != null &&
                        frame.Identity.SelectedStepSource != previous.Identity.SelectedStepSource,
                    selectedLandingEventChanged = previous != null &&
                        frame.Identity.SelectedLandingEventIdentity !=
                        previous.Identity.SelectedLandingEventIdentity,
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
