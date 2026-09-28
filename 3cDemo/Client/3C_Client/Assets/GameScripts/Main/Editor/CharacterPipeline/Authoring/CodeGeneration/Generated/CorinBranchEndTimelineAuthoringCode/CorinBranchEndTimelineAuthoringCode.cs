using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            FinalizeAuthoring(rootParts, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "5f985d00-f5d7-2a40-cfa7-7b1b83c8e022", CorinActionSteeringAuthoring.Id("CorinBranchEndTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
