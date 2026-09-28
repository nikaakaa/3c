
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var attack_Rush = BuildAttack_Rush(rootParts, context);
            var attack_Rush_Enhance = BuildAttack_Rush_Enhance(rootParts, context);
            var rushAttack = BuildRushAttack(rootParts, context);
            var attack_Rush_Explode = BuildAttack_Rush_Explode(rootParts, context);
            var attack_Rush_End = BuildAttack_Rush_End(rootParts, context);
            var attack_Rush_Enhance_Loop = BuildAttack_Rush_Enhance_Loop(rootParts, context);
            var attack_Rush_Enhance_Explode = BuildAttack_Rush_Enhance_Explode(rootParts, context);
            var attack_Rush_Enhance_End = BuildAttack_Rush_Enhance_End(rootParts, context);
            var attack_Rush_Enhance_Explode_End = BuildAttack_Rush_Enhance_Explode_End(rootParts, context);
            FinalizeAuthoring(rootParts, attack_Rush, attack_Rush_Enhance, rushAttack, attack_Rush_Explode, attack_Rush_End, attack_Rush_Enhance_Loop, attack_Rush_Enhance_Explode, attack_Rush_Enhance_End, attack_Rush_Enhance_Explode_End, context);
            return context.Complete(rootParts.graph);
        }
    }
}
