using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Linq;
using ThirdPersonSimulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseWorkerActorRegistration : IDisposable
    {
        readonly CharacterPoseWorkerScheduler m_Scheduler;
        internal readonly CharacterPoseWorkerProgramBatch ProgramBatch;
        bool m_Disposed;

        internal CharacterPoseWorkerActorRegistration(
            CharacterPoseWorkerScheduler scheduler,
            CharacterPoseWorkerProgramBatch programBatch)
        {
            m_Scheduler = scheduler;
            ProgramBatch = programBatch;
        }

        internal bool IsValid =>
            !m_Disposed && m_Scheduler != null && ProgramBatch != null;

        internal void Fence()
        {
            if (!m_Disposed)
                ProgramBatch.Fence();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Fence();
            m_Disposed = true;
            m_Scheduler.Unregister(this);
        }
    }

    internal readonly struct CharacterPoseWorkerStageLease
    {
        internal CharacterPoseWorkerStageLease(
            CharacterPoseWorkerActorRegistration registration,
            CharacterPoseProgramExecutionView executionView,
            CharacterPoseProgramExecutor executor,
            ActorId actorId,
            int stageIndex,
            in CharacterPoseGraphNativeBinding frame,
            NativeArray<float> operationWeights,
            NativeArray<CharacterRootOrientationWarpNativeControl>
                rootOrientationWarpControls,
            NativeArray<byte> linkedPoseActiveFragments)
        {
            Registration = registration;
            ExecutionView = executionView;
            Executor = executor;
            ActorId = actorId;
            StageIndex = stageIndex;
            Frame = frame;
            OperationWeights = operationWeights;
            RootOrientationWarpControls = rootOrientationWarpControls;
            LinkedPoseActiveFragments = linkedPoseActiveFragments;
        }

        internal CharacterPoseWorkerActorRegistration Registration { get; }
        internal CharacterPoseProgramExecutionView ExecutionView { get; }
        internal CharacterPoseProgramExecutor Executor { get; }
        internal ActorId ActorId { get; }
        internal int StageIndex { get; }
        internal CharacterPoseGraphNativeBinding Frame { get; }
        internal NativeArray<float> OperationWeights { get; }
        internal NativeArray<CharacterRootOrientationWarpNativeControl>
            RootOrientationWarpControls { get; }
        internal NativeArray<byte> LinkedPoseActiveFragments { get; }
        internal ulong CompletionIdentity => Frame.CompletionIdentity;
        internal bool IsValid =>
            Registration?.IsValid == true && ExecutionView != null &&
            Executor != null && ActorId.IsValid && StageIndex >= 0 &&
            CompletionIdentity != 0 && OperationWeights.IsCreated;
    }

    internal sealed class CharacterPoseWorkerScheduler : IDisposable
    {
        readonly Dictionary<string, CharacterPoseWorkerProgramBatch> m_Programs =
            new Dictionary<string, CharacterPoseWorkerProgramBatch>(
                StringComparer.Ordinal);
        bool m_Collecting;
        bool m_Disposed;

        internal CharacterPoseWorkerActorRegistration Register(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView)
        {
            RequireAlive();
            if (image == null || executionView == null)
                throw new ArgumentNullException(nameof(image));
            image.RequireValid();
            executionView.RequireValid();
            if (!BurstCompiler.IsEnabled)
            {
                throw new InvalidOperationException(
                    "Pose Worker requires the published Burst execution capability.");
            }
            string key = image.PoseProgramImageHash + "/" +
                         image.WorkerPlan.RigLayout.Identity;
            if (!m_Programs.TryGetValue(
                    key,
                    out CharacterPoseWorkerProgramBatch program))
            {
                program = new CharacterPoseWorkerProgramBatch(
                    image,
                    executionView);
                m_Programs.Add(key, program);
            }
            else
            {
                program.RequireProgram(image, executionView);
            }
            program.RegisterActor();
            return new CharacterPoseWorkerActorRegistration(this, program);
        }

        internal void BeginBatch()
        {
            RequireAlive();
            if (m_Collecting)
                throw new InvalidOperationException(
                    "Pose Worker Scheduler already has an open batch.");
            foreach (CharacterPoseWorkerProgramBatch program in
                     m_Programs.Values)
                program.BeginBatch();
            m_Collecting = true;
        }

        internal void Submit(in CharacterPoseWorkerStageLease lease)
        {
            RequireAlive();
            if (!m_Collecting || !lease.IsValid)
                throw new InvalidOperationException(
                    "Pose Worker Stage lease cannot be submitted.");
            lease.Registration.ProgramBatch.Submit(in lease);
        }

        internal void CompleteBatch()
        {
            RequireAlive();
            if (!m_Collecting)
                throw new InvalidOperationException(
                    "Pose Worker Scheduler has no open batch.");
            JobHandle combined = default;
            bool scheduled = false;
            Exception failure = null;
            try
            {
                foreach (CharacterPoseWorkerProgramBatch program in
                         m_Programs.Values)
                {
                    if (!program.TrySchedule(out JobHandle handle))
                        continue;
                    combined = scheduled
                        ? JobHandle.CombineDependencies(combined, handle)
                        : handle;
                    scheduled = true;
                }
                if (scheduled)
                    combined.Complete();
                foreach (CharacterPoseWorkerProgramBatch program in
                         m_Programs.Values)
                    program.CompletePending();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            foreach (CharacterPoseWorkerProgramBatch program in
                     m_Programs.Values)
            {
                try
                {
                    program.Fence();
                }
                catch (Exception exception)
                {
                    failure = failure == null
                        ? exception
                        : new AggregateException(
                            "Pose Worker execution and fence both failed.",
                            failure,
                            exception);
                }
            }
            m_Collecting = false;
            if (failure != null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        internal void DiscardBatch()
        {
            RequireAlive();
            if (!m_Collecting)
                return;
            foreach (CharacterPoseWorkerProgramBatch program in
                     m_Programs.Values)
                program.DiscardPending();
            m_Collecting = false;
        }

        internal void Fence(
            CharacterPoseWorkerActorRegistration registration)
        {
            RequireAlive();
            registration?.Fence();
        }

        internal void Unregister(
            CharacterPoseWorkerActorRegistration registration)
        {
            if (m_Disposed || registration == null)
                return;
            registration.ProgramBatch.UnregisterActor();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            foreach (CharacterPoseWorkerProgramBatch program in
                     m_Programs.Values)
                program.Dispose();
            m_Programs.Clear();
            m_Collecting = false;
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseWorkerScheduler));
        }
    }

    internal sealed class CharacterPoseWorkerProgramBatch : IDisposable
    {
        readonly struct RuntimeBatch
        {
            internal RuntimeBatch(
                CharacterPoseWorkerBatchPlan plan,
                int operationStart)
            {
                Plan = plan;
                OperationStart = operationStart;
            }

            internal CharacterPoseWorkerBatchPlan Plan { get; }
            internal int OperationStart { get; }
        }

        readonly CharacterPoseProgramImage m_Image;
        readonly RuntimeBatch[] m_Batches;
        readonly NativeArray<int> m_NativeOperationIndices;
        readonly List<CharacterPoseWorkerStageLease> m_Pending =
            new List<CharacterPoseWorkerStageLease>();
        NativeArray<CharacterPoseWorkerActorSlice> m_ActorSlices;
        JobHandle m_Outstanding;
        int m_ActorCount;
        bool m_HasOutstanding;
        bool m_Disposed;

        internal CharacterPoseWorkerProgramBatch(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            executionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            image.RequireValid();
            executionView.RequireValid();
            var nativeByOperation = new Dictionary<int, int>();
            for (int i = 0; i < executionView.OperationHeaders.Length; i++)
            {
                nativeByOperation.Add(
                    executionView.OperationHeaders[i].Index,
                    i);
            }
            int operationCount = image.WorkerPlan.Batches.Sum(
                value => value.OperationIndices.Count);
            m_NativeOperationIndices = new NativeArray<int>(
                operationCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_Batches = new RuntimeBatch[image.WorkerPlan.Batches.Count];
            int cursor = 0;
            for (int batchIndex = 0;
                 batchIndex < image.WorkerPlan.Batches.Count;
                 batchIndex++)
            {
                CharacterPoseWorkerBatchPlan batch =
                    image.WorkerPlan.Batches[batchIndex];
                m_Batches[batchIndex] = new RuntimeBatch(batch, cursor);
                for (int i = 0; i < batch.OperationIndices.Count; i++)
                {
                    int operationIndex = batch.OperationIndices[i];
                    if (!nativeByOperation.TryGetValue(
                            operationIndex,
                            out int nativeIndex))
                    {
                        throw new InvalidOperationException(
                            $"Pose Worker Operation #{operationIndex} has no Execution View handle.");
                    }
                    m_NativeOperationIndices[cursor++] = nativeIndex;
                }
            }
        }

        internal void RequireProgram(
            CharacterPoseProgramImage image,
            CharacterPoseProgramExecutionView executionView)
        {
            if (image == null || executionView == null ||
                !string.Equals(
                    image.PoseProgramImageHash,
                    m_Image.PoseProgramImageHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    image.WorkerPlan.RigLayout.Identity,
                    m_Image.WorkerPlan.RigLayout.Identity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Worker registration does not match its Program batch.");
            }
            executionView.RequireValid();
        }

        internal void RegisterActor()
        {
            Fence();
            m_ActorCount++;
            EnsureCapacity(m_ActorCount);
            if (m_Pending.Capacity < m_ActorCount)
                m_Pending.Capacity = m_ActorCount;
        }

        internal void UnregisterActor()
        {
            Fence();
            if (m_ActorCount <= 0)
                throw new InvalidOperationException(
                    "Pose Worker Program batch Actor count underflowed.");
            m_ActorCount--;
        }

        internal void BeginBatch()
        {
            Fence();
            m_Pending.Clear();
        }

        internal void Submit(in CharacterPoseWorkerStageLease lease)
        {
            if (lease.Registration.ProgramBatch != this ||
                m_Pending.Count >= m_ActorCount)
            {
                throw new InvalidOperationException(
                    "Pose Worker Program batch rejected a duplicate Actor lease.");
            }
            if (m_Pending.Count > 0 &&
                m_Pending[0].StageIndex != lease.StageIndex)
            {
                throw new InvalidOperationException(
                    "Actors sharing one Pose Program reached different Worker stages in one dependency wave.");
            }
            CharacterPoseGraphNativeBinding frame = lease.Frame;
            var slice = CharacterPoseWorkerActorSlice.Create(
                in frame,
                lease.ExecutionView,
                lease.OperationWeights,
                lease.RootOrientationWarpControls,
                lease.LinkedPoseActiveFragments);
            for (int i = 0; i < m_Pending.Count; i++)
            {
                CharacterPoseWorkerStageLease pending = m_Pending[i];
                if (ReferenceEquals(
                        pending.Registration,
                        lease.Registration) ||
                    pending.ActorId == lease.ActorId &&
                    pending.CompletionIdentity == lease.CompletionIdentity)
                {
                    throw new InvalidOperationException(
                        "Pose Worker Program batch rejected a duplicate Actor lease.");
                }
                if (m_ActorSlices[i].WritePageIdentity ==
                    slice.WritePageIdentity)
                {
                    throw new InvalidOperationException(
                        "Pose Worker Actor leases share one writable Frame page.");
                }
            }
            m_ActorSlices[m_Pending.Count] = slice;
            m_Pending.Add(lease);
        }

        internal void DiscardPending()
        {
            if (m_HasOutstanding)
            {
                throw new InvalidOperationException(
                    "Pose Worker Program batch cannot discard scheduled work.");
            }
            m_Pending.Clear();
        }

        internal bool TrySchedule(out JobHandle handle)
        {
            handle = default;
            if (m_Pending.Count == 0)
                return false;
            CharacterPoseProgramExecutionView view =
                m_Pending[0].ExecutionView;
            int stageIndex = m_Pending[0].StageIndex;
            JobHandle dependency = default;
            bool hasDependency = false;
            int currentWave = -1;
            JobHandle waveHandle = default;
            bool hasWaveHandle = false;
            for (int batchIndex = 0; batchIndex < m_Batches.Length; batchIndex++)
            {
                RuntimeBatch batch = m_Batches[batchIndex];
                if (batch.Plan.StageIndex != stageIndex)
                    continue;
                if (currentWave >= 0 &&
                    batch.Plan.DependencyWave != currentWave)
                {
                    dependency = waveHandle;
                    hasDependency = hasWaveHandle;
                    waveHandle = default;
                    hasWaveHandle = false;
                }
                currentWave = batch.Plan.DependencyWave;
                CharacterPoseWorkerKernelRange range = new
                    CharacterPoseWorkerKernelRange
                    {
                        OperationStart = batch.OperationStart,
                        OperationCount = batch.Plan.OperationIndices.Count
                    };
                JobHandle scheduled = Schedule(
                    batch.Plan.Kernel,
                    range,
                    view,
                    m_Pending.Count,
                    hasDependency ? dependency : default);
                waveHandle = hasWaveHandle
                    ? JobHandle.CombineDependencies(waveHandle, scheduled)
                    : scheduled;
                hasWaveHandle = true;
            }
            if (hasWaveHandle)
            {
                dependency = waveHandle;
                hasDependency = true;
            }
            if (!hasDependency)
            {
                throw new InvalidOperationException(
                    $"Pose Worker Stage #{stageIndex} has no compiled batches.");
            }
            m_Outstanding = dependency;
            m_HasOutstanding = true;
            handle = dependency;
            return true;
        }

        JobHandle Schedule(
            CharacterPoseWorkerKernelId kernel,
            CharacterPoseWorkerKernelRange range,
            CharacterPoseProgramExecutionView view,
            int actorCount,
            JobHandle dependency) => kernel switch
        {
            CharacterPoseWorkerKernelId.ParameterResolve =>
                new CharacterPoseParameterResolveWorkerKernel
                {
                    Range = range,
                    Actors = m_ActorSlices,
                    NativeOperationIndices = m_NativeOperationIndices,
                    Headers = view.OperationHeaders,
                    ParameterDefaults = view.ParameterDefaults,
                    Operations = view.ParameterResolveOperations,
                    Policies = view.ParameterPolicies
                }.Schedule(actorCount, 1, dependency),
            CharacterPoseWorkerKernelId.Blend =>
                new CharacterPoseBlendWorkerKernel
                {
                    Range = range,
                    Actors = m_ActorSlices,
                    NativeOperationIndices = m_NativeOperationIndices,
                    Headers = view.OperationHeaders,
                    ParameterDefaults = view.ParameterDefaults,
                    Operations = view.BlendOperations
                }.Schedule(actorCount, 1, dependency),
            CharacterPoseWorkerKernelId.Composition =>
                new CharacterPoseCompositionWorkerKernel
                {
                    Range = range,
                    Actors = m_ActorSlices,
                    NativeOperationIndices = m_NativeOperationIndices,
                    Headers = view.OperationHeaders,
                    ParameterDefaults = view.ParameterDefaults,
                    Operations = view.CompositionOperations,
                    BoneMasks = view.DenseBoneMasks,
                    AdditiveReferences = view.AdditiveReferences,
                    ParentIndices = view.ParentIndices
                }.Schedule(actorCount, 1, dependency),
            CharacterPoseWorkerKernelId.SpaceConversion =>
                new CharacterPoseSpaceConversionWorkerKernel
                {
                    Range = range,
                    Actors = m_ActorSlices,
                    NativeOperationIndices = m_NativeOperationIndices,
                    Headers = view.OperationHeaders,
                    ParameterDefaults = view.ParameterDefaults,
                    Operations = view.SpaceConversionOperations,
                    ParentIndices = view.ParentIndices
                }.Schedule(actorCount, 1, dependency),
            CharacterPoseWorkerKernelId.ComponentControl =>
                new CharacterPoseComponentControlWorkerKernel
                {
                    Range = range,
                    Actors = m_ActorSlices,
                    NativeOperationIndices = m_NativeOperationIndices,
                    Headers = view.OperationHeaders,
                    ParameterDefaults = view.ParameterDefaults,
                    Operations = view.ComponentControlOperations,
                    ModifyBones = view.ModifyBones,
                    RootOrientationWarps = view.RootOrientationWarps,
                    ParentIndices = view.ParentIndices
                }.Schedule(actorCount, 1, dependency),
            _ => throw new InvalidOperationException(
                $"Pose Worker Kernel '{kernel}' is unavailable.")
        };

        internal void CompletePending()
        {
            if (m_Pending.Count == 0)
                return;
            Fence();
            for (int i = 0; i < m_Pending.Count; i++)
            {
                CharacterPoseWorkerStageLease lease = m_Pending[i];
                lease.Executor.CompleteWorkerStage(
                    lease.StageIndex,
                    lease.CompletionIdentity);
            }
            m_Pending.Clear();
        }

        internal void Fence()
        {
            if (!m_HasOutstanding)
                return;
            try
            {
                m_Outstanding.Complete();
            }
            finally
            {
                m_HasOutstanding = false;
            }
        }

        void EnsureCapacity(int actorCapacity)
        {
            if (m_ActorSlices.IsCreated &&
                m_ActorSlices.Length >= actorCapacity)
                return;
            if (m_ActorSlices.IsCreated)
                m_ActorSlices.Dispose();
            m_ActorSlices = new NativeArray<CharacterPoseWorkerActorSlice>(
                actorCapacity,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Fence();
            if (m_ActorSlices.IsCreated)
                m_ActorSlices.Dispose();
            if (m_NativeOperationIndices.IsCreated)
                m_NativeOperationIndices.Dispose();
            m_Pending.Clear();
            m_Disposed = true;
        }
    }
}
