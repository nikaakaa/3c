using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraShake", menuName = "3C/Character/Camera/Shake")]
    public sealed class CameraShakeAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-shake/v1";

        [SerializeField] string m_Schema = SchemaVersion;
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
        [SerializeField] CameraSpace m_ShakeCenterSpace = CameraSpace.LocalAvatar;
        [SerializeField] bool m_RealtimeVibration;
        [SerializeField] int m_DissipationMode;
        [SerializeField] float m_ImpactRadius;
        [SerializeField] float m_DissipationDistance;
        [SerializeField] string m_CustomCurveKey = string.Empty;
        [SerializeField] float m_FadeInDuration;
        [SerializeField] CameraCurveAsset m_FadeInCurve;
        [SerializeField] float m_FadeOutDuration;
        [SerializeField] CameraCurveAsset m_FadeOutCurve;
        [SerializeField] CameraCurveAsset m_Curve;
        [SerializeField] bool m_IgnoreTimeScale;
        [SerializeField] CameraEffectStackingType m_PlayStackingType = CameraEffectStackingType.Add;
        [SerializeField] int m_PlayPriority;
        [SerializeField] int m_DataPriority;
        [SerializeField] string m_StandardConfigKey = string.Empty;

        public string Schema => m_Schema ?? string.Empty;
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
        public string CustomCurveKey => m_CustomCurveKey ?? string.Empty;
        public float FadeInDuration => m_FadeInDuration;
        public CameraCurveAsset FadeInCurve => m_FadeInCurve;
        public float FadeOutDuration => m_FadeOutDuration;
        public CameraCurveAsset FadeOutCurve => m_FadeOutCurve;
        public CameraCurveAsset Curve => m_Curve;
        public bool IgnoreTimeScale => m_IgnoreTimeScale;
        public CameraEffectStackingType PlayStackingType => m_PlayStackingType;
        public int PlayPriority => m_PlayPriority;
        public int DataPriority => m_DataPriority;
        public string StandardConfigKey => m_StandardConfigKey ?? string.Empty;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ShakeId) ||
                !float.IsFinite(AngleVertical) || !float.IsFinite(NoiseAngle) || !float.IsFinite(RadiusLength) ||
                RadiusLength < 0f || !float.IsFinite(DistanceToPlane) || !float.IsFinite(NoiseRatio) ||
                NoiseRatio < 0f || !float.IsFinite(ShakeTotalTime) || ShakeTotalTime <= 0f ||
                !float.IsFinite(Frequency) || Frequency < 0f || !float.IsFinite(RollAmplitude) ||
                !float.IsFinite(PitchAmplitude) || !float.IsFinite(YawAmplitude) ||
                !Enum.IsDefined(typeof(CameraSpace), ShakeCenterSpace) || !float.IsFinite(ImpactRadius) ||
                ImpactRadius < 0f || !float.IsFinite(DissipationDistance) || DissipationDistance < 0f ||
                !float.IsFinite(FadeInDuration) || FadeInDuration < 0f || !float.IsFinite(FadeOutDuration) ||
                FadeOutDuration < 0f || !FadeInCurve || !FadeOutCurve || !Curve ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType))
                throw new InvalidOperationException($"Camera Shake Asset '{name}' is incomplete.");
            FadeInCurve.RequireValid();
            FadeOutCurve.RequireValid();
            Curve.RequireValid();
        }
    }
}
