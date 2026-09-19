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
            var cues = new List<CorinRushTimelineAuthoringBuilder.Cue>();
            for (int frame = 2; frame <= 30; frame += 2)
                cues.Add(new CorinRushTimelineAuthoringBuilder.Cue { CueId = frame % 4 == 2 ? "Corin_Attack_Rush_Enhance_AttackProperty_01_01" : "Corin_Attack_Rush_Enhance_AttackProperty_01_02", Frame = frame });
            for (int frame = 32; frame <= 86; frame += 2)
                cues.Add(new CorinRushTimelineAuthoringBuilder.Cue { CueId = "Corin_Attack_Rush_Enhance_AttackProperty_01_02", Frame = frame });
            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_Explode", Condition = "Trigger_SawExplode", Frame = 43 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_Loop", Condition = "Reenter", Frame = 81 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_Enhance_End", Condition = "HoldFalse_ClickFalse", Frame = 86 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_Loop", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Enhance_Loop.anim", 86, cues, boundaries);
            return context.Complete(timeline);
        }
    }
}
