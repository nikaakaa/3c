using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
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

        public CameraOverrideTrackPayload(
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
}
