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
        [SerializeField] CameraTrackOrbitPayload[] m_CameraOrbits;
        public CameraFrameOnePointByTrackPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            CameraTrackOrbitPayload[] cameraOrbits,
            float aspectRatio,
            float fieldOfView,
            Vector2[] screenOffsets,
            float elevationRatio,
            float polarAngle)
            : base(stageId, CameraSequenceStageKind.FrameOnePointByTrack, makeContextDependent, playLength)
        {
            m_CameraOrbits = cameraOrbits ?? Array.Empty<CameraTrackOrbitPayload>();
            m_AspectRatio = aspectRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffsets = screenOffsets ?? Array.Empty<Vector2>();
            m_ElevationRatio = elevationRatio;
            m_PolarAngle = polarAngle;
        }

        public IReadOnlyList<CameraTrackOrbitPayload> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraTrackOrbitPayload>();
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public IReadOnlyList<Vector2> ScreenOffsets => m_ScreenOffsets ?? Array.Empty<Vector2>();
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
    }
}
