using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFrameOnePointByScreenOffsetStage : CameraSequenceStage
    {
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField, Min(0f)] float m_Radius = 3f;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByScreenOffset;
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float Radius => m_Radius;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(AspectRatio) || AspectRatio <= 0f || !float.IsFinite(FieldOfView) ||
                FieldOfView <= 0f || !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y) ||
                !float.IsFinite(Radius) || Radius <= 0f)
                throw new InvalidOperationException($"{source} contains invalid single-point screen framing.");
        }
    }
}
