using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlSkillAuthoringCode
    {
        public static BtsmtlSkillFlowGraph EnsureSkillRoot(
            BtsmtlAuthoringGenerationContext context,
            string identity,
            string name)
        {
            CharacterPipelineDefinition definition =
                context.ResolveExternalAsset<CharacterPipelineDefinition>(context.DefinitionAssetPath, 0L);
            if (!definition)
                throw new InvalidOperationException("Character Pipeline Definition is unavailable.");
            string outputPath = context.OutputAssetPath;
            BtsmtlSkillFlowGraph graph = AssetDatabase.LoadAssetAtPath<BtsmtlSkillFlowGraph>(outputPath);
            if (graph)
            {
                if (!string.Equals(graph.AuthoringId, identity, StringComparison.Ordinal) ||
                    graph.Role != BtsmtlSkillFlowGraphRole.Skill)
                    throw new InvalidOperationException($"Skill output '{outputPath}' has a different identity or role.");
            }
            else
            {
                if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null)
                    throw new InvalidOperationException($"Skill output '{outputPath}' is occupied by another asset type.");
                int separator = outputPath.LastIndexOf('/');
                string folder = separator > 0 ? outputPath.Substring(0, separator) : string.Empty;
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    throw new InvalidOperationException($"Skill output folder '{folder}' does not exist.");
                BtsmtlSkillFlowGraph existing = definition.SkillGraphs.SingleOrDefault(value =>
                    value && string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
                if (existing)
                    throw new InvalidOperationException($"Skill identity '{identity}' already belongs to '{AssetDatabase.GetAssetPath(existing)}'.");
                graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                graph.name = string.IsNullOrWhiteSpace(name) ? identity : name;
                graph.ConfigureIdentity(identity, BtsmtlSkillFlowGraphRole.Skill);
                AssetDatabase.CreateAsset(graph, outputPath);
                BtsmtlSkillFlowEditorMutation.Apply(
                    graph,
                    "创建技能根图",
                    () => BtsmtlSkillGraphAssetFactory.PopulateAnchors(graph),
                    false);
            }
            if (!definition.SkillGraphs.Contains(graph))
            {
                definition.SetSkillGraphs(definition.SkillGraphs.Concat(new[] { graph }).ToArray());
                EditorUtility.SetDirty(definition);
            }
            EditorUtility.SetDirty(graph);
            return graph;
        }

        public static void BindSkillRoot(
            BtsmtlAuthoringGenerationContext context,
            BtsmtlSkillFlowGraph graph)
        {
            CharacterPipelineDefinition definition =
                context.ResolveExternalAsset<CharacterPipelineDefinition>(context.DefinitionAssetPath, 0L);
            if (!definition || graph == null)
                throw new InvalidOperationException("Skill root binding requires a Definition and graph.");
            if (definition.SkillGraphs.Contains(graph))
                return;
            definition.SetSkillGraphs(definition.SkillGraphs.Concat(new[] { graph }).ToArray());
            EditorUtility.SetDirty(definition);
        }

        public static BtsmtlSkillFlowGraph EnsureChildGraph(
            FlowGraph owner,
            string identity,
            BtsmtlSkillFlowGraphRole role,
            string name)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            string path = AssetDatabase.GetAssetPath(owner);
            BtsmtlSkillFlowGraph existing = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<BtsmtlSkillFlowGraph>()
                .SingleOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            if (existing)
            {
                if (existing.Role != role)
                    throw new InvalidOperationException($"Skill child graph identity '{identity}' has a different role.");
                return existing;
            }
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能子图", () =>
            {
                BtsmtlSkillFlowGraph graph = BtsmtlSkillGraphAssetFactory.CreatePrivatePage(owner, role, name);
                graph.ConfigureIdentity(identity, role);
                EditorUtility.SetDirty(graph);
                return graph;
            });
        }

        public static BtsmtlSkillMacroGraph EnsureMacro(
            FlowGraph owner,
            string identity,
            string name)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            string path = AssetDatabase.GetAssetPath(owner);
            BtsmtlSkillMacroGraph existing = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<BtsmtlSkillMacroGraph>()
                .SingleOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            if (existing)
                return existing;
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能Macro", () =>
            {
                BtsmtlSkillMacroGraph graph = BtsmtlSkillGraphAssetFactory.CreatePrivateMacro(owner, name);
                graph.ConfigureIdentity(identity);
                EditorUtility.SetDirty(graph);
                return graph;
            });
        }

        public static BtsmtlSkillNativeStateMachine EnsureStateMachine(
            FlowGraph owner,
            string identity,
            string name,
            string ownerGraphId,
            string ownerNodeId)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            string path = AssetDatabase.GetAssetPath(owner);
            BtsmtlSkillNativeStateMachine existing = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<BtsmtlSkillNativeStateMachine>()
                .SingleOrDefault(value => string.Equals(value.AuthoringId, identity, StringComparison.Ordinal));
            if (existing)
            {
                BtsmtlSkillNativeStateMachineAuthoring.RequireOwner(existing, ownerGraphId, ownerNodeId);
                return existing;
            }
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能状态机", () =>
            {
                BtsmtlSkillNativeStateMachine machine =
                    BtsmtlSkillGraphAssetFactory.CreatePrivateStateMachine(
                        owner,
                        name,
                        ownerGraphId,
                        ownerNodeId);
                machine.ConfigureIdentity(identity);
                machine.ConfigureOwner(ownerGraphId, ownerNodeId);
                EditorUtility.SetDirty(machine);
                return machine;
            });
        }

        public static TimelineAsset EnsureTimeline(
            FlowGraph owner,
            string identity,
            string name)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            string path = AssetDatabase.GetAssetPath(owner);
            TimelineAsset existing = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<TimelineAsset>()
                .SingleOrDefault(value =>
                    value.Data != null && string.Equals(value.Data.AuthoringId, identity, StringComparison.Ordinal));
            if (existing)
                return existing;
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能Timeline", () =>
            {
                TimelineAsset asset = BtsmtlSkillGraphAssetFactory.CreatePrivateTimeline(owner, name);
                asset.Data.ConfigureAuthoringIdentity(identity);
                EditorUtility.SetDirty(asset);
                return asset;
            });
        }

        public static TimelineAsset EnsureTimelineRoot(
            BtsmtlAuthoringGenerationContext context,
            string identity,
            string name)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            string path = context.OutputAssetPath;
            TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (existing)
            {
                if (existing.Data != null &&
                    !string.IsNullOrEmpty(existing.Data.AuthoringId) &&
                    !string.Equals(existing.Data.AuthoringId, identity, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Timeline output '{path}' has a different authoring identity.");
                if (existing.Data == null)
                    existing.SetData(TimelineData.CreateDefault(name));
                existing.Data.ConfigureAuthoringIdentity(identity);
                existing.Data.Name = name;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException($"Timeline output '{path}' is occupied by another asset type.");
            int separator = path.LastIndexOf('/');
            string folder = separator > 0 ? path.Substring(0, separator) : string.Empty;
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException($"Timeline output folder '{folder}' does not exist.");
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            asset.name = string.IsNullOrWhiteSpace(name) ? identity : name;
            AssetDatabase.CreateAsset(asset, path);
            asset.SetData(TimelineData.CreateDefault(asset.name));
            asset.Data.ConfigureAuthoringIdentity(identity);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static FlowNode EnsureFlowNode(
            FlowGraph graph,
            Type nodeType,
            string identity,
            string name,
            Vector2 position)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            return BtsmtlSkillFlowGraphAuthoring.EnsureNode(graph, nodeType, identity, name, position);
        }

        public static BtsmtlSkillNativeState EnsureNativeState(
            BtsmtlSkillNativeStateMachine machine,
            Type nodeType,
            string identity,
            string name,
            Vector2 position)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            return BtsmtlSkillNativeStateMachineAuthoring.EnsureState(
                machine,
                nodeType,
                identity,
                name,
                position);
        }

        public static void ConfigureNativeState(
            BtsmtlSkillNativeState state,
            string name,
            Vector2 position,
            BtsmtlSkillFlowGraph body) =>
            BtsmtlSkillNativeStateMachineAuthoring.ConfigureState(state, name, position, body);

        public static void ConfigureNativeConnection(
            BtsmtlSkillNativeConnection connection,
            BtsmtlSkillFlowGraph condition,
            int priority,
            ProgramAbortPolicy abortPolicy,
            int order) =>
            BtsmtlSkillNativeStateMachineAuthoring.ConfigureConnection(
                connection,
                condition,
                priority,
                abortPolicy,
                order);

        public static BinderConnection EnsureFlowConnection(
            FlowGraph graph,
            FlowNode source,
            string sourcePortId,
            FlowNode target,
            string targetPortId,
            string identity)
        {
            if (graph == null || source == null || target == null)
                throw new ArgumentNullException(nameof(graph));
            return BtsmtlSkillFlowGraphAuthoring.EnsureConnection(
                graph,
                source,
                sourcePortId,
                target,
                targetPortId,
                identity);
        }

        public static BtsmtlSkillNativeConnection EnsureNativeConnection(
            BtsmtlSkillNativeStateMachine machine,
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target,
            string identity)
        {
            if (machine == null || source == null || target == null)
                throw new ArgumentNullException(nameof(machine));
            return BtsmtlSkillNativeStateMachineAuthoring.EnsureConnection(
                machine,
                source,
                target,
                identity);
        }

        public static void SetValue(FlowNode node, string portId, object value)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (node.GetInputPort(portId) is not ValueInput input)
                throw new InvalidOperationException($"Skill value port '{portId}' is missing.");
            if (input.type != null && value != null && !input.type.IsInstanceOfType(value))
                throw new InvalidOperationException($"Skill value port '{portId}' type does not match.");
            BtsmtlSkillFlowEditorMutation.Execute((FlowGraph)node.graph, "设置技能输入值", () => input.serializedValue = value);
        }

        public static Variable EnsureBlackboardDeclaration(
            FlowGraph graph,
            string variableId,
            string name,
            Type type,
            object value,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            string category,
            PipelineBlackboardInputBinding inputBinding,
            PipelineBlackboardFactProjection factProjection)
        {
            Variable existing = graph.GetGraphSource().localBlackboard.variables.Values
                .SingleOrDefault(variable => variable != null && variable.ID == variableId);
            if (existing == null)
                return BtsmtlSkillBlackboardDeclarations.Add(
                    graph,
                    new BtsmtlSkillBlackboardDeclaration(
                        variableId,
                        scope,
                        lifetime,
                        category,
                        inputBinding,
                        factProjection),
                    name,
                    type,
                    value);
            if (existing.name != name || existing.varType != type || existing is not ISerializedVariableValue)
                throw new InvalidOperationException($"Skill Blackboard declaration '{variableId}' has a different type or name.");
            BtsmtlSkillFlowEditorMutation.Execute(graph, "设置技能黑板值", () => existing.SetValueBoxed(value));
            return existing;
        }

        public static Track EnsureTrack(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Type trackType,
            string identity,
            string name)
        {
            Track existing = timeline.Tracks.SingleOrDefault(value => value != null && value.AuthoringId == identity);
            if (existing != null)
            {
                if (existing.GetType() != trackType)
                    throw new InvalidOperationException($"Timeline track identity '{identity}' has a different type.");
                existing.Name = name ?? existing.Name;
                return existing;
            }
            Track created = null;
            timeline.ApplyModify(() =>
            {
                int count = timeline.Tracks.Count;
                timeline.AddTrack(trackType, catalog);
                created = timeline.Tracks[count];
                created.ConfigureAuthoringIdentity(identity);
                created.Name = name ?? created.Name;
                timeline.Init();
            }, "生成Timeline轨道");
            return created;
        }

        public static Clip EnsureClip(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            string identity,
            int startFrame,
            UnityEngine.Object referenceObject)
        {
            Clip existing = track.Clips.SingleOrDefault(value => value != null && value.AuthoringId == identity);
            if (existing != null)
            {
                ConfigureClipFrames(existing, startFrame, existing.EndFrame, existing.OtherEaseInFrame,
                    existing.OtherEaseOutFrame, existing.SelfEaseInFrame, existing.SelfEaseOutFrame, existing.ClipInFrame);
                return existing;
            }
            Clip created = null;
            timeline.ApplyModify(() =>
            {
                created = referenceObject
                    ? timeline.AddClip(catalog, referenceObject, track, startFrame)
                    : timeline.AddClip(catalog, track, startFrame);
                created.ConfigureAuthoringIdentity(identity);
                timeline.Init();
            }, "生成Timeline片段");
            return created;
        }

        public static void ConfigureClipFrames(
            Clip clip,
            int startFrame,
            int endFrame,
            int otherEaseInFrame,
            int otherEaseOutFrame,
            int selfEaseInFrame,
            int selfEaseOutFrame,
            int clipInFrame)
        {
            clip.StartFrame = startFrame;
            clip.EndFrame = endFrame;
            clip.OtherEaseInFrame = otherEaseInFrame;
            clip.OtherEaseOutFrame = otherEaseOutFrame;
            clip.SelfEaseInFrame = selfEaseInFrame;
            clip.SelfEaseOutFrame = selfEaseOutFrame;
            clip.ClipInFrame = clipInFrame;
            clip.FrameToTime();
        }

        public static TimelineSection EnsureSection(
            TimelineData timeline,
            string identity,
            string name,
            int frame,
            string nextSectionId)
        {
            TimelineSection result = null;
            timeline.ApplyModify(() =>
            {
                result = timeline.EnsureSection(identity, name, frame);
                timeline.ConfigureSectionNext(result, nextSectionId);
            }, "生成Timeline段");
            return result;
        }

        public static TimelineExternalBindingDeclaration EnsureExternalBinding(
            TimelineData timeline,
            string identity,
            string bindingId,
            string displayName,
            string domain,
            TimelineBindingValueKind valueKind,
            TimelineBindingAccess access,
            TimelineBindingLifetime lifetime,
            string parameterId)
        {
            TimelineExternalBindingDeclaration result = null;
            timeline.ApplyModify(() =>
            {
                result = timeline.ExternalBindings.SingleOrDefault(value => value != null && value.AuthoringId == identity);
                if (result == null)
                {
                    result = timeline.AddExternalBinding(
                        bindingId,
                        displayName,
                        domain,
                        valueKind,
                        access,
                        lifetime,
                        parameterId);
                    result.ConfigureAuthoringIdentity(identity);
                }
                else
                {
                    result.Configure(bindingId, displayName, domain, valueKind, access, lifetime, parameterId);
                }
            }, "生成Timeline外部绑定");
            return result;
        }

        public static void PruneFlowGraph(
            FlowGraph graph,
            IEnumerable<string> nodeIdentities,
            IEnumerable<string> connectionIdentities)
        {
            BtsmtlSkillFlowGraphAuthoring.Prune(graph, nodeIdentities, connectionIdentities);
        }

        public static void PruneNativeStateMachine(
            BtsmtlSkillNativeStateMachine machine,
            IEnumerable<string> stateIdentities,
            IEnumerable<string> connectionIdentities)
        {
            BtsmtlSkillNativeStateMachineAuthoring.PruneConnections(machine, connectionIdentities);
            var keep = new HashSet<string>(stateIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            BtsmtlSkillFlowEditorMutation.Execute(machine, "清理技能FSM状态", () =>
            {
                foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>().ToArray())
                    if (state is not BtsmtlSkillNativeEntryState && state is not BtsmtlSkillNativeAnyState &&
                        state is not BtsmtlSkillNativeExitState && !keep.Contains(state.UID))
                        machine.RemoveNode(state, false);
            });
        }

        public static void PruneTimeline(
            TimelineData timeline,
            IEnumerable<string> trackIdentities,
            IEnumerable<string> clipIdentities,
            IEnumerable<string> sectionIdentities,
            IEnumerable<string> bindingIdentities)
        {
            var tracks = new HashSet<string>(trackIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var clips = new HashSet<string>(clipIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var sections = new HashSet<string>(sectionIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var bindings = new HashSet<string>(bindingIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            timeline.ApplyModify(() =>
            {
                foreach (Track track in timeline.Tracks.ToArray())
                {
                    if (!tracks.Contains(track.AuthoringId))
                    {
                        timeline.RemoveTrack(track);
                        continue;
                    }
                    foreach (Clip clip in track.Clips.ToArray())
                        if (!clips.Contains(clip.AuthoringId))
                            track.RemoveClip(clip);
                }
                foreach (TimelineSection section in timeline.Sections.ToArray())
                    if (!sections.Contains(section.AuthoringId))
                        timeline.RemoveSection(section);
                foreach (TimelineExternalBindingDeclaration binding in timeline.ExternalBindings.ToArray())
                    if (!bindings.Contains(binding.AuthoringId))
                        timeline.RemoveExternalBinding(binding);
                timeline.Init();
            }, "清理Timeline输出");
        }
    }
}
