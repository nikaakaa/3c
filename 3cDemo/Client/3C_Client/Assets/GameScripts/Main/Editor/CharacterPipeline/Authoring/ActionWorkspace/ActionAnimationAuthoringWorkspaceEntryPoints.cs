using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    public static class ActionAnimationAuthoringWorkspaceEntryPoints
    {
        sealed class Candidate
        {
            public Candidate(
                string label,
                ActionAnimationWorkspaceOpenRequest request)
            {
                Label = label;
                Request = request;
            }

            public string Label { get; }
            public ActionAnimationWorkspaceOpenRequest Request { get; }
        }

        static ActionAnimationAuthoringWorkspaceEntryPoints()
        {
            AuthoringDetailsCommandRegistry
                .Register<ActivateActionInstanceNode>(
                    ActionAnimationWorkspaceCommands.Open,
                    OpenFromActivation);
            AuthoringDetailsCommandRegistry
                .Register<TimelineNode>(
                    ActionAnimationWorkspaceCommands.Open,
                    OpenFromTimeline);
        }

        public static void OpenFromPoseSlot(
            CharacterPipelineDefinition definition,
            CharacterPresentationPoseGraphAsset asset,
            CharacterTypedPoseGraph graph,
            CharacterTypedPoseNode node)
        {
            if (!definition || !asset || graph == null || node == null)
            {
                ShowFailure(
                    "Pose Graph入口缺少精确Definition、Graph或Node。");
                return;
            }
            if (node.Payload is not
                CharacterAnimationSlotPosePayload)
            {
                ShowFailure(
                    $"Pose Node '{node.NodeId}'不是AnimationSlot。");
                return;
            }
            IReadOnlyList<Candidate> candidates =
                ResolveCandidates(
                    definition,
                    resolution =>
                        resolution.Slot != null &&
                        resolution.Slot.Asset == asset &&
                        ReferenceEquals(
                            resolution.Slot.Graph,
                            graph) &&
                        resolution.Slot.Node.NodeId.Equals(
                            node.NodeId));
            OpenOrChoose(
                "该AnimationSlot没有唯一可达的有限Action producer。",
                candidates);
        }

        static void OpenFromActivation(
            BaseTreeWindow window,
            ActivateActionInstanceNode node)
        {
            if (!TryGetDefinition(
                    window,
                    out CharacterPipelineDefinition definition))
                return;
            if (!node.ActionProfile)
            {
                ShowFailure(
                    $"ActivateActionInstanceNode '{node.GUID}'没有ActionProfile。");
                return;
            }
            ActionAnimationWorkspaceResolution resolution =
                ActionAnimationAuthoringWorkspaceResolver.Resolve(
                    new ActionAnimationWorkspaceOpenRequest(
                        definition,
                        node.ActionProfile.ActionId));
            ActionAnimationAuthoringWorkspaceWindow.Open(
                ExactRequest(
                    definition,
                    node.ActionProfile.ActionId,
                    resolution));
        }

        static void OpenFromTimeline(
            BaseTreeWindow window,
            TimelineNode node)
        {
            if (!TryGetDefinition(
                    window,
                    out CharacterPipelineDefinition definition))
                return;
            IReadOnlyList<Candidate> candidates =
                ResolveCandidates(
                    definition,
                    resolution =>
                        resolution.Timeline != null &&
                        ReferenceEquals(
                            resolution.Timeline.Node,
                            node));
            OpenOrChoose(
                "该有限Timeline没有唯一可达的ActionProfile。",
                candidates);
        }

        static IReadOnlyList<Candidate> ResolveCandidates(
            CharacterPipelineDefinition definition,
            Func<ActionAnimationWorkspaceResolution, bool> accept)
        {
            var candidates = new List<Candidate>();
            var seen =
                new HashSet<string>(StringComparer.Ordinal);
            foreach (ActionProfile profile in definition.ActionProfiles
                         .Where(value => value)
                         .OrderBy(
                             value => value.ActionId,
                             StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(profile.ActionId) ||
                    !seen.Add(profile.ActionId))
                    continue;
                ActionAnimationWorkspaceResolution resolution;
                try
                {
                    resolution =
                        ActionAnimationAuthoringWorkspaceResolver
                            .Resolve(
                                new ActionAnimationWorkspaceOpenRequest(
                                    definition,
                                    profile.ActionId));
                }
                catch (Exception)
                {
                    continue;
                }
                if (!accept(resolution))
                    continue;
                candidates.Add(
                    new Candidate(
                        string.IsNullOrWhiteSpace(
                            profile.DisplayName)
                            ? profile.ActionId
                            : $"{profile.DisplayName} ({profile.ActionId})",
                        ExactRequest(
                            definition,
                            profile.ActionId,
                            resolution)));
            }
            return candidates;
        }

        static ActionAnimationWorkspaceOpenRequest ExactRequest(
            CharacterPipelineDefinition definition,
            string actionId,
            ActionAnimationWorkspaceResolution resolution) =>
            new ActionAnimationWorkspaceOpenRequest(
                definition,
                actionId,
                resolution?.Timeline?.Timeline.AuthoringId ??
                string.Empty,
                resolution?.Producer?.Track.AuthoringId ??
                string.Empty,
                resolution?.Slot?.SlotId.Value ??
                string.Empty);

        static void OpenOrChoose(
            string emptyMessage,
            IReadOnlyList<Candidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                ShowFailure(emptyMessage);
                return;
            }
            if (candidates.Count == 1)
            {
                ActionAnimationAuthoringWorkspaceWindow.Open(
                    candidates[0].Request);
                return;
            }
            var menu = new GenericMenu();
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                menu.AddItem(
                    new UnityEngine.GUIContent(
                        candidate.Label),
                    false,
                    () =>
                        ActionAnimationAuthoringWorkspaceWindow
                            .Open(candidate.Request));
            }
            menu.ShowAsContext();
        }

        static bool TryGetDefinition(
            BaseTreeWindow window,
            out CharacterPipelineDefinition definition)
        {
            definition =
                (window?.AuthoringContext as
                    CharacterPipelineAuthoringContext)
                ?.Definition;
            if (definition)
                return true;
            ShowFailure(
                "当前Graph没有精确Character Definition authoring context。");
            return false;
        }

        static void ShowFailure(string message)
        {
            EditorUtility.DisplayDialog(
                "Action Animation Workspace",
                message,
                "OK");
        }
    }
}
