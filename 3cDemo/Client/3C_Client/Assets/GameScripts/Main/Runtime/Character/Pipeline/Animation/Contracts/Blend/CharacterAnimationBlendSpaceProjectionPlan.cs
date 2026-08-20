using System;
using System.Collections.Generic;
using UnityEngine;
using UnityAnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationBlendSpaceAxisPlan
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterValueType m_ValueType;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_Minimum;
        [SerializeField] float m_Maximum;

        internal CharacterAnimationBlendSpaceAxisPlan(CharacterAnimationBlendSpaceAxis axis)
        {
            if (axis == null || !axis.ParameterId.IsValid || axis.ValueType != PoseParameterValueType.Float ||
                string.IsNullOrWhiteSpace(axis.Unit) || !float.IsFinite(axis.Minimum) ||
                !float.IsFinite(axis.Maximum) || axis.Minimum >= axis.Maximum)
                throw new ArgumentException("Blend Space axis plan input is invalid.", nameof(axis));
            m_ParameterId = axis.ParameterId.Value;
            m_ValueType = axis.ValueType;
            m_Unit = axis.Unit;
            m_Minimum = axis.Minimum;
            m_Maximum = axis.Maximum;
        }

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public PoseParameterValueType ValueType => m_ValueType;
        public string Unit => m_Unit ?? string.Empty;
        public float Minimum => m_Minimum;
        public float Maximum => m_Maximum;

        public void RequireValid()
        {
            if (!ParameterId.IsValid || ValueType != PoseParameterValueType.Float ||
                string.IsNullOrWhiteSpace(Unit) || !float.IsFinite(Minimum) ||
                !float.IsFinite(Maximum) || Minimum >= Maximum)
                throw new InvalidOperationException("Blend Space axis plan is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceSamplePlan
    {
        [SerializeField] string m_SampleId = string.Empty;
        [SerializeField] UnityAnimationClip m_Clip;
        [SerializeField] string m_ClipIdentity = string.Empty;
        [SerializeField] string m_FullClipDependencyHash = string.Empty;
        [SerializeField] string m_AnalysisInputHash = string.Empty;
        [SerializeField] string m_RegisteredCurveHash = string.Empty;
        [SerializeField] float m_SourceDurationSeconds;
        [SerializeField] float m_PositionX;
        [SerializeField] float m_PositionY;
        [SerializeField] CharacterAnimationBlendSpaceSampleRole m_Role;
        [SerializeField] float m_StationaryNormalizedTime;
        [SerializeField] AnimationCurve m_FootPlacementWeightCurve;
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;
        [SerializeField] CharacterAnimationBlendSpaceSampleParameter[] m_Parameters = Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        internal CharacterAnimationBlendSpaceSamplePlan(
            CharacterAnimationBlendSpaceSample sample,
            string clipIdentity,
            string fullClipDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            float sourceDurationSeconds,
            AnimationCurve normalizedFootPlacementWeightCurve,
            AnimationFootFeaturePair footFeatures)
        {
            if (sample == null || !sample.SampleId.IsValid || !sample.Clip ||
                string.IsNullOrWhiteSpace(clipIdentity) || string.IsNullOrWhiteSpace(fullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(analysisInputHash) || string.IsNullOrWhiteSpace(registeredCurveHash) ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                normalizedFootPlacementWeightCurve == null || normalizedFootPlacementWeightCurve.length < 2)
                throw new ArgumentException("Blend Space Clip Sample plan input is invalid.", nameof(sample));
            m_SampleId = sample.SampleId.Value;
            m_Clip = sample.Clip;
            m_ClipIdentity = clipIdentity.Trim();
            m_FullClipDependencyHash = fullClipDependencyHash.Trim();
            m_AnalysisInputHash = analysisInputHash.Trim();
            m_RegisteredCurveHash = registeredCurveHash.Trim();
            m_SourceDurationSeconds = sourceDurationSeconds;
            m_PositionX = sample.Position.x;
            m_PositionY = sample.Position.y;
            m_Role = sample.Role;
            m_StationaryNormalizedTime = sample.StationaryNormalizedTime;
            m_FootPlacementWeightCurve = new AnimationCurve(normalizedFootPlacementWeightCurve.keys);
            if (footFeatures.IsValid)
            {
                m_LeftFootFeatures = footFeatures.Left;
                m_RightFootFeatures = footFeatures.Right;
            }
            m_Parameters = new CharacterAnimationBlendSpaceSampleParameter[sample.Parameters.Count];
            for (int i = 0; i < m_Parameters.Length; i++)
                m_Parameters[i] = new CharacterAnimationBlendSpaceSampleParameter(sample.Parameters[i].ParameterId, sample.Parameters[i].Value);
        }

        public CharacterAnimationBlendSpaceSampleId SampleId => string.IsNullOrWhiteSpace(m_SampleId) ? default : new CharacterAnimationBlendSpaceSampleId(m_SampleId);
        public UnityAnimationClip Clip => m_Clip;
        public string ClipIdentity => m_ClipIdentity ?? string.Empty;
        public string FullClipDependencyHash => m_FullClipDependencyHash ?? string.Empty;
        public string AnalysisInputHash => m_AnalysisInputHash ?? string.Empty;
        public string RegisteredCurveHash => m_RegisteredCurveHash ?? string.Empty;
        public float SourceDurationSeconds => m_SourceDurationSeconds;
        public float PositionX => m_PositionX;
        public float PositionY => m_PositionY;
        public CharacterAnimationBlendSpaceSampleRole Role => m_Role;
        public float StationaryNormalizedTime => m_StationaryNormalizedTime;
        public AnimationFootFeatureCurveSet LeftFootFeatures => m_LeftFootFeatures;
        public AnimationFootFeatureCurveSet RightFootFeatures => m_RightFootFeatures;
        public bool HasFootFeatures => m_LeftFootFeatures != null && m_RightFootFeatures != null;
        public IReadOnlyList<CharacterAnimationBlendSpaceSampleParameter> Parameters => m_Parameters ?? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        public float SampleFootPlacementWeight(float normalizedTime)
        {
            float value = m_FootPlacementWeightCurve.Evaluate(Mathf.Clamp01(normalizedTime));
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"Blend Space Sample '{SampleId}' Foot Placement Weight is invalid.");
            return Mathf.Clamp01(value);
        }

        public bool TryGetParameter(PoseParameterId parameterId, out float value)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (!Parameters[i].ParameterId.Equals(parameterId))
                    continue;
                value = Parameters[i].Value;
                return true;
            }
            value = 0f;
            return false;
        }

        public void RequireValid(bool requireFootFeatures)
        {
            if (!SampleId.IsValid || !Clip || !float.IsFinite(Clip.length) || Clip.length <= 0f ||
                string.IsNullOrWhiteSpace(ClipIdentity) || string.IsNullOrWhiteSpace(FullClipDependencyHash) ||
                string.IsNullOrWhiteSpace(AnalysisInputHash) || string.IsNullOrWhiteSpace(RegisteredCurveHash) ||
                !float.IsFinite(SourceDurationSeconds) || SourceDurationSeconds <= 0f ||
                !float.IsFinite(PositionX) || !float.IsFinite(PositionY) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), Role) ||
                !float.IsFinite(StationaryNormalizedTime) || StationaryNormalizedTime < 0f || StationaryNormalizedTime > 1f ||
                m_FootPlacementWeightCurve == null || m_FootPlacementWeightCurve.length < 2 ||
                requireFootFeatures && !HasFootFeatures)
                throw new InvalidOperationException($"Blend Space Sample plan '{SampleId}' is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceParameterPolicyPlan
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] CharacterAnimationBlendSpaceParameterPolicy m_Policy;

        internal CharacterAnimationBlendSpaceParameterPolicyPlan(CharacterAnimationBlendSpacePoseParameterPolicy policy)
        {
            if (policy == null || !policy.ParameterId.IsValid || !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceParameterPolicy), policy.Policy))
                throw new ArgumentException("Blend Space parameter policy plan input is invalid.", nameof(policy));
            m_ParameterId = policy.ParameterId.Value;
            m_Policy = policy.Policy;
        }

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public CharacterAnimationBlendSpaceParameterPolicy Policy => m_Policy;
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpacePlan
    {
        [SerializeField] string m_PlanIdentity = string.Empty;
        [SerializeField] string m_BlendSpaceId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterAnimationBlendSpaceMode m_Mode;
        [SerializeField] CharacterAnimationBlendSpaceAxisPlan m_XAxis;
        [SerializeReference] CharacterAnimationBlendSpaceAxisPlan m_YAxis;
        [SerializeField] CharacterAnimationBlendSpacePhasePolicy m_PhasePolicy;
        [SerializeField] int m_PhaseReferenceSampleIndex = -1;
        [SerializeField] CharacterAnimationBlendSpaceSamplePlan[] m_Samples = Array.Empty<CharacterAnimationBlendSpaceSamplePlan>();
        [SerializeField] CharacterAnimationBlendSpaceParameterPolicyPlan[] m_ParameterPolicies = Array.Empty<CharacterAnimationBlendSpaceParameterPolicyPlan>();
        [SerializeField] float[] m_SolverFactorsX = Array.Empty<float>();
        [SerializeField] float[] m_SolverFactorsY = Array.Empty<float>();
        [SerializeField] float[] m_SolverMagnitudes = Array.Empty<float>();

        internal CharacterAnimationBlendSpacePlan(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSamplePlan[] samples)
        {
            CharacterAnimationBlendSpaceValidationReport validation = CharacterAnimationBlendSpaceValidator.Validate(asset);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.Issues[0].ToString());
            m_BlendSpaceId = asset.BlendSpaceId.Value;
            m_ContentRevision = asset.ContentRevision;
            m_PlanIdentity = $"{m_BlendSpaceId}@{m_ContentRevision}";
            m_RigId = asset.Rig.RigId;
            m_RigRevision = asset.Rig.Revision;
            m_Mode = asset.Mode;
            m_XAxis = new CharacterAnimationBlendSpaceAxisPlan(asset.XAxis);
            m_YAxis = asset.AxisCount == 2 ? new CharacterAnimationBlendSpaceAxisPlan(asset.YAxis) : null;
            m_PhasePolicy = asset.PhasePolicy;
            m_Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            if (m_Samples.Length != asset.Samples.Count)
                throw new ArgumentException("Blend Space Sample plan count does not match authoring.", nameof(samples));
            var positions = new CharacterAnimationBlendSpaceCompiledPosition[m_Samples.Length];
            for (int i = 0; i < m_Samples.Length; i++)
                positions[i] = new CharacterAnimationBlendSpaceCompiledPosition(m_Samples[i].SampleId, m_Samples[i].PositionX, m_Samples[i].PositionY);
            var solver = new CharacterAnimationBlendSpaceSolverPlan(m_Mode, positions);
            var sortedSamples = new CharacterAnimationBlendSpaceSamplePlan[m_Samples.Length];
            for (int i = 0; i < solver.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceSampleId id = solver.GetPosition(i).SampleId;
                for (int source = 0; source < m_Samples.Length; source++)
                {
                    if (m_Samples[source].SampleId.Equals(id))
                    {
                        sortedSamples[i] = m_Samples[source];
                        break;
                    }
                }
            }
            m_Samples = sortedSamples;
            for (int i = 0; i < m_Samples.Length; i++)
            {
                if (m_Samples[i].SampleId.Equals(asset.PhaseReferenceSampleId))
                    m_PhaseReferenceSampleIndex = i;
            }
            int factorCount = solver.SampleCount * solver.SampleCount;
            m_SolverFactorsX = new float[factorCount];
            m_SolverFactorsY = new float[factorCount];
            for (int i = 0; i < solver.SampleCount; i++)
            {
                for (int j = 0; j < solver.SampleCount; j++)
                {
                    int index = i * solver.SampleCount + j;
                    m_SolverFactorsX[index] = solver.GetCompiledFactorX(i, j);
                    m_SolverFactorsY[index] = solver.GetCompiledFactorY(i, j);
                }
            }
            if (m_Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D)
            {
                m_SolverMagnitudes = new float[solver.SampleCount];
                for (int i = 0; i < solver.SampleCount; i++)
                    m_SolverMagnitudes[i] = solver.GetCompiledMagnitude(i);
            }
            m_ParameterPolicies = new CharacterAnimationBlendSpaceParameterPolicyPlan[asset.PoseParameterPolicies.Count];
            for (int i = 0; i < m_ParameterPolicies.Length; i++)
                m_ParameterPolicies[i] = new CharacterAnimationBlendSpaceParameterPolicyPlan(asset.PoseParameterPolicies[i]);
            RequireValid(false);
        }

        public string PlanIdentity => m_PlanIdentity ?? string.Empty;
        public CharacterAnimationBlendSpaceId BlendSpaceId => string.IsNullOrWhiteSpace(m_BlendSpaceId) ? default : new CharacterAnimationBlendSpaceId(m_BlendSpaceId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public CharacterAnimationBlendSpaceMode Mode => m_Mode;
        public CharacterAnimationBlendSpaceAxisPlan XAxis => m_XAxis;
        public CharacterAnimationBlendSpaceAxisPlan YAxis => m_YAxis;
        public int AxisCount => Mode == CharacterAnimationBlendSpaceMode.Linear1D ? 1 : 2;
        public CharacterAnimationBlendSpacePhasePolicy PhasePolicy => m_PhasePolicy;
        public int PhaseReferenceSampleIndex => m_PhaseReferenceSampleIndex;
        public IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan> Samples => m_Samples ?? Array.Empty<CharacterAnimationBlendSpaceSamplePlan>();
        public IReadOnlyList<CharacterAnimationBlendSpaceParameterPolicyPlan> ParameterPolicies => m_ParameterPolicies ?? Array.Empty<CharacterAnimationBlendSpaceParameterPolicyPlan>();
        public float ClockDurationSeconds
        {
            get
            {
                if (PhaseReferenceSampleIndex >= 0)
                    return Samples[PhaseReferenceSampleIndex].SourceDurationSeconds;
                for (int i = 0; i < Samples.Count; i++)
                {
                    if (Samples[i].Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                        return Samples[i].SourceDurationSeconds;
                }
                throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' has no dynamic clock sample.");
            }
        }

        public CharacterAnimationBlendSpaceSolverPlan CreateSolverPlan()
        {
            var positions = new CharacterAnimationBlendSpaceCompiledPosition[Samples.Count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = new CharacterAnimationBlendSpaceCompiledPosition(Samples[i].SampleId, Samples[i].PositionX, Samples[i].PositionY);
            return CharacterAnimationBlendSpaceSolverPlan.FromCompiled(Mode, positions, m_SolverFactorsX, m_SolverFactorsY, m_SolverMagnitudes);
        }

        public CharacterAnimationBlendSpacePhasePlan CreatePhasePlan(IReadOnlyList<AnimationClipPhasePlan> clipPhasePlans) =>
            CharacterAnimationBlendSpacePhasePlan.Create(this, clipPhasePlans);

        public void RequireValid(bool requireFootFeatures)
        {
            if (!BlendSpaceId.IsValid || string.IsNullOrWhiteSpace(ContentRevision) ||
                !string.Equals(PlanIdentity, $"{BlendSpaceId.Value}@{ContentRevision}", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(RigId) || string.IsNullOrWhiteSpace(RigRevision) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), Mode) || Samples.Count == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpacePhasePolicy), PhasePolicy) ||
                AxisCount == 1 && YAxis != null || AxisCount == 2 && YAxis == null ||
                PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase && PhaseReferenceSampleIndex != -1 ||
                PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase &&
                (PhaseReferenceSampleIndex < 0 || PhaseReferenceSampleIndex >= Samples.Count ||
                 Samples[PhaseReferenceSampleIndex].Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle))
                throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' is invalid.");
            XAxis.RequireValid();
            YAxis?.RequireValid();
            var ids = new HashSet<CharacterAnimationBlendSpaceSampleId>();
            for (int i = 0; i < Samples.Count; i++)
            {
                Samples[i]?.RequireValid(requireFootFeatures);
                if (Samples[i] == null || !ids.Add(Samples[i].SampleId))
                    throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' has duplicate or missing Samples.");
                var parameterIds = new HashSet<PoseParameterId>();
                for (int parameter = 0; parameter < Samples[i].Parameters.Count; parameter++)
                {
                    CharacterAnimationBlendSpaceSampleParameter value = Samples[i].Parameters[parameter];
                    if (value == null || !value.ParameterId.IsValid || !float.IsFinite(value.Value) || !parameterIds.Add(value.ParameterId))
                        throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' Sample '{Samples[i].SampleId}' has invalid parameters.");
                }
            }
            _ = CreateSolverPlan();
        }

        public CharacterAnimationBlendSpaceSamplePlan RequireSample(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                if (Samples[i].SampleId.Equals(sampleId))
                    return Samples[i];
            }
            throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' has no Sample '{sampleId}'.");
        }

        public bool TryGetParameterPolicy(PoseParameterId parameterId, out CharacterAnimationBlendSpaceParameterPolicy policy)
        {
            for (int i = 0; i < ParameterPolicies.Count; i++)
            {
                if (!ParameterPolicies[i].ParameterId.Equals(parameterId))
                    continue;
                policy = ParameterPolicies[i].Policy;
                return true;
            }
            policy = default;
            return false;
        }
    }
}
