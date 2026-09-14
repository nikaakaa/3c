using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeConstraintNodeRegistration
    {
        internal static void RegisterFootPlacement(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseNativeConstraintServiceBinding service,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterFootPlacementConstraintHandle>
                handleFactory)
        {
            RequireArguments(registry, service, handleFactory);
            var creator = new Creator((node, context) =>
                new CharacterPoseNativeFootPlacementHandler(
                    node.NodeId,
                    handleFactory(node, context),
                    service));
            registry.Register(
                CharacterPoseNodeKind.FootPlacement,
                creator.Create);
        }

        internal static void RegisterPoseBoneIkGoals(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseNativeConstraintServiceBinding service,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseBoneContributionConstraintHandle> handleFactory)
        {
            RequireArguments(registry, service, handleFactory);
            var creator = new Creator((node, context) =>
                new CharacterPoseNativePoseBoneIkGoalsHandler(
                    node.NodeId,
                    handleFactory(node, context),
                    service));
            registry.Register(
                CharacterPoseNodeKind.PoseBoneIKGoals,
                creator.Create);
        }

        internal static void RegisterGoalAssembler(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseNativeConstraintServiceBinding service,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterFullBodyIkGoalAssemblerConstraintHandle> handleFactory)
        {
            RequireArguments(registry, service, handleFactory);
            var creator = new Creator((node, context) =>
                new CharacterPoseNativeFullBodyIkGoalAssemblerHandler(
                    node.NodeId,
                    handleFactory(node, context),
                    service));
            registry.Register(
                CharacterPoseNodeKind.FullBodyIkGoalAssembler,
                creator.Create);
        }

        internal static void RegisterFullBodyIk(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseNativeConstraintServiceBinding service,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterFullBodyIkConstraintHandle>
                handleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, service, handleFactory);
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
            var creator = new Creator((node, context) =>
            {
                CharacterPoseNativeNodePoseBuffer buffer = bufferFactory(node, context) ??
                    throw new InvalidOperationException(
                        $"Pose native Full Body IK buffer factory returned no buffer for '{node.NodeId}'.");
                try
                {
                    return new CharacterPoseNativeFullBodyIkHandler(
                        node.NodeId,
                        handleFactory(node, context),
                        buffer,
                        service);
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }
            });
            registry.Register(
                CharacterPoseNodeKind.FullBodyIK,
                creator.Create);
        }

        static void RequireArguments(
            CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseNativeConstraintServiceBinding service,
            Delegate handleFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (handleFactory == null)
                throw new ArgumentNullException(nameof(handleFactory));
        }

        sealed class Creator
        {
            readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeNodeHandler> m_Create;

            internal Creator(
                Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                    ICharacterPoseNativeNodeHandler> create)
            {
                m_Create = create ?? throw new ArgumentNullException(nameof(create));
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                m_Create(node, context);
        }
    }
}
