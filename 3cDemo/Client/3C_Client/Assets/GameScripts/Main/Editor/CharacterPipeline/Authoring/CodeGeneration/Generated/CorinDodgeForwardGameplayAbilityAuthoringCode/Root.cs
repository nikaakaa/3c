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
            BtsmtlSkillAuthoringContract.ConfigureConnection(rootParts.stateEdge1, dodgeForward.graph6, 0, ProgramAbortPolicy.None, 0);
            BtsmtlSkillAuthoringCode.PruneFlowGraph(rootParts.graph, new[] { "a7c0040e-35e8-44d0-9de6-2df949cd76a2", "47098806-a9db-43fb-894a-783c38491e8c" }, new[] { "14cb740f-de1e-4bef-8bd5-ec827da247a9" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph1, new[] { "555d035e-3251-417d-83ac-c0b9c5d72314", "a757928f-ae8b-489d-9f8e-bf586374e90a", "c78616f5-3e17-4b1d-b6ec-7a200ebec941", "4a1648fa-c157-43c8-a5a0-272ee43a87aa" }, new[] { "1823df30-39a2-4088-9b6d-6e4f2a55cf53" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph2, new[] { "8c4daf5a-133b-49d7-bd36-04729d9346c7", "ee704955-b263-4e08-ac4f-d79c65aa2f19", "d2cb42aaf9fa46cda30a8227f9c431eb", "1ffc339cfc80443f99c1b3440e6382dd", "adeb7e88-3e33-4201-bd0e-92a00001eaf2", "a5909657-eed5-4c74-9172-25f6ded0b8b7", "b0d239f7-57a2-426f-96d1-62a85034900f" }, new[] { "07c227aa25aa434ca1dd5d7b3d8faf81", "bed077f00df14bd8b9610cfb9f7b7ca0", "ca4977d4fa6e43258d4c92b81127e112" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph3, new[] { "f8afa4a6bfd24deba6b47ff701ae3127", "258900970f6c4204b213b8a1676d84ce", "d9785381-0e21-4a4c-a1aa-a918eb99644d" }, new[] { "e214c21504e74f598aecf4eb85199d9f", "303077bb97854ee6b8c549061bc99138" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph4, new[] { "50a3ad51-d102-46c6-8f59-3556f8101df7", "b2376875-f4a0-4c2a-adaf-a6e4c0278a82", "ee9ad3b56f7e46cba0a163577c0abd18", "f9386ecb0b2c46d99d90d7bc24801f8f", "b2552514-5758-45ae-9201-f7293a7478c2", "0b17a643-0e7a-45d5-8d97-238ace861568", "8855643c-4b97-411e-bca9-e19ae9542912" }, new[] { "7890877386624486bedca0ca9b31a258", "c20a9591f4ef47cfb454db1b8fb90e97", "a4c0074828e34a248f1dc789c8ba349e" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph5, new[] { "3f4879384e4c483dac358a69edd5dde7", "2c16d1f9d4994b28a1760def26e567f4", "ae0d291d-cd16-422e-9e2e-d6df6f9a1a4a" }, new[] { "f5fb71f193504ab8b71932ca274c1bea", "dd2625a662174d599a2e812ea5001f00" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph6, new[] { "2eaafbfa-a850-4809-9fdd-e22b00780718", "3f7f717a-3ccb-4fca-a0b1-f2011e236a0a" }, new[] { "308a15ae-b5f5-49ab-ac56-3c070dea8130" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph7, new[] { "2f674f08-df32-4f91-990e-fa649fb575f3", "a857d034-5232-4fd7-b862-392f4bf7b481", "4b47ec21-2e99-4a55-aa52-ed3cd2f6a158", "97e22dff-29da-4ca9-b988-ac65c8a22951" }, new[] { "3c90d638-bf76-4f17-9f96-35270e8cc02b", "70ccc846-181f-435b-93b5-f2b34e6de9fa", "8db9bd6f-e8f0-4a51-a0bd-4e714ef449c9" });
            BtsmtlSkillAuthoringCode.PruneFlowGraph(dodgeForward.graph8, new[] { "305c199e-8041-4a93-9480-6478158ee98b", "91abbb22-f7b0-4ca5-8033-217d16956ba4", "7dbde062-cd4f-4947-b623-5b8f713e70a9" }, new[] { "02108c25-595d-44f0-9cd7-a7dec2ce5cbb", "d9e333a4-a954-44ff-81de-2a2e63bc8750" });
            BtsmtlSkillAuthoringCode.PruneNativeStateMachine(rootParts.stateMachine, new[] { "c41e85f4-dacc-49e3-b1b0-bb33b502eba6" }, new[] { "ad7bfdb0-de13-46c0-8078-2e82fcc8a267", "520afb0a-2306-43b7-9a5a-ae41126a5370" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(rootParts.graph, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph1, new[] { "4a7a79cd9fc14a8f807d3d9580548cc1", "d9ecf57b06514746a1eed7a5a7217a6b", "74848fd0-786e-4874-8ac7-9baa039f1363" });
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph2, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph3, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph4, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph5, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph6, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph7, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneBlackboard(dodgeForward.graph8, Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimeline(dodgeForward.timelineData, new[] { "1fabe64a-9df6-4bb3-a395-ebfa32d9874b", "37d5f20e-931b-c859-1243-f37083cf2954", "8e4b0f0d-829b-4818-9a8b-39d9246447ea", "a0c961a6-0179-4f4d-8b75-76fa3a8bcd0a", "6c30f307-83d1-4e59-8190-0a8374383fac" }, new[] { "9080e5fb-1e6c-414a-bd9f-94cab1cf7c5c", "913539bd-df57-4f13-b965-feafda823f36", "e9d429f1-595e-475a-b647-444a6e069c92", "ea9a052b-5407-4a22-b1c2-f5bbce6e31bc", "24fbe662-062a-4790-886e-1dcbb01abd4b" }, Array.Empty<string>(), Array.Empty<string>());
            BtsmtlSkillAuthoringCode.PruneTimelineMarkers(dodgeForward.timelineData, Array.Empty<string>());
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
