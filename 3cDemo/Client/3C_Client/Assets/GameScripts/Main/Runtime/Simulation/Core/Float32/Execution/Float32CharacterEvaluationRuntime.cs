using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal static class Float32CharacterEvaluationRuntime
    {
        public static Float32CharacterEvaluationResult Evaluate(
            Float32CharacterRuntime characterRuntime,
            SimulationActorBinding actor,
            IReadOnlyList<Float32GraphValueWorkspace> valueWorkspaces,
            Float32CharacterRuntimeState sourceState,
            SimulationTick tick,
            SimulationInput input,
            SimulationIngress[] ingress,
            int ingressCount,
            WorldBodyState beforeBody,
            bool diagnosticsEnabled,
            bool captureValues,
            bool captureControlFlow,
            out CharacterWorldSolveRequest worldRequest)
        {
            if (characterRuntime == null)
                throw new ArgumentNullException(nameof(characterRuntime));
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (sourceState == null)
                throw new ArgumentNullException(nameof(sourceState));
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (sourceState.NumericProfile != characterRuntime.NumericProfile ||
                beforeBody.ActorId != actor.ActorId ||
                input.NumericProfile != characterRuntime.NumericProfile)
                throw new InvalidOperationException("Float32 Character evaluation identity does not match the active Character Runtime.");

            Float32GameplayEffectRuntimeCatalog effectCatalog = actor.AbilityInstallations.GameplayEffectCatalog;
            var roleState = new Float32CharacterRuntimeStateTransaction(
                sourceState,
                tick,
                characterRuntime.TickRate,
                effectCatalog);
            Float32AbilityInvocationRuntime[] invocations = actor.InvocationScratch;
            int invocationCount = 0;
            Dictionary<CharacterSkillId, IFloat32AbilityActionControlPort> actionRuntimes = actor.ActionRuntimes;
            actionRuntimes.Clear();
            Float32GameplayEffectExecutionScratch sharedEffectScratch = actor.EffectExecutionScratch;
            sharedEffectScratch.Reset();
            var timelineLogicMotion = new List<AbilityTimelineLogicMotion>();
            var timelineLogicMotionWarps = new List<AbilityTimelineLogicMotionWarp>();
            var timelineAdvances = new List<AbilityTimelineAdvancePending>();
            var timelineStops = new List<AbilityTimelineStopPending>();
            Float32CharacterEvaluationOutput evaluationOutput = actor.EvaluationOutput;
            evaluationOutput.Clear();
            List<GameplayFact> facts = evaluationOutput.Facts;
            List<PresentationCommand> presentation = evaluationOutput.Presentation;
            List<SimulationTraceRecord> trace = evaluationOutput.Trace;
            List<SimulationTraceRecord> characterTrace = evaluationOutput.CharacterTrace;
            try
            {
                IFloat32AbilityExecutionServiceFactory serviceFactory = actor.ServiceFactory;
                IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory = actor.DomainRuntimeFactory;
                Float32AbilityExecutionInput abilityInput = actor.AbilityExecutionInput;
                abilityInput.Begin(input.Sequence, input.Values);
                var bodyFacts = new Float32AbilityBodyFacts(actor.ActorId, beforeBody);
                Float32MotionContributionScratch motionContributions = actor.MotionContributions;
                motionContributions.Begin();
                Float32CharacterControlMotionRuntime controlMotion = actor.ControlMotion;
                controlMotion.Begin(
                    abilityInput,
                    bodyFacts,
                    tick,
                    characterRuntime.TickRate);
                Float32CharacterTraceSink characterTraceSink = actor.CharacterTraceSink;
                characterTraceSink.Begin(
                    characterRuntime.NumericProfile,
                    actor.GameplayContentHash,
                    actor.ActorId,
                    tick,
                    diagnosticsEnabled);
                for (int i = 0; i < actor.AbilityInstallations.Installations.Count; i++)
                {
                    Float32GameplayAbilityExecutionInstallation installation = actor.AbilityInstallations.Installations[i];
                    var invocation = new Float32AbilityInvocationRuntime(
                        installation.Execution,
                        actor.AbilityInstallations,
                        domainRuntimeFactory,
                        installation.EquipmentLayout,
                        roleState.BindAbility(installation.Identity, installation.Layout, installation.Data),
                        roleState,
                        roleState.InputRequests,
                        roleState.ActionState,
                        roleState.HandleAllocatorState,
                        roleState.EventSequenceState,
                        roleState.GameplayEffectState,
                        roleState.EquipmentState,
                        actor.ActorId,
                        tick,
                        abilityInput,
                        bodyFacts,
                        new Float32AbilityExecutionWorkspace(sharedEffectScratch, timelineAdvances, timelineStops, valueWorkspaces[i]),
                        serviceFactory);
                    invocations[invocationCount++] = invocation;
                    invocation.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
                    actionRuntimes.Add(invocation.AbilityId, invocation.Actions);
                }

                characterRuntime.InputRuntime.Begin(roleState.InputRequests);
                characterRuntime.InputRuntime.ApplyRequests(input.Requests);

                bool effectAdvanced = false;
                for (int i = 0; i < invocationCount; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
                    ApplyIngress(invocation, ingress, ingressCount, sourceState);
                    if (!effectAdvanced && invocation.HasGameplayEffects)
                    {
                        ApplyGameplayEffectIngress(invocation, ingress, ingressCount);
                        invocation.AdvanceGameplayEffects();
                        effectAdvanced = true;
                    }
                    invocation.ApplyInputBindings();
                }

                if (!effectAdvanced)
                    RequireNoGameplayEffectIngress(ingress, ingressCount);

                var control = new Float32CharacterControlRuntime(
                    characterRuntime.ControlModules,
                    actor.ControlRuntimeBinding,
                    roleState,
                    roleState.InputRequests,
                    roleState.ActionState,
                    actor.ActorId,
                    tick,
                    characterRuntime.TickRate,
                    abilityInput,
                    bodyFacts,
                    controlMotion,
                    characterTraceSink,
                    actionRuntimes,
                    (skill, window) => IsActionWindowActive(invocations, invocationCount, skill, window),
                    route => ReadEquipmentActionContext(invocations, invocationCount, route));
                control.Tick();
                controlMotion.CopyContributionsTo(motionContributions);

                for (int i = 0; i < invocationCount; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
                    invocation.Tick();
                    invocation.CopyMotionContributionsTo(motionContributions);
                }

                for (int i = 0; i < timelineAdvances.Count; i++)
                {
                    actor.TimelineMotionReader.CopyPendingMotion(
                        timelineAdvances[i].RuntimeHandle,
                        timelineLogicMotion);
                    AppendTimelineMotion(motionContributions, timelineLogicMotion);
                }
                for (int i = 0; i < invocationCount; i++)
                    invocations[i].ClearTimelineMotionWarps();
                for (int advanceIndex = 0; advanceIndex < timelineAdvances.Count; advanceIndex++)
                {
                    actor.TimelineMotionWarpReader.CopyPendingMotionWarps(
                        timelineAdvances[advanceIndex].RuntimeHandle,
                        timelineLogicMotionWarps);
                    for (int i = 0; i < invocationCount; i++)
                    {
                        Float32AbilityInvocationRuntime invocation = invocations[i];
                        for (int warpIndex = 0; warpIndex < timelineLogicMotionWarps.Count; warpIndex++)
                        {
                            if (timelineLogicMotionWarps[warpIndex].AbilityId != invocation.AbilityId)
                                continue;
                            invocation.AddTimelineMotionWarp(timelineLogicMotionWarps[warpIndex]);
                        }
                    }
                }

                ResolvedGameplayMotion gameplayMotion = ResolveMotion(
                    motionContributions.Values,
                    motionContributions.Count,
                    beforeBody.Yaw,
                    invocations,
                    invocationCount,
                    characterTraceSink);
                for (int i = 0; i < invocationCount; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
                    invocation.Complete(facts, presentation, trace);
                    invocation.Accept(roleState.AcceptAbility);
                }
                trace.AddRange(characterTrace);
                Float32Scalar tickDelta = Float32Scalar.One / Float32Scalar.FromInt64(characterRuntime.TickRate);
                BodyMotionPrepareResult bodyMotion = CharacterBodyMotionRuntime.Prepare(
                    actor.ActorId,
                    tick,
                    beforeBody,
                    gameplayMotion,
                    actor.BodyMotionBinding,
                    tickDelta);
                WorldCapability requiredCapabilities = characterRuntime.RequiredWorldCapabilities |
                    actor.BodyMotionBinding.RequiredWorldCapability;
                worldRequest = new CharacterWorldSolveRequest(
                    characterRuntime.NumericProfile,
                    actor.ActorId,
                    new WorldRequestId(actor.ActorId, tick, 1),
                    tick,
                    beforeBody,
                    bodyMotion.Motion,
                    bodyMotion.Plan,
                    requiredCapabilities);
                Float32CharacterRuntimeState candidateState = roleState.Commit();
                roleState.Dispose();
                var result = new Float32CharacterEvaluationResult(
                    actor.ActorId,
                    tick,
                    candidateState,
                    facts.ToArray(),
                    presentation.ToArray(),
                    trace.ToArray(),
                    actor.TimelineRuntime,
                    timelineAdvances.ToArray(),
                    timelineStops.ToArray());
                actor.ClearInvocationScratch(invocationCount);
                actor.ClearActionRuntimes();
                sharedEffectScratch.Reset();
                evaluationOutput.Clear();
                abilityInput.Clear();
                return result;
            }
            catch
            {
                DiscardTimelineAdvances(actor.TimelineRuntime, timelineAdvances);
                DiscardTimelineStops(actor.TimelineRuntime, timelineStops);
                for (int i = 0; i < invocationCount; i++)
                    invocations[i].Dispose();
                actor.ClearInvocationScratch(invocationCount);
                actor.ClearActionRuntimes();
                sharedEffectScratch.Reset();
                evaluationOutput.Clear();
                abilityInput.Clear();
                actor.ControlMotion.ClearContributions();
                actor.MotionContributions.Clear();
                roleState.Dispose();
                throw;
            }
        }

        static void DiscardTimelineStops(
            IAbilityTimelineRuntime timelineRuntime,
            IReadOnlyList<AbilityTimelineStopPending> stops)
        {
            if (timelineRuntime == null)
                return;
            for (int i = 0; i < stops.Count; i++)
            {
                if (!stops[i].IsValid)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline stop.");
                timelineRuntime.DiscardStop(stops[i]);
            }
        }

        static void DiscardTimelineAdvances(
            IAbilityTimelineRuntime timelineRuntime,
            IReadOnlyList<AbilityTimelineAdvancePending> advances)
        {
            if (timelineRuntime == null)
                return;
            for (int i = 0; i < advances.Count; i++)
            {
                if (!advances[i].IsValid)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline advance.");
                timelineRuntime.Discard(advances[i]);
            }
        }

        static void ApplyIngress(
            Float32AbilityInvocationRuntime invocation,
            SimulationIngress[] ingress,
            int ingressCount,
            Float32CharacterRuntimeState sourceState)
        {
            for (int i = 0; i < ingressCount; i++)
            {
                SimulationIngress value = ingress[i];
                if (value.Header.Kind != SimulationIngressKind.ActionLifecycle ||
                    !OwnsAction(sourceState, value.ActionLifecycle.ActionInstanceId, invocation.AbilityId))
                    continue;
                invocation.ApplyActionIngress(value);
            }
        }

        static void ApplyGameplayEffectIngress(
            Float32AbilityInvocationRuntime invocation,
            SimulationIngress[] ingress,
            int ingressCount)
        {
            for (int i = 0; i < ingressCount; i++)
                if (ingress[i].Header.Kind != SimulationIngressKind.ActionLifecycle)
                    invocation.ApplyGameplayEffectIngress(ingress[i]);
        }

        static void RequireNoGameplayEffectIngress(SimulationIngress[] ingress, int ingressCount)
        {
            for (int i = 0; i < ingressCount; i++)
                if (ingress[i].Header.Kind != SimulationIngressKind.ActionLifecycle)
                    throw new InvalidOperationException(
                        "Float32 Character evaluation received Gameplay Effect ingress without an installed Gameplay Effect service.");
        }

        static bool OwnsAction(
            Float32CharacterRuntimeState state,
            ulong actionInstanceId,
            CharacterSkillId abilityId)
        {
            for (int i = 0; i < state.ActionInstances.Count; i++)
            {
                Float32ActionInstanceState action = state.ActionInstances[i];
                if (action.InstanceId == actionInstanceId)
                    return action.SkillId == abilityId;
            }
            return false;
        }

        static bool IsActionWindowActive(
            Float32AbilityInvocationRuntime[] invocations,
            int invocationCount,
            CharacterSkillId skillId,
            string windowType)
        {
            for (int i = 0; i < invocationCount; i++)
            {
                Float32AbilityInvocationRuntime invocation = invocations[i];
                if (invocation.AbilityId != skillId)
                    continue;
                return invocation.HasActionWindowProjection(windowType);
            }
            return false;
        }

        static (bool Found, EquipmentActionContext Context) ReadEquipmentActionContext(
            Float32AbilityInvocationRuntime[] invocations,
            int invocationCount,
            EquipmentActionRouteId route)
        {
            for (int i = 0; i < invocationCount; i++)
            {
                IEquipmentActionContextReader equipment = invocations[i].Equipment;
                if (equipment == null || !equipment.HasActionRoute(route))
                    continue;
                return equipment.TryReadActionContext(route, out EquipmentActionContext context)
                    ? (true, context)
                    : (false, default);
            }
            return (false, default);
        }

        static ResolvedGameplayMotion ResolveMotion(
            SimulationMotionContribution[] contributions,
            int contributionCount,
            Float32Yaw bodyYaw,
            Float32AbilityInvocationRuntime[] invocations,
            int invocationCount,
            Float32CharacterTraceSink trace)
        {
            ResolvedMotionChannel locomotion = Float32CharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = Float32CharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = Float32CharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.GameplayResult);
            for (int i = 0; i < invocationCount; i++)
                invocations[i].ApplyTimelineMotionWarps(ref action);
            TraceMotionChannel(locomotion, trace);
            TraceMotionChannel(action, trace);
            TraceMotionChannel(gameplayResult, trace);
            ResolvedGameplayMotion motion = Float32CharacterMotionResolver.Compose(locomotion, action, gameplayResult);
            ResolvedMotionChannel source = gameplayResult.TraceSource.IsValid ? gameplayResult :
                action.TraceSource.IsValid ? action : locomotion;
            if (source.TraceSource.IsValid)
                trace.Add("Character.Motion", source.TraceSource, "resolved_gameplay_motion", SimulationTraceSeverity.Information,
                    $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion};movementClock={Float32CharacterMotionResolver.FormatMovementClock(motion.MovementPlaybackClock)}",
                    source.TraceSourceGeneration);
            return motion;
        }

        static void AppendTimelineMotion(
            Float32MotionContributionScratch contributions,
            IReadOnlyList<AbilityTimelineLogicMotion> timelineMotion)
        {
            for (int i = 0; i < timelineMotion.Count; i++)
            {
                AbilityTimelineLogicMotion value = timelineMotion[i];
                contributions.Append(new SimulationMotionContribution(
                    value.Source,
                    value.AbilityId,
                    value.SourceGeneration,
                    new Float32Vector3(
                        Float32Scalar.FromSingle(value.DisplacementX.ToSingle()),
                        Float32Scalar.FromSingle(value.DisplacementY.ToSingle()),
                        Float32Scalar.FromSingle(value.DisplacementZ.ToSingle())),
                    Float32Scalar.FromSingle(value.YawDegrees.ToSingle()),
                    Float32Vector2.Zero,
                    (SimulationMotionContributionSpace)value.Space,
                    Float32Scalar.FromSingle(value.Weight.ToSingle()),
                    value.Priority,
                    (SimulationMotionChannel)value.Channel,
                    (SimulationMotionBlendMode)value.BlendMode,
                    value.ConsumeLowerChannels,
                    default,
                    default));
            }
        }

        static void TraceMotionChannel(
            ResolvedMotionChannel channel,
            Float32CharacterTraceSink trace)
        {
            if (!channel.TraceSource.IsValid)
                return;
            trace.Add("Character.Motion", channel.TraceSource, "motion_channel_resolved", SimulationTraceSeverity.Detail,
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};planarBasis={channel.PlanarBasis};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16};movementClock={Float32CharacterMotionResolver.FormatMovementClock(channel.MovementPlaybackClock)}",
                channel.TraceSourceGeneration);
        }


}
}
