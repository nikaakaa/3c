using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFrameMultiplePointsPayload : CameraSequenceStagePayload
    {
        [SerializeField] float m_Radius;
        [SerializeField] float m_HeightOffset;
        [SerializeField] float m_HeightRatio;
        [SerializeField] float m_PlayerHeight;
        [SerializeField] Vector2 m_AngleRange;
        [SerializeField] float m_FieldOfView;
        [SerializeField] CameraCurvePayload m_DeltaHeightToPitch;
        [SerializeReference] CameraFrameTwoPointsPayload m_FallbackTwoPoints;

        public CameraFrameMultiplePointsPayload(
            string stageId,
            float radius,
            float heightOffset,
            float heightRatio,
            float playerHeight,
            Vector2 angleRange,
            float fieldOfView,
            CameraCurvePayload deltaHeightToPitch,
            CameraFrameTwoPointsPayload fallbackTwoPoints)
            : base(stageId, CameraSequenceStageKind.FrameMultiplePointsChat)
        {
            m_Radius = radius;
            m_HeightOffset = heightOffset;
            m_HeightRatio = heightRatio;
            m_PlayerHeight = playerHeight;
            m_AngleRange = angleRange;
            m_FieldOfView = fieldOfView;
            m_DeltaHeightToPitch = deltaHeightToPitch;
            m_FallbackTwoPoints = fallbackTwoPoints;
        }

        public float Radius => m_Radius;
        public float HeightOffset => m_HeightOffset;
        public float HeightRatio => m_HeightRatio;
        public float PlayerHeight => m_PlayerHeight;
        public Vector2 AngleRange => m_AngleRange;
        public float FieldOfView => m_FieldOfView;
        public CameraCurvePayload DeltaHeightToPitch => m_DeltaHeightToPitch;
        public CameraFrameTwoPointsPayload FallbackTwoPoints => m_FallbackTwoPoints;
    }
}
