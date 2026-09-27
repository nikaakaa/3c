namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinBranchWalkTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context) =>
            CorinBranchTimelineAuthoring.Generate(context, "Walk", "Walk", 1.08333337m, true);
    }
}
