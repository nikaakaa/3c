using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
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

        public CameraZoomPayload(
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
}
