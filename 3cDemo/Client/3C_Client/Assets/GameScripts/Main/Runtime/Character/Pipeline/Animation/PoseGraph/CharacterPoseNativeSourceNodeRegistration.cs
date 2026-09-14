using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeSourceNodeRegistration
    {
        internal static void RegisterClipPlayer(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, AnimationClipPlayerRuntime>
                playerFactory,
            Func<CharacterPoseCanvasNode, int> bindingIndexFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(
                registry,
                source,
                sourceLeaseProvider,
                playerFactory,
                bindingIndexFactory,
                bufferFactory);
            var creator = new Creator((node, preparedBinding) =>
            {
                AnimationClipPlayerRuntime player = null;
                CharacterPoseNativeNodePoseBuffer buffer = null;
                try
                {
                    player = playerFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native Clip Player factory returned no player for '{node.NodeId}'.");
                    buffer = bufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeClipPlayerHandler(
                        player,
                        buffer,
                        new CharacterPoseNativeClipSourceModuleBinding(
                            source,
                            sourceLeaseProvider,
                            bindingIndexFactory(node)));
                }
                catch
                {
                    buffer?.Dispose();
                    player?.Dispose();
                    throw;
                }
            });
            registry.Register(CharacterPoseNodeKind.ClipPlayer, creator.Create);
        }

        internal static void RegisterBlendSpacePlayer(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, AnimationBlendSpacePlayerRuntime>
                playerFactory,
            Func<CharacterPoseCanvasNode, int> bindingIndexFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(
                registry,
                source,
                sourceLeaseProvider,
                playerFactory,
                bindingIndexFactory,
                bufferFactory);
            var creator = new Creator((node, preparedBinding) =>
            {
                AnimationBlendSpacePlayerRuntime player = null;
                CharacterPoseNativeNodePoseBuffer buffer = null;
                try
                {
                    player = playerFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native Blend Space Player factory returned no player for '{node.NodeId}'.");
                    buffer = bufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeBlendSpacePlayerHandler(
                        player,
                        buffer,
                        new CharacterPoseNativeBlendSpaceSourceModuleBinding(
                            source,
                            sourceLeaseProvider,
                            bindingIndexFactory(node)));
                }
                catch
                {
                    buffer?.Dispose();
                    player?.Dispose();
                    throw;
                }
            });
            registry.Register(
                CharacterPoseNodeKind.BlendSpacePlayer,
                creator.Create);
        }

        internal static void RegisterSelectedPosePlayer(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, AnimationSelectedPosePlayerRuntime>
                playerFactory,
            Func<CharacterPoseCanvasNode, PresentationPoseSourceSample>
                sampleFactory,
            Func<CharacterPoseCanvasNode, int> bindingIndexFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(
                registry,
                source,
                sourceLeaseProvider,
                playerFactory,
                sampleFactory,
                bindingIndexFactory,
                bufferFactory);
            var creator = new Creator((node, preparedBinding) =>
            {
                AnimationSelectedPosePlayerRuntime player = null;
                CharacterPoseNativeNodePoseBuffer buffer = null;
                try
                {
                    player = playerFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native Selected Player factory returned no player for '{node.NodeId}'.");
                    buffer = bufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeSelectedPosePlayerHandler(
                        player,
                        buffer,
                        new CharacterPoseNativeSelectedSourceModuleBinding(
                            source,
                            sourceLeaseProvider,
                            () => sampleFactory(node),
                            bindingIndexFactory(node)));
                }
                catch
                {
                    buffer?.Dispose();
                    player?.Dispose();
                    throw;
                }
            });
            registry.Register(
                CharacterPoseNodeKind.SelectedPosePlayer,
                creator.Create);
        }

        static void RequireArguments(
            CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Delegate playerFactory,
            Func<CharacterPoseCanvasNode, int> bindingIndexFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceLeaseProvider == null)
                throw new ArgumentNullException(nameof(sourceLeaseProvider));
            if (playerFactory == null)
                throw new ArgumentNullException(nameof(playerFactory));
            if (bindingIndexFactory == null)
                throw new ArgumentNullException(nameof(bindingIndexFactory));
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
        }

        sealed class Creator
        {
            readonly Func<
                CharacterPoseCanvasNode,
                CharacterPoseNativePreparedBinding,
                ICharacterPoseNativeNodeHandler> m_Create;

            internal Creator(
                Func<
                    CharacterPoseCanvasNode,
                    CharacterPoseNativePreparedBinding,
                    ICharacterPoseNativeNodeHandler> create)
            {
                m_Create = create ?? throw new ArgumentNullException(nameof(create));
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                m_Create(node, preparedBinding);
        }
    }
}
