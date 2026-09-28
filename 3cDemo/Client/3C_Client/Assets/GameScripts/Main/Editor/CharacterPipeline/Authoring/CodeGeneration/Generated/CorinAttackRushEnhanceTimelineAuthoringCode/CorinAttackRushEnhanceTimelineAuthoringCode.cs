using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var corin_Attack_Rush_Enhance_CamShake_E_01 = BuildCorin_Attack_Rush_Enhance_CamShake_E_01(rootParts, context);
            FinalizeAuthoring(rootParts, corin_Attack_Rush_Enhance_CamShake_E_01, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "b59713fa-52bb-e615-cf2e-42422136de74", CorinActionSteeringAuthoring.Id("CorinAttackRushEnhanceTimeline:steering"), new AnimationCurve(new Keyframe(0f, 60f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(4f, 60f, 0f, -7.84574175f, 0.333333343f, 0.13218151f) { weightedMode = (WeightedMode)3 }, new Keyframe(18f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }, new AnimationCurve(new Keyframe(0f, 999f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(1f, 999f, 0f, -99800.0938f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(1.00999999f, 1f, -99800.0938f, 0.00724742655f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70.0000076f, 1.5f, 0.00724742655f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            return context.Complete(rootParts.timeline);
        }
    }
}
