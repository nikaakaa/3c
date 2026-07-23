using System;
using System.Collections.Generic;
using UnityEngine;

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
                string.IsNullOrWhiteSpace(axis.Unit) || !float.IsFinite(axis.Minimum) || !float.IsFinite(axis.Maximum) || axis.Minimum >= axis.Maximum)
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
            if (!ParameterId.IsValid || ValueType != PoseParameterValueType.Float || string.IsNullOrWhiteSpace(Unit) ||
                !float.IsFinite(Minimum) || !float.IsFinite(Maximum) || Minimum >= Maximum)
                throw new InvalidOperationException("Blend Space axis plan is invalid.");
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceMarkerPlanPayload
    {
        [SerializeField] string m_MarkerId = string.Empty;
        [SerializeField] float m_NormalizedTime;

        internal CharacterAnimationBlendSpaceMarkerPlanPayload(CharacterAnimationBlendSpaceMarker marker)
        {
            m_MarkerId = marker?.MarkerId ?? throw new ArgumentNullException(nameof(marker));
            m_NormalizedTime = marker.NormalizedTime;
        }

        public string MarkerId => m_MarkerId ?? string.Empty;
        public float NormalizedTime => m_NormalizedTime;
        internal CharacterAnimationBlendSpaceMarkerPlan ToPlan() => new CharacterAnimationBlendSpaceMarkerPlan(MarkerId, NormalizedTime);
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceSamplePlan
    {
        [SerializeField] string m_SampleId = string.Empty;
        [SerializeField] AnimationClip m_Clip;
        [SerializeField] string m_ClipContentIdentity = string.Empty;
        [SerializeField] float m_PositionX;
        [SerializeField] float m_PositionY;
        [SerializeField] CharacterAnimationBlendSpaceSampleRole m_Role;
        [SerializeField] float m_StationaryNormalizedTime;
        [SerializeField] CharacterAnimationBlendSpaceMarkerPlanPayload[] m_Markers = Array.Empty<CharacterAnimationBlendSpaceMarkerPlanPayload>();
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;
        [SerializeField] CharacterAnimationBlendSpaceSampleParameter[] m_Parameters = Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        internal CharacterAnimationBlendSpaceSamplePlan(
            CharacterAnimationBlendSpaceSample sample,
            AnimationFootFeaturePair footFeatures)
        {
            if (sample == null || !sample.SampleId.IsValid || !sample.Clip || string.IsNullOrWhiteSpace(sample.ClipContentIdentity))
                throw new ArgumentException("Blend Space Sample plan input is invalid.", nameof(sample));
            m_SampleId = sample.SampleId.Value;
            m_Clip = sample.Clip;
            m_ClipContentIdentity = sample.ClipContentIdentity;
            m_PositionX = sample.Position.x;
            m_PositionY = sample.Position.y;
            m_Role = sample.Role;
            m_StationaryNormalizedTime = sample.StationaryNormalizedTime;
            m_Markers = new CharacterAnimationBlendSpaceMarkerPlanPayload[sample.Markers.Count];
            for (int i = 0; i < m_Markers.Length; i++)
                m_Markers[i] = new CharacterAnimationBlendSpaceMarkerPlanPayload(sample.Markers[i]);
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
        public AnimationClip Clip => m_Clip;
        public string ClipContentIdentity => m_ClipContentIdentity ?? string.Empty;
        public float PositionX => m_PositionX;
        public float PositionY => m_PositionY;
        public CharacterAnimationBlendSpaceSampleRole Role => m_Role;
        public float StationaryNormalizedTime => m_StationaryNormalizedTime;
        public IReadOnlyList<CharacterAnimationBlendSpaceMarkerPlanPayload> Markers => m_Markers ?? Array.Empty<CharacterAnimationBlendSpaceMarkerPlanPayload>();
        public AnimationFootFeatureCurveSet LeftFootFeatures => m_LeftFootFeatures;
        public AnimationFootFeatureCurveSet RightFootFeatures => m_RightFootFeatures;
        public bool HasFootFeatures => m_LeftFootFeatures != null && m_RightFootFeatures != null;
        public IReadOnlyList<CharacterAnimationBlendSpaceSampleParameter> Parameters => m_Parameters ?? Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        public bool TryGetParameter(PoseParameterId parameterId, out float value)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].ParameterId.Equals(parameterId))
                {
                    value = Parameters[i].Value;
                    return true;
                }
            }
            value = 0f;
            return false;
        }

        public void RequireValid(bool requireFootFeatures)
        {
            if (!SampleId.IsValid || !Clip || !float.IsFinite(Clip.length) || Clip.length <= 0f ||
                string.IsNullOrWhiteSpace(ClipContentIdentity) || !float.IsFinite(PositionX) || !float.IsFinite(PositionY) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), Role) ||
                !float.IsFinite(StationaryNormalizedTime) || StationaryNormalizedTime < 0f || StationaryNormalizedTime > 1f ||
                requireFootFeatures && !HasFootFeatures)
                throw new InvalidOperationException($"Blend Space Sample plan '{SampleId}' is invalid.");
        }

        internal CharacterAnimationBlendSpaceSamplePhasePlan CreatePhasePlan()
        {
            var markers = new CharacterAnimationBlendSpaceMarkerPlan[Markers.Count];
            for (int i = 0; i < markers.Length; i++)
                markers[i] = Markers[i].ToPlan();
            return new CharacterAnimationBlendSpaceSamplePhasePlan(SampleId, Role, Clip.length, StationaryNormalizedTime, markers);
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
        [SerializeField] AnimationMarkerSyncBinding m_MarkerSync = new AnimationMarkerSyncBinding();

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
            m_PhaseReferenceSampleIndex = -1;
            var positions = new CharacterAnimationBlendSpaceCompiledPosition[m_Samples.Length];
            for (int i = 0; i < m_Samples.Length; i++)
            {
                positions[i] = new CharacterAnimationBlendSpaceCompiledPosition(m_Samples[i].SampleId, m_Samples[i].PositionX, m_Samples[i].PositionY);
                if (m_Samples[i].SampleId.Equals(asset.PhaseReferenceSampleId))
                    m_PhaseReferenceSampleIndex = i;
            }
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
            m_PhaseReferenceSampleIndex = -1;
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
            m_MarkerSync = BuildMarkerSync();
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
        public AnimationMarkerSyncBinding MarkerSync => m_MarkerSync;
        public float ClockDurationSeconds
        {
            get
            {
                if (PhaseReferenceSampleIndex >= 0)
                    return Samples[PhaseReferenceSampleIndex].Clip.length;
                for (int i = 0; i < Samples.Count; i++)
                {
                    if (Samples[i].Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                        return Samples[i].Clip.length;
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

        public CharacterAnimationBlendSpacePhasePlan CreatePhasePlan()
        {
            var samples = new CharacterAnimationBlendSpaceSamplePhasePlan[Samples.Count];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Samples[i].CreatePhasePlan();
            return new CharacterAnimationBlendSpacePhasePlan(PhasePolicy, PhaseReferenceSampleIndex, samples);
        }

        public void RequireValid(bool requireFootFeatures)
        {
            if (!BlendSpaceId.IsValid || string.IsNullOrWhiteSpace(ContentRevision) ||
                !string.Equals(PlanIdentity, $"{BlendSpaceId.Value}@{ContentRevision}", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(RigId) || string.IsNullOrWhiteSpace(RigRevision) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), Mode) || Samples.Count == 0 ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpacePhasePolicy), PhasePolicy) ||
                AxisCount == 1 && YAxis != null || AxisCount == 2 && YAxis == null ||
                PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase && PhaseReferenceSampleIndex != -1 ||
                PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.MarkerSynchronizedPhase &&
                (PhaseReferenceSampleIndex < 0 || PhaseReferenceSampleIndex >= Samples.Count))
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
            _ = CreatePhasePlan();
            string markerError = string.Empty;
            if (MarkerSync == null || !MarkerSync.TryValidate(out markerError) ||
                PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.MarkerSynchronizedPhase != MarkerSync.IsMarkerGroup)
                throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' marker source is invalid: {markerError}");
        }

        AnimationMarkerSyncBinding BuildMarkerSync()
        {
            if (PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase)
                return new AnimationMarkerSyncBinding();
            CharacterAnimationBlendSpaceSamplePlan reference = m_Samples[m_PhaseReferenceSampleIndex];
            float duration = reference.Clip.length;
            var markers = new AnimationMarkerSyncMarkerBinding[reference.Markers.Count];
            var segments = new AnimationMarkerSyncSegmentOccurrence[reference.Markers.Count];
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < markers.Length; i++)
            {
                CharacterAnimationBlendSpaceMarkerPlanPayload marker = reference.Markers[i];
                markers[i] = new AnimationMarkerSyncMarkerBinding(
                    $"{BlendSpaceId.Value}/{reference.SampleId.Value}/{i}",
                    marker.MarkerId,
                    i + 1,
                    marker.NormalizedTime * duration);
            }
            for (int i = 0; i < segments.Length; i++)
            {
                int next = (i + 1) % markers.Length;
                string key = BTSMTL.Timeline.AnimationMarkerSyncAuthoring.PairKey(markers[i].MarkerId, markers[next].MarkerId);
                occurrences.TryGetValue(key, out int occurrence);
                occurrences[key] = occurrence + 1;
                float end = next == 0 ? duration + markers[next].TimeSeconds : markers[next].TimeSeconds;
                segments[i] = new AnimationMarkerSyncSegmentOccurrence(
                    occurrence,
                    i,
                    next,
                    markers[i].MarkerId,
                    markers[next].MarkerId,
                    markers[i].TimeSeconds,
                    end,
                    next == 0);
            }
            return new AnimationMarkerSyncBinding(
                BTSMTL.Timeline.AnimationSyncMode.MarkerGroup,
                BuildCanonicalMarkerGroup(markers),
                BTSMTL.Timeline.AnimationMarkerSequenceTopology.Cyclic,
                BTSMTL.Timeline.AnimationMarkerSyncRole.CanBeLeader,
                markers.Length + 1,
                duration,
                markers,
                segments);
        }

        static string BuildCanonicalMarkerGroup(IReadOnlyList<AnimationMarkerSyncMarkerBinding> markers)
        {
            var builder = new System.Text.StringBuilder("blend-space-marker-topology:");
            for (int i = 0; i < markers.Count; i++)
            {
                string id = markers[i].MarkerId;
                builder.Append(id.Length).Append(':').Append(id).Append(';');
            }
            return builder.ToString();
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
                if (ParameterPolicies[i].ParameterId.Equals(parameterId))
                {
                    policy = ParameterPolicies[i].Policy;
                    return true;
                }
            }
            policy = default;
            return false;
        }
    }
}
