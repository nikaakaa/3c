using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public sealed class CharacterNetworkInput
    {
        public InputSyncDomainInput Input { get; } = new InputSyncDomainInput();
        public MotionSyncDomainInput Motion { get; } = new MotionSyncDomainInput();
        public ActionSyncDomainInput Action { get; } = new ActionSyncDomainInput();
        public GameplayResultSyncDomainInput GameplayResult { get; } = new GameplayResultSyncDomainInput();
        public GameplayEffectSyncDomainInput GameplayEffect { get; } = new GameplayEffectSyncDomainInput();
        public PresentationSyncDomainInput Presentation { get; } = new PresentationSyncDomainInput();

        public void Clear()
        {
            Input.Clear();
            Motion.Clear();
            Action.Clear();
            GameplayResult.Clear();
            GameplayEffect.Clear();
            Presentation.Clear();
        }
    }

    public sealed class InputSyncDomainInput
    {
        public List<ExternalCharacterInputFact> Facts { get; } = new List<ExternalCharacterInputFact>();

        public void Clear()
        {
            Facts.Clear();
        }
    }

    public sealed class MotionSyncDomainInput
    {
        public List<ExternalPoseSample> ExternalPoseSamples { get; } = new List<ExternalPoseSample>();
        public List<ExternalPoseCorrection> ExternalPoseCorrections { get; } = new List<ExternalPoseCorrection>();

        public void Clear()
        {
            ExternalPoseSamples.Clear();
            ExternalPoseCorrections.Clear();
        }
    }

    public sealed class ActionSyncDomainInput
    {
        public List<ActionLifecycleTransition> LifecycleTransitions { get; } = new List<ActionLifecycleTransition>();

        public void Clear()
        {
            LifecycleTransitions.Clear();
        }
    }

    public sealed class GameplayResultSyncDomainInput
    {
        public List<IncomingGameplayResult> Results { get; } = new List<IncomingGameplayResult>();

        public void Clear()
        {
            Results.Clear();
        }
    }

    public sealed class GameplayEffectSyncDomainInput
    {
        public List<GameplayEffectLifecycleFact> LifecycleFacts { get; } = new List<GameplayEffectLifecycleFact>();
        public List<GameplayAttributeValueFact> AttributeFacts { get; } = new List<GameplayAttributeValueFact>();

        public void Clear()
        {
            LifecycleFacts.Clear();
            AttributeFacts.Clear();
        }
    }

    public sealed class PresentationSyncDomainInput
    {
        public List<GameplayCueFact> Cues { get; } = new List<GameplayCueFact>();

        public void Clear()
        {
            Cues.Clear();
        }
    }

}
