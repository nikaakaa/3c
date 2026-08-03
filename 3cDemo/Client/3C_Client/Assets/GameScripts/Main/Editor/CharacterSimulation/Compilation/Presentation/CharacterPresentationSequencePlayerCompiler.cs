using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPresentationSequencePlayerCompiler
    {
        internal const string CompilerVersion = "sequence-player-compiler/v4";

        internal static CharacterPresentationSequencePlayerDescriptor Compile(
            int index,
            int playerIndex,
            PoseNodeId nodeId,
            PresentationPoseSourceIndex sourceIndex,
            CharacterSequencePlayerPosePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return new CharacterPresentationSequencePlayerDescriptor(
                index,
                nodeId,
                sourceIndex,
                payload.Loop,
                payload.PlayRate,
                payload.InitialTime,
                payload.ClockSource,
                playerIndex);
        }
    }
}
