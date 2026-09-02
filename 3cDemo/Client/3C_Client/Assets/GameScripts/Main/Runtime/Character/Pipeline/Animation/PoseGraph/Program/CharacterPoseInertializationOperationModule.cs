using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseInertializationOperationModule
    {
        readonly CharacterPoseValueWorkspace m_Context;

        internal CharacterPoseInertializationOperationModule(
            CharacterPoseValueWorkspace context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal void EvaluateInertialization(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeInertializationOperation operation,
            float deltaSeconds)
        {
            int output = operation.OutputPoseValueIndex;
            int input = operation.InputPoseValueIndex;
            if (!m_Context.IsInputReady(input, header.Index) ||
                (uint)operation.InertializationIndex >= (uint)m_Context.m_Inertializations.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                m_Context.SetInvalid(output, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, header.Index);
                return;
            }
            int stateIndex = operation.InertializationIndex;
            PoseInertializationNativeNode node = m_Context.m_Inertializations[stateIndex];
            bool stateMachineOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.StateMachineTransition;
            bool directPlayerOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.DirectPlayerPolicy;
            if (!stateMachineOwner && !directPlayerOwner ||
                stateMachineOwner && (uint)node.ControlIndex >= (uint)m_Context.m_StateMachineControls.Length ||
                directPlayerOwner && node.ControlIndex != -1)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                m_Context.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control = stateMachineOwner
                ? m_Context.m_StateMachineControls[node.ControlIndex]
                : default;
            PoseDiscontinuityNative discontinuity = m_Context.m_ValueDiscontinuities[input];
            if (!m_Context.TryCopyValue(input, output, header.Index))
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            PoseInertializationNativeState state = CommittedInertialState(stateIndex);
            PrepareInertialNode(stateIndex, in state);
            if (m_Context.m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Reset);
                return;
            }

            if (discontinuity.IsReset)
            {
                state = default;
                state.LastEventIdentity = stateMachineOwner
                    ? control.Generation
                    : discontinuity.EventIdentity;
                state.RuntimeState = PoseInertializationRuntimeState.Reset;
                state.LastResetReason = discontinuity.ResetReason;
                state.LastResetSequence = discontinuity.ResetSequence;
                state.OutputCompletionIdentity = m_Context.m_CompletionIdentity;
            }
            else
            {
                ulong eventIdentity = stateMachineOwner
                    ? control.Generation
                    : discontinuity.EventIdentity;
                if (stateMachineOwner && eventIdentity == 0 ||
                    eventIdentity != 0 && eventIdentity < state.LastEventIdentity)
                {
                    ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                    m_Context.SetInvalid(
                        output,
                        m_Context.m_ValueContinuityIdentities[input],
                        AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                        header.Index);
                    return;
                }
                if (eventIdentity > state.LastEventIdentity)
                {
                    int sourceEndpointIndex;
                    int targetEndpointIndex;
                    bool inertialize;
                    if (stateMachineOwner)
                    {
                        sourceEndpointIndex = control.SourceStateIndex;
                        targetEndpointIndex = control.TargetStateIndex;
                        inertialize = control.BlendMode == CharacterPoseStateMachineBlendMode.Inertialization;
                    }
                    else
                    {
                        if (!discontinuity.IsPresent || discontinuity.HasPreviousEndpoint == 0 ||
                            discontinuity.HasCurrentEndpoint == 0 ||
                            discontinuity.PreviousEndpoint.PresentationPoseSourceIndex < 0 ||
                            discontinuity.CurrentEndpoint.PresentationPoseSourceIndex < 0)
                        {
                            ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                            m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                            return;
                        }
                        sourceEndpointIndex = discontinuity.PreviousEndpoint.PresentationPoseSourceIndex;
                        targetEndpointIndex = discontinuity.CurrentEndpoint.PresentationPoseSourceIndex;
                        inertialize = true;
                        state.LastReason = discontinuity.Reason;
                        state.PreviousEndpoint = discontinuity.PreviousEndpoint;
                        state.CurrentEndpoint = discontinuity.CurrentEndpoint;
                        state.PreviousContinuityIdentity = discontinuity.PreviousContinuityIdentity;
                        state.CurrentContinuityIdentity = discontinuity.CurrentContinuityIdentity;
                    }
                    bool rebase = state.Active != 0;
                    state.LastEventIdentity = eventIdentity;
                    state.Active = 0;
                    state.ElapsedSeconds = 0f;
                    if (!inertialize)
                    {
                        state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                    }
                    else
                    {
                        int ruleIndex = RequireInertialRule(
                            stateIndex,
                            sourceEndpointIndex,
                            targetEndpointIndex);
                        if (ruleIndex < 0 ||
                            m_Context.m_InertialRules[ruleIndex].Mode != PoseInertializationMode.Inertialize)
                        {
                            ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                            m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                            return;
                        }
                        if (state.HasHistory != 0)
                        {
                            CaptureInertialResidual(stateIndex, input, ruleIndex, ref state);
                            state.RuntimeState = rebase
                                ? PoseInertializationRuntimeState.Rebase
                                : PoseInertializationRuntimeState.Capture;
                        }
                        else
                        {
                            state.Active = 0;
                            state.ActiveRuleIndex = ruleIndex;
                            state.RuntimeState = PoseInertializationRuntimeState.Anchor;
                        }
                    }
                }
                else if (state.Active != 0)
                {
                    state.RuntimeState = PoseInertializationRuntimeState.Continue;
                }
            }

            if (state.Active != 0)
            {
                PoseInertializationNativeRule rule = m_Context.m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_Context.m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_Context.m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    EvaluateInertialEnvelope(rule, state.ElapsedSeconds, duration, out _, out float weight, out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(input) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(input) + bone];
                    Vector3 positionBase = m_Context.m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_Context.m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_Context.m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_Context.m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_Context.m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_Context.m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_Context.m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_Context.m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_Context.m_InertialScaleVelocityResiduals[residualIndex];
                    if (!CharacterPoseValueWorkspace.IsFinite(linear) || !CharacterPoseValueWorkspace.IsFinite(angular) || !CharacterPoseValueWorkspace.IsFinite(scaleVelocity))
                    {
                        ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                        m_Context.SetInvalid(output, m_Context.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                        return;
                    }
                    m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone] =
                        new AnimationBlendBoneVelocity(linear, angular, scaleVelocity);
                }
                ApplyInertialParameters(stateIndex, input, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                ApplyInertialFootFeatures(stateIndex, input, output, rule, state.ActiveDurationSeconds, state.ElapsedSeconds);
                state.ElapsedSeconds += deltaSeconds;
                state.LastDeltaSeconds = deltaSeconds;
                if (!anyActive)
                {
                    state.Active = 0;
                    state.RuntimeState = PoseInertializationRuntimeState.Complete;
                }
            }

            CommitInertialHistory(stateIndex, output, ref state);
            if (state.RuntimeState == 0)
                state.RuntimeState = PoseInertializationRuntimeState.Anchor;
            state.OutputCompletionIdentity = m_Context.m_CompletionIdentity;
            m_Context.m_InertialStates[stateIndex] = state;
        }

        internal void PrepareInertialNode(
            int stateIndex,
            in PoseInertializationNativeState state)
        {
            m_Context.m_InertialStates[stateIndex] = state;
            if (state.Active == 0)
                return;
            int residualOffset = stateIndex * m_Context.m_BoneCount;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                int index = residualOffset + bone;
                m_Context.m_InertialPositionResiduals[index] = m_Context.m_CommittedInertialPositionResiduals[index];
                m_Context.m_InertialRotationResiduals[index] = m_Context.m_CommittedInertialRotationResiduals[index];
                m_Context.m_InertialScaleResiduals[index] = m_Context.m_CommittedInertialScaleResiduals[index];
                m_Context.m_InertialLinearVelocityResiduals[index] = m_Context.m_CommittedInertialLinearVelocityResiduals[index];
                m_Context.m_InertialAngularVelocityResiduals[index] = m_Context.m_CommittedInertialAngularVelocityResiduals[index];
                m_Context.m_InertialScaleVelocityResiduals[index] = m_Context.m_CommittedInertialScaleVelocityResiduals[index];
            }
            int parameterOffset = stateIndex * m_Context.m_ParameterCount;
            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                int index = parameterOffset + parameter;
                m_Context.m_InertialParameterResiduals[index] = m_Context.m_CommittedInertialParameterResiduals[index];
            }
            m_Context.m_InertialAccumulatorLeftFeet[stateIndex] = m_Context.m_CommittedInertialAccumulatorLeftFeet[stateIndex];
            m_Context.m_InertialAccumulatorRightFeet[stateIndex] = m_Context.m_CommittedInertialAccumulatorRightFeet[stateIndex];
            m_Context.m_InertialAccumulatorHasFeet[stateIndex] = m_Context.m_CommittedInertialAccumulatorHasFeet[stateIndex];
        }

        internal PoseInertializationNativeState CommittedInertialState(int stateIndex) =>
            m_Context.m_InertialResetRequests[stateIndex] != 0
                ? default
                : m_Context.m_CommittedInertialStates[stateIndex];

        internal void CaptureInertialResidual(
            int stateIndex,
            int input,
            int ruleIndex,
            ref PoseInertializationNativeState state)
        {
            int historyPoseOffset = (stateIndex * 2 + state.HistoryPage) * m_Context.m_BoneCount;
            int residualOffset = stateIndex * m_Context.m_BoneCount;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                AnimationLocalBonePose previous = m_Context.m_CommittedInertialHistory[historyPoseOffset + bone];
                AnimationBlendBoneVelocity previousVelocity = m_Context.m_CommittedInertialHistoryVelocities[historyPoseOffset + bone];
                AnimationLocalBonePose target = m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(input) + bone];
                AnimationBlendBoneVelocity targetVelocity = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(input) + bone];
                m_Context.m_InertialPositionResiduals[residualOffset + bone] = previous.Position - target.Position;
                m_Context.m_InertialRotationResiduals[residualOffset + bone] =
                    AnimationPoseMath.QuaternionLog(previous.Rotation * Quaternion.Inverse(target.Rotation));
                m_Context.m_InertialScaleResiduals[residualOffset + bone] = previous.Scale - target.Scale;
                m_Context.m_InertialLinearVelocityResiduals[residualOffset + bone] = previousVelocity.Linear - targetVelocity.Linear;
                m_Context.m_InertialAngularVelocityResiduals[residualOffset + bone] = previousVelocity.Angular - targetVelocity.Angular;
                m_Context.m_InertialScaleVelocityResiduals[residualOffset + bone] = previousVelocity.Scale - targetVelocity.Scale;
            }
            PoseInertializationNativeRule rule = m_Context.m_InertialRules[ruleIndex];
            int historyParameterOffset = (stateIndex * 2 + state.HistoryPage) * m_Context.m_ParameterCount;
            int residualParameterOffset = stateIndex * m_Context.m_ParameterCount;
            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                m_Context.m_InertialParameterResiduals[residualParameterOffset + parameter] =
                    m_Context.m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_Context.m_CommittedInertialHistoryParameterAvailability[historyParameterOffset + parameter] != 0 &&
                    m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(input) + parameter] != 0
                        ? m_Context.m_CommittedInertialHistoryParameters[historyParameterOffset + parameter] -
                          m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(input) + parameter]
                        : 0f;
            }
            int historyFootIndex = stateIndex * 2 + state.HistoryPage;
            m_Context.m_InertialAccumulatorLeftFeet[stateIndex] = m_Context.m_CommittedInertialHistoryLeftFeet[historyFootIndex];
            m_Context.m_InertialAccumulatorRightFeet[stateIndex] = m_Context.m_CommittedInertialHistoryRightFeet[historyFootIndex];
            m_Context.m_InertialAccumulatorHasFeet[stateIndex] = m_Context.m_CommittedInertialHistoryHasFeet[historyFootIndex];
            state.ActiveRuleIndex = ruleIndex;
            state.ActiveDurationSeconds = rule.DurationSeconds;
            state.ElapsedSeconds = 0f;
            state.AccumulatorGeneration++;
            state.Active = 1;
        }

        internal void ApplyInertialParameters(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float durationSeconds,
            float elapsedSeconds)
        {
            EvaluateInertialEnvelope(rule, elapsedSeconds, durationSeconds, out _, out float weight, out _);
            int residualOffset = stateIndex * m_Context.m_ParameterCount;
            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                if (m_Context.m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(input) + parameter] != 0)
                {
                    m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(output) + parameter] =
                        m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(input) + parameter] +
                        weight * m_Context.m_InertialParameterResiduals[residualOffset + parameter];
                }
            }
        }

        internal void ApplyInertialFootFeatures(
            int stateIndex,
            int input,
            int output,
            PoseInertializationNativeRule rule,
            float durationSeconds,
            float elapsedSeconds)
        {
            if (m_Context.m_InertialAccumulatorHasFeet[stateIndex] == 0 || m_Context.m_ValueHasFootFeatures[input] == 0)
                return;
            float leftDuration = durationSeconds *
                                 m_Context.m_InertialDenseProfiles[rule.ProfileOffset + m_Context.m_LeftFootBoneIndex];
            float rightDuration = durationSeconds *
                                  m_Context.m_InertialDenseProfiles[rule.ProfileOffset + m_Context.m_RightFootBoneIndex];
            EvaluateInertialEnvelope(rule, elapsedSeconds, leftDuration, out float leftEnvelope, out _, out _);
            EvaluateInertialEnvelope(rule, elapsedSeconds, rightDuration, out float rightEnvelope, out _, out _);
            if (CharacterPoseValueWorkspace.TryResolveFeature(
                    true,
                    m_Context.m_InertialAccumulatorLeftFeet[stateIndex],
                    true,
                    m_Context.m_ValueLeftFootFeatures[input],
                    leftEnvelope,
                    true,
                    out AnimationFootFeatureSample left) &&
                CharacterPoseValueWorkspace.TryResolveFeature(
                    true,
                    m_Context.m_InertialAccumulatorRightFeet[stateIndex],
                    true,
                    m_Context.m_ValueRightFootFeatures[input],
                    rightEnvelope,
                    true,
                    out AnimationFootFeatureSample right))
            {
                m_Context.m_ValueLeftFootFeatures[output] = left;
                m_Context.m_ValueRightFootFeatures[output] = right;
                m_Context.m_ValueHasFootFeatures[output] = 1;
                ScaleContributionFootWeights(output, leftEnvelope, rightEnvelope);
            }
        }

        void ScaleContributionFootWeights(int value, float leftEnvelope, float rightEnvelope)
        {
            int count = m_Context.m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                int index = m_Context.ContributionOffset(value) + contribution;
                AnimationPrimitivePoseContribution source = m_Context.m_ValueContributions[index];
                m_Context.m_ValueContributions[index] = new AnimationPrimitivePoseContribution(
                    source.PhysicalPlayerIndex,
                    source.PhysicalSourceIndex,
                    source.PhysicalSourceGeneration,
                    source.Kind,
                    source.SourceOwnerIndex,
                    source.ContributionContinuityIdentity,
                    source.Weight,
                    source.LeftFootWeight * leftEnvelope,
                    source.RightFootWeight * rightEnvelope);
            }
        }

        internal void CommitInertialHistory(int stateIndex, int output, ref PoseInertializationNativeState state)
        {
            int page = state.HasHistory == 0 ? 0 : 1 - state.HistoryPage;
            int poseOffset = (stateIndex * 2 + page) * m_Context.m_BoneCount;
            for (int bone = 0; bone < m_Context.m_BoneCount; bone++)
            {
                m_Context.m_InertialHistory[poseOffset + bone] = m_Context.m_ValueDenseLocalPoses[m_Context.PoseOffset(output) + bone];
                m_Context.m_InertialHistoryVelocities[poseOffset + bone] = m_Context.m_ValueDenseVelocities[m_Context.PoseOffset(output) + bone];
            }
            int parameterOffset = (stateIndex * 2 + page) * m_Context.m_ParameterCount;
            for (int parameter = 0; parameter < m_Context.m_ParameterCount; parameter++)
            {
                m_Context.m_InertialHistoryParameters[parameterOffset + parameter] = m_Context.m_ValuePoseParameters[m_Context.ParameterOffset(output) + parameter];
                m_Context.m_InertialHistoryParameterAvailability[parameterOffset + parameter] =
                    m_Context.m_ValuePoseParameterAvailability[m_Context.ParameterOffset(output) + parameter];
            }
            int footIndex = stateIndex * 2 + page;
            m_Context.m_InertialHistoryLeftFeet[footIndex] = m_Context.m_ValueLeftFootFeatures[output];
            m_Context.m_InertialHistoryRightFeet[footIndex] = m_Context.m_ValueRightFootFeatures[output];
            m_Context.m_InertialHistoryHasFeet[footIndex] = m_Context.m_ValueHasFootFeatures[output];
            state.HistoryPage = page;
            state.HasHistory = 1;
            state.HistoryCompletionIdentity = m_Context.m_CompletionIdentity;
        }

        void ClearInertialState(int stateIndex, PoseInertializationRuntimeState runtimeState)
        {
            if ((uint)stateIndex < (uint)m_Context.m_InertialStates.Length)
            {
                m_Context.m_InertialStates[stateIndex] = new PoseInertializationNativeState
                {
                    RuntimeState = runtimeState,
                    OutputCompletionIdentity = m_Context.m_CompletionIdentity
                };
            }
        }

        internal int RequireInertialRule(int stateIndex, int sourceProducerIndex, int targetProducerIndex)
        {
            PoseInertializationNativeNode node = m_Context.m_Inertializations[stateIndex];
            int match = -1;
            for (int i = 0; i < node.RuleCount; i++)
            {
                int index = node.RuleOffset + i;
                PoseInertializationNativeRule rule = m_Context.m_InertialRules[index];
                if (rule.SourceEndpointIndex != sourceProducerIndex || rule.TargetEndpointIndex != targetProducerIndex)
                    continue;
                if (match >= 0)
                    return -1;
                match = index;
            }
            return match;
        }

        internal void EvaluateInertialEnvelope(
            PoseInertializationNativeRule rule,
            float elapsedSeconds,
            float durationSeconds,
            out float envelope,
            out float residualWeight,
            out float residualDerivativePerSecond)
        {
            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds)
            {
                envelope = 1f;
                residualWeight = 0f;
                residualDerivativePerSecond = 0f;
                return;
            }
            float normalized = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            EvaluateInertialCurve(rule, normalized, out float curve, out float derivative);
            EvaluateInertialCurve(rule, 0f, out _, out float startDerivative);
            EvaluateInertialCurve(rule, 1f, out _, out float endDerivative);
            float s2 = normalized * normalized;
            float s3 = s2 * normalized;
            float h10 = s3 - 2f * s2 + normalized;
            float h11 = s3 - s2;
            float h10Derivative = 3f * s2 - 4f * normalized + 1f;
            float h11Derivative = 3f * s2 - 2f * normalized;
            envelope = Mathf.Clamp01(curve - startDerivative * h10 - endDerivative * h11);
            float envelopeDerivative = derivative - startDerivative * h10Derivative - endDerivative * h11Derivative;
            residualWeight = 1f - envelope;
            residualDerivativePerSecond = -envelopeDerivative / durationSeconds;
        }

        void EvaluateInertialCurve(
            PoseInertializationNativeRule rule,
            float normalizedTime,
            out float value,
            out float derivative)
        {
            float time = Mathf.Clamp01(normalizedTime);
            AnimationBlendCurveSegment segment = m_Context.m_InertialCurveSegments[rule.CurveOffset + rule.CurveCount - 1];
            for (int i = 0; i < rule.CurveCount; i++)
            {
                AnimationBlendCurveSegment candidate = m_Context.m_InertialCurveSegments[rule.CurveOffset + i];
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

    }
}
