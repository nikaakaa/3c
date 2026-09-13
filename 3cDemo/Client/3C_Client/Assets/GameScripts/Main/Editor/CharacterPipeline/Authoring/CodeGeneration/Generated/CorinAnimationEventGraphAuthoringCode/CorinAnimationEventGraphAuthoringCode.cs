using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.EventGraphs;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public sealed partial class CorinAnimationEventGraphAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var generation = new GenerationState();
            BuildCreateRoot0(generation, context);
            BuildConfigureRoot0(generation, context);
            BuildBindRoot0(generation, context);
            BuildConnectRoot0(generation, context);
            return context.Complete(generation.eventGraph);
        }

        static void BuildCreateRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.eventGraph = EventGraphAuthoringCode.EnsureRoot<CharacterAnimationEventGraph>(context, "character.corin.animation-event-graph", "84c6547b918644d0b8504a8bae7c438b", "CorinAnimationEventGraph");
            generation.eventGraph.DeclareVariable<Boolean>("animation.history.has-previous-sample", "Has Previous Sample", false);
            generation.eventGraph.DeclareVariable<Vector2>("animation.history.previous-planar-velocity", "Previous Planar Velocity", new Vector2(0f, 0f));
            generation.eventGraph.DeclareVariable<CharacterPresentationMotionPhase>("animation.motion-phase", "Motion Phase", CharacterPresentationMotionPhase.GroundedStationary);
            generation.eventGraph.DeclareVariable<Single>("animation.facing-error", "Facing Error", 0f);
            generation.eventGraph.DeclareVariable<Single>("animation.horizontal-acceleration", "Horizontal Acceleration", 0f);
            generation.eventGraph.DeclareVariable<Vector2>("animation.desired-direction", "Desired Direction", new Vector2(0f, 0f));
            generation.eventGraph.DeclareVariable<Vector2>("animation.movement-direction", "Movement Direction", new Vector2(0f, 0f));
            generation.eventGraph.DeclareVariable<Single>("animation.vertical-speed", "Vertical Speed", 0f);
            generation.eventGraph.DeclareVariable<Single>("animation.horizontal-speed", "Horizontal Speed", 0f);
            generation.node = generation.eventGraph.AddAuthoringNode(typeof(StartEvent), "corin.animation-event-graph.start", new UnityEngine.Vector2(-1180f, -260f));
            generation.node1 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Vector2>), "corin.animation-event-graph.start.set-previous-planar", new UnityEngine.Vector2(-880f, -260f));
            generation.node2 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Boolean>), "corin.animation-event-graph.start.set-has-previous", new UnityEngine.Vector2(-620f, -260f));
            generation.node3 = generation.eventGraph.AddAuthoringNode(typeof(UpdateEvent), "corin.animation-event-graph.update", new UnityEngine.Vector2(-1180f, 80f));
            generation.node4 = generation.eventGraph.AddAuthoringNode(typeof(Split), "corin.animation-event-graph.update-split", new UnityEngine.Vector2(-920f, 80f));
            generation.node5 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Single>), "corin.animation-event-graph.set.horizontal-speed", new UnityEngine.Vector2(-348.6666f, -137.3333f));
            generation.node6 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Single>), "corin.animation-event-graph.set.vertical-speed", new UnityEngine.Vector2(-92f, -10.00003f));
            generation.node7 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Vector2>), "corin.animation-event-graph.set.movement-direction", new UnityEngine.Vector2(150f, 51.99997f));
            generation.node8 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Vector2>), "corin.animation-event-graph.set.desired-direction", new UnityEngine.Vector2(440f, 86.00006f));
            generation.node9 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Single>), "corin.animation-event-graph.set.facing-error", new UnityEngine.Vector2(695.3334f, 130.6667f));
            generation.node10 = generation.eventGraph.AddAuthoringNode(typeof(SwitchBool), "corin.animation-event-graph.branch.acceleration-history", new UnityEngine.Vector2(160f, 360f));
            generation.node11 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Single>), "corin.animation-event-graph.set.horizontal-acceleration", new UnityEngine.Vector2(420f, 300f));
            generation.node12 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Single>), "corin.animation-event-graph.set.horizontal-acceleration-zero", new UnityEngine.Vector2(420f, 440f));
            generation.node13 = generation.eventGraph.AddAuthoringNode(typeof(SwitchBool), "corin.animation-event-graph.branch.grounded", new UnityEngine.Vector2(160f, 620f));
            generation.node14 = generation.eventGraph.AddAuthoringNode(typeof(SwitchBool), "corin.animation-event-graph.branch.grounded-moving", new UnityEngine.Vector2(420f, 560f));
            generation.node15 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<CharacterPresentationMotionPhase>), "corin.animation-event-graph.set.motion-phase-grounded-moving", new UnityEngine.Vector2(680f, 500f));
            generation.node16 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<CharacterPresentationMotionPhase>), "corin.animation-event-graph.set.motion-phase-grounded-stationary", new UnityEngine.Vector2(680f, 620f));
            generation.node17 = generation.eventGraph.AddAuthoringNode(typeof(SwitchBool), "corin.animation-event-graph.branch.airborne-rising", new UnityEngine.Vector2(420f, 780f));
            generation.node18 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<CharacterPresentationMotionPhase>), "corin.animation-event-graph.set.motion-phase-airborne-rising", new UnityEngine.Vector2(680f, 740f));
            generation.node19 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<CharacterPresentationMotionPhase>), "corin.animation-event-graph.set.motion-phase-airborne-falling", new UnityEngine.Vector2(680f, 860f));
            generation.node20 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Vector2>), "corin.animation-event-graph.history.set-previous-planar", new UnityEngine.Vector2(420f, 1040f));
            generation.node21 = generation.eventGraph.AddAuthoringNode(typeof(SetVariable<Boolean>), "corin.animation-event-graph.history.set-has-previous", new UnityEngine.Vector2(680.6824f, 1040.667f));
            generation.node22 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphVector3InputNode), "corin.animation-event-graph.input.velocity", new UnityEngine.Vector2(-1180f, 420f));
            generation.node23 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector3PlanarNode>), "corin.animation-event-graph.calculate.velocity-planar", new UnityEngine.Vector2(-878.6666f, 420f));
            generation.node24 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2MagnitudeNode>), "corin.animation-event-graph.calculate.horizontal-speed", new UnityEngine.Vector2(-620f, 420f));
            generation.node25 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<FloatGreaterThan>), "corin.animation-event-graph.calculate.horizontal-moving-check", new UnityEngine.Vector2(-360f, 1400f));
            generation.node26 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<OR>), "corin.animation-event-graph.calculate.grounded-moving-check", new UnityEngine.Vector2(-80f, 1400f));
            generation.node27 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2NormalizeNode>), "corin.animation-event-graph.calculate.movement-direction", new UnityEngine.Vector2(-620f, 700f));
            generation.node28 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2SubtractNode>), "corin.animation-event-graph.calculate.planar-delta", new UnityEngine.Vector2(-360f, 1120f));
            generation.node29 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2MagnitudeNode>), "corin.animation-event-graph.calculate.planar-delta-magnitude", new UnityEngine.Vector2(-80f, 1120f));
            generation.node30 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<FloatDivide>), "corin.animation-event-graph.calculate.horizontal-acceleration", new UnityEngine.Vector2(200f, 1120f));
            generation.node31 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector3YNode>), "corin.animation-event-graph.calculate.vertical-speed", new UnityEngine.Vector2(-880f, 560f));
            generation.node32 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<FloatGreaterThan>), "corin.animation-event-graph.calculate.airborne-rising-check", new UnityEngine.Vector2(-360f, 1540f));
            generation.node33 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphQuaternionInputNode), "corin.animation-event-graph.input.rotation", new UnityEngine.Vector2(-1180f, 560f));
            generation.node34 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphQuaternionForwardNode>), "corin.animation-event-graph.calculate.forward", new UnityEngine.Vector2(-880f, 980f));
            generation.node35 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector3PlanarNode>), "corin.animation-event-graph.calculate.facing-planar", new UnityEngine.Vector2(-620f, 980f));
            generation.node36 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2NormalizeNode>), "corin.animation-event-graph.calculate.facing", new UnityEngine.Vector2(-360f, 980f));
            generation.node37 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2SignedAngleNode>), "corin.animation-event-graph.calculate.facing-error", new UnityEngine.Vector2(-80f, 980f));
            generation.node38 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphBoolInputNode), "corin.animation-event-graph.input.grounded", new UnityEngine.Vector2(-1180f, 700f));
            generation.node39 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphVector2InputNode), "corin.animation-event-graph.input.desired-planar-velocity", new UnityEngine.Vector2(-1180f, 840f));
            generation.node40 = generation.eventGraph.AddAuthoringNode(typeof(SimplexNodeWrapper<EventGraphVector2NormalizeNode>), "corin.animation-event-graph.calculate.desired-direction", new UnityEngine.Vector2(-620f, 840f));
            generation.node41 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphVector2InputNode), "corin.animation-event-graph.input.desired-facing", new UnityEngine.Vector2(-1180f, 980f));
            generation.node42 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphBoolInputNode), "corin.animation-event-graph.input.has-motion", new UnityEngine.Vector2(-1180f, 1120f));
            generation.node43 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphVector2InputNode), "corin.animation-event-graph.input.locomotion-planar-basis", new UnityEngine.Vector2(-1180f, 1260f));
            generation.node44 = generation.eventGraph.AddAuthoringNode(typeof(EventGraphDeltaNode), "corin.animation-event-graph.input.delta-seconds", new UnityEngine.Vector2(-1180f, 1400f));
            generation.node45 = generation.eventGraph.AddAuthoringNode(typeof(GetVariable<Vector2>), "corin.animation-event-graph.history.get-previous-planar", new UnityEngine.Vector2(-620f, 1120f));
            generation.node46 = generation.eventGraph.AddAuthoringNode(typeof(GetVariable<Boolean>), "corin.animation-event-graph.history.get-has-previous", new UnityEngine.Vector2(-620f, 1260f));
        }

        static void BuildConfigureRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.eventGraph.ConfigureCanvas("Animation Event Graph", "Corin动画事件图计算速度、方向、加速度、朝向误差和运动阶段，并维护实例历史。", new UnityEngine.Vector2(5f, 266f), 0.752373338f);
            generation.eventGraph.ConfigureVariable("animation.history.has-previous-sample", false);
            generation.eventGraph.ConfigureVariable("animation.history.previous-planar-velocity", false);
            generation.eventGraph.ConfigureVariable("animation.motion-phase", false);
            generation.eventGraph.ConfigureVariable("animation.facing-error", false);
            generation.eventGraph.ConfigureVariable("animation.horizontal-acceleration", false);
            generation.eventGraph.ConfigureVariable("animation.desired-direction", false);
            generation.eventGraph.ConfigureVariable("animation.movement-direction", false);
            generation.eventGraph.ConfigureVariable("animation.vertical-speed", false);
            generation.eventGraph.ConfigureVariable("animation.horizontal-speed", false);
            generation.eventGraph.ConfigureAssignment((SetVariable<UnityEngine.Vector2>)generation.node1, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<bool>)generation.node2, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureUpdateEvent((UpdateEvent)generation.node3);
            generation.eventGraph.ConfigureInstantSplit((Split)generation.node4, 8);
            generation.eventGraph.ConfigureAssignment((SetVariable<float>)generation.node5, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<float>)generation.node6, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<UnityEngine.Vector2>)generation.node7, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<UnityEngine.Vector2>)generation.node8, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<float>)generation.node9, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<float>)generation.node11, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<float>)generation.node12, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<UnityEngine.Vector2>)generation.node20, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureAssignment((SetVariable<bool>)generation.node21, ParadoxNotion.AssignOp.Set, false);
            generation.eventGraph.ConfigureHostInput(generation.node22, "presentation.velocity");
            generation.eventGraph.ConfigureHostInput(generation.node33, "presentation.rotation");
            generation.eventGraph.ConfigureHostInput(generation.node38, "presentation.grounded");
            generation.eventGraph.ConfigureHostInput(generation.node39, "presentation.desired-planar-velocity");
            generation.eventGraph.ConfigureHostInput(generation.node41, "presentation.desired-facing");
            generation.eventGraph.ConfigureHostInput(generation.node42, "presentation.has-motion");
            generation.eventGraph.ConfigureHostInput(generation.node43, "presentation.locomotion-planar-basis");
            generation.eventGraph.ConfigureHostInput(generation.node44, "host.delta-seconds");
        }

        static void BuildBindRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node1, "animation.history.previous-planar-velocity");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node2, "animation.history.has-previous-sample");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node5, "animation.horizontal-speed");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node6, "animation.vertical-speed");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node7, "animation.movement-direction");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node8, "animation.desired-direction");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node9, "animation.facing-error");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node11, "animation.horizontal-acceleration");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node12, "animation.horizontal-acceleration");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node15, "animation.motion-phase");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node16, "animation.motion-phase");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node18, "animation.motion-phase");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node19, "animation.motion-phase");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node20, "animation.history.previous-planar-velocity");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node21, "animation.history.has-previous-sample");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node45, "animation.history.previous-planar-velocity");
            generation.eventGraph.BindVariableNode((ParameterVariableNode)generation.node46, "animation.history.has-previous-sample");
        }

        static void BuildConnectRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.start-previous", generation.node, "Once", generation.node1, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.start-has-previous", generation.node1, "Out", generation.node2, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.update-split", generation.node3, "Out", generation.node4, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.speed", generation.node4, "0", generation.node5, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.vertical-speed", generation.node4, "1", generation.node6, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.movement-direction", generation.node4, "2", generation.node7, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.desired-direction", generation.node4, "3", generation.node8, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.facing-error", generation.node4, "4", generation.node9, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.acceleration-branch", generation.node4, "5", generation.node10, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.motion-phase-branch", generation.node4, "6", generation.node13, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.save-previous-planar", generation.node4, "7", generation.node20, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.acceleration-calculated", generation.node10, "True", generation.node11, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.acceleration-first-sample", generation.node10, "False", generation.node12, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.grounded-moving-branch", generation.node13, "True", generation.node14, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.airborne-branch", generation.node13, "False", generation.node17, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.grounded-moving", generation.node14, "True", generation.node15, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.grounded-stationary", generation.node14, "False", generation.node16, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.airborne-rising", generation.node17, "True", generation.node18, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.airborne-falling", generation.node17, "False", generation.node19, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.flow.save-has-previous", generation.node20, "Out", generation.node21, "In");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.velocity-planar", generation.node22, "Value", generation.node23, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.vertical-speed", generation.node22, "Value", generation.node31, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.horizontal-speed", generation.node23, "Value", generation.node24, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.movement-direction", generation.node23, "Value", generation.node27, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.current-planar", generation.node23, "Value", generation.node28, "a");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-previous-planar", generation.node23, "Value", generation.node20, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.horizontal-speed-check", generation.node24, "Value", generation.node25, "a");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-horizontal-speed", generation.node24, "Value", generation.node5, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.speed-motion-check", generation.node25, "Value", generation.node26, "b");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.grounded-moving-check", generation.node26, "Value", generation.node14, "Condition");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-movement-direction", generation.node27, "Value", generation.node7, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.planar-delta", generation.node28, "Value", generation.node29, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.acceleration-numerator", generation.node29, "Value", generation.node30, "a");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-horizontal-acceleration", generation.node30, "Value", generation.node11, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.vertical-rising-check", generation.node31, "Value", generation.node32, "a");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-vertical-speed", generation.node31, "Value", generation.node6, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.airborne-rising-check", generation.node32, "Value", generation.node17, "Condition");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.forward", generation.node33, "Value", generation.node34, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.facing-planar", generation.node34, "Value", generation.node35, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.facing", generation.node35, "Value", generation.node36, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.facing-error-from", generation.node36, "Value", generation.node37, "from");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-facing-error", generation.node37, "Value", generation.node9, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.grounded-check", generation.node38, "Value", generation.node13, "Condition");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.desired-direction", generation.node39, "Value", generation.node40, "value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.set-desired-direction", generation.node40, "Value", generation.node8, "Value");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.facing-error-to", generation.node41, "Value", generation.node37, "to");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.has-motion-check", generation.node42, "Value", generation.node26, "a");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.acceleration-delta", generation.node44, "Delta", generation.node30, "b");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.previous-planar", generation.node45, "Value", generation.node28, "b");
            generation.eventGraph.ConnectAuthoringPorts("corin.animation-event-graph.value.has-previous", generation.node46, "Value", generation.node10, "Condition");
        }

        sealed class GenerationState
        {
            internal CharacterAnimationEventGraph eventGraph;
            internal Node node;
            internal Node node1;
            internal Node node2;
            internal Node node3;
            internal Node node4;
            internal Node node5;
            internal Node node6;
            internal Node node7;
            internal Node node8;
            internal Node node9;
            internal Node node10;
            internal Node node11;
            internal Node node12;
            internal Node node13;
            internal Node node14;
            internal Node node15;
            internal Node node16;
            internal Node node17;
            internal Node node18;
            internal Node node19;
            internal Node node20;
            internal Node node21;
            internal Node node22;
            internal Node node23;
            internal Node node24;
            internal Node node25;
            internal Node node26;
            internal Node node27;
            internal Node node28;
            internal Node node29;
            internal Node node30;
            internal Node node31;
            internal Node node32;
            internal Node node33;
            internal Node node34;
            internal Node node35;
            internal Node node36;
            internal Node node37;
            internal Node node38;
            internal Node node39;
            internal Node node40;
            internal Node node41;
            internal Node node42;
            internal Node node43;
            internal Node node44;
            internal Node node45;
            internal Node node46;
        }
    }
}
