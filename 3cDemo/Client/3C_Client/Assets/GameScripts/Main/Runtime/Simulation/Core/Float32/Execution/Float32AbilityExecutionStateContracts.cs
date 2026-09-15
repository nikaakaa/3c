using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.SimulationActionTargetSnapshot>;

namespace ThirdPersonSimulation
{
    internal interface IFloat32SkillExecutionState : IDisposable
    {
        AbilityStateValue Get(int slotIndex);
        AbilityStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, AbilityStateValue value);
        void Set(TypedStateAddress address, AbilityStateValue value);
        void Reset(int slotIndex);
        GameplayAbilityExecutionAggregate<AbilityStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<AbilityStateValue> state);
        Float32MotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, Float32MotionWarpState value);
    }

    internal interface IFloat32InputRequestStatePort
    {
        SimulationTick Tick { get; }
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
    }

    internal interface IFloat32ActionRuntimeStatePort
    {
        ulong NextActionEventSequence();
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<Float32ActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<Float32ActionInstanceState> actions);
    }

    internal interface IFloat32HandleAllocatorStatePort
    {
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
    }

    internal interface IFloat32EventSequenceStatePort
    {
        ulong NextEventSequence();
    }

    internal interface IFloat32GameplayEffectStatePort
    {
        int TickRate { get; }
        SimulationGameplayEffectState GetGameplayEffectState(Float32GameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
    }

    internal interface IFloat32EquipmentStatePort
    {
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
    }
}
