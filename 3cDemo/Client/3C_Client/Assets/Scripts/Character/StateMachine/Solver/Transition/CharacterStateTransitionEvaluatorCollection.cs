using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public interface ICharacterStateTransitionConditionEvaluator
    {
        string Name { get; }
        IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions { get; }
        CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input);
    }

    public sealed class CharacterStateTransitionEvaluatorCollection
    {
        static readonly ICharacterStateTransitionConditionEvaluator[] defaultEvaluators =
        {
            new CharacterStateCoreConditionEvaluator(),
            new CharacterStateLocomotionConditionEvaluator(),
            new CharacterStateAnimationConditionEvaluator(),
            new CharacterStateActionConditionEvaluator()
        };

        readonly ICharacterStateTransitionConditionEvaluator[] evaluators;
        readonly Dictionary<CharacterStateTransitionConditionKind, ICharacterStateTransitionConditionEvaluator> evaluatorByKind;
        readonly CharacterStateTransitionConditionKind[] supportedConditions;

        public CharacterStateTransitionEvaluatorCollection(params ICharacterStateTransitionConditionEvaluator[] evaluators)
        {
            if (!TryBuild(evaluators, out this.evaluators, out evaluatorByKind, out supportedConditions, out string error))
                throw new InvalidOperationException(error);
        }

        CharacterStateTransitionEvaluatorCollection(
            ICharacterStateTransitionConditionEvaluator[] evaluators,
            Dictionary<CharacterStateTransitionConditionKind, ICharacterStateTransitionConditionEvaluator> evaluatorByKind,
            CharacterStateTransitionConditionKind[] supportedConditions)
        {
            this.evaluators = evaluators;
            this.evaluatorByKind = evaluatorByKind;
            this.supportedConditions = supportedConditions;
        }

        public static CharacterStateTransitionEvaluatorCollection Default { get; } =
            new CharacterStateTransitionEvaluatorCollection(defaultEvaluators);

        public IReadOnlyList<ICharacterStateTransitionConditionEvaluator> Evaluators => evaluators;
        public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supportedConditions;

        public static bool TryCreate(
            IReadOnlyList<ICharacterStateTransitionConditionEvaluator> evaluators,
            out CharacterStateTransitionEvaluatorCollection collection,
            out string error)
        {
            if (!TryBuild(evaluators, out ICharacterStateTransitionConditionEvaluator[] ordered, out Dictionary<CharacterStateTransitionConditionKind, ICharacterStateTransitionConditionEvaluator> map, out CharacterStateTransitionConditionKind[] keys, out error))
            {
                collection = null;
                return false;
            }

            collection = new CharacterStateTransitionEvaluatorCollection(ordered, map, keys);
            return true;
        }

        public bool Supports(CharacterStateTransitionConditionKind conditionKind)
        {
            return evaluatorByKind.ContainsKey(conditionKind);
        }

        public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
        {
            if (!evaluatorByKind.TryGetValue(input.Condition.Kind, out ICharacterStateTransitionConditionEvaluator evaluator))
                throw new InvalidOperationException($"Transition condition '{input.Condition.Kind}' has no evaluator.");

            return evaluator.Evaluate(in input);
        }

        static bool TryBuild(
            IReadOnlyList<ICharacterStateTransitionConditionEvaluator> source,
            out ICharacterStateTransitionConditionEvaluator[] ordered,
            out Dictionary<CharacterStateTransitionConditionKind, ICharacterStateTransitionConditionEvaluator> map,
            out CharacterStateTransitionConditionKind[] keys,
            out string error)
        {
            source = source ?? Array.Empty<ICharacterStateTransitionConditionEvaluator>();
            ordered = new ICharacterStateTransitionConditionEvaluator[source.Count];
            map = new Dictionary<CharacterStateTransitionConditionKind, ICharacterStateTransitionConditionEvaluator>();
            List<CharacterStateTransitionConditionKind> keyList = new List<CharacterStateTransitionConditionKind>();

            for (int i = 0; i < source.Count; i++)
            {
                ICharacterStateTransitionConditionEvaluator evaluator = source[i];
                if (evaluator == null)
                {
                    keys = Array.Empty<CharacterStateTransitionConditionKind>();
                    error = $"Condition evaluator[{i}] is missing.";
                    return false;
                }

                ordered[i] = evaluator;
                IReadOnlyList<CharacterStateTransitionConditionKind> supported = evaluator.SupportedConditions;
                if (supported == null || supported.Count == 0)
                {
                    keys = Array.Empty<CharacterStateTransitionConditionKind>();
                    error = $"Condition evaluator '{evaluator.Name}' declares no supported condition keys.";
                    return false;
                }

                for (int keyIndex = 0; keyIndex < supported.Count; keyIndex++)
                {
                    CharacterStateTransitionConditionKind key = supported[keyIndex];
                    if (map.TryGetValue(key, out ICharacterStateTransitionConditionEvaluator existing))
                    {
                        keys = Array.Empty<CharacterStateTransitionConditionKind>();
                        error = $"Condition evaluator key '{key}' is duplicated by '{existing.Name}' and '{evaluator.Name}'.";
                        return false;
                    }

                    map.Add(key, evaluator);
                    keyList.Add(key);
                }
            }

            keys = keyList.ToArray();
            error = string.Empty;
            return true;
        }
    }
}
