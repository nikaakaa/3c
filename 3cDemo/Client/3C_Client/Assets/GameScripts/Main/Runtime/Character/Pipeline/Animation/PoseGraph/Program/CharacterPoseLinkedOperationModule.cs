using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseLinkedOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseLinkedOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal bool EvaluateLinkedPoseCall(AnimationPoseGraphNativeOperation operation)
        {
            if ((uint)operation.LinkedPoseCallIndex >= (uint)m_Context.m_LinkedPoseCalls.Length)
                return false;
            AnimationPoseGraphNativeLinkedPoseCall call =
                m_Context.m_LinkedPoseCalls[operation.LinkedPoseCallIndex];
            AnimationPoseGraphNativeLinkedPoseCallControl control =
                m_Context.m_LinkedPoseCallControls[operation.LinkedPoseCallIndex];
            if (!control.IsActive || control.CandidateIndex < call.CandidateStart ||
                control.CandidateIndex >= call.CandidateStart + call.CandidateCount ||
                (uint)control.CandidateIndex >= (uint)m_Context.m_LinkedPoseCandidates.Length)
            {
                return false;
            }
            AnimationPoseGraphNativeLinkedPoseCandidate candidate =
                m_Context.m_LinkedPoseCandidates[control.CandidateIndex];
            if (!IsFragmentActive(candidate.FragmentIndex))
                return false;

            if (operation.OutputValueIndex >= 0)
            {
                if (candidate.OutputPoseValueIndex < 0 ||
                    !m_Context.IsInputReady(candidate.OutputPoseValueIndex, operation.Index) ||
                    !m_Context.TryCopyValue(
                        candidate.OutputPoseValueIndex,
                        operation.OutputValueIndex,
                        operation.Index))
                {
                    m_Context.SetInvalid(
                        operation.OutputValueIndex,
                        control.Generation,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        operation.Index);
                    return false;
                }
                m_Context.m_ValueContinuityIdentities[operation.OutputValueIndex] = CharacterPoseExecutionContext.CombineContinuity(
                    m_Context.m_ValueContinuityIdentities[operation.OutputValueIndex],
                    control.Generation,
                    operation.Index);
                if (control.PoseDiscontinuity != 0)
                {
                    ulong continuity = m_Context.m_ValueContinuityIdentities[operation.OutputValueIndex];
                    PoseDiscontinuity discontinuity = PoseDiscontinuity.Reset(
                        CharacterPoseExecutionContext.CombineContinuity(control.Generation, continuity, operation.Index),
                        m_Context.m_CompletionIdentity,
                        default,
                        continuity,
                        PoseDiscontinuityResetReason.BranchReplacement,
                        control.Generation,
                        false);
                    m_Context.m_ValueDiscontinuities[operation.OutputValueIndex] =
                        PoseDiscontinuityNative.From(in discontinuity);
                }
            }

            return operation.OutputValueIndex >= 0;
        }

        internal bool IsFragmentActive(int fragmentIndex) =>
            (uint)fragmentIndex < (uint)m_Context.m_LinkedPoseActiveFragments.Length &&
            m_Context.m_LinkedPoseActiveFragments[fragmentIndex] == 1;

    }
}
