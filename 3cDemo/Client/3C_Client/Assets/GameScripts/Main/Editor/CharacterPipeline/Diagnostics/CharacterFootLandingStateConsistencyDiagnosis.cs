using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLandingStateConsistencyDiagnosis : ICharacterFootDiagnosis
    {
        const double ExitJumpMeters = 0.01d;
        static readonly double[] s_HandoffOccurrenceThresholds =
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
            List<JObject> handoffs =
                context.Events("SwingToLandingFloorHandoff");
            CharacterFootDiagnosisTarget handoffTarget = context.Target(
                "swing-to-landing-floor-handoff-jump",
                "Swing进入Landing时Safety Floor补偿交接是否产生Correction或物理脚跳变",
                new[] { "SwingToLandingFloorHandoff" },
                new[] { "entryCorrectionStepMeters>0.01" },
                handoffs,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "entryCorrectionStepMeters") > ExitJumpMeters
                    ? new List<string>
                    {
                        "entryCorrectionStepMeters>0.01"
                    }
                    : new List<string>(),
                value => Math.Max(
                    CharacterFootDiagnosisContext.Metric(
                        value,
                        "entryCorrectionStepMeters"),
                    Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "entryPhysicalAnkleStepMeters"),
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "entryPhysicalSoleStepMeters"))),
                "entryCorrectionStepMeters",
                "entryCorrectionAlongUpMeters",
                "entryPhysicalAnkleStepMeters",
                "entryPhysicalAnkleAlongUpMeters",
                "entryPhysicalSoleStepMeters",
                "entryPhysicalSoleAlongUpMeters",
                "previousSafetyFloorClampMeters",
                "previousClearanceBeforeMeters",
                "previousClearanceAfterMeters",
                "previousResidualAfterDecayMeters",
                "landingUpdateDistanceMeters",
                "previousSafetyFloorCompensationMeters",
                "stepHeightMeters",
                "previousFormalFootHeightMeters",
                "formalFootHeightMeters",
                "previousProgress",
                "progress",
                "previousTimeToLandingSeconds",
                "timeToLandingSeconds");
            handoffTarget.occurrence = context.Occurrence(
                "ContinuousSwingToLandingBoundary",
                "entryCorrectionStepMeters",
                "Meters",
                handoffs,
                ExitJumpMeters,
                s_HandoffOccurrenceThresholds);
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
                        "entryCorrectionStepMeters"),
                    "entryCorrectionStepMeters",
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
                        "exitCorrectionStepMeters"),
                    "exitCorrectionStepMeters",
                    "frameCount"),
                context.Target(
                    "landing-exit-jump",
                    "Landing退出边界是否出现超过1厘米的Correction跳变",
                    new[] { "LandingStateSpan" },
                    new[] { "exitCorrectionStepMeters>0.01" },
                    spans,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "exitCorrectionStepMeters") > ExitJumpMeters
                        ? new List<string> { "exitCorrectionStepMeters>0.01" }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "exitCorrectionStepMeters"),
                    "entryCorrectionStepMeters",
                    "exitCorrectionStepMeters"),
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
                handoffTarget);
        }
    }

    [Serializable]
    internal sealed class CharacterFootSwingToLandingFloorHandoffAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string eventIdentity;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public string stateBefore;
        public string stateAfter;
        public double entryCorrectionStepMeters;
        public double entryCorrectionAlongUpMeters;
        public bool entryPhysicalAnkleAvailable;
        public double entryPhysicalAnkleStepMeters;
        public double entryPhysicalAnkleAlongUpMeters;
        public bool entryPhysicalSoleAvailable;
        public double entryPhysicalSoleStepMeters;
        public double entryPhysicalSoleAlongUpMeters;
        public double previousSafetyFloorClampMeters;
        public double previousSafetyFloorClearanceBeforeMeters;
        public double previousSafetyFloorClearanceAfterMeters;
        public double previousResidualAfterDecayMeters;
        public double landingUpdateDistanceMeters;
        public CharacterFootVectorFact previousFinalEffectiveCorrection;
        public CharacterFootVectorFact finalEffectiveCorrection;
        public CharacterFootVectorFact previousSafetyFloorMinimumCorrection;
        public CharacterFootVectorFact previousSafetyFloorOutputCorrection;
        public double previousSafetyFloorCompensationMeters;
        public double previousSafetyFloorCompensationAlongUpMeters;
        public bool currentSafetyFloorAvailable;
        public string currentFloorState;
        public bool currentFloorAccepted;
        public int currentFloorSurfaceIdentity;
        public double currentContactOwnership;
        public bool currentContactPlaneAvailable;
        public int currentContactSurfaceIdentity;
        public double stepHeightMeters;
        public string stepDirection;
        public double previousFormalFootHeightMeters;
        public double formalFootHeightMeters;
        public bool previousFormalFootHeightAvailable;
        public bool formalFootHeightAvailable;
        public double previousProgress;
        public double progress;
        public double previousTimeToLandingSeconds;
        public double timeToLandingSeconds;
        public bool previousSafetyFloorOwned;
        public bool residualWithinDeadline;
        public bool floorCompensationDroppedAtLanding;
    }
}
