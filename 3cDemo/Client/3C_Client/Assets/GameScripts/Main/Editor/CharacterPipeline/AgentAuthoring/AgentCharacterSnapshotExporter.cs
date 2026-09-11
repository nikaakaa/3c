using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Rules;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentCharacterSnapshotExporter
    {
        public AgentGraphSnapshot Export(CharacterPipelineDefinition definition)
        {
            return ExportSnapshot(definition, AgentSnapshotExportMode.Compact);
        }
        public AgentGraphSnapshot ExportFull(CharacterPipelineDefinition definition)
        {
            return ExportSnapshot(definition, AgentSnapshotExportMode.Full);
        }
        AgentGraphSnapshot ExportSnapshot(CharacterPipelineDefinition definition, AgentSnapshotExportMode mode)
        {

            AgentGraphSnapshot snapshot = new AgentGraphSnapshot();
            snapshot.domain = AgentAuthoringSchema.CharacterControllerDomain;
            snapshot.exportMode = mode.ToString();
            if (!definition)
                return snapshot;

            snapshot.definitionName = definition.name;
            snapshot.definitionAssetPath = AssetDatabase.GetAssetPath(definition);
            snapshot.rootAssetPath = snapshot.definitionAssetPath;
            snapshot.rootIdentity = AssetDatabase.AssetPathToGUID(snapshot.definitionAssetPath);
            snapshot.inputProviderOwnerId = CharacterSkillProviderOwners.Asset(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.InputProfile)));
            snapshot.gameplayProviderOwnerId = CharacterSkillProviderOwners.Asset(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.GameplayEffectProfile)));
            snapshot.controlModuleId = definition.ControlModuleId;
            if (!string.IsNullOrEmpty(snapshot.controlModuleId))
            {
                ICharacterControlModule controlModule =
                    CorinCharacterControlModuleCatalog.Create().Require(
                        new CharacterControlModuleId(snapshot.controlModuleId));
                snapshot.controlSemanticVersion = controlModule.Contract.SemanticVersion;
                snapshot.controlParameters = definition.ControlParameters
                    .Where(value => value != null)
                    .Select(value => new AgentControlParameter
                    {
                        id = value.ParameterId,
                        valueType = value.ValueKind.ToString(),
                        numericValue = value.NumericValue
                    })
                    .ToList();
            }

            ExportBodyMotion(definition.BodyMotionProfile, mode, snapshot);
            ExportInputs(definition.InputProfile, snapshot);
            ExportActionProfiles(definition.ActionProfiles, snapshot);

            var program = definition.SimulationProgram;
            if (program)
            {
                snapshot.programId = program.ProgramId;
                snapshot.sourceRevision = program.SourceRevision;
                snapshot.semanticHash = program.SemanticHash;
                snapshot.numericProfileId = program.NumericProfileId;
                snapshot.targetAbiVersion = program.TargetAbiVersion;
                snapshot.programHash = program.ProgramHash;
                snapshot.layoutHash = program.LayoutHash;
            }
            ExportPresentation(definition, snapshot);

            return snapshot;
        }
        static void ExportBodyMotion(
            CharacterBodyMotionProfile profile,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot)
        {
            if (!profile)
                return;
            string path = AssetDatabase.GetAssetPath(profile);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AgentBodyMotionProfile bodyMotion = snapshot.bodyMotion;
            bodyMotion.assetPath = path;
            bodyMotion.assetGuid = guid;
            bodyMotion.sourceIdentity = $"asset:{guid}";
            bodyMotion.contentRevision = CharacterAuthoringCompilationModel
                .ComputeBodyMotionContentRevision(profile, guid)
                .ToString();
            bodyMotion.semanticVersion = CharacterBodyMotionProfile.SemanticVersion;
            bodyMotion.requiredWorldCapability = WorldCapability.AirborneVerticalMotion.ToString();
            if (mode == AgentSnapshotExportMode.Full)
            {
                bodyMotion.gravityAcceleration = profile.GravityAcceleration.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                bodyMotion.maximumFallSpeed = profile.MaximumFallSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        static void ExportInputs(CharacterInputProfile inputProfile, AgentGraphSnapshot snapshot)
        {
            if (!inputProfile)
                return;

            IReadOnlyList<CharacterInputValueDefinition> values = inputProfile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition value = values[i];
                if (value == null)
                    continue;

                snapshot.inputValues.Add(new AgentInputValue
                {
                    inputValueId = value.InputValueId,
                    valueType = value.ValueType.ToString()
                });
            }

            IReadOnlyList<CharacterActionRequestDefinition> requests = inputProfile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition request = requests[i];
                if (request == null)
                    continue;

                snapshot.actionRequests.Add(new AgentActionRequest
                {
                    requestId = request.RequestId,
                    bufferSeconds = request.BufferSeconds,
                    priority = request.Priority,
                    timingClass = request.TimingClass.ToString()
                });
            }
        }
        static void ExportActionProfiles(IReadOnlyList<ActionProfile> profiles, AgentGraphSnapshot snapshot)
        {
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                if (!profile)
                    continue;

                string path = AssetDatabase.GetAssetPath(profile);
                snapshot.actionProfiles.Add(new AgentActionProfile
                {
                    actionId = profile.ActionId,
                    displayName = profile.DisplayName,
                    assetPath = path,
                    assetGuid = AssetDatabase.AssetPathToGUID(path),
                    targetRequirement = profile.TargetRequirement.ToString(),
                    grantedTags = profile.Tags.Select(value => value.Value).ToList(),
                    blockQuery = ExportTagQuery(profile.BlockTags),
                    cancelQuery = ExportTagQuery(profile.CancelTags)
                });
            }
        }
        void ExportPresentation(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot snapshot)
        {
            CharacterAnimationPresentationProfile presentation = definition.AnimationPresentationProfile;
            string profilePath = presentation ? AssetDatabase.GetAssetPath(presentation) : string.Empty;
            snapshot.presentation.profileAssetPath = profilePath;
            snapshot.presentation.profileAssetGuid = string.IsNullOrEmpty(profilePath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(profilePath);
            if (!presentation)
                return;

            CharacterPresentationPoseGraphAsset poseGraph = presentation.PoseGraph;
            string poseGraphPath = poseGraph ? AssetDatabase.GetAssetPath(poseGraph) : string.Empty;
            snapshot.presentation.poseGraphAssetPath = poseGraphPath;
            snapshot.presentation.poseGraphAssetGuid = string.IsNullOrEmpty(poseGraphPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(poseGraphPath);
            snapshot.presentation.poseGraphId = poseGraph && poseGraph.Graph != null ? poseGraph.Graph.GraphId.Value : string.Empty;
            snapshot.presentation.poseGraphRevision = poseGraph?.Graph?.ContentRevision ?? string.Empty;

            CharacterAnimationRigDefinition rig = presentation.RigDefinition;
            string rigPath = rig ? AssetDatabase.GetAssetPath(rig) : string.Empty;
            snapshot.presentation.rigAssetPath = rigPath;
            snapshot.presentation.rigAssetGuid = string.IsNullOrEmpty(rigPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(rigPath);
            snapshot.presentation.rigId = rig?.RigId ?? string.Empty;
            snapshot.presentation.rigRevision = rig?.Revision ?? string.Empty;

            snapshot.presentation.footAnalysisMode = presentation.FootPlacementAnalysisMode.ToString();
            snapshot.presentation.footAnalysisSourceAssetGuid = presentation.FootPlacementAnalysisSourceAssetGuid;
            if (CharacterFootPlacementAnalysisSource.IsAssetGuid(presentation.FootPlacementAnalysisSourceAssetGuid))
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(presentation.FootPlacementAnalysisSourceAssetGuid);
                CharacterFootPlacementAnalysisSource source =
                    AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(sourcePath);
                if (source)
                {
                    snapshot.presentation.footAnalysisSourceId = source.AnalysisSourceId.Value;
                    snapshot.presentation.footAnalysisSourceVersion = source.AnalysisVersion;
                    snapshot.presentation.footAnalysisAlgorithmVersion = CharacterFootPlacementAnalysisSource.AlgorithmVersion;
                }
            }

            ExportPresentationPoseGraphContext(
                presentation,
                poseGraph,
                poseGraph?.Graph,
                string.Empty,
                new HashSet<PoseGraphId>(),
                snapshot.presentation);

            ExportBlendSpaces(definition, presentation, snapshot.presentation);
        }

        static void ExportBlendSpaces(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AgentSnapshotAnimationPresentation destination)
        {
            var assets = new HashSet<CharacterAnimationBlendSpaceAsset>();
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding =
                    profile.PoseSourceBindings[i];
                if (binding is CharacterBlendSpacePoseSourceBinding blendSpace &&
                    blendSpace.BlendSpace)
                {
                    assets.Add(blendSpace.BlendSpace);
                }
            }
            string compileStatus = ResolvePresentationCompileStatus(definition, out string projectionRevision);
            foreach (CharacterAnimationBlendSpaceAsset asset in assets.OrderBy(value => value.BlendSpaceId.Value, StringComparer.Ordinal))
            {
                string path = AssetDatabase.GetAssetPath(asset);
                var entry = new AgentSnapshotAnimationBlendSpace
                {
                    assetPath = path,
                    assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path),
                    blendSpaceId = asset.BlendSpaceId.Value,
                    contentRevision = asset.ContentRevision,
                    mode = asset.Mode.ToString(),
                    xParameterId = asset.XAxis?.ParameterId.Value ?? string.Empty,
                    xUnit = asset.XAxis?.Unit ?? string.Empty,
                    xMinimum = asset.XAxis?.Minimum ?? 0f,
                    xMaximum = asset.XAxis?.Maximum ?? 0f,
                    yParameterId = asset.YAxis?.ParameterId.Value ?? string.Empty,
                    yUnit = asset.YAxis?.Unit ?? string.Empty,
                    yMinimum = asset.YAxis?.Minimum ?? 0f,
                    yMaximum = asset.YAxis?.Maximum ?? 0f,
                    sampleCount = asset.Samples.Count,
                    compileStatus = compileStatus,
                    projectionRevision = projectionRevision
                };
                CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(asset);
                for (int issue = 0; issue < report.Issues.Count; issue++)
                    entry.diagnostics.Add(report.Issues[issue].ToString());
                destination.blendSpaces.Add(entry);
            }
        }
        static string ResolvePresentationCompileStatus(
            CharacterPipelineDefinition definition,
            out string projectionRevision)
        {
            projectionRevision = definition && definition.PresentationProjection
                ? definition.PresentationProjection.ProjectionRevision
                : string.Empty;
            if (!definition || !definition.SimulationProgram || !definition.PresentationProjection)
                return "Missing";
            try
            {
                if (CharacterSimulationProgramBuildService.EvaluateExactArtifactStaleness(definition))
                    return "Stale";
                CharacterSimulationProgram program = definition.SimulationProgram.Load();
                CharacterPresentationSemanticContract contract = Float32CharacterPresentationContractAdapter.Create(program);
                _ = definition.PresentationProjection.Load(contract);
                return "Ready";
            }
            catch (Exception exception)
            {
                return "Corrupt: " + exception.Message;
            }
        }
        static void ExportPresentationPoseGraphContext(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseCanvasGraph graph,
            string scope,
            HashSet<PoseGraphId> path,
            AgentSnapshotAnimationPresentation destination)
        {
            if (!owner || graph == null || !path.Add(graph.GraphId))
                return;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseCanvasNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                CharacterPoseAuthoringNodeMetadata metadata =
                    CharacterPoseAuthoringMetadata.Require(node.Kind);
                CharacterPresentationPoseSourceSlot sourceSlot =
                    metadata.UsesPoseSourceSlot
                        ? metadata.Source(node.Payload)
                        : null;
                if (metadata.UsesPoseSourceSlot)
                {
                    ResolveObjectIdentity(
                        sourceSlot,
                        out string sourceSlotAssetPath,
                        out string sourceSlotAssetGuid,
                        out long sourceSlotLocalFileId);
                    CharacterPosePortDefinition[] parameters =
                        CharacterPoseAuthoringPortProjection.Get(node)
                        .Where(port => port != null && port.Direction == CharacterPosePortDirection.Input && port.Kind == CharacterPosePortKind.Parameter)
                        .ToArray();
                    destination.stateLocalPoseSources.Add(new AgentSnapshotStateLocalPoseSource
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        nodeKind = node.Kind.ToString(),
                        ownerKind = "StateLocalPoseSource",
                        sourceSlotName = sourceSlot ? sourceSlot.name : string.Empty,
                        sourceSlotAssetPath = sourceSlotAssetPath,
                        sourceSlotAssetGuid = sourceSlotAssetGuid,
                        sourceSlotLocalFileId = sourceSlotLocalFileId,
                        sourceKind = ResolveStateLocalPoseSourceKind(profile, sourceSlot),
                        xParameterPortId = parameters.Length > 0 ? parameters[0].PortId.Value : string.Empty,
                        yParameterPortId = parameters.Length > 1 ? parameters[1].PortId.Value : string.Empty,
                        inputRangePolicy =
                            metadata.OperationCode ==
                            CharacterPoseOperationCode.BlendSpacePlayer
                                ? metadata.InputRange(node.Payload).ToString()
                                : string.Empty
                    });
                }
                if (metadata.OperationFamily ==
                    CharacterPoseOperationFamily.ActionInput)
                {
                    destination.actionPlaybackInputs.Add(new AgentSnapshotActionPlaybackInput
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        ownerKind = "ActionAnimationChannel",
                        animationChannelId = metadata.Channel(node.Payload).IsValid
                            ? metadata.Channel(node.Payload).Value
                            : string.Empty
                    });
                }
                if (metadata.OperationFamily ==
                    CharacterPoseOperationFamily.AnimationSlot)
                {
                    GraphAuthoringFieldDescriptor slotField = metadata.Fields
                        .Single(value => value.PickerKind == "animation-slot");
                    string animationSlotValue = metadata.ReadField(
                        node.Payload,
                        slotField.FieldId.Value) as string;
                    AnimationSlotId animationSlotId =
                        string.IsNullOrWhiteSpace(animationSlotValue)
                            ? default
                            : new AnimationSlotId(animationSlotValue);
                    destination.animationSlots.Add(new AgentSnapshotAnimationSlot
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        ownerKind = "ActionAnimationChannel",
                        animationSlotId = animationSlotId.IsValid
                            ? animationSlotId.Value
                            : string.Empty,
                        animationSlotGroupId = animationSlotId.IsValid
                            ? profile.RigDefinition.RequireAnimationSlot(
                                animationSlotId).GroupId.Value
                            : string.Empty,
                        animationChannelId = metadata.Channel(node.Payload).IsValid
                            ? metadata.Channel(node.Payload).Value
                            : string.Empty
                    });
                }
                IReadOnlyList<CharacterPoseGraphDependency> dependencies =
                    metadata.ProjectGraphDependencies(node.Payload);
                for (int dependencyIndex = 0;
                     dependencyIndex < dependencies.Count;
                     dependencyIndex++)
                {
                    CharacterPoseGraphDependency dependency =
                        dependencies[dependencyIndex];
                    if (!dependency.GraphId.IsValid)
                        continue;
                    CharacterPoseCanvasGraph child =
                        owner.RequireGraph(dependency.GraphId);
                    ExportPresentationPoseGraphContext(
                        profile,
                        owner,
                        child,
                        dependency.Kind ==
                        CharacterPoseGraphDependencyKind.StatePose
                            ? scopedNodeId.Value + "/state/" +
                              dependency.OwnerIdentity
                            : scopedNodeId.Value + "/" + child.GraphId,
                        path,
                        destination);
                }
            }
            path.Remove(graph.GraphId);
        }

        static string ResolveStateLocalPoseSourceKind(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceSlot sourceSlot)
        {
            if (!sourceSlot || profile == null)
                return string.Empty;
            CharacterPresentationPoseSourceBinding source = sourceSlot
                ? profile.FindPoseSourceBinding(sourceSlot)
                : null;
            return source?.SourceKind.ToString() ?? string.Empty;
        }
        static void ResolveObjectIdentity(
            UnityEngine.Object asset,
            out string assetPath,
            out string assetGuid,
            out long localFileId)
        {
            assetPath = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            if (!asset ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset,
                    out assetGuid,
                    out localFileId))
            {
                assetGuid = string.Empty;
                localFileId = 0;
            }
        }
        static AgentGameplayTagQuery ExportTagQuery(ThirdPersonGameplay.Tags.GameplayTagQuery query)
        {
            var result = new AgentGameplayTagQuery();
            if (query == null)
                return result;
            result.all.AddRange(query.All.Select(value => value.Value));
            result.any.AddRange(query.Any.Select(value => value.Value));
            result.none.AddRange(query.None.Select(value => value.Value));
            return result;
        }

    }
}
