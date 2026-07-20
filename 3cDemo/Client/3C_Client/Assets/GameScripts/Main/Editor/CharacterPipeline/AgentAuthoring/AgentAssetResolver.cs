using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Input;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAssetResolver
    {
        readonly CharacterPipelineDefinition m_Definition;
        readonly AgentGraphSnapshot m_Snapshot;

        public AgentAssetResolver(CharacterPipelineDefinition definition, AgentGraphSnapshot snapshot)
        {
            m_Definition = definition;
            m_Snapshot = snapshot;
        }

        public bool TryGetRootTree(out BaseTree rootTree, AgentCompileReport report, string path)
        {
            rootTree = null;
            if (!m_Definition)
            {
                report?.Error(path, "missing_definition", "CharacterPipelineDefinition 缺失。", "从当前角色 Definition 打开 Agent authoring。");
                return false;
            }

            if (!m_Definition.RootTreeAsset)
            {
                report?.Error(path, "missing_root_tree", "CharacterPipelineDefinition.RootTreeAsset 缺失。", "先配置 RootTreeAsset。");
                return false;
            }

            rootTree = m_Definition.RootTreeAsset.Tree;
            return rootTree != null;
        }

        public bool TryResolveInputValue(string inputValueId, out CharacterInputValueDefinition value)
        {
            value = null;
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile || string.IsNullOrEmpty(inputValueId))
                return false;

            IReadOnlyList<CharacterInputValueDefinition> values = profile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition candidate = values[i];
                if (candidate != null && string.Equals(candidate.InputValueId, inputValueId, StringComparison.Ordinal))
                {
                    value = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolveActionRequest(string requestId, out CharacterActionRequestDefinition request)
        {
            request = null;
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile || string.IsNullOrEmpty(requestId))
                return false;

            IReadOnlyList<CharacterActionRequestDefinition> requests = profile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition candidate = requests[i];
                if (candidate != null && string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal))
                {
                    request = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolveActionProfile(string actionId, out ActionProfile profile)
        {
            profile = null;
            if (!m_Definition || string.IsNullOrEmpty(actionId))
                return false;

            IReadOnlyList<ActionProfile> profiles = m_Definition.ActionProfiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile candidate = profiles[i];
                if (candidate && string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal))
                {
                    profile = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolveTimelineAsset(AgentAssetReference reference, out TimelineAsset timelineAsset)
        {
            timelineAsset = null;
            UnityEngine.Object asset = ResolveObject(reference.AssetGuid, reference.AssetPath, typeof(TimelineAsset));
            if (asset is TimelineAsset directTimelineAsset)
            {
                timelineAsset = directTimelineAsset;
                return true;
            }

            return TryResolveTimelineFromSnapshot(reference.LogicalId, out timelineAsset);
        }

        public bool TryResolveActionContext(AgentAssetReference reference, out ActionContextSlot actionContext)
        {
            actionContext = null;
            UnityEngine.Object asset = ResolveObject(reference.AssetGuid, reference.AssetPath, typeof(ActionContextSlot));
            if (asset is ActionContextSlot directContext)
            {
                actionContext = directContext;
                return true;
            }

            return TryResolveActionContextFromSnapshot(reference.LogicalId, out actionContext);
        }

        bool TryResolveTimelineFromSnapshot(string key, out TimelineAsset timelineAsset)
        {
            timelineAsset = null;
            if (m_Snapshot == null || string.IsNullOrEmpty(key))
                return false;

            AgentSnapshotAsset match = FindSnapshotAsset(m_Snapshot.timelineAssets, key);
            if (match == null)
                return false;

            timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(match.assetPath);
            return timelineAsset;
        }

        bool TryResolveActionContextFromSnapshot(string key, out ActionContextSlot actionContext)
        {
            actionContext = null;
            if (m_Snapshot == null || string.IsNullOrEmpty(key))
                return false;

            AgentSnapshotAsset match = FindSnapshotAsset(m_Snapshot.actionContextAssets, key);
            if (match == null)
                return false;

            actionContext = AssetDatabase.LoadAssetAtPath<ActionContextSlot>(match.assetPath);
            return actionContext;
        }

        static AgentSnapshotAsset FindSnapshotAsset(List<AgentSnapshotAsset> assets, string key)
        {
            AgentSnapshotAsset match = null;
            for (int i = 0; i < assets.Count; i++)
            {
                AgentSnapshotAsset asset = assets[i];
                if (asset == null)
                    continue;

                bool same = string.Equals(asset.id, key, StringComparison.Ordinal) ||
                            string.Equals(asset.assetGuid, key, StringComparison.Ordinal) ||
                            string.Equals(asset.assetPath, key, StringComparison.Ordinal) ||
                            string.Equals(asset.name, key, StringComparison.Ordinal);
                if (!same)
                    continue;

                if (match != null)
                    return null;

                match = asset;
            }
            return match;
        }

        static UnityEngine.Object ResolveObject(string guid, string path, Type expectedType)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(resolvedPath))
                    return AssetDatabase.LoadAssetAtPath(resolvedPath, expectedType);
            }

            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.LoadAssetAtPath(path, expectedType);

            return null;
        }
    }
}
