using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphTransactionOwnerCollector
    {
        readonly HashSet<Object> m_Owners = new HashSet<Object>();

        public bool TryCollect(
            CharacterPipelineDefinition definition,
            out Object[] owners,
            out string errorCode,
            out string errorMessage)
        {
            owners = null;
            errorCode = string.Empty;
            errorMessage = string.Empty;
            m_Owners.Clear();

            if (!definition || !definition.RootTreeAsset || definition.RootTreeAsset.Tree == null)
                return Fail("transaction_root_missing", "无法为缺少 RootTreeAsset 的 Definition 建立 authoring 事务。", out errorCode, out errorMessage);

            if (!TryAddOwner(definition, out errorCode, out errorMessage) ||
                !TryAddOwner(definition.RootTreeAsset, out errorCode, out errorMessage) ||
                !TryAddOwner(definition.InputProfile, out errorCode, out errorMessage) ||
                !TryAddOwner(definition.SimulationProgram, out errorCode, out errorMessage) ||
                !TryAddOwner(definition.PresentationProjection, out errorCode, out errorMessage))
                return false;

            var projectionErrors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(
                definition.RootTreeAsset.Tree,
                projectionErrors);
            if (!projection.IsValid)
                return Fail("transaction_topology_invalid", string.Join("\n", projectionErrors), out errorCode, out errorMessage);

            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                if (!TryAddOwner(projection.Graphs[i].Graph.SerializedOwner, out errorCode, out errorMessage))
                    return false;
            }
            for (int i = 0; i < projection.Timelines.Count; i++)
            {
                if (!TryAddOwner(projection.Timelines[i].Timeline.SerializedOwner, out errorCode, out errorMessage))
                    return false;
            }

            for (int i = 0; i < definition.ActionProfiles.Count; i++)
            {
                if (!TryAddOwner(definition.ActionProfiles[i], out errorCode, out errorMessage))
                    return false;
            }
            if (!definition.GameplayEffectProfile || !definition.GameplayEffectProfile.TagCatalog ||
                !TryAddOwner(definition.GameplayEffectProfile.TagCatalog, out errorCode, out errorMessage))
                return Fail("transaction_tag_catalog_missing", "GameplayTagCatalog 缺失，无法保证 ActionProfile/tag 写入回滚。", out errorCode, out errorMessage);

            owners = new Object[m_Owners.Count];
            m_Owners.CopyTo(owners);
            return true;
        }

        bool TryAddOwner(Object owner, out string errorCode, out string errorMessage)
        {
            errorCode = string.Empty;
            errorMessage = string.Empty;
            if (!owner)
                return Fail("transaction_owner_missing", "可达 Graph 缺少 SerializedOwner，无法保证完整回滚。", out errorCode, out errorMessage);

            string assetPath = AssetDatabase.GetAssetPath(owner);
            if (string.IsNullOrEmpty(assetPath))
                return Fail("transaction_owner_not_asset", $"事务 owner 不是持久化项目资产：{owner.name}", out errorCode, out errorMessage);

            m_Owners.Add(owner);
            return true;
        }

        static bool Fail(string code, string message, out string errorCode, out string errorMessage)
        {
            errorCode = code;
            errorMessage = message;
            return false;
        }
    }
}
