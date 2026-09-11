using System;
using System.Collections.Generic;
using System.Linq;
using TreeDesigner.Authoring;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterPoseGraphAuthoringCapabilities
    {
        public static readonly GraphAuthoringCommandId PingPoseSource = new GraphAuthoringCommandId("ping-pose-source");
        public static readonly GraphAuthoringCommandId OpenPoseSource = new GraphAuthoringCommandId("open-pose-source");
        public static readonly GraphAuthoringCommandId OpenPoseSourceProfile = new GraphAuthoringCommandId("open-pose-source-profile");
        public static readonly GraphAuthoringCommandId OpenFullBodyIkProfile = new GraphAuthoringCommandId("open-full-body-ik-profile");
        public static readonly GraphAuthoringDomainId Domain = new GraphAuthoringDomainId("character-presentation");
        public static readonly GraphAuthoringDocumentRoleId RootGraph = new GraphAuthoringDocumentRoleId("pose-graph");
        public static readonly GraphAuthoringDocumentRoleId AnimationLayer = new GraphAuthoringDocumentRoleId("pose-animation-layer");
        public static readonly GraphAuthoringDocumentRoleId StatePoseGraph = new GraphAuthoringDocumentRoleId("pose-state-graph");
        public static readonly GraphAuthoringDocumentRoleId ControlRig = new GraphAuthoringDocumentRoleId("pose-control-rig");
        public static readonly GraphAuthoringDocumentRoleId Subgraph = new GraphAuthoringDocumentRoleId("pose-subgraph");
        public static readonly GraphAuthoringDocumentRoleId LinkedPoseEntry = new GraphAuthoringDocumentRoleId("linked-pose-entry");
        public static readonly GraphAuthoringDocumentRoleId StateMachine = new GraphAuthoringDocumentRoleId("pose-state-machine");
        public static readonly GraphAuthoringDocumentRoleId TransitionRule = new GraphAuthoringDocumentRoleId("pose-transition-rule");
        public static readonly GraphAuthoringCapabilityId StateMachineState = new GraphAuthoringCapabilityId("pose.state-machine.state");
        public static readonly GraphAuthoringCapabilityId StateMachineTransition = new GraphAuthoringCapabilityId("pose.state-machine.transition");

        static readonly IReadOnlyDictionary<PoseTransitionRuleOperationKind, GraphAuthoringCapabilityId> s_RuleCapabilities =
            Enum.GetValues(typeof(PoseTransitionRuleOperationKind))
                .Cast<PoseTransitionRuleOperationKind>()
                .ToDictionary(
                    value => value,
                    value => new GraphAuthoringCapabilityId(
                        "pose.transition-rule." +
                        ToKebabCase(value.ToString())));

        public static GraphAuthoringCapabilityId Get(CharacterPoseNodeKind kind)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseNodeKind), kind))
            {
                throw new InvalidOperationException(
                    $"Pose node kind '{kind}' has no authoring capability identity.");
            }
            return new GraphAuthoringCapabilityId(
                "pose." + ToKebabCase(kind.ToString()));
        }

        public static GraphAuthoringDocumentRoleId GetRole(
            CharacterPoseAuthoringGraphRole role)
        {
            return role switch
            {
                CharacterPoseAuthoringGraphRole.AnimGraph => RootGraph,
                CharacterPoseAuthoringGraphRole.AnimationLayer => AnimationLayer,
                CharacterPoseAuthoringGraphRole.StatePose => StatePoseGraph,
                CharacterPoseAuthoringGraphRole.TransitionRule => TransitionRule,
                CharacterPoseAuthoringGraphRole.ControlRig => ControlRig,
                CharacterPoseAuthoringGraphRole.Subgraph => Subgraph,
                CharacterPoseAuthoringGraphRole.LinkedPoseEntry => LinkedPoseEntry,
                _ => throw new InvalidOperationException($"Pose authoring graph role '{role}' has no document role.")
            };
        }

        public static bool TryResolveRole(
            string documentRole,
            out CharacterPoseAuthoringGraphRole role)
        {
            foreach (CharacterPoseAuthoringGraphRole candidate in
                     Enum.GetValues(typeof(CharacterPoseAuthoringGraphRole)))
            {
                if (string.Equals(
                        GetRole(candidate).Value,
                        documentRole,
                        StringComparison.Ordinal))
                {
                    role = candidate;
                    return true;
                }
            }
            role = default;
            return false;
        }

        public static GraphAuthoringDocumentRoleId ResolveGraphRole(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseCanvasGraph graph,
            ISet<PoseGraphId> linkedEntryGraphs)
        {
            if (!owner || graph == null || !owner.EnumerateGraphs().Contains(graph))
                throw new ArgumentException("Pose Graph role context is invalid.");
            if (linkedEntryGraphs != null && linkedEntryGraphs.Contains(graph.GraphId))
                return LinkedPoseEntry;
            if (ReferenceEquals(graph, owner.Graph))
                return RootGraph;
            if (graph.Role == CharacterPoseAuthoringGraphRole.AnimationLayer)
                return AnimationLayer;
            if (graph.Role == CharacterPoseAuthoringGraphRole.ControlRig)
                return ControlRig;
            bool stateGraph = owner.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Where(value => value.StateMachine != null)
                .SelectMany(value => value.StateMachine.States)
                .Where(value => value != null && value.PoseGraphId.IsValid)
                .Any(value => value.PoseGraphId.Equals(graph.GraphId));
            return stateGraph ? StatePoseGraph : Subgraph;
        }

        public static GraphAuthoringCapabilityId Get(
            PoseTransitionRuleOperationKind kind)
        {
            if (!s_RuleCapabilities.TryGetValue(
                    kind,
                    out GraphAuthoringCapabilityId capabilityId))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule operation kind '{kind}' has no authoring capability identity.");
            }
            return capabilityId;
        }

        public static bool TryGetRuleOperationKind(
            GraphAuthoringCapabilityId capabilityId,
            out PoseTransitionRuleOperationKind kind)
        {
            foreach (KeyValuePair<
                         PoseTransitionRuleOperationKind,
                         GraphAuthoringCapabilityId> entry in
                     s_RuleCapabilities)
            {
                if (!entry.Value.Equals(capabilityId))
                    continue;
                kind = entry.Key;
                return true;
            }
            kind = default;
            return false;
        }

        public static string ToKebabCase(string value)
        {
            var characters = new List<char>(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current))
                    characters.Add('-');
                characters.Add(char.ToLowerInvariant(current));
            }
            return new string(characters.ToArray());
        }
    }
}
