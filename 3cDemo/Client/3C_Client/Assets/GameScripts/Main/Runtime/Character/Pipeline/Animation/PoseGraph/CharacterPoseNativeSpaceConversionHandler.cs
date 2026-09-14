using System;
using System.Collections.Generic;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativePoseBufferCopy
    {
        internal static void CopyMetadata(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output)
        {
            if (input.DenseLocalPoses.Length != output.DenseLocalPoses.Length ||
                input.PoseParameters.Length != output.PoseParameters.Length ||
                input.ContributionCount[0] < 0 ||
                input.ContributionCount[0] > output.Contributions.Length ||
                input.Contributions.Length < input.ContributionCount[0] ||
                input.DenseContributionWeights.Length <
                input.ContributionCount[0] * input.DenseLocalPoses.Length)
            {
                throw new InvalidOperationException(
                    "Pose native node buffer layouts are incompatible.");
            }
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                output.DenseVelocities;
            NativeSlice<float> outputParameters = output.PoseParameters;
            NativeSlice<byte> outputParameterAvailability =
                output.PoseParameterAvailability;
            NativeSlice<AnimationPrimitivePoseContribution> outputContributions =
                output.Contributions;
            NativeSlice<float> outputContributionWeights =
                output.DenseContributionWeights;
            NativeSlice<int> outputContributionCount = output.ContributionCount;
            NativeSlice<float> outputOutputWeight = output.OutputWeight;
            NativeSlice<AnimationFootFeatureSample> outputLeftFootFeatures =
                output.LeftFootFeatures;
            NativeSlice<AnimationFootFeatureSample> outputRightFootFeatures =
                output.RightFootFeatures;
            NativeSlice<byte> outputHasFootFeatures = output.HasFootFeatures;
            NativeSlice<AnimationPoseAvailability> outputAvailability =
                output.Availability;
            NativeSlice<ulong> outputContinuity = output.ContinuityIdentity;
            NativeSlice<PoseDiscontinuityNative> outputDiscontinuity =
                output.Discontinuity;
            NativeSlice<AnimationPoseNativeInvalidReason> outputInvalidReason =
                output.InvalidReason;
            NativeSlice<ulong> outputCompletedAt = output.CompletedAt;
            for (int i = 0; i < outputVelocities.Length; i++)
                outputVelocities[i] = input.DenseVelocities[i];
            for (int i = 0; i < outputParameters.Length; i++)
            {
                outputParameters[i] = input.PoseParameters[i];
                outputParameterAvailability[i] =
                    input.PoseParameterAvailability[i];
            }
            for (int i = 0; i < outputContributions.Length; i++)
                outputContributions[i] = i < input.Contributions.Length
                    ? input.Contributions[i]
                    : default;
            int boneCount = output.DenseLocalPoses.Length;
            for (int contribution = 0;
                 contribution < input.ContributionCount[0];
                 contribution++)
            {
                for (int bone = 0; bone < boneCount; bone++)
                {
                    outputContributionWeights[
                        contribution * boneCount + bone] =
                        input.DenseContributionWeights[
                            contribution * boneCount + bone];
                }
            }
            for (int i = input.ContributionCount[0] * boneCount;
                 i < outputContributionWeights.Length;
                 i++)
            {
                outputContributionWeights[i] = 0f;
            }
            outputContributionCount[0] = input.ContributionCount[0];
            outputOutputWeight[0] = input.OutputWeight[0];
            outputLeftFootFeatures[0] = input.LeftFootFeatures[0];
            outputRightFootFeatures[0] = input.RightFootFeatures[0];
            outputHasFootFeatures[0] = input.HasFootFeatures[0];
            outputAvailability[0] = input.Availability[0];
            outputContinuity[0] = input.ContinuityIdentity[0];
            outputDiscontinuity[0] = input.Discontinuity[0];
            outputInvalidReason[0] = input.InvalidReason[0];
            outputCompletedAt[0] = input.CompletedAt[0];
        }
    }

    internal sealed class CharacterPoseNativeSpaceConversionHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly CharacterPoseNodeKind m_Kind;
        readonly CharacterPoseSpace m_InputSpace;
        readonly CharacterPoseSpace m_OutputSpace;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly CharacterComponentBonePose[] m_ComponentScratch;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        CharacterPoseNativePortValue m_Output;
        bool m_FrameOpen;
        int m_CommittedPageIndex = -1;
        int m_PageIndex = -1;
        bool m_Disposed;

        internal CharacterPoseNativeSpaceConversionHandler(
            PoseNodeId nodeId,
            CharacterPoseNodeKind kind,
            CharacterAnimationRigPayload rig,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid ||
                (kind != CharacterPoseNodeKind.LocalToComponentPose &&
                 kind != CharacterPoseNodeKind.ComponentToLocalPose))
            {
                throw new ArgumentException(
                    "Pose native space conversion binding is invalid.");
            }
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Rig.RequireValid();
            m_NodeId = nodeId;
            m_Kind = kind;
            m_InputSpace = kind == CharacterPoseNodeKind.LocalToComponentPose
                ? CharacterPoseSpace.Local
                : CharacterPoseSpace.Component;
            m_OutputSpace = kind == CharacterPoseNodeKind.LocalToComponentPose
                ? CharacterPoseSpace.Component
                : CharacterPoseSpace.Local;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
            m_ComponentScratch = new CharacterComponentBonePose[
                m_Rig.PoseBoneCount];
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => m_Kind;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind)
                throw new InvalidOperationException(
                    $"Pose space conversion handler '{NodeId}' does not match its graph node.");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            ClearFrame();
            m_CommittedPageIndex = -1;
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_Output = null;
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
            string outputPort = m_Kind == CharacterPoseNodeKind.LocalToComponentPose
                ? "component-pose"
                : "local-pose";
            if (portId.Value != outputPort)
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            string inputPort = m_Kind == CharacterPoseNodeKind.LocalToComponentPose
                ? "local-pose"
                : "component-pose";
            CharacterPoseNativePortValue inputValue = runtime.ReadInputValue(
                node,
                new PosePortId(inputPort));
            CharacterPoseNativePoseReadBinding input =
                RequireInput(inputValue);
            if (input.Availability[0] != AnimationPoseAvailability.Pose)
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' requires an available Pose.");
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            if (m_Kind == CharacterPoseNodeKind.LocalToComponentPose)
                ConvertLocalToComponent(in input);
            else
                ConvertComponentToLocal(in input);
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    in m_WriteBinding,
                    m_OutputSpace);
            m_Output = m_OutputSpace == CharacterPoseSpace.Local
                ? new CharacterPoseNativeLocalPoseValue(NodeId, in output)
                : new CharacterPoseNativeComponentPoseValue(NodeId, in output);
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
            if (m_Output == null)
                return;
            if (m_Output.CompletionIdentity != lineage.CompletionIdentity ||
                m_OutputSpace == CharacterPoseSpace.Local &&
                !(m_Output is CharacterPoseNativeLocalPoseValue) ||
                m_OutputSpace == CharacterPoseSpace.Component &&
                !(m_Output is CharacterPoseNativeComponentPoseValue))
            {
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' produced an invalid pending Pose.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output != null)
                m_CommittedPageIndex = m_PageIndex;
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason) =>
            ClearFrame();

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            m_CommittedPageIndex = -1;
            ClearFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            ClearFrame();
        }

        CharacterPoseNativePoseReadBinding RequireInput(
            CharacterPoseNativePortValue value)
        {
            if (m_InputSpace == CharacterPoseSpace.Local &&
                value is CharacterPoseNativeLocalPoseValue local)
                return local.Native;
            if (m_InputSpace == CharacterPoseSpace.Component &&
                value is CharacterPoseNativeComponentPoseValue component)
                return component.Native;
            throw new InvalidOperationException(
                $"Pose space conversion '{NodeId}' received the wrong input space.");
        }

        void ConvertLocalToComponent(
            in CharacterPoseNativePoseReadBinding input)
        {
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            for (int i = 0; i < m_Rig.PoseBoneCount; i++)
            {
                AnimationLocalBonePose local = input.DenseLocalPoses[i];
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        local,
                        m_ComponentScratch,
                        0,
                        out CharacterComponentBonePose component))
                {
                    throw new InvalidOperationException(
                        $"Pose space conversion '{NodeId}' could not derive component bone #{i}.");
                }
                m_ComponentScratch[i] = component;
                outputPoses[i] = new AnimationLocalBonePose(
                    component.Position,
                    component.Rotation,
                    component.Scale);
            }
        }

        void ConvertComponentToLocal(
            in CharacterPoseNativePoseReadBinding input)
        {
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            for (int i = 0; i < m_Rig.PoseBoneCount; i++)
            {
                AnimationLocalBonePose value = input.DenseLocalPoses[i];
                if (!value.IsValid)
                    throw new InvalidOperationException(
                        $"Pose space conversion '{NodeId}' received invalid component bone #{i}.");
                m_ComponentScratch[i] = new CharacterComponentBonePose(
                    value.Position,
                    value.Rotation,
                    value.Scale);
            }
            for (int i = 0; i < m_Rig.PoseBoneCount; i++)
            {
                int parent = m_Rig.GetPoseParentIndex(i);
                AnimationLocalBonePose local;
                if (parent < 0)
                {
                    CharacterComponentBonePose component = m_ComponentScratch[i];
                    local = new AnimationLocalBonePose(
                        component.Position,
                        component.Rotation,
                        component.Scale);
                }
                else if (!CharacterPoseConstraintMath.TryCreateLocal(
                             m_ComponentScratch[i],
                             m_ComponentScratch[parent],
                             out local))
                {
                    throw new InvalidOperationException(
                        $"Pose space conversion '{NodeId}' could not derive local bone #{i}.");
                }
                outputPoses[i] = local;
            }
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_Output = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeSpaceConversionHandler));
        }
    }
}
