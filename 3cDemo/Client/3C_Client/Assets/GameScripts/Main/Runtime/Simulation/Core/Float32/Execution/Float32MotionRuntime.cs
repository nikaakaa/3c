using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct ResolvedGameplayMotion
    {
        public ResolvedGameplayMotion(
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            Float32Vector2 locomotionPlanarBasis,
            bool hasMotion,
            CommittedMovementPlaybackClock movementPlaybackClock,
            string actionOwnerIdentity,
            string gameplayResultOwnerIdentity)
        {
            Displacement = displacement;
            YawDegrees = yawDegrees;
            LocomotionPlanarBasis = locomotionPlanarBasis;
            HasMotion = hasMotion;
            MovementPlaybackClock = movementPlaybackClock;
            ActionOwnerIdentity = actionOwnerIdentity ?? string.Empty;
            GameplayResultOwnerIdentity = gameplayResultOwnerIdentity ?? string.Empty;
        }

        public Float32Vector3 Displacement { get; }
        public Float32Scalar YawDegrees { get; }
        public Float32Vector2 LocomotionPlanarBasis { get; }
        public bool HasMotion { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public string ActionOwnerIdentity { get; }
        public string GameplayResultOwnerIdentity { get; }
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
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            Float32Vector2 planarBasis,
            SimulationMotionContributionSpace space,
            Float32Scalar weight,
            int priority,
            SimulationMotionChannel channel,
            SimulationMotionBlendMode blendMode,
            bool consumeLowerChannels,
            CommittedMovementPlaybackClock movementPlaybackClock)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (!sourceOperation.IsValid)
                throw new ArgumentException("Motion contribution source operation is invalid.", nameof(sourceOperation));
            SourceOperation = sourceOperation;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            PlanarBasis = planarBasis;
            Space = space;
            Weight = Float32Scalar.Clamp(weight, Float32Scalar.Zero, Float32Scalar.One);
            Priority = priority;
            Channel = channel;
            BlendMode = blendMode;
            ConsumeLowerChannels = consumeLowerChannels;
            if (channel == SimulationMotionChannel.Locomotion && !movementPlaybackClock.IsValid)
                throw new ArgumentException("Locomotion motion contribution has no committed Movement playback clock.", nameof(movementPlaybackClock));
            if (channel != SimulationMotionChannel.Locomotion && movementPlaybackClock.IsValid)
                throw new ArgumentException("Only the Locomotion motion channel may carry a committed Movement playback clock.", nameof(movementPlaybackClock));
            MovementPlaybackClock = movementPlaybackClock;
        }

        public string SourceIdentity { get; }
        public OperationHandle SourceOperation { get; }
        public Float32Vector3 Displacement { get; }
        public Float32Scalar YawDegrees { get; }
        public Float32Vector2 PlanarBasis { get; }
        public SimulationMotionContributionSpace Space { get; }
        public Float32Scalar Weight { get; }
        public int Priority { get; }
        public SimulationMotionChannel Channel { get; }
        public SimulationMotionBlendMode BlendMode { get; }
        public bool ConsumeLowerChannels { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public bool HasDelta => Weight > Float32Scalar.Zero &&
            (Displacement != Float32Vector3.Zero || YawDegrees != Float32Scalar.Zero);
        public bool ClaimsLowerChannels => Weight > Float32Scalar.Zero &&
            BlendMode == SimulationMotionBlendMode.Override &&
            ConsumeLowerChannels;
        public bool CanResolve => HasDelta || ClaimsLowerChannels || MovementPlaybackClock.IsValid;
    }

    internal struct ResolvedMotionChannel
    {
        public ResolvedMotionChannel(
            SimulationMotionChannel channel,
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            Float32Vector2 planarBasis,
            bool hasContribution,
            bool claimsLowerChannels,
            OperationHandle resolvedOwnerOperation,
            string resolvedOwnerIdentity,
            CommittedMovementPlaybackClock movementPlaybackClock,
            Float32Vector3 resolvedOwnerDisplacement,
            Float32Scalar resolvedOwnerYawDegrees,
            OperationHandle traceOperation,
            int participatingSourceCount,
            ulong participatingSourceFingerprint)
        {
            Channel = channel;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            PlanarBasis = planarBasis;
            HasContribution = hasContribution;
            ClaimsLowerChannels = claimsLowerChannels;
            ResolvedOwnerOperation = resolvedOwnerOperation;
            ResolvedOwnerIdentity = resolvedOwnerIdentity ?? string.Empty;
            MovementPlaybackClock = movementPlaybackClock;
            ResolvedOwnerDisplacement = resolvedOwnerDisplacement;
            ResolvedOwnerYawDegrees = resolvedOwnerYawDegrees;
            TraceOperation = traceOperation;
            ParticipatingSourceCount = participatingSourceCount;
            ParticipatingSourceFingerprint = participatingSourceFingerprint;
        }

        public SimulationMotionChannel Channel { get; }
        public Float32Vector3 Displacement { get; private set; }
        public Float32Scalar YawDegrees { get; private set; }
        public Float32Vector2 PlanarBasis { get; }
        public bool HasContribution { get; }
        public bool ClaimsLowerChannels { get; }
        public OperationHandle ResolvedOwnerOperation { get; }
        public string ResolvedOwnerIdentity { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public Float32Vector3 ResolvedOwnerDisplacement { get; }
        public Float32Scalar ResolvedOwnerYawDegrees { get; }
        public OperationHandle TraceOperation { get; }
        public int ParticipatingSourceCount { get; }
        public ulong ParticipatingSourceFingerprint { get; }
        public bool HasDelta => Displacement != Float32Vector3.Zero || YawDegrees != Float32Scalar.Zero;

        public void ApplyCorrection(Float32Vector3 displacement, Float32Scalar yawDegrees)
        {
            Displacement += displacement;
            YawDegrees += yawDegrees;
        }
    }

    internal sealed class Float32MotionAccumulator : Float32OperationModule,
        IFloat32MotionContributionSink,
        IFloat32MotionModifierSampleSink
    {
        readonly Float32EvaluationFrame m_Frame;
        readonly List<SimulationMotionContribution> m_Contributions;
        readonly List<MotionWarpSample<Float32Scalar, Float32ActionInstanceState>> m_WarpSamples;
        readonly Float32MotionWarpTarget m_MotionWarp;

        public Float32MotionAccumulator(
            Float32ProgramAccess access,
            Float32EvaluationFrame frame,
            Float32StatePort modifierState,
            List<SimulationMotionContribution> contributions,
            List<MotionWarpSample<Float32Scalar, Float32ActionInstanceState>> warpSamples)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
            m_WarpSamples = warpSamples ?? throw new ArgumentNullException(nameof(warpSamples));
            m_MotionWarp = new Float32MotionWarpTarget(access, frame, modifierState);
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
                    $"channel={contribution.Channel};blend={contribution.BlendMode};priority={contribution.Priority};weight={contribution.Weight};delta={contribution.Displacement};yaw={contribution.YawDegrees};claim={contribution.ClaimsLowerChannels};movementClock={FormatMovementClock(contribution.MovementPlaybackClock)}");
            }
        }

        public void Submit(MotionWarpSample<Float32Scalar, Float32ActionInstanceState> sample) => m_WarpSamples.Add(sample);

        public ResolvedGameplayMotion Resolve()
        {
            ResolvedMotionChannel locomotion = ResolveChannel(SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = ResolveChannel(SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = ResolveChannel(SimulationMotionChannel.GameplayResult);

            ProgramMotionModifierRuntime.ApplyActionWarp<Float32Scalar, Float32ActionInstanceState, ResolvedMotionChannel, Float32MotionWarpTarget>(
                m_Layout.MotionModifiers(ProgramMotionModifierChannel.Action),
                m_WarpSamples,
                action.ResolvedOwnerOperation,
                ref action,
                m_MotionWarp);
            RequireNoUnsupportedModifiers(ProgramMotionModifierChannel.GameplayResult);

            Float32Vector3 displacement = Float32Vector3.Zero;
            Float32Scalar yaw = Float32Scalar.Zero;
            Compose(locomotion, ref displacement, ref yaw);
            Compose(action, ref displacement, ref yaw);
            Compose(gameplayResult, ref displacement, ref yaw);

            bool hasMotion = displacement != Float32Vector3.Zero || yaw != Float32Scalar.Zero;
            var motion = new ResolvedGameplayMotion(
                displacement,
                yaw,
                locomotion.PlanarBasis,
                hasMotion,
                locomotion.MovementPlaybackClock,
                action.ResolvedOwnerIdentity,
                gameplayResult.ResolvedOwnerIdentity);
            TraceResolvedGameplayMotion(motion, action);
            return motion;
        }

        ResolvedMotionChannel ResolveChannel(SimulationMotionChannel channel)
        {
            Float32Vector3 additiveDisplacement = Float32Vector3.Zero;
            Float32Scalar additiveYaw = Float32Scalar.Zero;
            Float32Vector3 weightedDisplacement = Float32Vector3.Zero;
            Float32Scalar weightedYaw = Float32Scalar.Zero;
            Float32Scalar totalWeight = Float32Scalar.Zero;
            SimulationMotionContribution overrideWinner = default;
            Float32Vector3 overrideDisplacement = Float32Vector3.Zero;
            Float32Scalar overrideYaw = Float32Scalar.Zero;
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
                Float32Vector3 resolved = contribution.Space == SimulationMotionContributionSpace.ActorLocal
                    ? Float32Angle.RotatePlanar(contribution.Displacement, m_Frame.Body.Yaw)
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
                return new ResolvedMotionChannel(channel, Float32Vector3.Zero, Float32Scalar.Zero, Float32Vector2.Zero, false, false, OperationHandle.Invalid, string.Empty, default, Float32Vector3.Zero, Float32Scalar.Zero, OperationHandle.Invalid, 0, 0);

            Float32Vector3 channelDisplacement = additiveDisplacement;
            Float32Scalar channelYaw = additiveYaw;
            if (hasOverride)
            {
                channelDisplacement += overrideDisplacement;
                channelYaw += overrideYaw;
            }
            else if (hasWeighted && totalWeight > Float32Scalar.Zero)
            {
                channelDisplacement += new Float32Vector3(
                    weightedDisplacement.X / totalWeight,
                    weightedDisplacement.Y / totalWeight,
                    weightedDisplacement.Z / totalWeight);
                channelYaw += weightedYaw / totalWeight;
            }
            CommittedMovementPlaybackClock movementPlaybackClock =
                channel == SimulationMotionChannel.Locomotion && hasOverride
                    ? overrideWinner.MovementPlaybackClock
                    : default;
            if (channel == SimulationMotionChannel.Locomotion && !movementPlaybackClock.IsValid)
                throw new InvalidOperationException("Resolved Locomotion motion has no single committed Movement playback clock owner.");
            var result = new ResolvedMotionChannel(
                channel,
                channelDisplacement,
                channelYaw,
                hasOverride ? overrideWinner.PlanarBasis : Float32Vector2.Zero,
                true,
                hasOverride && overrideWinner.ConsumeLowerChannels,
                hasOverride ? overrideWinner.SourceOperation : OperationHandle.Invalid,
                hasOverride ? overrideWinner.SourceIdentity : string.Empty,
                movementPlaybackClock,
                hasOverride ? overrideDisplacement : Float32Vector3.Zero,
                hasOverride ? overrideYaw : Float32Scalar.Zero,
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
            ref Float32Vector3 displacement,
            ref Float32Scalar yaw)
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
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};planarBasis={channel.PlanarBasis};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16};movementClock={FormatMovementClock(channel.MovementPlaybackClock)}");
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
                $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion};movementClock={FormatMovementClock(motion.MovementPlaybackClock)}");
        }

        static string FormatMovementClock(CommittedMovementPlaybackClock clock) =>
            clock.IsValid
                ? $"{clock.OwnerIdentity}@{clock.Generation}:{clock.ContinuousTicks}/{clock.TickRate}#tick{clock.AuthorityTick.Value}"
                : "none";

        static ulong MixSource(ulong hash, int operation)
        {
            unchecked
            {
                hash ^= (uint)operation;
                return hash * 1099511628211UL;
            }
        }

    }

    internal sealed class Float32MotionWarpTarget : Float32OperationModule,
        IMotionModifierTarget<Float32Scalar, Float32ActionInstanceState, ResolvedMotionChannel>
    {
        readonly Float32EvaluationFrame m_Frame;
        readonly Float32StatePort m_State;

        public Float32MotionWarpTarget(
            Float32ProgramAccess access,
            Float32EvaluationFrame frame,
            Float32StatePort state)
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
            MotionWarpSample<Float32Scalar, Float32ActionInstanceState> sample,
            ref ResolvedMotionChannel channel)
        {
            Float32ActionInstanceState action = sample.Action;
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
            Float32ActionInstanceReference storedReference = Read(descriptor, ProgramStateSemantic.MotionWarpActionInstance).ActionInstanceReference;
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

            Float32Scalar previousProgress = NormalizeWindowTime(descriptor, sample.Segment.Previous);
            Float32Scalar currentProgress = NormalizeWindowTime(descriptor, sample.Segment.Current);
            Float32Scalar previousPositionProgress;
            Float32Scalar previousYawProgress;
            Float32Vector3 startBodyPosition;
            Float32Yaw startBodyYaw;
            Float32Vector3 sourceWindowStartPosition;
            Float32Scalar sourceWindowStartYaw;
            Float32Vector3 resolvedTargetPosition;
            Float32Yaw resolvedTargetYaw;
            ProgramMotionWarpLimitResult limitResult;
            Float32Vector3 previousWarpedPosition;
            Float32Yaw previousWarpedYaw;

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
                    Trace(descriptor, MotionModifierDiagnosticCode.PreservedByLimitPolicy, SimulationTraceSeverity.Information,
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

            Float32Scalar positionProgress = PositionProgress(descriptor, currentProgress);
            Float32Scalar yawProgress = YawProgress(descriptor, currentProgress);
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
                out Float32Vector3 currentWarpedPosition,
                out Float32Yaw currentWarpedYaw,
                out Float32Vector3 currentSourceRelative,
                out Float32Scalar currentSourceYawRelative);
            Float32Vector3 warpedSourceDelta = currentWarpedPosition - previousWarpedPosition;
            Float32Scalar warpedSourceYawDelta = Float32Angle.Delta(previousWarpedYaw, currentWarpedYaw);
            SourceDeltaAtSegment(descriptor, sample.Segment, out Float32Vector3 rawSourceDelta, out Float32Scalar rawSourceYawDelta);
            Float32Vector3 modifierPositionCorrection = warpedSourceDelta - rawSourceDelta;
            Float32Scalar modifierYawCorrection = warpedSourceYawDelta - rawSourceYawDelta;
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
            out Float32Vector3 startBodyPosition,
            out Float32Yaw startBodyYaw,
            out Float32Vector3 sourceWindowStartPosition,
            out Float32Scalar sourceWindowStartYaw,
            out Float32Vector3 resolvedTargetPosition,
            out Float32Yaw resolvedTargetYaw,
            out ProgramMotionWarpLimitResult limitResult)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            Float32Scalar warpStart = ClipTime(warp, TimelineClipTimePoint.Start);
            Float32Scalar warpEnd = ClipTime(warp, TimelineClipTimePoint.End);
            startBodyPosition = m_Frame.Body.Position;
            startBodyYaw = m_Frame.Body.Yaw;
            SourcePoseAtTime(source, warpStart, out sourceWindowStartPosition, out sourceWindowStartYaw);
            SourcePoseAtTime(source, warpEnd, out Float32Vector3 sourceWindowEndPosition, out Float32Scalar sourceWindowEndYaw);
            Float32Vector3 sourceEnd = sourceWindowEndPosition - sourceWindowStartPosition;
            Float32Scalar sourceEndYaw = sourceWindowEndYaw - sourceWindowStartYaw;
            Float32Vector3 nominalSourceEndOffset = Float32Angle.RotatePlanar(sourceEnd, startBodyYaw);
            Float32Vector3 nominalSourceEnd = startBodyPosition + nominalSourceEndOffset;
            Float32Vector3 requestedTargetPosition = descriptor.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? nominalSourceEnd
                : ResolveTargetPosition(descriptor, target, startBodyPosition, startBodyYaw, nominalSourceEnd.Y);
            Float32Vector3 requestedPositionCorrection = new Float32Vector3(
                requestedTargetPosition.X - nominalSourceEnd.X,
                Float32Scalar.Zero,
                requestedTargetPosition.Z - nominalSourceEnd.Z);
            Float32Scalar maximumPosition = descriptor.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? Float32Scalar.Zero
                : Scalar(descriptor.MaximumPositionCorrectionConstantIndex, "MaximumPlanarCorrection");
            bool positionExceeded = descriptor.TranslationMode != ProgramMotionWarpTranslationMode.Disabled &&
                                    new Float32Vector2(requestedPositionCorrection.X, requestedPositionCorrection.Z).Magnitude > maximumPosition;
            Float32Vector3 effectivePositionCorrection = positionExceeded
                ? ClampMagnitude(requestedPositionCorrection, maximumPosition)
                : requestedPositionCorrection;
            resolvedTargetPosition = nominalSourceEnd + effectivePositionCorrection;

            Float32Yaw nominalSourceEndYaw = new Float32Yaw(startBodyYaw.Degrees + sourceEndYaw);
            Float32Yaw requestedTargetYaw = nominalSourceEndYaw;
            if (descriptor.RotationMode == ProgramMotionWarpRotationMode.MatchTargetYaw)
                requestedTargetYaw = new Float32Yaw(target.Yaw.Degrees + Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees"));
            else if (descriptor.RotationMode == ProgramMotionWarpRotationMode.FaceTarget)
            {
                Float32Scalar directionX = target.Position.X - resolvedTargetPosition.X;
                Float32Scalar directionZ = target.Position.Z - resolvedTargetPosition.Z;
                if (directionX == Float32Scalar.Zero && directionZ == Float32Scalar.Zero)
                    Fail(MotionModifierDiagnosticCode.FaceTargetZeroDirection, descriptor, "FaceTarget desired actor position equals the target planar position.");
                requestedTargetYaw = new Float32Yaw(
                    Float32Angle.FromPlanarDirection(directionX, directionZ).Degrees +
                    Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees"));
            }
            Float32Scalar requestedYawCorrection = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Angle.Delta(nominalSourceEndYaw, requestedTargetYaw);
            Float32Scalar maximumYaw = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Scalar(descriptor.MaximumYawCorrectionConstantIndex, "MaximumYawCorrectionDegrees");
            if (descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled && descriptor.RotationMethod == ProgramMotionWarpRotationMethod.ConstantRate)
            {
                Float32Scalar rate = Scalar(descriptor.MaximumYawRateConstantIndex, "MaximumYawRateDegreesPerSecond");
                maximumYaw = Float32Scalar.Min(maximumYaw, rate * (warpEnd - warpStart));
            }
            bool yawExceeded = descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                               Float32Scalar.Abs(requestedYawCorrection) > maximumYaw;
            Float32Scalar effectiveYawCorrection = Float32Scalar.Clamp(requestedYawCorrection, -maximumYaw, maximumYaw);
            resolvedTargetYaw = new Float32Yaw(nominalSourceEndYaw.Degrees + effectiveYawCorrection);
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
            Float32ActionInstanceState action,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Vector3 sourceWindowStartPosition,
            Float32Scalar sourceWindowStartYaw,
            Float32Vector3 resolvedTargetPosition,
            Float32Yaw resolvedTargetYaw,
            ProgramMotionWarpLimitResult limitResult,
            Float32Vector3 previousWarpedPosition,
            Float32Yaw previousWarpedYaw,
            Float32Scalar positionProgress,
            Float32Scalar yawProgress)
        {
            Write(descriptor, ProgramStateSemantic.MotionWarpActive, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpInitialized, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpPlaybackGeneration, CharacterStateValue.FromUInt64(playbackGeneration));
            Write(descriptor, ProgramStateSemantic.MotionWarpActionInstance, CharacterStateValue.FromActionInstanceReference(Float32ActionInstanceReference.FromInstance(action)));
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

        Float32Vector3 ResolveTargetPosition(
            ProgramMotionModifierDescriptor descriptor,
            SimulationActionTargetSnapshot target,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Scalar targetY)
        {
            Float32Vector2 offset = Vector2(descriptor.TargetPlanarOffsetConstantIndex, "TargetPlanarOffset");
            Float32Vector3 localOffset = new Float32Vector3(offset.X, Float32Scalar.Zero, offset.Y);
            Float32Vector3 worldOffset;
            switch (descriptor.TargetOffsetSpace)
            {
                case ProgramMotionWarpTargetOffsetSpace.TargetLocal:
                    worldOffset = Float32Angle.RotatePlanar(localOffset, target.Yaw);
                    break;
                case ProgramMotionWarpTargetOffsetSpace.ApproachDirection:
                {
                    Float32Vector2 outward = new Float32Vector2(
                        startBodyPosition.X - target.Position.X,
                        startBodyPosition.Z - target.Position.Z);
                    if (outward.SqrMagnitude <= Float32Scalar.FromSingle(0.000001f))
                        Fail(MotionModifierDiagnosticCode.ApproachDirectionZero, descriptor, "ApproachDirection requires distinct target and warp-start planar positions.");
                    Float32Vector2 forward = outward.Normalized;
                    Float32Vector2 right = new Float32Vector2(forward.Y, -forward.X);
                    worldOffset = new Float32Vector3(
                        right.X * offset.X + forward.X * offset.Y,
                        Float32Scalar.Zero,
                        right.Y * offset.X + forward.Y * offset.Y);
                    break;
                }
                case ProgramMotionWarpTargetOffsetSpace.ActorStartLocal:
                    worldOffset = Float32Angle.RotatePlanar(localOffset, startBodyYaw);
                    break;
                case ProgramMotionWarpTargetOffsetSpace.World:
                    worldOffset = localOffset;
                    break;
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported target offset space '{descriptor.TargetOffsetSpace}'.");
                    return default;
            }
            return new Float32Vector3(
                target.Position.X + worldOffset.X,
                targetY,
                target.Position.Z + worldOffset.Z);
        }

        void EvaluateWarpPose(
            ProgramMotionModifierDescriptor descriptor,
            Float32Scalar sampleTime,
            Float32Scalar positionProgress,
            Float32Scalar yawProgress,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Vector3 sourceWindowStartPosition,
            Float32Scalar sourceWindowStartYaw,
            Float32Vector3 resolvedTargetPosition,
            Float32Yaw resolvedTargetYaw,
            out Float32Vector3 warpedPosition,
            out Float32Yaw warpedYaw,
            out Float32Vector3 sourceRelative,
            out Float32Scalar sourceYawRelative)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            SourcePoseAtTime(source, sampleTime, out Float32Vector3 sourcePosition, out Float32Scalar sourceYaw);
            SourcePoseAtTime(source, ClipTime(warp, TimelineClipTimePoint.End), out Float32Vector3 sourceEndPosition, out Float32Scalar sourceEndYaw);
            sourceRelative = sourcePosition - sourceWindowStartPosition;
            Float32Vector3 sourceEndRelative = sourceEndPosition - sourceWindowStartPosition;
            sourceYawRelative = sourceYaw - sourceWindowStartYaw;
            Float32Scalar sourceEndYawRelative = sourceEndYaw - sourceWindowStartYaw;
            Float32Yaw nominalCurrentYaw = new Float32Yaw(startBodyYaw.Degrees + sourceYawRelative);
            Float32Yaw nominalEndYaw = new Float32Yaw(startBodyYaw.Degrees + sourceEndYawRelative);
            Float32Scalar finalYawCorrection = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Angle.Delta(nominalEndYaw, resolvedTargetYaw);
            Float32Scalar currentYawCorrection;
            switch (descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? ProgramMotionWarpRotationMethod.ProgressCurve
                : descriptor.RotationMethod)
            {
                case ProgramMotionWarpRotationMethod.ProgressCurve:
                    currentYawCorrection = finalYawCorrection * yawProgress;
                    break;
                case ProgramMotionWarpRotationMethod.ConstantRate:
                {
                    Float32Scalar elapsed = Float32Scalar.Max(Float32Scalar.Zero, sampleTime - ClipTime(warp, TimelineClipTimePoint.Start));
                    Float32Scalar maximum = Scalar(descriptor.MaximumYawRateConstantIndex, "MaximumYawRateDegreesPerSecond") * elapsed;
                    currentYawCorrection = Float32Scalar.Clamp(finalYawCorrection, -maximum, maximum);
                    break;
                }
                case ProgramMotionWarpRotationMethod.ScaleSourceYaw:
                {
                    if (Float32Scalar.Abs(sourceEndYawRelative) <= Float32Scalar.FromSingle(0.000001f))
                        Fail(MotionModifierDiagnosticCode.ScaleSourceYawZero, descriptor, "ScaleSourceYaw requires non-zero source window yaw.");
                    Float32Scalar targetYawRelative = Float32Angle.Delta(startBodyYaw, resolvedTargetYaw);
                    currentYawCorrection = sourceYawRelative * (targetYawRelative / sourceEndYawRelative) - sourceYawRelative;
                    break;
                }
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported rotation method '{descriptor.RotationMethod}'.");
                    currentYawCorrection = Float32Scalar.Zero;
                    break;
            }
            warpedYaw = new Float32Yaw(nominalCurrentYaw.Degrees + currentYawCorrection);

            Float32Vector3 rotatedSource = Float32Angle.RotatePlanar(
                sourceRelative,
                new Float32Yaw(startBodyYaw.Degrees + currentYawCorrection));
            Float32Vector3 rotatedSourceEnd = Float32Angle.RotatePlanar(
                sourceEndRelative,
                new Float32Yaw(startBodyYaw.Degrees + finalYawCorrection));
            Float32Vector3 targetRelative = resolvedTargetPosition - startBodyPosition;
            Float32Vector3 warpedRelative;
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
                    Float32Vector3 endpointCorrection = Planar(targetRelative - rotatedSourceEnd);
                    warpedRelative = rotatedSource + endpointCorrection * positionProgress;
                    break;
                }
                case ProgramMotionWarpTranslationMode.LinearToTarget:
                    warpedRelative = new Float32Vector3(
                        targetRelative.X * positionProgress,
                        sourceRelative.Y,
                        targetRelative.Z * positionProgress);
                    break;
                default:
                    Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Unsupported translation mode '{descriptor.TranslationMode}'.");
                    warpedRelative = default;
                    break;
            }
            warpedPosition = startBodyPosition + new Float32Vector3(
                warpedRelative.X,
                sourceRelative.Y,
                warpedRelative.Z);
        }

        Float32Vector3 ScalePlanarToTarget(
            Float32Vector3 value,
            Float32Vector3 sourceEnd,
            Float32Vector3 targetEnd,
            ProgramMotionModifierDescriptor descriptor)
        {
            Float32Scalar denominator = sourceEnd.X * sourceEnd.X + sourceEnd.Z * sourceEnd.Z;
            if (denominator <= Float32Scalar.FromSingle(0.000001f))
                Fail(MotionModifierDiagnosticCode.ScaleSourcePositionZero, descriptor, "ScaleToTarget requires a non-zero source window planar endpoint.");
            Float32Scalar dot = sourceEnd.X * targetEnd.X + sourceEnd.Z * targetEnd.Z;
            Float32Scalar cross = sourceEnd.X * targetEnd.Z - sourceEnd.Z * targetEnd.X;
            return new Float32Vector3(
                (dot * value.X - cross * value.Z) / denominator,
                value.Y,
                (cross * value.X + dot * value.Z) / denominator);
        }

        Float32Scalar PositionProgress(ProgramMotionModifierDescriptor descriptor, Float32Scalar normalized)
        {
            return descriptor.TranslationMode is ProgramMotionWarpTranslationMode.SkewToTarget or ProgramMotionWarpTranslationMode.LinearToTarget
                ? SampleProgress(Curve(descriptor.PositionProgressCurveConstantIndex, "PositionProgressCurve"), normalized)
                : normalized;
        }

        Float32Scalar YawProgress(ProgramMotionModifierDescriptor descriptor, Float32Scalar normalized)
        {
            return descriptor.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                   descriptor.RotationMethod == ProgramMotionWarpRotationMethod.ProgressCurve
                ? SampleProgress(Curve(descriptor.YawProgressCurveConstantIndex, "YawProgressCurve"), normalized)
                : normalized;
        }

        void SourcePoseAtTime(
            ProgramCatalogEntry source,
            Float32Scalar time,
            out Float32Vector3 position,
            out Float32Scalar yaw)
        {
            Float32Scalar start = ClipTime(source, TimelineClipTimePoint.Start);
            Float32Scalar curveEnd = ClipTime(source, TimelineClipTimePoint.CurveEnd);
            Float32Scalar duration = Float32Scalar.Max(Float32Scalar.FromSingle(0.000001f), curveEnd - start);
            Float32Scalar normalized = Float32Scalar.Clamp((time - start) / duration, Float32Scalar.Zero, Float32Scalar.One);
            position = SampleSourcePosition(source, normalized);
            yaw = SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, normalized);
        }

        void SourceDeltaAtSegment(
            ProgramMotionModifierDescriptor descriptor,
            TimelineSegment<Float32Scalar> segment,
            out Float32Vector3 displacement,
            out Float32Scalar yaw)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            SourcePoseAtTime(source, segment.Previous, out Float32Vector3 previousPosition, out Float32Scalar previousYaw);
            SourcePoseAtTime(source, segment.Current, out Float32Vector3 currentPosition, out Float32Scalar currentYaw);
            displacement = Float32Angle.RotatePlanar(currentPosition - previousPosition, m_Frame.Body.Yaw);
            yaw = currentYaw - previousYaw;
        }

        static Float32Vector3 Planar(Float32Vector3 value) =>
            new Float32Vector3(value.X, Float32Scalar.Zero, value.Z);

        Float32Scalar NormalizeWindowTime(ProgramMotionModifierDescriptor descriptor, Float32Scalar time)
        {
            ProgramCatalogEntry warp = m_Program.CatalogEntries[descriptor.CatalogEntryIndex];
            Float32Scalar start = ClipTime(warp, TimelineClipTimePoint.Start);
            Float32Scalar end = ClipTime(warp, TimelineClipTimePoint.End);
            Float32Scalar duration = Float32Scalar.Max(Float32Scalar.FromSingle(0.000001f), end - start);
            return Float32Scalar.Clamp((time - start) / duration, Float32Scalar.Zero, Float32Scalar.One);
        }

        Float32Scalar ClipTime(ProgramCatalogEntry clip, TimelineClipTimePoint point)
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
            return Float32Scalar.FromInt64(frame) / Float32Scalar.FromInt64(frameRate);
        }

        ProgramCatalogEntry SourceCatalog(OperationHandle operation)
        {
            SimulationOperation source = Access.Operation(operation);
            return FindCatalog(source, ProgramCatalogEntryKind.MotionCurve) ??
                   FindCatalog(source, ProgramCatalogEntryKind.TimelineClip) ??
                   throw new InvalidOperationException($"MotionWarp source '{operation}' has no MotionCurve catalog.");
        }

        Float32Vector3 SampleSourcePosition(ProgramCatalogEntry source, Float32Scalar normalized) => new Float32Vector3(
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionX, normalized),
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionY, normalized),
            SampleSourceCurve(source, ProgramCatalogFieldId.PositionZ, normalized));

        Float32Scalar SampleSourceCurve(ProgramCatalogEntry source, ProgramCatalogFieldId field, Float32Scalar normalized)
        {
            ProgramConstant constant = CatalogConstant(source, field);
            if (constant.Kind != ProgramConstantKind.Bytes)
                throw new InvalidOperationException($"MotionCurve '{source.Identity}/{field}' is not a curve constant.");
            return Access.Services.RequireTimelineCurve(constant, $"{source.Identity}/{field}").Evaluate(normalized, Float32Scalar.Zero);
        }

        ProgramCurve Curve(int constantIndex, string name)
        {
            ProgramConstant constant = RequireConstant(constantIndex, ProgramConstantKind.Bytes, name);
            return Access.Services.RequireTimelineCurve(constant, name);
        }

        Float32Scalar Scalar(int constantIndex, string name) => RequireConstant(constantIndex, ProgramConstantKind.Scalar, name).Scalar;

        Float32Vector2 Vector2(int constantIndex, string name) => RequireConstant(constantIndex, ProgramConstantKind.Vector2, name).Vector2;

        ProgramConstant RequireConstant(int index, ProgramConstantKind kind, string name)
        {
            if (index < 0 || index >= m_Program.Constants.Count || m_Program.Constants[index].Kind != kind)
                throw new InvalidOperationException($"MotionWarp constant '{name}' has an invalid Program kind.");
            return m_Program.Constants[index];
        }

        static Float32Scalar SampleProgress(ProgramCurve curve, Float32Scalar normalized) =>
            Float32Scalar.Clamp(curve.Evaluate(normalized, normalized), Float32Scalar.Zero, Float32Scalar.One);

        static Float32Vector3 ClampMagnitude(Float32Vector3 value, Float32Scalar maximum)
        {
            if (maximum <= Float32Scalar.Zero)
                return Float32Vector3.Zero;
            Float32Scalar magnitude = new Float32Vector2(value.X, value.Z).Magnitude;
            return magnitude > maximum ? value * (maximum / magnitude) : value;
        }

        void RequireProgress(Float32Scalar value, ProgramMotionModifierDescriptor descriptor, string label)
        {
            if (value < Float32Scalar.Zero || value > Float32Scalar.One)
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

    internal sealed class Float32LocomotionRuntime : Float32OperationModule
    {
        readonly IFloat32ValueInputReader m_Values;
        readonly IFloat32MotionContributionSink m_Motion;
        readonly Float32EvaluationFrame m_Frame;

        public Float32LocomotionRuntime(
            Float32ProgramAccess access,
            IFloat32ValueInputReader values,
            IFloat32MotionContributionSink motion,
            Float32EvaluationFrame frame)
            : base(access)
        {
            m_Values = values ?? throw new ArgumentNullException(nameof(values));
            m_Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void Submit<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int committedTicks,
            ulong generation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            using Float32ValueInputLease inputs = m_Values.ReadInputs(cursor, operation);
            CharacterStateValue input = inputs.FindByKind(ProgramStateValueKind.Vector2);
            if (input.Kind != ProgramStateValueKind.Vector2)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has no Vector2 input.");
            Float32Vector2 move = input.Vector2;
            if (move.SqrMagnitude > Float32Scalar.One)
                move = move.Normalized;
            var movementPlaybackClock = new CommittedMovementPlaybackClock(
                SourcePath(operation),
                generation,
                m_Frame.Tick,
                committedTicks,
                m_Program.Manifest.TickRate);
            Float32Scalar delta = Float32Scalar.One / Float32Scalar.FromInt64(m_Program.Manifest.TickRate);
            ProgramConstant turnConstant = FindConstant(operation, OperationNamedConstant.TurnSpeedDegrees);
            if (turnConstant == null || turnConstant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid turn speed.");
            Float32Scalar maxYaw = turnConstant.Scalar * delta;
            Float32Vector3 displacement = ResolveDisplacement(operation, move, delta, committedTicks - 1);
            Float32Scalar yaw = Float32Scalar.Zero;
            if (move != Float32Vector2.Zero && maxYaw > Float32Scalar.Zero)
            {
                Float32Yaw desired = Float32Angle.FromPlanarDirection(move);
                yaw = Float32Scalar.Clamp(Float32Angle.Delta(m_Frame.Body.Yaw, desired), -maxYaw, maxYaw);
            }
            m_Motion.Submit(new SimulationMotionContribution(
                SourcePath(operation),
                operation.Handle,
                displacement,
                yaw,
                move,
                SimulationMotionContributionSpace.World,
                Float32Scalar.One,
                0,
                SimulationMotionChannel.Locomotion,
                SimulationMotionBlendMode.Override,
                false,
                movementPlaybackClock));
        }

        Float32Vector3 ResolveDisplacement(
            SimulationOperation operation,
            Float32Vector2 move,
            Float32Scalar delta,
            int elapsedTicks)
        {
            var mode = (LocomotionInputMotionDisplacementMode)operation.Integer1;
            if (mode == LocomotionInputMotionDisplacementMode.ConstantSpeed)
            {
                ProgramConstant speed = FindConstant(operation, OperationNamedConstant.MoveSpeed);
                if (speed == null || speed.Kind != ProgramConstantKind.Scalar)
                    throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has no Move Speed.");
                return new Float32Vector3(
                    move.X * speed.Scalar * delta,
                    Float32Scalar.Zero,
                    move.Y * speed.Scalar * delta);
            }
            if (mode != LocomotionInputMotionDisplacementMode.ActionMotionCurve)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid displacement mode '{operation.Integer1}'.");
            if (move == Float32Vector2.Zero)
                return Float32Vector3.Zero;

            ProgramConstant xConstant = FindConstant(operation, OperationNamedConstant.ActionMotionPositionX);
            ProgramConstant zConstant = FindConstant(operation, OperationNamedConstant.ActionMotionPositionZ);
            ProgramConstant durationConstant = FindConstant(operation, OperationNamedConstant.ActionMotionDuration);
            if (xConstant == null || xConstant.Kind != ProgramConstantKind.Bytes ||
                zConstant == null || zConstant.Kind != ProgramConstantKind.Bytes ||
                durationConstant == null || durationConstant.Kind != ProgramConstantKind.Scalar ||
                durationConstant.Scalar <= Float32Scalar.Zero)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid Action Motion Curve constants.");

            Float32Scalar tickRate = Float32Scalar.FromInt64(m_Program.Manifest.TickRate);
            Float32Scalar fromTime = Float32Scalar.FromInt64(elapsedTicks) / tickRate;
            Float32Scalar toTime = Float32Scalar.FromInt64(checked(elapsedTicks + 1)) / tickRate;
            bool looping = (LocomotionInputMotionExecutionMode)operation.Integer0 == LocomotionInputMotionExecutionMode.Continuous;
            ProgramCurve xCurve = Access.Services.RequireTimelineCurve(xConstant, xConstant.Identity);
            ProgramCurve zCurve = Access.Services.RequireTimelineCurve(zConstant, zConstant.Identity);
            Float32Scalar duration = durationConstant.Scalar;
            Float32Scalar localX = SampleCumulative(xCurve, toTime, duration, looping) -
                SampleCumulative(xCurve, fromTime, duration, looping);
            Float32Scalar localZ = SampleCumulative(zCurve, toTime, duration, looping) -
                SampleCumulative(zCurve, fromTime, duration, looping);

            Float32Vector2 forward = move.Normalized;
            Float32Vector2 right = new Float32Vector2(forward.Y, -forward.X);
            return new Float32Vector3(
                right.X * localX + forward.X * localZ,
                Float32Scalar.Zero,
                right.Y * localX + forward.Y * localZ);
        }

        static Float32Scalar SampleCumulative(
            ProgramCurve curve,
            Float32Scalar time,
            Float32Scalar duration,
            bool looping)
        {
            if (!looping)
                return curve.Evaluate(Float32Scalar.Clamp(time, Float32Scalar.Zero, duration), Float32Scalar.Zero);
            int cycle = (int)Math.Floor((time / duration).ToDouble());
            Float32Scalar localTime = time - duration * Float32Scalar.FromInt64(cycle);
            Float32Scalar total = curve.Evaluate(duration, Float32Scalar.Zero);
            return total * Float32Scalar.FromInt64(cycle) + curve.Evaluate(localTime, Float32Scalar.Zero);
        }
    }
}
