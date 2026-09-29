using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFrameOnePointByTrackPayload : CameraSequenceStagePayload
    {
        [SerializeField] float m_AspectRatio;
        [SerializeField] float m_FieldOfView;
        [SerializeField] Vector2[] m_ScreenOffsets;
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;
        [SerializeField] float m_CameraLocateRatio;
        [SerializeField] CameraTrackOrbitPayload m_TopOrbit;
        [SerializeField] float m_TopCurvature;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_AimOffset;
        [SerializeField] CameraTrackOrbitPayload[] m_CameraOrbits;
        [SerializeField] Vector4[] m_TrackControl1;
        [SerializeField] Vector4[] m_TrackControl2;
        public CameraFrameOnePointByTrackPayload(
            string stageId,
            CameraTrackOrbitPayload[] cameraOrbits,
            float aspectRatio,
            float fieldOfView,
            Vector2[] screenOffsets,
            Vector4[] trackControl1,
            Vector4[] trackControl2,
            float elevationRatio,
            float polarAngle,
            float cameraLocateRatio,
            CameraTrackOrbitPayload topOrbit,
            float topCurvature,
            Vector3 followOffset,
            Vector3 aimOffset)
            : base(stageId, CameraSequenceStageKind.FrameOnePointByTrack)
        {
            m_CameraOrbits = cameraOrbits ?? Array.Empty<CameraTrackOrbitPayload>();
            m_AspectRatio = aspectRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffsets = screenOffsets ?? Array.Empty<Vector2>();
            m_TrackControl1 = trackControl1;
            m_TrackControl2 = trackControl2;
            m_ElevationRatio = elevationRatio;
            m_PolarAngle = polarAngle;
            m_CameraLocateRatio = cameraLocateRatio;
            m_TopOrbit = topOrbit;
            m_TopCurvature = topCurvature;
            m_FollowOffset = followOffset;
            m_AimOffset = aimOffset;
        }

        public IReadOnlyList<CameraTrackOrbitPayload> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraTrackOrbitPayload>();
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public IReadOnlyList<Vector2> ScreenOffsets => m_ScreenOffsets ?? Array.Empty<Vector2>();
        public IReadOnlyList<Vector4> TrackControl1 => m_TrackControl1;
        public IReadOnlyList<Vector4> TrackControl2 => m_TrackControl2;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
        public float CameraLocateRatio => m_CameraLocateRatio;
        public CameraTrackOrbitPayload TopOrbit => m_TopOrbit;
        public float TopCurvature => m_TopCurvature;
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 AimOffset => m_AimOffset;
    }
}
