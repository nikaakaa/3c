using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Generated
{
    public sealed partial class LocomotionFullBodyPoseGraphAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var state = new GenerationState();
            LoadRootResources(state, context);
            BuildCreateSlots0(state, context);
            BuildCreateGraphs__1(state, context);
            BuildRootBindingRoot0(state, context);
            return context.Complete(state.poseAsset);
        }

        static void LoadRootResources(GenerationState state, BtsmtlAuthoringGenerationContext context)
        {
            state.asset = context.ResolveExternalAsset<CharacterPipelineDefinition>("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset", 11400000L);
            state.asset1 = context.ResolveExternalAsset<CharacterAnimationPresentationProfile>("Assets/Configs/Character/Corin/Pipeline/Presentation/Profiles/CorinAnimationPresentationProfile.asset", 11400000L);
        }

        static void BuildRootBindingRoot0(GenerationState state, BtsmtlAuthoringGenerationContext context)
        {
            state.poseAsset = BtsmtlPoseAuthoringCode.EnsureRoot(context, state.asset, state.asset1, new CharacterPoseCanvasGraph[] { state.poseGraph, state.poseGraph1, state.poseGraph2, state.poseGraph3, state.poseGraph4, state.poseGraph5, state.poseGraph6, state.poseGraph7, state.poseGraph8 }, new CharacterPresentationPoseSourceSlot[] { state.sourceSlot, state.sourceSlot1, state.sourceSlot2, state.sourceSlot3, state.sourceSlot4, state.sourceSlot5, state.sourceSlot6 }, new CharacterPoseResourceSlot[] { state.resourceSlot, state.resourceSlot1, state.resourceSlot2, state.resourceSlot3, state.resourceSlot4 }, new Int32[] { 0, 1, 2, 3, 4, 5, 6 }, new Int32[] { 0, 1, 2, 3, 4 }, new CharacterPoseStateMachineLayout[] { new CharacterPoseStateMachineLayout(new PoseStateMachineId("corin.locomotion"), new CharacterPoseStateMachineLayoutElement[] { new CharacterPoseStateMachineLayoutElement("12a31544976ddf152639d62c6d19142c", new Vector2(534f, 111.999977f)), new CharacterPoseStateMachineLayoutElement("89eeedab7b97adb04b02ef9abd06f8bc", new Vector2(16.6417236f, 542.0981f)), new CharacterPoseStateMachineLayoutElement("corin.locomotion.entry", new Vector2(-354f, 224f)), new CharacterPoseStateMachineLayoutElement("corin.locomotion.locomotion", new Vector2(726.705139f, 467.670349f)), new CharacterPoseStateMachineLayoutElement("corin.locomotion.start", new Vector2(23.3333282f, 670f)), new CharacterPoseStateMachineLayoutElement("corin.locomotion.stop", new Vector2(397.3333f, 822.000061f)), new CharacterPoseStateMachineLayoutElement("corin.locomotion.turn", new Vector2(420.27533f, 587.212952f)) }) });
        }

        sealed class GenerationState
        {
            internal CharacterPresentationPoseGraphAsset poseAsset;
            internal CharacterClipPoseSourceSlot sourceSlot;
            internal CharacterClipPoseSourceSlot sourceSlot1;
            internal CharacterClipPoseSourceSlot sourceSlot2;
            internal CharacterClipPoseSourceSlot sourceSlot3;
            internal CharacterClipPoseSourceSlot sourceSlot4;
            internal CharacterClipPoseSourceSlot sourceSlot5;
            internal CharacterClipPoseSourceSlot sourceSlot6;
            internal CharacterPoseResourceSlot resourceSlot;
            internal CharacterPoseResourceSlot resourceSlot1;
            internal CharacterPoseResourceSlot resourceSlot2;
            internal CharacterPoseResourceSlot resourceSlot3;
            internal CharacterPoseResourceSlot resourceSlot4;
            internal CharacterPoseCanvasGraph poseGraph;
            internal CharacterPoseCanvasGraph poseGraph1;
            internal CharacterPoseCanvasGraph poseGraph2;
            internal CharacterPoseCanvasGraph poseGraph3;
            internal CharacterPoseCanvasGraph poseGraph4;
            internal CharacterPoseCanvasGraph poseGraph5;
            internal CharacterPoseCanvasGraph poseGraph6;
            internal CharacterPoseCanvasGraph poseGraph7;
            internal CharacterPoseCanvasGraph poseGraph8;
            internal CharacterPipelineDefinition asset;
            internal CharacterAnimationPresentationProfile asset1;
        }
    }
}
