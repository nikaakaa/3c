using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinRushAttackGameplayAbilityAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.asset = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_01_01.asset", 11400000L);
            var asset1 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_01_02.asset", 11400000L);
            var asset2 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Rush_AttackProperty_02.asset", 11400000L);
            var asset3 = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset", 11400000L);
            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "RushAttack", "9152ca92-afdd-f163-b9e3-404a8c2b96aa", "RushAttack");
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), "39ade2b9-4e24-a29e-abfb-a81e84db5277", "RushAttack StateMachine", new Vector2(240f, 0f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "bb92e55c-4056-69fd-25aa-353be1336431", "技能入口", new Vector2(-360f, 0f));
            parts.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, "4413153b-fb40-eabd-22e2-22ad05775e03", "RushAttack StateMachine", "9152ca92-afdd-f163-b9e3-404a8c2b96aa", "39ade2b9-4e24-a29e-abfb-a81e84db5277");
            var state4 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "1846cf39-ab38-7848-ce4e-be693d20f698", "状态机出口", new Vector2(1320f, 0f));
            parts.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "1ff0e67d-fb55-144e-29a5-cd3187254d75", "Attack_Rush_Explode", new Vector2(270f, 420f));
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "4b7150eb-050a-2929-3660-b52314306ed3", "Attack_Rush", new Vector2(120f, 220f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "8256282c-8945-45b3-f33a-5c394daa6ef6", "状态机入口", new Vector2(-320f, 0f));
            var state5 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeAnyState), "b3a1e60a-b751-4cd0-a8bc-f19912dc28d8", null, new Vector2(60f, 493.3333f));
            parts.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "ed6a3eba-d3f2-0d8d-c8c4-a874e9f5f731", "Attack_Rush_End", new Vector2(420f, 220f));
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset3, new[] { parts.asset, asset1, asset2 }, new[] { endRule, endRule1, endRule2 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Rush", true, "", "", false);
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc");
            parts.stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state1, "d1668a05-e3af-b894-ddfb-9e848b7ce014");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state2, "64c2c918-d608-8b33-5233-48f2aadde1af");
            parts.stateEdge2 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state2, "c9ccc511-57a1-4c24-0673-13688d712861");
            parts.stateEdge3 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state2, (BtsmtlSkillNativeState)parts.state3, "242990af-270b-4fb3-efe6-cea7e6daa02c");
            parts.stateEdge4 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state3, (BtsmtlSkillNativeState)state4, "a7a9a9b4-bfe5-8bac-3dd3-2c8cad0d8df9");
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Attack_RushParts attack_Rush, RushAttackParts rushAttack, Attack_Rush_ExplodeParts attack_Rush_Explode, Attack_Rush_EndParts attack_Rush_End, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state1, rushAttack.graph2);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state2, rushAttack.graph5);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state3, rushAttack.graph7);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge, attack_Rush.graph1, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, attack_Rush_Explode.graph3, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge2, attack_Rush_Explode.graph4, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge3, attack_Rush_End.graph6, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge4, attack_Rush_End.graph8, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "bb92e55c-4056-69fd-25aa-353be1336431", "39ade2b9-4e24-a29e-abfb-a81e84db5277" }, new[] { "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush.graph1, new[] { "b939103e-8e47-3d2e-ad8f-39f34479975d", "2d98c02f-cc3a-7814-4bc1-7b199da0119d" }, new[] { "32a058e4-3e89-1366-edf0-31e3ff555bbd" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph2, new[] { "446e389c-c5c9-4f32-8214-84ef547b42d4", "2632c6ba-e2dc-f9df-7bd3-71336f057a46", "55b9f2ca-8bd1-2e45-b52f-33fed7e4aaaa", "3ec211ee-05cc-4ef4-a5fb-9024caae0dd6" }, new[] { "a63a8eab-4b5b-6407-3239-83270dbf9e41" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Explode.graph3, new[] { "b68ce95b-79e9-c12c-17ed-73c7105d4405", "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "1914af7e-8470-7b2c-1945-ec3689ec90f1", "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e" }, new[] { "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda", "6a4c65f0-062b-2154-92d8-87d2a68a20bb", "039e56d0-1b64-91ed-095b-2019f6cf8c7e", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Explode.graph4, new[] { "fb3bb13e-f4f3-65e2-234c-6848c7709a1c", "02412cb7-edc0-80ef-224d-487968aa253f" }, new[] { "6ada550c-6e77-a300-675b-1857c7dd1c6b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph5, new[] { "28e77a59-a8b1-470f-941e-7e5bca6cc467", "97c6a0c1-70cc-3aa3-8d48-90753d5ed1e6", "f9efcde8-1717-51f8-d479-c52979defca8", "2e807f11-7902-4560-8083-9ce15e6494f2" }, new[] { "e95cd5b2-a90a-6a1e-365b-6abbb3a3002b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph6, new[] { "790d6d88-47aa-183e-3b5b-2aea6eeb5528", "09177d2e-dc99-76e4-b7f7-9aedad98e9c9" }, new[] { "55db4864-553c-d3a1-74f7-721401df4dac" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph7, new[] { "720a1072-f4c3-446d-950c-3a37b8cf21c9", "8770dc67-3b72-74af-def8-085cbf69871f", "e21d43cf-55fc-293d-27e4-07ca61de6d99", "b0672745-af95-43e3-853b-cf5d62102d97" }, new[] { "b437e3fb-1bb1-3337-a2f9-a09206630264" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph8, new[] { "988af7b5-38ee-e0d0-0196-1811845991c9", "df2e016c-4ec6-02b6-c354-49e0386285b5" }, new[] { "df7a33bd-8f2d-9a7f-fcd4-13a9a2247d11" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "4b7150eb-050a-2929-3660-b52314306ed3", "1ff0e67d-fb55-144e-29a5-cd3187254d75", "ed6a3eba-d3f2-0d8d-c8c4-a874e9f5f731" }, new[] { "d1668a05-e3af-b894-ddfb-9e848b7ce014", "64c2c918-d608-8b33-5233-48f2aadde1af", "c9ccc511-57a1-4c24-0673-13688d712861", "242990af-270b-4fb3-efe6-cea7e6daa02c", "a7a9a9b4-bfe5-8bac-3dd3-2c8cad0d8df9" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Explode.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Explode.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph5, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_End.graph6, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph7, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_End.graph8, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, rootParts.graph);
        }

        sealed class RootParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeConnection stateEdge;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeConnection stateEdge1;
            internal BtsmtlSkillNativeConnection stateEdge2;
            internal BtsmtlSkillNativeState state2;
            internal BtsmtlSkillNativeConnection stateEdge3;
            internal BtsmtlSkillNativeState state3;
            internal BtsmtlSkillNativeConnection stateEdge4;
            internal GameplayEffectDefinition asset;
        }
    }
}
