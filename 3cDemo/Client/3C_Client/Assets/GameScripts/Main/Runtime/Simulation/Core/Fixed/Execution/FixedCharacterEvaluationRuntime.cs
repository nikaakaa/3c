using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal static class FixedCharacterEvaluationRuntime
    {
        public static FixedCharacterEvaluationResult Evaluate(
            FixedCharacterRuntime characterRuntime,
            SimulationActorBinding actor,
            FixedCharacterRuntimeState sourceState,
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
                throw new InvalidOperationException("Fixed Character evaluation identity does not match the active Character Runtime.");

            FixedGameplayEffectRuntimeCatalog effectCatalog = actor.AbilityInstallations.GameplayEffectCatalog;
            var roleState = actor.RuntimeState.Restart(
                sourceState,
                tick,
                characterRuntime.TickRate,
                effectCatalog,
                actor.InputRequestState,
                actor.ActionState,
                actor.ControlState,
                actor.EventSequenceState,
                actor.HandleAllocatorState,
                actor.GameplayEffectState,
                actor.EquipmentState);
            FixedAbilityInvocationRuntime[] invocations = actor.Invocations;
            int invocationCount = invocations.Length;
            Dictionary<CharacterSkillId, IFixedAbilityActionControlPort> actionRuntimes = actor.ActionRuntimes;
            actionRuntimes.Clear();
            FixedGameplayEffectExecutionScratch sharedEffectScratch = actor.EffectExecutionScratch;
            sharedEffectScratch.Reset();
            FixedAbilityExecutionWorkspace[] workspaces = actor.Workspaces;
            for (int i = 0; i < workspaces.Length; i++)
                workspaces[i].Reset();
            actor.ClearTimelineTransfers();
            List<AbilityTimelineAdvancePending> timelineAdvances = actor.TimelineAdvances;
            List<AbilityTimelineStopPending> timelineStops = actor.TimelineStops;
            List<AbilityTimelineLogicMotion> timelineLogicMotion = actor.TimelineLogicMotion;
            List<AbilityTimelineLogicMotionWarp> timelineLogicMotionWarps = actor.TimelineLogicMotionWarps;
            FixedCharacterEvaluationOutput evaluationOutput = actor.EvaluationOutput;
            evaluationOutput.Clear();
            List<GameplayFact> facts = evaluationOutput.Facts;
            List<PresentationCommand> presentation = evaluationOutput.Presentation;
            List<SimulationTraceRecord> trace = evaluationOutput.Trace;
            List<SimulationTraceRecord> characterTrace = evaluationOutput.CharacterTrace;
            FixedAbilityExecutionInput abilityInput = actor.AbilityExecutionInput;
            try
            {
                abilityInput.Begin(input.Sequence, input.Values);
                var bodyFacts = new FixedAbilityBodyFacts(actor.ActorId, beforeBody);
                FixedMotionContributionScratch motionContributions = actor.MotionContributions;
                motionContributions.Begin();
                FixedCharacterControlMotionRuntime controlMotion = actor.ControlMotion;
                controlMotion.Begin(
                    abilityInput,
                    bodyFacts,
                    tick,
                    characterRuntime.TickRate);
                FixedCharacterTraceSink characterTraceSink = actor.CharacterTraceSink;
                characterTraceSink.Begin(
                    characterRuntime.NumericProfile,
                    actor.GameplayContentHash,
                    actor.ActorId,
                    tick,
                    diagnosticsEnabled);
                for (int i = 0; i < actor.AbilityInstallations.Installations.Count; i++)
                {
                    FixedGameplayAbilityExecutionInstallation installation = actor.AbilityInstallations.Installations[i];
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    invocation.Begin(new FixedAbilityInvocationContext(
                        actor.ActorId,
                        tick,
                        abilityInput,
                        bodyFacts,
                        roleState.BindAbility(installation.Identity, installation.Layout, installation.Data),
                        roleState,
                        roleState.InputRequests,
                        roleState.ActionState,
                        roleState.HandleAllocatorState,
                        roleState.EventSequenceState,
                        roleState.GameplayEffectState,
                        roleState.EquipmentState));
                    invocation.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
                    actionRuntimes.Add(invocation.AbilityId, invocation.Actions);
                }

                characterRuntime.InputRuntime.Begin(roleState.InputRequests);
                characterRuntime.InputRuntime.ApplyRequests(input.Requests);

                bool effectAdvanced = false;
                for (int i = 0; i < invocationCount; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
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

                FixedCharacterControlRuntime control = characterRuntime.ControlRuntime(actor.ActorId);
                control.Begin(
                    roleState,
                    roleState.InputRequests,
                    roleState.ActionState,
                    tick,
                    bodyFacts);
                control.Tick();
                controlMotion.CopyContributionsTo(motionContributions);

                for (int i = 0; i < invocationCount; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
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
                        FixedAbilityInvocationRuntime invocation = invocations[i];
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
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    invocation.Complete(facts, presentation, trace);
                    invocation.Accept(roleState.AcceptAbility);
                }
                trace.AddRange(characterTrace);
                FixedScalar tickDelta = FixedScalar.One / FixedScalar.FromInt64(characterRuntime.TickRate);
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
                FixedCharacterRuntimeState candidateState = roleState.Commit();
                roleState.Dispose();
                var result = new FixedCharacterEvaluationResult(
                    actor.ActorId,
                    tick,
                    candidateState,
                    actor.TimelineRuntime,
                    facts.ToArray(),
                    presentation.ToArray(),
                    trace.ToArray(),
                    timelineAdvances.ToArray(),
                    timelineStops.ToArray());
                actor.ClearActionRuntimes();
                actor.ClearWorkspaces();
                actor.ClearTimelineTransfers();
                actor.ClearTimelineMotionScratches();
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
                    invocations[i].Abort();
                actor.ClearActionRuntimes();
                actor.ClearWorkspaces();
                actor.ClearTimelineTransfers();
                actor.ClearTimelineMotionScratches();
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
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline stop.");
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
                    throw new InvalidOperationException("Fixed Character evaluation has an empty Timeline advance.");
                timelineRuntime.Discard(advances[i]);
            }
        }

        static void ApplyIngress(
            FixedAbilityInvocationRuntime invocation,
            SimulationIngress[] ingress,
            int ingressCount,
            FixedCharacterRuntimeState sourceState)
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
            FixedAbilityInvocationRuntime invocation,
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
                        "Fixed Character evaluation received Gameplay Effect ingress without an installed Gameplay Effect service.");
        }

        static bool OwnsAction(
            FixedCharacterRuntimeState state,
            ulong actionInstanceId,
            CharacterSkillId abilityId)
        {
            for (int i = 0; i < state.ActionInstances.Length; i++)
            {
                FixedActionInstanceState action = state.ActionInstances[i];
                if (action.InstanceId == actionInstanceId)
                    return action.SkillId == abilityId;
            }
            return false;
        }

        internal static bool IsActionWindowActive(
            FixedAbilityInvocationRuntime[] invocations,
            CharacterSkillId skillId,
            string windowType)
        {
            for (int i = 0; i < invocations.Length; i++)
            {
                FixedAbilityInvocationRuntime invocation = invocations[i];
                if (invocation.AbilityId != skillId)
                    continue;
                return invocation.HasActionWindowProjection(windowType);
            }
            return false;
        }

        internal static (bool Found, EquipmentActionContext Context) ReadEquipmentActionContext(
            FixedAbilityInvocationRuntime[] invocations,
            EquipmentActionRouteId route)
        {
            for (int i = 0; i < invocations.Length; i++)
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
            FixedYaw bodyYaw,
            FixedAbilityInvocationRuntime[] invocations,
            int invocationCount,
            FixedCharacterTraceSink trace)
        {
            ResolvedMotionChannel locomotion = FixedCharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = FixedCharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = FixedCharacterMotionResolver.ResolveChannel(
                contributions,
                contributionCount,
                bodyYaw,
                SimulationMotionChannel.GameplayResult);
            for (int i = 0; i < invocationCount; i++)
                invocations[i].ApplyTimelineMotionWarps(ref action);
            TraceMotionChannel(locomotion, trace);
            TraceMotionChannel(action, trace);
            TraceMotionChannel(gameplayResult, trace);
            ResolvedGameplayMotion motion = FixedCharacterMotionResolver.Compose(locomotion, action, gameplayResult);
            ResolvedMotionChannel source = gameplayResult.TraceSource.IsValid ? gameplayResult :
                action.TraceSource.IsValid ? action : locomotion;
            if (source.TraceSource.IsValid)
                trace.Add("Character.Motion", source.TraceSource, "resolved_gameplay_motion", SimulationTraceSeverity.Information,
                    $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion};movementClock={FixedCharacterMotionResolver.FormatMovementClock(motion.MovementPlaybackClock)}",
                    source.TraceSourceGeneration);
            return motion;
        }

        static void AppendTimelineMotion(
            FixedMotionContributionScratch contributions,
            IReadOnlyList<AbilityTimelineLogicMotion> timelineMotion)
        {
            for (int i = 0; i < timelineMotion.Count; i++)
            {
                AbilityTimelineLogicMotion value = timelineMotion[i];
                contributions.Append(new SimulationMotionContribution(
                    value.Source,
                    value.AbilityId,
                    value.SourceGeneration,
                    new FixedVector3(value.DisplacementX, value.DisplacementY, value.DisplacementZ),
                    value.YawDegrees,
                    FixedVector2.Zero,
                    (SimulationMotionContributionSpace)value.Space,
                    value.Weight,
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
            FixedCharacterTraceSink trace)
        {
            if (!channel.TraceSource.IsValid)
                return;
            trace.Add("Character.Motion", channel.TraceSource, "motion_channel_resolved", SimulationTraceSeverity.Detail,
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};planarBasis={channel.PlanarBasis};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16};movementClock={FixedCharacterMotionResolver.FormatMovementClock(channel.MovementPlaybackClock)}",
                channel.TraceSourceGeneration);
        }


}
}
