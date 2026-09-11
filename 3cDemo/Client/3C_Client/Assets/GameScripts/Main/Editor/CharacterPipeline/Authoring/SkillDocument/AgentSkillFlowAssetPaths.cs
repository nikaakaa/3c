using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    internal static class AgentSkillFlowAssetPaths
    {
        public static string Root(CharacterPipelineDefinition definition, string localId)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(definitionPath) || !localId.StartsWith("local:", StringComparison.Ordinal))
                throw new InvalidOperationException("新技能根需要持久化Definition与local identity。");
            using var hash = SHA256.Create();
            string identity = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(localId)))
                .Replace("-", string.Empty).ToLowerInvariant();
            return Path.GetDirectoryName(definitionPath).Replace('\\', '/') + "/" +
                Path.GetFileNameWithoutExtension(definitionPath) + ".Skill." + identity + ".asset";
        }

        public static IReadOnlyList<string> PlannedRoots(
            CharacterPipelineDefinition definition,
            AgentPackageSkillFlowDocument document)
        {
            var paths = new List<string>();
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs)
            {
                if (graph.ownership != AgentGraphOwnership.RootAsset.ToString() ||
                    !graph.id.StartsWith("local:", StringComparison.Ordinal))
                    continue;
                string path = Root(definition, graph.id);
                RequireAvailable(path);
                paths.Add(path);
            }
            return paths;
        }

        public static void RequireAvailable(string path)
        {
            if (File.Exists(path) || File.Exists(path + ".meta") ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                throw new InvalidOperationException($"技能根目标资产已存在，不能覆盖：{path}");
        }
    }
}
