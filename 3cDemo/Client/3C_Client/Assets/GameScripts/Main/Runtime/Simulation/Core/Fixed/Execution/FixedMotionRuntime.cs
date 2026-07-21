using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct ResolvedGameplayMotion
    {
        public ResolvedGameplayMotion(FixedVector3 displacement, FixedScalar yawDegrees, bool hasMotion)
        {
            Displacement = displacement;
            YawDegrees = yawDegrees;
            HasMotion = hasMotion;
        }

        public FixedVector3 Displacement { get; }
        public FixedScalar YawDegrees { get; }
        public bool HasMotion { get; }
    }

    internal enum SimulationMotionChannel
    {
        Locomotion = 0,
        Action = 1,
        GameplayResult = 2
    }

    internal enum SimulationMotionBlendMode
    {
        Additive = 0,
        WeightedBlend = 1,
        Override = 2
    }

    internal enum SimulationMotionContributionSpace
    {
        ActorLocal = 0,
        World = 1
    }

    internal readonly struct SimulationMotionContribution
    {
        public SimulationMotionContribution(
            string sourceIdentity,
            OperationHandle sourceOperation,
            FixedVector3 displacement,
            FixedScalar yawDegrees,
            SimulationMotionContributionSpace space,
            FixedScalar weight,
            int priority,
            SimulationMotionChannel channel,
            SimulationMotionBlendMode blendMode,
            bool consumeLowerChannels)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (!sourceOperation.IsValid)
                throw new ArgumentException("Motion contribution source operation is invalid.", nameof(sourceOperation));
            SourceOperation = sourceOperation;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            Space = space;
            Weight = FixedScalar.Clamp(weight, FixedScalar.Zero, FixedScalar.One);
            Priority = priority;
            Channel = channel;
            BlendMode = blendMode;
            ConsumeLowerChannels = consumeLowerChannels;
        }

        public string SourceIdentity { get; }
        public OperationHandle SourceOperation { get; }
        public FixedVector3 Displacement { get; }
        public FixedScalar YawDegrees { get; }
        public SimulationMotionContributionSpace Space { get; }
        public FixedScalar Weight { get; }
        public int Priority { get; }
        public SimulationMotionChannel Channel { get; }
        public SimulationMotionBlendMode BlendMode { get; }
        public bool ConsumeLowerChannels { get; }
        public bool HasDelta => Weight > FixedScalar.Zero &&
            (Displacement != FixedVector3.Zero || YawDegrees != FixedScalar.Zero);
        public bool ClaimsLowerChannels => Weight > FixedScalar.Zero &&
            BlendMode == SimulationMotionBlendMode.Override &&
            ConsumeLowerChannels;
        public bool CanResolve => HasDelta || ClaimsLowerChannels;
    }

    internal struct ResolvedMotionChannel
    {
        public ResolvedMotionChannel(
            SimulationMotionChannel channel,
            FixedVector3 displacement,
            FixedScalar yawDegrees,
            bool hasContribution,
            bool claimsLowerChannels,
            OperationHandle resolvedOwnerOperation,
            string resolvedOwnerIdentity,
            FixedVector3 resolvedOwnerDisplacement,
            FixedScalar resolvedOwnerYawDegrees,
            OperationHandle traceOperation,
            int participatingSourceCount,
            ulong participatingSourceFingerprint)
        {
            Channel = channel;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            HasContribution = hasContribution;
            ClaimsLowerChannels = claimsLowerChannels;
            ResolvedOwnerOperation = resolvedOwnerOperation;
            ResolvedOwnerIdentity = resolvedOwnerIdentity ?? string.Empty;
            ResolvedOwnerDisplacement = resolvedOwnerDisplacement;
            ResolvedOwnerYawDegrees = resolvedOwnerYawDegrees;
            TraceOperation = traceOperation;
            ParticipatingSourceCount = participatingSourceCount;
            ParticipatingSourceFingerprint = participatingSourceFingerprint;
        }

        public SimulationMotionChannel Channel { get; }
        public FixedVector3 Displacement { get; private set; }
        public FixedScalar YawDegrees { get; private set; }
        public bool HasContribution { get; }
        public bool ClaimsLowerChannels { get; }
        public OperationHandle ResolvedOwnerOperation { get; }
        public string ResolvedOwnerIdentity { get; }
        public FixedVector3 ResolvedOwnerDisplacement { get; }
        public FixedScalar ResolvedOwnerYawDegrees { get; }
        public OperationHandle TraceOperation { get; }
        public int ParticipatingSourceCount { get; }
        public ulong ParticipatingSourceFingerprint { get; }
        public bool HasDelta => Displacement != FixedVector3.Zero || YawDegrees != FixedScalar.Zero;

        public void ApplyCorrection(FixedVector3 displacement, FixedScalar yawDegrees)
        {
            Displacement += displacement;
            YawDegrees += yawDegrees;
        }
    }

    internal sealed class FixedMotionAccumulator : FixedOperationModule,
        IFixedMotionContributionSink,
        IFixedMotionModifierSampleSink
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly List<SimulationMotionContribution> m_Contributions;
        readonly List<MotionWarpSample<FixedScalar, FixedActionInstanceState>> m_WarpSamples;
        readonly FixedMotionWarpTarget m_MotionWarp;

        public FixedMotionAccumulator(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            FixedStatePort modifierState,
            List<SimulationMotionContribution> contributions,
            List<MotionWarpSample<FixedScalar, FixedActionInstanceState>> warpSamples)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
            m_WarpSamples = warpSamples ?? throw new ArgumentNullException(nameof(warpSamples));
            m_MotionWarp = new FixedMotionWarpTarget(access, frame, modifierState);
        }

        public void Submit(SimulationMotionContribution contribution)
        {
            if (!contribution.CanResolve)
                return;
            m_Contributions.Add(contribution);
            if (m_Frame.Trace.Enabled)
            {
                m_Frame.Trace.Add(
                    Access.Operation(contribution.SourceOperation),
                    "motion_contribution",
                    SimulationTraceSeverity.Detail,
                    $"channel={contribution.Channel};blend={contribution.BlendMode};priority={contribution.Priority};weight={contribution.Weight};delta={contribution.Displacement};yaw={contribution.YawDegrees};claim={contribution.ClaimsLowerChannels}");
            }
        }

        public void Submit(MotionWarpSample<FixedScalar, FixedActionInstanceState> sample) => m_WarpSamples.Add(sample);

        public ResolvedGameplayMotion Resolve()
        {
            ResolvedMotionChannel locomotion = ResolveChannel(SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = ResolveChannel(SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = ResolveChannel(SimulationMotionChannel.GameplayResult);

            ProgramMotionModifierRuntime.ApplyActionWarp<FixedScalar, FixedActionInstanceState, ResolvedMotionChannel, FixedMotionWarpTarget>(
                m_Layout.MotionModifiers(ProgramMotionModifierChannel.Action),
                m_WarpSamples,
                action.ResolvedOwnerOperation,
                ref action,
                m_MotionWarp);
            RequireNoUnsupportedModifiers(ProgramMotionModifierChannel.GameplayResult);

            FixedVector3 displacement = FixedVector3.Zero;
            FixedScalar yaw = FixedScalar.Zero;
            Compose(locomotion, ref displacement, ref yaw);
            Compose(action, ref displacement, ref yaw);
            Compose(gameplayResult, ref displacement, ref yaw);

            bool hasMotion = displacement != FixedVector3.Zero || yaw != FixedScalar.Zero;
            var motion = new ResolvedGameplayMotion(displacement, yaw, hasMotion);
            TraceResolvedGameplayMotion(motion, action);
            return motion;
        }

        ResolvedMotionChannel ResolveChannel(SimulationMotionChannel channel)
        {
            FixedVector3 additiveDisplacement = FixedVector3.Zero;
            FixedScalar additiveYaw = FixedScalar.Zero;
            FixedVector3 weightedDisplacement = FixedVector3.Zero;
            FixedScalar weightedYaw = FixedScalar.Zero;
            FixedScalar totalWeight = FixedScalar.Zero;
            SimulationMotionContribution overrideWinner = default;
            FixedVector3 overrideDisplacement = FixedVector3.Zero;
            FixedScalar overrideYaw = FixedScalar.Zero;
            OperationHandle traceOperation = OperationHandle.Invalid;
            int sourceCount = 0;
            ulong sourceFingerprint = 1469598103934665603UL;
            bool hasAdditive = false;
            bool hasWeighted = false;
            bool hasOverride = false;
            for (int i = 0; i < m_Contributions.Count; i++)
            {
                SimulationMotionContribution contribution = m_Contributions[i];
                if (contribution.Channel != channel || !contribution.CanResolve)
                    continue;
                if (!traceOperation.IsValid)
                    traceOperation = contribution.SourceOperation;
                sourceCount++;
                sourceFingerprint = MixSource(sourceFingerprint, contribution.SourceOperation.Value);
                FixedVector3 resolved = contribution.Space == SimulationMotionContributionSpace.ActorLocal
                    ? FixedAngle.RotatePlanar(contribution.Displacement, m_Frame.Body.Yaw)
                    : contribution.Displacement;
                switch (contribution.BlendMode)
                {
                    case SimulationMotionBlendMode.Additive:
                        additiveDisplacement += resolved * contribution.Weight;
                        additiveYaw += contribution.YawDegrees * contribution.Weight;
                        hasAdditive = true;
                        break;
                    case SimulationMotionBlendMode.WeightedBlend:
                        weightedDisplacement += resolved * contribution.Weight;
                        weightedYaw += contribution.YawDegrees * contribution.Weight;
                        totalWeight += contribution.Weight;
                        hasWeighted = true;
                        break;
                    case SimulationMotionBlendMode.Override:
                        if (!hasOverride || contribution.Priority > overrideWinner.Priority)
                        {
                            overrideWinner = contribution;
                            overrideDisplacement = resolved * contribution.Weight;
                            overrideYaw = contribution.YawDegrees * contribution.Weight;
                            hasOverride = true;
                        }
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Motion contribution '{contribution.SourceIdentity}' has invalid blend mode '{contribution.BlendMode}'.");
                }
            }
            if (!hasAdditive && !hasWeighted && !hasOverride)
                return new ResolvedMotionChannel(channel, FixedVector3.Zero, FixedScalar.Zero, false, false, OperationHandle.Invalid, string.Empty, FixedVector3.Zero, FixedScalar.Zero, OperationHandle.Invalid, 0, 0);

            FixedVector3 channelDisplacement = additiveDisplacement;
            FixedScalar channelYaw = additiveYaw;
            if (hasOverride)
            {
                channelDisplacement += overrideDisplacement;
                channelYaw += overrideYaw;
            }
            else if (hasWeighted && totalWeight > FixedScalar.Zero)
            {
                channelDisplacement += new FixedVector3(
                    weightedDisplacement.X / totalWeight,
                    weightedDisplacement.Y / totalWeight,
                    weightedDisplacement.Z / totalWeight);
                channelYaw += weightedYaw / totalWeight;
            }
            var result = new ResolvedMotionChannel(
                channel,
                channelDisplacement,
                channelYaw,
                true,
                hasOverride && overrideWinner.ConsumeLowerChannels,
                hasOverride ? overrideWinner.SourceOperation : OperationHandle.Invalid,
                hasOverride ? overrideWinner.SourceIdentity : string.Empty,
                hasOverride ? overrideDisplacement : FixedVector3.Zero,
                hasOverride ? overrideYaw : FixedScalar.Zero,
                hasOverride ? overrideWinner.SourceOperation : traceOperation,
                sourceCount,
                sourceFingerprint);
            TraceChannel(result);
            return result;
        }

        void RequireNoUnsupportedModifiers(ProgramMotionModifierChannel channel)
        {
            if (m_Layout.MotionModifiers(channel).Length != 0)
                throw new InvalidOperationException($"Program contains unsupported '{channel}' Motion Modifiers.");
        }

        static void Compose(
            ResolvedMotionChannel channel,
            ref FixedVector3 displacement,
            ref FixedScalar yaw)
        {
            if (!channel.HasContribution)
                return;
            if (channel.ClaimsLowerChannels)
            {
                displacement = channel.Displacement;
                yaw = channel.YawDegrees;
                return;
            }
            displacement += channel.Displacement;
            yaw += channel.YawDegrees;
        }

        void TraceChannel(ResolvedMotionChannel channel)
        {
            if (!m_Frame.Trace.Enabled || !channel.TraceOperation.IsValid)
                return;
            m_Frame.Trace.Add(
                Access.Operation(channel.TraceOperation),
                "motion_channel_resolved",
                SimulationTraceSeverity.Detail,
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16}");
        }

        void TraceResolvedGameplayMotion(ResolvedGameplayMotion motion, ResolvedMotionChannel action)
        {
            if (!m_Frame.Trace.Enabled)
                return;
            OperationHandle operation = action.TraceOperation.IsValid ? action.TraceOperation : m_Frame.Layout.RootOperation;
            if (!operation.IsValid)
                operation = m_Layout.RootOperation;
            m_Frame.Trace.Add(
                Access.Operation(operation),
                "resolved_gameplay_motion",
                SimulationTraceSeverity.Information,
                $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion}");
        }

        static ulong MixSource(ulong hash, int operation)
        {
            unchecked
            {
                hash ^= (uint)operation;
                return hash * 1099511628211UL;
            }
        }

    }

    internal sealed class FixedMotionWarpTarget : FixedOperationModule,
        IMotionModifierTarget<FixedScalar, FixedActionInstanceState, ResolvedMotionChannel>
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedStatePort m_State;

        public FixedMotionWarpTarget(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            FixedStatePort state)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Reset(ProgramMotionModifierDescriptor descriptor)
        {
            for (int i = 0; i < descriptor.StateSlotCount; i++)
                m_State.Reset(descriptor.StateSlotStart + i);
        }

        public void TraceSourceNotResolved(ProgramMotionModifierDescriptor descriptor, OperationHandle resolvedOwner)
        {
            Trace(
                descriptor,
                MotionModifierDiagnosticCode.SourceNotResolved,
                SimulationTraceSeverity.Detail,
                $"source={descriptor.SourceMotionOperation};resolvedOwner={resolvedOwner}");
        }

        public void ApplyMotionWarp(
            ProgramMotionModifierDescriptor descriptor,
            MotionWarpSample<FixedScalar, FixedActionInstanceState> sample,
            ref ResolvedMotionChannel channel)
        {
            FixedActionInstanceState action = sample.Action;
            var currentAction = new TimelineActionContextIdentity(
                action.ActionId,
                action.ContextId,
                action.InstanceId,
                action.PredictionKey);
            if (!action.IsActive ||
                !currentAction.Equals(sample.ActionContext) ||
                !string.Equals(action.ContextId, descriptor.ActionContextIdentity, StringComparison.Ordinal))
            {
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"MotionWarp sample does not match Action Context '{descriptor.ActionContextIdentity}'.");
            }
            if (!action.TargetSnapshot.HasTarget)
            {
                ActionTargetRequirement requirement = Access.Services.RequireActionProfile(action.ActionId).TargetRequirement;
                if (requirement == ActionTargetRequirement.OptionalSnapshot)
                {
                    Reset(descriptor);
                    Trace(
                        descriptor,
                        MotionModifierDiagnosticCode.NoTargetByOptionalPolicy,
                        SimulationTraceSeverity.Information,
                        $"action={action.ActionId}:{action.InstanceId};source={descriptor.SourceMotionOperation};requirement={requirement}");
                    return;
                }
                Fail(MotionModifierDiagnosticCode.TargetSnapshotRequired, descriptor, $"Action '{action.ActionId}' has no immutable target snapshot for requirement '{requirement}'.");
            }

            bool active = Read(descriptor, ProgramStateSemantic.MotionWarpActive).Boolean;
            bool initialized = Read(descriptor, ProgramStateSemantic.MotionWarpInitialized).Boolean;
            FixedActionInstanceReference storedReference = Read(descriptor, ProgramStateSemantic.MotionWarpActionInstance).ActionInstanceReference;
            var storedAction = storedReference.IsValid
                ? new TimelineActionContextIdentity(storedReference.ActionId, storedReference.ContextId, storedReference.InstanceId, storedReference.PredictionKey)
                : default;
            MotionWarpLifecycleDecision lifecycle;
            try
            {
                lifecycle = MotionWarpRuntimeSemantics.ResolveLifecycle(
                    active,
                    initialized,
                    Read(descriptor, ProgramStateSemantic.MotionWarpPlaybackGeneration).UInt64,
                    storedAction,
                    Read(descriptor, ProgramStateSemantic.MotionWarpSourceOperation).Int32,
                    sample.PlaybackGeneration,
                    currentAction,
                    descriptor.SourceMotionOperation);
            }
            catch (InvalidOperationException exception)
            {
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, exception.Message);
                return;
            }

            FixedScalar previousProgress = NormalizeWindowTime(descriptor, sample.Segment.Previous);
            FixedScalar currentProgress = NormalizeWindowTime(descriptor, sample.Segment.Current);
            FixedScalar previousPositionProgress;
            FixedScalar previousYawProgress;
            FixedVector3 startBodyPosition;
            FixedYaw startBodyYaw;
            FixedVector3 sourceWindowStartPosition;
            FixedScalar sourceWindowStartYaw;
            FixedVector3 resolvedTargetPosition;
            FixedYaw resolvedTargetYaw;
            ProgramMotionWarpLimitResult limitResult;
            FixedVector3 previousWarpedPosition;
            FixedYaw previousWarpedYaw;

            if (lifecycle == MotionWarpLifecycleDecision.Initialize)
            {
                if (!InitializeWarp(
                        descriptor,
                        action.TargetSnapshot,
                        out startBodyPosition,
                        out startBodyYaw,
                        out sourceWindowStartPosition,
                        out sourceWindowStartYaw,
                        out resolvedTargetPosition,
                        out resolvedTargetYaw,
                        out limitResult))
                {
                    Reset(descriptor);
                    Trace(
                        descriptor,
                        MotionModifierDiagnosticCode.PreservedByLimitPolicy,
                        SimulationTraceSeverity.Information,
                        $"source={descriptor.SourceMotionOperation};action={action.InstanceId};target={action.TargetSnapshot.TargetId};policy={descriptor.LimitPolicy}");
                    return;
                }
                previousPositionProgress = PositionProgress(descriptor, previousProgress);
                previousYawProgress = YawProgress(descriptor, previousProgress);
                EvaluateWarpPose(
                    descriptor,
                    sample.Segment.Previous,
                    previousPositionProgress,
                    previousYawProgress,
                    startBodyPosition,
                    startBodyYaw,
                    sourceWindowStartPosition,
                    sourceWindowStartYaw,
                    resolvedTargetPosition,
                    resolvedTargetYaw,
                    out previousWarpedPosition,
                    out previousWarpedYaw,
                    out _,
                    out _);
                WriteInitialized(
                    descriptor,
                    sample.PlaybackGeneration,
                    action,
                    startBodyPosition,
                    startBodyYaw,
                    sourceWindowStartPosition,
                    sourceWindowStartYaw,
                    resolvedTargetPosition,
                    resolvedTargetYaw,
                    limitResult,
                    previousWarpedPosition,
                    previousWarpedYaw,
                    previousPositionProgress,
                    previousYawProgress);
            }
            else
            {
                startBodyPosition = Read(descriptor, ProgramStateSemantic.MotionWarpStartBodyPosition).Vector3;
                startBodyYaw = Read(descriptor, ProgramStateSemantic.MotionWarpStartBodyYaw).Yaw;
                sourceWindowStartPosition = Read(descriptor, ProgramStateSemantic.MotionWarpSourceWindowStartPosition).Vector3;
                sourceWindowStartYaw = Read(descriptor, ProgramStateSemantic.MotionWarpSourceWindowStartYaw).Scalar;
                resolvedTargetPosition = Read(descriptor, ProgramStateSemantic.MotionWarpResolvedTargetPosition).Vector3;
                resolvedTargetYaw = Read(descriptor, ProgramStateSemantic.MotionWarpResolvedTargetYaw).Yaw;
                limitResult = (ProgramMotionWarpLimitResult)Read(descriptor, ProgramStateSemantic.MotionWarpLimitResult).Int32;
                if (!Enum.IsDefined(typeof(ProgramMotionWarpLimitResult), limitResult) || limitResult == ProgramMotionWarpLimitResult.PreservedByLimitPolicy)
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Restored MotionWarp limit result '{limitResult}' is invalid for active state.");
                previousWarpedPosition = Read(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedPosition).Vector3;
                previousWarpedYaw = Read(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedYaw).Yaw;
                previousPositionProgress = Read(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress).Scalar;
                previousYawProgress = Read(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress).Scalar;
                RequireProgress(previousPositionProgress, descriptor, "position");
                RequireProgress(previousYawProgress, descriptor, "yaw");
            }

            FixedScalar positionProgress = PositionProgress(descriptor, currentProgress);
            FixedScalar yawProgress = YawProgress(descriptor, currentProgress);
            if (positionProgress < previousPositionProgress || yawProgress < previousYawProgress)
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, "Cumulative MotionWarp progress moved backwards within one playback generation.");
            EvaluateWarpPose(
                descriptor,
                sample.Segment.Current,
                positionProgress,
                yawProgress,
                startBodyPosition,
                startBodyYaw,
                sourceWindowStartPosition,
                sourceWindowStartYaw,
                resolvedTargetPosition,
                resolvedTargetYaw,
                out FixedVector3 currentWarpedPosition,
                out FixedYaw currentWarpedYaw,
                out FixedVector3 currentSourceRelative,
                out FixedScalar currentSourceYawRelative);
            FixedVector3 warpedSourceDelta = currentWarpedPosition - previousWarpedPosition;
            FixedScalar warpedSourceYawDelta = FixedAngle.Delta(previousWarpedYaw, currentWarpedYaw);
            SourceDeltaAtSegment(descriptor, sample.Segment, out FixedVector3 rawSourceDelta, out FixedScalar rawSourceYawDelta);
            FixedVector3 modifierPositionCorrection = warpedSourceDelta - rawSourceDelta;
            FixedScalar modifierYawCorrection = warpedSourceYawDelta - rawSourceYawDelta;
            channel.ApplyCorrection(modifierPositionCorrection, modifierYawCorrection);
            Write(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedPosition, CharacterStateValue.FromVector3(currentWarpedPosition));
            Write(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedYaw, CharacterStateValue.FromYaw(currentWarpedYaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress, CharacterStateValue.FromScalar(positionProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress, CharacterStateValue.FromScalar(yawProgress));
            Trace(
                descriptor,
                limitResult == ProgramMotionWarpLimitResult.AppliedClamped ? MotionModifierDiagnosticCode.AppliedClamped : MotionModifierDiagnosticCode.Applied,
                SimulationTraceSeverity.Information,
                $"source={descriptor.SourceMotionOperation};action={action.InstanceId};target={action.TargetSnapshot.TargetId};normalized={currentProgress};translation={descriptor.TranslationMode};offsetSpace={descriptor.TargetOffsetSpace};rotation={descriptor.RotationMode};rotationMethod={descriptor.RotationMethod};limit={limitResult};sourceWindowStartPosition={sourceWindowStartPosition};sourceWindowStartYaw={sourceWindowStartYaw};sourceCurrentRelative={currentSourceRelative};sourceCurrentYawRelative={currentSourceYawRelative};previousWarpedPosition={previousWarpedPosition};previousWarpedYaw={previousWarpedYaw};warpedCumulativePosition={currentWarpedPosition};warpedCumulativeYaw={currentWarpedYaw};warpedDelta={warpedSourceDelta};rawSourceDelta={rawSourceDelta};correction={modifierPositionCorrection};warpedYawDelta={warpedSourceYawDelta};rawSourceYawDelta={rawSourceYawDelta};yawCorrection={modifierYawCorrection};positionProgress={positionProgress};yawProgress={yawProgress};finalActionDisplacement={channel.Displacement};finalActionYaw={channel.YawDegrees}");
        }

        public void Fail(string code, ProgramMotionModifierDescriptor descriptor, string detail)
        {
            Trace(descriptor, code, SimulationTraceSeverity.Error, detail);
            throw new InvalidOperationException($"{code}: {detail}");
        }

        bool InitializeWarp(
            ProgramMotionModifierDescriptor descriptor,
            SimulationActionTargetSnapshot target,
            out FixedVector3 startBodyPosition,
            out FixedYaw startBodyYaw,
            out FixedVector3 sourceWindowStartPosition,
            out FixedScalar sourceWindowStartYaw,
            out FixedVector3 resolvedTargetPosition,
            out FixedYaw resolvedTargetYaw,
            out ProgramMotionWarpLimitResult limitResult)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            FixedScalar warpStart = ClipTime(warp, TimelineClipTimePoint.Start);
            FixedScalar warpEnd = ClipTime(warp, TimelineClipTimePoint.End);
            startBodyPosition = m_Frame.Body.Position;
            startBodyYaw = m_Frame.Body.Yaw;
            SourcePoseAtTime(source, warpStart, out sourceWindowStartPosition, out sourceWindowStartYaw);
            SourcePoseAtTime(source, warpEnd, out FixedVector3 sourceWindowEndPosition, out FixedScalar sourceWindowEndYaw);
            FixedVector3 sourceEnd = sourceWindowEndPosition - sourceWindowStartPosition;
            FixedScalar sourceEndYaw = sourceWindowEndYaw - sourceWindowStartYaw;
            FixedVector3 nominalSourceEndOffset = FixedAngle.RotatePlanar(sourceEnd, startBodyYaw);
            FixedVector3 nominalSourceEnd = startBodyPosition + nominalSourceEndOffset;
            FixedVector3 requestedTargetPosition = descriptor.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? nominalSourceEnd
                : ResolveTargetPosition(descriptor, target, startBodyPosition, startBodyYaw, nominalSourceEnd.Y);
            FixedVector3 requestedPositionCorrection = new FixedVector3(
                requestedTargetPosition.X - nominalSourceEnd.X,
                FixedScalar.Zero,
                requestedTargetPosition.Z - nominalSourceEnd.Z);
            FixedScalar maximumPosition = descriptor.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? FixedScalar.Zero
                : Scalar(descriptor.MaximumPositionCorrectionConstantIndex, "MaximumPlanarCorrection");
            bool positionExceeded = descriptor.TranslationMode != ProgramMotionWarpTranslationMode.Disabled &&
                                    new FixedVector2(requestedPositionCorrection.X, requestedPositionCorrection.Z).Magnitude > maximumPosition;
            FixedVector3 effectivePositionCorrection = positionExceeded
                ? ClampMagnitude(requestedPositionCorrection, maximumPosition)
                : requestedPositionCorrection;
            resolvedTargetPosition = nominalSourceEnd + effectivePositionCorrection;

            FixedYaw nominalSourceEndYaw = new FixedYaw(startBodyYaw.Degrees + sourceEndYaw);
            FixedYaw requestedTargetYaw = nominalSourceEndYaw;
            if (descriptor.RotationMode == ProgramMotionWarpRotationMode.MatchTargetYaw)
                requestedTargetYaw = new FixedYaw(target.Yaw.Degrees + Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees"));
            else if (descriptor.RotationMode == ProgramMotionWarpRotationMode.FaceTarget)
            {
                FixedScalar directionX = target.Position.X - resolvedTargetPosition.X;
                FixedScalar directionZ = target.Position.Z - resolvedTargetPosition.Z;
                if (directionX == FixedScalar.Zero && directionZ == FixedScalar.Zero)
                    Fail(MotionModifierDiagnosticCode.FaceTargetZeroDirection, descriptor, "FaceTarget desired actor position equals the target planar position.");
                requestedTargetYaw = new FixedYaw(
                    FixedAngle.FromPlanarDirection(directionX, directionZ).Degrees +
                    Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees"));
            }
            FixedScalar requestedYawCorrection = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? FixedScalar.Zero
                : FixedAngle.Delta(nominalSourceEndYaw, requestedTargetYaw);
            FixedScalar maximumYaw = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? FixedScalar.Zero
                : Scalar(descriptor.MaximumYawCorrectionConstantIndex, "MaximumYawCorrectionDegrees");
            if (descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled && descriptor.RotationMethod == ProgramMotionWarpRotationMethod.ConstantRate)
            {
                FixedScalar rate = Scalar(descriptor.MaximumYawRateConstantIndex, "MaximumYawRateDegreesPerSecond");
                maximumYaw = FixedScalar.Min(maximumYaw, rate * (warpEnd - warpStart));
            }
            bool yawExceeded = descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                               FixedScalar.Abs(requestedYawCorrection) > maximumYaw;
            FixedScalar effectiveYawCorrection = FixedScalar.Clamp(requestedYawCorrection, -maximumYaw, maximumYaw);
            resolvedTargetYaw = new FixedYaw(nominalSourceEndYaw.Degrees + effectiveYawCorrection);
            Trace(
                descriptor,
                MotionModifierDiagnosticCode.TargetResolved,
                SimulationTraceSeverity.Detail,
                $"source={descriptor.SourceMotionOperation};windowStart={warpStart};windowEnd={warpEnd};translation={descriptor.TranslationMode};offsetSpace={descriptor.TargetOffsetSpace};rotation={descriptor.RotationMode};rotationMethod={descriptor.RotationMethod};limitPolicy={descriptor.LimitPolicy};sourceWindowStartPosition={sourceWindowStartPosition};sourceWindowStartYaw={sourceWindowStartYaw};sourceWindowEndPosition={sourceWindowEndPosition};sourceWindowEndYaw={sourceWindowEndYaw};requestedTargetPosition={requestedTargetPosition};requestedTargetYaw={requestedTargetYaw};effectiveTargetPosition={resolvedTargetPosition};effectiveTargetYaw={resolvedTargetYaw};positionExceeded={positionExceeded};yawExceeded={yawExceeded}");
            bool exceeded = positionExceeded || yawExceeded;
            limitResult = exceeded ? ProgramMotionWarpLimitResult.AppliedClamped : ProgramMotionWarpLimitResult.Applied;
            if (exceeded && descriptor.LimitPolicy == ProgramMotionWarpLimitPolicy.PreserveSource)
            {
                limitResult = ProgramMotionWarpLimitResult.PreservedByLimitPolicy;
                return false;
            }
            return true;
        }

        void WriteInitialized(
            ProgramMotionModifierDescriptor descriptor,
            ulong playbackGeneration,
            FixedActionInstanceState action,
            FixedVector3 startBodyPosition,
            FixedYaw startBodyYaw,
            FixedVector3 sourceWindowStartPosition,
            FixedScalar sourceWindowStartYaw,
            FixedVector3 resolvedTargetPosition,
            FixedYaw resolvedTargetYaw,
            ProgramMotionWarpLimitResult limitResult,
            FixedVector3 previousWarpedPosition,
            FixedYaw previousWarpedYaw,
            FixedScalar positionProgress,
            FixedScalar yawProgress)
        {
            Write(descriptor, ProgramStateSemantic.MotionWarpActive, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpInitialized, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpPlaybackGeneration, CharacterStateValue.FromUInt64(playbackGeneration));
            Write(descriptor, ProgramStateSemantic.MotionWarpActionInstance, CharacterStateValue.FromActionInstanceReference(FixedActionInstanceReference.FromInstance(action)));
            Write(descriptor, ProgramStateSemantic.MotionWarpStartBodyPosition, CharacterStateValue.FromVector3(startBodyPosition));
            Write(descriptor, ProgramStateSemantic.MotionWarpStartBodyYaw, CharacterStateValue.FromYaw(startBodyYaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpSourceWindowStartPosition, CharacterStateValue.FromVector3(sourceWindowStartPosition));
            Write(descriptor, ProgramStateSemantic.MotionWarpSourceWindowStartYaw, CharacterStateValue.FromScalar(sourceWindowStartYaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpResolvedTargetPosition, CharacterStateValue.FromVector3(resolvedTargetPosition));
            Write(descriptor, ProgramStateSemantic.MotionWarpResolvedTargetYaw, CharacterStateValue.FromYaw(resolvedTargetYaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpLimitResult, CharacterStateValue.FromInt32((int)limitResult));
            Write(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedPosition, CharacterStateValue.FromVector3(previousWarpedPosition));
            Write(descriptor, ProgramStateSemantic.MotionWarpPreviousWarpedYaw, CharacterStateValue.FromYaw(previousWarpedYaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress, CharacterStateValue.FromScalar(positionProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress, CharacterStateValue.FromScalar(yawProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpSourceOperation, CharacterStateValue.FromInt32(descriptor.SourceMotionOperation.Value));
        }

        FixedVector3 ResolveTargetPosition(
            ProgramMotionModifierDescriptor descriptor,
            SimulationActionTargetSnapshot target,
            FixedVector3 startBodyPosition,
            FixedYaw startBodyYaw,
            FixedScalar targetY)
        {
            FixedVector2 offset = Vector2(descriptor.TargetPlanarOffsetConstantIndex, "TargetPlanarOffset");
            FixedVector3 localOffset = new FixedVector3(offset.X, FixedScalar.Zero, offset.Y);
            FixedVector3 worldOffset;
            switch (descriptor.TargetOffsetSpace)
            {
                case ProgramMotionWarpTargetOffsetSpace.TargetLocal:
                    worldOffset = FixedAngle.RotatePlanar(localOffset, target.Yaw);
                    break;
                case ProgramMotionWarpTargetOffsetSpace.ApproachDirection:
                {
                    FixedVector2 outward = new FixedVector2(
                        startBodyPosition.X - target.Position.X,
                        startBodyPosition.Z - target.Position.Z);
                    if (outward.SqrMagnitude == FixedScalar.Zero)
                        Fail(MotionModifierDiagnosticCode.ApproachDirectionZero, descriptor, "ApproachDirection requires distinct target and warp-start planar positions.");
                    FixedVector2 forward = outward.Normalized;
                    FixedVector2 right = new FixedVector2(forward.Y, -forward.X);
                    worldOffset = new FixedVector3(
                        right.X * offset.X + forward.X * offset.Y,
                        FixedScalar.Zero,
                        right.Y * offset.X + forward.Y * offset.Y);
                    break;
                }
                case ProgramMotionWarpTargetOffsetSpace.ActorStartLocal:
                    worldOffset = FixedAngle.RotatePlanar(localOffset, startBodyYaw);
                    break;
                case ProgramMotionWarpTargetOffsetSpace.World:
                    worldOffset = localOffset;
                    break;
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported target offset space '{descriptor.TargetOffsetSpace}'.");
                    return default;
            }
            return new FixedVector3(
                target.Position.X + worldOffset.X,
                targetY,
                target.Position.Z + worldOffset.Z);
        }

        void EvaluateWarpPose(
            ProgramMotionModifierDescriptor descriptor,
            FixedScalar sampleTime,
            FixedScalar positionProgress,
            FixedScalar yawProgress,
            FixedVector3 startBodyPosition,
            FixedYaw startBodyYaw,
            FixedVector3 sourceWindowStartPosition,
            FixedScalar sourceWindowStartYaw,
            FixedVector3 resolvedTargetPosition,
            FixedYaw resolvedTargetYaw,
            out FixedVector3 warpedPosition,
            out FixedYaw warpedYaw,
            out FixedVector3 sourceRelative,
            out FixedScalar sourceYawRelative)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            SourcePoseAtTime(source, sampleTime, out FixedVector3 sourcePosition, out FixedScalar sourceYaw);
            SourcePoseAtTime(source, ClipTime(warp, TimelineClipTimePoint.End), out FixedVector3 sourceEndPosition, out FixedScalar sourceEndYaw);
            sourceRelative = sourcePosition - sourceWindowStartPosition;
            FixedVector3 sourceEndRelative = sourceEndPosition - sourceWindowStartPosition;
            sourceYawRelative = sourceYaw - sourceWindowStartYaw;
            FixedScalar sourceEndYawRelative = sourceEndYaw - sourceWindowStartYaw;
            FixedYaw nominalCurrentYaw = new FixedYaw(startBodyYaw.Degrees + sourceYawRelative);
            FixedYaw nominalEndYaw = new FixedYaw(startBodyYaw.Degrees + sourceEndYawRelative);
            FixedScalar finalYawCorrection = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? FixedScalar.Zero
                : FixedAngle.Delta(nominalEndYaw, resolvedTargetYaw);
            FixedScalar currentYawCorrection;
            switch (descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? ProgramMotionWarpRotationMethod.ProgressCurve
                : descriptor.RotationMethod)
            {
                case ProgramMotionWarpRotationMethod.ProgressCurve:
                    currentYawCorrection = finalYawCorrection * yawProgress;
                    break;
                case ProgramMotionWarpRotationMethod.ConstantRate:
                {
                    FixedScalar elapsed = FixedScalar.Max(FixedScalar.Zero, sampleTime - ClipTime(warp, TimelineClipTimePoint.Start));
                    FixedScalar maximum = Scalar(descriptor.MaximumYawRateConstantIndex, "MaximumYawRateDegreesPerSecond") * elapsed;
                    currentYawCorrection = FixedScalar.Clamp(finalYawCorrection, -maximum, maximum);
                    break;
                }
                case ProgramMotionWarpRotationMethod.ScaleSourceYaw:
                {
                    if (FixedScalar.Abs(sourceEndYawRelative) <= FixedScalar.FromRatio(1, 1000000))
                        Fail(MotionModifierDiagnosticCode.ScaleSourceYawZero, descriptor, "ScaleSourceYaw requires non-zero source window yaw.");
                    FixedScalar targetYawRelative = FixedAngle.Delta(startBodyYaw, resolvedTargetYaw);
                    currentYawCorrection = sourceYawRelative * (targetYawRelative / sourceEndYawRelative) - sourceYawRelative;
                    break;
                }
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported rotation method '{descriptor.RotationMethod}'.");
                    currentYawCorrection = FixedScalar.Zero;
                    break;
            }
            warpedYaw = new FixedYaw(nominalCurrentYaw.Degrees + currentYawCorrection);

            FixedVector3 rotatedSource = FixedAngle.RotatePlanar(
                sourceRelative,
                new FixedYaw(startBodyYaw.Degrees + currentYawCorrection));
            FixedVector3 rotatedSourceEnd = FixedAngle.RotatePlanar(
                sourceEndRelative,
                new FixedYaw(startBodyYaw.Degrees + finalYawCorrection));
            FixedVector3 targetRelative = resolvedTargetPosition - startBodyPosition;
            FixedVector3 warpedRelative;
            switch (descriptor.TranslationMode)
            {
                case ProgramMotionWarpTranslationMode.Disabled:
                    warpedRelative = rotatedSource;
                    break;
                case ProgramMotionWarpTranslationMode.ScaleToTarget:
                    warpedRelative = ScalePlanarToTarget(rotatedSource, rotatedSourceEnd, targetRelative, descriptor);
                    break;
                case ProgramMotionWarpTranslationMode.SkewToTarget:
                {
                    FixedVector3 endpointCorrection = Planar(targetRelative - rotatedSourceEnd);
                    warpedRelative = rotatedSource + endpointCorrection * positionProgress;
                    break;
                }
                case ProgramMotionWarpTranslationMode.LinearToTarget:
                    warpedRelative = new FixedVector3(
                        targetRelative.X * positionProgress,
                        sourceRelative.Y,
                        targetRelative.Z * positionProgress);
                    break;
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported translation mode '{descriptor.TranslationMode}'.");
                    warpedRelative = default;
                    break;
            }
            warpedPosition = startBodyPosition + new FixedVector3(
                warpedRelative.X,
                sourceRelative.Y,
                warpedRelative.Z);
        }

        FixedVector3 ScalePlanarToTarget(
            FixedVector3 value,
            FixedVector3 sourceEnd,
            FixedVector3 targetEnd,
            ProgramMotionModifierDescriptor descriptor)
        {
            FixedScalar denominator = sourceEnd.X * sourceEnd.X + sourceEnd.Z * sourceEnd.Z;
            if (denominator <= FixedScalar.FromRatio(1, 1000000))
                Fail(MotionModifierDiagnosticCode.ScaleSourcePositionZero, descriptor, "ScaleToTarget requires a non-zero source window planar endpoint.");
            FixedScalar dot = sourceEnd.X * targetEnd.X + sourceEnd.Z * targetEnd.Z;
            FixedScalar cross = sourceEnd.X * targetEnd.Z - sourceEnd.Z * targetEnd.X;
            return new FixedVector3(
                (dot * value.X - cross * value.Z) / denominator,
                value.Y,
                (cross * value.X + dot * value.Z) / denominator);
        }

        FixedScalar PositionProgress(ProgramMotionModifierDescriptor descriptor, FixedScalar normalized)
        {
            return descriptor.TranslationMode is ProgramMotionWarpTranslationMode.SkewToTarget or ProgramMotionWarpTranslationMode.LinearToTarget
                ? SampleProgress(Curve(descriptor.PositionProgressCurveConstantIndex, "PositionProgressCurve"), normalized)
                : normalized;
        }

        FixedScalar YawProgress(ProgramMotionModifierDescriptor descriptor, FixedScalar normalized)
        {
            return descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                   descriptor.RotationMethod == ProgramMotionWarpRotationMethod.ProgressCurve
                ? SampleProgress(Curve(descriptor.YawProgressCurveConstantIndex, "YawProgressCurve"), normalized)
                : normalized;
        }

        void SourcePoseAtTime(
            ProgramCatalogEntry source,
            FixedScalar time,
            out FixedVector3 position,
            out FixedScalar yaw)
        {
            FixedScalar start = ClipTime(source, TimelineClipTimePoint.Start);
            FixedScalar curveEnd = ClipTime(source, TimelineClipTimePoint.CurveEnd);
            FixedScalar duration = FixedScalar.Max(FixedScalar.FromRatio(1, 1000000), curveEnd - start);
            FixedScalar normalized = FixedScalar.Clamp((time - start) / duration, FixedScalar.Zero, FixedScalar.One);
            position = SampleSourcePosition(source, normalized);
            yaw = SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, normalized);
        }

        void SourceDeltaAtSegment(
            ProgramMotionModifierDescriptor descriptor,
            TimelineSegment<FixedScalar> segment,
            out FixedVector3 displacement,
            out FixedScalar yaw)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            SourcePoseAtTime(source, segment.Previous, out FixedVector3 previousPosition, out FixedScalar previousYaw);
            SourcePoseAtTime(source, segment.Current, out FixedVector3 currentPosition, out FixedScalar currentYaw);
            displacement = FixedAngle.RotatePlanar(currentPosition - previousPosition, m_Frame.Body.Yaw);
            yaw = currentYaw - previousYaw;
        }

        static FixedVector3 Planar(FixedVector3 value) =>
            new FixedVector3(value.X, FixedScalar.Zero, value.Z);

        FixedScalar NormalizeWindowTime(ProgramMotionModifierDescriptor descriptor, FixedScalar time)
        {
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            FixedScalar start = ClipTime(warp, TimelineClipTimePoint.Start);
            FixedScalar end = ClipTime(warp, TimelineClipTimePoint.End);
            FixedScalar duration = FixedScalar.Max(FixedScalar.FromRatio(1, 1000000), end - start);
            return FixedScalar.Clamp((time - start) / duration, FixedScalar.Zero, FixedScalar.One);
        }

        FixedScalar ClipTime(ProgramCatalogEntry clip, TimelineClipTimePoint point)
        {
            ProgramCatalogFieldId field = point switch
            {
                TimelineClipTimePoint.Start => ProgramCatalogFieldId.StartFrame,
                TimelineClipTimePoint.End => ProgramCatalogFieldId.EndFrame,
                TimelineClipTimePoint.CurveEnd => ProgramCatalogFieldId.CurveEndFrame,
                _ => throw new ArgumentOutOfRangeException(nameof(point))
            };
            int frame = CatalogInt32(clip, field);
            string trackIdentity = CatalogIdentity(clip, ProgramCatalogFieldId.Track);
            ProgramCatalogEntry track = RequireCatalog(ProgramCatalogEntryKind.TimelineTrack, trackIdentity);
            string timelineIdentity = CatalogIdentity(track, ProgramCatalogFieldId.Timeline);
            ProgramCatalogEntry timeline = RequireCatalog(ProgramCatalogEntryKind.Timeline, timelineIdentity);
            int frameRate = CatalogInt32(timeline, ProgramCatalogFieldId.FrameRate);
            if (frameRate <= 0)
                throw new InvalidOperationException($"Timeline '{timeline.Identity}' has an invalid FrameRate.");
            return FixedScalar.FromInt64(frame) / FixedScalar.FromInt64(frameRate);
        }

        ProgramCatalogEntry SourceCatalog(OperationHandle operation)
        {
            SimulationOperation source = Access.Operation(operation);
            return FindCatalog(source, ProgramCatalogEntryKind.MotionCurve) ??
                   FindCatalog(source, ProgramCatalogEntryKind.TimelineClip) ??
                   throw new InvalidOperationException($"MotionWarp source '{operation}' has no MotionCurve catalog.");
        }

        FixedVector3 SampleSourcePosition(ProgramCatalogEntry source, FixedScalar normalized) => new FixedVector3(
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionX, normalized),
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionY, normalized),
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionZ, normalized));

        FixedScalar SampleSourceCurve(ProgramCatalogEntry source, ProgramCatalogFieldId field, FixedScalar normalized)
        {
            ProgramConstant constant = CatalogConstant(source, field);
            if (constant.Kind != ProgramConstantKind.Bytes)
                throw new InvalidOperationException($"MotionCurve '{source.Identity}/{field}' is not a curve constant.");
            return Access.Services.RequireTimelineCurve(constant, $"{source.Identity}/{field}").Evaluate(normalized, FixedScalar.Zero);
        }

        ProgramCurve Curve(int constantIndex, string name)
        {
            ProgramConstant constant = RequireConstant(constantIndex, ProgramConstantKind.Bytes, name);
            return Access.Services.RequireTimelineCurve(constant, name);
        }

        FixedScalar Scalar(int constantIndex, string name) => RequireConstant(constantIndex, ProgramConstantKind.Scalar, name).Scalar;

        FixedVector2 Vector2(int constantIndex, string name) => RequireConstant(constantIndex, ProgramConstantKind.Vector2, name).Vector2;

        ProgramConstant RequireConstant(int index, ProgramConstantKind kind, string name)
        {
            if (index < 0 || index >= m_Program.Constants.Count || m_Program.Constants[index].Kind != kind)
                throw new InvalidOperationException($"MotionWarp constant '{name}' has an invalid Program kind.");
            return m_Program.Constants[index];
        }

        static FixedScalar SampleProgress(ProgramCurve curve, FixedScalar normalized) =>
            FixedScalar.Clamp(curve.Evaluate(normalized, normalized), FixedScalar.Zero, FixedScalar.One);

        static FixedVector3 ClampMagnitude(FixedVector3 value, FixedScalar maximum)
        {
            if (maximum <= FixedScalar.Zero)
                return FixedVector3.Zero;
            FixedScalar magnitude = new FixedVector2(value.X, value.Z).Magnitude;
            return magnitude > maximum ? value * (maximum / magnitude) : value;
        }

        void RequireProgress(FixedScalar value, ProgramMotionModifierDescriptor descriptor, string label)
        {
            if (value < FixedScalar.Zero || value > FixedScalar.One)
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Restored {label} progress '{value}' is outside [0,1].");
        }

        CharacterStateValue Read(ProgramMotionModifierDescriptor descriptor, ProgramStateSemantic semantic) =>
            m_State.Get(Access.RequireOperationSlot(descriptor.Operation, semantic));

        void Write(ProgramMotionModifierDescriptor descriptor, ProgramStateSemantic semantic, CharacterStateValue value) =>
            m_State.Set(Access.RequireOperationSlot(descriptor.Operation, semantic), value);

        void Trace(
            ProgramMotionModifierDescriptor descriptor,
            string code,
            SimulationTraceSeverity severity,
            string detail)
        {
            m_Frame.Trace.Add(Access.Operation(descriptor.Operation), code, severity, detail);
        }
    }

    internal sealed class FixedLocomotionRuntime : FixedOperationModule
    {
        readonly IFixedValueInputReader m_Values;
        readonly IFixedMotionContributionSink m_Motion;
        readonly FixedEvaluationFrame m_Frame;

        public FixedLocomotionRuntime(
            FixedProgramAccess access,
            IFixedValueInputReader values,
            IFixedMotionContributionSink motion,
            FixedEvaluationFrame frame)
            : base(access)
        {
            m_Values = values ?? throw new ArgumentNullException(nameof(values));
            m_Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void Submit<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            using FixedValueInputLease inputs = m_Values.ReadInputs(cursor, operation);
            CharacterStateValue input = inputs.FindByKind(ProgramStateValueKind.Vector2);
            if (input.Kind != ProgramStateValueKind.Vector2)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has no Vector2 input.");
            FixedVector2 move = input.Vector2;
            if (move.SqrMagnitude > FixedScalar.One)
                move = move.Normalized;
            FixedScalar delta = FixedScalar.One / FixedScalar.FromInt64(m_Program.Manifest.TickRate);
            ProgramConstant speedConstant = FindConstant(operation, OperationNamedConstant.MoveSpeed);
            ProgramConstant turnConstant = FindConstant(operation, OperationNamedConstant.TurnSpeedDegrees);
            if (speedConstant == null || speedConstant.Kind != ProgramConstantKind.Scalar ||
                turnConstant == null || turnConstant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid speed constants.");
            FixedScalar speed = speedConstant.Scalar;
            FixedScalar maxYaw = turnConstant.Scalar * delta;
            FixedVector3 displacement = new FixedVector3(
                move.X * speed * delta,
                FixedScalar.Zero,
                move.Y * speed * delta);
            FixedScalar yaw = FixedScalar.Zero;
            if (move != FixedVector2.Zero && maxYaw > FixedScalar.Zero)
            {
                FixedYaw desired = FixedAngle.FromPlanarDirection(move);
                yaw = FixedScalar.Clamp(FixedAngle.Delta(m_Frame.Body.Yaw, desired), -maxYaw, maxYaw);
            }
            m_Motion.Submit(new SimulationMotionContribution(
                SourcePath(operation),
                operation.Handle,
                displacement,
                yaw,
                SimulationMotionContributionSpace.World,
                FixedScalar.One,
                0,
                SimulationMotionChannel.Locomotion,
                SimulationMotionBlendMode.Override,
                false));
        }
    }
}

