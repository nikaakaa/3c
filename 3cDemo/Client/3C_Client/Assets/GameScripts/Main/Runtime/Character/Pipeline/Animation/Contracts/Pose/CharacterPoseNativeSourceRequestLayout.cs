using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeSourceRequestLayout
    {
        readonly Dictionary<PoseGraphId, int> m_GraphCapacities;
        readonly Dictionary<PoseNodeId, int> m_StateMachineCapacities;

        internal CharacterPoseNativeSourceRequestLayout(
            Dictionary<PoseGraphId, int> ownedGraphCapacities,
            Dictionary<PoseNodeId, int> ownedStateMachineCapacities)
        {
            m_GraphCapacities = ownedGraphCapacities;
            m_StateMachineCapacities = ownedStateMachineCapacities;
        }

        internal int RequireGraph(PoseGraphId graphId) => m_GraphCapacities[graphId];
        internal int RequireStateMachine(PoseNodeId nodeId) => m_StateMachineCapacities[nodeId];
    }
}
