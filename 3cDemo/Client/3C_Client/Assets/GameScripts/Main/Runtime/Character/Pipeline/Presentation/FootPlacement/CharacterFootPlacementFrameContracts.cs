using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementFrameInput
    {
        internal CharacterFootPlacementFrameInput(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            in CharacterPresentationFactFrame facts,
            in CharacterFootPlacementPoseInput pose)
        {
            if (!actorId.IsValid || renderFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f || !body.IsValid || !facts.IsValid)
            {
                throw new ArgumentException("Foot Placement frame input is invalid.");
            }
            ActorId = actorId;
            RenderFrame = renderFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Body = body;
            Facts = facts;
            Pose = pose;
        }

        internal ActorId ActorId { get; }
        internal ulong RenderFrame { get; }
        internal float PresentationDeltaSeconds { get; }
        internal CharacterBodyPresentationFrame Body { get; }
        internal CharacterPresentationFactFrame Facts { get; }
        internal CharacterFootPlacementPoseInput Pose { get; }
    }
}
