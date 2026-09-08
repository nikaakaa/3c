using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public abstract class CameraSequenceStage
    {
        [SerializeField] string m_StageId = string.Empty;
        [SerializeField] bool m_MakeContextDependent;
        [SerializeField] float m_PlayLength = -1f;

        public string StageId => m_StageId ?? string.Empty;
        public bool MakeContextDependent => m_MakeContextDependent;
        public float PlayLength => m_PlayLength;
        public abstract CameraSequenceStageKind Kind { get; }

        public void ConfigureIdentity(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("Camera Sequence stage identity is missing.", nameof(stageId));
            m_StageId = stageId.Trim();
        }

        public virtual void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(StageId) || !Enum.IsDefined(typeof(CameraSequenceStageKind), Kind) ||
                !float.IsFinite(PlayLength) || PlayLength == 0f || PlayLength < -1f)
                throw new InvalidOperationException($"{source} contains an invalid Camera Sequence stage.");
        }
    }
}
