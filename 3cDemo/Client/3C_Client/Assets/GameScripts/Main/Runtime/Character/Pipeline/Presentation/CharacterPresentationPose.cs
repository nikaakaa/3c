using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterPresentationRootPose
    {
        public CharacterPresentationRootPose(Vector3 position, Quaternion rotation, bool grounded, bool valid)
        {
            Position = position;
            Rotation = rotation;
            Grounded = grounded;
            Valid = valid;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Grounded { get; }
        public bool Valid { get; }

        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return Position + Rotation * localPoint;
        }
    }

    public readonly struct CharacterVisualPose
    {
        public CharacterVisualPose(Vector3 position, Quaternion rotation, bool grounded, bool valid)
        {
            Position = position;
            Rotation = rotation;
            Grounded = grounded;
            Valid = valid;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Grounded { get; }
        public bool Valid { get; }
    }

}
