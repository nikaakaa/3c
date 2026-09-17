
namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var dodgeForward = BuildDodgeForward(rootParts, context);
            FinalizeAuthoring(rootParts, dodgeForward, context);
            return context.Complete(rootParts.graph);
        }
    }
}
