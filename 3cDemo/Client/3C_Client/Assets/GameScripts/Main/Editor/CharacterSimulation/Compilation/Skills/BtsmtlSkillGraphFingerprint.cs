using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FlowCanvas;
using NodeCanvas.Framework.Internal;
using ParadoxNotion.Serialization;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillGraphFingerprint
    {
        readonly Dictionary<FlowGraph, string> m_Hashes = new();
        readonly HashSet<FlowGraph> m_Active = new();

        public string Compute(FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph)
                throw new ArgumentException("版本计算只接受正式技能图。", nameof(graph));
            if (m_Hashes.TryGetValue(graph, out string existing))
                return existing;
            if (!m_Active.Add(graph))
                throw new InvalidOperationException("技能资产引用形成循环，无法计算版本。");
            try
            {
                var snapshot = new GraphSource
                {
                    nodes = graph.allNodes.OrderBy(node => node.UID, StringComparer.Ordinal).ToList(),
                    canvasGroups = graph.canvasGroups.ToList(),
                    localBlackboard = graph.GetGraphSource().localBlackboard
                };
                snapshot.SetMetaData(graph.GetGraphSource());
                var references = new List<UnityEngine.Object>();
                string json = JSONSerializer.Serialize(typeof(GraphSource), snapshot.Pack(graph), references);
                var text = new StringBuilder(json);
                foreach (UnityEngine.Object reference in references)
                {
                    if (reference == null)
                    {
                        text.Append("\nnull");
                        continue;
                    }
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference, out string guid, out long fileId))
                        throw new InvalidOperationException($"技能引用'{reference.name}'不是正式资产。");
                    text.Append('\n').Append(guid).Append(':').Append(fileId.ToString(CultureInfo.InvariantCulture));
                    if (reference is FlowGraph child)
                        text.Append(':').Append(Compute(child));
                    else
                        text.Append(':').Append(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference)));
                }
                using SHA256 sha = SHA256.Create();
                string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
                m_Hashes.Add(graph, hash);
                return hash;
            }
            finally
            {
                m_Active.Remove(graph);
            }
        }
    }
}
