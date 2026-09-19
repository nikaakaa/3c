using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushExplodeTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var cues = new List<CorinRushTimelineAuthoringBuilder.Cue> { new CorinRushTimelineAuthoringBuilder.Cue { CueId = "Corin_Attack_Rush_AttackProperty_02", Frame = 1 } };
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "NoHold_Mode4_44", Frame = 14 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "PressAttackA_Mode4_44", Frame = 14 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_End", Condition = "Terminal", Frame = 70 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Explode", "Rush", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode.anim", 70, cues, boundaries);
            return context.Complete(timeline);
        }
    }
}
