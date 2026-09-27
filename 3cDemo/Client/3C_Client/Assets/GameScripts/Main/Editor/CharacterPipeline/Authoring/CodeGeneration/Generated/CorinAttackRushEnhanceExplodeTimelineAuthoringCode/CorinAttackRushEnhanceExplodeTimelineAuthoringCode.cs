namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Explode", "Rush_Enhance", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Explode_FootMotionTarget.anim", 63, new CorinRushTimelineAuthoringBuilder.Window("RushAttackHandoff", 16, 44, 8102UL), new CorinRushTimelineAuthoringBuilder.Window("RushMoveExit", 24, 63, 8103UL));
            return context.Complete(timeline);
        }
    }
}