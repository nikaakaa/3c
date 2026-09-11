using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using static ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill.AgentPackageMappingSupport;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal enum AgentAuthoringDocumentReadPhase
    {
        TargetMutation,
        CheckoutRoundTrip
    }

    public sealed class AgentAuthoringPackageMapper
    {
        readonly AgentGraphPackageProjection m_GraphPackage =
            new AgentGraphPackageProjection();

        public Dictionary<string, JToken> ToFiles(
            AgentAuthoringTarget target,
            AgentCompileReport report)
        {
            var files = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (string.Equals(target.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                files["editable/controller.json"] = AgentControlDocumentMapper.ToToken(target.editable);
                files["editable/actions.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageActionsFile
                {
                    requests = target.editable.actionRequests,
                    profiles = target.editable.actionProfiles
                });
                AgentSkillDocumentMapper.Write(files, target.editable.skills);
                AgentSkillFlowDocumentMapper.Write(files, target.editable, report);
                AgentAuthoringPresentationPackageCodec.Write(
                    files,
                    target.editable.presentation,
                    report);
                AgentAuthoringPresentationPackageCodec.WriteReadonly(
                    files,
                    target.context.presentation,
                    report);
            }

            var nodeCatalog = new AgentPackageNodeCatalogFile
            {
                kinds = m_GraphPackage.ExportNodeKinds(target.domain).ToList(),
                skillKinds = string.Equals(target.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal)
                    ? AgentSkillPackageProjection.ExportCatalog().ToList()
                    : new List<AgentPackageSkillNodeKindDescriptor>()
            };
            AgentPackageNodeCatalogValidator.Validate(nodeCatalog, report);
            files["context/node-catalog.json"] = AgentAuthoringDocumentCodec.ToToken(nodeCatalog);
            files["context/graph-kinds.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageGraphKindsFile
            {
                kinds = m_GraphPackage.ExportGraphKinds(target.domain).ToList()
            });
            files["context/asset-catalog.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageAssetCatalogFile
            {
                inputValues = target.context.inputValues,
                actionRequests = target.context.actionRequests,
                animationBlendCurves = target.context.presentation?.blendCurves ??
                    new List<AgentDocumentBlendAssetContext>(),
                animationBlendProfiles = target.context.presentation?.blendProfiles ??
                    new List<AgentDocumentBlendAssetContext>(),
                animationClips = target.context.presentation?.animationClips ??
                    new List<AgentDocumentAnimationClipContext>()
            });
            files["context/dependencies.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageDependenciesFile
            {
                definitionName = target.context.definitionName,
                definitionAssetPath = target.context.definitionAssetPath,
                bodyMotion = target.context.bodyMotion,
                presentation = target.context.presentation,
                generatedProduct = target.context.generatedProduct,
                capabilities = target.context.capabilities
            });
            return files;
        }

        internal bool TryFromFiles(
            AgentAuthoringPackageManifest manifest,
            IReadOnlyDictionary<string, JToken> files,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId,
            AgentCompileReport report,
            AgentAuthoringDocumentReadPhase phase,
            out AgentAuthoringTarget target)
        {
            target = new AgentAuthoringTarget
            {
                domain = manifest.domain,
                rootIdentity = manifest.rootIdentity
            };
            if (!AgentAuthoringSchema.IsDomain(manifest.domain))
            {
                report.Error("manifest.domain", "unsupported_domain", "Agent Document只支持CharacterController；Behavior Designer行为不进入BTSMTL Document。");
                return false;
            }
            bool valid = true;
            AgentPackageControllerFile controller = new AgentPackageControllerFile();
            AgentPackageActionsFile actions = new AgentPackageActionsFile();
            if (string.Equals(manifest.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                valid &= TryFile(files, "editable/controller.json", report, out controller);
                valid &= TryFile(files, "editable/actions.json", report, out actions);
            }
            valid &= TryFile(files, "context/asset-catalog.json", report, out AgentPackageAssetCatalogFile assets);
            valid &= TryFile(files, "context/dependencies.json", report, out AgentPackageDependenciesFile dependencies);
            valid &= TryFile(files, "context/node-catalog.json", report, out AgentPackageNodeCatalogFile nodeCatalog);
            valid &= TryFile(files, "context/graph-kinds.json", report, out AgentPackageGraphKindsFile _);
            if (nodeCatalog != null)
                valid &= AgentPackageNodeCatalogValidator.Validate(nodeCatalog, report);
            if (!valid)
                return false;

            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                target.editable.control = AgentControlDocumentMapper.ReadController(controller, report);
                target.editable.actionRequests = actions.requests ?? new List<AgentActionRequest>();
                target.editable.actionProfiles = actions.profiles ?? new List<AgentActionProfile>();
                valid &= AgentSkillDocumentMapper.TryRead(files, target.editable, report);
                valid &= AgentSkillFlowDocumentMapper.TryRead(
                    files,
                    target.editable,
                    report,
                    inputProviderOwnerId,
                    gameplayProviderOwnerId);
            }
            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                valid &= AgentAuthoringPresentationPackageCodec.TryRead(
                    files,
                    report,
                    out AgentDocumentPresentationEditable presentation);
                target.editable.presentation = presentation;
            }
            if (files.ContainsKey("editable/ai/perception.json"))
            {
                report.Error("editable/ai/perception.json", "document_domain_file_invalid", "BTSMTL Document不能包含Behavior Designer AI分片。");
                valid = false;
            }

            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
                valid &= AgentSkillFlowDocumentMapper.Validate(
                    new AgentPackageSkillFlowDocument
                    {
                        skills = target.editable.skills,
                        graphs = target.editable.skillGraphs,
                        layouts = target.editable.skillGraphLayouts,
                        macros = target.editable.skillMacros,
                        timelines = target.editable.skillTimelines
                    },
                    report,
                    target.editable.control?.moduleId,
                    inputProviderOwnerId,
                    gameplayProviderOwnerId);
            valid &= AgentPackageMappingSupport.ValidatePrimaryIdentities(target.editable, report);

            target.context = new AgentDocumentContext
            {
                definitionName = dependencies.definitionName,
                definitionAssetPath = dependencies.definitionAssetPath,
                inputValues = assets.inputValues ?? new List<AgentInputValue>(),
                actionRequests = assets.actionRequests ?? new List<AgentActionRequest>(),
                bodyMotion = dependencies.bodyMotion ?? new AgentBodyMotionProfile(),
                presentation = dependencies.presentation ??
                               new AgentDocumentPresentationContext(),
                generatedProduct = dependencies.generatedProduct ?? new AgentDocumentGeneratedProduct(),
                capabilities = dependencies.capabilities ?? new List<string>()
            };
            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                valid &= AgentAuthoringPresentationPackageCodec.TryReadReadonly(
                    files,
                    report,
                    out List<AgentPackageLinkedPoseInterfaceFile> linkedPoseInterfaces);
                target.context.presentation.linkedPoseInterfaces =
                    linkedPoseInterfaces;
                valid &= AgentAuthoringPresentationPackageCodec
                    .ValidateReadonlyClosure(
                        target.editable.presentation,
                        linkedPoseInterfaces,
                        report);
            }
            return valid;
        }







        internal static bool TryDiscoverRemovedAuthoringFragments(
            IReadOnlyCollection<string> missingPaths,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var missing = new HashSet<string>(missingPaths ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string path in missing.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryResolveFragmentCompanion(path, out string companion))
                    continue;
                if (!missing.Contains(companion))
                {
                    report.Error(path, "document_fragment_remove_pair_incomplete", "删除Graph或Timeline分片必须同时删除同目录canonical pair。");
                    valid = false;
                    continue;
                }
                result.Add(path);
                result.Add(companion);
            }
            discovered = result;
            return valid;
        }

        static bool TryResolveFragmentCompanion(string path, out string companion)
        {
            companion = null;
            if (AgentAuthoringPresentationPackageCodec.IsImplementationGraphFile(path))
                companion = path.Substring(0, path.Length - "graph.json".Length) + "layout.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationGraphLayoutFile(path))
                companion = path.Substring(0, path.Length - "layout.json".Length) + "graph.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineFile(path))
                companion = path.Substring(0, path.Length - "state-machine.json".Length) + "layout.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineLayoutFile(path))
                companion = path.Substring(0, path.Length - "layout.json".Length) + "state-machine.json";
            return companion != null;
        }

        internal static string Segment(string identity)
        {
            string value = string.IsNullOrWhiteSpace(identity) ? "entity" : identity;
            var builder = new StringBuilder(value.Length);
            foreach (char character in value)
                builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
            string readable = builder.ToString().Trim('-');
            if (readable.Length > 48)
                readable = readable.Substring(0, 48);
            using SHA256 algorithm = SHA256.Create();
            string hash = string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value))
                .Take(6)
                .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
            return $"{(string.IsNullOrEmpty(readable) ? "entity" : readable)}-{hash}";
        }

        static bool TryFile<T>(
            IReadOnlyDictionary<string, JToken> files,
            string path,
            AgentCompileReport report,
            out T value)
        {
            value = default;
            if (!files.TryGetValue(path, out JToken token))
            {
                report.Error(path, "document_file_missing", $"Manifest缺少必需文件：{path}");
                return false;
            }
            return AgentAuthoringDocumentCodec.TryConvertToken(token, path, report, out value);
        }
    }
}
