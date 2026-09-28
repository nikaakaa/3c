using FlowCanvas.Nodes;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {
        static Attack_Rush_ExplodeParts BuildAttack_Rush_Explode(RootParts rootParts, BtsmtlAuthoringGenerationContext context)
        {
            var parts = new Attack_Rush_ExplodeParts();
            parts.graph4 = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, "faba30d8-c53d-b3c2-90d2-a484bb704d92", typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Attack_Rush Release");
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.graph4, new[] { "b68ce95b-79e9-c12c-17ed-73c7105d4405", "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "5ab8a6c4-1171-4c70-b69d-e78a3b74e41d", "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e" }, new[] { "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda", "6a4c65f0-062b-2154-92d8-87d2a68a20bb", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8" });
            var node14 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillBooleanInputFlowNode), "5ab8a6c4-1171-4c70-b69d-e78a3b74e41d", "AttackHeld", new Vector2(-520f, 120f));
            var node11 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillActionWindowActiveFlowNode), "b68ce95b-79e9-c12c-17ed-73c7105d4405", "Window RushRelease", new Vector2(-520f, 0f));
            var node12 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillNativeNodeWrapper<AND>), "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "AND", new Vector2(120f, 60f));
            var node16 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e", "NOT", new Vector2(-240f, 120f));
            var node13 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph4, typeof(BtsmtlSkillConditionResultFlowNode), "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "条件结果", new Vector2(600f, 180f));
            BtsmtlSkillAuthoringContract.Apply(node14, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "AttackHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringContract.Apply(node11, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RushRelease") });
            var edge5 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node11, "m_Output", node12, "a", "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda");
            var edge6 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node12, "Value", node13, "m_Result", "6a4c65f0-062b-2154-92d8-87d2a68a20bb");
            var edge7 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node14, "m_Output", node16, "value", "034c36c2-53ed-4e99-b8d6-dc57d4d5208e");
            var edge9 = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph4, node16, "Value", node12, "b", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8");
            parts.sawGraph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(rootParts.graph, CorinActionSteeringAuthoring.Id("corin.rush.saw-condition"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Rush SawExplode after frame 23");
            var saw = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillActionEventReceivedFlowNode), CorinActionSteeringAuthoring.Id("corin.rush.saw-event"), "SawExplode", Vector2.zero);
            var window = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillActionWindowActiveFlowNode), CorinActionSteeringAuthoring.Id("corin.rush.saw-window"), "RushSawExplode", new Vector2(0, 180));
            var both = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillNativeNodeWrapper<AND>), CorinActionSteeringAuthoring.Id("corin.rush.saw-and"), "AND", new Vector2(240, 0));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillConditionResultFlowNode), CorinActionSteeringAuthoring.Id("corin.rush.saw-result"), "条件结果", new Vector2(480, 0));
            BtsmtlSkillAuthoringContract.Apply(saw, new[] { new BtsmtlSkillAuthoringFieldValue("eventId", "SawExplode") });
            BtsmtlSkillAuthoringContract.Apply(window, new[] { new BtsmtlSkillAuthoringFieldValue("windowType", "RushSawExplode") });
            var a = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.sawGraph, saw, "m_Output", both, "a", CorinActionSteeringAuthoring.Id("corin.rush.saw-a"));
            var c = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.sawGraph, window, "m_Output", both, "b", CorinActionSteeringAuthoring.Id("corin.rush.saw-b"));
            var completed = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillStateRootCompletedFlowNode), CorinActionSteeringAuthoring.Id("corin.rush.saw-completed"), "状态主体已完成", new Vector2(0, 360));
            var either = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.sawGraph, typeof(BtsmtlSkillNativeNodeWrapper<OR>), CorinActionSteeringAuthoring.Id("corin.rush.saw-or"), "OR", new Vector2(360, 0));
            var e = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.sawGraph, completed, "m_Output", either, "a", CorinActionSteeringAuthoring.Id("corin.rush.saw-completed-edge"));
            var f = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.sawGraph, both, "Value", either, "b", CorinActionSteeringAuthoring.Id("corin.rush.saw-or-edge"));
            var d = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.sawGraph, either, "Value", result, "m_Result", CorinActionSteeringAuthoring.Id("corin.rush.saw-output"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(parts.sawGraph, new[] { saw.UID, window.UID, both.UID, result.UID, completed.UID, either.UID }, new[] { a.UID, c.UID, d.UID, e.UID, f.UID });
            BtsmtlSkillAuthoringCode.PruneBlackboard(parts.sawGraph, System.Array.Empty<string>());
            return parts;
        }

        sealed class Attack_Rush_ExplodeParts
        {
            internal BtsmtlSkillFlowGraph graph4;
            internal BtsmtlSkillFlowGraph sawGraph;
        }
    }
}
