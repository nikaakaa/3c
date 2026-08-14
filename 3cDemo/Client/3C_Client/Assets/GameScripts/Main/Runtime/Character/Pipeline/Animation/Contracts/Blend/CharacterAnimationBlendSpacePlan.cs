using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
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
    public sealed class CharacterAnimationSequenceMarkerPlanPayload
    {
        [SerializeField] string m_AuthoringId = string.Empty;
        [SerializeField] string m_MarkerId = string.Empty;
        [SerializeField] int m_Frame;
        [SerializeField] float m_NormalizedTime;

        internal CharacterAnimationSequenceMarkerPlanPayload(AnimationSyncMarker marker, int durationFrame)
        {
            if (marker == null || durationFrame <= 0)
                throw new ArgumentException("Animation Sequence Marker plan input is invalid.");
            m_AuthoringId = marker.AuthoringId;
            m_MarkerId = marker.MarkerId;
            m_Frame = marker.Frame;
            m_NormalizedTime = marker.Frame / (float)durationFrame;
        }

        public string AuthoringId => m_AuthoringId ?? string.Empty;
        public string MarkerId => m_MarkerId ?? string.Empty;
        public int Frame => m_Frame;
        public float NormalizedTime => m_NormalizedTime;
        internal CharacterAnimationBlendSpaceMarkerPlan ToPlan() => new CharacterAnimationBlendSpaceMarkerPlan(MarkerId, NormalizedTime);
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceSamplePlan
    {
        [SerializeField] string m_SampleId = string.Empty;
        [SerializeField] string m_SequenceAuthoringId = string.Empty;
        [SerializeField] UnityAnimationClip m_Clip;
        [SerializeField] string m_ClipContentIdentity = string.Empty;
        [SerializeField] AnimationSyncMode m_SyncMode;
        [SerializeField] AnimationSyncTimeMapping m_TimeMapping;
        [SerializeField] string m_SyncGroupId = string.Empty;
        [SerializeField] AnimationMarkerSequenceTopology m_SequenceTopology;
        [SerializeField] AnimationMarkerSyncRole m_SyncRole;
        [SerializeField] int m_DurationFrame;
        [SerializeField] float m_PositionX;
        [SerializeField] float m_PositionY;
        [SerializeField] CharacterAnimationBlendSpaceSampleRole m_Role;
        [SerializeField] float m_StationaryNormalizedTime;
        [SerializeField] CharacterAnimationSequenceMarkerPlanPayload[] m_Markers = Array.Empty<CharacterAnimationSequenceMarkerPlanPayload>();
        [SerializeField] AnimationFootFeatureCurveSet m_LeftFootFeatures;
        [SerializeField] AnimationFootFeatureCurveSet m_RightFootFeatures;
        [SerializeField] CharacterAnimationBlendSpaceSampleParameter[] m_Parameters = Array.Empty<CharacterAnimationBlendSpaceSampleParameter>();

        internal CharacterAnimationBlendSpaceSamplePlan(
            CharacterAnimationBlendSpaceSample sample,
            AnimationFootFeaturePair footFeatures)
        {
            if (sample == null || !sample.SampleId.IsValid || !sample.Sequence)
                throw new ArgumentException("Blend Space Sample plan input is invalid.", nameof(sample));
            sample.Sequence.RequireValid();
            m_SampleId = sample.SampleId.Value;
            m_SequenceAuthoringId = sample.Sequence.AuthoringId;
            m_Clip = sample.Sequence.Clip;
            m_ClipContentIdentity = sample.Sequence.ContentRevision;
            m_SyncMode = sample.Sequence.SyncMode;
            m_TimeMapping = sample.Sequence.TimeMapping;
            m_SyncGroupId = sample.Sequence.SyncGroupId;
            m_SequenceTopology = sample.Sequence.SequenceTopology;
            m_SyncRole = sample.Sequence.SyncRole;
            m_DurationFrame = sample.Sequence.DurationFrame;
            m_PositionX = sample.Position.x;
            m_PositionY = sample.Position.y;
            m_Role = sample.Role;
            m_StationaryNormalizedTime = sample.StationaryNormalizedTime;
            m_Markers = new CharacterAnimationSequenceMarkerPlanPayload[sample.Sequence.SyncMarkers.Count];
            for (int i = 0; i < m_Markers.Length; i++)
                m_Markers[i] = new CharacterAnimationSequenceMarkerPlanPayload(sample.Sequence.SyncMarkers[i], m_DurationFrame);
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
        public string SequenceAuthoringId => m_SequenceAuthoringId ?? string.Empty;
        public UnityAnimationClip Clip => m_Clip;
        public string ClipContentIdentity => m_ClipContentIdentity ?? string.Empty;
        public AnimationSyncMode SyncMode => m_SyncMode;
        public AnimationSyncTimeMapping TimeMapping => m_TimeMapping;
        public string SyncGroupId => m_SyncGroupId ?? string.Empty;
        public AnimationMarkerSequenceTopology SequenceTopology => m_SequenceTopology;
        public AnimationMarkerSyncRole SyncRole => m_SyncRole;
        public int DurationFrame => m_DurationFrame;
        public float PositionX => m_PositionX;
        public float PositionY => m_PositionY;
        public CharacterAnimationBlendSpaceSampleRole Role => m_Role;
        public float StationaryNormalizedTime => m_StationaryNormalizedTime;
        public IReadOnlyList<CharacterAnimationSequenceMarkerPlanPayload> Markers => m_Markers ?? Array.Empty<CharacterAnimationSequenceMarkerPlanPayload>();
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
            if (!SampleId.IsValid || string.IsNullOrWhiteSpace(SequenceAuthoringId) ||
                !Clip || !float.IsFinite(Clip.length) || Clip.length <= 0f || DurationFrame <= 0 ||
                string.IsNullOrWhiteSpace(ClipContentIdentity) || !float.IsFinite(PositionX) || !float.IsFinite(PositionY) ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), Role) ||
                !float.IsFinite(StationaryNormalizedTime) || StationaryNormalizedTime < 0f || StationaryNormalizedTime > 1f ||
                requireFootFeatures && !HasFootFeatures)
                throw new InvalidOperationException($"Blend Space Sample plan '{SampleId}' is invalid.");
        }

        internal CharacterAnimationBlendSpaceSamplePhasePlan CreatePhasePlan(
            AnimationFootPhaseTimeWarpPlan footPhaseWarp)
        {
            var markers = new CharacterAnimationBlendSpaceMarkerPlan[Markers.Count];
            for (int i = 0; i < markers.Length; i++)
                markers[i] = Markers[i].ToPlan();
            return new CharacterAnimationBlendSpaceSamplePhasePlan(
                SampleId,
                Role,
                Clip.length,
                StationaryNormalizedTime,
                markers,
                footPhaseWarp);
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
        [SerializeField] AnimationFootPhaseTimeWarpPlan[] m_FootPhaseWarps =
            Array.Empty<AnimationFootPhaseTimeWarpPlan>();

        internal CharacterAnimationBlendSpacePlan(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterAnimationBlendSpaceSamplePlan[] samples,
            IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, AnimationFootPhaseTimeWarpPlan> footPhaseWarps = null)
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
            m_FootPhaseWarps = new AnimationFootPhaseTimeWarpPlan[m_Samples.Length];
            if (footPhaseWarps != null)
            {
                for (int i = 0; i < m_Samples.Length; i++)
                    footPhaseWarps.TryGetValue(m_Samples[i].SampleId, out m_FootPhaseWarps[i]);
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
                samples[i] = Samples[i].CreatePhasePlan(m_FootPhaseWarps[i]);
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
                (PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase ||
                 PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase) &&
                (PhaseReferenceSampleIndex < 0 || PhaseReferenceSampleIndex >= Samples.Count) ||
                m_FootPhaseWarps == null || m_FootPhaseWarps.Length != Samples.Count)
                throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' is invalid.");
            XAxis.RequireValid();
            YAxis?.RequireValid();
            var ids = new HashSet<CharacterAnimationBlendSpaceSampleId>();
            for (int i = 0; i < Samples.Count; i++)
            {
                Samples[i]?.RequireValid(requireFootFeatures);
                if (Samples[i] == null || !ids.Add(Samples[i].SampleId))
                    throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' has duplicate or missing Samples.");
                bool requiresWarp =
                    PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase &&
                    i != PhaseReferenceSampleIndex &&
                    Samples[i].Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
                if (requiresWarp != (m_FootPhaseWarps[i] != null))
                    throw new InvalidOperationException(
                        $"Blend Space plan '{PlanIdentity}' Sample '{Samples[i].SampleId}' Foot Phase Warp presence is invalid.");
                m_FootPhaseWarps[i]?.RequireValid();
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
                (PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase ||
                 PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase) != MarkerSync.IsMarkerGroup)
                throw new InvalidOperationException($"Blend Space plan '{PlanIdentity}' marker source is invalid: {markerError}");
        }

        AnimationMarkerSyncBinding BuildMarkerSync()
        {
            if (PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase)
                return new AnimationMarkerSyncBinding();
            return BuildSampleMarkerSync(
                BlendSpaceId,
                PhasePolicy,
                m_Samples[m_PhaseReferenceSampleIndex]);
        }

        public AnimationMarkerSyncBinding BuildSampleMarkerSync(
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            return BuildSampleMarkerSync(
                BlendSpaceId,
                PhasePolicy,
                RequireSample(sampleId));
        }

        public static AnimationMarkerSyncBinding BuildSampleMarkerSync(
            CharacterAnimationBlendSpaceId blendSpaceId,
            CharacterAnimationBlendSpacePhasePolicy phasePolicy,
            CharacterAnimationBlendSpaceSamplePlan sample)
        {
            if (!blendSpaceId.IsValid || sample == null)
                throw new ArgumentException("Blend Space Sample marker source is invalid.");
            if (phasePolicy != CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase &&
                phasePolicy != CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase)
                return new AnimationMarkerSyncBinding();
            AnimationSyncTimeMapping requiredMapping =
                phasePolicy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase
                    ? AnimationSyncTimeMapping.GeneratedFootPhase
                    : AnimationSyncTimeMapping.MarkerSegmentFraction;
            if (sample.SyncMode != AnimationSyncMode.MarkerGroup ||
                sample.TimeMapping != requiredMapping ||
                sample.SequenceTopology != AnimationMarkerSequenceTopology.Cyclic ||
                sample.Markers.Count < 2)
                throw new InvalidOperationException($"Blend Space Sample '{sample.SampleId}' Sequence Marker plan is incompatible with {phasePolicy}.");
            float duration = sample.Clip.length;
            var markers = new AnimationMarkerSyncMarkerBinding[sample.Markers.Count];
            var segments = new AnimationMarkerSyncSegmentOccurrence[markers.Length];
            for (int i = 0; i < markers.Length; i++)
            {
                CharacterAnimationSequenceMarkerPlanPayload marker = sample.Markers[i];
                markers[i] = new AnimationMarkerSyncMarkerBinding(
                    marker.AuthoringId,
                    marker.MarkerId,
                    marker.Frame,
                    marker.NormalizedTime * duration);
            }
            for (int i = 0; i < segments.Length; i++)
            {
                int next = (i + 1) % markers.Length;
                float end = next == 0 ? duration + markers[next].TimeSeconds : markers[next].TimeSeconds;
                segments[i] = new AnimationMarkerSyncSegmentOccurrence(
                    i,
                    i,
                    next,
                    markers[i].MarkerId,
                    markers[next].MarkerId,
                    markers[i].TimeSeconds,
                    end,
                    next == 0);
            }
            return new AnimationMarkerSyncBinding(
                sample.SyncMode,
                sample.TimeMapping,
                sample.SyncGroupId,
                sample.SequenceTopology,
                sample.SyncRole,
                sample.DurationFrame,
                duration,
                markers,
                segments);
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
