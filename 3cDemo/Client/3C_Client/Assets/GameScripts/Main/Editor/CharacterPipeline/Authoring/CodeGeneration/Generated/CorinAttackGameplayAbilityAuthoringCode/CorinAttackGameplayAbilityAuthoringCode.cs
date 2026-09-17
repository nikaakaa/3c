
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var attack1 = BuildAttack1(rootParts, context);
            var attack = BuildAttack(rootParts, context);
            var attack2 = BuildAttack2(rootParts, context);
            var attack3 = BuildAttack3(rootParts, context);
            var attack4 = BuildAttack4(rootParts, context);
            var attack5 = BuildAttack5(rootParts, context);
            FinalizeAuthoring(rootParts, attack1, attack, attack2, attack3, attack4, attack5, context);
            return context.Complete(rootParts.graph);
        }
    }
}
