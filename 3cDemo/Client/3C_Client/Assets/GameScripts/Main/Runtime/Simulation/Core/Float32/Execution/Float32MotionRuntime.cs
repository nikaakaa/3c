using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

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
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline,
            string actionOwnerIdentity,
            string gameplayResultOwnerIdentity)
        {
            Displacement = displacement;
            YawDegrees = yawDegrees;
            LocomotionPlanarBasis = locomotionPlanarBasis;
            HasMotion = hasMotion;
            MovementPlaybackClock = movementPlaybackClock;
            LocomotionTimeline = locomotionTimeline;
            ActionOwnerIdentity = actionOwnerIdentity ?? string.Empty;
            GameplayResultOwnerIdentity = gameplayResultOwnerIdentity ?? string.Empty;
        }

        public Float32Vector3 Displacement { get; }
        public Float32Scalar YawDegrees { get; }
        public Float32Vector2 LocomotionPlanarBasis { get; }
        public bool HasMotion { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public CommittedLocomotionPlanarMotionTimeline LocomotionTimeline { get; }
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
            SimulationExecutionSource source,
            CharacterSkillId abilityId,
            ulong sourceGeneration,
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            Float32Vector2 planarBasis,
            SimulationMotionContributionSpace space,
            Float32Scalar weight,
            int priority,
            SimulationMotionChannel channel,
            SimulationMotionBlendMode blendMode,
            bool consumeLowerChannels,
            CommittedMovementPlaybackClock movementPlaybackClock,
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline)
        {
            if (!source.IsValid)
                throw new ArgumentException("Motion contribution source is invalid.", nameof(source));
            Source = source;
            AbilityId = abilityId;
            SourceGeneration = sourceGeneration;
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
            if (channel == SimulationMotionChannel.Locomotion &&
                (!locomotionTimeline.IsValid || !locomotionTimeline.Matches(movementPlaybackClock)))
            {
                throw new ArgumentException("Locomotion motion contribution has no matching committed motion timeline.", nameof(locomotionTimeline));
            }
            if (channel != SimulationMotionChannel.Locomotion && locomotionTimeline.IsValid)
                throw new ArgumentException("Only the Locomotion motion channel may carry a committed motion timeline.", nameof(locomotionTimeline));
            LocomotionTimeline = locomotionTimeline;
        }

        public SimulationExecutionSource Source { get; }
        public CharacterSkillId AbilityId { get; }
        public ulong SourceGeneration { get; }
        public string SourceIdentity => Source.Identity;
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
        public CommittedLocomotionPlanarMotionTimeline LocomotionTimeline { get; }
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
            SimulationExecutionSource resolvedOwnerSource,
            CharacterSkillId resolvedOwnerAbilityId,
            CommittedMovementPlaybackClock movementPlaybackClock,
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline,
            Float32Vector3 resolvedOwnerDisplacement,
            Float32Scalar resolvedOwnerYawDegrees,
            SimulationExecutionSource traceSource,
            CharacterSkillId traceAbilityId,
            ulong traceSourceGeneration,
            int participatingSourceCount,
            ulong participatingSourceFingerprint)
        {
            Channel = channel;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            PlanarBasis = planarBasis;
            HasContribution = hasContribution;
            ClaimsLowerChannels = claimsLowerChannels;
            ResolvedOwnerSource = resolvedOwnerSource;
            ResolvedOwnerAbilityId = resolvedOwnerAbilityId;
            MovementPlaybackClock = movementPlaybackClock;
            LocomotionTimeline = locomotionTimeline;
            ResolvedOwnerDisplacement = resolvedOwnerDisplacement;
            ResolvedOwnerYawDegrees = resolvedOwnerYawDegrees;
            TraceSource = traceSource;
            TraceAbilityId = traceAbilityId;
            TraceSourceGeneration = traceSourceGeneration;
            ParticipatingSourceCount = participatingSourceCount;
            ParticipatingSourceFingerprint = participatingSourceFingerprint;
        }

        public SimulationMotionChannel Channel { get; }
        public Float32Vector3 Displacement { get; private set; }
        public Float32Scalar YawDegrees { get; private set; }
        public Float32Vector2 PlanarBasis { get; }
        public bool HasContribution { get; }
        public bool ClaimsLowerChannels { get; }
        public SimulationExecutionSource ResolvedOwnerSource { get; }
        public CharacterSkillId ResolvedOwnerAbilityId { get; }
        public string ResolvedOwnerIdentity => ResolvedOwnerSource.IsValid ? ResolvedOwnerSource.Identity : string.Empty;
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public CommittedLocomotionPlanarMotionTimeline LocomotionTimeline { get; }
        public Float32Vector3 ResolvedOwnerDisplacement { get; }
        public Float32Scalar ResolvedOwnerYawDegrees { get; }
        public SimulationExecutionSource TraceSource { get; }
        public CharacterSkillId TraceAbilityId { get; }
        public ulong TraceSourceGeneration { get; }
        public int ParticipatingSourceCount { get; }
        public ulong ParticipatingSourceFingerprint { get; }
        public bool HasDelta => Displacement != Float32Vector3.Zero || YawDegrees != Float32Scalar.Zero;

        public void ApplyCorrection(Float32Vector3 displacement, Float32Scalar yawDegrees)
        {
            Displacement += displacement;
            YawDegrees += yawDegrees;
        }
    }

    internal readonly struct Float32MotionWarpState
    {
        public Float32MotionWarpState(
            bool active,
            bool initialized,
            ulong playbackGeneration,
            Float32ActionInstanceReference actionInstance,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Vector3 sourceWindowStartPosition,
            Float32Scalar sourceWindowStartYaw,
            Float32Vector3 resolvedTargetPosition,
            Float32Yaw resolvedTargetYaw,
            ProgramMotionWarpLimitResult limitResult,
            Float32Vector3 previousWarpedPosition,
            Float32Yaw previousWarpedYaw,
            Float32Scalar lastPositionProgress,
            Float32Scalar lastYawProgress,
            OperationHandle sourceOperation)
        {
            Active = active;
            Initialized = initialized;
            PlaybackGeneration = playbackGeneration;
            ActionInstance = actionInstance;
            StartBodyPosition = startBodyPosition;
            StartBodyYaw = startBodyYaw;
            SourceWindowStartPosition = sourceWindowStartPosition;
            SourceWindowStartYaw = sourceWindowStartYaw;
            ResolvedTargetPosition = resolvedTargetPosition;
            ResolvedTargetYaw = resolvedTargetYaw;
            LimitResult = limitResult;
            PreviousWarpedPosition = previousWarpedPosition;
            PreviousWarpedYaw = previousWarpedYaw;
            LastPositionProgress = lastPositionProgress;
            LastYawProgress = lastYawProgress;
            SourceOperation = sourceOperation;
        }

        public bool Active { get; }
        public bool Initialized { get; }
        public ulong PlaybackGeneration { get; }
        public Float32ActionInstanceReference ActionInstance { get; }
        public Float32Vector3 StartBodyPosition { get; }
        public Float32Yaw StartBodyYaw { get; }
        public Float32Vector3 SourceWindowStartPosition { get; }
        public Float32Scalar SourceWindowStartYaw { get; }
        public Float32Vector3 ResolvedTargetPosition { get; }
        public Float32Yaw ResolvedTargetYaw { get; }
        public ProgramMotionWarpLimitResult LimitResult { get; }
        public Float32Vector3 PreviousWarpedPosition { get; }
        public Float32Yaw PreviousWarpedYaw { get; }
        public Float32Scalar LastPositionProgress { get; }
        public Float32Scalar LastYawProgress { get; }
        public OperationHandle SourceOperation { get; }

        public Float32MotionWarpState WithProgress(
            Float32Vector3 previousWarpedPosition,
            Float32Yaw previousWarpedYaw,
            Float32Scalar lastPositionProgress,
            Float32Scalar lastYawProgress) =>
            new Float32MotionWarpState(
                Active,
                Initialized,
                PlaybackGeneration,
                ActionInstance,
                StartBodyPosition,
                StartBodyYaw,
                SourceWindowStartPosition,
                SourceWindowStartYaw,
                ResolvedTargetPosition,
                ResolvedTargetYaw,
                LimitResult,
                previousWarpedPosition,
                previousWarpedYaw,
                lastPositionProgress,
                lastYawProgress,
                SourceOperation);
    }

    internal static class Float32CharacterMotionResolver
    {
        public static string FormatMovementClock(CommittedMovementPlaybackClock clock) =>
            clock.IsValid
                ? $"{clock.OwnerIdentity}@{clock.Generation}:{clock.ContinuousTicks}/{clock.TickRate}#tick{clock.AuthorityTick.Value}"
                : "none";

        public static ResolvedMotionChannel ResolveChannel(
            SimulationMotionContribution[] contributions,
            int contributionCount,
            Float32Yaw bodyYaw,
            SimulationMotionChannel channel)
        {
            Float32Vector3 additiveDisplacement = Float32Vector3.Zero;
            Float32Scalar additiveYaw = Float32Scalar.Zero;
            Float32Vector3 weightedDisplacement = Float32Vector3.Zero;
            Float32Scalar weightedYaw = Float32Scalar.Zero;
            Float32Scalar totalWeight = Float32Scalar.Zero;
            SimulationMotionContribution overrideWinner = default;
            Float32Vector3 overrideDisplacement = Float32Vector3.Zero;
            Float32Scalar overrideYaw = Float32Scalar.Zero;
            SimulationExecutionSource traceSource = default;
            CharacterSkillId traceAbilityId = default;
            ulong traceSourceGeneration = 0;
            int sourceCount = 0;
            ulong sourceFingerprint = 1469598103934665603UL;
            bool hasAdditive = false;
            bool hasWeighted = false;
            bool hasOverride = false;
            for (int i = 0; i < contributionCount; i++)
            {
                SimulationMotionContribution contribution = contributions[i];
                if (contribution.Channel != channel || !contribution.CanResolve)
                    continue;
                if (!traceSource.IsValid)
                {
                    traceSource = contribution.Source;
                    traceAbilityId = contribution.AbilityId;
                    traceSourceGeneration = contribution.SourceGeneration;
                }
                sourceCount++;
                sourceFingerprint = MixSource(sourceFingerprint, contribution.Source.Identity);
                Float32Vector3 resolved = contribution.Space == SimulationMotionContributionSpace.ActorLocal
                    ? Float32Angle.RotatePlanar(contribution.Displacement, bodyYaw)
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
                return new ResolvedMotionChannel(channel, Float32Vector3.Zero, Float32Scalar.Zero, Float32Vector2.Zero, false, false, default, default, default, default, Float32Vector3.Zero, Float32Scalar.Zero, default, default, 0, 0, 0);

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
                hasOverride ? overrideWinner.Source : default,
                hasOverride ? overrideWinner.AbilityId : default,
                movementPlaybackClock,
                channel == SimulationMotionChannel.Locomotion && hasOverride
                    ? overrideWinner.LocomotionTimeline
                    : default,
                hasOverride ? overrideDisplacement : Float32Vector3.Zero,
                hasOverride ? overrideYaw : Float32Scalar.Zero,
                hasOverride ? overrideWinner.Source : traceSource,
                hasOverride ? overrideWinner.AbilityId : traceAbilityId,
                hasOverride ? overrideWinner.SourceGeneration : traceSourceGeneration,
                sourceCount,
                sourceFingerprint);
            return result;
        }

        public static ResolvedGameplayMotion Compose(
            ResolvedMotionChannel locomotion,
            ResolvedMotionChannel action,
            ResolvedMotionChannel gameplayResult)
        {
            Float32Vector3 displacement = Float32Vector3.Zero;
            Float32Scalar yaw = Float32Scalar.Zero;
            ComposeChannel(locomotion, ref displacement, ref yaw);
            ComposeChannel(action, ref displacement, ref yaw);
            ComposeChannel(gameplayResult, ref displacement, ref yaw);
            return new ResolvedGameplayMotion(
                displacement,
                yaw,
                locomotion.PlanarBasis,
                displacement != Float32Vector3.Zero || yaw != Float32Scalar.Zero,
                locomotion.MovementPlaybackClock,
                locomotion.LocomotionTimeline,
                action.ResolvedOwnerIdentity,
                gameplayResult.ResolvedOwnerIdentity);
        }

        static void ComposeChannel(
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

        static ulong MixSource(ulong hash, string identity)
        {
            unchecked
            {
                for (int i = 0; i < identity.Length; i++)
                {
                    hash ^= identity[i];
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }

    internal sealed class Float32MotionAccumulator : Float32OperationModule,
        IFloat32MotionContributionSink
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32AbilityExecutionWorkspace m_Workspace;
        readonly List<AbilityTimelineLogicMotionWarp> m_TimelineMotionWarps;
        readonly Float32MotionWarpTarget m_MotionWarp;

        public Float32MotionAccumulator(
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionWorkspace workspace,
            List<AbilityTimelineLogicMotionWarp> timelineMotionWarps,
            Float32ActionStateStore actions)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_TimelineMotionWarps = timelineMotionWarps ?? throw new ArgumentNullException(nameof(timelineMotionWarps));
            m_MotionWarp = new Float32MotionWarpTarget(access, frame, actions);
        }

        public void Submit(SimulationMotionContribution contribution)
        {
            if (!contribution.CanResolve)
                return;
            m_Workspace.SubmitMotionContribution(contribution);
            if (m_Frame.Trace.Enabled)
            {
                m_Frame.Trace.Add(
                    contribution.Source,
                    "motion_contribution",
                    SimulationTraceSeverity.Detail,
                    $"channel={contribution.Channel};blend={contribution.BlendMode};priority={contribution.Priority};weight={contribution.Weight};delta={contribution.Displacement};yaw={contribution.YawDegrees};claim={contribution.ClaimsLowerChannels};movementClock={Float32CharacterMotionResolver.FormatMovementClock(contribution.MovementPlaybackClock)}",
                    contribution.SourceGeneration);
            }
        }

        public void ApplyTimelineMotionWarps(ref ResolvedMotionChannel action)
        {
            OperationHandle resolvedOwner = action.ResolvedOwnerAbilityId == m_Ability.AbilityId &&
                action.ResolvedOwnerSource.IsSkillOperation
                ? action.ResolvedOwnerSource.Operation
                : OperationHandle.Invalid;
            AbilityTimelineMotionWarpRuntime.ApplyDirect(
                m_Layout.TimelineMotionWarpCatalog,
                m_TimelineMotionWarps,
                resolvedOwner,
                ref action,
                m_MotionWarp);
        }

    }

    internal sealed class Float32MotionWarpTarget : Float32OperationModule,
        IAbilityTimelineMotionWarpTarget<ResolvedMotionChannel>
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32ActionStateStore m_Actions;
        readonly IFloat32SkillExecutionState m_SkillState;

        public Float32MotionWarpTarget(
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actions)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_SkillState = m_Frame.SkillState ?? throw new InvalidOperationException("MotionWarp target requires an active Skill state.");
        }

        public void ApplyTimelineMotionWarp(AbilityTimelineLogicMotionWarp warp, ref ResolvedMotionChannel channel)
        {
            if (m_Actions.FindActive(warp.ActionContextIdentity, out Float32ActionInstanceState action) < 0)
                FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                    $"Timeline MotionWarp Action Context '{warp.ActionContextIdentity}' is not active.");
            if (action.SkillId != warp.AbilityId)
                FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                    "Timeline MotionWarp Ability does not match the active Action.");
            var currentAction = new TimelineActionContextIdentity(
                action.ActionId,
                action.ContextId,
                action.InstanceId,
                action.PredictionKey,
                action.SkillId,
                action.SkillEntryOperation,
                action.SkillExecutionGeneration);
            Float32MotionWarpState storedState = m_SkillState.GetMotionWarpState(warp.StateOperation);
            var storedAction = storedState.ActionInstance.IsValid
                ? new TimelineActionContextIdentity(
                    storedState.ActionInstance.ActionId,
                    storedState.ActionInstance.ContextId,
                    storedState.ActionInstance.InstanceId,
                    storedState.ActionInstance.PredictionKey,
                    storedState.ActionInstance.SkillId,
                    storedState.ActionInstance.SkillEntryOperation,
                    storedState.ActionInstance.SkillExecutionGeneration)
                : default;
            MotionWarpLifecycleDecision lifecycle = MotionWarpRuntimeSemantics.ResolveLifecycle(
                storedState.Active,
                storedState.Initialized,
                storedState.PlaybackGeneration,
                storedAction,
                storedState.SourceOperation.Value,
                warp.PlaybackGeneration,
                currentAction,
                warp.Source.Operation);
            Float32Vector3 startBodyPosition;
            Float32Yaw startBodyYaw;
            Float32Vector3 sourceWindowStartPosition;
            Float32Scalar sourceWindowStartYaw;
            Float32Vector3 resolvedTargetPosition;
            Float32Yaw resolvedTargetYaw;
            ProgramMotionWarpLimitResult limitResult;
            Float32Vector3 previousWarpedPosition;
            Float32Yaw previousWarpedYaw;
            Float32Scalar previousPositionProgress;
            Float32Scalar previousYawProgress;
            if (lifecycle == MotionWarpLifecycleDecision.Initialize)
            {
                if (!action.TargetSnapshot.HasTarget)
                {
                    ActionTargetRequirement requirement = Access.Services.RequireAdmissionProfile(action.ActionId).TargetRequirement;
                    if (requirement == ActionTargetRequirement.OptionalSnapshot)
                    {
                        m_SkillState.SetMotionWarpState(warp.StateOperation, default);
                        return;
                    }
                    FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.TargetSnapshotRequired,
                        $"Action '{action.ActionId}' has no immutable target snapshot for requirement '{requirement}'.");
                }
                ResolveDirectTarget(warp, action.TargetSnapshot,
                    out startBodyPosition,
                    out startBodyYaw,
                    out sourceWindowStartPosition,
                    out sourceWindowStartYaw,
                    out resolvedTargetPosition,
                    out resolvedTargetYaw,
                    out limitResult);
                if (limitResult == ProgramMotionWarpLimitResult.PreservedByLimitPolicy)
                {
                    m_SkillState.SetMotionWarpState(warp.StateOperation, default);
                    return;
                }
                EvaluateDirectPose(warp,
                    warp.PreviousPositionX,
                    warp.PreviousPositionY,
                    warp.PreviousPositionZ,
                    warp.PreviousYawDegrees,
                    warp.PreviousPositionProgress,
                    warp.PreviousYawProgress,
                    warp.PreviousTime,
                    startBodyPosition,
                    startBodyYaw,
                    sourceWindowStartPosition,
                    sourceWindowStartYaw,
                    resolvedTargetPosition,
                    resolvedTargetYaw,
                    out previousWarpedPosition,
                    out previousWarpedYaw);
                previousPositionProgress = Float32Scalar.FromSingle(warp.PreviousPositionProgress.ToSingle());
                previousYawProgress = Float32Scalar.FromSingle(warp.PreviousYawProgress.ToSingle());
                storedState = new Float32MotionWarpState(
                        true,
                        true,
                        warp.PlaybackGeneration,
                        Float32ActionInstanceReference.FromInstance(action),
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
                        previousYawProgress,
                        warp.Source.Operation);
            }
            else
            {
                startBodyPosition = storedState.StartBodyPosition;
                startBodyYaw = storedState.StartBodyYaw;
                sourceWindowStartPosition = storedState.SourceWindowStartPosition;
                sourceWindowStartYaw = storedState.SourceWindowStartYaw;
                resolvedTargetPosition = storedState.ResolvedTargetPosition;
                resolvedTargetYaw = storedState.ResolvedTargetYaw;
                limitResult = storedState.LimitResult;
                previousWarpedPosition = storedState.PreviousWarpedPosition;
                previousWarpedYaw = storedState.PreviousWarpedYaw;
                previousPositionProgress = storedState.LastPositionProgress;
                previousYawProgress = storedState.LastYawProgress;
            }
            Float32Scalar positionProgress = Float32Scalar.FromSingle(warp.CurrentPositionProgress.ToSingle());
            Float32Scalar yawProgress = Float32Scalar.FromSingle(warp.CurrentYawProgress.ToSingle());
            if (positionProgress < previousPositionProgress || yawProgress < previousYawProgress)
                FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                    "Cumulative Timeline MotionWarp progress moved backwards.");
            EvaluateDirectPose(warp,
                warp.CurrentPositionX,
                warp.CurrentPositionY,
                warp.CurrentPositionZ,
                warp.CurrentYawDegrees,
                warp.CurrentPositionProgress,
                warp.CurrentYawProgress,
                warp.CurrentTime,
                startBodyPosition,
                startBodyYaw,
                sourceWindowStartPosition,
                sourceWindowStartYaw,
                resolvedTargetPosition,
                resolvedTargetYaw,
                out Float32Vector3 currentWarpedPosition,
                out Float32Yaw currentWarpedYaw);
            Float32Vector3 rawSourceDelta = Float32Angle.RotatePlanar(
                new Float32Vector3(
                    Float32Scalar.FromSingle((warp.CurrentPositionX - warp.PreviousPositionX).ToSingle()),
                    Float32Scalar.FromSingle((warp.CurrentPositionY - warp.PreviousPositionY).ToSingle()),
                    Float32Scalar.FromSingle((warp.CurrentPositionZ - warp.PreviousPositionZ).ToSingle())),
                m_Frame.BodyFacts.Yaw);
            Float32Scalar rawSourceYawDelta = Float32Scalar.FromSingle((warp.CurrentYawDegrees - warp.PreviousYawDegrees).ToSingle());
            channel.ApplyCorrection(
                currentWarpedPosition - previousWarpedPosition - rawSourceDelta,
                Float32Angle.Delta(previousWarpedYaw, currentWarpedYaw) - rawSourceYawDelta);
            m_SkillState.SetMotionWarpState(warp.StateOperation,
                storedState.WithProgress(
                    currentWarpedPosition,
                    currentWarpedYaw,
                    positionProgress,
                    yawProgress));
        }

        public void FailTimelineMotionWarp(OperationHandle operation, string code, string detail)
        {
            throw new InvalidOperationException($"{code}: {detail}");
        }

        void ResolveDirectTarget(
            AbilityTimelineLogicMotionWarp warp,
            SimulationActionTargetSnapshot target,
            out Float32Vector3 startBodyPosition,
            out Float32Yaw startBodyYaw,
            out Float32Vector3 sourceWindowStartPosition,
            out Float32Scalar sourceWindowStartYaw,
            out Float32Vector3 resolvedTargetPosition,
            out Float32Yaw resolvedTargetYaw,
            out ProgramMotionWarpLimitResult limitResult)
        {
            startBodyPosition = m_Frame.BodyFacts.Position;
            startBodyYaw = m_Frame.BodyFacts.Yaw;
            sourceWindowStartPosition = new Float32Vector3(
                Float32Scalar.FromSingle(warp.SourceStartPositionX.ToSingle()),
                Float32Scalar.FromSingle(warp.SourceStartPositionY.ToSingle()),
                Float32Scalar.FromSingle(warp.SourceStartPositionZ.ToSingle()));
            sourceWindowStartYaw = Float32Scalar.FromSingle(warp.SourceStartYawDegrees.ToSingle());
            Float32Vector3 sourceEnd = new Float32Vector3(
                Float32Scalar.FromSingle((warp.SourceEndPositionX - warp.SourceStartPositionX).ToSingle()),
                Float32Scalar.FromSingle((warp.SourceEndPositionY - warp.SourceStartPositionY).ToSingle()),
                Float32Scalar.FromSingle((warp.SourceEndPositionZ - warp.SourceStartPositionZ).ToSingle()));
            Float32Scalar sourceEndYaw = Float32Scalar.FromSingle((warp.SourceEndYawDegrees - warp.SourceStartYawDegrees).ToSingle());
            Float32Vector3 nominalSourceEndOffset = Float32Angle.RotatePlanar(sourceEnd, startBodyYaw);
            Float32Vector3 nominalSourceEnd = startBodyPosition + nominalSourceEndOffset;
            Float32Vector3 requestedTargetPosition = warp.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? nominalSourceEnd
                : ResolveDirectTargetPosition(warp, target, startBodyPosition, startBodyYaw, nominalSourceEnd.Y);
            Float32Vector3 requestedPositionCorrection = new Float32Vector3(
                requestedTargetPosition.X - nominalSourceEnd.X,
                Float32Scalar.Zero,
                requestedTargetPosition.Z - nominalSourceEnd.Z);
            Float32Scalar maximumPosition = warp.TranslationMode == ProgramMotionWarpTranslationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Scalar.FromSingle(warp.MaximumPositionCorrection);
            bool positionExceeded = warp.TranslationMode != ProgramMotionWarpTranslationMode.Disabled &&
                                    new Float32Vector2(requestedPositionCorrection.X, requestedPositionCorrection.Z).Magnitude > maximumPosition;
            Float32Vector3 effectivePositionCorrection = positionExceeded
                ? ClampMagnitude(requestedPositionCorrection, maximumPosition)
                : requestedPositionCorrection;
            resolvedTargetPosition = nominalSourceEnd + effectivePositionCorrection;
            Float32Yaw nominalSourceEndYaw = new Float32Yaw(startBodyYaw.Degrees + sourceEndYaw);
            Float32Yaw requestedTargetYaw = nominalSourceEndYaw;
            if (warp.RotationMode == ProgramMotionWarpRotationMode.MatchTargetYaw)
                requestedTargetYaw = new Float32Yaw(target.Yaw.Degrees + Float32Scalar.FromSingle(warp.TargetYawOffsetDegrees));
            else if (warp.RotationMode == ProgramMotionWarpRotationMode.FaceTarget)
            {
                Float32Scalar directionX = target.Position.X - resolvedTargetPosition.X;
                Float32Scalar directionZ = target.Position.Z - resolvedTargetPosition.Z;
                if (directionX == Float32Scalar.Zero && directionZ == Float32Scalar.Zero)
                    FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.FaceTargetZeroDirection,
                        "FaceTarget desired actor position equals the target planar position.");
                requestedTargetYaw = new Float32Yaw(
                    Float32Angle.FromPlanarDirection(directionX, directionZ).Degrees +
                    Float32Scalar.FromSingle(warp.TargetYawOffsetDegrees));
            }
            Float32Scalar requestedYawCorrection = warp.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Angle.Delta(nominalSourceEndYaw, requestedTargetYaw);
            Float32Scalar maximumYaw = warp.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Scalar.FromSingle(warp.MaximumYawCorrectionDegrees);
            if (warp.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                warp.RotationMethod == ProgramMotionWarpRotationMethod.ConstantRate)
            {
                Float32Scalar maximumRate = Float32Scalar.FromSingle(warp.MaximumYawRateDegreesPerSecond) *
                    Float32Scalar.FromSingle((warp.EndTime - warp.StartTime).ToSingle());
                maximumYaw = Float32Scalar.Min(maximumYaw, maximumRate);
            }
            bool yawExceeded = warp.RotationMode != ProgramMotionWarpRotationMode.Disabled &&
                               Float32Scalar.Abs(requestedYawCorrection) > maximumYaw;
            Float32Scalar effectiveYawCorrection = Float32Scalar.Clamp(requestedYawCorrection, -maximumYaw, maximumYaw);
            resolvedTargetYaw = new Float32Yaw(nominalSourceEndYaw.Degrees + effectiveYawCorrection);
            bool exceeded = positionExceeded || yawExceeded;
            limitResult = exceeded ? ProgramMotionWarpLimitResult.AppliedClamped : ProgramMotionWarpLimitResult.Applied;
            if (exceeded && warp.LimitPolicy == ProgramMotionWarpLimitPolicy.PreserveSource)
                limitResult = ProgramMotionWarpLimitResult.PreservedByLimitPolicy;
        }

        Float32Vector3 ResolveDirectTargetPosition(
            AbilityTimelineLogicMotionWarp warp,
            SimulationActionTargetSnapshot target,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Scalar targetY)
        {
            Float32Vector2 offset = new Float32Vector2(
                Float32Scalar.FromSingle(warp.TargetPlanarOffsetX),
                Float32Scalar.FromSingle(warp.TargetPlanarOffsetY));
            Float32Vector3 localOffset = new Float32Vector3(offset.X, Float32Scalar.Zero, offset.Y);
            Float32Vector3 worldOffset;
            switch (warp.TargetOffsetSpace)
            {
                case ProgramMotionWarpTargetOffsetSpace.TargetLocal:
                    worldOffset = Float32Angle.RotatePlanar(localOffset, target.Yaw);
                    break;
                case ProgramMotionWarpTargetOffsetSpace.ApproachDirection:
                {
                    Float32Vector2 outward = new Float32Vector2(
                        startBodyPosition.X - target.Position.X,
                        startBodyPosition.Z - target.Position.Z);
                    if (outward.SqrMagnitude == Float32Scalar.Zero)
                        FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.ApproachDirectionZero,
                            "ApproachDirection requires distinct target and warp-start planar positions.");
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
                    FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                        $"Unsupported target offset space '{warp.TargetOffsetSpace}'.");
                    return default;
            }
            return new Float32Vector3(
                target.Position.X + worldOffset.X,
                targetY,
                target.Position.Z + worldOffset.Z);
        }

        void EvaluateDirectPose(
            AbilityTimelineLogicMotionWarp warp,
            FixedScalar sourcePositionX,
            FixedScalar sourcePositionY,
            FixedScalar sourcePositionZ,
            FixedScalar sourceYawDegrees,
            FixedScalar positionProgress,
            FixedScalar yawProgress,
            FixedScalar sampleTime,
            Float32Vector3 startBodyPosition,
            Float32Yaw startBodyYaw,
            Float32Vector3 sourceWindowStartPosition,
            Float32Scalar sourceWindowStartYaw,
            Float32Vector3 resolvedTargetPosition,
            Float32Yaw resolvedTargetYaw,
            out Float32Vector3 warpedPosition,
            out Float32Yaw warpedYaw)
        {
            warpedPosition = default;
            warpedYaw = default;
            Float32Vector3 sourceRelative = new Float32Vector3(
                Float32Scalar.FromSingle(sourcePositionX.ToSingle()),
                Float32Scalar.FromSingle(sourcePositionY.ToSingle()),
                Float32Scalar.FromSingle(sourcePositionZ.ToSingle())) - sourceWindowStartPosition;
            Float32Vector3 sourceEndRelative = new Float32Vector3(
                Float32Scalar.FromSingle(warp.SourceEndPositionX.ToSingle()),
                Float32Scalar.FromSingle(warp.SourceEndPositionY.ToSingle()),
                Float32Scalar.FromSingle(warp.SourceEndPositionZ.ToSingle())) - sourceWindowStartPosition;
            Float32Scalar sourceYawRelative = Float32Scalar.FromSingle(sourceYawDegrees.ToSingle()) - sourceWindowStartYaw;
            Float32Scalar sourceEndYawRelative = Float32Scalar.FromSingle(warp.SourceEndYawDegrees.ToSingle()) - sourceWindowStartYaw;
            Float32Yaw nominalCurrentYaw = new Float32Yaw(startBodyYaw.Degrees + sourceYawRelative);
            Float32Yaw nominalEndYaw = new Float32Yaw(startBodyYaw.Degrees + sourceEndYawRelative);
            Float32Scalar finalYawCorrection = warp.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Angle.Delta(nominalEndYaw, resolvedTargetYaw);
            Float32Scalar currentYawCorrection;
            switch (warp.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? ProgramMotionWarpRotationMethod.ProgressCurve
                : warp.RotationMethod)
            {
                case ProgramMotionWarpRotationMethod.ProgressCurve:
                    currentYawCorrection = finalYawCorrection * Float32Scalar.FromSingle(yawProgress.ToSingle());
                    break;
                case ProgramMotionWarpRotationMethod.ConstantRate:
                {
                    Float32Scalar elapsed = Float32Scalar.Max(
                        Float32Scalar.Zero,
                        Float32Scalar.FromSingle((sampleTime - warp.StartTime).ToSingle()));
                    Float32Scalar maximum = Float32Scalar.FromSingle(warp.MaximumYawRateDegreesPerSecond) * elapsed;
                    currentYawCorrection = Float32Scalar.Clamp(finalYawCorrection, -maximum, maximum);
                    break;
                }
                case ProgramMotionWarpRotationMethod.ScaleSourceYaw:
                {
                    if (Float32Scalar.Abs(sourceEndYawRelative) <= Float32Scalar.FromSingle(0.000001f))
                        FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.ScaleSourceYawZero,
                            "ScaleSourceYaw requires non-zero source window yaw.");
                    Float32Scalar targetYawRelative = Float32Angle.Delta(startBodyYaw, resolvedTargetYaw);
                    currentYawCorrection = sourceYawRelative * (targetYawRelative / sourceEndYawRelative) - sourceYawRelative;
                    break;
                }
                default:
                    FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                        $"Unsupported rotation method '{warp.RotationMethod}'.");
                    return;
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
            switch (warp.TranslationMode)
            {
                case ProgramMotionWarpTranslationMode.Disabled:
                    warpedRelative = rotatedSource;
                    break;
                case ProgramMotionWarpTranslationMode.ScaleToTarget:
                    warpedRelative = ScalePlanarToTarget(rotatedSource, rotatedSourceEnd, targetRelative);
                    break;
                case ProgramMotionWarpTranslationMode.SkewToTarget:
                {
                    Float32Vector3 endpointCorrection = Planar(targetRelative - rotatedSourceEnd);
                    warpedRelative = rotatedSource + endpointCorrection * Float32Scalar.FromSingle(positionProgress.ToSingle());
                    break;
                }
                case ProgramMotionWarpTranslationMode.LinearToTarget:
                    warpedRelative = new Float32Vector3(
                        targetRelative.X * Float32Scalar.FromSingle(positionProgress.ToSingle()),
                        sourceRelative.Y,
                        targetRelative.Z * Float32Scalar.FromSingle(positionProgress.ToSingle()));
                    break;
                default:
                    FailTimelineMotionWarp(warp.StateOperation, MotionModifierDiagnosticCode.InvalidState,
                        $"Unsupported translation mode '{warp.TranslationMode}'.");
                    return;
            }
            warpedPosition = startBodyPosition + new Float32Vector3(
                warpedRelative.X,
                sourceRelative.Y,
                warpedRelative.Z);
        }

        static Float32Vector3 ScalePlanarToTarget(
            Float32Vector3 value,
            Float32Vector3 sourceEnd,
            Float32Vector3 targetEnd)
        {
            Float32Scalar denominator = sourceEnd.X * sourceEnd.X + sourceEnd.Z * sourceEnd.Z;
            if (denominator <= Float32Scalar.FromSingle(0.000001f))
                throw new InvalidOperationException("ScaleToTarget requires a non-zero source window planar endpoint.");
            Float32Scalar dot = sourceEnd.X * targetEnd.X + sourceEnd.Z * targetEnd.Z;
            Float32Scalar cross = sourceEnd.X * targetEnd.Z - sourceEnd.Z * targetEnd.X;
            return new Float32Vector3(
                (dot * value.X - cross * value.Z) / denominator,
                value.Y,
                (cross * value.X + dot * value.Z) / denominator);
        }

        static Float32Vector3 Planar(Float32Vector3 value) =>
            new Float32Vector3(value.X, Float32Scalar.Zero, value.Z);

        static Float32Vector3 ClampMagnitude(Float32Vector3 value, Float32Scalar maximum)
        {
            if (maximum <= Float32Scalar.Zero)
                return Float32Vector3.Zero;
            Float32Scalar magnitude = new Float32Vector2(value.X, value.Z).Magnitude;
            return magnitude > maximum ? value * (maximum / magnitude) : value;
        }
    }

    internal sealed class Float32LocomotionRuntime : Float32OperationModule
    {
        readonly IFloat32ValueInputReader m_Values;
        readonly IFloat32MotionContributionSink m_Motion;
        readonly Float32AbilityExecutionFrame m_Frame;

        public Float32LocomotionRuntime(
            Float32GameplayAbilityExecutionAccess access,
            IFloat32ValueInputReader values,
            IFloat32MotionContributionSink motion,
            Float32AbilityExecutionFrame frame)
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
            AbilityStateValue input = inputs.FindByKind(ProgramStateValueKind.Vector2);
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
                m_Ability.TickRate);
            Float32Scalar delta = Float32Scalar.One / Float32Scalar.FromInt64(m_Ability.TickRate);
            ProgramConstant turnConstant = FindConstant(operation, OperationNamedConstant.TurnSpeedDegrees);
            if (turnConstant == null || turnConstant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid turn speed.");
            Float32Scalar maxYaw = turnConstant.Scalar * delta;
            Float32Vector3 displacement = ResolveDisplacement(operation, move, delta, committedTicks - 1);
            Float32Scalar yaw = Float32Scalar.Zero;
            if (move != Float32Vector2.Zero && maxYaw > Float32Scalar.Zero)
            {
                Float32Yaw desired = Float32Angle.FromPlanarDirection(move);
                yaw = Float32Scalar.Clamp(Float32Angle.Delta(m_Frame.BodyFacts.Yaw, desired), -maxYaw, maxYaw);
            }
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline = ResolveMotionTimeline(
                cursor,
                operation,
                move,
                displacement,
                yaw,
                turnConstant.Scalar,
                delta,
                generation);
            m_Motion.Submit(new SimulationMotionContribution(
                SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)),
                m_Ability.AbilityId,
                generation,
                displacement,
                yaw,
                move,
				SimulationMotionContributionSpace.World,
				Float32Scalar.One,
				0,
				SimulationMotionChannel.Locomotion,
				SimulationMotionBlendMode.Override,
				false,
                movementPlaybackClock,
                locomotionTimeline));
        }

        CommittedLocomotionPlanarMotionTimeline ResolveMotionTimeline<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            Float32Vector2 move,
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            Float32Scalar maximumYawVelocityDegreesPerSecond,
            Float32Scalar delta,
            ulong generation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            int durationTicks = ResolveDurationTicks(operation);
            string continuationOwner = string.Empty;
            Float32Vector2 continuationVelocity = Float32Vector2.Zero;
            if ((LocomotionInputMotionExecutionMode)operation.Integer0 == LocomotionInputMotionExecutionMode.Timed)
            {
                ProgramControlFlowEdge transition = cursor.PredictCurrentStateRootCompletionTransition();
                if (transition != null &&
                    TryFindSingleLocomotion(transition.Target, out SimulationOperation continuation) &&
                    (LocomotionInputMotionExecutionMode)continuation.Integer0 == LocomotionInputMotionExecutionMode.Continuous &&
                    (LocomotionInputMotionDisplacementMode)continuation.Integer1 == LocomotionInputMotionDisplacementMode.ConstantSpeed)
                {
                    ProgramConstant speed = FindConstant(continuation, OperationNamedConstant.MoveSpeed);
                    if (speed == null || speed.Kind != ProgramConstantKind.Scalar || speed.Scalar < Float32Scalar.Zero)
                        throw new InvalidOperationException($"Locomotion continuation '{SourcePath(continuation)}' has invalid Move Speed.");
                    continuationOwner = SourcePath(continuation);
                    continuationVelocity = move * speed.Scalar;
                }
            }
            Float32Vector2 currentVelocity = delta > Float32Scalar.Zero
                ? new Float32Vector2(displacement.X / delta, displacement.Z / delta)
                : Float32Vector2.Zero;
            return new CommittedLocomotionPlanarMotionTimeline(
                SourcePath(operation),
                generation,
                m_Frame.Tick,
                m_Ability.TickRate,
                currentVelocity.X.ToSingle(),
                currentVelocity.Y.ToSingle(),
                (yawDegrees / delta).ToSingle(),
                maximumYawVelocityDegreesPerSecond.ToSingle(),
                durationTicks,
                continuationOwner,
                continuationVelocity.X.ToSingle(),
                continuationVelocity.Y.ToSingle());
        }

        int ResolveDurationTicks(SimulationOperation operation)
        {
            if ((LocomotionInputMotionExecutionMode)operation.Integer0 != LocomotionInputMotionExecutionMode.Timed)
                return 0;
            ProgramConstant duration = FindConstant(operation, OperationNamedConstant.DurationSeconds);
            if (duration == null || duration.Kind != ProgramConstantKind.Scalar || duration.Scalar <= Float32Scalar.Zero)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid duration.");
            return checked((int)Math.Ceiling(duration.Scalar.ToDouble() * m_Ability.TickRate));
        }

        bool TryFindSingleLocomotion(OperationHandle state, out SimulationOperation motion)
        {
            motion = null;
            if (!state.IsValid || Access.Topology.Operation(state).Code != SimulationOperationCode.State)
                return false;
            ProgramControlFlowEdge root = Access.Topology.StateRoot(state);
            if (root == null)
                return false;
            var pending = new Stack<OperationHandle>();
            pending.Push(root.Target);
            while (pending.Count != 0)
            {
                OperationHandle handle = pending.Pop();
                SimulationOperation candidate = Access.Operation(handle);
                if (candidate.Code == SimulationOperationCode.LocomotionInputMotion)
                {
                    if (motion != null)
                        return false;
                    motion = candidate;
                }
                IReadOnlyList<ProgramControlFlowEdge> children = Edges(handle, ProgramControlFlowKind.Child);
                for (int i = children.Count - 1; i >= 0; i--)
                    pending.Push(children[i].Target);
            }
            return motion != null;
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

            Float32Scalar tickRate = Float32Scalar.FromInt64(m_Ability.TickRate);
            Float32Scalar fromTime = Float32Scalar.FromInt64(elapsedTicks) / tickRate;
            Float32Scalar toTime = Float32Scalar.FromInt64(checked(elapsedTicks + 1)) / tickRate;
            bool looping = (LocomotionInputMotionExecutionMode)operation.Integer0 == LocomotionInputMotionExecutionMode.Continuous;
            Float32GameplayAbilityCurve xCurve = Access.Services.RequireTimelineCurve(xConstant, xConstant.Identity);
            Float32GameplayAbilityCurve zCurve = Access.Services.RequireTimelineCurve(zConstant, zConstant.Identity);
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
            Float32GameplayAbilityCurve curve,
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

