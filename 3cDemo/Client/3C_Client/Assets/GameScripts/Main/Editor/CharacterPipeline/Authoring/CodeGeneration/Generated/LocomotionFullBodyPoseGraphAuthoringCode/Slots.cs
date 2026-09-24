using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Generated
{
    public sealed partial class LocomotionFullBodyPoseGraphAuthoringCode
    {
        static SlotsParts BuildSlots(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new SlotsParts();
            parts.sourceSlot = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_Idle_Inplace Source Binding");
            parts.sourceSlot1 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_RunStart_Inplace Source Binding");
            parts.sourceSlot2 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_RunLoop_Inplace Source Binding");
            parts.sourceSlot3 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_RunEnd_Inplace Source Binding");
            parts.sourceSlot4 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_WalkStart_Inplace Source Binding");
            parts.sourceSlot5 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_WalkLoop_Inplace Source Binding");
            parts.sourceSlot6 = BtsmtlPoseAuthoringCode.CreateSourceSlot<CharacterClipPoseSourceSlot>(context, "Corin_Pipeline_MovingTurn_Inplace Source Binding");
            parts.resourceSlot = BtsmtlPoseAuthoringCode.CreateResourceSlot(context, CharacterPoseResourceKind.BlendPolicy, "CorinActionBlendPolicy BlendPolicy");
            parts.resourceSlot1 = BtsmtlPoseAuthoringCode.CreateResourceSlot(context, CharacterPoseResourceKind.BlendProfile, "CorinLocomotionBlendProfile BlendProfile");
            parts.resourceSlot2 = BtsmtlPoseAuthoringCode.CreateResourceSlot(context, CharacterPoseResourceKind.FootPlacementCalibration, "CorinFootPlacementRigCalibration FootPlacementCalibration");
            parts.resourceSlot3 = BtsmtlPoseAuthoringCode.CreateResourceSlot(context, CharacterPoseResourceKind.FootPlacementProfile, "CorinFootPlacementProfile FootPlacementProfile");
            return parts;
        }

        sealed class SlotsParts
        {
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
        }
    }
}
