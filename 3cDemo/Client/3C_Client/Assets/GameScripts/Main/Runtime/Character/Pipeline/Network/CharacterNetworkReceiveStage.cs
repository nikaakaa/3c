using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public sealed class CharacterNetworkReceiveStage
    {
        readonly List<ExternalCharacterInputFact> m_PendingInputFacts = new List<ExternalCharacterInputFact>();
        readonly List<ExternalPoseSample> m_PendingPoseSamples = new List<ExternalPoseSample>();
        readonly List<ExternalPoseCorrection> m_PendingPoseCorrections = new List<ExternalPoseCorrection>();
        readonly List<ActionLifecycleTransition> m_PendingActionTransitions = new List<ActionLifecycleTransition>();
        readonly List<IncomingGameplayResult> m_PendingGameplayResults = new List<IncomingGameplayResult>();
        readonly List<GameplayEffectLifecycleFact> m_PendingEffectLifecycleFacts = new List<GameplayEffectLifecycleFact>();
        readonly List<GameplayAttributeValueFact> m_PendingAttributeFacts = new List<GameplayAttributeValueFact>();
        readonly List<GameplayCueFact> m_PendingPresentationCues = new List<GameplayCueFact>();

        public void Push(ExternalCharacterInputFact inputFact)
        {
            if (inputFact.IsValid)
                m_PendingInputFacts.Add(inputFact);
        }

        public void Push(ExternalPoseSample sample)
        {
            m_PendingPoseSamples.Add(sample);
        }

        public void Push(ExternalPoseCorrection correction)
        {
            m_PendingPoseCorrections.Add(correction);
        }

        public void Push(ActionLifecycleTransition transition)
        {
            if (transition.IsValid)
                m_PendingActionTransitions.Add(transition);
        }

        public void Push(IncomingGameplayResult result)
        {
            m_PendingGameplayResults.Add(result);
        }

        public void Push(GameplayEffectLifecycleFact lifecycleFact)
        {
            m_PendingEffectLifecycleFacts.Add(lifecycleFact);
        }

        public void Push(GameplayAttributeValueFact attributeFact)
        {
            m_PendingAttributeFacts.Add(attributeFact);
        }

        public void Push(GameplayCueFact cue)
        {
            if (cue.IsValid)
                m_PendingPresentationCues.Add(cue);
        }

        public void Collect(CharacterPipelineFrame frame)
        {
            frame.NetworkInput.Input.Facts.AddRange(m_PendingInputFacts);
            frame.NetworkInput.Motion.ExternalPoseSamples.AddRange(m_PendingPoseSamples);
            frame.NetworkInput.Motion.ExternalPoseCorrections.AddRange(m_PendingPoseCorrections);
            frame.NetworkInput.Action.LifecycleTransitions.AddRange(m_PendingActionTransitions);
            frame.NetworkInput.GameplayResult.Results.AddRange(m_PendingGameplayResults);
            frame.NetworkInput.GameplayEffect.LifecycleFacts.AddRange(m_PendingEffectLifecycleFacts);
            frame.NetworkInput.GameplayEffect.AttributeFacts.AddRange(m_PendingAttributeFacts);
            frame.NetworkInput.Presentation.Cues.AddRange(m_PendingPresentationCues);
            Clear();
        }

        public void Clear()
        {
            m_PendingInputFacts.Clear();
            m_PendingPoseSamples.Clear();
            m_PendingPoseCorrections.Clear();
            m_PendingActionTransitions.Clear();
            m_PendingGameplayResults.Clear();
            m_PendingEffectLifecycleFacts.Clear();
            m_PendingAttributeFacts.Clear();
            m_PendingPresentationCues.Clear();
        }
    }
}
