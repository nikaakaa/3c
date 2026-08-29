using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
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
            CharacterFootPathStageDiagnosisProjection.Apply(
                revision,
                context.Events("PathRevisionOutputJump"));
            CharacterFootDiagnosisTarget handoff = BuildTarget(
                context,
                "swing-to-landing-output-jump",
                "Swing进入Landing当帧，Foot Placement相对原动画新增的最终可见输出是否跳变",
                "SwingToLandingOutputJump",
                "ContinuousSwingToLandingFramePair");
            CharacterFootDiagnosisTarget actualEnvelope =
                BuildActualFootEnvelopeCounterfactual(context);
            return context.Document(
                DiagnosticId,
                stable,
                revision,
                handoff,
                actualEnvelope);
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

        static CharacterFootDiagnosisTarget BuildTarget(
            CharacterFootDiagnosisContext context,
            string targetId,
            string description,
            string eventKind,
            string sampleUnit)
        {
            List<JObject> events = context.Events(eventKind);
            CharacterFootDiagnosisTarget target = context.Target(
                targetId,
                description,
                new[] { eventKind },
                new[] { "footPlacementOutputOffsetStepMeters>0.02" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "FootPlacementOutputOffsetStep") >
                         PrimaryThresholdMeters
                    ? new List<string>
                    {
                        "footPlacementOutputOffsetStepMeters>0.02"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "FootPlacementOutputOffsetStep"),
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
                "FootPlacementOutputOffsetStep",
                "Meters",
                events,
                PrimaryThresholdMeters,
                s_Thresholds);
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
                             "FootPlacementOutputOffsetStep") >
                         PrimaryThresholdMeters
                    ? new List<string>
                    {
                        "footPlacementOutputOffsetStepMeters>0.02"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "FootPlacementOutputOffsetStep"),
                24);
            target.representativeEventCount =
                target.representativeEvents.Count;
            return target;
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
        public string postTransitionSource;
        public string postTransitionTarget;
        public string postTransitionAnchorCommand;
        public CharacterFootVectorFact stateTargetCorrection;
        public string interpolationPolicy;
        public CharacterFootVectorFact interpolationOutputCorrection;
        public bool interpolationCompleted;
        public bool plantInterpolationEvaluated;
        public string plantTargetEventIdentity;
        public bool plantTargetVerified;
        public string plantTargetKind;
        public string plantLockResponse;
        public CharacterFootVectorFact plantDesiredPoint;
        public CharacterFootVectorFact plantFilteredPoint;
        public double plantPreviousBlendWeight;
        public double plantBlendWeight;
        public double plantTargetMaximumVerticalSpeed;
        public double plantTargetHeightBefore;
        public double plantTargetVerticalDelta;
        public double plantTargetAppliedVerticalDelta;
        public double plantTargetHeightAfter;
        public string plantTargetHeightEventIdentity;
        public string plantTargetHeightUpdateReason;
        public bool plantTargetVerticalClamped;
        public CharacterFootVectorFact plantPreviousMixedWorldTarget;
        public CharacterFootVectorFact plantMixedWorldTarget;
        public CharacterFootVectorFact plantPreviousOutputPoint;
        public CharacterFootVectorFact plantOutputPoint;
        public string plantResidualCaptureReason;
        public CharacterFootVectorFact plantWorldResidualBeforeCapture;
        public CharacterFootVectorFact plantWorldResidualAfterCapture;
        public CharacterFootVectorFact plantWorldResidualAfterDecay;
        public string plantVerticalContinuityOwner;
        public string plantCorrectionStageDisposition;
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
