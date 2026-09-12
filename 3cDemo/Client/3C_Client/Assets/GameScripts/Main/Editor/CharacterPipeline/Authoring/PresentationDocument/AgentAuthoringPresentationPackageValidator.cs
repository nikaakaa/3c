using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;


using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation
{
    internal static class AgentAuthoringPresentationPackageValidator
    {
        internal static bool ValidateFileSet(
            IReadOnlyDictionary<string, JToken> files,
            AgentCompileReport report)
        {
            bool valid = true;
            foreach (KeyValuePair<string, JToken> pair in files)
            {
                if (pair.Key.StartsWith(
                        "readonly/presentation/",
                        StringComparison.Ordinal))
                {
                    if (!AgentAuthoringPresentationPackageCodec.IsInterfaceFile(pair.Key))
                    {
                        report.Error(
                            pair.Key,
                            "presentation_readonly_file_unknown",
                    "Document v8包含未知Presentation readonly文件。");
                        valid = false;
                    }
                    valid &= RejectInternalFields(pair.Value, pair.Key, report);
                    continue;
                }
                if (!pair.Key.StartsWith(
                        "editable/presentation/",
                        StringComparison.Ordinal) &&
                    !pair.Key.StartsWith(AgentAuthoringPresentationPackageCodec.ClipPrefix, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(
                        pair.Key,
                        AgentAuthoringPresentationPackageCodec.ProfilePath,
                        StringComparison.Ordinal) &&
                    !AgentAuthoringPresentationPackageCodec.IsGraphFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsClipFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsLayoutFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsStateMachineFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsStateMachineLayoutFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsImplementationFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsImplementationGraphFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsImplementationGraphLayoutFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineFile(pair.Key) &&
                    !AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineLayoutFile(pair.Key))
                {
                    report.Error(
                        pair.Key,
                        "presentation_file_unknown",
                    "Document v8包含未知Presentation文件。");
                    valid = false;
                }
                valid &= RejectInternalFields(pair.Value, pair.Key, report);
            }
            foreach (string layoutPath in files.Keys.Where(AgentAuthoringPresentationPackageCodec.IsLayoutFile))
            {
                string graphPath = layoutPath.Substring(
                    0,
                    layoutPath.Length - "layout.json".Length) + "graph.json";
                if (files.ContainsKey(graphPath))
                    continue;
                report.Error(
                    layoutPath,
                    "presentation_pose_graph_pair_missing",
                    "Pose Graph layout缺少同目录graph.json。");
                valid = false;
            }
            foreach (string machinePath in files.Keys.Where(AgentAuthoringPresentationPackageCodec.IsStateMachineFile))
            {
                string layoutPath = machinePath.Substring(
                    0,
                    machinePath.Length - "state-machine.json".Length) +
                    "layout.json";
                if (files.ContainsKey(layoutPath))
                    continue;
                report.Error(
                    machinePath,
                    "presentation_pose_state_machine_layout_recheckout_required",
                    "Pose StateMachine旧Document闭包缺少layout.json，请显式重新checkout。");
                valid = false;
            }
            foreach (string layoutPath in files.Keys.Where(
                         AgentAuthoringPresentationPackageCodec.IsStateMachineLayoutFile))
            {
                string machinePath = layoutPath.Substring(
                    0,
                    layoutPath.Length - "layout.json".Length) +
                    "state-machine.json";
                if (files.ContainsKey(machinePath))
                    continue;
                report.Error(
                    layoutPath,
                    "presentation_pose_state_machine_pair_missing",
                    "Pose StateMachine layout缺少同目录state-machine.json。");
                valid = false;
            }
            foreach (string layoutPath in files.Keys.Where(
                         AgentAuthoringPresentationPackageCodec.IsImplementationGraphLayoutFile))
            {
                string graphPath = layoutPath.Substring(
                    0,
                    layoutPath.Length - "layout.json".Length) + "graph.json";
                if (files.ContainsKey(graphPath))
                    continue;
                report.Error(
                    layoutPath,
                    "linked_pose_graph_pair_missing",
                    "Linked Pose graph layout缺少同目录graph.json。");
                valid = false;
            }
            foreach (string graphPath in files.Keys.Where(
                         AgentAuthoringPresentationPackageCodec.IsImplementationGraphFile))
            {
                string layoutPath = graphPath.Substring(
                    0,
                    graphPath.Length - "graph.json".Length) + "layout.json";
                if (files.ContainsKey(layoutPath))
                    continue;
                report.Error(
                    graphPath,
                    "linked_pose_graph_layout_missing",
                    "Linked Pose graph缺少同目录layout.json。");
                valid = false;
            }
            foreach (string layoutPath in files.Keys.Where(
                         AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineLayoutFile))
            {
                string machinePath = layoutPath.Substring(
                    0,
                    layoutPath.Length - "layout.json".Length) +
                    "state-machine.json";
                if (files.ContainsKey(machinePath))
                    continue;
                report.Error(
                    layoutPath,
                    "linked_pose_state_machine_pair_missing",
                    "Linked Pose StateMachine layout缺少同目录state-machine.json。");
                valid = false;
            }
            foreach (string machinePath in files.Keys.Where(
                         AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineFile))
            {
                string layoutPath = machinePath.Substring(
                    0,
                    machinePath.Length - "state-machine.json".Length) +
                    "layout.json";
                if (files.ContainsKey(layoutPath))
                    continue;
                report.Error(
                    machinePath,
                    "linked_pose_state_machine_layout_missing",
                    "Linked Pose StateMachine缺少同目录layout.json。");
                valid = false;
            }
            return valid;
        }

        internal static bool ValidatePresentation(
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            bool valid = ValidateAnimationClips(presentation, report);
            valid &= ValidateProfile(presentation.profile, presentation, report);
            var graphs = new Dictionary<string, AgentPackagePoseGraphFile>(
                StringComparer.Ordinal);
            var nodes = new Dictionary<string, AgentPackagePoseNode>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in presentation.poseGraphs ??
                         new List<AgentPackagePoseGraphFile>())
            {
                if (graph == null || !Identity(graph.id) ||
                    !graphs.TryAdd(graph.id, graph))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphPrefix,
                        "presentation_pose_graph_identity_invalid",
                        "Pose Graph identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateGraph(graph, nodes, report);
            }
            valid &= ValidateSubgraphSignatures(graphs, report);
            foreach (AgentPackagePoseGraphLayoutFile layout in
                     presentation.poseGraphLayouts ??
                     new List<AgentPackagePoseGraphLayoutFile>())
            {
                if (layout == null ||
                    !graphs.TryGetValue(
                        layout.graphId ?? string.Empty,
                        out AgentPackagePoseGraphFile graph))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphPrefix,
                        "presentation_pose_layout_owner_missing",
                        "Pose Graph layout引用了未知graph。");
                    valid = false;
                    continue;
                }
                HashSet<string> graphNodes = graph.nodes
                    .Where(value => value != null)
                    .Select(value => value.id)
                    .ToHashSet(StringComparer.Ordinal);
                HashSet<string> layoutNodes = new HashSet<string>(
                    StringComparer.Ordinal);
                foreach (AgentPackagePoseNodeLayout node in layout.nodes ??
                             new List<AgentPackagePoseNodeLayout>())
                {
                    if (node == null || !graphNodes.Contains(node.id) ||
                        !layoutNodes.Add(node.id) ||
                        !float.IsFinite(node.x) || !float.IsFinite(node.y))
                    {
                        report.Error(
                            AgentAuthoringPresentationPackageCodec.GraphDirectory(layout.graphId) + "/layout.json",
                            "presentation_pose_layout_invalid",
                            "Pose Graph layout节点缺失、重复或坐标非法。");
                        valid = false;
                    }
                }
                if (!graphNodes.SetEquals(layoutNodes))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(layout.graphId) + "/layout.json",
                        "presentation_pose_layout_incomplete",
                        "Pose Graph layout必须精确覆盖当前graph节点。");
                    valid = false;
                }
            }

            var machines = new Dictionary<string, AgentPackagePoseStateMachineFile>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseStateMachineFile machine in
                     presentation.poseStateMachines ??
                     new List<AgentPackagePoseStateMachineFile>())
            {
                if (machine == null || !Identity(machine.id) ||
                    !machines.TryAdd(machine.id, machine))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.StateMachinePrefix,
                        "presentation_pose_state_machine_identity_invalid",
                        "Pose StateMachine identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateStateMachine(machine, graphs, report);
            }
            var machineLayouts = new Dictionary<
                string,
                AgentPackagePoseStateMachineLayoutFile>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseStateMachineLayoutFile layout in
                     presentation.poseStateMachineLayouts ??
                     new List<AgentPackagePoseStateMachineLayoutFile>())
            {
                if (layout == null ||
                    !Identity(layout.stateMachineId) ||
                    !machines.TryGetValue(
                        layout.stateMachineId,
                        out AgentPackagePoseStateMachineFile machine) ||
                    !machineLayouts.TryAdd(layout.stateMachineId, layout))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.StateMachinePrefix,
                        "presentation_pose_state_machine_layout_owner_invalid",
                        "Pose StateMachine layout owner缺失、重复或引用未知StateMachine。");
                    valid = false;
                    continue;
                }
                var validElements = new HashSet<string>(
                    (machine.states ?? new List<AgentPackagePoseState>())
                        .Where(value => value != null)
                        .Select(value => value.id)
                        .Concat((machine.aliases ??
                                 new List<AgentPackagePoseStateAlias>())
                            .Where(value => value != null)
                            .Select(value => value.id))
                        .Concat(machine.entry == null
                            ? Array.Empty<string>()
                            : new[] { machine.entry.id }),
                    StringComparer.Ordinal);
                var explicitElements = new HashSet<string>(
                    StringComparer.Ordinal);
                if (layout.elements == null)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.StateMachineDirectory(layout.stateMachineId) +
                        "/layout.json",
                        "presentation_pose_state_machine_layout_invalid",
                        "Pose StateMachine layout必须显式提供elements数组。");
                    valid = false;
                    continue;
                }
                foreach (AgentPackagePoseStateMachineLayoutElement element in
                         layout.elements)
                {
                    if (element == null ||
                        !validElements.Contains(element.id) ||
                        !explicitElements.Add(element.id) ||
                        !float.IsFinite(element.x) ||
                        !float.IsFinite(element.y))
                    {
                        report.Error(
                            AgentAuthoringPresentationPackageCodec.StateMachineDirectory(layout.stateMachineId) +
                            "/layout.json",
                            "presentation_pose_state_machine_layout_invalid",
                            "Pose StateMachine layout元素缺失、重复、引用未知元素或坐标非法。");
                        valid = false;
                    }
                }
            }
            if (!machineLayouts.Keys.ToHashSet(StringComparer.Ordinal)
                    .SetEquals(machines.Keys))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.StateMachinePrefix,
                    "presentation_pose_state_machine_layout_incomplete",
                    "每个Pose StateMachine必须精确拥有一个layout分片。");
                valid = false;
            }

            var implementations = new Dictionary<string, AgentPackageLinkedPoseImplementationFile>(
                StringComparer.Ordinal);
            var implementationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     presentation.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                if (implementation == null || !Identity(implementation.id) ||
                    !implementations.TryAdd(implementation.id, implementation) ||
                    !Identity(implementation.implementationId) ||
                    !implementationIds.Add(implementation.implementationId))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ImplementationPrefix,
                        "linked_pose_implementation_identity_invalid",
                        "Linked Pose Implementation对象identity或业务identity缺失、重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateImplementation(
                    implementation,
                    AgentAuthoringPresentationPackageCodec.ImplementationDirectory(implementation.id) +
                    "/implementation.json",
                    report);
            }
            valid &= ValidateLinkedProfile(
                presentation.profile,
                implementations.Values,
                report);

            foreach (AgentPackagePoseNode node in nodes.Values)
            {
                CharacterPoseAuthoringNodeMetadata metadata =
                    CharacterPoseAuthoringMetadata.RequireCapability(
                        node.capability);
                bool stateMachine = metadata.OperationFamily ==
                    CharacterPoseOperationFamily.StateMachine;
                bool motionMatching = metadata.OperationFamily ==
                    CharacterPoseOperationFamily.MotionMatching;
                GraphAuthoringFieldDescriptor graphField = metadata.Fields
                    .SingleOrDefault(value => value.PickerKind == "pose-graph");
                string expectedGraph = motionMatching && graphField != null
                    ? node.properties?[graphField.FieldId.Value]?.Value<string>() ??
                      string.Empty
                    : string.Empty;
                bool validChild = stateMachine
                    ? !string.IsNullOrWhiteSpace(node.childDocumentId) &&
                      machines.ContainsKey(node.childDocumentId)
                    : motionMatching
                        ? !string.IsNullOrWhiteSpace(expectedGraph) &&
                          string.Equals(
                              node.childDocumentId,
                              expectedGraph,
                              StringComparison.Ordinal) &&
                          graphs.ContainsKey(expectedGraph)
                        : string.IsNullOrWhiteSpace(node.childDocumentId);
                if (!validChild)
                {
                    report.Error(
                        $"presentation.poseNodes[{node.id}].childDocumentId",
                        "presentation_pose_child_document_invalid",
                        "Pose节点child document必须与唯一Node Definition的直接图依赖一致。");
                    valid = false;
                }
            }
            AgentPackagePoseGraphFile[] rootGraphs = graphs.Values
                .Where(value => string.Equals(
                    value.role,
                    CharacterPoseGraphAuthoringCapabilities.RootGraph.Value,
                    StringComparison.Ordinal))
                .ToArray();
            if (rootGraphs.Length != 1)
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.GraphPrefix,
                    "presentation_pose_root_graph_invalid",
                    "Presentation Pose Graph必须恰好包含一个root graph。");
                valid = false;
            }
            else
            {
                valid &= ValidateGraphClosure(
                    new[] { rootGraphs[0].id },
                    graphs,
                    machines,
                    "editable/presentation",
                    report,
                    out _,
                    out _);
            }
            return valid;
        }

        static bool ValidateGraphClosure(
            IReadOnlyCollection<string> roots,
            IReadOnlyDictionary<string, AgentPackagePoseGraphFile> graphs,
            IReadOnlyDictionary<string, AgentPackagePoseStateMachineFile> machines,
            string path,
            AgentCompileReport report,
            out HashSet<string> reachableGraphs,
            out HashSet<string> reachableMachines)
        {
            reachableGraphs = new HashSet<string>(StringComparer.Ordinal);
            reachableMachines = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>(
                (roots ?? Array.Empty<string>())
                    .Where(value => graphs.ContainsKey(value ?? string.Empty))
                    .Reverse());
            while (pending.Count > 0)
            {
                string graphId = pending.Pop();
                if (!reachableGraphs.Add(graphId) ||
                    !graphs.TryGetValue(
                        graphId,
                        out AgentPackagePoseGraphFile graph))
                    continue;
                foreach (AgentPackagePoseNode node in graph.nodes ??
                             new List<AgentPackagePoseNode>())
                {
                    if (node == null)
                        continue;
                    CharacterPoseAuthoringNodeMetadata metadata;
                    try
                    {
                        metadata = CharacterPoseAuthoringMetadata.RequireCapability(
                            node.capability);
                    }
                    catch
                    {
                        continue;
                    }
                    if (metadata.OperationFamily ==
                        CharacterPoseOperationFamily.StateMachine)
                    {
                        if (!machines.TryGetValue(
                                node.childDocumentId ?? string.Empty,
                                out AgentPackagePoseStateMachineFile machine) ||
                            !reachableMachines.Add(machine.id))
                            continue;
                        foreach (AgentPackagePoseState state in machine.states ??
                                     new List<AgentPackagePoseState>())
                        {
                            if (state != null &&
                                graphs.ContainsKey(state.poseGraphId ?? string.Empty))
                                pending.Push(state.poseGraphId);
                        }
                        continue;
                    }
                    if (metadata.NativeRole !=
                            CharacterPoseNativeNodeRole.Subgraph &&
                        metadata.OperationFamily !=
                            CharacterPoseOperationFamily.MotionMatching)
                        continue;
                    GraphAuthoringFieldDescriptor graphField = metadata.Fields
                        .SingleOrDefault(value => value.PickerKind == "pose-graph");
                    string childGraphId = graphField == null
                        ? string.Empty
                        : node.properties?[graphField.FieldId.Value]
                            ?.Value<string>() ?? string.Empty;
                    if (graphs.ContainsKey(childGraphId))
                        pending.Push(childGraphId);
                }
            }

            bool valid = true;
            foreach (string graphId in graphs.Keys.Except(
                         reachableGraphs,
                         StringComparer.Ordinal))
            {
                report.Error(
                    path + ".poseGraphs[" + graphId + "]",
                    "presentation_pose_graph_closure_invalid",
                    "Pose Graph分片不在root或Entry Graph的正式闭包中。");
                valid = false;
            }
            foreach (string machineId in machines.Keys.Except(
                         reachableMachines,
                         StringComparer.Ordinal))
            {
                report.Error(
                    path + ".poseStateMachines[" + machineId + "]",
                    "presentation_pose_state_machine_closure_invalid",
                    "Pose StateMachine分片不由可达的PoseStateMachine节点持有。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateAnimationClips(
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            bool valid = true;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var clips = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageAnimationClipCurvesFile clip in presentation.animationClips ??
                         new List<AgentPackageAnimationClipCurvesFile>())
            {
                string path = AgentAuthoringPresentationPackageCodec.ClipPath(clip?.id);
                bool entryValid = clip != null && Identity(clip.id) && identities.Add(clip.id) &&
                                  Asset(clip.clip) && clips.Add(ReferenceIdentity(clip.clip)) &&
                                  string.Equals(clip.id, ReferenceIdentity(clip.clip), StringComparison.Ordinal) &&
                                  Identity(clip.dependencyBaseline) && Identity(clip.analysisInputHash) &&
                                  Identity(clip.registeredCurveHash) &&
                                  clip.curves != null &&
                                  clip.curves.Count <= CharacterAnimationClipRegisteredCurveCatalog.Channels.Count;
                var channels = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentPackageCurve curve in clip?.curves ?? new List<AgentPackageCurve>())
                {
                    entryValid &= curve != null &&
                                  RegisteredChannel(curve.channelId) &&
                                  channels.Add(curve.channelId) &&
                                  string.Equals(curve.timeDomain, "seconds", StringComparison.Ordinal) &&
                                  curve.keys != null && curve.keys.Count >= 2 &&
                                  ValidateClipCurveKeys(curve);
                }
                int footMotionCount = CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count(
                    value => channels.Contains(value.ChannelId));
                entryValid &= footMotionCount == 0 ||
                              footMotionCount == CharacterAnimationClipRegisteredCurveCatalog.FootMotionChannels.Count;
                if (entryValid)
                    continue;
                report.Error(path, "animation_clip_curve_fragment_invalid", "AnimationClip Curve分片的对象引用、baseline、channel、秒域或key合同非法。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateClipCurveKeys(AgentPackageCurve curve)
        {
            CharacterAnimationClipRegisteredCurveDescriptor descriptor;
            try
            {
                descriptor = CharacterAnimationClipRegisteredCurveCatalog.Require(curve.channelId);
            }
            catch
            {
                return false;
            }
            float previousTime = -1f;
            for (int i = 0; i < curve.keys.Count; i++)
            {
                AgentAnimationCurveKey key = curve.keys[i];
                if (key == null || !float.IsFinite(key.time) || key.time < 0f || key.time <= previousTime ||
                    !float.IsFinite(key.value) ||
                    !ValidTangent(key.inTangent, descriptor.AllowConstantTangents) ||
                    !ValidTangent(key.outTangent, descriptor.AllowConstantTangents) ||
                    !float.IsFinite(key.inWeight) || !float.IsFinite(key.outWeight) ||
                    descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.Normalized01 &&
                    (key.value < 0f || key.value > 1f) ||
                    descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.NonNegative &&
                    key.value < 0f ||
                    descriptor.ValueDomain == CharacterAnimationClipRegisteredCurveValueDomain.LockMode &&
                    key.value != 0f && key.value != 1f && key.value != 2f)
                    return false;
                previousTime = key.time;
            }
            return true;
        }

        static bool ValidTangent(float value, bool allowConstant) =>
            float.IsFinite(value) || allowConstant && float.IsPositiveInfinity(value);

        static bool RegisteredChannel(string channelId)
        {
            try
            {
                _ = CharacterAnimationClipRegisteredCurveCatalog.Require(channelId);
                return true;
            }
            catch
            {
                return false;
            }
        }
        internal static bool ValidateImplementation(
            AgentPackageLinkedPoseImplementationFile implementation,
            string path,
            AgentCompileReport report)
        {
            bool valid = implementation != null &&
                         Identity(implementation.id) &&
                         !string.IsNullOrWhiteSpace(implementation.name) &&
                         AssetReference(implementation.asset) &&
                         string.Equals(
                             implementation.id,
                             ReferenceIdentity(implementation.asset),
                             StringComparison.Ordinal) &&
                         Identity(implementation.ownerIdentity) &&
                         Identity(implementation.implementationId) &&
                         implementation.revision > 0 &&
                         Asset(implementation.interfaceAsset) &&
                         AssetReference(implementation.graphOwner) &&
                         Identity(implementation.graphOwnerIdentity) &&
                         (implementation.entries?.Count ?? 0) > 0;
            if (!valid)
            {
                report.Error(
                    path,
                    "linked_pose_implementation_invalid",
                    "Linked Pose Implementation对象引用、owner、业务identity、revision、Interface或Graph owner不完整。");
            }

            var graphs = new Dictionary<string, AgentPackagePoseGraphFile>(
                StringComparer.Ordinal);
            var nodes = new Dictionary<string, AgentPackagePoseNode>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in implementation?.poseGraphs ??
                         new List<AgentPackagePoseGraphFile>())
            {
                if (graph == null || !Identity(graph.id) ||
                    !graphs.TryAdd(graph.id, graph))
                {
                    report.Error(
                        path + ".poseGraphs",
                        "linked_pose_graph_identity_invalid",
                        "Linked Pose graph identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateGraph(graph, nodes, report);
            }
            valid &= ValidateSubgraphSignatures(graphs, report);

            var layouts = new Dictionary<string, AgentPackagePoseGraphLayoutFile>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphLayoutFile layout in
                     implementation?.poseGraphLayouts ??
                     new List<AgentPackagePoseGraphLayoutFile>())
            {
                if (layout == null || !Identity(layout.graphId) ||
                    !graphs.TryGetValue(layout.graphId, out AgentPackagePoseGraphFile graph) ||
                    !layouts.TryAdd(layout.graphId, layout))
                {
                    report.Error(
                        path + ".poseGraphLayouts",
                        "linked_pose_graph_layout_owner_invalid",
                        "Linked Pose graph layout owner缺失、重复或引用未知graph。");
                    valid = false;
                    continue;
                }
                HashSet<string> graphNodes = (graph.nodes ??
                        new List<AgentPackagePoseNode>())
                    .Where(value => value != null)
                    .Select(value => value.id)
                    .ToHashSet(StringComparer.Ordinal);
                HashSet<string> layoutNodes = (layout.nodes ??
                        new List<AgentPackagePoseNodeLayout>())
                    .Where(value => value != null &&
                                    float.IsFinite(value.x) &&
                                    float.IsFinite(value.y))
                    .Select(value => value.id)
                    .ToHashSet(StringComparer.Ordinal);
                if (layoutNodes.Count != (layout.nodes?.Count ?? 0) ||
                    !layoutNodes.SetEquals(graphNodes))
                {
                    report.Error(
                        path + $".poseGraphLayouts[{layout.graphId}]",
                        "linked_pose_graph_layout_invalid",
                        "Linked Pose graph layout必须以有限坐标精确覆盖全部节点。");
                    valid = false;
                }
            }
            if (!layouts.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(graphs.Keys))
            {
                report.Error(
                    path + ".poseGraphLayouts",
                    "linked_pose_graph_layout_incomplete",
                    "每个Linked Pose graph必须精确拥有一个layout分片。");
                valid = false;
            }

            var machines = new Dictionary<string, AgentPackagePoseStateMachineFile>(
                StringComparer.Ordinal);
            foreach (AgentPackagePoseStateMachineFile machine in
                     implementation?.poseStateMachines ??
                     new List<AgentPackagePoseStateMachineFile>())
            {
                if (machine == null || !Identity(machine.id) ||
                    !machines.TryAdd(machine.id, machine))
                {
                    report.Error(
                        path + ".poseStateMachines",
                        "linked_pose_state_machine_identity_invalid",
                        "Linked Pose StateMachine identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateStateMachine(machine, graphs, report);
            }
            HashSet<string> machineLayouts = (implementation?.poseStateMachineLayouts ??
                    new List<AgentPackagePoseStateMachineLayoutFile>())
                .Where(value => value != null && Identity(value.stateMachineId))
                .Select(value => value.stateMachineId)
                .ToHashSet(StringComparer.Ordinal);
            if (machineLayouts.Count !=
                    (implementation?.poseStateMachineLayouts?.Count ?? 0) ||
                !machineLayouts.SetEquals(machines.Keys))
            {
                report.Error(
                    path + ".poseStateMachineLayouts",
                    "linked_pose_state_machine_layout_incomplete",
                    "每个Linked Pose StateMachine必须精确拥有一个layout分片。");
                valid = false;
            }

            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            var entryGraphs = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseImplementationEntry entry in
                     implementation?.entries ??
                     new List<AgentPackageLinkedPoseImplementationEntry>())
            {
                if (entry == null || !Identity(entry.entryId) ||
                    !entryIds.Add(entry.entryId) || !Identity(entry.graphId) ||
                    !entryGraphs.Add(entry.graphId) ||
                    !graphs.TryGetValue(entry.graphId, out AgentPackagePoseGraphFile graph) ||
                    !string.Equals(
                        graph.role,
                        CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry.Value,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        path + $".entries[{entry?.entryId}]",
                        "linked_pose_entry_invalid",
                        "Linked Pose Entry identity必须唯一并引用同一owner下的LinkedPoseEntry graph。");
                    valid = false;
                }
            }
            HashSet<string> declaredEntryGraphs = graphs.Values
                .Where(value => string.Equals(
                    value.role,
                    CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry.Value,
                    StringComparison.Ordinal))
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            if (!entryGraphs.SetEquals(declaredEntryGraphs))
            {
                report.Error(
                    path + ".entries",
                    "linked_pose_entry_graph_closure_invalid",
                    "Implementation Entry映射必须精确覆盖全部LinkedPoseEntry graph。");
                valid = false;
            }
            valid &= ValidateGraphClosure(
                entryGraphs,
                graphs,
                machines,
                path,
                report,
                out _,
                out _);
            return valid;
        }

        static bool ValidateLinkedProfile(
            AgentPackagePresentationProfileFile profile,
            IEnumerable<AgentPackageLinkedPoseImplementationFile> implementations,
            AgentCompileReport report)
        {
            bool valid = true;
            var implementationById = (implementations ??
                    Enumerable.Empty<AgentPackageLinkedPoseImplementationFile>())
                .Where(value => value != null)
                .ToDictionary(
                    value => value.implementationId,
                    StringComparer.Ordinal);
            var groups = new Dictionary<string, AgentPackageLinkedPoseGroupBinding>(
                StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseGroupBinding group in
                     profile?.linkedPoseGroups ??
                     new List<AgentPackageLinkedPoseGroupBinding>())
            {
                if (group == null || !Identity(group.id) ||
                    !string.Equals(group.id, group.groupId, StringComparison.Ordinal) ||
                    !groups.TryAdd(group.groupId, group) ||
                    !Asset(group.interfaceAsset))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ProfilePath + ".linkedPoseGroups",
                        "linked_pose_group_invalid",
                        "Linked Pose Group identity重复、对象key不稳定或Interface引用非法。");
                    valid = false;
                }
            }
            var selectors = new HashSet<string>(StringComparer.Ordinal);
            var selectorGroups = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseSelectorBinding selector in
                     profile?.linkedPoseSelectors ??
                     new List<AgentPackageLinkedPoseSelectorBinding>())
            {
                bool selectorValid = selector != null &&
                                     Identity(selector.id) &&
                                     selectors.Add(selector.id) &&
                                     AssetReference(selector.asset) &&
                                     string.Equals(
                                         selector.id,
                                         ReferenceIdentity(selector.asset),
                                         StringComparison.Ordinal) &&
                                     Identity(selector.selectorId) &&
                                     groups.ContainsKey(selector.groupId ?? string.Empty) &&
                                     selectorGroups.Add(selector.groupId) &&
                                     string.Equals(
                                         selector.kind,
                                         "equipment",
                                         StringComparison.Ordinal) &&
                                     selector.equipment != null &&
                                     Identity(selector.equipment.slotId) &&
                                     Identity(selector.equipment.emptyImplementationId) &&
                                     implementationById.TryGetValue(
                                         selector.equipment.emptyImplementationId,
                                         out AgentPackageLinkedPoseImplementationFile empty) &&
                                     SameAssetReference(
                                         empty.interfaceAsset,
                                         groups[selector.groupId].interfaceAsset);
                var equipmentIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentPackageEquipmentLinkedPoseMapping mapping in
                         selector?.equipment?.mappings ??
                         new List<AgentPackageEquipmentLinkedPoseMapping>())
                {
                    selectorValid &= mapping != null &&
                                     Identity(mapping.id) &&
                                     string.Equals(
                                         mapping.id,
                                         mapping.equipmentId,
                                         StringComparison.Ordinal) &&
                                     equipmentIds.Add(mapping.equipmentId) &&
                                     implementationById.TryGetValue(
                                         mapping.implementationId ?? string.Empty,
                                         out AgentPackageLinkedPoseImplementationFile candidate) &&
                                     groups.TryGetValue(
                                         selector.groupId ?? string.Empty,
                                         out AgentPackageLinkedPoseGroupBinding selectedGroup) &&
                                     SameAssetReference(
                                         candidate.interfaceAsset,
                                         selectedGroup.interfaceAsset);
                }
                if (!selectorValid)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ProfilePath + $".linkedPoseSelectors[{selector?.selectorId}]",
                        "linked_pose_selector_invalid",
                        "Linked Pose selector对象key、Group、Equipment映射或显式Empty Implementation非法。");
                    valid = false;
                }
            }
            if (!selectorGroups.SetEquals(groups.Keys))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.ProfilePath + ".linkedPoseSelectors",
                    "linked_pose_selector_group_closure_invalid",
                    "每个Linked Pose Group必须恰好拥有一个selector。");
                valid = false;
            }
            HashSet<string> candidates = (profile?.linkedPoseSelectors ??
                    new List<AgentPackageLinkedPoseSelectorBinding>())
                .Where(value => value?.equipment != null)
                .SelectMany(value => (value.equipment.mappings ??
                        new List<AgentPackageEquipmentLinkedPoseMapping>())
                    .Select(mapping => mapping?.implementationId)
                    .Append(value.equipment.emptyImplementationId))
                .Where(Identity)
                .ToHashSet(StringComparer.Ordinal);
            if (!candidates.SetEquals(implementationById.Keys))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.ProfilePath + ".linkedPoseSelectors",
                    "linked_pose_candidate_closure_invalid",
                    "全部Implementation必须由唯一selector映射或显式Empty Implementation精确覆盖。");
                valid = false;
            }
            return valid;
        }

        static bool SameAssetReference(
            AgentPackageObjectReference left,
            AgentPackageObjectReference right) =>
            string.Equals(
                ReferenceIdentity(left),
                ReferenceIdentity(right),
                StringComparison.Ordinal);

        internal static bool ValidateInterface(
            AgentPackageLinkedPoseInterfaceFile value,
            string path,
            AgentCompileReport report)
        {
            bool valid = value != null && Identity(value.id) &&
                         Asset(value.asset) &&
                         Identity(value.ownerIdentity) &&
                         string.Equals(value.id, value.interfaceId, StringComparison.Ordinal) &&
                         value.revision > 0 && Identity(value.signatureHash) &&
                         Identity(value.factContractIdentity) &&
                         string.Equals(
                             value.executionContract,
                             CharacterLinkedPoseExecutionContract.Current,
                             StringComparison.Ordinal) &&
                         (value.entries?.Count ?? 0) > 0;
            var entries = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseInterfaceEntry entry in
                     value?.entries ?? new List<AgentPackageLinkedPoseInterfaceEntry>())
            {
                bool entryValid = entry != null && Identity(entry.entryId) &&
                                  entries.Add(entry.entryId) &&
                                  ExactEnum<CharacterPoseExecutionDomain>(
                                      entry.executionDomain) &&
                                  (entry.ports?.Count ?? 0) > 0;
                var ports = new HashSet<string>(StringComparer.Ordinal);
                var orders = new HashSet<int>();
                foreach (AgentPackageLinkedPoseInterfacePort port in
                         entry?.ports ??
                         new List<AgentPackageLinkedPoseInterfacePort>())
                {
                    entryValid &= port != null && Identity(port.portId) &&
                                  ports.Add(port.portId) &&
                                  ExactEnum<CharacterPosePortDirection>(
                                      port.direction) &&
                                  ExactEnum<CharacterPosePortKind>(port.kind) &&
                                  ExactEnum<CharacterPoseSpace>(port.space) &&
                                  port.order >= 0 && orders.Add(port.order);
                }
                valid &= entryValid;
            }
            if (!valid)
            {
                report.Error(
                    path,
                    "linked_pose_interface_contract_invalid",
                    "Linked Pose Interface稳定identity、revision、signature、Fact contract、Entry或typed ports非法。");
            }
            return valid;
        }

        static bool ExactEnum<T>(string value)
            where T : struct, Enum =>
            Enum.TryParse(value, false, out T parsed) &&
            Enum.IsDefined(typeof(T), parsed) &&
            string.Equals(value, parsed.ToString(), StringComparison.Ordinal);

        static bool ValidateProfile(
            AgentPackagePresentationProfileFile profile,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            if (profile == null || !Identity(profile.id) ||
                !Asset(profile.owner) ||
                !string.Equals(
                    profile.id,
                    profile.owner.assetGuid,
                    StringComparison.Ordinal) ||
                !Asset(profile.poseGraph) ||
                !Asset(profile.rig) ||
                profile.policy == null ||
                !Enum.TryParse(
                    profile.policy.footPlacementAnalysisMode,
                    false,
                    out CharacterFootPlacementAnalysisMode _))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.ProfilePath,
                    "presentation_profile_invalid",
                    "Presentation Profile owner、Pose Graph、Rig或Policy不完整。");
                return false;
            }
            bool valid = true;
            var slots = new HashSet<string>(StringComparer.Ordinal);
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseSourceBinding source in profile.poseSources ??
                         new List<AgentPackagePoseSourceBinding>())
            {
                string slotIdentity = ReferenceIdentity(source?.slot);
                string bindingIdentity = ReferenceIdentity(source?.binding);
                if (source == null || string.IsNullOrWhiteSpace(source.name) ||
                    !AssetReference(source.slot) ||
                    !AssetReference(source.binding) ||
                    !slots.Add(slotIdentity) ||
                    !bindings.Add(bindingIdentity) ||
                    !Enum.TryParse(
                        source.kind,
                        false,
                        out PresentationPoseSourceKind sourceKind) ||
                    !Asset(source.source) ||
                    !Identity(source.contentRevision) ||
                    !ValidatePoseSourceKind(source, sourceKind, presentation))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ProfilePath + $".poseSources[{source?.name}]",
                        "presentation_pose_source_invalid",
                        $"Pose source非法：kind={source?.kind ?? "<null>"}, " +
                        $"source={ReferenceIdentity(source?.source)}, " +
                        $"contentRevision={source?.contentRevision ?? "<null>"}, footAnalysisIdentity={source?.footAnalysisIdentity ?? "<null>"}, " +
                        $"slot={slotIdentity}, binding={bindingIdentity}。");
                    valid = false;
                }
            }
            var resourceSlots = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseResourceBinding resource in profile.poseResources ??
                         new List<AgentPackagePoseResourceBinding>())
            {
                string slotIdentity = ReferenceIdentity(resource?.slot);
                if (resource == null ||
                    !AssetReference(resource.slot) ||
                    !Asset(resource.resource) ||
                    !resourceSlots.Add(slotIdentity) ||
                    !Enum.TryParse(
                        resource.kind,
                        false,
                        out CharacterPoseResourceKind _))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ProfilePath + $".poseResources[{slotIdentity}]",
                        "presentation_pose_resource_invalid",
                        "Pose Resource Binding的Slot、资源或kind非法，或Slot重复。");
                    valid = false;
                }
            }
            var producers = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageAnimationProducerBinding producer in
                     profile.actionProducers ??
                     new List<AgentPackageAnimationProducerBinding>())
            {
                string id = $"{producer?.timelineId}:{producer?.trackId}";
                if (producer == null || !Identity(producer.timelineId) ||
                    !Identity(producer.trackId) || !producers.Add(id))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.ProfilePath + ".actionProducers",
                        "presentation_action_producer_invalid",
                        "Action producer binding字段不完整或identity重复。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidatePoseSourceKind(
            AgentPackagePoseSourceBinding source,
            PresentationPoseSourceKind kind,
            AgentDocumentPresentationEditable presentation)
        {
            if (kind == PresentationPoseSourceKind.Clip)
                return (presentation.animationClips ?? new List<AgentPackageAnimationClipCurvesFile>())
                    .Any(value => string.Equals(ReferenceIdentity(value?.clip), ReferenceIdentity(source.source), StringComparison.Ordinal));
            if (kind == PresentationPoseSourceKind.BlendSpace)
                return true;
            return kind == PresentationPoseSourceKind.MotionMatching &&
                   !string.IsNullOrWhiteSpace(source.searchDomainId) &&
                   source.databases != null && source.databases.Count > 0 &&
                   source.databases.All(Asset);
        }

        static bool ValidateGraph(
            AgentPackagePoseGraphFile graph,
            IDictionary<string, AgentPackagePoseNode> allNodes,
            AgentCompileReport report)
        {
            if (!CharacterPoseGraphAuthoringCapabilities.TryResolveRole(
                    graph.role,
                    out CharacterPoseAuthoringGraphRole authoringRole))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.GraphDirectory(graph.id) + "/graph.json.role",
                    "presentation_pose_graph_role_invalid",
                    "Pose Graph role不是正式Presentation role。");
                return false;
            }
            GraphAuthoringDocumentRoleId role =
                CharacterPoseGraphAuthoringCapabilities.GetRole(authoringRole);
            bool valid = Identity(graph.contentRevision);
            var parameterIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseParameter parameter in graph.parameters ??
                         new List<AgentPackagePoseParameter>())
            {
                if (parameter == null ||
                    !Identity(parameter.id) ||
                    !parameterIds.Add(parameter.id) ||
                    string.IsNullOrWhiteSpace(parameter.displayName) ||
                    !Enum.TryParse(parameter.valueType, false, out PoseParameterValueType _) ||
                    !Enum.TryParse(parameter.usage, false, out CharacterPoseParameterUsage parameterUsage) ||
                    !Enum.TryParse(parameter.category, false, out CharacterPoseParameterInputCategory parameterCategory) ||
                    parameterCategory != CharacterPoseParameterAccess.ResolveCategory(
                        new PoseParameterId(parameter.id),
                        parameterUsage) ||
                    !CharacterPoseParameterAccess.IsBlackboardInput(
                        new PoseParameterId(parameter.id),
                        parameterUsage) ||
                    !Enum.TryParse(parameter.scope, false, out CharacterPoseAuthoringGraphRole parameterScope) ||
                    parameterScope != authoringRole ||
                    !string.Equals(parameter.owner, graph.id, StringComparison.Ordinal) ||
                    !float.IsFinite(parameter.defaultValue))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graph.id) + "/graph.json.parameters",
                        "presentation_pose_parameter_invalid",
                        "Pose 参数必须具有唯一identity、作者名称、值类型、usage和有限默认值。");
                    valid = false;
                }
            }
            var nodes = new Dictionary<string, NodeContract>(StringComparer.Ordinal);
            foreach (AgentPackagePoseNode node in graph.nodes ??
                         new List<AgentPackagePoseNode>())
            {
                if (node == null || !Identity(node.id) ||
                    nodes.ContainsKey(node.id) ||
                    allNodes.ContainsKey(node.id))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graph.id) + "/graph.json.nodes",
                        "presentation_pose_node_identity_invalid",
                        "Pose node identity缺失、重复或跨graph复用。");
                    valid = false;
                    continue;
                }
                try
                {
                    var capabilityId =
                        new GraphAuthoringCapabilityId(node.capability);
                    GraphAuthoringCapabilityDescriptor capability =
                        CharacterPoseGraphCapabilityProjector.Catalog.Require(
                            capabilityId,
                            CharacterPoseGraphAuthoringCapabilities.Domain,
                            role);
                    valid &= ValidateNode(
                        node,
                        capability,
                        graph.id,
                        report,
                        out IReadOnlyList<
                            GraphAuthoringDynamicPortProjection>
                            portShape);
                    nodes.Add(node.id, new NodeContract(portShape));
                    allNodes.Add(node.id, node);
                }
                catch (Exception exception)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graph.id) +
                        $"/graph.json.nodes[{node.id}]",
                        "presentation_pose_capability_unknown",
                        exception.Message);
                    valid = false;
                }
            }
            var edges = new HashSet<string>(StringComparer.Ordinal);
            var connectedInputs = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseEdge edge in graph.edges ??
                         new List<AgentPackagePoseEdge>())
            {
                if (edge == null || !Identity(edge.id) ||
                    !edges.Add(edge.id) ||
                    edge.from == null || edge.to == null ||
                    !nodes.TryGetValue(edge.from.node ?? string.Empty, out NodeContract from) ||
                    !nodes.TryGetValue(edge.to.node ?? string.Empty, out NodeContract to) ||
                    !from.TryPort(edge.from.port, out PortContract source) ||
                    !to.TryPort(edge.to.port, out PortContract target) ||
                    source.Direction != GraphAuthoringPortDirection.Output ||
                    target.Direction != GraphAuthoringPortDirection.Input ||
                    !connectedInputs.Add(
                        (edge.to.node ?? string.Empty) + "\0" +
                        (edge.to.port ?? string.Empty)) ||
                    !string.Equals(
                        source.ValueType,
                        target.ValueType,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graph.id) + "/graph.json.edges",
                        "presentation_pose_edge_invalid",
                        "Pose edge identity、endpoint、方向或值类型非法。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateNode(
            AgentPackagePoseNode node,
            GraphAuthoringCapabilityDescriptor capability,
            string graphId,
            AgentCompileReport report,
            out IReadOnlyList<GraphAuthoringDynamicPortProjection>
                portShape)
        {
            bool valid = true;
            portShape = Array.Empty<GraphAuthoringDynamicPortProjection>();
            if (CharacterPoseGraphCapabilityProjector.IsDocumentNodeRetired(
                    node.capability))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) + $"/graph.json.nodes[{node.id}].capability",
                    "presentation_pose_retired_author_node",
                    "v7作者图不能包含Action Playback Input、Pose Parameter Resolve或Goal Assembler作者节点。");
                valid = false;
            }
            var fields = capability.Fields
                .Where(value => value.AuthoringWritable)
                .ToDictionary(
                value => value.FieldId.Value,
                StringComparer.Ordinal);
            HashSet<string> actual = (node.properties ?? new JObject())
                .Properties()
                .Select(value => value.Name)
                .ToHashSet(StringComparer.Ordinal);
            string[] unexpected = actual
                .Except(fields.Keys, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string[] missingRequired = fields.Values
                .Where(value => !value.Optional &&
                                !actual.Contains(value.FieldId.Value))
                .Select(value => value.FieldId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (unexpected.Length > 0 || missingRequired.Length > 0)
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                    $"/graph.json.nodes[{node.id}].properties",
                    "presentation_pose_properties_invalid",
                    $"Pose node properties必须由唯一Capability定义。unexpected=[{string.Join(",", unexpected)}]; missingRequired=[{string.Join(",", missingRequired)}]");
                valid = false;
            }
            foreach (KeyValuePair<string, GraphAuthoringFieldDescriptor> field in
                     fields)
            {
                if (!node.properties.TryGetValue(
                        field.Key,
                        StringComparison.Ordinal,
                        out JToken value))
                {
                    if (field.Value.Optional)
                        continue;
                }
                if (!BtsmtlAuthoringFieldValueValidation.Matches(
                        field.Value,
                        value) ||
                    field.Value.ValueKind == GraphAuthoringFieldValueKind.AssetReference &&
                    !AssetToken(value))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}].properties.{field.Key}",
                        "presentation_pose_property_value_invalid",
                        "Pose node property值类型或约束非法。");
                    valid = false;
                }
            }

            var dynamicPorts = new List<
                GraphAuthoringDynamicPortProjection>();
            foreach (AgentPackagePoseDynamicPort port in node.dynamicPorts ??
                         new List<AgentPackagePoseDynamicPort>())
            {
                if (port == null || !Identity(port.id) ||
                    string.IsNullOrWhiteSpace(port.name) ||
                    port.order < 0)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}].dynamicPorts",
                        "presentation_pose_dynamic_port_invalid",
                        "Pose dynamic port不符合Capability策略。");
                    valid = false;
                    continue;
                }
                try
                {
                    dynamicPorts.Add(
                        CharacterPoseAuthoringPortProjection.Dynamic(
                            port.id,
                            port.name,
                            port.valueType,
                            port.direction,
                            port.required,
                            port.order,
                            port.interfacePortId));
                }
                catch (Exception exception)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}].dynamicPorts",
                        "presentation_pose_dynamic_port_invalid",
                        exception.Message);
                    valid = false;
                }
            }
            try
            {
                portShape = GraphAuthoringNodePortShapeProjector
                    .ProjectComplete(
                        capability,
                        CharacterPoseAuthoringPortProjection.ReadTypedProperties(
                            capability,
                            node.properties),
                        dynamicPorts);
            }
            catch (Exception exception)
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                    $"/graph.json.nodes[{node.id}].dynamicPorts",
                    exception is GraphAuthoringPortShapeException shape
                        ? shape.Code
                        : "port_shape_property_invalid",
                    exception.Message);
                valid = false;
            }
            CharacterPoseAuthoringNodeMetadata metadata =
                CharacterPoseAuthoringMetadata.RequireCapability(
                    node.capability);
            if (metadata.OperationFamily == CharacterPoseOperationFamily.AnimationSlot)
            {
                string[] requiredFields = metadata.Fields
                    .Where(value => value.PickerKind == "animation-slot" ||
                                    value.PickerKind == "animation-channel")
                    .Select(value => value.FieldId.Value)
                    .ToArray();
                if (requiredFields.Any(field =>
                        !Identity(node.properties[field]?.Value<string>())))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}]",
                        "presentation_animation_slot_binding_invalid",
                        "AnimationSlot binding缺少Slot或AnimationChannel identity。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateSubgraphSignatures(
            IReadOnlyDictionary<string, AgentPackagePoseGraphFile> graphs,
            AgentCompileReport report)
        {
            bool valid = true;
            string subgraphCapability = CharacterPoseAuthoringMetadata
                .Require(CharacterPoseNodeKind.PoseSubgraph)
                .CapabilityIdentity;
            string inputCapability = CharacterPoseAuthoringMetadata
                .Require(CharacterPoseNodeKind.GraphInput)
                .CapabilityIdentity;
            string outputCapability = CharacterPoseAuthoringMetadata
                .Require(CharacterPoseNodeKind.GraphOutput)
                .CapabilityIdentity;
            GraphAuthoringFieldDescriptor graphField =
                CharacterPoseAuthoringMetadata
                    .RequireCapability(subgraphCapability)
                    .Fields.Single(value => value.PickerKind == "pose-graph");
            foreach (AgentPackagePoseGraphFile owner in graphs.Values)
            {
                foreach (AgentPackagePoseNode callSite in owner.nodes ??
                             new List<AgentPackagePoseNode>())
                {
                    if (!string.Equals(
                            callSite?.capability,
                            subgraphCapability,
                            StringComparison.Ordinal))
                        continue;
                    string childId = callSite.properties?[graphField.FieldId.Value]
                        ?.Value<string>();
                    if (!graphs.TryGetValue(
                            childId ?? string.Empty,
                            out AgentPackagePoseGraphFile child))
                    {
                        report.Error(
                            AgentAuthoringPresentationPackageCodec.GraphDirectory(owner.id) + $"/graph.json.nodes[{callSite.id}]",
                            "presentation_pose_subgraph_missing",
                            "Pose Subgraph引用的child Graph不存在。");
                        valid = false;
                        continue;
                    }

                    AgentPackagePoseNode[] inputs = (child.nodes ??
                            new List<AgentPackagePoseNode>())
                        .Where(value => string.Equals(
                            value?.capability,
                            inputCapability,
                            StringComparison.Ordinal))
                        .ToArray();
                    AgentPackagePoseNode[] outputs = (child.nodes ??
                            new List<AgentPackagePoseNode>())
                        .Where(value => string.Equals(
                            value?.capability,
                            outputCapability,
                            StringComparison.Ordinal))
                        .ToArray();
                    if (inputs.Length != 1 || outputs.Length != 1 ||
                        !SignatureMatches(callSite, inputs[0], outputs[0]))
                    {
                        report.Error(
                            AgentAuthoringPresentationPackageCodec.GraphDirectory(owner.id) + $"/graph.json.nodes[{callSite.id}].dynamicPorts",
                            "presentation_pose_subgraph_signature_mismatch",
                            "Pose Subgraph call site必须与child Graph的interface identity、方向、值类型和required精确一致。");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool SignatureMatches(
            AgentPackagePoseNode callSite,
            AgentPackagePoseNode graphInput,
            AgentPackagePoseNode graphOutput)
        {
            var expected = new Dictionary<string, DocumentSignaturePort>(
                StringComparer.Ordinal);
            if (!AddSignaturePorts(
                    graphInput.dynamicPorts,
                    GraphAuthoringPortDirection.Output,
                    GraphAuthoringPortDirection.Input,
                    expected) ||
                !AddSignaturePorts(
                    graphOutput.dynamicPorts,
                    GraphAuthoringPortDirection.Input,
                    GraphAuthoringPortDirection.Output,
                    expected))
                return false;
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseDynamicPort port in callSite.dynamicPorts ??
                         new List<AgentPackagePoseDynamicPort>())
            {
                if (port == null || !actual.Add(port.interfacePortId) ||
                    !expected.TryGetValue(port.interfacePortId, out DocumentSignaturePort expectedPort) ||
                    !Enum.TryParse(
                        port.direction,
                        false,
                        out GraphAuthoringPortDirection direction) ||
                    direction != expectedPort.Direction ||
                    !string.Equals(
                        port.valueType,
                        expectedPort.ValueType,
                        StringComparison.Ordinal) ||
                    port.required != expectedPort.Required)
                    return false;
            }
            return actual.SetEquals(expected.Keys);
        }

        static bool AddSignaturePorts(
            IEnumerable<AgentPackagePoseDynamicPort> ports,
            GraphAuthoringPortDirection childDirection,
            GraphAuthoringPortDirection callDirection,
            IDictionary<string, DocumentSignaturePort> target)
        {
            foreach (AgentPackagePoseDynamicPort port in ports ??
                         Enumerable.Empty<AgentPackagePoseDynamicPort>())
            {
                if (port == null ||
                    !Enum.TryParse(
                        port.direction,
                        false,
                        out GraphAuthoringPortDirection direction) ||
                    direction != childDirection ||
                    !target.TryAdd(
                        port.interfacePortId,
                        new DocumentSignaturePort(
                            callDirection,
                            port.valueType,
                            port.required)))
                    return false;
            }
            return true;
        }

        static bool ValidateStateMachine(
            AgentPackagePoseStateMachineFile machine,
            IReadOnlyDictionary<string, AgentPackagePoseGraphFile> graphs,
            AgentCompileReport report)
        {
            bool valid = Identity(machine.contentRevision) &&
                         machine.entry != null &&
                         Identity(machine.entry.id) &&
                         machine.maxTransitionsPerFrame > 0;
            if (!valid)
                report.Error(
                    AgentAuthoringPresentationPackageCodec.StateMachineDirectory(machine.id) +
                    "/state-machine.json",
                    "presentation_pose_state_machine_header_invalid",
                    "Pose StateMachine revision、entry或每帧最大转换数非法。");
            var states = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseState state in machine.states ??
                         new List<AgentPackagePoseState>())
            {
                if (state == null || !Identity(state.id) ||
                    !states.Add(state.id) ||
                    !state.alwaysResetOnEntry.HasValue ||
                    !graphs.TryGetValue(
                        state.poseGraphId ?? string.Empty,
                        out AgentPackagePoseGraphFile graph) ||
                    !string.Equals(
                        graph.role,
                        CharacterPoseGraphAuthoringCapabilities
                            .StatePoseGraph.Value,
                        StringComparison.Ordinal) ||
                    !(graph.nodes ?? new List<AgentPackagePoseNode>())
                    .Any(value => string.Equals(
                        value?.id,
                        state.outputPoseNodeId,
                        StringComparison.Ordinal)))
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.StateMachineDirectory(machine.id) +
                        "/state-machine.json.states",
                        "presentation_pose_state_invalid",
                        "Pose State必须显式配置alwaysResetOnEntry，并引用root-owned state graph和现有Output节点。");
                    valid = false;
                }
            }
            if (!states.Contains(machine.entry?.targetStateId ?? string.Empty))
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.StateMachineDirectory(machine.id) +
                    "/state-machine.json.entry",
                    "presentation_pose_state_machine_entry_invalid",
                    "Pose StateMachine entry必须指向现有State。");
                valid = false;
            }

            var aliases = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseStateAlias alias in machine.aliases ??
                         new List<AgentPackagePoseStateAlias>())
            {
                if (alias == null || !Identity(alias.id) ||
                    !aliases.Add(alias.id) || alias.sources == null ||
                    alias.sources.Count == 0)
                {
                    valid = false;
                }
            }
            foreach (AgentPackagePoseStateAlias alias in machine.aliases ??
                         new List<AgentPackagePoseStateAlias>())
            {
                foreach (AgentPackagePoseTransitionSource source in
                         alias?.sources ??
                         new List<AgentPackagePoseTransitionSource>())
                    valid &= Source(source, states, aliases);
            }

            var transitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackagePoseTransition transition in
                     machine.transitions ??
                     new List<AgentPackagePoseTransition>())
            {
                bool transitionValid = transition != null &&
                    Identity(transition.id) &&
                    transitions.Add(transition.id) &&
                    Source(transition.source, states, aliases) &&
                    states.Contains(transition.targetStateId ?? string.Empty) &&
                    transition.priority >= 0 &&
                    transition.rule != null &&
                    Identity(transition.rule.id) &&
                    Identity(transition.rule.contentRevision) &&
                    Identity(transition.rule.outputOperationId) &&
                    transition.rule.operations != null &&
                    CharacterPoseStateTransition.IsValidDocumentBlendSettings(
                        transition.blendLogic,
                        transition.durationSeconds,
                        transition.blendMode,
                        Identity(transition.customBlendCurveAssetId),
                        Identity(transition.blendProfileAssetId));
                if (!transitionValid)
                {
                    report.Error(
                        AgentAuthoringPresentationPackageCodec.StateMachineDirectory(machine.id) +
                        $"/state-machine.json.transitions[{transition?.id}]",
                        "presentation_pose_transition_invalid",
                        "Pose Transition source、target、rule或blend字段非法。");
                    valid = false;
                }
            }
            if (!valid)
            {
                report.Error(
                    AgentAuthoringPresentationPackageCodec.StateMachineDirectory(machine.id) + "/state-machine.json",
                    "presentation_pose_state_machine_invalid",
                    "Pose StateMachine entry、state、alias、transition或rule非法。");
            }
            return valid;
        }

        static bool Source(
            AgentPackagePoseTransitionSource source,
            HashSet<string> states,
            HashSet<string> aliases)
        {
            if (source == null ||
                !Enum.TryParse(
                    source.kind,
                    false,
                    out PoseStateTransitionSourceKind kind))
                return false;
            return kind == PoseStateTransitionSourceKind.State
                ? Identity(source.stateId) &&
                  string.IsNullOrEmpty(source.aliasId) &&
                  states.Contains(source.stateId)
                : Identity(source.aliasId) &&
                  string.IsNullOrEmpty(source.stateId) &&
                  aliases.Contains(source.aliasId);
        }

        internal static bool RejectInternalFields(
            JToken token,
            string path,
            AgentCompileReport report)
        {
            bool valid = true;
            if (token is JObject value)
            {
                foreach (JProperty property in value.Properties())
                {
                    string name = property.Name;
                    if (name.StartsWith("m_", StringComparison.Ordinal) ||
                        string.Equals(name, "typeName", StringComparison.Ordinal) ||
                        name.IndexOf(
                            "serializedProperty",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(
                            "runtime",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(
                            "compiled",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(
                            "generated",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(
                            "projection",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(
                            "cache",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.Error(
                            path + "." + name,
                            "presentation_internal_field_forbidden",
                            "Presentation Document禁止C#类型名、SerializedProperty path、runtime、Projection、generated或cache字段。");
                        valid = false;
                    }
                    valid &= RejectInternalFields(
                        property.Value,
                        path + "." + name,
                        report);
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                    valid &= RejectInternalFields(
                        array[i],
                        path + $"[{i}]",
                        report);
            }
            return valid;
        }

        internal static bool AssetToken(JToken token)
        {
            if (!(token is JObject value))
                return false;
            var reference = new AgentPackageObjectReference
            {
                assetPath = value["assetPath"]?.Value<string>(),
                assetGuid = value["assetGuid"]?.Value<string>(),
                localFileId = value["localFileId"]?.Value<long>() ?? 0,
                localId = value["localId"]?.Value<string>()
            };
            return AssetReference(reference) &&
                   value.Properties().Select(property => property.Name)
                       .ToHashSet(StringComparer.Ordinal)
                       .SetEquals(LocalIdentity(reference.localId)
                           ? new[] { "localId" }
                           : new[] { "assetPath", "assetGuid", "localFileId" });
        }

        internal static bool Asset(AgentPackageObjectReference value) =>
            value != null &&
            string.IsNullOrWhiteSpace(value.localId) &&
            !string.IsNullOrWhiteSpace(value.assetPath) &&
            value.assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
            !value.assetPath.Contains("\\") &&
            value.localFileId != 0 && value.assetGuid?.Length == 32 &&
            value.assetGuid.All(character =>
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f');

        internal static bool AssetReference(AgentPackageObjectReference value) =>
            Asset(value) ||
            value != null && LocalIdentity(value.localId) &&
            string.IsNullOrWhiteSpace(value.assetPath) &&
            string.IsNullOrWhiteSpace(value.assetGuid) &&
            value.localFileId == 0;

        internal static string ReferenceIdentity(AgentPackageObjectReference value) =>
            value == null
                ? string.Empty
                : LocalIdentity(value.localId)
                    ? value.localId
                    : value.assetGuid + ":" + value.localFileId;

        internal static bool Identity(string value) =>
            !string.IsNullOrWhiteSpace(value);

        internal static bool LocalIdentity(string value) =>
            value?.StartsWith("local:", StringComparison.Ordinal) == true &&
            !string.IsNullOrWhiteSpace(value.Substring("local:".Length));

        internal static bool TryFile<T>(
            IReadOnlyDictionary<string, JToken> files,
            string path,
            AgentCompileReport report,
            out T value)
        {
            files.TryGetValue(path, out JToken token);
            return AgentAuthoringDocumentCodec.TryConvertToken(
                token,
                path,
                report,
                out value);
        }

        sealed class NodeContract
        {
            readonly Dictionary<string, PortContract> m_Ports;

            public NodeContract(
                IReadOnlyList<GraphAuthoringDynamicPortProjection>
                    portShape)
            {
                m_Ports = (portShape ??
                    Array.Empty<GraphAuthoringDynamicPortProjection>())
                    .ToDictionary(
                    value => value.PortId.Value,
                    value => new PortContract(
                        value.ValueTypeId,
                        value.Direction),
                    StringComparer.Ordinal);
            }

            public bool TryPort(string id, out PortContract port) =>
                m_Ports.TryGetValue(id ?? string.Empty, out port);
        }

        readonly struct PortContract
        {
            public PortContract(
                string valueType,
                GraphAuthoringPortDirection direction)
            {
                ValueType = valueType;
                Direction = direction;
            }

            public string ValueType { get; }
            public GraphAuthoringPortDirection Direction { get; }
        }

        readonly struct DocumentSignaturePort
        {
            public DocumentSignaturePort(
                GraphAuthoringPortDirection direction,
                string valueType,
                bool required)
            {
                Direction = direction;
                ValueType = valueType;
                Required = required;
            }

            public GraphAuthoringPortDirection Direction { get; }
            public string ValueType { get; }
            public bool Required { get; }
        }
    }
}
