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
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;
        [SerializeField] CameraOrbitPayload[] m_CameraOrbits;
        public CameraFrameOnePointByTrackPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            CameraOrbitPayload[] cameraOrbits,
            float aspectRatio,
            float fieldOfView,
            Vector2 screenOffset,
            float elevationRatio,
            float polarAngle)
            : base(stageId, CameraSequenceStageKind.FrameOnePointByTrack, makeContextDependent, playLength)
        {
            m_CameraOrbits = cameraOrbits ?? Array.Empty<CameraOrbitPayload>();
            m_AspectRatio = aspectRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffset = screenOffset;
            m_ElevationRatio = elevationRatio;
            m_PolarAngle = polarAngle;
        }

        public IReadOnlyList<CameraOrbitPayload> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraOrbitPayload>();
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
    }
}
