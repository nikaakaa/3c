using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeSourceHandlerComposition
    {
        readonly CharacterPoseSourceModule m_Source;
        readonly Func<CharacterPoseSourceFrameLease> m_SourceLeaseProvider;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationClipPlayerRuntime> m_ClipPlayerFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationBlendSpacePlayerRuntime> m_BlendSpacePlayerFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationSelectedPosePlayerRuntime> m_SelectedPlayerFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, PresentationPoseSourceSample> m_SelectedSampleFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationBlendStackRuntime> m_StackFactory;
        readonly Func<CharacterPoseNativeInstanceContext, PoseNodeId, AnimationPoseSourceId, AnimationResolvedPoseSourceSample> m_ActionSampleProvider;
        readonly Func<CharacterPoseNativeInstanceContext, PoseNodeId, AnimationPoseSourceId, PresentationPoseSourceSample> m_ProviderSampleProvider;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, int> m_BindingIndexFactory;
        readonly Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> m_BufferFactory;
        readonly Func<CharacterPoseCanvasNode, IActionPresentationClockPolicy> m_ClockPolicyFactory;

        internal CharacterPoseNativeSourceHandlerComposition(
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationClipPlayerRuntime> clipPlayerFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationBlendSpacePlayerRuntime> blendSpacePlayerFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationSelectedPosePlayerRuntime> selectedPlayerFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, PresentationPoseSourceSample> selectedSampleFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, AnimationBlendStackRuntime> stackFactory,
            Func<CharacterPoseNativeInstanceContext, PoseNodeId, AnimationPoseSourceId, AnimationResolvedPoseSourceSample> actionSampleProvider,
            Func<CharacterPoseNativeInstanceContext, PoseNodeId, AnimationPoseSourceId, PresentationPoseSourceSample> providerSampleProvider,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, int> bindingIndexFactory,
            Func<CharacterPoseCanvasNode, CharacterPoseNativeInstanceContext, CharacterPoseNativeNodePoseBuffer> bufferFactory,
            Func<CharacterPoseCanvasNode, IActionPresentationClockPolicy> clockPolicyFactory)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_SourceLeaseProvider = sourceLeaseProvider ?? throw new ArgumentNullException(nameof(sourceLeaseProvider));
            m_ClipPlayerFactory = clipPlayerFactory ?? throw new ArgumentNullException(nameof(clipPlayerFactory));
            m_BlendSpacePlayerFactory = blendSpacePlayerFactory ?? throw new ArgumentNullException(nameof(blendSpacePlayerFactory));
            m_SelectedPlayerFactory = selectedPlayerFactory ?? throw new ArgumentNullException(nameof(selectedPlayerFactory));
            m_SelectedSampleFactory = selectedSampleFactory ?? throw new ArgumentNullException(nameof(selectedSampleFactory));
            m_StackFactory = stackFactory ?? throw new ArgumentNullException(nameof(stackFactory));
            m_ActionSampleProvider = actionSampleProvider ?? throw new ArgumentNullException(nameof(actionSampleProvider));
            m_ProviderSampleProvider = providerSampleProvider ?? throw new ArgumentNullException(nameof(providerSampleProvider));
            m_BindingIndexFactory = bindingIndexFactory ?? throw new ArgumentNullException(nameof(bindingIndexFactory));
            m_BufferFactory = bufferFactory ?? throw new ArgumentNullException(nameof(bufferFactory));
            m_ClockPolicyFactory = clockPolicyFactory ?? throw new ArgumentNullException(nameof(clockPolicyFactory));
        }

        internal void Register(CharacterPoseNativeNodeHandlerRegistry registry)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            registry.RegisterClipPlayer(
                m_Source,
                m_SourceLeaseProvider,
                m_ClipPlayerFactory,
                m_BindingIndexFactory,
                m_BufferFactory,
                m_ClockPolicyFactory);
            registry.RegisterBlendSpacePlayer(
                m_Source,
                m_SourceLeaseProvider,
                m_BlendSpacePlayerFactory,
                m_BindingIndexFactory,
                m_BufferFactory);
            registry.RegisterSelectedPosePlayer(
                m_Source,
                m_SourceLeaseProvider,
                m_SelectedPlayerFactory,
                m_SelectedSampleFactory,
                m_BindingIndexFactory,
                m_BufferFactory);
            registry.RegisterBlendStack(
                m_Source,
                m_SourceLeaseProvider,
                m_StackFactory,
                m_ActionSampleProvider,
                m_ProviderSampleProvider,
                m_BufferFactory);
            registry.RegisterAnimationSlot(
                m_Source,
                m_SourceLeaseProvider,
                m_StackFactory,
                m_ActionSampleProvider,
                m_ProviderSampleProvider,
                m_BufferFactory);
        }
    }
}
