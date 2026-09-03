using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseLinkedOperationModule
    {
        readonly NativeArray<AnimationPoseGraphNativeLinkedPoseCall>
            m_Calls;
        readonly NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate>
            m_Candidates;
        NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> m_Controls;
        NativeArray<byte> m_ActiveFragments;
        CharacterPoseValuePageSlice m_Values;

        internal CharacterPoseLinkedOperationModule(
            NativeArray<AnimationPoseGraphNativeLinkedPoseCall> calls,
            NativeArray<AnimationPoseGraphNativeLinkedPoseCandidate> candidates)
        {
            m_Calls = calls;
            m_Candidates = candidates;
        }

        internal void BindFrame(
            in CharacterPoseValuePageSlice values,
            NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl> controls,
            NativeArray<byte> activeFragments)
        {
            if (!values.IsValid ||
                !controls.IsCreated ||
                !activeFragments.IsCreated)
            {
                throw new ArgumentException(
                    "Linked Pose frame binding is invalid.");
            }
            m_Values = values;
            m_Controls = controls;
            m_ActiveFragments = activeFragments;
        }

        internal bool EvaluateLinkedPoseCall(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeLinkedPoseOperation operation)
        {
            if ((uint)operation.LinkedPoseCallIndex >= (uint)m_Calls.Length)
                return false;
            AnimationPoseGraphNativeLinkedPoseCall call =
                m_Calls[operation.LinkedPoseCallIndex];
            AnimationPoseGraphNativeLinkedPoseCallControl control =
                m_Controls[operation.LinkedPoseCallIndex];
            if (!control.IsActive || control.CandidateIndex < call.CandidateStart ||
                control.CandidateIndex >= call.CandidateStart + call.CandidateCount ||
                (uint)control.CandidateIndex >= (uint)m_Candidates.Length)
            {
                return false;
            }
            AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                m_Candidates[control.CandidateIndex];
            if (!IsFragmentActive(candidate.FragmentIndex))
                return false;

            if (operation.OutputPoseValueIndex >= 0)
            {
                if (candidate.OutputPoseValueIndex < 0 ||
                    !m_Values.IsInputReady(candidate.OutputPoseValueIndex, header.Index) ||
                    !m_Values.TryCopyValue(
                        candidate.OutputPoseValueIndex,
                        operation.OutputPoseValueIndex,
                        header.Index))
                {
                    m_Values.SetInvalid(
                        operation.OutputPoseValueIndex,
                        control.Generation,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        header.Index);
                    return false;
                }
                ulong outputContinuity = CharacterPosePureMath.CombineContinuity(
                    m_Values.Continuity(operation.OutputPoseValueIndex),
                    control.Generation,
                    header.Index);
                m_Values.SetContinuity(
                    operation.OutputPoseValueIndex,
                    outputContinuity);
                if (control.PoseDiscontinuity != 0)
                {
                    PoseDiscontinuity discontinuity = PoseDiscontinuity.Reset(
                        CharacterPosePureMath.CombineContinuity(control.Generation, outputContinuity, header.Index),
                        m_Values.CompletionIdentity,
                        default,
                        outputContinuity,
                        PoseDiscontinuityResetReason.BranchReplacement,
                        control.Generation,
                        false);
                    PoseDiscontinuityNative nativeDiscontinuity =
                        PoseDiscontinuityNative.From(in discontinuity);
                    m_Values.SetDiscontinuity(
                        operation.OutputPoseValueIndex,
                        in nativeDiscontinuity);
                }
            }

            return operation.OutputPoseValueIndex >= 0;
        }

        internal bool IsFragmentActive(int fragmentIndex) =>
            (uint)fragmentIndex < (uint)m_ActiveFragments.Length &&
            m_ActiveFragments[fragmentIndex] == 1;

    }
}
