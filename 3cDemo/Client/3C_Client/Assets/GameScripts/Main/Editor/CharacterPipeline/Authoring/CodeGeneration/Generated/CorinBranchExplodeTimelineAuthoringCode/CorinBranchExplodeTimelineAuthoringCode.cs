using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var corin_Attack_Branch_02_CamShake_E_02 = BuildCorin_Attack_Branch_02_CamShake_E_02(rootParts, context);
            var corin_Attack_Branch_02_CamShake_E_03 = BuildCorin_Attack_Branch_02_CamShake_E_03(rootParts, context);
            FinalizeAuthoring(rootParts, corin_Attack_Branch_02_CamShake_E_02, corin_Attack_Branch_02_CamShake_E_03, context);
            CorinActionSteeringAuthoring.Apply(rootParts.timelineData, "f60ca9ea-2098-0da8-4c41-284431256408", CorinActionSteeringAuthoring.Id("CorinBranchExplodeTimeline:steering"), AnimationCurve.Constant(0f, 1f, 0f), AnimationCurve.Constant(0f, 1f, 0f));
            return context.Complete(rootParts.timeline);
        }
    }
}
