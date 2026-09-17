using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraOverrideTrack", menuName = "3C/Character/Camera/Override Track")]
    public sealed class CameraOverrideTrackAsset : CameraEffectAsset
    {
        public const string SchemaVersion = "character-camera-override-track/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_TrackId = string.Empty;
        [SerializeField] CameraOverrideTrackSettings m_Settings = new CameraOverrideTrackSettings();
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;
        [SerializeField] bool m_ClearTracks;
        [SerializeField] string[] m_ClearTags = Array.Empty<string>();
        [SerializeField] float m_Duration = -1f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] float m_BlendInSeconds;
        [SerializeField] float m_BlendOutSeconds;
        [SerializeField] CameraCurveAsset m_BlendInCurve;
        [SerializeField] CameraCurveAsset m_BlendOutCurve;

        public string Schema => m_Schema ?? string.Empty;
        public string TrackId => m_TrackId ?? string.Empty;
        public CameraOverrideTrackSettings Settings => m_Settings;
        public int Priority => m_Priority;
        public override string EffectId => TrackId;
        public override int EffectPriority => Priority;
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
        public CameraCurveAsset BlendInCurve => m_BlendInCurve;
        public CameraCurveAsset BlendOutCurve => m_BlendOutCurve;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(TrackId) ||
                Settings == null || !float.IsFinite(Duration) || Duration == 0f || Duration < -1f ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) || !float.IsFinite(BlendInSeconds) ||
                !float.IsFinite(BlendOutSeconds) || BlendInSeconds < 0f || BlendOutSeconds < 0f ||
                string.IsNullOrWhiteSpace(Tag))
                throw new InvalidOperationException($"Camera Override Track Asset '{name}' is incomplete.");
            Settings.RequireValid($"{name}.Settings");
            RequireCurve(BlendInCurve, $"{name}.BlendInCurve");
            RequireCurve(BlendOutCurve, $"{name}.BlendOutCurve");
        }

        static void RequireCurve(CameraCurveAsset curve, string source)
        {
            if (!curve)
                throw new InvalidOperationException($"{source} is missing.");
            curve.RequireValid();
        }
    }
}
