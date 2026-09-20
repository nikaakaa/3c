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
            IReadOnlyList<SimulationIngress> ingress,
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
            var roleState = new FixedCharacterRuntimeStateTransaction(
                sourceState,
                tick,
                characterRuntime.TickRate,
                effectCatalog);
            var invocations = new List<FixedAbilityInvocationRuntime>(actor.AbilityInstallations.Installations.Count);
            var actionRuntimes = new Dictionary<CharacterSkillId, IFixedAbilityActionControlPort>();
            var sharedEffectScratch = new FixedGameplayEffectExecutionScratch();
            var motionContributions = new List<SimulationMotionContribution>();
            var timelineAdvances = new List<AbilityTimelineAdvancePending>();
            var timelineStops = new List<AbilityTimelineStopPending>();
            var facts = new List<GameplayFact>();
            var presentation = new List<PresentationCommand>();
            var trace = new List<SimulationTraceRecord>();
            var characterTrace = new List<SimulationTraceRecord>();
            try
            {
                var serviceFactory = new FixedAbilityExecutionServiceFactory(actor.TimelineRuntime);
                var domainRuntimeFactory = new FixedAbilityDomainRuntimeFactory();
                var abilityInput = new FixedAbilityExecutionInput(input.Sequence, input.Values);
                var bodyFacts = new FixedAbilityBodyFacts(actor.ActorId, beforeBody);
                var controlMotion = new FixedCharacterControlMotionRuntime(
                    abilityInput,
                    bodyFacts,
                    tick,
                    characterRuntime.TickRate,
                    actor.ControlRuntimeBinding.MotionBindings);
                var characterTraceSink = new FixedCharacterTraceSink(
                    characterTrace,
                    characterRuntime.NumericProfile,
                    actor.GameplayContentHash,
                    actor.ActorId,
                    tick,
                    diagnosticsEnabled);
                for (int i = 0; i < actor.AbilityInstallations.Installations.Count; i++)
                {
                    FixedGameplayAbilityExecutionInstallation installation = actor.AbilityInstallations.Installations[i];
                    var invocation = new FixedAbilityInvocationRuntime(
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
                        new FixedAbilityExecutionWorkspace(sharedEffectScratch, timelineAdvances, timelineStops),
                        serviceFactory);
                    invocations.Add(invocation);
                    invocation.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
                    actionRuntimes.Add(invocation.AbilityId, invocation.Actions);
                }

                new FixedCharacterInputRuntime(roleState.InputRequests, characterRuntime.InputRequestIds)
                    .ApplyRequests(input.Requests);

                bool effectAdvanced = false;
                for (int i = 0; i < invocations.Count; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    ApplyIngress(invocation, ingress, sourceState);
                    if (!effectAdvanced && invocation.HasGameplayEffects)
                    {
                        ApplyGameplayEffectIngress(invocation, ingress);
                        invocation.AdvanceGameplayEffects();
                        effectAdvanced = true;
                    }
                    invocation.ApplyInputBindings();
                }

                if (!effectAdvanced)
                    RequireNoGameplayEffectIngress(ingress);

                var control = new FixedCharacterControlRuntime(
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
                    (skill, window) => IsActionWindowActive(invocations, skill, window),
                    route => ReadEquipmentActionContext(invocations, route));
                control.Tick();
                motionContributions.AddRange(controlMotion.Contributions);

                for (int i = 0; i < invocations.Count; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    invocation.Tick();
                    motionContributions.AddRange(invocation.MotionContributions);
                }

                ResolvedGameplayMotion gameplayMotion = ResolveMotion(
                    motionContributions, beforeBody.Yaw, invocations, characterTraceSink);
                for (int i = 0; i < invocations.Count; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    FixedAbilityInvocationResult result = invocation.Complete();
                    facts.AddRange(result.GameplayFacts);
                    presentation.AddRange(result.PresentationCommands);
                    trace.AddRange(result.TraceRecords);
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
                return new FixedCharacterEvaluationResult(
                    actor.ActorId,
                    tick,
                    candidateState,
                    actor.TimelineRuntime,
                    facts.ToArray(),
                    presentation.ToArray(),
                    trace.ToArray(),
                    timelineAdvances.ToArray(),
                    timelineStops.ToArray());
            }
            catch
            {
                DiscardTimelineAdvances(actor.TimelineRuntime, timelineAdvances);
                DiscardTimelineStops(actor.TimelineRuntime, timelineStops);
                for (int i = 0; i < invocations.Count; i++)
                    invocations[i].Dispose();
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
            IReadOnlyList<SimulationIngress> ingress,
            FixedCharacterRuntimeState sourceState)
        {
            for (int i = 0; i < (ingress?.Count ?? 0); i++)
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
            IReadOnlyList<SimulationIngress> ingress)
        {
            for (int i = 0; i < (ingress?.Count ?? 0); i++)
                if (ingress[i].Header.Kind != SimulationIngressKind.ActionLifecycle)
                    invocation.ApplyGameplayEffectIngress(ingress[i]);
        }

        static void RequireNoGameplayEffectIngress(IReadOnlyList<SimulationIngress> ingress)
        {
            for (int i = 0; i < (ingress?.Count ?? 0); i++)
                if (ingress[i].Header.Kind != SimulationIngressKind.ActionLifecycle)
                    throw new InvalidOperationException(
                        "Fixed Character evaluation received Gameplay Effect ingress without an installed Gameplay Effect service.");
        }

        static bool OwnsAction(
            FixedCharacterRuntimeState state,
            ulong actionInstanceId,
            CharacterSkillId abilityId)
        {
            for (int i = 0; i < state.ActionInstances.Count; i++)
            {
                FixedActionInstanceState action = state.ActionInstances[i];
                if (action.InstanceId == actionInstanceId)
                    return action.SkillId == abilityId;
            }
            return false;
        }

        static bool IsActionWindowActive(
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations,
            CharacterSkillId skillId,
            string windowType)
        {
            for (int i = 0; i < invocations.Count; i++)
            {
                FixedAbilityInvocationRuntime invocation = invocations[i];
                if (invocation.AbilityId != skillId)
                    continue;
                return invocation.HasActionWindowProjection(windowType);
            }
            return false;
        }

        static (bool Found, EquipmentActionContext Context) ReadEquipmentActionContext(
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations,
            EquipmentActionRouteId route)
        {
            for (int i = 0; i < invocations.Count; i++)
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
            IReadOnlyList<SimulationMotionContribution> contributions,
            FixedYaw bodyYaw,
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations,
            FixedCharacterTraceSink trace)
        {
            ResolvedMotionChannel locomotion = FixedCharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = FixedCharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = FixedCharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.GameplayResult);
            for (int i = 0; i < invocations.Count; i++)
                invocations[i].ApplyMotionModifiers(ref action);
            TraceMotionChannel(locomotion, invocations, trace);
            TraceMotionChannel(action, invocations, trace);
            TraceMotionChannel(gameplayResult, invocations, trace);
            ResolvedGameplayMotion motion = FixedCharacterMotionResolver.Compose(locomotion, action, gameplayResult);
            ResolvedMotionChannel source = gameplayResult.TraceSource.IsValid ? gameplayResult :
                action.TraceSource.IsValid ? action : locomotion;
            if (source.TraceSource.IsValid)
                trace.Add("Character.Motion", source.TraceSource, "resolved_gameplay_motion", SimulationTraceSeverity.Information,
                    $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion};movementClock={FixedCharacterMotionResolver.FormatMovementClock(motion.MovementPlaybackClock)}",
                    MotionSourceGeneration(source, invocations));
            return motion;
        }

        static void TraceMotionChannel(
            ResolvedMotionChannel channel,
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations,
            FixedCharacterTraceSink trace)
        {
            if (!channel.TraceSource.IsValid)
                return;
            trace.Add("Character.Motion", channel.TraceSource, "motion_channel_resolved", SimulationTraceSeverity.Detail,
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};planarBasis={channel.PlanarBasis};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16};movementClock={FixedCharacterMotionResolver.FormatMovementClock(channel.MovementPlaybackClock)}",
                MotionSourceGeneration(channel, invocations));
        }

        static ulong MotionSourceGeneration(
            ResolvedMotionChannel channel,
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations)
        {
            if (!channel.TraceAbilityId.IsValid)
                return 1;
            for (int i = 0; i < invocations.Count; i++)
                if (invocations[i].AbilityId == channel.TraceAbilityId)
                    return invocations[i].MotionSourceGeneration(channel.TraceSource);
            throw new InvalidOperationException("Motion trace source has no owning Ability invocation.");
        }

}
}
