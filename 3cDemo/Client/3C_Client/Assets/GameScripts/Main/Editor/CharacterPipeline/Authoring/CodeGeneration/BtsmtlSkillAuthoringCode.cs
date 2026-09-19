using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Editor;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlSkillAuthoringCode
    {
        public static BtsmtlSkillFlowGraph EnsureAbilityRoot(
            BtsmtlAuthoringGenerationContext context,
            string abilityId,
            string graphIdentity,
            string name)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(abilityId) || string.IsNullOrWhiteSpace(graphIdentity))
                throw new ArgumentException("Gameplay Ability root identity is incomplete.");
            string outputPath = context.OutputAssetPath;
            GameplayAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(outputPath);
            if (!ability)
            {
                if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null)
                    throw new InvalidOperationException($"Gameplay Ability output '{outputPath}' is occupied by another asset type.");
                int separator = outputPath.LastIndexOf('/');
                string folder = separator > 0 ? outputPath.Substring(0, separator) : string.Empty;
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    throw new InvalidOperationException($"Gameplay Ability output folder '{folder}' does not exist.");
                ability = ScriptableObject.CreateInstance<GameplayAbilityDefinition>();
                ability.name = string.IsNullOrWhiteSpace(name) ? abilityId : name;
                ability.ConfigureIdentity(abilityId, name);
                AssetDatabase.CreateAsset(ability, outputPath);
            }
            else if (!string.Equals(ability.AbilityId, abilityId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Gameplay Ability output '{outputPath}' has identity '{ability.AbilityId}', expected '{abilityId}'.");
            }

            BtsmtlSkillFlowGraph graph = ability.AbilityGraph;
            if (graph == null)
            {
                graph = AssetDatabase.LoadAllAssetsAtPath(outputPath)
                    .OfType<BtsmtlSkillFlowGraph>()
                    .SingleOrDefault(value => string.Equals(value.AuthoringId, graphIdentity, StringComparison.Ordinal));
                if (graph != null)
                    ability.SetAbilityGraph(graph);
            }
            if (graph == null)
            {
                graph = BtsmtlSkillGraphAssetFactory.CreatePrivateAbilityGraph(ability, graphIdentity, name);
            }
            else if (!string.Equals(graph.AuthoringId, graphIdentity, StringComparison.Ordinal) ||
                     graph.Role != BtsmtlSkillFlowGraphRole.Skill)
            {
                throw new InvalidOperationException($"Gameplay Ability '{abilityId}' graph identity or role is invalid.");
            }
            EditorUtility.SetDirty(ability);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            GameplayAbilityDefinition persistedAbility =
                AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(outputPath);
            BtsmtlSkillFlowGraph persistedGraph = AssetDatabase.LoadAllAssetsAtPath(outputPath)
                .OfType<BtsmtlSkillFlowGraph>()
                .SingleOrDefault(value => string.Equals(value.AuthoringId, graphIdentity, StringComparison.Ordinal));
            if (!persistedAbility || !persistedGraph)
                throw new InvalidOperationException("Gameplay Ability root or private AbilityGraph was not persisted.");
            if (persistedAbility.AbilityGraph != persistedGraph)
            {
                persistedAbility.SetAbilityGraph(persistedGraph);
                EditorUtility.SetDirty(persistedAbility);
                AssetDatabase.SaveAssets();
            }
            return persistedGraph;
        }

        public static void BindAbilityRoot(
            BtsmtlAuthoringGenerationContext context,
            BtsmtlSkillFlowGraph graph)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            string outputPath = context.OutputAssetPath;
            GameplayAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(outputPath);
            if (!ability || ability.AbilityGraph != graph)
                throw new InvalidOperationException("Gameplay Ability root is not bound to its private Ability graph.");
            CharacterPipelineDefinition definition =
                context.ResolveExternalAsset<CharacterPipelineDefinition>(context.DefinitionAssetPath, 0L);
            if (!definition)
                throw new InvalidOperationException("Character Pipeline Definition is unavailable.");
            EditorUtility.SetDirty(ability);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(new[] { context.DefinitionAssetPath });
        }

        public static GameplayAbilityAdmissionProfile EnsureAdmissionProfileRoot(
            BtsmtlAuthoringGenerationContext context,
            string actionId,
            string displayName,
            string debugCategory,
            GameplayTagId[] tags,
            GameplayTagQuery requiredTags,
            GameplayTagQuery blockTags,
            GameplayTagQuery cancelTags,
            ActionTargetRequirement targetRequirement,
            int maxConcurrentInstances,
            string retiredAssetPath)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("Admission profile identity is required.", nameof(actionId));
            string outputPath = context.OutputAssetPath;
            GameplayAbilityAdmissionProfile profile =
                AssetDatabase.LoadAssetAtPath<GameplayAbilityAdmissionProfile>(outputPath);
            if (!profile)
            {
                if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null)
                    throw new InvalidOperationException($"Admission profile output '{outputPath}' is occupied by another asset type.");
                int separator = outputPath.LastIndexOf('/');
                string folder = separator > 0 ? outputPath.Substring(0, separator) : string.Empty;
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    throw new InvalidOperationException($"Admission profile output folder '{folder}' does not exist.");
                profile = ScriptableObject.CreateInstance<GameplayAbilityAdmissionProfile>();
                profile.name = string.IsNullOrWhiteSpace(displayName) ? actionId : displayName;
                AssetDatabase.CreateAsset(profile, outputPath);
            }
            else if (!string.Equals(profile.ActionId, actionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Admission profile output '{outputPath}' has identity '{profile.ActionId}', expected '{actionId}'.");
            }

            profile.ConfigureIdentity(actionId, displayName, debugCategory);
            profile.ConfigureGameplayTags(tags);
            profile.ConfigureRequiredTags(requiredTags?.All, requiredTags?.Any, requiredTags?.None);
            profile.ConfigureBlockTags(blockTags?.All, blockTags?.Any, blockTags?.None);
            profile.ConfigureCancelTags(cancelTags?.All, cancelTags?.Any, cancelTags?.None);
            profile.ConfigureTargetRequirement(targetRequirement);
            profile.ConfigureMaxConcurrentInstances(maxConcurrentInstances);
            EditorUtility.SetDirty(profile);
            if (!string.IsNullOrEmpty(retiredAssetPath) &&
                !string.Equals(retiredAssetPath, outputPath, StringComparison.Ordinal))
                context.DeleteAsset(retiredAssetPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameplayAbilityAdmissionProfile>(outputPath);
        }

        public static GameplayAbilityAdmissionProfile EnsureAdmissionProfileRoot(
            BtsmtlAuthoringGenerationContext context,
            string actionId,
            string displayName,
            string debugCategory,
            GameplayTagId[] tags,
            GameplayTagQuery requiredTags,
            GameplayTagQuery blockTags,
            GameplayTagQuery cancelTags,
            ActionTargetRequirement targetRequirement,
            int maxConcurrentInstances) =>
            EnsureAdmissionProfileRoot(context, actionId, displayName, debugCategory, tags, requiredTags, blockTags, cancelTags, targetRequirement, maxConcurrentInstances, null);

        public static void ConfigureAbility(
            BtsmtlAuthoringGenerationContext context,
            string debugCategory,
            GameplayTagId[] tags,
            GameplayAbilityAdmissionProfile admissionProfile,
            GameplayEffectDefinition[] effects,
            GameplayAbilityEndRule[] endRules,
            GameplayAbilitySubgraphDependencyConfiguration[] subgraphDependencies,
            string[] followUpAbilityIds,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetInputValueId,
            string targetKey,
            bool createGrant)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            GameplayAbilityDefinition ability =
                AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(context.OutputAssetPath);
            if (!ability)
                throw new InvalidOperationException("Gameplay Ability output asset is unavailable.");
            CharacterPipelineDefinition definition =
                context.ResolveExternalAsset<CharacterPipelineDefinition>(context.DefinitionAssetPath, 0L);
            if (!definition)
                throw new InvalidOperationException("Character Pipeline Definition is unavailable.");
            ability.ConfigureMetadata(debugCategory, tags);
            ability.ConfigureAdmissionProfile(admissionProfile);
            if (!definition.AdmissionProfiles.Contains(admissionProfile))
                definition.SetAdmissionProfiles(definition.AdmissionProfiles.Concat(new[] { admissionProfile }));
            ability.ConfigureEffects(effects);
            ability.ConfigureEndRules(endRules);
            ability.ConfigureSubgraphDependencies(subgraphDependencies);
            ability.ConfigureFollowUps(followUpAbilityIds);
            AbilityGrant grant = definition.AbilityGrants
                .SingleOrDefault(value => value != null && value.Ability == ability);
            if (grant == null)
            {
                if (!createGrant)
                {
                    EditorUtility.SetDirty(ability);
                    return;
                }
                grant = new AbilityGrant();
                definition.SetAbilityGrants(definition.AbilityGrants.Concat(new[] { grant }));
            }
            grant.Configure(
                ability,
                sourceInputRequestId,
                consumeSourceInputRequest,
                targetInputValueId,
                targetKey);
            EditorUtility.SetDirty(ability);
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
                BtsmtlSkillGraphAssetFactory.EnsureRequiredAnchors(existing);
                return existing;
            }
            return BtsmtlSkillFlowEditorMutation.Execute(owner, "创建技能子图", () =>
            {
                BtsmtlSkillFlowGraph graph = BtsmtlSkillGraphAssetFactory.CreatePrivatePage(owner, role, name);
                graph.ConfigureIdentity(identity, role);
                BtsmtlSkillGraphAssetFactory.EnsureRequiredAnchors(graph);
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
            {
                existing.Data.Name = name ?? existing.Data.Name;
                existing.Data.Loop = false;
                return existing;
            }
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
                existing.Data.Loop = false;
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

        public static BtsmtlSkillFlowGraph EnsureTimelineGraph(
            TimelineAsset owner,
            string identity,
            string name,
            BtsmtlSkillFlowGraphRole role = BtsmtlSkillFlowGraphRole.TimelineBody)
        {
            return TimelineGraphAuthoring.EnsureSubAsset(owner, identity, name, role);
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
            FlowNode node = BtsmtlSkillFlowGraphAuthoring.EnsureNode(graph, nodeType, identity, name, position);
            ResetNodeDefaults(node);
            return node;
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

        public static void ConfigureNativeState(
            BtsmtlSkillNativeState state,
            BtsmtlSkillFlowGraph body) =>
            BtsmtlSkillNativeStateMachineAuthoring.ConfigureState(state, BtsmtlSkillFlowGraphAuthoring.ReadNodeName(state), state.position, body);

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
            string name,
            TimelineExecutionDomain executionDomain = TimelineExecutionDomain.Logic)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            Track expected = Activator.CreateInstance(trackType) as Track;
            TimelineTrackContract contract = catalog.RequireTrack(expected?.ContractKind);
            if (!contract.SupportsExecutionDomain(executionDomain))
                throw new InvalidOperationException($"Timeline track '{contract.Kind}' does not support execution domain '{executionDomain}'.");
            Track existing = timeline.Tracks.SingleOrDefault(value => value != null && value.AuthoringId == identity);
            if (existing != null)
            {
                if (existing.GetType() != trackType)
                    throw new InvalidOperationException($"Timeline track identity '{identity}' has a different type.");
                existing.Name = name ?? existing.Name;
                existing.PersistentMuted = false;
                existing.ConfigureExecutionDomain(executionDomain);
                return existing;
            }
            Track created = null;
            PrepareTimelineMutation(timeline);
            timeline.ApplyModify(() =>
            {
                int count = timeline.Tracks.Count;
                timeline.AddTrack(trackType, catalog);
                created = timeline.Tracks[count];
                created.ConfigureAuthoringIdentity(identity);
                created.Name = name ?? created.Name;
                created.ConfigureExecutionDomain(executionDomain);
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
                ConfigureClipSegment(existing, startFrame, DefaultEndFrame(existing, startFrame, referenceObject), 0, 0, 0);
                ConfigureClipExecution(track, existing);
                return existing;
            }
            Clip created = null;
            PrepareTimelineMutation(timeline);
            timeline.ApplyModify(() =>
            {
                created = referenceObject
                    ? timeline.AddClip(catalog, referenceObject, track, startFrame)
                    : timeline.AddClip(catalog, track, startFrame);
                created.ConfigureAuthoringIdentity(identity);
                ConfigureClipExecution(track, created);
                timeline.Init();
            }, "生成Timeline片段");
            return created;
        }

        public static void EnsureMarker(TimelineData timeline, Track track, string identity, int frame, ScriptableObject graph)
        {
            PrepareTimelineMutation(timeline);
            timeline.ApplyModify(() =>
            {
                TimelineMarker marker = track.Markers.SingleOrDefault(value => value.AuthoringId == identity);
                if (marker == null)
                {
                    marker = track.AddMarker(frame, graph);
                    marker.ConfigureAuthoringIdentity(identity);
                }
                else
                    marker.Configure(frame, graph);
            }, "生成Timeline触发点");
        }

        public static void PruneTimelineMarkers(TimelineData timeline, IEnumerable<string> markerIdentities)
        {
            var identities = new HashSet<string>(markerIdentities, StringComparer.Ordinal);
            PrepareTimelineMutation(timeline);
            timeline.ApplyModify(() =>
            {
                var previous = BtsmtlSkillOwnedAssets.Collect(timeline.SerializedOwner);
                foreach (Track track in timeline.Tracks)
                    foreach (TimelineMarker marker in track.Markers.ToArray())
                        if (!identities.Contains(marker.AuthoringId))
                            track.RemoveMarker(marker);
                BtsmtlSkillOwnedAssets.ReleaseUnreferenced(timeline.SerializedOwner, previous);
            }, "清理Timeline触发点");
        }

        static void ConfigureClipExecution(Track track, Clip clip)
        {
            clip.InheritExecutionDomain();
            if (clip is TreeClip tree)
                tree.SetExitSource(track.ExecutionDomain == TimelineExecutionDomain.Logic
                    ? TimelineClipExitSource.TreeDecision
                    : TimelineClipExitSource.FrameBoundary);
        }

        public static Clip EnsureClip(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            string identity,
            int startFrame,
            UnityEngine.Object referenceObject,
            int endFrame,
            int selfEaseInFrame,
            int selfEaseOutFrame,
            int clipInFrame)
        {
            Clip clip = EnsureClip(timeline, catalog, track, identity, startFrame, referenceObject);
            ConfigureClipSegment(clip, startFrame, endFrame, selfEaseInFrame, selfEaseOutFrame, clipInFrame);
            return clip;
        }

        public static void ConfigureClipSegment(
            Clip clip,
            int startFrame,
            int endFrame,
            int selfEaseInFrame,
            int selfEaseOutFrame,
            int clipInFrame)
        {
            clip.StartFrame = startFrame;
            clip.EndFrame = endFrame;
            clip.SelfEaseInFrame = selfEaseInFrame;
            clip.SelfEaseOutFrame = selfEaseOutFrame;
            clip.ClipInFrame = clipInFrame;
            clip.FrameToTime();
            clip.Track?.UpdateMix();
        }

        public static void ConfigureMotionCurveSource(
            MotionCurveClip clip,
            RootMotionCurveAsset source,
            float sourceStartTime,
            float sourceEndTime) =>
            clip.ConfigureSource(source, sourceStartTime, sourceEndTime);

        static int DefaultEndFrame(Clip clip, int startFrame, UnityEngine.Object referenceObject)
        {
            if (referenceObject is UnityEngine.AnimationClip animation)
                return startFrame + Mathf.RoundToInt(animation.length * TimelineUtility.FrameRate);
            return startFrame + (clip is SignalClip ? 1 : 3);
        }

        static void ResetNodeDefaults(FlowNode node)
        {
            if (node is BtsmtlSkillLoopFlowNode loop)
                loop.SetStopType(BtsmtlSkillLoopStopType.None);
            if (node is BtsmtlSkillParallelFlowNode parallel)
                parallel.SetMode(BtsmtlSkillParallelMode.JumpComplete);
            if (node is BtsmtlSkillStateExitCauseFlowNode cause)
                cause.SetCause(BtsmtlSkillStateExitCause.StateTransition);
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
                locomotion.Configure(
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultMoveSpeed,
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultDisplacementMode,
                    null,
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultTurnSpeedDegrees,
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultCameraRelative,
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultExecutionMode,
                    ThirdPersonCharacter.Pipeline.Motion.LocomotionInputMotionAuthoringRules.DefaultDurationSeconds);
        }

        public static TimelineSection EnsureSection(
            TimelineData timeline,
            string identity,
            string name,
            int frame,
            string nextSectionId)
        {
            TimelineSection result = null;
            PrepareTimelineMutation(timeline);
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
            PrepareTimelineMutation(timeline);
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

        public static void PruneBlackboard(FlowGraph graph, IEnumerable<string> variableIdentities)
        {
            if (graph is not BtsmtlSkillFlowGraph skillGraph)
                throw new InvalidOperationException("技能黑板清理必须作用于正式Skill Graph。");
            var keep = new HashSet<string>(variableIdentities ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var variables = skillGraph.blackboard.variables.Values
                .Where(value => value != null && !keep.Contains(value.ID))
                .ToArray();
            foreach (Variable variable in variables)
                if (BtsmtlSkillGraphClosure.Validate(skillGraph, false)
                    .SelectMany(value => value.allNodes)
                    .OfType<IBtsmtlSkillBlackboardAccessNode>()
                    .Any(node => node.Variable.DeclarationId == variable.ID &&
                        string.Equals(node.Variable.OwnerId, skillGraph.AuthoringId, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"技能黑板变量'{variable.name}'仍被节点引用。");
            BtsmtlSkillFlowEditorMutation.Apply(
                skillGraph,
                "清理技能黑板声明",
                () =>
                {
                    foreach (Variable variable in variables)
                        skillGraph.blackboard.RemoveVariable(variable.name);
                    skillGraph.SetBlackboardDeclarations(skillGraph.BlackboardDeclarations
                        .Where(declaration => keep.Contains(declaration.VariableId))
                        .ToArray());
                },
                false);
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
            PrepareTimelineMutation(timeline);
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

        static void PrepareTimelineMutation(TimelineData timeline)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            timeline.UpdateSerializedTimeline();
        }
    }
}
