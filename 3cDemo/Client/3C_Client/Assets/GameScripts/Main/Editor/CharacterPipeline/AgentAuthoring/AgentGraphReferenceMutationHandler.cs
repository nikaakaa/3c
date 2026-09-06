using System;
using System.Linq;
using TreeDesigner;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphReferenceMutationHandler : IAgentMutationHandler
    {
        const string StateMachineReferenceKey = "scopedGraph.m_InlineGraph";
        const string StateBehaviorReferenceKey = "stateBehaviorGraph.m_InlineSubTree";
        const string SharedTreeReferenceKey = "treeReference.m_SharedTreeAsset";

        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            return command switch
            {
                AgentEnsureGraphMutation ensure => PreflightEnsure(session, ensure),
                AgentConfigureGraphReferenceMutation configure => PreflightConfigure(session, configure),
                _ => throw new InvalidOperationException($"Unsupported graph reference command: {command.Kind}")
            };
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureGraphMutation ensure:
                    ApplyEnsure(session, ensure);
                    break;
                case AgentConfigureGraphReferenceMutation configure:
                    ApplyConfigure(session, configure);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported graph reference command: {command.Kind}");
            }
        }

        static bool PreflightEnsure(AgentMutationSession session, AgentEnsureGraphMutation command)
        {
            bool valid = command.Ownership == AgentGraphOwnership.Inline;
            if (!valid)
                session.Report.Error(command.Path, "graph_shared_asset_creation_unsupported", "新Graph只能创建为Inline，SharedAsset必须引用已存在的正式资产。");
            if (!session.TryResolveGraph(command.ParentGraph, command.Path + ".parentGraph", out BaseTree parent))
                return false;
            if (parent == null)
            {
                if (!session.IsApply &&
                    command.OwnerNode.Value.PlannedIdentity.IsValid &&
                    !session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node))
                {
                    session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_identity_missing", "新增Graph的owner Node必须由更早的typed Mutation计划创建。");
                    valid = false;
                }
                if (!session.IsApply && command.OwnerNode.Value.PlannedIdentity.IsValid &&
                    (command.GraphKind == AgentGraphKind.StateMachineGraph &&
                     !session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.Node) ||
                     command.GraphKind == AgentGraphKind.StateBehaviorSubTree &&
                     !session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.State)))
                {
                    session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_kind_invalid", "Graph reference owner的planned输出类型与Graph kind不匹配。");
                    valid = false;
                }
                if (command.GraphKind == AgentGraphKind.SubTree ||
                    command.GraphKind == AgentGraphKind.BaseTree ||
                    command.GraphKind == AgentGraphKind.RunnableTree)
                {
                    session.Report.Error(command.Path, "graph_reference_api_unavailable", "SubTree/通用Graph挂载与参数绑定尚无正式共享authoring API。");
                    valid = false;
                }
            }
            else if (!session.TryResolveNode(parent, command.OwnerNode, command.Path + ".ownerNode", out BaseNode owner))
                return false;
            else if (owner == null)
            {
                bool planned = !session.IsApply &&
                    command.OwnerNode.Value.PlannedIdentity.IsValid &&
                    session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node);
                if (!planned)
                {
                    session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_identity_missing", "新增Graph的owner Node在当前Mutation计划中无法解析。");
                    valid = false;
                }
                else if (command.GraphKind == AgentGraphKind.SubTree ||
                         command.GraphKind == AgentGraphKind.BaseTree ||
                         command.GraphKind == AgentGraphKind.RunnableTree)
                {
                    session.Report.Error(command.Path, "graph_reference_api_unavailable", "SubTree/通用Graph挂载与参数绑定尚无正式共享authoring API。");
                    valid = false;
                }
                else if (!PlannedOwnerKindMatches(command))
                {
                    session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_kind_invalid", "Graph reference owner的planned输出类型与Graph kind不匹配。");
                    valid = false;
                }
            }
            else
                valid &= ValidateOwner(owner, command, session);
            if (command.GraphKind == AgentGraphKind.ConditionRuleGraph || command.GraphKind == AgentGraphKind.Unknown)
            {
                session.Report.Error(command.Path + ".graphKind", "graph_creation_kind_unsupported", "该Graph kind不能通过节点Graph reference创建。");
                valid = false;
            }
            if (!valid)
                return false;
            session.AddPlanned(command, parent, command.DisplayName, "create or reuse inline graph reference");
            return true;
        }

        static bool PreflightConfigure(AgentMutationSession session, AgentConfigureGraphReferenceMutation command)
        {
            bool valid = true;
            if (command.ReferenceKey != StateMachineReferenceKey &&
                command.ReferenceKey != StateBehaviorReferenceKey &&
                command.ReferenceKey != SharedTreeReferenceKey)
            {
                session.Report.Error(command.Path, "graph_reference_api_unavailable", "该Graph reference没有正式共享authoring setter。");
                valid = false;
            }
            if (!session.TryResolveGraph(command.OwnerGraph, command.Path + ".ownerGraph", out BaseTree ownerGraph))
                return false;
            if (ownerGraph == null && command.OwnerNode.Value.PlannedIdentity.IsValid &&
                (command.ReferenceKey == StateMachineReferenceKey &&
                 !session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.Node) ||
                 command.ReferenceKey == StateBehaviorReferenceKey &&
                 !session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.State)))
            {
                session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_kind_invalid", "Graph reference owner的planned输出类型与正式setter不匹配。");
                valid = false;
            }
            BaseTree plannedChild = null;
            if (command.ChildGraph.IsValid && !session.TryResolveGraph(command.ChildGraph, command.Path + ".childGraph", out plannedChild))
                valid = false;
            if (ownerGraph != null)
            {
                if (!session.TryResolveNode(ownerGraph, command.OwnerNode, command.Path + ".ownerNode", out BaseNode owner))
                    return false;
                if (owner == null)
                {
                    bool planned = !session.IsApply && command.OwnerNode.Value.PlannedIdentity.IsValid &&
                        session.HasPlannedIdentity(command.OwnerNode.Value.PlannedIdentity.Identity, AgentMutationOutputKind.StateMachine, AgentMutationOutputKind.State, AgentMutationOutputKind.Node);
                    if (!planned)
                    {
                        session.Report.Error(command.Path + ".ownerNode", "graph_owner_planned_identity_missing", "Graph reference owner在当前Mutation计划中无法解析。");
                        return false;
                    }
                }
                if (owner != null)
                {
                    NodeGraphReference reference = FindReference(owner, command.ReferenceKey);
                    if (reference.OwnerNode == null)
                    {
                        session.Report.Error(command.Path + ".graphReferenceKey", "graph_reference_unknown", $"节点没有该Graph reference：{command.ReferenceKey}");
                        valid = false;
                    }
                    else if (!IsSupportedReference(owner, command.ReferenceKey))
                    {
                        session.Report.Error(command.Path, "graph_reference_api_unavailable", "该Graph reference没有正式共享authoring setter。");
                        valid = false;
                    }
                    else if (!OwnershipModeAllowed(owner, command.ReferenceKey, command.Ownership))
                    {
                        session.Report.Error(command.Path + ".graphOwnership", "graph_reference_ownership_mismatch", "目标Graph reference ownership没有对应的正式setter。");
                        valid = false;
                    }
                    if (command.ChildGraph.IsValid && plannedChild != null && !CanAttach(owner, command.ReferenceKey, plannedChild))
                    {
                        session.Report.Error(command.Path + ".childGraph", "graph_reference_type_mismatch", "目标Graph类型不符合节点Graph reference能力。");
                        valid = false;
                    }
                }
            }
            if (command.InputBindings.Count > 0 || command.OutputBindings.Count > 0)
            {
                session.Report.Error(command.Path + ".bindings", "graph_binding_api_unavailable", "SubTree动态参数绑定尚无正式共享authoring替换API。");
                valid = false;
            }
            if (command.Ownership == AgentGraphOwnership.SharedAsset && string.IsNullOrEmpty(command.SharedAssetPath))
            {
                session.Report.Error(command.Path + ".sharedAssetPath", "graph_shared_asset_path_missing", "SharedAsset Graph reference必须给出正式资产路径。");
                valid = false;
            }
            if (valid)
                session.AddPlanned(command, ownerGraph, command.ReferenceKey, "configure formal graph reference");
            return valid;
        }

        static void ApplyEnsure(AgentMutationSession session, AgentEnsureGraphMutation command)
        {
            if (!session.TryResolveGraph(command.ParentGraph, command.Path + ".parentGraph", out BaseTree parent) ||
                !session.TryResolveNode(parent, command.OwnerNode, command.Path + ".ownerNode", out BaseNode owner))
                return;
            BaseTree existing = command.GraphKind switch
            {
                AgentGraphKind.StateMachineGraph when owner is StateMachineNode stateMachine => stateMachine.Graph,
                AgentGraphKind.StateBehaviorSubTree when owner is StateNode state => state.SubTree,
                _ => null
            };
            if (existing != null)
            {
                existing.name = command.DisplayName;
                existing.CheckInit();
                session.RefreshIndex(command.Path);
                session.AddAppliedGraph(command, parent, existing, existing.GraphAuthoringId, "reused specialized graph");
                return;
            }
            if (!TryCreateGraph(command.GraphKind, command.DisplayName, out BaseTree graph))
            {
                session.Report.Error(command.Path, "graph_creation_failed", $"无法创建Graph：{command.GraphKind}");
                return;
            }
            graph.CheckInit();
            if (string.IsNullOrEmpty(graph.GraphAuthoringId))
            {
                session.Report.Error(command.Path, "graph_identity_missing", "新Graph未生成正式GraphAuthoringId。");
                return;
            }
            if (!TryAttach(owner, command.ReferenceKey, graph, null, command.Ownership, command.Path, session))
                return;
            owner.Refresh();
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddAppliedGraph(command, parent, graph, graph.GraphAuthoringId, "inline graph");
        }

        static void ApplyConfigure(AgentMutationSession session, AgentConfigureGraphReferenceMutation command)
        {
            if (!session.TryResolveGraph(command.OwnerGraph, command.Path + ".ownerGraph", out BaseTree ownerGraph) ||
                !session.TryResolveNode(ownerGraph, command.OwnerNode, command.Path + ".ownerNode", out BaseNode owner))
                return;
            BaseTree child = null;
            if (command.ChildGraph.IsValid && !session.TryResolveGraph(command.ChildGraph, command.Path + ".childGraph", out child))
                return;
            if (!TryAttach(owner, command.ReferenceKey, child, command.SharedAssetPath, command.Ownership, command.Path, session))
                return;
            owner.Refresh();
            session.RefreshIndex(command.Path);
            session.AddAppliedWithoutIdentity(command, ownerGraph, command.ReferenceKey, "configured graph reference");
        }

        static bool ValidateOwner(BaseNode owner, AgentEnsureGraphMutation command, AgentMutationSession session)
        {
            NodeGraphReference reference = FindReference(owner, command.ReferenceKey);
            if (reference.OwnerNode == null)
            {
                session.Report.Error(command.Path + ".graphReferenceKey", "graph_reference_unknown", $"节点没有该Graph reference：{command.ReferenceKey}");
                return false;
            }
            if (!IsSupportedReference(owner, command.ReferenceKey))
            {
                session.Report.Error(command.Path, "graph_reference_api_unavailable", "该Graph reference没有正式共享authoring setter。");
                return false;
            }
            if (!reference.Inline)
            {
                session.Report.Error(command.Path + ".graphOwnership", "graph_reference_not_inline", "新Graph必须挂载到Inline Graph reference。");
                return false;
            }
            if (reference.Tree != null &&
                !((command.GraphKind == AgentGraphKind.StateMachineGraph && reference.Tree is StateMachineGraph) ||
                  (command.GraphKind == AgentGraphKind.StateBehaviorSubTree && reference.Tree is StateBehaviorSubTree)))
            {
                session.Report.Error(command.Path, "graph_reference_already_bound", "新Graph的目标reference已经绑定Graph。");
                return false;
            }
            bool supported = command.GraphKind switch
            {
                AgentGraphKind.StateMachineGraph => owner is StateMachineNode && command.ReferenceKey == StateMachineReferenceKey,
                AgentGraphKind.StateBehaviorSubTree => owner is StateNode && command.ReferenceKey == StateBehaviorReferenceKey,
                _ => false
            };
            if (!supported)
                session.Report.Error(command.Path + ".graphKind", "graph_owner_kind_mismatch", "Graph kind与owner Node的正式Graph reference能力不一致。");
            return supported;
        }

        static bool PlannedOwnerKindMatches(AgentEnsureGraphMutation command)
        {
            return command.GraphKind == AgentGraphKind.StateMachineGraph && command.ReferenceKey == "scopedGraph.m_InlineGraph" ||
                   command.GraphKind == AgentGraphKind.StateBehaviorSubTree && command.ReferenceKey == "stateBehaviorGraph.m_InlineSubTree";
        }

        static NodeGraphReference FindReference(BaseNode owner, string key)
        {
            if (owner == null)
                return default;
            NodeGraphReference? reference = owner.GetGraphReferences()
                .FirstOrDefault(value => string.Equals(value.Key, key, StringComparison.Ordinal));
            return reference ?? default;
        }

        static bool IsSupportedReference(BaseNode owner, string key)
        {
            return owner is StateMachineNode && key == StateMachineReferenceKey ||
                   owner is StateNode && key == StateBehaviorReferenceKey ||
                   owner?.GetModule<TreeReferenceModule>() != null && key == SharedTreeReferenceKey;
        }

        static bool CanAttach(BaseNode owner, string key, BaseTree child)
        {
            if (child == null)
                return true;
            return owner is StateMachineNode && key == StateMachineReferenceKey && child is StateMachineGraph ||
                   owner is StateNode && key == StateBehaviorReferenceKey && child is SubTree;
        }

        static bool OwnershipModeAllowed(BaseNode owner, string key, AgentGraphOwnership ownership)
        {
            if (owner is StateMachineNode && key == StateMachineReferenceKey ||
                owner is StateNode && key == StateBehaviorReferenceKey)
                return ownership == AgentGraphOwnership.Inline || ownership == AgentGraphOwnership.SharedAsset;
            return owner?.GetModule<TreeReferenceModule>() != null &&
                   key == SharedTreeReferenceKey &&
                   ownership == AgentGraphOwnership.SharedAsset;
        }

        static bool TryAttach(
            BaseNode owner,
            string key,
            BaseTree child,
            string sharedAssetPath,
            AgentGraphOwnership ownership,
            string path,
            AgentMutationSession session)
        {
            if (child == null)
            {
                if (owner is StateMachineNode stateMachine && key == StateMachineReferenceKey)
                {
                    stateMachine.GetModule<ScopedGraphReferenceModule>()?.SetInlineGraph(null);
                    return true;
                }
                if (owner is StateNode state && key == StateBehaviorReferenceKey)
                {
                    state.GetModule<StateBehaviorGraphReferenceModule>()?.SetInlineSubTree(null);
                    return true;
                }
                if (owner?.GetModule<TreeReferenceModule>() is TreeReferenceModule treeReference && key == SharedTreeReferenceKey)
                {
                    treeReference.SetTreeAsset(null);
                    return true;
                }
            }
            if (ownership == AgentGraphOwnership.Inline && CanAttach(owner, key, child))
            {
                if (owner is StateMachineNode stateMachine && key == StateMachineReferenceKey)
                {
                    stateMachine.GetModule<ScopedGraphReferenceModule>()?.SetInlineGraph(child as StateMachineGraph);
                    return true;
                }
                if (owner is StateNode state && key == StateBehaviorReferenceKey)
                {
                    state.GetModule<StateBehaviorGraphReferenceModule>()?.SetInlineSubTree(child as SubTree);
                    return true;
                }
            }
            else if (ownership == AgentGraphOwnership.SharedAsset && key == SharedTreeReferenceKey)
            {
                BaseTreeAsset asset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(sharedAssetPath);
                if (!asset)
                {
                    session.Report.Error(path + ".sharedAssetPath", "graph_shared_asset_not_found", $"SharedAsset无法解析：{sharedAssetPath}");
                    return false;
                }
                TreeReferenceModule treeReference = owner.GetModule<TreeReferenceModule>();
                if (treeReference != null)
                {
                    treeReference.SetTreeAsset(asset);
                    return true;
                }
            }
            else if (ownership == AgentGraphOwnership.SharedAsset && key == StateMachineReferenceKey && owner is StateMachineNode stateMachine)
            {
                BaseTreeAsset asset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(sharedAssetPath);
                if (!asset || asset.Tree is not StateMachineGraph)
                {
                    session.Report.Error(path + ".sharedAssetPath", "graph_shared_asset_type_invalid", $"Shared StateMachineGraph asset无法解析：{sharedAssetPath}");
                    return false;
                }
                stateMachine.GetModule<ScopedGraphReferenceModule>()?.SetSharedGraphAsset(asset);
                return true;
            }
            else if (ownership == AgentGraphOwnership.SharedAsset && key == StateBehaviorReferenceKey && owner is StateNode state)
            {
                BaseTreeAsset asset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(sharedAssetPath);
                if (!asset || asset.Tree is not SubTree)
                {
                    session.Report.Error(path + ".sharedAssetPath", "graph_shared_asset_type_invalid", $"Shared StateBehavior SubTree asset无法解析：{sharedAssetPath}");
                    return false;
                }
                state.GetModule<StateBehaviorGraphReferenceModule>()?.SetSharedSubTreeAsset(asset);
                return true;
            }
            session.Report.Error(path, "graph_reference_owner_unsupported", $"正式authoring API不支持该Graph reference：{key}");
            return false;
        }

        static bool TryCreateGraph(AgentGraphKind kind, string name, out BaseTree graph)
        {
            graph = kind switch
            {
                AgentGraphKind.StateMachineGraph => StateMachineNode.CreateDefaultGraph(),
                AgentGraphKind.StateBehaviorSubTree => StateNode.CreateDefaultStateBehaviorGraph(),
                _ => null
            };
            if (graph == null)
                return false;
            graph.name = string.IsNullOrWhiteSpace(name) ? graph.GetType().Name : name;
            graph.EnsureGraphAuthoringId();
            return true;
        }
    }
}
