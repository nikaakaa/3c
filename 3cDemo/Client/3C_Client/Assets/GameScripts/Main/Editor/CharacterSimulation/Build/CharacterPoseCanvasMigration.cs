using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterPoseCanvasMigration
    {
        const string CorinPoseGraphPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset";

        public static void MigrateCorin()
        {
            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                    CorinPoseGraphPath);
            if (!asset)
                throw new InvalidOperationException(
                    $"Corin Pose Graph asset '{CorinPoseGraphPath}' is missing.");

            CharacterPoseCanvasGraph[] graphs = asset.CreateLegacyCanvasGraphs();
            if (graphs.Length == 0)
                throw new InvalidOperationException(
                    "Corin Pose Graph migration produced no graphs.");
            var graphIds = new HashSet<PoseGraphId>();
            try
            {
                foreach (CharacterPoseCanvasGraph graph in graphs)
                {
                    if (!graphIds.Add(graph.GraphId))
                        throw new InvalidOperationException(
                            $"Corin Pose Graph migration contains duplicate Graph identity '{graph.GraphId}'.");
                    CharacterPoseCanvasMutationPreflight.RequireValid(graph);
                    graph.RequireValid();
                }

                asset.SetMigratedCanvasGraphs(
                    graphs[0],
                    graphs.Skip(1).ToArray());
                foreach (CharacterPoseCanvasGraph graph in graphs)
                    EditorUtility.SetDirty(graph);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                CharacterPresentationPoseGraphAsset reloaded =
                    AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(
                        CorinPoseGraphPath);
                if (!reloaded || !reloaded.Graph ||
                    reloaded.EnumerateGraphs().Count() != graphs.Length)
                    throw new InvalidOperationException(
                        "Corin Pose Graph migration did not round-trip the Canvas Graph catalog.");
                foreach (CharacterPoseCanvasGraph graph in reloaded.EnumerateGraphs())
                    CharacterPoseCanvasMutationPreflight.RequireValid(graph);
                UnityEngine.Debug.Log(
                    $"Corin Pose Graph migrated to Canvas Graph catalog: {graphs.Length} graphs.");
            }
            catch
            {
                foreach (CharacterPoseCanvasGraph graph in graphs)
                    if (graph && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph)))
                        UnityEngine.Object.DestroyImmediate(graph);
                throw;
            }
        }

    }
}
