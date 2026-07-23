using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    public sealed class CharacterAnimationProducerBindingRequestEntry
    {
        public string timelineAuthoringId = string.Empty;
        public string trackAuthoringId = string.Empty;
        public string sourceKind = string.Empty;
        public string sourceAssetPath = string.Empty;
    }

    [Serializable]
    public sealed class CharacterAnimationProducerBindingRequest
    {
        public string fixedProgramWrapperPath = string.Empty;
        public CharacterAnimationProducerBindingRequestEntry[] bindings =
            Array.Empty<CharacterAnimationProducerBindingRequestEntry>();
    }

    public sealed class CharacterAnimationProducerBindingInspectionEntry
    {
        public string producerIdentity = string.Empty;
        public string displayName = string.Empty;
        public string sourceKind = string.Empty;
        public string sourceAssetPath = string.Empty;
        public string[] clipPaths = Array.Empty<string>();
    }

    public sealed class CharacterAnimationProducerBindingResult
    {
        public string definitionPath = string.Empty;
        public string profilePath = string.Empty;
        public string projectionPath = string.Empty;
        public string projectionRevision = string.Empty;
        public string float32ProgramWrapperPath = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
        public CharacterAnimationProducerBindingInspectionEntry[] bindings =
            Array.Empty<CharacterAnimationProducerBindingInspectionEntry>();
        public string[] createdSourcePaths = Array.Empty<string>();
        public bool authoringSucceeded;
        public bool buildSucceeded;
    }

    public static class CharacterAnimationProducerBindingAuthoringService
    {
        sealed class ResolvedBinding
        {
            public CharacterAnimationProducerBindingRequestEntry Request;
            public AnimationProducerAuthoringEntry Producer;
            public AnimationPoseSourceKind SourceKind;
            public string SourcePath;
        }

        sealed class Context
        {
            public CharacterPipelineDefinition Definition;
            public CharacterAnimationPresentationProfile Profile;
            public CharacterAnimationProducerBindingRequest Request;
            public ResolvedBinding[] Bindings;
        }

        public static CharacterAnimationProducerBindingResult Inspect(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            return CreateResult(BuildContext(definition, request), Array.Empty<string>(), false);
        }

        public static CharacterAnimationProducerBindingResult Apply(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            Context context = BuildContext(definition, request);
            var createdPaths = new List<string>();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure Character Animation Producer Bindings");
            try
            {
                for (int i = 0; i < context.Bindings.Length; i++)
                {
                    ResolvedBinding binding = context.Bindings[i];
                    if (binding.SourceKind == AnimationPoseSourceKind.BlendSpace)
                    {
                        CharacterAnimationBlendSpaceAsset blendSpace =
                            AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendSpaceAsset>(binding.SourcePath);
                        if (!blendSpace)
                            throw new InvalidOperationException($"Blend Space source '{binding.SourcePath}' does not exist.");
                        CharacterAnimationPresentationAuthoringService.ConfigureBlendSpaceProducerBinding(
                            context.Profile,
                            context.Definition,
                            binding.Producer.ProducerId,
                            blendSpace);
                        continue;
                    }

                    TransitionAssetBase source = AssetDatabase.LoadAssetAtPath<TransitionAssetBase>(binding.SourcePath);
                    if (!source)
                    {
                        source = CreateTimelineSource(binding.Producer, binding.SourcePath);
                        createdPaths.Add(binding.SourcePath);
                    }
                    if (!source.IsValid)
                        throw new InvalidOperationException($"Timeline source '{binding.SourcePath}' is invalid.");
                    CharacterAnimationPresentationAuthoringService.ConfigureTimelineProducerBinding(
                        context.Profile,
                        context.Definition,
                        binding.Producer.ProducerId,
                        source);
                }
                NormalizePlayerTopology(context);
                Validate(context);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                return CreateResult(BuildContext(definition, request), createdPaths.ToArray(), false);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                for (int i = 0; i < createdPaths.Count; i++)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(createdPaths[i]))
                        AssetDatabase.DeleteAsset(createdPaths[i]);
                }
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void NormalizePlayerTopology(Context context)
        {
            CharacterPresentationPoseGraphAsset owner = context.Profile.PoseGraph;
            CharacterPoseGraphData graph = owner.Graph;
            AnimationChannelId[] timelineOnlyChannels = context.Bindings
                .GroupBy(value => value.Producer.AnimationChannelId)
                .Where(group => group.All(value => value.SourceKind == AnimationPoseSourceKind.Timeline))
                .Select(group => group.Key)
                .OrderBy(value => value)
                .ToArray();
            for (int channelIndex = 0; channelIndex < timelineOnlyChannels.Length; channelIndex++)
            {
                AnimationChannelId channelId = timelineOnlyChannels[channelIndex];
                CharacterPoseNodeDefinition selection = graph.Nodes.SingleOrDefault(value =>
                    value != null && value.Kind == CharacterPoseNodeKind.AnimationSelectionInput &&
                    value.AnimationChannelId == channelId);
                if (selection == null)
                    continue;
                CharacterPoseNodeDefinition[] markers = graph.Edges
                    .Where(value => value != null && value.SourceNodeId == selection.NodeId)
                    .Select(value => graph.Nodes.Single(node => node.NodeId == value.TargetNodeId))
                    .Where(value => value.Kind == CharacterPoseNodeKind.MarkerSync)
                    .Distinct()
                    .ToArray();
                if (markers.Length == 0)
                    continue;
                if (markers.Length != 1)
                    throw new InvalidOperationException($"Animation Channel '{channelId}' has an ambiguous MarkerSync boundary.");
                CharacterPoseNodeDefinition marker = markers[0];
                CharacterPoseNodeDefinition player = RequireSingleTarget(
                    graph,
                    marker,
                    CharacterPoseNodeKind.SelectedPosePlayer,
                    CharacterPoseNodeKind.BlendSpacePlayer,
                    CharacterPoseNodeKind.BlendStack);
                if (player.Kind != CharacterPoseNodeKind.BlendSpacePlayer)
                    continue;
                CharacterPoseNodeDefinition downstream = RequireSinglePoseTarget(graph, player);
                PoseNodeId[] axisNodes = graph.Edges
                    .Where(value => value != null && value.TargetNodeId == player.NodeId)
                    .Select(value => graph.Nodes.Single(node => node.NodeId == value.SourceNodeId))
                    .Where(value => value.Kind == CharacterPoseNodeKind.ProgramParameterInput)
                    .Select(value => value.NodeId)
                    .Distinct()
                    .ToArray();
                PoseParameterId[] axisParameters = graph.Nodes
                    .Where(value => value != null && axisNodes.Contains(value.NodeId) && value.ParameterId.IsValid)
                    .Select(value => value.ParameterId)
                    .Distinct()
                    .ToArray();
                for (int i = 0; i < axisNodes.Length; i++)
                {
                    if (graph.Edges.Any(value => value != null && value.SourceNodeId == axisNodes[i] && value.TargetNodeId != player.NodeId))
                        throw new InvalidOperationException($"Blend Space axis node '{axisNodes[i]}' has a consumer outside Player '{player.NodeId}'.");
                }
                Vector2 position = player.Position;
                string displayName = player.DisplayName;
                CharacterPresentationPoseGraphAuthoringService.DeleteSelection(
                    owner,
                    graph,
                    axisNodes.Concat(new[] { player.NodeId }).ToArray(),
                    Array.Empty<string>());
                RemoveUnusedAxisParameters(context, graph, axisParameters);
                CharacterPoseNodeDefinition selected = CharacterPresentationPoseGraphAuthoringService.CreateNode(
                    owner,
                    graph,
                    CharacterPoseNodeKind.SelectedPosePlayer,
                    position);
                CharacterPresentationPoseGraphAuthoringService.RenameNode(
                    owner,
                    graph,
                    selected.NodeId,
                    string.IsNullOrWhiteSpace(displayName) ? "Locomotion Player" : displayName.Replace("Blend Space ", string.Empty));
                Connect(owner, graph, marker, CharacterPosePortKind.AnimationSelection, selected, CharacterPosePortKind.AnimationSelection);
                Connect(owner, graph, selected, CharacterPosePortKind.Pose, downstream, CharacterPosePortKind.Pose);
            }
            RemoveUnusedAxisParameters(
                context,
                graph,
                graph.Parameters
                    .Where(parameter => parameter != null &&
                                        !graph.Nodes.Any(node => node != null &&
                                            node.Kind == CharacterPoseNodeKind.ProgramParameterInput &&
                                            node.ParameterId.Equals(parameter.ParameterId)) &&
                                        !graph.Nodes.Any(node => node != null &&
                                            node.ParameterId.Equals(parameter.ParameterId)))
                    .Select(parameter => parameter.ParameterId)
                    .ToArray());
        }

        static void RemoveUnusedAxisParameters(
            Context context,
            CharacterPoseGraphData graph,
            IReadOnlyList<PoseParameterId> candidates)
        {
            if (candidates.Count == 0)
                return;
            var removable = new HashSet<PoseParameterId>(candidates.Where(parameterId =>
                !graph.Nodes.Any(value => value != null && value.Kind == CharacterPoseNodeKind.ProgramParameterInput &&
                                         value.ParameterId.Equals(parameterId))));
            if (removable.Count == 0)
                return;
            if (graph.Nodes.Any(value => value != null && value.ParameterId.IsValid &&
                                         removable.Contains(value.ParameterId)))
                throw new InvalidOperationException("A removed Blend Space axis parameter is still owned by another Pose node.");

            CharacterPresentationPoseGraphAuthoringService.ConfigureParameters(
                context.Profile.PoseGraph,
                graph.Parameters.Where(value => value != null && !removable.Contains(value.ParameterId)).ToArray());

            CharacterPoseNodeDefinition[] policyNodes = graph.Nodes
                .Where(value => value != null && value.ParameterPolicies.Any(policy => removable.Contains(policy.ParameterId)))
                .ToArray();
            for (int i = 0; i < policyNodes.Length; i++)
            {
                CharacterPoseNodeDefinition node = policyNodes[i];
                CharacterPresentationPoseGraphAuthoringService.ConfigureNode(
                    context.Profile.PoseGraph,
                    graph,
                    node.NodeId,
                    node.AnimationChannelId,
                    node.ProgramProducerId,
                    node.ParameterId,
                    node.SelectionAvailability,
                    node.BlendSpaceInputRangePolicy,
                    node.BlendPolicy,
                    node.InertializationPolicy,
                    node.BoneMask,
                    node.Weight,
                    node.ParameterPolicies.Where(value => !removable.Contains(value.ParameterId)).ToArray(),
                    node.AdditiveReferencePoseId,
                    node.AdditiveReferenceSpace,
                    node.AdditiveScalePolicy,
                    node.BoneId,
                    node.ModifyBoneReferenceSpace,
                    node.ModifyBoneOperations,
                    node.ModifyPosition,
                    node.ModifyRotation.eulerAngles,
                    node.ModifyScale,
                    node.FootPlacementProfile,
                    node.FootPlacementCalibration);
            }

            CharacterPoseInertializationPolicy[] inertializationPolicies = graph.Nodes
                .Where(value => value != null && value.Kind == CharacterPoseNodeKind.Inertialization &&
                                value.InertializationPolicy)
                .Select(value => value.InertializationPolicy)
                .Distinct()
                .ToArray();
            for (int i = 0; i < inertializationPolicies.Length; i++)
                RemoveInertializationParameterFilters(context, inertializationPolicies[i], removable);
        }

        static void RemoveInertializationParameterFilters(
            Context context,
            CharacterPoseInertializationPolicy policy,
            HashSet<PoseParameterId> removed)
        {
            bool changed = policy.DefaultRule.ParameterFilters.Any(value => removed.Contains(value.ParameterId)) ||
                           policy.Overrides.Any(value => value.Rule.ParameterFilters.Any(filter => removed.Contains(filter.ParameterId)));
            if (!changed)
                return;
            Undo.RecordObject(policy, "Remove Unused Pose Inertialization Parameters");
            CharacterPoseInertializationRule defaultRule = CloneRuleWithoutParameters(policy.DefaultRule, removed);
            CharacterPoseInertializationOverride[] overrides = policy.Overrides.Select(value =>
            {
                var replacement = new CharacterPoseInertializationOverride();
                replacement.Configure(
                    value.SourceProducerIdentity,
                    value.TargetProducerIdentity,
                    CloneRuleWithoutParameters(value.Rule, removed));
                return replacement;
            }).ToArray();
            policy.Configure(
                policy.PolicyId,
                Guid.NewGuid().ToString("N"),
                defaultRule,
                overrides,
                context.Profile.RigDefinition);
            EditorUtility.SetDirty(policy);
        }

        static CharacterPoseInertializationRule CloneRuleWithoutParameters(
            CharacterPoseInertializationRule source,
            HashSet<PoseParameterId> removed)
        {
            var replacement = new CharacterPoseInertializationRule();
            replacement.Configure(
                source.Mode,
                source.DurationSeconds,
                source.Curve,
                source.BlendProfile,
                source.ParameterFilters.Where(value => !removed.Contains(value.ParameterId)).ToArray());
            return replacement;
        }

        static CharacterPoseNodeDefinition RequireSingleTarget(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition source,
            params CharacterPoseNodeKind[] allowedKinds)
        {
            CharacterPoseNodeDefinition[] targets = graph.Edges
                .Where(value => value != null && value.SourceNodeId == source.NodeId)
                .Select(value => graph.Nodes.Single(node => node.NodeId == value.TargetNodeId))
                .Where(value => allowedKinds.Contains(value.Kind))
                .Distinct()
                .ToArray();
            if (targets.Length != 1)
                throw new InvalidOperationException($"Pose node '{source.NodeId}' does not have one exact Player target.");
            return targets[0];
        }

        static CharacterPoseNodeDefinition RequireSinglePoseTarget(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition source)
        {
            CharacterPoseNodeDefinition[] targets = graph.Edges
                .Where(value => value != null && value.SourceNodeId == source.NodeId &&
                                RequirePort(source, value.SourcePortId).Kind == CharacterPosePortKind.Pose)
                .Select(value => graph.Nodes.Single(node => node.NodeId == value.TargetNodeId))
                .Distinct()
                .ToArray();
            if (targets.Length != 1)
                throw new InvalidOperationException($"Pose Player '{source.NodeId}' does not have one exact Pose target.");
            return targets[0];
        }

        static void Connect(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition source,
            CharacterPosePortKind sourceKind,
            CharacterPoseNodeDefinition target,
            CharacterPosePortKind targetKind)
        {
            CharacterPresentationPoseGraphAuthoringService.Connect(
                owner,
                graph,
                source.NodeId,
                source.Ports.Single(value => value.Kind == sourceKind && value.Direction == CharacterPosePortDirection.Output).PortId,
                target.NodeId,
                target.Ports.Single(value => value.Kind == targetKind && value.Direction == CharacterPosePortDirection.Input).PortId);
        }

        static CharacterPosePortDefinition RequirePort(CharacterPoseNodeDefinition node, PosePortId portId) =>
            node.Ports.Single(value => value.PortId.Equals(portId));

        public static CharacterAnimationProducerBindingResult Build(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            Context context = BuildContext(definition, request);
            Validate(context);
            var buildRequest = new CharacterSimulationBuildRequest(
                definition,
                CharacterSimulationBuildPublicationMode.Publish,
                new ICharacterSimulationTargetBuildAdapter[]
                {
                    CharacterSimulationTargetCatalog.Float32(definition),
                    new FixedCharacterSimulationTargetBuildAdapter(request.fixedProgramWrapperPath)
                });
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(buildRequest);
            if (!result.IsValid)
                throw new InvalidOperationException(string.Join("\n", result.Report.Messages.Select(value => value.ToString())));
            AssetDatabase.SaveAssets();
            CharacterAnimationProducerBindingResult output = CreateResult(
                BuildContext(definition, request),
                Array.Empty<string>(),
                true);
            output.projectionRevision = definition.PresentationProjection
                ? definition.PresentationProjection.ProjectionRevision
                : string.Empty;
            return output;
        }

        static Context BuildContext(
            CharacterPipelineDefinition definition,
            CharacterAnimationProducerBindingRequest request)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile
                ? definition.AnimationPresentationProfile
                : throw new InvalidOperationException($"Character Definition '{definition.name}' has no Animation Presentation Profile.");
            request.fixedProgramWrapperPath = NormalizeAssetPath(request.fixedProgramWrapperPath, ".asset");
            AnimationProducerAuthoringEntry[] producers = CharacterAnimationPresentationAuthoringService
                .DiscoverProducerTracks(definition)
                .OrderBy(value => value.ProgramProducerIdentity, StringComparer.Ordinal)
                .ToArray();
            CharacterAnimationProducerBindingRequestEntry[] requested = request.bindings ??
                Array.Empty<CharacterAnimationProducerBindingRequestEntry>();
            if (requested.Length != producers.Length)
                throw new InvalidOperationException("Producer binding request must cover the complete reachable Animation producer topology.");

            var producerMap = producers.ToDictionary(value => value.ProducerId);
            var identities = new HashSet<AnimationProducerId>();
            var resolved = new ResolvedBinding[requested.Length];
            for (int i = 0; i < requested.Length; i++)
            {
                CharacterAnimationProducerBindingRequestEntry entry = requested[i] ??
                    throw new InvalidOperationException($"Producer binding request #{i} is missing.");
                var producerId = new AnimationProducerId(entry.timelineAuthoringId, entry.trackAuthoringId);
                if (!producerId.IsValid || !identities.Add(producerId) || !producerMap.TryGetValue(producerId, out AnimationProducerAuthoringEntry producer))
                    throw new InvalidOperationException($"Producer binding request #{i} does not identify one reachable producer exactly once.");
                AnimationPoseSourceKind kind = entry.sourceKind switch
                {
                    "Timeline" => AnimationPoseSourceKind.Timeline,
                    "BlendSpace" => AnimationPoseSourceKind.BlendSpace,
                    _ => throw new InvalidOperationException($"Producer '{producerId}' source kind must be Timeline or BlendSpace.")
                };
                string sourcePath = NormalizeAssetPath(entry.sourceAssetPath, ".asset");
                if (kind == AnimationPoseSourceKind.Timeline && producer.SourceClips.Count == 0)
                    throw new InvalidOperationException($"Timeline producer '{producerId}' has no source clips.");
                resolved[i] = new ResolvedBinding
                {
                    Request = entry,
                    Producer = producer,
                    SourceKind = kind,
                    SourcePath = sourcePath
                };
            }
            Array.Sort(resolved, (left, right) =>
                string.CompareOrdinal(left.Producer.ProgramProducerIdentity, right.Producer.ProgramProducerIdentity));
            return new Context
            {
                Definition = definition,
                Profile = profile,
                Request = request,
                Bindings = resolved
            };
        }

        static TransitionAssetBase CreateTimelineSource(AnimationProducerAuthoringEntry producer, string path)
        {
            string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory) || !AssetDatabase.IsValidFolder(directory))
                throw new InvalidOperationException($"Timeline source directory '{directory}' does not exist.");
            var source = ScriptableObject.CreateInstance<TransitionAsset>();
            source.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(source, path);
            Undo.RegisterCreatedObjectUndo(source, "Create Character Animation Timeline Source");
            if (producer.SourceClips.Count == 1)
            {
                source.Transition = new ClipTransition { Clip = producer.SourceClips[0].Clip };
            }
            else
            {
                var sequence = new TransitionSequence
                {
                    Transitions = producer.SourceClips
                        .Select(value => (ITransition)new ClipTransition { Clip = value.Clip })
                        .ToArray()
                };
                source.Transition = sequence;
            }
            EditorUtility.SetDirty(source);
            return source;
        }

        static void Validate(Context context)
        {
            for (int i = 0; i < context.Bindings.Length; i++)
            {
                ResolvedBinding expected = context.Bindings[i];
                AnimationProducerPresentationBinding actual = context.Profile.FindProducerBinding(expected.Producer.ProducerId);
                if (actual == null || actual.SourceKind != expected.SourceKind)
                    throw new InvalidOperationException($"Producer '{expected.Producer.ProducerId}' does not use the requested source kind.");
                UnityEngine.Object source = expected.SourceKind == AnimationPoseSourceKind.BlendSpace
                    ? actual.BlendSpaceSource
                    : actual.Source;
                if (!source || !string.Equals(AssetDatabase.GetAssetPath(source), expected.SourcePath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Producer '{expected.Producer.ProducerId}' does not use source '{expected.SourcePath}'.");
            }
            var profileErrors = new List<string>();
            context.Profile.CollectConfigurationErrors(profileErrors);
            if (profileErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", profileErrors));
            CharacterPoseGraphValidationReport graphReport = CharacterPresentationPoseGraphValidator.Validate(
                context.Profile.PoseGraph,
                context.Profile.RigDefinition,
                context.Bindings.Select(value => value.Producer.AnimationChannelId).Distinct().ToArray());
            if (!graphReport.IsValid)
            {
                var errors = new List<string>();
                graphReport.CopyMessagesTo(errors);
                throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        static CharacterAnimationProducerBindingResult CreateResult(
            Context context,
            string[] createdPaths,
            bool buildSucceeded)
        {
            return new CharacterAnimationProducerBindingResult
            {
                definitionPath = AssetDatabase.GetAssetPath(context.Definition),
                profilePath = AssetDatabase.GetAssetPath(context.Profile),
                projectionPath = AssetDatabase.GetAssetPath(context.Definition.PresentationProjection),
                projectionRevision = context.Definition.PresentationProjection
                    ? context.Definition.PresentationProjection.ProjectionRevision
                    : string.Empty,
                float32ProgramWrapperPath = AssetDatabase.GetAssetPath(context.Definition.SimulationProgram),
                fixedProgramWrapperPath = context.Request.fixedProgramWrapperPath,
                bindings = context.Bindings.Select(value =>
                {
                    AnimationProducerPresentationBinding binding =
                        context.Profile.FindProducerBinding(value.Producer.ProducerId);
                    UnityEngine.Object source = binding?.SourceKind == AnimationPoseSourceKind.BlendSpace
                        ? binding.BlendSpaceSource
                        : binding?.Source;
                    return new CharacterAnimationProducerBindingInspectionEntry
                    {
                        producerIdentity = value.Producer.ProgramProducerIdentity,
                        displayName = value.Producer.DisplayName,
                        sourceKind = binding?.SourceKind.ToString() ?? "Missing",
                        sourceAssetPath = source ? AssetDatabase.GetAssetPath(source) : string.Empty,
                        clipPaths = value.Producer.SourceClips
                            .Select(clip => clip.Clip ? AssetDatabase.GetAssetPath(clip.Clip) : string.Empty)
                            .ToArray()
                    };
                }).ToArray(),
                createdSourcePaths = createdPaths,
                authoringSucceeded = true,
                buildSucceeded = buildSucceeded
            };
        }

        static string NormalizeAssetPath(string value, string extension)
        {
            string path = (value ?? string.Empty).Trim().Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                path.Contains(".."))
                throw new InvalidOperationException($"Asset path '{path}' must be an explicit Assets/...{extension} path.");
            return path;
        }
    }
}
