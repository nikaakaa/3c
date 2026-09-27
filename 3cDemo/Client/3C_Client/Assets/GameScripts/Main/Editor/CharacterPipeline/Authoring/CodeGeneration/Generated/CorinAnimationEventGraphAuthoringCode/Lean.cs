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
        static void BuildLean(CharacterAnimationEventGraph graph, Split update)
        {
            const string prefix = "corin.animation-event-graph.lean.";
            int edge = 0;
            FlowNode Add<T>(string id, int column, int row) where T : Node =>
                (FlowNode)graph.AddAuthoringNode(typeof(T), prefix + id, new Vector2(column * 270f, 1900f + row * 180f));
            FlowNode Pure<T>(string id, int column, int row) where T : SimplexNode =>
                Add<SimplexNodeWrapper<T>>(id, column, row);
            void Connect(FlowNode from, string output, FlowNode to, string input) =>
                graph.ConnectAuthoringPorts(prefix + "edge." + edge++, from, output, to, input);
            void Value(FlowNode from, FlowNode to, string input) => Connect(from, "Value", to, input);
            FlowNode Read<T>(string variable, string id, int column, int row)
            {
                var node = Add<GetVariable<T>>(id, column, row);
                graph.BindVariableNode((ParameterVariableNode)node, variable);
                return node;
            }
            FlowNode Write<T>(string variable, string id, int column, int row)
            {
                var node = Add<SetVariable<T>>(id, column, row);
                graph.BindVariableNode((ParameterVariableNode)node, variable);
                graph.ConfigureAssignment((SetVariable<T>)node, AssignOp.Set, false);
                return node;
            }
            void Declare<T>(string id, string name, T value)
            {
                graph.DeclareVariable(id, name, value);
                graph.ConfigureVariable(id, true);
            }
            Declare("animation.lean.max-angle", "Lean / 最大倾角（度，参考默认跑映射上限）", 16f);
            Declare("animation.lean.tilt-seconds", "Lean / 倾斜时间（秒，参考 dump）", 0.3f);
            Declare("animation.lean.recover-seconds", "Lean / 回正时间（秒，参考 dump）", 0.3f);
            Declare("animation.lean.full-turn-rate", "Lean / 满倾转速（度每秒，项目调参）", 180f);
            Declare("animation.lean.full-speed", "Lean / 满倾速度（米每秒，项目调参）", 6f);
            Declare("animation.lean.eligible", "Lean / 跑动适用", false);
            Declare("animation.lean.turn-rate", "Lean / 有符号转向速度", 0f);
            Declare("animation.lean.target-angle", "Lean / 目标倾角", 0f);
            Declare("animation.lean.angle", "Lean / 平滑倾角", 0f);
            Declare("animation.lean.rotation", "Lean / 输出旋转", Quaternion.identity);
            Declare("animation.lean.previous-direction", "Lean / 上帧运动方向", Vector2.zero);
            Declare("animation.lean.has-previous-direction", "Lean / 上帧方向有效", false);

            var mode = Add<EventGraphStringInputNode>("movement-mode", -5, 0);
            graph.ConfigureHostInput(mode, CharacterPresentationFactSchema.MovementMode.Value);
            var run = Pure<EventGraphStringEqualNode>("is-run", -4, 0);
            graph.ConfigureValueInput(run, "b", "presentation.movement-mode.state/RunLoop");
            Value(mode, run, "a");
            var grounded = Add<EventGraphBoolInputNode>("grounded", -5, 1);
            graph.ConfigureHostInput(grounded, CharacterPresentationFactSchema.Grounded.Value);
            var speed = Read<float>("animation.horizontal-speed", "speed", -5, 2);
            var moving = Pure<FloatGreaterThan>("has-direction", -4, 2);
            Value(speed, moving, "a");
            graph.ConfigureValueInput(moving, "b", 0.0001f);
            var groundRun = Pure<AND>("ground-run", -3, 0);
            Value(run, groundRun, "a"); Value(grounded, groundRun, "b");
            var eligible = Pure<AND>("eligible", -2, 0);
            Value(groundRun, eligible, "a"); Value(moving, eligible, "b");
            var saveEligible = Write<bool>("animation.lean.eligible", "save-eligible", 0, 0);
            Value(eligible, saveEligible, "Value");
            var currentDirection = Read<Vector2>("animation.movement-direction", "direction", -5, 3);
            var previousDirection = Read<Vector2>("animation.lean.previous-direction", "previous-direction", -5, 4);
            var previousValid = Read<bool>("animation.lean.has-previous-direction", "previous-valid", -3, 3);
            var angle = Pure<EventGraphVector2SignedAngleNode>("direction-angle", -4, 3);
            Value(previousDirection, angle, "from"); Value(currentDirection, angle, "to");
            var absAngle = Pure<EventGraphFloatAbsNode>("absolute-direction-angle", -3, 4);
            Value(angle, absAngle, "value");
            var unambiguous = Pure<FloatLessThan>("unambiguous-direction", -2, 4);
            Value(absAngle, unambiguous, "a"); graph.ConfigureValueInput(unambiguous, "b", 179.999f);
            var historyValid = Pure<AND>("history-valid", -2, 3);
            Value(previousValid, historyValid, "a"); Value(eligible, historyValid, "b");
            var turnValid = Pure<AND>("turn-valid", -1, 3);
            Value(historyValid, turnValid, "a"); Value(unambiguous, turnValid, "b");
            var delta = Add<EventGraphDeltaNode>("delta", -5, 5);
            var angularSpeed = Pure<FloatDivide>("angular-speed", -3, 5);
            Value(angle, angularSpeed, "a"); Connect(delta, "Delta", angularSpeed, "b");
            var validRate = Pure<EventGraphFloatSelectNode>("valid-turn-rate", -1, 4);
            Value(turnValid, validRate, "condition"); Value(angularSpeed, validRate, "whenTrue");
            graph.ConfigureValueInput(validRate, "whenFalse", 0f);
            var saveRate = Write<float>("animation.lean.turn-rate", "save-turn-rate", 1, 0);
            Value(validRate, saveRate, "Value");
            var rate = Read<float>("animation.lean.turn-rate", "turn-rate", 0, 2);
            var fullRate = Read<float>("animation.lean.full-turn-rate", "full-turn-rate", 0, 3);
            var maxAngle = Read<float>("animation.lean.max-angle", "max-angle", 0, 5);
            var fullSpeed = Read<float>("animation.lean.full-speed", "full-speed", 0, 6);
            var normalizedRate = Pure<FloatDivide>("normalized-turn-rate", 1, 2);
            Value(rate, normalizedRate, "a"); Value(fullRate, normalizedRate, "b");
            var clampedRate = Pure<EventGraphFloatClampNode>("clamped-turn-rate", 2, 2);
            Value(normalizedRate, clampedRate, "value"); graph.ConfigureValueInput(clampedRate, "minimum", -1f); graph.ConfigureValueInput(clampedRate, "maximum", 1f);
            var normalizedSpeed = Pure<FloatDivide>("normalized-speed", 1, 6);
            Value(speed, normalizedSpeed, "a"); Value(fullSpeed, normalizedSpeed, "b");
            var speedWeight = Pure<EventGraphFloatClampNode>("speed-weight", 2, 6);
            Value(normalizedSpeed, speedWeight, "value"); graph.ConfigureValueInput(speedWeight, "minimum", 0f); graph.ConfigureValueInput(speedWeight, "maximum", 1f);
            var weightedRate = Pure<FloatMultiply>("weighted-turn-rate", 3, 3);
            Value(clampedRate, weightedRate, "a"); Value(speedWeight, weightedRate, "b");
            var target = Pure<FloatMultiply>("target-angle", 4, 3);
            Value(weightedRate, target, "a"); Value(maxAngle, target, "b");
            var saveTarget = Write<float>("animation.lean.target-angle", "save-target", 2, 0);
            Value(target, saveTarget, "Value");

            var current = Read<float>("animation.lean.angle", "current-angle", 3, 7);
            var desired = Read<float>("animation.lean.target-angle", "desired-angle", 3, 8);
            var signProduct = Pure<FloatMultiply>("side-product", 4, 7);
            Value(current, signProduct, "a"); Value(desired, signProduct, "b");
            var opposite = Pure<FloatLessThan>("opposite-side", 5, 7);
            Value(signProduct, opposite, "a"); graph.ConfigureValueInput(opposite, "b", 0f);
            var stepTarget = Pure<EventGraphFloatSelectNode>("return-before-switch", 6, 7);
            Value(opposite, stepTarget, "condition"); graph.ConfigureValueInput(stepTarget, "whenTrue", 0f); Value(desired, stepTarget, "whenFalse");
            var absCurrent = Pure<EventGraphFloatAbsNode>("absolute-current", 4, 9);
            Value(current, absCurrent, "value");
            var absTarget = Pure<EventGraphFloatAbsNode>("absolute-target", 7, 7);
            Value(stepTarget, absTarget, "value");
            var increasing = Pure<FloatGreaterThan>("increasing-tilt", 8, 7);
            Value(absTarget, increasing, "a"); Value(absCurrent, increasing, "b");
            var tiltTime = Read<float>("animation.lean.tilt-seconds", "tilt-time", 6, 9);
            var recoverTime = Read<float>("animation.lean.recover-seconds", "recover-time", 6, 10);
            var duration = Pure<EventGraphFloatSelectNode>("response-time", 8, 9);
            Value(increasing, duration, "condition"); Value(tiltTime, duration, "whenTrue"); Value(recoverTime, duration, "whenFalse");
            var degreesPerSecond = Pure<FloatDivide>("degrees-per-second", 9, 9);
            Value(maxAngle, degreesPerSecond, "a"); Value(duration, degreesPerSecond, "b");
            var step = Pure<FloatMultiply>("angle-step", 10, 9);
            Value(degreesPerSecond, step, "a"); Connect(delta, "Delta", step, "b");
            var approach = Pure<EventGraphFloatMoveTowardsNode>("approach-angle", 11, 7);
            Value(current, approach, "current"); Value(stepTarget, approach, "target"); Value(step, approach, "maxDelta");
            var saveAngle = Write<float>("animation.lean.angle", "save-angle", 3, 0);
            Value(approach, saveAngle, "Value");
            var rotation = Pure<EventGraphQuaternionAngleAxisNode>("rotation", 4, 1);
            Value(current, rotation, "angle"); graph.ConfigureValueInput(rotation, "axis", Vector3.forward);
            var saveRotation = Write<Quaternion>("animation.lean.rotation", "save-rotation", 4, 0);
            Value(rotation, saveRotation, "Value");
            var saveDirection = Write<Vector2>("animation.lean.previous-direction", "save-direction", 5, 0);
            Value(currentDirection, saveDirection, "Value");
            var saveValid = Write<bool>("animation.lean.has-previous-direction", "save-valid", 6, 0);
            Value(eligible, saveValid, "Value");
            graph.ConfigureInstantSplit(update, 9);
            Connect(update, "8", saveEligible, "In");
            Connect(saveEligible, "Out", saveRate, "In");
            Connect(saveRate, "Out", saveTarget, "In");
            Connect(saveTarget, "Out", saveAngle, "In");
            Connect(saveAngle, "Out", saveRotation, "In");
            Connect(saveRotation, "Out", saveDirection, "In");
            Connect(saveDirection, "Out", saveValid, "In");
        }
    }
}
