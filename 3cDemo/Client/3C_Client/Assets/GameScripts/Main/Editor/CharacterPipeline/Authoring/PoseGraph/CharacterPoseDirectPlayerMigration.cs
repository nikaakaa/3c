using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseDirectPlayerMigration
    {
        const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string ActionBlendProfilePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Blend/Action/CorinActionBlendProfile.asset";
        const string LocomotionBlendProfilePath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Blend/Locomotion/CorinLocomotionBlendProfile.asset";
        const string AttackTimelinePath = "Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset";

        [MenuItem("Tools/3C/Pose/Migrate Corin to UE Authoring")]
        static void MigrateCorinToUeAuthoring()
        {
            MigrateCorin();
            MigrateCorinBodyChain();
            CleanCorinControlRigGraphDuplicates();
            CleanCorinRootParameterResolver();
            CleanCorinRootActionPlaybackInput();
            MigrateCorinControlRigGoalAssembly();
            MigrateCorinActionTimelineSlot();
        }

        static void MigrateCorin()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph ||
                !definition.AnimationPresentationProfile.RigDefinition)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterAnimationPresentationProfile profile = definition.AnimationPresentationProfile;
            CharacterPresentationPoseGraphAsset asset = profile.PoseGraph;
            CharacterAnimationRigDefinition rig = profile.RigDefinition;
            CharacterAnimationBlendProfile actionProfile =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendProfile>(ActionBlendProfilePath);
            CharacterAnimationBlendProfile locomotionProfile =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendProfile>(LocomotionBlendProfilePath);
            if (!actionProfile || !locomotionProfile)
                throw new InvalidOperationException("Corin Blend Profile assets are incomplete.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Corin Pose Players");
            var owners = new List<UnityEngine.Object> { asset, rig, profile };
            owners.AddRange(asset.EnumerateGraphs().Where(value => value));
            Undo.RegisterCompleteObjectUndo(owners.ToArray(), "Migrate Corin Pose Players");
            try
            {
                foreach (CharacterPoseCanvasGraph graph in asset.EnumerateGraphs().Where(value => value))
                {
                    CharacterPoseCanvasNode[] nodes = graph.Nodes.Select(node =>
                    {
                        if (node.Payload is not CharacterClipPlayerPosePayload oldPlayer ||
                            !oldPlayer.SourceSlot)
                            return node;
                        CharacterPresentationPoseSourceBinding binding =
                            profile.FindPoseSourceBinding(oldPlayer.SourceSlot);
                        if (binding is not CharacterClipPoseSourceBinding clipBinding || !clipBinding.Clip)
                            throw new InvalidOperationException($"Pose Player '{node.NodeId}' Source Slot '{oldPlayer.SourceSlot.name}' is not a Clip binding.");
                        return new CharacterPoseCanvasNode(
                            node.NodeId,
                            node.DisplayName,
                            new CharacterClipPlayerPosePayload(
                                clipBinding.Clip,
                                oldPlayer.PlayRate,
                                oldPlayer.InitialTime,
                                clipBinding.Clip.isLooping,
                                oldPlayer.ClockSource),
                            node.DynamicPorts.ToArray(),
                            node.position);
                    }).ToArray();
                    CharacterPoseCanvasGraph candidate = CharacterPoseCanvasGraph.CreateAuthoring(
                        graph.GraphId,
                        Guid.NewGuid().ToString("N"),
                        graph.Parameters.ToArray(),
                        nodes,
                        graph.Connections.ToArray(),
                        graph.Layout,
                        graph.Role);
                    graph.ApplyAuthoringState(candidate);
                    UnityEngine.Object.DestroyImmediate(candidate);
                }
                rig.SetAnimationSlots(new[]
                {
                    new CharacterAnimationSlotDefinition(
                        new AnimationSlotId("corin.full-body-action"),
                        new AnimationSlotGroupId("corin.full-body-action"),
                        "Full Body Action")
                });
                rig.SetBlendProfiles(new[] { actionProfile, locomotionProfile });
                CharacterPresentationPoseSourceBinding[] bindings =
                    profile.PoseSourceBindings.ToArray();
                CharacterPresentationPoseSourceSlot[] slots =
                    asset.SourceSlots.ToArray();
                if (asset.EnumerateGraphs()
                    .SelectMany(value => value.Nodes)
                    .Any(value => value.PresentationPoseSourceSlot))
                    throw new InvalidOperationException(
                        "Corin Pose Graph still contains a legacy Source Slot consumer.");
                profile.SetPoseSourceBindings(Array.Empty<CharacterPresentationPoseSourceBinding>());
                asset.SetSourceSlots(Array.Empty<CharacterPresentationPoseSourceSlot>());
                for (int i = 0; i < bindings.Length; i++)
                    if (bindings[i])
                        Undo.DestroyObjectImmediate(bindings[i]);
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i])
                        Undo.DestroyObjectImmediate(slots[i]);
                EditorUtility.SetDirty(asset);
                EditorUtility.SetDirty(rig);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void MigrateCorinActionTimelineSlot()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.RigDefinition)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterAnimationRigDefinition rig = definition.AnimationPresentationProfile.RigDefinition;
            rig.RequireAnimationSlot(new AnimationSlotId("corin.full-body-action"));
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AttackTimelinePath);
            if (!timeline || timeline.Data == null)
                throw new InvalidOperationException("Corin Attack Timeline is missing.");
            AnimationTrack track = timeline.Data.Tracks
                .OfType<AnimationTrack>()
                .Single(value => string.Equals(value.AnimationChannelId.Value, "FullBodyAction", StringComparison.Ordinal));
            if (track.Clips.OfType<BTSMTL.Timeline.AnimationClip>().Any(value => value == null || !value.Clip))
                throw new InvalidOperationException("Corin Attack Timeline contains an invalid animation clip.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Corin Action Timeline Slot");
            Undo.RegisterCompleteObjectUndo(timeline, "Migrate Corin Action Timeline Slot");
            try
            {
                track.SetAnimationSlotId("corin.full-body-action");
                foreach (BTSMTL.Timeline.AnimationClip clip in track.Clips.OfType<BTSMTL.Timeline.AnimationClip>())
                    clip.BlendProfileId = "corin.animation-rig.action-blend-profile";
                if (timeline.Data.Sections.Count == 0)
                {
                    TimelineSection section = timeline.Data.AddSection("Attack", 0);
                    timeline.Data.ConfigureSectionNext(section, string.Empty);
                }
                timeline.Data.Init();
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void MigrateCorinBodyChain()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterPresentationPoseGraphAsset asset =
                definition.AnimationPresentationProfile.PoseGraph;
            const string controlRigGraphId = "corin.control-rig.body.graph";
            if (asset.TryGetGraph(new PoseGraphId(controlRigGraphId), out _))
                throw new InvalidOperationException($"Corin Control Rig graph '{controlRigGraphId}' already exists.");

            CharacterPoseCanvasGraph root = asset.Graph;
            CharacterPoseCanvasNode stateMachine = RequireNode(
                root,
                "corin.locomotion.pose-state-machine");
            CharacterPoseCanvasNode slot = RequireNode(
                root,
                "corin.full-body-action.slot");
            CharacterPoseCanvasNode parameter = RequireNode(
                root,
                "fd84c35ae6ed49a1b72703b92795c43c");
            CharacterPoseCanvasNode output = root.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.OutputPose);
            CharacterPoseCanvasNode localToComponent = RequireNode(
                root,
                "corin.pose.local-to-component");
            CharacterPoseCanvasNode footPlacement = RequireNode(
                root,
                "cc0cb7f36cb9426d88f6ae03f255a013");
            CharacterPoseCanvasNode goalAssembler = RequireNode(
                root,
                "corin.pose.full-body-ik-goal-assembler");
            CharacterPoseCanvasNode fullBodyIk = RequireNode(
                root,
                "1acecc122c13d615cc425fa3cc8a70d0");
            CharacterPoseCanvasNode componentToLocal = RequireNode(
                root,
                "corin.pose.component-to-local");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Corin Body Chain to Control Rig");
            var owners = new List<UnityEngine.Object> { asset, root };
            owners.AddRange(asset.EnumerateGraphs().Where(value => value));
            Undo.RegisterCompleteObjectUndo(owners.ToArray(), "Migrate Corin Body Chain to Control Rig");
            try
            {
                PoseInterfacePortId inputPoseInterface = new PoseInterfacePortId("entry.pose.input");
                PoseInterfacePortId outputPoseInterface = new PoseInterfacePortId("entry.pose.output");
                PoseInterfacePortId footWeightInterface = new PoseInterfacePortId("foot-placement-weight");
                var graphInput = new CharacterPoseCanvasNode(
                    new PoseNodeId(controlRigGraphId + "/input"),
                    "Control Rig Input",
                    new CharacterGraphInputPosePayload(),
                    new[]
                    {
                        new CharacterPoseDynamicPort(
                            new PosePortId("pose.local"),
                            "Input Pose",
                            CharacterPosePortKind.LocalPose,
                            CharacterPosePortDirection.Output,
                            true,
                            0,
                            inputPoseInterface),
                        new CharacterPoseDynamicPort(
                            new PosePortId("foot-weight"),
                            "Foot Placement Weight",
                            CharacterPosePortKind.Parameter,
                            CharacterPosePortDirection.Output,
                            true,
                            1,
                            footWeightInterface)
                    },
                    new Vector2(-700f, 0f));
                var graphOutput = new CharacterPoseCanvasNode(
                    new PoseNodeId(controlRigGraphId + "/output"),
                    "Control Rig Output",
                    new CharacterGraphOutputPosePayload(),
                    new[]
                    {
                        new CharacterPoseDynamicPort(
                            new PosePortId("pose.local"),
                            "Output Pose",
                            CharacterPosePortKind.LocalPose,
                            CharacterPosePortDirection.Input,
                            true,
                            0,
                            outputPoseInterface)
                    },
                    new Vector2(700f, 0f));
                CharacterPoseCanvasNode[] rigNodes =
                {
                    graphInput,
                    CloneNode(localToComponent, controlRigGraphId + "/local-to-component", new Vector2(-500f, 0f)),
                    CloneNode(footPlacement, controlRigGraphId + "/foot-placement", new Vector2(-250f, -180f)),
                    CloneNodeWithDynamicPort(
                        fullBodyIk,
                        controlRigGraphId + "/full-body-ik",
                        new CharacterPoseDynamicPort(
                            new PosePortId("goal-contribution.0"),
                            "Goal Contribution",
                            CharacterPosePortKind.FullBodyIkGoalContribution,
                            CharacterPosePortDirection.Input,
                            true,
                            0),
                        new Vector2(250f, 0f)),
                    CloneNode(componentToLocal, controlRigGraphId + "/component-to-local", new Vector2(500f, 0f)),
                    graphOutput
                };
                CharacterPoseCanvasConnection[] rigEdges =
                {
                    Edge(controlRigGraphId + "/input-pose", graphInput, "pose.local", rigNodes[1], "local-pose"),
                    Edge(controlRigGraphId + "/foot-weight", graphInput, "foot-weight", rigNodes[2], "weight"),
                    Edge(controlRigGraphId + "/component-to-foot", rigNodes[1], "component-pose", rigNodes[2], "pose"),
                    Edge(controlRigGraphId + "/component-to-ik", rigNodes[1], "component-pose", rigNodes[3], "pose"),
                    Edge(controlRigGraphId + "/foot-to-ik", rigNodes[2], "contribution", rigNodes[3], "goal-contribution.0"),
                    Edge(controlRigGraphId + "/ik-to-local", rigNodes[3], "result", rigNodes[4], "component-pose"),
                    Edge(controlRigGraphId + "/local-to-output", rigNodes[4], "local-pose", graphOutput, "pose.local")
                };
                CharacterPoseCanvasGraph controlRig = CharacterPoseCanvasGraph.CreateAuthoring(
                    new PoseGraphId(controlRigGraphId),
                    Guid.NewGuid().ToString("N"),
                    root.Parameters.ToArray(),
                    rigNodes,
                    rigEdges,
                    new[]
                    {
                        new CharacterPoseGraphLayoutEntry(graphInput.NodeId, graphInput.position),
                        new CharacterPoseGraphLayoutEntry(rigNodes[1].NodeId, rigNodes[1].position),
                        new CharacterPoseGraphLayoutEntry(rigNodes[2].NodeId, rigNodes[2].position),
                        new CharacterPoseGraphLayoutEntry(rigNodes[3].NodeId, rigNodes[3].position),
                        new CharacterPoseGraphLayoutEntry(rigNodes[4].NodeId, rigNodes[4].position),
                        new CharacterPoseGraphLayoutEntry(graphOutput.NodeId, graphOutput.position)
                    },
                    CharacterPoseAuthoringGraphRole.ControlRig);
                asset.AddGraph(controlRig);

                var call = new CharacterPoseCanvasNode(
                    new PoseNodeId("corin.control-rig.body.call"),
                    "Control Rig: Body",
                    new CharacterPoseSubgraphPayload(
                        new CharacterPoseSubgraphReference()),
                    new[]
                    {
                        new CharacterPoseDynamicPort(
                            new PosePortId("pose.input"),
                            "Input Pose",
                            CharacterPosePortKind.LocalPose,
                            CharacterPosePortDirection.Input,
                            true,
                            0,
                            inputPoseInterface),
                        new CharacterPoseDynamicPort(
                            new PosePortId("foot-weight.input"),
                            "Foot Placement Weight",
                            CharacterPosePortKind.Parameter,
                            CharacterPosePortDirection.Input,
                            true,
                            1,
                            footWeightInterface),
                        new CharacterPoseDynamicPort(
                            new PosePortId("pose.output"),
                            "Output Pose",
                            CharacterPosePortKind.LocalPose,
                            CharacterPosePortDirection.Output,
                            true,
                            2,
                            outputPoseInterface)
                    },
                    new Vector2(380f, 0f));
                call.Subgraph.Assign(new PoseGraphId(controlRigGraphId));
                CharacterPoseCanvasNode[] rootNodes = root.Nodes
                    .Where(value => !new[]
                    {
                        localToComponent.NodeId,
                        footPlacement.NodeId,
                        goalAssembler.NodeId,
                        fullBodyIk.NodeId,
                        componentToLocal.NodeId
                    }.Contains(value.NodeId))
                    .Concat(new[] { call })
                    .ToArray();
                CharacterPoseCanvasConnection[] rootEdges = root.Edges
                    .Where(value =>
                        value.SourceNodeId != localToComponent.NodeId &&
                        value.TargetNodeId != localToComponent.NodeId &&
                        value.SourceNodeId != footPlacement.NodeId &&
                        value.TargetNodeId != footPlacement.NodeId &&
                        value.SourceNodeId != goalAssembler.NodeId &&
                        value.TargetNodeId != goalAssembler.NodeId &&
                        value.SourceNodeId != fullBodyIk.NodeId &&
                        value.TargetNodeId != fullBodyIk.NodeId &&
                        value.SourceNodeId != componentToLocal.NodeId &&
                        value.TargetNodeId != componentToLocal.NodeId)
                    .Concat(new[]
                    {
                        Edge("corin.root-to-control-rig", slot, "pose", call, "pose.input"),
                        Edge("corin.weight-to-control-rig", parameter, "parameter", call, "foot-weight.input"),
                        Edge("corin.control-rig-to-output", call, "pose.output", output, "pose")
                    })
                    .ToArray();
                CharacterPoseCanvasGraph rootCandidate = CharacterPoseCanvasGraph.CreateAuthoring(
                    root.GraphId,
                    Guid.NewGuid().ToString("N"),
                    root.Parameters.ToArray(),
                    rootNodes,
                    rootEdges,
                    root.Layout.Where(value => rootNodes.Any(node => node.NodeId == value.NodeId)).ToArray(),
                    root.Role);
                root.ApplyAuthoringState(rootCandidate);
                UnityEngine.Object.DestroyImmediate(rootCandidate);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void CleanCorinControlRigGraphDuplicates()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterPresentationPoseGraphAsset asset =
                definition.AnimationPresentationProfile.PoseGraph;
            PoseGraphId graphId = new PoseGraphId("corin.control-rig.body.graph");
            CharacterPoseCanvasGraph[] duplicates = AssetDatabase.LoadAllAssetsAtPath(
                    AssetDatabase.GetAssetPath(asset))
                .OfType<CharacterPoseCanvasGraph>()
                .Where(value => value != null && value.GraphId == graphId)
                .ToArray();
            CharacterPoseCanvasGraph valid = duplicates.SingleOrDefault(value =>
                value.Nodes.Any(node => node.Kind == CharacterPoseNodeKind.GraphInput &&
                    node.DynamicPorts.Any(port => port.InterfacePortId == new PoseInterfacePortId("entry.pose.input"))) &&
                value.Nodes.Any(node => node.Kind == CharacterPoseNodeKind.GraphOutput &&
                    node.DynamicPorts.Any(port => port.InterfacePortId == new PoseInterfacePortId("entry.pose.output"))));
            if (!valid || duplicates.Length < 2)
                throw new InvalidOperationException("Corin Control Rig graph does not have the expected duplicate cleanup shape.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clean Corin Control Rig Graph Duplicates");
            Undo.RegisterCompleteObjectUndo(asset, "Clean Corin Control Rig Graph Duplicates");
            try
            {
                if (asset.TryGetGraph(graphId, out _))
                    asset.RemoveGraph(graphId);
                for (int i = 0; i < duplicates.Length; i++)
                    if (duplicates[i] != valid)
                        Undo.DestroyObjectImmediate(duplicates[i]);
                asset.AddGraph(valid);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void CleanCorinRootParameterResolver()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterPresentationPoseGraphAsset asset =
                definition.AnimationPresentationProfile.PoseGraph;
            CharacterPoseCanvasGraph root = asset.Graph;
            CharacterPoseCanvasNode[] resolvers = root.Nodes
                .Where(value => value.Kind == CharacterPoseNodeKind.PoseParameterResolve)
                .ToArray();
            if (resolvers.Length == 0)
                throw new InvalidOperationException("Corin root has no Pose Parameter Resolve node to clean.");
            if (resolvers.Any(value => root.Edges.Any(edge =>
                    edge.SourceNodeId == value.NodeId || edge.TargetNodeId == value.NodeId) &&
                root.Edges.Any(edge => edge.SourceNodeId == value.NodeId)))
                throw new InvalidOperationException("Corin Pose Parameter Resolve still has a downstream consumer.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clean Corin Root Parameter Resolver");
            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { asset, root }, "Clean Corin Root Parameter Resolver");
            try
            {
                HashSet<PoseNodeId> removed = new HashSet<PoseNodeId>(resolvers.Select(value => value.NodeId));
                CharacterPoseCanvasNode[] nodes = root.Nodes
                    .Where(value => !removed.Contains(value.NodeId))
                    .ToArray();
                CharacterPoseCanvasConnection[] edges = root.Edges
                    .Where(value => !removed.Contains(value.SourceNodeId) &&
                        !removed.Contains(value.TargetNodeId))
                    .ToArray();
                CharacterPoseCanvasGraph candidate = CharacterPoseCanvasGraph.CreateAuthoring(
                    root.GraphId,
                    Guid.NewGuid().ToString("N"),
                    root.Parameters.ToArray(),
                    nodes,
                    edges,
                    root.Layout.Where(value => !removed.Contains(value.NodeId)).ToArray(),
                    root.Role);
                root.ApplyAuthoringState(candidate);
                UnityEngine.Object.DestroyImmediate(candidate);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void CleanCorinRootActionPlaybackInput()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterPresentationPoseGraphAsset asset =
                definition.AnimationPresentationProfile.PoseGraph;
            CharacterPoseCanvasGraph root = asset.Graph;
            CharacterPoseCanvasNode[] inputs = root.Nodes
                .Where(value => value.Kind == CharacterPoseNodeKind.ActionPlaybackInput)
                .ToArray();
            if (inputs.Length == 0)
                throw new InvalidOperationException("Corin root has no Action Playback Input node to clean.");
            HashSet<PoseNodeId> removed = new HashSet<PoseNodeId>(inputs.Select(value => value.NodeId));
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Clean Corin Root Action Playback Input");
            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { asset, root }, "Clean Corin Root Action Playback Input");
            try
            {
                CharacterPoseCanvasNode[] nodes = root.Nodes
                    .Where(value => !removed.Contains(value.NodeId))
                    .ToArray();
                CharacterPoseCanvasConnection[] edges = root.Edges
                    .Where(value => !removed.Contains(value.SourceNodeId) &&
                        !removed.Contains(value.TargetNodeId))
                    .ToArray();
                CharacterPoseCanvasGraph candidate = CharacterPoseCanvasGraph.CreateAuthoring(
                    root.GraphId,
                    Guid.NewGuid().ToString("N"),
                    root.Parameters.ToArray(),
                    nodes,
                    edges,
                    root.Layout.Where(value => !removed.Contains(value.NodeId)).ToArray(),
                    root.Role);
                root.ApplyAuthoringState(candidate);
                UnityEngine.Object.DestroyImmediate(candidate);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static void MigrateCorinControlRigGoalAssembly()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph)
                throw new InvalidOperationException("Corin Definition lacks exact Animation Presentation context.");
            CharacterPresentationPoseGraphAsset asset =
                definition.AnimationPresentationProfile.PoseGraph;
            CharacterPoseCanvasGraph graph = asset.RequireGraph(
                new PoseGraphId("corin.control-rig.body.graph"));
            CharacterPoseCanvasNode graphInput = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.GraphInput);
            CharacterPoseCanvasNode graphOutput = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.GraphOutput);
            CharacterPoseCanvasNode localToComponent = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.LocalToComponentPose);
            CharacterPoseCanvasNode footPlacement = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.FootPlacement);
            CharacterPoseCanvasNode goalAssembler = graph.Nodes.SingleOrDefault(
                value => value.Kind == CharacterPoseNodeKind.FullBodyIkGoalAssembler);
            CharacterPoseCanvasNode fullBodyIk = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.FullBodyIK);
            CharacterPoseCanvasNode componentToLocal = graph.Nodes.Single(
                value => value.Kind == CharacterPoseNodeKind.ComponentToLocalPose);
            if (goalAssembler == null)
                throw new InvalidOperationException("Corin Control Rig has no author Goal Assembler to migrate.");
            if (fullBodyIk.DynamicPorts.Any(value =>
                    value.Kind == CharacterPosePortKind.FullBodyIkGoalContribution))
                throw new InvalidOperationException("Corin Control Rig Full Body IK already has direct Goal Contribution input.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Migrate Corin Control Rig Goal Assembly");
            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { asset, graph }, "Migrate Corin Control Rig Goal Assembly");
            try
            {
                CharacterPoseCanvasNode[] nodes =
                {
                    graphInput,
                    localToComponent,
                    footPlacement,
                    CloneNodeWithDynamicPort(
                        fullBodyIk,
                        fullBodyIk.NodeId.Value,
                        new CharacterPoseDynamicPort(
                            new PosePortId("goal-contribution.0"),
                            "Goal Contribution",
                            CharacterPosePortKind.FullBodyIkGoalContribution,
                            CharacterPosePortDirection.Input,
                            true,
                            0),
                        fullBodyIk.position),
                    componentToLocal,
                    graphOutput
                };
                CharacterPoseCanvasConnection[] edges =
                {
                    Edge("corin.control-rig.body/input-pose", graphInput, "pose.local", localToComponent, "local-pose"),
                    Edge("corin.control-rig.body/foot-weight", graphInput, "foot-weight", footPlacement, "weight"),
                    Edge("corin.control-rig.body/component-to-foot", localToComponent, "component-pose", footPlacement, "pose"),
                    Edge("corin.control-rig.body/component-to-ik", localToComponent, "component-pose", nodes[3], "pose"),
                    Edge("corin.control-rig.body/foot-to-ik", footPlacement, "contribution", nodes[3], "goal-contribution.0"),
                    Edge("corin.control-rig.body/ik-to-local", nodes[3], "result", componentToLocal, "component-pose"),
                    Edge("corin.control-rig.body/local-to-output", componentToLocal, "local-pose", graphOutput, "pose.local")
                };
                CharacterPoseCanvasGraph candidate = CharacterPoseCanvasGraph.CreateAuthoring(
                    graph.GraphId,
                    Guid.NewGuid().ToString("N"),
                    graph.Parameters.ToArray(),
                    nodes,
                    edges,
                    graph.Layout.Where(value => nodes.Any(node => node.NodeId == value.NodeId)).ToArray(),
                    CharacterPoseAuthoringGraphRole.ControlRig);
                graph.ApplyAuthoringState(candidate);
                UnityEngine.Object.DestroyImmediate(candidate);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        static CharacterPoseCanvasNode RequireNode(
            CharacterPoseCanvasGraph graph,
            string nodeId) =>
            graph.RequireNode(new PoseNodeId(nodeId));

        static CharacterPoseCanvasNode CloneNode(
            CharacterPoseCanvasNode source,
            string nodeId,
            Vector2 position) =>
            new CharacterPoseCanvasNode(
                new PoseNodeId(nodeId),
                source.DisplayName,
                source.Payload,
                source.DynamicPorts.ToArray(),
                position);

        static CharacterPoseCanvasNode CloneNodeWithDynamicPort(
            CharacterPoseCanvasNode source,
            string nodeId,
            CharacterPoseDynamicPort port,
            Vector2 position) =>
            new CharacterPoseCanvasNode(
                new PoseNodeId(nodeId),
                source.DisplayName,
                source.Payload,
                source.DynamicPorts.Concat(new[] { port }).ToArray(),
                position);

        static CharacterPoseCanvasConnection Edge(
            string edgeId,
            CharacterPoseCanvasNode source,
            string sourcePortId,
            CharacterPoseCanvasNode target,
            string targetPortId) =>
            new CharacterPoseCanvasConnection(
                edgeId,
                source.NodeId,
                new PosePortId(sourcePortId),
                target.NodeId,
                new PosePortId(targetPortId));
    }
}
