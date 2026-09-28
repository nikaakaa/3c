using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            parts.asset = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_01_01.asset", 11400000L);
            var asset1 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_01_02.asset", 11400000L);
            var asset2 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_02_01.asset", 11400000L);
            var asset3 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_02_02_01.asset", 11400000L);
            var asset4 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_02_02_02.asset", 11400000L);
            var asset5 = context.ResolveExternalAsset<GameplayEffectDefinition>("Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties/Corin_Attack_Branch_02_AttackProperty_03.asset", 11400000L);
            var asset6 = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinBranchAdmissionProfile.asset", 11400000L);
            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "BranchAttack", "d1855f22-91dd-43e5-738f-4ccabc5606f5", "BranchAttack");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "161dca04-66b6-20aa-1a31-2458affda431", "技能入口", new Vector2(-360f, 0f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), "beb74dec-4552-c8e7-aa56-ef3335a1f9e7", "BranchAttack StateMachine", new Vector2(240f, 0f));
            parts.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, "94f9c009-2c28-ace7-1c42-ba0f8e9ba5fd", "BranchAttack StateMachine", "d1855f22-91dd-43e5-738f-4ccabc5606f5", "beb74dec-4552-c8e7-aa56-ef3335a1f9e7");
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "2ca9e5a5-0889-426e-8c20-d5108fc32767", "Start", new Vector2(0f, 0f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "4f90016f-9304-be29-76a8-8f4bca65b209", "Entry", new Vector2(-320f, 0f));
            parts.state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "7e2bb0a1-d298-8ef2-da8d-7f525b6d9f75", "End", new Vector2(1200f, 0f));
            parts.state6 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "8d987d48-dbfa-e391-df49-5825bfe74678", "Walk", new Vector2(600f, 0f));
            var state4 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "9be835b8-1874-0035-bea1-369fc2f6b622", "Exit", new Vector2(1500f, 0f));
            parts.state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "adb77609-5157-54af-01e2-6fcc78e85436", "Explode", new Vector2(900f, 0f));
            var state7 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeAnyState), "cef13446-f781-40f4-9e0a-f9589ba53645", null, new Vector2(80f, 460f));
            parts.state5 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "f17cd26a-29b1-9025-146b-800e04319a7f", "Loop", new Vector2(300f, 0f));
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset6, new[] { parts.asset, asset1, asset2, asset3, asset4, asset5 }, new[] { endRule, endRule1, endRule2 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Branch", true, "ActionTarget", "", false);
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "5e95652e-c784-bef7-7cff-b42497f25ac2");
            var stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state1, "beaeb642-3a96-c44e-929c-f4e108f9ed43");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state2, "46525b6a-143d-9c6a-8504-2c9ac97ab255");
            parts.stateEdge2 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)parts.state5, "9cdd9ef6-a2a2-3f2b-8287-cc68a5ebc037");
            parts.stateEdge3 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state2, (BtsmtlSkillNativeState)parts.state3, "471d0c01-4355-b5c6-507c-2e9c18495d5b");
            parts.stateEdge4 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state3, (BtsmtlSkillNativeState)state4, "997dae0e-196e-9968-aa75-aefeb2c84148");
            parts.stateEdge6 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state5, (BtsmtlSkillNativeState)parts.state6, "337bbd17-6450-4911-6bd9-e0b42c35526a");
            parts.stateEdge5 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state5, (BtsmtlSkillNativeState)parts.state2, "5240b3e6-f608-746d-bdde-c6230964fbd0");
            parts.stateEdge7 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state6, (BtsmtlSkillNativeState)parts.state2, "c3ed9e6a-e055-d9c2-28e7-0558d95747b9");
            parts.stateEdge8 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state6, (BtsmtlSkillNativeState)parts.state5, "faf5737e-7d61-7bdd-aeff-7f1281f0b7c4");
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, BranchAttackParts branchAttack, ExplodeParts explode, LoopParts loop, EndParts end, WalkParts walk, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state1, branchAttack.graph1);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state2, branchAttack.graph4);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state3, branchAttack.graph6);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state5, branchAttack.graph8);
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state6, branchAttack.graph11);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, explode.graph2, 100, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge2, loop.graph3, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge3, end.graph5, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge4, end.graph7, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge6, walk.graph10, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge5, explode.graph9, 100, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge7, explode.graph9, 100, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge8, loop.graph12, 0, ProgramAbortPolicy.None, 1);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "161dca04-66b6-20aa-1a31-2458affda431", "beb74dec-4552-c8e7-aa56-ef3335a1f9e7" }, new[] { "5e95652e-c784-bef7-7cff-b42497f25ac2" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branchAttack.graph1, new[] { "4ab87d0d-e5a7-4e50-e2df-544301e7f0fc", "95386920-cebd-2d3c-f221-a2f33bba5f76", "cf1b257e-1739-5c84-d043-c195b28a2eaa", "29fb343e-c67c-2f9c-d218-b1ad2df26835" }, new[] { "226c5af3-0b41-abcc-6ae8-157c0a389bae" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(explode.graph2, new[] { "03f3919f-1ae9-11dd-3bb6-1b317fd53755", "86908c72-bc7f-6759-9380-6045b8e17c75", "7a4e9072-0daf-449c-3623-85a81e4fa46a", "dda5eda1-c952-a2b3-fbe7-81f7889baf57", "e56fe5a0-3a42-074c-0550-8aa11971acb1" }, new[] { "6e0ae88a-496b-8b2d-d2ae-01c205807825", "14444ac7-47df-6187-7608-33c85ed67cc6", "ce0dfb9c-179c-6fda-bf67-0da72c42f1cb", "5167cf1e-05c8-d34e-28d8-3c6eddf0f1ab" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(loop.graph3, new[] { "9bb194e4-b643-fee7-b32d-246d6202d865", "dfb451d9-5b14-938f-0253-2df754df8b65" }, new[] { "98b2b443-0615-8c6a-6d3a-f5b81c1d78d3" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branchAttack.graph4, new[] { "0951c9ea-e5aa-f6a3-640b-e08fa7a6fac1", "8646c812-da6e-a522-354b-60f141efb11e", "255985ff-8069-fca6-6605-ae73735ce75e", "716e7788-28b8-d0db-77a4-f8955d3f800d" }, new[] { "2c6d744a-d773-05a8-d134-418406e3d4cf" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(end.graph5, new[] { "c14c347d-5d46-9afa-8328-042449d0ffac", "8b6167f3-dba9-4215-33a6-12a02c05cc15" }, new[] { "554c1966-3b23-5c06-85a1-d93e8b55c886" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branchAttack.graph6, new[] { "c59593cb-5b5f-9eb0-98aa-bee7b7db48ed", "9308ee5d-14d9-26a2-e137-2f99a1cde280", "f4681b0d-4709-c76f-2e56-b3a98d3c5b11", "3cd61447-d6a9-42ac-0a1d-7d15ef647bb5" }, new[] { "7cbcc729-974f-bdd0-556e-e666a072ef20" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(end.graph7, new[] { "960931a1-c06d-088f-8e6f-4c1d53cca96c", "39f055ab-f09a-4d45-18d1-bb92b0bfedcf" }, new[] { "141066df-9138-91bd-3c5b-c2441039b12e" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branchAttack.graph8, new[] { "a138d90c-ee37-801b-a065-4030bfaae9c4", "e7154d3b-e27f-f722-e04e-52ad6c85d128", "c5ad3a8c-a13e-1a37-ea35-97e58edeea2c", "43ba89d0-ddb2-8b41-95e5-cba7e67c5c0d" }, new[] { "0e634d85-e8ff-0d8b-5278-67310a4df75e" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(explode.graph9, new[] { "e903d461-7f42-01ae-0822-70eb70f8a75d", "d22eecb4-b437-7576-0b21-4bb28ad5c613", "14e58aaa-f7f4-de6e-ee9d-3b9808fe8f30" }, new[] { "d2f8057e-c7f8-afa5-d25a-f6648f40fc09", "d2610220-29d4-7f04-841e-4500f5164d19" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(walk.graph10, new[] { "607f4707-fa09-ede1-34fd-cb0673e8a8b2", "cc505aaa-fa75-6c4c-8a19-800ea845d82e", "e9a363c8-c22e-203f-01a7-391d4eb19d1e" }, new[] { "a5f6bcf4-d262-841e-17e8-98013080011a", "7fd3af54-12fc-4222-dc62-3a6929531082" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(branchAttack.graph11, new[] { "8d8e9bde-769d-7406-f256-5ce338bb39fe", "568bbb9f-a3e8-8ac4-1dd2-640635d10c29", "d3f48f1e-be9f-1cc4-7146-452193f95e76", "61549d03-9c96-e380-2df9-175723a111ca" }, new[] { "d7220402-f48c-b4c1-814d-6fc7fba13aad" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(loop.graph12, new[] { "401922e5-0787-ebd2-ae51-88acca8b7a29", "4f2908f7-a69e-c374-6ff5-294175de1bb5", "69f6f4f6-df03-7193-5979-e4ed08a292ae", "8762b35e-8d26-6af4-0a0a-b48d7a2e409f" }, new[] { "3673d22e-103c-634b-1402-d7a81895f71a", "a6224092-96f3-deb3-90b1-db663311e63b", "31760954-157f-7fb3-a6cc-e8822eb6776d" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "2ca9e5a5-0889-426e-8c20-d5108fc32767", "adb77609-5157-54af-01e2-6fcc78e85436", "7e2bb0a1-d298-8ef2-da8d-7f525b6d9f75", "f17cd26a-29b1-9025-146b-800e04319a7f", "8d987d48-dbfa-e391-df49-5825bfe74678" }, new[] { "beaeb642-3a96-c44e-929c-f4e108f9ed43", "46525b6a-143d-9c6a-8504-2c9ac97ab255", "9cdd9ef6-a2a2-3f2b-8287-cc68a5ebc037", "471d0c01-4355-b5c6-507c-2e9c18495d5b", "997dae0e-196e-9968-aa75-aefeb2c84148", "5240b3e6-f608-746d-bdde-c6230964fbd0", "337bbd17-6450-4911-6bd9-e0b42c35526a", "c3ed9e6a-e055-d9c2-28e7-0558d95747b9", "faf5737e-7d61-7bdd-aeff-7f1281f0b7c4" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(branchAttack.graph1, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(explode.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(loop.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(branchAttack.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(end.graph5, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(branchAttack.graph6, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(end.graph7, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(branchAttack.graph8, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(explode.graph9, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(walk.graph10, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(branchAttack.graph11, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(loop.graph12, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, rootParts.graph);
        }

        sealed class RootParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeConnection stateEdge1;
            internal BtsmtlSkillNativeConnection stateEdge2;
            internal BtsmtlSkillNativeState state2;
            internal BtsmtlSkillNativeConnection stateEdge3;
            internal BtsmtlSkillNativeState state3;
            internal BtsmtlSkillNativeConnection stateEdge4;
            internal BtsmtlSkillNativeState state5;
            internal BtsmtlSkillNativeConnection stateEdge5;
            internal BtsmtlSkillNativeConnection stateEdge6;
            internal BtsmtlSkillNativeState state6;
            internal BtsmtlSkillNativeConnection stateEdge7;
            internal BtsmtlSkillNativeConnection stateEdge8;
            internal GameplayEffectDefinition asset;
        }
    }
}
