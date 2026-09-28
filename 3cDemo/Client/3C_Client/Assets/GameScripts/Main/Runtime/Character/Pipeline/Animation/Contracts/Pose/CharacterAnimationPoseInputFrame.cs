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
            m_VariableFrame.IsValid;

        internal bool IsPublishedVariableFrame => m_VariableFrame.IsValid;

        internal CharacterAnimationVariableFrame PublishedVariableFrame =>
            m_VariableFrame;

        internal static CharacterAnimationVariableContract BindContract(
            CharacterAnimationVariableContract contract,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            if (contract == null || parameters == null)
                throw new ArgumentException("Animation Event Graph variable binding is incomplete.");
            var seen = new HashSet<PoseParameterId>();
            for (int i = 0; i < parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = parameters[i];
                if (parameter.Usage != CharacterPoseParameterUsage.Control)
                    continue;
                PoseParameterId parameterId = parameter.ParameterId;
                if (!parameterId.IsValid ||
                    !contract.TryGet(parameterId.Value, out _))
                {
                    throw new InvalidOperationException(
                        $"Animation Event Graph variable '{parameterId}' is missing from the published contract.");
                }
                if (!seen.Add(parameterId))
                    throw new ArgumentException(
                        $"Animation Event Graph variable binding contains duplicate '{parameterId}'.");
            }
            return contract;
        }

        internal static CharacterAnimationPoseInputFrame FromPublishedVariables(
            CharacterAnimationVariableFrame frame,
            CharacterAnimationVariableContract contract)
        {
            if (!frame.IsValid || contract == null ||
                !ReferenceEquals(frame.Values.Contract, contract.Source))
                throw new ArgumentException("Animation Event Graph variable frame does not match its bound contract.");
            return new CharacterAnimationPoseInputFrame(frame);
        }

        internal EventGraphValue RequireValue(EventGraphVariableBinding binding) =>
            m_VariableFrame.Require(binding);

        internal bool TryRead(
            PoseParameterId parameterId,
            out EventGraphValue value)
        {
            if (!parameterId.IsValid)
            {
                value = default;
                return false;
            }
            if (m_VariableFrame.IsValid)
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
