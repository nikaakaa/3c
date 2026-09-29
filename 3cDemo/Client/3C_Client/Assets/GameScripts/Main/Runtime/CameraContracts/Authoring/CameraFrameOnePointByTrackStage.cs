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
        [SerializeField, Tooltip("Top, Middle, Bottom")] CameraTrackOrbitDescriptor[] m_CameraOrbits = Array.Empty<CameraTrackOrbitDescriptor>();
        [SerializeField, Tooltip("Bottom, Middle, Top")] Vector2[] m_ScreenOffsets = Array.Empty<Vector2>();
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;
        [SerializeField] float m_CameraLocateRatio = 1f;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByTrack;
        public IReadOnlyList<CameraTrackOrbitDescriptor> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraTrackOrbitDescriptor>();
        public IReadOnlyList<Vector2> ScreenOffsets => m_ScreenOffsets ?? Array.Empty<Vector2>();
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
        public float CameraLocateRatio => m_CameraLocateRatio;

        public void ConfigureCameraLocateRatio(float cameraLocateRatio)
        {
            m_CameraLocateRatio = cameraLocateRatio;
        }

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (CameraOrbits.Count != 3 || ScreenOffsets.Count != CameraOrbits.Count ||
                !float.IsFinite(AspectRatio) || AspectRatio <= 0f ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(ElevationRatio) ||
                !float.IsFinite(PolarAngle) || !float.IsFinite(CameraLocateRatio) || CameraLocateRatio <= 0f)
                throw new InvalidOperationException($"{source} contains invalid single-point track framing.");
            for (int i = 0; i < CameraOrbits.Count; i++)
            {
                CameraTrackOrbitDescriptor orbit = CameraOrbits[i];
                if (orbit == null)
                    throw new InvalidOperationException($"{source}.CameraOrbits[{i}] is missing.");
                orbit.RequireValid($"{source}.CameraOrbits[{i}]");
                if (!float.IsFinite(ScreenOffsets[i].x) || !float.IsFinite(ScreenOffsets[i].y))
                    throw new InvalidOperationException($"{source}.ScreenOffsets[{i}] is invalid.");
            }
        }
    }
}
