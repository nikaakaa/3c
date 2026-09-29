using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using FlowCanvas;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeRootOrientationSource : IDisposable
    {
        bool IsRelevant { get; }
        AnimationPoseSourceId SourceId { get; }
        float SampleTime { get; }
        float Duration { get; }
        void Prepare(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        void CommitFrame();
        void DiscardFrame();
        void ResetFrame();
    }

    internal sealed class CharacterPoseNativeRootOrientationWarpHandler :
        ICharacterPoseNativeNodeHandler
    {
        struct State
        {
            internal AnimationPoseSourceId SourceId;
            internal ulong BodyDiscontinuityGeneration;
            internal float CapturedTargetAngle;
            internal float CurrentFacingError;
            internal float SourceYaw;
            internal float RootYawOffset;
            internal bool Relevant;
        }

        readonly PoseNodeId m_NodeId;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly RootMotionCurveAsset m_YawCurve;
        readonly ICharacterPoseNativeRootOrientationSource m_Source;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_PoseInput;
        EventGraphVariableBinding m_FacingErrorBinding;
        State m_CommittedState;
        State m_PendingState;
        CharacterPoseNativeLocalPoseValue m_Output;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeRootOrientationWarpHandler(
            PoseNodeId nodeId,
            in CharacterPoseNativePreparedBinding preparedBinding,
            RootMotionCurveAsset yawCurve,
            ICharacterPoseNativeRootOrientationSource source,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Root Orientation Warp handler identity is invalid.",
                    nameof(nodeId));
            if (!preparedBinding.IsValid)
                throw new ArgumentException(
                    "Pose native Root Orientation Warp prepared binding is invalid.",
                    nameof(preparedBinding));
            m_Rig = preparedBinding.Rig;
            m_YawCurve = yawCurve ? yawCurve :
                throw new ArgumentNullException(nameof(yawCurve));
            if (!m_YawCurve.TryValidate(out string curveError))
                throw new ArgumentException(curveError, nameof(yawCurve));
            if (preparedBinding.InputContract.AnimationVariables == null ||
                !preparedBinding.InputContract.AnimationVariables.TryGet(
                    CharacterAnimationVariableIds.FacingError,
                    out _))
            {
                throw new ArgumentException(
                    "Pose native Root Orientation Warp requires the FacingError EventGraph variable.",
                    nameof(preparedBinding));
            }
            m_NodeId = nodeId;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            if (m_OutputBuffer.BoneCount != m_Rig.PoseBoneCount)
                throw new ArgumentException(
                    "Pose native Root Orientation Warp buffer layout is invalid.",
                    nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.RootOrientationWarp;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || node.RootOrientationYawCurveSlot == null)
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' does not match its graph node.");
            m_FacingErrorBinding = runtime.InstanceContext.VariableContract.Bind(
                CharacterAnimationVariableIds.FacingError);
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
            m_Source.ResetFrame();
            m_CommittedState = default;
            m_PendingState = default;
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
                    $"Root Orientation Warp '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_PendingState = m_CommittedState;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            m_Source.Prepare(runtime, in input, in lineage);
            return null;
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "result")
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeLocalPoseValue inputValue =
                runtime.ReadInput(m_PoseInput, m_NodeId, "pose");
            CharacterPoseNativePoseReadBinding input = inputValue.Native;
            if (!input.IsValid || input.Space != CharacterPoseSpace.Local ||
                input.Availability[0] != AnimationPoseAvailability.Pose ||
                input.CompletionIdentity != runtime.CurrentLineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' requires the current native Local Pose.");
            }
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> poses =
                m_WriteBinding.DenseLocalPoses;
            poses.CopyFrom(input.DenseLocalPoses);
            ref readonly CharacterPoseNativeFrameInput frame =
                ref runtime.CurrentInput;
            EventGraphValue facingValue = frame.ParameterFrame.RequireValue(
                m_FacingErrorBinding);
            if (facingValue.Kind != EventGraphValueKind.Float32 ||
                !float.IsFinite(facingValue.Float32Value))
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' FacingError is not Float32.");
            }
            if (m_Source.IsRelevant)
            {
                if (!m_Source.SourceId.IsValid ||
                    !float.IsFinite(m_Source.SampleTime) || m_Source.SampleTime < 0f ||
                    !float.IsFinite(m_Source.Duration) || m_Source.Duration <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Root Orientation Warp '{NodeId}' source sampling state is invalid.");
                }
                if (!m_PendingState.Relevant ||
                    m_PendingState.SourceId != m_Source.SourceId ||
                    m_PendingState.BodyDiscontinuityGeneration !=
                    frame.FactFrame.BodyDiscontinuityGeneration)
                {
                    m_PendingState.CapturedTargetAngle = Mathf.DeltaAngle(
                        0f,
                        facingValue.Float32Value);
                }
                m_PendingState.Relevant = true;
                m_PendingState.SourceId = m_Source.SourceId;
                m_PendingState.BodyDiscontinuityGeneration =
                    frame.FactFrame.BodyDiscontinuityGeneration;
                m_PendingState.CurrentFacingError = Mathf.DeltaAngle(
                    0f,
                    facingValue.Float32Value);
                float sampleTime = Mathf.Clamp(
                    m_Source.SampleTime,
                    0f,
                    m_Source.Duration);
                m_PendingState.SourceYaw = m_YawCurve.EvaluateYaw(sampleTime);
                float authorProgress = Mathf.Abs(m_YawCurve.TotalYaw) <= 0.00001f
                    ? 0f
                    : m_PendingState.SourceYaw / m_YawCurve.TotalYaw;
                m_PendingState.RootYawOffset = Mathf.DeltaAngle(
                    0f,
                    facingValue.Float32Value -
                    m_PendingState.CapturedTargetAngle +
                    m_PendingState.CapturedTargetAngle * authorProgress);
                poses[m_Rig.RootPhysicalBoneIndex] = new AnimationLocalBonePose(
                    poses[m_Rig.RootPhysicalBoneIndex].Position,
                    Quaternion.AngleAxis(
                        m_PendingState.RootYawOffset,
                        Vector3.up) *
                    poses[m_Rig.RootPhysicalBoneIndex].Rotation,
                    poses[m_Rig.RootPhysicalBoneIndex].Scale);
            }
            else
            {
                m_PendingState = default;
            }
            m_WriteBinding.CompletedAt[0] = m_WriteBinding.CompletionIdentity;
            m_WriteBinding.InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding);
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
            return m_Output;
        }

        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;

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
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity ||
                m_Output.Native.InvalidReason[0] != AnimationPoseNativeInvalidReason.None)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' pending output is invalid.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            m_Source.CommitFrame();
            m_CommittedState = m_PendingState;
            if (m_Output != null &&
                m_Output.CompletionIdentity == lineage.CompletionIdentity)
                m_CommittedPageIndex = m_PageIndex;
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            m_Source.DiscardFrame();
            m_PendingState = m_CommittedState;
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
            m_Source.ResetFrame();
            m_CommittedPageIndex = -1;
            m_CommittedState = default;
            ClearFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Source.Dispose();
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            ClearFrame();
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeRootOrientationWarpHandler));
        }
    }
}
