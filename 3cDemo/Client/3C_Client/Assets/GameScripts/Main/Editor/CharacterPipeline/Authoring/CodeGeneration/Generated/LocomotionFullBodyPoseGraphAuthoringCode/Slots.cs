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
    public sealed partial class LocomotionFullBodyPoseGraphAuthoringCode
    {
        static void BuildCreateSlots0(GenerationState state, BtsmtlAuthoringGenerationContext context)
        {
            state.sourceSlot = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_Idle_Inplace Source Binding");
            state.sourceSlot1 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_RunStart_Inplace Source Binding");
            state.sourceSlot2 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_RunLoop_Inplace Source Binding");
            state.sourceSlot3 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_RunEnd_Inplace Source Binding");
            state.sourceSlot4 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_WalkStart_Inplace Source Binding");
            state.sourceSlot5 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_WalkLoop_Inplace Source Binding");
            state.sourceSlot6 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>("Corin_Pipeline_MovingTurn_Inplace Source Binding");
            state.resourceSlot = BtsmtlPoseAuthoringCode.CreateResourceSlot(CharacterPoseResourceKind.BlendPolicy, "CorinActionBlendPolicy BlendPolicy");
            state.resourceSlot1 = BtsmtlPoseAuthoringCode.CreateResourceSlot(CharacterPoseResourceKind.BlendProfile, "CorinLocomotionBlendProfile BlendProfile");
            state.resourceSlot2 = BtsmtlPoseAuthoringCode.CreateResourceSlot(CharacterPoseResourceKind.FootPlacementCalibration, "CorinFootPlacementRigCalibration FootPlacementCalibration");
            state.resourceSlot3 = BtsmtlPoseAuthoringCode.CreateResourceSlot(CharacterPoseResourceKind.FootPlacementProfile, "CorinFootPlacementProfile FootPlacementProfile");
            state.resourceSlot4 = BtsmtlPoseAuthoringCode.CreateResourceSlot(CharacterPoseResourceKind.InertializationPolicy, "CorinPoseInertializationPolicy InertializationPolicy");
        }
    }
}
