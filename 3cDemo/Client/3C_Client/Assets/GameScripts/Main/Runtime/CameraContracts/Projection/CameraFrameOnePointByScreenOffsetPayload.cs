using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFrameOnePointByScreenOffsetPayload : CameraSequenceStagePayload
    {
        [SerializeField] float m_AspectRatio;
        [SerializeField] float m_FieldOfView;
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField] float m_Radius;

        public CameraFrameOnePointByScreenOffsetPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            float aspectRatio,
            float fieldOfView,
            Vector2 screenOffset,
            float radius)
            : base(stageId, CameraSequenceStageKind.FrameOnePointByScreenOffset, makeContextDependent, playLength)
        {
            m_AspectRatio = aspectRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffset = screenOffset;
            m_Radius = radius;
        }

        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float Radius => m_Radius;
    }
}
