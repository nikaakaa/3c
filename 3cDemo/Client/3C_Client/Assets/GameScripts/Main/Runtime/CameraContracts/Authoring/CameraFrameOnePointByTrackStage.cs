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
        [SerializeField] CameraTrackOrbitDescriptor m_TopOrbit;
        [SerializeField] float m_TopCurvature;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_AimOffset;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByTrack;
        public IReadOnlyList<CameraTrackOrbitDescriptor> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraTrackOrbitDescriptor>();
        public IReadOnlyList<Vector2> ScreenOffsets => m_ScreenOffsets ?? Array.Empty<Vector2>();
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
        public float CameraLocateRatio => m_CameraLocateRatio;
        public CameraTrackOrbitDescriptor TopOrbit => m_TopOrbit;
        public float TopCurvature => m_TopCurvature;
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 AimOffset => m_AimOffset;

        public void ConfigureOrbit(CameraTrackOrbitDescriptor[] cameraOrbits, Vector2[] screenOffsets,
            float fieldOfView, float elevationRatio, float cameraLocateRatio,
            CameraTrackOrbitDescriptor topOrbit, float topCurvature, Vector3 followOffset, Vector3 aimOffset)
        {
            m_CameraOrbits = cameraOrbits;
            m_ScreenOffsets = screenOffsets;
            m_FieldOfView = fieldOfView;
            m_ElevationRatio = elevationRatio;
            m_CameraLocateRatio = cameraLocateRatio;
            m_TopOrbit = topOrbit;
            m_TopCurvature = topCurvature;
            m_FollowOffset = followOffset;
            m_AimOffset = aimOffset;
        }

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (CameraOrbits.Count != 3 || ScreenOffsets.Count != CameraOrbits.Count || TopOrbit == null ||
                !float.IsFinite(AspectRatio) || AspectRatio <= 0f ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(ElevationRatio) ||
                !float.IsFinite(PolarAngle) || !float.IsFinite(CameraLocateRatio) || CameraLocateRatio <= 0f)
                throw new InvalidOperationException($"{source} contains invalid single-point track framing.");
            TopOrbit.RequireValid(source + ".TopOrbit");
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
