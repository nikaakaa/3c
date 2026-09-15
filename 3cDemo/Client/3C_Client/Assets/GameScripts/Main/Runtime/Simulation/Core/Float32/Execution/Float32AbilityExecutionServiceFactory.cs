using System;

namespace ThirdPersonSimulation
{
    internal interface IFloat32AbilityDomainRuntimeFactory
    {
        Float32AbilityDomainRuntimeServices Create(
            Float32AbilityExecutionContext execution,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actionStore,
            Float32HandleAllocator handles,
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
            Float32AbilityExecutionContext execution,
            Float32GameplayAbilityExecutionAccess access,
            Float32AbilityExecutionFrame frame,
            Float32ActionStateStore actionStore,
            Float32HandleAllocator handles,
            Float32AbilityExecutionWorkspace workspace)
        {
            Float32GameplayEffectOperationRuntime gameplayEffects = execution.Services.GameplayEffectCatalog == null
                ? null
                : new Float32GameplayEffectOperationRuntime(
                    access,
                    frame,
                    actionStore,
                    handles,
                    frame.Facts,
                    frame.Presentation,
                    frame.Trace,
                    workspace.GameplayEffects);
            Float32EquipmentRuntime equipment = null;
            if (execution.EquipmentLayout != null)
            {
                equipment = new Float32EquipmentRuntime(
                    access,
                    frame,
                    actionStore,
                    handles,
                    gameplayEffects,
                    frame.Facts,
                    frame.Trace,
                    execution.EquipmentLayout ??
                    throw new InvalidOperationException(
                        $"Ability '{execution.Data.AbilityId}' requires the declared Equipment layout."));
            }
            return new Float32AbilityDomainRuntimeServices(gameplayEffects, equipment);
        }
    }

    internal sealed class Float32AbilityExecutionServiceFactory : IFloat32AbilityExecutionServiceFactory
    {
        public Float32AbilityExecutionAssembly Create(
            Float32AbilityExecutionContext execution,
            IFloat32AbilityActionBindingProvider actionBindings,
            IFloat32AbilityDomainRuntimeFactory domainRuntimeFactory,
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionWorkspace workspace)
        {
            domainRuntimeFactory = domainRuntimeFactory ?? throw new ArgumentNullException(nameof(domainRuntimeFactory));
            Float32GameplayAbilityExecutionAccess access = execution.Services.Access;
            Float32StatePort controlState = frame.CreateStatePort(
                "Control",
                execution.Services.ControlPolicy);
            Float32ActionStateStore actionStore = new Float32ActionStateStore(access, frame);
            Float32InputRuntime input = new Float32InputRuntime(access, frame);
            Float32HandleAllocator handles = new Float32HandleAllocator(access, frame);
            Float32BlackboardRuntime blackboard = new Float32BlackboardRuntime(
                access,
                frame.CreateStatePort("Blackboard", execution.Services.BlackboardPolicy),
                frame,
                actionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            Float32AbilityDomainRuntimeServices domainServices = domainRuntimeFactory.Create(
                execution,
                access,
                frame,
                actionStore,
                handles,
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
                workspace);
            Float32MotionAccumulator motion = new Float32MotionAccumulator(
                access,
                frame,
                workspace.MotionContributions,
                workspace.MotionWarpSamples,
                actionStore);
            Float32LocomotionRuntime locomotion = new Float32LocomotionRuntime(
                access,
                values,
                motion,
                frame);
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
                frame.Trace);
            var services = new Float32AbilityExecutionServiceSet(
                frame,
                target,
                actionStore,
                gameplayEffects,
                equipment,
                values,
                blackboard);
            control = new Float32AbilityOperationControlRuntime(execution.Data, services);
            Float32AbilityDomainRuntime domain = new Float32AbilityDomainRuntime(
                execution.Data.Binding,
                execution.Services,
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
