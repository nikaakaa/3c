using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseLinkedOperationModule
    {
        readonly CharacterPoseValueWorkspace m_Context;

        internal CharacterPoseLinkedOperationModule(
            CharacterPoseValueWorkspace context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal bool EvaluateLinkedPoseCall(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeLinkedPoseOperation operation)
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

            if (operation.OutputPoseValueIndex >= 0)
            {
                if (candidate.OutputPoseValueIndex < 0 ||
                    !m_Context.IsInputReady(candidate.OutputPoseValueIndex, header.Index) ||
                    !m_Context.TryCopyValue(
                        candidate.OutputPoseValueIndex,
                        operation.OutputPoseValueIndex,
                        header.Index))
                {
                    m_Context.SetInvalid(
                        operation.OutputPoseValueIndex,
                        control.Generation,
                        AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete,
                        header.Index);
                    return false;
                }
                m_Context.m_ValueContinuityIdentities[operation.OutputPoseValueIndex] = CharacterPoseValueWorkspace.CombineContinuity(
                    m_Context.m_ValueContinuityIdentities[operation.OutputPoseValueIndex],
                    control.Generation,
                    header.Index);
                if (control.PoseDiscontinuity != 0)
                {
                    ulong continuity = m_Context.m_ValueContinuityIdentities[operation.OutputPoseValueIndex];
                    PoseDiscontinuity discontinuity = PoseDiscontinuity.Reset(
                        CharacterPoseValueWorkspace.CombineContinuity(control.Generation, continuity, header.Index),
                        m_Context.m_CompletionIdentity,
                        default,
                        continuity,
                        PoseDiscontinuityResetReason.BranchReplacement,
                        control.Generation,
                        false);
                    m_Context.m_ValueDiscontinuities[operation.OutputPoseValueIndex] =
                        PoseDiscontinuityNative.From(in discontinuity);
                }
            }

            return operation.OutputPoseValueIndex >= 0;
        }

        internal bool IsFragmentActive(int fragmentIndex) =>
            (uint)fragmentIndex < (uint)m_Context.m_LinkedPoseActiveFragments.Length &&
            m_Context.m_LinkedPoseActiveFragments[fragmentIndex] == 1;

    }
}
