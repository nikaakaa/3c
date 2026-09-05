using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAclCompressionSettings
    {
        public const string CurrentRevision = "acl-compression-settings/v1";
        internal const string NativeEncodingAlgorithmVersion = "acl-native-encoding/v2";
        internal const float NativeEncodingPrecisionFraction = 0.25f;

        [SerializeField] string m_Revision = CurrentRevision;
        [SerializeField] int m_SampleRate = 60;
        [SerializeField] float m_TransformPrecision = 0.01f;
        [SerializeField] float m_ScalePrecision = 0.01f;
        [SerializeField] float m_RotationPrecisionDegrees = 0.5f;
        [SerializeField] float m_ScalarPrecision = 0.00001f;
        [SerializeField] float m_ShellDistance = 3f;
        [SerializeField] bool m_OptimizeLoops;
        [SerializeField] bool m_EnableDatabase;
        [SerializeField] bool m_EnableMediumTier;
        [SerializeField] bool m_EnableLowTier;
        [SerializeField] bool m_EnablePerTrackRounding;
        [SerializeField] float m_MediumImportanceTierProportion;
        [SerializeField] float m_LowImportanceTierProportion = 0.5f;
        [SerializeField] int m_MaxDatabaseChunkSize = 1024 * 1024;
        [SerializeField] string m_CompilerOptions = "msvc;fp:precise;simd:on";

        public string Revision => m_Revision ?? string.Empty;
        public int SampleRate => m_SampleRate;
        public float TransformPrecision => m_TransformPrecision;
        public float ScalePrecision => m_ScalePrecision;
        public float RotationPrecisionDegrees => m_RotationPrecisionDegrees;
        public float ScalarPrecision => m_ScalarPrecision;
        public float ShellDistance => m_ShellDistance;
        public bool OptimizeLoops => m_OptimizeLoops;
        public bool EnableDatabase => m_EnableDatabase;
        public bool EnableMediumTier => m_EnableMediumTier;
        public bool EnableLowTier => m_EnableLowTier;
        public bool EnablePerTrackRounding => m_EnablePerTrackRounding;
        public float MediumImportanceTierProportion => m_MediumImportanceTierProportion;
        public float LowImportanceTierProportion => m_LowImportanceTierProportion;
        public int MaxDatabaseChunkSize => m_MaxDatabaseChunkSize;
        public string CompilerOptions => m_CompilerOptions ?? string.Empty;

        internal float NativeTransformPrecision =>
            Mathf.Min(TransformPrecision, ScalePrecision) * NativeEncodingPrecisionFraction;
        internal float NativeScalarPrecision =>
            ScalarPrecision * NativeEncodingPrecisionFraction;

        public CharacterAclCompressionSettings() { }

        public CharacterAclCompressionSettings(
            int sampleRate,
            float transformPrecision,
            float scalePrecision,
            float rotationPrecisionDegrees,
            float scalarPrecision,
            float shellDistance,
            bool optimizeLoops,
            bool enableDatabase,
            bool enableMediumTier,
            bool enableLowTier,
            bool enablePerTrackRounding,
            float mediumImportanceTierProportion,
            float lowImportanceTierProportion,
            int maxDatabaseChunkSize,
            string compilerOptions)
        {
            m_Revision = CurrentRevision;
            m_SampleRate = sampleRate;
            m_TransformPrecision = transformPrecision;
            m_ScalePrecision = scalePrecision;
            m_RotationPrecisionDegrees = rotationPrecisionDegrees;
            m_ScalarPrecision = scalarPrecision;
            m_ShellDistance = shellDistance;
            m_OptimizeLoops = optimizeLoops;
            m_EnableDatabase = enableDatabase;
            m_EnableMediumTier = enableMediumTier;
            m_EnableLowTier = enableLowTier;
            m_EnablePerTrackRounding = enablePerTrackRounding;
            m_MediumImportanceTierProportion = mediumImportanceTierProportion;
            m_LowImportanceTierProportion = lowImportanceTierProportion;
            m_MaxDatabaseChunkSize = maxDatabaseChunkSize;
            m_CompilerOptions = compilerOptions?.Trim() ?? string.Empty;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!string.Equals(Revision, CurrentRevision, StringComparison.Ordinal) ||
                SampleRate <= 0 ||
                !float.IsFinite(TransformPrecision) || TransformPrecision < 0f ||
                !float.IsFinite(ScalePrecision) || ScalePrecision < 0f ||
                !float.IsFinite(RotationPrecisionDegrees) || RotationPrecisionDegrees <= 0f ||
                !float.IsFinite(ScalarPrecision) || ScalarPrecision < 0f ||
                !float.IsFinite(ShellDistance) || ShellDistance <= 0f ||
                !float.IsFinite(MediumImportanceTierProportion) ||
                !float.IsFinite(LowImportanceTierProportion) ||
                MediumImportanceTierProportion < 0f || MediumImportanceTierProportion > 1f ||
                LowImportanceTierProportion < 0f || LowImportanceTierProportion > 1f ||
                MediumImportanceTierProportion + LowImportanceTierProportion > 1f ||
                MaxDatabaseChunkSize < 4096 ||
                string.IsNullOrWhiteSpace(CompilerOptions))
            {
                throw new InvalidOperationException("ACL compression settings are invalid.");
            }
            if ((EnableMediumTier || EnableLowTier) && !EnableDatabase)
                throw new InvalidOperationException("ACL quality tiers require database compression.");
        }
    }
}
