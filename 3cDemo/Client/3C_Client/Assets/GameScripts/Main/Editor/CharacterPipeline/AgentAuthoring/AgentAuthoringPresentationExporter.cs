using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAuthoringPresentationExporter
    {
        public AgentDocumentPresentationEditable Export(
            CharacterPipelineDefinition definition)
        {
            if (!definition || !definition.AnimationPresentationProfile)
                throw new InvalidOperationException(
                    "Character Definition requires a formal Animation Presentation Profile.");
            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile;
            CharacterPresentationPoseGraphAsset poseAsset =
                profile.PoseGraph
                    ? profile.PoseGraph
                    : throw new InvalidOperationException(
                        "Presentation Profile requires a Pose Graph asset.");
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();

            var result = new AgentDocumentPresentationEditable
            {
                profile = ExportProfile(profile)
            };
            AppendPoseGraph(result, poseAsset);
            result.linkedPoseImplementations = profile.LinkedPoseImplementations
                .Select(ExportLinkedPoseImplementation)
                .OrderBy(value => value.implementationId, StringComparer.Ordinal)
                .ToList();
            return result;
        }

        public AgentDocumentPresentationEditable ExportPoseGraph(
            CharacterPresentationPoseGraphAsset poseAsset)
        {
            if (!poseAsset)
                throw new ArgumentNullException(nameof(poseAsset));
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
            var result = new AgentDocumentPresentationEditable();
            AppendPoseGraph(result, poseAsset);
            return result;
        }

        static void AppendPoseGraph(
            AgentDocumentPresentationEditable result,
            CharacterPresentationPoseGraphAsset poseAsset)
        {
            AppendPoseGraph(
                result.poseGraphs,
                result.poseGraphLayouts,
                result.poseStateMachines,
                result.poseStateMachineLayouts,
                poseAsset,
                new HashSet<PoseGraphId>());
        }

        static void AppendPoseGraph(
            ICollection<AgentPackagePoseGraphFile> graphs,
            ICollection<AgentPackagePoseGraphLayoutFile> layouts,
            ICollection<AgentPackagePoseStateMachineFile> stateMachines,
            ICollection<AgentPackagePoseStateMachineLayoutFile> stateMachineLayouts,
            CharacterPresentationPoseGraphAsset poseAsset,
            ISet<PoseGraphId> linkedEntryGraphs)
        {
            HashSet<PoseGraphId> stateGraphs = poseAsset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Where(value => value.StateMachine != null)
                .SelectMany(value => value.StateMachine.States)
                .Where(value => value != null && value.PoseGraphId.IsValid)
                .Select(value => value.PoseGraphId)
                .ToHashSet();
            foreach (CharacterTypedPoseGraph graph in poseAsset.EnumerateGraphs())
            {
                if (graph == null)
                    throw new InvalidOperationException(
                        "Pose Graph root-owned catalog contains a missing record.");
                GraphAuthoringDocumentRoleId role =
                    linkedEntryGraphs.Contains(graph.GraphId)
                        ? CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry
                        : ReferenceEquals(graph, poseAsset.Graph)
                        ? CharacterPoseGraphAuthoringCapabilities.RootGraph
                        : stateGraphs.Contains(graph.GraphId)
                            ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                            : CharacterPoseGraphAuthoringCapabilities.Subgraph;
                graphs.Add(ExportGraph(graph, role));
                layouts.Add(ExportLayout(graph));
            }

            foreach (CharacterPoseStateMachineDefinition machine in poseAsset
                         .EnumerateGraphs()
                         .Where(value => value != null)
                         .SelectMany(value => value.Nodes)
                         .Select(value => value?.Payload)
                         .OfType<CharacterPoseStateMachineNodePayload>()
                         .Select(value => value.StateMachine)
                         .Where(value => value != null)
                         .OrderBy(value => value.StateMachineId))
            {
                stateMachines.Add(ExportStateMachine(machine));
                stateMachineLayouts.Add(
                    ExportStateMachineLayout(poseAsset, machine));
            }
        }

        static AgentPackageLinkedPoseImplementationFile ExportLinkedPoseImplementation(
            CharacterLinkedPoseImplementationAsset implementation)
        {
            implementation?.RequireValid();
            if (!implementation)
                throw new InvalidOperationException(
                    "Presentation Profile contains a missing Linked Pose Implementation.");
            CharacterPresentationPoseGraphAsset graphOwner = implementation.Entries
                .Select(value => value?.GraphOwner)
                .Distinct()
                .SingleOrDefault() ?? throw new InvalidOperationException(
                $"Linked Pose Implementation '{implementation.ImplementationId}' must have one graph owner.");
            AgentPackageAssetReferenceV3 asset = Asset(implementation, true);
            var result = new AgentPackageLinkedPoseImplementationFile
            {
                id = ReferenceIdentity(asset),
                name = implementation.name,
                asset = asset,
                ownerIdentity = implementation.OwnerIdentity,
                implementationId = implementation.ImplementationId.Value,
                revision = implementation.Revision.Value,
                interfaceAsset = Asset(implementation.Interface, true),
                graphOwner = Asset(graphOwner, true),
                graphOwnerIdentity = implementation.Entries[0].GraphOwnerIdentity,
                entries = implementation.Entries
                    .Select(value => new AgentPackageLinkedPoseImplementationEntry
                    {
                        entryId = value.EntryId.Value,
                        graphId = value.GraphId.Value
                    })
                    .OrderBy(value => value.entryId, StringComparer.Ordinal)
                    .ToList()
            };
            if (implementation.Entries.Any(value =>
                    value.GraphOwner != graphOwner ||
                    !string.Equals(
                        value.GraphOwnerIdentity,
                        result.graphOwnerIdentity,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"Linked Pose Implementation '{implementation.ImplementationId}' Entry graph owners are inconsistent.");
            }
            var entryGraphs = implementation.Entries
                .Select(value => value.GraphId)
                .ToHashSet();
            AppendPoseGraph(
                result.poseGraphs,
                result.poseGraphLayouts,
                result.poseStateMachines,
                result.poseStateMachineLayouts,
                graphOwner,
                entryGraphs);
            return result;
        }

        static AgentPackagePresentationProfileFile ExportProfile(
            CharacterAnimationPresentationProfile profile)
        {
            AgentPackageAssetReferenceV3 owner = Asset(profile, true);
            return new AgentPackagePresentationProfileFile
            {
                id = owner.assetGuid,
                owner = owner,
                poseGraph = Asset(profile.PoseGraph, true),
                rig = Asset(profile.RigDefinition, true),
                policy = new AgentPackagePresentationPolicy
                {
                    motionMatchingProfile = Asset(profile.MotionMatchingProfile, false),
                    footPlacementAnalysisMode =
                        profile.FootPlacementAnalysisMode.ToString(),
                    footPlacementAnalysisSourceAssetGuid =
                        profile.FootPlacementAnalysisSourceAssetGuid
                },
                poseSources = profile.PoseSourceBindings
                    .Where(binding => binding && binding.Slot)
                    .Select(ExportPoseSource)
                    .OrderBy(value => value.slot.assetGuid, StringComparer.Ordinal)
                    .ThenBy(value => value.slot.localFileId)
                    .ToList(),
                actionProducers = profile.ProducerBindings
                    .Select(ExportProducer)
                    .OrderBy(value => value.timelineId, StringComparer.Ordinal)
                    .ThenBy(value => value.trackId, StringComparer.Ordinal)
                    .ToList(),
                linkedPoseGroups = profile.LinkedPoseGroups
                    .Select(value => new AgentPackageLinkedPoseGroupBinding
                    {
                        id = value.GroupId.Value,
                        groupId = value.GroupId.Value,
                        interfaceAsset = Asset(value.Interface, true)
                    })
                    .OrderBy(value => value.groupId, StringComparer.Ordinal)
                    .ToList(),
                linkedPoseSelectors = profile.LinkedPoseSelectors
                    .Select(ExportLinkedPoseSelector)
                    .OrderBy(value => value.selectorId, StringComparer.Ordinal)
                    .ToList()
            };
        }

        static AgentPackageLinkedPoseSelectorBinding ExportLinkedPoseSelector(
            CharacterLinkedPoseSelectorBindingAsset selector)
        {
            if (selector is not CharacterEquipmentLinkedPoseSelectionBinding equipment)
                throw new InvalidOperationException(
                    $"Linked Pose selector '{selector?.name ?? "missing"}' has no Document v3 codec.");
            AgentPackageAssetReferenceV3 asset = Asset(equipment, true);
            return new AgentPackageLinkedPoseSelectorBinding
            {
                id = ReferenceIdentity(asset),
                kind = "equipment",
                asset = asset,
                selectorId = equipment.SelectorId.Value,
                groupId = equipment.GroupId.Value,
                equipment = new AgentPackageEquipmentLinkedPoseSelectorPayload
                {
                    slotId = equipment.SlotId.Value,
                    emptyImplementationId = equipment.EmptyImplementationId.Value,
                    mappings = equipment.Mappings.Select(value =>
                        new AgentPackageEquipmentLinkedPoseMapping
                        {
                            id = value.EquipmentId.Value,
                            equipmentId = value.EquipmentId.Value,
                            implementationId = value.ImplementationId.Value
                        }).OrderBy(value => value.equipmentId, StringComparer.Ordinal)
                        .ToList()
                }
            };
        }

        static AgentPackagePoseSourceBinding ExportPoseSource(
            CharacterPresentationPoseSourceBinding binding)
        {
            if (!binding || !binding.Slot)
                throw new InvalidOperationException(
                    "Presentation Profile contains a missing Pose source binding.");
            CharacterSequencePoseSourceBinding sequence =
                binding as CharacterSequencePoseSourceBinding;
            CharacterMotionMatchingPoseSourceBinding motionMatching =
                binding as CharacterMotionMatchingPoseSourceBinding;
            return new AgentPackagePoseSourceBinding
            {
                name = binding.Slot.name,
                kind = binding.SourceKind.ToString(),
                slot = Asset(binding.Slot, true),
                binding = Asset(binding, true),
                source = Asset(binding.SourceAsset, true),
                rig = Asset(binding.Rig, true),
                loop = sequence && sequence.Loop,
                defaultPlayRate = sequence ? sequence.DefaultPlayRate : 1f,
                markerGroupId = sequence ? sequence.MarkerGroupId : string.Empty,
                markerTopology = sequence
                    ? sequence.MarkerTopology.ToString()
                    : AnimationMarkerSequenceTopology.Unspecified.ToString(),
                syncRole = sequence
                    ? sequence.SyncRole.ToString()
                    : AnimationMarkerSyncRole.Unspecified.ToString(),
                markers = (sequence?.Markers ??
                           Array.Empty<PresentationPoseSourceMarker>()).Select(value =>
                    new AgentPackagePoseSourceMarker
                    {
                        id = value.AuthoringId,
                        markerId = value.MarkerId,
                        frame = value.Frame
                    }).ToList(),
                footPlacementWeight = sequence
                    ? ExportCurve(sequence.FootPlacementWeightCurve)
                    : null,
                searchDomainId = motionMatching?.SearchDomainId.Value ??
                                 string.Empty,
                databases = motionMatching?.Databases
                    .Select(value => Asset(value, true))
                    .ToList() ?? new List<AgentPackageAssetReferenceV3>(),
                footAnalysisIdentity = binding.FootAnalysisIdentity,
                contentRevision = binding.ContentRevision
            };
        }

        static AgentPackageAnimationProducerBinding ExportProducer(
            AnimationProducerPresentationBinding binding)
        {
            if (binding == null)
                throw new InvalidOperationException(
                    "Presentation Profile contains a missing Action producer binding.");
            return new AgentPackageAnimationProducerBinding
            {
                timelineId = binding.ProducerId.TimelineAuthoringId,
                trackId = binding.ProducerId.TrackAuthoringId,
                source = Asset(binding.Source, true),
                footAnalysisIdentity = binding.FootAnalysisIdentity
            };
        }

        static AgentPackagePoseGraphFile ExportGraph(
            CharacterTypedPoseGraph graph,
            GraphAuthoringDocumentRoleId role)
        {
            return new AgentPackagePoseGraphFile
            {
                id = graph.GraphId.Value,
                role = role.Value,
                contentRevision = graph.ContentRevision,
                parameters = graph.Parameters.Select(value =>
                    new AgentPackagePoseParameter
                    {
                        id = value.ParameterId.Value,
                        valueType = value.ValueType.ToString(),
                        unit = value.Unit,
                        defaultValue = value.DefaultValue
                    }).ToList(),
                nodes = graph.Nodes.Select(value => ExportNode(value, role)).ToList(),
                edges = graph.Edges.Select(value =>
                    new AgentPackagePoseEdge
                    {
                        id = value.EdgeId,
                        from = new AgentPackagePoseEndpoint
                        {
                            node = value.SourceNodeId.Value,
                            port = value.SourcePortId.Value
                        },
                        to = new AgentPackagePoseEndpoint
                        {
                            node = value.TargetNodeId.Value,
                            port = value.TargetPortId.Value
                        }
                    }).ToList()
            };
        }

        static AgentPackagePoseNode ExportNode(
            CharacterTypedPoseNode node,
            GraphAuthoringDocumentRoleId role)
        {
            if (node?.Payload == null)
                throw new InvalidOperationException(
                    "Pose Graph contains a node without typed payload.");
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                    CharacterPoseGraphAuthoringCapabilities.Get(node.Kind),
                    CharacterPoseGraphAuthoringCapabilities.Domain,
                    role);
            var properties = new JObject();
            foreach (GraphAuthoringFieldDescriptor field in capability.Fields
                         .Where(value => value.AuthoringWritable)
                         .OrderBy(value => value.FieldId))
            {
                properties[field.FieldId.Value] =
                    CharacterPoseAuthoringPayloadCodec.EncodeValue(
                        CharacterPoseAuthoringPayloadCodec.Read(
                            node.Payload,
                            field.FieldId.Value),
                        asset => AgentAuthoringDocumentCodec.ToToken(
                            Asset(asset, false)));
            }
            return new AgentPackagePoseNode
            {
                id = node.NodeId.Value,
                capability = capability.CapabilityId.Value,
                name = node.DisplayName,
                properties = properties,
                dynamicPorts = node.DynamicPorts.Select(value =>
                    new AgentPackagePoseDynamicPort
                    {
                        id = value.PortId.Value,
                        name = value.DisplayName,
                        valueType =
                            CharacterTypedPoseGraphDocument.ValueType(value.Kind),
                        direction = value.Direction.ToString(),
                        required = value.Required,
                        order = value.Order,
                        interfacePortId = value.InterfacePortId.Value
                    }).ToList(),
                childDocumentId = node.Payload switch
                {
                    CharacterPoseStateMachineNodePayload stateMachine =>
                        stateMachine.StateMachine?.StateMachineId.Value,
                    CharacterMotionMatchingPosePayload motionMatching =>
                        motionMatching.EntryGraph?.PoseGraphId.Value,
                    _ => string.Empty
                }
            };
        }

        static AgentPackagePoseGraphLayoutFile ExportLayout(
            CharacterTypedPoseGraph graph) =>
            new AgentPackagePoseGraphLayoutFile
            {
                graphId = graph.GraphId.Value,
                nodes = graph.Layout.Select(value =>
                    new AgentPackagePoseNodeLayout
                    {
                        id = value.NodeId.Value,
                        x = value.Position.x,
                        y = value.Position.y
                    }).ToList()
            };

        static AgentPackagePoseStateMachineFile ExportStateMachine(
            CharacterPoseStateMachineDefinition machine)
        {
            return new AgentPackagePoseStateMachineFile
            {
                id = machine.StateMachineId.Value,
                contentRevision = machine.ContentRevision,
                entry = new AgentPackagePoseStateEntry
                {
                    id = machine.Entry.EntryId.Value,
                    targetStateId = machine.Entry.TargetStateId.Value
                },
                maxTransitionsPerFrame = machine.MaxTransitionsPerFrame,
                states = machine.States.Select(value =>
                    new AgentPackagePoseState
                    {
                        id = value.StateId.Value,
                        name = value.DisplayName,
                        poseGraphId = value.PoseGraphId.Value,
                        outputPoseNodeId = value.OutputPoseNodeId.Value,
                        alwaysResetOnEntry = value.AlwaysResetOnEntry
                    }).ToList(),
                aliases = machine.Aliases.Select(value =>
                    new AgentPackagePoseStateAlias
                    {
                        id = value.AliasId.Value,
                        name = value.DisplayName,
                        sources = value.Sources.Select(ExportSource).ToList()
                    }).ToList(),
                transitions = machine.Transitions.Select(value =>
                    new AgentPackagePoseTransition
                    {
                        id = value.TransitionId.Value,
                        source = ExportSource(value.Source),
                        targetStateId = value.TargetStateId.Value,
                        priority = value.Priority,
                        rule = ExportRule(value.Rule),
                        blendLogic = value.BlendLogic.ToString(),
                        durationSeconds = value.DurationSeconds,
                        blendMode = value.BlendMode.ToString(),
                        customBlendCurveAssetId = value.BlendMode == CharacterAnimationBlendMode.Custom
                            ? value.CustomBlendCurve.CurveId
                            : null,
                        blendProfileAssetId = value.BlendProfile
                            ? value.BlendProfile.ProfileId
                            : null
                    }).ToList()
            };
        }

        static AgentPackagePoseStateMachineLayoutFile ExportStateMachineLayout(
            CharacterPresentationPoseGraphAsset poseAsset,
            CharacterPoseStateMachineDefinition machine) =>
            new AgentPackagePoseStateMachineLayoutFile
            {
                stateMachineId = machine.StateMachineId.Value,
                elements = poseAsset
                    .GetExplicitStateMachineLayout(machine.StateMachineId)
                    .OrderBy(value => value.ElementId, StringComparer.Ordinal)
                    .Select(value =>
                        new AgentPackagePoseStateMachineLayoutElement
                        {
                            id = value.ElementId,
                            x = value.Position.x,
                            y = value.Position.y
                        })
                    .ToList()
            };

        static AgentPackagePoseTransitionSource ExportSource(
            CharacterPoseStateTransitionSource source) =>
            new AgentPackagePoseTransitionSource
            {
                kind = source.Kind.ToString(),
                stateId = source.StateId.Value,
                aliasId = source.AliasId.Value
            };

        static AgentPackagePoseTransitionRule ExportRule(
            CharacterPoseTransitionRuleGraph rule) =>
            new AgentPackagePoseTransitionRule
            {
                id = rule.GraphId.Value,
                contentRevision = rule.ContentRevision,
                outputOperationId = rule.OutputOperationId.Value,
                operations = rule.Operations.Select(value =>
                    new AgentPackagePoseTransitionRuleOperation
                    {
                        id = value.OperationId.Value,
                        kind = value.Kind.ToString(),
                        inputA = value.InputA.Value,
                        inputB = value.InputB.Value,
                        factId = value.FactId.Value,
                        boolLiteral = value.BoolLiteral,
                        floatLiteral = value.FloatLiteral,
                        enumTypeId = value.EnumTypeId,
                        enumLiteral = value.EnumLiteral,
                        identityLiteral = value.IdentityLiteral
                    }).ToList()
            };

        static AgentPackageCurve ExportCurve(AnimationCurve curve)
        {
            if (curve == null)
                throw new InvalidOperationException(
                    "Presentation Pose source Foot Placement curve is missing.");
            return new AgentPackageCurve
            {
                timeDomain = "Normalized",
                bounded = true,
                minimum = 0f,
                maximum = 1f,
                zero = 0f,
                unit = "Weight",
                preWrapMode = curve.preWrapMode.ToString(),
                postWrapMode = curve.postWrapMode.ToString(),
                keys = curve.keys.Select(value => new AgentAnimationCurveKey
                {
                    time = value.time,
                    value = value.value,
                    inTangent = value.inTangent,
                    outTangent = value.outTangent,
                    inWeight = value.inWeight,
                    outWeight = value.outWeight,
                    weightedMode = value.weightedMode.ToString()
                }).ToList()
            };
        }

        static AgentPackageAssetReferenceV3 Asset(
            UnityEngine.Object asset,
            bool required)
        {
            if (!asset)
            {
                if (!required)
                    return null;
                throw new InvalidOperationException(
                    "Presentation document references a missing asset.");
            }
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrWhiteSpace(path) ||
                string.IsNullOrWhiteSpace(guid))
            {
                throw new InvalidOperationException(
                    $"Presentation asset '{asset.name}' is not persistent.");
            }
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset,
                    out _,
                    out long localFileId) || localFileId == 0)
            {
                throw new InvalidOperationException(
                    $"Presentation asset '{asset.name}' has no persistent object identity.");
            }
            return new AgentPackageAssetReferenceV3
            {
                assetPath = path,
                assetGuid = guid,
                localFileId = localFileId
            };
        }

        internal static AgentPackageAssetReferenceV3 ExportAsset(
            UnityEngine.Object asset,
            bool required) => Asset(asset, required);

        internal static string ReferenceIdentity(
            AgentPackageAssetReferenceV3 reference) =>
            reference == null
                ? string.Empty
                : string.IsNullOrWhiteSpace(reference.localId)
                    ? reference.assetGuid + ":" + reference.localFileId
                    : reference.localId;
    }
}
