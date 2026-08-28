using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootLandingPathContinuityDiagnosis :
        ICharacterFootDiagnosis
    {
        const double ClearanceToleranceMeters = 0.0001d;

        public string DiagnosticId => "landing-path-continuity";
        public string FileName => "landing-path-continuity.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<JObject> events = context.Events("PathContinuity");
            List<JObject> identityOnly = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "identityOnlyInputChange"));
            List<JObject> revisions = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathRevisionExpected") ||
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathResidualRebuilt") ||
                !CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathRevisionReasonMatchesExpected"));
            List<JObject> releasingToSwing = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "releasingCompletedToSwing"));
            List<JObject> deadlines = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "deadlineReached"));
            List<JObject> residualContinuity = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathContinuityEvaluated") &&
                !CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathResidualRebuilt"));
            List<JObject> lateApproach =
                context.Events("LateApproachLandingRevision");
            CharacterFootDiagnosisTarget lateApproachTarget =
                context.Target(
                    "late-approach-landing-revision",
                    "同一Landing Event进入Approach Contact后Consumed NextSwingLanding是否仍跨Surface或超阈值换点",
                    new[] { "LateApproachLandingRevision" },
                    new[]
                    {
                        "consumedSurfaceChanged=true",
                        "LandingPointDelta>SwingRevisionDistance"
                    },
                    lateApproach,
                    value =>
                    {
                        var rules = new List<string>(2);
                        if (CharacterFootDiagnosisContext.Evidence(
                                value,
                                "consumedSurfaceChanged"))
                        {
                            rules.Add("consumedSurfaceChanged=true");
                        }
                        if (CharacterFootDiagnosisContext.Evidence(
                                value,
                                "consumedPointExceededSwingRevisionDistance"))
                        {
                            rules.Add(
                                "LandingPointDelta>SwingRevisionDistance");
                        }
                        return rules;
                    },
                    value => Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "LandingPointDelta"),
                        Math.Max(
                            CharacterFootDiagnosisContext.Metric(
                                value,
                                "CorrectionStep"),
                            Math.Max(
                                CharacterFootDiagnosisContext.Metric(
                                    value,
                                    "PhysicalAnkleAlongUpStep"),
                                CharacterFootDiagnosisContext.Metric(
                                    value,
                                    "PhysicalSoleAlongUpStep")))),
                    "LandingPointDelta",
                    "ObservedLandingPointDelta",
                    "SwingRevisionDistance",
                    "CorrectionStep",
                    "PhysicalAnkleAlongUpStep",
                    "PhysicalSoleAlongUpStep",
                    "SelectedEventPhase",
                    "SelectedApproachContactPhase",
                    "CurrentEventPhase",
                    "CurrentApproachContactPhase");
            return context.Document(
                DiagnosticId,
                context.Target(
                    "identity-only-residual-rebuild",
                    "只有Ground Path输入identity变化时是否错误重建Swing Residual",
                    new[] { "PathContinuity" },
                    new[] { "identityOnlyInputChange=true&&pathResidualRebuilt=true" },
                    identityOnly,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "pathResidualRebuilt")
                        ? new List<string>
                        {
                            "identityOnlyInputChange=true&&pathResidualRebuilt=true"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "correctionStepMeters"),
                    "correctionStepMeters",
                    "landingPointDeltaMeters",
                    "swingTargetDeltaMeters",
                    "residualBeforeRevisionMeters",
                    "residualAfterDecayMeters"),
                context.Target(
                    "path-revision-contract-mismatch",
                    "Path可用性、Event、Landing Point和Swing Target是否与Residual重建原因一致",
                    new[] { "PathContinuity" },
                    new[]
                    {
                        "pathRevisionExpected!=pathResidualRebuilt",
                        "pathRevisionReasonMatchesExpected=false"
                    },
                    revisions,
                    RevisionContractRules,
                    value => Math.Max(
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "landingPointDeltaMeters"),
                        CharacterFootDiagnosisContext.Metric(
                            value,
                            "swingTargetDeltaMeters")),
                    "landingPointDeltaMeters",
                    "swingTargetDeltaMeters",
                    "landingUpdateDistanceMeters",
                    "residualBeforeRevisionMeters",
                    "residualBeforeDecayMeters",
                    "residualAfterDecayMeters"),
                context.Target(
                    "releasing-to-swing-envelope-violation",
                    "Releasing完成进入Swing的同帧脚底是否仍低于真实Envelope",
                    new[] { "PathContinuity" },
                    new[] { "safetyFloorClearanceAfterMeters<-0.0001" },
                    releasingToSwing,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "safetyFloorClearanceAfterMeters") <
                             -ClearanceToleranceMeters
                        ? new List<string>
                        {
                            "safetyFloorClearanceAfterMeters<-0.0001"
                        }
                        : new List<string>(),
                    value => -CharacterFootDiagnosisContext.Metric(
                        value,
                        "safetyFloorClearanceAfterMeters"),
                    "safetyFloorClearanceBeforeMeters",
                    "safetyFloorClearanceAfterMeters",
                    "safetyFloorClampMeters",
                    "correctionStepMeters"),
                context.Target(
                    "residual-deadline-miss",
                    "到达Landing截止帧时Swing Residual是否仍超过SwingRevisionDistance",
                    new[] { "PathContinuity" },
                    new[]
                    {
                        "residualAfterDecayMeters>landingUpdateDistanceMeters"
                    },
                    deadlines,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "residualAfterDecayMeters") >
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "landingUpdateDistanceMeters") +
                             ClearanceToleranceMeters
                        ? new List<string>
                        {
                            "residualAfterDecayMeters>landingUpdateDistanceMeters"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "residualAfterDecayMeters") -
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "landingUpdateDistanceMeters"),
                    "timeToLandingSeconds",
                    "landingUpdateDistanceMeters",
                    "residualBeforeDecayMeters",
                    "residualAfterDecayMeters",
                    "baseHalfLifeSeconds",
                    "deadlineHalfLifeSeconds",
                    "appliedHalfLifeSeconds"),
                context.Target(
                    "residual-growth-without-revision",
                    "没有Path Revision时Swing Residual是否反而增大",
                    new[] { "PathContinuity" },
                    new[] { "residualGrewWithoutRevision=true" },
                    residualContinuity,
                    value => CharacterFootDiagnosisContext.Evidence(
                                 value,
                                 "residualGrewWithoutRevision")
                        ? new List<string>
                        {
                            "residualGrewWithoutRevision=true"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "residualAfterDecayMeters") -
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "residualBeforeDecayMeters"),
                    "residualBeforeDecayMeters",
                    "residualAfterDecayMeters",
                    "appliedHalfLifeSeconds"),
                lateApproachTarget);
        }

        static List<string> RevisionContractRules(JObject value)
        {
            var rules = new List<string>(2);
            bool expected = CharacterFootDiagnosisContext.Evidence(
                value,
                "pathRevisionExpected");
            bool rebuilt = CharacterFootDiagnosisContext.Evidence(
                value,
                "pathResidualRebuilt");
            if (expected != rebuilt)
                rules.Add("pathRevisionExpected!=pathResidualRebuilt");
            if (!CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathRevisionReasonMatchesExpected"))
            {
                rules.Add("pathRevisionReasonMatchesExpected=false");
            }
            return rules;
        }
    }
    [Serializable]
    internal sealed class CharacterFootLateApproachLandingRevisionAnalysis
    {
        public int previousFrame;
        public int frame;
        public string side;
        public string landingEventIdentity;
        public string previousSourceIdentity;
        public string sourceIdentity;
        public int previousSourceCycle;
        public int sourceCycle;
        public string previousContributionContinuityIdentity;
        public string contributionContinuityIdentity;
        public double previousSelectedEventPhase;
        public double selectedEventPhase;
        public double previousSelectedApproachContactPhase;
        public double selectedApproachContactPhase;
        public double previousSelectedLandingPhase;
        public double selectedLandingPhase;
        public double previousCurrentEventPhase;
        public double currentEventPhase;
        public double previousCurrentApproachContactPhase;
        public double currentApproachContactPhase;
        public bool previousSelectedInApproachContactToLanding;
        public bool selectedInApproachContactToLanding;
        public bool previousCurrentAtOrAfterApproachContact;
        public bool currentAtOrAfterApproachContact;
        public bool previousObservedAvailable;
        public bool observedAvailable;
        public string previousObservedEventIdentity;
        public string observedEventIdentity;
        public int previousObservedSurfaceIdentity;
        public int observedSurfaceIdentity;
        public CharacterFootVectorFact previousObservedPoint;
        public CharacterFootVectorFact observedPoint;
        public double observedLandingPointDeltaMeters;
        public string previousConsumedEventIdentity;
        public string consumedEventIdentity;
        public int previousConsumedSurfaceIdentity;
        public int consumedSurfaceIdentity;
        public CharacterFootVectorFact previousConsumedPoint;
        public CharacterFootVectorFact consumedPoint;
        public double landingPointDeltaMeters;
        public double landingUpdateDistanceMeters;
        public double correctionStepMeters;
        public bool physicalAnkleAvailable;
        public double physicalAnkleAlongUpStepMeters;
        public bool physicalSoleAvailable;
        public double physicalSoleAlongUpStepMeters;
        public bool consumedSurfaceChanged;
        public bool consumedPointExceededSwingRevisionDistance;
    }

}
