using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCamera;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Authoring;
using UnityEditor;
using UnityEngine;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed class BtsmtlSkillAuthoringCodeAdapter : IBtsmtlAuthoringCodeDomainAdapter
    {
        public string DomainId => "btsmtl-skill";

        public bool CanHandle(object root) =>
            root is GameplayAbilityDefinition ||
            root is GameplayAbilityAdmissionProfile ||
            root is BtsmtlSkillFlowGraph ||
            root is TimelineAsset;

        public void Emit(BtsmtlAuthoringCodeExportContext context, object root)
        {
            AddUsings(context);
            switch (root)
            {
                case GameplayAbilityDefinition ability:
                    if (ability.AbilityGraph == null)
                    {
                        context.ReportError("ability_graph_missing", ability.AbilityId, "Gameplay Ability缺少私有AbilityGraph。");
                        break;
                    }
                    EmitFlowGraph(context, ability.AbilityGraph, true, ability.AbilityId);
                    break;
                case GameplayAbilityAdmissionProfile admissionProfile:
                    EmitAdmissionProfile(context, admissionProfile);
                    break;
                case BtsmtlSkillFlowGraph graph:
                    context.ReportError("skill_root_retired", graph.AuthoringId, "独立Skill根已退役；必须从GameplayAbilityDefinition导出Ability。");
                    break;
                case TimelineAsset timeline:
                    EmitTimelineRoot(context, timeline);
                    break;
                default:
                    context.ReportError("skill_root_unsupported", root?.GetType().FullName, "Skill领域不支持该导出根对象。");
                    break;
            }
        }

        static void AddUsings(BtsmtlAuthoringCodeExportContext context)
        {
            context.AddUsing("BTSMTL.Timeline");
            context.AddUsing("FlowCanvas");
            context.AddUsing("FlowCanvas.Macros");
            context.AddUsing("FlowCanvas.Nodes");
            context.AddUsing("NodeCanvas.Framework");
            context.AddUsing("ThirdPersonCamera");
            context.AddUsing("ThirdPersonCharacter.ActionSystem");
            context.AddUsing("ThirdPersonCharacter.Control.Authoring");
            context.AddUsing("ThirdPersonCharacter.Pipeline.Motion.RootMotion");
            context.AddUsing("ThirdPersonGameplay.Attributes");
            context.AddUsing("ThirdPersonGameplay.Effects");
            context.AddUsing("ThirdPersonGameplay.Tags");
            context.AddUsing("ThirdPersonSimulation");
            context.AddUsing("TreeDesigner");
            context.AddUsing("UnityEngine");
        }

        static void EmitAdmissionProfile(
            BtsmtlAuthoringCodeExportContext context,
            GameplayAbilityAdmissionProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.ActionId))
            {
                context.ReportError("admission_profile_identity_missing", profile.name, "GameplayAbilityAdmissionProfile缺少ActionId。");
                return;
            }
            string variable = context.RegisterObject(profile, profile.ActionId, "admissionProfile", true);
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Create,
                $"var {variable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureAdmissionProfileRoot(context, {String(profile.ActionId)}, {String(profile.DisplayName)}, {String(profile.DebugCategory)}, {GameplayTagArray(profile.Tags)}, {GameplayTagQuery(profile.RequiredTags)}, {GameplayTagQuery(profile.BlockTags)}, {GameplayTagQuery(profile.CancelTags)}, {EnumValue(typeof(ActionTargetRequirement), profile.TargetRequirement)}, {profile.MaxConcurrentInstances}, {String(AssetDatabase.GetAssetPath(profile))});");
        }

        void EmitFlowGraph(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlSkillFlowGraph root,
            bool abilityRoot,
            string abilityId)
        {
            BtsmtlSkillAuthoringClosure closure;
            try
            {
                closure = BtsmtlSkillAuthoringClosure.Create(root, true);
            }
            catch (Exception error)
            {
                context.ReportError("skill_graph_closure_invalid", root.AuthoringId, error.Message);
                return;
            }

            IReadOnlyList<FlowGraph> graphs = closure.Graphs;
            IReadOnlyList<BtsmtlSkillNativeStateMachine> machines = closure.Machines;
            IReadOnlyDictionary<FlowGraph, FlowGraph> graphOwners = closure.GraphOwners;
            IReadOnlyDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> machineOwners = closure.MachineOwners;
            IReadOnlyList<TimelineAsset> timelines = closure.Timelines;
            IReadOnlyDictionary<TimelineAsset, FlowGraph> timelineOwners = closure.TimelineOwners;
            IReadOnlyList<TimelineAsset> privateTimelines = timelines
                .Where(value => BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(value, root))
                .ToList();
            RegisterGraphs(context, graphs, root, graphOwners, closure.GraphPlacementOwners);
            RegisterMachines(context, machines, machineOwners, root, graphOwners, closure.GraphPlacementOwners);
            RegisterTimelines(context, timelines, timelineOwners, root, graphOwners, closure.GraphPlacementOwners);

            string resolvedAbilityId = abilityRoot
                ? string.IsNullOrWhiteSpace(abilityId)
                    ? ResolveAbilityId(context, root)
                    : abilityId
                : string.Empty;
            EmitGraphCreation(context, graphs, root, graphOwners, abilityRoot, resolvedAbilityId);
            if (abilityRoot)
                EmitAbilityConfiguration(context, root, resolvedAbilityId, graphs);
            EmitGraphNodeCreation(context, graphs);
            EmitMachineCreation(context, machines, machineOwners);
            EmitTimelineCreation(context, timelines, timelineOwners, root);
            EmitMachineNodeCreation(context, machines);
            EmitTimelineContentCreation(context, privateTimelines);
            EmitGraphConfiguration(context, graphs);
            EmitMachineConfiguration(context, machines);
            EmitTimelineConfiguration(context, privateTimelines);
            EmitGraphConnections(context, graphs);
            EmitMachineConnections(context, machines);
            EmitPrune(context, graphs, machines, privateTimelines);
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.BindAbilityRoot(context, {Variable(context, root, root.AuthoringId)});");
        }

        string ResolveAbilityId(BtsmtlAuthoringCodeExportContext context, BtsmtlSkillFlowGraph root)
        {
            if (context.Root is GameplayAbilityDefinition ability && !string.IsNullOrWhiteSpace(ability.AbilityId))
                return ability.AbilityId;
            throw new InvalidOperationException($"Ability graph '{root.AuthoringId}' has no GameplayAbilityDefinition root.");
        }

        void EmitAbilityConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlSkillFlowGraph root,
            string abilityId,
            IReadOnlyList<FlowGraph> graphs)
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(context.DefinitionAssetPath);
            GameplayAbilityDefinition ability = context.Root as GameplayAbilityDefinition;
            if (!ability || !definition)
            {
                context.ReportError("ability_root_missing", abilityId, "完整Ability导出必须以GameplayAbilityDefinition为根。");
                return;
            }
            AbilityGrant grant = definition.AbilityGrants
                .SingleOrDefault(value => value != null && value.Ability == ability);
            GameplayAbilityAdmissionProfile admissionProfile = ability.AdmissionProfile;
            if (!admissionProfile)
            {
                context.ReportError("ability_admission_profile_missing", abilityId, "Gameplay Ability缺少准入规则来源。");
                return;
            }
            IReadOnlyList<GameplayEffectDefinition> effects = ability.Effects;
            IReadOnlyList<GameplayAbilityEndRule> endRules = ability.EndRules;
            IReadOnlyList<string> followUps = ability.AllowedFollowUpAbilityIds;
            var ruleVariables = new List<string>();
            for (int i = 0; i < endRules.Count; i++)
            {
                GameplayAbilityEndRule rule = endRules[i];
                if (rule == null)
                {
                    context.ReportError("ability_end_rule_missing", abilityId, $"Gameplay Ability结束规则#{i}为空。");
                    continue;
                }
                string variable = context.AllocateVariableName("endRule");
                ruleVariables.Add(variable);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"var {variable} = new {TypeName(typeof(GameplayAbilityEndRule))}();");
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{variable}.Configure({EnumValue(typeof(GameplayAbilityEndTrigger), rule.Trigger)}, {EnumValue(typeof(ActionLifecycleTransitionType), rule.TransitionType)}, {String(rule.ActionWindowType)}, {String(rule.Reason)});");
            }
            string ruleArray = ruleVariables.Count == 0
                ? $"Array.Empty<{TypeName(typeof(GameplayAbilityEndRule))}>()"
                : $"new[] {{ {string.Join(", ", ruleVariables)} }}";
            IReadOnlyList<GameplayAbilitySubgraphDependencyConfiguration> dependencies =
                ability.SubgraphDependencies ??
                Array.Empty<GameplayAbilitySubgraphDependencyConfiguration>();
            var dependencyVariables = new List<string>();
            for (int i = 0; i < dependencies.Count; i++)
            {
                GameplayAbilitySubgraphDependencyConfiguration dependency = dependencies[i];
                if (dependency == null)
                {
                    context.ReportError("ability_dependency_missing", abilityId, $"Gameplay Ability子图依赖#{i}为空。");
                    continue;
                }
                string variable = context.AllocateVariableName("dependency");
                dependencyVariables.Add(variable);
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"var {variable} = new {TypeName(typeof(GameplayAbilitySubgraphDependencyConfiguration))}({String(dependency.SubgraphIdentity)}, {String(dependency.CallSiteIdentity)});");
            }
            string dependencyArray = dependencyVariables.Count == 0
                ? $"Array.Empty<{TypeName(typeof(GameplayAbilitySubgraphDependencyConfiguration))}>()"
                : $"new[] {{ {string.Join(", ", dependencyVariables)} }}";
            string effectArray = AssetArray(context, effects, typeof(GameplayEffectDefinition));
            string debugCategory = ability.DebugCategory;
            string tags = GameplayTagArray(ability.Tags);
            string followUpArray = StringArray(followUps);
            string sourceInput = grant?.SourceInputRequestId ?? string.Empty;
            bool consume = grant?.ConsumeSourceInputRequest ?? true;
            string targetInput = grant?.TargetInputValueId ?? string.Empty;
            string targetKey = grant?.TargetKey ?? string.Empty;
            bool createGrant = false;
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Configure,
                $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.ConfigureAbility(context, {String(debugCategory)}, {tags}, {ExternalAsset(context, admissionProfile, typeof(GameplayAbilityAdmissionProfile))}, {effectArray}, {ruleArray}, {dependencyArray}, {followUpArray}, {String(sourceInput)}, {Bool(consume)}, {String(targetInput)}, {String(targetKey)}, {Bool(createGrant)});");
        }

        void EmitTimelineRoot(BtsmtlAuthoringCodeExportContext context, TimelineAsset root)
        {
            BtsmtlSkillAuthoringClosure closure;
            try
            {
                closure = BtsmtlSkillAuthoringClosure.Create(root, true);
            }
            catch (Exception error)
            {
                context.ReportError("timeline_graph_closure_invalid", root.name, error.Message);
                return;
            }
            IReadOnlyList<FlowGraph> graphs = closure.Graphs;
            TimelineAsset[] nestedTimelines = closure.Timelines.Where(value => value != root).ToArray();
            TimelineAsset[] ownedTimelines = closure.Timelines.Where(value =>
                value == root || BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(value, root)).ToArray();
            RegisterTimeline(context, root, true);
            RegisterGraphs(context, graphs, root, closure.GraphOwners, closure.GraphPlacementOwners);
            RegisterMachines(context, closure.Machines, closure.MachineOwners, root,
                closure.GraphOwners, closure.GraphPlacementOwners);
            RegisterTimelines(context, nestedTimelines, closure.TimelineOwners, root,
                closure.GraphOwners, closure.GraphPlacementOwners);
            EmitTimelineCreation(context, new[] { root }, closure.TimelineOwners, root);
            EmitGraphCreation(context, graphs, root, closure.GraphOwners, false, string.Empty);
            EmitGraphNodeCreation(context, graphs);
            EmitMachineCreation(context, closure.Machines, closure.MachineOwners);
            EmitTimelineCreation(context, nestedTimelines, closure.TimelineOwners, root);
            EmitMachineNodeCreation(context, closure.Machines);
            EmitTimelineContentCreation(context, ownedTimelines);
            EmitGraphConfiguration(context, graphs);
            EmitMachineConfiguration(context, closure.Machines);
            EmitTimelineConfiguration(context, ownedTimelines);
            EmitGraphConnections(context, graphs);
            EmitMachineConnections(context, closure.Machines);
            EmitPrune(context, graphs, closure.Machines, ownedTimelines);
        }

        void RegisterGraphs(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs,
            UnityEngine.Object root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> owners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> placementOwners)
        {
            foreach (FlowGraph graph in graphs)
            {
                if (graph is not IBtsmtlSkillAuthoringGraph authoring)
                {
                    context.ReportError("skill_graph_type_invalid", graph?.GetType().FullName, "Skill闭包包含非正式技能图。");
                    continue;
                }
                RegisterObject(
                    context,
                    graph,
                    $"graph:{authoring.AuthoringId}",
                    "graph",
                    ReferenceEquals(graph, root),
                    SectionForGraph(graph, root, owners, placementOwners));
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                {
                    RegisterNode(context, graph, node, SectionForGraph(graph, root, owners, placementOwners));
                }
            }
        }

        void RegisterMachines(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines,
            IReadOnlyDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> owners,
            UnityEngine.Object root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> graphOwners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> placementOwners)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
            {
                string sectionName = owners.TryGetValue(machine, out FlowGraph owner)
                    ? SectionForGraph(owner, root, graphOwners, placementOwners)
                    : "Root";
                RegisterObject(context, machine, $"fsm:{machine.AuthoringId}", "stateMachine", false, sectionName);
                RegisterMachineMembers(context, machine, sectionName);
            }
        }

        void RegisterMachineMembers(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlSkillNativeStateMachine machine,
            string sectionName)
        {
            foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
            {
                RegisterObject(
                    context,
                    state,
                    $"state:{machine.AuthoringId}:{state.UID}",
                    "state",
                    false,
                    sectionName,
                    TypeName(typeof(BtsmtlSkillNativeState)));
                foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    RegisterObject(
                        context,
                        connection,
                        $"state-edge:{machine.AuthoringId}:{connection.UID}",
                        "stateEdge",
                        false,
                        sectionName,
                        TypeName(typeof(BtsmtlSkillNativeConnection)));
            }
        }

        void RegisterTimelines(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<TimelineAsset> timelines,
            IReadOnlyDictionary<TimelineAsset, FlowGraph> owners,
            UnityEngine.Object root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> graphOwners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> placementOwners)
        {
            foreach (TimelineAsset timeline in timelines)
            {
                string section = owners.TryGetValue(timeline, out FlowGraph owner) && owner != null
                    ? SectionForGraph(owner, root, graphOwners, placementOwners)
                    : TimelineSection(timeline);
                RegisterTimeline(context, timeline, false, section);
            }
        }

        void RegisterTimeline(
            BtsmtlAuthoringCodeExportContext context,
            TimelineAsset timeline,
            bool isRoot,
            string sectionName = null)
        {
            if (timeline?.Data == null)
            {
                context.ReportError("timeline_data_missing", timeline?.name, "TimelineAsset缺少TimelineData。");
                return;
            }
            string timelineIdentity = timeline.Data.AuthoringId;
            string assetPath = AssetDatabase.GetAssetPath(timeline);
            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(timeline, out _, out long localFileId))
            {
                context.ReportError("timeline_asset_not_persistent", timeline.name, "Timeline资产必须有稳定的本地文件身份。");
                return;
            }
            string timelineSection = isRoot ? "Root" : sectionName ?? TimelineSection(timeline);
            RegisterObject(
                context,
                timeline,
                $"timeline-asset:{assetPath}:{localFileId}",
                "timeline",
                isRoot,
                timelineSection,
                TypeName(typeof(TimelineAsset)));
            RegisterObject(
                context,
                timeline.Data,
                $"timeline:{timelineIdentity}",
                "timelineData",
                false,
                timelineSection);
            foreach (Track track in timeline.Data.Tracks)
            {
                RegisterObject(
                    context,
                    track,
                    $"track:{timelineIdentity}:{track.AuthoringId}",
                    "track",
                    false,
                    timelineSection,
                    TypeName(typeof(Track)));
                foreach (Clip clip in track.Clips)
                    RegisterObject(
                        context,
                        clip,
                        $"clip:{timelineIdentity}:{clip.AuthoringId}",
                        "clip",
                        false,
                        timelineSection,
                        TypeName(typeof(Clip)));
            }
            foreach (TimelineSection section in timeline.Data.Sections)
                RegisterObject(
                    context,
                    section,
                    $"section:{timelineIdentity}:{section.AuthoringId}",
                    "section",
                    false,
                    timelineSection);
            foreach (TimelineExternalBindingDeclaration binding in timeline.Data.ExternalBindings)
                RegisterObject(
                    context,
                    binding,
                    $"binding:{timelineIdentity}:{binding.AuthoringId}",
                    "binding",
                    false,
                    timelineSection);
        }

        void RegisterNode(
            BtsmtlAuthoringCodeExportContext context,
            FlowGraph graph,
            FlowNode node,
            string sectionName)
        {
            string graphIdentity = (graph as IBtsmtlSkillAuthoringGraph)?.AuthoringId ?? graph.GetType().FullName;
            RegisterObject(
                context,
                node,
                $"node:{graphIdentity}:{node.UID}",
                "node",
                false,
                sectionName,
                TypeName(typeof(FlowNode)));
            foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>())
                RegisterObject(
                    context,
                    connection,
                    $"edge:{graphIdentity}:{connection.UID}",
                    "edge",
                    false,
                    sectionName,
                    TypeName(typeof(BinderConnection)));
        }

        void EmitGraphCreation(
            BtsmtlAuthoringCodeExportContext context,
            IReadOnlyList<FlowGraph> graphs,
            UnityEngine.Object root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> owners,
            bool abilityRoot,
            string abilityId)
        {
            foreach (FlowGraph graph in graphs)
            {
                string variable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                IBtsmtlSkillAuthoringGraph authoring = (IBtsmtlSkillAuthoringGraph)graph;
                string expression;
                bool canRunWithSectionDependencies = false;
                if (ReferenceEquals(graph, root) && graph is BtsmtlSkillFlowGraph skillRoot &&
                    skillRoot.Role == BtsmtlSkillFlowGraphRole.Skill)
                {
                    if (!abilityRoot)
                        context.ReportError("skill_root_retired", skillRoot.AuthoringId, "独立Skill根已退役；必须从GameplayAbilityDefinition导出Ability。");
                    expression = abilityRoot
                        ? $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureAbilityRoot(context, {String(abilityId)}, {String(skillRoot.AuthoringId)}, {String(skillRoot.name)})"
                        : ExternalAsset(context, graph, graph.GetType());
                }
                else if (BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(graph, root) && owners.TryGetValue(graph, out FlowGraph owner))
                {
                    string ownerVariable = Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}");
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringGraphCreationContract))}.EnsureOwnedGraph<{TypeName(graph.GetType())}>({ownerVariable}, {String(authoring.AuthoringId)}, typeof({TypeName(graph.GetType())}), {EnumValue(typeof(BtsmtlSkillFlowGraphRole), authoring.Role)}, {String(graph.name)})";
                    canRunWithSectionDependencies = true;
                }
                else if (root is TimelineAsset timelineRoot && BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(graph, root))
                {
                    string ownerVariable = Variable(context, timelineRoot, $"timeline:{timelineRoot.Data.AuthoringId}");
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTimelineGraph({ownerVariable}, {String(authoring.AuthoringId)}, {String(graph.name)}, {EnumValue(typeof(BtsmtlSkillFlowGraphRole), authoring.Role)})";
                    canRunWithSectionDependencies = true;
                }
                else
                {
                    if (ReferenceEquals(graph, root) && graph is not BtsmtlSkillMacroGraph)
                        context.ReportError("skill_root_role_unsupported", authoring.AuthoringId, "只有Skill根图可以作为Definition生成根。");
                    expression = ExternalAsset(context, graph, graph.GetType());
                }
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = {expression};",
                    canRunWithSectionDependencies: canRunWithSectionDependencies);
            }
        }

        void EmitMachineCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines,
            IReadOnlyDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> owners)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
            {
                string variable = Variable(context, machine, $"fsm:{machine.AuthoringId}");
                string expression;
                bool canRunWithSectionDependencies = false;
                if (owners.TryGetValue(machine, out FlowGraph owner) && BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(machine, owner))
                {
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureStateMachine({Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}")}, {String(machine.AuthoringId)}, {String(machine.name)}, {String(machine.OwnerGraphId)}, {String(machine.OwnerNodeId)})";
                    canRunWithSectionDependencies = true;
                }
                else
                    expression = ExternalAsset(context, machine, machine.GetType());
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = {expression};",
                    canRunWithSectionDependencies: canRunWithSectionDependencies);
            }
        }

        void EmitTimelineCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<TimelineAsset> timelines,
            IReadOnlyDictionary<TimelineAsset, FlowGraph> owners,
            UnityEngine.Object root)
        {
            foreach (TimelineAsset timeline in timelines)
            {
                string variable = Variable(context, timeline, $"timeline-asset:{AssetDatabase.GetAssetPath(timeline)}");
                string expression;
                bool canRunWithSectionDependencies = false;
                if (ReferenceEquals(timeline, root))
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTimelineRoot(context, {String(timeline.Data.AuthoringId)}, {String(timeline.name)})";
                else if (owners.TryGetValue(timeline, out FlowGraph owner) && BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(timeline, root) && owner != null)
                {
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTimeline({Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}")}, {String(timeline.Data.AuthoringId)}, {String(timeline.name)})";
                    canRunWithSectionDependencies = true;
                }
                else
                    expression = ExternalAsset(context, timeline, typeof(TimelineAsset));
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {variable} = {expression};",
                    canRunWithSectionDependencies: canRunWithSectionDependencies);
            }
        }

        void EmitGraphNodeCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs)
        {
            foreach (FlowGraph graph in graphs)
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    string graphVariable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                    string nodeVariable = Variable(context, node, $"node:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}:{node.UID}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        $"var {nodeVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureFlowNode({graphVariable}, typeof({TypeName(node.GetType())}), {String(node.UID)}, {String(node.name)}, {BtsmtlAuthoringCodeValues.Vector2(node.position)});");
                }
        }

        void EmitMachineNodeCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    string machineVariable = Variable(context, machine, $"fsm:{machine.AuthoringId}");
                    string stateVariable = Variable(context, state, $"state:{machine.AuthoringId}:{state.UID}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        $"var {stateVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureNativeState({machineVariable}, typeof({TypeName(state.GetType())}), {String(state.UID)}, {String(state.name)}, {BtsmtlAuthoringCodeValues.Vector2(state.position)});");
                }
        }

        void EmitTimelineContentCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<TimelineAsset> timelines)
        {
            foreach (TimelineAsset timeline in timelines)
            {
                if (timeline?.Data == null)
                    continue;
                string timelineIdentity = timeline.Data.AuthoringId;
                string timelineVariable = Variable(context, timeline, $"timeline-asset:{AssetDatabase.GetAssetPath(timeline)}");
                string dataVariable = Variable(context, timeline.Data, $"timeline:{timelineIdentity}");
                string catalogVariable = context.AllocateVariableName("timelineCatalog");
                context.RegisterLocalVariable(
                    catalogVariable,
                    TypeName(typeof(TimelineContractCatalog)),
                    context.VariableSections[dataVariable]);
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Create, $"var {dataVariable} = {timelineVariable}.Data;");
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {catalogVariable} = {TypeName(typeof(TimelineTreeContractComposition))}.Create();");
                foreach (Track track in timeline.Data.Tracks)
                {
                    string trackVariable = Variable(context, track, $"track:{timelineIdentity}:{track.AuthoringId}");
                    TimelineExecutionDomain executionDomain = TimelineAuthoringTrackBinding.Export(track, TimelineTreeContractComposition.Create()).ExecutionDomain;
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        $"var {trackVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTrack({dataVariable}, {catalogVariable}, typeof({TypeName(track.GetType())}), {String(track.AuthoringId)}, {String(track.Name)}, {EnumValue(typeof(TimelineExecutionDomain), executionDomain)});");
                    foreach (Clip clip in track.Clips)
                    {
                        string clipVariable = Variable(context, clip, $"clip:{timelineIdentity}:{clip.AuthoringId}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        NeedsClipSegmentOverride(clip, TimelineAuthoringPropertyContract.ReferenceAsset(clip))
                            ? $"var {clipVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureClip({dataVariable}, {catalogVariable}, {trackVariable}, {String(clip.AuthoringId)}, {clip.StartFrame}, {TimelineReference(context, clip)}, {clip.EndFrame}, {clip.SelfEaseInFrame}, {clip.SelfEaseOutFrame}, {clip.ClipInFrame});"
                            : $"var {clipVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureClip({dataVariable}, {catalogVariable}, {trackVariable}, {String(clip.AuthoringId)}, {clip.StartFrame}, {TimelineReference(context, clip)});");
                    }
                }
            }
        }

        void EmitGraphConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs)
        {
            foreach (FlowGraph graph in graphs)
            {
                EmitBlackboardConfiguration(context, graph);
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    EmitNodeAuthoringFields(context, node);
                    EmitValueInputs(context, node);
                }
            }
        }

        void EmitBlackboardConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring)
                return;
            foreach (BtsmtlSkillBlackboardDeclaration declaration in authoring.BlackboardDeclarations)
            {
                Variable variable = graph.GetGraphSource().localBlackboard.variables.Values
                    .SingleOrDefault(value => value != null && value.ID == declaration.VariableId);
                if (variable is not ISerializedVariableValue serialized)
                {
                    context.ReportError(
                        "blackboard_value_missing",
                        $"{authoring.AuthoringId}:{declaration.VariableId}",
                        "Skill Blackboard声明缺少正式序列化值。");
                    continue;
                }
                string graphVariable = Variable(context, graph, $"graph:{authoring.AuthoringId}");
                string valueExpression = BtsmtlAuthoringCodeValues.Value(
                    context,
                    serialized.serializedValue,
                    variable.varType,
                    $"{authoring.AuthoringId}:{declaration.VariableId}");
                string inputBinding = declaration.InputBinding == null
                    ? "null"
                    : $"new {TypeName(typeof(PipelineBlackboardInputBinding))}({String(declaration.InputBinding.InputValueId)})";
                string factProjection = declaration.FactProjection == null
                    ? "null"
                    : $"new {TypeName(typeof(PipelineBlackboardFactProjection))}({EnumValue(typeof(PipelineBlackboardFactProjectionKind), declaration.FactProjection.Kind)}, {String(declaration.FactProjection.ActionWindowType)}, {String(declaration.FactProjection.ActionWindowId)}, {declaration.FactProjection.ActionWindowDigest}UL)";
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureBlackboardDeclaration({graphVariable}, {String(declaration.VariableId)}, {String(variable.name)}, typeof({TypeName(variable.varType)}), {valueExpression}, {EnumValue(typeof(PipelineBlackboardVariableScope), declaration.Scope)}, {EnumValue(typeof(PipelineBlackboardVariableLifetime), declaration.Lifetime)}, {String(declaration.Category)}, {inputBinding}, {factProjection});");
            }
        }

        void EmitNodeAuthoringFields(
            BtsmtlAuthoringCodeExportContext context,
            FlowNode node)
        {
            string nodeVariable = Variable(context, node, NodeKey(node));
            var values = new List<string>();
            foreach (GraphAuthoringFieldValue field in BtsmtlSkillAuthoringContract.ReadFields(node))
            {
                if (!field.IsValid)
                {
                    context.ReportError(
                        "skill_authoring_field_invalid",
                        $"{node.UID}:{field.Field.FieldId.Value}",
                        "Skill节点字段值不符合核心作者合同。");
                    continue;
                }
                if (BtsmtlSkillAuthoringContract.IsDefault(field))
                    continue;
                values.Add(
                    $"new {TypeName(typeof(BtsmtlSkillAuthoringFieldValue))}({String(field.Field.FieldId.Value)}, {FieldExpression(context, field.Value, $"{node.UID}:{field.Field.FieldId.Value}")})");
            }
            if (values.Count != 0)
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringContract))}.Apply({nodeVariable}, new[] {{ {string.Join(", ", values)} }});");
        }

        void EmitValueInputs(
            BtsmtlAuthoringCodeExportContext context,
            FlowNode node)
        {
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected || input.isDefaultValue)
                    continue;
                string value = BtsmtlAuthoringCodeValues.Value(context, input.serializedValue, input.type, $"{node.UID}:{input.ID}");
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.SetValue({Variable(context, node, NodeKey(node))}, {String(input.ID)}, {value});");
            }
        }

        void EmitMachineConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
                    if (state.Body != null)
                        context.AddStatement(
                            BtsmtlAuthoringCodeEmissionPhase.Bind,
                            $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.ConfigureNativeState(({TypeName(typeof(BtsmtlSkillNativeState))}){Variable(context, state, $"state:{machine.AuthoringId}:{state.UID}")}, {GraphExpression(context, state.Body)});");
        }

        void EmitTimelineConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<TimelineAsset> timelines)
        {
            foreach (TimelineAsset timeline in timelines)
            {
                if (timeline?.Data == null)
                    continue;
                TimelineData data = timeline.Data;
                string dataVariable = Variable(context, data, $"timeline:{data.AuthoringId}");
                if (!string.Equals(data.Name, timeline.name, StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{dataVariable}.Name = {String(data.Name)};");
                if (data.Loop)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{dataVariable}.Loop = true;");
                foreach (TimelineExternalBindingDeclaration binding in data.ExternalBindings)
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureExternalBinding({dataVariable}, {String(binding.AuthoringId)}, {String(binding.BindingId)}, {String(binding.DisplayName)}, {String(binding.Domain)}, {EnumValue(typeof(TimelineBindingValueKind), binding.ValueKind)}, {EnumValue(typeof(TimelineBindingAccess), binding.Access)}, {EnumValue(typeof(TimelineBindingLifetime), binding.Lifetime)}, {String(binding.ParameterId)});");
                foreach (TimelineSection section in data.Sections)
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureSection({dataVariable}, {String(section.AuthoringId)}, {String(section.Name)}, {section.Frame}, {String(section.NextSectionId)});");
                foreach (Track track in data.Tracks)
                {
                    string trackVariable = Variable(context, track, $"track:{data.AuthoringId}:{track.AuthoringId}");
                    foreach (TimelineMarker marker in track.Markers)
                    {
                        if (marker.Graph is not ITimelineTreeGraphAsset trigger || !trigger.IsTimelineTrigger)
                        {
                            context.ReportError("timeline_marker_graph_invalid", marker.AuthoringId, "Marker缺少正式TimelineTrigger图。");
                            continue;
                        }
                        if (!context.TryGetVariable(marker.Graph, out string graphReference))
                        {
                            if (BtsmtlSkillAuthoringClosure.IsPrivateSubAsset(marker.Graph, timeline))
                            {
                                context.ReportError("timeline_marker_graph_not_in_closure", marker.AuthoringId,
                                    "Marker私有图不在正式重建闭包中，不能引用旧子资产替代重建。");
                                continue;
                            }
                            graphReference = ExternalAsset(context, marker.Graph, marker.Graph.GetType());
                        }
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureMarker({dataVariable}, {trackVariable}, {String(marker.AuthoringId)}, {marker.Frame}, {graphReference});");
                    }
                    if (track.PersistentMuted)
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"{trackVariable}.PersistentMuted = true;");
                    TimelineAuthoringTrackExport trackAuthoring = TimelineAuthoringTrackBinding.Export(track, TimelineTreeContractComposition.Create());
                    if (!string.IsNullOrEmpty(trackAuthoring.AnimationChannelId))
                    {
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"(({TypeName(typeof(AnimationTrack))}){trackVariable}).SetAnimationChannelId(new {TypeName(typeof(AnimationChannelId))}({String(trackAuthoring.AnimationChannelId)}));");
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"(({TypeName(typeof(AnimationTrack))}){trackVariable}).SetAnimationSlotId({String(trackAuthoring.AnimationSlotId)});");
                    }
                    foreach (Clip clip in track.Clips)
                        EmitTimelineClipProperties(context, data, clip);
                }
            }
        }

        void EmitTimelineClipProperties(
            BtsmtlAuthoringCodeExportContext context,
            TimelineData data,
            Clip clip)
        {
            string clipVariable = Variable(context, clip, $"clip:{data.AuthoringId}:{clip.AuthoringId}");
            IReadOnlyList<TimelineAuthoringPropertyValue> properties =
                TimelineAuthoringPropertyContract.Read(clip);
            if (properties.Count != 0)
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(TimelineAuthoringPropertyContract))}.Apply({Variable(context, data, $"timeline:{data.AuthoringId}")}, {clipVariable}, new[] {{ {string.Join(", ", properties.Select(value => TimelinePropertyValue(context, value, clip.AuthoringId)))} }});");
            foreach (TimelineCurveChannelDescriptor descriptor in TimelineCurveChannelCatalog.All)
            {
                if (!descriptor.Supports(clip))
                    continue;
                AnimationCurve curve;
                try
                {
                    curve = descriptor.Read(clip);
                }
                catch (Exception error)
                {
                    context.ReportError("timeline_curve_read_failed", $"{clip.AuthoringId}:{descriptor.ChannelId.Value}", error.Message);
                    continue;
                }
                if (descriptor.IsDefault(curve))
                    continue;
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(TimelineCurveChannelCatalog))}.Require({String(descriptor.ChannelId.Value)}).Replace({clipVariable}, {CurveExpression(curve)});");
            }
        }

        string FieldExpression(
            BtsmtlAuthoringCodeExportContext context,
            object value,
            string subject)
        {
            if (value is IReadOnlyList<BtsmtlSkillStepPort> steps)
                return StepsExpression(context, steps, subject);
            if (value is BtsmtlSkillTargetSnapshotReference snapshot)
                return $"new {TypeName(typeof(BtsmtlSkillTargetSnapshotReference))}({String(snapshot.DeclarationId)}, {String(snapshot.OwnerId)})";
            if (value is GameplayTagQuery query)
                return GameplayTagQuery(query);
            if (value is UnityEngine.Object asset && context.TryGetVariable(asset, out string variable))
                return variable;
            return BtsmtlAuthoringCodeValues.Value(context, value, value?.GetType(), subject);
        }

        string StepsExpression(
            BtsmtlAuthoringCodeExportContext context,
            IReadOnlyList<BtsmtlSkillStepPort> steps,
            string subject)
        {
            if (steps.Count == 0)
                return $"Array.Empty<{TypeName(typeof(BtsmtlSkillStepPort))}>()";
            var expressions = new List<string>();
            for (int i = 0; i < steps.Count; i++)
            {
                BtsmtlSkillStepPort step = steps[i];
                if (step == null)
                {
                    context.ReportError("skill_step_missing", $"{subject}:{i}", "Skill组合节点包含空步骤。");
                    continue;
                }
                expressions.Add(
                    $"{TypeName(typeof(BtsmtlSkillAuthoringContract))}.CreateStep({String(step.Id)}, {String(step.Name)}, {GraphExpression(context, step.Condition)}, {step.Priority}, {EnumValue(typeof(ProgramAbortPolicy), step.AbortPolicy)})");
            }
            return expressions.Count == 0
                ? $"Array.Empty<{TypeName(typeof(BtsmtlSkillStepPort))}>()"
                : $"new[] {{ {string.Join(", ", expressions)} }}";
        }

        string TimelinePropertyValue(
            BtsmtlAuthoringCodeExportContext context,
            TimelineAuthoringPropertyValue property,
            string subject)
        {
            string expression;
            if (property.Value is UnityEngine.Object asset && context.TryGetVariable(asset, out string variable))
                expression = variable;
            else
                expression = BtsmtlAuthoringCodeValues.Value(
                    context,
                    property.Value,
                    property.Value?.GetType(),
                    $"{subject}:{property.PropertyId}");
            return $"new {TypeName(typeof(TimelineAuthoringPropertyValue))}({String(property.PropertyId)}, {EnumValue(typeof(TimelineAuthoringPropertyKind), property.Kind)}, {expression})";
        }

        void EmitGraphConnections(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs)
        {
            foreach (FlowGraph graph in graphs)
            {
                string graphVariable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                {
                    foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().OrderBy(value => value.UID, StringComparer.Ordinal))
                    {
                         if (connection.sourceNode is not FlowNode source || connection.targetNode is not FlowNode target)
                        {
                            context.ReportError("skill_edge_endpoint_missing", connection.UID, "Skill连线端点不是正式FlowNode。");
                            continue;
                        }
                        string edgeVariable = Variable(context, connection, $"edge:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}:{connection.UID}");
                        context.AddStatement(
                            BtsmtlAuthoringCodeEmissionPhase.Connect,
                            $"var {edgeVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureFlowConnection({graphVariable}, {Variable(context, source, NodeKey(source))}, {String(connection.sourcePortID)}, {Variable(context, target, NodeKey(target))}, {String(connection.targetPortID)}, {String(connection.UID)});");
                        BtsmtlSkillConnectionAuthoringValue configuration =
                            BtsmtlSkillAuthoringContract.ReadConnection(connection);
                        if (configuration.HasNonDefaultConfiguration)
                        {
                            context.AddStatement(
                                BtsmtlAuthoringCodeEmissionPhase.Connect,
                                $"{TypeName(typeof(BtsmtlSkillAuthoringContract))}.ConfigureConnection({edgeVariable}, {GraphExpression(context, configuration.Condition)}, {configuration.Priority}, {EnumValue(typeof(ProgramAbortPolicy), configuration.AbortPolicy)}, {configuration.Order});");
                        }
                    }
                }
            }
        }

        void EmitMachineConnections(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
            {
                string machineVariable = Variable(context, machine, $"fsm:{machine.AuthoringId}");
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
                    foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>().OrderBy(value => value.UID, StringComparer.Ordinal))
                    {
                        if (connection.targetNode is not BtsmtlSkillNativeState target)
                        {
                            context.ReportError("skill_state_edge_endpoint_missing", connection.UID, "Skill FSM连线目标不是正式State。");
                            continue;
                        }
                        string edgeVariable = Variable(context, connection, $"state-edge:{machine.AuthoringId}:{connection.UID}");
                        string stateVariable = Variable(context, state, $"state:{machine.AuthoringId}:{state.UID}");
                        string targetVariable = Variable(context, target, $"state:{machine.AuthoringId}:{target.UID}");
                        context.AddStatement(
                            BtsmtlAuthoringCodeEmissionPhase.Connect,
                            $"var {edgeVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureNativeConnection({machineVariable}, ({TypeName(typeof(BtsmtlSkillNativeState))}){stateVariable}, ({TypeName(typeof(BtsmtlSkillNativeState))}){targetVariable}, {String(connection.UID)});");
                        BtsmtlSkillConnectionAuthoringValue configuration =
                            BtsmtlSkillAuthoringContract.ReadConnection(connection);
                        if (configuration.HasNonDefaultConfiguration)
                            context.AddStatement(
                                BtsmtlAuthoringCodeEmissionPhase.Connect,
                                $"{TypeName(typeof(BtsmtlSkillAuthoringContract))}.ConfigureConnection({edgeVariable}, {GraphExpression(context, configuration.Condition)}, {configuration.Priority}, {EnumValue(typeof(ProgramAbortPolicy), configuration.AbortPolicy)}, {configuration.Order});");
                    }
            }
        }

        void EmitPrune(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines,
            IEnumerable<TimelineAsset> timelines)
        {
            foreach (FlowGraph graph in graphs)
            {
                string graphVariable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                string ids = StringArray(graph.allNodes.OfType<FlowNode>()
                    .Select(value => value.UID));
                string edgeIds = StringArray(graph.allNodes.OfType<FlowNode>()
                     .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                    .Select(value => value.UID));
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.PruneFlowGraph({graphVariable}, {ids}, {edgeIds});");
            }
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
            {
                string machineVariable = Variable(context, machine, $"fsm:{machine.AuthoringId}");
                string ids = StringArray(machine.allNodes.OfType<BtsmtlSkillNativeState>()
                    .Where(value => value is not BtsmtlSkillNativeEntryState && value is not BtsmtlSkillNativeAnyState && value is not BtsmtlSkillNativeExitState)
                    .Select(value => value.UID));
                string edgeIds = StringArray(machine.allNodes.OfType<BtsmtlSkillNativeState>()
                    .SelectMany(value => value.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    .Select(value => value.UID));
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.PruneNativeStateMachine({machineVariable}, {ids}, {edgeIds});");
            }
            foreach (FlowGraph graph in graphs)
            {
                if (graph is not BtsmtlSkillFlowGraph skillGraph)
                    continue;
                string graphVariable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                string variableIds = StringArray(skillGraph.blackboard.variables.Values.Select(value => value.ID));
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.PruneBlackboard({graphVariable}, {variableIds});");
            }
            foreach (TimelineAsset timeline in timelines)
            {
                if (timeline?.Data == null)
                    continue;
                string dataVariable = Variable(context, timeline.Data, $"timeline:{timeline.Data.AuthoringId}");
                string tracks = StringArray(timeline.Data.Tracks.Select(value => value.AuthoringId));
                string clips = StringArray(timeline.Data.Tracks.SelectMany(value => value.Clips).Select(value => value.AuthoringId));
                string sections = StringArray(timeline.Data.Sections.Select(value => value.AuthoringId));
                string bindings = StringArray(timeline.Data.ExternalBindings.Select(value => value.AuthoringId));
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.PruneTimeline({dataVariable}, {tracks}, {clips}, {sections}, {bindings});");
                string markers = StringArray(timeline.Data.Tracks.SelectMany(value => value.Markers).Select(value => value.AuthoringId));
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                    $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.PruneTimelineMarkers({dataVariable}, {markers});");
            }
        }

        string TimelineReference(
            BtsmtlAuthoringCodeExportContext context,
            Clip clip)
        {
            TimelineAuthoringReferenceValue reference = TimelineAuthoringPropertyContract.Reference(clip);
            if (reference.IsMissing)
                context.ReportError(reference.ErrorCode, clip.AuthoringId, reference.ErrorMessage);
            if (reference.Asset && context.TryGetVariable(reference.Asset, out string variable))
                return variable;
            if (reference.Asset)
                return ExternalAsset(context, reference.Asset, reference.Asset.GetType());
            return "null";
        }

        static bool NeedsClipSegmentOverride(Clip clip, UnityEngine.Object reference) =>
            TimelineAuthoringPropertyContract.NeedsSegmentOverride(clip, reference);

        static string CurveExpression(AnimationCurve curve) => BtsmtlAuthoringCodeValues.AnimationCurve(curve);

        string ExternalAsset(
            BtsmtlAuthoringCodeExportContext context,
            UnityEngine.Object asset,
            Type type) => BtsmtlAuthoringCodeValues.ExternalAsset(context, asset, type);

        string GraphExpression(
            BtsmtlAuthoringCodeExportContext context,
            FlowGraph graph)
        {
            if (graph == null)
                return "null";
            if (context.TryGetVariable(graph, out string variable))
                return variable;
            return ExternalAsset(context, graph, graph.GetType());
        }

        static void AddUnique<T>(List<T> values, T value) where T : class
        {
            if (value != null && !values.Contains(value))
                values.Add(value);
        }

        static void AddGraphOwner(
            BtsmtlAuthoringCodeExportContext context,
            IDictionary<FlowGraph, FlowGraph> owners,
            FlowGraph child,
            FlowGraph owner,
            string subject)
        {
            if (child == null || owner == null)
                return;
            if (owners.TryGetValue(child, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                context.ReportError("skill_graph_owner_ambiguous", subject, "同一Skill闭包对象出现多个owner。");
            else
                owners[child] = owner;
        }

        static void AddMachineOwner(
            BtsmtlAuthoringCodeExportContext context,
            IDictionary<BtsmtlSkillNativeStateMachine, FlowGraph> owners,
            BtsmtlSkillNativeStateMachine machine,
            FlowGraph owner,
            string nodeId)
        {
            if (owners.TryGetValue(machine, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                context.ReportError("skill_state_machine_owner_ambiguous", machine.AuthoringId, $"状态机被多个图节点拥有：{nodeId}。");
            else
                owners[machine] = owner;
        }

        static void AddTimelineOwner(
            BtsmtlAuthoringCodeExportContext context,
            IDictionary<TimelineAsset, FlowGraph> owners,
            TimelineAsset timeline,
            FlowGraph owner,
            string nodeId)
        {
            if (owners.TryGetValue(timeline, out FlowGraph existing) && !ReferenceEquals(existing, owner))
                context.ReportError("skill_timeline_owner_ambiguous", timeline.name, $"Timeline被多个图节点拥有：{nodeId}。");
            else
                owners[timeline] = owner;
        }

        static void RegisterObject(
            BtsmtlAuthoringCodeExportContext context,
            object source,
            string identity,
            string variableHint,
            bool isRoot = false,
            string sectionName = null,
            string storageTypeName = null)
        {
            if (source != null)
                context.RegisterObject(source, identity, variableHint, isRoot, sectionName, storageTypeName);
        }

        static string SectionForGraph(
            FlowGraph graph,
            UnityEngine.Object root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> owners,
            IReadOnlyDictionary<FlowGraph, FlowGraph> placementOwners)
        {
            if (ReferenceEquals(graph, root))
                return "Root";
            FlowGraph current = graph;
            if (placementOwners != null &&
                placementOwners.TryGetValue(current, out FlowGraph placementOwner) &&
                placementOwner != null)
                current = placementOwner;
            while (owners != null &&
                   owners.TryGetValue(current, out FlowGraph owner) &&
                   owner != null &&
                   !ReferenceEquals(owner, root))
                current = owner;
            if (current is BtsmtlSkillFlowGraph skillGraph)
            {
                if (skillGraph.Role == BtsmtlSkillFlowGraphRole.StateBody)
                    return ReadableSectionName(TrimSuffix(skillGraph.name, " State Body"), "StateBody");
                if (skillGraph.Role == BtsmtlSkillFlowGraphRole.ConditionRule)
                    return ReadableSectionName(skillGraph.name, "Condition");
            }
            return ReadableSectionName(current?.name, "Graph");
        }

        static string TimelineSection(TimelineAsset timeline) =>
            ReadableSectionName(timeline?.name, "Timeline");

        static string ReadableSectionName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            return BtsmtlAuthoringCodeSyntax.Identifier(value.Trim());
        }

        static string TrimSuffix(string value, string suffix) =>
            !string.IsNullOrEmpty(value) &&
            value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;

        static string Variable(BtsmtlAuthoringCodeExportContext context, object source, string subject)
        {
            if (context.TryGetVariable(source, out string variable))
                return variable;
            return context.RequireVariable(source, subject);
        }

        static string NodeKey(FlowNode node) =>
            $"node:{((IBtsmtlSkillAuthoringGraph)node.graph).AuthoringId}:{node.UID}";

        static string TypeName(Type type) => BtsmtlAuthoringCodeSyntax.TypeName(type);
        static string String(string value) => BtsmtlAuthoringCodeSyntax.StringLiteral(value);
        static string Bool(bool value) => value ? "true" : "false";
        static string EnumValue(Type type, Enum value) => BtsmtlAuthoringCodeSyntax.EnumLiteral(TypeName(type), value.ToString());

        static string StringArray(IEnumerable<string> values)
        {
            string[] items = (values ?? Enumerable.Empty<string>()).ToArray();
            return items.Length == 0
                ? "Array.Empty<string>()"
                : $"new[] {{ {string.Join(", ", items.Select(String))} }}";
        }

        static string GameplayTagArray(IEnumerable<GameplayTagId> values)
        {
            string[] items = (values ?? Enumerable.Empty<GameplayTagId>())
                .Select(value => $"new {TypeName(typeof(GameplayTagId))}({String(value.Value)})")
                .ToArray();
            return items.Length == 0
                ? $"Array.Empty<{TypeName(typeof(GameplayTagId))}>()"
                : $"new[] {{ {string.Join(", ", items)} }}";
        }

        string AssetArray(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<GameplayEffectDefinition> values,
            Type elementType)
        {
            string[] items = (values ?? Enumerable.Empty<GameplayEffectDefinition>())
                .Select(value => ExternalAsset(context, value, elementType))
                .ToArray();
            return items.Length == 0
                ? $"Array.Empty<{TypeName(elementType)}>()"
                : $"new[] {{ {string.Join(", ", items)} }}";
        }

        static string GameplayTagQuery(GameplayTagQuery query)
        {
            string Tags(IEnumerable<GameplayTagId> values)
            {
                string[] items = (values ?? Enumerable.Empty<GameplayTagId>())
                    .Select(value => "new " + TypeName(typeof(GameplayTagId)) + "(" + String(value.Value) + ")")
                    .ToArray();
                return items.Length == 0
                    ? "Array.Empty<" + TypeName(typeof(GameplayTagId)) + ">()"
                    : $"new[] {{ {string.Join(", ", items)} }}";
            }
            return $"new {TypeName(typeof(GameplayTagQuery))}({Tags(query?.All)}, {Tags(query?.Any)}, {Tags(query?.None)})";
        }
    }
}

