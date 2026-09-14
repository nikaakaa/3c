using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseCanvasNativePorts
    {
        sealed class PortRegistration
        {
            internal readonly Type Type;
            internal readonly Action<CharacterPoseCanvasNode, CharacterPosePortDefinition> Add;

            internal PortRegistration(
                Type type,
                Action<CharacterPoseCanvasNode, CharacterPosePortDefinition> add)
            {
                Type = type;
                Add = add;
            }
        }

        static readonly IReadOnlyDictionary<CharacterPosePortKind, PortRegistration> s_Types =
            new Dictionary<CharacterPosePortKind, PortRegistration>
            {
                [CharacterPosePortKind.LocalPose] = Registration<CharacterPoseNativeLocalPoseValue>(),
                [CharacterPosePortKind.ComponentPose] = Registration<CharacterPoseNativeComponentPoseValue>(),
                [CharacterPosePortKind.Parameter] = Registration<CharacterPoseNativeParameterValue>(),
                [CharacterPosePortKind.PoseDiscontinuity] = Registration<CharacterPoseNativeDiscontinuityValue>(),
                [CharacterPosePortKind.ActionPlayback] = Registration<CharacterPoseNativeActionPlaybackValue>(),
                [CharacterPosePortKind.FullBodyIkGoals] = Registration<CharacterPoseNativeFullBodyIkGoalsValue>(),
                [CharacterPosePortKind.FullBodyIkGoalContribution] = Registration<CharacterPoseNativeGoalContributionValue>(),
                [CharacterPosePortKind.PoseHistory] = Registration<CharacterPoseNativeHistoryValue>(),
                [CharacterPosePortKind.Trajectory] = Registration<CharacterPoseNativeTrajectoryValue>(),
                [CharacterPosePortKind.PresentationFacts] = Registration<CharacterPoseNativeFactsValue>(),
                [CharacterPosePortKind.MotionMatchingBinding] = Registration<CharacterPoseNativeMotionMatchingBindingValue>()
            };

        static PortRegistration Registration<T>() =>
            new PortRegistration(typeof(T), Add<T>);

        static PortRegistration Require(CharacterPosePortKind kind) =>
            s_Types.TryGetValue(kind, out PortRegistration registration)
                ? registration
                : throw new InvalidOperationException($"Unknown Pose port kind '{kind}'.");

        internal static void Register(CharacterPoseCanvasNode node)
        {
            foreach (CharacterPosePortDefinition port in Shape(node))
                Require(port.Kind).Add(node, port);
        }

        internal static Type BindingType(CharacterPoseCanvasConnection connection)
        {
            CharacterPosePortDefinition port = Shape(connection.SourceNode).Single(value =>
                value.Direction == CharacterPosePortDirection.Output &&
                value.PortId.Equals(connection.SourcePortId));
            return Require(port.Kind).Type;
        }

        internal static int Index(Port port, CharacterPosePortDirection direction)
        {
            CharacterPosePortDefinition[] ports = Shape((CharacterPoseCanvasNode)port.parent)
                .Where(value => value.Direction == direction)
                .ToArray();
            int index = Array.FindIndex(ports, value => value.PortId.Value == port.ID);
            return index >= 0
                ? index
                : throw new InvalidOperationException($"Pose port '{port.ID}' is not declared.");
        }

        static IReadOnlyList<CharacterPosePortDefinition> Shape(CharacterPoseCanvasNode node)
        {
#if UNITY_EDITOR
            if (PoseCanvasEditorBridge.PortShape != null)
                return PoseCanvasEditorBridge.PortShape(node);
#endif
            return RuntimeShape(node);
        }

        static IReadOnlyList<CharacterPosePortDefinition> RuntimeShape(
            CharacterPoseCanvasNode node)
        {
            var ports = new List<CharacterPosePortDefinition>();
            switch (node.Kind)
            {
                case CharacterPoseNodeKind.ProgramParameterInput:
                    ports.Add(Out("parameter", "Parameter", CharacterPosePortKind.Parameter));
                    break;
                case CharacterPoseNodeKind.ActionPlaybackInput:
                    ports.Add(Out("action-playback", "Action Playback", CharacterPosePortKind.ActionPlayback));
                    break;
                case CharacterPoseNodeKind.SelectedPosePlayer:
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.BlendSpacePlayer:
                    ports.Add(In("x", "X", CharacterPosePortKind.Parameter));
                    ports.Add(In("y", "Y", CharacterPosePortKind.Parameter, false));
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("discontinuity", "Discontinuity", CharacterPosePortKind.PoseDiscontinuity));
                    break;
                case CharacterPoseNodeKind.ClipPlayer:
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("discontinuity", "Discontinuity", CharacterPosePortKind.PoseDiscontinuity));
                    break;
                case CharacterPoseNodeKind.PoseStateMachine:
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.AnimationSlot:
                    ports.Add(In("source-pose", "Source Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.BlendStack:
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.Inertialization:
                    ports.Add(In("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("result", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.BlendPose:
                    AddBinaryLocalPose(ports, "Base", "Overlay");
                    break;
                case CharacterPoseNodeKind.LayeredBoneBlend:
                    AddBinaryLocalPose(ports, "Base", "Overlay");
                    break;
                case CharacterPoseNodeKind.AdditivePose:
                    AddBinaryLocalPose(ports, "Base", "Additive");
                    break;
                case CharacterPoseNodeKind.PoseParameterResolve:
                    ports.Add(In("base-pose", "Base Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(In("parameter-source-pose", "Parameter Source Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.ModifyBone:
                    AddUnaryComponentPose(ports);
                    break;
                case CharacterPoseNodeKind.RootOrientationWarp:
                    ports.Add(In("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("result", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.LocalToComponentPose:
                    ports.Add(In("local-pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("component-pose", "Component Pose", CharacterPosePortKind.ComponentPose));
                    break;
                case CharacterPoseNodeKind.ComponentToLocalPose:
                    ports.Add(In("component-pose", "Component Pose", CharacterPosePortKind.ComponentPose));
                    ports.Add(Out("local-pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.OutputPose:
                    ports.Add(In("pose", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.FootPlacement:
                    ports.Add(In("pose", "Component Pose", CharacterPosePortKind.ComponentPose));
                    ports.Add(In("weight", "Weight Override", CharacterPosePortKind.Parameter, false));
                    ports.Add(Out("contribution", "Goal Contribution", CharacterPosePortKind.FullBodyIkGoalContribution));
                    break;
                case CharacterPoseNodeKind.PoseBoneIKGoals:
                    ports.Add(In("pose", "Component Pose", CharacterPosePortKind.ComponentPose));
                    ports.Add(Out("contribution", "Goal Contribution", CharacterPosePortKind.FullBodyIkGoalContribution));
                    break;
                case CharacterPoseNodeKind.FullBodyIK:
                    ports.Add(In("pose", "Component Pose", CharacterPosePortKind.ComponentPose));
                    ports.Add(In("goals", "Full Body IK Goals", CharacterPosePortKind.FullBodyIkGoals, false));
                    ports.Add(Out("result", "Solved Component Pose", CharacterPosePortKind.ComponentPose));
                    break;
                case CharacterPoseNodeKind.FullBodyIkGoalAssembler:
                    ports.Add(Out("goals", "Full Body IK Goals", CharacterPosePortKind.FullBodyIkGoals));
                    break;
                case CharacterPoseNodeKind.PoseHistoryCollector:
                    ports.Add(In("pose.local.input", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("pose.local", "Local Pose", CharacterPosePortKind.LocalPose));
                    ports.Add(Out("history.pose", "Previous Pose History", CharacterPosePortKind.PoseHistory));
                    break;
                case CharacterPoseNodeKind.MotionMatchingPose:
                    ports.Add(In("history.pose", "Previous Pose History", CharacterPosePortKind.PoseHistory));
                    ports.Add(In("trajectory.query", "Trajectory", CharacterPosePortKind.Trajectory, false));
                    ports.Add(In("presentation.facts", "Presentation Facts", CharacterPosePortKind.PresentationFacts, false));
                    ports.Add(In("motion-matching.binding", "Binding", CharacterPosePortKind.MotionMatchingBinding, false));
                    ports.Add(Out("pose.local", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.EntryPoseInput:
                    ports.Add(Out("pose.local", "Local Pose", CharacterPosePortKind.LocalPose));
                    break;
                case CharacterPoseNodeKind.GraphInput:
                case CharacterPoseNodeKind.GraphOutput:
                case CharacterPoseNodeKind.PoseSubgraph:
                case CharacterPoseNodeKind.LinkedPoseCall:
                    break;
                default:
                    throw new InvalidOperationException($"Pose node kind '{node.Kind}' has no runtime port shape.");
            }
            ports.AddRange(node.DynamicPorts
                .OrderBy(value => value.Order)
                .Select(value => new CharacterPosePortDefinition(
                    value.PortId,
                    value.DisplayName,
                    value.Kind,
                    value.Direction,
                    value.Required,
                    value.InterfacePortId)));
            return ports;
        }

        static void AddBinaryLocalPose(
            List<CharacterPosePortDefinition> ports,
            string first,
            string second)
        {
            ports.Add(In("base", first + " Local Pose", CharacterPosePortKind.LocalPose));
            ports.Add(In("overlay", second + " Local Pose", CharacterPosePortKind.LocalPose));
            ports.Add(In("weight", "Weight", CharacterPosePortKind.Parameter, false));
            ports.Add(Out("result", "Local Pose", CharacterPosePortKind.LocalPose));
        }

        static void AddUnaryComponentPose(List<CharacterPosePortDefinition> ports)
        {
            ports.Add(In("pose", "Component Pose", CharacterPosePortKind.ComponentPose));
            ports.Add(In("weight", "Weight", CharacterPosePortKind.Parameter, false));
            ports.Add(Out("result", "Component Pose", CharacterPosePortKind.ComponentPose));
        }

        static CharacterPosePortDefinition In(
            string id,
            string name,
            CharacterPosePortKind kind,
            bool required = true) => new CharacterPosePortDefinition(
                new PosePortId(id),
                name,
                kind,
                CharacterPosePortDirection.Input,
                required);

        static CharacterPosePortDefinition Out(
            string id,
            string name,
            CharacterPosePortKind kind,
            bool required = true) => new CharacterPosePortDefinition(
                new PosePortId(id),
                name,
                kind,
                CharacterPosePortDirection.Output,
                required);

        static void Add<T>(CharacterPoseCanvasNode node, CharacterPosePortDefinition port)
        {
            if (port.Direction == CharacterPosePortDirection.Input)
                node.AddValueInput<T>(port.Name, port.PortId.Value);
            else
                node.AddValueOutput<T>(
                    port.Name,
                    () => node.ReadNativeOutput<T>(port.PortId),
                    port.PortId.Value);
        }
    }
}
