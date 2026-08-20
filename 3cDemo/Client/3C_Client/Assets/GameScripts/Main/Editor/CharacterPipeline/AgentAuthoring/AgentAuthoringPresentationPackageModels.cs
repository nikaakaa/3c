using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    [Serializable]
    public sealed class AgentPackageAssetReferenceV4
    {
        public string assetPath;
        public string assetGuid;
        public long localFileId;
        public string localId;
    }

    [Serializable]
    public sealed class AgentDocumentPresentationEditable
    {
        public AgentPackagePresentationProfileFile profile;
        public List<AgentPackageAnimationClipCurvesFile> animationClips =
            new List<AgentPackageAnimationClipCurvesFile>();
        public List<AgentPackagePoseGraphFile> poseGraphs =
            new List<AgentPackagePoseGraphFile>();
        public List<AgentPackagePoseGraphLayoutFile> poseGraphLayouts =
            new List<AgentPackagePoseGraphLayoutFile>();
        public List<AgentPackagePoseStateMachineFile> poseStateMachines =
            new List<AgentPackagePoseStateMachineFile>();
        public List<AgentPackagePoseStateMachineLayoutFile>
            poseStateMachineLayouts =
                new List<AgentPackagePoseStateMachineLayoutFile>();
        public List<AgentPackageLinkedPoseImplementationFile>
            linkedPoseImplementations =
                new List<AgentPackageLinkedPoseImplementationFile>();
    }

    [Serializable]
    public sealed class AgentPackagePresentationProfileFile
    {
        public string id;
        public AgentPackageAssetReferenceV4 owner;
        public AgentPackageAssetReferenceV4 poseGraph;
        public AgentPackageAssetReferenceV4 rig;
        public AgentPackagePresentationPolicy policy;
        public List<AgentPackagePoseSourceBinding> poseSources =
            new List<AgentPackagePoseSourceBinding>();
        public List<AgentPackageAnimationProducerBinding> actionProducers =
            new List<AgentPackageAnimationProducerBinding>();
        public List<AgentPackageLocomotionSyncGroup> locomotionSyncGroups =
            new List<AgentPackageLocomotionSyncGroup>();
        public List<AgentPackageLinkedPoseGroupBinding> linkedPoseGroups =
            new List<AgentPackageLinkedPoseGroupBinding>();
        public List<AgentPackageLinkedPoseSelectorBinding> linkedPoseSelectors =
            new List<AgentPackageLinkedPoseSelectorBinding>();
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseInterfaceFile
    {
        public string id;
        public AgentPackageAssetReferenceV4 asset;
        public string ownerIdentity;
        public string interfaceId;
        public ulong revision;
        public string signatureHash;
        public string factContractIdentity;
        public string executionContract;
        public List<AgentPackageLinkedPoseInterfaceEntry> entries =
            new List<AgentPackageLinkedPoseInterfaceEntry>();
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseInterfaceEntry
    {
        public string entryId;
        public string executionDomain;
        public List<AgentPackageLinkedPoseInterfacePort> ports =
            new List<AgentPackageLinkedPoseInterfacePort>();
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseInterfacePort
    {
        public string portId;
        public string direction;
        public string kind;
        public string space;
        public bool required;
        public int order;
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseImplementationFile
    {
        public string id;
        public string name;
        public AgentPackageAssetReferenceV4 asset;
        public string ownerIdentity;
        public string implementationId;
        public ulong revision;
        public AgentPackageAssetReferenceV4 interfaceAsset;
        public AgentPackageAssetReferenceV4 graphOwner;
        public string graphOwnerIdentity;
        public List<AgentPackageLinkedPoseImplementationEntry> entries =
            new List<AgentPackageLinkedPoseImplementationEntry>();
        public List<AgentPackagePoseGraphFile> poseGraphs =
            new List<AgentPackagePoseGraphFile>();
        public List<AgentPackagePoseGraphLayoutFile> poseGraphLayouts =
            new List<AgentPackagePoseGraphLayoutFile>();
        public List<AgentPackagePoseStateMachineFile> poseStateMachines =
            new List<AgentPackagePoseStateMachineFile>();
        public List<AgentPackagePoseStateMachineLayoutFile>
            poseStateMachineLayouts =
                new List<AgentPackagePoseStateMachineLayoutFile>();
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseImplementationEntry
    {
        public string entryId;
        public string graphId;
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseGroupBinding
    {
        public string id;
        public string groupId;
        public AgentPackageAssetReferenceV4 interfaceAsset;
    }

    [Serializable]
    public sealed class AgentPackageLinkedPoseSelectorBinding
    {
        public string id;
        public string kind;
        public AgentPackageAssetReferenceV4 asset;
        public string selectorId;
        public string groupId;
        public AgentPackageEquipmentLinkedPoseSelectorPayload equipment;
    }

    [Serializable]
    public sealed class AgentPackageEquipmentLinkedPoseSelectorPayload
    {
        public string slotId;
        public string emptyImplementationId;
        public List<AgentPackageEquipmentLinkedPoseMapping> mappings =
            new List<AgentPackageEquipmentLinkedPoseMapping>();
    }

    [Serializable]
    public sealed class AgentPackageEquipmentLinkedPoseMapping
    {
        public string id;
        public string equipmentId;
        public string implementationId;
    }

    [Serializable]
    public sealed class AgentPackagePresentationPolicy
    {
        public AgentPackageAssetReferenceV4 motionMatchingProfile;
        public string footPlacementAnalysisMode;
        public string footPlacementAnalysisSourceAssetGuid;
    }

    [Serializable]
    public sealed class AgentPackagePoseSourceBinding
    {
        public string name;
        public string kind;
        public AgentPackageAssetReferenceV4 slot;
        public AgentPackageAssetReferenceV4 binding;
        public AgentPackageAssetReferenceV4 source;
        public string searchDomainId;
        public List<AgentPackageAssetReferenceV4> databases =
            new List<AgentPackageAssetReferenceV4>();
        public string footAnalysisIdentity;
        public string contentRevision;
    }

    [Serializable]
    public sealed class AgentPackageAnimationClipCurvesFile
    {
        public string id;
        public AgentPackageAssetReferenceV4 clip;
        public string dependencyBaseline;
        public string analysisInputHash;
        public List<AgentPackageCurve> curves = new List<AgentPackageCurve>();
    }

    [Serializable]
    public sealed class AgentPackageLocomotionSyncGroup
    {
        public string groupId;
        public List<AgentPackageAssetReferenceV4> members =
            new List<AgentPackageAssetReferenceV4>();
    }

    [Serializable]
    public sealed class AgentPackageAnimationProducerBinding
    {
        public string timelineId;
        public string trackId;
        public AgentPackageAssetReferenceV4 source;
    }

    [Serializable]
    public sealed class AgentPackagePoseGraphFile
    {
        public string id;
        public string role;
        public string contentRevision;
        public List<AgentPackagePoseParameter> parameters =
            new List<AgentPackagePoseParameter>();
        public List<AgentPackagePoseNode> nodes =
            new List<AgentPackagePoseNode>();
        public List<AgentPackagePoseEdge> edges =
            new List<AgentPackagePoseEdge>();
    }

    [Serializable]
    public sealed class AgentPackagePoseParameter
    {
        public string id;
        public string valueType;
        public string unit;
        public float defaultValue;
    }

    [Serializable]
    public sealed class AgentPackagePoseNode
    {
        public string id;
        public string capability;
        public string name;
        public JObject properties = new JObject();
        public List<AgentPackagePoseDynamicPort> dynamicPorts =
            new List<AgentPackagePoseDynamicPort>();
        public string childDocumentId;
    }

    [Serializable]
    public sealed class AgentPackageAnimationSlotBinding
    {
        public string slotId;
        public string animationChannelId;
    }

    [Serializable]
    public sealed class AgentPackagePoseDynamicPort
    {
        public string id;
        public string name;
        public string valueType;
        public string direction;
        public bool required;
        public int order;
        public string interfacePortId;
    }

    [Serializable]
    public sealed class AgentPackagePoseEndpoint
    {
        public string node;
        public string port;
    }

    [Serializable]
    public sealed class AgentPackagePoseEdge
    {
        public string id;
        public AgentPackagePoseEndpoint from;
        public AgentPackagePoseEndpoint to;
    }

    [Serializable]
    public sealed class AgentPackagePoseGraphLayoutFile
    {
        public string graphId;
        public List<AgentPackagePoseNodeLayout> nodes =
            new List<AgentPackagePoseNodeLayout>();
    }

    [Serializable]
    public sealed class AgentPackagePoseNodeLayout
    {
        public string id;
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class AgentPackagePoseStateMachineFile
    {
        public string id;
        public string contentRevision;
        public AgentPackagePoseStateEntry entry;
        public int maxTransitionsPerFrame;
        public List<AgentPackagePoseState> states =
            new List<AgentPackagePoseState>();
        public List<AgentPackagePoseStateAlias> aliases =
            new List<AgentPackagePoseStateAlias>();
        public List<AgentPackagePoseTransition> transitions =
            new List<AgentPackagePoseTransition>();
    }

    [Serializable]
    public sealed class AgentPackagePoseStateMachineLayoutFile
    {
        public string stateMachineId;
        public List<AgentPackagePoseStateMachineLayoutElement> elements =
            new List<AgentPackagePoseStateMachineLayoutElement>();
    }

    [Serializable]
    public sealed class AgentPackagePoseStateMachineLayoutElement
    {
        public string id;
        public float x;
        public float y;
    }

    [Serializable]
    public sealed class AgentPackagePoseStateEntry
    {
        public string id;
        public string targetStateId;
    }

    [Serializable]
    public sealed class AgentPackagePoseState
    {
        public string id;
        public string name;
        public string poseGraphId;
        public string outputPoseNodeId;
        public bool? alwaysResetOnEntry;
    }

    [Serializable]
    public sealed class AgentPackagePoseStateAlias
    {
        public string id;
        public string name;
        public List<AgentPackagePoseTransitionSource> sources =
            new List<AgentPackagePoseTransitionSource>();
    }

    [Serializable]
    public sealed class AgentPackagePoseTransitionSource
    {
        public string kind;
        public string stateId;
        public string aliasId;
    }

    [Serializable]
    public sealed class AgentPackagePoseTransition
    {
        public string id;
        public AgentPackagePoseTransitionSource source;
        public string targetStateId;
        public int priority;
        public AgentPackagePoseTransitionRule rule;
        public string blendLogic;
        public float durationSeconds;
        public string blendMode;
        public string customBlendCurveAssetId;
        public string blendProfileAssetId;
    }

    [Serializable]
    public sealed class AgentPackagePoseTransitionRule
    {
        public string id;
        public string contentRevision;
        public List<AgentPackagePoseTransitionRuleOperation> operations =
            new List<AgentPackagePoseTransitionRuleOperation>();
        public string outputOperationId;
    }

    [Serializable]
    public sealed class AgentPackagePoseTransitionRuleOperation
    {
        public string id;
        public string kind;
        public string inputA;
        public string inputB;
        public string factId;
        public bool boolLiteral;
        public float floatLiteral;
        public string enumTypeId;
        public int enumLiteral;
        public string identityLiteral;
    }
}
