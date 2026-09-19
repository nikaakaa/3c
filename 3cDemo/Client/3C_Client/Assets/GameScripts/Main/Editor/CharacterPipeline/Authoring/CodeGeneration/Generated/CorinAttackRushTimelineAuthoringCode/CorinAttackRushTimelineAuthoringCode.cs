using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var cues = new List<CorinRushTimelineAuthoringBuilder.Cue> { new CorinRushTimelineAuthoringBuilder.Cue { CueId = "Corin_Attack_Rush_AttackProperty_01_01", Frame = 8 } };
            for (int frame = 10; frame <= 70; frame += 2)
                cues.Add(new CorinRushTimelineAuthoringBuilder.Cue { CueId = "Corin_Attack_Rush_AttackProperty_01_02", Frame = frame });
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Explode", Condition = "HoldFalse_ClickFalse", Frame = 13 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Explode", Condition = "Trigger_SawExplode", Frame = 23 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Explode", Condition = "Terminal", Frame = 70 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush", "Rush", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush.anim", 70, cues, boundaries);
            return context.Complete(timeline);
        }
    }
}
