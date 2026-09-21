using BTSMTL.Timeline;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class SeparateTimelineTracksAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var root = context.ResolveExternalAsset<UnityEngine.Object>(context.OutputAssetPath, 0);
            var catalog = TimelineTreeContractComposition.Create();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(context.OutputAssetPath))
            {
                if (asset is not TimelineAsset timeline)
                    continue;
                timeline.Data.UpdateSerializedTimeline();
                timeline.Data.ApplyModify(() => timeline.Data.SeparateOverlappingTreeClips(catalog), "拆分重叠片段为独立轨道");
            }
            return context.Complete(root);
        }
    }
}
