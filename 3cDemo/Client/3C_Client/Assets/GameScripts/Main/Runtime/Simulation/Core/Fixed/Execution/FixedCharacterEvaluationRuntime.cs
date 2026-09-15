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
            CharacterSimulationInput input,
            IReadOnlyList<SimulationIngress> ingress,
            WorldBodyState beforeBody,
            bool diagnosticsEnabled,
            bool captureValues,
            bool captureControlFlow)
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

            var effectCatalog = !actor.RequiresGameplayEffects || actor.GameplayEffectRuntimeBinding == null
                ? null
                : new FixedGameplayEffectRuntimeCatalog(actor.GameplayEffectRuntimeBinding);
            var roleState = new FixedCharacterRuntimeStateTransaction(
                sourceState,
                actor.ActorId,
                tick,
                characterRuntime.TickRate,
                effectCatalog);
            var invocations = new List<FixedAbilityInvocationRuntime>(actor.AbilityInstallations.Installations.Count);
            var actionRuntimes = new Dictionary<CharacterSkillId, FixedActionRuntime>();
            var sharedEffectScratch = new FixedGameplayEffectExecutionScratch();
            var results = new List<FixedAbilityInvocationResult>(invocations.Capacity);
            var facts = new List<GameplayFact>();
            var presentation = new List<PresentationCommand>();
            var trace = new List<SimulationTraceRecord>();
            var controlTrace = new List<SimulationTraceRecord>();
            try
            {
                var serviceFactory = new FixedAbilityExecutionServiceFactory(
                    actor.EquipmentRuntimeBinding);
                var abilityInput = new FixedAbilityExecutionInput(input.Sequence, input.Values, input.Requests);
                var bodyFacts = new FixedAbilityBodyFacts(actor.ActorId, beforeBody);
                var workspace = new FixedAbilityExecutionWorkspace(sharedEffectScratch);
                var controlMotion = new FixedCharacterControlMotionRuntime(
                    abilityInput,
                    bodyFacts,
                    tick,
                    characterRuntime.TickRate,
                    actor.ControlRuntimeBinding.MotionBindings);
                var controlTraceSink = new FixedCharacterControlTraceSink(
                    controlTrace,
                    characterRuntime.NumericProfile,
                    actor.GameplayContentHash,
                    actor.ActorId,
                    tick,
                    diagnosticsEnabled);
                workspace.Reset();
                for (int i = 0; i < actor.AbilityInstallations.Installations.Count; i++)
                {
                    FixedGameplayAbilityExecutionInstallation installation = actor.AbilityInstallations.Installations[i];
                    var invocation = new FixedAbilityInvocationRuntime(
                        installation,
                        actor.AbilityInstallations,
                        roleState.BindAbility(installation),
                        roleState,
                        actor.ActorId,
                        tick,
                        abilityInput,
                        ingress,
                        bodyFacts,
                        workspace,
                        roleState.InputRequests,
                        roleState.ActionState,
                        roleState.HandleAllocatorState,
                        roleState.EventSequenceState,
                        roleState.GameplayEffectState,
                        roleState.EquipmentState,
                        serviceFactory,
                        roleState.AcceptAbility);
                    invocations.Add(invocation);
                    actionRuntimes.Add(installation.Data.AbilityId, invocation.Actions);
                }

                new FixedCharacterInputRuntime(roleState.InputRequests, characterRuntime.InputRequestIds)
                    .ApplyRequests(input.Requests);

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
                    controlTraceSink,
                    actionRuntimes,
                    (skill, window) => IsActionWindowActive(invocations, skill, window),
                    route => ReadEquipmentActionContext(invocations, route));
                control.Tick();
                trace.AddRange(controlTrace);
                if (invocations.Count != 0)
                    workspace.MotionContributions.AddRange(controlMotion.Contributions);

                bool effectAdvanced = false;
                for (int i = 0; i < invocations.Count; i++)
                {
                    FixedAbilityInvocationRuntime invocation = invocations[i];
                    invocation.BeginEvaluation(diagnosticsEnabled, captureValues, captureControlFlow);
                    ApplyIngress(invocation, ingress, sourceState);
                    if (!effectAdvanced && invocation.GameplayEffects != null)
                    {
                        ApplyGameplayEffectIngress(invocation, ingress);
                        invocation.AdvanceGameplayEffects();
                        effectAdvanced = true;
                    }
                    invocation.ApplyInputBindings();
                    invocation.Tick();
                    FixedAbilityInvocationResult result = invocation.Complete();
                    results.Add(result);
                    facts.AddRange(result.GameplayFacts);
                    presentation.AddRange(result.PresentationCommands);
                    trace.AddRange(result.TraceRecords);
                    invocation.Accept();
                }

                if (!effectAdvanced)
                    RequireNoGameplayEffectIngress(ingress);

                ResolvedGameplayMotion gameplayMotion = invocations.Count == 0
                    ? controlMotion.Resolve()
                    : ResolveMotion(results);
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
                var worldRequest = new CharacterWorldSolveRequest(
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
                    worldRequest,
                    facts,
                    presentation,
                    trace,
                    diagnosticsEnabled);
            }
            catch
            {
                for (int i = 0; i < invocations.Count; i++)
                    invocations[i].Dispose();
                roleState.Dispose();
                throw;
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
                    !OwnsAction(sourceState, value.ActionLifecycle.ActionInstanceId, invocation.Installation.Data.AbilityId))
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
                if (invocation.Installation.Data.AbilityId != skillId)
                    continue;
                IReadOnlyList<SimulationActionWindowProjectionCandidate> projections =
                    invocation.Workspace.ActionWindowProjections;
                for (int projectionIndex = 0; projectionIndex < projections.Count; projectionIndex++)
                {
                    SimulationActionWindowProjectionCandidate projection = projections[projectionIndex];
                    if (string.Equals(projection.WindowType, windowType, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }
            return false;
        }

        static (bool Found, EquipmentActionContext Context) ReadEquipmentActionContext(
            IReadOnlyList<FixedAbilityInvocationRuntime> invocations,
            EquipmentActionRouteId route)
        {
            for (int i = 0; i < invocations.Count; i++)
            {
                FixedEquipmentRuntime equipment = invocations[i].Equipment;
                if (equipment == null || !equipment.HasActionRoute(route))
                    continue;
                return equipment.TryReadActionContext(route, out EquipmentActionContext context)
                    ? (true, context)
                    : (false, default);
            }
            return (false, default);
        }

        static ResolvedGameplayMotion ResolveMotion(
            IReadOnlyList<FixedAbilityInvocationResult> results)
        {
            FixedVector3 displacement = FixedVector3.Zero;
            FixedScalar yaw = FixedScalar.Zero;
            FixedVector2 planarBasis = FixedVector2.Zero;
            bool hasMotion = false;
            CommittedMovementPlaybackClock movementClock = default;
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline = default;
            bool hasMovementClock = false;
            string actionOwner = string.Empty;
            string gameplayResultOwner = string.Empty;
            for (int i = 0; i < results.Count; i++)
            {
                ResolvedGameplayMotion motion = results[i].Motion;
                displacement += motion.Displacement;
                yaw += motion.YawDegrees;
                hasMotion |= motion.HasMotion;
                if (planarBasis == FixedVector2.Zero && motion.LocomotionPlanarBasis != FixedVector2.Zero)
                    planarBasis = motion.LocomotionPlanarBasis;
                if (!string.IsNullOrEmpty(motion.ActionOwnerIdentity) && string.IsNullOrEmpty(actionOwner))
                    actionOwner = motion.ActionOwnerIdentity;
                if (!string.IsNullOrEmpty(motion.GameplayResultOwnerIdentity) && string.IsNullOrEmpty(gameplayResultOwner))
                    gameplayResultOwner = motion.GameplayResultOwnerIdentity;
                if (!motion.MovementPlaybackClock.IsValid)
                    continue;
                if (hasMovementClock)
                {
                    if (!movementClock.Equals(motion.MovementPlaybackClock) ||
                        !locomotionTimeline.Equals(motion.LocomotionTimeline))
                        throw new InvalidOperationException(
                            "Fixed Character evaluation produced multiple incompatible locomotion playback clocks.");
                }
                else
                {
                    movementClock = motion.MovementPlaybackClock;
                    locomotionTimeline = motion.LocomotionTimeline;
                    hasMovementClock = true;
                }
            }
            return new ResolvedGameplayMotion(
                displacement,
                yaw,
                planarBasis,
                hasMotion,
                movementClock,
                locomotionTimeline,
                actionOwner,
                gameplayResultOwner);
        }

    internal sealed class FixedAbilityExecutionServiceFactory : IFixedAbilityExecutionServiceFactory
    {
        readonly CharacterEquipmentRuntimeBinding m_EquipmentRuntimeBinding;

        public FixedAbilityExecutionServiceFactory(
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding)
        {
            m_EquipmentRuntimeBinding = equipmentRuntimeBinding;
        }

        public FixedAbilityExecutionAssembly Create(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionInstallationSet installations,
            FixedAbilityExecutionFrame frame,
            FixedAbilityExecutionWorkspace workspace)
        {
            FixedGameplayAbilityExecutionAccess access = installation.Access;
            FixedStatePort controlState = frame.CreateStatePort(
                "Control",
                installation.Services.ControlPolicy);
            FixedActionStateStore actionStore = new FixedActionStateStore(access, frame);
            FixedInputRuntime input = new FixedInputRuntime(access, frame);
            FixedHandleAllocator handles = new FixedHandleAllocator(access, frame);
            FixedBlackboardRuntime blackboard = new FixedBlackboardRuntime(
                access,
                frame.CreateStatePort("Blackboard", installation.Services.BlackboardPolicy),
                frame,
                actionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            FixedGameplayEffectOperationRuntime gameplayEffects = installation.GameplayEffectCatalog == null
                ? null
                : new FixedGameplayEffectOperationRuntime(
                    access,
                    frame,
                    actionStore,
                    handles,
                    frame.Facts,
                    frame.Presentation,
                    frame.Trace,
                    workspace.GameplayEffects);
            FixedEquipmentRuntime equipment = null;
            if (installation.RequiresEquipment &&
                m_EquipmentRuntimeBinding != null)
            {
                EquipmentProgramLayout layout = EquipmentProgramLayoutCompiler.Compile(
                    m_EquipmentRuntimeBinding,
                    installation.Data.CatalogEntries,
                    installation.Data.References,
                    installation.Data.Producers);
                equipment = new FixedEquipmentRuntime(
                    access,
                    frame,
                    actionStore,
                    handles,
                    gameplayEffects,
                    frame.Facts,
                    frame.Trace,
                    layout);
            }
            bool equipmentEnabled = installation.RequiresEquipment;
            if (equipmentEnabled && equipment == null)
                throw new InvalidOperationException(
                    $"Ability '{installation.Data.AbilityId}' requires the declared Equipment service.");

            FixedAbilityControlRuntime control = null;
            FixedActionRuntime actions = new FixedActionRuntime(
                access,
                installations,
                frame,
                input,
                actionStore,
                blackboard,
                gameplayEffects,
                gameplayEffects,
                handles,
                frame.Facts,
                frame.Trace,
                equipment,
                operation => control == null ||
                    !control.IsActive(operation) && !control.IsStopping(operation));
            FixedValueRuntime values = new FixedValueRuntime(
                access,
                input,
                actionStore,
                actions,
                gameplayEffects,
                equipment,
                blackboard,
                frame,
                workspace);
            FixedMotionAccumulator motion = new FixedMotionAccumulator(
                access,
                frame,
                workspace.MotionContributions,
                workspace.MotionWarpSamples,
                actionStore);
            FixedLocomotionRuntime locomotion = new FixedLocomotionRuntime(
                access,
                values,
                motion,
                frame);
            FixedAbilityExecutionTarget target = new FixedAbilityExecutionTarget(
                access,
                controlState,
                frame.CreateOperationStateReset(),
                values,
                blackboard,
                actions,
                gameplayEffects,
                equipment,
                locomotion,
                frame.Facts,
                frame.Presentation,
                frame.Trace);
            var services = new FixedAbilityExecutionServiceSet(
                frame,
                target,
                actions,
                actionStore,
                input,
                gameplayEffects,
                equipment,
                values,
                blackboard,
                motion);
            control = new FixedAbilityControlRuntime(installation, services);
            FixedAbilityDomainRuntime domain = new FixedAbilityDomainRuntime(
                installation,
                actions,
                actionStore,
                control);
            return new FixedAbilityExecutionAssembly(
                actionStore,
                input,
                actions,
                gameplayEffects,
                equipment,
                values,
                blackboard,
                motion,
                locomotion,
                control,
                domain);
        }
    }
}
}
