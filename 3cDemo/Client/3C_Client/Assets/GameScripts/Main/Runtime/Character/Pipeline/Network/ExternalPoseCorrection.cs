using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public readonly struct ExternalPoseCorrection
    {
        public ExternalPoseCorrection(ulong inputSequence, ulong sourceTick, Vector3 position, Quaternion rotation)
        {
            InputSequence = inputSequence;
            SourceTick = sourceTick;
            Position = position;
            Rotation = rotation;
        }

        public ulong InputSequence { get; }
        public ulong SourceTick { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }

}
