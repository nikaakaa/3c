using ThirdPersonCharacter.ActionSystem;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public sealed class CharacterActionLifecycleInputStage
    {
        public void Resolve(CharacterNetworkInput input, ActionRuntime runtime)
        {
            if (input == null || runtime == null)
                return;

            for (int i = 0; i < input.Action.LifecycleTransitions.Count; i++)
                runtime.ApplyActionLifecycleTransition(input.Action.LifecycleTransitions[i]);
        }
    }
}
