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
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary> { new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Host_Route", Condition = "Terminal", Frame = 80 } };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_End", "Rush", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_End.anim", 80, Array.Empty<CorinRushTimelineAuthoringBuilder.Cue>(), boundaries);
            return context.Complete(timeline);
        }
    }
}
