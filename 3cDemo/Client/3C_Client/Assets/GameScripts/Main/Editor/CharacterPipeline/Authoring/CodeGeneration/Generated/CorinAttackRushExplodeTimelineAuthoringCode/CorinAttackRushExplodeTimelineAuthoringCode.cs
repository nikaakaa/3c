using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var open_RushAttackHandoff__14 = BuildOpen_RushAttackHandoff__14(rootParts, context);
            var corin_Attack_Rush_CamShake_E_02 = BuildCorin_Attack_Rush_CamShake_E_02(rootParts, context);
            FinalizeAuthoring(rootParts, open_RushAttackHandoff__14, corin_Attack_Rush_CamShake_E_02, context);
            var steeringMotion = CorinActionSteeringAuthoring.AddMotion(context, rootParts.timelineData, "Attack_Rush_Explode", "Assets/Configs/Character/Corin/Pipeline/Motion/RootMotion/CorinActionMotion/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode_FootMotionTarget.asset", 1.0500000000465661287307739258m);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, steeringMotion, CorinActionSteeringAuthoring.Id("CorinAttackRushExplodeTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
