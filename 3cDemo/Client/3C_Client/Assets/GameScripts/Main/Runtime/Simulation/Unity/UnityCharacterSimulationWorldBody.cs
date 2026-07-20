using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    internal static class UnityCharacterSimulationWorldBody
    {
        public static WorldBodyState CreateInitial(
            ActorId actorId,
            UnityCharacterControllerWorldBodyBinding binding)
        {
            Vector3 position = binding.LogicRoot.position;
            return new WorldBodyState(
                actorId,
                new Float32Vector3(
                    Float32ScalarBoundary.ConvertExternal(position.x, $"{binding.BindingId}/initial-position-x"),
                    Float32ScalarBoundary.ConvertExternal(position.y, $"{binding.BindingId}/initial-position-y"),
                    Float32ScalarBoundary.ConvertExternal(position.z, $"{binding.BindingId}/initial-position-z")),
                new Float32Yaw(Float32ScalarBoundary.ConvertExternal(
                    Mathf.DeltaAngle(0f, binding.LogicRoot.eulerAngles.y),
                    $"{binding.BindingId}/initial-yaw")),
                Float32Vector3.Zero,
                Float32Scalar.Zero,
                binding.CharacterController.isGrounded,
                WorldCollisionSummary.None);
        }
    }
}
