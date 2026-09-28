using System;
using BTSMTL.Timeline;
using FlowCanvas.Nodes;
using ThirdPersonCamera;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinBranchAttackGameplayAbilityAuthoringCode
    {
        static string HoldId(string key) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity("Corin.BranchAttack.Hold." + key)).ToString("D");

        static HoldParts BuildHold(RootParts root, BtsmtlAuthoringGenerationContext context)
        {
            var state = BtsmtlSkillAuthoringCode.EnsureNativeState(root.stateMachine, typeof(BtsmtlSkillNativeState), HoldId("state"), "Hold", Vector2.zero);
            var body = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(root.graph, HoldId("body"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.StateBody, "Hold State Body");
            BtsmtlSkillAuthoringCode.ConfigureNativeState(state, body);
            var entry = BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillRootFlowNode), HoldId("root"), "持续攻击", new Vector2(0f, 200f));
            BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillStateOnEnterFlowNode), HoldId("enter"), "进入状态", new Vector2(0f, 0f));
            BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillStateOnExitFlowNode), HoldId("exit"), "退出状态", new Vector2(0f, 400f));
            var parallel = (BtsmtlSkillParallelFlowNode)BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillParallelFlowNode), HoldId("parallel"), "动作及持续镜头", new Vector2(240f, 200f));
            parallel.SetMode(BtsmtlSkillParallelMode.FirstChild);
            parallel.SetSteps(new[] { new BtsmtlSkillStepPort(HoldId("action-port"), "持续攻击"), new BtsmtlSkillStepPort(HoldId("camera-port"), "持续镜头") });
            var machineNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillStateMachineFlowNode), HoldId("machine-node"), "Start / Loop / Walk", new Vector2(520f, 120f));
            var machine = BtsmtlSkillAuthoringCode.EnsureStateMachine(body, HoldId("machine"), "Hold StateMachine", HoldId("body"), HoldId("machine-node"));
            BtsmtlSkillAuthoringContract.Apply(machineNode, new[] { new BtsmtlSkillAuthoringFieldValue("graphId", machine) });
            var cameraNode = BtsmtlSkillAuthoringCode.EnsureFlowNode(body, typeof(BtsmtlSkillTimelineFlowNode), HoldId("camera-node"), "保持推镜", new Vector2(520f, 300f));
            var timeline = BtsmtlSkillAuthoringCode.EnsureTimeline(body, HoldId("camera-timeline"), "Branch Hold Camera");
            var data = timeline.Data;
            var catalog = TimelineTreeContractComposition.Create();
            var track = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(CameraEffectTrack), HoldId("camera-track"), "Camera Effects", TimelineExecutionDomain.Presentation);
            var stretch = context.ResolveExternalAsset<CameraStretchAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamStretch_01.asset", 11400000L);
            var zoom = context.ResolveExternalAsset<CameraZoomAsset>("Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/Corin_Attack_Branch_02_CamZoom_01.asset", 11400000L);
            var stretchClip = BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track, HoldId("stretch"), 0m, null, 0.25m, 0m, 0m, 0m);
            var zoomClip = BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track, HoldId("zoom"), 0m, null, 0.25m, 0m, 0m, 0m);
            TimelineAuthoringPropertyContract.Apply(data, stretchClip, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, stretch) });
            TimelineAuthoringPropertyContract.Apply(data, zoomClip, new[] { new TimelineAuthoringPropertyValue("effect", TimelineAuthoringPropertyKind.Object, zoom) });
            BtsmtlSkillAuthoringCode.PruneTimeline(data, new[] { HoldId("camera-track") }, new[] { HoldId("stretch"), HoldId("zoom") }, Array.Empty<string>(), Array.Empty<string>());
            BtsmtlSkillAuthoringContract.Apply(cameraNode, new[] { new BtsmtlSkillAuthoringFieldValue("actionContext", null), new BtsmtlSkillAuthoringFieldValue("timelineId", timeline), new BtsmtlSkillAuthoringFieldValue("timelineOwnership", BtsmtlSkillTimelineOwnership.Private), new BtsmtlSkillAuthoringFieldValue("playbackMode", TimelinePlaybackMode.HoldLastFrame) });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(body, entry, "Output", parallel, "Input", HoldId("root-edge"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(body, parallel, HoldId("action-port"), machineNode, "Input", HoldId("action-edge"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(body, parallel, HoldId("camera-port"), cameraNode, "Input", HoldId("camera-edge"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(body, new[] { HoldId("root"), HoldId("enter"), HoldId("exit"), HoldId("parallel"), HoldId("machine-node"), HoldId("camera-node") }, new[] { HoldId("root-edge"), HoldId("action-edge"), HoldId("camera-edge") });
            BtsmtlSkillAuthoringCode.PruneBlackboard(body, Array.Empty<string>());
            return new HoldParts
            {
                machine = machine,
                state = state,
                entry = BtsmtlSkillAuthoringCode.EnsureNativeState(machine, typeof(BtsmtlSkillNativeEntryState), HoldId("inner-entry"), "Entry", new Vector2(-300f, 0f)),
                exit = BtsmtlSkillAuthoringCode.EnsureNativeState(machine, typeof(BtsmtlSkillNativeExitState), HoldId("inner-exit"), "Exit", new Vector2(900f, 0f))
            };
        }

        static BtsmtlSkillFlowGraph BuildHoldCompleted(RootParts root)
        {
            var completed = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(root.graph, HoldId("completed"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Hold Complete");
            var done = BtsmtlSkillAuthoringCode.EnsureFlowNode(completed, typeof(BtsmtlSkillStateRootCompletedFlowNode), HoldId("done"), "状态主体已完成", Vector2.zero);
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(completed, typeof(BtsmtlSkillConditionResultFlowNode), HoldId("result"), "条件结果", new Vector2(300f, 0f));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(completed, done, "m_Output", result, "m_Result", HoldId("completed-edge"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(completed, new[] { HoldId("done"), HoldId("result") }, new[] { HoldId("completed-edge") });
            BtsmtlSkillAuthoringCode.PruneBlackboard(completed, Array.Empty<string>());
            return completed;
        }

        static BtsmtlSkillFlowGraph BuildWalkRelease(RootParts root)
        {
            var graph = BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(root.graph, HoldId("walk-release"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.ConditionRule, "Walk Release");
            var held = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillBooleanInputFlowNode), HoldId("walk-held"), "BranchHeld", Vector2.zero);
            var not = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillNativeNodeWrapper<NOT>), HoldId("walk-not"), "NOT", new Vector2(240f, 0f));
            var result = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillConditionResultFlowNode), HoldId("walk-result"), "条件结果", new Vector2(480f, 0f));
            BtsmtlSkillAuthoringContract.Apply(held, new[] { new BtsmtlSkillAuthoringFieldValue("inputId", "BranchHeld"), new BtsmtlSkillAuthoringFieldValue("providerOwnerId", "asset:be650df85b1e49ab9d1cefc91c6cc809") });
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, held, "m_Output", not, "value", HoldId("walk-input-edge"));
            BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, not, "Value", result, "m_Result", HoldId("walk-result-edge"));
            BtsmtlSkillAuthoringCode.PruneFlowGraph(graph, new[] { HoldId("walk-held"), HoldId("walk-not"), HoldId("walk-result") }, new[] { HoldId("walk-input-edge"), HoldId("walk-result-edge") });
            BtsmtlSkillAuthoringCode.PruneBlackboard(graph, Array.Empty<string>());
            return graph;
        }

        sealed class HoldParts
        {
            internal BtsmtlSkillNativeStateMachine machine;
            internal BtsmtlSkillNativeState state;
            internal BtsmtlSkillNativeState entry;
            internal BtsmtlSkillNativeState exit;
        }
    }
}
