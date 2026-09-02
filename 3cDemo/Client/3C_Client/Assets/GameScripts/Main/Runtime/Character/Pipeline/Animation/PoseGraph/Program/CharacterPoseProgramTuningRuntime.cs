using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramTuningRuntime : IDisposable
    {
        readonly CharacterPoseProgramTuningState m_Tuning;
        readonly CharacterPoseActorState m_ActorState;

        internal CharacterPoseProgramTuningRuntime(
            CharacterPoseProgramTuningState tuning,
            CharacterPoseActorState actorState)
        {
            m_Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            m_ActorState = actorState ??
                throw new ArgumentNullException(nameof(actorState));
        }

        internal CharacterPoseProgramTuningView Require(ulong generation) =>
            m_Tuning.RequireCommitted(generation);

        internal string Prepare(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation)
        {
            try
            {
                m_Tuning.PrepareCandidate(layout, block, generation);
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                string error = m_ActorState.PoseStateSources.StateMachines[i]
                    .PrepareTuningCandidate(layout, block);
                if (string.IsNullOrEmpty(error))
                    continue;
                Discard();
                return error;
            }
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
            {
                string error = m_ActorState.Stacks[i].PrepareTuningCandidate(
                    layout,
                    block);
                if (string.IsNullOrEmpty(error))
                    continue;
                Discard();
                return error;
            }
            string inertializationError = m_ActorState.Inertialization
                .PrepareTuningCandidate(layout, block);
            if (!string.IsNullOrEmpty(inertializationError))
            {
                Discard();
                return inertializationError;
            }
            return string.Empty;
        }

        internal void Commit(ulong generation)
        {
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.StateMachines[i]
                    .CommitTuningCandidate();
            }
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].CommitTuningCandidate();
            m_ActorState.Inertialization.CommitTuningCandidate();
            m_Tuning.CommitCandidate(generation);
        }

        internal void Discard()
        {
            for (int i = 0;
                 i < m_ActorState.PoseStateSources.StateMachines.Length;
                 i++)
            {
                m_ActorState.PoseStateSources.StateMachines[i]
                    .DiscardTuningCandidate();
            }
            for (int i = 0; i < m_ActorState.Stacks.Length; i++)
                m_ActorState.Stacks[i].DiscardTuningCandidate();
            m_ActorState.Inertialization.DiscardTuningCandidate();
            m_Tuning.DiscardCandidate();
        }

        public void Dispose() => m_Tuning.Dispose();
    }
}
