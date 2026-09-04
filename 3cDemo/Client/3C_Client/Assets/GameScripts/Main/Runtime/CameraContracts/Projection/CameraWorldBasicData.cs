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
            float rotationLengthSquared = rotation.x * rotation.x + rotation.y * rotation.y +
                rotation.z * rotation.z + rotation.w * rotation.w;
            Rotation = rotationLengthSquared > 0.0001f ? rotation.normalized : Quaternion.identity;
            Radius = radius;
            Offset = offset;
            FieldOfView = Mathf.Clamp(fieldOfView, 5f, 170f);
        }

        public Vector3 PivotLocation { get; }
        public Quaternion Rotation { get; }
        public float Radius { get; }
        public Vector2 Offset { get; }
        public float FieldOfView { get; }
        public Vector3 CameraToPivot => Rotation * new Vector3(Offset.x, Offset.y, Radius);
        public Vector3 Location => PivotLocation - CameraToPivot;

        public CameraWorldBasicData WithPivotLocation(Vector3 pivotLocation) =>
            new CameraWorldBasicData(pivotLocation, Rotation, Radius, Offset, FieldOfView);

        public CameraWorldBasicData WithRotation(Quaternion rotation) =>
            new CameraWorldBasicData(PivotLocation, rotation, Radius, Offset, FieldOfView);

        public CameraWorldBasicData WithRadius(float radius) =>
            new CameraWorldBasicData(PivotLocation, Rotation, radius, Offset, FieldOfView);

        public CameraWorldBasicData WithOffset(Vector2 offset) =>
            new CameraWorldBasicData(PivotLocation, Rotation, Radius, offset, FieldOfView);

        public CameraWorldBasicData WithFieldOfView(float fieldOfView) =>
            new CameraWorldBasicData(PivotLocation, Rotation, Radius, Offset, fieldOfView);

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
