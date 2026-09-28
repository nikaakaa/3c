using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchWalkTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            FinalizeAuthoring(rootParts, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "9b5184d4-2515-4e4b-7cad-dd09418c89e3", CorinActionSteeringAuthoring.Id("CorinBranchWalkTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(64f, 0.5f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            return context.Complete(rootParts.timeline);
        }
    }
}
