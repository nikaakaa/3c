
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var branchAttack = BuildBranchAttack(rootParts, context);
            var explode = BuildExplode(rootParts, context);
            var loop = BuildLoop(rootParts, context);
            var end = BuildEnd(rootParts, context);
            var walk = BuildWalk(rootParts, context);
            FinalizeAuthoring(rootParts, branchAttack, explode, loop, end, walk, context);
            return context.Complete(rootParts.graph);
        }
    }
}
