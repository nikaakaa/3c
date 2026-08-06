using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [Serializable]
    public sealed class CharacterFootPlacementRigGeometryValidationIdentity
    {
        public const int CurrentSchemaVersion = 2;

        [SerializeField] int m_SchemaVersion;
        [SerializeField] string m_IdentityHash = string.Empty;
        [SerializeField] string m_GeometryContentHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] string m_CalibrationId = string.Empty;
        [SerializeField] string m_CalibrationRevision = string.Empty;
        [SerializeField] string m_SamplingRigAssetGuid = string.Empty;
        [SerializeField] string m_SamplingRigDependencyHash = string.Empty;
        [SerializeField] string m_PreviewClipAssetGuid = string.Empty;
        [SerializeField] string m_PreviewClipDependencyHash = string.Empty;
        [SerializeField] int m_PreviewNormalizedTimeBits;

        public CharacterFootPlacementRigGeometryValidationIdentity(
            string geometryContentHash,
            string rigId,
            string rigRevision,
            CharacterFootPlacementRigCalibrationId calibrationId,
            string calibrationRevision,
            string samplingRigAssetGuid,
            string samplingRigDependencyHash,
            string previewClipAssetGuid,
            string previewClipDependencyHash,
            float previewNormalizedTime)
        {
            m_SchemaVersion = CurrentSchemaVersion;
            m_GeometryContentHash = RequireHash(geometryContentHash, nameof(geometryContentHash));
            m_RigId = RequireText(rigId, nameof(rigId));
            m_RigRevision = RequireText(rigRevision, nameof(rigRevision));
            m_CalibrationId = calibrationId.Value;
            m_CalibrationRevision = RequireText(calibrationRevision, nameof(calibrationRevision));
            m_SamplingRigAssetGuid = RequireGuid(samplingRigAssetGuid, nameof(samplingRigAssetGuid));
            m_SamplingRigDependencyHash = RequireHash(samplingRigDependencyHash, nameof(samplingRigDependencyHash));
            m_PreviewClipAssetGuid = RequireGuid(previewClipAssetGuid, nameof(previewClipAssetGuid));
            m_PreviewClipDependencyHash = RequireHash(previewClipDependencyHash, nameof(previewClipDependencyHash));
            if (!float.IsFinite(previewNormalizedTime) || previewNormalizedTime < 0f || previewNormalizedTime > 1f)
                throw new ArgumentOutOfRangeException(nameof(previewNormalizedTime));
            m_PreviewNormalizedTimeBits = BitConverter.SingleToInt32Bits(previewNormalizedTime);
            m_IdentityHash = ComputeIdentityHash().Value;
            RequireValid();
        }

        public int SchemaVersion => m_SchemaVersion;
        public string IdentityHash => m_IdentityHash ?? string.Empty;
        public string GeometryContentHash => m_GeometryContentHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterFootPlacementRigCalibrationId CalibrationId =>
            new CharacterFootPlacementRigCalibrationId(m_CalibrationId);
        public string CalibrationRevision => m_CalibrationRevision ?? string.Empty;
        public string SamplingRigAssetGuid => m_SamplingRigAssetGuid ?? string.Empty;
        public string SamplingRigDependencyHash => m_SamplingRigDependencyHash ?? string.Empty;
        public string PreviewClipAssetGuid => m_PreviewClipAssetGuid ?? string.Empty;
        public string PreviewClipDependencyHash => m_PreviewClipDependencyHash ?? string.Empty;
        public float PreviewNormalizedTime => BitConverter.Int32BitsToSingle(m_PreviewNormalizedTimeBits);

        public void RequireValid()
        {
            if (m_SchemaVersion != CurrentSchemaVersion)
                throw new InvalidOperationException($"Foot Placement geometry validation schema '{m_SchemaVersion}' is unsupported.");
            _ = new StableHash(IdentityHash);
            _ = new StableHash(GeometryContentHash);
            _ = RequireText(RigId, nameof(RigId));
            _ = RequireText(RigRevision, nameof(RigRevision));
            _ = CalibrationId;
            _ = RequireText(CalibrationRevision, nameof(CalibrationRevision));
            _ = RequireGuid(SamplingRigAssetGuid, nameof(SamplingRigAssetGuid));
            _ = RequireHash(SamplingRigDependencyHash, nameof(SamplingRigDependencyHash));
            _ = RequireGuid(PreviewClipAssetGuid, nameof(PreviewClipAssetGuid));
            _ = RequireHash(PreviewClipDependencyHash, nameof(PreviewClipDependencyHash));
            if (!float.IsFinite(PreviewNormalizedTime) || PreviewNormalizedTime < 0f || PreviewNormalizedTime > 1f)
                throw new InvalidOperationException("Foot Placement geometry validation preview time is invalid.");
            if (!string.Equals(IdentityHash, ComputeIdentityHash().Value, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement geometry validation identity hash is stale.");
        }

        public void RequireMatches(
            CharacterAnimationRigDefinition rig,
            CharacterFootPlacementRigCalibration calibration)
        {
            if (!calibration)
                throw new ArgumentNullException(nameof(calibration));
            RequireValid();
            if (rig)
                rig.RequireValid();
            string expectedRigId = rig ? rig.RigId : calibration.RigId;
            string expectedRigRevision = rig ? rig.Revision : calibration.RigRevision;
            if (!string.Equals(RigId, expectedRigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, expectedRigRevision, StringComparison.Ordinal) ||
                CalibrationId != calibration.CalibrationId ||
                !string.Equals(CalibrationRevision, calibration.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement geometry validation identity does not match the current Rig and Calibration revision.");
        }

        StableHash ComputeIdentityHash()
        {
            return StableHash.Compute(
                "character-foot-placement-rig-geometry-validation/v2",
                m_SchemaVersion.ToString(CultureInfo.InvariantCulture),
                GeometryContentHash,
                RigId,
                RigRevision,
                m_CalibrationId ?? string.Empty,
                CalibrationRevision,
                SamplingRigAssetGuid,
                SamplingRigDependencyHash,
                PreviewClipAssetGuid,
                PreviewClipDependencyHash,
                m_PreviewNormalizedTimeBits.ToString("x8", CultureInfo.InvariantCulture));
        }

        static string RequireText(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException($"Foot Placement geometry validation '{field}' is invalid.", field);
            return value;
        }

        static string RequireGuid(string value, string field)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                throw new ArgumentException($"Foot Placement geometry validation '{field}' is not an asset GUID.", field);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9' && c < 'a' || c > 'f')
                    throw new ArgumentException($"Foot Placement geometry validation '{field}' is not an asset GUID.", field);
            }
            return value;
        }

        static string RequireHash(string value, string field)
        {
            _ = new StableHash(value);
            return value;
        }
    }
}
