using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        internal PoseInertializationNativeNode(
            PoseInertializationTemporalOwnerKind temporalOwnerKind,
            int controlIndex,
            int ruleOffset,
            int ruleCount)
        {
            TemporalOwnerKind = temporalOwnerKind;
            ControlIndex = controlIndex;
            RuleOffset = ruleOffset;
            RuleCount = ruleCount;
        }

        internal PoseInertializationTemporalOwnerKind TemporalOwnerKind { get; }
        internal int ControlIndex { get; }
        internal int RuleOffset { get; }
        internal int RuleCount { get; }
    }

    internal readonly struct PoseInertializationNativeRule
    {
        internal PoseInertializationNativeRule(
            int sourceEndpointIndex,
            int targetEndpointIndex,
            PoseInertializationMode mode,
            float durationSeconds,
            int curveOffset,
            int curveCount,
            int profileOffset,
            int parameterModeOffset)
        {
            SourceEndpointIndex = sourceEndpointIndex;
            TargetEndpointIndex = targetEndpointIndex;
            Mode = mode;
            DurationSeconds = durationSeconds;
            CurveOffset = curveOffset;
            CurveCount = curveCount;
            ProfileOffset = profileOffset;
            ParameterModeOffset = parameterModeOffset;
        }

        internal int SourceEndpointIndex { get; }
        internal int TargetEndpointIndex { get; }
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
        internal PoseDiscontinuityNativeEndpoint PreviousEndpoint;
        internal PoseDiscontinuityNativeEndpoint CurrentEndpoint;
        internal ulong PreviousContinuityIdentity;
        internal ulong CurrentContinuityIdentity;
        internal ulong HistoryCompletionIdentity;
        internal ulong OutputCompletionIdentity;
    }

    internal sealed class PoseInertializationNativeProgram : IDisposable
    {
        sealed class Page
        {
            internal NativeArray<PoseInertializationNativeState> States;
            internal NativeArray<AnimationLocalBonePose> HistoryPoses;
            internal NativeArray<AnimationBlendBoneVelocity>
                HistoryVelocities;
            internal NativeArray<float> HistoryParameters;
            internal NativeArray<byte> HistoryParameterAvailability;
            internal NativeArray<AnimationFootFeatureSample>
                HistoryLeftFeet;
            internal NativeArray<AnimationFootFeatureSample>
                HistoryRightFeet;
            internal NativeArray<byte> HistoryHasFeet;
            internal NativeArray<AnimationFootFeatureSample>
                AccumulatorLeftFeet;
            internal NativeArray<AnimationFootFeatureSample>
                AccumulatorRightFeet;
            internal NativeArray<byte> AccumulatorHasFeet;
            internal NativeArray<Vector3> PositionResiduals;
            internal NativeArray<Vector3> RotationResiduals;
            internal NativeArray<Vector3> ScaleResiduals;
            internal NativeArray<Vector3> LinearVelocityResiduals;
            internal NativeArray<Vector3> AngularVelocityResiduals;
            internal NativeArray<Vector3> ScaleVelocityResiduals;
            internal NativeArray<float> ParameterResiduals;
        }

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
        Page m_CommittedPage;
        Page m_PendingPage;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_SlotNodeOffset;
        bool m_FrameOpen;
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
            m_BoneCount = plan.PoseBoneCount;
            m_ParameterCount = plan.Parameters.Count;
            m_SlotNodeOffset = plan.Inertializations.Count;
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
            for (int slot = 0; slot < plan.AnimationSlots.Count; slot++)
            {
                CharacterAnimationSlotDescriptor descriptor = plan.AnimationSlots[slot];
                ruleCount = checked(ruleCount + descriptor.RequestRoutes.Count);
                for (int route = 0; route < descriptor.RequestRoutes.Count; route++)
                {
                    CharacterAnimationSlotRequestRouteDescriptor value = descriptor.RequestRoutes[route];
                    if (value.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
                        segmentCount = checked(segmentCount + curves.Require(value.CurveIndex).Segments.Count);
                }
            }
            int nodeCount = checked(plan.Inertializations.Count + plan.AnimationSlots.Count);
            m_Nodes = Allocate<PoseInertializationNativeNode>(nodeCount, false);
            m_Rules = Allocate<PoseInertializationNativeRule>(ruleCount, false);
            m_CurveSegments = Allocate<AnimationBlendCurveSegment>(segmentCount, false);
            m_DenseProfiles = Allocate<float>(checked(ruleCount * m_BoneCount), false);
            m_ParameterModes = Allocate<PoseParameterInertializationMode>(checked(ruleCount * m_ParameterCount), false);
            m_States = Allocate<PoseInertializationNativeState>(nodeCount, true);
            int historyBoneCount = checked(nodeCount * 2 * m_BoneCount);
            int historyParameterCount = checked(nodeCount * 2 * m_ParameterCount);
            m_HistoryPoses = Allocate<AnimationLocalBonePose>(historyBoneCount, true);
            m_HistoryVelocities = Allocate<AnimationBlendBoneVelocity>(historyBoneCount, true);
            m_HistoryParameters = Allocate<float>(historyParameterCount, true);
            m_HistoryParameterAvailability = Allocate<byte>(historyParameterCount, true);
            m_HistoryLeftFeet = Allocate<AnimationFootFeatureSample>(checked(nodeCount * 2), true);
            m_HistoryRightFeet = Allocate<AnimationFootFeatureSample>(checked(nodeCount * 2), true);
            m_HistoryHasFeet = Allocate<byte>(checked(nodeCount * 2), true);
            m_AccumulatorLeftFeet = Allocate<AnimationFootFeatureSample>(nodeCount, true);
            m_AccumulatorRightFeet = Allocate<AnimationFootFeatureSample>(nodeCount, true);
            m_AccumulatorHasFeet = Allocate<byte>(nodeCount, true);
            int residualBoneCount = checked(nodeCount * m_BoneCount);
            m_PositionResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_RotationResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ScaleResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_LinearVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_AngularVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ScaleVelocityResiduals = Allocate<UnityEngine.Vector3>(residualBoneCount, true);
            m_ParameterResiduals = Allocate<float>(checked(nodeCount * m_ParameterCount), true);
            m_CommittedPage = CaptureActivePage();
            m_PendingPage = AllocatePage(nodeCount);
            Compile(plan, curves, profiles);
        }

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Pose Inertialization frame is already open.");
            BindPage(m_PendingPage);
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            Page previousCommitted = m_CommittedPage;
            m_CommittedPage = m_PendingPage;
            m_PendingPage = previousCommitted;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            BindPage(m_CommittedPage);
            m_FrameOpen = false;
        }

        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal int SlotNodeOffset => m_SlotNodeOffset;
        internal NativeArray<PoseInertializationNativeNode> Nodes => m_Nodes;
        internal NativeArray<PoseInertializationNativeRule> Rules => m_Rules;
        internal NativeArray<AnimationBlendCurveSegment> CurveSegments => m_CurveSegments;
        internal NativeArray<float> DenseProfiles => m_DenseProfiles;
        internal NativeArray<PoseParameterInertializationMode> ParameterModes => m_ParameterModes;
        internal bool HasOpenFrame => m_FrameOpen;
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
        internal NativeArray<PoseInertializationNativeState> CommittedStates => m_CommittedPage.States;
        internal NativeArray<AnimationLocalBonePose> CommittedHistoryPoses => m_CommittedPage.HistoryPoses;
        internal NativeArray<AnimationBlendBoneVelocity> CommittedHistoryVelocities => m_CommittedPage.HistoryVelocities;
        internal NativeArray<float> CommittedHistoryParameters => m_CommittedPage.HistoryParameters;
        internal NativeArray<byte> CommittedHistoryParameterAvailability => m_CommittedPage.HistoryParameterAvailability;
        internal NativeArray<AnimationFootFeatureSample> CommittedHistoryLeftFeet => m_CommittedPage.HistoryLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> CommittedHistoryRightFeet => m_CommittedPage.HistoryRightFeet;
        internal NativeArray<byte> CommittedHistoryHasFeet => m_CommittedPage.HistoryHasFeet;
        internal NativeArray<AnimationFootFeatureSample> CommittedAccumulatorLeftFeet => m_CommittedPage.AccumulatorLeftFeet;
        internal NativeArray<AnimationFootFeatureSample> CommittedAccumulatorRightFeet => m_CommittedPage.AccumulatorRightFeet;
        internal NativeArray<byte> CommittedAccumulatorHasFeet => m_CommittedPage.AccumulatorHasFeet;
        internal NativeArray<Vector3> CommittedPositionResiduals => m_CommittedPage.PositionResiduals;
        internal NativeArray<Vector3> CommittedRotationResiduals => m_CommittedPage.RotationResiduals;
        internal NativeArray<Vector3> CommittedScaleResiduals => m_CommittedPage.ScaleResiduals;
        internal NativeArray<Vector3> CommittedLinearVelocityResiduals => m_CommittedPage.LinearVelocityResiduals;
        internal NativeArray<Vector3> CommittedAngularVelocityResiduals => m_CommittedPage.AngularVelocityResiduals;
        internal NativeArray<Vector3> CommittedScaleVelocityResiduals => m_CommittedPage.ScaleVelocityResiduals;
        internal NativeArray<float> CommittedParameterResiduals => m_CommittedPage.ParameterResiduals;

        internal PoseInertializationNativeState GetAnimationSlotState(int animationSlotIndex)
        {
            if (animationSlotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(animationSlotIndex));
            int stateIndex = checked(m_SlotNodeOffset + animationSlotIndex);
            if ((uint)stateIndex >= (uint)m_States.Length)
                throw new ArgumentOutOfRangeException(nameof(animationSlotIndex));
            return m_States[stateIndex];
        }

        internal bool TryGetStateMachineState(
            int stateMachineIndex,
            out PoseInertializationNativeState state)
        {
            if (stateMachineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(stateMachineIndex));
            for (int nodeIndex = 0; nodeIndex < m_SlotNodeOffset; nodeIndex++)
            {
                if (m_Nodes[nodeIndex].ControlIndex == stateMachineIndex)
                {
                    state = m_States[nodeIndex];
                    return true;
                }
            }
            state = default;
            return false;
        }

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
                int controlIndex = descriptor.TemporalOwnerKind ==
                                   PoseInertializationTemporalOwnerKind.StateMachineTransition
                    ? descriptor.InputOwnerIndex
                    : -1;
                m_Nodes[node] = new PoseInertializationNativeNode(
                    descriptor.TemporalOwnerKind,
                    controlIndex,
                    ruleOffset,
                    descriptor.Rules.Count);
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
                            m_DenseProfiles[profileOffset + bone] =
                                profile.GlobalDurationMultiplier * profile.DenseDurationMultipliers[bone];
                    }
                    int parameterOffset = ruleOffset * m_ParameterCount;
                    if (source.ParameterModes.Count != m_ParameterCount)
                        throw new InvalidOperationException($"Pose Inertialization rule #{ruleOffset} has an incomplete Parameter filter.");
                    for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        m_ParameterModes[parameterOffset + parameter] = source.ParameterModes[parameter];
                    m_Rules[ruleOffset] = new PoseInertializationNativeRule(
                        source.SourceEndpointIndex,
                        source.TargetEndpointIndex,
                        source.Mode,
                        source.DurationSeconds,
                        curveOffset,
                        curveCount,
                        profileOffset,
                        parameterOffset);
                    ruleOffset++;
                }
            }
            for (int slot = 0; slot < plan.AnimationSlots.Count; slot++)
            {
                CharacterAnimationSlotDescriptor descriptor = plan.AnimationSlots[slot];
                var endpoints = new Dictionary<TransitionEndpointId, CharacterAnimationSlotEndpointDescriptor>();
                for (int endpoint = 0; endpoint < descriptor.Endpoints.Count; endpoint++)
                    endpoints.Add(descriptor.Endpoints[endpoint].EndpointId, descriptor.Endpoints[endpoint]);
                int nodeIndex = m_SlotNodeOffset + slot;
                m_Nodes[nodeIndex] = new PoseInertializationNativeNode(
                    default,
                    descriptor.ActionPlayer.PlayerIndex,
                    ruleOffset,
                    descriptor.RequestRoutes.Count);
                for (int routeIndex = 0; routeIndex < descriptor.RequestRoutes.Count; routeIndex++)
                {
                    CharacterAnimationSlotRequestRouteDescriptor route = descriptor.RequestRoutes[routeIndex];
                    CharacterAnimationSlotEndpointDescriptor source = endpoints[route.SourceEndpointId];
                    CharacterAnimationSlotEndpointDescriptor target = endpoints[route.TargetEndpointId];
                    PoseInertializationMode mode =
                        route.BlendLogic == AnimationTransitionBlendLogic.Inertialization
                            ? PoseInertializationMode.Inertialize
                            : PoseInertializationMode.HardCut;
                    int curveOffset = -1;
                    int curveCount = 0;
                    int profileOffset = -1;
                    if (mode == PoseInertializationMode.Inertialize)
                    {
                        AnimationBlendCurvePayload curve = curves.Require(route.CurveIndex);
                        curveOffset = segmentOffset;
                        curveCount = curve.Segments.Count;
                        for (int segment = 0; segment < curveCount; segment++)
                            m_CurveSegments[segmentOffset++] = curve.Segments[segment];
                        AnimationBlendProfilePayload profile = profiles.Require(route.BlendProfileIndex);
                        profileOffset = ruleOffset * m_BoneCount;
                        for (int bone = 0; bone < m_BoneCount; bone++)
                            m_DenseProfiles[profileOffset + bone] =
                                profile.GlobalDurationMultiplier * profile.DenseDurationMultipliers[bone];
                    }
                    int parameterOffset = ruleOffset * m_ParameterCount;
                    for (int parameter = 0; parameter < m_ParameterCount; parameter++)
                        m_ParameterModes[parameterOffset + parameter] = PoseParameterInertializationMode.Snap;
                    m_Rules[ruleOffset] = new PoseInertializationNativeRule(
                        source.ProgramProducerIndex,
                        target.ProgramProducerIndex,
                        mode,
                        mode == PoseInertializationMode.Inertialize ? route.DurationSeconds : 0f,
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
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Pose Inertialization frame must close before reset.");
            ClearPage(m_CommittedPage);
            ClearPage(m_PendingPage);
            BindPage(m_CommittedPage);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_CommittedPage != null)
                DisposePage(m_CommittedPage);
            else
                DisposePage(CaptureActivePage());
            DisposePage(m_PendingPage);
            m_CommittedPage = null;
            m_PendingPage = null;
            Dispose(ref m_ParameterModes);
            Dispose(ref m_DenseProfiles);
            Dispose(ref m_CurveSegments);
            Dispose(ref m_Rules);
            Dispose(ref m_Nodes);
        }

        static NativeArray<T> Allocate<T>(int length, bool clear) where T : unmanaged =>
            new NativeArray<T>(length, Allocator.Persistent,
                clear ? NativeArrayOptions.ClearMemory : NativeArrayOptions.UninitializedMemory);

        Page CaptureActivePage() => new Page
        {
            States = m_States,
            HistoryPoses = m_HistoryPoses,
            HistoryVelocities = m_HistoryVelocities,
            HistoryParameters = m_HistoryParameters,
            HistoryParameterAvailability = m_HistoryParameterAvailability,
            HistoryLeftFeet = m_HistoryLeftFeet,
            HistoryRightFeet = m_HistoryRightFeet,
            HistoryHasFeet = m_HistoryHasFeet,
            AccumulatorLeftFeet = m_AccumulatorLeftFeet,
            AccumulatorRightFeet = m_AccumulatorRightFeet,
            AccumulatorHasFeet = m_AccumulatorHasFeet,
            PositionResiduals = m_PositionResiduals,
            RotationResiduals = m_RotationResiduals,
            ScaleResiduals = m_ScaleResiduals,
            LinearVelocityResiduals = m_LinearVelocityResiduals,
            AngularVelocityResiduals = m_AngularVelocityResiduals,
            ScaleVelocityResiduals = m_ScaleVelocityResiduals,
            ParameterResiduals = m_ParameterResiduals
        };

        Page AllocatePage(int nodeCount)
        {
            var page = new Page();
            try
            {
                page.States = Allocate<PoseInertializationNativeState>(nodeCount, true);
                int historyBoneCount = checked(nodeCount * 2 * m_BoneCount);
                int historyParameterCount = checked(nodeCount * 2 * m_ParameterCount);
                page.HistoryPoses = Allocate<AnimationLocalBonePose>(historyBoneCount, true);
                page.HistoryVelocities = Allocate<AnimationBlendBoneVelocity>(historyBoneCount, true);
                page.HistoryParameters = Allocate<float>(historyParameterCount, true);
                page.HistoryParameterAvailability = Allocate<byte>(historyParameterCount, true);
                page.HistoryLeftFeet = Allocate<AnimationFootFeatureSample>(checked(nodeCount * 2), true);
                page.HistoryRightFeet = Allocate<AnimationFootFeatureSample>(checked(nodeCount * 2), true);
                page.HistoryHasFeet = Allocate<byte>(checked(nodeCount * 2), true);
                page.AccumulatorLeftFeet = Allocate<AnimationFootFeatureSample>(nodeCount, true);
                page.AccumulatorRightFeet = Allocate<AnimationFootFeatureSample>(nodeCount, true);
                page.AccumulatorHasFeet = Allocate<byte>(nodeCount, true);
                int residualBoneCount = checked(nodeCount * m_BoneCount);
                page.PositionResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.RotationResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.ScaleResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.LinearVelocityResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.AngularVelocityResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.ScaleVelocityResiduals = Allocate<Vector3>(residualBoneCount, true);
                page.ParameterResiduals = Allocate<float>(checked(nodeCount * m_ParameterCount), true);
                return page;
            }
            catch
            {
                DisposePage(page);
                throw;
            }
        }

        void BindPage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            m_States = page.States;
            m_HistoryPoses = page.HistoryPoses;
            m_HistoryVelocities = page.HistoryVelocities;
            m_HistoryParameters = page.HistoryParameters;
            m_HistoryParameterAvailability = page.HistoryParameterAvailability;
            m_HistoryLeftFeet = page.HistoryLeftFeet;
            m_HistoryRightFeet = page.HistoryRightFeet;
            m_HistoryHasFeet = page.HistoryHasFeet;
            m_AccumulatorLeftFeet = page.AccumulatorLeftFeet;
            m_AccumulatorRightFeet = page.AccumulatorRightFeet;
            m_AccumulatorHasFeet = page.AccumulatorHasFeet;
            m_PositionResiduals = page.PositionResiduals;
            m_RotationResiduals = page.RotationResiduals;
            m_ScaleResiduals = page.ScaleResiduals;
            m_LinearVelocityResiduals = page.LinearVelocityResiduals;
            m_AngularVelocityResiduals = page.AngularVelocityResiduals;
            m_ScaleVelocityResiduals = page.ScaleVelocityResiduals;
            m_ParameterResiduals = page.ParameterResiduals;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Pose Inertialization frame is not open.");
        }

        static void ClearPage(Page page)
        {
            Clear(page.States);
            Clear(page.HistoryPoses);
            Clear(page.HistoryVelocities);
            Clear(page.HistoryParameters);
            Clear(page.HistoryParameterAvailability);
            Clear(page.HistoryLeftFeet);
            Clear(page.HistoryRightFeet);
            Clear(page.HistoryHasFeet);
            Clear(page.AccumulatorLeftFeet);
            Clear(page.AccumulatorRightFeet);
            Clear(page.AccumulatorHasFeet);
            Clear(page.PositionResiduals);
            Clear(page.RotationResiduals);
            Clear(page.ScaleResiduals);
            Clear(page.LinearVelocityResiduals);
            Clear(page.AngularVelocityResiduals);
            Clear(page.ScaleVelocityResiduals);
            Clear(page.ParameterResiduals);
        }

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            Dispose(ref page.ParameterResiduals);
            Dispose(ref page.ScaleVelocityResiduals);
            Dispose(ref page.AngularVelocityResiduals);
            Dispose(ref page.LinearVelocityResiduals);
            Dispose(ref page.ScaleResiduals);
            Dispose(ref page.RotationResiduals);
            Dispose(ref page.PositionResiduals);
            Dispose(ref page.HistoryHasFeet);
            Dispose(ref page.AccumulatorHasFeet);
            Dispose(ref page.AccumulatorRightFeet);
            Dispose(ref page.AccumulatorLeftFeet);
            Dispose(ref page.HistoryRightFeet);
            Dispose(ref page.HistoryLeftFeet);
            Dispose(ref page.HistoryParameters);
            Dispose(ref page.HistoryParameterAvailability);
            Dispose(ref page.HistoryVelocities);
            Dispose(ref page.HistoryPoses);
            Dispose(ref page.States);
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(PoseInertializationNativeProgram));
            }
        }

        static void Clear<T>(NativeArray<T> values) where T : unmanaged
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = default;
        }

        static void Dispose<T>(ref NativeArray<T> values) where T : unmanaged
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
