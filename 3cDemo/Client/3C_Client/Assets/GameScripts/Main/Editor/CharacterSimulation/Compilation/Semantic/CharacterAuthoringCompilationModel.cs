using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum CharacterCompositionRootRole : byte
    {
        Character = 1,
        EquipmentPersistent = 2,
        EquipmentRoute = 3
    }

    public sealed class CharacterCompositionRoot
    {
        public CharacterCompositionRoot(
            CharacterCompositionRootRole role,
            string ownerIdentity,
            EquipmentFeatureId featureId,
            EquipmentActionRouteId routeId,
            string sourcePath,
            CharacterAuthoringGraphOccurrence occurrence)
        {
            if (!Enum.IsDefined(typeof(CharacterCompositionRootRole), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            FeatureId = featureId;
            RouteId = routeId;
            SourcePath = SimulationIdentity.Require(sourcePath, nameof(sourcePath));
            Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
            if (role == CharacterCompositionRootRole.Character && (featureId.IsValid || routeId.IsValid) ||
                role == CharacterCompositionRootRole.EquipmentPersistent && (!featureId.IsValid || routeId.IsValid) ||
                role == CharacterCompositionRootRole.EquipmentRoute && (!featureId.IsValid || !routeId.IsValid))
            {
                throw new ArgumentException("Character composition root identity is inconsistent.");
            }
        }

        public CharacterCompositionRootRole Role { get; }
        public string OwnerIdentity { get; }
        public EquipmentFeatureId FeatureId { get; }
        public EquipmentActionRouteId RouteId { get; }
        public string SourcePath { get; }
        public CharacterAuthoringGraphOccurrence Occurrence { get; }
        public string Identity => $"{(byte)Role}:{OwnerIdentity}:{FeatureId.Value}:{RouteId.Value}:{Occurrence.Graph.GraphAuthoringId}";
    }

    public sealed class CharacterAuthoringCompilationModel
    {
        readonly ReadOnlyDictionary<string, CharacterAuthoringBlackboardDeclaration> m_Declarations;
        readonly ReadOnlyDictionary<string, TimelineData> m_Timelines;
        readonly ReadOnlyDictionary<UnityEngine.Object, string> m_AssetGuids;

        internal CharacterAuthoringCompilationModel(
            CharacterPipelineDefinition definition,
            string definitionPath,
            string definitionGuid,
            ProgramId programId,
            ProgramRevision sourceRevision,
            IEnumerable<CharacterCompositionRoot> roots,
            IDictionary<string, CharacterAuthoringBlackboardDeclaration> declarations,
            IDictionary<string, TimelineData> timelines,
            IDictionary<UnityEngine.Object, string> assetGuids,
            CharacterSimulationNodeEmitterRegistry nodeEmitters,
            CharacterSimulationTimelineEmitterRegistry timelineEmitters)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            DefinitionPath = definitionPath ?? throw new ArgumentNullException(nameof(definitionPath));
            DefinitionGuid = definitionGuid ?? throw new ArgumentNullException(nameof(definitionGuid));
            ProgramId = programId;
            SourceRevision = sourceRevision;
            CharacterCompositionRoot[] stableRoots = (roots ?? throw new ArgumentNullException(nameof(roots)))
                .OrderBy(value => value.Identity, StringComparer.Ordinal)
                .ToArray();
            if (stableRoots.Length == 0 || stableRoots.Count(value => value.Role == CharacterCompositionRootRole.Character) != 1)
                throw new ArgumentException("Character compilation requires exactly one Character root.", nameof(roots));
            Roots = Array.AsReadOnly(stableRoots);
            Root = stableRoots.Single(value => value.Role == CharacterCompositionRootRole.Character).Occurrence;
            m_Declarations = new ReadOnlyDictionary<string, CharacterAuthoringBlackboardDeclaration>(
                new SortedDictionary<string, CharacterAuthoringBlackboardDeclaration>(declarations, StringComparer.Ordinal));
            m_Timelines = new ReadOnlyDictionary<string, TimelineData>(
                new SortedDictionary<string, TimelineData>(timelines, StringComparer.Ordinal));
            m_AssetGuids = new ReadOnlyDictionary<UnityEngine.Object, string>(
                new Dictionary<UnityEngine.Object, string>(assetGuids));
            NodeEmitters = nodeEmitters ?? throw new ArgumentNullException(nameof(nodeEmitters));
            TimelineEmitters = timelineEmitters ?? throw new ArgumentNullException(nameof(timelineEmitters));
            InputProfile = definition.InputProfile;
            GameplayEffectProfile = definition.GameplayEffectProfile;
            BodyMotionProfile = definition.BodyMotionProfile;
            BodyMotionProfileGuid = GetAssetGuid(BodyMotionProfile);
            BodyMotionSourceIdentity = $"asset:{BodyMotionProfileGuid}";
            BodyMotionContentRevision = ComputeBodyMotionContentRevision(BodyMotionProfile, BodyMotionProfileGuid);
            AnimationPresentationProfile = definition.AnimationPresentationProfile;
            ActionProfiles = definition.BuildCompiledActionProfileCatalog();
            BehaviorProfiles = definition.BehaviorProfiles.Where(value => value).OrderBy(value => value.BehaviorId, StringComparer.Ordinal).ToArray();
            InputValues = InputProfile ? InputProfile.InputValues.Where(value => value != null).OrderBy(value => value.InputValueId, StringComparer.Ordinal).ToArray() : Array.Empty<CharacterInputValueDefinition>();
            InputRequests = InputProfile ? InputProfile.ActionRequests.Where(value => value != null).OrderBy(value => value.RequestId, StringComparer.Ordinal).ToArray() : Array.Empty<CharacterActionRequestDefinition>();
            TagDefinitions = GameplayEffectProfile && GameplayEffectProfile.TagCatalog
                ? GameplayEffectProfile.TagCatalog.Tags.Where(value => value != null).OrderBy(value => value.TagId.Value, StringComparer.Ordinal).ToArray()
                : Array.Empty<GameplayTagDefinition>();
            InitialTags = GameplayEffectProfile ? GameplayEffectProfile.InitialTags.OrderBy(value => value.Value, StringComparer.Ordinal).ToArray() : Array.Empty<GameplayTagId>();
            InitialAttributes = GameplayEffectProfile
                ? GameplayEffectProfile.InitialAttributes.Where(value => value != null).OrderBy(value => value.Definition ? value.Definition.AttributeId.Value : string.Empty, StringComparer.Ordinal).ToArray()
                : Array.Empty<InitialGameplayAttributeValue>();
            AttributeDefinitions = GameplayEffectProfile
                ? GameplayEffectProfile.AttributeDefinitions.Where(value => value).OrderBy(value => value.AttributeId.Value, StringComparer.Ordinal).ToArray()
                : Array.Empty<GameplayAttributeDefinition>();
            EffectDefinitions = GameplayEffectProfile
                ? GameplayEffectProfile.EffectDefinitions.Where(value => value).OrderBy(value => value.EffectId.Value, StringComparer.Ordinal).ToArray()
                : Array.Empty<GameplayEffectDefinition>();
        }

        public CharacterPipelineDefinition Definition { get; }
        public string DefinitionPath { get; }
        public string DefinitionGuid { get; }
        public ProgramId ProgramId { get; }
        public ProgramRevision SourceRevision { get; }
        public int TickRate => Definition.SimulationTickRate;
        public CharacterAuthoringGraphOccurrence Root { get; }
        public IReadOnlyList<CharacterCompositionRoot> Roots { get; }
        public IReadOnlyDictionary<string, CharacterAuthoringBlackboardDeclaration> Declarations => m_Declarations;
        public IReadOnlyDictionary<string, TimelineData> Timelines => m_Timelines;
        public CharacterInputProfile InputProfile { get; }
        public CharacterGameplayEffectProfile GameplayEffectProfile { get; }
        public CharacterBodyMotionProfile BodyMotionProfile { get; }
        public string BodyMotionProfileGuid { get; }
        public string BodyMotionSourceIdentity { get; }
        public StableHash BodyMotionContentRevision { get; }
        public ThirdPersonCharacter.Pipeline.Animation.CharacterAnimationPresentationProfile AnimationPresentationProfile { get; }
        public IReadOnlyList<ActionProfile> ActionProfiles { get; }
        public IReadOnlyList<GameplayBehaviorProfile> BehaviorProfiles { get; }
        public IReadOnlyList<CharacterInputValueDefinition> InputValues { get; }
        public IReadOnlyList<CharacterActionRequestDefinition> InputRequests { get; }
        public IReadOnlyList<GameplayTagDefinition> TagDefinitions { get; }
        public IReadOnlyList<GameplayTagId> InitialTags { get; }
        public IReadOnlyList<InitialGameplayAttributeValue> InitialAttributes { get; }
        public IReadOnlyList<GameplayAttributeDefinition> AttributeDefinitions { get; }
        public IReadOnlyList<GameplayEffectDefinition> EffectDefinitions { get; }

        public static StableHash ComputeBodyMotionContentRevision(CharacterBodyMotionProfile profile, string assetGuid)
        {
            if (!profile || string.IsNullOrWhiteSpace(assetGuid))
                throw new ArgumentException("Body Motion Profile identity is incomplete.");
            return StableHash.Compute(
                $"character-body-motion-profile/{CharacterBodyMotionProfile.SemanticVersion}",
                assetGuid,
                profile.GravityAcceleration.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                profile.MaximumFallSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        internal CharacterSimulationNodeEmitterRegistry NodeEmitters { get; }
        internal CharacterSimulationTimelineEmitterRegistry TimelineEmitters { get; }
        internal string GetAssetGuid(UnityEngine.Object asset)
        {
            if (!asset || !m_AssetGuids.TryGetValue(asset, out string guid))
                throw new InvalidOperationException($"Asset '{asset?.name}' was not registered by Authoring Discovery.");
            return guid;
        }
    }

    public sealed class CharacterAuthoringGraphOccurrence
    {
        internal CharacterAuthoringGraphOccurrence(
            BaseTree graph,
            string route,
            IEnumerable<BaseExposedProperty> declarations,
            IEnumerable<BaseNode> nodes,
            IEnumerable<CharacterAuthoringEdgeRecord> edges,
            IEnumerable<CharacterAuthoringEdgeRecord> propertyEdges,
            IEnumerable<CharacterAuthoringGraphReferenceRecord> graphReferences,
            IEnumerable<CharacterAuthoringTimelineRecord> timelines,
            string entryNodeId)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Declarations = Array.AsReadOnly((declarations ?? Array.Empty<BaseExposedProperty>()).ToArray());
            Nodes = Array.AsReadOnly((nodes ?? Array.Empty<BaseNode>()).ToArray());
            Edges = Array.AsReadOnly((edges ?? Array.Empty<CharacterAuthoringEdgeRecord>()).ToArray());
            PropertyEdges = Array.AsReadOnly((propertyEdges ?? Array.Empty<CharacterAuthoringEdgeRecord>()).ToArray());
            GraphReferences = Array.AsReadOnly((graphReferences ?? Array.Empty<CharacterAuthoringGraphReferenceRecord>()).ToArray());
            Timelines = Array.AsReadOnly((timelines ?? Array.Empty<CharacterAuthoringTimelineRecord>()).ToArray());
            EntryNodeId = entryNodeId ?? string.Empty;
        }

        public BaseTree Graph { get; }
        public string Route { get; }
        public IReadOnlyList<BaseExposedProperty> Declarations { get; }
        public IReadOnlyList<BaseNode> Nodes { get; }
        public IReadOnlyList<CharacterAuthoringEdgeRecord> Edges { get; }
        public IReadOnlyList<CharacterAuthoringEdgeRecord> PropertyEdges { get; }
        public IReadOnlyList<CharacterAuthoringGraphReferenceRecord> GraphReferences { get; }
        public IReadOnlyList<CharacterAuthoringTimelineRecord> Timelines { get; }
        public string EntryNodeId { get; }
    }

    public sealed class CharacterAuthoringEdgeRecord
    {
        internal CharacterAuthoringEdgeRecord(BaseEdge edge, string route, CharacterAuthoringGraphOccurrence conditionGraph)
        {
            Edge = edge ?? throw new ArgumentNullException(nameof(edge));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            ConditionGraph = conditionGraph;
        }

        public BaseEdge Edge { get; }
        public string Route { get; }
        public CharacterAuthoringGraphOccurrence ConditionGraph { get; }
    }

    public sealed class CharacterAuthoringGraphReferenceRecord
    {
        internal CharacterAuthoringGraphReferenceRecord(BaseNode owner, NodeGraphReference reference, string route, CharacterAuthoringGraphOccurrence child)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Reference = reference;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        public BaseNode Owner { get; }
        public NodeGraphReference Reference { get; }
        public string Route { get; }
        public CharacterAuthoringGraphOccurrence Child { get; }
    }

    public sealed class CharacterAuthoringTimelineRecord
    {
        internal CharacterAuthoringTimelineRecord(TimelineNode node, TimelineData timeline, string graphRoute, string route, IEnumerable<CharacterAuthoringTrackRecord> tracks)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            GraphRoute = graphRoute ?? throw new ArgumentNullException(nameof(graphRoute));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Tracks = Array.AsReadOnly((tracks ?? Array.Empty<CharacterAuthoringTrackRecord>()).ToArray());
        }

        public TimelineNode Node { get; }
        public TimelineData Timeline { get; }
        public string GraphRoute { get; }
        public string Route { get; }
        public IReadOnlyList<CharacterAuthoringTrackRecord> Tracks { get; }
    }

    public sealed class CharacterAuthoringTrackRecord
    {
        internal CharacterAuthoringTrackRecord(Track track, int authoringIndex, string route, IEnumerable<CharacterAuthoringClipRecord> clips)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Clips = Array.AsReadOnly((clips ?? Array.Empty<CharacterAuthoringClipRecord>()).ToArray());
        }

        public Track Track { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
        public IReadOnlyList<CharacterAuthoringClipRecord> Clips { get; }
    }

    public sealed class CharacterAuthoringClipRecord
    {
        internal CharacterAuthoringClipRecord(Clip clip, int authoringIndex, string route, CharacterAuthoringGraphOccurrence treeGraph)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            TreeGraph = treeGraph;
        }

        public Clip Clip { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
        public CharacterAuthoringGraphOccurrence TreeGraph { get; }
    }

    public sealed class CharacterAuthoringBlackboardDeclaration
    {
        internal CharacterAuthoringBlackboardDeclaration(BaseTree graph, BaseExposedProperty declaration, string route)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public BaseTree Graph { get; }
        public BaseExposedProperty Declaration { get; }
        public string Route { get; }
    }

    public sealed class CharacterAuthoringDiscovery
    {
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly CharacterSimulationTimelineEmitterRegistry m_TimelineEmitters;
        readonly Dictionary<string, IdentityOwner> m_Identities = new Dictionary<string, IdentityOwner>(StringComparer.Ordinal);
        readonly Dictionary<string, CharacterAuthoringBlackboardDeclaration> m_Declarations = new Dictionary<string, CharacterAuthoringBlackboardDeclaration>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineData> m_Timelines = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Dictionary<UnityEngine.Object, string> m_AssetGuids = new Dictionary<UnityEngine.Object, string>();
        readonly HashSet<string> m_Routes = new HashSet<string>(StringComparer.Ordinal);

        public CharacterAuthoringDiscovery(CharacterSimulationCompileReport report)
        {
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_NodeEmitters = CharacterSimulationNodeEmitterRegistry.CreateDefault();
            m_TimelineEmitters = CharacterSimulationTimelineEmitterRegistry.CreateDefault();
        }

        public CharacterAuthoringCompilationModel Discover(
            CharacterPipelineDefinition definition,
            string definitionPath,
            string definitionGuid,
            ProgramRevision sourceRevision,
            BaseTree root)
        {
            ValidateDefinition(definition, definitionPath, definitionGuid, root);
            if (!m_Report.IsValid)
                return null;
            var roots = new List<CharacterCompositionRoot>();
            DiscoverCompositionRoot(
                roots,
                CharacterCompositionRootRole.Character,
                $"asset:{definitionGuid}",
                default,
                default,
                definitionPath,
                root,
                $"root:{root.GraphAuthoringId}");
            DiscoverEquipmentRoots(definition, roots);
            if (roots.Count == 0 || !m_Report.IsValid)
                return null;
            return new CharacterAuthoringCompilationModel(
                definition,
                definitionPath,
                definitionGuid,
                new ProgramId($"character:{definitionGuid}"),
                sourceRevision,
                roots,
                m_Declarations,
                m_Timelines,
                m_AssetGuids,
                m_NodeEmitters,
                m_TimelineEmitters);
        }

        void DiscoverEquipmentRoots(CharacterPipelineDefinition definition, List<CharacterCompositionRoot> roots)
        {
            if (!definition.EquipmentCapabilityEnabled || !definition.EquipmentProfile)
                return;
            CharacterEquipmentFeatureDefinition[] features = definition.EquipmentProfile.Features
                .Where(value => value)
                .OrderBy(value => value.FeatureIdValue, StringComparer.Ordinal)
                .ToArray();
            for (int featureIndex = 0; featureIndex < features.Length; featureIndex++)
            {
                CharacterEquipmentFeatureDefinition feature = features[featureIndex];
                string featureGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(feature));
                string ownerIdentity = $"asset:{featureGuid}";
                if (feature.PersistentGraph != null)
                {
                    DiscoverCompositionRoot(
                        roots,
                        CharacterCompositionRootRole.EquipmentPersistent,
                        ownerIdentity,
                        feature.FeatureId,
                        default,
                        $"{AssetDatabase.GetAssetPath(feature)}#persistent",
                        feature.PersistentGraph,
                        $"equipment:feature:{feature.FeatureIdValue}:persistent:{feature.PersistentGraph.GraphAuthoringId}");
                }
                EquipmentFeatureRouteImplementation[] routes = feature.RouteImplementations
                    .Where(value => value != null)
                    .OrderBy(value => value.RouteIdValue, StringComparer.Ordinal)
                    .ToArray();
                for (int routeIndex = 0; routeIndex < routes.Length; routeIndex++)
                {
                    EquipmentFeatureRouteImplementation route = routes[routeIndex];
                    if (route.InlineGraph == null)
                        continue;
                    DiscoverCompositionRoot(
                        roots,
                        CharacterCompositionRootRole.EquipmentRoute,
                        ownerIdentity,
                        feature.FeatureId,
                        route.RouteId,
                        $"{AssetDatabase.GetAssetPath(feature)}#route:{route.RouteIdValue}",
                        route.InlineGraph,
                        $"equipment:feature:{feature.FeatureIdValue}:route:{route.RouteIdValue}:{route.InlineGraph.GraphAuthoringId}");
                }
            }
        }

        void DiscoverCompositionRoot(
            List<CharacterCompositionRoot> roots,
            CharacterCompositionRootRole role,
            string ownerIdentity,
            EquipmentFeatureId featureId,
            EquipmentActionRouteId routeId,
            string sourcePath,
            BaseTree graph,
            string route)
        {
            if (graph == null)
            {
                m_Report.DiscoveryError("composition_root_graph_missing", sourcePath, $"Composition root '{role}' has no Graph.");
                return;
            }
            NestedGraphValidationResult validation = graph.ValidateNestedGraphReferences();
            for (int i = 0; i < validation.Issues.Count; i++)
            {
                NestedGraphValidationIssue issue = validation.Issues[i];
                m_Report.DiscoveryError(
                    $"nested_graph_{issue.Kind}",
                    $"{role}/{ownerIdentity}/{issue.Tree?.GraphAuthoringId}/{issue.Node?.GUID}/{issue.Key}",
                    issue.Message);
            }
            CharacterAuthoringGraphOccurrence occurrence = DiscoverGraph(graph, route, new List<BaseTree>());
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology = CharacterAuthoringTopologyProjection.Build(graph, topologyErrors);
            for (int i = 0; i < topologyErrors.Count; i++)
                m_Report.DiscoveryError("composition_root_topology_invalid", route, topologyErrors[i]);
            if (topology.IsValid)
            {
                ValidateActionWindowQueries(topology);
                ValidateActionTargetChains(topology);
            }
            if (occurrence == null)
                return;
            string identity = $"{(byte)role}:{ownerIdentity}:{featureId.Value}:{routeId.Value}:{graph.GraphAuthoringId}";
            if (roots.Any(value => string.Equals(value.Identity, identity, StringComparison.Ordinal)))
            {
                m_Report.DiscoveryError("composition_root_duplicate", route, $"Composition root '{identity}' is duplicated.");
                return;
            }
            roots.Add(new CharacterCompositionRoot(role, ownerIdentity, featureId, routeId, sourcePath, occurrence));
        }

        void ValidateActionWindowQueries(CharacterAuthoringTopologyProjection topology)
        {
            for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
            {
                CharacterAuthoringGraphEntry entry = topology.Graphs[graphIndex];
                if (!entry.FirstOccurrence)
                    continue;

                HashSet<string> visibleOwnerIds = entry.VisibleGraphs
                    .OfType<BaseTree>()
                    .Select(value => value.GraphAuthoringId)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (ActionWindowActiveInfoNode query in entry.Graph.Nodes.OfType<ActionWindowActiveInfoNode>())
                {
                    string source = $"{entry.Route}/node:{query.GUID}";
                    if (string.IsNullOrWhiteSpace(query.WindowType))
                    {
                        m_Report.DiscoveryError("action_window_type_missing", source, "ActionWindowActive requires a non-empty WindowType.");
                        continue;
                    }

                    bool matched = false;
                    var candidates = new List<string>();
                    for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
                    {
                        TimelineData timeline = topology.Timelines[timelineIndex].Timeline;
                        foreach (TreeClip clip in timeline.Tracks.OfType<TreeTrack>().SelectMany(value => value.Clips).OfType<TreeClip>())
                        {
                            if (clip.ResolvedTree == null)
                                continue;
                            foreach (ExposedPropertyNode setter in clip.ResolvedTree.Nodes.OfType<ExposedPropertyNode>())
                            {
                                PipelineBlackboardVariableReference reference = setter.BlackboardVariable;
                                if (setter.NodeType != ExposedPropertyNodeType.Set || !reference.IsValid)
                                    continue;
                                if (!m_Declarations.TryGetValue(DeclarationIdentity(reference.DeclarationOwnerId, reference.DeclarationId), out CharacterAuthoringBlackboardDeclaration declarationRecord))
                                    continue;
                                BaseExposedProperty declaration = declarationRecord.Declaration;
                                if (declaration.BlackboardFactProjection != PipelineBlackboardFactProjectionKind.ActionWindow ||
                                    !string.Equals(declaration.ActionWindowType, query.WindowType, StringComparison.Ordinal))
                                    continue;

                                candidates.Add($"owner={declarationRecord.Graph.GraphAuthoringId},phase={clip.ExecutionPhase},windowId={declaration.ActionWindowId},clip={clip.AuthoringId}");
                                if (clip.ExecutionPhase == TimelineTreeExecutionPhase.Decision &&
                                    visibleOwnerIds.Contains(declarationRecord.Graph.GraphAuthoringId))
                                    matched = true;
                            }
                        }
                    }

                    if (!matched)
                    {
                        string available = candidates.Count == 0 ? "none" : string.Join(";", candidates);
                        m_Report.DiscoveryError(
                            "action_window_phase_unavailable",
                            source,
                            $"WindowType '{query.WindowType}' has no visible Decision TreeClip projection for the current frame. VisibleOwners={string.Join(",", visibleOwnerIds.OrderBy(value => value, StringComparer.Ordinal))}; Candidates={available}.");
                    }
                }
            }
        }

        void ValidateActionTargetChains(CharacterAuthoringTopologyProjection topology)
        {
            var issues = new List<ActionTargetAuthoringIssue>();
            ActionTargetAuthoringValidation.Collect(topology, issues);
            for (int i = 0; i < issues.Count; i++)
            {
                ActionTargetAuthoringIssue issue = issues[i];
                m_Report.DiscoveryError(issue.Code, issue.Path, issue.Message);
            }
        }

        CharacterAuthoringGraphOccurrence DiscoverGraph(BaseTree graph, string route, List<BaseTree> stack)
        {
            if (graph == null)
                return null;
            if (stack.Contains(graph))
            {
                m_Report.DiscoveryError("graph_cycle", route, $"Graph reference cycle reaches '{graph.GraphAuthoringId}'.");
                return null;
            }
            if (!m_Routes.Add(route))
            {
                m_Report.DiscoveryError("graph_route_duplicate", route, "Graph route is duplicated.");
                return null;
            }
            stack.Add(graph);
            try
            {
                graph.RebindReadOnlyViewReferences();
                RegisterIdentity(graph.GraphAuthoringId, graph, "Graph", route);
                ValidateSerializedOwner(graph.SerializedOwner, "Graph", route);
                BaseExposedProperty[] declarations = graph.ExposedProperties.Where(value => value != null).OrderBy(value => value.DeclarationId, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < graph.ExposedProperties.Count; i++)
                {
                    if (graph.ExposedProperties[i] == null)
                        m_Report.DiscoveryError("blackboard_declaration_missing", route, "Graph contains a missing Blackboard declaration.");
                }
                for (int i = 0; i < declarations.Length; i++)
                    DiscoverDeclaration(graph, declarations[i], route);

                BaseNode[] nodes = graph.Nodes.Where(value => value != null).OrderBy(value => value.GUID, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    if (graph.Nodes[i] == null)
                        m_Report.DiscoveryError("node_missing", route, "Graph contains a missing serialized Node.");
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    BaseNode node = nodes[i];
                    RegisterIdentity(node.GUID, node, "Node", $"{route}/node:{node.GUID}");
                    ValidateModules(node, route);
                    ValidateAssetReferences(node, route);
                    if (!m_NodeEmitters.TryGet(node.GetType(), out _))
                        m_Report.DiscoveryError("node_emitter_missing", $"{route}/node:{node.GUID}", $"Node type '{node.GetType().FullName}' has no Character Simulation emitter.");
                }

                CharacterAuthoringEdgeRecord[] edges = DiscoverEdges(graph.Edges, graph, route, stack);
                CharacterAuthoringEdgeRecord[] propertyEdges = DiscoverEdges(graph.PropertyEdges, graph, route, stack);
                var graphReferences = new List<CharacterAuthoringGraphReferenceRecord>();
                var timelines = new List<CharacterAuthoringTimelineRecord>();
                for (int i = 0; i < nodes.Length; i++)
                {
                    BaseNode node = nodes[i];
                    if (node is TimelineNode timelineNode)
                    {
                        CharacterAuthoringTimelineRecord timeline = DiscoverTimeline(graph, timelineNode, route, stack);
                        if (timeline != null)
                            timelines.Add(timeline);
                    }
                    NodeGraphReference[] references = node.GetGraphReferences().OrderBy(value => value.Key, StringComparer.Ordinal).ToArray();
                    for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
                    {
                        NodeGraphReference reference = references[referenceIndex];
                        string referenceRoute = GraphReferenceRoute(route, node, reference);
                        if (reference.Required && reference.Tree == null)
                        {
                            m_Report.DiscoveryError("graph_reference_missing", referenceRoute, $"Required graph reference '{reference.Label}' is missing.");
                            continue;
                        }
                        if (reference.Tree == null)
                            continue;
                        if ((reference.Inline && reference.SharedAsset) || (!reference.Inline && !reference.SharedAsset))
                            m_Report.DiscoveryError("graph_reference_ownership_invalid", referenceRoute, "Graph reference must be exactly one of inline or shared.");
                        if ((node is StateMachineNode || node is StateNode) && string.IsNullOrEmpty(reference.ScopeId))
                            m_Report.DiscoveryError("graph_scope_missing", referenceRoute, "State graph reference requires a stable scope identity.");
                        CharacterAuthoringGraphOccurrence child = DiscoverGraph(reference.Tree, referenceRoute, stack);
                        if (child != null)
                            graphReferences.Add(new CharacterAuthoringGraphReferenceRecord(node, reference, referenceRoute, child));
                    }
                }
                string entryNodeId = ResolveEntryNodeId(graph, nodes, route);
                return new CharacterAuthoringGraphOccurrence(graph, route, declarations, nodes, edges, propertyEdges, graphReferences, timelines, entryNodeId);
            }
            catch (Exception exception)
            {
                m_Report.DiscoveryError("graph_discovery_failed", route, exception.ToString());
                return null;
            }
            finally
            {
                stack.RemoveAt(stack.Count - 1);
            }
        }

        CharacterAuthoringEdgeRecord[] DiscoverEdges<T>(IReadOnlyList<T> source, BaseTree graph, string route, List<BaseTree> stack) where T : BaseEdge
        {
            var records = new List<CharacterAuthoringEdgeRecord>();
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null)
                    m_Report.DiscoveryError("edge_missing", route, "Graph contains a missing serialized Edge.");
            }
            T[] edges = source.Where(value => value != null).OrderBy(value => value.GUID, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < edges.Length; i++)
            {
                BaseEdge edge = edges[i];
                string edgeRoute = $"{route}/edge:{edge.GUID}";
                RegisterIdentity(edge.GUID, edge, edge is PropertyEdge ? "PropertyEdge" : "Edge", edgeRoute);
                if (string.IsNullOrEmpty(edge.StartNodeGUID) || string.IsNullOrEmpty(edge.EndNodeGUID) ||
                    !graph.Nodes.Any(value => value != null && value.GUID == edge.StartNodeGUID) ||
                    !graph.Nodes.Any(value => value != null && value.GUID == edge.EndNodeGUID))
                {
                    m_Report.DiscoveryError("edge_endpoint_invalid", edgeRoute, "Edge references a missing endpoint Node.");
                }
                CharacterAuthoringGraphOccurrence condition = null;
                if (edge.HasConditionRuleGraphConfiguration)
                {
                    if (!edge.TryResolveConditionRuleGraph(out ConditionRuleGraph conditionGraph, out string error))
                        m_Report.DiscoveryError("condition_graph_invalid", edgeRoute, error);
                    else
                        condition = DiscoverGraph(conditionGraph, $"{edgeRoute}/condition:{conditionGraph.GraphAuthoringId}", stack);
                }
                records.Add(new CharacterAuthoringEdgeRecord(edge, edgeRoute, condition));
            }
            return records.ToArray();
        }

        CharacterAuthoringTimelineRecord DiscoverTimeline(BaseTree ownerGraph, TimelineNode node, string graphRoute, List<BaseTree> stack)
        {
            string nodeRoute = $"{graphRoute}/node:{node.GUID}";
            TimelineData timeline = node.Timeline;
            if (timeline == null || node.TimelineOwnership == TimelineOwnership.Missing)
            {
                m_Report.DiscoveryError("timeline_missing", nodeRoute, "TimelineNode is missing its formal inline/shared Timeline.");
                return null;
            }
            if (node.TimelineOwnership == TimelineOwnership.Inline && !ReferenceEquals(timeline.SerializedOwner, ownerGraph.SerializedOwner))
                m_Report.DiscoveryError("timeline_inline_owner_mismatch", nodeRoute, "Inline Timeline serialized owner does not match its Graph owner.");
            if (node.TimelineOwnership == TimelineOwnership.Shared && !ReferenceEquals(timeline.SerializedOwner, node.SharedTimelineAsset))
                m_Report.DiscoveryError("timeline_shared_owner_mismatch", nodeRoute, "Shared Timeline serialized owner does not match its Timeline asset.");
            ValidateSerializedOwner(timeline.SerializedOwner, "Timeline", nodeRoute);
            timeline.Init();
            var timelineErrors = new List<string>();
            if (!timeline.ValidateAuthoringIdentities(timelineErrors))
            {
                for (int i = 0; i < timelineErrors.Count; i++)
                    m_Report.DiscoveryError("timeline_identity_invalid", nodeRoute, timelineErrors[i]);
            }
            string timelineRoute = $"{nodeRoute}/timeline:{timeline.AuthoringId}";
            RegisterIdentity(timeline.AuthoringId, timeline, "Timeline", timelineRoute);
            if (m_Timelines.TryGetValue(timeline.AuthoringId, out TimelineData existing) && !ReferenceEquals(existing, timeline))
                m_Report.DiscoveryError("timeline_identity_duplicate", nodeRoute, $"Timeline identity '{timeline.AuthoringId}' belongs to multiple authoring objects.");
            else
                m_Timelines[timeline.AuthoringId] = timeline;
            var tracks = new List<CharacterAuthoringTrackRecord>();
            Track[] stableTracks = timeline.Tracks.Where(value => value != null).OrderBy(value => value.AuthoringId, StringComparer.Ordinal).ToArray();
            if (stableTracks.Length != timeline.Tracks.Count)
                m_Report.DiscoveryError("timeline_track_missing", timelineRoute, "Timeline contains a missing Track.");
            for (int i = 0; i < stableTracks.Length; i++)
            {
                Track track = stableTracks[i];
                int authoringIndex = IndexOfReference(timeline.Tracks, track);
                string trackRoute = $"{timelineRoute}/track:{track.AuthoringId}";
                RegisterIdentity(track.AuthoringId, track, "TimelineTrack", trackRoute);
                if (!m_TimelineEmitters.TryGetTrack(track.GetType(), out _))
                    m_Report.DiscoveryError("timeline_track_emitter_missing", trackRoute, $"Track type '{track.GetType().FullName}' has no Character Simulation emitter.");
                var clips = new List<CharacterAuthoringClipRecord>();
                Clip[] stableClips = track.Clips.Where(value => value != null).OrderBy(value => value.AuthoringId, StringComparer.Ordinal).ToArray();
                if (stableClips.Length != track.Clips.Count)
                    m_Report.DiscoveryError("timeline_clip_missing", trackRoute, "Timeline Track contains a missing Clip.");
                for (int clipIndex = 0; clipIndex < stableClips.Length; clipIndex++)
                {
                    Clip clip = stableClips[clipIndex];
                    string clipRoute = $"{trackRoute}/clip:{clip.AuthoringId}";
                    RegisterIdentity(clip.AuthoringId, clip, "TimelineClip", clipRoute);
                    if (!m_TimelineEmitters.TryGetClip(clip.GetType(), out _))
                        m_Report.DiscoveryError("timeline_clip_emitter_missing", clipRoute, $"Clip type '{clip.GetType().FullName}' has no Character Simulation emitter.");
                    if (clip.EndFrame <= clip.StartFrame)
                        m_Report.DiscoveryError("timeline_clip_range_invalid", clipRoute, "Timeline Clip requires EndFrame greater than StartFrame.");
                    if (clip is BTSMTL.Timeline.AnimationClip animation && !animation.Clip)
                        m_Report.DiscoveryError("animation_projection_missing", clipRoute, "Animation Clip resource is missing from the authoring source.");
                    CharacterAuthoringGraphOccurrence treeGraph = null;
                    if (clip is TreeClip treeClip)
                    {
                        if (treeClip.Ownership == TimelineTreeOwnership.Missing || treeClip.ResolvedTree == null)
                            m_Report.DiscoveryError("tree_clip_graph_missing", clipRoute, "TreeClip is missing its formal inline/shared Tree.");
                        else
                        {
                            if (treeClip.Ownership == TimelineTreeOwnership.Inline && !ReferenceEquals(treeClip.ResolvedTree.SerializedOwner, timeline.SerializedOwner))
                                m_Report.DiscoveryError("tree_clip_inline_owner_mismatch", clipRoute, "Inline TreeClip graph serialized owner does not match its Timeline owner.");
                            if (treeClip.Ownership == TimelineTreeOwnership.Shared && !ReferenceEquals(treeClip.ResolvedTree.SerializedOwner, treeClip.SharedTreeAsset))
                                m_Report.DiscoveryError("tree_clip_shared_owner_mismatch", clipRoute, "Shared TreeClip graph serialized owner does not match its Tree asset.");
                            treeGraph = DiscoverGraph(treeClip.ResolvedTree, $"{clipRoute}/tree:{treeClip.ResolvedTree.GraphAuthoringId}", stack);
                        }
                    }
                    clips.Add(new CharacterAuthoringClipRecord(clip, IndexOfReference(track.Clips, clip), clipRoute, treeGraph));
                }
                tracks.Add(new CharacterAuthoringTrackRecord(track, authoringIndex, trackRoute, clips));
            }
            if (node.PlaybackMode == TimelinePlaybackMode.Loop && timeline.MaxFrame <= 0)
                m_Report.DiscoveryError("timeline_loop_duration_invalid", timelineRoute, "Loop Timeline duration must be greater than zero.");
            return new CharacterAuthoringTimelineRecord(node, timeline, graphRoute, timelineRoute, tracks);
        }

        void DiscoverDeclaration(BaseTree graph, BaseExposedProperty declaration, string route)
        {
            string source = $"{route}/blackboard:{declaration.DeclarationId}";
            RegisterIdentity(declaration.DeclarationId, declaration, "BlackboardDeclaration", source);
            string key = DeclarationIdentity(graph.GraphAuthoringId, declaration.DeclarationId);
            if (m_Declarations.TryGetValue(key, out CharacterAuthoringBlackboardDeclaration existing))
            {
                if (!ReferenceEquals(existing.Declaration, declaration))
                    m_Report.DiscoveryError("blackboard_declaration_duplicate", source, $"Declaration identity '{key}' belongs to multiple objects.");
                return;
            }
            if (!PipelineBlackboardVariablePolicy.IsValid(declaration.BlackboardScope, declaration.BlackboardLifetime))
                m_Report.DiscoveryError("blackboard_policy_invalid", source, $"Scope '{declaration.BlackboardScope}' cannot use lifetime '{declaration.BlackboardLifetime}'.");
            if (!PipelineBlackboardVariablePolicy.TryValidateInputBinding(declaration, out string inputBindingError))
                m_Report.DiscoveryError("blackboard_input_binding_invalid", source, inputBindingError);
            if (!PipelineBlackboardFactProjectionPolicy.TryValidate(declaration, out string projectionError))
                m_Report.DiscoveryError("blackboard_projection_invalid", source, projectionError);
            if (!IsPortableBlackboardType(declaration.ValueType))
                m_Report.DiscoveryError("blackboard_type_unsupported", source, $"Blackboard type '{declaration.ValueType?.FullName}' is not portable.");
            m_Declarations.Add(key, new CharacterAuthoringBlackboardDeclaration(graph, declaration, route));
        }

        void ValidateDefinition(CharacterPipelineDefinition definition, string definitionPath, string definitionGuid, BaseTree root)
        {
            if (!definition || string.IsNullOrEmpty(definitionPath) || string.IsNullOrEmpty(definitionGuid) || root == null)
            {
                m_Report.DiscoveryError("definition_identity_missing", definitionPath, "CharacterPipelineDefinition, GUID and Root Tree are required.");
                return;
            }
            var errors = new List<string>();
            try
            {
                definition.CollectConfigurationErrors(errors);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
            for (int i = 0; i < errors.Count; i++)
                m_Report.DiscoveryError("definition_configuration_invalid", $"asset:{definitionGuid}", errors[i]);
            ValidateAssetIdentity(definition, "CharacterPipelineDefinition", definitionPath);
            ValidateAssetIdentity(definition.RootTreeAsset, "RootTree", definitionPath);
            ValidateAssetIdentity(definition.InputProfile, "InputProfile", definitionPath);
            ValidateAssetIdentity(definition.GameplayEffectProfile, "GameplayEffectProfile", definitionPath);
            ValidateAssetIdentity(definition.BodyMotionProfile, "BodyMotionProfile", definitionPath);
            ValidateAssetIdentity(definition.AnimationPresentationProfile, "AnimationPresentationProfile", definitionPath);
            ValidateAssetIdentity(definition.EquipmentProfile, "EquipmentProfile", definitionPath);
            ValidateAssetIdentity(definition.EquipmentPresentationProfile, "EquipmentPresentationProfile", definitionPath);
            IReadOnlyList<ActionProfile> compiledActions = definition.BuildCompiledActionProfileCatalog();
            for (int i = 0; i < compiledActions.Count; i++)
                ValidateAssetIdentity(compiledActions[i], "ActionProfile", definitionPath);
            for (int i = 0; i < definition.BehaviorProfiles.Count; i++)
                ValidateAssetIdentity(definition.BehaviorProfiles[i], "BehaviorProfile", definitionPath);
            if (definition.GameplayEffectProfile)
            {
                ValidateAssetIdentity(definition.GameplayEffectProfile.TagCatalog, "GameplayTagCatalog", definitionPath);
                for (int i = 0; i < definition.GameplayEffectProfile.AttributeDefinitions.Count; i++)
                    ValidateAssetIdentity(definition.GameplayEffectProfile.AttributeDefinitions[i], "GameplayAttributeDefinition", definitionPath);
                for (int i = 0; i < definition.GameplayEffectProfile.EffectDefinitions.Count; i++)
                    ValidateAssetIdentity(definition.GameplayEffectProfile.EffectDefinitions[i], "GameplayEffectDefinition", definitionPath);
            }
            if (definition.EquipmentProfile)
            {
                IReadOnlyList<CharacterEquipmentFeatureDefinition> features = definition.EquipmentProfile.Features;
                for (int i = 0; i < features.Count; i++)
                    ValidateAssetIdentity(features[i], "EquipmentFeature", definitionPath);
                IReadOnlyList<EquipmentDefinition> equipment = definition.EquipmentProfile.Equipment;
                for (int i = 0; i < equipment.Count; i++)
                    ValidateAssetIdentity(equipment[i], "EquipmentDefinition", definitionPath);
            }
            if (definition.EquipmentPresentationProfile)
            {
                IReadOnlyList<EquipmentVisualBindingDefinition> bindings = definition.EquipmentPresentationProfile.VisualBindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    if (bindings[i] != null && bindings[i].VisualPrefab)
                        ValidateAssetIdentity(bindings[i].VisualPrefab, "EquipmentVisualPrefab", definitionPath);
                }
            }
        }

        void ValidateModules(BaseNode node, string route)
        {
            for (int i = 0; i < node.Modules.Count; i++)
            {
                NodeModule module = node.Modules[i];
                if (module == null)
                {
                    m_Report.DiscoveryError("node_module_missing", $"{route}/node:{node.GUID}", $"Node module #{i} is missing.");
                    continue;
                }
                Type type = module.GetType();
                bool supported = type == typeof(ScopedGraphReferenceModule) ||
                                 type == typeof(StateBehaviorGraphReferenceModule) ||
                                 type == typeof(TreeReferenceModule) ||
                                 type == typeof(TimelineOwnershipModule);
                if (!supported)
                    m_Report.DiscoveryError("node_module_emitter_missing", $"{route}/node:{node.GUID}/module:{module.ModuleId}", $"Node Module type '{type.FullName}' has no Character Simulation compiler.");
            }
        }

        void ValidateAssetReferences(BaseNode node, string route)
        {
            foreach (NodeAssetReference reference in node.GetAssetReferences().OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                if (reference.Required && !reference.Asset)
                    m_Report.DiscoveryError("asset_reference_missing", $"{route}/node:{node.GUID}/{reference.Key}", $"Required asset reference '{reference.Label}' is missing.");
                if (reference.Asset)
                    ValidateAssetIdentity(reference.Asset, reference.Label, $"{route}/node:{node.GUID}/{reference.Key}");
            }
        }

        void ValidateSerializedOwner(object owner, string kind, string source)
        {
            if (owner is UnityEngine.Object asset)
                ValidateAssetIdentity(asset, $"{kind} serialized owner", source);
            else
                m_Report.DiscoveryError("serialized_owner_missing", source, $"{kind} serialized owner is missing or is not a Unity asset object.");
        }

        void ValidateAssetIdentity(UnityEngine.Object asset, string kind, string source)
        {
            if (!asset)
                return;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
                m_Report.DiscoveryError("asset_identity_missing", source, $"{kind} '{asset.name}' is not persisted with a Unity asset GUID.");
            else
                m_AssetGuids[asset] = guid;
        }

        void RegisterIdentity(string identity, object owner, string kind, string source)
        {
            if (!AuthoringIdentity.IsValid(identity))
            {
                m_Report.DiscoveryError("authoring_identity_invalid", source, $"{kind} identity '{identity}' is missing or malformed.");
                return;
            }
            if (!m_Identities.TryGetValue(identity, out IdentityOwner existing))
            {
                m_Identities.Add(identity, new IdentityOwner(owner, kind, source));
                return;
            }
            if (!ReferenceEquals(existing.Owner, owner))
                m_Report.DiscoveryError("authoring_identity_duplicate", source, $"{kind} identity '{identity}' is already owned by {existing.Kind} at '{existing.Source}'.");
        }

        string ResolveEntryNodeId(BaseTree graph, IReadOnlyList<BaseNode> nodes, string route)
        {
            BaseNode entry = graph switch
            {
                ConditionRuleGraph => nodes.OfType<ConditionRuleResultNode>().SingleOrDefault(),
                StateMachineGraph stateMachine => stateMachine.EnterNode,
                _ => nodes.OfType<RootNode>().SingleOrDefault()
            };
            if (entry == null)
                m_Report.DiscoveryError("graph_entry_missing", route, $"Graph type '{graph.GetType().FullName}' has no unique entry Node.");
            return entry?.GUID ?? string.Empty;
        }

        static bool IsPortableBlackboardType(Type type)
        {
            return type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(string) ||
                   type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(ActionTargetSnapshot);
        }

        static string DeclarationIdentity(string ownerId, string declarationId) => $"blackboard:{ownerId}:{declarationId}";
        static string GraphReferenceRoute(string route, BaseNode node, NodeGraphReference reference) => $"{route}/node:{node.GUID}/reference:{reference.Key}/scope:{reference.ScopeId}";

        static int IndexOfReference<T>(IReadOnlyList<T> values, T target) where T : class
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (ReferenceEquals(values[i], target))
                    return i;
            }
            throw new InvalidOperationException("Discovered authoring object is absent from its serialized owner list.");
        }

        readonly struct IdentityOwner
        {
            public IdentityOwner(object owner, string kind, string source)
            {
                Owner = owner;
                Kind = kind;
                Source = source;
            }

            public object Owner { get; }
            public string Kind { get; }
            public string Source { get; }
        }
    }
}
