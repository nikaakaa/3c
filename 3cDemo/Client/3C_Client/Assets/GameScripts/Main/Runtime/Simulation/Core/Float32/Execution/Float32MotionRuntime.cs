using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct ResolvedGameplayMotion
    {
        public ResolvedGameplayMotion(Float32Vector3 displacement, Float32Scalar yawDegrees, bool hasMotion)
        {
            Displacement = displacement;
            YawDegrees = yawDegrees;
            HasMotion = hasMotion;
        }

        public Float32Vector3 Displacement { get; }
        public Float32Scalar YawDegrees { get; }
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
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
            SimulationMotionContributionSpace space,
            Float32Scalar weight,
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
            Weight = Float32Scalar.Clamp(weight, Float32Scalar.Zero, Float32Scalar.One);
            Priority = priority;
            Channel = channel;
            BlendMode = blendMode;
            ConsumeLowerChannels = consumeLowerChannels;
        }

        public string SourceIdentity { get; }
        public OperationHandle SourceOperation { get; }
        public Float32Vector3 Displacement { get; }
        public Float32Scalar YawDegrees { get; }
        public SimulationMotionContributionSpace Space { get; }
        public Float32Scalar Weight { get; }
        public int Priority { get; }
        public SimulationMotionChannel Channel { get; }
        public SimulationMotionBlendMode BlendMode { get; }
        public bool ConsumeLowerChannels { get; }
        public bool HasDelta => Weight > Float32Scalar.Zero &&
            (Displacement != Float32Vector3.Zero || YawDegrees != Float32Scalar.Zero);
        public bool ClaimsLowerChannels => Weight > Float32Scalar.Zero &&
            BlendMode == SimulationMotionBlendMode.Override &&
            ConsumeLowerChannels;
        public bool CanResolve => HasDelta || ClaimsLowerChannels;
    }

    internal struct ResolvedMotionChannel
    {
        public ResolvedMotionChannel(
            SimulationMotionChannel channel,
            Float32Vector3 displacement,
            Float32Scalar yawDegrees,
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
        public Float32Vector3 Displacement { get; private set; }
        public Float32Scalar YawDegrees { get; private set; }
        public bool HasContribution { get; }
        public bool ClaimsLowerChannels { get; }
        public OperationHandle ResolvedOwnerOperation { get; }
        public string ResolvedOwnerIdentity { get; }
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
        readonly List<MotionWarpSample<Float32Scalar>> m_WarpSamples;
        readonly Float32MotionWarpTarget m_MotionWarp;

        public Float32MotionAccumulator(
            Float32ProgramAccess access,
            Float32EvaluationFrame frame,
            Float32ActionStateStore actions,
            Float32StatePort modifierState,
            List<SimulationMotionContribution> contributions,
            List<MotionWarpSample<Float32Scalar>> warpSamples)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Contributions = contributions ?? throw new ArgumentNullException(nameof(contributions));
            m_WarpSamples = warpSamples ?? throw new ArgumentNullException(nameof(warpSamples));
            m_MotionWarp = new Float32MotionWarpTarget(access, frame, actions, modifierState);
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

        public void Submit(MotionWarpSample<Float32Scalar> sample) => m_WarpSamples.Add(sample);

        public ResolvedGameplayMotion Resolve()
        {
            ResolvedMotionChannel locomotion = ResolveChannel(SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = ResolveChannel(SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = ResolveChannel(SimulationMotionChannel.GameplayResult);

            ProgramMotionModifierRuntime.ApplyActionWarp<Float32Scalar, ResolvedMotionChannel, Float32MotionWarpTarget>(
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
            var motion = new ResolvedGameplayMotion(displacement, yaw, hasMotion);
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
                return new ResolvedMotionChannel(channel, Float32Vector3.Zero, Float32Scalar.Zero, false, false, OperationHandle.Invalid, string.Empty, OperationHandle.Invalid, 0, 0);

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

    internal sealed class Float32MotionWarpTarget : Float32OperationModule,
        IMotionModifierTarget<Float32Scalar, ResolvedMotionChannel>
    {
        readonly Float32EvaluationFrame m_Frame;
        readonly Float32ActionStateStore m_Actions;
        readonly Float32StatePort m_State;

        public Float32MotionWarpTarget(
            Float32ProgramAccess access,
            Float32EvaluationFrame frame,
            Float32ActionStateStore actions,
            Float32StatePort state)
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
            MotionWarpSample<Float32Scalar> sample,
            ref ResolvedMotionChannel channel)
        {
            if (m_Actions.FindActive(descriptor.ActionContextIdentity, out Float32ActionInstanceState action) < 0 || !action.IsActive)
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
            ProgramCurve positionProgressCurve = Curve(descriptor.PositionProgressCurveConstantIndex, "PositionProgressCurve");
            ProgramCurve yawProgressCurve = Curve(descriptor.YawProgressCurveConstantIndex, "YawProgressCurve");
            Float32Scalar previousPositionProgress;
            Float32Scalar previousYawProgress;
            Float32Vector3 totalPositionCorrection;
            Float32Scalar totalYawCorrection;
            Float32Vector3 nominalEnd;
            Float32Vector3 desiredPosition;

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

            Float32Scalar positionProgress = SampleProgress(positionProgressCurve, currentProgress);
            Float32Scalar yawProgress = SampleProgress(yawProgressCurve, currentProgress);
            if (positionProgress < previousPositionProgress || yawProgress < previousYawProgress)
                Fail(MotionModifierDiagnosticCode.InvalidState, descriptor, "Cumulative MotionWarp progress moved backwards within one playback generation.");
            Float32Vector3 positionDelta = totalPositionCorrection * (positionProgress - previousPositionProgress);
            Float32Scalar yawDelta = totalYawCorrection * (yawProgress - previousYawProgress);
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
            Float32Scalar sourceTime,
            SimulationActionTargetSnapshot target,
            out Float32Vector3 positionCorrection,
            out Float32Scalar yawCorrection,
            out Float32Vector3 nominalEnd,
            out Float32Vector3 desiredPosition)
        {
            ProgramCatalogEntry source = SourceCatalog(descriptor.SourceMotionOperation);
            Float32Scalar sourceStart = ClipTime(source, TimelineClipTimePoint.Start);
            Float32Scalar sourceEnd = ClipTime(source, TimelineClipTimePoint.CurveEnd);
            Float32Scalar duration = Float32Scalar.Max(Float32Scalar.FromSingle(0.000001f), sourceEnd - sourceStart);
            Float32Scalar normalized = Float32Scalar.Clamp((sourceTime - sourceStart) / duration, Float32Scalar.Zero, Float32Scalar.One);
            Float32Vector3 remaining = SampleSourcePosition(source, Float32Scalar.One) - SampleSourcePosition(source, normalized);
            Float32Scalar remainingYaw = SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, Float32Scalar.One) -
                                         SampleSourceCurve(source, ProgramCatalogFieldId.Yaw, normalized);
            if (CatalogInt32(source, ProgramCatalogFieldId.Space) == 0)
                remaining = Float32Angle.RotatePlanar(remaining, m_Frame.Body.Yaw);
            nominalEnd = m_Frame.Body.Position + remaining;

            ProgramConstant offsetConstant = RequireConstant(descriptor.TargetLocalPlanarOffsetConstantIndex, ProgramConstantKind.Vector2, "TargetLocalPlanarOffset");
            Float32Vector2 offset = offsetConstant.Vector2;
            Float32Vector3 worldOffset = Float32Angle.RotatePlanar(
                new Float32Vector3(offset.X, Float32Scalar.Zero, offset.Y),
                target.Yaw);
            desiredPosition = descriptor.PositionMode == ProgramMotionWarpPositionMode.MatchTargetPlanarPosition
                ? new Float32Vector3(target.Position.X + worldOffset.X, nominalEnd.Y, target.Position.Z + worldOffset.Z)
                : new Float32Vector3(nominalEnd.X, nominalEnd.Y, nominalEnd.Z);
            Float32Vector3 rawPosition = descriptor.PositionMode == ProgramMotionWarpPositionMode.MatchTargetPlanarPosition
                ? new Float32Vector3(desiredPosition.X - nominalEnd.X, Float32Scalar.Zero, desiredPosition.Z - nominalEnd.Z)
                : Float32Vector3.Zero;
            Float32Scalar positionWeight = Scalar(descriptor.PositionWeightConstantIndex, "PositionWeight");
            Float32Scalar maximumPosition = Scalar(descriptor.MaximumPositionCorrectionConstantIndex, "MaxTotalPositionCorrection");
            positionCorrection = ClampMagnitude(rawPosition * positionWeight, maximumPosition);

            Float32Yaw nominalYaw = new Float32Yaw(m_Frame.Body.Yaw.Degrees + remainingYaw);
            Float32Scalar yawOffset = Scalar(descriptor.TargetYawOffsetConstantIndex, "TargetYawOffsetDegrees");
            Float32Yaw desiredYaw = nominalYaw;
            if (descriptor.RotationMode == ProgramMotionWarpRotationMode.MatchTargetYaw)
            {
                desiredYaw = new Float32Yaw(target.Yaw.Degrees + yawOffset);
            }
            else if (descriptor.RotationMode == ProgramMotionWarpRotationMode.FaceTarget)
            {
                Float32Scalar directionX = target.Position.X - desiredPosition.X;
                Float32Scalar directionZ = target.Position.Z - desiredPosition.Z;
                if (directionX == Float32Scalar.Zero && directionZ == Float32Scalar.Zero)
                    Fail(MotionModifierDiagnosticCode.FaceTargetZeroDirection, descriptor, "FaceTarget desired actor position equals the target planar position.");
                desiredYaw = new Float32Yaw(Float32Angle.FromPlanarDirection(directionX, directionZ).Degrees + yawOffset);
            }
            Float32Scalar rawYaw = descriptor.RotationMode == ProgramMotionWarpRotationMode.Disabled
                ? Float32Scalar.Zero
                : Float32Angle.Delta(nominalYaw, desiredYaw);
            Float32Scalar yawWeight = Scalar(descriptor.YawWeightConstantIndex, "YawWeight");
            Float32Scalar maximumYaw = Scalar(descriptor.MaximumYawCorrectionConstantIndex, "MaxTotalYawCorrectionDegrees");
            yawCorrection = Float32Scalar.Clamp(rawYaw * yawWeight, -maximumYaw, maximumYaw);
        }

        void WriteInitialized(
            ProgramMotionModifierDescriptor descriptor,
            ulong playbackGeneration,
            Float32ActionInstanceState action,
            Float32Vector3 totalPositionCorrection,
            Float32Scalar totalYawCorrection,
            Float32Scalar positionProgress,
            Float32Scalar yawProgress)
        {
            Write(descriptor, ProgramStateSemantic.MotionWarpActive, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpInitialized, CharacterStateValue.FromBoolean(true));
            Write(descriptor, ProgramStateSemantic.MotionWarpPlaybackGeneration, CharacterStateValue.FromUInt64(playbackGeneration));
            Write(descriptor, ProgramStateSemantic.MotionWarpActionInstance, CharacterStateValue.FromActionInstanceReference(Float32ActionInstanceReference.FromInstance(action)));
            Write(descriptor, ProgramStateSemantic.MotionWarpWindowStartPosition, CharacterStateValue.FromVector3(m_Frame.Body.Position));
            Write(descriptor, ProgramStateSemantic.MotionWarpWindowStartYaw, CharacterStateValue.FromYaw(m_Frame.Body.Yaw));
            Write(descriptor, ProgramStateSemantic.MotionWarpTotalPlanarCorrection, CharacterStateValue.FromVector3(totalPositionCorrection));
            Write(descriptor, ProgramStateSemantic.MotionWarpTotalYawCorrection, CharacterStateValue.FromYaw(new Float32Yaw(totalYawCorrection)));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastPositionProgress, CharacterStateValue.FromScalar(positionProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpLastYawProgress, CharacterStateValue.FromScalar(yawProgress));
            Write(descriptor, ProgramStateSemantic.MotionWarpSourceOperation, CharacterStateValue.FromInt32(descriptor.SourceMotionOperation.Value));
        }

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
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            using Float32ValueInputLease inputs = m_Values.ReadInputs(cursor, operation);
            CharacterStateValue input = inputs.FindByKind(ProgramStateValueKind.Vector2);
            if (input.Kind != ProgramStateValueKind.Vector2)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has no Vector2 input.");
            Float32Vector2 move = input.Vector2;
            if (move.SqrMagnitude > Float32Scalar.One)
                move = move.Normalized;
            Float32Scalar delta = Float32Scalar.One / Float32Scalar.FromInt64(m_Program.Manifest.TickRate);
            ProgramConstant speedConstant = FindConstant(operation, OperationNamedConstant.MoveSpeed);
            ProgramConstant turnConstant = FindConstant(operation, OperationNamedConstant.TurnSpeedDegrees);
            if (speedConstant == null || speedConstant.Kind != ProgramConstantKind.Scalar ||
                turnConstant == null || turnConstant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"Locomotion operation '{SourcePath(operation)}' has invalid speed constants.");
            Float32Scalar speed = speedConstant.Scalar;
            Float32Scalar maxYaw = turnConstant.Scalar * delta;
            Float32Vector3 displacement = new Float32Vector3(
                move.X * speed * delta,
                Float32Scalar.Zero,
                move.Y * speed * delta);
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
                SimulationMotionContributionSpace.World,
                Float32Scalar.One,
                0,
                SimulationMotionChannel.Locomotion,
                SimulationMotionBlendMode.Override,
                false));
        }
    }
}
