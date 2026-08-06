using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal readonly struct CharacterMotionMatchingSearchKernelResult
    {
        internal CharacterMotionMatchingSearchKernelResult(
            int databaseIndex,
            MotionMatchingQuery query,
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult evaluation,
            MotionMatchingSelectionDecisionKind decisionKind,
            MotionMatchingSelectionGeneration generation,
            MotionMatchingSearchTriggerReason triggerReason)
        {
            if (!evaluation.IsValid || decisionKind == MotionMatchingSelectionDecisionKind.Invalid || !generation.IsValid)
                throw new ArgumentException("Motion Matching Search Kernel result is incomplete.");
            if (databaseIndex < 0 || !query.QueryId.IsValid)
                throw new ArgumentException("Motion Matching Search Kernel winner is incomplete.");
            DatabaseIndex = databaseIndex;
            Query = query;
            Search = search;
            Evaluation = evaluation;
            DecisionKind = decisionKind;
            Generation = generation;
            TriggerReason = triggerReason;
            InvalidReason = MotionMatchingInvalidReason.None;
        }

        internal CharacterMotionMatchingSearchKernelResult(
            MotionMatchingInvalidReason invalidReason,
            MotionMatchingSearchTriggerReason triggerReason)
        {
            if (invalidReason == MotionMatchingInvalidReason.None)
                throw new ArgumentException("Invalid Motion Matching Search Kernel result requires a reason.", nameof(invalidReason));
            DatabaseIndex = -1;
            Query = default;
            Search = default;
            Evaluation = default;
            DecisionKind = MotionMatchingSelectionDecisionKind.Invalid;
            Generation = default;
            TriggerReason = triggerReason;
            InvalidReason = invalidReason;
        }

        internal int DatabaseIndex { get; }
        internal MotionMatchingQuery Query { get; }
        internal MotionMatchingSearchResult Search { get; }
        internal MotionMatchingPlanEvaluationResult Evaluation { get; }
        internal MotionMatchingSelectionDecisionKind DecisionKind { get; }
        internal MotionMatchingSelectionGeneration Generation { get; }
        internal MotionMatchingSearchTriggerReason TriggerReason { get; }
        internal MotionMatchingInvalidReason InvalidReason { get; }
        internal bool IsValid => DecisionKind != MotionMatchingSelectionDecisionKind.Invalid && Generation.IsValid && Evaluation.IsValid;
    }

    internal static class CharacterMotionMatchingSearchKernel
    {
        internal static CharacterMotionMatchingSearchKernelResult Evaluate(
            CharacterMotionMatchingRuntimeDatabase[] databases,
            MotionMatchingQuery[] queries,
            int databaseCount,
            int currentDatabaseIndex,
            MotionMatchingSelectionGeneration currentGeneration,
            MotionMatchingSelectionGeneration jumpGeneration,
            MotionMatchingSearchTriggerReason triggerReason)
        {
            if (databases == null || queries == null || databases.Length != queries.Length ||
                databaseCount <= 0 || databaseCount > databases.Length ||
                currentDatabaseIndex < -1 || currentDatabaseIndex >= databaseCount ||
                !jumpGeneration.IsValid || currentGeneration.IsValid && jumpGeneration.Value <= currentGeneration.Value)
            {
                throw new ArgumentException("Motion Matching Search Kernel input is incomplete.");
            }
            int winnerIndex = -1;
            MotionMatchingSearchResult winnerSearch = default;
            MotionMatchingPlanEvaluationResult winnerEvaluation = default;
            for (int databaseIndex = 0; databaseIndex < databaseCount; databaseIndex++)
            {
                CharacterMotionMatchingRuntimeDatabase database = databases[databaseIndex] ??
                    throw new ArgumentException($"Motion Matching Search Kernel Database #{databaseIndex} is missing.");
                MotionMatchingQuery query = queries[databaseIndex];
                MotionMatchingSearchResult search = new MotionMatchingExactSearch(database).Search(query);
                MotionMatchingPlanEvaluationResult evaluation = new MotionMatchingPlanEvaluator(database).Evaluate(query, search);
                if (!evaluation.IsValid ||
                    winnerEvaluation.IsValid &&
                    MotionMatchingPlanEvaluator.Compare(evaluation.Plan, winnerEvaluation.Plan) >= 0)
                {
                    continue;
                }
                winnerIndex = databaseIndex;
                winnerSearch = search;
                winnerEvaluation = evaluation;
            }
            if (winnerIndex < 0)
            {
                return new CharacterMotionMatchingSearchKernelResult(
                    MotionMatchingInvalidReason.NoValidPlan,
                    triggerReason);
            }
            MotionMatchingQuery winnerQuery = queries[winnerIndex];
            MotionMatchingSelectionDecisionKind kind;
            MotionMatchingSelectionGeneration generation = currentGeneration;
            if (!currentGeneration.IsValid || winnerQuery.Initialization)
            {
                generation = jumpGeneration;
                kind = MotionMatchingSelectionDecisionKind.Initialize;
            }
            else if (winnerIndex == currentDatabaseIndex &&
                     winnerEvaluation.Plan.ContinueCurrent)
            {
                kind = MotionMatchingSelectionDecisionKind.Continue;
            }
            else
            {
                generation = jumpGeneration;
                kind = MotionMatchingSelectionDecisionKind.Jump;
            }
            return new CharacterMotionMatchingSearchKernelResult(
                winnerIndex,
                winnerQuery,
                winnerSearch,
                winnerEvaluation,
                kind,
                generation,
                triggerReason);
        }

    }
}
