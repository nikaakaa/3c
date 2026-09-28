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
            parts.state5 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "0be1a129-b4d5-cea1-0cd8-849e1e5cb91e", "Attack_Rush_Enhance", new Vector2(600f, 420f));
            var state4 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "1846cf39-ab38-7848-ce4e-be693d20f698", "状态机出口", new Vector2(1560f, 0f));
            parts.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "1ff0e67d-fb55-144e-29a5-cd3187254d75", "Attack_Rush_Explode", new Vector2(270f, 420f));
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "4b7150eb-050a-2929-3660-b52314306ed3", "Attack_Rush", new Vector2(120f, 220f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "8256282c-8945-45b3-f33a-5c394daa6ef6", "状态机入口", new Vector2(-320f, 0f));
            parts.state6 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "934f6ef3-060f-2f88-a3a6-47ee56b62fa7", "Attack_Rush_Enhance_Loop", new Vector2(780f, 420f));
            var state10 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeAnyState), "b3a1e60a-b751-4cd0-a8bc-f19912dc28d8", null, new Vector2(60f, 493.3333f));
            parts.state7 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "b5183f75-f8cd-c51b-063e-ff48fe19df88", "Attack_Rush_Enhance_Explode", new Vector2(960f, 620f));
            parts.state8 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "d2e92087-62e7-9470-7463-310f69990cb8", "Attack_Rush_Enhance_Explode_End", new Vector2(1200f, 620f));
            parts.state9 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "dd0ed881-b81c-95ee-d717-889c03a67b26", "Attack_Rush_Enhance_End", new Vector2(960f, 220f));
            parts.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "ed6a3eba-d3f2-0d8d-c8c4-a874e9f5f731", "Attack_Rush_End", new Vector2(420f, 220f));
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RushMoveExit", "RushMovement");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset3, new[] { parts.asset, asset1, asset2 }, new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Attack", true, "ActionTarget", "", false);
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state5, "44d550db-5946-4042-78bf-195efbab85dd");
            parts.stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state1, "d1668a05-e3af-b894-ddfb-9e848b7ce014");
            parts.stateEdge2 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state2, "64c2c918-d608-8b33-5233-48f2aadde1af");
            parts.stateEdge3 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state2, "c9ccc511-57a1-4c24-0673-13688d712861");
            parts.stateEdge4 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state2, (BtsmtlSkillNativeState)parts.state3, "242990af-270b-4fb3-efe6-cea7e6daa02c");
            parts.stateEdge5 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state3, (BtsmtlSkillNativeState)state4, "a7a9a9b4-bfe5-8bac-3dd3-2c8cad0d8df9");
            parts.stateEdge6 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state5, (BtsmtlSkillNativeState)parts.state6, "e6eca648-e1ed-fa41-c464-11ae5c87204d");
            parts.stateEdge7 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state6, (BtsmtlSkillNativeState)parts.state7, "44e2d93f-9035-7bfa-e086-132ce39bf9a3");
            parts.stateEdge8 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state6, (BtsmtlSkillNativeState)parts.state9, "eb363ff1-9960-0c6e-5be9-fd06e60e1961");
            parts.stateEdge9 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state7, (BtsmtlSkillNativeState)parts.state8, "83e6ab98-074a-bbf6-0e58-f36c79752f98");
            parts.stateEdge10 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state8, (BtsmtlSkillNativeState)state4, "ce8ad170-928e-2b10-4739-79077bfb09cc");
            parts.stateEdge11 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state9, (BtsmtlSkillNativeState)parts.state3, "a806bfff-3407-e384-94ea-f6f00dceb48c");
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, Attack_RushParts attack_Rush, Attack_Rush_EnhanceParts attack_Rush_Enhance, RushAttackParts rushAttack, Attack_Rush_ExplodeParts attack_Rush_Explode, Attack_Rush_EndParts attack_Rush_End, Attack_Rush_Enhance_LoopParts attack_Rush_Enhance_Loop, Attack_Rush_Enhance_ExplodeParts attack_Rush_Enhance_Explode, Attack_Rush_Enhance_EndParts attack_Rush_Enhance_End, Attack_Rush_Enhance_Explode_EndParts attack_Rush_Enhance_Explode_End, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state1, rushAttack.graph3);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state2, rushAttack.graph6);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state3, rushAttack.graph8);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state5, rushAttack.graph10);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state6, rushAttack.graph12);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state7, rushAttack.graph15);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state8, rushAttack.graph17);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state9, rushAttack.graph19);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, attack_Rush_Enhance.graph2, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge, attack_Rush.graph1, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge2, attack_Rush_Explode.graph4, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge3, attack_Rush_Explode.sawGraph, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge4, attack_Rush_End.graph7, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge5, attack_Rush_End.graph9, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge6, attack_Rush_Enhance_Loop.graph11, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge7, attack_Rush_Enhance_Explode.graph13, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge8, attack_Rush_Enhance_End.graph14, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge9, attack_Rush_Enhance_Explode_End.graph16, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge10, attack_Rush_Enhance_Explode_End.graph18, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge11, attack_Rush_End.graph20, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "bb92e55c-4056-69fd-25aa-353be1336431", "39ade2b9-4e24-a29e-abfb-a81e84db5277" }, new[] { "af4c2a98-eee8-05c5-2abf-0c7134d5c2bc" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush.graph1, new[] { "daf2ebca-8e14-4635-4420-1942458dd100", "942775c7-e01c-23e8-d556-ec49028886cf", "9e6027b3-7a1e-c33b-a472-a74aff3b3f36" }, new[] { "2b118f75-e8ec-dd4d-10dc-a0e672bbb19b", "c9389271-d008-f330-4b2a-8d841cf84326" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance.graph2, new[] { "f9328591-ddce-9d80-372b-9517ce860503", "f60686e3-816c-599b-aa44-19ef7445b059" }, new[] { "dd8316df-30a6-dfff-5f45-eba5d84d7d1c" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph3, new[] { "446e389c-c5c9-4f32-8214-84ef547b42d4", "2632c6ba-e2dc-f9df-7bd3-71336f057a46", "55b9f2ca-8bd1-2e45-b52f-33fed7e4aaaa", "3ec211ee-05cc-4ef4-a5fb-9024caae0dd6" }, new[] { "a63a8eab-4b5b-6407-3239-83270dbf9e41" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Explode.graph4, new[] { "b68ce95b-79e9-c12c-17ed-73c7105d4405", "bcf27c63-c7dd-6f20-8e3a-6d0ced1f17c8", "f8591ee3-7002-f816-c60e-39f0a20a6c1b", "5ab8a6c4-1171-4c70-b69d-e78a3b74e41d", "ccdc4afe-9a1a-c18a-f109-3c6b1aa8534e" }, new[] { "c6c5c063-cad9-40c7-82dc-a2fd35fbaeda", "6a4c65f0-062b-2154-92d8-87d2a68a20bb", "034c36c2-53ed-4e99-b8d6-dc57d4d5208e", "85c2dfc3-df48-aa7e-8462-c2b1c0bd50c8" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph6, new[] { "28e77a59-a8b1-470f-941e-7e5bca6cc467", "97c6a0c1-70cc-3aa3-8d48-90753d5ed1e6", "f9efcde8-1717-51f8-d479-c52979defca8", "2e807f11-7902-4560-8083-9ce15e6494f2" }, new[] { "e95cd5b2-a90a-6a1e-365b-6abbb3a3002b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph7, new[] { "790d6d88-47aa-183e-3b5b-2aea6eeb5528", "09177d2e-dc99-76e4-b7f7-9aedad98e9c9" }, new[] { "55db4864-553c-d3a1-74f7-721401df4dac" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph8, new[] { "720a1072-f4c3-446d-950c-3a37b8cf21c9", "8770dc67-3b72-74af-def8-085cbf69871f", "e21d43cf-55fc-293d-27e4-07ca61de6d99", "b0672745-af95-43e3-853b-cf5d62102d97" }, new[] { "b437e3fb-1bb1-3337-a2f9-a09206630264" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph9, new[] { "988af7b5-38ee-e0d0-0196-1811845991c9", "df2e016c-4ec6-02b6-c354-49e0386285b5" }, new[] { "df7a33bd-8f2d-9a7f-fcd4-13a9a2247d11" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph10, new[] { "ff816c21-8c1a-4cb6-80ff-87f349aee315", "912756a8-c75a-d57e-4273-e85519e5de87", "003a1591-39c8-19a7-d890-316afb0632d8", "d6218a38-e97a-441d-89ca-fca6f0f160a0" }, new[] { "07fa5309-0d3f-232b-c55c-09ac82532597" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance_Loop.graph11, new[] { "6610fe2f-917f-a764-3dd1-be40634024dc", "0cb833c2-eeb7-d987-2fb0-80485d40f69a" }, new[] { "1683a105-e09d-bdd2-93c2-02896fece37d" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph12, new[] { "75a54ebb-66cd-493f-a4dd-5103fbba3cba", "7212ba26-f915-8db3-3c3e-b4c5fa4c0c66", "f95825eb-a126-dad4-6d31-5157e5fb8278", "23993e8e-a387-4988-8ce4-7ed38c1cccce" }, new[] { "fb4091d1-a81e-57f7-4df9-35b383adc91d" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance_Explode.graph13, new[] { "62699490-8e09-2d2b-9784-8385a84d553b", "40af559c-8a92-e7e9-2b08-48b0a64667ac" }, new[] { "6ed1e803-9f89-3ca3-1353-2db4a28d0b18" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance_End.graph14, new[] { "80706ada-c9b7-68c1-684e-60d57794aefd", "35a19981-5de3-c3ee-8669-1d81a7b2caff", "017bf1ae-b901-cb3d-d981-09176016ef65" }, new[] { "7f7ac07f-fd2b-7445-83b1-5100162ffcce", "b87c1c72-fd18-057c-c366-c5872a6c270b" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph15, new[] { "85b0a724-c1ff-4f87-887f-0ed5d8f76045", "b69dbedb-2fd9-4eaf-174d-862cb9d43dd3", "dad3a6e3-da0b-a040-d0e2-c62cd44f7474", "f83a19e1-d20c-4547-811d-68d9a81567e8" }, new[] { "356d988e-8a1f-d5f4-5881-57982bc37e1a" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance_Explode_End.graph16, new[] { "0f4dbd22-86df-becf-57e2-4eddb3ff9013", "353dd2b8-6fce-6215-e31e-5f8d623a822a" }, new[] { "19e8d9f9-e887-8838-5086-8af6bc2a6f26" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph17, new[] { "179f6491-edd5-4d97-9a25-8b8ade5f5fd5", "b28cd39b-0d43-49ad-7627-021f500e953e", "4c1c23cf-e4ea-ab46-1fc6-d9f98482e9b1", "3a56d26b-4da8-49d3-a7d4-6b3282c3b9aa" }, new[] { "5e678128-1b8e-db0d-9f81-f08d6e58fa53" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_Enhance_Explode_End.graph18, new[] { "52d972d9-b1e8-3072-aa80-961ffce9036f", "9353e5c4-af3e-452f-7a8e-13d1cb60b1e9" }, new[] { "bdfc8c70-96f0-ccce-7bfb-db01e0d07c68" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rushAttack.graph19, new[] { "3b959670-4572-4249-9e5a-6826000f3528", "4a622d0c-608c-9358-00ce-857b572b5141", "9a7b4266-72bb-d1d6-1ef4-1bb5297c8e26", "d64dcab8-25f3-4577-b4f1-6b8afcdfe0b0" }, new[] { "6ecf6584-df68-ef7a-861d-6a7a7e2f3785" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(attack_Rush_End.graph20, new[] { "42c9da54-a38d-ab50-b580-66fc78a7bd54", "748cef4a-7de4-dd3a-9cd9-2bcecb1b29ec" }, new[] { "e0c5afd0-5c15-75c6-4dae-e8993ffddc94" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "4b7150eb-050a-2929-3660-b52314306ed3", "1ff0e67d-fb55-144e-29a5-cd3187254d75", "ed6a3eba-d3f2-0d8d-c8c4-a874e9f5f731", "0be1a129-b4d5-cea1-0cd8-849e1e5cb91e", "934f6ef3-060f-2f88-a3a6-47ee56b62fa7", "b5183f75-f8cd-c51b-063e-ff48fe19df88", "d2e92087-62e7-9470-7463-310f69990cb8", "dd0ed881-b81c-95ee-d717-889c03a67b26" }, new[] { "d1668a05-e3af-b894-ddfb-9e848b7ce014", "44d550db-5946-4042-78bf-195efbab85dd", "64c2c918-d608-8b33-5233-48f2aadde1af", "c9ccc511-57a1-4c24-0673-13688d712861", "242990af-270b-4fb3-efe6-cea7e6daa02c", "a7a9a9b4-bfe5-8bac-3dd3-2c8cad0d8df9", "e6eca648-e1ed-fa41-c464-11ae5c87204d", "44e2d93f-9035-7bfa-e086-132ce39bf9a3", "eb363ff1-9960-0c6e-5be9-fd06e60e1961", "83e6ab98-074a-bbf6-0e58-f36c79752f98", "ce8ad170-928e-2b10-4739-79077bfb09cc", "a806bfff-3407-e384-94ea-f6f00dceb48c" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Explode.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph6, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_End.graph7, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph8, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_End.graph9, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph10, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance_Loop.graph11, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph12, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance_Explode.graph13, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance_End.graph14, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph15, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance_Explode_End.graph16, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph17, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_Enhance_Explode_End.graph18, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(rushAttack.graph19, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(attack_Rush_End.graph20, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, rootParts.graph);
        }

        sealed class RootParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeConnection stateEdge;
            internal BtsmtlSkillNativeConnection stateEdge1;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeConnection stateEdge2;
            internal BtsmtlSkillNativeConnection stateEdge3;
            internal BtsmtlSkillNativeState state2;
            internal BtsmtlSkillNativeConnection stateEdge4;
            internal BtsmtlSkillNativeState state3;
            internal BtsmtlSkillNativeConnection stateEdge5;
            internal BtsmtlSkillNativeState state5;
            internal BtsmtlSkillNativeConnection stateEdge6;
            internal BtsmtlSkillNativeState state6;
            internal BtsmtlSkillNativeConnection stateEdge7;
            internal BtsmtlSkillNativeConnection stateEdge8;
            internal BtsmtlSkillNativeState state7;
            internal BtsmtlSkillNativeConnection stateEdge9;
            internal BtsmtlSkillNativeState state8;
            internal BtsmtlSkillNativeConnection stateEdge10;
            internal BtsmtlSkillNativeState state9;
            internal BtsmtlSkillNativeConnection stateEdge11;
            internal GameplayEffectDefinition asset;
        }
    }
}
