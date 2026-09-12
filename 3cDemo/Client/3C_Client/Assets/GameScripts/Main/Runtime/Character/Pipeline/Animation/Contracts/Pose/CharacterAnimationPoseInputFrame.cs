using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterAnimationPoseInputFrame
    {
        readonly CharacterAnimationVariableFrame m_VariableFrame;
        readonly PoseParameterId[] m_ParameterIds;

        CharacterAnimationPoseInputFrame(
            CharacterAnimationVariableFrame variableFrame,
            PoseParameterId[] parameterIds)
        {
            m_VariableFrame = variableFrame;
            m_ParameterIds = parameterIds;
        }

        internal bool IsValid =>
            m_VariableFrame != null && m_ParameterIds != null;

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
            var ids = new PoseParameterId[parameterIds.Count];
            for (int i = 0; i < ids.Length; i++)
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
                    if (ids[prior].Equals(parameterId))
                        throw new ArgumentException(
                            $"Animation Event Graph variable frame contains duplicate '{parameterId}'.");
                }
                ids[i] = parameterId;
            }
            return new CharacterAnimationPoseInputFrame(frame, ids);
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
            {
                if (!Contains(parameterId))
                {
                    value = default;
                    return false;
                }
                return m_VariableFrame.TryRead(parameterId.Value, out value);
            }
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

        bool Contains(PoseParameterId parameterId)
        {
            for (int i = 0; i < m_ParameterIds.Length; i++)
            {
                if (m_ParameterIds[i].Equals(parameterId))
                    return true;
            }
            return false;
        }
    }
}
