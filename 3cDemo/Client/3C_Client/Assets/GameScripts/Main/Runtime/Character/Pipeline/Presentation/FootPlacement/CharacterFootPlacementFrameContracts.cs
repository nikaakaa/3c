using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementFrameInput
    {
        readonly CharacterBodyPresentationFrame m_Body;
        readonly CharacterPresentationFactFrame m_Facts;
        readonly CharacterAnimationPoseInputFrame m_ParameterFrame;
        readonly CharacterFootPlacementPoseInput m_Pose;

        internal CharacterFootPlacementFrameInput(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            float footPlacementWeight,
            CharacterBodyPresentationFrame body,
            in CharacterPresentationFactFrame facts,
            in CharacterAnimationPoseInputFrame parameterFrame,
            in CharacterFootPlacementPoseInput pose)
        {
            if (!actorId.IsValid || renderFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f ||
                !float.IsFinite(footPlacementWeight) ||
                footPlacementWeight < 0f || footPlacementWeight > 1f ||
                !body.IsValid || !facts.IsValid || !parameterFrame.IsValid)
            {
                throw new ArgumentException("Foot Placement frame input is invalid.");
            }
            ActorId = actorId;
            RenderFrame = renderFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            FootPlacementWeight = footPlacementWeight;
            m_Body = body;
            m_Facts = facts;
            m_ParameterFrame = parameterFrame;
            m_Pose = pose;
        }

        internal ActorId ActorId { get; }
        internal ulong RenderFrame { get; }
        internal float PresentationDeltaSeconds { get; }
        internal float FootPlacementWeight { get; }
        internal ref readonly CharacterBodyPresentationFrame Body => ref m_Body;
        internal ref readonly CharacterPresentationFactFrame Facts => ref m_Facts;
        internal ref readonly CharacterAnimationPoseInputFrame ParameterFrame => ref m_ParameterFrame;
        internal ref readonly CharacterFootPlacementPoseInput Pose => ref m_Pose;
    }
}
