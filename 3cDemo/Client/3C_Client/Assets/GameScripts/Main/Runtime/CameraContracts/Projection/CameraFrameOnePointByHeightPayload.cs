using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFrameOnePointByHeightPayload : CameraSequenceStagePayload
    {
        [SerializeField] float m_EntityHeight;
        [SerializeField] float m_HeightRatio;
        [SerializeField] float m_FieldOfView;
        [SerializeField] Vector2 m_ScreenOffset;

        public CameraFrameOnePointByHeightPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            float entityHeight,
            float heightRatio,
            float fieldOfView,
            Vector2 screenOffset)
            : base(stageId, CameraSequenceStageKind.FrameOnePointByHeight, makeContextDependent, playLength)
        {
            m_EntityHeight = entityHeight;
            m_HeightRatio = heightRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffset = screenOffset;
        }

        public float EntityHeight => m_EntityHeight;
        public float HeightRatio => m_HeightRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
    }
}
