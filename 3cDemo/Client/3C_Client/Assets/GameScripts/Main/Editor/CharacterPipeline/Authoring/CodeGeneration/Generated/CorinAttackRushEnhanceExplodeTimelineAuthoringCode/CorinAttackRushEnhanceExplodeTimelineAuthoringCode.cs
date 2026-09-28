using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackRushEnhanceExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var rushAttackHandoff = BuildRushAttackHandoff(rootParts, context);
            var rushMoveExit = BuildRushMoveExit(rootParts, context);
            var corin_Attack_Rush_CamShake_E_02 = BuildCorin_Attack_Rush_CamShake_E_02(rootParts, context);
            FinalizeAuthoring(rootParts, rushAttackHandoff, rushMoveExit, corin_Attack_Rush_CamShake_E_02, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "39f3c856-86e0-881a-15c6-eb398e905529", CorinActionSteeringAuthoring.Id("CorinAttackRushEnhanceExplodeTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
