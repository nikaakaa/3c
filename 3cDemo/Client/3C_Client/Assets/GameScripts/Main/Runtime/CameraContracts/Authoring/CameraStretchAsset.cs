using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraStretch", menuName = "3C/Character/Camera/Stretch")]
    public sealed class CameraStretchAsset : CameraEffectAsset
    {
        public const string SchemaVersion = "character-camera-stretch/v2";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_StretchId = string.Empty;
        [SerializeField] CameraCurveAsset m_StartCurve;
        [SerializeField] CameraCurveAsset m_EndCurve;
        [SerializeField] string[] m_RuntimeCamFollowYPoints = Array.Empty<string>();
        [SerializeField] float m_RotationZ;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IsAppliedElevationRatio;
        [SerializeField] bool m_IsAppliedEndElevationAngle;
        [SerializeField] CameraEffectStackingType m_PlayStackingType = CameraEffectStackingType.Add;
        [SerializeField] int m_StackingType;
        [SerializeField] float m_RuntimeCamFollowYOffsetRatio;
        [SerializeField] bool m_IsElevationAngleAbsolute;
        [SerializeField] bool m_IgnorePriorityInEndTime;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] bool m_IsEndElevationAngleAbsolute;
        [SerializeField] float m_ElevationAngleMin;
        [SerializeField] float m_RecoilTime;
        [SerializeField] int m_DataPriority;
        [SerializeField] float m_EndElevationAngleMax;
        [SerializeField] bool m_ApplyAimPointsCameraFollowYOffset;
        [SerializeField] bool m_ApplyRuntimeCamFollowYOffset;
        [SerializeField] CameraSpace m_CamOffsetSpace = CameraSpace.LocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_ElevationAngleMax;
        [SerializeField] float m_HoldTime = -1f;
        [SerializeField] float m_DelayTime;
        [SerializeField] Vector3 m_CamOffset;
        [SerializeField] float m_EndElevationAngleMin;
        [SerializeField] float m_RadiusRatio;
        [SerializeField] float m_StretchTime;
        [SerializeField] CameraFovVariationType m_FovVariationType = CameraFovVariationType.Absolute;

        public string Schema => m_Schema ?? string.Empty;
        public string StretchId => m_StretchId ?? string.Empty;
        public CameraCurveAsset StartCurve => m_StartCurve;
        public CameraCurveAsset EndCurve => m_EndCurve;
        public IReadOnlyList<string> RuntimeCamFollowYPoints => m_RuntimeCamFollowYPoints;
        public float RotationZ => m_RotationZ;
        public bool IgnoreLocalAvatar => m_IgnoreLocalAvatar;
        public bool IsAppliedElevationRatio => m_IsAppliedElevationRatio;
        public bool IsAppliedEndElevationAngle => m_IsAppliedEndElevationAngle;
        public CameraEffectStackingType PlayStackingType => m_PlayStackingType;
        public int StackingType => m_StackingType;
        public float RuntimeCamFollowYOffsetRatio => m_RuntimeCamFollowYOffsetRatio;
        public bool IsElevationAngleAbsolute => m_IsElevationAngleAbsolute;
        public bool IgnorePriorityInEndTime => m_IgnorePriorityInEndTime;
        public bool IgnoreWorldTimeScale => m_IgnoreWorldTimeScale;
        public bool IsEndElevationAngleAbsolute => m_IsEndElevationAngleAbsolute;
        public float ElevationAngleMin => m_ElevationAngleMin;
        public float RecoilTime => m_RecoilTime;
        public int DataPriority => m_DataPriority;

        public override string EffectId => StretchId;
        public override int EffectPriority => DataPriority;
        public float EndElevationAngleMax => m_EndElevationAngleMax;
        public bool ApplyAimPointsCameraFollowYOffset => m_ApplyAimPointsCameraFollowYOffset;
        public bool ApplyRuntimeCamFollowYOffset => m_ApplyRuntimeCamFollowYOffset;
        public CameraSpace CamOffsetSpace => m_CamOffsetSpace;
        public bool IgnoreOwnerTimeScale => m_IgnoreOwnerTimeScale;
        public float ElevationAngleMax => m_ElevationAngleMax;
        public float HoldTime => m_HoldTime;
        public float DelayTime => m_DelayTime;
        public Vector3 CamOffset => m_CamOffset;
        public float EndElevationAngleMin => m_EndElevationAngleMin;
        public float RadiusRatio => m_RadiusRatio;
        public float StretchTime => m_StretchTime;
        public CameraFovVariationType FovVariationType => m_FovVariationType;

        public void ConfigureStacking(CameraEffectStackingType playStackingType, int stackingType)
        {
            m_PlayStackingType = playStackingType;
            m_StackingType = stackingType;
        }

        public void ConfigureFollowPoints(string[] runtimeCamFollowYPoints)
        {
            m_Schema = SchemaVersion;
            m_RuntimeCamFollowYPoints = runtimeCamFollowYPoints;
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(StretchId) ||
                !StartCurve || !EndCurve ||
                !float.IsFinite(RotationZ) || !float.IsFinite(RuntimeCamFollowYOffsetRatio) ||
                !float.IsFinite(ElevationAngleMin) || !float.IsFinite(EndElevationAngleMin) ||
                !float.IsFinite(ElevationAngleMax) || !float.IsFinite(EndElevationAngleMax) ||
                !float.IsFinite(RecoilTime) || RecoilTime < 0f || !float.IsFinite(HoldTime) || HoldTime < -1f ||
                !float.IsFinite(DelayTime) || DelayTime < 0f || !float.IsFinite(CamOffset.x) ||
                !float.IsFinite(CamOffset.y) || !float.IsFinite(CamOffset.z) || !float.IsFinite(RadiusRatio) ||
                !float.IsFinite(StretchTime) || StretchTime < 0f ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType) ||
                !Enum.IsDefined(typeof(CameraSpace), CamOffsetSpace) ||
                !Enum.IsDefined(typeof(CameraFovVariationType), FovVariationType))
                throw new InvalidOperationException($"Camera Stretch Asset '{name}' is incomplete.");
            StartCurve.RequireValid();
            EndCurve.RequireValid();
        }
    }
}
