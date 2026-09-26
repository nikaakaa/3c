using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "NoHold_Mode4_40", Frame = 40 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "PressAttackA_Mode4_40", Frame = 55 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_End", Condition = "Terminal", Frame = 55 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_End", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode.anim", 70, boundaries);
            return context.Complete(timeline);
        }
    }
}
