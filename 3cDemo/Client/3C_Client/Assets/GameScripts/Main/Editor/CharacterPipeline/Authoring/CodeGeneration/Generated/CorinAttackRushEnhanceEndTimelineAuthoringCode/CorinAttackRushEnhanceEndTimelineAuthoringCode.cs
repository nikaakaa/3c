using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation.Fixed;
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
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "NoHold_Mode4_40", SourceFrame = 40 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Normal_04", Condition = "PressAttackA_Mode4_40", SourceFrame = 55 },
                new CorinRushTimelineAuthoringBuilder.Boundary { Target = "Attack_Rush_End", Condition = "Terminal", SourceFrame = 55 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush_Enhance_End", "Rush_Enhance", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode.anim", 70, boundaries);
            var noHold = timeline.Data.Tracks.SelectMany(track => track.Clips)
                .Single(clip => clip.AuthoringId == "04605ce1-0711-5112-bdb0-e4c943952a61");
            timeline.Data.ApplyModify(() => noHold.ConfigureTimeRange(
                FixedScalar.FromDecimal(0.38333332538604736328125m), noHold.EndTime), "设置松开攻击边界时间");
            return context.Complete(timeline);
        }
    }
}
