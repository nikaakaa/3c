using System;
using System.Collections.Generic;
using FlowCanvas;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeParameterResolveHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly struct BoundParameterPolicy
        {
            internal BoundParameterPolicy(PoseParameterId parameterId, int parameterIndex, PoseParameterResolvePolicy policy)
            {
                ParameterId = parameterId;
                ParameterIndex = parameterIndex;
                Policy = policy;
            }

            internal PoseParameterId ParameterId { get; }
            internal int ParameterIndex { get; }
            internal PoseParameterResolvePolicy Policy { get; }
        }

        readonly PoseNodeId m_NodeId;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_BasePoseInput;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_SourcePoseInput;
        BoundParameterPolicy[] m_ParameterPolicies;
        int m_PageIndex = -1;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        bool m_FrameOpen;
        int m_CommittedPageIndex = -1;
        bool m_Disposed;

        internal CharacterPoseNativeParameterResolveHandler(
            PoseNodeId nodeId,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Parameter Resolve handler identity is invalid.",
                    nameof(nodeId));
            m_NodeId = nodeId;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseParameterResolve;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind)
                throw new InvalidOperationException(
                    $"Parameter Resolve handler '{NodeId}' does not match its graph node.");
            m_BasePoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "base-pose");
            m_SourcePoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "parameter-source-pose");
            IReadOnlyList<CharacterPoseParameterPolicy> policies = node.ParameterPolicies;
            m_ParameterPolicies = new BoundParameterPolicy[policies.Count];
            for (int i = 0; i < policies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = policies[i];
                if (policy == null || !policy.ParameterId.IsValid)
                    throw new InvalidOperationException(
                        $"Parameter Resolve '{NodeId}' has an invalid parameter policy.");
                PoseParameterId parameterId = policy.ParameterId;
                int parameterIndex = FindParameterIndex(
                    runtime.PreparedBinding.InputContract.Parameters,
                    parameterId);
                if (parameterIndex < 0 || parameterIndex >= m_OutputBuffer.ParameterCount)
                    throw new InvalidOperationException(
                        $"Parameter Resolve '{NodeId}' references unknown parameter '{parameterId}'.");
                if (policy.Policy < PoseParameterResolvePolicy.Base || policy.Policy > PoseParameterResolvePolicy.Min)
                    throw new InvalidOperationException(
                        $"Parameter Resolve '{NodeId}' has an unsupported policy.");
                m_ParameterPolicies[i] = new BoundParameterPolicy(parameterId, parameterIndex, policy.Policy);
            }
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
                    $"Parameter Resolve '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
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
            if (portId.Value != "pose")
                throw new InvalidOperationException(
                    $"Parameter Resolve '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeLocalPoseValue basePose =
                runtime.ReadInput(
                    m_BasePoseInput,
                    m_NodeId,
                    "base-pose");
            CharacterPoseNativeLocalPoseValue sourcePose =
                runtime.ReadInput(
                    m_SourcePoseInput,
                    m_NodeId,
                    "parameter-source-pose");
            CharacterPoseNativePoseReadBinding baseBinding =
                RequireAvailable(basePose, "Base");
            CharacterPoseNativePoseReadBinding sourceBinding =
                RequireAvailable(sourcePose, "Parameter source");
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in baseBinding,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> outputPoses = m_WriteBinding.DenseLocalPoses;
            outputPoses.CopyFrom(baseBinding.DenseLocalPoses);
            ResolveParameters(
                in baseBinding,
                in sourceBinding);
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding);
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
            if (m_Output == null)
                return;
            if (m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Space != CharacterPoseSpace.Local ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Parameter Resolve '{NodeId}' produced an invalid pending Pose.");
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

        void ResolveParameters(
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding sourcePose)
        {
            NativeSlice<float> outputParameters = m_WriteBinding.PoseParameters;
            NativeSlice<byte> outputAvailability =
                m_WriteBinding.PoseParameterAvailability;
            for (int policyIndex = 0;
                 policyIndex < m_ParameterPolicies.Length;
                 policyIndex++)
            {
                BoundParameterPolicy policy = m_ParameterPolicies[policyIndex];
                int parameterIndex = policy.ParameterIndex;
                byte baseAvailable = basePose.PoseParameterAvailability[parameterIndex];
                byte sourceAvailable = sourcePose.PoseParameterAvailability[parameterIndex];
                if (baseAvailable > 1 || sourceAvailable > 1)
                    throw new InvalidOperationException(
                        $"Parameter Resolve '{NodeId}' has invalid availability for '{policy.ParameterId}'.");
                switch (policy.Policy)
                {
                    case PoseParameterResolvePolicy.Base:
                        outputAvailability[parameterIndex] = baseAvailable;
                        outputParameters[parameterIndex] = basePose.PoseParameters[parameterIndex];
                        break;
                    case PoseParameterResolvePolicy.Overlay:
                        outputAvailability[parameterIndex] = sourceAvailable;
                        outputParameters[parameterIndex] = sourcePose.PoseParameters[parameterIndex];
                        break;
                    case PoseParameterResolvePolicy.Weighted:
                        ResolveWeighted(
                            parameterIndex,
                            in basePose,
                            in sourcePose,
                            outputParameters,
                            outputAvailability);
                        break;
                    case PoseParameterResolvePolicy.Max:
                        ResolveExtremum(
                            parameterIndex,
                            in basePose,
                            in sourcePose,
                            outputParameters,
                            outputAvailability,
                            true);
                        break;
                    case PoseParameterResolvePolicy.Min:
                        ResolveExtremum(
                            parameterIndex,
                            in basePose,
                            in sourcePose,
                            outputParameters,
                            outputAvailability,
                            false);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Parameter Resolve '{NodeId}' has an unsupported policy.");
                }
            }
        }

        static void ResolveWeighted(
            int index,
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding sourcePose,
            NativeSlice<float> outputParameters,
            NativeSlice<byte> outputAvailability)
        {
            byte baseAvailable = basePose.PoseParameterAvailability[index];
            byte sourceAvailable = sourcePose.PoseParameterAvailability[index];
            float baseWeight = baseAvailable != 0 ? basePose.OutputWeight[0] : 0f;
            float sourceWeight = sourceAvailable != 0 ? sourcePose.OutputWeight[0] : 0f;
            float total = baseWeight + sourceWeight;
            if (total <= 0f)
            {
                outputAvailability[index] = 0;
                outputParameters[index] = 0f;
                return;
            }
            outputAvailability[index] = 1;
            outputParameters[index] =
                ((baseAvailable != 0 ? basePose.PoseParameters[index] * baseWeight : 0f) +
                 (sourceAvailable != 0 ? sourcePose.PoseParameters[index] * sourceWeight : 0f)) /
                total;
        }

        static void ResolveExtremum(
            int index,
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding sourcePose,
            NativeSlice<float> outputParameters,
            NativeSlice<byte> outputAvailability,
            bool maximum)
        {
            byte baseAvailable = basePose.PoseParameterAvailability[index];
            byte sourceAvailable = sourcePose.PoseParameterAvailability[index];
            if (baseAvailable == 0 && sourceAvailable == 0)
            {
                outputAvailability[index] = 0;
                outputParameters[index] = 0f;
                return;
            }
            if (baseAvailable == 0)
            {
                outputAvailability[index] = 1;
                outputParameters[index] = sourcePose.PoseParameters[index];
                return;
            }
            if (sourceAvailable == 0)
            {
                outputAvailability[index] = 1;
                outputParameters[index] = basePose.PoseParameters[index];
                return;
            }
            outputAvailability[index] = 1;
            outputParameters[index] = maximum
                ? Math.Max(basePose.PoseParameters[index], sourcePose.PoseParameters[index])
                : Math.Min(basePose.PoseParameters[index], sourcePose.PoseParameters[index]);
        }

        static int FindParameterIndex(
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters,
            PoseParameterId parameterId)
        {
            for (int i = 0; i < parameters.Count; i++)
                if (parameters[i]?.ParameterId.Equals(parameterId) == true)
                    return i;
            return -1;
        }

        static CharacterPoseNativePoseReadBinding RequireAvailable(
            CharacterPoseNativeLocalPoseValue value,
            string branch)
        {
            ref readonly CharacterPoseNativePoseReadBinding binding = ref value.Native;
            if (!binding.IsValid ||
                binding.Space != CharacterPoseSpace.Local ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.CompletedAt[0] != binding.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Parameter Resolve {branch} input is unavailable.");
            }
            return binding;
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
                    $"Parameter Resolve '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeParameterResolveHandler));
        }
    }
}
