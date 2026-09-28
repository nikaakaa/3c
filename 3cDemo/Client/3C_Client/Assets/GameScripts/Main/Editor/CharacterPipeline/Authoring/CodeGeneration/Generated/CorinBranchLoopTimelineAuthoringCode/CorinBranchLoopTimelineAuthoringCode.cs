using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchLoopTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            FinalizeAuthoring(rootParts, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "911dcf11-c2f4-248e-c0d0-a3bf8551f70c", CorinActionSteeringAuthoring.Id("CorinBranchLoopTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(150.000015f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            return context.Complete(rootParts.timeline);
        }
    }
}
