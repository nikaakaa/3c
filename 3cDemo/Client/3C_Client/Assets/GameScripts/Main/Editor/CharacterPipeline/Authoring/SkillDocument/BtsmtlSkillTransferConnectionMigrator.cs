#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillTransferConnectionMigrator
    {
        static readonly string[] SkillRoots =
        {
            "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.f642ec00edd595e1dcf13e4e27fe79339915e188264180f92f10350c5baf3060.asset",
            "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.405b6c5d13e66ae379c647152961ea33db573cbb25270013699cd5b5b1b68c0c.asset",
            "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.Skill.9b38fd12dc097f01acc9250c3f9a4ff3b919bf10536a1e004132ad755f59a127.asset"
        };

        public static string DryRun()
        {
            string result = Run(apply: false);
            Debug.Log($"[TransferMigrator] {result}");
            return result;
        }

        public static string Apply()
        {
            string result = Run(apply: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TransferMigrator] {result}");
            return result;
        }

        [MenuItem("Tools/BTSMTL/迁移状态机转移连线-干跑")]
        static void DryRunMenu() => DryRun();

        [MenuItem("Tools/BTSMTL/迁移状态机转移连线-应用")]
        static void ApplyMenu() => Apply();

        static string Run(bool apply)
        {
            int graphs = 0, legacyEdges = 0, alreadyMigrated = 0, migrated = 0;
            var errors = new List<string>();
            foreach (string path in SkillRoots)
            {
                var root = AssetDatabase.LoadAssetAtPath<BtsmtlSkillFlowGraph>(path);
                if (root == null)
                {
                    errors.Add("root missing: " + path);
                    continue;
                }
                IReadOnlyList<FlowGraph> closure;
                try
                {
                    closure = BtsmtlSkillGraphClosure.Validate(root, true);
                }
                catch (Exception e)
                {
                    errors.Add("closure: " + e.Message);
                    continue;
                }
                foreach (FlowGraph graph in closure)
                {
                    if (!(graph is IBtsmtlSkillFlowGraph authoring) ||
                        authoring.Role != BtsmtlSkillFlowGraphRole.StateMachine)
                        continue;
                    graphs++;
                    var pending = new List<(BinderConnection edge, BtsmtlSkillStepPort step)>();
                    foreach (FlowNode node in graph.allNodes.ToArray())
                    {
                        if (!(node is BtsmtlSkillCompositeFlowNode composite))
                            continue;
                        foreach (BinderConnection edge in node.outConnections.OfType<BinderConnection>().ToArray())
                        {
                            if (!(edge.sourcePort is FlowOutput))
                                continue;
                            if (edge is BtsmtlSkillFlowConnection)
                            {
                                alreadyMigrated++;
                                continue;
                            }
                            legacyEdges++;
                            BtsmtlSkillStepPort step = composite.Steps.FirstOrDefault(value => value.Id == edge.sourcePortID);
                            if (step == null)
                            {
                                errors.Add($"{authoring.AuthoringId}/{node.name}: step missing for {edge.sourcePortID}");
                                continue;
                            }
                            pending.Add((edge, step));
                        }
                    }
                    if (!apply || pending.Count == 0)
                        continue;
                    BtsmtlSkillFlowEditorMutation.Execute(graph, "迁移状态机转移连线", () =>
                    {
                        foreach (var (edge, step) in pending)
                        {
                            Port source = edge.sourcePort;
                            Port target = edge.targetPort;
                            string uid = edge.UID;
                            if (source == null || target == null)
                                throw new InvalidOperationException($"迁移连线端点缺失：{uid}");
                            graph.RemoveConnection(edge, false);
                            var created = graph.CreatePortConnection(source, target) as BtsmtlSkillFlowConnection
                                ?? throw new InvalidOperationException($"迁移重建失败：{uid}");
                            created.ConfigureAuthoringIdentity(uid);
                            created.Configure(step.Condition, step.Priority, step.AbortPolicy);
                            migrated++;
                        }
                    });
                }
            }
            return $"apply={apply} graphs={graphs} legacyEdges={legacyEdges} alreadyMigrated={alreadyMigrated} migrated={migrated} errors=[{string.Join(" | ", errors)}]";
        }
    }
}
#endif
