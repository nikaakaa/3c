using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    [Serializable]
    public sealed class AgentPackageSkillFlowDocument
    {
        public List<AgentPackageSkillDefinitionFile> skills = new List<AgentPackageSkillDefinitionFile>();
        public List<AgentPackageSkillFlowGraphFile> graphs = new List<AgentPackageSkillFlowGraphFile>();
        public List<AgentPackageSkillFlowGraphLayoutFile> layouts = new List<AgentPackageSkillFlowGraphLayoutFile>();
        public List<AgentPackageSkillMacroFile> macros = new List<AgentPackageSkillMacroFile>();
        public List<AgentPackageSkillTimelineFile> timelines = new List<AgentPackageSkillTimelineFile>();
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowGraphFile
    {
        public string id;
        public string role;
        public string name;
        public string contentRevision;
        public string ownership;
        public AgentPackageSkillGraphOwner owner;
        public AgentPackageObjectReference asset;
        public List<AgentPackageSkillGraphAnchor> anchors = new List<AgentPackageSkillGraphAnchor>();
        public List<AgentPackageSkillFlowNode> nodes = new List<AgentPackageSkillFlowNode>();
        public List<AgentPackageSkillFlowEdge> edges = new List<AgentPackageSkillFlowEdge>();
        public List<AgentPackageSkillBlackboardDeclaration> blackboardDeclarations = new List<AgentPackageSkillBlackboardDeclaration>();
    }

    [Serializable]
    public sealed class AgentPackageSkillGraphOwner
    {
        public string kind;
        public string skillId;
        public string graphId;
        public string nodeId;
        public string referenceKey;
        public string timelineId;
        public string trackId;
        public string clipId;
        public AgentPackageObjectReference asset;
    }

    [Serializable]
    public sealed class AgentPackageSkillGraphAnchor
    {
        public string kind;
        public string nodeId;
        public List<AgentPackageSkillFlowStep> steps = new List<AgentPackageSkillFlowStep>();
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowStep
    {
        public string id;
        public string name;
        public string conditionGraphId;
        public int priority;
        public string abortPolicy;
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowNode
    {
        public string id;
        public string capability;
        public string name;
        public JObject properties = new JObject();
        public JObject values = new JObject();
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowEdgeEndpoint
    {
        public string node;
        public string port;
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowEdge
    {
        public string id;
        public string kind;
        public AgentPackageSkillFlowEdgeEndpoint from;
        public AgentPackageSkillFlowEdgeEndpoint to;
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowGraphLayoutFile
    {
        public string graphId;
        public List<AgentPackageSkillFlowNodeLayout> nodes = new List<AgentPackageSkillFlowNodeLayout>();
    }

    [Serializable]
    public sealed class AgentPackageSkillFlowNodeLayout
    {
        public string id;
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class AgentPackageSkillMacroFile
    {
        public string id;
        public string graphId;
        public string ownership;
        public AgentPackageSkillGraphOwner owner;
        public AgentPackageObjectReference asset;
        public List<AgentPackageSkillMacroParameter> inputs = new List<AgentPackageSkillMacroParameter>();
        public List<AgentPackageSkillMacroParameter> outputs = new List<AgentPackageSkillMacroParameter>();
    }

    [Serializable]
    public sealed class AgentPackageSkillMacroParameter
    {
        public string id;
        public string name;
        public string valueType;
    }

    [Serializable]
    public sealed class AgentPackageSkillBlackboardDeclaration
    {
        public string id;
        public string key;
        public string valueType;
        public string scope;
        public string lifetime;
        public string category;
        public JToken defaultValue;
        public AgentPackageSkillBlackboardInputBinding inputBinding;
        public AgentPackageSkillBlackboardFactProjection factProjection;
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineFile
    {
        public string id;
        public string name;
        public string ownership;
        public AgentPackageObjectReference asset;
        public string ownerGraphId;
        public string ownerNodeId;
        public List<AgentPackageSkillTimelineCallSite> callSites = new List<AgentPackageSkillTimelineCallSite>();
        public List<AgentPackageSkillTimelineSection> sections = new List<AgentPackageSkillTimelineSection>();
        public List<AgentPackageSkillTimelineExternalBinding> externalBindings = new List<AgentPackageSkillTimelineExternalBinding>();
        public List<AgentPackageSkillTimelineTrack> tracks = new List<AgentPackageSkillTimelineTrack>();
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineCallSite
    {
        public string graphId;
        public string nodeId;
        public string playbackMode;
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineSection
    {
        public string id;
        public string name;
        public int frame;
        public string nextSectionId;
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineExternalBinding
    {
        public string id;
        public string bindingId;
        public string displayName;
        public string domain;
        public string parameterId;
        public string valueKind;
        public string access;
        public string lifetime;
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineTrack
    {
        public string id;
        public string kind;
        public string name;
        public string animationChannelId;
        public string animationSlotId;
        public List<AgentPackageSkillTimelineClip> clips = new List<AgentPackageSkillTimelineClip>();
    }

    [Serializable]
    public sealed class AgentPackageSkillTimelineClip
    {
        public string id;
        public string kind;
        public int startFrame;
        public int endFrame;
        public int otherEaseInFrame;
        public int otherEaseOutFrame;
        public int selfEaseInFrame;
        public int selfEaseOutFrame;
        public int clipInFrame;
        public JObject properties = new JObject();
        public AgentPackageObjectReference animationClip;
        public string treeGraphId;
        public string treeOwnership;
        public string treePhase;
        [JsonIgnore]
        public List<AgentPackageCurve> curves = new List<AgentPackageCurve>();
    }

    [Serializable]
    public sealed class AgentPackageSkillNodeKindDescriptor
    {
        public string kind;
        public List<string> graphRoles = new List<string>();
        public List<string> properties = new List<string>();
        public List<AgentPackagePortDescriptor> flowPorts = new List<AgentPackagePortDescriptor>();
        public List<AgentPackagePortDescriptor> valuePorts = new List<AgentPackagePortDescriptor>();
        public bool canCreate;
        public bool canConfigure;
    }

    internal static class AgentSkillFlowDocumentClone
    {
        public static AgentPackageSkillFlowDocument Clone(AgentPackageSkillFlowDocument source)
        {
            AgentPackageSkillFlowDocument result = AgentAuthoringDocumentCodec.Clone(source) ??
                new AgentPackageSkillFlowDocument();
            if (source?.timelines == null || result.timelines == null)
                return result;

            foreach (AgentPackageSkillTimelineFile sourceTimeline in source.timelines)
            {
                AgentPackageSkillTimelineFile targetTimeline = result.timelines.FirstOrDefault(value =>
                    value != null && value.id == sourceTimeline?.id);
                if (sourceTimeline == null || targetTimeline == null)
                    continue;
                foreach (AgentPackageSkillTimelineTrack sourceTrack in sourceTimeline.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                {
                    AgentPackageSkillTimelineTrack targetTrack = targetTimeline.tracks?.FirstOrDefault(value =>
                        value != null && value.id == sourceTrack?.id);
                    if (sourceTrack == null || targetTrack == null)
                        continue;
                    foreach (AgentPackageSkillTimelineClip sourceClip in sourceTrack.clips ?? new List<AgentPackageSkillTimelineClip>())
                    {
                        AgentPackageSkillTimelineClip targetClip = targetTrack.clips?.FirstOrDefault(value =>
                            value != null && value.id == sourceClip?.id);
                        if (sourceClip == null || targetClip == null)
                            continue;
                        targetClip.curves = (sourceClip.curves ?? new List<AgentPackageCurve>())
                            .Select(value => AgentAuthoringDocumentCodec.Clone(value))
                            .ToList();
                    }
                }
            }
            return result;
        }
    }
}
