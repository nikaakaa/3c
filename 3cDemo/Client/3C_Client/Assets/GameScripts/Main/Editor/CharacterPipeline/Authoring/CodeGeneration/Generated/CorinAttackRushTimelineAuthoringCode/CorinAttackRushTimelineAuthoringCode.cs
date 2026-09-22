using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class CorinAttackRushTimelineAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {            var boundaries = new List<CorinRushTimelineAuthoringBuilder.Boundary>
            {
                new CorinRushTimelineAuthoringBuilder.Boundary { WindowType = "RushRelease", WindowId = "RushReleaseOpen", Digest = 8101UL, SourceFrame = 13 }
            };
            var timeline = CorinRushTimelineAuthoringBuilder.Build(context, "Attack_Rush", "Rush", "Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Attack_Rush.anim", 70, boundaries);
            return context.Complete(timeline);
        }
    }
}
