using System;
using System.Collections.Generic;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringSchema
    {
        public const string Version = "btsmtl-agent-authoring-document.v8";
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

    [Serializable]
    public sealed class AgentBodyMotionProfile
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
    public sealed class AgentPackageSkillBlackboardInputBinding
    {
        public string inputValueId;
    }

    [Serializable]
    public sealed class AgentPackageSkillBlackboardFactProjection
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
    public sealed class AgentInputValue
    {
        public string inputValueId;
        public string valueType;
    }

    [Serializable]
    public sealed class AgentActionRequest
    {
        public string requestId;
        public float bufferSeconds;
        public int priority;
        public string timingClass;
    }

    [Serializable]
    public sealed class AgentActionProfile
    {
        public string actionId;
        public string displayName;
        public string assetPath;
        public string assetGuid;
        public string targetRequirement;
        public List<string> grantedTags = new List<string>();
        public AgentGameplayTagQuery blockQuery = new AgentGameplayTagQuery();
        public AgentGameplayTagQuery cancelQuery = new AgentGameplayTagQuery();
    }

    [Serializable]
    public sealed class AgentGameplayTagQuery
    {
        public List<string> all = new List<string>();
        public List<string> any = new List<string>();
        public List<string> none = new List<string>();
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
