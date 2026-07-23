using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    public sealed class AnimationProducerChannelMigration
    {
        public string timelineAuthoringId = string.Empty;
        public string trackAuthoringId = string.Empty;
        public string animationChannelId = string.Empty;
    }

    [Serializable]
    public sealed class CharacterAnimationPresentationMigrationRequest
    {
        public string assetDirectory = string.Empty;
        public string assetPrefix = string.Empty;
        public string rigId = string.Empty;
        public string targetPrefabPath = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
        public AnimationProducerChannelMigration[] producers = Array.Empty<AnimationProducerChannelMigration>();
    }

    public sealed class CharacterAnimationPresentationMigrationInspection
    {
        public string definitionPath = string.Empty;
        public string profilePath = string.Empty;
        public string assetDirectory = string.Empty;
        public string[] prefabPaths = Array.Empty<string>();
        public string[] producerIdentities = Array.Empty<string>();
        public int boneCount;
        public string leftFootBoneId = string.Empty;
        public string rightFootBoneId = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
    }

    public sealed class CharacterAnimationPresentationMigrationResult
    {
        public CharacterAnimationPresentationMigrationInspection inspection;
        public string rigPath = string.Empty;
        public string poseGraphPath = string.Empty;
        public string actionBlendPolicyPath = string.Empty;
        public string locomotionInertializationPolicyPath = string.Empty;
        public string[] sourcePaths = Array.Empty<string>();
        public bool buildSucceeded;
    }

    public sealed class CharacterAnimationPresentationTargetBuildResult
    {
        public string float32ProgramWrapperPath = string.Empty;
        public string fixedProgramWrapperPath = string.Empty;
        public string projectionPath = string.Empty;
        public bool buildSucceeded;
    }

    public static class CharacterAnimationPresentationMigrationAuthoringService
    {
        const string BaseLocomotionChannel = "BaseLocomotion";
        const string FullBodyActionChannel = "FullBodyAction";

        sealed class PrefabRigLayout
        {
            public string Path;
            public string HostPath;
            public DeterministicRollbackCharacterHost Host;
            public Transform VisualRoot;
            public Animator Animator;
            public CharacterFootPlacementProfile FootPlacementProfile;
            public CharacterFootPlacementRigCalibration Calibration;
            public Transform[] Bones;
            public string[] BonePaths;
            public AnimationBoneId[] BoneIds;
            public string LeftFootPath;
            public string RightFootPath;
            public AnimationBoneId LeftFootBoneId;
            public AnimationBoneId RightFootBoneId;
        }

        sealed class MigrationContext
        {
            public CharacterPipelineDefinition Definition;
            public CharacterAnimationPresentationProfile Profile;
            public CharacterAnimationPresentationMigrationRequest Request;
            public IReadOnlyList<AnimationProducerAuthoringEntry> Producers;
            public Dictionary<AnimationProducerId, AnimationChannelId> Channels;
            public PrefabRigLayout[] Prefabs;
            public CharacterAnimationPresentationMigrationInspection Inspection;
        }

        public static CharacterAnimationPresentationMigrationInspection Inspect(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationMigrationRequest request)
        {
            return BuildContext(definition, request, true).Inspection;
        }

        public static CharacterAnimationPresentationMigrationResult Apply(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationMigrationRequest request)
        {
            MigrationContext context = BuildContext(definition, request, true);
            string prefix = context.Request.assetPrefix.Trim();
            string directory = NormalizeAssetPath(context.Request.assetDirectory);
            string rigPath = AssetPath(directory, prefix + "AnimationRigDefinition.asset");
            string maskPath = AssetPath(directory, prefix + "FullBodyActionMask.asset");
            string blendProfilePath = AssetPath(directory, prefix + "ActionBlendProfile.asset");
            string inertialProfilePath = AssetPath(directory, prefix + "LocomotionInertialBlendProfile.asset");
            string blendPolicyPath = AssetPath(directory, prefix + "ActionBlendPolicy.asset");
            string inertialPolicyPath = AssetPath(directory, prefix + "LocomotionInertializationPolicy.asset");
            string poseGraphPath = AssetPath(directory, prefix + "PresentationPoseGraph.asset");
            var reservedPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                rigPath,
                maskPath,
                blendProfilePath,
                inertialProfilePath,
                blendPolicyPath,
                inertialPolicyPath,
                poseGraphPath
            };
            string[] sourcePaths = BuildSourcePaths(context, directory, prefix, reservedPaths);
            string[] migrationPaths = reservedPaths.Concat(sourcePaths).ToArray();
            RequirePathsAvailable(migrationPaths);

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Character Animation Presentation");
            try
            {
                ConfigureProducerChannels(context);
                CharacterAnimationRigDefinition rig = CreateRig(context, rigPath);
                CharacterAnimationBoneMaskAsset mask = CreateFullBodyMask(rig, maskPath);
                CharacterAnimationBlendProfile blendProfile = CreateBlendProfile(
                    rig, rig.RigId + ".action-blend-profile", blendProfilePath);
                CharacterAnimationBlendProfile inertialProfile = CreateBlendProfile(
                    rig, rig.RigId + ".locomotion-inertial-profile", inertialProfilePath);
                CharacterAnimationBlendPolicy blendPolicy = CreateBlendPolicy(context, rig, blendProfile, blendPolicyPath);
                CharacterPoseInertializationPolicy inertialPolicy = CreateInertializationPolicy(
                    context, rig, inertialProfile, inertialPolicyPath);
                CharacterPresentationPoseGraphAsset poseGraph = CreatePoseGraph(
                    context,
                    rig,
                    mask,
                    blendPolicy,
                    inertialPolicy,
                    poseGraphPath);
                TransitionAssetBase[] sources = CreateSources(context, sourcePaths);
                ConfigureProfile(context, poseGraph, rig, sources);
                ValidateProfile(context);
                ConfigurePrefabs(context, rig);
                BuildTargets(context);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                return new CharacterAnimationPresentationMigrationResult
                {
                    inspection = context.Inspection,
                    rigPath = rigPath,
                    poseGraphPath = poseGraphPath,
                    actionBlendPolicyPath = blendPolicyPath,
                    locomotionInertializationPolicyPath = inertialPolicyPath,
                    sourcePaths = sourcePaths,
                    buildSucceeded = true
                };
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                for (int pathIndex = 0; pathIndex < migrationPaths.Length; pathIndex++)
                {
                    if (AssetDatabase.LoadMainAssetAtPath(migrationPaths[pathIndex]))
                        AssetDatabase.DeleteAsset(migrationPaths[pathIndex]);
                }
                throw;
            }
        }

        public static CharacterAnimationPresentationTargetBuildResult RebuildTargets(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationMigrationRequest request)
        {
            MigrationContext context = BuildContext(definition, request, true);
            ValidateConfiguredProducerChannels(context);
            ValidateProfile(context);
            BuildTargets(context);
            AssetDatabase.SaveAssets();
            return new CharacterAnimationPresentationTargetBuildResult
            {
                float32ProgramWrapperPath = AssetDatabase.GetAssetPath(context.Definition.SimulationProgram),
                fixedProgramWrapperPath = context.Request.fixedProgramWrapperPath,
                projectionPath = AssetDatabase.GetAssetPath(context.Definition.PresentationProjection),
                buildSucceeded = true
            };
        }

        static MigrationContext BuildContext(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationMigrationRequest request,
            bool requireAssetDirectory)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile
                ? definition.AnimationPresentationProfile
                : throw new InvalidOperationException($"Character Definition '{definition.name}' has no Animation Presentation Profile.");
            string directory = NormalizeAssetPath(request.assetDirectory);
            if (requireAssetDirectory && !AssetDatabase.IsValidFolder(directory))
                throw new InvalidOperationException($"Animation Presentation asset directory '{directory}' does not exist.");
            if (string.IsNullOrWhiteSpace(request.assetPrefix) ||
                request.assetPrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException("Animation Presentation asset prefix is invalid.");
            if (string.IsNullOrWhiteSpace(request.rigId))
                throw new InvalidOperationException("Animation Rig identity is required.");
            request.fixedProgramWrapperPath = NormalizeAssetPath(request.fixedProgramWrapperPath);
            if (!request.fixedProgramWrapperPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Fixed Character Program wrapper must be an explicit Assets/... .asset path.");

            IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                CharacterAnimationPresentationAuthoringService.DiscoverProducerTracks(definition);
            Dictionary<AnimationProducerId, AnimationChannelId> channels = BuildChannelMap(producers, request.producers);
            RequireSourceClips(producers);
            PrefabRigLayout[] prefabs = FindPrefabLayouts(request.targetPrefabPath);
            PrefabRigLayout template = prefabs[0];
            return new MigrationContext
            {
                Definition = definition,
                Profile = profile,
                Request = request,
                Producers = producers,
                Channels = channels,
                Prefabs = prefabs,
                Inspection = new CharacterAnimationPresentationMigrationInspection
                {
                    definitionPath = AssetDatabase.GetAssetPath(definition),
                    profilePath = AssetDatabase.GetAssetPath(profile),
                    assetDirectory = directory,
                    prefabPaths = prefabs.Select(value => string.IsNullOrEmpty(value.HostPath)
                        ? value.Path
                        : value.Path + "::" + value.HostPath).ToArray(),
                    producerIdentities = producers.Select(value => value.ProgramProducerIdentity).ToArray(),
                    boneCount = template.Bones.Length,
                    leftFootBoneId = template.LeftFootBoneId.Value,
                    rightFootBoneId = template.RightFootBoneId.Value,
                    fixedProgramWrapperPath = request.fixedProgramWrapperPath
                }
            };
        }

        static Dictionary<AnimationProducerId, AnimationChannelId> BuildChannelMap(
            IReadOnlyList<AnimationProducerAuthoringEntry> producers,
            IReadOnlyList<AnimationProducerChannelMigration> assignments)
        {
            var result = new Dictionary<AnimationProducerId, AnimationChannelId>();
            for (int i = 0; i < (assignments?.Count ?? 0); i++)
            {
                AnimationProducerChannelMigration assignment = assignments[i]
                    ?? throw new InvalidOperationException($"Animation producer channel assignment #{i} is missing.");
                var producerId = new AnimationProducerId(assignment.timelineAuthoringId, assignment.trackAuthoringId);
                var channelId = new AnimationChannelId(assignment.animationChannelId);
                if (channelId.Value != BaseLocomotionChannel && channelId.Value != FullBodyActionChannel)
                    throw new InvalidOperationException($"Animation producer '{producerId}' uses unsupported migration channel '{channelId}'.");
                if (!result.TryAdd(producerId, channelId))
                    throw new InvalidOperationException($"Animation producer '{producerId}' has duplicate channel assignments.");
            }
            var expected = new HashSet<AnimationProducerId>(producers.Select(value => value.ProducerId));
            if (!expected.SetEquals(result.Keys))
                throw new InvalidOperationException("Animation producer channel assignments do not exactly cover the Definition topology.");
            if (!result.Values.Any(value => value.Value == BaseLocomotionChannel) ||
                !result.Values.Any(value => value.Value == FullBodyActionChannel))
                throw new InvalidOperationException("Animation producer migration requires both BaseLocomotion and FullBodyAction channels.");
            return result;
        }

        static PrefabRigLayout[] FindPrefabLayouts(string requestedPath)
        {
            string path = NormalizeAssetPath(requestedPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Animation Presentation target Prefab '{path}' does not exist.");
            DeterministicRollbackCharacterHost[] hosts = prefab.GetComponentsInChildren<DeterministicRollbackCharacterHost>(true);
            if (hosts.Length != 1)
                throw new InvalidOperationException($"Animation Presentation target Prefab '{path}' requires exactly one Deterministic Rollback Character Host.");
            return new[] { BuildPrefabLayout(path, hosts[0]) };
        }

        static PrefabRigLayout BuildPrefabLayout(string path, DeterministicRollbackCharacterHost host)
        {
            if (!host.Animancer || !host.Animancer.Animator || !host.VisualRoot ||
                host.Animancer.Animator.transform != host.VisualRoot)
                throw new InvalidOperationException($"Prefab '{path}' has an incomplete Animancer VisualRoot binding.");
            if (!host.FootPlacement || !host.FootPlacement.Profile || !host.FootPlacement.Rig ||
                !host.FootPlacement.Rig.Calibration)
                throw new InvalidOperationException($"Prefab '{path}' has an incomplete Foot Placement binding.");
            CharacterFootPlacementRigBinding footRig = host.FootPlacement.Rig.BuildBinding();
            var bones = new List<Transform>();
            CollectAnimationRigTransforms(host.VisualRoot, bones);
            string[] bonePaths = bones.Select(value => AnimationUtility.CalculateTransformPath(value, host.VisualRoot)).ToArray();
            if (bonePaths.Distinct(StringComparer.Ordinal).Count() != bonePaths.Length)
                throw new InvalidOperationException($"Prefab '{path}' has duplicated Animation Rig transform paths.");
            var boneIds = bonePaths.Select(BoneId).ToArray();
            var pathIndices = bonePaths.Select((value, index) => (value, index))
                .ToDictionary(value => value.value, value => value.index, StringComparer.Ordinal);
            string leftPath = AnimationUtility.CalculateTransformPath(footRig.LeftAnkle, host.VisualRoot);
            string rightPath = AnimationUtility.CalculateTransformPath(footRig.RightAnkle, host.VisualRoot);
            if (!pathIndices.TryGetValue(leftPath, out int leftIndex) || !pathIndices.TryGetValue(rightPath, out int rightIndex))
                throw new InvalidOperationException($"Prefab '{path}' Foot Placement ankles are outside the Animation Rig.");
            return new PrefabRigLayout
            {
                Path = path,
                HostPath = AnimationUtility.CalculateTransformPath(host.transform, host.transform.root),
                Host = host,
                VisualRoot = host.VisualRoot,
                Animator = host.Animancer.Animator,
                FootPlacementProfile = host.FootPlacement.Profile,
                Calibration = host.FootPlacement.Rig.Calibration,
                Bones = bones.ToArray(),
                BonePaths = bonePaths,
                BoneIds = boneIds,
                LeftFootPath = leftPath,
                RightFootPath = rightPath,
                LeftFootBoneId = boneIds[leftIndex],
                RightFootBoneId = boneIds[rightIndex]
            };
        }

        static void RequireSourceClips(IReadOnlyList<AnimationProducerAuthoringEntry> producers)
        {
            for (int producerIndex = 0; producerIndex < producers.Count; producerIndex++)
            {
                AnimationProducerAuthoringEntry producer = producers[producerIndex];
                if (producer.SourceClips.Count == 0 || producer.SourceClips.Any(value => !value.Clip))
                    throw new InvalidOperationException($"Animation producer '{producer.ProgramProducerIdentity}' requires valid source clips for migration.");
            }
        }

        static void ConfigureProducerChannels(MigrationContext context)
        {
            for (int i = 0; i < context.Producers.Count; i++)
            {
                AnimationProducerAuthoringEntry producer = context.Producers[i];
                UnityEngine.Object serializedOwner = producer.Timeline.Timeline.SerializedOwner;
                Undo.RecordObject(serializedOwner, "Configure Animation Producer Channel");
                producer.Track.SetAnimationChannelId(context.Channels[producer.ProducerId]);
                producer.Timeline.Timeline.Init();
                EditorUtility.SetDirty(serializedOwner);
            }
        }

        static void ValidateConfiguredProducerChannels(MigrationContext context)
        {
            for (int i = 0; i < context.Producers.Count; i++)
            {
                AnimationProducerAuthoringEntry producer = context.Producers[i];
                AnimationChannelId expected = context.Channels[producer.ProducerId];
                if (producer.Track.AnimationChannelId != expected)
                {
                    throw new InvalidOperationException(
                        $"Animation producer '{producer.ProgramProducerIdentity}' is bound to '{producer.Track.AnimationChannelId}' instead of '{expected}'.");
                }
            }
        }

        static void BuildTargets(MigrationContext context)
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
                return;
            string message = string.Join(
                "\n",
                result.Report.Messages.Select(value => value.ToString()));
            throw new InvalidOperationException(
                $"Character Definition '{context.Definition.name}' failed to build Presentation targets.\n{message}");
        }

        static CharacterAnimationRigDefinition CreateRig(MigrationContext context, string path)
        {
            PrefabRigLayout template = context.Prefabs[0];
            var bones = new CharacterAnimationBoneDefinition[template.Bones.Length];
            var indices = template.Bones.Select((value, index) => (value, index)).ToDictionary(value => value.value, value => value.index);
            for (int i = 0; i < bones.Length; i++)
            {
                Transform transform = template.Bones[i];
                int parentIndex = transform == template.VisualRoot ? -1 : indices[transform.parent];
                bones[i] = new CharacterAnimationBoneDefinition(
                    template.BoneIds[i],
                    parentIndex,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale);
            }
            CharacterAnimationRigDefinition rig = CreateAsset<CharacterAnimationRigDefinition>(path);
            rig.Configure(
                context.Request.rigId,
                Guid.NewGuid().ToString("N"),
                bones,
                CharacterAnimationRootBonePolicy.ExcludeSourceRoot,
                CharacterAnimationScalePolicy.PreserveReferenceScale,
                template.LeftFootBoneId,
                template.RightFootBoneId);
            EditorUtility.SetDirty(rig);
            return rig;
        }

        static CharacterAnimationBoneMaskAsset CreateFullBodyMask(CharacterAnimationRigDefinition rig, string path)
        {
            CharacterAnimationBoneMaskAsset mask = CreateAsset<CharacterAnimationBoneMaskAsset>(path);
            mask.Configure(
                rig.RigId + ".full-body-action",
                rig,
                rig.Bones.Select(value => new CharacterAnimationBoneWeight(value.BoneId, 1f)).ToArray());
            EditorUtility.SetDirty(mask);
            return mask;
        }

        static CharacterAnimationBlendProfile CreateBlendProfile(
            CharacterAnimationRigDefinition rig,
            string profileId,
            string path)
        {
            CharacterAnimationBlendProfile profile = CreateAsset<CharacterAnimationBlendProfile>(path);
            profile.Configure(profileId, rig, 1f, Array.Empty<CharacterAnimationBoneDurationMultiplier>());
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static CharacterAnimationBlendPolicy CreateBlendPolicy(
            MigrationContext context,
            CharacterAnimationRigDefinition rig,
            CharacterAnimationBlendProfile profile,
            string path)
        {
            string[] actionProducers = context.Producers
                .Where(value => context.Channels[value.ProducerId].Value == FullBodyActionChannel)
                .Select(value => value.ProgramProducerIdentity)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var overrides = new List<CharacterAnimationBlendTransitionOverride>();
            for (int target = 0; target < actionProducers.Length; target++)
                overrides.Add(CreateOverride(string.Empty, true, actionProducers[target], false, profile));
            for (int source = 0; source < actionProducers.Length; source++)
            {
                for (int target = 0; target < actionProducers.Length; target++)
                    overrides.Add(CreateOverride(actionProducers[source], false, actionProducers[target], false, profile));
                overrides.Add(CreateOverride(actionProducers[source], false, string.Empty, true, profile));
            }
            var stack = new CharacterAnimationBlendStackPolicy();
            stack.Configure(4, AnimationStoredPosePolicy.CompressOldest, 0.05f, 1f);
            CharacterAnimationBlendPolicy policy = CreateAsset<CharacterAnimationBlendPolicy>(path);
            policy.Configure(
                rig.RigId + ".full-body-action",
                Guid.NewGuid().ToString("N"),
                stack,
                CreateRule(profile),
                overrides.ToArray(),
                rig);
            EditorUtility.SetDirty(policy);
            return policy;
        }

        static CharacterAnimationBlendTransitionOverride CreateOverride(
            string source,
            bool sourceEmpty,
            string target,
            bool targetEmpty,
            CharacterAnimationBlendProfile profile)
        {
            var value = new CharacterAnimationBlendTransitionOverride();
            value.Configure(source, sourceEmpty, target, targetEmpty, CreateRule(profile));
            return value;
        }

        static CharacterAnimationBlendTransitionRule CreateRule(CharacterAnimationBlendProfile profile)
        {
            var rule = new CharacterAnimationBlendTransitionRule();
            rule.Configure(0.16f, new CharacterAnimationBlendCurve(), profile);
            return rule;
        }

        static CharacterPoseInertializationPolicy CreateInertializationPolicy(
            MigrationContext context,
            CharacterAnimationRigDefinition rig,
            CharacterAnimationBlendProfile profile,
            string path)
        {
            CharacterPoseInertializationPolicy policy = CreateAsset<CharacterPoseInertializationPolicy>(path);
            var rule = new CharacterPoseInertializationRule();
            rule.Configure(
                PoseInertializationMode.Inertialize,
                0.18f,
                new CharacterAnimationBlendCurve(),
                profile,
                new[]
                {
                    new CharacterPoseParameterInertializationFilter(
                        AnimationPoseParameterIds.ActionWeight,
                        PoseParameterInertializationMode.Snap),
                    new CharacterPoseParameterInertializationFilter(
                        AnimationPoseParameterIds.FootPlacementWeight,
                        PoseParameterInertializationMode.Inertialize)
                });
            policy.Configure(
                context.Request.rigId + ".base-locomotion",
                Guid.NewGuid().ToString("N"),
                rule,
                Array.Empty<CharacterPoseInertializationOverride>(),
                rig);
            EditorUtility.SetDirty(policy);
            return policy;
        }

        static CharacterPresentationPoseGraphAsset CreatePoseGraph(
            MigrationContext context,
            CharacterAnimationRigDefinition rig,
            CharacterAnimationBoneMaskAsset mask,
            CharacterAnimationBlendPolicy blendPolicy,
            CharacterPoseInertializationPolicy inertialPolicy,
            string path)
        {
            CharacterPresentationPoseGraphAsset asset = CreateAsset<CharacterPresentationPoseGraphAsset>(path);
            CharacterPresentationPoseGraphAuthoringService.ConfigureParameters(asset, new[]
            {
                new CharacterPoseParameterDeclaration(AnimationPoseParameterIds.ActionWeight, PoseParameterValueType.Float, 1f),
                new CharacterPoseParameterDeclaration(AnimationPoseParameterIds.FootPlacementWeight, PoseParameterValueType.Float, 1f)
            });
            CharacterPoseGraphData graph = asset.Graph;
            CharacterPoseNodeDefinition baseSelection = Node(asset, graph, CharacterPoseNodeKind.AnimationSelectionInput, "Base Locomotion Selection", -900f, -160f);
            Configure(asset, graph, baseSelection, animationChannelId: new AnimationChannelId(BaseLocomotionChannel), selectionAvailability: AnimationSelectionAvailabilityPolicy.RequireSelection);
            CharacterPoseNodeDefinition marker = Node(asset, graph, CharacterPoseNodeKind.MarkerSync, "Locomotion Marker Sync", -680f, -160f);
            CharacterPoseNodeDefinition player = Node(asset, graph, CharacterPoseNodeKind.SelectedPosePlayer, "Locomotion Player", -460f, -160f);
            CharacterPoseNodeDefinition inertial = Node(asset, graph, CharacterPoseNodeKind.Inertialization, "Locomotion Inertialization", -240f, -160f);
            Configure(asset, graph, inertial, inertializationPolicy: inertialPolicy);
            CharacterPoseNodeDefinition actionSelection = Node(asset, graph, CharacterPoseNodeKind.AnimationSelectionInput, "Full Body Action Selection", -680f, 180f);
            Configure(asset, graph, actionSelection, animationChannelId: new AnimationChannelId(FullBodyActionChannel), selectionAvailability: AnimationSelectionAvailabilityPolicy.AllowEmpty);
            CharacterPoseNodeDefinition stack = Node(asset, graph, CharacterPoseNodeKind.BlendStack, "Full Body Action Blend Stack", -460f, 180f);
            Configure(asset, graph, stack, blendPolicy: blendPolicy);
            CharacterPoseNodeDefinition actionWeight = Node(asset, graph, CharacterPoseNodeKind.ProgramParameterInput, "Action Weight", -240f, 320f);
            Configure(asset, graph, actionWeight, parameterId: AnimationPoseParameterIds.ActionWeight);
            CharacterPoseNodeDefinition layered = Node(asset, graph, CharacterPoseNodeKind.LayeredBoneBlend, "Base And Full Body Action", 0f, 0f);
            Configure(asset, graph, layered, boneMask: mask);
            CharacterPoseNodeDefinition resolve = Node(asset, graph, CharacterPoseNodeKind.PoseParameterResolve, "Resolve Pose Parameters", 240f, 0f);
            Configure(asset, graph, resolve, parameterPolicies: graph.Parameters.Select(value => new CharacterPoseParameterPolicy(value.ParameterId, PoseParameterResolvePolicy.Weighted)).ToArray());
            CharacterPoseNodeDefinition footWeight = Node(asset, graph, CharacterPoseNodeKind.ProgramParameterInput, "Foot Placement Weight", 240f, 260f);
            Configure(asset, graph, footWeight, parameterId: AnimationPoseParameterIds.FootPlacementWeight);
            CharacterPoseNodeDefinition foot = Node(asset, graph, CharacterPoseNodeKind.FootPlacement, "Foot Placement", 480f, 0f);
            Configure(
                asset,
                graph,
                foot,
                footPlacementProfile: context.Prefabs[0].FootPlacementProfile,
                footPlacementCalibration: context.Prefabs[0].Calibration);
            CharacterPoseNodeDefinition output = Node(asset, graph, CharacterPoseNodeKind.OutputPose, "Output Pose", 720f, 0f);

            Connect(asset, graph, baseSelection, CharacterPosePortKind.AnimationSelection, 0, marker, CharacterPosePortKind.AnimationSelection, 0);
            Connect(asset, graph, marker, CharacterPosePortKind.AnimationSelection, 0, player, CharacterPosePortKind.AnimationSelection, 0);
            Connect(asset, graph, player, CharacterPosePortKind.Pose, 0, inertial, CharacterPosePortKind.Pose, 0);
            Connect(asset, graph, inertial, CharacterPosePortKind.Pose, 0, layered, CharacterPosePortKind.Pose, 0);
            Connect(asset, graph, actionSelection, CharacterPosePortKind.AnimationSelection, 0, stack, CharacterPosePortKind.AnimationSelection, 0);
            Connect(asset, graph, stack, CharacterPosePortKind.Pose, 0, layered, CharacterPosePortKind.Pose, 1);
            Connect(asset, graph, actionWeight, CharacterPosePortKind.Parameter, 0, layered, CharacterPosePortKind.Parameter, 0);
            Connect(asset, graph, layered, CharacterPosePortKind.Pose, 0, resolve, CharacterPosePortKind.Pose, 0);
            Connect(asset, graph, stack, CharacterPosePortKind.Pose, 0, resolve, CharacterPosePortKind.Pose, 1);
            Connect(asset, graph, resolve, CharacterPosePortKind.Pose, 0, foot, CharacterPosePortKind.Pose, 0);
            Connect(asset, graph, footWeight, CharacterPosePortKind.Parameter, 0, foot, CharacterPosePortKind.Parameter, 0);
            Connect(asset, graph, foot, CharacterPosePortKind.Pose, 0, output, CharacterPosePortKind.Pose, 0);

            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(
                asset,
                rig,
                new[] { new AnimationChannelId(BaseLocomotionChannel), new AnimationChannelId(FullBodyActionChannel) });
            if (!report.IsValid)
            {
                var errors = new List<string>();
                report.CopyMessagesTo(errors);
                throw new InvalidOperationException(string.Join("\n", errors));
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static TransitionAssetBase[] CreateSources(MigrationContext context, IReadOnlyList<string> paths)
        {
            var result = new TransitionAssetBase[context.Producers.Count];
            for (int i = 0; i < context.Producers.Count; i++)
            {
                AnimationProducerAuthoringEntry producer = context.Producers[i];
                TransitionAsset source = CreateAsset<TransitionAsset>(paths[i]);
                if (producer.SourceClips.Count == 1)
                {
                    source.Transition = new ClipTransition { Clip = producer.SourceClips[0].Clip };
                }
                else
                {
                    var sequence = new TransitionSequence();
                    sequence.Transitions = producer.SourceClips
                        .Select(value => (ITransition)new ClipTransition { Clip = value.Clip })
                        .ToArray();
                    source.Transition = sequence;
                }
                if (!source.IsValid)
                    throw new InvalidOperationException($"Animation producer '{producer.ProgramProducerIdentity}' created an invalid Animancer source.");
                EditorUtility.SetDirty(source);
                result[i] = source;
            }
            return result;
        }

        static void ConfigureProfile(
            MigrationContext context,
            CharacterPresentationPoseGraphAsset graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<TransitionAssetBase> sources)
        {
            Undo.RecordObject(context.Profile, "Configure Animation Presentation Profile");
            context.Profile.SetPresentationGraph(graph, rig);
            var bindings = new AnimationProducerPresentationBinding[context.Producers.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = new AnimationProducerPresentationBinding();
                bindings[i].ConfigureTimeline(context.Producers[i].ProducerId, sources[i]);
            }
            context.Profile.SetProducerBindings(bindings);
            EditorUtility.SetDirty(context.Profile);
        }

        static void ValidateProfile(MigrationContext context)
        {
            var errors = new List<string>();
            context.Profile.CollectConfigurationErrors(errors);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
            IReadOnlyList<AnimationProducerAuthoringEntry> resolved =
                CharacterAnimationPresentationAuthoringService.DiscoverProducers(context.Profile, context.Definition);
            if (resolved.Count != context.Producers.Count)
                throw new InvalidOperationException("Animation Presentation Profile does not resolve the complete producer topology after migration.");
        }

        static void ConfigurePrefabs(MigrationContext context, CharacterAnimationRigDefinition rig)
        {
            var payload = new CharacterAnimationRigPayload(rig);
            PrefabRigLayout expected = context.Prefabs[0];
            GameObject root = PrefabUtility.LoadPrefabContents(expected.Path);
            try
            {
                Transform hostTransform = string.IsNullOrEmpty(expected.HostPath)
                    ? root.transform
                    : root.transform.Find(expected.HostPath);
                DeterministicRollbackCharacterHost host = hostTransform
                    ? hostTransform.GetComponent<DeterministicRollbackCharacterHost>()
                    : null;
                if (!host)
                    throw new InvalidOperationException($"Prefab '{expected.Path}' Host '{expected.HostPath}' changed while its Animation Rig migration was running.");
                Transform[] bones = expected.BonePaths.Select(path => string.IsNullOrEmpty(path) ? host.VisualRoot : host.VisualRoot.Find(path)).ToArray();
                if (bones.Any(value => !value))
                    throw new InvalidOperationException($"Prefab '{expected.Path}' Host '{expected.HostPath}' changed while its Animation Rig migration was running.");
                CharacterAnimationRigBinding binding = host.VisualRoot.GetComponent<CharacterAnimationRigBinding>();
                if (!binding)
                    binding = Undo.AddComponent<CharacterAnimationRigBinding>(host.VisualRoot.gameObject);
                host.Animancer.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                binding.Configure(host.Animancer.Animator, payload, bones);
                host.ConfigureAnimationRigBinding(binding);
                EditorUtility.SetDirty(host.Animancer.Animator);
                EditorUtility.SetDirty(binding);
                EditorUtility.SetDirty(host);
                PrefabUtility.SaveAsPrefabAsset(root, expected.Path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static string[] BuildSourcePaths(
            MigrationContext context,
            string directory,
            string prefix,
            HashSet<string> reservedPaths)
        {
            var result = new string[context.Producers.Count];
            for (int i = 0; i < result.Length; i++)
            {
                string name = SanitizeFileName(context.Producers[i].Timeline.Timeline.Name + "_" + context.Producers[i].Track.Name);
                string path = AssetPath(directory, prefix + name + "Source.asset");
                if (!reservedPaths.Add(path))
                    throw new InvalidOperationException($"Animation source asset path '{path}' is duplicated.");
                result[i] = path;
            }
            return result;
        }

        static CharacterPoseNodeDefinition Node(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseGraphData graph,
            CharacterPoseNodeKind kind,
            string name,
            float x,
            float y)
        {
            CharacterPoseNodeDefinition node = CharacterPresentationPoseGraphAuthoringService.CreateNode(asset, graph, kind, new Vector2(x, y));
            CharacterPresentationPoseGraphAuthoringService.RenameNode(asset, graph, node.NodeId, name);
            return graph.Nodes.Single(value => value.NodeId == node.NodeId);
        }

        static void Configure(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition node,
            AnimationChannelId animationChannelId = default,
            PoseParameterId parameterId = default,
            AnimationSelectionAvailabilityPolicy selectionAvailability = AnimationSelectionAvailabilityPolicy.RequireSelection,
            CharacterAnimationBlendPolicy blendPolicy = null,
            CharacterPoseInertializationPolicy inertializationPolicy = null,
            CharacterAnimationBoneMaskAsset boneMask = null,
            CharacterPoseParameterPolicy[] parameterPolicies = null,
            CharacterFootPlacementProfile footPlacementProfile = null,
            CharacterFootPlacementRigCalibration footPlacementCalibration = null)
        {
            CharacterPresentationPoseGraphAuthoringService.ConfigureNode(
                asset,
                graph,
                node.NodeId,
                animationChannelId,
                string.Empty,
                parameterId,
                selectionAvailability,
                CharacterAnimationBlendSpaceInputRangePolicy.Clamp,
                blendPolicy,
                inertializationPolicy,
                boneMask,
                1f,
                parameterPolicies ?? Array.Empty<CharacterPoseParameterPolicy>(),
                AnimationAdditiveReferencePoseIds.RigReference,
                AdditiveReferenceSpace.Local,
                AdditiveScalePolicy.Multiply,
                default,
                ModifyBoneReferenceSpace.Local,
                ModifyBoneOperationMask.None,
                Vector3.zero,
                Vector3.zero,
                Vector3.one,
                footPlacementProfile,
                footPlacementCalibration);
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
            CharacterPresentationPoseGraphAuthoringService.Connect(asset, graph, source.NodeId, sourcePort.PortId, target.NodeId, targetPort.PortId);
        }

        static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "Create Animation Presentation Asset");
            return asset;
        }

        static void RequirePathsAvailable(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path))
                    throw new InvalidOperationException($"Animation Presentation migration target '{path}' already exists.");
            }
        }

        static string NormalizeAssetPath(string path)
        {
            string result = (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!result.StartsWith("Assets/", StringComparison.Ordinal) && result != "Assets")
                throw new InvalidOperationException("Animation Presentation asset directory must be an explicit Assets/... path.");
            return result;
        }

        static string AssetPath(string directory, string fileName) => directory + "/" + fileName;

        static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? string.Empty).Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }

        static AnimationBoneId BoneId(string path)
        {
            string source = string.IsNullOrEmpty(path) ? "root" : path;
            string normalized = new string(source.Select(character =>
                char.IsLetterOrDigit(character) || character == '.' || character == '_' || character == '-' || character == '/'
                    ? character
                    : '_').ToArray());
            return new AnimationBoneId("animation-bone/" + normalized);
        }

        static void CollectTransforms(Transform root, List<Transform> destination)
        {
            destination.Add(root);
            for (int i = 0; i < root.childCount; i++)
                CollectTransforms(root.GetChild(i), destination);
        }

        static void CollectAnimationRigTransforms(Transform visualRoot, List<Transform> destination)
        {
            var required = new HashSet<Transform> { visualRoot };
            SkinnedMeshRenderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException($"Animator '{visualRoot.name}' has no Skinned Mesh Renderer bone topology.");
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                var rendererBones = new List<Transform>(renderer.bones);
                if (renderer.rootBone)
                    rendererBones.Add(renderer.rootBone);
                for (int boneIndex = 0; boneIndex < rendererBones.Count; boneIndex++)
                {
                    Transform current = rendererBones[boneIndex];
                    if (!current || current != visualRoot && !current.IsChildOf(visualRoot))
                        throw new InvalidOperationException($"Skinned Mesh Renderer '{renderer.name}' contains a bone outside Animator '{visualRoot.name}'.");
                    while (current)
                    {
                        required.Add(current);
                        if (current == visualRoot)
                            break;
                        current = current.parent;
                    }
                }
            }
            var hierarchy = new List<Transform>();
            CollectTransforms(visualRoot, hierarchy);
            destination.AddRange(hierarchy.Where(required.Contains));
        }
    }
}
