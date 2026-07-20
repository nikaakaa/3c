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
        readonly List<MotionWarpSample<FixedScalar>> m_WarpSamples;
        readonly FixedMotionWarpTarget m_MotionWarp;

        public FixedMotionAccumulator(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            FixedActionStateStore actions,
            FixedStatePort modifierState,
            List<SimulationMotionContribution> contributions,
            List<MotionWarpSample<FixedScalar>> warpSamples)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
            m_WarpSamples = warpSamples ?? throw new ArgumentNullException(nameof(warpSamples));
            m_MotionWarp = new FixedMotionWarpTarget(access, frame, actions, modifierState);
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

        public void Submit(MotionWarpSample<FixedScalar> sample) => m_WarpSamples.Add(sample);

        public ResolvedGameplayMotion Resolve()
        {
            ResolvedMotionChannel locomotion = ResolveChannel(SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = ResolveChannel(SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = ResolveChannel(SimulationMotionChannel.GameplayResult);

            ProgramMotionModifierRuntime.ApplyActionWarp<FixedScalar, ResolvedMotionChannel, FixedMotionWarpTarget>(
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
                return new ResolvedMotionChannel(channel, FixedVector3.Zero, FixedScalar.Zero, false, false, OperationHandle.Invalid, string.Empty, OperationHandle.Invalid, 0, 0);

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
        IMotionModifierTarget<FixedScalar, ResolvedMotionChannel>
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedActionStateStore m_Actions;
        readonly FixedStatePort m_State;

        public FixedMotionWarpTarget(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            FixedActionStateStore actions,
            FixedStatePort state)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
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
            MotionWarpSample<FixedScalar> sample,
            ref ResolvedMotionChannel channel)
        {
            if (m_Actions.FindActive(descriptor.ActionContextIdentity, out FixedActionInstanceState action) < 0 || !action.IsActive)
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, $"Action Context '{descriptor.ActionContextIdentity}' has no active Action instance.");
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

            var currentAction = new TimelineActionContextIdentity(
                action.ActionId,
                action.ContextId,
                action.InstanceId,
                action.PredictionKey);
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
            ProgramCurve positionProgressCurve = Curve(descriptor.PositionProgressCurveConstantIndex, "PositionProgressCurve");
            ProgramCurve yawProgressCurve = Curve(descriptor.YawProgressCurveConstantIndex, "YawProgressCurve");
            FixedScalar previousPositionProgress;
            FixedScalar previousYawProgress;
            FixedVector3 totalPositionCorrection;
            FixedScalar totalYawCorrection;
            FixedVector3 nominalEnd;
            FixedVector3 desiredPosition;

            if (lifecycle == MotionWarpLifecycleDecision.Initialize)
            {
                CalculateTotalCorrection(
                    descriptor,
                    sample.Segment.Previous,
                    action.TargetSnapshot,
                    out totalPositionCorrection,
                    out totalYawCorrection,
                    out nominalEnd,
                    out desiredPosition);
                previousPositionProgress = SampleProgress(positionProgressCurve, previousProgress);
                previousYawProgress = SampleProgress(yawProgressCurve, previousProgress);
                WriteInitialized(
                    descriptor,
                    sample.PlaybackGeneration,
                    action,
                    totalPositionCorrection,
                    totalYawCorrection,
                    previousPositionProgress,
                    previousYawProgress);
            }
            else
            {
                totalPositionCorrection = Read(descriptor, ProgramStateSemantic.MotionWarpTotalPlanarCorrection).Vector3;
                totalYawCorrection = Read(descriptor, ProgramStateSemantic.MotionWarpTotalYawCorrection).Yaw.Degrees;
                previousPositionProgress = Read(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress).Scalar;
                previousYawProgress = Read(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress).Scalar;
                RequireProgress(previousPositionProgress, descriptor, "position");
                RequireProgress(previousYawProgress, descriptor, "yaw");
                nominalEnd = m_Frame.Body.Position + channel.Displacement;
                desiredPosition = nominalEnd + totalPositionCorrection;
            }

            FixedScalar positionProgress = SampleProgress(positionProgressCurve, currentProgress);
            FixedScalar yawProgress = SampleProgress(yawProgressCurve, currentProgress);
            if (positionProgress < previousPositionProgress || yawProgress < previousYawProgress)
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, "Cumulative MotionWarp progress moved backwards within one playback generation.");
            FixedVector3 positionDelta = totalPositionCorrection * (positionProgress - previousPositionProgress);
            FixedScalar yawDelta = totalYawCorrection * (yawProgress - previousYawProgress);
            channel.ApplyCorrection(positionDelta, yawDelta);
            Write(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress, CharacterStateValue.FromScalar(positionProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress, CharacterStateValue.FromScalar(yawProgress));
            Trace(
                descriptor,
                "motion_warp_applied",
                SimulationTraceSeverity.Information,
                $"source={descriptor.SourceMotionOperation};action={action.InstanceId};target={action.TargetSnapshot.TargetId};nominalEnd={nominalEnd};desired={desiredPosition};totalPosition={totalPositionCorrection};totalYaw={totalYawCorrection};positionProgress={positionProgress};yawProgress={yawProgress};positionDelta={positionDelta};yawDelta={yawDelta}");
        }

        public void Fail(string code, ProgramMotionModifierDescriptor descriptor, string detail)
        {
            Trace(descriptor, code, SimulationTraceSeverity.Error, detail);
            throw new InvalidOperationException($"{code}: {detail}");
        }

        void CalculateTotalCorrection(
            ProgramMotionModifierDescriptor descriptor,
            FixedScalar sourceTime,
            SimulationActionTargetSnapshot target,
            out FixedVector3 positionCorrection,
            out FixedScalar yawCorrection,
            out FixedVector3 nominalEnd,
            out FixedVector3 desiredPosition)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            FixedScalar sourceStart = ClipTime(source, TimelineClipTimePoint.Start);
            FixedScalar sourceEnd = ClipTime(source, TimelineClipTimePoint.CurveEnd);
            FixedScalar duration = FixedScalar.Max(FixedScalar.FromRatio(1, 1000000), sourceEnd - sourceStart);
            FixedScalar normalized = FixedScalar.Clamp((sourceTime - sourceStart) / duration, FixedScalar.Zero, FixedScalar.One);
            FixedVector3 remaining = SampleSourcePosition(source, FixedScalar.One) - SampleSourcePosition(source, normalized);
            FixedScalar remainingYaw = SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, FixedScalar.One) -
                                     SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, normalized);
            if (CatalogInt32(source, ProgramCatalogFieldId.Space) == 0)
                remaining = FixedAngle.RotatePlanar(remaining, m_Frame.Body.Yaw);
            nominalEnd = m_Frame.Body.Position + remaining;

            ProgramConstant offsetConstant = RequireConstant(descriptor.TargetLocalPlanarOffsetConstantIndex, ProgramConstantKind.Vector2, "TargetLocalPlanarOffset");
            FixedVector2 offset = offsetConstant.Vector2;
            FixedVector3 worldOffset = FixedAngle.RotatePlanar(
                new FixedVector3(offset.X, FixedScalar.Zero, offset.Y),
                target.Yaw);
            desiredPosition = descriptor.PositionMode == ProgramMotionWarpPositionMode.MatchTargetPlanarPosition
                ? new FixedVector3(target.Position.X + worldOffset.X, nominalEnd.Y, target.Position.Z + worldOffset.Z)
                : new FixedVector3(nominalEnd.X, nominalEnd.Y, nominalEnd.Z);
            FixedVector3 rawPosition = descriptor.PositionMode == ProgramMotionWarpPositionMode.MatchTargetPlanarPosition
                ? new FixedVector3(desiredPosition.X - nominalEnd.X, FixedScalar.Zero, desiredPosition.Z - nominalEnd.Z)
                : FixedVector3.Zero;
            FixedScalar positionWeight = Scalar(descriptor.PositionWeightConstantIndex, "PositionWeight");
            FixedScalar maximumPosition = Scalar(descriptor.MaximumPositionCorrectionConstantIndex, "MaxTotalPositionCorrection");
            positionCorrection = ClampMagnitude(rawPosition * positionWeight, maximumPosition);

            FixedYaw nominalYaw = new FixedYaw(m_Frame.Body.Yaw.Degrees + remainingYaw);
            FixedScalar yawOffset = Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees");
            FixedYaw desiredYaw = nominalYaw;
            if (descriptor.RotationMode == ProgramMotionWarpRotationMode.MatchTargetYaw)
            {
                desiredYaw = new FixedYaw(target.Yaw.Degrees + yawOffset);
            }
            else if (descriptor.RotationMode == ProgramMotionWarpRotationMode.FaceTarget)
            {
                FixedScalar directionX = target.Position.X - desiredPosition.X;
                FixedScalar directionZ = target.Position.Z - desiredPosition.Z;
                if (directionX == FixedScalar.Zero && directionZ == FixedScalar.Zero)
                    Fail(MotionModifierDiagnosticCode.FaceTargetZeroDirection, descriptor, "FaceTarget desired actor position equals the target planar position.");
                desiredYaw = new FixedYaw(FixedAngle.FromPlanarDirection(directionX, directionZ).Degrees + yawOffset);
            }
            FixedScalar rawYaw = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? FixedScalar.Zero
                : FixedAngle.Delta(nominalYaw, desiredYaw);
            FixedScalar yawWeight = Scalar(descriptor.YawWeightConstantIndex, "YawWeight");
            FixedScalar maximumYaw = Scalar(descriptor.MaximumYawCorrectionConstantIndex, "MaxTotalYawCorrectionDegrees");
            yawCorrection = FixedScalar.Clamp(rawYaw * yawWeight, -maximumYaw, maximumYaw);
        }

        void WriteInitialized(
            ProgramMotionModifierDescriptor descriptor,
            ulong playbackGeneration,
            FixedActionInstanceState action,
            FixedVector3 totalPositionCorrection,
            FixedScalar totalYawCorrection,
            FixedScalar positionProgress,
            FixedScalar yawProgress)
        {
            Write(descriptor, ProgramStateSemantic.MotionWarpActive, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpInitialized, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpPlaybackGeneration, CharacterStateValue.FromUInt64(playbackGeneration));
            Write(descriptor, ProgramStateSemantic.MotionWarpActionInstance, CharacterStateValue.FromActionInstanceReference(FixedActionInstanceReference.FromInstance(action)));
            Write(descriptor, ProgramStateSemantic.MotionWarpWindowStartPosition, CharacterStateValue.FromVector3(m_Frame.Body.Position));
            Write(descriptor, ProgramStateSemantic.MotionWarpWindowStartYaw, CharacterStateValue.FromYaw(m_Frame.Body.Yaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpTotalPlanarCorrection, CharacterStateValue.FromVector3(totalPositionCorrection));
            Write(descriptor, ProgramStateSemantic.MotionWarpTotalYawCorrection, CharacterStateValue.FromYaw(new FixedYaw(totalYawCorrection)));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress, CharacterStateValue.FromScalar(positionProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress, CharacterStateValue.FromScalar(yawProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpSourceOperation, CharacterStateValue.FromInt32(descriptor.SourceMotionOperation.Value));
        }

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

