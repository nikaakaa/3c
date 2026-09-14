using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
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

        internal static void RegisterBlendStack(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                stackFactory,
            Func<PoseNodeId, AnimationPoseSourceId,
                AnimationResolvedPoseSourceSample> actionSampleProvider,
            Func<PoseNodeId, AnimationPoseSourceId,
                PresentationPoseSourceSample> providerSampleProvider,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(
                registry,
                source,
                sourceLeaseProvider,
                stackFactory,
                actionSampleProvider,
                providerSampleProvider,
                bufferFactory);
            var creator = new BlendStackCreator(
                source,
                sourceLeaseProvider,
                stackFactory,
                actionSampleProvider,
                providerSampleProvider,
                bufferFactory);
            registry.Register(
                CharacterPoseNodeKind.BlendStack,
                creator.Create);
        }

        internal static void RegisterAnimationSlot(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                stackFactory,
            Func<PoseNodeId, AnimationPoseSourceId,
                AnimationResolvedPoseSourceSample> actionSampleProvider,
            Func<PoseNodeId, AnimationPoseSourceId,
                PresentationPoseSourceSample> providerSampleProvider,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(
                registry,
                source,
                sourceLeaseProvider,
                stackFactory,
                actionSampleProvider,
                providerSampleProvider,
                bufferFactory);
            var creator = new AnimationSlotCreator(
                source,
                sourceLeaseProvider,
                stackFactory,
                actionSampleProvider,
                providerSampleProvider,
                bufferFactory);
            registry.Register(
                CharacterPoseNodeKind.AnimationSlot,
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

        static void RequireArguments(
            CharacterPoseNativeNodeHandlerRegistry registry,
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Delegate stackFactory,
            Delegate actionSampleProvider,
            Delegate providerSampleProvider,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceLeaseProvider == null)
                throw new ArgumentNullException(nameof(sourceLeaseProvider));
            if (stackFactory == null)
                throw new ArgumentNullException(nameof(stackFactory));
            if (actionSampleProvider == null)
                throw new ArgumentNullException(nameof(actionSampleProvider));
            if (providerSampleProvider == null)
                throw new ArgumentNullException(nameof(providerSampleProvider));
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

        sealed class BlendStackCreator
        {
            readonly CharacterPoseSourceModule m_Source;
            readonly Func<CharacterPoseSourceFrameLease> m_SourceLeaseProvider;
            readonly Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                m_StackFactory;
            readonly Func<PoseNodeId, AnimationPoseSourceId,
                AnimationResolvedPoseSourceSample> m_ActionSampleProvider;
            readonly Func<PoseNodeId, AnimationPoseSourceId,
                PresentationPoseSourceSample> m_ProviderSampleProvider;
            readonly Func<CharacterPoseCanvasNode,
                CharacterPoseNativeNodePoseBuffer> m_BufferFactory;

            internal BlendStackCreator(
                CharacterPoseSourceModule source,
                Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
                Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                    stackFactory,
                Func<PoseNodeId, AnimationPoseSourceId,
                    AnimationResolvedPoseSourceSample> actionSampleProvider,
                Func<PoseNodeId, AnimationPoseSourceId,
                    PresentationPoseSourceSample> providerSampleProvider,
                Func<CharacterPoseCanvasNode,
                    CharacterPoseNativeNodePoseBuffer> bufferFactory)
            {
                m_Source = source;
                m_SourceLeaseProvider = sourceLeaseProvider;
                m_StackFactory = stackFactory;
                m_ActionSampleProvider = actionSampleProvider;
                m_ProviderSampleProvider = providerSampleProvider;
                m_BufferFactory = bufferFactory;
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context)
            {
                AnimationBlendStackRuntime stack = null;
                CharacterPoseNativeNodePoseBuffer buffer = null;
                try
                {
                    stack = m_StackFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native Blend Stack factory returned no stack for '{node.NodeId}'.");
                    buffer = m_BufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeBlendStackHandler(
                        stack,
                        m_Source,
                        new CharacterPoseNativeBlendStackSourceModuleBinding(
                            m_Source,
                            m_SourceLeaseProvider,
                            m_ActionSampleProvider,
                            m_ProviderSampleProvider),
                        buffer);
                }
                catch
                {
                    buffer?.Dispose();
                    stack?.Dispose();
                    throw;
                }
            }
        }

        sealed class AnimationSlotCreator
        {
            readonly CharacterPoseSourceModule m_Source;
            readonly Func<CharacterPoseSourceFrameLease> m_SourceLeaseProvider;
            readonly Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                m_StackFactory;
            readonly Func<PoseNodeId, AnimationPoseSourceId,
                AnimationResolvedPoseSourceSample> m_ActionSampleProvider;
            readonly Func<PoseNodeId, AnimationPoseSourceId,
                PresentationPoseSourceSample> m_ProviderSampleProvider;
            readonly Func<CharacterPoseCanvasNode,
                CharacterPoseNativeNodePoseBuffer> m_BufferFactory;

            internal AnimationSlotCreator(
                CharacterPoseSourceModule source,
                Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
                Func<CharacterPoseCanvasNode, AnimationBlendStackRuntime>
                    stackFactory,
                Func<PoseNodeId, AnimationPoseSourceId,
                    AnimationResolvedPoseSourceSample> actionSampleProvider,
                Func<PoseNodeId, AnimationPoseSourceId,
                    PresentationPoseSourceSample> providerSampleProvider,
                Func<CharacterPoseCanvasNode,
                    CharacterPoseNativeNodePoseBuffer> bufferFactory)
            {
                m_Source = source;
                m_SourceLeaseProvider = sourceLeaseProvider;
                m_StackFactory = stackFactory;
                m_ActionSampleProvider = actionSampleProvider;
                m_ProviderSampleProvider = providerSampleProvider;
                m_BufferFactory = bufferFactory;
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context)
            {
                AnimationBlendStackRuntime stack = null;
                CharacterPoseNativeNodePoseBuffer innerBuffer = null;
                CharacterPoseNativeNodePoseBuffer outputBuffer = null;
                CharacterPoseNativeAnimationSlotSourceBinding slotSource = null;
                try
                {
                    stack = m_StackFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native Animation Slot factory returned no stack for '{node.NodeId}'.");
                    innerBuffer = m_BufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native slot buffer factory returned no inner buffer for '{node.NodeId}'.");
                    slotSource = new CharacterPoseNativeAnimationSlotSourceBinding(
                        stack,
                        m_Source,
                        new CharacterPoseNativeBlendStackSourceModuleBinding(
                            m_Source,
                            m_SourceLeaseProvider,
                            m_ActionSampleProvider,
                            m_ProviderSampleProvider),
                        innerBuffer);
                    outputBuffer = m_BufferFactory(node) ??
                        throw new InvalidOperationException(
                            $"Pose native slot buffer factory returned no output buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeAnimationSlotHandler(
                        node.NodeId,
                        node.AnimationSlotId,
                        node.SelectionAvailability,
                        slotSource,
                        outputBuffer);
                }
                catch
                {
                    slotSource?.Dispose();
                    if (slotSource == null)
                    {
                        innerBuffer?.Dispose();
                        stack?.Dispose();
                    }
                    outputBuffer?.Dispose();
                    throw;
                }
            }
        }
    }
}
