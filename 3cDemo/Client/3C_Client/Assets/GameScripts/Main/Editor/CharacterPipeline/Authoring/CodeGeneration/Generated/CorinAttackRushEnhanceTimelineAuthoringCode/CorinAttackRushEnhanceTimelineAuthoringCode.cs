namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance", "Rush_Enhance", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Start_FootMotionTarget.anim", 38);
            return context.Complete(timeline);
        }
    }
}