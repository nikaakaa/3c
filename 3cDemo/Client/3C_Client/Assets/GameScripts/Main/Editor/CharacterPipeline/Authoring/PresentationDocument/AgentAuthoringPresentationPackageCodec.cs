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
    internal static class AgentAuthoringPresentationPackageCodec
    {
        internal const string ProfilePath = "editable/presentation/profile.json";
        internal const string ClipPrefix = "editable/animation-clips/";
        internal const string GraphPrefix = "editable/presentation/pose-graphs/";
        internal const string StateMachinePrefix =
            "editable/presentation/pose-state-machines/";
        internal const string InterfacePrefix =
            "readonly/presentation/linked-pose-interfaces/";
        internal const string ImplementationPrefix =
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
                    "Character Document v7缺少Presentation Profile目标状态。");
                return;
            }
            files.Add(
                ProfilePath,
                AgentAuthoringDocumentCodec.ToToken(presentation.profile));
            foreach (AgentPackageAnimationClipCurvesFile clip in presentation.animationClips ??
                         new List<AgentPackageAnimationClipCurvesFile>())
            {
                files.Add(ClipPath(clip?.id), AgentAuthoringDocumentCodec.ToToken(clip));
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

            foreach (string path in files.Keys.Where(IsClipFile).OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryFile(files, path, report, out AgentPackageAnimationClipCurvesFile clip) ||
                    !string.Equals(path, ClipPath(clip.id), StringComparison.Ordinal))
                {
                    report.Error(path, "animation_clip_curve_path_mismatch", "AnimationClip Curve目录与identity必须一致。");
                    valid = false;
                    continue;
                }
                presentation.animationClips.Add(clip);
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
            if (IsClipFile(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageAnimationClipCurvesFile _, out raw);
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
                    "Document v7包含未知Presentation文件。");
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
            AgentCompileReport report) =>
            AgentAuthoringPresentationPackageValidator.ValidateFileSet(files, report);

        static bool ValidatePresentation(
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report) =>
            AgentAuthoringPresentationPackageValidator.ValidatePresentation(presentation, report);

        static bool ValidateImplementation(
            AgentPackageLinkedPoseImplementationFile implementation,
            string path,
            AgentCompileReport report) =>
            AgentAuthoringPresentationPackageValidator.ValidateImplementation(implementation, path, report);

        static bool ValidateInterface(
            AgentPackageLinkedPoseInterfaceFile value,
            string path,
            AgentCompileReport report) =>
            AgentAuthoringPresentationPackageValidator.ValidateInterface(value, path, report);

        static bool Identity(string value) =>
            AgentAuthoringPresentationPackageValidator.Identity(value);

        static bool LocalIdentity(string value) =>
            AgentAuthoringPresentationPackageValidator.LocalIdentity(value);

        static string ReferenceIdentity(AgentPackageObjectReference value) =>
            AgentAuthoringPresentationPackageValidator.ReferenceIdentity(value);

        static bool TryFile<T>(
            IReadOnlyDictionary<string, JToken> files,
            string path,
            AgentCompileReport report,
            out T value) =>
            AgentAuthoringPresentationPackageValidator.TryFile(files, path, report, out value);
        internal static string GraphDirectory(string id) =>
            GraphPrefix + AgentAuthoringPackageMapper.Segment(id);

        internal static string InterfacePath(string id) =>
            InterfacePrefix + AgentAuthoringPackageMapper.Segment(id) +
            "/interface.json";

        internal static string ImplementationDirectory(string id) =>
            ImplementationPrefix + AgentAuthoringPackageMapper.Segment(id);

        internal static string ImplementationGraphDirectory(
            string implementationId,
            string graphId) =>
            ImplementationDirectory(implementationId) + "/pose-graphs/" +
            AgentAuthoringPackageMapper.Segment(graphId);

        internal static string ImplementationStateMachineDirectory(
            string implementationId,
            string stateMachineId) =>
            ImplementationDirectory(implementationId) +
            "/pose-state-machines/" +
            AgentAuthoringPackageMapper.Segment(stateMachineId);

        internal static string ImplementationOwnerDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) ||
                !path.StartsWith(ImplementationPrefix, StringComparison.Ordinal))
                return string.Empty;
            int separator = path.IndexOf('/', ImplementationPrefix.Length);
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        internal static string DirectoryPath(string path)
        {
            int separator = path.LastIndexOf('/');
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        internal static string StateMachineDirectory(string id) =>
            StateMachinePrefix + AgentAuthoringPackageMapper.Segment(id);

        internal static string ClipPath(string id) =>
            ClipPrefix + AgentAuthoringPackageMapper.Segment(id) + "/curves.json";

        internal static bool IsClipFile(string path) =>
            path.StartsWith(ClipPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/curves.json", StringComparison.Ordinal);

        internal static bool IsGraphFile(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/graph.json", StringComparison.Ordinal);

        internal static bool IsLayoutFile(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        internal static bool IsStateMachineFile(string path) =>
            path.StartsWith(StateMachinePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/state-machine.json", StringComparison.Ordinal);

        internal static bool IsStateMachineLayoutFile(string path) =>
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
    }
}
