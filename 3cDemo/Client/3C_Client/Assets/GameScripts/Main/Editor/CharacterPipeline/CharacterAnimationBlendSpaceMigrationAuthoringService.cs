using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed class CharacterAnimationBlendSpaceMigrationSampleRequest
    {
        public string timelineAuthoringId = string.Empty;
        public string trackAuthoringId = string.Empty;
        public string sampleId = string.Empty;
        public float position;
        public CharacterAnimationBlendSpaceSampleRole role = CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
        public float stationaryNormalizedTime;
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSpaceMigrationRequest
    {
        public string animationChannelId = string.Empty;
        public string blendSpaceAssetPath = string.Empty;
        public string blendSpaceId = string.Empty;
        public string axisParameterId = string.Empty;
        public string axisUnit = string.Empty;
        public float axisMinimum;
        public float axisMaximum;
        public string fixedProgramWrapperPath = string.Empty;
        public CharacterAnimationBlendSpaceMigrationSampleRequest[] samples =
            Array.Empty<CharacterAnimationBlendSpaceMigrationSampleRequest>();
    }

    public sealed class CharacterAnimationBlendSpaceMigrationProducerInspection
    {
        public string producerIdentity = string.Empty;
        public string displayName = string.Empty;
        public string sourceKind = string.Empty;
        public string sourcePath = string.Empty;
        public string[] clipPaths = Array.Empty<string>();
    }

    public sealed class CharacterAnimationBlendSpaceMigrationSampleInspection
    {
        public string sampleId = string.Empty;
        public string producerIdentity = string.Empty;
        public string clipPath = string.Empty;
        public float position;
        public string role = string.Empty;
    }

    public sealed class CharacterAnimationBlendSpaceMigrationInspection
    {
        public string definitionPath = string.Empty;
        public string profilePath = string.Empty;
        public string poseGraphPath = string.Empty;
        public string rigPath = string.Empty;
        public string projectionPath = string.Empty;
        public string animationChannelId = string.Empty;
        public string currentPlayerKind = string.Empty;
        public string currentPlayerNodeId = string.Empty;
        public string inertializationNodeId = string.Empty;
        public string blendSpaceAssetPath = string.Empty;
        public string blendSpaceId = string.Empty;
        public string axisParameterId = string.Empty;
        public string axisUnit = string.Empty;
        public float axisMinimum;
        public float axisMaximum;
        public CharacterAnimationBlendSpaceMigrationProducerInspection[] producers =
            Array.Empty<CharacterAnimationBlendSpaceMigrationProducerInspection>();
        public CharacterAnimationBlendSpaceMigrationSampleInspection[] samples =
            Array.Empty<CharacterAnimationBlendSpaceMigrationSampleInspection>();
        public bool readyForMigration;
        public bool migrated;
    }

    public sealed class CharacterAnimationBlendSpaceMigrationResult
    {
        public CharacterAnimationBlendSpaceMigrationInspection inspection;
        public string[] deletedTimelineSourcePaths = Array.Empty<string>();
        public string[] retainedTimelineSourcePaths = Array.Empty<string>();
        public string float32ProgramWrapperPath = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
        public string projectionPath = string.Empty;
        public string projectionRevision = string.Empty;
        public bool authoringSucceeded;
        public bool buildSucceeded;
    }

    public static class CharacterAnimationBlendSpaceMigrationAuthoringService
    {
        sealed class SampleBinding
        {
            public CharacterAnimationBlendSpaceMigrationSampleRequest Request;
            public AnimationProducerAuthoringEntry Producer;
            public AnimationProducerSourceClipAuthoringEntry Clip;
        }

        sealed class PosePath
        {
            public CharacterPoseNodeDefinition Selection;
            public CharacterPoseNodeDefinition Marker;
            public CharacterPoseNodeDefinition Player;
            public CharacterPoseNodeDefinition Inertialization;
        }

        sealed class Context
        {
            public CharacterPipelineDefinition Definition;
            public CharacterAnimationPresentationProfile Profile;
            public CharacterPresentationPoseGraphAsset PoseGraph;
            public CharacterAnimationRigDefinition Rig;
            public CharacterAnimationBlendSpaceMigrationRequest Request;
            public AnimationChannelId ChannelId;
            public PoseParameterId AxisParameterId;
            public AnimationProducerAuthoringEntry[] ChannelProducers;
            public SampleBinding[] Samples;
            public PosePath Path;
        }

        public static CharacterAnimationBlendSpaceMigrationInspection Inspect(
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request)
        {
            Context context = BuildContext(definition, request, false);
            return CreateInspection(context);
        }

        public static CharacterAnimationBlendSpaceMigrationResult Apply(
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request)
        {
            Context context = BuildContext(definition, request, true);
            string assetPath = NormalizeAssetPath(request.blendSpaceAssetPath, ".asset");
            RemoveOrphanedMigrationAsset(context, assetPath);
            string[] oldSourcePaths = context.ChannelProducers
                .Select(value => context.Profile.FindProducerBinding(value.ProducerId))
                .Where(value => value != null && value.Source)
                .Select(value => AssetDatabase.GetAssetPath(value.Source))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Character Locomotion Blend Space");
            CharacterAnimationBlendSpaceAsset blendSpace = null;
            try
            {
                blendSpace = ScriptableObject.CreateInstance<CharacterAnimationBlendSpaceAsset>();
                blendSpace.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(blendSpace, assetPath);
                Undo.RegisterCreatedObjectUndo(blendSpace, "Create Character Locomotion Blend Space");
                ConfigureBlendSpace(context, blendSpace);
                ConfigurePoseGraph(context);
                ConfigureProducerBindings(context, blendSpace);
                ValidateAuthoring(context, blendSpace);
                AssetDatabase.SaveAssets();
                string[] retained = DeleteOldSources(context.Profile, oldSourcePaths);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                string[] deleted = oldSourcePaths.Except(retained, StringComparer.Ordinal).ToArray();
                CharacterAnimationBlendSpaceMigrationInspection inspection = CreateInspection(
                    BuildContext(definition, request, false));
                return new CharacterAnimationBlendSpaceMigrationResult
                {
                    inspection = inspection,
                    deletedTimelineSourcePaths = deleted,
                    retainedTimelineSourcePaths = retained,
                    float32ProgramWrapperPath = AssetDatabase.GetAssetPath(definition.SimulationProgram),
                    fixedProgramWrapperPath = request.fixedProgramWrapperPath,
                    projectionPath = AssetDatabase.GetAssetPath(definition.PresentationProjection),
                    projectionRevision = definition.PresentationProjection
                        ? definition.PresentationProjection.ProjectionRevision
                        : string.Empty,
                    authoringSucceeded = true,
                    buildSucceeded = false
                };
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                if (AssetDatabase.LoadMainAssetAtPath(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        public static CharacterAnimationBlendSpaceMigrationResult Build(
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request)
        {
            Context context = BuildContext(definition, request, false);
            CharacterAnimationBlendSpaceAsset blendSpace = RequireMigrated(context);
            CharacterAnimationBlendSpaceAuthoringService.SetMode(blendSpace, CharacterAnimationBlendSpaceMode.Linear1D);
            EditorUtility.SetDirty(blendSpace);
            ValidateAuthoring(context, blendSpace);
            CharacterSimulationBuildResult build = BuildTargets(context);
            AssetDatabase.SaveAssets();
            return new CharacterAnimationBlendSpaceMigrationResult
            {
                inspection = CreateInspection(BuildContext(definition, request, false)),
                float32ProgramWrapperPath = AssetDatabase.GetAssetPath(definition.SimulationProgram),
                fixedProgramWrapperPath = request.fixedProgramWrapperPath,
                projectionPath = AssetDatabase.GetAssetPath(definition.PresentationProjection),
                projectionRevision = definition.PresentationProjection
                    ? definition.PresentationProjection.ProjectionRevision
                    : string.Empty,
                authoringSucceeded = true,
                buildSucceeded = build.IsValid
            };
        }

        static Context BuildContext(
            CharacterPipelineDefinition definition,
            CharacterAnimationBlendSpaceMigrationRequest request,
            bool requireLegacyPlayer)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile
                ? definition.AnimationPresentationProfile
                : throw new InvalidOperationException($"Character Definition '{definition.name}' has no Animation Presentation Profile.");
            CharacterPresentationPoseGraphAsset poseGraph = profile.PoseGraph
                ? profile.PoseGraph
                : throw new InvalidOperationException($"Animation Presentation Profile '{profile.name}' has no Pose Graph.");
            CharacterAnimationRigDefinition rig = profile.RigDefinition
                ? profile.RigDefinition
                : throw new InvalidOperationException($"Animation Presentation Profile '{profile.name}' has no Animation Rig.");
            rig.RequireValid();
            var channelId = new AnimationChannelId(request.animationChannelId);
            var axisParameterId = new PoseParameterId(request.axisParameterId);
            if (!axisParameterId.Equals(AnimationPoseParameterIds.MotorPlanarSpeed))
                throw new InvalidOperationException("Linear locomotion Blend Space migration requires the formal MotorPlanarSpeed ParameterId.");
            if (string.IsNullOrWhiteSpace(request.axisUnit) || !float.IsFinite(request.axisMinimum) ||
                !float.IsFinite(request.axisMaximum) || request.axisMinimum >= request.axisMaximum)
                throw new InvalidOperationException("Blend Space axis contract is invalid.");
            NormalizeAssetPath(request.blendSpaceAssetPath, ".asset");
            NormalizeAssetPath(request.fixedProgramWrapperPath, ".asset");
            _ = new CharacterAnimationBlendSpaceId(request.blendSpaceId);

            AnimationProducerAuthoringEntry[] producers = CharacterAnimationPresentationAuthoringService
                .DiscoverProducerTracks(definition)
                .Where(value => value.AnimationChannelId == channelId)
                .OrderBy(value => value.ProgramProducerIdentity, StringComparer.Ordinal)
                .ToArray();
            if (producers.Length == 0)
                throw new InvalidOperationException($"Animation Channel '{channelId}' has no reachable producer.");
            if (requireLegacyPlayer)
            {
                for (int i = 0; i < producers.Length; i++)
                {
                    AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producers[i].ProducerId);
                    if (binding == null || binding.SourceKind != AnimationPoseSourceKind.Timeline || !binding.Source)
                    {
                        throw new InvalidOperationException(
                            $"Animation producer '{producers[i].ProgramProducerIdentity}' is not an exact Timeline source migration input.");
                    }
                }
            }
            SampleBinding[] samples = ResolveSamples(request, producers);
            PosePath path = ResolvePosePath(poseGraph.Graph, channelId);
            if (requireLegacyPlayer && path.Player.Kind != CharacterPoseNodeKind.SelectedPosePlayer)
                throw new InvalidOperationException($"Animation Channel '{channelId}' is not connected to the legacy SelectedPosePlayer boundary.");
            return new Context
            {
                Definition = definition,
                Profile = profile,
                PoseGraph = poseGraph,
                Rig = rig,
                Request = request,
                ChannelId = channelId,
                AxisParameterId = axisParameterId,
                ChannelProducers = producers,
                Samples = samples,
                Path = path
            };
        }

        static SampleBinding[] ResolveSamples(
            CharacterAnimationBlendSpaceMigrationRequest request,
            IReadOnlyList<AnimationProducerAuthoringEntry> producers)
        {
            CharacterAnimationBlendSpaceMigrationSampleRequest[] requests = request.samples ??
                Array.Empty<CharacterAnimationBlendSpaceMigrationSampleRequest>();
            if (requests.Length < 2)
                throw new InvalidOperationException("Linear locomotion Blend Space requires at least two explicit samples.");
            var producerIndex = producers.ToDictionary(value => value.ProducerId);
            var sampleIds = new HashSet<CharacterAnimationBlendSpaceSampleId>();
            var positions = new HashSet<float>();
            var result = new SampleBinding[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                CharacterAnimationBlendSpaceMigrationSampleRequest sample = requests[i]
                    ?? throw new InvalidOperationException($"Blend Space migration Sample #{i} is missing.");
                var producerId = new AnimationProducerId(sample.timelineAuthoringId, sample.trackAuthoringId);
                var sampleId = new CharacterAnimationBlendSpaceSampleId(sample.sampleId);
                if (!producerIndex.TryGetValue(producerId, out AnimationProducerAuthoringEntry producer))
                    throw new InvalidOperationException($"Blend Space Sample '{sampleId}' producer '{producerId}' is outside the target Animation Channel.");
                if (producer.SourceClips.Count != 1 || !producer.SourceClips[0].Clip)
                    throw new InvalidOperationException($"Blend Space Sample '{sampleId}' producer must resolve exactly one AnimationClip.");
                if (!sampleIds.Add(sampleId) || !float.IsFinite(sample.position) || !positions.Add(sample.position) ||
                    sample.position < request.axisMinimum || sample.position > request.axisMaximum)
                    throw new InvalidOperationException($"Blend Space Sample '{sampleId}' identity or position is invalid or duplicated.");
                if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), sample.role) ||
                    !float.IsFinite(sample.stationaryNormalizedTime) || sample.stationaryNormalizedTime < 0f ||
                    sample.stationaryNormalizedTime > 1f)
                    throw new InvalidOperationException($"Blend Space Sample '{sampleId}' role or stationary time is invalid.");
                result[i] = new SampleBinding
                {
                    Request = sample,
                    Producer = producer,
                    Clip = producer.SourceClips[0]
                };
            }
            return result.OrderBy(value => value.Request.position).ToArray();
        }

        static PosePath ResolvePosePath(CharacterPoseGraphData graph, AnimationChannelId channelId)
        {
            CharacterPoseNodeDefinition selection = graph.Nodes.SingleOrDefault(value =>
                value != null && value.Kind == CharacterPoseNodeKind.AnimationSelectionInput &&
                value.AnimationChannelId == channelId)
                ?? throw new InvalidOperationException($"Pose Graph has no unique Selection Input for '{channelId}'.");
            CharacterPoseNodeDefinition marker = RequireSingleTarget(graph, selection, CharacterPoseNodeKind.MarkerSync);
            CharacterPoseNodeDefinition player = RequireSingleTarget(
                graph,
                marker,
                CharacterPoseNodeKind.SelectedPosePlayer,
                CharacterPoseNodeKind.BlendSpacePlayer);
            CharacterPoseNodeDefinition inertialization = RequireSingleTarget(
                graph,
                player,
                CharacterPoseNodeKind.Inertialization);
            return new PosePath
            {
                Selection = selection,
                Marker = marker,
                Player = player,
                Inertialization = inertialization
            };
        }

        static CharacterPoseNodeDefinition RequireSingleTarget(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition source,
            params CharacterPoseNodeKind[] allowed)
        {
            CharacterPoseNodeDefinition[] targets = graph.Edges
                .Where(value => value != null && value.SourceNodeId == source.NodeId)
                .Select(value => graph.Nodes.Single(node => node.NodeId == value.TargetNodeId))
                .Where(value => allowed.Contains(value.Kind))
                .Distinct()
                .ToArray();
            if (targets.Length != 1)
                throw new InvalidOperationException($"Pose Node '{source.NodeId}' has no unique '{string.Join("/", allowed)}' target.");
            return targets[0];
        }

        static void ConfigureBlendSpace(Context context, CharacterAnimationBlendSpaceAsset asset)
        {
            CharacterAnimationBlendSpaceAuthoringService.Initialize(
                asset,
                new CharacterAnimationBlendSpaceId(context.Request.blendSpaceId));
            CharacterAnimationBlendSpaceAuthoringService.SetRig(asset, context.Rig);
            CharacterAnimationBlendSpaceAuthoringService.SetMode(asset, CharacterAnimationBlendSpaceMode.Linear1D);
            CharacterAnimationBlendSpaceAuthoringService.SetAxis(
                asset,
                0,
                context.AxisParameterId,
                context.Request.axisUnit,
                context.Request.axisMinimum,
                context.Request.axisMaximum);
            for (int i = 0; i < context.Samples.Length; i++)
            {
                SampleBinding sample = context.Samples[i];
                var sampleId = new CharacterAnimationBlendSpaceSampleId(sample.Request.sampleId);
                CharacterAnimationBlendSpaceAuthoringService.CreateSample(
                    asset,
                    sampleId,
                    new Vector2(sample.Request.position, 0f));
                CharacterAnimationBlendSpaceAuthoringService.SetSampleClip(asset, sampleId, sample.Clip.Clip);
                CharacterAnimationBlendSpaceAuthoringService.SetSampleRole(
                    asset,
                    sampleId,
                    sample.Request.role,
                    sample.Request.stationaryNormalizedTime);
            }
            CharacterAnimationBlendSpaceAuthoringService.SetPhase(
                asset,
                CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase,
                default);
            CharacterAnimationBlendSpacePoseParameterPolicy[] policies = context.PoseGraph.Graph.Parameters
                .Where(value => value != null && !value.ParameterId.Equals(context.AxisParameterId))
                .Select(value => new CharacterAnimationBlendSpacePoseParameterPolicy(
                    value.ParameterId,
                    CharacterAnimationBlendSpaceParameterPolicy.Unavailable))
                .ToArray();
            CharacterAnimationBlendSpaceAuthoringService.ReplacePoseParameterPolicies(asset, policies);
            CharacterAnimationBlendSpaceAuthoringService.SetPreview(asset, Vector2.zero, 0f);
        }

        static void ConfigurePoseGraph(Context context)
        {
            CharacterPoseGraphData graph = context.PoseGraph.Graph;
            CharacterPoseParameterDeclaration[] parameters = graph.Parameters
                .Where(value => value != null && !value.ParameterId.Equals(context.AxisParameterId))
                .Concat(new[]
                {
                    new CharacterPoseParameterDeclaration(
                        context.AxisParameterId,
                        PoseParameterValueType.Float,
                        0f,
                        context.Request.axisUnit)
                })
                .OrderBy(value => value.ParameterId)
                .ToArray();
            CharacterPresentationPoseGraphAuthoringService.ConfigureParameters(context.PoseGraph, parameters);
            ExtendPoseParameterPolicies(context, parameters);
            ExtendInertializationPolicies(context, parameters);

            Vector2 playerPosition = context.Path.Player.Position;
            CharacterPresentationPoseGraphAuthoringService.DeleteSelection(
                context.PoseGraph,
                graph,
                new[] { context.Path.Player.NodeId },
                Array.Empty<string>());
            CharacterPoseNodeDefinition player = CharacterPresentationPoseGraphAuthoringService.CreateNode(
                context.PoseGraph,
                graph,
                CharacterPoseNodeKind.BlendSpacePlayer,
                playerPosition);
            CharacterPresentationPoseGraphAuthoringService.RenameNode(
                context.PoseGraph,
                graph,
                player.NodeId,
                "Locomotion Blend Space Player");
            ConfigureNode(context.PoseGraph, graph, player.NodeId, blendSpaceInputRangePolicy: CharacterAnimationBlendSpaceInputRangePolicy.Clamp);
            CharacterPoseNodeDefinition parameter = CharacterPresentationPoseGraphAuthoringService.CreateNode(
                context.PoseGraph,
                graph,
                CharacterPoseNodeKind.ProgramParameterInput,
                playerPosition + new Vector2(-10f, -190f));
            CharacterPresentationPoseGraphAuthoringService.RenameNode(
                context.PoseGraph,
                graph,
                parameter.NodeId,
                "Motor Planar Speed");
            ConfigureNode(context.PoseGraph, graph, parameter.NodeId, parameterId: context.AxisParameterId);

            player = graph.Nodes.Single(value => value.NodeId == player.NodeId);
            parameter = graph.Nodes.Single(value => value.NodeId == parameter.NodeId);
            CharacterPoseNodeDefinition marker = graph.Nodes.Single(value => value.NodeId == context.Path.Marker.NodeId);
            CharacterPoseNodeDefinition inertialization = graph.Nodes.Single(value => value.NodeId == context.Path.Inertialization.NodeId);
            Connect(context.PoseGraph, graph, marker, CharacterPosePortKind.AnimationSelection, 0, player, CharacterPosePortKind.AnimationSelection, 0);
            Connect(context.PoseGraph, graph, parameter, CharacterPosePortKind.Parameter, 0, player, CharacterPosePortKind.Parameter, 0);
            Connect(context.PoseGraph, graph, player, CharacterPosePortKind.Pose, 0, inertialization, CharacterPosePortKind.Pose, 0);
        }

        static void ExtendPoseParameterPolicies(
            Context context,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            CharacterPoseGraphData graph = context.PoseGraph.Graph;
            PoseParameterId[] ids = parameters.Select(value => value.ParameterId).ToArray();
            CharacterPoseNodeDefinition[] resolvers = graph.Nodes
                .Where(value => value != null && value.Kind == CharacterPoseNodeKind.PoseParameterResolve)
                .ToArray();
            for (int i = 0; i < resolvers.Length; i++)
            {
                CharacterPoseNodeDefinition node = resolvers[i];
                var existing = node.ParameterPolicies.ToDictionary(value => value.ParameterId, value => value.Policy);
                CharacterPoseParameterPolicy[] policies = ids.Select(value => new CharacterPoseParameterPolicy(
                    value,
                    existing.TryGetValue(value, out PoseParameterResolvePolicy policy)
                        ? policy
                        : PoseParameterResolvePolicy.Base)).ToArray();
                ConfigureNode(context.PoseGraph, graph, node.NodeId, parameterPolicies: policies, preserve: node);
            }
        }

        static void ExtendInertializationPolicies(
            Context context,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            CharacterPoseInertializationPolicy[] policies = context.PoseGraph.Graph.Nodes
                .Where(value => value != null && value.Kind == CharacterPoseNodeKind.Inertialization && value.InertializationPolicy)
                .Select(value => value.InertializationPolicy)
                .Distinct()
                .ToArray();
            for (int i = 0; i < policies.Length; i++)
            {
                CharacterPoseInertializationPolicy policy = policies[i];
                Undo.RecordObject(policy, "Extend Pose Inertialization Parameters");
                CharacterPoseInertializationRule defaultRule = CloneRule(policy.DefaultRule, parameters);
                CharacterPoseInertializationOverride[] overrides = policy.Overrides.Select(value =>
                {
                    var clone = new CharacterPoseInertializationOverride();
                    clone.Configure(
                        value.SourceProducerIdentity,
                        value.TargetProducerIdentity,
                        CloneRule(value.Rule, parameters));
                    return clone;
                }).ToArray();
                policy.Configure(
                    policy.PolicyId,
                    Guid.NewGuid().ToString("N"),
                    defaultRule,
                    overrides,
                    context.Rig);
                EditorUtility.SetDirty(policy);
            }
        }

        static CharacterPoseInertializationRule CloneRule(
            CharacterPoseInertializationRule source,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters)
        {
            var modes = source.ParameterFilters.ToDictionary(value => value.ParameterId, value => value.Mode);
            CharacterPoseParameterInertializationFilter[] filters = parameters.Select(value =>
                new CharacterPoseParameterInertializationFilter(
                    value.ParameterId,
                    modes.TryGetValue(value.ParameterId, out PoseParameterInertializationMode mode)
                        ? mode
                        : PoseParameterInertializationMode.Snap)).ToArray();
            var result = new CharacterPoseInertializationRule();
            result.Configure(
                source.Mode,
                source.DurationSeconds,
                source.Curve,
                source.BlendProfile,
                filters);
            return result;
        }

        static void ConfigureProducerBindings(Context context, CharacterAnimationBlendSpaceAsset blendSpace)
        {
            for (int i = 0; i < context.ChannelProducers.Length; i++)
            {
                CharacterAnimationPresentationAuthoringService.ConfigureBlendSpaceProducerBinding(
                    context.Profile,
                    context.Definition,
                    context.ChannelProducers[i].ProducerId,
                    blendSpace);
            }
        }

        static void RemoveOrphanedMigrationAsset(Context context, string assetPath)
        {
            CharacterAnimationBlendSpaceAsset existing =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendSpaceAsset>(assetPath);
            if (!existing)
                return;
            bool referenced = context.Profile.ProducerBindings.Any(value =>
                value != null && ReferenceEquals(value.BlendSpaceSource, existing));
            if (referenced || context.Path.Player.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                throw new InvalidOperationException($"Blend Space migration target '{assetPath}' is already part of the formal configuration.");
            if (!AssetDatabase.DeleteAsset(assetPath))
                throw new InvalidOperationException($"Orphaned Blend Space migration asset '{assetPath}' could not be removed.");
        }

        static CharacterAnimationBlendSpaceAsset RequireMigrated(Context context)
        {
            if (context.Path.Player.Kind != CharacterPoseNodeKind.BlendSpacePlayer)
                throw new InvalidOperationException($"Animation Channel '{context.ChannelId}' is not connected to BlendSpacePlayer.");
            CharacterAnimationBlendSpaceAsset[] assets = context.ChannelProducers
                .Select(value => context.Profile.FindProducerBinding(value.ProducerId))
                .Where(value => value != null && value.SourceKind == AnimationPoseSourceKind.BlendSpace && value.BlendSpaceSource)
                .Select(value => value.BlendSpaceSource)
                .Distinct()
                .ToArray();
            if (assets.Length != 1 || context.ChannelProducers.Any(value =>
                    context.Profile.FindProducerBinding(value.ProducerId)?.SourceKind != AnimationPoseSourceKind.BlendSpace))
                throw new InvalidOperationException($"Animation Channel '{context.ChannelId}' does not have one complete Blend Space binding.");
            CharacterAnimationBlendSpaceAsset asset = assets[0];
            if (!string.Equals(AssetDatabase.GetAssetPath(asset), context.Request.blendSpaceAssetPath, StringComparison.Ordinal) ||
                !string.Equals(asset.BlendSpaceId.Value, context.Request.blendSpaceId, StringComparison.Ordinal) ||
                asset.Mode != CharacterAnimationBlendSpaceMode.Linear1D ||
                !asset.XAxis.ParameterId.Equals(context.AxisParameterId) ||
                !string.Equals(asset.XAxis.Unit, context.Request.axisUnit, StringComparison.Ordinal) ||
                !asset.XAxis.Minimum.Equals(context.Request.axisMinimum) ||
                !asset.XAxis.Maximum.Equals(context.Request.axisMaximum) ||
                asset.Samples.Count != context.Samples.Length)
                throw new InvalidOperationException("Published Blend Space authoring does not match the explicit migration request.");
            for (int i = 0; i < context.Samples.Length; i++)
            {
                SampleBinding expected = context.Samples[i];
                CharacterAnimationBlendSpaceSample sample = asset.FindSample(
                    new CharacterAnimationBlendSpaceSampleId(expected.Request.sampleId));
                if (sample == null || !ReferenceEquals(sample.Clip, expected.Clip.Clip) ||
                    !sample.Position.x.Equals(expected.Request.position) || sample.Position.y != 0f ||
                    sample.Role != expected.Request.role ||
                    !sample.StationaryNormalizedTime.Equals(
                        expected.Request.role == CharacterAnimationBlendSpaceSampleRole.StationaryPose
                            ? expected.Request.stationaryNormalizedTime
                            : 0f))
                    throw new InvalidOperationException($"Blend Space Sample '{expected.Request.sampleId}' does not match the explicit migration request.");
            }
            return asset;
        }

        static void ValidateAuthoring(Context context, CharacterAnimationBlendSpaceAsset blendSpace)
        {
            CharacterAnimationBlendSpaceValidationReport blendSpaceReport =
                CharacterAnimationBlendSpaceValidator.Validate(blendSpace);
            if (!blendSpaceReport.IsValid)
                throw new InvalidOperationException(string.Join("\n", blendSpaceReport.Issues.Select(value => value.ToString())));
            var profileErrors = new List<string>();
            context.Profile.CollectConfigurationErrors(profileErrors);
            if (profileErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", profileErrors));
            AnimationChannelId[] channels = CharacterAnimationPresentationAuthoringService
                .DiscoverProducerTracks(context.Definition)
                .Select(value => value.AnimationChannelId)
                .Where(value => value.IsValid)
                .Distinct()
                .ToArray();
            CharacterPoseGraphValidationReport graphReport = CharacterPresentationPoseGraphValidator.Validate(
                context.PoseGraph,
                context.Rig,
                channels);
            if (!graphReport.IsValid)
            {
                var errors = new List<string>();
                graphReport.CopyMessagesTo(errors);
                throw new InvalidOperationException(string.Join("\n", errors));
            }
        }

        static CharacterSimulationBuildResult BuildTargets(Context context)
        {
            var request = new CharacterSimulationBuildRequest(
                context.Definition,
                CharacterSimulationBuildPublicationMode.Publish,
                new ICharacterSimulationTargetBuildAdapter[]
                {
                    CharacterSimulationTargetCatalog.Float32(context.Definition),
                    new FixedCharacterSimulationTargetBuildAdapter(context.Request.fixedProgramWrapperPath)
                });
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(request);
            if (result.IsValid)
                return result;
            throw new InvalidOperationException(string.Join("\n", result.Report.Messages.Select(value => value.ToString())));
        }

        static string[] DeleteOldSources(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<string> paths)
        {
            var referenced = new HashSet<string>(profile.ProducerBindings
                .Where(value => value != null && value.Source)
                .Select(value => AssetDatabase.GetAssetPath(value.Source)), StringComparer.Ordinal);
            var retained = new List<string>();
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (referenced.Contains(path) || !AssetDatabase.DeleteAsset(path))
                    retained.Add(path);
            }
            return retained.ToArray();
        }

        static CharacterAnimationBlendSpaceMigrationInspection CreateInspection(Context context)
        {
            CharacterAnimationBlendSpaceAsset current = context.ChannelProducers
                .Select(value => context.Profile.FindProducerBinding(value.ProducerId)?.BlendSpaceSource)
                .Where(value => value)
                .Distinct()
                .SingleOrDefault();
            bool allBlendSpace = current && context.ChannelProducers.All(value =>
            {
                AnimationProducerPresentationBinding binding = context.Profile.FindProducerBinding(value.ProducerId);
                return binding != null && binding.SourceKind == AnimationPoseSourceKind.BlendSpace &&
                       ReferenceEquals(binding.BlendSpaceSource, current);
            });
            return new CharacterAnimationBlendSpaceMigrationInspection
            {
                definitionPath = AssetDatabase.GetAssetPath(context.Definition),
                profilePath = AssetDatabase.GetAssetPath(context.Profile),
                poseGraphPath = AssetDatabase.GetAssetPath(context.PoseGraph),
                rigPath = AssetDatabase.GetAssetPath(context.Rig),
                projectionPath = AssetDatabase.GetAssetPath(context.Definition.PresentationProjection),
                animationChannelId = context.ChannelId.Value,
                currentPlayerKind = context.Path.Player.Kind.ToString(),
                currentPlayerNodeId = context.Path.Player.NodeId.Value,
                inertializationNodeId = context.Path.Inertialization.NodeId.Value,
                blendSpaceAssetPath = current ? AssetDatabase.GetAssetPath(current) : context.Request.blendSpaceAssetPath,
                blendSpaceId = current ? current.BlendSpaceId.Value : context.Request.blendSpaceId,
                axisParameterId = current ? current.XAxis.ParameterId.Value : context.AxisParameterId.Value,
                axisUnit = current ? current.XAxis.Unit : context.Request.axisUnit,
                axisMinimum = current ? current.XAxis.Minimum : context.Request.axisMinimum,
                axisMaximum = current ? current.XAxis.Maximum : context.Request.axisMaximum,
                producers = context.ChannelProducers.Select(value =>
                {
                    AnimationProducerPresentationBinding binding = context.Profile.FindProducerBinding(value.ProducerId);
                    return new CharacterAnimationBlendSpaceMigrationProducerInspection
                    {
                        producerIdentity = value.ProgramProducerIdentity,
                        displayName = value.DisplayName,
                        sourceKind = binding?.SourceKind.ToString() ?? "Missing",
                        sourcePath = binding != null && binding.Source
                            ? AssetDatabase.GetAssetPath(binding.Source)
                            : binding != null && binding.BlendSpaceSource
                                ? AssetDatabase.GetAssetPath(binding.BlendSpaceSource)
                                : string.Empty,
                        clipPaths = value.SourceClips.Select(clip => AssetDatabase.GetAssetPath(clip.Clip)).ToArray()
                    };
                }).ToArray(),
                samples = context.Samples.Select(value => new CharacterAnimationBlendSpaceMigrationSampleInspection
                {
                    sampleId = value.Request.sampleId,
                    producerIdentity = value.Producer.ProgramProducerIdentity,
                    clipPath = AssetDatabase.GetAssetPath(value.Clip.Clip),
                    position = value.Request.position,
                    role = value.Request.role.ToString()
                }).ToArray(),
                readyForMigration = context.Path.Player.Kind == CharacterPoseNodeKind.SelectedPosePlayer,
                migrated = context.Path.Player.Kind == CharacterPoseNodeKind.BlendSpacePlayer && allBlendSpace
            };
        }

        static void ConfigureNode(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseGraphData graph,
            PoseNodeId nodeId,
            PoseParameterId parameterId = default,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp,
            CharacterPoseParameterPolicy[] parameterPolicies = null,
            CharacterPoseNodeDefinition preserve = null)
        {
            CharacterPoseNodeDefinition node = preserve ?? graph.Nodes.Single(value => value.NodeId == nodeId);
            CharacterPresentationPoseGraphAuthoringService.ConfigureNode(
                asset,
                graph,
                nodeId,
                node.AnimationChannelId,
                node.ProgramProducerId,
                parameterId.IsValid ? parameterId : node.ParameterId,
                node.SelectionAvailability,
                blendSpaceInputRangePolicy,
                node.BlendPolicy,
                node.InertializationPolicy,
                node.BoneMask,
                node.Weight,
                parameterPolicies ?? node.ParameterPolicies.ToArray(),
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

        static void Connect(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition source,
            CharacterPosePortKind sourceKind,
            int sourceIndex,
            CharacterPoseNodeDefinition target,
            CharacterPosePortKind targetKind,
            int targetIndex)
        {
            CharacterPosePortDefinition sourcePort = source.Ports
                .Where(value => value.Kind == sourceKind && value.Direction == CharacterPosePortDirection.Output)
                .ElementAt(sourceIndex);
            CharacterPosePortDefinition targetPort = target.Ports
                .Where(value => value.Kind == targetKind && value.Direction == CharacterPosePortDirection.Input)
                .ElementAt(targetIndex);
            CharacterPresentationPoseGraphAuthoringService.Connect(
                asset,
                graph,
                source.NodeId,
                sourcePort.PortId,
                target.NodeId,
                targetPort.PortId);
        }

        static string NormalizeAssetPath(string value, string extension)
        {
            string path = (value ?? string.Empty).Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Asset path '{path}' must be an explicit Assets/...{extension} path.");
            return path;
        }
    }
}
