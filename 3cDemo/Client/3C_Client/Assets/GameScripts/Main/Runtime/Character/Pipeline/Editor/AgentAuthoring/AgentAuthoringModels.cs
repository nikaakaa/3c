using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringSchema
    {
        public const string Version = "agent-character-controller-synthesis.v6";
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
        public string exportMode = AgentSnapshotExportMode.Compact.ToString();
        public string definitionName;
        public string definitionAssetPath;
        public string rootTreeAssetPath;
        public string rootGraphAuthoringId;
        public string programId;
        public ulong compilationRevision;
        public string sourceContentHash;
        public List<AgentSnapshotGraphSummary> graphSummaries = new List<AgentSnapshotGraphSummary>();
        public List<AgentSnapshotStateMachineSummary> stateMachines = new List<AgentSnapshotStateMachineSummary>();
        public List<AgentSnapshotGraph> graphs = new List<AgentSnapshotGraph>();
        public List<AgentSnapshotInputValue> inputValues = new List<AgentSnapshotInputValue>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public List<AgentSnapshotActionProfile> actionProfiles = new List<AgentSnapshotActionProfile>();
        public AgentSnapshotAnimationPresentation presentation = new AgentSnapshotAnimationPresentation();
        public List<AgentSnapshotBlackboardDeclaration> blackboardDeclarations = new List<AgentSnapshotBlackboardDeclaration>();
        public List<AgentSnapshotTimeline> timelines = new List<AgentSnapshotTimeline>();
        public List<AgentSnapshotTimelineTreeClip> timelineTreeClips = new List<AgentSnapshotTimelineTreeClip>();
        public List<AgentSnapshotAsset> timelineAssets = new List<AgentSnapshotAsset>();
        public List<AgentSnapshotAsset> actionContextAssets = new List<AgentSnapshotAsset>();
    }

    [Serializable]
    public sealed class AgentSnapshotGraphSummary
    {
        public string graphAuthoringId;
        public string path;
        public string name;
        public string kind;
        public string ownership;
        public string ownerNode;
        public string referenceKey;
        public List<AgentSnapshotAuthoringRoute> routes = new List<AgentSnapshotAuthoringRoute>();
    }

    [Serializable]
    public sealed class AgentSnapshotStateMachineSummary
    {
        public string graphAuthoringId;
        public string graphPath;
        public string name;
        public string ownerNode;
        public List<AgentSnapshotAuthoringRoute> routes = new List<AgentSnapshotAuthoringRoute>();
        public List<AgentSnapshotStateSummary> states = new List<AgentSnapshotStateSummary>();
        public List<AgentSnapshotTransitionSummary> transitions = new List<AgentSnapshotTransitionSummary>();
    }

    [Serializable]
    public sealed class AgentSnapshotStateSummary
    {
        public string stateAuthoringId;
        public string state;
        public string behaviorGraphAuthoringId;
        public string behaviorGraphPath;
        public List<AgentSnapshotNestedStateMachineSummary> nestedStateMachines = new List<AgentSnapshotNestedStateMachineSummary>();
        public List<AgentSnapshotActionActivationSummary> actionActivations = new List<AgentSnapshotActionActivationSummary>();
        public List<AgentSnapshotTimelineBindingSummary> timelines = new List<AgentSnapshotTimelineBindingSummary>();
        public List<AgentSnapshotLifecycleSummary> lifecycleTransitions = new List<AgentSnapshotLifecycleSummary>();
    }

    [Serializable]
    public sealed class AgentSnapshotNestedStateMachineSummary
    {
        public string nodeAuthoringId;
        public string node;
        public string graphAuthoringId;
        public string graphPath;
        public string ownership;
    }

    [Serializable]
    public sealed class AgentSnapshotTransitionSummary
    {
        public string edgeAuthoringId;
        public string fromElementAuthoringId;
        public string toElementAuthoringId;
        public string from;
        public string to;
        public int priority;
        public List<string> requests = new List<string>();
        public List<string> conditions = new List<string>();
    }

    [Serializable]
    public sealed class AgentSnapshotActionActivationSummary
    {
        public string displayName;
        public string actionProfile;
        public string sourceRequest;
        public string actionContext;
        public string targetKey;
        public string targetSnapshotBlackboardKey;
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineBindingSummary
    {
        public string nodeAuthoringId;
        public string timelineAuthoringId;
        public string displayName;
        public string timeline;
        public string ownership;
        public string graphPath;
        public string timelineAssetPath;
        public string timelineAssetGuid;
        public string actionContext;
        public string playbackMode;
        public int trackCount;
        public int clipCount;
    }

    [Serializable]
    public sealed class AgentSnapshotBlackboardDeclaration
    {
        public string declarationId;
        public string ownerId;
        public string graphPath;
        public string key;
        public string valueType;
        public string scope;
        public string lifetime;
        public string authority;
        public string syncPolicy;
        public string factProjection;
        public string windowType;
        public string windowId;
        public ulong digest;
        public string categoryPath;
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineTreeClip
    {
        public string timelineAuthoringId;
        public string trackAuthoringId;
        public string clipAuthoringId;
        public string timeline;
        public string timelineNodePath;
        public string timelineOwnership;
        public string timelineAssetPath;
        public int trackIndex;
        public int clipIndex;
        public int startFrame;
        public int endFrame;
        public string phase;
        public string ownership;
        public string treeName;
        public List<string> blackboardOutputs = new List<string>();
        public List<string> projectedFacts = new List<string>();
    }

    [Serializable]
    public sealed class AgentSnapshotTimeline
    {
        public string timelineAuthoringId;
        public string name;
        public List<AgentSnapshotTimelineTrack> tracks = new List<AgentSnapshotTimelineTrack>();
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineTrack
    {
        public string trackAuthoringId;
        public string typeName;
        public string name;
        public int index;
        public string layerId;
        public List<AgentSnapshotTimelineClip> clips = new List<AgentSnapshotTimelineClip>();
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineClip
    {
        public string clipAuthoringId;
        public string typeName;
        public int index;
        public int startFrame;
        public int endFrame;
        public string animationClipAssetPath;
        public string animationClipAssetGuid;
    }

    [Serializable]
    public sealed class AgentSnapshotLifecycleSummary
    {
        public string displayName;
        public string transitionType;
        public string reason;
        public string actionContext;
    }

    [Serializable]
    public sealed class AgentSnapshotGraph
    {
        public string graphAuthoringId;
        public string path;
        public string name;
        public string kind;
        public string ownership;
        public string ownerElementAuthoringId;
        public string referenceKey;
        public string sharedAssetPath;
        public List<AgentSnapshotAuthoringRoute> routes = new List<AgentSnapshotAuthoringRoute>();
        public List<AgentSnapshotNode> nodes = new List<AgentSnapshotNode>();
        public List<AgentSnapshotFlowEdge> flowEdges = new List<AgentSnapshotFlowEdge>();
        public List<AgentSnapshotPropertyEdge> propertyEdges = new List<AgentSnapshotPropertyEdge>();
    }

    [Serializable]
    public sealed class AgentSnapshotNode
    {
        public string elementAuthoringId;
        public string typeName;
        public string displayName;
        public string nodeTypeDisplayName;
        public Vector2 position;
        public List<AgentSnapshotGraphReference> graphReferences = new List<AgentSnapshotGraphReference>();
        public List<AgentSnapshotAssetReference> assetReferences = new List<AgentSnapshotAssetReference>();
        public List<AgentSnapshotPropertyPort> propertyPorts = new List<AgentSnapshotPropertyPort>();
    }

    [Serializable]
    public sealed class AgentSnapshotGraphReference
    {
        public string key;
        public string label;
        public string graphAuthoringId;
        public string graphPath;
        public string graphKind;
        public string ownership;
        public string scopeId;
        public string sharedAssetPath;
        public bool required;
    }

    [Serializable]
    public sealed class AgentSnapshotAssetReference
    {
        public string key;
        public string label;
        public string assetPath;
        public string assetGuid;
        public string assetType;
        public bool required;
    }

    [Serializable]
    public sealed class AgentSnapshotPropertyPort
    {
        public string portId;
        public string displayName;
        public string direction;
        public string valueType;
    }

    [Serializable]
    public sealed class AgentSnapshotFlowEdge
    {
        public string elementAuthoringId;
        public string startElementAuthoringId;
        public string endElementAuthoringId;
        public string startPort;
        public string endPort;
        public int flowOrder;
        public int transitionPriority;
        public string conditionRuleGraphAuthoringId;
        public string conditionRuleGraphPath;
    }

    [Serializable]
    public sealed class AgentSnapshotPropertyEdge
    {
        public string elementAuthoringId;
        public string startElementAuthoringId;
        public string endElementAuthoringId;
        public string startPortId;
        public string endPortId;
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
    }

    [Serializable]
    public sealed class AgentSnapshotActionProfile
    {
        public string actionId;
        public string displayName;
        public string assetPath;
        public string assetGuid;
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationLayer
    {
        public string layerId;
        public int order;
        public int animancerLayerIndex;
        public string avatarMaskAssetPath;
        public string avatarMaskAssetGuid;
        public string blendMode;
        public string outputPolicy;
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationPresentation
    {
        public string definitionAssetPath;
        public string transitionLibraryAssetPath;
        public string transitionLibraryAssetGuid;
        public List<AgentSnapshotAnimationLayer> layers = new List<AgentSnapshotAnimationLayer>();
        public List<AgentSnapshotAnimationProducer> producers = new List<AgentSnapshotAnimationProducer>();
    }

    [Serializable]
    public sealed class AgentSnapshotAnimationProducer
    {
        public AgentSnapshotAuthoringRoute route = new AgentSnapshotAuthoringRoute();
        public string timelineAuthoringId;
        public string trackAuthoringId;
        public string timelineName;
        public string trackName;
        public string layerId;
        public string transitionAssetPath;
        public string transitionAssetGuid;
        public string easing;
    }

    [Serializable]
    public sealed class AgentSnapshotAuthoringRoute
    {
        public string rootGraphAuthoringId;
        public List<AgentSnapshotAuthoringRouteSegment> segments = new List<AgentSnapshotAuthoringRouteSegment>();
    }

    [Serializable]
    public sealed class AgentSnapshotAuthoringRouteSegment
    {
        public string kind;
        public string ownerElementKind;
        public string ownerGraphAuthoringId;
        public string ownerElementAuthoringId;
        public string referenceKey;
        public string scopeId;
        public string childGraphAuthoringId;
        public string ownership;
        public string timelineAuthoringId;
        public string trackAuthoringId;
        public string clipAuthoringId;
    }

    [Serializable]
    public sealed class AgentSnapshotAsset
    {
        public string id;
        public string name;
        public string assetPath;
        public string assetGuid;
        public string assetType;
    }

    [Serializable]
    public sealed class AgentControllerIntent
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string macro;
        public string target = "ActionStateMachine";
        public string stateMachine;
        public string categoryState = "Attack";
        public string nestedStateMachine;
        public string request;
        public string actionContext;
        public string actionContextAssetPath;
        public string actionContextAssetGuid;
        public List<AgentControllerIntentStep> steps = new List<AgentControllerIntentStep>();
        public List<AgentControllerIntentCancel> cancel = new List<AgentControllerIntentCancel>();
        public List<string> locomotionStates = new List<string>();
        public string hitReactionState;
        public string hitReactionTimeline;
        public string hitReactionActionProfile;
    }

    [Serializable]
    public sealed class AgentControllerIntentStep
    {
        public string state;
        public string request;
        public string actionProfile;
        public string timeline;
        public string timelineOwnership = AgentTimelineOwnership.Inline.ToString();
        public string timelineAssetPath;
        public string timelineAssetGuid;
    }

    [Serializable]
    public sealed class AgentControllerIntentCancel
    {
        public string request;
        public string from;
        public string to;
        public string reason;
        public string blackboardKey;
    }

    [Serializable]
    public sealed class AgentPatchIR
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string sourceMacro;
        public string sourceMacroVersion;
        public List<AgentPatchOperation> operations = new List<AgentPatchOperation>();
    }

    [Serializable]
    public sealed class AgentPatchOperation
    {
        public string id;
        public string op;
        public string graphAuthoringId;
        public string graphOperationId;
        public string targetGraphAuthoringId;
        public string targetGraphOperationId;
        public string stateMachineGraphAuthoringId;
        public string stateMachineOperationId;
        public string stateAuthoringId;
        public string stateOperationId;
        public string fromElementAuthoringId;
        public string fromOperationId;
        public string toElementAuthoringId;
        public string toOperationId;
        public string sourceElementAuthoringId;
        public string sourceOperationId;
        public string targetElementAuthoringId;
        public string targetOperationId;
        public string timelineAuthoringId;
        public string trackAuthoringId;
        public string clipAuthoringId;
        public string graph;
        public string targetGraph;
        public string stateMachine;
        public string state;
        public string displayName;
        public string nodeType;
        public string from;
        public string to;
        public string sourceNode;
        public string targetNode;
        public string startPort;
        public string endPort;
        public string startPropertyPort;
        public string endPropertyPort;
        public string lifecycleSlot;
        public string timeline;
        public string timelineOwnership = AgentTimelineOwnership.Inline.ToString();
        public string timelineAssetPath;
        public string timelineAssetGuid;
        public string actionProfile;
        public string actionContext;
        public string actionContextAssetPath;
        public string actionContextAssetGuid;
        public string request;
        public List<AgentConditionGroup> conditionGroups = new List<AgentConditionGroup>();
        public List<AgentConditionTerm> cancelGuards = new List<AgentConditionTerm>();
        public string sourceInputRequestId;
        public bool consumeSourceInputRequest = true;
        public string targetKey;
        public string targetSnapshotBlackboardKey;
        public string lifecycleType;
        public string reason;
        public string completeReason;
        public string abortReason;
        public string inputId;
        public string inputValueType;
        public int transitionPriority;
        public Vector2 position;
    }

    [Serializable]
    public sealed class AgentConditionGroup
    {
        public List<AgentConditionTerm> terms = new List<AgentConditionTerm>();
    }

    [Serializable]
    public sealed class AgentConditionTerm
    {
        public string kind;
        public string blackboardKey;
        public bool negate;
        public string from;
        public string to;
        public string request;
    }

    [Serializable]
    public sealed class AgentCompileReport
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public bool success;
        public bool applied;
        public AgentEvaluationMetrics metrics = new AgentEvaluationMetrics();
        public List<AgentCompileMessage> messages = new List<AgentCompileMessage>();
        public List<AgentCompileDiffEntry> plannedDiff = new List<AgentCompileDiffEntry>();
        public List<AgentCompileDiffEntry> appliedDiff = new List<AgentCompileDiffEntry>();

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
        public string operationId;
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
        public int repairIterations;
        public int diffSize;
        public int businessCoverageCount;
        public int businessCoverageMissingCount;
    }
}
