using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal static class Float32CharacterEvaluationRuntime
    {
        public static Float32PendingActorEvaluation Evaluate(
            Float32CharacterRuntime characterRuntime,
            SimulationActorBinding actor,
            Float32CharacterRuntimeState sourceState,
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
                throw new InvalidOperationException("Float32 Character evaluation identity does not match the active Character Runtime.");

            var effectCatalog = actor.GameplayEffectRuntimeBinding == null
                ? null
                : new Float32GameplayEffectRuntimeCatalog(actor.GameplayEffectRuntimeBinding);
            var roleState = new Float32CharacterRuntimeStateTransaction(
                sourceState,
                actor.ActorId,
                tick,
                characterRuntime.TickRate,
                effectCatalog);
            var invocations = new List<Float32AbilityInvocationRuntime>(actor.AbilityInstallations.Installations.Count);
            var actionRuntimes = new Dictionary<CharacterSkillId, Float32ActionRuntime>();
            var sharedEffectScratch = new Float32GameplayEffectExecutionScratch();
            var results = new List<Float32AbilityInvocationResult>(invocations.Capacity);
            var facts = new List<GameplayFact>();
            var presentation = new List<PresentationCommand>();
            var trace = new List<SimulationTraceRecord>();
            try
            {
                var abilityInput = new Float32AbilityExecutionInput(input.Sequence, input.Values, input.Requests);
                var bodyFacts = new Float32AbilityBodyFacts(actor.ActorId, beforeBody);
                var workspace = new Float32AbilityExecutionWorkspace(sharedEffectScratch);
                workspace.Reset();
                for (int i = 0; i < actor.AbilityInstallations.Installations.Count; i++)
                {
                    Float32GameplayAbilityExecutionInstallation installation = actor.AbilityInstallations.Installations[i];
                    var invocation = new Float32AbilityInvocationRuntime(
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
                        roleState,
                        roleState,
                        roleState,
                        roleState,
                        roleState,
                        new Float32CharacterAbilityExecutionServiceFactory(installation, actor.ControlRuntimeBinding, actor.EquipmentRuntimeBinding, sharedEffectScratch),
                        roleState.AcceptAbility);
                    invocations.Add(invocation);
                    actionRuntimes.Add(installation.Data.AbilityId, invocation.Actions);
                }

                new Float32CharacterInputRuntime(roleState.InputRequests, characterRuntime.InputRequestIds)
                    .ApplyRequests(input.Requests);

                if (invocations.Count != 0)
                {
                    Float32AbilityInvocationRuntime controlOwner = invocations[0];
                    var control = new Float32CharacterControlRuntime(
                        characterRuntime.ControlModules,
                        actor.ControlRuntimeBinding,
                        roleState,
                        roleState.InputRequests,
                        roleState,
                        actor.ActorId,
                        tick,
                        characterRuntime.TickRate,
                        controlOwner.Frame.Input,
                        controlOwner.Frame.BodyFacts,
                        controlOwner.Input,
                        controlOwner.Locomotion,
                        controlOwner.Frame.Trace,
                        actionRuntimes,
                        (skill, window) => IsActionWindowActive(invocations, skill, window),
                        route => ReadEquipmentActionContext(invocations, route));
                    control.Tick();
                }

                bool effectAdvanced = false;
                for (int i = 0; i < invocations.Count; i++)
                {
                    Float32AbilityInvocationRuntime invocation = invocations[i];
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
                    Float32AbilityInvocationResult result = invocation.Complete();
                    results.Add(result);
                    facts.AddRange(result.GameplayFacts);
                    presentation.AddRange(result.PresentationCommands);
                    trace.AddRange(result.TraceRecords);
                    invocation.Accept();
                }

                if (!effectAdvanced)
                    RequireNoGameplayEffectIngress(ingress);

                ResolvedGameplayMotion gameplayMotion = ResolveMotion(results);
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
                var worldRequest = new CharacterWorldSolveRequest(
                    characterRuntime.NumericProfile,
                    actor.ActorId,
                    new WorldRequestId(actor.ActorId, tick, 1),
                    tick,
                    beforeBody,
                    bodyMotion.Motion,
                    bodyMotion.Plan,
                    requiredCapabilities);
                return new Float32PendingActorEvaluation(
                    actor.ActorId,
                    tick,
                    sourceState,
                    roleState,
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
            Float32AbilityInvocationRuntime invocation,
            IReadOnlyList<SimulationIngress> ingress,
            Float32CharacterRuntimeState sourceState)
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
            IReadOnlyList<Float32AbilityInvocationRuntime> invocations,
            EquipmentActionRouteId route)
        {
            for (int i = 0; i < invocations.Count; i++)
            {
                Float32EquipmentRuntime equipment = invocations[i].Equipment;
                if (equipment == null || !equipment.HasActionRoute(route))
                    continue;
                return equipment.TryReadActionContext(route, out EquipmentActionContext context)
                    ? (true, context)
                    : (false, default);
            }
            return (false, default);
        }

        static ResolvedGameplayMotion ResolveMotion(
            IReadOnlyList<Float32AbilityInvocationResult> results)
        {
            Float32Vector3 displacement = Float32Vector3.Zero;
            Float32Scalar yaw = Float32Scalar.Zero;
            Float32Vector2 planarBasis = Float32Vector2.Zero;
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
                if (planarBasis == Float32Vector2.Zero && motion.LocomotionPlanarBasis != Float32Vector2.Zero)
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
                            "Float32 Character evaluation produced multiple incompatible locomotion playback clocks.");
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

    internal sealed class Float32CharacterAbilityExecutionServiceFactory : IFloat32AbilityExecutionServiceFactory
    {
        readonly Float32GameplayAbilityExecutionInstallation installation;
        readonly CharacterControlRuntimeBinding controlRuntimeBinding;
        readonly CharacterEquipmentRuntimeBinding equipmentRuntimeBinding;
        readonly Float32GameplayEffectExecutionScratch gameplayEffectScratch;

        public Float32CharacterAbilityExecutionServiceFactory(
            Float32GameplayAbilityExecutionInstallation installation,
            CharacterControlRuntimeBinding controlRuntimeBinding,
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding,
            Float32GameplayEffectExecutionScratch gameplayEffectScratch)
        {
            this.installation = installation ?? throw new ArgumentNullException(nameof(installation));
            this.controlRuntimeBinding = controlRuntimeBinding;
            this.equipmentRuntimeBinding = equipmentRuntimeBinding;
            this.gameplayEffectScratch = gameplayEffectScratch ?? throw new ArgumentNullException(nameof(gameplayEffectScratch));
        }

        public Float32GameplayEffectOperationRuntime CreateGameplayEffects(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actions,
            Float32HandleAllocator handles,
            Float32FactSink facts,
            Float32PresentationSink presentation,
            Float32TraceSink trace,
            Float32AbilityExecutionWorkspace workspace)
        {
            return installation.GameplayEffectCatalog == null
                ? null
                : new Float32GameplayEffectOperationRuntime(
                    access,
                    frame,
                    actions,
                    handles,
                    facts,
                    presentation,
                    trace,
                    workspace.GameplayEffects);
        }

        public Float32EquipmentRuntime CreateEquipment(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actions,
            Float32HandleAllocator handles,
            Float32GameplayEffectOperationRuntime gameplayEffects)
        {
            if (!installation.Data.Capabilities.HasGameplayCapability("Equipment") || equipmentRuntimeBinding == null)
                return null;
            var layout = EquipmentProgramLayoutCompiler.Compile(
                equipmentRuntimeBinding,
                installation.Data.CatalogEntries,
                installation.Data.References,
                installation.Data.Producers);
            return new Float32EquipmentRuntime(
                access,
                frame,
                actions,
                handles,
                gameplayEffects,
                frame.Facts,
                frame.Trace,
                layout);
        }

        public Float32LocomotionRuntime CreateLocomotion(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionAccess access,
            Float32ValueRuntime values,
            Float32MotionAccumulator motion,
            Float32AbilityExecutionFrame frame)
        {
            return new Float32LocomotionRuntime(access, values, motion, frame, controlRuntimeBinding);
        }
    }
}
}
