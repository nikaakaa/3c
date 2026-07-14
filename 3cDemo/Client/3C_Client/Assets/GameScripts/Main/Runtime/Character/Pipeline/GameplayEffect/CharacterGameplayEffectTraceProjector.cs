using System;
using BTSMTL.Diagnostics;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayEffectTraceProjector
    {
        public void Project(GameplayEffectChangeSet changes, RuntimeDiagnosticsContext diagnostics)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            if (diagnostics == null)
                return;
            RuntimeInstanceKey character = RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
            for (int i = 0; i < changes.EffectChanges.Count; i++)
            {
                GameplayEffectLifecycleChange value = changes.EffectChanges[i];
                diagnostics.Publish(RuntimeTraceChannel.GameplayEffect, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayEffectLifecycle, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.EffectId.Value, Status = value.Operation.ToString(), OwnerId = value.InstanceId.Value.ToString(), Time = value.LifecycleRevision, SecondaryTime = changes.LocalLogicTick, Cycle = value.StackCount });
            }
            for (int i = 0; i < changes.AttributeChanges.Count; i++)
            {
                GameplayEffectAttributeChange change = changes.AttributeChanges[i];
                GameplayAttributeChange value = change.Value;
                diagnostics.Publish(RuntimeTraceChannel.GameplayEffect, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayAttributeChanged, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.AttributeId.Value, Detail = change.CauseEffectId.Value, OwnerId = change.CauseEffectInstanceId.Value.ToString(), Time = value.Revision, Value = DebugValueSnapshot.Capture(value.AfterCurrent) });
            }
            for (int i = 0; i < changes.TagChanges.Count; i++)
            {
                GameplayTagCountChange value = changes.TagChanges[i];
                diagnostics.Publish(RuntimeTraceChannel.GameplayEffect, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayTagChanged, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.TagId.Value, Cycle = value.After, Time = changes.LocalLogicTick });
            }
            for (int i = 0; i < changes.CueChanges.Count; i++)
            {
                GameplayCueChange value = changes.CueChanges[i];
                diagnostics.Publish(RuntimeTraceChannel.GameplayEffect, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayCueSubmitted, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.CueId, Status = value.Trigger.ToString(), Detail = value.EffectId.Value, OwnerId = value.InstanceId.Value.ToString(), Time = changes.LocalLogicTick });
            }
            for (int i = 0; i < changes.ExecutionFailures.Count; i++)
            {
                GameplayEffectExecutionFailure value = changes.ExecutionFailures[i];
                diagnostics.Publish(RuntimeTraceChannel.GameplayEffect, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayEffectLifecycle, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.OwnerEffectId.Value, Status = $"ExecutionFailed:{value.Code}", Detail = $"{value.Trigger}:{value.RequestedEffectId.Value}:{value.Reason}", OwnerId = value.OwnerInstanceId.Value.ToString(), Time = changes.LocalLogicTick });
            }
        }
    }
}
