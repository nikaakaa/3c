using System;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public static class EventGraphAuthoringCode
    {
        public static T EnsureRoot<T>(
            BtsmtlAuthoringGenerationContext context,
            string identity,
            string contentRevision,
            string name)
            where T : HostEventGraph
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("Event graph identity is required.", nameof(identity));
            if (string.IsNullOrWhiteSpace(contentRevision))
                throw new ArgumentException("Event graph content revision is required.", nameof(contentRevision));

            string outputPath = context.OutputAssetPath;
            T graph = AssetDatabase.LoadAssetAtPath<T>(outputPath);
            if (graph)
            {
                if (!string.Equals(graph.AuthoringId, identity, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Event graph output '{outputPath}' has a different authoring identity.");
                graph.ClearAuthoringContent();
            }
            else
            {
                if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null)
                    throw new InvalidOperationException(
                        $"Event graph output '{outputPath}' is occupied by another asset type.");
                EnsureOutputFolder(outputPath);
                graph = ScriptableObject.CreateInstance<T>();
                graph.name = string.IsNullOrWhiteSpace(name) ? identity : name.Trim();
                AssetDatabase.CreateAsset(graph, outputPath);
            }

            graph.ConfigureAuthoringIdentity(identity, contentRevision);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        static void EnsureOutputFolder(string outputPath)
        {
            int separator = outputPath.LastIndexOf('/');
            if (separator <= "Assets".Length)
                throw new InvalidOperationException(
                    $"Event graph output '{outputPath}' has no valid asset folder.");
            string folder = outputPath.Substring(0, separator);
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] segments = folder.Split('/');
            string current = segments[0];
            if (!string.Equals(current, "Assets", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Event graph output folder '{folder}' must be inside Assets.");
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
