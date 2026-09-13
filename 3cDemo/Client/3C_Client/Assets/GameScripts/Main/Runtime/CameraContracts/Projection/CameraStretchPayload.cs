using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraStretchPayload
    {
        [SerializeField] string m_StretchId = string.Empty;
        [SerializeField] CameraCurvePayload m_StartCurve;
        [SerializeField] CameraCurvePayload m_EndCurve;
        [SerializeField] float m_RuntimeCamFollowYPoints;
        [SerializeField] float m_RotationZ;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IsAppliedElevationRatio;
        [SerializeField] bool m_IsAppliedEndElevationAngle;
        [SerializeField] CameraEffectStackingType m_PlayStackingType;
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
        [SerializeField] CameraSpace m_CamOffsetSpace;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_ElevationAngleMax;
        [SerializeField] float m_HoldTime;
        [SerializeField] float m_DelayTime;
        [SerializeField] Vector3 m_CamOffset;
        [SerializeField] float m_EndElevationAngleMin;
        [SerializeField] float m_RadiusRatio;
        [SerializeField] float m_StretchTime;
        [SerializeField] CameraFovVariationType m_FovVariationType;

        public CameraStretchPayload(
            string stretchId,
            CameraCurvePayload startCurve,
            CameraCurvePayload endCurve,
            float runtimeCamFollowYPoints,
            float rotationZ,
            bool ignoreLocalAvatar,
            bool isAppliedElevationRatio,
            bool isAppliedEndElevationAngle,
            CameraEffectStackingType playStackingType,
            float runtimeCamFollowYOffsetRatio,
            bool isElevationAngleAbsolute,
            bool ignorePriorityInEndTime,
            bool ignoreWorldTimeScale,
            bool isEndElevationAngleAbsolute,
            float elevationAngleMin,
            float recoilTime,
            int dataPriority,
            float endElevationAngleMax,
            bool applyAimPointsCameraFollowYOffset,
            bool applyRuntimeCamFollowYOffset,
            CameraSpace camOffsetSpace,
            bool ignoreOwnerTimeScale,
            float elevationAngleMax,
            float holdTime,
            float delayTime,
            Vector3 camOffset,
            float endElevationAngleMin,
            float radiusRatio,
            float stretchTime,
            CameraFovVariationType fovVariationType)
        {
            m_StretchId = stretchId ?? string.Empty;
            m_StartCurve = startCurve;
            m_EndCurve = endCurve;
            m_RuntimeCamFollowYPoints = runtimeCamFollowYPoints;
            m_RotationZ = rotationZ;
            m_IgnoreLocalAvatar = ignoreLocalAvatar;
            m_IsAppliedElevationRatio = isAppliedElevationRatio;
            m_IsAppliedEndElevationAngle = isAppliedEndElevationAngle;
            m_PlayStackingType = playStackingType;
            m_RuntimeCamFollowYOffsetRatio = runtimeCamFollowYOffsetRatio;
            m_IsElevationAngleAbsolute = isElevationAngleAbsolute;
            m_IgnorePriorityInEndTime = ignorePriorityInEndTime;
            m_IgnoreWorldTimeScale = ignoreWorldTimeScale;
            m_IsEndElevationAngleAbsolute = isEndElevationAngleAbsolute;
            m_ElevationAngleMin = elevationAngleMin;
            m_RecoilTime = recoilTime;
            m_DataPriority = dataPriority;
            m_EndElevationAngleMax = endElevationAngleMax;
            m_ApplyAimPointsCameraFollowYOffset = applyAimPointsCameraFollowYOffset;
            m_ApplyRuntimeCamFollowYOffset = applyRuntimeCamFollowYOffset;
            m_CamOffsetSpace = camOffsetSpace;
            m_IgnoreOwnerTimeScale = ignoreOwnerTimeScale;
            m_ElevationAngleMax = elevationAngleMax;
            m_HoldTime = holdTime;
            m_DelayTime = delayTime;
            m_CamOffset = camOffset;
            m_EndElevationAngleMin = endElevationAngleMin;
            m_RadiusRatio = radiusRatio;
            m_StretchTime = stretchTime;
            m_FovVariationType = fovVariationType;
        }

        public string StretchId => m_StretchId ?? string.Empty;
        public CameraCurvePayload StartCurve => m_StartCurve;
        public CameraCurvePayload EndCurve => m_EndCurve;
        public float RuntimeCamFollowYPoints => m_RuntimeCamFollowYPoints;
        public float RotationZ => m_RotationZ;
        public bool IgnoreLocalAvatar => m_IgnoreLocalAvatar;
        public bool IsAppliedElevationRatio => m_IsAppliedElevationRatio;
        public bool IsAppliedEndElevationAngle => m_IsAppliedEndElevationAngle;
        public CameraEffectStackingType PlayStackingType => m_PlayStackingType;
        public float RuntimeCamFollowYOffsetRatio => m_RuntimeCamFollowYOffsetRatio;
        public bool IsElevationAngleAbsolute => m_IsElevationAngleAbsolute;
        public bool IgnorePriorityInEndTime => m_IgnorePriorityInEndTime;
        public bool IgnoreWorldTimeScale => m_IgnoreWorldTimeScale;
        public bool IsEndElevationAngleAbsolute => m_IsEndElevationAngleAbsolute;
        public float ElevationAngleMin => m_ElevationAngleMin;
        public float RecoilTime => m_RecoilTime;
        public int DataPriority => m_DataPriority;
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

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(StretchId) || StartCurve == null || EndCurve == null ||
                !float.IsFinite(RuntimeCamFollowYPoints) || !float.IsFinite(RotationZ) ||
                !float.IsFinite(RuntimeCamFollowYOffsetRatio) || !float.IsFinite(ElevationAngleMin) ||
                !float.IsFinite(EndElevationAngleMin) || !float.IsFinite(ElevationAngleMax) ||
                !float.IsFinite(EndElevationAngleMax) || !float.IsFinite(RecoilTime) || RecoilTime < 0f ||
                !float.IsFinite(HoldTime) || HoldTime < -1f || !float.IsFinite(DelayTime) || DelayTime < 0f ||
                !Finite(CamOffset) || !float.IsFinite(RadiusRatio) || !float.IsFinite(StretchTime) || StretchTime < 0f ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType) ||
                !Enum.IsDefined(typeof(CameraSpace), CamOffsetSpace) ||
                !Enum.IsDefined(typeof(CameraFovVariationType), FovVariationType))
                throw new InvalidOperationException($"{source} contains an invalid Camera Stretch payload.");
            StartCurve.RequireValid(source + ".StartCurve");
            EndCurve.RequireValid(source + ".EndCurve");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
