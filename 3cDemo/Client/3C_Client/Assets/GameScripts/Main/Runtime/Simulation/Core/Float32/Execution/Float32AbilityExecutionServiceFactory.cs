using System;

namespace ThirdPersonSimulation
{
    internal interface IFloat32AbilityDomainRuntimeFactory
    {
        Float32AbilityDomainRuntimeServices Create(
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            IFloat32AbilityExecutionSavepointPort savepointPort,
            Float32ActionStateStore actionStore,
            Float32HandleAllocator handles,
            EquipmentProgramLayout equipmentLayout,
            Float32AbilityExecutionWorkspace workspace);
    }

    internal readonly struct Float32AbilityDomainRuntimeServices
    {
        public Float32AbilityDomainRuntimeServices(
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment)
        {
            GameplayEffects = gameplayEffects;
            Equipment = equipment;
        }

        public Float32GameplayEffectOperationRuntime GameplayEffects { get; }
        public Float32EquipmentRuntime Equipment { get; }
    }

    internal sealed class Float32AbilityDomainRuntimeFactory : IFloat32AbilityDomainRuntimeFactory
    {
        public Float32AbilityDomainRuntimeServices Create(
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            IFloat32AbilityExecutionSavepointPort savepointPort,
            Float32ActionStateStore actionStore,
            Float32HandleAllocator handles,
            EquipmentProgramLayout equipmentLayout,
            Float32AbilityExecutionWorkspace workspace)
        {
            Float32GameplayEffectOperationRuntime gameplayEffects = gameplayEffectCatalog == null
                ? null
                : new Float32GameplayEffectOperationRuntime(
                    access,
                    savepointPort,
                    frame,
                    actionStore,
                    handles,
                    frame.Facts,
                    frame.Presentation,
                    frame.Trace,
                    workspace.GameplayEffects);
            Float32EquipmentRuntime equipment = null;
            if (equipmentLayout != null)
            {
                equipment = new Float32EquipmentRuntime(
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
            return new Float32AbilityDomainRuntimeServices(gameplayEffects, equipment);
        }
    }

    internal sealed class Float32AbilityExecutionServiceFactory : IFloat32AbilityExecutionServiceFactory
    {
        readonly IAbilityTimelineRuntime m_TimelineRuntime;
        public Float32AbilityExecutionServiceFactory(IAbilityTimelineRuntime timelineRuntime)
        {
            m_TimelineRuntime = timelineRuntime;
        }
        public Float32AbilityExecutionAssembly Create(
            Float32GameplayAbilityExecutionData executionData,
            Float32GameplayAbilityExecutionServices executionServices,
            IFloat32AbilityActionBindingProvider actionBindings,
            IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory,
            EquipmentProgramLayout equipmentLayout,
            Float32AbilityExecutionFrame frame,
            IFloat32AbilityExecutionSavepointPort savepointPort,
            IFloat32InputRequestStatePort inputRequests,
            Float32AbilityExecutionWorkspace workspace)
        {
            domainRuntimeFactory = domainRuntimeFactory ?? throw new ArgumentNullException(nameof(domainRuntimeFactory));
            Float32GameplayAbilityExecutionAccess access = executionServices.Access;
            Float32StatePort controlState = frame.CreateStatePort(
                "Control",
                executionServices.ControlPolicy);
            Float32ActionStateStore actionStore = new Float32ActionStateStore(access, frame);
            Float32InputRuntime input = new Float32InputRuntime(access, frame, inputRequests);
            Float32HandleAllocator handles = new Float32HandleAllocator(access, frame);
            Float32BlackboardRuntime blackboard = new Float32BlackboardRuntime(
                access,
                frame.CreateStatePort("Blackboard", executionServices.BlackboardPolicy),
                frame,
                actionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            Float32AbilityDomainRuntimeServices domainServices = domainRuntimeFactory.Create(
                executionServices.GameplayEffectCatalog,
                access,
                frame,
                savepointPort,
                actionStore,
                handles,
                equipmentLayout,
                workspace);
            Float32GameplayEffectOperationRuntime gameplayEffects = domainServices.GameplayEffects;
            Float32EquipmentRuntime equipment = domainServices.Equipment;

            Float32AbilityOperationControlRuntime control = null;
            Float32ActionRuntime actions = new Float32ActionRuntime(
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
            Float32ValueRuntime values = new Float32ValueRuntime(
                access,
                input,
                actionStore,
                actions,
                gameplayEffects,
                equipment,
                blackboard,
                frame,
                controlState,
                workspace);
            Float32MotionAccumulator motion = new Float32MotionAccumulator(
                access,
                frame,
                workspace,
                workspace.TimelineMotionWarps,
                actionStore);
            Float32LocomotionRuntime locomotion = new Float32LocomotionRuntime(
                access,
                values,
                motion,
                frame);
            var treeClipLink = new Float32TreeClipInvokerLink();
            Float32AbilityExecutionTarget target = new Float32AbilityExecutionTarget(
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
                workspace.TimelineStops,
                frame.Tick,
                treeClipLink);
            var services = new Float32AbilityExecutionServiceSet(
                frame,
                target,
                actionStore,
                gameplayEffects,
                equipment,
                values,
                blackboard,
                workspace.TimelineAdvances,
                workspace.TimelineStops);
            control = new Float32AbilityOperationControlRuntime(executionData, services, treeClipLink);
            Float32AbilityDomainRuntime domain = new Float32AbilityDomainRuntime(
                executionData.Binding,
                executionServices,
                actions,
                actionStore,
                control);
            return new Float32AbilityExecutionAssembly(
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
