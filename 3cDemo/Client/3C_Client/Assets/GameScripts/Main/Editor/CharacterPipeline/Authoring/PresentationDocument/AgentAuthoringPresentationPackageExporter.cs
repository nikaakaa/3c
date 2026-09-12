using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using UnityAnimationClip = UnityEngine.AnimationClip;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation
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
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();

            var result = new AgentDocumentPresentationEditable
            {
                profile = ExportProfile(profile)
            };
            ExportAnimationClips(definition, profile, result);
            AppendPoseGraph(result, poseAsset, profile);
            result.linkedPoseImplementations = profile.LinkedPoseImplementations
                .Select(value => ExportLinkedPoseImplementation(value, profile))
                .OrderBy(value => value.implementationId, StringComparer.Ordinal)
                .ToList();
            return result;
        }

        public AgentDocumentPresentationEditable ExportPoseGraph(
            CharacterPresentationPoseGraphAsset poseAsset)
        {
            if (!poseAsset)
                throw new ArgumentNullException(nameof(poseAsset));
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
            var result = new AgentDocumentPresentationEditable();
            AppendPoseGraph(result, poseAsset, null);
            return result;
        }

        public AgentDocumentPresentationEditable ExportPoseGraph(
            CharacterPresentationPoseGraphAsset poseAsset,
            CharacterAnimationPresentationProfile profile)
        {
            if (!poseAsset)
                throw new ArgumentNullException(nameof(poseAsset));
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
            var result = new AgentDocumentPresentationEditable();
            AppendPoseGraph(result, poseAsset, profile);
            return result;
        }

        static void AppendPoseGraph(
            AgentDocumentPresentationEditable result,
            CharacterPresentationPoseGraphAsset poseAsset,
            CharacterAnimationPresentationProfile profile)
        {
            AppendPoseGraph(
                result.poseGraphs,
                result.poseGraphLayouts,
                result.poseStateMachines,
                result.poseStateMachineLayouts,
                poseAsset,
                profile,
                new HashSet<PoseGraphId>());
        }

        static void AppendPoseGraph(
            ICollection<AgentPackagePoseGraphFile> graphs,
            ICollection<AgentPackagePoseGraphLayoutFile> layouts,
            ICollection<AgentPackagePoseStateMachineFile> stateMachines,
            ICollection<AgentPackagePoseStateMachineLayoutFile> stateMachineLayouts,
            CharacterPresentationPoseGraphAsset poseAsset,
            CharacterAnimationPresentationProfile profile,
            ISet<PoseGraphId> linkedEntryGraphs)
        {
            foreach (CharacterPoseCanvasGraph graph in poseAsset.EnumerateGraphs())
            {
                if (graph == null)
                    throw new InvalidOperationException(
                        "Pose Graph root-owned catalog contains a missing record.");
                GraphAuthoringDocumentRoleId role =
                    CharacterPoseGraphAuthoringCapabilities.ResolveGraphRole(
                        poseAsset,
                        graph,
                        linkedEntryGraphs);
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
                stateMachines.Add(ExportStateMachine(machine, profile));
                stateMachineLayouts.Add(
                    ExportStateMachineLayout(poseAsset, machine));
            }
        }

        static AgentPackageLinkedPoseImplementationFile ExportLinkedPoseImplementation(
            CharacterLinkedPoseImplementationAsset implementation,
            CharacterAnimationPresentationProfile profile)
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
            AgentPackageObjectReference asset = Asset(implementation, true);
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
                profile,
                entryGraphs);
            return result;
        }

        static AgentPackagePresentationProfileFile ExportProfile(
            CharacterAnimationPresentationProfile profile)
        {
            AgentPackageObjectReference owner = Asset(profile, true);
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
                    .Select(ExportPoseSource)
                    .OrderBy(value => ReferenceIdentity(value.slot), StringComparer.Ordinal)
                    .ToList(),
                poseResources = profile.PoseResourceBindings
                    .Select(ExportPoseResource)
                    .OrderBy(value => ReferenceIdentity(value.slot), StringComparer.Ordinal)
                    .ToList(),
                actionProducers = profile.ProducerBindings
                    .Select(ExportProducer)
                    .OrderBy(value => value.timelineId, StringComparer.Ordinal)
                    .ThenBy(value => value.trackId, StringComparer.Ordinal)
                    .ToList(),
                locomotionSyncGroups = profile.LocomotionSyncGroups
                    .Select(group => new AgentPackageLocomotionSyncGroup
                    {
                        groupId = group.GroupId,
                        members = group.Members.Select(value => Asset(value, true)).ToList()
                    })
                    .OrderBy(value => value.groupId, StringComparer.Ordinal)
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

        static AgentPackagePoseSourceBinding ExportPoseSource(
            CharacterPresentationPoseSourceBinding binding)
        {
            if (!binding || !binding.Slot || !binding.SourceAsset)
                throw new InvalidOperationException("Presentation Profile contains an invalid Pose Source Binding.");
            var result = new AgentPackagePoseSourceBinding
            {
                name = binding.name,
                kind = binding.SourceKind.ToString(),
                slot = Asset(binding.Slot, true),
                binding = Asset(binding, true),
                source = Asset(binding.SourceAsset, true),
                footAnalysisIdentity = binding.FootAnalysisIdentity,
                contentRevision = binding.ContentRevision
            };
            if (binding is CharacterMotionMatchingPoseSourceBinding motionMatching)
            {
                result.searchDomainId = motionMatching.SearchDomainId.Value;
                result.databases = motionMatching.Databases
                    .Select(value => Asset(value, true))
                    .ToList();
            }
            return result;
        }

        static AgentPackagePoseResourceBinding ExportPoseResource(
            CharacterPoseResourceBinding binding)
        {
            if (binding == null)
                throw new InvalidOperationException(
                    "Presentation Profile contains a missing Pose Resource Binding.");
            binding.RequireValid();
            return new AgentPackagePoseResourceBinding
            {
                kind = binding.Slot.Kind.ToString(),
                slot = Asset(binding.Slot, true),
                resource = Asset(binding.Resource, true)
            };
        }

        static AgentPackageLinkedPoseSelectorBinding ExportLinkedPoseSelector(
            CharacterLinkedPoseSelectorBindingAsset selector)
        {
            if (selector is not CharacterEquipmentLinkedPoseSelectionBinding equipment)
                throw new InvalidOperationException(
                    $"Linked Pose selector '{selector?.name ?? "missing"}' has no Document v8 codec.");
            AgentPackageObjectReference asset = Asset(equipment, true);
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

        static void ExportAnimationClips(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AgentDocumentPresentationEditable destination)
        {
            var clips = new HashSet<UnityAnimationClip>();
            void Add(UnityAnimationClip clip)
            {
                if (!clip)
                    return;
                clips.Add(clip);
            }

            foreach (CharacterPoseCanvasGraph graph in EnumeratePoseGraphs(profile))
            {
                for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
                {
                    CharacterPresentationPoseSourceSlot slot =
                        graph.Nodes[nodeIndex]?.Payload == null
                            ? null
                            : CharacterPoseAuthoringMetadata
                                .Require(graph.Nodes[nodeIndex].Kind)
                                .Source(graph.Nodes[nodeIndex].Payload);
                    CharacterPresentationPoseSourceBinding binding =
                        slot ? profile.FindPoseSourceBinding(slot) : null;
                    if (binding is CharacterClipPoseSourceBinding clipBinding)
                        Add(clipBinding.Clip);
                    else if (binding is CharacterBlendSpacePoseSourceBinding blendBinding)
                        for (int sampleIndex = 0;
                             sampleIndex < blendBinding.BlendSpace.Samples.Count;
                             sampleIndex++)
                            Add(blendBinding.BlendSpace.Samples[sampleIndex]?.Clip);
                }
            }
            IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                CharacterAnimationPresentationAuthoringService.DiscoverProducers(profile, definition);
            for (int i = 0; i < producers.Count; i++)
            {
                for (int clipIndex = 0; clipIndex < producers[i].Track.Clips.Count; clipIndex++)
                {
                    if (producers[i].Track.Clips[clipIndex] is BTSMTL.Timeline.AnimationClip segment)
                        Add(segment.Clip);
                }
            }
            foreach (UnityAnimationClip clip in clips.OrderBy(value => AssetDatabase.GetAssetPath(value), StringComparer.Ordinal))
            {
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
                AgentPackageObjectReference reference = Asset(clip, true);
                var file = new AgentPackageAnimationClipCurvesFile
                {
                    id = $"{reference.assetGuid}:{reference.localFileId}",
                    clip = reference,
                    dependencyBaseline = identity.FullDependencyHash,
                    analysisInputHash = identity.AnalysisInputHash,
                    registeredCurveHash = identity.RegisteredCurveHash
                };
                for (int channelIndex = 0;
                     channelIndex < CharacterAnimationClipRegisteredCurveCatalog.Channels.Count;
                     channelIndex++)
                {
                    CharacterAnimationClipRegisteredCurveDescriptor descriptor =
                        CharacterAnimationClipRegisteredCurveCatalog.Channels[channelIndex];
                    AnimationCurve curve;
                    if (!CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                            clip,
                            descriptor.ChannelId,
                            out curve))
                    {
                        continue;
                    }
                    AgentPackageCurve exported = ExportCurve(curve);
                    exported.channelId = descriptor.ChannelId;
                    exported.bounded = descriptor.ValueDomain ==
                                           CharacterAnimationClipRegisteredCurveValueDomain.Normalized01 ||
                                       descriptor.ValueDomain ==
                                           CharacterAnimationClipRegisteredCurveValueDomain.LockMode;
                    exported.minimum = descriptor.ValueDomain ==
                                       CharacterAnimationClipRegisteredCurveValueDomain.Signed
                        ? float.NegativeInfinity
                        : 0f;
                    exported.maximum = descriptor.ValueDomain ==
                                       CharacterAnimationClipRegisteredCurveValueDomain.Normalized01
                        ? 1f
                        : descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.LockMode
                            ? 2f
                            : float.PositiveInfinity;
                    exported.unit = descriptor.Unit;
                    file.curves.Add(exported);
                }
                destination.animationClips.Add(file);
            }
        }

        static IEnumerable<CharacterPoseCanvasGraph> EnumeratePoseGraphs(
            CharacterAnimationPresentationProfile profile)
        {
            var owners = new List<CharacterPresentationPoseGraphAsset>();
            if (profile.PoseGraph)
                owners.Add(profile.PoseGraph);
            for (int implementationIndex = 0;
                 implementationIndex < profile.LinkedPoseImplementations.Count;
                 implementationIndex++)
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    profile.LinkedPoseImplementations[implementationIndex];
                if (implementation == null)
                    continue;
                for (int entryIndex = 0;
                     entryIndex < implementation.Entries.Count;
                     entryIndex++)
                {
                    CharacterPresentationPoseGraphAsset owner =
                        implementation.Entries[entryIndex]?.GraphOwner;
                    if (owner && !owners.Contains(owner))
                        owners.Add(owner);
                }
            }

            var graphIds = new HashSet<string>(StringComparer.Ordinal);
            for (int ownerIndex = 0; ownerIndex < owners.Count; ownerIndex++)
            {
                foreach (CharacterPoseCanvasGraph graph in owners[ownerIndex].EnumerateGraphs())
                {
                    if (graph != null && graphIds.Add(graph.GraphId.Value))
                        yield return graph;
                }
            }
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
                trackId = binding.ProducerId.TrackAuthoringId
            };
        }

        static AgentPackagePoseGraphFile ExportGraph(
            CharacterPoseCanvasGraph graph,
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
            CharacterPoseCanvasNode node,
            GraphAuthoringDocumentRoleId role)
        {
            if (node?.Payload == null)
                throw new InvalidOperationException(
                    "Pose Graph contains a node without typed payload.");
            CharacterPoseAuthoringNodeMetadata metadata =
                CharacterPoseAuthoringMetadata.Require(node.Kind);
            GraphAuthoringCapabilityDescriptor capability =
                CharacterPoseGraphCapabilityProjector.Catalog.Require(
                    new GraphAuthoringCapabilityId(metadata.CapabilityIdentity),
                    CharacterPoseGraphAuthoringCapabilities.Domain,
                    role);
            _ = metadata.ProjectPortShape(node);
            var properties = new JObject();
            foreach (GraphAuthoringFieldDescriptor field in capability.Fields
                         .Where(value => value.AuthoringWritable)
                         .OrderBy(value => value.FieldId))
            {
                properties[field.FieldId.Value] =
                    CharacterPoseAuthoringPayloadCodec.EncodeValue(
                        metadata.ReadField(
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
                            CharacterPoseCanvasGraphDocument.ValueType(value.Kind),
                        direction = value.Direction.ToString(),
                        required = value.Required,
                        order = value.Order,
                        interfacePortId = value.InterfacePortId.Value
                    }).ToList(),
                childDocumentId =
                    metadata.ProjectChildDocumentId(node.Payload)
            };
        }

        static AgentPackagePoseGraphLayoutFile ExportLayout(
            CharacterPoseCanvasGraph graph) =>
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
            CharacterPoseStateMachineDefinition machine,
            CharacterAnimationPresentationProfile profile)
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
                            ? ResolveBlendCurveId(profile, value.CustomBlendCurveSlot)
                            : null,
                        blendProfileAssetId = value.BlendProfileSlot
                            ? ResolveBlendProfileId(profile, value.BlendProfileSlot)
                            : null
                    }).ToList()
            };
        }

        static string ResolveBlendCurveId(
            CharacterAnimationPresentationProfile profile,
            CharacterPoseResourceSlot slot)
        {
            if (!profile || !slot ||
                profile.FindPoseResourceBinding(slot)?.Resource
                    is not CharacterAnimationBlendCurveAsset curve)
            {
                throw new InvalidOperationException(
                    $"Pose State Transition Blend Curve Slot '{slot?.name ?? "<missing>"}' has no exact Profile resource binding.");
            }
            curve.RequireValid();
            return curve.CurveId;
        }

        static string ResolveBlendProfileId(
            CharacterAnimationPresentationProfile profile,
            CharacterPoseResourceSlot slot)
        {
            if (!profile || !slot ||
                profile.FindPoseResourceBinding(slot)?.Resource
                    is not CharacterAnimationBlendProfile blendProfile)
            {
                throw new InvalidOperationException(
                    $"Pose State Transition Blend Profile Slot '{slot?.name ?? "<missing>"}' has no exact Profile resource binding.");
            }
            return blendProfile.ProfileId;
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
                timeDomain = "seconds",
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

        static AgentPackageObjectReference Asset(
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
            return new AgentPackageObjectReference
            {
                assetPath = path,
                assetGuid = guid,
                localFileId = localFileId
            };
        }

        internal static AgentPackageObjectReference ExportAsset(
            UnityEngine.Object asset,
            bool required) => Asset(asset, required);

        internal static string ReferenceIdentity(
            AgentPackageObjectReference reference) =>
            reference == null
                ? string.Empty
                : string.IsNullOrWhiteSpace(reference.localId)
                    ? reference.assetGuid + ":" + reference.localFileId
                    : reference.localId;
    }
}
