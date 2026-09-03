using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseOutputOperationModule
    {
        CharacterPoseValuePageSlice m_Values;
        CharacterPoseGraphNativeBinding m_Frame;
        CharacterFinalPosePublicationOutputBinding m_Output;

        internal void BindFrame(
            in CharacterPoseValuePageSlice values,
            CharacterPoseGraphNativeBinding frame,
            in CharacterFinalPosePublicationOutputBinding output)
        {
            if (!values.IsValid || !output.IsValid)
                throw new ArgumentException("Pose Output frame binding is invalid.");
            frame.RequireValid();
            m_Values = values;
            m_Frame = frame;
            m_Output = output;
        }

        internal void EvaluateOutputPose(
            in CharacterPoseNativeOperationHeader header,
            in CharacterPoseNativeOutputOperation operation)
        {
            int input = operation.InputPoseValueIndex;
            if (!m_Values.IsInputReady(input, header.Index))
            {
                AnimationPoseNativeInvalidReason reason =
                    AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
                m_Values.RecordGraphInvalid(reason, header.Index);
                m_Output.WriteInvalid(
                    reason,
                    (ulong)header.Index + 1UL);
                return;
            }
            ulong continuity = CharacterPosePureMath.CombineContinuity(
                m_Values.Continuity(input),
                (ulong)header.Index + 1UL,
                header.Index);
            if (m_Values.Availability(input) == AnimationPoseAvailability.NoPose)
            {
                AnimationPoseNativeInvalidReason reason =
                    AnimationPoseNativeInvalidReason.PoseGraphOutputInvalid;
                m_Values.RecordGraphInvalid(reason, header.Index);
                m_Output.WriteInvalid(reason, continuity);
                return;
            }
            if (m_Values.Availability(input) == AnimationPoseAvailability.Invalid)
            {
                AnimationPoseNativeInvalidReason reason =
                    CharacterPosePureMath.NormalizeInvalidReason(m_Values.InvalidReason(input));
                m_Values.RecordGraphInvalid(reason, header.Index);
                m_Output.WriteInvalid(reason, continuity);
                return;
            }
            if (!m_Values.TryValidateValueDeep(
                    input,
                    out AnimationPoseNativeInvalidReason invalidReason))
            {
                invalidReason = CharacterPosePureMath.NormalizeInvalidReason(invalidReason);
                m_Values.RecordGraphInvalid(invalidReason, header.Index);
                m_Output.WriteInvalid(invalidReason, continuity);
                return;
            }
            var inputBinding = new AnimationPoseValueNativeReadBinding(
                in m_Frame,
                input);
            m_Output.WritePose(
                in inputBinding,
                m_Values.OutputWeight(input),
                continuity);
        }

    }
}
