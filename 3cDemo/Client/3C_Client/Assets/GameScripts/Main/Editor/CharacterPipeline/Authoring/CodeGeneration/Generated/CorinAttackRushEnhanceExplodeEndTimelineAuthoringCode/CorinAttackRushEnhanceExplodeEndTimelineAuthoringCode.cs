using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushEnhanceExplodeEndTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary> { new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Host_Route", Condition = "Terminal", Frame = 118 } };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Explode_End", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_End.anim", 118, Array.Empty<CorinRushTimelineAuthoringBuilder.Cue>(), boundaries);
            return context.Complete(timeline);
        }
    }
}
