using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraRigResult
    {
        public CameraRigResult(CameraBasisSnapshot basis, Vector3 position, Quaternion rotation, float fieldOfView, bool valid)
        {
            Basis = basis;
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
            Valid = valid;
        }

        public CameraBasisSnapshot Basis { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float FieldOfView { get; }
        public bool Valid { get; }
    }
}
