using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentAuthoringPresentationPackageCodec
    {
        const string ProfilePath = "editable/presentation/profile.json";
        const string SequencePrefix = "editable/animation-sequences/";
        const string GraphPrefix = "editable/presentation/pose-graphs/";
        const string StateMachinePrefix =
            "editable/presentation/pose-state-machines/";
        const string InterfacePrefix =
            "readonly/presentation/linked-pose-interfaces/";
        const string ImplementationPrefix =
            "editable/presentation/linked-pose-implementations/";

        public static void WriteReadonly(
            IDictionary<string, JToken> files,
            AgentDocumentPresentationContext presentation,
            AgentCompileReport report)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseInterfaceFile value in
                     presentation?.linkedPoseInterfaces ??
                     new List<AgentPackageLinkedPoseInterfaceFile>())
            {
                string path = InterfacePath(value?.id);
                if (value == null || !Identity(value.id) ||
                    !identities.Add(value.id))
                {
                    report.Error(
                        InterfacePrefix,
                        "linked_pose_interface_identity_invalid",
                        "Linked Pose Interface context identity缺失或重复。");
                    continue;
                }
                files.Add(path, AgentAuthoringDocumentCodec.ToToken(value));
            }
        }

        public static void Write(
            IDictionary<string, JToken> files,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            if (presentation?.profile == null)
            {
                report.Error(
                    ProfilePath,
                    "presentation_profile_missing",
                    "Character Document v3缺少Presentation Profile目标状态。");
                return;
            }
            files.Add(
                ProfilePath,
                AgentAuthoringDocumentCodec.ToToken(presentation.profile));
            foreach (AgentPackageAnimationSequenceFile sequence in presentation.animationSequences ??
                         new List<AgentPackageAnimationSequenceFile>())
            {
                string directory = SequenceDirectory(sequence?.id);
                files.Add(directory + "/sequence.json", AgentAuthoringDocumentCodec.ToToken(sequence));
                AgentPackageAnimationSequenceCurvesFile curves =
                    presentation.animationSequenceCurves?.SingleOrDefault(value =>
                        string.Equals(value?.sequenceId, sequence?.id, StringComparison.Ordinal));
                if (curves == null)
                {
                    report.Error(directory + "/curves.json", "animation_sequence_curves_missing", "Animation Sequence缺少curves.json分片。");
                    continue;
                }
                files.Add(directory + "/curves.json", AgentAuthoringDocumentCodec.ToToken(curves));
            }
            foreach (AgentPackagePoseGraphFile graph in presentation.poseGraphs ??
                         new List<AgentPackagePoseGraphFile>())
            {
                string directory = GraphDirectory(graph?.id);
                files.Add(
                    directory + "/graph.json",
                    AgentAuthoringDocumentCodec.ToToken(graph));
                AgentPackagePoseGraphLayoutFile layout =
                    presentation.poseGraphLayouts?.SingleOrDefault(
                        value => string.Equals(
                            value?.graphId,
                            graph?.id,
                            StringComparison.Ordinal));
                if (layout == null)
                {
                    report.Error(
                        directory + "/layout.json",
                        "presentation_pose_layout_missing",
                        $"Pose Graph '{graph?.id}'缺少layout分片。");
                    continue;
                }
                files.Add(
                    directory + "/layout.json",
                    AgentAuthoringDocumentCodec.ToToken(layout));
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     presentation.poseStateMachines ??
                     new List<AgentPackagePoseStateMachineFile>())
            {
                string directory = StateMachineDirectory(machine?.id);
                files.Add(
                    directory + "/state-machine.json",
                    AgentAuthoringDocumentCodec.ToToken(machine));
                AgentPackagePoseStateMachineLayoutFile layout =
                    presentation.poseStateMachineLayouts?.SingleOrDefault(
                        value => string.Equals(
                            value?.stateMachineId,
                            machine?.id,
                            StringComparison.Ordinal));
                if (layout == null)
                {
                    report.Error(
                        directory + "/layout.json",
                        "presentation_pose_state_machine_layout_missing",
                        $"Pose StateMachine '{machine?.id}'缺少layout分片。");
                    continue;
                }
                files.Add(
                    directory + "/layout.json",
                    AgentAuthoringDocumentCodec.ToToken(layout));
            }
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     presentation.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                WriteImplementation(files, implementation, report);
            }
        }

        static void WriteImplementation(
            IDictionary<string, JToken> files,
            AgentPackageLinkedPoseImplementationFile implementation,
            AgentCompileReport report)
        {
            string directory = ImplementationDirectory(implementation?.id);
            if (implementation == null || !Identity(implementation.id))
            {
                report.Error(
                    ImplementationPrefix,
                    "linked_pose_implementation_identity_invalid",
                    "Linked Pose Implementation identity缺失。");
                return;
            }
            JObject header = (JObject)AgentAuthoringDocumentCodec.ToToken(
                implementation);
            header.Remove(nameof(implementation.poseGraphs));
            header.Remove(nameof(implementation.poseGraphLayouts));
            header.Remove(nameof(implementation.poseStateMachines));
            header.Remove(nameof(implementation.poseStateMachineLayouts));
            files.Add(directory + "/implementation.json", header);
            foreach (AgentPackagePoseGraphFile graph in implementation.poseGraphs ??
                         new List<AgentPackagePoseGraphFile>())
            {
                string graphDirectory = ImplementationGraphDirectory(
                    implementation.id,
                    graph?.id);
                files.Add(
                    graphDirectory + "/graph.json",
                    AgentAuthoringDocumentCodec.ToToken(graph));
                AgentPackagePoseGraphLayoutFile layout =
                    implementation.poseGraphLayouts?.SingleOrDefault(value =>
                        string.Equals(
                            value?.graphId,
                            graph?.id,
                            StringComparison.Ordinal));
                if (layout == null)
                {
                    report.Error(
                        graphDirectory + "/layout.json",
                        "linked_pose_entry_layout_missing",
                        $"Linked Pose graph '{graph?.id}'缺少layout分片。");
                    continue;
                }
                files.Add(
                    graphDirectory + "/layout.json",
                    AgentAuthoringDocumentCodec.ToToken(layout));
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     implementation.poseStateMachines ??
                     new List<AgentPackagePoseStateMachineFile>())
            {
                string machineDirectory = ImplementationStateMachineDirectory(
                    implementation.id,
                    machine?.id);
                files.Add(
                    machineDirectory + "/state-machine.json",
                    AgentAuthoringDocumentCodec.ToToken(machine));
                AgentPackagePoseStateMachineLayoutFile layout =
                    implementation.poseStateMachineLayouts?.SingleOrDefault(value =>
                        string.Equals(
                            value?.stateMachineId,
                            machine?.id,
                            StringComparison.Ordinal));
                if (layout == null)
                {
                    report.Error(
                        machineDirectory + "/layout.json",
                        "linked_pose_state_machine_layout_missing",
                        $"Linked Pose StateMachine '{machine?.id}'缺少layout分片。");
                    continue;
                }
                files.Add(
                    machineDirectory + "/layout.json",
                    AgentAuthoringDocumentCodec.ToToken(layout));
            }
        }

        public static bool TryRead(
            IReadOnlyDictionary<string, JToken> files,
            AgentCompileReport report,
            out AgentDocumentPresentationEditable presentation)
        {
            presentation = new AgentDocumentPresentationEditable();
            bool valid = ValidateFileSet(files, report);
            valid &= TryFile(
                files,
                ProfilePath,
                report,
                out AgentPackagePresentationProfileFile profile);
            presentation.profile = profile;

            foreach (string path in files.Keys.Where(IsSequenceFile).OrderBy(value => value, StringComparer.Ordinal))
            {
                string curvesPath = path.Substring(0, path.Length - "sequence.json".Length) + "curves.json";
                if (!TryFile(files, path, report, out AgentPackageAnimationSequenceFile sequence) ||
                    !TryFile(files, curvesPath, report, out AgentPackageAnimationSequenceCurvesFile curves))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(path, SequenceDirectory(sequence.id) + "/sequence.json", StringComparison.Ordinal) ||
                    !string.Equals(curves.sequenceId, sequence.id, StringComparison.Ordinal))
                {
                    report.Error(path, "animation_sequence_path_mismatch", "Animation Sequence目录、identity与curves owner必须一致。");
                    valid = false;
                    continue;
                }
                presentation.animationSequences.Add(sequence);
                presentation.animationSequenceCurves.Add(curves);
            }

            foreach (string graphPath in files.Keys
                         .Where(IsGraphFile)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = graphPath.Substring(
                    0,
                    graphPath.Length - "graph.json".Length) + "layout.json";
                if (!TryFile(
                        files,
                        graphPath,
                        report,
                        out AgentPackagePoseGraphFile graph) ||
                    !TryFile(
                        files,
                        layoutPath,
                        report,
                        out AgentPackagePoseGraphLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                string expected = GraphDirectory(graph.id) + "/graph.json";
                if (!string.Equals(graphPath, expected, StringComparison.Ordinal) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(
                        graphPath,
                        "presentation_pose_graph_path_mismatch",
                        "Pose Graph目录、graph id与layout graphId必须一致。");
                    valid = false;
                    continue;
                }
                presentation.poseGraphs.Add(graph);
                presentation.poseGraphLayouts.Add(layout);
            }

            foreach (string path in files.Keys
                         .Where(IsStateMachineFile)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = path.Substring(
                    0,
                    path.Length - "state-machine.json".Length) +
                    "layout.json";
                if (!TryFile(
                        files,
                        path,
                        report,
                        out AgentPackagePoseStateMachineFile machine) ||
                    !TryFile(
                        files,
                        layoutPath,
                        report,
                        out AgentPackagePoseStateMachineLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                string expected =
                    StateMachineDirectory(machine.id) + "/state-machine.json";
                if (!string.Equals(path, expected, StringComparison.Ordinal) ||
                    !string.Equals(
                        layout.stateMachineId,
                        machine.id,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        path,
                        "presentation_pose_state_machine_path_mismatch",
                        "Pose StateMachine目录、state-machine id与layout stateMachineId必须一致。");
                    valid = false;
                    continue;
                }
                if (!files.TryGetValue(layoutPath, out JToken layoutToken) ||
                    layoutToken?["elements"] is not JArray)
                {
                    report.Error(
                        layoutPath,
                        "presentation_pose_state_machine_layout_invalid",
                        "Pose StateMachine layout必须显式提供elements数组。");
                    valid = false;
                    continue;
                }
                presentation.poseStateMachines.Add(machine);
                presentation.poseStateMachineLayouts.Add(layout);
            }

            foreach (string path in files.Keys
                         .Where(IsImplementationFile)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryReadImplementation(
                        files,
                        path,
                        report,
                        out AgentPackageLinkedPoseImplementationFile implementation))
                {
                    valid = false;
                    continue;
                }
                presentation.linkedPoseImplementations.Add(implementation);
            }

            valid &= ValidatePresentation(presentation, report);
            return valid;
        }

        public static bool TryReadReadonly(
            IReadOnlyDictionary<string, JToken> files,
            AgentCompileReport report,
            out List<AgentPackageLinkedPoseInterfaceFile> interfaces)
        {
            interfaces = new List<AgentPackageLinkedPoseInterfaceFile>();
            bool valid = true;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in files.Keys
                         .Where(IsInterfaceFile)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryFile(
                        files,
                        path,
                        report,
                        out AgentPackageLinkedPoseInterfaceFile value))
                {
                    valid = false;
                    continue;
                }
                if (value == null || !Identity(value.id) ||
                    !identities.Add(value.id) ||
                    !string.Equals(path, InterfacePath(value.id), StringComparison.Ordinal) ||
                    !ValidateInterface(value, path, report))
                {
                    report.Error(
                        path,
                        "linked_pose_interface_context_invalid",
                        "Linked Pose Interface context的identity、路径或签名合同非法。");
                    valid = false;
                    continue;
                }
                interfaces.Add(value);
            }
            return valid;
        }

        public static bool ValidateReadonlyClosure(
            AgentDocumentPresentationEditable presentation,
            IReadOnlyCollection<AgentPackageLinkedPoseInterfaceFile> interfaces,
            AgentCompileReport report)
        {
            HashSet<string> available = (interfaces ??
                    Array.Empty<AgentPackageLinkedPoseInterfaceFile>())
                .Where(value => value?.asset != null)
                .Select(value => ReferenceIdentity(value.asset))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> required = (presentation?.profile?.linkedPoseGroups ??
                    new List<AgentPackageLinkedPoseGroupBinding>())
                .Where(value => value?.interfaceAsset != null)
                .Select(value => ReferenceIdentity(value.interfaceAsset))
                .Concat((presentation?.linkedPoseImplementations ??
                         new List<AgentPackageLinkedPoseImplementationFile>())
                    .Where(value => value?.interfaceAsset != null)
                    .Select(value => ReferenceIdentity(value.interfaceAsset)))
                .ToHashSet(StringComparer.Ordinal);
            if (available.SetEquals(required))
                return true;
            report.Error(
                InterfacePrefix,
                "linked_pose_interface_context_closure_invalid",
                "Readonly Interface context必须精确覆盖Group与Implementation引用的Interface集合。");
            return false;
        }

        static bool TryReadImplementation(
            IReadOnlyDictionary<string, JToken> files,
            string path,
            AgentCompileReport report,
            out AgentPackageLinkedPoseImplementationFile implementation)
        {
            implementation = null;
            if (files.TryGetValue(path, out JToken headerToken) &&
                (headerToken?[nameof(implementation.poseGraphs)] != null ||
                 headerToken?[nameof(implementation.poseGraphLayouts)] != null ||
                 headerToken?[nameof(implementation.poseStateMachines)] != null ||
                 headerToken?[nameof(implementation.poseStateMachineLayouts)] != null))
            {
                report.Error(
                    path,
                    "linked_pose_implementation_inline_graph_forbidden",
                    "Implementation Graph与layout必须使用独立分片，不能内联进implementation.json。");
                return false;
            }
            if (!TryFile(files, path, report, out implementation) ||
                implementation == null)
                return false;
            string directory = ImplementationDirectory(implementation.id);
            if (!string.Equals(
                    path,
                    directory + "/implementation.json",
                    StringComparison.Ordinal))
            {
                report.Error(
                    path,
                    "linked_pose_implementation_path_mismatch",
                    "Linked Pose Implementation目录必须使用implementation object identity的canonical segment。");
                return false;
            }
            bool valid = true;
            foreach (string graphPath in files.Keys
                         .Where(value =>
                             value.StartsWith(directory + "/pose-graphs/", StringComparison.Ordinal) &&
                             value.EndsWith("/graph.json", StringComparison.Ordinal))
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = graphPath.Substring(
                    0,
                    graphPath.Length - "graph.json".Length) + "layout.json";
                if (!TryFile(files, graphPath, report, out AgentPackagePoseGraphFile graph) ||
                    !TryFile(files, layoutPath, report, out AgentPackagePoseGraphLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(
                        graphPath,
                        ImplementationGraphDirectory(implementation.id, graph.id) +
                        "/graph.json",
                        StringComparison.Ordinal) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(
                        graphPath,
                        "linked_pose_graph_path_mismatch",
                        "Linked Pose graph目录、graph id与layout graphId必须一致。");
                    valid = false;
                    continue;
                }
                implementation.poseGraphs.Add(graph);
                implementation.poseGraphLayouts.Add(layout);
            }
            foreach (string machinePath in files.Keys
                         .Where(value =>
                             value.StartsWith(directory + "/pose-state-machines/", StringComparison.Ordinal) &&
                             value.EndsWith("/state-machine.json", StringComparison.Ordinal))
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = machinePath.Substring(
                    0,
                    machinePath.Length - "state-machine.json".Length) +
                    "layout.json";
                if (!TryFile(files, machinePath, report, out AgentPackagePoseStateMachineFile machine) ||
                    !TryFile(files, layoutPath, report, out AgentPackagePoseStateMachineLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(
                        machinePath,
                        ImplementationStateMachineDirectory(implementation.id, machine.id) +
                        "/state-machine.json",
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        layout.stateMachineId,
                        machine.id,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        machinePath,
                        "linked_pose_state_machine_path_mismatch",
                        "Linked Pose StateMachine目录、id与layout owner必须一致。");
                    valid = false;
                    continue;
                }
                implementation.poseStateMachines.Add(machine);
                implementation.poseStateMachineLayouts.Add(layout);
            }
            valid &= ValidateImplementation(implementation, path, report);
            return valid;
        }

        public static bool TryReadContent(
            string relativePath,
            string fullPath,
            AgentCompileReport report,
            out JToken raw)
        {
            raw = null;
            if (string.Equals(relativePath, ProfilePath, StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePresentationProfileFile _,
                    out raw);
            if (IsSequenceFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageAnimationSequenceFile _, out raw);
            if (IsSequenceCurvesFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageAnimationSequenceCurvesFile _, out raw);
            if (IsGraphFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseGraphFile _,
                    out raw);
            if (IsLayoutFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseGraphLayoutFile _,
                    out raw);
            if (IsStateMachineFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseStateMachineFile _,
                    out raw);
            if (IsStateMachineLayoutFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseStateMachineLayoutFile _,
                    out raw);
            if (IsInterfaceFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackageLinkedPoseInterfaceFile _,
                    out raw);
            if (IsImplementationFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackageLinkedPoseImplementationFile _,
                    out raw);
            if (IsImplementationGraphFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseGraphFile _,
                    out raw);
            if (IsImplementationGraphLayoutFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseGraphLayoutFile _,
                    out raw);
            if (IsImplementationStateMachineFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseStateMachineFile _,
                    out raw);
            if (IsImplementationStateMachineLayoutFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(
                    fullPath,
                    report,
                    out AgentPackagePoseStateMachineLayoutFile _,
                    out raw);
            report.Error(
                relativePath,
                "presentation_file_unknown",
                "Document v3包含未知Presentation文件。");
            return false;
        }

        internal static bool IsDiscoverablePoseGraphFragment(string path) =>
            IsGraphFile(path) || IsLayoutFile(path);

        internal static bool TryDiscoverNewPoseGraphFragments(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in candidates.Keys
                         .Select(DirectoryPath)
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string graphPath = directory + "/graph.json";
                string layoutPath = directory + "/layout.json";
                if (!candidates.ContainsKey(graphPath) ||
                    !candidates.ContainsKey(layoutPath))
                {
                    report.Error(
                        directory,
                        "presentation_new_pose_graph_pair_incomplete",
                        "新增Pose Graph必须同时提供同目录graph.json与layout.json。");
                    valid = false;
                    continue;
                }
                if (!TryFile(
                        candidates,
                        graphPath,
                        report,
                        out AgentPackagePoseGraphFile graph) ||
                    !TryFile(
                        candidates,
                        layoutPath,
                        report,
                        out AgentPackagePoseGraphLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                if (!LocalIdentity(graph.id))
                {
                    report.Error(
                        graphPath + ".id",
                        "presentation_new_pose_graph_identity_not_local",
                        "新增Pose Graph必须使用local:<meaningful-id> identity。");
                    valid = false;
                    continue;
                }
                if (!string.Equals(
                        graph.role,
                        CharacterPoseGraphAuthoringCapabilities
                            .StatePoseGraph.Value,
                        StringComparison.Ordinal) &&
                    !string.Equals(
                        graph.role,
                        CharacterPoseGraphAuthoringCapabilities
                            .Subgraph.Value,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        graphPath + ".role",
                        "presentation_new_pose_graph_role_invalid",
                        "新增Pose Graph只允许state graph或subgraph role，不能创建第二个root graph。");
                    valid = false;
                    continue;
                }
                string expectedDirectory = GraphDirectory(graph.id);
                if (!string.Equals(
                        directory,
                        expectedDirectory,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        layout.graphId,
                        graph.id,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        graphPath,
                        "presentation_new_pose_graph_path_mismatch",
                        "新增Pose Graph目录必须使用graph local identity的canonical segment，layout graphId必须与graph id一致。");
                    valid = false;
                    continue;
                }
                result.Add(graphPath);
                result.Add(layoutPath);
            }
            discovered = result;
            return valid;
        }

        internal static bool IsDiscoverableLinkedPoseFragment(string path) =>
            IsImplementationFile(path) ||
            IsImplementationGraphFile(path) ||
            IsImplementationGraphLayoutFile(path) ||
            IsImplementationStateMachineFile(path) ||
            IsImplementationStateMachineLayoutFile(path);

        internal static bool TryDiscoverNewLinkedPoseFragments(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in candidates.Keys
                         .Select(ImplementationOwnerDirectory)
                         .Where(value => !string.IsNullOrEmpty(value))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string implementationPath = directory + "/implementation.json";
                if (candidates.ContainsKey(implementationPath))
                {
                    if (!TryReadImplementation(
                            candidates,
                            implementationPath,
                            report,
                            out AgentPackageLinkedPoseImplementationFile implementation) ||
                        !LocalIdentity(implementation.id) ||
                        !LocalIdentity(implementation.asset?.localId) ||
                        !LocalIdentity(implementation.graphOwner?.localId))
                    {
                        report.Error(
                            implementationPath,
                            "linked_pose_new_implementation_invalid",
                            "新增Linked Pose Implementation及Graph owner必须使用local:* identity并提供完整canonical闭包。");
                        valid = false;
                        continue;
                    }
                    foreach (string path in candidates.Keys.Where(value =>
                                 value.StartsWith(directory + "/", StringComparison.Ordinal)))
                        result.Add(path);
                    continue;
                }

                foreach (string graphPath in candidates.Keys.Where(value =>
                             value.StartsWith(directory + "/pose-graphs/", StringComparison.Ordinal) &&
                             IsImplementationGraphFile(value)))
                {
                    string layoutPath = graphPath.Substring(
                        0,
                        graphPath.Length - "graph.json".Length) + "layout.json";
                    if (!candidates.ContainsKey(layoutPath) ||
                        !TryFile(candidates, graphPath, report, out AgentPackagePoseGraphFile graph) ||
                        !TryFile(candidates, layoutPath, report, out AgentPackagePoseGraphLayoutFile layout) ||
                        !LocalIdentity(graph.id) ||
                        !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal) ||
                        !string.Equals(
                            graphPath,
                            directory + "/pose-graphs/" +
                            AgentAuthoringPackageMapper.Segment(graph.id) +
                            "/graph.json",
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            graph.role,
                            CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry.Value,
                            StringComparison.Ordinal) &&
                        !string.Equals(
                            graph.role,
                            CharacterPoseGraphAuthoringCapabilities.StatePoseGraph.Value,
                            StringComparison.Ordinal) &&
                        !string.Equals(
                            graph.role,
                            CharacterPoseGraphAuthoringCapabilities.Subgraph.Value,
                            StringComparison.Ordinal))
                    {
                        report.Error(
                            graphPath,
                            "linked_pose_new_graph_pair_invalid",
                            "新增Linked Pose graph必须使用local:* identity、canonical目录和允许的Entry/state/subgraph role。");
                        valid = false;
                        continue;
                    }
                    result.Add(graphPath);
                    result.Add(layoutPath);
                }
                foreach (string machinePath in candidates.Keys.Where(value =>
                             value.StartsWith(directory + "/pose-state-machines/", StringComparison.Ordinal) &&
                             IsImplementationStateMachineFile(value)))
                {
                    string layoutPath = machinePath.Substring(
                        0,
                        machinePath.Length - "state-machine.json".Length) +
                        "layout.json";
                    if (!candidates.ContainsKey(layoutPath) ||
                        !TryFile(candidates, machinePath, report, out AgentPackagePoseStateMachineFile machine) ||
                        !TryFile(candidates, layoutPath, report, out AgentPackagePoseStateMachineLayoutFile layout) ||
                        !LocalIdentity(machine.id) ||
                        !string.Equals(layout.stateMachineId, machine.id, StringComparison.Ordinal) ||
                        !string.Equals(
                            machinePath,
                            directory + "/pose-state-machines/" +
                            AgentAuthoringPackageMapper.Segment(machine.id) +
                            "/state-machine.json",
                            StringComparison.Ordinal))
                    {
                        report.Error(
                            machinePath,
                            "linked_pose_new_state_machine_pair_invalid",
                            "新增Linked Pose StateMachine必须使用local:* identity与canonical pair。");
                        valid = false;
                        continue;
                    }
                    result.Add(machinePath);
                    result.Add(layoutPath);
                }
            }
            discovered = result;
            return valid;
        }

        internal static bool TryDiscoverRemovedLinkedPoseFragments(
            IReadOnlyCollection<string> declaredPaths,
            IReadOnlyCollection<string> actualPaths,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var declared = new HashSet<string>(
                declaredPaths ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var actual = new HashSet<string>(
                actualPaths ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var missing = declared.Except(actual, StringComparer.Ordinal)
                .Where(IsDiscoverableLinkedPoseFragment)
                .ToHashSet(StringComparer.Ordinal);
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in missing
                         .Select(ImplementationOwnerDirectory)
                         .Where(value => !string.IsNullOrEmpty(value))
                         .Distinct(StringComparer.Ordinal))
            {
                string implementationPath = directory + "/implementation.json";
                if (!missing.Contains(implementationPath))
                    continue;
                string[] closure = declared.Where(value =>
                        value.StartsWith(directory + "/", StringComparison.Ordinal))
                    .ToArray();
                if (closure.Any(actual.Contains))
                {
                    report.Error(
                        implementationPath,
                        "linked_pose_implementation_remove_closure_incomplete",
                        "删除Linked Pose Implementation必须删除implementation与全部嵌套Graph闭包。");
                    valid = false;
                    continue;
                }
                result.UnionWith(closure);
            }
            discovered = result;
            return valid;
        }

        static bool ValidateFileSet(
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
                    if (!IsInterfaceFile(pair.Key))
                    {
                        report.Error(
                            pair.Key,
                            "presentation_readonly_file_unknown",
                            "Document v3包含未知Presentation readonly文件。");
                        valid = false;
                    }
                    valid &= RejectInternalFields(pair.Value, pair.Key, report);
                    continue;
                }
                if (!pair.Key.StartsWith(
                        "editable/presentation/",
                        StringComparison.Ordinal) &&
                    !pair.Key.StartsWith(SequencePrefix, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(
                        pair.Key,
                        ProfilePath,
                        StringComparison.Ordinal) &&
                    !IsGraphFile(pair.Key) &&
                    !IsSequenceFile(pair.Key) &&
                    !IsSequenceCurvesFile(pair.Key) &&
                    !IsLayoutFile(pair.Key) &&
                    !IsStateMachineFile(pair.Key) &&
                    !IsStateMachineLayoutFile(pair.Key) &&
                    !IsImplementationFile(pair.Key) &&
                    !IsImplementationGraphFile(pair.Key) &&
                    !IsImplementationGraphLayoutFile(pair.Key) &&
                    !IsImplementationStateMachineFile(pair.Key) &&
                    !IsImplementationStateMachineLayoutFile(pair.Key))
                {
                    report.Error(
                        pair.Key,
                        "presentation_file_unknown",
                        "Document v3包含未知Presentation文件。");
                    valid = false;
                }
                valid &= RejectInternalFields(pair.Value, pair.Key, report);
            }
            foreach (string sequencePath in files.Keys.Where(IsSequenceFile))
            {
                string curvesPath = sequencePath.Substring(0, sequencePath.Length - "sequence.json".Length) + "curves.json";
                if (!files.ContainsKey(curvesPath))
                {
                    report.Error(sequencePath, "animation_sequence_curves_pair_missing", "Animation Sequence缺少同目录curves.json。");
                    valid = false;
                }
            }
            foreach (string curvesPath in files.Keys.Where(IsSequenceCurvesFile))
            {
                string sequencePath = curvesPath.Substring(0, curvesPath.Length - "curves.json".Length) + "sequence.json";
                if (!files.ContainsKey(sequencePath))
                {
                    report.Error(curvesPath, "animation_sequence_pair_missing", "Animation Sequence curves缺少同目录sequence.json。");
                    valid = false;
                }
            }
            foreach (string layoutPath in files.Keys.Where(IsLayoutFile))
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
            foreach (string machinePath in files.Keys.Where(IsStateMachineFile))
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
                         IsStateMachineLayoutFile))
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
                         IsImplementationGraphLayoutFile))
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
                         IsImplementationGraphFile))
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
                         IsImplementationStateMachineLayoutFile))
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
                         IsImplementationStateMachineFile))
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

        static bool ValidatePresentation(
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            bool valid = ValidateSequences(presentation, report);
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
                        GraphPrefix,
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
                        GraphPrefix,
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
                            GraphDirectory(layout.graphId) + "/layout.json",
                            "presentation_pose_layout_invalid",
                            "Pose Graph layout节点缺失、重复或坐标非法。");
                        valid = false;
                    }
                }
                if (!graphNodes.SetEquals(layoutNodes))
                {
                    report.Error(
                        GraphDirectory(layout.graphId) + "/layout.json",
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
                        StateMachinePrefix,
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
                        StateMachinePrefix,
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
                        StateMachineDirectory(layout.stateMachineId) +
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
                            StateMachineDirectory(layout.stateMachineId) +
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
                    StateMachinePrefix,
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
                        ImplementationPrefix,
                        "linked_pose_implementation_identity_invalid",
                        "Linked Pose Implementation对象identity或业务identity缺失、重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateImplementation(
                    implementation,
                    ImplementationDirectory(implementation.id) +
                    "/implementation.json",
                    report);
            }
            valid &= ValidateLinkedProfile(
                presentation.profile,
                implementations.Values,
                report);

            foreach (AgentPackagePoseNode node in nodes.Values)
            {
                bool stateMachine = string.Equals(
                    node.capability,
                    CharacterPoseGraphAuthoringCapabilities
                        .Get(CharacterPoseNodeKind.PoseStateMachine).Value,
                    StringComparison.Ordinal);
                if (stateMachine != !string.IsNullOrWhiteSpace(node.childDocumentId) ||
                    stateMachine &&
                    !machines.ContainsKey(node.childDocumentId))
                {
                    report.Error(
                        $"presentation.poseNodes[{node.id}].childDocumentId",
                        "presentation_pose_child_document_invalid",
                        "PoseStateMachine节点必须唯一引用现有StateMachine文档，其它节点不得声明child document。");
                    valid = false;
                }
            }
            HashSet<string> ownedMachines = nodes.Values
                .Where(value => !string.IsNullOrWhiteSpace(value.childDocumentId))
                .Select(value => value.childDocumentId)
                .ToHashSet(StringComparer.Ordinal);
            if (!ownedMachines.SetEquals(machines.Keys))
            {
                report.Error(
                    StateMachinePrefix,
                    "presentation_pose_state_machine_owner_invalid",
                    "每个Pose StateMachine必须由唯一typed节点持有。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateSequences(
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report)
        {
            bool valid = true;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var assets = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, AgentPackageAnimationSequenceCurvesFile> curves =
                (presentation.animationSequenceCurves ?? new List<AgentPackageAnimationSequenceCurvesFile>())
                .Where(value => value != null && Identity(value.sequenceId))
                .GroupBy(value => value.sequenceId, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
            foreach (AgentPackageAnimationSequenceFile sequence in presentation.animationSequences ??
                         new List<AgentPackageAnimationSequenceFile>())
            {
                string path = SequenceDirectory(sequence?.id) + "/sequence.json";
                if (sequence == null || !Identity(sequence.id) || !identities.Add(sequence.id) ||
                    !AssetReference(sequence.asset) || !assets.Add(ReferenceIdentity(sequence.asset)) ||
                    !Asset(sequence.clip) || !Asset(sequence.rig) || !Asset(sequence.footAnalysisSource) ||
                    !float.IsFinite(sequence.defaultPlayRate) || sequence.defaultPlayRate <= 0f ||
                    !Identity(sequence.contentRevision) || string.IsNullOrWhiteSpace(sequence.footAnalysisIdentity) ||
                    !Enum.TryParse(sequence.syncMode, false, out AnimationSyncMode syncMode) ||
                    !Enum.TryParse(sequence.timeMapping, false, out AnimationSyncTimeMapping timeMapping) ||
                    !Enum.TryParse(sequence.markerTopology, false, out AnimationMarkerSequenceTopology topology) ||
                    !Enum.TryParse(sequence.syncRole, false, out AnimationMarkerSyncRole syncRole) ||
                    !ValidateSequenceMarkers(sequence, syncMode, timeMapping, topology, syncRole) ||
                    !ValidateSequenceNotifies(sequence) ||
                    !curves.TryGetValue(sequence.id, out AgentPackageAnimationSequenceCurvesFile sequenceCurves) ||
                    sequenceCurves.curves == null || sequenceCurves.curves.Count == 0 ||
                    sequenceCurves.curves.Any(value => value == null || !Identity(value.channelId) || value.keys == null || value.keys.Count == 0))
                {
                    report.Error(path, "animation_sequence_invalid", "Animation Sequence owner、引用、Marker、Curve、Notify或Analysis合同非法。");
                    valid = false;
                }
            }
            if (curves.Count != identities.Count)
            {
                report.Error(SequencePrefix, "animation_sequence_curve_closure_invalid", "Animation Sequence与curves分片必须一一对应。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateSequenceMarkers(
            AgentPackageAnimationSequenceFile sequence,
            AnimationSyncMode syncMode,
            AnimationSyncTimeMapping timeMapping,
            AnimationMarkerSequenceTopology topology,
            AnimationMarkerSyncRole syncRole)
        {
            List<AgentPackageAnimationSequenceMarker> markers = sequence.markers ??
                new List<AgentPackageAnimationSequenceMarker>();
            if (syncMode == AnimationSyncMode.None)
                return timeMapping == AnimationSyncTimeMapping.Unspecified &&
                       topology == AnimationMarkerSequenceTopology.Unspecified &&
                       syncRole == AnimationMarkerSyncRole.Unspecified &&
                       string.IsNullOrEmpty(sequence.markerGroupId) && markers.Count == 0;
            if (syncMode != AnimationSyncMode.MarkerGroup || markers.Count < 2 || !Identity(sequence.markerGroupId) ||
                topology != (sequence.loop ? AnimationMarkerSequenceTopology.Cyclic : AnimationMarkerSequenceTopology.Finite) ||
                syncRole is not (AnimationMarkerSyncRole.CanBeLeader or AnimationMarkerSyncRole.AlwaysLeader or AnimationMarkerSyncRole.AlwaysFollower) ||
                timeMapping is not (AnimationSyncTimeMapping.MarkerSegmentFraction or AnimationSyncTimeMapping.GeneratedFootPhase))
                return false;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            int previousFrame = -1;
            for (int i = 0; i < markers.Count; i++)
            {
                AgentPackageAnimationSequenceMarker marker = markers[i];
                if (marker == null || !Identity(marker.id) || !Identity(marker.markerId) ||
                    !identities.Add(marker.id) || marker.frame <= previousFrame)
                    return false;
                previousFrame = marker.frame;
            }
            return true;
        }

        static bool ValidateSequenceNotifies(AgentPackageAnimationSequenceFile sequence)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageAnimationSequenceNotify notify in sequence.notifies ??
                         new List<AgentPackageAnimationSequenceNotify>())
            {
                if (notify == null || !Identity(notify.id) || !identities.Add(notify.id) || notify.frame < 0 ||
                    !Enum.TryParse(notify.kind, false, out AnimationSequenceNotifyKind kind) ||
                    string.IsNullOrWhiteSpace(notify.primaryValue) ||
                    kind is AnimationSequenceNotifyKind.FootstepAudio or AnimationSequenceNotifyKind.VisualEffect &&
                    string.IsNullOrWhiteSpace(notify.secondaryValue))
                    return false;
            }
            return true;
        }

        static bool ValidateImplementation(
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
                        ProfilePath + ".linkedPoseGroups",
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
                        ProfilePath + $".linkedPoseSelectors[{selector?.selectorId}]",
                        "linked_pose_selector_invalid",
                        "Linked Pose selector对象key、Group、Equipment映射或显式Empty Implementation非法。");
                    valid = false;
                }
            }
            if (!selectorGroups.SetEquals(groups.Keys))
            {
                report.Error(
                    ProfilePath + ".linkedPoseSelectors",
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
                    ProfilePath + ".linkedPoseSelectors",
                    "linked_pose_candidate_closure_invalid",
                    "全部Implementation必须由唯一selector映射或显式Empty Implementation精确覆盖。");
                valid = false;
            }
            return valid;
        }

        static bool SameAssetReference(
            AgentPackageAssetReferenceV3 left,
            AgentPackageAssetReferenceV3 right) =>
            string.Equals(
                ReferenceIdentity(left),
                ReferenceIdentity(right),
                StringComparison.Ordinal);

        static bool ValidateInterface(
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
                    ProfilePath,
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
                        ProfilePath + $".poseSources[{source?.name}]",
                        "presentation_pose_source_invalid",
                        $"Pose source非法：kind={source?.kind ?? "<null>"}, " +
                        $"source={ReferenceIdentity(source?.source)}, " +
                        $"contentRevision={source?.contentRevision ?? "<null>"}, footAnalysisIdentity={source?.footAnalysisIdentity ?? "<null>"}, " +
                        $"slot={slotIdentity}, binding={bindingIdentity}。");
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
                    !Identity(producer.trackId) || !producers.Add(id) ||
                    !Asset(producer.source) ||
                    string.IsNullOrWhiteSpace(producer.footAnalysisIdentity))
                {
                    report.Error(
                        ProfilePath + ".actionProducers",
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
                return (presentation.animationSequences ?? new List<AgentPackageAnimationSequenceFile>())
                    .Any(value => string.Equals(ReferenceIdentity(value?.asset), ReferenceIdentity(source.source), StringComparison.Ordinal));
            if (kind == PresentationPoseSourceKind.BlendSpace)
                return string.IsNullOrWhiteSpace(source.footAnalysisIdentity);
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
            GraphAuthoringDocumentRoleId role;
            try
            {
                role = new GraphAuthoringDocumentRoleId(graph.role);
                if (!role.Equals(CharacterPoseGraphAuthoringCapabilities.RootGraph) &&
                    !role.Equals(CharacterPoseGraphAuthoringCapabilities.StatePoseGraph) &&
                    !role.Equals(CharacterPoseGraphAuthoringCapabilities.Subgraph) &&
                    !role.Equals(CharacterPoseGraphAuthoringCapabilities.LinkedPoseEntry))
                    throw new InvalidOperationException();
            }
            catch
            {
                report.Error(
                    GraphDirectory(graph.id) + "/graph.json.role",
                    "presentation_pose_graph_role_invalid",
                    "Pose Graph role不是正式Presentation role。");
                return false;
            }
            bool valid = Identity(graph.contentRevision);
            var nodes = new Dictionary<string, NodeContract>(StringComparer.Ordinal);
            foreach (AgentPackagePoseNode node in graph.nodes ??
                         new List<AgentPackagePoseNode>())
            {
                if (node == null || !Identity(node.id) ||
                    nodes.ContainsKey(node.id) ||
                    allNodes.ContainsKey(node.id))
                {
                    report.Error(
                        GraphDirectory(graph.id) + "/graph.json.nodes",
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
                        CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                            capabilityId,
                            CharacterPoseGraphAuthoringCapabilities.Domain,
                            role);
                    valid &= ValidateNode(node, capability, graph.id, report);
                    nodes.Add(node.id, new NodeContract(node, capability));
                    allNodes.Add(node.id, node);
                }
                catch (Exception exception)
                {
                    report.Error(
                        GraphDirectory(graph.id) +
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
                        GraphDirectory(graph.id) + "/graph.json.edges",
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
            AgentCompileReport report)
        {
            bool valid = true;
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
                    GraphDirectory(graphId) +
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
                if (!ValidateValue(field.Value, value))
                {
                    report.Error(
                        GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}].properties.{field.Key}",
                        "presentation_pose_property_value_invalid",
                        "Pose node property值类型或约束非法。");
                    valid = false;
                }
            }

            var ports = new HashSet<string>(
                capability.FixedPorts.Select(value => value.PortId.Value),
                StringComparer.Ordinal);
            var interfacePorts = new HashSet<string>(StringComparer.Ordinal);
            var portOrders = new HashSet<int>();
            foreach (AgentPackagePoseDynamicPort port in node.dynamicPorts ??
                         new List<AgentPackagePoseDynamicPort>())
            {
                if (port == null || !Identity(port.id) ||
                    !ports.Add(port.id) ||
                    string.IsNullOrWhiteSpace(port.name) ||
                    !ValidPoseValueType(port.valueType) ||
                    Identity(port.interfacePortId) &&
                    !interfacePorts.Add(port.interfacePortId) ||
                    !Enum.TryParse(
                        port.direction,
                        false,
                        out GraphAuthoringPortDirection direction) ||
                    port.order < 0 || !portOrders.Add(port.order) ||
                    capability.DynamicPortPolicy ==
                    GraphAuthoringDynamicPortPolicy.None ||
                    capability.DynamicPortPolicy ==
                    GraphAuthoringDynamicPortPolicy.OrderedInputs &&
                    direction != GraphAuthoringPortDirection.Input ||
                    capability.DynamicPortPolicy ==
                    GraphAuthoringDynamicPortPolicy.OrderedOutputs &&
                    direction != GraphAuthoringPortDirection.Output)
                {
                    report.Error(
                        GraphDirectory(graphId) +
                        $"/graph.json.nodes[{node.id}].dynamicPorts",
                        "presentation_pose_dynamic_port_invalid",
                        "Pose dynamic port不符合Capability策略。");
                    valid = false;
                }
            }
            if (string.Equals(
                    node.capability,
                    CharacterPoseGraphAuthoringCapabilities
                        .Get(CharacterPoseNodeKind.AnimationSlot).Value,
                    StringComparison.Ordinal))
            {
                var binding = new AgentPackageAnimationSlotBinding
                {
                    slotId = node.properties["slot-id"]?.Value<string>(),
                    animationChannelId =
                        node.properties["animation-channel-id"]?.Value<string>()
                };
                if (!Identity(binding.slotId) ||
                    !Identity(binding.animationChannelId))
                {
                    report.Error(
                        GraphDirectory(graphId) +
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
            string subgraphCapability = CharacterPoseGraphAuthoringCapabilities
                .Get(CharacterPoseNodeKind.PoseSubgraph).Value;
            string inputCapability = CharacterPoseGraphAuthoringCapabilities
                .Get(CharacterPoseNodeKind.GraphInput).Value;
            string outputCapability = CharacterPoseGraphAuthoringCapabilities
                .Get(CharacterPoseNodeKind.GraphOutput).Value;
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
                    string childId = callSite.properties?["graph-id"]?.Value<string>();
                    if (!graphs.TryGetValue(
                            childId ?? string.Empty,
                            out AgentPackagePoseGraphFile child))
                    {
                        report.Error(
                            GraphDirectory(owner.id) + $"/graph.json.nodes[{callSite.id}]",
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
                            GraphDirectory(owner.id) + $"/graph.json.nodes[{callSite.id}].dynamicPorts",
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

        static bool ValidPoseValueType(string value) => value switch
        {
            "pose.local" => true,
            "pose.component" => true,
            "pose.parameter" => true,
            "pose.discontinuity" => true,
            "pose.action-playback" => true,
            "component.full-body-ik-goals" => true,
            _ => false
        };

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
                    StateMachineDirectory(machine.id) +
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
                        StateMachineDirectory(machine.id) +
                        "/state-machine.json.states",
                        "presentation_pose_state_invalid",
                        "Pose State必须显式配置alwaysResetOnEntry，并引用root-owned state graph和现有Output节点。");
                    valid = false;
                }
            }
            if (!states.Contains(machine.entry?.targetStateId ?? string.Empty))
            {
                report.Error(
                    StateMachineDirectory(machine.id) +
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
                    Enum.TryParse(
                        transition.blendLogic,
                        false,
                        out AnimationTransitionBlendLogic blendLogic) &&
                    Enum.IsDefined(
                        typeof(AnimationTransitionBlendLogic),
                        blendLogic) &&
                    string.Equals(
                        transition.blendLogic,
                        blendLogic.ToString(),
                        StringComparison.Ordinal) &&
                    Enum.TryParse(
                        transition.blendMode,
                        false,
                        out CharacterAnimationBlendMode blendMode) &&
                    Enum.IsDefined(
                        typeof(CharacterAnimationBlendMode),
                        blendMode) &&
                    string.Equals(
                        transition.blendMode,
                        blendMode.ToString(),
                        StringComparison.Ordinal) &&
                    float.IsFinite(transition.durationSeconds) &&
                    transition.durationSeconds >= 0f &&
                    (blendLogic != AnimationTransitionBlendLogic.Inertialization ||
                     transition.durationSeconds > 0f) &&
                    (blendMode == CharacterAnimationBlendMode.Custom) ==
                    Identity(transition.customBlendCurveAssetId) &&
                    ((blendLogic == AnimationTransitionBlendLogic.StandardBlend &&
                      transition.durationSeconds == 0f &&
                      string.IsNullOrWhiteSpace(transition.blendProfileAssetId)) ||
                     Identity(transition.blendProfileAssetId));
                if (!transitionValid)
                {
                    report.Error(
                        StateMachineDirectory(machine.id) +
                        $"/state-machine.json.transitions[{transition?.id}]",
                        "presentation_pose_transition_invalid",
                        "Pose Transition source、target、rule或blend字段非法。");
                    valid = false;
                }
            }
            if (!valid)
            {
                report.Error(
                    StateMachineDirectory(machine.id) + "/state-machine.json",
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

        static bool ValidateValue(
            GraphAuthoringFieldDescriptor field,
            JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return !field.Constraint.NonEmpty;
            bool typeValid = field.ValueKind switch
            {
                GraphAuthoringFieldValueKind.Boolean =>
                    value.Type == JTokenType.Boolean,
                GraphAuthoringFieldValueKind.Integer =>
                    value.Type == JTokenType.Integer,
                GraphAuthoringFieldValueKind.Float =>
                    value.Type == JTokenType.Float ||
                    value.Type == JTokenType.Integer,
                GraphAuthoringFieldValueKind.Vector2 =>
                    Vector(value, "x", "y"),
                GraphAuthoringFieldValueKind.Vector3 =>
                    Vector(value, "x", "y", "z"),
                GraphAuthoringFieldValueKind.Quaternion =>
                    Vector(value, "x", "y", "z", "w"),
                GraphAuthoringFieldValueKind.AssetReference =>
                    AssetToken(value),
                GraphAuthoringFieldValueKind.String or
                    GraphAuthoringFieldValueKind.Enum or
                    GraphAuthoringFieldValueKind.IdentityReference =>
                    value.Type == JTokenType.String,
                GraphAuthoringFieldValueKind.Object =>
                    value.Type == JTokenType.Object ||
                    value.Type == JTokenType.Array,
                _ => false
            };
            if (!typeValid)
                return false;
            if (value.Type == JTokenType.String)
            {
                string text = value.Value<string>();
                if (field.Constraint.NonEmpty &&
                    string.IsNullOrWhiteSpace(text))
                    return false;
                if (field.Constraint.AllowedValues.Count > 0 &&
                    !field.Constraint.AllowedValues.Contains(text))
                    return false;
            }
            if (value.Type != JTokenType.Float &&
                value.Type != JTokenType.Integer)
                return true;
            double number = value.Value<double>();
            return (!field.Constraint.Minimum.HasValue ||
                    number >= field.Constraint.Minimum.Value) &&
                   (!field.Constraint.Maximum.HasValue ||
                    number <= field.Constraint.Maximum.Value);
        }

        static bool RejectInternalFields(
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

        static bool Vector(JToken token, params string[] fields)
        {
            if (!(token is JObject value) ||
                value.Properties().Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal)
                    .SetEquals(fields) == false)
                return false;
            return fields.All(field =>
                value[field]?.Type == JTokenType.Integer ||
                value[field]?.Type == JTokenType.Float);
        }

        static bool AssetToken(JToken token)
        {
            if (!(token is JObject value))
                return false;
            var reference = new AgentPackageAssetReferenceV3
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

        static bool Asset(AgentPackageAssetReferenceV3 value) =>
            value != null &&
            string.IsNullOrWhiteSpace(value.localId) &&
            !string.IsNullOrWhiteSpace(value.assetPath) &&
            value.assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
            !value.assetPath.Contains("\\") &&
            value.localFileId != 0 && value.assetGuid?.Length == 32 &&
            value.assetGuid.All(character =>
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f');

        static bool AssetReference(AgentPackageAssetReferenceV3 value) =>
            Asset(value) ||
            value != null && LocalIdentity(value.localId) &&
            string.IsNullOrWhiteSpace(value.assetPath) &&
            string.IsNullOrWhiteSpace(value.assetGuid) &&
            value.localFileId == 0;

        static string ReferenceIdentity(AgentPackageAssetReferenceV3 value) =>
            value == null
                ? string.Empty
                : LocalIdentity(value.localId)
                    ? value.localId
                    : value.assetGuid + ":" + value.localFileId;

        static bool Identity(string value) =>
            !string.IsNullOrWhiteSpace(value);

        static bool LocalIdentity(string value) =>
            value?.StartsWith("local:", StringComparison.Ordinal) == true &&
            !string.IsNullOrWhiteSpace(value.Substring("local:".Length));

        static bool TryFile<T>(
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

        static string GraphDirectory(string id) =>
            GraphPrefix + AgentAuthoringPackageMapper.Segment(id);

        static string InterfacePath(string id) =>
            InterfacePrefix + AgentAuthoringPackageMapper.Segment(id) +
            "/interface.json";

        static string ImplementationDirectory(string id) =>
            ImplementationPrefix + AgentAuthoringPackageMapper.Segment(id);

        static string ImplementationGraphDirectory(
            string implementationId,
            string graphId) =>
            ImplementationDirectory(implementationId) + "/pose-graphs/" +
            AgentAuthoringPackageMapper.Segment(graphId);

        static string ImplementationStateMachineDirectory(
            string implementationId,
            string stateMachineId) =>
            ImplementationDirectory(implementationId) +
            "/pose-state-machines/" +
            AgentAuthoringPackageMapper.Segment(stateMachineId);

        static string ImplementationOwnerDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) ||
                !path.StartsWith(ImplementationPrefix, StringComparison.Ordinal))
                return string.Empty;
            int separator = path.IndexOf('/', ImplementationPrefix.Length);
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        static string DirectoryPath(string path)
        {
            int separator = path.LastIndexOf('/');
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        static string StateMachineDirectory(string id) =>
            StateMachinePrefix + AgentAuthoringPackageMapper.Segment(id);

        static string SequenceDirectory(string id) =>
            SequencePrefix + AgentAuthoringPackageMapper.Segment(id);

        static bool IsSequenceFile(string path) =>
            path.StartsWith(SequencePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/sequence.json", StringComparison.Ordinal);

        static bool IsSequenceCurvesFile(string path) =>
            path.StartsWith(SequencePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/curves.json", StringComparison.Ordinal);

        static bool IsGraphFile(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/graph.json", StringComparison.Ordinal);

        static bool IsLayoutFile(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        static bool IsStateMachineFile(string path) =>
            path.StartsWith(StateMachinePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/state-machine.json", StringComparison.Ordinal);

        static bool IsStateMachineLayoutFile(string path) =>
            path.StartsWith(StateMachinePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        internal static bool IsInterfaceFile(string path) =>
            path.StartsWith(InterfacePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/interface.json", StringComparison.Ordinal);

        internal static bool IsImplementationFile(string path) =>
            path.StartsWith(ImplementationPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/implementation.json", StringComparison.Ordinal);

        internal static bool IsImplementationGraphFile(string path) =>
            path.StartsWith(ImplementationPrefix, StringComparison.Ordinal) &&
            path.Contains("/pose-graphs/", StringComparison.Ordinal) &&
            path.EndsWith("/graph.json", StringComparison.Ordinal);

        internal static bool IsImplementationGraphLayoutFile(string path) =>
            path.StartsWith(ImplementationPrefix, StringComparison.Ordinal) &&
            path.Contains("/pose-graphs/", StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        internal static bool IsImplementationStateMachineFile(string path) =>
            path.StartsWith(ImplementationPrefix, StringComparison.Ordinal) &&
            path.Contains("/pose-state-machines/", StringComparison.Ordinal) &&
            path.EndsWith("/state-machine.json", StringComparison.Ordinal);

        internal static bool IsImplementationStateMachineLayoutFile(string path) =>
            path.StartsWith(ImplementationPrefix, StringComparison.Ordinal) &&
            path.Contains("/pose-state-machines/", StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        sealed class NodeContract
        {
            readonly Dictionary<string, PortContract> m_Ports;

            public NodeContract(
                AgentPackagePoseNode node,
                GraphAuthoringCapabilityDescriptor capability)
            {
                m_Ports = capability.FixedPorts.ToDictionary(
                    value => value.PortId.Value,
                    value => new PortContract(
                        value.ValueTypeId,
                        value.Direction),
                    StringComparer.Ordinal);
                foreach (AgentPackagePoseDynamicPort port in node.dynamicPorts ??
                             new List<AgentPackagePoseDynamicPort>())
                {
                    m_Ports[port.id] = new PortContract(
                        port.valueType,
                        Enum.Parse<GraphAuthoringPortDirection>(port.direction));
                }
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
