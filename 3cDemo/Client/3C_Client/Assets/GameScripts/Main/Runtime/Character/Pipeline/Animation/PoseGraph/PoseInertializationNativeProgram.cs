using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PoseInertializationRuntimeState : byte
    {
        Reset = 1,
        Anchor = 2,
        HardCut = 3,
        Capture = 4,
        Continue = 5,
        Rebase = 6,
        Complete = 7,
        Invalid = 8
    }

    internal readonly struct PoseInertializationNativeNode
    {
        internal PoseInertializationNativeNode(int inputPlayerIndex, int ruleOffset, int ruleCount)
        {
            InputPlayerIndex = inputPlayerIndex;
            RuleOffset = ruleOffset;
            RuleCount = ruleCount;
        }

        internal int InputPlayerIndex { get; }
        internal int RuleOffset { get; }
        internal int RuleCount { get; }
    }

    internal readonly struct PoseInertializationNativeRule
    {
        internal PoseInertializationNativeRule(
            int sourceProducerIndex,
            int targetProducerIndex,
            PoseInertializationMode mode,
            float durationSeconds,
            int curveOffset,
            int curveCount,
            int profileOffset,
            int parameterModeOffset)
        {
            SourceProducerIndex = sourceProducerIndex;
            TargetProducerIndex = targetProducerIndex;
            Mode = mode;
            DurationSeconds = durationSeconds;
            CurveOffset = curveOffset;
            CurveCount = curveCount;
            ProfileOffset = profileOffset;
            ParameterModeOffset = parameterModeOffset;
        }

        internal int SourceProducerIndex { get; }
        internal int TargetProducerIndex { get; }
        internal PoseInertializationMode Mode { get; }
        internal float DurationSeconds { get; }
        internal int CurveOffset { get; }
        internal int CurveCount { get; }
        internal int ProfileOffset { get; }
        internal int ParameterModeOffset { get; }
    }

    internal struct PoseInertializationNativeState
    {
        internal ulong LastEventIdentity;
        internal ulong AccumulatorGeneration;
        internal float ElapsedSeconds;
        internal float LastDeltaSeconds;
        internal int HistoryPage;
        internal int ActiveRuleIndex;
        internal byte HasHistory;
        internal byte Active;
        internal PoseInertializationRuntimeState RuntimeState;
        internal PoseDiscontinuityReason LastReason;
        internal PoseDiscontinuityResetReason LastResetReason;
        internal ulong LastResetSequence;
        internal PoseDiscontinuityEndpoint PreviousEndpoint;
        internal PoseDiscontinuityEndpoint CurrentEndpoint;
        internal ulong PreviousContinuityIdentity;
        internal ulong CurrentContinuityIdentity;
        internal ulong HistoryCompletionIdentity;
        internal ulong OutputCompletionIdentity;
    }

    internal sealed class PoseInertializationNativeProgram : IDisposable
    {
        NativeArray<PoseInertializationNativeNode> m_Nodes;
        NativeArray<PoseInertializationNativeRule> m_Rules;
        NativeArray<AnimationBlendCurveSegment> m_CurveSegments;
        NativeArray<float> m_DenseProfiles;
        NativeArray<PoseParameterInertializationMode> m_ParameterModes;
        NativeArray<PoseInertializationNativeState> m_States;
        NativeArray<AnimationLocalBonePose> m_HistoryPoses;
        NativeArray<AnimationBlendBoneVelocity> m_HistoryVelocities;
        NativeArray<float> m_HistoryParameters;
        NativeArray<byte> m_HistoryParameterAvailability;
        NativeArray<AnimationFootFeatureSample> m_HistoryLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_HistoryRightFeet;
        NativeArray<byte> m_HistoryHasFeet;
        NativeArray<AnimationFootFeatureSample> m_AccumulatorLeftFeet;
        NativeArray<AnimationFootFeatureSample> m_AccumulatorRightFeet;
        NativeArray<byte> m_AccumulatorHasFeet;
        NativeArray<UnityEngine.Vector3> m_PositionResiduals;
        NativeArray<UnityEngine.Vector3> m_RotationResiduals;
        NativeArray<UnityEngine.Vector3> m_ScaleResiduals;
        NativeArray<UnityEngine.Vector3> m_LinearVelocityResiduals;
        NativeArray<UnityEngine.Vector3> m_AngularVelocityResiduals;
        NativeArray<UnityEngine.Vector3> m_ScaleVelocityResiduals;
        NativeArray<float> m_ParameterResiduals;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        bool m_Disposed;

        internal PoseInertializationNativeProgram(
            CharacterPresentationPosePlan plan,
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            if (plan == null || curves == null || profiles == null)
                throw new ArgumentNullException(nameof(plan));
            plan.RequireValid();
            plan.RequireInertializationValid();
            m_BoneCount = plan.BoneCount;
            m_ParameterCount = plan.Parameters.Count;
            int ruleCount = 0;
            int segmentCount = 0;
            for (int node = 0; node < plan.Inertializations.Count; node++)
            {
                CharacterPresentationInertializationDescriptor descriptor = plan.Inertializations[node];
                ruleCount = checked(ruleCount + descriptor.Rules.Count);
                for (int rule = 0; rule < descriptor.Rules.Count; rule++)
                {
                    CharacterPresentationInertializationRuleDescriptor value = descriptor.Rules[rule];
                    if (value.Mode == PoseInertializationMode.Inertialize)
                        segmentCount = checked(segmentCount + curves.Require(value.CurveIndex).Segments.Count);
                }
            }
            m_Nodes = Allocate<PoseInertializationNativeNode>(plan.Inertializations.Count, false);
            m_Rules = Allocate<PoseInertializationNativeRule>(ruleCount, false);
            m_CurveSegments = Allocate<AnimationBlendCurveSegment>(segmentCount, false);
            m_DenseProfiles = Allocate<float>(checked(ruleCount * m_BoneCount), false);
            m_ParameterModes = Allocate<PoseParameterInertializationMode>(checked(ruleCount * m_ParameterCount), false);
            m_States = Allocate<PoseInertializationNativeState>(plan.Inertializations.Count, true);
            int historyBoneCount = checked(plan.Inertializations.Count * 2 * m_BoneCount);
            int historyParameterCount = checked(plan.Inertializations.Count * 2 * m_ParameterCount);
            m_HistoryPoses = Allocate<AnimationLocalBonePose>(historyBoneCount, true);
            m_HistoryVelocities = Allocate<AnimationBlendBoneVelocity>(historyBoneCount, true);
            m_HistoryParameters = Allocate<float>(historyParameterCount, true);
            m_HistoryParameterAvailability = Allocate<byte>(historyParameterCount, true);
            m_HistoryLeftFeet = Allocate<AnimationFootFeatureSample>(checked(plan.Inertializations.Count * 2), true);
            m_HistoryRightFeet = Allocate<AnimationFootFeatureSample>(checked(plan.Inertializations.Count * 2), true);
            m_HistoryHasFeet = Allocate<byte>(checked(plan.Inertializations.Count * 2), true);
            m_AccumulatorLeftFeet = Allocate<AnimationFootFeatureSample>(plan.Inertializations.Count, true);
            m_AccumulatorRightFeet = Allocate<AnimationFootFeatureSample>(plan.Inertializations.Count, true);
            m_AccumulatorHasFeet = Allocate<byte>(plan.Inertializations.Count, true);
            int residualBoneCount = checked(plan.Inertializations.Count * m_BoneCount);
            m_PositionResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_RotationResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ScaleResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_LinearVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_AngularVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ScaleVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ParameterResiduals = Allocate<float>(checked(plan.Inertializations.Count * m_ParameterCount), true);
            Compile(plan, curves, profiles);
        }

        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal NativeArray<PoseInertializationNativeNode> Nodes => m_Nodes;
        internal NativeArray<PoseInertializationNativeRule> Rules => m_Rules;
        internal NativeArray<AnimationBlendCurveSegment> CurveSegments => m_CurveSegments;
        internal NativeArray<float> DenseProfiles => m_DenseProfiles;
        internal NativeArray<PoseParameterInertializationMode> ParameterModes => m_ParameterModes;
        internal NativeArray<PoseInertializationNativeState> States => m_States;
        internal NativeArray<AnimationLocalBonePose> HistoryPoses => m_HistoryPoses;
        internal NativeArray<AnimationBlendBoneVelocity> HistoryVelocities => m_HistoryVelocities;
        internal NativeArray<float> HistoryParameters => m_HistoryParameters;
        internal NativeArray<byte> HistoryParameterAvailability => m_HistoryParameterAvailability;
        internal NativeArray<AnimationFootFeatureSample> HistoryLeftFeet => m_HistoryLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> HistoryRightFeet => m_HistoryRightFeet;
        internal NativeArray<byte> HistoryHasFeet => m_HistoryHasFeet;
        internal NativeArray<AnimationFootFeatureSample> AccumulatorLeftFeet => m_AccumulatorLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> AccumulatorRightFeet => m_AccumulatorRightFeet;
        internal NativeArray<byte> AccumulatorHasFeet => m_AccumulatorHasFeet;
        internal NativeArray<UnityEngine.Vector3> PositionResiduals => m_PositionResiduals;
        internal NativeArray<UnityEngine.Vector3> RotationResiduals => m_RotationResiduals;
        internal NativeArray<UnityEngine.Vector3> ScaleResiduals => m_ScaleResiduals;
        internal NativeArray<UnityEngine.Vector3> LinearVelocityResiduals => m_LinearVelocityResiduals;
        internal NativeArray<UnityEngine.Vector3> AngularVelocityResiduals => m_AngularVelocityResiduals;
        internal NativeArray<UnityEngine.Vector3> ScaleVelocityResiduals => m_ScaleVelocityResiduals;
        internal NativeArray<float> ParameterResiduals => m_ParameterResiduals;

        internal float GetBoneEnvelope(int nodeIndex, int boneIndex)
        {
            if ((uint)nodeIndex >= (uint)m_States.Length || (uint)boneIndex >= (uint)m_BoneCount)
                throw new ArgumentOutOfRangeException();
            PoseInertializationNativeState state = m_States[nodeIndex];
            if (state.Active == 0 || (uint)state.ActiveRuleIndex >= (uint)m_Rules.Length)
                return 1f;
            PoseInertializationNativeRule rule = m_Rules[state.ActiveRuleIndex];
            float duration = rule.DurationSeconds * m_DenseProfiles[rule.ProfileOffset + boneIndex];
            if (duration <= 0f || state.ElapsedSeconds >= duration)
                return 1f;
            float normalized = Mathf.Clamp01(state.ElapsedSeconds / duration);
            EvaluateCurve(rule, normalized, out float curve, out float derivative);
            EvaluateCurve(rule, 0f, out _, out float startDerivative);
            EvaluateCurve(rule, 1f, out _, out float endDerivative);
            float s2 = normalized * normalized;
            float s3 = s2 * normalized;
            float h10 = s3 - 2f * s2 + normalized;
            float h11 = s3 - s2;
            return Mathf.Clamp01(curve - startDerivative * h10 - endDerivative * h11);
        }

        void EvaluateCurve(
            PoseInertializationNativeRule rule,
            float normalizedTime,
            out float value,
            out float derivative)
        {
            float time = Mathf.Clamp01(normalizedTime);
            AnimationBlendCurveSegment segment = m_CurveSegments[rule.CurveOffset + rule.CurveCount - 1];
            for (int i = 0; i < rule.CurveCount; i++)
            {
                AnimationBlendCurveSegment candidate = m_CurveSegments[rule.CurveOffset + i];
                if (time <= candidate.EndTime)
                {
                    segment = candidate;
                    break;
                }
            }
            float u = (time - segment.StartTime) / (segment.EndTime - segment.StartTime);
            value = Mathf.Clamp01(((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
            derivative = ((3f * segment.A * u + 2f * segment.B) * u + segment.C) /
                         (segment.EndTime - segment.StartTime);
        }

        void Compile(
            CharacterPresentationPosePlan plan,
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            int ruleOffset = 0;
            int segmentOffset = 0;
            for (int node = 0; node < plan.Inertializations.Count; node++)
            {
                CharacterPresentationInertializationDescriptor descriptor = plan.Inertializations[node];
                if (descriptor.Index != node || descriptor.Rules.Count == 0)
                    throw new InvalidOperationException($"Pose Inertialization descriptor #{node} is incomplete.");
                m_Nodes[node] = new PoseInertializationNativeNode(
                    descriptor.InputPlayerIndex, ruleOffset, descriptor.Rules.Count);
                for (int ruleIndex = 0; ruleIndex < descriptor.Rules.Count; ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor source = descriptor.Rules[ruleIndex];
                    int curveOffset = -1;
                    int curveCount = 0;
                    int profileOffset = -1;
                    if (source.Mode == PoseInertializationMode.Inertialize)
                    {
                        AnimationBlendCurvePayload curve = curves.Require(source.CurveIndex);
                        curveOffset = segmentOffset;
                        curveCount = curve.Segments.Count;
                        for (int segment = 0; segment < curveCount; segment++)
                            m_CurveSegments[segmentOffset++] = curve.Segments[segment];
                        AnimationBlendProfilePayload profile = profiles.Require(source.ProfileIndex);
                        profileOffset = ruleOffset * m_BoneCount;
                        for (int bone = 0; bone < m_BoneCount; bone++)
                            m_DenseProfiles[profileOffset + bone] = profile.DenseDurationMultipliers[bone];
                    }
                    int parameterOffset = ruleOffset * m_ParameterCount;
                    if (source.ParameterModes.Count != m_ParameterCount)
                        throw new InvalidOperationException($"Pose Inertialization rule #{ruleOffset} has an incomplete Parameter filter.");
                    for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        m_ParameterModes[parameterOffset + parameter] = source.ParameterModes[parameter];
                    m_Rules[ruleOffset] = new PoseInertializationNativeRule(
                        source.SourceProgramProducerIndex,
                        source.TargetProgramProducerIndex,
                        source.Mode,
                        source.DurationSeconds,
                        curveOffset,
                        curveCount,
                        profileOffset,
                        parameterOffset);
                    ruleOffset++;
                }
            }
            if (ruleOffset != m_Rules.Length || segmentOffset != m_CurveSegments.Length)
                throw new InvalidOperationException("Pose Inertialization Native payload layout is inconsistent.");
        }

        internal void Reset()
        {
            for (int i = 0; i < m_States.Length; i++)
                m_States[i] = default;
            Clear(m_HistoryPoses);
            Clear(m_HistoryVelocities);
            Clear(m_HistoryParameters);
            Clear(m_HistoryParameterAvailability);
            Clear(m_HistoryLeftFeet);
            Clear(m_HistoryRightFeet);
            Clear(m_HistoryHasFeet);
            Clear(m_AccumulatorLeftFeet);
            Clear(m_AccumulatorRightFeet);
            Clear(m_AccumulatorHasFeet);
            Clear(m_PositionResiduals);
            Clear(m_RotationResiduals);
            Clear(m_ScaleResiduals);
            Clear(m_LinearVelocityResiduals);
            Clear(m_AngularVelocityResiduals);
            Clear(m_ScaleVelocityResiduals);
            Clear(m_ParameterResiduals);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Dispose(ref m_ParameterResiduals);
            Dispose(ref m_ScaleVelocityResiduals);
            Dispose(ref m_AngularVelocityResiduals);
            Dispose(ref m_LinearVelocityResiduals);
            Dispose(ref m_ScaleResiduals);
            Dispose(ref m_RotationResiduals);
            Dispose(ref m_PositionResiduals);
            Dispose(ref m_HistoryHasFeet);
            Dispose(ref m_AccumulatorHasFeet);
            Dispose(ref m_AccumulatorRightFeet);
            Dispose(ref m_AccumulatorLeftFeet);
            Dispose(ref m_HistoryRightFeet);
            Dispose(ref m_HistoryLeftFeet);
            Dispose(ref m_HistoryParameters);
            Dispose(ref m_HistoryParameterAvailability);
            Dispose(ref m_HistoryVelocities);
            Dispose(ref m_HistoryPoses);
            Dispose(ref m_States);
            Dispose(ref m_ParameterModes);
            Dispose(ref m_DenseProfiles);
            Dispose(ref m_CurveSegments);
            Dispose(ref m_Rules);
            Dispose(ref m_Nodes);
        }

        static NativeArray<T> Allocate<T>(int length, bool clear) where T : struct =>
            new NativeArray<T>(length, Allocator.Persistent,
                clear ? NativeArrayOptions.ClearMemory : NativeArrayOptions.UninitializedMemory);

        static void Clear<T>(NativeArray<T> values) where T : struct
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = default;
        }

        static void Dispose<T>(ref NativeArray<T> values) where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
