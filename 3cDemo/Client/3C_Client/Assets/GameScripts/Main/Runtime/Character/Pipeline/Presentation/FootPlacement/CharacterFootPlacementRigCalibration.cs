using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFootPlacementRigCalibrationId : IEquatable<CharacterFootPlacementRigCalibrationId>
    {
        public CharacterFootPlacementRigCalibrationId(string value)
        {
            Value = Require(value, nameof(value));
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(CharacterFootPlacementRigCalibrationId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterFootPlacementRigCalibrationId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterFootPlacementRigCalibrationId left, CharacterFootPlacementRigCalibrationId right) => left.Equals(right);
        public static bool operator !=(CharacterFootPlacementRigCalibrationId left, CharacterFootPlacementRigCalibrationId right) => !left.Equals(right);

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Foot Placement calibration identity is invalid.", field);
            return value;
        }
    }

    [Serializable]
    public readonly struct CharacterFootPlacementCurrentSupportFootprintPose
    {
        internal CharacterFootPlacementCurrentSupportFootprintPose(
            Vector3 footPivot,
            Vector3 basePoint,
            Vector3 heelPoint,
            Vector3 positiveLateralPoint,
            Vector3 negativeLateralPoint,
            Vector3 toeTipPoint)
        {
            FootPivot = footPivot;
            BasePoint = basePoint;
            HeelPoint = heelPoint;
            PositiveLateralPoint = positiveLateralPoint;
            NegativeLateralPoint = negativeLateralPoint;
            ToeTipPoint = toeTipPoint;
        }

        public Vector3 FootPivot { get; }
        public Vector3 BasePoint { get; }
        public Vector3 HeelPoint { get; }
        public Vector3 PositiveLateralPoint { get; }
        public Vector3 NegativeLateralPoint { get; }
        public Vector3 ToeTipPoint { get; }
    }

    [Serializable]
    public readonly struct CharacterFootPlacementCurrentSupportFootprintCalibration
    {
        public CharacterFootPlacementCurrentSupportFootprintCalibration(
            Vector3 baseFootLocalOffset,
            Vector3 heelFootLocalOffset,
            Vector3 positiveLateralFootLocalOffset,
            Vector3 negativeLateralFootLocalOffset,
            Vector3 toeTipOffsetInFootAxes)
        {
            BaseFootLocalOffset = baseFootLocalOffset;
            HeelFootLocalOffset = heelFootLocalOffset;
            PositiveLateralFootLocalOffset = positiveLateralFootLocalOffset;
            NegativeLateralFootLocalOffset = negativeLateralFootLocalOffset;
            ToeTipOffsetInFootAxes = toeTipOffsetInFootAxes;
        }

        public Vector3 BaseFootLocalOffset { get; }
        public Vector3 HeelFootLocalOffset { get; }
        public Vector3 PositiveLateralFootLocalOffset { get; }
        public Vector3 NegativeLateralFootLocalOffset { get; }
        public Vector3 ToeTipOffsetInFootAxes { get; }

        public CharacterFootPlacementCurrentSupportFootprintPose Resolve(
            Vector3 footPosition,
            Quaternion footRotation,
            Vector3 toePosition) =>
            new CharacterFootPlacementCurrentSupportFootprintPose(
                footPosition,
                footPosition + footRotation * BaseFootLocalOffset,
                footPosition + footRotation * HeelFootLocalOffset,
                footPosition + footRotation * PositiveLateralFootLocalOffset,
                footPosition + footRotation * NegativeLateralFootLocalOffset,
                toePosition + footRotation * ToeTipOffsetInFootAxes);
    }

    [Serializable]
    public readonly struct CharacterFootPlacementFootCalibration
    {
        public CharacterFootPlacementFootCalibration(
            Vector3 heelContactLocalOffset,
            Vector3 toeContactLocalOffset,
            Quaternion soleFrameLocalRotation)
        {
            HeelContactLocalOffset = heelContactLocalOffset;
            ToeContactLocalOffset = toeContactLocalOffset;
            SoleFrameLocalRotation = soleFrameLocalRotation;
        }

        public Vector3 HeelContactLocalOffset { get; }
        public Vector3 ToeContactLocalOffset { get; }
        public Quaternion SoleFrameLocalRotation { get; }
        public Vector3 SoleForwardLocalAxis => SoleFrameLocalRotation * Vector3.forward;
        public Vector3 SoleUpLocalAxis => SoleFrameLocalRotation * Vector3.up;
        public Vector3 SoleRightLocalAxis => SoleFrameLocalRotation * Vector3.right;
    }

    [CreateAssetMenu(
        fileName = "CharacterFootPlacementRigCalibration",
        menuName = "3C/Presentation/Foot Placement Rig Calibration")]
    public sealed class CharacterFootPlacementRigCalibration : ScriptableObject
    {
        public const int CurrentSchemaVersion = 5;

        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] int m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] Vector3 m_CurrentSupportBaseFootLocalOffset;
        [SerializeField] Vector3 m_CurrentSupportHeelFootLocalOffset;
        [SerializeField] Vector3 m_CurrentSupportPositiveLateralFootLocalOffset;
        [SerializeField] Vector3 m_CurrentSupportNegativeLateralFootLocalOffset;
        [SerializeField] Vector3 m_CurrentSupportToeTipOffsetInFootAxes;
        [SerializeField] Vector3 m_LeftHeelContactLocalOffset;
        [SerializeField] Vector3 m_LeftToeContactLocalOffset;
        [SerializeField] Quaternion m_LeftSoleFrameLocalRotation;
        [SerializeField] Vector3 m_RightHeelContactLocalOffset;
        [SerializeField] Vector3 m_RightToeContactLocalOffset;
        [SerializeField] Quaternion m_RightSoleFrameLocalRotation;
        [SerializeField] CharacterFootPlacementRigGeometryValidationIdentity m_GeometryValidation;

        public CharacterFootPlacementRigCalibrationId CalibrationId =>
            new CharacterFootPlacementRigCalibrationId(m_CalibrationId);
        public int SchemaVersion => m_SchemaVersion;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterFootPlacementRigGeometryValidationIdentity GeometryValidation => m_GeometryValidation;
        public CharacterFootPlacementCurrentSupportFootprintCalibration CurrentSupportFootprint =>
            new CharacterFootPlacementCurrentSupportFootprintCalibration(
                m_CurrentSupportBaseFootLocalOffset,
                m_CurrentSupportHeelFootLocalOffset,
                m_CurrentSupportPositiveLateralFootLocalOffset,
                m_CurrentSupportNegativeLateralFootLocalOffset,
                m_CurrentSupportToeTipOffsetInFootAxes);
        public CharacterFootPlacementFootCalibration Left => new CharacterFootPlacementFootCalibration(
            m_LeftHeelContactLocalOffset,
            m_LeftToeContactLocalOffset,
            m_LeftSoleFrameLocalRotation);
        public CharacterFootPlacementFootCalibration Right => new CharacterFootPlacementFootCalibration(
            m_RightHeelContactLocalOffset,
            m_RightToeContactLocalOffset,
            m_RightSoleFrameLocalRotation);

        public CharacterFootPlacementFootCalibration GetFoot(CharacterFootSide side)
        {
            return side == CharacterFootSide.Left
                ? Left
                : side == CharacterFootSide.Right
                    ? Right
                    : throw new ArgumentOutOfRangeException(nameof(side));
        }

        public void Configure(
            CharacterFootPlacementRigCalibrationId calibrationId,
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementCurrentSupportFootprintCalibration currentSupportFootprint,
            CharacterFootPlacementFootCalibration left,
            CharacterFootPlacementFootCalibration right)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            RequireValidDraft(currentSupportFootprint, left, right);
            m_CalibrationId = calibrationId.Value;
            m_SchemaVersion = CurrentSchemaVersion;
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_CurrentSupportBaseFootLocalOffset = currentSupportFootprint.BaseFootLocalOffset;
            m_CurrentSupportHeelFootLocalOffset = currentSupportFootprint.HeelFootLocalOffset;
            m_CurrentSupportPositiveLateralFootLocalOffset =
                currentSupportFootprint.PositiveLateralFootLocalOffset;
            m_CurrentSupportNegativeLateralFootLocalOffset =
                currentSupportFootprint.NegativeLateralFootLocalOffset;
            m_CurrentSupportToeTipOffsetInFootAxes =
                currentSupportFootprint.ToeTipOffsetInFootAxes;
            m_LeftHeelContactLocalOffset = left.HeelContactLocalOffset;
            m_LeftToeContactLocalOffset = left.ToeContactLocalOffset;
            m_LeftSoleFrameLocalRotation = Normalize(left.SoleFrameLocalRotation);
            m_RightHeelContactLocalOffset = right.HeelContactLocalOffset;
            m_RightToeContactLocalOffset = right.ToeContactLocalOffset;
            m_RightSoleFrameLocalRotation = Normalize(right.SoleFrameLocalRotation);
            m_ContentRevision = ComputeContentRevision();
            m_GeometryValidation = null;
            RequireConfiguredForAuthoring();
        }

        public void PublishGeometryValidation(CharacterFootPlacementRigGeometryValidationIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            RequireConfiguredForAuthoring();
            identity.RequireMatches(null, this);
            m_GeometryValidation = identity;
            RequireValid();
        }

        public void RequireValid()
        {
            RequireConfiguredForAuthoring();
            if (m_GeometryValidation == null)
                throw new InvalidOperationException("Foot Placement calibration geometry validation identity is missing.");
            m_GeometryValidation.RequireValid();
            if (!string.Equals(m_GeometryValidation.RigId, RigId, StringComparison.Ordinal) ||
                !string.Equals(m_GeometryValidation.RigRevision, RigRevision, StringComparison.Ordinal) ||
                m_GeometryValidation.CalibrationId != CalibrationId ||
                !string.Equals(m_GeometryValidation.CalibrationRevision, ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement calibration geometry validation identity is stale.");
        }

        public void RequireConfiguredForAuthoring()
        {
            _ = CalibrationId;
            if (m_SchemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Foot Placement calibration schema '{m_SchemaVersion}' is unsupported.");
            if (string.IsNullOrWhiteSpace(RigId) ||
                string.IsNullOrWhiteSpace(RigRevision))
                throw new InvalidOperationException("Foot Placement calibration Rig identity is missing.");
            RequireFoot(Left, "Left");
            RequireFoot(Right, "Right");
            RequireCurrentSupportFootprint(CurrentSupportFootprint);
            string computed = ComputeContentRevision();
            if (string.IsNullOrEmpty(m_ContentRevision) ||
                !string.Equals(m_ContentRevision, computed, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement calibration content revision is stale.");
        }

        public void RequireRig(CharacterAnimationRigDefinition rig)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            RequireValid();
            rig.RequireValid();
            if (!string.Equals(RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, rig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Foot Placement calibration '{CalibrationId}' does not match Rig '{rig.RigId}@{rig.Revision}'.");
        }

        public void RequireRigForAuthoring(CharacterAnimationRigDefinition rig)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            RequireConfiguredForAuthoring();
            rig.RequireValid();
            if (!string.Equals(RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, rig.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Foot Placement calibration '{CalibrationId}' does not match Rig '{rig.RigId}@{rig.Revision}'.");
        }

        public static void RequireValidDraft(
            CharacterFootPlacementCurrentSupportFootprintCalibration currentSupportFootprint,
            CharacterFootPlacementFootCalibration left,
            CharacterFootPlacementFootCalibration right)
        {
            RequireCurrentSupportFootprint(currentSupportFootprint);
            RequireFoot(left, "Left");
            RequireFoot(right, "Right");
        }

        public string ComputeContentRevision()
        {
            return StableHash.Compute(
                "character-foot-placement-rig-calibration/v5-current-support-footprint",
                m_CalibrationId ?? string.Empty,
                m_SchemaVersion.ToString(CultureInfo.InvariantCulture),
                RigId,
                RigRevision,
                Format(m_CurrentSupportBaseFootLocalOffset),
                Format(m_CurrentSupportHeelFootLocalOffset),
                Format(m_CurrentSupportPositiveLateralFootLocalOffset),
                Format(m_CurrentSupportNegativeLateralFootLocalOffset),
                Format(m_CurrentSupportToeTipOffsetInFootAxes),
                Format(m_LeftHeelContactLocalOffset),
                Format(m_LeftToeContactLocalOffset),
                Format(m_LeftSoleFrameLocalRotation),
                Format(m_RightHeelContactLocalOffset),
                Format(m_RightToeContactLocalOffset),
                Format(m_RightSoleFrameLocalRotation)).ToString();
        }

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_CalibrationId))
                return;
            m_ContentRevision = ComputeContentRevision();
        }

        static void RequireFoot(CharacterFootPlacementFootCalibration value, string side)
        {
            RequireFinite(value.HeelContactLocalOffset, $"{side}HeelContactLocalOffset");
            RequireFinite(value.ToeContactLocalOffset, $"{side}ToeContactLocalOffset");
            if (value.HeelContactLocalOffset.sqrMagnitude <= 0.00000001f &&
                value.ToeContactLocalOffset.sqrMagnitude <= 0.00000001f)
                throw new InvalidOperationException($"Foot Placement calibration '{side}' contact offsets are not configured.");
            RequireUnit(value.SoleFrameLocalRotation, $"{side}SoleFrameLocalRotation");
        }

        static void RequireCurrentSupportFootprint(
            CharacterFootPlacementCurrentSupportFootprintCalibration value)
        {
            RequireFinite(value.BaseFootLocalOffset, nameof(value.BaseFootLocalOffset));
            RequireFinite(value.HeelFootLocalOffset, nameof(value.HeelFootLocalOffset));
            RequireFinite(
                value.PositiveLateralFootLocalOffset,
                nameof(value.PositiveLateralFootLocalOffset));
            RequireFinite(
                value.NegativeLateralFootLocalOffset,
                nameof(value.NegativeLateralFootLocalOffset));
            RequireFinite(value.ToeTipOffsetInFootAxes, nameof(value.ToeTipOffsetInFootAxes));
            if ((value.HeelFootLocalOffset - value.BaseFootLocalOffset).sqrMagnitude <= 0.00000001f ||
                (value.PositiveLateralFootLocalOffset - value.BaseFootLocalOffset).sqrMagnitude <= 0.00000001f ||
                (value.NegativeLateralFootLocalOffset - value.BaseFootLocalOffset).sqrMagnitude <= 0.00000001f ||
                (value.PositiveLateralFootLocalOffset -
                 value.NegativeLateralFootLocalOffset).sqrMagnitude <= 0.00000001f ||
                value.ToeTipOffsetInFootAxes.sqrMagnitude <= 0.00000001f)
            {
                throw new InvalidOperationException(
                    "Foot Placement Current Support footprint is degenerate.");
            }
        }

        static Quaternion Normalize(Quaternion value)
        {
            RequireFinite(value, nameof(value));
            float magnitude = Mathf.Sqrt(
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.0001f)
                throw new InvalidOperationException("Foot Placement calibration sole frame is degenerate.");
            float inverse = 1f / magnitude;
            return new Quaternion(value.x * inverse, value.y * inverse, value.z * inverse, value.w * inverse);
        }

        static void RequireUnit(Quaternion value, string field)
        {
            RequireFinite(value, field);
            float squareMagnitude = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            if (Mathf.Abs(squareMagnitude - 1f) > 0.01f)
                throw new InvalidOperationException($"Foot Placement calibration '{field}' must be normalized.");
        }

        static void RequireFinite(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException($"Foot Placement calibration '{field}' is not finite.");
        }

        static void RequireFinite(Quaternion value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w))
                throw new InvalidOperationException($"Foot Placement calibration '{field}' is not finite.");
        }

        static string Format(Vector3 value)
        {
            return string.Concat(
                value.x.ToString("R", CultureInfo.InvariantCulture), "|",
                value.y.ToString("R", CultureInfo.InvariantCulture), "|",
                value.z.ToString("R", CultureInfo.InvariantCulture));
        }

        static string Format(Quaternion value)
        {
            return string.Concat(
                value.x.ToString("R", CultureInfo.InvariantCulture), "|",
                value.y.ToString("R", CultureInfo.InvariantCulture), "|",
                value.z.ToString("R", CultureInfo.InvariantCulture), "|",
                value.w.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
