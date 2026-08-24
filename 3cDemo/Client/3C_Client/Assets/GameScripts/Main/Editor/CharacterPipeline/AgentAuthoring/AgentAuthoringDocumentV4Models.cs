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
        public int blackboardSchemaRevision;
        public List<AgentSnapshotGraph> graphs = new List<AgentSnapshotGraph>();
        public List<AgentSnapshotStateMachineSummary> stateMachines = new List<AgentSnapshotStateMachineSummary>();
        public List<AgentSnapshotBlackboardDeclaration> blackboardDeclarations = new List<AgentSnapshotBlackboardDeclaration>();
        public List<AgentSnapshotTimeline> timelines = new List<AgentSnapshotTimeline>();
        public List<AgentSnapshotTimelineTreeClip> timelineTreeClips = new List<AgentSnapshotTimelineTreeClip>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public List<AgentSnapshotActionProfile> actionProfiles = new List<AgentSnapshotActionProfile>();
        public AgentDocumentPresentationEditable presentation;
        public AgentDocumentAIEditable aiController;
    }

    [Serializable]
    public sealed class AgentDocumentAIEditable
    {
        public string controllerId;
        public string definitionAssetPath;
        public string definitionAssetGuid;
        public string treeAssetPath;
        public string treeAssetGuid;
        public string graphAuthoringId;
        public string authoringRole;
        public string perceptionAssetPath;
        public string perceptionAssetGuid;
        public string candidateOrdering;
        public List<string> candidateActorIds = new List<string>();
        public string controlledCharacterAssetPath;
        public string controlledCharacterAssetGuid;
        public List<AgentSnapshotAIBlackboardDeclaration> blackboardDeclarations = new List<AgentSnapshotAIBlackboardDeclaration>();
        public List<AgentSnapshotAINode> nodes = new List<AgentSnapshotAINode>();
    }

    [Serializable]
    public sealed class AgentDocumentContext
    {
        public string definitionName;
        public string definitionAssetPath;
        public string rootTreeAssetPath;
        public string rootGraphAuthoringId;
        public List<AgentSnapshotInputValue> inputValues = new List<AgentSnapshotInputValue>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public AgentSnapshotBodyMotionProfile bodyMotion = new AgentSnapshotBodyMotionProfile();
        public AgentDocumentPresentationContext presentation =
            new AgentDocumentPresentationContext();
        public List<AgentSnapshotAsset> timelineAssets = new List<AgentSnapshotAsset>();
        public List<AgentSnapshotAsset> actionContextAssets = new List<AgentSnapshotAsset>();
        public AgentDocumentGeneratedProduct generatedProduct = new AgentDocumentGeneratedProduct();
        public AgentDocumentAIContext aiController;
        public List<string> capabilities = new List<string>();
    }

    [Serializable]
    public sealed class AgentDocumentPresentationContext
    {
        public AgentPackageAssetReferenceV4 rig;
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
        public List<AgentSnapshotStateLocalPoseSource> stateLocalPoseSources =
            new List<AgentSnapshotStateLocalPoseSource>();
        public List<AgentSnapshotActionPlaybackInput> actionPlaybackInputs =
            new List<AgentSnapshotActionPlaybackInput>();
        public List<AgentSnapshotAnimationSlot> animationSlots =
            new List<AgentSnapshotAnimationSlot>();
        public List<AgentSnapshotAnimationProducer> producers =
            new List<AgentSnapshotAnimationProducer>();
        public List<AgentSnapshotAnimationBlendSpace> blendSpaces =
            new List<AgentSnapshotAnimationBlendSpace>();
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
        public AgentPackageAssetReferenceV4 clip;
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
    public sealed class AgentDocumentAIContext
    {
        public string characterProgramId;
        public string characterProgramHash;
        public bool characterProgramStale;
        public string intentProgramAssetPath;
        public string intentProgramAssetGuid;
        public string intentProgramId;
        public string intentProgramHash;
        public string intentProgramSourceRevision;
        public bool intentProgramStale;
    }

    [Serializable]
    public sealed class AgentPackageControllerFile
    {
        public List<AgentSnapshotStateMachineSummary> stateMachines = new List<AgentSnapshotStateMachineSummary>();
        public List<AgentSnapshotTimelineTreeClip> timelineTreeClips = new List<AgentSnapshotTimelineTreeClip>();
    }

    [Serializable]
    public sealed class AgentPackageBlackboardFile
    {
        public int schemaRevision;
        public List<AgentSnapshotBlackboardDeclaration> declarations = new List<AgentSnapshotBlackboardDeclaration>();
    }

    [Serializable]
    public sealed class AgentPackageActionsFile
    {
        public List<AgentSnapshotActionRequest> requests = new List<AgentSnapshotActionRequest>();
        public List<AgentSnapshotActionProfile> profiles = new List<AgentSnapshotActionProfile>();
    }

    [Serializable]
    public sealed class AgentPackageAIFile
    {
        public int blackboardSchemaRevision;
        public AgentPackageAIController controller;
    }

    [Serializable]
    public sealed class AgentPackageAIController
    {
        public string controllerId;
        public string definitionAssetPath;
        public string definitionAssetGuid;
        public string treeAssetPath;
        public string treeAssetGuid;
        public string graphAuthoringId;
        public string authoringRole;
        public string perceptionAssetPath;
        public string perceptionAssetGuid;
        public string candidateOrdering;
        public List<string> candidateActorIds = new List<string>();
        public string controlledCharacterAssetPath;
        public string controlledCharacterAssetGuid;
        public List<AgentPackageAIBlackboardDeclaration> blackboard = new List<AgentPackageAIBlackboardDeclaration>();
        public List<AgentPackageAINodeConfiguration> nodes = new List<AgentPackageAINodeConfiguration>();
    }

    [Serializable]
    public sealed class AgentPackageAIBlackboardDeclaration
    {
        public string id;
        public string key;
        public string valueType;
        public string scope;
        public string defaultValue;
    }

    [Serializable]
    public sealed class AgentPackageAINodeConfiguration
    {
        public string id;
        public string memoryValueKind;
        public string memoryDeclarationId;
        public string inputId;
        public string requestId;
        public float requestBufferSeconds;
        public int requestPriority;
        public string requestRepeatPolicy;
    }

    [Serializable]
    public sealed class AgentPackageGraphFile
    {
        public string id;
        public string kind;
        public string ownership;
        public AgentPackageGraphOwner owner;
        public string sharedAssetPath;
        public List<AgentPackageNode> nodes = new List<AgentPackageNode>();
        public List<AgentPackageFlowEdge> flowEdges = new List<AgentPackageFlowEdge>();
        public List<AgentPackagePropertyEdge> propertyEdges = new List<AgentPackagePropertyEdge>();
    }

    [Serializable]
    public sealed class AgentPackageGraphOwner
    {
        public string entityId;
        public string slot;
    }

    [Serializable]
    public sealed class AgentPackageNode
    {
        public string id;
        public string kind;
        public string name;
        public JObject properties;
    }

    [Serializable]
    public sealed class AgentPackageGraphReference
    {
        public string key;
        public string graphId;
        public string ownership;
        public string sharedAssetPath;
    }

    [Serializable]
    public sealed class AgentPackageAssetReference
    {
        public string key;
        public string assetPath;
        public string assetGuid;
    }

    [Serializable]
    public sealed class AgentPackageExposedProperty
    {
        public string mode;
        public string declarationId;
        public string valueType;
        public JToken value;
    }

    [Serializable]
    public sealed class AgentPackageEdgeEndpoint
    {
        public string node;
        public string port;
    }

    [Serializable]
    public sealed class AgentPackageFlowEdge
    {
        public string id;
        public AgentPackageEdgeEndpoint from;
        public AgentPackageEdgeEndpoint to;
        public int flowOrder;
        public int transitionPriority;
        public string abortPolicy;
        public string conditionGraph;
    }

    [Serializable]
    public sealed class AgentPackagePropertyEdge
    {
        public string id;
        public AgentPackageEdgeEndpoint from;
        public AgentPackageEdgeEndpoint to;
    }

    [Serializable]
    public sealed class AgentPackageLayoutFile
    {
        public string graphId;
        public List<AgentPackageNodeLayout> nodes = new List<AgentPackageNodeLayout>();
    }

    [Serializable]
    public sealed class AgentPackageNodeLayout
    {
        public string id;
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class AgentPackageTimelineFile
    {
        public string id;
        public string name;
        public List<AgentSnapshotTimelineCallSite> callSites = new List<AgentSnapshotTimelineCallSite>();
        public List<AgentSnapshotTimelineSection> sections = new List<AgentSnapshotTimelineSection>();
        public List<AgentSnapshotTimelineTrack> tracks = new List<AgentSnapshotTimelineTrack>();
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
        public List<AgentSnapshotInputValue> inputValues = new List<AgentSnapshotInputValue>();
        public List<AgentSnapshotActionRequest> actionRequests = new List<AgentSnapshotActionRequest>();
        public List<AgentSnapshotBlackboardDeclaration> blackboardDeclarations = new List<AgentSnapshotBlackboardDeclaration>();
        public List<AgentSnapshotAIBlackboardDeclaration> aiBlackboardDeclarations = new List<AgentSnapshotAIBlackboardDeclaration>();
        public List<AgentSnapshotAsset> timelineAssets = new List<AgentSnapshotAsset>();
        public List<AgentSnapshotAsset> actionContextAssets = new List<AgentSnapshotAsset>();
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
        public string rootTreeAssetPath;
        public string rootGraphAuthoringId;
        public AgentSnapshotBodyMotionProfile bodyMotion;
        public AgentDocumentPresentationContext presentation;
        public AgentDocumentGeneratedProduct generatedProduct;
        public AgentDocumentAIContext aiController;
        public List<string> capabilities = new List<string>();
        public List<AgentPackageDependency> graphDependencies = new List<AgentPackageDependency>();
        public List<AgentPackageDependency> timelineDependencies = new List<AgentPackageDependency>();
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
            AgentGraphSnapshot snapshot,
            AgentAuthoringTarget target,
            string sourceRevision,
            string editableHash,
            string contextHash)
        {
            Snapshot = snapshot;
            Target = target;
            SourceRevision = sourceRevision;
            EditableHash = editableHash;
            ContextHash = contextHash;
        }

        public AgentGraphSnapshot Snapshot { get; }
        public AgentAuthoringTarget Target { get; }
        public string SourceRevision { get; }
        public string EditableHash { get; }
        public string ContextHash { get; }
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
