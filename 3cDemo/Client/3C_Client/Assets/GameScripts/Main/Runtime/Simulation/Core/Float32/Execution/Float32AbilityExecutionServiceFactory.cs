using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityExecutionServiceFactory : IFloat32AbilityExecutionServiceFactory
    {
        public Float32AbilityExecutionAssembly Create(
            Float32GameplayAbilityExecutionInstallation installation,
            IFloat32AbilityActionBindingProvider actionBindings,
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionWorkspace workspace)
        {
            Float32GameplayAbilityExecutionAccess access = installation.Access;
            Float32StatePort controlState = frame.CreateStatePort(
                "Control",
                installation.Services.ControlPolicy);
            Float32ActionStateStore actionStore = new Float32ActionStateStore(access, frame);
            Float32InputRuntime input = new Float32InputRuntime(access, frame);
            Float32HandleAllocator handles = new Float32HandleAllocator(access, frame);
            Float32BlackboardRuntime blackboard = new Float32BlackboardRuntime(
                access,
                frame.CreateStatePort("Blackboard", installation.Services.BlackboardPolicy),
                frame,
                actionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            Float32GameplayEffectOperationRuntime gameplayEffects = installation.GameplayEffectCatalog == null
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
            if (installation.RequiresEquipment)
            {
                equipment = new Float32EquipmentRuntime(
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
            control = new Float32AbilityOperationControlRuntime(installation, services);
            Float32AbilityDomainRuntime domain = new Float32AbilityDomainRuntime(
                installation,
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
