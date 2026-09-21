using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary> { new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_Loop", Condition = "Terminal", SourceFrame = 35 } };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Start.anim", 44, boundaries);
            return context.Complete(timeline);
        }
    }
}
