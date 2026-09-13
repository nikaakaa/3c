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
                case BtsmtlSkillFlowGraph graph:
                    EmitFlowGraph(
                        context,
                        graph,
                        context.Request.RecipeType.StartsWith("character.ability.", StringComparison.Ordinal),
                        null);
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

        void EmitFlowGraph(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlSkillFlowGraph root,
            bool abilityRoot,
            string abilityId)
        {
            IReadOnlyList<FlowGraph> closure;
            try
            {
                closure = BtsmtlSkillGraphClosure.Validate(root, true);
            }
            catch (Exception error)
            {
                context.ReportError("skill_graph_closure_invalid", root.AuthoringId, error.Message);
                return;
            }

            var graphs = closure
                .Where(graph => !IsRetiredLifecycleGraph(graph))
                .ToList();
            var graphOwners = new Dictionary<FlowGraph, FlowGraph>();
            var machines = new List<BtsmtlSkillNativeStateMachine>();
            var machineOwners = new Dictionary<BtsmtlSkillNativeStateMachine, FlowGraph>();
            var timelines = new List<TimelineAsset>();
            var timelineOwners = new Dictionary<TimelineAsset, FlowGraph>();
            CollectReferences(context, graphs, graphOwners, machines, machineOwners, timelines, timelineOwners);
            RegisterGraphs(context, graphs, root);
            RegisterMachines(context, machines);
            RegisterTimelines(context, timelines);

            string resolvedAbilityId = abilityRoot
                ? string.IsNullOrWhiteSpace(abilityId)
                    ? ResolveAbilityId(context, root)
                    : abilityId
                : string.Empty;
            EmitGraphCreation(context, graphs, root, graphOwners, abilityRoot, resolvedAbilityId);
            if (abilityRoot)
                EmitAbilityConfiguration(context, root, resolvedAbilityId, closure);
            EmitMachineCreation(context, machines, machineOwners);
            EmitTimelineCreation(context, timelines, timelineOwners, root);
            EmitGraphNodeCreation(context, graphs);
            EmitMachineNodeCreation(context, machines);
            EmitTimelineContentCreation(context, timelines);
            EmitGraphConfiguration(context, graphs);
            EmitMachineConfiguration(context, machines);
            EmitTimelineConfiguration(context, timelines);
            EmitGraphConnections(context, graphs);
            EmitMachineConnections(context, machines);
            EmitPrune(context, graphs, machines, timelines);
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.RootBinding,
                abilityRoot
                    ? $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.BindAbilityRoot(context, {Variable(context, root, root.AuthoringId)});"
                    : $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.BindSkillRoot(context, {Variable(context, root, root.AuthoringId)});");
        }

        string ResolveAbilityId(BtsmtlAuthoringCodeExportContext context, BtsmtlSkillFlowGraph root)
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(context.DefinitionAssetPath);
            CharacterSkillAuthoringDefinition legacy = definition?.SkillDefinitions
                .SingleOrDefault(value => value != null &&
                    string.Equals(value.EntryGraphAuthoringId, root.AuthoringId, StringComparison.Ordinal));
            if (legacy == null || string.IsNullOrWhiteSpace(legacy.SkillId))
                throw new InvalidOperationException($"Ability graph '{root.AuthoringId}' has no Ability identity source.");
            return legacy.SkillId;
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
            CharacterSkillAuthoringDefinition legacy = definition?.SkillDefinitions
                .SingleOrDefault(value => value != null &&
                    (string.Equals(value.SkillId, abilityId, StringComparison.Ordinal) ||
                     string.Equals(value.EntryGraphAuthoringId, root.AuthoringId, StringComparison.Ordinal)));
            AbilityGrant grant = ability != null
                ? definition?.AbilityGrants.SingleOrDefault(value => value != null && value.Ability == ability)
                : definition?.AbilityGrants.SingleOrDefault(value => value != null &&
                    string.Equals(value.AbilityId, abilityId, StringComparison.Ordinal));
            ActionProfile admissionProfile = ability?.AdmissionProfile ?? grant?.Ability?.AdmissionProfile ?? legacy?.ActionProfile;
            if (!admissionProfile)
            {
                context.ReportError("ability_admission_profile_missing", abilityId, "Gameplay Ability缺少准入规则来源。");
                return;
            }
            IReadOnlyList<GameplayEffectDefinition> effects = ability?.Effects ?? Array.Empty<GameplayEffectDefinition>();
            IReadOnlyList<GameplayAbilityEndRule> endRules = ability?.EndRules ??
                CollectLegacyEndRules(graphs);
            IReadOnlyList<string> followUps = ability?.AllowedFollowUpAbilityIds ??
                (legacy?.AllowedFollowUpSkillIds ?? Array.Empty<string>());
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
                ability?.SubgraphDependencies ?? legacy?.SubgraphDependencies ??
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
            string debugCategory = ability?.DebugCategory ?? string.Empty;
            string tags = GameplayTagArray(ability?.Tags);
            string followUpArray = StringArray(followUps);
            string sourceInput = grant?.SourceInputRequestId ?? legacy?.SourceInputRequestId ?? string.Empty;
            bool consume = grant?.ConsumeSourceInputRequest ?? legacy?.ConsumeSourceInputRequest ?? true;
            string targetInput = grant?.TargetInputValueId ?? legacy?.TargetInputValueId ?? string.Empty;
            string targetKey = grant?.TargetKey ?? legacy?.TargetKey ?? string.Empty;
            bool createGrant = ability == null;
            context.AddStatement(
                BtsmtlAuthoringCodeEmissionPhase.Configure,
                $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.ConfigureAbility(context, {String(debugCategory)}, {tags}, {ExternalAsset(context, admissionProfile, typeof(ActionProfile))}, {effectArray}, {ruleArray}, {dependencyArray}, {followUpArray}, {String(sourceInput)}, {Bool(consume)}, {String(targetInput)}, {String(targetKey)}, {Bool(createGrant)});");
        }

        static IReadOnlyList<GameplayAbilityEndRule> CollectLegacyEndRules(
            IReadOnlyList<FlowGraph> graphs)
        {
            var result = new List<GameplayAbilityEndRule>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var windowTypes = new Dictionary<BtsmtlSkillFlowGraph, string>();
            for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
            {
                if (graphs[graphIndex] is not BtsmtlSkillFlowGraph graph ||
                    graph.Role != BtsmtlSkillFlowGraphRole.ConditionRule)
                    continue;
                string windowType = string.Empty;
                foreach (BtsmtlSkillActionWindowActiveFlowNode node in graph.allNodes
                             .OfType<BtsmtlSkillActionWindowActiveFlowNode>())
                {
                    if (string.IsNullOrWhiteSpace(node.WindowType))
                        continue;
                    if (string.IsNullOrEmpty(windowType))
                        windowType = node.WindowType;
                    else if (!string.Equals(windowType, node.WindowType, StringComparison.Ordinal))
                    {
                        windowType = string.Empty;
                        break;
                    }
                }
                windowTypes[graph] = windowType;
            }
            for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
            {
                FlowGraph graph = graphs[graphIndex];
                foreach (BtsmtlSkillSubmitActionLifecycleFlowNode node in graph.allNodes
                             .OfType<BtsmtlSkillSubmitActionLifecycleFlowNode>()
                             .OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    GameplayAbilityEndTrigger trigger;
                    string actionWindowType = string.Empty;
                    switch (node.TransitionType)
                    {
                        case ActionLifecycleTransitionType.Complete:
                            trigger = GameplayAbilityEndTrigger.ExecutionCompleted;
                            break;
                        case ActionLifecycleTransitionType.Cancel:
                            trigger = GameplayAbilityEndTrigger.CancelRequested;
                            break;
                        case ActionLifecycleTransitionType.Interrupt:
                            trigger = GameplayAbilityEndTrigger.InterruptRequested;
                            break;
                        case ActionLifecycleTransitionType.Abort:
                            trigger = GameplayAbilityEndTrigger.AbortRequested;
                            break;
                        default:
                            continue;
                    }
                    if (node.name.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        node.TransitionType == ActionLifecycleTransitionType.Cancel)
                    {
                        actionWindowType = ResolveLegacyActionWindowType(
                            graph,
                            node,
                            windowTypes);
                        if (!string.IsNullOrEmpty(actionWindowType))
                            trigger = GameplayAbilityEndTrigger.ActionWindowClosed;
                    }
                    string ruleKey = string.Concat(trigger, "\u001f", actionWindowType);
                    if (!seen.Add(ruleKey))
                        continue;
                    var rule = new GameplayAbilityEndRule();
                    rule.Configure(trigger, node.TransitionType, actionWindowType, node.Reason);
                    result.Add(rule);
                }
            }
            return result;
        }

        static string ResolveLegacyActionWindowType(
            FlowGraph owner,
            BtsmtlSkillSubmitActionLifecycleFlowNode target,
            IReadOnlyDictionary<BtsmtlSkillFlowGraph, string> windowTypes)
        {
            var matches = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillSelectorFlowNode selector in owner.allNodes.OfType<BtsmtlSkillSelectorFlowNode>())
            {
                foreach (BinderConnection connection in selector.outConnections.OfType<BinderConnection>())
                {
                    if (!ReferenceEquals(connection.targetNode, target))
                        continue;
                    BtsmtlSkillStepPort step = selector.Steps.SingleOrDefault(value =>
                        string.Equals(value.Id, connection.sourcePortID, StringComparison.Ordinal));
                    if (step?.Condition is BtsmtlSkillFlowGraph condition &&
                        windowTypes.TryGetValue(condition, out string windowType) &&
                        !string.IsNullOrEmpty(windowType))
                        matches.Add(windowType);
                }
            }
            return matches.Count == 1 ? matches.Single() : string.Empty;
        }

        void EmitTimelineRoot(BtsmtlAuthoringCodeExportContext context, TimelineAsset root)
        {
            if (root.Data == null)
            {
                context.ReportError("timeline_data_missing", root.name, "TimelineAsset缺少TimelineData。");
                return;
            }
            RegisterTimeline(context, root, true);
            EmitTimelineCreation(context, new[] { root }, new Dictionary<TimelineAsset, FlowGraph>(), root);
            EmitTimelineContentCreation(context, new[] { root });
            EmitTimelineConfiguration(context, new[] { root });
            EmitPrune(context, Array.Empty<FlowGraph>(), Array.Empty<BtsmtlSkillNativeStateMachine>(), new[] { root });
        }

        void CollectReferences(
            BtsmtlAuthoringCodeExportContext context,
            IReadOnlyList<FlowGraph> graphs,
            Dictionary<FlowGraph, FlowGraph> graphOwners,
            List<BtsmtlSkillNativeStateMachine> machines,
            Dictionary<BtsmtlSkillNativeStateMachine, FlowGraph> machineOwners,
            List<TimelineAsset> timelines,
            Dictionary<TimelineAsset, FlowGraph> timelineOwners)
        {
            foreach (FlowGraph graph in graphs)
            {
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                {
                    if (IsRetiredLifecycleNode(node))
                        continue;
                    if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                        AddGraphOwner(context, graphOwners, macroGraph, graph, $"node:{node.UID}/macro");
                    if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine != null)
                    {
                        AddUnique(machines, stateMachine.StateMachine);
                        AddMachineOwner(context, machineOwners, stateMachine.StateMachine, graph, node.UID);
                        foreach (BtsmtlSkillFlowGraph child in BtsmtlSkillNativeStateMachineContract.References(stateMachine.StateMachine))
                            AddGraphOwner(context, graphOwners, child, graph, $"node:{node.UID}/state-machine");
                    }
                    if (node is BtsmtlSkillStateFlowNode state && state.Body != null)
                        AddGraphOwner(context, graphOwners, state.Body, graph, $"node:{node.UID}/body");
                    if (node is BtsmtlSkillTimelineFlowNode timelineNode && timelineNode.TimelineAsset != null)
                    {
                        AddUnique(timelines, timelineNode.TimelineAsset);
                        AddTimelineOwner(context, timelineOwners, timelineNode.TimelineAsset, graph, node.UID);
                        foreach (TreeClip tree in timelineNode.Timeline?.Tracks.SelectMany(value => value.Clips).OfType<TreeClip>() ?? Enumerable.Empty<TreeClip>())
                            if (tree.AssetTree is BtsmtlSkillFlowGraph child)
                                AddGraphOwner(context, graphOwners, child, graph, $"node:{node.UID}/timeline/clip:{tree.AuthoringId}");
                    }
                    if (node is BtsmtlSkillCompositeFlowNode composite)
                        foreach (BtsmtlSkillStepPort step in composite.Steps)
                            if (step?.Condition != null)
                                AddGraphOwner(context, graphOwners, step.Condition, graph, $"node:{node.UID}/step:{step.Id}");
                    foreach (BtsmtlSkillFlowConnection transfer in node.outConnections.OfType<BtsmtlSkillFlowConnection>())
                        if (transfer.Condition != null)
                            AddGraphOwner(context, graphOwners, transfer.Condition, graph, $"node:{node.UID}/edge:{transfer.UID}");
                }
            }
            foreach (BtsmtlSkillNativeStateMachine machine in machines.ToArray())
            {
                FlowGraph owner = machineOwners[machine];
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
                {
                    if (state.Body != null)
                        AddGraphOwner(context, graphOwners, state.Body, owner, $"fsm:{machine.AuthoringId}/state:{state.UID}/body");
                    foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                        if (connection.Condition != null)
                            AddGraphOwner(context, graphOwners, connection.Condition, owner, $"fsm:{machine.AuthoringId}/edge:{connection.UID}");
                }
            }
        }

        void RegisterGraphs(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs,
            FlowGraph root)
        {
            foreach (FlowGraph graph in graphs)
            {
                if (IsRetiredLifecycleGraph(graph))
                    continue;
                if (graph is not IBtsmtlSkillAuthoringGraph authoring)
                {
                    context.ReportError("skill_graph_type_invalid", graph?.GetType().FullName, "Skill闭包包含非正式技能图。");
                    continue;
                }
                RegisterObject(context, graph, $"graph:{authoring.AuthoringId}", "graph", ReferenceEquals(graph, root));
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                {
                    if (IsRetiredLifecycleNode(node))
                        continue;
                    RegisterNode(context, graph, node);
                }
            }
        }

        void RegisterMachines(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<BtsmtlSkillNativeStateMachine> machines)
        {
            foreach (BtsmtlSkillNativeStateMachine machine in machines)
            {
                RegisterObject(context, machine, $"fsm:{machine.AuthoringId}", "stateMachine");
                RegisterMachineMembers(context, machine);
            }
        }

        void RegisterMachineMembers(
            BtsmtlAuthoringCodeExportContext context,
            BtsmtlSkillNativeStateMachine machine)
        {
            foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
            {
                RegisterObject(context, state, $"state:{machine.AuthoringId}:{state.UID}", "state");
                foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    RegisterObject(context, connection, $"state-edge:{machine.AuthoringId}:{connection.UID}", "stateEdge");
            }
        }

        void RegisterTimelines(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<TimelineAsset> timelines)
        {
            foreach (TimelineAsset timeline in timelines)
                RegisterTimeline(context, timeline, false);
        }

        void RegisterTimeline(
            BtsmtlAuthoringCodeExportContext context,
            TimelineAsset timeline,
            bool isRoot)
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
            RegisterObject(context, timeline, $"timeline-asset:{assetPath}:{localFileId}", "timeline", isRoot);
            RegisterObject(context, timeline.Data, $"timeline:{timelineIdentity}", "timelineData");
            foreach (Track track in timeline.Data.Tracks)
            {
                RegisterObject(context, track, $"track:{timelineIdentity}:{track.AuthoringId}", "track");
                foreach (Clip clip in track.Clips)
                    RegisterObject(context, clip, $"clip:{timelineIdentity}:{clip.AuthoringId}", "clip");
            }
            foreach (TimelineSection section in timeline.Data.Sections)
                RegisterObject(context, section, $"section:{timelineIdentity}:{section.AuthoringId}", "section");
            foreach (TimelineExternalBindingDeclaration binding in timeline.Data.ExternalBindings)
                RegisterObject(context, binding, $"binding:{timelineIdentity}:{binding.AuthoringId}", "binding");
        }

        void RegisterNode(BtsmtlAuthoringCodeExportContext context, FlowGraph graph, FlowNode node)
        {
            string graphIdentity = (graph as IBtsmtlSkillAuthoringGraph)?.AuthoringId ?? graph.GetType().FullName;
            RegisterObject(context, node, $"node:{graphIdentity}:{node.UID}", "node");
            foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>()
                         .Where(value => value.targetNode is not FlowNode target || !IsRetiredLifecycleNode(target)))
                RegisterObject(context, connection, $"edge:{graphIdentity}:{connection.UID}", "edge");
        }

        void EmitGraphCreation(
            BtsmtlAuthoringCodeExportContext context,
            IReadOnlyList<FlowGraph> graphs,
            BtsmtlSkillFlowGraph root,
            IReadOnlyDictionary<FlowGraph, FlowGraph> owners,
            bool abilityRoot,
            string abilityId)
        {
            foreach (FlowGraph graph in graphs)
            {
                string variable = Variable(context, graph, $"graph:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}");
                IBtsmtlSkillAuthoringGraph authoring = (IBtsmtlSkillAuthoringGraph)graph;
                string expression;
                if (ReferenceEquals(graph, root) && graph is BtsmtlSkillFlowGraph skillRoot &&
                    skillRoot.Role == BtsmtlSkillFlowGraphRole.Skill)
                {
                    expression = abilityRoot
                        ? $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureAbilityRoot(context, {String(abilityId)}, {String(skillRoot.AuthoringId)}, {String(skillRoot.name)})"
                        : $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureSkillRoot(context, {String(skillRoot.AuthoringId)}, {String(skillRoot.name)})";
                }
                else if (IsPrivateSubAsset(graph, root) && owners.TryGetValue(graph, out FlowGraph owner))
                {
                    string ownerVariable = Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}");
                    expression = graph is BtsmtlSkillMacroGraph
                        ? $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureMacro({ownerVariable}, {String(authoring.AuthoringId)}, {String(graph.name)})"
                        : $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureChildGraph({ownerVariable}, {String(authoring.AuthoringId)}, {EnumValue(typeof(BtsmtlSkillFlowGraphRole), ((BtsmtlSkillFlowGraph)graph).Role)}, {String(graph.name)})";
                }
                else
                {
                    if (ReferenceEquals(graph, root) && graph is not BtsmtlSkillMacroGraph)
                        context.ReportError("skill_root_role_unsupported", authoring.AuthoringId, "只有Skill根图可以作为Definition生成根。");
                    expression = ExternalAsset(context, graph, graph.GetType());
                }
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Create, $"var {variable} = {expression};");
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
                if (owners.TryGetValue(machine, out FlowGraph owner) && IsPrivateSubAsset(machine, owner))
                {
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureStateMachine({Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}")}, {String(machine.AuthoringId)}, {String(machine.name)}, {String(machine.OwnerGraphId)}, {String(machine.OwnerNodeId)})";
                }
                else
                    expression = ExternalAsset(context, machine, machine.GetType());
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Create, $"var {variable} = {expression};");
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
                if (ReferenceEquals(timeline, root))
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTimelineRoot(context, {String(timeline.Data.AuthoringId)}, {String(timeline.name)})";
                else if (owners.TryGetValue(timeline, out FlowGraph owner) && IsPrivateSubAsset(timeline, root) && owner != null)
                    expression = $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTimeline({Variable(context, owner, $"graph:{((IBtsmtlSkillAuthoringGraph)owner).AuthoringId}")}, {String(timeline.Data.AuthoringId)}, {String(timeline.name)})";
                else
                    expression = ExternalAsset(context, timeline, typeof(TimelineAsset));
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Create, $"var {variable} = {expression};");
            }
        }

        void EmitGraphNodeCreation(
            BtsmtlAuthoringCodeExportContext context,
            IEnumerable<FlowGraph> graphs)
        {
            foreach (FlowGraph graph in graphs)
                foreach (FlowNode node in graph.allNodes.OfType<FlowNode>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    if (IsRetiredLifecycleNode(node))
                        continue;
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
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Create, $"var {dataVariable} = {timelineVariable}.Data;");
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Create,
                    $"var {catalogVariable} = {TypeName(typeof(TimelineTreeContractComposition))}.Create();");
                foreach (Track track in timeline.Data.Tracks)
                {
                    string trackVariable = Variable(context, track, $"track:{timelineIdentity}:{track.AuthoringId}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        $"var {trackVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureTrack({dataVariable}, {catalogVariable}, typeof({TypeName(track.GetType())}), {String(track.AuthoringId)}, {String(track.Name)});");
                    foreach (Clip clip in track.Clips)
                    {
                        string clipVariable = Variable(context, clip, $"clip:{timelineIdentity}:{clip.AuthoringId}");
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Create,
                        NeedsClipSegmentOverride(clip)
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
                    if (IsRetiredLifecycleNode(node))
                        continue;
                    EmitNodeConfiguration(context, node);
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

        void EmitNodeConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            FlowNode node)
        {
            string nodeVariable = Variable(context, node, NodeKey(node));
            if (node is BtsmtlSkillCompositeFlowNode composite)
            {
                var stepVariables = new List<string>();
                for (int index = 0; index < composite.Steps.Count; index++)
                {
                    BtsmtlSkillStepPort step = composite.Steps[index];
                    string stepVariable = $"{nodeVariable}_step_{index}";
                    stepVariables.Add(stepVariable);
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"var {stepVariable} = new {TypeName(typeof(BtsmtlSkillStepPort))}({String(step.Id)}, {String(step.Name)});");
                    if (step.Condition != null || step.Priority != 0 || step.AbortPolicy != ProgramAbortPolicy.None)
                        context.AddStatement(
                            BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"{stepVariable}.Configure({String(step.Name)}, {GraphExpression(context, step.Condition)}, {step.Priority}, {EnumValue(typeof(ProgramAbortPolicy), step.AbortPolicy)});");
                }
                string steps = stepVariables.Count == 0
                    ? $"Array.Empty<{TypeName(typeof(BtsmtlSkillStepPort))}>()"
                    : $"new[] {{ {string.Join(", ", stepVariables)} }}";
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCompositeFlowNode))}){nodeVariable}).SetSteps({steps});");
            }
            if (node is BtsmtlSkillLoopFlowNode loop && loop.StopType != BtsmtlSkillLoopStopType.None)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillLoopFlowNode))}){nodeVariable}).SetStopType({EnumValue(typeof(BtsmtlSkillLoopStopType), loop.StopType)});");
            if (node is BtsmtlSkillParallelFlowNode parallel && parallel.Mode != BtsmtlSkillParallelMode.JumpComplete)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillParallelFlowNode))}){nodeVariable}).SetMode({EnumValue(typeof(BtsmtlSkillParallelMode), parallel.Mode)});");
            if (node is BtsmtlSkillStateExitCauseFlowNode cause && cause.Cause != BtsmtlSkillStateExitCause.StateTransition)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillStateExitCauseFlowNode))}){nodeVariable}).SetCause({EnumValue(typeof(BtsmtlSkillStateExitCause), cause.Cause)});");
            if (node is IBtsmtlSkillInputNode input)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(IBtsmtlSkillInputNode))}){nodeVariable}).SetInputId({String(input.InputId)}, {String(input.ProviderOwnerId)});");
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillGameplayTagFlowNode))}){nodeVariable}).Configure(new {TypeName(typeof(GameplayTagId))}({String(tag.Tag.Value)}), {String(tag.ProviderOwnerId)});");
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillMoveFacingAngleFlowNode))}){nodeVariable}).Configure({String(moveFacing.ProviderOwnerId)});");
            if (node is BtsmtlSkillCharacterStateVector3FlowNode stateVector3)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCharacterStateVector3FlowNode))}){nodeVariable}).Configure({String(stateVector3.FieldId)}, {String(stateVector3.ProviderOwnerId)});");
            if (node is BtsmtlSkillCharacterStateScalarFlowNode stateScalar)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCharacterStateScalarFlowNode))}){nodeVariable}).Configure({String(stateScalar.FieldId)}, {String(stateScalar.ProviderOwnerId)});");
            if (node is BtsmtlSkillCharacterStateYawFlowNode stateYaw)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCharacterStateYawFlowNode))}){nodeVariable}).Configure({String(stateYaw.FieldId)}, {String(stateYaw.ProviderOwnerId)});");
            if (node is BtsmtlSkillCharacterStateBooleanFlowNode stateBoolean)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCharacterStateBooleanFlowNode))}){nodeVariable}).Configure({String(stateBoolean.FieldId)}, {String(stateBoolean.ProviderOwnerId)});");
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillGameplayTagQueryFlowNode))}){nodeVariable}).Configure({GameplayTagQuery(tagQuery.Query)}, {String(tagQuery.ProviderOwnerId)});");
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillGameplayAttributeFlowNode))}){nodeVariable}).Configure(new {TypeName(typeof(GameplayAttributeId))}({String(attribute.Attribute.Value)}), {String(attribute.ProviderOwnerId)});");
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillApplyGameplayEffectFlowNode))}){nodeVariable}).Configure({ExternalAsset(context, applyEffect.Effect, typeof(GameplayEffectDefinition))}, {ActionContextExpression(context, applyEffect.ActionContext)}, {Bool(applyEffect.Predicted)}, {String(applyEffect.ProviderOwnerId)});");
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillRemoveGameplayEffectFlowNode))}){nodeVariable}).Configure({EnumValue(typeof(ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector), removeEffect.Selector)}, {removeEffect.Handle}UL, {ExternalAsset(context, removeEffect.Effect, typeof(GameplayEffectDefinition))}, {GameplayTagQuery(removeEffect.EffectTagQuery)}, {String(removeEffect.ProviderOwnerId)});");
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillActionContextActiveFlowNode))}){nodeVariable}).SetActionContext({ActionContextExpression(context, contextActive.ActionContext)});");
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillActionWindowActiveFlowNode))}){nodeVariable}).SetWindowType({String(window.WindowType)});");
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillCanActivateActionFlowNode))}){nodeVariable}).Configure({ExternalAsset(context, admission.ActionProfile, typeof(ActionProfile))}, {String(admission.TargetSnapshotDeclarationId)}, {String(admission.TargetSnapshotOwnerId)});");
            if (node is IBtsmtlSkillBlackboardReadNode blackboardRead)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(node.GetType())}){nodeVariable}).SetVariable(new {TypeName(typeof(BtsmtlSkillBlackboardReference))}({String(blackboardRead.Variable.DeclarationId)}, {String(blackboardRead.Variable.OwnerId)}));");
            if (node is BtsmtlSkillBlackboardAccessFlowNode blackboard)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillBlackboardAccessFlowNode))}){nodeVariable}).Configure(new {TypeName(typeof(BtsmtlSkillBlackboardReference))}({String(blackboard.Variable.DeclarationId)}, {String(blackboard.Variable.OwnerId)}), {EnumValue(typeof(BtsmtlSkillBlackboardValueType), blackboard.DeclaredType)}, {ExternalAsset(context, blackboard.FactContext, typeof(UnityEngine.Object))});");
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine != null)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Bind,
                    $"(({TypeName(typeof(BtsmtlSkillStateMachineFlowNode))}){nodeVariable}).SetStateMachine({Variable(context, stateMachine.StateMachine, $"fsm:{stateMachine.StateMachine.AuthoringId}")});");
            if (node is BtsmtlSkillStateFlowNode state && state.Body != null)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Bind,
                    $"(({TypeName(typeof(BtsmtlSkillStateFlowNode))}){nodeVariable}).SetBody({GraphExpression(context, state.Body)});");
            if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset != null)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Bind,
                    $"(({TypeName(typeof(BtsmtlSkillTimelineFlowNode))}){nodeVariable}).Configure({Variable(context, timeline.TimelineAsset, $"timeline-asset:{AssetDatabase.GetAssetPath(timeline.TimelineAsset)}")}, {EnumValue(typeof(BtsmtlSkillTimelineOwnership), timeline.Ownership)}, {ActionContextExpression(context, timeline.ActionContext)}, {EnumValue(typeof(TimelinePlaybackMode), timeline.PlaybackMode)});");
            if (node is BtsmtlSkillLocomotionFlowNode locomotion &&
                (!Mathf.Approximately(locomotion.MoveSpeed, ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultMoveSpeed) ||
                 locomotion.DisplacementMode != ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultDisplacementMode ||
                 locomotion.ActionMotionCurve != null ||
                 !Mathf.Approximately(locomotion.TurnSpeedDegrees, ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultTurnSpeedDegrees) ||
                 locomotion.CameraRelative != ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultCameraRelative ||
                 locomotion.ExecutionMode != ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultExecutionMode ||
                 !Mathf.Approximately(locomotion.DurationSeconds, ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultDurationSeconds)))
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(BtsmtlSkillLocomotionFlowNode))}){nodeVariable}).Configure({BtsmtlAuthoringCodeSyntax.FloatLiteral(locomotion.MoveSpeed)}, {EnumValue(typeof(LocomotionInputMotionDisplacementMode), locomotion.DisplacementMode)}, {ExternalAsset(context, locomotion.ActionMotionCurve, typeof(RootMotionCurveAsset))}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(locomotion.TurnSpeedDegrees)}, {Bool(locomotion.CameraRelative)}, {EnumValue(typeof(LocomotionInputMotionExecutionMode), locomotion.ExecutionMode)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(locomotion.DurationSeconds)});");
            if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Bind,
                    $"(({TypeName(typeof(MacroNodeWrapper))}){nodeVariable}).macro = {Variable(context, macroGraph, $"graph:{macroGraph.AuthoringId}")};");
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
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{dataVariable}.ConfigureAuthoringIdentity({String(data.AuthoringId)});");
                if (!string.Equals(data.Name, timeline.name, StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{dataVariable}.Name = {String(data.Name)};");
                if (!Mathf.Approximately(data.Scale, 1f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{dataVariable}.Scale = {BtsmtlAuthoringCodeSyntax.FloatLiteral(data.Scale)};");
                if (data.Loop)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"{dataVariable}.Loop = true;");
            string catalogVariable = context.AllocateVariableName("timelineCatalog");
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
                    if (track.PersistentMuted)
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"{trackVariable}.PersistentMuted = true;");
                    if (track is AnimationTrack animation)
                    {
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"(({TypeName(typeof(AnimationTrack))}){trackVariable}).SetAnimationChannelId(new {TypeName(typeof(AnimationChannelId))}({String(animation.AnimationChannelId.Value)}));");
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                            $"(({TypeName(typeof(AnimationTrack))}){trackVariable}).SetAnimationSlotId({String(animation.AnimationSlotId)});");
                    }
                    foreach (Clip clip in track.Clips)
                        EmitClipConfiguration(context, data, clip);
                }
            }
        }

        void EmitClipConfiguration(
            BtsmtlAuthoringCodeExportContext context,
            TimelineData data,
            Clip clip)
        {
            string clipVariable = Variable(context, clip, $"clip:{data.AuthoringId}:{clip.AuthoringId}");
            if (clip is TimelineAnimationClip animation)
            {
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(TimelineAnimationClip))}){clipVariable}).Clip = {ExternalAsset(context, animation.Clip, typeof(UnityEngine.AnimationClip))};");
                if (animation.ExtraPolationMode != ExtraPolationMode.None)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(TimelineAnimationClip))}){clipVariable}).ExtraPolationMode = {EnumValue(typeof(ExtraPolationMode), animation.ExtraPolationMode)};");
                if (!string.IsNullOrEmpty(animation.BlendProfileId))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(TimelineAnimationClip))}){clipVariable}).BlendProfileId = {String(animation.BlendProfileId)};");
            }
            if (clip is MotionCurveClip motion)
            {
                if (!string.Equals(motion.CurveId, "MotionCurve", StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).CurveId = {String(motion.CurveId)};");
                if (motion.CurveEndFrame != motion.EndFrame || NeedsClipSegmentOverride(clip))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).CurveEndFrame = {motion.CurveEndFrame};");
                if (motion.Space != TimelineMotionContributionSpace.Local)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).Space = {EnumValue(typeof(TimelineMotionContributionSpace), motion.Space)};");
                if (motion.Channel != TimelineMotionChannel.Action)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).Channel = {EnumValue(typeof(TimelineMotionChannel), motion.Channel)};");
                if (motion.BlendMode != TimelineMotionBlendMode.Override)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).BlendMode = {EnumValue(typeof(TimelineMotionBlendMode), motion.BlendMode)};");
                if (motion.Priority != 100)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).Priority = {motion.Priority};");
                if (!motion.ConsumeLowerChannels)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(MotionCurveClip))}){clipVariable}).ConsumeLowerChannels = false;");
            }
            if (clip is MotionWarpClip warp)
            {
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(MotionWarpClip))}){clipVariable}).ConfigureAuthoring({EnumValue(typeof(MotionWarpTranslationMode), warp.TranslationMode)}, {EnumValue(typeof(MotionWarpTargetOffsetSpace), warp.TargetOffsetSpace)}, {EnumValue(typeof(MotionWarpRotationMode), warp.RotationMode)}, {EnumValue(typeof(MotionWarpRotationMethod), warp.RotationMethod)}, {BtsmtlAuthoringCodeValues.Vector2(warp.TargetPlanarOffset)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(warp.TargetYawOffsetDegrees)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(warp.MaxTotalPositionCorrection)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(warp.MaxTotalYawCorrectionDegrees)}, {BtsmtlAuthoringCodeSyntax.FloatLiteral(warp.MaximumYawRateDegreesPerSecond)}, {EnumValue(typeof(MotionWarpLimitPolicy), warp.LimitPolicy)}, {MotionWarpCurve(warp, true)}, {MotionWarpCurve(warp, false)});");
                if (!string.IsNullOrEmpty(warp.SourceMotionClipId))
                {
                    MotionCurveClip source = data.Tracks.SelectMany(value => value.Clips).OfType<MotionCurveClip>()
                        .SingleOrDefault(value => value.AuthoringId == warp.SourceMotionClipId);
                    if (source == null)
                        context.ReportError("motion_warp_source_missing", warp.AuthoringId, "MotionWarp source identity无法解析。");
                    else
                        context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Bind,
                            $"{TypeName(typeof(MotionWarpAuthoring))}.BindSource({Variable(context, data, $"timeline:{data.AuthoringId}")}, ({TypeName(typeof(MotionWarpClip))}){clipVariable}, ({TypeName(typeof(MotionCurveClip))}){Variable(context, source, $"clip:{data.AuthoringId}:{source.AuthoringId}")});");
                }
            }
            if (clip is ActionCueClip actionCue)
            {
                if (!string.Equals(actionCue.CueId, "Cue", StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(ActionCueClip))}){clipVariable}).CueId = {String(actionCue.CueId)};");
                if (!string.Equals(actionCue.CueType, "Gameplay", StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(ActionCueClip))}){clipVariable}).CueType = {String(actionCue.CueType)};");
            }
            if (clip is CameraStateClip cameraState)
            {
                if (cameraState.Mode != TimelineCameraMode.SkillCloseup)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).Mode = {EnumValue(typeof(TimelineCameraMode), cameraState.Mode)};");
                if (!string.IsNullOrEmpty(cameraState.SequenceId))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).SequenceId = {String(cameraState.SequenceId)};");
                if (cameraState.Priority != 100)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).Priority = {cameraState.Priority};");
                if (!Mathf.Approximately(cameraState.BlendInSeconds, 0.15f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).BlendInSeconds = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraState.BlendInSeconds)};");
                if (!Mathf.Approximately(cameraState.BlendOutSeconds, 0.2f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).BlendOutSeconds = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraState.BlendOutSeconds)};");
                if (!string.IsNullOrEmpty(cameraState.TargetKey))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).TargetKey = {String(cameraState.TargetKey)};");
                if (cameraState.InterruptPolicy != TimelineCameraInterruptPolicy.BlendOut)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStateClip))}){clipVariable}).InterruptPolicy = {EnumValue(typeof(TimelineCameraInterruptPolicy), cameraState.InterruptPolicy)};");
            }
            if (clip is CameraCueClip cameraCue)
            {
                if (!string.Equals(cameraCue.CueId, "CameraCue", StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).CueId = {String(cameraCue.CueId)};");
                if (cameraCue.CueKind != TimelineCameraCueKind.Shake)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).CueKind = {EnumValue(typeof(TimelineCameraCueKind), cameraCue.CueKind)};");
                if (!string.Equals(cameraCue.CueType, "Camera", StringComparison.Ordinal))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).CueType = {String(cameraCue.CueType)};");
                if (!string.IsNullOrEmpty(cameraCue.ResourceId))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).ResourceId = {String(cameraCue.ResourceId)};");
                if (!Mathf.Approximately(cameraCue.Intensity, 1f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).Intensity = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraCue.Intensity)};");
                if (!Mathf.Approximately(cameraCue.DurationSeconds, 0.2f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).DurationSeconds = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraCue.DurationSeconds)};");
                if (cameraCue.Priority != 0)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).Priority = {cameraCue.Priority};");
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).WeightCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraCue.WeightCurve)};");
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).EaseInCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraCue.EaseInCurve)};");
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(CameraCueClip))}){clipVariable}).EaseOutCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraCue.EaseOutCurve)};");
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                if (cameraResponse.LookResponse != TimelineCameraLookResponseMode.Suppressed)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResponseClip))}){clipVariable}).LookResponse = {EnumValue(typeof(TimelineCameraLookResponseMode), cameraResponse.LookResponse)};");
                if (!Mathf.Approximately(cameraResponse.ManualOrbitWeight, 0f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResponseClip))}){clipVariable}).ManualOrbitWeight = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraResponse.ManualOrbitWeight)};");
                if (!Mathf.Approximately(cameraResponse.PitchResponseWeight, 1f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResponseClip))}){clipVariable}).PitchResponseWeight = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraResponse.PitchResponseWeight)};");
                if (!Mathf.Approximately(cameraResponse.YawResponseWeight, 1f))
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResponseClip))}){clipVariable}).YawResponseWeight = {BtsmtlAuthoringCodeSyntax.FloatLiteral(cameraResponse.YawResponseWeight)};");
                if (cameraResponse.Priority != 100)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResponseClip))}){clipVariable}).Priority = {cameraResponse.Priority};");
            }
            if (clip is CameraOverrideClip cameraOverride)
            {
                if (!cameraOverride.OverrideTrack)
                    context.ReportError("camera_override_resource_missing", cameraOverride.AuthoringId, "Camera Override Clip缺少正式资源引用。");
                else
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraOverrideClip))}){clipVariable}).OverrideTrack = {ExternalAsset(context, cameraOverride.OverrideTrack, typeof(CameraOverrideTrackAsset))};");
            }
            if (clip is CameraZoomClip cameraZoom)
            {
                if (!cameraZoom.Zoom)
                    context.ReportError("camera_zoom_resource_missing", cameraZoom.AuthoringId, "Camera Zoom Clip缺少正式资源引用。");
                else
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraZoomClip))}){clipVariable}).Zoom = {ExternalAsset(context, cameraZoom.Zoom, typeof(CameraZoomAsset))};");
            }
            if (clip is CameraStretchClip cameraStretch)
            {
                if (!cameraStretch.Stretch)
                    context.ReportError("camera_stretch_resource_missing", cameraStretch.AuthoringId, "Camera Stretch Clip缺少正式资源引用。");
                else
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraStretchClip))}){clipVariable}).Stretch = {ExternalAsset(context, cameraStretch.Stretch, typeof(CameraStretchAsset))};");
            }
            if (clip is CameraShotClip cameraShot)
            {
                if (!cameraShot.Shot)
                    context.ReportError("camera_shot_resource_missing", cameraShot.AuthoringId, "Camera Shot Clip缺少正式资源引用。");
                else
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraShotClip))}){clipVariable}).Shot = {ExternalAsset(context, cameraShot.Shot, typeof(CameraShotAsset))};");
            }
            if (clip is CameraResourceClip cameraResource)
            {
                if (!IsLinearCurve(cameraResource.WeightCurve, 1f, 1f))
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResourceClip))}){clipVariable}).WeightCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraResource.WeightCurve)};");
                if (!IsLinearCurve(cameraResource.EaseInCurve, 0f, 1f))
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResourceClip))}){clipVariable}).EaseInCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraResource.EaseInCurve)};");
                if (!IsLinearCurve(cameraResource.EaseOutCurve, 0f, 1f))
                    context.AddStatement(
                        BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(CameraResourceClip))}){clipVariable}).EaseOutCurve = {BtsmtlAuthoringCodeValues.AnimationCurve(cameraResource.EaseOutCurve)};");
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
                context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"(({TypeName(typeof(ScenePresentationParameterCurveClip))}){clipVariable}).ConfigureBindings({String(sceneParameter.TargetBindingId)}, {String(sceneParameter.ParameterBindingId)}, {CurveExpression(sceneParameter.ValueCurve, TimelineCurveChannelCatalog.ScenePresentationValue)});");
            if (clip is TreeClip tree)
                if (tree.ExecutionPhase != TimelineTreeExecutionPhase.Commit)
                    context.AddStatement(BtsmtlAuthoringCodeEmissionPhase.Configure,
                        $"(({TypeName(typeof(TreeClip))}){clipVariable}).SetExecutionPhase({EnumValue(typeof(TimelineTreeExecutionPhase), tree.ExecutionPhase)});");

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
                if (IsDefaultCurve(curve, descriptor))
                    continue;
                context.AddStatement(
                    BtsmtlAuthoringCodeEmissionPhase.Configure,
                    $"{TypeName(typeof(TimelineCurveChannelCatalog))}.Require({String(descriptor.ChannelId.Value)}).Replace({clipVariable}, {CurveExpression(curve, descriptor.ChannelId)});");
            }
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
                    if (IsRetiredLifecycleNode(node))
                        continue;
                    foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().OrderBy(value => value.UID, StringComparer.Ordinal))
                    {
                         if (connection.sourceNode is not FlowNode source || connection.targetNode is not FlowNode target)
                        {
                            context.ReportError("skill_edge_endpoint_missing", connection.UID, "Skill连线端点不是正式FlowNode。");
                            continue;
                        }
                        if (IsRetiredLifecycleNode(source) || IsRetiredLifecycleNode(target))
                            continue;
                        string edgeVariable = Variable(context, connection, $"edge:{((IBtsmtlSkillAuthoringGraph)graph).AuthoringId}:{connection.UID}");
                        context.AddStatement(
                            BtsmtlAuthoringCodeEmissionPhase.Connect,
                            $"var {edgeVariable} = {TypeName(typeof(BtsmtlSkillAuthoringCode))}.EnsureFlowConnection({graphVariable}, {Variable(context, source, NodeKey(source))}, {String(connection.sourcePortID)}, {Variable(context, target, NodeKey(target))}, {String(connection.targetPortID)}, {String(connection.UID)});");
                        if (connection is BtsmtlSkillFlowConnection transfer &&
                            (transfer.Condition != null || transfer.Priority != 0 ||
                             transfer.AbortPolicy != ProgramAbortPolicy.None || transfer.Order != 0))
                        {
                            context.AddStatement(
                                BtsmtlAuthoringCodeEmissionPhase.Connect,
                                $"(({TypeName(typeof(BtsmtlSkillFlowConnection))}){edgeVariable}).Configure({GraphExpression(context, transfer.Condition)}, {transfer.Priority}, {EnumValue(typeof(ProgramAbortPolicy), transfer.AbortPolicy)}, {transfer.Order});");
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
                        if (connection.Condition != null || connection.Priority != 0 ||
                            connection.AbortPolicy != ProgramAbortPolicy.None || connection.Order != 0)
                            context.AddStatement(
                                BtsmtlAuthoringCodeEmissionPhase.Connect,
                                $"{TypeName(typeof(BtsmtlSkillAuthoringCode))}.ConfigureNativeConnection(({TypeName(typeof(BtsmtlSkillNativeConnection))}){edgeVariable}, {GraphExpression(context, connection.Condition)}, {connection.Priority}, {EnumValue(typeof(ProgramAbortPolicy), connection.AbortPolicy)}, {connection.Order});");
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
                    .Where(value => !IsRetiredLifecycleNode(value))
                    .Select(value => value.UID));
                string edgeIds = StringArray(graph.allNodes.OfType<FlowNode>()
                    .Where(value => !IsRetiredLifecycleNode(value))
                     .SelectMany(value => value.outConnections.OfType<BinderConnection>()
                         .Where(connection => connection.targetNode is not FlowNode target || !IsRetiredLifecycleNode(target)))
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
            }
        }

        string TimelineReference(
            BtsmtlAuthoringCodeExportContext context,
            Clip clip)
        {
            if (clip is TimelineAnimationClip animation)
                return ExternalAsset(context, animation.Clip, typeof(UnityEngine.AnimationClip));
            if (clip is TreeClip tree)
            {
                if (tree.AssetTree is UnityEngine.Object asset)
                    return context.TryGetVariable(asset, out string variable)
                        ? variable
                        : ExternalAsset(context, asset, typeof(ScriptableObject));
                context.ReportError("timeline_tree_source_unsupported", clip.AuthoringId, "TreeClip不是正式资产节点图引用。");
            }
            return "null";
        }

        static bool NeedsClipSegmentOverride(Clip clip)
        {
            int defaultEndFrame = clip.StartFrame +
                (clip is TimelineAnimationClip animation && animation.Clip
                    ? Mathf.RoundToInt(animation.Clip.length * TimelineUtility.FrameRate)
                    : clip is SignalClip ? 1 : 3);
            return clip.EndFrame != defaultEndFrame ||
                clip.SelfEaseInFrame != 0 ||
                clip.SelfEaseOutFrame != 0 ||
                clip.ClipInFrame != 0;
        }

        static string CurveExpression(AnimationCurve curve, TimelineCurveChannelId channelId)
        {
            return CurveExpression(curve, TimelineCurveChannelCatalog.Require(channelId.Value));
        }

        static string CurveExpression(AnimationCurve curve, TimelineCurveChannelDescriptor descriptor)
        {
            if (IsDefaultCurve(curve, descriptor))
                return $"{TypeName(typeof(TimelineCurveChannelCatalog))}.Require({String(descriptor.ChannelId.Value)}).CreateDefaultCurve()";
            return BtsmtlAuthoringCodeValues.AnimationCurve(curve);
        }

        static string MotionWarpCurve(MotionWarpClip warp, bool position)
        {
            if (position && !warp.UsesPositionProgress || !position && !warp.UsesYawProgress)
                return "null";
            return CurveExpression(
                position ? warp.PositionProgressCurve : warp.YawProgressCurve,
                position ? TimelineCurveChannelCatalog.MotionWarpPositionProgress : TimelineCurveChannelCatalog.MotionWarpYawProgress);
        }

        static bool IsDefaultCurve(AnimationCurve curve, TimelineCurveChannelDescriptor descriptor)
        {
            if (curve == null || descriptor == null)
                return false;
            return CurvesEqual(curve, descriptor.CreateDefaultCurve());
        }

        static bool IsLinearCurve(AnimationCurve curve, float firstValue, float lastValue)
        {
            if (curve == null || curve.length != 2)
                return false;
            Keyframe[] keys = curve.keys;
            return keys[0].time == 0f && keys[0].value == firstValue &&
                keys[1].time == 1f && keys[1].value == lastValue &&
                keys[0].inTangent == 0f && keys[0].outTangent == 0f &&
                keys[1].inTangent == 0f && keys[1].outTangent == 0f &&
                keys[0].inWeight == 0f && keys[0].outWeight == 0f &&
                keys[1].inWeight == 0f && keys[1].outWeight == 0f &&
                keys[0].weightedMode == WeightedMode.None && keys[1].weightedMode == WeightedMode.None &&
                curve.preWrapMode == WrapMode.ClampForever && curve.postWrapMode == WrapMode.ClampForever;
        }

        static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
        {
            if (left == null || right == null || left.preWrapMode != right.preWrapMode ||
                left.postWrapMode != right.postWrapMode || left.length != right.length)
                return false;
            Keyframe[] leftKeys = left.keys;
            Keyframe[] rightKeys = right.keys;
            for (int i = 0; i < leftKeys.Length; i++)
            {
                Keyframe a = leftKeys[i];
                Keyframe b = rightKeys[i];
                if (a.time != b.time || a.value != b.value || a.inTangent != b.inTangent ||
                    a.outTangent != b.outTangent || a.inWeight != b.inWeight ||
                    a.outWeight != b.outWeight || a.weightedMode != b.weightedMode)
                    return false;
            }
            return true;
        }

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

        static bool IsRetiredLifecycleGraph(FlowGraph graph)
        {
            if (graph is not BtsmtlSkillFlowGraph)
                return false;
            string name = graph.name ?? string.Empty;
            return name.IndexOf("ActionExit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Action Exit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsRetiredLifecycleNode(FlowNode node)
        {
            if (node is BtsmtlSkillStateOnExitFlowNode ||
                node is BtsmtlSkillSubmitActionLifecycleFlowNode ||
                node is BtsmtlSkillSucceedFlowNode &&
                node.graph is IBtsmtlSkillFlowGraph { Role: BtsmtlSkillFlowGraphRole.StateBody })
                return true;
            return node is BtsmtlSkillSelectorFlowNode &&
                ((node.name ?? string.Empty).IndexOf("Action Exit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (node.name ?? string.Empty).IndexOf("ActionExit", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        string ActionContextExpression(
            BtsmtlAuthoringCodeExportContext context,
            ActionContextSlot actionContext)
        {
            if (context.Request.RecipeType.StartsWith("character.ability.", StringComparison.Ordinal))
                return "null";
            return ExternalAsset(context, actionContext, typeof(ActionContextSlot));
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

        static bool IsPrivateSubAsset(UnityEngine.Object child, UnityEngine.Object owner)
        {
            string childPath = AssetDatabase.GetAssetPath(child);
            string ownerPath = AssetDatabase.GetAssetPath(owner);
            return AssetDatabase.IsSubAsset(child) && !string.IsNullOrEmpty(childPath) && childPath == ownerPath;
        }

        static void RegisterObject(
            BtsmtlAuthoringCodeExportContext context,
            object source,
            string identity,
            string variableHint,
            bool isRoot = false)
        {
            if (source != null)
                context.RegisterObject(source, identity, variableHint, isRoot);
        }

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
