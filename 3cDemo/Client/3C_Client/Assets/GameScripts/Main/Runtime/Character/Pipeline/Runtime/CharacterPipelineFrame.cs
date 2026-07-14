using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed class CharacterPipelineFrame
    {
        public GameplayLogicTickContext Context { get; private set; }
        public CharacterInputFrame Input { get; private set; } = new CharacterInputFrame();
        public CharacterNetworkInput NetworkInput { get; } = new CharacterNetworkInput();
        public AnimationLayerSelectionBatch AnimationSelections { get; } = new AnimationLayerSelectionBatch();
        public CharacterPipelineOutput Output { get; } = new CharacterPipelineOutput();

        public void Begin(GameplayLogicTickContext context)
        {
            Context = context;
            Input.Begin(context, CharacterInputSource.None, context.InputSequence, null, false);
            NetworkInput.Clear();
            AnimationSelections.Begin();
            Output.Clear();
        }

        public void SetInput(CharacterInputFrame input)
        {
            Input = input;
        }

        public void ClearTransient()
        {
            NetworkInput.Clear();
            Output.Clear();
        }
    }
}
