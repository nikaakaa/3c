using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationBlendSpacePlayerPlan
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_OperationIndex = -1;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] int m_SelectionInputIndex = -1;
        [SerializeField] int m_XParameterIndex = -1;
        [SerializeField] int m_YParameterIndex = -1;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_InputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        [SerializeField] int[] m_BlendSpacePlanIndices = Array.Empty<int>();

        internal CharacterAnimationBlendSpacePlayerPlan(
            PoseNodeId nodeId,
            int operationIndex,
            int playerIndex,
            int selectionInputIndex,
            int xParameterIndex,
            int yParameterIndex,
            CharacterAnimationBlendSpaceInputRangePolicy inputRangePolicy,
            int[] blendSpacePlanIndices)
        {
            if (!nodeId.IsValid || operationIndex < 0 || playerIndex < 0 || selectionInputIndex < 0 ||
                xParameterIndex < 0 || yParameterIndex < -1 ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), inputRangePolicy) ||
                blendSpacePlanIndices == null || blendSpacePlanIndices.Length == 0)
                throw new ArgumentException("Blend Space Player plan is invalid.");
            m_NodeId = nodeId.Value;
            m_OperationIndex = operationIndex;
            m_PlayerIndex = playerIndex;
            m_SelectionInputIndex = selectionInputIndex;
            m_XParameterIndex = xParameterIndex;
            m_YParameterIndex = yParameterIndex;
            m_InputRangePolicy = inputRangePolicy;
            m_BlendSpacePlanIndices = (int[])blendSpacePlanIndices.Clone();
        }

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public int OperationIndex => m_OperationIndex;
        public int PlayerIndex => m_PlayerIndex;
        public int SelectionInputIndex => m_SelectionInputIndex;
        public int XParameterIndex => m_XParameterIndex;
        public int YParameterIndex => m_YParameterIndex;
        public CharacterAnimationBlendSpaceInputRangePolicy InputRangePolicy => m_InputRangePolicy;
        public IReadOnlyList<int> BlendSpacePlanIndices => m_BlendSpacePlanIndices ?? Array.Empty<int>();

        public void RequireValid(CharacterPresentationProjection projection)
        {
            if (projection == null || !NodeId.IsValid || OperationIndex < 0 || OperationIndex >= projection.PosePlan.Operations.Count ||
                PlayerIndex < 0 || SelectionInputIndex < 0 || SelectionInputIndex >= projection.PosePlan.SelectionInputs.Count ||
                XParameterIndex < 0 || XParameterIndex >= projection.PosePlan.Parameters.Count ||
                YParameterIndex < -1 || YParameterIndex >= projection.PosePlan.Parameters.Count ||
                !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), InputRangePolicy) || BlendSpacePlanIndices.Count == 0)
                throw new InvalidOperationException("Blend Space Player plan is invalid.");
            CharacterPresentationPoseOperation operation = projection.PosePlan.Operations[OperationIndex];
            if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer || operation.NodeId != NodeId ||
                operation.PlayerIndex != PlayerIndex || operation.SelectionInputIndex != SelectionInputIndex ||
                operation.ParameterIndex != XParameterIndex || operation.ParameterIndexB != YParameterIndex ||
                operation.BlendSpaceInputRangePolicy != InputRangePolicy)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' operation binding is inconsistent.");
            var indices = new HashSet<int>();
            for (int i = 0; i < BlendSpacePlanIndices.Count; i++)
            {
                int index = BlendSpacePlanIndices[i];
                if (index < 0 || index >= projection.BlendSpaces.Count || !indices.Add(index))
                    throw new InvalidOperationException($"Blend Space Player '{NodeId}' source plan binding is invalid.");
            }
        }

        public bool ContainsPlan(int planIndex)
        {
            for (int i = 0; i < BlendSpacePlanIndices.Count; i++)
            {
                if (BlendSpacePlanIndices[i] == planIndex)
                    return true;
            }
            return false;
        }
    }
}
