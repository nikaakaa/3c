using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeManagedNodeRegistration
    {
        internal static void RegisterStateMachine(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, ICharacterPoseNativeStateMachineSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator(node =>
                CreateSourceHandler(
                    node,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeStateMachineHandler(
                            value.NodeId,
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.PoseStateMachine,
                creator.Create);
        }

        internal static void RegisterLinkedPose(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, ICharacterPoseNativeLinkedPoseSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator(node =>
                CreateSourceHandler(
                    node,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeLinkedPoseHandler(
                            value.NodeId,
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.LinkedPoseCall,
                creator.Create);
        }

        internal static void RegisterMotionMatching(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode,
                ICharacterPoseNativeMotionMatchingSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator(node =>
                CreateSourceHandler(
                    node,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeMotionMatchingHandler(
                            value.NodeId,
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.MotionMatchingPose,
                creator.Create);
        }

        internal static void RegisterHistoryCollector(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode,
                CharacterPoseHistoryId> historyIdFactory,
            Func<CharacterPoseCanvasNode,
                ICharacterPoseNativeHistoryCollectorSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            if (historyIdFactory == null)
                throw new ArgumentNullException(nameof(historyIdFactory));
            var creator = new Creator(node =>
                CreateSourceHandler(
                    node,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeHistoryCollectorHandler(
                            value.NodeId,
                            historyIdFactory(value),
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.PoseHistoryCollector,
                creator.Create);
        }

        internal static void RegisterEntryPose(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, ICharacterPoseNativeEntryPoseSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator(node =>
                CreateSourceHandler(
                    node,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeEntryPoseHandler(
                            value.NodeId,
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.EntryPoseInput,
                creator.Create);
        }

        static ICharacterPoseNativeNodeHandler CreateSourceHandler<TSource>(
            CharacterPoseCanvasNode node,
            Func<CharacterPoseCanvasNode, TSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory,
            Func<CharacterPoseCanvasNode, TSource,
                CharacterPoseNativeNodePoseBuffer,
                ICharacterPoseNativeNodeHandler> handlerFactory)
            where TSource : class, IDisposable
        {
            TSource source = null;
            CharacterPoseNativeNodePoseBuffer buffer = null;
            try
            {
                source = sourceFactory(node) ??
                    throw new InvalidOperationException(
                        $"Pose native source factory returned no source for '{node.NodeId}'.");
                buffer = bufferFactory(node) ??
                    throw new InvalidOperationException(
                        $"Pose native buffer factory returned no buffer for '{node.NodeId}'.");
                return handlerFactory(node, source, buffer);
            }
            catch
            {
                buffer?.Dispose();
                source?.Dispose();
                throw;
            }
        }

        static void RequireArguments<TSource>(
            CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, TSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
            where TSource : class, IDisposable
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (sourceFactory == null)
                throw new ArgumentNullException(nameof(sourceFactory));
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
        }

        sealed class Creator
        {
            readonly Func<CharacterPoseCanvasNode,
                ICharacterPoseNativeNodeHandler> m_Create;

            internal Creator(
                Func<CharacterPoseCanvasNode,
                    ICharacterPoseNativeNodeHandler> create)
            {
                m_Create = create ?? throw new ArgumentNullException(nameof(create));
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                m_Create(node);
        }
    }
}
