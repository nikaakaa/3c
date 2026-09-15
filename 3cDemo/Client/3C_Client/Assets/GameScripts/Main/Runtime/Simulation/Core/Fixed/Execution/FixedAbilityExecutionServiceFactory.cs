using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionServiceFactory : IFixedAbilityExecutionServiceFactory
    {
        public FixedAbilityExecutionAssembly Create(
            FixedGameplayAbilityExecutionInstallation installation,
            IFixedAbilityInstallationProvider installations,
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
            if (installation.RequiresEquipment)
            {
                equipment = new FixedEquipmentRuntime(
                    access,
                    frame,
                    actionStore,
                    handles,
                    gameplayEffects,
                    frame.Facts,
                    frame.Trace,
                    installation.EquipmentLayout ??
                    throw new InvalidOperationException(
                        $"Ability '{installation.Data.AbilityId}' requires the declared Equipment layout."));
            }

            FixedAbilityOperationControlRuntime control = null;
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
                gameplayEffects,
                equipment,
                values,
                blackboard);
            control = new FixedAbilityOperationControlRuntime(installation, services);
            FixedAbilityDomainRuntime domain = new FixedAbilityDomainRuntime(
                installation,
                actions,
                actionStore,
                control);
            return new FixedAbilityExecutionAssembly(
                input,
                actions,
                gameplayEffects,
                equipment,
                blackboard,
                motion,
                control,
                domain);
        }
    }
}
