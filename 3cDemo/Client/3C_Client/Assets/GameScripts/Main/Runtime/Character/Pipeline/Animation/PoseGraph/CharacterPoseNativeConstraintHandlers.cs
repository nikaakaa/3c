using System;
using System.Collections.Generic;
using FlowCanvas;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeConstraintServiceBinding
    {
        readonly CharacterPoseConstraintRuntime m_Constraints;
        readonly CharacterPoseWorldContextAdapter m_WorldContext;
        readonly int m_FootPlacementWeightParameterIndex;

        internal CharacterPoseNativeConstraintServiceBinding(
            CharacterPoseConstraintRuntime constraints,
            CharacterPoseWorldContextAdapter worldContext,
            int footPlacementWeightParameterIndex)
        {
            m_Constraints = constraints ??
                throw new ArgumentNullException(nameof(constraints));
            if (m_Constraints.HasFootPlacement && worldContext == null)
                throw new ArgumentNullException(nameof(worldContext));
            if (m_Constraints.HasFootPlacement && footPlacementWeightParameterIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(footPlacementWeightParameterIndex));
            m_WorldContext = worldContext;
            m_FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
        }

        internal CharacterPoseConstraintRuntime Constraints => m_Constraints;

        internal CharacterFootPlacementConstraintOperationResult
            EvaluateFootPlacement(
                CharacterPoseNativeGraphRuntime runtime,
                in CharacterFootPlacementConstraintHandle handle,
                in CharacterPoseNativePoseReadBinding componentPose,
                bool hasWeightOverride,
                float weightOverride)
        {
            RequireOpen();
            ulong completionIdentity = runtime.CurrentLineage.CompletionIdentity;
            ulong frameSequence = runtime.CurrentInput.PresentationFrame;
            if (!m_Constraints.HasFootPlacement)
            {
                return m_Constraints.RecordUnavailableFootPlacement(
                    in handle,
                    frameSequence,
                    completionIdentity);
            }
            AnimationPoseValueNativeReadBinding inputBinding =
                new AnimationPoseValueNativeReadBinding(in componentPose);
            CharacterPoseNativeFrameInput input = runtime.CurrentInput;
            CharacterBodyPresentationFrame body = input.BodyFrame;
            CharacterPresentationFactFrame facts = input.FactFrame;
            CharacterAnimationPoseInputFrame parameters = input.ParameterFrame;
            CharacterFootPlacementFrameInput frame =
                m_WorldContext.BuildFootPlacement(
                    input.ActorId,
                    frameSequence,
                    input.DeltaSeconds,
                    in body,
                    in facts,
                    in parameters,
                    completionIdentity,
                    in inputBinding,
                    m_FootPlacementWeightParameterIndex);
            if (hasWeightOverride)
            {
                CharacterPresentationFactFrame frameFacts = frame.Facts;
                CharacterAnimationPoseInputFrame frameParameters =
                    frame.ParameterFrame;
                CharacterFootPlacementPoseInput framePose = frame.Pose;
                frame = new CharacterFootPlacementFrameInput(
                    frame.ActorId,
                    frame.RenderFrame,
                    frame.PresentationDeltaSeconds,
                    weightOverride,
                    frame.Body,
                    in frameFacts,
                    in frameParameters,
                    in framePose);
            }
            return m_Constraints.EvaluateFootPlacement(
                in handle,
                in frame);
        }

        internal CharacterPoseBoneContributionOperationResult
            EvaluatePoseBoneContribution(
                in CharacterPoseBoneContributionConstraintHandle handle,
                NativeSlice<AnimationLocalBonePose> componentPose,
                ulong frameSequence,
                ulong completionIdentity)
        {
            RequireOpen();
            return m_Constraints.ExecutePoseBoneContribution(
                in handle,
                componentPose,
                frameSequence,
                completionIdentity);
        }

        internal CharacterFullBodyIkGoalAssemblerOperationResult
            EvaluateGoalAssembler(
                in CharacterFullBodyIkGoalAssemblerConstraintHandle handle,
                ulong frameSequence,
                ulong completionIdentity)
        {
            RequireOpen();
            return m_Constraints.ExecuteGoalAssembler(
                in handle,
                frameSequence,
                completionIdentity);
        }

        internal void BindGoalSet(in CharacterFullBodyIkGoalSet value)
        {
            RequireOpen();
            m_Constraints.BindPendingGoalSet(in value);
        }

        internal CharacterFullBodyIkConstraintOperationResult EvaluateFullBodyIk(
            in CharacterFullBodyIkConstraintHandle handle,
            NativeSlice<AnimationLocalBonePose> outputPose,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireOpen();
            return m_Constraints.ExecuteFullBodyIk(
                in handle,
                outputPose,
                frameSequence,
                completionIdentity);
        }

        internal NativeSlice<CharacterFullBodyIkGoal> RequireContributionGoals(
            in CharacterFullBodyIkGoalContributionHeader header)
        {
            RequireOpen();
            return m_Constraints.RequirePendingContributionGoals(in header);
        }

        internal CharacterFullBodyIkGoalSet RequireGoalSet(
            in CharacterFullBodyIkGoalSetHeader header)
        {
            RequireOpen();
            return m_Constraints.RequirePendingGoalSet(in header);
        }

        void RequireOpen()
        {
            if (!m_Constraints.HasPendingFrame)
                throw new InvalidOperationException(
                    "Pose native Constraint service has no pending frame.");
        }
    }

    internal abstract class CharacterPoseNativeConstraintNodeHandler :
        ICharacterPoseNativeNodeHandler
    {
        protected readonly PoseNodeId m_NodeId;
        protected readonly CharacterPoseNativeConstraintServiceBinding m_Service;
        protected bool m_FrameOpen;
        bool m_Disposed;

        protected CharacterPoseNativeConstraintNodeHandler(
            PoseNodeId nodeId,
            CharacterPoseNodeKind kind,
            CharacterPoseNativeConstraintServiceBinding service)
        {
            if (!nodeId.IsValid || service == null)
                throw new ArgumentException(
                    "Pose native Constraint handler binding is invalid.");
            m_NodeId = nodeId;
            Kind = kind;
            m_Service = service;
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind { get; }

        public virtual void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            if (runtime.Graph.RequireNode(NodeId).Kind != Kind)
                throw new InvalidOperationException(
                    $"Pose Constraint handler '{NodeId}' does not match its graph node.");
        }

        public virtual void Start(CharacterPoseNativeGraphRuntime runtime) =>
            RequireAlive();

        public virtual void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            ClearFrame();
        }

        public virtual void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Pose Constraint handler '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            OnBeginFrame();
        }

        protected virtual void OnBeginFrame() { }

        public virtual IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage) => null;

        public abstract CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage);

        public virtual void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
        }

        public virtual void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
        }

        public abstract void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage);

        public virtual void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output) =>
            ClearFrame();

        public virtual void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason) =>
            ClearFrame();

        public virtual void Stop(CharacterPoseNativeGraphRuntime runtime) =>
            ClearFrame();

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            OnDispose();
            ClearFrame();
        }

        protected virtual void OnDispose() { }

        protected virtual void ClearFrame() => m_FrameOpen = false;

        protected void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Pose Constraint handler '{NodeId}' has no open frame.");
        }

        protected void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeConstraintNodeHandler));
        }
    }

    internal sealed class CharacterPoseNativeFootPlacementHandler :
        CharacterPoseNativeConstraintNodeHandler
    {
        readonly CharacterFootPlacementConstraintHandle m_Handle;
        CharacterPoseNativeGoalContributionValue m_Output;

        internal CharacterPoseNativeFootPlacementHandler(
            PoseNodeId nodeId,
            in CharacterFootPlacementConstraintHandle handle,
            CharacterPoseNativeConstraintServiceBinding service)
            : base(nodeId, CharacterPoseNodeKind.FootPlacement, service)
        {
            if (!handle.IsValid)
                throw new ArgumentException(
                    "Pose native Foot Placement handle is invalid.",
                    nameof(handle));
            m_Handle = handle;
        }

        public override CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "contribution")
                throw new InvalidOperationException(
                    $"Foot Placement '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeComponentPoseValue input =
                runtime.ReadInput<CharacterPoseNativeComponentPoseValue>(
                    node,
                    "pose");
            CharacterPoseNativePoseReadBinding binding = input.Native;
            if (!binding.IsValid ||
                binding.Space != CharacterPoseSpace.Component ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.CompletedAt[0] != binding.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Foot Placement '{NodeId}' received an unavailable Component Pose.");
            }
            bool hasWeightOverride = false;
            float weightOverride = 0f;
            ValueInput<CharacterPoseNativeParameterValue> weight =
                node.GetInputPort("weight") as
                ValueInput<CharacterPoseNativeParameterValue>;
            if (weight != null && weight.isConnected)
            {
                CharacterPoseNativeParameterValue value = runtime.ReadInput<
                    CharacterPoseNativeParameterValue>(node, "weight");
                if (value.Value.Kind != BTSMTL.EventGraphs.EventGraphValueKind.Float32 ||
                    !float.IsFinite(value.Value.Float32Value) ||
                    value.Value.Float32Value < 0f ||
                    value.Value.Float32Value > 1f)
                {
                    throw new InvalidOperationException(
                        $"Foot Placement '{NodeId}' weight override must be a Float32 in [0, 1].");
                }
                hasWeightOverride = true;
                weightOverride = value.Value.Float32Value;
            }
            CharacterFootPlacementConstraintOperationResult result =
                m_Service.EvaluateFootPlacement(
                    runtime,
                    in m_Handle,
                    in binding,
                    hasWeightOverride,
                    weightOverride);
            if (!result.IsValid)
                throw new InvalidOperationException(
                    $"Foot Placement '{NodeId}' returned an invalid Constraint result.");
            CharacterFullBodyIkGoalContributionHeader header =
                result.Contribution;
            NativeSlice<CharacterFullBodyIkGoal> goals =
                m_Service.RequireContributionGoals(in header);
            m_Output = new CharacterPoseNativeGoalContributionValue(
                NodeId,
                header,
                goals);
            return m_Output;
        }

        public override void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null ||
                m_Output.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Foot Placement '{NodeId}' has no pending Goal Contribution.");
            }
        }

        protected override void ClearFrame()
        {
            base.ClearFrame();
            m_Output = null;
        }
    }

    internal sealed class CharacterPoseNativePoseBoneIkGoalsHandler :
        CharacterPoseNativeConstraintNodeHandler
    {
        readonly CharacterPoseBoneContributionConstraintHandle m_Handle;
        CharacterPoseNativeGoalContributionValue m_Output;

        internal CharacterPoseNativePoseBoneIkGoalsHandler(
            PoseNodeId nodeId,
            in CharacterPoseBoneContributionConstraintHandle handle,
            CharacterPoseNativeConstraintServiceBinding service)
            : base(nodeId, CharacterPoseNodeKind.PoseBoneIKGoals, service)
        {
            if (!handle.IsValid)
                throw new ArgumentException(
                    "Pose native Pose Bone IK Goals handle is invalid.",
                    nameof(handle));
            m_Handle = handle;
        }

        public override CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "contribution")
                throw new InvalidOperationException(
                    $"Pose Bone IK Goals '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeComponentPoseValue input =
                runtime.ReadInput<CharacterPoseNativeComponentPoseValue>(
                    node,
                    "pose");
            CharacterPoseNativePoseReadBinding binding = input.Native;
            if (!binding.IsValid ||
                binding.Space != CharacterPoseSpace.Component ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.CompletedAt[0] != binding.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Pose Bone IK Goals '{NodeId}' received an unavailable Component Pose.");
            }
            CharacterPoseNativeFrameInput frame = runtime.CurrentInput;
            CharacterPoseBoneContributionOperationResult result =
                m_Service.EvaluatePoseBoneContribution(
                    in m_Handle,
                    binding.DenseLocalPoses,
                    frame.PresentationFrame,
                    runtime.CurrentLineage.CompletionIdentity);
            if (!result.IsValid)
                throw new InvalidOperationException(
                    $"Pose Bone IK Goals '{NodeId}' returned an invalid Constraint result.");
            CharacterFullBodyIkGoalContributionHeader header =
                result.Contribution;
            m_Output = new CharacterPoseNativeGoalContributionValue(
                NodeId,
                header,
                m_Service.RequireContributionGoals(in header));
            return m_Output;
        }

        public override void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null ||
                m_Output.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Pose Bone IK Goals '{NodeId}' has no pending Goal Contribution.");
            }
        }

        protected override void ClearFrame()
        {
            base.ClearFrame();
            m_Output = null;
        }
    }

    internal sealed class CharacterPoseNativeFullBodyIkGoalAssemblerHandler :
        CharacterPoseNativeConstraintNodeHandler
    {
        readonly CharacterFullBodyIkGoalAssemblerConstraintHandle m_Handle;
        CharacterPoseNativeFullBodyIkGoalsValue m_Output;

        internal CharacterPoseNativeFullBodyIkGoalAssemblerHandler(
            PoseNodeId nodeId,
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle,
            CharacterPoseNativeConstraintServiceBinding service)
            : base(nodeId, CharacterPoseNodeKind.FullBodyIkGoalAssembler, service)
        {
            if (!handle.IsValid)
                throw new ArgumentException(
                    "Pose native Goal Assembler handle is invalid.",
                    nameof(handle));
            m_Handle = handle;
        }

        public override CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "goals")
                throw new InvalidOperationException(
                    $"Full Body IK Goal Assembler '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeFrameInput frame = runtime.CurrentInput;
            CharacterFullBodyIkGoalAssemblerOperationResult result =
                m_Service.EvaluateGoalAssembler(
                    in m_Handle,
                    frame.PresentationFrame,
                    runtime.CurrentLineage.CompletionIdentity);
            if (!result.IsValid)
                throw new InvalidOperationException(
                    $"Full Body IK Goal Assembler '{NodeId}' returned an invalid Constraint result.");
            CharacterFullBodyIkGoalSet goalSet =
                m_Service.RequireGoalSet(in result.GoalSet);
            m_Output = new CharacterPoseNativeFullBodyIkGoalsValue(
                NodeId,
                goalSet);
            return m_Output;
        }

        public override void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null ||
                m_Output.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Full Body IK Goal Assembler '{NodeId}' has no pending Goal Set.");
            }
        }

        protected override void ClearFrame()
        {
            base.ClearFrame();
            m_Output = null;
        }
    }

    internal sealed class CharacterPoseNativeFullBodyIkHandler :
        CharacterPoseNativeConstraintNodeHandler
    {
        readonly CharacterFullBodyIkConstraintHandle m_Handle;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        CharacterPoseNativeComponentPoseValue m_Output;

        internal CharacterPoseNativeFullBodyIkHandler(
            PoseNodeId nodeId,
            in CharacterFullBodyIkConstraintHandle handle,
            CharacterPoseNativeNodePoseBuffer outputBuffer,
            CharacterPoseNativeConstraintServiceBinding service)
            : base(nodeId, CharacterPoseNodeKind.FullBodyIK, service)
        {
            if (!handle.IsValid)
                throw new ArgumentException(
                    "Pose native Full Body IK handle is invalid.",
                    nameof(handle));
            m_Handle = handle;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        protected override void OnBeginFrame()
        {
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_Output = null;
        }

        public override void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            m_CommittedPageIndex = -1;
            base.Reset(runtime, resetGeneration);
        }

        public override CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "result")
                throw new InvalidOperationException(
                    $"Full Body IK '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeComponentPoseValue input =
                runtime.ReadInput<CharacterPoseNativeComponentPoseValue>(
                    node,
                    "pose");
            CharacterPoseNativePoseReadBinding binding = input.Native;
            if (!binding.IsValid ||
                binding.Space != CharacterPoseSpace.Component ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.CompletedAt[0] != binding.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Full Body IK '{NodeId}' received an unavailable Component Pose.");
            }
            ValueInput<CharacterPoseNativeFullBodyIkGoalsValue> goalsInput =
                node.GetInputPort("goals") as
                ValueInput<CharacterPoseNativeFullBodyIkGoalsValue>;
            if (goalsInput != null && goalsInput.isConnected)
            {
                CharacterPoseNativeFullBodyIkGoalsValue goals = runtime.ReadInput<
                    CharacterPoseNativeFullBodyIkGoalsValue>(node, "goals");
                m_Service.BindGoalSet(in goals.Value);
            }
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in binding,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            for (int i = 0; i < outputPoses.Length; i++)
                outputPoses[i] = binding.DenseLocalPoses[i];
            CharacterPoseNativeFrameInput frame = runtime.CurrentInput;
            CharacterFullBodyIkConstraintOperationResult result =
                m_Service.EvaluateFullBodyIk(
                    in m_Handle,
                    outputPoses,
                    frame.PresentationFrame,
                    runtime.CurrentLineage.CompletionIdentity);
            if (!result.IsValid ||
                !result.Matches(
                    in m_Handle,
                    frame.PresentationFrame,
                    runtime.CurrentLineage.CompletionIdentity))
            {
                throw new InvalidOperationException(
                    $"Full Body IK '{NodeId}' returned an invalid Constraint result.");
            }
            NativeSlice<AnimationPoseAvailability> availability =
                m_WriteBinding.Availability;
            NativeSlice<AnimationPoseNativeInvalidReason> invalidReason =
                m_WriteBinding.InvalidReason;
            NativeSlice<ulong> completedAt = m_WriteBinding.CompletedAt;
            availability[0] = AnimationPoseAvailability.Pose;
            invalidReason[0] = AnimationPoseNativeInvalidReason.None;
            completedAt[0] = m_WriteBinding.CompletionIdentity;
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    in m_WriteBinding,
                    CharacterPoseSpace.Component);
            m_Output = new CharacterPoseNativeComponentPoseValue(NodeId, in output);
            return m_Output;
        }

        public override void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null ||
                m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Space != CharacterPoseSpace.Component ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Full Body IK '{NodeId}' has no valid pending Component Pose.");
            }
        }

        public override void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            if (m_Output != null)
                m_CommittedPageIndex = m_PageIndex;
            ClearFrame();
        }

        public override void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            m_CommittedPageIndex = -1;
            ClearFrame();
        }

        protected override void ClearFrame()
        {
            base.ClearFrame();
            m_PageIndex = -1;
            m_WriteBinding = default;
            m_Output = null;
        }

        protected override void OnDispose()
        {
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
        }
    }
}
