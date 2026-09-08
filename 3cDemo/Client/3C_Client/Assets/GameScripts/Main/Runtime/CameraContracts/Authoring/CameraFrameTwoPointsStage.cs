using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFrameTwoPointsStage : CameraSequenceStage
    {
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_MinPlayerHeightRatio;
        [SerializeField] float m_MaxPlayerHeightRatio = 1f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] float m_Pitch;
        [SerializeField] Vector2 m_MainHorizontalOffset;
        [SerializeField] Vector2 m_SubHorizontalOffset;
        [SerializeField] float m_MainVerticalOffset;
        [SerializeField] Vector2 m_TargetVerticalOffset;
        [SerializeField] Vector2 m_PitchRange = new Vector2(-70f, 70f);
        [SerializeField] float m_PlayerHeight = 1.8f;
        [SerializeField] string m_BeginCameraDataId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameTwoPointsChat;
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

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(AspectRatio) || AspectRatio <= 0f || !float.IsFinite(HeightRatio) ||
                HeightRatio <= 0f || !float.IsFinite(MinPlayerHeightRatio) ||
                !float.IsFinite(MaxPlayerHeightRatio) || MinPlayerHeightRatio > MaxPlayerHeightRatio ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(Pitch) ||
                !float.IsFinite(PitchRange.x) || !float.IsFinite(PitchRange.y) || PitchRange.x >= PitchRange.y ||
                !float.IsFinite(PlayerHeight) || PlayerHeight <= 0f || string.IsNullOrWhiteSpace(BeginCameraDataId))
                throw new InvalidOperationException($"{source} contains invalid two-point framing.");
        }
    }
}
