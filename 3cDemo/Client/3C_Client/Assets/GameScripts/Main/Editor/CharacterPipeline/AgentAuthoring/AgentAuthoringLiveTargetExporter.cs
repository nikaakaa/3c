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
    public sealed class AgentAuthoringLiveTargetExporter
    {
        public AgentAuthoringTarget Export(CharacterPipelineDefinition definition)
        {
            return ExportTarget(definition);
        }
        AgentAuthoringTarget ExportTarget(CharacterPipelineDefinition definition)
        {

            var target = new AgentAuthoringTarget
            {
                domain = AgentAuthoringSchema.CharacterControllerDomain,
                editable = new AgentDocumentEditable(),
                context = new AgentDocumentContext
                {
                    presentation = new AgentDocumentPresentationContext()
                }
            };
            if (!definition)
                return target;

            target.context.definitionName = definition.name;
            target.context.definitionAssetPath = AssetDatabase.GetAssetPath(definition);
            target.rootIdentity = AssetDatabase.AssetPathToGUID(target.context.definitionAssetPath);
            target.context.inputProviderOwnerId = CharacterSkillProviderOwners.Asset(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.InputProfile)));
            target.context.gameplayProviderOwnerId = CharacterSkillProviderOwners.Asset(
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.GameplayEffectProfile)));
            target.editable.control.moduleId = definition.ControlModuleId;
            if (!string.IsNullOrEmpty(target.editable.control.moduleId))
            {
                ICharacterControlModule controlModule =
                    CorinCharacterControlModuleCatalog.Create().Require(
                        new CharacterControlModuleId(target.editable.control.moduleId));
                target.editable.control.semanticVersion = controlModule.Contract.SemanticVersion;
                target.editable.control.parameters = definition.ControlParameters
                    .Where(value => value != null)
                    .Select(value => new AgentControlParameter
                    {
                        id = value.ParameterId,
                        valueType = value.ValueKind.ToString(),
                        numericValue = value.NumericValue
                    })
                    .ToList();
            }

            ExportBodyMotion(definition.BodyMotionProfile, target.context.bodyMotion);
            ExportInputs(definition.InputProfile, target.context);
            target.editable.actionRequests = target.context.actionRequests;
            ExportActionProfiles(definition.ActionProfiles, target.editable);

            var program = definition.SimulationProgram;
            if (program)
            {
                target.context.generatedProduct.programId = program.ProgramId;
                target.context.generatedProduct.sourceRevision = program.SourceRevision;
                target.context.generatedProduct.semanticHash = program.SemanticHash;
                target.context.generatedProduct.numericProfileId = program.NumericProfileId;
                target.context.generatedProduct.targetAbiVersion = program.TargetAbiVersion;
                target.context.generatedProduct.programHash = program.ProgramHash;
                target.context.generatedProduct.layoutHash = program.LayoutHash;
            }
            ExportPresentation(definition, target);

            return target;
        }
        static void ExportBodyMotion(
            CharacterBodyMotionProfile profile,
            AgentBodyMotionProfile bodyMotion)
        {
            if (!profile)
                return;
            string path = AssetDatabase.GetAssetPath(profile);
            string guid = AssetDatabase.AssetPathToGUID(path);
            bodyMotion.assetPath = path;
            bodyMotion.assetGuid = guid;
            bodyMotion.sourceIdentity = $"asset:{guid}";
            bodyMotion.contentRevision = CharacterAuthoringCompilationModel
                .ComputeBodyMotionContentRevision(profile, guid)
                .ToString();
            bodyMotion.semanticVersion = CharacterBodyMotionProfile.SemanticVersion;
            bodyMotion.requiredWorldCapability = WorldCapability.AirborneVerticalMotion.ToString();
            bodyMotion.gravityAcceleration = profile.GravityAcceleration.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            bodyMotion.maximumFallSpeed = profile.MaximumFallSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
        static void ExportInputs(CharacterInputProfile inputProfile, AgentDocumentContext context)
        {
            if (!inputProfile)
                return;

            IReadOnlyList<CharacterInputValueDefinition> values = inputProfile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition value = values[i];
                if (value == null)
                    continue;

                context.inputValues.Add(new AgentInputValue
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

                context.actionRequests.Add(new AgentActionRequest
                {
                    requestId = request.RequestId,
                    bufferSeconds = request.BufferSeconds,
                    priority = request.Priority,
                    timingClass = request.TimingClass.ToString()
                });
            }
        }
        static void ExportActionProfiles(IReadOnlyList<ActionProfile> profiles, AgentDocumentEditable editable)
        {
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                if (!profile)
                    continue;

                string path = AssetDatabase.GetAssetPath(profile);
                editable.actionProfiles.Add(new AgentActionProfile
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
            AgentAuthoringTarget target)
        {
            CharacterAnimationPresentationProfile presentation = definition.AnimationPresentationProfile;
            if (!presentation)
                return;

            CharacterPresentationPoseGraphAsset poseGraph = presentation.PoseGraph;
            AgentDocumentPresentationContext context = target.context.presentation;
            context.footAnalysisSourceId = string.Empty;
            context.footAnalysisSourceVersion = 0;
            context.footAnalysisAlgorithmVersion = string.Empty;
            if (CharacterFootPlacementAnalysisSource.IsAssetGuid(presentation.FootPlacementAnalysisSourceAssetGuid))
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(presentation.FootPlacementAnalysisSourceAssetGuid);
                CharacterFootPlacementAnalysisSource source =
                    AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(sourcePath);
                if (source)
                {
                    context.footAnalysisSourceId = source.AnalysisSourceId.Value;
                    context.footAnalysisSourceVersion = source.AnalysisVersion;
                    context.footAnalysisAlgorithmVersion = CharacterFootPlacementAnalysisSource.AlgorithmVersion;
                }
            }

            ExportPresentationPoseGraphContext(
                presentation,
                poseGraph,
                poseGraph?.Graph,
                string.Empty,
                new HashSet<PoseGraphId>(),
                context);

            ExportBlendSpaces(definition, presentation, context);
        }

        static void ExportBlendSpaces(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AgentDocumentPresentationContext destination)
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
                var entry = new AgentDocumentAnimationBlendSpaceContext
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
            AgentDocumentPresentationContext destination)
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
                    destination.stateLocalPoseSources.Add(new AgentDocumentStateLocalPoseSourceContext
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
                    destination.actionPlaybackInputs.Add(new AgentDocumentActionPlaybackInputContext
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
                    destination.animationSlots.Add(new AgentDocumentAnimationSlotContext
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
