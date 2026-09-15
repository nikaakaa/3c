using System;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeManagedHandlerComposition
    {
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeLinkedPoseSource> m_LinkedPoseSourceFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeMotionMatchingSource> m_MotionMatchingSourceFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseHistoryId> m_HistoryIdFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeHistoryCollectorSource> m_HistorySourceFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeEntryPoseSource> m_EntryPoseSourceFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> m_SubgraphRequestIdFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> m_SubgraphInstanceIdFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> m_SubgraphResetGenerationFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, string> m_SubgraphReasonFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, RootMotionCurveAsset> m_RootOrientationCurveFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeRootOrientationSource> m_RootOrientationSourceFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> m_BufferFactory;

        internal CharacterPoseNativeManagedHandlerComposition(
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeLinkedPoseSource> linkedPoseSourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeMotionMatchingSource> motionMatchingSourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseHistoryId> historyIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeHistoryCollectorSource> historySourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeEntryPoseSource> entryPoseSourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> subgraphRequestIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> subgraphInstanceIdFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ulong> subgraphResetGenerationFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, string> subgraphReasonFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, RootMotionCurveAsset> rootOrientationCurveFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, ICharacterPoseNativeRootOrientationSource> rootOrientationSourceFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> bufferFactory)
        {
            m_LinkedPoseSourceFactory = linkedPoseSourceFactory ?? throw new ArgumentNullException(nameof(linkedPoseSourceFactory));
            m_MotionMatchingSourceFactory = motionMatchingSourceFactory ?? throw new ArgumentNullException(nameof(motionMatchingSourceFactory));
            m_HistoryIdFactory = historyIdFactory ?? throw new ArgumentNullException(nameof(historyIdFactory));
            m_HistorySourceFactory = historySourceFactory ?? throw new ArgumentNullException(nameof(historySourceFactory));
            m_EntryPoseSourceFactory = entryPoseSourceFactory ?? throw new ArgumentNullException(nameof(entryPoseSourceFactory));
            m_SubgraphRequestIdFactory = subgraphRequestIdFactory ?? throw new ArgumentNullException(nameof(subgraphRequestIdFactory));
            m_SubgraphInstanceIdFactory = subgraphInstanceIdFactory ?? throw new ArgumentNullException(nameof(subgraphInstanceIdFactory));
            m_SubgraphResetGenerationFactory = subgraphResetGenerationFactory ?? throw new ArgumentNullException(nameof(subgraphResetGenerationFactory));
            m_SubgraphReasonFactory = subgraphReasonFactory ?? throw new ArgumentNullException(nameof(subgraphReasonFactory));
            m_RootOrientationCurveFactory = rootOrientationCurveFactory ?? throw new ArgumentNullException(nameof(rootOrientationCurveFactory));
            m_RootOrientationSourceFactory = rootOrientationSourceFactory ?? throw new ArgumentNullException(nameof(rootOrientationSourceFactory));
            m_BufferFactory = bufferFactory ?? throw new ArgumentNullException(nameof(bufferFactory));
        }

        internal void Register(CharacterPoseNativeNodeHandlerRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            registry.RegisterLinkedPose(
                m_LinkedPoseSourceFactory,
                m_BufferFactory);
            registry.RegisterMotionMatching(
                m_MotionMatchingSourceFactory,
                m_BufferFactory);
            registry.RegisterHistoryCollector(
                m_HistoryIdFactory,
                m_HistorySourceFactory,
                m_BufferFactory);
            registry.RegisterEntryPose(
                m_EntryPoseSourceFactory,
                m_BufferFactory);
            registry.RegisterSubgraph(
                m_SubgraphRequestIdFactory,
                m_SubgraphInstanceIdFactory,
                m_SubgraphResetGenerationFactory,
                m_SubgraphReasonFactory);
            registry.RegisterRootOrientationWarp(
                m_RootOrientationCurveFactory,
                m_RootOrientationSourceFactory,
                m_BufferFactory);
        }
    }
}
