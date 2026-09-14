using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativePureNodeHandlerRegistration
    {
        internal static void RegisterPureValueHandlers(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                IReadOnlyList<float>> boneMaskFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
            if (boneMaskFactory == null)
                throw new ArgumentNullException(nameof(boneMaskFactory));
            var creator = new Creator(bufferFactory, boneMaskFactory);
            registry.Register(CharacterPoseNodeKind.BlendPose, creator.CreateBlend);
            registry.Register(
                CharacterPoseNodeKind.LayeredBoneBlend,
                creator.CreateLayered);
            registry.Register(
                CharacterPoseNodeKind.AdditivePose,
                creator.CreateAdditive);
            registry.Register(
                CharacterPoseNodeKind.ModifyBone,
                creator.CreateModifyBone);
            registry.Register(
                CharacterPoseNodeKind.PoseParameterResolve,
                creator.CreateParameterResolve);
            registry.Register(
                CharacterPoseNodeKind.LocalToComponentPose,
                creator.CreateLocalToComponent);
            registry.Register(
                CharacterPoseNodeKind.ComponentToLocalPose,
                creator.CreateComponentToLocal);
        }

        sealed class Creator
        {
            readonly Func<
                CharacterPoseCanvasNode,
                CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer> m_BufferFactory;
            readonly Func<
                CharacterPoseCanvasNode,
                CharacterPoseNativeInstanceContext,
                IReadOnlyList<float>> m_BoneMaskFactory;

            internal Creator(
                Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                    CharacterPoseNativeNodePoseBuffer>
                    bufferFactory,
                Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                    IReadOnlyList<float>>
                    boneMaskFactory)
            {
                m_BufferFactory = bufferFactory;
                m_BoneMaskFactory = boneMaskFactory;
            }

            internal ICharacterPoseNativeNodeHandler CreateBlend(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeBlendPoseHandler(
                        node.NodeId,
                        buffer));

            internal ICharacterPoseNativeNodeHandler CreateLayered(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context)
            {
                IReadOnlyList<float> boneMask = m_BoneMaskFactory(node, context);
                return CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeLayeredBoneBlendHandler(
                        node.NodeId,
                        preparedBinding.Rig,
                        boneMask,
                        buffer));
            }

            internal ICharacterPoseNativeNodeHandler CreateAdditive(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeAdditivePoseHandler(
                        node.NodeId,
                        preparedBinding.Rig,
                        buffer));

            internal ICharacterPoseNativeNodeHandler CreateModifyBone(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeModifyBoneHandler(
                        node.NodeId,
                        preparedBinding.Rig,
                        buffer));

            internal ICharacterPoseNativeNodeHandler CreateParameterResolve(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeParameterResolveHandler(
                        node.NodeId,
                        buffer));

            internal ICharacterPoseNativeNodeHandler CreateLocalToComponent(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeSpaceConversionHandler(
                        node.NodeId,
                        node.Kind,
                        preparedBinding.Rig,
                        buffer));

            internal ICharacterPoseNativeNodeHandler CreateComponentToLocal(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                CreateWithBuffer(
                    node,
                    in context,
                    buffer => new CharacterPoseNativeSpaceConversionHandler(
                        node.NodeId,
                        node.Kind,
                        preparedBinding.Rig,
                        buffer));

            ICharacterPoseNativeNodeHandler CreateWithBuffer(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativeInstanceContext context,
                Func<
                    CharacterPoseNativeNodePoseBuffer,
                    ICharacterPoseNativeNodeHandler> creator)
            {
                CharacterPoseNativeNodePoseBuffer buffer =
                    m_BufferFactory(node, context) ??
                    throw new InvalidOperationException(
                        $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                try
                {
                    return creator(buffer);
                }
                catch
                {
                    buffer.Dispose();
                    throw;
                }
            }
        }

    }
}
