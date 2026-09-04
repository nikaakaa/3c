using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CharacterCameraProfile", menuName = "3C/Character/Camera/Profile")]
    public sealed class CharacterCameraProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-profile/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] CameraSequenceAsset m_DefaultSequence;
        [SerializeField] CameraSequenceAsset[] m_Sequences = Array.Empty<CameraSequenceAsset>();
        [SerializeField] CameraOverrideTrackAsset[] m_OverrideTracks = Array.Empty<CameraOverrideTrackAsset>();
        [SerializeField] CameraZoomAsset[] m_Zooms = Array.Empty<CameraZoomAsset>();
        [SerializeField] CameraStretchAsset[] m_Stretches = Array.Empty<CameraStretchAsset>();
        [SerializeField] CameraShakeAsset[] m_Shakes = Array.Empty<CameraShakeAsset>();
        [SerializeField] CameraShotAsset[] m_Shots = Array.Empty<CameraShotAsset>();
        [SerializeField] CameraCurveAsset[] m_Curves = Array.Empty<CameraCurveAsset>();
        [SerializeField] CameraOrbitDescriptor m_DefaultSphere = new CameraOrbitDescriptor(2f, 3f, 0.5f);
        [SerializeField] CameraOrbitDescriptor[] m_DefaultOrbitGroup = Array.Empty<CameraOrbitDescriptor>();
        [SerializeField] float m_NearClipPlane = 0.05f;
        [SerializeField] float m_FarClipPlane = 1000f;
        [SerializeField] float m_CameraLocateRadius = 3f;
        [SerializeField] float m_DefaultElevationAngle = 15f;
        [SerializeField] float m_DefaultFieldOfView = 60f;
        [SerializeField] float m_DefaultSmoothTime = 0.08f;
        [SerializeField] float m_RotationTransitionSeconds = 0.2f;
        [SerializeField] float m_ChangeAvatarTransitionSeconds = 0.2f;
        [SerializeField] CameraInputSettings m_Input = new CameraInputSettings();
        [SerializeField] CameraLockingSettings m_Locking = new CameraLockingSettings();
        [SerializeField] CameraCollisionSettings m_Collision = new CameraCollisionSettings();
        [SerializeField] CameraTargetSlot[] m_TargetSlots = Array.Empty<CameraTargetSlot>();

        public string Schema => m_Schema ?? string.Empty;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public CameraSequenceAsset DefaultSequence => m_DefaultSequence;
        public IReadOnlyList<CameraSequenceAsset> Sequences => m_Sequences ?? Array.Empty<CameraSequenceAsset>();
        public IReadOnlyList<CameraOverrideTrackAsset> OverrideTracks => m_OverrideTracks ?? Array.Empty<CameraOverrideTrackAsset>();
        public IReadOnlyList<CameraZoomAsset> Zooms => m_Zooms ?? Array.Empty<CameraZoomAsset>();
        public IReadOnlyList<CameraStretchAsset> Stretches => m_Stretches ?? Array.Empty<CameraStretchAsset>();
        public IReadOnlyList<CameraShakeAsset> Shakes => m_Shakes ?? Array.Empty<CameraShakeAsset>();
        public IReadOnlyList<CameraShotAsset> Shots => m_Shots ?? Array.Empty<CameraShotAsset>();
        public IReadOnlyList<CameraCurveAsset> Curves => m_Curves ?? Array.Empty<CameraCurveAsset>();
        public CameraOrbitDescriptor DefaultSphere => m_DefaultSphere;
        public IReadOnlyList<CameraOrbitDescriptor> DefaultOrbitGroup => m_DefaultOrbitGroup ?? Array.Empty<CameraOrbitDescriptor>();
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
        public IReadOnlyList<CameraTargetSlot> TargetSlots => m_TargetSlots ?? Array.Empty<CameraTargetSlot>();

        public string Revision
        {
            get
            {
                var value = new StringBuilder(SchemaVersion).Append('|').Append(ProfileId);
                value.Append('|').Append(DefaultSequence ? DefaultSequence.SequenceId : string.Empty);
                value.Append('|').Append(DefaultFieldOfView.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(DefaultSmoothTime.ToString("R", CultureInfo.InvariantCulture));
                AppendAssetIds(value, Sequences);
                AppendAssetIds(value, OverrideTracks);
                AppendAssetIds(value, Zooms);
                AppendAssetIds(value, Stretches);
                AppendAssetIds(value, Shakes);
                AppendAssetIds(value, Shots);
                AppendAssetIds(value, Curves);
                using SHA256 algorithm = SHA256.Create();
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
                var result = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ProfileId) ||
                !DefaultSequence || DefaultSphere == null || Input == null || Locking == null || Collision == null ||
                !float.IsFinite(NearClipPlane) || NearClipPlane < 0f || !float.IsFinite(FarClipPlane) ||
                FarClipPlane <= NearClipPlane || !float.IsFinite(CameraLocateRadius) || CameraLocateRadius <= 0f ||
                !float.IsFinite(DefaultElevationAngle) || !float.IsFinite(DefaultFieldOfView) || DefaultFieldOfView <= 0f ||
                !float.IsFinite(DefaultSmoothTime) || DefaultSmoothTime < 0f ||
                !float.IsFinite(RotationTransitionSeconds) || RotationTransitionSeconds < 0f ||
                !float.IsFinite(ChangeAvatarTransitionSeconds) || ChangeAvatarTransitionSeconds < 0f)
                throw new InvalidOperationException($"Character Camera Profile '{name}' is incomplete.");
            DefaultSequence.RequireValid();
            DefaultSphere.RequireValid($"{name}.DefaultSphere");
            Input.RequireValid($"{name}.Input");
            Locking.RequireValid($"{name}.Locking");
            Collision.RequireValid($"{name}.Collision");
            RequireAssets(Sequences, "Sequence");
            RequireAssets(OverrideTracks, "Override Track");
            RequireAssets(Zooms, "Zoom");
            RequireAssets(Stretches, "Stretch");
            RequireAssets(Shakes, "Shake");
            RequireAssets(Shots, "Shot");
            RequireAssets(Curves, "Curve");
            var slots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < TargetSlots.Count; i++)
            {
                CameraTargetSlot slot = TargetSlots[i];
                if (slot == null || !slots.Add(slot.SlotId))
                    throw new InvalidOperationException($"Character Camera Profile '{name}' target slot #{i} is missing or duplicated.");
                slot.RequireValid($"{name}.TargetSlots[{i}]");
            }
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            try
            {
                RequireValid();
                return true;
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return false;
            }
        }

        static void RequireAssets<T>(IReadOnlyList<T> values, string label) where T : UnityEngine.Object
        {
            var identities = new HashSet<int>();
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (!value || !identities.Add(value.GetInstanceID()))
                    throw new InvalidOperationException($"Character Camera Profile contains missing or duplicated {label} asset #{i}.");
                switch (value)
                {
                    case CameraSequenceAsset sequence: sequence.RequireValid(); break;
                    case CameraOverrideTrackAsset overrideTrack: overrideTrack.RequireValid(); break;
                    case CameraZoomAsset zoom: zoom.RequireValid(); break;
                    case CameraStretchAsset stretch: stretch.RequireValid(); break;
                    case CameraShakeAsset shake: shake.RequireValid(); break;
                    case CameraShotAsset shot: shot.RequireValid(); break;
                    case CameraCurveAsset curve: curve.RequireValid(); break;
                }
            }
        }

        static void AppendAssetIds<T>(StringBuilder value, IReadOnlyList<T> assets) where T : UnityEngine.Object
        {
            for (int i = 0; i < assets.Count; i++)
                value.Append('|').Append(assets[i] ? assets[i].name : string.Empty);
        }
    }
}
