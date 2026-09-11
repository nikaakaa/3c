using System;
using System.Collections.Generic;
using System.Linq;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{

    internal static class BtsmtlSkillDocumentDiffModule
    {
        public static void Build(
            AgentAuthoringTarget current,
            AgentDocumentEditable target,
            AgentMutationPlanBuilder mutations,
            AgentCompileReport report)
        {
            AgentPackageSkillFlowDocument currentDocument = new AgentPackageSkillFlowDocument
            {
                skills = current?.editable?.skills ?? new List<AgentPackageSkillDefinitionFile>(),
                graphs = current?.editable?.skillGraphs ?? new List<AgentPackageSkillFlowGraphFile>(),
                layouts = current?.editable?.skillGraphLayouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>(),
                macros = current?.editable?.skillMacros ?? new List<AgentPackageSkillMacroFile>(),
                timelines = current?.editable?.skillTimelines ?? new List<AgentPackageSkillTimelineFile>()
            };
            AgentPackageSkillFlowDocument targetDocument = new AgentPackageSkillFlowDocument
            {
                skills = target?.skills ?? new List<AgentPackageSkillDefinitionFile>(),
                graphs = target?.skillGraphs ?? new List<AgentPackageSkillFlowGraphFile>(),
                layouts = target?.skillGraphLayouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>(),
                macros = target?.skillMacros ?? new List<AgentPackageSkillMacroFile>(),
                timelines = target?.skillTimelines ?? new List<AgentPackageSkillTimelineFile>()
            };
            if (SameSemantic(currentDocument, targetDocument))
                return;
            mutations.Add(
                "document.editable.skills.flow",
                id => new BtsmtlSetSkillDocumentMutation(
                    id,
                    "document.editable.skills.flow",
                    AgentSkillFlowDocumentClone.Clone(targetDocument)));
        }

        static bool SameSemantic(
            AgentPackageSkillFlowDocument left,
            AgentPackageSkillFlowDocument right)
        {
            AgentPackageSkillFlowDocument leftValue = Normalize(left);
            AgentPackageSkillFlowDocument rightValue = Normalize(right);
            return string.Equals(
                SemanticHash(leftValue),
                SemanticHash(rightValue),
                StringComparison.Ordinal);
        }

        static string SemanticHash(AgentPackageSkillFlowDocument value)
        {
            return AgentAuthoringDocumentCodec.Hash(new
            {
                document = value,
                curves = value.timelines
                    .SelectMany(timeline => timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                    .SelectMany(track => track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                    .SelectMany(clip => clip?.curves ?? new List<AgentPackageCurve>())
                    .ToList()
            });
        }

        static AgentPackageSkillFlowDocument Normalize(AgentPackageSkillFlowDocument source)
        {
            AgentPackageSkillFlowDocument value = AgentSkillFlowDocumentClone.Clone(source);
            foreach (AgentPackageSkillFlowGraphFile graph in value.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
                if (graph != null)
                    graph.contentRevision = string.Empty;
            value.skills = (value.skills ?? new List<AgentPackageSkillDefinitionFile>())
                .OrderBy(skill => skill?.skillId, StringComparer.Ordinal)
                .ToList();
            foreach (AgentPackageSkillDefinitionFile skill in value.skills)
            {
                if (skill == null)
                    continue;
                skill.subgraphDependencies = (skill.subgraphDependencies ?? new List<AgentPackageSkillSubgraphDependency>())
                    .OrderBy(dependency => dependency?.subgraphIdentity, StringComparer.Ordinal)
                    .ThenBy(dependency => dependency?.callSiteIdentity, StringComparer.Ordinal)
                    .ToList();
                skill.allowedFollowUpSkillIds = (skill.allowedFollowUpSkillIds ?? new List<string>())
                    .OrderBy(identity => identity, StringComparer.Ordinal)
                    .ToList();
            }
            value.graphs = (value.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
                .OrderBy(graph => graph?.id, StringComparer.Ordinal)
                .ToList();
            foreach (AgentPackageSkillFlowGraphFile graph in value.graphs)
            {
                if (graph == null)
                    continue;
                graph.anchors = (graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    .OrderBy(anchor => anchor?.kind, StringComparer.Ordinal)
                    .ToList();
                graph.nodes = (graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                    .OrderBy(node => node?.id, StringComparer.Ordinal)
                    .ToList();
                graph.edges = (graph.edges ?? new List<AgentPackageSkillFlowEdge>())
                    .OrderBy(edge => edge?.id, StringComparer.Ordinal)
                    .ToList();
                graph.blackboardDeclarations = (graph.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                    .OrderBy(declaration => declaration?.id, StringComparer.Ordinal)
                    .ToList();
            }
            value.layouts = (value.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                .OrderBy(layout => layout?.graphId, StringComparer.Ordinal)
                .ToList();
            value.macros = (value.macros ?? new List<AgentPackageSkillMacroFile>())
                .OrderBy(macro => macro?.id, StringComparer.Ordinal)
                .ToList();
            value.timelines = (value.timelines ?? new List<AgentPackageSkillTimelineFile>())
                .OrderBy(timeline => timeline?.id, StringComparer.Ordinal)
                .ToList();
            foreach (AgentPackageSkillTimelineFile timeline in value.timelines)
                if (timeline != null)
                    timeline.externalBindings = (timeline.externalBindings ??
                            new List<AgentPackageSkillTimelineExternalBinding>())
                        .OrderBy(binding => binding?.id, StringComparer.Ordinal)
                        .ToList();
            return value;
        }
    }
}
