using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using NodeCanvas.Framework;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    internal static class BtsmtlSkillDocumentAssetValidator
    {
        public static bool Validate(
            AgentMutationSession session,
            AgentPackageSkillFlowDocument document,
            string path)
        {
            bool valid = true;
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph?.asset != null && !string.IsNullOrEmpty(graph.asset.localId) && !graph.asset.localId.StartsWith("local:", StringComparison.Ordinal))
                {
                    session.Report.Error(path + ".graphs[" + graph.id + "].asset", "skill_graph_asset_invalid", "Skill Graph计划资产identity无效。");
                    valid = false;
                }
                if (graph?.asset != null && string.IsNullOrEmpty(graph.asset.localId) &&
                    !session.Resolver.TryResolveSkillObject(graph.asset, out FlowGraph _))
                {
                    session.Report.Error(path + ".graphs[" + graph.id + "].asset", "skill_graph_asset_unresolved", "Skill Graph asset引用无法解析。");
                    valid = false;
                }
                if (graph?.asset != null && string.IsNullOrEmpty(graph.asset.localId) &&
                    session.Resolver.TryResolveSkillObject(graph.asset, out FlowGraph graphAsset))
                {
                    bool mainOwner = graph.ownership == AgentGraphOwnership.SharedAsset.ToString() ||
                        graph.ownership == AgentGraphOwnership.RootAsset.ToString();
                    bool main = AssetDatabase.IsMainAsset(graphAsset);
                    if (mainOwner != main)
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].ownership", "skill_graph_asset_ownership_invalid", "Skill Graph ownership与正式资产类型不一致。");
                        valid = false;
                    }
                    if (!mainOwner &&
                        AssetDatabase.GetAssetPath(graphAsset) != OwnerPath(session, document, graph.owner?.graphId))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].owner", "skill_graph_owner_file_invalid", "私有Skill Graph必须保存在调用方所属的技能文件中。");
                        valid = false;
                    }
                    valid &= ValidateExistingGraphIdentity(graph, graphAsset, path, session.Report);
                }
                foreach (AgentPackageSkillFlowNode node in graph?.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    JObject properties = node?.properties;
                    foreach (BtsmtlSkillNodeAuthoringReferenceAttribute reference in
                             BtsmtlSkillNodeAuthoringValidation.References(node?.capability))
                    {
                        if (reference.Optional && properties?[reference.FieldId] == null)
                            continue;
                        bool resolved = reference.Kind switch
                        {
                            BtsmtlSkillNodeAuthoringReferenceKind.ActionRequest =>
                                session.Resolver.TryResolveActionRequest(
                                    properties?.Value<string>(reference.FieldId),
                                    out _),
                            BtsmtlSkillNodeAuthoringReferenceKind.InputValue =>
                                session.Resolver.TryResolveInputValue(
                                    properties?.Value<string>(reference.FieldId),
                                    out _),
                            BtsmtlSkillNodeAuthoringReferenceKind.ActionProfile =>
                                properties?[reference.FieldId] is JObject profile &&
                                session.Resolver.TryResolveActionProfile(
                                    profile.Value<string>("id"),
                                    out _),
                            BtsmtlSkillNodeAuthoringReferenceKind.Asset =>
                                properties?[reference.FieldId] is JObject assetReference &&
                                session.Resolver.TryResolveSkillObject(
                                    assetReference.ToObject<AgentPackageObjectReference>(),
                                    out UnityEngine.Object asset) &&
                                (reference.ObjectType == null || reference.ObjectType.IsInstanceOfType(asset)),
                            _ => false
                        };
                        if (!resolved)
                        {
                            session.Report.Error(
                                path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties." + reference.FieldId,
                                reference.ErrorCode,
                                reference.ErrorMessage);
                            valid = false;
                        }
                    }
                }
                foreach (AgentPackageSkillBlackboardDeclaration declaration in graph?.blackboardDeclarations ??
                         new List<AgentPackageSkillBlackboardDeclaration>())
                    if (declaration?.inputBinding != null &&
                        !session.Resolver.TryResolvePortableInputValue(
                            declaration.inputBinding.inputValueId,
                            ProgramInputValueKind.ActionTargetSnapshot))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].blackboardDeclarations[" + declaration.id + "].inputBinding",
                            "skill_input_binding_unresolved", "Skill Blackboard inputBinding的inputValueId无法解析。");
                        valid = false;
                    }
            }
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                if (timeline?.asset != null && string.IsNullOrEmpty(timeline.asset.localId))
                {
                    if (!session.Resolver.TryResolveSkillObject(timeline.asset, out TimelineAsset timelineAsset))
                    {
                        session.Report.Error(path + ".timelines[" + timeline.id + "].asset", "skill_timeline_asset_unresolved", "Skill Timeline asset引用无法解析。");
                        valid = false;
                        continue;
                    }
                    bool shared = timeline.ownership == BtsmtlSkillTimelineOwnership.Shared.ToString();
                    string ownerPath = OwnerPath(session, document, timeline.ownerGraphId);
                    if (shared != AssetDatabase.IsMainAsset(timelineAsset) ||
                        !shared && (!AssetDatabase.IsSubAsset(timelineAsset) ||
                                    AssetDatabase.GetAssetPath(timelineAsset) != ownerPath))
                    {
                        session.Report.Error(path + ".timelines[" + timeline.id + "].ownership", "skill_timeline_asset_ownership_invalid", "Skill Timeline ownership与正式资产类型不一致。");
                        valid = false;
                    }
                }
            }
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                foreach (AgentPackageSkillTimelineTrack track in timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                    foreach (AgentPackageSkillTimelineClip clip in track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                        if (clip?.kind == TimelineContractKinds.AnimationClip &&
                            !session.Resolver.TryResolveSkillObject(clip.animationClip, out UnityEngine.AnimationClip _))
                        {
                            session.Report.Error(path + ".timelines[" + timeline.id + "].tracks[" + track.id + "].clips[" + clip.id + "].animationClip",
                                "skill_animation_clip_unresolved", "Skill Timeline AnimationClip引用无法解析。");
                            valid = false;
                        }
            foreach (AgentPackageSkillDefinitionFile skill in document.skills ?? new List<AgentPackageSkillDefinitionFile>())
            {
                if (skill == null)
                    continue;
                if (!session.Resolver.TryResolveActionProfile(skill.actionProfileId, out _))
                {
                    session.Report.Error(path + ".skills[" + skill.skillId + "].actionProfileId", "skill_action_profile_unresolved", "Skill ActionProfile无法解析。");
                    valid = false;
                }
                if (!session.Resolver.TryResolveActionContext(new AgentAssetReference(
                        skill.actionContext,
                        skill.actionContextAssetPath,
                        skill.actionContextAssetGuid), out _))
                {
                    session.Report.Error(path + ".skills[" + skill.skillId + "].actionContext", "skill_action_context_unresolved", "Skill ActionContext无法解析。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateExistingGraphIdentity(
            AgentPackageSkillFlowGraphFile target,
            FlowGraph asset,
            string path,
            AgentCompileReport report)
        {
            bool valid = true;
            var nodes = asset.allNodes.OfType<FlowNode>()
                .ToDictionary(value => value.UID, StringComparer.Ordinal);
            var edges = asset.allNodes.OfType<FlowNode>()
                .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                .ToDictionary(value => value.UID, StringComparer.Ordinal);
            foreach (AgentPackageSkillGraphAnchor anchor in target.anchors ?? new List<AgentPackageSkillGraphAnchor>())
            {
                if (!string.IsNullOrEmpty(anchor?.nodeId) &&
                    !anchor.nodeId.StartsWith("local:", StringComparison.Ordinal) &&
                    !nodes.ContainsKey(anchor.nodeId))
                {
                    report.Error(path + ".graphs[" + target.id + "].anchors[" + anchor.kind + "]",
                        "skill_anchor_identity_missing",
                        "Document anchor identity不存在于当前正式Skill Graph。");
                    valid = false;
                }
            }
            foreach (AgentPackageSkillFlowNode node in target.nodes ?? new List<AgentPackageSkillFlowNode>())
            {
                if (!string.IsNullOrEmpty(node?.id) &&
                    !node.id.StartsWith("local:", StringComparison.Ordinal) &&
                    !nodes.ContainsKey(node.id))
                {
                    report.Error(path + ".graphs[" + target.id + "].nodes[" + node.id + "]",
                        "skill_node_identity_missing",
                        "Document Node identity不存在于当前正式Skill Graph。");
                    valid = false;
                }
            }
            foreach (AgentPackageSkillFlowEdge edge in target.edges ?? new List<AgentPackageSkillFlowEdge>())
            {
                if (!string.IsNullOrEmpty(edge?.id) &&
                    !edge.id.StartsWith("local:", StringComparison.Ordinal) &&
                    !edges.ContainsKey(edge.id))
                {
                    report.Error(path + ".graphs[" + target.id + "].edges[" + edge.id + "]",
                        "skill_edge_identity_missing",
                        "Document Edge identity不存在于当前正式Skill Graph。");
                    valid = false;
                    continue;
                }
                if (edge != null && !string.IsNullOrEmpty(edge.conditionGraphId) &&
                    edges.TryGetValue(edge.id, out BinderConnection actual) &&
                    actual is not BtsmtlSkillFlowConnection)
                {
                    report.Error(path + ".graphs[" + target.id + "].edges[" + edge.id + "]",
                        "skill_edge_transfer_type_mismatch",
                        "Document Edge声明了转移条件，但正式Skill Graph连线未携带转移数据。");
                    valid = false;
                }
            }
            return valid;
        }

        static string OwnerPath(
            AgentMutationSession session,
            AgentPackageSkillFlowDocument document,
            string graphId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(graphId) && visited.Add(graphId))
            {
                AgentPackageSkillFlowGraphFile owner = document.graphs.FirstOrDefault(value => value?.id == graphId);
                if (owner == null)
                    return string.Empty;
                if (session.Resolver.TryResolveSkillObject(owner.asset, out FlowGraph asset))
                {
                    string path = AssetDatabase.GetAssetPath(asset);
                    return AssetDatabase.LoadMainAssetAtPath(path) is IBtsmtlSkillFlowGraph ? path : string.Empty;
                }
                graphId = owner.owner?.graphId;
            }
            return string.Empty;
        }
    }
}
