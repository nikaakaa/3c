using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraZoom", menuName = "3C/Character/Camera/Zoom")]
    public sealed class CameraZoomAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-zoom/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ZoomId = string.Empty;
        [SerializeField] CameraCurveAsset m_StartCurve;
        [SerializeField] CameraCurveAsset m_EndCurve;
        [SerializeField] int m_DataPriority;
        [SerializeField] bool m_IgnorePriorityInEndTime;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_LastTime = -1f;
        [SerializeField] float m_StartTime;
        [SerializeField] CameraEffectStackingType m_StackingType = CameraEffectStackingType.Replace;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] CameraFovVariationType m_FovVariationType = CameraFovVariationType.Absolute;
        [SerializeField] float m_DelayTime;
        [SerializeField] float m_EndTime = 1f;
        [SerializeField] CameraEffectStackingType m_PlayStackingType = CameraEffectStackingType.Add;

        public string Schema => m_Schema ?? string.Empty;
        public string ZoomId => m_ZoomId ?? string.Empty;
        public CameraCurveAsset StartCurve => m_StartCurve;
        public CameraCurveAsset EndCurve => m_EndCurve;
        public int DataPriority => m_DataPriority;
        public bool IgnorePriorityInEndTime => m_IgnorePriorityInEndTime;
        public bool IgnoreWorldTimeScale => m_IgnoreWorldTimeScale;
        public bool IgnoreLocalAvatar => m_IgnoreLocalAvatar;
        public bool IgnoreOwnerTimeScale => m_IgnoreOwnerTimeScale;
        public float LastTime => m_LastTime;
        public float StartTime => m_StartTime;
        public CameraEffectStackingType StackingType => m_StackingType;
        public float FieldOfView => m_FieldOfView;
        public CameraFovVariationType FovVariationType => m_FovVariationType;
        public float DelayTime => m_DelayTime;
        public float EndTime => m_EndTime;
        public CameraEffectStackingType PlayStackingType => m_PlayStackingType;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ZoomId) ||
                !StartCurve || !EndCurve || !float.IsFinite(LastTime) || LastTime < -1f ||
                !float.IsFinite(StartTime) || StartTime < 0f || !float.IsFinite(DelayTime) || DelayTime < 0f ||
                !float.IsFinite(EndTime) || EndTime < 0f ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), StackingType) ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType) ||
                !Enum.IsDefined(typeof(CameraFovVariationType), FovVariationType))
                throw new InvalidOperationException($"Camera Zoom Asset '{name}' is incomplete.");
            StartCurve.RequireValid();
            EndCurve.RequireValid();
        }
    }
}
