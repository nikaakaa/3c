using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinDodgeForwardGameplayAbilityAuthoringCode
    {

        static RootParts BuildRoot(BtsmtlAuthoringGenerationContext context)
        {
            var parts = new RootParts();
            var asset = context.ResolveExternalAsset<GameplayAbilityAdmissionProfile>("Assets/Configs/Character/Corin/Pipeline/Abilities/AdmissionProfiles/CorinDodgeAdmissionProfile.asset", 11400000L);
            parts.graph = BtsmtlSkillAuthoringCode.EnsureAbilityRoot(context, "DodgeForward", "12fd389b09dc017facc9250c3f9a4ff3", "DodgeForward");
            var node1 = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillStateMachineFlowNode), "47098806-a9db-43fb-894a-783c38491e8c", "DodgeForward StateMachine", new Vector2(260f, 84f));
            var node = BtsmtlSkillAuthoringCode.EnsureFlowNode(parts.graph, typeof(BtsmtlSkillRootFlowNode), "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "技能入口", new Vector2(-220.3514f, 84.09288f));
            parts.stateMachine = BtsmtlSkillAuthoringCode.EnsureStateMachine(parts.graph, "dc99afc8-813d-4957-a166-df4b28dc46a4", "DodgeForward StateMachine", "12fd389b09dc017facc9250c3f9a4ff3", "47098806-a9db-43fb-894a-783c38491e8c");
            var state2 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeExitState), "09a76e8e-93d6-4a57-8d21-77a635b07acd", "状态机出口", new Vector2(460f, 0f));
            var state3 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeAnyState), "0c87a3ad-304a-4230-9f8f-9bce249746bc", "任意状态", new Vector2(0f, 240f));
            parts.state1 = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeState), "c41e85f4-dacc-49e3-b1b0-bb33b502eba6", "DodgeForward", new Vector2(80f, 0f));
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(parts.stateMachine, typeof(BtsmtlSkillNativeEntryState), "fc180d6b-1f14-486d-9502-e2311d4fc23d", "技能入口", new Vector2(-260f, 0f));
            var endRule = new GameplayAbilityEndRule();
            endRule.Configure(GameplayAbilityEndTrigger.AbortRequested, ActionLifecycleTransitionType.Abort, "", "TreeAbort");
            var endRule1 = new GameplayAbilityEndRule();
            endRule1.Configure(GameplayAbilityEndTrigger.ExecutionCompleted, ActionLifecycleTransitionType.Complete, "", "TimelineCompleted");
            var endRule2 = new GameplayAbilityEndRule();
            endRule2.Configure(GameplayAbilityEndTrigger.InterruptRequested, ActionLifecycleTransitionType.Interrupt, "", "TreeInterrupt");
            var endRule3 = new GameplayAbilityEndRule();
            endRule3.Configure(GameplayAbilityEndTrigger.ActionWindowClosed, ActionLifecycleTransitionType.Cancel, "RecoveryOpen", "RecoveryCancel");
            BtsmtlSkillAuthoringCode.ConfigureAbility(context, "", Array.Empty<GameplayTagId>(), asset, Array.Empty<GameplayEffectDefinition>(), new[] { endRule, endRule1, endRule2, endRule3 }, Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>(), Array.Empty<string>(), "Dodge", true, "", "", false);
            BtsmtlSkillAuthoringContract.Apply(node1, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", parts.stateMachine) });
            var edge = BtsmtlSkillAuthoringCode.EnsureFlowConnection(parts.graph, node, "Output", node1, "Input", "14cb740f-de1e-4bef-8bd5-ec827da247a9");
            var stateEdge = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)state, (BtsmtlSkillNativeState)parts.state1, "ad7bfdb0-de13-46c0-8078-2e82fcc8a267");
            parts.stateEdge1 = BtsmtlSkillAuthoringCode.EnsureNativeConnection(parts.stateMachine, (BtsmtlSkillNativeState)parts.state1, (BtsmtlSkillNativeState)state2, "520afb0a-2306-43b7-9a5a-ae41126a5370");
            return parts;
        }

        static void FinalizeAuthoring(RootParts rootParts, DodgeForwardParts dodgeForward, BtsmtlAuthoringGenerationContext context)
        {
            BtsmtlSkillAuthoringCode.ConfigureNativeState((BtsmtlSkillNativeState)rootParts.state1, dodgeForward.graph1);
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, dodgeForward.graph4, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "47098806-a9db-43fb-894a-783c38491e8c" }, new[] { "14cb740f-de1e-4bef-8bd5-ec827da247a9" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph1, new[] { "555d035e-3251-417d-83ac-c0b9c5d72314", "a757928f-ae8b-489d-9f8e-bf586374e90a", "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "4a1648fa-c157-43c8-a5a0-272ee43a87aa" }, new[] { "1823df30-39a2-4088-9b6d-6e4f2a55cf53" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph2, new[] { "8c4daf5a-133b-49d7-bd36-04729d9346c7", "ee704955-b263-4e08-ac4f-d79c65aa2f19", "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "a5909657-eed5-4c74-9172-25f6ded0b8b7", "b0d239f7-57a2-426f-96d1-62a85034900f" }, new[] { "41bd0710-f8cf-48f3-bad7-29c7416b6098" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph3, new[] { "50a3ad51-d102-46c6-8f59-3556f8101df7", "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "b2552514-5758-45ae-9201-f7293a7478c2", "0b17a643-0e7a-45d5-8d97-238ace861568", "8855643c-4b97-411e-bca9-e19ae9542912" }, new[] { "124777d1-9bbf-4292-9ca7-4b89326a8ed4" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph4, new[] { "2eaafbfa-a850-4809-9fdd-e22b00780718", "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a" }, new[] { "308a15ae-b5f5-49ab-ac56-3c070dea8130" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "c41e85f4-dacc-49e3-b1b0-bb33b502eba6" }, new[] { "ad7bfdb0-de13-46c0-8078-2e82fcc8a267", "520afb0a-2306-43b7-9a5a-ae41126a5370" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph1, new[] { "4a7a79cd9fc14a8f807d3d9580548cc1", "d9ecf57b06514746a1eed7a5a7217a6b" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(dodgeForward.timelineData, new[] { "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a" }, new[] { "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", "913539bd-df57-4f13-b965-feafda823f36", "e9d429f1-595e-475a-b647-444a6e069c92", "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc" }, Array.Empty<string>(), Array.Empty<string>());
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
