using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchStartTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var branch_Release_Window = BuildBranch_Release_Window(rootParts, context);
            var corin_Attack_Branch_02_CamShake_E_01 = BuildCorin_Attack_Branch_02_CamShake_E_01(rootParts, context);
            FinalizeAuthoring(rootParts, branch_Release_Window, corin_Attack_Branch_02_CamShake_E_01, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "fdce0b22-e9ee-804e-4071-e9ea13042ca1", CorinActionSteeringAuthoring.Id("CorinBranchStartTimeline:steering"), new AnimationCurve(new Keyframe(0f, 60f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(18f, 60f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(18.0100002f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(30f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(30.0100002f, 12f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(54f, 12f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(54.0099983f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(100f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }, new AnimationCurve(new Keyframe(0f, 999f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1f, 999f, 0f, -99900.0991f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1.00999999f, 0f, -99900.0991f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(100.000008f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            return context.Complete(rootParts.timeline);
        }
    }
}
