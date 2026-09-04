using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraWorldBasicData
    {
        public CameraWorldBasicData(
            Vector3 pivotLocation,
            Quaternion rotation,
            float radius,
            Vector2 offset,
            float fieldOfView)
        {
            PivotLocation = pivotLocation;
            Rotation = rotation;
            Radius = radius;
            Offset = offset;
            FieldOfView = fieldOfView;
        }

        public Vector3 PivotLocation { get; }
        public Quaternion Rotation { get; }
        public float Radius { get; }
        public Vector2 Offset { get; }
        public float FieldOfView { get; }

        public bool IsValid =>
            Finite(PivotLocation) && Finite(Rotation) &&
            float.IsFinite(Radius) && Radius > 0f &&
            float.IsFinite(Offset.x) && float.IsFinite(Offset.y) &&
            float.IsFinite(FieldOfView) && FieldOfView > 0f;

        public void RequireValid(string source)
        {
            if (!IsValid)
                throw new InvalidOperationException($"{source} contains invalid WorldBasicCameraData.");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool Finite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
