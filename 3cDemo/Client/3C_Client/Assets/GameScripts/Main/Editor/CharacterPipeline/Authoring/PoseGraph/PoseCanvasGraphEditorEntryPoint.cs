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
            DestroyTransientPoseGraphs();
            if (GraphEditor.current != null)
            {
                if (HasTransientPoseGraph(GraphEditor.rootGraph))
                {
                    GraphEditor.current.Close();
                    return;
                }
                BindGraph(GraphEditor.currentGraph);
            }
        }

        static void BindGraph(NodeCanvas.Framework.Graph value)
        {
            Detach();
            if (GraphEditor.current != null &&
                HasTransientPoseGraph(GraphEditor.rootGraph))
            {
                GraphEditor.current.Close();
                return;
            }
            if (value is CharacterPoseCanvasGraph temporary &&
                !EditorUtility.IsPersistent(temporary))
            {
                if (GraphEditor.current != null &&
                    GraphEditor.currentGraph == temporary)
                    GraphEditor.SetReferences((NodeCanvas.Framework.Graph)null);
                return;
            }
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

        static bool HasTransientPoseGraph(NodeCanvas.Framework.Graph root)
        {
            for (NodeCanvas.Framework.Graph current = root;
                 current != null;
                 current = current.GetCurrentChildGraph())
            {
                if (current is CharacterPoseCanvasGraph pose &&
                    !EditorUtility.IsPersistent(pose))
                    return true;
            }
            return false;
        }

        static void DestroyTransientPoseGraphs()
        {
            CharacterPoseCanvasGraph[] graphs =
                Resources.FindObjectsOfTypeAll<CharacterPoseCanvasGraph>();
            for (int i = 0; i < graphs.Length; i++)
            {
                CharacterPoseCanvasGraph graph = graphs[i];
                if (!graph || EditorUtility.IsPersistent(graph))
                    continue;
                if (graph.NativeRuntime != null)
                    continue;
                if (GraphEditor.currentGraph == graph)
                    GraphEditor.current.Close();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        static void Detach()
        {
            if (s_Graph != null && s_Graph.EditorWriteRouter == s_Session)
                s_Graph.EditorWriteRouter = null;
            s_Graph = null;
            s_Session = null;
            FlowCanvas.FlowNode.ClearPortInteraction();
        }

        [UnityEditor.Callbacks.OnOpenAsset(0)]
        static bool OpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not CharacterPresentationPoseGraphAsset asset)
                return false;
            Open(asset, asset.Graph);
            return true;
        }
        [MenuItem("Tools/3C/Pose Canvas/Open Selected Pose Graph")]
        public static void OpenSelectedPoseGraph()
        {
            CharacterPresentationPoseGraphAsset asset =
                Selection.activeObject as CharacterPresentationPoseGraphAsset;
            if (asset == null)
                throw new InvalidOperationException(
                    "Select a shared Presentation Pose Graph asset before opening the Pose Canvas.");
            if (asset.Graph == null)
                throw new InvalidOperationException(
                    $"Pose Graph asset '{AssetDatabase.GetAssetPath(asset)}' has no migrated authoring graph.");
            Open(asset, asset.Graph);
        }

        [MenuItem("Tools/3C/Pose Canvas/Open Selected Pose Graph", true)]
        static bool ValidateOpenSelectedPoseGraph() =>
            Selection.activeObject is CharacterPresentationPoseGraphAsset;

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
