using UnityEngine;

namespace ThirdPersonScene
{
    public readonly struct InteractiveTallGrassBlade
    {
        public InteractiveTallGrassBlade(Vector3 position, float height, float width, float yawDegrees)
        {
            Position = position;
            Height = height;
            Width = width;
            YawDegrees = yawDegrees;
        }

        public Vector3 Position { get; }
        public float Height { get; }
        public float Width { get; }
        public float YawDegrees { get; }
    }
}
