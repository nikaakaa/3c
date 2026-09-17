using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeBackGameplayAbilityAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinDodgeAdmissionProfile.asset", 11400000L);
            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeBack", "5d6c5b40e613e36a79c647152961ea33", "DodgeBack");
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "37772f70-8c7f-4126-ac64-e32fcca47890", "技能入口", new Vector2(-354.6667f, 6f));
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), "c5122005-2d41-4e69-93e9-e6b33f6afc61", "DodgeBack StateMachine", new Vector2(260f, 6f));
            parts.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, "8d9a3eed-127b-4405-9e67-6f33d5d3d3bf", "DodgeBack StateMachine", "5d6c5b40e613e36a79c647152961ea33", "c5122005-2d41-4e69-93e9-e6b33f6afc61");
            var state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "00b9a83c-948d-4896-ac17-2a2404189e07", "状态机出口", new Vector2(521.3334f, 124.0001f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "1fa0f321-eba5-4e94-aa38-822d84de055e", "技能入口", new Vector2(-260f, 0f));
            var state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeAnyState), "6d7e8b1d-4e9c-4c84-a1e0-8d7f0e4c04c5", "任意状态", new Vector2(0f, 240f));
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "e376c542-fd18-4a79-bd6c-df249bc626e0", "DodgeBack", new Vector2(41.49386f, 35.21167f));
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "9e42a23e-40ab-4c4f-9680-92be8536ccfa");
            var stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state1, "027effc7-ec6b-453c-90a5-cedb9418730a");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)state2, "56b68cd3-0249-4f8f-9af2-06c451584b84");
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, DodgeBackParts dodgeBack, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state1, dodgeBack.graph1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, dodgeBack.graph4, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "37772f70-8c7f-4126-ac64-e32fcca47890", "c5122005-2d41-4e69-93e9-e6b33f6afc61" }, new[] { "9e42a23e-40ab-4c4f-9680-92be8536ccfa" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeBack.graph1, new[] { "e3379cf4-a2a0-4288-b190-d9952dcd30ed", "f02da1e1-1177-4116-b3ed-766e3251db81", "61586c6f-c735-4125-8875-41f1ff5f93c7", "e6673554-94b0-4f98-a52e-01158c43b92c" }, new[] { "1d4bfe96-f332-4168-a910-fecb32dd4a43" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeBack.graph2, new[] { "3d9b2d59-0fee-42f8-8e53-246b538ecf33", "f46d6c90-8f68-45ff-baab-be044b379d59", "c46a2985-824e-4035-8374-efd6ef392c96", "fb5e08cb-e551-4c3d-9b1b-2a36e5b32b39", "b3e23c3c-2686-43af-b85b-066bfed036c3" }, new[] { "51e60cb8-d436-465c-b8cf-4ca33e8ee920" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeBack.graph3, new[] { "d52c4af5-4427-4a97-8234-518f2c15d57a", "2c4bb2f9-dc4a-4d9e-98b5-835b1f42c0e5", "14ae0b1b-d797-423a-ae5f-4e48a32b5ff1", "bac2516e-ac8f-45a4-8e6d-da36c659c790", "f6e4ecf3-e1b7-4e61-95da-c0abb154fa94" }, new[] { "8f725471-ff0c-4f15-a97e-4874a314760d" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeBack.graph4, new[] { "b4b30de0-f2a9-4145-8a74-376a9fcb4ca8", "12d56e7c-5437-4a5f-bed0-c0c5c22ccb75" }, new[] { "797b1212-00ef-4290-9fb4-17258e039be7" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "e376c542-fd18-4a79-bd6c-df249bc626e0" }, new[] { "027effc7-ec6b-453c-90a5-cedb9418730a", "56b68cd3-0249-4f8f-9af2-06c451584b84" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeBack.graph1, new[] { "066d593b6cd64cdb9de608744b830ca3", "131816a77d7344d7b03d11917cb9c75d" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeBack.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeBack.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeBack.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(dodgeBack.timelineData, new[] { "82f04395-f39f-487a-8112-e45882a37deb", "8f2a9050-893a-41e1-be05-57312ab21153", "a636f440-208c-47db-88c1-afe9792423df" }, new[] { "b0ee4319-922b-410e-8c0c-b4fe70eb7504", "ca9b83e7-d1c4-4224-b62d-a51b4a4b9e0d", "180ec5ff-8aca-43b2-93aa-32d2354b4851", "b88aeaf5-d4a4-4a9d-a706-d6da6cb8072a" }, Array.Empty<string>(), Array.Empty<string>());
            BtsmtlSkillAuthoringCode.BindAbilityRoot(context, rootParts.graph);
        }

        sealed class RootParts
        {
            internal BtsmtlSkillFlowGraph graph;
            internal BtsmtlSkillNativeStateMachine stateMachine;
            internal BtsmtlSkillNativeState state1;
            internal BtsmtlSkillNativeConnection stateEdge1;
        }
    }
}
