using System;
using System.Globalization;
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
    public readonly struct CharacterFootPlacementFootCalibration
    {
        public CharacterFootPlacementFootCalibration(
            Vector3 heelSoleLocalOffset,
            Vector3 toeSoleLocalOffset,
            Vector3 semanticForwardLocalAxis,
            Vector3 semanticUpLocalAxis,
            Vector3 kneePoleVisualRootLocalDirection)
        {
            HeelSoleLocalOffset = heelSoleLocalOffset;
            ToeSoleLocalOffset = toeSoleLocalOffset;
            SemanticForwardLocalAxis = semanticForwardLocalAxis;
            SemanticUpLocalAxis = semanticUpLocalAxis;
            KneePoleVisualRootLocalDirection = kneePoleVisualRootLocalDirection;
        }

        public Vector3 HeelSoleLocalOffset { get; }
        public Vector3 ToeSoleLocalOffset { get; }
        public Vector3 SemanticForwardLocalAxis { get; }
        public Vector3 SemanticUpLocalAxis { get; }
        public Vector3 KneePoleVisualRootLocalDirection { get; }
    }

    [CreateAssetMenu(
        fileName = "CharacterFootPlacementRigCalibration",
        menuName = "3C/Presentation/Foot Placement Rig Calibration")]
    public sealed class CharacterFootPlacementRigCalibration : ScriptableObject
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] int m_SchemaVersion = CurrentSchemaVersion;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] Vector3 m_LeftHeelSoleLocalOffset;
        [SerializeField] Vector3 m_LeftToeSoleLocalOffset;
        [SerializeField] Vector3 m_LeftSemanticForwardLocalAxis;
        [SerializeField] Vector3 m_LeftSemanticUpLocalAxis;
        [SerializeField] Vector3 m_LeftKneePoleVisualRootLocalDirection;
        [SerializeField] Vector3 m_RightHeelSoleLocalOffset;
        [SerializeField] Vector3 m_RightToeSoleLocalOffset;
        [SerializeField] Vector3 m_RightSemanticForwardLocalAxis;
        [SerializeField] Vector3 m_RightSemanticUpLocalAxis;
        [SerializeField] Vector3 m_RightKneePoleVisualRootLocalDirection;

        public CharacterFootPlacementRigCalibrationId CalibrationId =>
            new CharacterFootPlacementRigCalibrationId(m_CalibrationId);
        public int SchemaVersion => m_SchemaVersion;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public CharacterFootPlacementFootCalibration Left => new CharacterFootPlacementFootCalibration(
            m_LeftHeelSoleLocalOffset,
            m_LeftToeSoleLocalOffset,
            m_LeftSemanticForwardLocalAxis,
            m_LeftSemanticUpLocalAxis,
            m_LeftKneePoleVisualRootLocalDirection);
        public CharacterFootPlacementFootCalibration Right => new CharacterFootPlacementFootCalibration(
            m_RightHeelSoleLocalOffset,
            m_RightToeSoleLocalOffset,
            m_RightSemanticForwardLocalAxis,
            m_RightSemanticUpLocalAxis,
            m_RightKneePoleVisualRootLocalDirection);

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
            CharacterFootPlacementFootCalibration left,
            CharacterFootPlacementFootCalibration right)
        {
            m_CalibrationId = calibrationId.Value;
            m_SchemaVersion = CurrentSchemaVersion;
            m_LeftHeelSoleLocalOffset = left.HeelSoleLocalOffset;
            m_LeftToeSoleLocalOffset = left.ToeSoleLocalOffset;
            m_LeftSemanticForwardLocalAxis = Normalize(left.SemanticForwardLocalAxis);
            m_LeftSemanticUpLocalAxis = Orthogonalize(left.SemanticUpLocalAxis, m_LeftSemanticForwardLocalAxis);
            m_LeftKneePoleVisualRootLocalDirection = Normalize(left.KneePoleVisualRootLocalDirection);
            m_RightHeelSoleLocalOffset = right.HeelSoleLocalOffset;
            m_RightToeSoleLocalOffset = right.ToeSoleLocalOffset;
            m_RightSemanticForwardLocalAxis = Normalize(right.SemanticForwardLocalAxis);
            m_RightSemanticUpLocalAxis = Orthogonalize(right.SemanticUpLocalAxis, m_RightSemanticForwardLocalAxis);
            m_RightKneePoleVisualRootLocalDirection = Normalize(right.KneePoleVisualRootLocalDirection);
            m_ContentRevision = ComputeContentRevision();
            RequireValid();
        }

        public void RequireValid()
        {
            _ = CalibrationId;
            if (m_SchemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Foot Placement calibration schema '{m_SchemaVersion}' is unsupported.");
            RequireFoot(Left, "Left");
            RequireFoot(Right, "Right");
            string computed = ComputeContentRevision();
            if (string.IsNullOrEmpty(m_ContentRevision) ||
                !string.Equals(m_ContentRevision, computed, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement calibration content revision is stale.");
        }

        public string ComputeContentRevision()
        {
            return StableHash.Compute(
                "character-foot-placement-rig-calibration/v1",
                m_CalibrationId ?? string.Empty,
                m_SchemaVersion.ToString(CultureInfo.InvariantCulture),
                Format(m_LeftHeelSoleLocalOffset),
                Format(m_LeftToeSoleLocalOffset),
                Format(m_LeftSemanticForwardLocalAxis),
                Format(m_LeftSemanticUpLocalAxis),
                Format(m_LeftKneePoleVisualRootLocalDirection),
                Format(m_RightHeelSoleLocalOffset),
                Format(m_RightToeSoleLocalOffset),
                Format(m_RightSemanticForwardLocalAxis),
                Format(m_RightSemanticUpLocalAxis),
                Format(m_RightKneePoleVisualRootLocalDirection)).ToString();
        }

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_CalibrationId))
                return;
            m_ContentRevision = ComputeContentRevision();
        }

        static void RequireFoot(CharacterFootPlacementFootCalibration value, string side)
        {
            RequireFinite(value.HeelSoleLocalOffset, $"{side}HeelSoleLocalOffset");
            RequireFinite(value.ToeSoleLocalOffset, $"{side}ToeSoleLocalOffset");
            if (value.HeelSoleLocalOffset.sqrMagnitude <= 0.00000001f &&
                value.ToeSoleLocalOffset.sqrMagnitude <= 0.00000001f)
                throw new InvalidOperationException($"Foot Placement calibration '{side}' sole offsets are not configured.");
            RequireUnit(value.SemanticForwardLocalAxis, $"{side}SemanticForwardLocalAxis");
            RequireUnit(value.SemanticUpLocalAxis, $"{side}SemanticUpLocalAxis");
            RequireUnit(value.KneePoleVisualRootLocalDirection, $"{side}KneePoleVisualRootLocalDirection");
            if (Mathf.Abs(Vector3.Dot(value.SemanticForwardLocalAxis, value.SemanticUpLocalAxis)) > 0.01f)
                throw new InvalidOperationException($"Foot Placement calibration '{side}' semantic axes are not orthogonal.");
        }

        static Vector3 Normalize(Vector3 value)
        {
            RequireFinite(value, nameof(value));
            if (value.sqrMagnitude <= 0.0001f)
                throw new InvalidOperationException("Foot Placement calibration direction is degenerate.");
            return value.normalized;
        }

        static Vector3 Orthogonalize(Vector3 value, Vector3 forward)
        {
            Vector3 result = Vector3.ProjectOnPlane(Normalize(value), forward);
            if (result.sqrMagnitude <= 0.0001f)
                throw new InvalidOperationException("Foot Placement calibration semantic axes are collinear.");
            return result.normalized;
        }

        static void RequireUnit(Vector3 value, string field)
        {
            RequireFinite(value, field);
            if (Mathf.Abs(value.sqrMagnitude - 1f) > 0.01f)
                throw new InvalidOperationException($"Foot Placement calibration '{field}' must be normalized.");
        }

        static void RequireFinite(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException($"Foot Placement calibration '{field}' is not finite.");
        }

        static string Format(Vector3 value)
        {
            return string.Concat(
                value.x.ToString("R", CultureInfo.InvariantCulture), "|",
                value.y.ToString("R", CultureInfo.InvariantCulture), "|",
                value.z.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}
