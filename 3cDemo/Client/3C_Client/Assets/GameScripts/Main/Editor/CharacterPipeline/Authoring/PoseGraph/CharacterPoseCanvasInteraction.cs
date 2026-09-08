using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NodeCanvas.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseCanvasInteraction
    {
        static readonly ConditionalWeakTable<NodeCanvas.Framework.Graph, HashSet<string>> s_ActiveNodes = new();
        internal static void SetActiveNodes(NodeCanvas.Framework.Graph graph, ISet<string> ids)
        {
            HashSet<string> active = s_ActiveNodes.GetValue(graph, _ => new HashSet<string>(StringComparer.Ordinal));
            active.Clear();
            active.UnionWith(ids);
        }
        internal static bool IsActive(NodeCanvas.Framework.Graph graph, string id) =>
            s_ActiveNodes.TryGetValue(graph, out var ids) && ids.Contains(id);
        internal static void Apply(Action action)
        {
            try { action(); }
            catch (InvalidOperationException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
            catch (ArgumentException error) { GraphEditor.current?.ShowNotification(new GUIContent(error.Message)); }
        }
    }
}
