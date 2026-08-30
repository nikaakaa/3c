using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    internal sealed class CharacterFootCorrectionResponseCadenceAnalysis
    {
        public string classification;
        public int firstFrame;
        public int previousFrame;
        public int frame;
        public string pathIdentity;
        public string previousPathRevisionReason;
        public string currentPathRevisionReason;
        public string previousObservationCacheState;
        public string previousObservationQueryPurpose;
        public string previousObservationRefreshMode;
        public string previousObservationQueryReason;
        public string currentObservationCacheState;
        public string currentObservationQueryPurpose;
        public string currentObservationRefreshMode;
        public string currentObservationQueryReason;
        public string firstLargeStepStage;
    }

    internal sealed class CharacterFootSwingPathJitterDiagnosis :
        ICharacterFootDiagnosis
    {
        const double PrimaryThresholdMeters = 0.02d;
        static readonly double[] s_Thresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };

        public string DiagnosticId => "swing-path-jitter";
        public string FileName => "swing-path-jitter.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            CharacterFootDiagnosisTarget stable = BuildTarget(
                context,
                "stable-swing-output-jump",
                "稳定Path的普通Swing中，Foot Placement相对原动画新增的最终可见输出是否逐帧跳变",
                "StableSwingOutputJump",
                "ContinuousAcceptedUnanchoredStableSwingFramePair");
            CharacterFootDiagnosisTarget revision = BuildTarget(
                context,
                "path-revision-output-jump",
                "Path语义修订当帧，Foot Placement相对原动画新增的最终可见输出是否跳变",
                "PathRevisionOutputJump",
                "ContinuousAcceptedUnanchoredPathRevisionSwingFramePair");
            CharacterFootDiagnosisTarget revisionEvidence = BuildTarget(
                context,
                "path-revision-amplification-evidence",
                "Path修订可见跳变的首个放大阶段证据是否齐全；不重复计质量分",
                "PathRevisionOutputJump",
                "ContinuousAcceptedUnanchoredPathRevisionSwingFramePair");
            revisionEvidence.scorePolicy = "Informational";
            CharacterFootPathStageDiagnosisProjection.Apply(
                revisionEvidence,
                context.Events("PathRevisionOutputJump"));
            CharacterFootDiagnosisTarget actualEnvelope =
                BuildActualFootEnvelopeCounterfactual(context);
            CharacterFootDiagnosisTarget correctionCadence =
                BuildCorrectionResponseCadence(context);
            return context.Document(
                DiagnosticId,
                stable,
                revision,
                revisionEvidence,
                actualEnvelope,
                correctionCadence);
        }

        static CharacterFootDiagnosisTarget BuildCorrectionResponseCadence(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events(
                "StableSwingCorrectionResponseCadence");
            CharacterFootDiagnosisTarget target = context.Target(
                "stable-swing-correction-response-cadence",
                "同Source、Cycle、Event与Ground Path的连续Swing三帧中，Correction输出是否在小于5毫米Hold与大于2厘米Advance之间切换",
                new[] { "StableSwingCorrectionResponseCadence" },
                new[]
                {
                    "previousFinalEffectiveCorrectionStep<0.005&&currentFinalEffectiveCorrectionStep>0.02",
                    "previousFinalEffectiveCorrectionStep>0.02&&currentFinalEffectiveCorrectionStep<0.005"
                },
                events,
                value => CharacterFootDiagnosisContext.Evidence(
                             value,
                             "holdToAdvance")
                    ? new List<string>
                    {
                        "previousFinalEffectiveCorrectionStep<0.005&&currentFinalEffectiveCorrectionStep>0.02"
                    }
                    : CharacterFootDiagnosisContext.Evidence(
                        value,
                        "advanceToHold")
                        ? new List<string>
                        {
                            "previousFinalEffectiveCorrectionStep>0.02&&currentFinalEffectiveCorrectionStep<0.005"
                        }
                        : new List<string>(),
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "PreviousFinalEffectiveCorrectionStep"),
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "CurrentFinalEffectiveCorrectionStep")),
                "HoldMaximumMeters",
                "AdvanceMinimumMeters",
                "PreviousDesiredResponseDelta",
                "CurrentDesiredResponseDelta",
                "PreviousCorrectionResponsePrevious",
                "PreviousCorrectionResponseCurrent",
                "PreviousCorrectionResponseAppliedDelta",
                "PreviousCorrectionResponseSelectedSpeed",
                "CurrentCorrectionResponsePrevious",
                "CurrentCorrectionResponseCurrent",
                "CurrentCorrectionResponseAppliedDelta",
                "CurrentCorrectionResponseSelectedSpeed",
                "PreviousResponseOutputStep",
                "CurrentResponseOutputStep",
                "PreviousFinalEffectiveCorrectionStep",
                "CurrentFinalEffectiveCorrectionStep",
                "PreviousFormalFootHeightDelta",
                "CurrentFormalFootHeightDelta",
                "PreviousEnvelopeSampleStep",
                "CurrentEnvelopeSampleStep",
                "PreviousEnvelopeSampleAlongUpDelta",
                "CurrentEnvelopeSampleAlongUpDelta",
                "PreviousOriginalSoleStep",
                "CurrentOriginalSoleStep",
                "PreviousEnvelopeDirectionContribution",
                "CurrentEnvelopeDirectionContribution",
                "PreviousOriginalSoleDirectionContribution",
                "CurrentOriginalSoleDirectionContribution");
            target.scorePolicy = "Informational";
            target.occurrence = context.Occurrence(
                "ContinuousAcceptedSameSourceCycleEventPathSwingFrameTriple",
                "CurrentFinalEffectiveCorrectionStep",
                "Meters",
                events,
                PrimaryThresholdMeters,
                s_Thresholds);
            target.categoricalMeasurements = new SortedDictionary<
                string,
                List<CharacterFootDiagnosisCategoryCount>>(
                StringComparer.Ordinal)
            {
                ["CadenceTransition"] = CategoryCounts(
                    events,
                    CorrectionCadenceTransition),
                ["FirstLargeStepStage"] = CategoryCounts(
                    events,
                    value => value["correctionResponseCadence"]?
                        ["firstLargeStepStage"]?.Value<string>() ??
                        "Unavailable"),
                ["ObservationClassification"] = CategoryCounts(
                    events,
                    CorrectionCadenceObservation)
            };
            return target;
        }

        static CharacterFootDiagnosisTarget
            BuildActualFootEnvelopeCounterfactual(
                CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events(
                "ActualFootEnvelopeCounterfactual");
            List<JObject> unique = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "uniqueInCorridor"));
            CharacterFootDiagnosisTarget target = context.Target(
                "swing-actual-foot-envelope-counterfactual",
                "同一Ground Path普通Swing中，实际脚水平位置的唯一Envelope候选相对Builder目标需要提前抬升多少",
                new[] { "ActualFootEnvelopeCounterfactual" },
                new[]
                {
                    "uniqueInCorridor=true&&actualProgressEnvelopeAdvanceAboveBuilderTarget>0.02"
                },
                events,
                value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "uniqueInCorridor") &&
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "ActualProgressEnvelopeAdvanceAboveBuilderTarget") >
                    PrimaryThresholdMeters
                        ? new List<string>
                        {
                            "uniqueInCorridor=true&&actualProgressEnvelopeAdvanceAboveBuilderTarget>0.02"
                        }
                        : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "ActualProgressEnvelopeAdvanceAboveBuilderTarget"),
                "ActualProgressEnvelopeAdvanceAboveBuilderTarget",
                "ActualProgressEnvelopeMinimumCorrection",
                "BuilderSwingTargetAlongUp",
                "ActualFootCrossTrackDistance",
                "ActualEnvelopeCandidateCount",
                "ActualEnvelopeHeightSpan",
                "GroundEnvelopeHardClamp",
                "FootPlacementOutputOffsetStep",
                "PresentationDeltaSeconds");
            target.scorePolicy = "Informational";
            target.occurrence = context.Occurrence(
                "ContinuousAcceptedUnanchoredSameGroundPathSwingFramePairWithUniqueActualEnvelope",
                "ActualProgressEnvelopeAdvanceAboveBuilderTarget",
                "Meters",
                unique,
                PrimaryThresholdMeters,
                s_Thresholds);
            target.categoricalMeasurements = new SortedDictionary<
                string,
                List<CharacterFootDiagnosisCategoryCount>>(
                StringComparer.Ordinal)
            {
                ["CounterfactualState"] = CategoryCounts(
                    events,
                    CounterfactualState),
                ["GroundEnvelopeOwner"] = CategoryCounts(
                    events,
                    value => CharacterFootDiagnosisContext.Evidence(
                        value,
                        "groundEnvelopeOwner")
                        ? "Consumed"
                        : "NotConsumed")
            };
            return target;
        }

        internal static CharacterFootDiagnosisTarget BuildTarget(
            CharacterFootDiagnosisContext context,
            string targetId,
            string description,
            string eventKind,
            string sampleUnit,
            string metricName = "FootPlacementOutputOffsetStep")
        {
            List<JObject> events = context.Events(eventKind);
            CharacterFootDiagnosisTarget target = context.Target(
                targetId,
                description,
                new[] { eventKind },
                new[] { metricName + ">0.02" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             metricName) >
                         PrimaryThresholdMeters
                    ? new List<string>
                    {
                        metricName + ">0.02"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    metricName),
                metricName,
                "FootPlacementOutputOffsetStep",
                "FootPlacementOutputOffsetSpeed",
                "AnkleOutputOffsetStep",
                "HeelOutputOffsetStep",
                "ToeOutputOffsetStep",
                "AnkleOutputOffsetSpeed",
                "HeelOutputOffsetSpeed",
                "ToeOutputOffsetSpeed",
                "EndpointDelta",
                "LandingPointDelta",
                "TargetDelta",
                "PathRevisionDelta",
                "PhaseAdvanceDelta",
                "ObservedSwingTargetDelta",
                "PresentationDeltaSeconds",
                "BodyTickSpan");
            target.occurrence = context.Occurrence(
                sampleUnit,
                metricName,
                "Meters",
                events,
                PrimaryThresholdMeters,
                s_Thresholds);
            target.scorePolicy = "Health";
            target.supplementalOccurrences = new List<
                CharacterFootDiagnosisOccurrenceProfile>
            {
                context.Occurrence(
                    sampleUnit,
                    "AnkleOutputOffsetStep",
                    "Meters",
                    events,
                    PrimaryThresholdMeters,
                    s_Thresholds),
                context.Occurrence(
                    sampleUnit,
                    "HeelOutputOffsetStep",
                    "Meters",
                    events,
                    PrimaryThresholdMeters,
                    s_Thresholds),
                context.Occurrence(
                    sampleUnit,
                    "ToeOutputOffsetStep",
                    "Meters",
                    events,
                    PrimaryThresholdMeters,
                    s_Thresholds)
            };
            target.measurements[
                    "FootPlacementOutputOffsetAcceleration"] =
                Distribution(
                    events,
                    "FootPlacementOutputOffsetAcceleration",
                    "accelerationAvailable");
            target.measurements[
                    "FootPlacementOutputOffsetJerk"] =
                Distribution(
                    events,
                    "FootPlacementOutputOffsetJerk",
                    "jerkAvailable");
            target.categoricalMeasurements = new SortedDictionary<
                string,
                List<CharacterFootDiagnosisCategoryCount>>(
                StringComparer.Ordinal)
            {
                ["PrimaryProbe"] = CategoryCounts(
                    events,
                    value => value["visibleOutputJump"]?
                                 ["primaryProbe"]?.Value<string>() ??
                             "Unavailable"),
                ["SafetyFloorOwner"] = CategoryCounts(
                    events,
                    value => value["visibleOutputJump"]?
                                 ["safetyFloorOwner"]?.Value<string>() ??
                             "Unavailable"),
                ["PathRevisionReason"] = CategoryCounts(
                    events,
                    value => value["visibleOutputJump"]?
                                 ["pathRevisionReason"]?.Value<string>() ??
                             "None"),
                ["PresentationSamplingClassification"] = CategoryCounts(
                    events,
                    value => value["visibleOutputJump"]?
                                 ["presentationSamplingClassification"]?
                                 .Value<string>() ?? "Unavailable")
            };
            target.representativeEvents = context.Representatives(
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             metricName) >
                         PrimaryThresholdMeters
                    ? new List<string>
                    {
                        metricName + ">0.02"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    metricName),
                24);
            target.representativeEventCount =
                target.representativeEvents.Count;
            return target;
        }

        static string CorrectionCadenceTransition(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "holdToAdvance"))
            {
                return "HoldToAdvance";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "advanceToHold"))
            {
                return "AdvanceToHold";
            }
            return "ContinuousCadence";
        }

        static string CorrectionCadenceObservation(JObject value)
        {
            JObject detail = value["correctionResponseCadence"] as JObject;
            if (detail == null)
                return "Unavailable";
            bool previousQuery = CharacterFootDiagnosisContext.Evidence(
                value,
                "previousObservationQueryExecuted");
            bool currentQuery = CharacterFootDiagnosisContext.Evidence(
                value,
                "currentObservationQueryExecuted");
            if (previousQuery || currentQuery)
                return "QueryExecuted";
            string previous = detail["previousObservationCacheState"]?
                .Value<string>() ?? "Unavailable";
            string current = detail["currentObservationCacheState"]?
                .Value<string>() ?? "Unavailable";
            return previous == "Reused" && current == "Reused"
                ? "Reused"
                : previous + "To" + current;
        }

        static CharacterFootDiagnosisDistribution Distribution(
            List<JObject> events,
            string metric,
            string availabilityEvidence) =>
            CharacterFootDiagnosisDistribution.Create(
                events
                    .Where(value =>
                        CharacterFootDiagnosisContext.Evidence(
                            value,
                            availabilityEvidence))
                    .Select(value =>
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            metric))
                    .ToList());

        static List<CharacterFootDiagnosisCategoryCount> CategoryCounts(
            List<JObject> events,
            Func<JObject, string> selector) =>
            events
                .GroupBy(selector, StringComparer.Ordinal)
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new CharacterFootDiagnosisCategoryCount
                {
                    value = value.Key,
                    count = value.Count()
                })
                .ToList();

        static string CounterfactualState(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "uniqueInCorridor"))
            {
                return "UniqueInCorridor";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "ambiguousInCorridor"))
            {
                return "AmbiguousInCorridor";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "outsideGroundPathCorridor"))
            {
                return "OutsideGroundPathCorridor";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "noIntersection"))
            {
                return "NoIntersection";
            }
            return "Unavailable";
        }
    }

    [Serializable]
    internal sealed class CharacterFootVisibleOutputProbeFact
    {
        public CharacterFootVectorFact previousAnimatedSource;
        public CharacterFootVectorFact animatedSource;
        public CharacterFootVectorFact previousFinalPhysical;
        public CharacterFootVectorFact finalPhysical;
        public CharacterFootVectorFact previousOutputOffset;
        public CharacterFootVectorFact outputOffset;
        public CharacterFootVectorFact outputOffsetStep;
        public double outputOffsetStepMeters;
        public CharacterFootVectorFact outputOffsetVelocity;
        public double outputOffsetSpeedMetersPerSecond;
        public bool accelerationAvailable;
        public CharacterFootVectorFact outputOffsetAcceleration;
        public double outputOffsetAccelerationMetersPerSecondSquared;
        public bool jerkAvailable;
        public CharacterFootVectorFact outputOffsetJerk;
        public double outputOffsetJerkMetersPerSecondCubed;
    }

    [Serializable]
    internal sealed class CharacterFootVisibleOutputJumpAnalysis
    {
        public string category;
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string sourceIdentity;
        public int sourceCycle;
        public string previousConstraintState;
        public string constraintStateBefore;
        public string constraintState;
        public string preTransitionReason;
        public string preTransitionSource;
        public string preTransitionTarget;
        public string preTransitionAnchorCommand;
        public string postTransitionReason;
        public bool postTransitionEvaluated;
        public string postTransitionSource;
        public string postTransitionTarget;
        public string postTransitionAnchorCommand;
        public CharacterFootVectorFact stateTargetCorrection;
        public string interpolationPolicy;
        public CharacterFootVectorFact interpolationOutputCorrection;
        public bool interpolationCompleted;
        public bool plantInterpolationEvaluated;
        public CharacterFootVectorFact targetHeightComponentUp;
        public string plantTargetEventIdentity;
        public bool plantTargetVerified;
        public string plantTargetKind;
        public string plantLockResponse;
        public bool plantLockWeightCompleted;
        public CharacterFootVectorFact plantDesiredPoint;
        public CharacterFootVectorFact plantFilteredPoint;
        public string swingTargetHeightAdoptionMode;
        public string plantTargetHeightAdoptionMode;
        public double plantTargetMaximumVerticalSpeed;
        public double plantTargetHeightBefore;
        public double plantTargetHeightTarget;
        public double plantTargetVerticalDelta;
        public double plantTargetAppliedVerticalDelta;
        public double plantTargetHeightAfter;
        public string plantTargetHeightEventIdentity;
        public string plantTargetHeightUpdateReason;
        public bool plantTargetVerticalClamped;
        public CharacterFootVectorFact plantPreviousSelectedWorldTarget;
        public CharacterFootVectorFact plantSelectedWorldTarget;
        public bool previousResponseOutputAvailable;
        public CharacterFootVectorFact previousResponseOutputPoint;
        public CharacterFootVectorFact desiredOutputPoint;
        public CharacterFootVectorFact responseOutputPoint;
        public string plantResidualCaptureReason;
        public CharacterFootVectorFact plantWorldResidualBeforeCapture;
        public CharacterFootVectorFact plantWorldResidualCapturedBeforeDecay;
        public bool plantWorldResidualDecayApplied;
        public double plantWorldResidualBaseHalfLifeSeconds;
        public bool plantWorldResidualDeadlineHalfLifeAvailable;
        public double plantWorldResidualDeadlineHalfLifeSeconds;
        public double plantWorldResidualAppliedHalfLifeSeconds;
        public CharacterFootVectorFact plantWorldResidualAfterDecay;
        public double plantWorldResidualCompletionTolerance;
        public bool plantWorldResidualClearedAtCompletionTolerance;
        public bool correctionResponseEvaluated;
        public CharacterFootResponseDomainFact responseDomain;
        public bool correctionResponseInitializedBefore;
        public bool correctionResponseInitializedThisFrame;
        public string correctionResponseInitializationReason;
        public double? correctionResponseDesired;
        public CharacterFootVectorFact correctionResponseRequestedDirection;
        public CharacterFootVectorFact correctionResponsePreviousDirection;
        public bool correctionResponseDirectionLimited;
        public double correctionResponseMaximumDirectionChangeDegrees;
        public double correctionResponseAppliedDirectionChangeDegrees;
        public bool correctionResponseVisibleOutputTransferred;
        public double? correctionResponseBeforeRebase;
        public double? correctionResponsePrevious;
        public double? correctionResponseCurrent;
        public CharacterFootVectorFact correctionResponseDirection;
        public string correctionResponseDeltaDirection;
        public double correctionResponseSelectedSpeed;
        public double? correctionResponseAppliedDelta;
        public string plantVerticalContinuityOwners;
        public CharacterFootVectorFact plantEffectiveCorrectionBefore;
        public CharacterFootVectorFact plantEffectiveCorrectionAfter;
        public double plantOutputDistance;
        public double plantPenetrationDepth;
        public double presentationDeltaSeconds;
        public ulong bodyTickSpan;
        public string presentationSamplingClassification;
        public bool lowPresentationCadence;
        public bool outputSpeedAnomaly;
        public string primaryProbe;
        public double footPlacementOutputOffsetStepMeters;
        public double footPlacementOutputOffsetSpeedMetersPerSecond;
        public bool accelerationAvailable;
        public double footPlacementOutputOffsetAccelerationMetersPerSecondSquared;
        public bool jerkAvailable;
        public double footPlacementOutputOffsetJerkMetersPerSecondCubed;
        public CharacterFootVisibleOutputProbeFact ankle;
        public CharacterFootVisibleOutputProbeFact heel;
        public CharacterFootVisibleOutputProbeFact toe;
        public string safetyFloorOwner;
        public int safetyFloorOwnerSurfaceIdentity;
        public string safetyFloorOwnerPathIdentity;
        public string pathRevisionReason;
        public double pathNoiseFloorMeters;
        public double endpointDeltaMeters;
        public double landingPointDeltaMeters;
        public double targetDeltaMeters;
        public bool pathAvailabilityChanged;
        public bool landingEventChanged;
        public bool endpointTreadChanged;
        public bool counterfactualPathRevision;
        public bool pathResidualRebuilt;
        public CharacterFootSwingTargetCounterfactual
            swingTargetCounterfactual;
    }
}
