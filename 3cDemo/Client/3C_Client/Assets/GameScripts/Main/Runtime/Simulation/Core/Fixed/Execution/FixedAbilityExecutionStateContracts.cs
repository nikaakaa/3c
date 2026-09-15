using System;
using System.Collections.Generic;
using SimulationActionActivationRequestState = ThirdPersonSimulation.SimulationActionActivationRequestState<ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot>;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedSkillExecutionState : IDisposable
    {
        AbilityStateValue Get(int slotIndex);
        AbilityStateValue Get(TypedStateAddress address);
        void Set(int slotIndex, AbilityStateValue value);
        void Set(TypedStateAddress address, AbilityStateValue value);
        void Reset(int slotIndex);
        GameplayAbilityExecutionAggregate<AbilityStateValue> GetAbilityExecutionState();
        void SetAbilityExecutionState(GameplayAbilityExecutionAggregate<AbilityStateValue> state);
        FixedMotionWarpState GetMotionWarpState(OperationHandle operation);
        void SetMotionWarpState(OperationHandle operation, FixedMotionWarpState value);
    }

    internal interface IFixedInputRequestStatePort
    {
        SimulationTick Tick { get; }
        SimulationInputRequestState GetInputRequest(string requestId);
        void SetInputRequest(string requestId, SimulationInputRequestState state);
    }

    internal interface IFixedActionRuntimeStatePort
    {
        ulong NextActionEventSequence();
        IReadOnlyList<SimulationActionActivationRequestState> GetActionActivationRequests();
        void SetActionActivationRequests(IReadOnlyList<SimulationActionActivationRequestState> requests);
        IReadOnlyList<FixedActionInstanceState> GetActionInstances();
        void SetActionInstances(IReadOnlyList<FixedActionInstanceState> actions);
    }

    internal interface IFixedHandleAllocatorStatePort
    {
        ulong NextHandleAllocator();
        ulong CaptureHandleAllocator();
        void RestoreHandleAllocator(ulong value);
    }

    internal interface IFixedEventSequenceStatePort
    {
        ulong NextEventSequence();
    }

    internal interface IFixedGameplayEffectStatePort
    {
        int TickRate { get; }
        SimulationGameplayEffectState GetGameplayEffectState(FixedGameplayEffectExecutionScratch scratch);
        GameplayEffectStateAggregate GetGameplayEffectAggregate();
    }

    internal interface IFixedEquipmentStatePort
    {
        EquipmentStateAggregate GetEquipmentState();
        void SetEquipmentState(EquipmentStateAggregate state);
    }
}
