using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public enum AgentDocumentSyncState
    {
        Clean,
        TreeDirty,
        DocumentDirty,
        Conflict,
        ApplyFailed
    }

    [Serializable]
    public sealed class AgentAuthoringPackageManifest
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string domain;
        public string rootIdentity;
        public List<string> files = new List<string>();
    }

    [Serializable]
    public sealed class AgentAuthoringPackageSync
    {
        public string schemaVersion = AgentAuthoringSchema.Version;
        public string domain;
        public string rootIdentity;
        public string rootAssetPath;
        public string baseSourceRevision;
        public string baseEditableHash;
        public string baseContextHash;
    }

    [Serializable]
    public sealed class AgentAuthoringTarget
    {
        public string domain;
        public string rootIdentity;
        public AgentDocumentEditable editable = new AgentDocumentEditable();
        public AgentDocumentContext context = new AgentDocumentContext();
    }

    [Serializable]
    public sealed class AgentDocumentEditable
    {
        public AgentDocumentControlConfiguration control = new AgentDocumentControlConfiguration();
        public List<AgentActionRequest> actionRequests = new List<AgentActionRequest>();
        public List<AgentActionProfile> actionProfiles = new List<AgentActionProfile>();
        public List<AgentPackageSkillDefinitionFile> skills = new List<AgentPackageSkillDefinitionFile>();
        public List<AgentPackageSkillFlowGraphFile> skillGraphs = new List<AgentPackageSkillFlowGraphFile>();
        public List<AgentPackageSkillFlowGraphLayoutFile> skillGraphLayouts = new List<AgentPackageSkillFlowGraphLayoutFile>();
        public List<AgentPackageSkillMacroFile> skillMacros = new List<AgentPackageSkillMacroFile>();
        public List<AgentPackageSkillTimelineFile> skillTimelines = new List<AgentPackageSkillTimelineFile>();
        public AgentDocumentPresentationEditable presentation;
    }

    [Serializable]
    public sealed class AgentDocumentContext
    {
        public string definitionName;
        public string definitionAssetPath;
        [JsonIgnore]
        public string inputProviderOwnerId;
        [JsonIgnore]
        public string gameplayProviderOwnerId;
        public List<AgentInputValue> inputValues = new List<AgentInputValue>();
        public List<AgentActionRequest> actionRequests = new List<AgentActionRequest>();
        public AgentBodyMotionProfile bodyMotion = new AgentBodyMotionProfile();
        public AgentDocumentPresentationContext presentation =
            new AgentDocumentPresentationContext();
        public AgentDocumentGeneratedProduct generatedProduct = new AgentDocumentGeneratedProduct();
        public List<string> capabilities = new List<string>();
    }

    [Serializable]
    public sealed class AgentDocumentPresentationContext
    {
        public AgentPackageObjectReference rig;
        public string rigId;
        public string rigRevision;
        public string rootBonePolicy;
        public string scalePolicy;
        public string pelvisBoneId;
        public AgentDocumentLegChainContext leftLeg = new AgentDocumentLegChainContext();
        public AgentDocumentLegChainContext rightLeg = new AgentDocumentLegChainContext();
        public List<AgentDocumentPoseCapabilityContext> poseCapabilities =
            new List<AgentDocumentPoseCapabilityContext>();
        [JsonIgnore]
        public List<AgentPackageLinkedPoseInterfaceFile> linkedPoseInterfaces =
            new List<AgentPackageLinkedPoseInterfaceFile>();
        public List<AgentDocumentRigBoneContext> physicalBones =
            new List<AgentDocumentRigBoneContext>();
        public List<AgentDocumentVirtualBoneContext> virtualBones =
            new List<AgentDocumentVirtualBoneContext>();
        public List<AgentDocumentStateLocalPoseSourceContext> stateLocalPoseSources =
            new List<AgentDocumentStateLocalPoseSourceContext>();
        public List<AgentDocumentActionPlaybackInputContext> actionPlaybackInputs =
            new List<AgentDocumentActionPlaybackInputContext>();
        public List<AgentDocumentAnimationSlotContext> animationSlots =
            new List<AgentDocumentAnimationSlotContext>();
        public List<AgentDocumentAnimationBlendSpaceContext> blendSpaces =
            new List<AgentDocumentAnimationBlendSpaceContext>();
        public List<AgentDocumentBlendAssetContext> blendCurves =
            new List<AgentDocumentBlendAssetContext>();
        public List<AgentDocumentBlendAssetContext> blendProfiles =
            new List<AgentDocumentBlendAssetContext>();
        public List<AgentDocumentAnimationClipContext> animationClips =
            new List<AgentDocumentAnimationClipContext>();
        public string footAnalysisSourceId;
        public int footAnalysisSourceVersion;
        public string footAnalysisAlgorithmVersion;
    }

    [Serializable]
    public sealed class AgentDocumentStateLocalPoseSourceContext
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
    public sealed class AgentDocumentActionPlaybackInputContext
    {
        public string graphId;
        public string nodeId;
        public string ownerKind;
        public string animationChannelId;
    }

    [Serializable]
    public sealed class AgentDocumentAnimationSlotContext
    {
        public string graphId;
        public string nodeId;
        public string ownerKind;
        public string animationSlotId;
        public string animationSlotGroupId;
        public string animationChannelId;
    }

    [Serializable]
    public sealed class AgentDocumentAnimationBlendSpaceContext
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
    public sealed class AgentDocumentBlendAssetContext
    {
        public string id;
        public string kind;
        public string revision;
        public string rigId;
        public string rigRevision;
        public string assetPath;
        public string assetGuid;
    }

    [Serializable]
    public sealed class AgentDocumentAnimationClipContext
    {
        public string id;
        public string name;
        public AgentPackageObjectReference clip;
        public bool writable;
        public string dependencyBaseline;
        public string analysisInputHash;
        public string registeredCurveHash;
    }

    [Serializable]
    public sealed class AgentDocumentRigBoneContext
    {
        public string id;
        public int parentIndex;
    }

    [Serializable]
    public sealed class AgentDocumentLegChainContext
    {
        public string hipBoneId;
        public string kneeBoneId;
        public string ankleBoneId;
        public string toeBoneId;
    }

    [Serializable]
    public sealed class AgentDocumentPoseCapabilityContext
    {
        public string id;
        public string nodeKind;
        public string executionDomain;
        public bool workerThreadSafe;
        public string workerKernel;
        public List<AgentDocumentPoseCapabilityPortContext> ports =
            new List<AgentDocumentPoseCapabilityPortContext>();
    }

    [Serializable]
    public sealed class AgentDocumentPoseCapabilityPortContext
    {
        public string id;
        public string valueType;
        public string direction;
        public bool required;
    }

    [Serializable]
    public sealed class AgentDocumentVirtualBoneContext
    {
        public string id;
        public string name;
        public string sourcePhysicalBoneId;
        public string targetPhysicalBoneId;
    }

    [Serializable]
    public sealed class AgentDocumentGeneratedProduct
    {
        public string programId;
        public string sourceRevision;
        public string semanticHash;
        public string numericProfileId;
        public int targetAbiVersion;
        public string programHash;
        public string layoutHash;
        public bool stale;
    }

    [Serializable]
    public sealed class AgentPackageControllerFile
    {
        public string controlModuleId;
        public int controlSemanticVersion;
        public List<AgentControlParameter> controlParameters = new List<AgentControlParameter>();
    }

    [Serializable]
    public sealed class AgentDocumentControlConfiguration
    {
        public string moduleId;
        public int semanticVersion;
        public List<AgentControlParameter> parameters = new List<AgentControlParameter>();
    }

    [Serializable]
    public sealed class AgentControlParameter
    {
        public string id;
        public string valueType;
        public double numericValue;
    }

    [Serializable]
    public sealed class AgentPackageActionsFile
    {
        public List<AgentActionRequest> requests = new List<AgentActionRequest>();
        public List<AgentActionProfile> profiles = new List<AgentActionProfile>();
    }

    [Serializable]
    public sealed class AgentPackageSkillDefinitionFile
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
        public List<AgentPackageSkillSubgraphDependency> subgraphDependencies = new List<AgentPackageSkillSubgraphDependency>();
        public List<string> allowedFollowUpSkillIds = new List<string>();
    }

    [Serializable]
    public sealed class AgentPackageSkillSubgraphDependency
    {
        public string subgraphIdentity;
        public string callSiteIdentity;
    }

    [Serializable]
    public sealed class AgentPackageAssetReference
    {
        public string key;
        public string assetPath;
        public string assetGuid;
    }

    [Serializable]
    public sealed class AgentPackageCurvesFile
    {
        public string timelineId;
        public List<AgentPackageCurve> curves = new List<AgentPackageCurve>();
    }

    [Serializable]
    public sealed class AgentPackageCurve
    {
        public string clipId;
        public string channelId;
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
    public sealed class AgentPackageNodeCatalogFile
    {
        public List<AgentPackageNodeKindDescriptor> kinds = new List<AgentPackageNodeKindDescriptor>();
        public List<AgentPackageSkillNodeKindDescriptor> skillKinds = new List<AgentPackageSkillNodeKindDescriptor>();
    }

    [Serializable]
    public sealed class AgentPackageNodeKindDescriptor
    {
        public string kind;
        public List<string> graphKinds = new List<string>();
        public List<string> properties = new List<string>();
        public JObject defaults;
        public List<AgentPackagePortDescriptor> flowPorts = new List<AgentPackagePortDescriptor>();
        public List<AgentPackagePortDescriptor> propertyPorts = new List<AgentPackagePortDescriptor>();
        public List<AgentPackagePortVariantDescriptor> portVariants = new List<AgentPackagePortVariantDescriptor>();
        public bool canCreate;
        public bool canConfigure;
        public bool canDelete;
    }

    [Serializable]
    public sealed class AgentPackagePortDescriptor
    {
        public string key;
        public string direction;
        public string valueType;
        public string capacity;
        public bool required;
    }

    [Serializable]
    public sealed class AgentPackagePortVariantDescriptor
    {
        public string id;
        public AgentPackagePortVariantCondition when;
        public List<AgentPackagePortDescriptor> flowPorts = new List<AgentPackagePortDescriptor>();
        public List<AgentPackagePortDescriptor> propertyPorts = new List<AgentPackagePortDescriptor>();
    }

    [Serializable]
    public sealed class AgentPackagePortVariantCondition
    {
        public string field;
        public string valueKind;
        public string equals;
    }

    [Serializable]
    public sealed class AgentPackageGraphKindsFile
    {
        public List<AgentPackageGraphKindDescriptor> kinds = new List<AgentPackageGraphKindDescriptor>();
    }

    [Serializable]
    public sealed class AgentPackageGraphKindDescriptor
    {
        public string kind;
        public string ownerSlot;
        public List<string> nodeKinds = new List<string>();
        public List<AgentPackageAnchorDescriptor> anchors = new List<AgentPackageAnchorDescriptor>();
    }

    [Serializable]
    public sealed class AgentPackageAnchorDescriptor
    {
        public string anchor;
        public List<AgentPackagePortDescriptor> flowPorts = new List<AgentPackagePortDescriptor>();
        public List<AgentPackagePortDescriptor> propertyPorts = new List<AgentPackagePortDescriptor>();
    }

    [Serializable]
    public sealed class AgentPackageAssetCatalogFile
    {
        public List<AgentInputValue> inputValues = new List<AgentInputValue>();
        public List<AgentActionRequest> actionRequests = new List<AgentActionRequest>();
        public List<AgentDocumentBlendAssetContext> animationBlendCurves =
            new List<AgentDocumentBlendAssetContext>();
        public List<AgentDocumentBlendAssetContext> animationBlendProfiles =
            new List<AgentDocumentBlendAssetContext>();
        public List<AgentDocumentAnimationClipContext> animationClips =
            new List<AgentDocumentAnimationClipContext>();
    }

    [Serializable]
    public sealed class AgentPackageDependenciesFile
    {
        public string definitionName;
        public string definitionAssetPath;
        public AgentBodyMotionProfile bodyMotion;
        public AgentDocumentPresentationContext presentation;
        public AgentDocumentGeneratedProduct generatedProduct;
        public List<string> capabilities = new List<string>();
    }

    [Serializable]
    public sealed class AgentPackageDependency
    {
        public string id;
        public string ownerId;
        public string slot;
        public string ownership;
        public string mode;
    }

    public sealed class AgentAuthoringPackageProjection
    {
        public AgentAuthoringPackageProjection(
            AgentAuthoringTarget target,
            string sourceRevision,
            string editableHash,
            string contextHash,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            Target = target;
            SourceRevision = sourceRevision;
            EditableHash = editableHash;
            ContextHash = contextHash;
            InputProviderOwnerId = inputProviderOwnerId ?? string.Empty;
            GameplayProviderOwnerId = gameplayProviderOwnerId ?? string.Empty;
        }

        public AgentAuthoringTarget Target { get; }
        public string SourceRevision { get; }
        public string EditableHash { get; }
        public string ContextHash { get; }
        public string InputProviderOwnerId { get; }
        public string GameplayProviderOwnerId { get; }
    }

    public sealed class AgentAuthoringPackageState
    {
        public AgentAuthoringPackageState(
            string packagePath,
            AgentAuthoringTarget target,
            AgentAuthoringPackageSync sync,
            string editableHash,
            string contextHash,
            string documentHash,
            AgentDocumentSyncState syncState)
        {
            PackagePath = packagePath;
            Target = target;
            Sync = sync;
            EditableHash = editableHash;
            ContextHash = contextHash;
            DocumentHash = documentHash;
            SyncState = syncState;
        }

        public string PackagePath { get; }
        public AgentAuthoringTarget Target { get; }
        public AgentAuthoringPackageSync Sync { get; }
        public string EditableHash { get; }
        public string ContextHash { get; }
        public string DocumentHash { get; }
        public AgentDocumentSyncState SyncState { get; }
    }
}
