using UnityEngine;

namespace ThirdPersonPresentation
{
    public readonly struct PresentationPose
    {
        public PresentationPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public static PresentationPose FromTransform(Transform source)
        {
            return new PresentationPose(source.position, source.rotation);
        }
    }
}
