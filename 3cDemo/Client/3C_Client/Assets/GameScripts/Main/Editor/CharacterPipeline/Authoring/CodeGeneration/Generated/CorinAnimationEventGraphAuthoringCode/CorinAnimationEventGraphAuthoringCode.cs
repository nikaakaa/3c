using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public sealed partial class CorinAnimationEventGraphAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            return context.Complete(rootParts.eventGraph);
        }
    }
}
