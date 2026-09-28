using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var open_RushRelease__13 = BuildOpen_RushRelease__13(rootParts, context);
            var corin_Attack_Rush_CamShake_E_01 = BuildCorin_Attack_Rush_CamShake_E_01(rootParts, context);
            FinalizeAuthoring(rootParts, open_RushRelease__13, corin_Attack_Rush_CamShake_E_01, context);
            var steeringMotion = CorinActionSteeringAuthoring.AddMotion(context, rootParts.timelineData, "Attack_Rush", "Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_FootMotionTarget.asset", 1.166666666744276881217956543m);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, steeringMotion, CorinActionSteeringAuthoring.Id("CorinAttackRushTimeline:steering"), new AnimationCurve(new Keyframe(0f, 60f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(4f, 60f, 0f, -7.84574175f, 0.333333343f, 0.13218151f) { weightedMode = (WeightedMode)3 }, new Keyframe(18f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever }, new AnimationCurve(new Keyframe(0f, 999f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(1f, 999f, 0f, -99750.0938f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(1.00999999f, 1.5f, -99750.0938f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(70.0000076f, 1.5f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            var steeringCatalog = BTSMTL.Timeline.TimelineTreeContractComposition.Create();
            var sawTrack = BtsmtlSkillAuthoringCode.EnsureTrack(rootParts.timelineData, steeringCatalog, typeof(BTSMTL.Timeline.TreeTrack), CorinActionSteeringAuthoring.Id("corin.rush.saw-window-track"), "SawExplode Window", BTSMTL.Timeline.TimelineExecutionDomain.Logic);
            CorinRushTimelineAuthoringBuilder.AddWindow(rootParts.timeline, steeringCatalog, sawTrack, "Attack_Rush", new CorinRushTimelineAuthoringBuilder.Window("RushSawExplode", 23, 70, 11003UL));
            return context.Complete(rootParts.timeline);
        }
    }
}
