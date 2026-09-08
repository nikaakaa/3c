using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCamera;
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
        Character = 1
    }

    public sealed class CharacterCompositionRoot
    {
        public CharacterCompositionRoot(
            CharacterCompositionRootRole role,
            string ownerIdentity,
            string sourcePath,
            CharacterAuthoringGraphOccurrence occurrence)
        {
            if (!Enum.IsDefined(typeof(CharacterCompositionRootRole), role))
                throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            SourcePath = SimulationIdentity.Require(sourcePath, nameof(sourcePath));
            Occurrence = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
        }

        public CharacterCompositionRootRole Role { get; }
        public string OwnerIdentity { get; }
        public string SourcePath { get; }
        public CharacterAuthoringGraphOccurrence Occurrence { get; }
        public string Identity => $"{(byte)Role}:{OwnerIdentity}:{Occurrence.Graph.GraphAuthoringId}";
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
            IEnumerable<CharacterSkillCompilationRecord> skills,
            IDictionary<string, CharacterAuthoringBlackboardDeclaration> declarations,
            IDictionary<string, TimelineData> timelines,
            IDictionary<UnityEngine.Object, string> assetGuids,
            CharacterSimulationNodeEmitterRegistry nodeEmitters,
            TimelineSemanticEmitterRegistry timelineEmitters)
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
            SkillRecords = Array.AsReadOnly((skills ?? throw new ArgumentNullException(nameof(skills)))
                .OrderBy(value => value.SkillId)
                .ToArray());
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
            CameraProfile = definition.CameraProfile;
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
        public IReadOnlyList<CharacterSkillCompilationRecord> SkillRecords { get; }
        public IReadOnlyDictionary<string, CharacterAuthoringBlackboardDeclaration> Declarations => m_Declarations;
        public IReadOnlyDictionary<string, TimelineData> Timelines => m_Timelines;
        public CharacterInputProfile InputProfile { get; }
        public CharacterGameplayEffectProfile GameplayEffectProfile { get; }
        public CharacterBodyMotionProfile BodyMotionProfile { get; }
        public string BodyMotionProfileGuid { get; }
        public string BodyMotionSourceIdentity { get; }
        public StableHash BodyMotionContentRevision { get; }
        public ThirdPersonCharacter.Pipeline.Animation.CharacterAnimationPresentationProfile AnimationPresentationProfile { get; }
        public CharacterCameraProfile CameraProfile { get; }
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
        internal TimelineSemanticEmitterRegistry TimelineEmitters { get; }
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
            string entryNodeId,
            CharacterAuthoringGraphSignature signature)
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
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
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
        public CharacterAuthoringGraphSignature Signature { get; }
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
        internal CharacterAuthoringGraphReferenceRecord(
            BaseNode owner,
            NodeGraphReference reference,
            string route,
            CharacterAuthoringGraphOccurrence child,
            CharacterAuthoringGraphCallFrame callFrame)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Reference = reference;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Child = child ?? throw new ArgumentNullException(nameof(child));
            CallFrame = callFrame ?? throw new ArgumentNullException(nameof(callFrame));
        }

        public BaseNode Owner { get; }
        public NodeGraphReference Reference { get; }
        public string Route { get; }
        public CharacterAuthoringGraphOccurrence Child { get; }
        public CharacterAuthoringGraphCallFrame CallFrame { get; }
    }

    public sealed class TimelineSemanticContentRecord
    {
        internal TimelineSemanticContentRecord(
            TimelineData timeline,
            string route,
            int maxFrame,
            TimelineContentUnit contentUnit,
            IEnumerable<TimelineSemanticTrackRecord> tracks,
            IReadOnlyDictionary<string, TimelineSemanticTreeRecord> treeRecords)
        {
            Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            if (maxFrame < 0)
                throw new ArgumentOutOfRangeException(nameof(maxFrame));
            ContentUnit = contentUnit ?? throw new ArgumentNullException(nameof(contentUnit));
            MaxFrame = maxFrame;
            Tracks = Array.AsReadOnly((tracks ?? Array.Empty<TimelineSemanticTrackRecord>()).ToArray());
            TreeRecords = new ReadOnlyDictionary<string, TimelineSemanticTreeRecord>(
                new Dictionary<string, TimelineSemanticTreeRecord>(treeRecords ?? new Dictionary<string, TimelineSemanticTreeRecord>(), StringComparer.Ordinal));
        }

        public TimelineData Timeline { get; }
        public string Route { get; }
        public int MaxFrame { get; }
        public TimelineContentUnit ContentUnit { get; }
        public IReadOnlyList<TimelineSemanticTrackRecord> Tracks { get; }
        public IReadOnlyDictionary<string, TimelineSemanticTreeRecord> TreeRecords { get; }
    }

    public sealed class TimelineSemanticTreeRecord
    {
        internal TimelineSemanticTreeRecord(TreeClip clip, TimelineRunningTree tree, string route)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public TreeClip Clip { get; }
        public TimelineRunningTree Tree { get; }
        public string Route { get; }
    }

    public sealed class CharacterAuthoringTimelineRecord
    {
        internal CharacterAuthoringTimelineRecord(
            TimelineNode node,
            TimelineSemanticContentRecord content,
            string graphRoute,
            string route,
            IReadOnlyDictionary<string, CharacterAuthoringGraphOccurrence> treeGraphs)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            GraphRoute = graphRoute ?? throw new ArgumentNullException(nameof(graphRoute));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            TreeGraphs = new ReadOnlyDictionary<string, CharacterAuthoringGraphOccurrence>(
                new Dictionary<string, CharacterAuthoringGraphOccurrence>(treeGraphs ?? new Dictionary<string, CharacterAuthoringGraphOccurrence>(), StringComparer.Ordinal));
        }

        public TimelineNode Node { get; }
        public TimelineSemanticContentRecord Content { get; }
        public TimelineData Timeline => Content.Timeline;
        public string GraphRoute { get; }
        public string Route { get; }
        public IReadOnlyList<TimelineSemanticTrackRecord> Tracks => Content.Tracks;
        public IReadOnlyDictionary<string, CharacterAuthoringGraphOccurrence> TreeGraphs { get; }
    }

    public sealed class TimelineSemanticTrackRecord
    {
        internal TimelineSemanticTrackRecord(
            Track track,
            int authoringIndex,
            string route,
            IEnumerable<TimelineSemanticClipRecord> clips)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Clips = Array.AsReadOnly((clips ?? Array.Empty<TimelineSemanticClipRecord>()).ToArray());
        }

        public Track Track { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
        public IReadOnlyList<TimelineSemanticClipRecord> Clips { get; }
    }

    public sealed class TimelineSemanticClipRecord
    {
        internal TimelineSemanticClipRecord(Clip clip, int authoringIndex, string route)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            AuthoringIndex = authoringIndex;
            Route = route ?? throw new ArgumentNullException(nameof(route));
        }

        public Clip Clip { get; }
        public int AuthoringIndex { get; }
        public string Route { get; }
    }

    public sealed class CharacterBlackboardDeclarationSnapshot
    {
        public CharacterBlackboardDeclarationSnapshot(string identity, string key, Type valueType,
            PipelineBlackboardVariableScope scope, PipelineBlackboardVariableLifetime lifetime, string category,
            object defaultValue, PipelineBlackboardInputBinding inputBinding, PipelineBlackboardFactProjection factProjection)
        {
            DeclarationId = identity;
            BlackboardKey = key;
            ValueType = valueType;
            BlackboardScope = scope;
            BlackboardLifetime = lifetime;
            BlackboardCategoryPath = category;
            DefaultValue = defaultValue;
            InputBinding = inputBinding;
            FactProjection = factProjection;
        }

        public string DeclarationId { get; }
        public string BlackboardKey { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope BlackboardScope { get; }
        public PipelineBlackboardVariableLifetime BlackboardLifetime { get; }
        public string BlackboardCategoryPath { get; }
        public object DefaultValue { get; }
        public PipelineBlackboardInputBinding InputBinding { get; }
        public string InputValueId => InputBinding?.InputValueId ?? string.Empty;
        public PipelineBlackboardFactProjection FactProjection { get; }
    }

    public sealed class CharacterAuthoringBlackboardDeclaration
    {
        internal CharacterAuthoringBlackboardDeclaration(BaseTree graph, BaseExposedProperty declaration, string route)
        {
            GraphId = graph.GraphAuthoringId;
            SourceType = declaration.GetType().FullName;
            ContentHash = GraphAuthoringFingerprint.Compute(graph);
            AuthoringDeclaration = declaration;
            Declaration = new CharacterBlackboardDeclarationSnapshot(declaration.DeclarationId, declaration.BlackboardKey,
                declaration.ValueType, declaration.BlackboardScope, declaration.BlackboardLifetime, declaration.BlackboardCategoryPath,
                declaration.GetValue(), declaration.InputBinding, declaration.FactProjection);
            Route = route;
        }

        internal CharacterAuthoringBlackboardDeclaration(FlowGraph graph, BtsmtlSkillBlackboardDeclaration declaration, string route, string contentHash)
        {
            Variable variable = BtsmtlSkillBlackboardDeclarations.RequireVariable(graph, declaration.VariableId);
            GraphId = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
            SourceType = variable.GetType().FullName;
            ContentHash = contentHash;
            AuthoringDeclaration = variable;
            Declaration = new CharacterBlackboardDeclarationSnapshot(variable.ID, variable.name, variable.varType,
                declaration.Scope, declaration.Lifetime, declaration.Category, ((ISerializedVariableValue)variable).serializedValue,
                declaration.InputBinding, declaration.FactProjection);
            Route = route;
        }

        public string GraphId { get; }
        public string SourceType { get; }
        public string ContentHash { get; }
        public object AuthoringDeclaration { get; }
        public CharacterBlackboardDeclarationSnapshot Declaration { get; }
        public string Route { get; }
    }

    public sealed class CharacterAuthoringDiscovery
    {
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly TimelineSemanticEmitterRegistry m_TimelineEmitters;
        readonly Dictionary<string, IdentityOwner> m_Identities = new Dictionary<string, IdentityOwner>(StringComparer.Ordinal);
        readonly Dictionary<string, CharacterAuthoringBlackboardDeclaration> m_Declarations = new Dictionary<string, CharacterAuthoringBlackboardDeclaration>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineData> m_Timelines = new Dictionary<string, TimelineData>(StringComparer.Ordinal);
        readonly Dictionary<UnityEngine.Object, string> m_AssetGuids = new Dictionary<UnityEngine.Object, string>();
        readonly HashSet<string> m_Routes = new HashSet<string>(StringComparer.Ordinal);

        public CharacterAuthoringDiscovery(CharacterSimulationCompileReport report)
        {
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_NodeEmitters = CharacterSimulationNodeEmitterRegistry.CreateDefault();
            m_TimelineEmitters = TimelineSemanticEmitterRegistry.CreateDefault();
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
                definitionPath,
                root,
                $"root:{root.GraphAuthoringId}");
            if (roots.Count == 0 || !m_Report.IsValid)
                return null;
            IReadOnlyList<CharacterSkillCompilationRecord> skills = CharacterSkillCompilationDiscovery.Discover(
                definition.SkillDefinitions,
                definition.SkillGraphs,
                m_TimelineEmitters,
                m_Report);
            if (!m_Report.IsValid)
                return null;
            foreach (CharacterSkillCompilationRecord skill in skills)
                foreach (BtsmtlSkillGraphOccurrence graph in skill.EntryGraph.EnumerateOccurrences())
                    foreach (BtsmtlSkillBlackboardDeclaration declaration in ((IBtsmtlSkillFlowGraph)graph.Graph).BlackboardDeclarations)
                    {
                        var record = new CharacterAuthoringBlackboardDeclaration(graph.Graph, declaration, graph.Route, graph.ContentHash);
                        string identity = DeclarationIdentity(graph.GraphId, declaration.VariableId);
                        if (m_Declarations.TryGetValue(identity, out CharacterAuthoringBlackboardDeclaration existing))
                        {
                            if (!ReferenceEquals(existing.AuthoringDeclaration, record.AuthoringDeclaration))
                                m_Report.DiscoveryError("blackboard_declaration_duplicate", graph.Route, $"声明'{identity}'指向不同对象。");
                        }
                        else
                            m_Declarations.Add(identity, record);
                    }
            foreach (CharacterSkillCompilationRecord skill in skills)
                foreach (BtsmtlSkillGraphOccurrence graph in skill.EntryGraph.EnumerateOccurrences())
                    foreach (BtsmtlSkillTimelineOccurrence record in graph.Timelines)
                    {
                        TimelineData timeline = record.Content.Timeline;
                        RegisterIdentity(timeline.AuthoringId, timeline, "Timeline", record.Content.Route);
                        ValidateAssetIdentity(record.Node.TimelineAsset, "Skill Timeline", record.Content.Route);
                        if (m_Timelines.TryGetValue(timeline.AuthoringId, out TimelineData existing) && !ReferenceEquals(existing, timeline))
                            m_Report.DiscoveryError("timeline_identity_duplicate", record.Content.Route, "不同Timeline内容使用了相同身份。");
                        else
                            m_Timelines[timeline.AuthoringId] = timeline;
                        foreach (TimelineSemanticTrackRecord track in record.Content.Tracks)
                        {
                            RegisterIdentity(track.Track.AuthoringId, track.Track, "TimelineTrack", track.Route);
                            foreach (TimelineSemanticClipRecord clip in track.Clips)
                                RegisterIdentity(clip.Clip.AuthoringId, clip.Clip, "TimelineClip", clip.Route);
                        }
                    }
            foreach (CharacterSkillCompilationRecord skill in skills)
                ValidateSkillActionWindowQueries(skill.EntryGraph);
            if (!m_Report.IsValid)
                return null;
            return new CharacterAuthoringCompilationModel(
                definition,
                definitionPath,
                definitionGuid,
                new ProgramId($"character:{definitionGuid}"),
                sourceRevision,
                roots,
                skills,
                m_Declarations,
                m_Timelines,
                m_AssetGuids,
                m_NodeEmitters,
                m_TimelineEmitters);
        }

        void DiscoverCompositionRoot(
            List<CharacterCompositionRoot> roots,
            CharacterCompositionRootRole role,
            string ownerIdentity,
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
            string identity = $"{(byte)role}:{ownerIdentity}:{graph.GraphAuthoringId}";
            if (roots.Any(value => string.Equals(value.Identity, identity, StringComparison.Ordinal)))
            {
                m_Report.DiscoveryError("composition_root_duplicate", route, $"Composition root '{identity}' is duplicated.");
                return;
            }
            roots.Add(new CharacterCompositionRoot(role, ownerIdentity, sourcePath, occurrence));
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
                                CharacterBlackboardDeclarationSnapshot declaration = declarationRecord.Declaration;
                                if (declaration.FactProjection?.Kind != PipelineBlackboardFactProjectionKind.ActionWindow ||
                                    !string.Equals(declaration.FactProjection.ActionWindowType, query.WindowType, StringComparison.Ordinal))
                                    continue;

                                candidates.Add($"owner={declarationRecord.GraphId},phase={clip.ExecutionPhase},windowId={declaration.FactProjection.ActionWindowId},clip={clip.AuthoringId}");
                                if (clip.ExecutionPhase == TimelineTreeExecutionPhase.Decision &&
                                    visibleOwnerIds.Contains(declarationRecord.GraphId))
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

        void ValidateSkillActionWindowQueries(BtsmtlSkillGraphOccurrence root)
        {
            var paths = new Dictionary<BtsmtlSkillGraphOccurrence, BtsmtlSkillGraphOccurrence[]>();
            var phases = new Dictionary<BtsmtlSkillGraphOccurrence, TreeClip>();
            Collect(root, Array.Empty<BtsmtlSkillGraphOccurrence>(), null);
            var projections = new List<(string WindowType, BtsmtlSkillGraphOccurrence Owner, TreeClip Clip, string Route)>();
            foreach (var pair in phases)
                foreach (IBtsmtlSkillBlackboardAccessNode setter in pair.Key.Nodes.OfType<IBtsmtlSkillBlackboardAccessNode>())
                {
                    if (!setter.Writes || !setter.Variable.IsValid)
                        continue;
                    BtsmtlSkillGraphOccurrence owner = paths[pair.Key].LastOrDefault(graph => graph.GraphId == setter.Variable.OwnerId);
                    if (owner == null)
                        continue;
                    BtsmtlSkillBlackboardDeclaration declaration = ((IBtsmtlSkillFlowGraph)owner.Graph).BlackboardDeclarations
                        .SingleOrDefault(value => value.VariableId == setter.Variable.DeclarationId);
                    if (declaration?.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow)
                        projections.Add((declaration.FactProjection.ActionWindowType, owner, pair.Value, pair.Key.Route));
                }
            foreach (var pair in paths)
                foreach (BtsmtlSkillActionWindowActiveFlowNode query in pair.Key.Nodes.OfType<BtsmtlSkillActionWindowActiveFlowNode>())
                {
                    string source = $"{pair.Key.Route}/node:{query.UID}";
                    if (string.IsNullOrWhiteSpace(query.WindowType))
                    {
                        m_Report.DiscoveryError("action_window_type_missing", source, "动作窗口查询必须指定窗口类型。");
                        continue;
                    }
                    var candidates = projections.Where(value => value.WindowType == query.WindowType).ToArray();
                    if (candidates.Any(value => value.Clip.ExecutionPhase == TimelineTreeExecutionPhase.Decision && pair.Value.Contains(value.Owner)))
                        continue;
                    string available = candidates.Length == 0 ? "无" : string.Join(";", candidates.Select(value =>
                        $"owner={value.Owner.Route},phase={value.Clip.ExecutionPhase},clip={value.Clip.AuthoringId},source={value.Route}"));
                    m_Report.DiscoveryError("action_window_phase_unavailable", source,
                        $"窗口'{query.WindowType}'没有当前调用可见的Decision TreeClip投射。候选={available}。");
                }

            void Collect(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillGraphOccurrence[] ancestors, TreeClip phase)
            {
                BtsmtlSkillGraphOccurrence[] path = ancestors.Append(graph).ToArray();
                paths.Add(graph, path);
                if (phase != null)
                    phases.Add(graph, phase);
                foreach (BtsmtlSkillGraphReferenceOccurrence reference in graph.References)
                    Collect(reference.Child, path, phase);
                foreach (BtsmtlSkillEdgeOccurrence edge in graph.Edges)
                    if (edge.Condition != null)
                        Collect(edge.Condition, path, phase);
                foreach (BtsmtlSkillTimelineOccurrence timeline in graph.Timelines)
                    foreach (TimelineSemanticTrackRecord track in timeline.Content.Tracks)
                        foreach (TimelineSemanticClipRecord clip in track.Clips)
                            if (clip.Clip is TreeClip tree)
                                Collect(timeline.Trees[tree.AuthoringId], path, tree);
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
                CharacterAuthoringGraphSignature signature = new CharacterAuthoringGraphSignature(declarations);
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
                        {
                            CharacterAuthoringGraphCallFrame callFrame = CharacterAuthoringGraphCallFrameFactory.Create(
                                node,
                                reference,
                                child,
                                referenceRoute,
                                m_Report);
                            graphReferences.Add(new CharacterAuthoringGraphReferenceRecord(
                                node,
                                reference,
                                referenceRoute,
                                child,
                                callFrame));
                        }
                    }
                }
                string entryNodeId = ResolveEntryNodeId(graph, nodes, route);
                return new CharacterAuthoringGraphOccurrence(
                    graph,
                    route,
                    declarations,
                    nodes,
                    edges,
                    propertyEdges,
                    graphReferences,
                    timelines,
                    entryNodeId,
                    signature);
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
            string timelineRoute = $"{nodeRoute}/timeline:{timeline.AuthoringId}";
            TimelineSemanticContentDiscoveryResult discovered = TimelineSemanticContentDiscovery.Discover(
                timeline,
                timelineRoute,
                m_TimelineEmitters,
                m_Report);
            if (!discovered.IsValid)
                return null;
            var treeGraphs = new Dictionary<string, CharacterAuthoringGraphOccurrence>(StringComparer.Ordinal);
            int treeDiscoveryMessageCount = m_Report.Messages.Count;
            foreach (TimelineSemanticTreeRecord treeRecord in discovered.Content.TreeRecords.Values.OrderBy(value => value.Route, StringComparer.Ordinal))
            {
                TreeClip treeClip = treeRecord.Clip;
                if (treeClip.Ownership == TimelineTreeOwnership.Inline && !ReferenceEquals(treeRecord.Tree.SerializedOwner, timeline.SerializedOwner))
                    m_Report.DiscoveryError("tree_clip_inline_owner_mismatch", treeRecord.Route, "Inline TreeClip graph serialized owner does not match its Timeline owner.");
                if (treeClip.Ownership == TimelineTreeOwnership.Shared && !ReferenceEquals(treeRecord.Tree.SerializedOwner, treeClip.SharedTreeAsset))
                    m_Report.DiscoveryError("tree_clip_shared_owner_mismatch", treeRecord.Route, "Shared TreeClip graph serialized owner does not match its Tree asset.");
                CharacterAuthoringGraphOccurrence tree = DiscoverGraph(treeRecord.Tree, treeRecord.Route, stack);
                if (tree == null)
                    m_Report.DiscoveryError("tree_clip_graph_compile_failed", treeRecord.Route, "TreeClip did not produce a Character graph discovery record.");
                else
                    treeGraphs[treeClip.AuthoringId] = tree;
            }
            if (treeGraphs.Count != discovered.Content.TreeRecords.Count || m_Report.Messages.Count != treeDiscoveryMessageCount)
                return null;
            RegisterIdentity(timeline.AuthoringId, timeline, "Timeline", timelineRoute);
            if (m_Timelines.TryGetValue(timeline.AuthoringId, out TimelineData existing) && !ReferenceEquals(existing, timeline))
                m_Report.DiscoveryError("timeline_identity_duplicate", nodeRoute, $"Timeline identity '{timeline.AuthoringId}' belongs to multiple authoring objects.");
            else
                m_Timelines[timeline.AuthoringId] = timeline;
            for (int trackIndex = 0; trackIndex < discovered.Content.Tracks.Count; trackIndex++)
            {
                TimelineSemanticTrackRecord trackRecord = discovered.Content.Tracks[trackIndex];
                Track track = trackRecord.Track;
                string trackRoute = trackRecord.Route;
                RegisterIdentity(track.AuthoringId, track, "TimelineTrack", trackRoute);
                for (int clipIndex = 0; clipIndex < trackRecord.Clips.Count; clipIndex++)
                {
                    TimelineSemanticClipRecord clipRecord = trackRecord.Clips[clipIndex];
                    Clip clip = clipRecord.Clip;
                    string clipRoute = clipRecord.Route;
                    RegisterIdentity(clip.AuthoringId, clip, "TimelineClip", clipRoute);
                }
            }
            if (node.PlaybackMode == TimelinePlaybackMode.Loop && discovered.Content.MaxFrame <= 0)
                m_Report.DiscoveryError("timeline_loop_duration_invalid", timelineRoute, "Loop Timeline duration must be greater than zero.");
            return new CharacterAuthoringTimelineRecord(
                node,
                discovered.Content,
                graphRoute,
                timelineRoute,
                treeGraphs);
        }

        void DiscoverDeclaration(BaseTree graph, BaseExposedProperty declaration, string route)
        {
            string source = $"{route}/blackboard:{declaration.DeclarationId}";
            RegisterIdentity(declaration.DeclarationId, declaration, "BlackboardDeclaration", source);
            string key = DeclarationIdentity(graph.GraphAuthoringId, declaration.DeclarationId);
            if (m_Declarations.TryGetValue(key, out CharacterAuthoringBlackboardDeclaration existing))
            {
                if (!ReferenceEquals(existing.AuthoringDeclaration, declaration))
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
