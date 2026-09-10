using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAuthoringDocumentExporter
    {
        public AgentAuthoringPackageProjection Export(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));

            bool generatedProductStale =
                CharacterSimulationProgramBuildService
                    .EvaluateExactArtifactStaleness(definition);
            AgentGraphSnapshot snapshot = new AgentGraphSnapshotExporter().ExportFull(definition);
            snapshot.controlModuleId = definition.ControlModuleId;
            List<AgentSnapshotSkillDefinition> skills = AgentSkillDocumentExporter.Export(definition.SkillDefinitions);
            snapshot.skills = skills;
            var skillReport = new AgentCompileReport
            {
                success = true,
                domain = AgentAuthoringSchema.CharacterControllerDomain,
                rootIdentity = snapshot.rootIdentity
            };
            AgentPackageSkillFlowDocument skillDocument =
                AgentSkillFlowDocumentExporter.Export(definition, skillReport);
            if (!AgentSkillFlowDocumentMapper.Validate(
                    skillDocument,
                    skillReport,
                    definition.ControlModuleId,
                    CharacterSkillProviderOwners.Asset(AssetDatabase.AssetPathToGUID(
                        AssetDatabase.GetAssetPath(definition.InputProfile))),
                    CharacterSkillProviderOwners.Asset(AssetDatabase.AssetPathToGUID(
                        AssetDatabase.GetAssetPath(definition.GameplayEffectProfile)))) ||
                skillReport.HasErrors())
                throw new InvalidOperationException(string.Join(Environment.NewLine, skillReport.messages.Select(value => value.message)));
            AddSkillInputBindings(snapshot, skillDocument);
            snapshot.skillGraphs = skillDocument.graphs;
            snapshot.skillGraphLayouts = skillDocument.layouts;
            snapshot.skillMacros = skillDocument.macros;
            snapshot.skillTimelines = skillDocument.timelines;
            var editableGraphs = new List<AgentSnapshotGraph>();
            var editableStateMachines = new List<AgentSnapshotStateMachineSummary>();
            HashSet<string> skillTimelineIds =
                skillDocument.timelines
                    .Where(value => value != null && !string.IsNullOrWhiteSpace(value.id))
                    .Select(value => value.id)
                    .ToHashSet(StringComparer.Ordinal);
            List<AgentSnapshotTimeline> editableTimelines =
                AgentAuthoringDocumentCodec.Clone(
                    snapshot.timelines
                        .Where(value => value != null && !skillTimelineIds.Contains(value.timelineAuthoringId))
                        .ToList()) ??
                new List<AgentSnapshotTimeline>();
            List<AgentSnapshotTimelineTreeClip> editableTimelineTreeClips =
                AgentAuthoringDocumentCodec.Clone(snapshot.timelineTreeClips) ??
                new List<AgentSnapshotTimelineTreeClip>();
            editableTimelineTreeClips = editableTimelineTreeClips
                .Where(value => value != null && !skillTimelineIds.Contains(value.timelineAuthoringId))
                .ToList();
            var editable = new AgentDocumentEditable
            {
                control = new AgentDocumentControlConfiguration
                {
                    moduleId = snapshot.controlModuleId,
                    semanticVersion = snapshot.controlSemanticVersion,
                    parameters = snapshot.controlParameters
                },
                blackboardSchemaRevision = TreeDesigner.PipelineBlackboardAuthoringSchema.CurrentRevision,
                graphs = editableGraphs,
                stateMachines = editableStateMachines,
                blackboardDeclarations = snapshot.blackboardDeclarations,
                timelines = editableTimelines,
                timelineTreeClips = editableTimelineTreeClips,
                actionRequests = snapshot.actionRequests,
                actionProfiles = snapshot.actionProfiles,
                skills = skills,
                skillGraphs = skillDocument.graphs,
                skillGraphLayouts = skillDocument.layouts,
                skillMacros = skillDocument.macros,
                skillTimelines = skillDocument.timelines,
                presentation = new AgentAuthoringPresentationExporter().Export(definition)
            };
            var context = new AgentDocumentContext
            {
                definitionName = snapshot.definitionName,
                definitionAssetPath = snapshot.definitionAssetPath,
                inputValues = snapshot.inputValues,
                bodyMotion = snapshot.bodyMotion,
                presentation = ExportPresentationContext(
                    definition,
                    snapshot.presentation),
                timelineAssets = snapshot.timelineAssets,
                actionContextAssets = snapshot.actionContextAssets,
                generatedProduct = ExportGeneratedProduct(
                    snapshot,
                    generatedProductStale),
                capabilities = CharacterCapabilities()
            };
            AgentGraphSnapshot projectionSnapshot = AgentAuthoringDocumentCodec.Clone(snapshot);
            projectionSnapshot.graphs = editableGraphs;
            projectionSnapshot.stateMachines = editableStateMachines;
            projectionSnapshot.timelines = editableTimelines;
            projectionSnapshot.timelineTreeClips = editableTimelineTreeClips;
            projectionSnapshot.skills = skills;
            projectionSnapshot.skillGraphs = skillDocument.graphs;
            projectionSnapshot.skillGraphLayouts = skillDocument.layouts;
            projectionSnapshot.skillMacros = skillDocument.macros;
            projectionSnapshot.skillTimelines = skillDocument.timelines;
            return Finish(projectionSnapshot, editable, context);
        }

        static AgentAuthoringPackageProjection Finish(
            AgentGraphSnapshot snapshot,
            AgentDocumentEditable editable,
            AgentDocumentContext context)
        {
            var target = new AgentAuthoringTarget
            {
                domain = snapshot.domain,
                rootIdentity = snapshot.rootIdentity,
                editable = editable,
                context = context
            };
            var report = new AgentCompileReport
            {
                schemaVersion = AgentAuthoringSchema.Version,
                domain = snapshot.domain,
                rootIdentity = snapshot.rootIdentity
            };
            Dictionary<string, Newtonsoft.Json.Linq.JToken> files = new AgentAuthoringPackageMapper().ToFiles(target, snapshot, report);
            if (report.HasErrors())
                throw new InvalidOperationException(string.Join(Environment.NewLine, report.messages.Select(message => message.message)));
            string editableHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair => pair.Key.StartsWith("editable/", StringComparison.Ordinal)));
            string contextHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair =>
                pair.Key.StartsWith("context/", StringComparison.Ordinal) ||
                pair.Key.StartsWith("readonly/", StringComparison.Ordinal)));
            string sourceRevision = ComputeSourceRevision(editable);
            snapshot.schemaVersion = AgentAuthoringSchema.Version;
            snapshot.sourceRevision = sourceRevision;
            return new AgentAuthoringPackageProjection(snapshot, target, sourceRevision, editableHash, contextHash);
        }

        static void AddSkillInputBindings(
            AgentGraphSnapshot snapshot,
            AgentPackageSkillFlowDocument document)
        {
            var inputIds = new HashSet<string>(
                snapshot.inputValues.Select(value => value.inputValueId),
                StringComparer.Ordinal);
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs)
                foreach (AgentPackageSkillBlackboardDeclaration declaration in graph.blackboardDeclarations)
                {
                    string inputValueId = declaration.inputBinding?.inputValueId;
                    if (string.IsNullOrWhiteSpace(inputValueId) || !inputIds.Add(inputValueId))
                        continue;
                    snapshot.inputValues.Add(new AgentSnapshotInputValue
                    {
                        inputValueId = inputValueId,
                        valueType = ProgramInputValueKind.ActionTargetSnapshot.ToString()
                    });
                }
        }

        static string ComputeSourceRevision(AgentDocumentEditable editable)
        {
            AgentPackageSkillFlowDocument skillDocument = AgentSkillFlowDocumentClone.Clone(new AgentPackageSkillFlowDocument
            {
                skills = editable?.skills,
                graphs = editable?.skillGraphs,
                layouts = editable?.skillGraphLayouts,
                macros = editable?.skillMacros,
                timelines = editable?.skillTimelines
            });
            AgentDocumentEditable semantic = AgentAuthoringDocumentCodec.Clone(editable);
            foreach (AgentSnapshotGraph graph in semantic.graphs ?? new List<AgentSnapshotGraph>())
            {
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                    node.position = null;
            }
            if (semantic.presentation != null)
            {
                semantic.presentation.poseGraphLayouts =
                    new List<AgentPackagePoseGraphLayoutFile>();
                semantic.presentation.poseStateMachineLayouts =
                    new List<AgentPackagePoseStateMachineLayoutFile>();
                foreach (AgentPackageLinkedPoseImplementationFile implementation in
                         semantic.presentation.linkedPoseImplementations ??
                         new List<AgentPackageLinkedPoseImplementationFile>())
                {
                    implementation.poseGraphLayouts =
                        new List<AgentPackagePoseGraphLayoutFile>();
                    implementation.poseStateMachineLayouts =
                        new List<AgentPackagePoseStateMachineLayoutFile>();
                }
            }
            semantic.skillGraphLayouts = new List<AgentPackageSkillFlowGraphLayoutFile>();
            foreach (AgentPackageSkillFlowGraphFile graph in semantic.skillGraphs ?? new List<AgentPackageSkillFlowGraphFile>())
                if (graph != null)
                    graph.contentRevision = string.Empty;
            skillDocument.layouts = new List<AgentPackageSkillFlowGraphLayoutFile>();
            foreach (AgentPackageSkillFlowGraphFile graph in skillDocument.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
                if (graph != null)
                    graph.contentRevision = string.Empty;
            return AgentAuthoringDocumentCodec.Hash(new
            {
                document = semantic,
                skillTimelineCurves = skillDocument.timelines
                    .SelectMany(timeline => timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                    .SelectMany(track => track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                    .SelectMany(clip => clip?.curves ?? new List<AgentPackageCurve>())
                    .ToList()
            });
        }

        static AgentDocumentGeneratedProduct ExportGeneratedProduct(
            AgentGraphSnapshot snapshot,
            bool? stale = null)
        {
            return new AgentDocumentGeneratedProduct
            {
                programId = snapshot.programId,
                sourceRevision = snapshot.sourceRevision,
                semanticHash = snapshot.semanticHash,
                numericProfileId = snapshot.numericProfileId,
                targetAbiVersion = snapshot.targetAbiVersion,
                programHash = snapshot.programHash,
                layoutHash = snapshot.layoutHash,
                stale = stale ??
                        string.IsNullOrEmpty(snapshot.programHash)
            };
        }

        static AgentDocumentPresentationContext ExportPresentationContext(
            CharacterPipelineDefinition definition,
            AgentSnapshotAnimationPresentation snapshot)
        {
            CharacterAnimationRigDefinition rig =
                definition.AnimationPresentationProfile?.RigDefinition;
            if (!rig)
                return new AgentDocumentPresentationContext();
            string rigPath = AssetDatabase.GetAssetPath(rig);
            return new AgentDocumentPresentationContext
            {
                rig = new AgentPackageObjectReference
                {
                    assetPath = rigPath,
                    assetGuid = AssetDatabase.AssetPathToGUID(rigPath),
                    localFileId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        rig,
                        out _,
                        out long localFileId)
                            ? localFileId
                            : 0
                },
                rigId = rig.RigId,
                rigRevision = rig.Revision,
                rootBonePolicy = rig.RootBonePolicy.ToString(),
                scalePolicy = rig.ScalePolicy.ToString(),
                pelvisBoneId = rig.PelvisBoneId.Value,
                linkedPoseInterfaces = definition.AnimationPresentationProfile
                    .LinkedPoseGroups
                    .Select(value => value.Interface)
                    .Concat(definition.AnimationPresentationProfile
                        .LinkedPoseImplementations
                        .Where(value => value)
                        .Select(value => value.Interface))
                    .Where(value => value)
                    .Distinct()
                    .OrderBy(value => value.InterfaceId)
                    .Select(ExportLinkedPoseInterface)
                    .ToList(),
                leftLeg = ExportLegChain(rig.LeftLeg),
                rightLeg = ExportLegChain(rig.RightLeg),
                poseCapabilities = ExportPoseCapabilities(),
                physicalBones = rig.PhysicalBones.Select(value =>
                    new AgentDocumentRigBoneContext
                    {
                        id = value.BoneId.Value,
                        parentIndex = value.ParentIndex
                    }).ToList(),
                virtualBones = rig.VirtualBones.Select(value =>
                    new AgentDocumentVirtualBoneContext
                    {
                        id = value.VirtualBoneId.Value,
                        name = value.DisplayName,
                        sourcePhysicalBoneId =
                            value.SourcePhysicalBoneId.Value,
                        targetPhysicalBoneId =
                            value.TargetPhysicalBoneId.Value
                    }).ToList(),
                stateLocalPoseSources =
                    snapshot.stateLocalPoseSources,
                actionPlaybackInputs =
                    snapshot.actionPlaybackInputs,
                animationSlots = snapshot.animationSlots,
                producers = snapshot.producers,
                blendSpaces = snapshot.blendSpaces,
                blendCurves = ExportBlendCurves(),
                blendProfiles = ExportBlendProfiles(
                    rig),
                animationClips = ExportAnimationClips(
                    definition),
                footAnalysisSourceId =
                    snapshot.footAnalysisSourceId,
                footAnalysisSourceVersion =
                    snapshot.footAnalysisSourceVersion,
                footAnalysisAlgorithmVersion =
                    snapshot.footAnalysisAlgorithmVersion
            };
        }

        static AgentDocumentLegChainContext ExportLegChain(CharacterAnimationLegChainDefinition leg) =>
            new AgentDocumentLegChainContext
            {
                hipBoneId = leg.HipBoneId.Value,
                kneeBoneId = leg.KneeBoneId.Value,
                ankleBoneId = leg.AnkleBoneId.Value,
                toeBoneId = leg.ToeBoneId.Value
            };

        static List<AgentDocumentPoseCapabilityContext> ExportPoseCapabilities()
        {
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
            return CharacterPoseGraphAuthoringCapabilities.Catalog.Descriptors
                .Where(value => value.DomainId.Equals(
                    CharacterPoseGraphAuthoringCapabilities.Domain))
                .Where(value => value.AuthoringType != null)
                .OrderBy(value => value.CapabilityId.Value, StringComparer.Ordinal)
                .Select(value => new AgentDocumentPoseCapabilityContext
                {
                    id = value.CapabilityId.Value,
                    nodeKind = value.ExternalKind,
                    executionDomain = value.ExecutionDomainId,
                    workerThreadSafe = CharacterPoseNodeDefinitionModule.Shared
                        .RequireCapability(value.CapabilityId.Value)
                        .WorkerThreadSafe,
                    workerKernel = CharacterPoseNodeDefinitionModule.Shared
                        .RequireCapability(value.CapabilityId.Value)
                        .WorkerKernel.ToString(),
                    ports = value.FixedPorts
                        .OrderBy(port => port.Order)
                        .Select(port =>
                            new AgentDocumentPoseCapabilityPortContext
                            {
                                id = port.PortId.Value,
                                valueType = port.ValueTypeId,
                                direction = port.Direction.ToString(),
                                required = port.Required
                            })
                        .ToList()
                })
                .ToList();
        }

        static List<AgentDocumentAnimationClipContext> ExportAnimationClips(
            CharacterPipelineDefinition definition)
        {
            AgentDocumentPresentationEditable presentation =
                new AgentAuthoringPresentationExporter().Export(definition);
            return presentation.animationClips
                .Select(value => new AgentDocumentAnimationClipContext
                {
                    id = value.id,
                    name = System.IO.Path.GetFileNameWithoutExtension(value.clip.assetPath),
                    clip = value.clip,
                    writable = value.clip.assetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase),
                    dependencyBaseline = value.dependencyBaseline,
                    analysisInputHash = value.analysisInputHash,
                    registeredCurveHash = value.registeredCurveHash
                })
                .ToList();
        }

        static List<AgentDocumentBlendAssetContext> ExportBlendCurves() =>
            AssetDatabase.FindAssets("t:CharacterAnimationBlendCurveAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendCurveAsset>)
                .Where(value => value)
                .GroupBy(value => value.CurveId, StringComparer.Ordinal)
                .Select(group => group.Distinct().Single())
                .OrderBy(value => value.CurveId, StringComparer.Ordinal)
                .Select(value => BlendAsset(
                    value.CurveId,
                    "AnimationBlendCurve",
                    value.Revision,
                    string.Empty,
                    string.Empty,
                    value))
                .ToList();

        static List<AgentDocumentBlendAssetContext> ExportBlendProfiles(
            CharacterAnimationRigDefinition rig) =>
            AssetDatabase.FindAssets("t:CharacterAnimationBlendProfile")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendProfile>)
                .Where(value => value &&
                                string.Equals(value.RigId, rig.RigId, StringComparison.Ordinal) &&
                                string.Equals(value.RigRevision, rig.Revision, StringComparison.Ordinal))
                .GroupBy(value => value.ProfileId, StringComparer.Ordinal)
                .Select(group => group.Distinct().Single())
                .OrderBy(value => value.ProfileId, StringComparer.Ordinal)
                .Select(value => BlendAsset(
                    value.ProfileId,
                    "AnimationBlendProfile",
                    StableHash.Compute(
                        AnimationBlendCanonicalPayload.ProfileKey(
                            new AnimationBlendProfilePayload(value, rig))).ToString(),
                    value.RigId,
                    value.RigRevision,
                    value))
                .ToList();

        static AgentDocumentBlendAssetContext BlendAsset(
            string id,
            string kind,
            string revision,
            string rigId,
            string rigRevision,
            UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return new AgentDocumentBlendAssetContext
            {
                id = id,
                kind = kind,
                revision = revision,
                rigId = rigId,
                rigRevision = rigRevision,
                assetPath = path,
                assetGuid = AssetDatabase.AssetPathToGUID(path)
            };
        }

        static List<string> CharacterCapabilities()
        {
            var capabilities = new HashSet<string>(StringComparer.Ordinal)
            {
                "Graph",
                "StateMachine",
                "ConditionRule",
                "Blackboard",
                "Action",
                "Timeline",
                "MotionWarp",
                "AnimationChannel",
                "AnimationClip",
                "RegisteredCurveChannel",
                "PresentationProfile",
                "PoseGraph",
                "PoseStateMachine",
                "PoseTransitionRule",
                "LinkedPoseInterfaceRuntime"
            };
            foreach (AgentPackageSkillNodeKindDescriptor descriptor in AgentSkillFlowAuthoringCapabilities.ExportCatalog())
                capabilities.Add(descriptor.kind);
            return capabilities.OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        static AgentPackageLinkedPoseInterfaceFile ExportLinkedPoseInterface(
            CharacterLinkedPoseInterfaceAsset value)
        {
            value.RequireValid();
            AgentPackageObjectReference asset =
                AgentAuthoringPresentationExporter.ExportAsset(value, true);
            return new AgentPackageLinkedPoseInterfaceFile
            {
                id = value.InterfaceId.Value,
                asset = asset,
                ownerIdentity = value.OwnerIdentity,
                interfaceId = value.InterfaceId.Value,
                revision = value.Revision.Value,
                signatureHash = value.SignatureHash.ToString(),
                factContractIdentity = value.FactContractIdentity.ToString(),
                executionContract = value.ExecutionContract,
                entries = value.Entries.Select(entry =>
                    new AgentPackageLinkedPoseInterfaceEntry
                    {
                        entryId = entry.EntryId.Value,
                        executionDomain = entry.ExecutionDomain.ToString(),
                        ports = entry.Ports.Select(port =>
                            new AgentPackageLinkedPoseInterfacePort
                            {
                                portId = port.PortId.Value,
                                direction = port.Direction.ToString(),
                                kind = port.Kind.ToString(),
                                space = port.Space.ToString(),
                                required = port.Required,
                                order = port.Order
                            }).OrderBy(port => port.order).ToList()
                    }).OrderBy(entry => entry.entryId, StringComparer.Ordinal)
                    .ToList()
            };
        }

    }
}
