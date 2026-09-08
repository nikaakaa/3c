#if UNITY_EDITOR
using System;
using System.Linq;
using FlowCanvas;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseCanvasNativePorts
    {
        sealed class LocalPose { }
        sealed class ComponentPose { }
        sealed class Parameter { }
        sealed class Discontinuity { }
        sealed class ActionPlayback { }
        sealed class FullBodyIkGoals { }
        sealed class GoalContribution { }
        sealed class PoseHistory { }
        sealed class Trajectory { }
        sealed class PresentationFacts { }
        sealed class MotionMatchingBinding { }

        sealed class PortRegistration
        {
            internal readonly Type Type;
            internal readonly Action<CharacterPoseCanvasNode, CharacterPosePortDefinition> Add;
            internal PortRegistration(Type type, Action<CharacterPoseCanvasNode, CharacterPosePortDefinition> add)
            {
                Type = type;
                Add = add;
            }
        }

        static readonly System.Collections.Generic.IReadOnlyDictionary<CharacterPosePortKind, PortRegistration> s_Types =
            new System.Collections.Generic.Dictionary<CharacterPosePortKind, PortRegistration>
            {
                [CharacterPosePortKind.LocalPose] = Registration<LocalPose>(),
                [CharacterPosePortKind.ComponentPose] = Registration<ComponentPose>(),
                [CharacterPosePortKind.Parameter] = Registration<float>(),
                [CharacterPosePortKind.PoseDiscontinuity] = Registration<Discontinuity>(),
                [CharacterPosePortKind.ActionPlayback] = Registration<ActionPlayback>(),
                [CharacterPosePortKind.FullBodyIkGoals] = Registration<FullBodyIkGoals>(),
                [CharacterPosePortKind.FullBodyIkGoalContribution] = Registration<GoalContribution>(),
                [CharacterPosePortKind.PoseHistory] = Registration<PoseHistory>(),
                [CharacterPosePortKind.Trajectory] = Registration<Trajectory>(),
                [CharacterPosePortKind.PresentationFacts] = Registration<PresentationFacts>(),
                [CharacterPosePortKind.MotionMatchingBinding] = Registration<MotionMatchingBinding>()
            };

        static PortRegistration Registration<T>() => new PortRegistration(typeof(T), Add<T>);

        static PortRegistration Require(CharacterPosePortKind kind) =>
            s_Types.TryGetValue(kind, out PortRegistration registration) ? registration :
                throw new InvalidOperationException($"Unknown Pose port kind '{kind}'.");

        internal static void Register(CharacterPoseCanvasNode node)
        {
            foreach (CharacterPosePortDefinition port in Shape(node))
                Require(port.Kind).Add(node, port);
        }

        internal static Type BindingType(CharacterPoseCanvasConnection connection)
        {
            CharacterPosePortDefinition port = Shape(connection.SourceNode).Single(value =>
                value.Direction == CharacterPosePortDirection.Output && value.PortId.Equals(connection.SourcePortId));
            return Require(port.Kind).Type;
        }
        internal static int Index(Port port, CharacterPosePortDirection direction)
        {
            CharacterPosePortDefinition[] ports = Shape((CharacterPoseCanvasNode)port.parent)
                .Where(value => value.Direction == direction).ToArray();
            int index = Array.FindIndex(ports, value => value.PortId.Value == port.ID);
            return index >= 0 ? index : throw new InvalidOperationException($"Pose port '{port.ID}' is not declared.");
        }

        static System.Collections.Generic.IReadOnlyList<CharacterPosePortDefinition> Shape(CharacterPoseCanvasNode node) =>
            (PoseCanvasEditorBridge.PortShape ?? throw new InvalidOperationException("Pose authoring port schema is not registered."))(node);

        static void Add<T>(CharacterPoseCanvasNode node, CharacterPosePortDefinition port)
        {
            if (port.Direction == CharacterPosePortDirection.Input)
                node.AddValueInput<T>(port.Name, port.PortId.Value);
            else
                node.AddValueOutput<T>(port.Name, () => throw new InvalidOperationException(
                    "Pose authoring ports do not execute; inspect committed runtime diagnostics."), port.PortId.Value);
        }
    }
}
#endif
