using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseWorkerBatchPlanPass
    {
        readonly struct BatchKey : IEquatable<BatchKey>
        {
            internal BatchKey(
                int stageIndex,
                int wave,
                CharacterPoseWorkerKernelId kernel,
                CharacterPoseExecutionDomain domain)
            {
                StageIndex = stageIndex;
                Wave = wave;
                Kernel = kernel;
                Domain = domain;
            }

            internal int StageIndex { get; }
            internal int Wave { get; }
            internal CharacterPoseWorkerKernelId Kernel { get; }
            internal CharacterPoseExecutionDomain Domain { get; }

            public bool Equals(BatchKey other) =>
                StageIndex == other.StageIndex &&
                Wave == other.Wave &&
                Kernel == other.Kernel &&
                Domain == other.Domain;

            public override bool Equals(object obj) =>
                obj is BatchKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(StageIndex, Wave, Kernel, Domain);
        }

        internal static CharacterPoseWorkerPlan Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseFamilyPayloadBinding binding,
            CharacterPoseStageSchedule schedule,
            CharacterPoseWorkspacePlan workspace)
        {
            if (request == null || binding == null || schedule == null ||
                workspace == null ||
                binding.Operations.Length != schedule.OperationCount ||
                workspace.FrameCacheCapacity != schedule.OperationCount)
            {
                throw new ArgumentException(
                    "Pose Worker Batch Plan input is invalid.");
            }

            CharacterPoseBoundOperation[] operations = binding.Operations;
            int[] stageByOperation = BuildStageMap(schedule, operations.Length);
            Dictionary<int, int> poseProducers = BuildPoseProducers(operations);
            int[] waves = Enumerable.Repeat(-1, operations.Length).ToArray();
            var grouped = new Dictionary<BatchKey, List<int>>();
            for (int operationIndex = 0;
                 operationIndex < operations.Length;
                 operationIndex++)
            {
                CharacterPoseBoundOperation operation = operations[operationIndex];
                if (!CharacterPoseWorkerKernels.IsWorkerDomain(
                        operation.ExecutionDomain))
                {
                    continue;
                }
                CharacterPoseWorkerKernelId kernel =
                    CharacterPoseWorkerKernels.Require(operation.Code);
                int stageIndex = stageByOperation[operationIndex];
                int wave = ResolveWave(
                    operation,
                    stageIndex,
                    stageByOperation,
                    poseProducers,
                    waves);
                waves[operationIndex] = wave;
                var key = new BatchKey(
                    stageIndex,
                    wave,
                    kernel,
                    operation.ExecutionDomain);
                if (!grouped.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>();
                    grouped.Add(key, indices);
                }
                indices.Add(operationIndex);
            }
            if (grouped.Count == 0)
            {
                throw new InvalidOperationException(
                    "Pose Program has no Pure Value or Pure Pose Worker work.");
            }

            CharacterPoseRigExecutionLayout rigLayout = BuildRigLayout(
                request);
            var batches = new List<CharacterPoseWorkerBatchPlan>(grouped.Count);
            foreach (KeyValuePair<BatchKey, List<int>> entry in grouped
                         .OrderBy(value => value.Key.StageIndex)
                         .ThenBy(value => value.Key.Wave)
                         .ThenBy(value => value.Key.Kernel))
            {
                int[] operationIndices = entry.Value.OrderBy(value => value).ToArray();
                int[] reads = operationIndices
                    .SelectMany(index => PoseInputs(operations[index]))
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                int[] writes = operationIndices
                    .Select(index => operations[index].OutputValueIndex)
                    .Where(value => value >= 0)
                    .ToArray();
                if (writes.Length != writes.Distinct().Count() ||
                    reads.Intersect(writes).Any())
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Batch stage={entry.Key.StageIndex}, wave={entry.Key.Wave}, kernel={entry.Key.Kernel} has an aliasing write set.");
                }
                int[] inputCompletions = reads
                    .Where(poseProducers.ContainsKey)
                    .Select(value => poseProducers[value])
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray();
                string actorBatchKey = StableHash.Compute(
                    CharacterPoseWorkerPlan.SchemaVersion,
                    request.Rig.RigId,
                    request.Rig.Revision,
                    rigLayout.Identity,
                    entry.Key.StageIndex.ToString(),
                    entry.Key.Wave.ToString(),
                    ((int)entry.Key.Kernel).ToString(),
                    ((int)entry.Key.Domain).ToString(),
                    string.Join(",", operationIndices)).ToString();
                int workspaceStart = writes.Length == 0 ? 0 : writes.Min();
                int workspaceCount = writes.Length == 0
                    ? 0
                    : checked(writes.Max() - workspaceStart + 1);
                batches.Add(new CharacterPoseWorkerBatchPlan(
                    batches.Count,
                    $"worker-batch-{batches.Count}-{actorBatchKey}",
                    actorBatchKey,
                    entry.Key.StageIndex,
                    entry.Key.Wave,
                    entry.Key.Kernel,
                    entry.Key.Domain,
                    operationIndices,
                    CreateRanges(reads, CharacterPoseWorkerValueAccess.Read),
                    CreateRanges(writes, CharacterPoseWorkerValueAccess.Write),
                    inputCompletions,
                    operationIndices,
                    workspaceStart,
                    workspaceCount));
            }
            CharacterPoseWorkerKernelId[] kernelSet = batches
                .Select(value => value.Kernel)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            var result = new CharacterPoseWorkerPlan(
                rigLayout,
                kernelSet,
                batches.ToArray());
            result.RequireValid(
                binding.OperationPages,
                schedule.Stages,
                workspace.PoseValueCapacity,
                workspace.FrameCacheCapacity,
                request.Rig.RigId,
                request.Rig.Revision);
            return result;
        }

        static int[] BuildStageMap(
            CharacterPoseStageSchedule schedule,
            int operationCount)
        {
            int[] result = Enumerable.Repeat(-1, operationCount).ToArray();
            for (int stageIndex = 0; stageIndex < schedule.Stages.Count; stageIndex++)
            {
                CharacterPresentationPoseStage stage = schedule.Stages[stageIndex];
                for (int operationIndex = stage.OperationStart;
                     operationIndex < stage.OperationStart + stage.OperationCount;
                     operationIndex++)
                {
                    if (result[operationIndex] != -1)
                        throw new InvalidOperationException(
                            $"Pose Operation #{operationIndex} belongs to multiple Stages.");
                    result[operationIndex] = stageIndex;
                }
            }
            if (result.Any(value => value < 0))
                throw new InvalidOperationException(
                    "Pose Worker Batch Plan has an incomplete Stage map.");
            return result;
        }

        static Dictionary<int, int> BuildPoseProducers(
            IReadOnlyList<CharacterPoseBoundOperation> operations)
        {
            var result = new Dictionary<int, int>();
            for (int i = 0; i < operations.Count; i++)
            {
                int output = operations[i].OutputValueIndex;
                if (output >= 0 && !result.TryAdd(output, i))
                    throw new InvalidOperationException(
                        $"Pose Value #{output} has multiple producers.");
            }
            return result;
        }

        static int ResolveWave(
            CharacterPoseBoundOperation operation,
            int stageIndex,
            IReadOnlyList<int> stageByOperation,
            IReadOnlyDictionary<int, int> poseProducers,
            IReadOnlyList<int> waves)
        {
            int wave = 0;
            foreach (int input in PoseInputs(operation))
            {
                if (!poseProducers.TryGetValue(input, out int producerIndex) ||
                    stageByOperation[producerIndex] != stageIndex ||
                    !CharacterPoseWorkerKernels.IsWorkerDomain(
                        operation.ExecutionDomain))
                {
                    continue;
                }
                int producerWave = waves[producerIndex];
                if (producerWave < 0)
                {
                    throw new InvalidOperationException(
                        $"Pose Worker Operation #{operation.Index} has an unresolved in-stage dependency.");
                }
                wave = Math.Max(wave, checked(producerWave + 1));
            }
            return wave;
        }

        static IEnumerable<int> PoseInputs(
            CharacterPoseBoundOperation operation)
        {
            if (operation.InputValueIndexA >= 0)
                yield return operation.InputValueIndexA;
            if (operation.InputValueIndexB >= 0)
                yield return operation.InputValueIndexB;
        }

        static CharacterPoseWorkerValueRange[] CreateRanges(
            IReadOnlyList<int> values,
            CharacterPoseWorkerValueAccess access)
        {
            if (values.Count == 0)
                return Array.Empty<CharacterPoseWorkerValueRange>();
            var result = new List<CharacterPoseWorkerValueRange>();
            int start = values[0];
            int previous = start;
            for (int i = 1; i < values.Count; i++)
            {
                int value = values[i];
                if (value == previous + 1)
                {
                    previous = value;
                    continue;
                }
                result.Add(new CharacterPoseWorkerValueRange(
                    CharacterPoseValueReferenceKind.Pose,
                    access,
                    start,
                    previous - start + 1));
                start = value;
                previous = value;
            }
            result.Add(new CharacterPoseWorkerValueRange(
                CharacterPoseValueReferenceKind.Pose,
                access,
                start,
                previous - start + 1));
            return result.ToArray();
        }

        static CharacterPoseRigExecutionLayout BuildRigLayout(
            CharacterPoseCompilationRequest request)
        {
            var values = new List<string>
            {
                CharacterPoseWorkerPlan.SchemaVersion,
                request.Rig.RigId,
                request.Rig.Revision,
                request.Rig.PhysicalBoneCount.ToString(),
                request.Rig.VirtualBoneCount.ToString()
            };
            for (int i = 0; i < request.Rig.PhysicalBones.Count; i++)
            {
                values.Add(
                    $"physical:{i}:{request.Rig.PhysicalBones[i].BoneId}:{request.Rig.PhysicalBones[i].ParentIndex}");
            }
            for (int i = 0; i < request.Rig.VirtualBones.Count; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone =
                    request.Rig.VirtualBones[i];
                values.Add(
                    $"virtual:{i}:{bone.VirtualBoneId}:{bone.SourcePhysicalBoneId}:{bone.TargetPhysicalBoneId}");
            }
            return new CharacterPoseRigExecutionLayout(
                StableHash.Compute(values.ToArray()).ToString(),
                request.Rig.RigId,
                request.Rig.Revision,
                request.Rig.PoseBoneCount,
                request.Rig.PhysicalBoneCount,
                request.Rig.VirtualBoneCount);
        }
    }
}
