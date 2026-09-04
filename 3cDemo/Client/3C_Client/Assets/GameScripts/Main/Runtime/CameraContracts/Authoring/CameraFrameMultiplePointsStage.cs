using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFrameMultiplePointsStage : CameraSequenceStage
    {
        [SerializeField, Min(0f)] float m_Radius = 3f;
        [SerializeField] float m_HeightOffset;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_PlayerHeight = 1.8f;
        [SerializeField] Vector2 m_AngleRange = new Vector2(-70f, 70f);
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
        [SerializeField] string m_BeginCameraDataId = string.Empty;
        [SerializeField] CameraCurveAsset m_DeltaHeightToPitch;
        [SerializeField] CameraFrameTwoPointsStage m_FallbackTwoPoints;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameMultiplePointsChat;
        public float Radius => m_Radius;
        public float HeightOffset => m_HeightOffset;
        public float HeightRatio => m_HeightRatio;
        public float PlayerHeight => m_PlayerHeight;
        public Vector2 AngleRange => m_AngleRange;
        public float FieldOfView => m_FieldOfView;
        public LayerMask LayerMask => m_LayerMask;
        public string BeginCameraDataId => m_BeginCameraDataId ?? string.Empty;
        public CameraCurveAsset DeltaHeightToPitch => m_DeltaHeightToPitch;
        public CameraFrameTwoPointsStage FallbackTwoPoints => m_FallbackTwoPoints;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(Radius) || Radius <= 0f || !float.IsFinite(HeightOffset) ||
                !float.IsFinite(HeightRatio) || HeightRatio <= 0f || !float.IsFinite(PlayerHeight) ||
                PlayerHeight <= 0f || !float.IsFinite(AngleRange.x) || !float.IsFinite(AngleRange.y) ||
                AngleRange.x >= AngleRange.y || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                string.IsNullOrWhiteSpace(BeginCameraDataId) || !DeltaHeightToPitch || FallbackTwoPoints == null)
                throw new InvalidOperationException($"{source} contains invalid multiple-point framing.");
            DeltaHeightToPitch.RequireValid();
            FallbackTwoPoints.RequireValid($"{source}.FallbackTwoPoints");
        }
    }
}
