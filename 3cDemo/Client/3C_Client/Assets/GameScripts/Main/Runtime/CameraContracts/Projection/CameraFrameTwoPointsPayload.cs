using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFrameTwoPointsPayload : CameraSequenceStagePayload
    {
        [SerializeField] float m_AspectRatio;
        [SerializeField] float m_HeightRatio;
        [SerializeField] float m_MinPlayerHeightRatio;
        [SerializeField] float m_MaxPlayerHeightRatio;
        [SerializeField] float m_FieldOfView;
        [SerializeField] float m_Pitch;
        [SerializeField] Vector2 m_MainHorizontalOffset;
        [SerializeField] Vector2 m_SubHorizontalOffset;
        [SerializeField] float m_MainVerticalOffset;
        [SerializeField] Vector2 m_TargetVerticalOffset;
        [SerializeField] Vector2 m_PitchRange;
        [SerializeField] float m_PlayerHeight;
        [SerializeField] string m_BeginCameraDataId = string.Empty;

        public CameraFrameTwoPointsPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            float aspectRatio,
            float heightRatio,
            float minPlayerHeightRatio,
            float maxPlayerHeightRatio,
            float fieldOfView,
            float pitch,
            Vector2 mainHorizontalOffset,
            Vector2 subHorizontalOffset,
            float mainVerticalOffset,
            Vector2 targetVerticalOffset,
            Vector2 pitchRange,
            float playerHeight,
            string beginCameraDataId)
            : base(stageId, CameraSequenceStageKind.FrameTwoPointsChat, makeContextDependent, playLength)
        {
            m_AspectRatio = aspectRatio;
            m_HeightRatio = heightRatio;
            m_MinPlayerHeightRatio = minPlayerHeightRatio;
            m_MaxPlayerHeightRatio = maxPlayerHeightRatio;
            m_FieldOfView = fieldOfView;
            m_Pitch = pitch;
            m_MainHorizontalOffset = mainHorizontalOffset;
            m_SubHorizontalOffset = subHorizontalOffset;
            m_MainVerticalOffset = mainVerticalOffset;
            m_TargetVerticalOffset = targetVerticalOffset;
            m_PitchRange = pitchRange;
            m_PlayerHeight = playerHeight;
            m_BeginCameraDataId = beginCameraDataId ?? string.Empty;
        }

        public float AspectRatio => m_AspectRatio;
        public float HeightRatio => m_HeightRatio;
        public float MinPlayerHeightRatio => m_MinPlayerHeightRatio;
        public float MaxPlayerHeightRatio => m_MaxPlayerHeightRatio;
        public float FieldOfView => m_FieldOfView;
        public float Pitch => m_Pitch;
        public Vector2 MainHorizontalOffset => m_MainHorizontalOffset;
        public Vector2 SubHorizontalOffset => m_SubHorizontalOffset;
        public float MainVerticalOffset => m_MainVerticalOffset;
        public Vector2 TargetVerticalOffset => m_TargetVerticalOffset;
        public Vector2 PitchRange => m_PitchRange;
        public float PlayerHeight => m_PlayerHeight;
        public string BeginCameraDataId => m_BeginCameraDataId ?? string.Empty;
    }
}
