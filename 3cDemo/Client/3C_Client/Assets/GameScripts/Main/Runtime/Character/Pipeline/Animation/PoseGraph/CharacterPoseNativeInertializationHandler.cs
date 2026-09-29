using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using FlowCanvas;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeInertializationHandler :
        ICharacterPoseNativeNodeHandler
    {
        struct State
        {
            internal ulong LastEventIdentity;
            internal float ElapsedSeconds;
            internal float DurationSeconds;
            internal bool HasHistory;
            internal bool Active;
        }

        struct EnvelopeSample
        {
            internal float Duration;
            internal float Envelope;
            internal float ResidualWeight;
            internal float ResidualDerivative;
        }

        sealed class ResidualPage
        {
            internal readonly Vector3[] Position;
            internal readonly Vector3[] Rotation;
            internal readonly Vector3[] Scale;
            internal readonly Vector3[] LinearVelocity;
            internal readonly Vector3[] AngularVelocity;
            internal readonly Vector3[] ScaleVelocity;
            internal readonly float[] Parameters;

            internal ResidualPage(int boneCount, int parameterCount)
            {
                Position = new Vector3[boneCount];
                Rotation = new Vector3[boneCount];
                Scale = new Vector3[boneCount];
                LinearVelocity = new Vector3[boneCount];
                AngularVelocity = new Vector3[boneCount];
                ScaleVelocity = new Vector3[boneCount];
                Parameters = new float[parameterCount];
            }
        }

        readonly PoseNodeId m_NodeId;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterPoseInertializationPolicy m_Policy;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly AnimationBlendCurvePayload m_Curve;
        readonly float m_CurveStartDerivative;
        readonly float m_CurveEndDerivative;
        readonly int[] m_BoneEnvelopeIndices;
        readonly float[] m_EnvelopeProfiles;
        readonly EnvelopeSample[] m_EnvelopeSamples;
        readonly PoseParameterInertializationMode[] m_ParameterModes;
        readonly int m_LeftFootBoneIndex;
        readonly int m_RightFootBoneIndex;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_PoseInput;
        AnimationLocalBonePose[] m_CommittedHistory;
        AnimationLocalBonePose[] m_PendingHistory;
        AnimationBlendBoneVelocity[] m_CommittedHistoryVelocities;
        AnimationBlendBoneVelocity[] m_PendingHistoryVelocities;
        float[] m_CommittedHistoryParameters;
        float[] m_PendingHistoryParameters;
        byte[] m_CommittedHistoryParameterAvailability;
        byte[] m_PendingHistoryParameterAvailability;
        AnimationFootFeatureSample m_CommittedLeftFoot;
        AnimationFootFeatureSample m_PendingLeftFoot;
        AnimationFootFeatureSample m_CommittedRightFoot;
        AnimationFootFeatureSample m_PendingRightFoot;
        bool m_CommittedHasFootFeatures;
        bool m_PendingHasFootFeatures;
        ResidualPage m_PendingResiduals;
        ResidualPage m_CommittedResiduals;
        bool m_HasPendingResiduals;
        State m_CommittedState;
        State m_PendingState;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeInertializationHandler(
            PoseNodeId nodeId,
            in CharacterPoseNativePreparedBinding preparedBinding,
            CharacterPoseInertializationPolicy policy,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Inertialization handler identity is invalid.",
                    nameof(nodeId));
            if (!preparedBinding.IsValid)
                throw new ArgumentException(
                    "Pose native Inertialization prepared binding is invalid.",
                    nameof(preparedBinding));
            m_Rig = preparedBinding.Rig;
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (preparedBinding.Profile.RigDefinition == null)
                throw new ArgumentException(
                    "Pose native Inertialization Rig definition is missing.",
                    nameof(preparedBinding));
            policy.RequireValid(preparedBinding.Profile.RigDefinition);
            CharacterPoseDirectInertializationRule directRule =
                policy.DirectPlayerRule ??
                throw new ArgumentException(
                    "Pose native Inertialization policy has no direct rule.",
                    nameof(policy));
            m_Curve = directRule.Mode == PoseInertializationMode.Inertialize
                ? directRule.CompileCurve()
                : null;
            m_CurveStartDerivative = m_Curve == null ? 0f : AnimationBlendCurveEvaluator.EvaluateDerivative(m_Curve, 0f);
            m_CurveEndDerivative = m_Curve == null ? 0f : AnimationBlendCurveEvaluator.EvaluateDerivative(m_Curve, 1f);
            float[] denseProfiles = directRule.Mode == PoseInertializationMode.Inertialize
                ? directRule.BlendProfile.BuildDense(
                    preparedBinding.Profile.RigDefinition)
                : CreateUnitProfiles(m_Rig.PoseBoneCount);
            BuildEnvelopeLayout(denseProfiles, out m_BoneEnvelopeIndices, out m_EnvelopeProfiles);
            m_EnvelopeSamples = new EnvelopeSample[m_EnvelopeProfiles.Length];
            m_ParameterModes = BuildParameterModes(
                policy.Response,
                preparedBinding.InputContract);
            m_NodeId = nodeId;
            m_Policy = policy;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            if (m_OutputBuffer.BoneCount != m_Rig.PoseBoneCount ||
                m_OutputBuffer.ParameterCount != m_ParameterModes.Length)
            {
                throw new ArgumentException(
                    "Pose native Inertialization buffer layout does not match the binding.",
                    nameof(outputBuffer));
            }
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
            m_LeftFootBoneIndex = m_Rig.LeftLeg.AnklePhysicalBoneIndex;
            m_RightFootBoneIndex = m_Rig.RightLeg.AnklePhysicalBoneIndex;
            m_CommittedHistory = new AnimationLocalBonePose[m_Rig.PoseBoneCount];
            m_PendingHistory = new AnimationLocalBonePose[m_Rig.PoseBoneCount];
            m_CommittedHistoryVelocities =
                new AnimationBlendBoneVelocity[m_Rig.PoseBoneCount];
            m_PendingHistoryVelocities =
                new AnimationBlendBoneVelocity[m_Rig.PoseBoneCount];
            m_CommittedHistoryParameters = new float[m_ParameterModes.Length];
            m_PendingHistoryParameters = new float[m_ParameterModes.Length];
            m_CommittedHistoryParameterAvailability =
                new byte[m_ParameterModes.Length];
            m_PendingHistoryParameterAvailability =
                new byte[m_ParameterModes.Length];
            m_PendingResiduals = new ResidualPage(m_Rig.PoseBoneCount, m_ParameterModes.Length);
            m_CommittedResiduals = new ResidualPage(m_Rig.PoseBoneCount, m_ParameterModes.Length);
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.Inertialization;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || node.InertializationPolicySlot == null ||
                m_Policy == null)
            {
                throw new InvalidOperationException(
                    $"Inertialization handler '{NodeId}' does not match its graph node.");
            }
            m_PoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "pose");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            m_CommittedState = default;
            m_PendingState = default;
            m_CommittedHasFootFeatures = false;
            m_PendingHasFootFeatures = false;
            m_CommittedPageIndex = -1;
            ClearFrame();
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_PendingState = m_CommittedState;
            m_HasPendingResiduals = false;
            m_PendingLeftFoot = m_CommittedLeftFoot;
            m_PendingRightFoot = m_CommittedRightFoot;
            m_PendingHasFootFeatures = m_CommittedHasFootFeatures;
            m_WriteBinding = default;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage) => null;

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "result")
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeLocalPoseValue inputValue =
                runtime.ReadInput(m_PoseInput, m_NodeId, "pose");
            CharacterPoseNativePoseReadBinding input = inputValue.Native;
            if (!input.IsValid || input.Space != CharacterPoseSpace.Local)
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' input Pose is not a native Local Pose.");
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyAttributes(
                in input,
                in m_WriteBinding);
            if (input.Availability[0] == AnimationPoseAvailability.Pose)
                EvaluatePose(in input, in m_WriteBinding, runtime.CurrentInput.DeltaSeconds);
            else
            {
                CopyBones(in input, in m_WriteBinding);
                m_PendingState = default;
                m_PendingHasFootFeatures = false;
            }
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    in m_WriteBinding,
                    CharacterPoseSpace.Local);
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
            return m_Output;
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
        }

        public void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null || !m_Output.Native.IsValid ||
                m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Availability[0] == AnimationPoseAvailability.Invalid ||
                m_Output.Native.InvalidReason[0] != AnimationPoseNativeInvalidReason.None)
            {
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' pending output is invalid.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            State state = m_CommittedState;
            m_CommittedState = m_PendingState;
            m_PendingState = state;
            Swap(ref m_CommittedHistory, ref m_PendingHistory);
            Swap(ref m_CommittedHistoryVelocities, ref m_PendingHistoryVelocities);
            Swap(ref m_CommittedHistoryParameters, ref m_PendingHistoryParameters);
            Swap(
                ref m_CommittedHistoryParameterAvailability,
                ref m_PendingHistoryParameterAvailability);
            if (m_HasPendingResiduals)
                (m_CommittedResiduals, m_PendingResiduals) = (m_PendingResiduals, m_CommittedResiduals);
            AnimationFootFeatureSample left = m_CommittedLeftFoot;
            m_CommittedLeftFoot = m_PendingLeftFoot;
            m_PendingLeftFoot = left;
            AnimationFootFeatureSample right = m_CommittedRightFoot;
            m_CommittedRightFoot = m_PendingRightFoot;
            m_PendingRightFoot = right;
            bool hasFeet = m_CommittedHasFootFeatures;
            m_CommittedHasFootFeatures = m_PendingHasFootFeatures;
            m_PendingHasFootFeatures = hasFeet;
            m_CommittedPageIndex = m_PageIndex;
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            m_PendingState = m_CommittedState;
            m_PendingHasFootFeatures = m_CommittedHasFootFeatures;
            ClearFrame();
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
            {
                CharacterPoseNativeFrameLineage lineage = runtime.CurrentLineage;
                DiscardFrame(
                    runtime,
                    in lineage,
                    CharacterPoseNativeFailureCode.Disposed);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            m_FrameOpen = false;
        }

        void EvaluatePose(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output,
            float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            PoseDiscontinuityNative discontinuity = input.Discontinuity[0];
            if (!discontinuity.IsValid)
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' input Discontinuity is invalid.");
            if (discontinuity.IsReset)
            {
                m_PendingState = new State
                {
                    LastEventIdentity = discontinuity.EventIdentity
                };
            }
            else if (discontinuity.IsPresent &&
                     discontinuity.EventIdentity < m_PendingState.LastEventIdentity)
            {
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' received an older Discontinuity event.");
            }
            else if (discontinuity.EventIdentity > m_PendingState.LastEventIdentity)
            {
                BeginTransition(
                    input,
                    discontinuity.EventIdentity);
            }
            if (m_PendingState.Active)
                ApplyResiduals(in input, in output, deltaSeconds);
            else
                CopyBones(in input, in output);
            CommitHistory(in output);
        }

        static void CopyBones(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output)
        {
            NativeSlice<AnimationLocalBonePose> poses = output.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> velocities = output.DenseVelocities;
            poses.CopyFrom(input.DenseLocalPoses);
            velocities.CopyFrom(input.DenseVelocities);
        }

        void BeginTransition(
            in CharacterPoseNativePoseReadBinding input,
            ulong eventIdentity)
        {
            CharacterPoseDirectInertializationRule rule =
                m_Policy.DirectPlayerRule;
            m_PendingState.LastEventIdentity = eventIdentity;
            m_PendingState.ElapsedSeconds = 0f;
            m_PendingState.DurationSeconds = rule.DurationSeconds;
            m_PendingState.Active = false;
            if (rule.Mode != PoseInertializationMode.Inertialize ||
                !m_CommittedState.HasHistory)
            {
                return;
            }
            NativeSlice<AnimationLocalBonePose> inputPoses =
                input.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> inputVelocities =
                input.DenseVelocities;
            for (int bone = 0; bone < m_Rig.PoseBoneCount; bone++)
            {
                ref readonly AnimationLocalBonePose previous =
                    ref m_CommittedHistory[bone];
                ref readonly AnimationLocalBonePose target = ref inputPoses[bone];
                if (!previous.IsValid || !target.IsValid)
                    throw new InvalidOperationException(
                        $"Inertialization '{NodeId}' history Bone #{bone} is invalid.");
                m_PendingResiduals.Position[bone] = previous.Position - target.Position;
                m_PendingResiduals.Rotation[bone] =
                    AnimationPoseMath.QuaternionLog(
                        previous.Rotation * Quaternion.Inverse(target.Rotation));
                m_PendingResiduals.Scale[bone] = previous.Scale - target.Scale;
                ref readonly AnimationBlendBoneVelocity previousVelocity =
                    ref m_CommittedHistoryVelocities[bone];
                ref readonly AnimationBlendBoneVelocity targetVelocity =
                    ref inputVelocities[bone];
                if (!previousVelocity.IsValid || !targetVelocity.IsValid)
                    throw new InvalidOperationException(
                        $"Inertialization '{NodeId}' history velocity Bone #{bone} is invalid.");
                m_PendingResiduals.LinearVelocity[bone] =
                    previousVelocity.Linear - targetVelocity.Linear;
                m_PendingResiduals.AngularVelocity[bone] =
                    previousVelocity.Angular - targetVelocity.Angular;
                m_PendingResiduals.ScaleVelocity[bone] =
                    previousVelocity.Scale - targetVelocity.Scale;
            }
            for (int parameter = 0; parameter < m_ParameterModes.Length; parameter++)
            {
                m_PendingResiduals.Parameters[parameter] =
                    m_ParameterModes[parameter] ==
                    PoseParameterInertializationMode.Inertialize &&
                    m_CommittedHistoryParameterAvailability[parameter] != 0 &&
                    input.PoseParameterAvailability[parameter] != 0
                        ? m_CommittedHistoryParameters[parameter] -
                          input.PoseParameters[parameter]
                        : 0f;
            }
            m_PendingLeftFoot = m_CommittedLeftFoot;
            m_PendingRightFoot = m_CommittedRightFoot;
            m_PendingHasFootFeatures = m_CommittedHasFootFeatures;
            m_PendingState.Active = true;
            m_HasPendingResiduals = true;
        }

        void ApplyResiduals(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output,
            float deltaSeconds)
        {
            ResidualPage residuals = m_HasPendingResiduals ? m_PendingResiduals : m_CommittedResiduals;
            for (int i = 0; i < m_EnvelopeSamples.Length; i++)
            {
                ref EnvelopeSample sample = ref m_EnvelopeSamples[i];
                sample.Duration = m_PendingState.DurationSeconds * m_EnvelopeProfiles[i];
                EvaluateEnvelope(
                    sample.Duration,
                    m_PendingState.ElapsedSeconds,
                    out sample.Envelope,
                    out sample.ResidualWeight,
                    out sample.ResidualDerivative);
            }
            bool anyActive = false;
            NativeSlice<AnimationLocalBonePose> poses = output.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> velocities = output.DenseVelocities;
            NativeSlice<AnimationLocalBonePose> inputPoses =
                input.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> inputVelocities =
                input.DenseVelocities;
            for (int bone = 0; bone < m_Rig.PoseBoneCount; bone++)
            {
                ref readonly EnvelopeSample sample =
                    ref m_EnvelopeSamples[m_BoneEnvelopeIndices[bone]];
                float residualWeight = sample.ResidualWeight;
                float residualDerivative = sample.ResidualDerivative;
                anyActive |= m_PendingState.ElapsedSeconds < sample.Duration;
                ref readonly AnimationLocalBonePose target = ref inputPoses[bone];
                ref readonly AnimationBlendBoneVelocity targetVelocity =
                    ref inputVelocities[bone];
                Vector3 positionBase = residuals.Position[bone] +
                    m_PendingState.ElapsedSeconds * residuals.LinearVelocity[bone];
                Vector3 rotationBase = residuals.Rotation[bone] +
                    m_PendingState.ElapsedSeconds * residuals.AngularVelocity[bone];
                Vector3 scaleBase = residuals.Scale[bone] +
                    m_PendingState.ElapsedSeconds * residuals.ScaleVelocity[bone];
                Vector3 linear = targetVelocity.Linear +
                    residualDerivative * positionBase +
                    residualWeight * residuals.LinearVelocity[bone];
                Vector3 angular = targetVelocity.Angular +
                    residualDerivative * rotationBase +
                    residualWeight * residuals.AngularVelocity[bone];
                Vector3 scaleVelocity = targetVelocity.Scale +
                    residualDerivative * scaleBase +
                    residualWeight * residuals.ScaleVelocity[bone];
                if (!AnimationPoseMath.IsFinite(linear) ||
                    !AnimationPoseMath.IsFinite(angular) ||
                    !AnimationPoseMath.IsFinite(scaleVelocity))
                {
                    throw new InvalidOperationException(
                        $"Inertialization '{NodeId}' Bone #{bone} velocity is invalid.");
                }
                poses[bone] = new AnimationLocalBonePose(
                    target.Position + residualWeight * positionBase,
                    AnimationPoseMath.QuaternionExp(residualWeight * rotationBase) *
                    target.Rotation,
                    target.Scale + residualWeight * scaleBase);
                velocities[bone] = new AnimationBlendBoneVelocity(
                    linear,
                    angular,
                    scaleVelocity);
            }
            ApplyParameters(in input, in output, m_PendingState.ElapsedSeconds);
            ApplyFootFeatures(in input, in output);
            m_PendingState.ElapsedSeconds += deltaSeconds;
            if (!anyActive)
                m_PendingState.Active = false;
        }

        void ApplyParameters(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output,
            float elapsedSeconds)
        {
            EvaluateEnvelope(
                m_PendingState.DurationSeconds,
                elapsedSeconds,
                out _,
                out float residualWeight,
                out _);
            NativeSlice<float> parameters = output.PoseParameters;
            float[] residuals = (m_HasPendingResiduals ? m_PendingResiduals : m_CommittedResiduals).Parameters;
            for (int parameter = 0; parameter < m_ParameterModes.Length; parameter++)
            {
                if (m_ParameterModes[parameter] ==
                        PoseParameterInertializationMode.Inertialize &&
                    input.PoseParameterAvailability[parameter] != 0)
                {
                    parameters[parameter] = input.PoseParameters[parameter] +
                        residualWeight * residuals[parameter];
                }
            }
        }

        void ApplyFootFeatures(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output)
        {
            if (!m_PendingHasFootFeatures || input.HasFootFeatures[0] == 0)
                return;
            float leftEnvelope = m_EnvelopeSamples[m_BoneEnvelopeIndices[m_LeftFootBoneIndex]].Envelope;
            float rightEnvelope = m_EnvelopeSamples[m_BoneEnvelopeIndices[m_RightFootBoneIndex]].Envelope;
            AnimationFootFeatureBlendAccumulator left = default;
            if (leftEnvelope < 1f)
                left.Add(m_PendingLeftFoot, 1f - leftEnvelope);
            if (leftEnvelope > 0f)
                left.Add(input.LeftFootFeatures[0], leftEnvelope);
            AnimationFootFeatureBlendAccumulator right = default;
            if (rightEnvelope < 1f)
                right.Add(m_PendingRightFoot, 1f - rightEnvelope);
            if (rightEnvelope > 0f)
                right.Add(input.RightFootFeatures[0], rightEnvelope);
            m_PendingLeftFoot = left.Resolve();
            m_PendingRightFoot = right.Resolve();
            m_PendingHasFootFeatures = true;
            NativeSlice<AnimationPrimitivePoseContribution> contributions =
                output.Contributions;
            int count = output.ContributionCount[0];
            for (int contribution = 0; contribution < count; contribution++)
            {
                AnimationPrimitivePoseContribution source = contributions[contribution];
                contributions[contribution] = new AnimationPrimitivePoseContribution(
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

        void CommitHistory(in AnimationPlayerPoseNativeWriteBinding output)
        {
            NativeSlice<AnimationLocalBonePose> outputPoses =
                output.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                output.DenseVelocities;
            for (int bone = 0; bone < m_Rig.PoseBoneCount; bone++)
            {
                m_PendingHistory[bone] = outputPoses[bone];
                m_PendingHistoryVelocities[bone] = outputVelocities[bone];
            }
            for (int parameter = 0; parameter < m_ParameterModes.Length; parameter++)
            {
                m_PendingHistoryParameters[parameter] = output.PoseParameters[parameter];
                m_PendingHistoryParameterAvailability[parameter] =
                    output.PoseParameterAvailability[parameter];
            }
            m_PendingLeftFoot = output.LeftFootFeatures[0];
            m_PendingRightFoot = output.RightFootFeatures[0];
            m_PendingHasFootFeatures = output.HasFootFeatures[0] != 0;
            m_PendingState.HasHistory = true;
        }

        void EvaluateEnvelope(
            float duration,
            float elapsedSeconds,
            out float envelope,
            out float residualWeight,
            out float residualDerivative)
        {
            if (duration <= 0f || elapsedSeconds >= duration || m_Curve == null)
            {
                envelope = 1f;
                residualWeight = 0f;
                residualDerivative = 0f;
                return;
            }
            float normalized = Mathf.Clamp01(elapsedSeconds / duration);
            AnimationBlendCurveEvaluator.EvaluateWithDerivative(
                m_Curve,
                normalized,
                out float curve,
                out float derivative);
            float square = normalized * normalized;
            float cube = square * normalized;
            float h10 = cube - 2f * square + normalized;
            float h11 = cube - square;
            float h10Derivative = 3f * square - 4f * normalized + 1f;
            float h11Derivative = 3f * square - 2f * normalized;
            envelope = Mathf.Clamp01(
                curve - m_CurveStartDerivative * h10 - m_CurveEndDerivative * h11);
            float envelopeDerivative = derivative -
                m_CurveStartDerivative * h10Derivative -
                m_CurveEndDerivative * h11Derivative;
            residualWeight = 1f - envelope;
            residualDerivative = -envelopeDerivative / duration;
        }

        static PoseParameterInertializationMode[] BuildParameterModes(
            CharacterPoseInertializationResponse response,
            CharacterAnimationInputContract contract)
        {
            if (response == null || contract == null || contract.Parameters.Count <= 0)
                throw new ArgumentException(
                    "Pose native Inertialization parameter contract is invalid.");
            var modes = new PoseParameterInertializationMode[
                contract.Parameters.Count];
            for (int i = 0; i < modes.Length; i++)
                modes[i] = PoseParameterInertializationMode.Snap;
            for (int i = 0; i < response.ParameterFilters.Count; i++)
            {
                CharacterPoseParameterInertializationFilter filter =
                    response.ParameterFilters[i];
                int index = -1;
                for (int parameter = 0; parameter < contract.Parameters.Count; parameter++)
                {
                    if (contract.Parameters[parameter].ParameterId.Equals(filter.ParameterId))
                    {
                        index = parameter;
                        break;
                    }
                }
                if (index < 0 || index >= modes.Length)
                    throw new InvalidOperationException(
                        $"Inertialization parameter '{filter.ParameterId}' is not in the input contract.");
                modes[index] = filter.Mode;
            }
            return modes;
        }

        static void BuildEnvelopeLayout(
            float[] denseProfiles,
            out int[] boneIndices,
            out float[] profiles)
        {
            boneIndices = new int[denseProfiles.Length];
            var indices = new Dictionary<float, int>();
            var uniqueProfiles = new List<float>();
            for (int bone = 0; bone < denseProfiles.Length; bone++)
            {
                float profile = denseProfiles[bone];
                if (!indices.TryGetValue(profile, out int index))
                {
                    index = uniqueProfiles.Count;
                    indices.Add(profile, index);
                    uniqueProfiles.Add(profile);
                }
                boneIndices[bone] = index;
            }
            profiles = uniqueProfiles.ToArray();
        }

        static float[] CreateUnitProfiles(int count)
        {
            var values = new float[count];
            for (int i = 0; i < values.Length; i++)
                values[i] = 1f;
            return values;
        }

        static void Swap<T>(ref T[] left, ref T[] right)
        {
            T[] value = left;
            left = right;
            right = value;
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Inertialization '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_HasPendingResiduals = false;
            m_PageIndex = -1;
            m_WriteBinding = default;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeInertializationHandler));
        }
    }
}
