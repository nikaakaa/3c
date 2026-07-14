using System;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonGameplay.Effects;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayCueProjector
    {
        public void Project(GameplayEffectChangeSet changes, CharacterPipelineOutput output)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            for (int i = 0; i < changes.CueChanges.Count; i++)
            {
                GameplayCueChange value = changes.CueChanges[i];
                output.SyncFacts.Presentation.CueEvents.Add(new GameplayCueFact(
                    value.EffectId.Value,
                    value.CueId,
                    value.Trigger.ToString(),
                    value.Context.SourceActionInstanceId,
                    value.EffectId,
                    value.InstanceId,
                    value.Context,
                    changes.LocalLogicTick));
            }
        }
    }
}
