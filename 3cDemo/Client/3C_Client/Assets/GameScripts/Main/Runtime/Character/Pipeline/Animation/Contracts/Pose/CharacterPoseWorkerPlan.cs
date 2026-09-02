using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseWorkerKernelId : byte
    {
        None = 0,
        ParameterResolve = 1,
        Blend = 2,
        Composition = 3,
        SpaceConversion = 4,
        ComponentControl = 5
    }

    public enum CharacterPoseWorkerValueAccess : byte
    {
        Read = 1,
        Write = 2
    }

    [Serializable]
    public sealed class CharacterPoseWorkerValueRange
    {
        [SerializeField] CharacterPoseValueReferenceKind m_Kind;
        [SerializeField] CharacterPoseWorkerValueAccess m_Access;
        [SerializeField] int m_Start;
        [SerializeField] int m_Count;

        public CharacterPoseWorkerValueRange(
            CharacterPoseValueReferenceKind kind,
            CharacterPoseWorkerValueAccess access,
            int start,
            int count)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseValueReferenceKind), kind) ||
                !Enum.IsDefined(typeof(CharacterPoseWorkerValueAccess), access) ||
                start < 0 || count <= 0)
            {
                throw new ArgumentException("Pose Worker Value range is invalid.");
            }
            m_Kind = kind;
            m_Access = access;
            m_Start = start;
            m_Count = count;
        }

        public CharacterPoseValueReferenceKind Kind => m_Kind;
        public CharacterPoseWorkerValueAccess Access => m_Access;
        public int Start => m_Start;
        public int Count => m_Count;
        public int End => checked(m_Start + m_Count);
    }

    [Serializable]
    public sealed class CharacterPoseWorkerBatchPlan
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_BatchIdentity = string.Empty;
        [SerializeField] string m_ActorBatchKey = string.Empty;
        [SerializeField] int m_StageIndex;
        [SerializeField] int m_DependencyWave;
        [SerializeField] CharacterPoseWorkerKernelId m_Kernel;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] int[] m_OperationIndices = Array.Empty<int>();
        [SerializeField] CharacterPoseWorkerValueRange[] m_ReadRanges =
            Array.Empty<CharacterPoseWorkerValueRange>();
        [SerializeField] CharacterPoseWorkerValueRange[] m_WriteRanges =
            Array.Empty<CharacterPoseWorkerValueRange>();
        [SerializeField] int[] m_InputCompletionIndices = Array.Empty<int>();
        [SerializeField] int[] m_OutputCompletionIndices = Array.Empty<int>();
        [SerializeField] int m_PoseWorkspaceStart;
        [SerializeField] int m_PoseWorkspaceCount;

        public CharacterPoseWorkerBatchPlan(
            int index,
            string batchIdentity,
            string actorBatchKey,
            int stageIndex,
            int dependencyWave,
            CharacterPoseWorkerKernelId kernel,
            CharacterPoseExecutionDomain executionDomain,
            int[] operationIndices,
            CharacterPoseWorkerValueRange[] readRanges,
            CharacterPoseWorkerValueRange[] writeRanges,
            int[] inputCompletionIndices,
            int[] outputCompletionIndices,
            int poseWorkspaceStart,
            int poseWorkspaceCount)
        {
            if (index < 0 || stageIndex < 0 || dependencyWave < 0 ||
                kernel == CharacterPoseWorkerKernelId.None ||
                !Enum.IsDefined(typeof(CharacterPoseWorkerKernelId), kernel) ||
                !CharacterPoseWorkerKernels.IsWorkerDomain(executionDomain) ||
                operationIndices == null || operationIndices.Length == 0 ||
                operationIndices.Any(value => value < 0) ||
                operationIndices.Distinct().Count() != operationIndices.Length ||
                readRanges == null || writeRanges == null ||
                inputCompletionIndices == null || outputCompletionIndices == null ||
                outputCompletionIndices.Length != operationIndices.Length ||
                poseWorkspaceStart < 0 || poseWorkspaceCount < 0)
            {
                throw new ArgumentException("Pose Worker Batch plan is invalid.");
            }
            m_Index = index;
            m_BatchIdentity = PoseIdentity.Require(
                batchIdentity,
                nameof(batchIdentity));
            m_ActorBatchKey = PoseIdentity.Require(
                actorBatchKey,
                nameof(actorBatchKey));
            m_StageIndex = stageIndex;
            m_DependencyWave = dependencyWave;
            m_Kernel = kernel;
            m_ExecutionDomain = executionDomain;
            m_OperationIndices = operationIndices;
            m_ReadRanges = readRanges;
            m_WriteRanges = writeRanges;
            m_InputCompletionIndices = inputCompletionIndices;
            m_OutputCompletionIndices = outputCompletionIndices;
            m_PoseWorkspaceStart = poseWorkspaceStart;
            m_PoseWorkspaceCount = poseWorkspaceCount;
        }

        public int Index => m_Index;
        public string BatchIdentity => m_BatchIdentity ?? string.Empty;
        public string ActorBatchKey => m_ActorBatchKey ?? string.Empty;
        public int StageIndex => m_StageIndex;
        public int DependencyWave => m_DependencyWave;
        public CharacterPoseWorkerKernelId Kernel => m_Kernel;
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public IReadOnlyList<int> OperationIndices =>
            m_OperationIndices ?? Array.Empty<int>();
        public IReadOnlyList<CharacterPoseWorkerValueRange> ReadRanges =>
            m_ReadRanges ?? Array.Empty<CharacterPoseWorkerValueRange>();
        public IReadOnlyList<CharacterPoseWorkerValueRange> WriteRanges =>
            m_WriteRanges ?? Array.Empty<CharacterPoseWorkerValueRange>();
        public IReadOnlyList<int> InputCompletionIndices =>
            m_InputCompletionIndices ?? Array.Empty<int>();
        public IReadOnlyList<int> OutputCompletionIndices =>
            m_OutputCompletionIndices ?? Array.Empty<int>();
        public int PoseWorkspaceStart => m_PoseWorkspaceStart;
        public int PoseWorkspaceCount => m_PoseWorkspaceCount;
    }

    [Serializable]
    public sealed class CharacterPoseRigExecutionLayout
    {
        [SerializeField] string m_Identity = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] int m_PoseBoneCount;
        [SerializeField] int m_PhysicalBoneCount;
        [SerializeField] int m_VirtualBoneCount;

        public CharacterPoseRigExecutionLayout(
            string identity,
            string rigId,
            string rigRevision,
            int poseBoneCount,
            int physicalBoneCount,
            int virtualBoneCount)
        {
            if (poseBoneCount <= 0 || physicalBoneCount <= 0 ||
                virtualBoneCount < 0 ||
                physicalBoneCount + virtualBoneCount != poseBoneCount)
            {
                throw new ArgumentException(
                    "Pose Worker Rig execution layout is invalid.");
            }
            m_Identity = PoseIdentity.Require(identity, nameof(identity));
            m_RigId = PoseIdentity.Require(rigId, nameof(rigId));
            m_RigRevision = PoseIdentity.Require(
                rigRevision,
                nameof(rigRevision));
            m_PoseBoneCount = poseBoneCount;
            m_PhysicalBoneCount = physicalBoneCount;
            m_VirtualBoneCount = virtualBoneCount;
        }

        public string Identity => m_Identity ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public int PoseBoneCount => m_PoseBoneCount;
        public int PhysicalBoneCount => m_PhysicalBoneCount;
        public int VirtualBoneCount => m_VirtualBoneCount;
    }

    [Serializable]
    public sealed class CharacterPoseWorkerPlan
    {
        public const string SchemaVersion = "character-pose-worker-plan/v1";
        public const string ExecutionPolicy = "worker-required/v1";
        public const string PlatformCapability = "unity-jobs-burst-hpcsharp/v1";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_ExecutionPolicy = ExecutionPolicy;
        [SerializeField] string m_PlatformCapability = PlatformCapability;
        [SerializeField] CharacterPoseRigExecutionLayout m_RigLayout;
        [SerializeField] CharacterPoseWorkerKernelId[] m_KernelSet =
            Array.Empty<CharacterPoseWorkerKernelId>();
        [SerializeField] CharacterPoseWorkerBatchPlan[] m_Batches =
            Array.Empty<CharacterPoseWorkerBatchPlan>();

        public CharacterPoseWorkerPlan(
            CharacterPoseRigExecutionLayout rigLayout,
            CharacterPoseWorkerKernelId[] kernelSet,
            CharacterPoseWorkerBatchPlan[] batches)
        {
            m_RigLayout = rigLayout ??
                throw new ArgumentNullException(nameof(rigLayout));
            m_KernelSet = kernelSet ??
                throw new ArgumentNullException(nameof(kernelSet));
            m_Batches = batches ?? throw new ArgumentNullException(nameof(batches));
            if (m_KernelSet.Length == 0 || m_Batches.Length == 0 ||
                m_KernelSet.Any(value => value == CharacterPoseWorkerKernelId.None) ||
                m_KernelSet.Distinct().Count() != m_KernelSet.Length)
            {
                throw new ArgumentException("Pose Worker plan has no fixed Kernel Set.");
            }
        }

        public string Version => m_SchemaVersion ?? string.Empty;
        public string Policy => m_ExecutionPolicy ?? string.Empty;
        public string Capability => m_PlatformCapability ?? string.Empty;
        public CharacterPoseRigExecutionLayout RigLayout => m_RigLayout;
        public IReadOnlyList<CharacterPoseWorkerKernelId> KernelSet =>
            m_KernelSet ?? Array.Empty<CharacterPoseWorkerKernelId>();
        public IReadOnlyList<CharacterPoseWorkerBatchPlan> Batches =>
            m_Batches ?? Array.Empty<CharacterPoseWorkerBatchPlan>();

        public void RequireValid(
            CharacterPoseOperationPages operationPages,
            IReadOnlyList<CharacterPresentationPoseStage> stages,
            int poseValueCount,
            int frameCacheCount,
            string rigId,
            string rigRevision)
        {
            if (!string.Equals(Version, SchemaVersion, StringComparison.Ordinal) ||
                !string.Equals(Policy, ExecutionPolicy, StringComparison.Ordinal) ||
                !string.Equals(Capability, PlatformCapability, StringComparison.Ordinal) ||
                RigLayout == null || operationPages == null || stages == null ||
                poseValueCount <= 0 || frameCacheCount <= 0 ||
                !string.Equals(RigLayout.RigId, rigId, StringComparison.Ordinal) ||
                !string.Equals(RigLayout.RigRevision, rigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Pose Worker plan identity is invalid.");
            }
            var workerOperations = new HashSet<int>();
            var kernels = new HashSet<CharacterPoseWorkerKernelId>();
            for (int batchIndex = 0; batchIndex < Batches.Count; batchIndex++)
            {
                CharacterPoseWorkerBatchPlan batch = Batches[batchIndex];
                if (batch == null || batch.Index != batchIndex ||
                    (uint)batch.StageIndex >= (uint)stages.Count ||
                    stages[batch.StageIndex].ExecutionDomain != batch.ExecutionDomain)
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Batch #{batchIndex} identity is invalid.");
                }
                kernels.Add(batch.Kernel);
                for (int i = 0; i < batch.OperationIndices.Count; i++)
                {
                    int operationIndex = batch.OperationIndices[i];
                    if ((uint)operationIndex >= (uint)operationPages.Headers.Count ||
                        !workerOperations.Add(operationIndex))
                    {
                        throw new InvalidOperationException(
                            $"Pose Worker Batch #{batchIndex} Operation range is invalid.");
                    }
                    CharacterPoseOperationHeader operation =
                        operationPages.Headers[operationIndex];
                    if (operation.ExecutionDomain != batch.ExecutionDomain ||
                        CharacterPoseWorkerKernels.Require(operation.Code) != batch.Kernel ||
                        batch.OutputCompletionIndices[i] != operation.Index)
                    {
                        throw new InvalidOperationException(
                            $"Pose Worker Batch #{batchIndex} Kernel mapping is invalid.");
                    }
                }
                RequireRanges(batch.ReadRanges, CharacterPoseWorkerValueAccess.Read, poseValueCount);
                RequireRanges(batch.WriteRanges, CharacterPoseWorkerValueAccess.Write, poseValueCount);
                if (batch.InputCompletionIndices.Any(value => value < 0 || value >= frameCacheCount))
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Batch #{batchIndex} Completion dependency is invalid.");
                }
            }
            for (int operationIndex = 0;
                 operationIndex < operationPages.Headers.Count;
                 operationIndex++)
            {
                bool worker = CharacterPoseWorkerKernels.IsWorkerDomain(
                    operationPages.Headers[operationIndex].ExecutionDomain);
                if (worker != workerOperations.Contains(operationIndex))
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Operation #{operationIndex} ownership is incomplete.");
                }
            }
            if (!kernels.SetEquals(KernelSet))
                throw new InvalidOperationException("Pose Worker Kernel Set is incomplete.");
        }

        static void RequireRanges(
            IReadOnlyList<CharacterPoseWorkerValueRange> ranges,
            CharacterPoseWorkerValueAccess access,
            int poseValueCount)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                CharacterPoseWorkerValueRange range = ranges[i];
                if (range == null || range.Access != access ||
                    range.Kind == CharacterPoseValueReferenceKind.Pose &&
                    range.End > poseValueCount)
                {
                    throw new InvalidOperationException(
                        "Pose Worker typed Value range is invalid.");
                }
            }
        }
    }

    public static class CharacterPoseWorkerKernels
    {
        public static bool IsWorkerDomain(CharacterPoseExecutionDomain domain) =>
            domain == CharacterPoseExecutionDomain.PureValue ||
            domain == CharacterPoseExecutionDomain.PurePose;

        public static CharacterPoseWorkerKernelId Require(
            CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.PoseParameterResolve =>
                CharacterPoseWorkerKernelId.ParameterResolve,
            CharacterPoseOperationCode.BlendPose =>
                CharacterPoseWorkerKernelId.Blend,
            CharacterPoseOperationCode.LayeredBoneBlend or
            CharacterPoseOperationCode.AdditivePose =>
                CharacterPoseWorkerKernelId.Composition,
            CharacterPoseOperationCode.LocalToComponentPose or
            CharacterPoseOperationCode.ComponentToLocalPose =>
                CharacterPoseWorkerKernelId.SpaceConversion,
            CharacterPoseOperationCode.ModifyBone or
            CharacterPoseOperationCode.RootOrientationWarp =>
                CharacterPoseWorkerKernelId.ComponentControl,
            _ => throw new InvalidOperationException(
                $"Pose Operation '{code}' has no AOT Worker Kernel.")
        };
    }
}
