using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>();
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_End", "Rush", "Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/Generated/InPlaceTargets/Avatar_Female_Size01_Corin_Ani_Attack_Rush_End_FootMotionTarget.anim", 80, boundaries);
            return context.Complete(timeline);
        }
    }
}
