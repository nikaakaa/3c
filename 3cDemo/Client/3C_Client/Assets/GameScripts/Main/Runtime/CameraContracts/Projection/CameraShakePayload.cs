using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraShakePayload
    {
        [SerializeField] string m_ShakeId = string.Empty;
        [SerializeField] int m_ShakeType;
        [SerializeField] int m_CameraShakePropertyConfig;
        [SerializeField] float m_AngleVertical;
        [SerializeField] float m_NoiseAngle;
        [SerializeField] float m_RadiusLength;
        [SerializeField] float m_DistanceToPlane;
        [SerializeField] float m_NoiseRatio;
        [SerializeField] float m_ShakeTotalTime;
        [SerializeField] float m_Frequency;
        [SerializeField] float m_RollAmplitude;
        [SerializeField] float m_PitchAmplitude;
        [SerializeField] float m_YawAmplitude;
        [SerializeField] CameraSpace m_ShakeCenterSpace;
        [SerializeField] bool m_RealtimeVibration;
        [SerializeField] int m_DissipationMode;
        [SerializeField] float m_ImpactRadius;
        [SerializeField] float m_DissipationDistance;
        [SerializeField] CameraCurvePayload m_CustomCurve;
        [SerializeField] float m_FadeInDuration;
        [SerializeField] CameraCurvePayload m_FadeInCurve;
        [SerializeField] float m_FadeOutDuration;
        [SerializeField] CameraCurvePayload m_FadeOutCurve;
        [SerializeField] CameraCurvePayload m_Curve;
        [SerializeField] bool m_IgnoreTimeScale;
        [SerializeField] CameraEffectStackingType m_PlayStackingType;
        [SerializeField] int m_PlayPriority;
        [SerializeField] int m_DataPriority;
        [SerializeField] string m_StandardConfigKey = string.Empty;

        public CameraShakePayload(
            string shakeId,
            int shakeType,
            int cameraShakePropertyConfig,
            float angleVertical,
            float noiseAngle,
            float radiusLength,
            float distanceToPlane,
            float noiseRatio,
            float shakeTotalTime,
            float frequency,
            float rollAmplitude,
            float pitchAmplitude,
            float yawAmplitude,
            CameraSpace shakeCenterSpace,
            bool realtimeVibration,
            int dissipationMode,
            float impactRadius,
            float dissipationDistance,
            CameraCurvePayload customCurve,
            float fadeInDuration,
            CameraCurvePayload fadeInCurve,
            float fadeOutDuration,
            CameraCurvePayload fadeOutCurve,
            CameraCurvePayload curve,
            bool ignoreTimeScale,
            CameraEffectStackingType playStackingType,
            int playPriority,
            int dataPriority,
            string standardConfigKey)
        {
            m_ShakeId = shakeId ?? string.Empty;
            m_ShakeType = shakeType;
            m_CameraShakePropertyConfig = cameraShakePropertyConfig;
            m_AngleVertical = angleVertical;
            m_NoiseAngle = noiseAngle;
            m_RadiusLength = radiusLength;
            m_DistanceToPlane = distanceToPlane;
            m_NoiseRatio = noiseRatio;
            m_ShakeTotalTime = shakeTotalTime;
            m_Frequency = frequency;
            m_RollAmplitude = rollAmplitude;
            m_PitchAmplitude = pitchAmplitude;
            m_YawAmplitude = yawAmplitude;
            m_ShakeCenterSpace = shakeCenterSpace;
            m_RealtimeVibration = realtimeVibration;
            m_DissipationMode = dissipationMode;
            m_ImpactRadius = impactRadius;
            m_DissipationDistance = dissipationDistance;
            m_CustomCurve = customCurve;
            m_FadeInDuration = fadeInDuration;
            m_FadeInCurve = fadeInCurve;
            m_FadeOutDuration = fadeOutDuration;
            m_FadeOutCurve = fadeOutCurve;
            m_Curve = curve;
            m_IgnoreTimeScale = ignoreTimeScale;
            m_PlayStackingType = playStackingType;
            m_PlayPriority = playPriority;
            m_DataPriority = dataPriority;
            m_StandardConfigKey = standardConfigKey ?? string.Empty;
        }

        public string ShakeId => m_ShakeId ?? string.Empty;
        public int ShakeType => m_ShakeType;
        public int CameraShakePropertyConfig => m_CameraShakePropertyConfig;
        public float AngleVertical => m_AngleVertical;
        public float NoiseAngle => m_NoiseAngle;
        public float RadiusLength => m_RadiusLength;
        public float DistanceToPlane => m_DistanceToPlane;
        public float NoiseRatio => m_NoiseRatio;
        public float ShakeTotalTime => m_ShakeTotalTime;
        public float Frequency => m_Frequency;
        public float RollAmplitude => m_RollAmplitude;
        public float PitchAmplitude => m_PitchAmplitude;
        public float YawAmplitude => m_YawAmplitude;
        public CameraSpace ShakeCenterSpace => m_ShakeCenterSpace;
        public bool RealtimeVibration => m_RealtimeVibration;
        public int DissipationMode => m_DissipationMode;
        public float ImpactRadius => m_ImpactRadius;
        public float DissipationDistance => m_DissipationDistance;
        public CameraCurvePayload CustomCurve => m_CustomCurve;
        public float FadeInDuration => m_FadeInDuration;
        public CameraCurvePayload FadeInCurve => m_FadeInCurve;
        public float FadeOutDuration => m_FadeOutDuration;
        public CameraCurvePayload FadeOutCurve => m_FadeOutCurve;
        public CameraCurvePayload Curve => m_Curve;
        public bool IgnoreTimeScale => m_IgnoreTimeScale;
        public CameraEffectStackingType PlayStackingType => m_PlayStackingType;
        public int PlayPriority => m_PlayPriority;
        public int DataPriority => m_DataPriority;
        public string StandardConfigKey => m_StandardConfigKey ?? string.Empty;

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(ShakeId) || !float.IsFinite(AngleVertical) ||
                !float.IsFinite(NoiseAngle) || !float.IsFinite(RadiusLength) || RadiusLength < 0f ||
                !float.IsFinite(DistanceToPlane) || !float.IsFinite(NoiseRatio) || NoiseRatio < 0f ||
                !float.IsFinite(ShakeTotalTime) || ShakeTotalTime == 0f || !float.IsFinite(Frequency) || Frequency < 0f ||
                !float.IsFinite(RollAmplitude) || !float.IsFinite(PitchAmplitude) || !float.IsFinite(YawAmplitude) ||
                (byte)ShakeCenterSpace < (byte)CameraSpace.World ||
                (byte)ShakeCenterSpace > (byte)CameraSpace.Camera ||
                !float.IsFinite(ImpactRadius) || ImpactRadius < 0f ||
                !float.IsFinite(DissipationDistance) || DissipationDistance < 0f || !float.IsFinite(FadeInDuration) ||
                FadeInDuration < 0f || !float.IsFinite(FadeOutDuration) || FadeOutDuration < 0f ||
                FadeInDuration > 0f && FadeInCurve == null ||
                FadeOutDuration > 0f && FadeOutCurve == null || ShakeTotalTime > 0f && Curve == null ||
                DissipationMode == 5 && CustomCurve == null ||
                (byte)PlayStackingType < (byte)CameraEffectStackingType.Replace ||
                (byte)PlayStackingType > (byte)CameraEffectStackingType.HighestPriority)
                throw new InvalidOperationException($"{source} contains an invalid Camera Shake payload.");
            FadeInCurve?.RequireValid(source + ".FadeInCurve");
            FadeOutCurve?.RequireValid(source + ".FadeOutCurve");
            Curve?.RequireValid(source + ".Curve");
        }
    }
}
