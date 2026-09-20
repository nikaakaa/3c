using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraOverrideTrackSettingsPayload
    {
        [SerializeField] CameraOrbitPayload m_TopOrbit;
        [SerializeField] CameraOrbitPayload[] m_Orbits = Array.Empty<CameraOrbitPayload>();
        [SerializeField] float[] m_ScreenY = Array.Empty<float>();
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_AimOffset;
        [SerializeField] float m_FieldOfView;

        public CameraOverrideTrackSettingsPayload(
            CameraOrbitPayload topOrbit,
            CameraOrbitPayload[] orbits,
            float[] screenY,
            Vector3 followOffset,
            Vector3 aimOffset,
            float fieldOfView)
        {
            m_TopOrbit = topOrbit;
            m_Orbits = orbits ?? Array.Empty<CameraOrbitPayload>();
            m_ScreenY = screenY ?? Array.Empty<float>();
            m_FollowOffset = followOffset;
            m_AimOffset = aimOffset;
            m_FieldOfView = fieldOfView;
        }

        public CameraOrbitPayload TopOrbit => m_TopOrbit;
        public IReadOnlyList<CameraOrbitPayload> Orbits => m_Orbits ?? Array.Empty<CameraOrbitPayload>();
        public IReadOnlyList<float> ScreenY => m_ScreenY ?? Array.Empty<float>();
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 AimOffset => m_AimOffset;
        public float FieldOfView => m_FieldOfView;

        public void RequireValid(string source)
        {
            if (TopOrbit == null || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !Finite(FollowOffset) || !Finite(AimOffset) ||
                ScreenY.Count != 0 && ScreenY.Count != Orbits.Count)
                throw new InvalidOperationException($"{source} contains invalid Override track settings.");
            TopOrbit.RequireValid($"{source}.TopOrbit");
            for (int i = 0; i < Orbits.Count; i++)
            {
                if (Orbits[i] == null)
                    throw new InvalidOperationException($"{source}.Orbits[{i}] is missing.");
                Orbits[i].RequireValid($"{source}.Orbits[{i}]");
                if (ScreenY.Count != 0 && (!float.IsFinite(ScreenY[i]) || ScreenY[i] < 0f || ScreenY[i] > 1f))
                    throw new InvalidOperationException($"{source}.ScreenY[{i}] is invalid.");
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

[Serializable]
    public sealed class CameraOverrideTrackPayload
    {
        [SerializeField] string m_TrackId = string.Empty;
        [SerializeField] CameraOverrideTrackSettingsPayload m_Settings;
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
            CameraOverrideTrackSettingsPayload settings,
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
        public CameraOverrideTrackSettingsPayload Settings => m_Settings;
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

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(TrackId) || Settings == null || string.IsNullOrWhiteSpace(Tag) ||
                !float.IsFinite(Duration) || Duration == 0f || Duration < -1f ||
                (byte)TimeDomain < (byte)CameraTimeDomain.PresentationScaled ||
                (byte)TimeDomain > (byte)CameraTimeDomain.LocalAvatarScaled ||
                !float.IsFinite(BlendInSeconds) ||
                BlendInSeconds < 0f || !float.IsFinite(BlendOutSeconds) || BlendOutSeconds < 0f ||
                BlendInCurve == null || BlendOutCurve == null)
                throw new InvalidOperationException($"{source} contains an invalid Camera Override Track payload.");
            Settings.RequireValid(source + ".Settings");
            BlendInCurve.RequireValid(source + ".BlendInCurve");
            BlendOutCurve.RequireValid(source + ".BlendOutCurve");
            for (int i = 0; i < ClearTags.Count; i++)
                if (string.IsNullOrWhiteSpace(ClearTags[i]) || !string.Equals(ClearTags[i], ClearTags[i].Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException($"{source}.ClearTags[{i}] is invalid.");
        }
    }
}
