using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillFlowDocumentOwnerCollector
    {
        public static bool TryCollect(
            CharacterPipelineDefinition definition,
            AgentPackageSkillFlowDocument document,
            ISet<UnityEngine.Object> owners,
            AgentCompileReport report)
        {
            if (!definition || document == null || owners == null)
            {
                report.Error("transaction.skill-flow", "skill_transaction_owner_invalid", "Skill Flow事务缺少Definition、Document或owner集合。");
                return false;
            }
            bool valid = TryAdd(definition, owners, report, "definition");
            var current = new BtsmtlSkillGraphClosureIndex();
            try
            {
                current.Build(definition);
            }
            catch (Exception exception)
            {
                report.Error("transaction.skill-flow", "skill_transaction_index_invalid", exception.Message);
                return false;
            }
            foreach (FlowGraph graph in current.Graphs.Values.Distinct())
                valid &= TryAdd(graph, owners, report, "graph");
            foreach (TimelineAsset timeline in current.Timelines.Values.Distinct())
                valid &= TryAdd(timeline, owners, report, "timeline");
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
                if (graph?.asset != null && string.IsNullOrEmpty(graph.asset.localId))
                    valid &= TryAdd(Resolve(graph.asset), owners, report, "target-graph");
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                if (timeline?.asset != null && string.IsNullOrEmpty(timeline.asset.localId))
                    valid &= TryAdd(Resolve(timeline.asset), owners, report, "target-timeline");
            foreach (AgentPackageSkillMacroFile macro in document.macros ?? new List<AgentPackageSkillMacroFile>())
                if (macro?.asset != null && string.IsNullOrEmpty(macro.asset.localId))
                    valid &= TryAdd(Resolve(macro.asset), owners, report, "target-macro");
            return valid;
        }

        static UnityEngine.Object Resolve(AgentPackageObjectReference reference)
        {
            if (reference == null || !string.IsNullOrEmpty(reference.localId))
                return null;
            string path = AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (string.IsNullOrEmpty(path) || path != reference.assetPath)
                return null;
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            return assets.FirstOrDefault(value =>
                value && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _, out long localFileId) &&
                localFileId == reference.localFileId);
        }

        static bool TryAdd(
            UnityEngine.Object owner,
            ISet<UnityEngine.Object> owners,
            AgentCompileReport report,
            string label)
        {
            if (!owner || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(owner)))
            {
                report.Error("transaction.skill-flow." + label, "skill_transaction_owner_missing", "Skill Flow事务owner缺失或不是持久化资产。");
                return false;
            }
            owners.Add(owner);
            return true;
        }
    }
}
