
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var open_RushAttackHandoff__14 = BuildOpen_RushAttackHandoff__14(rootParts, context);
            FinalizeAuthoring(rootParts, open_RushAttackHandoff__14, context);
            return context.Complete(rootParts.timeline);
        }
    }
}
