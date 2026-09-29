using System;
using System.Collections.Generic;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativePoseBufferCopy
    {
        internal static void ValidateLayout(
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
        }

        internal static void CopyMetadata(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output)
        {
            CopyAttributes(in input, in output);
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                output.DenseVelocities;
            outputVelocities.CopyFrom(input.DenseVelocities);
        }

        internal static void CopyAttributes(
            in CharacterPoseNativePoseReadBinding input,
            in AnimationPlayerPoseNativeWriteBinding output)
        {
            ValidateLayout(in input, in output);
            NativeSlice<float> outputParameters = output.PoseParameters;
            NativeSlice<byte> outputParameterAvailability =
                output.PoseParameterAvailability;
            NativeSlice<AnimationPrimitivePoseContribution> outputContributions =
                output.Contributions;
            NativeSlice<float> outputContributionWeights =
                output.DenseContributionWeights;
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
            outputParameters.CopyFrom(input.PoseParameters);
            outputParameterAvailability.CopyFrom(input.PoseParameterAvailability);
            int contributionCount = input.ContributionCount[0];
            ExtendContributionPrefix(in output, contributionCount);
            outputContributions.Slice(0, contributionCount).CopyFrom(input.Contributions.Slice(0, contributionCount));
            int boneCount = output.DenseLocalPoses.Length;
            int weightCount = contributionCount * boneCount;
            outputContributionWeights.Slice(0, weightCount).CopyFrom(input.DenseContributionWeights.Slice(0, weightCount));
            CompleteContributions(in output, contributionCount);
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

        internal static void ExtendContributionPrefix(
            in AnimationPlayerPoseNativeWriteBinding output,
            int contributionCount)
        {
            NativeSlice<int> count = output.ContributionCount;
            if (contributionCount > count[0])
                count[0] = contributionCount;
        }

        internal static void CompleteContributions(
            in AnimationPlayerPoseNativeWriteBinding output,
            int contributionCount)
        {
            // Pending writes retain their touched prefix until tail cleanup succeeds.
            NativeSlice<int> count = output.ContributionCount;
            int previousCount = count[0];
            NativeSlice<AnimationPrimitivePoseContribution> contributions = output.Contributions;
            NativeSlice<float> weights = output.DenseContributionWeights;
            for (int i = contributionCount; i < previousCount; i++)
                contributions[i] = default;
            int boneCount = output.DenseLocalPoses.Length;
            int previousWeightCount = previousCount * boneCount;
            for (int i = contributionCount * boneCount; i < previousWeightCount; i++)
                weights[i] = 0f;
            count[0] = contributionCount;
        }
    }

    internal sealed class CharacterPoseNativeSpaceConversionHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly CharacterPoseNodeKind m_Kind;
        readonly CharacterPoseSpace m_InputSpace;
        readonly CharacterPoseSpace m_OutputSpace;
        readonly PosePortId m_InputPort;
        readonly PosePortId m_OutputPort;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly int[] m_ParentIndices;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly CharacterComponentBonePose[] m_ComponentScratch;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_LocalPoseInput;
        FlowCanvas.ValueInput<CharacterPoseNativeComponentPoseValue> m_ComponentPoseInput;
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
            m_ParentIndices = new int[rig.PoseBoneCount];
            for (int i = 0; i < m_ParentIndices.Length; i++)
                m_ParentIndices[i] = rig.GetPoseParentIndex(i);
            m_NodeId = nodeId;
            m_Kind = kind;
            m_InputPort = new PosePortId(kind == CharacterPoseNodeKind.LocalToComponentPose
                ? "local-pose"
                : "component-pose");
            m_OutputPort = new PosePortId(kind == CharacterPoseNodeKind.LocalToComponentPose
                ? "component-pose"
                : "local-pose");
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
            if (m_InputSpace == CharacterPoseSpace.Local)
                m_LocalPoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                    node,
                    m_InputPort.Value);
            else
                m_ComponentPoseInput = runtime.RequireInputPort<CharacterPoseNativeComponentPoseValue>(
                    node,
                    m_InputPort.Value);
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
            if (portId != m_OutputPort)
                throw new InvalidOperationException(
                    $"Pose space conversion '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativePortValue inputValue = m_InputSpace == CharacterPoseSpace.Local
                ? runtime.ReadInput(m_LocalPoseInput, m_NodeId, m_InputPort.Value)
                : runtime.ReadInput(m_ComponentPoseInput, m_NodeId, m_InputPort.Value);
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
            if (m_OutputSpace == CharacterPoseSpace.Local)
            {
                m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                    m_Output as CharacterPoseNativeLocalPoseValue,
                    NodeId,
                    in output);
            }
            else
            {
                m_Output = CharacterPoseNativeComponentPoseValue.Reuse(
                    m_Output as CharacterPoseNativeComponentPoseValue,
                    NodeId,
                    in output);
            }
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
            if (m_Output == null ||
                m_Output.CompletionIdentity != lineage.CompletionIdentity)
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
            if (m_Output != null &&
                m_Output.CompletionIdentity == lineage.CompletionIdentity)
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
                        m_ParentIndices[i],
                        m_ComponentScratch,
                        0,
                        out CharacterComponentBonePose component))
                {
                    throw new InvalidOperationException(
                        $"Pose space conversion '{NodeId}' could not derive component bone #{i}.");
                }
                m_ComponentScratch[i] = component;
                outputPoses[i] = new AnimationLocalBonePose(in component);
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
                m_ComponentScratch[i] = new CharacterComponentBonePose(in value);
            }
            for (int i = 0; i < m_Rig.PoseBoneCount; i++)
            {
                int parent = m_ParentIndices[i];
                AnimationLocalBonePose local;
                if (parent < 0)
                {
                    CharacterComponentBonePose component = m_ComponentScratch[i];
                    local = new AnimationLocalBonePose(in component);
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
