
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var dodgeBack = BuildDodgeBack(rootParts, context);
            FinalizeAuthoring(rootParts, dodgeBack, context);
            return context.Complete(rootParts.graph);
        }
    }
}
