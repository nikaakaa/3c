using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFrameOnePointByTrackStage : CameraSequenceStage
    {
        [SerializeField] CameraOrbitDescriptor[] m_CameraOrbits = Array.Empty<CameraOrbitDescriptor>();
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByTrack;
        public IReadOnlyList<CameraOrbitDescriptor> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraOrbitDescriptor>();
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (CameraOrbits.Count != 3 || !float.IsFinite(AspectRatio) || AspectRatio <= 0f ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(ElevationRatio) ||
                !float.IsFinite(PolarAngle) || !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y))
                throw new InvalidOperationException($"{source} contains invalid single-point track framing.");
            for (int i = 0; i < CameraOrbits.Count; i++)
            {
                CameraOrbitDescriptor orbit = CameraOrbits[i];
                if (orbit == null)
                    throw new InvalidOperationException($"{source}.CameraOrbits[{i}] is missing.");
                orbit.RequireValid($"{source}.CameraOrbits[{i}]");
            }
        }
    }
}
