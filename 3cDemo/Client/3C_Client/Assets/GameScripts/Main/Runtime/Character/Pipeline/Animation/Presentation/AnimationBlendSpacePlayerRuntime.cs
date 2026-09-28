using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class AnimationBlendSpacePlayerRuntime : IDisposable
    {
        struct State
        {
            internal CharacterAnimationPoseInputFrame ParameterFrame;
            internal double RawContinuousTime;
            internal double ContinuousTime;
            internal double ContinuationAnchorRawTime;
            internal double ContinuationAnchorEffectiveTime;
            internal float SampleTime;
            internal int Cycle;
            internal int ClipSampleCount;
            internal ulong NextSourceGeneration;
            internal ulong ContinuityIdentity;
            internal ulong NextContinuityIdentity;
            internal ulong NextEventIdentity;
            internal ulong ResetSequence;
            internal ulong NextResetSequence;
            internal AnimationPoseSourceId SourceId;
            internal ulong PredictionSourceIdentity;
            internal PoseDiscontinuityEndpoint Endpoint;
            internal PoseDiscontinuityResetReason
                PendingResetReason;
            internal CharacterAnimationBlendSpaceCanonicalPhase
                CanonicalPhase;
            internal float RawX;
            internal float RawY;
            internal float X;
            internal float Y;
            internal bool HasFootFeatures;
            internal bool Relevant;
            internal bool SourceRetained;
            internal bool HasCompletedFrame;
            internal bool HasContinuationAnchor;
        }

        readonly CharacterAnimationBlendSpacePlayerPlan m_Descriptor;
        readonly CharacterAnimationBlendSpacePlan m_Plan;
        readonly CharacterAnimationBlendSpaceSolverPlan m_Solver;
        readonly CharacterAnimationBlendSpacePhasePlan m_Phase;
        readonly CharacterAnimationBlendSpaceWeightPage m_Weights;
        readonly CharacterAnimationBlendSpaceTimePage m_Times;
        readonly Dictionary<CharacterAnimationBlendSpaceSampleId, int> m_SampleIndices;
        readonly int[] m_ActiveSampleIndices;
        readonly SourceParameterBinding[] m_SourceParameters;
        readonly AnimationBlendSourcePoseWorkspace m_SourceWorkspace;
        readonly AnimationFootAnalysisProjectionIdentity m_FootAnalysis;
        readonly PoseParameterId[] m_ParameterIds;
        readonly CharacterPoseParameterUsage[] m_ParameterUsages;
        readonly float[] m_Parameters;
        readonly byte[] m_ParameterAvailability;
        readonly ClipSamplePlan[] m_ClipSamples;
        readonly double[] m_SampleRawTimes;
        readonly AnimationPlayerReleaseJournal m_Releases;
        readonly int m_FootPlacementWeightParameterIndex;
        State m_CommittedState;
        State m_PendingState;
        bool m_FrameOpen;
        bool m_Disposed;

        ref State ActiveState
        {
            get
            {
                if (m_FrameOpen)
                    return ref m_PendingState;
                return ref m_CommittedState;
            }
        }

        CharacterAnimationPoseInputFrame m_ParameterFrame { get => ActiveState.ParameterFrame; set => ActiveState.ParameterFrame = value; }
        double m_ContinuousTime { get => ActiveState.ContinuousTime; set => ActiveState.ContinuousTime = value; }
        double m_RawContinuousTime { get => ActiveState.RawContinuousTime; set => ActiveState.RawContinuousTime = value; }
        double m_ContinuationAnchorRawTime { get => ActiveState.ContinuationAnchorRawTime; set => ActiveState.ContinuationAnchorRawTime = value; }
        double m_ContinuationAnchorEffectiveTime { get => ActiveState.ContinuationAnchorEffectiveTime; set => ActiveState.ContinuationAnchorEffectiveTime = value; }
        float m_SampleTime { get => ActiveState.SampleTime; set => ActiveState.SampleTime = value; }
        int m_Cycle { get => ActiveState.Cycle; set => ActiveState.Cycle = value; }
        int m_ClipSampleCount { get => ActiveState.ClipSampleCount; set => ActiveState.ClipSampleCount = value; }
        ulong m_NextSourceGeneration { get => ActiveState.NextSourceGeneration; set => ActiveState.NextSourceGeneration = value; }
        ulong m_ContinuityIdentity { get => ActiveState.ContinuityIdentity; set => ActiveState.ContinuityIdentity = value; }
        ulong m_NextContinuityIdentity { get => ActiveState.NextContinuityIdentity; set => ActiveState.NextContinuityIdentity = value; }
        ulong m_NextEventIdentity { get => ActiveState.NextEventIdentity; set => ActiveState.NextEventIdentity = value; }
        ulong m_ResetSequence { get => ActiveState.ResetSequence; set => ActiveState.ResetSequence = value; }
        ulong m_NextResetSequence { get => ActiveState.NextResetSequence; set => ActiveState.NextResetSequence = value; }
        AnimationPoseSourceId m_SourceId
        {
            get => ActiveState.SourceId;
            set
            {
                ref State state = ref ActiveState;
                state.SourceId = value;
                state.PredictionSourceIdentity = value.IsValid
                    ? AnimationPredictedFootStepSample.SourceIdentity(value)
                    : 0;
            }
        }
        PoseDiscontinuityEndpoint m_Endpoint { get => ActiveState.Endpoint; set => ActiveState.Endpoint = value; }
        PoseDiscontinuityResetReason m_PendingResetReason { get => ActiveState.PendingResetReason; set => ActiveState.PendingResetReason = value; }
        CharacterAnimationBlendSpaceCanonicalPhase m_CanonicalPhase { get => ActiveState.CanonicalPhase; set => ActiveState.CanonicalPhase = value; }
        float m_RawX { get => ActiveState.RawX; set => ActiveState.RawX = value; }
        float m_RawY { get => ActiveState.RawY; set => ActiveState.RawY = value; }
        float m_X { get => ActiveState.X; set => ActiveState.X = value; }
        float m_Y { get => ActiveState.Y; set => ActiveState.Y = value; }
        bool m_HasFootFeatures { get => ActiveState.HasFootFeatures; set => ActiveState.HasFootFeatures = value; }
        bool m_Relevant { get => ActiveState.Relevant; set => ActiveState.Relevant = value; }
        bool m_SourceRetained { get => ActiveState.SourceRetained; set => ActiveState.SourceRetained = value; }
        bool m_HasCompletedFrame { get => ActiveState.HasCompletedFrame; set => ActiveState.HasCompletedFrame = value; }
        bool m_HasContinuationAnchor { get => ActiveState.HasContinuationAnchor; set => ActiveState.HasContinuationAnchor = value; }

        internal AnimationBlendSpacePlayerRuntime(
            CharacterAnimationBlendSpacePlayerPlan descriptor,
            CharacterAnimationBlendSpacePlan plan,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters,
            int footPlacementWeightParameterIndex,
            CharacterAnimationRigPayload rig,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            System.Collections.Generic.IReadOnlyList<AnimationClipPhasePlan> clipPhasePlans)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            NodeId = descriptor.NodeId;
            m_Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if ((uint)footPlacementWeightParameterIndex >= (uint)parameters.Count ||
                !parameters[footPlacementWeightParameterIndex].ParameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight))
                throw new ArgumentException("Blend Space Player foot weight parameter binding is invalid.", nameof(footPlacementWeightParameterIndex));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            m_FootAnalysis = footAnalysis;
            m_Solver = m_Plan.CreateSolverPlan();
            m_Phase = m_Plan.CreatePhasePlan(clipPhasePlans);
            m_Weights = new CharacterAnimationBlendSpaceWeightPage(m_Plan.Samples.Count);
            m_Times = new CharacterAnimationBlendSpaceTimePage(m_Plan.Samples.Count);
            m_SampleIndices = new Dictionary<CharacterAnimationBlendSpaceSampleId, int>(m_Plan.Samples.Count);
            m_ActiveSampleIndices = new int[m_Plan.Samples.Count];
            for (int i = 0; i < m_Plan.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSamplePlan sample = m_Plan.Samples[i];
                m_SampleIndices.Add(sample.SampleId, i);
                if (sample.HasFootFeatures)
                {
                    sample.LeftFootFeatures.RequireValid();
                    sample.RightFootFeatures.RequireValid();
                }
            }
            m_SourceParameters = new SourceParameterBinding[parameters.Count];
            m_ParameterIds = new PoseParameterId[parameters.Count];
            m_ParameterUsages = new CharacterPoseParameterUsage[parameters.Count];
            m_Parameters = new float[parameters.Count];
            m_ParameterAvailability = new byte[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                m_ParameterIds[i] = parameters[i].ParameterId;
                m_ParameterUsages[i] = parameters[i].Usage;
                m_Parameters[i] = parameters[i].DefaultValue;
                m_ParameterAvailability[i] = 1;
                if (i != descriptor.XParameterIndex && i != descriptor.YParameterIndex &&
                    i != footPlacementWeightParameterIndex &&
                    parameters[i].Usage != CharacterPoseParameterUsage.AnimatedProperty)
                    m_SourceParameters[i] = new SourceParameterBinding(m_Plan, parameters[i].ParameterId);
            }
            m_FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
            m_ClipSamples = new ClipSamplePlan[m_Plan.Samples.Count];
            m_SampleRawTimes = new double[m_Plan.Samples.Count];
            m_SourceWorkspace =
                new AnimationBlendSourcePoseWorkspace(
                    rig,
                    parameters.Count,
                    AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_Releases = new AnimationPlayerReleaseJournal(
                AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_CommittedState = new State
            {
                NextSourceGeneration = 1,
                ContinuityIdentity = 1,
                NextContinuityIdentity = 2,
                NextEventIdentity = 1,
                ResetSequence = 1,
                NextResetSequence = 2,
                PendingResetReason = PoseDiscontinuityResetReason.Initialization
            };
            m_PendingState = m_CommittedState;
        }

        internal PoseNodeId NodeId { get; }
        internal int PlayerIndex => m_Descriptor.PlayerIndex;
        internal AnimationPoseSourceId SourceId => m_SourceId;
        internal bool IsRelevant => m_Relevant;
        internal IReadOnlyList<CharacterAnimationBlendSpaceSamplePlan>
            ResourceSamples => m_Plan.Samples;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal float RemainingTime => float.MaxValue;
        internal double ContinuousTime => m_ContinuousTime;
        internal double RawContinuousTime => m_RawContinuousTime;
        internal AnimationReadOnlyBuffer<ClipSamplePlan> ClipSamples =>
            new AnimationReadOnlyBuffer<ClipSamplePlan>(
                m_ClipSamples,
                0,
                m_ClipSampleCount);

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' frame is already open.");
            m_PendingState = m_CommittedState;
            m_Releases.BeginFrame();
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            DiscardSourceFrame();
            m_SourceWorkspace.DiscardPreparedReleases();
            m_Releases.DiscardFrame();
            m_PendingState = m_CommittedState;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' frame is not open.");
            m_CommittedState = m_PendingState;
            m_Releases.CommitFrame();
            m_FrameOpen = false;
        }

        internal void SetRelevant(bool relevant)
        {
            RequireAlive();
            if (m_Relevant == relevant)
                return;
            m_Relevant = relevant;
            m_HasCompletedFrame = false;
            if (!relevant)
            {
                ReleaseRetainedSource();
                m_SourceId = default;
                m_Endpoint = default;
                ClearContinuationAnchor();
                return;
            }
            if (m_NextSourceGeneration == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' source generation was exhausted.");
            ulong generation = m_NextSourceGeneration++;
            m_SourceId = new AnimationPoseSourceId(
                m_Descriptor.PresentationPoseSourceIndex,
                AnimationPoseSourceKind.BlendSpace,
                new AnimationPoseSelectionGeneration(generation));
            m_Endpoint =
                new PoseDiscontinuityEndpoint(m_SourceId);
            m_ContinuityIdentity =
                AllocateContinuityIdentity();
            m_ResetSequence = AllocateResetSequence();
            m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            ClearContinuationAnchor();
            SetRawClock(0d);
        }

        internal void SetParameterFrame(
            in CharacterAnimationPoseInputFrame parameterFrame)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!parameterFrame.IsValid)
                throw new ArgumentException(
                    "Blend Space Player parameter frame is invalid.",
                    nameof(parameterFrame));
            m_ParameterFrame = parameterFrame;
        }

        internal void Advance(float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!float.IsFinite(presentationDeltaSeconds) ||
                presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (!m_Relevant || presentationDeltaSeconds == 0f)
                return;
            SetRawClock(m_RawContinuousTime + presentationDeltaSeconds);
        }

        internal void SetSynchronizedTime(double continuousTime)
        {
            RequireAlive();
            RequireOpenFrame();
            if (continuousTime < m_ContinuousTime)
            {
                m_HasCompletedFrame = false;
                m_ContinuityIdentity = AllocateContinuityIdentity();
                m_ResetSequence = AllocateResetSequence();
                m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            }
            SetClock(continuousTime);
        }

        internal void AnchorSynchronizedTime()
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' is not relevant.");
            m_ContinuationAnchorRawTime = m_RawContinuousTime;
            m_ContinuationAnchorEffectiveTime = m_ContinuousTime;
            m_HasContinuationAnchor = true;
        }

        internal void BeginFrame(ulong completionIdentity)
        {
            RequireAlive();
            RequireOpenFrame();
            m_SourceWorkspace.BeginFrame(completionIdentity);
        }

        internal void CommitSourceFrame()
        {
            RequireAlive();
            if (m_SourceWorkspace.HasOpenFrame)
                m_SourceWorkspace.CommitFrame(m_SourceWorkspace.CompletionIdentity);
        }

        internal void DiscardSourceFrame()
        {
            RequireAlive();
            if (m_SourceWorkspace.HasOpenFrame)
                m_SourceWorkspace.DiscardFrame(m_SourceWorkspace.CompletionIdentity);
        }

        internal AnimationPoseSourceCaptureBinding PrepareCapture(
            float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant || !m_ParameterFrame.IsValid)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' has no relevant parameter frame.");
            float rawX = m_ParameterFrame.Require(m_Plan.XAxis.ParameterId);
            float rawY = m_Plan.AxisCount == 2
                ? m_ParameterFrame.Require(m_Plan.YAxis.ParameterId)
                : 0f;
            float x = ApplyRange(
                rawX,
                m_Plan.XAxis,
                m_Descriptor.InputRangePolicy,
                NodeId);
            float y = m_Plan.AxisCount == 2
                ? ApplyRange(
                    rawY,
                    m_Plan.YAxis,
                    m_Descriptor.InputRangePolicy,
                    NodeId)
                : 0f;
            if (!CharacterAnimationBlendSpaceWeightEvaluator.Evaluate(
                    m_Solver,
                    x,
                    y,
                    m_Weights,
                    out CharacterAnimationBlendSpaceSolveFailure solveFailure))
            {
                throw new InvalidOperationException(
                    $"Blend Space '{m_Plan.PlanIdentity}' weight solve failed: {solveFailure}.");
            }
            if (!CharacterAnimationBlendSpacePhaseMapper.Map(
                    m_Phase,
                    m_ContinuousTime,
                    m_Cycle,
                    m_SampleRawTimes,
                    m_Times,
                    out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
                    out CharacterAnimationBlendSpacePhaseFailure phaseFailure))
            {
                throw new InvalidOperationException(
                    $"Blend Space '{m_Plan.PlanIdentity}' phase solve failed: {phaseFailure}.");
            }
            for (int i = 0; i < m_Weights.Count; i++)
                m_ActiveSampleIndices[i] = m_SampleIndices[m_Weights.GetSampleId(i)];
            WriteParameters(x, y);
            var left = new AnimationFootFeatureBlendAccumulator();
            var right = new AnimationFootFeatureBlendAccumulator();
            float footPlacementWeight = 0f;
            m_ClipSampleCount = 0;
            float nativePropertyWeight = 0f;
            for (int weightIndex = 0; weightIndex < m_Weights.Count; weightIndex++)
            {
                int sampleIndex = m_ActiveSampleIndices[weightIndex];
                float weight = m_Weights.GetWeight(weightIndex);
                CharacterAnimationBlendSpaceSamplePlan sample =
                    m_Plan.Samples[sampleIndex];
                CharacterAnimationBlendSpaceSampleTime time =
                    m_Times.Get(sampleIndex);
                bool looping = sample.IsLooping;
                double continuousClipTime = time.RawContinuousTime;
                m_ClipSamples[m_ClipSampleCount++] = sample.IsAcl
                    ? new ClipSamplePlan(
                        sampleIndex,
                        sample.SampleId,
                        sample.ResourceCatalogIndex,
                        sample.GroupClipIndex,
                        sample.SourceDurationSeconds,
                        time.ClipTime,
                        continuousClipTime,
                        time.NormalizedTime,
                        weight,
                        looping)
                    : new ClipSamplePlan(
                        sampleIndex,
                        sample.SampleId,
                        sample.Clip,
                        time.ClipTime,
                        continuousClipTime,
                        time.NormalizedTime,
                        weight,
                        looping);
                footPlacementWeight += sample.SampleFootPlacementWeight(time.NormalizedTime) * weight;
                if (sample.HasFootFeatures)
                {
                    ulong predictionSourceIdentity = AnimationPredictedFootStepSample.SourceIdentity(
                        ActiveState.PredictionSourceIdentity, sample.SampleId.Value);
                    left.Add(
                        sample.LeftFootFeatures.SamplePrepared(time.NormalizedTime).BindPredictionSource(
                            predictionSourceIdentity,
                            time.Cycle),
                        weight,
                        1f);
                    right.Add(
                        sample.RightFootFeatures.SamplePrepared(time.NormalizedTime).BindPredictionSource(
                            predictionSourceIdentity,
                            time.Cycle),
                        weight,
                        1f);
                }
                if (!sample.IsAcl)
                    nativePropertyWeight += weight;
            }
            if (m_ClipSampleCount == 0)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' produced no active samples.");
            m_Parameters[m_FootPlacementWeightParameterIndex] = Mathf.Clamp01(footPlacementWeight);
            m_ParameterAvailability[m_FootPlacementWeightParameterIndex] = 1;
            bool resetNativeProperties = true;
            for (int sampleIndex = 0; sampleIndex < m_ClipSampleCount; sampleIndex++)
            {
                ClipSamplePlan clipSample = m_ClipSamples[sampleIndex];
                CharacterAnimationBlendSpaceSamplePlan sample =
                    m_Plan.Samples[clipSample.ClipBindingIndex];
                sample.SampleNativeProperties(
                    clipSample.NormalizedTime,
                    clipSample.IsAcl || nativePropertyWeight <= 0f
                        ? 0f
                        : clipSample.Weight / nativePropertyWeight,
                    m_Parameters,
                    m_ParameterAvailability,
                    resetNativeProperties);
                if (!clipSample.IsAcl && sample.ScalarPage != null)
                    resetNativeProperties = false;
            }
            bool hasFootFeatures =
                m_Plan.Samples[0].HasFootFeatures;
            m_RawX = rawX;
            m_RawY = rawY;
            m_X = x;
            m_Y = y;
            m_CanonicalPhase = canonicalPhase;
            m_HasFootFeatures = hasFootFeatures;
            AnimationPoseSourceCaptureBinding capture =
                m_SourceWorkspace.PrepareCapture(
                    m_SourceId,
                    m_ContinuityIdentity,
                    PlayerIndex,
                    1f,
                    new AnimationReadOnlyBuffer<float>(
                        m_Parameters,
                        0,
                        m_Parameters.Length),
                    new AnimationReadOnlyBuffer<byte>(
                        m_ParameterAvailability,
                        0,
                        m_ParameterAvailability.Length),
                    hasFootFeatures ? left.Resolve() : default,
                    hasFootFeatures ? right.Resolve() : default,
                    hasFootFeatures,
                    presentationDeltaSeconds);
            m_SourceRetained = true;
            return capture;
        }

        internal AnimationSelectedPosePlayerJob PrepareJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex,
            in CharacterPoseSourceScalarReadView scalarReadView)
        {
            RequireAlive();
            RequireOpenFrame();
            return new AnimationSelectedPosePlayerJob(
                m_SourceWorkspace.RequireNativeReadBinding(completionIdentity),
                in output,
                physicalSource,
                sourceIndex,
                in scalarReadView,
                m_ContinuityIdentity,
                BuildDiscontinuity(completionIdentity),
                m_Relevant
                    ? AnimationSelectionAvailabilityPolicy.RequireSelection
                    : AnimationSelectionAvailabilityPolicy.AllowEmpty,
                m_Relevant,
                !m_Relevant);
        }

        internal void CompleteFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            CommitSourceFrame();
            m_HasCompletedFrame = m_Relevant;
            if (m_Relevant)
                m_PendingResetReason = PoseDiscontinuityResetReason.None;
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            RequireClosedFrame();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            ReleaseRetainedSource();
            m_Relevant = false;
            m_SourceRetained = false;
            m_HasCompletedFrame = false;
            m_SourceId = default;
            m_Endpoint = default;
            m_ParameterFrame = default;
            m_ContinuityIdentity =
                AllocateContinuityIdentity();
            m_ResetSequence = AllocateResetSequence();
            m_PendingResetReason = reason;
            ClearContinuationAnchor();
            SetRawClock(0d);
            m_SourceWorkspace.ResetContinuity();
        }

        internal void ResetForStateEntry()
        {
            RequireAlive();
            ClearContinuationAnchor();
            SetRawClock(0d);
            m_HasCompletedFrame = false;
            m_ContinuityIdentity =
                AllocateContinuityIdentity();
            m_ResetSequence = AllocateResetSequence();
            m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            if (!m_FrameOpen)
                m_SourceWorkspace.ResetContinuity();
        }

        internal int PendingReleaseCount
        {
            get
            {
                RequireAlive();
                return m_Releases.Count;
            }
        }

        internal AnimationPlayerReleaseToken PrepareRelease(
            int releaseOrdinal)
        {
            RequireAlive();
            AnimationPoseSourceId sourceId =
                m_Releases.PrepareRelease(releaseOrdinal);
            try
            {
                AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                    m_SourceWorkspace.PrepareRelease(sourceId);
                return new AnimationPlayerReleaseToken(
                    releaseOrdinal,
                    sourceId,
                    in sourcePoseRelease);
            }
            catch
            {
                m_Releases.CancelPreparedRelease(releaseOrdinal);
                throw;
            }
        }

        internal void ApplyPreparedRelease(
            in AnimationPlayerReleaseToken token)
        {
            AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                token.SourcePoseRelease;
            m_SourceWorkspace.ApplyPreparedRelease(
                in sourcePoseRelease);
            m_Releases.ApplyPreparedRelease(token.ReleaseOrdinal);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Releases.Clear();
            m_SourceWorkspace.Dispose();
        }

        void WriteParameters(float x, float y)
        {
            for (int parameterIndex = 0;
                 parameterIndex < m_Parameters.Length;
                 parameterIndex++)
            {
                float value;
                bool available;
                if (parameterIndex == m_Descriptor.XParameterIndex)
                {
                    value = x;
                    available = true;
                }
                else if (parameterIndex == m_Descriptor.YParameterIndex)
                {
                    value = y;
                    available = true;
                }
                else
                {
                    if (m_ParameterUsages[parameterIndex] == CharacterPoseParameterUsage.AnimatedProperty)
                    {
                        m_ParameterAvailability[parameterIndex] = 1;
                        continue;
                    }
                    if (parameterIndex == m_FootPlacementWeightParameterIndex)
                    {
                        m_Parameters[parameterIndex] = 0f;
                        m_ParameterAvailability[parameterIndex] = 1;
                        continue;
                    }
                    available = TryResolveSourceParameter(parameterIndex, out value);
                }
                m_Parameters[parameterIndex] = value;
                m_ParameterAvailability[parameterIndex] =
                    available ? (byte)1 : (byte)0;
            }
        }

        bool TryResolveSourceParameter(
            int parameterIndex,
            out float result)
        {
            SourceParameterBinding binding = m_SourceParameters[parameterIndex];
            CharacterAnimationBlendSpaceParameterPolicy policy = binding.Policy;
            if (policy == CharacterAnimationBlendSpaceParameterPolicy.Unavailable)
            {
                result = 0f;
                return false;
            }
            float weighted = 0f;
            float availableWeight = 0f;
            for (int i = 0; i < m_Weights.Count; i++)
            {
                int sampleIndex = m_ActiveSampleIndices[i];
                float? sampleValue = binding.Values[sampleIndex];
                float weight = m_Weights.GetWeight(i);
                if (!sampleValue.HasValue)
                {
                    if (policy ==
                        CharacterAnimationBlendSpaceParameterPolicy.RequireAllSamplesWeighted)
                    {
                        throw new InvalidOperationException(
                            $"Blend Space '{m_Plan.PlanIdentity}' active Sample '{m_Plan.Samples[sampleIndex].SampleId}' has no Parameter '{m_ParameterIds[parameterIndex]}'.");
                    }
                    continue;
                }
                weighted += sampleValue.Value * weight;
                availableWeight += weight;
            }
            if (!float.IsFinite(weighted) ||
                !float.IsFinite(availableWeight) ||
                availableWeight <= 0f)
            {
                throw new InvalidOperationException(
                    $"Blend Space '{m_Plan.PlanIdentity}' cannot resolve Parameter '{m_ParameterIds[parameterIndex]}'.");
            }
            result = weighted / availableWeight;
            return true;
        }

        void SetClock(double continuousTime)
        {
            if (!double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            double duration = m_Plan.ClockDurationSeconds;
            m_ContinuousTime = continuousTime;
            m_Cycle = checked((int)Math.Floor(continuousTime / duration));
            m_SampleTime = (float)(continuousTime - m_Cycle * duration);
            if (m_SampleTime >= duration)
                m_SampleTime = 0f;
        }

        void SetRawClock(double continuousTime)
        {
            if (!double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            double effectiveTime = m_HasContinuationAnchor
                ? m_ContinuationAnchorEffectiveTime +
                  continuousTime -
                  m_ContinuationAnchorRawTime
                : continuousTime;
            if (!double.IsFinite(effectiveTime) || effectiveTime < 0d)
                throw new InvalidOperationException($"Blend Space Player '{NodeId}' continuation anchor produced an invalid time.");
            m_RawContinuousTime = continuousTime;
            SetClock(effectiveTime);
        }

        void ClearContinuationAnchor()
        {
            m_ContinuationAnchorRawTime = 0d;
            m_ContinuationAnchorEffectiveTime = 0d;
            m_HasContinuationAnchor = false;
        }

        void ReleaseRetainedSource()
        {
            if (!m_SourceRetained)
                return;
            m_Releases.Append(m_SourceId);
            m_SourceRetained = false;
        }

        ulong AllocateContinuityIdentity()
        {
            if (m_NextContinuityIdentity ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' continuity identity was exhausted.");
            }
            return m_NextContinuityIdentity++;
        }

        ulong AllocateResetSequence()
        {
            if (m_NextResetSequence == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' reset sequence was exhausted.");
            }
            return m_NextResetSequence++;
        }

        PoseDiscontinuity BuildDiscontinuity(ulong completionIdentity)
        {
            if (m_PendingResetReason == PoseDiscontinuityResetReason.None)
                return default;
            return PoseDiscontinuity.Reset(
                AllocateEventIdentity(),
                completionIdentity,
                m_Endpoint,
                m_ContinuityIdentity,
                m_PendingResetReason,
                m_ResetSequence,
                m_Relevant);
        }

        ulong AllocateEventIdentity()
        {
            if (m_NextEventIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' discontinuity identity was exhausted.");
            }
            return m_NextEventIdentity++;
        }

        readonly struct SourceParameterBinding
        {
            internal SourceParameterBinding(CharacterAnimationBlendSpacePlan plan, PoseParameterId parameterId)
            {
                if (!plan.TryGetParameterPolicy(parameterId, out CharacterAnimationBlendSpaceParameterPolicy policy))
                    throw new InvalidOperationException(
                        $"Blend Space '{plan.PlanIdentity}' has no policy for Pose Parameter '{parameterId}'.");
                Policy = policy;
                Values = policy == CharacterAnimationBlendSpaceParameterPolicy.Unavailable
                    ? Array.Empty<float?>()
                    : new float?[plan.Samples.Count];
                for (int i = 0; i < Values.Length; i++)
                    if (plan.Samples[i].TryGetParameter(parameterId, out float value))
                        Values[i] = value;
            }

            internal CharacterAnimationBlendSpaceParameterPolicy Policy { get; }
            internal float?[] Values { get; }
        }

        static float ApplyRange(
            float value,
            CharacterAnimationBlendSpaceAxisPlan axis,
            CharacterAnimationBlendSpaceInputRangePolicy policy,
            PoseNodeId nodeId)
        {
            if (!float.IsFinite(value) || axis == null)
                throw new InvalidOperationException(
                    $"Blend Space Player '{nodeId}' received an invalid axis value.");
            if (value >= axis.Minimum && value <= axis.Maximum)
                return value;
            if (policy == CharacterAnimationBlendSpaceInputRangePolicy.Clamp)
                return Mathf.Clamp(value, axis.Minimum, axis.Maximum);
            if (policy == CharacterAnimationBlendSpaceInputRangePolicy.Reject)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player '{nodeId}' Parameter '{axis.ParameterId}' value {value} is outside [{axis.Minimum}, {axis.Maximum}].");
            }
            throw new InvalidOperationException(
                $"Blend Space Player '{nodeId}' has an invalid input range policy.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationBlendSpacePlayerRuntime));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' frame is not open.");
        }

        void RequireClosedFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' frame must be closed.");
        }
    }
}
