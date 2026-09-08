using System;
using System.Linq;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class PoseCanvasEditorSessionVerification
    {
        const string AssetPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset";

        [MenuItem("Tools/3C/Pose Canvas/Verify Editor Write Session")]
        public static void Run()
        {
            var sb = new System.Text.StringBuilder();
            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(AssetPath);
            CharacterPoseCanvasGraph graph = asset.Graph;
            int undoGroupBefore = Undo.GetCurrentGroup();
            var session = new CharacterPoseCanvasEditorWriteSession(asset, graph);
            graph.EditorWriteRouter = session;

            int nodeCountBefore = graph.Nodes.Count;
            int edgeCountBefore = graph.Connections.Count;

            try
            {
                bool guardOk = false;
                try { graph.AddNode(typeof(CharacterPoseCanvasNode), Vector2.zero); }
                catch (InvalidOperationException) { guardOk = true; }
                sb.Append("guard=").Append(guardOk ? "OK" : "FAIL").AppendLine();

                CharacterPoseNodeDefinition clipDefinition =
                    CharacterPoseNodeDefinitionModule.Shared.All.First(
                        value => value.Kind == CharacterPoseNodeKind.ClipPlayer);
                Node created = session.CreateNodeFromCapability(
                    clipDefinition.Capability.CapabilityId.Value,
                    new Vector2(-600f, 300f),
                    null,
                    -1);
                sb.Append("create=").Append(graph.Nodes.Count == nodeCountBefore + 1 ? "OK " + ((CharacterPoseCanvasNode)created).NodeId : "FAIL")
                    .AppendLine();

                CharacterPoseCanvasNode outputNode = graph.Nodes.First(
                    value => value.Kind == CharacterPoseNodeKind.OutputPose);
                Connection connected = graph.ConnectNodes(created, outputNode, -1, -1);
                sb.Append("connect=").Append(connected != null && graph.Connections.Count == edgeCountBefore + 1
                    ? "OK " + ((CharacterPoseCanvasConnection)connected).EdgeId
                    : "FAIL").AppendLine();

                graph.RemoveConnection(connected);
                sb.Append("disconnect=").Append(graph.Connections.Count == edgeCountBefore ? "OK" : "FAIL").AppendLine();

                session.RemoveNode(created);
                sb.Append("delete=").Append(graph.Nodes.Count == nodeCountBefore ? "OK" : "FAIL").AppendLine();

                while (Undo.GetCurrentGroup() > undoGroupBefore)
                    Undo.PerformUndo();
                Undo.PerformUndo();
                sb.Append("undo=").Append(graph.Nodes.Count == nodeCountBefore && graph.Connections.Count == edgeCountBefore
                    ? "OK"
                    : $"NODES {graph.Nodes.Count}/{nodeCountBefore} EDGES {graph.Connections.Count}/{edgeCountBefore}")
                    .AppendLine();
            }
            finally
            {
                graph.EditorWriteRouter = null;
                System.IO.File.WriteAllText("Temp/pose-editor-session-verification.txt", sb.ToString());
                Debug.Log("Pose editor session verification written to Temp/pose-editor-session-verification.txt");
            }
        }
    }
}
