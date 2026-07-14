using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonGameplay.Effects;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayEffectInputMapper
    {
        readonly List<GameplayEffectAuthorityInput> m_Inputs = new List<GameplayEffectAuthorityInput>();

        public IReadOnlyList<GameplayEffectAuthorityInput> Map(CharacterPipelineFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            m_Inputs.Clear();
            MapLifecycle(frame.NetworkInput.GameplayEffect.LifecycleFacts);
            MapAttributes(frame.NetworkInput.GameplayEffect.AttributeFacts);
            MapGameplayResults(frame.NetworkInput.GameplayResult.Results);
            return m_Inputs;
        }

        void MapLifecycle(IReadOnlyList<GameplayEffectLifecycleFact> facts)
        {
            for (int i = 0; i < facts.Count; i++)
            {
                GameplayEffectLifecycleFact fact = facts[i];
                if (!fact.IsValid)
                    throw new InvalidOperationException($"Invalid Gameplay Effect lifecycle authority fact at index {i}.");
                m_Inputs.Add(new GameplayEffectAuthorityInput(
                    ResolveKind(fact.Operation),
                    fact.Operation,
                    fact.EffectId,
                    fact.InstanceId,
                    fact.Context,
                    fact.StartTick,
                    fact.EndTick,
                    fact.StackCount,
                    fact.LifecycleRevision,
                    fact.DefinitionRevision,
                    fact.SetByCallerValues,
                    predictionKey: fact.Context.PredictionKey,
                    actionInstanceId: fact.Context.SourceActionInstanceId));
            }
        }

        void MapAttributes(IReadOnlyList<GameplayAttributeValueFact> facts)
        {
            for (int i = 0; i < facts.Count; i++)
            {
                GameplayAttributeValueFact fact = facts[i];
                if (!fact.IsValid)
                    throw new InvalidOperationException($"Invalid Gameplay Attribute authority fact at index {i}.");
                m_Inputs.Add(new GameplayEffectAuthorityInput(
                    GameplayEffectAuthorityInputKind.AttributeValue,
                    effectId: fact.CauseEffectId,
                    context: fact.CauseContext,
                    attributeId: fact.AttributeId,
                    baseValue: fact.BaseValue,
                    currentValue: fact.CurrentValue,
                    valueRevision: fact.ValueRevision,
                    causeEffectInstanceId: fact.CauseEffectInstanceId,
                    predictionKey: fact.CauseContext.PredictionKey,
                    actionInstanceId: fact.CauseContext.SourceActionInstanceId));
            }
        }

        void MapGameplayResults(IReadOnlyList<IncomingGameplayResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                IncomingGameplayResult result = results[i];
                IncomingGameplayEffectApplication application = result.EffectApplication;
                if (!application.IsPresent)
                    continue;
                if (!application.IsValid)
                    throw new InvalidOperationException($"Gameplay Result '{result.ResultId}' has an invalid Effect application.");
                var context = new GameplayEffectContext(
                    result.SourceActorId,
                    result.TargetActorId,
                    result.ActionInstanceId,
                    application.PredictionKey,
                    result.ResultId,
                    result.SourceTick,
                    GameplayEffectApplicationMode.Confirmed);
                m_Inputs.Add(new GameplayEffectAuthorityInput(
                    GameplayEffectAuthorityInputKind.Lifecycle,
                    GameplayEffectLifecycleOperation.Applied,
                    application.EffectId,
                    application.InstanceId,
                    context,
                    result.SourceTick,
                    result.SourceTick,
                    1,
                    application.LifecycleRevision,
                    application.DefinitionRevision,
                    application.SetByCallerValues,
                    predictionKey: application.PredictionKey,
                    actionInstanceId: result.ActionInstanceId));
            }
        }

        static GameplayEffectAuthorityInputKind ResolveKind(GameplayEffectLifecycleOperation operation)
        {
            switch (operation)
            {
                case GameplayEffectLifecycleOperation.Confirmed:
                    return GameplayEffectAuthorityInputKind.ConfirmPrediction;
                case GameplayEffectLifecycleOperation.Rejected:
                    return GameplayEffectAuthorityInputKind.RejectPrediction;
                case GameplayEffectLifecycleOperation.Corrected:
                    return GameplayEffectAuthorityInputKind.CorrectPrediction;
                default:
                    return GameplayEffectAuthorityInputKind.Lifecycle;
            }
        }
    }
}
