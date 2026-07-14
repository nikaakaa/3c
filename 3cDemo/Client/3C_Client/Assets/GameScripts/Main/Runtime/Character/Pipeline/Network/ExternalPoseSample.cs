using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public readonly struct ExternalPoseSample
    {
        public ExternalPoseSample(ulong sourceTick, Vector3 position, Quaternion rotation, string stateId)
        {
            SourceTick = sourceTick;
            Position = position;
            Rotation = rotation;
            StateId = stateId;
        }

        public ulong SourceTick { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public string StateId { get; }
    }
}
