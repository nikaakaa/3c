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
                throw new InvalidOperationException("Float32 Character evaluation identity does not match the active Character Runtime.");

            Float32GameplayEffectRuntimeCatalog effectCatalog = actor.AbilityInstallations.GameplayEffectCatalog;
            var roleState = new Float32CharacterRuntimeStateTransaction(
                sourceState,
                tick,
                characterRuntime.TickRate,
                effectCatalog);
            var invocations = new List<Float32AbilityInvocationRuntime>(actor.AbilityInstallations.Installations.Count);
            var actionRuntimes = new Dictionary<CharacterSkillId, IFloat32AbilityActionControlPort>();
            var sharedEffectScratch = new Float32GameplayEffectExecutionScratch();
            var motionContributions = new List<SimulationMotionContribution>();
            var timelineAdvances = new List<IAbilityTimelinePending>();
            var timelineStops = new List<IAbilityTimelineStopPending>();
            var facts = new List<GameplayFact>();
            var presentation = new List<PresentationCommand>();
            var trace = new List<SimulationTraceRecord>();
            var characterTrace = new List<SimulationTraceRecord>();
            try
            {
                var serviceFactory = new Float32AbilityExecutionServiceFactory(actor.TimelineRuntime);
                var domainRuntimeFactory = new Float32AbilityDomainRuntimeFactory();
                var abilityInput = new Float32AbilityExecutionInput(input.Sequence, input.Values);
                var bodyFacts = new Float32AbilityBodyFacts(actor.ActorId, beforeBody);
                var controlMotion = new Float32CharacterControlMotionRuntime(
                    abilityInput,
                    bodyFacts,
                    tick,
                    characterRuntime.TickRate,
                    actor.ControlRuntimeBinding.MotionBindings);
                var characterTraceSink = new Float32CharacterTraceSink(
                    characterTrace,
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
                    invocations.Add(invocation);
                    invocation.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
                    actionRuntimes.Add(invocation.AbilityId, invocation.Actions);
                }

                new Float32CharacterInputRuntime(roleState.InputRequests, characterRuntime.InputRequestIds)
                    .ApplyRequests(input.Requests);

                bool effectAdvanced = false;
                for (int i = 0; i < invocations.Count; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
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
                    (skill, window) => IsActionWindowActive(invocations, skill, window),
                    route => ReadEquipmentActionContext(invocations, route));
                control.Tick();
                motionContributions.AddRange(controlMotion.Contributions);

                for (int i = 0; i < invocations.Count; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
                    invocation.Tick();
                    motionContributions.AddRange(invocation.MotionContributions);
                }

                ResolvedGameplayMotion gameplayMotion = ResolveMotion(
                    motionContributions, beforeBody.Yaw, invocations, characterTraceSink);
                for (int i = 0; i < invocations.Count; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
                    Float32AbilityInvocationResult result = invocation.Complete();
                    facts.AddRange(result.GameplayFacts);
                    presentation.AddRange(result.PresentationCommands);
                    trace.AddRange(result.TraceRecords);
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
                return new Float32CharacterEvaluationResult(
                    actor.ActorId,
                    tick,
                    candidateState,
                    facts,
                    presentation,
                    trace,
                    actor.TimelineRuntime,                timelineAdvances,
                    timelineStops);
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
            IReadOnlyList<IAbilityTimelineStopPending> stops)
        {
            if (timelineRuntime == null)
                return;
            for (int i = 0; i < stops.Count; i++)
            {
                if (stops[i] == null)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline stop.");
                timelineRuntime.DiscardStop(stops[i]);
            }
        }

        static void DiscardTimelineAdvances(
            IAbilityTimelineRuntime timelineRuntime,
            IReadOnlyList<IAbilityTimelinePending> advances)
        {
            if (timelineRuntime == null)
                return;
            for (int i = 0; i < advances.Count; i++)
            {
                if (advances[i] == null)
                    throw new InvalidOperationException("Float32 Character evaluation has an empty Timeline advance.");
                timelineRuntime.Discard(advances[i]);
            }
        }

        static void ApplyIngress(
            Float32AbilityInvocationRuntime invocation,
            IReadOnlyList<SimulationIngress> ingress,
            Float32CharacterRuntimeState sourceState)
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
            Float32AbilityInvocationRuntime invocation,
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
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations,
            CharacterSkillId skillId,
            string windowType)
        {
            for (int i = 0; i < invocations.Count; i++)
            {
                Float32AbilityInvocationRuntime invocation = invocations[i];
                if (invocation.AbilityId != skillId)
                    continue;
                return invocation.HasActionWindowProjection(windowType);
            }
            return false;
        }

        static (bool Found, EquipmentActionContext Context) ReadEquipmentActionContext(
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations,
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
            Float32Yaw bodyYaw,
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations,
            Float32CharacterTraceSink trace)
        {
            ResolvedMotionChannel locomotion = Float32CharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.Locomotion);
            ResolvedMotionChannel action = Float32CharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.Action);
            ResolvedMotionChannel gameplayResult = Float32CharacterMotionResolver.ResolveChannel(contributions, bodyYaw, SimulationMotionChannel.GameplayResult);
            for (int i = 0; i < invocations.Count; i++)
                invocations[i].ApplyMotionModifiers(ref action);
            TraceMotionChannel(locomotion, invocations, trace);
            TraceMotionChannel(action, invocations, trace);
            TraceMotionChannel(gameplayResult, invocations, trace);
            ResolvedGameplayMotion motion = Float32CharacterMotionResolver.Compose(locomotion, action, gameplayResult);
            ResolvedMotionChannel source = gameplayResult.TraceSource.IsValid ? gameplayResult :
                action.TraceSource.IsValid ? action : locomotion;
            if (source.TraceSource.IsValid)
                trace.Add("Character.Motion", source.TraceSource, "resolved_gameplay_motion", SimulationTraceSeverity.Information,
                    $"delta={motion.Displacement};yaw={motion.YawDegrees};hasMotion={motion.HasMotion};movementClock={Float32CharacterMotionResolver.FormatMovementClock(motion.MovementPlaybackClock)}",
                    MotionSourceGeneration(source, invocations));
            return motion;
        }

        static void TraceMotionChannel(
            ResolvedMotionChannel channel,
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations,
            Float32CharacterTraceSink trace)
        {
            if (!channel.TraceSource.IsValid)
                return;
            trace.Add("Character.Motion", channel.TraceSource, "motion_channel_resolved", SimulationTraceSeverity.Detail,
                $"channel={channel.Channel};owner={channel.ResolvedOwnerIdentity};delta={channel.Displacement};yaw={channel.YawDegrees};planarBasis={channel.PlanarBasis};claim={channel.ClaimsLowerChannels};sources={channel.ParticipatingSourceCount};fingerprint={channel.ParticipatingSourceFingerprint:x16};movementClock={Float32CharacterMotionResolver.FormatMovementClock(channel.MovementPlaybackClock)}",
                MotionSourceGeneration(channel, invocations));
        }

        static ulong MotionSourceGeneration(
            ResolvedMotionChannel channel,
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations)
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
