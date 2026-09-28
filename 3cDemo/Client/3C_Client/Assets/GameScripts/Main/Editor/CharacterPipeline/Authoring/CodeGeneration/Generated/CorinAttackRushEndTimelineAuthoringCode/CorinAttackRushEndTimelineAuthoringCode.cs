using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            FinalizeAuthoring(rootParts, context);
            var steeringMotion = CorinActionSteeringAuthoring.AddMotion(context, rootParts.timelineData, "Attack_Rush_End", "Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_End_FootMotionTarget.asset", 1.333333333255723118782043457m);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, steeringMotion, CorinActionSteeringAuthoring.Id("CorinAttackRushEndTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
