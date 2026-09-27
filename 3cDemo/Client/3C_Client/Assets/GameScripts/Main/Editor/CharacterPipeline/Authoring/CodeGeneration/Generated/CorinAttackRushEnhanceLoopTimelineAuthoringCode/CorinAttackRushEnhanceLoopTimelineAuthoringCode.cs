namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceLoopTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Loop", "Rush_Enhance", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Loop_FootMotionTarget.anim", 80);
            return context.Complete(timeline);
        }
    }
}