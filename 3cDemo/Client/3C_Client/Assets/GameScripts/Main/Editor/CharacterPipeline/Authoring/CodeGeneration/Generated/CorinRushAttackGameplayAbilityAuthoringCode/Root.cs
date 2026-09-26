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
            var admission = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinRushAdmissionProfile.asset", 11400000L);
            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "RushAttack", "9152ca92-afdd-f163-b9e3-404a8c2b96aa", "RushAttack");
            var machineNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), "39ade2b9-4e24-a29e-abfb-a81e84db5277", "RushAttack StateMachine", new Vector2(240f, 0f));
            var rootNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "bb92e55c-4056-69fd-25aa-353be1336431", "技能入口", new Vector2(-360f, 0f));
            parts.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, "4413153b-fb40-eabd-22e2-22ad05775e03", "RushAttack StateMachine", "9152ca92-afdd-f163-b9e3-404a8c2b96aa", "39ade2b9-4e24-a29e-abfb-a81e84db5277");

            var exit = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "1846cf39-ab38-7848-ce4e-be693d20f698", "状态机出口", new Vector2(1560f, 0f));
            var entry = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "8256282c-8945-45b3-f33a-5c394daa6ef6", "状态机入口", new Vector2(-320f, 0f));
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "4b7150eb-050a-2929-3660-b52314306ed3", "Attack_Rush", new Vector2(120f, 220f));
            parts.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "1ff0e67d-fb55-144e-29a5-cd3187254d75", "Attack_Rush_Explode", new Vector2(270f, 420f));
            parts.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "ed6a3eba-d3f2-0d8d-c8c4-a874e9f5f731", "Attack_Rush_End", new Vector2(420f, 220f));
            parts.enhanceStart = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), RushId("state.enhance-start"), "Attack_Rush_Enhance", new Vector2(600f, 420f));
            parts.enhanceLoop = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), RushId("state.enhance-loop"), "Attack_Rush_Enhance_Loop", new Vector2(780f, 420f));
            parts.enhanceEnd = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), RushId("state.enhance-end"), "Attack_Rush_Enhance_End", new Vector2(960f, 220f));
            parts.enhanceExplode = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), RushId("state.enhance-explode"), "Attack_Rush_Enhance_Explode", new Vector2(960f, 620f));
            parts.enhanceExplodeEnd = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), RushId("state.enhance-explode-end"), "Attack_Rush_Enhance_Explode_End", new Vector2(1200f, 620f));

            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var interruptRule = new GameplayAbilityEndRule();
            interruptRule.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var completeRule = new GameplayAbilityEndRule();
            completeRule.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            var movementRule = new GameplayAbilityEndRule();
            movementRule.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RushMoveExit", "RushMovement");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), admission, new[] { parts.asset, asset1, asset2 }, new[] { endRule, interruptRule, completeRule, movementRule }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Attack", true, "", "", false);
            BtsmtlSkillAuthoringContract.Apply(machineNode, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, rootNode, "Output", machineNode, "Input", "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc");

            parts.stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, entry, parts.state1, "d1668a05-e3af-b894-ddfb-9e848b7ce014");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.state1, parts.state2, "64c2c918-d608-8b33-5233-48f2aadde1af");
            parts.stateEdge2 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.state1, parts.state2, "c9ccc511-57a1-4c24-0673-13688d712861");
            parts.stateEdge3 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.state2, parts.state3, "242990af-270b-4fb3-efe6-cea7e6daa02c");
            parts.stateEdge4 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.state3, exit, "a7a9a9b4-bfe5-8bac-3dd3-2c8cad0d8df9");
            parts.enhanceEntryEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, entry, parts.enhanceStart, RushId("edge.entry-enhance"));
            parts.enhanceStartEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceStart, parts.enhanceLoop, RushId("edge.enhance-start-loop"));
            parts.enhanceLoopExplodeEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceLoop, parts.enhanceExplode, RushId("edge.enhance-loop-explode"));
            parts.enhanceLoopEndEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceLoop, parts.enhanceEnd, RushId("edge.enhance-loop-end"));
            parts.enhanceEndEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceEnd, exit, RushId("edge.enhance-end-exit"));
            parts.enhanceExplodeEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceExplode, parts.enhanceExplodeEnd, RushId("edge.enhance-explode-end"));
            parts.enhanceExplodeEndEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, parts.enhanceExplodeEnd, exit, RushId("edge.enhance-explode-end-exit"));
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, RushAttackParts rushAttack, RushEnhanceParts rushEnhance, Attack_Rush_ExplodeParts attack_Rush_Explode, Attack_Rush_EndParts attack_Rush_End, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state1, rushAttack.graph2);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state2, rushAttack.graph5);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.state3, rushAttack.graph7);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.enhanceStart, rushEnhance.bodyEnhance.Graph);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.enhanceLoop, rushEnhance.bodyLoop.Graph);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.enhanceEnd, rushEnhance.bodyEnd.Graph);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.enhanceExplode, rushEnhance.bodyExplode.Graph);
            BtsmtlSkillAuthoringCode.ConfigureNativeState(rootParts.enhanceExplodeEnd, rushEnhance.bodyExplodeEnd.Graph);

            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge, rushEnhance.noBadge.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, attack_Rush_Explode.graph3, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge2, attack_Rush_Explode.graph4, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge3, attack_Rush_End.graph6, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge4, attack_Rush_End.graph8, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceEntryEdge, rushEnhance.badge.Graph, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceStartEdge, rushEnhance.startComplete.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceLoopExplodeEdge, rushEnhance.sawExplode.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceLoopEndEdge, rushEnhance.loopReleased.Graph, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceEndEdge, rushEnhance.complete.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceExplodeEdge, rushEnhance.complete.Graph, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.enhanceExplodeEndEdge, rushEnhance.complete.Graph, 0, ProgramAbortPolicy.None, 0);

            rushEnhance.Prune();

            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "bb92e55c-4056-69fd-25aa-353be1336431", "39ade2b9-4e24-a29e-abfb-a81e84db5277" }, new[] { "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph2, new[] { "446e389c-c5c9-4f32-8214-84ef547b42d4", "2632c6ba-e2dc-f9df-7bd3-71336f057a46", "55b9f2ca-8bd1-2e45-b52f-33fed7e4aaaa", "3ec211ee-05cc-4ef4-a5fb-9024caae0dd6" }, new[] { "a63a8eab-4b5b-6407-3239-83270dbf9e41" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Explode.graph3, new[] { "b68ce95b-79e9-c12c-17ed-73c7105d4405", "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "25c4f4af-d551-413d-8438-ed8b695a56dc", "1ef2a06e-dded-4c95-9df5-9d2f78f5b160", "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e" }, new[] { "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda", "6a4c65f0-062b-2154-92d8-87d2a68a20bb", "034c36c2-53ed-4e99-b8d6-dc57d4d5208e", "039e56d0-1b64-91ed-095b-2019f6cf8c7e", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Explode.graph4, new[] { "fb3bb13e-f4f3-65e2-234c-6848c7709a1c", "02412cb7-edc0-80ef-224d-487968aa253f" }, new[] { "6ada550c-6e77-a300-675b-1857c7dd1c6b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph5, new[] { "28e77a59-a8b1-470f-941e-7e5bca6cc467", "97c6a0c1-70cc-3aa3-8d48-90753d5ed1e6", "f9efcde8-1717-51f8-d479-c52979defca8", "2e807f11-7902-4560-8083-9ce15e6494f2" }, new[] { "e95cd5b2-a90a-6a1e-365b-6abbb3a3002b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph6, new[] { "790d6d88-47aa-183e-3b5b-2aea6eeb5528", "09177d2e-dc99-76e4-b7f7-9aedad98e9c9" }, new[] { "55db4864-553c-d3a1-74f7-721401df4dac" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph7, new[] { "720a1072-f4c3-446d-950c-3a37b8cf21c9", "8770dc67-3b72-74af-def8-085cbf69871f", "e21d43cf-55fc-293d-27e4-07ca61de6d99", "b0672745-af95-43e3-853b-cf5d62102d97" }, new[] { "b437e3fb-1bb1-3337-a2f9-a09206630264" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph8, new[] { "988af7b5-38ee-e0d0-0196-1811845991c9", "df2e016c-4ec6-02b6-c354-49e0386285b5" }, new[] { "df7a33bd-8f2d-9a7f-fcd4-13a9a2247d11" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine,
                new[] { rootParts.state1.UID, rootParts.state2.UID, rootParts.state3.UID, rootParts.enhanceStart.UID, rootParts.enhanceLoop.UID, rootParts.enhanceEnd.UID, rootParts.enhanceExplode.UID, rootParts.enhanceExplodeEnd.UID },
                new[] { rootParts.stateEdge.UID, rootParts.stateEdge1.UID, rootParts.stateEdge2.UID, rootParts.stateEdge3.UID, rootParts.stateEdge4.UID, rootParts.enhanceEntryEdge.UID, rootParts.enhanceStartEdge.UID, rootParts.enhanceLoopExplodeEdge.UID, rootParts.enhanceLoopEndEdge.UID, rootParts.enhanceEndEdge.UID, rootParts.enhanceExplodeEdge.UID, rootParts.enhanceExplodeEndEdge.UID });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, rootParts.graph);
        }

        sealed class RootParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeState state2;
            internal BtsmtlSkillNativeState state3;
            internal BtsmtlSkillNativeConnection stateEdge;
            internal BtsmtlSkillNativeConnection stateEdge1;
            internal BtsmtlSkillNativeConnection stateEdge2;
            internal BtsmtlSkillNativeConnection stateEdge3;
            internal BtsmtlSkillNativeConnection stateEdge4;
            internal BtsmtlSkillNativeState enhanceStart;
            internal BtsmtlSkillNativeState enhanceLoop;
            internal BtsmtlSkillNativeState enhanceEnd;
            internal BtsmtlSkillNativeState enhanceExplode;
            internal BtsmtlSkillNativeState enhanceExplodeEnd;
            internal BtsmtlSkillNativeConnection enhanceEntryEdge;
            internal BtsmtlSkillNativeConnection enhanceStartEdge;
            internal BtsmtlSkillNativeConnection enhanceLoopExplodeEdge;
            internal BtsmtlSkillNativeConnection enhanceLoopEndEdge;
            internal BtsmtlSkillNativeConnection enhanceEndEdge;
            internal BtsmtlSkillNativeConnection enhanceExplodeEdge;
            internal BtsmtlSkillNativeConnection enhanceExplodeEndEdge;
            internal GameplayEffectDefinition asset;
        }

        static string RushId(string seed) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity($"corin.rush.ability:{seed}")).ToString("D");
    }
}
