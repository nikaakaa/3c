
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var attack = BuildAttack(rootParts, context);
            var attack1 = BuildAttack1(attack, rootParts, context);
            var attack4 = BuildAttack4(attack, rootParts, context);
            var attack2 = BuildAttack2(attack, rootParts, context);
            var attack3 = BuildAttack3(attack, rootParts, context);
            var attack5 = BuildAttack5(attack, rootParts, context);
            var attack5_End2 = BuildAttack5_End2(attack, rootParts, context);
            var attack5_End = BuildAttack5_End(attack, rootParts, context);
            FinalizeAuthoring(rootParts, attack, attack1, attack4, attack2, attack3, attack5, attack5_End2, attack5_End, context);
            return context.Complete(rootParts.graph);
        }
    }
}
