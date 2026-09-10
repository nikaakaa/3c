using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BTSMTL.Timeline;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Control.Authoring;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAuthoringDocumentStore
    {
        static readonly UTF8Encoding s_Utf8WithoutBom = new UTF8Encoding(false);
        readonly AgentAuthoringPackageMapper m_Mapper = new AgentAuthoringPackageMapper();

        public string GetPackagePath(string domain, string rootAssetPath, string rootIdentity)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                                 throw new InvalidOperationException("无法解析Unity项目根目录。");
            string keySource = $"{domain}\n{rootAssetPath}\n{rootIdentity}";
            string hash = Hash(keySource).Substring(0, 16);
            string readable = Sanitize(rootIdentity);
            if (readable.Length > 8)
                readable = readable.Substring(0, 8);
            string directoryName = $"{readable}-{hash}.btsmtl";
            return Path.GetFullPath(Path.Combine(projectRoot, "AgentAuthoring", "Documents", domain, directoryName));
        }

        public bool Exists(string packagePath)
        {
            return Directory.Exists(packagePath);
        }

        public bool RequiresCheckout(string packagePath)
        {
            string manifestPath = Path.Combine(packagePath, "manifest.json");
            if (!File.Exists(manifestPath))
                return false;
            try
            {
                JObject manifest = JObject.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
                string schemaVersion = manifest.Value<string>("schemaVersion");
                if (!string.IsNullOrWhiteSpace(schemaVersion) &&
                    !string.Equals(schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
                    return true;
                string controllerPath = Path.Combine(packagePath, "editable", "controller.json");
                if (File.Exists(controllerPath))
                {
                    JObject controller = JObject.Parse(
                        File.ReadAllText(controllerPath, Encoding.UTF8));
                    if (string.IsNullOrWhiteSpace(controller.Value<string>("controlModuleId")))
                        return true;
                }
                string profilePath = Path.Combine(
                    packagePath,
                    "editable",
                    "presentation",
                    "profile.json");
                if (File.Exists(profilePath))
                {
                    JObject profile = JObject.Parse(
                        File.ReadAllText(profilePath, Encoding.UTF8));
                    if (profile["poseSources"] is JArray sources &&
                        sources.OfType<JObject>().Any(value =>
                            value["id"] != null ||
                            value["slot"] == null ||
                            value["binding"] == null ||
                            value["timeMapping"] == null) ||
                        profile.Descendants().OfType<JObject>().Any(value =>
                            value["assetGuid"] != null &&
                            value["assetPath"] != null &&
                            value["localFileId"] == null))
                    {
                        return true;
                    }
                }
                var files = manifest["files"]?.Values<string>()
                    .Select(value => value?.Replace('\\', '/'))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToHashSet(StringComparer.Ordinal) ??
                    new HashSet<string>(StringComparer.Ordinal);
                if (files.Any(value =>
                        string.Equals(value, "editable/blackboard.json", StringComparison.Ordinal) ||
                        value.StartsWith("editable/graphs/", StringComparison.Ordinal) ||
                        value.StartsWith("editable/timelines/", StringComparison.Ordinal)))
                    return true;
                bool presentationLayoutIncomplete = files
                    .Where(value =>
                        value.StartsWith(
                            "editable/presentation/pose-state-machines/",
                            StringComparison.Ordinal) &&
                        value.EndsWith(
                            "/state-machine.json",
                            StringComparison.Ordinal))
                    .Any(value =>
                        !files.Contains(
                            value.Substring(
                                0,
                                value.Length -
                                "state-machine.json".Length) +
                            "layout.json"));
                return presentationLayoutIncomplete ||
                       RequiresSkillProviderOwnerRefresh(packagePath, files) ||
                       RequiresSkillTimelineRefresh(packagePath, files) ||
                       CanRefreshReadOnlyContext(packagePath, files);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        static bool RequiresSkillProviderOwnerRefresh(
            string packagePath,
            IReadOnlyCollection<string> files)
        {
            foreach (string relativePath in files.Where(value =>
                         value.StartsWith("editable/skills/graphs/", StringComparison.Ordinal) &&
                         value.EndsWith("/graph.json", StringComparison.Ordinal)))
            {
                string fullPath = ResolveInside(packagePath, relativePath);
                if (!File.Exists(fullPath))
                    return true;
                JObject graph = JObject.Parse(File.ReadAllText(fullPath, Encoding.UTF8));
                foreach (JObject node in graph["nodes"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    string capability = node.Value<string>("capability");
                    if (!BtsmtlSkillCapabilityCatalog.Properties(capability).Contains("providerOwnerId"))
                        continue;
                    if (string.IsNullOrWhiteSpace((node["properties"] as JObject)?.Value<string>("providerOwnerId")))
                        return true;
                }
            }
            return false;
        }

        static bool RequiresSkillTimelineRefresh(
            string packagePath,
            IReadOnlyCollection<string> files)
        {
            foreach (string relativePath in files.Where(value =>
                         value.StartsWith("editable/skills/timelines/", StringComparison.Ordinal) &&
                         value.EndsWith("/timeline.json", StringComparison.Ordinal)))
            {
                string fullPath = ResolveInside(packagePath, relativePath);
                if (!File.Exists(fullPath))
                    return true;
                JObject timeline = JObject.Parse(File.ReadAllText(fullPath, Encoding.UTF8));
                foreach (JObject track in timeline["tracks"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    if (track.Value<string>("kind") != TimelineContractKinds.AnimationTrack)
                        continue;
                    if (string.IsNullOrWhiteSpace(track.Value<string>("animationSlotId")))
                        return true;
                    foreach (JObject clip in track["clips"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    {
                        if (clip.Value<string>("kind") != TimelineContractKinds.AnimationClip)
                            continue;
                        if (string.IsNullOrWhiteSpace((clip["properties"] as JObject)?.Value<string>("blendProfileId")))
                            return true;
                    }
                }
            }
            return false;
        }

        static bool CanRefreshReadOnlyContext(
            string packagePath,
            IReadOnlyCollection<string> files)
        {
            var validation = new AgentCompileReport { success = true };
            bool contextInvalid = files
                .Where(IsReadOnlyPath)
                .Any(relativePath =>
                    !TryReadContent(
                        relativePath,
                        ResolveInside(packagePath, relativePath),
                        validation,
                        out _));
            if (!contextInvalid)
                return false;
            string syncPath = Path.Combine(packagePath, ".sync.json");
            if (!AgentAuthoringDocumentCodec.TryReadFile(
                    syncPath,
                    validation,
                    out AgentAuthoringPackageSync sync,
                    out _) ||
                string.IsNullOrWhiteSpace(sync.baseEditableHash))
                return false;
            var editable = new Dictionary<string, JToken>(StringComparer.Ordinal);
            try
            {
                foreach (string relativePath in files.Where(value =>
                             value.StartsWith("editable/", StringComparison.Ordinal)))
                {
                    string fullPath = ResolveInside(packagePath, relativePath);
                    editable.Add(
                        relativePath,
                        JToken.Parse(
                            File.ReadAllText(fullPath, Encoding.UTF8),
                            new JsonLoadSettings
                            {
                                DuplicatePropertyNameHandling =
                                    DuplicatePropertyNameHandling.Error,
                                LineInfoHandling = LineInfoHandling.Load,
                                CommentHandling = CommentHandling.Load
                            }));
                }
            }
            catch (JsonException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            return string.Equals(
                AgentAuthoringDocumentCodec.HashFiles(editable),
                sync.baseEditableHash,
                StringComparison.Ordinal);
        }

        public string Write(
            string packagePath,
            AgentAuthoringTarget target,
            AgentGraphSnapshot snapshot,
            AgentAuthoringPackageSync sync,
            AgentCompileReport report,
            bool allowIncompletePresentationRepair,
            out string editableHash,
            out string contextHash)
        {
            Dictionary<string, JToken> files = m_Mapper.ToFiles(target, snapshot, report);
            if (report.HasErrors())
                throw new InvalidOperationException("Document package导出失败。");
            editableHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair => pair.Key.StartsWith("editable/", StringComparison.Ordinal)));
            contextHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair => IsReadOnlyPath(pair.Key)));
            var manifest = new AgentAuthoringPackageManifest
            {
                domain = target.domain,
                rootIdentity = target.rootIdentity,
                files = files.Keys.OrderBy(value => value, StringComparer.Ordinal).ToList()
            };
            string documentHash = AgentAuthoringDocumentCodec.HashDocument(manifest, editableHash, contextHash);
            Publish(
                packagePath,
                manifest,
                sync,
                files,
                target,
                snapshot,
                documentHash,
                editableHash,
                contextHash,
                allowIncompletePresentationRepair);
            return documentHash;
        }

        public bool TryRead(
            string packagePath,
            string domain,
            string rootIdentity,
            string rootAssetPath,
            AgentGraphSnapshot current,
            AgentCompileReport report,
            out AgentAuthoringTarget target,
            out AgentAuthoringPackageSync sync,
            out string editableHash,
            out string contextHash,
            out string documentHash)
        {
            return TryRead(
                packagePath,
                domain,
                rootIdentity,
                rootAssetPath,
                current,
                report,
                AgentAuthoringDocumentReadPhase.TargetMutation,
                out target,
                out sync,
                out editableHash,
                out contextHash,
                out documentHash);
        }

        bool TryRead(
            string packagePath,
            string domain,
            string rootIdentity,
            string rootAssetPath,
            AgentGraphSnapshot current,
            AgentCompileReport report,
            AgentAuthoringDocumentReadPhase phase,
            out AgentAuthoringTarget target,
            out AgentAuthoringPackageSync sync,
            out string editableHash,
            out string contextHash,
            out string documentHash)
        {
            target = null;
            sync = null;
            editableHash = null;
            contextHash = null;
            documentHash = null;
            if (!Directory.Exists(packagePath))
            {
                report.Error("document", "document_missing", $"Agent Authoring Document package不存在：{packagePath}");
                return false;
            }

            string manifestPath = Path.Combine(packagePath, "manifest.json");
            string syncPath = Path.Combine(packagePath, ".sync.json");
            if (!AgentAuthoringDocumentCodec.TryReadFile(manifestPath, report, out AgentAuthoringPackageManifest manifest, out _) ||
                !AgentAuthoringDocumentCodec.ValidateManifest(manifest, domain, rootIdentity, report) ||
                !AgentAuthoringDocumentCodec.TryReadFile(syncPath, report, out sync, out _) ||
                !AgentAuthoringDocumentCodec.ValidateSync(sync, manifest, rootAssetPath, report))
                return false;

            HashSet<string> declared = new HashSet<string>(manifest.files.Select(AgentAuthoringDocumentCodec.NormalizeRelativePath), StringComparer.Ordinal);
            HashSet<string> actual = new HashSet<string>(
                Directory.GetFiles(packagePath, "*", SearchOption.AllDirectories)
                    .Select(path => AgentAuthoringDocumentCodec.NormalizeRelativePath(Path.GetRelativePath(packagePath, path)))
                    .Where(path => !string.Equals(path, "manifest.json", StringComparison.Ordinal) &&
                                   !string.Equals(path, ".sync.json", StringComparison.Ordinal)),
                StringComparer.Ordinal);
            string[] missingPaths = declared
                .Except(actual, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            string[] unknownPaths = actual
                .Except(declared, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!AgentAuthoringPresentationPackageCodec.TryDiscoverRemovedLinkedPoseFragments(
                    declared,
                    actual,
                    report,
                    out IReadOnlyCollection<string> removedLinkedPoseFragments) ||
                !AgentSkillFlowDocumentMapper.TryDiscoverRemovedFragments(
                    declared,
                    actual,
                    report,
                    out IReadOnlyCollection<string> removedSkillFragments) ||
                !AgentAuthoringPackageMapper.TryDiscoverRemovedAuthoringFragments(
                    missingPaths
                        .Except(
                            removedLinkedPoseFragments.Concat(removedSkillFragments),
                            StringComparer.Ordinal)
                        .ToArray(),
                    report,
                    out IReadOnlyCollection<string> removedPairedFragments))
                return false;
            IReadOnlyCollection<string> removedFragments =
                removedLinkedPoseFragments
                    .Concat(removedSkillFragments)
                    .Concat(removedPairedFragments)
                    .ToArray();
            string[] rejectedMissing = missingPaths
                .Except(removedFragments, StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (rejectedMissing.Length > 0)
            {
                string missing = string.Join(", ", rejectedMissing);
                string unknown = string.Join(", ", unknownPaths);
                report.Error("manifest.json.files", "document_manifest_file_mismatch", $"Document package文件清单不一致。missing=[{missing}] unknown=[{unknown}]");
                return false;
            }
            IReadOnlyCollection<string> discovered = Array.Empty<string>();
            if (unknownPaths.Length > 0)
            {
                var poseCandidates = new Dictionary<string, JToken>(StringComparer.Ordinal);
                var linkedPoseCandidates = new Dictionary<string, JToken>(StringComparer.Ordinal);
                 var skillCandidates = new Dictionary<string, JToken>(StringComparer.Ordinal);
                var skillFlowCandidates = new Dictionary<string, JToken>(StringComparer.Ordinal);
                foreach (string relativePath in unknownPaths.Where(path =>
                             AgentAuthoringPresentationPackageCodec.IsDiscoverablePoseGraphFragment(path) ||
                             AgentAuthoringPresentationPackageCodec.IsDiscoverableLinkedPoseFragment(path) ||
                              AgentSkillDocumentMapper.IsDefinitionPath(path) ||
                             AgentSkillFlowDocumentMapper.IsFragmentPath(path)))
                {
                    string fullPath = ResolveInside(packagePath, relativePath);
                    if (!TryReadContent(
                            relativePath,
                            fullPath,
                            report,
                            out JToken raw))
                        return false;
                    if (AgentAuthoringPresentationPackageCodec.IsDiscoverablePoseGraphFragment(relativePath))
                        poseCandidates.Add(relativePath, raw);
                    else if (AgentAuthoringPresentationPackageCodec.IsDiscoverableLinkedPoseFragment(relativePath))
                        linkedPoseCandidates.Add(relativePath, raw);
                    else if (AgentSkillFlowDocumentMapper.IsFragmentPath(relativePath))
                        skillFlowCandidates.Add(relativePath, raw);
                    else if (AgentSkillDocumentMapper.IsDefinitionPath(relativePath))
                        skillCandidates.Add(relativePath, raw);
                }
                if (!AgentAuthoringPresentationPackageCodec
                        .TryDiscoverNewPoseGraphFragments(
                            poseCandidates,
                            report,
                            out IReadOnlyCollection<string> discoveredPoseGraphs) ||
                     !AgentAuthoringPresentationPackageCodec
                         .TryDiscoverNewLinkedPoseFragments(
                            linkedPoseCandidates,
                             report,
                             out IReadOnlyCollection<string> discoveredLinkedPose) ||
                      !AgentSkillDocumentMapper.TryDiscoverNewFragments(
                            skillCandidates,
                            report,
                            out IReadOnlyCollection<string> discoveredSkills) ||
                     !AgentSkillFlowDocumentMapper.TryDiscoverNewFragments(
                            skillFlowCandidates,
                            report,
                            out IReadOnlyCollection<string> discoveredSkillFlow))
                    return false;
                discovered = discoveredPoseGraphs
                    .Concat(discoveredLinkedPose)
                    .Concat(discoveredSkills)
                    .Concat(discoveredSkillFlow)
                    .ToArray();
                string[] rejected = unknownPaths
                    .Except(discovered, StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                if (rejected.Length > 0)
                {
                    report.Error(
                        "manifest.json.files",
                        "document_manifest_file_mismatch",
                        $"Document package文件清单不一致。missing=[] unknown=[{string.Join(", ", rejected)}]");
                    return false;
                }
            }
            if (removedFragments.Count > 0 || discovered.Count > 0)
            {
                manifest = new AgentAuthoringPackageManifest
                {
                    schemaVersion = manifest.schemaVersion,
                    domain = manifest.domain,
                    rootIdentity = manifest.rootIdentity,
                    files = declared
                        .Except(removedFragments, StringComparer.Ordinal)
                        .Concat(discovered)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToList()
                };
                declared = new HashSet<string>(manifest.files, StringComparer.Ordinal);
            }

            var files = new Dictionary<string, JToken>(StringComparer.Ordinal);
            foreach (string relativePath in declared.OrderBy(value => value, StringComparer.Ordinal))
            {
                string fullPath = ResolveInside(packagePath, relativePath);
                if (!TryReadContent(relativePath, fullPath, report, out JToken raw))
                    return false;
                files.Add(relativePath, raw);
            }

            editableHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair => pair.Key.StartsWith("editable/", StringComparison.Ordinal)));
            contextHash = AgentAuthoringDocumentCodec.HashFiles(files.Where(pair => IsReadOnlyPath(pair.Key)));
            documentHash = AgentAuthoringDocumentCodec.HashDocument(manifest, editableHash, contextHash);
            return m_Mapper.TryFromFiles(manifest, files, current, report, phase, out target);
        }

        static bool TryReadContent(string relativePath, string fullPath, AgentCompileReport report, out JToken raw)
        {
            raw = null;
            if (string.Equals(relativePath, "editable/controller.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageControllerFile _, out raw);
            if (string.Equals(relativePath, "editable/actions.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageActionsFile _, out raw);
            if (relativePath.StartsWith("editable/skills/", StringComparison.Ordinal) &&
                relativePath.EndsWith("/definition.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageSkillDefinitionFile _, out raw);
            if (AgentSkillFlowDocumentMapper.IsGraphPath(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageSkillFlowGraphFile _, out raw);
            if (AgentSkillFlowDocumentMapper.IsGraphLayoutPath(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageSkillFlowGraphLayoutFile _, out raw);
            if (AgentSkillFlowDocumentMapper.IsMacroPath(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageSkillMacroFile _, out raw);
            if (AgentSkillFlowDocumentMapper.IsTimelinePath(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageSkillTimelineFile _, out raw);
            if (AgentSkillFlowDocumentMapper.IsTimelineCurvesPath(relativePath))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageCurvesFile _, out raw);
            if (string.Equals(relativePath, "context/node-catalog.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageNodeCatalogFile _, out raw);
            if (string.Equals(relativePath, "context/graph-kinds.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageGraphKindsFile _, out raw);
            if (string.Equals(relativePath, "context/asset-catalog.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageAssetCatalogFile _, out raw);
            if (string.Equals(relativePath, "context/dependencies.json", StringComparison.Ordinal))
                return AgentAuthoringDocumentCodec.TryReadFile(fullPath, report, out AgentPackageDependenciesFile _, out raw);
            if (relativePath.StartsWith("editable/presentation/", StringComparison.Ordinal) ||
                relativePath.StartsWith("editable/animation-clips/", StringComparison.Ordinal) ||
                relativePath.StartsWith("readonly/presentation/", StringComparison.Ordinal))
                return AgentAuthoringPresentationPackageCodec.TryReadContent(relativePath, fullPath, report, out raw);
            report.Error(relativePath, "document_file_unknown", $"Manifest包含未登记JSON文件：{relativePath}");
            return false;
        }

        static bool IsReadOnlyPath(string path) =>
            path.StartsWith("context/", StringComparison.Ordinal) ||
            path.StartsWith("readonly/", StringComparison.Ordinal);

        void Publish(
            string packagePath,
            AgentAuthoringPackageManifest manifest,
            AgentAuthoringPackageSync sync,
            IReadOnlyDictionary<string, JToken> files,
            AgentAuthoringTarget target,
            AgentGraphSnapshot snapshot,
            string expectedDocumentHash,
            string expectedEditableHash,
            string expectedContextHash,
            bool allowIncompletePresentationRepair)
        {
            string parent = Path.GetDirectoryName(packagePath) ?? throw new InvalidOperationException("Document package缺少父目录。");
            Directory.CreateDirectory(parent);
            string token = Guid.NewGuid().ToString("N");
            string stagingPath = Path.Combine(parent, $".{token}.staging");
            string rollbackPath = Path.Combine(parent, $".{token}.rollback");
            try
            {
                Directory.CreateDirectory(stagingPath);
                WriteFile(Path.Combine(stagingPath, "manifest.json"), AgentAuthoringDocumentCodec.ToJson(manifest));
                WriteFile(Path.Combine(stagingPath, ".sync.json"), AgentAuthoringDocumentCodec.ToJson(sync));
                foreach (KeyValuePair<string, JToken> file in files)
                    WriteFile(ResolveInside(stagingPath, file.Key), AgentAuthoringDocumentCodec.ToJson(file.Value));

                var validation = new AgentCompileReport
                {
                    success = true,
                    schemaVersion = AgentAuthoringSchema.Version,
                    domain = target.domain,
                    rootIdentity = target.rootIdentity
                };
                bool reread = TryRead(
                    stagingPath,
                    target.domain,
                    target.rootIdentity,
                    sync.rootAssetPath,
                    snapshot,
                    validation,
                    AgentAuthoringDocumentReadPhase.CheckoutRoundTrip,
                    out _,
                    out _,
                    out string stagedEditableHash,
                    out string stagedContextHash,
                    out string stagedDocumentHash);
                bool editableHashMatches = string.Equals(
                    stagedEditableHash,
                    expectedEditableHash,
                    StringComparison.Ordinal);
                bool contextHashMatches = string.Equals(
                    stagedContextHash,
                    expectedContextHash,
                    StringComparison.Ordinal);
                bool documentHashMatches = string.Equals(
                    stagedDocumentHash,
                    expectedDocumentHash,
                    StringComparison.Ordinal);
                bool repairablePresentationState =
                    allowIncompletePresentationRepair &&
                    validation.messages.Any(value =>
                        string.Equals(
                            value.severity,
                            AgentReportSeverity.Error.ToString(),
                            StringComparison.Ordinal)) &&
                    validation.messages
                        .Where(value => string.Equals(
                            value.severity,
                            AgentReportSeverity.Error.ToString(),
                            StringComparison.Ordinal))
                        .All(value =>
                            value.code == "presentation_pose_properties_invalid" ||
                            value.code == "presentation_pose_property_value_invalid" ||
                            value.code == "presentation_pose_state_machine_invalid");
                if (!reread && !repairablePresentationState ||
                    !editableHashMatches ||
                    !contextHashMatches ||
                    !documentHashMatches)
                {
                    throw new InvalidOperationException(
                        FormatValidationFailure(
                            validation,
                            reread,
                            expectedEditableHash,
                            stagedEditableHash,
                            editableHashMatches,
                            expectedContextHash,
                            stagedContextHash,
                            contextHashMatches,
                            expectedDocumentHash,
                            stagedDocumentHash,
                            documentHashMatches));
                }

                if (Directory.Exists(packagePath))
                {
                    CopyDirectoryContents(packagePath, rollbackPath);
                    MirrorDirectoryContents(stagingPath, packagePath);
                }
                else
                {
                    Directory.Move(stagingPath, packagePath);
                }
                if (Directory.Exists(rollbackPath))
                    Directory.Delete(rollbackPath, true);
            }
            catch (Exception publishException)
            {
                try
                {
                    if (Directory.Exists(rollbackPath))
                        MirrorDirectoryContents(rollbackPath, packagePath);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Document package publish与rollback都失败。",
                        publishException,
                        rollbackException);
                }
                throw;
            }
            finally
            {
                if (Directory.Exists(stagingPath))
                    Directory.Delete(stagingPath, true);
                if (Directory.Exists(rollbackPath))
                    Directory.Delete(rollbackPath, true);
            }
        }

        static void CopyDirectoryContents(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);
            foreach (string sourceFile in Directory.GetFiles(
                         sourcePath,
                         "*",
                         SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                string targetFile = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetPath);
                File.Copy(sourceFile, targetFile, true);
            }
        }

        static void MirrorDirectoryContents(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);
            var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string sourceFile in Directory.GetFiles(
                         sourcePath,
                         "*",
                         SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                expectedFiles.Add(relativePath);
                string targetFile = Path.Combine(targetPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? targetPath);
                File.Copy(sourceFile, targetFile, true);
            }
            foreach (string targetFile in Directory.GetFiles(
                         targetPath,
                         "*",
                         SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(targetPath, targetFile);
                if (!expectedFiles.Contains(relativePath))
                    File.Delete(targetFile);
            }
            foreach (string directory in Directory.GetDirectories(
                         targetPath,
                         "*",
                         SearchOption.AllDirectories)
                     .OrderByDescending(value => value.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
        }

        static string FormatValidationFailure(
            AgentCompileReport validation,
            bool reread,
            string expectedEditableHash,
            string actualEditableHash,
            bool editableHashMatches,
            string expectedContextHash,
            string actualContextHash,
            bool contextHashMatches,
            string expectedDocumentHash,
            string actualDocumentHash,
            bool documentHashMatches)
        {
            IReadOnlyList<AgentCompileMessage> messages =
                validation?.messages != null
                    ? validation.messages
                    : Array.Empty<AgentCompileMessage>();
            var lines = new List<string>
            {
                "document_package_staging_validation_failed",
                $"reread={FormatBoolean(reread)}",
                $"hash.editable.match={FormatBoolean(editableHashMatches)}; expected={FormatDiagnosticValue(expectedEditableHash)}; actual={FormatDiagnosticValue(actualEditableHash)}",
                $"hash.context.match={FormatBoolean(contextHashMatches)}; expected={FormatDiagnosticValue(expectedContextHash)}; actual={FormatDiagnosticValue(actualContextHash)}",
                $"hash.document.match={FormatBoolean(documentHashMatches)}; expected={FormatDiagnosticValue(expectedDocumentHash)}; actual={FormatDiagnosticValue(actualDocumentHash)}",
                $"validation.messages.count={messages.Count}"
            };
            for (int i = 0; i < messages.Count; i++)
            {
                AgentCompileMessage message = messages[i];
                lines.Add(
                    $"validation.messages[{i}].code={FormatDiagnosticValue(message?.code)}; " +
                    $"path={FormatDiagnosticValue(message?.path)}; " +
                    $"message={FormatDiagnosticValue(message?.message)}");
            }
            return string.Join(Environment.NewLine, lines);
        }

        static string FormatBoolean(bool value)
        {
            return value ? "true" : "false";
        }

        static string FormatDiagnosticValue(string value)
        {
            return value == null
                ? "<null>"
                : new JValue(value).ToString(Formatting.None);
        }

        static string ResolveInside(string root, string relativePath)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(rootFull, AgentAuthoringDocumentCodec.NormalizeRelativePath(relativePath)));
            if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Document package路径越界：{relativePath}");
            return full;
        }

        static void WriteFile(string path, string content)
        {
            string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Document文件缺少父目录。");
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, content, s_Utf8WithoutBom);
        }

        static string Hash(string value)
        {
            using SHA256 algorithm = SHA256.Create();
            return string.Concat(algorithm.ComputeHash(s_Utf8WithoutBom.GetBytes(value))
                .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "root";
            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character);
            }
            string result = builder.ToString().Trim('-');
            return string.IsNullOrEmpty(result) ? "root" : result;
        }
    }
}
