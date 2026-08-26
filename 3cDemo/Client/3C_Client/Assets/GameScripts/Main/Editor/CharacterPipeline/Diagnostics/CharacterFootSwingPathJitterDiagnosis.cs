using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootSwingPathJitterDiagnosis : ICharacterFootDiagnosis
    {
        const double CorrectionStepMeters = 0.02d;
        static readonly double[] s_OccurrenceThresholds =
        {
            0.01d,
            0.02d,
            0.05d,
            0.10d
        };

        public string DiagnosticId => "swing-path-jitter";
        public string FileName => "swing-path-jitter.json";

        public CharacterFootDiagnosisDocument Build(CharacterFootDiagnosisContext context)
        {
            List<JObject> allPathChanges =
                context.Events("PathChange");
            List<JObject> events =
                ResolveEligibleEvents(allPathChanges);
            List<JObject> phaseEvents =
                ResolvePhaseAdvanceEvents(allPathChanges);
            CharacterFootDiagnosisTarget target = context.Target(
                "path-change-correction-jump",
                "无Anchor Swing的Ground Path变化附近是否出现修正跳变",
                new[] { "PathChange" },
                new[] { "correctionStepMaximumMeters>0.02" },
                events,
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") > CorrectionStepMeters
                    ? new List<string> { "correctionStepMaximumMeters>0.02" }
                    : new List<string>(),
                value => CharacterFootDiagnosisContext.Metric(
                             value,
                             "correctionStepMaximumMeters") /
                         CorrectionStepMeters,
                "nextLandingEndpointDeltaMeters",
                "nextLandingEndpointDeltaMinimumMeters",
                "nextLandingEndpointDeltaMaximumMeters",
                "semanticPathChangeCount",
                "correctionStepMaximumMeters",
                "correctionExcursionMeters",
                "correctionJerkMetersPerSecondCubed",
                "ActualReconstructionError",
                "PhaseAdvanceDelta",
                "PathRevisionDelta",
                "ObservedSwingTargetDelta",
                "PathRevisionContribution",
                "PhaseContribution");
            target.occurrence = context.Occurrence(
                "UniqueUnanchoredSwingPathJump",
                "correctionStepMaximumMeters",
                "Meters",
                events,
                CorrectionStepMeters,
                s_OccurrenceThresholds);
            CharacterFootPathStageDiagnosisProjection.Apply(
                target,
                events);
            CharacterFootDiagnosisTarget phaseTarget = context.Target(
                "swing-phase-advance",
                "Ground Path变化窗口中的Swing Target跳变是否已由Landing Phase推进解释",
                new[] { "PathChange" },
                new[] { "swingPhaseAdvance" },
                phaseEvents,
                value => new List<string>
                {
                    "swingPhaseAdvance"
                },
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "PhaseAdvanceDelta"),
                "ActualReconstructionError",
                "PhaseAdvanceDelta",
                "PathRevisionDelta",
                "ObservedSwingTargetDelta",
                "PathRevisionContribution",
                "PhaseContribution");
            CharacterFootPathStageDiagnosisProjection.Apply(
                phaseTarget,
                phaseEvents);
            return context.Document(
                DiagnosticId,
                target,
                phaseTarget);
        }

        static List<JObject> ResolveEligibleEvents(List<JObject> events) =>
            events
                .Where(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "unanchoredSwingEligible") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "anchorAvailable") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "swingPhaseAdvance"))
                .GroupBy(
                    value => (
                        side: value.Value<string>("side") ?? string.Empty,
                        peakFrame: value.Value<int?>("peakFrame") ?? 0))
                .Select(MergePeakGroup)
                .OrderBy(value => value.Value<int?>("peakFrame") ?? 0)
                .ThenBy(
                    value => value.Value<string>("side"),
                    StringComparer.Ordinal)
                .ToList();

        static List<JObject> ResolvePhaseAdvanceEvents(
            List<JObject> events) =>
            events
                .Where(value =>
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "unanchoredSwingEligible") &&
                    !CharacterFootDiagnosisContext.Evidence(
                        value,
                        "anchorAvailable") &&
                    CharacterFootDiagnosisContext.Evidence(
                        value,
                        "swingPhaseAdvance"))
                .GroupBy(
                    value => (
                        side: value.Value<string>("side") ??
                              string.Empty,
                        peakFrame:
                            value.Value<int?>("peakFrame") ?? 0))
                .Select(MergePeakGroup)
                .OrderBy(
                    value =>
                        value.Value<int?>("peakFrame") ?? 0)
                .ThenBy(
                    value => value.Value<string>("side"),
                    StringComparer.Ordinal)
                .ToList();

        static JObject MergePeakGroup(
            IGrouping<(string side, int peakFrame), JObject> group)
        {
            List<JObject> values = group.ToList();
            JObject selected = values
                .OrderByDescending(value =>
                    value["pathStageAnalysis"]?["available"]
                        ?.Value<bool?>() ?? false)
                .ThenBy(value => Math.Abs(
                    group.Key.peakFrame -
                    (value.Value<int?>("endFrame") ?? 0)))
                .ThenBy(value => value.Value<int?>("startFrame") ?? 0)
                .First();
            var merged = (JObject)selected.DeepClone();
            merged["startFrame"] = values.Min(
                value => value.Value<int?>("startFrame") ?? 0);
            merged["endFrame"] = values.Max(
                value => value.Value<int?>("endFrame") ?? 0);
            JObject metrics = merged["metrics"] as JObject ?? new JObject();
            metrics["semanticPathChangeCount"] = values.Count;
            metrics["nextLandingEndpointDeltaMinimumMeters"] = values.Min(
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "nextLandingEndpointDeltaMeters"));
            metrics["nextLandingEndpointDeltaMaximumMeters"] = values.Max(
                value => CharacterFootDiagnosisContext.Metric(
                    value,
                    "nextLandingEndpointDeltaMeters"));
            merged["metrics"] = metrics;
            return merged;
        }
    }
}
