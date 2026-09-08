using System;
using System.Linq;
using NodeCanvas.Editor;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class PoseCanvasGraphEditorEntryPoint
    {
        static CharacterPoseCanvasGraph s_Graph;
        static CharacterPoseCanvasEditorWriteSession s_Session;

        [InitializeOnLoadMethod]
        static void Register()
        {
            GraphEditor.onCurrentGraphChanged -= BindGraph;
            GraphEditor.onCurrentGraphChanged += BindGraph;
            GraphEditor.onEditorClosed -= Detach;
            GraphEditor.onEditorClosed += Detach;
            AssemblyReloadEvents.beforeAssemblyReload -= Detach;
            AssemblyReloadEvents.beforeAssemblyReload += Detach;
            EditorApplication.delayCall += Restore;
        }

        static void Restore()
        {
            if (GraphEditor.current != null)
                BindGraph(GraphEditor.currentGraph);
        }

        static void BindGraph(NodeCanvas.Framework.Graph value)
        {
            Detach();
            if (value is not CharacterPoseCanvasGraph graph)
            {
                CharacterPoseGraphWorkspace.HandleGraphSelection(value);
                return;
            }
            string path = AssetDatabase.GetAssetPath(graph);
            CharacterPresentationPoseGraphAsset asset = AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(path);
            if (asset == null || !asset.TryGetGraph(graph.GraphId, out CharacterPoseCanvasGraph owned) || owned != graph)
                throw new InvalidOperationException("Pose Canvas requires its exact persisted authoring owner.");
            s_Graph = graph;
            s_Session = new CharacterPoseCanvasEditorWriteSession(asset, graph);
            graph.EditorWriteRouter = s_Session;
            PoseCanvasEditorBridge.VisualsRefresh?.Invoke(graph);
            CharacterPoseGraphWorkspace.HandleGraphSelection(graph);
        }

        static void Detach()
        {
            if (s_Graph != null && s_Graph.EditorWriteRouter == s_Session)
                s_Graph.EditorWriteRouter = null;
            s_Graph = null;
            s_Session = null;
            CharacterPoseCanvasPortsGUI.ResetDrag();
        }

        [UnityEditor.Callbacks.OnOpenAsset(0)]
        static bool OpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not CharacterPresentationPoseGraphAsset asset)
                return false;
            Open(asset, asset.Graph);
            return true;
        }
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
            const string definitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
            CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(definitionPath);
            if (definition == null || definition.AnimationPresentationProfile == null || definition.AnimationPresentationProfile.PoseGraph != asset)
                throw new InvalidOperationException("Corin Definition does not own this Pose Graph.");
            CharacterPoseGraphWorkspace.Open(asset, definition.AnimationPresentationProfile, definition.PresentationProjection, definition);
        }

        internal static void Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseCanvasGraph graph)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (!asset.TryGetGraph(graph.GraphId, out CharacterPoseCanvasGraph owned) || owned != graph)
                throw new InvalidOperationException("Pose Canvas graph does not belong to the selected authoring asset.");
            CharacterPoseGraphWorkspace workspace = CharacterPoseGraphWorkspace.OpenAuthoring(asset);
            workspace.FocusGraph(graph.GraphId);
        }
    }
}
