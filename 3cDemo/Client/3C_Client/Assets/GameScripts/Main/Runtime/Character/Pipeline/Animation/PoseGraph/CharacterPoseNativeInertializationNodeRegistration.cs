using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeInertializationNodeRegistration
    {
        internal static void Register(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseInertializationPolicy>
                policyFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
            if (policyFactory == null)
                throw new ArgumentNullException(nameof(policyFactory));
            var creator = new Creator(bufferFactory, policyFactory);
            registry.Register(
                CharacterPoseNodeKind.Inertialization,
                creator.Create);
        }

        sealed class Creator
        {
            readonly Func<CharacterPoseCanvasNode,
                CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer> m_BufferFactory;
            readonly Func<CharacterPoseCanvasNode,
                CharacterPoseNativeInstanceContext,
                CharacterPoseInertializationPolicy> m_PolicyFactory;

            internal Creator(
                Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                    CharacterPoseNativeNodePoseBuffer>
                    bufferFactory,
                Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                    CharacterPoseInertializationPolicy>
                    policyFactory)
            {
                m_BufferFactory = bufferFactory;
                m_PolicyFactory = policyFactory;
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context)
            {
                CharacterPoseInertializationPolicy policy =
                    m_PolicyFactory(node, context) ??
                    throw new InvalidOperationException(
                        $"Pose native Inertialization policy factory returned no policy for '{node.NodeId}'.");
                CharacterPoseNativeNodePoseBuffer buffer =
                    m_BufferFactory(node, context) ??
                    throw new InvalidOperationException(
                        $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                try
                {
                    return new CharacterPoseNativeInertializationHandler(
                        node.NodeId,
                        in preparedBinding,
                        policy,
                        buffer);
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
