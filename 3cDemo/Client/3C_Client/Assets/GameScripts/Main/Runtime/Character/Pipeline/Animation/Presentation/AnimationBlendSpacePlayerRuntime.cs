using System;
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
            internal CharacterPresentationProgramParameterFrame ParameterFrame;
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
        readonly AnimationBlendSourcePoseWorkspace m_SourceWorkspace;
        readonly AnimationFootAnalysisProjectionIdentity m_FootAnalysis;
        readonly PoseParameterId[] m_ParameterIds;
        readonly float[] m_Parameters;
        readonly byte[] m_ParameterAvailability;
        readonly ClipSamplePlan[] m_ClipSamples;
        readonly AnimationPlayerReleaseJournal m_Releases;
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

        CharacterPresentationProgramParameterFrame m_ParameterFrame { get => ActiveState.ParameterFrame; set => ActiveState.ParameterFrame = value; }
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
        AnimationPoseSourceId m_SourceId { get => ActiveState.SourceId; set => ActiveState.SourceId = value; }
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
            CharacterPresentationPosePlan posePlan,
            CharacterAnimationRigPayload rig,
            AnimationFootAnalysisProjectionIdentity footAnalysis)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (posePlan == null)
                throw new ArgumentNullException(nameof(posePlan));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            m_FootAnalysis = footAnalysis;
            m_Plan.RequireValid(footAnalysis != null && footAnalysis.IsEnabled);
            m_Solver = m_Plan.CreateSolverPlan();
            m_Phase = m_Plan.CreatePhasePlan();
            m_Weights = new CharacterAnimationBlendSpaceWeightPage(m_Plan.Samples.Count);
            m_Times = new CharacterAnimationBlendSpaceTimePage(m_Plan.Samples.Count);
            m_ParameterIds = new PoseParameterId[posePlan.Parameters.Count];
            m_Parameters = new float[posePlan.Parameters.Count];
            m_ParameterAvailability = new byte[posePlan.Parameters.Count];
            for (int i = 0; i < posePlan.Parameters.Count; i++)
                m_ParameterIds[i] = posePlan.Parameters[i].ParameterId;
            m_ClipSamples = new ClipSamplePlan[m_Plan.Samples.Count];
            m_SourceWorkspace =
                new AnimationBlendSourcePoseWorkspace(
                    rig,
                    posePlan.Parameters.Count,
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

        internal PoseNodeId NodeId => m_Descriptor.NodeId;
        internal int PlayerIndex => m_Descriptor.PlayerIndex;
        internal AnimationPoseSourceId SourceId => m_SourceId;
        internal bool IsRelevant => m_Relevant;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal float RemainingTime => float.MaxValue;
        internal AnimationMarkerSyncBinding MarkerSync => m_Plan.MarkerSync;
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
            in CharacterPresentationProgramParameterFrame parameterFrame)
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
                    m_Times,
                    out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
                    out CharacterAnimationBlendSpacePhaseFailure phaseFailure))
            {
                throw new InvalidOperationException(
                    $"Blend Space '{m_Plan.PlanIdentity}' phase solve failed: {phaseFailure}.");
            }
            WriteParameters(x, y);
            var left = new AnimationFootFeatureBlendAccumulator();
            var right = new AnimationFootFeatureBlendAccumulator();
            m_ClipSampleCount = 0;
            for (int weightIndex = 0; weightIndex < m_Weights.Count; weightIndex++)
            {
                CharacterAnimationBlendSpaceSampleId sampleId =
                    m_Weights.GetSampleId(weightIndex);
                float weight = m_Weights.GetWeight(weightIndex);
                CharacterAnimationBlendSpaceSamplePlan sample =
                    m_Plan.RequireSample(sampleId);
                CharacterAnimationBlendSpaceSampleTime time =
                    FindTime(m_Times, sampleId);
                int sampleIndex = FindSampleIndex(m_Plan, sampleId);
                bool looping =
                    sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
                double continuousClipTime = looping
                    ? m_Cycle * (double)sample.Clip.length + time.ClipTime
                    : time.ClipTime;
                m_ClipSamples[m_ClipSampleCount++] = new ClipSamplePlan(
                    sampleIndex,
                    sample.SampleId,
                    sample.Clip,
                    time.ClipTime,
                    continuousClipTime,
                    time.NormalizedTime,
                    weight,
                    looping);
                if (sample.HasFootFeatures)
                {
                    left.Add(
                        sample.LeftFootFeatures.Sample(time.NormalizedTime).BindPredictionSource(
                            AnimationPredictedFootStepSample.SourceIdentity(m_SourceId, sample.SampleId.Value),
                            time.Cycle),
                        weight,
                        1f);
                    right.Add(
                        sample.RightFootFeatures.Sample(time.NormalizedTime).BindPredictionSource(
                            AnimationPredictedFootStepSample.SourceIdentity(m_SourceId, sample.SampleId.Value),
                            time.Cycle),
                        weight,
                        1f);
                }
            }
            if (m_ClipSampleCount == 0)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' produced no active samples.");
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

        internal AnimationBlendSpacePlayerRuntimeSnapshot CreateDiagnosticsSnapshot(
            AnimationBlendSpaceSampleRuntimeSnapshot[] samples,
            ref int sampleCount)
        {
            RequireAlive();
            if (!m_HasCompletedFrame || !m_Relevant || samples == null)
                throw new InvalidOperationException(
                    $"Blend Space Player '{NodeId}' has no completed diagnostics frame.");
            if (sampleCount < 0 ||
                sampleCount + m_Weights.Count > samples.Length)
            {
                throw new InvalidOperationException(
                    "Blend Space diagnostics sample capacity was exceeded.");
            }
            int sampleOffset = sampleCount;
            for (int i = 0; i < m_Weights.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleId sampleId =
                    m_Weights.GetSampleId(i);
                CharacterAnimationBlendSpaceSamplePlan sample =
                    m_Plan.RequireSample(sampleId);
                CharacterAnimationBlendSpaceSampleTime time =
                    FindTime(m_Times, sampleId);
                bool hasFootFeatures = sample.HasFootFeatures;
                samples[sampleCount++] =
                    new AnimationBlendSpaceSampleRuntimeSnapshot(
                        sampleId,
                        m_Weights.GetWeight(i),
                        time.ClipTime,
                        time.NormalizedTime,
                        hasFootFeatures,
                        hasFootFeatures
                            ? m_FootAnalysis.AnalysisSourceId
                            : string.Empty,
                        hasFootFeatures
                            ? m_FootAnalysis.AnalysisVersion
                            : 0,
                        hasFootFeatures
                            ? m_FootAnalysis.ArtifactContentHash
                            : string.Empty,
                        hasFootFeatures
                            ? sample.LeftFootFeatures.Sample(
                                time.NormalizedTime)
                            : default,
                        hasFootFeatures
                            ? sample.RightFootFeatures.Sample(
                                time.NormalizedTime)
                            : default);
            }
            return new AnimationBlendSpacePlayerRuntimeSnapshot(
                NodeId,
                m_Descriptor.PresentationPoseSourceIndex,
                m_SourceId,
                m_Plan.BlendSpaceId,
                m_Plan.ContentRevision,
                m_Plan.Mode,
                m_RawX,
                m_RawY,
                m_X,
                m_Y,
                m_CanonicalPhase,
                m_HasFootFeatures,
                sampleOffset,
                m_Weights.Count);
        }

        internal AnimationSelectedPosePlayerJob PrepareJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex)
        {
            RequireAlive();
            RequireOpenFrame();
            return new AnimationSelectedPosePlayerJob(
                m_SourceWorkspace.RequireNativeReadBinding(completionIdentity),
                in output,
                physicalSource,
                sourceIndex,
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
                    available = TryResolveSourceParameter(
                        m_Plan,
                        m_Weights,
                        m_Descriptor,
                        parameterIndex,
                        out value);
                }
                m_Parameters[parameterIndex] = value;
                m_ParameterAvailability[parameterIndex] =
                    available ? (byte)1 : (byte)0;
            }
        }

        bool TryResolveSourceParameter(
            CharacterAnimationBlendSpacePlan plan,
            CharacterAnimationBlendSpaceWeightPage weights,
            CharacterAnimationBlendSpacePlayerPlan player,
            int parameterIndex,
            out float result)
        {
            PoseParameterId parameterId =
                parameterIndex == player.XParameterIndex
                    ? plan.XAxis.ParameterId
                    : parameterIndex == player.YParameterIndex
                        ? plan.YAxis.ParameterId
                        : default;
            if (!parameterId.IsValid)
                parameterId = m_ParameterIds[parameterIndex];
            if (!plan.TryGetParameterPolicy(
                    parameterId,
                    out CharacterAnimationBlendSpaceParameterPolicy policy))
            {
                throw new InvalidOperationException(
                    $"Blend Space '{plan.PlanIdentity}' has no policy for Pose Parameter '{parameterId}'.");
            }
            if (policy == CharacterAnimationBlendSpaceParameterPolicy.Unavailable)
            {
                result = 0f;
                return false;
            }
            float weighted = 0f;
            float availableWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                CharacterAnimationBlendSpaceSamplePlan sample =
                    plan.RequireSample(weights.GetSampleId(i));
                float weight = weights.GetWeight(i);
                if (!sample.TryGetParameter(parameterId, out float value))
                {
                    if (policy ==
                        CharacterAnimationBlendSpaceParameterPolicy.RequireAllSamplesWeighted)
                    {
                        throw new InvalidOperationException(
                            $"Blend Space '{plan.PlanIdentity}' active Sample '{sample.SampleId}' has no Parameter '{parameterId}'.");
                    }
                    continue;
                }
                weighted += value * weight;
                availableWeight += weight;
            }
            if (!float.IsFinite(weighted) ||
                !float.IsFinite(availableWeight) ||
                availableWeight <= 0f)
            {
                throw new InvalidOperationException(
                    $"Blend Space '{plan.PlanIdentity}' cannot resolve Parameter '{parameterId}'.");
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

        static int FindSampleIndex(
            CharacterAnimationBlendSpacePlan plan,
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < plan.Samples.Count; i++)
            {
                if (plan.Samples[i].SampleId.Equals(sampleId))
                    return i;
            }
            throw new InvalidOperationException(
                $"Blend Space '{plan.PlanIdentity}' weight references unknown Sample '{sampleId}'.");
        }

        static CharacterAnimationBlendSpaceSampleTime FindTime(
            CharacterAnimationBlendSpaceTimePage times,
            CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < times.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleTime time = times.Get(i);
                if (time.SampleId.Equals(sampleId))
                    return time;
            }
            throw new InvalidOperationException(
                $"Blend Space time page has no Sample '{sampleId}'.");
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
