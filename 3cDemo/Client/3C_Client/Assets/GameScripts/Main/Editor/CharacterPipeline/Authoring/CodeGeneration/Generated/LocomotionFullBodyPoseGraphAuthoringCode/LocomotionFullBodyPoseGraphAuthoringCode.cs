using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;

namespace ThirdPersonCharacter.Generated
{
    public sealed partial class LocomotionFullBodyPoseGraphAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var slots = BuildSlots(context);
            var graphs = BuildGraphs(context);
            FinalizeAuthoring(rootParts, slots, graphs, context);
            return context.Complete(rootParts.poseAsset);
        }
    }
}
