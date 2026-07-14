using System;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayEffectFactProjector
    {
        public void Project(GameplayEffectChangeSet changes, CharacterPipelineOutput output)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            GameplayEffectSyncDomainOutput destination = output.SyncFacts.GameplayEffect;
            for (int i = 0; i < changes.EffectChanges.Count; i++)
            {
                GameplayEffectLifecycleChange value = changes.EffectChanges[i];
                destination.LifecycleFacts.Add(new GameplayEffectLifecycleFact(
                    value.EffectId,
                    value.InstanceId,
                    value.Operation,
                    value.Context,
                    value.StartTick,
                    value.EndTick,
                    value.StackCount,
                    value.LifecycleRevision,
                    value.DefinitionRevision,
                    value.Instant,
                    value.SetByCallerValues,
                    changes.LocalLogicTick));
            }
            for (int i = 0; i < changes.AttributeChanges.Count; i++)
            {
                GameplayEffectAttributeChange change = changes.AttributeChanges[i];
                GameplayAttributeChange value = change.Value;
                destination.AttributeFacts.Add(new GameplayAttributeValueFact(
                    value.AttributeId,
                    value.BeforeBase,
                    value.AfterBase,
                    value.BeforeCurrent,
                    value.AfterCurrent,
                    value.Revision,
                    change.CauseEffectId,
                    change.CauseEffectInstanceId,
                    change.CauseContext,
                    changes.LocalLogicTick));
            }
        }
    }
}
