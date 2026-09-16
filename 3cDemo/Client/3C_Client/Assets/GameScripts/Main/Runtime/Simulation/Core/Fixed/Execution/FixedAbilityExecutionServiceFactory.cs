using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedAbilityDomainRuntimeFactory
    {
        FixedAbilityDomainRuntimeServices Create(
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog,
            FixedGameplayAbilityExecutionAccess access,
            FixedAbilityExecutionFrame frame,
            IFixedAbilityExecutionSavepointPort savepointPort,
            FixedActionStateStore actionStore,
            FixedHandleAllocator handles,
            EquipmentProgramLayout equipmentLayout,
            FixedAbilityExecutionWorkspace workspace);
    }

    internal readonly struct FixedAbilityDomainRuntimeServices
    {
        public FixedAbilityDomainRuntimeServices(
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment)
        {
            GameplayEffects = gameplayEffects;
            Equipment = equipment;
        }

        public FixedGameplayEffectOperationRuntime GameplayEffects { get; }
        public FixedEquipmentRuntime Equipment { get; }
    }

    internal sealed class FixedAbilityDomainRuntimeFactory : IFixedAbilityDomainRuntimeFactory
    {
        public FixedAbilityDomainRuntimeServices Create(
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog,
            FixedGameplayAbilityExecutionAccess access,
            FixedAbilityExecutionFrame frame,
            IFixedAbilityExecutionSavepointPort savepointPort,
            FixedActionStateStore actionStore,
            FixedHandleAllocator handles,
            EquipmentProgramLayout equipmentLayout,
            FixedAbilityExecutionWorkspace workspace)
        {
            FixedGameplayEffectOperationRuntime gameplayEffects = gameplayEffectCatalog == null
                ? null
                : new FixedGameplayEffectOperationRuntime(
                    access,
                    savepointPort,
                    frame,
                    actionStore,
                    handles,
                    frame.Facts,
                    frame.Presentation,
                    frame.Trace,
                    workspace.GameplayEffects);
            FixedEquipmentRuntime equipment = null;
            if (equipmentLayout != null)
            {
                equipment = new FixedEquipmentRuntime(
                    access,
                    savepointPort,
                    frame,
                    actionStore,
                    handles,
                    gameplayEffects,
                    frame.Facts,
                    frame.Trace,
                    equipmentLayout);
            }
            return new FixedAbilityDomainRuntimeServices(gameplayEffects, equipment);
        }
    }

    internal sealed class FixedAbilityExecutionServiceFactory : IFixedAbilityExecutionServiceFactory
    {
        readonly IAbilityTimelineRuntime m_TimelineRuntime;

        public FixedAbilityExecutionServiceFactory(IAbilityTimelineRuntime timelineRuntime)
        {
            m_TimelineRuntime = timelineRuntime;
        }

        public FixedAbilityExecutionAssembly Create(
            FixedGameplayAbilityExecutionData executionData,
            FixedGameplayAbilityExecutionServices executionServices,
            IFixedAbilityActionBindingProvider actionBindings,
            IFixedAbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            FixedAbilityExecutionFrame frame,
            IFixedAbilityExecutionSavepointPort savepointPort,
            IFixedInputRequestStatePort inputRequests,
            FixedAbilityExecutionWorkspace workspace)
        {
            FixedGameplayAbilityExecutionAccess access = executionServices.Access;
            FixedStatePort controlState = frame.CreateStatePort(
                "Control",
                executionServices.ControlPolicy);
            FixedActionStateStore actionStore = new FixedActionStateStore(access, frame);
            FixedInputRuntime input = new FixedInputRuntime(access, frame, inputRequests);
            FixedHandleAllocator handles = new FixedHandleAllocator(access, frame);
            FixedBlackboardRuntime blackboard = new FixedBlackboardRuntime(
                access,
                frame.CreateStatePort("Blackboard", executionServices.BlackboardPolicy),
                frame,
                actionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            FixedAbilityDomainRuntimeServices domainServices = domainRuntimeFactory.Create(
                executionServices.GameplayEffectCatalog,
                access,
                frame,
                savepointPort,
                actionStore,
                handles,
                equipmentLayout,
                workspace);
            FixedGameplayEffectOperationRuntime gameplayEffects = domainServices.GameplayEffects;
            FixedEquipmentRuntime equipment = domainServices.Equipment;

            FixedAbilityOperationControlRuntime control = null;
            FixedActionRuntime actions = new FixedActionRuntime(
                access,
                actionBindings,
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
                frame.Trace,
                m_TimelineRuntime,
                actionStore,
                workspace.TimelineAdvances,
                frame.Tick);
            var services = new FixedAbilityExecutionServiceSet(
                frame,
                target,
                actionStore,
                gameplayEffects,
                equipment,
                values,
                blackboard,
                workspace.TimelineAdvances);
            control = new FixedAbilityOperationControlRuntime(executionData, services);
            FixedAbilityDomainRuntime domain = new FixedAbilityDomainRuntime(
                executionData.Binding,
                executionServices,
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
