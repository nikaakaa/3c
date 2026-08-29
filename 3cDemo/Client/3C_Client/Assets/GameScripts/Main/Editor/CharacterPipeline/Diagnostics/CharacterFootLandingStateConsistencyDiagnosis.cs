using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
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
            List<JObject> stableSwingPlantBlend = context.Events(
                "StableSwingPlantBlendKinematics");
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
                "PlantMixedWorldTargetStep",
                "PlantDesiredOutputPointStep",
                "PlantResponseOutputPointStep",
                "PlantWorldResidualCaptureDelta",
                "PlantWorldResidualCaptureContinuityError",
                "PlantWorldResidualDecayStep",
                "PlantWorldResidualAfterDecay",
                "PlantWorldResidualAppliedHalfLifeSeconds",
                "PlantCorrectionResponseDesired",
                "PlantCorrectionResponsePrevious",
                "PlantCorrectionResponseCurrent",
                "PlantCorrectionResponseSelectedSpeed",
                "PlantCorrectionResponseAppliedDelta",
                "PlantEffectiveCorrectionStep",
                "PlantTargetAppliedVerticalDelta",
                "PlantBlendWeightDelta",
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
            CharacterFootDiagnosisTarget plantBlendStutterTarget =
                context.Target(
                    "stable-swing-plant-blend-stutter",
                    "同Event、同Source、稳定Path的Swing中，Formal Contact推进是否被Plant Blend单帧Hold，并给物理脚引入相对动画Source的额外加速度",
                    new[] { "StableSwingPlantBlendKinematics" },
                    new[] { "advanceToHold=true" },
                    stableSwingPlantBlend,
                    value => CharacterFootDiagnosisContext.Evidence(
                        value,
                        "advanceToHold")
                        ? new List<string> { "advanceToHold=true" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "FootPlacementAddedAccelerationMetersPerFrameSquared"),
                    "FormalContactPrevious",
                    "FormalContactCurrent",
                    "FormalContactDelta",
                    "PlantBlendPrevious",
                    "PlantBlendCurrent",
                    "PlantBlendDelta",
                    "SourceAnkleStepMeters",
                    "PhysicalAnkleStepMeters",
                    "FootPlacementOffsetStepMeters",
                    "SourceAnkleSpeedMetersPerSecond",
                    "PhysicalAnkleSpeedMetersPerSecond",
                    "FootPlacementOffsetSpeedMetersPerSecond",
                    "SourceAnkleAccelerationMetersPerFrameSquared",
                    "PhysicalAnkleAccelerationMetersPerFrameSquared",
                    "FootPlacementAddedAccelerationMetersPerFrameSquared",
                    "SourceAnkleAccelerationMetersPerSecondSquared",
                    "PhysicalAnkleAccelerationMetersPerSecondSquared",
                    "FootPlacementAddedAccelerationMetersPerSecondSquared",
                    "SourceVelocityDirectionCosine",
                    "PhysicalVelocityDirectionCosine",
                    "FootPlacementOffsetVelocityDirectionCosine",
                    "PreviousPresentationDeltaSeconds",
                    "PresentationDeltaSeconds");
            plantBlendStutterTarget.occurrence = context.Occurrence(
                "StableSwingPlantBlendFrameTriple",
                "FootPlacementAddedAccelerationMetersPerFrameSquared",
                "MetersPerFrameSquared",
                stableSwingPlantBlend,
                PrimaryOutputJumpMeters,
                s_OutputThresholds);
            plantBlendStutterTarget.categoricalMeasurements =
                new SortedDictionary<
                    string,
                    List<CharacterFootDiagnosisCategoryCount>>(
                    StringComparer.Ordinal)
                {
                    ["PlantBlendTransition"] = CategoryCounts(
                        stableSwingPlantBlend,
                        PlantBlendTransition),
                    ["TrajectoryResponse"] = CategoryCounts(
                        stableSwingPlantBlend,
                        TrajectoryResponse)
                };
            return context.Document(
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
                plantBlendStutterTarget);
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
                    "plantWeightStarted"))
            {
                return "WeightStartedResidualCapture";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantWeightCompleted"))
            {
                return "WeightCompletedResidualCapture";
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
                    "plantWeightBlendOwned"))
            {
                return "PlantWeightBlendContinuity";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "plantTargetOwned"))
            {
                return "PlantTargetContinuity";
            }
            return "ContinuousPlantBlend";
        }

        static string PlantBlendTransition(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "advanceToHold"))
            {
                return "AdvanceToHold";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "holdToAdvance"))
            {
                return "HoldToAdvance";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "continuousHold"))
            {
                return "ContinuousHold";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "continuousAdvance"))
            {
                return "ContinuousAdvance";
            }
            return "Stable";
        }

        static string TrajectoryResponse(JObject value)
        {
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "trajectoryDirectionReversalIntroduced"))
            {
                return "FootPlacementIntroducedDirectionReversal";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "sourceDirectionReversed"))
            {
                return "SourceDirectionReversal";
            }
            if (CharacterFootDiagnosisContext.Evidence(
                    value,
                    "advanceToHold") ||
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "holdToAdvance") ||
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "continuousHold"))
            {
                return "SpeedHoldRelease";
            }
            return "ContinuousTrajectory";
        }
    }


}
