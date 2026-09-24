
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var attack_Rush = BuildAttack_Rush(rootParts, context);
            var rushAttack = BuildRushAttack(rootParts, context);
            var attack_Rush_Explode = BuildAttack_Rush_Explode(rootParts, context);
            var attack_Rush_End = BuildAttack_Rush_End(rootParts, context);
            FinalizeAuthoring(rootParts, attack_Rush, rushAttack, attack_Rush_Explode, attack_Rush_End, context);
            return context.Complete(rootParts.graph);
        }
    }
}
