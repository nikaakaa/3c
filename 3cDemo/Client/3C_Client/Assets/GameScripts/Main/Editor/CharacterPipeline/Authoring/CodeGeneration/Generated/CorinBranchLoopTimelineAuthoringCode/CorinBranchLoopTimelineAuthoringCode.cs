namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinBranchLoopTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context) =>
            CorinBranchTimelineAuthoring.Generate(context, "Loop", "Loop", 1.03333342m, true);
    }
}
