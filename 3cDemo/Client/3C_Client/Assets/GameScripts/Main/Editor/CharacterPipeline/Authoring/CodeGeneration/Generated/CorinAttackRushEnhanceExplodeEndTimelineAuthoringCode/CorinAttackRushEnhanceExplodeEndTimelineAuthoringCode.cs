namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceExplodeEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Explode_End", "Rush_Enhance", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_End_FootMotionTarget.anim", 118, new CorinRushTimelineAuthoringBuilder.Window("RushMoveExit", 16, 118, 8103UL));
            return context.Complete(timeline);
        }
    }
}