using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringSchema
    {
        public const string Version = "btsmtl-agent-authoring-document.v7";
        public const string CharacterControllerDomain = "CharacterController";

        public static bool IsDomain(string domain)
        {
            return string.Equals(domain, CharacterControllerDomain, StringComparison.Ordinal);
        }
    }

    public enum AgentGraphKind
    {
        Unknown,
        BaseTree,
        RunnableTree,
        SubTree,
        StateBehaviorSubTree,
        StateMachineGraph,
        ConditionRuleGraph
    }

    public enum AgentGraphOwnership
    {
        Unknown,
        RootAsset,
        Inline,
        SharedAsset
    }

    public enum AgentTimelineOwnership
    {
        Inline,
        Shared
    }

    public enum AgentReportSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum AgentSnapshotExportMode
    {
        Compact,
        Full
    }

    [Serializable]
    public sealed class AgentGraphSnapshot
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string domain;
        public string rootAssetPath;
        public string rootIdentity;
        public string exportMode = AgentSnapshotExportMode.Compact.ToString();
        public string definitionName;
        public string definitionAssetPath;
        public string controlModuleId;
        public int controlSemanticVersion;
        public List<AgentSnapshotControlParameter> controlParameters = new List<AgentSnapshotControlParameter>();
        public string programId;
        public string sourceRevision;
        public string semanticHash;
        public string numericProfileId;
        public int targetAbiVersion;
        public string programHash;
        public string layoutHash;
        public AgentSnapshotBodyMotionProfile bodyMotion = new AgentSnapshotBodyMotionProfile();
        public List<AgentSnapshotInputValue> inputValues = new List<AgentSnapshotInputValue>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public string inputProviderOwnerId;
        public string gameplayProviderOwnerId;
        public List<AgentSnapshotActionProfile> actionProfiles = new List<AgentSnapshotActionProfile>();
        public List<AgentSnapshotSkillDefinition> skills = new List<AgentSnapshotSkillDefinition>();
        public List<AgentPackageSkillFlowGraphFile> skillGraphs = new List<AgentPackageSkillFlowGraphFile>();
        public List<AgentPackageSkillFlowGraphLayoutFile> skillGraphLayouts = new List<AgentPackageSkillFlowGraphLayoutFile>();
        public List<AgentPackageSkillMacroFile> skillMacros = new List<AgentPackageSkillMacroFile>();
        public List<AgentPackageSkillTimelineFile> skillTimelines = new List<AgentPackageSkillTimelineFile>();
        public AgentSnapshotAnimationPresentation presentation = new AgentSnapshotAnimationPresentation();
    }

    [Serializable]
    public sealed class AgentSnapshotBodyMotionProfile
    {
        public string assetPath;
        public string assetGuid;
        public string sourceIdentity;
        public string contentRevision;
        public int semanticVersion;
        public string requiredWorldCapability;
        public string gravityAcceleration;
        public string maximumFallSpeed;
    }

    [Serializable]
    public sealed class AgentSnapshotBlackboardInputBinding
    {
        public string inputValueId;
    }

    [Serializable]
    public sealed class AgentSnapshotBlackboardFactProjection
    {
        public string kind;
        public string windowType;
        public string windowId;
        public ulong digest;
    }

    [Serializable]
    public sealed class AgentAnimationCurveKey
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
        public float inWeight;
        public float outWeight;
        public string weightedMode = WeightedMode.None.ToString();
    }

    [Serializable]
    public sealed class AgentAnimationCurvePayload
    {
        public string preWrapMode;
        public string postWrapMode;
        public List<AgentAnimationCurveKey> keys = new List<AgentAnimationCurveKey>();
    }

    [Serializable]
    public sealed class AgentSnapshotNode
    {
        public string elementAuthoringId;
        public string typeName;
        public AgentSnapshotExposedProperty exposedProperty;
    }

    [Serializable]
    public sealed class AgentSnapshotExposedProperty
    {
        public string mode;
    }

    [Serializable]
    public sealed class AgentSnapshotInputValue
    {
        public string inputValueId;
        public string valueType;
    }

    [Serializable]
    public sealed class AgentSnapshotActionRequest
    {
        public string requestId;
        public float bufferSeconds;
        public int priority;
        public string timingClass;
    }

    [Serializable]
    public sealed class AgentSnapshotActionProfile
    {
        public string actionId;
        public string displayName;
        public string assetPath;
        public string assetGuid;
        public string targetRequirement;
        public List<string> grantedTags = new List<string>();
        public AgentSnapshotGameplayTagQuery blockQuery = new AgentSnapshotGameplayTagQuery();
        public AgentSnapshotGameplayTagQuery cancelQuery = new AgentSnapshotGameplayTagQuery();
    }

    [Serializable]
    public sealed class AgentSnapshotSkillDefinition
    {
        public string skillId;
        public string entryGraphAuthoringId;
        public string actionProfileId;
        public string actionProfileAssetPath;
        public string actionProfileAssetGuid;
        public string actionContext;
        public string actionContextAssetPath;
        public string actionContextAssetGuid;
        public string sourceInputRequestId;
        public bool consumeSourceInputRequest = true;
        public string targetInputValueId;
        public string targetKey;
        public List<AgentSnapshotSkillSubgraphDependency> subgraphDependencies = new List<AgentSnapshotSkillSubgraphDependency>();
        public List<string> allowedFollowUpSkillIds = new List<string>();
    }

    [Serializable]
    public sealed class AgentSnapshotSkillSubgraphDependency
    {
        public string subgraphIdentity;
        public string callSiteIdentity;
    }

    [Serializable]
    public sealed class AgentSnapshotGameplayTagQuery
    {
        public List<string> all = new List<string>();
        public List<string> any = new List<string>();
        public List<string> none = new List<string>();
    }

    [Serializable]
    public sealed class AgentSnapshotStateLocalPoseSource
    {
        public string graphId;
        public string nodeId;
        public string nodeKind;
        public string ownerKind;
        public string sourceSlotName;
        public string sourceSlotAssetPath;
        public string sourceSlotAssetGuid;
        public long sourceSlotLocalFileId;
        public string sourceKind;
        public string xParameterPortId;
        public string yParameterPortId;
        public string inputRangePolicy;
    }

    [Serializable]
    public sealed class AgentSnapshotActionPlaybackInput
    {
        public string graphId;
        public string nodeId;
        public string ownerKind;
        public string animationChannelId;
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationSlot
    {
        public string graphId;
        public string nodeId;
        public string ownerKind;
        public string animationSlotId;
        public string animationSlotGroupId;
        public string animationChannelId;
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationPresentation
    {
        public string profileAssetPath;
        public string profileAssetGuid;
        public string poseGraphAssetPath;
        public string poseGraphAssetGuid;
        public string poseGraphId;
        public string poseGraphRevision;
        public string rigAssetPath;
        public string rigAssetGuid;
        public string rigId;
        public string rigRevision;
        public string footAnalysisMode;
        public string footAnalysisSourceAssetGuid;
        public string footAnalysisSourceId;
        public int footAnalysisSourceVersion;
        public string footAnalysisAlgorithmVersion;
        public List<AgentSnapshotStateLocalPoseSource> stateLocalPoseSources =
            new List<AgentSnapshotStateLocalPoseSource>();
        public List<AgentSnapshotActionPlaybackInput> actionPlaybackInputs =
            new List<AgentSnapshotActionPlaybackInput>();
        public List<AgentSnapshotAnimationSlot> animationSlots =
            new List<AgentSnapshotAnimationSlot>();
        public List<AgentSnapshotAnimationBlendSpace> blendSpaces = new List<AgentSnapshotAnimationBlendSpace>();
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationBlendSpace
    {
        public string assetPath;
        public string assetGuid;
        public string blendSpaceId;
        public string contentRevision;
        public string mode;
        public string xParameterId;
        public string xUnit;
        public float xMinimum;
        public float xMaximum;
        public string yParameterId;
        public string yUnit;
        public float yMinimum;
        public float yMaximum;
        public int sampleCount;
        public string compileStatus;
        public string projectionRevision;
        public List<string> diagnostics = new List<string>();
    }

    [Serializable]
    public sealed class AgentMutationDraftSet
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string domain;
        public string rootIdentity;
        public string sourceRevision;
        public List<AgentMutationDraft> mutations = new List<AgentMutationDraft>();
    }

    [Serializable]
    public sealed class AgentMutationDraft
    {
        public string id;
        [NonSerialized]
        public string sourcePath;
        public AgentMutationKind kind;
        public string displayName;
        public string gameplayTag;
        public string parentGameplayTag;
        public string debugCategory;
        public List<string> grantedTags = new List<string>();
        public List<string> queryAll = new List<string>();
        public List<string> queryAny = new List<string>();
        public List<string> queryNone = new List<string>();
        public string actionProfile;
        public string targetRequirement;
        public string request;
        public string requestTimingClass;
        public string controlModuleId;
        public int controlSemanticVersion;
        public List<AgentSnapshotControlParameter> controlParameters = new List<AgentSnapshotControlParameter>();
        public AgentPackageSkillFlowDocument skillFlowDocument;
    }

    [Serializable]
    public sealed class AgentCompileReport
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string domain;
        public string rootIdentity;
        public bool success;
        public bool applied;
        public AgentEvaluationMetrics metrics = new AgentEvaluationMetrics();
        public List<AgentCompileMessage> messages = new List<AgentCompileMessage>();
        public List<AgentCompileDiffEntry> plannedDiff = new List<AgentCompileDiffEntry>();
        public List<AgentCompileDiffEntry> appliedDiff = new List<AgentCompileDiffEntry>();
        public List<AgentTouchedOwner> touchedOwners =
            new List<AgentTouchedOwner>();

        public void Info(string path, string code, string message, string suggestion = "")
        {
            Add(AgentReportSeverity.Info, path, code, message, suggestion);
        }

        public void Warning(string path, string code, string message, string suggestion = "")
        {
            Add(AgentReportSeverity.Warning, path, code, message, suggestion);
        }

        public void Error(string path, string code, string message, string suggestion = "")
        {
            Add(AgentReportSeverity.Error, path, code, message, suggestion);
            success = false;
        }

        public bool HasErrors()
        {
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].severity == AgentReportSeverity.Error.ToString())
                    return true;
            }
            return false;
        }

        void Add(
            AgentReportSeverity severity,
            string path,
            string code,
            string message,
            string suggestion)
        {
            messages.Add(new AgentCompileMessage
            {
                severity = severity.ToString(),
                path = path ?? string.Empty,
                code = code ?? string.Empty,
                message = message ?? string.Empty,
                suggestion = suggestion ?? string.Empty
            });
        }
    }

    [Serializable]
    public sealed class AgentTouchedOwner
    {
        public string assetGuid;
        public string assetPath;
        public string assetType;
    }

    [Serializable]
    public sealed class AgentCompileMessage
    {
        public string severity;
        public string path;
        public string code;
        public string message;
        public string suggestion;
    }

    [Serializable]
    public sealed class AgentCompileDiffEntry
    {
        public string mutationId;
        public string action;
        public string graph;
        public string target;
        public string detail;
    }

    [Serializable]
    public sealed class AgentEvaluationMetrics
    {
        public int schemaValidCount;
        public int schemaInvalidCount;
        public int compileSuccessCount;
        public int compileFailureCount;
        public int semanticValidCount;
        public int semanticInvalidCount;
        public int assetResolvedCount;
        public int assetResolveFailureCount;
        public int diffSize;
        public int businessCoverageCount;
        public int businessCoverageMissingCount;
    }
}
