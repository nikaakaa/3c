using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    public readonly struct CharacterActorPoseSnapshot
    {
        public CharacterActorPoseSnapshot(Vector3 position, Vector3 planarForward, bool valid)
        {
            Position = position;
            PlanarForward = planarForward;
            Valid = valid;
        }

        public Vector3 Position { get; }
        public Vector3 PlanarForward { get; }
        public bool Valid { get; }

        public static CharacterActorPoseSnapshot Capture(CharacterLogicBodyState state)
        {
            if (!state.IsValid)
                return default;

            Quaternion rotation = state.Rotation.ToUnityRotation();
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.000001f)
                return default;

            return new CharacterActorPoseSnapshot(state.Position.ToUnityVector(), forward.normalized, true);
        }
    }
}
