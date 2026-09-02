using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseOutputOperationModule
    {
        readonly CharacterPoseExecutionContext m_Context;

        internal CharacterPoseOutputOperationModule(
            CharacterPoseExecutionContext context)
        {
            m_Context = context ??
                throw new ArgumentNullException(nameof(context));
        }

        internal void EvaluateOutputPose(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeOutputOperation operation)
        {
            int input = operation.InputPoseValueIndex;
            if (!m_Context.IsInputReady(input, header.Index))
            {
                AnimationPoseNativeInvalidReason reason =
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
                m_Context.RecordGraphInvalid(reason, header.Index);
                m_Context.m_FinalOutput.WriteInvalid(
                    reason,
                    (ulong)header.Index + 1UL);
                return;
            }
            ulong continuity = CharacterPoseExecutionContext.CombineContinuity(
                m_Context.m_ValueContinuityIdentities[input],
                (ulong)header.Index + 1UL,
                header.Index);
            if (m_Context.m_ValueAvailability[input] == AnimationPoseAvailability.NoPose)
            {
                AnimationPoseNativeInvalidReason reason =
                    AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid;
                m_Context.RecordGraphInvalid(reason, header.Index);
                m_Context.m_FinalOutput.WriteInvalid(reason, continuity);
                return;
            }
            if (m_Context.m_ValueAvailability[input] == AnimationPoseAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason =
                    CharacterPoseExecutionContext.NormalizeInvalidReason(m_Context.m_ValueInvalidReasons[input]);
                m_Context.RecordGraphInvalid(reason, header.Index);
                m_Context.m_FinalOutput.WriteInvalid(reason, continuity);
                return;
            }
            if (!m_Context.TryValidateValueDeep(
                    input,
                    out AnimationPoseNativeInvalidReason invalidReason))
            {
                invalidReason = CharacterPoseExecutionContext.NormalizeInvalidReason(invalidReason);
                m_Context.RecordGraphInvalid(invalidReason, header.Index);
                m_Context.m_FinalOutput.WriteInvalid(invalidReason, continuity);
                return;
            }
            var inputBinding = new AnimationPoseValueNativeReadBinding(
                in m_Context.m_FrameBinding,
                input);
            m_Context.m_FinalOutput.WritePose(
                in inputBinding,
                m_Context.m_ValueOutputWeights[input],
                continuity);
        }

    }
}
