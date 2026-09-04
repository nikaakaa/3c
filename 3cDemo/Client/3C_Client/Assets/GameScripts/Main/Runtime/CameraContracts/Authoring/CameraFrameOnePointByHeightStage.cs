using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFrameOnePointByHeightStage : CameraSequenceStage
    {
        [SerializeField] float m_EntityHeight = 1.8f;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] Vector2 m_ScreenOffset;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByHeight;
        public float EntityHeight => m_EntityHeight;
        public float HeightRatio => m_HeightRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(EntityHeight) || EntityHeight <= 0f || !float.IsFinite(HeightRatio) ||
                HeightRatio <= 0f || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y))
                throw new InvalidOperationException($"{source} contains invalid single-point height framing.");
        }
    }
}
