using System;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class PoseCanvasGraphEditorEntryPoint
    {
        const string CorinPoseGraphPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset";

        [MenuItem("Tools/3C/Pose Canvas/Open Corin in CanvasCore Graph Editor")]
        public static void OpenCorin()
        {
            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(CorinPoseGraphPath);
            if (asset == null || asset.Graph == null)
                throw new InvalidOperationException(
                    $"Corin Pose Graph asset '{CorinPoseGraphPath}' is missing or not migrated.");
            Open(asset, asset.Graph);
        }

        internal static void Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph graph)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            var session = new CharacterPoseCanvasEditorWriteSession(asset, graph);
            graph.EditorWriteRouter = session;
            GraphEditor editor = GraphEditor.OpenWindow(graph);
            EditorApplication.CallbackFunction detach = null;
            detach = () =>
            {
                if (editor != null)
                    return;
                if (graph.EditorWriteRouter == session)
                    graph.EditorWriteRouter = null;
                EditorApplication.update -= detach;
            };
            EditorApplication.update += detach;
        }
    }
}
