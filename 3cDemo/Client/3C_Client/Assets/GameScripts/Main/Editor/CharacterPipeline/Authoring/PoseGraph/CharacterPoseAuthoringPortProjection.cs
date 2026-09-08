using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseAuthoringPortProjection
    {
        public static IReadOnlyList<CharacterPosePortDefinition> Get(
            CharacterPoseCanvasNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            return CharacterPoseNodeDefinitionModule.Shared
                .Require(node.Kind)
                .ProjectPortShape(node)
                .Select(ToPosePort)
                .ToArray();
        }

        public static IReadOnlyList<CharacterPosePortDefinition>
            GetDeclared(CharacterPoseCanvasNode node) =>
            CharacterPoseNodeDefinitionModule.Shared
                .Require(node.Kind)
                .ProjectDeclaredPortShape(node.Payload)
                .Select(ToPosePort)
                .ToArray();

        public static CharacterPosePortDefinition Require(
            CharacterPoseCanvasNode node,
            string portId,
            CharacterPosePortDirection? direction = null)
        {
            CharacterPosePortDefinition port = Get(node)
                .SingleOrDefault(value =>
                    string.Equals(
                        value.PortId.Value,
                        portId,
                        StringComparison.Ordinal) &&
                    (!direction.HasValue ||
                     value.Direction == direction.Value));
            return port ??
                   throw new InvalidOperationException(
                       $"Pose node '{node.NodeId}' does not declare port '{portId}'.");
        }

        public static CharacterPosePortKind Kind(string valueTypeId) =>
            valueTypeId switch
            {
                "pose.local" => CharacterPosePortKind.LocalPose,
                "pose.component" => CharacterPosePortKind.ComponentPose,
                "pose.parameter" =>
                    CharacterPosePortKind.Parameter,
                "pose.discontinuity" =>
                    CharacterPosePortKind.PoseDiscontinuity,
                "pose.action-playback" =>
                    CharacterPosePortKind.ActionPlayback,
                "component.full-body-ik-goals" =>
                    CharacterPosePortKind.FullBodyIkGoals,
                "component.full-body-ik-goal-contribution" =>
                    CharacterPosePortKind.FullBodyIkGoalContribution,
                "pose.history" => CharacterPosePortKind.PoseHistory,
                "motion-matching.trajectory" => CharacterPosePortKind.Trajectory,
                "presentation.facts" => CharacterPosePortKind.PresentationFacts,
                "motion-matching.binding" => CharacterPosePortKind.MotionMatchingBinding,
                _ => throw new InvalidOperationException(
                    $"Pose value type '{valueTypeId}' is not registered.")
            };

        public static string ValueType(CharacterPosePortKind kind) =>
            kind switch
            {
                CharacterPosePortKind.LocalPose => "pose.local",
                CharacterPosePortKind.ComponentPose => "pose.component",
                CharacterPosePortKind.Parameter =>
                    "pose.parameter",
                CharacterPosePortKind.PoseDiscontinuity =>
                    "pose.discontinuity",
                CharacterPosePortKind.ActionPlayback =>
                    "pose.action-playback",
                CharacterPosePortKind.FullBodyIkGoals =>
                    "component.full-body-ik-goals",
                CharacterPosePortKind.FullBodyIkGoalContribution =>
                    "component.full-body-ik-goal-contribution",
                CharacterPosePortKind.PoseHistory => "pose.history",
                CharacterPosePortKind.Trajectory => "motion-matching.trajectory",
                CharacterPosePortKind.PresentationFacts => "presentation.facts",
                CharacterPosePortKind.MotionMatchingBinding => "motion-matching.binding",
                _ => throw new InvalidOperationException(
                    $"Pose port kind '{kind}' is not registered.")
            };

        static CharacterPosePortDefinition ToPosePort(
            GraphAuthoringDynamicPortProjection port) =>
            new CharacterPosePortDefinition(
                new PosePortId(port.PortId.Value),
                port.DisplayName,
                Kind(port.ValueTypeId),
                port.Direction ==
                GraphAuthoringPortDirection.Input
                    ? CharacterPosePortDirection.Input
                    : CharacterPosePortDirection.Output,
                port.Required,
                string.IsNullOrWhiteSpace(port.InterfacePortId)
                    ? default
                    : new PoseInterfacePortId(port.InterfacePortId));
    }
}
