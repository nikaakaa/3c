using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraTimeDomain : byte
    {
        PresentationScaled = 1,
        PresentationUnscaled = 2,
        OwnerScaled = 3,
        LocalAvatarScaled = 4
    }

    public enum CameraSpace : byte
    {
        World = 1,
        Core = 2,
        LocalAvatar = 3,
        Camera = 4
    }

    public enum CameraSequenceStageKind : byte
    {
        FrameOnePointByHeight = 1,
        FrameOnePointByScreenOffset = 2,
        FrameOnePointByTrack = 3,
        FrameTwoPointsChat = 4,
        FrameMultiplePointsChat = 5,
        FrameOneEntity = 6,
        FrameTwoEntities = 7,
        FrameMultipleEntities = 8,
        FixedInCoreSpace = 9,
        HandleCameraVolume = 10,
        RotationEulerOffset = 11,
        RotationLast = 12
    }

    public enum CameraEffectStackingType : byte
    {
        Replace = 1,
        Add = 2,
        HighestPriority = 3
    }

    public enum CameraFovVariationType : byte
    {
        Absolute = 1,
        Additive = 2,
        Multiplicative = 3
    }

    public enum CameraShakeDirection : byte
    {
        Camera = 1,
        Impact = 2,
        Directional = 3
    }

    [Serializable]
    public sealed class CameraCurveKey
    {
        [SerializeField] float m_Time;
        [SerializeField] float m_Value;
        [SerializeField] float m_InTangent;
        [SerializeField] float m_OutTangent;

        public CameraCurveKey() { }

        public CameraCurveKey(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
        }

        public float Time => m_Time;
        public float Value => m_Value;
        public float InTangent => m_InTangent;
        public float OutTangent => m_OutTangent;
    }

    [Serializable]
    public sealed class CameraCurvePayload
    {
        [SerializeField] string m_CurveId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CameraCurveKey[] m_Keys = Array.Empty<CameraCurveKey>();
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] float m_MinValue;
        [SerializeField] float m_MaxValue;
        [SerializeField] string m_Unit = string.Empty;

        internal CameraCurvePayload(
            string curveId,
            string revision,
            CameraCurveKey[] keys,
            CameraTimeDomain timeDomain,
            float minValue,
            float maxValue,
            string unit)
        {
            m_CurveId = curveId ?? string.Empty;
            m_Revision = revision ?? string.Empty;
            m_Keys = keys ?? Array.Empty<CameraCurveKey>();
            m_TimeDomain = timeDomain;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Unit = unit ?? string.Empty;
        }

        public string CurveId => m_CurveId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public IReadOnlyList<CameraCurveKey> Keys => m_Keys ?? Array.Empty<CameraCurveKey>();
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public float MinValue => m_MinValue;
        public float MaxValue => m_MaxValue;
        public string Unit => m_Unit ?? string.Empty;

        public float Evaluate(float time)
        {
            IReadOnlyList<CameraCurveKey> keys = Keys;
            if (keys.Count == 0)
                throw new InvalidOperationException($"Camera Curve '{CurveId}' has no keys.");
            if (keys.Count == 1 || time <= keys[0].Time)
                return keys[0].Value;
            int last = keys.Count - 1;
            if (time >= keys[last].Time)
                return keys[last].Value;
            for (int index = 1; index < keys.Count; index++)
            {
                CameraCurveKey right = keys[index];
                if (time > right.Time)
                    continue;
                CameraCurveKey left = keys[index - 1];
                float duration = right.Time - left.Time;
                float t = duration <= 0f ? 1f : Mathf.Clamp01((time - left.Time) / duration);
                float t2 = t * t;
                float t3 = t2 * t;
                float h00 = 2f * t3 - 3f * t2 + 1f;
                float h10 = t3 - 2f * t2 + t;
                float h01 = -2f * t3 + 3f * t2;
                float h11 = t3 - t2;
                return h00 * left.Value +
                       h10 * duration * left.OutTangent +
                       h01 * right.Value +
                       h11 * duration * right.InTangent;
            }
            return keys[last].Value;
        }

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(CurveId) || string.IsNullOrWhiteSpace(Revision) ||
                Keys.Count == 0 || !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(MinValue) || !float.IsFinite(MaxValue) || MinValue > MaxValue ||
                string.IsNullOrWhiteSpace(Unit))
            {
                throw new InvalidOperationException($"{source} contains an invalid Camera Curve payload.");
            }
            float previousTime = float.NegativeInfinity;
            for (int i = 0; i < Keys.Count; i++)
            {
                CameraCurveKey key = Keys[i];
                if (key == null || !float.IsFinite(key.Time) || !float.IsFinite(key.Value) ||
                    !float.IsFinite(key.InTangent) || !float.IsFinite(key.OutTangent) ||
                    key.Time <= previousTime)
                {
                    throw new InvalidOperationException($"{source} Curve key #{i} is invalid or not ordered.");
                }
                previousTime = key.Time;
            }
        }
    }

    [CreateAssetMenu(fileName = "CameraCurve", menuName = "3C/Character/Camera/Curve")]
    public sealed class CameraCurveAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-curve/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_CurveId = string.Empty;
        [SerializeField] AnimationCurve m_Curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f));
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] float m_MinValue;
        [SerializeField] float m_MaxValue = 1f;
        [SerializeField] string m_Unit = "normalized";

        public string Schema => m_Schema ?? string.Empty;
        public string CurveId => m_CurveId ?? string.Empty;
        public AnimationCurve Curve => Copy(m_Curve);
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public float MinValue => m_MinValue;
        public float MaxValue => m_MaxValue;
        public string Unit => m_Unit ?? string.Empty;
        public string Revision => ComputeRevision();
        public string DependencyIdentity => $"{CurveId}@{Revision}";

        public void Configure(
            string curveId,
            AnimationCurve curve,
            CameraTimeDomain timeDomain,
            float minValue,
            float maxValue,
            string unit)
        {
            m_Schema = SchemaVersion;
            m_CurveId = RequireIdentity(curveId, nameof(curveId));
            m_Curve = Copy(curve);
            m_TimeDomain = timeDomain;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Unit = RequireIdentity(unit, nameof(unit));
            RequireValid();
        }

        public CameraCurvePayload Compile()
        {
            RequireValid();
            Keyframe[] keys = m_Curve.keys;
            var payload = new CameraCurveKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Camera Curve '{name}' does not support weighted tangents.");
                payload[i] = new CameraCurveKey(key.time, key.value, key.inTangent, key.outTangent);
            }
            return new CameraCurvePayload(
                CurveId,
                Revision,
                payload,
                TimeDomain,
                MinValue,
                MaxValue,
                Unit);
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(CurveId) || m_Curve == null || m_Curve.length == 0 ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(MinValue) || !float.IsFinite(MaxValue) || MinValue > MaxValue ||
                string.IsNullOrWhiteSpace(Unit))
            {
                throw new InvalidOperationException($"Camera Curve Asset '{name}' is incomplete.");
            }
            Keyframe[] keys = m_Curve.keys;
            float previousTime = float.NegativeInfinity;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (key.weightedMode != WeightedMode.None || !float.IsFinite(key.time) ||
                    !float.IsFinite(key.value) || !float.IsFinite(key.inTangent) ||
                    !float.IsFinite(key.outTangent) || key.time <= previousTime)
                {
                    throw new InvalidOperationException($"Camera Curve Asset '{name}' key #{i} is invalid.");
                }
                previousTime = key.time;
            }
        }

        public void RegenerateIdentity() => m_CurveId = Guid.NewGuid().ToString("N");

        string ComputeRevision()
        {
            var value = new StringBuilder(SchemaVersion);
            value.Append('|').Append(CurveId);
            value.Append('|').Append((byte)TimeDomain);
            value.Append('|').Append(MinValue.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(MaxValue.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(Unit);
            if (m_Curve != null)
            {
                Keyframe[] keys = m_Curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe key = keys[i];
                    value.Append('|').Append(key.time.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.value.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                }
            }
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        static AnimationCurve Copy(AnimationCurve source)
        {
            if (source == null)
                return null;
            return new AnimationCurve(source.keys)
            {
                preWrapMode = WrapMode.Clamp,
                postWrapMode = WrapMode.Clamp
            };
        }

        static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Camera identity is missing or contains surrounding whitespace.", parameterName);
            return value.Trim();
        }
    }

    [Serializable]
    public sealed class CameraOrbitDescriptor
    {
        [SerializeField] float m_Height;
        [SerializeField, Min(0f)] float m_Radius = 3f;
        [SerializeField] float m_ScreenY = 0.5f;

        public float Height => m_Height;
        public float Radius => m_Radius;
        public float ScreenY => m_ScreenY;

        public CameraOrbitDescriptor() { }

        public CameraOrbitDescriptor(float height, float radius, float screenY)
        {
            m_Height = height;
            m_Radius = radius;
            m_ScreenY = screenY;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f ||
                !float.IsFinite(ScreenY) || ScreenY < 0f || ScreenY > 1f)
                throw new InvalidOperationException($"{source} contains an invalid Camera orbit.");
        }
    }

    [Serializable]
    public sealed class CameraTargetSlot
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] CameraSpace m_Space = CameraSpace.World;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;
        [SerializeField] bool m_Required = true;

        public string SlotId => m_SlotId ?? string.Empty;
        public CameraSpace Space => m_Space;
        public string AnchorKey => m_AnchorKey ?? string.Empty;
        public string AimPointKey => m_AimPointKey ?? string.Empty;
        public string PreferredBoneKey => m_PreferredBoneKey ?? string.Empty;
        public bool Required => m_Required;

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(SlotId) || !Enum.IsDefined(typeof(CameraSpace), Space) ||
                string.IsNullOrWhiteSpace(AnchorKey) && string.IsNullOrWhiteSpace(AimPointKey) &&
                string.IsNullOrWhiteSpace(PreferredBoneKey))
                throw new InvalidOperationException($"{source} contains an invalid Camera target slot.");
        }
    }

    [Serializable]
    public sealed class CameraLockingSettings
    {
        [SerializeField] bool m_EnableTargetLock;
        [SerializeField] bool m_EnableBossLock;
        [SerializeField] float m_TransitionSeconds = 0.2f;
        [SerializeField] float m_Weight = 1f;

        public bool EnableTargetLock => m_EnableTargetLock;
        public bool EnableBossLock => m_EnableBossLock;
        public float TransitionSeconds => m_TransitionSeconds;
        public float Weight => m_Weight;

        public CameraLockingSettings() { }

        internal CameraLockingSettings(CameraLockingSettings source)
        {
            m_EnableTargetLock = source.EnableTargetLock;
            m_EnableBossLock = source.EnableBossLock;
            m_TransitionSeconds = source.TransitionSeconds;
            m_Weight = source.Weight;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(TransitionSeconds) || TransitionSeconds < 0f ||
                !float.IsFinite(Weight) || Weight < 0f || Weight > 1f)
                throw new InvalidOperationException($"{source} contains invalid Camera locking settings.");
        }
    }

    [Serializable]
    public sealed class CameraInputSettings
    {
        [SerializeField] Vector2 m_Sensitivity = new Vector2(0.12f, 0.0025f);
        [SerializeField] Vector2 m_PitchLimit = new Vector2(-70f, 70f);
        [SerializeField] float m_DefaultResponseWeight = 1f;
        [SerializeField] float m_PitchResponseWeight = 1f;
        [SerializeField] float m_YawResponseWeight = 1f;

        public Vector2 Sensitivity => m_Sensitivity;
        public Vector2 PitchLimit => m_PitchLimit;
        public float DefaultResponseWeight => m_DefaultResponseWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;

        public CameraInputSettings() { }

        internal CameraInputSettings(CameraInputSettings source)
        {
            m_Sensitivity = source.Sensitivity;
            m_PitchLimit = source.PitchLimit;
            m_DefaultResponseWeight = source.DefaultResponseWeight;
            m_PitchResponseWeight = source.PitchResponseWeight;
            m_YawResponseWeight = source.YawResponseWeight;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Sensitivity.x) || !float.IsFinite(Sensitivity.y) ||
                !float.IsFinite(PitchLimit.x) || !float.IsFinite(PitchLimit.y) ||
                PitchLimit.x >= PitchLimit.y || !float.IsFinite(DefaultResponseWeight) ||
                !float.IsFinite(PitchResponseWeight) || !float.IsFinite(YawResponseWeight) ||
                DefaultResponseWeight < 0f || DefaultResponseWeight > 1f ||
                PitchResponseWeight < 0f || PitchResponseWeight > 1f ||
                YawResponseWeight < 0f || YawResponseWeight > 1f)
                throw new InvalidOperationException($"{source} contains invalid Camera input settings.");
        }
    }

    [Serializable]
    public sealed class CameraCollisionSettings
    {
        [SerializeField] bool m_Enabled = true;
        [SerializeField] LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] float m_Radius = 0.2f;
        [SerializeField, Min(0f)] float m_NearClipPlane = 0.05f;
        [SerializeField, Min(0f)] float m_SmoothTime = 0.08f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;

        public bool Enabled => m_Enabled;
        public LayerMask LayerMask => m_LayerMask;
        public float Radius => m_Radius;
        public float NearClipPlane => m_NearClipPlane;
        public float SmoothTime => m_SmoothTime;
        public CameraTimeDomain TimeDomain => m_TimeDomain;

        public CameraCollisionSettings() { }

        internal CameraCollisionSettings(CameraCollisionSettings source)
        {
            m_Enabled = source.Enabled;
            m_LayerMask = source.LayerMask;
            m_Radius = source.Radius;
            m_NearClipPlane = source.NearClipPlane;
            m_SmoothTime = source.SmoothTime;
            m_TimeDomain = source.TimeDomain;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Radius) || Radius < 0f || !float.IsFinite(NearClipPlane) ||
                NearClipPlane < 0f || !float.IsFinite(SmoothTime) || SmoothTime < 0f ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain))
                throw new InvalidOperationException($"{source} contains invalid Camera collision settings.");
        }
    }

    [Serializable]
    public abstract class CameraSequenceStage
    {
        [SerializeField] string m_StageId = string.Empty;
        [SerializeField] bool m_MakeContextDependent;
        [SerializeField] float m_PlayLength = -1f;

        public string StageId => m_StageId ?? string.Empty;
        public bool MakeContextDependent => m_MakeContextDependent;
        public float PlayLength => m_PlayLength;
        public abstract CameraSequenceStageKind Kind { get; }

        public void ConfigureIdentity(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                throw new ArgumentException("Camera Sequence stage identity is missing.", nameof(stageId));
            m_StageId = stageId.Trim();
        }

        public virtual void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(StageId) || !Enum.IsDefined(typeof(CameraSequenceStageKind), Kind) ||
                !float.IsFinite(PlayLength) || PlayLength == 0f || PlayLength < -1f)
                throw new InvalidOperationException($"{source} contains an invalid Camera Sequence stage.");
        }
    }

    [Serializable]
    public sealed class CameraFrameOnePointByHeightStage : CameraSequenceStage
    {
        [SerializeField] float m_EntityHeight = 1.8f;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] Vector2 m_ScreenOffset;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByHeight;
        public float EntityHeight => m_EntityHeight;
        public float HeightRatio => m_HeightRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(EntityHeight) || EntityHeight <= 0f || !float.IsFinite(HeightRatio) ||
                HeightRatio <= 0f || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y))
                throw new InvalidOperationException($"{source} contains invalid single-point height framing.");
        }
    }

    [Serializable]
    public sealed class CameraFrameOnePointByScreenOffsetStage : CameraSequenceStage
    {
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField, Min(0f)] float m_Radius = 3f;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByScreenOffset;
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float Radius => m_Radius;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(AspectRatio) || AspectRatio <= 0f || !float.IsFinite(FieldOfView) ||
                FieldOfView <= 0f || !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y) ||
                !float.IsFinite(Radius) || Radius <= 0f)
                throw new InvalidOperationException($"{source} contains invalid single-point screen framing.");
        }
    }

    [Serializable]
    public sealed class CameraFrameOnePointByTrackStage : CameraSequenceStage
    {
        [SerializeField] CameraOrbitDescriptor[] m_CameraOrbits = Array.Empty<CameraOrbitDescriptor>();
        [SerializeField] Vector2 m_ScreenOffset;
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] float m_ElevationRatio;
        [SerializeField] float m_PolarAngle;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOnePointByTrack;
        public IReadOnlyList<CameraOrbitDescriptor> CameraOrbits => m_CameraOrbits ?? Array.Empty<CameraOrbitDescriptor>();
        public Vector2 ScreenOffset => m_ScreenOffset;
        public float AspectRatio => m_AspectRatio;
        public float FieldOfView => m_FieldOfView;
        public float ElevationRatio => m_ElevationRatio;
        public float PolarAngle => m_PolarAngle;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (CameraOrbits.Count == 0 || !float.IsFinite(AspectRatio) || AspectRatio <= 0f ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(ElevationRatio) ||
                !float.IsFinite(PolarAngle) || !float.IsFinite(ScreenOffset.x) || !float.IsFinite(ScreenOffset.y))
                throw new InvalidOperationException($"{source} contains invalid single-point track framing.");
            for (int i = 0; i < CameraOrbits.Count; i++)
                CameraOrbits[i]?.RequireValid($"{source}.CameraOrbits[{i}]");
        }
    }

    [Serializable]
    public sealed class CameraFrameTwoPointsStage : CameraSequenceStage
    {
        [SerializeField] float m_AspectRatio = 1.7777778f;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_MinPlayerHeightRatio;
        [SerializeField] float m_MaxPlayerHeightRatio = 1f;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] float m_Pitch;
        [SerializeField] Vector2 m_MainHorizontalOffset;
        [SerializeField] Vector2 m_SubHorizontalOffset;
        [SerializeField] float m_MainVerticalOffset;
        [SerializeField] Vector2 m_TargetVerticalOffset;
        [SerializeField] Vector2 m_PitchRange = new Vector2(-70f, 70f);
        [SerializeField] float m_PlayerHeight = 1.8f;
        [SerializeField] string m_BeginCameraDataId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameTwoPointsChat;
        public float AspectRatio => m_AspectRatio;
        public float HeightRatio => m_HeightRatio;
        public float MinPlayerHeightRatio => m_MinPlayerHeightRatio;
        public float MaxPlayerHeightRatio => m_MaxPlayerHeightRatio;
        public float FieldOfView => m_FieldOfView;
        public float Pitch => m_Pitch;
        public Vector2 MainHorizontalOffset => m_MainHorizontalOffset;
        public Vector2 SubHorizontalOffset => m_SubHorizontalOffset;
        public float MainVerticalOffset => m_MainVerticalOffset;
        public Vector2 TargetVerticalOffset => m_TargetVerticalOffset;
        public Vector2 PitchRange => m_PitchRange;
        public float PlayerHeight => m_PlayerHeight;
        public string BeginCameraDataId => m_BeginCameraDataId ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(AspectRatio) || AspectRatio <= 0f || !float.IsFinite(HeightRatio) ||
                HeightRatio <= 0f || !float.IsFinite(MinPlayerHeightRatio) ||
                !float.IsFinite(MaxPlayerHeightRatio) || MinPlayerHeightRatio > MaxPlayerHeightRatio ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || !float.IsFinite(Pitch) ||
                !float.IsFinite(PitchRange.x) || !float.IsFinite(PitchRange.y) || PitchRange.x >= PitchRange.y ||
                !float.IsFinite(PlayerHeight) || PlayerHeight <= 0f || string.IsNullOrWhiteSpace(BeginCameraDataId))
                throw new InvalidOperationException($"{source} contains invalid two-point framing.");
        }
    }

    [Serializable]
    public sealed class CameraFrameMultiplePointsStage : CameraSequenceStage
    {
        [SerializeField, Min(0f)] float m_Radius = 3f;
        [SerializeField] float m_HeightOffset;
        [SerializeField] float m_HeightRatio = 0.5f;
        [SerializeField] float m_PlayerHeight = 1.8f;
        [SerializeField] Vector2 m_AngleRange = new Vector2(-70f, 70f);
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
        [SerializeField] string m_BeginCameraDataId = string.Empty;
        [SerializeField] CameraCurveAsset m_DeltaHeightToPitch;
        [SerializeField] CameraFrameTwoPointsStage m_FallbackTwoPoints;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameMultiplePointsChat;
        public float Radius => m_Radius;
        public float HeightOffset => m_HeightOffset;
        public float HeightRatio => m_HeightRatio;
        public float PlayerHeight => m_PlayerHeight;
        public Vector2 AngleRange => m_AngleRange;
        public float FieldOfView => m_FieldOfView;
        public LayerMask LayerMask => m_LayerMask;
        public string BeginCameraDataId => m_BeginCameraDataId ?? string.Empty;
        public CameraCurveAsset DeltaHeightToPitch => m_DeltaHeightToPitch;
        public CameraFrameTwoPointsStage FallbackTwoPoints => m_FallbackTwoPoints;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(Radius) || Radius <= 0f || !float.IsFinite(HeightOffset) ||
                !float.IsFinite(HeightRatio) || HeightRatio <= 0f || !float.IsFinite(PlayerHeight) ||
                PlayerHeight <= 0f || !float.IsFinite(AngleRange.x) || !float.IsFinite(AngleRange.y) ||
                AngleRange.x >= AngleRange.y || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                string.IsNullOrWhiteSpace(BeginCameraDataId) || !DeltaHeightToPitch || FallbackTwoPoints == null)
                throw new InvalidOperationException($"{source} contains invalid multiple-point framing.");
            DeltaHeightToPitch.RequireValid();
            FallbackTwoPoints.RequireValid($"{source}.FallbackTwoPoints");
        }
    }

    [Serializable]
    public class CameraEntityFrameStage : CameraSequenceStage
    {
        [SerializeField] string m_MainTargetSlotId = string.Empty;
        [SerializeField] string[] m_SubTargetSlotIds = Array.Empty<string>();
        [SerializeField] string m_FramePolicyId = string.Empty;
        [SerializeField] string m_RotationPolicyId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOneEntity;
        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();
        public string FramePolicyId => m_FramePolicyId ?? string.Empty;
        public string RotationPolicyId => m_RotationPolicyId ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(MainTargetSlotId) || string.IsNullOrWhiteSpace(FramePolicyId) ||
                string.IsNullOrWhiteSpace(RotationPolicyId))
                throw new InvalidOperationException($"{source} contains invalid entity framing bindings.");
        }
    }

    [Serializable]
    public sealed class CameraTwoEntitiesFrameStage : CameraEntityFrameStage
    {
        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameTwoEntities;
    }

    [Serializable]
    public sealed class CameraMultipleEntitiesFrameStage : CameraEntityFrameStage
    {
        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameMultipleEntities;
    }

    [Serializable]
    public sealed class CameraFixedInCoreStage : CameraSequenceStage
    {
        [SerializeField] string m_FixedPolicyId = string.Empty;
        [SerializeField] string m_ActiveChannel = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FixedInCoreSpace;
        public string FixedPolicyId => m_FixedPolicyId ?? string.Empty;
        public string ActiveChannel => m_ActiveChannel ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(FixedPolicyId) || string.IsNullOrWhiteSpace(ActiveChannel))
                throw new InvalidOperationException($"{source} contains invalid fixed-space framing bindings.");
        }
    }

    [Serializable]
    public sealed class CameraHandleVolumeStage : CameraSequenceStage
    {
        [SerializeField] string m_CollisionDataId = string.Empty;
        [SerializeField] bool m_HandleLineOfSightCollision = true;
        [SerializeField] float m_NearClipPlane = 0.05f;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.HandleCameraVolume;
        public string CollisionDataId => m_CollisionDataId ?? string.Empty;
        public bool HandleLineOfSightCollision => m_HandleLineOfSightCollision;
        public float NearClipPlane => m_NearClipPlane;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(CollisionDataId) || !float.IsFinite(NearClipPlane) || NearClipPlane < 0f)
                throw new InvalidOperationException($"{source} contains invalid camera volume handling.");
        }
    }

    [Serializable]
    public sealed class CameraRotationEulerOffsetStage : CameraSequenceStage
    {
        [SerializeField] Vector3 m_Offset;
        [SerializeField] bool m_FlipForward;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.RotationEulerOffset;
        public Vector3 Offset => m_Offset;
        public bool FlipForward => m_FlipForward;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(Offset.x) || !float.IsFinite(Offset.y) || !float.IsFinite(Offset.z))
                throw new InvalidOperationException($"{source} contains invalid Euler rotation offsets.");
        }
    }

    [Serializable]
    public sealed class CameraRotationLastStage : CameraSequenceStage
    {
        [SerializeField] Vector3 m_OverrideRotation;
        [SerializeField] Vector3 m_Rotation;
        [SerializeField] bool m_UseRelativeYaw;
        [SerializeField] string m_LastCameraDataId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.RotationLast;
        public Vector3 OverrideRotation => m_OverrideRotation;
        public Vector3 Rotation => m_Rotation;
        public bool UseRelativeYaw => m_UseRelativeYaw;
        public string LastCameraDataId => m_LastCameraDataId ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(OverrideRotation.x) || !float.IsFinite(OverrideRotation.y) ||
                !float.IsFinite(OverrideRotation.z) || !float.IsFinite(Rotation.x) ||
                !float.IsFinite(Rotation.y) || !float.IsFinite(Rotation.z) ||
                string.IsNullOrWhiteSpace(LastCameraDataId))
                throw new InvalidOperationException($"{source} contains invalid last-rotation settings.");
        }
    }

    [CreateAssetMenu(fileName = "CameraSequence", menuName = "3C/Character/Camera/Sequence")]
    public sealed class CameraSequenceAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-sequence/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeField] CameraSequenceStage[] m_Stages = Array.Empty<CameraSequenceStage>();
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;

        public string Schema => m_Schema ?? string.Empty;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public IReadOnlyList<CameraSequenceStage> Stages => m_Stages ?? Array.Empty<CameraSequenceStage>();
        public CameraTimeDomain TimeDomain => m_TimeDomain;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(SequenceId) || Stages.Count == 0 ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain))
                throw new InvalidOperationException($"Camera Sequence Asset '{name}' is incomplete.");
            for (int i = 0; i < Stages.Count; i++)
            {
                CameraSequenceStage stage = Stages[i];
                if (stage == null)
                    throw new InvalidOperationException($"Camera Sequence Asset '{name}' stage #{i} is missing.");
                stage.RequireValid($"{name}.Stages[{i}]");
            }
        }
    }

    [Serializable]
    public sealed class CameraOverrideTrackSettings
    {
        [SerializeField] CameraOrbitDescriptor m_TopOrbit = new CameraOrbitDescriptor(2f, 0.2f, 0.5f);
        [SerializeField] CameraOrbitDescriptor[] m_Orbits = Array.Empty<CameraOrbitDescriptor>();
        [SerializeField] float[] m_ScreenY = Array.Empty<float>();
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_AimOffset;
        [SerializeField] float m_FieldOfView = 60f;

        public CameraOrbitDescriptor TopOrbit => m_TopOrbit;
        public IReadOnlyList<CameraOrbitDescriptor> Orbits => m_Orbits ?? Array.Empty<CameraOrbitDescriptor>();
        public IReadOnlyList<float> ScreenY => m_ScreenY ?? Array.Empty<float>();
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 AimOffset => m_AimOffset;
        public float FieldOfView => m_FieldOfView;

        public CameraOverrideTrackSettings() { }

        internal CameraOverrideTrackSettings(
            CameraOrbitDescriptor topOrbit,
            CameraOrbitDescriptor[] orbits,
            float[] screenY,
            Vector3 followOffset,
            Vector3 aimOffset,
            float fieldOfView)
        {
            m_TopOrbit = topOrbit;
            m_Orbits = orbits ?? Array.Empty<CameraOrbitDescriptor>();
            m_ScreenY = screenY ?? Array.Empty<float>();
            m_FollowOffset = followOffset;
            m_AimOffset = aimOffset;
            m_FieldOfView = fieldOfView;
        }

        public void RequireValid(string source)
        {
            if (TopOrbit == null || !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !float.IsFinite(FollowOffset.x) || !float.IsFinite(FollowOffset.y) ||
                !float.IsFinite(FollowOffset.z) || !float.IsFinite(AimOffset.x) ||
                !float.IsFinite(AimOffset.y) || !float.IsFinite(AimOffset.z))
                throw new InvalidOperationException($"{source} contains invalid Override track settings.");
            TopOrbit.RequireValid($"{source}.TopOrbit");
            for (int i = 0; i < Orbits.Count; i++)
                Orbits[i]?.RequireValid($"{source}.Orbits[{i}]");
            for (int i = 0; i < ScreenY.Count; i++)
            {
                if (!float.IsFinite(ScreenY[i]) || ScreenY[i] < 0f || ScreenY[i] > 1f)
                    throw new InvalidOperationException($"{source}.ScreenY[{i}] is invalid.");
            }
            if (ScreenY.Count != 0 && ScreenY.Count != Orbits.Count)
                throw new InvalidOperationException($"{source}.ScreenY must match Orbits.");
        }
    }

    [CreateAssetMenu(fileName = "CameraOverrideTrack", menuName = "3C/Character/Camera/Override Track")]
    public sealed class CameraOverrideTrackAsset : ScriptableObject
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

    [CreateAssetMenu(fileName = "CameraZoom", menuName = "3C/Character/Camera/Zoom")]
    public sealed class CameraZoomAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-zoom/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ZoomId = string.Empty;
        [SerializeField] CameraCurveAsset m_StartCurve;
        [SerializeField] CameraCurveAsset m_EndCurve;
        [SerializeField] int m_DataPriority;
        [SerializeField] bool m_IgnorePriorityInEndTime;
        [SerializeField] bool m_IgnoreWorldTimeScale;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_LastTime = -1f;
        [SerializeField] float m_StartTime;
        [SerializeField] CameraEffectStackingType m_StackingType = CameraEffectStackingType.Replace;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] CameraFovVariationType m_FovVariationType = CameraFovVariationType.Absolute;
        [SerializeField] float m_DelayTime;
        [SerializeField] float m_EndTime = 1f;
        [SerializeField] CameraEffectStackingType m_PlayStackingType = CameraEffectStackingType.Add;

        public string Schema => m_Schema ?? string.Empty;
        public string ZoomId => m_ZoomId ?? string.Empty;
        public CameraCurveAsset StartCurve => m_StartCurve;
        public CameraCurveAsset EndCurve => m_EndCurve;
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

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ZoomId) ||
                !StartCurve || !EndCurve || !float.IsFinite(LastTime) || LastTime < -1f ||
                !float.IsFinite(StartTime) || StartTime < 0f || !float.IsFinite(DelayTime) || DelayTime < 0f ||
                !float.IsFinite(EndTime) || EndTime < 0f || EndTime < StartTime ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), StackingType) ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType) ||
                !Enum.IsDefined(typeof(CameraFovVariationType), FovVariationType))
                throw new InvalidOperationException($"Camera Zoom Asset '{name}' is incomplete.");
            StartCurve.RequireValid();
            EndCurve.RequireValid();
        }
    }

    [CreateAssetMenu(fileName = "CameraStretch", menuName = "3C/Character/Camera/Stretch")]
    public sealed class CameraStretchAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-stretch/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_StretchId = string.Empty;
        [SerializeField] CameraCurveAsset m_StartCurve;
        [SerializeField] CameraCurveAsset m_EndCurve;
        [SerializeField] float m_RuntimeCamFollowYPoints;
        [SerializeField] float m_RotationZ;
        [SerializeField] bool m_IgnoreLocalAvatar;
        [SerializeField] bool m_IsAppliedElevationRatio;
        [SerializeField] bool m_IsAppliedEndElevationAngle;
        [SerializeField] CameraEffectStackingType m_PlayStackingType = CameraEffectStackingType.Add;
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
        [SerializeField] CameraSpace m_CamOffsetSpace = CameraSpace.LocalAvatar;
        [SerializeField] bool m_IgnoreOwnerTimeScale;
        [SerializeField] float m_ElevationAngleMax;
        [SerializeField] float m_HoldTime = -1f;
        [SerializeField] float m_DelayTime;
        [SerializeField] Vector3 m_CamOffset;
        [SerializeField] float m_EndElevationAngleMin;
        [SerializeField] float m_RadiusRatio;
        [SerializeField] float m_StretchTime;
        [SerializeField] CameraFovVariationType m_FovVariationType = CameraFovVariationType.Absolute;

        public string Schema => m_Schema ?? string.Empty;
        public string StretchId => m_StretchId ?? string.Empty;
        public CameraCurveAsset StartCurve => m_StartCurve;
        public CameraCurveAsset EndCurve => m_EndCurve;
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

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(StretchId) ||
                !StartCurve || !EndCurve || !float.IsFinite(RuntimeCamFollowYPoints) ||
                !float.IsFinite(RotationZ) || !float.IsFinite(RuntimeCamFollowYOffsetRatio) ||
                !float.IsFinite(ElevationAngleMin) || !float.IsFinite(EndElevationAngleMin) ||
                !float.IsFinite(ElevationAngleMax) || !float.IsFinite(EndElevationAngleMax) ||
                !float.IsFinite(RecoilTime) || RecoilTime < 0f || !float.IsFinite(HoldTime) || HoldTime < -1f ||
                !float.IsFinite(DelayTime) || DelayTime < 0f || !float.IsFinite(CamOffset.x) ||
                !float.IsFinite(CamOffset.y) || !float.IsFinite(CamOffset.z) || !float.IsFinite(RadiusRatio) ||
                !float.IsFinite(StretchTime) || StretchTime < 0f ||
                !Enum.IsDefined(typeof(CameraEffectStackingType), PlayStackingType) ||
                !Enum.IsDefined(typeof(CameraSpace), CamOffsetSpace) ||
                !Enum.IsDefined(typeof(CameraFovVariationType), FovVariationType))
                throw new InvalidOperationException($"Camera Stretch Asset '{name}' is incomplete.");
            StartCurve.RequireValid();
            EndCurve.RequireValid();
        }
    }

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

    [Serializable]
    public sealed class CameraShotBlendSettings
    {
        [SerializeField] float m_Duration;
        [SerializeField] CameraCurveAsset m_Curve;
        [SerializeField] bool m_UseCoreSpace;
        [SerializeField] bool m_UseDelta;

        public float Duration => m_Duration;
        public CameraCurveAsset Curve => m_Curve;
        public bool UseCoreSpace => m_UseCoreSpace;
        public bool UseDelta => m_UseDelta;

        public CameraShotBlendSettings() { }

        internal CameraShotBlendSettings(float duration, CameraCurveAsset curve, bool useCoreSpace, bool useDelta)
        {
            m_Duration = duration;
            m_Curve = curve;
            m_UseCoreSpace = useCoreSpace;
            m_UseDelta = useDelta;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Duration) || Duration < 0f || !Curve)
                throw new InvalidOperationException($"{source} is incomplete.");
            Curve.RequireValid();
        }
    }

    [CreateAssetMenu(fileName = "CameraShot", menuName = "3C/Character/Camera/Shot")]
    public sealed class CameraShotAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-shot/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ShotId = string.Empty;
        [SerializeField] string m_CinePrefabPath = string.Empty;
        [SerializeField] string m_FollowTargetSlotId = string.Empty;
        [SerializeField] string m_LookAtTargetSlotId = string.Empty;
        [SerializeField] float m_NearClipPlane = 0.05f;
        [SerializeField] float m_FarClipPlane = 1000f;
        [SerializeField] float m_Duration = -1f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] bool m_IgnoreCameraCollision;
        [SerializeField] bool m_ApplyEntityTimeScale;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_LookAtOffset;
        [SerializeField] Vector3 m_OffsetRotation;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] CameraShotBlendSettings m_BlendIn = new CameraShotBlendSettings();
        [SerializeField] CameraShotBlendSettings m_BlendOut = new CameraShotBlendSettings();
        [SerializeField] bool m_BlendWithIgnoreLookAtTarget;
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;

        public string Schema => m_Schema ?? string.Empty;
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
        public CameraShotBlendSettings BlendIn => m_BlendIn;
        public CameraShotBlendSettings BlendOut => m_BlendOut;
        public bool BlendWithIgnoreLookAtTarget => m_BlendWithIgnoreLookAtTarget;
        public int Priority => m_Priority;
        public string Tag => m_Tag ?? string.Empty;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ShotId) ||
                string.IsNullOrWhiteSpace(CinePrefabPath) || string.IsNullOrWhiteSpace(FollowTargetSlotId) ||
                string.IsNullOrWhiteSpace(LookAtTargetSlotId) || !float.IsFinite(NearClipPlane) || NearClipPlane < 0f ||
                !float.IsFinite(FarClipPlane) || FarClipPlane <= NearClipPlane || !float.IsFinite(Duration) ||
                Duration == 0f || Duration < -1f || !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(FollowOffset.x) || !float.IsFinite(FollowOffset.y) || !float.IsFinite(FollowOffset.z) ||
                !float.IsFinite(LookAtOffset.x) || !float.IsFinite(LookAtOffset.y) || !float.IsFinite(LookAtOffset.z) ||
                !float.IsFinite(OffsetRotation.x) || !float.IsFinite(OffsetRotation.y) || !float.IsFinite(OffsetRotation.z) ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || BlendIn == null || BlendOut == null ||
                string.IsNullOrWhiteSpace(Tag))
                throw new InvalidOperationException($"Camera Shot Asset '{name}' is incomplete.");
            BlendIn.RequireValid($"{name}.BlendIn");
            BlendOut.RequireValid($"{name}.BlendOut");
        }
    }

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
