using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
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
            var editable = new AgentDocumentEditable
            {
                blackboardSchemaRevision = TreeDesigner.PipelineBlackboardAuthoringSchema.CurrentRevision,
                graphs = snapshot.graphs,
                stateMachines = snapshot.stateMachines,
                blackboardDeclarations = snapshot.blackboardDeclarations,
                timelines = snapshot.timelines,
                timelineTreeClips = snapshot.timelineTreeClips,
                actionRequests = snapshot.actionRequests,
                actionProfiles = snapshot.actionProfiles,
                presentation = new AgentAuthoringPresentationExporter().Export(definition)
            };
            var context = new AgentDocumentContext
            {
                definitionName = snapshot.definitionName,
                definitionAssetPath = snapshot.definitionAssetPath,
                rootTreeAssetPath = snapshot.rootTreeAssetPath,
                rootGraphAuthoringId = snapshot.rootGraphAuthoringId,
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
            return Finish(snapshot, editable, context);
        }

        public AgentAuthoringPackageProjection Export(AIControllerDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));

            AgentGraphSnapshot characterSnapshot = definition.ControlledCharacter
                ? new AgentGraphSnapshotExporter().ExportFull(definition.ControlledCharacter)
                : new AgentGraphSnapshot();
            AgentGraphSnapshot snapshot = new AgentAIControllerSnapshotExporter().Export(
                definition,
                AgentSnapshotExportMode.Full,
                characterSnapshot);
            AgentSnapshotAIController controller = snapshot.aiController ?? new AgentSnapshotAIController();
            var editable = new AgentDocumentEditable
            {
                blackboardSchemaRevision = TreeDesigner.PipelineBlackboardAuthoringSchema.CurrentRevision,
                graphs = snapshot.graphs,
                aiController = new AgentDocumentAIEditable
                {
                    controllerId = controller.controllerId,
                    definitionAssetPath = controller.definitionAssetPath,
                    definitionAssetGuid = controller.definitionAssetGuid,
                    treeAssetPath = controller.treeAssetPath,
                    treeAssetGuid = controller.treeAssetGuid,
                    graphAuthoringId = controller.graphAuthoringId,
                    authoringRole = controller.authoringRole,
                    perceptionAssetPath = controller.perceptionAssetPath,
                    perceptionAssetGuid = controller.perceptionAssetGuid,
                    candidateOrdering = controller.candidateOrdering,
                    candidateActorIds = controller.candidateActorIds,
                    controlledCharacterAssetPath = controller.controlledCharacterAssetPath,
                    controlledCharacterAssetGuid = controller.controlledCharacterAssetGuid,
                    blackboardDeclarations = controller.blackboardDeclarations,
                    nodes = controller.nodes
                }
            };
            var context = new AgentDocumentContext
            {
                definitionName = snapshot.definitionName,
                definitionAssetPath = snapshot.definitionAssetPath,
                rootTreeAssetPath = snapshot.rootTreeAssetPath,
                rootGraphAuthoringId = snapshot.rootGraphAuthoringId,
                inputValues = characterSnapshot.inputValues,
                actionRequests = characterSnapshot.actionRequests,
                generatedProduct = ExportGeneratedProduct(snapshot),
                aiController = new AgentDocumentAIContext
                {
                    characterProgramId = controller.characterProgramId,
                    characterProgramHash = controller.characterProgramHash,
                    characterProgramStale = controller.characterProgramStale,
                    intentProgramAssetPath = controller.intentProgramAssetPath,
                    intentProgramAssetGuid = controller.intentProgramAssetGuid,
                    intentProgramId = controller.intentProgramId,
                    intentProgramHash = controller.intentProgramHash,
                    intentProgramSourceRevision = controller.intentProgramSourceRevision,
                    intentProgramStale = controller.intentProgramStale
                },
                capabilities = AICapabilities()
            };
            return Finish(snapshot, editable, context);
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

        static string ComputeSourceRevision(AgentDocumentEditable editable)
        {
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
            return AgentAuthoringDocumentCodec.Hash(semantic);
        }

        static AgentDocumentGeneratedProduct ExportGeneratedProduct(
            AgentGraphSnapshot snapshot,
            bool? stale = null)
        {
            return new AgentDocumentGeneratedProduct
            {
                programId = snapshot.programId,
                sourceRevision = snapshot.aiController?.intentProgramSourceRevision ?? snapshot.sourceRevision,
                semanticHash = snapshot.semanticHash,
                numericProfileId = snapshot.numericProfileId,
                targetAbiVersion = snapshot.targetAbiVersion,
                programHash = snapshot.aiController?.intentProgramHash ?? snapshot.programHash,
                layoutHash = snapshot.layoutHash,
                stale = stale ??
                        snapshot.aiController?.intentProgramStale ??
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
                rig = new AgentPackageAssetReferenceV3
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
                animationSequences = ExportAnimationSequences(
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
                .OrderBy(value => value.CapabilityId.Value, StringComparer.Ordinal)
                .Select(value => new AgentDocumentPoseCapabilityContext
                {
                    id = value.CapabilityId.Value,
                    nodeKind = value.ExternalKind,
                    executionDomain = value.ExecutionDomainId,
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

        static List<AgentDocumentAnimationSequenceContext> ExportAnimationSequences(
            CharacterPipelineDefinition definition)
        {
            AgentDocumentPresentationEditable presentation =
                new AgentAuthoringPresentationExporter().Export(definition);
            return presentation.animationSequences
                .Select(value => new AgentDocumentAnimationSequenceContext
                {
                    id = value.id,
                    name = value.name,
                    asset = value.asset,
                    clip = value.clip,
                    rig = value.rig,
                    footAnalysisSource = value.footAnalysisSource,
                    footAnalysisIdentity = value.footAnalysisIdentity,
                    contentRevision = value.contentRevision
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
            return new List<string>
            {
                "Graph",
                "StateMachine",
                "ConditionRule",
                "Blackboard",
                "Action",
                "Timeline",
                "MotionWarp",
                "AnimationChannel",
                "AnimationSequence",
                "RegisteredCurveChannel",
                "PresentationProfile",
                "PoseGraph",
                "PoseStateMachine",
                "PoseTransitionRule",
                "LinkedPoseInterfaceRuntime"
            };
        }

        static AgentPackageLinkedPoseInterfaceFile ExportLinkedPoseInterface(
            CharacterLinkedPoseInterfaceAsset value)
        {
            value.RequireValid();
            AgentPackageAssetReferenceV3 asset =
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

        static List<string> AICapabilities()
        {
            return new List<string>
            {
                "Graph",
                "Blackboard",
                "Perception",
                "Observation",
                "Memory",
                "Intent",
                "BTConditionRule"
            };
        }
    }
}
