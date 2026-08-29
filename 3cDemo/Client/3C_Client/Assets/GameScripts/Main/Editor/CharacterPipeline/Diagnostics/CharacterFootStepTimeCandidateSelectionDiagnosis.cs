using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootStepTimeCandidateSelectionDiagnosis :
        ICharacterFootDiagnosis
    {
        const int RepresentativeLimitPerReason = 8;

        public string DiagnosticId => "step-time-candidate-selection";
        public string FileName => "step-time-candidate-selection.json";

        public CharacterFootDiagnosisDocument Build(
            CharacterFootDiagnosisContext context)
        {
            List<CharacterFootStepTimeCandidateSelectionObservation>
                observations = context.StepTimeCandidateSelections()
                    .Select(
                        CharacterFootStepTimeCandidateSelectionObservation
                            .From)
                    .ToList();
            var target = new CharacterFootDiagnosisTarget
            {
                id = "step-time-candidate-selection-observations",
                question =
                    "Formal Step Time与Current/Incoming候选选择事实是否具有足够样本",
                eventKinds = new List<string>
                {
                    "StepTimeCandidateSelectionObservation"
                },
                rules = new List<string>(),
                eligibleEventCount = observations.Count,
                matchedEventCount = 0,
                matchedEventRateAvailable = observations.Count > 0,
                matchedEventRate = observations.Count > 0 ? 0d : null,
                scorePolicy = "Informational",
                measurements = new SortedDictionary<
                    string,
                    CharacterFootDiagnosisDistribution>(
                    StringComparer.Ordinal),
                representativeEvents = new List<
                    CharacterFootDiagnosisEvidence>()
            };
            CharacterFootDiagnosisDocument document = context.Document(
                DiagnosticId,
                target);
            document.stepTimeCandidateSelection =
                CharacterFootStepTimeCandidateSelectionReport.Create(
                    observations,
                    RepresentativeLimitPerReason);
            return document;
        }
    }

    [Serializable]
    internal sealed class CharacterFootStepTimeCandidateSelectionReport
    {
        public int observationCount;
        public int formalObservationAvailableCount;
        public int currentEligibleCount;
        public int incomingEligibleCount;
        public SortedDictionary<string, int> selectedSourceCounts;
        public SortedDictionary<string, int> formalCloserCandidateCounts;
        public SortedDictionary<string, int> representativeReasonCounts;
        public SortedDictionary<string, CharacterFootDiagnosisDistribution>
            timeDeltaDistributions;
        public List<CharacterFootStepTimeCandidateRepresentative>
            representativeEvents;
        public List<CharacterFootStepTimeCandidateSelectionObservation>
            observations;

        internal static CharacterFootStepTimeCandidateSelectionReport Create(
            List<CharacterFootStepTimeCandidateSelectionObservation>
                observations,
            int representativeLimitPerReason)
        {
            return new CharacterFootStepTimeCandidateSelectionReport
            {
                observationCount = observations.Count,
                formalObservationAvailableCount = observations.Count(
                    value => value.formalObservationAvailable),
                currentEligibleCount = observations.Count(
                    value => value.current.eligible),
                incomingEligibleCount = observations.Count(
                    value => value.incoming.eligible),
                selectedSourceCounts = Counts(
                    observations.Select(value => value.selectedSource)),
                formalCloserCandidateCounts = Counts(
                    observations.Select(
                        value => value.formalCloserCandidate)),
                representativeReasonCounts =
                    RepresentativeReasonCounts(observations),
                timeDeltaDistributions = new SortedDictionary<
                    string,
                    CharacterFootDiagnosisDistribution>(
                    StringComparer.Ordinal)
                {
                    ["formalToCurrentAbsoluteDeltaSeconds"] =
                        Distribution(
                            observations.Select(
                                value => value
                                    .formalToCurrentAbsoluteDeltaSeconds)),
                    ["formalToIncomingAbsoluteDeltaSeconds"] =
                        Distribution(
                            observations.Select(
                                value => value
                                    .formalToIncomingAbsoluteDeltaSeconds)),
                    ["formalToSelectedAbsoluteDeltaSeconds"] =
                        Distribution(
                            observations.Select(
                                value => value
                                    .formalToSelectedAbsoluteDeltaSeconds))
                },
                representativeEvents = Representatives(
                    observations,
                    representativeLimitPerReason),
                observations = observations
            };
        }

        static CharacterFootDiagnosisDistribution Distribution(
            IEnumerable<double?> values) =>
            CharacterFootDiagnosisDistribution.Create(
                values
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList());

        static SortedDictionary<string, int> Counts(
            IEnumerable<string> values) =>
            new SortedDictionary<string, int>(
                values
                    .GroupBy(value => value ?? string.Empty)
                    .ToDictionary(
                        value => value.Key,
                        value => value.Count(),
                        StringComparer.Ordinal),
                StringComparer.Ordinal);

        static SortedDictionary<string, int> RepresentativeReasonCounts(
            List<CharacterFootStepTimeCandidateSelectionObservation>
                observations) =>
            new SortedDictionary<string, int>(StringComparer.Ordinal)
            {
                ["FormalToSelectedDeltaAboveOneMillisecond"] =
                    observations.Count(
                        value => value
                            .formalToSelectedTimeDeltaAboveOneMillisecond),
                ["NormalizedTimeWrap"] = observations.Count(
                    value => value.normalizedTimeWrapped),
                ["SelectedLandingEventChange"] = observations.Count(
                    value => value.selectedLandingEventChanged),
                ["SelectedSourceChange"] = observations.Count(
                    value => value.selectedSourceChanged)
            };

        static List<CharacterFootStepTimeCandidateRepresentative>
            Representatives(
                List<CharacterFootStepTimeCandidateSelectionObservation>
                    observations,
                int limitPerReason)
        {
            var selected = new Dictionary<(int frame, string side),
                CharacterFootStepTimeCandidateRepresentative>();
            AddReason(
                observations,
                selected,
                "NormalizedTimeWrap",
                value => value.normalizedTimeWrapped,
                limitPerReason);
            AddReason(
                observations,
                selected,
                "SelectedSourceChange",
                value => value.selectedSourceChanged,
                limitPerReason);
            AddReason(
                observations,
                selected,
                "SelectedLandingEventChange",
                value => value.selectedLandingEventChanged,
                limitPerReason);
            AddReason(
                observations,
                selected,
                "FormalToSelectedDeltaAboveOneMillisecond",
                value => value
                    .formalToSelectedTimeDeltaAboveOneMillisecond,
                limitPerReason);
            return selected.Values
                .OrderBy(value => value.frame)
                .ThenBy(value => value.side, StringComparer.Ordinal)
                .ToList();
        }

        static void AddReason(
            List<CharacterFootStepTimeCandidateSelectionObservation>
                observations,
            Dictionary<(int frame, string side),
                CharacterFootStepTimeCandidateRepresentative> selected,
            string reason,
            Func<CharacterFootStepTimeCandidateSelectionObservation, bool>
                predicate,
            int limit)
        {
            IEnumerable<CharacterFootStepTimeCandidateSelectionObservation>
                matches = observations.Where(predicate);
            matches = matches
                .OrderByDescending(
                    value =>
                        value.formalToSelectedAbsoluteDeltaSeconds ?? 0d)
                .ThenBy(value => value.frame)
                .ThenBy(value => value.side, StringComparer.Ordinal);
            foreach (CharacterFootStepTimeCandidateSelectionObservation value
                     in matches.Take(limit))
            {
                var key = (value.frame, value.side);
                if (!selected.TryGetValue(
                        key,
                        out CharacterFootStepTimeCandidateRepresentative
                            representative))
                {
                    representative =
                        CharacterFootStepTimeCandidateRepresentative.From(
                            value);
                    selected.Add(key, representative);
                }
                representative.reasons.Add(reason);
                representative.reasons.Sort(StringComparer.Ordinal);
            }
        }
    }

    [Serializable]
    internal sealed class CharacterFootStepTimeCandidateSelectionObservation
    {
        public int frame;
        public string side;
        public string formalCompletionIdentity;
        public bool formalObservationAvailable;
        public string formalSourceIdentity;
        public int formalSourceCycle;
        public string formalContributionContinuityIdentity;
        public double formalNormalizedTime;
        public double formalTimeSeconds;
        public double maximumPredictionTimeSeconds;
        public string lastLandingEventIdentity;
        public string selectedSource;
        public string selectedLandingEventIdentity;
        public double selectedEventPhase;
        public double selectedApproachContactPhase;
        public double selectedLandingPhase;
        public bool selectedAtOrAfterApproachContact;
        public bool selectedInApproachContactToLanding;
        public CharacterFootStepTimeCandidateObservation current;
        public CharacterFootStepTimeCandidateObservation incoming;
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

        internal static CharacterFootStepTimeCandidateSelectionObservation
            From(JObject value) =>
            new CharacterFootStepTimeCandidateSelectionObservation
            {
                frame = value.Value<int>("frame"),
                side = value.Value<string>("side") ?? string.Empty,
                formalCompletionIdentity =
                    value.Value<string>("formalCompletionIdentity") ??
                    string.Empty,
                formalObservationAvailable =
                    value.Value<bool>("formalObservationAvailable"),
                formalSourceIdentity =
                    value.Value<string>("formalSourceIdentity") ??
                    string.Empty,
                formalSourceCycle = value.Value<int>("formalSourceCycle"),
                formalContributionContinuityIdentity =
                    value.Value<string>(
                        "formalContributionContinuityIdentity") ??
                    string.Empty,
                formalNormalizedTime =
                    value.Value<double>("formalNormalizedTime"),
                formalTimeSeconds = value.Value<double>("formalTimeSeconds"),
                maximumPredictionTimeSeconds =
                    value.Value<double>("maximumPredictionTimeSeconds"),
                lastLandingEventIdentity =
                    value.Value<string>("lastLandingEventIdentity") ?? "0",
                selectedSource =
                    value.Value<string>("selectedSource") ?? string.Empty,
                selectedLandingEventIdentity =
                    value.Value<string>("selectedLandingEventIdentity") ??
                    "0",
                selectedEventPhase =
                    value.Value<double>("selectedEventPhase"),
                selectedApproachContactPhase =
                    value.Value<double>("selectedApproachContactPhase"),
                selectedLandingPhase =
                    value.Value<double>("selectedLandingPhase"),
                selectedAtOrAfterApproachContact =
                    value.Value<bool>("selectedAtOrAfterApproachContact"),
                selectedInApproachContactToLanding =
                    value.Value<bool>(
                        "selectedInApproachContactToLanding"),
                current = CharacterFootStepTimeCandidateObservation.From(
                    value["current"] as JObject),
                incoming = CharacterFootStepTimeCandidateObservation.From(
                    value["incoming"] as JObject),
                selectedOldTimeSeconds =
                    value.Value<double?>("selectedOldTimeSeconds"),
                formalToCurrentAbsoluteDeltaSeconds =
                    value.Value<double?>(
                        "formalToCurrentAbsoluteDeltaSeconds"),
                formalToIncomingAbsoluteDeltaSeconds =
                    value.Value<double?>(
                        "formalToIncomingAbsoluteDeltaSeconds"),
                formalToSelectedAbsoluteDeltaSeconds =
                    value.Value<double?>(
                        "formalToSelectedAbsoluteDeltaSeconds"),
                formalCloserCandidate =
                    value.Value<string>("formalCloserCandidate") ??
                    "Unavailable",
                closerCandidateAvailable =
                    value.Value<bool>("closerCandidateAvailable"),
                closerCandidateLandingEventIdentity =
                    value.Value<string>(
                        "closerCandidateLandingEventIdentity") ?? "0",
                closerCandidateSourceSampleCycle =
                    value.Value<int>(
                        "closerCandidateSourceSampleCycle"),
                closerCandidateSourceLandingCycleOffset =
                    value.Value<int>(
                        "closerCandidateSourceLandingCycleOffset"),
                closerCandidateLandingEventDiffersFromLastLanding =
                    value.Value<bool>(
                        "closerCandidateLandingEventDiffersFromLastLanding"),
                normalizedTimeWrapped =
                    value.Value<bool>("normalizedTimeWrapped"),
                selectedSourceChanged =
                    value.Value<bool>("selectedSourceChanged"),
                selectedLandingEventChanged =
                    value.Value<bool>("selectedLandingEventChanged"),
                formalToSelectedTimeDeltaAboveOneMillisecond =
                    value.Value<bool>(
                        "formalToSelectedTimeDeltaAboveOneMillisecond")
            };
    }

    [Serializable]
    internal sealed class CharacterFootStepTimeCandidateObservation
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
        public CharacterFootStepTimeCandidateVector3 rootLocalLanding;
        public bool positiveTime;
        public bool withinMaximumPredictionTime;
        public bool timeConditionEligible;
        public bool landingEventDiffersFromLastLanding;
        public bool otherConditionsEligible;
        public bool eligible;

        internal static CharacterFootStepTimeCandidateObservation From(
            JObject value)
        {
            value ??= new JObject();
            return new CharacterFootStepTimeCandidateObservation
            {
                isValid = value.Value<bool>("isValid"),
                isAuthoritative = value.Value<bool>("isAuthoritative"),
                hasConsistentLandingEventIdentity =
                    value.Value<bool>(
                        "hasConsistentLandingEventIdentity"),
                isPreSwing = value.Value<bool>("isPreSwing"),
                isSwing = value.Value<bool>("isSwing"),
                eventOrdinal = value.Value<int>("eventOrdinal"),
                sourceLandingCycleOffset =
                    value.Value<int>("sourceLandingCycleOffset"),
                sourceSampleCycle = value.Value<int>("sourceSampleCycle"),
                contributionContinuityIdentity =
                    value.Value<string>(
                        "contributionContinuityIdentity") ?? "0",
                landingEventIdentity =
                    value.Value<string>("landingEventIdentity") ?? "0",
                timeToLandingSeconds =
                    value.Value<double>("timeToLandingSeconds"),
                eventPhase = value.Value<double>("eventPhase"),
                approachContactPhase =
                    value.Value<double>("approachContactPhase"),
                landingPhase = value.Value<double>("landingPhase"),
                atOrAfterApproachContact =
                    value.Value<bool>("atOrAfterApproachContact"),
                inApproachContactToLanding =
                    value.Value<bool>("inApproachContactToLanding"),
                rootLocalLanding =
                    CharacterFootStepTimeCandidateVector3.From(
                        value["rootLocalLanding"] as JObject),
                positiveTime = value.Value<bool>("positiveTime"),
                withinMaximumPredictionTime =
                    value.Value<bool>("withinMaximumPredictionTime"),
                timeConditionEligible =
                    value.Value<bool>("timeConditionEligible"),
                landingEventDiffersFromLastLanding =
                    value.Value<bool>(
                        "landingEventDiffersFromLastLanding"),
                otherConditionsEligible =
                    value.Value<bool>("otherConditionsEligible"),
                eligible = value.Value<bool>("eligible")
            };
        }
    }

    [Serializable]
    internal sealed class CharacterFootStepTimeCandidateVector3
    {
        public double x;
        public double y;
        public double z;

        internal static CharacterFootStepTimeCandidateVector3 From(
            JObject value)
        {
            value ??= new JObject();
            return new CharacterFootStepTimeCandidateVector3
            {
                x = value.Value<double>("x"),
                y = value.Value<double>("y"),
                z = value.Value<double>("z")
            };
        }
    }

    [Serializable]
    internal sealed class CharacterFootStepTimeCandidateRepresentative
    {
        public int frame;
        public string side;
        public List<string> reasons;
        public double formalNormalizedTime;
        public double formalTimeSeconds;
        public string lastLandingEventIdentity;
        public string selectedSource;
        public string selectedLandingEventIdentity;
        public double currentOldTimeSeconds;
        public double incomingOldTimeSeconds;
        public double? selectedOldTimeSeconds;
        public double? formalToCurrentAbsoluteDeltaSeconds;
        public double? formalToIncomingAbsoluteDeltaSeconds;
        public double? formalToSelectedAbsoluteDeltaSeconds;
        public string formalCloserCandidate;
        public string closerCandidateLandingEventIdentity;
        public int closerCandidateSourceSampleCycle;
        public int closerCandidateSourceLandingCycleOffset;
        public bool closerCandidateLandingEventDiffersFromLastLanding;

        internal static CharacterFootStepTimeCandidateRepresentative From(
            CharacterFootStepTimeCandidateSelectionObservation value) =>
            new CharacterFootStepTimeCandidateRepresentative
            {
                frame = value.frame,
                side = value.side,
                reasons = new List<string>(),
                formalNormalizedTime = value.formalNormalizedTime,
                formalTimeSeconds = value.formalTimeSeconds,
                lastLandingEventIdentity =
                    value.lastLandingEventIdentity,
                selectedSource = value.selectedSource,
                selectedLandingEventIdentity =
                    value.selectedLandingEventIdentity,
                currentOldTimeSeconds =
                    value.current.timeToLandingSeconds,
                incomingOldTimeSeconds =
                    value.incoming.timeToLandingSeconds,
                selectedOldTimeSeconds = value.selectedOldTimeSeconds,
                formalToCurrentAbsoluteDeltaSeconds =
                    value.formalToCurrentAbsoluteDeltaSeconds,
                formalToIncomingAbsoluteDeltaSeconds =
                    value.formalToIncomingAbsoluteDeltaSeconds,
                formalToSelectedAbsoluteDeltaSeconds =
                    value.formalToSelectedAbsoluteDeltaSeconds,
                formalCloserCandidate = value.formalCloserCandidate,
                closerCandidateLandingEventIdentity =
                    value.closerCandidateLandingEventIdentity,
                closerCandidateSourceSampleCycle =
                    value.closerCandidateSourceSampleCycle,
                closerCandidateSourceLandingCycleOffset =
                    value.closerCandidateSourceLandingCycleOffset,
                closerCandidateLandingEventDiffersFromLastLanding =
                    value.closerCandidateLandingEventDiffersFromLastLanding
            };
    }
}
