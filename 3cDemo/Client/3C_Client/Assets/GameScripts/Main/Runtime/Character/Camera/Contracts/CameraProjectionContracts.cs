using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraSequenceInterruptPolicy : byte
    {
        BlendOut = 0,
        Cut = 1,
        HoldUntilSourceEnds = 2
    }

    public enum CameraResponseMode : byte
    {
        Full = 1,
        Suppressed = 2,
        Weighted = 3
    }

    public enum CameraEffectKind : byte
    {
        Override = 1,
        Zoom = 2,
        Stretch = 3,
        Shake = 4,
        Shot = 5
    }

    public readonly struct CameraSequenceRequest
    {
        public CameraSequenceRequest(
            string sequenceId,
            int priority,
            float weight,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            CameraSequenceInterruptPolicy interruptPolicy)
        {
            SequenceId = sequenceId ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            BlendInSeconds = Mathf.Max(0f, blendInSeconds);
            BlendOutSeconds = Mathf.Max(0f, blendOutSeconds);
            TargetKey = targetKey ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            InterruptPolicy = interruptPolicy;
        }

        public string SequenceId { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float BlendInSeconds { get; }
        public float BlendOutSeconds { get; }
        public string TargetKey { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public CameraSequenceInterruptPolicy InterruptPolicy { get; }
        public bool Active => Weight > 0f && !string.IsNullOrWhiteSpace(SequenceId);
    }

    public readonly struct CameraResponseRequest
    {
        public CameraResponseRequest(
            CameraResponseMode mode,
            float manualOrbitWeight,
            float pitchWeight,
            float yawWeight,
            int priority,
            float weight,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            Mode = mode;
            ManualOrbitWeight = Mathf.Clamp01(manualOrbitWeight);
            PitchWeight = Mathf.Clamp01(pitchWeight);
            YawWeight = Mathf.Clamp01(yawWeight);
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public CameraResponseMode Mode { get; }
        public float ManualOrbitWeight { get; }
        public float PitchWeight { get; }
        public float YawWeight { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Weight > 0f;

        public Vector2 Apply(Vector2 lookInput)
        {
            switch (Mode)
            {
                case CameraResponseMode.Suppressed:
                    return Vector2.zero;
                case CameraResponseMode.Weighted:
                    return new Vector2(
                        lookInput.x * ManualOrbitWeight * YawWeight,
                        lookInput.y * ManualOrbitWeight * PitchWeight);
                default:
                    return lookInput;
            }
        }
    }

    public readonly struct CameraEffectRequest
    {
        public CameraEffectRequest(
            CameraEffectKind kind,
            string resourceId,
            float weight,
            int priority,
            string sourceId,
            ulong generation,
            string eventId,
            ulong sourceActionInstanceId)
        {
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            Weight = Mathf.Max(0f, weight);
            Priority = priority;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            EventId = eventId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public CameraEffectKind Kind { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public int Priority { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public string EventId { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Weight > 0f && !string.IsNullOrWhiteSpace(ResourceId);
    }

    [Serializable]
    public sealed class CameraOrbitPayload
    {
        [SerializeField] float m_Height;
        [SerializeField] float m_Radius;
        [SerializeField] float m_ScreenY;

        internal CameraOrbitPayload(float height, float radius, float screenY)
        {
            m_Height = height;
            m_Radius = radius;
            m_ScreenY = screenY;
        }

        public float Height => m_Height;
        public float Radius => m_Radius;
        public float ScreenY => m_ScreenY;
    }

    [Serializable]
    public sealed class CameraSequenceStagePayload
    {
        [SerializeField] string m_StageId = string.Empty;
        [SerializeField] CameraSequenceStageKind m_Kind;
        [SerializeField] bool m_MakeContextDependent;
        [SerializeField] float m_PlayLength;
        [SerializeField] float m_EntityHeight;
        [SerializeField] float m_HeightRatio;
        [SerializeField] float m_FieldOfView;
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField] float m_AspectRatio;
        [SerializeField] float m_Radius;
        [SerializeField] CameraOrbitPayload[] m_CameraOrbits = Array.Empty<CameraOrbitPayload>();
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;
        [SerializeField] float m_MinPlayerHeightRatio;
        [SerializeField] float m_MaxPlayerHeightRatio;
        [SerializeField] float m_Pitch;
        [SerializeField] Vector2 m_MainHorizontalOffset;
        [SerializeField] Vector2 m_SubHorizontalOffset;
        [SerializeField] float m_MainVerticalOffset;
        [SerializeField] Vector2 m_TargetVerticalOffset;
        [SerializeField] Vector2 m_PitchRange;
        [SerializeField] float m_PlayerHeight;
        [SerializeField] string m_BeginCameraDataId = string.Empty;
        [SerializeField] float m_HeightOffset;
        [SerializeField] Vector2 m_AngleRange;
        [SerializeField] LayerMask m_LayerMask;
        [SerializeField] string m_DeltaHeightToPitchCurveId = string.Empty;
        [SerializeField] string m_FallbackStageId = string.Empty;
        [SerializeField] string m_MainTargetSlotId = string.Empty;
        [SerializeField] string[] m_SubTargetSlotIds = Array.Empty<string>();
        [SerializeField] string m_FramePolicyId = string.Empty;
        [SerializeField] string m_RotationPolicyId = string.Empty;
        [SerializeField] string m_FixedPolicyId = string.Empty;
        [SerializeField] string m_ActiveChannel = string.Empty;
        [SerializeField] string m_CollisionDataId = string.Empty;
        [SerializeField] bool m_HandleLineOfSightCollision;
        [SerializeField] float m_NearClipPlane;
        [SerializeField] Vector3 m_RotationOffset;
        [SerializeField] bool m_FlipForward;
        [SerializeField] Vector3 m_OverrideRotation;
        [SerializeField] Vector3 m_Rotation;
        [SerializeField] bool m_UseRelativeYaw;
        [SerializeField] string m_LastCameraDataId = string.Empty;

        internal CameraSequenceStagePayload(
            string stageId,
            CameraSequenceStageKind kind,
            bool makeContextDependent,
            float playLength,
            float entityHeight,
            float heightRatio,
            float fieldOfView,
            Vector2 screenOffset,
            float aspectRatio,
            float radius,
            CameraOrbitPayload[] cameraOrbits,
            float elevationRatio,
            float polarAngle,
            float minPlayerHeightRatio,
            float maxPlayerHeightRatio,
            float pitch,
            Vector2 mainHorizontalOffset,
            Vector2 subHorizontalOffset,
            float mainVerticalOffset,
            Vector2 targetVerticalOffset,
            Vector2 pitchRange,
            float playerHeight,
            string beginCameraDataId,
            float heightOffset,
            Vector2 angleRange,
            LayerMask layerMask,
            string deltaHeightToPitchCurveId,
            string fallbackStageId,
            string mainTargetSlotId,
            string[] subTargetSlotIds,
            string framePolicyId,
            string rotationPolicyId,
            string fixedPolicyId,
            string activeChannel,
            string collisionDataId,
            bool handleLineOfSightCollision,
            float nearClipPlane,
            Vector3 rotationOffset,
            bool flipForward,
            Vector3 overrideRotation,
            Vector3 rotation,
            bool useRelativeYaw,
            string lastCameraDataId)
        {
            m_StageId = stageId ?? string.Empty;
            m_Kind = kind;
            m_MakeContextDependent = makeContextDependent;
            m_PlayLength = playLength;
            m_EntityHeight = entityHeight;
            m_HeightRatio = heightRatio;
            m_FieldOfView = fieldOfView;
            m_ScreenOffset = screenOffset;
            m_AspectRatio = aspectRatio;
            m_Radius = radius;
            m_CameraOrbits = cameraOrbits ?? Array.Empty<CameraOrbitPayload>();
            m_ElevationRatio = elevationRatio;
            m_PolarAngle = polarAngle;
            m_MinPlayerHeightRatio = minPlayerHeightRatio;
            m_MaxPlayerHeightRatio = maxPlayerHeightRatio;
            m_Pitch = pitch;
            m_MainHorizontalOffset = mainHorizontalOffset;
            m_SubHorizontalOffset = subHorizontalOffset;
            m_MainVerticalOffset = mainVerticalOffset;
            m_TargetVerticalOffset = targetVerticalOffset;
            m_PitchRange = pitchRange;
            m_PlayerHeight = playerHeight;
            m_BeginCameraDataId = beginCameraDataId ?? string.Empty;
            m_HeightOffset = heightOffset;
            m_AngleRange = angleRange;
            m_LayerMask = layerMask;
            m_DeltaHeightToPitchCurveId = deltaHeightToPitchCurveId ?? string.Empty;
            m_FallbackStageId = fallbackStageId ?? string.Empty;
            m_MainTargetSlotId = mainTargetSlotId ?? string.Empty;
            m_SubTargetSlotIds = subTargetSlotIds ?? Array.Empty<string>();
            m_FramePolicyId = framePolicyId ?? string.Empty;
            m_RotationPolicyId = rotationPolicyId ?? string.Empty;
            m_FixedPolicyId = fixedPolicyId ?? string.Empty;
            m_ActiveChannel = activeChannel ?? string.Empty;
            m_CollisionDataId = collisionDataId ?? string.Empty;
            m_HandleLineOfSightCollision = handleLineOfSightCollision;
            m_NearClipPlane = nearClipPlane;
            m_RotationOffset = rotationOffset;
            m_FlipForward = flipForward;
            m_OverrideRotation = overrideRotation;
            m_Rotation = rotation;
            m_UseRelativeYaw = useRelativeYaw;
            m_LastCameraDataId = lastCameraDataId ?? string.Empty;
        }

        public string StageId => m_StageId ?? string.Empty;
        public CameraSequenceStageKind Kind => m_Kind;
        public bool MakeContextDependent => m_MakeContextDependent;
        public float PlayLength => m_PlayLength;
        public float EntityHeight => m_EntityHeight;
        public float HeightRatio => m_HeightRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float AspectRatio => m_AspectRatio;
        public float Radius => m_Radius;
        public IReadOnlyList<CameraOrbitPayload> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraOrbitPayload>();
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;
        public float MinPlayerHeightRatio => m_MinPlayerHeightRatio;
        public float MaxPlayerHeightRatio => m_MaxPlayerHeightRatio;
        public float Pitch => m_Pitch;
        public Vector2 MainHorizontalOffset => m_MainHorizontalOffset;
        public Vector2 SubHorizontalOffset => m_SubHorizontalOffset;
        public float MainVerticalOffset => m_MainVerticalOffset;
        public Vector2 TargetVerticalOffset => m_TargetVerticalOffset;
        public Vector2 PitchRange => m_PitchRange;
        public float PlayerHeight => m_PlayerHeight;
        public string BeginCameraDataId => m_BeginCameraDataId ?? string.Empty;
        public float HeightOffset => m_HeightOffset;
        public Vector2 AngleRange => m_AngleRange;
        public LayerMask LayerMask => m_LayerMask;
        public string DeltaHeightToPitchCurveId => m_DeltaHeightToPitchCurveId ?? string.Empty;
        public string FallbackStageId => m_FallbackStageId ?? string.Empty;
        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();
        public string FramePolicyId => m_FramePolicyId ?? string.Empty;
        public string RotationPolicyId => m_RotationPolicyId ?? string.Empty;
        public string FixedPolicyId => m_FixedPolicyId ?? string.Empty;
        public string ActiveChannel => m_ActiveChannel ?? string.Empty;
        public string CollisionDataId => m_CollisionDataId ?? string.Empty;
        public bool HandleLineOfSightCollision => m_HandleLineOfSightCollision;
        public float NearClipPlane => m_NearClipPlane;
        public Vector3 RotationOffset => m_RotationOffset;
        public bool FlipForward => m_FlipForward;
        public Vector3 OverrideRotation => m_OverrideRotation;
        public Vector3 Rotation => m_Rotation;
        public bool UseRelativeYaw => m_UseRelativeYaw;
        public string LastCameraDataId => m_LastCameraDataId ?? string.Empty;
    }

    [Serializable]
    public sealed class CameraSequencePayload
    {
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] CameraSequenceStagePayload[] m_Stages = Array.Empty<CameraSequenceStagePayload>();

        internal CameraSequencePayload(string sequenceId, CameraTimeDomain timeDomain, CameraSequenceStagePayload[] stages)
        {
            m_SequenceId = sequenceId ?? string.Empty;
            m_TimeDomain = timeDomain;
            m_Stages = stages ?? Array.Empty<CameraSequenceStagePayload>();
        }

        public string SequenceId => m_SequenceId ?? string.Empty;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public IReadOnlyList<CameraSequenceStagePayload> Stages => m_Stages ?? Array.Empty<CameraSequenceStagePayload>();
    }

    [Serializable]
    public sealed class CameraOverrideTrackPayload
    {
        [SerializeField] string m_TrackId = string.Empty;
        [SerializeField] CameraOverrideTrackSettings m_Settings;
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;
        [SerializeField] bool m_ClearTracks;
        [SerializeField] string[] m_ClearTags = Array.Empty<string>();
        [SerializeField] float m_Duration;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] float m_BlendInSeconds;
        [SerializeField] float m_BlendOutSeconds;
        [SerializeField] CameraCurvePayload m_BlendInCurve;
        [SerializeField] CameraCurvePayload m_BlendOutCurve;

        internal CameraOverrideTrackPayload(
            string trackId,
            CameraOverrideTrackSettings settings,
            int priority,
            string tag,
            bool clearTracks,
            string[] clearTags,
            float duration,
            CameraTimeDomain timeDomain,
            bool ignoreLocalAvatar,
            bool ignoreOwnerTimeScale,
            bool ignoreWorldTimeScale,
            float blendInSeconds,
            float blendOutSeconds,
            CameraCurvePayload blendInCurve,
            CameraCurvePayload blendOutCurve)
        {
            m_TrackId = trackId ?? string.Empty;
            m_Settings = settings;
            m_Priority = priority;
            m_Tag = tag ?? string.Empty;
            m_ClearTracks = clearTracks;
            m_ClearTags = clearTags ?? Array.Empty<string>();
            m_Duration = duration;
            m_TimeDomain = timeDomain;
            m_IgnoreLocalAvatar = ignoreLocalAvatar;
            m_IgnoreOwnerTimeScale = ignoreOwnerTimeScale;
            m_IgnoreWorldTimeScale = ignoreWorldTimeScale;
            m_BlendInSeconds = blendInSeconds;
            m_BlendOutSeconds = blendOutSeconds;
            m_BlendInCurve = blendInCurve;
            m_BlendOutCurve = blendOutCurve;
        }

        public string TrackId => m_TrackId ?? string.Empty;
        public CameraOverrideTrackSettings Settings => m_Settings;
        public int Priority => m_Priority;
        public string Tag => m_Tag ?? string.Empty;
        public bool ClearTracks => m_ClearTracks;
        public IReadOnlyList<string> ClearTags => m_ClearTags ?? Array.Empty<string>();
        public float Duration => m_Duration;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public bool IgnoreLocalAvatar => m_IgnoreLocalAvatar;
        public bool IgnoreOwnerTimeScale => m_IgnoreOwnerTimeScale;
        public bool IgnoreWorldTimeScale => m_IgnoreWorldTimeScale;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public CameraCurvePayload BlendInCurve => m_BlendInCurve;
        public CameraCurvePayload BlendOutCurve => m_BlendOutCurve;
    }

    [Serializable]
    public sealed class CameraZoomPayload
    {
        [SerializeField] string m_ZoomId = string.Empty;
        [SerializeField] CameraCurvePayload m_StartCurve;
        [SerializeField] CameraCurvePayload m_EndCurve;
        [SerializeField] int m_DataPriority;
        [SerializeField] bool m_IgnorePriorityInEndTime;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_LastTime;
        [SerializeField] float m_StartTime;
        [SerializeField] CameraEffectStackingType m_StackingType;
        [SerializeField] float m_FieldOfView;
        [SerializeField] CameraFovVariationType m_FovVariationType;
        [SerializeField] float m_DelayTime;
        [SerializeField] float m_EndTime;
        [SerializeField] CameraEffectStackingType m_PlayStackingType;

        internal CameraZoomPayload(
            string zoomId,
            CameraCurvePayload startCurve,
            CameraCurvePayload endCurve,
            int dataPriority,
            bool ignorePriorityInEndTime,
            bool ignoreWorldTimeScale,
            bool ignoreLocalAvatar,
            bool ignoreOwnerTimeScale,
            float lastTime,
            float startTime,
            CameraEffectStackingType stackingType,
            float fieldOfView,
            CameraFovVariationType fovVariationType,
            float delayTime,
            float endTime,
            CameraEffectStackingType playStackingType)
        {
            m_ZoomId = zoomId ?? string.Empty;
            m_StartCurve = startCurve;
            m_EndCurve = endCurve;
            m_DataPriority = dataPriority;
            m_IgnorePriorityInEndTime = ignorePriorityInEndTime;
            m_IgnoreWorldTimeScale = ignoreWorldTimeScale;
            m_IgnoreLocalAvatar = ignoreLocalAvatar;
            m_IgnoreOwnerTimeScale = ignoreOwnerTimeScale;
            m_LastTime = lastTime;
            m_StartTime = startTime;
            m_StackingType = stackingType;
            m_FieldOfView = fieldOfView;
            m_FovVariationType = fovVariationType;
            m_DelayTime = delayTime;
            m_EndTime = endTime;
            m_PlayStackingType = playStackingType;
        }

        public string ZoomId => m_ZoomId ?? string.Empty;
        public CameraCurvePayload StartCurve => m_StartCurve;
        public CameraCurvePayload EndCurve => m_EndCurve;
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
    }

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

        internal CameraStretchPayload(
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
    }

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
        [SerializeField] string m_CustomCurveKey = string.Empty;
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

        internal CameraShakePayload(
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
            string customCurveKey,
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
            m_CustomCurveKey = customCurveKey ?? string.Empty;
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
        public string CustomCurveKey => m_CustomCurveKey ?? string.Empty;
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
    }

    [Serializable]
    public sealed class CameraShotBlendPayload
    {
        [SerializeField] float m_Duration;
        [SerializeField] CameraCurvePayload m_Curve;
        [SerializeField] bool m_UseCoreSpace;
        [SerializeField] bool m_UseDelta;

        internal CameraShotBlendPayload(CameraShotBlendSettings source)
        {
            m_Duration = source.Duration;
            m_Curve = source.Curve.Compile();
            m_UseCoreSpace = source.UseCoreSpace;
            m_UseDelta = source.UseDelta;
        }

        internal CameraShotBlendPayload(float duration, CameraCurvePayload curve, bool useCoreSpace, bool useDelta)
        {
            m_Duration = duration;
            m_Curve = curve;
            m_UseCoreSpace = useCoreSpace;
            m_UseDelta = useDelta;
        }

        public float Duration => m_Duration;
        public CameraCurvePayload Curve => m_Curve;
        public bool UseCoreSpace => m_UseCoreSpace;
        public bool UseDelta => m_UseDelta;
    }

    [Serializable]
    public sealed class CameraShotPayload
    {
        [SerializeField] string m_ShotId = string.Empty;
        [SerializeField] string m_CinePrefabPath = string.Empty;
        [SerializeField] string m_FollowTargetSlotId = string.Empty;
        [SerializeField] string m_LookAtTargetSlotId = string.Empty;
        [SerializeField] float m_NearClipPlane;
        [SerializeField] float m_FarClipPlane;
        [SerializeField] float m_Duration;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] bool m_IgnoreCameraCollision;
        [SerializeField] bool m_ApplyEntityTimeScale;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_LookAtOffset;
        [SerializeField] Vector3 m_OffsetRotation;
        [SerializeField] float m_FieldOfView;
        [SerializeField] CameraShotBlendPayload m_BlendIn;
        [SerializeField] CameraShotBlendPayload m_BlendOut;
        [SerializeField] bool m_BlendWithIgnoreLookAtTarget;
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;

        internal CameraShotPayload(
            string shotId,
            string cinePrefabPath,
            string followTargetSlotId,
            string lookAtTargetSlotId,
            float nearClipPlane,
            float farClipPlane,
            float duration,
            CameraTimeDomain timeDomain,
            bool ignoreCameraCollision,
            bool applyEntityTimeScale,
            Vector3 followOffset,
            Vector3 lookAtOffset,
            Vector3 offsetRotation,
            float fieldOfView,
            CameraShotBlendPayload blendIn,
            CameraShotBlendPayload blendOut,
            bool blendWithIgnoreLookAtTarget,
            int priority,
            string tag)
        {
            m_ShotId = shotId ?? string.Empty;
            m_CinePrefabPath = cinePrefabPath ?? string.Empty;
            m_FollowTargetSlotId = followTargetSlotId ?? string.Empty;
            m_LookAtTargetSlotId = lookAtTargetSlotId ?? string.Empty;
            m_NearClipPlane = nearClipPlane;
            m_FarClipPlane = farClipPlane;
            m_Duration = duration;
            m_TimeDomain = timeDomain;
            m_IgnoreCameraCollision = ignoreCameraCollision;
            m_ApplyEntityTimeScale = applyEntityTimeScale;
            m_FollowOffset = followOffset;
            m_LookAtOffset = lookAtOffset;
            m_OffsetRotation = offsetRotation;
            m_FieldOfView = fieldOfView;
            m_BlendIn = blendIn;
            m_BlendOut = blendOut;
            m_BlendWithIgnoreLookAtTarget = blendWithIgnoreLookAtTarget;
            m_Priority = priority;
            m_Tag = tag ?? string.Empty;
        }

        public string ShotId => m_ShotId ?? string.Empty;
        public string CinePrefabPath => m_CinePrefabPath ?? string.Empty;
        public string FollowTargetSlotId => m_FollowTargetSlotId ?? string.Empty;
        public string LookAtTargetSlotId => m_LookAtTargetSlotId ?? string.Empty;
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float Duration => m_Duration;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public bool IgnoreCameraCollision => m_IgnoreCameraCollision;
        public bool ApplyEntityTimeScale => m_ApplyEntityTimeScale;
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 LookAtOffset => m_LookAtOffset;
        public Vector3 OffsetRotation => m_OffsetRotation;
        public float FieldOfView => m_FieldOfView;
        public CameraShotBlendPayload BlendIn => m_BlendIn;
        public CameraShotBlendPayload BlendOut => m_BlendOut;
        public bool BlendWithIgnoreLookAtTarget => m_BlendWithIgnoreLookAtTarget;
        public int Priority => m_Priority;
        public string Tag => m_Tag ?? string.Empty;
    }

    [Serializable]
    public sealed class CameraTargetSlotPayload
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] CameraSpace m_Space;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;
        [SerializeField] bool m_Required;

        internal CameraTargetSlotPayload(CameraTargetSlot source)
        {
            m_SlotId = source.SlotId;
            m_Space = source.Space;
            m_AnchorKey = source.AnchorKey;
            m_AimPointKey = source.AimPointKey;
            m_PreferredBoneKey = source.PreferredBoneKey;
            m_Required = source.Required;
        }

        public string SlotId => m_SlotId ?? string.Empty;
        public CameraSpace Space => m_Space;
        public string AnchorKey => m_AnchorKey ?? string.Empty;
        public string AimPointKey => m_AimPointKey ?? string.Empty;
        public string PreferredBoneKey => m_PreferredBoneKey ?? string.Empty;
        public bool Required => m_Required;
    }

    [Serializable]
    public sealed class CharacterCameraProjectionPayload
    {
        public const string SchemaVersion = "character-camera-projection/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_ProfileRevision = string.Empty;
        [SerializeField] CameraSequencePayload m_DefaultSequence;
        [SerializeField] CameraSequencePayload[] m_Sequences = Array.Empty<CameraSequencePayload>();
        [SerializeField] CameraOverrideTrackPayload[] m_OverrideTracks = Array.Empty<CameraOverrideTrackPayload>();
        [SerializeField] CameraZoomPayload[] m_Zooms = Array.Empty<CameraZoomPayload>();
        [SerializeField] CameraStretchPayload[] m_Stretches = Array.Empty<CameraStretchPayload>();
        [SerializeField] CameraShakePayload[] m_Shakes = Array.Empty<CameraShakePayload>();
        [SerializeField] CameraShotPayload[] m_Shots = Array.Empty<CameraShotPayload>();
        [SerializeField] CameraCurvePayload[] m_Curves = Array.Empty<CameraCurvePayload>();
        [SerializeField] CameraOrbitPayload m_DefaultSphere;
        [SerializeField] CameraOrbitPayload[] m_DefaultOrbitGroup = Array.Empty<CameraOrbitPayload>();
        [SerializeField] float m_NearClipPlane;
        [SerializeField] float m_FarClipPlane;
        [SerializeField] float m_CameraLocateRadius;
        [SerializeField] float m_DefaultElevationAngle;
        [SerializeField] float m_DefaultFieldOfView;
        [SerializeField] float m_DefaultSmoothTime;
        [SerializeField] float m_RotationTransitionSeconds;
        [SerializeField] float m_ChangeAvatarTransitionSeconds;
        [SerializeField] CameraInputSettings m_Input;
        [SerializeField] CameraLockingSettings m_Locking;
        [SerializeField] CameraCollisionSettings m_Collision;
        [SerializeField] CameraTargetSlotPayload[] m_TargetSlots = Array.Empty<CameraTargetSlotPayload>();

        internal CharacterCameraProjectionPayload(
            CharacterCameraProfile profile,
            CameraSequencePayload defaultSequence,
            CameraSequencePayload[] sequences,
            CameraOverrideTrackPayload[] overrideTracks,
            CameraZoomPayload[] zooms,
            CameraStretchPayload[] stretches,
            CameraShakePayload[] shakes,
            CameraShotPayload[] shots,
            CameraCurvePayload[] curves,
            CameraOrbitPayload defaultSphere,
            CameraOrbitPayload[] defaultOrbitGroup,
            CameraTargetSlotPayload[] targetSlots)
        {
            m_ProfileId = profile.ProfileId;
            m_ProfileRevision = profile.Revision;
            m_DefaultSequence = defaultSequence;
            m_Sequences = sequences ?? Array.Empty<CameraSequencePayload>();
            m_OverrideTracks = overrideTracks ?? Array.Empty<CameraOverrideTrackPayload>();
            m_Zooms = zooms ?? Array.Empty<CameraZoomPayload>();
            m_Stretches = stretches ?? Array.Empty<CameraStretchPayload>();
            m_Shakes = shakes ?? Array.Empty<CameraShakePayload>();
            m_Shots = shots ?? Array.Empty<CameraShotPayload>();
            m_Curves = curves ?? Array.Empty<CameraCurvePayload>();
            m_DefaultSphere = defaultSphere;
            m_DefaultOrbitGroup = defaultOrbitGroup ?? Array.Empty<CameraOrbitPayload>();
            m_NearClipPlane = profile.NearClipPlane;
            m_FarClipPlane = profile.FarClipPlane;
            m_CameraLocateRadius = profile.CameraLocateRadius;
            m_DefaultElevationAngle = profile.DefaultElevationAngle;
            m_DefaultFieldOfView = profile.DefaultFieldOfView;
            m_DefaultSmoothTime = profile.DefaultSmoothTime;
            m_RotationTransitionSeconds = profile.RotationTransitionSeconds;
            m_ChangeAvatarTransitionSeconds = profile.ChangeAvatarTransitionSeconds;
            m_Input = new CameraInputSettings(profile.Input);
            m_Locking = new CameraLockingSettings(profile.Locking);
            m_Collision = new CameraCollisionSettings(profile.Collision);
            m_TargetSlots = targetSlots ?? Array.Empty<CameraTargetSlotPayload>();
        }

        public string Schema => m_Schema ?? string.Empty;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public string ProfileRevision => m_ProfileRevision ?? string.Empty;
        public CameraSequencePayload DefaultSequence => m_DefaultSequence;
        public IReadOnlyList<CameraSequencePayload> Sequences => m_Sequences ?? Array.Empty<CameraSequencePayload>();
        public IReadOnlyList<CameraOverrideTrackPayload> OverrideTracks => m_OverrideTracks ?? Array.Empty<CameraOverrideTrackPayload>();
        public IReadOnlyList<CameraZoomPayload> Zooms => m_Zooms ?? Array.Empty<CameraZoomPayload>();
        public IReadOnlyList<CameraStretchPayload> Stretches => m_Stretches ?? Array.Empty<CameraStretchPayload>();
        public IReadOnlyList<CameraShakePayload> Shakes => m_Shakes ?? Array.Empty<CameraShakePayload>();
        public IReadOnlyList<CameraShotPayload> Shots => m_Shots ?? Array.Empty<CameraShotPayload>();
        public IReadOnlyList<CameraCurvePayload> Curves => m_Curves ?? Array.Empty<CameraCurvePayload>();
        public CameraOrbitPayload DefaultSphere => m_DefaultSphere;
        public IReadOnlyList<CameraOrbitPayload> DefaultOrbitGroup => m_DefaultOrbitGroup ?? Array.Empty<CameraOrbitPayload>();
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float CameraLocateRadius => m_CameraLocateRadius;
        public float DefaultElevationAngle => m_DefaultElevationAngle;
        public float DefaultFieldOfView => m_DefaultFieldOfView;
        public float DefaultSmoothTime => m_DefaultSmoothTime;
        public float RotationTransitionSeconds => m_RotationTransitionSeconds;
        public float ChangeAvatarTransitionSeconds => m_ChangeAvatarTransitionSeconds;
        public CameraInputSettings Input => m_Input;
        public CameraLockingSettings Locking => m_Locking;
        public CameraCollisionSettings Collision => m_Collision;
        public IReadOnlyList<CameraTargetSlotPayload> TargetSlots => m_TargetSlots ?? Array.Empty<CameraTargetSlotPayload>();

        public bool TryGetSequence(string sequenceId, out CameraSequencePayload payload)
        {
            if (DefaultSequence != null && string.Equals(DefaultSequence.SequenceId, sequenceId, StringComparison.Ordinal))
            {
                payload = DefaultSequence;
                return true;
            }
            for (int i = 0; i < Sequences.Count; i++)
            {
                CameraSequencePayload candidate = Sequences[i];
                if (candidate != null && string.Equals(candidate.SequenceId, sequenceId, StringComparison.Ordinal))
                {
                    payload = candidate;
                    return true;
                }
            }
            payload = null;
            return false;
        }

        public bool TryGetOverride(string resourceId, out CameraOverrideTrackPayload payload) =>
            TryFind(OverrideTracks, resourceId, value => value.TrackId, out payload);

        public bool TryGetZoom(string resourceId, out CameraZoomPayload payload) =>
            TryFind(Zooms, resourceId, value => value.ZoomId, out payload);

        public bool TryGetStretch(string resourceId, out CameraStretchPayload payload) =>
            TryFind(Stretches, resourceId, value => value.StretchId, out payload);

        public bool TryGetShake(string resourceId, out CameraShakePayload payload) =>
            TryFind(Shakes, resourceId, value => value.ShakeId, out payload);

        public bool TryGetShot(string resourceId, out CameraShotPayload payload) =>
            TryFind(Shots, resourceId, value => value.ShotId, out payload);

        static bool TryFind<T>(
            IReadOnlyList<T> values,
            string resourceId,
            Func<T, string> identity,
            out T result) where T : class
        {
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (value != null && string.Equals(identity(value), resourceId, StringComparison.Ordinal))
                {
                    result = value;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ProfileId) ||
                string.IsNullOrWhiteSpace(ProfileRevision) || DefaultSequence == null || DefaultSphere == null ||
                Input == null || Locking == null || Collision == null || !float.IsFinite(NearClipPlane) ||
                NearClipPlane < 0f || !float.IsFinite(FarClipPlane) || FarClipPlane <= NearClipPlane ||
                !float.IsFinite(CameraLocateRadius) || CameraLocateRadius <= 0f ||
                !float.IsFinite(DefaultElevationAngle) || !float.IsFinite(DefaultFieldOfView) || DefaultFieldOfView <= 0f ||
                !float.IsFinite(DefaultSmoothTime) || DefaultSmoothTime < 0f ||
                !float.IsFinite(RotationTransitionSeconds) || RotationTransitionSeconds < 0f ||
                !float.IsFinite(ChangeAvatarTransitionSeconds) || ChangeAvatarTransitionSeconds < 0f)
                throw new InvalidOperationException("Character Camera Projection payload is incomplete.");
        }
    }

    public readonly struct CameraTargetSnapshot
    {
        public CameraTargetSnapshot(
            string key,
            Vector3 anchorPoint,
            Vector3 aimPoint,
            Vector3 velocity,
            bool valid)
        {
            Key = key ?? string.Empty;
            AnchorPoint = anchorPoint;
            AimPoint = aimPoint;
            Velocity = velocity;
            Valid = valid;
        }

        public string Key { get; }
        public Vector3 AnchorPoint { get; }
        public Vector3 AimPoint { get; }
        public Vector3 Velocity { get; }
        public bool Valid { get; }
    }

    public readonly struct CameraFrameInput
    {
        public CameraFrameInput(
            Vector3 bodyPosition,
            Quaternion bodyRotation,
            Vector2 lookInput,
            float scaledDeltaSeconds,
            float unscaledDeltaSeconds,
            float presentationDeltaSeconds,
            float ownerTimeScale,
            float localAvatarTimeScale,
            bool paused,
            bool resetHistory,
            IReadOnlyList<CameraTargetSnapshot> targets)
        {
            BodyPosition = bodyPosition;
            BodyRotation = bodyRotation;
            LookInput = lookInput;
            ScaledDeltaSeconds = RequireDelta(scaledDeltaSeconds, nameof(scaledDeltaSeconds));
            UnscaledDeltaSeconds = RequireDelta(unscaledDeltaSeconds, nameof(unscaledDeltaSeconds));
            PresentationDeltaSeconds = RequireDelta(presentationDeltaSeconds, nameof(presentationDeltaSeconds));
            OwnerTimeScale = RequireScale(ownerTimeScale, nameof(ownerTimeScale));
            LocalAvatarTimeScale = RequireScale(localAvatarTimeScale, nameof(localAvatarTimeScale));
            Paused = paused;
            ResetHistory = resetHistory;
            Targets = targets ?? Array.Empty<CameraTargetSnapshot>();
        }

        public Vector3 BodyPosition { get; }
        public Quaternion BodyRotation { get; }
        public Vector2 LookInput { get; }
        public float ScaledDeltaSeconds { get; }
        public float UnscaledDeltaSeconds { get; }
        public float PresentationDeltaSeconds { get; }
        public float OwnerTimeScale { get; }
        public float LocalAvatarTimeScale { get; }
        public bool Paused { get; }
        public bool ResetHistory { get; }
        public IReadOnlyList<CameraTargetSnapshot> Targets { get; }

        public float Delta(CameraTimeDomain domain)
        {
            switch (domain)
            {
                case CameraTimeDomain.PresentationUnscaled:
                    return UnscaledDeltaSeconds;
                case CameraTimeDomain.OwnerScaled:
                    return ScaledDeltaSeconds * OwnerTimeScale;
                case CameraTimeDomain.LocalAvatarScaled:
                    return ScaledDeltaSeconds * LocalAvatarTimeScale;
                default:
                    return PresentationDeltaSeconds;
            }
        }

        static float RequireDelta(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }

        static float RequireScale(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }

    public enum CameraEffectStage : byte
    {
        Sequence = 1,
        Override = 2,
        Zoom = 3,
        Stretch = 4,
        Shake = 5,
        Shot = 6,
        Collision = 7
    }

    public readonly struct CameraEffectContribution
    {
        public CameraEffectContribution(
            CameraEffectStage stage,
            string resourceId,
            float weight,
            float remainingSeconds,
            int priority,
            bool active)
        {
            Stage = stage;
            ResourceId = resourceId ?? string.Empty;
            Weight = weight;
            RemainingSeconds = remainingSeconds;
            Priority = priority;
            Active = active;
        }

        public CameraEffectStage Stage { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public float RemainingSeconds { get; }
        public int Priority { get; }
        public bool Active { get; }
    }

    public readonly struct CameraFramePlan
    {
        public CameraFramePlan(
            Vector3 followPoint,
            Vector3 aimPoint,
            float fieldOfView,
            float nearClipPlane,
            float farClipPlane,
            Vector2 lookDelta,
            float orbitYaw,
            float orbitPitch,
            float orbitRadius,
            string sequenceId,
            string sourceId,
            ulong sourceActionInstanceId,
            float blendProgress,
            bool resetHistory,
            bool valid,
            float radiusScale = 1f,
            Vector3 cameraOffset = default,
            float rollDegrees = 0f)
        {
            FollowPoint = followPoint;
            AimPoint = aimPoint;
            FieldOfView = fieldOfView;
            NearClipPlane = nearClipPlane;
            FarClipPlane = farClipPlane;
            LookDelta = lookDelta;
            OrbitYaw = orbitYaw;
            OrbitPitch = orbitPitch;
            OrbitRadius = orbitRadius;
            SequenceId = sequenceId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            BlendProgress = Mathf.Clamp01(blendProgress);
            ResetHistory = resetHistory;
            RadiusScale = radiusScale;
            CameraOffset = cameraOffset;
            RollDegrees = rollDegrees;
            Valid = valid;
        }

        public Vector3 FollowPoint { get; }
        public Vector3 AimPoint { get; }
        public float FieldOfView { get; }
        public float NearClipPlane { get; }
        public float FarClipPlane { get; }
        public Vector2 LookDelta { get; }
        public float OrbitYaw { get; }
        public float OrbitPitch { get; }
        public float OrbitRadius { get; }
        public string SequenceId { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public float BlendProgress { get; }
        public bool ResetHistory { get; }
        public float RadiusScale { get; }
        public Vector3 CameraOffset { get; }
        public float RollDegrees { get; }
        public bool Valid { get; }

        public static CameraFramePlan Invalid => default;

        public CameraFramePlan WithTargets(Vector3 followPoint, Vector3 aimPoint) => new CameraFramePlan(
            followPoint,
            aimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            cameraOffset: CameraOffset,
            rollDegrees: RollDegrees);

        public CameraFramePlan WithFieldOfView(float fieldOfView) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            fieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithLookDelta(Vector2 lookDelta) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            lookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithResetHistory(bool resetHistory) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            resetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithOrbit(float yaw, float pitch, float radius) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            yaw,
            pitch,
            radius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            RollDegrees);

        public CameraFramePlan WithCameraOffset(Vector3 cameraOffset) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            cameraOffset,
            RollDegrees);

        public CameraFramePlan WithRoll(float rollDegrees) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            RadiusScale,
            CameraOffset,
            rollDegrees);

        public CameraFramePlan WithRadiusScale(float radiusScale) => new CameraFramePlan(
            FollowPoint,
            AimPoint,
            FieldOfView,
            NearClipPlane,
            FarClipPlane,
            LookDelta,
            OrbitYaw,
            OrbitPitch,
            OrbitRadius,
            SequenceId,
            SourceId,
            SourceActionInstanceId,
            BlendProgress,
            ResetHistory,
            Valid,
            radiusScale,
            CameraOffset,
            RollDegrees);
    }

    public readonly struct CameraRigResult
    {
        public CameraRigResult(CameraBasisSnapshot basis, Vector3 position, Quaternion rotation, float fieldOfView, bool valid)
        {
            Basis = basis;
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
            Valid = valid;
        }

        public CameraBasisSnapshot Basis { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float FieldOfView { get; }
        public bool Valid { get; }
    }
}
