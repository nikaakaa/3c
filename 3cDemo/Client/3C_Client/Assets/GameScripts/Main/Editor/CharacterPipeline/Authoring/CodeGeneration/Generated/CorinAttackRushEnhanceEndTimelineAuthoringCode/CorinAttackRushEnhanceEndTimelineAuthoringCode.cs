using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var rushAttackHandoff = BuildRushAttackHandoff(rootParts, context);
            var rushMoveExit = BuildRushMoveExit(rootParts, context);
            FinalizeAuthoring(rootParts, rushAttackHandoff, rushMoveExit, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "50de4831-18d8-a8e0-acaa-8b563d4f8b68", CorinActionSteeringAuthoring.Id("CorinAttackRushEnhanceEndTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
