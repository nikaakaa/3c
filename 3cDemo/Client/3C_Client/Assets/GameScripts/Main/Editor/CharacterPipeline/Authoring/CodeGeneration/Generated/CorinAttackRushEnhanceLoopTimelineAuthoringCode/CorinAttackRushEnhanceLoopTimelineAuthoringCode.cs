using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceLoopTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var corin_Attack_Rush_Enhance_CamShake_E_01 = BuildCorin_Attack_Rush_Enhance_CamShake_E_01(rootParts, context);
            FinalizeAuthoring(rootParts, corin_Attack_Rush_Enhance_CamShake_E_01, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "be9bc72e-d950-88e2-400e-156b6017c7f3", CorinActionSteeringAuthoring.Id("CorinAttackRushEnhanceLoopTimeline:steering"), new AnimationCurve(new Keyframe(0f, 60f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(4f, 60f, 0f, -7.84574175f, 0.333333343f, 0.13218151f) { weightedMode = (WeightedMode)3 }, new Keyframe(18f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }, new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70.0000076f, 1f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            return context.Complete(rootParts.timeline);
        }
    }
}
