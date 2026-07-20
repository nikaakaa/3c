using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterPosePostProcessResetReason : byte
    {
        Initialization = 1,
        BodyStreamReset = 2,
        PresentationReset = 3,
        MissingAnimationOutput = 4,
        InvalidPose = 5,
        Dispose = 6
    }

    internal readonly struct CharacterPosePostProcessReset
    {
        public CharacterPosePostProcessReset(
            ActorId actorId,
            ulong renderFrame,
            ulong resetSequence,
            CharacterPosePostProcessResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            ActorId = actorId;
            RenderFrame = renderFrame;
            ResetSequence = resetSequence;
            Reason = reason;
            BodyReason = bodyReason;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong ResetSequence { get; }
        public CharacterPosePostProcessResetReason Reason { get; }
        public CharacterBodyPresentationResetReason BodyReason { get; }
    }

    internal readonly struct CharacterPosePostProcessFrame
    {
        public CharacterPosePostProcessFrame(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            IReadOnlyList<AnimationPoseContribution> animationContributions)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Pose Post Process Actor identity is invalid.", nameof(actorId));
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (presentationDeltaSeconds < 0f || float.IsNaN(presentationDeltaSeconds) ||
                float.IsInfinity(presentationDeltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            ActorId = actorId;
            RenderFrame = renderFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Body = body;
            AnimationContributions = animationContributions ??
                                     throw new ArgumentNullException(nameof(animationContributions));
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public float PresentationDeltaSeconds { get; }
        public CharacterBodyPresentationFrame Body { get; }
        public IReadOnlyList<AnimationPoseContribution> AnimationContributions { get; }
    }

    internal interface ICharacterPosePostProcessPass : IDisposable
    {
        void Present(CharacterPosePostProcessFrame frame);
        void Reset(CharacterPosePostProcessReset reset);
    }
}
