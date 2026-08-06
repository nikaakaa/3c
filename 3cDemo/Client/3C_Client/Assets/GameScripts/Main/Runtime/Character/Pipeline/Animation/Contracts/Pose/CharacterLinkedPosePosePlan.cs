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
            CharacterTypedPoseGraph graph,
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

    public sealed partial class CharacterPresentationPosePlan
    {
        [SerializeField] CharacterLinkedPoseEntryFragmentPlanDescriptor[] m_LinkedPoseFragments = Array.Empty<CharacterLinkedPoseEntryFragmentPlanDescriptor>();
        [SerializeField] CharacterLinkedPoseCallPlanDescriptor[] m_LinkedPoseCalls = Array.Empty<CharacterLinkedPoseCallPlanDescriptor>();

        public IReadOnlyList<CharacterLinkedPoseEntryFragmentPlanDescriptor> LinkedPoseFragments => m_LinkedPoseFragments ?? Array.Empty<CharacterLinkedPoseEntryFragmentPlanDescriptor>();
        public IReadOnlyList<CharacterLinkedPoseCallPlanDescriptor> LinkedPoseCalls => m_LinkedPoseCalls ?? Array.Empty<CharacterLinkedPoseCallPlanDescriptor>();

        void RequireLinkedPoseValid()
        {
            if (LinkedPoseCalls.Count == 0 && LinkedPoseFragments.Count == 0)
            {
                if (Operations.Any(value => value.Code == CharacterPoseOperationCode.LinkedPoseCall || value.LinkedPoseFragmentIndex >= 0))
                    throw new InvalidOperationException("Pose Plan has Linked Pose operations without compiled descriptors.");
                return;
            }
            for (int fragmentIndex = 0; fragmentIndex < LinkedPoseFragments.Count; fragmentIndex++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = LinkedPoseFragments[fragmentIndex];
                if (fragment == null || fragment.Index != fragmentIndex || fragment.OperationCount < 0 ||
                    fragment.OperationStart < 0 || fragment.OperationStart + fragment.OperationCount > Operations.Count ||
                    fragment.StageStart < 0 || fragment.StageCount < 0 ||
                    fragment.StageStart + fragment.StageCount > Stages.Count ||
                    fragment.RootOrientationWarpStart < 0 || fragment.RootOrientationWarpCount < 0 ||
                    fragment.RootOrientationWarpStart + fragment.RootOrientationWarpCount > RootOrientationWarps.Count ||
                    fragment.MotionMatchingProviderStart < 0 || fragment.MotionMatchingProviderCount < 0)
                {
                    throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} is invalid.");
                }
                for (int operationIndex = fragment.OperationStart; operationIndex < fragment.OperationStart + fragment.OperationCount; operationIndex++)
                {
                    if (Operations[operationIndex].LinkedPoseFragmentIndex != fragmentIndex)
                        throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} operation range is not isolated.");
                }
                if ((fragment.OperationCount == 0) != (fragment.StageCount == 0) ||
                    fragment.StageCount > 0 &&
                    (Stages[fragment.StageStart].OperationStart != fragment.OperationStart ||
                     Stages[fragment.StageStart + fragment.StageCount - 1].OperationStart +
                     Stages[fragment.StageStart + fragment.StageCount - 1].OperationCount !=
                     fragment.OperationStart + fragment.OperationCount))
                {
                    throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} stage coverage is incomplete.");
                }
                for (int stageIndex = fragment.StageStart; stageIndex < fragment.StageStart + fragment.StageCount; stageIndex++)
                {
                    CharacterPresentationPoseStage stage = Stages[stageIndex];
                    if (stage.OperationStart < fragment.OperationStart ||
                        stage.OperationStart + stage.OperationCount > fragment.OperationStart + fragment.OperationCount)
                    {
                        throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} stage range is not isolated.");
                    }
                }
                int rootOrientationWarpCount = 0;
                for (int operationIndex = fragment.OperationStart; operationIndex < fragment.OperationStart + fragment.OperationCount; operationIndex++)
                {
                    CharacterPresentationPoseOperation operation = Operations[operationIndex];
                    if (operation.Code != CharacterPoseOperationCode.RootOrientationWarp)
                        continue;
                    if (operation.RootOrientationWarpIndex != fragment.RootOrientationWarpStart + rootOrientationWarpCount)
                        throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} Root Orientation Warp range is not contiguous.");
                    rootOrientationWarpCount++;
                }
                if (rootOrientationWarpCount != fragment.RootOrientationWarpCount)
                    throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} Root Orientation Warp count is stale.");
                int expectedMotionMatchingStart = CountMotionMatchingProviders(0, fragment.StateMachineStart);
                int motionMatchingProviderCount = CountMotionMatchingProviders(
                    fragment.StateMachineStart,
                    fragment.StateMachineCount);
                if (fragment.MotionMatchingProviderStart != expectedMotionMatchingStart ||
                    fragment.MotionMatchingProviderCount != motionMatchingProviderCount)
                {
                    throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} Motion Matching provider range is stale.");
                }
            }
            var callNodes = new HashSet<PoseNodeId>();
            var ownedFragments = new HashSet<int>();
            for (int callIndex = 0; callIndex < LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = LinkedPoseCalls[callIndex];
                if (call == null || call.Index != callIndex || !callNodes.Add(call.NodeId))
                    throw new InvalidOperationException($"Linked Pose Call plan descriptor #{callIndex} is invalid.");
                CharacterPresentationPoseOperation operation = Operations.SingleOrDefault(value => value.Code == CharacterPoseOperationCode.LinkedPoseCall && value.LinkedPoseCallIndex == callIndex);
                if (operation == null || operation.NodeId != call.NodeId || operation.LinkedPoseFragmentIndex >= 0 || operation.ExecutionDomain != call.ExecutionDomain)
                    throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' operation ownership is invalid.");
                var implementations = new HashSet<LinkedPoseImplementationId>();
                CharacterLinkedPosePortValueBinding[] expectedInputs = null;
                CharacterLinkedPosePortValueBinding[] expectedOutputs = null;
                for (int fragmentOffset = 0; fragmentOffset < call.FragmentIndices.Count; fragmentOffset++)
                {
                    int fragmentIndex = call.FragmentIndices[fragmentOffset];
                    if ((uint)fragmentIndex >= (uint)LinkedPoseFragments.Count || !ownedFragments.Add(fragmentIndex))
                        throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' references fragment outside the plan.");
                    CharacterLinkedPoseEntryFragmentPlanDescriptor fragment = LinkedPoseFragments[fragmentIndex];
                    if (fragment.GroupId != call.GroupId || fragment.InterfaceId != call.InterfaceId || fragment.InterfaceSignature != call.InterfaceSignature ||
                        fragment.EntryId != call.EntryId || !implementations.Add(fragment.ImplementationId))
                    {
                        throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' fragment closure is inconsistent.");
                    }
                    CharacterLinkedPosePortValueBinding[] inputs = fragment.Inputs.OrderBy(value => value.PortId).ToArray();
                    CharacterLinkedPosePortValueBinding[] outputs = fragment.Outputs.OrderBy(value => value.PortId).ToArray();
                    if (expectedInputs == null)
                    {
                        expectedInputs = inputs;
                        expectedOutputs = outputs;
                    }
                    else if (!SamePortKinds(expectedInputs, inputs) || !SamePortKinds(expectedOutputs, outputs))
                    {
                        throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' candidate fragments do not share one typed port contract.");
                    }
                    for (int outputIndex = 0; outputIndex < outputs.Length; outputIndex++)
                    {
                        CharacterLinkedPosePortValueBinding output = outputs[outputIndex];
                        bool produced = Operations
                            .Skip(fragment.OperationStart)
                            .Take(fragment.OperationCount)
                            .Any(value => output.Kind == CharacterPosePortKind.FullBodyIkGoals
                                ? value.OutputFullBodyIkGoalSetValueIndex == output.ValueIndex
                                : value.OutputValueIndex == output.ValueIndex);
                        if (!produced)
                        {
                            produced = fragment.Inputs.Any(value => value.Kind == output.Kind && value.ValueIndex == output.ValueIndex);
                        }
                        if (!produced)
                            throw new InvalidOperationException($"Linked Pose fragment #{fragmentIndex} output '{output.PortId}' is outside its operation range.");
                    }
                }
                int poseOutputCount = expectedOutputs.Count(value => value.Kind == CharacterPosePortKind.LocalPose || value.Kind == CharacterPosePortKind.ComponentPose);
                int goalOutputCount = expectedOutputs.Count(value => value.Kind == CharacterPosePortKind.FullBodyIkGoals);
                if (poseOutputCount > 1 || goalOutputCount > 1 || poseOutputCount + goalOutputCount != expectedOutputs.Length ||
                    (poseOutputCount == 1) != (operation.OutputValueIndex >= 0) ||
                    (goalOutputCount == 1) != (operation.OutputFullBodyIkGoalSetValueIndex >= 0))
                {
                    throw new InvalidOperationException($"Linked Pose Call '{call.NodeId}' output workspace does not match its Interface Entry.");
                }
            }
            if (LinkedPoseCalls.Count != Operations.Count(value => value.Code == CharacterPoseOperationCode.LinkedPoseCall) ||
                ownedFragments.Count != LinkedPoseFragments.Count)
                throw new InvalidOperationException("Pose Plan Linked Pose Call descriptor closure is incomplete.");
        }

        static bool SamePortKinds(
            IReadOnlyList<CharacterLinkedPosePortValueBinding> left,
            IReadOnlyList<CharacterLinkedPosePortValueBinding> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i].PortId != right[i].PortId || left[i].Kind != right[i].Kind)
                    return false;
            }
            return true;
        }

        int CountMotionMatchingProviders(int start, int count)
        {
            if (start < 0 || count < 0 || start + count > StateMachines.Count)
                throw new InvalidOperationException("Linked Pose fragment StateMachine range is invalid.");
            int result = 0;
            for (int machineIndex = start; machineIndex < start + count; machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine = StateMachines[machineIndex];
                for (int stateIndex = 0; stateIndex < machine.States.Count; stateIndex++)
                {
                    CharacterPoseStateDescriptor poseState = machine.States[stateIndex];
                    for (int providerIndex = 0; providerIndex < poseState.SourceProviders.Count; providerIndex++)
                    {
                        if (poseState.SourceProviders[providerIndex].SourceKind == AnimationPoseSourceKind.MotionMatching)
                            result = checked(result + 1);
                    }
                }
            }
            return result;
        }
    }
}
