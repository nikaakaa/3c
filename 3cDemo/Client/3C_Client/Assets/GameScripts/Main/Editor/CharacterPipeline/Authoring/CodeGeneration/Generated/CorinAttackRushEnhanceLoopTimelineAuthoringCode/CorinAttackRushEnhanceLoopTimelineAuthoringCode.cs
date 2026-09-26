using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceLoopTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_Explode", Condition = "Trigger_SawExplode", Frame = 43 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_Loop", Condition = "Reenter", Frame = 81 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_End", Condition = "HoldFalse_ClickFalse", Frame = 86 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Loop", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Loop.anim", 86, boundaries);
            return context.Complete(timeline);
        }
    }
}
