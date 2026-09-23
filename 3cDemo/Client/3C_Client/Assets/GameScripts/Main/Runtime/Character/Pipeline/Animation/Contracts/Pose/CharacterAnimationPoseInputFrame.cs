using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterAnimationPoseInputFrame
    {
        readonly CharacterAnimationVariableFrame m_VariableFrame;

        CharacterAnimationPoseInputFrame(
            CharacterAnimationVariableFrame variableFrame)
        {
            m_VariableFrame = variableFrame;
        }

        internal bool IsValid =>
            m_VariableFrame != null;

        internal bool IsPublishedVariableFrame => m_VariableFrame != null;

        internal CharacterAnimationVariableFrame PublishedVariableFrame =>
            m_VariableFrame;

        internal static CharacterAnimationPoseInputFrame FromPublishedVariables(
            CharacterAnimationVariableFrame frame,
            IReadOnlyList<PoseParameterId> parameterIds)
        {
            if (frame == null || parameterIds == null)
                throw new ArgumentException(
                    "Animation Event Graph variable frame is incomplete.");
            for (int i = 0; i < parameterIds.Count; i++)
            {
                PoseParameterId parameterId = parameterIds[i];
                if (!parameterId.IsValid ||
                    !frame.TryRead(parameterId.Value, out _))
                {
                    throw new InvalidOperationException(
                        $"Animation Event Graph variable '{parameterId}' is missing from the published frame.");
                }
                for (int prior = 0; prior < i; prior++)
                {
                    if (parameterIds[prior].Equals(parameterId))
                        throw new ArgumentException(
                            $"Animation Event Graph variable frame contains duplicate '{parameterId}'.");
                }
            }
            return new CharacterAnimationPoseInputFrame(frame);
        }

        internal bool TryRead(
            PoseParameterId parameterId,
            out EventGraphValue value)
        {
            if (!parameterId.IsValid)
            {
                value = default;
                return false;
            }
            if (m_VariableFrame != null)
                return m_VariableFrame.TryRead(parameterId.Value, out value);
            value = default;
            return false;
        }

        internal EventGraphValue RequireValue(PoseParameterId parameterId)
        {
            if (!TryRead(parameterId, out EventGraphValue value))
                throw new InvalidOperationException(
                    $"Pose input variable '{parameterId}' is unavailable in the current frame.");
            return value;
        }

        internal float Require(PoseParameterId parameterId)
        {
            EventGraphValue value = RequireValue(parameterId);
            if (value.Kind != EventGraphValueKind.Float32)
                throw new InvalidOperationException(
                    $"Pose input variable '{parameterId}' is not Float32.");
            return value.Float32Value;
        }

    }
}
