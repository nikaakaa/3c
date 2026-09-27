namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_End", "Rush_Enhance", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode_FootMotionTarget.anim", 70, new CorinRushTimelineAuthoringBuilder.Window("RushAttackHandoff", 0, 40, 8102UL), new CorinRushTimelineAuthoringBuilder.Window("RushMoveExit", 12, 70, 8103UL));
            return context.Complete(timeline);
        }
    }
}