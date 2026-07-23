using System;
using System.Collections.Generic;
using System.Text;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class BlendSpaceAnimationPoseRequestResolver
    {
        sealed class PlanState
        {
            internal PlanState(CharacterAnimationBlendSpacePlan plan)
            {
                Plan = plan ?? throw new ArgumentNullException(nameof(plan));
                Solver = plan.CreateSolverPlan();
                Phase = plan.CreatePhasePlan();
                Weights = new CharacterAnimationBlendSpaceWeightPage(plan.Samples.Count);
                Times = new CharacterAnimationBlendSpaceTimePage(plan.Samples.Count);
            }

            internal CharacterAnimationBlendSpacePlan Plan { get; }
            internal CharacterAnimationBlendSpaceSolverPlan Solver { get; }
            internal CharacterAnimationBlendSpacePhasePlan Phase { get; }
            internal CharacterAnimationBlendSpaceWeightPage Weights { get; }
            internal CharacterAnimationBlendSpaceTimePage Times { get; }
        }

        sealed class TraceState
        {
            internal string DominantSampleId = string.Empty;
            internal double LastContinuousTime = double.NegativeInfinity;
        }

        readonly struct TraceKey : IEquatable<TraceKey>
        {
            internal TraceKey(PoseNodeId playerNodeId, int programProducerIndex)
            {
                PlayerNodeId = playerNodeId;
                ProgramProducerIndex = programProducerIndex;
            }

            internal PoseNodeId PlayerNodeId { get; }
            internal int ProgramProducerIndex { get; }
            public bool Equals(TraceKey other) =>
                PlayerNodeId.Equals(other.PlayerNodeId) && ProgramProducerIndex == other.ProgramProducerIndex;
            public override bool Equals(object obj) => obj is TraceKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(PlayerNodeId, ProgramProducerIndex);
        }

        readonly CharacterPresentationProjection m_Projection;
        readonly Dictionary<PoseNodeId, CharacterAnimationBlendSpacePlayerPlan> m_Players = new Dictionary<PoseNodeId, CharacterAnimationBlendSpacePlayerPlan>();
        readonly Dictionary<int, PlanState> m_Plans = new Dictionary<int, PlanState>();
        readonly Dictionary<TraceKey, TraceState> m_TraceStates = new Dictionary<TraceKey, TraceState>();
        readonly CharacterAnimationTracePublisher m_TracePublisher = new CharacterAnimationTracePublisher();
        readonly AnimationBlendSpacePlayerRuntimeSnapshot[] m_PlayerSnapshots;
        readonly AnimationBlendSpaceSampleRuntimeSnapshot[] m_SampleSnapshots;
        int m_PlayerSnapshotCount;
        int m_SampleSnapshotCount;

        internal BlendSpaceAnimationPoseRequestResolver(CharacterPresentationProjection projection, int sourceCapacity)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            int maximumSamples = 0;
            for (int i = 0; i < projection.BlendSpacePlayers.Count; i++)
            {
                CharacterAnimationBlendSpacePlayerPlan player = projection.BlendSpacePlayers[i];
                player.RequireValid(projection);
                m_Players.Add(player.NodeId, player);
                for (int plan = 0; plan < player.BlendSpacePlanIndices.Count; plan++)
                {
                    int index = player.BlendSpacePlanIndices[plan];
                    if (!m_Plans.ContainsKey(index))
                        m_Plans.Add(index, new PlanState(projection.BlendSpaces[index]));
                    maximumSamples = Math.Max(maximumSamples, projection.BlendSpaces[index].Samples.Count);
                }
            }
            m_PlayerSnapshots = new AnimationBlendSpacePlayerRuntimeSnapshot[checked(sourceCapacity * Math.Max(1, projection.BlendSpacePlayers.Count))];
            m_SampleSnapshots = new AnimationBlendSpaceSampleRuntimeSnapshot[checked(m_PlayerSnapshots.Length * Math.Max(1, maximumSamples))];
        }

        internal int PlayerSnapshotCount => m_PlayerSnapshotCount;
        internal int SampleSnapshotCount => m_SampleSnapshotCount;
        internal AnimationBlendSpacePlayerRuntimeSnapshot GetPlayerSnapshot(int index) =>
            index >= 0 && index < m_PlayerSnapshotCount ? m_PlayerSnapshots[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal AnimationBlendSpaceSampleRuntimeSnapshot GetSampleSnapshot(int index) =>
            index >= 0 && index < m_SampleSnapshotCount ? m_SampleSnapshots[index] : throw new ArgumentOutOfRangeException(nameof(index));

        internal void BeginFrame()
        {
            Array.Clear(m_PlayerSnapshots, 0, m_PlayerSnapshotCount);
            Array.Clear(m_SampleSnapshots, 0, m_SampleSnapshotCount);
            m_PlayerSnapshotCount = 0;
            m_SampleSnapshotCount = 0;
        }

        internal bool TryResolve(
            CharacterAnimationPresentationBindingIndex bindings,
            AnimationPoseRequestWorkspace workspace,
            PoseNodeId playerNodeId,
            AnimationChannelId animationChannelId,
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            ulong presentationRequestSequence,
            int programProducerIndex,
            float visualSampleTime,
            double continuousVisualTime,
            int cycle,
            float visualTimeScale,
            in CharacterBodyPresentationFrame bodyFrame,
            RuntimeDiagnosticsContext diagnostics,
            out AnimationSourcePoseSample sourceSample)
        {
            sourceSample = default;
            if (bindings == null || workspace == null || !playerNodeId.IsValid || !animationChannelId.IsValid ||
                !sourceId.IsValid || sourceId.SourceKind != AnimationPoseSourceKind.BlendSpace ||
                sourcePoseContinuityIdentity == 0 || presentationRequestSequence == 0 || programProducerIndex < 0 ||
                !float.IsFinite(visualSampleTime) || visualSampleTime < 0f ||
                double.IsNaN(continuousVisualTime) || double.IsInfinity(continuousVisualTime) || continuousVisualTime < 0d ||
                cycle < 0 || !float.IsFinite(visualTimeScale) || visualTimeScale < 0f || !bodyFrame.IsValid)
                throw new ArgumentException("Blend Space Animation Selection input is invalid.");
            if (!m_Players.TryGetValue(playerNodeId, out CharacterAnimationBlendSpacePlayerPlan player))
                throw new InvalidOperationException($"Pose node '{playerNodeId}' is not a compiled Blend Space Player.");
            CharacterPresentationProducerEntry producer = m_Projection.Producers[programProducerIndex];
            if (producer == null || producer.ProgramProducerIndex != programProducerIndex ||
                producer.AnimationSourceKind != AnimationPoseSourceKind.BlendSpace ||
                producer.AnimationChannelId != animationChannelId || !producer.ProducerId.Equals(sourceId.PlaybackId.ProducerId) ||
                !player.ContainsPlan(producer.BlendSpacePlanIndex) || !m_Plans.TryGetValue(producer.BlendSpacePlanIndex, out PlanState state))
                throw new InvalidOperationException("Blend Space Animation Selection does not match its Projection source map.");

            float rawX = ResolveAxis(state.Plan.XAxis.ParameterId, in bodyFrame);
            float rawY = state.Plan.AxisCount == 2 ? ResolveAxis(state.Plan.YAxis.ParameterId, in bodyFrame) : 0f;
            float x = ApplyRange(rawX, state.Plan.XAxis, player.InputRangePolicy, playerNodeId);
            float y = state.Plan.AxisCount == 2
                ? ApplyRange(rawY, state.Plan.YAxis, player.InputRangePolicy, playerNodeId)
                : 0f;
            if (!CharacterAnimationBlendSpaceWeightEvaluator.Evaluate(state.Solver, x, y, state.Weights, out CharacterAnimationBlendSpaceSolveFailure solveFailure))
                throw new InvalidOperationException($"Blend Space '{state.Plan.PlanIdentity}' weight solve failed: {solveFailure}.");
            if (!CharacterAnimationBlendSpacePhaseMapper.Map(state.Phase, continuousVisualTime, cycle, state.Times, out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase, out CharacterAnimationBlendSpacePhaseFailure phaseFailure))
                throw new InvalidOperationException($"Blend Space '{state.Plan.PlanIdentity}' phase solve failed: {phaseFailure}.");

            CharacterPresentationPosePlan posePlan = m_Projection.PosePlan;
            AnimationPoseRequestWorkspaceRow row = workspace.PrepareRow(sourceId);
            workspace.RequireCurrent(row);
            if (row.ClipCapacity < state.Plan.Samples.Count || row.ParameterCount != posePlan.Parameters.Count)
                throw new InvalidOperationException("Blend Space Animation Selection workspace does not match its Projection plan.");
            WriteParameters(state, posePlan, player, x, y, row);

            var left = new AnimationFootFeatureBlendAccumulator();
            var right = new AnimationFootFeatureBlendAccumulator();
            int clipCount = 0;
            for (int weightIndex = 0; weightIndex < state.Weights.Count; weightIndex++)
            {
                CharacterAnimationBlendSpaceSampleId sampleId = state.Weights.GetSampleId(weightIndex);
                float sampleWeight = state.Weights.GetWeight(weightIndex);
                int sampleIndex = FindSampleIndex(state.Plan, sampleId);
                CharacterAnimationBlendSpaceSamplePlan sample = state.Plan.Samples[sampleIndex];
                CharacterAnimationBlendSpaceSampleTime time = FindTime(state.Times, sampleId);
                bool looping = sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
                double continuousClipTime = looping ? cycle * (double)sample.Clip.length + time.ClipTime : time.ClipTime;
                row.Clips[row.ClipOffset + clipCount++] = new ClipSamplePlan(
                    sampleIndex,
                    sample.SampleId,
                    sample.Clip,
                    time.ClipTime,
                    continuousClipTime,
                    time.NormalizedTime,
                    sampleWeight,
                    looping);
                if (sample.HasFootFeatures)
                {
                    left.Add(sample.LeftFootFeatures.Sample(time.NormalizedTime), sampleWeight, visualTimeScale);
                    right.Add(sample.RightFootFeatures.Sample(time.NormalizedTime), sampleWeight, visualTimeScale);
                }
            }
            if (clipCount == 0)
                return false;
            workspace.RequireCurrent(row);
            var selection = new AnimationSelectionFrame(
                animationChannelId,
                sourceId,
                sourcePoseContinuityIdentity,
                presentationRequestSequence,
                programProducerIndex,
                default,
                visualSampleTime,
                continuousVisualTime,
                cycle,
                true,
                visualTimeScale,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(row.Clips, row.ClipOffset, clipCount, workspace, row.LeaseGeneration),
                new PresentationParameterPageId(row.LeaseGeneration),
                new AnimationReadOnlyBuffer<float>(row.PoseParameters, row.ParameterOffset, row.ParameterCount, workspace, row.LeaseGeneration),
                new AnimationReadOnlyBuffer<byte>(row.PoseParameterAvailability, row.ParameterOffset, row.ParameterCount, workspace, row.LeaseGeneration));
            bool hasFootFeatures = m_Projection.FootAnalysis != null && m_Projection.FootAnalysis.IsEnabled;
            sourceSample = hasFootFeatures
                ? new AnimationSourcePoseSample(selection, left.Resolve(), right.Resolve(), true)
                : new AnimationSourcePoseSample(selection, default, default, false);
            CaptureDiagnostics(
                state,
                playerNodeId,
                animationChannelId,
                sourceId,
                rawX,
                rawY,
                x,
                y,
                canonicalPhase,
                hasFootFeatures);
            PublishTrace(
                diagnostics,
                state,
                playerNodeId,
                animationChannelId,
                sourceId,
                programProducerIndex,
                visualSampleTime,
                continuousVisualTime,
                cycle,
                rawX,
                x,
                canonicalPhase);
            return true;
        }

        void PublishTrace(
            RuntimeDiagnosticsContext diagnostics,
            PlanState state,
            PoseNodeId playerNodeId,
            AnimationChannelId animationChannelId,
            AnimationPoseSourceId sourceId,
            int programProducerIndex,
            float visualSampleTime,
            double continuousVisualTime,
            int cycle,
            float rawX,
            float x,
            CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase)
        {
            int dominantIndex = 0;
            float dominantWeight = state.Weights.GetWeight(0);
            for (int i = 1; i < state.Weights.Count; i++)
            {
                float weight = state.Weights.GetWeight(i);
                if (weight > dominantWeight)
                {
                    dominantIndex = i;
                    dominantWeight = weight;
                }
            }
            string dominantSampleId = state.Weights.GetSampleId(dominantIndex).Value;
            var key = new TraceKey(playerNodeId, programProducerIndex);
            if (!m_TraceStates.TryGetValue(key, out TraceState trace))
            {
                trace = new TraceState();
                m_TraceStates.Add(key, trace);
            }
            bool dominantChanged = !string.Equals(trace.DominantSampleId, dominantSampleId, StringComparison.Ordinal);
            bool intervalElapsed = continuousVisualTime < trace.LastContinuousTime ||
                                   continuousVisualTime - trace.LastContinuousTime >= 0.5d;
            if (!dominantChanged && !intervalElapsed)
                return;
            trace.DominantSampleId = dominantSampleId;
            trace.LastContinuousTime = continuousVisualTime;
            var weights = new StringBuilder();
            for (int i = 0; i < state.Weights.Count; i++)
            {
                if (i > 0)
                    weights.Append(',');
                CharacterAnimationBlendSpaceSampleId sampleId = state.Weights.GetSampleId(i);
                CharacterAnimationBlendSpaceSampleTime time = FindTime(state.Times, sampleId);
                weights.Append(sampleId.Value)
                    .Append('=')
                    .Append(state.Weights.GetWeight(i).ToString("F3"))
                    .Append('@')
                    .Append(time.ClipTime.ToString("F3"));
            }
            m_TracePublisher.PublishBlendSpaceSample(
                diagnostics,
                animationChannelId,
                sourceId,
                playerNodeId,
                dominantSampleId,
                dominantWeight,
                visualSampleTime,
                continuousVisualTime,
                cycle,
                rawX,
                x,
                canonicalPhase.NormalizedPhase,
                weights.ToString());
        }

        void CaptureDiagnostics(
            PlanState state,
            PoseNodeId playerNodeId,
            AnimationChannelId animationChannelId,
            AnimationPoseSourceId sourceId,
            float rawX,
            float rawY,
            float x,
            float y,
            CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            bool hasFootFeatures)
        {
            if (m_PlayerSnapshotCount >= m_PlayerSnapshots.Length ||
                m_SampleSnapshotCount + state.Weights.Count > m_SampleSnapshots.Length)
                throw new InvalidOperationException("Blend Space runtime diagnostics fixed capacity was exceeded.");
            int sampleOffset = m_SampleSnapshotCount;
            for (int i = 0; i < state.Weights.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleId sampleId = state.Weights.GetSampleId(i);
                CharacterAnimationBlendSpaceSamplePlan sample = state.Plan.RequireSample(sampleId);
                CharacterAnimationBlendSpaceSampleTime time = FindTime(state.Times, sampleId);
                float weight = state.Weights.GetWeight(i);
                AnimationFootFeatureSample left = sample.HasFootFeatures
                    ? sample.LeftFootFeatures.Sample(time.NormalizedTime)
                    : default;
                AnimationFootFeatureSample right = sample.HasFootFeatures
                    ? sample.RightFootFeatures.Sample(time.NormalizedTime)
                    : default;
                m_SampleSnapshots[m_SampleSnapshotCount++] = new AnimationBlendSpaceSampleRuntimeSnapshot(
                    sampleId,
                    weight,
                    time.ClipTime,
                    time.NormalizedTime,
                    sample.HasFootFeatures,
                    sample.HasFootFeatures ? m_Projection.FootAnalysis.AnalysisSourceId : string.Empty,
                    sample.HasFootFeatures ? m_Projection.FootAnalysis.AnalysisVersion : 0,
                    sample.HasFootFeatures ? m_Projection.FootAnalysis.ArtifactContentHash : string.Empty,
                    left,
                    right);
            }
            m_PlayerSnapshots[m_PlayerSnapshotCount++] = new AnimationBlendSpacePlayerRuntimeSnapshot(
                playerNodeId,
                animationChannelId,
                sourceId,
                state.Plan.BlendSpaceId,
                state.Plan.ContentRevision,
                state.Plan.Mode,
                rawX,
                rawY,
                x,
                y,
                canonicalPhase,
                hasFootFeatures,
                sampleOffset,
                state.Weights.Count);
        }

        static void WriteParameters(
            PlanState state,
            CharacterPresentationPosePlan posePlan,
            CharacterAnimationBlendSpacePlayerPlan player,
            float x,
            float y,
            AnimationPoseRequestWorkspaceRow row)
        {
            for (int parameterIndex = 0; parameterIndex < posePlan.Parameters.Count; parameterIndex++)
            {
                float value;
                bool available;
                if (parameterIndex == player.XParameterIndex)
                {
                    value = x;
                    available = true;
                }
                else if (parameterIndex == player.YParameterIndex)
                {
                    value = y;
                    available = true;
                }
                else
                    available = TryResolveSourceParameter(state, posePlan.Parameters[parameterIndex].ParameterId, out value);
                row.PoseParameters[row.ParameterOffset + parameterIndex] = value;
                row.PoseParameterAvailability[row.ParameterOffset + parameterIndex] = available ? (byte)1 : (byte)0;
            }
        }

        static bool TryResolveSourceParameter(PlanState state, PoseParameterId parameterId, out float result)
        {
            if (!state.Plan.TryGetParameterPolicy(parameterId, out CharacterAnimationBlendSpaceParameterPolicy policy))
                throw new InvalidOperationException($"Blend Space '{state.Plan.PlanIdentity}' has no policy for Pose Parameter '{parameterId}'.");
            if (policy == CharacterAnimationBlendSpaceParameterPolicy.Unavailable)
            {
                result = 0f;
                return false;
            }
            float weighted = 0f;
            float availableWeight = 0f;
            for (int i = 0; i < state.Weights.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleId sampleId = state.Weights.GetSampleId(i);
                float sampleWeight = state.Weights.GetWeight(i);
                CharacterAnimationBlendSpaceSamplePlan sample = state.Plan.RequireSample(sampleId);
                if (!sample.TryGetParameter(parameterId, out float value))
                {
                    if (policy == CharacterAnimationBlendSpaceParameterPolicy.RequireAllSamplesWeighted)
                        throw new InvalidOperationException($"Blend Space '{state.Plan.PlanIdentity}' active Sample '{sample.SampleId}' has no Parameter '{parameterId}'.");
                    continue;
                }
                weighted += value * sampleWeight;
                availableWeight += sampleWeight;
            }
            if (!float.IsFinite(weighted) || !float.IsFinite(availableWeight) || availableWeight <= 0f)
                throw new InvalidOperationException($"Blend Space '{state.Plan.PlanIdentity}' cannot resolve Parameter '{parameterId}'.");
            result = weighted / availableWeight;
            return true;
        }

        static int FindSampleIndex(CharacterAnimationBlendSpacePlan plan, CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < plan.Samples.Count; i++)
            {
                if (plan.Samples[i].SampleId.Equals(sampleId))
                    return i;
            }
            throw new InvalidOperationException($"Blend Space '{plan.PlanIdentity}' weight references an unknown Sample '{sampleId}'.");
        }

        static CharacterAnimationBlendSpaceSampleTime FindTime(
            CharacterAnimationBlendSpaceTimePage times,
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < times.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleTime time = times.Get(i);
                if (time.SampleId.Equals(sampleId))
                    return time;
            }
            throw new InvalidOperationException($"Blend Space time page has no Sample '{sampleId}'.");
        }

        static float ResolveAxis(PoseParameterId parameterId, in CharacterBodyPresentationFrame bodyFrame)
        {
            Vector3 localVelocity = Quaternion.Inverse(bodyFrame.VisibleRotation) * bodyFrame.VisibleVelocity;
            if (parameterId.Equals(AnimationPoseParameterIds.MotorPlanarSpeed))
                return new Vector2(bodyFrame.VisibleVelocity.x, bodyFrame.VisibleVelocity.z).magnitude;
            if (parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityX))
                return localVelocity.x;
            if (parameterId.Equals(AnimationPoseParameterIds.MotorLocalVelocityY))
                return localVelocity.z;
            throw new InvalidOperationException($"Blend Space axis Parameter '{parameterId}' has no formal Character Motor source.");
        }

        static float ApplyRange(
            float value,
            CharacterAnimationBlendSpaceAxisPlan axis,
            CharacterAnimationBlendSpaceInputRangePolicy policy,
            PoseNodeId nodeId)
        {
            if (!float.IsFinite(value) || axis == null)
                throw new InvalidOperationException($"Blend Space Player '{nodeId}' received an invalid axis value.");
            if (value >= axis.Minimum && value <= axis.Maximum)
                return value;
            if (policy == CharacterAnimationBlendSpaceInputRangePolicy.Clamp)
                return Mathf.Clamp(value, axis.Minimum, axis.Maximum);
            if (policy == CharacterAnimationBlendSpaceInputRangePolicy.Reject)
                throw new InvalidOperationException($"Blend Space Player '{nodeId}' Parameter '{axis.ParameterId}' value {value} is outside [{axis.Minimum}, {axis.Maximum}].");
            throw new InvalidOperationException($"Blend Space Player '{nodeId}' has an invalid input range policy.");
        }
    }
}
