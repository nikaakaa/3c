using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringSchema
    {
        public const string Version = "btsmtl-agent-authoring-document.v4";
        public const string CharacterControllerDomain = "CharacterController";
        public const string AIControllerDomain = "AIController";

        public static bool IsDomain(string domain)
        {
            return string.Equals(domain, CharacterControllerDomain, StringComparison.Ordinal) ||
                   string.Equals(domain, AIControllerDomain, StringComparison.Ordinal);
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
        public string rootTreeAssetPath;
        public string rootGraphAuthoringId;
        public int blackboardSchemaRevision;
        public string programId;
        public string sourceRevision;
        public string semanticHash;
        public string numericProfileId;
        public int targetAbiVersion;
        public string programHash;
        public string layoutHash;
        public AgentSnapshotBodyMotionProfile bodyMotion = new AgentSnapshotBodyMotionProfile();
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
        public AgentSnapshotAIController aiController;
    }

    [Serializable]
    public sealed class AgentSnapshotAIController
    {
        public string controllerId;
        public string definitionAssetPath;
        public string definitionAssetGuid;
        public string treeAssetPath;
        public string treeAssetGuid;
        public string graphAuthoringId;
        public string authoringRole;
        public string sourceRevision;
        public string perceptionAssetPath;
        public string perceptionAssetGuid;
        public string candidateOrdering;
        public List<string> candidateActorIds = new List<string>();
        public string controlledCharacterAssetPath;
        public string controlledCharacterAssetGuid;
        public string characterProgramId;
        public string characterProgramHash;
        public bool characterProgramStale;
        public List<AgentSnapshotInputValue> inputValues = new List<AgentSnapshotInputValue>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public List<AgentSnapshotAIBlackboardDeclaration> blackboardDeclarations = new List<AgentSnapshotAIBlackboardDeclaration>();
        public List<AgentSnapshotAINode> nodes = new List<AgentSnapshotAINode>();
        public string intentProgramAssetPath;
        public string intentProgramAssetGuid;
        public string intentProgramId;
        public string intentProgramHash;
        public string intentProgramSourceRevision;
        public bool intentProgramStale;
    }

    [Serializable]
    public sealed class AgentSnapshotAIBlackboardDeclaration
    {
        public string declarationAuthoringId;
        public string ownerGraphAuthoringId;
        public string displayName;
        public string valueType;
        public string scope;
        public string lifetime;
        public string defaultValue;
    }

    [Serializable]
    public sealed class AgentSnapshotAINode
    {
        public string graphAuthoringId;
        public string nodeAuthoringId;
        public string nodeType;
        public string capability;
        public string memoryDeclarationAuthoringId;
        public string memoryValueKind;
        public string inputId;
        public string requestId;
        public float requestBufferSeconds;
        public int requestPriority;
        public string requestRepeatPolicy;
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
        public List<AgentSnapshotBlackboardWriteSummary> blackboardWrites = new List<AgentSnapshotBlackboardWriteSummary>();
    }

    [Serializable]
    public sealed class AgentSnapshotBlackboardWriteSummary
    {
        public string nodeAuthoringId;
        public string declarationAuthoringId;
        public string declarationOwnerId;
        public string key;
        public string valueType;
        public bool boolValue;
        public string lifecyclePhase;
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
        public List<AgentSnapshotConditionTerm> conditionTerms = new List<AgentSnapshotConditionTerm>();
    }

    [Serializable]
    public sealed class AgentSnapshotConditionTerm
    {
        public string kind;
        public bool negate;
        public string request;
        public string blackboardKey;
        public string windowType;
        public string actionProfile;
        public string actionProfileAssetPath;
        public string actionProfileAssetGuid;
        public string targetSnapshotBlackboardKey;
        public string compareType;
    }

    [Serializable]
    public sealed class AgentSnapshotActionActivationSummary
    {
        public string nodeAuthoringId;
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
    public sealed class AgentSnapshotBlackboardDeclaration
    {
        public string declarationId;
        public string ownerId;
        public string graphPath;
        public string key;
        public string valueType;
        public JToken defaultValue;
        public string scope;
        public string lifetime;
        public AgentSnapshotBlackboardInputBinding inputBinding;
        public AgentSnapshotBlackboardFactProjection factProjection;
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
        public int clipInFrame;
        public string extraPolationMode;
        public string phase;
        public string ownership;
        public string treeName;
        public List<AgentSnapshotTreeClipWrite> writes = new List<AgentSnapshotTreeClipWrite>();
    }

    [Serializable]
    public sealed class AgentSnapshotTreeClipWrite
    {
        public string declarationId;
        public string declarationOwnerId;
        public string blackboardKey;
    }

    [Serializable]
    public sealed class AgentSnapshotTimeline
    {
        public string timelineAuthoringId;
        public string name;
        public List<AgentSnapshotTimelineCallSite> callSites = new List<AgentSnapshotTimelineCallSite>();
        public List<AgentSnapshotTimelineSection> sections = new List<AgentSnapshotTimelineSection>();
        public List<AgentSnapshotTimelineTrack> tracks = new List<AgentSnapshotTimelineTrack>();
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineSection
    {
        public string sectionAuthoringId;
        public string name;
        public int frame;
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineCallSite
    {
        public string nodeAuthoringId;
        public string graphPath;
        public string playbackMode;
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineTrack
    {
        public string trackAuthoringId;
        public string typeName;
        public string name;
        public int index;
        public string animationChannelId;
        public bool motionWarpTrack;
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
        public int otherEaseInFrame;
        public int otherEaseOutFrame;
        public int selfEaseInFrame;
        public int selfEaseOutFrame;
        public int easeInFrame;
        public int easeOutFrame;
        public int clipInFrame;
        public string extraPolationMode;
        public AgentPackageAssetReferenceV4 animationClip;
        public string curveId;
        public int curveEndFrame;
        public string motionSpace;
        public string motionChannel;
        public string motionBlendMode;
        public int motionPriority;
        public bool consumeLowerChannels;
        public bool motionWarpClip;
        public string sourceMotionClipAuthoringId;
        public string sourceMotionClipPath;
        public string translationMode;
        public string targetOffsetSpace;
        public string rotationMode;
        public string rotationMethod;
        public AgentSnapshotVector2 targetPlanarOffset;
        public float targetYawOffsetDegrees;
        public float maxTotalPositionCorrection;
        public float maxTotalYawCorrectionDegrees;
        public float maximumYawRateDegreesPerSecond;
        public string limitPolicy;
        public List<AgentSnapshotTimelineCurveChannel> curveChannels = new List<AgentSnapshotTimelineCurveChannel>();
    }

    [Serializable]
    public sealed class AgentSnapshotTimelineCurveChannel
    {
        public string channelId;
        public string displayName;
        public string timeDomain;
        public bool bounded;
        public float minimum;
        public float maximum;
        public float zero;
        public string unit;
        public string preWrapMode;
        public string postWrapMode;
        public List<AgentAnimationCurveKey> keys = new List<AgentAnimationCurveKey>();
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
    public sealed class AgentSnapshotLifecycleSummary
    {
        public string nodeAuthoringId;
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
    public sealed class AgentSnapshotVector2
    {
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class AgentSnapshotNode
    {
        public string elementAuthoringId;
        public string typeName;
        public string displayName;
        public string nodeTypeDisplayName;
        public AgentSnapshotVector2 position;
        public List<AgentSnapshotGraphReference> graphReferences = new List<AgentSnapshotGraphReference>();
        public List<AgentSnapshotAssetReference> assetReferences = new List<AgentSnapshotAssetReference>();
        public List<AgentSnapshotPropertyPort> propertyPorts = new List<AgentSnapshotPropertyPort>();
        public AgentSnapshotExposedProperty exposedProperty;
        public string loopStopType;
        public string compareType;
        public float moveSpeed;
        public string displacementMode;
        public float turnSpeedDegrees;
        public bool cameraRelative;
        public string executionMode;
        public float durationSeconds;
        public string inputId;
        public string requestId;
        public string blackboardDeclarationId;
        public string stateExitCause;
        public string actionContextId;
        public string windowType;
        public string actionProfileId;
        public string targetSnapshotBlackboardDeclarationId;
    }

    [Serializable]
    public sealed class AgentSnapshotExposedProperty
    {
        public string mode;
        public string declarationAuthoringId;
        public string declarationOwnerId;
        public string key;
        public string valueType;
        public JToken value;
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
        public string abortPolicy;
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
        public List<AgentSnapshotAnimationProducer> producers = new List<AgentSnapshotAnimationProducer>();
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
    public sealed class AgentSnapshotAnimationProducer
    {
        public AgentSnapshotAuthoringRoute route = new AgentSnapshotAuthoringRoute();
        public string ownerKind;
        public string timelineAuthoringId;
        public string trackAuthoringId;
        public string timelineName;
        public string trackName;
        public string actionContextId;
        public string animationChannelId;
        public string sourceAssetPath;
        public string sourceAssetGuid;
        public string sourceAssetType;
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
        public string graphAuthoringId;
        public string graphPlannedIdentity;
        public string targetGraphAuthoringId;
        public string targetGraphPlannedIdentity;
        public string stateMachineGraphAuthoringId;
        public string stateMachinePlannedIdentity;
        public string stateAuthoringId;
        public string statePlannedIdentity;
        public string fromElementAuthoringId;
        public string fromPlannedIdentity;
        public string toElementAuthoringId;
        public string toPlannedIdentity;
        public string sourceElementAuthoringId;
        public string sourcePlannedIdentity;
        public string targetElementAuthoringId;
        public string targetPlannedIdentity;
        public string flowEdgeAuthoringId;
        public string flowEdgePlannedIdentity;
        public string timelineAuthoringId;
        public string timelinePlannedIdentity;
        public string sectionAuthoringId;
        public string trackAuthoringId;
        public string trackPlannedIdentity;
        public string clipAuthoringId;
        public string clipPlannedIdentity;
        public string sourceMotionClipAuthoringId;
        public string declarationAuthoringId;
        public string declarationPlannedIdentity;
        public string graph;
        public string targetGraph;
        public string stateMachine;
        public string state;
        public string displayName;
        public string nodeType;
        public string exposedPropertyMode;
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
        public string targetRequirement;
        public string actionContext;
        public string actionContextAssetPath;
        public string actionContextAssetGuid;
        public string request;
        public string requestTimingClass;
        public string blackboardKey;
        public string blackboardValueType;
        public JToken blackboardDefaultValue;
        public string blackboardScope;
        public string blackboardLifetime;
        public int blackboardSchemaRevision;
        public AgentSnapshotBlackboardInputBinding inputBinding;
        public AgentSnapshotBlackboardFactProjection factProjection;
        public string windowType;
        public string windowId;
        public ulong digest;
        public string categoryPath;
        public bool blackboardBoolValue;
        public int blackboardIntValue;
        public float blackboardFloatValue;
        public Vector2 blackboardVector2Value;
        public Vector3 blackboardVector3Value;
        public string blackboardActorIdValue;
        public string blackboardTargetActorIdValue;
        public Vector3 blackboardTargetPositionValue;
        public float blackboardTargetYawValue;
        public int startFrame;
        public int endFrame;
        public int clipInFrame;
        public string extraPolationMode;
        public string animationChannelId;
        public AgentPackageAssetReferenceV4 animationClip;
        public int frameOffset;
        public int selfEaseInFrame;
        public int selfEaseOutFrame;
        public string timelinePhase;
        public string translationMode;
        public string targetOffsetSpace;
        public string rotationMode;
        public string rotationMethod;
        public Vector2 targetPlanarOffset;
        public float targetYawOffsetDegrees;
        public float maxTotalPositionCorrection;
        public float maxTotalYawCorrectionDegrees;
        public float maximumYawRateDegreesPerSecond;
        public string limitPolicy;
        public List<AgentAnimationCurveKey> positionProgressCurve = new List<AgentAnimationCurveKey>();
        public List<AgentAnimationCurveKey> yawProgressCurve = new List<AgentAnimationCurveKey>();
        public string curveChannelId;
        public string curveId;
        public int curveEndFrame;
        public string motionSpace;
        public string motionChannel;
        public string motionBlendMode;
        public int motionPriority;
        public bool consumeLowerChannels;
        public AgentAnimationCurvePayload curve = new AgentAnimationCurvePayload();
        public string gameplayTag;
        public string parentGameplayTag;
        public string debugCategory;
        public List<string> grantedTags = new List<string>();
        public List<string> queryAll = new List<string>();
        public List<string> queryAny = new List<string>();
        public List<string> queryNone = new List<string>();
        public List<AgentConditionGroup> conditionGroups = new List<AgentConditionGroup>();
        public List<AgentConditionGroup> cancelConditionGroups = new List<AgentConditionGroup>();
        public string sourceInputRequestId;
        public bool consumeSourceInputRequest = true;
        public string targetKey;
        public string targetSnapshotBlackboardKey;
        public string targetSnapshotBlackboardDeclarationId;
        public string targetSnapshotBlackboardDeclarationPlannedIdentity;
        public string lifecycleType;
        public string reason;
        public string completeReason;
        public string interruptReason;
        public string abortReason;
        public string inputId;
        public string conditionValueConfiguration;
        public string stateExitCause;
        public string controllerId;
        public string rootTreeAssetPath;
        public string controlledCharacterAssetPath;
        public string controlledCharacterAssetGuid;
        public string perceptionProfileAssetPath;
        public string perceptionProfileAssetGuid;
        public string candidateOrdering;
        public List<string> candidateActorIds = new List<string>();
        public string aiNodeKind;
        public string loopStopType;
        public string compareType;
        public float moveSpeed;
        public string displacementMode;
        public string actionMotionCurve;
        public string actionMotionCurveAssetPath;
        public string actionMotionCurveAssetGuid;
        public float turnSpeedDegrees;
        public bool cameraRelative;
        public string executionMode;
        public float durationSeconds;
        public string abortPolicy;
        public string aiMemoryValueKind;
        public string aiRequestRepeatPolicy;
        public float requestBufferSeconds;
        public int requestPriority;
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
        public string windowType;
        public string actionProfile;
        public string actionProfileAssetPath;
        public string actionProfileAssetGuid;
        public string targetSnapshotBlackboardKey;
        public string compareType;
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
        public int repairIterations;
        public int diffSize;
        public int businessCoverageCount;
        public int businessCoverageMissingCount;
    }
}
