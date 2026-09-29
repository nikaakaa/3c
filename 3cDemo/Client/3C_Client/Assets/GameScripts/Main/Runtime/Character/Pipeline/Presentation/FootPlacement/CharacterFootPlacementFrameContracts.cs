using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementFrameInput
    {
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
            Body = body;
            Facts = facts;
            ParameterFrame = parameterFrame;
            m_Pose = pose;
        }

        internal ActorId ActorId { get; }
        internal ulong RenderFrame { get; }
        internal float PresentationDeltaSeconds { get; }
        internal float FootPlacementWeight { get; }
        internal CharacterBodyPresentationFrame Body { get; }
        internal CharacterPresentationFactFrame Facts { get; }
        internal CharacterAnimationPoseInputFrame ParameterFrame { get; }
        internal ref readonly CharacterFootPlacementPoseInput Pose => ref m_Pose;
    }
}
