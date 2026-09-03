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
        readonly CharacterPoseManagedValuePage m_Values;
        readonly NativeArray<PoseInertializationNativeNode> m_Inertializations;
        readonly NativeArray<PoseInertializationNativeRule> m_InertialRules;
        readonly NativeArray<AnimationBlendCurveSegment> m_InertialCurveSegments;
        readonly NativeArray<float> m_InertialDenseProfiles;
        readonly NativeArray<PoseParameterInertializationMode>
            m_InertialParameterModes;
        NativeArray<PoseInertializationNativeState> m_InertialStates;
        NativeArray<AnimationLocalBonePose> m_InertialHistory;
        NativeArray<AnimationBlendBoneVelocity>
            m_InertialHistoryVelocities;
        NativeArray<float> m_InertialHistoryParameters;
        NativeArray<byte> m_InertialHistoryParameterAvailability;
        NativeArray<AnimationFootFeatureSample>
            m_InertialHistoryLeftFeet;
        NativeArray<AnimationFootFeatureSample>
            m_InertialHistoryRightFeet;
        NativeArray<byte> m_InertialHistoryHasFeet;
        NativeArray<AnimationFootFeatureSample>
            m_InertialAccumulatorLeftFeet;
        NativeArray<AnimationFootFeatureSample>
            m_InertialAccumulatorRightFeet;
        NativeArray<byte> m_InertialAccumulatorHasFeet;
        NativeArray<Vector3> m_InertialPositionResiduals;
        NativeArray<Vector3> m_InertialRotationResiduals;
        NativeArray<Vector3> m_InertialScaleResiduals;
        NativeArray<Vector3> m_InertialLinearVelocityResiduals;
        NativeArray<Vector3> m_InertialAngularVelocityResiduals;
        NativeArray<Vector3> m_InertialScaleVelocityResiduals;
        NativeArray<float> m_InertialParameterResiduals;
        NativeArray<byte> m_InertialResetRequests;
        readonly NativeArray<PoseInertializationNativeState>
            m_CommittedInertialStates;
        readonly NativeArray<AnimationLocalBonePose>
            m_CommittedInertialHistory;
        readonly NativeArray<AnimationBlendBoneVelocity>
            m_CommittedInertialHistoryVelocities;
        readonly NativeArray<float> m_CommittedInertialHistoryParameters;
        readonly NativeArray<byte>
            m_CommittedInertialHistoryParameterAvailability;
        readonly NativeArray<AnimationFootFeatureSample>
            m_CommittedInertialHistoryLeftFeet;
        readonly NativeArray<AnimationFootFeatureSample>
            m_CommittedInertialHistoryRightFeet;
        readonly NativeArray<byte> m_CommittedInertialHistoryHasFeet;
        readonly NativeArray<AnimationFootFeatureSample>
            m_CommittedInertialAccumulatorLeftFeet;
        readonly NativeArray<AnimationFootFeatureSample>
            m_CommittedInertialAccumulatorRightFeet;
        readonly NativeArray<byte> m_CommittedInertialAccumulatorHasFeet;
        readonly NativeArray<Vector3> m_CommittedInertialPositionResiduals;
        readonly NativeArray<Vector3> m_CommittedInertialRotationResiduals;
        readonly NativeArray<Vector3> m_CommittedInertialScaleResiduals;
        readonly NativeArray<Vector3>
            m_CommittedInertialLinearVelocityResiduals;
        readonly NativeArray<Vector3>
            m_CommittedInertialAngularVelocityResiduals;
        readonly NativeArray<Vector3>
            m_CommittedInertialScaleVelocityResiduals;
        readonly NativeArray<float> m_CommittedInertialParameterResiduals;
        readonly int m_AnimationSlotNodeOffset;
        NativeArray<CharacterPoseStateMachineNativeControl>
            m_StateMachineControls;

        internal CharacterPoseInertializationOperationModule(
            CharacterPoseManagedValuePage values,
            PoseInertializationNativeProgram program)
        {
            m_Values = values ??
                throw new ArgumentNullException(nameof(values));
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            m_Inertializations = program.Nodes;
            m_InertialRules = program.Rules;
            m_InertialCurveSegments = program.CurveSegments;
            m_InertialDenseProfiles = program.DenseProfiles;
            m_InertialParameterModes = program.ParameterModes;
            m_InertialStates = program.States;
            m_InertialHistory = program.HistoryPoses;
            m_InertialHistoryVelocities = program.HistoryVelocities;
            m_InertialHistoryParameters = program.HistoryParameters;
            m_InertialHistoryParameterAvailability =
                program.HistoryParameterAvailability;
            m_InertialHistoryLeftFeet = program.HistoryLeftFeet;
            m_InertialHistoryRightFeet = program.HistoryRightFeet;
            m_InertialHistoryHasFeet = program.HistoryHasFeet;
            m_InertialAccumulatorLeftFeet = program.AccumulatorLeftFeet;
            m_InertialAccumulatorRightFeet = program.AccumulatorRightFeet;
            m_InertialAccumulatorHasFeet = program.AccumulatorHasFeet;
            m_InertialPositionResiduals = program.PositionResiduals;
            m_InertialRotationResiduals = program.RotationResiduals;
            m_InertialScaleResiduals = program.ScaleResiduals;
            m_InertialLinearVelocityResiduals =
                program.LinearVelocityResiduals;
            m_InertialAngularVelocityResiduals =
                program.AngularVelocityResiduals;
            m_InertialScaleVelocityResiduals =
                program.ScaleVelocityResiduals;
            m_InertialParameterResiduals = program.ParameterResiduals;
            m_InertialResetRequests = program.ResetRequests;
            m_CommittedInertialStates = program.CommittedStates;
            m_CommittedInertialHistory = program.CommittedHistoryPoses;
            m_CommittedInertialHistoryVelocities =
                program.CommittedHistoryVelocities;
            m_CommittedInertialHistoryParameters =
                program.CommittedHistoryParameters;
            m_CommittedInertialHistoryParameterAvailability =
                program.CommittedHistoryParameterAvailability;
            m_CommittedInertialHistoryLeftFeet =
                program.CommittedHistoryLeftFeet;
            m_CommittedInertialHistoryRightFeet =
                program.CommittedHistoryRightFeet;
            m_CommittedInertialHistoryHasFeet =
                program.CommittedHistoryHasFeet;
            m_CommittedInertialAccumulatorLeftFeet =
                program.CommittedAccumulatorLeftFeet;
            m_CommittedInertialAccumulatorRightFeet =
                program.CommittedAccumulatorRightFeet;
            m_CommittedInertialAccumulatorHasFeet =
                program.CommittedAccumulatorHasFeet;
            m_CommittedInertialPositionResiduals =
                program.CommittedPositionResiduals;
            m_CommittedInertialRotationResiduals =
                program.CommittedRotationResiduals;
            m_CommittedInertialScaleResiduals =
                program.CommittedScaleResiduals;
            m_CommittedInertialLinearVelocityResiduals =
                program.CommittedLinearVelocityResiduals;
            m_CommittedInertialAngularVelocityResiduals =
                program.CommittedAngularVelocityResiduals;
            m_CommittedInertialScaleVelocityResiduals =
                program.CommittedScaleVelocityResiduals;
            m_CommittedInertialParameterResiduals =
                program.CommittedParameterResiduals;
            m_AnimationSlotNodeOffset = program.SlotNodeOffset;
        }

        internal void BindFrame(
            NativeArray<CharacterPoseStateMachineNativeControl> controls) =>
            m_StateMachineControls = controls;

        internal int AnimationSlotNodeOffset => m_AnimationSlotNodeOffset;
        internal int StateCount => m_InertialStates.Length;
        internal PoseInertializationNativeRule Rule(int index) =>
            m_InertialRules[index];
        internal float DenseProfileWeight(
            in PoseInertializationNativeRule rule,
            int bone) =>
            m_InertialDenseProfiles[rule.ProfileOffset + bone];
        internal Vector3 PositionResidual(int index) =>
            m_InertialPositionResiduals[index];
        internal Vector3 RotationResidual(int index) =>
            m_InertialRotationResiduals[index];
        internal Vector3 ScaleResidual(int index) =>
            m_InertialScaleResiduals[index];
        internal Vector3 LinearVelocityResidual(int index) =>
            m_InertialLinearVelocityResiduals[index];
        internal Vector3 AngularVelocityResidual(int index) =>
            m_InertialAngularVelocityResiduals[index];
        internal Vector3 ScaleVelocityResidual(int index) =>
            m_InertialScaleVelocityResiduals[index];
        internal void SetState(
            int index,
            in PoseInertializationNativeState state) =>
            m_InertialStates[index] = state;

        internal void EvaluateInertialization(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeInertializationOperation operation,
            float deltaSeconds)
        {
            int output = operation.OutputPoseValueIndex;
            int input = operation.InputPoseValueIndex;
            if (!m_Values.IsInputReady(input, header.Index) ||
                (uint)operation.InertializationIndex >= (uint)m_Inertializations.Length ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                m_Values.SetInvalid(output, (ulong)header.Index + 1UL, AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete, header.Index);
                return;
            }
            int stateIndex = operation.InertializationIndex;
            PoseInertializationNativeNode node = m_Inertializations[stateIndex];
            bool stateMachineOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.StateMachineTransition;
            bool directPlayerOwner = node.TemporalOwnerKind ==
                                     PoseInertializationTemporalOwnerKind.DirectPlayerPolicy;
            if (!stateMachineOwner && !directPlayerOwner ||
                stateMachineOwner && (uint)node.ControlIndex >= (uint)m_StateMachineControls.Length ||
                directPlayerOwner && node.ControlIndex != -1)
            {
                ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                m_Values.SetInvalid(
                    output,
                    (ulong)header.Index + 1UL,
                    AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid,
                    header.Index);
                return;
            }
            CharacterPoseStateMachineNativeControl control = stateMachineOwner
                ? m_StateMachineControls[node.ControlIndex]
                : default;
            PoseDiscontinuityNative discontinuity = m_Values.m_ValueDiscontinuities[input];
            if (!m_Values.TryCopyValue(input, output, header.Index))
            {
                ClearInertialState(operation.InertializationIndex, PoseInertializationRuntimeState.Invalid);
                m_Values.SetInvalid(output, m_Values.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                return;
            }
            PoseInertializationNativeState state = CommittedInertialState(stateIndex);
            PrepareInertialNode(stateIndex, in state);
            if (m_Values.m_ValueAvailability[input] != AnimationPoseAvailability.Pose)
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
                state.OutputCompletionIdentity = m_Values.m_CompletionIdentity;
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
                    m_Values.SetInvalid(
                        output,
                        m_Values.m_ValueContinuityIdentities[input],
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
                            m_Values.SetInvalid(output, m_Values.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
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
                            m_InertialRules[ruleIndex].Mode != PoseInertializationMode.Inertialize)
                        {
                            ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                            m_Values.SetInvalid(output, m_Values.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
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
                PoseInertializationNativeRule rule = m_InertialRules[state.ActiveRuleIndex];
                bool anyActive = false;
                for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
                {
                    int residualIndex = stateIndex * m_Values.m_BoneCount + bone;
                    float duration = state.ActiveDurationSeconds *
                                     m_InertialDenseProfiles[rule.ProfileOffset + bone];
                    EvaluateInertialEnvelope(rule, state.ElapsedSeconds, duration, out _, out float weight, out float derivative);
                    anyActive |= state.ElapsedSeconds < duration;
                    AnimationLocalBonePose target = m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(input) + bone];
                    AnimationBlendBoneVelocity targetVelocity = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(input) + bone];
                    Vector3 positionBase = m_InertialPositionResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 rotationBase = m_InertialRotationResiduals[residualIndex] +
                                           state.ElapsedSeconds * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleBase = m_InertialScaleResiduals[residualIndex] +
                                        state.ElapsedSeconds * m_InertialScaleVelocityResiduals[residualIndex];
                    Vector3 linear = targetVelocity.Linear + derivative * positionBase +
                                     weight * m_InertialLinearVelocityResiduals[residualIndex];
                    Vector3 angular = targetVelocity.Angular + derivative * rotationBase +
                                      weight * m_InertialAngularVelocityResiduals[residualIndex];
                    Vector3 scaleVelocity = targetVelocity.Scale + derivative * scaleBase +
                                            weight * m_InertialScaleVelocityResiduals[residualIndex];
                    if (!CharacterPosePureMath.IsFinite(linear) || !CharacterPosePureMath.IsFinite(angular) || !CharacterPosePureMath.IsFinite(scaleVelocity))
                    {
                        ClearInertialState(stateIndex, PoseInertializationRuntimeState.Invalid);
                        m_Values.SetInvalid(output, m_Values.m_ValueContinuityIdentities[input], AnimationPoseNativeInvalidReason.PoseGraphOperationInvalid, header.Index);
                        return;
                    }
                    m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone] = new AnimationLocalBonePose(
                        target.Position + weight * positionBase,
                        AnimationPoseMath.QuaternionExp(weight * rotationBase) * target.Rotation,
                        target.Scale + weight * scaleBase);
                    m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone] =
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
            state.OutputCompletionIdentity = m_Values.m_CompletionIdentity;
            m_InertialStates[stateIndex] = state;
        }

        internal void PrepareInertialNode(
            int stateIndex,
            in PoseInertializationNativeState state)
        {
            m_InertialStates[stateIndex] = state;
            if (state.Active == 0)
                return;
            int residualOffset = stateIndex * m_Values.m_BoneCount;
            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                int index = residualOffset + bone;
                m_InertialPositionResiduals[index] = m_CommittedInertialPositionResiduals[index];
                m_InertialRotationResiduals[index] = m_CommittedInertialRotationResiduals[index];
                m_InertialScaleResiduals[index] = m_CommittedInertialScaleResiduals[index];
                m_InertialLinearVelocityResiduals[index] = m_CommittedInertialLinearVelocityResiduals[index];
                m_InertialAngularVelocityResiduals[index] = m_CommittedInertialAngularVelocityResiduals[index];
                m_InertialScaleVelocityResiduals[index] = m_CommittedInertialScaleVelocityResiduals[index];
            }
            int parameterOffset = stateIndex * m_Values.m_ParameterCount;
            for (int parameter = 0; parameter < m_Values.m_ParameterCount; parameter++)
            {
                int index = parameterOffset + parameter;
                m_InertialParameterResiduals[index] = m_CommittedInertialParameterResiduals[index];
            }
            m_InertialAccumulatorLeftFeet[stateIndex] = m_CommittedInertialAccumulatorLeftFeet[stateIndex];
            m_InertialAccumulatorRightFeet[stateIndex] = m_CommittedInertialAccumulatorRightFeet[stateIndex];
            m_InertialAccumulatorHasFeet[stateIndex] = m_CommittedInertialAccumulatorHasFeet[stateIndex];
        }

        internal PoseInertializationNativeState CommittedInertialState(int stateIndex) =>
            m_InertialResetRequests[stateIndex] != 0
                ? default
                : m_CommittedInertialStates[stateIndex];

        internal void CaptureInertialResidual(
            int stateIndex,
            int input,
            int ruleIndex,
            ref PoseInertializationNativeState state)
        {
            int historyPoseOffset = (stateIndex * 2 + state.HistoryPage) * m_Values.m_BoneCount;
            int residualOffset = stateIndex * m_Values.m_BoneCount;
            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                AnimationLocalBonePose previous = m_CommittedInertialHistory[historyPoseOffset + bone];
                AnimationBlendBoneVelocity previousVelocity = m_CommittedInertialHistoryVelocities[historyPoseOffset + bone];
                AnimationLocalBonePose target = m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(input) + bone];
                AnimationBlendBoneVelocity targetVelocity = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(input) + bone];
                m_InertialPositionResiduals[residualOffset + bone] = previous.Position - target.Position;
                m_InertialRotationResiduals[residualOffset + bone] =
                    AnimationPoseMath.QuaternionLog(previous.Rotation * Quaternion.Inverse(target.Rotation));
                m_InertialScaleResiduals[residualOffset + bone] = previous.Scale - target.Scale;
                m_InertialLinearVelocityResiduals[residualOffset + bone] = previousVelocity.Linear - targetVelocity.Linear;
                m_InertialAngularVelocityResiduals[residualOffset + bone] = previousVelocity.Angular - targetVelocity.Angular;
                m_InertialScaleVelocityResiduals[residualOffset + bone] = previousVelocity.Scale - targetVelocity.Scale;
            }
            PoseInertializationNativeRule rule = m_InertialRules[ruleIndex];
            int historyParameterOffset = (stateIndex * 2 + state.HistoryPage) * m_Values.m_ParameterCount;
            int residualParameterOffset = stateIndex * m_Values.m_ParameterCount;
            for (int parameter = 0; parameter < m_Values.m_ParameterCount; parameter++)
            {
                m_InertialParameterResiduals[residualParameterOffset + parameter] =
                    m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_CommittedInertialHistoryParameterAvailability[historyParameterOffset + parameter] != 0 &&
                    m_Values.m_ValuePoseParameterAvailability[m_Values.ParameterOffset(input) + parameter] != 0
                        ? m_CommittedInertialHistoryParameters[historyParameterOffset + parameter] -
                          m_Values.m_ValuePoseParameters[m_Values.ParameterOffset(input) + parameter]
                        : 0f;
            }
            int historyFootIndex = stateIndex * 2 + state.HistoryPage;
            m_InertialAccumulatorLeftFeet[stateIndex] = m_CommittedInertialHistoryLeftFeet[historyFootIndex];
            m_InertialAccumulatorRightFeet[stateIndex] = m_CommittedInertialHistoryRightFeet[historyFootIndex];
            m_InertialAccumulatorHasFeet[stateIndex] = m_CommittedInertialHistoryHasFeet[historyFootIndex];
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
            int residualOffset = stateIndex * m_Values.m_ParameterCount;
            for (int parameter = 0; parameter < m_Values.m_ParameterCount; parameter++)
            {
                if (m_InertialParameterModes[rule.ParameterModeOffset + parameter] == PoseParameterInertializationMode.Inertialize &&
                    m_Values.m_ValuePoseParameterAvailability[m_Values.ParameterOffset(input) + parameter] != 0)
                {
                    m_Values.m_ValuePoseParameters[m_Values.ParameterOffset(output) + parameter] =
                        m_Values.m_ValuePoseParameters[m_Values.ParameterOffset(input) + parameter] +
                        weight * m_InertialParameterResiduals[residualOffset + parameter];
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
            if (m_InertialAccumulatorHasFeet[stateIndex] == 0 || m_Values.m_ValueHasFootFeatures[input] == 0)
                return;
            float leftDuration = durationSeconds *
                                 m_InertialDenseProfiles[rule.ProfileOffset + m_Values.m_LeftFootBoneIndex];
            float rightDuration = durationSeconds *
                                  m_InertialDenseProfiles[rule.ProfileOffset + m_Values.m_RightFootBoneIndex];
            EvaluateInertialEnvelope(rule, elapsedSeconds, leftDuration, out float leftEnvelope, out _, out _);
            EvaluateInertialEnvelope(rule, elapsedSeconds, rightDuration, out float rightEnvelope, out _, out _);
            if (CharacterPosePureMath.TryResolveFeature(
                    true,
                    m_InertialAccumulatorLeftFeet[stateIndex],
                    true,
                    m_Values.m_ValueLeftFootFeatures[input],
                    leftEnvelope,
                    true,
                    out AnimationFootFeatureSample left) &&
                CharacterPosePureMath.TryResolveFeature(
                    true,
                    m_InertialAccumulatorRightFeet[stateIndex],
                    true,
                    m_Values.m_ValueRightFootFeatures[input],
                    rightEnvelope,
                    true,
                    out AnimationFootFeatureSample right))
            {
                m_Values.m_ValueLeftFootFeatures[output] = left;
                m_Values.m_ValueRightFootFeatures[output] = right;
                m_Values.m_ValueHasFootFeatures[output] = 1;
                ScaleContributionFootWeights(output, leftEnvelope, rightEnvelope);
            }
        }

        void ScaleContributionFootWeights(int value, float leftEnvelope, float rightEnvelope)
        {
            int count = m_Values.m_ValueContributionCounts[value];
            for (int contribution = 0; contribution < count; contribution++)
            {
                int index = m_Values.ContributionOffset(value) + contribution;
                AnimationPrimitivePoseContribution source = m_Values.m_ValueContributions[index];
                m_Values.m_ValueContributions[index] = new AnimationPrimitivePoseContribution(
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
            int poseOffset = (stateIndex * 2 + page) * m_Values.m_BoneCount;
            for (int bone = 0; bone < m_Values.m_BoneCount; bone++)
            {
                m_InertialHistory[poseOffset + bone] = m_Values.m_ValueDenseLocalPoses[m_Values.PoseOffset(output) + bone];
                m_InertialHistoryVelocities[poseOffset + bone] = m_Values.m_ValueDenseVelocities[m_Values.PoseOffset(output) + bone];
            }
            int parameterOffset = (stateIndex * 2 + page) * m_Values.m_ParameterCount;
            for (int parameter = 0; parameter < m_Values.m_ParameterCount; parameter++)
            {
                m_InertialHistoryParameters[parameterOffset + parameter] = m_Values.m_ValuePoseParameters[m_Values.ParameterOffset(output) + parameter];
                m_InertialHistoryParameterAvailability[parameterOffset + parameter] =
                    m_Values.m_ValuePoseParameterAvailability[m_Values.ParameterOffset(output) + parameter];
            }
            int footIndex = stateIndex * 2 + page;
            m_InertialHistoryLeftFeet[footIndex] = m_Values.m_ValueLeftFootFeatures[output];
            m_InertialHistoryRightFeet[footIndex] = m_Values.m_ValueRightFootFeatures[output];
            m_InertialHistoryHasFeet[footIndex] = m_Values.m_ValueHasFootFeatures[output];
            state.HistoryPage = page;
            state.HasHistory = 1;
            state.HistoryCompletionIdentity = m_Values.m_CompletionIdentity;
        }

        void ClearInertialState(int stateIndex, PoseInertializationRuntimeState runtimeState)
        {
            if ((uint)stateIndex < (uint)m_InertialStates.Length)
            {
                m_InertialStates[stateIndex] = new PoseInertializationNativeState
                {
                    RuntimeState = runtimeState,
                    OutputCompletionIdentity = m_Values.m_CompletionIdentity
                };
            }
        }

        internal int RequireInertialRule(int stateIndex, int sourceProducerIndex, int targetProducerIndex)
        {
            PoseInertializationNativeNode node = m_Inertializations[stateIndex];
            int match = -1;
            for (int i = 0; i < node.RuleCount; i++)
            {
                int index = node.RuleOffset + i;
                PoseInertializationNativeRule rule = m_InertialRules[index];
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
            AnimationBlendCurveSegment segment = m_InertialCurveSegments[rule.CurveOffset + rule.CurveCount - 1];
            for (int i = 0; i < rule.CurveCount; i++)
            {
                AnimationBlendCurveSegment candidate = m_InertialCurveSegments[rule.CurveOffset + i];
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
