using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal enum CharacterFootContactSupportGapAvailability
    {
        NotRequested,
        ReleasingNotContactHolding,
        OwnershipUnavailable,
        PlacementWeightZero,
        PhysicalPoseUnavailable,
        SameEventAnchorUnavailable,
        ContactHoldingStateUnavailable,
        Available
    }

    [Serializable]
    internal sealed class CharacterFootContactSupportGapFrame
    {
        public int frame;
        public string side;
        public string availability;
        public bool requested;
        public bool applicable;
        public string constraintState;
        public string requestEventIdentity;
        public string anchorEventIdentity;
        public int anchorSurfaceIdentity;
        public string anchorWorldRevision;
        public string anchorAcquiredFrame;
        public string anchorAcquiredCompletion;
        public CharacterFootVectorFact anchorPoint;
        public CharacterFootVectorFact anchorNormal;
        public CharacterFootVectorFact physicalHeel;
        public CharacterFootVectorFact physicalToe;
        public double formalFootPlacementWeight;
        public double lockWeight;
        public double deltaSeconds;
        public bool currentSupportAvailable;
        public string currentSupportRejectReason;
        public int currentSupportSurfaceIdentity;
        public bool landingReachAvailable;
        public bool landingReachGoalClamped;
        public double? heelClearanceMeters;
        public double? toeClearanceMeters;
        public double? soleClearanceMeters;
        public double? wholeFootGapMeters;
        public double? inPlaneAnchorDistanceMeters;
        public double? previousGapDeltaMeters;
        public string gapMotion;
    }

    [Serializable]
    internal sealed class CharacterFootContactSupportGapSequence
    {
        public string referenceKind = "VerifiedContactAnchorPlane";
        public string classification;
        public List<CharacterFootContactSupportGapFrame> frames;
    }

    [Serializable]
    internal sealed class CharacterFootContactSupportGapCoverage
    {
        public string measurementDomain = "VerifiedContactAnchorPlane";
        public string durationTimeDomain = "PresentationDeltaSeconds";
        public bool provesFiniteSurfaceSupportUnderPhysicalFoot = false;
        public double primaryGapThresholdMeters;
        public double persistentMinimumSeconds;
        public int requestedFrameCount;
        public int applicableFrameCount;
        public int notApplicableFrameCount;
        public int availableFrameCount;
        public int unavailableFrameCount;
        public double? availableFrameRate;
        public int intervalCount;
        public List<CharacterFootDiagnosisCategoryCount> availabilityCounts;
        public List<CharacterFootContactSupportGapFrame> unavailableExamples;
    }

    [Serializable]
    internal sealed class CharacterFootContactAcquisitionContinuityAnalysis
    {
        public string acquisitionReason;
        public string lineageClassification;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public string previousEventIdentity;
        public string eventIdentity;
        public CharacterFootVectorFact anchor;
        public CharacterFootVectorFact previousOriginalSole;
        public CharacterFootVectorFact originalSole;
        public CharacterFootVectorFact previousVisibleOutput;
        public CharacterFootVectorFact previousResponseOutput;
        public CharacterFootVectorFact capturedBeforeDecay;
        public CharacterFootVectorFact afterDecay;
        public CharacterFootVectorFact desiredOutput;
        public CharacterFootVectorFact responseOutput;
        public CharacterFootVectorFact finalOutput;
        public string plantResidualCaptureReason;
        public string correctionResponseInitializationReason;
    }

    [Serializable]
    internal sealed class CharacterFootLockWeightCompletionAnalysis
    {
        public string outcome;
        public string eventIdentity;
        public int firstFrame;
        public int lastFrame;
        public int? firstFullWeightFrame;
        public int? landingCompletedFrame;
        public string sourceIdentity;
        public int sourceCycle;
        public string completionState;
        public string completionPlantTargetKind;
    }

    internal sealed class CharacterFootLandingStateConsistencyDiagnosis : ICharacterFootDiagnosis
    {
        const double ExitJumpMeters = 0.01d;
        const double PrimaryOutputJumpMeters = 0.02d;
        static readonly double[] s_OutputThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };
        public string DiagnosticId => "landing-state-consistency";
        public string FileName => "landing-state-consistency.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> boundaries = context.Events("LandingStateBoundary");
            List<JObject> spans = context.Events("LandingStateSpan");
            List<JObject> releases = context.Events("Release");
            List<JObject> handoffs = context.Events(
                "SwingToLandingFloorHandoff");
            List<JObject> plantInterpolation = context.Events(
                "PlantInterpolationOutputJump");
            List<JObject> contactAcquisitions = context.Events(
                "ContactAcquisitionContinuity");
            List<JObject> lockWeightEvents = context.Events(
                "LockWeightCompletionEvent");
            List<JObject> approachProgressOwnership = context.Events(
                "ApproachProgressOwnership");
            List<JObject> actionHardOwnership = context.Events(
                "ActionHardOwnership");
            List<JObject> contactTransitions = context.Events(
                "ContactTransitionContext");
            List<JObject> formalGoalWeights = context.Events(
                "FormalGoalWeightPolicy");
            List<JObject> reentryGeometry = context.Events(
                "ContactReentryOutputGeometry");
            CharacterFootDiagnosisTarget releaseTarget = context.Target(
                "release-flyback",
                "Releasing阶段是否出现Correction突跳后反向回拉",
                new[] { "Release" },
                new[]
                {
                    "velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01"
                },
                releases,
                value =>
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "velocityDirectionReversalCount") > 0d &&
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctionExcursionMeters") > ExitJumpMeters
                        ? new List<string>
                        {
                            "velocityDirectionReversalCount>0&&correctionExcursionMeters>0.01"
                        }
                        : new List<string>(),
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctionExcursionMeters") / ExitJumpMeters,
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "velocityDirectionReversalCount")),
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "velocityDirectionReversalCount");
            CharacterFootDiagnosisTarget handoffTarget = context.Target(
                "swing-to-landing-floor-handoff",
                "Swing进入Landing时，上一帧Ground Envelope补偿、Residual截止与Floor所有权切换是否造成正式Sole世界输出额外跳变",
                new[] { "SwingToLandingFloorHandoff" },
                new[] { "entryStateAdditionalOutputStepMeters>0.02" },
                handoffs,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "entryStateAdditionalOutputStepMeters") >
                         PrimaryOutputJumpMeters
                    ? new List<string>
                    {
                        "entryStateAdditionalOutputStepMeters>0.02"
                    }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "entryStateAdditionalOutputStepMeters"),
                "entryStateAdditionalOutputStepMeters",
                "entryCorrectedSoleStepMeters",
                "entryAnimatedSoleStepMeters",
                "entryOutputBlendParameter",
                "entryCorrectionReexpressionStepMeters",
                "entryCorrectionReexpressionAlongUpMeters",
                "entryPhysicalAnkleStepMeters",
                "entryPhysicalSoleStepMeters",
                "previousSafetyFloorClampMeters",
                "previousSafetyFloorCompensationMeters",
                "previousResidualAfterDecayMeters",
                "swingResidualToleranceMeters",
                "stepHeightMeters",
                "previousFormalFootHeightMeters",
                "formalFootHeightMeters",
                "previousProgress",
                "progress",
                "previousTimeToLandingSeconds",
                "timeToLandingSeconds");
            handoffTarget.occurrence = context.Occurrence(
                "ContinuousSwingToLandingFramePair",
                "entryStateAdditionalOutputStepMeters",
                "Meters",
                handoffs,
                PrimaryOutputJumpMeters,
                s_OutputThresholds);
            handoffTarget.supplementalOccurrences = new List<
                CharacterFootDiagnosisOccurrenceProfile>
            {
                context.Occurrence(
                    "ContinuousSwingToLandingFramePair",
                    "entryPhysicalAnkleStepMeters",
                    "Meters",
                    handoffs,
                    PrimaryOutputJumpMeters,
                    s_OutputThresholds),
                context.Occurrence(
                    "ContinuousSwingToLandingFramePair",
                    "entryPhysicalSoleStepMeters",
                    "Meters",
                    handoffs,
                    PrimaryOutputJumpMeters,
                    s_OutputThresholds)
            };
            handoffTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["HandoffEvidence"] = CategoryCounts(
                        handoffs,
                        HandoffEvidence)
                };
            CharacterFootDiagnosisTarget plantTarget = context.Target(
                "plant-interpolation-output-jump",
                "Plant目标高度、世界Residual或直接目标接管是否伴随Foot Placement最终可见输出跳变",
                new[] { "PlantInterpolationOutputJump" },
                new[] { "footPlacementOutputOffsetStepMeters>0.02" },
                plantInterpolation,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "FootPlacementOutputOffsetStep") >
                         PrimaryOutputJumpMeters
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
                "PlantSelectedWorldTargetStep",
                "DesiredOutputPointStep",
                "ResponseOutputPointStep",
                "PlantWorldResidualCaptureDelta",
                "PlantWorldResidualCaptureContinuityError",
                "PlantWorldResidualDecayStep",
                "PlantWorldResidualAfterDecay",
                "PlantWorldResidualAppliedHalfLifeSeconds",
                "CorrectionResponseDesired",
                "CorrectionResponsePrevious",
                "CorrectionResponseCurrent",
                "CorrectionResponseSelectedSpeed",
                "CorrectionResponseAppliedDelta",
                "CorrectionResponseRequestedDirectionChangeDegrees",
                "CorrectionResponseMaximumDirectionChangeDegrees",
                "CorrectionResponseAppliedDirectionChangeDegrees",
                "PlantEffectiveCorrectionStep",
                "PlantTargetAppliedVerticalDelta",
                "PlantOutputDistance",
                "PlantPenetrationDepth",
                "PresentationDeltaSeconds",
                "BodyTickSpan");
            plantTarget.occurrence = context.Occurrence(
                "ContinuousPlantInterpolationFramePair",
                "FootPlacementOutputOffsetStep",
                "Meters",
                plantInterpolation,
                PrimaryOutputJumpMeters,
                s_OutputThresholds);
            plantTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["PlantDriver"] = CategoryCounts(
                        plantInterpolation,
                        PlantDriver)
                };
            CharacterFootDiagnosisTarget contactAcquisitionTarget =
                context.Target(
                    "contact-acquisition-continuity",
                    "非Idle正式接触建锚首帧，上一可见输出经World Residual与Correction Response接管后是否产生超过2厘米的世界输出步长",
                    new[] { "ContactAcquisitionContinuity" },
                    new[]
                    {
                        "previousVisibleToFinalOutputStepMeters>0.02"
                    },
                    contactAcquisitions,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "PreviousVisibleToFinalOutputStepMeters") >
                             PrimaryOutputJumpMeters
                        ? new List<string>
                        {
                            "previousVisibleToFinalOutputStepMeters>0.02"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "PreviousVisibleToFinalOutputStepMeters"),
                    "AnimationBaselineStepMeters",
                    "AnimationBaselineHorizontalStepMeters",
                    "AnimationBaselineAlongUpStepMeters",
                    "OriginalSoleToAnchorMeters",
                    "OriginalSoleToAnchorHorizontalMeters",
                    "OriginalSoleToAnchorAlongUpMeters",
                    "PreviousVisibleOutputToAnchorMeters",
                    "PreviousVisibleOutputToAnchorHorizontalMeters",
                    "PreviousVisibleOutputToAnchorAlongUpMeters",
                    "PreviousResponseOutputToAnchorMeters",
                    "PreviousResponseOutputToAnchorHorizontalMeters",
                    "PreviousResponseOutputToAnchorAlongUpMeters",
                    "CapturedResidualMeters",
                    "ResidualAfterDecayMeters",
                    "ResidualDecayStepMeters",
                    "ResidualCaptureContinuityErrorMeters",
                    "DesiredToResponseMeters",
                    "DesiredToResponseHorizontalMeters",
                    "DesiredToResponseAlongUpMeters",
                    "PreviousVisibleToFinalOutputStepMeters",
                    "PreviousVisibleToFinalOutputHorizontalStepMeters",
                    "PreviousVisibleToFinalOutputAlongUpStepMeters",
                    "ResponseOutputToAnchorMeters",
                    "ResponseOutputToAnchorHorizontalMeters",
                    "ResponseOutputToAnchorAlongUpMeters",
                    "FinalOutputToAnchorMeters",
                    "FinalOutputToAnchorHorizontalMeters",
                    "FinalOutputToAnchorAlongUpMeters",
                    "AnchorToSelectedTargetErrorMeters",
                    "CorrectionResponseDesired",
                    "CorrectionResponsePrevious",
                    "CorrectionResponseCurrent",
                    "CorrectionResponseAppliedDelta");
            contactAcquisitionTarget.occurrence = context.Occurrence(
                "NonIdleFormalContactAcquisitionFramePair",
                "PreviousVisibleToFinalOutputStepMeters",
                "Meters",
                contactAcquisitions,
                PrimaryOutputJumpMeters,
                s_OutputThresholds);
            contactAcquisitionTarget.supplementalOccurrences = new List<
                CharacterFootDiagnosisOccurrenceProfile>
            {
                context.Occurrence(
                    "NonIdleFormalContactAcquisitionFramePair",
                    "PreviousVisibleOutputToAnchorHorizontalMeters",
                    "Meters",
                    contactAcquisitions,
                    PrimaryOutputJumpMeters,
                    s_OutputThresholds),
                context.Occurrence(
                    "NonIdleFormalContactAcquisitionFramePair",
                    "FinalOutputToAnchorHorizontalMeters",
                    "Meters",
                    contactAcquisitions,
                    PrimaryOutputJumpMeters,
                    s_OutputThresholds)
            };
            contactAcquisitionTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["AcquisitionReason"] = CategoryCounts(
                        contactAcquisitions,
                        ContactAcquisitionReason),
                    ["SourceContributionLineage"] = CategoryCounts(
                        contactAcquisitions,
                        ContactAcquisitionLineage),
                    ["ContinuityEvidence"] = CategoryCounts(
                        contactAcquisitions,
                        ContactAcquisitionEvidence)
                };
            CharacterFootDiagnosisTarget lockWeightTarget = context.Target(
                "lock-weight-completion-by-contact-event",
                "每个正式Contact Event达到满Lock Weight后是否保留完成资格直到几何闭合进入Locked，且未满权Event不得进入Locked",
                new[] { "LockWeightCompletionEvent" },
                new[]
                {
                    "reachedFullWeight=true&&geometryClosedAndLocked=false",
                    "reachedFullWeight=false&&enteredLocked=true"
                },
                lockWeightEvents,
                value =>
                {
                    var rules = new List<string>();
                    if (CharacterFootDiagnosisContext.Evidence(
                            value,
                            "fullWeightNotClosedInWindow"))
                    {
                        rules.Add(
                            "reachedFullWeight=true&&geometryClosedAndLocked=false");
                    }
                    if (CharacterFootDiagnosisContext.Evidence(
                            value,
                            "lockedWithoutFullWeight"))
                    {
                        rules.Add(
                            "reachedFullWeight=false&&enteredLocked=true");
                    }
                    return rules;
                },
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "PlantOutputDistanceAtCompletion"),
                "WindowFrameCount",
                "RequestFrameCount",
                "LockWeightMaximum",
                "LockWeightCompletionThreshold",
                "FirstFullWeightFrame",
                "LandingCompletedFrame",
                "PlantOutputDistanceAtCompletion",
                "PlantPenetrationDepthAtCompletion",
                "LandingLockCompletionTolerance",
                "GroundPenetrationTolerance");
            lockWeightTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["CompletionOutcome"] = CategoryCounts(
                        lockWeightEvents,
                        LockWeightCompletionOutcome)
                };
            CharacterFootDiagnosisTarget approachOwnershipTarget =
                context.Target(
                    "approach-progress-ownership",
                    "正式Approach只准备Prediction目标；Plant所有权不得提前接管，Goal权重按正式FootPlacementWeight与Contact政策独立对账",
                    new[] { "ApproachProgressOwnership" },
                    new[]
                    {
                        "progressMonotonic=false",
                        "sameEventPlantInterpolation=true",
                        "sameEventResidualCapture=true",
                        "approachEventVisiblePositionOwned=true"
                    },
                    approachProgressOwnership,
                    value =>
                    {
                        var rules = new List<string>();
                        if (!CharacterFootDiagnosisContext.Evidence(
                                value,
                                "progressMonotonic"))
                            rules.Add("progressMonotonic=false");
                        if (CharacterFootDiagnosisContext.Evidence(
                                value,
                                "sameEventPlantInterpolation"))
                            rules.Add("sameEventPlantInterpolation=true");
                        if (CharacterFootDiagnosisContext.Evidence(
                                value,
                                "sameEventResidualCapture"))
                            rules.Add("sameEventResidualCapture=true");
                        if (CharacterFootDiagnosisContext.Evidence(
                                value,
                                "approachEventVisiblePositionOwned"))
                            rules.Add(
                                "approachEventVisiblePositionOwned=true");
                        return rules;
                    },
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "FinalEffectiveCorrectionStep"),
                    "ApproachProgress",
                    "ApproachProgressDelta",
                    "FormalFootPlacementWeight",
                    "FormalFootPlacementWeightDelta",
                    "PreparedTargetPointStep",
                    "SelectedTargetPositionStep",
                    "FinalEffectiveCorrectionStep",
                    "PositionWeightDelta",
                    "RotationWeightDelta");
            CharacterFootDiagnosisTarget actionOwnershipTarget =
                context.Target(
                    "action-hard-ownership",
                    "Action Pose占用期间，Hard Ownership Loss是否仍只由Grounded与Current Step Authority决定",
                    new[] { "ActionHardOwnership" },
                    new[] { "actionIndependentOwnership=false" },
                    actionHardOwnership,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "actionIndependentOwnership")
                        ? new List<string>()
                        : new List<string>
                        {
                            "actionIndependentOwnership=false"
                        },
                    value => CharacterFootDiagnosisContext.Evidence(
                        value,
                        "hardOwnershipLoss")
                            ? 1d
                            : 0d,
                    "ActionFootWeight",
                    "FormalFootPlacementWeight",
                    "MotionPositionWeight",
                    "MotionRotationWeight",
                    "ResolvedPositionWeight",
                    "ResolvedRotationWeight");
            CharacterFootDiagnosisTarget contactTransitionTarget =
                context.Target(
                    "contact-transition-context",
                    "上一与当前Lock请求、Contact边沿计时、Event历史及Verified Anchor是否沿唯一Committed Context连续推进",
                    new[] { "ContactTransitionContext" },
                    new[]
                    {
                        "transitionContractConsistent=false",
                        "contextMatchesPreviousFrame=false"
                    },
                    contactTransitions,
                    value =>
                    {
                        var rules = new List<string>();
                        if (!CharacterFootDiagnosisContext.Evidence(
                                value,
                                "transitionContractConsistent"))
                        {
                            rules.Add(
                                "transitionContractConsistent=false");
                        }
                        if (!CharacterFootDiagnosisContext.Evidence(
                                value,
                                "contextMatchesPreviousFrame"))
                        {
                            rules.Add(
                                "contextMatchesPreviousFrame=false");
                        }
                        return rules;
                    },
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "CurrentContactEdgeSeconds"),
                    "PreviousLockRequestWeight",
                    "CurrentLockRequestWeight",
                    "PreviousContactEdgeSeconds",
                    "CurrentContactEdgeSeconds");
            contactTransitionTarget.categoricalMeasurements =
                new SortedDictionary<string, List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["PostTransitionExecution"] = CategoryCounts(
                        contactTransitions,
                        value => CharacterFootDiagnosisContext.Evidence(
                            value, "postTransitionEvaluated")
                            ? "Executed" : "NotExecuted")
                };
            CharacterFootDiagnosisTarget formalGoalWeightTarget = context.Target(
                "formal-goal-weight-policy",
                "Ready与Unavailable帧的Motion、Resolved和最终Goal权重是否来自正式FootPlacementWeight及Contact/Lock政策",
                new[] { "FormalGoalWeightPolicy" },
                new[] { "formalWeightPolicyConsistent=false" },
                formalGoalWeights,
                value => CharacterFootDiagnosisContext.Evidence(
                    value, "formalWeightPolicyConsistent")
                    ? new List<string>()
                    : new List<string> { "formalWeightPolicyConsistent=false" },
                value => CharacterFootDiagnosisContext.Metric(
                    value, "FormalFootPlacementWeight"),
                "FormalFootPlacementWeight", "LockWeight",
                "MotionPositionWeight", "MotionRotationWeight",
                "ResolvedPositionWeight", "ResolvedRotationWeight",
                "FinalGoalPositionWeight", "FinalGoalRotationWeight");
            CharacterFootDiagnosisTarget reentryGeometryTarget = context.Target(
                "contact-reentry-output-geometry",
                "同Event重入帧的上一Response、Residual捕获与衰减、Desired、Response及最终Sole之间实际移动多少；历史保留不代表几何连续",
                new[] { "ContactReentryOutputGeometry" },
                new[] { "sameEventReentryGeometryAvailable=true" },
                reentryGeometry,
                value => CharacterFootDiagnosisContext.Evidence(
                    value, "sameEventReentryGeometryAvailable")
                    ? new List<string> { "sameEventReentryGeometryAvailable=true" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value, "PreviousResponseToResponseStepMeters"),
                "CapturedTargetToPreviousResponseDistanceMeters",
                "ResidualDecayStepMeters", "CapturedTargetToDesiredStepMeters",
                "DesiredToResponseStepMeters", "PreviousResponseToResponseStepMeters",
                "ResponseToFinalSoleStepMeters");
            reentryGeometryTarget.scorePolicy = "Informational";
            List<JObject> contactGapObservations = context.Events(
                "ContactSupportGapObservation");
            List<JObject> contactGapFrames = contactGapObservations.Where(
                value => CharacterFootDiagnosisContext.Evidence(
                    value, "referenceAvailable")).ToList();
            List<JObject> contactGapIntervals = context.Events(
                "ContactSupportGapInterval");
            CharacterFootDiagnosisTarget contactGapTarget = context.Target(
                "contact-support-gap",
                "正式要求接触的Landing/Locked且同Event已验证Anchor可用时，最终物理Heel与Toe是否同时离开该接触平面；包含Landing收敛，不含Releasing，不证明有限Surface下方存在支撑",
                new[] { "ContactSupportGapObservation" },
                new[] { "WholeFootGapMeters>0.01" },
                contactGapFrames,
                value => CharacterFootDiagnosisContext.Metric(
                    value, "WholeFootGapMeters") >
                    CharacterFootMotionDiagnosticAnalyzer.ContactSupportGapThresholdMeters
                    ? new List<string> { "WholeFootGapMeters>0.01" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                    value, "WholeFootGapMeters"),
                "WholeFootGapMeters", "HeelClearanceMeters", "ToeClearanceMeters",
                "SoleClearanceMeters", "InPlaneAnchorDistanceMeters");
            contactGapTarget.scorePolicy = "Informational";
            contactGapTarget.occurrence = context.Occurrence(
                "LandingOrLockedSameEventVerifiedContactPlanePhysicalFootFrame",
                "WholeFootGapMeters", "Meters", contactGapFrames,
                CharacterFootMotionDiagnosticAnalyzer.ContactSupportGapThresholdMeters,
                s_OutputThresholds);
            contactGapTarget.categoricalMeasurements =
                new SortedDictionary<string, List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["GapMotion"] = CategoryCounts(contactGapFrames,
                        value => value["contactSupportGap"].Value<string>("classification")),
                    ["ConstraintState"] = CategoryCounts(contactGapFrames,
                        value => value["contactSupportGap"]["frames"][0]
                            .Value<string>("constraintState"))
                };
            CharacterFootDiagnosisTarget persistentContactGapTarget = context.Target(
                "persistent-contact-support-gap",
                "连续Landing/Locked同Event同已验证Anchor接触段是否整脚离面超过1厘米持续至少100毫秒，或Locked帧离面超过1厘米；Releasing不适用，100毫秒仅为诊断阈值",
                new[] { "ContactSupportGapInterval" },
                new[]
                {
                    "WholeFootGapMeters>0.01&&LongestGapDurationSeconds>=0.1",
                    "Locked&&WholeFootGapMeters>0.01"
                },
                contactGapIntervals,
                value =>
                {
                    var rules = new List<string>();
                    if (CharacterFootDiagnosisContext.Evidence(value, "persistentGap"))
                        rules.Add("WholeFootGapMeters>0.01&&LongestGapDurationSeconds>=0.1");
                    if (CharacterFootDiagnosisContext.Evidence(value, "lockedGap"))
                        rules.Add("Locked&&WholeFootGapMeters>0.01");
                    return rules;
                },
                value => CharacterFootDiagnosisContext.Metric(
                    value, "MaximumWholeFootGapMeters"),
                "MaximumWholeFootGapMeters", "EntryWholeFootGapMeters",
                "ExitWholeFootGapMeters", "LongestGapDurationSeconds",
                "ObservedDurationSeconds", "FrameCount", "GapFrameCount",
                "ClosingFrameCount");
            persistentContactGapTarget.categoricalMeasurements =
                new SortedDictionary<string, List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["GapClassification"] = CategoryCounts(contactGapIntervals,
                        value => value["contactSupportGap"].Value<string>("classification"))
                };
            CharacterFootDiagnosisDocument document = context.Document(
                DiagnosticId,
                context.Target(
                    "missed-landing-entry",
                    "Formal落地边界发生时Runtime是否仍未进入Landing或Locked",
                    new[] { "LandingStateBoundary" },
                    new[] { "runtimeLandingAtBoundary=false&&runtimeLockedAtBoundary=false" },
                    boundaries,
                    value => !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "runtimeLandingAtBoundary") &&
                             !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "runtimeLockedAtBoundary")
                        ? new List<string>
                        {
                            "runtimeLandingAtBoundary=false&&runtimeLockedAtBoundary=false"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctionStepMeters"),
                    "formalStepTimeSeconds",
                    "correctionStepMeters",
                    "finalSoleStepMeters"),
                context.Target(
                    "early-landing-entry",
                    "Runtime Landing入口是否没有对应Formal落地边界",
                    new[] { "LandingStateSpan" },
                    new[] { "entryFollowedFormalBoundary=false" },
                    spans,
                    value => !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "entryFollowedFormalBoundary")
                        ? new List<string> { "entryFollowedFormalBoundary=false" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "entryStateAdditionalOutputStepMeters"),
                    "entryStateAdditionalOutputStepMeters",
                    "entryCorrectedSoleStepMeters",
                    "entryCorrectionReexpressionStepMeters",
                    "frameCount"),
                context.Target(
                    "landing-without-contact-plane",
                    "Runtime Landing状态段是否缺少同Event接触平面",
                    new[] { "LandingStateSpan" },
                    new[] { "contactPlaneAvailableThroughout=false" },
                    spans,
                    value => !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "contactPlaneAvailableThroughout")
                        ? new List<string> { "contactPlaneAvailableThroughout=false" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(value, "frameCount"),
                    "frameCount"),
                context.Target(
                    "landing-not-closing",
                    "多帧Landing状态段是否没有向Anchor闭合",
                    new[] { "LandingStateSpan" },
                    new[] { "frameCount>1&&correctedSoleAnchorClosureMeters<=0" },
                    spans,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "frameCount") > 1d &&
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "correctedSoleAnchorClosureMeters") <= 0d
                        ? new List<string>
                        {
                            "frameCount>1&&correctedSoleAnchorClosureMeters<=0"
                        }
                        : new List<string>(),
                    value => -CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctedSoleAnchorClosureMeters"),
                    "frameCount",
                    "correctedSoleAnchorDistanceEntryMeters",
                    "correctedSoleAnchorDistanceExitMeters",
                    "correctedSoleAnchorClosureMeters",
                    "finalSoleAnchorClosureMeters"),
                context.Target(
                    "landing-wrong-exit",
                    "Landing连续退出是否没有进入Locked或Releasing",
                    new[] { "LandingStateSpan" },
                    new[] { "hasContinuousExit=true&&exitedToLocked=false&&exitedToReleasing=false" },
                    spans,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "hasContinuousExit") &&
                             !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "exitedToLocked") &&
                             !CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "exitedToReleasing")
                        ? new List<string>
                        {
                            "hasContinuousExit=true&&exitedToLocked=false&&exitedToReleasing=false"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "exitStateAdditionalOutputStepMeters"),
                    "exitStateAdditionalOutputStepMeters",
                    "exitCorrectedSoleStepMeters",
                    "exitCorrectionReexpressionStepMeters",
                    "frameCount"),
                context.Target(
                    "landing-exit-jump",
                    "Landing退出边界是否出现超过1厘米、且不能由Anchor保持到Animated Sole正常位移混合解释的正式世界输出跳变",
                    new[] { "LandingStateSpan" },
                    new[] { "exitStateAdditionalOutputStepMeters>0.01" },
                    spans,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "exitStateAdditionalOutputStepMeters") >
                             ExitJumpMeters
                        ? new List<string>
                        {
                            "exitStateAdditionalOutputStepMeters>0.01"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "exitStateAdditionalOutputStepMeters"),
                    "entryStateAdditionalOutputStepMeters",
                    "exitStateAdditionalOutputStepMeters",
                    "entryCorrectedSoleStepMeters",
                    "exitCorrectedSoleStepMeters",
                    "entryAnimatedSoleStepMeters",
                    "exitAnimatedSoleStepMeters",
                    "entryOutputBlendParameter",
                    "exitOutputBlendParameter",
                    "entryCorrectionReexpressionStepMeters",
                    "exitCorrectionReexpressionStepMeters",
                    "entryFinalPhysicalAnkleStepMeters",
                    "exitFinalPhysicalAnkleStepMeters",
                    "entryFinalPhysicalSoleStepMeters",
                    "exitFinalPhysicalSoleStepMeters"),
                context.Target(
                    "landing-persists-after-formal-unlock",
                    "Runtime Landing期间是否已经出现Formal Unlocked",
                    new[] { "LandingStateSpan" },
                    new[] { "formalUnlockedWithinLanding=true" },
                    spans,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "formalUnlockedWithinLanding")
                        ? new List<string> { "formalUnlockedWithinLanding=true" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "formalUnlockedFrameCount"),
                    "formalUnlockedFrameCount",
                    "frameCount"),
                releaseTarget,
                handoffTarget,
                plantTarget,
                contactAcquisitionTarget,
                lockWeightTarget,
                approachOwnershipTarget,
                actionOwnershipTarget,
                contactTransitionTarget,
                formalGoalWeightTarget,
                reentryGeometryTarget,
                contactGapTarget,
                persistentContactGapTarget);
            document.contactSupportGapCoverage = new CharacterFootContactSupportGapCoverage
            {
                primaryGapThresholdMeters =
                    CharacterFootMotionDiagnosticAnalyzer.ContactSupportGapThresholdMeters,
                persistentMinimumSeconds =
                    CharacterFootMotionDiagnosticAnalyzer.ContactSupportGapPersistentSeconds,
                requestedFrameCount = contactGapObservations.Count,
                applicableFrameCount = contactGapObservations.Count(
                    value => CharacterFootDiagnosisContext.Evidence(
                        value, "measurementApplicable")),
                notApplicableFrameCount = contactGapObservations.Count(
                    value => !CharacterFootDiagnosisContext.Evidence(
                        value, "measurementApplicable")),
                availableFrameCount = contactGapFrames.Count,
                unavailableFrameCount = contactGapObservations.Count(
                    value => CharacterFootDiagnosisContext.Evidence(
                        value, "measurementApplicable") &&
                        !CharacterFootDiagnosisContext.Evidence(
                            value, "referenceAvailable")),
                intervalCount = contactGapIntervals.Count,
                availabilityCounts = CategoryCounts(contactGapObservations,
                    value => value["contactSupportGap"]["frames"][0]
                        .Value<string>("availability")),
                unavailableExamples = contactGapObservations.Where(
                        value => CharacterFootDiagnosisContext.Evidence(
                            value, "measurementApplicable") &&
                            !CharacterFootDiagnosisContext.Evidence(
                            value, "referenceAvailable"))
                    .GroupBy(value => value["contactSupportGap"]["frames"][0]
                        .Value<string>("availability"), StringComparer.Ordinal)
                    .SelectMany(group => group.Take(3))
                    .Select(value => value["contactSupportGap"]["frames"][0]
                        .ToObject<CharacterFootContactSupportGapFrame>())
                    .ToList()
            };
            document.contactSupportGapCoverage.availableFrameRate =
                document.contactSupportGapCoverage.applicableFrameCount > 0
                    ? (double?)contactGapFrames.Count /
                        document.contactSupportGapCoverage.applicableFrameCount : null;
            return document;
        }

        static List<CharacterFootDiagnosisCategoryCount> CategoryCounts(
            List<JObject> events,
            Func<JObject, string> selector) => events
            .GroupBy(selector, StringComparer.Ordinal)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => new CharacterFootDiagnosisCategoryCount
            {
                value = value.Key,
                count = value.Count()
            })
            .ToList();

        static string HandoffEvidence(JObject value)
        {
            bool floor = CharacterFootDiagnosisContext.Evidence(
                value,
                "previousSafetyFloorOwned");
            bool residual = CharacterFootDiagnosisContext.Evidence(
                value,
                "residualWithinDeadline");
            bool dropped = CharacterFootDiagnosisContext.Evidence(
                value,
                "floorCompensationDroppedAtLanding");
            if (floor && residual && dropped)
                return "FloorCompensationDroppedAfterResidualDeadline";
            if (floor && dropped)
                return "FloorCompensationDropped";
            if (floor)
                return "PreviousGroundEnvelopeOwned";
            return "NoPreviousGroundEnvelopeOwnership";
        }

        static string PlantDriver(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetEventChanged"))
            {
                return "TargetEventChanged";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetKindChanged"))
            {
                return "TargetKindChanged";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantLockResponseChanged"))
            {
                return "LockResponseChanged";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetForceRefreshed"))
            {
                return "TargetForceRefresh";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetVerticalClamped"))
            {
                return "TargetRateClamp";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantResidualCaptured"))
            {
                return "WorldResidualCaptured";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantWorldResidualOwned"))
            {
                return "WorldResidualContinuity";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "correctionResponseOwned"))
            {
                return "CorrectionResponseContinuity";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "targetHeightOwned"))
            {
                return "TargetHeightContinuity";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetOwned"))
            {
                return "PlantTargetContinuity";
            }
            return "PlantTargetTracking";
        }

        static string ContactAcquisitionReason(JObject value) =>
            CharacterFootDiagnosisContext.Evidence(
                value,
                "newEventContactAcquired")
                ? "NewEventContactAcquired"
                : "ContactAcquired";

        static string ContactAcquisitionLineage(JObject value)
        {
            bool source = CharacterFootDiagnosisContext.Evidence(
                value,
                "sourceContinuous");
            bool contribution = CharacterFootDiagnosisContext.Evidence(
                value,
                "contributionContinuous");
            if (source && contribution)
                return "SourceAndContributionContinuous";
            if (source)
                return "ContributionChanged";
            if (contribution)
                return "SourceChanged";
            return "SourceAndContributionChanged";
        }

        static string ContactAcquisitionEvidence(JObject value)
        {
            bool capture = CharacterFootDiagnosisContext.Evidence(
                value,
                "captureContinuitySatisfied");
            bool target = CharacterFootDiagnosisContext.Evidence(
                value,
                "anchorMatchesSelectedTarget");
            if (capture && target)
                return "CaptureAndAnchorConsistent";
            if (!capture && !target)
                return "CaptureAndAnchorMismatch";
            return capture
                ? "AnchorTargetMismatch"
                : "CaptureContinuityMismatch";
        }

        static string LockWeightCompletionOutcome(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "lockedWithoutFullWeight"))
            {
                return "LockedWithoutFullWeight";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "geometryClosedAndLocked"))
            {
                return "FullWeightClosedAndLocked";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "reachedFullWeight"))
            {
                return "FullWeightNotClosedInWindow";
            }
            return "NoFullWeightNoLock";
        }

    }


}
