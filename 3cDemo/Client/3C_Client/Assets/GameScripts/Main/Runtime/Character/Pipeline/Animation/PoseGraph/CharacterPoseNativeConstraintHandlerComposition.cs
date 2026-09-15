using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeConstraintHandlerComposition
    {
        readonly CharacterPoseNativeConstraintServiceBinding m_Service;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFootPlacementConstraintHandle> m_FootPlacementHandleFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseBoneContributionConstraintHandle> m_PoseBoneHandleFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFullBodyIkGoalAssemblerConstraintHandle> m_GoalAssemblerHandleFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFullBodyIkConstraintHandle> m_FullBodyIkHandleFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> m_BufferFactory;

        internal CharacterPoseNativeConstraintHandlerComposition(
            CharacterPoseNativeConstraintServiceBinding service,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFootPlacementConstraintHandle> footPlacementHandleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseBoneContributionConstraintHandle> poseBoneHandleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFullBodyIkGoalAssemblerConstraintHandle> goalAssemblerHandleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterFullBodyIkConstraintHandle> fullBodyIkHandleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> bufferFactory)
        {
            m_Service = service ?? throw new ArgumentNullException(nameof(service));
            m_FootPlacementHandleFactory = footPlacementHandleFactory ?? throw new ArgumentNullException(nameof(footPlacementHandleFactory));
            m_PoseBoneHandleFactory = poseBoneHandleFactory ?? throw new ArgumentNullException(nameof(poseBoneHandleFactory));
            m_GoalAssemblerHandleFactory = goalAssemblerHandleFactory ?? throw new ArgumentNullException(nameof(goalAssemblerHandleFactory));
            m_FullBodyIkHandleFactory = fullBodyIkHandleFactory ?? throw new ArgumentNullException(nameof(fullBodyIkHandleFactory));
            m_BufferFactory = bufferFactory ?? throw new ArgumentNullException(nameof(bufferFactory));
        }

        internal void Register(CharacterPoseNativeNodeHandlerRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            registry.RegisterFootPlacement(
                m_Service,
                m_FootPlacementHandleFactory);
            registry.RegisterPoseBoneIkGoals(
                m_Service,
                m_PoseBoneHandleFactory);
            registry.RegisterGoalAssembler(
                m_Service,
                m_GoalAssemblerHandleFactory);
            registry.RegisterFullBodyIk(
                m_Service,
                m_FullBodyIkHandleFactory,
                m_BufferFactory);
        }
    }
}
