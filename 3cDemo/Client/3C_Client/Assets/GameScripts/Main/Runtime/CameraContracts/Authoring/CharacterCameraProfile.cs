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
        public const string SchemaVersion = "character-camera-profile/v2";

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
        [SerializeField] float m_NearClipPlane = 0.05f;
        [SerializeField] float m_FarClipPlane = 1000f;
        [SerializeField] float m_CameraLocateRadius = 3f;
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
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float CameraLocateRadius => m_CameraLocateRadius;
        public float DefaultFieldOfView => m_DefaultFieldOfView;
        public float DefaultSmoothTime => m_DefaultSmoothTime;
        public float RotationTransitionSeconds => m_RotationTransitionSeconds;
        public float ChangeAvatarTransitionSeconds => m_ChangeAvatarTransitionSeconds;
        public CameraInputSettings Input => m_Input;
        public CameraLockingSettings Locking => m_Locking;
        public CameraCollisionSettings Collision => m_Collision;
        public IReadOnlyList<CameraTargetSlot> TargetSlots => m_TargetSlots ?? Array.Empty<CameraTargetSlot>();

        public bool HasSequence(string sequenceId)
        {
            if (string.IsNullOrWhiteSpace(sequenceId))
                return false;
            if (DefaultSequence && string.Equals(DefaultSequence.SequenceId, sequenceId, StringComparison.Ordinal))
                return true;
            for (int i = 0; i < Sequences.Count; i++)
                if (Sequences[i] && string.Equals(Sequences[i].SequenceId, sequenceId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public string Revision
        {
            get
            {
                var value = new StringBuilder(SchemaVersion).Append('|').Append(ProfileId);
                value.Append('|').Append(DefaultSequence ? DefaultSequence.SequenceId : string.Empty);
                value.Append('|').Append(CameraAssetRevision.Compute(DefaultSequence));
                CameraFrameOnePointByTrackStage defaultTrack = RequireDefaultTrack();
                for (int i = 0; i < defaultTrack.CameraOrbits.Count; i++)
                {
                    value.Append('|').Append(defaultTrack.CameraOrbits[i].Height.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(defaultTrack.CameraOrbits[i].Radius.ToString("R", CultureInfo.InvariantCulture));
                }
                for (int i = 0; i < defaultTrack.ScreenOffsets.Count; i++)
                {
                    value.Append('|').Append(defaultTrack.ScreenOffsets[i].x.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(defaultTrack.ScreenOffsets[i].y.ToString("R", CultureInfo.InvariantCulture));
                }
                value.Append('|').Append(defaultTrack.AspectRatio.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(defaultTrack.FieldOfView.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(defaultTrack.ElevationRatio.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(defaultTrack.PolarAngle.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(NearClipPlane.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(FarClipPlane.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(CameraLocateRadius.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(DefaultFieldOfView.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(DefaultSmoothTime.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(RotationTransitionSeconds.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(ChangeAvatarTransitionSeconds.ToString("R", CultureInfo.InvariantCulture));
                AppendInput(value, Input);
                AppendLocking(value, Locking);
                AppendCollision(value, Collision);
                AppendAssetIds(value, Sequences);
                AppendAssetIds(value, OverrideTracks);
                AppendAssetIds(value, Zooms);
                AppendAssetIds(value, Stretches);
                AppendAssetIds(value, Shakes);
                AppendAssetIds(value, Shots);
                AppendAssetIds(value, Curves);
                for (int i = 0; i < TargetSlots.Count; i++)
                {
                    CameraTargetSlot slot = TargetSlots[i];
                    value.Append('|').Append(slot?.SlotId ?? string.Empty);
                    value.Append('|').Append(slot?.AnchorKey ?? string.Empty);
                    value.Append('|').Append(slot?.AimPointKey ?? string.Empty);
                    value.Append('|').Append(slot?.PreferredBoneKey ?? string.Empty);
                    value.Append('|').Append(slot?.Required ?? false);
                    value.Append('|').Append((byte)(slot?.Space ?? CameraSpace.World));
                }
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
                !DefaultSequence || Input == null || Locking == null || Collision == null ||
                !float.IsFinite(NearClipPlane) || NearClipPlane < 0f || !float.IsFinite(FarClipPlane) ||
                FarClipPlane <= NearClipPlane || !float.IsFinite(CameraLocateRadius) || CameraLocateRadius <= 0f ||
                !float.IsFinite(DefaultFieldOfView) || DefaultFieldOfView <= 0f ||
                !float.IsFinite(DefaultSmoothTime) || DefaultSmoothTime < 0f ||
                !float.IsFinite(RotationTransitionSeconds) || RotationTransitionSeconds < 0f ||
                !float.IsFinite(ChangeAvatarTransitionSeconds) || ChangeAvatarTransitionSeconds < 0f)
                throw new InvalidOperationException($"Character Camera Profile '{name}' is incomplete.");
            DefaultSequence.RequireValid();
            RequireDefaultTrack();
            Input.RequireValid($"{name}.Input");
            Locking.RequireValid($"{name}.Locking");
            Collision.RequireValid($"{name}.Collision");
            RequireAssets(Sequences, "Sequence");
            RequireUniqueSequenceIdentity();
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
            var objectIdentities = new HashSet<int>();
            var resourceIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (!value || !objectIdentities.Add(value.GetInstanceID()))
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
                string resourceIdentity = ResolveResourceIdentity(value);
                if (resourceIdentities.TryGetValue(resourceIdentity, out string previousAssetName))
                    throw new InvalidOperationException(
                        $"Character Camera Profile contains duplicated {label} identity '{resourceIdentity}' on assets '{previousAssetName}' and '{value.name}'.");
                resourceIdentities.Add(resourceIdentity, value.name);
            }
        }

        static string ResolveResourceIdentity(UnityEngine.Object value)
        {
            string identity = value switch
            {
                CameraSequenceAsset sequence => sequence.SequenceId,
                CameraOverrideTrackAsset overrideTrack => overrideTrack.TrackId,
                CameraZoomAsset zoom => zoom.ZoomId,
                CameraStretchAsset stretch => stretch.StretchId,
                CameraShakeAsset shake => shake.ShakeId,
                CameraShotAsset shot => shot.ShotId,
                CameraCurveAsset curve => curve.CurveId,
                _ => string.Empty
            };
            if (string.IsNullOrWhiteSpace(identity))
                throw new InvalidOperationException($"Character Camera Profile resource '{value.name}' has no stable identity.");
            return identity;
        }

        void RequireUniqueSequenceIdentity()
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                CameraSequenceAsset sequence = Sequences[i];
                if (!string.Equals(DefaultSequence.SequenceId, sequence.SequenceId, StringComparison.Ordinal))
                    continue;
                throw new InvalidOperationException(
                    $"Character Camera Profile contains duplicated Sequence identity '{sequence.SequenceId}' on assets '{DefaultSequence.name}' and '{sequence.name}'.");
            }
        }

        static void AppendAssetIds<T>(StringBuilder value, IReadOnlyList<T> assets) where T : UnityEngine.Object
        {
            for (int i = 0; i < assets.Count; i++)
                value.Append('|').Append(assets[i] ? assets[i].name : string.Empty)
                    .Append('|').Append(CameraAssetRevision.Compute(assets[i]));
        }

        static void AppendInput(StringBuilder value, CameraInputSettings input)
        {
            value.Append('|').Append(input.Sensitivity.x.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.Sensitivity.y.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.PitchLimit.x.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.PitchLimit.y.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.DefaultResponseWeight.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.PitchResponseWeight.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(input.YawResponseWeight.ToString("R", CultureInfo.InvariantCulture));
        }

        static void AppendLocking(StringBuilder value, CameraLockingSettings locking)
        {
            value.Append('|').Append(locking.EnableTargetLock);
            value.Append('|').Append(locking.EnableBossLock);
            value.Append('|').Append(locking.TransitionSeconds.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(locking.Weight.ToString("R", CultureInfo.InvariantCulture));
        }

        static void AppendCollision(StringBuilder value, CameraCollisionSettings collision)
        {
            value.Append('|').Append(collision.Enabled);
            value.Append('|').Append(collision.LayerMask.value);
            value.Append('|').Append(collision.Radius.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(collision.NearClipPlane.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(collision.SmoothTime.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append((byte)collision.TimeDomain);
            value.Append('|').Append((byte)collision.TriggerMode);
        }

        static void AppendOrbit(StringBuilder value, CameraOrbitDescriptor orbit)
        {
            if (orbit == null)
            {
                value.Append("|missing-orbit");
                return;
            }
            value.Append('|').Append(orbit.Height.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(orbit.Radius.ToString("R", CultureInfo.InvariantCulture));
        }

        CameraFrameOnePointByTrackStage RequireDefaultTrack()
        {
            CameraFrameOnePointByTrackStage result = null;
            for (int i = 0; i < DefaultSequence.Stages.Count; i++)
            {
                if (DefaultSequence.Stages[i] is not CameraFrameOnePointByTrackStage track)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Character Camera Profile '{name}' default Sequence contains multiple track stages.");
                result = track;
            }
            return result ?? throw new InvalidOperationException(
                $"Character Camera Profile '{name}' default Sequence must own a CameraFrameOnePointByTrack stage.");
        }
    }
}
