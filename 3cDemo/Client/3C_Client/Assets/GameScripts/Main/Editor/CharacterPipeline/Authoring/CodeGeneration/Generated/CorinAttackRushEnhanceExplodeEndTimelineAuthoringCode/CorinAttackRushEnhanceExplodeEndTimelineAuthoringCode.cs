using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var rushMoveExit = BuildRushMoveExit(rootParts, context);
            FinalizeAuthoring(rootParts, rushMoveExit, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "1ea108be-0bb5-461f-7a4e-a82624912322", CorinActionSteeringAuthoring.Id("CorinAttackRushEnhanceExplodeEndTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
