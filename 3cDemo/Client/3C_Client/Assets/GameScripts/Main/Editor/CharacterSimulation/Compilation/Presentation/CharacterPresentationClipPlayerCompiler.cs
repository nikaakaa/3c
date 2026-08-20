using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPresentationClipPlayerCompiler
    {
        internal const string CompilerVersion = "clip-player-compiler/v4";

        internal static CharacterPresentationClipPlayerDescriptor Compile(
            int index,
            int playerIndex,
            PoseNodeId nodeId,
            PresentationPoseSourceIndex sourceIndex,
            CharacterClipPlayerPosePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return new CharacterPresentationClipPlayerDescriptor(
                index,
                nodeId,
                sourceIndex,
                payload.PlayRate,
                payload.InitialTime,
                payload.ClockSource,
                playerIndex);
        }
    }
}
