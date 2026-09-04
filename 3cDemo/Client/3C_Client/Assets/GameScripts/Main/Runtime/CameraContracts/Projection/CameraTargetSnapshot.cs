using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraTargetSnapshot
    {
        public CameraTargetSnapshot(
            string key,
            Vector3 anchorPoint,
            Vector3 aimPoint,
            Vector3 velocity,
            bool valid)
        {
            Key = key ?? string.Empty;
            AnchorPoint = anchorPoint;
            AimPoint = aimPoint;
            Velocity = velocity;
            Valid = valid;
        }

        public string Key { get; }
        public Vector3 AnchorPoint { get; }
        public Vector3 AimPoint { get; }
        public Vector3 Velocity { get; }
        public bool Valid { get; }
    }
}
