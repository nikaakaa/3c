
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var open_RushRelease__13 = BuildOpen_RushRelease__13(rootParts, context);
            FinalizeAuthoring(rootParts, open_RushRelease__13, context);
            return context.Complete(rootParts.timeline);
        }
    }
}
