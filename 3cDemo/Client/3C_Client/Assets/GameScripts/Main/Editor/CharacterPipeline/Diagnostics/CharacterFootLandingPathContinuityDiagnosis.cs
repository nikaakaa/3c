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
            List<JObject> safetyFloorClamps = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "safetyFloorClamped"));
            List<JObject> residualContinuity = events.FindAll(value =>
                CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathContinuityEvaluated") &&
                !CharacterFootDiagnosisContext.Evidence(
                    value,
                    "pathResidualRebuilt"));
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
                    new[] { "envelopeClearanceAfterMeters<-0.0001" },
                    releasingToSwing,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "envelopeClearanceAfterMeters") <
                             -ClearanceToleranceMeters
                        ? new List<string>
                        {
                            "envelopeClearanceAfterMeters<-0.0001"
                        }
                        : new List<string>(),
                    value => -CharacterFootDiagnosisContext.Metric(
                        value,
                        "envelopeClearanceAfterMeters"),
                    "envelopeClearanceBeforeMeters",
                    "envelopeClearanceAfterMeters",
                    "safetyFloorClampMeters",
                    "correctionStepMeters"),
                context.Target(
                    "residual-deadline-miss",
                    "到达Landing截止帧时Swing Residual是否仍超过LandingUpdateDistance",
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
                    "large-safety-floor-clamp",
                    "真实Envelope安全Floor是否在单帧产生超过LandingUpdateDistance的硬抬升",
                    new[] { "PathContinuity" },
                    new[]
                    {
                        "safetyFloorClampMeters>landingUpdateDistanceMeters"
                    },
                    safetyFloorClamps,
                    value => CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "safetyFloorClampMeters") >
                             CharacterFootDiagnosisContext.Metric(
                                 value,
                                 "landingUpdateDistanceMeters")
                        ? new List<string>
                        {
                            "safetyFloorClampMeters>landingUpdateDistanceMeters"
                        }
                        : new List<string>(),
                    value => CharacterFootDiagnosisContext.Metric(
                        value,
                        "safetyFloorClampMeters"),
                    "safetyFloorClampMeters",
                    "landingUpdateDistanceMeters",
                    "envelopeClearanceBeforeMeters",
                    "envelopeClearanceAfterMeters",
                    "residualBeforeDecayMeters",
                    "residualAfterDecayMeters",
                    "correctionStepMeters"),
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
                    "appliedHalfLifeSeconds"));
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
}
