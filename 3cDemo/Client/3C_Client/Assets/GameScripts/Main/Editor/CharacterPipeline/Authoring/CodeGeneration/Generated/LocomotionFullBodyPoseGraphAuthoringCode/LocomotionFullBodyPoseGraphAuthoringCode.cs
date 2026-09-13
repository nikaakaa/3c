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
            var generation = new GenerationState();
            LoadSlotsResources(generation, context);
            BuildCreateSlots0(generation, context);
            BuildRootBindingSlots0(generation, context);
            return context.Complete(generation.poseAsset);
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
