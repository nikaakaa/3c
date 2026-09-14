using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterLinkedPosePortValueBinding
    {
        [SerializeField] string m_PortId = string.Empty;
        [SerializeField] CharacterPosePortKind m_Kind;
        [SerializeField] int m_ValueIndex = -1;

        public PoseInterfacePortId PortId => string.IsNullOrWhiteSpace(m_PortId) ? default : new PoseInterfacePortId(m_PortId);
        public CharacterPosePortKind Kind => m_Kind;
        public int ValueIndex => m_ValueIndex;

        public CharacterLinkedPosePortValueBinding(
            PoseInterfacePortId portId,
            CharacterPosePortKind kind,
            int valueIndex)
        {
            if (!portId.IsValid || !Enum.IsDefined(typeof(CharacterPosePortKind), kind) || valueIndex < 0)
                throw new ArgumentException("Linked Pose port value binding is invalid.");
            m_PortId = portId.Value;
            m_Kind = kind;
            m_ValueIndex = valueIndex;
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseEntryFragmentPlanDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_InterfaceSignature = string.Empty;
        [SerializeField] string m_ImplementationId = string.Empty;
        [SerializeField] ulong m_ImplementationRevision;
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_GraphRevision = string.Empty;
        [SerializeField] int m_OperationStart;
        [SerializeField] int m_OperationCount;
        [SerializeField] int m_PoseValueStart;
        [SerializeField] int m_PoseValueCount;
        [SerializeField] int m_GoalSetValueStart;
        [SerializeField] int m_GoalSetValueCount;
        [SerializeField] int m_PlayerStart;
        [SerializeField] int m_PlayerCount;
        [SerializeField] int m_StateMachineStart;
        [SerializeField] int m_StateMachineCount;
        [SerializeField] int m_InertializationStart;
        [SerializeField] int m_InertializationCount;
        [SerializeField] int m_RootOrientationWarpStart;
        [SerializeField] int m_RootOrientationWarpCount;
        [SerializeField] int m_MotionMatchingProviderStart;
        [SerializeField] int m_MotionMatchingProviderCount;
        [SerializeField] int m_StageStart = -1;
        [SerializeField] int m_StageCount;
        [SerializeField] CharacterLinkedPosePortValueBinding[] m_Inputs = Array.Empty<CharacterLinkedPosePortValueBinding>();
        [SerializeField] CharacterLinkedPosePortValueBinding[] m_Outputs = Array.Empty<CharacterLinkedPosePortValueBinding>();
        [SerializeField] int[] m_SourceIndices = Array.Empty<int>();

        public int Index => m_Index;
        public LinkedPoseGroupId GroupId => new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => new LinkedPoseInterfaceId(m_InterfaceId);
        public StableHash InterfaceSignature => new StableHash(m_InterfaceSignature);
        public LinkedPoseImplementationId ImplementationId => new LinkedPoseImplementationId(m_ImplementationId);
        public LinkedPoseRevision ImplementationRevision => new LinkedPoseRevision(m_ImplementationRevision);
        public LinkedPoseEntryId EntryId => new LinkedPoseEntryId(m_EntryId);
        public PoseGraphId GraphId => new PoseGraphId(m_GraphId);
        public string GraphRevision => m_GraphRevision ?? string.Empty;
        public int OperationStart => m_OperationStart;
        public int OperationCount => m_OperationCount;
        public int PoseValueStart => m_PoseValueStart;
        public int PoseValueCount => m_PoseValueCount;
        public int GoalSetValueStart => m_GoalSetValueStart;
        public int GoalSetValueCount => m_GoalSetValueCount;
        public int PlayerStart => m_PlayerStart;
        public int PlayerCount => m_PlayerCount;
        public int StateMachineStart => m_StateMachineStart;
        public int StateMachineCount => m_StateMachineCount;
        public int InertializationStart => m_InertializationStart;
        public int InertializationCount => m_InertializationCount;
        public int RootOrientationWarpStart => m_RootOrientationWarpStart;
        public int RootOrientationWarpCount => m_RootOrientationWarpCount;
        public int MotionMatchingProviderStart => m_MotionMatchingProviderStart;
        public int MotionMatchingProviderCount => m_MotionMatchingProviderCount;
        public int StageStart => m_StageStart;
        public int StageCount => m_StageCount;
        public int FrameCompletionStart => m_OperationStart;
        public int FrameCompletionCount => m_OperationCount;
        public int PlayerCompletionStart => m_PlayerStart;
        public int PlayerCompletionCount => m_PlayerCount;
        public int StageCompletionStart => m_StageStart;
        public int StageCompletionCount => m_StageCount;
        public int OperationDiagnosticStart => m_OperationStart;
        public int OperationDiagnosticCount => m_OperationCount;
        public int StageDiagnosticStart => m_StageStart;
        public int StageDiagnosticCount => m_StageCount;
        public IReadOnlyList<CharacterLinkedPosePortValueBinding> Inputs => m_Inputs ?? Array.Empty<CharacterLinkedPosePortValueBinding>();
        public IReadOnlyList<CharacterLinkedPosePortValueBinding> Outputs => m_Outputs ?? Array.Empty<CharacterLinkedPosePortValueBinding>();
        public IReadOnlyList<int> SourceIndices => m_SourceIndices ?? Array.Empty<int>();

        public CharacterLinkedPoseEntryFragmentPlanDescriptor(
            int index,
            LinkedPoseGroupId groupId,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            CharacterLinkedPoseImplementationAsset implementation,
            LinkedPoseEntryId entryId,
            CharacterPoseCanvasGraph graph,
            int operationStart,
            int operationCount,
            int poseValueStart,
            int poseValueCount,
            int goalSetValueStart,
            int goalSetValueCount,
            int playerStart,
            int playerCount,
            int stateMachineStart,
            int stateMachineCount,
            int inertializationStart,
            int inertializationCount,
            int rootOrientationWarpStart,
            int rootOrientationWarpCount,
            int motionMatchingProviderStart,
            int motionMatchingProviderCount,
            CharacterLinkedPosePortValueBinding[] inputs,
            CharacterLinkedPosePortValueBinding[] outputs,
            int[] sourceIndices)
        {
            linkedInterface?.RequireValid();
            implementation?.RequireValid();
            if (index < 0 || !groupId.IsValid || !linkedInterface || !implementation ||
                implementation.Interface != linkedInterface || !entryId.IsValid || graph == null ||
                operationStart < 0 || operationCount < 0 || poseValueStart < 0 || poseValueCount < 0 ||
                goalSetValueStart < 0 || goalSetValueCount < 0 || playerStart < 0 || playerCount < 0 ||
                stateMachineStart < 0 || stateMachineCount < 0 || inertializationStart < 0 || inertializationCount < 0 ||
                rootOrientationWarpStart < 0 || rootOrientationWarpCount < 0 ||
                motionMatchingProviderStart < 0 || motionMatchingProviderCount < 0)
            {
                throw new ArgumentException("Linked Pose Entry fragment plan descriptor is invalid.");
            }
            m_Index = index;
            m_GroupId = groupId.Value;
            m_InterfaceId = linkedInterface.InterfaceId.Value;
            m_InterfaceSignature = linkedInterface.SignatureHash.ToString();
            m_ImplementationId = implementation.ImplementationId.Value;
            m_ImplementationRevision = implementation.Revision.Value;
            m_EntryId = entryId.Value;
            m_GraphId = graph.GraphId.Value;
            m_GraphRevision = graph.ContentRevision;
            m_OperationStart = operationStart;
            m_OperationCount = operationCount;
            m_PoseValueStart = poseValueStart;
            m_PoseValueCount = poseValueCount;
            m_GoalSetValueStart = goalSetValueStart;
            m_GoalSetValueCount = goalSetValueCount;
            m_PlayerStart = playerStart;
            m_PlayerCount = playerCount;
            m_StateMachineStart = stateMachineStart;
            m_StateMachineCount = stateMachineCount;
            m_InertializationStart = inertializationStart;
            m_InertializationCount = inertializationCount;
            m_RootOrientationWarpStart = rootOrientationWarpStart;
            m_RootOrientationWarpCount = rootOrientationWarpCount;
            m_MotionMatchingProviderStart = motionMatchingProviderStart;
            m_MotionMatchingProviderCount = motionMatchingProviderCount;
            m_Inputs = inputs ?? Array.Empty<CharacterLinkedPosePortValueBinding>();
            m_Outputs = outputs ?? Array.Empty<CharacterLinkedPosePortValueBinding>();
            m_SourceIndices = sourceIndices?.Distinct().OrderBy(value => value).ToArray() ?? Array.Empty<int>();
        }

        internal void BindStageRange(int stageStart, int stageCount)
        {
            if (m_StageStart >= 0 || stageStart < 0 || stageCount < 0)
                throw new InvalidOperationException($"Linked Pose fragment #{Index} stage range cannot be rebound.");
            m_StageStart = stageStart;
            m_StageCount = stageCount;
        }
    }

    [Serializable]
    public sealed class CharacterLinkedPoseCallPlanDescriptor
    {
        [SerializeField] int m_Index = -1;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_InterfaceId = string.Empty;
        [SerializeField] string m_InterfaceSignature = string.Empty;
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] int[] m_FragmentIndices = Array.Empty<int>();

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public LinkedPoseGroupId GroupId => new LinkedPoseGroupId(m_GroupId);
        public LinkedPoseInterfaceId InterfaceId => new LinkedPoseInterfaceId(m_InterfaceId);
        public StableHash InterfaceSignature => new StableHash(m_InterfaceSignature);
        public LinkedPoseEntryId EntryId => new LinkedPoseEntryId(m_EntryId);
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public IReadOnlyList<int> FragmentIndices => m_FragmentIndices ?? Array.Empty<int>();

        public CharacterLinkedPoseCallPlanDescriptor(
            int index,
            PoseNodeId nodeId,
            LinkedPoseGroupId groupId,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            LinkedPoseEntryId entryId,
            CharacterPoseExecutionDomain executionDomain,
            int[] fragmentIndices)
        {
            linkedInterface?.RequireValid();
            if (index < 0 || !nodeId.IsValid || !groupId.IsValid || !linkedInterface || !entryId.IsValid ||
                !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) || fragmentIndices == null || fragmentIndices.Length == 0)
            {
                throw new ArgumentException("Linked Pose Call plan descriptor is invalid.");
            }
            linkedInterface.RequireEntry(entryId);
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_GroupId = groupId.Value;
            m_InterfaceId = linkedInterface.InterfaceId.Value;
            m_InterfaceSignature = linkedInterface.SignatureHash.ToString();
            m_EntryId = entryId.Value;
            m_ExecutionDomain = executionDomain;
            m_FragmentIndices = (int[])fragmentIndices.Clone();
        }
    }

}
