using System;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeManagedNodeRegistration
    {
        internal static void RegisterStateMachine(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeStateMachineSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator((node, context) =>
                CreateSourceHandler(
                    node,
                    in context,
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
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeLinkedPoseSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator((node, context) =>
                CreateSourceHandler(
                    node,
                    in context,
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
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeMotionMatchingSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator((node, context) =>
                CreateSourceHandler(
                    node,
                    in context,
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
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseHistoryId> historyIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeHistoryCollectorSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            if (historyIdFactory == null)
                throw new ArgumentNullException(nameof(historyIdFactory));
            var creator = new Creator((node, context) =>
                CreateSourceHandler(
                    node,
                    in context,
                    sourceFactory,
                    bufferFactory,
                    (value, source, buffer) =>
                        new CharacterPoseNativeHistoryCollectorHandler(
                            value.NodeId,
                            historyIdFactory(value, context),
                            source,
                            buffer)));
            registry.Register(
                CharacterPoseNodeKind.PoseHistoryCollector,
                creator.Create);
        }

        internal static void RegisterEntryPose(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeEntryPoseSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            RequireArguments(registry, sourceFactory, bufferFactory);
            var creator = new Creator((node, context) =>
                CreateSourceHandler(
                    node,
                    in context,
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

        internal static void RegisterSubgraph(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong>
                requestIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong>
                instanceIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong>
                resetGenerationFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, string>
                reasonFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (requestIdFactory == null)
                throw new ArgumentNullException(nameof(requestIdFactory));
            if (instanceIdFactory == null)
                throw new ArgumentNullException(nameof(instanceIdFactory));
            if (resetGenerationFactory == null)
                throw new ArgumentNullException(nameof(resetGenerationFactory));
            if (reasonFactory == null)
                throw new ArgumentNullException(nameof(reasonFactory));
            var creator = new Creator((node, context) =>
                new CharacterPoseNativeSubgraphHandler(
                    node.NodeId,
                    requestIdFactory(node, context),
                    instanceIdFactory(node, context),
                    resetGenerationFactory(node, context),
                    reasonFactory(node, context),
                    registry));
            registry.Register(
                CharacterPoseNodeKind.PoseSubgraph,
                creator.Create);
        }

        internal static void RegisterRootOrientationWarp(
            this CharacterPoseNativeNodeHandlerRegistry registry,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                RootMotionCurveAsset> curveFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeRootOrientationSource> sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
                bufferFactory)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (curveFactory == null)
                throw new ArgumentNullException(nameof(curveFactory));
            if (sourceFactory == null)
                throw new ArgumentNullException(nameof(sourceFactory));
            if (bufferFactory == null)
                throw new ArgumentNullException(nameof(bufferFactory));
            var creator = new PreparedCreator((node, preparedBinding, context) =>
            {
                ICharacterPoseNativeRootOrientationSource source = null;
                CharacterPoseNativeNodePoseBuffer buffer = null;
                try
                {
                    RootMotionCurveAsset curve = curveFactory(node, context) ??
                        throw new InvalidOperationException(
                            $"Pose native Root Orientation curve factory returned no curve for '{node.NodeId}'.");
                    source = sourceFactory(node, context) ??
                        throw new InvalidOperationException(
                            $"Pose native Root Orientation source factory returned no source for '{node.NodeId}'.");
                    buffer = bufferFactory(node, context) ??
                        throw new InvalidOperationException(
                            $"Pose native Root Orientation buffer factory returned no buffer for '{node.NodeId}'.");
                    return new CharacterPoseNativeRootOrientationWarpHandler(
                        node.NodeId,
                        in preparedBinding,
                        curve,
                        source,
                        buffer);
                }
                catch
                {
                    buffer?.Dispose();
                    source?.Dispose();
                    throw;
                }
            });
            registry.Register(
                CharacterPoseNodeKind.RootOrientationWarp,
                creator.Create);
        }

        static ICharacterPoseNativeNodeHandler CreateSourceHandler<TSource>(
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeInstanceContext context,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, TSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
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
                source = sourceFactory(node, context) ??
                    throw new InvalidOperationException(
                        $"Pose native source factory returned no source for '{node.NodeId}'.");
                buffer = bufferFactory(node, context) ??
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
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, TSource>
                sourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext,
                CharacterPoseNativeNodePoseBuffer>
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

        sealed class PreparedCreator
        {
            readonly Func<CharacterPoseCanvasNode,
                CharacterPoseNativePreparedBinding,
                CharacterPoseNativeInstanceContext,
                ICharacterPoseNativeNodeHandler> m_Create;

            internal PreparedCreator(
                Func<CharacterPoseCanvasNode,
                    CharacterPoseNativePreparedBinding,
                    CharacterPoseNativeInstanceContext,
                    ICharacterPoseNativeNodeHandler> create)
            {
                m_Create = create ?? throw new ArgumentNullException(nameof(create));
            }

            internal ICharacterPoseNativeNodeHandler Create(
                CharacterPoseCanvasNode node,
                in CharacterPoseNativePreparedBinding preparedBinding,
                in CharacterPoseNativeInstanceContext context) =>
                m_Create(node, preparedBinding, context);
        }
    }
}
