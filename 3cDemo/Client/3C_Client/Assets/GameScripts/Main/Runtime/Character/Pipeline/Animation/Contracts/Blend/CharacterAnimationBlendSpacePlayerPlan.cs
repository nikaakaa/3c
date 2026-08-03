using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationBlendSpacePlayerPlan
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_PresentationPoseSourceIndex = -1;
        [SerializeField] int m_OperationIndex = -1;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] int m_XParameterIndex = -1;
        [SerializeField] int m_YParameterIndex = -1;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_InputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        [SerializeField] int m_BlendSpacePlanIndex = -1;

        internal CharacterAnimationBlendSpacePlayerPlan(
            PoseNodeId nodeId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            int operationIndex,
            int playerIndex,
            int xParameterIndex,
            int yParameterIndex,
            CharacterAnimationBlendSpaceInputRangePolicy inputRangePolicy,
            int blendSpacePlanIndex)
        {
            if (!nodeId.IsValid || !presentationPoseSourceIndex.IsValid ||
                operationIndex < 0 || playerIndex < 0 ||
                xParameterIndex < 0 || yParameterIndex < -1 ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), inputRangePolicy) ||
                blendSpacePlanIndex < 0)
                throw new ArgumentException("Blend Space Player plan is invalid.");
            m_NodeId = nodeId.Value;
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex.Value;
            m_OperationIndex = operationIndex;
            m_PlayerIndex = playerIndex;
            m_XParameterIndex = xParameterIndex;
            m_YParameterIndex = yParameterIndex;
            m_InputRangePolicy = inputRangePolicy;
            m_BlendSpacePlanIndex = blendSpacePlanIndex;
        }

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex < 0
                ? default
                : new PresentationPoseSourceIndex(m_PresentationPoseSourceIndex);
        public int OperationIndex => m_OperationIndex;
        public int PlayerIndex => m_PlayerIndex;
        public int XParameterIndex => m_XParameterIndex;
        public int YParameterIndex => m_YParameterIndex;
        public CharacterAnimationBlendSpaceInputRangePolicy InputRangePolicy => m_InputRangePolicy;
        public int BlendSpacePlanIndex => m_BlendSpacePlanIndex;

        public void RequireValid(CharacterPresentationProjection projection)
        {
            if (projection == null || !NodeId.IsValid || OperationIndex < 0 || OperationIndex >= projection.PosePlan.Operations.Count ||
                !PresentationPoseSourceIndex.IsValid || PlayerIndex < 0 ||
                XParameterIndex < 0 || XParameterIndex >= projection.PosePlan.Parameters.Count ||
                YParameterIndex < -1 || YParameterIndex >= projection.PosePlan.Parameters.Count ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), InputRangePolicy) ||
                BlendSpacePlanIndex < 0 || BlendSpacePlanIndex >= projection.BlendSpaces.Count)
                throw new InvalidOperationException("Blend Space Player plan is invalid.");
            CharacterPresentationPoseOperation operation = projection.PosePlan.Operations[OperationIndex];
            if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer || operation.NodeId != NodeId ||
                operation.PresentationPoseSourceIndex != PresentationPoseSourceIndex ||
                operation.PlayerIndex != PlayerIndex ||
                operation.ParameterIndex != XParameterIndex || operation.ParameterIndexB != YParameterIndex ||
                operation.BlendSpaceInputRangePolicy != InputRangePolicy)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' operation binding is inconsistent.");
        }
    }
}
