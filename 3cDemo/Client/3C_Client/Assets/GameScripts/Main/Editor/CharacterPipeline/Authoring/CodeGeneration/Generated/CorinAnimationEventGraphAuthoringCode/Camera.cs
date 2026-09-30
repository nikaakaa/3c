using System.Linq;
using BTSMTL.EventGraphs;
using FlowCanvas;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ParadoxNotion;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.EventGraph
{
    public sealed partial class CorinAnimationEventGraphAuthoringCode
    {
        static void BuildCamera(CharacterAnimationEventGraph graph)
        {
            const string prefix = "corin.animation-event-graph.camera.";
            int edge = 0;
            FlowNode Add<T>(string id, int column, int row) where T : Node =>
                (FlowNode)graph.AddAuthoringNode(typeof(T), prefix + id, new Vector2(column * 270f, 3400f + row * 180f));
            void Connect(FlowNode from, string output, FlowNode to, string input) =>
                graph.ConnectAuthoringPorts(prefix + "edge." + edge++, from, output, to, input);
            void Value(FlowNode from, FlowNode to, string input) => Connect(from, "Value", to, input);
            FlowNode Equal(FlowNode state, string id, string name, int row)
            {
                var result = Add<SimplexNodeWrapper<EventGraphStringEqualNode>>(id, -3, row);
                Value(state, result, "a");
                graph.ConfigureValueInput(result, "b", name);
                return result;
            }
            FlowNode Or(FlowNode a, FlowNode b, string id, int column, int row)
            {
                var result = Add<SimplexNodeWrapper<OR>>(id, column, row);
                Value(a, result, "a"); Value(b, result, "b");
                return result;
            }
            FlowNode Input(string id, string inputId, int row)
            {
                var node = Add<EventGraphBoolInputNode>(id, -4, row);
                graph.ConfigureHostInput(node, inputId);
                return node;
            }
            FlowNode And(FlowNode a, FlowNode b, string id, int column, int row)
            {
                var result = Add<SimplexNodeWrapper<AND>>(id, column, row);
                Value(a, result, "a"); Value(b, result, "b");
                return result;
            }
            FlowNode Write(string variable, string name, FlowNode value, int row)
            {
                graph.DeclareVariable(variable, name, false);
                graph.ConfigureVariable(variable, true);
                var result = Add<SetVariable<bool>>(variable, 2, row);
                graph.BindVariableNode((ParameterVariableNode)result, variable);
                graph.ConfigureAssignment((SetVariable<bool>)result, AssignOp.Set, false);
                Value(value, result, "Value");
                return result;
            }
            var update = graph.allNodes.OfType<Split>().Single(node => node.UID == "corin.animation-event-graph.update-split");
            int updatePort = update.GetOutputFlowPorts().Count();
            graph.ConfigureInstantSplit(update, updatePort + 1);
            var state = Add<EventGraphStringInputNode>("control-state", -4, 0);
            graph.ConfigureHostInput(state, CharacterAnimationEventGraphHost.ControlStateInputPrefix +
                "control:character.corin.control:active-state");
            var idle = Equal(state, "idle", "Idle", 0);
            var walkStop = Equal(state, "walk-stop", "WalkStopping", -2);
            var runStop = Equal(state, "run-stop", "RunStopping", -1);
            idle = Or(idle, Or(walkStop, runStop, "stopping", -2, -1), "idle-or-stop", -1, 0);
            var walkStart = Equal(state, "walk-start", "WalkStart", 1);
            var walkLoop = Equal(state, "walk-loop", "WalkLoop", 2);
            var runLoop = Equal(state, "run-loop", "RunLoop", 3);
            var turn = Equal(state, "turn", "MovingTurn", 4);
            var walk = Or(walkStart, walkLoop, "walk", -2, 1);
            var run = Or(runLoop, turn, "run", -2, 3);
            var moving = Or(walk, run, "moving", -1, 2);
            var forward = Add<EventGraphBoolInputNode>("dodge-forward", -4, 5);
            graph.ConfigureHostInput(forward, CharacterAnimationEventGraphHost.AbilityActiveInputPrefix + "DodgeForward");
            var back = Add<EventGraphBoolInputNode>("dodge-back", -4, 6);
            graph.ConfigureHostInput(back, CharacterAnimationEventGraphHost.AbilityActiveInputPrefix + "DodgeBack");
            var evade = Or(forward, back, "evade", -2, 5);
            var attack = Input("attack", CharacterAnimationEventGraphHost.AbilityActiveInputPrefix + "Attack", 7);
            var rush = Input("rush", CharacterAnimationEventGraphHost.AbilityActiveInputPrefix + "RushAttack", 8);
            var branch = Input("branch", CharacterAnimationEventGraphHost.AbilityActiveInputPrefix + "BranchAttack", 9);
            var attacking = Or(attack, Or(rush, branch, "rush-or-branch", -3, 8), "attacking", -2, 7);
            var action = Or(attacking, evade, "action", -1, 7);
            var noAction = Add<SimplexNodeWrapper<NOT>>("no-action", 0, 7);
            Value(action, noAction, "value");
            idle = And(idle, noAction, "locomotion-idle", 0, 0);
            moving = And(moving, noAction, "locomotion-move", 0, 2);
            moving = Or(moving, forward, "move-or-forward-evade", 1, 2);
            var recoveryInputs = new[]
            {
                CharacterAnimationEventGraphHost.TimelineClipActiveInputPrefix + "10f4cb90-8b9a-4944-b77c-14efc9a3124d/7df37ded-43d4-4c6c-83a4-0227e64ccb8a",
                CharacterAnimationEventGraphHost.TimelineClipActiveInputPrefix + "21349b9d-8c58-4616-b8f3-6df7d560bb74/7a5caece-fc2f-4860-837c-755f36421dda",
                CharacterAnimationEventGraphHost.TimelineClipActiveInputPrefix + "c5761f3c-7517-4803-9e3b-019b66f52d41/2c0107ca-fde7-4043-8dd4-1939f2663551",
                CharacterAnimationEventGraphHost.TimelineClipActiveInputPrefix + "3a57e427-7c35-4910-99ac-68fca87b055e/2173d615-dd8c-4d5b-a550-58a798c0d69e",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "fafc1039-2302-5930-8a14-4039f95bc786",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "65c2a50d-80a7-57f0-b531-b2dabda051dd",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "8c852140-8ed0-ae2b-b3b3-654743e1e6ee",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "a8e1b908-009a-6920-2a28-13cb003dafe2",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "dd7314a9-4fcc-26c0-ca7c-1abc4fc6e7ce",
                CharacterAnimationEventGraphHost.TimelineActiveInputPrefix + "6b6bda6e-be52-5ca5-c54a-dccbedf7338e"
            };
            for (int i = 0; i < recoveryInputs.Length; i++)
                idle = Or(idle, Input("recovery-" + i, recoveryInputs[i], 10 + i), "idle-or-recovery-" + i, i - 3, 10 + i);
            var saveIdle = Write("camera.control.idle", "Camera Control Idle", idle, 0);
            var saveMove = Write("camera.control.move", "Camera Control Move", moving, 1);
            var saveEvade = Write("camera.control.evade", "Camera Control Evade", evade, 2);
            var hasCamera = Input("has-camera", CharacterAnimationEventGraphHost.HasCameraInput, -3);
            var cameraBranch = Add<SwitchBool>("camera-branch", 1, -3);
            Value(hasCamera, cameraBranch, "Condition");
            Connect(update, updatePort.ToString(System.Globalization.CultureInfo.InvariantCulture), cameraBranch, "In");
            Connect(cameraBranch, "True", saveIdle, "In");
            Connect(saveIdle, "Out", saveMove, "In");
            Connect(saveMove, "Out", saveEvade, "In");
        }
    }
}
